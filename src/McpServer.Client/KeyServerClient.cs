using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for keyserver endpoints used by transactional diffgram exchange.
/// FR-MCP-118, FR-MCP-120, FR-MCP-121.
/// </summary>
/// <seealso cref="McpServerClient.KeyServer"/>
public sealed class KeyServerClient : McpClientBase
{
    /// <inheritdoc />
    public KeyServerClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal KeyServerClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Registers or updates a transaction-security party.</summary>
    /// <param name="request">Party registration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registered party state.</returns>
    public async Task<PartyRegistrationResponse> RegisterPartyAsync(
        PartyRegistrationRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<PartyRegistrationResponse>("mcpserver/keyserver/parties", request, cancellationToken);

    /// <summary>Signs a canonical transaction manifest.</summary>
    /// <param name="request">Manifest signing payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Signed manifest response.</returns>
    public async Task<TransactionManifestSignResponse> SignManifestAsync(
        TransactionManifestSignRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<TransactionManifestSignResponse>("mcpserver/keyserver/manifests/sign", request, cancellationToken);

    /// <summary>Verifies a signed transaction manifest.</summary>
    /// <param name="request">Manifest verification payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification result.</returns>
    public async Task<TransactionManifestVerifyResponse> VerifyManifestAsync(
        TransactionManifestVerifyRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<TransactionManifestVerifyResponse>("mcpserver/keyserver/manifests/verify", request, cancellationToken);

    /// <summary>Gets persisted public trace metadata for a signed manifest.</summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Manifest trace record.</returns>
    public async Task<TransactionManifestTraceRecord> GetManifestAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
        => await GetAsync<TransactionManifestTraceRecord>(
            $"mcpserver/keyserver/manifests/{Encode(transactionId)}",
            cancellationToken);

    /// <summary>Gets one public key descriptor for a registered party.</summary>
    /// <param name="partyId">Party identifier.</param>
    /// <param name="keyId">Key identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Public key descriptor.</returns>
    public async Task<PartyKeyDescriptor> GetPartyKeyAsync(
        string partyId,
        string keyId,
        CancellationToken cancellationToken = default)
        => await GetAsync<PartyKeyDescriptor>(
            $"mcpserver/keyserver/parties/{Encode(partyId)}/keys/{Encode(keyId)}",
            cancellationToken);

    private static string Encode(string value) => Uri.EscapeDataString(value);
}
