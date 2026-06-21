// Phase 6 parity adoption tests for mcpserver-cline-plugin (TR-MCP-AGENT-PARITY-020).
// Follows Byrd v4: written FIRST (RED for gaps), GREEN after plugin fixes are applied.
// Tests are static file/content assertions - no live MCP server or bash execution required.
// Plugin root resolved from sibling directory or CLINE_PLUGIN_ROOT env var.
// Note: Cline is TypeScript-based. Cache management is in src/cache/cache-manager.ts,
//       tools coverage is in src/tools/, lifecycle scripts are in lib/.
using System;
using System.IO;
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

/// <summary>TR-MCP-AGENT-PARITY-020: Phase 6 parity assertions for mcpserver-cline-plugin.</summary>
public class Cline_V4ParityTests
{
    internal static string ResolvePluginRoot()
    {
        // AppContext.BaseDirectory = tests/AgentPluginCore/bin/Debug/net10.0/  (5 levels up to repo root)
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var sibling = Path.GetFullPath(Path.Combine(repoRoot, "..", "mcpserver-cline-plugin"));
        if (Directory.Exists(sibling)) return sibling;

        var envRoot = Environment.GetEnvironmentVariable("CLINE_PLUGIN_ROOT");
        if (!string.IsNullOrEmpty(envRoot) && Directory.Exists(envRoot)) return envRoot;

        return sibling; // return path even if missing; individual tests will fail with clear messages
    }

    private static readonly string PluginRoot = ResolvePluginRoot();

    private static string ReadSrc(string relativePath) =>
        File.ReadAllText(Path.Combine(PluginRoot, "src", relativePath));

    // =========================================================================
    // Already-passing (Phase 0/1/2 work) - these must stay GREEN
    // =========================================================================

    /// <summary>TypeScript cache manager must enforce 3-retry limit matching v4 core (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void Cline_CacheManager_MaxRetriesIsThree()
    {
        Assert.Contains("MAX_RETRIES = 3", ReadSrc("cache/cache-manager.ts"));
    }

    /// <summary>Core lifecycle scripts must exist in lib/ (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Cline_LifecycleScripts_Present()
    {
        foreach (var script in new[] { "user-prompt-submit.sh", "stop-gate.sh" })
            Assert.True(File.Exists(Path.Combine(PluginRoot, "lib", script)),
                $"Lifecycle script missing: lib/{script}");
    }

    /// <summary>All 5 core MCP tool surfaces must be implemented in src/tools/ (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Cline_ToolsCoverage_HasAll5CoreSurfaces()
    {
        // Cline exposes MCP tools directly; tools/ replaces skills/ SKILL.md for this plugin
        foreach (var toolFile in new[] { "session.ts", "todo.ts", "requirements.ts", "graphrag.ts" })
            Assert.True(File.Exists(Path.Combine(PluginRoot, "src", "tools", toolFile)),
                $"Tool file missing: src/tools/{toolFile}");
        // workspace surface via skills/
        Assert.True(File.Exists(Path.Combine(PluginRoot, "skills", "workspace", "SKILL.md")),
            "skills/workspace/SKILL.md missing");
    }

    /// <summary>Cache manager must use .mcpServer/failsafe layout (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void Cline_CacheManager_UsesFailsafeLayout()
    {
        var content = ReadSrc("cache/cache-manager.ts");
        // TypeScript uses path.join('.mcpServer', 'failsafe') - check tokens separately
        Assert.Contains(".mcpServer", content);
        Assert.Contains("failsafe", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Plugin version must be on the v1.x parity release line (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Cline_Version_IsOnV1Line()
    {
        var versionFile = Path.Combine(PluginRoot, ".version");
        Assert.True(File.Exists(versionFile), ".version file missing");
        var version = File.ReadAllText(versionFile).Trim();
        Assert.True(version.StartsWith("1.", StringComparison.Ordinal),
            $"Plugin .version must be on v1.x parity release line, got: {version}");
    }

    // =========================================================================
    // Phase 6 gaps - RED until plugin fixes applied
    // =========================================================================

    /// <summary>ENFORCEMENT.md must reference the v4 shared protocol (TR-MCP-AGENT-PARITY-020).</summary>
    [Fact]
    public void Cline_HasEnforcementMd_ReferencingSharedProtocol()
    {
        var possiblePaths = new[]
        {
            Path.Combine(PluginRoot, "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "lib", "ENFORCEMENT.md"),
            Path.Combine(PluginRoot, "docs", "ENFORCEMENT.md"),
        };
        var found = Array.Find(possiblePaths, File.Exists);
        Assert.True(found != null,
            "ENFORCEMENT.md missing from plugin root, lib/, or docs/ (required for Phase 6 parity)");
        var content = File.ReadAllText(found!);
        Assert.Contains("v4", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shared", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>TypeScript cache manager must provide base64url workspace key for v4 scoping (TR-MCP-AGENT-PARITY-013).</summary>
    [Fact]
    public void Cline_CacheManager_ProvidesV4WorkspaceKeyEncoding()
    {
        var content = ReadSrc("cache/cache-manager.ts");
        // v4 parity: failsafe path must include base64url-encoded workspace key in workspaces/ sub-path
        Assert.Contains("base64", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("workspaces", content, StringComparison.OrdinalIgnoreCase);
    }
}
