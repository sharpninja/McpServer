namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018-16: Grok must complete the full pack.
/// </summary>
public sealed class MemoryBenchPluginTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-16: Grok completes every prompt × both conditions with tokens_*.</summary>
    [Fact]
    public void Grok_CompletesFullPack_Required()
    {
        var pack = MemoryBenchCatalog.LoadPack();
        var result = MemoryBenchCatalog.StubGrok();
        Assert.Equal("grok", result.Plugins.Single());
        foreach (var prompt in pack.Prompts)
        {
            foreach (var condition in new[] { "without_memory", "with_memory" })
            {
                var cell = Assert.Single(result.Cells, item => item.Plugin == "grok" && item.PromptId == prompt.Id && item.Condition == condition);
                Assert.True(cell.TokensIn >= 0);
                Assert.True(cell.TokensOut >= 0);
                Assert.Equal(cell.TokensIn + cell.TokensOut, cell.TokensTotal);
                Assert.False(string.IsNullOrWhiteSpace(cell.TokenSource));
            }
        }

        Assert.True(result.Cells.Where(cell => cell.Condition == "with_memory" && cell.PromptId is "PREF-001" or "DEC-001" or "FACT-001").All(cell => cell.Pass));
        Assert.True(result.Cells.Where(cell => cell.PromptId is "NEG-001" or "SAFE-001").All(cell => cell.Pass));
    }
}
