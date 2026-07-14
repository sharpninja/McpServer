using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-103: Durable conflict record created when a proxy replay cannot be
/// applied cleanly against the hub's authoritative version.
/// </summary>
public sealed class FederationConflictEntity
{
    /// <summary>Hub-wide conflict identifier.</summary>
    [Key]
    [StringLength(256)]
    public required string ConflictId { get; set; }

    /// <summary>Operation that caused the conflict.</summary>
    [Required]
    [StringLength(256)]
    public required string OperationId { get; set; }

    /// <summary>Proxy that submitted the conflicting operation.</summary>
    [Required]
    [StringLength(256)]
    public required string ProxyId { get; set; }

    /// <summary>Mutable state domain where the conflict occurred.</summary>
    [Required]
    [StringLength(128)]
    public required string Domain { get; set; }

    /// <summary>Domain-specific resource identifier.</summary>
    [StringLength(1024)]
    public string? ResourceId { get; set; }

    /// <summary>Version observed by the proxy.</summary>
    [StringLength(256)]
    public string? ProxyVersion { get; set; }

    /// <summary>Authoritative version observed by the hub.</summary>
    [StringLength(256)]
    public string? HubVersion { get; set; }

    /// <summary>Resolution status such as <c>open</c>, <c>hub_wins</c>, or <c>proxy_wins</c>.</summary>
    [Required]
    [StringLength(64)]
    public string ResolutionStatus { get; set; } = "open";

    /// <summary>Opaque conflict details JSON for operator review.</summary>
    public string? DetailsJson { get; set; }

    /// <summary>UTC timestamp when the conflict was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the conflict was resolved.</summary>
    public DateTimeOffset? ResolvedAtUtc { get; set; }
}
