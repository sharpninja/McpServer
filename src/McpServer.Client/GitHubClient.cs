using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for GitHub integration endpoints (<c>/mcpserver/gh</c>). Provides issue and pull request
/// management, commenting, label listing, and bidirectional sync between GitHub issues and
/// workspace TODO items.
/// </summary>
/// <seealso cref="McpServerClient.GitHub"/>
public sealed class GitHubClient : McpClientBase
{
    /// <inheritdoc />
    public GitHubClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal GitHubClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>List GitHub issues.</summary>
    public async Task<GitHubIssueListResult> ListIssuesAsync(string? state = null, int limit = 30, CancellationToken cancellationToken = default)
    {
        var qs = BuildIssueListQuery(state, limit);
        return await GetAsync<GitHubIssueListResult>($"mcpserver/gh/issues{qs}", cancellationToken);
    }

    /// <summary>Get a single issue by number.</summary>
    public async Task<GitHubIssueDetail> GetIssueAsync(int number, CancellationToken cancellationToken = default)
    {
        return await GetAsync<GitHubIssueDetail>($"mcpserver/gh/issues/{number}", cancellationToken);
    }

    /// <summary>Create a new GitHub issue.</summary>
    public async Task<GitHubCreateIssueResult> CreateIssueAsync(GitHubIssueRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GitHubCreateIssueResult>("mcpserver/gh/issues", request, cancellationToken);
    }

    /// <summary>Update a GitHub issue.</summary>
    public async Task<GitHubMutationResult> UpdateIssueAsync(int number, GitHubIssueUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<GitHubMutationResult>($"mcpserver/gh/issues/{number}", request, cancellationToken);
    }

    /// <summary>Close a GitHub issue.</summary>
    public async Task<GitHubMutationResult> CloseIssueAsync(int number, string? reason = null, CancellationToken cancellationToken = default)
    {
        var qs = reason is not null ? $"?reason={Uri.EscapeDataString(reason)}" : string.Empty;
        return await PostAsync<GitHubMutationResult>($"mcpserver/gh/issues/{number}/close{qs}", null, cancellationToken);
    }

    /// <summary>Reopen a GitHub issue.</summary>
    public async Task<GitHubMutationResult> ReopenIssueAsync(int number, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GitHubMutationResult>($"mcpserver/gh/issues/{number}/reopen", null, cancellationToken);
    }

    /// <summary>Comment on a GitHub issue.</summary>
    public async Task<GitHubMutationResult> CommentOnIssueAsync(int number, string body, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GitHubMutationResult>($"mcpserver/gh/issues/{number}/comments", new GitHubCommentRequest { Body = body }, cancellationToken);
    }

