using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-DB-004: Append-only generic audit ledger for mutable MCP database
/// entities. Domain-specific audit tables may remain for compatibility, but
/// mutations should also be mirrored here.
/// </summary>
public sealed class DataAuditLogEntity
{
    /// <summary>Stable audit row identifier.</summary>
    [Key]
    [MaxLength(64)]
    public string AuditId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Workspace affected by the mutation, or empty for global rows.</summary>
    [Required]
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Entity CLR type or domain kind.</summary>
    [Required]
    [MaxLength(256)]
    public string EntityKind { get; set; } = string.Empty;

    /// <summary>Stable key for the affected entity.</summary>
    [Required]
    [MaxLength(1024)]
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>Semantic action, such as create, update, or delete.</summary>
    [Required]
    [MaxLength(64)]
    public string Action { get; set; } = string.Empty;

    /// <summary>Actor responsible for the mutation.</summary>
    [Required]
    [MaxLength(256)]
    public string Actor { get; set; } = "system";

    /// <summary>Agent, service, import, federation, or other source type.</summary>
    [Required]
    [MaxLength(128)]
    public string SourceType { get; set; } = "McpDbContext";

    /// <summary>Optional request identifier for correlation.</summary>
    [MaxLength(256)]
    public string? RequestId { get; set; }

    /// <summary>Optional cross-system correlation identifier.</summary>
    [MaxLength(256)]
    public string? CorrelationId { get; set; }

    /// <summary>Optional federation operation identifier.</summary>
    [MaxLength(256)]
    public string? FederationOperationId { get; set; }

    /// <summary>UTC timestamp when the mutation was recorded.</summary>
    public DateTimeOffset OccurredAtUtc { get; set; }

    /// <summary>Sanitized JSON snapshot before the mutation.</summary>
    public string? PreviousSnapshotJson { get; set; }

    /// <summary>Sanitized JSON snapshot after the mutation.</summary>
    public string? CurrentSnapshotJson { get; set; }

    /// <summary>Optional sanitized JSON diff.</summary>
    public string? DiffJson { get; set; }

    /// <summary>Optional metadata JSON for domain-specific details.</summary>
    public string? MetadataJson { get; set; }
}
