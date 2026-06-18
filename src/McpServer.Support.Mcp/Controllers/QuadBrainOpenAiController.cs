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
    [ProducesResponseType(typeof(OpenAiChatCompletionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OpenAiChatCompletionResponse>> ChatCompletionsAsync(
        [FromBody] OpenAiChatCompletionRequest? request,
        CancellationToken cancellationToken)
    {
        var token = OpenAiBearerAuth.ExtractToken(Request.Headers.Authorization, Request.Headers["X-Api-Key"]);
        if (token is null || _tokenService.ResolveWorkspaceByToken(token) is null)
            return Unauthorized(new { error = "A valid workspace token is required (Authorization: Bearer <token>)." });

        if (request is null || request.Messages is not { Count: > 0 })
            return BadRequest(new { error = "messages is required." });

        try
        {
            return Ok(await _chat.CompleteAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
