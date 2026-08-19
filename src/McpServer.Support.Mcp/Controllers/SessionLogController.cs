using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// TR-PLANNED-CORE-013: Session log submit and query endpoints (MVP-SUPPORT-011).
/// FR-SUPPORT-010: Agents POST session log payloads; clients GET with optional filters.
/// </summary>
[ApiController]
[Route("mcpserver/sessionlog")]
public sealed class SessionLogController : ControllerBase
{
    private const int MaxTurnCount = 5000;

    private readonly ISessionLogService _service;
    private readonly ILogger<SessionLogController> _logger;

    /// <summary>TR-PLANNED-CORE-013: Constructor.</summary>
    public SessionLogController(ISessionLogService service,
        ILogger<SessionLogController> logger)
    {
        _logger = logger;
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// TR-PLANNED-CORE-013: Submit a session log (upsert by SourceType + SessionId).
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
            return ClassifiedError(new ArgumentException("sourceType is required."));

        if (string.IsNullOrWhiteSpace(dto.SessionId))
            return ClassifiedError(new ArgumentException("sessionId is required."));

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

        try
        {
            var id = await _service.SubmitAsync(dto, sourceFilePath: null, contentHash: null, cancellationToken).ConfigureAwait(false);

            return Created(
                new Uri($"/mcpserver/sessionlog?agent={Uri.EscapeDataString(dto.SourceType)}&sessionId={Uri.EscapeDataString(dto.SessionId)}", UriKind.Relative),
                new { id, sourceType = dto.SourceType, sessionId = dto.SessionId });
        }
        catch (Exception ex) when (ex is DbUpdateException or ArgumentException or InvalidOperationException or StorageCommandBudgetExceededException)
        {
            return ClassifiedError(ex);
        }
    }

