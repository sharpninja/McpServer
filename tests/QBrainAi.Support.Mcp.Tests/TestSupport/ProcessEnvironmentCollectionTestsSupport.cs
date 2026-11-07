namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-9: the collection name that both halves of ProcessEnvironmentCollectionTests use (the mock gate half in QBrainAi.Support.Mcp.Tests, the binding half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-TESTRESOURCE-001 ac-9; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from ProcessEnvironmentCollectionTests (RequiredCollection) when that class split into its mock gate half and its binding half (slice S8, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class ProcessEnvironmentCollectionTestsSupport
{
    internal const string RequiredCollection = "Process environment";
}
