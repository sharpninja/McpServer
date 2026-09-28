using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Options;

/// <summary>
/// TR-MCP-TRIAGESTORE-002: rejects a non-positive or unbounded session-log Submit command budget.
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
