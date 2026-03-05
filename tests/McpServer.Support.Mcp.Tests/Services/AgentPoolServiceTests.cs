using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

public sealed class AgentPoolServiceTests
{
    [Fact]
    public async Task EnqueueOneShotAsync_DispatchesAndCompletes()
    {
        using var service = CreateService(out var voiceService);

        var enqueue = await service.EnqueueOneShotAsync(new AgentPoolOneShotRequest
        {
            Context = AgentPoolOneShotContext.AdHoc,
            PromptText = "Summarize this TODO.",
            UseWorkspaceContext = true,
        }).ConfigureAwait(true);

        Assert.True(enqueue.Success);
        Assert.False(string.IsNullOrWhiteSpace(enqueue.JobId));

        var completed = await WaitForJobStatusAsync(service, enqueue.JobId!, "completed").ConfigureAwait(true);
        Assert.Equal("completed", completed.Status);
        Assert.Equal("assistant output", completed.ResponseText);

        await voiceService.Received(1)
            .SubmitTurnAsync(Arg.Any<string>(), Arg.Any<VoiceTurnRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task CancelQueueItemAsync_QueuedItem_TransitionsToCanceled()
    {
        using var service = CreateService(out var voiceService);
        var firstTurnGate = new TaskCompletionSource<VoiceTurnResponse?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var submitCount = 0;
        voiceService.SubmitTurnAsync(Arg.Any<string>(), Arg.Any<VoiceTurnRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                submitCount++;
                if (submitCount == 1)
                    return firstTurnGate.Task;

                var sessionId = ci.ArgAt<string>(0);
                return Task.FromResult<VoiceTurnResponse?>(CreateCompletedTurn(sessionId));
            });

        var first = await service.EnqueueOneShotAsync(new AgentPoolOneShotRequest
        {
            Context = AgentPoolOneShotContext.AdHoc,
            PromptText = "First work item",
        }).ConfigureAwait(true);
        Assert.True(first.Success);
        _ = await WaitForJobStatusAsync(service, first.JobId!, "processing").ConfigureAwait(true);

        var second = await service.EnqueueOneShotAsync(new AgentPoolOneShotRequest
        {
            Context = AgentPoolOneShotContext.AdHoc,
            PromptText = "Second work item",
        }).ConfigureAwait(true);
        Assert.True(second.Success);
        _ = await WaitForJobStatusAsync(service, second.JobId!, "queued").ConfigureAwait(true);

        var cancel = await service.CancelQueueItemAsync(second.JobId!).ConfigureAwait(true);
        Assert.True(cancel.Success);

        var canceled = await WaitForJobStatusAsync(service, second.JobId!, "canceled").ConfigureAwait(true);
        Assert.Equal("canceled", canceled.Status);

        firstTurnGate.TrySetResult(CreateCompletedTurn("sess-planner"));
        _ = await WaitForJobStatusAsync(service, first.JobId!, "completed").ConfigureAwait(true);
    }

    [Fact]
    public async Task MoveQueueItemUpAsync_ReordersQueuedItems()
    {
        using var service = CreateService(out var voiceService);
        var firstTurnGate = new TaskCompletionSource<VoiceTurnResponse?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var submitCount = 0;
        voiceService.SubmitTurnAsync(Arg.Any<string>(), Arg.Any<VoiceTurnRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                submitCount++;
                if (submitCount == 1)
                    return firstTurnGate.Task;

                var sessionId = ci.ArgAt<string>(0);
                return Task.FromResult<VoiceTurnResponse?>(CreateCompletedTurn(sessionId));
            });

        var first = await service.EnqueueOneShotAsync(new AgentPoolOneShotRequest
        {
            Context = AgentPoolOneShotContext.AdHoc,
            PromptText = "First",
        }).ConfigureAwait(true);
        Assert.True(first.Success);
        _ = await WaitForJobStatusAsync(service, first.JobId!, "processing").ConfigureAwait(true);

        var second = await service.EnqueueOneShotAsync(new AgentPoolOneShotRequest
        {
            Context = AgentPoolOneShotContext.AdHoc,
            PromptText = "Second",
        }).ConfigureAwait(true);
        Assert.True(second.Success);

        var third = await service.EnqueueOneShotAsync(new AgentPoolOneShotRequest
        {
            Context = AgentPoolOneShotContext.AdHoc,
            PromptText = "Third",
        }).ConfigureAwait(true);
        Assert.True(third.Success);

        _ = await WaitForJobStatusAsync(service, second.JobId!, "queued").ConfigureAwait(true);
        _ = await WaitForJobStatusAsync(service, third.JobId!, "queued").ConfigureAwait(true);

        var moved = await service.MoveQueueItemUpAsync(third.JobId!).ConfigureAwait(true);
        Assert.True(moved.Success);

        var queue = await service.GetQueueItemsAsync().ConfigureAwait(true);
        var secondIndex = queue.ToList().FindIndex(x => string.Equals(x.JobId, second.JobId, StringComparison.OrdinalIgnoreCase));
        var thirdIndex = queue.ToList().FindIndex(x => string.Equals(x.JobId, third.JobId, StringComparison.OrdinalIgnoreCase));
        Assert.True(secondIndex >= 0);
        Assert.True(thirdIndex >= 0);
        Assert.True(thirdIndex < secondIndex);

        firstTurnGate.TrySetResult(CreateCompletedTurn("sess-planner"));
        _ = await WaitForJobStatusAsync(service, first.JobId!, "completed").ConfigureAwait(true);
    }

