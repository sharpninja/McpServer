using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>Tests for signed federation operation envelopes.</summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-05</term><description>FR-MCP-103; TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): every signer reads the class's settable clock, so signing stamps and the expiry and future-issue checks are decided by the test's instants, and the D4 bindings of AgeBoundaryContractTests were added, because the sibling sweep of hostile validation round 12 finding R12-F2 found these cases on the live clock; the assertions are unchanged.</description></item>
/// <item><term>2026-11-05</term><description>FR-MCP-103; TR-MCP-USECASE-018 AC-006: the two D4 bindings and Operation() sign and verify with the generic proxy ids proxy-a and proxy-b instead of lab machine names, because the tenth holistic read before hostile validation round 13 (finding c5) found these slice Z4 lines naming the lab, against the plan 27.7 scrub of this pull request; the assertions are unchanged.</description></item>
/// </list>
/// </remarks>
public sealed class FederationEnvelopeSignerTests
{
    /// <summary>FR-MCP-103; TR-MCP-USECASE-018 AC-006: the settable clock every signer of this class reads, set to the first age-boundary reference.</summary>
    /// <remarks>2026-11-05, FR-MCP-103; TR-MCP-USECASE-018 AC-006: this entry documents the field, added in slice Z4 (the sibling sweep of hostile validation round 12 finding R12-F2), because the ninth holistic read before hostile validation round 13 (finding e1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private readonly QBrainAi.Support.Mcp.Tests.TestSupport.SettableTimeProvider _clock =
        new(QBrainAi.Support.Mcp.Tests.TestSupport.AgeBoundaryContractTests.References[0]);

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
        envelope.ExpiresAtUtc = _clock.GetUtcNow().AddSeconds(-1);

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
        var sut = new FederationEnvelopeSigner(monitor, _clock);

        Assert.False(sut.IsConfigured);
    }

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 binding, decision D4 expiry of
    /// <see cref="QBrainAi.Support.Mcp.Tests.TestSupport.AgeBoundaryContractTests"/>: for each scripted reference time and
    /// each contract row, an envelope signed so that it expires at the row's time is rejected as expired at the reference
    /// exactly when the contract says (valid at its expiry instant, because only an expiry before now is rejected), and an
    /// envelope that is not expired verifies. Test data: the contract's times; the 300 second TTL of the class's signer.
    /// </summary>
    /// <remarks>2026-11-05, FR-MCP-103; TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): added for the sibling sweep of hostile validation round 12 finding R12-F2.</remarks>
    [Fact]
    public void EnvelopeExpiry_UsesScriptedClock() =>
        QBrainAi.Support.Mcp.Tests.TestSupport.AgeBoundaryContractTests.AssertDecision("D4 federation envelope expiry", (reference, recorded) =>
        {
            var sut = CreateSigner();
            _clock.Set(recorded.AddSeconds(-300));
            var envelope = sut.Sign(Operation(), "proxy-a", "proxy-b");
            Assert.Equal(recorded, envelope.ExpiresAtUtc);
            _clock.Set(reference);
            var result = sut.Verify(envelope, "proxy-b");
            var expired = !result.IsValid && (result.Error ?? string.Empty).Contains("expired", StringComparison.OrdinalIgnoreCase);
            Assert.True(expired || result.IsValid, result.Error);
            return expired;
        });

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 binding, decision D4 future issue of
    /// <see cref="QBrainAi.Support.Mcp.Tests.TestSupport.AgeBoundaryContractTests"/>: for each scripted reference time and
    /// each contract row, an envelope issued at the row's time is rejected as issued in the future exactly when the
    /// contract says (valid when issued exactly five minutes ahead), and an envelope that is not rejected verifies.
    /// </summary>
    /// <remarks>2026-11-05, FR-MCP-103; TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): added for the sibling sweep of hostile validation round 12 finding R12-F2.</remarks>
    [Fact]
    public void EnvelopeFutureIssue_UsesScriptedClock() =>
        QBrainAi.Support.Mcp.Tests.TestSupport.AgeBoundaryContractTests.AssertDecision("D4 federation envelope future issue", (reference, recorded) =>
        {
            var sut = CreateSigner();
            _clock.Set(recorded);
            var envelope = sut.Sign(Operation(), "proxy-a", "proxy-b");
            Assert.Equal(recorded, envelope.IssuedAtUtc);
            _clock.Set(reference);
            var result = sut.Verify(envelope, "proxy-b");
            var future = !result.IsValid && (result.Error ?? string.Empty).Contains("future", StringComparison.OrdinalIgnoreCase);
            Assert.True(future || result.IsValid, result.Error);
            return future;
        });

    /// <summary>FR-MCP-103: the fixed operation request the two D4 age-boundary bindings sign.</summary>
    /// <returns>A new request with fixed ids and a JSON body of {}.</returns>
    /// <remarks>2026-11-05, FR-MCP-103; TR-MCP-USECASE-018 AC-006: this entry documents the helper, added in slice Z4 when the cases moved to the settable clock (the sibling sweep of hostile validation round 12 finding R12-F2), because the ninth holistic read before hostile validation round 13 (finding e1; MEMORY-FACT-008) found it without XML documentation. 2026-11-05, FR-MCP-103; TR-MCP-USECASE-018 AC-006: the summary names the two D4 bindings, its only callers, and the returns line the JSON body, because the tenth holistic read (finding g1) found that the first text said every case signs it and the body is empty; its proxy id is generic (finding c5).</remarks>
    private static FederationOperationRequest Operation() => new()
    {
        OperationId = "op-1",
        ProxyId = "proxy-a",
        Domain = "todo",
        BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
    };

    /// <summary>FR-MCP-103: a signer with signing enabled, a fixed secret and a 300 second envelope TTL, reading the class's settable clock.</summary>
    /// <returns>The signer under test.</returns>
    /// <remarks>2026-11-05, FR-MCP-103; TR-MCP-USECASE-018 AC-006: this entry documents the helper, changed in slice Z4 to pass the settable clock (the sibling sweep of hostile validation round 12 finding R12-F2), because the ninth holistic read before hostile validation round 13 (finding e1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private FederationEnvelopeSigner CreateSigner()
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
        return new FederationEnvelopeSigner(monitor, _clock);
    }
}
