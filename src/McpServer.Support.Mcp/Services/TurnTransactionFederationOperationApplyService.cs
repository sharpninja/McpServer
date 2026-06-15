using System.Text.Json;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Gates global federation mutation-adapter applies through
/// the turn transaction coordinator before reporting successful apply.
/// </summary>
public sealed class TurnTransactionFederationOperationApplyService : IFederationOperationApplyService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly FederationOperationApplyService _inner;
    private readonly ITurnTransactionCoordinator _coordinator;
    private long _lastSequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Initializes a new instance of the <see cref="TurnTransactionFederationOperationApplyService"/> class.</summary>
    /// <param name="inner">Inner adapter apply service.</param>
    /// <param name="coordinator">Turn transaction coordinator.</param>
    public TurnTransactionFederationOperationApplyService(
        FederationOperationApplyService inner,
        ITurnTransactionCoordinator coordinator)
    {
        _inner = inner;
        _coordinator = coordinator;
    }

    /// <inheritdoc />
    public async ValueTask<FederationApplyResult> ApplyAsync(
        FederationOperationRequest operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            return new FederationApplyResult
            {
                Applied = false,
                Conflict = true,
                Message = string.IsNullOrWhiteSpace(status.Message)
                    ? "Turn transaction coordinator is degraded."
                    : status.Message,
            };
        }

        FederationApplyResult? applyResult = null;
        var transaction = BuildTransactionRequest(operation);
        var result = await _coordinator.ExecuteAsync(
                transaction,
                async ct =>
                {
                    applyResult = await _inner.ApplyAsync(operation, ct).ConfigureAwait(false);
                    return ToMutationResult(applyResult);
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (IsTransactionSuccess(result) && applyResult is not null)
            return applyResult;

        if (applyResult is not null && applyResult.Conflict)
            return applyResult;

        return new FederationApplyResult
        {
            Applied = false,
            Conflict = true,
            Version = applyResult?.Version,
            Message = BuildFailureMessage(result),
        };
    }

    private TurnTransactionRequest BuildTransactionRequest(FederationOperationRequest operation)
    {
        var sequence = NextSequence();
        var transactionId = NormalizeOptional(operation.OperationId);
        var turnId = NormalizeOptional(operation.SourceOperationId)
            ?? transactionId
            ?? $"federation-apply-{sequence}";

        return new TurnTransactionRequest
        {
            TransactionId = transactionId,
            TurnId = turnId,
            OperationName = ResolveOperationName(operation),
            OperationBodyJson = JsonSerializer.Serialize(operation, JsonOptions),
            Sequence = sequence,
            Mutating = true,
        };
    }

    private static string ResolveOperationName(FederationOperationRequest operation)
    {
        if (!string.IsNullOrWhiteSpace(operation.Method))
            return operation.Method.Trim();

        if (!string.IsNullOrWhiteSpace(operation.HttpMethod) || !string.IsNullOrWhiteSpace(operation.Path))
            return $"{operation.HttpMethod?.Trim() ?? "APPLY"} {operation.Path?.Trim() ?? NormalizeDomain(operation.Domain)}".Trim();

        return $"federation.{NormalizeDomain(operation.Domain)}.apply";
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeDomain(string? domain)
        => string.IsNullOrWhiteSpace(domain) ? "unknown" : domain.Trim();

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

    private static TurnMutationResult ToMutationResult(FederationApplyResult result)
        => new()
        {
            Success = result.Applied || result.AlreadyApplied,
            ResultJson = JsonSerializer.Serialize(result, JsonOptions),
            Error = result.Conflict ? result.Message : null,
        };

    private static bool IsTransactionSuccess(TurnTransactionResult result)
        => string.Equals(result.Status, "committed", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(result.Status, "bypassed", StringComparison.OrdinalIgnoreCase);

    private static string BuildFailureMessage(TurnTransactionResult result)
    {
        var message = string.IsNullOrWhiteSpace(result.Message)
            ? result.Reason.ToString()
            : result.Message;
        return $"Turn transaction coordinator did not commit federation apply '{result.TransactionId}': {message}";
    }
}
