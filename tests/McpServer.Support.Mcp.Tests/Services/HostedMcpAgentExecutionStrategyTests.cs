using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="HostedMcpAgentExecutionStrategy"/> timeout normalization.
/// </summary>
public sealed class HostedMcpAgentExecutionStrategyTests
{
    [Fact]
    public void ResolveHostedTimeout_InfiniteTimeout_PreservesInfinity()
    {
        var timeout = HostedMcpAgentExecutionStrategy.ResolveHostedTimeout(Timeout.InfiniteTimeSpan);

        Assert.Equal(Timeout.InfiniteTimeSpan, timeout);
    }

    [Fact]
    public void ResolveHostedTimeout_ZeroTimeout_UsesHostedDefault()
    {
        var timeout = HostedMcpAgentExecutionStrategy.ResolveHostedTimeout(TimeSpan.Zero);

        Assert.Equal(TimeSpan.FromSeconds(300), timeout);
    }
}
