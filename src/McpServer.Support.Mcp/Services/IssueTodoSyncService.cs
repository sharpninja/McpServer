using McpServer.Support.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-GH-013-002, TR-GH-013-003: Bidirectional sync between GitHub Issues and MCP TODOs.
/// FR-SUPPORT-013: Automatic TODO tracking with ISSUE-&lt;number&gt; IDs.
/// </summary>
public sealed class IssueTodoSyncService(
    IGitHubCliService github,
    ITodoService todoService,
    ILogger<IssueTodoSyncService> logger) : IIssueTodoSyncService
{
    private const string IssueIdPrefix = "ISSUE-";

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

        var existing = await todoService.GetByIdAsync(todoId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            var update = new TodoUpdateRequest
            {
                Title = issue.Title,
                Done = done,
                Priority = priority,
                Section = section,
                Note = note,
                Description = description.Count > 0 ? description : null
            };
            var result = await todoService.UpdateAsync(todoId, update, ct).ConfigureAwait(false);
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
            var result = await todoService.CreateAsync(create, ct).ConfigureAwait(false);
            if (result.Success)
            {
                // Set note and done status after creation
                await todoService.UpdateAsync(todoId, new TodoUpdateRequest { Note = note, Done = done }, ct).ConfigureAwait(false);
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
        if (!todoId.StartsWith(IssueIdPrefix, StringComparison.OrdinalIgnoreCase))
            return new GitHubMutationResult(false, null, $"TODO id {todoId} is not an ISSUE-* id");

        if (!int.TryParse(todoId.AsSpan(IssueIdPrefix.Length), out var issueNumber))
            return new GitHubMutationResult(false, null, $"Cannot parse issue number from {todoId}");

        var todo = await todoService.GetByIdAsync(todoId, ct).ConfigureAwait(false);
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

        // Sync title
        if (!string.Equals(todo.Title, issue.Title, StringComparison.Ordinal))
        {
            var updateResult = await github.UpdateIssueAsync(issueNumber, new GitHubIssueUpdateRequest { Title = todo.Title }, ct).ConfigureAwait(false);
            if (!updateResult.Success)
                return updateResult;
            logger.LogInformation("Updated title for issue #{Number}", issueNumber);
        }

        return new GitHubMutationResult(true, issue.Url, null);
    }

    /// <inheritdoc />
    public async Task<IssueSyncResult> SyncAllTodosToIssuesAsync(CancellationToken ct = default)
    {
        var queryResult = await todoService.QueryAsync(new TodoQueryRequest { Keyword = IssueIdPrefix }, ct).ConfigureAwait(false);
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
            if (string.Equals(label.Name, "priority:high", StringComparison.OrdinalIgnoreCase)) return "high";
            if (string.Equals(label.Name, "priority:medium", StringComparison.OrdinalIgnoreCase)) return "medium";
            if (string.Equals(label.Name, "priority:low", StringComparison.OrdinalIgnoreCase)) return "low";
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
}
