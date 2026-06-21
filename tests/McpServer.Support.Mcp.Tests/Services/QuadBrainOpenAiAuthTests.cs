using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-QBOPENAI-001 (slice 3): Verifies Bearer/api-key extraction and that the QuadBrain OpenAI
/// endpoint authorizes a valid workspace token and rejects a missing/invalid one.
/// </summary>
public sealed class QuadBrainOpenAiAuthTests
{
    /// <summary>A Bearer authorization header yields its token.</summary>
    [Fact]
    public void ExtractToken_Bearer_ReturnsToken()
        => Assert.Equal("abc123", OpenAiBearerAuth.ExtractToken("Bearer abc123", null));

    /// <summary>The X-Api-Key header is used when there is no Bearer authorization.</summary>
    [Fact]
    public void ExtractToken_ApiKeyFallback_ReturnsKey()
        => Assert.Equal("key-9", OpenAiBearerAuth.ExtractToken(null, "key-9"));

    /// <summary>A non-Bearer authorization with no api key yields null.</summary>
    [Fact]
    public void ExtractToken_NoneUsable_ReturnsNull()
        => Assert.Null(OpenAiBearerAuth.ExtractToken("Basic xxx", null));

    /// <summary>A valid workspace token in the Authorization header is accepted.</summary>
    [Fact]
    public async Task ChatCompletions_ValidBearer_Authorized()
    {
        var tokenService = new WorkspaceTokenService();
        var token = tokenService.GenerateToken(@"F:\ws");
        var controller = BuildController(tokenService, $"Bearer {token}");

        var result = await controller.ChatCompletionsAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "hi" }] },
            CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    /// <summary>A missing token is rejected with 401.</summary>
    [Fact]
    public async Task ChatCompletions_NoToken_Unauthorized()
    {
        var controller = BuildController(new WorkspaceTokenService(), authorizationHeader: null);

        var result = await controller.ChatCompletionsAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "hi" }] },
            CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    private static QuadBrainOpenAiController BuildController(WorkspaceTokenService tokenService, string? authorizationHeader)
    {
        var controller = new QuadBrainOpenAiController(new FakeChatService(), tokenService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        if (!string.IsNullOrEmpty(authorizationHeader))
            controller.ControllerContext.HttpContext.Request.Headers.Authorization = authorizationHeader;
        return controller;
    }

    private sealed class FakeChatService : IQuadBrainOpenAiChatService
    {
        public Task<OpenAiChatCompletionResponse> CompleteAsync(
            OpenAiChatCompletionRequest request,
            string? sessionId = null,
            string? turnId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new OpenAiChatCompletionResponse
            {
                Choices = [new OpenAiChatChoice { Message = new OpenAiChatResponseMessage { Content = "ok" } }],
            });
    }
}
