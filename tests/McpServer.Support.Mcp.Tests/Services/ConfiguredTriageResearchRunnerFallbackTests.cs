using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-008: Validates the primary -&gt; secondary -&gt; tertiary triage fallback chain in
/// <see cref="ConfiguredTriageResearchRunner"/> using a scripted execution strategy keyed by agent
/// path. Validates FR-MCP-TRIAGE-006 / TR-MCP-TRIAGE-006.
/// </summary>
public sealed class ConfiguredTriageResearchRunnerFallbackTests
{
    private static AgentCliResult RateLimited(string who) =>
        new() { State = AgentCliResultState.Error, Stderr = $"{who}: 429 Too Many Requests (rate limit)", Stdout = $"{who} stdout", ExitCode = 1 };

    private static AgentCliResult Ok(string who) =>
        new() { State = AgentCliResultState.Success, Body = $$"""{"title":"{{who}} result"}""", Stdout = $"{who} stdout", Stderr = string.Empty, ExitCode = 0 };

    private static TriageResearchRequest BuildRequest() =>
        new(
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
            "F:\\GitHub\\McpServer");

    private static TriageOptions ThreeTierOptions() => new()
    {
        AgentPath = "cline",
        AgentModel = "model-primary",
        ExecutionStrategy = "fake-triage",
        Secondary = new TriageFallbackAgent { AgentPath = "grok", AgentModel = "model-grok", ExecutionStrategy = "fake-triage" },
        Tertiary = new TriageFallbackAgent { AgentPath = "claude", AgentModel = "model-claude", ExecutionStrategy = "fake-triage" },
    };

