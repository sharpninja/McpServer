using System.Security.Cryptography;
using System.Text;
using QBrainAi.Support.Mcp.Options;
using Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>FR-MCP-103: HMAC-SHA256 implementation of federation envelope signing.</summary>
public sealed class FederationEnvelopeSigner : IFederationEnvelopeSigner
{
    private const string Algorithm = "HMAC-SHA256";
    private const string Canonicalization = "federation-envelope-v1";

    private readonly IOptionsMonitor<FederationOptions> _options;
    private readonly TimeProvider _time;

    /// <summary>Initializes a new instance of the <see cref="FederationEnvelopeSigner"/> class.</summary>
    /// <param name="options">Federation options containing the shared signing secret.</param>
    /// <param name="time">The clock signing stamps and verification checks against; null uses <see cref="TimeProvider.System"/>.</param>
    /// <remarks>2026-11-05, FR-MCP-103; TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): added the optional <paramref name="time"/> clock, read once by Sign and once by Verify instead of DateTimeOffset.UtcNow, because hostile validation round 12 finding R12-F2 and its sibling sweep showed the envelope expiry and future-issue checks ran on the live clock in unit cases (mock gate ug-r13z4-mockgate-b-1).</remarks>
    public FederationEnvelopeSigner(IOptionsMonitor<FederationOptions> options, TimeProvider? time = null)
    {
        _options = options;
        _time = time ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public bool IsConfigured
    {
        get
        {
            var options = _options.CurrentValue;
            return options.Signing.Enabled &&
                   (!string.IsNullOrWhiteSpace(options.Signing.SharedSecret) ||
                    !string.IsNullOrWhiteSpace(options.EnrollmentToken));
        }
    }

    /// <inheritdoc />
    /// <remarks>2026-11-05, FR-MCP-103; TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): stamps the envelope's issue and expiry times from the injected clock, read once, instead of DateTimeOffset.UtcNow (hostile validation round 12 finding R12-F2 and its sibling sweep; mock gate ug-r13z4-mockgate-b-1). This member's own entry was added after the seventh holistic read before hostile validation round 13 (finding c5); until then the change was logged only on the constructor.</remarks>
    public FederationExecutionEnvelope Sign(
        FederationOperationRequest operation,
        string sourceProxyId,
        string? targetProxyId = null,
        string applyMode = "state")
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceProxyId);
        if (!_options.CurrentValue.Signing.Enabled)
            throw new InvalidOperationException("Federation envelope signing is disabled.");

