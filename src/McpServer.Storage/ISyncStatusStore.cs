namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-CORE-013, TR-MCP-MT-004: Stores last sync run result per workspace for sync.status endpoint.
/// </summary>
public interface ISyncStatusStore
{
    /// <summary>Gets the last sync result (or null if never run).</summary>
    /// <returns>The last sync run result, or <see langword="null"/> if no sync has been run.</returns>
    SyncRunResult? GetLast();

    /// <summary>Sets the last sync result.</summary>
    /// <param name="result">The sync run result to store.</param>
    void SetLast(SyncRunResult result);

    /// <summary>Gets the last sync result for a specific workspace.</summary>
    /// <param name="workspaceId">Normalized workspace identifier.</param>
    /// <returns>The last sync run result for the workspace, or <see langword="null"/>.</returns>
    SyncRunResult? GetLast(string workspaceId);

    /// <summary>Sets the last sync result for a specific workspace.</summary>
    /// <param name="workspaceId">Normalized workspace identifier.</param>
    /// <param name="result">The sync run result to store.</param>
    void SetLast(string workspaceId, SyncRunResult result);
}
