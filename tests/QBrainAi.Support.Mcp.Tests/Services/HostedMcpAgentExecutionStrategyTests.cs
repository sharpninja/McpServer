using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="HostedMcpAgentExecutionStrategy"/> timeout normalization.
/// </summary>
public sealed class HostedMcpAgentExecutionStrategyTests
{
    [Fact]
    public void ResolveHostedTimeoutInfiniteTimeoutPreservesInfinity()
    {
        var timeout = HostedMcpAgentExecutionStrategy.ResolveHostedTimeout(Timeout.InfiniteTimeSpan);

        Assert.Equal(Timeout.InfiniteTimeSpan, timeout);
    }

    [Fact]
    public void ResolveHostedTimeoutZeroTimeoutUsesHostedDefault()
    {
        var timeout = HostedMcpAgentExecutionStrategy.ResolveHostedTimeout(TimeSpan.Zero);

        Assert.Equal(TimeSpan.FromSeconds(300), timeout);
    }
}
