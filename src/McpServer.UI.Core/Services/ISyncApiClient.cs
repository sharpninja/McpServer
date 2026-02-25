using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>Host-provided API abstraction for sync endpoints.</summary>
public interface ISyncApiClient
{
    /// <summary>Gets current sync status.</summary>
    Task<SyncStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs a sync operation.</summary>
    Task<SyncRunSummary> RunAsync(CancellationToken cancellationToken = default);
}
