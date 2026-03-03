using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for writing repository files.</summary>
[ViewModelCommand("write-repo-file", Description = "Write repository file content")]
internal sealed partial class WriteRepoFileViewModel : ObservableObject
{
    private readonly CqrsRelayCommand<RepoWriteOutcome> _command;

    public WriteRepoFileViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsRelayCommand<RepoWriteOutcome>(dispatcher, () => new WriteRepoFileCommand(Path, Content));
    }

    [ObservableProperty] private string _path = string.Empty;
    [ObservableProperty] private string _content = string.Empty;

    public IAsyncRelayCommand WriteCommand => _command;
    public IAsyncRelayCommand PrimaryCommand => WriteCommand;
    public Result<RepoWriteOutcome>? LastResult => _command.LastResult;
}

