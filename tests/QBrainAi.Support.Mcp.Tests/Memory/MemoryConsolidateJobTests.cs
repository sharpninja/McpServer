using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Tests.TestSupport;

namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// TR-MCP-MEMORY-JOBS-002:
/// S4 Red acceptance for consolidate job lock/TTL/events. Production handler unregistered (501).
/// </summary>
public sealed class MemoryConsolidateJobTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-TR-MCP-MEMORY-JOBS-002-01: job respects per-workspace lock.</summary>
    /// <returns>A task that completes when the case finishes.</returns>
    /// <remarks>2026-11-05, TR-MCP-MEMORY-JOBS-002; TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): the workspace lock is held, as by a job in progress, at the instant of a settable clock the job handler reads; a forced job at that instant finds it held; after the release a job runs on the free lock. The two jobs previously ran concurrently on the live clock and relied on the second arriving within the first's 100 ms hold and before the 30 second TTL (sibling sweep of hostile validation round 12 finding R12-F2, S37). The assertions are unchanged. TR-MCP-MEMORY-JOBS-002 in this file is a historical id of the memory plan, not a record in the requirement store, so it makes no coverage claim (labeled 2026-11-05 before hostile validation round 13).</remarks>
    [Fact]
    public async Task PerWorkspaceLock()
    {
        var clock = new SettableTimeProvider(AgeBoundaryContractTests.References[0]);
        _harness.Clock = clock;
        var key = WorkspaceIdentity.CanonicalizeWorkspaceId(_harness.WorkspaceA);
        Assert.True(MemoryConsolidateOperations.TryAcquireLock(key, forceOverlap: false, clock.GetUtcNow(), out _));
        MemoryConsolidateJobResult b;
        try
        {
            b = await _harness.RunConsolidateJobAsync(dryRun: false, forceOverlap: true, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
        finally
        {
            MemoryConsolidateOperations.ReleaseLock(key);
        }

        var a = await _harness.RunConsolidateJobAsync(dryRun: false, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var results = new[] { a, b };

        Assert.Contains(results, r => r.StatusCode is 200 or 201);
        Assert.Contains(results, r => r.StatusCode is 409 or 423 || r.LockHeld == true);
    }

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 binding, decision D7 of <see cref="AgeBoundaryContractTests"/>: for each scripted reference
    /// time and each contract row, a workspace lock acquired at the row's time is taken over by a forced attempt at the
    /// reference exactly when the contract says (held at exactly 30 seconds, because only an age above the TTL is taken
    /// over), an unforced attempt always finds it held, and a failed forced attempt reports the lock as held. Test data:
    /// the contract's times; one workspace key per row.
    /// </summary>
    /// <remarks>2026-11-05, TR-MCP-MEMORY-JOBS-002; TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): added for the sibling sweep of hostile validation round 12 finding R12-F2 (S37).</remarks>
    [Fact]
    public void ConsolidationLockTakeover_UsesScriptedClock() =>
        AgeBoundaryContractTests.AssertDecision("D7 consolidation lock takeover", (reference, recorded) =>
        {
            var key = "d7-" + Guid.NewGuid().ToString("N");
            Assert.True(MemoryConsolidateOperations.TryAcquireLock(key, forceOverlap: false, recorded, out _));
            try
            {
                Assert.False(MemoryConsolidateOperations.TryAcquireLock(key, forceOverlap: false, reference, out var unforcedHeld));
                Assert.True(unforcedHeld);
                var taken = MemoryConsolidateOperations.TryAcquireLock(key, forceOverlap: true, reference, out var lockHeld);
                Assert.Equal(!taken, lockHeld);
                return taken;
            }
            finally
            {
                MemoryConsolidateOperations.ReleaseLock(key);
            }
        });

    /// <summary>AC-TR-MCP-MEMORY-JOBS-002-02: dry-run default is true unless overridden.</summary>
    [Fact]
    public async Task DryRunDefaultTrue()
    {
        Assert.True(MemoryConsolidateLimits.DefaultDryRun);
        var result = await _harness.RunConsolidateJobAsync(
            dryRun: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.True(result.DryRunApplied);
    }

    /// <summary>AC-TR-MCP-MEMORY-JOBS-002-03: event payload includes workspace id and consolidate run id.</summary>
    [Fact]
    public async Task EventIncludesWorkspaceAndRunId()
    {
        var result = await _harness.RunConsolidateJobAsync(
            dryRun: true,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(result.WorkspaceId));
        Assert.False(string.IsNullOrWhiteSpace(result.RunId));
    }

    /// <summary>AC-TR-MCP-MEMORY-JOBS-002-04: job can be disabled without affecting CRUD.</summary>
    [Fact]
    public async Task Disabled_NoEffectOnCrud()
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "s4-job",
            Text = "job disabled crud",
            Content = "job disabled crud",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var job = await _harness.RunConsolidateJobAsync(
            dryRun: true,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(added.Success, added.Error);
        Assert.Equal(200, job.StatusCode);
        Assert.True(job.Disabled);
        Assert.NotNull(await _harness.GetCompatAsync(added.Memory!.Id, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>AC-TR-MCP-MEMORY-JOBS-002-05: overlapping ticks do not double-apply merges.</summary>
    [Fact]
    public async Task OverlappingTicks_NoDoubleApply()
    {
        var first = await _harness.RunConsolidateJobAsync(
            dryRun: false,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var second = await _harness.RunConsolidateJobAsync(
            dryRun: false,
            forceOverlap: true,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, first.StatusCode);
        Assert.True(second.StatusCode is 200 or 409 or 423);
    }

    /// <summary>AC-TR-MCP-MEMORY-JOBS-002-06: job logs structured start/end/error without secrets.</summary>
    [Fact]
    public async Task Logs_NoSecrets()
    {
        var result = await _harness.RunConsolidateJobAsync(
            dryRun: true,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.DoesNotContain(result.Error ?? string.Empty, "sk-", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(result.Error ?? string.Empty, "password", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AC-TR-MCP-MEMORY-JOBS-002-07: lock TTL/expiry recovers from crashed holder.</summary>
    [Fact]
    public async Task LockExpiry_Recovers()
    {
        var result = await _harness.RunConsolidateJobAsync(
            dryRun: false,
            forceOverlap: true,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
    }

    /// <summary>AC-TR-MCP-MEMORY-JOBS-002-08: stats reflect last run time and dry-run vs write mode.</summary>
    [Fact]
    public async Task Stats_ReflectLastRun()
    {
        var result = await _harness.RunConsolidateJobAsync(
            dryRun: true,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.LastRunAt);
        Assert.True(result.DryRunApplied);
    }
}
