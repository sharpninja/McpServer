using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for appsettings-path diagnostics.</summary>
[ViewModelCommand("diagnostic-appsettings-path", Description = "Get diagnostic appsettings path")]
internal sealed partial class DiagnosticAppSettingsPathViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<DiagnosticAppSettingsSnapshot> _command;

    public DiagnosticAppSettingsPathViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsQueryCommand<DiagnosticAppSettingsSnapshot>(
            dispatcher,
            static () => new GetDiagnosticAppSettingsPathQuery());
    }

    public IAsyncRelayCommand GetCommand => _command;
    public IAsyncRelayCommand PrimaryCommand => GetCommand;
    public Result<DiagnosticAppSettingsSnapshot>? LastResult => _command.LastResult;
}

