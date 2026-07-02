using McpServer.TransactionSecurity.Models;

namespace McpServer.Acid.IntegrationTests;

/// <summary>
/// TEST-MCP-ACID-002: Demonstrates participant pluggability - the key server and subscriber are real spun-up
/// hosts (WebApplicationFactory, torn down after the test) while the MCP Server coordinator drives the full
/// transaction over the HTTP transports. Proves the same lifecycle commits against running instances.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AcidRunningInstanceTests
{
    /// <summary>Full happy-path commit with running key server and subscriber hosts.</summary>
    [Fact]
    public async Task FullLifecycle_RunningKeyServerAndSubscriber_Commits()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllRunning);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);

        var result = await harness.Coordinator.ExecuteAsync(
            new TurnTransactionRequest
            {
                TransactionId = "txn-running-commit",
                TurnId = "turn-running-commit",
                OperationName = "todo.update",
                OperationBodyJson = "{\"id\":\"PLAN-TURNTRANSACTIONS-001\"}",
                PublisherPartyId = AcidTransactionHarness.PublisherPartyId,
                SubscriberPartyId = AcidTransactionHarness.SubscriberPartyId,
                Sequence = 1,
                Mutating = true,
            },
            _ => Task.FromResult(new TurnMutationResult { Success = true, ResultJson = "{\"updated\":true}" }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.True(result.MutationApplied);
        Assert.False(result.Degraded);
        Assert.False(string.IsNullOrWhiteSpace(result.DiffgramId));
    }
}
