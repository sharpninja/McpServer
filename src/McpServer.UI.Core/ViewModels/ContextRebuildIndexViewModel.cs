using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for context index rebuilds.</summary>
[ViewModelCommand("rebuild-context-index", Description = "Rebuild context index")]
internal sealed partial class ContextRebuildIndexViewModel : ObservableObject
{
    private readonly CqrsRelayCommand<ContextRebuildResult> _command;

    public ContextRebuildIndexViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsRelayCommand<ContextRebuildResult>(dispatcher, static () => new RebuildContextIndexCommand());
    }

    public IAsyncRelayCommand RebuildCommand => _command;
    public IAsyncRelayCommand PrimaryCommand => RebuildCommand;
    public Result<ContextRebuildResult>? LastResult => _command.LastResult;
}

