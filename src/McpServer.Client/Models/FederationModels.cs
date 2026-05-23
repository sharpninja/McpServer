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

    /// <summary>Number of unacknowledged fanout rows.</summary>
    [JsonPropertyName("fanoutDepth")]
    public int FanoutDepth { get; set; }

    /// <summary>Current stale-read status. none means no stale read is currently reported.</summary>
    [JsonPropertyName("staleReadStatus")]
    public string StaleReadStatus { get; set; } = "none";
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

/// <summary>FR-MCP-103: Request body used by a LocalProxy to enroll with a hub.</summary>
public sealed class FederationEnrollmentRequest
{
    /// <summary>Stable proxy identifier. If empty, the hub may derive one from the display name.</summary>
    [JsonPropertyName("proxyId")]
    public string? ProxyId { get; set; }

    /// <summary>Human-readable proxy name, usually the machine name.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>Callback base URL for hub fanout or local execution envelopes.</summary>
    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; set; }

    /// <summary>Enrollment token shared with the hub.</summary>
    [JsonPropertyName("enrollmentToken")]
    public string? EnrollmentToken { get; set; }

    /// <summary>Opaque proxy metadata JSON.</summary>
    [JsonPropertyName("metadataJson")]
    public string? MetadataJson { get; set; }

    /// <summary>Workspaces hosted by the proxy at enrollment time.</summary>
    [JsonPropertyName("workspaces")]
    public IReadOnlyList<FederationWorkspaceRegistrationRequest> Workspaces { get; set; } = [];
}

/// <summary>FR-MCP-103: Response returned when a proxy enrollment succeeds.</summary>
public sealed class FederationEnrollmentResponse
{
    /// <summary>Accepted proxy identifier.</summary>
    [JsonPropertyName("proxyId")]
    public string ProxyId { get; set; } = "";

    /// <summary>Whether enrollment was accepted by the hub.</summary>
    [JsonPropertyName("accepted")]
    public bool Accepted { get; set; }

    /// <summary>Current hub UTC timestamp.</summary>
    [JsonPropertyName("serverTimeUtc")]
    public DateTimeOffset ServerTimeUtc { get; set; }

    /// <summary>Expected heartbeat interval in seconds.</summary>
    [JsonPropertyName("heartbeatSeconds")]
    public int HeartbeatSeconds { get; set; }
}

/// <summary>FR-MCP-103: Request body for proxy heartbeat updates.</summary>
public sealed class FederationHeartbeatRequest
{
    /// <summary>Proxy status label, usually online.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Opaque heartbeat metadata JSON.</summary>
    [JsonPropertyName("metadataJson")]
    public string? MetadataJson { get; set; }

    /// <summary>Current hosted workspace snapshot from the proxy.</summary>
    [JsonPropertyName("workspaces")]
    public IReadOnlyList<FederationWorkspaceRegistrationRequest> Workspaces { get; set; } = [];
}

/// <summary>FR-MCP-103: Heartbeat result returned to a proxy.</summary>
public sealed class FederationHeartbeatResponse
{
    /// <summary>Proxy identifier that was updated.</summary>
    [JsonPropertyName("proxyId")]
    public string ProxyId { get; set; } = "";

    /// <summary>UTC timestamp recorded by the hub.</summary>
    [JsonPropertyName("recordedAtUtc")]
    public DateTimeOffset RecordedAtUtc { get; set; }

    /// <summary>Current hub-side queued operation count for this proxy.</summary>
    [JsonPropertyName("queueDepth")]
    public int QueueDepth { get; set; }

    /// <summary>Current open conflict count for this proxy.</summary>
    [JsonPropertyName("conflictCount")]
    public int ConflictCount { get; set; }
}

/// <summary>FR-MCP-103: Workspace registration payload supplied by a proxy.</summary>
public sealed class FederationWorkspaceRegistrationRequest
{
    /// <summary>Optional hub-wide workspace identifier. If omitted, the hub derives one.</summary>
    [JsonPropertyName("globalWorkspaceId")]
    public string? GlobalWorkspaceId { get; set; }

    /// <summary>Workspace display name.</summary>
    [JsonPropertyName("workspaceName")]
    public string? WorkspaceName { get; set; }

    /// <summary>Proxy-local absolute workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = "";

    /// <summary>Whether the workspace is enabled on the proxy.</summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>Proxy-reported workspace version, when available.</summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>Opaque workspace metadata JSON.</summary>
    [JsonPropertyName("metadataJson")]
    public string? MetadataJson { get; set; }
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

    /// <summary>Whether the adapter implements local apply semantics for signed federation operations.</summary>
    [JsonPropertyName("applySupported")]
    public bool ApplySupported { get; set; }
}

