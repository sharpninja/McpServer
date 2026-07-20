using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-SEC-006: Follows a renamed brain-slot party id into the transaction-security key store.
/// </summary>
/// <remarks>
/// <para>
/// Trusted parties and their keys live in the TransactionSecurity key store, not in
/// <see cref="McpServer.Support.Mcp.Storage.McpDbContext"/>, so the EF migration that renamed the
/// Creativity/Logic brain-slot roles could rewrite <c>BrainSlotDefinitions.PartyId</c> but could not move the
/// signing key that still sits under the legacy hemisphere party id. Without this reconciliation an upgraded
/// installation reports the quad as NotReady because
/// <see cref="BrainSlotRegistryService.GetStatusAsync"/> cannot find an active signing key for the new party.
/// </para>
/// <para>
/// The reconciliation COPIES key material; it never moves it. Historical diffgram signatures reference the legacy
/// key id and must stay verifiable, so the legacy party and its key rows are left untouched. It is idempotent: it
/// is a no-op once the new key id exists, a no-op when no legacy key is found, and it never generates new key
/// material for the renamed party.
/// </para>
/// </remarks>
internal sealed class BrainSlotPartyKeyReconciler
{
    /// <summary>Renamed party id mapped to the legacy party id that may still hold its signing key.</summary>
    private static readonly Dictionary<string, string> LegacyPartyIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["brain-slot:creativity"] = "brain-slot:left-hemisphere",
        ["brain-slot:logic"] = "brain-slot:right-hemisphere",
    };

    private readonly IKeyServerPartyRegistry _partyRegistry;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="BrainSlotPartyKeyReconciler"/> class.</summary>
    /// <param name="partyRegistry">Trusted-party registry backing the brain-slot signing keys.</param>
    /// <param name="logger">Diagnostic logger owned by the calling registry service.</param>
    public BrainSlotPartyKeyReconciler(IKeyServerPartyRegistry partyRegistry, ILogger logger)
    {
        _partyRegistry = partyRegistry ?? throw new ArgumentNullException(nameof(partyRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Resolves the legacy party id for a renamed brain-slot party id.</summary>
    /// <param name="partyId">Current party id carried by the brain-slot row.</param>
    /// <returns>The legacy party id, or <see langword="null"/> when the party was never renamed.</returns>
    public static string? ResolveLegacyPartyId(string? partyId)
    {
        if (string.IsNullOrWhiteSpace(partyId))
            return null;
        return LegacyPartyIds.TryGetValue(partyId.Trim(), out var legacyPartyId) ? legacyPartyId : null;
    }

    /// <summary>
    /// TR-MCP-SEC-006: Copies the legacy party's active signing key onto the renamed party when the renamed party
    /// holds no key at its conventional signing key id.
    /// </summary>
    /// <param name="partyId">Current party id carried by the brain-slot row.</param>
    /// <param name="role">Brain-slot role, used for the registered party role label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when key material was copied; otherwise <see langword="false"/>.</returns>
    public async Task<bool> TryAdoptLegacySigningKeyAsync(
        string partyId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var legacyPartyId = ResolveLegacyPartyId(partyId);
        if (legacyPartyId is null)
            return false;

        var normalizedPartyId = partyId.Trim();
        var signingKeyId = BrainSlotValidation.SigningKeyId(normalizedPartyId);
        var existing = await _partyRegistry.GetPartyKeyAsync(normalizedPartyId, signingKeyId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
            return false;

        var legacySigningKey = await _partyRegistry
            .GetPartyKeyAsync(legacyPartyId, BrainSlotValidation.SigningKeyId(legacyPartyId), cancellationToken)
            .ConfigureAwait(false);
        if (!IsAdoptableSigningKey(legacySigningKey))
            return false;

        var legacyEncryptionKey = await _partyRegistry
            .GetPartyKeyAsync(legacyPartyId, BrainSlotValidation.EncryptionKeyId(legacyPartyId), cancellationToken)
            .ConfigureAwait(false);

        await _partyRegistry.RegisterPartyAsync(
            new PartyRegistrationRequest
            {
                PartyId = normalizedPartyId,
                Role = BrainSlotValidation.DefaultPartyId(role),
                ActiveSigningKeyId = signingKeyId,
                ActiveEncryptionKeyId = BrainSlotValidation.EncryptionKeyId(normalizedPartyId),
                SigningPublicKeyPem = legacySigningKey!.PublicKeyPem,
                EncryptionPublicKeyPem = legacyEncryptionKey?.PublicKeyPem,
                Status = "active",
            },
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Brain-slot party {PartyId} adopted the signing key material held by legacy party {LegacyPartyId}; the legacy party and key rows are preserved.",
            normalizedPartyId,
            legacyPartyId);
        return true;
    }

    /// <summary>Returns true when the legacy descriptor is an active signing key carrying public material.</summary>
    /// <param name="key">Legacy key descriptor, when present.</param>
    /// <returns><see langword="true"/> when the descriptor may be copied forward.</returns>
    private static bool IsAdoptableSigningKey(PartyKeyDescriptor? key)
        => key is not null
            && !string.IsNullOrWhiteSpace(key.PublicKeyPem)
            && string.Equals(key.Purpose, "signing", StringComparison.OrdinalIgnoreCase)
            && string.Equals(key.Status, "active", StringComparison.OrdinalIgnoreCase);
}
