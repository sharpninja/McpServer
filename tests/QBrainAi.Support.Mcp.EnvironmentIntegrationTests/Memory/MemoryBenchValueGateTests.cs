using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 (historical) / FR-MCP-MEMORY-018 (historical): H7a value-gate policy.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-MEMORY-018; TEST-MCP-MEMORY-018; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryBenchValueGateTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryBenchCatalog is linked into this assembly; only the namespace changed.</remarks>
public sealed class MemoryBenchValueGateTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-47: Other plugins stay blocked until H7a AGREE.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void GrokH7a_RequiresAgreeBeforeOtherPlugins()
    {
        var gate = MemoryBenchValueGate.Load(MemoryBenchCatalog.FindRepoRoot());
        Assert.Equal("H7a", gate.Gate);
        Assert.True(gate.Agree);
        Assert.Contains("hostile-validator-20260919T081530Z.md", gate.Receipt, StringComparison.Ordinal);
        Assert.Contains("hostile-validator-20260919T081530Z.md", gate.Note, StringComparison.Ordinal);
        Assert.True(gate.H7bBlockedUntilH7aAgree);
        Assert.Equal("grok", gate.DefaultPlugin);
        Assert.True(MemoryBenchValueGate.IsH7aAgreed(MemoryBenchCatalog.FindRepoRoot()));
        var resolved = MemoryBenchAdapterRegistry.Resolve(["claude-code"]);
        Assert.Equal("claude-code", resolved["claude-code"].PluginId);
        Assert.Equal("recorded-fixture", resolved["claude-code"].ValidationEntrypoint);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-50: H-done defaults to H7a; H7b is follow-on.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void HDone_DefaultRequiresH7a_H7bFollowOn()
    {
        var gate = MemoryBenchValueGate.Load(MemoryBenchCatalog.FindRepoRoot());
        Assert.Equal("H7a", gate.HDoneDefaultRequires);
        Assert.True(gate.H7bFollowOn);
        Assert.True(gate.H7bBlockedUntilH7aAgree);
        var plan = File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "docs", "plans", "mcp-memory-002.md"));
        Assert.Contains("H-done requires H7a", plan, StringComparison.Ordinal);
        Assert.Contains("H7b is a follow-on", plan, StringComparison.Ordinal);
    }
}
