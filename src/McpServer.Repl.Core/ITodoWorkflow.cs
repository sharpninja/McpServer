namespace McpServer.Repl.Core;

/// <summary>
/// Defines the canonical TODO workflow operations for agent-driven task management.
/// All operations enforce TODO identifier rules, dependency tracking, and projection semantics.
/// </summary>
/// <remarks>
/// <para><strong>Canonical Identifier Rules:</strong></para>
/// <list type="bullet">
/// <item>
/// <term>TODO ID</term>
/// <description>Format: <c>&lt;PHASE&gt;-&lt;AREA&gt;-###</c> or <c>ISSUE-{number}</c>. Regex: <c>^[A-Z]+-[A-Z0-9]+-\d{3}$</c> or <c>^ISSUE-\d+$</c></description>
/// </item>
/// <item>
/// <term>Valid examples</term>
/// <description><c>PLAN-NAMINGCONVENTIONS-001</c>, <c>MCP-API-042</c>, <c>ISSUE-17</c></description>
/// </item>
/// <item>
/// <term>Invalid examples</term>
/// <description><c>plan-api-001</c>, <c>MCP-API-42</c>, <c>ISSUE-ABC</c>, <c>MCPAPI001</c></description>
/// </item>
/// <item>
/// <term>Special ID</term>
/// <description><c>ISSUE-NEW</c> for create requests. Server creates GitHub issue and returns canonical <c>ISSUE-{number}</c>.</description>
/// </item>
/// </list>
/// <para><strong>Selection State Convenience:</strong></para>
/// <para>
/// The workflow maintains a <see cref="ITodoSelectionState"/> tracking the currently selected TODO item.
/// Operations like <see cref="UpdateAsync(ITodoUpdateRequest, CancellationToken)"/>, 
/// <see cref="DeleteAsync(CancellationToken)"/>, and streaming methods can use the
/// selected TODO without passing the ID explicitly. This reduces command verbosity in interactive sessions
/// where agents work on one TODO at a time. Use <see cref="SelectAsync"/> to set the active TODO context,
/// and <see cref="GetProjectionStatusAsync"/> to check whether the selected TODO has a valid projection.
/// </para>
/// <para><strong>Projection and Streaming:</strong></para>
/// <para>
/// TODOs can have an associated "projection" containing status analysis, implementation plan, and execution state.
/// The <see cref="StreamStatusAsync"/>, <see cref="StreamPlanAsync"/>, and <see cref="StreamImplementAsync"/>
/// operations generate or update this projection, emitting events as they progress. Projections can become stale
/// or corrupted; use <see cref="RepairProjectionAsync"/> to rebuild them from source TODO data.
/// </para>
/// </remarks>
public interface ITodoWorkflow
{
    /// <summary>
    /// Queries TODO items with optional filtering and pagination.
    /// Returns all TODOs matching the specified criteria.
    /// </summary>
    /// <param name="keyword">Optional keyword filter for title/description search.</param>
    /// <param name="priority">Optional priority filter. Valid values: "critical", "high", "medium", "low".</param>
    /// <param name="section">Optional section filter (e.g., "Backend", "Frontend", "Infrastructure").</param>
    /// <param name="id">Optional exact TODO ID filter.</param>
    /// <param name="done">Optional completion status filter. If null, returns both completed and incomplete TODOs.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous query operation, containing matching TODOs.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the query fails due to storage errors.</exception>
    Task<ITodoQueryResult> QueryAsync(
        string? keyword = null,
        string? priority = null,
        string? section = null,
        string? id = null,
        bool? done = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific TODO item by its canonical identifier.
    /// </summary>
    /// <param name="id">The TODO identifier conforming to canonical rules.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous get operation, containing the TODO item.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TODO is not found or a storage error occurs.</exception>
    Task<ITodoItem> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects a TODO item as the active context for subsequent operations.
    /// Sets the <see cref="ITodoSelectionState"/> to the specified TODO.
    /// </summary>
    /// <param name="id">The TODO identifier to select.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous selection operation.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TODO is not found or a storage error occurs.</exception>
    /// <remarks>
    /// After selection, operations like <see cref="UpdateAsync(ITodoUpdateRequest, CancellationToken)"/> and
    /// <see cref="DeleteAsync(CancellationToken)"/> will target the selected TODO without requiring the ID parameter.
    /// </remarks>
    Task SelectAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new TODO item with the specified metadata.
    /// </summary>
    /// <param name="request">The TODO creation request with all required fields.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous creation operation, containing the created TODO.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    /// <exception cref="ArgumentException">Thrown if required fields are missing or violate identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a TODO with the same ID already exists or a storage error occurs.</exception>
    /// <remarks>
    /// If <see cref="ITodoCreateRequest.Id"/> is <c>ISSUE-NEW</c>, the server creates a GitHub issue and returns
    /// the TODO with a canonical <c>ISSUE-{number}</c> identifier.
    /// </remarks>
    Task<ITodoMutationResult> CreateAsync(ITodoCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing TODO item with the specified changes.
    /// </summary>
    /// <param name="id">The TODO identifier to update.</param>
    /// <param name="request">The update request containing fields to modify. Only provided fields are updated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous update operation, containing the updated TODO.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TODO is not found or a storage error occurs.</exception>
    Task<ITodoMutationResult> UpdateAsync(string id, ITodoUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the currently selected TODO item with the specified changes.
    /// Uses the TODO ID from <see cref="ITodoSelectionState"/>.
    /// </summary>
    /// <param name="request">The update request containing fields to modify. Only provided fields are updated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous update operation, containing the updated TODO.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no TODO is selected, the TODO is not found, or a storage error occurs.</exception>
    /// <remarks>
    /// Call <see cref="SelectAsync"/> first to set the active TODO context.
    /// </remarks>
    Task<ITodoMutationResult> UpdateAsync(ITodoUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific TODO item.
    /// </summary>
    /// <param name="id">The TODO identifier to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous deletion operation.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TODO is not found or a storage error occurs.</exception>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the currently selected TODO item.
    /// Uses the TODO ID from <see cref="ITodoSelectionState"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous deletion operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no TODO is selected, the TODO is not found, or a storage error occurs.</exception>
    /// <remarks>
    /// Call <see cref="SelectAsync"/> first to set the active TODO context.
    /// </remarks>
    Task DeleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes the requirements referenced by a TODO and returns traceability information.
    /// </summary>
    /// <param name="id">The TODO identifier to analyze.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous analysis operation, containing requirement details.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TODO is not found or a storage error occurs.</exception>
    Task<ITodoRequirementsAnalysis> AnalyzeRequirementsAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams status analysis events for a TODO item.
    /// Emits events as the analysis progresses, returning a final status summary.
    /// </summary>
    /// <param name="id">The TODO identifier to analyze.</param>
    /// <param name="eventCallback">Callback invoked for each analysis event. Receives event type and data.</param>
    /// <param name="cancellationToken">A token to cancel the streaming operation.</param>
    /// <returns>A task representing the asynchronous streaming operation.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="ArgumentNullException">Thrown if eventCallback is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TODO is not found or a streaming error occurs.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via cancellationToken.</exception>
    /// <remarks>
    /// Event types emitted: "status.progress", "status.complete", "status.error".
    /// Cancellation is graceful; the stream closes cleanly without leaving partial state.
    /// </remarks>
    Task StreamStatusAsync(string id, Func<IStreamingEvent, Task> eventCallback, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams implementation plan events for a TODO item.
    /// Emits events as the plan is generated, returning a structured implementation plan.
    /// </summary>
    /// <param name="id">The TODO identifier to plan.</param>
    /// <param name="eventCallback">Callback invoked for each planning event. Receives event type and data.</param>
    /// <param name="cancellationToken">A token to cancel the streaming operation.</param>
    /// <returns>A task representing the asynchronous streaming operation.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="ArgumentNullException">Thrown if eventCallback is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TODO is not found or a streaming error occurs.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via cancellationToken.</exception>
    /// <remarks>
    /// Event types emitted: "plan.progress", "plan.complete", "plan.error".
    /// Cancellation is graceful; the stream closes cleanly without leaving partial state.
    /// </remarks>
    Task StreamPlanAsync(string id, Func<IStreamingEvent, Task> eventCallback, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams implementation execution events for a TODO item.
    /// Emits events as the implementation progresses, executing subtasks and recording actions.
    /// </summary>
    /// <param name="id">The TODO identifier to implement.</param>
    /// <param name="eventCallback">Callback invoked for each implementation event. Receives event type and data.</param>
    /// <param name="cancellationToken">A token to cancel the streaming operation.</param>
    /// <returns>A task representing the asynchronous streaming operation.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="ArgumentNullException">Thrown if eventCallback is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TODO is not found or a streaming error occurs.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via cancellationToken.</exception>
    /// <remarks>
    /// Event types emitted: "implement.progress", "implement.complete", "implement.error".
    /// Cancellation is graceful; the stream closes cleanly without leaving partial state.
    /// </remarks>
    Task StreamImplementAsync(string id, Func<IStreamingEvent, Task> eventCallback, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the projection status for a TODO item, indicating whether it has valid status/plan/implementation state.
    /// </summary>
    /// <param name="id">The TODO identifier to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous status check operation, containing projection health.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TODO is not found or a storage error occurs.</exception>
    Task<ITodoProjectionStatus> GetProjectionStatusAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Repairs the projection for a TODO item, rebuilding status/plan/implementation state from source data.
    /// Use this when projections become stale or corrupted.
    /// </summary>
    /// <param name="id">The TODO identifier to repair.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous repair operation.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TODO is not found or a repair error occurs.</exception>
    Task RepairProjectionAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current TODO selection state.
    /// Returns null if no TODO is currently selected.
    /// </summary>
    /// <returns>The current selection state, or null if no TODO is selected.</returns>
    ITodoSelectionState? CurrentSelection();
}

/// <summary>
/// Represents the runtime state of the active TODO selection.
/// Used to track the current TODO context for operations that don't require explicit ID parameters.
/// </summary>
public interface ITodoSelectionState
{
    /// <summary>
    /// Gets the selected TODO identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the selected TODO title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the selected TODO section.
    /// </summary>
    string Section { get; }

    /// <summary>
    /// Gets the selected TODO priority.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string Priority { get; }

    /// <summary>
    /// Gets whether the selected TODO is marked as complete.
    /// </summary>
    bool Done { get; }

    /// <summary>
    /// Gets the timestamp when this TODO was selected.
    /// </summary>
    DateTimeOffset SelectedAt { get; }
}

/// <summary>
/// Represents the result of a TODO query operation.
/// </summary>
public interface ITodoQueryResult
{
    /// <summary>
    /// Gets the TODO items matching the query.
    /// </summary>
    IReadOnlyList<ITodoItem> Items { get; }

    /// <summary>
    /// Gets the total number of TODOs matching the filter (ignoring pagination).
    /// </summary>
    int TotalCount { get; }
}

/// <summary>
/// Represents a TODO item with all metadata fields.
/// </summary>
public interface ITodoItem
{
    /// <summary>
    /// Gets the unique TODO identifier.
    /// Format: <c>&lt;PHASE&gt;-&lt;AREA&gt;-###</c> or <c>ISSUE-{number}</c>
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the brief title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the grouping category (e.g., "Backend", "Frontend", "Infrastructure").
    /// </summary>
    string Section { get; }

    /// <summary>
    /// Gets the priority level.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string Priority { get; }

    /// <summary>
    /// Gets whether the task is complete.
    /// </summary>
    bool Done { get; }

    /// <summary>
    /// Gets the effort estimate (e.g., "2h", "1d").
    /// </summary>
    string? Estimate { get; }

    /// <summary>
    /// Gets additional context notes.
    /// </summary>
    string? Note { get; }

    /// <summary>
    /// Gets detailed description lines.
    /// </summary>
    IReadOnlyList<string> Description { get; }

    /// <summary>
    /// Gets technical implementation notes.
    /// </summary>
    IReadOnlyList<string> TechnicalDetails { get; }

    /// <summary>
    /// Gets implementation subtasks.
    /// </summary>
    IReadOnlyList<ITodoSubtask> ImplementationTasks { get; }

    /// <summary>
    /// Gets the completion timestamp (ISO 8601), or null if not complete.
    /// </summary>
    string? CompletedDate { get; }

    /// <summary>
    /// Gets the summary of what was done, or null if not complete.
    /// </summary>
    string? DoneSummary { get; }

    /// <summary>
    /// Gets what work remains.
    /// </summary>
    string? Remaining { get; }

    /// <summary>
    /// Gets the priority justification.
    /// </summary>
    string? PriorityNote { get; }

    /// <summary>
    /// Gets a reference link or identifier.
    /// </summary>
    string? Reference { get; }

    /// <summary>
    /// Gets the IDs of prerequisite TODOs.
    /// </summary>
    IReadOnlyList<string> DependsOn { get; }

    /// <summary>
    /// Gets the functional requirement IDs.
    /// </summary>
    IReadOnlyList<string> FunctionalRequirements { get; }

    /// <summary>
    /// Gets the technical requirement IDs.
    /// </summary>
    IReadOnlyList<string> TechnicalRequirements { get; }
}

/// <summary>
/// Represents a subtask within a TODO item.
/// </summary>
public interface ITodoSubtask
{
    /// <summary>
    /// Gets the subtask description.
    /// </summary>
    string Task { get; }

    /// <summary>
    /// Gets whether the subtask is complete.
    /// </summary>
    bool Done { get; }
}

/// <summary>
/// Represents a request to create a new TODO item.
/// </summary>
public interface ITodoCreateRequest
{
    /// <summary>
    /// Gets the TODO identifier.
    /// Must match <c>^[A-Z]+-[A-Z0-9]+-\d{3}$</c> or <c>^ISSUE-\d+$</c>, or <c>ISSUE-NEW</c> to create a GitHub issue.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the brief title. Required.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the grouping category. Required.
    /// </summary>
    string Section { get; }

    /// <summary>
    /// Gets the priority level. Required.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string Priority { get; }

    /// <summary>
    /// Gets the effort estimate (e.g., "2h", "1d").
    /// </summary>
    string? Estimate { get; }

    /// <summary>
    /// Gets detailed description lines.
    /// </summary>
    IReadOnlyList<string>? Description { get; }

    /// <summary>
    /// Gets technical implementation notes.
    /// </summary>
    IReadOnlyList<string>? TechnicalDetails { get; }

    /// <summary>
    /// Gets implementation subtasks.
    /// </summary>
    IReadOnlyList<ITodoSubtask>? ImplementationTasks { get; }

    /// <summary>
    /// Gets additional context notes.
    /// </summary>
    string? Note { get; }

    /// <summary>
    /// Gets what work remains.
    /// </summary>
    string? Remaining { get; }

    /// <summary>
    /// Gets the IDs of prerequisite TODOs.
    /// </summary>
    IReadOnlyList<string>? DependsOn { get; }

    /// <summary>
    /// Gets the functional requirement IDs.
    /// </summary>
    IReadOnlyList<string>? FunctionalRequirements { get; }

    /// <summary>
    /// Gets the technical requirement IDs.
    /// </summary>
    IReadOnlyList<string>? TechnicalRequirements { get; }
}

/// <summary>
/// Represents a request to update an existing TODO item.
/// All fields are optional; only provided fields are updated.
/// </summary>
public interface ITodoUpdateRequest
{
    /// <summary>
    /// Gets the updated title. Null preserves existing value.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets the updated priority. Null preserves existing value.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string? Priority { get; }

    /// <summary>
    /// Gets the updated section. Null preserves existing value.
    /// </summary>
    string? Section { get; }

    /// <summary>
    /// Gets the updated completion status. Null preserves existing value.
    /// </summary>
    bool? Done { get; }

    /// <summary>
    /// Gets the updated effort estimate. Null preserves existing value.
    /// </summary>
    string? Estimate { get; }

    /// <summary>
    /// Gets updated description lines. Null preserves existing value.
    /// </summary>
    IReadOnlyList<string>? Description { get; }

    /// <summary>
    /// Gets updated technical implementation notes. Null preserves existing value.
    /// </summary>
    IReadOnlyList<string>? TechnicalDetails { get; }

    /// <summary>
    /// Gets updated implementation subtasks. Null preserves existing value.
    /// </summary>
    IReadOnlyList<ITodoSubtask>? ImplementationTasks { get; }

    /// <summary>
    /// Gets updated additional context notes. Null preserves existing value.
    /// </summary>
    string? Note { get; }

    /// <summary>
    /// Gets the completion timestamp. Null preserves existing value.
    /// </summary>
    string? CompletedDate { get; }

    /// <summary>
    /// Gets the completion summary. Null preserves existing value.
    /// </summary>
    string? DoneSummary { get; }

    /// <summary>
    /// Gets updated remaining work notes. Null preserves existing value.
    /// </summary>
    string? Remaining { get; }

    /// <summary>
    /// Gets updated prerequisite TODO IDs. Null preserves existing value.
    /// </summary>
    IReadOnlyList<string>? DependsOn { get; }

    /// <summary>
    /// Gets updated functional requirement IDs. Null preserves existing value.
    /// </summary>
    IReadOnlyList<string>? FunctionalRequirements { get; }

    /// <summary>
    /// Gets updated technical requirement IDs. Null preserves existing value.
    /// </summary>
    IReadOnlyList<string>? TechnicalRequirements { get; }
}

/// <summary>
/// Represents the result of a TODO creation or update operation.
/// </summary>
public interface ITodoMutationResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the TODO item after the mutation.
    /// </summary>
    ITodoItem Item { get; }
}

/// <summary>
/// Represents the result of a requirements analysis for a TODO item.
/// </summary>
public interface ITodoRequirementsAnalysis
{
    /// <summary>
    /// Gets the TODO identifier that was analyzed.
    /// </summary>
    string TodoId { get; }

    /// <summary>
    /// Gets the functional requirements referenced by this TODO.
    /// </summary>
    IReadOnlyList<IRequirementReference> FunctionalRequirements { get; }

    /// <summary>
    /// Gets the technical requirements referenced by this TODO.
    /// </summary>
    IReadOnlyList<IRequirementReference> TechnicalRequirements { get; }

    /// <summary>
    /// Gets whether all referenced requirements exist in the project.
    /// </summary>
    bool AllRequirementsExist { get; }
}

/// <summary>
/// Represents a reference to a requirement in the requirements analysis.
/// </summary>
public interface IRequirementReference
{
    /// <summary>
    /// Gets the requirement identifier (e.g., "FR-MCP-001", "TR-MCP-ARCH-001").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the requirement title, or null if not found.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets whether the requirement exists in the project.
    /// </summary>
    bool Exists { get; }
}

/// <summary>
/// Represents a streaming event emitted during TODO workflow operations.
/// </summary>
public interface IStreamingEvent
{
    /// <summary>
    /// Gets the event type (e.g., "status.progress", "plan.complete", "implement.error").
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the event-specific data payload.
    /// Structure depends on the <see cref="EventType"/>.
    /// </summary>
    object? Data { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the sequence number of this event within the stream.
    /// </summary>
    int Sequence { get; }
}

/// <summary>
/// Represents the projection status for a TODO item.
/// </summary>
public interface ITodoProjectionStatus
{
    /// <summary>
    /// Gets the TODO identifier.
    /// </summary>
    string TodoId { get; }

    /// <summary>
    /// Gets whether the TODO has a valid status projection.
    /// </summary>
    bool HasStatus { get; }

    /// <summary>
    /// Gets whether the TODO has a valid plan projection.
    /// </summary>
    bool HasPlan { get; }

    /// <summary>
    /// Gets whether the TODO has a valid implementation projection.
    /// </summary>
    bool HasImplementation { get; }

    /// <summary>
    /// Gets the timestamp when the projections were last updated.
    /// </summary>
    DateTimeOffset? LastUpdated { get; }

    /// <summary>
    /// Gets whether the projections are stale and need repair.
    /// </summary>
    bool IsStale { get; }
}
