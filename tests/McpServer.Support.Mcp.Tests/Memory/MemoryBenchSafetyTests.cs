namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018-12: Negative prompts must not hallucinate memory.
/// </summary>
public sealed class MemoryBenchSafetyTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-12: NEG-001 forbids invented favorites in both conditions.</summary>
    [Fact]
    public void NegativePrompts_NoHallucinatedMemory()
    {
        var cells = MemoryBenchCatalog.StubGrok().Cells.Where(cell => cell.PromptId == "NEG-001").ToList();
        Assert.Equal(2, cells.Count);
        foreach (var cell in cells)
        {
            Assert.True(cell.Pass);
            Assert.DoesNotContain("your favorite is", cell.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("you prefer rust", cell.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BENCH-PREF-LANG", cell.Answer, StringComparison.Ordinal);
        }
    }
}
