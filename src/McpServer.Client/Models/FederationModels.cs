using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>FR-MCP-077: Full federation status snapshot returned by the management API.</summary>
public sealed class FederationStatusResponse
{
    /// <summary>Whether federation is globally enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Effective federation role after compatibility inference.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "Standalone";

    /// <summary>Configured federation role before compatibility inference.</summary>
    [JsonPropertyName("configuredRole")]
    public string ConfiguredRole { get; set; } = "Standalone";

    /// <summary>Hub base URL configured for LocalProxy mode.</summary>
    [JsonPropertyName("hubBaseUrl")]
    public string? HubBaseUrl { get; set; }

    /// <summary>Stable local proxy identifier.</summary>
    [JsonPropertyName("proxyId")]
    public string? ProxyId { get; set; }

    /// <summary>Whether an enrollment token is configured. The token value is never returned.</summary>
    [JsonPropertyName("hasEnrollmentToken")]
    public bool HasEnrollmentToken { get; set; }

    /// <summary>Registered federation targets.</summary>
    [JsonPropertyName("targets")]
    public IReadOnlyList<FederationTargetInfo> Targets { get; set; } = [];

    /// <summary>Per-workspace routing rules.</summary>
    [JsonPropertyName("workspaceRoutes")]
    public IReadOnlyList<WorkspaceRouteInfo> WorkspaceRoutes { get; set; } = [];

    /// <summary>Number of enrolled proxies known by the hub.</summary>
    [JsonPropertyName("proxyCount")]
    public int ProxyCount { get; set; }

    /// <summary>Number of proxy-hosted workspaces known by the hub.</summary>
    [JsonPropertyName("hostedWorkspaceCount")]
    public int HostedWorkspaceCount { get; set; }

    /// <summary>Number of queued operations waiting for replay or acknowledgement.</summary>
    [JsonPropertyName("queueDepth")]
    public int QueueDepth { get; set; }

    /// <summary>Number of open conflicts.</summary>
    [JsonPropertyName("conflictCount")]
    public int ConflictCount { get; set; }
}

/// <summary>FR-MCP-077: Information about a registered federation target.</summary>
public sealed class FederationTargetInfo
{
    /// <summary>Unique target name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>Base URL of the remote MCP server.</summary>
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = "";

    /// <summary>Whether an API key is configured for this target.</summary>
    [JsonPropertyName("hasApiKey")]
    public bool HasApiKey { get; set; }

    /// <summary>Whether this target is the global default.</summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }
}

/// <summary>FR-MCP-077: A workspace-specific routing rule.</summary>
public sealed class WorkspaceRouteInfo
{
    /// <summary>Absolute workspace path this rule applies to.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = "";

    /// <summary>Name of the federation target this workspace routes to.</summary>
    [JsonPropertyName("targetName")]
    public string TargetName { get; set; } = "";
}

/// <summary>FR-MCP-103: Proxy inventory row returned by the hub.</summary>
public sealed class FederationProxyInfo
{
    /// <summary>Stable proxy identifier.</summary>
    [JsonPropertyName("proxyId")]
    public string ProxyId { get; set; } = "";

    /// <summary>Human-readable proxy name.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>Federation role reported by the proxy.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "LocalProxy";

    /// <summary>Proxy callback base URL, if configured.</summary>
    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; set; }

    /// <summary>Current proxy status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "enrolled";

    /// <summary>UTC timestamp of the most recent heartbeat.</summary>
    [JsonPropertyName("lastHeartbeatUtc")]
    public DateTimeOffset? LastHeartbeatUtc { get; set; }

    /// <summary>Number of registered workspaces hosted by the proxy.</summary>
    [JsonPropertyName("workspaceCount")]
    public int WorkspaceCount { get; set; }
}

/// <summary>FR-MCP-103: Workspace inventory row returned by the hub.</summary>
public sealed class FederationWorkspaceInfo
{
    /// <summary>Hub-wide workspace identifier.</summary>
    [JsonPropertyName("globalWorkspaceId")]
    public string GlobalWorkspaceId { get; set; } = "";

    /// <summary>Proxy that hosts the workspace.</summary>
    [JsonPropertyName("proxyId")]
    public string ProxyId { get; set; } = "";

    /// <summary>Workspace display name.</summary>
    [JsonPropertyName("workspaceName")]
    public string? WorkspaceName { get; set; }

    /// <summary>Proxy-local absolute workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = "";

    /// <summary>Whether the workspace is enabled on the proxy.</summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    /// <summary>Proxy-reported workspace version.</summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>UTC timestamp when the workspace was last seen by the hub.</summary>
    [JsonPropertyName("lastSeenUtc")]
    public DateTimeOffset LastSeenUtc { get; set; }
}

