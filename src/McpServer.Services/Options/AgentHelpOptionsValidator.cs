using Microsoft.Extensions.Options;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Options;

/// <summary>
/// FR-MCP-HELP-001: Validates <see cref="AgentHelpOptions"/> configuration at startup.
/// TR-MCP-HELP-001: Rejects unsupported execution strategies and incomplete API key wiring.
/// </summary>
public sealed class AgentHelpOptionsValidator : IValidateOptions<AgentHelpOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AgentHelpOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        if (!AgentExecutionStrategyNames.IsSupported(options.DefaultExecutionStrategy))
        {
            return ValidateOptionsResult.Fail(
                $"AgentHelp DefaultExecutionStrategy '{options.DefaultExecutionStrategy}' is unsupported. Supported values: {string.Join(", ", AgentExecutionStrategyNames.SupportedNames)}.");
        }

        if (!string.IsNullOrWhiteSpace(options.ModelApiKey)
            && string.IsNullOrWhiteSpace(options.ModelApiKeyEnvironmentVariableName))
        {
            return ValidateOptionsResult.Fail(
                "AgentHelp ModelApiKeyEnvironmentVariableName is required when ModelApiKey is configured.");
        }

        if (string.IsNullOrWhiteSpace(options.TranscriptDirectory))
        {
            return ValidateOptionsResult.Fail("AgentHelp TranscriptDirectory is required when Agent Help is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.IncidentDirectory))
        {
            return ValidateOptionsResult.Fail("AgentHelp IncidentDirectory is required when Agent Help is enabled.");
        }

        if (options.MaxTurnsPerSession < 1)
        {
            return ValidateOptionsResult.Fail("AgentHelp MaxTurnsPerSession must be at least 1.");
        }
        if (options.HelperTimeout <= TimeSpan.Zero || options.HelperTimeout == Timeout.InfiniteTimeSpan)
        {
            return ValidateOptionsResult.Fail("AgentHelp HelperTimeout must be a positive finite duration.");
        }

        return ValidateOptionsResult.Success;
    }
}