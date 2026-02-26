using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for the TODO tab list/grid.
/// Queries TODO items and exposes list-friendly summaries.
/// </summary>
[ViewModelCommand("list-todos", Description = "List TODO items")]
public sealed partial class TodoListViewModel : AreaListViewModelBase<TodoListItem>
{
    private readonly CqrsQueryCommand<ListTodosResult> _refreshCommand;

    /// <summary>Initializes a new instance of the TODO list ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    public TodoListViewModel(Dispatcher dispatcher)
        : base(McpArea.Todo)
    {
        _refreshCommand = new CqrsQueryCommand<ListTodosResult>(dispatcher, BuildQuery);
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
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading TODO items...";

        try
        {
            var result = await _refreshCommand.DispatchAsync(ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Unknown error loading TODO items.";
                StatusMessage = "TODO load failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.TotalCount);
            StatusMessage = $"Loaded {Items.Count} TODO items.";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
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