/// <summary>FR-MCP-103: Queue status projection for hub and proxy diagnostics.</summary>
public sealed class FederationQueueStatusResponse
{
    /// <summary>Optional proxy filter used for this status response.</summary>
    [JsonPropertyName("proxyId")]
    public string? ProxyId { get; set; }

    /// <summary>Number of operations waiting for replay or acknowledgement.</summary>
    [JsonPropertyName("queueDepth")]
    public int QueueDepth { get; set; }

    /// <summary>Number of operations currently in conflict.</summary>
    [JsonPropertyName("conflictCount")]
    public int ConflictCount { get; set; }

    /// <summary>Number of unacknowledged hub fanout rows.</summary>
    [JsonPropertyName("fanoutDepth")]
    public int FanoutDepth { get; set; }
}

/// <summary>FR-MCP-103: Conflict inventory row.</summary>
public sealed class FederationConflictInfo
{
    /// <summary>Conflict identifier.</summary>
    [JsonPropertyName("conflictId")]
    public string ConflictId { get; set; } = "";

    /// <summary>Operation that caused the conflict.</summary>
    [JsonPropertyName("operationId")]
    public string OperationId { get; set; } = "";

    /// <summary>Proxy that submitted the conflicting operation.</summary>
    [JsonPropertyName("proxyId")]
    public string ProxyId { get; set; } = "";

    /// <summary>Mutable state domain where the conflict occurred.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = "";

    /// <summary>Domain-specific resource identifier.</summary>
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; set; }

    /// <summary>Proxy-observed version.</summary>
    [JsonPropertyName("proxyVersion")]
    public string? ProxyVersion { get; set; }

    /// <summary>Hub-authoritative version.</summary>
    [JsonPropertyName("hubVersion")]
    public string? HubVersion { get; set; }

    /// <summary>Resolution status.</summary>
    [JsonPropertyName("resolutionStatus")]
    public string ResolutionStatus { get; set; } = "open";

    /// <summary>UTC timestamp when the conflict was created.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>FR-MCP-103: Adapter coverage row used by diagnostics.</summary>
public sealed class FederationStateAdapterCoverage
{
    /// <summary>Mutable state domain.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = "";

    /// <summary>Whether an adapter is registered for the domain.</summary>
    [JsonPropertyName("covered")]
    public bool Covered { get; set; }

    /// <summary>Whether the domain is intentionally exempt from replication.</summary>
    [JsonPropertyName("localOnly")]
    public bool LocalOnly { get; set; }
}

/// <summary>FR-MCP-077: Request to add a federation target.</summary>
public sealed class FederationTargetAddRequest
{
    /// <summary>Unique name for the target.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>Base URL of the remote MCP server.</summary>
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = "";

    /// <summary>Optional API key for authenticating with the remote server.</summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }
}

/// <summary>FR-MCP-077: Request to add or update a workspace routing rule.</summary>
public sealed class WorkspaceRouteRequest
{
    /// <summary>Absolute workspace path to route.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = "";

    /// <summary>Name of the federation target to route to.</summary>
    [JsonPropertyName("targetName")]
    public string TargetName { get; set; } = "";
}

/// <summary>FR-MCP-077: Result of a tunnel-based target auto-discovery operation.</summary>
public sealed class TunnelDiscoveryResult
{
    /// <summary>Number of new targets registered in this call.</summary>
    [JsonPropertyName("discovered")]
    public int Discovered { get; set; }

    /// <summary>The newly registered target info objects.</summary>
    [JsonPropertyName("targets")]
    public IReadOnlyList<FederationTargetInfo> Targets { get; set; } = [];
}

/// <summary>FR-MCP-077: Connection credentials for a federated peer.</summary>
public sealed class FederationConnectionInfo
{
    /// <summary>This server's local base URL.</summary>
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = "";

    /// <summary>TCP port the server is listening on.</summary>
    [JsonPropertyName("port")]
    public int Port { get; set; }

    /// <summary>Full-access workspace token.</summary>
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = "";
}

/// <summary>FR-MCP-085: Request body for the federation push endpoint.</summary>
public sealed class FederationPushRequest
{
    /// <summary>Optional filter for which data types to push. Empty means push all.</summary>
    [JsonPropertyName("types")]
    public IReadOnlyList<string>? Types { get; set; }
}

/// <summary>FR-MCP-085: Result of a federation push operation.</summary>
public sealed class FederationPushResult
{
    /// <summary>Number of items successfully pushed.</summary>
    [JsonPropertyName("succeeded")]
    public int Succeeded { get; set; }

    /// <summary>Number of items that failed to push.</summary>
    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    /// <summary>Error messages from failed items.</summary>
    [JsonPropertyName("errors")]
    public IReadOnlyList<string> Errors { get; set; } = [];
}
