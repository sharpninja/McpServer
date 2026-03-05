using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-013: 4NF session log entry entity. One row per request/response pair.
/// FR-SUPPORT-010: Child of <see cref="SessionLogEntity"/>.
/// </summary>
public sealed class SessionLogEntryEntity
{
    /// <summary>TR-PLANNED-013: Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>TR-PLANNED-013: Foreign key to parent session.</summary>
    public long SessionLogId { get; set; }

    /// <summary>TR-PLANNED-013: Unique request identifier within the session.</summary>
    [MaxLength(256)]
    public string? RequestId { get; set; }

    /// <summary>TR-PLANNED-013: Timestamp of the request (UTC).</summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>TR-PLANNED-013: AI model used for this entry.</summary>
    [MaxLength(128)]
    public string? Model { get; set; }

    /// <summary>TR-PLANNED-013: Model provider (e.g. OpenAI, Anthropic).</summary>
    [MaxLength(128)]
    public string? ModelProvider { get; set; }

    /// <summary>TR-PLANNED-013: Full user query text.</summary>
    public string? QueryText { get; set; }

    /// <summary>TR-PLANNED-013: Short title summarizing the query.</summary>
    [MaxLength(1024)]
    public string? QueryTitle { get; set; }

    /// <summary>TR-PLANNED-013: Agent response text.</summary>
    public string? Response { get; set; }

    /// <summary>TR-PLANNED-013: Agent interpretation of the request.</summary>
    public string? Interpretation { get; set; }

    /// <summary>TR-PLANNED-013: Entry status (e.g. completed, in_progress).</summary>
    [MaxLength(64)]
    public string? Status { get; set; }

    /// <summary>TR-PLANNED-013: Token count for this entry.</summary>
    public int? TokenCount { get; set; }

    /// <summary>TR-PLANNED-013: Failure note if the entry failed.</summary>
    public string? FailureNote { get; set; }

    /// <summary>TR-PLANNED-013: Success score for this entry.</summary>
    public double? Score { get; set; }

    /// <summary>TR-PLANNED-013: Whether this was a premium request.</summary>
    public bool? IsPremium { get; set; }

    /// <summary>TR-PLANNED-013: Raw context data serialized as JSON text.</summary>
    public string? RawContextJson { get; set; }

    /// <summary>TR-PLANNED-013: Original entry before normalization serialized as JSON text.</summary>
    public string? OriginalEntryJson { get; set; }

    /// <summary>TR-PLANNED-013: Navigation to parent session.</summary>
    public SessionLogEntity? SessionLog { get; set; }

    /// <summary>TR-PLANNED-013: Navigation to actions.</summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF Core navigation collection")]
    public ICollection<SessionLogActionEntity> Actions { get; set; } = new List<SessionLogActionEntity>();

    /// <summary>TR-PLANNED-013: Navigation to tags.</summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF Core navigation collection")]
    public ICollection<SessionLogEntryTagEntity> Tags { get; set; } = new List<SessionLogEntryTagEntity>();

    /// <summary>TR-PLANNED-013: Navigation to context items.</summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF Core navigation collection")]
    public ICollection<SessionLogEntryContextEntity> ContextItems { get; set; } = new List<SessionLogEntryContextEntity>();

    /// <summary>TR-PLANNED-013: Navigation to processing dialog items. The AI model can independently append entries.</summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF Core navigation collection")]
    public ICollection<SessionLogProcessingDialogEntity> ProcessingDialog { get; set; } = new List<SessionLogProcessingDialogEntity>();

    /// <summary>TR-PLANNED-013: Navigation to commits recorded during this entry.</summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF Core navigation collection")]
    public ICollection<SessionLogCommitEntity> Commits { get; set; } = new List<SessionLogCommitEntity>();

    /// <summary>TR-PLANNED-013: Navigation to generic string-list items (design decisions, requirements, files modified, blockers).</summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF Core navigation collection")]
    public ICollection<SessionLogEntryStringListEntity> StringListItems { get; set; } = new List<SessionLogEntryStringListEntity>();
}
