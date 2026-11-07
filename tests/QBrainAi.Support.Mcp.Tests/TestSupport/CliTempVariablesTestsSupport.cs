using Xunit;
using static QBrainAi.Support.Mcp.Tests.TestSupport.CliTempVariables;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-10; TR-MCP-USECASE-018 AC-006: the helpers that both halves of CliTempVariablesTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-TESTRESOURCE-001 ac-10; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from CliTempVariablesTests (Folder and Prior) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class CliTempVariablesTestsSupport
{
    internal const string Folder = "/scripted/cli-temp-folder";

    internal static string? Prior(string name) =>
        name == "MCPSERVER_GROK_ONESHOT_TEMP" ? null : "/scripted/prior/" + name;
}
