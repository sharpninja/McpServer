using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// TR-MCP-DIR-003: ViewModel for listing workspaces. Dispatches <see cref="ListWorkspacesQuery"/>
/// through the CQRS Dispatcher and exposes results as an observable collection.
/// </summary>
[ViewModelCommand("list-workspaces", Description = "List all registered workspaces")]
public partial class WorkspaceListViewModel : ObservableObject
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<WorkspaceListViewModel> _logger;


    /// <summary>Initializes a new <see cref="WorkspaceListViewModel"/>.</summary>
    /// <param name="dispatcher">The CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public WorkspaceListViewModel(Dispatcher dispatcher,
        ILogger<WorkspaceListViewModel> logger)
    {
        _logger = logger;
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
        InvokeOnUiThread(() =>
        {
            IsLoading = true;
            ErrorMessage = null;
        });
        try
        {
            var result = await RefreshCommand.DispatchAsync(ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                var items = result.Value.Items.ToArray();
                var totalCount = result.Value.TotalCount;

                InvokeOnUiThread(() =>
                {
                    Workspaces.Clear();
                    foreach (var ws in items)
                        Workspaces.Add(ws);
                    TotalCount = totalCount;
                });
            }
            else
            {
                var error = result.Error ?? "Unknown error loading workspaces.";
                InvokeOnUiThread(() => ErrorMessage = error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            var error = ex.Message;
            InvokeOnUiThread(() => ErrorMessage = error);
        }
        finally
        {
            InvokeOnUiThread(() => IsLoading = false);
        }
    }

    private static void InvokeOnUiThread(Action action)
    {
        if (TryInvokeTerminalGuiApplication(action))
            return;

        action();
    }

    private static bool TryInvokeTerminalGuiApplication(Action action)
    {
        var applicationType = Type.GetType("Terminal.Gui.Application, Terminal.Gui");
        if (applicationType is null)
            return false;

        var invokeMethod = applicationType.GetMethod(
            "Invoke",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            binder: null,
            types: [typeof(Action)],
            modifiers: null);

        if (invokeMethod is null)
            return false;

        invokeMethod.Invoke(obj: null, parameters: [action]);
        return true;
    }
}
