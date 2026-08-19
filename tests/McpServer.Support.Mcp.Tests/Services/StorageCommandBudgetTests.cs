using System.Diagnostics;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

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
}
