// Phase 5 parity adoption tests for mcpserver-codex-plugin (TR-MCP-AGENT-PARITY-020).
// Follows Byrd v4: written FIRST (RED for gaps), GREEN after plugin fixes are applied.
// Tests are static file/content assertions - no live MCP server or bash execution required.
// Plugin root resolved from sibling directory or CODEX_PLUGIN_ROOT env var.
// Note: Codex uses lib/ for hook scripts and .codex-plugin/plugin.json for activation
//       rather than hooks/hooks.json + hooks/scripts/. Assertions reflect that architecture.
using System;
using System.IO;
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

/// <summary>TR-MCP-AGENT-PARITY-020: Phase 5 parity assertions for mcpserver-codex-plugin.</summary>
public class Codex_V4ParityTests
{
    internal static string ResolvePluginRoot()
    {
        // AppContext.BaseDirectory = tests/AgentPluginCore/bin/Debug/net10.0/  (5 levels up to repo root)
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var sibling = Path.GetFullPath(Path.Combine(repoRoot, "..", "mcpserver-codex-plugin"));
        if (Directory.Exists(sibling)) return sibling;

        var envRoot = Environment.GetEnvironmentVariable("CODEX_PLUGIN_ROOT");
        if (!string.IsNullOrEmpty(envRoot) && Directory.Exists(envRoot)) return envRoot;

        return sibling; // return path even if missing; individual tests will fail with clear messages
    }

    private static readonly string PluginRoot = ResolvePluginRoot();

    private static string ReadLib(string filename) =>
        File.ReadAllText(Path.Combine(PluginRoot, "lib", filename));

    // =========================================================================
    // Already-passing (Phase 0/1/2 work) - these must stay GREEN
    // =========================================================================

    /// <summary>Cache manager must enforce 3-retry limit matching v4 core (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void Codex_CacheManager_MaxRetriesIsThree()
    {
        Assert.Contains("MAX_RETRIES=3", ReadLib("cache-manager.sh"));
    }

    /// <summary>Core lifecycle hook scripts must exist in lib/ (Codex uses lib/ not hooks/scripts/) (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Codex_LifecycleScripts_Present()
    {
        foreach (var script in new[] { "session-start.sh", "user-prompt-submit.sh", "stop-gate.sh" })
            Assert.True(File.Exists(Path.Combine(PluginRoot, "lib", script)),
                $"Lifecycle script missing: lib/{script}");
    }

    /// <summary>All 5 core MCP surfaces must have SKILL.md (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Codex_Skills_HasAll5CoreSurfaces()
    {
        foreach (var surface in new[] { "session", "todo", "requirements", "workspace", "graphrag" })
        {
            var path = Path.Combine(PluginRoot, "skills", surface, "SKILL.md");
            Assert.True(File.Exists(path), $"SKILL.md missing for surface: {surface}");
        }
    }

    /// <summary>Marker resolver must use HMAC-SHA256 matching v4 core (TR-MCP-AGENT-PARITY-012).</summary>
    [Fact]
    public void Codex_MarkerResolver_UsesHmacSha256()
    {
        Assert.Contains("hmac-sha256", ReadLib("marker-resolver.sh"), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>codex-jsonl.js must exist for JSONL transcript processing (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Codex_CodexJsonl_ScriptPresent()
    {
        Assert.True(File.Exists(Path.Combine(PluginRoot, "lib", "codex-jsonl.js")),
            "lib/codex-jsonl.js missing");
    }

    /// <summary>Plugin version must be on the v1.x parity release line (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Codex_Version_IsOnV1Line()
    {
        var versionFile = Path.Combine(PluginRoot, ".version");
        Assert.True(File.Exists(versionFile), ".version file missing");
        var version = File.ReadAllText(versionFile).Trim();
        Assert.True(version.StartsWith("1.", StringComparison.Ordinal),
            $"Plugin .version must be on v1.x parity release line, got: {version}");
    }

    // =========================================================================
    // Phase 5 gaps - RED until plugin fixes applied
    // =========================================================================

    /// <summary>ENFORCEMENT.md must document the shared v4 protocol (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Codex_HasEnforcementMd_ReferencingSharedProtocol()
    {
        var possiblePaths = new[]
        {
            Path.Combine(PluginRoot, "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "lib", "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "docs", "ENFORCEMENT.md"),
        };
        var found = Array.Find(possiblePaths, File.Exists);
        Assert.True(found != null,
            "ENFORCEMENT.md missing from plugin root, lib/, or docs/ (required for Phase 5 parity)");
        var content = File.ReadAllText(found!);
        Assert.Contains("v4", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shared", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>cache-scope.sh must expose a base64url workspace key function (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void Codex_CacheScope_ProvidesBase64UrlWorkspaceKey()
    {
        var content = ReadLib("cache-scope.sh");
        Assert.Contains("cache_scope_workspace_key_v4", content);
        Assert.Contains("base64", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>cache-scope.sh must provide .mcpServer/failsafe layout per v4 core spec (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void Codex_CacheScope_ProvidesV1FailsafeLayout()
    {
        var content = ReadLib("cache-scope.sh");
        Assert.Contains(".mcpServer/failsafe", content);
        Assert.Contains("cache_scope_v4_failsafe_root", content);
    }

    /// <summary>Subagent capture script must exist in lib/ for Codex architecture (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Codex_SubagentCapture_ScriptPresent()
    {
        Assert.True(File.Exists(Path.Combine(PluginRoot, "lib", "subagent-import.sh")),
            "lib/subagent-import.sh missing (Codex keeps hook scripts in lib/, not hooks/scripts/)");
    }
}
