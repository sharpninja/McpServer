using System.Net.WebSockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.AgentHelp;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-HELP-001: Agent Help conversation endpoints for session lifecycle, turns, transcripts, and streaming.
/// TR-MCP-HELP-002: HTTP and WebSocket API surface for help orchestration.
/// </summary>
[ApiController]
[Route("mcpserver/agent-help")]
public sealed class AgentHelpController : ControllerBase
{
    private static readonly JsonSerializerOptions s_sseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly IAgentHelpConversationService _helpService;
    private readonly WorkspaceContext _workspaceContext;
    private readonly ILogger<AgentHelpController> _logger;

    /// <summary>
    /// TR-MCP-HELP-002: Creates a new <see cref="AgentHelpController"/>.
    /// </summary>
    public AgentHelpController(
        IAgentHelpConversationService helpService,
        WorkspaceContext workspaceContext,
        ILogger<AgentHelpController> logger)
    {
        _helpService = helpService ?? throw new ArgumentNullException(nameof(helpService));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// FR-MCP-HELP-001: Creates a new Agent Help session.
    /// TR-MCP-HELP-002: Session create endpoint.
    /// </summary>
    [HttpPost("session")]
    public async Task<ActionResult<AgentHelpSessionCreateResponse>> CreateSessionAsync(
        [FromBody] AgentHelpSessionCreateRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            request ??= new AgentHelpSessionCreateRequest();
            if (string.IsNullOrWhiteSpace(request.WorkspacePath))
                request = request with { WorkspacePath = _workspaceContext.WorkspacePath };

            var result = await _helpService.CreateSessionAsync(request, cancellationToken).ConfigureAwait(false);
            return Created(
                new Uri($"/mcpserver/agent-help/session/{Uri.EscapeDataString(result.SessionId)}", UriKind.Relative),
                result);
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
    /// FR-MCP-HELP-001: Gets the current status for an Agent Help session.
    /// TR-MCP-HELP-002: Session status endpoint.
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public async Task<ActionResult<AgentHelpSessionStatusDto>> GetStatusAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _helpService.GetStatusAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (result is null)
                return NotFound(new { error = $"Agent Help session '{sessionId}' not found." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    /// <summary>
    /// FR-MCP-HELP-001: Submits a single help turn for synchronous processing.
    /// TR-MCP-HELP-002: Turn endpoint.
    /// </summary>
    [HttpPost("session/{sessionId}/turn")]
    public async Task<ActionResult<AgentHelpTurnResponse>> SubmitTurnAsync(
        string sessionId,
        [FromBody] AgentHelpTurnRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        try
        {
            var result = await _helpService.SubmitTurnAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
            if (result is null)
                return NotFound(new { error = $"Agent Help session '{sessionId}' not found." });
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
    /// FR-MCP-HELP-001: Submits a help turn with streaming response via Server-Sent Events.
    /// TR-MCP-HELP-002: SSE turn stream endpoint.
    /// </summary>
    [HttpPost("session/{sessionId}/turn/stream")]
    public async Task SubmitTurnStreamingAsync(
        string sessionId,
        [FromBody] AgentHelpTurnRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "Request body is required." }, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        _logger.LogInformation("Agent Help SSE stream starting for session {SessionId}", sessionId);
        var eventCount = 0;

        try
        {
            await foreach (var evt in _helpService
                .SubmitTurnStreamingAsync(sessionId, request, cancellationToken)
                .ConfigureAwait(false))
            {
                eventCount++;
                var json = JsonSerializer.Serialize(evt, s_sseJsonOptions);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
                await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Agent Help SSE stream completed for session {SessionId}: {EventCount} events",
                sessionId,
                eventCount);
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Agent Help SSE stream canceled for session {SessionId}", sessionId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Agent Help SSE argument error for session {SessionId}", sessionId);
            var json = JsonSerializer.Serialize(new AgentHelpStreamEvent { Type = "error", Message = ex.Message }, s_sseJsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Agent Help SSE stream failed for session {SessionId}", sessionId);
            var json = JsonSerializer.Serialize(
                new AgentHelpStreamEvent { Type = "error", Message = $"Agent Help turn stream failed: {ex.Message}" },
                s_sseJsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// FR-MCP-HELP-003: Returns transcript entries captured for an Agent Help session.
    /// TR-MCP-HELP-003: Transcript retrieval endpoint.
    /// </summary>
    [HttpGet("session/{sessionId}/transcript")]
    public async Task<ActionResult<AgentHelpTranscriptResponse>> GetTranscriptAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _helpService.GetTranscriptAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (result is null)
                return NotFound(new { error = $"Agent Help session '{sessionId}' not found." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    /// <summary>
    /// FR-MCP-HELP-001: Accepts a WebSocket connection for bidirectional Agent Help turn streaming.
    /// TR-MCP-HELP-002: WebSocket stream endpoint using JSON frames for client messages and server events.
    /// </summary>
    [HttpGet("session/{sessionId}/stream")]
    public async Task StreamWebSocketAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                new { error = "WebSocket upgrade required. Connect with a WebSocket client." },
                cancellationToken).ConfigureAwait(false);
            return;
        }

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        _logger.LogInformation("Agent Help WebSocket connected for session {SessionId}", sessionId);

        var receiveBuffer = new byte[16 * 1024];
        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var receiveResult = await webSocket.ReceiveAsync(receiveBuffer, cancellationToken).ConfigureAwait(false);
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                    break;

                if (receiveResult.MessageType != WebSocketMessageType.Text)
                    continue;

                var messageJson = Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count);
                AgentHelpWebSocketClientMessage? clientMessage;
                try
                {
                    clientMessage = JsonSerializer.Deserialize<AgentHelpWebSocketClientMessage>(messageJson, s_sseJsonOptions);
                }
                catch (JsonException)
                {
                    await SendWebSocketEventAsync(
                        webSocket,
                        new AgentHelpStreamEvent { Type = "error", Message = "Invalid JSON client frame." },
                        cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (clientMessage is null || !string.Equals(clientMessage.Type, "turn", StringComparison.OrdinalIgnoreCase))
                {
                    await SendWebSocketEventAsync(
                        webSocket,
                        new AgentHelpStreamEvent { Type = "error", Message = "Expected client frame type 'turn'." },
                        cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(clientMessage.UserMessage))
                {
                    await SendWebSocketEventAsync(
                        webSocket,
                        new AgentHelpStreamEvent { Type = "error", Message = "UserMessage is required." },
                        cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var turnRequest = new AgentHelpTurnRequest { UserMessage = clientMessage.UserMessage };
                await foreach (var evt in _helpService
                    .SubmitTurnStreamingAsync(sessionId, turnRequest, cancellationToken)
                    .ConfigureAwait(false))
                {
                    await SendWebSocketEventAsync(webSocket, evt, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Agent Help WebSocket canceled for session {SessionId}", sessionId);
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "Agent Help WebSocket closed unexpectedly for session {SessionId}", sessionId);
        }
        finally
        {
            if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Session stream closed.",
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private static async Task SendWebSocketEventAsync(
        WebSocket webSocket,
        AgentHelpStreamEvent evt,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(evt, s_sseJsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await webSocket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken)
            .ConfigureAwait(false);
    }
}