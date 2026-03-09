using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Options;

public sealed class VoiceConversationOptionsValidatorTests
{
    [Fact]
    public void Validate_ReturnsSuccess_WhenVoiceIsDisabled()
    {
        var validator = new VoiceConversationOptionsValidator();
        var options = new VoiceConversationOptions { Enabled = false };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_Fails_WhenDefaultExecutionStrategyIsUnknown()
    {
        var validator = new VoiceConversationOptionsValidator();
        var options = new VoiceConversationOptions
        {
            DefaultExecutionStrategy = "unknown-strategy",
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DefaultExecutionStrategy", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenDefaultExecutionStrategyIsHostedAgentFramework()
    {
        var validator = new VoiceConversationOptionsValidator();
        var options = new VoiceConversationOptions
        {
            DefaultExecutionStrategy = AgentExecutionStrategyNames.HostedAgentFramework,
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_Fails_WhenModelApiKeyConfiguredWithoutEnvironmentVariableName()
    {
        var validator = new VoiceConversationOptionsValidator();
        var options = new VoiceConversationOptions
        {
            ModelApiKey = "secret",
            ModelApiKeyEnvironmentVariableName = " ",
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ModelApiKeyEnvironmentVariableName", result.FailureMessage, StringComparison.Ordinal);
    }
}
