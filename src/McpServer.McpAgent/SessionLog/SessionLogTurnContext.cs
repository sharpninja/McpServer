using McpServer.Client.Models;

namespace McpServer.McpAgent.SessionLog;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Strongly typed in-memory state for a single session-log turn.
/// Hosts use this type to continue a session within the current process without constructing raw
/// transport DTOs.
/// </summary>
public sealed class SessionLogTurnContext
{
    private readonly List<UnifiedActionDto> _actions = [];
    private readonly List<string> _blockers = [];
    private readonly List<string> _contextList = [];
    private readonly List<string> _designDecisions = [];
    private readonly List<string> _filesModified = [];
    private readonly List<ProcessingDialogItemDto> _processingDialog = [];
    private readonly List<string> _requirementsDiscovered = [];
    private readonly List<string> _tags = [];

    internal SessionLogTurnContext(string requestId, string timestamp)
    {
        RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
        Timestamp = timestamp ?? throw new ArgumentNullException(nameof(timestamp));
    }

    /// <summary>
    /// Gets the canonical request identifier assigned to the turn.
    /// </summary>
    public string RequestId { get; }

    /// <summary>
    /// Gets the ISO 8601 timestamp captured when the turn started.
    /// </summary>
    public string Timestamp { get; }

    /// <summary>
    /// Gets the full user query text associated with the turn.
    /// </summary>
    public string? QueryText { get; internal set; }

    /// <summary>
    /// Gets the short user-query title associated with the turn.
    /// </summary>
    public string? QueryTitle { get; internal set; }

    /// <summary>FR-MCP-SESSIONLOGCTX-001: Current plan file or <c>None</c>.</summary>
    public string? PlanFile { get; internal set; }

    /// <summary>FR-MCP-SESSIONLOGCTX-001: Current MCP TODO id or <c>None</c>.</summary>
    public string? TodoId { get; internal set; }

    /// <summary>
    /// Gets the most recent response text recorded for the turn.
    /// </summary>
    public string? Response { get; internal set; }

    /// <summary>
    /// Gets the most recent interpretation text recorded for the turn.
    /// </summary>
    public string? Interpretation { get; internal set; }

    /// <summary>
    /// Gets the current turn status (for example <c>in_progress</c>, <c>completed</c>, or <c>failed</c>).
    /// </summary>
    public string? Status { get; internal set; }

    /// <summary>
    /// Gets the model identifier recorded for the turn.
    /// </summary>
    public string? Model { get; internal set; }

    /// <summary>
    /// Gets the model-provider identifier recorded for the turn.
    /// </summary>
    public string? ModelProvider { get; internal set; }

    /// <summary>
    /// Gets the approximate token count recorded for the turn.
    /// </summary>
    public int? TokenCount { get; internal set; }

    /// <summary>
    /// Gets the failure note recorded for the turn, when present.
    /// </summary>
    public string? FailureNote { get; internal set; }

