namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-013: Stores last sync run result for sync.status endpoint.
/// </summary>
public interface ISyncStatusStore
{
    /// <summary>Gets the last sync result (or null if never run).</summary>
    /// <returns>The last sync run result, or <see langword="null"/> if no sync has been run.</returns>
    SyncRunResult? GetLast();

    /// <summary>Sets the last sync result.</summary>
    /// <param name="result">The sync run result to store.</param>
    void SetLast(SyncRunResult result);
}
