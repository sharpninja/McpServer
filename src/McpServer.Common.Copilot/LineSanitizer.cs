using System.Text.RegularExpressions;

namespace McpServer.Common.Copilot;

/// <summary>Sanitises text by replacing typographic characters with ASCII equivalents.</summary>
public static class LineSanitizer
{
    private static readonly Regex PowerShellPromptPattern = new(
        @"^PS (?<location>(?:[A-Za-z]:\\|\\\\|/|~).+>)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Replaces typographic characters that cause rendering issues in downstream
    /// consumers (terminals, SSE streams, TUI controls, databases) with their ASCII equivalents.
    /// </summary>
    public static string Sanitize(string line)
        => Sanitize(line, null);

    /// <summary>
    /// Replaces typographic characters and, when possible, rewrites the default PowerShell prompt
    /// to the supplied label so interactive agent output can identify the active model.
    /// </summary>
    public static string Sanitize(string line, string? powerShellPromptLabel)
    {
        // Em dash (U+2014) and en dash (U+2013) → ASCII hyphen-minus
        if (line.Contains('\u2014') || line.Contains('\u2013'))
        {
            line = line.Replace('\u2014', '-').Replace('\u2013', '-');
        }

        if (!string.IsNullOrWhiteSpace(powerShellPromptLabel))
        {
            var newline = line.EndsWith("\r\n", StringComparison.Ordinal)
                ? "\r\n"
                : line.EndsWith('\n')
                    ? "\n"
                    : string.Empty;
            var trimmed = newline.Length == 0
                ? line
                : line[..^newline.Length];
            var match = PowerShellPromptPattern.Match(trimmed);
            if (match.Success)
                line = $"{powerShellPromptLabel.Trim()} {match.Groups["location"].Value}{newline}";
        }

        return line;
    }
}
