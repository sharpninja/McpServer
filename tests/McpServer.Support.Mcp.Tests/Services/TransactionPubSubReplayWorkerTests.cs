using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for the durable transaction pub-sub replay worker. TEST-MCP-161.</summary>
public sealed class TransactionPubSubReplayWorkerTests
{
    /// <summary>Replay worker does not call replay or retention services when durable pub-sub is disabled.</summary>
    [Fact]
    public async Task ReplayOnceAsync_WhenDurableDisabled_DoesNotReplay()
    {
        var replay = Substitute.For<ITransactionPubSubReplayService>();
        var worker = CreateWorker(replay, new TurnTransactionOptions
        {
            DurablePubSubEnabled = false,
            PubSubReplayWorkerEnabled = true,
        });

        await worker.ReplayOnceAsync(CancellationToken.None).ConfigureAwait(true);

        await replay.DidNotReceiveWithAnyArgs().ReplayPendingAsync(default, default).ConfigureAwait(true);
        await replay.DidNotReceiveWithAnyArgs().PurgeCompletedAsync(default, default, default).ConfigureAwait(true);
    }

    /// <summary>Replay worker calls replay and retention services with configured batch sizes when enabled.</summary>
    [Fact]
    public async Task ReplayOnceAsync_WhenDurableEnabled_CallsReplayAndRetentionWithConfiguredBatches()
    {
        var replay = Substitute.For<ITransactionPubSubReplayService>();
        replay.ReplayPendingAsync(7, Arg.Any<CancellationToken>())
            .Returns(new TransactionPubSubReplayResult());
        replay.PurgeCompletedAsync(Arg.Any<DateTimeOffset>(), 11, Arg.Any<CancellationToken>())
            .Returns(new TransactionPubSubRetentionResult());
        var worker = CreateWorker(replay, new TurnTransactionOptions
        {
            DurablePubSubEnabled = true,
            PubSubReplayWorkerEnabled = true,
            PubSubReplayBatchSize = 7,
            PubSubRetentionEnabled = true,
            PubSubTerminalRetentionSeconds = 30,
            PubSubRetentionBatchSize = 11,
        });

        await worker.ReplayOnceAsync(CancellationToken.None).ConfigureAwait(true);

        await replay.Received(1).ReplayPendingAsync(7, Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await replay.Received(1).PurgeCompletedAsync(Arg.Any<DateTimeOffset>(), 11, Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    private static TransactionPubSubReplayWorker CreateWorker(
        ITransactionPubSubReplayService replay,
        TurnTransactionOptions options)
        => new(
            replay,
            Monitor(options),
            NullLogger<TransactionPubSubReplayWorker>.Instance);

    private static IOptionsMonitor<TurnTransactionOptions> Monitor(TurnTransactionOptions options)
    {
        var monitor = Substitute.For<IOptionsMonitor<TurnTransactionOptions>>();
        monitor.CurrentValue.Returns(options);
        return monitor;
    }
}
