using System.Runtime.CompilerServices;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Fails closed for voice/external agent mutations while required
/// turn transactions are active because interactive process side effects are not compensated.
/// </summary>
public sealed class TransactionGatedVoiceConversationService : IVoiceConversationService
{
    private const string DeferredVoiceMutationMessage =
        "Voice and interactive agent mutations are not transaction compensated while required turn transactions are active.";

    private readonly IVoiceConversationService _inner;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedVoiceConversationService"/> class.</summary>
    /// <param name="inner">Underlying voice conversation service.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction enforcement options.</param>
    public TransactionGatedVoiceConversationService(
        IVoiceConversationService inner,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<VoiceSessionCreateResponse> CreateSessionAsync(
        VoiceSessionCreateRequest? request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfMutationBlocked();
        return _inner.CreateSessionAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<VoiceTurnResponse?> SubmitTurnAsync(
        string sessionId,
        VoiceTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfMutationBlocked();
        return _inner.SubmitTurnAsync(sessionId, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<VoiceInterruptResponse?> InterruptAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ThrowIfMutationBlocked();
        return _inner.InterruptAsync(sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> SendEscapeAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ThrowIfMutationBlocked();
        return _inner.SendEscapeAsync(sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> SendSessionMessageAsync(
        string sessionId,
        string message,
        CancellationToken cancellationToken = default)
    {
        ThrowIfMutationBlocked();
        return _inner.SendSessionMessageAsync(sessionId, message, cancellationToken);
    }

    /// <inheritdoc />
    public Task<VoiceSessionStatusDto?> GetStatusAsync(string sessionId, CancellationToken cancellationToken = default)
        => _inner.GetStatusAsync(sessionId, cancellationToken);

    /// <inheritdoc />
    public Task<VoiceTranscriptResponse?> GetTranscriptAsync(string sessionId, CancellationToken cancellationToken = default)
        => _inner.GetTranscriptAsync(sessionId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ThrowIfMutationBlocked();
        return _inner.DeleteSessionAsync(sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public VoiceSessionStatusDto? FindSessionByDevice(string deviceId)
        => _inner.FindSessionByDevice(deviceId);

    /// <inheritdoc />
    public IAsyncEnumerable<VoiceTurnStreamEvent> SubmitTurnStreamingAsync(
        string sessionId,
        VoiceTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        if (ShouldDeferMutation(out var error))
            return BlockedStream(error, cancellationToken);

        return _inner.SubmitTurnStreamingAsync(sessionId, request, cancellationToken);
    }

    private void ThrowIfMutationBlocked()
    {
        if (ShouldDeferMutation(out var error))
            throw new InvalidOperationException(error);
    }

    private bool ShouldDeferMutation(out string error)
    {
        error = string.Empty;
        if (_coordinator is null)
            return false;

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            error = string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message;
            return true;
        }

        if (!RequiresMutationTransactions(status))
            return false;

        error = DeferredVoiceMutationMessage;
        return true;
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);

    private static async IAsyncEnumerable<VoiceTurnStreamEvent> BlockedStream(
        string error,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new VoiceTurnStreamEvent { Type = "error", Message = error, Status = "error" };
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
