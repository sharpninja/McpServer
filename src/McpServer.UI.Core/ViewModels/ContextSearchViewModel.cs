using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for context search.</summary>
[ViewModelCommand("context-search", Description = "Search indexed context")]
internal sealed partial class ContextSearchViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<ContextSearchPayload> _command;

    public ContextSearchViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsQueryCommand<ContextSearchPayload>(dispatcher, BuildQuery);
    }

    [ObservableProperty] private string _query = string.Empty;
    [ObservableProperty] private string? _sourceType;
    [ObservableProperty] private int _limit = 20;

    public IAsyncRelayCommand SearchCommand => _command;
    public IAsyncRelayCommand PrimaryCommand => SearchCommand;
    public Result<ContextSearchPayload>? LastResult => _command.LastResult;

    private SearchContextQuery BuildQuery() => new()
    {
        Query = Query,
        SourceType = string.IsNullOrWhiteSpace(SourceType) ? null : SourceType.Trim(),
        Limit = Limit <= 0 ? 20 : Limit,
    };
}