/// <summary>FR-MCP-103: Hub outbox sync row delivered to a proxy.</summary>
public sealed class FederationSyncItem
{
    /// <summary>Monotonic hub sequence number.</summary>
    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }

    /// <summary>Operation identifier to synchronize.</summary>
    [JsonPropertyName("operationId")]
    public string OperationId { get; set; } = "";

    /// <summary>Proxy that originated the operation.</summary>
    [JsonPropertyName("proxyId")]
    public string ProxyId { get; set; } = "";

    /// <summary>Optional source operation ID used for echo suppression.</summary>
    [JsonPropertyName("sourceOperationId")]
    public string? SourceOperationId { get; set; }

    /// <summary>Hub-wide workspace identifier affected by the operation.</summary>
    [JsonPropertyName("globalWorkspaceId")]
    public string? GlobalWorkspaceId { get; set; }

    /// <summary>Mutable state domain affected by the operation.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = "";

    /// <summary>Domain-specific resource identifier.</summary>
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; set; }

    /// <summary>HTTP method for proxied REST operations.</summary>
    [JsonPropertyName("httpMethod")]
    public string? HttpMethod { get; set; }

    /// <summary>Request path for proxied REST operations.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>MCP method or tool name for transport operations.</summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary>Serialized operation headers.</summary>
    [JsonPropertyName("headersJson")]
    public string? HeadersJson { get; set; }

    /// <summary>Base64-encoded operation payload.</summary>
    [JsonPropertyName("bodyBase64")]
    public string? BodyBase64 { get; set; }

    /// <summary>Proxy-observed base version for optimistic conflict detection.</summary>
    [JsonPropertyName("baseVersion")]
    public string? BaseVersion { get; set; }

    /// <summary>Hub version associated with the operation.</summary>
    [JsonPropertyName("hubVersion")]
    public string? HubVersion { get; set; }

    /// <summary>Signed envelope supplied by the hub for proxy-side apply, when signing is configured.</summary>
    [JsonPropertyName("envelope")]
    public FederationExecutionEnvelope? Envelope { get; set; }
}

/// <summary>FR-MCP-103: Signed operation envelope exchanged between hub and local proxies.</summary>
public sealed class FederationExecutionEnvelope
{
    /// <summary>Envelope schema version.</summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Unique envelope identifier.</summary>
    [JsonPropertyName("envelopeId")]
    public string EnvelopeId { get; set; } = "";

    /// <summary>Proxy that issued the envelope.</summary>
    [JsonPropertyName("sourceProxyId")]
    public string SourceProxyId { get; set; } = "";

    /// <summary>Destination proxy, or null for hub intake.</summary>
    [JsonPropertyName("targetProxyId")]
    public string? TargetProxyId { get; set; }

    /// <summary>Operation carried by the envelope.</summary>
    [JsonPropertyName("operation")]
    public FederationOperationRequest Operation { get; set; } = new();

    /// <summary>UTC timestamp when the envelope was issued.</summary>
    [JsonPropertyName("issuedAtUtc")]
    public DateTimeOffset IssuedAtUtc { get; set; }

    /// <summary>UTC timestamp when the envelope expires.</summary>
    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>Replay-protection nonce.</summary>
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = "";

    /// <summary>SHA-256 hash of the decoded operation body bytes.</summary>
    [JsonPropertyName("bodySha256")]
    public string BodySha256 { get; set; } = "";

    /// <summary>Apply mode, for example state or local_execution.</summary>
    [JsonPropertyName("applyMode")]
    public string ApplyMode { get; set; } = "state";

    /// <summary>HMAC signature over the canonical envelope payload.</summary>
    [JsonPropertyName("signature")]
    public FederationEnvelopeSignature? Signature { get; set; }
}

/// <summary>FR-MCP-103: Signed hub request for a machine-local proxy operation.</summary>
public sealed class FederationLocalExecutionRequest
{
    /// <summary>Local execution method, for example desktop_launch.</summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = "";

    /// <summary>Proxy-local workspace path used to resolve local resources.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }

    /// <summary>Full path to an executable for desktop launch operations.</summary>
    [JsonPropertyName("executablePath")]
    public string? ExecutablePath { get; set; }

    /// <summary>Optional command-line arguments.</summary>
    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }

    /// <summary>Optional process working directory.</summary>
    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    /// <summary>Optional process environment variables.</summary>
    [JsonPropertyName("environmentVariables")]
    public Dictionary<string, string>? EnvironmentVariables { get; set; }

    /// <summary>Whether the launched process should not create a visible window.</summary>
    [JsonPropertyName("createNoWindow")]
    public bool CreateNoWindow { get; set; } = true;

    /// <summary>Window style for desktop launch operations.</summary>
    [JsonPropertyName("windowStyle")]
    public string WindowStyle { get; set; } = "Hidden";

    /// <summary>Whether to wait for process exit.</summary>
    [JsonPropertyName("waitForExit")]
    public bool WaitForExit { get; set; } = true;

    /// <summary>Optional timeout in milliseconds when waiting for exit.</summary>
    [JsonPropertyName("timeoutMs")]
    public int? TimeoutMs { get; set; }
}

