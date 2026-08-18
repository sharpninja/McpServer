using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>TR-HANDOFF-AUDIT-001: Normalized handoff ingestion run. Does not store raw source content.</summary>
public sealed class HandoffIngestionRunEntity
{
    /// <summary>Durable run identifier.</summary>
    [Key]
    [StringLength(128)]
    public required string RunId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public required string WorkspaceId { get; set; }

    /// <summary>Source kind name.</summary>
    [Required]
    [StringLength(32)]
    public required string SourceKind { get; set; }

    /// <summary>Path or artifact locator. Never raw source content.</summary>
    [Required]
    [StringLength(2048)]
    public required string SourceLocator { get; set; }

    /// <summary>SHA-256 of decoded source bytes.</summary>
    [Required]
    [StringLength(64)]
    public required string ContentSha256 { get; set; }

    /// <summary>UTC extraction timestamp.</summary>
    public DateTimeOffset ExtractedAtUtc { get; set; }

    /// <summary>Prompt version used for extraction.</summary>
    [Required]
    [StringLength(128)]
    public required string PromptVersion { get; set; }

    /// <summary>Template identifier when present.</summary>
    [StringLength(128)]
    public string? TemplateVersion { get; set; }

    /// <summary>Agent name.</summary>
    [StringLength(128)]
    public string? Agent { get; set; }

    /// <summary>Model name.</summary>
    [StringLength(128)]
    public string? Model { get; set; }

    /// <summary>Recorded confidence.</summary>
    public double? Confidence { get; set; }

    /// <summary>Requested mode name.</summary>
    [Required]
    [StringLength(32)]
    public required string Mode { get; set; }

    /// <summary>Review state name.</summary>
    [Required]
    [StringLength(32)]
    public required string ReviewState { get; set; }

    /// <summary>Created TODO identifier.</summary>
    [StringLength(128)]
    public string? CreatedTodoId { get; set; }

    /// <summary>Normalized draft JSON. Never raw source content.</summary>
    public string? DraftJson { get; set; }

    /// <summary>Whether this run was forced past replay.</summary>
    public bool Force { get; set; }

    /// <summary>
    /// Provider-portable unique replay identity. Fixed-length SHA-256 hex of the canonical payload.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string ReplayIdentity { get; set; } = string.Empty;

    /// <summary>Durable processing state: None, Processing, or Terminal.</summary>
    [Required]
    [StringLength(32)]
    public string ProcessingState { get; set; } = "None";

    /// <summary>Service instance that currently holds the ingest lease.</summary>
    [StringLength(128)]
    public string? ProcessingOwner { get; set; }

    /// <summary>When the ingest lease expires and another instance may take over.</summary>
    public DateTimeOffset? ProcessingLeaseExpiresAtUtc { get; set; }

    /// <summary>Service instance that currently holds the approval lease.</summary>
    [StringLength(128)]
    public string? ApprovalOwner { get; set; }

    /// <summary>When the approval lease expires and another instance may take over.</summary>
    public DateTimeOffset? ApprovalLeaseExpiresAtUtc { get; set; }

    /// <summary>Durable TODO creation intent persisted before calling ITodoService.</summary>
    [StringLength(128)]
    public string? TodoCreationIntentId { get; set; }

    /// <summary>Durable success flag mapped by GET/replay. False for failed source or extraction.</summary>
    public bool Succeeded { get; set; }

    /// <summary>Durable error message when <see cref="Succeeded"/> is false.</summary>
    [StringLength(1024)]
    public string? Error { get; set; }

    /// <summary>Stable machine-readable outcome code such as run_not_found or extract_malformed.</summary>
    [StringLength(64)]
    public string? ErrorCode { get; set; }

    /// <summary>Optimistic concurrency version used for approval state transitions.</summary>
    public int StateVersion { get; set; }

    /// <summary>Reviewer identity when approved or rejected.</summary>
    [StringLength(128)]
    public string? Reviewer { get; set; }

    /// <summary>Review notes. Must not contain secrets or source content.</summary>
    [StringLength(1024)]
    public string? ReviewNotes { get; set; }

    /// <summary>Child diagnostics.</summary>
    public List<HandoffDiagnosticEntity> Diagnostics { get; set; } = [];
}
