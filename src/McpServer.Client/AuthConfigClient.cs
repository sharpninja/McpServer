using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for public auth configuration endpoint (<c>/auth/config</c>).
/// </summary>
public sealed class AuthConfigClient : McpClientBase
{
    /// <inheritdoc />
    public AuthConfigClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal AuthConfigClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Gets public OIDC configuration metadata.</summary>
    public async Task<AuthConfigResponse> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<AuthConfigResponse>("auth/config", cancellationToken);
    }
}
