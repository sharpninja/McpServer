using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-121: Background worker that replays and retains durable turn transaction pub-sub messages.
/// </summary>
public sealed class TransactionPubSubReplayWorker : BackgroundService
{
    private readonly ITransactionPubSubReplayService _replayService;
    private readonly IOptionsMonitor<TurnTransactionOptions> _options;
    private readonly ILogger<TransactionPubSubReplayWorker> _logger;

    /// <summary>Initializes a new instance of the <see cref="TransactionPubSubReplayWorker"/> class.</summary>
    /// <param name="replayService">Durable pub-sub replay service.</param>
    /// <param name="options">Turn transaction options.</param>
    /// <param name="logger">Logger.</param>
    public TransactionPubSubReplayWorker(
        ITransactionPubSubReplayService replayService,
        IOptionsMonitor<TurnTransactionOptions> options,
        ILogger<TransactionPubSubReplayWorker> logger)
    {
        _replayService = replayService;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReplayOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Transaction pub-sub replay cycle failed.");
            }

            var interval = Math.Max(1, _options.CurrentValue.PubSubReplayIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>Runs one replay and retention cycle. Exposed for focused tests and operational probes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the cycle has finished.</returns>
    public async Task ReplayOnceAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (!options.DurablePubSubEnabled || !options.PubSubReplayWorkerEnabled)
            return;

        await _replayService
            .ReplayPendingAsync(Math.Max(1, options.PubSubReplayBatchSize), cancellationToken)
            .ConfigureAwait(false);

        if (!options.PubSubRetentionEnabled || options.PubSubTerminalRetentionSeconds <= 0)
            return;

        var completedBeforeUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(options.PubSubTerminalRetentionSeconds));
        await _replayService
            .PurgeCompletedAsync(
                completedBeforeUtc,
                Math.Max(1, options.PubSubRetentionBatchSize),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