    [Fact]
    public async Task StartAgentAsync_WithWorkspacePath_CreatesWorkspaceScopedInstance()
    {
        using var service = CreateService(out _);

        var result = await service.StartAgentAsync("planner", @"C:\workspace-a").ConfigureAwait(true);
        Assert.True(result.Success);

        var agents = await service.GetAgentsAsync(@"C:\workspace-a").ConfigureAwait(true);
        Assert.Single(agents);
        Assert.Equal("planner", agents[0].AgentName);
        Assert.Equal(@"C:\workspace-a", agents[0].WorkspacePath);
    }

    [Fact]
    public async Task GetAgentsAsync_FiltersByWorkspace()
    {
        using var service = CreateService(out _);

        await service.StartAgentAsync("planner", @"C:\ws-a").ConfigureAwait(true);
        await service.StartAgentAsync("planner", @"C:\ws-b").ConfigureAwait(true);

        var all = await service.GetAgentsAsync().ConfigureAwait(true);
        Assert.Equal(2, all.Count);

        var wsA = await service.GetAgentsAsync(@"C:\ws-a").ConfigureAwait(true);
        Assert.Single(wsA);
        Assert.Equal(@"C:\ws-a", wsA[0].WorkspacePath);

        var wsB = await service.GetAgentsAsync(@"C:\ws-b").ConfigureAwait(true);
        Assert.Single(wsB);
        Assert.Equal(@"C:\ws-b", wsB[0].WorkspacePath);
    }

    [Fact]
    public async Task StopAgentAsync_StopsOnlyInSpecifiedWorkspace()
    {
        using var service = CreateService(out _);

        await service.StartAgentAsync("planner", @"C:\ws-a").ConfigureAwait(true);
        await service.StartAgentAsync("planner", @"C:\ws-b").ConfigureAwait(true);

        var stop = await service.StopAgentAsync("planner", @"C:\ws-a").ConfigureAwait(true);
        Assert.True(stop.Success);

        var wsA = await service.GetAgentsAsync(@"C:\ws-a").ConfigureAwait(true);
        Assert.Single(wsA);
        Assert.Equal("offline", wsA[0].Lifecycle);

        var wsB = await service.GetAgentsAsync(@"C:\ws-b").ConfigureAwait(true);
        Assert.Single(wsB);
        Assert.Equal("idle", wsB[0].Lifecycle);
    }

    [Fact]
    public async Task SeedWorkspaceAgentsAsync_CreatesInstancesForAllDefinitions()
    {
        using var service = CreateService(out _);

        await service.SeedWorkspaceAgentsAsync(@"C:\my-workspace").ConfigureAwait(true);

        var agents = await service.GetAgentsAsync(@"C:\my-workspace").ConfigureAwait(true);
        Assert.Single(agents);
        Assert.Equal("planner", agents[0].AgentName);
        Assert.Equal(@"C:\my-workspace", agents[0].WorkspacePath);
        Assert.Equal("idle", agents[0].Lifecycle);
    }

    [Fact]
    public async Task ConnectInteractiveAsync_RejectsWrongWorkspace()
    {
        using var service = CreateService(out _);

        await service.StartAgentAsync("planner", @"C:\ws-a").ConfigureAwait(true);

        var result = await service.ConnectInteractiveAsync("planner", @"C:\ws-nonexistent").ConfigureAwait(true);
        Assert.True(result.Success);

        var agents = await service.GetAgentsAsync(@"C:\ws-nonexistent").ConfigureAwait(true);
        Assert.Single(agents);
    }

    [Fact]
    public async Task EnqueueOneShotAsync_IncludesWorkspaceInQueueItem()
    {
        using var service = CreateService(out _);

        var enqueue = await service.EnqueueOneShotAsync(new AgentPoolOneShotRequest
        {
            Context = AgentPoolOneShotContext.AdHoc,
            PromptText = "Test prompt",
            WorkspacePath = @"C:\ws-test",
        }).ConfigureAwait(true);

        Assert.True(enqueue.Success);

        var completed = await WaitForJobStatusAsync(service, enqueue.JobId!, "completed").ConfigureAwait(true);
        Assert.Equal(@"C:\ws-test", completed.WorkspacePath);
    }

