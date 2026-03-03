namespace McpServer.Common.Copilot;

/// <summary>Sanitises individual lines read from Copilot CLI process output.</summary>
internal static class LineSanitizer
{
    /// <summary>
    /// Replaces typographic characters that cause rendering issues in downstream
    /// consumers (terminals, SSE streams, TUI controls) with their ASCII equivalents.
    /// </summary>
    internal static string Sanitize(string line)
    {
        // Em dash (U+2014) and en dash (U+2013) → ASCII hyphen-minus
        if (line.Contains('\u2014') || line.Contains('\u2013'))
        {
            line = line.Replace('\u2014', '-').Replace('\u2013', '-');
        }

        return line;
    }
}
