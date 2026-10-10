namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018-13: Conflict prompts prefer the newer seeded memory.
/// </summary>
public sealed class MemoryBenchConflictTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-13: with_memory prefers pnpm; without_memory does not assert it.</summary>
    [Fact]
    public void PrefersNewerSeeded()
    {
        var result = MemoryBenchCatalog.StubGrok();
        var withMemory = result.Cells.Single(cell => cell.PromptId == "CONF-001" && cell.Condition == "with_memory");
        var withoutMemory = result.Cells.Single(cell => cell.PromptId == "CONF-001" && cell.Condition == "without_memory");
        Assert.True(withMemory.Pass);
        Assert.Contains("pnpm", withMemory.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("standardize on npm", withMemory.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.True(withoutMemory.Pass);
        Assert.DoesNotContain("pnpm", withoutMemory.Answer, StringComparison.OrdinalIgnoreCase);
    }
}
