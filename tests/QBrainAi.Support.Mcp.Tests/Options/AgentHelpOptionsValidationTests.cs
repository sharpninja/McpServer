using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Options;

/// <summary>
/// TEST-MCP-HELP-004: Agent Help options validation tests.
/// </summary>
public sealed class AgentHelpOptionsValidationTests
{
    [Fact]
    public void ValidateReturnsSuccessWhenAgentHelpIsDisabled()
    {
        var validator = new AgentHelpOptionsValidator();
        var options = new AgentHelpOptions { Enabled = false, TranscriptDirectory = "" };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateFailsWhenDefaultExecutionStrategyIsUnknown()
    {
        var validator = new AgentHelpOptionsValidator();
        var options = new AgentHelpOptions
        {
            DefaultExecutionStrategy = "unknown-strategy",
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DefaultExecutionStrategy", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateReturnsSuccessWhenDefaultExecutionStrategyIsHostedMcpAgent()
    {
        var validator = new AgentHelpOptionsValidator();
        var options = new AgentHelpOptions
        {
            DefaultExecutionStrategy = AgentExecutionStrategyNames.HostedMcpAgent,
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    /// <summary>TEST-MCP-HELP-004: The grok-cli execution strategy passes options validation.</summary>
    [Fact]
    public void ValidateReturnsSuccessWhenDefaultExecutionStrategyIsGrokCli()
    {
        var validator = new AgentHelpOptionsValidator();
        var options = new AgentHelpOptions
        {
            DefaultExecutionStrategy = AgentExecutionStrategyNames.GrokCli,
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    /// <summary>FR-MCP-HELP-001: Agent Help defaults its execution strategy to grok-cli.</summary>
    [Fact]
    public void AgentHelpOptionsDefaultExecutionStrategyIsGrokCli()
    {
        var options = new AgentHelpOptions();

        Assert.Equal(AgentExecutionStrategyNames.GrokCli, options.DefaultExecutionStrategy);
    }

    [Fact]
    public void ValidateFailsWhenModelApiKeyConfiguredWithoutEnvironmentVariableName()
    {
        var validator = new AgentHelpOptionsValidator();
        var options = new AgentHelpOptions
        {
            ModelApiKey = "secret",
            ModelApiKeyEnvironmentVariableName = " ",
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ModelApiKeyEnvironmentVariableName", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateFailsWhenTranscriptDirectoryMissing()
    {
        var validator = new AgentHelpOptionsValidator();
        var options = new AgentHelpOptions
        {
            TranscriptDirectory = " ",
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("TranscriptDirectory", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateFailsWhenMaxTurnsPerSessionIsZero()
    {
        var validator = new AgentHelpOptionsValidator();
        var options = new AgentHelpOptions
        {
            MaxTurnsPerSession = 0,
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxTurnsPerSession", result.FailureMessage, StringComparison.Ordinal);
    }
}