using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-054: Runtime API for pooled agent lifecycle, one-shot queue operations, and monitoring streams.
/// </summary>
[ApiController]
[Route("mcpserver/agent-pool")]
public sealed class AgentPoolController : ControllerBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IAgentPoolService _agentPoolService;
    private readonly WorkspaceContext _workspaceContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentPoolController"/> class.
    /// </summary>
    public AgentPoolController(IAgentPoolService agentPoolService, WorkspaceContext workspaceContext)
    {
        _agentPoolService = agentPoolService ?? throw new ArgumentNullException(nameof(agentPoolService));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
    }

    /// <summary>
    /// Returns pooled runtime state for the active workspace.
    /// </summary>
    [HttpGet("agents")]
    public async Task<ActionResult<IReadOnlyList<AgentPoolAgentStatusDto>>> GetAgentsAsync([FromQuery] string? workspace, CancellationToken cancellationToken)
        => Ok(await _agentPoolService.GetAgentsAsync(_workspaceContext.WorkspacePath, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Starts a pooled agent.
    /// </summary>
    [HttpPost("agents/{agentName}/start")]
    public async Task<ActionResult<AgentPoolMutationResult>> StartAgentAsync(string agentName, CancellationToken cancellationToken)
        => ToActionResult(await _agentPoolService.StartAgentAsync(agentName, _workspaceContext.WorkspacePath, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Stops a pooled agent.
    /// </summary>
    [HttpPost("agents/{agentName}/stop")]
    public async Task<ActionResult<AgentPoolMutationResult>> StopAgentAsync(string agentName, CancellationToken cancellationToken)
        => ToActionResult(await _agentPoolService.StopAgentAsync(agentName, _workspaceContext.WorkspacePath, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Recycles a pooled agent immediately.
    /// </summary>
    [HttpPost("agents/{agentName}/recycle")]
    public async Task<ActionResult<AgentPoolMutationResult>> RecycleAgentAsync(string agentName, CancellationToken cancellationToken)
        => ToActionResult(await _agentPoolService.RecycleAgentAsync(agentName, _workspaceContext.WorkspacePath, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Connects to a pooled interactive voice session.
    /// </summary>
    [HttpPost("agents/{agentName}/connect")]
    public async Task<ActionResult<AgentPoolConnectResult>> ConnectAgentAsync(string agentName, CancellationToken cancellationToken)
    {
        var result = await _agentPoolService.ConnectInteractiveAsync(agentName, _workspaceContext.WorkspacePath, cancellationToken).ConfigureAwait(false);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Connects to default pooled interactive voice session when agent name is omitted.
    /// </summary>
    [HttpPost("connect")]
    public async Task<ActionResult<AgentPoolConnectResult>> ConnectDefaultAgentAsync(CancellationToken cancellationToken)
    {
        var result = await _agentPoolService.ConnectInteractiveAsync(null, _workspaceContext.WorkspacePath, cancellationToken).ConfigureAwait(false);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Lists one-shot queue items.
    /// </summary>
    [HttpGet("queue")]
    public async Task<ActionResult<IReadOnlyList<AgentPoolQueueItemDto>>> GetQueueAsync(CancellationToken cancellationToken)
        => Ok(await _agentPoolService.GetQueueItemsAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Enqueues a one-shot request.
    /// </summary>
    [HttpPost("queue/one-shot")]
    public async Task<ActionResult<AgentPoolEnqueueResult>> EnqueueOneShotAsync(
        [FromBody] AgentPoolOneShotRequest request,
        CancellationToken cancellationToken)
    {
        var scopedRequest = request with { WorkspacePath = _workspaceContext.WorkspacePath };
        var result = await _agentPoolService.EnqueueOneShotAsync(scopedRequest, cancellationToken).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Cancels a queued or processing one-shot request.
    /// </summary>
    [HttpPost("queue/{jobId}/cancel")]
    public async Task<ActionResult<AgentPoolMutationResult>> CancelQueueItemAsync(string jobId, CancellationToken cancellationToken)
        => ToActionResult(await _agentPoolService.CancelQueueItemAsync(jobId, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Removes a queued one-shot request.
    /// </summary>
    [HttpDelete("queue/{jobId}")]
    public async Task<ActionResult<AgentPoolMutationResult>> RemoveQueueItemAsync(string jobId, CancellationToken cancellationToken)
        => ToActionResult(await _agentPoolService.RemoveQueueItemAsync(jobId, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Moves a queued one-shot item up by one position.
    /// </summary>
    [HttpPost("queue/{jobId}/move-up")]
    public async Task<ActionResult<AgentPoolMutationResult>> MoveQueueItemUpAsync(string jobId, CancellationToken cancellationToken)
        => ToActionResult(await _agentPoolService.MoveQueueItemUpAsync(jobId, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Moves a queued one-shot item down by one position.
    /// </summary>
    [HttpPost("queue/{jobId}/move-down")]
    public async Task<ActionResult<AgentPoolMutationResult>> MoveQueueItemDownAsync(string jobId, CancellationToken cancellationToken)
        => ToActionResult(await _agentPoolService.MoveQueueItemDownAsync(jobId, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Resolves one-shot prompt text without enqueuing.
    /// </summary>
    [HttpPost("queue/resolve")]
    public async Task<ActionResult<AgentPoolPromptResolutionResult>> ResolveOneShotPromptAsync(
        [FromBody] AgentPoolOneShotRequest request,
        CancellationToken cancellationToken)
    {
        var scopedRequest = request with { WorkspacePath = _workspaceContext.WorkspacePath };
        var result = await _agentPoolService.ResolvePromptAsync(scopedRequest, cancellationToken).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Streams pooled lifecycle notifications as SSE.
    /// </summary>
    [HttpGet("notifications")]
    [Produces("text/event-stream")]
    public async Task StreamNotificationsAsync(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";

        await foreach (var evt in _agentPoolService.SubscribeNotificationsAsync(cancellationToken).ConfigureAwait(false))
        {
            var json = JsonSerializer.Serialize(evt, s_jsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Streams read-only events for a specific one-shot queue item.
    /// </summary>
    [HttpGet("jobs/{jobId}/stream")]
    [Produces("text/event-stream")]
    public async Task StreamJobAsync(string jobId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";

        await foreach (var evt in _agentPoolService.SubscribeJobStreamAsync(jobId, cancellationToken).ConfigureAwait(false))
        {
            var json = JsonSerializer.Serialize(evt, s_jsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private ActionResult<AgentPoolMutationResult> ToActionResult(AgentPoolMutationResult result)
        => result.Success ? Ok(result) : BadRequest(result);
}
