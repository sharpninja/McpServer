namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018-15: Recall tools are the with_memory path when injection hosts differ.
/// </summary>
public sealed class MemoryBenchToolUseTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-15: with_memory uses memory_recall; without_memory does not.</summary>
    [Fact]
    public void RecallUsedWhenNoInjection()
    {
        var result = MemoryBenchCatalog.StubGrok();
        Assert.All(result.Cells.Where(cell => cell.Condition == "with_memory"), cell =>
            Assert.Contains("memory_recall", cell.ToolCalls));
        Assert.All(result.Cells.Where(cell => cell.Condition == "without_memory"), cell =>
            Assert.DoesNotContain("memory_recall", cell.ToolCalls));
    }
}
