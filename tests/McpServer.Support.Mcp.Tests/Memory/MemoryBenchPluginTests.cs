using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018-16..23: Each plugin completes the full pack after H7a.
/// </summary>
public sealed class MemoryBenchPluginTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-16: Grok completes every prompt × both conditions with tokens_*.</summary>
    [Fact]
    public void Grok_CompletesFullPack_Required()
        => AssertPluginCompletesFullPack("grok", MemoryBenchCatalog.StubGrok());

    /// <summary>AC-FR-MCP-MEMORY-018-17: Claude Code completes the full pack after H7a AGREE.</summary>
    [Fact]
    public void ClaudeCode_CompletesFullPack_AfterGrokValueGate()
        => AssertPluginCompletesFullPack("claude-code", MemoryBenchCatalog.StubAll());

    /// <summary>AC-FR-MCP-MEMORY-018-18: Claude Cowork completes the full pack after H7a AGREE.</summary>
    [Fact]
    public void ClaudeCowork_CompletesFullPack_AfterGrokValueGate()
        => AssertPluginCompletesFullPack("claude-cowork", MemoryBenchCatalog.StubAll());

    /// <summary>AC-FR-MCP-MEMORY-018-19: Cline completes the full pack after H7a AGREE.</summary>
    [Fact]
    public void Cline_CompletesFullPack_AfterGrokValueGate()
        => AssertPluginCompletesFullPack("cline", MemoryBenchCatalog.StubAll());

    /// <summary>AC-FR-MCP-MEMORY-018-20: Cline v2 completes the full pack after H7a AGREE.</summary>
    [Fact]
    public void ClineV2_CompletesFullPack_AfterGrokValueGate()
        => AssertPluginCompletesFullPack("cline-v2", MemoryBenchCatalog.StubAll());

    /// <summary>AC-FR-MCP-MEMORY-018-21: Codex completes the full pack after H7a AGREE.</summary>
    [Fact]
    public void Codex_CompletesFullPack_AfterGrokValueGate()
        => AssertPluginCompletesFullPack("codex", MemoryBenchCatalog.StubAll());

    /// <summary>AC-FR-MCP-MEMORY-018-22: Copilot completes the full pack after H7a AGREE.</summary>
    [Fact]
    public void Copilot_CompletesFullPack_AfterGrokValueGate()
        => AssertPluginCompletesFullPack("copilot", MemoryBenchCatalog.StubAll());

    /// <summary>AC-FR-MCP-MEMORY-018-23: OpenCode completes the full pack after H7a AGREE.</summary>
    [Fact]
    public void Opencode_CompletesFullPack_AfterGrokValueGate()
        => AssertPluginCompletesFullPack("opencode", MemoryBenchCatalog.StubAll());

    /// <summary>AC-FR-MCP-MEMORY-018-09 / H7b: Stub -Plugin all writes per-cell tokens for all eight plugins.</summary>
    [Fact]
    public void AllEight_WritesStubArtifacts()
    {
        var directory = Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "docs", "benchmarks", "results");
        Directory.CreateDirectory(directory);
        var result = MemoryBenchCatalog.RunStub(["all"], directory, utcStamp: "20260919T091800Z");
        Assert.Equal(MemoryBenchAdapterRegistry.AllPluginIds, result.Plugins);
        Assert.Equal(128, result.Cells.Count);
        Assert.Equal(1.0, result.Cells.Average(cell => cell.Pass ? 1 : 0));
        Assert.True(File.Exists(Path.Combine(directory, "memory-bench-20260919T091800Z.json")));
        Assert.True(File.Exists(Path.Combine(directory, "memory-bench-20260919T091800Z.md")));
    }

    private static void AssertPluginCompletesFullPack(string plugin, MemoryBenchRunResult result)
    {
        Assert.True(MemoryBenchValueGate.IsH7aAgreed(MemoryBenchCatalog.FindRepoRoot()));
        Assert.Contains(plugin, result.Plugins);
        var pack = MemoryBenchCatalog.LoadPack();
        foreach (var prompt in pack.Prompts)
        {
            foreach (var condition in new[] { "without_memory", "with_memory" })
            {
                var cell = Assert.Single(result.Cells, item => item.Plugin == plugin && item.PromptId == prompt.Id && item.Condition == condition);
                Assert.True(cell.TokensIn >= 0);
                Assert.True(cell.TokensOut >= 0);
                Assert.Equal(cell.TokensIn + cell.TokensOut, cell.TokensTotal);
                Assert.False(string.IsNullOrWhiteSpace(cell.TokenSource));
            }
        }

        Assert.True(result.Cells.Where(cell => cell.Plugin == plugin && cell.Condition == "with_memory" && cell.PromptId is "PREF-001" or "DEC-001" or "FACT-001").All(cell => cell.Pass));
        Assert.True(result.Cells.Where(cell => cell.Plugin == plugin && cell.PromptId is "NEG-001" or "SAFE-001").All(cell => cell.Pass));
    }
}
