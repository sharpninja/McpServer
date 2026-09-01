using System.Diagnostics;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
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
/// BUG-TRIAGE-144 / FR-MCP-TRIAGEERR-001: sessionlog_replace_section transient storage
/// failure is retryable and does not drop the in-progress turn.
/// Fixture: in-memory <see cref="McpDbContext"/> plus a SaveChanges interceptor that throws
/// a connection-class SQLite CANTOPEN (error 14) or hangs past the 5s budget.
/// </summary>
public sealed class SessionLogReplaceSectionRetryableTests
{
    private const string WorkspacePath = @"E:\tests\sessionlog-replace-retryable";
    private const string Agent = "Cursor";
    private const string RequestId = "req-20260819T220000Z-entry-001";

    /// <summary>
    /// Named: replace_section unreachable storage retryable true and turn still gettable.
    /// After a CANTOPEN on replace_section, GetAsync still returns the original turn so a
    /// later dialog does not need to recreate it.
    /// </summary>
    [Fact]
    public async Task ReplaceTurnSectionAsync_UnreachableStorage_IsRetryableAndTurnRemainsGettable()
    {
        var dbName = $"SessionLogReplaceRetry_{Guid.NewGuid():N}";
        var sessionId = "Cursor-20260819T220000Z-replace-retryable";

        var seedOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        await using (var seedDb = new McpDbContext(seedOptions))
        {
            seedDb.Database.EnsureCreated();
            seedDb.OverrideWorkspaceId(WorkspacePath);
            var seeder = new SessionLogService(
                seedDb,
                NullLogger<SessionLogService>.Instance,
                Substitute.For<IChangeEventBus>(),
                new WorkspaceContext { WorkspacePath = WorkspacePath });
            await seeder.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        var failingOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new UnreachableSaveChangesInterceptor())
            .Options;
        await using (var failingDb = new McpDbContext(failingOptions))
        {
            failingDb.OverrideWorkspaceId(WorkspacePath);
            var failing = new SessionLogService(
                failingDb,
                NullLogger<SessionLogService>.Instance,
                Substitute.For<IChangeEventBus>(),
                new WorkspaceContext { WorkspacePath = WorkspacePath });

            var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
                failing.ReplaceTurnSectionAsync(
                    Agent,
                    sessionId,
                    RequestId,
                    "tags",
                    new UnifiedRequestEntryDto { RequestId = RequestId, Tags = ["retry-me"] },
                    TestContext.Current.CancellationToken))
                .ConfigureAwait(true);

            var classified = McpErrorClassifier.Classify(ex);
            Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
            Assert.True(classified.Retryable);
        }

        var readOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        await using var readDb = new McpDbContext(readOptions);
        readDb.OverrideWorkspaceId(WorkspacePath);
        var reader = new SessionLogService(
            readDb,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            new WorkspaceContext { WorkspacePath = WorkspacePath });
        var fetched = await reader.GetAsync(
            Agent,
            sessionId,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(fetched);
        var turn = Assert.Single(fetched!.Turns!);
        Assert.Equal(RequestId, turn.RequestId);
        Assert.Equal("in_progress", turn.Status);
        Assert.True(turn.Tags is null || !turn.Tags.Contains("retry-me"));

        var dialogCount = await reader.AppendProcessingDialogAsync(
            Agent,
            sessionId,
            RequestId,
            [new ProcessingDialogItemDto
            {
                Timestamp = "2026-08-19T22:10:00Z",
                Role = "model",
                Category = "observation",
                Content = "storage recovered",
            }],
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(1, dialogCount);
    }

    /// <summary>
    /// BUG-TRIAGE-144: replace_section SaveChanges is wrapped in the 5s storage budget
    /// and classifies as backend_unavailable retryable true.
    /// </summary>
    [Fact]
    public async Task ReplaceTurnSectionAsync_HungSaveChanges_FailsFastWithRetryableUnavailable()
    {
        var dbName = $"SessionLogReplaceHung_{Guid.NewGuid():N}";
        var sessionId = "Cursor-20260819T220000Z-replace-hung";
        var seedOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        await using (var seedDb = new McpDbContext(seedOptions))
        {
            seedDb.Database.EnsureCreated();
            seedDb.OverrideWorkspaceId(WorkspacePath);
            var seeder = new SessionLogService(
                seedDb,
                NullLogger<SessionLogService>.Instance,
                Substitute.For<IChangeEventBus>(),
                new WorkspaceContext { WorkspacePath = WorkspacePath });
            await seeder.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        var hungOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new HungSaveChangesInterceptor())
            .Options;
        await using var hungDb = new McpDbContext(hungOptions);
        hungDb.OverrideWorkspaceId(WorkspacePath);
        var sut = new SessionLogService(
            hungDb,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            new WorkspaceContext { WorkspacePath = WorkspacePath });

        var clock = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<StorageCommandBudgetExceededException>(() =>
            sut.ReplaceTurnSectionAsync(
                Agent,
                sessionId,
                RequestId,
                "tags",
                new UnifiedRequestEntryDto { RequestId = RequestId, Tags = ["hung"] },
                TestContext.Current.CancellationToken))
            .ConfigureAwait(true);
        clock.Stop();

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(8), $"SaveChanges budget took {clock.Elapsed}.");
        var classified = McpErrorClassifier.Classify(ex);
        Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
        Assert.True(classified.Retryable);
    }

    /// <summary>Delays SaveChanges until the storage budget cancels the token.</summary>
    private sealed class HungSaveChangesInterceptor : SaveChangesInterceptor
    {
        /// <inheritdoc />
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
            return result;
        }
    }

    /// <summary>Throws a connection-class SQLite CANTOPEN during SaveChanges.</summary>
    private sealed class UnreachableSaveChangesInterceptor : SaveChangesInterceptor
    {
        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => throw new SqliteException("unable to open database file", 14);
    }

    private static UnifiedSessionLogDto CreateSession(string sessionId)
    {
        return new UnifiedSessionLogDto
        {
            SourceType = Agent,
            SessionId = sessionId,
            Title = "Replace retryable",
            Status = "in_progress",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = RequestId,
                    Timestamp = "2026-08-19T22:00:00Z",
                    QueryText = "replace retryable",
                    Status = "in_progress",
                    PlanFile = SessionLogTurnContextValidator.NoneSentinel,
                    TodoId = SessionLogTurnContextValidator.NoneSentinel,
                    Tags = ["seed-tag"],
                },
            ],
        };
    }
}
