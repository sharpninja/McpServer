using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for repository listing.</summary>
[ViewModelCommand("list-repo-entries", Description = "List repository files/directories")]
internal sealed partial class RepoListViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<RepoListResultView> _command;

    public RepoListViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsQueryCommand<RepoListResultView>(dispatcher, BuildQuery);
    }

    [ObservableProperty] private string? _path;

    public IAsyncRelayCommand ListCommand => _command;
    public IAsyncRelayCommand PrimaryCommand => ListCommand;
    public Result<RepoListResultView>? LastResult => _command.LastResult;

    private ListRepoEntriesQuery BuildQuery() => new() { Path = string.IsNullOrWhiteSpace(Path) ? null : Path.Trim() };
}

