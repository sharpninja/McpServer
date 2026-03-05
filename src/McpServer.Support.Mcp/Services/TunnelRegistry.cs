using System.Collections.Concurrent;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Registry managing all known tunnel providers. Providers are injected via DI
/// and the active provider (determined by <c>Mcp:Tunnel:Provider</c>) is started
/// automatically as part of the <see cref="IHostedService"/> lifecycle.
/// </summary>
public sealed class TunnelRegistry : IHostedService
{
    private readonly ConcurrentDictionary<string, TunnelEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<TunnelRegistry> _logger;

    /// <summary>Initializes a new instance of the <see cref="TunnelRegistry"/> class.</summary>
    /// <param name="providers">All tunnel provider singletons registered in DI.</param>
    /// <param name="options">Tunnel configuration determining which provider is active.</param>
    /// <param name="logger">Logger.</param>
    public TunnelRegistry(
        IEnumerable<ITunnelProvider> providers,
        IOptions<TunnelOptions> options,
        ILogger<TunnelRegistry> logger)
    {
        _logger = logger;
        var activeProvider = (options.Value.Provider ?? "").Trim().ToUpperInvariant();

        foreach (var provider in providers)
        {
            var enabled = provider.ProviderName.Equals(activeProvider, StringComparison.OrdinalIgnoreCase);
            _entries[provider.ProviderName] = new TunnelEntry(provider, enabled);
            _logger.LogInformation("Tunnel provider registered: {Provider}, Enabled={Enabled}", provider.ProviderName, enabled);
        }
    }

    /// <summary>Lists all registered tunnel providers with their current state.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Snapshot of all tunnel entries.</returns>
    public async Task<IReadOnlyList<TunnelInfo>> ListAsync(CancellationToken ct = default)
    {
        var results = new List<TunnelInfo>(_entries.Count);
        foreach (var (name, entry) in _entries)
        {
            var status = await entry.Provider.GetStatusAsync(ct).ConfigureAwait(false);
            results.Add(new TunnelInfo(name, entry.Enabled, status.IsRunning, status.PublicUrl, status.Error));
        }

        return results;
    }

    /// <summary>Gets info for a single provider.</summary>
    /// <param name="providerName">Provider name (case-insensitive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tunnel info, or <c>null</c> if not registered.</returns>
    public async Task<TunnelInfo?> GetAsync(string providerName, CancellationToken ct = default)
    {
        if (!_entries.TryGetValue(providerName, out var entry))
            return null;

        var status = await entry.Provider.GetStatusAsync(ct).ConfigureAwait(false);
        return new TunnelInfo(entry.Provider.ProviderName, entry.Enabled, status.IsRunning, status.PublicUrl, status.Error);
    }

    /// <summary>Enables a provider (does not start it).</summary>
    /// <returns><c>true</c> if the provider was found and enabled.</returns>
    public bool Enable(string providerName)
    {
        if (!_entries.TryGetValue(providerName, out var entry))
            return false;

        _entries[providerName] = entry with { Enabled = true };
        _logger.LogInformation("Tunnel provider enabled: {Provider}", providerName);
        return true;
    }

    /// <summary>Disables a provider. If running, it is stopped first.</summary>
    /// <param name="providerName">Provider name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the provider was found and disabled.</returns>
    public async Task<bool> DisableAsync(string providerName, CancellationToken ct = default)
    {
        if (!_entries.TryGetValue(providerName, out var entry))
            return false;

        var status = await entry.Provider.GetStatusAsync(ct).ConfigureAwait(false);
        if (status.IsRunning)
            await entry.Provider.StopAsync(ct).ConfigureAwait(false);

        _entries[providerName] = entry with { Enabled = false };
        _logger.LogInformation("Tunnel provider disabled: {Provider}", providerName);
        return true;
    }

