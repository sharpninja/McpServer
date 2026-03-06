namespace McpServer.Todo.Validation.Models;

/// <summary>A flattened TODO item with section and priority context.</summary>
public sealed class TodoFlatItem
{
    /// <summary>
    /// Gets or sets <c>Id</c> for validation payload/state handling.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>Title</c> for validation payload/state handling.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>Section</c> for validation payload/state handling.
    /// </summary>
    public string Section { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>Priority</c> for validation payload/state handling.
    /// </summary>
    public string Priority { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>Done</c> for validation payload/state handling.
    /// </summary>
    public bool Done { get; set; }
    /// <summary>
    /// Gets or sets <c>Estimate</c> for validation payload/state handling.
    /// </summary>
    public string? Estimate { get; set; }
    /// <summary>
    /// Gets or sets <c>Note</c> for validation payload/state handling.
    /// </summary>
    public string? Note { get; set; }
    /// <summary>
    /// Gets or sets <c>Description</c> for validation payload/state handling.
    /// </summary>
    public List<string>? Description { get; set; }
    /// <summary>
    /// Gets or sets <c>TechnicalDetails</c> for validation payload/state handling.
    /// </summary>
    public List<string>? TechnicalDetails { get; set; }
    /// <summary>
    /// Gets or sets <c>ImplementationTasks</c> for validation payload/state handling.
    /// </summary>
    public List<TodoFlatTask>? ImplementationTasks { get; set; }
    /// <summary>
    /// Gets or sets <c>CompletedDate</c> for validation payload/state handling.
    /// </summary>
    public string? CompletedDate { get; set; }
    /// <summary>
    /// Gets or sets <c>DoneSummary</c> for validation payload/state handling.
    /// </summary>
    public string? DoneSummary { get; set; }
    /// <summary>
    /// Gets or sets <c>Remaining</c> for validation payload/state handling.
    /// </summary>
    public string? Remaining { get; set; }
    /// <summary>
    /// Gets or sets <c>PriorityNote</c> for validation payload/state handling.
    /// </summary>
    public string? PriorityNote { get; set; }
    /// <summary>
    /// Gets or sets <c>Reference</c> for validation payload/state handling.
    /// </summary>
    public string? Reference { get; set; }
    /// <summary>
    /// Gets or sets <c>DependsOn</c> for validation payload/state handling.
    /// </summary>
    public List<string>? DependsOn { get; set; }
    /// <summary>
    /// Gets or sets <c>FunctionalRequirements</c> for validation payload/state handling.
    /// </summary>
    public List<string>? FunctionalRequirements { get; set; }
    /// <summary>
    /// Gets or sets <c>TechnicalRequirements</c> for validation payload/state handling.
    /// </summary>
    public List<string>? TechnicalRequirements { get; set; }
}

/// <summary>Flattened implementation task.</summary>
public sealed class TodoFlatTask
{
    /// <summary>
    /// Gets or sets <c>Task</c> for validation payload/state handling.
    /// </summary>
    public string Task { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>Done</c> for validation payload/state handling.
    /// </summary>
    public bool Done { get; set; }
}

/// <summary>Result of a TODO query.</summary>
public sealed class TodoQueryResult
{
    /// <summary>
    /// Gets or sets <c>Items</c> for validation payload/state handling.
    /// </summary>
    public List<TodoFlatItem> Items { get; set; } = [];
    /// <summary>
    /// Gets or sets <c>TotalCount</c> for validation payload/state handling.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>Result of a TODO mutation (create/update/delete).</summary>
public sealed class TodoMutationResult
{
    /// <summary>
    /// Gets or sets <c>Success</c> for validation payload/state handling.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets <c>Error</c> for validation payload/state handling.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets <c>Item</c> for validation payload/state handling.
    /// </summary>
    public TodoFlatItem? Item { get; set; }
}

/// <summary>Result of a requirements analysis.</summary>
public sealed class RequirementsAnalysisResult
{
    /// <summary>
    /// Gets or sets <c>Success</c> for validation payload/state handling.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets <c>FunctionalRequirements</c> for validation payload/state handling.
    /// </summary>
    public List<string>? FunctionalRequirements { get; set; }
    /// <summary>
    /// Gets or sets <c>TechnicalRequirements</c> for validation payload/state handling.
    /// </summary>
    public List<string>? TechnicalRequirements { get; set; }
    /// <summary>
    /// Gets or sets <c>Error</c> for validation payload/state handling.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets <c>CopilotResponse</c> for validation payload/state handling.
    /// </summary>
    public string? CopilotResponse { get; set; }
}

/// <summary>Error response shape.</summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// Gets or sets <c>Error</c> for validation payload/state handling.
    /// </summary>
    public string? Error { get; set; }
}
