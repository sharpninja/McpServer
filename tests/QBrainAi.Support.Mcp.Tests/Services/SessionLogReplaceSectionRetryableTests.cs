using System.Diagnostics;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.SessionLogReplaceSectionRetryableTestsSupport;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// BUG-TRIAGE-144 / FR-MCP-TRIAGEERR-001: sessionlog_replace_section transient storage
/// failure is retryable and does not drop the in-progress turn.
/// Fixture: in-memory <see cref="McpDbContext"/> plus a SaveChanges interceptor that throws
/// a connection-class SQLite CANTOPEN (error 14) or hangs past the 5s budget.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-TRIAGEERR-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (ReplaceTurnSectionAsync_HungSaveChanges_FailsFastWithRetryableUnavailable) and the helpers only it uses (HungSaveChangesInterceptor) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (WorkspacePath, Agent, RequestId and CreateSession) moved once into Services/SessionLogReplaceSectionRetryableTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class SessionLogReplaceSectionRetryableTests
{
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
}
