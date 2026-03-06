using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for Health tab snapshot history and detail display.
/// Maintains a client-side history list of health checks and exposes an async command for checks.
/// </summary>
[ViewModelCommand("check-health", Description = "Check MCP server health and append a snapshot")]
public sealed class HealthSnapshotsViewModel : AreaListViewModelBase<HealthSnapshot>
{
    private readonly Dispatcher _dispatcher;
    private readonly WorkspaceContextViewModel _workspaceContext;
    private readonly CqrsQueryCommand<HealthSnapshot> _checkHealthCommand;
    private readonly ILogger<HealthSnapshotsViewModel> _logger;


    /// <summary>Initializes a new instance of the health snapshots ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context for reacting to workspace changes.</param>
    /// <param name="logger">Logger instance.</param>
    public HealthSnapshotsViewModel(Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<HealthSnapshotsViewModel> logger)
        : base(McpArea.Health)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _workspaceContext = workspaceContext;
        _checkHealthCommand = new CqrsQueryCommand<HealthSnapshot>(dispatcher, static () => new CheckHealthQuery());
        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
            {
                OnPropertyChanged(nameof(ActiveWorkspacePath));
                _logger.LogInformation("Workspace changed to '{WorkspacePath}' — clearing health snapshots",
                    workspaceContext.ActiveWorkspacePath);
                ClearItems();
            }
        };
    }

    /// <summary>
    /// Primary async command used by the UI and <c>director exec</c>.
    /// </summary>
    public IAsyncRelayCommand CheckHealthCommand => _checkHealthCommand;

    /// <summary>Alias for <see cref="CheckHealthCommand"/> for ViewModel registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => CheckHealthCommand;

    /// <summary>Last CQRS dispatch result.</summary>
    public Result<HealthSnapshot>? LastResult => _checkHealthCommand.LastResult;

    /// <summary>Current active workspace path selected in the shared context.</summary>
    public string? ActiveWorkspacePath => _workspaceContext.ActiveWorkspacePath;

    /// <summary>
    /// Executes the health check and appends the returned snapshot to the local history list.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task CheckAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Checking server health...";

        try
        {
            var result = await _checkHealthCommand.DispatchAsync(ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Unknown error checking health.";
                StatusMessage = "Health check failed.";
                return;
            }

            Items.Insert(0, result.Value);
            SelectedIndex = 0;
            TotalCount = Items.Count;
            LastRefreshedAt = DateTimeOffset.UtcNow;
            StatusMessage = $"Health check recorded at {result.Value.CheckedAt:HH:mm:ss}.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Health check failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Runs the Director workspace-initialization workflow through CQRS and updates status/error state.
    /// </summary>
    /// <param name="workspacePath">Absolute workspace path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dispatch result with initialization info on success.</returns>
    public async Task<Result<WorkspaceInitInfo>> InitializeWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            return Result<WorkspaceInitInfo>.Failure("WorkspacePath is required.");

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Initializing workspace...";

        try
        {
            var result = await _dispatcher.SendAsync(new InitWorkspaceCommand(workspacePath), ct).ConfigureAwait(true);
            if (result.IsSuccess)
            {
                StatusMessage = "Workspace initialization completed.";
                LastRefreshedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                ErrorMessage = result.Error ?? "Workspace initialization failed.";
                StatusMessage = "Workspace initialization failed.";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Workspace initialization failed.";
            return Result<WorkspaceInitInfo>.Failure(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Initializes the currently selected active workspace, if present.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dispatch result with initialization info on success.</returns>
    public Task<Result<WorkspaceInitInfo>> InitializeActiveWorkspaceAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ActiveWorkspacePath))
            return Task.FromResult(Result<WorkspaceInitInfo>.Failure("No active workspace is selected."));

        return InitializeWorkspaceAsync(ActiveWorkspacePath, ct);
    }
}
