namespace McpServer.Support.Mcp.Options;

/// <summary>
/// FR-MCP-077: Configuration for server federation. Bound from <c>Mcp:Federation</c>.
/// When <see cref="Enabled"/> is <c>false</c> (default), no proxying is performed.
/// </summary>
public sealed class FederationOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:Federation";

    /// <summary>Whether federation proxying is active. Defaults to <c>false</c>.</summary>
    public bool Enabled { get; set; }

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
