using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-122 through FR-MCP-124: Subscriber endpoints for encrypted diffgram
/// commit, transaction status, and abort operations.
/// </summary>
[ApiController]
[Route("mcpserver/subscriber")]
public sealed class SubscriberController : ControllerBase
{
    private readonly ISubscriberCommitService _commitService;

    /// <summary>Initializes a new instance of the <see cref="SubscriberController"/> class.</summary>
    /// <param name="commitService">Subscriber commit service.</param>
    public SubscriberController(ISubscriberCommitService commitService)
    {
        _commitService = commitService;
    }

    /// <summary>Commits a signed and encrypted diffgram.</summary>
    /// <param name="request">Commit payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Commit response.</returns>
    [HttpPost("diffgrams/commit")]
    public async Task<ActionResult<DiffgramCommitResponse>> CommitDiffgramAsync(
        [FromBody] DiffgramCommitRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _commitService.CommitDiffgramAsync(request, cancellationToken).ConfigureAwait(false);
        return string.Equals(response.Status, "rejected", StringComparison.OrdinalIgnoreCase)
            ? BadRequest(response)
            : Ok(response);
    }

    /// <summary>Gets subscriber transaction status.</summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction status response.</returns>
    [HttpGet("transactions/{transactionId}/status")]
    public async Task<ActionResult<TransactionStatusResponse>> GetTransactionStatusAsync(
        string transactionId,
        CancellationToken cancellationToken)
    {
        var response = await _commitService.GetTransactionStatusAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (response is null)
            return NotFound(new { error = $"Transaction '{transactionId}' was not found." });

        return Ok(response);
    }

    /// <summary>Aborts a transaction before commit.</summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="request">Abort payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Abort response.</returns>
    [HttpPost("transactions/{transactionId}/abort")]
    public async Task<ActionResult<TransactionAbortResponse>> AbortTransactionAsync(
        string transactionId,
        [FromBody] TransactionAbortRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _commitService.AbortTransactionAsync(transactionId, request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