    /// <summary>List repository labels.</summary>
    public async Task<GitHubLabelsResult> ListLabelsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<GitHubLabelsResult>("mcpserver/gh/labels", cancellationToken);
    }

    /// <summary>List pull requests.</summary>
    public async Task<GitHubPullListResult> ListPullsAsync(string? state = null, int limit = 30, CancellationToken cancellationToken = default)
    {
        var qs = BuildIssueListQuery(state, limit);
        return await GetAsync<GitHubPullListResult>($"mcpserver/gh/pulls{qs}", cancellationToken);
    }

    /// <summary>Comment on a pull request.</summary>
    public async Task<GitHubMutationResult> CommentOnPullAsync(int number, string body, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GitHubMutationResult>($"mcpserver/gh/pulls/{number}/comments", new GitHubCommentRequest { Body = body }, cancellationToken);
    }

    /// <summary>Sync issues from GitHub to TODO items.</summary>
    public async Task<IssueSyncResult> SyncFromGitHubAsync(string? state = "open", int limit = 30, CancellationToken cancellationToken = default)
    {
        var qs = BuildIssueListQuery(state, limit);
        return await PostAsync<IssueSyncResult>($"mcpserver/gh/issues/sync/from-github{qs}", null, cancellationToken);
    }

    /// <summary>Sync TODO items back to GitHub issues.</summary>
    public async Task<IssueSyncResult> SyncToGitHubAsync(CancellationToken cancellationToken = default)
    {
        return await PostAsync<IssueSyncResult>("mcpserver/gh/issues/sync/to-github", null, cancellationToken);
    }

    /// <summary>Sync a single issue by number.</summary>
    public async Task<SingleIssueSyncResult> SyncIssueAsync(int number, string direction = "from-github", CancellationToken cancellationToken = default)
    {
        var qs = $"?direction={Uri.EscapeDataString(direction)}";
        return await PostAsync<SingleIssueSyncResult>($"mcpserver/gh/issues/{number}/sync{qs}", null, cancellationToken);
    }

    /// <summary>Get GitHub auth status for the active workspace.</summary>
    public async Task<GitHubAuthStatusResult> GetAuthStatusAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<GitHubAuthStatusResult>("mcpserver/gh/auth/status", cancellationToken);
    }

    /// <summary>Set or replace the workspace GitHub token.</summary>
    public async Task<GitHubOperationResult> SetAuthTokenAsync(GitHubAuthTokenUpsertRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<GitHubOperationResult>("mcpserver/gh/auth/token", request, cancellationToken);
    }

    /// <summary>Delete the workspace GitHub token.</summary>
    public async Task<GitHubOperationResult> DeleteAuthTokenAsync(CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<GitHubOperationResult>("mcpserver/gh/auth/token", cancellationToken);
    }

    /// <summary>Get OAuth app bootstrap configuration.</summary>
    public async Task<GitHubOAuthConfigResult> GetOAuthConfigAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<GitHubOAuthConfigResult>("mcpserver/gh/oauth/config", cancellationToken);
    }

    /// <summary>Build a GitHub OAuth authorize URL from server-side configuration.</summary>
    public async Task<GitHubAuthorizeUrlResult> GetAuthorizeUrlAsync(string? state = null, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(state)
            ? "mcpserver/gh/oauth/authorize-url"
            : $"mcpserver/gh/oauth/authorize-url?state={Uri.EscapeDataString(state)}";
        return await GetAsync<GitHubAuthorizeUrlResult>(path, cancellationToken);
    }

    /// <summary>List workflow runs.</summary>
    public async Task<GitHubWorkflowRunListResult> ListWorkflowRunsAsync(
        string? branch = null,
        string? status = null,
        string? eventName = null,
        string? workflow = null,
        int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildWorkflowRunQuery(branch, status, eventName, workflow, limit);
        return await GetAsync<GitHubWorkflowRunListResult>($"mcpserver/gh/actions/runs{qs}", cancellationToken);
    }

    /// <summary>Get workflow run details.</summary>
    public async Task<GitHubWorkflowRunDetail> GetWorkflowRunAsync(long runId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<GitHubWorkflowRunDetail>($"mcpserver/gh/actions/runs/{runId}", cancellationToken);
    }

    /// <summary>Request a rerun of a workflow run.</summary>
    public async Task<GitHubOperationResult> RerunWorkflowRunAsync(long runId, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GitHubOperationResult>($"mcpserver/gh/actions/runs/{runId}/rerun", null, cancellationToken);
    }

    /// <summary>Cancel a workflow run.</summary>
    public async Task<GitHubOperationResult> CancelWorkflowRunAsync(long runId, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GitHubOperationResult>($"mcpserver/gh/actions/runs/{runId}/cancel", null, cancellationToken);
    }

    private static string BuildIssueListQuery(string? state, int limit)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (state is not null) parts.Add($"state={Uri.EscapeDataString(state)}");
        if (limit != 30) parts.Add($"limit={limit}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }

    private static string BuildWorkflowRunQuery(string? branch, string? status, string? eventName, string? workflow, int limit)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (branch is not null) parts.Add($"branch={Uri.EscapeDataString(branch)}");
        if (status is not null) parts.Add($"status={Uri.EscapeDataString(status)}");
        if (eventName is not null) parts.Add($"event={Uri.EscapeDataString(eventName)}");
        if (workflow is not null) parts.Add($"workflow={Uri.EscapeDataString(workflow)}");
        if (limit != 30) parts.Add($"limit={limit}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }
}
