using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace McpServer.AgentFramework;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-006: Validates hosted-agent options before the registration surface is used.
/// </summary>
public sealed partial class McpAgentFrameworkOptionsValidator : IValidateOptions<McpAgentFrameworkOptions>
{
    /// <summary>
    /// Validates an <see cref="McpAgentFrameworkOptions"/> instance for hosted-agent registration.
    /// </summary>
    /// <param name="name">The options instance name.</param>
    /// <param name="options">The options instance being validated.</param>
    /// <returns>A validation result describing whether the configuration is acceptable.</returns>
    public ValidateOptionsResult Validate(string? name, McpAgentFrameworkOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.BaseUrl is null)
        {
            failures.Add("BaseUrl is required.");
        }
        else
        {
            if (!options.BaseUrl.IsAbsoluteUri)
                failures.Add("BaseUrl must be an absolute URI.");

            if (options.BaseUrl.IsAbsoluteUri
                && !string.Equals(options.BaseUrl.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(options.BaseUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("BaseUrl must use the http or https scheme.");
            }
        }

        if (options.RequireAuthentication
            && string.IsNullOrWhiteSpace(options.ApiKey)
            && string.IsNullOrWhiteSpace(options.BearerToken))
        {
            failures.Add("Either ApiKey or BearerToken must be configured when authentication is required.");
        }

        if (string.IsNullOrWhiteSpace(options.AgentId))
            failures.Add("AgentId is required.");

        if (string.IsNullOrWhiteSpace(options.AgentName))
            failures.Add("AgentName is required.");

        if (string.IsNullOrWhiteSpace(options.SourceType))
        {
            failures.Add("SourceType is required.");
        }
        else if (!SourceTypePattern().IsMatch(options.SourceType))
        {
            failures.Add("SourceType must match ^[A-Z][A-Za-z0-9]*$ so later session-log workflows can preserve canonical agent/source identifiers.");
        }

        if (options.Timeout <= TimeSpan.Zero)
            failures.Add("Timeout must be greater than zero.");

        if (options.WorkspacePath is not null)
        {
            if (string.IsNullOrWhiteSpace(options.WorkspacePath))
            {
                failures.Add("WorkspacePath cannot be empty when provided.");
            }
            else
            {
                if (!Path.IsPathFullyQualified(options.WorkspacePath))
                    failures.Add("WorkspacePath must be fully qualified when provided.");

                if (options.WorkspacePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    failures.Add("WorkspacePath contains invalid path characters.");

                try
                {
                    _ = Path.GetFullPath(options.WorkspacePath);
                }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
                {
                    failures.Add($"WorkspacePath is invalid: {ex.Message}");
                }
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    [GeneratedRegex("^[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SourceTypePattern();
}
