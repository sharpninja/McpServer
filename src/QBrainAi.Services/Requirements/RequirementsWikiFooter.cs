namespace QBrainAi.Support.Mcp.Requirements;

/// <summary>Formats and detaches the exported wiki's managed trailing Markdown footer.</summary>
/// <remarks>2026-11-26, TR-MCP-WIKIEXPORT-001 ac-WikiFooterCoverage, ac-WikiFooterValidation,
/// and ac-WikiFooterRoundTrip: keep footer text separate from canonical requirement content.</remarks>
internal static class RequirementsWikiFooter
{
    private const string Begin = "<!-- mcp-wiki-footer:begin -->";
    private const string End = "<!-- mcp-wiki-footer:end -->";

    /// <summary>Identifies reserved marker text in a template.</summary>
    internal static bool ContainsBoundary(string template) =>
        template.Contains(Begin, StringComparison.Ordinal) || template.Contains(End, StringComparison.Ordinal);

    /// <summary>Appends a bounded footer to a complete Markdown page.</summary>
    internal static string Append(string content, string footer) =>
        content.TrimEnd() + "\n\n" + Begin + "\n" + footer + "\n" + End + "\n";

    /// <summary>Detaches a complete trailing block when the manifest identifies an inline footer export.</summary>
    internal static string? Remove(string? content)
    {
        if (content is null)
            return null;

        var trimmed = content.TrimEnd();
        var begin = trimmed.LastIndexOf("\n" + Begin, StringComparison.Ordinal);
        if (begin < 0 || !trimmed.EndsWith("\n" + End, StringComparison.Ordinal))
            return content;

        var footer = trimmed.AsSpan(begin + 1 + Begin.Length, trimmed.Length - begin - 1 - Begin.Length - End.Length);
        if (footer.Length < 2 || (footer[0] is not '\r' and not '\n') ||
            footer.Contains(Begin, StringComparison.Ordinal) || footer.Contains(End, StringComparison.Ordinal))
        {
            return content;
        }

        return trimmed[..begin].TrimEnd() + "\n";
    }
}
