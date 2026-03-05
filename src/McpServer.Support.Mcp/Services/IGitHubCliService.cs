using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013, TR-GH-013-001: Wrapper for GitHub CLI (gh) for issues and PRs.
/// FR-SUPPORT-010, FR-SUPPORT-013: Uses existing local gh auth.
/// </summary>
public interface IGitHubCliService
{
    /// <summary>Lists issues (gh issue list). Returns empty list if gh not available or not authenticated.</summary>
    /// <param name="state">Optional state filter (open, closed, all).</param>
    /// <param name="limit">Maximum number of issues to return.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Issue list result with success flag and items.</returns>
    Task<GitHubIssueListResult> ListIssuesAsync(string? state, int limit, CancellationToken cancellationToken = default);

    /// <summary>Lists pull requests (gh pr list).</summary>
    /// <param name="state">Optional state filter (open, closed, all).</param>
    /// <param name="limit">Maximum number of PRs to return.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Pull request list result with success flag and items.</returns>
    Task<GitHubPullListResult> ListPullsAsync(string? state, int limit, CancellationToken cancellationToken = default);

    /// <summary>Creates an issue (gh issue create).</summary>
    /// <param name="title">Issue title.</param>
    /// <param name="body">Optional issue body.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Create result with the new issue number and URL.</returns>
    Task<GitHubCreateIssueResult> CreateIssueAsync(string title, string? body, CancellationToken cancellationToken = default);

    /// <summary>Adds a comment to an issue (gh issue comment).</summary>
    /// <param name="issueId">Issue number or identifier.</param>
    /// <param name="body">Comment body text.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Comment result indicating success or failure.</returns>
    Task<GitHubCommentResult> CommentOnIssueAsync(string issueId, string body, CancellationToken cancellationToken = default);

    /// <summary>Adds a comment to a PR (gh pr comment).</summary>
    /// <param name="prId">PR number or identifier.</param>
    /// <param name="body">Comment body text.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Comment result indicating success or failure.</returns>
    Task<GitHubCommentResult> CommentOnPullAsync(string prId, string body, CancellationToken cancellationToken = default);

    /// <summary>TR-GH-013-001: Gets full issue detail (gh issue view --json).</summary>
    /// <param name="issueNumber">Issue number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Issue detail result.</returns>
    Task<GitHubIssueDetailResult> GetIssueAsync(int issueNumber, CancellationToken ct = default);

    /// <summary>TR-GH-013-001: Updates issue metadata (gh issue edit).</summary>
    /// <param name="issueNumber">Issue number.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation result.</returns>
    Task<GitHubMutationResult> UpdateIssueAsync(int issueNumber, GitHubIssueUpdateRequest request, CancellationToken ct = default);

    /// <summary>TR-GH-013-001: Closes an issue (gh issue close).</summary>
    /// <param name="issueNumber">Issue number.</param>
    /// <param name="reason">Close reason (completed or not_planned). Default: completed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation result.</returns>
    Task<GitHubMutationResult> CloseIssueAsync(int issueNumber, string? reason = null, CancellationToken ct = default);

    /// <summary>TR-GH-013-001: Reopens an issue (gh issue reopen).</summary>
    /// <param name="issueNumber">Issue number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation result.</returns>
    Task<GitHubMutationResult> ReopenIssueAsync(int issueNumber, CancellationToken ct = default);

    /// <summary>TR-GH-013-001: Lists repository labels (gh label list --json).</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Labels result.</returns>
    Task<GitHubLabelsResult> ListIssueLabelsAsync(CancellationToken ct = default);

    /// <summary>TR-MCP-GH-004: Lists GitHub Actions workflow runs (gh run list --json).</summary>
    /// <param name="query">Optional workflow run filters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Workflow run list result.</returns>
    Task<GitHubWorkflowRunListResult> ListWorkflowRunsAsync(GitHubWorkflowRunQuery query, CancellationToken ct = default);

