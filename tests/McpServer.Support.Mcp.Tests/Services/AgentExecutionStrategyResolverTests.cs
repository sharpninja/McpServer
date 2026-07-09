using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-HELP-008, FR-MCP-HELP-011: Verifies the execution strategy resolver maps the
/// canonical grok-cli name and the grok-build alias to the registered Grok CLI strategy.
/// </summary>
public sealed class AgentExecutionStrategyResolverTests
{
    /// <summary>The resolver returns the grok strategy for the canonical grok-cli name.</summary>
    [Fact]
    public void Resolve_GrokCli_ReturnsGrokStrategy()
    {
        var resolver = new AgentExecutionStrategyResolver([new FakeStrategy(AgentExecutionStrategyNames.GrokCli)]);

        var resolved = resolver.Resolve("grok-cli");

        Assert.Equal(AgentExecutionStrategyNames.GrokCli, resolved.Name);
    }

    /// <summary>The resolver maps the grok-build alias to the registered grok-cli strategy.</summary>
    [Fact]
    public void Resolve_GrokBuildAlias_ReturnsGrokStrategy()
    {
        var resolver = new AgentExecutionStrategyResolver([new FakeStrategy(AgentExecutionStrategyNames.GrokCli)]);

        var resolved = resolver.Resolve("grok-build");

        Assert.Equal(AgentExecutionStrategyNames.GrokCli, resolved.Name);
    }

    private sealed class FakeStrategy(string name) : IAgentExecutionStrategy
    {
        public string Name { get; } = name;

        public ValueTask<IAgentExecutionSession> CreateSessionAsync(
            AgentExecutionSessionRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Session creation is not exercised in resolver tests.");
    }
}
