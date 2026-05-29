// Phase 6 gap closure: Cline plugin v4 parity adoption (PLAN-AGENTPARITY-001).
// Replaces Phase 0 always-false stub with real assertions against the plugin repo.
// Detailed assertions in Cline_V4ParityTests.
// TR-MCP-AGENT-PARITY-020 (Cline plugin adoption).
using System.IO;
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

/// <summary>TR-MCP-AGENT-PARITY-020: Cline plugin phase 6 parity gap sentinel.</summary>
public class Cline_GapTests
{
    private static readonly string PluginRoot = Cline_V4ParityTests.ResolvePluginRoot();

    /// <summary>Sentinel: ENFORCEMENT.md must reference v4 - the primary Phase 6 gap from Phase 0 stub.</summary>
    [Fact]
    public void Cline_MustCloseV4ParityGaps_ForV4Compliance()
    {
        var possible = new[]
        {
            Path.Combine(PluginRoot, "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "lib", "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "docs", "ENFORCEMENT.md"),
        };
        var found = System.Array.Find(possible, File.Exists);
        Assert.True(found != null,
            "ENFORCEMENT.md missing from mcpserver-cline-plugin root or lib/ - required for Phase 6 v4 parity (TR-MCP-AGENT-PARITY-020)");
        var content = File.ReadAllText(found!);
        Assert.Contains("v4", content, System.StringComparison.OrdinalIgnoreCase);
    }
}
