using System.Globalization;
using System.Text;

namespace FWH.Support.Mcp.Models;

/// <summary>
/// TR-GH-013-005: Frontmatter convention for ISSUE-* TODO note fields.
/// Provides structured metadata for GitHub issue status tracking.
/// </summary>
public sealed record IssueNoteFrontmatter
{
    /// <summary>Issue state (open, closed).</summary>
    public string? Status { get; init; }

    /// <summary>GitHub issue URL.</summary>
    public string? GitHubUrl { get; init; }

    /// <summary>Issue labels.</summary>
    public IReadOnlyList<string>? Labels { get; init; }

    /// <summary>Issue assignees.</summary>
    public IReadOnlyList<string>? Assignees { get; init; }

    /// <summary>Issue creation date.</summary>
    public string? Created { get; init; }

    /// <summary>Issue last updated date.</summary>
    public string? Updated { get; init; }

    /// <summary>TR-GH-013-005: Serializes frontmatter to a formatted note string.</summary>
    public string Serialize()
    {
        var sb = new StringBuilder();
        if (Status is not null) sb.AppendLine(CultureInfo.InvariantCulture, $"status: {Status}");
        if (GitHubUrl is not null) sb.AppendLine(CultureInfo.InvariantCulture, $"github-url: {GitHubUrl}");
        if (Labels is { Count: > 0 }) sb.AppendLine(CultureInfo.InvariantCulture, $"labels: {string.Join(", ", Labels)}");
        if (Assignees is { Count: > 0 }) sb.AppendLine(CultureInfo.InvariantCulture, $"assignees: {string.Join(", ", Assignees)}");
        if (Created is not null) sb.AppendLine(CultureInfo.InvariantCulture, $"created: {Created}");
        if (Updated is not null) sb.AppendLine(CultureInfo.InvariantCulture, $"updated: {Updated}");
        return sb.ToString().TrimEnd();
    }

    /// <summary>TR-GH-013-005: Parses a note string into IssueNoteFrontmatter.</summary>
    /// <param name="note">The note string to parse.</param>
    /// <returns>Parsed frontmatter, or null if note is null/empty.</returns>
    public static IssueNoteFrontmatter? Parse(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return null;

        string? status = null, githubUrl = null, created = null, updated = null;
        IReadOnlyList<string>? labels = null, assignees = null;

        foreach (var rawLine in note.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;
            var colonIndex = line.IndexOf(':', StringComparison.Ordinal);
            if (colonIndex < 0) continue;

            var key = line[..colonIndex].Trim();
            var value = line[(colonIndex + 1)..].Trim();

            if (string.Equals(key, "status", StringComparison.OrdinalIgnoreCase)) status = value;
            else if (string.Equals(key, "github-url", StringComparison.OrdinalIgnoreCase)) githubUrl = line[(colonIndex + 1)..].Trim();
            else if (string.Equals(key, "labels", StringComparison.OrdinalIgnoreCase)) labels = ParseCsvList(value);
            else if (string.Equals(key, "assignees", StringComparison.OrdinalIgnoreCase)) assignees = ParseCsvList(value);
            else if (string.Equals(key, "created", StringComparison.OrdinalIgnoreCase)) created = value;
            else if (string.Equals(key, "updated", StringComparison.OrdinalIgnoreCase)) updated = value;
        }

        if (status is null && githubUrl is null && labels is null && assignees is null && created is null && updated is null)
            return null;

        return new IssueNoteFrontmatter
        {
            Status = status,
            GitHubUrl = githubUrl,
            Labels = labels,
            Assignees = assignees,
            Created = created,
            Updated = updated
        };
    }

    private static List<string> ParseCsvList(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
             .Where(s => s.Length > 0)
             .ToList();
}
