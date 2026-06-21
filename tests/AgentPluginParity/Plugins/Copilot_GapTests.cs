// Phase 8 gap closure: Copilot plugin v4 parity adoption (PLAN-AGENTPARITY-001).
// Replaces Phase 0 always-false stub with real assertions against the plugin repo.
// TR-MCP-AGENT-PARITY-020 (Copilot plugin adoption).
using System.IO;
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

/// <summary>TR-MCP-AGENT-PARITY-020: Copilot plugin phase 8 parity gap sentinel.</summary>
public class Copilot_GapTests
{
    private static readonly string PluginRoot = Copilot_V4ParityTests.ResolvePluginRoot();

    /// <summary>Sentinel: ENFORCEMENT.md must exist with v4 reference (Phase 8 primary gap).</summary>
    [Fact]
    public void Copilot_MustCloseV4ParityGaps_ForV4Compliance()
    {
        var possible = new[]
        {
            Path.Combine(PluginRoot, "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "lib", "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "docs", "ENFORCEMENT.md"),
        };
        var found = System.Array.Find(possible, File.Exists);
        Assert.True(found != null,
            "ENFORCEMENT.md missing - required for Phase 8 v4 parity (TR-MCP-AGENT-PARITY-020)");
        Assert.Contains("v4", File.ReadAllText(found!), System.StringComparison.OrdinalIgnoreCase);
    }
}
