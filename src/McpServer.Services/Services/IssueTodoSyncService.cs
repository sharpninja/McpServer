using System.Globalization;
using System.Text;
using McpServer.Support.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-GH-013-002, TR-GH-013-003: Bidirectional sync between GitHub Issues and MCP TODOs.
/// FR-SUPPORT-013: Automatic TODO tracking with ISSUE-&lt;number&gt; IDs.
/// </summary>
public sealed class IssueTodoSyncService(
    IGitHubCliService github,
    WorkspaceServiceAccessor workspaceAccessor,
    ILogger<IssueTodoSyncService> logger) : IIssueTodoSyncService
{
    private const string IssueIdPrefix = "ISSUE-";
    private const string GitHubCommentsBeginMarker = "<!-- BEGIN MCP GITHUB COMMENTS -->";
    private const string GitHubCommentsEndMarker = "<!-- END MCP GITHUB COMMENTS -->";
    private const string GitHubCommentsHeading = "## GitHub Comments";

    /// <inheritdoc />
    public async Task<TodoMutationResult> SyncIssueToTodoAsync(GitHubIssueDetail issue, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(issue);
        var todoId = $"{IssueIdPrefix}{issue.Number}";
        var priority = MapPriority(issue.Labels);
        var section = MapSection(issue.Labels);
        var done = string.Equals(issue.State, "CLOSED", StringComparison.OrdinalIgnoreCase);

        var frontmatter = new IssueNoteFrontmatter
        {
            Status = issue.State,
            GitHubUrl = issue.Url,
            Labels = issue.Labels.Select(l => l.Name).ToList(),
            Assignees = issue.Assignees.ToList(),
            Created = issue.CreatedAt,
            Updated = issue.UpdatedAt
        };
        var note = frontmatter.Serialize();

        var description = new List<string>();
        if (!string.IsNullOrWhiteSpace(issue.Body))
        {
            var bodyPreview = issue.Body.Length > 500 ? issue.Body[..500] + "..." : issue.Body;
            description.Add(bodyPreview);
        }

        var existing = await workspaceAccessor.GetTodoService().GetByIdAsync(todoId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            var update = new TodoUpdateRequest
            {
                Title = issue.Title,
                Done = done,
                Priority = existing.Priority,
                Section = section,
                Note = MergeIssueNote(existing.Note, note, issue.Comments)
            };
            var result = await workspaceAccessor.GetTodoService().UpdateAsync(todoId, update, ct).ConfigureAwait(false);
            logger.LogInformation("Updated TODO {Id} from issue #{Number}", todoId, issue.Number);
            return result;
        }
        else
        {
            var create = new TodoCreateRequest
            {
                Id = todoId,
                Title = issue.Title,
                Section = section,
                Priority = priority,
                Description = description.Count > 0 ? description : null
            };
            var result = await workspaceAccessor.GetTodoService().CreateAsync(create, ct).ConfigureAwait(false);
            if (result.Success)
            {
                // Set note and done status after creation
                await workspaceAccessor.GetTodoService().UpdateAsync(
                    todoId,
                    new TodoUpdateRequest
                    {
                        Note = MergeIssueNote(null, note, issue.Comments),
                        Done = done
                    },
                    ct).ConfigureAwait(false);
            }
            logger.LogInformation("Created TODO {Id} from issue #{Number}", todoId, issue.Number);
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<IssueSyncResult> SyncAllIssuesToTodosAsync(string? state, int limit, CancellationToken ct = default)
    {
        var issueListResult = await github.ListIssuesAsync(state, limit, ct).ConfigureAwait(false);
        if (!issueListResult.Success)
        {
            return new IssueSyncResult { Failed = 1, Errors = [issueListResult.Error ?? "Failed to list issues"] };
        }

        var synced = 0;
        var skipped = 0;
        var failed = 0;
        var errors = new List<string>();

        foreach (var issueItem in issueListResult.Issues)
        {
            try
            {
                var detailResult = await github.GetIssueAsync(issueItem.Number, ct).ConfigureAwait(false);
                if (!detailResult.Success || detailResult.Issue is null)
                {
                    failed++;
                    errors.Add($"Issue #{issueItem.Number}: {detailResult.ErrorMessage ?? "Failed to get detail"}");
                    continue;
                }

                var result = await SyncIssueToTodoAsync(detailResult.Issue, ct).ConfigureAwait(false);
                if (result.Success)
                    synced++;
                else
                {
                    failed++;
                    errors.Add($"Issue #{issueItem.Number}: {result.Error ?? "Failed to sync"}");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError("{ExceptionDetail}", ex.ToString());
                failed++;
                errors.Add($"Issue #{issueItem.Number}: {ex.Message}");
            }
        }

        logger.LogInformation("Issue->TODO sync: {Synced} synced, {Skipped} skipped, {Failed} failed", synced, skipped, failed);
        return new IssueSyncResult { Synced = synced, Skipped = skipped, Failed = failed, Errors = errors };
    }

    /// <inheritdoc />
    public async Task<GitHubMutationResult> SyncTodoToIssueAsync(string todoId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(todoId);
        if (!TryParseIssueNumber(todoId, out var issueNumber, out var parseError))
            return new GitHubMutationResult(false, null, parseError);

        var todo = await workspaceAccessor.GetTodoService().GetByIdAsync(todoId, ct).ConfigureAwait(false);
        if (todo is null)
            return new GitHubMutationResult(false, null, $"TODO {todoId} not found");

        var issueResult = await github.GetIssueAsync(issueNumber, ct).ConfigureAwait(false);
        if (!issueResult.Success || issueResult.Issue is null)
            return new GitHubMutationResult(false, null, issueResult.ErrorMessage ?? "Failed to get issue from GitHub");

        var issue = issueResult.Issue;
        var isIssueOpen = string.Equals(issue.State, "OPEN", StringComparison.OrdinalIgnoreCase);

        // Sync done status
        if (todo.Done && isIssueOpen)
        {
            var closeResult = await github.CloseIssueAsync(issueNumber, "completed", ct).ConfigureAwait(false);
            if (!closeResult.Success)
                return closeResult;
            logger.LogInformation("Closed issue #{Number} (TODO {Id} is done)", issueNumber, todoId);
        }
        else if (!todo.Done && !isIssueOpen)
        {
            var reopenResult = await github.ReopenIssueAsync(issueNumber, ct).ConfigureAwait(false);
            if (!reopenResult.Success)
                return reopenResult;
            logger.LogInformation("Reopened issue #{Number} (TODO {Id} is not done)", issueNumber, todoId);
        }

        var updateRequest = BuildIssueUpdateRequest(todo, issue);
        if (updateRequest is not null)
        {
            var updateResult = await github.UpdateIssueAsync(issueNumber, updateRequest, ct).ConfigureAwait(false);
            if (updateResult is null || !updateResult.Success)
                return updateResult ?? new GitHubMutationResult(false, null, $"UpdateIssueAsync returned null for issue #{issueNumber}");
            logger.LogInformation("Updated metadata for issue #{Number}", issueNumber);
        }

        return new GitHubMutationResult(true, issue.Url, null);
    }

    /// <inheritdoc />
    public async Task<GitHubCommentResult> CommentOnTodoUpdateAsync(TodoFlatItem previousTodo, TodoFlatItem currentTodo, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(previousTodo);
        ArgumentNullException.ThrowIfNull(currentTodo);

        if (!string.Equals(previousTodo.Id, currentTodo.Id, StringComparison.OrdinalIgnoreCase))
            return new GitHubCommentResult(false, "Cannot comment on mismatched TODO ids.");

        if (!TryParseIssueNumber(currentTodo.Id, out var issueNumber, out var parseError))
            return new GitHubCommentResult(false, parseError);

        var commentBody = BuildTodoUpdateComment(previousTodo, currentTodo);
        if (string.IsNullOrWhiteSpace(commentBody))
            return new GitHubCommentResult(true, null);

        var result = await github.CommentOnIssueAsync(issueNumber.ToString(CultureInfo.InvariantCulture), commentBody, ct).ConfigureAwait(false);
        if (result.Success)
            logger.LogInformation("Added TODO update comment to issue #{Number}", issueNumber);

        return result;
    }

    /// <inheritdoc />
    public async Task<IssueSyncResult> SyncAllTodosToIssuesAsync(CancellationToken ct = default)
    {
        var queryResult = await workspaceAccessor.GetTodoService().QueryAsync(new TodoQueryRequest { Keyword = IssueIdPrefix }, ct).ConfigureAwait(false);
        var issueTodos = queryResult.Items.Where(t => t.Id.StartsWith(IssueIdPrefix, StringComparison.OrdinalIgnoreCase)).ToList();

        var synced = 0;
        var failed = 0;
        var errors = new List<string>();

        foreach (var todo in issueTodos)
        {
            try
            {
                var result = await SyncTodoToIssueAsync(todo.Id, ct).ConfigureAwait(false);
                if (result.Success)
                    synced++;
                else
                {
                    failed++;
                    errors.Add($"{todo.Id}: {result.ErrorMessage ?? "Failed to sync"}");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError("{ExceptionDetail}", ex.ToString());
                failed++;
                errors.Add($"{todo.Id}: {ex.Message}");
            }
        }

        logger.LogInformation("TODO->Issue sync: {Synced} synced, {Failed} failed", synced, failed);
        return new IssueSyncResult { Synced = synced, Failed = failed, Errors = errors };
    }

    /// <summary>TR-GH-013-002: Maps issue labels to priority.</summary>
    internal static string MapPriority(IReadOnlyList<GitHubLabel> labels)
    {
        foreach (var label in labels)
        {
            if (TryMapPriorityLabel(label.Name, out var priority))
                return priority;
        }
        return "low";
    }

    /// <summary>TR-GH-013-002: Maps issue labels to section. Derives section from <c>area:*</c> labels; defaults to "issues".</summary>
    internal static string MapSection(IReadOnlyList<GitHubLabel> labels)
    {
        const string areaPrefix = "area:";
        foreach (var label in labels)
        {
            if (label.Name.StartsWith(areaPrefix, StringComparison.OrdinalIgnoreCase))
                return label.Name[areaPrefix.Length..].ToLowerInvariant();
        }
        return "issues";
    }

    internal static string? BuildTodoUpdateComment(TodoFlatItem previousTodo, TodoFlatItem currentTodo)
    {
        var changes = new List<string>();
        var previousUserNote = ExtractUserNoteBody(previousTodo.Note);
        var currentUserNote = ExtractUserNoteBody(currentTodo.Note);

        if (!string.Equals(previousTodo.Title, currentTodo.Title, StringComparison.Ordinal))
            changes.Add(FormattableString.Invariant($"- Title: \"{previousTodo.Title}\" -> \"{currentTodo.Title}\""));
        if (!string.Equals(previousTodo.Priority, currentTodo.Priority, StringComparison.OrdinalIgnoreCase))
            changes.Add(FormattableString.Invariant($"- Priority: {ToCanonicalPriorityLabel(previousTodo.Priority)} -> {ToCanonicalPriorityLabel(currentTodo.Priority)}"));
        if (!string.Equals(previousTodo.Section, currentTodo.Section, StringComparison.OrdinalIgnoreCase))
            changes.Add(FormattableString.Invariant($"- Section: {previousTodo.Section} -> {currentTodo.Section}"));
        if (previousTodo.Done != currentTodo.Done)
            changes.Add(FormattableString.Invariant($"- Done: {previousTodo.Done.ToString().ToLowerInvariant()} -> {currentTodo.Done.ToString().ToLowerInvariant()}"));
        if (!string.Equals(previousTodo.Estimate, currentTodo.Estimate, StringComparison.Ordinal))
            changes.Add(FormattableString.Invariant($"- Estimate: {FormatValue(previousTodo.Estimate)} -> {FormatValue(currentTodo.Estimate)}"));
        if (!string.Equals(previousUserNote, currentUserNote, StringComparison.Ordinal))
        {
            var appendedComment = TryExtractAppendedNote(previousUserNote, currentUserNote);
            changes.Add(string.IsNullOrWhiteSpace(appendedComment)
                ? "- Note updated."
                : $"- Comment added:{Environment.NewLine}{IndentBlock(appendedComment)}");
        }
        if (!StringListsEqual(previousTodo.TechnicalDetails, currentTodo.TechnicalDetails, StringComparer.Ordinal))
            changes.Add("- Technical details updated.");
        if (!TaskListsEqual(previousTodo.ImplementationTasks, currentTodo.ImplementationTasks))
            changes.Add("- Implementation tasks updated.");
        if (!string.Equals(previousTodo.CompletedDate, currentTodo.CompletedDate, StringComparison.Ordinal))
            changes.Add(FormattableString.Invariant($"- Completed date: {FormatValue(previousTodo.CompletedDate)} -> {FormatValue(currentTodo.CompletedDate)}"));
        if (!string.Equals(previousTodo.DoneSummary, currentTodo.DoneSummary, StringComparison.Ordinal))
            changes.Add("- Done summary updated.");
        if (!string.Equals(previousTodo.Remaining, currentTodo.Remaining, StringComparison.Ordinal))
            changes.Add("- Remaining updated.");
        if (!StringListsEqual(previousTodo.DependsOn, currentTodo.DependsOn, StringComparer.OrdinalIgnoreCase))
            changes.Add("- Dependencies updated.");
        if (!StringListsEqual(previousTodo.FunctionalRequirements, currentTodo.FunctionalRequirements, StringComparer.OrdinalIgnoreCase))
            changes.Add("- Functional requirements updated.");
        if (!StringListsEqual(previousTodo.TechnicalRequirements, currentTodo.TechnicalRequirements, StringComparer.OrdinalIgnoreCase))
            changes.Add("- Technical requirements updated.");

        if (changes.Count == 0)
            return null;

        var builder = new StringBuilder();
        builder.AppendLine("MCP TODO update synced from the workspace.");
        builder.AppendLine();
        foreach (var change in changes)
            builder.AppendLine(change);
        return builder.ToString().TrimEnd();
    }

    private static GitHubIssueUpdateRequest? BuildIssueUpdateRequest(TodoFlatItem todo, GitHubIssueDetail issue)
    {
        var addLabels = new List<string>();
        var removeLabels = new List<string>();

        var desiredPriorityLabel = ToCanonicalPriorityLabel(todo.Priority);
        var existingPriorityLabels = issue.Labels
            .Select(static label => label.Name)
            .Where(static labelName => TryMapPriorityLabel(labelName, out _))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (!existingPriorityLabels.Contains(desiredPriorityLabel, StringComparer.Ordinal))
            addLabels.Add(desiredPriorityLabel);

        foreach (var label in existingPriorityLabels)
        {
            if (!string.Equals(label, desiredPriorityLabel, StringComparison.Ordinal))
                removeLabels.Add(label);
        }

        var title = string.Equals(todo.Title, issue.Title, StringComparison.Ordinal)
            ? null
            : todo.Title;

        if (title is null && addLabels.Count == 0 && removeLabels.Count == 0)
            return null;

        return new GitHubIssueUpdateRequest
        {
            Title = title,
            AddLabels = addLabels.Count > 0 ? addLabels : null,
            RemoveLabels = removeLabels.Count > 0 ? removeLabels : null
        };
    }

    private static string MergeIssueNote(string? existingNote, string frontmatter, IReadOnlyList<GitHubIssueComment> comments)
    {
        var sections = new List<string>();
        var trimmedFrontmatter = frontmatter.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedFrontmatter))
            sections.Add(trimmedFrontmatter);

        var preservedBody = ExtractUserNoteBody(existingNote);
        if (!string.IsNullOrWhiteSpace(preservedBody))
            sections.Add(preservedBody);

        var commentsSection = BuildGitHubCommentsSection(comments);
        if (!string.IsNullOrWhiteSpace(commentsSection))
            sections.Add(commentsSection);

        return string.Join(Environment.NewLine + Environment.NewLine, sections);
    }

    private static bool IsIssueFrontmatterLine(string line)
        => line.StartsWith("status:", StringComparison.OrdinalIgnoreCase)
           || line.StartsWith("github-url:", StringComparison.OrdinalIgnoreCase)
           || line.StartsWith("labels:", StringComparison.OrdinalIgnoreCase)
           || line.StartsWith("assignees:", StringComparison.OrdinalIgnoreCase)
           || line.StartsWith("created:", StringComparison.OrdinalIgnoreCase)
           || line.StartsWith("updated:", StringComparison.OrdinalIgnoreCase);

    private static string? ExtractUserNoteBody(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return null;

        var bodyLines = new List<string>();
        var inGeneratedComments = false;
        foreach (var rawLine in note.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.Trim();
            if (string.Equals(trimmed, GitHubCommentsBeginMarker, StringComparison.Ordinal))
            {
                inGeneratedComments = true;
                continue;
            }

            if (string.Equals(trimmed, GitHubCommentsEndMarker, StringComparison.Ordinal))
            {
                inGeneratedComments = false;
                continue;
            }

            if (inGeneratedComments || IsIssueFrontmatterLine(trimmed))
                continue;

            bodyLines.Add(line);
        }

        return TrimBlankLines(bodyLines);
    }

    private static string? BuildGitHubCommentsSection(IReadOnlyList<GitHubIssueComment> comments)
    {
        if (comments.Count == 0)
            return null;

        var lines = new List<string>
        {
            GitHubCommentsBeginMarker,
            GitHubCommentsHeading,
            string.Empty
        };

        foreach (var comment in comments)
        {
            lines.Add(FormattableString.Invariant($"### {BuildGitHubCommentHeading(comment)}"));
            lines.AddRange(SplitCommentBody(comment.Body));
            lines.Add(string.Empty);
        }

        if (lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);

        lines.Add(GitHubCommentsEndMarker);
        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildGitHubCommentHeading(GitHubIssueComment comment)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(comment.Author))
            parts.Add(comment.Author.Trim());
        if (!string.IsNullOrWhiteSpace(comment.CreatedAt))
            parts.Add(comment.CreatedAt.Trim());

        return parts.Count == 0 ? "GitHub comment" : string.Join(" | ", parts);
    }

    private static IReadOnlyList<string> SplitCommentBody(string? body)
    {
        var normalized = NormalizeMultilineText(body);
        return string.IsNullOrWhiteSpace(normalized)
            ? ["(empty)"]
            : normalized.Split('\n');
    }

    private static string? TryExtractAppendedNote(string? previousUserNote, string? currentUserNote)
    {
        var previous = NormalizeMultilineText(previousUserNote);
        var current = NormalizeMultilineText(currentUserNote);
        if (string.IsNullOrWhiteSpace(current))
            return null;

        if (string.IsNullOrWhiteSpace(previous))
            return current;

        if (!current.StartsWith(previous, StringComparison.Ordinal))
            return null;

        var suffix = current[previous.Length..].Trim();
        return string.IsNullOrWhiteSpace(suffix) ? null : suffix;
    }

    private static string IndentBlock(string value)
    {
        var normalized = NormalizeMultilineText(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return "  (empty)";

        return string.Join(
            Environment.NewLine,
            normalized.Split('\n').Select(static line => $"  {line}"));
    }

    private static string NormalizeMultilineText(string? value)
        => (value ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

    private static string? TrimBlankLines(List<string> lines)
    {
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
            lines.RemoveAt(0);
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
            lines.RemoveAt(lines.Count - 1);

        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
    }

    private static bool TryParseIssueNumber(string todoId, out int issueNumber, out string? error)
    {
        issueNumber = 0;
        error = null;

        if (!todoId.StartsWith(IssueIdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            error = $"TODO id {todoId} is not an ISSUE-* id";
            return false;
        }

        if (!int.TryParse(todoId.AsSpan(IssueIdPrefix.Length), out issueNumber))
        {
            error = $"Cannot parse issue number from {todoId}";
            return false;
        }

        return true;
    }

    private static bool TryMapPriorityLabel(string labelName, out string priority)
    {
        priority = "low";
        if (string.IsNullOrWhiteSpace(labelName) || !labelName.StartsWith("priority:", StringComparison.OrdinalIgnoreCase))
            return false;

        var value = labelName["priority:".Length..].Trim();
        if (string.Equals(value, "high", StringComparison.OrdinalIgnoreCase))
        {
            priority = "high";
            return true;
        }

        if (string.Equals(value, "medium", StringComparison.OrdinalIgnoreCase))
        {
            priority = "medium";
            return true;
        }

        if (string.Equals(value, "low", StringComparison.OrdinalIgnoreCase))
        {
            priority = "low";
            return true;
        }

        return false;
    }

    private static string ToCanonicalPriorityLabel(string? priority)
        => priority?.Trim().ToLowerInvariant() switch
        {
            "high" => "priority: HIGH",
            "medium" => "priority: MEDIUM",
            _ => "priority: LOW"
        };

    private static string FormatValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? "(empty)" : value.Trim();

    private static bool StringListsEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right, StringComparer comparer)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        if (left.Count != right.Count)
            return false;

        for (var index = 0; index < left.Count; index++)
        {
            if (!comparer.Equals(left[index], right[index]))
                return false;
        }

        return true;
    }

    private static bool TaskListsEqual(IReadOnlyList<TodoFlatTask>? left, IReadOnlyList<TodoFlatTask>? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        if (left.Count != right.Count)
            return false;

        for (var index = 0; index < left.Count; index++)
        {
            if (!string.Equals(left[index].Task, right[index].Task, StringComparison.Ordinal) || left[index].Done != right[index].Done)
                return false;
        }

        return true;
    }
}
