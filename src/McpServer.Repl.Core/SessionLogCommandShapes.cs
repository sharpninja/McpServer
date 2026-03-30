namespace McpServer.Repl.Core;

/// <summary>
/// Defines YAML command shapes for the <c>workflow.sessionlog.*</c> namespace.
/// All commands follow the REPL protocol request envelope structure with method-specific parameters.
/// </summary>
/// <remarks>
/// <para>
/// Command methods in this namespace:
/// <list type="bullet">
/// <item><c>workflow.sessionlog.bootstrap</c> — Initialize session log subsystem</item>
/// <item><c>workflow.sessionlog.openSession</c> — Create new session</item>
/// <item><c>workflow.sessionlog.currentSession</c> — Get active session state</item>
/// <item><c>workflow.sessionlog.beginTurn</c> — Start new turn</item>
/// <item><c>workflow.sessionlog.updateTurn</c> — Modify active turn</item>
/// <item><c>workflow.sessionlog.completeTurn</c> — Finalize turn as completed</item>
/// <item><c>workflow.sessionlog.failTurn</c> — Finalize turn as failed</item>
/// <item><c>workflow.sessionlog.appendDialog</c> — Add dialog items to turn</item>
/// <item><c>workflow.sessionlog.appendActions</c> — Add actions to turn</item>
/// <item><c>workflow.sessionlog.queryHistory</c> — Query session log history</item>
/// </list>
/// </para>
/// <para>
/// All request envelopes follow the structure:
/// <code>
/// type: request
/// payload:
///   requestId: &lt;unique-request-id&gt;
///   method: workflow.sessionlog.&lt;operation&gt;
///   params:
///     &lt;operation-specific-parameters&gt;
/// </code>
/// </para>
/// <para>
/// All successful responses follow the structure:
/// <code>
/// type: result
/// payload:
///   requestId: &lt;matching-request-id&gt;
///   result:
///     &lt;operation-specific-result&gt;
/// </code>
/// </para>
/// <para>
/// All error responses follow the structure defined in <see cref="ISessionLogError"/>.
/// </para>
/// </remarks>
public static class SessionLogCommandShapes
{
    /// <summary>
    /// The namespace prefix for all session log workflow commands.
    /// </summary>
    public const string MethodNamespace = "workflow.sessionlog";

    /// <summary>
    /// Command method for bootstrapping the session log subsystem.
    /// Method: <c>workflow.sessionlog.bootstrap</c>
    /// </summary>
    public const string BootstrapMethod = "workflow.sessionlog.bootstrap";

    /// <summary>
    /// Command method for opening a new session.
    /// Method: <c>workflow.sessionlog.openSession</c>
    /// </summary>
    public const string OpenSessionMethod = "workflow.sessionlog.openSession";

    /// <summary>
    /// Command method for retrieving the current active session state.
    /// Method: <c>workflow.sessionlog.currentSession</c>
    /// </summary>
    public const string CurrentSessionMethod = "workflow.sessionlog.currentSession";

    /// <summary>
    /// Command method for beginning a new turn.
    /// Method: <c>workflow.sessionlog.beginTurn</c>
    /// </summary>
    public const string BeginTurnMethod = "workflow.sessionlog.beginTurn";

    /// <summary>
    /// Command method for updating the active turn.
    /// Method: <c>workflow.sessionlog.updateTurn</c>
    /// </summary>
    public const string UpdateTurnMethod = "workflow.sessionlog.updateTurn";

    /// <summary>
    /// Command method for completing the active turn.
    /// Method: <c>workflow.sessionlog.completeTurn</c>
    /// </summary>
    public const string CompleteTurnMethod = "workflow.sessionlog.completeTurn";

    /// <summary>
    /// Command method for failing the active turn.
    /// Method: <c>workflow.sessionlog.failTurn</c>
    /// </summary>
    public const string FailTurnMethod = "workflow.sessionlog.failTurn";

    /// <summary>
    /// Command method for appending dialog items to the active turn.
    /// Method: <c>workflow.sessionlog.appendDialog</c>
    /// </summary>
    public const string AppendDialogMethod = "workflow.sessionlog.appendDialog";

    /// <summary>
    /// Command method for appending actions to the active turn.
    /// Method: <c>workflow.sessionlog.appendActions</c>
    /// </summary>
    public const string AppendActionsMethod = "workflow.sessionlog.appendActions";

