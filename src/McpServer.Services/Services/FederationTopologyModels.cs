namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-103: Request body used by a LocalProxy to enroll with a hub.</summary>
public sealed class FederationEnrollmentRequest
{
    /// <summary>Stable proxy identifier. If empty, the hub may derive one from the display name.</summary>
    public string? ProxyId { get; set; }

    /// <summary>Human-readable proxy name, usually the machine name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Callback base URL for hub fanout or local execution envelopes.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Enrollment token shared with the hub.</summary>
    public string? EnrollmentToken { get; set; }

    /// <summary>Opaque proxy metadata JSON.</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Workspaces hosted by the proxy at enrollment time.</summary>
    public IReadOnlyList<FederationWorkspaceRegistrationRequest> Workspaces { get; set; } = [];
}

/// <summary>FR-MCP-103: Response returned when a proxy enrollment succeeds.</summary>
public sealed class FederationEnrollmentResponse
{
    /// <summary>Accepted proxy identifier.</summary>
    public string ProxyId { get; set; } = string.Empty;

    /// <summary>Whether enrollment was accepted by the hub.</summary>
    public bool Accepted { get; set; }

    /// <summary>Current hub UTC timestamp.</summary>
    public DateTimeOffset ServerTimeUtc { get; set; }

    /// <summary>Expected heartbeat interval in seconds.</summary>
    public int HeartbeatSeconds { get; set; }
}

/// <summary>FR-MCP-103: Request body for proxy heartbeat updates.</summary>
public sealed class FederationHeartbeatRequest
{
    /// <summary>Proxy status label, usually <c>online</c>.</summary>
    public string? Status { get; set; }

    /// <summary>Opaque heartbeat metadata JSON.</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Current hosted workspace snapshot from the proxy.</summary>
    public IReadOnlyList<FederationWorkspaceRegistrationRequest> Workspaces { get; set; } = [];
}

/// <summary>FR-MCP-103: Heartbeat result returned to a proxy.</summary>
public sealed class FederationHeartbeatResponse
{
    /// <summary>Proxy identifier that was updated.</summary>
    public string ProxyId { get; set; } = string.Empty;

    /// <summary>UTC timestamp recorded by the hub.</summary>
    public DateTimeOffset RecordedAtUtc { get; set; }

    /// <summary>Current hub-side queued operation count for this proxy.</summary>
    public int QueueDepth { get; set; }

    /// <summary>Current open conflict count for this proxy.</summary>
    public int ConflictCount { get; set; }
}

/// <summary>FR-MCP-103: Workspace registration payload supplied by a proxy.</summary>
public sealed class FederationWorkspaceRegistrationRequest
{
    /// <summary>Optional hub-wide workspace identifier. If omitted, the hub derives one.</summary>
    public string? GlobalWorkspaceId { get; set; }

    /// <summary>Workspace display name.</summary>
    public string? WorkspaceName { get; set; }

    /// <summary>Proxy-local absolute workspace path.</summary>
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Whether the workspace is enabled on the proxy.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Proxy-reported workspace version, when available.</summary>
    public string? Version { get; set; }

    /// <summary>Opaque workspace metadata JSON.</summary>
    public string? MetadataJson { get; set; }
}

/// <summary>FR-MCP-103: Proxy inventory row returned by the hub.</summary>
public sealed class FederationProxyInfo
{
    /// <summary>Stable proxy identifier.</summary>
    public string ProxyId { get; set; } = string.Empty;

    /// <summary>Human-readable proxy name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Federation role reported by the proxy.</summary>
    public string Role { get; set; } = "LocalProxy";

    /// <summary>Proxy callback base URL, if configured.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Current proxy status.</summary>
    public string Status { get; set; } = "enrolled";

    /// <summary>UTC timestamp of the most recent heartbeat.</summary>
    public DateTimeOffset? LastHeartbeatUtc { get; set; }

    /// <summary>Number of registered workspaces hosted by the proxy.</summary>
    public int WorkspaceCount { get; set; }
}

/// <summary>FR-MCP-103: Workspace inventory row returned by the hub.</summary>
public sealed class FederationWorkspaceInfo
{
    /// <summary>Hub-wide workspace identifier.</summary>
    public string GlobalWorkspaceId { get; set; } = string.Empty;

    /// <summary>Proxy that hosts the workspace.</summary>
    public string ProxyId { get; set; } = string.Empty;

    /// <summary>Workspace display name.</summary>
    public string? WorkspaceName { get; set; }

    /// <summary>Proxy-local absolute workspace path.</summary>
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Whether the workspace is enabled on the proxy.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Proxy-reported workspace version.</summary>
    public string? Version { get; set; }

    /// <summary>UTC timestamp when the workspace was last seen by the hub.</summary>
    public DateTimeOffset LastSeenUtc { get; set; }
}

/// <summary>FR-MCP-103: Operation replay or intake request.</summary>
public sealed class FederationOperationRequest
{
    /// <summary>Operation identifier supplied by the caller. If empty, the hub assigns one.</summary>
    public string? OperationId { get; set; }

    /// <summary>Proxy that originated the operation.</summary>
    public string ProxyId { get; set; } = string.Empty;

    /// <summary>Optional source operation ID used for echo suppression.</summary>
    public string? SourceOperationId { get; set; }

    /// <summary>Hub-wide workspace identifier affected by the operation.</summary>
    public string? GlobalWorkspaceId { get; set; }

    /// <summary>Mutable state domain affected by the operation.</summary>
    public string Domain { get; set; } = "unknown";

    /// <summary>Domain-specific resource identifier.</summary>
    public string? ResourceId { get; set; }

