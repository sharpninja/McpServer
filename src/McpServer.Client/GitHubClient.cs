using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>Client for GitHub integration endpoints (/mcp/gh).</summary>
public sealed class GitHubClient : McpClientBase
{
    /// <summary>Initializes a new instance of <see cref="GitHubClient"/>.</summary>
    public GitHubClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    /// <summary>List GitHub issues.</summary>
    public async Task<GitHubIssueListResult> ListIssuesAsync(string? state = null, int limit = 30, CancellationToken cancellationToken = default)
    {
        var qs = BuildIssueListQuery(state, limit);
        return await GetAsync<GitHubIssueListResult>($"mcp/gh/issues{qs}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Get a single issue by number.</summary>
    public async Task<GitHubIssueDetail> GetIssueAsync(int number, CancellationToken cancellationToken = default)
    {
        return await GetAsync<GitHubIssueDetail>($"mcp/gh/issues/{number}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Create a new GitHub issue.</summary>
    public async Task<GitHubCreateIssueResult> CreateIssueAsync(GitHubIssueRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GitHubCreateIssueResult>("mcp/gh/issues", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Update a GitHub issue.</summary>
    public async Task<GitHubMutationResult> UpdateIssueAsync(int number, GitHubIssueUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<GitHubMutationResult>($"mcp/gh/issues/{number}", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Close a GitHub issue.</summary>
    public async Task<GitHubMutationResult> CloseIssueAsync(int number, string? reason = null, CancellationToken cancellationToken = default)
    {
        var qs = reason is not null ? $"?reason={Uri.EscapeDataString(reason)}" : string.Empty;
        return await PostAsync<GitHubMutationResult>($"mcp/gh/issues/{number}/close{qs}", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reopen a GitHub issue.</summary>
    public async Task<GitHubMutationResult> ReopenIssueAsync(int number, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GitHubMutationResult>($"mcp/gh/issues/{number}/reopen", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Comment on a GitHub issue.</summary>
    public async Task<GitHubMutationResult> CommentOnIssueAsync(int number, string body, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GitHubMutationResult>($"mcp/gh/issues/{number}/comments", new GitHubCommentRequest { Body = body }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>List repository labels.</summary>
    public async Task<GitHubLabelsResult> ListLabelsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<GitHubLabelsResult>("mcp/gh/labels", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>List pull requests.</summary>
    public async Task<GitHubPullListResult> ListPullsAsync(string? state = null, int limit = 30, CancellationToken cancellationToken = default)
    {
        var qs = BuildIssueListQuery(state, limit);
        return await GetAsync<GitHubPullListResult>($"mcp/gh/pulls{qs}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Comment on a pull request.</summary>
    public async Task<GitHubMutationResult> CommentOnPullAsync(int number, string body, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GitHubMutationResult>($"mcp/gh/pulls/{number}/comments", new GitHubCommentRequest { Body = body }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sync issues from GitHub to TODO items.</summary>
    public async Task<IssueSyncResult> SyncFromGitHubAsync(string? state = "open", int limit = 30, CancellationToken cancellationToken = default)
    {
        var qs = BuildIssueListQuery(state, limit);
        return await PostAsync<IssueSyncResult>($"mcp/gh/issues/sync/from-github{qs}", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sync TODO items back to GitHub issues.</summary>
    public async Task<IssueSyncResult> SyncToGitHubAsync(CancellationToken cancellationToken = default)
    {
        return await PostAsync<IssueSyncResult>("mcp/gh/issues/sync/to-github", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sync a single issue by number.</summary>
    public async Task<SingleIssueSyncResult> SyncIssueAsync(int number, string direction = "from-github", CancellationToken cancellationToken = default)
    {
        var qs = $"?direction={Uri.EscapeDataString(direction)}";
        return await PostAsync<SingleIssueSyncResult>($"mcp/gh/issues/{number}/sync{qs}", null, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildIssueListQuery(string? state, int limit)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (state is not null) parts.Add($"state={Uri.EscapeDataString(state)}");
        if (limit != 30) parts.Add($"limit={limit}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }
}
