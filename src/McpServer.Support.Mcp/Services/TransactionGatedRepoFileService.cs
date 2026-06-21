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

    /// <inheritdoc />
    public async Task<RepoEditResult> EditAsync(
        string relativePath,
        string oldString,
        string newString,
        bool replaceAll = false,
        int? expectedOccurrences = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(oldString);
        ArgumentNullException.ThrowIfNull(newString);

        if (_coordinator is null)
            return await _inner.EditAsync(relativePath, oldString, newString, replaceAll, expectedOccurrences, cancellationToken).ConfigureAwait(false);

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            return new RepoEditResult(
                false,
                0,
                string.IsNullOrWhiteSpace(status.Message) ? "Turn transaction coordinator is degraded." : status.Message);
        }

        var requiresMutationTransactions = RequiresMutationTransactions(status);
        if (requiresMutationTransactions && _compensation is null)
            return new RepoEditResult(false, 0, "Repository file provider does not support transaction rollback compensation.");

        RepoEditResult? editResult = null;
        var hasEditResult = false;
        var transaction = BuildEditTransactionRequest(relativePath, oldString, newString, replaceAll);
        var result = await _coordinator.ExecuteAsync(
                transaction,
                async ct =>
                {
                    RepoFileSnapshot? snapshot = null;
                    if (_compensation is not null)
                        snapshot = await _compensation.CaptureForWriteAsync(relativePath, ct).ConfigureAwait(false);

                    var edit = await _inner.EditAsync(relativePath, oldString, newString, replaceAll, expectedOccurrences, ct).ConfigureAwait(false);
                    editResult = edit;
                    hasEditResult = true;

                    Func<CancellationToken, Task>? rollback = null;
                    if (edit.Written && snapshot is not null)
                    {
                        // The inner service computed the edited content; re-capture it so rollback's hash guard
                        // matches the content the transaction actually wrote before restoring the pre-edit snapshot.
                        var postEdit = await _compensation!.CaptureForWriteAsync(relativePath, ct).ConfigureAwait(false);
                        var writtenContent = postEdit?.Content ?? string.Empty;
                        rollback = rollbackCt => RestoreWriteOrThrowAsync(snapshot, writtenContent, rollbackCt);
                    }

                    return new TurnMutationResult
                    {
                        Success = edit.Written,
                        ResultJson = JsonSerializer.Serialize(edit, JsonOptions),
                        Error = edit.Error,
                        RollbackAsync = rollback,
                    };
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (hasEditResult && (!editResult!.Written || IsTransactionSuccess(result)))
            return editResult;

        return ToEditTransactionFailure("repo.edit", result);
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

    private TurnTransactionRequest BuildEditTransactionRequest(string relativePath, string oldString, string newString, bool replaceAll)
    {
        var sequence = NextSequence();
        return new TurnTransactionRequest
        {
            TurnId = $"repo.edit-{sequence}",
            OperationName = "repo.edit",
            OperationBodyJson = JsonSerializer.Serialize(
                new RepoEditTransactionPayload(relativePath, ComputeSha256(oldString), ComputeSha256(newString), replaceAll),
                JsonOptions),
            Sequence = sequence,
            Mutating = true,
        };
    }

    private static RepoEditResult ToEditTransactionFailure(string operationName, TurnTransactionResult result)
    {
        var transactionId = string.IsNullOrWhiteSpace(result.TransactionId) ? "unassigned" : result.TransactionId;
        var message = string.IsNullOrWhiteSpace(result.Message) ? result.Reason.ToString() : result.Message;
        if (result.RollbackAttempted)
        {
            message = result.RollbackSucceeded
                ? $"{message} Rollback completed."
                : $"{message} Rollback failed: {result.RollbackError ?? "unknown error"}.";
        }

        return new RepoEditResult(false, 0, $"Turn transaction coordinator did not commit {operationName} '{transactionId}': {message}");
    }

    private sealed record RepoWriteTransactionPayload(string RelativePath, string ContentSha256, int ContentLength);

    private sealed record RepoEditTransactionPayload(string RelativePath, string OldSha256, string NewSha256, bool ReplaceAll);
}
