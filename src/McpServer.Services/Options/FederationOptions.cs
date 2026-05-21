namespace McpServer.Support.Mcp.Options;

/// <summary>
/// FR-MCP-077 / FR-MCP-103: Configuration for server federation. Bound from
/// <c>Mcp:Federation</c>. When <see cref="Enabled"/> is <c>false</c> (default),
/// no request proxying is performed.
/// </summary>
public sealed class FederationOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:Federation";

    /// <summary>Whether federation proxying is active. Defaults to <c>false</c>.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Federation role for this server. Defaults to <see cref="FederationRole.Standalone"/>.
    /// Existing deployments that set <see cref="Enabled"/> and a target but omit this value
    /// are treated as <see cref="FederationRole.DirectProxy"/> for compatibility.
    /// </summary>
    public FederationRole Role { get; set; } = FederationRole.Standalone;

    /// <summary>
    /// Hub base URL used when <see cref="Role"/> is <see cref="FederationRole.LocalProxy"/>.
    /// Empty or <c>null</c> means the local proxy cannot route to a hub yet.
    /// </summary>
    public string? HubBaseUrl { get; set; }

    /// <summary>
    /// Stable local proxy identifier sent to the hub. If omitted, the local machine name
    /// is used at runtime.
    /// </summary>
    public string? ProxyId { get; set; }

    /// <summary>
    /// Enrollment token shared with the hub for initial proxy enrollment.
    /// This value is never returned by status endpoints.
    /// </summary>
    public string? EnrollmentToken { get; set; }

    /// <summary>
    /// Maximum number of sequential federation hops before returning
    /// <c>508 Loop Detected</c>. Defaults to <c>3</c>.
    /// </summary>
    public int MaxHops { get; set; } = 3;

    /// <summary>
    /// Name of the default federation target used when no workspace-specific route matches.
    /// Empty or <c>null</c> disables the global default.
    /// </summary>
    public string? DefaultTarget { get; set; }

    /// <summary>Named federation targets (remote MCP server base URLs).</summary>
    public List<FederationTargetOptions> Targets { get; set; } = [];

    /// <summary>Per-workspace routing overrides.</summary>
    public List<WorkspaceRouteOptions> WorkspaceRoutes { get; set; } = [];

    /// <summary>Durable local outbox settings for proxy-side queued writes.</summary>
    public FederationQueueOptions Queue { get; set; } = new();

    /// <summary>Hub/proxy synchronization settings.</summary>
    public FederationSyncOptions Sync { get; set; } = new();
}

/// <summary>FR-MCP-103: Supported federation topology roles.</summary>
public enum FederationRole
{
    /// <summary>Serve local workspaces only; do not route or accept proxy replication.</summary>
    Standalone = 0,

    /// <summary>Existing point-to-point federation mode using configured targets and workspace routes.</summary>
    DirectProxy = 1,

    /// <summary>Authoritative hub that tracks local proxies, global workspaces, operations, and conflicts.</summary>
    Hub = 2,

    /// <summary>Local server that proxies MCP traffic to a hub and queues writes during hub outages.</summary>
    LocalProxy = 3,
}

/// <summary>
/// FR-MCP-077: Configuration for a single named federation target.
/// </summary>
public sealed class FederationTargetOptions
{
    /// <summary>Unique name used to reference this target in routes and management API calls.</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Base URL of the remote MCP server (e.g. <c>http://localhost:7148</c> or
    /// <c>https://xxx.ngrok.io</c>). Must not have a trailing slash.
    /// </summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>
    /// Optional API key to use when forwarding requests to this target.
    /// When set, overrides the inbound <c>X-Api-Key</c> header value.
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// FR-MCP-077: Maps a specific workspace path to a named federation target.
/// </summary>
public sealed class WorkspaceRouteOptions
{
    /// <summary>Absolute path of the workspace to route (case-insensitive comparison).</summary>
    public string WorkspacePath { get; set; } = "";

    /// <summary>Name of the <see cref="FederationTargetOptions"/> to route this workspace to.</summary>
    public string TargetName { get; set; } = "";
}

/// <summary>FR-MCP-103: Configuration for local durable queued writes.</summary>
public sealed class FederationQueueOptions
{
    /// <summary>Whether mutating proxy requests may be queued locally when the hub is unavailable.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Maximum number of replay attempts before an operation is left pending for operator review.</summary>
    public int MaxReplayAttempts { get; set; } = 10;

    /// <summary>Maximum queued request body size, in bytes, retained in the local outbox.</summary>
    public int MaxBodyBytes { get; set; } = 1_048_576;
}

/// <summary>FR-MCP-103: Configuration for hub/proxy synchronization.</summary>
public sealed class FederationSyncOptions
{
    /// <summary>Expected heartbeat interval in seconds.</summary>
    public int HeartbeatSeconds { get; set; } = 30;

    /// <summary>Proxy-side replay polling interval in seconds.</summary>
    public int ReplayIntervalSeconds { get; set; } = 15;

    /// <summary>Hub-side fanout polling interval in seconds.</summary>
    public int FanoutIntervalSeconds { get; set; } = 15;
}
