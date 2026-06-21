// Phase 4 parity adoption tests for mcpserver-grok-plugin (TR-MCP-AGENT-PARITY-020).
// Follows Byrd v4: written FIRST (RED for gaps), GREEN after plugin fixes are applied.
// Tests are static file/content assertions - no live MCP server or bash execution required.
// Plugin root resolved from sibling directory or GROK_PLUGIN_ROOT env var.
using System;
using System.IO;
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

/// <summary>TR-MCP-AGENT-PARITY-020: Phase 4 parity assertions for mcpserver-grok-plugin.</summary>
public class Grok_V4ParityTests
{
    internal static string ResolvePluginRoot()
    {
        // AppContext.BaseDirectory = tests/AgentPluginCore/bin/Debug/net10.0/  (5 levels up to repo root)
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var sibling = Path.GetFullPath(Path.Combine(repoRoot, "..", "mcpserver-grok-plugin"));
        if (Directory.Exists(sibling)) return sibling;

        var envRoot = Environment.GetEnvironmentVariable("GROK_PLUGIN_ROOT");
        if (!string.IsNullOrEmpty(envRoot) && Directory.Exists(envRoot)) return envRoot;

        return sibling; // return path even if missing; individual tests will fail with clear messages
    }

    private static readonly string PluginRoot = ResolvePluginRoot();

    private static string ReadLib(string filename) =>
        File.ReadAllText(Path.Combine(PluginRoot, "lib", filename));

    private static string ReadHooks(string filename) =>
        File.ReadAllText(Path.Combine(PluginRoot, "hooks", filename));

    // =========================================================================
    // Already-passing (Phase 0/1/2 work) - these must stay GREEN
    // =========================================================================

    /// <summary>Cache manager must enforce 3-retry limit matching v4 core (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void Grok_CacheManager_MaxRetriesIsThree()
    {
        Assert.Contains("MAX_RETRIES=3", ReadLib("cache-manager.sh"));
    }

    /// <summary>All required hook lifecycle events must be present (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Grok_HooksJson_HasFullRequiredSurface()
    {
        var json = ReadHooks("hooks.json");
        foreach (var hook in new[] { "SessionStart", "UserPromptSubmit", "Stop", "PostToolUse", "SessionEnd" })
            Assert.Contains(hook, json);
    }

    /// <summary>All 5 core MCP surfaces must have SKILL.md (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Grok_Skills_HasAll5CoreSurfaces()
    {
        foreach (var surface in new[] { "session", "todo", "requirements", "workspace", "graphrag" })
        {
            var path = Path.Combine(PluginRoot, "skills", surface, "SKILL.md");
            Assert.True(File.Exists(path), $"SKILL.md missing for surface: {surface}");
        }
    }

