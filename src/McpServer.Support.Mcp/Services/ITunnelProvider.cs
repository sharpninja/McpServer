namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Strategy interface for tunnel providers. Implementations manage the lifecycle
/// of an external tunnel process (ngrok, cloudflared, frpc) as an <see cref="IHostedService"/>.
/// </summary>
public interface ITunnelProvider : IHostedService
{
    /// <summary>Provider name (e.g. <c>ngrok</c>, <c>cloudflare</c>, <c>frp</c>).</summary>
    string ProviderName { get; }

    /// <summary>Get the current tunnel status including the public URL.</summary>
    Task<TunnelStatus> GetStatusAsync(CancellationToken ct = default);
}

/// <summary>Tunnel provider status.</summary>
/// <param name="IsRunning">Whether the tunnel process is currently running.</param>
/// <param name="PublicUrl">The public URL assigned by the tunnel provider, if available.</param>
/// <param name="Error">Error message if the tunnel failed to start or crashed.</param>
public sealed record TunnelStatus(bool IsRunning, string? PublicUrl = null, string? Error = null);
