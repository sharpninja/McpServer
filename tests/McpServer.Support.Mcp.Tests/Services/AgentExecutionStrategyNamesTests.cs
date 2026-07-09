using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-HELP-008, FR-MCP-HELP-011: Verifies the agent execution strategy name catalog,
/// including the Grok CLI strategy name, its alias, and support/normalization behavior.
/// </summary>
public sealed class AgentExecutionStrategyNamesTests
{
    /// <summary>The Grok CLI canonical name is <c>grok-cli</c>.</summary>
    [Fact]
    public void GrokCli_CanonicalName_IsGrokCli()
    {
        Assert.Equal("grok-cli", AgentExecutionStrategyNames.GrokCli);
    }

    /// <summary>The supported-name catalog includes the Grok CLI strategy.</summary>
    [Fact]
    public void SupportedNames_IncludesGrokCli()
    {
        Assert.Contains(AgentExecutionStrategyNames.GrokCli, AgentExecutionStrategyNames.SupportedNames);
    }

    /// <summary><see cref="AgentExecutionStrategyNames.IsSupported"/> accepts the canonical grok-cli name.</summary>
    [Fact]
    public void IsSupported_GrokCli_ReturnsTrue()
    {
        Assert.True(AgentExecutionStrategyNames.IsSupported("grok-cli"));
    }

    /// <summary><see cref="AgentExecutionStrategyNames.IsSupported"/> accepts the grok-build alias.</summary>
    [Fact]
    public void IsSupported_GrokBuildAlias_ReturnsTrue()
    {
        Assert.True(AgentExecutionStrategyNames.IsSupported("grok-build"));
    }

    /// <summary>The grok-build alias normalizes to the canonical grok-cli name.</summary>
    [Fact]
    public void NormalizeOrDefault_GrokBuildAlias_NormalizesToGrokCli()
    {
        Assert.Equal("grok-cli", AgentExecutionStrategyNames.NormalizeOrDefault("grok-build"));
    }

    /// <summary>The canonical grok-cli name normalizes to itself.</summary>
    [Fact]
    public void NormalizeOrDefault_GrokCli_ReturnsGrokCli()
    {
        Assert.Equal("grok-cli", AgentExecutionStrategyNames.NormalizeOrDefault("grok-cli"));
    }
}
