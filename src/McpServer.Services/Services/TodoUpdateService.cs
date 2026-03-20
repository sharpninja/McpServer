using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TODO-004, TR-MCP-GH-006: Orchestrates TODO update flows that require
/// ISSUE-* synchronization, immutable descriptions, and GitHub change comments.
/// </summary>
public sealed class TodoUpdateService
{
    private const string IssueIdPrefix = "ISSUE-";

    private readonly WorkspaceServiceAccessor _workspaceAccessor;
    private readonly IIssueTodoSyncService? _issueTodoSyncService;
    private readonly ILogger<TodoUpdateService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TodoUpdateService"/> class.
    /// </summary>
    /// <param name="workspaceAccessor">Workspace-aware accessor for the active TODO service.</param>
    /// <param name="issueTodoSyncService">GitHub issue sync orchestration for ISSUE-* TODOs.</param>
    /// <param name="logger">Logger for update-flow diagnostics.</param>
    public TodoUpdateService(
        WorkspaceServiceAccessor workspaceAccessor,
        IIssueTodoSyncService? issueTodoSyncService,
        ILogger<TodoUpdateService> logger)
    {
        _workspaceAccessor = workspaceAccessor ?? throw new ArgumentNullException(nameof(workspaceAccessor));
        _issueTodoSyncService = issueTodoSyncService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Updates a TODO item in the active workspace. ISSUE-* items keep their description immutable after
    /// first sync, then push MCP-authored changes to GitHub and add a GitHub issue comment summarizing the
    /// change set.
    /// </summary>
    /// <param name="id">TODO item identifier.</param>
    /// <param name="request">Requested TODO updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mutation result returned by the underlying TODO store or sync orchestration.</returns>
    public async Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(request);

        var todoService = _workspaceAccessor.GetTodoService();
        var existing = await todoService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return new TodoMutationResult(false, $"Item with id '{id}' not found.", FailureKind: TodoMutationFailureKind.NotFound);

        var effectiveRequest = NormalizeRequest(id, request);
        if (!HasEffectiveChanges(existing, effectiveRequest))
        {
            _logger.LogDebug("Skipped no-op TODO update for {TodoId}.", id);
            return new TodoMutationResult(true, Item: existing);
        }

        var result = await todoService.UpdateAsync(id, effectiveRequest, cancellationToken).ConfigureAwait(false);
        if (!result.Success || result.Item is null || !IsIssueTodoId(id))
            return result;

        if (_issueTodoSyncService is null)
        {
            return new TodoMutationResult(
                false,
                $"Updated TODO {id} locally but GitHub issue sync is not configured.",
                result.Item,
                TodoMutationFailureKind.ExternalSyncFailed);
        }

        var syncResult = await _issueTodoSyncService.SyncTodoToIssueAsync(id, cancellationToken).ConfigureAwait(false);
        if (!syncResult.Success)
        {
            return new TodoMutationResult(
                false,
                $"Updated TODO {id} locally but failed to sync GitHub issue: {syncResult.ErrorMessage}",
                result.Item,
                TodoMutationFailureKind.ExternalSyncFailed);
        }

        var commentResult = await _issueTodoSyncService
            .CommentOnTodoUpdateAsync(existing, result.Item, cancellationToken)
            .ConfigureAwait(false);
        if (!commentResult.Success)
        {
            return new TodoMutationResult(
                false,
                $"Updated TODO {id} and synced GitHub issue, but failed to add GitHub comment: {commentResult.Error}",
                result.Item,
                TodoMutationFailureKind.ExternalSyncFailed);
        }

        return result;
    }

    internal static bool IsIssueTodoId(string id)
        => id.StartsWith(IssueIdPrefix, StringComparison.OrdinalIgnoreCase);

    internal static TodoUpdateRequest NormalizeRequest(string id, TodoUpdateRequest request)
        => IsIssueTodoId(id)
            ? request with { Description = null }
            : request;

    internal static bool HasEffectiveChanges(TodoFlatItem existing, TodoUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(request);

        if (request.Title is not null && !string.Equals(request.Title, existing.Title, StringComparison.Ordinal))
            return true;
        if (request.Priority is not null && !string.Equals(request.Priority, existing.Priority, StringComparison.OrdinalIgnoreCase))
            return true;
        if (request.Section is not null && !string.Equals(request.Section, existing.Section, StringComparison.OrdinalIgnoreCase))
            return true;
        if (request.Done.HasValue && request.Done.Value != existing.Done)
            return true;
        if (request.Estimate is not null && !string.Equals(request.Estimate, existing.Estimate, StringComparison.Ordinal))
            return true;
        if (request.Description is not null && !StringListsEqual(request.Description, existing.Description, StringComparer.Ordinal))
            return true;
        if (request.TechnicalDetails is not null && !StringListsEqual(request.TechnicalDetails, existing.TechnicalDetails, StringComparer.Ordinal))
            return true;
        if (request.ImplementationTasks is not null && !TaskListsEqual(request.ImplementationTasks, existing.ImplementationTasks))
            return true;
        if (request.Note is not null && !string.Equals(request.Note, existing.Note, StringComparison.Ordinal))
            return true;
        if (request.CompletedDate is not null && !string.Equals(request.CompletedDate, existing.CompletedDate, StringComparison.Ordinal))
            return true;
        if (request.DoneSummary is not null && !string.Equals(request.DoneSummary, existing.DoneSummary, StringComparison.Ordinal))
            return true;
        if (request.Remaining is not null && !string.Equals(request.Remaining, existing.Remaining, StringComparison.Ordinal))
            return true;
        if (request.DependsOn is not null && !StringListsEqual(request.DependsOn, existing.DependsOn, StringComparer.OrdinalIgnoreCase))
            return true;
        if (request.FunctionalRequirements is not null && !StringListsEqual(request.FunctionalRequirements, existing.FunctionalRequirements, StringComparer.OrdinalIgnoreCase))
            return true;
        if (request.TechnicalRequirements is not null && !StringListsEqual(request.TechnicalRequirements, existing.TechnicalRequirements, StringComparer.OrdinalIgnoreCase))
            return true;

        return false;
    }

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
