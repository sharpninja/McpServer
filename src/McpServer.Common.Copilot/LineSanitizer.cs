namespace McpServer.Common.Copilot;

/// <summary>Sanitises text by replacing typographic characters with ASCII equivalents.</summary>
public static class LineSanitizer
{
    /// <summary>
    /// Replaces typographic characters that cause rendering issues in downstream
    /// consumers (terminals, SSE streams, TUI controls, databases) with their ASCII equivalents.
    /// </summary>
    public static string Sanitize(string line)
    {
        // Em dash (U+2014) and en dash (U+2013) → ASCII hyphen-minus
        if (line.Contains('\u2014') || line.Contains('\u2013'))
        {
            line = line.Replace('\u2014', '-').Replace('\u2013', '-');
        }

        return line;
    }
}
