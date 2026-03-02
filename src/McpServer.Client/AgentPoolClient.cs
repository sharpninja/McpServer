using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for pooled-runtime endpoints (<c>/mcpserver/agent-pool</c>) including agent lifecycle,
/// one-shot queue operations, prompt resolution, and SSE streams.
/// </summary>
/// <seealso cref="McpServerClient.AgentPool"/>
public sealed class AgentPoolClient : McpClientBase
{
    private static readonly JsonSerializerOptions s_streamJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public AgentPoolClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal AgentPoolClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Gets runtime status for all configured pooled agents.</summary>
    public async Task<IReadOnlyList<AgentPoolAgentStatus>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IReadOnlyList<AgentPoolAgentStatus>>("mcpserver/agent-pool/agents", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Starts a pooled agent session.</summary>
    public async Task<AgentPoolMutationResult> StartAgentAsync(string agentName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolMutationResult>($"mcpserver/agent-pool/agents/{Encode(agentName)}/start", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Stops a pooled agent session.</summary>
    public async Task<AgentPoolMutationResult> StopAgentAsync(string agentName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolMutationResult>($"mcpserver/agent-pool/agents/{Encode(agentName)}/stop", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Recycles a pooled agent session.</summary>
    public async Task<AgentPoolMutationResult> RecycleAgentAsync(string agentName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolMutationResult>($"mcpserver/agent-pool/agents/{Encode(agentName)}/recycle", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Connects to a specific pooled interactive session.</summary>
    public async Task<AgentPoolConnectResult> ConnectAsync(string agentName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolConnectResult>($"mcpserver/agent-pool/agents/{Encode(agentName)}/connect", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Connects to the default pooled interactive session.</summary>
    public async Task<AgentPoolConnectResult> ConnectDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolConnectResult>("mcpserver/agent-pool/connect", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists queue items.</summary>
    public async Task<IReadOnlyList<AgentPoolQueueItem>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IReadOnlyList<AgentPoolQueueItem>>("mcpserver/agent-pool/queue", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Enqueues a one-shot request.</summary>
    public async Task<AgentPoolEnqueueResult> EnqueueOneShotAsync(AgentPoolOneShotRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolEnqueueResult>("mcpserver/agent-pool/queue/one-shot", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cancels a queued or processing one-shot request.</summary>
    public async Task<AgentPoolMutationResult> CancelQueueItemAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolMutationResult>($"mcpserver/agent-pool/queue/{Encode(jobId)}/cancel", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes a queue item.</summary>
    public async Task<AgentPoolMutationResult> RemoveQueueItemAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<AgentPoolMutationResult>($"mcpserver/agent-pool/queue/{Encode(jobId)}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Moves a queued one-shot item up by one position.</summary>
    public async Task<AgentPoolMutationResult> MoveQueueItemUpAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolMutationResult>($"mcpserver/agent-pool/queue/{Encode(jobId)}/move-up", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Moves a queued one-shot item down by one position.</summary>
    public async Task<AgentPoolMutationResult> MoveQueueItemDownAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolMutationResult>($"mcpserver/agent-pool/queue/{Encode(jobId)}/move-down", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Resolves one-shot prompt text without enqueuing.</summary>
    public async Task<AgentPoolPromptResolutionResult> ResolvePromptAsync(AgentPoolOneShotRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolPromptResolutionResult>("mcpserver/agent-pool/queue/resolve", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Streams global pooled-runtime notifications via SSE.</summary>
    public IAsyncEnumerable<AgentPoolNotificationEvent> StreamNotificationsAsync(CancellationToken cancellationToken = default)
        => StreamJsonSseAsync<AgentPoolNotificationEvent>("mcpserver/agent-pool/notifications", cancellationToken);

    /// <summary>Streams read-only events for a single queue job via SSE.</summary>
    public IAsyncEnumerable<AgentPoolJobStreamEvent> StreamJobAsync(string jobId, CancellationToken cancellationToken = default)
        => StreamJsonSseAsync<AgentPoolJobStreamEvent>($"mcpserver/agent-pool/jobs/{Encode(jobId)}/stream", cancellationToken);

    private static string Encode(string value) => System.Uri.EscapeDataString(value);

    private async IAsyncEnumerable<T> StreamJsonSseAsync<T>(string path, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        where T : class
    {
        await foreach (var line in StreamSseAsync(path, cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var item = JsonSerializer.Deserialize<T>(line, s_streamJsonOptions);
            if (item is not null)
                yield return item;
        }
    }
}
