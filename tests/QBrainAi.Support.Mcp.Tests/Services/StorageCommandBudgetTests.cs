using System.Diagnostics;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-002: the 5 second storage budget expires as storage-unavailable
/// instead of hanging until the REPL timeout.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGESTORE-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (ExecuteAsync_HungWork_FailsWithinEightSeconds and ExecuteAsync_ExplicitBudget_ExpiresAtThatBudget) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</remarks>
public sealed class StorageCommandBudgetTests
{
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
