using System.Data.Common;
using System.Diagnostics;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-007 / FR-MCP-TRIAGESTORE-002: dense beginTurn/completeTurn loads
/// must not cartesian-join sibling session collections, and a failed graph load must not
/// report success or treat the session as missing.
/// Fixture: Sqlite session with two turns that each carry actions, tags, context, dialog,
/// commits, and string lists, plus session tags.
/// </summary>
public sealed class SessionLogDenseGraphTests
{
    private const string WorkspacePath = @"E:\tests\sessionlog-dense-graph";
    private const string Agent = "Cursor";
    private const string SessionId = "Cursor-20260928T220000Z-dense-graph";
    private const string ExistingRequestId = "req-20260928T220000Z-dense-existing";
    private const string SecondRequestId = "req-20260928T220001Z-dense-second";
    private const string NewRequestId = "req-20260928T220002Z-dense-new";

    /// <summary>
    /// TEST-MCP-TRIAGESTORE-007: UpsertTurn (beginTurn/completeTurn) loads sibling collections
    /// as separate queries. One SELECT must not join SessionLogActions and SessionLogTurnTags.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_DenseGraph_UsesSplitQueriesNotCartesianJoin()
    {
        await using var connection = OpenConnection();
        await SeedDenseSessionAsync(connection).ConfigureAwait(true);

        var capture = new CommandCaptureInterceptor();
        var (sut, db) = BuildSut(connection, capture);
        using (db)
        {
            var actionsBefore = Count(connection, "SELECT COUNT(*) FROM SessionLogActions");
            var tagsBefore = Count(connection, "SELECT COUNT(*) FROM SessionLogTurnTags");

            await sut.UpsertTurnAsync(Agent, SessionId, NewTurn(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            var selects = capture.Commands.Where(static sql => sql.Contains("SELECT", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.Contains(selects, static sql => sql.Contains("SessionLogActions", StringComparison.Ordinal));
            Assert.Contains(selects, static sql => sql.Contains("SessionLogTurnTags", StringComparison.Ordinal));
            Assert.DoesNotContain(
                selects,
                static sql => sql.Contains("SessionLogActions", StringComparison.Ordinal)
                    && sql.Contains("SessionLogTurnTags", StringComparison.Ordinal));
            Assert.Equal(actionsBefore, Count(connection, "SELECT COUNT(*) FROM SessionLogActions"));
            Assert.Equal(tagsBefore, Count(connection, "SELECT COUNT(*) FROM SessionLogTurnTags"));
            Assert.Equal(1, Count(connection, "SELECT COUNT(*) FROM SessionLogTurns t JOIN SessionLogs s ON s.Id = t.SessionLogId WHERE t.RequestId = 'req-20260928T220002Z-dense-new'"));
        }
    }

    /// <summary>
    /// TEST-MCP-TRIAGESTORE-007: a graph load that exceeds the command budget fails retryable
    /// and does not insert the turn or clear the existing session.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_SlowGraphLoad_FailsRetryableWithoutPersisting()
    {
        await using var connection = OpenConnection();
        await SeedDenseSessionAsync(connection).ConfigureAwait(true);

        var (sut, db) = BuildSut(connection, new DelayedReaderInterceptor(TimeSpan.FromSeconds(4)), budgetSeconds: 1);
        using (db)
        {
            var turnsBefore = Count(connection, "SELECT COUNT(*) FROM SessionLogTurns");
            var clock = Stopwatch.StartNew();
            var ex = await Assert.ThrowsAsync<StorageCommandBudgetExceededException>(() =>
                sut.UpsertTurnAsync(Agent, SessionId, NewTurn(), TestContext.Current.CancellationToken)).ConfigureAwait(true);
            clock.Stop();

            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(3), $"Graph load budget took {clock.Elapsed}.");
            var classified = McpErrorClassifier.Classify(ex);
            Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
            Assert.True(classified.Retryable);
            Assert.NotEqual(McpErrorClassifier.NotFound, classified.Code);
            Assert.Equal(turnsBefore, Count(connection, "SELECT COUNT(*) FROM SessionLogTurns"));
            Assert.Equal(0, Count(connection, "SELECT COUNT(*) FROM SessionLogTurns t WHERE t.RequestId = 'req-20260928T220002Z-dense-new'"));
            Assert.Equal(1, Count(connection, "SELECT COUNT(*) FROM SessionLogTurns t WHERE t.RequestId = 'req-20260928T220000Z-dense-existing'"));
        }
    }

    /// <summary>
    /// TEST-MCP-TRIAGESTORE-007: SQL 1205 during graph load is retryable backend_unavailable.
    /// The session is not reported missing and the new turn is not persisted.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_SqlDeadlock1205_DoesNotReportMissingSession()
    {
        await using var connection = OpenConnection();
        await SeedDenseSessionAsync(connection).ConfigureAwait(true);

        var (sut, db) = BuildSut(connection, new DeadlockReaderInterceptor());
        using (db)
        {
            var turnsBefore = Count(connection, "SELECT COUNT(*) FROM SessionLogTurns");
            var ex = await Assert.ThrowsAsync<StorageGraphMaterializationException>(() =>
                sut.UpsertTurnAsync(Agent, SessionId, NewTurn(), TestContext.Current.CancellationToken)).ConfigureAwait(true);

            Assert.Contains("not treated as missing", ex.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
            var classified = McpErrorClassifier.Classify(ex);
            Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
            Assert.True(classified.Retryable);
            Assert.Equal(503, classified.StatusCode);
            Assert.Equal(turnsBefore, Count(connection, "SELECT COUNT(*) FROM SessionLogTurns"));
            Assert.Equal(1, Count(connection, "SELECT COUNT(*) FROM SessionLogs WHERE SessionId = 'Cursor-20260928T220000Z-dense-graph'"));
        }
    }

    private static async Task SeedDenseSessionAsync(SqliteConnection connection)
    {
        var (sut, db) = BuildSut(connection, interceptor: null);
        using (db)
        {
            db.Database.EnsureCreated();
            await sut.SubmitAsync(CreateDenseSession(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
    }

    private static (SessionLogService Sut, McpDbContext Db) BuildSut(
        SqliteConnection connection,
        DbCommandInterceptor? interceptor,
        int? budgetSeconds = null)
    {
        var builder = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection);
        if (interceptor is not null)
            builder.AddInterceptors(interceptor);

        var workspace = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var db = new McpDbContext(builder.Options, workspace);
        db.OverrideWorkspaceId(WorkspacePath);
        var submitOptions = budgetSeconds is null
            ? null
            : Microsoft.Extensions.Options.Options.Create(new SessionLogSubmitOptions { SubmitCommandBudgetSeconds = budgetSeconds.Value });
        var sut = new SessionLogService(
            db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            workspace,
            submitOptions);
        return (sut, db);
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static long Count(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)(command.ExecuteScalar() ?? 0L);
    }

    private static UnifiedRequestEntryDto NewTurn() => new()
    {
        RequestId = NewRequestId,
        Timestamp = "2026-09-28T22:00:02Z",
        QueryText = "begin turn on a dense session",
        Status = "in_progress",
        PlanFile = SessionLogTurnContextValidator.NoneSentinel,
        TodoId = SessionLogTurnContextValidator.NoneSentinel,
    };

    private static UnifiedSessionLogDto CreateDenseSession()
    {
        return new UnifiedSessionLogDto
        {
            SourceType = Agent,
            SessionId = SessionId,
            Title = "Dense graph",
            Status = "in_progress",
            TurnCount = 2,
            Tags = ["dense", "slice-1"],
            Turns =
            [
                DenseTurn(ExistingRequestId, "2026-09-28T22:00:00Z"),
                DenseTurn(SecondRequestId, "2026-09-28T22:00:01Z"),
            ],
        };
    }

    private static UnifiedRequestEntryDto DenseTurn(string requestId, string timestamp) => new()
    {
        RequestId = requestId,
        Timestamp = timestamp,
        QueryText = "dense turn " + requestId,
        Status = "in_progress",
        PlanFile = SessionLogTurnContextValidator.NoneSentinel,
        TodoId = SessionLogTurnContextValidator.NoneSentinel,
        Tags = ["turn-tag-a", "turn-tag-b"],
        ContextList = ["docs/a.md", "docs/b.md"],
        Actions =
        [
            new UnifiedActionDto { Order = 0, Description = "edit a", Type = "edit", Status = "completed", FilePath = "src/a.cs" },
            new UnifiedActionDto { Order = 1, Description = "edit b", Type = "edit", Status = "completed", FilePath = "src/b.cs" },
        ],
        ProcessingDialog =
        [
            new ProcessingDialogItemDto { Timestamp = timestamp, Role = "model", Content = "thinking", Category = "reasoning" },
            new ProcessingDialogItemDto { Timestamp = timestamp, Role = "tool", Content = "ran", Category = "tool_call" },
        ],
        Commits =
        [
            new SessionLogCommitDto
            {
                Sha = "sha-" + requestId,
                Branch = "develop",
                Message = "dense",
                Author = "Cursor",
                FilesChanged = ["src/a.cs"],
            },
        ],
        DesignDecisions = ["split the include graph"],
        RequirementsDiscovered = ["FR-MCP-TRIAGESTORE-002"],
        FilesModified = ["src/a.cs", "src/b.cs"],
        Blockers = ["cartesian join"],
    };

    /// <summary>Records reader command text issued while a session graph is loaded.</summary>
    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        /// <summary>Command texts observed for this context.</summary>
        public List<string> Commands { get; } = [];

        /// <inheritdoc />
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    /// <summary>Delays each reader command so an unbudgeted graph load outlasts the storage budget.</summary>
    private sealed class DelayedReaderInterceptor : DbCommandInterceptor
    {
        private readonly TimeSpan _delay;

        /// <summary>Initializes the interceptor.</summary>
        /// <param name="delay">How long each reader waits.</param>
        public DelayedReaderInterceptor(TimeSpan delay) => _delay = delay;

        /// <inheritdoc />
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Fails the first reader with SQL Server deadlock 1205.</summary>
    private sealed class DeadlockReaderInterceptor : DbCommandInterceptor
    {
        /// <inheritdoc />
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
            => throw SqlExceptionFactory.Create(1205, "Transaction was deadlocked on lock resources and chosen as the deadlock victim.");

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => throw SqlExceptionFactory.Create(1205, "Transaction was deadlocked on lock resources and chosen as the deadlock victim.");
    }
}
