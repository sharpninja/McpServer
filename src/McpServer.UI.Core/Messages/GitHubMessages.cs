using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query to list GitHub issues.</summary>
public sealed record ListIssuesQuery(string? State = null, int Limit = 30) : IQuery<GitHubIssueListResult>;

/// <summary>Query to load a single GitHub issue.</summary>
public sealed record GetIssueQuery(int Number) : IQuery<GitHubIssueDetail?>;

/// <summary>Command to create a GitHub issue.</summary>
public sealed record CreateIssueCommand : ICommand<GitHubCreateIssueOutcome>
{
    /// <summary>Issue title.</summary>
    public required string Title { get; init; }

    /// <summary>Issue body markdown.</summary>
    public string? Body { get; init; }
}

/// <summary>Command to update a GitHub issue.</summary>
public sealed record UpdateIssueCommand : ICommand<GitHubMutationOutcome>
{
    /// <summary>Issue number to update.</summary>
    public required int Number { get; init; }

    /// <summary>Updated title.</summary>
    public string? Title { get; init; }

    /// <summary>Updated body.</summary>
    public string? Body { get; init; }

    /// <summary>Labels to add.</summary>
    public IReadOnlyList<string>? AddLabels { get; init; }

    /// <summary>Labels to remove.</summary>
    public IReadOnlyList<string>? RemoveLabels { get; init; }

    /// <summary>Assignees to add.</summary>
    public IReadOnlyList<string>? AddAssignees { get; init; }

    /// <summary>Assignees to remove.</summary>
    public IReadOnlyList<string>? RemoveAssignees { get; init; }

    /// <summary>Milestone name.</summary>
    public string? Milestone { get; init; }
}

/// <summary>Command to close an issue.</summary>
public sealed record CloseIssueCommand(int Number, string? Reason = null) : ICommand<GitHubMutationOutcome>;

/// <summary>Command to reopen an issue.</summary>
public sealed record ReopenIssueCommand(int Number) : ICommand<GitHubMutationOutcome>;

/// <summary>Command to add an issue comment.</summary>
public sealed record CommentOnIssueCommand(int Number, string Body) : ICommand<GitHubMutationOutcome>;

/// <summary>Query to list repository labels.</summary>
public sealed record ListLabelsQuery : IQuery<GitHubLabelsResult>;

/// <summary>Query to list pull requests.</summary>
public sealed record ListPullsQuery(string? State = null, int Limit = 30) : IQuery<GitHubPullListResult>;

/// <summary>Command to add a pull request comment.</summary>
public sealed record CommentOnPullCommand(int Number, string Body) : ICommand<GitHubMutationOutcome>;

/// <summary>Command to synchronize issues from GitHub to TODO items.</summary>
public sealed record SyncFromGitHubCommand(string? State = "open", int Limit = 30) : ICommand<GitHubSyncOutcome>;

/// <summary>Command to synchronize TODO items to GitHub issues.</summary>
public sealed record SyncToGitHubCommand : ICommand<GitHubSyncOutcome>;

/// <summary>Command to synchronize a single issue in one direction.</summary>
public sealed record SyncSingleIssueCommand(int Number, string Direction = "from-github") : ICommand<GitHubSingleIssueSyncOutcome>;

/// <summary>Issue list result.</summary>
public sealed record GitHubIssueListResult(IReadOnlyList<GitHubIssueSummary> Items, string? Error);

/// <summary>Issue list summary item.</summary>
public sealed record GitHubIssueSummary(int Number, string Title, string? State, string? Url);

/// <summary>Issue detail result.</summary>
public sealed record GitHubIssueDetail(
    int Number,
    string Title,
    string? Body,
    string? State,
    string? Url,
    IReadOnlyList<GitHubLabelInfo> Labels,
    IReadOnlyList<string> Assignees,
    string? Milestone,
    string? CreatedAt,
    string? UpdatedAt,
    string? ClosedAt,
    string? Author,
    IReadOnlyList<GitHubIssueCommentInfo> Comments);

/// <summary>GitHub label.</summary>
public sealed record GitHubLabelInfo(string Name, string? Color, string? Description);

/// <summary>GitHub issue comment.</summary>
public sealed record GitHubIssueCommentInfo(string? Author, string? Body, string? CreatedAt);

/// <summary>Mutation result for issue/pull actions.</summary>
public sealed record GitHubMutationOutcome(bool Success, string? Url, string? ErrorMessage);

/// <summary>Create issue command result.</summary>
public sealed record GitHubCreateIssueOutcome(int Number, string? Url);

/// <summary>Label list result.</summary>
public sealed record GitHubLabelsResult(IReadOnlyList<GitHubLabelInfo> Labels, string? Error);

/// <summary>Pull request list result.</summary>
public sealed record GitHubPullListResult(IReadOnlyList<GitHubPullSummary> Items, string? Error);

/// <summary>Pull request list item.</summary>
public sealed record GitHubPullSummary(int Number, string Title, string? State, string? Url);

/// <summary>Bulk sync result.</summary>
public sealed record GitHubSyncOutcome(int Synced, int Skipped, int Failed, IReadOnlyList<string> Errors);

/// <summary>Single issue sync result.</summary>
public sealed record GitHubSingleIssueSyncOutcome(bool Success, string? Url, string? TodoId);
