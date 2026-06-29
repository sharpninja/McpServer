using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-003: direct triage agent runner configuration tests.
/// </summary>
public sealed class ConfiguredTriageResearchRunnerTests
{
    /// <summary>
    /// TEST-MCP-TRIAGE-003: the configured runner passes the triage prompt, workspace,
    /// max run time, agent identity, model, and environment parameters to the selected
    /// direct execution strategy.
    /// </summary>
    [Fact]
    public async Task RunAsync_WithConfiguredAgent_PassesDirectAgentOptionsToStrategy()
    {
        var maxRunTime = TimeSpan.FromSeconds(42);
        var strategy = new CapturingAgentExecutionStrategy();
        var resolver = new CapturingAgentExecutionStrategyResolver(strategy);
        var runner = new ConfiguredTriageResearchRunner(
            Microsoft.Extensions.Options.Options.Create(new TriageOptions
            {
                AgentPath = "triage-agent.exe",
                AgentName = "TriageAgent",
                AgentModel = "model-triage",
                ExecutionStrategy = "fake-triage",
                MaxRunTime = maxRunTime,
                AgentParameters = new Dictionary<string, string> { ["TRIAGE_MODE"] = "1" },
            }),
            resolver);

        var result = await runner.RunAsync(new TriageResearchRequest(
            new TriageGroupDetail
            {
                GroupId = "triage-group-001",
                Status = "collecting",
                ReportCount = 1,
                WorkspacePath = "F:\\GitHub\\McpServer",
                Title = "Plugin triage bug",
                Summary = "Plugin wrapper failed",
                QuietDeadlineUtc = DateTimeOffset.UtcNow,
            },
            "{\"groupId\":\"triage-group-001\"}",
            "rendered prompt",
            "F:\\GitHub\\McpServer"));

        Assert.True(result.Success);
        Assert.Equal("""{"title":"triage result"}""", result.OutputJson);
        Assert.Equal("triage stdout", result.AgentStdout);
        Assert.Equal("triage stderr", result.AgentStderr);
        Assert.Equal(0, result.AgentExitCode);
        Assert.Equal("fake-triage", resolver.LastStrategyName);
        Assert.NotNull(strategy.LastRequest);
        Assert.Equal("rendered prompt", strategy.LastRequest.InitialPrompt);
        Assert.Equal("F:\\GitHub\\McpServer", strategy.LastRequest.WorkspacePath);
        Assert.Equal("TriageAgent", strategy.LastRequest.AgentName);
        Assert.Equal("fake-triage", strategy.LastRequest.ExecutionStrategy);
        Assert.Equal("triage-agent.exe", strategy.LastRequest.Options.AgentPath);
        Assert.Equal("model-triage", strategy.LastRequest.Options.Model);
        Assert.True(strategy.LastRequest.Options.Silent);
        Assert.Equal(maxRunTime, strategy.LastRequest.Options.Timeout);
        Assert.Equal("F:\\GitHub\\McpServer", strategy.LastRequest.Options.WorkingDirectory);
        Assert.Equal("1", strategy.LastRequest.Options.EnvironmentVariables["TRIAGE_MODE"]);
        Assert.Equal(TimeSpan.FromSeconds(5), strategy.Session.EndTimeout);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: the configured runner passes the triage output
    /// callback through to the selected direct execution strategy.
    /// </summary>
    [Fact]
    public async Task RunAsync_WithOutputCallback_PassesCallbackToStrategyOptions()
    {
        var streamed = new List<string>();
        var strategy = new CapturingAgentExecutionStrategy();
        var resolver = new CapturingAgentExecutionStrategyResolver(strategy);
        var runner = new ConfiguredTriageResearchRunner(
            Microsoft.Extensions.Options.Options.Create(new TriageOptions
            {
                AgentPath = "triage-agent.exe",
                ExecutionStrategy = "fake-triage",
            }),
            resolver);

        await runner.RunAsync(new TriageResearchRequest(
            new TriageGroupDetail
            {
                GroupId = "triage-group-001",
                Status = "collecting",
                ReportCount = 1,
                WorkspacePath = "F:\\GitHub\\McpServer",
                Title = "Plugin triage bug",
                Summary = "Plugin wrapper failed",
                QuietDeadlineUtc = DateTimeOffset.UtcNow,
            },
            "{}",
            "rendered prompt",
            "F:\\GitHub\\McpServer",
            update =>
            {
                streamed.Add($"{update.StreamName}:{update.Text}");
                return Task.CompletedTask;
            }));

        Assert.NotNull(strategy.LastRequest);
        Assert.NotNull(strategy.LastRequest.Options.AgentOutputReceivedAsync);

        await strategy.LastRequest.Options.AgentOutputReceivedAsync!("stdout", "partial output");

        Assert.Contains("stdout:partial output", streamed);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: failed direct-agent runs keep full stderr as captured
    /// output but return a concise inspectable error summary.
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenAgentTimesOut_ReturnsConciseErrorAndPreservesCapturedStderr()
    {
        var largeStderr = string.Concat(
            "normal Codex trace",
            Environment.NewLine,
            new string('x', 4096),
            Environment.NewLine,
            "error: Codex CLI triage run was cancelled or timed out.");
        var strategy = new CapturingAgentExecutionStrategy();
        strategy.Session.InitialResponse = new CopilotResult
        {
            State = CopilotResultState.Error,
            Body = string.Empty,
            Stdout = "partial stdout",
            Stderr = largeStderr,
        };
        var resolver = new CapturingAgentExecutionStrategyResolver(strategy);
        var runner = new ConfiguredTriageResearchRunner(
            Microsoft.Extensions.Options.Options.Create(new TriageOptions
            {
                AgentPath = "triage-agent.exe",
                ExecutionStrategy = "fake-triage",
            }),
            resolver);

        var result = await runner.RunAsync(new TriageResearchRequest(
            new TriageGroupDetail
            {
                GroupId = "triage-group-001",
                Status = "collecting",
                ReportCount = 1,
                WorkspacePath = "F:\\GitHub\\McpServer",
                Title = "Plugin triage bug",
                Summary = "Plugin wrapper failed",
                QuietDeadlineUtc = DateTimeOffset.UtcNow,
            },
            "{}",
            "rendered prompt",
            "F:\\GitHub\\McpServer"));

        Assert.False(result.Success);
        Assert.Equal("Codex CLI triage run was cancelled or timed out.", result.Error);
        Assert.Equal("partial stdout", result.AgentStdout);
        Assert.Equal(largeStderr, result.AgentStderr);
        Assert.True(result.Error!.Length < 200);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: missing direct-agent configuration fails without attempting
    /// to resolve or invoke an execution strategy.
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenAgentPathMissing_ReturnsConfigurationFailure()
    {
        var resolver = new CapturingAgentExecutionStrategyResolver(new CapturingAgentExecutionStrategy());
        var runner = new ConfiguredTriageResearchRunner(
            Microsoft.Extensions.Options.Options.Create(new TriageOptions { AgentPath = string.Empty }),
            resolver);

        var result = await runner.RunAsync(new TriageResearchRequest(
            new TriageGroupDetail
            {
                GroupId = "triage-group-001",
                Status = "collecting",
                ReportCount = 1,
                WorkspacePath = "F:\\GitHub\\McpServer",
                Title = "Plugin triage bug",
                Summary = "Plugin wrapper failed",
                QuietDeadlineUtc = DateTimeOffset.UtcNow,
            },
            "{}",
            "rendered prompt",
            "F:\\GitHub\\McpServer"));

        Assert.False(result.Success);
        Assert.Contains("not configured", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Null(resolver.LastStrategyName);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-003: default triage options use Codex CLI and existing
    /// Codex configuration instead of requiring a custom appsettings section.
    /// </summary>
    [Fact]
    public void TriageOptions_DefaultsToCodexCliAgent()
    {
        var options = new TriageOptions();

        Assert.Equal("triage", options.AgentName);
        Assert.Equal("codex", options.AgentPath);
        Assert.Equal("auto", options.AgentModel);
        Assert.Equal(AgentExecutionStrategyNames.CodexCli, options.ExecutionStrategy);
        Assert.True(options.MaxRunTime > TimeSpan.FromMinutes(10));
    }

    private sealed class CapturingAgentExecutionStrategyResolver(IAgentExecutionStrategy strategy) : IAgentExecutionStrategyResolver
    {
        public string? LastStrategyName { get; private set; }

        public IAgentExecutionStrategy Resolve(string? strategyName)
        {
            LastStrategyName = strategyName;
            return strategy;
        }
    }

    private sealed class CapturingAgentExecutionStrategy : IAgentExecutionStrategy
    {
        public CapturingAgentExecutionSession Session { get; } = new();

        public AgentExecutionSessionRequest? LastRequest { get; private set; }

        public string Name => "fake-triage";

        public ValueTask<IAgentExecutionSession> CreateSessionAsync(
            AgentExecutionSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return ValueTask.FromResult<IAgentExecutionSession>(Session);
        }
    }

    private sealed class CapturingAgentExecutionSession : IAgentExecutionSession
    {
        public bool IsAlive => true;

        public int? ProcessId => null;

        public TimeSpan? EndTimeout { get; private set; }

        public CopilotResult InitialResponse { get; set; } = new()
        {
            State = CopilotResultState.Success,
            Body = """{"title":"triage result"}""",
            Stdout = "triage stdout",
            Stderr = "triage stderr",
            ExitCode = 0,
        };

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task<CopilotResult> ReadInitialResponseAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(InitialResponse);

        public IAsyncEnumerable<string> ReadInitialResponseStreamingAsync(CancellationToken cancellationToken = default)
            => EmptyAsyncEnumerable();

        public Task<CopilotResult> SendAsync(string prompt, CancellationToken cancellationToken = default)
            => Task.FromResult(new CopilotResult { State = CopilotResultState.Success, Body = string.Empty });

        public IAsyncEnumerable<string> SendStreamingAsync(string prompt, CancellationToken cancellationToken = default)
            => EmptyAsyncEnumerable();

        public Task SendEscapeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EndAsync(TimeSpan timeout)
        {
            EndTimeout = timeout;
            return Task.CompletedTask;
        }

        private static async IAsyncEnumerable<string> EmptyAsyncEnumerable()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
