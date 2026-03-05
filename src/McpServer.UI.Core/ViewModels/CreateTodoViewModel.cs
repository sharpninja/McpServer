using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for creating TODO items.</summary>
[ViewModelCommand("create-todo", Description = "Create TODO item")]
public sealed partial class CreateTodoViewModel : ObservableObject
{
    private readonly CqrsRelayCommand<TodoMutationOutcome> _createCommand;

    /// <summary>Initializes a new create-TODO ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    public CreateTodoViewModel(Dispatcher dispatcher)
    {
        _createCommand = new CqrsRelayCommand<TodoMutationOutcome>(dispatcher, BuildCommand);
    }

    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _section = "mvp-mcp";
    [ObservableProperty] private string _priority = "medium";
    [ObservableProperty] private string? _estimate;
    [ObservableProperty] private string? _note;
    [ObservableProperty] private string? _remaining;
    [ObservableProperty] private IReadOnlyList<string>? _description;
    [ObservableProperty] private IReadOnlyList<string>? _technicalDetails;
    [ObservableProperty] private IReadOnlyList<TodoTaskDetail>? _implementationTasks;
    [ObservableProperty] private IReadOnlyList<string>? _dependsOn;
    [ObservableProperty] private IReadOnlyList<string>? _functionalRequirements;
    [ObservableProperty] private IReadOnlyList<string>? _technicalRequirements;

    /// <summary>Create command.</summary>
    public IAsyncRelayCommand CreateCommand => _createCommand;

    /// <summary>Primary command alias for exec.</summary>
    public IAsyncRelayCommand PrimaryCommand => CreateCommand;

    /// <summary>Last create result.</summary>
    public Result<TodoMutationOutcome>? LastResult => _createCommand.LastResult;

    private CreateTodoCommand BuildCommand() => new()
    {
        Id = Id,
        Title = Title,
        Section = Section,
        Priority = Priority,
        Estimate = Estimate,
        Note = Note,
        Remaining = Remaining,
        Description = Description,
        TechnicalDetails = TechnicalDetails,
        ImplementationTasks = ImplementationTasks,
        DependsOn = DependsOn,
        FunctionalRequirements = FunctionalRequirements,
        TechnicalRequirements = TechnicalRequirements,
    };
}
