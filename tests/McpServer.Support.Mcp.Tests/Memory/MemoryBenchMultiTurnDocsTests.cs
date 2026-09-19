namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-019: v2 is the real efficiency bench; v1 remains smoke/regression.
/// </summary>
public sealed class MemoryBenchMultiTurnDocsTests
{
    /// <summary>AC-FR-MCP-MEMORY-019: README documents the v1/v2 split and success-gated means.</summary>
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
