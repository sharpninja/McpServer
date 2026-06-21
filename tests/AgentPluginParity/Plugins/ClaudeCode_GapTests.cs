// Phase 3 gap closure: ClaudeCode plugin v4 parity adoption (PLAN-AGENTPARITY-001).
// Replaces Phase 0 always-false stub with real assertions against the plugin repo.
// Detailed assertions in ClaudeCode_V4ParityTests.
// TR-MCP-AGENT-PARITY-020 (ClaudeCode plugin adoption).
using System.IO;
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

/// <summary>TR-MCP-AGENT-PARITY-020: ClaudeCode plugin phase 3 parity gap sentinel.</summary>
public class ClaudeCode_GapTests
{
    private static readonly string PluginRoot = ClaudeCode_V4ParityTests.ResolvePluginRoot();

    /// <summary>Sentinel: ENFORCEMENT.md must exist - the primary Phase 3 gap from Phase 0 stub.</summary>
    [Fact]
    public void ClaudeCode_MustCloseV4ParityGaps_ForV4Compliance()
    {
        // Phase 3: key gap is ENFORCEMENT.md documenting shared v4 protocol
        var possible = new[]
        {
            Path.Combine(PluginRoot, "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "lib", "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "docs", "ENFORCEMENT.md"),
        };
        var found = System.Array.Find(possible, File.Exists);
        Assert.True(found != null,
            "ENFORCEMENT.md missing from mcpserver-claude-code-plugin root or lib/ - required for Phase 3 v4 parity (TR-MCP-AGENT-PARITY-020)");
    }
}
