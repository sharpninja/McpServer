using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for listing workspace-scoped agent assignments.
/// </summary>
[ViewModelCommand("workspace-agents-list", Description = "List workspace agent assignments")]
public sealed class WorkspaceAgentListViewModel : AreaListViewModelBase<WorkspaceAgentItem>
{
    private readonly Dispatcher _dispatcher;
    private readonly WorkspaceContextViewModel _workspaceContext;
    private readonly ILogger<WorkspaceAgentListViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceAgentListViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context.</param>
    /// <param name="logger">Logger instance.</param>
    public WorkspaceAgentListViewModel(
        Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<WorkspaceAgentListViewModel> logger)
        : base(McpArea.Agents)
    {
        _dispatcher = dispatcher;
        _workspaceContext = workspaceContext;
        _logger = logger;

        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
                _ = Task.Run(() => LoadAsync());
        };
    }

    /// <summary>Current active workspace path from the shared context.</summary>
    public string? ActiveWorkspacePath => _workspaceContext.ActiveWorkspacePath;

    /// <summary>
    /// Loads workspace assignments for the active (or explicitly provided) workspace.
    /// </summary>
    /// <param name="workspacePath">Optional workspace path override.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(string? workspacePath = null, CancellationToken ct = default)
    {
        if (IsLoading)
            return;

        var targetWorkspace = string.IsNullOrWhiteSpace(workspacePath)
            ? ActiveWorkspacePath
            : workspacePath;
        if (string.IsNullOrWhiteSpace(targetWorkspace))
        {
            ClearItems();
            StatusMessage = "No active workspace selected.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = $"Loading workspace agents for '{targetWorkspace}'...";
        try
        {
            var result = await _dispatcher.QueryAsync(new ListWorkspaceAgentsQuery(targetWorkspace), ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to load workspace agents.";
                StatusMessage = "Workspace agent load failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.TotalCount);
            StatusMessage = $"Loaded {Items.Count} workspace agents.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Workspace agent load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
