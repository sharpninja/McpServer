using McpServer.Support.Mcp.Models;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Fails closed for GitHub CLI mutations while required turn
/// transactions are active because remote GitHub side effects are not compensated.
/// </summary>
public sealed class TransactionGatedGitHubCliService : IGitHubCliService
{
    private const string DeferredGitHubMutationMessage =
        "GitHub mutations are not transaction compensated while required turn transactions are active.";

    private readonly IGitHubCliService _inner;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedGitHubCliService"/> class.</summary>
    /// <param name="inner">Underlying GitHub CLI service.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction enforcement options.</param>
    public TransactionGatedGitHubCliService(
        IGitHubCliService inner,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<GitHubIssueListResult> ListIssuesAsync(string? state, int limit, CancellationToken cancellationToken = default)
        => _inner.ListIssuesAsync(state, limit, cancellationToken);

    /// <inheritdoc />
    public Task<GitHubPullListResult> ListPullsAsync(string? state, int limit, CancellationToken cancellationToken = default)
        => _inner.ListPullsAsync(state, limit, cancellationToken);

    /// <inheritdoc />
    public Task<GitHubCreateIssueResult> CreateIssueAsync(string title, string? body, CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new GitHubCreateIssueResult(false, null, null, error));

        return _inner.CreateIssueAsync(title, body, cancellationToken);
    }

    /// <inheritdoc />
    public Task<GitHubCommentResult> CommentOnIssueAsync(string issueId, string body, CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new GitHubCommentResult(false, error));

        return _inner.CommentOnIssueAsync(issueId, body, cancellationToken);
    }

    /// <inheritdoc />
    public Task<GitHubCommentResult> CommentOnPullAsync(string prId, string body, CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new GitHubCommentResult(false, error));

        return _inner.CommentOnPullAsync(prId, body, cancellationToken);
    }

    /// <inheritdoc />
    public Task<GitHubIssueDetailResult> GetIssueAsync(int issueNumber, CancellationToken ct = default)
        => _inner.GetIssueAsync(issueNumber, ct);

    /// <inheritdoc />
    public Task<GitHubMutationResult> UpdateIssueAsync(
        int issueNumber,
        GitHubIssueUpdateRequest request,
        CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new GitHubMutationResult(false, null, error));

        return _inner.UpdateIssueAsync(issueNumber, request, ct);
    }

    /// <inheritdoc />
    public Task<GitHubMutationResult> CloseIssueAsync(int issueNumber, string? reason = null, CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new GitHubMutationResult(false, null, error));

        return _inner.CloseIssueAsync(issueNumber, reason, ct);
    }

    /// <inheritdoc />
    public Task<GitHubMutationResult> ReopenIssueAsync(int issueNumber, CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new GitHubMutationResult(false, null, error));

        return _inner.ReopenIssueAsync(issueNumber, ct);
    }

    /// <inheritdoc />
    public Task<GitHubLabelsResult> ListIssueLabelsAsync(CancellationToken ct = default)
        => _inner.ListIssueLabelsAsync(ct);

    /// <inheritdoc />
    public Task<GitHubWorkflowRunListResult> ListWorkflowRunsAsync(GitHubWorkflowRunQuery query, CancellationToken ct = default)
        => _inner.ListWorkflowRunsAsync(query, ct);

    /// <inheritdoc />
    public Task<GitHubWorkflowRunDetailResult> GetWorkflowRunAsync(long runId, CancellationToken ct = default)
        => _inner.GetWorkflowRunAsync(runId, ct);

    /// <inheritdoc />
    public Task<GitHubMutationResult> RerunWorkflowRunAsync(long runId, CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new GitHubMutationResult(false, null, error));

        return _inner.RerunWorkflowRunAsync(runId, ct);
    }

    /// <inheritdoc />
    public Task<GitHubMutationResult> CancelWorkflowRunAsync(long runId, CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new GitHubMutationResult(false, null, error));

        return _inner.CancelWorkflowRunAsync(runId, ct);
    }

    private bool ShouldDeferMutation(out string error)
    {
        error = string.Empty;
        if (_coordinator is null)
            return false;

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            error = string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message;
            return true;
        }

        if (!RequiresMutationTransactions(status))
            return false;

        error = DeferredGitHubMutationMessage;
        return true;
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);
}
