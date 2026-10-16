namespace QBrainAi.Repl.Core.Compatibility;

/// <summary>
/// TR-MCP-QBRAIN-003: Marker type for the SharpNinja.McpServer.Repl.Core 1.x facade package.
/// Consumers should reference namespace QBrainAi.Repl.Core from package QBrainAI.Repl.Core.
/// </summary>
public static class FacadeMarker
{
    /// <summary>Gets the replacement package id.</summary>
    public const string ReplacementPackageId = "QBrainAI.Repl.Core";
}
