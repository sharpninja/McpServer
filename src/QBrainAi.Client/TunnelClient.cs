using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>
/// Client for tunnel endpoints (<c>/qbrainai/tunnel</c>). Manages tunnel provider lifecycle:
/// list strategies, enable/disable, start, stop, restart, and status.
/// </summary>
/// <seealso cref="QBrainAiClient.Tunnel"/>
public sealed class TunnelClient : McpClientBase
{
    /// <inheritdoc />
    public TunnelClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options) { }

    internal TunnelClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>List all registered tunnel providers.</summary>
    public async Task<List<TunnelProviderInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<TunnelProviderInfo>>("qbrainai/tunnel/list", cancellationToken);
    }

    /// <summary>Get the status of a specific tunnel provider.</summary>
    public async Task<TunnelProviderInfo> GetStatusAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TunnelProviderInfo>($"qbrainai/tunnel/{providerName}/status", cancellationToken);
    }

    /// <summary>Enable a tunnel provider.</summary>
    public async Task<TunnelProviderInfo> EnableAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TunnelProviderInfo>($"qbrainai/tunnel/{providerName}/enable", null, cancellationToken);
    }

    /// <summary>Disable a tunnel provider.</summary>
    public async Task<TunnelProviderInfo> DisableAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TunnelProviderInfo>($"qbrainai/tunnel/{providerName}/disable", null, cancellationToken);
    }

    /// <summary>Start a tunnel provider.</summary>
    public async Task<TunnelProviderInfo> StartAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TunnelProviderInfo>($"qbrainai/tunnel/{providerName}/start", null, cancellationToken);
    }

    /// <summary>Stop a tunnel provider.</summary>
    public async Task<TunnelProviderInfo> StopAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TunnelProviderInfo>($"qbrainai/tunnel/{providerName}/stop", null, cancellationToken);
    }

    /// <summary>Restart a tunnel provider (stop then start).</summary>
    public async Task<TunnelProviderInfo> RestartAsync(string providerName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TunnelProviderInfo>($"qbrainai/tunnel/{providerName}/restart", null, cancellationToken);
    }
}
