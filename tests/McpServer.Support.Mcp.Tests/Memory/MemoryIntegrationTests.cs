using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018-46 / FR-MCP-MEMORY-018-49:
/// Grok-lane integration using recorded fixtures (no live XAI key required).
/// </summary>
public sealed class MemoryIntegrationTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-018-46: Integration tests use the Grok lane as the pilot host.</summary>
    [Fact]
    public void UseGrokLane_AsPilot()
    {
        IMemoryBenchPluginAdapter adapter = new MemoryBenchGrokAdapter();
        Assert.Equal("grok", adapter.PluginId);
        var result = MemoryBenchCatalog.StubGrok();
        Assert.All(result.Cells, cell => Assert.Equal("grok", cell.Plugin));
        var readme = File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "docs", "benchmarks", "README.md"));
        Assert.Contains("Grok", readme, StringComparison.Ordinal);
        Assert.Contains("MemoryIntegrationTests", readme, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-49: Grok remember→recall/injection path records tokens as primary.</summary>
    [Fact]
    public async Task Grok_RememberRecall_EndToEnd()
    {
        var remembered = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Title = "Editor preference",
            Content = "BENCH-PREF-EDITOR=neovim",
            Type = "preference",
            Tags = ["bench", "pref"],
            Confidence = 0.95,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(remembered.StatusCode is 200 or 201, remembered.Error);

        var recall = await _harness.RecallAsync("BENCH-PREF-EDITOR=neovim", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Contains(recall.Items ?? [], item =>
            (item.Content ?? item.Text ?? string.Empty).Contains("neovim", StringComparison.OrdinalIgnoreCase));

        var pack = MemoryBenchCatalog.LoadPack();
        var prompt = pack.Prompts.Single(item => item.Id == "PREF-001");
        var adapter = new MemoryBenchGrokAdapter();
        var turn = adapter.Execute(new MemoryBenchTurnContext
        {
            Plugin = "grok",
            Prompt = prompt,
            Condition = "with_memory",
            Mode = MemoryBenchModes.Recorded,
            Injection = MemoryRequiredMemoriesRenderer.RenderRequiredMemories(
            [
                new MemoryItem
                {
                    Id = remembered.MemoryId ?? "MEMORY-BENCH-001",
                    Category = "preference",
                    Scope = MemoryScope.Workspace,
                    Text = "BENCH-PREF-EDITOR=neovim",
                    Version = 1,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch,
                    UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                    Content = "BENCH-PREF-EDITOR=neovim",
                },
            ]),
            EffectiveMemories = prompt.SeededMemories,
            ToolsAvailable = true,
        });

        Assert.Contains("neovim", turn.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REQUIRED MEMORIES", turn.Transcript, StringComparison.Ordinal);
        Assert.Contains("memory_recall", turn.ToolCalls);
        var (tokensIn, tokensOut, tokensTotal) = MemoryBenchTokenEstimator.EstimateTurn(
            prompt.UserText,
            turn.Transcript,
            turn.ToolPayloadText,
            turn.Answer);
        Assert.True(tokensIn > 0);
        Assert.True(tokensOut > 0);
        Assert.Equal(tokensIn + tokensOut, tokensTotal);
        Assert.Equal(MemoryBenchTokenSources.Recorded, turn.TokenSourceHint);
    }
}
