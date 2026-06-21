using McpServer.Support.Mcp.Controllers;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>Controller tests for turn transaction management endpoints. TEST-MCP-161.</summary>
public sealed class TurnTransactionsControllerTests
{
    /// <summary>Pub-sub status endpoint returns pending messages and forwards the normalized limit.</summary>
    [Fact]
    public async Task GetPubSubStatus_ReturnsPendingMessagesAndPassesLimit()
    {
        var replay = Substitute.For<ITransactionPubSubReplayService>();
        replay.GetPendingMessagesAsync(25, Arg.Any<CancellationToken>())
            .Returns([
                new TransactionPubSubMessageStatus
                {
                    OperationId = "commit:txn-controller-status",
                    TransactionId = "txn-controller-status",
                    Kind = "commit",
                    TopicName = "topic.commit",
                    SubscriberId = "subscriber-a",
                    Status = "pending",
                },
            ]);
        var controller = new TurnTransactionsController(Substitute.For<ITurnTransactionCoordinator>(), replay);

        var result = await controller.GetPubSubStatus(25, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var messages = Assert.IsAssignableFrom<IReadOnlyList<TransactionPubSubMessageStatus>>(ok.Value);
        var message = Assert.Single(messages);
        Assert.Equal("topic.commit", message.TopicName);
        await replay.Received(1).GetPendingMessagesAsync(25, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>Pub-sub replay endpoint returns replay counts and forwards the normalized limit.</summary>
    [Fact]
    public async Task ReplayPubSub_ReturnsReplayCountsAndPassesLimit()
    {
        var replay = Substitute.For<ITransactionPubSubReplayService>();
        replay.ReplayPendingAsync(12, Arg.Any<CancellationToken>())
            .Returns(new TransactionPubSubReplayResult
            {
                AttemptedCount = 12,
                AcknowledgedCount = 10,
                PendingCount = 2,
            });
        var controller = new TurnTransactionsController(Substitute.For<ITurnTransactionCoordinator>(), replay);

        var result = await controller.ReplayPubSub(12, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var replayResult = Assert.IsType<TransactionPubSubReplayResult>(ok.Value);
        Assert.Equal(12, replayResult.AttemptedCount);
        Assert.Equal(10, replayResult.AcknowledgedCount);
        Assert.Equal(2, replayResult.PendingCount);
        await replay.Received(1).ReplayPendingAsync(12, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>Pub-sub retention endpoint returns purge counts and forwards the cutoff and normalized limit.</summary>
    [Fact]
    public async Task PurgePubSubRetention_ReturnsRetentionCountsAndPassesCutoff()
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-10);
        var replay = Substitute.For<ITransactionPubSubReplayService>();
        replay.PurgeCompletedAsync(cutoff, 9, Arg.Any<CancellationToken>())
            .Returns(new TransactionPubSubRetentionResult
            {
                CompletedBeforeUtc = cutoff,
                MaxMessages = 9,
                PurgedCount = 3,
                RetainedPendingCount = 1,
            });
        var controller = new TurnTransactionsController(Substitute.For<ITurnTransactionCoordinator>(), replay);

        var result = await controller.PurgePubSubRetention(cutoff, 9, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var retention = Assert.IsType<TransactionPubSubRetentionResult>(ok.Value);
        Assert.Equal(3, retention.PurgedCount);
        Assert.Equal(1, retention.RetainedPendingCount);
        await replay.Received(1).PurgeCompletedAsync(cutoff, 9, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }
}
