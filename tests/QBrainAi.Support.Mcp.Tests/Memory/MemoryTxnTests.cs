using QBrainAi.Support.Mcp.Services;
using QBrainAi.TransactionSecurity.Models;
using QBrainAi.TransactionSecurity.Services;
using NSubstitute;

namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-010: Transaction gating fails closed without a required turn.
/// </summary>
public sealed class MemoryTxnTests
{
    /// <summary>FR-MCP-173: memory.add bypasses the coordinator and calls the inner service.</summary>
    [Fact]
    public async Task MissingTurn_FailsClosed()
    {
        var inner = Substitute.For<IMemoryService>();
        inner.AddAsync(Arg.Any<MemoryAddRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(true, Memory: new MemoryItem
            {
                Id = "MEMORY-TXN-001",
                Category = "TXN",
                Scope = MemoryScope.Workspace,
                Text = "should not persist without turn",
                Version = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            }));
        var gated = new TransactionGatedMemoryService(inner, new RejectingCoordinator());

        var result = await gated.AddAsync(new MemoryAddRequest
        {
            Category = "txn",
            Text = "no turn",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await inner.Received(1)
            .AddAsync(Arg.Any<MemoryAddRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>FR-MCP-173: remember without a CQRS dispatcher fails before a memory write.</summary>
    [Fact]
    public async Task NewMutations_HonorGating()
    {
        var names = typeof(ITransactionGatedMemoryService).GetMethods()
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var method in new[] { "RememberAsync", "PromoteAsync", "ConsolidateAsync", "RevertAsync" })
            Assert.Contains(method, names);

        var inner = Substitute.For<IMemoryService>();
        var gated = new TransactionGatedMemoryService(inner, new RejectingCoordinator());
        var remember = typeof(ITransactionGatedMemoryService).GetMethod("RememberAsync");
        Assert.NotNull(remember);
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => (Task)remember!.Invoke(gated, [
            new MemoryRememberRequest { Content = "gated", Type = "fact" },
            TestContext.Current.CancellationToken,
        ])!).ConfigureAwait(true);
        Assert.Contains("dispatcher", thrown.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RejectingCoordinator : ITurnTransactionCoordinator
    {
        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new TurnTransactionResult
            {
                TransactionId = request.TransactionId ?? "txn-s1",
                Status = "rejected",
                Reason = TransactionFailureReason.UnknownKey,
                MutationApplied = false,
                Message = "no open turn",
            });

        public TurnTransactionStatusResponse GetStatus()
            => new()
            {
                Enabled = true,
                Degraded = false,
                LastReason = TransactionFailureReason.None,
                Message = "available",
            };
    }
}
