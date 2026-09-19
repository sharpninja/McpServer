using McpServer.Support.Mcp.Services;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-011 / FR-MCP-MEMORY-011 / TR-MCP-MEMORY-SEARCH-002:
/// Mocks-first green proof that S2 recall/index port contracts are assertable.
/// Production S2 indexer/fusion is intentionally absent (Red tests live in the matrix classes).
/// </summary>
public sealed class MemoryS2PortMockTests
{
    /// <summary>AC-FR-MCP-MEMORY-011-01: mock recall rejects an empty query with 400.</summary>
    [Fact]
    public async Task PortMock_EmptyQuery_Returns400()
    {
        var port = Substitute.For<IMemoryS2Port>();
        port.RecallAsync(Arg.Any<MemoryRecallRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryRecallResult(400, FailureKind: MemoryMutationFailureKind.Validation));

        var result = await port.RecallAsync(
            new MemoryRecallRequest { Query = string.Empty },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, result.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-03: mock recall rejects a null query with 400.</summary>
    [Fact]
    public async Task PortMock_NullQuery_Returns400()
    {
        var port = Substitute.For<IMemoryS2Port>();
        port.RecallAsync(Arg.Any<MemoryRecallRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryRecallResult(400, FailureKind: MemoryMutationFailureKind.Validation));

        var result = await port.RecallAsync(
            new MemoryRecallRequest { Query = null },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, result.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-04 / AC-FR-MCP-MEMORY-011-05: mock recall returns scored exact and paraphrase hits.</summary>
    [Fact]
    public async Task PortMock_ExactAndParaphrase_ReturnScoredHits()
    {
        var port = Substitute.For<IMemoryS2Port>();
        port.RecallAsync(Arg.Any<MemoryRecallRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new MemoryRecallResult(200, [
                    new MemoryRecallHit { Id = "MEMORY-S2-001", Score = 0.91, Content = "BENCH-PREF-EDITOR=neovim" },
                ], RankingMode: "hybrid"),
                new MemoryRecallResult(200, [
                    new MemoryRecallHit { Id = "MEMORY-S2-001", Score = 0.74, Content = "BENCH-PREF-EDITOR=neovim", MatchKind = "vector" },
                ], RankingMode: "hybrid"));

        var exact = await port.RecallAsync(
            new MemoryRecallRequest { Query = "BENCH-PREF-EDITOR=neovim", MinScore = 0.2, TopN = 5 },
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        var paraphrase = await port.RecallAsync(
            new MemoryRecallRequest { Query = "which editor", MinScore = 0.2, TopN = 5 },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, exact.StatusCode);
        Assert.Equal("MEMORY-S2-001", exact.Items![0].Id);
        Assert.True(exact.Items[0].Score >= 0.2);
        Assert.Equal(200, paraphrase.StatusCode);
        Assert.True(paraphrase.Items![0].Score >= 0.2);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-15 / AC-FR-MCP-MEMORY-011-17: mock recall rejects out-of-range minScore and non-positive topN.</summary>
    [Fact]
    public async Task PortMock_MinScoreAndTopNBounds_Return400()
    {
        var port = Substitute.For<IMemoryS2Port>();
        port.RecallAsync(Arg.Any<MemoryRecallRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryRecallResult(400, FailureKind: MemoryMutationFailureKind.Validation));

        var minScore = await port.RecallAsync(
            new MemoryRecallRequest { Query = "q", MinScore = -1 },
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        var topN = await port.RecallAsync(
            new MemoryRecallRequest { Query = "q", TopN = 0 },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, minScore.StatusCode);
        Assert.Equal(400, topN.StatusCode);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-22: mock recall applies combined filters with AND semantics.</summary>
    [Fact]
    public async Task PortMock_CombinedFilters_AndSemantics()
    {
        var port = Substitute.For<IMemoryS2Port>();
        port.RecallAsync(Arg.Any<MemoryRecallRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryRecallResult(200, [
                new MemoryRecallHit
                {
                    Id = "MEMORY-S2-KEEP",
                    Score = 0.8,
                    Type = "fact",
                    Tags = ["alpha"],
                    Scope = MemoryScope.Workspace,
                },
            ]));

        var result = await port.RecallAsync(
            new MemoryRecallRequest
            {
                Query = "token",
                Tags = ["alpha"],
                Type = "fact",
                Scope = MemoryScope.Workspace,
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Single(result.Items!);
        Assert.Equal("MEMORY-S2-KEEP", result.Items![0].Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-10 / AC-FR-MCP-MEMORY-011-11: mock recall excludes soft-deleted and foreign hits.</summary>
    [Fact]
    public async Task PortMock_SoftDeletedAndForeign_Excluded()
    {
        var port = Substitute.For<IMemoryS2Port>();
        port.RecallAsync(Arg.Any<MemoryRecallRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryRecallResult(200, [
                new MemoryRecallHit { Id = "MEMORY-S2-LIVE", Score = 0.7, AnnWorkspaceId = "ws-a" },
            ]));

        var result = await port.RecallAsync(
            new MemoryRecallRequest { Query = "token" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.DoesNotContain(result.Items!, item => item.Id == "MEMORY-S2-DELETED");
        Assert.DoesNotContain(result.Items!, item => item.Id == "MEMORY-S2-FOREIGN");
        Assert.Contains(result.Items!, item => item.Id == "MEMORY-S2-LIVE" && item.Score.HasValue);
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-01: mock index flips EmbeddingStatus to ready.</summary>
    [Fact]
    public async Task PortMock_Index_ReadyOrFailedStatus()
    {
        var port = Substitute.For<IMemoryS2Port>();
        port.IndexAsync("MEMORY-S2-001", Arg.Any<CancellationToken>())
            .Returns(new MemoryIndexResult(200, "MEMORY-S2-001", "ready"));

        var result = await port.IndexAsync("MEMORY-S2-001", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("ready", result.EmbeddingStatus);
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-02: mock reconcile repairs stale status.</summary>
    [Fact]
    public async Task PortMock_Reconcile_RepairsStale()
    {
        var port = Substitute.For<IMemoryS2Port>();
        port.ReconcileAsync(Arg.Any<CancellationToken>())
            .Returns(new MemoryIndexResult(200, EmbeddingStatus: "pending", ReadyCount: 0, FailedCount: 0));

        var result = await port.ReconcileAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("pending", result.EmbeddingStatus);
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-12: mock batch index counts terminal outcomes.</summary>
    [Fact]
    public async Task PortMock_BatchIndex_AllTerminal()
    {
        var port = Substitute.For<IMemoryS2Port>();
        port.BatchIndexAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryIndexResult(200, ReadyCount: 2, FailedCount: 0));

        var result = await port.BatchIndexAsync(
            ["MEMORY-S2-001", "MEMORY-S2-002"],
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(2, result.ReadyCount + result.FailedCount);
    }
}
