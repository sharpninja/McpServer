using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-118 through FR-MCP-121: Keyserver endpoints for party registration,
/// key lookup, manifest signing, and manifest verification.
/// </summary>
[ApiController]
[Route("mcpserver/keyserver")]
public sealed class KeyServerController : ControllerBase
{
    private readonly IKeyServerPartyRegistry _partyRegistry;
    private readonly IKeyServerManifestService _manifestService;

    /// <summary>Initializes a new instance of the <see cref="KeyServerController"/> class.</summary>
    /// <param name="partyRegistry">Party/key registry.</param>
    /// <param name="manifestService">Manifest signing and verification service.</param>
    public KeyServerController(
        IKeyServerPartyRegistry partyRegistry,
        IKeyServerManifestService manifestService)
    {
        _partyRegistry = partyRegistry;
        _manifestService = manifestService;
    }

    /// <summary>Registers or updates a transaction-security party.</summary>
    /// <param name="request">Party registration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registered party response.</returns>
    [HttpPost("parties")]
    public async Task<ActionResult<PartyRegistrationResponse>> RegisterPartyAsync(
        [FromBody] PartyRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _partyRegistry.RegisterPartyAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>Gets one registered public key descriptor.</summary>
    /// <param name="partyId">Party identifier.</param>
    /// <param name="keyId">Key identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Public key descriptor.</returns>
    [HttpGet("parties/{partyId}/keys/{keyId}")]
    public async Task<ActionResult<PartyKeyDescriptor>> GetPartyKeyAsync(
        string partyId,
        string keyId,
        CancellationToken cancellationToken)
    {
        var key = await _partyRegistry.GetPartyKeyAsync(partyId, keyId, cancellationToken).ConfigureAwait(false);
        if (key is null)
            return NotFound(new { error = $"Key '{keyId}' for party '{partyId}' was not found." });

        return Ok(key);
    }

    /// <summary>Signs a canonical transaction manifest.</summary>
    /// <param name="request">Manifest signing payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Signed manifest response.</returns>
    [HttpPost("manifests/sign")]
    public async Task<ActionResult<TransactionManifestSignResponse>> SignManifestAsync(
        [FromBody] TransactionManifestSignRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _manifestService.SignManifestAsync(request, cancellationToken).ConfigureAwait(false);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>Verifies a signed transaction manifest.</summary>
    /// <param name="request">Manifest verification payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification response.</returns>
    [HttpPost("manifests/verify")]
    public async Task<ActionResult<TransactionManifestVerifyResponse>> VerifyManifestAsync(
        [FromBody] TransactionManifestVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _manifestService.VerifyManifestAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsValid ? Ok(response) : BadRequest(response);
    }
}
