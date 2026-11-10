using System.Diagnostics;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-001: session-log persist is idempotent on identical actions,
/// session tags persist, replace missing turn is not-found, canceled statuses round-trip.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGESTORE-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/SessionLogTriageStoreTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case and the helpers only it uses (HungSaveChangesInterceptor) moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class SessionLogTriageStoreTestsBase in Services/SessionLogTriageStoreTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class SessionLogTriageStoreTests : SessionLogTriageStoreTestsBase, IDisposable
{
    /// <summary>
    /// TEST-MCP-TRIAGESTORE-007: Submit SaveChanges uses the configured command budget
    /// and surfaces backend_unavailable when that budget elapses. This fixture sets 1 second
    /// so the test does not wait for the 30 second default.
    /// </summary>
    /// <remarks>
    /// Integration: its outcome depends on a real storage-command budget timer firing while the stalled step runs, a wall-clock wait under TR-MCP-USECASE-018 AC-006; its elapsed check is a hang guard only.
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-05</term><description>TEST-MCP-TRIAGESTORE-007; TR-MCP-USECASE-018 AC-006 (a wall-clock bound makes a case an integration case): which budget elapsed is read from the exception message (the configured 1 second storage command budget, so neither the 5 second intake budget nor the 30 second submit default passes), the stalled SaveChanges is proved reached, and the elapsed time keeps a 20 second hang guard (below that default and the interceptor's 60 second stall) instead of the 4 and 5 second bounds, because the Windows integration lane ug-r13z4c-final-win-lane-1 measured 4.42 seconds and the case took about 1 or about 4 seconds across nine Windows lanes (ug-r13z4c-win-lane-hungsave-durations-1.txt): the old bound timed the whole Submit and the host's scheduling, not the budget. The classification line above was restated after the eighth holistic read before hostile validation round 13 (finding b2), because it still named the elapsed bound this entry removed.</description></item>
    /// </list>
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task SubmitAsync_HungSaveChanges_FailsFastWithStorageUnavailable()
    {
        var interceptor = new HungSaveChangesInterceptor();
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogHungSave_{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;
        await using var db = new McpDbContext(options);
        db.Database.EnsureCreated();
        db.OverrideWorkspaceId(WorkspacePath);
        var sut = new SessionLogService(
            db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            new WorkspaceContext { WorkspacePath = WorkspacePath },
            Microsoft.Extensions.Options.Options.Create(new SessionLogSubmitOptions { SubmitCommandBudgetSeconds = 1 }));

        var clock = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<StorageCommandBudgetExceededException>(() =>
            sut.SubmitAsync(CreateSession("Cursor-20260818T220000Z-hung-save"), cancellationToken: TestContext.Current.CancellationToken))
            .ConfigureAwait(true);
        clock.Stop();

        Assert.True(interceptor.Entered, "SaveChanges was never reached, so no budget cut a stalled save.");
        Assert.Contains("within the 1 second storage command budget", ex.Message, StringComparison.Ordinal);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(20), $"The stalled SaveChanges was not cut short by its budget: {clock.Elapsed}.");
        var classified = McpErrorClassifier.Classify(ex);
        Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
        Assert.True(classified.Retryable);
    }

    /// <summary>Delays SaveChanges until the storage budget cancels the token, and records that SaveChanges was reached.</summary>
    /// <remarks>2026-11-05, TEST-MCP-TRIAGESTORE-007: records its entry, so the hung-save case proves the budget cut a stalled SaveChanges (slice Z5, after the Windows integration lane ug-r13z4c-final-win-lane-1).</remarks>
    private sealed class HungSaveChangesInterceptor : SaveChangesInterceptor
    {
        /// <summary>Whether SaveChanges reached this interceptor.</summary>
        internal bool Entered { get; private set; }

        /// <inheritdoc />
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Entered = true;
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}