    private static VoiceTurnResponse CreateCompletedTurn(string sessionId)
    {
        return new VoiceTurnResponse
        {
            SessionId = sessionId,
            TurnId = "turn-0001",
            Status = "completed",
            AssistantDisplayText = "assistant output",
            AssistantSpeakText = "assistant output",
            ToolCalls = [],
            LatencyMs = 12,
            ModelRequested = "gpt-5.3-codex",
            ModelResolved = "gpt-5.3-codex",
        };
    }

    private static async Task<AgentPoolQueueItemDto> WaitForJobStatusAsync(
        AgentPoolService service,
        string jobId,
        params string[] statuses)
    {
        for (var i = 0; i < 200; i++)
        {
            var queue = await service.GetQueueItemsAsync().ConfigureAwait(true);
            var item = queue.FirstOrDefault(x => string.Equals(x.JobId, jobId, StringComparison.OrdinalIgnoreCase));
            if (item is not null && statuses.Any(s => string.Equals(s, item.Status, StringComparison.OrdinalIgnoreCase)))
                return item;

            await Task.Delay(20).ConfigureAwait(true);
        }

        throw new TimeoutException($"Timed out waiting for job '{jobId}' status [{string.Join(", ", statuses)}].");
    }

    private static AgentPoolService CreateService(out IVoiceConversationService voiceService)
    {
        voiceService = Substitute.For<IVoiceConversationService>();
        voiceService.GetStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VoiceSessionStatusDto?>(null));
        voiceService.CreateSessionAsync(Arg.Any<VoiceSessionCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var req = ci.Arg<VoiceSessionCreateRequest>() ?? new VoiceSessionCreateRequest();
                var agentName = (req.AgentName ?? "agent").ToLowerInvariant();
                return Task.FromResult(new VoiceSessionCreateResponse
                {
                    SessionId = $"sess-{agentName}",
                    Status = "idle",
                    Language = "en-US",
                    ModelRequested = req.AgentModel,
                    ModelResolved = req.AgentModel,
                });
            });
        voiceService.SubmitTurnAsync(Arg.Any<string>(), Arg.Any<VoiceTurnRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var sessionId = ci.ArgAt<string>(0);
                return Task.FromResult<VoiceTurnResponse?>(CreateCompletedTurn(sessionId));
            });

        var templateService = Substitute.For<IPromptTemplateService>();
        var todoPromptProvider = Substitute.For<ITodoPromptProvider>();
        todoPromptProvider.GetPlanPromptAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult("Plan todo {id}"));
        todoPromptProvider.GetStatusPromptAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult("Status todo {id}"));
        todoPromptProvider.GetImplementPromptAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult("Implement todo {id}"));

        var renderer = new PromptTemplateRenderer(NullLogger<PromptTemplateRenderer>.Instance);
        var workspaceAccessor = CreateWorkspaceAccessor();
        var poolOptions = new AgentPoolOptions
        {
            Enabled = true,
            MaxQueueSize = 20,
            Agents =
            [
                new AgentPoolDefinitionOptions
                {
                    AgentName = "planner",
                    AgentPath = "agent.exe",
                    AgentModel = "gpt-5.3-codex",
                    IsInteractiveDefault = true,
                    IsTodoPlanDefault = true,
                    IsTodoStatusDefault = true,
                    IsTodoImplementDefault = true,
                }
            ]
        };

        return new AgentPoolService(
            voiceService,
            templateService,
            renderer,
            todoPromptProvider,
            workspaceAccessor,
            CreateOptionsMonitor(poolOptions),
            CreateOptionsMonitor(new TodoPromptOptions { BaseUrl = "http://localhost:7147" }),
            NullLogger<AgentPoolService>.Instance);
    }

    private static WorkspaceServiceAccessor CreateWorkspaceAccessor()
    {
        var todoService = Substitute.For<ITodoService>();
        var todoFactory = Substitute.For<ITodoServiceFactory>();
        todoFactory.CreateForWorkspace(Arg.Any<string>(), Arg.Any<WorkspaceContext>()).Returns(todoService);
        var resolver = new TodoServiceResolver(
            todoService,
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = Environment.CurrentDirectory }),
            todoFactory);

        return new WorkspaceServiceAccessor(
            resolver,
            new HttpContextAccessor(),
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = Environment.CurrentDirectory }));
    }

    private static IOptionsMonitor<T> CreateOptionsMonitor<T>(T value) where T : class
    {
        var monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(value);
        monitor.Get(Arg.Any<string>()).Returns(value);
        monitor.OnChange(Arg.Any<Action<T, string?>>()).Returns(new NoopDisposable());
        return monitor;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
