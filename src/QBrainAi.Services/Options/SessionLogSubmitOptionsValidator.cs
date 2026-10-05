using Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.Options;

/// <summary>
/// TR-MCP-TRIAGESTORE-002: rejects a session-log command budget outside 1 through 300 seconds.
/// </summary>
public sealed class SessionLogSubmitOptionsValidator : IValidateOptions<SessionLogSubmitOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SessionLogSubmitOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.SubmitCommandBudgetSeconds is < SessionLogSubmitOptions.MinimumSubmitCommandBudgetSeconds
            or > SessionLogSubmitOptions.MaximumSubmitCommandBudgetSeconds)
        {
            return ValidateOptionsResult.Fail(
                $"Mcp:SessionLog:SubmitCommandBudgetSeconds must be between {SessionLogSubmitOptions.MinimumSubmitCommandBudgetSeconds} and {SessionLogSubmitOptions.MaximumSubmitCommandBudgetSeconds}.");
        }

        return ValidateOptionsResult.Success;
    }
}
