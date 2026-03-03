using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for reading repository files.</summary>
[ViewModelCommand("get-repo-file", Description = "Read repository file content")]
internal sealed partial class RepoFileViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<RepoFileDetail> _command;

    public RepoFileViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsQueryCommand<RepoFileDetail>(dispatcher, () => new GetRepoFileQuery(Path));
    }

    [ObservableProperty] private string _path = string.Empty;

    public IAsyncRelayCommand GetFileCommand => _command;
    public IAsyncRelayCommand PrimaryCommand => GetFileCommand;
    public Result<RepoFileDetail>? LastResult => _command.LastResult;
}

