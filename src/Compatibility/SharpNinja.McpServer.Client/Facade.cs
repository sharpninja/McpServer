namespace QBrainAi.Client.Compatibility;

/// <summary>
/// TR-MCP-QBRAIN-003: Marker type for the SharpNinja.McpServer.Client 1.x facade package.
/// Consumers should reference namespace QBrainAi.Client from package QBrainAI.Client.
/// </summary>
public static class FacadeMarker
{
    /// <summary>Gets the replacement package id.</summary>
    public const string ReplacementPackageId = "QBrainAI.Client";
}
