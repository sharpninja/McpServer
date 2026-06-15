using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-161: Verifies persisted GitHub token writes fail closed while
/// required turn transactions are active.
/// </summary>
public sealed class TransactionGatedGitHubWorkspaceTokenStoreTests
{
    /// <summary>
    /// TEST-MCP-161: Token upsert and delete reject before mutating storage when
    /// external GitHub auth state is not transaction compensated.
    /// </summary>
    [Fact]
    public async Task Mutations_WhenTransactionsRequired_ThrowWithoutCallingInner()
    {
        var inner = Substitute.For<IGitHubWorkspaceTokenStore>();
        var sut = CreateSut(inner, new CapturingCoordinator());

        var upsert = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.UpsertAsync(@"F:\GitHub\McpServer", "token"))
            .ConfigureAwait(true);
        var delete = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.DeleteAsync(@"F:\GitHub\McpServer"))
            .ConfigureAwait(true);

        Assert.Contains("not transaction compensated", upsert.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not transaction compensated", delete.Message, StringComparison.OrdinalIgnoreCase);
        await inner.DidNotReceive().UpsertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Token reads delegate and do not allocate turn transactions.
    /// </summary>
    [Fact]
    public async Task GetAsync_DelegatesWithoutCoordinatorTransaction()
    {
        var inner = Substitute.For<IGitHubWorkspaceTokenStore>();
        inner.GetAsync(@"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .Returns(new GitHubWorkspaceTokenRecord(@"F:\GitHub\McpServer", "token", DateTimeOffset.UtcNow, null));
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var record = await sut.GetAsync(@"F:\GitHub\McpServer").ConfigureAwait(true);

        Assert.NotNull(record);
        Assert.Null(coordinator.Request);
        await inner.Received(1).GetAsync(@"F:\GitHub\McpServer", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Degraded transaction security fails closed with the
    /// coordinator message.
    /// </summary>
    [Fact]
    public async Task UpsertAsync_WhenCoordinatorDegraded_ThrowsCoordinatorFailure()
    {
        var inner = Substitute.For<IGitHubWorkspaceTokenStore>();
        var sut = CreateSut(inner, new CapturingCoordinator(degraded: true, message: "txn degraded"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.UpsertAsync(@"F:\GitHub\McpServer", "token"))
            .ConfigureAwait(true);

        Assert.Equal("txn degraded", exception.Message);
        await inner.DidNotReceive().UpsertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Non-required transaction mode preserves direct token store
    /// mutation behavior.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WhenTransactionsNotRequired_DelegatesToInner()
    {
        var inner = Substitute.For<IGitHubWorkspaceTokenStore>();
        inner.DeleteAsync(@"F:\GitHub\McpServer", Arg.Any<CancellationToken>()).Returns(true);
        var sut = CreateSut(
            inner,
            new CapturingCoordinator(),
            new TurnTransactionOptions { Enabled = true, RequiredForMutations = false });

        var removed = await sut.DeleteAsync(@"F:\GitHub\McpServer").ConfigureAwait(true);

        Assert.True(removed);
        await inner.Received(1).DeleteAsync(@"F:\GitHub\McpServer", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static TransactionGatedGitHubWorkspaceTokenStore CreateSut(
        IGitHubWorkspaceTokenStore inner,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? options = null)
        => new(
            inner,
            coordinator,
            MsOptions.Options.Create(options ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

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
                TransactionId = request.TransactionId ?? "txn-github-token-test",
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
