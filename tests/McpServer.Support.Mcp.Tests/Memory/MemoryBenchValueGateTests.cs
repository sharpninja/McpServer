using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018: H7a value-gate policy.
/// </summary>
public sealed class MemoryBenchValueGateTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-47: Other plugins stay blocked until H7a AGREE.</summary>
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
