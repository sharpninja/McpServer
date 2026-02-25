using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for execution-path diagnostics.</summary>
[ViewModelCommand("diagnostic-execution-path", Description = "Get diagnostic execution path")]
internal sealed partial class DiagnosticExecutionPathViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<DiagnosticExecutionPathSnapshot> _command;

    public DiagnosticExecutionPathViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsQueryCommand<DiagnosticExecutionPathSnapshot>(
            dispatcher,
            static () => new GetDiagnosticExecutionPathQuery());
    }

    public IAsyncRelayCommand GetCommand => _command;
    public IAsyncRelayCommand PrimaryCommand => GetCommand;
    public Result<DiagnosticExecutionPathSnapshot>? LastResult => _command.LastResult;
}

