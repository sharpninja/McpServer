using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 (historical) / TR-MCP-MEMORY-BENCH-002 (historical): Harness technology contracts.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-MEMORY-018; TR-MCP-MEMORY-BENCH-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 11 Category=Integration cases moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</remarks>
public sealed class MemoryBenchTechTests
{
    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-10: Missing token fields hard-fail finalize.</summary>
    [Fact]
    public void MissingTokens_HardFail()
    {
        var incomplete = new MemoryBenchRunResult
        {
            Cells =
            [
                new MemoryBenchCellResult
                {
                    Plugin = "grok",
                    PromptId = "PREF-001",
                    Condition = "with_memory",
                    Transcript = "missing tokens",
                },
            ],
        };
        var ex = Assert.Throws<MemoryBenchTokenSchemaException>(() => MemoryBenchHarness.Finalize(incomplete));
        Assert.Contains("lacks required token fields", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-11: Estimator is pure and versioned.</summary>
    [Fact]
    public void Estimator_VersionedDeterministic()
    {
        Assert.Equal("memory-bench-whitespace", MemoryBenchTokenEstimator.EstimatorId);
        Assert.Equal("1.0.0", MemoryBenchTokenEstimator.EstimatorVersion);
        Assert.Equal(MemoryBenchTokenEstimator.Count("one two"), MemoryBenchTokenEstimator.Count("one two"));
        Assert.Equal(2, MemoryBenchTokenEstimator.Count("one two"));
    }
}
