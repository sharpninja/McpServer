using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TRIAGE-001: Durable workspace-scoped incidental bug report row.
/// </summary>
public sealed class TriageReportEntity
{
    /// <summary>Workspace discriminator used by MCP multi-tenant filters.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Durable triage report id.</summary>
    [Key]
    [StringLength(128)]
    public required string ReportId { get; set; }

    /// <summary>Owning group id.</summary>
    [Required]
    [StringLength(128)]
    public required string GroupId { get; set; }

    /// <summary>Submitting workspace path before any MCP Server routing.</summary>
    [Required]
    [StringLength(1024)]
    public required string OriginalWorkspacePath { get; set; }

    /// <summary>Effective workspace path used for persistence and grouping.</summary>
    [Required]
    [StringLength(1024)]
    public required string EffectiveWorkspacePath { get; set; }

    /// <summary>Report title.</summary>
    [Required]
    [StringLength(512)]
    public required string Title { get; set; }

    /// <summary>Report summary.</summary>
    [Required]
    public required string Summary { get; set; }

    /// <summary>Observed behavior, if provided.</summary>
    public string? ObservedBehavior { get; set; }

    /// <summary>Expected behavior, if provided.</summary>
    public string? ExpectedBehavior { get; set; }

    /// <summary>Severity label.</summary>
    [StringLength(32)]
    public string? Severity { get; set; }

    /// <summary>Component or plugin name.</summary>
    [StringLength(256)]
    public string? Component { get; set; }

    /// <summary>Caller-provided dedupe key.</summary>
    [StringLength(512)]
    public string? DedupeKey { get; set; }

    /// <summary>Error signature or invariant text.</summary>
    public string? ErrorSignature { get; set; }

    /// <summary>Deterministic grouping fingerprint.</summary>
    [Required]
    [StringLength(128)]
    public required string Fingerprint { get; set; }

    /// <summary>Evidence map serialized as JSON (single-valued map; not a 4NF list).</summary>
    public string? EvidenceJson { get; set; }

    /// <summary>TR-MCP-TRIAGE-001: 4NF child rows for the affected-paths, affected-symbols,
    /// reproduction-hints, and tags lists (discriminated by <see cref="TriageReportListItemEntity.ListType"/>).</summary>
    public List<TriageReportListItemEntity> ListItems { get; set; } = [];

    /// <summary>Reporting agent identity.</summary>
    [StringLength(128)]
    public string? ReporterAgent { get; set; }

    /// <summary>Submitting session id.</summary>
    [StringLength(256)]
    public string? SessionId { get; set; }

    /// <summary>Submitting turn id.</summary>
    [StringLength(256)]
    public string? TurnId { get; set; }

    /// <summary>Active TODO id when the report was discovered.</summary>
    [StringLength(128)]
    public string? CurrentTodoId { get; set; }

    /// <summary>Optional idempotency key.</summary>
    [StringLength(512)]
    public string? IdempotencyKey { get; set; }

    /// <summary>Report status.</summary>
    [Required]
    [StringLength(64)]
    public required string Status { get; set; }

    /// <summary>UTC timestamp when the report was created.</summary>
    public DateTimeOffset CreatedUtc { get; set; }
}
