// Phase 5 gap closure: Codex plugin v4 parity adoption (PLAN-AGENTPARITY-001).
// Replaces Phase 0 always-false stub with real assertions against the plugin repo.
// Detailed assertions in Codex_V4ParityTests.
// TR-MCP-AGENT-PARITY-020 (Codex plugin adoption).
using System.IO;
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

/// <summary>TR-MCP-AGENT-PARITY-020: Codex plugin phase 5 parity gap sentinel.</summary>
public class Codex_GapTests
{
    private static readonly string PluginRoot = Codex_V4ParityTests.ResolvePluginRoot();

    /// <summary>Sentinel: ENFORCEMENT.md must exist - the primary Phase 5 gap from Phase 0 stub.</summary>
    [Fact]
    public void Codex_MustCloseV4ParityGaps_ForV4Compliance()
    {
        // Phase 5: key gap is ENFORCEMENT.md documenting shared v4 protocol
        var possible = new[]
        {
            Path.Combine(PluginRoot, "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "lib", "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "docs", "ENFORCEMENT.md"),
        };
        var found = System.Array.Find(possible, File.Exists);
        Assert.True(found != null,
            "ENFORCEMENT.md missing from mcpserver-codex-plugin root or lib/ - required for Phase 5 v4 parity (TR-MCP-AGENT-PARITY-020)");
    }
}
