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
/// ViewModel for the TODO tab list/grid.
/// Queries TODO items and exposes list-friendly summaries.
/// </summary>
[ViewModelCommand("list-todos", Description = "List TODO items")]
public sealed partial class TodoListViewModel : AreaListViewModelBase<TodoListItem>
{
    private readonly CqrsQueryCommand<ListTodosResult> _refreshCommand;
    private readonly ILogger<TodoListViewModel> _logger;


    /// <summary>Initializes a new instance of the TODO list ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context for reacting to workspace changes.</param>
    /// <param name="logger">Logger instance.</param>
    public TodoListViewModel(Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<TodoListViewModel> logger)
        : base(McpArea.Todo)
    {
        _logger = logger;
        _refreshCommand = new CqrsQueryCommand<ListTodosResult>(dispatcher, BuildQuery);
        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
            {
                _logger.LogInformation("Workspace changed to '{WorkspacePath}' — scheduling TODO reload",
                    workspaceContext.ActiveWorkspacePath);
                _ = Task.Run(() => LoadAsync());
            }
        };
    }

    /// <summary>Optional keyword filter.</summary>
    [ObservableProperty]
    private string? _keyword;

    /// <summary>Optional priority filter.</summary>
    [ObservableProperty]
    private string? _priority;

    /// <summary>Optional section filter.</summary>
    [ObservableProperty]
    private string? _section;

    /// <summary>Optional exact ID filter.</summary>
    [ObservableProperty]
    private string? _todoId;

    /// <summary>Optional completion-state filter.</summary>
    [ObservableProperty]
    private bool? _done;

    /// <summary>Refresh command (also the primary command for exec).</summary>
    public IAsyncRelayCommand RefreshCommand => _refreshCommand;

    /// <summary>Primary command alias for registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => RefreshCommand;

    /// <summary>Last query result.</summary>
    public Result<ListTodosResult>? LastResult => _refreshCommand.LastResult;

    /// <summary>Loads TODO items into the list.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("LoadAsync called — building query with filters: Keyword={Keyword}, Priority={Priority}, Section={Section}, Id={Id}, Done={Done}",
            Keyword, Priority, Section, TodoId, Done);

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading TODO items...";

        try
        {
            var result = await _refreshCommand.DispatchAsync(ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                _logger.LogWarning("LoadAsync failed: {Error}", result.Error ?? "null result");
                ErrorMessage = result.Error ?? "Unknown error loading TODO items.";
                StatusMessage = "TODO load failed.";
                return;
            }

            _logger.LogInformation("LoadAsync succeeded — {Count} items returned (total {Total})",
                result.Value.Items.Count, result.Value.TotalCount);

            SetItems(result.Value.Items, result.Value.TotalCount);
            StatusMessage = $"Loaded {Items.Count} TODO items.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "TODO load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private ListTodosQuery BuildQuery() => new()
    {
        Keyword = NormalizeFilter(Keyword),
        Priority = NormalizeFilter(Priority),
        Section = NormalizeFilter(Section),
        Id = NormalizeFilter(TodoId),
        Done = Done,
    };

    private static string? NormalizeFilter(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
