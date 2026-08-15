using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Storage.Entities;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-SESSIONLOGCTX-001 / AC-FR-MCP-SESSIONLOGCTX-001-006 / AC-TR-MCP-SESSIONLOG-006-003:
/// Extracts planFile and todoId from turn contents and agent history under ~.
/// </summary>
public sealed class SessionLogTurnContextExtractor
{
    private static readonly Regex PathToken = new(
        @"(?:~[/\\]|[A-Za-z]:[/\\]|[/\\]|(?<![\w./\\-]))(?:[\w.-]+[/\\])+[\w.-]+\.[A-Za-z0-9]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TodoToken = new(
        @"\b(?:[A-Z]+-[A-Z0-9]+-\d{3}|ISSUE-\d+)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const int MaxHistoryFiles = 20;
    private const int MaxHistoryBytes = 1_048_576;

    /// <summary>
    /// Extracts a validated pair. Always returns <see cref="SessionLogTurnContextValidator.NoneSentinel"/>
    /// or a valid value. Never invents ids or paths.
    /// </summary>
    /// <param name="turn">Turn entity with children loaded.</param>
    /// <param name="workspacePath">Active workspace path for relative matching.</param>
    /// <param name="userProfilePath">Fake or real user profile used as ~.</param>
    /// <param name="agentSessionId">Optional session AgentSessionId for history matching.</param>
    /// <param name="agentSessionTranscriptFile">Optional exact transcript path.</param>
    /// <returns>Normalized planFile and todoId.</returns>
    public (string PlanFile, string TodoId) Extract(
        SessionLogTurnEntity turn,
        string? workspacePath,
        string? userProfilePath = null,
        string? agentSessionId = null,
        string? agentSessionTranscriptFile = null)
    {
        ArgumentNullException.ThrowIfNull(turn);

        var sources = CollectSources(turn, workspacePath, userProfilePath, agentSessionId, agentSessionTranscriptFile);
        var todoId = ExtractTodoId(turn, sources);
        var planFile = ExtractPlanFile(turn, sources, workspacePath, userProfilePath);
        return (planFile, todoId);
    }

    private static List<string> CollectSources(
        SessionLogTurnEntity turn,
        string? workspacePath,
        string? userProfilePath,
        string? agentSessionId,
        string? agentSessionTranscriptFile)
    {
        var parts = new List<string>();
        AddIfAny(parts, turn.QueryTitle);
        AddIfAny(parts, turn.QueryText);
        AddIfAny(parts, turn.Response);
        AddIfAny(parts, turn.Interpretation);
        foreach (var tag in turn.Tags)
            AddIfAny(parts, tag.Tag);
        foreach (var context in turn.ContextItems)
            AddIfAny(parts, context.ContextItem);
        foreach (var item in turn.StringListItems)
            AddIfAny(parts, item.Value);
        foreach (var action in turn.Actions)
        {
            AddIfAny(parts, action.FilePath);
            AddIfAny(parts, action.Description);
        }

        foreach (var dialog in turn.ProcessingDialog)
            AddIfAny(parts, dialog.Content);

        AppendHistory(parts, workspacePath, userProfilePath, agentSessionId, agentSessionTranscriptFile);
        return parts;
    }

    private static void AppendHistory(
        List<string> parts,
        string? workspacePath,
        string? userProfilePath,
        string? agentSessionId,
        string? agentSessionTranscriptFile)
    {
        var files = new List<string>();
        if (!string.IsNullOrWhiteSpace(agentSessionTranscriptFile) && File.Exists(agentSessionTranscriptFile))
            files.Add(agentSessionTranscriptFile);

        var home = userProfilePath
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home) || !Directory.Exists(home))
            return;

