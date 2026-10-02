namespace McpServer.TransactionSecurity;

/// <summary>
/// FR-MCP-173: keyserver manifest signing is limited to QuadBrain transactions.
/// General-agent first-party mutations (TODO, requirements, session-log, memory, and
/// other TransactionGated adapters) must not call the keyserver.
/// </summary>
public static class TurnTransactionKeyserverScope
{
    /// <summary>Party-id prefix used by QuadBrain brain-slot publishers.</summary>
    public const string BrainSlotPartyPrefix = "brain-slot:";

    /// <summary>Operation-name prefix used by brain-slot invocation and weight update.</summary>
    public const string BrainSlotOperationPrefix = "brain-slot.";

    /// <summary>Operation-name prefix reserved for QuadBrain orchestration mutations.</summary>
    public const string QuadBrainOperationPrefix = "quadbrain.";

    /// <summary>
    /// Returns <see langword="true"/> when the request is a QuadBrain/brain-slot
    /// transaction that may use keyserver signing.
    /// </summary>
    /// <param name="request">Turn transaction request.</param>
    /// <returns>Whether keyserver signing is required.</returns>
    public static bool RequiresKeyserver(Models.TurnTransactionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return RequiresKeyserver(request.PublisherPartyId, request.OperationName);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the publisher party or operation name
    /// identifies a QuadBrain/brain-slot transaction.
    /// </summary>
    /// <param name="publisherPartyId">Optional publisher party id.</param>
    /// <param name="operationName">Logical operation name.</param>
    /// <returns>Whether keyserver signing is required.</returns>
    public static bool RequiresKeyserver(string? publisherPartyId, string? operationName)
    {
        if (!string.IsNullOrWhiteSpace(publisherPartyId) &&
            publisherPartyId.StartsWith(BrainSlotPartyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(operationName) &&
            (operationName.StartsWith(BrainSlotOperationPrefix, StringComparison.OrdinalIgnoreCase) ||
             operationName.StartsWith(QuadBrainOperationPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> when a TransactionGated adapter should skip the
    /// coordinator entirely (no keyserver, no coordinator round-trip).
    /// </summary>
    /// <param name="coordinator">Optional coordinator.</param>
    /// <param name="operationName">Logical operation name.</param>
    /// <param name="publisherPartyId">Optional publisher party id.</param>
    /// <returns>Whether the adapter should invoke the inner mutation directly.</returns>
    public static bool ShouldBypassCoordinator(
        Services.ITurnTransactionCoordinator? coordinator,
        string operationName,
        string? publisherPartyId = null)
        => coordinator is null || !RequiresKeyserver(publisherPartyId, operationName);
}
