namespace McpServer.Repl.Core;

/// <summary>
/// Defines YAML command shapes for the <c>workflow.todo.*</c> namespace.
/// All commands follow the REPL protocol request envelope structure with method-specific parameters.
/// </summary>
/// <remarks>
/// <para>
/// Command methods in this namespace:
/// <list type="bullet">
/// <item><c>workflow.todo.query</c> — Query TODO items with filters</item>
/// <item><c>workflow.todo.get</c> — Get specific TODO by ID</item>
/// <item><c>workflow.todo.select</c> — Select TODO as active context</item>
/// <item><c>workflow.todo.create</c> — Create new TODO</item>
/// <item><c>workflow.todo.update</c> — Update TODO by ID</item>
/// <item><c>workflow.todo.updateSelected</c> — Update currently selected TODO</item>
/// <item><c>workflow.todo.delete</c> — Delete TODO by ID</item>
/// <item><c>workflow.todo.deleteSelected</c> — Delete currently selected TODO</item>
/// <item><c>workflow.todo.analyzeRequirements</c> — Analyze requirement references</item>
/// <item><c>workflow.todo.streamStatus</c> — Stream status analysis events</item>
/// <item><c>workflow.todo.streamPlan</c> — Stream plan generation events</item>
/// <item><c>workflow.todo.streamImplement</c> — Stream implementation execution events</item>
/// <item><c>workflow.todo.getProjectionStatus</c> — Get projection health status</item>
/// <item><c>workflow.todo.repairProjection</c> — Repair corrupted projections</item>
/// <item><c>workflow.todo.currentSelection</c> — Get active TODO selection state</item>
/// </list>
/// </para>
/// <para>
/// All request envelopes follow the structure:
/// <code>
/// type: request
/// payload:
///   requestId: &lt;unique-request-id&gt;
///   method: workflow.todo.&lt;operation&gt;
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
/// All error responses follow the structure defined in <see cref="ITodoError"/>.
/// </para>
/// <para><strong>Streaming Event Envelopes:</strong></para>
/// <para>
/// Streaming operations emit events using the <c>event</c> envelope type:
/// <code>
/// type: event
/// payload:
///   event: workflow.todo.&lt;eventName&gt;
///   data:
///     eventType: &lt;event-type&gt;
///     sequence: &lt;event-sequence-number&gt;
///     timestamp: &lt;iso8601-timestamp&gt;
///     &lt;event-specific-data&gt;
/// </code>
/// </para>
/// <para><strong>Cancellation Semantics:</strong></para>
/// <para>
/// All streaming operations support graceful cancellation. When a cancellation token is triggered:
/// <list type="bullet">
/// <item>The stream closes cleanly without emitting partial state</item>
/// <item>A final event with type <c>*.cancelled</c> is emitted (e.g., "status.cancelled")</item>
/// <item>No further events are emitted after cancellation</item>
/// <item>Partial work is not persisted unless explicitly documented</item>
/// </list>
/// Example cancellation event:
/// <code>
/// type: event
/// payload:
///   event: workflow.todo.streamStatus
///   data:
///     eventType: status.cancelled
///     sequence: 15
///     timestamp: 2026-03-04T11:50:00Z
///     message: Stream cancelled by user request
/// </code>
/// </para>
/// </remarks>
public static class TodoCommandShapes
{
    /// <summary>
    /// The namespace prefix for all TODO workflow commands.
    /// </summary>
    public const string MethodNamespace = "workflow.todo";

    /// <summary>
    /// Command method for querying TODO items.
    /// Method: <c>workflow.todo.query</c>
    /// </summary>
    public const string QueryMethod = "workflow.todo.query";

    /// <summary>
    /// Command method for getting a specific TODO item.
    /// Method: <c>workflow.todo.get</c>
    /// </summary>
    public const string GetMethod = "workflow.todo.get";

    /// <summary>
    /// Command method for selecting a TODO as active context.
    /// Method: <c>workflow.todo.select</c>
    /// </summary>
    public const string SelectMethod = "workflow.todo.select";

    /// <summary>
    /// Command method for creating a new TODO item.
    /// Method: <c>workflow.todo.create</c>
    /// </summary>
    public const string CreateMethod = "workflow.todo.create";

