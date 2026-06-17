using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;

namespace McpServer.Acid.IntegrationTests;

/// <summary>Subscriber double that rejects commits with a configurable reason (non-degraded rejection paths).</summary>
internal sealed class RejectingSubscriberCommitService(TransactionFailureReason reason) : ISubscriberCommitService
{
    /// <inheritdoc />
    public Task<DiffgramCommitResponse> CommitDiffgramAsync(DiffgramCommitRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new DiffgramCommitResponse
        {
            TransactionId = request.Manifest.TransactionId,
            Status = "rejected",
            Reason = reason,
        });

    /// <inheritdoc />
    public Task<TransactionStatusResponse?> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken = default)
        => Task.FromResult<TransactionStatusResponse?>(null);

    /// <inheritdoc />
    public Task<TransactionAbortResponse> AbortTransactionAsync(string transactionId, TransactionAbortRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new TransactionAbortResponse
        {
            TransactionId = transactionId,
            Status = "aborted",
            Reason = request.Reason,
            AbortedAtUtc = DateTimeOffset.UtcNow,
        });
}

/// <summary>Subscriber double that is always unavailable, used to drive degraded-mode/rollback paths.</summary>
internal sealed class UnavailableSubscriberCommitService : ISubscriberCommitService
{
    /// <inheritdoc />
    public Task<DiffgramCommitResponse> CommitDiffgramAsync(DiffgramCommitRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new DiffgramCommitResponse
        {
            TransactionId = request.Manifest.TransactionId,
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
        });

    /// <inheritdoc />
    public Task<TransactionStatusResponse?> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken = default)
        => Task.FromResult<TransactionStatusResponse?>(null);

    /// <inheritdoc />
    public Task<TransactionAbortResponse> AbortTransactionAsync(string transactionId, TransactionAbortRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new TransactionAbortResponse
        {
            TransactionId = transactionId,
            Status = "aborted",
            Reason = request.Reason,
            AbortedAtUtc = DateTimeOffset.UtcNow,
        });
}
