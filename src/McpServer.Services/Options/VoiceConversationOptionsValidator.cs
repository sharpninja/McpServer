using Microsoft.Extensions.Options;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Validates <see cref="VoiceConversationOptions"/> configuration.
/// </summary>
public sealed class VoiceConversationOptionsValidator : IValidateOptions<VoiceConversationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, VoiceConversationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        if (!AgentExecutionStrategyNames.IsSupported(options.DefaultExecutionStrategy))
        {
            return ValidateOptionsResult.Fail(
                $"VoiceConversation DefaultExecutionStrategy '{options.DefaultExecutionStrategy}' is unsupported. Supported values: {string.Join(", ", AgentExecutionStrategyNames.SupportedNames)}.");
        }

        if (!string.IsNullOrWhiteSpace(options.ModelApiKey)
            && string.IsNullOrWhiteSpace(options.ModelApiKeyEnvironmentVariableName))
        {
            return ValidateOptionsResult.Fail(
                "VoiceConversation ModelApiKeyEnvironmentVariableName is required when ModelApiKey is configured.");
        }

        return ValidateOptionsResult.Success;
    }
}
