using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for sync endpoints (<c>/mcp/sync</c>). Triggers full ingestion runs (repo files,
/// session logs, external docs) and retrieves the current sync status.
/// </summary>
/// <seealso cref="McpServerClient.Sync"/>
public sealed class SyncClient : McpClientBase
{
    /// <inheritdoc />
    public SyncClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal SyncClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Trigger a full ingestion sync (repo, session logs, external docs).</summary>
    public async Task<SyncRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        return await PostAsync<SyncRunResult>("mcp/sync/run", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Get the current sync status.</summary>
    public async Task<SyncStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<SyncStatus>("mcp/sync/status", cancellationToken).ConfigureAwait(false);
    }
}
