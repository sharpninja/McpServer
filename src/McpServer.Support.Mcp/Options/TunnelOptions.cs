namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Configuration for the optional tunnel provider. Bound from <c>Mcp:Tunnel</c>.
/// When <see cref="Provider"/> is empty (default), no tunnel is started.
/// </summary>
public sealed class TunnelOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:Tunnel";

    /// <summary>Provider key: <c>ngrok</c>, <c>cloudflare</c>, <c>frp</c>, or empty (disabled).</summary>
    public string Provider { get; set; } = "";

    /// <summary>Local port to tunnel (defaults to the main server port).</summary>
    public int Port { get; set; } = 7147;

    /// <summary>ngrok-specific options.</summary>
    public NgrokTunnelOptions Ngrok { get; set; } = new();

    /// <summary>Cloudflare Tunnel-specific options.</summary>
    public CloudflareTunnelOptions Cloudflare { get; set; } = new();

    /// <summary>FRP (Fast Reverse Proxy) client options.</summary>
    public FrpTunnelOptions Frp { get; set; } = new();
}

/// <summary>Options for the ngrok tunnel provider.</summary>
public sealed class NgrokTunnelOptions
{
    /// <summary>Optional ngrok subdomain (requires paid plan).</summary>
    public string? Subdomain { get; set; }

    /// <summary>ngrok auth token. Can also be set via <c>ngrok config add-authtoken</c>.</summary>
    public string? AuthToken { get; set; }

    /// <summary>ngrok region (e.g. <c>us</c>, <c>eu</c>, <c>ap</c>).</summary>
    public string? Region { get; set; }
}

/// <summary>Options for the Cloudflare Tunnel provider.</summary>
public sealed class CloudflareTunnelOptions
{
    /// <summary>Named tunnel identifier (requires <c>cloudflared tunnel create</c>).</summary>
    public string? TunnelName { get; set; }

    /// <summary>Custom hostname for the tunnel (requires DNS setup).</summary>
    public string? Hostname { get; set; }
}

/// <summary>Options for the FRP (Fast Reverse Proxy) client tunnel provider.</summary>
public sealed class FrpTunnelOptions
{
    /// <summary>FRP server address (hostname or IP).</summary>
    public string ServerAddress { get; set; } = "127.0.0.1";

    /// <summary>FRP server bind port.</summary>
    public int ServerPort { get; set; } = 7000;

    /// <summary>Authentication token shared with the FRP server.</summary>
    public string? Token { get; set; }

    /// <summary>Subdomain for HTTP proxy (requires server <c>subdomainHost</c> config).</summary>
    public string? Subdomain { get; set; }

    /// <summary>Custom domain for HTTP proxy.</summary>
    public string? CustomDomain { get; set; }
}
