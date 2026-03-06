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
/// ViewModel for loading and exposing a single session log detail record.
/// </summary>
[ViewModelCommand("get-session-log", Description = "Get session log details by session id")]
public sealed partial class SessionLogDetailViewModel : AreaDetailViewModelBase<SessionLogDetail>
{
    private readonly CqrsQueryCommand<SessionLogDetail?> _loadCommand;
    private readonly ILogger<SessionLogDetailViewModel> _logger;


    /// <summary>Initializes a new instance of the session log detail ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context for reacting to workspace changes.</param>
    /// <param name="logger">Logger instance.</param>
    public SessionLogDetailViewModel(Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<SessionLogDetailViewModel> logger)
        : base(McpArea.SessionLogs)
    {
        _logger = logger;
        _loadCommand = new CqrsQueryCommand<SessionLogDetail?>(
            dispatcher,
            () => new GetSessionLogQuery(SessionId));
        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
            {
                _logger.LogInformation("Workspace changed to '{WorkspacePath}' — clearing session log detail",
                    workspaceContext.ActiveWorkspacePath);
                SessionId = string.Empty;
                Detail = null;
                ErrorMessage = null;
                StatusMessage = "Session log detail cleared for workspace change.";
            }
        };
    }

    /// <summary>Session identifier to load.</summary>
    [ObservableProperty]
    private string _sessionId = string.Empty;

    /// <summary>Primary async command used by UI and <c>director exec</c>.</summary>
    public IAsyncRelayCommand LoadCommand => _loadCommand;

    /// <summary>Alias for ViewModel registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => LoadCommand;

    /// <summary>Last CQRS dispatch result.</summary>
    public Result<SessionLogDetail?>? LastResult => _loadCommand.LastResult;

    /// <summary>Loads the session log detail for <see cref="SessionId"/>.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Loading session log detail...";

        try
        {
            if (string.IsNullOrWhiteSpace(SessionId))
            {
                Detail = null;
                ErrorMessage = "SessionId is required.";
                StatusMessage = "Session log detail load failed.";
                return;
            }

            var result = await _loadCommand.DispatchAsync(ct).ConfigureAwait(true);
            if (!result.IsSuccess)
            {
                Detail = null;
                ErrorMessage = result.Error ?? "Unknown error loading session log detail.";
                StatusMessage = "Session log detail load failed.";
                return;
            }

            Detail = result.Value;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value is null
                ? "Session log not found."
                : $"Loaded session log '{result.Value.SessionId}'.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            Detail = null;
            ErrorMessage = ex.Message;
            StatusMessage = "Session log detail load failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
