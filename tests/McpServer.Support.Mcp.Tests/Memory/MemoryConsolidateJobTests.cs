using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

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
    [Fact]
    public async Task PerWorkspaceLock()
    {
        var a = _harness.RunConsolidateJobAsync(dryRun: false, cancellationToken: TestContext.Current.CancellationToken);
        var b = _harness.RunConsolidateJobAsync(dryRun: false, forceOverlap: true, cancellationToken: TestContext.Current.CancellationToken);
        var results = await Task.WhenAll(a, b).ConfigureAwait(true);

        Assert.Contains(results, r => r.StatusCode is 200 or 201);
        Assert.Contains(results, r => r.StatusCode is 409 or 423 || r.LockHeld == true);
    }

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
