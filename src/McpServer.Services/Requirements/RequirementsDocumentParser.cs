using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements.Models;

namespace McpServer.Support.Mcp.Requirements;

internal static class RequirementsDocumentParser
{
    private static readonly Regex s_frEntryRegex = new(
        @"^##\s+(?:\*\*)?(?<id>FR-[^\s*]+)\s+(?<title>.+?)\s*\r?\n\r?\n(?<body>[\s\S]*?)(?=^##\s+(?:\*\*)?FR-[^\s*]+\s+|\z)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex s_trEntryRegex = new(
        @"^##\s+(?:\*\*)?(?<id>TR-[^\s*]+).*?\r?\n\r?\n(?<body>[\s\S]*?)(?=^##\s+(?:\*\*)?TR-[^\s*]+.*$|\z)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex s_testEntryRegex = new(
        @"^\s*-\s+(?<id>TEST-[^:\r\n]+):\s*(?<condition>.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex s_testHeadingRegex = new(
        @"^\s*###\s+(?<id>TEST-[^\r\n]+?)\s*$",
        RegexOptions.Compiled);

    private static readonly Regex s_acceptanceCriterionRegex = new(
        @"^\s*-\s+\[(?<state>[xX\s])\]\s+(?<text>.*?)(?:\s+\(evidence:\s*(?<evidence>.*?)\))?\s*$",
        RegexOptions.Compiled);

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
            var title = CleanHeadingTitle(match.Groups["title"].Value);
            var (body, acceptanceCriteria) = SplitAcceptanceCriteria(NormalizeBody(match.Groups["body"].Value));
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
                continue;

