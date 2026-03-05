using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.Tests.TestInfrastructure;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace McpServer.UI.Core.Tests.ViewModels;

public sealed class VoiceViewModelIntegrationTests
{
    [Fact]
    public async Task FullLifecycleSequence_RoutesThroughDispatcherAndSynchronizesState()
    {
        var client = Substitute.For<IVoiceApiClient>();
        var created = new VoiceSessionInfo("session-42", "active", "en", "model-requested", "model-resolved");
        var firstTurnToolCall = new VoiceToolCallInfo(
            TurnId: "turn-1",
            ToolName: "todo.list",
            Step: 1,
            ArgumentsJson: "{}",
            Status: "ok",
            IsMutation: false,
            ResultSummary: "1 item",
            Error: null);
        var turn = new VoiceTurnInfo(
            SessionId: "session-42",
            TurnId: "turn-1",
            Status: "completed",
            AssistantDisplayText: "done",
            AssistantSpeakText: "done",
            ToolCalls: [firstTurnToolCall],
            Error: null,
            LatencyMs: 24,
            ModelRequested: "model-requested",
            ModelResolved: "model-resolved");
        var status = new VoiceSessionStatusInfo(
            SessionId: "session-42",
            Status: "active",
            Language: "en",
            CreatedUtc: "2026-03-03T00:00:00Z",
            LastUpdatedUtc: "2026-03-03T00:00:01Z",
            IsTurnActive: false,
            LastError: null,
            LastTurnId: "turn-1",
            TurnCounter: 1,
            TranscriptCount: 2);
        var transcript = new VoiceTranscriptInfo(
            "session-42",
            [
                new VoiceTranscriptEntryInfo("2026-03-03T00:00:01Z", "turn-1", "user", "text", "hello"),
                new VoiceTranscriptEntryInfo("2026-03-03T00:00:02Z", "turn-1", "assistant", "text", "done")
            ]);
        var interrupt = new VoiceInterruptInfo("session-42", Interrupted: true, Status: "interrupted");

        client.CreateSessionAsync(Arg.Any<CreateVoiceSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(created);
        client.SubmitTurnAsync(Arg.Any<SubmitVoiceTurnCommand>(), Arg.Any<CancellationToken>())
            .Returns(turn);
        client.GetStatusAsync("session-42", Arg.Any<CancellationToken>())
            .Returns(status);
        client.GetTranscriptAsync("session-42", Arg.Any<CancellationToken>())
            .Returns(transcript);
        client.InterruptAsync(Arg.Any<InterruptVoiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(interrupt);
        client.DeleteSessionAsync("session-42", Arg.Any<CancellationToken>())
            .Returns(true);

        using var sp = CreateHost(client);
        var workspaceContext = sp.GetRequiredService<WorkspaceContextViewModel>();
        workspaceContext.ActiveWorkspacePath = "/repo/current-workspace";
        var vm = sp.GetRequiredService<VoiceViewModel>();

        vm.Language = "en";
        vm.DeviceId = "device-7";
        vm.ClientName = "director";
        vm.AgentName = "assistant";
        vm.AgentModel = "model-requested";

        var createdResult = await vm.CreateSessionAsync();
        Assert.NotNull(createdResult);
        Assert.Equal("session-42", vm.SessionId);
        Assert.NotNull(vm.LastSession);
        Assert.Equal("Voice session 'session-42' created.", vm.StatusMessage);

        vm.UserTranscriptText = "hello";
        var turnResult = await vm.SubmitTurnAsync();
        Assert.NotNull(turnResult);
        Assert.NotNull(vm.LastTurn);
        Assert.Equal("turn-1", vm.LastTurn!.TurnId);
        Assert.Equal(string.Empty, vm.UserTranscriptText);
        Assert.Single(vm.LastTurnToolCalls);
        Assert.Equal("todo.list", vm.LastTurnToolCalls[0].ToolName);

        var statusResult = await vm.LoadStatusAsync();
        Assert.NotNull(statusResult);
        Assert.NotNull(vm.LastStatus);
        Assert.Equal("active", vm.LastStatus!.Status);

        var transcriptResult = await vm.LoadTranscriptAsync();
        Assert.NotNull(transcriptResult);
        Assert.Equal(2, vm.TranscriptItems.Count);
        Assert.Equal("assistant", vm.TranscriptItems[1].Role);

        var interruptResult = await vm.InterruptAsync();
        Assert.NotNull(interruptResult);
        Assert.NotNull(vm.LastInterrupt);
        Assert.True(vm.LastInterrupt!.Interrupted);

        var deleted = await vm.DeleteSessionAsync();
        Assert.True(deleted);
        Assert.Null(vm.SessionId);
        Assert.Null(vm.LastSession);
        Assert.Null(vm.LastTurn);
        Assert.Null(vm.LastStatus);
        Assert.Null(vm.LastInterrupt);
        Assert.Empty(vm.TranscriptItems);
        Assert.Empty(vm.LastTurnToolCalls);

        await client.Received(1).CreateSessionAsync(
            Arg.Is<CreateVoiceSessionCommand>(c =>
                c != null &&
                c.WorkspacePath == "/repo/current-workspace" &&
                c.Language == "en" &&
                c.DeviceId == "device-7" &&
                c.ClientName == "director" &&
                c.AgentName == "assistant" &&
                c.AgentModel == "model-requested"),
            Arg.Any<CancellationToken>());
        await client.Received(1).SubmitTurnAsync(
            Arg.Is<SubmitVoiceTurnCommand>(c =>
                c != null &&
                c.SessionId == "session-42" &&
                c.UserTranscriptText == "hello"),
            Arg.Any<CancellationToken>());
        await client.Received(1).GetStatusAsync("session-42", Arg.Any<CancellationToken>());
        await client.Received(1).GetTranscriptAsync("session-42", Arg.Any<CancellationToken>());
        await client.Received(1).InterruptAsync(
            Arg.Is<InterruptVoiceCommand>(c => c != null && c.SessionId == "session-42"),
            Arg.Any<CancellationToken>());
        await client.Received(1).DeleteSessionAsync("session-42", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSessionAsync_NormalizesPayloadAndUsesWorkspaceContext()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.CreateSessionAsync(Arg.Any<CreateVoiceSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(new VoiceSessionInfo("session-9", "active", "en", null, "model-z"));

        using var sp = CreateHost(client);
        var workspaceContext = sp.GetRequiredService<WorkspaceContextViewModel>();
        workspaceContext.ActiveWorkspacePath = "/workspaces/repo-a";
        var vm = sp.GetRequiredService<VoiceViewModel>();

        vm.Language = " en ";
        vm.DeviceId = " device-9 ";
        vm.ClientName = " client-9 ";
        vm.AgentName = " helper-agent ";
        vm.AgentModel = " model-z ";

        var created = await vm.CreateSessionAsync();

        Assert.NotNull(created);
        await client.Received(1).CreateSessionAsync(
            Arg.Is<CreateVoiceSessionCommand>(c =>
                c != null &&
                c.Language == "en" &&
                c.DeviceId == "device-9" &&
                c.ClientName == "client-9" &&
                c.WorkspacePath == "/workspaces/repo-a" &&
                c.AgentName == "helper-agent" &&
                c.AgentModel == "model-z"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSessionAsync_WhenAuthorizationFails_ReportsPermissionDenied()
    {
        var client = Substitute.For<IVoiceApiClient>();
        var auth = new ConfigurableAuthorizationPolicyService(defaultAllow: true)
            .SetAction(McpActionKeys.VoiceCreateSession, allowed: false, requiredRole: McpRoles.Admin);

        using var sp = CreateHost(client, auth);
        var vm = sp.GetRequiredService<VoiceViewModel>();

        var created = await vm.CreateSessionAsync();

        Assert.Null(created);
        Assert.Equal("Create voice session failed.", vm.StatusMessage);
        Assert.NotNull(vm.ErrorMessage);
        Assert.Contains("Permission denied", vm.ErrorMessage!, StringComparison.Ordinal);
        Assert.False(vm.IsBusy);
        await client.DidNotReceiveWithAnyArgs().CreateSessionAsync(default!, default);
    }

    [Fact]
    public async Task CreateSessionAsync_TogglesBusyWhileAwaitingClient()
    {
        var client = Substitute.For<IVoiceApiClient>();
        var pendingCreate = new TaskCompletionSource<VoiceSessionInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.CreateSessionAsync(Arg.Any<CreateVoiceSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => pendingCreate.Task);

        using var sp = CreateHost(client);
        var vm = sp.GetRequiredService<VoiceViewModel>();

        var createTask = vm.CreateSessionAsync();
        Assert.True(vm.IsBusy);

        pendingCreate.SetResult(new VoiceSessionInfo("session-busy", "active", "en", null, null));
        await createTask;

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task SubmitTurnAsync_WhenSessionMissing_UsesGuardMessageAndSkipsClient()
    {
        var client = Substitute.For<IVoiceApiClient>();

        using var sp = CreateHost(client);
        var vm = sp.GetRequiredService<VoiceViewModel>();
        vm.UserTranscriptText = "hello";

        var turn = await vm.SubmitTurnAsync();

        Assert.Null(turn);
        Assert.Equal("Set a session id first.", vm.StatusMessage);
        await client.DidNotReceiveWithAnyArgs().SubmitTurnAsync(default!, default);
    }

    [Fact]
    public async Task SubmitTurnAsync_WhenTranscriptMissing_UsesGuardMessageAndSkipsClient()
    {
        var client = Substitute.For<IVoiceApiClient>();

        using var sp = CreateHost(client);
        var vm = sp.GetRequiredService<VoiceViewModel>();
        vm.SessionId = "session-1";
        vm.UserTranscriptText = "   ";

        var turn = await vm.SubmitTurnAsync();

        Assert.Null(turn);
        Assert.Equal("Enter transcript text first.", vm.StatusMessage);
        await client.DidNotReceiveWithAnyArgs().SubmitTurnAsync(default!, default);
    }

    [Fact]
    public async Task SessionRequiredOperations_WhenSessionMissing_ReturnGuardMessagesAndSkipClientCalls()
    {
        var client = Substitute.For<IVoiceApiClient>();

        using var sp = CreateHost(client);
        var vm = sp.GetRequiredService<VoiceViewModel>();

        var interrupt = await vm.InterruptAsync();
        Assert.Null(interrupt);
        Assert.Equal("Set a session id first.", vm.StatusMessage);

        var status = await vm.LoadStatusAsync();
        Assert.Null(status);
        Assert.Equal("Set a session id first.", vm.StatusMessage);

        var transcript = await vm.LoadTranscriptAsync();
        Assert.Null(transcript);
        Assert.Equal("Set a session id first.", vm.StatusMessage);

        var deleted = await vm.DeleteSessionAsync();
        Assert.False(deleted);
        Assert.Equal("Set a session id first.", vm.StatusMessage);

        await client.DidNotReceiveWithAnyArgs().InterruptAsync(default!, default);
        await client.DidNotReceiveWithAnyArgs().GetStatusAsync(default!, default);
        await client.DidNotReceiveWithAnyArgs().GetTranscriptAsync(default!, default);
        await client.DidNotReceiveWithAnyArgs().DeleteSessionAsync(default!, default);
    }

    [Fact]
    public async Task SubmitTurnAsync_WhenClientThrows_PropagatesErrorAndResetsBusy()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.SubmitTurnAsync(Arg.Any<SubmitVoiceTurnCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<VoiceTurnInfo>(new InvalidOperationException("submit exploded")));

        using var sp = CreateHost(client);
        var vm = sp.GetRequiredService<VoiceViewModel>();
        vm.SessionId = "session-77";
        vm.UserTranscriptText = "ping";

        var turn = await vm.SubmitTurnAsync();

        Assert.Null(turn);
        Assert.Equal("Submit turn failed.", vm.StatusMessage);
        Assert.Equal("submit exploded", vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task DeleteSessionAsync_WhenClientReturnsFalse_RetainsSessionState()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.DeleteSessionAsync("session-10", Arg.Any<CancellationToken>())
            .Returns(false);

        using var sp = CreateHost(client);
        var vm = sp.GetRequiredService<VoiceViewModel>();
        vm.SessionId = "session-10";
        vm.UserTranscriptText = "pending text";
        vm.LastStatus = new VoiceSessionStatusInfo(
            SessionId: "session-10",
            Status: "active",
            Language: "en",
            CreatedUtc: "2026-03-03T00:00:00Z",
            LastUpdatedUtc: "2026-03-03T00:00:05Z",
            IsTurnActive: false,
            LastError: null,
            LastTurnId: "turn-10",
            TurnCounter: 1,
            TranscriptCount: 1);

        var deleted = await vm.DeleteSessionAsync();

        Assert.False(deleted);
        Assert.Equal("session-10", vm.SessionId);
        Assert.Equal("pending text", vm.UserTranscriptText);
        Assert.NotNull(vm.LastStatus);
        Assert.Equal("Session 'session-10' was not deleted.", vm.StatusMessage);
    }

    [Fact]
    public async Task LoadTranscriptAsync_ReplacesCollectionOnSubsequentLoads()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.GetTranscriptAsync("session-11", Arg.Any<CancellationToken>())
            .Returns(
                new VoiceTranscriptInfo(
                    "session-11",
                    [
                        new VoiceTranscriptEntryInfo("2026-03-03T00:00:00Z", "turn-1", "user", "text", "first"),
                        new VoiceTranscriptEntryInfo("2026-03-03T00:00:01Z", "turn-1", "assistant", "text", "reply")
                    ]),
                new VoiceTranscriptInfo(
                    "session-11",
                    [
                        new VoiceTranscriptEntryInfo("2026-03-03T00:00:02Z", "turn-2", "assistant", "text", "replacement")
                    ]));

        using var sp = CreateHost(client);
        var vm = sp.GetRequiredService<VoiceViewModel>();
        vm.SessionId = "session-11";

        var first = await vm.LoadTranscriptAsync();
        Assert.NotNull(first);
        Assert.Equal(2, vm.TranscriptItems.Count);

        var second = await vm.LoadTranscriptAsync();
        Assert.NotNull(second);
        Assert.Single(vm.TranscriptItems);
        Assert.Equal("turn-2", vm.TranscriptItems[0].TurnId);
        Assert.Equal("replacement", vm.TranscriptItems[0].Text);
    }

    [Fact]
    public async Task SubmitTurnAsync_ReplacesToolCallCollectionAcrossTurns()
    {
        var client = Substitute.For<IVoiceApiClient>();
        client.SubmitTurnAsync(Arg.Any<SubmitVoiceTurnCommand>(), Arg.Any<CancellationToken>())
            .Returns(
                new VoiceTurnInfo(
                    SessionId: "session-12",
                    TurnId: "turn-1",
                    Status: "completed",
                    AssistantDisplayText: "first",
                    AssistantSpeakText: "first",
                    ToolCalls:
                    [
                        new VoiceToolCallInfo("turn-1", "tool-a", 1, "{}", "ok", false, null, null),
                        new VoiceToolCallInfo("turn-1", "tool-b", 2, "{}", "ok", true, null, null)
                    ],
                    Error: null,
                    LatencyMs: 11,
                    ModelRequested: null,
                    ModelResolved: null),
                new VoiceTurnInfo(
                    SessionId: "session-12",
                    TurnId: "turn-2",
                    Status: "completed",
                    AssistantDisplayText: "second",
                    AssistantSpeakText: "second",
                    ToolCalls:
                    [
                        new VoiceToolCallInfo("turn-2", "tool-c", 1, "{}", "ok", false, null, null)
                    ],
                    Error: null,
                    LatencyMs: 12,
                    ModelRequested: null,
                    ModelResolved: null));

        using var sp = CreateHost(client);
        var vm = sp.GetRequiredService<VoiceViewModel>();
        vm.SessionId = "session-12";

        vm.UserTranscriptText = "first";
        var firstTurn = await vm.SubmitTurnAsync();
        Assert.NotNull(firstTurn);
        Assert.Equal(2, vm.LastTurnToolCalls.Count);

        vm.UserTranscriptText = "second";
        var secondTurn = await vm.SubmitTurnAsync();
        Assert.NotNull(secondTurn);
        Assert.Single(vm.LastTurnToolCalls);
        Assert.Equal("tool-c", vm.LastTurnToolCalls[0].ToolName);
    }

    [Fact]
    public void ClearSessionState_ResetsRetainedCollectionsAndProperties()
    {
        var client = Substitute.For<IVoiceApiClient>();

        using var sp = CreateHost(client);
        var vm = sp.GetRequiredService<VoiceViewModel>();
        vm.SessionId = "session-clear";
        vm.LastSession = new VoiceSessionInfo("session-clear", "active", "en", null, null);
        vm.LastTurn = new VoiceTurnInfo("session-clear", "turn-clear", "completed", null, null, [], null, 10, null, null);
        vm.LastStatus = new VoiceSessionStatusInfo(
            SessionId: "session-clear",
            Status: "active",
            Language: "en",
            CreatedUtc: "2026-03-03T00:00:00Z",
            LastUpdatedUtc: "2026-03-03T00:00:01Z",
            IsTurnActive: false,
            LastError: null,
            LastTurnId: "turn-clear",
            TurnCounter: 1,
            TranscriptCount: 1);
        vm.LastInterrupt = new VoiceInterruptInfo("session-clear", true, "interrupted");
        vm.UserTranscriptText = "unsent";
        vm.TranscriptItems.Add(new VoiceTranscriptEntryInfo("2026-03-03T00:00:00Z", "turn-clear", "assistant", "text", "reply"));
        vm.LastTurnToolCalls.Add(new VoiceToolCallInfo("turn-clear", "tool", 1, "{}", "ok", false, null, null));

        vm.ClearSessionState();

        Assert.Null(vm.SessionId);
        Assert.Null(vm.LastSession);
        Assert.Null(vm.LastTurn);
        Assert.Null(vm.LastStatus);
        Assert.Null(vm.LastInterrupt);
        Assert.Null(vm.UserTranscriptText);
        Assert.Empty(vm.TranscriptItems);
        Assert.Empty(vm.LastTurnToolCalls);
    }

    private static ServiceProvider CreateHost(
        IVoiceApiClient client,
        ConfigurableAuthorizationPolicyService? authorization = null)
    {
        return UiCoreTestHost.Create(services =>
        {
            services.AddSingleton(client);
            if (authorization is not null)
            {
                services.AddSingleton<IAuthorizationPolicyService>(authorization);
            }
        });
    }
}
