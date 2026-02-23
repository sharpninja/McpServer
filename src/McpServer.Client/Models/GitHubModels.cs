using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>A GitHub issue summary for list results.</summary>
public sealed class GitHubIssueItem
{
    /// <summary>Issue number.</summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>Issue title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Issue state (open, closed).</summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>Issue URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>Full GitHub issue detail.</summary>
public sealed class GitHubIssueDetail
{
    /// <summary>Issue number.</summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>Issue title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Issue body (markdown).</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>Issue state.</summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>Issue URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Labels.</summary>
    [JsonPropertyName("labels")]
    public IReadOnlyList<GitHubLabel> Labels { get; set; } = [];

    /// <summary>Assignee usernames.</summary>
    [JsonPropertyName("assignees")]
    public IReadOnlyList<string> Assignees { get; set; } = [];

    /// <summary>Milestone name.</summary>
    [JsonPropertyName("milestone")]
    public string? Milestone { get; set; }

    /// <summary>Created date.</summary>
    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    /// <summary>Last updated date.</summary>
    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }

    /// <summary>Closed date.</summary>
    [JsonPropertyName("closedAt")]
    public string? ClosedAt { get; set; }

    /// <summary>Author username.</summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>Issue comments.</summary>
    [JsonPropertyName("comments")]
    public IReadOnlyList<GitHubIssueComment> Comments { get; set; } = [];
}

/// <summary>A GitHub label.</summary>
public sealed class GitHubLabel
{
    /// <summary>Label name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Label color hex.</summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    /// <summary>Label description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>A comment on a GitHub issue.</summary>
public sealed class GitHubIssueComment
{
    /// <summary>Comment author.</summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>Comment body.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>Created date.</summary>
    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }
}

/// <summary>Request to create a GitHub issue.</summary>
public sealed class GitHubIssueRequest
{
    /// <summary>Issue title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Issue body.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}

/// <summary>Request to update a GitHub issue.</summary>
public sealed class GitHubIssueUpdateRequest
{
    /// <summary>Updated title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Updated body.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>Labels to add.</summary>
    [JsonPropertyName("addLabels")]
    public IReadOnlyList<string>? AddLabels { get; set; }

    /// <summary>Labels to remove.</summary>
    [JsonPropertyName("removeLabels")]
    public IReadOnlyList<string>? RemoveLabels { get; set; }

    /// <summary>Assignees to add.</summary>
    [JsonPropertyName("addAssignees")]
    public IReadOnlyList<string>? AddAssignees { get; set; }

    /// <summary>Assignees to remove.</summary>
    [JsonPropertyName("removeAssignees")]
    public IReadOnlyList<string>? RemoveAssignees { get; set; }

    /// <summary>Milestone name.</summary>
    [JsonPropertyName("milestone")]
    public string? Milestone { get; set; }
}

/// <summary>Request to comment on an issue or PR.</summary>
public sealed class GitHubCommentRequest
{
    /// <summary>Comment body.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}

/// <summary>Result of a GitHub issue list.</summary>
public sealed class GitHubIssueListResult
{
    /// <summary>Issues.</summary>
    [JsonPropertyName("issues")]
    public IReadOnlyList<GitHubIssueItem> Issues { get; set; } = [];

    /// <summary>Error message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>Result of a GitHub mutation.</summary>
public sealed class GitHubMutationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Resource URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Error message.</summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

/// <summary>Result of creating a GitHub issue.</summary>
public sealed class GitHubCreateIssueResult
{
    /// <summary>New issue number.</summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>Issue URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>Result of a GitHub labels query.</summary>
public sealed class GitHubLabelsResult
{
    /// <summary>Labels.</summary>
    [JsonPropertyName("labels")]
    public IReadOnlyList<GitHubLabel>? Labels { get; set; }

    /// <summary>Error message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>GitHub pull request summary.</summary>
public sealed class GitHubPullItem
{
    /// <summary>PR number.</summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>PR title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>PR state.</summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>PR URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>Result of a GitHub pull list.</summary>
public sealed class GitHubPullListResult
{
    /// <summary>Pull requests.</summary>
    [JsonPropertyName("pulls")]
    public IReadOnlyList<GitHubPullItem> Pulls { get; set; } = [];

    /// <summary>Error message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>Result of an issue sync operation.</summary>
public sealed class IssueSyncResult
{
    /// <summary>Number of items synced.</summary>
    [JsonPropertyName("synced")]
    public int Synced { get; set; }

    /// <summary>Number of items skipped.</summary>
    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }

    /// <summary>Number of items that failed.</summary>
    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    /// <summary>Error messages.</summary>
    [JsonPropertyName("errors")]
    public IReadOnlyList<string> Errors { get; set; } = [];
}

/// <summary>Result of syncing a single issue.</summary>
public sealed class SingleIssueSyncResult
{
    /// <summary>Whether the sync succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Issue URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>TODO item ID.</summary>
    [JsonPropertyName("todoId")]
    public string? TodoId { get; set; }
}
