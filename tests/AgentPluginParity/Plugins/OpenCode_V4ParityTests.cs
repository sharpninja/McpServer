// Phase 10 parity adoption tests for mcpserver-opencode-plugin (TR-MCP-AGENT-PARITY-020).
// Follows Byrd v4: written FIRST (RED for gaps), GREEN after plugin fixes are applied.
// Tests are static file/content assertions - no live MCP server or bash execution required.
// Plugin root resolved from sibling directory or OPENCODE_PLUGIN_ROOT env var.
using System;
using System.IO;
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

/// <summary>TR-MCP-AGENT-PARITY-020: Phase 10 parity assertions for mcpserver-opencode-plugin.</summary>
public class OpenCode_V4ParityTests
{
    internal static string ResolvePluginRoot()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var sibling = Path.GetFullPath(Path.Combine(repoRoot, "..", "mcpserver-opencode-plugin"));
        if (Directory.Exists(sibling)) return sibling;

        var envRoot = Environment.GetEnvironmentVariable("OPENCODE_PLUGIN_ROOT");
        if (!string.IsNullOrEmpty(envRoot) && Directory.Exists(envRoot)) return envRoot;

        return sibling;
    }

    private static readonly string PluginRoot = ResolvePluginRoot();

    private static string ReadSrc(string filename) =>
        File.ReadAllText(Path.Combine(PluginRoot, "src", filename));

    // =========================================================================
    // Already-passing - must stay GREEN
    // =========================================================================

    /// <summary>All 5 core MCP tool surfaces must exist in src/tools/ (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void OpenCode_ToolsCoverage_HasAll5CoreSurfaces()
    {
        foreach (var surface in new[] { "session", "todo", "requirements", "graphrag", "workspace" })
        {
            var p = Path.Combine(PluginRoot, "src", "tools", $"{surface}.ts");
            Assert.True(File.Exists(p), $"src/tools/{surface}.ts missing");
        }
    }

    // =========================================================================
    // Phase 10 gaps - RED until plugin fixes applied
    // =========================================================================

    /// <summary>Plugin version must be on the v1.x parity release line (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void OpenCode_Version_IsOnV1Line()
    {
        var versionFile = Path.Combine(PluginRoot, ".version");
        Assert.True(File.Exists(versionFile), ".version file missing");
        var version = File.ReadAllText(versionFile).Trim();
        Assert.True(version.StartsWith("1.", StringComparison.Ordinal),
            $"Plugin .version must be on v1.x parity release line, got: {version}");
    }

    /// <summary>ENFORCEMENT.md must document the shared v4 protocol (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void OpenCode_HasEnforcementMd_ReferencingSharedProtocol()
    {
        var possiblePaths = new[]
        {
            Path.Combine(PluginRoot, "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "lib", "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "docs", "ENFORCEMENT.md"),
        };
        var found = Array.Find(possiblePaths, File.Exists);
        Assert.True(found != null, "ENFORCEMENT.md missing (required for Phase 10 parity)");
        var content = File.ReadAllText(found!);
        Assert.Contains("v4", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shared", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Cache manager must declare a MAX_RETRIES constant (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void OpenCode_CacheManager_MaxRetriesIsThree()
    {
        var content = ReadSrc("cache/cache-manager.ts");
        Assert.Contains("MAX_RETRIES", content);
        Assert.Contains("3", content);
    }

    /// <summary>Cache manager must use workspace-scoped .mcpServer/failsafe layout (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void OpenCode_CacheManager_UsesWorkspaceFailsafeLayout()
    {
        var content = ReadSrc("cache/cache-manager.ts");
        Assert.Contains(".mcpServer", content);
        Assert.Contains("failsafe", content);
        Assert.Contains("WORKSPACE_PATH", content);
    }
}
