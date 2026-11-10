using System.Diagnostics;
using System.Text;

using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.TestSupport;

using Microsoft.Extensions.Logging.Abstractions;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Tests the one-shot CLI execution strategy's command shapes for Codex, Claude, Grok and Cline, and the strategy
/// name. The class runs in <see cref="ProcessEnvironmentCollection"/> because every test that starts a session writes
/// the process environment: the strategy's temp-folder lookup reads the CLI temp variables, so those tests set and
/// restore them through <see cref="CliTempVariables"/> and are integration tests (TR-MCP-USECASE-018 AC-006,
/// TR-MCP-USECASE-019 AC-008; hostile validation finding R6-F1).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 4 Category=Integration cases (ReadInitialResponseAsync_WithCodexAgent_UsesCodexOneShotCommandShape, ReadInitialResponseAsync_WithClaudeAgent_UsesClaudePlanModeCommandShape, ReadInitialResponseAsync_WithGrokAgent_UsesGrokPromptFileCommandShape and ReadInitialResponseAsync_WithClineAgent_UsesClineParameterizedPromptCommandShape) and the helpers only they use (PrimaryTempName, CreateStrategy, CreateRequest, GetArgumentAfter, CapturingProcessEnvironmentService, CapturingProcessSpawner and CapturingSpawnedProcess) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class OneShotCliAgentExecutionStrategyTests
{
    /// <summary>The one-shot strategy name is supported and is the default instead of Copilot.</summary>
    [Fact]
    public void AgentExecutionStrategyNames_SupportsOneShotCliAndDefaultsAwayFromCopilot()
    {
        Assert.Contains(AgentExecutionStrategyNames.OneShotCli, AgentExecutionStrategyNames.SupportedNames);
        Assert.True(AgentExecutionStrategyNames.IsSupported(AgentExecutionStrategyNames.OneShotCli));
        Assert.Equal(AgentExecutionStrategyNames.OneShotCli, AgentExecutionStrategyNames.NormalizeOrDefault(null));
    }
}
