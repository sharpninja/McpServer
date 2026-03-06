using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for the Sessions tab list/grid.
/// Queries the session-log endpoint and exposes summaries as a list.
/// </summary>
[ViewModelCommand("list-session-logs", Description = "List session logs")]
public sealed partial class SessionLogListViewModel : AreaListViewModelBase<SessionLogSummary>
{
    private readonly CqrsQueryCommand<ListSessionLogsResult> _refreshCommand;
    private readonly ILogger<SessionLogListViewModel> _logger;


    /// <summary>Initializes a new instance of the session log list ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context for reacting to workspace changes.</param>
    /// <param name="logger">Logger instance.</param>
    public SessionLogListViewModel(Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<SessionLogListViewModel> logger)
        : base(McpArea.SessionLogs)
    {
        _logger = logger;
        _refreshCommand = new CqrsQueryCommand<ListSessionLogsResult>(dispatcher, BuildQuery);
        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
                _ = Task.Run(() => LoadAsync());
        };
    }

    /// <summary>Filter by source/agent.</summary>
    [ObservableProperty]
    private string? _agent;

    /// <summary>Filter by model.</summary>
    [ObservableProperty]
    private string? _model;

    /// <summary>Full-text search filter.</summary>
    [ObservableProperty]
    private string? _text;

    /// <summary>Page size.</summary>
    [ObservableProperty]
    private int _limit = 20;

    /// <summary>Page offset.</summary>
    [ObservableProperty]
    private int _offset;

    /// <summary>Refresh command (also the primary command for exec).</summary>
    public IAsyncRelayCommand RefreshCommand => _refreshCommand;

    /// <summary>Primary command alias for registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => RefreshCommand;

    /// <summary>Last query result.</summary>
    public Result<ListSessionLogsResult>? LastResult => _refreshCommand.LastResult;

    /// <summary>Loads session logs into the list.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading session logs...";

        try
        {
            var result = await _refreshCommand.DispatchAsync(ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Unknown error loading session logs.";
                StatusMessage = "Session log load failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.TotalCount);
            StatusMessage = $"Loaded {Items.Count} session logs.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Session log load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private ListSessionLogsQuery BuildQuery() => new()
    {
        Agent = Agent,
        Model = Model,
        Text = Text,
        Limit = Limit <= 0 ? 20 : Limit,
        Offset = Math.Max(0, Offset),
    };
}
