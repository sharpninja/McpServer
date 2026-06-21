using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-121: Status endpoints for the turn transaction coordinator.
/// </summary>
[ApiController]
[Route("mcpserver/turntransactions")]
public sealed class TurnTransactionsController : ControllerBase
{
    private readonly ITurnTransactionCoordinator _coordinator;
    private readonly ITransactionPubSubReplayService _pubSubReplayService;

    /// <summary>Initializes a new instance of the <see cref="TurnTransactionsController"/> class.</summary>
    /// <param name="coordinator">Turn transaction coordinator.</param>
    /// <param name="pubSubReplayService">Durable pub-sub replay and retention service.</param>
    public TurnTransactionsController(
        ITurnTransactionCoordinator coordinator,
        ITransactionPubSubReplayService? pubSubReplayService = null)
    {
        _coordinator = coordinator;
        _pubSubReplayService = pubSubReplayService ?? ControllerNoopTransactionPubSubReplayService.Instance;
    }

    /// <summary>Gets the current turn transaction coordinator status.</summary>
    /// <returns>Coordinator status.</returns>
    [HttpGet("status")]
    public ActionResult<TurnTransactionStatusResponse> GetStatus()
        => Ok(_coordinator.GetStatus());

    /// <summary>Gets pending durable pub-sub message status records.</summary>
    /// <param name="maxMessages">Maximum records to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pending durable pub-sub message status records.</returns>
    [HttpGet("pubsub/status")]
    public async Task<ActionResult<IReadOnlyList<TransactionPubSubMessageStatus>>> GetPubSubStatus(
        [FromQuery] int maxMessages = 100,
        CancellationToken cancellationToken = default)
        => Ok(await _pubSubReplayService
            .GetPendingMessagesAsync(NormalizeLimit(maxMessages), cancellationToken)
            .ConfigureAwait(false));

    /// <summary>Runs one durable pub-sub replay cycle.</summary>
    /// <param name="maxMessages">Maximum pending records to attempt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Replay result counts.</returns>
    [HttpPost("pubsub/replay")]
    public async Task<ActionResult<TransactionPubSubReplayResult>> ReplayPubSub(
        [FromQuery] int maxMessages = 100,
        CancellationToken cancellationToken = default)
        => Ok(await _pubSubReplayService
            .ReplayPendingAsync(NormalizeLimit(maxMessages), cancellationToken)
            .ConfigureAwait(false));

    /// <summary>Purges completed durable pub-sub messages before the supplied retention cutoff.</summary>
    /// <param name="completedBeforeUtc">Optional completed-message cutoff. Defaults to the current UTC instant.</param>
    /// <param name="maxMessages">Maximum completed records to purge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Retention purge result counts.</returns>
    [HttpPost("pubsub/retention/purge")]
    public async Task<ActionResult<TransactionPubSubRetentionResult>> PurgePubSubRetention(
        [FromQuery] DateTimeOffset? completedBeforeUtc = null,
        [FromQuery] int maxMessages = 100,
        CancellationToken cancellationToken = default)
        => Ok(await _pubSubReplayService
            .PurgeCompletedAsync(
                completedBeforeUtc ?? DateTimeOffset.UtcNow,
                NormalizeLimit(maxMessages),
                cancellationToken)
            .ConfigureAwait(false));

    private static int NormalizeLimit(int maxMessages)
        => Math.Clamp(maxMessages, 1, 1000);

    private sealed class ControllerNoopTransactionPubSubReplayService : ITransactionPubSubReplayService
    {
        public static readonly ControllerNoopTransactionPubSubReplayService Instance = new();

        public Task<TransactionPubSubReplayResult> ReplayPendingAsync(
            int maxMessages = 100,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new TransactionPubSubReplayResult());

        public Task<IReadOnlyList<TransactionPubSubMessageStatus>> GetPendingMessagesAsync(
            int maxMessages = 100,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TransactionPubSubMessageStatus>>([]);

        public Task<TransactionPubSubRetentionResult> PurgeCompletedAsync(
            DateTimeOffset completedBeforeUtc,
            int maxMessages = 100,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new TransactionPubSubRetentionResult
            {
                CompletedBeforeUtc = completedBeforeUtc,
                MaxMessages = NormalizeLimit(maxMessages),
            });
    }
}
