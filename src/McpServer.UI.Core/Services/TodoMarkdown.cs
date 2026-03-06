using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using McpServer.UI.Core.Models;

namespace McpServer.UI.Core.Services;

#pragma warning disable CS1591

/// <summary>Round-trip between McpTodoFlatItem and YAML front matter + markdown (same format as VS extension).</summary>
public static class TodoMarkdown
{
    public static string BlankTemplate()
    {
        return string.Join("\n",
            "---",
            "id: NEW-TODO",
            "section: mvp-app",
            "priority: low",
            "estimate: ",
            "depends-on: []",
            "---",
            "",
            "# ",
            "",
            "Description goes here.",
            "",
            "## Technical Details",
            "",
            "- ",
            "",
            "## Implementation Tasks",
            "",
            "- [ ] ",
            "");
    }

    public static string ToMarkdown(McpTodoFlatItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        var fm = new List<string> { "---" };
        fm.Add($"id: {item.Id}");
        fm.Add($"section: {item.Section ?? ""}");
        fm.Add($"priority: {item.Priority ?? ""}");
        if (item.Done) fm.Add("done: true");
        if (!string.IsNullOrEmpty(item.Estimate)) fm.Add($"estimate: {YamlScalar(item.Estimate!)}");
        if (!string.IsNullOrEmpty(item.Note)) fm.Add($"note: {YamlScalar(item.Note!)}");
        if (!string.IsNullOrEmpty(item.CompletedDate)) fm.Add($"completed: {item.CompletedDate}");
        if (!string.IsNullOrEmpty(item.DoneSummary)) fm.Add($"done-summary: {YamlScalar(item.DoneSummary!)}");
        if (!string.IsNullOrEmpty(item.Remaining)) fm.Add($"remaining: {YamlScalar(item.Remaining!)}");
        if (item.DependsOn?.Count > 0)
        {
            fm.Add("depends-on:");
            foreach (var d in item.DependsOn) fm.Add($"  - {d ?? ""}");
        }
        if (item.FunctionalRequirements?.Count > 0)
        {
            fm.Add("functional-requirements:");
            foreach (var fr in item.FunctionalRequirements) fm.Add($"  - {fr}");
        }
        if (item.TechnicalRequirements?.Count > 0)
        {
            fm.Add("technical-requirements:");
            foreach (var tr in item.TechnicalRequirements) fm.Add($"  - {tr}");
        }
        fm.Add("---");

        var body = new List<string> { "" };
        body.Add($"# {item.Title ?? ""}");
        body.Add("");

        if (item.Description?.Count > 0)
        {
            body.AddRange(item.Description);
            body.Add("");
        }
        if (item.TechnicalDetails?.Count > 0)
        {
            body.Add("## Technical Details");
            body.Add("");
            foreach (var d in item.TechnicalDetails) body.Add($"- {d}");
            body.Add("");
        }
        if (item.ImplementationTasks?.Count > 0)
        {
            body.Add("## Implementation Tasks");
            body.Add("");
            foreach (var t in item.ImplementationTasks)
                body.Add($"- [{(t.Done ? 'x' : ' ')}] {t.Task ?? ""}");
            body.Add("");
        }

        return string.Join("\n", fm) + string.Join("\n", body).TrimEnd();
    }

    public static McpTodoUpdateRequest FromMarkdown(string markdown)
    {
        if (markdown == null) throw new ArgumentNullException(nameof(markdown));
        var req = new McpTodoUpdateRequest();
        var (fm, bodyLines) = SplitFrontMatter(markdown);

        string? currentListKey = null;
        List<string>? currentList = null;

        foreach (var line in fm)
        {
            if (line.TrimStart().StartsWith("- ", StringComparison.Ordinal) && currentListKey != null)
            {
                var listValue = line.TrimStart().Substring(2).Trim();
                if (!string.IsNullOrEmpty(listValue))
                    currentList?.Add(listValue);
                continue;
            }

            if (currentListKey != null && currentList != null)
            {
                AssignListField(req, currentListKey, currentList);
                currentListKey = null;
                currentList = null;
            }

            var colon = line.IndexOf(':');
            if (colon <= 0) continue;
            var key = line.Substring(0, colon).Trim().ToLowerInvariant();
            var value = line.Substring(colon + 1).Trim();

            if (key == "id") continue;

            if (value == "" || value == "[]")
            {
                currentListKey = key;
                currentList = new List<string>();
                if (value == "[]") AssignListField(req, key, currentList);
                continue;
            }

            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                var inner = value.Substring(1, value.Length - 2).Trim();
                var items = string.IsNullOrEmpty(inner)
                    ? new List<string>()
                    : new List<string>(inner.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0));
                AssignListField(req, key, items);
                continue;
            }

