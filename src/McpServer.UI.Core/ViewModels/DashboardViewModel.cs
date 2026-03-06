using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for the web dashboard aggregate counts and health status.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<DashboardViewModel> _logger;

    /// <summary>Initializes a new dashboard ViewModel instance.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public DashboardViewModel(Dispatcher dispatcher, ILogger<DashboardViewModel> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _workspaceCount;

    [ObservableProperty]
    private int _todoCount;

    [ObservableProperty]
    private int _sessionLogCount;

    [ObservableProperty]
    private int _templateCount;

    [ObservableProperty]
    private string _healthStatus = "unknown";

    /// <summary>Loads dashboard counters and health status.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var workspaceTask = _dispatcher.QueryAsync(new ListWorkspacesQuery(), ct);
            var todoTask = _dispatcher.QueryAsync(new ListTodosQuery(), ct);
            var sessionTask = _dispatcher.QueryAsync(new ListSessionLogsQuery { Limit = 20, Offset = 0 }, ct);
            var templateTask = _dispatcher.QueryAsync(new ListTemplatesQuery(), ct);
            var healthTask = _dispatcher.QueryAsync(new CheckHealthQuery(), ct);

            await Task.WhenAll(workspaceTask, todoTask, sessionTask, templateTask, healthTask).ConfigureAwait(true);

            WorkspaceCount = workspaceTask.Result.IsSuccess && workspaceTask.Result.Value is not null
                ? workspaceTask.Result.Value.TotalCount
                : 0;
            TodoCount = todoTask.Result.IsSuccess && todoTask.Result.Value is not null
                ? todoTask.Result.Value.TotalCount
                : 0;
            SessionLogCount = sessionTask.Result.IsSuccess && sessionTask.Result.Value is not null
                ? sessionTask.Result.Value.TotalCount
                : 0;
            TemplateCount = templateTask.Result.IsSuccess && templateTask.Result.Value is not null
                ? templateTask.Result.Value.TotalCount
                : 0;

            var health = healthTask.Result.IsSuccess ? healthTask.Result.Value : null;
            HealthStatus = string.IsNullOrWhiteSpace(health?.Status) ? "unknown" : health.Status;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            WorkspaceCount = 0;
            TodoCount = 0;
            SessionLogCount = 0;
            TemplateCount = 0;
            HealthStatus = "unknown";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
