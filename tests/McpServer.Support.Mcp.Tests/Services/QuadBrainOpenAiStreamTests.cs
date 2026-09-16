using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-QBPROGRESS-001: stream=true emits role events before the Arbiter assistant chunk.</summary>
public sealed class QuadBrainOpenAiStreamTests
{
    /// <summary>Role SSE events appear in the body before the final assistant content.</summary>
    [Fact]
    public async Task ChatCompletions_StreamTrue_WritesRoleEventsBeforeArbiterChunk()
    {
        var tokenService = new WorkspaceTokenService();
        var token = tokenService.GenerateToken(@"F:\ws");
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        http.Request.Headers.Authorization = $"Bearer {token}";
        var controller = new QuadBrainOpenAiController(new ProgressingChatService(), tokenService)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
        };

        var result = await controller.ChatCompletionsAsync(
            new OpenAiChatCompletionRequest
            {
                Stream = true,
                Messages = [new OpenAiChatMessage { Role = "user", Content = "hi" }],
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        await result.ExecuteResultAsync(controller.ControllerContext).ConfigureAwait(true);
        http.Response.Body.Position = 0;
        var body = await new StreamReader(http.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Contains("event: quadbrain.role", body, StringComparison.Ordinal);
        Assert.Contains("\"role\":\"Creativity\"", body, StringComparison.Ordinal);
        Assert.Contains("c-out", body, StringComparison.Ordinal);
        Assert.Contains("arbiter-decision", body, StringComparison.Ordinal);
        Assert.True(body.IndexOf("c-out", StringComparison.Ordinal) < body.IndexOf("arbiter-decision", StringComparison.Ordinal));
        Assert.Contains("data: [DONE]", body, StringComparison.Ordinal);
    }

    private sealed class ProgressingChatService : IQuadBrainOpenAiChatService
    {
        public async Task<OpenAiChatCompletionResponse> CompleteAsync(
            OpenAiChatCompletionRequest request,
            string? sessionId = null,
            string? turnId = null,
            CancellationToken cancellationToken = default)
        {
            request.Progress?.Report(QuadBrainRoleProgress.Started(BrainSlotRoles.Creativity));
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            request.Progress?.Report(QuadBrainRoleProgress.Completed(BrainSlotRoles.Creativity, "c-out"));
            return new OpenAiChatCompletionResponse
            {
                Id = "chat-progress",
                Model = "QuadBrain",
                Choices =
                [
                    new OpenAiChatChoice
                    {
                        Message = new OpenAiChatResponseMessage { Role = "assistant", Content = "arbiter-decision" },
                        FinishReason = "stop",
                    },
                ],
            };
        }
    }
}
