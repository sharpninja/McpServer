using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-011 / TR-MCP-MEMORY-SEARCH-002 / FR-MCP-MEMORY-011:
/// S2 Green acceptance for the dedicated memory indexer, ANN/FTS fusion,
/// and EmbeddingStatus transitions.
/// </summary>
public sealed class MemoryIndexerTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-01: Indexing a memory flips EmbeddingStatus to ready (or failed with reason).</summary>
    [Fact]
    public async Task ReadyOrFailedStatus()
    {
        var memory = await SeedAsync("index-ready-or-failed body").ConfigureAwait(true);
        Assert.Equal("pending", await _harness.GetEmbeddingStatusAsync(memory.Id).ConfigureAwait(true));

        var indexed = await _harness.IndexAsync(memory.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var status = indexed.EmbeddingStatus
            ?? await _harness.GetEmbeddingStatusAsync(memory.Id).ConfigureAwait(true);

        Assert.True(status is "ready" or "failed", indexed.Error ?? $"EmbeddingStatus was '{status}'.");
        if (status == "failed")
            Assert.False(string.IsNullOrWhiteSpace(indexed.FailureReason));
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-02: Startup/consolidate reconcile repairs stale EmbeddingStatus vs row hash.</summary>
    [Fact]
    public async Task Reconcile_RepairsStale()
    {
        var memory = await SeedAsync("stale-hash original").ConfigureAwait(true);
        await _harness.SetEmbeddingStatusAsync(memory.Id, "ready").ConfigureAwait(true);
        await _harness.UpdateCompatAsync(
            memory.Id,
            new MemoryUpdateRequest { Text = "stale-hash changed", Content = "stale-hash changed" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _harness.SetEmbeddingStatusAsync(memory.Id, "ready").ConfigureAwait(true);

        var reconciled = await _harness.ReconcileIndexAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var status = await _harness.GetEmbeddingStatusAsync(memory.Id).ConfigureAwait(true);

        Assert.True(reconciled.StatusCode is 200 or 201, reconciled.Error);
        Assert.True(
            status is "pending" or "failed" || (status == "ready" && reconciled.ReadyCount >= 1),
            $"Reconcile left EmbeddingStatus '{status}' without a counted repair.");
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-05: Failed embedding does not crash the host; memory remains listable via CRUD.</summary>
    [Fact]
    public async Task FailedEmbed_StillListable()
    {
        var memory = await SeedAsync("failed-embed still listable").ConfigureAwait(true);
        var indexed = await _harness.IndexAsync(
            memory.Id,
            provider: "cloud",
            cloudEnabled: false,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var listed = await _harness.ListCompatAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var got = await _harness.GetCompatAsync(memory.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.True(indexed.EmbeddingStatus == "failed" || indexed.StatusCode is 200 or 201 or 400, indexed.Error);
        Assert.Equal("failed", indexed.EmbeddingStatus ?? await _harness.GetEmbeddingStatusAsync(memory.Id).ConfigureAwait(true));
        Assert.Contains(listed.Items, item => item.Id == memory.Id);
        Assert.Equal(memory.Id, got?.Id);
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-06: Re-index after Content change updates vector/FTS; old vector is not exclusive.</summary>
    [Fact]
    public async Task Reindex_AfterContentChange()
    {
        var memory = await SeedAsync("obsolete-vector-token").ConfigureAwait(true);
        await _harness.IndexAsync(memory.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await _harness.UpdateCompatAsync(
            memory.Id,
            new MemoryUpdateRequest { Text = "fresh-vector-token", Content = "fresh-vector-token" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var reindexed = await _harness.IndexAsync(memory.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var obsolete = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = "obsolete-vector-token", MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var fresh = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = "fresh-vector-token", MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(reindexed.EmbeddingStatus is "ready" or "failed", reindexed.Error);
        Assert.Contains(fresh.Items ?? [], item => item.Id == memory.Id && item.Score.HasValue);
        Assert.DoesNotContain(obsolete.Items ?? [], item =>
            item.Id == memory.Id
            && string.Equals(item.MatchKind, "vector", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-07: HNSW/FTS side tables are workspace-safe (no cross-workspace ANN leakage).</summary>
    [Fact]
    public async Task Ann_NoCrossWorkspaceLeak()
    {
        var local = await SeedAsync("ann-local-token workspace A").ConfigureAwait(true);
        var foreign = await SeedAsync("ann-local-token workspace B", workspacePath: _harness.WorkspaceB)
            .ConfigureAwait(true);
        await _harness.IndexAsync(local.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await _harness.IndexAsync(foreign.Id, workspacePath: _harness.WorkspaceB, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = "ann-local-token", MinScore = 0.1 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        Assert.DoesNotContain(recall.Items ?? [], item => item.Id == foreign.Id);
        var hit = Assert.Single(recall.Items ?? [], item => item.Id == local.Id);
        Assert.True(hit.Score.HasValue);
        Assert.Equal(_harness.WorkspaceA, hit.AnnWorkspaceId);
        Assert.Equal("hybrid", recall.RankingMode, ignoreCase: true);
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-08: Indexer respects CancellationToken and leaves status failed/pending, not half-ready.</summary>
    [Fact]
    public async Task Cancel_LeavesSafeStatus()
    {
        var memory = await SeedAsync("cancel-index body").ConfigureAwait(true);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(true);

        var indexed = await _harness.IndexAsync(memory.Id, cancellationToken: cts.Token).ConfigureAwait(true);
        var status = indexed.EmbeddingStatus
            ?? await _harness.GetEmbeddingStatusAsync(memory.Id).ConfigureAwait(true);

        Assert.True(status is "pending" or "failed", $"Cancelled index left EmbeddingStatus '{status}'.");
        Assert.NotEqual("ready", status);
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-09: Local ONNX path works without cloud credentials in the test host.</summary>
    [Fact]
    public async Task OnnxLocal_NoCloudRequired()
    {
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", null);
        var memory = await SeedAsync("onnx local embedding body").ConfigureAwait(true);

        var indexed = await _harness.IndexAsync(
            memory.Id,
            provider: "onnx",
            cloudEnabled: false,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var status = indexed.EmbeddingStatus
            ?? await _harness.GetEmbeddingStatusAsync(memory.Id).ConfigureAwait(true);

        Assert.True(indexed.StatusCode is 200 or 201, indexed.Error);
        Assert.Equal("ready", status);
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-10: Cloud embedding path is opt-in and fails closed when disabled.</summary>
    [Fact]
    public async Task CloudOptIn_FailsClosedWhenOff()
    {
        var memory = await SeedAsync("cloud opt-in disabled").ConfigureAwait(true);

        var indexed = await _harness.IndexAsync(
            memory.Id,
            provider: "cloud",
            cloudEnabled: false,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var listed = await _harness.ListCompatAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var status = indexed.EmbeddingStatus
            ?? await _harness.GetEmbeddingStatusAsync(memory.Id).ConfigureAwait(true);

        Assert.Equal("failed", status);
        Assert.False(string.IsNullOrWhiteSpace(indexed.FailureReason));
        Assert.Contains(listed.Items, item => item.Id == memory.Id);
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-11: Empty Content cannot reach ready status (validation precedes index).</summary>
    [Fact]
    public async Task EmptyContent_NeverReady()
    {
        await using (var db = _harness.CreateContext(_harness.WorkspaceA))
        {
            db.Memories.Add(new McpServer.Support.Mcp.Storage.Entities.MemoryEntity
            {
                Id = "MEMORY-S2EMPTY-001",
                Category = "fact",
                Scope = McpServer.Support.Mcp.Storage.Entities.MemoryEntity.WorkspaceScope,
                WorkspaceId = _harness.WorkspaceA,
                Text = string.Empty,
                Content = string.Empty,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                EmbeddingStatus = "pending",
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        var indexed = await _harness.IndexAsync(
            "MEMORY-S2EMPTY-001",
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var status = indexed.EmbeddingStatus
            ?? await _harness.GetEmbeddingStatusAsync("MEMORY-S2EMPTY-001").ConfigureAwait(true);

        Assert.Equal(400, indexed.StatusCode);
        Assert.NotEqual("ready", status);
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-12: Batch index of N fixtures completes with all ready or explicit failures counted.</summary>
    [Fact]
    public async Task BatchIndex_AllTerminal()
    {
        var first = await SeedAsync("batch-index-one").ConfigureAwait(true);
        var second = await SeedAsync("batch-index-two").ConfigureAwait(true);

        var batched = await _harness.BatchIndexAsync(
            [first.Id, second.Id],
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var firstStatus = await _harness.GetEmbeddingStatusAsync(first.Id).ConfigureAwait(true);
        var secondStatus = await _harness.GetEmbeddingStatusAsync(second.Id).ConfigureAwait(true);

        Assert.True(batched.StatusCode is 200 or 201, batched.Error);
        Assert.Equal(2, batched.ReadyCount + batched.FailedCount);
        Assert.True(firstStatus is "ready" or "failed");
        Assert.True(secondStatus is "ready" or "failed");
    }

    private async Task<MemoryItem> SeedAsync(string content, string? workspacePath = null)
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
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
