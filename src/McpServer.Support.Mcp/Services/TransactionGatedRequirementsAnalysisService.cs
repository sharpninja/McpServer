using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Fails closed for TODO requirements analysis while required
/// turn transactions are active because analyzer document side effects are not compensated.
/// </summary>
public sealed class TransactionGatedRequirementsAnalysisService : IRequirementsService
{
    private readonly IRequirementsService _inner;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionGatedRequirementsAnalysisService"/> class.
    /// </summary>
    /// <param name="inner">Underlying requirements analyzer service.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional turn transaction options.</param>
    public TransactionGatedRequirementsAnalysisService(
        IRequirementsService inner,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<RequirementsAnalysisResult> AnalyzeAsync(
        string todoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(todoId);

        if (_coordinator is null)
            return _inner.AnalyzeAsync(todoId, cancellationToken);

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            return Task.FromResult(new RequirementsAnalysisResult(
                false,
                Error: string.IsNullOrWhiteSpace(status.Message)
                    ? "Turn transaction coordinator is degraded."
                    : status.Message));
        }

        if (RequiresMutationTransactions(status))
        {
            return Task.FromResult(new RequirementsAnalysisResult(
                false,
                Error: "TODO requirements analysis is not transaction compensated while required turn transactions are active."));
        }

        return _inner.AnalyzeAsync(todoId, cancellationToken);
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);
}
