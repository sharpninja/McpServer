using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-103: Durable hub-side record for one enrolled local proxy.
/// </summary>
public sealed class FederationProxyEntity
{
    /// <summary>Stable proxy identifier supplied by the proxy or assigned by the hub.</summary>
    [Key]
    [MaxLength(256)]
    public required string ProxyId { get; set; }

    /// <summary>Human-readable proxy display name, usually the machine name.</summary>
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    /// <summary>Federation role reported by the proxy.</summary>
    [Required]
    [MaxLength(64)]
    public string Role { get; set; } = "LocalProxy";

    /// <summary>Proxy base URL when it can receive hub fanout or execution callbacks.</summary>
    [MaxLength(2048)]
    public string? BaseUrl { get; set; }

    /// <summary>Opaque metadata JSON reported by the proxy.</summary>
    public string? MetadataJson { get; set; }

    /// <summary>UTC timestamp when the proxy was first enrolled.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the proxy record was last updated.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>UTC timestamp of the most recent heartbeat from this proxy.</summary>
    public DateTimeOffset? LastHeartbeatUtc { get; set; }

    /// <summary>Current health/status label such as <c>enrolled</c> or <c>online</c>.</summary>
    [Required]
    [MaxLength(64)]
    public string Status { get; set; } = "enrolled";
}
