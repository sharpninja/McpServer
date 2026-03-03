using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for context pack generation.</summary>
[ViewModelCommand("context-pack", Description = "Build deterministic context pack")]
internal sealed partial class ContextPackViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<ContextPackPayload> _command;

    public ContextPackViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsQueryCommand<ContextPackPayload>(dispatcher, BuildQuery);
    }

    [ObservableProperty] private string _query = string.Empty;
    [ObservableProperty] private string? _queryId;
    [ObservableProperty] private int _limit = 20;

    public IAsyncRelayCommand PackCommand => _command;
    public IAsyncRelayCommand PrimaryCommand => PackCommand;
    public Result<ContextPackPayload>? LastResult => _command.LastResult;

    private PackContextQuery BuildQuery() => new()
    {
        Query = Query,
        QueryId = string.IsNullOrWhiteSpace(QueryId) ? null : QueryId.Trim(),
        Limit = Limit <= 0 ? 20 : Limit,
    };
}

