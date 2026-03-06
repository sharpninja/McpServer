using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for context search.</summary>
[ViewModelCommand("context-search", Description = "Search indexed context")]
public sealed partial class ContextSearchViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<ContextSearchPayload> _searchCommand;
    private readonly CqrsQueryCommand<ContextSourcesPayload> _sourcesCommand;
    private readonly ILogger<ContextSearchViewModel> _logger;

    /// <summary>Initializes a new context-search ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public ContextSearchViewModel(Dispatcher dispatcher, ILogger<ContextSearchViewModel> logger)
    {
        _logger = logger;
        _searchCommand = new CqrsQueryCommand<ContextSearchPayload>(dispatcher, BuildQuery);
        _sourcesCommand = new CqrsQueryCommand<ContextSourcesPayload>(dispatcher, static () => new ListContextSourcesQuery());
    }

    [ObservableProperty] private string _query = string.Empty;
    [ObservableProperty] private string? _sourceType;
    [ObservableProperty] private int _limit = 20;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private ContextSearchPayload? _searchResult;
    [ObservableProperty] private IReadOnlyList<string> _sourceTypes = Array.Empty<string>();

    /// <summary>Gets the command used to execute context searches.</summary>
    public IAsyncRelayCommand SearchCommand => _searchCommand;

    /// <summary>Primary command alias for ViewModel registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => SearchCommand;

    /// <summary>Gets the last CQRS dispatch result from a search run.</summary>
    public Result<ContextSearchPayload>? LastResult => _searchCommand.LastResult;

    /// <summary>Loads source types and executes search if a query is present.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await LoadSourcesAsync(ct).ConfigureAwait(true);

            if (string.IsNullOrWhiteSpace(Query))
            {
                SearchResult = null;
                return;
            }

            var result = await _searchCommand.DispatchAsync(ct).ConfigureAwait(true);
            if (!result.IsSuccess)
            {
                SearchResult = null;
                ErrorMessage = result.Error ?? "Unknown error running context search.";
                return;
            }

            SearchResult = result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SearchResult = null;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadSourcesAsync(CancellationToken ct)
    {
        var result = await _sourcesCommand.DispatchAsync(ct).ConfigureAwait(true);
        if (!result.IsSuccess || result.Value is null)
        {
            SourceTypes = Array.Empty<string>();
            return;
        }

        SourceTypes = result.Value.Sources
            .Select(static source => source.SourceType)
            .Where(static sourceType => !string.IsNullOrWhiteSpace(sourceType))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static sourceType => sourceType)
            .ToArray()!;
    }

    private SearchContextQuery BuildQuery() => new()
    {
        Query = Query,
        SourceType = string.IsNullOrWhiteSpace(SourceType) ? null : SourceType.Trim(),
        Limit = Limit <= 0 ? 20 : Limit,
    };
}

