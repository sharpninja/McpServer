using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-052..058: Runtime service for pooled agent lifecycle, one-shot queueing, and stream notifications.
/// </summary>
public interface IAgentPoolService
{
    /// <summary>
    /// Lists runtime state for all configured pooled agents.
    /// </summary>
    Task<IReadOnlyList<AgentPoolAgentStatusDto>> GetAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all queue items across active and terminal states.
    /// </summary>
    Task<IReadOnlyList<AgentPoolQueueItemDto>> GetQueueItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to a pooled interactive agent session.
    /// </summary>
    Task<AgentPoolConnectResult> ConnectInteractiveAsync(string? agentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a pooled agent session.
    /// </summary>
    Task<AgentPoolMutationResult> StartAgentAsync(string agentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a pooled agent session.
    /// </summary>
    Task<AgentPoolMutationResult> StopAgentAsync(string agentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recycles a pooled agent session immediately.
    /// </summary>
    Task<AgentPoolMutationResult> RecycleAgentAsync(string agentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues a one-shot request for pooled execution.
    /// </summary>
    Task<AgentPoolEnqueueResult> EnqueueOneShotAsync(AgentPoolOneShotRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a queued or processing one-shot request.
    /// </summary>
    Task<AgentPoolMutationResult> CancelQueueItemAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a queued item from the queue.
    /// </summary>
    Task<AgentPoolMutationResult> RemoveQueueItemAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a queued item up by one position.
    /// </summary>
    Task<AgentPoolMutationResult> MoveQueueItemUpAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a queued item down by one position.
    /// </summary>
    Task<AgentPoolMutationResult> MoveQueueItemDownAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves and validates one-shot prompt text.
    /// </summary>
    Task<AgentPoolPromptResolutionResult> ResolvePromptAsync(AgentPoolOneShotRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to global agent-pool lifecycle notifications.
    /// </summary>
    IAsyncEnumerable<AgentPoolNotificationEventDto> SubscribeNotificationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to read-only events for a single queue item.
    /// </summary>
    IAsyncEnumerable<AgentPoolJobStreamEventDto> SubscribeJobStreamAsync(string jobId, CancellationToken cancellationToken = default);
}
