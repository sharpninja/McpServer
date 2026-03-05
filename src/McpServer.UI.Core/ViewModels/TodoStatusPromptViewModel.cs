using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for TODO status prompt generation.</summary>
[ViewModelCommand("todo-prompt-status", Description = "Generate TODO status prompt output")]
internal sealed partial class TodoStatusPromptViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<TodoPromptOutput> _command;

    public TodoStatusPromptViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsQueryCommand<TodoPromptOutput>(dispatcher, BuildQuery);
    }

    [ObservableProperty] private string _todoId = string.Empty;

    public IAsyncRelayCommand GenerateCommand => _command;

    public IAsyncRelayCommand PrimaryCommand => GenerateCommand;

    public Result<TodoPromptOutput>? LastResult => _command.LastResult;

    private GenerateTodoStatusPromptQuery BuildQuery() => new(TodoId);
}