    /// <summary>HTTP method for proxied REST operations.</summary>
    public string? HttpMethod { get; set; }

    /// <summary>Request path for proxied REST operations.</summary>
    public string? Path { get; set; }

    /// <summary>MCP method or tool name for transport operations.</summary>
    public string? Method { get; set; }

    /// <summary>Serialized operation headers.</summary>
    public string? HeadersJson { get; set; }

    /// <summary>Base64-encoded operation payload.</summary>
    public string? BodyBase64 { get; set; }

    /// <summary>Proxy-observed base version for optimistic conflict detection.</summary>
    public string? BaseVersion { get; set; }
}

/// <summary>FR-MCP-103: Operation acknowledgement request.</summary>
public sealed class FederationOperationAckRequest
{
    /// <summary>Status to assign to the acknowledged operation.</summary>
    public string Status { get; set; } = "acknowledged";

    /// <summary>Hub-assigned version after apply, when available.</summary>
    public string? HubVersion { get; set; }

    /// <summary>Error text when acknowledgement represents a failed replay.</summary>
    public string? Error { get; set; }
}

/// <summary>FR-MCP-103: Operation status returned by intake and acknowledgement endpoints.</summary>
public sealed class FederationOperationResponse
{
    /// <summary>Operation identifier.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>Current operation status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Whether this call created a new operation row.</summary>
    public bool Created { get; set; }
}

/// <summary>FR-MCP-103: Durable local operation row waiting to replay to the hub.</summary>
public sealed class FederationOperationReplayItem
{
    /// <summary>Operation identifier.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>Proxy that originated the operation.</summary>
    public string ProxyId { get; set; } = string.Empty;

    /// <summary>Optional source operation ID used for echo suppression.</summary>
    public string? SourceOperationId { get; set; }

    /// <summary>Hub-wide workspace identifier affected by the operation.</summary>
    public string? GlobalWorkspaceId { get; set; }

    /// <summary>Mutable state domain affected by the operation.</summary>
    public string Domain { get; set; } = "unknown";

    /// <summary>Domain-specific resource identifier.</summary>
    public string? ResourceId { get; set; }

    /// <summary>HTTP method for proxied REST operations.</summary>
    public string? HttpMethod { get; set; }

    /// <summary>Request path for proxied REST operations.</summary>
    public string? Path { get; set; }

    /// <summary>MCP method or tool name for transport operations.</summary>
    public string? Method { get; set; }

    /// <summary>Serialized operation headers.</summary>
    public string? HeadersJson { get; set; }

    /// <summary>Base64-encoded operation payload.</summary>
    public string? BodyBase64 { get; set; }

    /// <summary>Proxy-observed base version for optimistic conflict detection.</summary>
    public string? BaseVersion { get; set; }

    /// <summary>Current operation status.</summary>
    public string Status { get; set; } = "queued";

    /// <summary>Replay attempts recorded for this operation.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Creates the hub intake request represented by this replay item.</summary>
    public FederationOperationRequest ToRequest()
        => new()
        {
            OperationId = OperationId,
            ProxyId = ProxyId,
            SourceOperationId = SourceOperationId,
            GlobalWorkspaceId = GlobalWorkspaceId,
            Domain = Domain,
            ResourceId = ResourceId,
            HttpMethod = HttpMethod,
            Path = Path,
            Method = Method,
            HeadersJson = HeadersJson,
            BodyBase64 = BodyBase64,
            BaseVersion = BaseVersion,
        };
}

/// <summary>FR-MCP-103: Queue status projection for hub and proxy diagnostics.</summary>
public sealed class FederationQueueStatusResponse
{
    /// <summary>Optional proxy filter used for this status response.</summary>
    public string? ProxyId { get; set; }

    /// <summary>Number of operations waiting for replay or acknowledgement.</summary>
    public int QueueDepth { get; set; }

    /// <summary>Number of operations currently in conflict.</summary>
    public int ConflictCount { get; set; }

    /// <summary>Number of unacknowledged hub fanout rows.</summary>
    public int FanoutDepth { get; set; }
}

/// <summary>FR-MCP-103: Conflict inventory row.</summary>
public sealed class FederationConflictInfo
{
    /// <summary>Conflict identifier.</summary>
    public string ConflictId { get; set; } = string.Empty;

    /// <summary>Operation that caused the conflict.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>Proxy that submitted the conflicting operation.</summary>
    public string ProxyId { get; set; } = string.Empty;

    /// <summary>Mutable state domain where the conflict occurred.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Domain-specific resource identifier.</summary>
    public string? ResourceId { get; set; }

    /// <summary>Proxy-observed version.</summary>
    public string? ProxyVersion { get; set; }

    /// <summary>Hub-authoritative version.</summary>
    public string? HubVersion { get; set; }

    /// <summary>Resolution status.</summary>
    public string ResolutionStatus { get; set; } = "open";

    /// <summary>UTC timestamp when the conflict was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>FR-MCP-103: Conflict resolution request.</summary>
public sealed class FederationConflictResolutionRequest
{
    /// <summary>Resolution status to apply, for example <c>hub_wins</c>.</summary>
    public string ResolutionStatus { get; set; } = "hub_wins";
}

/// <summary>FR-MCP-103: Hub outbox sync row delivered to a proxy.</summary>
public sealed class FederationSyncItem
{
    /// <summary>Monotonic hub sequence number.</summary>
    public long Sequence { get; set; }

    /// <summary>Operation identifier to synchronize.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>Mutable state domain affected by the operation.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Domain-specific resource identifier.</summary>
    public string? ResourceId { get; set; }

    /// <summary>Hub version associated with the operation.</summary>
    public string? HubVersion { get; set; }
}
