namespace FWH.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-013: In-memory store for last sync run result.
/// </summary>
public sealed class SyncStatusStore : ISyncStatusStore
{
    private volatile SyncRunResult? _last;

    /// <inheritdoc />
    public SyncRunResult? GetLast() => _last;

    /// <inheritdoc />
    public void SetLast(SyncRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _last = result;
    }
}
