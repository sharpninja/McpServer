// Phase 7 gap closure: ClineV2 plugin v4 parity adoption (PLAN-AGENTPARITY-001).
// Replaces Phase 0 always-false stub with real assertions against the plugin repo.
// Detailed assertions in ClineV2_V4ParityTests.
// TR-MCP-AGENT-PARITY-020 (ClineV2 plugin adoption).
using System.IO;
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

/// <summary>TR-MCP-AGENT-PARITY-020: ClineV2 plugin phase 7 parity gap sentinel.</summary>
public class ClineV2_GapTests
{
    private static readonly string PluginRoot = ClineV2_V4ParityTests.ResolvePluginRoot();

    /// <summary>Sentinel: ENFORCEMENT.md must reference v4 - the primary Phase 7 gap from Phase 0 stub.</summary>
    [Fact]
    public void ClineV2_MustCloseV4ParityGaps_ForV4Compliance()
    {
        var possible = new[]
        {
            Path.Combine(PluginRoot, "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "lib", "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "docs", "ENFORCEMENT.md"),
        };
        var found = System.Array.Find(possible, File.Exists);
        Assert.True(found != null,
            "ENFORCEMENT.md missing from mcpserver-cline-v2-plugin root or lib/ - required for Phase 7 v4 parity (TR-MCP-AGENT-PARITY-020)");
        var content = File.ReadAllText(found!);
        Assert.Contains("v4", content, System.StringComparison.OrdinalIgnoreCase);
    }
}