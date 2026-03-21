using System.Globalization;
using System.Text.RegularExpressions;

namespace McpServer.McpAgent;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Canonical helpers for hosted-agent session and request identifiers.
/// </summary>
public static partial class McpSessionIdentifiers
{
    /// <summary>
    /// Creates a canonical session identifier in the form <c>&lt;Agent&gt;-&lt;yyyyMMddTHHmmssZ&gt;-&lt;suffix&gt;</c>.
    /// </summary>
    /// <param name="sourceType">Canonical source-type prefix for the session identifier.</param>
    /// <param name="suffix">Raw suffix text that will be normalized into the canonical slug format.</param>
    /// <param name="timestampUtc">UTC timestamp to embed in the identifier.</param>
    /// <returns>A canonical session identifier.</returns>
    public static string CreateSessionId(string sourceType, string suffix, DateTimeOffset timestampUtc)
    {
        ValidateSourceType(sourceType);
        var normalizedSuffix = SanitizeSlugToken(suffix);
        var timestampToken = FormatTimestamp(timestampUtc);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{sourceType}-{timestampToken}-{normalizedSuffix}");
    }

    /// <summary>
    /// Creates a canonical request identifier in the form <c>req-&lt;yyyyMMddTHHmmssZ&gt;-&lt;slugOrOrdinal&gt;</c>.
    /// </summary>
    /// <param name="slugOrOrdinal">Raw slug or ordinal text that will be normalized into the canonical slug format.</param>
    /// <param name="timestampUtc">UTC timestamp to embed in the identifier.</param>
    /// <returns>A canonical request identifier.</returns>
    public static string CreateRequestId(string slugOrOrdinal, DateTimeOffset timestampUtc)
    {
        var normalizedSlug = SanitizeSlugToken(slugOrOrdinal);
        var timestampToken = FormatTimestamp(timestampUtc);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{McpHostedAgentDefaults.RequestIdPrefix}-{timestampToken}-{normalizedSlug}");
    }

    /// <summary>
    /// Normalizes raw identifier suffix text into the canonical lowercase slug form.
    /// </summary>
    /// <param name="value">Raw suffix text to normalize.</param>
    /// <returns>The canonical lowercase slug token.</returns>
    public static string SanitizeSlugToken(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var token = value.Trim().ToLowerInvariant();
        token = NonSlugTokenPattern().Replace(token, "-");
        token = token.Trim('-');

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException(
                "Identifier suffix must contain at least one ASCII letter or digit after sanitization.",
                nameof(value));
        }

        return token;
    }

    /// <summary>
    /// Validates a canonical session identifier against the expected source-type prefix.
    /// </summary>
    /// <param name="sessionId">Candidate session identifier.</param>
    /// <param name="expectedSourceType">Expected source-type prefix.</param>
    /// <param name="validationError">Validation error when the identifier is invalid; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the identifier is valid; otherwise <see langword="false"/>.</returns>
    public static bool TryValidateSessionId(string? sessionId, string? expectedSourceType, out string? validationError)
    {
        if (string.IsNullOrWhiteSpace(expectedSourceType))
        {
            validationError = "SourceType is required.";
            return false;
        }

        if (!SourceTypePattern().IsMatch(expectedSourceType))
        {
            validationError = "SourceType must match ^[A-Z][A-Za-z0-9]*$ so hosted-agent identifiers preserve canonical agent prefixes.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            validationError = "SessionId is required.";
            return false;
        }

        if (!SessionIdPattern().IsMatch(sessionId))
        {
            validationError = "SessionId must match <Agent>-<yyyyMMddTHHmmssZ>-<suffix> (example: Copilot-20260304T113901Z-namingconv).";
            return false;
        }

        if (!sessionId.StartsWith($"{expectedSourceType}-", StringComparison.Ordinal))
        {
            validationError = $"SessionId must start with the exact SourceType prefix '{expectedSourceType}-'.";
            return false;
        }

        validationError = null;
        return true;
    }

    /// <summary>
    /// Validates a canonical request identifier.
    /// </summary>
    /// <param name="requestId">Candidate request identifier.</param>
    /// <param name="validationError">Validation error when the identifier is invalid; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the identifier is valid; otherwise <see langword="false"/>.</returns>
    public static bool TryValidateRequestId(string? requestId, out string? validationError)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            validationError = "RequestId is required.";
            return false;
        }

        if (!RequestIdPattern().IsMatch(requestId))
        {
            validationError = "RequestId must match req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal> (example: req-20260304T113901Z-plan-namingconventions-001).";
            return false;
        }

        validationError = null;
        return true;
    }

    private static string FormatTimestamp(DateTimeOffset timestampUtc)
        => timestampUtc.ToUniversalTime().ToString(McpHostedAgentDefaults.IdentifierTimestampFormat, CultureInfo.InvariantCulture);

    private static void ValidateSourceType(string sourceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceType);

        if (!SourceTypePattern().IsMatch(sourceType))
        {
            throw new ArgumentException(
                "SourceType must match ^[A-Z][A-Za-z0-9]*$ so hosted-agent identifiers preserve canonical agent prefixes.",
                nameof(sourceType));
        }
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonSlugTokenPattern();

    [GeneratedRegex("^[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SourceTypePattern();

    [GeneratedRegex(McpHostedAgentDefaults.SessionIdPattern, RegexOptions.CultureInvariant)]
    private static partial Regex SessionIdPattern();

    [GeneratedRegex(McpHostedAgentDefaults.RequestIdPattern, RegexOptions.CultureInvariant)]
    private static partial Regex RequestIdPattern();
}
