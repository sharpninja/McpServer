using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query to list TODO items with optional filters.</summary>
public sealed record ListTodosQuery : IQuery<ListTodosResult>
{
    /// <summary>Optional keyword filter.</summary>
    public string? Keyword { get; init; }

    /// <summary>Optional priority filter.</summary>
    public string? Priority { get; init; }

    /// <summary>Optional section filter.</summary>
    public string? Section { get; init; }

    /// <summary>Optional exact TODO ID filter.</summary>
    public string? Id { get; init; }

    /// <summary>Optional completion-state filter.</summary>
    public bool? Done { get; init; }
}

/// <summary>Result of a TODO list query.</summary>
public sealed record ListTodosResult(IReadOnlyList<TodoListItem> Items, int TotalCount);

/// <summary>List-friendly TODO item summary.</summary>
public sealed record TodoListItem(
    string Id,
    string Title,
    string Section,
    string Priority,
    bool Done,
    string? Estimate);

/// <summary>Query to load a single TODO item by ID.</summary>
public sealed record GetTodoQuery(string TodoId) : IQuery<TodoDetail?>;

/// <summary>Detailed TODO item view.</summary>
public sealed record TodoDetail(
    string Id,
    string Title,
    string Section,
    string Priority,
    bool Done,
    string? Estimate,
    string? Note,
    IReadOnlyList<string> Description,
    IReadOnlyList<string> TechnicalDetails,
    IReadOnlyList<TodoTaskDetail> ImplementationTasks,
    string? CompletedDate,
    string? DoneSummary,
    string? Remaining,
    string? PriorityNote,
    string? Reference,
    IReadOnlyList<string> DependsOn,
    IReadOnlyList<string> FunctionalRequirements,
    IReadOnlyList<string> TechnicalRequirements);

/// <summary>Detail view of a TODO sub-task.</summary>
public sealed record TodoTaskDetail(string Task, bool Done);

/// <summary>Typed result of a TODO create/update/delete mutation.</summary>
public sealed record TodoMutationOutcome(
    bool Success,
    string? Error,
    TodoDetail? Item);

/// <summary>Typed result of TODO requirements analysis.</summary>
public sealed record TodoRequirementsAnalysis(
    bool Success,
    IReadOnlyList<string> FunctionalRequirements,
    IReadOnlyList<string> TechnicalRequirements,
    string? Error,
    string? CopilotResponse);

/// <summary>Typed result of a generated TODO prompt stream aggregated to text.</summary>
public sealed record TodoPromptOutput(
    string TodoId,
    string PromptType,
    IReadOnlyList<string> Lines,
    string Text);

/// <summary>Command to create a TODO item.</summary>
public sealed record CreateTodoCommand : ICommand<TodoMutationOutcome>
{
    /// <summary>Item ID (required).</summary>
    public required string Id { get; init; }

    /// <summary>Title (required).</summary>
    public required string Title { get; init; }

    /// <summary>Section (required).</summary>
    public required string Section { get; init; }

    /// <summary>Priority (required).</summary>
    public required string Priority { get; init; }

    /// <summary>Optional estimate.</summary>
    public string? Estimate { get; init; }

    /// <summary>Optional note.</summary>
    public string? Note { get; init; }

    /// <summary>Optional remaining-work note.</summary>
    public string? Remaining { get; init; }

    /// <summary>Optional description lines.</summary>
    public IReadOnlyList<string>? Description { get; init; }

    /// <summary>Optional technical details.</summary>
    public IReadOnlyList<string>? TechnicalDetails { get; init; }

    /// <summary>Optional implementation task checklist.</summary>
    public IReadOnlyList<TodoTaskDetail>? ImplementationTasks { get; init; }

    /// <summary>Optional dependency IDs.</summary>
    public IReadOnlyList<string>? DependsOn { get; init; }

    /// <summary>Optional associated functional requirement IDs.</summary>
    public IReadOnlyList<string>? FunctionalRequirements { get; init; }

    /// <summary>Optional associated technical requirement IDs.</summary>
    public IReadOnlyList<string>? TechnicalRequirements { get; init; }
}

/// <summary>Command to update an existing TODO item.</summary>
public sealed record UpdateTodoCommand : ICommand<TodoMutationOutcome>
{
    /// <summary>ID of the item to update.</summary>
    public required string TodoId { get; init; }

    /// <summary>Updated title (null = no change).</summary>
    public string? Title { get; init; }

    /// <summary>Updated section (null = no change).</summary>
    public string? Section { get; init; }

    /// <summary>Updated priority (null = no change).</summary>
    public string? Priority { get; init; }

    /// <summary>Updated completion state (null = no change).</summary>
    public bool? Done { get; init; }

    /// <summary>Updated estimate (null = no change).</summary>
    public string? Estimate { get; init; }

    /// <summary>Updated note (null = no change).</summary>
    public string? Note { get; init; }

    /// <summary>Updated completed date (null = no change).</summary>
    public string? CompletedDate { get; init; }

    /// <summary>Updated done summary (null = no change).</summary>
    public string? DoneSummary { get; init; }

    /// <summary>Updated remaining-work note (null = no change).</summary>
    public string? Remaining { get; init; }

    /// <summary>Updated description lines (null = no change).</summary>
    public IReadOnlyList<string>? Description { get; init; }

    /// <summary>Updated technical details (null = no change).</summary>
    public IReadOnlyList<string>? TechnicalDetails { get; init; }

    /// <summary>Updated implementation tasks (null = no change).</summary>
    public IReadOnlyList<TodoTaskDetail>? ImplementationTasks { get; init; }

    /// <summary>Updated dependency IDs (null = no change).</summary>
    public IReadOnlyList<string>? DependsOn { get; init; }

    /// <summary>Updated functional requirement IDs (null = no change).</summary>
    public IReadOnlyList<string>? FunctionalRequirements { get; init; }

    /// <summary>Updated technical requirement IDs (null = no change).</summary>
    public IReadOnlyList<string>? TechnicalRequirements { get; init; }
}

/// <summary>Command to delete a TODO item.</summary>
public sealed record DeleteTodoCommand(string TodoId) : ICommand<TodoMutationOutcome>;

/// <summary>Command to analyze TODO requirements and update FR/TR links.</summary>
public sealed record AnalyzeTodoRequirementsCommand(string TodoId) : ICommand<TodoRequirementsAnalysis>;

/// <summary>Query to generate a TODO status prompt output.</summary>
public sealed record GenerateTodoStatusPromptQuery(string TodoId) : IQuery<TodoPromptOutput>;

/// <summary>Query to generate a TODO implement prompt output.</summary>
public sealed record GenerateTodoImplementPromptQuery(string TodoId) : IQuery<TodoPromptOutput>;

/// <summary>Query to generate a TODO plan prompt output.</summary>
public sealed record GenerateTodoPlanPromptQuery(string TodoId) : IQuery<TodoPromptOutput>;
