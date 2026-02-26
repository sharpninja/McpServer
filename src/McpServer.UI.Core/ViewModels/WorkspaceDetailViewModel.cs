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
/// ViewModel for loading and exposing a single workspace detail record.
/// </summary>
[ViewModelCommand("get-workspace", Description = "Get workspace details by path")]
public sealed partial class WorkspaceDetailViewModel : AreaDetailViewModelBase<WorkspaceDetail>
{
    private readonly CqrsQueryCommand<WorkspaceDetail?> _getWorkspaceCommand;
    private readonly ILogger<WorkspaceDetailViewModel> _logger;


    /// <summary>Initializes a new instance of the workspace detail ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public WorkspaceDetailViewModel(Dispatcher dispatcher,
        ILogger<WorkspaceDetailViewModel> logger)
        : base(McpArea.Workspaces)
    {
        _logger = logger;
        _getWorkspaceCommand = new CqrsQueryCommand<WorkspaceDetail?>(
            dispatcher,
            () => new GetWorkspaceQuery(WorkspacePath));
    }

    /// <summary>Workspace path to load.</summary>
    [ObservableProperty]
    private string _workspacePath = "";

    /// <summary>Primary async command used by UI and <c>director exec</c>.</summary>
    public IAsyncRelayCommand GetWorkspaceCommand => _getWorkspaceCommand;

    /// <summary>Alias for ViewModel registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => GetWorkspaceCommand;

    /// <summary>Last CQRS dispatch result.</summary>
    public Result<WorkspaceDetail?>? LastResult => _getWorkspaceCommand.LastResult;

    /// <summary>Loads the workspace detail for <see cref="WorkspacePath"/>.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Loading workspace details...";

        try
        {
            if (string.IsNullOrWhiteSpace(WorkspacePath))
            {
                Detail = null;
                ErrorMessage = "WorkspacePath is required.";
                StatusMessage = "Workspace detail load failed.";
                return;
            }

            var result = await _getWorkspaceCommand.DispatchAsync(ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                Detail = null;
                ErrorMessage = result.Error ?? "Unknown error loading workspace details.";
                StatusMessage = "Workspace detail load failed.";
                return;
            }

            Detail = result.Value;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value is null
                ? "Workspace not found."
                : $"Loaded workspace '{result.Value.Name}'.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            Detail = null;
            ErrorMessage = ex.Message;
            StatusMessage = "Workspace detail load failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
