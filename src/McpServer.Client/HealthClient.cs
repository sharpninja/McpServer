using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for the server health endpoint (<c>/health</c>).
/// </summary>
/// <seealso cref="McpServerClient.Health"/>
public sealed class HealthClient : McpClientBase
{
    /// <inheritdoc />
    public HealthClient(HttpClient http, McpServerClientOptions options)
        : base(http, options)
    {
    }

    internal HealthClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
    }

    /// <summary>
    /// Gets the current server health payload.
    /// </summary>
    public async Task<HealthCheckResult> GetAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<HealthCheckResult>("health", cancellationToken);
    }
}
