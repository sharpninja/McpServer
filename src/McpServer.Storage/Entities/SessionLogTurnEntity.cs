using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-CORE-013: 4NF session log turn entity. One row per request/response pair.
/// FR-SUPPORT-010: Child of <see cref="SessionLogEntity"/>.
/// </summary>
public sealed class SessionLogTurnEntity
{
    /// <summary>TR-PLANNED-CORE-013: Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>TR-PLANNED-CORE-013: Foreign key to parent session.</summary>
    public long SessionLogId { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Unique request identifier within the session.</summary>
    [StringLength(256)]
    public string? RequestId { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Timestamp of the request (UTC).</summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>TR-PLANNED-CORE-013: AI model used for this turn.</summary>
    [StringLength(128)]
    public string? Model { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Model provider (e.g. OpenAI, Anthropic).</summary>
    [StringLength(128)]
    public string? ModelProvider { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Full user query text.</summary>
    public string? QueryText { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Short title summarizing the query.</summary>
    [StringLength(1024)]
    public string? QueryTitle { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Agent response text.</summary>
    public string? Response { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Agent interpretation of the request.</summary>
    public string? Interpretation { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Turn status (e.g. completed, in_progress).</summary>
    [StringLength(64)]
    public string? Status { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Token count for this turn.</summary>
    public int? TokenCount { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Failure note if the turn failed.</summary>
    public string? FailureNote { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Success score for this turn.</summary>
    public double? Score { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Whether this was a premium request.</summary>
    public bool? IsPremium { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Raw context data serialized as JSON text.</summary>
    public string? RawContextJson { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Original turn before normalization serialized as JSON text.</summary>
    public string? OriginalEntryJson { get; set; }

    /// <summary>
    /// FR-MCP-SESSIONLOGCTX-001 / TR-MCP-SESSIONLOG-006:
    /// Current plan file for this turn, or the sentinel <c>None</c>.
    /// </summary>
    [Required]
    [StringLength(2048)]
    public string PlanFile { get; set; } = "None";

    /// <summary>
    /// FR-MCP-SESSIONLOGCTX-001 / TR-MCP-SESSIONLOG-006:
    /// Current MCP TODO id for this turn, or the sentinel <c>None</c>.
    /// </summary>
    [Required]
    [StringLength(128)]
    public string TodoId { get; set; } = "None";

    /// <summary>TR-PLANNED-CORE-013: Navigation to parent session.</summary>
    public SessionLogEntity? SessionLog { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Navigation to actions.</summary>
    public ICollection<SessionLogActionEntity> Actions { get; } = new List<SessionLogActionEntity>();

    /// <summary>TR-PLANNED-CORE-013: Navigation to tags.</summary>
    public ICollection<SessionLogTurnTagEntity> Tags { get; } = new List<SessionLogTurnTagEntity>();

    /// <summary>TR-PLANNED-CORE-013: Navigation to context items.</summary>
    public ICollection<SessionLogTurnContextEntity> ContextItems { get; } = new List<SessionLogTurnContextEntity>();

    /// <summary>TR-PLANNED-CORE-013: Navigation to processing dialog items. The AI model can independently append dialog items.</summary>
    public ICollection<SessionLogProcessingDialogEntity> ProcessingDialog { get; } = new List<SessionLogProcessingDialogEntity>();

    /// <summary>TR-PLANNED-CORE-013: Navigation to commits recorded during this turn.</summary>
    public ICollection<SessionLogCommitEntity> Commits { get; } = new List<SessionLogCommitEntity>();

    /// <summary>TR-PLANNED-CORE-013: Navigation to generic string-list items (design decisions, requirements, files modified, blockers).</summary>
    public ICollection<SessionLogTurnStringListEntity> StringListItems { get; } = new List<SessionLogTurnStringListEntity>();
}

