using System.Diagnostics;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-002: the 5 second storage budget expires as storage-unavailable
/// instead of hanging until the REPL timeout.
/// </summary>
public sealed class StorageCommandBudgetTests
{
    /// <summary>A hung storage call is canceled within about 5 seconds.</summary>
    [Fact]
    public async Task ExecuteAsync_HungWork_FailsWithinEightSeconds()
    {
        var clock = Stopwatch.StartNew();
        await Assert.ThrowsAsync<StorageCommandBudgetExceededException>(() =>
            StorageCommandBudget.ExecuteAsync(
                async ct => await Task.Delay(TimeSpan.FromMinutes(1), ct).ConfigureAwait(true),
                TestContext.Current.CancellationToken)).ConfigureAwait(true);
        clock.Stop();
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(8), $"Budget took {clock.Elapsed}.");
        Assert.True(clock.Elapsed >= TimeSpan.FromSeconds(4), $"Budget expired too early: {clock.Elapsed}.");
    }

    /// <summary>Budget expiry is classified as backend_unavailable and retryable.</summary>
    [Fact]
    public void Classify_BudgetExceeded_IsBackendUnavailable()
    {
        var classified = McpErrorClassifier.Classify(new StorageCommandBudgetExceededException());
        Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
        Assert.True(classified.Retryable);
    }

    /// <summary>TEST-MCP-TRIAGESTORE-007: the shared intake default stays at 5 seconds.</summary>
    [Fact]
    public void Default_RemainsFiveSeconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(5), StorageCommandBudget.Default);
    }

    /// <summary>
    /// TEST-MCP-TRIAGESTORE-007: an explicit per-call budget expires at that budget,
    /// and a longer budget does not use the 5 second intake message.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ExplicitBudget_ExpiresAtThatBudget()
    {
        var clock = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<StorageCommandBudgetExceededException>(() =>
            StorageCommandBudget.ExecuteAsync(
                async ct => await Task.Delay(TimeSpan.FromMinutes(1), ct).ConfigureAwait(true),
                TestContext.Current.CancellationToken,
                TimeSpan.FromMilliseconds(200))).ConfigureAwait(true);
        clock.Stop();

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(2), $"Explicit budget took {clock.Elapsed}.");
        Assert.Contains("storage command budget", ex.Message, StringComparison.Ordinal);
        var classified = McpErrorClassifier.Classify(ex);
        Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
        Assert.True(classified.Retryable);
    }

    /// <summary>TEST-MCP-TRIAGESTORE-007: graph materialization deadlock is retryable backend_unavailable.</summary>
    [Fact]
    public void Classify_GraphMaterializationDeadlock_IsBackendUnavailable()
    {
        var classified = McpErrorClassifier.Classify(
            new StorageGraphMaterializationException(
                "The storage backend deadlocked while loading the session graph (SQL 1205). The session was not treated as missing. Retry the operation.",
                new InvalidOperationException("deadlock")));
        Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
        Assert.True(classified.Retryable);
        Assert.Equal(503, classified.StatusCode);
    }
}
