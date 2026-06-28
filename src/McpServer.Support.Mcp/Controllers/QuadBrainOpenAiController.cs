using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-QBOPENAI-001: OpenAI-compatible chat-completions surface backed by QuadBrain orchestration. Lets an
/// OpenAI-compatible client (such as QBAgent) point at <c>{baseUrl}/v1</c> and use QuadBrain as a drop-in model.
/// </summary>
[ApiController]
[Route("v1")]
public sealed class QuadBrainOpenAiController : ControllerBase
{
    private readonly IQuadBrainOpenAiChatService _chat;
    private readonly WorkspaceTokenService _tokenService;

    /// <summary>Initializes a new instance of the <see cref="QuadBrainOpenAiController"/> class.</summary>
    /// <param name="chat">The QuadBrain OpenAI chat-completion service.</param>
    /// <param name="tokenService">The workspace token service used to authorize Bearer tokens.</param>
    public QuadBrainOpenAiController(IQuadBrainOpenAiChatService chat, WorkspaceTokenService tokenService)
    {
        _chat = chat ?? throw new ArgumentNullException(nameof(chat));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    /// <summary>OpenAI-compatible chat-completions endpoint (<c>POST /v1/chat/completions</c>).</summary>
    /// <param name="request">The OpenAI-compatible chat-completion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An OpenAI-compatible chat-completion response.</returns>
    [HttpPost("chat/completions")]
    [Produces("application/json", "text/event-stream")]
    [ProducesResponseType(typeof(OpenAiChatCompletionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChatCompletionsAsync(
        [FromBody] OpenAiChatCompletionRequest? request,
        CancellationToken cancellationToken)
    {
        var token = OpenAiBearerAuth.ExtractToken(Request.Headers.Authorization, Request.Headers["X-Api-Key"]);
        if (token is null || _tokenService.ResolveWorkspaceByToken(token) is null)
            return Unauthorized(new { error = "A valid workspace token is required (Authorization: Bearer <token>)." });

        if (request is null || request.Messages is not { Count: > 0 })
            return BadRequest(new { error = "messages is required." });

        // FR-MCP-QUAD-SESSION-001: a QuadBrain instance is attached to a single session via the X-Session-Id
        // header (multiple instances run concurrently, one per session). X-Turn-Id optionally correlates the turn.
        var sessionId = Request.Headers["X-Session-Id"].FirstOrDefault();
        var turnId = Request.Headers["X-Turn-Id"].FirstOrDefault();

        try
        {
            var completion = await _chat.CompleteAsync(request, sessionId, turnId, cancellationToken).ConfigureAwait(false);
            return request.Stream
                ? new OpenAiChatCompletionStreamResult(completion)
                : Ok(completion);
        }
        catch (ArgumentException ex)
        {
            // FR-MCP-QBOPENAI-001: invalid request shape maps to an OpenAI-compatible 400 error envelope.
            return BadRequest(new { error = new { message = ex.Message, type = "invalid_request_error" } });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The client aborted; nothing to return.
            throw;
        }
        catch (Exception ex)
        {
            // FR-MCP-QBOPENAI-001 (G-016): orchestration/provider failures map to an OpenAI-compatible 500 error
            // envelope rather than a raw stack trace, so OpenAI clients can parse the failure consistently.
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = new { message = ex.Message, type = "server_error" } });
        }
    }

    private sealed class OpenAiChatCompletionStreamResult(OpenAiChatCompletionResponse completion) : IActionResult
    {
        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = "text/event-stream";

            var choice = completion.Choices.FirstOrDefault() ?? new OpenAiChatChoice();
            await WriteEventAsync(response, CreateDeltaChunk(choice), context.HttpContext.RequestAborted)
                .ConfigureAwait(false);
            await WriteEventAsync(response, CreateTerminalChunk(choice), context.HttpContext.RequestAborted)
                .ConfigureAwait(false);
            await response.WriteAsync("data: [DONE]\n\n", context.HttpContext.RequestAborted).ConfigureAwait(false);
            await response.Body.FlushAsync(context.HttpContext.RequestAborted).ConfigureAwait(false);
        }

        private object CreateDeltaChunk(OpenAiChatChoice choice)
            => new
            {
                id = completion.Id,
                @object = "chat.completion.chunk",
                created = completion.Created,
                model = completion.Model,
                choices = new[]
                {
                    new
                    {
                        index = choice.Index,
                        delta = new
                        {
                            role = "assistant",
                            content = choice.Message.Content,
                            tool_calls = choice.Message.ToolCalls,
                        },
                        finish_reason = (string?)null,
                    },
                },
            };

        private object CreateTerminalChunk(OpenAiChatChoice choice)
            => new
            {
                id = completion.Id,
                @object = "chat.completion.chunk",
                created = completion.Created,
                model = completion.Model,
                choices = new[]
                {
                    new
                    {
                        index = choice.Index,
                        delta = new { },
                        finish_reason = choice.FinishReason,
                    },
                },
            };

        private static async Task WriteEventAsync(HttpResponse response, object payload, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await response.WriteAsync("data: ", cancellationToken).ConfigureAwait(false);
            await response.WriteAsync(json, cancellationToken).ConfigureAwait(false);
            await response.WriteAsync("\n\n", cancellationToken).ConfigureAwait(false);
        }
    }
}
