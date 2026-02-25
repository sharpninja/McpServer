using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// TR-MCP-DIR-003: ViewModel for listing workspaces. Dispatches <see cref="ListWorkspacesQuery"/>
/// through the CQRS Dispatcher and exposes results as an observable collection.
/// </summary>
[ViewModelCommand("list-workspaces", Description = "List all registered workspaces")]
public partial class WorkspaceListViewModel : ObservableObject
{
    private readonly Dispatcher _dispatcher;

    /// <summary>Initializes a new <see cref="WorkspaceListViewModel"/>.</summary>
    /// <param name="dispatcher">The CQRS dispatcher.</param>
    public WorkspaceListViewModel(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        RefreshCommand = new CqrsQueryCommand<ListWorkspacesResult>(dispatcher, () => new ListWorkspacesQuery());
    }

    /// <summary>The workspaces loaded from the server.</summary>
    public ObservableCollection<WorkspaceSummary> Workspaces { get; } = [];

    /// <summary>Total count from the last query.</summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>Whether data is currently loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Error message from the last load attempt.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>The primary command — refreshes the workspace list.</summary>
    public CqrsQueryCommand<ListWorkspacesResult> RefreshCommand { get; }

    /// <summary>Alias for <see cref="RefreshCommand"/> for <see cref="IViewModelRegistry"/> discovery.</summary>
    public CqrsQueryCommand<ListWorkspacesResult> PrimaryCommand => RefreshCommand;

    /// <summary>The result from the last query execution.</summary>
    public Result<ListWorkspacesResult>? LastResult => RefreshCommand.LastResult;

    /// <summary>Loads workspaces by dispatching the query and populating the collection.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await RefreshCommand.DispatchAsync(ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                Workspaces.Clear();
                foreach (var ws in result.Value.Items)
                    Workspaces.Add(ws);
                TotalCount = result.Value.TotalCount;
            }
            else
            {
                ErrorMessage = result.Error ?? "Unknown error loading workspaces.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
