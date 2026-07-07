using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

/// <summary>
/// TEST-MCP-PLUGIN-TRIAGE-001: static parity coverage for triage guidance in every
/// MCP Server plugin skill bundle changed by the triage feature.
/// </summary>
public sealed class TriageSkillBundleTests
{
    /// <summary>Enumerates MCP Server plugin roots that must carry triage guidance.</summary>
    /// <returns>Agent name, sibling plugin directory, and environment variable override.</returns>
    public static IEnumerable<object[]> PluginRoots()
    {
        yield return ["Codex", "mcpserver-codex-plugin", "CODEX_PLUGIN_ROOT"];
        yield return ["Claude", "mcpserver-claude-code-plugin", "CLAUDE_PLUGIN_ROOT"];
        yield return ["Copilot", "mcpserver-copilot-plugin", "COPILOT_PLUGIN_ROOT"];
        yield return ["Cline", "mcpserver-cline-plugin", "CLINE_PLUGIN_ROOT"];
        yield return ["GrokCode", "mcpserver-grok-plugin", "GROK_PLUGIN_ROOT"];
    }

    /// <summary>
    /// TEST-MCP-PLUGIN-TRIAGE-001: each plugin has a triage skill that tells agents when
    /// to use triage, when not to use it, and that intake is asynchronous.
    /// </summary>
    /// <param name="agentName">The plugin agent identity.</param>
    /// <param name="directoryName">The sibling plugin directory.</param>
    /// <param name="environmentVariable">Optional environment variable override.</param>
    [Theory]
    [MemberData(nameof(PluginRoots))]
    public void PluginTriageSkill_IncludesAsyncIncidentalBugGuidance(
        string agentName,
        string directoryName,
        string environmentVariable)
    {
        var pluginRoot = ResolvePluginRoot(directoryName, environmentVariable);
        var skillPath = Path.Combine(pluginRoot, "skills", "triage", "SKILL.md");

        Assert.True(File.Exists(skillPath), $"{agentName} triage skill missing: {skillPath}");
        var content = File.ReadAllText(skillPath);

        Assert.Contains("incidental bug", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("active requested fix", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not expect immediate resolution", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("continue", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("triage_report", content, StringComparison.Ordinal);
        Assert.Contains("triage_status", content, StringComparison.Ordinal);
        Assert.Contains("McpServer", content, StringComparison.Ordinal);
        Assert.Contains("plugin", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-PLUGIN-TRIAGE-001: each plugin schema accepts the workflow.triage wrapper
    /// namespace and validates the report/status request shapes exposed in the triage skill.
    /// </summary>
    /// <param name="agentName">The plugin agent identity.</param>
    /// <param name="directoryName">The sibling plugin directory.</param>
    /// <param name="environmentVariable">Optional environment variable override.</param>
    [Theory]
    [MemberData(nameof(PluginRoots))]
    public void PluginTriageSchema_IncludesWorkflowTriageRules(
        string agentName,
        string directoryName,
        string environmentVariable)
    {
        var pluginRoot = ResolvePluginRoot(directoryName, environmentVariable);
        var schemaPath = Path.Combine(pluginRoot, "schemas", "repl-yaml-message.schema.json");

        Assert.True(File.Exists(schemaPath), $"{agentName} triage schema missing: {schemaPath}");
        var content = File.ReadAllText(schemaPath);

        Assert.Contains("workflow\\\\.(sessionlog|todo|memory|requirements|graphrag|triage)", content, StringComparison.Ordinal);
        Assert.Contains("\"triageRules\"", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.report", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.getReport", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.queryGroups", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.dashboard", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.getGroup", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.queryRuns", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.getRun", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.queryCreatedTodos", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.flushGroup", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.retryGroup", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.createGroup", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.consolidateIntoGroup", content, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.mergeGroups", content, StringComparison.Ordinal);
        Assert.Contains("\"required\": [\"title\", \"summary\"]", content, StringComparison.Ordinal);
        Assert.Contains("\"required\": [\"reportId\"]", content, StringComparison.Ordinal);
        Assert.Contains("\"required\": [\"groupId\"]", content, StringComparison.Ordinal);
        Assert.Contains("\"required\": [\"runId\"]", content, StringComparison.Ordinal);
        Assert.Contains("\"required\": [\"targetGroupId\"]", content, StringComparison.Ordinal);
    }

    private static string ResolvePluginRoot(string directoryName, string environmentVariable)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var sibling = Path.GetFullPath(Path.Combine(repoRoot, "..", directoryName));
        if (Directory.Exists(sibling))
            return sibling;

        var envRoot = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(envRoot) && Directory.Exists(envRoot))
            return envRoot;

        return sibling;
    }
}
