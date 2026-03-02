namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Service for querying and managing TODO items from the YAML file.
/// Provides CRUD operations and search by keyword, priority, or id.
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
}

/// <summary>TR-PLANNED-013: Query parameters for searching TODO items.</summary>
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

/// <summary>TR-PLANNED-013: Result of a TODO query.</summary>
public sealed record TodoQueryResult(IReadOnlyList<TodoFlatItem> Items, int TotalCount);

/// <summary>TR-PLANNED-013: A flattened TODO item with section and priority context.</summary>
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

    /// <summary>IDs of TODO items this item depends on.</summary>
    public IReadOnlyList<string>? DependsOn { get; init; }

    /// <summary>Associated functional requirement IDs (e.g. FR-LOC-001).</summary>
    public IReadOnlyList<string>? FunctionalRequirements { get; init; }

    /// <summary>Associated technical requirement IDs (e.g. TR-LOC-001).</summary>
    public IReadOnlyList<string>? TechnicalRequirements { get; init; }
}

/// <summary>TR-PLANNED-013: Flattened implementation task.</summary>
public sealed record TodoFlatTask(string Task, bool Done);

/// <summary>TR-PLANNED-013: Request to create a new TODO item.</summary>
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

    /// <summary>IDs of TODO items this item depends on.</summary>
    public IReadOnlyList<string>? DependsOn { get; init; }

    /// <summary>Associated functional requirement IDs.</summary>
    public IReadOnlyList<string>? FunctionalRequirements { get; init; }

    /// <summary>Associated technical requirement IDs.</summary>
    public IReadOnlyList<string>? TechnicalRequirements { get; init; }
}

/// <summary>TR-PLANNED-013: Request to update an existing TODO item.</summary>
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

    /// <summary>Updated dependency list (null = no change).</summary>
    public IReadOnlyList<string>? DependsOn { get; init; }

    /// <summary>Updated functional requirement IDs (null = no change).</summary>
    public IReadOnlyList<string>? FunctionalRequirements { get; init; }

    /// <summary>Updated technical requirement IDs (null = no change).</summary>
    public IReadOnlyList<string>? TechnicalRequirements { get; init; }
}

/// <summary>TR-PLANNED-013: Result of a TODO mutation (create/update/delete).</summary>
public sealed record TodoMutationResult(bool Success, string? Error = null, TodoFlatItem? Item = null);

/// <summary>Request to move a TODO item to a different workspace.</summary>
public sealed record TodoMoveRequest
{
    /// <summary>Absolute path of the target workspace to move the item to. Required.</summary>
    public required string TargetWorkspacePath { get; init; }
}
