namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-103: Signs and verifies federation operation envelopes.</summary>
public interface IFederationEnvelopeSigner
{
    /// <summary>Whether a signing key is configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Creates and signs an envelope for a federation operation.</summary>
    /// <param name="operation">Operation to carry in the envelope.</param>
    /// <param name="sourceProxyId">Envelope issuer proxy identifier.</param>
    /// <param name="targetProxyId">Destination proxy identifier, or null for hub intake.</param>
    /// <param name="applyMode">Apply mode such as <c>state</c>.</param>
    /// <returns>A signed envelope.</returns>
    FederationExecutionEnvelope Sign(
        FederationOperationRequest operation,
        string sourceProxyId,
        string? targetProxyId = null,
        string applyMode = "state");

    /// <summary>Verifies an envelope signature and freshness.</summary>
    /// <param name="envelope">Envelope to verify.</param>
    /// <param name="expectedTargetProxyId">Optional required target proxy identifier.</param>
    /// <returns>Verification result.</returns>
    FederationEnvelopeVerificationResult Verify(FederationExecutionEnvelope envelope, string? expectedTargetProxyId = null);
}
