namespace QBrainAi.McpAgent.Compatibility;

/// <summary>
/// TR-MCP-QBRAIN-003: Marker type for the SharpNinja.McpServer.McpAgent 1.x facade package.
/// Consumers should reference namespace QBrainAi.McpAgent from package QBrainAI.McpAgent.
/// </summary>
public static class FacadeMarker
{
    /// <summary>Gets the replacement package id.</summary>
    public const string ReplacementPackageId = "QBrainAI.McpAgent";
}
