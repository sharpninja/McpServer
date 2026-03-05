using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for deleting TODO items.</summary>
[ViewModelCommand("delete-todo", Description = "Delete TODO item")]
public sealed partial class DeleteTodoViewModel : ObservableObject
{
    private readonly CqrsRelayCommand<TodoMutationOutcome> _deleteCommand;

    /// <summary>Initializes a new delete-TODO ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    public DeleteTodoViewModel(Dispatcher dispatcher)
    {
        _deleteCommand = new CqrsRelayCommand<TodoMutationOutcome>(dispatcher, BuildCommand);
    }

    [ObservableProperty] private string _todoId = string.Empty;

    /// <summary>Delete command.</summary>
    public IAsyncRelayCommand DeleteCommand => _deleteCommand;

    /// <summary>Primary command alias for exec.</summary>
    public IAsyncRelayCommand PrimaryCommand => DeleteCommand;

    /// <summary>Last delete result.</summary>
    public Result<TodoMutationOutcome>? LastResult => _deleteCommand.LastResult;

    private DeleteTodoCommand BuildCommand() => new(TodoId);
}
