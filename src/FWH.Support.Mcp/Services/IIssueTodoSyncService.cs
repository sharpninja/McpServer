using FWH.Support.Mcp.Models;

namespace FWH.Support.Mcp.Services;

/// <summary>
/// TR-GH-013-002, TR-GH-013-003: Bidirectional sync between GitHub Issues and MCP TODOs.
/// FR-SUPPORT-013: Automatic TODO tracking with ISSUE-&lt;number&gt; IDs.
/// </summary>
public interface IIssueTodoSyncService
{
    /// <summary>TR-GH-013-003: Syncs a single GitHub issue to a TODO item.</summary>
    /// <param name="issue">Full issue detail.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation result.</returns>
    Task<TodoMutationResult> SyncIssueToTodoAsync(GitHubIssueDetail issue, CancellationToken ct = default);

    /// <summary>TR-GH-013-003: Batch sync from GitHub to TODO.yaml.</summary>
    /// <param name="state">Issue state filter (open, closed, all).</param>
    /// <param name="limit">Max issues to sync.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Sync result with counts.</returns>
    Task<IssueSyncResult> SyncAllIssuesToTodosAsync(string? state, int limit, CancellationToken ct = default);

    /// <summary>TR-GH-013-003: Syncs a TODO item back to GitHub.</summary>
    /// <param name="todoId">TODO item id (ISSUE-{number}).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation result.</returns>
    Task<GitHubMutationResult> SyncTodoToIssueAsync(string todoId, CancellationToken ct = default);

    /// <summary>TR-GH-013-003: Batch sync from TODO.yaml to GitHub.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Sync result with counts.</returns>
    Task<IssueSyncResult> SyncAllTodosToIssuesAsync(CancellationToken ct = default);
}

/// <summary>TR-GH-013-003: Result of an issue sync operation.</summary>
public sealed record IssueSyncResult
{
    /// <summary>Number of items synced.</summary>
    public int Synced { get; init; }

    /// <summary>Number of items skipped (unchanged).</summary>
    public int Skipped { get; init; }

    /// <summary>Number of items that failed.</summary>
    public int Failed { get; init; }

    /// <summary>Error messages from failed syncs.</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
