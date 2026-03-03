using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for listing context sources.</summary>
[ViewModelCommand("list-context-sources", Description = "List indexed context sources")]
internal sealed partial class ContextSourcesViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<ContextSourcesPayload> _command;

    public ContextSourcesViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsQueryCommand<ContextSourcesPayload>(dispatcher, static () => new ListContextSourcesQuery());
    }

    public IAsyncRelayCommand ListCommand => _command;
    public IAsyncRelayCommand PrimaryCommand => ListCommand;
    public Result<ContextSourcesPayload>? LastResult => _command.LastResult;
}

