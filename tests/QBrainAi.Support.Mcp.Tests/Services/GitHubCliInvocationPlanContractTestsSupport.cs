namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-FED-PATH-001 AC5: the setting name a GitHub CLI refusal names, used by the unit contract GitHubCliInvocationPlanContractTests in QBrainAi.Support.Mcp.Tests and by the real-host binding helper of GitHubCliServiceTests in QBrainAi.Support.Mcp.EnvironmentIntegrationTests, defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-FED-PATH-001 AC5; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from GitHubCliInvocationPlanContractTests (RepositorySetting, with its documentation) when the real-host binding helper that also asserts on it moved to the integration-owned project (slice S8, hostile validation finding R14-F4), so both use one definition instead of a copy (DRY); its accessibility stays internal.</remarks>
internal static class GitHubCliInvocationPlanContractTestsSupport
{
    /// <summary>The setting a refusal names.</summary>
    internal const string RepositorySetting = "Mcp:GitHub:Repository";
}
