using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for signed federation operation envelopes.</summary>
public sealed class FederationEnvelopeSignerTests
{
    /// <summary>Signed envelopes verify successfully before expiry.</summary>
    [Fact]
    public void Sign_ProducesVerifiableEnvelope()
    {
        var sut = CreateSigner();

        var envelope = sut.Sign(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
        }, "PAYTON-LEGION2", "PAYTON-DESKTOP");

        var result = sut.Verify(envelope, "PAYTON-DESKTOP");

        Assert.True(result.IsValid, result.Error);
        Assert.Equal("HMAC-SHA256", envelope.Signature?.Algorithm);
        Assert.NotEmpty(envelope.BodySha256);
    }

    /// <summary>Body tampering is rejected through the body hash check.</summary>
    [Fact]
    public void Verify_RejectsBodyTampering()
    {
        var sut = CreateSigner();
        var envelope = sut.Sign(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
            BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
        }, "PAYTON-LEGION2");
        envelope.Operation.BodyBase64 = Convert.ToBase64String("{\"changed\":true}"u8.ToArray());

        var result = sut.Verify(envelope);

        Assert.False(result.IsValid);
        Assert.Contains("body hash", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Malformed Base64 operation bodies are rejected without throwing.</summary>
    [Fact]
    public void Verify_RejectsMalformedBodyBase64()
    {
        var sut = CreateSigner();
        var envelope = sut.Sign(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
            BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
        }, "PAYTON-LEGION2");
        envelope.Operation.BodyBase64 = "not-base64";

        var result = sut.Verify(envelope);

        Assert.False(result.IsValid);
        Assert.Contains("base64", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Target mismatches are rejected before local apply.</summary>
    [Fact]
    public void Verify_RejectsWrongTarget()
    {
        var sut = CreateSigner();
        var envelope = sut.Sign(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
        }, "hub", "PAYTON-LEGION2");

        var result = sut.Verify(envelope, "PAYTON-DESKTOP");

        Assert.False(result.IsValid);
        Assert.Contains("target proxy", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Expired envelopes are rejected.</summary>
    [Fact]
    public void Verify_RejectsExpiredEnvelope()
    {
        var sut = CreateSigner();
        var envelope = sut.Sign(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
        }, "PAYTON-LEGION2");
        envelope.ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1);

        var result = sut.Verify(envelope);

        Assert.False(result.IsValid);
        Assert.Contains("expired", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Signing options can disable envelope signing even when a key exists.</summary>
    [Fact]
    public void IsConfigured_ReturnsFalseWhenSigningDisabled()
    {
        var monitor = Substitute.For<IOptionsMonitor<FederationOptions>>();
        monitor.CurrentValue.Returns(new FederationOptions
        {
            EnrollmentToken = "test-secret",
            Signing = new FederationSigningOptions
            {
                Enabled = false,
                EnvelopeTtlSeconds = 300,
            },
        });
        var sut = new FederationEnvelopeSigner(monitor);

        Assert.False(sut.IsConfigured);
    }

    private static FederationEnvelopeSigner CreateSigner()
    {
        var monitor = Substitute.For<IOptionsMonitor<FederationOptions>>();
        monitor.CurrentValue.Returns(new FederationOptions
        {
            EnrollmentToken = "test-secret",
            Signing = new FederationSigningOptions
            {
                Enabled = true,
                EnvelopeTtlSeconds = 300,
            },
        });
        return new FederationEnvelopeSigner(monitor);
    }
}
