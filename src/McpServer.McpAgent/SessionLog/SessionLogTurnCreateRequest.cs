namespace McpServer.McpAgent.SessionLog;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-006: Parameters for creating a new request entry (turn) within the active session-log workflow context.
/// </summary>
public sealed class SessionLogTurnCreateRequest
{
    /// <summary>
    /// Gets or sets an optional caller-supplied request identifier. When <see langword="null"/>,
    /// a canonical identifier is generated via <see cref="IMcpSessionIdentifierFactory.CreateRequestId"/>.
    /// When supplied, the value must pass <see cref="IMcpSessionIdentifierFactory.TryValidateRequestId"/>.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets or sets an optional suffix seed used when generating a canonical request identifier.
    /// When <see langword="null"/>, the workflow falls back to <see cref="QueryTitle"/>,
    /// then <see cref="QueryText"/>, then <c>turn</c>.
    /// </summary>
    public string? RequestIdSuffix { get; set; }

    /// <summary>
    /// Gets or sets the full user query text for this turn.
    /// </summary>
    public string? QueryText { get; set; }

    /// <summary>
    /// Gets or sets a short title summarising the user query.
    /// </summary>
    public string? QueryTitle { get; set; }

    /// <summary>
    /// Gets or sets the agent interpretation of the turn request.
    /// </summary>
    public string? Interpretation { get; set; }

    /// <summary>
    /// Gets or sets an initial response payload when a host creates a turn from pre-existing state.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Gets or sets the AI model used for this turn, falling back to the session-level model when <see langword="null"/>.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the initial turn status. Defaults to <c>in_progress</c>.
    /// </summary>
    public string Status { get; set; } = "in_progress";

    /// <summary>
    /// Gets or sets the approximate token count for this turn.
    /// </summary>
    public int? TokenCount { get; set; }

    /// <summary>
    /// Gets or sets the model-provider identifier used for this turn.
    /// </summary>
    public string? ModelProvider { get; set; }

    /// <summary>
    /// Gets or sets a failure note when the turn is created from an already failed state.
    /// </summary>
    public string? FailureNote { get; set; }

    /// <summary>
    /// Gets or sets an optional success score for the turn.
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the turn consumed premium capacity.
    /// </summary>
    public bool? IsPremium { get; set; }

    /// <summary>
    /// Gets or sets an optional list of tags for this turn.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets referenced files or resources used as context for this turn.
    /// </summary>
    public List<string>? ContextList { get; set; }

    /// <summary>
    /// Gets or sets design decisions recorded when the turn is created from pre-existing state.
    /// </summary>
    public List<string>? DesignDecisions { get; set; }

    /// <summary>
    /// Gets or sets requirement IDs discovered when the turn is created from pre-existing state.
    /// </summary>
    public List<string>? RequirementsDiscovered { get; set; }

    /// <summary>
    /// Gets or sets file paths modified when the turn is created from pre-existing state.
    /// </summary>
    public List<string>? FilesModified { get; set; }

    /// <summary>
    /// Gets or sets blockers recorded when the turn is created from pre-existing state.
    /// </summary>
    public List<string>? Blockers { get; set; }
}
