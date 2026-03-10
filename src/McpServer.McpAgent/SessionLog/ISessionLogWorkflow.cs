namespace McpServer.McpAgent.SessionLog;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Built-in session-log workflow service that bootstraps and maintains an
/// in-memory <see cref="SessionLogWorkflowContext"/> for within-host continuation and persists every change to the MCP Server via
/// <see cref="McpServer.Client.SessionLogClient.SubmitAsync"/>.
/// <para>
/// Typical usage:
/// <list type="number">
///   <item><description>Call <see cref="BootstrapAsync"/> once to create the session and obtain the workflow context.</description></item>
///   <item><description>Call <see cref="BeginTurnAsync"/> or <see cref="CreateTurnAsync"/> at the start of each agent turn.</description></item>
///   <item><description>Call <see cref="AppendDialogAsync"/> and <see cref="AppendActionsAsync"/> while work is in progress.</description></item>
///   <item><description>Call <see cref="CompleteTurnAsync"/> or <see cref="FailTurnAsync"/> to finish the turn.</description></item>
///   <item><description>Call <see cref="PersistAsync"/> or <see cref="UpdateSessionAsync"/> when session-level state changes.</description></item>
/// </list>
/// The returned context supports continuation within the current host process. Resuming a session
/// by session ID alone is not currently supported because <c>McpServer.Client</c> does not expose
/// direct session lookup by identifier.
/// </para>
/// </summary>
public interface ISessionLogWorkflow
{
    /// <summary>
    /// Gets the current in-memory workflow context, or <see langword="null"/> before
    /// <see cref="BootstrapAsync"/> has been called.
    /// </summary>
    SessionLogWorkflowContext? Context { get; }

    /// <summary>
    /// Bootstraps a new session log, generating a canonical session identifier when
    /// <see cref="SessionLogBootstrapRequest.SessionId"/> is <see langword="null"/>, and
    /// submitting the initial log entry to the MCP Server.
    /// </summary>
    /// <param name="request">Bootstrap parameters for the session.</param>
    /// <param name="cancellationToken">Cancellation token for the HTTP call.</param>
    /// <returns>The newly created <see cref="SessionLogWorkflowContext"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="SessionLogBootstrapRequest.SessionId"/> is supplied but fails
    /// canonical validation.
    /// </exception>
    Task<SessionLogWorkflowContext> BootstrapAsync(
        SessionLogBootstrapRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates session-level metadata on the current context and resubmits the full session log.
    /// Only non-<see langword="null"/> properties on <paramref name="request"/> are applied.
    /// </summary>
    /// <param name="request">Fields to update.</param>
    /// <param name="cancellationToken">Cancellation token for the HTTP call.</param>
    /// <returns>The updated <see cref="SessionLogWorkflowContext"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="BootstrapAsync"/> has not yet been called.</exception>
    Task<SessionLogWorkflowContext> UpdateSessionAsync(
        SessionLogSessionUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the current in-memory session-log context without otherwise changing session metadata.
    /// Use this when a host wants an explicit checkpoint after a series of in-memory updates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the HTTP call.</param>
    /// <returns>The persisted <see cref="SessionLogWorkflowContext"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="BootstrapAsync"/> has not yet been called.</exception>
    Task<SessionLogWorkflowContext> PersistAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new request entry (turn) and returns the strongly typed in-memory turn context.
    /// This is the preferred host-facing API when the host intends to continue mutating the turn
    /// within the current process.
    /// </summary>
    /// <param name="request">Parameters for the new turn.</param>
    /// <param name="cancellationToken">Cancellation token for the HTTP call.</param>
    /// <returns>The newly created <see cref="SessionLogTurnContext"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="BootstrapAsync"/> has not yet been called.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="SessionLogTurnCreateRequest.RequestId"/> is supplied but fails
    /// canonical validation.
    /// </exception>
    Task<SessionLogTurnContext> BeginTurnAsync(
        SessionLogTurnCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new request entry (turn) within the current session, generating a canonical
    /// request identifier when <see cref="SessionLogTurnCreateRequest.RequestId"/> is
    /// <see langword="null"/>, and resubmits the full session log. This method is equivalent to
    /// <see cref="BeginTurnAsync"/> but returns the session context for callers that want to chain
    /// workflow operations through the enclosing session state.
    /// </summary>
    /// <param name="request">Parameters for the new turn.</param>
    /// <param name="cancellationToken">Cancellation token for the HTTP call.</param>
    /// <returns>The updated <see cref="SessionLogWorkflowContext"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="BootstrapAsync"/> has not yet been called.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="SessionLogTurnCreateRequest.RequestId"/> is supplied but fails
    /// canonical validation.
    /// </exception>
    Task<SessionLogWorkflowContext> CreateTurnAsync(
        SessionLogTurnCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends processing-dialog items to an existing turn by using
    /// <see cref="McpServer.Client.SessionLogClient.AppendDialogAsync"/>
    /// and mirroring the appended items into the in-memory turn context.
    /// </summary>
    /// <param name="request">The dialog-append parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the HTTP call.</param>
    /// <returns>The updated <see cref="SessionLogTurnContext"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the request identifier or dialog payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="BootstrapAsync"/> has not yet been called, or when the requested turn does not exist.
    /// </exception>
    Task<SessionLogTurnContext> AppendDialogAsync(
        SessionLogDialogAppendRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends one or more ordered actions to an existing turn and resubmits the full session log.
    /// </summary>
    /// <param name="request">The action-append parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the HTTP call.</param>
    /// <returns>The updated <see cref="SessionLogTurnContext"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the request identifier or action payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="BootstrapAsync"/> has not yet been called, or when the requested turn does not exist.
    /// </exception>
    Task<SessionLogTurnContext> AppendActionsAsync(
        SessionLogActionAppendRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing request entry (turn) identified by
    /// <see cref="SessionLogTurnUpdateRequest.RequestId"/> and resubmits the full session log.
    /// Only non-<see langword="null"/> properties on <paramref name="request"/> are applied.
    /// </summary>
    /// <param name="request">Fields to update on the matching entry.</param>
    /// <param name="cancellationToken">Cancellation token for the HTTP call.</param>
    /// <returns>The updated <see cref="SessionLogWorkflowContext"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="BootstrapAsync"/> has not yet been called, or when
    /// <see cref="SessionLogTurnUpdateRequest.RequestId"/> does not match any entry in the context.
    /// </exception>
    Task<SessionLogWorkflowContext> UpdateTurnAsync(
        SessionLogTurnUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing turn as completed and resubmits the full session log.
    /// </summary>
    /// <param name="request">Completion parameters for the target turn.</param>
    /// <param name="cancellationToken">Cancellation token for the HTTP call.</param>
    /// <returns>The updated <see cref="SessionLogTurnContext"/>.</returns>
    Task<SessionLogTurnContext> CompleteTurnAsync(
        SessionLogTurnCompleteRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing turn as failed, records a failure note, and resubmits the full session log.
    /// </summary>
    /// <param name="request">Failure parameters for the target turn.</param>
    /// <param name="cancellationToken">Cancellation token for the HTTP call.</param>
    /// <returns>The updated <see cref="SessionLogTurnContext"/>.</returns>
    Task<SessionLogTurnContext> FailTurnAsync(
        SessionLogTurnFailureRequest request,
        CancellationToken cancellationToken = default);
}
