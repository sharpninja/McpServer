using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-011 / FR-MCP-MEMORY-011 / FR-MCP-MEMORY-014 / TR-MCP-MEMORY-SEARCH-002:
/// S1 AfterRevert stays green. S2 production recall matrix asserts hybrid search behavior.
/// </summary>
public sealed class MemoryRecallTests : IDisposable
{
    private const string ExactToken = "BENCH-PREF-EDITOR=neovim";
    private const string ParaphraseQuery = "which text editor does the operator like to use daily";
    private const string ExactContent = "The operator prefers neovim. BENCH-PREF-EDITOR=neovim";
    private const string SemanticContent = "Daily editing happens in neovim because it is the preferred operator tool.";

    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-014-14: After revert, recall/index refresh reflects reverted Content.</summary>
    [Fact]
    public async Task AfterRevert_IndexReflectsContent()
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "recall",
            Text = "original-recall-token",
            Content = "original-recall-token",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        await _harness.UpdateCompatAsync(
            added.Memory!.Id,
            new MemoryUpdateRequest { Text = "updated-recall-token", Content = "updated-recall-token" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var reverted = await _harness.RevertAsync(added.Memory.Id, 1, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.True(reverted.StatusCode is 200 or 201, reverted.Error);

        var recall = await _harness.RecallAsync("original-recall-token", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Contains(recall.Items ?? [], item =>
            item.Id == added.Memory.Id
            && (item.Content ?? item.Text ?? string.Empty).Contains("original-recall-token", StringComparison.Ordinal));
        Assert.DoesNotContain(recall.Items ?? [], item =>
            item.Id == added.Memory.Id
            && (item.Content ?? item.Text ?? string.Empty).Contains("updated-recall-token", StringComparison.Ordinal));
    }

    /// <summary>AC-FR-MCP-MEMORY-011-01: Empty query returns 400.</summary>
    [Fact]
    public async Task EmptyQuery_Returns400()
    {
        var recall = await _harness.RecallAsync(string.Empty, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, recall.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, recall.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-02: Whitespace-only query returns 400.</summary>
    [Fact]
    public async Task WhitespaceQuery_Returns400()
    {
        var recall = await _harness.RecallAsync("   \n\t  ", TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, recall.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, recall.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-03: Null query returns 400.</summary>
    [Fact]
    public async Task NullQuery_Returns400()
    {
        var recall = await _harness.RecallAsync((string?)null, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, recall.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, recall.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-04: Seeded exact token query returns the target in top-N with score &gt;= minScore.</summary>
    [Fact]
    public async Task ExactToken_ReturnsTargetInTopN()
    {
        var seeded = await SeedAsync(ExactContent, type: "preference", tags: ["editor"]).ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0.2, TopN = 5 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertScoredHitInTopN(recall, seeded.Id, minScore: 0.2, topN: 5);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-05: Seeded paraphrase/semantic query returns the target in top-N with score &gt;= minScore.</summary>
    [Fact]
    public async Task Paraphrase_ReturnsTargetInTopN()
    {
        var seeded = await SeedAsync(SemanticContent, type: "preference", tags: ["editor"]).ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ParaphraseQuery, MinScore = 0.2, TopN = 5 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertScoredHitInTopN(recall, seeded.Id, minScore: 0.2, topN: 5);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-06: Tag filter excludes non-matching tags.</summary>
    [Fact]
    public async Task TagFilter_ExcludesOthers()
    {
        var keep = await SeedAsync($"{ExactToken} keep-tag", type: "fact", tags: ["keep"]).ConfigureAwait(true);
        var drop = await SeedAsync($"{ExactToken} drop-tag", type: "fact", tags: ["drop"]).ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, Tags = ["keep"], MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        AssertScoredHitInTopN(recall, keep.Id, minScore: 0.1, topN: MemorySearchLimits.DefaultTopN);
        Assert.DoesNotContain(recall.Items ?? [], item => item.Id == drop.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-07: Type filter excludes non-matching types.</summary>
    [Fact]
    public async Task TypeFilter_ExcludesOthers()
    {
        var keep = await SeedAsync($"{ExactToken} type-fact", type: "fact").ConfigureAwait(true);
        var drop = await SeedAsync($"{ExactToken} type-pref", type: "preference").ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, Type = "fact", MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        AssertScoredHitInTopN(recall, keep.Id, minScore: 0.1, topN: MemorySearchLimits.DefaultTopN);
        Assert.DoesNotContain(recall.Items ?? [], item => item.Id == drop.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-08: Scope filter honors Global vs Workspace visibility.</summary>
    [Fact]
    public async Task ScopeFilter_HonorsVisibility()
    {
        var global = await SeedAsync($"{ExactToken} global-row", type: "fact", scope: MemoryScope.Global).ConfigureAwait(true);
        var local = await SeedAsync($"{ExactToken} workspace-row", type: "fact", scope: MemoryScope.Workspace).ConfigureAwait(true);

        var globalOnly = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, Scope = MemoryScope.Global, MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var workspaceOnly = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, Scope = MemoryScope.Workspace, MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertScoredHitInTopN(globalOnly, global.Id, minScore: 0.1, topN: MemorySearchLimits.DefaultTopN);
        Assert.DoesNotContain(globalOnly.Items ?? [], item => item.Id == local.Id);
        AssertScoredHitInTopN(workspaceOnly, local.Id, minScore: 0.1, topN: MemorySearchLimits.DefaultTopN);
        Assert.DoesNotContain(workspaceOnly.Items ?? [], item => item.Id == global.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-09: No matches returns 200 with empty items (not 404).</summary>
    [Fact]
    public async Task NoMatches_ReturnsEmpty200()
    {
        await SeedAsync(ExactContent).ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = "S2-NO-SUCH-TOKEN-9f3c2a1b", MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        Assert.NotNull(recall.Items);
        Assert.Empty(recall.Items);
        Assert.NotEqual(404, recall.StatusCode);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-10: Soft-deleted memories never appear in recall results.</summary>
    [Fact]
    public async Task SoftDeleted_NeverReturned()
    {
        var live = await SeedAsync($"{ExactToken} live-row").ConfigureAwait(true);
        var doomed = await SeedAsync($"{ExactToken} doomed-row").ConfigureAwait(true);
        await _harness.RemoveCompatAsync(doomed.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        Assert.DoesNotContain(recall.Items ?? [], item => item.Id == doomed.Id);
        AssertScoredHitInTopN(recall, live.Id, minScore: 0.1, topN: MemorySearchLimits.DefaultTopN);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-11: Other-workspace Workspace memories never appear in recall.</summary>
    [Fact]
    public async Task ForeignWorkspace_NeverReturned()
    {
        var local = await SeedAsync($"{ExactToken} local-a").ConfigureAwait(true);
        var foreign = await SeedAsync($"{ExactToken} foreign-b", workspacePath: _harness.WorkspaceB).ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        Assert.DoesNotContain(recall.Items ?? [], item => item.Id == foreign.Id);
        AssertScoredHitInTopN(recall, local.Id, minScore: 0.1, topN: MemorySearchLimits.DefaultTopN);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-12: Global memories can appear in recall for any workspace.</summary>
    [Fact]
    public async Task Global_VisibleToAllWorkspaces()
    {
        var global = await SeedAsync($"{ExactToken} global-shared", scope: MemoryScope.Global).ConfigureAwait(true);

        var fromA = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var fromB = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0.1 },
            workspacePath: _harness.WorkspaceB,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertScoredHitInTopN(fromA, global.Id, minScore: 0.1, topN: MemorySearchLimits.DefaultTopN);
        AssertScoredHitInTopN(fromB, global.Id, minScore: 0.1, topN: MemorySearchLimits.DefaultTopN);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-13: minScore=0 includes all scored hits up to topN.</summary>
    [Fact]
    public async Task MinScoreZero_IncludesAllHits()
    {
        var first = await SeedAsync($"{ExactToken} score-zero-a").ConfigureAwait(true);
        var second = await SeedAsync($"{ExactToken} score-zero-b").ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0, TopN = 10 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        Assert.NotNull(recall.Items);
        Assert.True(recall.Items.Count <= 10);
        Assert.Contains(recall.Items, item => item.Id == first.Id && item.Score is >= 0);
        Assert.Contains(recall.Items, item => item.Id == second.Id && item.Score is >= 0);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-14: minScore=1 returns only perfect-score hits (or empty).</summary>
    [Fact]
    public async Task MinScoreOne_OnlyPerfect()
    {
        await SeedAsync($"{ExactToken} imperfect").ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 1, TopN = 10 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        Assert.NotNull(recall.Items);
        Assert.All(recall.Items, item => Assert.Equal(1, item.Score));
    }

    /// <summary>AC-FR-MCP-MEMORY-011-15: minScore &lt; 0 or &gt; 1 returns 400.</summary>
    [Fact]
    public async Task MinScoreOutOfRange_Returns400()
    {
        var low = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = -0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var high = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 1.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, low.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, low.FailureKind);
        Assert.Equal(400, high.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, high.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-16: topN omitted uses the documented default.</summary>
    [Fact]
    public async Task TopNOmitted_UsesDefault()
    {
        for (var i = 0; i < MemorySearchLimits.DefaultTopN + 3; i++)
        {
            await SeedAsync($"{ExactToken} default-topn-{i:00}").ConfigureAwait(true);
        }

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        Assert.NotNull(recall.Items);
        Assert.Equal(MemorySearchLimits.DefaultTopN, recall.Items.Count);
        Assert.All(recall.Items, item => Assert.True(item.Score.HasValue));
    }

    /// <summary>AC-FR-MCP-MEMORY-011-17: topN=0 or negative returns 400.</summary>
    [Fact]
    public async Task TopNNonPositive_Returns400()
    {
        var zero = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, TopN = 0 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var negative = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, TopN = -3 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, zero.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, zero.FailureKind);
        Assert.Equal(400, negative.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, negative.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-18: topN above configured max is clamped or 400 (documented) and stable.</summary>
    [Fact]
    public async Task TopNAboveMax_StableBehavior()
    {
        for (var i = 0; i < MemorySearchLimits.MaxTopN + 5; i++)
        {
            await SeedAsync($"{ExactToken} above-max-{i:00}").ConfigureAwait(true);
        }

        var first = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, TopN = MemorySearchLimits.MaxTopN + 5, MinScore = 0 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var second = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, TopN = MemorySearchLimits.MaxTopN + 5, MinScore = 0 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(first.StatusCode is 200 or 400, first.Error);
        if (first.StatusCode == 400)
        {
            Assert.Equal(MemoryMutationFailureKind.Validation, first.FailureKind);
            Assert.Equal(400, second.StatusCode);
            return;
        }

        Assert.NotNull(first.Items);
        Assert.True(first.Items.Count <= MemorySearchLimits.MaxTopN);
        Assert.Equal(first.Items.Select(item => item.Id), second.Items?.Select(item => item.Id));
    }

    /// <summary>AC-FR-MCP-MEMORY-011-19: Results are ordered by descending score; ties broken by Id ascending.</summary>
    [Fact]
    public async Task OrderedByScoreThenId()
    {
        await SeedAsync($"{ExactToken} order-a", id: "MEMORY-S2ORD-002").ConfigureAwait(true);
        await SeedAsync($"{ExactToken} order-b", id: "MEMORY-S2ORD-001").ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0, TopN = 10 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        Assert.NotNull(recall.Items);
        Assert.True(recall.Items.Count >= 2);
        Assert.All(recall.Items, item => Assert.True(item.Score.HasValue));
        var ordered = recall.Items
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Select(item => item.Id);
        Assert.Equal(ordered, recall.Items.Select(item => item.Id));
    }

    /// <summary>AC-FR-MCP-MEMORY-011-20: Each hit includes id, score, and enough fields to open the memory.</summary>
    [Fact]
    public async Task HitIncludesIdAndScore()
    {
        var seeded = await SeedAsync(ExactContent, title: "Editor preference").ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0.1, TopN = 5 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var hit = AssertScoredHitInTopN(recall, seeded.Id, minScore: 0.1, topN: 5);
        Assert.False(string.IsNullOrWhiteSpace(hit.Id));
        Assert.True(hit.Score.HasValue);
        Assert.False(string.IsNullOrWhiteSpace(hit.Content ?? hit.Text));
    }

    /// <summary>AC-FR-MCP-MEMORY-011-21: Query longer than configured max returns 400.</summary>
    [Fact]
    public async Task QueryTooLong_Returns400()
    {
        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = new string('q', MemorySearchLimits.MaxQueryLength + 1) },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, recall.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, recall.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-22: Combined tag+type+scope filters AND together.</summary>
    [Fact]
    public async Task CombinedFilters_AndSemantics()
    {
        var keep = await SeedAsync(
            $"{ExactToken} and-keep",
            type: "fact",
            tags: ["alpha"],
            scope: MemoryScope.Workspace).ConfigureAwait(true);
        var wrongTag = await SeedAsync(
            $"{ExactToken} and-tag",
            type: "fact",
            tags: ["beta"],
            scope: MemoryScope.Workspace).ConfigureAwait(true);
        var wrongType = await SeedAsync(
            $"{ExactToken} and-type",
            type: "preference",
            tags: ["alpha"],
            scope: MemoryScope.Workspace).ConfigureAwait(true);
        var wrongScope = await SeedAsync(
            $"{ExactToken} and-scope",
            type: "fact",
            tags: ["alpha"],
            scope: MemoryScope.Global).ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest
            {
                Query = ExactToken,
                Tags = ["alpha"],
                Type = "fact",
                Scope = MemoryScope.Workspace,
                MinScore = 0.1,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertScoredHitInTopN(recall, keep.Id, minScore: 0.1, topN: MemorySearchLimits.DefaultTopN);
        Assert.DoesNotContain(recall.Items ?? [], item => item.Id == wrongTag.Id);
        Assert.DoesNotContain(recall.Items ?? [], item => item.Id == wrongType.Id);
        Assert.DoesNotContain(recall.Items ?? [], item => item.Id == wrongScope.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-23: Unindexed pending/failed memories are excluded from the vector path but may BM25-match.</summary>
    [Fact]
    public async Task Unindexed_DocumentedBehavior()
    {
        var pending = await SeedAsync($"{ExactToken} pending-embed").ConfigureAwait(true);
        await _harness.SetEmbeddingStatusAsync(pending.Id, "pending").ConfigureAwait(true);
        var failed = await SeedAsync($"{ExactToken} failed-embed").ConfigureAwait(true);
        await _harness.SetEmbeddingStatusAsync(failed.Id, "failed").ConfigureAwait(true);

        var exact = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var semantic = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ParaphraseQuery, MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, exact.StatusCode);
        foreach (var id in new[] { pending.Id, failed.Id })
        {
            var hit = exact.Items?.FirstOrDefault(item => item.Id == id);
            if (hit is not null)
            {
                Assert.Equal("bm25", hit.MatchKind, ignoreCase: true);
                Assert.True(hit.Score.HasValue);
            }
        }

        Assert.DoesNotContain(semantic.Items ?? [], item =>
            (item.Id == pending.Id || item.Id == failed.Id)
            && string.Equals(item.MatchKind, "vector", StringComparison.OrdinalIgnoreCase));
        Assert.False(string.IsNullOrWhiteSpace(exact.RankingMode));
    }

    /// <summary>AC-FR-MCP-MEMORY-011-24: After content update, recall reflects new content (index refresh).</summary>
    [Fact]
    public async Task AfterUpdate_IndexReflectsNewContent()
    {
        var seeded = await SeedAsync("old-s2-token-alpha").ConfigureAwait(true);
        await _harness.UpdateCompatAsync(
            seeded.Id,
            new MemoryUpdateRequest { Text = "new-s2-token-omega", Content = "new-s2-token-omega" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var indexed = await _harness.IndexAsync(seeded.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var oldHit = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = "old-s2-token-alpha", MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var newHit = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = "new-s2-token-omega", MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(indexed.EmbeddingStatus is "ready" or "failed", indexed.Error);
        AssertScoredHitInTopN(newHit, seeded.Id, minScore: 0.1, topN: MemorySearchLimits.DefaultTopN);
        Assert.DoesNotContain(oldHit.Items ?? [], item =>
            item.Id == seeded.Id
            && string.Equals(item.MatchKind, "vector", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>AC-FR-MCP-MEMORY-011-25: Recall does not return memories from sessionlog; only the Memory store.</summary>
    [Fact]
    public async Task DoesNotSearchSessionLog()
    {
        await using (var db = _harness.CreateContext(_harness.WorkspaceA))
        {
            db.SessionLogs.Add(new McpServer.Support.Mcp.Storage.Entities.SessionLogEntity
            {
                SourceType = "CursorGrok",
                SessionId = "CursorGrok-20260919T054000Z-s2",
                Model = "grok",
                Started = DateTimeOffset.UtcNow,
                LastUpdated = DateTimeOffset.UtcNow,
                WorkspaceId = _harness.WorkspaceA,
                Title = "S2-SESSIONLOG-ONLY-TOKEN transcript",
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = "S2-SESSIONLOG-ONLY-TOKEN", MinScore = 0 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        Assert.DoesNotContain(recall.Items ?? [], item =>
            (item.Content ?? item.Text ?? item.Title ?? string.Empty)
                .Contains("S2-SESSIONLOG-ONLY-TOKEN", StringComparison.Ordinal));
    }

    /// <summary>AC-FR-MCP-MEMORY-011-27: Unicode query tokens match unicode Content.</summary>
    [Fact]
    public async Task UnicodeQuery_MatchesContent()
    {
        var seeded = await SeedAsync("日本語ガイド supplementary 🧠").ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = "日本語 🧠", MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertScoredHitInTopN(recall, seeded.Id, minScore: 0.1, topN: MemorySearchLimits.DefaultTopN);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-28: Identical scores produce deterministic ordering across two consecutive recalls.</summary>
    [Fact]
    public async Task TieBreak_Deterministic()
    {
        await SeedAsync($"{ExactToken} tie-a", id: "MEMORY-S2TIE-002").ConfigureAwait(true);
        await SeedAsync($"{ExactToken} tie-b", id: "MEMORY-S2TIE-001").ConfigureAwait(true);

        var first = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0, TopN = 10 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var second = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0, TopN = 10 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, first.StatusCode);
        Assert.Equal(200, second.StatusCode);
        Assert.NotNull(first.Items);
        Assert.All(first.Items, item => Assert.True(item.Score.HasValue));
        Assert.Equal(first.Items.Select(item => item.Id), second.Items?.Select(item => item.Id));
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-03: Fusion weights from config change fixture ordering in a documented way.</summary>
    [Fact]
    public async Task FusionWeights_AffectOrdering()
    {
        var lexical = await SeedAsync($"{ExactToken} unique-lexical-anchor-zzzz").ConfigureAwait(true);
        var semantic = await SeedAsync(SemanticContent).ConfigureAwait(true);

        var bm25Heavy = await _harness.RecallAsync(
            new MemoryRecallRequest
            {
                Query = ExactToken,
                MinScore = 0,
                TopN = 5,
                FusionBm25Weight = 0.9,
                FusionVectorWeight = 0.1,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var vectorHeavy = await _harness.RecallAsync(
            new MemoryRecallRequest
            {
                Query = ParaphraseQuery,
                MinScore = 0,
                TopN = 5,
                FusionBm25Weight = 0.1,
                FusionVectorWeight = 0.9,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, bm25Heavy.StatusCode);
        Assert.Equal(200, vectorHeavy.StatusCode);
        Assert.Equal("hybrid", bm25Heavy.RankingMode, ignoreCase: true);
        Assert.Contains(bm25Heavy.Items ?? [], item => item.Id == lexical.Id && item.Score.HasValue);
        Assert.Contains(vectorHeavy.Items ?? [], item => item.Id == semantic.Id && item.Score.HasValue);
        Assert.NotEqual(
            bm25Heavy.Items?.Select(item => item.Id).ToArray(),
            vectorHeavy.Items?.Select(item => item.Id).ToArray());
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-04: Rerank remains off unless Mcp:Memory:Rerank:Enabled=true.</summary>
    [Fact]
    public async Task Rerank_DefaultOff()
    {
        await SeedAsync(ExactContent).ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = ExactToken, MinScore = 0.1, RerankEnabled = null },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        Assert.False(recall.RerankApplied);
        Assert.Equal("hybrid", recall.RankingMode, ignoreCase: true);
    }

    private async Task<MemoryItem> SeedAsync(
        string content,
        string type = "fact",
        IReadOnlyList<string>? tags = null,
        MemoryScope scope = MemoryScope.Workspace,
        string? workspacePath = null,
        string? id = null,
        string? title = null)
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Id = id,
            Category = type,
            Scope = scope,
            Text = content,
            Content = content,
            Title = title,
            Type = type,
            Tags = tags,
        }, workspacePath: workspacePath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);
        Assert.NotNull(added.Memory);
        return added.Memory!;
    }

    private static MemoryRecallHit AssertScoredHitInTopN(MemoryRecallResult recall, string id, double minScore, int topN)
    {
        Assert.Equal(200, recall.StatusCode);
        Assert.NotNull(recall.Items);
        Assert.True(recall.Items.Count <= topN, $"Expected at most {topN} hits, found {recall.Items.Count}.");
        var hit = Assert.Single(recall.Items, item => item.Id == id);
        Assert.True(hit.Score.HasValue, "Hybrid recall hits must include a score.");
        Assert.True(hit.Score >= minScore, $"Score {hit.Score} was below minScore {minScore}.");
        return hit;
    }
}
