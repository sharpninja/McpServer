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
/// TEST-MCP-161: Verifies GitHub CLI writes fail closed while required turn
/// transactions are active because external GitHub side effects are not compensated.
/// </summary>
public sealed class TransactionGatedGitHubCliServiceTests
{
    /// <summary>
    /// TEST-MCP-161: GitHub issue, comment, and workflow mutations return
    /// failures before invoking the underlying gh CLI service.
    /// </summary>
    [Fact]
    public async Task Mutations_WhenTransactionsRequired_ReturnDeferredFailuresWithoutCallingInner()
    {
        var inner = Substitute.For<IGitHubCliService>();
        var sut = CreateSut(inner, new CapturingCoordinator());

        var create = await sut.CreateIssueAsync("title", "body").ConfigureAwait(true);
        var issueComment = await sut.CommentOnIssueAsync("42", "comment").ConfigureAwait(true);
        var pullComment = await sut.CommentOnPullAsync("43", "comment").ConfigureAwait(true);
        var update = await sut.UpdateIssueAsync(42, new GitHubIssueUpdateRequest { Title = "updated" }).ConfigureAwait(true);
        var close = await sut.CloseIssueAsync(42, "completed").ConfigureAwait(true);
        var reopen = await sut.ReopenIssueAsync(42).ConfigureAwait(true);
        var rerun = await sut.RerunWorkflowRunAsync(1001).ConfigureAwait(true);
        var cancel = await sut.CancelWorkflowRunAsync(1001).ConfigureAwait(true);

        Assert.False(create.Success);
        Assert.False(issueComment.Success);
        Assert.False(pullComment.Success);
        Assert.False(update.Success);
        Assert.False(close.Success);
        Assert.False(reopen.Success);
        Assert.False(rerun.Success);
        Assert.False(cancel.Success);
        Assert.Contains("not transaction compensated", create.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transaction compensated", issueComment.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transaction compensated", pullComment.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transaction compensated", update.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transaction compensated", close.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transaction compensated", reopen.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transaction compensated", rerun.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transaction compensated", cancel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        await inner.DidNotReceive().CreateIssueAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().CommentOnIssueAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().CommentOnPullAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().UpdateIssueAsync(Arg.Any<int>(), Arg.Any<GitHubIssueUpdateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().CloseIssueAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().ReopenIssueAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().RerunWorkflowRunAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().CancelWorkflowRunAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
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

        var issues = await sut.ListIssuesAsync("open", 30).ConfigureAwait(true);
        var pulls = await sut.ListPullsAsync("open", 30).ConfigureAwait(true);
        var issue = await sut.GetIssueAsync(42).ConfigureAwait(true);
        var labels = await sut.ListIssueLabelsAsync().ConfigureAwait(true);
        var runs = await sut.ListWorkflowRunsAsync(new GitHubWorkflowRunQuery()).ConfigureAwait(true);
        var run = await sut.GetWorkflowRunAsync(1001).ConfigureAwait(true);

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
    /// TEST-MCP-161: Degraded transaction security fails closed with the
    /// coordinator message.
    /// </summary>
    [Fact]
    public async Task CloseIssueAsync_WhenCoordinatorDegraded_ReturnsCoordinatorFailure()
    {
        var inner = Substitute.For<IGitHubCliService>();
        var sut = CreateSut(inner, new CapturingCoordinator(degraded: true, message: "txn degraded"));

        var result = await sut.CloseIssueAsync(42, "completed").ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("txn degraded", result.ErrorMessage);
        await inner.DidNotReceive().CloseIssueAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
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

        var result = await sut.CreateIssueAsync("title", "body").ConfigureAwait(true);

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
