using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Options;

/// <summary>
/// TEST-MCP-HELP-004: Agent Help options validation tests.
/// </summary>
public sealed class AgentHelpOptionsValidationTests
{
    [Fact]
    public void Validate_ReturnsSuccess_WhenAgentHelpIsDisabled()
    {
        var validator = new AgentHelpOptionsValidator();
        var options = new AgentHelpOptions { Enabled = false, TranscriptDirectory = "" };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_Fails_WhenDefaultExecutionStrategyIsUnknown()
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
    public void Validate_ReturnsSuccess_WhenDefaultExecutionStrategyIsHostedMcpAgent()
    {
        var validator = new AgentHelpOptionsValidator();
        var options = new AgentHelpOptions
        {
            DefaultExecutionStrategy = AgentExecutionStrategyNames.HostedMcpAgent,
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_Fails_WhenModelApiKeyConfiguredWithoutEnvironmentVariableName()
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
    public void Validate_Fails_WhenTranscriptDirectoryMissing()
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
    public void Validate_Fails_WhenMaxTurnsPerSessionIsZero()
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