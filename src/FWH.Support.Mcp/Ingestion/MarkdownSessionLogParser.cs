using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FWH.Support.Mcp.Models;

namespace FWH.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-013: Parses Markdown session log files into UnifiedSessionLogDto.
/// FR-SUPPORT-010: Enables ingestion of legacy Markdown session logs alongside JSON.
/// </summary>
internal sealed partial class MarkdownSessionLogParser
{
    /// <summary>Well-known section names in session log Markdown files.</summary>
    private static readonly string[] KnownSections =
    [
        "Session Overview", "Changes Made", "Technical Requirements",
        "Testing", "Documentation", "Files Summary",
        "Deployment", "Verification", "Commit Strategy", "Success Criteria"
    ];

    /// <summary>TR-PLANNED-013: Attempt to parse a Markdown file as a session log.</summary>
    /// <param name="markdownContent">Full Markdown content.</param>
    /// <param name="filePath">Path to the source file (for deriving source type and session id).</param>
    /// <returns>Parsed DTO, or null if the content is not a valid session log.</returns>
    public static UnifiedSessionLogDto? TryParse(string markdownContent, string filePath)
    {
        if (string.IsNullOrWhiteSpace(markdownContent))
            return null;

        // Must have a session log header to be recognized
        var titleMatch = TitleRegex().Match(markdownContent);
        if (!titleMatch.Success)
            return null;

        var title = titleMatch.Groups[1].Value.Trim();

        var dateMatch = DateRegex().Match(markdownContent);
        var statusMatch = StatusRegex().Match(markdownContent);
        var branchMatch = BranchRegex().Match(markdownContent);
        var modelMatch = ModelRegex().Match(markdownContent);
        var durationMatch = DurationRegex().Match(markdownContent);

        var dateStr = dateMatch.Success ? dateMatch.Groups[1].Value.Trim() : null;
        var status = statusMatch.Success ? statusMatch.Groups[1].Value.Trim() : "unknown";
        var branch = branchMatch.Success ? branchMatch.Groups[1].Value.Trim() : null;
        var model = modelMatch.Success ? modelMatch.Groups[1].Value.Trim() : null;
        var duration = durationMatch.Success ? durationMatch.Groups[1].Value.Trim() : null;

        // Remove status emoji/markers
        status = status.Replace("✅", "", StringComparison.Ordinal)
                       .Replace("🚧", "", StringComparison.Ordinal)
                       .Replace("⚠️", "", StringComparison.Ordinal)
                       .Trim();

        // Derive source type and session id from filename
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var (sourceType, sessionId) = DeriveIdentity(fileName);

        DateTimeOffset? started = null;
        if (dateStr != null && DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            started = parsed;

        var startedStr = started?.ToString("o", CultureInfo.InvariantCulture);

        // Extract sections for actions and entries
        var actions = ExtractActions(markdownContent);
        var entries = new List<UnifiedRequestEntryDto>();

        // Build a summary entry including all known sections
        var summaryResponse = BuildSummaryResponse(markdownContent, duration);

        if (actions.Count > 0 || !string.IsNullOrWhiteSpace(title))
        {
            entries.Add(new UnifiedRequestEntryDto
            {
                RequestId = $"{sourceType}-{sessionId}-summary",
                Timestamp = startedStr,
                QueryText = title,
                QueryTitle = "Session Summary",
                Response = summaryResponse,
                Actions = actions,
                Model = model
            });
        }

        // Extract individual request entries (### Request sub-sections)
        var requestEntries = ExtractRequestEntries(markdownContent, sourceType, sessionId, startedStr, model);
        entries.AddRange(requestEntries);

        return new UnifiedSessionLogDto
        {
            SourceType = sourceType,
            SessionId = sessionId,
            Title = title,
            Model = model,
            Started = startedStr,
            LastUpdated = startedStr,
            Status = status,
            EntryCount = entries.Count,
            Workspace = new WorkspaceInfoDto
            {
                Project = "FunWasHad",
                Branch = branch
            },
            Entries = entries
        };
    }

    /// <summary>
    /// TR-PLANNED-013: Produces a normalized, structured text representation of a Markdown session log
    /// suitable for chunking, FTS, and vector embedding. Matches the format of NormalizeJsonSessionLog.
    /// </summary>
    internal static string NormalizeToStructuredText(string markdownContent)
    {
        if (string.IsNullOrWhiteSpace(markdownContent))
            return string.Empty;

        var titleMatch = TitleRegex().Match(markdownContent);
        if (!titleMatch.Success)
            return markdownContent;

        var sb = new StringBuilder();
        var title = titleMatch.Groups[1].Value.Trim();

        var modelMatch = ModelRegex().Match(markdownContent);
        var durationMatch = DurationRegex().Match(markdownContent);
        var dateMatch = DateRegex().Match(markdownContent);
        var statusMatch = StatusRegex().Match(markdownContent);

        sb.Append("Session: ").AppendLine(title);
        if (dateMatch.Success) sb.Append("Date: ").AppendLine(dateMatch.Groups[1].Value.Trim());
        if (modelMatch.Success) sb.Append("Model: ").AppendLine(modelMatch.Groups[1].Value.Trim());
        if (durationMatch.Success) sb.Append("Duration: ").AppendLine(durationMatch.Groups[1].Value.Trim());
        if (statusMatch.Success) sb.Append("Status: ").AppendLine(statusMatch.Groups[1].Value.Trim());

        // Extract and append each known section
        foreach (var sectionName in KnownSections)
        {
            var sectionContent = ExtractSection(markdownContent, sectionName);
            if (!string.IsNullOrWhiteSpace(sectionContent))
            {
                sb.AppendLine("---");
                sb.Append("Section: ").AppendLine(sectionName);
                sb.AppendLine(sectionContent);
            }
        }

        // Append request entries if present
        var requestMatches = RequestHeaderRegex().Matches(markdownContent);
        if (requestMatches.Count > 0)
        {
            foreach (Match rm in requestMatches)
            {
                sb.AppendLine("---");
                sb.Append("Request: ").AppendLine(rm.Groups[1].Value.Trim());
                var reqBody = ExtractSubSectionBody(markdownContent, rm.Index + rm.Length);
                if (!string.IsNullOrWhiteSpace(reqBody))
                    sb.AppendLine(reqBody);
            }
        }

        return sb.ToString();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Agent names must be lowercase per convention")]
    private static (string SourceType, string SessionId) DeriveIdentity(string fileName)
    {
        // Pattern: agent-SESSION-LOG-YYYY-MM-DD or agent-session-log-YYYY-MM-DD
        var parts = fileName.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var agent = parts[0].ToLowerInvariant();
            return (agent, fileName.ToLowerInvariant());
        }
        return ("unknown", fileName.ToLowerInvariant());
    }

    private static string BuildSummaryResponse(string content, string? duration)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(duration))
            sb.Append("Duration: ").AppendLine(duration);

