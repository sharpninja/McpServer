using McpServer.Support.Mcp.Models;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Fails closed for agent-pool runtime mutations while required
/// turn transactions are active because queue and external-session side effects are not compensated.
/// </summary>
public sealed class TransactionGatedAgentPoolService : IAgentPoolService
{
    private const string DeferredAgentPoolMutationMessage =
        "Agent-pool runtime mutations are not transaction compensated while required turn transactions are active.";

    private readonly IAgentPoolService _inner;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedAgentPoolService"/> class.</summary>
    /// <param name="inner">Underlying agent-pool service.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction enforcement options.</param>
    public TransactionGatedAgentPoolService(
        IAgentPoolService inner,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentPoolAgentStatusDto>> GetAgentsAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
        => _inner.GetAgentsAsync(workspacePath, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentPoolQueueItemDto>> GetQueueItemsAsync(CancellationToken cancellationToken = default)
        => _inner.GetQueueItemsAsync(cancellationToken);

    /// <inheritdoc />
    public Task<AgentPoolConnectResult> ConnectInteractiveAsync(
        string? agentName,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new AgentPoolConnectResult { Success = false, Error = error });

        return _inner.ConnectInteractiveAsync(agentName, workspacePath, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentPoolMutationResult> StartAgentAsync(
        string agentName,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(FailedMutation(error));

        return _inner.StartAgentAsync(agentName, workspacePath, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentPoolMutationResult> StopAgentAsync(
        string agentName,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(FailedMutation(error));

        return _inner.StopAgentAsync(agentName, workspacePath, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentPoolMutationResult> RecycleAgentAsync(
        string agentName,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(FailedMutation(error));

        return _inner.RecycleAgentAsync(agentName, workspacePath, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentPoolEnqueueResult> EnqueueOneShotAsync(
        AgentPoolOneShotRequest request,
        CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new AgentPoolEnqueueResult { Success = false, Error = error });

        return _inner.EnqueueOneShotAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentPoolMutationResult> CancelQueueItemAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(FailedMutation(error));

        return _inner.CancelQueueItemAsync(jobId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentPoolMutationResult> RemoveQueueItemAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(FailedMutation(error));

        return _inner.RemoveQueueItemAsync(jobId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentPoolMutationResult> MoveQueueItemUpAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(FailedMutation(error));

        return _inner.MoveQueueItemUpAsync(jobId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentPoolMutationResult> MoveQueueItemDownAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(FailedMutation(error));

        return _inner.MoveQueueItemDownAsync(jobId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentPoolPromptResolutionResult> ResolvePromptAsync(
        AgentPoolOneShotRequest request,
        CancellationToken cancellationToken = default)
        => _inner.ResolvePromptAsync(request, cancellationToken);

    /// <inheritdoc />
    public IAsyncEnumerable<AgentPoolNotificationEventDto> SubscribeNotificationsAsync(CancellationToken cancellationToken = default)
        => _inner.SubscribeNotificationsAsync(cancellationToken);

    /// <inheritdoc />
    public IAsyncEnumerable<AgentPoolJobStreamEventDto> SubscribeJobStreamAsync(
        string jobId,
        CancellationToken cancellationToken = default)
        => _inner.SubscribeJobStreamAsync(jobId, cancellationToken);

    /// <inheritdoc />
    public Task SeedWorkspaceAgentsAsync(string workspacePath, CancellationToken cancellationToken = default)
        => _inner.SeedWorkspaceAgentsAsync(workspacePath, cancellationToken);

    private bool ShouldDeferMutation(out string error)
    {
        error = string.Empty;
        if (_coordinator is null)
            return false;

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            error = string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message;
            return true;
        }

        if (!RequiresMutationTransactions(status))
            return false;

        error = DeferredAgentPoolMutationMessage;
        return true;
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);

    private static AgentPoolMutationResult FailedMutation(string error)
        => new() { Success = false, Error = error };
}
