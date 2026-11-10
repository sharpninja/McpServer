using System.Data;
using System.Diagnostics;
using QBrainAi.Support.Mcp.Controllers;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Requirements.RequirementsRecoveryTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Requirements;

/// <summary>
/// TEST-MCP-REQRECOVERY-001: atomic requirements recovery on SQLite.
/// Dry-run writes nothing. Apply is one serializable commit. A second item failure or a
/// failed SaveChanges leaves zero requirement rows and zero run rows.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-REQRECOVERY-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Requirements/RequirementsRecoveryTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case and the helpers only it uses (DelayedSaveInterceptor) moved unchanged with the class attributes; the helpers both halves use come from Requirements/RequirementsRecoveryTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class RequirementsRecoveryTests
{
    /// <summary>A hung SaveChanges is retryable HTTP 503 and persists nothing.</summary>
    /// <remarks>
    /// Integration: its outcome depends on a real storage-command budget timer firing while the delayed step runs, a wall-clock wait under TR-MCP-USECASE-018 AC-006; its elapsed check is a hang guard only.
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-05</term><description>TR-MCP-USECASE-018 AC-006 (a wall-clock bound makes a case an integration case): which budget elapsed is read from the exception message (the configured 1 second storage command budget; the delayed save would finish after 4 seconds and throw nothing), and the elapsed time keeps a 20 second hang guard instead of the 3 second bound, because the sibling sweep of the Windows integration lane failure at b79fd51f (slice Z5) found this case at up to 7.12 seconds of run time across nine Windows lanes (ug-r13z5-win-lane-elapsed-bound-durations-1.txt): its bound also timed the host. The classification line above was restated after the eighth holistic read before hostile validation round 13 (finding b2), because it still named the elapsed bound this entry removed.</description></item>
    /// </list>
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ApplyAsync_HungSave_IsRetryable503()
    {
        await using var connection = OpenConnection();
        var (sut, db) = Build(connection, new DelayedSaveInterceptor(TimeSpan.FromSeconds(4)), budget: TimeSpan.FromSeconds(1));
        using (db)
        {
            db.Database.EnsureCreated();
            var clock = Stopwatch.StartNew();
            var ex = await Assert.ThrowsAsync<StorageCommandBudgetExceededException>(() => sut.ApplyAsync(
                Request("slow-001", Item("fr", "FR-MCP-REQRECOVERY-001", "slow")),
                TestContext.Current.CancellationToken)).ConfigureAwait(true);
            clock.Stop();

            Assert.Contains("within the 1 second storage command budget", ex.Message, StringComparison.Ordinal);
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(20), $"The recovery save was not cut short by its budget: {clock.Elapsed}.");
            var classified = McpErrorClassifier.Classify(ex);
            Assert.Equal(503, classified.StatusCode);
            Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
            Assert.True(classified.Retryable);
            Assert.Equal(0, Count(connection, "SELECT COUNT(*) FROM Requirements"));
            Assert.Equal(0, Count(connection, "SELECT COUNT(*) FROM RequirementsRecoveryRuns"));
        }
    }

    /// <summary>Delays SaveChanges until the caller cancels or the delay elapses.</summary>
    private sealed class DelayedSaveInterceptor : SaveChangesInterceptor
    {
        private readonly TimeSpan _delay;

        public DelayedSaveInterceptor(TimeSpan delay) => _delay = delay;

        /// <inheritdoc />
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
        }
    }
}
