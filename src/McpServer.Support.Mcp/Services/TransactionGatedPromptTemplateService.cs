using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Executes prompt-template mutations through the turn transaction coordinator.
/// </summary>
public sealed class TransactionGatedPromptTemplateService : IPromptTemplateService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPromptTemplateService _inner;
    private readonly IPromptTemplateCompensation? _compensation;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;
    private long _lastSequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedPromptTemplateService"/> class.</summary>
    /// <param name="inner">Underlying prompt-template service.</param>
    /// <param name="compensation">Optional prompt-template storage compensation service.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction options.</param>
    public TransactionGatedPromptTemplateService(
        IPromptTemplateService inner,
        IPromptTemplateCompensation? compensation = null,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _compensation = compensation;
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<PromptTemplateQueryResult> QueryAsync(
        string? category = null,
        string? tag = null,
        string? keyword = null,
        CancellationToken cancellationToken = default)
        => _inner.QueryAsync(category, tag, keyword, cancellationToken);

    /// <inheritdoc />
    public Task<PromptTemplate?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _inner.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<PromptTemplateMutationResult> CreateAsync(
        PromptTemplateCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteMutationAsync(
            "prompt_template.create",
            new PromptTemplateCreateTransactionPayload(
                request.Id,
                request.Title,
                request.Category,
                ComputeOptionalSha256(request.Content),
                request.Content?.Length ?? 0,
                request.Tags,
                request.Engine),
            ct => _inner.CreateAsync(request, ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<PromptTemplateMutationResult> UpdateAsync(
        string id,
        PromptTemplateUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteMutationAsync(
            "prompt_template.update",
            new PromptTemplateUpdateTransactionPayload(
                id,
                request.Title,
                request.Category,
                request.Content is null ? null : ComputeOptionalSha256(request.Content),
                request.Content?.Length,
                request.Tags,
                request.Engine),
            ct => _inner.UpdateAsync(id, request, ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<PromptTemplateMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "prompt_template.delete",
            new PromptTemplateDeleteTransactionPayload(id),
            ct => _inner.DeleteAsync(id, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<PromptTemplateTestResult> TestAsync(
        string id,
        PromptTemplateTestRequest request,
        CancellationToken cancellationToken = default)
        => _inner.TestAsync(id, request, cancellationToken);

    /// <inheritdoc />
    public Task<PromptTemplateTestResult> TestInlineAsync(
        PromptTemplateTestRequest request,
        CancellationToken cancellationToken = default)
        => _inner.TestInlineAsync(request, cancellationToken);

    private async Task<PromptTemplateMutationResult> ExecuteMutationAsync(
        string operationName,
        object operationBody,
        Func<CancellationToken, Task<PromptTemplateMutationResult>> mutation,
        CancellationToken cancellationToken)
    {
        if (_coordinator is null)
            return await mutation(cancellationToken).ConfigureAwait(false);

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            return new PromptTemplateMutationResult(
                false,
                string.IsNullOrWhiteSpace(status.Message)
                    ? "Turn transaction coordinator is degraded."
                    : status.Message);
        }

        var requiresMutationTransactions = RequiresMutationTransactions(status);
        if (requiresMutationTransactions && _compensation is null)
            return new PromptTemplateMutationResult(false, "Prompt-template storage does not support transaction rollback compensation.");

        PromptTemplateMutationResult? mutationResult = null;
        var hasMutationResult = false;
        var transaction = BuildTransactionRequest(operationName, operationBody);
        var result = await _coordinator.ExecuteAsync(
                transaction,
                async ct =>
                {
                    var before = _compensation is null
                        ? null
                        : await _compensation.CaptureFileAsync(ct).ConfigureAwait(false);

                    mutationResult = await mutation(ct).ConfigureAwait(false);
                    hasMutationResult = true;

                    PromptTemplateFileSnapshot? after = null;
                    if (mutationResult.Success && _compensation is not null)
                        after = await _compensation.CaptureFileAsync(ct).ConfigureAwait(false);

                    return new TurnMutationResult
                    {
                        Success = mutationResult.Success,
                        ResultJson = JsonSerializer.Serialize(mutationResult, JsonOptions),
                        Error = mutationResult.Error,
                        RollbackAsync = mutationResult.Success && before is not null && after is not null
                            ? rollbackCt => RestoreFileOrThrowAsync(before, after.ContentSha256, rollbackCt)
                            : null,
                    };
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (hasMutationResult && (!mutationResult!.Success || IsTransactionSuccess(result)))
            return mutationResult;

        return ToTransactionFailure(operationName, result);
    }

    private async Task RestoreFileOrThrowAsync(
        PromptTemplateFileSnapshot snapshot,
        string expectedCurrentContentSha256,
        CancellationToken cancellationToken)
    {
        if (_compensation is null)
            throw new InvalidOperationException("Prompt-template storage does not support transaction rollback compensation.");

        await _compensation.RestoreFileAsync(snapshot, expectedCurrentContentSha256, cancellationToken).ConfigureAwait(false);
    }

    private TurnTransactionRequest BuildTransactionRequest(string operationName, object operationBody)
    {
        var sequence = NextSequence();
        return new TurnTransactionRequest
        {
            TurnId = $"{operationName}-{sequence}",
            OperationName = operationName,
            OperationBodyJson = JsonSerializer.Serialize(operationBody, JsonOptions),
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

    private static PromptTemplateMutationResult ToTransactionFailure(string operationName, TurnTransactionResult result)
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

        return new PromptTemplateMutationResult(
            false,
            $"Turn transaction coordinator did not commit {operationName} '{transactionId}': {message}");
    }

    private static string? ComputeOptionalSha256(string? value)
        => value is null ? null : ComputeSha256(value);

    private static string ComputeSha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record PromptTemplateCreateTransactionPayload(
        string Id,
        string Title,
        string Category,
        string? ContentSha256,
        int ContentLength,
        IReadOnlyList<string>? Tags,
        string? Engine);

    private sealed record PromptTemplateUpdateTransactionPayload(
        string Id,
        string? Title,
        string? Category,
        string? ContentSha256,
        int? ContentLength,
        IReadOnlyList<string>? Tags,
        string? Engine);

    private sealed record PromptTemplateDeleteTransactionPayload(string Id);
}
