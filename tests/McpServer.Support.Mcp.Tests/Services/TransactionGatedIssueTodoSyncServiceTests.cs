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
/// TEST-MCP-161: Verifies bidirectional GitHub issue/TODO sync writes fail
/// closed while required turn transactions are active.
/// </summary>
public sealed class TransactionGatedIssueTodoSyncServiceTests
{
    /// <summary>
    /// TEST-MCP-161: All issue/TODO sync mutations reject before invoking the
    /// underlying sync implementation.
    /// </summary>
    [Fact]
    public async Task Mutations_WhenTransactionsRequired_ReturnDeferredFailuresWithoutCallingInner()
    {
        var inner = Substitute.For<IIssueTodoSyncService>();
        var sut = CreateSut(inner, new CapturingCoordinator());
        var previous = CreateTodo("ISSUE-42", "Before");
        var current = CreateTodo("ISSUE-42", "After");

        var issueToTodo = await sut.SyncIssueToTodoAsync(CreateIssue(42), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var allIssues = await sut.SyncAllIssuesToTodosAsync("open", 30, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var todoToIssue = await sut.SyncTodoToIssueAsync("ISSUE-42", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var comment = await sut.CommentOnTodoUpdateAsync(previous, current, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var allTodos = await sut.SyncAllTodosToIssuesAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(issueToTodo.Success);
        Assert.Equal(TodoMutationFailureKind.ExternalSyncFailed, issueToTodo.FailureKind);
        Assert.Equal(1, allIssues.Failed);
        Assert.False(todoToIssue.Success);
        Assert.False(comment.Success);
        Assert.Equal(1, allTodos.Failed);
        Assert.Contains("not transaction compensated", issueToTodo.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transaction compensated", allIssues.Errors.Single(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transaction compensated", todoToIssue.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transaction compensated", comment.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transaction compensated", allTodos.Errors.Single(), StringComparison.OrdinalIgnoreCase);
        await inner.DidNotReceive().SyncIssueToTodoAsync(Arg.Any<GitHubIssueDetail>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().SyncAllIssuesToTodosAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().SyncTodoToIssueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().CommentOnTodoUpdateAsync(Arg.Any<TodoFlatItem>(), Arg.Any<TodoFlatItem>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().SyncAllTodosToIssuesAsync(Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Degraded transaction security fails closed with the
    /// coordinator message.
    /// </summary>
    [Fact]
    public async Task SyncTodoToIssueAsync_WhenCoordinatorDegraded_ReturnsCoordinatorFailure()
    {
        var inner = Substitute.For<IIssueTodoSyncService>();
        var sut = CreateSut(inner, new CapturingCoordinator(degraded: true, message: "txn degraded"));

        var result = await sut.SyncTodoToIssueAsync("ISSUE-42", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("txn degraded", result.ErrorMessage);
        await inner.DidNotReceive().SyncTodoToIssueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Non-required transaction mode preserves direct issue sync
    /// behavior.
    /// </summary>
    [Fact]
    public async Task SyncAllTodosToIssuesAsync_WhenTransactionsNotRequired_DelegatesToInner()
    {
        var inner = Substitute.For<IIssueTodoSyncService>();
        inner.SyncAllTodosToIssuesAsync(Arg.Any<CancellationToken>())
            .Returns(new IssueSyncResult { Synced = 1 });
        var sut = CreateSut(
            inner,
            new CapturingCoordinator(),
            new TurnTransactionOptions { Enabled = true, RequiredForMutations = false });

        var result = await sut.SyncAllTodosToIssuesAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(1, result.Synced);
        await inner.Received(1).SyncAllTodosToIssuesAsync(Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static TransactionGatedIssueTodoSyncService CreateSut(
        IIssueTodoSyncService inner,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? options = null)
        => new(
            inner,
            coordinator,
            MsOptions.Options.Create(options ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

    private static GitHubIssueDetail CreateIssue(int number)
        => new(number, "issue", "body", "OPEN", "url", [], [], null, null, null, null, "author", []);

    private static TodoFlatItem CreateTodo(string id, string title)
        => new()
        {
            Id = id,
            Title = title,
            Section = "github",
            Priority = "medium",
            Done = false,
        };

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
                TransactionId = request.TransactionId ?? "txn-issue-sync-test",
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
