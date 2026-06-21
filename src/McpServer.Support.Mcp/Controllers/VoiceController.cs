using System.Text.Encodings.Web;
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
    private static readonly JsonSerializerOptions s_sseJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

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
            request ??= new VoiceSessionCreateRequest();
            if (string.IsNullOrWhiteSpace(request.WorkspacePath))
                request.WorkspacePath = _workspaceContext.WorkspacePath;

            var result = await _voiceService.CreateSessionAsync(request, cancellationToken).ConfigureAwait(false);
            return Created(new Uri($"/mcpserver/voice/session/{Uri.EscapeDataString(result.SessionId)}", UriKind.Relative), result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Finds an active voice session for the specified device.
    /// </summary>
    [HttpGet("session")]
    public ActionResult<VoiceSessionStatusDto> FindSessionByDeviceAsync([FromQuery] string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return BadRequest(new { error = "deviceId query parameter is required." });

        try
        {
            var result = _voiceService.FindSessionByDevice(deviceId);
            if (result is null)
                return NotFound(new { error = $"No active voice session found for device '{deviceId}'." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
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
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
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

        try
        {
            _ = await _voiceService.SendSessionMessageAsync(sessionId, "User is here.", cancellationToken).ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = ex.Message }, cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (InvalidOperationException ex)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await Response.WriteAsJsonAsync(new { error = ex.Message }, cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Streaming voice turn failed before SSE setup for session {SessionId}", sessionId);
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";
            Response.Headers["X-Accel-Buffering"] = "no";
            var json = JsonSerializer.Serialize(new VoiceTurnStreamEvent { Type = "error", Message = $"Voice turn processing failed. {ex.Message}" }, s_sseJsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
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
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("SSE stream canceled by client disconnect for session {SessionId}", sessionId);
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
            var json = JsonSerializer.Serialize(new VoiceTurnStreamEvent { Type = "error", Message = $"Voice turn stream failed after {eventCount} events: {ex.Message}" }, s_sseJsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (HttpContext.RequestAborted.IsCancellationRequested)
            {
                try
                {
                    _ = await _voiceService.SendSessionMessageAsync(sessionId, "User is AFK.", CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Failed to send AFK presence message for session {SessionId}", sessionId);
                }
            }
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
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Sends three ESC characters to the active Copilot interactive session stdin, cancelling the current generation.
    /// </summary>
    [HttpPost("session/{sessionId}/escape")]
    public async Task<IActionResult> SendEscapeAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var sent = await _voiceService.SendEscapeAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (!sent)
                return NotFound(new { error = $"Voice session '{sessionId}' not found or has no active interactive session." });
            return Ok(new { sent = true });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
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
            _logger.LogError("{ExceptionDetail}", ex.ToString());
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
            _logger.LogError("{ExceptionDetail}", ex.ToString());
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
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }
}