    /// <summary>
    /// Command method for updating a TODO item by ID.
    /// Method: <c>workflow.todo.update</c>
    /// </summary>
    public const string UpdateMethod = "workflow.todo.update";

    /// <summary>
    /// Command method for updating the currently selected TODO item.
    /// Method: <c>workflow.todo.updateSelected</c>
    /// </summary>
    public const string UpdateSelectedMethod = "workflow.todo.updateSelected";

    /// <summary>
    /// Command method for deleting a TODO item by ID.
    /// Method: <c>workflow.todo.delete</c>
    /// </summary>
    public const string DeleteMethod = "workflow.todo.delete";

    /// <summary>
    /// Command method for deleting the currently selected TODO item.
    /// Method: <c>workflow.todo.deleteSelected</c>
    /// </summary>
    public const string DeleteSelectedMethod = "workflow.todo.deleteSelected";

    /// <summary>
    /// Command method for analyzing requirements referenced by a TODO.
    /// Method: <c>workflow.todo.analyzeRequirements</c>
    /// </summary>
    public const string AnalyzeRequirementsMethod = "workflow.todo.analyzeRequirements";

    /// <summary>
    /// Command method for streaming status analysis events.
    /// Method: <c>workflow.todo.streamStatus</c>
    /// </summary>
    public const string StreamStatusMethod = "workflow.todo.streamStatus";

    /// <summary>
    /// Command method for streaming plan generation events.
    /// Method: <c>workflow.todo.streamPlan</c>
    /// </summary>
    public const string StreamPlanMethod = "workflow.todo.streamPlan";

    /// <summary>
    /// Command method for streaming implementation execution events.
    /// Method: <c>workflow.todo.streamImplement</c>
    /// </summary>
    public const string StreamImplementMethod = "workflow.todo.streamImplement";

    /// <summary>
    /// Command method for getting projection status.
    /// Method: <c>workflow.todo.getProjectionStatus</c>
    /// </summary>
    public const string GetProjectionStatusMethod = "workflow.todo.getProjectionStatus";

    /// <summary>
    /// Command method for repairing projection state.
    /// Method: <c>workflow.todo.repairProjection</c>
    /// </summary>
    public const string RepairProjectionMethod = "workflow.todo.repairProjection";

    /// <summary>
    /// Command method for getting current selection state.
    /// Method: <c>workflow.todo.currentSelection</c>
    /// </summary>
    public const string CurrentSelectionMethod = "workflow.todo.currentSelection";
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.query</c> command.
/// All fields are optional filters; omitted fields return all matching TODOs.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-query-001
///   method: workflow.todo.query
///   params:
///     keyword: authentication
///     priority: high
///     section: Backend
///     done: false
/// </code>
/// </remarks>
public interface IQueryTodoParams
{
    /// <summary>
    /// Gets the optional keyword filter for title/description search.
    /// </summary>
    string? Keyword { get; }

    /// <summary>
    /// Gets the optional priority filter.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string? Priority { get; }

    /// <summary>
    /// Gets the optional section filter.
    /// </summary>
    string? Section { get; }

    /// <summary>
    /// Gets the optional exact TODO ID filter.
    /// </summary>
    string? Id { get; }

