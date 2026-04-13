namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-085: Result of a federation push operation indicating how many items
/// were successfully pushed to the remote target and any errors encountered.
/// </summary>
/// <param name="Succeeded">Number of items successfully pushed.</param>
/// <param name="Failed">Number of items that failed to push.</param>
/// <param name="Errors">Error messages for failed items.</param>
public sealed record FederationPushResult(int Succeeded, int Failed, IReadOnlyList<string> Errors);
