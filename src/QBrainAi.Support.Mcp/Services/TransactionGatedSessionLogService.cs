using QBrainAi.Support.Mcp.Models;
using QBrainAi.TransactionSecurity;
using QBrainAi.TransactionSecurity.Options;
using QBrainAi.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001 / FR-MCP-173 / TR-MCP-TXNKEY-001: Session-log mutations persist
/// through the inner session-log service without coordinator or keyserver signing.
/// Keyserver remains limited to QuadBrain brain-slot transactions. Every session-log
/// operation is non-QuadBrain, so this gate keeps no snapshot, restore, or revive path
/// (retired 2026-10-13 by operator decision, PLAN-SESSIONTRIAGEFIX-001 A1, A2, A4); an
/// operation the keyserver scope would route to the coordinator fails closed instead of
/// running uncompensated. Applied workspace-stamp repair stays fail-closed while required
/// turn transactions are active.
/// </summary>
public sealed class TransactionGatedSessionLogService : ISessionLogService
{
    private readonly ISessionLogService _inner;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedSessionLogService"/> class.</summary>
    /// <param name="inner">Local session-log service that performs durable mutations.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction enforcement options.</param>
    public TransactionGatedSessionLogService(
        ISessionLogService inner,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<SessionLogQueryResult> QueryAsync(
        SessionLogQueryRequest request,
        CancellationToken cancellationToken = default)
        => _inner.QueryAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<UnifiedSessionLogDto?> GetAsync(
        string sourceType,
        string sessionId,
        CancellationToken cancellationToken = default)
        => _inner.GetAsync(sourceType, sessionId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsUnchangedAsync(
        string sourceType,
        string sessionId,
        string contentHash,
        CancellationToken cancellationToken = default)
        => _inner.IsUnchangedAsync(sourceType, sessionId, contentHash, cancellationToken);

    /// <inheritdoc />
    public Task<long> SubmitAsync(
        UnifiedSessionLogDto dto,
        string? sourceFilePath = null,
        string? contentHash = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return ExecuteMutationAsync(
            "sessionlog.submit",
            ct => _inner.SubmitAsync(dto, sourceFilePath, contentHash, ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> AppendProcessingDialogAsync(
        string sourceType,
        string sessionId,
        string requestId,
        IReadOnlyList<ProcessingDialogItemDto> items,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.dialog",
            ct => _inner.AppendProcessingDialogAsync(sourceType, sessionId, requestId, items, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<long> UpsertTurnAsync(
        string sourceType,
        string sessionId,
        UnifiedRequestEntryDto turn,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.upsert_turn",
            ct => _inner.UpsertTurnAsync(sourceType, sessionId, turn, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<long> ReplaceTurnAsync(
        string sourceType,
        string sessionId,
        UnifiedRequestEntryDto turn,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.replace_turn",
            ct => _inner.ReplaceTurnAsync(sourceType, sessionId, turn, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<long> SetSessionTitleAsync(
        string sourceType,
        string sessionId,
        string title,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.set_session_title",
            ct => _inner.SetSessionTitleAsync(sourceType, sessionId, title, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<long> SetTurnTitleAsync(
        string sourceType,
        string sessionId,
        string requestId,
        string title,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.set_turn_title",
            ct => _inner.SetTurnTitleAsync(sourceType, sessionId, requestId, title, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> ReplaceTurnSectionAsync(
        string sourceType,
        string sessionId,
        string requestId,
        string section,
        UnifiedRequestEntryDto payload,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.replace_section",
            ct => _inner.ReplaceTurnSectionAsync(sourceType, sessionId, requestId, section, payload, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> ClearTurnSectionAsync(
        string sourceType,
        string sessionId,
        string requestId,
        string section,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.clear_section",
            ct => _inner.ClearTurnSectionAsync(sourceType, sessionId, requestId, section, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> DeleteTurnItemAsync(
        string sourceType,
        string sessionId,
        string requestId,
        string section,
        string itemKey,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.delete_item",
            ct => _inner.DeleteTurnItemAsync(sourceType, sessionId, requestId, section, itemKey, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> DeleteTurnAsync(
        string sourceType,
        string sessionId,
        string requestId,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.delete_turn",
            ct => _inner.DeleteTurnAsync(sourceType, sessionId, requestId, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> DeleteSessionAsync(
        string sourceType,
        string sessionId,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.delete_session",
            ct => _inner.DeleteSessionAsync(sourceType, sessionId, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> OpenSessionAsync(
        string sourceType,
        string sessionId,
        string? title = null,
        string? model = null,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.open",
            ct => _inner.OpenSessionAsync(sourceType, sessionId, title, model, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<int> RepairWorkspaceStampsAsync(
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (dryRun)
            return _inner.RepairWorkspaceStampsAsync(true, cancellationToken);

        ThrowIfUncompensatedRepairBlocked();
        return _inner.RepairWorkspaceStampsAsync(false, cancellationToken);
    }

    // FR-MCP-173 / TR-MCP-TXNKEY-001: session-log operations bypass the coordinator and persist through
    // the inner service. No session-log operation name is a QuadBrain one, so the fail-closed branch only
    // guards against a change to the keyserver scope that would otherwise leave a write uncompensated.
    private async Task<TResult> ExecuteMutationAsync<TResult>(
        string operationName,
        Func<CancellationToken, Task<TResult>> mutation,
        CancellationToken cancellationToken)
    {
        if (!TurnTransactionKeyserverScope.ShouldBypassCoordinator(_coordinator, operationName))
        {
            throw new InvalidOperationException(
                $"Session-log operation '{operationName}' has no transaction-compensated path; FR-MCP-173 routes every session-log operation around the turn transaction coordinator.");
        }

        return await mutation(cancellationToken).ConfigureAwait(false);
    }

    private void ThrowIfUncompensatedRepairBlocked()
    {
        if (_coordinator is null)
            return;

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(status.Message)
                    ? "Turn transaction coordinator is degraded."
                    : status.Message);
        }

        if (status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true))
        {
            throw new InvalidOperationException(
                "Session-log workspace stamp repair is not transaction compensated while required turn transactions are active.");
        }
    }
}
