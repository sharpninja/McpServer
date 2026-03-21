namespace McpServer.McpAgent.SessionLog;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Parameters for completing an active turn in the built-in
/// session-log workflow.
/// </summary>
public sealed class SessionLogTurnCompleteRequest
{
    /// <summary>
    /// Gets or sets the identifier of the request entry to complete.
    /// </summary>
    public required string RequestId { get; set; }

    /// <summary>
    /// Gets or sets the final response recorded for the turn.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Gets or sets the final interpretation recorded for the turn.
    /// </summary>
    public string? Interpretation { get; set; }

    /// <summary>
    /// Gets or sets the final turn model identifier.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the final model-provider identifier.
    /// </summary>
    public string? ModelProvider { get; set; }

    /// <summary>
    /// Gets or sets the final token count.
    /// </summary>
    public int? TokenCount { get; set; }

    /// <summary>
    /// Gets or sets the final success score.
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the completed turn consumed premium capacity.
    /// </summary>
    public bool? IsPremium { get; set; }

    /// <summary>
    /// Gets or sets tags to replace on the completed turn.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets context items to replace on the completed turn.
    /// </summary>
    public List<string>? ContextList { get; set; }

    /// <summary>
    /// Gets or sets file paths modified during the completed turn.
    /// </summary>
    public List<string>? FilesModified { get; set; }

    /// <summary>
    /// Gets or sets design decisions recorded during the completed turn.
    /// </summary>
    public List<string>? DesignDecisions { get; set; }

    /// <summary>
    /// Gets or sets requirement IDs discovered during the completed turn.
    /// </summary>
    public List<string>? RequirementsDiscovered { get; set; }

    /// <summary>
    /// Gets or sets blockers that remain after the completed turn.
    /// </summary>
    public List<string>? Blockers { get; set; }
}
