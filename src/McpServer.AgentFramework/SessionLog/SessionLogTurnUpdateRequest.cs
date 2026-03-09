using McpServer.Client.Models;

namespace McpServer.AgentFramework.SessionLog;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-006: Parameters for updating an existing request entry (turn) within the active session-log workflow context.
/// Only non-<see langword="null"/> properties are applied; omitted properties leave the entry unchanged.
/// </summary>
public sealed class SessionLogTurnUpdateRequest
{
    /// <summary>
    /// Gets or sets the identifier of the request entry to update. Must match a
    /// <see cref="UnifiedRequestEntryDto.RequestId"/> that was previously added via
    /// <see cref="ISessionLogWorkflow.CreateTurnAsync"/>.
    /// </summary>
    public required string RequestId { get; set; }

    /// <summary>
    /// Gets or sets the agent response text, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Gets or sets the updated interpretation text, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public string? Interpretation { get; set; }

    /// <summary>
    /// Gets or sets the updated turn status, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the updated turn model identifier, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the updated approximate token count, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public int? TokenCount { get; set; }

    /// <summary>
    /// Gets or sets the updated model-provider identifier, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public string? ModelProvider { get; set; }

    /// <summary>
    /// Gets or sets the updated failure note, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public string? FailureNote { get; set; }

    /// <summary>
    /// Gets or sets the updated success score, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// Gets or sets the updated premium-capacity flag, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public bool? IsPremium { get; set; }

    /// <summary>
    /// Gets or sets the updated tags, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets the updated context list, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public List<string>? ContextList { get; set; }

    /// <summary>
    /// Gets or sets the actions taken during this turn, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public List<UnifiedActionDto>? Actions { get; set; }

    /// <summary>
    /// Gets or sets a complete replacement processing-dialog list, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public List<ProcessingDialogItemDto>? ProcessingDialog { get; set; }

    /// <summary>
    /// Gets or sets file paths modified during this turn, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public List<string>? FilesModified { get; set; }

    /// <summary>
    /// Gets or sets design decisions made during this turn, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public List<string>? DesignDecisions { get; set; }

    /// <summary>
    /// Gets or sets requirement IDs discovered or referenced during this turn, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public List<string>? RequirementsDiscovered { get; set; }

    /// <summary>
    /// Gets or sets blockers or issues encountered during this turn, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public List<string>? Blockers { get; set; }
}
