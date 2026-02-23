using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>Client for sync endpoints (/mcp/sync).</summary>
public sealed class SyncClient : McpClientBase
{
    /// <summary>Initializes a new instance of <see cref="SyncClient"/>.</summary>
    public SyncClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    /// <summary>Trigger a full ingestion sync.</summary>
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
