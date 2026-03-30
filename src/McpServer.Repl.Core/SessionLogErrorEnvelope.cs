namespace McpServer.Repl.Core;

/// <summary>
/// Defines structured error envelopes for Session Log workflow operations.
/// All errors follow the REPL protocol error envelope structure with standardized codes.
/// </summary>
/// <remarks>
/// <para>
/// Error envelope structure:
/// <code>
/// type: error
/// payload:
///   requestId: &lt;matching-request-id&gt;
///   code: &lt;error-code&gt;
///   message: &lt;human-readable-message&gt;
///   details:
///     &lt;optional-context-specific-details&gt;
/// </code>
/// </para>
/// <para>
/// Standard error codes for session log operations:
/// <list type="bullet">
/// <item><c>bootstrap_failed</c> — Bootstrap operation failed</item>
/// <item><c>session_not_found</c> — No active session exists</item>
/// <item><c>session_already_exists</c> — Session with same ID already exists</item>
/// <item><c>invalid_session_id</c> — Session ID violates canonical identifier rules</item>
/// <item><c>invalid_request_id</c> — Request ID violates canonical identifier rules</item>
/// <item><c>turn_not_found</c> — No active turn exists</item>
/// <item><c>turn_already_exists</c> — Turn with same request ID already exists</item>
/// <item><c>turn_immutable</c> — Turn is completed or failed and cannot be modified</item>
/// <item><c>invalid_turn_state</c> — Operation not allowed in current turn state</item>
/// <item><c>invalid_parameter</c> — Required parameter missing or invalid</item>
/// <item><c>storage_error</c> — Underlying storage operation failed</item>
/// <item><c>internal_error</c> — Unexpected internal error</item>
/// </list>
/// </para>
/// </remarks>
public interface ISessionLogError
{
    /// <summary>
    /// Gets the request ID that this error corresponds to.
    /// Must match the request ID from the failed command.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the error code indicating the failure category.
    /// See remarks for standard error codes.
    /// </summary>
    string Code { get; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets optional additional error details or context.
    /// Structure depends on the error code and operation.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Details { get; }
}

/// <summary>
/// Provides standard error code constants for Session Log operations.
/// </summary>
public static class SessionLogErrorCodes
{
    /// <summary>
    /// Bootstrap operation failed due to configuration or storage errors.
    /// </summary>
    public const string BootstrapFailed = "bootstrap_failed";

    /// <summary>
    /// No active session exists when attempting to perform a session-dependent operation.
    /// </summary>
    public const string SessionNotFound = "session_not_found";

    /// <summary>
    /// Attempted to create a session with an ID that already exists.
    /// </summary>
    public const string SessionAlreadyExists = "session_already_exists";

    /// <summary>
    /// Session ID does not conform to canonical identifier rules.
    /// Format: <c>&lt;Agent&gt;-&lt;yyyyMMddTHHmmssZ&gt;-&lt;suffix&gt;</c>
    /// Regex: <c>^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$</c>
    /// </summary>
    public const string InvalidSessionId = "invalid_session_id";

    /// <summary>
    /// Request ID does not conform to canonical identifier rules.
    /// Format: <c>req-&lt;yyyyMMddTHHmmssZ&gt;-&lt;slugOrOrdinal&gt;</c>
    /// Regex: <c>^req-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$</c>
    /// </summary>
    public const string InvalidRequestId = "invalid_request_id";

    /// <summary>
    /// No active turn exists when attempting to perform a turn-dependent operation.
    /// </summary>
    public const string TurnNotFound = "turn_not_found";

    /// <summary>
    /// Attempted to create a turn with a request ID that already exists in the current session.
    /// </summary>
    public const string TurnAlreadyExists = "turn_already_exists";

    /// <summary>
    /// Turn is in "completed" or "failed" status and cannot be modified.
    /// Turns become immutable after completion or failure.
    /// </summary>
    public const string TurnImmutable = "turn_immutable";

    /// <summary>
    /// Operation is not allowed in the current turn state.
    /// For example, attempting to complete a turn that is already failed.
    /// </summary>
    public const string InvalidTurnState = "invalid_turn_state";

    /// <summary>
    /// Required parameter is missing, empty, or contains invalid data.
    /// Check the error details for specific parameter information.
    /// </summary>
    public const string InvalidParameter = "invalid_parameter";

    /// <summary>
    /// Underlying storage operation (file I/O, database, etc.) failed.
    /// Check the error details for specific storage error information.
    /// </summary>
    public const string StorageError = "storage_error";

    /// <summary>
    /// Unexpected internal error occurred during operation.
    /// This indicates a bug or unhandled edge case.
    /// </summary>
    public const string InternalError = "internal_error";
}

/// <summary>
/// Example error envelopes for common Session Log error scenarios.
/// These examples demonstrate the YAML structure for different error conditions.
/// </summary>
/// <remarks>
/// <para><strong>Example 1: Bootstrap Failed</strong></para>
/// <code>
/// type: error
/// payload:
///   requestId: req-20260304T113901Z-bootstrap-001
///   code: bootstrap_failed
///   message: Failed to initialize session log storage
///   details:
///     storageError: Unable to create directory /path/to/sessionlogs
///     permissions: read-only
/// </code>
/// <para><strong>Example 2: Invalid Session ID</strong></para>
/// <code>
/// type: error
/// payload:
///   requestId: req-20260304T113901Z-open-001
///   code: invalid_session_id
///   message: Session ID does not conform to canonical format
///   details:
///     providedId: copilot-20260304-feature-auth
///     expectedFormat: &lt;Agent&gt;-&lt;yyyyMMddTHHmmssZ&gt;-&lt;suffix&gt;
///     regex: ^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$
/// </code>
/// <para><strong>Example 3: Session Not Found</strong></para>
/// <code>
/// type: error
/// payload:
///   requestId: req-20260304T113901Z-beginturn-001
///   code: session_not_found
///   message: No active session exists
///   details:
///     operation: beginTurn
///     hint: Call workflow.sessionlog.openSession first
/// </code>
/// <para><strong>Example 4: Turn Immutable</strong></para>
/// <code>
/// type: error
/// payload:
///   requestId: req-20260304T113901Z-update-001
///   code: turn_immutable
///   message: Cannot modify turn with status 'completed'
///   details:
///     requestId: req-20260304T113901Z-add-jwt-001
///     currentStatus: completed
///     completedAt: 2026-03-04T11:50:00Z
/// </code>
/// <para><strong>Example 5: Invalid Parameter</strong></para>
/// <code>
/// type: error
/// payload:
///   requestId: req-20260304T113901Z-beginturn-001
///   code: invalid_parameter
///   message: Parameter 'queryText' cannot be null or empty
///   details:
///     parameter: queryText
///     value: null
///     constraint: required, non-empty string
/// </code>
/// <para><strong>Example 6: Turn Already Exists</strong></para>
/// <code>
/// type: error
/// payload:
///   requestId: req-20260304T113901Z-beginturn-001
///   code: turn_already_exists
///   message: Turn with request ID already exists in current session
///   details:
///     requestId: req-20260304T113901Z-add-jwt-001
///     sessionId: Copilot-20260304T113901Z-feature-auth
///     existingTurnStatus: completed
/// </code>
/// </remarks>
public static class SessionLogErrorExamples
{
    // This class contains only documentation in XML comments.
    // No implementation is needed; all examples are in the remarks section above.
}