        var workspaceSlug = string.IsNullOrWhiteSpace(workspacePath)
            ? null
            : Path.GetFileName(workspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        foreach (var rootName in new[] { ".grok", ".claude", ".codex", ".cursor" })
        {
            var root = Path.Combine(home, rootName);
            if (!Directory.Exists(root))
                continue;

            IEnumerable<string> candidates;
            try
            {
                candidates = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in candidates)
            {
                if (files.Count >= MaxHistoryFiles)
                    break;
                if (HistoryFileMatches(file, agentSessionId, workspaceSlug))
                    files.Add(file);
            }
        }

        if (!string.IsNullOrWhiteSpace(workspacePath))
        {
            var map = Path.Combine(workspacePath, ".mcpServer", "plan-todo-map.yaml");
            if (File.Exists(map))
                files.Add(map);
        }

        foreach (var file in files.Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxHistoryFiles))
        {
            try
            {
                var info = new FileInfo(file);
                if (!info.Exists || info.Length == 0)
                    continue;
                using var stream = info.OpenRead();
                var buffer = new byte[Math.Min(MaxHistoryBytes, info.Length)];
                var read = stream.Read(buffer, 0, buffer.Length);
                parts.Add(System.Text.Encoding.UTF8.GetString(buffer, 0, read));
            }
            catch (IOException)
            {
                // Skip unreadable history files.
            }
            catch (UnauthorizedAccessException)
            {
                // Skip locked history files.
            }
        }
    }

    private static bool HistoryFileMatches(string file, string? agentSessionId, string? workspaceSlug)
    {
        if (!string.IsNullOrWhiteSpace(agentSessionId)
            && ContainsPathSegment(file, agentSessionId))
            return true;
        if (!string.IsNullOrWhiteSpace(workspaceSlug)
            && workspaceSlug.Length >= 4
            && ContainsPathSegment(file, workspaceSlug))
            return true;
        return false;
    }

    private static bool ContainsPathSegment(string path, string segment)
    {
        var normalized = path.Replace('\\', '/');
        var needle = segment.Replace('\\', '/');
        var index = 0;
        while ((index = normalized.IndexOf(needle, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var beforeOk = index == 0 || IsPathSeparator(normalized[index - 1]);
            var afterIndex = index + needle.Length;
            var afterOk = afterIndex >= normalized.Length || IsPathSeparator(normalized[afterIndex]);
            if (beforeOk && afterOk)
                return true;
            index = afterIndex;
        }

        return false;
    }

    private static bool IsPathSeparator(char ch) => ch is '/' or '\\' or '%' or ':' or '.';

    private static string ExtractTodoId(SessionLogTurnEntity turn, IReadOnlyList<string> sources)
    {
        var tagHits = turn.Tags
            .Select(t => t.Tag)
            .Where(tag => tag is not null && IsTodoId(tag))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (tagHits.Count == 1)
            return tagHits[0]!;
        if (tagHits.Count > 1)
            return SessionLogTurnContextValidator.NoneSentinel;

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            foreach (Match match in TodoToken.Matches(source))
            {
                var value = match.Value;
                if (!IsTodoId(value) || IsEmbeddedInRequirementId(source, match.Index))
                    continue;
                counts[value] = counts.GetValueOrDefault(value) + 1;
            }
        }

        if (counts.Count == 0)
            return SessionLogTurnContextValidator.NoneSentinel;
        var max = counts.Values.Max();
        var winners = counts.Where(pair => pair.Value == max).Select(pair => pair.Key).ToList();
        return winners.Count == 1 ? winners[0] : SessionLogTurnContextValidator.NoneSentinel;
    }

    private static bool IsTodoId(string value)
    {
        if (SessionLogTurnContextValidator.RequirementId.IsMatch(value))
            return false;
        return SessionLogTurnContextValidator.CanonicalTodoId.IsMatch(value)
            || SessionLogTurnContextValidator.IssueTodoId.IsMatch(value);
    }

    private static bool IsEmbeddedInRequirementId(string source, int index)
    {
        if (index >= 5 && source.AsSpan(index - 5, 5).Equals("TEST-", StringComparison.Ordinal))
            return true;
        if (index >= 3)
        {
            var prefix = source.AsSpan(index - 3, 3);
            if (prefix.Equals("TR-", StringComparison.Ordinal) || prefix.Equals("FR-", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string ExtractPlanFile(
        SessionLogTurnEntity turn,
        IReadOnlyList<string> sources,
        string? workspacePath,
        string? userProfilePath)
    {
        var candidates = new List<string>();
        foreach (var context in turn.ContextItems)
            AddCandidate(candidates, context.ContextItem, userProfilePath);
        foreach (var item in turn.StringListItems.Where(i => i.ListType == "FileModified"))
            AddCandidate(candidates, item.Value, userProfilePath);
        foreach (var action in turn.Actions)
            AddCandidate(candidates, action.FilePath, userProfilePath);

        foreach (var source in sources)
        {
            foreach (Match match in PathToken.Matches(source))
                AddCandidate(candidates, match.Value.Trim().Trim('"', '\''), userProfilePath);
        }

        var unique = candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (unique.Count == 0)
            return SessionLogTurnContextValidator.NoneSentinel;

        var docsPlans = unique.Where(p => p.Contains("/docs/plans/", StringComparison.OrdinalIgnoreCase)).ToList();
        if (docsPlans.Count == 1)
            return docsPlans[0];

        var homePlans = unique.Where(LooksLikePlanFile).ToList();
        if (homePlans.Count == 1)
            return homePlans[0];

        var namedPlan = unique.Where(p => Path.GetFileName(p).Contains("plan", StringComparison.OrdinalIgnoreCase)).ToList();
        if (namedPlan.Count == 1)
            return namedPlan[0];

        return unique.Count == 1 ? unique[0] : SessionLogTurnContextValidator.NoneSentinel;
    }

    private static bool LooksLikePlanFile(string path)
    {
        var file = Path.GetFileName(path);
        return file.Contains("plan", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/docs/plans/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/plans/", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddCandidate(List<string> candidates, string? raw, string? userProfilePath)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;
        try
        {
            var normalized = SessionLogTurnContextValidator.NormalizePlanFile(raw, userProfilePath);
            if (normalized != SessionLogTurnContextValidator.NoneSentinel)
                candidates.Add(normalized);
        }
        catch (ArgumentException)
        {
            // Drop invalid path tokens.
        }
    }

    private static void AddIfAny(List<string> parts, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parts.Add(value);
    }
}
