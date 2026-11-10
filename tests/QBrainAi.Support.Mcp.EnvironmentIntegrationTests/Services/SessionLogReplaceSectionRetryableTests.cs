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

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// BUG-TRIAGE-144 / FR-MCP-TRIAGEERR-001: sessionlog_replace_section transient storage
/// failure is retryable and does not drop the in-progress turn.
/// Fixture: in-memory <see cref="McpDbContext"/> plus a SaveChanges interceptor that throws
/// a connection-class SQLite CANTOPEN (error 14) or hangs past the 5s budget.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-TRIAGEERR-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/SessionLogReplaceSectionRetryableTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case and the helpers only it uses (HungSaveChangesInterceptor) moved unchanged with the class attributes; the helpers both halves use come from Services/SessionLogReplaceSectionRetryableTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class SessionLogReplaceSectionRetryableTests
{
    /// <summary>
    /// BUG-TRIAGE-144: replace_section SaveChanges is wrapped in the 5s storage budget
    /// and classifies as backend_unavailable retryable true.
    /// </summary>
    /// <remarks>
    /// Integration: its outcome depends on a real storage-command budget timer firing while the stalled step runs, a wall-clock wait under TR-MCP-USECASE-018 AC-006; its elapsed check is a hang guard only.
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-05</term><description>TR-MCP-USECASE-018 AC-006 (a wall-clock bound makes a case an integration case): which budget elapsed is read from the exception message (the 5 second intake budget), and the elapsed time keeps a 30 second hang guard (below the interceptor's 60 second stall) instead of the 8 second bound, because the sibling sweep of the Windows integration lane failure at b79fd51f (slice Z5) found this case at 5.07 to 8.64 seconds of run time across nine Windows lanes (ug-r13z5-win-lane-elapsed-bound-durations-1.txt): its bound also timed the host. The classification line above was restated after the eighth holistic read before hostile validation round 13 (finding b2), because it still named the elapsed bound this entry removed.</description></item>
    /// </list>
    /// </remarks>
    [Trait("Category", "Integration")]
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

        Assert.Contains("within the 5 second intake budget", ex.Message, StringComparison.Ordinal);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(30), $"The stalled SaveChanges was not cut short by its budget: {clock.Elapsed}.");
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
}
