using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for tunnel endpoints (<c>/mcp/tunnel</c>). Manages tunnel provider lifecycle:
/// list strategies, enable/disable, start, stop, restart, and status.
/// </summary>
/// <seealso cref="McpServerClient.Tunnel"/>
public sealed class TunnelClient : McpClientBase
{
    /// <inheritdoc />
    public TunnelClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    /// <summary>List all registered tunnel providers.</summary>
    public async Task<List<TunnelProviderInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<TunnelProviderInfo>>("mcp/tunnel/list", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Get the status of a specific tunnel provider.</summary>
    public async Task<TunnelProviderInfo> GetStatusAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TunnelProviderInfo>($"mcp/tunnel/{providerName}/status", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Enable a tunnel provider.</summary>
    public async Task<TunnelProviderInfo> EnableAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TunnelProviderInfo>($"mcp/tunnel/{providerName}/enable", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Disable a tunnel provider.</summary>
    public async Task<TunnelProviderInfo> DisableAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TunnelProviderInfo>($"mcp/tunnel/{providerName}/disable", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Start a tunnel provider.</summary>
    public async Task<TunnelProviderInfo> StartAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TunnelProviderInfo>($"mcp/tunnel/{providerName}/start", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Stop a tunnel provider.</summary>
    public async Task<TunnelProviderInfo> StopAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TunnelProviderInfo>($"mcp/tunnel/{providerName}/stop", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Restart a tunnel provider (stop then start).</summary>
    public async Task<TunnelProviderInfo> RestartAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TunnelProviderInfo>($"mcp/tunnel/{providerName}/restart", null, cancellationToken).ConfigureAwait(false);
    }
}
