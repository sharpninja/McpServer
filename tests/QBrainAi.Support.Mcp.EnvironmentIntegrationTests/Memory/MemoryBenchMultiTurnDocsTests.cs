namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-019 (historical): v2 is the real efficiency bench; v1 remains smoke/regression.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-MEMORY-019; TEST-MCP-MEMORY-019; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryBenchMultiTurnDocsTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryBenchMultiTurnCatalog moved with it; only the namespace changed.</remarks>
public sealed class MemoryBenchMultiTurnDocsTests
{
    /// <summary>AC-FR-MCP-MEMORY-019: README documents the v1/v2 split and success-gated means.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Readme_DocumentsV2AsRealEfficiencyBench()
    {
        var readme = File.ReadAllText(Path.Combine(MemoryBenchMultiTurnCatalog.FindRepoRoot(), "docs", "benchmarks", "README.md"));
        Assert.Contains("memory-prompt-pack-v1.yaml", readme, StringComparison.Ordinal);
        Assert.Contains("smoke/regression", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("memory-prompt-pack-v2-multiturn.yaml", readme, StringComparison.Ordinal);
        Assert.Contains("real efficiency", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("successful jobs", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pass=false", readme, StringComparison.Ordinal);
        Assert.Contains("Do not claim an efficiency win from failed", readme, StringComparison.Ordinal);
    }
}
