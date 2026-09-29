using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-REQRECOVERY-001: REST dry-run, apply, and get-by-idempotency-key for atomic requirements recovery.
/// </summary>
[ApiController]
[Route("mcpserver/requirements/recovery")]
public sealed class RequirementsRecoveryController : ControllerBase
{
    private readonly IRequirementsRecoveryService _recovery;

    /// <summary>Initializes the controller.</summary>
    /// <param name="recovery">Recovery service for the active workspace.</param>
    public RequirementsRecoveryController(IRequirementsRecoveryService recovery)
    {
        _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
    }

    /// <summary>Plans (<c>dry-run</c>) or applies a recovery payload.</summary>
    /// <param name="request">Mode, idempotency key, and requirement items.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>200 with the plan or applied result, or 400/409/503.</returns>
    [HttpPost]
    public async Task<ActionResult<RequirementsRecoveryResult>> PostAsync(
        [FromBody] RequirementsRecoveryRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(ClassifiedPayload(McpErrorClassifier.Classify(new ArgumentException("Request body is required."))));

        var mode = request.Mode?.Trim().ToLowerInvariant();
        try
        {
            var result = mode switch
            {
                "dry-run" => await _recovery.PlanAsync(request, cancellationToken).ConfigureAwait(false),
                "apply" => await _recovery.ApplyAsync(request, cancellationToken).ConfigureAwait(false),
                _ => null,
            };
            if (result is null)
            {
                return BadRequest(ClassifiedPayload(McpErrorClassifier.Classify(new ArgumentException("mode must be dry-run or apply."))));
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return Map(ex);
        }
    }

    /// <summary>
    /// Alias for mode=<c>dry-run</c>. Keeps the mode-body POST for backward compatibility with the REPL client.
    /// </summary>
    [HttpPost("dry-run")]
    public Task<ActionResult<RequirementsRecoveryResult>> DryRunAsync(
        [FromBody] RequirementsRecoveryRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is not null)
            request.Mode = "dry-run";
        return PostAsync(request, cancellationToken);
    }

    /// <summary>
    /// Alias for mode=<c>apply</c>. Keeps the mode-body POST for backward compatibility with the REPL client.
    /// </summary>
    [HttpPost("apply")]
    public Task<ActionResult<RequirementsRecoveryResult>> ApplyRouteAsync(
        [FromBody] RequirementsRecoveryRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is not null)
            request.Mode = "apply";
        return PostAsync(request, cancellationToken);
    }

    /// <summary>Gets a stored recovery run. Dry-run does not store a run, so that key is 404.</summary>
    /// <param name="idempotencyKey">Idempotency key.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>200 with the stored result, or 400/404/503.</returns>
    [HttpGet("{idempotencyKey}")]
    public async Task<ActionResult<RequirementsRecoveryResult>> GetAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _recovery.GetAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Map(ex);
        }
    }

    /// <summary>
    /// FR-MCP-TRIAGEERR-001: maps recovery failures to the shared machine-readable envelope
    /// (<c>code</c>/<c>message</c>/<c>retryable</c>/<c>details</c> plus ProblemDetails extensions).
    /// </summary>
    private ActionResult Map(Exception exception)
    {
        var classified = McpErrorClassifier.Classify(exception);
        return StatusCode(classified.StatusCode, ClassifiedPayload(classified));
    }

    private static object ClassifiedPayload(McpErrorClassification classified)
        => new
        {
            type = "https://httpstatuses.io/" + classified.StatusCode,
            title = classified.Code,
            status = classified.StatusCode,
            detail = classified.Message,
            code = classified.Code,
            error = classified.Code,
            message = classified.Message,
            retryable = classified.Retryable,
            details = classified.Details,
        };
}
