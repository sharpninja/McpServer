namespace McpServer.Todo.Validation.Models;

/// <summary>A flattened TODO item with section and priority context.</summary>
public sealed class TodoFlatItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public bool Done { get; set; }
    public string? Estimate { get; set; }
    public string? Note { get; set; }
    public List<string>? Description { get; set; }
    public List<string>? TechnicalDetails { get; set; }
    public List<TodoFlatTask>? ImplementationTasks { get; set; }
    public string? CompletedDate { get; set; }
    public string? DoneSummary { get; set; }
    public string? Remaining { get; set; }
    public string? PriorityNote { get; set; }
    public string? Reference { get; set; }
    public List<string>? DependsOn { get; set; }
    public List<string>? FunctionalRequirements { get; set; }
    public List<string>? TechnicalRequirements { get; set; }
}

/// <summary>Flattened implementation task.</summary>
public sealed class TodoFlatTask
{
    public string Task { get; set; } = string.Empty;
    public bool Done { get; set; }
}

/// <summary>Result of a TODO query.</summary>
public sealed class TodoQueryResult
{
    public List<TodoFlatItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
}

/// <summary>Result of a TODO mutation (create/update/delete).</summary>
public sealed class TodoMutationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public TodoFlatItem? Item { get; set; }
}

/// <summary>Result of a requirements analysis.</summary>
public sealed class RequirementsAnalysisResult
{
    public bool Success { get; set; }
    public List<string>? FunctionalRequirements { get; set; }
    public List<string>? TechnicalRequirements { get; set; }
    public string? Error { get; set; }
    public string? CopilotResponse { get; set; }
}

/// <summary>Error response shape.</summary>
public sealed class ErrorResponse
{
    public string? Error { get; set; }
}