    /// <summary>
    /// Gets the optional completion status filter.
    /// If null, returns both completed and incomplete TODOs.
    /// </summary>
    bool? Done { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.todo.query</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-query-001
///   result:
///     items:
///       - id: MCP-AUTH-001
///         title: Implement JWT authentication
///         section: Backend
///         priority: high
///         done: false
///         estimate: 4h
///         description:
///           - Add JWT token generation
///           - Add JWT token validation
///         technicalDetails: []
///         implementationTasks: []
///         dependsOn: []
///         functionalRequirements: [FR-AUTH-001]
///         technicalRequirements: [TR-AUTH-001]
///     totalCount: 1
/// </code>
/// </remarks>
public interface IQueryTodoResult
{
    /// <summary>
    /// Gets the TODO items matching the query.
    /// </summary>
    IReadOnlyList<ITodoItem> Items { get; }

    /// <summary>
    /// Gets the total number of TODOs matching the filter.
    /// </summary>
    int TotalCount { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.get</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-get-001
///   method: workflow.todo.get
///   params:
///     id: MCP-AUTH-001
/// </code>
/// </remarks>
public interface IGetTodoParams
{
    /// <summary>
    /// Gets the TODO identifier to retrieve.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.todo.get</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-get-001
///   result:
///     item:
///       id: MCP-AUTH-001
///       title: Implement JWT authentication
///       section: Backend
///       priority: high
///       done: false
///       estimate: 4h
///       note: null
///       description:
///         - Add JWT token generation
///         - Add JWT token validation
///       technicalDetails: []
///       implementationTasks:
///         - task: Create TokenService
///           done: false
///         - task: Create JwtValidator
///           done: false
///       completedDate: null
///       doneSummary: null
///       remaining: Need integration tests
///       priorityNote: null
///       reference: null
///       dependsOn: []
///       functionalRequirements: [FR-AUTH-001]
///       technicalRequirements: [TR-AUTH-001]
/// </code>
/// </remarks>
public interface IGetTodoResult
{
    /// <summary>
    /// Gets the requested TODO item.
    /// </summary>
    ITodoItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.select</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-select-001
///   method: workflow.todo.select
///   params:
///     id: MCP-AUTH-001
/// </code>
/// </remarks>
public interface ISelectTodoParams
{
    /// <summary>
    /// Gets the TODO identifier to select.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.todo.select</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-select-001
///   result:
///     selected: true
///     id: MCP-AUTH-001
///     title: Implement JWT authentication
///     section: Backend
///     priority: high
///     done: false
///     selectedAt: 2026-03-04T11:45:23Z
/// </code>
/// </remarks>
public interface ISelectTodoResult
{
    /// <summary>
    /// Gets whether the selection succeeded.
    /// </summary>
    bool Selected { get; }

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
    /// </summary>
    string Priority { get; }

    /// <summary>
    /// Gets whether the selected TODO is complete.
    /// </summary>
    bool Done { get; }

    /// <summary>
    /// Gets the timestamp when the selection occurred.
    /// </summary>
    DateTimeOffset SelectedAt { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.create</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-create-001
///   method: workflow.todo.create
///   params:
///     id: MCP-AUTH-001
///     title: Implement JWT authentication
///     section: Backend
///     priority: high
///     estimate: 4h
///     description:
///       - Add JWT token generation
///       - Add JWT token validation
///     implementationTasks:
///       - task: Create TokenService
///         done: false
///       - task: Create JwtValidator
///         done: false
///     functionalRequirements: [FR-AUTH-001]
///     technicalRequirements: [TR-AUTH-001]
/// </code>
/// </remarks>
public interface ICreateTodoParams
{
    /// <summary>
    /// Gets the TODO identifier. Must match canonical rules or be <c>ISSUE-NEW</c>.
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
    /// Gets the effort estimate.
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
/// Represents the result for the <c>workflow.todo.create</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-create-001
///   result:
///     success: true
///     item:
///       id: MCP-AUTH-001
///       title: Implement JWT authentication
///       section: Backend
///       priority: high
///       done: false
///       ...
/// </code>
/// </remarks>
public interface ICreateTodoResult
{
    /// <summary>
    /// Gets whether the creation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the created TODO item.
    /// </summary>
    ITodoItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.update</c> command.
/// All fields except <c>id</c> are optional; only provided fields are updated.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-update-001
///   method: workflow.todo.update
///   params:
///     id: MCP-AUTH-001
///     remaining: Need integration tests
///     done: false
/// </code>
/// </remarks>
public interface IUpdateTodoParams
{
    /// <summary>
    /// Gets the TODO identifier to update.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the updated title. Null preserves existing value.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets the updated priority. Null preserves existing value.
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
/// Represents the result for the <c>workflow.todo.update</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-update-001
///   result:
///     success: true
///     item:
///       id: MCP-AUTH-001
///       title: Implement JWT authentication
///       section: Backend
///       priority: high
///       done: false
///       remaining: Need integration tests
///       ...
/// </code>
/// </remarks>
public interface IUpdateTodoResult
{
    /// <summary>
    /// Gets whether the update succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the updated TODO item.
    /// </summary>
    ITodoItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.updateSelected</c> command.
/// All fields are optional; only provided fields are updated.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-updatesel-001
///   method: workflow.todo.updateSelected
///   params:
///     remaining: Need integration tests
///     done: false
/// </code>
/// </remarks>
public interface IUpdateSelectedTodoParams
{
    /// <summary>
    /// Gets the updated title. Null preserves existing value.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets the updated priority. Null preserves existing value.
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
/// Represents the result for the <c>workflow.todo.updateSelected</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-updatesel-001
///   result:
///     success: true
///     item:
///       id: MCP-AUTH-001
///       title: Implement JWT authentication
///       section: Backend
///       priority: high
///       done: false
///       remaining: Need integration tests
///       ...
/// </code>
/// </remarks>
public interface IUpdateSelectedTodoResult
{
    /// <summary>
    /// Gets whether the update succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the updated TODO item.
    /// </summary>
    ITodoItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.delete</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-delete-001
///   method: workflow.todo.delete
///   params:
///     id: MCP-AUTH-001
/// </code>
/// </remarks>
public interface IDeleteTodoParams
{
    /// <summary>
    /// Gets the TODO identifier to delete.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.todo.delete</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-delete-001
///   result:
///     deleted: true
///     id: MCP-AUTH-001
/// </code>
/// </remarks>
public interface IDeleteTodoResult
{
    /// <summary>
    /// Gets whether the deletion succeeded.
    /// </summary>
    bool Deleted { get; }

    /// <summary>
    /// Gets the identifier of the deleted TODO.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.deleteSelected</c> command.
/// This command takes no parameters; it deletes the currently selected TODO.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-deletesel-001
///   method: workflow.todo.deleteSelected
///   params: {}
/// </code>
/// </remarks>
public interface IDeleteSelectedTodoParams
{
}

/// <summary>
/// Represents the result for the <c>workflow.todo.deleteSelected</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-deletesel-001
///   result:
///     deleted: true
///     id: MCP-AUTH-001
/// </code>
/// </remarks>
public interface IDeleteSelectedTodoResult
{
    /// <summary>
    /// Gets whether the deletion succeeded.
    /// </summary>
    bool Deleted { get; }

    /// <summary>
    /// Gets the identifier of the deleted TODO.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.analyzeRequirements</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-analyze-001
///   method: workflow.todo.analyzeRequirements
///   params:
///     id: MCP-AUTH-001
/// </code>
/// </remarks>
public interface IAnalyzeRequirementsParams
{
    /// <summary>
    /// Gets the TODO identifier to analyze.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.todo.analyzeRequirements</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-analyze-001
///   result:
///     todoId: MCP-AUTH-001
///     functionalRequirements:
///       - id: FR-AUTH-001
///         title: User authentication
///         exists: true
///     technicalRequirements:
///       - id: TR-AUTH-001
///         title: JWT token standard
///         exists: true
///     allRequirementsExist: true
/// </code>
/// </remarks>
public interface IAnalyzeRequirementsResult
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
    /// Gets whether all referenced requirements exist.
    /// </summary>
    bool AllRequirementsExist { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.streamStatus</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-status-001
///   method: workflow.todo.streamStatus
///   params:
///     id: MCP-AUTH-001
/// </code>
/// </remarks>
public interface IStreamStatusParams
{
    /// <summary>
    /// Gets the TODO identifier to analyze.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents streaming events for the <c>workflow.todo.streamStatus</c> operation.
/// Events are emitted with type "workflow.todo.streamStatus" and event-specific data.
/// </summary>
/// <remarks>
/// <para><strong>Progress Event:</strong></para>
/// <code>
/// type: event
/// payload:
///   event: workflow.todo.streamStatus
///   data:
///     eventType: status.progress
///     sequence: 1
///     timestamp: 2026-03-04T11:45:30Z
///     message: Analyzing TODO dependencies...
///     progress: 25
/// </code>
/// <para><strong>Complete Event:</strong></para>
/// <code>
/// type: event
/// payload:
///   event: workflow.todo.streamStatus
///   data:
///     eventType: status.complete
///     sequence: 10
///     timestamp: 2026-03-04T11:46:00Z
///     todoId: MCP-AUTH-001
///     status: ready
///     blockers: []
///     dependencies: [MCP-AUTH-002]
/// </code>
/// <para><strong>Error Event:</strong></para>
/// <code>
/// type: event
/// payload:
///   event: workflow.todo.streamStatus
///   data:
///     eventType: status.error
///     sequence: 5
///     timestamp: 2026-03-04T11:45:45Z
///     message: Failed to analyze dependencies
///     errorCode: dependency_error
/// </code>
/// <para><strong>Cancelled Event:</strong></para>
/// <code>
/// type: event
/// payload:
///   event: workflow.todo.streamStatus
///   data:
///     eventType: status.cancelled
///     sequence: 7
///     timestamp: 2026-03-04T11:45:50Z
///     message: Stream cancelled by user request
/// </code>
/// </remarks>
public interface IStreamStatusEvent
{
    /// <summary>
    /// Gets the event type.
    /// Valid values: "status.progress", "status.complete", "status.error", "status.cancelled".
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the sequence number of this event within the stream.
    /// </summary>
    int Sequence { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the event-specific data payload.
    /// Structure depends on <see cref="EventType"/>.
    /// </summary>
    object? Data { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.streamPlan</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-plan-001
///   method: workflow.todo.streamPlan
///   params:
///     id: MCP-AUTH-001
/// </code>
/// </remarks>
public interface IStreamPlanParams
{
    /// <summary>
    /// Gets the TODO identifier to plan.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents streaming events for the <c>workflow.todo.streamPlan</c> operation.
/// Event types: "plan.progress", "plan.complete", "plan.error", "plan.cancelled".
/// </summary>
/// <remarks>
/// See <see cref="IStreamStatusEvent"/> for event envelope examples. Plan events follow the same structure.
/// </remarks>
public interface IStreamPlanEvent
{
    /// <summary>
    /// Gets the event type.
    /// Valid values: "plan.progress", "plan.complete", "plan.error", "plan.cancelled".
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the sequence number of this event within the stream.
    /// </summary>
    int Sequence { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the event-specific data payload.
    /// </summary>
    object? Data { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.streamImplement</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-implement-001
///   method: workflow.todo.streamImplement
///   params:
///     id: MCP-AUTH-001
/// </code>
/// </remarks>
public interface IStreamImplementParams
{
    /// <summary>
    /// Gets the TODO identifier to implement.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents streaming events for the <c>workflow.todo.streamImplement</c> operation.
/// Event types: "implement.progress", "implement.complete", "implement.error", "implement.cancelled".
/// </summary>
/// <remarks>
/// See <see cref="IStreamStatusEvent"/> for event envelope examples. Implementation events follow the same structure.
/// </remarks>
public interface IStreamImplementEvent
{
    /// <summary>
    /// Gets the event type.
    /// Valid values: "implement.progress", "implement.complete", "implement.error", "implement.cancelled".
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the sequence number of this event within the stream.
    /// </summary>
    int Sequence { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the event-specific data payload.
    /// </summary>
    object? Data { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.getProjectionStatus</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-projstat-001
///   method: workflow.todo.getProjectionStatus
///   params:
///     id: MCP-AUTH-001
/// </code>
/// </remarks>
public interface IGetProjectionStatusParams
{
    /// <summary>
    /// Gets the TODO identifier to check.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.todo.getProjectionStatus</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-projstat-001
///   result:
///     todoId: MCP-AUTH-001
///     hasStatus: true
///     hasPlan: true
///     hasImplementation: false
///     lastUpdated: 2026-03-04T11:45:00Z
///     isStale: false
/// </code>
/// </remarks>
public interface IGetProjectionStatusResult
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
    /// Gets the timestamp when projections were last updated.
    /// </summary>
    DateTimeOffset? LastUpdated { get; }

    /// <summary>
    /// Gets whether the projections are stale and need repair.
    /// </summary>
    bool IsStale { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.repairProjection</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-repair-001
///   method: workflow.todo.repairProjection
///   params:
///     id: MCP-AUTH-001
/// </code>
/// </remarks>
public interface IRepairProjectionParams
{
    /// <summary>
    /// Gets the TODO identifier to repair.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.todo.repairProjection</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-repair-001
///   result:
///     repaired: true
///     todoId: MCP-AUTH-001
///     repairedAt: 2026-03-04T11:50:00Z
/// </code>
/// </remarks>
public interface IRepairProjectionResult
{
    /// <summary>
    /// Gets whether the repair succeeded.
    /// </summary>
    bool Repaired { get; }

    /// <summary>
    /// Gets the TODO identifier that was repaired.
    /// </summary>
    string TodoId { get; }

    /// <summary>
    /// Gets the timestamp when the repair completed.
    /// </summary>
    DateTimeOffset RepairedAt { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.todo.currentSelection</c> command.
/// This command takes no parameters.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-currsel-001
///   method: workflow.todo.currentSelection
///   params: {}
/// </code>
/// </remarks>
public interface ICurrentSelectionParams
{
}

/// <summary>
/// Represents the result for the <c>workflow.todo.currentSelection</c> command.
/// Returns null/empty if no TODO is selected.
/// </summary>
/// <remarks>
/// Example YAML when TODO is selected:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-currsel-001
///   result:
///     id: MCP-AUTH-001
///     title: Implement JWT authentication
///     section: Backend
///     priority: high
///     done: false
///     selectedAt: 2026-03-04T11:45:23Z
/// </code>
/// Example YAML when no TODO is selected:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-currsel-001
///   result: null
/// </code>
/// </remarks>
public interface ICurrentSelectionResult
{
    /// <summary>
    /// Gets the selected TODO identifier, or null if no TODO is selected.
    /// </summary>
    string? Id { get; }

    /// <summary>
    /// Gets the selected TODO title, or null if no TODO is selected.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets the selected TODO section, or null if no TODO is selected.
    /// </summary>
    string? Section { get; }

    /// <summary>
    /// Gets the selected TODO priority, or null if no TODO is selected.
    /// </summary>
    string? Priority { get; }

    /// <summary>
    /// Gets whether the selected TODO is complete, or null if no TODO is selected.
    /// </summary>
    bool? Done { get; }

    /// <summary>
    /// Gets the timestamp when selection occurred, or null if no TODO is selected.
    /// </summary>
    DateTimeOffset? SelectedAt { get; }
}

/// <summary>
/// Defines structured error envelopes for TODO workflow operations.
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
/// Standard error codes for TODO operations:
/// <list type="bullet">
/// <item><c>todo_not_found</c> — TODO with specified ID does not exist</item>
/// <item><c>todo_already_exists</c> — TODO with same ID already exists</item>
/// <item><c>invalid_todo_id</c> — TODO ID violates canonical identifier rules</item>
/// <item><c>invalid_parameter</c> — Required parameter missing or invalid</item>
/// <item><c>no_selection</c> — No TODO is currently selected</item>
/// <item><c>projection_error</c> — Projection operation failed</item>
/// <item><c>stream_error</c> — Streaming operation failed</item>
/// <item><c>storage_error</c> — Underlying storage operation failed</item>
/// <item><c>internal_error</c> — Unexpected internal error</item>
/// </list>
/// </para>
/// </remarks>
public interface ITodoError
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
/// Provides standard error code constants for TODO operations.
/// </summary>
public static class TodoErrorCodes
{
    /// <summary>
    /// TODO with specified ID does not exist.
    /// </summary>
    public const string TodoNotFound = "todo_not_found";

    /// <summary>
    /// TODO with same ID already exists when attempting to create.
    /// </summary>
    public const string TodoAlreadyExists = "todo_already_exists";

    /// <summary>
    /// TODO ID does not conform to canonical identifier rules.
    /// Format: <c>&lt;PHASE&gt;-&lt;AREA&gt;-###</c> or <c>ISSUE-{number}</c>
    /// </summary>
    public const string InvalidTodoId = "invalid_todo_id";

    /// <summary>
    /// Required parameter is missing, empty, or contains invalid data.
    /// </summary>
    public const string InvalidParameter = "invalid_parameter";

    /// <summary>
    /// No TODO is currently selected when attempting an operation that requires selection.
    /// </summary>
    public const string NoSelection = "no_selection";

    /// <summary>
    /// Projection operation (status/plan/implementation) failed.
    /// </summary>
    public const string ProjectionError = "projection_error";

    /// <summary>
    /// Streaming operation failed during event emission.
    /// </summary>
    public const string StreamError = "stream_error";

    /// <summary>
    /// Underlying storage operation (file I/O, database, etc.) failed.
    /// </summary>
    public const string StorageError = "storage_error";

    /// <summary>
    /// Unexpected internal error occurred during operation.
    /// </summary>
    public const string InternalError = "internal_error";
}
