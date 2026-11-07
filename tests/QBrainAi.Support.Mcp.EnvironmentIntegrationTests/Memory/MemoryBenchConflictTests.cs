namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 (historical) / FR-MCP-MEMORY-018-13 (historical): Conflict prompts prefer the newer seeded memory.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-MEMORY-018; TEST-MCP-MEMORY-018; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryBenchConflictTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryBenchCatalog is linked into this assembly; only the namespace changed.</remarks>
public sealed class MemoryBenchConflictTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-13: with_memory prefers pnpm; without_memory does not assert it.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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
