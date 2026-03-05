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

/// <summary>GitHub auth status for the active workspace.</summary>
public sealed class GitHubAuthStatusResult
{
    /// <summary>Resolved workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Current auth mode (stored_token, cli_fallback, or none).</summary>
    [JsonPropertyName("authMode")]
    public string AuthMode { get; set; } = string.Empty;

    /// <summary>Whether a workspace token is stored.</summary>
    [JsonPropertyName("hasStoredToken")]
    public bool HasStoredToken { get; set; }

    /// <summary>Stored token update timestamp.</summary>
    [JsonPropertyName("tokenUpdatedAtUtc")]
    public DateTimeOffset? TokenUpdatedAtUtc { get; set; }

    /// <summary>Stored token expiration timestamp.</summary>
    [JsonPropertyName("tokenExpiresAtUtc")]
    public DateTimeOffset? TokenExpiresAtUtc { get; set; }

    /// <summary>Whether CLI fallback auth is allowed when no stored token exists.</summary>
    [JsonPropertyName("cliFallbackAllowed")]
    public bool CliFallbackAllowed { get; set; }

    /// <summary>Whether OAuth bootstrap settings are configured.</summary>
    [JsonPropertyName("oauthConfigured")]
    public bool OAuthConfigured { get; set; }
}

/// <summary>Request body for setting a workspace GitHub token.</summary>
public sealed class GitHubAuthTokenUpsertRequest
{
    /// <summary>OAuth access token or PAT.</summary>
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Optional token expiration timestamp.</summary>
    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}

/// <summary>GitHub OAuth app bootstrap configuration.</summary>
public sealed class GitHubOAuthConfigResult
{
    /// <summary>GitHub OAuth app client ID.</summary>
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth redirect URI.</summary>
    [JsonPropertyName("redirectUri")]
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>OAuth scopes string.</summary>
    [JsonPropertyName("scopes")]
    public string Scopes { get; set; } = string.Empty;

    /// <summary>OAuth authorize endpoint.</summary>
    [JsonPropertyName("authorizeEndpoint")]
    public string AuthorizeEndpoint { get; set; } = string.Empty;

    /// <summary>Whether OAuth values are configured.</summary>
    [JsonPropertyName("isConfigured")]
    public bool IsConfigured { get; set; }
}

/// <summary>Authorize URL payload.</summary>
public sealed class GitHubAuthorizeUrlResult
{
    /// <summary>Fully-composed authorize URL.</summary>
    [JsonPropertyName("authorizeUrl")]
    public string AuthorizeUrl { get; set; } = string.Empty;
}

/// <summary>Result of listing workflow runs.</summary>
public sealed class GitHubWorkflowRunListResult
{
    /// <summary>Workflow runs returned from GitHub.</summary>
    [JsonPropertyName("runs")]
    public IReadOnlyList<GitHubWorkflowRunItem> Runs { get; set; } = [];

    /// <summary>Error message from the request, if any.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>Summary of a workflow run.</summary>
public sealed class GitHubWorkflowRunItem
{
    /// <summary>Workflow run identifier.</summary>
    [JsonPropertyName("runId")]
    public long RunId { get; set; }

    /// <summary>Workflow name.</summary>
    [JsonPropertyName("workflowName")]
    public string? WorkflowName { get; set; }

    /// <summary>Workflow run title.</summary>
    [JsonPropertyName("displayTitle")]
    public string? DisplayTitle { get; set; }

    /// <summary>Head branch.</summary>
    [JsonPropertyName("headBranch")]
    public string? HeadBranch { get; set; }

    /// <summary>Run status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Run conclusion.</summary>
    [JsonPropertyName("conclusion")]
    public string? Conclusion { get; set; }

    /// <summary>Trigger event name.</summary>
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    /// <summary>Workflow run URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Creation timestamp.</summary>
    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    /// <summary>Last update timestamp.</summary>
    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }
}

/// <summary>Detailed workflow run payload.</summary>
public sealed class GitHubWorkflowRunDetail
{
    /// <summary>Workflow run identifier.</summary>
    [JsonPropertyName("runId")]
    public long RunId { get; set; }

    /// <summary>Workflow name.</summary>
    [JsonPropertyName("workflowName")]
    public string? WorkflowName { get; set; }

    /// <summary>Workflow run title.</summary>
    [JsonPropertyName("displayTitle")]
    public string? DisplayTitle { get; set; }

    /// <summary>Head branch.</summary>
    [JsonPropertyName("headBranch")]
    public string? HeadBranch { get; set; }

    /// <summary>Head SHA.</summary>
    [JsonPropertyName("headSha")]
    public string? HeadSha { get; set; }

    /// <summary>Run status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Run conclusion.</summary>
    [JsonPropertyName("conclusion")]
    public string? Conclusion { get; set; }

    /// <summary>Trigger event name.</summary>
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    /// <summary>Workflow run URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Attempt number.</summary>
    [JsonPropertyName("attempt")]
    public int? Attempt { get; set; }

    /// <summary>Creation timestamp.</summary>
    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    /// <summary>Last update timestamp.</summary>
    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }

    /// <summary>Workflow jobs.</summary>
    [JsonPropertyName("jobs")]
    public IReadOnlyList<GitHubWorkflowRunJob> Jobs { get; set; } = [];
}

/// <summary>Workflow run job payload.</summary>
public sealed class GitHubWorkflowRunJob
{
    /// <summary>Job name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Job status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Job conclusion.</summary>
    [JsonPropertyName("conclusion")]
    public string? Conclusion { get; set; }

    /// <summary>Job start timestamp.</summary>
    [JsonPropertyName("startedAt")]
    public string? StartedAt { get; set; }

    /// <summary>Job completion timestamp.</summary>
    [JsonPropertyName("completedAt")]
    public string? CompletedAt { get; set; }

    /// <summary>Job URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Job steps.</summary>
    [JsonPropertyName("steps")]
    public IReadOnlyList<GitHubWorkflowRunJobStep> Steps { get; set; } = [];
}

/// <summary>Workflow run job step payload.</summary>
public sealed class GitHubWorkflowRunJobStep
{
    /// <summary>Step name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Step status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Step conclusion.</summary>
    [JsonPropertyName("conclusion")]
    public string? Conclusion { get; set; }

    /// <summary>Step order number.</summary>
    [JsonPropertyName("number")]
    public int? Number { get; set; }
}

/// <summary>Simple operation result payload.</summary>
public sealed class GitHubOperationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error payload when the operation fails.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