        foreach (var sectionName in KnownSections)
        {
            var sectionContent = ExtractSection(content, sectionName);
            if (!string.IsNullOrWhiteSpace(sectionContent))
            {
                sb.Append(sectionName).AppendLine(":");
                sb.AppendLine(sectionContent);
                sb.AppendLine();
            }
        }

        var result = sb.ToString().Trim();
        return string.IsNullOrEmpty(result) ? ExtractSection(content, "Session Overview") ?? string.Empty : result;
    }

    private static List<UnifiedRequestEntryDto> ExtractRequestEntries(
        string content, string sourceType, string sessionId, string? timestamp, string? model)
    {
        var entries = new List<UnifiedRequestEntryDto>();
        var matches = RequestHeaderRegex().Matches(content);
        var order = 0;

        foreach (Match match in matches)
        {
            var requestTitle = match.Groups[1].Value.Trim();
            var body = ExtractSubSectionBody(content, match.Index + match.Length);

            entries.Add(new UnifiedRequestEntryDto
            {
                RequestId = $"{sourceType}-{sessionId}-req-{order:D3}",
                Timestamp = timestamp,
                QueryText = requestTitle,
                QueryTitle = requestTitle,
                Response = body,
                Model = model
            });
            order++;
        }

        return entries;
    }

    private static string? ExtractSubSectionBody(string content, int startIndex)
    {
        if (startIndex >= content.Length)
            return null;

        // Find the next ### or ## header, or end of content
        var nextHeader = Regex.Match(content[startIndex..], @"\n###?\s", RegexOptions.None);
        var endIndex = nextHeader.Success ? startIndex + nextHeader.Index : content.Length;
        var body = content[startIndex..endIndex].Trim();
        return string.IsNullOrEmpty(body) ? null : body;
    }

    private static List<UnifiedActionDto> ExtractActions(string content)
    {
        var actions = new List<UnifiedActionDto>();
        var section = ExtractSection(content, "Changes Made");
        if (string.IsNullOrWhiteSpace(section)) return actions;

        var lines = section.Split('\n');
        var order = 0;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                actions.Add(new UnifiedActionDto
                {
                    Order = order++,
                    Description = trimmed[2..].Trim(),
                    Type = "change",
                    Status = "completed"
                });
            }
        }
        return actions;
    }

    private static string? ExtractSection(string content, string sectionName)
    {
        var pattern = $@"##\s+\d*\.?\s*{Regex.Escape(sectionName)}.*?\n(.*?)(?=\n##\s|\Z)";
        var match = Regex.Match(content, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    [GeneratedRegex(@"^#\s+(?:Copilot\s+)?Session\s+Log\s*[-–—]\s*(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"\*\*Date:\*\*\s*(.+?)$", RegexOptions.Multiline)]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"\*\*Status:\*\*\s*(.+?)$", RegexOptions.Multiline)]
    private static partial Regex StatusRegex();

    [GeneratedRegex(@"\*\*Branch:\*\*\s*(.+?)$", RegexOptions.Multiline)]
    private static partial Regex BranchRegex();

    [GeneratedRegex(@"\*\*Model:\*\*\s*(.+?)$", RegexOptions.Multiline)]
    private static partial Regex ModelRegex();

    [GeneratedRegex(@"\*\*Duration:\*\*\s*(.+?)$", RegexOptions.Multiline)]
    private static partial Regex DurationRegex();

    [GeneratedRegex(@"^###\s+(?:Request\s+)?(.+)$", RegexOptions.Multiline)]
    private static partial Regex RequestHeaderRegex();
}
