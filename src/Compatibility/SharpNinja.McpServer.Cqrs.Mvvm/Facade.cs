namespace QBrainAi.Cqrs.Mvvm.Compatibility;

/// <summary>
/// TR-MCP-QBRAIN-003: Marker type for the SharpNinja.McpServer.Cqrs.Mvvm 1.x facade package.
/// Consumers should reference namespace QBrainAi.Cqrs.Mvvm from package QBrainAI.Cqrs.Mvvm.
/// </summary>
public static class FacadeMarker
{
    /// <summary>Gets the replacement package id.</summary>
    public const string ReplacementPackageId = "QBrainAI.Cqrs.Mvvm";
}
