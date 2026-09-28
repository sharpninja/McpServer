using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for turn-transaction diagnostic endpoints (<c>/mcpserver/turntransactions</c>).
/// </summary>
/// <seealso cref="McpServerClient.TurnTransactions"/>
public sealed class TurnTransactionsClient : McpClientBase
{
    /// <inheritdoc />
    public TurnTransactionsClient(HttpClient http, McpServerClientOptions options)
        : base(http, options)
    {
    }

    internal TurnTransactionsClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
    }

    /// <summary>
    /// Gets the current turn-transaction gate status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction gate state and last failure reason.</returns>
    public async Task<TurnTransactionStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<TurnTransactionStatusResponse>("mcpserver/turntransactions/status", cancellationToken);
    }

    /// <summary>
    /// Lists persisted transaction pub/sub messages for diagnostics.
    /// </summary>
    /// <param name="maxMessages">Maximum number of messages to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent persisted pub/sub message states.</returns>
    public async Task<IReadOnlyList<TransactionPubSubMessageStatus>> GetPubSubStatusAsync(
        int maxMessages = 100,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<IReadOnlyList<TransactionPubSubMessageStatus>>(
            $"mcpserver/turntransactions/pubsub/status?maxMessages={maxMessages}",
            cancellationToken);
    }

    /// <summary>
    /// Replays pending persisted transaction pub/sub messages.
    /// </summary>
    /// <param name="maxMessages">Maximum number of messages to attempt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Replay counts for attempted, acknowledged, and remaining messages.</returns>
    public async Task<TransactionPubSubReplayResult> ReplayPubSubAsync(
        int maxMessages = 100,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<TransactionPubSubReplayResult>(
            $"mcpserver/turntransactions/pubsub/replay?maxMessages={maxMessages}",
            null,
            cancellationToken);
    }

    /// <summary>
    /// Purges completed persisted transaction pub/sub messages older than a cutoff.
    /// </summary>
    /// <param name="completedBeforeUtc">Cutoff timestamp. When null, the server chooses its default.</param>
    /// <param name="maxMessages">Maximum number of completed messages to purge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Retention purge counts.</returns>
    public async Task<TransactionPubSubRetentionResult> PurgePubSubRetentionAsync(
        DateTimeOffset? completedBeforeUtc = null,
        int maxMessages = 100,
        CancellationToken cancellationToken = default)
    {
        var query = $"maxMessages={maxMessages}";
        if (completedBeforeUtc.HasValue)
        {
            query = $"completedBeforeUtc={Uri.EscapeDataString(completedBeforeUtc.Value.ToString("O", CultureInfo.InvariantCulture))}&{query}";
        }

        return await PostAsync<TransactionPubSubRetentionResult>(
            $"mcpserver/turntransactions/pubsub/retention/purge?{query}",
            null,
            cancellationToken);
    }
}