    /// <summary>Marker resolver must use HMAC-SHA256 matching v4 core (TR-MCP-AGENT-PARITY-012).</summary>
    [Fact]
    public void Grok_MarkerResolver_UsesHmacSha256()
    {
        var content = ReadLib("marker-resolver.sh");
        Assert.Contains("hmac-sha256", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Subagent capture scripts must exist (TR-MCP-AGENT-PARITY-020 subagent requirement).</summary>
    [Fact]
    public void Grok_SubagentCapture_ScriptsPresent()
    {
        Assert.True(File.Exists(Path.Combine(PluginRoot, "hooks", "scripts", "subagent-import.sh")),
            "subagent-import.sh missing");
        Assert.True(File.Exists(Path.Combine(PluginRoot, "lib", "codex-jsonl.js")),
            "codex-jsonl.js missing");
    }

    // =========================================================================
    // Phase 4 gaps - RED until plugin fixes applied
    // =========================================================================

    /// <summary>ENFORCEMENT.md must document the shared v4 protocol (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Grok_HasEnforcementMd_ReferencingSharedProtocol()
    {
        var possiblePaths = new[]
        {
            Path.Combine(PluginRoot, "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "lib", "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "docs", "ENFORCEMENT.md"),
        };
        var found = Array.Find(possiblePaths, File.Exists);
        Assert.True(found != null,
            "ENFORCEMENT.md missing from plugin root, lib/, or docs/ (required for Phase 4 parity)");
        var content = File.ReadAllText(found!);
        Assert.Contains("v4", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shared", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>cache-scope.sh must expose a base64url workspace key function (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void Grok_CacheScope_ProvidesBase64UrlWorkspaceKey()
    {
        var content = ReadLib("cache-scope.sh");
        // Phase 4: v4 parity adds cache_scope_workspace_key_v4 using base64url encoding
        Assert.Contains("cache_scope_workspace_key_v4", content);
        Assert.Contains("base64", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>cache-scope.sh must provide .mcpServer/failsafe layout per v4 core spec (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void Grok_CacheScope_ProvidesV1FailsafeLayout()
    {
        var content = ReadLib("cache-scope.sh");
        // Phase 4: v4 parity adds cache_scope_v4_failsafe_root using .mcpServer/failsafe/ layout
        Assert.Contains(".mcpServer/failsafe", content);
        Assert.Contains("cache_scope_v4_failsafe_root", content);
    }

    /// <summary>Plugin version must be on the v1.x parity release line (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Grok_Version_IsOnV1Line()
    {
        var versionFile = Path.Combine(PluginRoot, ".version");
        Assert.True(File.Exists(versionFile), ".version file missing");
        var version = File.ReadAllText(versionFile).Trim();
        Assert.True(version.StartsWith("1.", StringComparison.Ordinal),
            $"Plugin .version must be on v1.x parity release line, got: {version}");
    }

    // =========================================================================
    // Phase 4 session-failure bugs - RED until plugin fixes applied
    // =========================================================================

    /// <summary>marker-resolver.ps1 must not shadow the read-only $PID automatic variable (session failure fix).</summary>
    [Fact]
    public void Grok_MarkerResolverPs1_NoPidVariableShadowing()
    {
        var content = File.ReadAllText(Path.Combine(PluginRoot, "lib", "marker-resolver.ps1"));
        // $pid = ... shadows PowerShell's read-only automatic $PID variable, crashing bootstrap
        Assert.DoesNotContain("$pid =", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>hooks.json must reference GROK_PLUGIN_ROOT so hooks resolve without CLAUDE_PLUGIN_ROOT set.</summary>
    [Fact]
    public void Grok_HooksJson_UsesGrokPluginRootFallback()
    {
        var json = ReadHooks("hooks.json");
        Assert.Contains("GROK_PLUGIN_ROOT", json);
    }

    /// <summary>Grok-native plugin descriptor must exist for agent auto-discovery.</summary>
    [Fact]
    public void Grok_HasGrokPluginDescriptor()
    {
        var path = Path.Combine(PluginRoot, ".grok-plugin", "plugin.json");
        Assert.True(File.Exists(path), ".grok-plugin/plugin.json missing (required for Grok agent auto-discovery)");
    }

    /// <summary>Grok-native plugin descriptor must expose skills, hooks, and the MCP server bridge.</summary>
    [Fact]
    public void Grok_PluginDescriptor_DeclaresSkillsHooksAndMcpServer()
    {
        var path = Path.Combine(PluginRoot, ".grok-plugin", "plugin.json");
        var json = File.ReadAllText(path);

        Assert.Contains("\"skills\"", json, StringComparison.Ordinal);
        Assert.Contains("\"hooks\"", json, StringComparison.Ordinal);
        Assert.Contains("\"mcpServers\"", json, StringComparison.Ordinal);
        Assert.Contains(".mcp.json", json, StringComparison.Ordinal);

        var mcpJson = File.ReadAllText(Path.Combine(PluginRoot, ".mcp.json"));
        Assert.Contains("\"mcpserver\"", mcpJson, StringComparison.Ordinal);
        Assert.Contains("\"type\": \"http\"", mcpJson, StringComparison.Ordinal);
        Assert.Contains("mcp-transport", mcpJson, StringComparison.Ordinal);
        Assert.DoesNotContain("mcpserver-repl", mcpJson, StringComparison.Ordinal);
        Assert.DoesNotContain("--agent-stdio", mcpJson, StringComparison.Ordinal);
    }

    /// <summary>Claude-compatible descriptor must also expose skills and MCP server config for Grok compatibility loading.</summary>
    [Fact]
    public void Grok_ClaudeCompatibleDescriptor_DeclaresSkillsAndMcpServer()
    {
        var path = Path.Combine(PluginRoot, ".claude-plugin", "plugin.json");
        Assert.True(File.Exists(path), ".claude-plugin/plugin.json missing");

        var json = File.ReadAllText(path);
        Assert.Contains("\"skills\"", json, StringComparison.Ordinal);
        Assert.Contains("\"mcpServers\"", json, StringComparison.Ordinal);
        Assert.Contains(".mcp.json", json, StringComparison.Ordinal);
    }
}
