using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Abstraction over GitHub integration endpoints used by UI.Core CQRS handlers.
/// </summary>
public interface IGitHubApiClient
{
    /// <summary>Lists repository issues.</summary>
    Task<GitHubIssueListResult> ListIssuesAsync(ListIssuesQuery query, CancellationToken cancellationToken = default);

    /// <summary>Gets issue detail by number.</summary>
    Task<GitHubIssueDetail?> GetIssueAsync(int number, CancellationToken cancellationToken = default);

    /// <summary>Creates an issue.</summary>
    Task<GitHubCreateIssueOutcome> CreateIssueAsync(CreateIssueCommand command, CancellationToken cancellationToken = default);

    /// <summary>Updates an issue.</summary>
    Task<GitHubMutationOutcome> UpdateIssueAsync(UpdateIssueCommand command, CancellationToken cancellationToken = default);

    /// <summary>Closes an issue.</summary>
    Task<GitHubMutationOutcome> CloseIssueAsync(CloseIssueCommand command, CancellationToken cancellationToken = default);

    /// <summary>Reopens an issue.</summary>
    Task<GitHubMutationOutcome> ReopenIssueAsync(ReopenIssueCommand command, CancellationToken cancellationToken = default);

    /// <summary>Adds a comment to an issue.</summary>
    Task<GitHubMutationOutcome> CommentOnIssueAsync(CommentOnIssueCommand command, CancellationToken cancellationToken = default);

    /// <summary>Lists repository labels.</summary>
    Task<GitHubLabelsResult> ListLabelsAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists pull requests.</summary>
    Task<GitHubPullListResult> ListPullsAsync(ListPullsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Adds a comment to a pull request.</summary>
    Task<GitHubMutationOutcome> CommentOnPullAsync(CommentOnPullCommand command, CancellationToken cancellationToken = default);

    /// <summary>Synchronizes issues from GitHub into TODO items.</summary>
    Task<GitHubSyncOutcome> SyncFromGitHubAsync(SyncFromGitHubCommand command, CancellationToken cancellationToken = default);

    /// <summary>Synchronizes TODO items to GitHub.</summary>
    Task<GitHubSyncOutcome> SyncToGitHubAsync(CancellationToken cancellationToken = default);

    /// <summary>Synchronizes a single issue by direction.</summary>
    Task<GitHubSingleIssueSyncOutcome> SyncSingleIssueAsync(SyncSingleIssueCommand command, CancellationToken cancellationToken = default);
}
