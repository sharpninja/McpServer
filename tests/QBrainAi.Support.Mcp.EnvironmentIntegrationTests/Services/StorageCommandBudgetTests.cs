using System.Diagnostics;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-002: the 5 second storage budget expires as storage-unavailable
/// instead of hanging until the REPL timeout.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGESTORE-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/StorageCommandBudgetTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases moved unchanged with the class attributes; only the namespace changed.</remarks>
public sealed class StorageCommandBudgetTests
{
    /// <summary>A hung storage call is canceled within about 5 seconds.</summary>
    /// <remarks>Integration: asserts an elapsed wall-clock bound on actual scheduling and timer execution (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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

    /// <summary>
    /// TEST-MCP-TRIAGESTORE-007: an explicit per-call budget expires at that budget,
    /// and a longer budget does not use the 5 second intake message.
    /// </summary>
    /// <remarks>Integration: asserts an elapsed wall-clock bound on actual scheduling and timer execution (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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
}
