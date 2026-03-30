namespace McpServer.Repl.Core;

/// <summary>
/// Defines the canonical Session Log workflow operations for agent-driven audit trails.
/// All operations enforce structured identifier rules and turn lifecycle state transitions.
/// </summary>
/// <remarks>
/// <para><strong>Canonical Identifier Rules:</strong></para>
/// <list type="bullet">
/// <item>
/// <term>agent</term>
/// <description>Agent name in PascalCase (e.g., "Copilot", "Cline", "Cursor"). Must match the sourceType prefix in sessionId.</description>
/// </item>
/// <item>
/// <term>sessionId</term>
/// <description>Format: <c>&lt;Agent&gt;-&lt;yyyyMMddTHHmmssZ&gt;-&lt;suffix&gt;</c>. Regex: <c>^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$</c></description>
/// </item>
/// <item>
/// <term>requestId</term>
/// <description>Format: <c>req-&lt;yyyyMMddTHHmmssZ&gt;-&lt;slugOrOrdinal&gt;</c>. Regex: <c>^req-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$</c></description>
/// </item>
/// </list>
/// <para>
/// Valid examples:
/// <list type="bullet">
/// <item>sessionId: <c>Copilot-20260304T113901Z-namingconv</c></item>
/// <item>requestId: <c>req-20260304T113901Z-plan-namingconventions-001</c></item>
/// </list>
/// </para>
/// <para>
/// Invalid examples:
/// <list type="bullet">
/// <item>sessionId: <c>copilot-20260304T113901Z-namingconv</c> (lowercase prefix)</item>
/// <item>requestId: <c>req-plan-namingconventions-001</c> (missing timestamp)</item>
/// </list>
/// </para>
/// <para><strong>Turn Lifecycle State Transitions:</strong></para>
/// <list type="number">
/// <item>
/// <term>Created (in_progress)</term>
/// <description>Turn initiated via <see cref="BeginTurnAsync"/>. Initial state allows dialog and action appends.</description>
/// </item>
/// <item>
/// <term>Updated (in_progress)</term>
/// <description>Turn modified via <see cref="UpdateTurnAsync"/>, <see cref="AppendDialogAsync"/>, or <see cref="AppendActionsAsync"/>. Remains mutable.</description>
/// </item>
/// <item>
/// <term>Completed (completed)</term>
/// <description>Turn finalized via <see cref="CompleteTurnAsync"/>. Immutable; no further modifications allowed.</description>
/// </item>
/// <item>
/// <term>Failed (failed)</term>
/// <description>Turn marked as failed via <see cref="FailTurnAsync"/>. Immutable; captures error state.</description>
/// </item>
/// </list>
/// </remarks>
public interface ISessionLogWorkflow
{
    /// <summary>
    /// Bootstraps the session log subsystem and prepares for workflow operations.
    /// This operation is idempotent and safe to call multiple times.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous bootstrap operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if bootstrap fails due to configuration or storage errors.</exception>
    Task BootstrapAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a new session log for the specified agent with the given metadata.
    /// The sessionId must conform to canonical identifier rules.
    /// </summary>
    /// <param name="agent">The agent name in PascalCase (e.g., "Copilot", "Cline", "Cursor").</param>
    /// <param name="sessionId">The unique session identifier conforming to <c>&lt;Agent&gt;-&lt;yyyyMMddTHHmmssZ&gt;-&lt;suffix&gt;</c> format.</param>
    /// <param name="title">A brief session summary.</param>
    /// <param name="model">The AI model name (e.g., "claude-sonnet-4-20250514").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous session creation operation.</returns>
    /// <exception cref="ArgumentException">Thrown if agent, sessionId, title, or model is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a session with the same sessionId already exists.</exception>
    Task OpenSessionAsync(string agent, string sessionId, string title, string model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current active session state.
    /// Returns null if no session is currently active.
    /// </summary>
    /// <returns>The current session state, or null if no session is active.</returns>
    ISessionLogState? CurrentSession();

    /// <summary>
    /// Begins a new turn within the current active session.
    /// The requestId must conform to canonical identifier rules.
    /// The turn is created with status "in_progress".
    /// </summary>
    /// <param name="requestId">The unique request identifier conforming to <c>req-&lt;yyyyMMddTHHmmssZ&gt;-&lt;slugOrOrdinal&gt;</c> format.</param>
    /// <param name="queryTitle">A short summary of the user query.</param>
    /// <param name="queryText">The full user query or task description.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous turn creation operation.</returns>
    /// <exception cref="ArgumentException">Thrown if requestId, queryTitle, or queryText is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no session is active or a turn with the same requestId already exists in the session.</exception>
    Task BeginTurnAsync(string requestId, string queryTitle, string queryText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the current active turn with response text, interpretation, and optional metadata.
    /// The turn must be in "in_progress" status.
    /// </summary>
    /// <param name="response">The agent's response text. If null, the existing response is preserved.</param>
    /// <param name="interpretation">The agent's understanding of the query. If null, the existing interpretation is preserved.</param>
    /// <param name="tokenCount">Approximate token count for this turn. If null, the existing tokenCount is preserved.</param>
    /// <param name="tags">Tags to add to the turn (e.g., "refactor", "bugfix"). If null, existing tags are preserved.</param>
    /// <param name="contextList">Files or resources referenced in this turn. If null, existing contextList is preserved.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous turn update operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no turn is active or the turn is already completed/failed.</exception>
    Task UpdateTurnAsync(
        string? response = null,
        string? interpretation = null,
        int? tokenCount = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<string>? contextList = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the current active turn as completed with a final response.
    /// Transitions the turn from "in_progress" to "completed" status.
    /// Once completed, the turn becomes immutable.
    /// </summary>
    /// <param name="response">The final response text.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous turn completion operation.</returns>
    /// <exception cref="ArgumentException">Thrown if response is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no turn is active or the turn is already completed/failed.</exception>
    Task CompleteTurnAsync(string response, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the current active turn as failed with an error message and optional error code.
    /// Transitions the turn from "in_progress" to "failed" status.
    /// Once failed, the turn becomes immutable.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="errorCode">Optional error code categorizing the failure (e.g., "invalid_workspace", "auth_failed").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous turn failure operation.</returns>
    /// <exception cref="ArgumentException">Thrown if errorMessage is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no turn is active or the turn is already completed/failed.</exception>
    Task FailTurnAsync(string errorMessage, string? errorCode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends dialog items (reasoning, tool calls, observations, decisions) to the current active turn.
    /// The turn must be in "in_progress" status.
    /// </summary>
    /// <param name="dialogItems">The dialog items to append.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous dialog append operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if dialogItems is null.</exception>
    /// <exception cref="ArgumentException">Thrown if dialogItems is empty or contains invalid items.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no turn is active or the turn is already completed/failed.</exception>
    Task AppendDialogAsync(IReadOnlyList<IDialogItem> dialogItems, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends actions (file edits, design decisions, commits, etc.) to the current active turn.
    /// The turn must be in "in_progress" status.
    /// </summary>
    /// <param name="actions">The actions to append.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous action append operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if actions is null.</exception>
    /// <exception cref="ArgumentException">Thrown if actions is empty or contains invalid items.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no turn is active or the turn is already completed/failed.</exception>
    Task AppendActionsAsync(IReadOnlyList<ISessionAction> actions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the session log history with optional filtering and pagination.
    /// Returns session logs matching the specified criteria.
    /// </summary>
    /// <param name="agent">Optional agent name filter. If null or empty, returns logs for all agents.</param>
    /// <param name="limit">Maximum number of sessions to return. Default is 10.</param>
    /// <param name="offset">Number of sessions to skip for pagination. Default is 0.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous query operation, containing the matching session logs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if limit or offset is negative.</exception>
    Task<IReadOnlyList<ISessionLogSummary>> QueryHistoryAsync(
        string? agent = null,
        int limit = 10,
        int offset = 0,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the runtime state of the active session and turn.
/// Used to track the current session and turn context for workflow operations.
/// </summary>
public interface ISessionLogState
{
    /// <summary>
    /// Gets the agent name for the active session.
    /// </summary>
    string Agent { get; }

    /// <summary>
    /// Gets the unique session identifier.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the session title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the AI model name used for the session.
    /// </summary>
    string Model { get; }

    /// <summary>
    /// Gets the timestamp when the session started.
    /// </summary>
    DateTimeOffset Started { get; }

    /// <summary>
    /// Gets the timestamp of the last update to the session.
    /// </summary>
    DateTimeOffset LastUpdated { get; }

    /// <summary>
    /// Gets the current session status.
    /// Valid values: "in_progress", "completed".
    /// </summary>
    string Status { get; }

    /// <summary>
    /// Gets the unique request identifier for the current active turn.
    /// Returns null if no turn is currently active.
    /// </summary>
    string? CurrentTurnRequestId { get; }

    /// <summary>
    /// Gets the current turn status.
    /// Returns null if no turn is currently active.
    /// Valid values: "in_progress", "completed", "failed".
    /// </summary>
    string? CurrentTurnStatus { get; }

    /// <summary>
    /// Gets the total number of turns in this session.
    /// </summary>
    int TurnCount { get; }
}

/// <summary>
/// Represents a dialog item within a turn's processing dialog.
/// Used for streaming reasoning, tool calls, tool results, observations, and decisions.
/// </summary>
public interface IDialogItem
{
    /// <summary>
    /// Gets the timestamp when this dialog item was created.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the role that created this dialog item.
    /// Valid values: "model", "tool", "system", "user".
    /// </summary>
    string Role { get; }

    /// <summary>
    /// Gets the content of this dialog item.
    /// Can be reasoning text, tool output, observation, or decision description.
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Gets the category of this dialog item.
    /// Valid values: "reasoning", "tool_call", "tool_result", "observation", "decision".
    /// </summary>
    string Category { get; }
}

/// <summary>
/// Represents an action within a turn.
/// Actions capture file modifications, design decisions, commits, and other recorded operations.
/// </summary>
public interface ISessionAction
{
    /// <summary>
    /// Gets the sequence number of this action within the turn.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Gets a description of what was done in this action.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the action type.
    /// Valid values include: "edit", "create", "delete", "design_decision", "commit", "pr_comment",
    /// "issue_comment", "web_reference", "dependency_add", "license_violation", "origin_violation",
    /// "origin_review", "entity_violation", "copilot_invocation", "policy_change".
    /// See action-types.md for the canonical list.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Gets the current status of this action.
    /// Valid values: "completed", "in_progress", "failed".
    /// </summary>
    string Status { get; }

    /// <summary>
    /// Gets the file path affected by this action, if applicable.
    /// Returns an empty string if no file is involved.
    /// </summary>
    string FilePath { get; }
}

/// <summary>
/// Represents a summary of a session log in query results.
/// Used for history queries without returning full turn details.
/// </summary>
public interface ISessionLogSummary
{
    /// <summary>
    /// Gets the agent name.
    /// </summary>
    string Agent { get; }

    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the session title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the AI model name.
    /// </summary>
    string Model { get; }

    /// <summary>
    /// Gets the session start timestamp.
    /// </summary>
    DateTimeOffset Started { get; }

    /// <summary>
    /// Gets the last update timestamp.
    /// </summary>
    DateTimeOffset LastUpdated { get; }

    /// <summary>
    /// Gets the session status.
    /// Valid values: "in_progress", "completed".
    /// </summary>
    string Status { get; }

    /// <summary>
    /// Gets the total number of turns in this session.
    /// </summary>
    int TurnCount { get; }

    /// <summary>
    /// Gets the tags applied across all turns in this session.
    /// </summary>
    IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the total number of files modified in this session.
    /// </summary>
    int FilesModifiedCount { get; }
}
