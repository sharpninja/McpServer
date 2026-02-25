using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for triggering sync runs.</summary>
[ViewModelCommand("run-sync", Description = "Run ingestion sync")]
public sealed partial class RunSyncViewModel : ObservableObject
{
    private readonly CqrsRelayCommand<SyncRunSummary> _command;

    /// <summary>Initializes a new run-sync ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    public RunSyncViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsRelayCommand<SyncRunSummary>(dispatcher, static () => new RunSyncCommand());
    }

    /// <summary>Dispatches the sync-run command.</summary>
    public IAsyncRelayCommand RunCommand => _command;

    /// <summary>Primary command alias for <c>director exec</c>.</summary>
    public IAsyncRelayCommand PrimaryCommand => RunCommand;

    /// <summary>Result from the last dispatch.</summary>
    public Result<SyncRunSummary>? LastResult => _command.LastResult;
}

