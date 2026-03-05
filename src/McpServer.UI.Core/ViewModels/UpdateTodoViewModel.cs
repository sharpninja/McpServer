using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for updating TODO items.</summary>
[ViewModelCommand("update-todo", Description = "Update TODO item")]
public sealed partial class UpdateTodoViewModel : ObservableObject
{
    private readonly CqrsRelayCommand<TodoMutationOutcome> _updateCommand;

    /// <summary>Initializes a new update-TODO ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    public UpdateTodoViewModel(Dispatcher dispatcher)
    {
        _updateCommand = new CqrsRelayCommand<TodoMutationOutcome>(dispatcher, BuildCommand);
    }

    [ObservableProperty] private string _todoId = string.Empty;
    [ObservableProperty] private string? _title;
    [ObservableProperty] private string? _section;
    [ObservableProperty] private string? _priority;
    [ObservableProperty] private bool? _done;
    [ObservableProperty] private string? _estimate;
    [ObservableProperty] private string? _note;
    [ObservableProperty] private string? _completedDate;
    [ObservableProperty] private string? _doneSummary;
    [ObservableProperty] private string? _remaining;
    [ObservableProperty] private IReadOnlyList<string>? _description;
    [ObservableProperty] private IReadOnlyList<string>? _technicalDetails;
    [ObservableProperty] private IReadOnlyList<TodoTaskDetail>? _implementationTasks;
    [ObservableProperty] private IReadOnlyList<string>? _dependsOn;
    [ObservableProperty] private IReadOnlyList<string>? _functionalRequirements;
    [ObservableProperty] private IReadOnlyList<string>? _technicalRequirements;

    /// <summary>Update command.</summary>
    public IAsyncRelayCommand UpdateCommand => _updateCommand;

    /// <summary>Primary command alias for exec.</summary>
    public IAsyncRelayCommand PrimaryCommand => UpdateCommand;

    /// <summary>Last update result.</summary>
    public Result<TodoMutationOutcome>? LastResult => _updateCommand.LastResult;

    private UpdateTodoCommand BuildCommand() => new()
    {
        TodoId = TodoId,
        Title = Title,
        Section = Section,
        Priority = Priority,
        Done = Done,
        Estimate = Estimate,
        Note = Note,
        CompletedDate = CompletedDate,
        DoneSummary = DoneSummary,
        Remaining = Remaining,
        Description = Description,
        TechnicalDetails = TechnicalDetails,
        ImplementationTasks = ImplementationTasks,
        DependsOn = DependsOn,
        FunctionalRequirements = FunctionalRequirements,
        TechnicalRequirements = TechnicalRequirements,
    };
}
