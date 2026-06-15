using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Executes repository write mutations through the turn transaction coordinator.
/// </summary>
public sealed class TransactionGatedRepoFileService : IRepoFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IRepoFileService _inner;
    private readonly IRepoFileCompensation? _compensation;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;
    private long _lastSequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedRepoFileService"/> class.</summary>
    /// <param name="inner">Underlying repository file service.</param>
    /// <param name="compensation">Optional rollback compensation service.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction options.</param>
    public TransactionGatedRepoFileService(
        IRepoFileService inner,
        IRepoFileCompensation? compensation = null,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _compensation = compensation;
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<RepoFileReadResult?> ReadAsync(string relativePath, CancellationToken cancellationToken = default)
        => _inner.ReadAsync(relativePath, cancellationToken);

    /// <inheritdoc />
    public Task<RepoListResult> ListAsync(string? relativePath, CancellationToken cancellationToken = default)
        => _inner.ListAsync(relativePath, cancellationToken);

    /// <inheritdoc />
    public async Task<RepoWriteResult> WriteAsync(
        string relativePath,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (_coordinator is null)
            return await _inner.WriteAsync(relativePath, content, cancellationToken).ConfigureAwait(false);

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            return new RepoWriteResult(
                false,
                string.IsNullOrWhiteSpace(status.Message)
                    ? "Turn transaction coordinator is degraded."
                    : status.Message);
        }

        var requiresMutationTransactions = RequiresMutationTransactions(status);
        if (requiresMutationTransactions && _compensation is null)
            return new RepoWriteResult(false, "Repository file provider does not support transaction rollback compensation.");

        RepoWriteResult? writeResult = null;
        var hasWriteResult = false;
        var transaction = BuildTransactionRequest(relativePath, content);
        var result = await _coordinator.ExecuteAsync(
                transaction,
                async ct =>
                {
                    RepoFileSnapshot? snapshot = null;
                    if (_compensation is not null)
                        snapshot = await _compensation.CaptureForWriteAsync(relativePath, ct).ConfigureAwait(false);

                    writeResult = await _inner.WriteAsync(relativePath, content, ct).ConfigureAwait(false);
                    hasWriteResult = true;
                    return new TurnMutationResult
                    {
                        Success = writeResult.Written,
                        ResultJson = JsonSerializer.Serialize(writeResult, JsonOptions),
                        Error = writeResult.Error,
                        RollbackAsync = writeResult.Written && snapshot is not null
                            ? rollbackCt => RestoreWriteOrThrowAsync(snapshot, content, rollbackCt)
                            : null,
                    };
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (hasWriteResult && (!writeResult!.Written || IsTransactionSuccess(result)))
            return writeResult;

        return ToTransactionFailure("repo.write", result);
    }

    private async Task RestoreWriteOrThrowAsync(
        RepoFileSnapshot snapshot,
        string writtenContent,
        CancellationToken cancellationToken)
    {
        if (_compensation is null)
            throw new InvalidOperationException("Repository file provider does not support transaction rollback compensation.");

        await _compensation.RestoreWriteAsync(snapshot, writtenContent, cancellationToken).ConfigureAwait(false);
    }

    private TurnTransactionRequest BuildTransactionRequest(string relativePath, string content)
    {
        var sequence = NextSequence();
        return new TurnTransactionRequest
        {
            TurnId = $"repo.write-{sequence}",
            OperationName = "repo.write",
            OperationBodyJson = JsonSerializer.Serialize(
                new RepoWriteTransactionPayload(relativePath, ComputeSha256(content), content.Length),
                JsonOptions),
            Sequence = sequence,
            Mutating = true,
        };
    }

    private long NextSequence()
    {
        while (true)
        {
            var current = Volatile.Read(ref _lastSequence);
            var next = Math.Max(current + 1, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (Interlocked.CompareExchange(ref _lastSequence, next, current) == current)
                return next;
        }
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);

    private static bool IsTransactionSuccess(TurnTransactionResult result)
        => string.Equals(result.Status, "committed", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(result.Status, "bypassed", StringComparison.OrdinalIgnoreCase);

    private static RepoWriteResult ToTransactionFailure(string operationName, TurnTransactionResult result)
    {
        var transactionId = string.IsNullOrWhiteSpace(result.TransactionId)
            ? "unassigned"
            : result.TransactionId;
        var message = string.IsNullOrWhiteSpace(result.Message)
            ? result.Reason.ToString()
            : result.Message;
        if (result.RollbackAttempted)
        {
            message = result.RollbackSucceeded
                ? $"{message} Rollback completed."
                : $"{message} Rollback failed: {result.RollbackError ?? "unknown error"}.";
        }

        return new RepoWriteResult(
            false,
            $"Turn transaction coordinator did not commit {operationName} '{transactionId}': {message}");
    }

    private static string ComputeSha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record RepoWriteTransactionPayload(string RelativePath, string ContentSha256, int ContentLength);
}
