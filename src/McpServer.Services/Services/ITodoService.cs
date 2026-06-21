using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-CORE-013: Service for querying and managing TODO items.
/// Provides CRUD operations, search, and audit-history access.
/// </summary>
public interface ITodoService
{
    /// <summary>Query TODO items by optional keyword, priority, and/or id.</summary>
    Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Get a single TODO item by its id.</summary>
    Task<TodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Create a new TODO item in the specified section and priority.</summary>
    Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Update an existing TODO item by id.</summary>
    Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Delete a TODO item by id.</summary>
    Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Get append-only audit history for a TODO item.</summary>
    Task<TodoAuditQueryResult> GetAuditAsync(string id, int limit = 50, int offset = 0, CancellationToken cancellationToken = default);

    /// <summary>TR-MCP-TODO-006: Get projection status for database-authoritative TODO storage.</summary>
    Task<TodoProjectionStatusResult> GetProjectionStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>TR-MCP-TODO-006: Repair TODO.yaml projection from database-authoritative TODO storage.</summary>
    Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-TXN-001: Internal store capability for restoring TODO state during transaction rollback compensation.
/// </summary>
public interface ITodoCompensationService
{
    /// <summary>Captures the provider-specific current state needed to restore a TODO item later.</summary>
    Task<TodoCompensationSnapshot?> CaptureForRestoreAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Updates a TODO item while atomically capturing its restore point under the provider write lock.</summary>
    Task<TodoCompensatedMutationResult> UpdateWithRestorePointAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes a TODO item while atomically capturing its restore point under the provider write lock.</summary>
    Task<TodoCompensatedMutationResult> DeleteWithRestorePointAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Deletes a TODO item created by an uncommitted transaction during rollback compensation.</summary>
    Task<TodoMutationResult> DeleteCreatedAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Restores a previously captured provider-specific TODO state.</summary>
    Task<TodoMutationResult> RestoreAsync(TodoCompensationSnapshot snapshot, CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-TXN-001: Advertises whether a TODO service can actually perform rollback compensation.
/// </summary>
public interface ITodoCompensationCapability
{
    /// <summary>
    /// Gets a value indicating whether rollback compensation can be performed without deferring failure to mutation time.
    /// </summary>
    bool SupportsRollbackCompensation { get; }
}

/// <summary>TR-MCP-TXN-001: Opaque provider-specific TODO compensation snapshot.</summary>
public sealed record TodoCompensationSnapshot
{
    /// <summary>Provider identifier that owns the snapshot payload.</summary>
    public required string Provider { get; init; }

    /// <summary>Provider-specific state. Only the provider that created the snapshot should interpret it.</summary>
    public required object State { get; init; }
}

/// <summary>TR-MCP-TXN-001: TODO mutation result paired with the pre-mutation restore point captured by the provider.</summary>
public sealed record TodoCompensatedMutationResult
{
    /// <summary>The TODO mutation result returned by the provider.</summary>
    public required TodoMutationResult Result { get; init; }

    /// <summary>The pre-mutation restore point, or <see langword="null"/> when no item existed to restore.</summary>
    public TodoCompensationSnapshot? Snapshot { get; init; }
}

/// <summary>TR-PLANNED-CORE-013: Query parameters for searching TODO items.</summary>
public sealed record TodoQueryRequest
{
    /// <summary>Free-text keyword to match against id, title, description, and technical details.</summary>
    public string? Keyword { get; init; }

    /// <summary>Filter by priority level: high, medium, or low.</summary>
    public string? Priority { get; init; }

    /// <summary>Filter by section name (e.g. mvp-app, mvp-support).</summary>
    public string? Section { get; init; }

    /// <summary>Filter by item id.</summary>
    public string? Id { get; init; }

    /// <summary>Filter by done status.</summary>
    public bool? Done { get; init; }
}

/// <summary>TR-PLANNED-CORE-013: Result of a TODO query.</summary>
public sealed record TodoQueryResult(IReadOnlyList<TodoFlatItem> Items, int TotalCount);

/// <summary>TR-PLANNED-CORE-013: A flattened TODO item with section and priority context.</summary>
public sealed record TodoFlatItem
{
    /// <summary>Item id (e.g. MVP-APP-001).</summary>
    public required string Id { get; init; }

    /// <summary>Item title.</summary>
    public required string Title { get; init; }

    /// <summary>Section key (e.g. mvp-app, mvp-support).</summary>
    public required string Section { get; init; }

    /// <summary>Priority level (high, medium, low).</summary>
    public required string Priority { get; init; }

    /// <summary>Whether the item is done.</summary>
    public required bool Done { get; init; }

    /// <summary>Estimate string (e.g. "96-128 hours").</summary>
    public string? Estimate { get; init; }

    /// <summary>Optional note.</summary>
    public string? Note { get; init; }

    /// <summary>Description lines.</summary>
    public IReadOnlyList<string>? Description { get; init; }

    /// <summary>Technical detail lines.</summary>
    public IReadOnlyList<string>? TechnicalDetails { get; init; }

    /// <summary>Implementation sub-tasks.</summary>
    public IReadOnlyList<TodoFlatTask>? ImplementationTasks { get; init; }

    /// <summary>Completion date if done.</summary>
    public string? CompletedDate { get; init; }

    /// <summary>Done summary text.</summary>
    public string? DoneSummary { get; init; }

    /// <summary>Remaining work text.</summary>
    public string? Remaining { get; init; }

    /// <summary>Priority note override.</summary>
    public string? PriorityNote { get; init; }

    /// <summary>Reference link.</summary>
    public string? Reference { get; init; }

    /// <summary>Code-review phase label for remediation items.</summary>
    public string? Phase { get; init; }

    /// <summary>IDs of TODO items this item depends on.</summary>
    public IReadOnlyList<string>? DependsOn { get; init; }

    /// <summary>Associated functional requirement IDs (e.g. FR-LOC-001).</summary>
    public IReadOnlyList<string>? FunctionalRequirements { get; init; }

    /// <summary>Associated technical requirement IDs (e.g. TR-LOC-001).</summary>
    public IReadOnlyList<string>? TechnicalRequirements { get; init; }
}

/// <summary>TR-PLANNED-CORE-013: Flattened implementation task.</summary>
public sealed record TodoFlatTask(string Task, bool Done);

/// <summary>TR-PLANNED-CORE-013: Request to create a new TODO item.</summary>
public sealed record TodoCreateRequest
{
    /// <summary>Item id (e.g. MVP-APP-006). Required.</summary>
    public required string Id { get; init; }

    /// <summary>Item title. Required.</summary>
    public required string Title { get; init; }

    /// <summary>Section key (e.g. mvp-app, mvp-support). Required.</summary>
    public required string Section { get; init; }

    /// <summary>Priority level (high, medium, low). Required.</summary>
    public required string Priority { get; init; }

    /// <summary>Estimate string.</summary>
    public string? Estimate { get; init; }

    /// <summary>Description lines.</summary>
    public IReadOnlyList<string>? Description { get; init; }

    /// <summary>Technical detail lines.</summary>
    public IReadOnlyList<string>? TechnicalDetails { get; init; }

    /// <summary>Implementation sub-tasks.</summary>
    public IReadOnlyList<TodoFlatTask>? ImplementationTasks { get; init; }

    /// <summary>Optional note.</summary>
    public string? Note { get; init; }

    /// <summary>Remaining work text.</summary>
    public string? Remaining { get; init; }

    /// <summary>Optional code-review phase label.</summary>
    public string? Phase { get; init; }

    /// <summary>IDs of TODO items this item depends on.</summary>
    public IReadOnlyList<string>? DependsOn { get; init; }

    /// <summary>Associated functional requirement IDs.</summary>
    public IReadOnlyList<string>? FunctionalRequirements { get; init; }

    /// <summary>Associated technical requirement IDs.</summary>
    public IReadOnlyList<string>? TechnicalRequirements { get; init; }
}

/// <summary>TR-PLANNED-CORE-013: Request to update an existing TODO item.</summary>
public sealed record TodoUpdateRequest
{
    /// <summary>Updated title (null = no change).</summary>
    public string? Title { get; init; }

    /// <summary>Updated priority (null = no change).</summary>
    public string? Priority { get; init; }

    /// <summary>Updated section (null = no change).</summary>
    public string? Section { get; init; }

    /// <summary>Updated done status (null = no change).</summary>
    public bool? Done { get; init; }

    /// <summary>Updated estimate (null = no change).</summary>
    public string? Estimate { get; init; }

    /// <summary>Updated description lines (null = no change).</summary>
    public IReadOnlyList<string>? Description { get; init; }

    /// <summary>Updated technical details (null = no change).</summary>
    public IReadOnlyList<string>? TechnicalDetails { get; init; }

    /// <summary>Updated implementation tasks (null = no change).</summary>
    public IReadOnlyList<TodoFlatTask>? ImplementationTasks { get; init; }

    /// <summary>Updated note (null = no change).</summary>
    public string? Note { get; init; }

    /// <summary>Updated completed date (null = no change).</summary>
    public string? CompletedDate { get; init; }

    /// <summary>Updated done summary (null = no change).</summary>
    public string? DoneSummary { get; init; }

    /// <summary>Updated remaining text (null = no change).</summary>
    public string? Remaining { get; init; }

    /// <summary>Updated reference text (null = no change).</summary>
    public string? Reference { get; init; }

    /// <summary>Updated code-review phase label (null = no change).</summary>
    public string? Phase { get; init; }

    /// <summary>Updated dependency list (null = no change).</summary>
    public IReadOnlyList<string>? DependsOn { get; init; }

    /// <summary>Updated functional requirement IDs (null = no change).</summary>
    public IReadOnlyList<string>? FunctionalRequirements { get; init; }

    /// <summary>Updated technical requirement IDs (null = no change).</summary>
    public IReadOnlyList<string>? TechnicalRequirements { get; init; }
}

/// <summary>Classifies the failure mode of a TODO mutation.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TodoMutationFailureKind
{
    /// <summary>No failure classification applies.</summary>
    None = 0,

    /// <summary>The request content was invalid.</summary>
    Validation = 1,

    /// <summary>The request conflicted with existing state.</summary>
    Conflict = 2,

    /// <summary>The target TODO item was not found.</summary>
    NotFound = 3,

    /// <summary>The authoritative database mutation succeeded but TODO.yaml projection failed.</summary>
    ProjectionFailed = 4,

    /// <summary>An external dependency failed after the local state changed.</summary>
    ExternalSyncFailed = 5,
}

/// <summary>TR-PLANNED-CORE-013: Result of a TODO mutation (create/update/delete).</summary>
public sealed record TodoMutationResult(
    bool Success,
    string? Error = null,
    TodoFlatItem? Item = null,
    TodoMutationFailureKind FailureKind = TodoMutationFailureKind.None);

/// <summary>TR-MCP-TODO-005: Result of querying TODO audit history.</summary>
public sealed record TodoAuditQueryResult(IReadOnlyList<TodoAuditEntry> Entries, int TotalCount);

/// <summary>TR-MCP-TODO-006: Status of database-authoritative TODO.yaml projection health and consistency.</summary>
public sealed record TodoProjectionStatusResult(
    string AuthoritativeStore,
    string AuthoritativeDataSource,
    string ProjectionTargetPath,
    bool ProjectionTargetExists,
    bool ProjectionConsistent,
    bool RepairRequired,
    string VerifiedAtUtc,
    string? LastImportedFromYamlUtc = null,
    string? LastProjectedToYamlUtc = null,
    string? LastProjectionFailureUtc = null,
    string? LastProjectionFailure = null,
    string? Message = null);

/// <summary>TR-MCP-TODO-006: Result of an operator-requested TODO.yaml projection repair attempt.</summary>
public sealed record TodoProjectionRepairResult(
    bool Success,
    string? Error,
    TodoProjectionStatusResult Status);

/// <summary>TR-MCP-TODO-005: Append-only audit entry for a TODO item.</summary>
public sealed record TodoAuditEntry
{
    /// <summary>Monotonic audit row identifier.</summary>
    public required long AuditId { get; init; }

    /// <summary>TODO item identifier.</summary>
    public required string TodoId { get; init; }

    /// <summary>Monotonic version for this TODO id.</summary>
    public required int Version { get; init; }

    /// <summary>Recorded action (imported, created, updated, deleted).</summary>
    public required string Action { get; init; }

    /// <summary>UTC timestamp when the history row was recorded.</summary>
    public required string RecordedAtUtc { get; init; }

    /// <summary>Post-mutation snapshot.</summary>
    public TodoFlatItem? Snapshot { get; init; }

    /// <summary>Pre-mutation snapshot.</summary>
    public TodoFlatItem? PreviousSnapshot { get; init; }

    /// <summary>Origin of the mutation or backfill operation.</summary>
    public string? Source { get; init; }
}

/// <summary>Request to move a TODO item to a different workspace.</summary>
public sealed record TodoMoveRequest
{
    /// <summary>Absolute path of the target workspace to move the item to. Required.</summary>
    public required string TargetWorkspacePath { get; init; }
}
