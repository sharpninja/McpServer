using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for public auth configuration endpoint (<c>/auth/config</c>).
/// This endpoint is unauthenticated, so requests bypass the base class auth check.
/// </summary>
public sealed class AuthConfigClient : McpClientBase
{
    private readonly HttpClient _http;
    private readonly string _scheme;
    private readonly string _host;

    /// <inheritdoc />
    public AuthConfigClient(HttpClient http, McpServerClientOptions options)
        : base(http, options)
    {
        _http = http;
        _scheme = options.BaseUrl.Scheme;
        _host = options.BaseUrl.Host;
    }

    internal AuthConfigClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
        _http = http;
        _scheme = options.BaseUrl.Scheme;
        _host = options.BaseUrl.Host;
    }

    /// <summary>Gets public OIDC configuration metadata. No authentication required.</summary>
    public async Task<AuthConfigResponse> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"{_scheme}://{_host}:{Port}/auth/config");
        using var response = await _http.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<AuthConfigResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}
