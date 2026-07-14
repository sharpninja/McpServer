using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-103: Durable operation record used for proxy queued writes,
/// hub replay intake, acknowledgements, and idempotency.
/// </summary>
public sealed class FederationOperationEntity
{
    /// <summary>Hub-wide operation identifier.</summary>
    [Key]
    [StringLength(256)]
    public required string OperationId { get; set; }

    /// <summary>Proxy that originated or currently owns the operation.</summary>
    [Required]
    [StringLength(256)]
    public required string ProxyId { get; set; }

    /// <summary>Optional operation ID from the upstream source to suppress echoes.</summary>
    [StringLength(256)]
    public string? SourceOperationId { get; set; }

    /// <summary>Hub-wide workspace identifier affected by the operation.</summary>
    [StringLength(512)]
    public string? GlobalWorkspaceId { get; set; }

    /// <summary>Mutable state domain, such as <c>todo</c>, <c>session_log</c>, or <c>requirements</c>.</summary>
    [Required]
    [StringLength(128)]
    public string Domain { get; set; } = "unknown";

    /// <summary>Domain-specific resource identifier.</summary>
    [StringLength(1024)]
    public string? ResourceId { get; set; }

    /// <summary>HTTP method when the operation came from an MCP REST proxy request.</summary>
    [StringLength(32)]
    public string? HttpMethod { get; set; }

    /// <summary>Request path when the operation came from an MCP REST proxy request.</summary>
    [StringLength(2048)]
    public string? Path { get; set; }

    /// <summary>Command or tool method when the operation came from an MCP transport request.</summary>
    [StringLength(512)]
    public string? Method { get; set; }

    /// <summary>Serialized headers or metadata associated with the operation.</summary>
    public string? HeadersJson { get; set; }

    /// <summary>Base64-encoded request payload retained for replay, subject to configured size limits.</summary>
    public string? BodyBase64 { get; set; }

    /// <summary>Version observed on the proxy before applying the operation.</summary>
    [StringLength(256)]
    public string? BaseVersion { get; set; }

    /// <summary>Version assigned by the hub after applying the operation.</summary>
    [StringLength(256)]
    public string? HubVersion { get; set; }

    /// <summary>Status label: <c>queued</c>, <c>accepted</c>, <c>replayed</c>, <c>acknowledged</c>, or <c>conflict</c>.</summary>
    [Required]
    [StringLength(64)]
    public string Status { get; set; } = "queued";

    /// <summary>Number of replay attempts made for this operation.</summary>
    public int AttemptCount { get; set; }

    /// <summary>UTC timestamp when the operation was first queued or accepted.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the operation was last updated.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the operation was acknowledged by its target.</summary>
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }

    /// <summary>Last replay or conflict error text.</summary>
    public string? LastError { get; set; }
}