        var now = _time.GetUtcNow();
        var ttl = Math.Max(1, _options.CurrentValue.Signing.EnvelopeTtlSeconds);
        var envelope = new FederationExecutionEnvelope
        {
            SourceProxyId = sourceProxyId.Trim(),
            TargetProxyId = string.IsNullOrWhiteSpace(targetProxyId) ? null : targetProxyId.Trim(),
            Operation = operation,
            IssuedAtUtc = now,
            ExpiresAtUtc = now.AddSeconds(ttl),
            BodySha256 = ComputeBodyHash(operation.BodyBase64),
            ApplyMode = string.IsNullOrWhiteSpace(applyMode) ? "state" : applyMode.Trim(),
        };
        envelope.Signature = new FederationEnvelopeSignature
        {
            Algorithm = Algorithm,
            Canonicalization = Canonicalization,
            Value = ComputeSignature(envelope),
        };
        return envelope;
    }

    /// <inheritdoc />
    /// <remarks>2026-11-05, FR-MCP-103; TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): checks the envelope's expiry and future issue against the injected clock, read once, instead of DateTimeOffset.UtcNow (hostile validation round 12 finding R12-F2 and its sibling sweep; mock gate ug-r13z4-mockgate-b-1). This member's own entry was added after the seventh holistic read before hostile validation round 13 (finding c5); until then the change was logged only on the constructor.</remarks>
    public FederationEnvelopeVerificationResult Verify(FederationExecutionEnvelope envelope, string? expectedTargetProxyId = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.SchemaVersion != 1)
            return Invalid("Unsupported envelope schema version.");
        if (envelope.Signature is null)
            return Invalid("Envelope signature is missing.");
        if (!IsConfigured)
            return Invalid("Federation envelope signing key is not configured.");
        if (!string.Equals(envelope.Signature.Algorithm, Algorithm, StringComparison.Ordinal) ||
            !string.Equals(envelope.Signature.Canonicalization, Canonicalization, StringComparison.Ordinal))
        {
            return Invalid("Envelope signature metadata is unsupported.");
        }

        var now = _time.GetUtcNow();
        if (envelope.ExpiresAtUtc < now)
            return Invalid("Envelope has expired.");
        if (envelope.IssuedAtUtc > now.AddMinutes(5))
            return Invalid("Envelope issue timestamp is in the future.");
        if (!string.IsNullOrWhiteSpace(expectedTargetProxyId) &&
            !string.Equals(envelope.TargetProxyId, expectedTargetProxyId, StringComparison.OrdinalIgnoreCase))
        {
            return Invalid("Envelope target proxy does not match this proxy.");
        }

        string actualBodyHash;
        try
        {
            actualBodyHash = ComputeBodyHash(envelope.Operation.BodyBase64);
        }
        catch (FormatException)
        {
            return Invalid("Envelope operation body is not valid base64.");
        }

        if (!string.Equals(envelope.BodySha256, actualBodyHash, StringComparison.OrdinalIgnoreCase))
            return Invalid("Envelope body hash does not match operation body.");

        if (string.IsNullOrWhiteSpace(envelope.Signature.Value))
            return Invalid("Envelope signature value is missing.");

        var expectedSignature = ComputeSignature(envelope);
        byte[] suppliedBytes;
        try
        {
            suppliedBytes = Convert.FromHexString(envelope.Signature.Value);
        }
        catch (FormatException)
        {
            return Invalid("Envelope signature is not hexadecimal.");
        }

        var expectedBytes = Convert.FromHexString(expectedSignature);
        return CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes)
            ? new FederationEnvelopeVerificationResult { IsValid = true }
            : Invalid("Envelope signature is invalid.");
    }

    private string ComputeSignature(FederationExecutionEnvelope envelope)
    {
        var key = ResolveSigningKey();
        var payload = BuildCanonicalPayload(envelope);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private string ResolveSigningKey()
    {
        var options = _options.CurrentValue;
        var key = !string.IsNullOrWhiteSpace(options.Signing.SharedSecret)
            ? options.Signing.SharedSecret
            : options.EnrollmentToken;
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Mcp:Federation:Signing:SharedSecret or Mcp:Federation:EnrollmentToken is required for signed federation envelopes.");

        return key;
    }

    private static string BuildCanonicalPayload(FederationExecutionEnvelope envelope)
    {
        var op = envelope.Operation;
        var lines = new[]
        {
            Canonicalization,
            envelope.SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            envelope.EnvelopeId,
            envelope.SourceProxyId,
            envelope.TargetProxyId ?? string.Empty,
            op.OperationId ?? string.Empty,
            op.ProxyId,
            op.SourceOperationId ?? string.Empty,
            op.GlobalWorkspaceId ?? string.Empty,
            op.Domain,
            op.ResourceId ?? string.Empty,
            op.HttpMethod ?? string.Empty,
            op.Path ?? string.Empty,
            op.Method ?? string.Empty,
            op.HeadersJson ?? string.Empty,
            op.BodyBase64 ?? string.Empty,
            op.BaseVersion ?? string.Empty,
            envelope.IssuedAtUtc.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            envelope.ExpiresAtUtc.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            envelope.Nonce,
            envelope.BodySha256,
            envelope.ApplyMode,
        };
        return string.Join('\n', lines);
    }

    private static string ComputeBodyHash(string? bodyBase64)
    {
        var body = string.IsNullOrWhiteSpace(bodyBase64)
            ? []
            : Convert.FromBase64String(bodyBase64);
        return Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant();
    }

    private static FederationEnvelopeVerificationResult Invalid(string error)
        => new() { IsValid = false, Error = error };
}
