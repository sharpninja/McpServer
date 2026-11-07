using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-003: Verifies the Codex CLI execution strategy used by
/// background triage invokes <c>codex exec</c> non-interactively. The class runs in
/// <see cref="ProcessEnvironmentCollection"/> because every test that starts a session writes the process environment:
/// the strategy's temp-folder lookup reads the CLI temp variables, so those tests set and restore them through
/// <see cref="CliTempVariables"/> and are integration tests (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008;
/// hostile validation finding R6-F1).
/// </summary>
/// <remarks>
/// <para>2026-11-07, TEST-MCP-TRIAGE-003; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 4 Category=Integration cases (ReadInitialResponseAsync_WhenCodexFails_ReturnsErrorResult, ReadInitialResponseAsync_WhenCodexTimesOut_PreservesPartialStreams, ReadInitialResponseAsync_WithOutputCallback_StreamsAgentOutput and ReadInitialResponseAsync_WithConfiguredSharedTempRoot_UsesSharedOutputPath) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (PrimaryTempName, CreateRequest, CapturingProcessEnvironmentService, CapturingProcessSpawner and FakeSpawnedProcess) moved once into the abstract base class CodexCliAgentExecutionStrategyTestsBase in Services/CodexCliAgentExecutionStrategyTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</para>
/// <para>2026-11-07, TEST-MCP-TRIAGE-003; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the two helpers only the Windows and Linux platform classes of this file use (LaunchDefaultAsync and AssertCodexExecWithoutCopilotInteractiveFlags) moved unchanged to the integration half of the same name in QBrainAi.Support.Mcp.EnvironmentIntegrationTests, together with those two Category=Integration classes (slice S8, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit case and the class attributes are unchanged.</para>
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class CodexCliAgentExecutionStrategyTests : CodexCliAgentExecutionStrategyTestsBase
{
    /// <summary>
    /// TEST-MCP-TRIAGE-003: The strategy resolver recognizes the configured
    /// <c>codex-cli</c> strategy name.
    /// </summary>
    [Fact]
    public void AgentExecutionStrategyNames_SupportsCodexCli()
    {
        Assert.True(AgentExecutionStrategyNames.IsSupported("codex-cli"));
        Assert.Contains("codex-cli", AgentExecutionStrategyNames.SupportedNames);
    }
}
