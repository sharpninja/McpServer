using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-221 / FR-MCP-173: GitHub CLI writes skip coordinator/keyserver and
/// invoke the inner service even when turn transactions are required.
/// </summary>
public sealed class TransactionGatedGitHubCliServiceTests
{
    /// <summary>
    /// TEST-MCP-221: GitHub issue, comment, and workflow mutations invoke the
    /// inner gh CLI service even when turn transactions are required.
    /// </summary>
    [Fact]
    public async Task Mutations_WhenTransactionsRequired_DelegateToInner()
    {
        var inner = Substitute.For<IGitHubCliService>();
        inner.CreateIssueAsync("title", "body", Arg.Any<CancellationToken>())
            .Returns(new GitHubCreateIssueResult(true, 42, "url", null));
        inner.CommentOnIssueAsync("42", "comment", Arg.Any<CancellationToken>())
            .Returns(new GitHubCommentResult(true, null));
        inner.CommentOnPullAsync("43", "comment", Arg.Any<CancellationToken>())
            .Returns(new GitHubCommentResult(true, null));
        inner.UpdateIssueAsync(42, Arg.Any<GitHubIssueUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "url", null));
        inner.CloseIssueAsync(42, "completed", Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "url", null));
        inner.ReopenIssueAsync(42, Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "url", null));
        inner.RerunWorkflowRunAsync(1001, Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "url", null));
        inner.CancelWorkflowRunAsync(1001, Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "url", null));
        var sut = CreateSut(inner, new CapturingCoordinator());

        var create = await sut.CreateIssueAsync("title", "body", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var issueComment = await sut.CommentOnIssueAsync("42", "comment", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var pullComment = await sut.CommentOnPullAsync("43", "comment", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var update = await sut.UpdateIssueAsync(42, new GitHubIssueUpdateRequest { Title = "updated" }, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var close = await sut.CloseIssueAsync(42, "completed", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var reopen = await sut.ReopenIssueAsync(42, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var rerun = await sut.RerunWorkflowRunAsync(1001, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var cancel = await sut.CancelWorkflowRunAsync(1001, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(create.Success);
        Assert.True(issueComment.Success);
        Assert.True(pullComment.Success);
        Assert.True(update.Success);
        Assert.True(close.Success);
        Assert.True(reopen.Success);
        Assert.True(rerun.Success);
        Assert.True(cancel.Success);
        await inner.Received(1).CreateIssueAsync("title", "body", Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).CommentOnIssueAsync("42", "comment", Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).CommentOnPullAsync("43", "comment", Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).UpdateIssueAsync(42, Arg.Any<GitHubIssueUpdateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).CloseIssueAsync(42, "completed", Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).ReopenIssueAsync(42, Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).RerunWorkflowRunAsync(1001, Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).CancelWorkflowRunAsync(1001, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: GitHub reads delegate to the underlying service and do not
    /// allocate turn transactions.
    /// </summary>
    [Fact]
    public async Task Reads_DelegateWithoutCoordinatorTransaction()
    {
        var inner = Substitute.For<IGitHubCliService>();
        inner.ListIssuesAsync("open", 30, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueListResult(true, null, [new GitHubIssueItem(42, "issue", "url", "OPEN")]));
        inner.ListPullsAsync("open", 30, Arg.Any<CancellationToken>())
            .Returns(new GitHubPullListResult(true, null, [new GitHubPullItem(43, "pull", "url", "OPEN")]));
        inner.GetIssueAsync(42, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(true, CreateIssue(42), null));
        inner.ListIssueLabelsAsync(Arg.Any<CancellationToken>())
            .Returns(new GitHubLabelsResult(true, [new GitHubLabel("bug", "ffffff", null)], null));
        inner.ListWorkflowRunsAsync(Arg.Any<GitHubWorkflowRunQuery>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubWorkflowRunListResult(true, [CreateRun(1001)], null));
        inner.GetWorkflowRunAsync(1001, Arg.Any<CancellationToken>())
            .Returns(new GitHubWorkflowRunDetailResult(true, CreateRunDetail(1001), null));
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var issues = await sut.ListIssuesAsync("open", 30, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var pulls = await sut.ListPullsAsync("open", 30, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var issue = await sut.GetIssueAsync(42, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var labels = await sut.ListIssueLabelsAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var runs = await sut.ListWorkflowRunsAsync(new GitHubWorkflowRunQuery(), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var run = await sut.GetWorkflowRunAsync(1001, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(issues.Success);
        Assert.True(pulls.Success);
        Assert.True(issue.Success);
        Assert.True(labels.Success);
        Assert.True(runs.Success);
        Assert.True(run.Success);
        Assert.Null(coordinator.Request);
        await inner.Received(1).ListIssuesAsync("open", 30, Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).ListPullsAsync("open", 30, Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).GetIssueAsync(42, Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).ListIssueLabelsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).ListWorkflowRunsAsync(Arg.Any<GitHubWorkflowRunQuery>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).GetWorkflowRunAsync(1001, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-221: Degraded transaction security does not block GitHub CLI writes.
    /// </summary>
    [Fact]
    public async Task CloseIssueAsync_WhenCoordinatorDegraded_DelegatesToInner()
    {
        var inner = Substitute.For<IGitHubCliService>();
        inner.CloseIssueAsync(42, "completed", Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "url", null));
        var sut = CreateSut(inner, new CapturingCoordinator(degraded: true, message: "txn degraded"));

        var result = await sut.CloseIssueAsync(42, "completed", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await inner.Received(1).CloseIssueAsync(42, "completed", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Non-required transaction mode preserves direct GitHub
    /// mutation behavior.
    /// </summary>
    [Fact]
    public async Task CreateIssueAsync_WhenTransactionsNotRequired_DelegatesToInner()
    {
        var inner = Substitute.For<IGitHubCliService>();
        inner.CreateIssueAsync("title", "body", Arg.Any<CancellationToken>())
            .Returns(new GitHubCreateIssueResult(true, 42, "url", null));
        var sut = CreateSut(
            inner,
            new CapturingCoordinator(),
            new TurnTransactionOptions { Enabled = true, RequiredForMutations = false });

        var result = await sut.CreateIssueAsync("title", "body", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal(42, result.Number);
        await inner.Received(1).CreateIssueAsync("title", "body", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static TransactionGatedGitHubCliService CreateSut(
        IGitHubCliService inner,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? options = null)
        => new(
            inner,
            coordinator,
            MsOptions.Options.Create(options ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

    private static GitHubIssueDetail CreateIssue(int number)
        => new(number, "issue", "body", "OPEN", "url", [], [], null, null, null, null, "author", []);

    private static GitHubWorkflowRunItem CreateRun(long runId)
        => new(runId, "ci", "CI", "main", "completed", "success", "push", "url", "created", "updated");

    private static GitHubWorkflowRunDetail CreateRunDetail(long runId)
        => new(runId, "ci", "CI", "main", "sha", "completed", "success", "push", "url", 1, "created", "updated", []);

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        private readonly bool _degraded;
        private readonly string _message;

        public CapturingCoordinator(bool degraded = false, string message = "ready")
        {
            _degraded = degraded;
            _message = message;
        }

        public TurnTransactionRequest? Request { get; private set; }

        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new TurnTransactionResult
            {
                TransactionId = request.TransactionId ?? "txn-github-test",
                Status = "committed",
                Reason = TransactionFailureReason.None,
                MutationApplied = false,
            });
        }

        public TurnTransactionStatusResponse GetStatus()
            => new()
            {
                Enabled = true,
                Degraded = _degraded,
                LastReason = TransactionFailureReason.None,
                Message = _message,
            };
    }
}
