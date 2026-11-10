using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 (historical) / FR-MCP-MEMORY-018-28 (historical): Bench workspace isolation.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-MEMORY-018; TEST-MCP-MEMORY-018; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryBenchIsolationTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryBenchCatalog and MemoryS1Harness are linked into this assembly; only the namespace changed.</remarks>
public sealed class MemoryBenchIsolationTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-28: Pack run cannot read or mutate the primary workspace.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void NoPrimaryWorkspaceBleed()
    {
        var result = MemoryBenchCatalog.StubGrok();
        Assert.Equal("primary-operator-workspace", result.PrimaryWorkspaceId);
        Assert.NotEqual(result.PrimaryWorkspaceId, result.BenchWorkspaceId);
        Assert.False(result.PrimaryWorkspaceMutated);
        Assert.All(result.Cells, cell =>
            Assert.DoesNotContain(result.PrimaryWorkspaceId, cell.Transcript, StringComparison.Ordinal));

        using var harness = new MemoryS1Harness();
        var before = harness.WorkspaceA;
        _ = new MemoryBenchHarness().Run(MemoryBenchCatalog.FindRepoRoot(), new MemoryBenchRunOptions
        {
            Plugins = ["grok"],
            PrimaryWorkspaceId = before,
        });
        Assert.True(Directory.Exists(before) || !Directory.Exists(Path.Combine(before, "memories")));
    }
}
