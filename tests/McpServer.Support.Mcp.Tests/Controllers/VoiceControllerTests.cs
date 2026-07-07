using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-089: Validates VoiceController streaming endpoint fault handling for FR-MCP-046 voice sessions.
/// Uses a mocked <see cref="IVoiceConversationService"/> that throws on the initial presence message to
/// verify the controller returns an SSE error event instead of allowing an unhandled exception to escape.
/// </summary>
public sealed class VoiceControllerTests
{
    /// <summary>
    /// TEST-MCP-089: Verifies that a send-message failure during stream setup is handled in-controller and
    /// surfaced to the client as an SSE error payload rather than crashing the ASP.NET request pipeline.
    /// </summary>
    [Fact]
    public async Task SubmitTurnStreamingAsync_SendSessionMessageThrows_WritesErrorEvent()
    {
        var voiceService = Substitute.For<IVoiceConversationService>();
        voiceService
            .SendSessionMessageAsync("voice-1", "User is here.", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new IOException("The pipe is being closed.")));
        voiceService
            .SubmitTurnStreamingAsync("voice-1", Arg.Any<VoiceTurnRequest>(), Arg.Any<CancellationToken>())
            .Returns(EmptyEvents());

        var controller = new VoiceController(
            voiceService,
            new WorkspaceContext { WorkspacePath = @"E:\github\McpServer" },
            NullLogger<VoiceController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Response.Body = new MemoryStream();

        await controller.SubmitTurnStreamingAsync(
            "voice-1",
            new VoiceTurnRequest { UserTranscriptText = "Hello" },
            CancellationToken.None).ConfigureAwait(true);

        controller.Response.Body.Position = 0;
        using var reader = new StreamReader(controller.Response.Body, leaveOpen: true);
        var payload = await reader.ReadToEndAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("text/event-stream", controller.Response.ContentType);
        Assert.Contains("Voice turn processing failed.", payload, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-161: Verifies transaction-gate failures during stream setup return a pre-stream service
    /// unavailable response instead of starting an SSE stream.
    /// </summary>
    [Fact]
    public async Task SubmitTurnStreamingAsync_SendSessionMessageTransactionGate_ReturnsServiceUnavailableJson()
    {
        var voiceService = Substitute.For<IVoiceConversationService>();
        voiceService
            .SendSessionMessageAsync("voice-1", "User is here.", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("Voice mutations are not transaction compensated.")));

        var controller = new VoiceController(
            voiceService,
            new WorkspaceContext { WorkspacePath = @"E:\github\McpServer" },
            NullLogger<VoiceController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Response.Body = new MemoryStream();

        await controller.SubmitTurnStreamingAsync(
            "voice-1",
            new VoiceTurnRequest { UserTranscriptText = "Hello" },
            CancellationToken.None).ConfigureAwait(true);

        controller.Response.Body.Position = 0;
        using var reader = new StreamReader(controller.Response.Body, leaveOpen: true);
        var payload = await reader.ReadToEndAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, controller.Response.StatusCode);
        Assert.NotEqual("text/event-stream", controller.Response.ContentType);
        Assert.Contains("not transaction compensated", payload, StringComparison.OrdinalIgnoreCase);
    }

    private static async IAsyncEnumerable<VoiceTurnStreamEvent> EmptyEvents()
    {
        await Task.CompletedTask.ConfigureAwait(true);
        yield break;
    }
}
