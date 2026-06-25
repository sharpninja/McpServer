using System;
using System.Net.Http;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly string _scheme;
    private readonly string _host;

    /// <inheritdoc />
    public HealthClient(HttpClient http, McpServerClientOptions options)
        : base(http, options)
    {
        _http = http;
        _scheme = options.BaseUrl.Scheme;
        _host = options.BaseUrl.Host;
    }

    internal HealthClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
        _http = http;
        _scheme = options.BaseUrl.Scheme;
        _host = options.BaseUrl.Host;
    }

    /// <summary>
    /// Gets the current server health payload.
    /// </summary>
    public async Task<HealthCheckResult> GetAsync(CancellationToken cancellationToken = default)
    {
        return await GetPublicAsync<HealthCheckResult>("health", cancellationToken);
    }

    /// <summary>
    /// Gets the liveness health payload from <c>/alive</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Liveness health payload.</returns>
    public async Task<HealthCheckResult> GetAliveAsync(CancellationToken cancellationToken = default)
    {
        return await GetPublicAsync<HealthCheckResult>("alive", cancellationToken);
    }

    /// <summary>
    /// Gets the readiness health payload from <c>/ready</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Readiness health payload.</returns>
    public async Task<HealthCheckResult> GetReadyAsync(CancellationToken cancellationToken = default)
    {
        return await GetPublicAsync<HealthCheckResult>("ready", cancellationToken);
    }

    /// <summary>
    /// Gets server startup timestamp diagnostics from <c>/server-startup-utc</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server startup diagnostics.</returns>
    public async Task<ServerStartupResult> GetServerStartupAsync(CancellationToken cancellationToken = default)
    {
        return await GetPublicAsync<ServerStartupResult>("server-startup-utc", cancellationToken);
    }

    /// <summary>
    /// Gets marker-file timestamp diagnostics for a repository path.
    /// </summary>
    /// <param name="repoPath">Repository path to inspect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Marker-file timestamp diagnostics.</returns>
    public async Task<MarkerFileTimestampResult> GetMarkerFileTimestampAsync(
        string repoPath,
        CancellationToken cancellationToken = default)
    {
        return await GetPublicAsync<MarkerFileTimestampResult>(
            $"marker-file-timestamp?repoPath={Uri.EscapeDataString(repoPath)}",
            cancellationToken);
    }

    private async Task<T> GetPublicAsync<T>(string path, CancellationToken cancellationToken)
    {
        var uri = new Uri($"{_scheme}://{_host}:{Port}/{path.TrimStart('/')}");
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(true);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);

        if (!response.IsSuccessStatusCode)
        {
            throw new McpServerException(
                $"HTTP {(int)response.StatusCode}: {content}",
                (int)response.StatusCode);
        }

        return JsonSerializer.Deserialize<T>(content, s_jsonOptions)
            ?? throw new McpServerException("Response deserialized to null.", (int)response.StatusCode);
    }
}
