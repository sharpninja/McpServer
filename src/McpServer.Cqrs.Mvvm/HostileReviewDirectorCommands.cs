using CommunityToolkit.Mvvm.Input;

namespace McpServer.Cqrs.Mvvm;

/// <summary>FR-MCP-HOSTILEREVIEW-006: Director command names. Submit/status/get/query only.</summary>
public static class HostileReviewDirectorCommands
{
    /// <summary>Submit a review request.</summary>
    public const string Submit = "hostile-review-submit";

    /// <summary>Read-only status.</summary>
    public const string Status = "hostile-review-status";

    /// <summary>Read-only get.</summary>
    public const string Get = "hostile-review-get";

    /// <summary>AND-filtered query.</summary>
    public const string Query = "hostile-review-query";
}

/// <summary>FR-MCP-HOSTILEREVIEW-006: Director executor contract.</summary>
public interface IHostileReviewDirectorExecutor
{
    /// <summary>Submit.</summary>
    Task<object?> SubmitAsync(string? targetType, string? mode, string? scopeStatement, string? requestingAgent, string? workspacePath, CancellationToken cancellationToken);

    /// <summary>Status.</summary>
    Task<object?> StatusAsync(string? requestId, CancellationToken cancellationToken);

    /// <summary>Get.</summary>
    Task<object?> GetAsync(string? requestId, CancellationToken cancellationToken);

    /// <summary>Query.</summary>
    Task<object?> QueryAsync(string? model, string? effort, string? requestingAgent, string? targetType, CancellationToken cancellationToken);
}

/// <summary>FR-MCP-HOSTILEREVIEW-001: Director submit command.</summary>
[ViewModelCommand("hostile-review-submit", Description = "Submit a bounded hostile review request.")]
public sealed class HostileReviewSubmitDirectorCommand
{
    private readonly IHostileReviewDirectorExecutor _executor;

    /// <summary>Constructor.</summary>
    public HostileReviewSubmitDirectorCommand(IHostileReviewDirectorExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        PrimaryCommand = new AsyncRelayCommand(ExecuteAsync);
    }

    /// <summary>Primary command.</summary>
    public IAsyncRelayCommand PrimaryCommand { get; }

    /// <summary>Result.</summary>
    public object? Result { get; private set; }

    /// <summary>Target type.</summary>
    public string? TargetType { get; set; }

    /// <summary>Mode.</summary>
    public string? Mode { get; set; }

    /// <summary>Scope.</summary>
    public string? ScopeStatement { get; set; }

    /// <summary>Requester.</summary>
    public string? RequestingAgent { get; set; }

    /// <summary>Workspace path.</summary>
    public string? WorkspacePath { get; set; }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
        => Result = await _executor.SubmitAsync(TargetType, Mode, ScopeStatement, RequestingAgent, WorkspacePath, cancellationToken).ConfigureAwait(false);
}

/// <summary>FR-MCP-HOSTILEREVIEW-006: Director status command.</summary>
[ViewModelCommand("hostile-review-status", Description = "Read-only hostile review status.")]
public sealed class HostileReviewStatusDirectorCommand
{
    private readonly IHostileReviewDirectorExecutor _executor;

    /// <summary>Constructor.</summary>
    public HostileReviewStatusDirectorCommand(IHostileReviewDirectorExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        PrimaryCommand = new AsyncRelayCommand(ExecuteAsync);
    }

    /// <summary>Primary command.</summary>
    public IAsyncRelayCommand PrimaryCommand { get; }

    /// <summary>Result.</summary>
    public object? Result { get; private set; }

    /// <summary>Request id.</summary>
    public string? RequestId { get; set; }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
        => Result = await _executor.StatusAsync(RequestId, cancellationToken).ConfigureAwait(false);
}

/// <summary>FR-MCP-HOSTILEREVIEW-004: Director get command.</summary>
[ViewModelCommand("hostile-review-get", Description = "Read-only hostile review get.")]
public sealed class HostileReviewGetDirectorCommand
{
    private readonly IHostileReviewDirectorExecutor _executor;

    /// <summary>Constructor.</summary>
    public HostileReviewGetDirectorCommand(IHostileReviewDirectorExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        PrimaryCommand = new AsyncRelayCommand(ExecuteAsync);
    }

    /// <summary>Primary command.</summary>
    public IAsyncRelayCommand PrimaryCommand { get; }

    /// <summary>Result.</summary>
    public object? Result { get; private set; }

    /// <summary>Request id.</summary>
    public string? RequestId { get; set; }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
        => Result = await _executor.GetAsync(RequestId, cancellationToken).ConfigureAwait(false);
}

/// <summary>FR-MCP-HOSTILEREVIEW-005: Director query command.</summary>
[ViewModelCommand("hostile-review-query", Description = "Query hostile review runs.")]
public sealed class HostileReviewQueryDirectorCommand
{
    private readonly IHostileReviewDirectorExecutor _executor;

    /// <summary>Constructor.</summary>
    public HostileReviewQueryDirectorCommand(IHostileReviewDirectorExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        PrimaryCommand = new AsyncRelayCommand(ExecuteAsync);
    }

    /// <summary>Primary command.</summary>
    public IAsyncRelayCommand PrimaryCommand { get; }

    /// <summary>Result.</summary>
    public object? Result { get; private set; }

    /// <summary>Model filter.</summary>
    public string? Model { get; set; }

    /// <summary>Effort filter.</summary>
    public string? Effort { get; set; }

    /// <summary>Requester filter.</summary>
    public string? RequestingAgent { get; set; }

    /// <summary>Target type filter.</summary>
    public string? TargetType { get; set; }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
        => Result = await _executor.QueryAsync(Model, Effort, RequestingAgent, TargetType, cancellationToken).ConfigureAwait(false);
}

