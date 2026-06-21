using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for subscriber endpoints that verify, decrypt, commit, and abort transactional diffgrams.
/// FR-MCP-122, FR-MCP-123, FR-MCP-124.
/// </summary>
/// <seealso cref="McpServerClient.Subscriber"/>
public sealed class SubscriberClient : McpClientBase
{
    /// <inheritdoc />
    public SubscriberClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal SubscriberClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Commits a signed and encrypted diffgram.</summary>
    /// <param name="request">Commit payload containing manifest and encrypted body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Commit result.</returns>
    public async Task<DiffgramCommitResponse> CommitDiffgramAsync(
        DiffgramCommitRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<DiffgramCommitResponse>("mcpserver/subscriber/diffgrams/commit", request, cancellationToken);

    /// <summary>Gets subscriber transaction status by transaction identifier.</summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current transaction status.</returns>
    public async Task<TransactionStatusResponse> GetTransactionStatusAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
        => await GetAsync<TransactionStatusResponse>(
            $"mcpserver/subscriber/transactions/{Encode(transactionId)}/status",
            cancellationToken);

    /// <summary>Aborts a subscriber transaction before commit.</summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="request">Abort payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Abort result.</returns>
    public async Task<TransactionAbortResponse> AbortTransactionAsync(
        string transactionId,
        TransactionAbortRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<TransactionAbortResponse>(
            $"mcpserver/subscriber/transactions/{Encode(transactionId)}/abort",
            request,
            cancellationToken);

    private static string Encode(string value) => Uri.EscapeDataString(value);
}
