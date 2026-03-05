using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Handlers;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.UI.Core.Tests.Handlers;

public sealed class VoiceHandlersTests
{
    [Fact]
    public async Task CreateVoiceSessionHandler_CallsClientAndReturnsSession()
    {
        var client = Substitute.For<IVoiceApiClient>();
        var expected = new VoiceSessionInfo("session-1", "active", "en", "requested", "resolved");
        client.CreateSessionAsync(Arg.Any<CreateVoiceSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var handler = new CreateVoiceSessionCommandHandler(
            client,
            AllowOnly(McpActionKeys.VoiceCreateSession),
            NullLogger<CreateVoiceSessionCommandHandler>.Instance);

        var command = new CreateVoiceSessionCommand { DeviceId = "dev-01", Language = "en" };
        var result = await handler.HandleAsync(command, CallContextFactory.Create());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("session-1", result.Value!.SessionId);
        await client.Received(1).CreateSessionAsync(
            Arg.Is<CreateVoiceSessionCommand>(c => c != null && c.DeviceId == "dev-01"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitVoiceTurnHandler_CallsClientAndReturnsTurn()
    {
        var client = Substitute.For<IVoiceApiClient>();
        var expected = new VoiceTurnInfo(
            SessionId: "session-1",
            TurnId: "turn-1",
            Status: "completed",
            AssistantDisplayText: "done",
            AssistantSpeakText: "done",
            ToolCalls: [],
            Error: null,
            LatencyMs: 42,
            ModelRequested: "req",
            ModelResolved: "res");
        client.SubmitTurnAsync(Arg.Any<SubmitVoiceTurnCommand>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var handler = new SubmitVoiceTurnCommandHandler(
            client,
            AllowOnly(McpActionKeys.VoiceSubmitTurn),
            NullLogger<SubmitVoiceTurnCommandHandler>.Instance);

        var command = new SubmitVoiceTurnCommand
        {
            SessionId = "session-1",
            UserTranscriptText = "hello"
        };

        var result = await handler.HandleAsync(command, CallContextFactory.Create());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("turn-1", result.Value!.TurnId);
        await client.Received(1).SubmitTurnAsync(
            Arg.Is<SubmitVoiceTurnCommand>(c =>
                c != null &&
                c.SessionId == "session-1" &&
                c.UserTranscriptText == "hello"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InterruptVoiceHandler_CallsClientAndReturnsInterruptInfo()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.InterruptAsync(Arg.Any<InterruptVoiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(new VoiceInterruptInfo("session-1", true, "interrupted"));

        var handler = new InterruptVoiceCommandHandler(
            client,
            AllowOnly(McpActionKeys.VoiceInterrupt),
            NullLogger<InterruptVoiceCommandHandler>.Instance);

        var result = await handler.HandleAsync(new InterruptVoiceCommand("session-1"), CallContextFactory.Create());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Interrupted);
        await client.Received(1).InterruptAsync(
            Arg.Is<InterruptVoiceCommand>(c => c != null && c.SessionId == "session-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVoiceStatusHandler_CallsClientAndReturnsStatus()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.GetStatusAsync("session-1", Arg.Any<CancellationToken>())
            .Returns(new VoiceSessionStatusInfo(
                SessionId: "session-1",
                Status: "active",
                Language: "en",
                CreatedUtc: "2026-03-01T00:00:00Z",
                LastUpdatedUtc: "2026-03-01T00:00:05Z",
                IsTurnActive: false,
                LastError: null,
                LastTurnId: "turn-3",
                TurnCounter: 3,
                TranscriptCount: 8));

        var handler = new GetVoiceStatusQueryHandler(
            client,
            AllowOnly(McpActionKeys.VoiceStatus),
            NullLogger<GetVoiceStatusQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetVoiceStatusQuery("session-1"), CallContextFactory.Create());

        Assert.True(result.IsSuccess);
        Assert.Equal("active", result.Value!.Status);
        await client.Received(1).GetStatusAsync("session-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVoiceTranscriptHandler_CallsClientAndReturnsTranscript()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.GetTranscriptAsync("session-1", Arg.Any<CancellationToken>())
            .Returns(new VoiceTranscriptInfo(
                "session-1",
                [new VoiceTranscriptEntryInfo("2026-03-01T00:00:00Z", "turn-1", "user", "text", "hello")]));

        var handler = new GetVoiceTranscriptQueryHandler(
            client,
            AllowOnly(McpActionKeys.VoiceTranscript),
            NullLogger<GetVoiceTranscriptQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetVoiceTranscriptQuery("session-1"), CallContextFactory.Create());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value!.Items);
        await client.Received(1).GetTranscriptAsync("session-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteVoiceSessionHandler_CallsClientAndReturnsDeletionFlag()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.DeleteSessionAsync("session-1", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new DeleteVoiceSessionCommandHandler(
            client,
            AllowOnly(McpActionKeys.VoiceDeleteSession),
            NullLogger<DeleteVoiceSessionCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteVoiceSessionCommand("session-1"), CallContextFactory.Create());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        await client.Received(1).DeleteSessionAsync("session-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateVoiceSessionHandler_WhenUnauthorized_ReturnsPermissionFailure()
    {
        var client = Substitute.For<IVoiceApiClient>();
        var auth = new ConfigurableAuthorizationPolicyService(defaultAllow: false)
            .SetAction(McpActionKeys.VoiceCreateSession, allowed: false, requiredRole: McpRoles.Viewer);

        var handler = new CreateVoiceSessionCommandHandler(
            client,
            auth,
            NullLogger<CreateVoiceSessionCommandHandler>.Instance);

        var result = await handler.HandleAsync(new CreateVoiceSessionCommand(), CallContextFactory.Create());

        Assert.True(result.IsFailure);
        Assert.Contains("Permission denied", result.Error);
        await client.DidNotReceiveWithAnyArgs().CreateSessionAsync(default!, default);
    }

    private static ConfigurableAuthorizationPolicyService AllowOnly(string actionKey)
        => new ConfigurableAuthorizationPolicyService(defaultAllow: false)
            .SetAction(actionKey, allowed: true, requiredRole: McpRoles.Viewer);
}