    /// <summary>
    /// TR-PLANNED-CORE-013: Query session logs with optional filters and pagination.
    /// </summary>
    /// <param name="agent">Filter by agent source type.</param>
    /// <param name="agentDefinitionId">Filter by linked agent definition identifier.</param>
    /// <param name="model">Filter by AI model.</param>
    /// <param name="text">Full-text search over turn text fields.</param>
    /// <param name="from">Sessions started on or after this date (ISO 8601).</param>
    /// <param name="to">Sessions last updated on or before this date (ISO 8601).</param>
    /// <param name="limit">Page size (default 100, max 1000).</param>
    /// <param name="offset">Number of sessions to skip (default 0).</param>
    /// <param name="planFile">Exact planFile filter (None or a path; <c>~/</c> is expanded).</param>
    /// <param name="todoId">Exact todoId filter (None or a canonical TODO id).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with paginated session logs.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SessionLogQueryResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> QueryAsync(
        [FromQuery] string? agent,
        [FromQuery] string? agentDefinitionId,
        [FromQuery] string? model,
        [FromQuery] string? text,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        [FromQuery] string? planFile = null,
        [FromQuery] string? todoId = null,
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
            Offset = offset,
            PlanFile = planFile,
            TodoId = todoId
        };

        try
        {
            var result = await _service.QueryAsync(request, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (Exception ex) when (ex is SessionLogSchemaPendingMigrationException or InvalidOperationException)
        {
            return ClassifiedError(ex);
        }
    }

    /// <summary>
    /// FR-SUPPORT-013: Fetch a single session log by (agent, sessionId). Returns
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
    /// FR-SUPPORT-013: Upsert a single turn on an existing session by RequestId.
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
            return ClassifiedError(ex);
        }
    }

    /// <summary>
    /// TR-PLANNED-CORE-013: Append processing dialog items to an existing turn.
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
            return ClassifiedError(ex);
        }
    }

    /// <summary>
    /// FR-SUPPORT-014: Idempotent ensure-session keyed by (agent, sessionId).
    /// Creates the session when missing; otherwise leaves it untouched. Stateless:
    /// callable from any process with no prior in-process session state.
    /// </summary>
    /// <param name="agent">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="body">Optional title/model for a newly created session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the session key and whether it was created.</returns>
    [HttpPost("{agent}/{sessionId}/open")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OpenSessionAsync(
        string agent,
        string sessionId,
        [FromBody] SessionLifecycleOpenRequest? body,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.OpenSessionAsync(agent, sessionId, body?.Title, body?.Model, cancellationToken).ConfigureAwait(false);
            return Ok(new { agent, sessionId, created });
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid session identifier.");
        }
    }

    /// <summary>
    /// TR-MCP-SESSIONLOG-005: Explicitly set an existing session's title. This is
    /// the dedicated session-retitle path, so a whole-session submit (which the
    /// plugin now issues with the title omitted on incidental re-submit) cannot
    /// clobber an agent rename.
    /// </summary>
    /// <param name="agent">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="body">Body carrying the new session title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the session row id, 400 on validation failure, or 404 if the session does not exist.</returns>
    [HttpPost("{agent}/{sessionId}/title")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetSessionTitleAsync(
        string agent,
        string sessionId,
        [FromBody] SessionTitleRequest? body,
        CancellationToken cancellationToken)
    {
        try
        {
            var sessionRowId = await _service.SetSessionTitleAsync(agent, sessionId, body?.Title ?? string.Empty, cancellationToken).ConfigureAwait(false);
            return Ok(new { turnId = sessionRowId, agent, sessionId, retitled = true });
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid session identifier.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return ClassifiedError(ex);
        }
    }

    /// <summary>
    /// TR-MCP-SESSIONLOG-005: Explicitly set an existing turn's title. The
    /// dedicated turn-retitle path; unlike the PUT replace it does not reset
    /// omitted scalars or clear collections.
    /// </summary>
    /// <param name="agent">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Turn request identifier.</param>
    /// <param name="body">Body carrying the new turn title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the turn id, 400 on validation failure, or 404 if the session or turn does not exist.</returns>
    [HttpPost("{agent}/{sessionId}/{requestId}/title")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTurnTitleAsync(
        string agent,
        string sessionId,
        string requestId,
        [FromBody] SessionTitleRequest? body,
        CancellationToken cancellationToken)
    {
        try
        {
            var turnId = await _service.SetTurnTitleAsync(agent, sessionId, requestId, body?.Title ?? string.Empty, cancellationToken).ConfigureAwait(false);
            return Ok(new { turnId, agent, sessionId, requestId, retitled = true });
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid turn identifier.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return ClassifiedError(ex);
        }
    }

    /// <summary>
    /// FR-SUPPORT-014: Begins (or re-opens) a turn keyed by
    /// (agent, sessionId, requestId) with status in_progress. Stateless: no
    /// in-process "active session" is required or kept.
    /// </summary>
    /// <param name="agent">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Turn request identifier.</param>
    /// <param name="body">Optional query title/text and model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the turn id, 400 on validation failure, or 404 if the session does not exist.</returns>
    [HttpPost("{agent}/{sessionId}/{requestId}/begin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> BeginTurnAsync(
        string agent,
        string sessionId,
        string requestId,
        [FromBody] SessionLifecycleBeginRequest? body,
        CancellationToken cancellationToken)
    {
        var turn = new UnifiedRequestEntryDto
        {
            RequestId = requestId,
            Timestamp = body?.Timestamp ?? DateTimeOffset.UtcNow.ToString("o"),
            QueryTitle = body?.QueryTitle,
            QueryText = body?.QueryText,
            Model = body?.Model,
            PlanFile = body?.PlanFile,
            TodoId = body?.TodoId,
            Status = "in_progress",
        };
        return UpsertLifecycleTurnAsync(agent, sessionId, turn, StatusCodes.Status201Created, cancellationToken);
    }

    /// <summary>
    /// FR-SUPPORT-014: Completes a turn keyed by (agent, sessionId, requestId),
    /// merging the supplied payload onto the existing turn (omitted fields are
    /// preserved). The terminal-turn compliance gate requires at least one design
    /// decision, action, or commit.
    /// </summary>
    /// <param name="agent">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Turn request identifier.</param>
    /// <param name="body">Optional turn payload merged onto the existing turn.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the turn id, 400 on compliance/validation failure, or 404 if the session does not exist.</returns>
    [HttpPost("{agent}/{sessionId}/{requestId}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> CompleteTurnAsync(
        string agent,
        string sessionId,
        string requestId,
        [FromBody] UnifiedRequestEntryDto? body,
        CancellationToken cancellationToken)
        => FinalizeTurnAsync(agent, sessionId, requestId, body, "completed", cancellationToken);

    /// <summary>
    /// FR-SUPPORT-014: Fails a turn keyed by (agent, sessionId, requestId),
    /// recording the failure note. Subject to the same terminal-turn compliance
    /// gate as complete.
    /// </summary>
    /// <param name="agent">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Turn request identifier.</param>
    /// <param name="body">Optional turn payload (failureNote, evidence) merged onto the existing turn.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the turn id, 400 on compliance/validation failure, or 404 if the session does not exist.</returns>
    [HttpPost("{agent}/{sessionId}/{requestId}/fail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> FailTurnAsync(
        string agent,
        string sessionId,
        string requestId,
        [FromBody] UnifiedRequestEntryDto? body,
        CancellationToken cancellationToken)
        => FinalizeTurnAsync(agent, sessionId, requestId, body, "failed", cancellationToken);

    private Task<IActionResult> FinalizeTurnAsync(
        string agent,
        string sessionId,
        string requestId,
        UnifiedRequestEntryDto? body,
        string status,
        CancellationToken cancellationToken)
    {
        var turn = body ?? new UnifiedRequestEntryDto();
        turn.RequestId = requestId;
        turn.Status = status;
        turn.Timestamp ??= DateTimeOffset.UtcNow.ToString("o");
        return UpsertLifecycleTurnAsync(agent, sessionId, turn, StatusCodes.Status200OK, cancellationToken);
    }

    private async Task<IActionResult> UpsertLifecycleTurnAsync(
        string agent,
        string sessionId,
        UnifiedRequestEntryDto turn,
        int successStatusCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var turnId = await _service.UpsertTurnAsync(agent, sessionId, turn, cancellationToken).ConfigureAwait(false);
            var payload = new { turnId, agent, sessionId, requestId = turn.RequestId, status = turn.Status };
            return successStatusCode == StatusCodes.Status201Created
                ? Created(
                    new Uri($"/mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}", UriKind.Relative),
                    payload)
                : Ok(payload);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid turn payload.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return ClassifiedError(ex);
        }
    }

    /// <summary>
    /// BUG-SESSIONLOG-WS-005: Re-stamps session-log child rows whose WorkspaceId
    /// drifted away from their parent session. Idempotent data repair for rows
    /// written before the parent-inheritance stamping invariant was enforced.
    /// </summary>
    /// <param name="dryRun">When true, reports the drifted-row count without persisting changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the number of rows re-stamped (or counted when dryRun).</returns>
    [HttpPost("repair-workspace-stamps")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RepairWorkspaceStampsAsync([FromQuery] bool dryRun, CancellationToken cancellationToken)
    {
        var repaired = await _service.RepairWorkspaceStampsAsync(dryRun, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Session log workspace stamp repair {Mode}: {Count} rows", dryRun ? "dry-run" : "applied", repaired);
        return Ok(new { repaired, dryRun });
    }

    /// <summary>
    /// FR-SUPPORT-010G: PATCH a turn - additive merge. Omitted scalar fields and
    /// omitted collections are preserved; collection items are appended. This is
    /// the explicit verb for the long-standing additive submit behavior.
    /// </summary>
    [HttpPatch("{agent}/{sessionId}/{requestId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> PatchTurnAsync(
        string agent,
        string sessionId,
        string requestId,
        [FromBody] UnifiedRequestEntryDto? body,
        CancellationToken cancellationToken)
    {
        var turn = body ?? new UnifiedRequestEntryDto();
        turn.RequestId = requestId;
        return UpsertLifecycleTurnAsync(agent, sessionId, turn, StatusCodes.Status200OK, cancellationToken);
    }

    /// <summary>
    /// FR-SUPPORT-010G: PUT a turn - REPLACE. Omitted scalar fields are reset and
    /// every section becomes exactly what the body carries (omitted/empty sections
    /// are cleared). Use this to remove data by re-stating the turn.
    /// </summary>
    [HttpPut("{agent}/{sessionId}/{requestId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceTurnAsync(
        string agent,
        string sessionId,
        string requestId,
        [FromBody] UnifiedRequestEntryDto? body,
        CancellationToken cancellationToken)
    {
        var turn = body ?? new UnifiedRequestEntryDto();
        turn.RequestId = requestId;
        try
        {
            var turnId = await _service.ReplaceTurnAsync(agent, sessionId, turn, cancellationToken).ConfigureAwait(false);
            return Ok(new { turnId, agent, sessionId, requestId, replaced = true });
        }
        catch (Exception ex)
        {
            return ClassifiedError(ex);
        }
    }

    /// <summary>
    /// FR-SUPPORT-010G: PUT a single turn section - REPLACE just that section.
    /// Sections: actions, tags, context, dialog, commits, designDecisions,
    /// requirementsDiscovered, filesModified, blockers. An empty/omitted section
    /// property clears it. Other sections are left untouched.
    /// </summary>
    [HttpPut("{agent}/{sessionId}/{requestId}/sections/{section}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceTurnSectionAsync(
        string agent,
        string sessionId,
        string requestId,
        string section,
        [FromBody] UnifiedRequestEntryDto? body,
        CancellationToken cancellationToken)
    {
        var payload = body ?? new UnifiedRequestEntryDto();
        payload.RequestId = requestId;
        try
        {
            var found = await _service.ReplaceTurnSectionAsync(agent, sessionId, requestId, section, payload, cancellationToken).ConfigureAwait(false);
            return found
                ? Ok(new { agent, sessionId, requestId, section, replaced = true })
                : ClassifiedError(new KeyNotFoundException($"Turn not found: {agent}/{sessionId}/{requestId}"));
        }
        catch (Exception ex)
        {
            return ClassifiedError(ex);
        }
    }

    /// <summary>
    /// FR-SUPPORT-010G: DELETE all items in a turn section (clear the section).
    /// </summary>
    [HttpDelete("{agent}/{sessionId}/{requestId}/sections/{section}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearTurnSectionAsync(
        string agent,
        string sessionId,
        string requestId,
        string section,
        CancellationToken cancellationToken)
    {
        try
        {
            var found = await _service.ClearTurnSectionAsync(agent, sessionId, requestId, section, cancellationToken).ConfigureAwait(false);
            return found
                ? Ok(new { agent, sessionId, requestId, section, cleared = true })
                : ClassifiedError(new KeyNotFoundException($"Turn not found: {agent}/{sessionId}/{requestId}"));
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid section or identifier.");
        }
    }

    /// <summary>
    /// FR-SUPPORT-010G: DELETE a single item from a turn section. The item key is
    /// the value for string sections (tags/context/string-lists), the SHA for
    /// commits, the Order for actions, and the ordinal for dialog.
    /// </summary>
    [HttpDelete("{agent}/{sessionId}/{requestId}/sections/{section}/items/{itemKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTurnItemAsync(
        string agent,
        string sessionId,
        string requestId,
        string section,
        string itemKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var found = await _service.DeleteTurnItemAsync(agent, sessionId, requestId, section, itemKey, cancellationToken).ConfigureAwait(false);
            return found
                ? Ok(new { agent, sessionId, requestId, section, itemKey, deleted = true })
                : ClassifiedError(new KeyNotFoundException($"Item not found in section '{section}' of turn {agent}/{sessionId}/{requestId}."));
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid section or identifier.");
        }
    }

    /// <summary>
    /// FR-SUPPORT-010G: DELETE a single turn (and all of its child rows). The
    /// parent session is preserved.
    /// </summary>
    [HttpDelete("{agent}/{sessionId}/{requestId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTurnAsync(
        string agent,
        string sessionId,
        string requestId,
        CancellationToken cancellationToken)
    {
        try
        {
            var found = await _service.DeleteTurnAsync(agent, sessionId, requestId, cancellationToken).ConfigureAwait(false);
            return found
                ? Ok(new { agent, sessionId, requestId, deleted = true })
                : ClassifiedError(new KeyNotFoundException($"Turn not found: {agent}/{sessionId}/{requestId}"));
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid identifier.");
        }
    }

    /// <summary>
    /// FR-SUPPORT-010G: DELETE an entire session and every turn and child row
    /// beneath it.
    /// </summary>
    [HttpDelete("{agent}/{sessionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSessionAsync(
        string agent,
        string sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var found = await _service.DeleteSessionAsync(agent, sessionId, cancellationToken).ConfigureAwait(false);
            return found
                ? Ok(new { agent, sessionId, deleted = true })
                : ClassifiedError(new KeyNotFoundException($"Session not found: {agent}/{sessionId}"));
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid identifier.");
        }
    }

    /// <summary>FR-MCP-TRIAGEERR-001: maps persistence and validation failures to the shared envelope.</summary>
    private IActionResult ClassifiedError(Exception exception)
    {
        _logger.LogError("{ExceptionDetail}", exception.ToString());
        var classified = McpErrorClassifier.Classify(exception);
        return new ObjectResult(new
        {
            type = "https://httpstatuses.io/" + classified.StatusCode,
            title = classified.Code,
            status = classified.StatusCode,
            detail = classified.Message,
            code = classified.Code,
            message = classified.Message,
            retryable = classified.Retryable,
            details = classified.Details,
        })
        {
            StatusCode = classified.StatusCode,
            ContentTypes = { "application/problem+json" },
        };
    }
}

/// <summary>FR-SUPPORT-014: Optional body for the stateless open-session endpoint.</summary>
/// <param name="Title">Human-readable session title.</param>
/// <param name="Model">AI model identifier.</param>
public sealed record SessionLifecycleOpenRequest(string? Title, string? Model);

/// <summary>FR-SUPPORT-014 / FR-MCP-SESSIONLOGCTX-001: Optional body for the stateless begin-turn endpoint.</summary>
/// <param name="QueryTitle">Short turn title.</param>
/// <param name="QueryText">Full user query text.</param>
/// <param name="Timestamp">ISO 8601 turn timestamp; defaults to now.</param>
/// <param name="Model">AI model identifier.</param>
/// <param name="PlanFile">Current plan file or the sentinel <c>None</c>. Required on first persist.</param>
/// <param name="TodoId">Current MCP TODO id or the sentinel <c>None</c>. Required on first persist.</param>
public sealed record SessionLifecycleBeginRequest(
    string? QueryTitle,
    string? QueryText,
    string? Timestamp,
    string? Model,
    string? PlanFile,
    string? TodoId);

/// <summary>TR-MCP-SESSIONLOG-005: Body for the explicit session/turn title-set endpoints.</summary>
/// <param name="Title">New title to set on the session or turn.</param>
public sealed record SessionTitleRequest(string? Title);