    /// <summary>TEST-MCP-TRIAGE-008: a rate-limited primary advances to the secondary (grok) which succeeds.</summary>
    [Fact]
    public async Task RunAsync_WhenPrimaryRateLimited_FallsBackToSecondary()
    {
        var strategy = new ScriptedAgentExecutionStrategy(new()
        {
            ["cline"] = RateLimited("cline"),
            ["grok"] = Ok("grok"),
        });
        var runner = new ConfiguredTriageResearchRunner(Microsoft.Extensions.Options.Options.Create(ThreeTierOptions()), new PassthroughResolver(strategy));

        var result = await runner.RunAsync(BuildRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("""{"title":"grok result"}""", result.OutputJson);
        Assert.Equal(new[] { "cline", "grok" }, strategy.InvokedAgentPaths);
    }

    /// <summary>TEST-MCP-TRIAGE-008: a rate-limited primary and secondary advance to the tertiary (claude) which succeeds.</summary>
    [Fact]
    public async Task RunAsync_WhenPrimaryAndSecondaryRateLimited_FallsBackToTertiary()
    {
        var strategy = new ScriptedAgentExecutionStrategy(new()
        {
            ["cline"] = RateLimited("cline"),
            ["grok"] = RateLimited("grok"),
            ["claude"] = Ok("claude"),
        });
        var runner = new ConfiguredTriageResearchRunner(Microsoft.Extensions.Options.Options.Create(ThreeTierOptions()), new PassthroughResolver(strategy));

        var result = await runner.RunAsync(BuildRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("""{"title":"claude result"}""", result.OutputJson);
        Assert.Equal(new[] { "cline", "grok", "claude" }, strategy.InvokedAgentPaths);
    }

    /// <summary>TEST-MCP-TRIAGE-008: when every tier is rate-limited the run fails after trying all three, preserving the last tier's stderr.</summary>
    [Fact]
    public async Task RunAsync_WhenAllTiersRateLimited_FailsAfterTryingAll()
    {
        var strategy = new ScriptedAgentExecutionStrategy(new()
        {
            ["cline"] = RateLimited("cline"),
            ["grok"] = RateLimited("grok"),
            ["claude"] = RateLimited("claude"),
        });
        var runner = new ConfiguredTriageResearchRunner(Microsoft.Extensions.Options.Options.Create(ThreeTierOptions()), new PassthroughResolver(strategy));

        var result = await runner.RunAsync(BuildRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(new[] { "cline", "grok", "claude" }, strategy.InvokedAgentPaths);
        Assert.Contains("claude", result.AgentStderr, StringComparison.Ordinal);
    }

    /// <summary>TEST-MCP-TRIAGE-008: a non-retryable primary failure returns immediately without touching the fallback tiers.</summary>
    [Fact]
    public async Task RunAsync_WhenPrimaryFailsNonRetryable_DoesNotFallBack()
    {
        var strategy = new ScriptedAgentExecutionStrategy(new()
        {
            ["cline"] = new AgentCliResult { State = AgentCliResultState.Error, Stderr = "System.NullReferenceException: object not set", ExitCode = 1 },
            ["grok"] = Ok("grok"),
        });
        var runner = new ConfiguredTriageResearchRunner(Microsoft.Extensions.Options.Options.Create(ThreeTierOptions()), new PassthroughResolver(strategy));

        var result = await runner.RunAsync(BuildRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(new[] { "cline" }, strategy.InvokedAgentPaths);
    }

    /// <summary>TEST-MCP-TRIAGE-008: a successful primary never invokes the fallback tiers.</summary>
    [Fact]
    public async Task RunAsync_WhenPrimarySucceeds_DoesNotFallBack()
    {
        var strategy = new ScriptedAgentExecutionStrategy(new()
        {
            ["cline"] = Ok("cline"),
            ["grok"] = Ok("grok"),
        });
        var runner = new ConfiguredTriageResearchRunner(Microsoft.Extensions.Options.Options.Create(ThreeTierOptions()), new PassthroughResolver(strategy));

        var result = await runner.RunAsync(BuildRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(new[] { "cline" }, strategy.InvokedAgentPaths);
    }

    /// <summary>TEST-MCP-TRIAGE-008: each tier is invoked with its own configured agent path and model.</summary>
    [Fact]
    public async Task RunAsync_PassesPerTierAgentPathAndModel()
    {
        var strategy = new ScriptedAgentExecutionStrategy(new()
        {
            ["cline"] = RateLimited("cline"),
            ["grok"] = RateLimited("grok"),
            ["claude"] = Ok("claude"),
        });
        var runner = new ConfiguredTriageResearchRunner(Microsoft.Extensions.Options.Options.Create(ThreeTierOptions()), new PassthroughResolver(strategy));

        await runner.RunAsync(BuildRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, strategy.Requests.Count);
        Assert.Equal(("cline", "model-primary"), (strategy.Requests[0].Options.AgentPath, strategy.Requests[0].Options.Model));
        Assert.Equal(("grok", "model-grok"), (strategy.Requests[1].Options.AgentPath, strategy.Requests[1].Options.Model));
        Assert.Equal(("claude", "model-claude"), (strategy.Requests[2].Options.AgentPath, strategy.Requests[2].Options.Model));
    }

    /// <summary>TEST-MCP-TRIAGE-008: a primary timeout (marker text, FallbackOnTimeout default) advances to the secondary.</summary>
    [Fact]
    public async Task RunAsync_WhenPrimaryTimesOut_FallsBackToSecondary()
    {
        var strategy = new ScriptedAgentExecutionStrategy(new()
        {
            ["cline"] = new AgentCliResult { State = AgentCliResultState.Error, Stderr = "error: One-shot CLI agent run was cancelled or timed out.", ExitCode = null },
            ["grok"] = Ok("grok"),
        });
        var runner = new ConfiguredTriageResearchRunner(Microsoft.Extensions.Options.Options.Create(ThreeTierOptions()), new PassthroughResolver(strategy));

        var result = await runner.RunAsync(BuildRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(new[] { "cline", "grok" }, strategy.InvokedAgentPaths);
    }

    /// <summary>TEST-MCP-TRIAGE-008: a disabled secondary tier (blank agent path) is skipped, advancing straight to the tertiary.</summary>
    [Fact]
    public async Task RunAsync_WhenSecondaryTierDisabled_SkipsToTertiary()
    {
        var options = ThreeTierOptions();
        options.Secondary = new TriageFallbackAgent { AgentPath = string.Empty, ExecutionStrategy = "fake-triage" };
        var strategy = new ScriptedAgentExecutionStrategy(new()
        {
            ["cline"] = RateLimited("cline"),
            ["claude"] = Ok("claude"),
        });
        var runner = new ConfiguredTriageResearchRunner(Microsoft.Extensions.Options.Options.Create(options), new PassthroughResolver(strategy));

        var result = await runner.RunAsync(BuildRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(new[] { "cline", "claude" }, strategy.InvokedAgentPaths);
    }

    private sealed class PassthroughResolver(IAgentExecutionStrategy strategy) : IAgentExecutionStrategyResolver
    {
        public IAgentExecutionStrategy Resolve(string? strategyName) => strategy;
    }

    private sealed class ScriptedAgentExecutionStrategy(Dictionary<string, AgentCliResult> resultsByAgentPath) : IAgentExecutionStrategy
    {
        private readonly Dictionary<string, AgentCliResult> _resultsByAgentPath = resultsByAgentPath;

        public List<AgentExecutionSessionRequest> Requests { get; } = [];

        public IReadOnlyList<string?> InvokedAgentPaths => Requests.ConvertAll(r => r.Options.AgentPath);

        public string Name => "fake-triage";

        public ValueTask<IAgentExecutionSession> CreateSessionAsync(
            AgentExecutionSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            var path = request.Options.AgentPath ?? string.Empty;
            var result = _resultsByAgentPath.TryGetValue(path, out var scripted)
                ? scripted
                : new AgentCliResult { State = AgentCliResultState.Error, Stderr = $"no scripted result for agent '{path}'" };
            return ValueTask.FromResult<IAgentExecutionSession>(new StaticSession(result));
        }
    }

    private sealed class StaticSession(AgentCliResult result) : IAgentExecutionSession
    {
        public bool IsAlive => false;

        public int? ProcessId => null;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task<AgentCliResult> ReadInitialResponseAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public IAsyncEnumerable<string> ReadInitialResponseStreamingAsync(CancellationToken cancellationToken = default)
            => EmptyAsyncEnumerable();

        public Task<AgentCliResult> SendAsync(string prompt, CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public IAsyncEnumerable<string> SendStreamingAsync(string prompt, CancellationToken cancellationToken = default)
            => EmptyAsyncEnumerable();

        public Task SendEscapeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EndAsync(TimeSpan timeout) => Task.CompletedTask;

        private static async IAsyncEnumerable<string> EmptyAsyncEnumerable()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
