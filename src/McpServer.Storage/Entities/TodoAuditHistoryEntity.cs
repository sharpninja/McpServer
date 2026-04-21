using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TODO-005 (provider-agnostic): Append-only audit row for a TODO item.
/// One row per mutation; ordered by <see cref="AuditId"/> within a given
/// <see cref="TodoId"/>. Routed through <c>McpDbContext</c> to whichever provider
/// <c>Mcp:Database:Provider</c> selects.
/// </summary>
public sealed class TodoAuditHistoryEntity
{
    /// <summary>Monotonic audit identifier (auto-incremented primary key).</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AuditId { get; set; }

    /// <summary>
    /// TR-MCP-TODO-008 workspace discriminator. Filter column (non-PK; the PK
    /// stays on <see cref="AuditId"/> so audits remain append-only under a
    /// single monotonic identity). Combined with <see cref="TodoId"/> and
    /// <see cref="Version"/> in the unique index so the same
    /// <c>(TodoId, Version)</c> may exist under different workspaces.
    /// </summary>
    [Required]
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>TODO item identifier this audit row belongs to.</summary>
    [Required]
    [MaxLength(128)]
    public required string TodoId { get; set; }

    /// <summary>Monotonic version number for <see cref="TodoId"/> (starts at 1).</summary>
    public int Version { get; set; }

    /// <summary>Action label: <c>imported</c> | <c>created</c> | <c>updated</c> | <c>deleted</c>.</summary>
    [Required]
    [MaxLength(32)]
    public required string Action { get; set; }

    /// <summary>UTC timestamp when the audit row was recorded (ISO-8601).</summary>
    [Required]
    [MaxLength(64)]
    public required string RecordedAtUtc { get; set; }

    /// <summary>JSON-serialized post-mutation <see cref="TodoItemEntity"/> snapshot.</summary>
    public string? SnapshotJson { get; set; }

    /// <summary>JSON-serialized pre-mutation <see cref="TodoItemEntity"/> snapshot.</summary>
    public string? PreviousSnapshotJson { get; set; }

    /// <summary>Origin of the mutation or backfill (e.g. <c>yaml-import</c>, <c>api</c>).</summary>
    [MaxLength(128)]
    public string? Source { get; set; }
}
