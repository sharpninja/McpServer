using Xunit;

namespace QBrainAi.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 AC-001 (PLAN-SESSIONTRIAGEFIX-001 E5, triage-report-914a2812): the deterministic matrix gives each PowerShell host
/// its scenario session id through MCP_SESSION_ID, the plugins' own session-id input, so every verb persists under that id.
/// Mock gate: the environment assertion is proven against pinned environments and rejects a missing id, another id, and a suffix-only
/// setting. Binding: the environment PluginSessionLogWorkflowAdapter.ApplyScenarioSessionId builds for each host kind; the five PowerShell rows
/// of Theory_Agent_BootstrapBeginAppendComplete_CapturesIdsCacheShaReceipt are the end-to-end proof. Fixture: none (environment maps only).
/// </summary>
[Trait("PluginInt", "Deterministic")]
[Trait("Category", "Integration")]
public sealed class PluginScenarioSessionIdTests
{
    private static readonly PluginHostKind[] PowerShellHosts =
        [PluginHostKind.Codex, PluginHostKind.ClaudeCode, PluginHostKind.ClaudeCowork, PluginHostKind.Copilot, PluginHostKind.Grok];

    private static readonly PluginHostKind[] NodeHosts = [PluginHostKind.Cline, PluginHostKind.ClineV2, PluginHostKind.OpenCode];

    /// <summary>TEST-MCP-PLUGININT-001 AC-001: a PowerShell host's environment carries exactly the scenario session id in MCP_SESSION_ID.</summary>
    private static void AssertScenarioSessionEnvironment(IReadOnlyDictionary<string, string> environment, string sessionId)
    {
        Assert.True(environment.TryGetValue("MCP_SESSION_ID", out var value), "MCP_SESSION_ID is set for a PowerShell host.");
        Assert.Equal(sessionId, value);
        Assert.EndsWith("-pluginint", value, StringComparison.Ordinal);
    }

    /// <summary>
    /// Mock gate for TEST-MCP-PLUGININT-001 AC-001: the assertion accepts an environment carrying the pinned scenario session id and rejects one
    /// without it, one with the plugin-minted id, and one that sets only MCP_SESSION_SUFFIX. Data: pinned dictionaries; no adapter, no process.
    /// </summary>
    [Fact]
    public void ScenarioSessionEnvironmentAssertion_AcceptsPinnedIdAndRejectsMissingOtherOrSuffixOnly()
    {
        const string sessionId = "Codex-20261015T000000Z-pluginint";
        AssertScenarioSessionEnvironment(new Dictionary<string, string>(StringComparer.Ordinal) { ["MCP_SESSION_ID"] = sessionId }, sessionId);
        Assert.ThrowsAny<Exception>(() => AssertScenarioSessionEnvironment(new Dictionary<string, string>(StringComparer.Ordinal), sessionId));
        Assert.ThrowsAny<Exception>(() => AssertScenarioSessionEnvironment(
            new Dictionary<string, string>(StringComparer.Ordinal) { ["MCP_SESSION_ID"] = "Codex-20261015T000000Z-plugin-session" },
            sessionId));
        Assert.ThrowsAny<Exception>(() => AssertScenarioSessionEnvironment(
            new Dictionary<string, string>(StringComparer.Ordinal) { ["MCP_SESSION_SUFFIX"] = "pluginint" },
            sessionId));
    }

    /// <summary>
    /// Binding for TEST-MCP-PLUGININT-001 AC-001: ApplyScenarioSessionId gives every PowerShell host kind the scenario session id. Data: one
    /// pinned id per host kind added to an environment that already holds the workspace path.
    /// </summary>
    [Fact]
    public void ScenarioSessionEnvironment_PowerShellHosts_CarryScenarioSessionId()
    {
        foreach (var hostKind in PowerShellHosts)
        {
            var sessionId = hostKind + "-20261015T000000Z-pluginint";
            var environment = new Dictionary<string, string>(StringComparer.Ordinal) { ["MCP_WORKSPACE_PATH"] = "/tmp/pluginint-workspace" };
            PluginSessionLogWorkflowAdapter.ApplyScenarioSessionId(environment, hostKind, sessionId);
            AssertScenarioSessionEnvironment(environment, sessionId);
            Assert.Equal("/tmp/pluginint-workspace", environment["MCP_WORKSPACE_PATH"]);
        }
    }

    /// <summary>
    /// Binding for TEST-MCP-PLUGININT-001 AC-001: Node host kinds keep their environment unchanged; their session id travels through the
    /// Node path. Data: one pinned id per Node host kind.
    /// </summary>
    [Fact]
    public void ScenarioSessionEnvironment_NodeHosts_Unchanged()
    {
        foreach (var hostKind in NodeHosts)
        {
            var environment = new Dictionary<string, string>(StringComparer.Ordinal) { ["MCP_WORKSPACE_PATH"] = "/tmp/pluginint-workspace" };
            PluginSessionLogWorkflowAdapter.ApplyScenarioSessionId(environment, hostKind, hostKind + "-20261015T000000Z-pluginint");
            Assert.False(environment.ContainsKey("MCP_SESSION_ID"), hostKind + " keeps its environment unchanged.");
            Assert.Single(environment);
        }
    }
}
