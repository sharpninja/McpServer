using System.Collections.Concurrent;

namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-013, TR-MCP-MT-004: In-memory store for last sync run result, workspace-keyed.
/// </summary>
public sealed class SyncStatusStore : ISyncStatusStore
{
    private volatile SyncRunResult? _last;
    private readonly ConcurrentDictionary<string, SyncRunResult> _perWorkspace = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public SyncRunResult? GetLast() => _last;

    /// <inheritdoc />
    public void SetLast(SyncRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _last = result;
    }

    /// <inheritdoc />
    public SyncRunResult? GetLast(string workspaceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        return _perWorkspace.TryGetValue(workspaceId, out var r) ? r : null;
    }

    /// <inheritdoc />
    public void SetLast(string workspaceId, SyncRunResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        ArgumentNullException.ThrowIfNull(result);
        _perWorkspace[workspaceId] = result;
    }
}
