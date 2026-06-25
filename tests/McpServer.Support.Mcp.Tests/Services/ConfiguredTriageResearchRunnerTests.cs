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
    /// TEST-MCP-TRIAGE-005: missing direct-agent configuration fails without attempting
    /// to resolve or invoke an execution strategy.
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenAgentPathMissing_ReturnsConfigurationFailure()
    {
        var resolver = new CapturingAgentExecutionStrategyResolver(new CapturingAgentExecutionStrategy());
        var runner = new ConfiguredTriageResearchRunner(
            Microsoft.Extensions.Options.Options.Create(new TriageOptions()),
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

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task<CopilotResult> ReadInitialResponseAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new CopilotResult { State = CopilotResultState.Success, Body = """{"title":"triage result"}""" });

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
