using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace McpServer.McpAgent;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-006: Validates hosted-agent options before the registration surface is used.
/// </summary>
public sealed partial class McpAgentOptionsValidator : IValidateOptions<McpAgentOptions>
{
    /// <summary>
    /// Validates an <see cref="McpAgentOptions"/> instance for hosted-agent registration.
    /// </summary>
    /// <param name="name">The options instance name.</param>
    /// <param name="options">The options instance being validated.</param>
    /// <returns>A validation result describing whether the configuration is acceptable.</returns>
    public ValidateOptionsResult Validate(string? name, McpAgentOptions options)
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

        if (!Enum.IsDefined(options.ExecutionProfile))
            failures.Add("ExecutionProfile must be a defined McpAgentExecutionProfile value.");

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

        if (options.ExecutionProfile == McpAgentExecutionProfile.AcidTightlyCoupled)
        {
            var definition = QBAgentDefinition.Instance;

            if (string.IsNullOrWhiteSpace(options.WorkspacePath))
                failures.Add("WorkspacePath is required for the ACID tightly coupled hosted-agent profile.");

            if (!string.Equals(options.AgentId, definition.AgentId, StringComparison.Ordinal))
                failures.Add($"AgentId must be {definition.AgentId} for the ACID tightly coupled hosted-agent profile.");

            if (!string.Equals(options.AgentName, definition.AgentName, StringComparison.Ordinal))
                failures.Add($"AgentName must be {definition.AgentName} for the ACID tightly coupled hosted-agent profile.");

            if (!string.Equals(options.SourceType, definition.SourceType, StringComparison.Ordinal))
                failures.Add($"SourceType must be {definition.SourceType} for the ACID tightly coupled hosted-agent profile.");

            if (!options.RequireAuthentication)
                failures.Add("RequireAuthentication must be true for the ACID tightly coupled hosted-agent profile.");

            if (!options.RequireSessionTurnBoundary)
                failures.Add("RequireSessionTurnBoundary must be true for the ACID tightly coupled hosted-agent profile.");

            if (!options.RequireDurableAudit)
                failures.Add("RequireDurableAudit must be true for the ACID tightly coupled hosted-agent profile.");

            if (!options.RequireTransactionalMutations)
                failures.Add("RequireTransactionalMutations must be true for the ACID tightly coupled hosted-agent profile.");

            if (!options.RequireSerializedToolInvocation)
                failures.Add("RequireSerializedToolInvocation must be true for the ACID tightly coupled hosted-agent profile.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    [GeneratedRegex("^[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SourceTypePattern();
}
