using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.Tests.TestInfrastructure;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace McpServer.UI.Core.Tests.ViewModels;

public sealed class VoiceViewModelTests
{
    [Fact]
    public async Task CreateSessionAsync_PopulatesSessionState()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.CreateSessionAsync(Arg.Any<CreateVoiceSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(new VoiceSessionInfo("session-1", "active", "en", "requested", "resolved"));

        using var sp = UiCoreTestHost.Create(services => services.AddSingleton(client));
        var vm = sp.GetRequiredService<VoiceViewModel>();
        vm.DeviceId = "device-1";

        var created = await vm.CreateSessionAsync();

        Assert.NotNull(created);
        Assert.Equal("session-1", vm.SessionId);
        Assert.NotNull(vm.LastSession);
        Assert.Equal("session-1", vm.LastSession!.SessionId);
    }

    [Fact]
    public async Task SubmitTurnAsync_PopulatesLastTurnAndToolCalls()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.SubmitTurnAsync(Arg.Any<SubmitVoiceTurnCommand>(), Arg.Any<CancellationToken>())
            .Returns(new VoiceTurnInfo(
                SessionId: "session-1",
                TurnId: "turn-1",
                Status: "completed",
                AssistantDisplayText: "done",
                AssistantSpeakText: "done",
                ToolCalls:
                [
                    new VoiceToolCallInfo(
                        TurnId: "turn-1",
                        ToolName: "list-todos",
                        Step: 1,
                        ArgumentsJson: "{}",
                        Status: "ok",
                        IsMutation: false,
                        ResultSummary: "1 item",
                        Error: null)
                ],
                Error: null,
                LatencyMs: 55,
                ModelRequested: "req",
                ModelResolved: "res"));

        using var sp = UiCoreTestHost.Create(services => services.AddSingleton(client));
        var vm = sp.GetRequiredService<VoiceViewModel>();
        vm.SessionId = "session-1";
        vm.UserTranscriptText = "hello";

        var turn = await vm.SubmitTurnAsync();

        Assert.NotNull(turn);
        Assert.NotNull(vm.LastTurn);
        Assert.Equal("turn-1", vm.LastTurn!.TurnId);
        Assert.Single(vm.LastTurnToolCalls);
        Assert.Equal(string.Empty, vm.UserTranscriptText);
    }

    [Fact]
    public async Task LoadTranscriptAsync_PopulatesTranscriptItems()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.GetTranscriptAsync("session-1", Arg.Any<CancellationToken>())
            .Returns(new VoiceTranscriptInfo(
                "session-1",
                [new VoiceTranscriptEntryInfo("2026-03-01T00:00:00Z", "turn-1", "assistant", "text", "reply")]));

        using var sp = UiCoreTestHost.Create(services => services.AddSingleton(client));
        var vm = sp.GetRequiredService<VoiceViewModel>();
        vm.SessionId = "session-1";

        var transcript = await vm.LoadTranscriptAsync();

        Assert.NotNull(transcript);
        Assert.Single(vm.TranscriptItems);
        Assert.Equal("assistant", vm.TranscriptItems[0].Role);
    }

    [Fact]
    public async Task DeleteSessionAsync_ClearsSessionState()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.DeleteSessionAsync("session-1", Arg.Any<CancellationToken>()).Returns(true);

        using var sp = UiCoreTestHost.Create(services => services.AddSingleton(client));
        var vm = sp.GetRequiredService<VoiceViewModel>();
        vm.SessionId = "session-1";
        vm.UserTranscriptText = "pending";

        var deleted = await vm.DeleteSessionAsync();

        Assert.True(deleted);
        Assert.Null(vm.SessionId);
        Assert.Null(vm.LastSession);
        Assert.Null(vm.LastTurn);
        Assert.Empty(vm.TranscriptItems);
    }
}