/// <summary>FR-MCP-103: Result of a signed machine-local proxy operation.</summary>
public sealed class FederationLocalExecutionResult
{
    /// <summary>Whether local execution succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Human-readable result or error text.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>Process identifier when a process was launched.</summary>
    [JsonPropertyName("processId")]
    public int? ProcessId { get; set; }

    /// <summary>Exit code when a process was launched and waited on.</summary>
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; set; }
}

/// <summary>FR-MCP-103: Operation replay or intake request.</summary>
public sealed class FederationOperationRequest
{
    /// <summary>Operation identifier supplied by the caller.</summary>
    [JsonPropertyName("operationId")]
    public string? OperationId { get; set; }

    /// <summary>Proxy that originated the operation.</summary>
    [JsonPropertyName("proxyId")]
    public string ProxyId { get; set; } = "";

    /// <summary>Optional source operation ID used for echo suppression.</summary>
    [JsonPropertyName("sourceOperationId")]
    public string? SourceOperationId { get; set; }

    /// <summary>Hub-wide workspace identifier affected by the operation.</summary>
    [JsonPropertyName("globalWorkspaceId")]
    public string? GlobalWorkspaceId { get; set; }

    /// <summary>Mutable state domain affected by the operation.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = "unknown";

    /// <summary>Domain-specific resource identifier.</summary>
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; set; }

    /// <summary>HTTP method for proxied REST operations.</summary>
    [JsonPropertyName("httpMethod")]
    public string? HttpMethod { get; set; }

    /// <summary>Request path for proxied REST operations.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>MCP method or tool name for transport operations.</summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary>Serialized operation headers.</summary>
    [JsonPropertyName("headersJson")]
    public string? HeadersJson { get; set; }

    /// <summary>Base64-encoded operation payload.</summary>
    [JsonPropertyName("bodyBase64")]
    public string? BodyBase64 { get; set; }

    /// <summary>Proxy-observed base version for optimistic conflict detection.</summary>
    [JsonPropertyName("baseVersion")]
    public string? BaseVersion { get; set; }
}

/// <summary>FR-MCP-103: Operation acknowledgement request.</summary>
public sealed class FederationOperationAckRequest
{
    /// <summary>Status to assign to the acknowledged operation.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "acknowledged";

    /// <summary>Hub-assigned version after apply, when available.</summary>
    [JsonPropertyName("hubVersion")]
    public string? HubVersion { get; set; }

    /// <summary>Error text when acknowledgement represents a failed replay.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>FR-MCP-103: Signature metadata for a federation execution envelope.</summary>
public sealed class FederationEnvelopeSignature
{
    /// <summary>Signature algorithm.</summary>
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = "";

    /// <summary>Canonicalization format.</summary>
    [JsonPropertyName("canonicalization")]
    public string Canonicalization { get; set; } = "";

    /// <summary>Lowercase hexadecimal signature value.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}

/// <summary>FR-MCP-103: Operation status returned by intake and acknowledgement endpoints.</summary>
public sealed class FederationOperationResponse
{
    /// <summary>Operation identifier.</summary>
    [JsonPropertyName("operationId")]
    public string OperationId { get; set; } = "";

    /// <summary>Current operation status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    /// <summary>Whether this call created a new operation row.</summary>
    [JsonPropertyName("created")]
    public bool Created { get; set; }
}

/// <summary>FR-MCP-103: Conflict resolution request.</summary>
public sealed class FederationConflictResolutionRequest
{
    /// <summary>Resolution status to apply, for example hub_wins.</summary>
    [JsonPropertyName("resolutionStatus")]
    public string ResolutionStatus { get; set; } = "hub_wins";
}

/// <summary>FR-MCP-103: Recipient-specific sync acknowledgement request.</summary>
public sealed class FederationSyncAckRequest
{
    /// <summary>Status to assign to the acknowledged sync row.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "acknowledged";

    /// <summary>Hub-assigned version after apply, when available.</summary>
    [JsonPropertyName("hubVersion")]
    public string? HubVersion { get; set; }

    /// <summary>Error text when acknowledgement represents a failed apply.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Proxy that received and applied the sync row.</summary>
    [JsonPropertyName("proxyId")]
    public string? ProxyId { get; set; }
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
