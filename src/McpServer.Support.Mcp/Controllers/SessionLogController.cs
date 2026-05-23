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
    private const int MaxTurnCount = 5000;

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
            return ValidationProblem(detail: "Request body is required.", title: "Invalid session log body.");

        if (string.IsNullOrWhiteSpace(dto.SourceType))
            return ValidationProblem(detail: "sourceType is required.", title: "Invalid session log body.");

        if (string.IsNullOrWhiteSpace(dto.SessionId))
            return ValidationProblem(detail: "sessionId is required.", title: "Invalid session log body.");

        var sessionIdError = SessionLogIdentifierValidator.ValidateSessionId(dto.SessionId, dto.SourceType);
        if (sessionIdError is not null)
            return ValidationProblem(detail: sessionIdError, title: "Invalid sessionId.");

        if (dto.Turns is { Count: > 0 })
        {
            foreach (var turn in dto.Turns)
            {
                var requestIdError = SessionLogIdentifierValidator.ValidateRequestId(turn.RequestId);
                if (requestIdError is not null)
                    return ValidationProblem(detail: requestIdError, title: "Invalid requestId.");
            }
        }

        if (dto.Turns is { Count: > MaxTurnCount })
            return ValidationProblem(detail: $"Turn count exceeds maximum of {MaxTurnCount}.", title: "Too many turns.");

        var id = await _service.SubmitAsync(dto, sourceFilePath: null, contentHash: null, cancellationToken).ConfigureAwait(false);

        return Created(
            new Uri($"/mcpserver/sessionlog?agent={Uri.EscapeDataString(dto.SourceType)}&sessionId={Uri.EscapeDataString(dto.SessionId)}", UriKind.Relative),
            new { id, sourceType = dto.SourceType, sessionId = dto.SessionId });
    }

    /// <summary>
    /// TR-PLANNED-013: Query session logs with optional filters and pagination.
    /// </summary>
    /// <param name="agent">Filter by agent source type.</param>
    /// <param name="agentDefinitionId">Filter by linked agent definition identifier.</param>
    /// <param name="model">Filter by AI model.</param>
    /// <param name="text">Full-text search over turn text fields.</param>
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
        [FromQuery] string? agentDefinitionId,
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
            AgentDefinitionId = agentDefinitionId,
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
    /// FR-SUPPORT-010C: Fetch a single session log by (agent, sessionId). Returns
    /// 404 when the session does not exist or is excluded by the current workspace
    /// tenancy filter.
    /// </summary>
    /// <param name="agent">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the session, or 404 Not Found.</returns>
    [HttpGet("{agent}/{sessionId}")]
    [ProducesResponseType(typeof(UnifiedSessionLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnifiedSessionLogDto>> GetByIdAsync(
        string agent,
        string sessionId,
        CancellationToken cancellationToken)
    {
        var sessionIdError = SessionLogIdentifierValidator.ValidateSessionId(sessionId, agent);
        if (sessionIdError is not null)
            return Problem(detail: sessionIdError, statusCode: StatusCodes.Status400BadRequest, title: "Invalid session identifier.");

        var dto = await _service.GetAsync(agent, sessionId, cancellationToken).ConfigureAwait(false);
        if (dto is null)
            return NotFound();
        return Ok(dto);
    }

    /// <summary>
    /// FR-SUPPORT-010C: Upsert a single turn on an existing session by RequestId.
    /// Use this for incremental turn updates without re-POSTing the whole session
    /// payload.
    /// </summary>
    /// <param name="agent">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="turn">Turn payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the turn id, 400 on validation failure, or 404 if the parent session does not exist.</returns>
    [HttpPost("{agent}/{sessionId}/turn")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpsertTurnAsync(
        string agent,
        string sessionId,
        [FromBody] UnifiedRequestEntryDto turn,
        CancellationToken cancellationToken)
    {
        if (turn is null)
            return Problem(detail: "Request body must be a UnifiedRequestEntryDto.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid turn body.");

        try
        {
            var turnId = await _service.UpsertTurnAsync(agent, sessionId, turn, cancellationToken).ConfigureAwait(false);
            return Created(
                new Uri($"/mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}", UriKind.Relative),
                new { turnId, agent, sessionId, requestId = turn.RequestId });
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid turn payload.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// TR-PLANNED-013: Append processing dialog items to an existing turn.
    /// The AI model calls this endpoint on the fly to record reasoning, tool calls, and execution trace.
    /// </summary>
    /// <param name="agent">Agent source type (e.g. Cursor, Copilot).</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Request turn identifier within the session.</param>
    /// <param name="items">Dialog items to append.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the new total dialog count, or 404 if turn not found.</returns>
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

        var sessionIdError = SessionLogIdentifierValidator.ValidateSessionId(sessionId, agent);
        if (sessionIdError is not null)
            return BadRequest(new { error = sessionIdError });

        var requestIdError = SessionLogIdentifierValidator.ValidateRequestId(requestId);
        if (requestIdError is not null)
            return BadRequest(new { error = requestIdError });

        try
        {
            var totalCount = await _service.AppendProcessingDialogAsync(
                agent, sessionId, requestId, items, cancellationToken).ConfigureAwait(false);
            return Ok(new { agent, sessionId, requestId, totalDialogCount = totalCount });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return NotFound(new { error = ex.Message });
        }
    }
}
