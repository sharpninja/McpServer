using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-012 / FR-MCP-MEMORY-012:
/// S3 Green acceptance for <c>memory_explore</c> and Hebbian visibility.
/// </summary>
public sealed class MemoryExploreTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-012-01: Given explicit edge A to B, explore(A) includes B with weight and EdgeType.</summary>
    [Fact]
    public async Task ExplicitEdge_ReturnedWithWeight()
    {
        var seed = await SeedAsync("explore seed A").ConfigureAwait(true);
        var neighbor = await SeedAsync("explore neighbor B").ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(seed.Id, neighbor.Id, "explicit", 0.8)
            .ConfigureAwait(true);

        var explore = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id, Depth = 1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, explore.StatusCode);
        var hit = Assert.Single(explore.Items ?? [], item => item.Id == neighbor.Id);
        Assert.Equal(0.8, hit.Weight);
        Assert.Equal("explicit", hit.EdgeType, ignoreCase: true);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-02: With Hebbian Enabled=false (default), explore never returns co-retrieved-only edges.</summary>
    [Fact]
    public async Task HebbianOff_NoCoRetrievedOnlyEdges()
    {
        Assert.False(MemoryExploreLimits.DefaultHebbianEnabled);

        var seed = await SeedAsync("hebbian-off seed").ConfigureAwait(true);
        var explicitNeighbor = await SeedAsync("hebbian-off explicit").ConfigureAwait(true);
        var coRetrievedOnly = await SeedAsync("hebbian-off co-retrieved").ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(seed.Id, explicitNeighbor.Id, "explicit", 0.9)
            .ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(seed.Id, coRetrievedOnly.Id, "co-retrieved", 0.7)
            .ConfigureAwait(true);

        var explore = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id, HebbianEnabled = null },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, explore.StatusCode);
        Assert.False(explore.HebbianApplied);
        Assert.Contains(explore.Items ?? [], item => item.Id == explicitNeighbor.Id && item.EdgeType.Equals("explicit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(explore.Items ?? [], item => item.Id == coRetrievedOnly.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-03: With Hebbian enabled, co-retrieval strengthens co-retrieved edges without changing recall ranking.</summary>
    [Fact]
    public async Task HebbianOn_StrengthensCoRetrieved()
    {
        var first = await SeedAsync("hebbian-on shared token alpha").ConfigureAwait(true);
        var second = await SeedAsync("hebbian-on shared token beta").ConfigureAwait(true);

        var before = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = "hebbian-on shared token", MinScore = 0, TopN = 10 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var rankingBefore = (before.Items ?? []).Select(item => item.Id).ToArray();

        var hebbian = await _harness.RecordHebbianAsync(
            [first.Id, second.Id],
            hebbianEnabled: true,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var explore = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = first.Id, HebbianEnabled = true },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var after = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = "hebbian-on shared token", MinScore = 0, TopN = 10 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(hebbian.StatusCode is 200 or 201, hebbian.Error);
        Assert.True(hebbian.RankingUnchanged);
        Assert.Equal(200, explore.StatusCode);
        Assert.True(explore.HebbianApplied);
        Assert.Contains(explore.Items ?? [], item =>
            item.Id == second.Id
            && item.EdgeType.Equals("co-retrieved", StringComparison.OrdinalIgnoreCase)
            && item.Weight > 0);
        Assert.Equal(rankingBefore, (after.Items ?? []).Select(item => item.Id).ToArray());
    }

    /// <summary>AC-FR-MCP-MEMORY-012-04: Unknown seed memory id returns 404.</summary>
    [Fact]
    public async Task UnknownSeed_Returns404()
    {
        var explore = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = "MEMORY-NO-SUCH-SEED" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(404, explore.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.NotFound, explore.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-05: Explore results are confined to the caller workspace Effective set.</summary>
    [Fact]
    public async Task NoCrossWorkspaceLeakage()
    {
        var local = await SeedAsync("explore local seed").ConfigureAwait(true);
        var localNeighbor = await SeedAsync("explore local neighbor").ConfigureAwait(true);
        var foreign = await SeedAsync("explore foreign neighbor", workspacePath: _harness.WorkspaceB)
            .ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(local.Id, localNeighbor.Id, "explicit", 0.6)
            .ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(local.Id, foreign.Id, "explicit", 0.9)
            .ConfigureAwait(true);

        var explore = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = local.Id },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, explore.StatusCode);
        Assert.Contains(explore.Items ?? [], item => item.Id == localNeighbor.Id);
        Assert.DoesNotContain(explore.Items ?? [], item => item.Id == foreign.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-06: Soft-deleted seed returns 404.</summary>
    [Fact]
    public async Task SoftDeletedSeed_Returns404()
    {
        var seed = await SeedAsync("soft-deleted seed").ConfigureAwait(true);
        var neighbor = await SeedAsync("soft-deleted seed neighbor").ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(seed.Id, neighbor.Id, "explicit", 1)
            .ConfigureAwait(true);
        await _harness.RemoveCompatAsync(seed.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var explore = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(404, explore.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.NotFound, explore.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-07: Soft-deleted neighbor is omitted from explore results.</summary>
    [Fact]
    public async Task SoftDeletedNeighbor_Omitted()
    {
        var seed = await SeedAsync("live seed for deleted neighbor").ConfigureAwait(true);
        var live = await SeedAsync("live neighbor").ConfigureAwait(true);
        var doomed = await SeedAsync("doomed neighbor").ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(seed.Id, live.Id, "explicit", 0.5)
            .ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(seed.Id, doomed.Id, "explicit", 0.9)
            .ConfigureAwait(true);
        await _harness.RemoveCompatAsync(doomed.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var explore = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, explore.StatusCode);
        Assert.Contains(explore.Items ?? [], item => item.Id == live.Id);
        Assert.DoesNotContain(explore.Items ?? [], item => item.Id == doomed.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-08: depth=1 returns only direct neighbors; depth&gt;1 returns transitive hops.</summary>
    [Fact]
    public async Task Depth_ControlsHops()
    {
        var seed = await SeedAsync("depth seed").ConfigureAwait(true);
        var hop1 = await SeedAsync("depth hop-1").ConfigureAwait(true);
        var hop2 = await SeedAsync("depth hop-2").ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(seed.Id, hop1.Id, "explicit", 1)
            .ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(hop1.Id, hop2.Id, "explicit", 1)
            .ConfigureAwait(true);

        var depth1 = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id, Depth = 1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var depth2 = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id, Depth = 2 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, depth1.StatusCode);
        Assert.Contains(depth1.Items ?? [], item => item.Id == hop1.Id && item.Depth == 1);
        Assert.DoesNotContain(depth1.Items ?? [], item => item.Id == hop2.Id);
        Assert.Equal(200, depth2.StatusCode);
        Assert.Contains(depth2.Items ?? [], item => item.Id == hop1.Id);
        Assert.Contains(depth2.Items ?? [], item => item.Id == hop2.Id && item.Depth == 2);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-09: depth=0 or negative returns 400.</summary>
    [Fact]
    public async Task DepthNonPositive_Returns400()
    {
        var seed = await SeedAsync("depth bounds seed").ConfigureAwait(true);

        var zero = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id, Depth = 0 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var negative = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id, Depth = -1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, zero.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, zero.FailureKind);
        Assert.Equal(400, negative.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, negative.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-10: depth above max returns 400 (documented).</summary>
    [Fact]
    public async Task DepthAboveMax_StableBehavior()
    {
        var seed = await SeedAsync("depth max seed").ConfigureAwait(true);

        var explore = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id, Depth = MemoryExploreLimits.MaxDepth + 1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(MemoryExploreLimits.DepthAboveMaxStatusCode, explore.StatusCode);
        Assert.Equal(400, explore.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, explore.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-11: Self-loop edges are rejected on create (400) or never returned.</summary>
    [Fact]
    public async Task SelfLoop_RejectedOrAbsent()
    {
        var seed = await SeedAsync("self-loop seed").ConfigureAwait(true);

        var created = await _harness.CreateEdgeAsync(
            new MemoryCreateEdgeRequest
            {
                FromMemoryId = seed.Id,
                ToMemoryId = seed.Id,
                EdgeType = "explicit",
                Weight = 1,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var explore = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(
            created.StatusCode == MemoryExploreLimits.SelfLoopStatusCode
            || (explore.StatusCode == 200 && (explore.Items ?? []).All(item => item.Id != seed.Id)),
            created.Error ?? "Self-loop must be rejected on create or omitted from explore.");
        if (created.StatusCode == MemoryExploreLimits.SelfLoopStatusCode)
            Assert.Equal(MemoryMutationFailureKind.Validation, created.FailureKind);
        if (explore.StatusCode == 200)
            Assert.DoesNotContain(explore.Items ?? [], item => item.Id == seed.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-13: Explore with query seed (no id) returns the neighborhood around the top recall hit or empty.</summary>
    [Fact]
    public async Task QuerySeed_UsesTopRecallOrEmpty()
    {
        var top = await SeedAsync("query-seed unique-token-alpha").ConfigureAwait(true);
        var neighbor = await SeedAsync("query-seed neighbor").ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(top.Id, neighbor.Id, "explicit", 0.4)
            .ConfigureAwait(true);

        var withHits = await _harness.ExploreAsync(
            new MemoryExploreRequest { Query = "query-seed unique-token-alpha" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var empty = await _harness.ExploreAsync(
            new MemoryExploreRequest { Query = "S3-NO-SUCH-QUERY-TOKEN" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, withHits.StatusCode);
        Assert.Equal(top.Id, withHits.SeedId);
        Assert.Contains(withHits.Items ?? [], item => item.Id == neighbor.Id);
        Assert.Equal(200, empty.StatusCode);
        Assert.NotNull(empty.Items);
        Assert.Empty(empty.Items);
        Assert.NotEqual(404, empty.StatusCode);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-16: While Hebbian is off, explore ignores previously created co-retrieved edges unless explicitly kept.</summary>
    [Fact]
    public async Task HebbianToggle_DocumentedVisibility()
    {
        var seed = await SeedAsync("hebbian-toggle seed").ConfigureAwait(true);
        var neighbor = await SeedAsync("hebbian-toggle neighbor").ConfigureAwait(true);

        var on = await _harness.RecordHebbianAsync(
            [seed.Id, neighbor.Id],
            hebbianEnabled: true,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var whileOn = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id, HebbianEnabled = true },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var whileOff = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id, HebbianEnabled = false },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(on.StatusCode is 200 or 201, on.Error);
        Assert.Equal(200, whileOn.StatusCode);
        Assert.Contains(whileOn.Items ?? [], item =>
            item.Id == neighbor.Id
            && item.EdgeType.Equals("co-retrieved", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(200, whileOff.StatusCode);
        Assert.False(whileOff.HebbianApplied);
        Assert.DoesNotContain(whileOff.Items ?? [], item =>
            item.Id == neighbor.Id
            && item.EdgeType.Equals("co-retrieved", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>AC-FR-MCP-MEMORY-012-17: Explore does not mutate memories' Content.</summary>
    [Fact]
    public async Task DoesNotMutateContent()
    {
        const string original = "explore must not rewrite this content";
        var seed = await SeedAsync(original).ConfigureAwait(true);
        var neighbor = await SeedAsync("explore neighbor content").ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(seed.Id, neighbor.Id, "explicit", 1)
            .ConfigureAwait(true);

        var explore = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var after = await _harness.GetCompatAsync(seed.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(200, explore.StatusCode);
        Assert.Equal(original, after?.Content ?? after?.Text);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-18: A to B does not imply B to A unless a reverse edge exists.</summary>
    [Fact]
    public async Task Directed_NoImpliedReverse()
    {
        var from = await SeedAsync("directed from").ConfigureAwait(true);
        var to = await SeedAsync("directed to").ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(from.Id, to.Id, "explicit", 0.75)
            .ConfigureAwait(true);

        var forward = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = from.Id },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var reverse = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = to.Id },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, forward.StatusCode);
        Assert.Contains(forward.Items ?? [], item => item.Id == to.Id);
        Assert.Equal(200, reverse.StatusCode);
        Assert.DoesNotContain(reverse.Items ?? [], item => item.Id == from.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-19: maxNeighbors truncates; stable order is weight desc then id.</summary>
    [Fact]
    public async Task MaxNeighbors_TruncatesStable()
    {
        var seed = await SeedAsync("max-neighbors seed").ConfigureAwait(true);
        var heavy = await SeedAsync("max-neighbors heavy", id: "MEMORY-S3NBR-003").ConfigureAwait(true);
        var midA = await SeedAsync("max-neighbors mid-a", id: "MEMORY-S3NBR-001").ConfigureAwait(true);
        var midB = await SeedAsync("max-neighbors mid-b", id: "MEMORY-S3NBR-002").ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(seed.Id, heavy.Id, "explicit", 0.9)
            .ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(seed.Id, midA.Id, "explicit", 0.5)
            .ConfigureAwait(true);
        await _harness.InsertEdgeFixtureAsync(seed.Id, midB.Id, "explicit", 0.5)
            .ConfigureAwait(true);

        var first = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id, MaxNeighbors = 2 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var second = await _harness.ExploreAsync(
            new MemoryExploreRequest { SeedId = seed.Id, MaxNeighbors = 2 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, first.StatusCode);
        Assert.NotNull(first.Items);
        Assert.Equal(2, first.Items.Count);
        Assert.Equal(heavy.Id, first.Items[0].Id);
        Assert.True(first.Items[0].Weight >= first.Items[1].Weight);
        if (first.Items[0].Weight == first.Items[1].Weight)
            Assert.True(string.CompareOrdinal(first.Items[0].Id, first.Items[1].Id) < 0);
        Assert.Equal(first.Items.Select(item => item.Id), (second.Items ?? []).Select(item => item.Id));
    }

    private async Task<MemoryItem> SeedAsync(
        string content,
        string? workspacePath = null,
        string? id = null)
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Id = id,
            Category = "fact",
            Scope = MemoryScope.Workspace,
            Text = content,
            Content = content,
            Type = "fact",
        }, workspacePath: workspacePath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);
        Assert.NotNull(added.Memory);
        return added.Memory!;
    }
}
