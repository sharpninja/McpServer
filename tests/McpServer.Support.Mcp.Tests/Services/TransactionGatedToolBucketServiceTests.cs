using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-161: Bucket orchestration mutations fail closed while required
/// transactions are active until full bucket/GitHub compensation is designed.
/// </summary>
public sealed class TransactionGatedToolBucketServiceTests
{
    /// <summary>
    /// TEST-MCP-161: Add/remove/install/sync reject before invoking the inner
    /// bucket service when transaction enforcement is required.
    /// </summary>
    [Fact]
    public async Task Mutations_WhenTransactionsRequired_ReturnDeferredFailuresWithoutCallingInner()
    {
        var inner = Substitute.For<IToolBucketService>();
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var add = await sut.AddBucketAsync(new BucketAddRequest("official", "owner", "repo")).ConfigureAwait(true);
        var remove = await sut.RemoveBucketAsync("official", uninstallTools: true).ConfigureAwait(true);
        var install = await sut.InstallAsync("official", "tool-alpha").ConfigureAwait(true);
        var sync = await sut.SyncAsync("official").ConfigureAwait(true);

        Assert.False(add.Success);
        Assert.False(remove.Success);
        Assert.False(install.Success);
        Assert.False(sync.Success);
        Assert.Contains("deferred", add.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deferred", remove.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deferred", install.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deferred", sync.Error, StringComparison.OrdinalIgnoreCase);
        _ = inner.DidNotReceive().AddBucketAsync(Arg.Any<BucketAddRequest>(), Arg.Any<CancellationToken>());
        _ = inner.DidNotReceive().RemoveBucketAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        _ = inner.DidNotReceive().InstallAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        _ = inner.DidNotReceive().SyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Null(coordinator.Request);
    }

    /// <summary>
    /// TEST-MCP-161: Bucket reads are pass-through operations and do not allocate
    /// coordinator transactions.
    /// </summary>
    [Fact]
    public async Task ListAndBrowseAsync_DelegateWithoutCoordinatorTransaction()
    {
        var inner = Substitute.For<IToolBucketService>();
        inner.ListBucketsAsync(Arg.Any<CancellationToken>())
            .Returns(new BucketListResult([new BucketDto(1, "official", "owner", "repo", "main", "/", DateTimeOffset.UtcNow, null)], 1));
        inner.BrowseAsync("official", Arg.Any<CancellationToken>())
            .Returns(new BucketBrowseResult(true, Tools: [new ToolManifest("tool-alpha", "Tool", ["alpha"], null, null, "tool-alpha.json")]));
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var list = await sut.ListBucketsAsync().ConfigureAwait(true);
        var browse = await sut.BrowseAsync("official").ConfigureAwait(true);

        Assert.Single(list.Buckets);
        Assert.True(browse.Success);
        Assert.NotNull(browse.Tools);
        Assert.Single(browse.Tools!);
        Assert.Null(coordinator.Request);
        _ = inner.Received(1).ListBucketsAsync(Arg.Any<CancellationToken>());
        _ = inner.Received(1).BrowseAsync("official", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST-MCP-161: Non-required transaction mode preserves the existing direct
    /// bucket mutation behavior.
    /// </summary>
    [Fact]
    public async Task AddBucketAsync_WhenTransactionsNotRequired_DelegatesToInner()
    {
        var inner = Substitute.For<IToolBucketService>();
        inner.AddBucketAsync(Arg.Any<BucketAddRequest>(), Arg.Any<CancellationToken>())
            .Returns(new BucketMutationResult(true, Bucket: new BucketDto(1, "official", "owner", "repo", "main", "/", DateTimeOffset.UtcNow, null)));
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(
            inner,
            coordinator,
            new TurnTransactionOptions { Enabled = true, RequiredForMutations = false });

        var result = await sut.AddBucketAsync(new BucketAddRequest("official", "owner", "repo")).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(result.Bucket);
        Assert.Null(coordinator.Request);
        _ = inner.Received(1).AddBucketAsync(Arg.Any<BucketAddRequest>(), Arg.Any<CancellationToken>());
    }

    private static TransactionGatedToolBucketService CreateSut(
        IToolBucketService inner,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? options = null)
        => new(
            inner,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(options ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        public TurnTransactionRequest? Request { get; private set; }

        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new TurnTransactionResult
            {
                TransactionId = request.TransactionId ?? "txn-tool-bucket-test",
                Status = "committed",
                Reason = TransactionFailureReason.None,
                MutationApplied = false,
            });
        }

        public TurnTransactionStatusResponse GetStatus()
            => new()
            {
                Enabled = true,
                Degraded = false,
                LastReason = TransactionFailureReason.None,
                Message = "ready",
            };
    }
}
