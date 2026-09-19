using System.Text.RegularExpressions;

namespace McpServer.Common.AgentCli;

/// <summary>
/// Redacts API keys and <c>X-Api-Key</c> header values from logs and diagnostic dumps.
/// Does not preserve any key material; replacements use a fixed placeholder.
/// </summary>
public static partial class ApiKeyRedaction
{
    /// <summary>Fixed placeholder written in place of secret material.</summary>
    public const string Placeholder = "[REDACTED]";

    /// <summary>
    /// Returns <paramref name="text"/> with bearer tokens, API key assignments, and
    /// <c>X-Api-Key</c> header values replaced by <see cref="Placeholder"/>.
    /// </summary>
    /// <param name="text">Log or diagnostic text that may contain secrets.</param>
    /// <returns>The redacted text, or empty when <paramref name="text"/> is null.</returns>
    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        text = BearerRegex().Replace(text, "Bearer [REDACTED]");
        text = JsonSecretRegex().Replace(text, "${1}[REDACTED]");
        text = AssignmentRegex().Replace(text, "${1}[REDACTED]");
        return text;
    }

    /// <summary>Returns true when <paramref name="headerName"/> must never be logged in clear text.</summary>
    /// <param name="headerName">HTTP header name.</param>
    /// <returns><see langword="true"/> when the header value is a credential or cookie.</returns>
    public static bool IsSensitiveHeaderName(string? headerName)
    {
        if (string.IsNullOrWhiteSpace(headerName))
            return false;

        return headerName.Equals("X-Api-Key", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Redacts a single header value. Sensitive header names become <see cref="Placeholder"/>;
    /// other values still pass through <see cref="Redact(string?)"/>.
    /// </summary>
    /// <param name="headerName">HTTP header name.</param>
    /// <param name="headerValue">Raw header value.</param>
    /// <returns>A value safe to write to logs or diagnostic dumps.</returns>
    public static string RedactHeaderValue(string headerName, string? headerValue)
    {
        if (IsSensitiveHeaderName(headerName))
            return Placeholder;

        return Redact(headerValue);
    }

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase)]
    private static partial Regex BearerRegex();

    [GeneratedRegex(
        @"(?i)((?:x-api-key|api[_-]?key|apikey)\s*[:=]\s*)(?:""[^""\r\n]*""|'[^'\r\n]*'|[^\s;&,""']+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex AssignmentRegex();

    [GeneratedRegex(
        @"(?i)(""(?:x-api-key|api[_-]?key|apikey|apiKey)""\s*:\s*"")[^""]+",
        RegexOptions.CultureInvariant)]
    private static partial Regex JsonSecretRegex();
}
