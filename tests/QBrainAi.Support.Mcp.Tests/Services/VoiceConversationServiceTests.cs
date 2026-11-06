using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Tests <see cref="VoiceConversationService"/>: session reuse, execution strategy selection, turn options, the streaming
/// error path, and (TR-MCP-USECASE-018 AC-006, appended 2026-11-06) the idle-session lifecycle and its idle decisions on
/// scripted time. Every service here is built on a <see cref="TriggeredTimerTimeProvider"/>, so its idle-cleanup timer
/// fires only where a test fires it and no case depends on the system clock or on elapsed time.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "This includes an age, expiry or staleness decision that a background callback in the code under test makes"): every fixture service now runs on a scripted clock whose timers fire only when the test fires them, and four bindings were added (the lifecycle trace of <see cref="IdleSessionLifecycleContractTests"/>, decisions D8 and D9 of <see cref="AgeBoundaryContractTests"/>, and the cleanup schedule), because hostile validation round 13 finding R13-F1 found that the session-reuse case depended on the service's real 60-second cleanup timer and the live clock. The earlier cases and their assertions are unchanged; documented in the same change (MEMORY-FACT-008).</description></item>
/// <item><term>2026-11-06</term><description>TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "A test that triggers such a callback asserts its decisions only after it has observed that the pass it triggered has run to its end"): the lifecycle pool creates the trace's sentinel in a second workspace, decision D8's binding asserts a sentinel removed by the same fired pass before each row is read, and the three sibling cases of the round 13 sweep (the two SubmitTurnAsync cases and the streaming error case) gained XML docs, because the fifteenth holistic read before hostile validation round 14 (findings d7 and g2) found that a keep decision was read without observing that the pass had finished and that those docs were missing. The earlier assertions are unchanged.</description></item>
/// </list>
/// </remarks>
public sealed class VoiceConversationServiceTests
{
    /// <summary>TR-MCP-USECASE-018 AC-006: the fixed UTC instant the fixture clock starts at when a test supplies none.</summary>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added with the scripted fixture clock (hostile validation round 13 finding R13-F1).</remarks>
    private static readonly DateTimeOffset FixtureStart = new(2001, 2, 3, 4, 5, 6, TimeSpan.Zero);

    /// <summary>Two creates for the same agent and workspace reuse the idle session.</summary>
    /// <returns>A task that completes when the check finishes.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: documented, and now on the scripted fixture clock, so no cleanup can run between the two creates (hostile validation round 13 finding R13-F1); the assertion is unchanged.</remarks>
    [Fact]
    public async Task CreateSessionAsync_SameAgentAndWorkspace_ReusesExistingIdleSession()
    {
        using var service = CreateService();

        var first = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            AgentModel = "gpt-5.3-codex",
            WorkspacePath = @"E:\ws-a",
            OneShotSession = false,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var second = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            AgentModel = "gpt-5.3-codex",
            WorkspacePath = @"E:\ws-a",
            OneShotSession = false,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(first.SessionId, second.SessionId);
    }

    [Fact]
    public async Task CreateSessionAsync_SameAgentDifferentWorkspace_DoesNotReuseSession()
    {
        using var service = CreateService();

        var first = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            AgentModel = "gpt-5.3-codex",
            WorkspacePath = @"E:\ws-a",
            OneShotSession = false,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var second = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            AgentModel = "gpt-5.3-codex",
            WorkspacePath = @"E:\ws-b",
            OneShotSession = false,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotEqual(first.SessionId, second.SessionId);
    }

