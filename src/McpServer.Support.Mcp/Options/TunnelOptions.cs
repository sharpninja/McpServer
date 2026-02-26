namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Configuration for the optional tunnel provider. Bound from <c>Mcp:Tunnel</c>.
/// When <see cref="Provider"/> is empty (default), no tunnel is started.
/// </summary>
public sealed class TunnelOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:Tunnel";

    /// <summary>Provider key: <c>ngrok</c>, <c>cloudflare</c>, or empty (disabled).</summary>
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
    /// <summary>Enable ngrok tunnel configuration (selection is still controlled by <c>Mcp:Tunnel:Provider</c>).</summary>
    public bool Enabled { get; set; }

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
    /// <summary>Enable Cloudflare tunnel configuration (selection is still controlled by <c>Mcp:Tunnel:Provider</c>).</summary>
    public bool Enabled { get; set; }

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

    /// <summary>
    /// Proxy type for the generated FRP proxy config. Supported values: <c>http</c>, <c>tcp</c>.
    /// </summary>
    public string ProxyType { get; set; } = "http";

    /// <summary>
    /// Optional remote port for FRP TCP mode. When omitted in <c>tcp</c> mode, the local
    /// tunnel port (<c>Mcp:Tunnel:Port</c>) is used as the remote port.
    /// </summary>
    public int? RemotePort { get; set; }

    /// <summary>
    /// Optional start of a 1:1 FRP TCP port range mapping (local and remote ports match).
    /// Example: <c>7147</c> with <see cref="TcpPortRangeEnd"/> <c>7160</c>.
    /// </summary>
    public int? TcpPortRangeStart { get; set; }

    /// <summary>
    /// Optional end of a 1:1 FRP TCP port range mapping (local and remote ports match).
    /// Requires <see cref="TcpPortRangeStart"/>.
    /// </summary>
    public int? TcpPortRangeEnd { get; set; }

    /// <summary>
    /// When <c>true</c> and <see cref="ProxyType"/> is <c>tcp</c> with no explicit
    /// <see cref="RemotePort"/> or <see cref="TcpPortRangeStart"/>/<see cref="TcpPortRangeEnd"/>,
    /// the server will auto-map the primary MCP port plus enabled workspace ports and
    /// periodically restart <c>frpc</c> when that port set changes.
    /// </summary>
    public bool AutoMapWorkspacePorts { get; set; } = true;

    /// <summary>
    /// Reconcile interval (seconds) for dynamic FRP TCP auto-mapping refresh.
    /// Only used when <see cref="AutoMapWorkspacePorts"/> is enabled.
    /// </summary>
    public int ReconcileIntervalSeconds { get; set; } = 10;

    /// <summary>Authentication token shared with the FRP server.</summary>
    public string? Token { get; set; }

    /// <summary>Subdomain for HTTP proxy (requires server <c>subdomainHost</c> config).</summary>
    public string? Subdomain { get; set; }

    /// <summary>Custom domain for HTTP proxy.</summary>
    public string? CustomDomain { get; set; }

    /// <summary>
    /// Optional explicit public base URL used for status reporting (for example a Railway domain).
    /// When set, this takes precedence over <see cref="CustomDomain"/> and <see cref="Subdomain"/>.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Startup wait time before considering the tunnel process healthy. Used to detect early
    /// <c>frpc</c> startup failures and report a clear error instead of optimistic success.
    /// </summary>
    public int StartupTimeoutSeconds { get; set; } = 5;
}
