using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>FR-MCP-077: Full federation status snapshot returned by the management API.</summary>
public sealed class FederationStatusResponse
{
    /// <summary>Whether federation is globally enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Registered federation targets.</summary>
    [JsonPropertyName("targets")]
    public IReadOnlyList<FederationTargetInfo> Targets { get; set; } = [];

    /// <summary>Per-workspace routing rules.</summary>
    [JsonPropertyName("workspaceRoutes")]
    public IReadOnlyList<WorkspaceRouteInfo> WorkspaceRoutes { get; set; } = [];
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