    /// <summary>TR-MCP-GH-004: Gets a single workflow run detail (gh run view --json).</summary>
    /// <param name="runId">Workflow run identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Workflow run detail result.</returns>
    Task<GitHubWorkflowRunDetailResult> GetWorkflowRunAsync(long runId, CancellationToken ct = default);

    /// <summary>TR-MCP-GH-004: Re-runs a workflow run (gh run rerun).</summary>
    /// <param name="runId">Workflow run identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation result.</returns>
    Task<GitHubMutationResult> RerunWorkflowRunAsync(long runId, CancellationToken ct = default);

    /// <summary>TR-MCP-GH-004: Cancels a running workflow run (gh run cancel).</summary>
    /// <param name="runId">Workflow run identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation result.</returns>
    Task<GitHubMutationResult> CancelWorkflowRunAsync(long runId, CancellationToken ct = default);
}

/// <summary>TR-PLANNED-013: Result of listing issues.</summary>
/// <param name="Success">Whether the gh command succeeded.</param>
/// <param name="Error">Error message if <paramref name="Success"/> is <see langword="false"/>.</param>
/// <param name="Issues">List of issue items.</param>
public sealed record GitHubIssueListResult(bool Success, string? Error, IReadOnlyList<GitHubIssueItem> Issues);

/// <summary>TR-PLANNED-013: Single issue item.</summary>
/// <param name="Number">Issue number.</param>
/// <param name="Title">Issue title.</param>
/// <param name="Url">Issue URL on GitHub.</param>
/// <param name="State">Issue state (open, closed).</param>
public sealed record GitHubIssueItem(int Number, string Title, string? Url, string? State);

/// <summary>TR-PLANNED-013: Result of listing PRs.</summary>
/// <param name="Success">Whether the gh command succeeded.</param>
/// <param name="Error">Error message if <paramref name="Success"/> is <see langword="false"/>.</param>
/// <param name="Pulls">List of pull request items.</param>
public sealed record GitHubPullListResult(bool Success, string? Error, IReadOnlyList<GitHubPullItem> Pulls);

/// <summary>TR-PLANNED-013: Single PR item.</summary>
/// <param name="Number">Pull request number.</param>
/// <param name="Title">Pull request title.</param>
/// <param name="Url">Pull request URL on GitHub.</param>
/// <param name="State">Pull request state (open, closed, merged).</param>
public sealed record GitHubPullItem(int Number, string Title, string? Url, string? State);

/// <summary>TR-PLANNED-013: Result of creating an issue.</summary>
/// <param name="Success">Whether the issue was created.</param>
/// <param name="Number">New issue number, or <see langword="null"/> on failure.</param>
/// <param name="Url">New issue URL, or <see langword="null"/> on failure.</param>
/// <param name="Error">Error message on failure.</param>
public sealed record GitHubCreateIssueResult(bool Success, int? Number, string? Url, string? Error);

/// <summary>TR-PLANNED-013: Result of adding a comment.</summary>
/// <param name="Success">Whether the comment was added.</param>
/// <param name="Error">Error message on failure.</param>
public sealed record GitHubCommentResult(bool Success, string? Error);

/// <summary>TR-MCP-GH-004: Query options when listing workflow runs.</summary>
public sealed record GitHubWorkflowRunQuery
{
    /// <summary>Optional branch filter.</summary>
    public string? Branch { get; init; }

    /// <summary>Optional run status filter.</summary>
    public string? Status { get; init; }

    /// <summary>Optional trigger event filter.</summary>
    public string? Event { get; init; }

    /// <summary>Optional workflow name filter.</summary>
    public string? Workflow { get; init; }

    /// <summary>Maximum runs to return. Default 30.</summary>
    public int Limit { get; init; } = 30;
}

/// <summary>TR-MCP-GH-004: Result of listing workflow runs.</summary>
/// <param name="Success">Whether the query succeeded.</param>
/// <param name="Runs">Workflow runs returned from GitHub.</param>
/// <param name="ErrorMessage">Error details on failure.</param>
public sealed record GitHubWorkflowRunListResult(bool Success, IReadOnlyList<GitHubWorkflowRunItem> Runs, string? ErrorMessage);

