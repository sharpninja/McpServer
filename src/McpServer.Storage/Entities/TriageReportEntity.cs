using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TRIAGE-001: Durable workspace-scoped incidental bug report row.
/// </summary>
public sealed class TriageReportEntity
{
    /// <summary>Workspace discriminator used by MCP multi-tenant filters.</summary>
    [Required]
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Durable triage report id.</summary>
    [Key]
    [MaxLength(128)]
    public required string ReportId { get; set; }

    /// <summary>Owning group id.</summary>
    [Required]
    [MaxLength(128)]
    public required string GroupId { get; set; }

    /// <summary>Submitting workspace path before any MCP Server routing.</summary>
    [Required]
    [MaxLength(1024)]
    public required string OriginalWorkspacePath { get; set; }

    /// <summary>Effective workspace path used for persistence and grouping.</summary>
    [Required]
    [MaxLength(1024)]
    public required string EffectiveWorkspacePath { get; set; }

    /// <summary>Report title.</summary>
    [Required]
    [MaxLength(512)]
    public required string Title { get; set; }

    /// <summary>Report summary.</summary>
    [Required]
    public required string Summary { get; set; }

    /// <summary>Observed behavior, if provided.</summary>
    public string? ObservedBehavior { get; set; }

    /// <summary>Expected behavior, if provided.</summary>
    public string? ExpectedBehavior { get; set; }

    /// <summary>Severity label.</summary>
    [MaxLength(32)]
    public string? Severity { get; set; }

    /// <summary>Component or plugin name.</summary>
    [MaxLength(256)]
    public string? Component { get; set; }

    /// <summary>Caller-provided dedupe key.</summary>
    [MaxLength(512)]
    public string? DedupeKey { get; set; }

    /// <summary>Error signature or invariant text.</summary>
    public string? ErrorSignature { get; set; }

    /// <summary>Deterministic grouping fingerprint.</summary>
    [Required]
    [MaxLength(128)]
    public required string Fingerprint { get; set; }

    /// <summary>Affected paths serialized as JSON.</summary>
    public string? AffectedPathsJson { get; set; }

    /// <summary>Affected symbols serialized as JSON.</summary>
    public string? AffectedSymbolsJson { get; set; }

    /// <summary>Evidence map serialized as JSON.</summary>
    public string? EvidenceJson { get; set; }

    /// <summary>Reproduction hints serialized as JSON.</summary>
    public string? ReproductionHintsJson { get; set; }

    /// <summary>Tags serialized as JSON.</summary>
    public string? TagsJson { get; set; }

    /// <summary>Reporting agent identity.</summary>
    [MaxLength(128)]
    public string? ReporterAgent { get; set; }

    /// <summary>Submitting session id.</summary>
    [MaxLength(256)]
    public string? SessionId { get; set; }

    /// <summary>Submitting turn id.</summary>
    [MaxLength(256)]
    public string? TurnId { get; set; }

    /// <summary>Active TODO id when the report was discovered.</summary>
    [MaxLength(128)]
    public string? CurrentTodoId { get; set; }

    /// <summary>Optional idempotency key.</summary>
    [MaxLength(512)]
    public string? IdempotencyKey { get; set; }

    /// <summary>Report status.</summary>
    [Required]
    [MaxLength(64)]
    public required string Status { get; set; }

    /// <summary>UTC timestamp when the report was created.</summary>
    public DateTimeOffset CreatedUtc { get; set; }
}
