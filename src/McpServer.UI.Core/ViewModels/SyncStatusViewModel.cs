using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for sync status.</summary>
[ViewModelCommand("get-sync-status", Description = "Get sync status")]
public sealed partial class SyncStatusViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<SyncStatusSnapshot> _command;

    /// <summary>Initializes a new sync-status ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    public SyncStatusViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsQueryCommand<SyncStatusSnapshot>(dispatcher, static () => new GetSyncStatusQuery());
    }

    /// <summary>Dispatches the sync-status query.</summary>
    public IAsyncRelayCommand GetStatusCommand => _command;

    /// <summary>Primary command alias for <c>director exec</c>.</summary>
    public IAsyncRelayCommand PrimaryCommand => GetStatusCommand;

    /// <summary>Result from the last dispatch.</summary>
    public Result<SyncStatusSnapshot>? LastResult => _command.LastResult;
}

