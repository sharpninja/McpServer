using McpServer.Client;
using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="IAgentPoolApiClient"/> backed by <see cref="McpServerClient"/>.
/// </summary>
internal sealed class AgentPoolApiClientAdapter : IAgentPoolApiClient
{
    private readonly DirectorMcpContext _context;

    /// <summary>Initializes a new instance of the <see cref="AgentPoolApiClientAdapter"/> class.</summary>
    public AgentPoolApiClientAdapter(DirectorMcpContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ListAgentPoolAgentsResult> ListAgentsAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client.AgentPool.GetAgentsAsync(cancellationToken).ConfigureAwait(false);
        var items = response
            .Select(i => new AgentPoolRuntimeAgentSnapshot(
                i.AgentName,
                i.Lifecycle,
                i.SessionId,
                i.ActiveJobId,
                i.ActiveVoiceLinks,
                i.WorkspacePath))
            .ToList();

        return new ListAgentPoolAgentsResult(items, items.Count);
    }

    /// <inheritdoc />
    public async Task<ListAgentPoolQueueResult> ListQueueAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client.AgentPool.GetQueueAsync(cancellationToken).ConfigureAwait(false);
        var items = response
            .Select(i => new AgentPoolQueueItemSnapshot(
                i.JobId,
                i.AgentName,
                i.Status,
                i.Context?.ToString(),
                i.RenderedPrompt,
                i.WorkspacePath))
            .ToList();

        return new ListAgentPoolQueueResult(items, items.Count);
    }

    /// <inheritdoc />
    public async Task<AgentPoolMutationOutcome> StartAgentAsync(string agentName, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.AgentPool.StartAgentAsync(agentName, cancellationToken).ConfigureAwait(false);
        return MapMutation(result);
    }

    /// <inheritdoc />
    public async Task<AgentPoolMutationOutcome> StopAgentAsync(string agentName, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.AgentPool.StopAgentAsync(agentName, cancellationToken).ConfigureAwait(false);
        return MapMutation(result);
    }

    /// <inheritdoc />
    public async Task<AgentPoolMutationOutcome> RecycleAgentAsync(string agentName, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.AgentPool.RecycleAgentAsync(agentName, cancellationToken).ConfigureAwait(false);
        return MapMutation(result);
    }

    /// <inheritdoc />
    public async Task<AgentPoolConnectOutcome> ConnectAsync(string agentName, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.AgentPool.ConnectAsync(agentName, cancellationToken).ConfigureAwait(false);
        return new AgentPoolConnectOutcome(
            result.Success,
            result.Error,
            result.SessionId,
            null,
            null,
            result.AgentName);
    }

    /// <inheritdoc />
    public async Task<AgentPoolMutationOutcome> CancelQueueItemAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.AgentPool.CancelQueueItemAsync(jobId, cancellationToken).ConfigureAwait(false);
        return MapMutation(result);
    }

    /// <inheritdoc />
    public async Task<AgentPoolMutationOutcome> RemoveQueueItemAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.AgentPool.RemoveQueueItemAsync(jobId, cancellationToken).ConfigureAwait(false);
        return MapMutation(result);
    }

    /// <inheritdoc />
    public async Task<AgentPoolMutationOutcome> MoveQueueItemUpAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.AgentPool.MoveQueueItemUpAsync(jobId, cancellationToken).ConfigureAwait(false);
        return MapMutation(result);
    }

    /// <inheritdoc />
    public async Task<AgentPoolMutationOutcome> MoveQueueItemDownAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.AgentPool.MoveQueueItemDownAsync(jobId, cancellationToken).ConfigureAwait(false);
        return MapMutation(result);
    }

    /// <inheritdoc />
    public async Task<AgentPoolPromptResolutionOutcome> ResolvePromptAsync(
        AgentPoolEnqueueDraft request,
        CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.AgentPool.ResolvePromptAsync(MapRequest(request), cancellationToken).ConfigureAwait(false);
        return new AgentPoolPromptResolutionOutcome(
            result.Success,
            result.Error,
            result.PromptText,
            result.TemplateResolved,
            false,
            false);
    }

    /// <inheritdoc />
    public async Task<AgentPoolEnqueueOutcome> EnqueueOneShotAsync(
        AgentPoolEnqueueDraft request,
        CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.AgentPool.EnqueueOneShotAsync(MapRequest(request), cancellationToken).ConfigureAwait(false);
        return new AgentPoolEnqueueOutcome(result.Success, result.Error, result.JobId, result.AgentName);
    }

    private static AgentPoolMutationOutcome MapMutation(AgentPoolMutationResult result)
        => new(result.Success, result.Error);

    private static AgentPoolOneShotRequest MapRequest(AgentPoolEnqueueDraft request)
        => new()
        {
            AgentName = request.AgentName,
            Context = request.Context switch
            {
                AgentPoolPromptContext.Plan => AgentPoolOneShotContext.Plan,
                AgentPoolPromptContext.Status => AgentPoolOneShotContext.Status,
                AgentPoolPromptContext.Implement => AgentPoolOneShotContext.Implement,
                _ => AgentPoolOneShotContext.AdHoc
            },
            PromptText = request.PromptText,
            UseWorkspaceContext = request.UseWorkspaceContext
        };
}
