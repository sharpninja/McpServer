using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Abstraction over pooled-runtime endpoints used by UI.Core CQRS handlers.
/// </summary>
public interface IAgentPoolApiClient
{
    /// <summary>Lists pooled runtime agents.</summary>
    Task<ListAgentPoolAgentsResult> ListAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists pooled queue items.</summary>
    Task<ListAgentPoolQueueResult> ListQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>Starts a pooled agent session.</summary>
    Task<AgentPoolMutationOutcome> StartAgentAsync(string agentName, CancellationToken cancellationToken = default);

    /// <summary>Stops a pooled agent session.</summary>
    Task<AgentPoolMutationOutcome> StopAgentAsync(string agentName, CancellationToken cancellationToken = default);

    /// <summary>Recycles a pooled agent session.</summary>
    Task<AgentPoolMutationOutcome> RecycleAgentAsync(string agentName, CancellationToken cancellationToken = default);

    /// <summary>Connects to a pooled interactive session for an agent.</summary>
    Task<AgentPoolConnectOutcome> ConnectAsync(string agentName, CancellationToken cancellationToken = default);

    /// <summary>Cancels a pooled queue item.</summary>
    Task<AgentPoolMutationOutcome> CancelQueueItemAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>Removes a pooled queue item.</summary>
    Task<AgentPoolMutationOutcome> RemoveQueueItemAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>Moves a pooled queue item up.</summary>
    Task<AgentPoolMutationOutcome> MoveQueueItemUpAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>Moves a pooled queue item down.</summary>
    Task<AgentPoolMutationOutcome> MoveQueueItemDownAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>Resolves one-shot prompt text before queueing.</summary>
    Task<AgentPoolPromptResolutionOutcome> ResolvePromptAsync(AgentPoolEnqueueDraft request, CancellationToken cancellationToken = default);

    /// <summary>Queues a one-shot pooled request.</summary>
    Task<AgentPoolEnqueueOutcome> EnqueueOneShotAsync(AgentPoolEnqueueDraft request, CancellationToken cancellationToken = default);
}
