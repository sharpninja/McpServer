using McpServer.Cqrs.Mvvm;
using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-HOSTILEREVIEW-006: Director executor that delegates to IHostileReviewService.</summary>
public sealed class HostileReviewDirectorExecutor : IHostileReviewDirectorExecutor
{
    private readonly IHostileReviewService _service;

    /// <summary>TR-MCP-HOSTILEREVIEW-006: Constructor.</summary>
    public HostileReviewDirectorExecutor(IHostileReviewService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <inheritdoc />
    public Task<object?> SubmitAsync(string? targetType, string? mode, string? scopeStatement, string? requestingAgent, string? workspacePath, CancellationToken cancellationToken)
        => Box(_service.SubmitAsync(new HostileReviewSubmitRequest
        {
            TargetType = targetType ?? "code",
            Mode = mode ?? "adversarial",
            ScopeStatement = scopeStatement ?? string.Empty,
            RequestingAgent = requestingAgent ?? "director",
            WorkspacePath = workspacePath,
        }, cancellationToken));

    /// <inheritdoc />
    public Task<object?> StatusAsync(string? requestId, CancellationToken cancellationToken)
        => Box(_service.StatusAsync(requestId ?? string.Empty, cancellationToken));

    /// <inheritdoc />
    public Task<object?> GetAsync(string? requestId, CancellationToken cancellationToken)
        => Box(_service.GetAsync(requestId ?? string.Empty, cancellationToken));

    /// <inheritdoc />
    public Task<object?> QueryAsync(string? model, string? effort, string? requestingAgent, string? targetType, CancellationToken cancellationToken)
        => BoxList(_service.QueryAsync(new HostileReviewQueryRequest
        {
            Model = model,
            Effort = effort,
            RequestingAgent = requestingAgent,
            TargetType = targetType,
        }, cancellationToken));

    private static async Task<object?> Box(Task<HostileReviewResult> task)
        => await task.ConfigureAwait(false);

    private static async Task<object?> BoxList(Task<IReadOnlyList<HostileReviewResult>> task)
        => await task.ConfigureAwait(false);
}
