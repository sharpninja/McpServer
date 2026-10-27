namespace QBrainAi.Cqrs.Compatibility;

/// <summary>
/// TR-MCP-QBRAIN-003: Marker type for the SharpNinja.McpServer.Cqrs 1.x facade package.
/// Consumers should reference namespace QBrainAi.Cqrs from package QBrainAI.Cqrs.
/// </summary>
public static class FacadeMarker
{
    /// <summary>Gets the replacement package id.</summary>
    public const string ReplacementPackageId = "QBrainAI.Cqrs";
}
