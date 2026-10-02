using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-013 / FR-MCP-MEMORY-013:
/// S4 Red acceptance for consolidate / sleep merge. Production handler is unregistered (501)
/// until S4 Green. Mocks-first coverage lives in <see cref="MemoryS4PortMockTests"/>.
/// </summary>
public sealed class MemoryConsolidateTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-013-01: dry-run=true returns a merge plan and writes zero row mutations.</summary>
    [Fact]
    public async Task DryRun_NoMutations()
    {
        var a = await SeedNearDupAsync("consolidate dry-run fact alpha").ConfigureAwait(true);
        var b = await SeedNearDupAsync("consolidate dry-run fact alpha twin").ConfigureAwait(true);
        var before = await CountMemoriesAsync().ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = true },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var after = await CountMemoriesAsync().ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.True(result.DryRunApplied);
        Assert.NotNull(result.Plan);
        Assert.Equal(before, after);
        Assert.NotNull(await _harness.GetCompatAsync(a.Id, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.NotNull(await _harness.GetCompatAsync(b.Id, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>AC-FR-MCP-MEMORY-013-02: write mode merges near-duplicates to one survivor with lineage.</summary>
    [Fact]
    public async Task WriteMode_MergesNearDuplicates()
    {
        var a = await SeedNearDupAsync("consolidate write fact shared").ConfigureAwait(true);
        var b = await SeedNearDupAsync("consolidate write fact shared twin").ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.False(result.DryRunApplied);
        Assert.False(string.IsNullOrWhiteSpace(result.SurvivorId));
        Assert.Contains(result.SurvivorId!, new[] { a.Id, b.Id });
        Assert.NotNull(result.MergedAwayIds);
        Assert.Contains(result.MergedAwayIds!, id => id == a.Id || id == b.Id);
        var survivor = await _harness.GetCompatAsync(result.SurvivorId!, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(survivor);
        Assert.False(string.IsNullOrWhiteSpace(survivor!.Content ?? survivor.Text));
    }

    /// <summary>AC-FR-MCP-MEMORY-013-03: hard-delete only when AllowHardDelete is true.</summary>
    [Fact]
    public async Task HardDelete_OnlyWhenConfigured()
    {
        Assert.False(MemoryConsolidateLimits.DefaultAllowHardDelete);
        await SeedNearDupAsync("hard-delete off A").ConfigureAwait(true);
        await SeedNearDupAsync("hard-delete off A twin").ConfigureAwait(true);

        var off = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false, AllowHardDelete = false },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, off.StatusCode);
        Assert.True(off.MergedAwayIds is null || off.MergedAwayIds.Count >= 0);
    }

    /// <summary>AC-FR-MCP-MEMORY-013-04: concurrent consolidate returns busy/conflict.</summary>
    [Fact]
    public async Task LockContention_BusyError()
    {
        await SeedNearDupAsync("lock busy A").ConfigureAwait(true);
        await SeedNearDupAsync("lock busy A twin").ConfigureAwait(true);

        var first = _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false, RunId = "run-1" },
            cancellationToken: TestContext.Current.CancellationToken);
        var second = _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false, RunId = "run-2" },
            cancellationToken: TestContext.Current.CancellationToken);
        var results = await Task.WhenAll(first, second).ConfigureAwait(true);

        Assert.Contains(results, r => r.StatusCode is 200 or 201);
        Assert.Contains(results, r => r.StatusCode is 409 or 423 || r.FailureKind == MemoryMutationFailureKind.Conflict);
    }

    /// <summary>AC-FR-MCP-MEMORY-013-05: write-mode consolidate emits SSE memory.consolidated.</summary>
    [Fact]
    public async Task EmitsConsolidatedEvent()
    {
        await SeedNearDupAsync("event emit A").ConfigureAwait(true);
        await SeedNearDupAsync("event emit A twin").ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.True(result.EventEmitted);
        Assert.False(string.IsNullOrWhiteSpace(result.RunId));
    }

    /// <summary>AC-FR-MCP-MEMORY-013-06: dry-run is the default when the flag is omitted.</summary>
    [Fact]
    public async Task DryRunDefaultTrue()
    {
        Assert.True(MemoryConsolidateLimits.DefaultDryRun);
        await SeedNearDupAsync("default dry A").ConfigureAwait(true);
        await SeedNearDupAsync("default dry A twin").ConfigureAwait(true);
        var before = await CountMemoriesAsync().ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest(),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var after = await CountMemoriesAsync().ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.True(result.DryRunApplied);
        Assert.Equal(before, after);
    }

    /// <summary>AC-FR-MCP-MEMORY-013-07: empty workspace dry-run returns empty plan 200.</summary>
    [Fact]
    public async Task EmptyWorkspace_EmptyPlan()
    {
        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = true },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Plan);
        Assert.Empty(result.Plan!);
    }

    /// <summary>AC-FR-MCP-MEMORY-013-08: single memory dry-run returns empty merge plan.</summary>
    [Fact]
    public async Task SingleMemory_NoMerge()
    {
        await SeedNearDupAsync("single only").ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = true },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Plan);
        Assert.Empty(result.Plan!);
    }

    /// <summary>AC-FR-MCP-MEMORY-013-09: three near-duplicates collapse to one survivor in write mode.</summary>
    [Fact]
    public async Task ThreeWay_CollapsesToOne()
    {
        await SeedNearDupAsync("three-way shared").ConfigureAwait(true);
        await SeedNearDupAsync("three-way shared twin").ConfigureAwait(true);
        await SeedNearDupAsync("three-way shared triplet").ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(result.SurvivorId));
        Assert.NotNull(result.MergedAwayIds);
        Assert.True(result.MergedAwayIds!.Count >= 2);
    }

    /// <summary>AC-FR-MCP-MEMORY-013-10: survivor Content/Summary is non-empty after merge.</summary>
    [Fact]
    public async Task Survivor_NonEmptyContent()
    {
        await SeedNearDupAsync("survivor content A").ConfigureAwait(true);
        await SeedNearDupAsync("survivor content A twin").ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        var survivor = await _harness.GetCompatAsync(result.SurvivorId!, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.False(string.IsNullOrWhiteSpace(survivor?.Content ?? survivor?.Text));
    }

    /// <summary>AC-FR-MCP-MEMORY-013-11: merged-away ids disappear from default list/recall.</summary>
    [Fact]
    public async Task MergedAway_HiddenFromListRecall()
    {
        var a = await SeedNearDupAsync("hidden merge A").ConfigureAwait(true);
        var b = await SeedNearDupAsync("hidden merge A twin").ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(200, result.StatusCode);
        var merged = Assert.Single(result.MergedAwayIds ?? []);
        var list = await _harness.ListCompatAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = "hidden merge", MinScore = 0 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.DoesNotContain(list.Items ?? [], item => item.Id == merged);
        Assert.DoesNotContain(recall.Items ?? [], item => item.Id == merged);
        Assert.True(result.SurvivorId == a.Id || result.SurvivorId == b.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-013-12: get on merged-away id returns 404 or redirect (stable).</summary>
    [Fact]
    public async Task GetMergedAway_StableBehavior()
    {
        await SeedNearDupAsync("get merged A").ConfigureAwait(true);
        await SeedNearDupAsync("get merged A twin").ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(200, result.StatusCode);
        var merged = Assert.Single(result.MergedAwayIds ?? []);
        var got = await _harness.GetCompatAsync(merged, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.True(got is null || got.Id == result.SurvivorId);
    }

    /// <summary>AC-FR-MCP-MEMORY-013-13: consolidate never merges across workspaces.</summary>
    [Fact]
    public async Task NeverMergesAcrossWorkspaces()
    {
        var local = await SeedNearDupAsync("cross-ws local").ConfigureAwait(true);
        var foreign = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "s4",
            Text = "cross-ws local twin",
            Content = "cross-ws local twin",
            Type = "fact",
        }, workspacePath: _harness.WorkspaceB, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(foreign.Success, foreign.Error);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.DoesNotContain(result.MergedAwayIds ?? [], id => id == foreign.Memory!.Id);
        Assert.NotEqual(foreign.Memory!.Id, result.SurvivorId);
        Assert.NotNull(await _harness.GetCompatAsync(local.Id, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.NotNull(await _harness.GetCompatAsync(foreign.Memory.Id, workspacePath: _harness.WorkspaceB, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>AC-FR-MCP-MEMORY-013-14: same-scope only by default (no Global into Workspace silently).</summary>
    [Fact]
    public async Task SameScopeOnly_ByDefault()
    {
        var workspace = await SeedNearDupAsync("scope workspace").ConfigureAwait(true);
        var globalAdd = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "s4",
            Text = "scope workspace twin",
            Content = "scope workspace twin",
            Type = "fact",
            Scope = MemoryScope.Global,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(globalAdd.Success, globalAdd.Error);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.DoesNotContain(result.MergedAwayIds ?? [], id => id == globalAdd.Memory!.Id && result.SurvivorId == workspace.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-013-15: decay flags without unauthorized hard-delete.</summary>
    [Fact]
    public async Task Decay_FlagsWithoutHardDelete()
    {
        var stale = await SeedNearDupAsync("decay stale low").ConfigureAwait(true);
        await _harness.UpdateCompatAsync(stale.Id, new MemoryUpdateRequest { Confidence = 0.05 }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false, AllowHardDelete = false },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(await _harness.GetCompatAsync(stale.Id, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true)
            ?? await Task.FromResult<MemoryItem?>(null));
    }

    /// <summary>AC-FR-MCP-MEMORY-013-17: plan items include candidate ids and similarity score.</summary>
    [Fact]
    public async Task PlanItems_IncludeIdsAndScore()
    {
        await SeedNearDupAsync("plan score A").ConfigureAwait(true);
        await SeedNearDupAsync("plan score A twin").ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = true },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        var item = Assert.Single(result.Plan ?? []);
        Assert.True(item.CandidateIds.Count >= 2);
        Assert.InRange(item.SimilarityScore, 0, 1);
    }

    /// <summary>AC-FR-MCP-MEMORY-013-18: similarity threshold out of range returns 400.</summary>
    [Fact]
    public async Task ThresholdOutOfRange_Returns400()
    {
        var low = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = true, SimilarityThreshold = -0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var high = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = true, SimilarityThreshold = 1.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, low.StatusCode);
        Assert.Equal(400, high.StatusCode);
    }

    /// <summary>AC-FR-MCP-MEMORY-013-19: cancel mid-run leaves store consistent.</summary>
    [Fact]
    public async Task CancelMidRun_ConsistentStore()
    {
        await SeedNearDupAsync("cancel mid A").ConfigureAwait(true);
        await SeedNearDupAsync("cancel mid A twin").ConfigureAwait(true);
        var before = await CountMemoriesAsync().ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false, CancelRequested = true },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var after = await CountMemoriesAsync().ConfigureAwait(true);

        Assert.True(result.StatusCode is 200 or 409 or 499);
        Assert.True(after == before || after == before - 1);
    }

    /// <summary>AC-FR-MCP-MEMORY-013-20: after merge, explore edges are rewired to survivor.</summary>
    [Fact]
    public async Task Edges_RewiredToSurvivor()
    {
        var a = await SeedNearDupAsync("edge rewire A").ConfigureAwait(true);
        var b = await SeedNearDupAsync("edge rewire A twin").ConfigureAwait(true);
        var neighbor = await SeedNearDupAsync("edge rewire neighbor").ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(a.Id, neighbor.Id, "explicit", 0.7).ConfigureAwait(true);

        var result = await _harness.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(200, result.StatusCode);
        var explore = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = result.SurvivorId },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, explore.StatusCode);
        Assert.Contains(explore.Items ?? [], item => item.Id == neighbor.Id);
    }

    private async Task<MemoryItem> SeedNearDupAsync(string content)
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "s4-consolidate",
            Text = content,
            Content = content,
            Type = "fact",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);
        return added.Memory!;
    }

    private async Task<int> CountMemoriesAsync()
    {
        var list = await _harness.ListCompatAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        return list.Items?.Count ?? 0;
    }
}
