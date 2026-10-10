namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018: Benchmark docs contracts.
/// </summary>
public sealed class MemoryBenchDocsTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-32: README documents one-plugin and all-eight runs.</summary>
    [Fact]
    public void Readme_RunInstructions()
    {
        var readme = Readme();
        Assert.Contains("./build.ps1 BenchMemory", readme, StringComparison.Ordinal);
        Assert.Contains("-Plugin grok", readme, StringComparison.Ordinal);
        Assert.Contains("-Plugin all", readme, StringComparison.Ordinal);
        Assert.Contains("with_memory", readme, StringComparison.Ordinal);
        Assert.Contains("without_memory", readme, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-33: USER-GUIDE or MCP-SERVER links the pack.</summary>
    [Fact]
    public void UserGuide_LinksPack()
    {
        var root = MemoryBenchCatalog.FindRepoRoot();
        var docs = File.ReadAllText(Path.Combine(root, "docs", "USER-GUIDE.md"))
            + File.ReadAllText(Path.Combine(root, "docs", "MCP-SERVER.md"));
        Assert.Contains("docs/benchmarks/memory-prompt-pack-v1.yaml", docs, StringComparison.Ordinal);
        Assert.Contains("docs/benchmarks/README.md", docs, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-45: README states tokens are primary; pass/fail is correctness/safety.</summary>
    [Fact]
    public void Readme_TokensArePrimaryMetric()
    {
        var readme = Readme();
        Assert.Contains("Primary metric: tokens used", readme, StringComparison.Ordinal);
        Assert.Contains("correctness/safety", readme, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-14: README documents Grok as the pilot validation path.</summary>
    [Fact]
    public void Readme_GrokIsPilot()
    {
        var readme = Readme();
        Assert.Contains("Grok", readme, StringComparison.Ordinal);
        Assert.Contains("MemoryBenchGrokAdapter", readme, StringComparison.Ordinal);
        Assert.Contains("MemoryIntegrationTests", readme, StringComparison.Ordinal);
        Assert.Contains("pilot", readme, StringComparison.OrdinalIgnoreCase);
    }

    private static string Readme()
        => File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "docs", "benchmarks", "README.md"));
}
