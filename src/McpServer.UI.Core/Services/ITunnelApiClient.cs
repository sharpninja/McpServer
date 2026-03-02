using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>Host-provided API abstraction for tunnel endpoints.</summary>
public interface ITunnelApiClient
{
    /// <summary>Lists all registered tunnel providers.</summary>
    Task<TunnelListSnapshot> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Enables a tunnel provider.</summary>
    Task<TunnelProviderSnapshot> EnableAsync(string providerName, CancellationToken cancellationToken = default);

    /// <summary>Disables a tunnel provider.</summary>
    Task<TunnelProviderSnapshot> DisableAsync(string providerName, CancellationToken cancellationToken = default);

    /// <summary>Starts a tunnel provider.</summary>
    Task<TunnelProviderSnapshot> StartAsync(string providerName, CancellationToken cancellationToken = default);

    /// <summary>Stops a tunnel provider.</summary>
    Task<TunnelProviderSnapshot> StopAsync(string providerName, CancellationToken cancellationToken = default);

    /// <summary>Restarts a tunnel provider.</summary>
    Task<TunnelProviderSnapshot> RestartAsync(string providerName, CancellationToken cancellationToken = default);
}
