using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-010: Transaction gating fails closed without a required turn.
/// </summary>
public sealed class MemoryTxnTests
{
    /// <summary>AC-FR-MCP-MEMORY-010-38: Mutation without required turn transaction fails closed.</summary>
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

        Assert.False(result.Success);
        await inner.DidNotReceive()
            .AddAsync(Arg.Any<MemoryAddRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>AC-TR-MCP-MEMORY-API-002-09: Mutating new endpoints honor transaction gating.</summary>
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
        var result = await ((Task<MemoryRememberResult>)remember!.Invoke(gated, [
            new MemoryRememberRequest { Content = "gated", Type = "fact" },
            TestContext.Current.CancellationToken,
        ])!).ConfigureAwait(true);

        Assert.True(result.StatusCode is 400 or 403 or 409);
        await inner.DidNotReceive()
            .AddAsync(Arg.Any<MemoryAddRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
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