    [Fact]
    public async Task SendSessionMessageAsync_OneShotSession_ReturnsFalse()
    {
        using var service = CreateService();
        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            OneShotSession = true,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var sent = await service.SendSessionMessageAsync(created.SessionId, "User is here.", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(sent);
    }

    [Fact]
    public async Task CreateSessionAsync_UsesConfiguredDefaultExecutionStrategy_WhenRequestOmitsOne()
    {
        using var service = CreateService(defaultExecutionStrategy: AgentExecutionStrategyNames.HostedMcpAgent);

        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(AgentExecutionStrategyNames.HostedMcpAgent, created.ExecutionStrategy);
    }

    [Fact]
    public async Task CreateSessionAsync_ExplicitExecutionStrategy_OverridesConfiguredDefault()
    {
        using var service = CreateService(defaultExecutionStrategy: AgentExecutionStrategyNames.HostedMcpAgent);

        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            ExecutionStrategy = AgentExecutionStrategyNames.CopilotCli,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(AgentExecutionStrategyNames.CopilotCli, created.ExecutionStrategy);
    }

    /// <summary>
    /// A turn submitted to a hosted-strategy session passes the configured model API key to the execution options under
    /// the configured environment variable name.
    /// </summary>
    /// <returns>A task that completes when the check finishes.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: documented; since slice Z12 the service runs on the scripted fixture clock, so no cleanup can remove the session between create and submit (a sibling of hostile validation round 13 finding R13-F1), and the docs were added because the fifteenth holistic read before hostile validation round 14 (finding g2) found them missing; the assertions are unchanged.</remarks>
    [Fact]
    public async Task SubmitTurnAsync_IncludesConfiguredModelApiKeyInExecutionOptions()
    {
        var hostedStrategy = new CapturingAgentExecutionStrategy(AgentExecutionStrategyNames.HostedMcpAgent);
        using var service = CreateService(
            defaultExecutionStrategy: AgentExecutionStrategyNames.HostedMcpAgent,
            modelApiKey: "voice-model-key",
            modelApiKeyEnvironmentVariableName: "ANTHROPIC_API_KEY",
            hostedStrategy: hostedStrategy);

        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var response = await service.SubmitTurnAsync(created.SessionId, new VoiceTurnRequest
        {
            UserTranscriptText = "hello",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(response);
        Assert.Equal("completed", response!.Status);
        Assert.NotNull(hostedStrategy.LastRequest);
        Assert.Equal(
            "voice-model-key",
            hostedStrategy.LastRequest!.Options.EnvironmentVariables["ANTHROPIC_API_KEY"]);
    }

    /// <summary>A turn submitted to a hosted-strategy session passes an infinite timeout to the execution options.</summary>
    /// <returns>A task that completes when the check finishes.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: documented; since slice Z12 the service runs on the scripted fixture clock, so no cleanup can remove the session between create and submit (a sibling of hostile validation round 13 finding R13-F1), and the docs were added because the fifteenth holistic read before hostile validation round 14 (finding g2) found them missing; the assertions are unchanged.</remarks>
    [Fact]
    public async Task SubmitTurnAsync_UsesInfiniteCopilotTimeoutInExecutionOptions()
    {
        var hostedStrategy = new CapturingAgentExecutionStrategy(AgentExecutionStrategyNames.HostedMcpAgent);
        using var service = CreateService(
            defaultExecutionStrategy: AgentExecutionStrategyNames.HostedMcpAgent,
            hostedStrategy: hostedStrategy);

        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var response = await service.SubmitTurnAsync(created.SessionId, new VoiceTurnRequest
        {
            UserTranscriptText = "hello",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(response);
        Assert.NotNull(hostedStrategy.LastRequest);
        Assert.Equal(Timeout.InfiniteTimeSpan, hostedStrategy.LastRequest!.Options.Timeout);
    }

    /// <summary>
    /// A streamed turn whose runtime returns an empty stream emits one terminal error event, sets the session status to
    /// error with the runtime message, and records the user entry and the assistant error entry in the transcript.
    /// </summary>
    /// <returns>A task that completes when the check finishes.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: documented; since slice Z12 the service runs on the scripted fixture clock, so no cleanup can remove the session between create and the streamed turn (a sibling of hostile validation round 13 finding R13-F1), and the docs were added because the fifteenth holistic read before hostile validation round 14 (finding g2) found them missing; the assertions are unchanged.</remarks>
    [Fact]
    public async Task SubmitTurnStreamingAsync_EmptyRuntimeStream_EmitsErrorAndRecordsAssistantError()
    {
        using var service = CreateService(defaultExecutionStrategy: AgentExecutionStrategyNames.HostedMcpAgent);
        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            WorkspacePath = @"E:\ws-a",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var events = await CollectAsync(service.SubmitTurnStreamingAsync(created.SessionId, new VoiceTurnRequest
        {
            UserTranscriptText = "Read the file .github/copilot-instructions.md and follow those instructions.",
        }, cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);

        var terminal = Assert.Single(events, static evt => evt.Type is "done" or "error");
        Assert.Equal("error", terminal.Type);
        Assert.Contains("No response returned from voice runtime", terminal.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(events, static evt => evt.Type == "done" && evt.Status == "completed");

        var status = await service.GetStatusAsync(created.SessionId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(status);
        Assert.Equal("error", status!.Status);
        Assert.Contains("No response returned from voice runtime", status.LastError, StringComparison.Ordinal);
        Assert.Equal(2, status.TranscriptCount);

        var transcript = await service.GetTranscriptAsync(created.SessionId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(transcript);
        Assert.Equal(2, transcript!.Items.Count);
        Assert.Contains(transcript.Items, static item => item.Role == "user");
        var assistant = Assert.Single(transcript.Items, static item => item.Role == "assistant");
        Assert.Equal("error", assistant.Category);
        Assert.Contains("No response returned from voice runtime", assistant.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AgentCliInteractiveSession_ReadInitialResponseStreamingAsync_StdoutClosesAfterNonzeroExit_ThrowsDiagnostic()
    {
        const string stderr = "GitHub Copilot CLI requires Node.js v24 or higher.";
        var process = new FakeSpawnedProcess(stdout: string.Empty, stderr: stderr, exitCode: 1);
        var spawner = new FakeProcessSpawner(process);
        var processEnvironment = Substitute.For<IProcessEnvironmentService>();
        processEnvironment
            .ResolveExecutable(Arg.Any<System.Diagnostics.ProcessStartInfo>(), Arg.Any<string>())
            .Returns(call => call.ArgAt<string>(1));
        var client = new AgentCliClient(
            CreateOptionsMonitor(new AgentCliClientOptions
            {
                AgentPath = "copilot",
                Model = "auto",
                WorkingDirectory = AppContext.BaseDirectory,
                Timeout = Timeout.InfiniteTimeSpan,
            }),
            processEnvironment,
            spawner,
            NullLogger<AgentCliClient>.Instance);

        await using var session = client.CreateInteractiveSession("hello");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in session.ReadInitialResponseStreamingAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true))
            {
            }
        }).ConfigureAwait(true);

        Assert.Contains("CLI agent exited with code 1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Node.js v24", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 binding of <see cref="IdleSessionLifecycleContractTests.AssertLifecycleAsync"/>: on the
    /// production service, two creates reuse one session while its scripted idle age is eligible, across a cleanup the
    /// test triggers between them; no cleanup runs unless the test fires the service's timer, even past the threshold; a
    /// fired cleanup keeps a session exactly at the strict 15-minute threshold and removes it one second past it; and a
    /// create after the removal gives a new session. The keep decision after the fire at the threshold is read only once
    /// the same fired pass is seen to have removed the trace's sentinel, a session in a second workspace one second
    /// older. Run at a reference in 2001 and one in 2041.
    /// </summary>
    /// <returns>A task that completes when the trace finishes.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added as the reviewer's proposed test of hostile validation round 13 finding R13-F1. 2026-11-06, TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "A test that triggers such a callback asserts its decisions only after it has observed that the pass it triggered has run to its end"): the bound trace now includes the sentinel step (fifteenth holistic read before hostile validation round 14, finding d7).</remarks>
    [Fact]
    public Task VoiceSessionReuse_DoesNotDependOnWallClockOrCleanupScheduling() =>
        IdleSessionLifecycleContractTests.AssertLifecycleAsync(reference => new ServiceSessionPool(reference));

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 binding of decision D8 of <see cref="AgeBoundaryContractTests"/>: the cleanup the
    /// service's timer runs removes a session whose recorded idle time is more than the 15-minute timeout before the
    /// scripted reference, and keeps one exactly at it or one second short of it, at a reference in 2001 and one in 2041.
    /// Each row's decision is read only after the same fired pass is seen to have run to its end: a sentinel session in a
    /// second workspace, two seconds past the timeout, is already removed.
    /// </summary>
    /// <returns>A task that completes when every row has been asserted.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added as the reviewer's proposed test of hostile validation round 13 finding R13-F1. 2026-11-06, TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "A test that triggers such a callback asserts its decisions only after it has observed that the pass it triggered has run to its end"): each row now creates the sentinel first and asserts it removed after the fire, before the row's decision is read, because the fifteenth holistic read before hostile validation round 14 (finding d7) found that a pass still running when the row was read could make a keep row pass.</remarks>
    [Fact]
    public Task IdleCleanup_UsesScriptedAgeBoundary() =>
        AgeBoundaryContractTests.AssertDecisionAsync("D8 voice idle-session cleanup", async (reference, recorded) =>
        {
            var sentinelRecorded = reference - IdleSessionLifecycleContractTests.IdleTimeout - AgeBoundaryContractTests.Step - AgeBoundaryContractTests.Step;
            var clock = new TriggeredTimerTimeProvider(sentinelRecorded);
            using var service = CreateService(time: clock);
            var sentinel = await service.CreateSessionAsync(SentinelRequest(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            clock.Set(recorded);
            var created = await service.CreateSessionAsync(ReuseRequest(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            clock.Set(reference);
            Assert.Single(clock.Timers).Fire();
            var sentinelTranscript = await service.GetTranscriptAsync(sentinel.SessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(sentinelTranscript is null, $"At {reference:O}: the fired pass has run to its end before the row is read: the sentinel, two seconds past the timeout, is already removed.");
            var transcript = await service.GetTranscriptAsync(created.SessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            return transcript is null;
        });

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 binding of decision D9 of <see cref="AgeBoundaryContractTests"/>: a device lookup skips a
    /// session whose recorded idle time is more than the 15-minute timeout before the scripted reference, and finds one
    /// exactly at it or one second short of it, with no cleanup fired.
    /// </summary>
    /// <returns>A task that completes when every row has been asserted.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added for the second idle decision of the service (hostile validation round 13 finding R13-F1).</remarks>
    [Fact]
    public Task FindSessionByDevice_UsesScriptedAgeBoundary() =>
        AgeBoundaryContractTests.AssertDecisionAsync("D9 voice device-session idle skip", async (reference, recorded) =>
        {
            var clock = new TriggeredTimerTimeProvider(recorded);
            using var service = CreateService(time: clock);
            await service.CreateSessionAsync(new VoiceSessionCreateRequest
            {
                AgentName = "planner",
                DeviceId = "device-1",
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            clock.Set(reference);
            var found = service.FindSessionByDevice("device-1");
            Assert.Equal(0, Assert.Single(clock.Timers).FireCount);
            return found is null;
        });

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006: the service arms exactly one idle-cleanup timer through its clock, with the production
    /// schedule (first run after 60 seconds, then every 60 seconds), never fires it on its own, and disposes it.
    /// </summary>
    /// <returns>A task that completes when the check finishes.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added so the binding of hostile validation round 13 finding R13-F1 pins the schedule the production code keeps.</remarks>
    [Fact]
    public async Task IdleCleanupTimer_IsArmedThroughTheClockWithTheProductionSchedule()
    {
        var clock = new TriggeredTimerTimeProvider(FixtureStart);
        using var service = CreateService(time: clock);
        await service.CreateSessionAsync(ReuseRequest(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        clock.Advance(TimeSpan.FromDays(1));

        var timer = Assert.Single(clock.Timers);
        Assert.Equal(TimeSpan.FromSeconds(60), VoiceConversationService.IdleCleanupInterval);
        Assert.Equal(TimeSpan.FromSeconds(60), timer.DueTime);
        Assert.Equal(TimeSpan.FromSeconds(60), timer.Period);
        Assert.Equal(0, timer.FireCount);
        Assert.False(timer.IsDisposed);

        service.Dispose();
        Assert.True(timer.IsDisposed);
    }

    /// <summary>TR-MCP-USECASE-018 AC-006: the create request the reuse cases send (one fixed agent and workspace).</summary>
    /// <returns>The request.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added for the bindings of hostile validation round 13 finding R13-F1; the same values as the reuse case.</remarks>
    private static VoiceSessionCreateRequest ReuseRequest() => new()
    {
        AgentName = "planner",
        AgentModel = "gpt-5.3-codex",
        WorkspacePath = @"E:\ws-a",
        OneShotSession = false,
    };

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006: the create request of the sentinel session, the reuse agent in a second workspace, so no
    /// reuse case reuses it and a fired cleanup pass that removed it has run to its end.
    /// </summary>
    /// <returns>The request.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "A test that triggers such a callback asserts its decisions only after it has observed that the pass it triggered has run to its end"): added for the fifteenth holistic read before hostile validation round 14, finding d7.</remarks>
    private static VoiceSessionCreateRequest SentinelRequest() => new()
    {
        AgentName = "planner",
        AgentModel = "gpt-5.3-codex",
        WorkspacePath = @"E:\ws-sentinel",
        OneShotSession = false,
    };

    /// <summary>Builds the service under test with fake strategies and the scripted clock.</summary>
    /// <param name="defaultExecutionStrategy">The configured default execution strategy.</param>
    /// <param name="modelApiKey">The configured model API key.</param>
    /// <param name="modelApiKeyEnvironmentVariableName">The variable name the key is passed under.</param>
    /// <param name="hostedStrategy">The hosted strategy, or null for a fake.</param>
    /// <param name="oneShotStrategy">The one-shot strategy, or null for a fake.</param>
    /// <param name="time">The service's clock, or null for a <see cref="TriggeredTimerTimeProvider"/> at <see cref="FixtureStart"/>.</param>
    /// <returns>The service.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added the <paramref name="time"/> parameter, defaulting to a scripted clock whose timers fire only when a test fires them, and the idle timeout now comes from <see cref="IdleSessionLifecycleContractTests.IdleTimeout"/> (15 minutes, unchanged), because hostile validation round 13 finding R13-F1 found that every service here armed a real cleanup timer; documented in the same change (MEMORY-FACT-008).</remarks>
    private static VoiceConversationService CreateService(
        string defaultExecutionStrategy = AgentExecutionStrategyNames.OneShotCli,
        string? modelApiKey = null,
        string modelApiKeyEnvironmentVariableName = "OPENAI_API_KEY",
        IAgentExecutionStrategy? hostedStrategy = null,
        IAgentExecutionStrategy? oneShotStrategy = null,
        TimeProvider? time = null)
    {
        var copilotClient = Substitute.For<IAgentCliClient>();
        var workspaceAccessor = CreateWorkspaceAccessor();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(AppContext.BaseDirectory);
        hostEnvironment.EnvironmentName.Returns("Test");
        hostEnvironment.ApplicationName.Returns("QBrainAi.Support.Mcp.Tests");
        var strategyResolver = new AgentExecutionStrategyResolver(
        [
            new CopilotCliAgentExecutionStrategy(copilotClient),
            oneShotStrategy ?? new FakeAgentExecutionStrategy(AgentExecutionStrategyNames.OneShotCli),
            hostedStrategy ?? new FakeAgentExecutionStrategy(AgentExecutionStrategyNames.HostedMcpAgent),
        ]);
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        var issueSyncService = Substitute.For<IIssueTodoSyncService>();
        var services = new ServiceCollection();
        services.AddSingleton<IAgentExecutionStrategyResolver>(strategyResolver);
        services.AddSingleton(gitHubCliService);
        services.AddSingleton(issueSyncService);
        services.AddSingleton(new TodoCreationService(workspaceAccessor, gitHubCliService, NullLogger<TodoCreationService>.Instance));
        services.AddSingleton(new TodoUpdateService(workspaceAccessor, issueSyncService, NullLogger<TodoUpdateService>.Instance));
        var serviceProvider = services.BuildServiceProvider();

        return new VoiceConversationService(
            copilotClient,
            serviceProvider,
            workspaceAccessor,
            configuration,
            CreateOptionsMonitor(new VoiceConversationOptions
            {
                Enabled = true,
                CopilotModel = "gpt-5.3-codex",
                DefaultExecutionStrategy = defaultExecutionStrategy,
                ModelApiKey = modelApiKey,
                ModelApiKeyEnvironmentVariableName = modelApiKeyEnvironmentVariableName,
                SessionIdleTimeoutMinutes = IdleSessionLifecycleContractTests.IdleTimeout,
            }),
            CreateOptionsMonitor(new TodoPromptOptions { BaseUrl = "http://localhost:7147" }),
            hostEnvironment,
            NullLogger<VoiceConversationService>.Instance,
            NullLoggerFactory.Instance,
            time ?? new TriggeredTimerTimeProvider(FixtureStart));
    }

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006: the production service as a pool of <see cref="IdleSessionLifecycleContractTests"/>:
    /// the scripted clock is a <see cref="TriggeredTimerTimeProvider"/>, a create sends <see cref="ReuseRequest"/>, a
    /// session exists while its transcript can be read (a read that does not refresh its idle time), and a triggered
    /// cleanup fires the service's one cleanup timer.
    /// </summary>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added for the lifecycle binding of hostile validation round 13 finding R13-F1. 2026-11-06, TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "A test that triggers such a callback asserts its decisions only after it has observed that the pass it triggered has run to its end"): the sentinel create was added, sending <see cref="SentinelRequest"/>, for the fifteenth holistic read before hostile validation round 14, finding d7.</remarks>
    private sealed class ServiceSessionPool : IdleSessionLifecycleContractTests.IIdleSessionPool
    {
        /// <summary>The service's scripted clock.</summary>
        private readonly TriggeredTimerTimeProvider _clock;

        /// <summary>The service under test.</summary>
        private readonly VoiceConversationService _service;

        /// <summary>Creates the service with its clock at <paramref name="start"/>.</summary>
        /// <param name="start">The scripted start instant.</param>
        internal ServiceSessionPool(DateTimeOffset start)
        {
            _clock = new TriggeredTimerTimeProvider(start);
            _service = CreateService(time: _clock);
        }

        /// <inheritdoc />
        public void SetClock(DateTimeOffset utcNow) => _clock.Set(utcNow);

        /// <inheritdoc />
        public async Task<string> CreateAsync() =>
            (await _service.CreateSessionAsync(ReuseRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true)).SessionId;

        /// <inheritdoc />
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added; the sentinel is a session for the same agent in a second workspace, which the fixed session never reuses (fifteenth holistic read before hostile validation round 14, finding d7).</remarks>
        public async Task<string> CreateSentinelAsync() =>
            (await _service.CreateSessionAsync(SentinelRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true)).SessionId;

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string sessionId) =>
            await _service.GetTranscriptAsync(sessionId, TestContext.Current.CancellationToken).ConfigureAwait(true) is not null;

        /// <inheritdoc />
        public void TriggerCleanup() => Assert.Single(_clock.Timers).Fire();

        /// <inheritdoc />
        public void Dispose() => _service.Dispose();
    }

    private static WorkspaceServiceAccessor CreateWorkspaceAccessor()
    {
        var todoService = Substitute.For<ITodoService>();
        var todoFactory = Substitute.For<ITodoServiceFactory>();
        todoFactory.CreateForWorkspace(Arg.Any<string>(), Arg.Any<WorkspaceContext>()).Returns(todoService);
        var resolver = new TodoServiceResolver(
            todoService,
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = AppContext.BaseDirectory }),
            todoFactory);

        return new WorkspaceServiceAccessor(
            resolver,
            new HttpContextAccessor(),
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = AppContext.BaseDirectory }));
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

    private sealed class FakeAgentExecutionStrategy(string name) : IAgentExecutionStrategy
    {
        public string Name { get; } = name;

        public ValueTask<IAgentExecutionSession> CreateSessionAsync(
            AgentExecutionSessionRequest request,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IAgentExecutionSession>(new FakeAgentExecutionSession());
    }

    private sealed class CapturingAgentExecutionStrategy(string name) : IAgentExecutionStrategy
    {
        public AgentExecutionSessionRequest? LastRequest { get; private set; }

        public string Name { get; } = name;

        public ValueTask<IAgentExecutionSession> CreateSessionAsync(
            AgentExecutionSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return ValueTask.FromResult<IAgentExecutionSession>(new FakeAgentExecutionSession());
        }
    }

    private sealed class FakeAgentExecutionSession : IAgentExecutionSession
    {
        private const string FinalResponseBody = """{"type":"final_response","displayText":"done","speakText":"done"}""";

        public bool IsAlive => true;

        public int? ProcessId => null;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task EndAsync(TimeSpan timeout) => Task.CompletedTask;

        public Task<AgentCliResult> ReadInitialResponseAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentCliResult { Body = FinalResponseBody, State = AgentCliResultState.Success });

        public IAsyncEnumerable<string> ReadInitialResponseStreamingAsync(CancellationToken cancellationToken = default) =>
            EmptyAsyncEnumerable();

        public Task SendEscapeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<AgentCliResult> SendAsync(string prompt, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentCliResult { Body = FinalResponseBody, State = AgentCliResultState.Success });

        public IAsyncEnumerable<string> SendStreamingAsync(string prompt, CancellationToken cancellationToken = default) =>
            EmptyAsyncEnumerable();

        private static async IAsyncEnumerable<string> EmptyAsyncEnumerable()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> stream)
    {
        var items = new List<T>();
        await foreach (var item in stream.ConfigureAwait(true))
            items.Add(item);
        return items;
    }

    private sealed class FakeProcessSpawner(ISpawnedProcess process) : IProcessSpawner
    {
        public ISpawnedProcess Spawn(System.Diagnostics.ProcessStartInfo startInfo) => process;
    }

    private sealed class FakeSpawnedProcess : ISpawnedProcess
    {
        private readonly MemoryStream _stdout;
        private readonly MemoryStream _stderr;
        private readonly MemoryStream _stdin = new();

        public FakeSpawnedProcess(string stdout, string stderr, int exitCode)
        {
            _stdout = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(stdout));
            _stderr = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(stderr));
            StandardOutput = new StreamReader(_stdout);
            StandardError = new StreamReader(_stderr);
            StandardInput = new StreamWriter(_stdin);
            ExitCode = exitCode;
        }

        public StreamReader StandardOutput { get; }

        public StreamReader StandardError { get; }

        public StreamWriter? StandardInput { get; }

        public int Id => 1234;

        public bool HasExited => true;

        public int ExitCode { get; }

        public void Dispose()
        {
            StandardInput?.Dispose();
            StandardOutput.Dispose();
            StandardError.Dispose();
            _stdin.Dispose();
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Kill()
        {
        }
    }
}

