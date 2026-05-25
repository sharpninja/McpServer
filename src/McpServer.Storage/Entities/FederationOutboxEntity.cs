using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-103: Hub fanout entry waiting to be synchronized to a local proxy.
/// </summary>
public sealed class FederationOutboxEntity
{
    /// <summary>Monotonic database-generated sequence used by sync consumers.</summary>
    [Key]
    public long Sequence { get; set; }

    /// <summary>Destination proxy ID.</summary>
    [Required]
    [MaxLength(256)]
    public required string ProxyId { get; set; }

    /// <summary>Operation to deliver to the proxy.</summary>
    [Required]
    [MaxLength(256)]
    public required string OperationId { get; set; }

    /// <summary>UTC timestamp when the outbox row was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the destination proxy acknowledged the row.</summary>
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
}
