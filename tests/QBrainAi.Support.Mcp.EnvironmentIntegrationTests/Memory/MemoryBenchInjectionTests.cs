namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 (historical) / FR-MCP-MEMORY-018-14 (historical): REQUIRED MEMORIES appears only with_memory.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-MEMORY-018; TEST-MCP-MEMORY-018; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryBenchInjectionTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryBenchCatalog is linked into this assembly; only the namespace changed.</remarks>
public sealed class MemoryBenchInjectionTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-14: Injection header is present only when memory is on.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void InjectionPresentOnlyWithMemory()
    {
        var result = MemoryBenchCatalog.StubGrok();
        Assert.All(result.Cells.Where(cell => cell.Condition == "with_memory"), cell =>
        {
            Assert.True(cell.InjectionPresent);
            Assert.Contains("REQUIRED MEMORIES", cell.Transcript, StringComparison.Ordinal);
        });
        Assert.All(result.Cells.Where(cell => cell.Condition == "without_memory"), cell =>
        {
            Assert.False(cell.InjectionPresent);
            Assert.DoesNotContain("BENCH-PREF-EDITOR=neovim", cell.Transcript, StringComparison.Ordinal);
        });
    }
}