    /// <summary>Starts a provider. Must be enabled first.</summary>
    /// <param name="providerName">Provider name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tunnel info after start, or <c>null</c> if not found.</returns>
    public async Task<TunnelInfo?> StartAsync(string providerName, CancellationToken ct = default)
    {
        if (!_entries.TryGetValue(providerName, out var entry))
            return null;

        if (!entry.Enabled)
        {
            var current = await entry.Provider.GetStatusAsync(ct).ConfigureAwait(false);
            return new TunnelInfo(entry.Provider.ProviderName, false, current.IsRunning, current.PublicUrl,
                "Provider is disabled. Enable it first.");
        }

        var pre = await entry.Provider.GetStatusAsync(ct).ConfigureAwait(false);
        if (!pre.IsRunning)
            await entry.Provider.StartAsync(ct).ConfigureAwait(false);

        var post = await entry.Provider.GetStatusAsync(ct).ConfigureAwait(false);
        return new TunnelInfo(entry.Provider.ProviderName, true, post.IsRunning, post.PublicUrl, post.Error);
    }

    /// <summary>Stops a running provider.</summary>
    /// <param name="providerName">Provider name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tunnel info after stop, or <c>null</c> if not found.</returns>
    public async Task<TunnelInfo?> StopAsync(string providerName, CancellationToken ct = default)
    {
        if (!_entries.TryGetValue(providerName, out var entry))
            return null;

        await entry.Provider.StopAsync(ct).ConfigureAwait(false);
        var post = await entry.Provider.GetStatusAsync(ct).ConfigureAwait(false);
        return new TunnelInfo(entry.Provider.ProviderName, entry.Enabled, post.IsRunning, post.PublicUrl, post.Error);
    }

    /// <summary>Restarts a provider (stop then start). Must be enabled.</summary>
    /// <param name="providerName">Provider name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tunnel info after restart, or <c>null</c> if not found.</returns>
    public async Task<TunnelInfo?> RestartAsync(string providerName, CancellationToken ct = default)
    {
        if (!_entries.TryGetValue(providerName, out var entry))
            return null;

        await entry.Provider.StopAsync(ct).ConfigureAwait(false);

        if (!entry.Enabled)
        {
            var current = await entry.Provider.GetStatusAsync(ct).ConfigureAwait(false);
            return new TunnelInfo(entry.Provider.ProviderName, false, current.IsRunning, current.PublicUrl,
                "Provider is disabled. Enable it first.");
        }

        await entry.Provider.StartAsync(ct).ConfigureAwait(false);
        var post = await entry.Provider.GetStatusAsync(ct).ConfigureAwait(false);
        return new TunnelInfo(entry.Provider.ProviderName, true, post.IsRunning, post.PublicUrl, post.Error);
    }

    /// <inheritdoc />
    /// <summary>Starts all enabled providers as part of the hosted service lifecycle.</summary>
    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        await StartEnabledAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <summary>Stops all running providers as part of the hosted service lifecycle.</summary>
    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        await StopAllAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Starts all enabled providers. Called during application startup.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task StartEnabledAsync(CancellationToken ct = default)
    {
        foreach (var (name, entry) in _entries)
        {
            if (!entry.Enabled)
                continue;

            try
            {
                await entry.Provider.StartAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("Tunnel provider started: {Provider}", name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to start tunnel provider: {Provider}", name);
            }
        }
    }

    /// <summary>Stops all running providers. Called during application shutdown.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task StopAllAsync(CancellationToken ct = default)
    {
        foreach (var (name, entry) in _entries)
        {
            try
            {
                var status = await entry.Provider.GetStatusAsync(ct).ConfigureAwait(false);
                if (status.IsRunning)
                {
                    await entry.Provider.StopAsync(ct).ConfigureAwait(false);
                    _logger.LogInformation("Tunnel provider stopped: {Provider}", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error stopping tunnel provider: {Provider}", name);
            }
        }
    }

    private sealed record TunnelEntry(ITunnelProvider Provider, bool Enabled);
}

/// <summary>Snapshot of a tunnel provider's state.</summary>
/// <param name="Provider">Provider name (e.g. <c>ngrok</c>, <c>cloudflare</c>, <c>frp</c>).</param>
/// <param name="Enabled">Whether the provider is enabled for use.</param>
/// <param name="IsRunning">Whether the tunnel process is currently running.</param>
/// <param name="PublicUrl">The public URL assigned by the provider, if available.</param>
/// <param name="Error">Error message, if any.</param>
public sealed record TunnelInfo(string Provider, bool Enabled, bool IsRunning, string? PublicUrl, string? Error);
