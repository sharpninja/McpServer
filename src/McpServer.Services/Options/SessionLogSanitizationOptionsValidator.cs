using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Options;

/// <summary>
/// TEST-MCP-SESSIONLOGSAN-001: Validates <see cref="SessionLogSanitizationOptions"/> before sanitizer rules can run.
/// </summary>
public sealed class SessionLogSanitizationOptionsValidator : IValidateOptions<SessionLogSanitizationOptions>
{
    private const int MinimumTimeoutMilliseconds = 1;
    private const int MaximumTimeoutMilliseconds = 60000;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SessionLogSanitizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxRuleCount < 0)
            return ValidateOptionsResult.Fail("SessionLogSanitization MaxRuleCount must be zero or greater.");

        if (options.MaxPatternLength <= 0)
            return ValidateOptionsResult.Fail("SessionLogSanitization MaxPatternLength must be greater than zero.");

        if (options.RegexTimeoutMilliseconds is < MinimumTimeoutMilliseconds or > MaximumTimeoutMilliseconds)
        {
            return ValidateOptionsResult.Fail(
                $"SessionLogSanitization RegexTimeoutMilliseconds must be between {MinimumTimeoutMilliseconds} and {MaximumTimeoutMilliseconds}.");
        }

        var rules = options.Rules ?? [];
        if (rules.Count > options.MaxRuleCount)
            return ValidateOptionsResult.Fail("SessionLogSanitization Rules exceeds MaxRuleCount.");

        var seenRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var timeout = TimeSpan.FromMilliseconds(options.RegexTimeoutMilliseconds);

        foreach (var rule in rules)
        {
            if (rule is null)
                return ValidateOptionsResult.Fail("SessionLogSanitization Rules cannot contain null entries.");

            if (string.IsNullOrWhiteSpace(rule.Id))
                return ValidateOptionsResult.Fail("SessionLogSanitization rule Id is required.");

            var ruleId = rule.Id.Trim();
            if (!seenRuleIds.Add(ruleId))
                return ValidateOptionsResult.Fail($"SessionLogSanitization duplicate rule Id '{ruleId}'.");

            if (string.IsNullOrWhiteSpace(rule.Pattern))
                return ValidateOptionsResult.Fail($"SessionLogSanitization rule '{ruleId}' Pattern is required.");

            if (rule.Pattern.Length > options.MaxPatternLength)
                return ValidateOptionsResult.Fail($"SessionLogSanitization rule '{ruleId}' Pattern exceeds MaxPatternLength.");

            if (!string.IsNullOrEmpty(rule.Replacement) && rule.Replacement.Contains('$'))
                return ValidateOptionsResult.Fail($"SessionLogSanitization rule '{ruleId}' Replacement cannot use regex group expansion.");

            try
            {
                _ = new Regex(rule.Pattern, RegexOptions.CultureInvariant, timeout);
            }
            catch (ArgumentException)
            {
                return ValidateOptionsResult.Fail($"SessionLogSanitization rule '{ruleId}' Pattern is invalid.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
