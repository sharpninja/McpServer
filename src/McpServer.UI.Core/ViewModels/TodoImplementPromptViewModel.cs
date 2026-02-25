using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for TODO implement prompt generation.</summary>
[ViewModelCommand("todo-prompt-implement", Description = "Generate TODO implementation prompt output")]
internal sealed partial class TodoImplementPromptViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<TodoPromptOutput> _command;

    public TodoImplementPromptViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsQueryCommand<TodoPromptOutput>(dispatcher, BuildQuery);
    }

    [ObservableProperty] private string _todoId = string.Empty;

    public IAsyncRelayCommand GenerateCommand => _command;

    public IAsyncRelayCommand PrimaryCommand => GenerateCommand;

    public Result<TodoPromptOutput>? LastResult => _command.LastResult;

    private GenerateTodoImplementPromptQuery BuildQuery() => new(TodoId);
}