            AssignScalarField(req, key, value);
        }

        if (currentListKey != null && currentList != null)
            AssignListField(req, currentListKey, currentList);

        // Parse body
        var description = new List<string>();
        var technicalDetails = new List<string>();
        var tasks = new List<McpTodoFlatTask>();
        var currentSection = "description";

        foreach (var line in bodyLines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("# ", StringComparison.Ordinal) && !trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                req.Title = trimmed.Substring(2).Trim();
                currentSection = "description";
                continue;
            }

            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                var heading = trimmed.Substring(3).Trim().ToUpperInvariant();
                if (heading.Contains("TECHNICAL")) currentSection = "technical-details";
                else if (heading.Contains("IMPLEMENTATION") || heading.Contains("TASK")) currentSection = "implementation-tasks";
                else currentSection = "description";
                continue;
            }

            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            switch (currentSection)
            {
                case "technical-details":
                    var bulletTd = Regex.Match(trimmed, @"^-\s+(.+)$");
                    technicalDetails.Add(bulletTd.Success ? bulletTd.Groups[1].Value : trimmed);
                    break;
                case "implementation-tasks":
                    var taskMatch = Regex.Match(trimmed, @"^-\s*\[([ xX])\]\s+(.+)$");
                    if (taskMatch.Success)
                        tasks.Add(new McpTodoFlatTask { Task = taskMatch.Groups[2].Value, Done = taskMatch.Groups[1].Value.Equals("x", StringComparison.OrdinalIgnoreCase) });
                    else
                    {
                        var plainBullet = Regex.Match(trimmed, @"^-\s+(.+)$");
                        if (plainBullet.Success)
                            tasks.Add(new McpTodoFlatTask { Task = plainBullet.Groups[1].Value, Done = false });
                    }
                    break;
                default:
                    description.Add(trimmed);
                    break;
            }
        }

        if (description.Count > 0) req.Description = description;
        if (technicalDetails.Count > 0) req.TechnicalDetails = technicalDetails;
        if (tasks.Count > 0) req.ImplementationTasks = tasks;

        return req;
    }

    /// <summary>Extract the todo ID from YAML front matter.</summary>
    public static string? ExtractId(string markdown)
    {
        var (fm, _) = SplitFrontMatter(markdown);
        foreach (var line in fm)
        {
            var colon = line.IndexOf(':');
            if (colon <= 0) continue;
            var key = line.Substring(0, colon).Trim().ToLowerInvariant();
            if (key == "id") return line.Substring(colon + 1).Trim();
        }
        return null;
    }

    private static (List<string> frontMatter, List<string> body) SplitFrontMatter(string doc)
    {
        var lines = doc.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        int start = -1, end = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                if (start < 0) start = i;
                else { end = i; break; }
            }
        }
        if (start < 0 || end < 0)
            return (new List<string>(), lines.ToList());
        return (lines.Skip(start + 1).Take(end - start - 1).ToList(), lines.Skip(end + 1).ToList());
    }

    private static string YamlScalar(string s)
    {
        if (Regex.IsMatch(s, @"[:#\[\]{}&*!|>'""% @`]") || s.Contains("\n"))
            return $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        return s;
    }

    private static void AssignScalarField(McpTodoUpdateRequest req, string key, string value)
    {
        switch (key)
        {
            case "section": req.Section = value; break;
            case "priority": req.Priority = value; break;
            case "done": req.Done = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase); break;
            case "estimate": req.Estimate = value; break;
            case "note": req.Note = value; break;
            case "completed": req.CompletedDate = value; break;
            case "done-summary": req.DoneSummary = value; break;
            case "remaining": req.Remaining = value; break;
        }
    }

    private static void AssignListField(McpTodoUpdateRequest req, string key, List<string> items)
    {
        switch (key)
        {
            case "depends-on": req.DependsOn = items; break;
            case "functional-requirements": req.FunctionalRequirements = items; break;
            case "technical-requirements": req.TechnicalRequirements = items; break;
        }
    }
}

#pragma warning restore CS1591