    /// <summary>
    /// Command method for querying session log history.
    /// Method: <c>workflow.sessionlog.queryHistory</c>
    /// </summary>
    public const string QueryHistoryMethod = "workflow.sessionlog.queryHistory";
}

/// <summary>
/// Represents the parameters for the <c>workflow.sessionlog.bootstrap</c> command.
/// This command takes no parameters.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-bootstrap-001
///   method: workflow.sessionlog.bootstrap
///   params: {}
/// </code>
/// </remarks>
public interface IBootstrapParams
{
    // No parameters for bootstrap operation
}

/// <summary>
/// Represents the result for the <c>workflow.sessionlog.bootstrap</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-bootstrap-001
///   result:
///     initialized: true
/// </code>
/// </remarks>
public interface IBootstrapResult
{
    /// <summary>
    /// Gets whether the bootstrap operation completed successfully.
    /// </summary>
    bool Initialized { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.sessionlog.openSession</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-open-001
///   method: workflow.sessionlog.openSession
///   params:
///     agent: Copilot
///     sessionId: Copilot-20260304T113901Z-feature-auth
///     title: Implementing JWT authentication
///     model: claude-sonnet-4-20250514
/// </code>
/// </remarks>
public interface IOpenSessionParams
{
    /// <summary>
    /// Gets the agent name in PascalCase (e.g., "Copilot", "Cline", "Cursor").
    /// </summary>
    string Agent { get; }

    /// <summary>
    /// Gets the unique session identifier conforming to canonical rules.
    /// Format: <c>&lt;Agent&gt;-&lt;yyyyMMddTHHmmssZ&gt;-&lt;suffix&gt;</c>
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the session title (brief summary).
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the AI model name (e.g., "claude-sonnet-4-20250514").
    /// </summary>
    string Model { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.sessionlog.openSession</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-open-001
///   result:
///     sessionId: Copilot-20260304T113901Z-feature-auth
///     started: 2026-03-04T11:39:01Z
/// </code>
/// </remarks>
public interface IOpenSessionResult
{
    /// <summary>
    /// Gets the session identifier that was created.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the timestamp when the session started.
    /// </summary>
    DateTimeOffset Started { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.sessionlog.currentSession</c> command.
/// This command takes no parameters.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-current-001
///   method: workflow.sessionlog.currentSession
///   params: {}
/// </code>
/// </remarks>
public interface ICurrentSessionParams
{
    // No parameters for currentSession operation
}

/// <summary>
/// Represents the result for the <c>workflow.sessionlog.currentSession</c> command.
/// Returns null/empty if no session is active.
/// </summary>
/// <remarks>
/// Example YAML when session is active:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-current-001
///   result:
///     agent: Copilot
///     sessionId: Copilot-20260304T113901Z-feature-auth
///     title: Implementing JWT authentication
///     model: claude-sonnet-4-20250514
///     started: 2026-03-04T11:39:01Z
///     lastUpdated: 2026-03-04T11:45:23Z
///     status: in_progress
///     currentTurnRequestId: req-20260304T113901Z-add-jwt-001
///     currentTurnStatus: in_progress
///     turnCount: 3
/// </code>
/// Example YAML when no session is active:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-current-001
///   result: null
/// </code>
/// </remarks>
public interface ICurrentSessionResult
{
    /// <summary>
    /// Gets the agent name, or null if no session is active.
    /// </summary>
    string? Agent { get; }

    /// <summary>
    /// Gets the session identifier, or null if no session is active.
    /// </summary>
    string? SessionId { get; }

    /// <summary>
    /// Gets the session title, or null if no session is active.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets the AI model name, or null if no session is active.
    /// </summary>
    string? Model { get; }

    /// <summary>
    /// Gets the session start timestamp, or null if no session is active.
    /// </summary>
    DateTimeOffset? Started { get; }

    /// <summary>
    /// Gets the last update timestamp, or null if no session is active.
    /// </summary>
    DateTimeOffset? LastUpdated { get; }

    /// <summary>
    /// Gets the session status, or null if no session is active.
    /// Valid values: "in_progress", "completed".
    /// </summary>
    string? Status { get; }

    /// <summary>
    /// Gets the current turn request identifier, or null if no turn is active.
    /// </summary>
    string? CurrentTurnRequestId { get; }

    /// <summary>
    /// Gets the current turn status, or null if no turn is active.
    /// Valid values: "in_progress", "completed", "failed".
    /// </summary>
    string? CurrentTurnStatus { get; }

    /// <summary>
    /// Gets the total number of turns, or 0 if no session is active.
    /// </summary>
    int TurnCount { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.sessionlog.beginTurn</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-beginturn-001
///   method: workflow.sessionlog.beginTurn
///   params:
///     requestId: req-20260304T113901Z-add-jwt-001
///     queryTitle: Add JWT authentication
///     queryText: Implement JWT token generation and validation for the API
/// </code>
/// </remarks>
public interface IBeginTurnParams
{
    /// <summary>
    /// Gets the unique request identifier for the new turn.
    /// Format: <c>req-&lt;yyyyMMddTHHmmssZ&gt;-&lt;slugOrOrdinal&gt;</c>
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the short summary of the user query.
    /// </summary>
    string QueryTitle { get; }

    /// <summary>
    /// Gets the full user query or task description.
    /// </summary>
    string QueryText { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.sessionlog.beginTurn</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-beginturn-001
///   result:
///     requestId: req-20260304T113901Z-add-jwt-001
///     timestamp: 2026-03-04T11:45:23Z
///     status: in_progress
/// </code>
/// </remarks>
public interface IBeginTurnResult
{
    /// <summary>
    /// Gets the request identifier for the created turn.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the timestamp when the turn was created.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the initial turn status (always "in_progress").
    /// </summary>
    string Status { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.sessionlog.updateTurn</c> command.
/// All fields are optional; only provided fields are updated.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-update-001
///   method: workflow.sessionlog.updateTurn
///   params:
///     response: Created TokenService and JwtValidator classes
///     interpretation: User wants JWT authentication with token generation and validation
///     tokenCount: 1250
///     tags:
///       - feature
///       - security
///     contextList:
///       - src/TokenService.cs
///       - src/JwtValidator.cs
/// </code>
/// </remarks>
public interface IUpdateTurnParams
{
    /// <summary>
    /// Gets the agent's response text. Null preserves existing value.
    /// </summary>
    string? Response { get; }

    /// <summary>
    /// Gets the agent's interpretation. Null preserves existing value.
    /// </summary>
    string? Interpretation { get; }

    /// <summary>
    /// Gets the approximate token count. Null preserves existing value.
    /// </summary>
    int? TokenCount { get; }

    /// <summary>
    /// Gets tags to add. Null preserves existing tags.
    /// </summary>
    IReadOnlyList<string>? Tags { get; }

    /// <summary>
    /// Gets files/resources referenced. Null preserves existing contextList.
    /// </summary>
    IReadOnlyList<string>? ContextList { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.sessionlog.updateTurn</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-update-001
///   result:
///     updated: true
///     lastUpdated: 2026-03-04T11:46:15Z
/// </code>
/// </remarks>
public interface IUpdateTurnResult
{
    /// <summary>
    /// Gets whether the update succeeded.
    /// </summary>
    bool Updated { get; }

    /// <summary>
    /// Gets the timestamp of the update.
    /// </summary>
    DateTimeOffset LastUpdated { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.sessionlog.completeTurn</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-complete-001
///   method: workflow.sessionlog.completeTurn
///   params:
///     response: JWT authentication successfully implemented with token generation and validation
/// </code>
/// </remarks>
public interface ICompleteTurnParams
{
    /// <summary>
    /// Gets the final response text.
    /// </summary>
    string Response { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.sessionlog.completeTurn</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-complete-001
///   result:
///     requestId: req-20260304T113901Z-add-jwt-001
///     status: completed
///     completedAt: 2026-03-04T11:50:00Z
/// </code>
/// </remarks>
public interface ICompleteTurnResult
{
    /// <summary>
    /// Gets the request identifier for the completed turn.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the final turn status (always "completed").
    /// </summary>
    string Status { get; }

    /// <summary>
    /// Gets the timestamp when the turn was completed.
    /// </summary>
    DateTimeOffset CompletedAt { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.sessionlog.failTurn</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-fail-001
///   method: workflow.sessionlog.failTurn
///   params:
///     errorMessage: Unable to create TokenService due to missing dependencies
///     errorCode: dependency_missing
/// </code>
/// </remarks>
public interface IFailTurnParams
{
    /// <summary>
    /// Gets the error message describing the failure.
    /// </summary>
    string ErrorMessage { get; }

    /// <summary>
    /// Gets the optional error code categorizing the failure.
    /// Examples: "invalid_workspace", "auth_failed", "dependency_missing".
    /// </summary>
    string? ErrorCode { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.sessionlog.failTurn</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-fail-001
///   result:
///     requestId: req-20260304T113901Z-add-jwt-001
///     status: failed
///     failedAt: 2026-03-04T11:48:30Z
///     errorCode: dependency_missing
/// </code>
/// </remarks>
public interface IFailTurnResult
{
    /// <summary>
    /// Gets the request identifier for the failed turn.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the final turn status (always "failed").
    /// </summary>
    string Status { get; }

    /// <summary>
    /// Gets the timestamp when the turn failed.
    /// </summary>
    DateTimeOffset FailedAt { get; }

    /// <summary>
    /// Gets the error code, if provided.
    /// </summary>
    string? ErrorCode { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.sessionlog.appendDialog</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-appenddialog-001
///   method: workflow.sessionlog.appendDialog
///   params:
///     dialogItems:
///       - timestamp: 2026-03-04T11:46:00Z
///         role: model
///         content: Analyzing authentication requirements...
///         category: reasoning
///       - timestamp: 2026-03-04T11:46:05Z
///         role: tool
///         content: File created successfully
///         category: tool_result
/// </code>
/// </remarks>
public interface IAppendDialogParams
{
    /// <summary>
    /// Gets the dialog items to append to the active turn.
    /// </summary>
    IReadOnlyList<IDialogItem> DialogItems { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.sessionlog.appendDialog</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-appenddialog-001
///   result:
///     appended: 2
///     totalDialogItems: 15
/// </code>
/// </remarks>
public interface IAppendDialogResult
{
    /// <summary>
    /// Gets the number of dialog items appended.
    /// </summary>
    int Appended { get; }

    /// <summary>
    /// Gets the total number of dialog items in the turn after appending.
    /// </summary>
    int TotalDialogItems { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.sessionlog.appendActions</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-appendactions-001
///   method: workflow.sessionlog.appendActions
///   params:
///     actions:
///       - order: 1
///         description: Created TokenService class
///         type: create
///         status: completed
///         filePath: src/TokenService.cs
///       - order: 2
///         description: Created JwtValidator class
///         type: create
///         status: completed
///         filePath: src/JwtValidator.cs
/// </code>
/// </remarks>
public interface IAppendActionsParams
{
    /// <summary>
    /// Gets the actions to append to the active turn.
    /// </summary>
    IReadOnlyList<ISessionAction> Actions { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.sessionlog.appendActions</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-appendactions-001
///   result:
///     appended: 2
///     totalActions: 5
/// </code>
/// </remarks>
public interface IAppendActionsResult
{
    /// <summary>
    /// Gets the number of actions appended.
    /// </summary>
    int Appended { get; }

    /// <summary>
    /// Gets the total number of actions in the turn after appending.
    /// </summary>
    int TotalActions { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.sessionlog.queryHistory</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-query-001
///   method: workflow.sessionlog.queryHistory
///   params:
///     agent: Copilot
///     limit: 10
///     offset: 0
/// </code>
/// </remarks>
public interface IQueryHistoryParams
{
    /// <summary>
    /// Gets the optional agent name filter.
    /// If null or empty, returns logs for all agents.
    /// </summary>
    string? Agent { get; }

    /// <summary>
    /// Gets the maximum number of sessions to return.
    /// Default is 10.
    /// </summary>
    int Limit { get; }

    /// <summary>
    /// Gets the number of sessions to skip for pagination.
    /// Default is 0.
    /// </summary>
    int Offset { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.sessionlog.queryHistory</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-query-001
///   result:
///     sessions:
///       - agent: Copilot
///         sessionId: Copilot-20260304T113901Z-feature-auth
///         title: Implementing JWT authentication
///         model: claude-sonnet-4-20250514
///         started: 2026-03-04T11:39:01Z
///         lastUpdated: 2026-03-04T11:50:00Z
///         status: completed
///         turnCount: 3
///         tags:
///           - feature
///           - security
///         filesModifiedCount: 5
///     totalCount: 25
///     offset: 0
///     limit: 10
/// </code>
/// </remarks>
public interface IQueryHistoryResult
{
    /// <summary>
    /// Gets the session log summaries matching the query.
    /// </summary>
    IReadOnlyList<ISessionLogSummary> Sessions { get; }

    /// <summary>
    /// Gets the total number of sessions matching the filter (ignoring pagination).
    /// </summary>
    int TotalCount { get; }

    /// <summary>
    /// Gets the offset used in the query.
    /// </summary>
    int Offset { get; }

    /// <summary>
    /// Gets the limit used in the query.
    /// </summary>
    int Limit { get; }
}
