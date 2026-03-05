using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Requirements.Models;

namespace McpServer.Support.Mcp.Requirements;

internal static class RequirementsDocumentParser
{
    private static readonly Regex s_frEntryRegex = new(
        @"^##\s+(?<id>FR-[^\s]+)\s+(?<title>.+?)\s*\r?\n\r?\n(?<body>[\s\S]*?)(?=^##\s+FR-[^\s]+\s+|\z)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex s_trEntryRegex = new(
        @"^##\s+(?<id>TR-[^\s]+)\s*\r?\n\r?\n(?<body>[\s\S]*?)(?=^##\s+TR-[^\s]+\s*$|\z)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex s_testEntryRegex = new(
        @"^\s*-\s+(?<id>TEST-[^:\r\n]+):\s*(?<condition>.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex s_trBoldTitleRegex = new(
        @"^\*\*(?<title>.+?)\*\*\s*[—-]\s*(?<rest>.*)$",
        RegexOptions.Compiled);

    public static IReadOnlyList<FrEntry> ParseFunctional(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var list = new List<FrEntry>();
        foreach (Match match in s_frEntryRegex.Matches(content))
        {
            if (!match.Success)
                continue;

            var id = match.Groups["id"].Value.Trim();
            var title = match.Groups["title"].Value.Trim();
            var body = NormalizeBody(match.Groups["body"].Value);
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
                continue;

            list.Add(new FrEntry(id, title, body));
        }

        return list;
    }

    public static IReadOnlyList<TrEntry> ParseTechnical(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var list = new List<TrEntry>();
        foreach (Match match in s_trEntryRegex.Matches(content))
        {
            if (!match.Success)
                continue;

            var id = match.Groups["id"].Value.Trim();
            if (string.IsNullOrWhiteSpace(id))
                continue;

            var bodyRaw = NormalizeBody(match.Groups["body"].Value);
            var (title, body) = SplitTechnicalTitle(bodyRaw);
            list.Add(new TrEntry(id, title, body));
        }

        return list;
    }

    public static IReadOnlyList<TestEntry> ParseTesting(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var list = new List<TestEntry>();
        foreach (Match match in s_testEntryRegex.Matches(content))
        {
            if (!match.Success)
                continue;

            var id = match.Groups["id"].Value.Trim();
            var condition = match.Groups["condition"].Value.Trim();
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(condition))
                continue;

            list.Add(new TestEntry(id, condition));
        }

        return list;
    }

    public static IReadOnlyList<FrTrMapping> ParseMapping(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var list = new List<FrTrMapping>();
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith('|') || trimmed.Length < 3)
                continue;

            var cells = trimmed.Trim('|')
                .Split('|')
                .Select(static cell => cell.Trim())
                .ToArray();
            if (cells.Length < 2)
                continue;

            if (cells[0].Equals("FR", StringComparison.OrdinalIgnoreCase))
                continue;

            if (cells[0].StartsWith("---", StringComparison.Ordinal))
                continue;

            if (!cells[0].StartsWith("FR-", StringComparison.OrdinalIgnoreCase))
                continue;

            list.Add(new FrTrMapping(cells[0], ParseMappingTrIds(cells[1])));
        }

        return list;
    }

    private static IReadOnlyList<string> ParseMappingTrIds(string cell)
    {
        if (string.IsNullOrWhiteSpace(cell) || cell.Contains("planned", StringComparison.OrdinalIgnoreCase))
            return [];

        return cell
            .Split([','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static (string Title, string Body) SplitTechnicalTitle(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return (string.Empty, string.Empty);

        var normalized = body.Replace("\r\n", "\n", StringComparison.Ordinal);
        var firstNewLine = normalized.IndexOf('\n');
        var firstLine = firstNewLine >= 0 ? normalized[..firstNewLine].Trim() : normalized.Trim();
        var remainder = firstNewLine >= 0 ? normalized[(firstNewLine + 1)..].TrimStart('\n') : string.Empty;

        var titleMatch = s_trBoldTitleRegex.Match(firstLine);
        if (!titleMatch.Success)
            return (string.Empty, NormalizeBody(body));

        var title = titleMatch.Groups["title"].Value.Trim();
        var firstLineRemainder = titleMatch.Groups["rest"].Value.Trim();
        var rebuiltBody = string.IsNullOrWhiteSpace(remainder)
            ? firstLineRemainder
            : string.IsNullOrWhiteSpace(firstLineRemainder)
                ? remainder
                : $"{firstLineRemainder}\n{remainder}";

        return (title, NormalizeBody(rebuiltBody));
    }

    private static string NormalizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        return body
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Trim();
    }
}
