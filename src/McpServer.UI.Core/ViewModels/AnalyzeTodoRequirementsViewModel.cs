using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for TODO requirements analysis.</summary>
[ViewModelCommand("todo-requirements", Description = "Analyze TODO requirements and return FR/TR associations")]
internal sealed partial class AnalyzeTodoRequirementsViewModel : ObservableObject
{
    private readonly CqrsRelayCommand<TodoRequirementsAnalysis> _command;

    public AnalyzeTodoRequirementsViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsRelayCommand<TodoRequirementsAnalysis>(dispatcher, BuildCommand);
    }

    [ObservableProperty] private string _todoId = string.Empty;

    public IAsyncRelayCommand AnalyzeCommand => _command;

    public IAsyncRelayCommand PrimaryCommand => AnalyzeCommand;

    public Result<TodoRequirementsAnalysis>? LastResult => _command.LastResult;

    private AnalyzeTodoRequirementsCommand BuildCommand() => new(TodoId);
}
