namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Shared per-turn orchestration context passed by object reference to every role strategy.
/// This <see cref="TransactionId"/> is the upstream turn identifier, not per-invocation brain-slot-{guid}.
/// </summary>
public sealed class BrainSlotTurnContext
{
    private readonly Dictionary<string, string> _evidence = new(StringComparer.Ordinal);

    /// <summary>Original user input for the QuadBrain turn.</summary>
    public required string OriginalInput { get; init; }

    /// <summary>Optional session identifier from metadata.</summary>
    public string? SessionId { get; init; }

    /// <summary>Owning session-log turn identifier.</summary>
    public string? TurnId { get; init; }

    /// <summary>Upstream turn transaction identifier (not the per-invocation brain-slot id).</summary>
    public string? TransactionId { get; init; }

    /// <summary>Committed role outputs, mutated only between awaited orchestration stages.</summary>
    public IReadOnlyDictionary<string, string> CommittedRoleEvidence => _evidence;

    /// <summary>Records or replaces committed evidence for a canonical role key.</summary>
    public void SetCommittedEvidence(string role, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        if (string.IsNullOrWhiteSpace(value))
        {
            _evidence.Remove(role);
            return;
        }

        _evidence[role] = value;
    }

    /// <summary>Builds a fallback context when invocation is called without an orchestration-owned instance.</summary>
    public static BrainSlotTurnContext FromInvokeRequest(BrainSlotInvokeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        string? sessionId = null;
        string? transactionId = null;
        request.Metadata?.TryGetValue("sessionId", out sessionId);
        request.Metadata?.TryGetValue("transactionId", out transactionId);
        return new BrainSlotTurnContext
        {
            OriginalInput = request.Input,
            TurnId = request.TurnId,
            SessionId = sessionId,
            TransactionId = transactionId,
        };
    }
}
