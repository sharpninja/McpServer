using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Options;

public sealed class VoiceConversationOptionsValidatorTests
{
    [Fact]
    public void ValidateReturnsSuccessWhenVoiceIsDisabled()
    {
        var validator = new VoiceConversationOptionsValidator();
        var options = new VoiceConversationOptions { Enabled = false };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateFailsWhenDefaultExecutionStrategyIsUnknown()
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
    public void ValidateReturnsSuccessWhenDefaultExecutionStrategyIsHostedMcpAgent()
    {
        var validator = new VoiceConversationOptionsValidator();
        var options = new VoiceConversationOptions
        {
            DefaultExecutionStrategy = AgentExecutionStrategyNames.HostedMcpAgent,
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateReturnsSuccessWhenDefaultExecutionStrategyIsOneShotCli()
    {
        var validator = new VoiceConversationOptionsValidator();
        var options = new VoiceConversationOptions
        {
            DefaultExecutionStrategy = AgentExecutionStrategyNames.OneShotCli,
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateReturnsSuccessWhenDefaultExecutionStrategyUsesLegacyHostedAgentFrameworkAlias()
    {
        var validator = new VoiceConversationOptionsValidator();
        var options = new VoiceConversationOptions
        {
            DefaultExecutionStrategy = AgentExecutionStrategyNames.HostedAgentFrameworkLegacy,
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateFailsWhenModelApiKeyConfiguredWithoutEnvironmentVariableName()
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