    /// <summary>
    /// Gets the success score recorded for the turn, when present.
    /// </summary>
    public double? Score { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the turn used premium capacity.
    /// </summary>
    public bool? IsPremium { get; internal set; }

    /// <summary>
    /// Gets the tags recorded for the turn.
    /// </summary>
    public IReadOnlyList<string> Tags => _tags;

    /// <summary>
    /// Gets the context items recorded for the turn.
    /// </summary>
    public IReadOnlyList<string> ContextList => _contextList;

    /// <summary>
    /// Gets the ordered actions recorded for the turn.
    /// </summary>
    public IReadOnlyList<UnifiedActionDto> Actions => _actions;

    /// <summary>
    /// Gets the processing-dialog items mirrored into the turn state.
    /// </summary>
    public IReadOnlyList<ProcessingDialogItemDto> ProcessingDialog => _processingDialog;

    /// <summary>
    /// Gets the design decisions recorded for the turn.
    /// </summary>
    public IReadOnlyList<string> DesignDecisions => _designDecisions;

    /// <summary>
    /// Gets the requirement IDs recorded for the turn.
    /// </summary>
    public IReadOnlyList<string> RequirementsDiscovered => _requirementsDiscovered;

    /// <summary>
    /// Gets the file paths recorded for the turn.
    /// </summary>
    public IReadOnlyList<string> FilesModified => _filesModified;

    /// <summary>
    /// Gets the blockers recorded for the turn.
    /// </summary>
    public IReadOnlyList<string> Blockers => _blockers;

    internal void ReplaceTags(IEnumerable<string>? tags) => ReplaceStringList(_tags, tags);

    internal void ReplaceContextList(IEnumerable<string>? contextList) => ReplaceStringList(_contextList, contextList);

    internal void ReplaceDesignDecisions(IEnumerable<string>? designDecisions) => ReplaceStringList(_designDecisions, designDecisions);

    internal void ReplaceRequirementsDiscovered(IEnumerable<string>? requirementsDiscovered) => ReplaceStringList(_requirementsDiscovered, requirementsDiscovered);

    internal void ReplaceFilesModified(IEnumerable<string>? filesModified) => ReplaceStringList(_filesModified, filesModified);

    internal void ReplaceBlockers(IEnumerable<string>? blockers) => ReplaceStringList(_blockers, blockers);

    internal void ReplaceActions(IEnumerable<UnifiedActionDto>? actions)
    {
        _actions.Clear();
        AppendActions(actions);
    }

    internal void AppendActions(IEnumerable<UnifiedActionDto>? actions)
    {
        if (actions is null)
            return;

        foreach (var action in actions)
        {
            ArgumentNullException.ThrowIfNull(action);
            _actions.Add(CloneAction(action, _actions.Count + 1));
        }
    }

    internal void ReplaceProcessingDialog(IEnumerable<ProcessingDialogItemDto>? items)
    {
        _processingDialog.Clear();
        AppendProcessingDialog(items);
    }

    internal void AppendProcessingDialog(IEnumerable<ProcessingDialogItemDto>? items)
    {
        if (items is null)
            return;

        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);
            _processingDialog.Add(CloneDialogItem(item));
        }
    }

    internal UnifiedRequestEntryDto ToDto() => new()
    {
        RequestId = RequestId,
        Timestamp = Timestamp,
        QueryText = QueryText,
        QueryTitle = QueryTitle,
        Response = Response,
        Interpretation = Interpretation,
        Status = Status,
        Actions = _actions.Count > 0 ? _actions.Select((action, index) => CloneAction(action, index + 1)).ToList() : null,
        Model = Model,
        ModelProvider = ModelProvider,
        TokenCount = TokenCount,
        Tags = _tags.Count > 0 ? [.. _tags] : null,
        ContextList = _contextList.Count > 0 ? [.. _contextList] : null,
        FailureNote = FailureNote,
        Score = Score,
        IsPremium = IsPremium,
        ProcessingDialog = _processingDialog.Count > 0 ? _processingDialog.Select(CloneDialogItem).ToList() : null,
        DesignDecisions = _designDecisions.Count > 0 ? [.. _designDecisions] : null,
        RequirementsDiscovered = _requirementsDiscovered.Count > 0 ? [.. _requirementsDiscovered] : null,
        FilesModified = _filesModified.Count > 0 ? [.. _filesModified] : null,
        Blockers = _blockers.Count > 0 ? [.. _blockers] : null,
        PlanFile = PlanFile,
        TodoId = TodoId,
    };

    private static void ReplaceStringList(List<string> target, IEnumerable<string>? values)
    {
        target.Clear();
        if (values is null)
            return;

        foreach (var value in values)
        {
            if (value is not null)
                target.Add(value);
        }
    }

    private static UnifiedActionDto CloneAction(UnifiedActionDto action, int order) => new()
    {
        Order = order,
        Description = action.Description,
        Type = action.Type,
        Status = action.Status,
        FilePath = action.FilePath,
    };

    private static ProcessingDialogItemDto CloneDialogItem(ProcessingDialogItemDto item) => new()
    {
        Timestamp = item.Timestamp,
        Role = item.Role,
        Content = item.Content,
        Category = item.Category,
    };
}
