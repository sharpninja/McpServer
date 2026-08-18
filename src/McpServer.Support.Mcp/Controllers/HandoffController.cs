using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>FR-HANDOFF-007: REST API for handoff ingest, inspect, and approval.</summary>
[ApiController]
[Route("mcpserver/handoff")]
public sealed class HandoffController : ControllerBase
{
    private readonly IHandoffIngestionService _handoffIngestionService;

    /// <summary>TR-HANDOFF-SURFACE-001: Constructor.</summary>
    public HandoffController(IHandoffIngestionService handoffIngestionService)
    {
        _handoffIngestionService = handoffIngestionService ?? throw new ArgumentNullException(nameof(handoffIngestionService));
    }

    /// <summary>FR-HANDOFF-001: Ingest a handoff document and return the shared result contract.</summary>
    [HttpPost("ingest")]
    public async Task<ActionResult<HandoffIngestionResult>> IngestAsync(
        [FromBody] HandoffIngestionRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new HandoffIngestionResult { Success = false, Error = "Request body is required." });

        var result = await _handoffIngestionService.IngestAsync(request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    /// <summary>FR-HANDOFF-006: Inspect a persisted handoff run.</summary>
    [HttpGet("runs/{runId}")]
    public async Task<ActionResult<HandoffIngestionResult>> GetRunAsync(string runId, CancellationToken cancellationToken)
    {
        var result = await _handoffIngestionService.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    /// <summary>FR-HANDOFF-004: Approve or reject a stored handoff run after revalidation.</summary>
    [HttpPost("runs/{runId}/approve")]
    public async Task<ActionResult<HandoffIngestionResult>> ApproveAsync(
        string runId,
        [FromBody] HandoffApprovalRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new HandoffIngestionResult { Success = false, Error = "Request body is required." });

        var result = await _handoffIngestionService.ApproveAsync(runId, request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    private ActionResult<HandoffIngestionResult> ToActionResult(HandoffIngestionResult result)
    {
        if (result.Success)
            return Ok(result);

        var status = HandoffHttpStatus.FromErrorCode(result.ErrorCode);
        if (status >= 500)
        {
            result.Error = "Handoff processing failed.";
        }

        return status switch
        {
            404 => NotFound(result),
            409 => Conflict(result),
            400 => BadRequest(result),
            _ => StatusCode(status, result),
        };
    }
}
