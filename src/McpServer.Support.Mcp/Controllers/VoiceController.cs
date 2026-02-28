using System.Text.Json;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// Voice conversation endpoints for Android clients (session lifecycle, turns, interrupts, transcript/status).
/// </summary>
[ApiController]
[Route("mcpserver/voice")]
public sealed class VoiceController : ControllerBase
{
    private static readonly JsonSerializerOptions s_sseJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    private readonly IVoiceConversationService _voiceService;
    private readonly WorkspaceContext _workspaceContext;
    private readonly ILogger<VoiceController> _logger;

    /// <summary>
    /// Creates a new <see cref="VoiceController"/>.
    /// </summary>
    public VoiceController(IVoiceConversationService voiceService, WorkspaceContext workspaceContext, ILogger<VoiceController> logger)
    {
        _voiceService = voiceService ?? throw new ArgumentNullException(nameof(voiceService));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new voice conversation session.
    /// </summary>
    [HttpPost("session")]
    public async Task<ActionResult<VoiceSessionCreateResponse>> CreateSessionAsync(
        [FromBody] VoiceSessionCreateRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Workspace resolution is enforced by WorkspaceResolutionMiddleware —
            // if we get here, the workspace is valid.

            // Stamp the resolved workspace path so Copilot launches with the correct CWD
            request ??= new VoiceSessionCreateRequest();
            if (string.IsNullOrWhiteSpace(request.WorkspacePath))
                request.WorkspacePath = _workspaceContext.WorkspacePath;

            var result = await _voiceService.CreateSessionAsync(request, cancellationToken).ConfigureAwait(false);
            return Created(new Uri($"/mcpserver/voice/session/{Uri.EscapeDataString(result.SessionId)}", UriKind.Relative), result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Submits a single transcribed voice turn for processing.
    /// </summary>
    [HttpPost("session/{sessionId}/turn")]
    public async Task<ActionResult<VoiceTurnResponse>> SubmitTurnAsync(
        string sessionId,
        [FromBody] VoiceTurnRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        try
        {
            var result = await _voiceService.SubmitTurnAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
            if (result is null)
                return NotFound(new { error = $"Voice session '{sessionId}' not found." });
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice turn request failed for session {SessionId}", sessionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Voice turn processing failed." });
        }
    }

    /// <summary>
    /// Submits a single transcribed voice turn with streaming response via Server-Sent Events.
    /// </summary>
    [HttpPost("session/{sessionId}/turn/stream")]
    public async Task SubmitTurnStreamingAsync(
        string sessionId,
        [FromBody] VoiceTurnRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "Request body is required." }, cancellationToken).ConfigureAwait(false);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        _logger.LogInformation("SSE stream starting for session {SessionId}", sessionId);
        var eventCount = 0;

        try
        {
            await foreach (var evt in _voiceService.SubmitTurnStreamingAsync(sessionId, request, cancellationToken).ConfigureAwait(false))
            {
                eventCount++;
                var json = JsonSerializer.Serialize(evt, s_sseJsonOptions);
                _logger.LogDebug("SSE event #{Count} type={Type} for session {SessionId}: {Json}", eventCount, evt.Type, sessionId, json.Length > 200 ? json[..200] + "..." : json);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
                await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("SSE stream completed for session {SessionId}: {EventCount} events", sessionId, eventCount);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "SSE stream argument error for session {SessionId} after {EventCount} events", sessionId, eventCount);
            var json = JsonSerializer.Serialize(new VoiceTurnStreamEvent { Type = "error", Message = ex.Message }, s_sseJsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Streaming voice turn failed for session {SessionId} after {EventCount} events", sessionId, eventCount);
            var json = JsonSerializer.Serialize(new VoiceTurnStreamEvent { Type = "error", Message = "Voice turn processing failed." }, s_sseJsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Interrupts the active turn for a voice session, if one exists.
    /// </summary>
    [HttpPost("session/{sessionId}/interrupt")]
    public async Task<ActionResult<VoiceInterruptResponse>> InterruptAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _voiceService.InterruptAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (result is null)
                return NotFound(new { error = $"Voice session '{sessionId}' not found." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the current status for a voice session.
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public async Task<ActionResult<VoiceSessionStatusDto>> GetStatusAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _voiceService.GetStatusAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (result is null)
                return NotFound(new { error = $"Voice session '{sessionId}' not found." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Returns transcript entries captured for a voice session.
    /// </summary>
    [HttpGet("session/{sessionId}/transcript")]
    public async Task<ActionResult<VoiceTranscriptResponse>> GetTranscriptAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _voiceService.GetTranscriptAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (result is null)
                return NotFound(new { error = $"Voice session '{sessionId}' not found." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a voice session and any in-memory transcript/tool state for it.
    /// </summary>
    [HttpDelete("session/{sessionId}")]
    public async Task<IActionResult> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _voiceService.DeleteSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (!deleted)
                return NotFound(new { error = $"Voice session '{sessionId}' not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }
}
