namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-7: the collection name that both halves of SqlServerLocalDbCollectionTests use (the mock gate half in QBrainAi.Support.Mcp.Tests, the binding half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-TESTRESOURCE-001 ac-7; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from SqlServerLocalDbCollectionTests (RequiredCollection) when that class split into its mock gate half and its binding half (slice S8, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class SqlServerLocalDbCollectionTestsSupport
{
    internal const string RequiredCollection = "SQL Server LocalDB";
}
