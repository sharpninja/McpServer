using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for workspace runtime actions: status, init, start, stop, and health probing.
/// </summary>
[ViewModelCommand("workspace-health-probe", Description = "Check workspace runtime status and health, and run init/start/stop actions")]
public sealed partial class WorkspaceHealthProbeViewModel : ObservableObject
{
    private readonly Dispatcher _dispatcher;
    private readonly CqrsQueryCommand<WorkspaceProcessState> _getStatusCommand;
    private readonly CqrsRelayCommand<WorkspaceInitInfo> _initializeCommand;
    private readonly CqrsRelayCommand<WorkspaceProcessState> _startCommand;
    private readonly CqrsRelayCommand<WorkspaceProcessState> _stopCommand;
    private readonly CqrsQueryCommand<WorkspaceHealthState> _checkHealthCommand;
    private readonly ILogger<WorkspaceHealthProbeViewModel> _logger;

    /// <summary>Initializes a new instance of the workspace runtime/health ViewModel.</summary>
    public WorkspaceHealthProbeViewModel(Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<WorkspaceHealthProbeViewModel> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        _getStatusCommand = new CqrsQueryCommand<WorkspaceProcessState>(dispatcher, () => new GetWorkspaceStatusQuery(WorkspacePath));
        _initializeCommand = new CqrsRelayCommand<WorkspaceInitInfo>(dispatcher, () => new InitWorkspaceCommand(WorkspacePath));
        _startCommand = new CqrsRelayCommand<WorkspaceProcessState>(dispatcher, () => new StartWorkspaceCommand(WorkspacePath));
        _stopCommand = new CqrsRelayCommand<WorkspaceProcessState>(dispatcher, () => new StopWorkspaceCommand(WorkspacePath));
        _checkHealthCommand = new CqrsQueryCommand<WorkspaceHealthState>(dispatcher, () => new CheckWorkspaceHealthQuery(WorkspacePath));
        WorkspacePath = workspaceContext.ActiveWorkspacePath ?? string.Empty;
        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
            {
                WorkspacePath = workspaceContext.ActiveWorkspacePath ?? string.Empty;
                ClearState();
            }
        };
    }

    /// <summary>Workspace path targeted by runtime operations.</summary>
    [ObservableProperty]
    private string _workspacePath = string.Empty;

    /// <summary>Whether an operation is active.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Last error message.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Last status message.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Last formatted process-status summary.</summary>
    [ObservableProperty]
    private string _processStatusText = string.Empty;

    /// <summary>Last formatted health summary.</summary>
    [ObservableProperty]
    private string _healthStatusText = string.Empty;

    /// <summary>Last process-state result.</summary>
    [ObservableProperty]
    private WorkspaceProcessState? _lastProcessState;

    /// <summary>Last health-probe result.</summary>
    [ObservableProperty]
    private WorkspaceHealthState? _lastHealthState;

    /// <summary>Last workspace-init result.</summary>
    [ObservableProperty]
    private WorkspaceInitInfo? _lastInitInfo;

    /// <summary>Timestamp of the last successful operation.</summary>
    [ObservableProperty]
    private DateTimeOffset? _lastUpdatedAt;

    /// <summary>Command that queries the current workspace process status.</summary>
    public IAsyncRelayCommand GetStatusCommand => _getStatusCommand;

    /// <summary>Command that runs the workspace initialization workflow.</summary>
    public IAsyncRelayCommand InitializeCommand => _initializeCommand;

    /// <summary>Command that starts the workspace host.</summary>
    public IAsyncRelayCommand StartCommand => _startCommand;

    /// <summary>Command that stops the workspace host.</summary>
    public IAsyncRelayCommand StopCommand => _stopCommand;

    /// <summary>Command that probes the workspace health endpoint.</summary>
    public IAsyncRelayCommand CheckHealthCommand => _checkHealthCommand;

    /// <summary>Primary command alias for ViewModel registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => CheckHealthCommand;

    /// <summary>Last process-status query result.</summary>
    public Result<WorkspaceProcessState>? LastStatusResult => _getStatusCommand.LastResult;

    /// <summary>Last workspace-init command result.</summary>
    public Result<WorkspaceInitInfo>? LastInitializeResult => _initializeCommand.LastResult;

    /// <summary>Last workspace-start command result.</summary>
    public Result<WorkspaceProcessState>? LastStartResult => _startCommand.LastResult;

    /// <summary>Last workspace-stop command result.</summary>
    public Result<WorkspaceProcessState>? LastStopResult => _stopCommand.LastResult;

    /// <summary>Last workspace-health query result.</summary>
    public Result<WorkspaceHealthState>? LastHealthResult => _checkHealthCommand.LastResult;

    /// <summary>Queries the current workspace process status.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task GetStatusAsync(CancellationToken ct = default)
    {
        await RunProcessOperationAsync(
                () => _getStatusCommand.DispatchAsync(ct),
                "Loading workspace status...",
                successMessagePrefix: "Workspace status loaded.")
            .ConfigureAwait(true);
    }

    /// <summary>Runs the workspace initialization workflow.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (!EnsureWorkspacePath())
            return;

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Initializing workspace...";

        try
        {
            var result = await _initializeCommand.DispatchAsync(ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Unknown error initializing workspace.";
                StatusMessage = "Workspace initialization failed.";
                return;
            }

            LastInitInfo = result.Value;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = $"Workspace initialization completed for '{result.Value.WorkspacePath}'.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Workspace initialization failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Starts the workspace host.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task StartAsync(CancellationToken ct = default)
    {
        await RunProcessOperationAsync(
                () => _startCommand.DispatchAsync(ct),
                "Starting workspace...",
                successMessagePrefix: "Workspace start completed.")
            .ConfigureAwait(true);
    }

    /// <summary>Stops the workspace host.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task StopAsync(CancellationToken ct = default)
    {
        await RunProcessOperationAsync(
                () => _stopCommand.DispatchAsync(ct),
                "Stopping workspace...",
                successMessagePrefix: "Workspace stop completed.")
            .ConfigureAwait(true);
    }

    /// <summary>Probes the workspace health endpoint.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task CheckHealthAsync(CancellationToken ct = default)
    {
        if (!EnsureWorkspacePath())
            return;

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Checking workspace health...";

        try
        {
            var result = await _checkHealthCommand.DispatchAsync(ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Unknown error checking workspace health.";
                StatusMessage = "Workspace health check failed.";
                return;
            }

            LastHealthState = result.Value;
            HealthStatusText = FormatHealthStatus(result.Value);
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value.Success
                ? "Workspace health check completed."
                : "Workspace health check reported an unhealthy result.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Workspace health check failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunProcessOperationAsync(
        Func<Task<Result<WorkspaceProcessState>>> action,
        string busyMessage,
        string successMessagePrefix)
    {
        if (!EnsureWorkspacePath())
            return;

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = busyMessage;

        try
        {
            var result = await action().ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Unknown workspace runtime error.";
                StatusMessage = busyMessage.Replace("...", " failed.");
                return;
            }

            LastProcessState = result.Value;
            ProcessStatusText = FormatProcessStatus(result.Value);
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = successMessagePrefix;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = busyMessage.Replace("...", " failed.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool EnsureWorkspacePath()
    {
        if (!string.IsNullOrWhiteSpace(WorkspacePath))
            return true;

        ErrorMessage = "WorkspacePath is required.";
        StatusMessage = "Workspace runtime action failed.";
        return false;
    }

    private void ClearState()
    {
        ErrorMessage = null;
        StatusMessage = null;
        ProcessStatusText = string.Empty;
        HealthStatusText = string.Empty;
        LastProcessState = null;
        LastHealthState = null;
        LastInitInfo = null;
    }

    private static string FormatProcessStatus(WorkspaceProcessState status)
    {
        var state = status.IsRunning ? "Running" : "Stopped";
        var pid = status.Pid.HasValue ? $"PID {status.Pid.Value}" : "PID n/a";
        var port = status.Port.HasValue ? $"Port {status.Port.Value}" : "Port n/a";
        var uptime = string.IsNullOrWhiteSpace(status.Uptime) ? string.Empty : $", Uptime {status.Uptime}";
        var error = string.IsNullOrWhiteSpace(status.Error) ? string.Empty : $" ({status.Error})";
        return $"{state}, {pid}, {port}{uptime}{error}";
    }

    private static string FormatHealthStatus(WorkspaceHealthState health)
    {
        var status = health.StatusCode > 0 ? $"HTTP {health.StatusCode}" : "HTTP n/a";
        var url = string.IsNullOrWhiteSpace(health.Url) ? string.Empty : $" @ {health.Url}";
        var body = string.IsNullOrWhiteSpace(health.Body)
            ? string.Empty
            : $" | {TruncateSingleLine(health.Body, 180)}";
        var error = string.IsNullOrWhiteSpace(health.Error) ? string.Empty : $" ({health.Error})";
        return $"{(health.Success ? "Healthy" : "Unhealthy")} {status}{url}{body}{error}";
    }

    private static string TruncateSingleLine(string text, int maxLength)
    {
        var singleLine = (text ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (singleLine.Length <= maxLength)
            return singleLine;

        return singleLine[..Math.Max(0, maxLength - 3)] + "...";
    }
}