/// <summary>TR-MCP-GH-004: Summary of a workflow run.</summary>
/// <param name="RunId">Workflow run identifier.</param>
/// <param name="WorkflowName">Workflow name.</param>
/// <param name="DisplayTitle">Run display title.</param>
/// <param name="HeadBranch">Branch associated with the run.</param>
/// <param name="Status">Current run status.</param>
/// <param name="Conclusion">Final run conclusion, if available.</param>
/// <param name="Event">Triggering GitHub event.</param>
/// <param name="Url">Run URL.</param>
/// <param name="CreatedAt">Creation timestamp.</param>
/// <param name="UpdatedAt">Last update timestamp.</param>
public sealed record GitHubWorkflowRunItem(
    long RunId,
    string? WorkflowName,
    string? DisplayTitle,
    string? HeadBranch,
    string? Status,
    string? Conclusion,
    string? Event,
    string? Url,
    string? CreatedAt,
    string? UpdatedAt);

/// <summary>TR-MCP-GH-004: Result of reading a workflow run detail.</summary>
/// <param name="Success">Whether the query succeeded.</param>
/// <param name="Run">Detailed run payload on success.</param>
/// <param name="ErrorMessage">Error details on failure.</param>
public sealed record GitHubWorkflowRunDetailResult(bool Success, GitHubWorkflowRunDetail? Run, string? ErrorMessage);

/// <summary>TR-MCP-GH-004: Detailed workflow run payload.</summary>
/// <param name="RunId">Workflow run identifier.</param>
/// <param name="WorkflowName">Workflow name.</param>
/// <param name="DisplayTitle">Run display title.</param>
/// <param name="HeadBranch">Branch associated with the run.</param>
/// <param name="HeadSha">Commit SHA associated with the run.</param>
/// <param name="Status">Current run status.</param>
/// <param name="Conclusion">Final run conclusion, if available.</param>
/// <param name="Event">Triggering GitHub event.</param>
/// <param name="Url">Run URL.</param>
/// <param name="Attempt">Attempt number.</param>
/// <param name="CreatedAt">Creation timestamp.</param>
/// <param name="UpdatedAt">Last update timestamp.</param>
/// <param name="Jobs">Jobs executed as part of this run.</param>
public sealed record GitHubWorkflowRunDetail(
    long RunId,
    string? WorkflowName,
    string? DisplayTitle,
    string? HeadBranch,
    string? HeadSha,
    string? Status,
    string? Conclusion,
    string? Event,
    string? Url,
    int? Attempt,
    string? CreatedAt,
    string? UpdatedAt,
    IReadOnlyList<GitHubWorkflowRunJob> Jobs);

/// <summary>TR-MCP-GH-004: Workflow run job payload.</summary>
/// <param name="Name">Job name.</param>
/// <param name="Status">Job status.</param>
/// <param name="Conclusion">Job conclusion.</param>
/// <param name="StartedAt">Job start timestamp.</param>
/// <param name="CompletedAt">Job completion timestamp.</param>
/// <param name="Url">Job URL.</param>
/// <param name="Steps">Job step details.</param>
public sealed record GitHubWorkflowRunJob(
    string? Name,
    string? Status,
    string? Conclusion,
    string? StartedAt,
    string? CompletedAt,
    string? Url,
    IReadOnlyList<GitHubWorkflowRunJobStep> Steps);

/// <summary>TR-MCP-GH-004: Workflow job step payload.</summary>
/// <param name="Name">Step name.</param>
/// <param name="Status">Step status.</param>
/// <param name="Conclusion">Step conclusion.</param>
/// <param name="Number">Execution order.</param>
public sealed record GitHubWorkflowRunJobStep(
    string? Name,
    string? Status,
    string? Conclusion,
    int? Number);
