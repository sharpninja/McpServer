using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>Director adapter for <see cref="ITunnelApiClient"/> backed by <see cref="McpServer.Client.McpServerClient"/>.</summary>
internal sealed class TunnelApiClientAdapter : ITunnelApiClient
{
    private readonly DirectorMcpContext _context;

    /// <summary>Initializes a new instance of the <see cref="TunnelApiClientAdapter"/> class.</summary>
    public TunnelApiClientAdapter(DirectorMcpContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<TunnelListSnapshot> ListAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var providers = await client.Tunnel.ListAsync(cancellationToken).ConfigureAwait(false);
        return new TunnelListSnapshot(providers.Select(Map).ToList());
    }

    /// <inheritdoc />
    public async Task<TunnelProviderSnapshot> EnableAsync(string providerName, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var info = await client.Tunnel.EnableAsync(providerName, cancellationToken).ConfigureAwait(false);
        return Map(info);
    }

    /// <inheritdoc />
    public async Task<TunnelProviderSnapshot> DisableAsync(string providerName, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var info = await client.Tunnel.DisableAsync(providerName, cancellationToken).ConfigureAwait(false);
        return Map(info);
    }

    /// <inheritdoc />
    public async Task<TunnelProviderSnapshot> StartAsync(string providerName, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var info = await client.Tunnel.StartAsync(providerName, cancellationToken).ConfigureAwait(false);
        return Map(info);
    }

    /// <inheritdoc />
    public async Task<TunnelProviderSnapshot> StopAsync(string providerName, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var info = await client.Tunnel.StopAsync(providerName, cancellationToken).ConfigureAwait(false);
        return Map(info);
    }

    /// <inheritdoc />
    public async Task<TunnelProviderSnapshot> RestartAsync(string providerName, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var info = await client.Tunnel.RestartAsync(providerName, cancellationToken).ConfigureAwait(false);
        return Map(info);
    }

    private static TunnelProviderSnapshot Map(TunnelProviderInfo info)
        => new(info.Provider, info.Enabled, info.IsRunning, info.PublicUrl, info.Error);
}
