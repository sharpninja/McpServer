using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// TR-PLANNED-013: Session log submit and query endpoints (MVP-SUPPORT-011).
/// FR-SUPPORT-010: Agents POST session log payloads; clients GET with optional filters.
/// </summary>
[ApiController]
[Route("mcpserver/sessionlog")]
public sealed class SessionLogController : ControllerBase
{
    private const int MaxEntryCount = 5000;

    private readonly ISessionLogService _service;
    private readonly ILogger<SessionLogController> _logger;


    /// <summary>TR-PLANNED-013: Constructor.</summary>
    public SessionLogController(ISessionLogService service,
        ILogger<SessionLogController> logger)
    {
        _logger = logger;
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// TR-PLANNED-013: Submit a session log (upsert by SourceType + SessionId).
    /// </summary>
    /// <param name="dto">Unified session log payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with location header, or 400 if validation fails.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitAsync([FromBody] UnifiedSessionLogDto dto, CancellationToken cancellationToken)
    {
        if (dto is null)
            return BadRequest(new { error = "Request body is required." });

        if (string.IsNullOrWhiteSpace(dto.SourceType))
            return BadRequest(new { error = "SourceType is required." });

        if (string.IsNullOrWhiteSpace(dto.SessionId))
            return BadRequest(new { error = "SessionId is required." });

        if (dto.Entries is { Count: > MaxEntryCount })
            return BadRequest(new { error = $"Entry count exceeds maximum of {MaxEntryCount}." });

        var id = await _service.SubmitAsync(dto, sourceFilePath: null, contentHash: null, cancellationToken).ConfigureAwait(false);

        return Created(
            new Uri($"/mcpserver/sessionlog?agent={Uri.EscapeDataString(dto.SourceType)}&sessionId={Uri.EscapeDataString(dto.SessionId)}", UriKind.Relative),
            new { id, sourceType = dto.SourceType, sessionId = dto.SessionId });
    }

    /// <summary>
    /// TR-PLANNED-013: Query session logs with optional filters and pagination.
    /// </summary>
    /// <param name="agent">Filter by agent source type.</param>
    /// <param name="model">Filter by AI model.</param>
    /// <param name="text">Full-text search over entry text fields.</param>
    /// <param name="from">Sessions started on or after this date (ISO 8601).</param>
    /// <param name="to">Sessions last updated on or before this date (ISO 8601).</param>
    /// <param name="limit">Page size (default 100, max 1000).</param>
    /// <param name="offset">Number of sessions to skip (default 0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with paginated session logs.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SessionLogQueryResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionLogQueryResult>> QueryAsync(
        [FromQuery] string? agent,
        [FromQuery] string? model,
        [FromQuery] string? text,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var request = new SessionLogQueryRequest
        {
            Agent = agent,
            Model = model,
            Text = text,
            From = from,
            To = to,
            Limit = limit,
            Offset = offset
        };

        var result = await _service.QueryAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// TR-PLANNED-013: Append processing dialog items to an existing entry.
    /// The AI model calls this endpoint on the fly to record reasoning, tool calls, and execution trace.
    /// </summary>
    /// <param name="agent">Agent source type (e.g. Cursor, Copilot).</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Request entry identifier within the session.</param>
    /// <param name="items">Dialog items to append.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the new total dialog count, or 404 if entry not found.</returns>
    [HttpPost("{agent}/{sessionId}/{requestId}/dialog")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AppendDialogAsync(
        string agent,
        string sessionId,
        string requestId,
        [FromBody] IReadOnlyList<ProcessingDialogItemDto> items,
        CancellationToken cancellationToken)
    {
        if (items is null or { Count: 0 })
            return BadRequest(new { error = "At least one dialog item is required." });

        try
        {
            var totalCount = await _service.AppendProcessingDialogAsync(
                agent, sessionId, requestId, items, cancellationToken).ConfigureAwait(false);
            return Ok(new { agent, sessionId, requestId, totalDialogCount = totalCount });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return NotFound(new { error = ex.Message });
        }
    }
}
