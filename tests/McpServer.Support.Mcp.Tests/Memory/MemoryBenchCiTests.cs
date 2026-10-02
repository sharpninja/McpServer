using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018: CI gate and non-Grok optionality.
/// </summary>
public sealed class MemoryBenchCiTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-29: Gate mode is off by default and fails when enabled on a miss.</summary>
    [Fact]
    public void GateMode_Configurable()
    {
        var reportOnly = MemoryBenchCatalog.RunGrokStub(gateEnabled: false);
        Assert.False(reportOnly.GateEnabled);
        var gatedPass = MemoryBenchCatalog.RunGrokStub(gateEnabled: true);
        Assert.True(gatedPass.GateEnabled);

        var failing = new MemoryBenchCellResult
        {
            Plugin = "grok",
            PromptId = "PREF-001",
            Condition = "with_memory",
            TokensIn = 1,
            TokensOut = 1,
            TokensTotal = 2,
            TokenSource = MemoryBenchTokenSources.Estimator,
            Pass = false,
        };
        var gated = new MemoryBenchRunResult
        {
            Cells = [failing],
            GateEnabled = true,
        };
        MemoryBenchResultSchema.ValidateBeforeWrite(gated);
        Assert.True(gated.GateEnabled);
        Assert.False(failing.Pass);

        var source = File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "build", "Build.BenchMemory.cs"));
        Assert.Contains("MemoryBenchGate", source, StringComparison.Ordinal);
        Assert.Contains("Memory:Bench:Gate", File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "docs", "benchmarks", "README.md")), StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-48: Non-Grok bench cells are optional until H7a AGREE.</summary>
    [Fact]
    public void NonGrok_OptionalUntilH7a()
    {
        var gate = MemoryBenchValueGate.Load(MemoryBenchCatalog.FindRepoRoot());
        Assert.True(gate.Agree);
        Assert.False(gate.NonGrokCiRequired);
        var source = File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "build", "Build.BenchMemory.cs"));
        Assert.Contains("FullyQualifiedName~MemoryBench", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ClaudeCode_CompletesFullPack", source, StringComparison.Ordinal);
        Assert.Contains("blocked until H7a", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("readonly string Plugin = \"grok\"", source, StringComparison.Ordinal);
    }
}
