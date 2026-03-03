namespace McpServer.UI.Core.Authorization;

/// <summary>
/// Logical MCP administration areas used for UI navigation and authorization.
/// </summary>
public enum McpArea
{
    /// <summary>Health/status checks for the active MCP server.</summary>
    Health,

    /// <summary>Workspace registration and lifecycle management.</summary>
    Workspaces,

    /// <summary>Workspace policy/compliance configuration.</summary>
    Policy,

    /// <summary>Agent definitions, workspace agents, and related events.</summary>
    Agents,

    /// <summary>TODO management and prompt helpers.</summary>
    Todo,

    /// <summary>Session log query and submission tooling.</summary>
    SessionLogs,

    /// <summary>Local CQRS dispatcher log history captured by the Director process.</summary>
    DispatcherLogs,

    /// <summary>Context search, pack, sources, and index operations.</summary>
    Context,

    /// <summary>Repository file browse/read/write operations.</summary>
    Repo,

    /// <summary>Tool registry search and tool/bucket management.</summary>
    ToolRegistry,

    /// <summary>GitHub issues, pull requests, labels, and sync flows.</summary>
    GitHub,

    /// <summary>Live event stream viewer.</summary>
    Events,

    /// <summary>Diagnostic endpoints and snapshots.</summary>
    Diagnostic,

    /// <summary>Workspace auth configuration inspection.</summary>
    AuthConfig,

    /// <summary>Tunnel provider lifecycle management.</summary>
    Tunnels,

    /// <summary>Prompt template registry management.</summary>
    Templates,
}
