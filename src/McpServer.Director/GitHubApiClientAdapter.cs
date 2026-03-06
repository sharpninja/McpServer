using McpServer.Client;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="IGitHubApiClient"/> backed by <see cref="McpServerClient"/>.
/// </summary>
internal sealed class GitHubApiClientAdapter : IGitHubApiClient
{
    private readonly DirectorMcpContext _context;
    private readonly ILogger<GitHubApiClientAdapter> _logger;

    public GitHubApiClientAdapter(
        DirectorMcpContext context,
        ILogger<GitHubApiClientAdapter>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<GitHubApiClientAdapter>.Instance;
    }

    public async Task<GitHubIssueListResult> ListIssuesAsync(ListIssuesQuery query, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.GitHub.ListIssuesAsync(query.State, query.Limit, cancellationToken).ConfigureAwait(true);
        return new GitHubIssueListResult(result.Issues.Select(MapIssueSummary).ToList(), result.Error);
    }

    public async Task<GitHubIssueDetail?> GetIssueAsync(int number, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
            var result = await client.GitHub.GetIssueAsync(number, cancellationToken).ConfigureAwait(true);
            return MapIssueDetail(result);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<GitHubCreateIssueOutcome> CreateIssueAsync(CreateIssueCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.GitHub.CreateIssueAsync(
            new McpServer.Client.Models.GitHubIssueRequest
            {
                Title = command.Title,
                Body = command.Body
            },
            cancellationToken).ConfigureAwait(true);
        return new GitHubCreateIssueOutcome(result.Number, result.Url);
    }

    public async Task<GitHubMutationOutcome> UpdateIssueAsync(UpdateIssueCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.GitHub.UpdateIssueAsync(
            command.Number,
            new McpServer.Client.Models.GitHubIssueUpdateRequest
            {
                Title = command.Title,
                Body = command.Body,
                AddLabels = command.AddLabels,
                RemoveLabels = command.RemoveLabels,
                AddAssignees = command.AddAssignees,
                RemoveAssignees = command.RemoveAssignees,
                Milestone = command.Milestone
            },
            cancellationToken).ConfigureAwait(true);
        return MapMutation(result);
    }

    public async Task<GitHubMutationOutcome> CloseIssueAsync(CloseIssueCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.GitHub.CloseIssueAsync(command.Number, command.Reason, cancellationToken).ConfigureAwait(true);
        return MapMutation(result);
    }

    public async Task<GitHubMutationOutcome> ReopenIssueAsync(ReopenIssueCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.GitHub.ReopenIssueAsync(command.Number, cancellationToken).ConfigureAwait(true);
        return MapMutation(result);
    }

    public async Task<GitHubMutationOutcome> CommentOnIssueAsync(CommentOnIssueCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.GitHub.CommentOnIssueAsync(command.Number, command.Body, cancellationToken).ConfigureAwait(true);
        return MapMutation(result);
    }

    public async Task<GitHubLabelsResult> ListLabelsAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.GitHub.ListLabelsAsync(cancellationToken).ConfigureAwait(true);
        return new GitHubLabelsResult(
            (result.Labels ?? []).Select(MapLabel).ToList(),
            result.Error);
    }

    public async Task<GitHubPullListResult> ListPullsAsync(ListPullsQuery query, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.GitHub.ListPullsAsync(query.State, query.Limit, cancellationToken).ConfigureAwait(true);
        return new GitHubPullListResult(
            result.Pulls.Select(p => new GitHubPullSummary(p.Number, p.Title, p.State, p.Url)).ToList(),
            result.Error);
    }

    public async Task<GitHubMutationOutcome> CommentOnPullAsync(CommentOnPullCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.GitHub.CommentOnPullAsync(command.Number, command.Body, cancellationToken).ConfigureAwait(true);
        return MapMutation(result);
    }

    public async Task<GitHubSyncOutcome> SyncFromGitHubAsync(SyncFromGitHubCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.GitHub.SyncFromGitHubAsync(command.State, command.Limit, cancellationToken).ConfigureAwait(true);
        return new GitHubSyncOutcome(result.Synced, result.Skipped, result.Failed, result.Errors);
    }

    public async Task<GitHubSyncOutcome> SyncToGitHubAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.GitHub.SyncToGitHubAsync(cancellationToken).ConfigureAwait(true);
        return new GitHubSyncOutcome(result.Synced, result.Skipped, result.Failed, result.Errors);
    }

    public async Task<GitHubSingleIssueSyncOutcome> SyncSingleIssueAsync(SyncSingleIssueCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.GitHub.SyncIssueAsync(command.Number, command.Direction, cancellationToken).ConfigureAwait(true);
        return new GitHubSingleIssueSyncOutcome(result.Success, result.Url, result.TodoId);
    }

    private async Task<McpServerClient> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_context.HasControlConnection)
            return await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(true);
        return await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(true);
    }

    private static GitHubIssueSummary MapIssueSummary(McpServer.Client.Models.GitHubIssueItem issue)
        => new(issue.Number, issue.Title, issue.State, issue.Url);

    private static GitHubIssueDetail MapIssueDetail(McpServer.Client.Models.GitHubIssueDetail issue)
        => new(
            issue.Number,
            issue.Title,
            issue.Body,
            issue.State,
            issue.Url,
            issue.Labels.Select(MapLabel).ToList(),
            issue.Assignees,
            issue.Milestone,
            issue.CreatedAt,
            issue.UpdatedAt,
            issue.ClosedAt,
            issue.Author,
            issue.Comments.Select(MapComment).ToList());

    private static GitHubLabelInfo MapLabel(McpServer.Client.Models.GitHubLabel label)
        => new(label.Name, label.Color, label.Description);

    private static GitHubIssueCommentInfo MapComment(McpServer.Client.Models.GitHubIssueComment comment)
        => new(comment.Author, comment.Body, comment.CreatedAt);

    private static GitHubMutationOutcome MapMutation(McpServer.Client.Models.GitHubMutationResult result)
        => new(result.Success, result.Url, result.ErrorMessage);
}
