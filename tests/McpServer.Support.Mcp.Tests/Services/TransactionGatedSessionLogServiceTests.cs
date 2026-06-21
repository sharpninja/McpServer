using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-161: Session-log mutations execute through the turn transaction
/// coordinator and restore durable session-log records when post-mutation commit
/// fails.
/// </summary>
public sealed class TransactionGatedSessionLogServiceTests
{
    private const string WorkspacePath = @"E:\tests\transaction-gated-sessionlog";
    private const string Agent = "Codex";
    private const string RequestId = "req-20260614T120000Z-seed-sessionlog-gate";

    /// <summary>
    /// TEST-MCP-161: A pre-mutation coordinator rejection prevents the session-log
    /// submit from creating a parent session row or any child rows.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotPersistSession()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TransactionFailureReason.UnknownKey,
            Message = "signing failed",
        };
        var sessionId = BuildSessionId("submit-rejected-before-mutation");
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => sut.SubmitAsync(CreateSession(sessionId)))
                .ConfigureAwait(true);
            Assert.Contains("signing failed", ex.Message, StringComparison.Ordinal);
        }

        Assert.NotNull(coordinator.Request);
        Assert.Equal("sessionlog.submit", coordinator.Request.OperationName);
        Assert.Equal(0, CountSessionRows(connection, sessionId));
        Assert.Equal(0, CountTurnRows(connection, sessionId));
        Assert.Equal(0, CountAllChildRows(connection, sessionId));
    }

    /// <summary>
    /// TEST-MCP-161: When an add-style submit succeeds locally and the subscriber
    /// rejects afterward, rollback preserves the created session-log record.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_WhenCommitFailsAfterCreatedSession_RestoresCreatedSessionRecord()
    {
        using var connection = OpenConnection();
        var sessionId = BuildSessionId("submit-created-restore");
        long? createdSessionRowId = null;
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
            BeforeRollback = () => createdSessionRowId = ScalarLong(
                connection,
                "SELECT Id FROM SessionLogs WHERE SessionId = $sid",
                ("$sid", sessionId)),
        };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => sut.SubmitAsync(CreateSession(sessionId)))
                .ConfigureAwait(true);
            Assert.Contains("Rollback completed", ex.Message, StringComparison.Ordinal);
        }

        Assert.True(coordinator.RollbackAttempted);
        Assert.True(coordinator.RollbackSucceeded);
        Assert.NotNull(createdSessionRowId);
        Assert.Equal(
            createdSessionRowId.Value,
            ScalarLong(connection, "SELECT Id FROM SessionLogs WHERE SessionId = $sid", ("$sid", sessionId)));
        Assert.Equal(1, CountSessionRows(connection, sessionId));
        Assert.Equal(1, CountTurnRows(connection, sessionId));
        Assert.True(CountAllChildRows(connection, sessionId) > 0);

        var restored = await GetSessionAsync(connection, sessionId).ConfigureAwait(true);
        Assert.NotNull(restored);
        Assert.Equal("Seed Session", restored!.Title);
        Assert.NotNull(restored.Turns);
        Assert.Single(restored.Turns!);
        Assert.Equal("seed response", restored.Turns![0].Response);
    }

    /// <summary>
    /// TEST-MCP-161: A failed post-mutation replace restores the prior turn
    /// scalars and all child sections from the pre-image graph.
    /// </summary>
    [Fact]
    public async Task ReplaceTurnAsync_WhenCommitFailsAfterMutation_RestoresPriorTurnGraph()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => sut.ReplaceTurnAsync(
                        Agent,
                        sessionId,
                        new UnifiedRequestEntryDto
                        {
                            RequestId = RequestId,
                            Status = "completed",
                            Actions =
                            [
                                new UnifiedActionDto
                                {
                                    Order = 0,
                                    Description = "replacement action",
                                    Type = "edit",
                                    Status = "completed",
                                },
                            ],
                        }))
                .ConfigureAwait(true);
            Assert.Contains("Rollback completed", ex.Message, StringComparison.Ordinal);
        }

        var restored = await GetTurnAsync(connection, sessionId).ConfigureAwait(true);
        Assert.Equal("seed query", restored.QueryText);
        Assert.Equal("seed response", restored.Response);
        Assert.Equal("in_progress", restored.Status);
        Assert.Equal(new[] { "seed-tag-a", "seed-tag-b" }, restored.Tags!.OrderBy(tag => tag).ToArray());
        Assert.Equal(2, restored.Actions!.Count);
        Assert.Equal(2, restored.ProcessingDialog!.Count);
        Assert.Equal(2, restored.Commits!.Count);
        Assert.Equal(2, restored.DesignDecisions!.Count);
        Assert.Equal(16, CountVisibleChildRows(connection, sessionId));
        Assert.True(CountSoftDeletedChildRows(connection, sessionId) > 0);
    }

    /// <summary>
    /// TEST-MCP-161: A failed post-mutation session delete restores the parent
    /// session, its turn, and every child row from the pre-delete graph.
    /// </summary>
    [Fact]
    public async Task DeleteSessionAsync_WhenCommitFailsAfterMutation_RestoresPriorSessionGraph()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => sut.DeleteSessionAsync(Agent, sessionId))
                .ConfigureAwait(true);
            Assert.Contains("Rollback completed", ex.Message, StringComparison.Ordinal);
        }

        Assert.Equal(1, CountSessionRows(connection, sessionId));
        Assert.Equal(1, CountTurnRows(connection, sessionId));
        Assert.Equal(16, CountVisibleChildRows(connection, sessionId));
        Assert.True(CountSoftDeletedChildRows(connection, sessionId) > 0);

        var restored = await GetSessionAsync(connection, sessionId).ConfigureAwait(true);
        Assert.NotNull(restored);
        Assert.NotNull(restored!.Turns);
        var turn = Assert.Single(restored.Turns!);
        Assert.Equal("seed response", turn.Response);
        Assert.Equal(new[] { "seed-tag-a", "seed-tag-b" }, turn.Tags!.OrderBy(tag => tag).ToArray());
    }

    /// <summary>
    /// TR-MCP-DB-003: rollback code must not bypass tracked soft-delete protection with
    /// bulk physical delete operations.
    /// </summary>
    [Fact]
    public void TransactionGatedSessionLogServiceSource_DoesNotUseBulkPhysicalDeletes()
    {
        var sourcePath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "McpServer.Support.Mcp",
            "Services",
            "TransactionGatedSessionLogService.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("ExecuteDelete", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE FROM", source, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-161: Workspace-stamp repair dry-run remains a direct pass-through.
    /// </summary>
    [Fact]
    public async Task RepairWorkspaceStampsAsync_DryRunDoesNotUseCoordinator()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator();
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
            Assert.Equal(0, await sut.RepairWorkspaceStampsAsync(dryRun: true).ConfigureAwait(true));

        Assert.Null(coordinator.Request);
    }

    /// <summary>Applied workspace-stamp repair fails closed while required transactions are active.</summary>
    [Fact]
    public async Task RepairWorkspaceStampsAsync_WhenTransactionsRequired_FailsClosedWithoutCoordinatorExecute()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator();
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => sut.RepairWorkspaceStampsAsync(dryRun: false))
                .ConfigureAwait(true);
            Assert.Contains("not transaction compensated", ex.Message, StringComparison.Ordinal);
        }

        Assert.Null(coordinator.Request);
    }

    /// <summary>Applied workspace-stamp repair fails closed when the transaction coordinator is degraded.</summary>
    [Fact]
    public async Task RepairWorkspaceStampsAsync_WhenCoordinatorDegraded_FailsClosedWithoutCoordinatorExecute()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator { StatusDegraded = true, StatusMessage = "txn degraded" };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => sut.RepairWorkspaceStampsAsync(dryRun: false))
                .ConfigureAwait(true);
            Assert.Contains("txn degraded", ex.Message, StringComparison.Ordinal);
        }

        Assert.Null(coordinator.Request);
    }

    /// <summary>Applied workspace-stamp repair delegates when transaction gating is not required for mutations.</summary>
    [Fact]
    public async Task RepairWorkspaceStampsAsync_WhenTransactionsNotRequired_Delegates()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator();
        var (sut, db) = BuildGatedSut(
            connection,
            coordinator,
            new TurnTransactionOptions { Enabled = true, RequiredForMutations = false });

        using (db)
            Assert.Equal(0, await sut.RepairWorkspaceStampsAsync(dryRun: false).ConfigureAwait(true));

        Assert.Null(coordinator.Request);
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static (TransactionGatedSessionLogService Sut, McpDbContext Db) BuildGatedSut(
        SqliteConnection connection,
        CapturingCoordinator coordinator,
        TurnTransactionOptions? transactionOptions = null)
    {
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options;
        var workspaceContext = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var db = new McpDbContext(options, workspaceContext);
        db.Database.EnsureCreated();
        var inner = new SessionLogService(
            db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            workspaceContext);
        var sut = new TransactionGatedSessionLogService(
            inner,
            db,
            coordinator,
            workspaceContext,
            MsOptions.Options.Create(transactionOptions ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));
        return (sut, db);
    }

    private static string SeedFullSession(SqliteConnection connection)
    {
        var sessionId = BuildSessionId("seed");
        var (sut, db) = BuildGatedSut(connection, new CapturingCoordinator());
        using (db)
            sut.SubmitAsync(CreateSession(sessionId)).GetAwaiter().GetResult();
        return sessionId;
    }

    private static UnifiedSessionLogDto CreateSession(string sessionId)
        => new()
        {
            SourceType = Agent,
            SessionId = sessionId,
            Title = "Seed Session",
            Model = "gpt-5.4",
            Started = "2026-06-14T12:00:00Z",
            LastUpdated = "2026-06-14T12:30:00Z",
            Status = "in_progress",
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = RequestId,
                    Timestamp = "2026-06-14T12:01:00Z",
                    QueryText = "seed query",
                    QueryTitle = "seed",
                    Response = "seed response",
                    Status = "in_progress",
                    Tags = ["seed-tag-a", "seed-tag-b"],
                    ContextList = ["docs/a.md", "docs/b.md"],
                    Actions =
                    [
                        new UnifiedActionDto { Order = 0, Description = "action zero", Type = "edit", Status = "completed", FilePath = "src/a.cs" },
                        new UnifiedActionDto { Order = 1, Description = "action one", Type = "create", Status = "completed", FilePath = "src/b.cs" },
                    ],
                    ProcessingDialog =
                    [
                        new ProcessingDialogItemDto { Timestamp = "2026-06-14T12:02:00Z", Role = "model", Content = "thinking", Category = "reasoning" },
                        new ProcessingDialogItemDto { Timestamp = "2026-06-14T12:03:00Z", Role = "tool", Content = "ran", Category = "tool_call" },
                    ],
                    Commits =
                    [
                        new SessionLogCommitDto { Sha = "sha-1", Branch = "main", Message = "first", Author = "Codex" },
                        new SessionLogCommitDto { Sha = "sha-2", Branch = "main", Message = "second", Author = "Codex" },
                    ],
                    DesignDecisions = ["chose A over B", "deferred C"],
                    RequirementsDiscovered = ["FR-MCP-120"],
                    FilesModified = ["src/a.cs", "src/b.cs"],
                    Blockers = ["needs review"],
                },
            ],
        };

    private static async Task<UnifiedSessionLogDto?> GetSessionAsync(SqliteConnection connection, string sessionId)
    {
        var (sut, db) = BuildGatedSut(connection, new CapturingCoordinator());
        using (db)
            return await sut.GetAsync(Agent, sessionId).ConfigureAwait(true);
    }

    private static async Task<UnifiedRequestEntryDto> GetTurnAsync(SqliteConnection connection, string sessionId)
    {
        var session = await GetSessionAsync(connection, sessionId).ConfigureAwait(true);
        Assert.NotNull(session);
        Assert.NotNull(session!.Turns);
        return session.Turns!.Single(turn => turn.RequestId == RequestId);
    }

    private static int CountSessionRows(SqliteConnection connection, string sessionId)
        => ScalarCount(connection, "SELECT COUNT(*) FROM SessionLogs WHERE SessionId = $sid", ("$sid", sessionId));

    private static int CountTurnRows(SqliteConnection connection, string sessionId)
        => ScalarCount(
            connection,
            "SELECT COUNT(*) FROM SessionLogTurns t JOIN SessionLogs s ON s.Id = t.SessionLogId WHERE s.SessionId = $sid",
            ("$sid", sessionId));

    private static int CountAllChildRows(SqliteConnection connection, string sessionId)
        => CountChildRows(connection, sessionId, null);

    private static int CountVisibleChildRows(SqliteConnection connection, string sessionId)
        => CountChildRows(connection, sessionId, isDeleted: false);

    private static int CountSoftDeletedChildRows(SqliteConnection connection, string sessionId)
        => CountChildRows(connection, sessionId, isDeleted: true);

    private static int CountChildRows(SqliteConnection connection, string sessionId, bool? isDeleted)
    {
        var tables = new[]
        {
            "SessionLogActions", "SessionLogTurnTags", "SessionLogTurnContexts",
            "SessionLogProcessingDialogs", "SessionLogCommits", "SessionLogTurnStringLists",
        };
        var total = 0;
        var deletedPredicate = isDeleted.HasValue ? " AND c.IsDeleted = $isDeleted" : string.Empty;
        foreach (var table in tables)
        {
            total += ScalarCount(
                connection,
                $"SELECT COUNT(*) FROM {table} c JOIN SessionLogTurns t ON t.Id = c.SessionLogTurnId " +
                $"JOIN SessionLogs s ON s.Id = t.SessionLogId WHERE s.SessionId = $sid{deletedPredicate}",
                isDeleted.HasValue ? [("$sid", sessionId), ("$isDeleted", isDeleted.Value ? 1 : 0)] : [("$sid", sessionId)]);
        }

        return total;
    }

    private static string FindRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static int ScalarCount(SqliteConnection connection, string sql, params (string Name, object Value)[] args)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static long ScalarLong(SqliteConnection connection, string sql, params (string Name, object Value)[] args)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private static string BuildSessionId(string suffix) => $"{Agent}-20260614T120000Z-{suffix}";

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        public TurnTransactionRequest? Request { get; private set; }

        public bool InvokeMutation { get; init; } = true;

        public bool InvokeRollback { get; init; }

        public string Status { get; init; } = "committed";

        public TransactionFailureReason Reason { get; init; } = TransactionFailureReason.None;

        public string? Message { get; init; }

        public bool StatusEnabled { get; init; } = true;

        public bool StatusDegraded { get; init; }

        public string StatusMessage { get; init; } = "ready";

        public bool RollbackAttempted { get; private set; }

        public bool RollbackSucceeded { get; private set; }

        public Action? BeforeRollback { get; init; }

        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
            => ExecuteCoreAsync(request, mutation, cancellationToken);

        public TurnTransactionStatusResponse GetStatus()
            => new()
            {
                Enabled = StatusEnabled,
                Degraded = StatusDegraded,
                LastReason = TransactionFailureReason.None,
                Message = StatusMessage,
            };

        private async Task<TurnTransactionResult> ExecuteCoreAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken)
        {
            Request = request;
            TurnMutationResult? mutationResult = null;
            string? rollbackError = null;

            if (InvokeMutation)
            {
                mutationResult = await mutation(cancellationToken).ConfigureAwait(false);
                if (InvokeRollback && mutationResult.RollbackAsync is not null)
                {
                    RollbackAttempted = true;
                    BeforeRollback?.Invoke();
                    try
                    {
                        await mutationResult.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        RollbackSucceeded = true;
                    }
                    catch (Exception ex)
                    {
                        rollbackError = ex.Message;
                    }
                }
            }

            return new TurnTransactionResult
            {
                TransactionId = request.TransactionId ?? "txn-sessionlog-test",
                Status = Status,
                Reason = Reason,
                MutationApplied = InvokeMutation,
                MutationResult = mutationResult,
                Message = Message,
                RollbackAttempted = RollbackAttempted,
                RollbackSucceeded = RollbackSucceeded,
                RollbackError = rollbackError,
            };
        }
    }
}
