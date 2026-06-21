// Phase 8 parity adoption tests for mcpserver-copilot-plugin (TR-MCP-AGENT-PARITY-020).
// Follows Byrd v4: written FIRST (RED for gaps), GREEN after plugin fixes are applied.
// Tests are static file/content assertions - no live MCP server or bash execution required.
// Plugin root resolved from sibling directory or COPILOT_PLUGIN_ROOT env var.
using System;
using System.IO;
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

/// <summary>TR-MCP-AGENT-PARITY-020: Phase 8 parity assertions for mcpserver-copilot-plugin.</summary>
public class Copilot_V4ParityTests
{
    internal static string ResolvePluginRoot()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var sibling = Path.GetFullPath(Path.Combine(repoRoot, "..", "mcpserver-copilot-plugin"));
        if (Directory.Exists(sibling)) return sibling;

        var envRoot = Environment.GetEnvironmentVariable("COPILOT_PLUGIN_ROOT");
        if (!string.IsNullOrEmpty(envRoot) && Directory.Exists(envRoot)) return envRoot;

        return sibling;
    }

    private static readonly string PluginRoot = ResolvePluginRoot();

    private static string ReadLib(string filename) =>
        File.ReadAllText(Path.Combine(PluginRoot, "lib", filename));

    private static string ReadHooks(string filename) =>
        File.ReadAllText(Path.Combine(PluginRoot, "hooks", filename));

    // =========================================================================
    // Already-passing - must stay GREEN
    // =========================================================================

    /// <summary>Cache manager must enforce 3-retry limit (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void Copilot_CacheManager_MaxRetriesIsThree()
    {
        Assert.Contains("MAX_RETRIES=3", ReadLib("cache-manager.sh"));
    }

    /// <summary>All required hook lifecycle events must be present (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Copilot_HooksJson_HasFullRequiredSurface()
    {
        // hooks.json is at plugin root for Copilot
        var json = File.ReadAllText(Path.Combine(PluginRoot, "hooks.json"));
        foreach (var hook in new[] { "SessionStart", "UserPromptSubmit", "Stop", "PostToolUse", "SessionEnd" })
            Assert.Contains(hook, json);
    }

    /// <summary>All 5 core MCP surfaces must have SKILL.md (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Copilot_Skills_HasAll5CoreSurfaces()
    {
        foreach (var surface in new[] { "session", "todo", "requirements", "workspace", "graphrag" })
        {
            var p = Path.Combine(PluginRoot, "skills", surface, "SKILL.md");
            Assert.True(File.Exists(p), $"SKILL.md missing for surface: {surface}");
        }
    }

    /// <summary>Marker resolver must use HMAC-SHA256 (TR-MCP-AGENT-PARITY-012).</summary>
    [Fact]
    public void Copilot_MarkerResolver_UsesHmacSha256()
    {
        Assert.Contains("hmac-sha256", ReadLib("marker-resolver.sh"), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Plugin version must be on the v1.x parity release line (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Copilot_Version_IsOnV1Line()
    {
        var versionFile = Path.Combine(PluginRoot, ".version");
        Assert.True(File.Exists(versionFile), ".version file missing");
        var version = File.ReadAllText(versionFile).Trim();
        Assert.True(version.StartsWith("1.", StringComparison.Ordinal),
            $"Plugin .version must be on v1.x parity release line, got: {version}");
    }

    // =========================================================================
    // Phase 8 gaps - RED until plugin fixes applied
    // =========================================================================

    /// <summary>ENFORCEMENT.md must document the shared v4 protocol (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Copilot_HasEnforcementMd_ReferencingSharedProtocol()
    {
        var possiblePaths = new[]
        {
            Path.Combine(PluginRoot, "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "lib", "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "docs", "ENFORCEMENT.md"),
        };
        var found = Array.Find(possiblePaths, File.Exists);
        Assert.True(found != null, "ENFORCEMENT.md missing (required for Phase 8 parity)");
        var content = File.ReadAllText(found!);
        Assert.Contains("v4", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shared", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>cache-scope.sh must expose a base64url workspace key function (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void Copilot_CacheScope_ProvidesBase64UrlWorkspaceKey()
    {
        var content = ReadLib("cache-scope.sh");
        Assert.Contains("cache_scope_workspace_key_v4", content);
        Assert.Contains("base64", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>cache-scope.sh must provide .mcpServer/failsafe layout (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void Copilot_CacheScope_ProvidesV4FailsafeLayout()
    {
        var content = ReadLib("cache-scope.sh");
        Assert.Contains(".mcpServer/failsafe", content);
        Assert.Contains("cache_scope_v4_failsafe_root", content);
    }

    /// <summary>Subagent capture scripts must exist (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Copilot_SubagentCapture_ScriptsPresent()
    {
        Assert.True(File.Exists(Path.Combine(PluginRoot, "hooks", "scripts", "subagent-import.sh")),
            "hooks/scripts/subagent-import.sh missing");
        Assert.True(File.Exists(Path.Combine(PluginRoot, "lib", "codex-jsonl.js")),
            "lib/codex-jsonl.js missing");
    }
}