            list.Add(new FrEntry(id, title, body, AcceptanceCriteria: acceptanceCriteria));
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
            var (title, bodyWithCriteria) = SplitTechnicalTitle(bodyRaw);
            var (body, acceptanceCriteria) = SplitAcceptanceCriteria(bodyWithCriteria);
            list.Add(new TrEntry(id, title, body, AcceptanceCriteria: acceptanceCriteria));
        }

        return list;
    }

    public static IReadOnlyList<TestEntry> ParseTesting(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var list = new List<TestEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ParseTestingSections(list, seen, content);
        ParseTestingListItems(list, seen, content);

        foreach (var line in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var cells = SplitMarkdownTableRow(line);
            if (cells.Count < 2)
                continue;

            AddTestEntry(list, seen, cells[0], DecodeWikiTableCell(cells[1]));
        }

        return list;
    }

    private static void ParseTestingSections(
        ICollection<TestEntry> entries,
        ISet<string> seen,
        string content)
    {
        var lines = NormalizeLines(content);
        for (var i = 0; i < lines.Length;)
        {
            var heading = s_testHeadingRegex.Match(lines[i]);
            if (!heading.Success)
            {
                i++;
                continue;
            }

            var id = heading.Groups["id"].Value;
            i++;
            var block = ReadTestBlock(lines, ref i);
            AddTestEntry(entries, seen, id, block.Condition, block.AcceptanceCriteria);
        }
    }

    private static void ParseTestingListItems(
        ICollection<TestEntry> entries,
        ISet<string> seen,
        string content)
    {
        var lines = NormalizeLines(content);
        for (var i = 0; i < lines.Length;)
        {
            var match = s_testEntryRegex.Match(lines[i]);
            if (!match.Success)
            {
                i++;
                continue;
            }

            var id = match.Groups["id"].Value;
            var condition = match.Groups["condition"].Value;
            i++;
            var block = ReadTestBlock(lines, ref i, condition);
            AddTestEntry(entries, seen, id, block.Condition, block.AcceptanceCriteria);
        }
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

            list.Add(new FrTrMapping(
                cells[0],
                ParseMappingIds(cells[1]),
                cells.Length >= 3 && cells[2].Contains("TEST-", StringComparison.OrdinalIgnoreCase) ? ParseMappingIds(cells[2]) : []));
        }

        return list;
    }

    private static string CleanHeadingTitle(string raw)
    {
        var title = (raw ?? string.Empty).Trim().Trim('*').Trim();
        if (title.StartsWith("—", StringComparison.Ordinal) || title.StartsWith("-", StringComparison.Ordinal))
            title = title[1..].Trim();
        return title.TrimEnd('*').Trim();
    }

    private static IReadOnlyList<string> ParseMappingIds(string cell)
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

    private static (string Body, IReadOnlyList<AcceptanceCriterion> AcceptanceCriteria) SplitAcceptanceCriteria(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return (string.Empty, []);

        var bodyLines = new List<string>();
        var criteria = new List<AcceptanceCriterion>();
        var readingCriteria = false;
        foreach (var line in NormalizeLines(body))
        {
            var trimmed = line.Trim();
            if (trimmed.Equals("**Acceptance Criteria:**", StringComparison.OrdinalIgnoreCase))
            {
                readingCriteria = true;
                continue;
            }

            if (readingCriteria)
            {
                var criterion = ParseAcceptanceCriterion(line);
                if (criterion is not null)
                    criteria.Add(criterion);
                continue;
            }

            bodyLines.Add(line);
        }

        return (NormalizeBody(string.Join('\n', bodyLines)), criteria);
    }

    private static string NormalizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        return body
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Trim();
    }

    private static TestBlock ReadTestBlock(string[] lines, ref int index, string? firstConditionLine = null)
    {
        var conditionLines = new List<string>();
        if (!string.IsNullOrWhiteSpace(firstConditionLine))
            conditionLines.Add(firstConditionLine.Trim());

        var criteria = new List<AcceptanceCriterion>();
        var readingCriteria = false;
        while (index < lines.Length)
        {
            var line = lines[index];
            var trimmed = line.Trim();
            if (IsTestBlockBoundary(line))
                break;

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                index++;
                continue;
            }

            if (trimmed.Equals("**Acceptance Criteria:**", StringComparison.OrdinalIgnoreCase))
            {
                readingCriteria = true;
                index++;
                continue;
            }

            if (readingCriteria)
            {
                var criterion = ParseAcceptanceCriterion(line);
                if (criterion is not null)
                    criteria.Add(criterion);
            }
            else
            {
                conditionLines.Add(trimmed);
            }

            index++;
        }

        return new TestBlock(string.Join("\n", conditionLines).Trim(), criteria);
    }

    private static bool IsTestBlockBoundary(string line)
    {
        if (s_testEntryRegex.IsMatch(line) || s_testHeadingRegex.IsMatch(line))
            return true;

        var trimmed = line.TrimStart();
        return trimmed.StartsWith("## ", StringComparison.Ordinal)
               || (trimmed.StartsWith('|') && trimmed.Contains("TEST-", StringComparison.OrdinalIgnoreCase));
    }

    private static AcceptanceCriterion? ParseAcceptanceCriterion(string line)
    {
        var match = s_acceptanceCriterionRegex.Match(line);
        if (!match.Success)
            return null;

        var text = match.Groups["text"].Value.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return new AcceptanceCriterion
        {
            Text = text,
            IsSatisfied = match.Groups["state"].Value.Equals("x", StringComparison.OrdinalIgnoreCase),
            Evidence = match.Groups["evidence"].Success ? match.Groups["evidence"].Value.Trim() : null
        };
    }

    private static string[] NormalizeLines(string content) =>
        content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

    private static void AddTestEntry(
        ICollection<TestEntry> entries,
        ISet<string> seen,
        string rawId,
        string rawCondition,
        IReadOnlyList<AcceptanceCriterion> criteria)
    {
        var id = rawId.Trim();
        var condition = rawCondition.Trim();
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(condition)
            || !id.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase)
            || !seen.Add(id))
        {
            return;
        }

        entries.Add(new TestEntry(id, condition, AcceptanceCriteria: criteria));
    }

    private static void AddTestEntry(
        ICollection<TestEntry> entries,
        ISet<string> seen,
        string rawId,
        string rawCondition) =>
        AddTestEntry(entries, seen, rawId, rawCondition, []);

    private static IReadOnlyList<string> SplitMarkdownTableRow(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith('|') || !trimmed.EndsWith('|'))
            return [];

        var inner = trimmed.Trim('|');
        if (inner.Contains("---", StringComparison.Ordinal) && inner.Replace("|", string.Empty, StringComparison.Ordinal).Trim('-').Length == 0)
            return [];

        const string escapedPipePlaceholder = "\uE000";
        return inner
            .Replace("\\|", escapedPipePlaceholder, StringComparison.Ordinal)
            .Split('|', StringSplitOptions.TrimEntries)
            .Select(static cell => cell.Replace(escapedPipePlaceholder, "|", StringComparison.Ordinal).Trim())
            .Where(static cell => !cell.Equals("ID", StringComparison.OrdinalIgnoreCase)
                                  && !cell.Equals("Requirement", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static string DecodeWikiTableCell(string value) =>
        value
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Trim();

    private sealed record TestBlock(string Condition, IReadOnlyList<AcceptanceCriterion> AcceptanceCriteria);
}
