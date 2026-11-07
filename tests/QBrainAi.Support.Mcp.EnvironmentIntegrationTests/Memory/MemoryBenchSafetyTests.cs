namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 (historical) / FR-MCP-MEMORY-018-12 (historical): Negative prompts must not hallucinate memory.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-MEMORY-018; TEST-MCP-MEMORY-018; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryBenchSafetyTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryBenchCatalog is linked into this assembly; only the namespace changed.</remarks>
public sealed class MemoryBenchSafetyTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-12: NEG-001 forbids invented favorites in both conditions.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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
