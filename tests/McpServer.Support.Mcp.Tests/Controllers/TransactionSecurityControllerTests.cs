using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// Unit tests for keyserver and subscriber transaction-security controllers.
/// TEST-MCP-158, TEST-MCP-159, TEST-MCP-160, TEST-MCP-165.
/// </summary>
public sealed class TransactionSecurityControllerTests
{
    /// <summary>Party registration generates usable signing and encryption key descriptors.</summary>
    [Fact]
    public async Task RegisterParty_ThenGetKey_ReturnsGeneratedPublicKey()
    {
        using var services = CreateServices();
        var controller = new KeyServerController(services.KeyServer, services.KeyServer);

        var registration = await controller.RegisterPartyAsync(
            new PartyRegistrationRequest { PartyId = "publisher-1", Role = "publisher" },
            CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(registration.Result);
        var party = Assert.IsType<PartyRegistrationResponse>(ok.Value);
        var keyResult = await controller.GetPartyKeyAsync(
            party.PartyId,
            party.ActiveSigningKeyId!,
            CancellationToken.None).ConfigureAwait(true);

        var keyOk = Assert.IsType<OkObjectResult>(keyResult.Result);
        var key = Assert.IsType<PartyKeyDescriptor>(keyOk.Value);
        Assert.Equal("signing", key.Purpose);
        Assert.Contains("BEGIN PUBLIC KEY", key.PublicKeyPem, StringComparison.Ordinal);
    }

    /// <summary>Signed manifests verify successfully and return a canonical manifest hash.</summary>
    [Fact]
    public async Task SignManifest_ThenVerifyManifest_ReturnsValidManifestHash()
    {
        using var services = CreateServices();
        await RegisterStandardPartiesAsync(services.KeyServer).ConfigureAwait(true);
        var controller = new KeyServerController(services.KeyServer, services.KeyServer);

        var sign = await controller.SignManifestAsync(
            CreateSignRequest("txn-1", sequence: 1, nonce: "nonce-1"),
            CancellationToken.None).ConfigureAwait(true);

        var signOk = Assert.IsType<OkObjectResult>(sign.Result);
        var signed = Assert.IsType<TransactionManifestSignResponse>(signOk.Value);
        Assert.True(signed.Success);
        Assert.NotNull(signed.Manifest!.Signature);

        var verify = await controller.VerifyManifestAsync(
            new TransactionManifestVerifyRequest
            {
                Manifest = signed.Manifest,
                ExpectedSubscriberPartyId = "subscriber-1",
            },
            CancellationToken.None).ConfigureAwait(true);

        var verifyOk = Assert.IsType<OkObjectResult>(verify.Result);
        var verified = Assert.IsType<TransactionManifestVerifyResponse>(verifyOk.Value);
        Assert.True(verified.IsValid);
        Assert.Equal(TransactionFailureReason.None, verified.Reason);
        Assert.False(string.IsNullOrWhiteSpace(verified.ManifestHashSha256));
    }

    /// <summary>Manifest verification rejects tampered signed content.</summary>
    [Fact]
    public async Task VerifyManifest_WithTamperedHash_ReturnsBadRequest()
    {
        using var services = CreateServices();
        await RegisterStandardPartiesAsync(services.KeyServer).ConfigureAwait(true);
        var controller = new KeyServerController(services.KeyServer, services.KeyServer);
        var signed = await SignAsync(services.KeyServer, "txn-2", sequence: 2, nonce: "nonce-2").ConfigureAwait(true);
        signed.DiffgramSha256 = Sha256Hex("tampered");

        var result = await controller.VerifyManifestAsync(
            new TransactionManifestVerifyRequest { Manifest = signed, ExpectedSubscriberPartyId = "subscriber-1" },
            CancellationToken.None).ConfigureAwait(true);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<TransactionManifestVerifyResponse>(badRequest.Value);
        Assert.False(response.IsValid);
        Assert.Equal(TransactionFailureReason.ManifestSignatureMismatch, response.Reason);
    }

    /// <summary>Keyserver signing rejects disabled publisher parties with a deterministic reason.</summary>
    [Fact]
    public async Task SignManifest_WithDisabledPublisher_ReturnsDisabledParty()
    {
        using var services = CreateServices();
        await services.KeyServer.RegisterPartyAsync(
            new PartyRegistrationRequest { PartyId = "publisher-1", Role = "publisher", Status = "disabled" },
            CancellationToken.None).ConfigureAwait(true);
        await services.KeyServer.RegisterPartyAsync(
            new PartyRegistrationRequest { PartyId = "subscriber-1", Role = "subscriber" },
            CancellationToken.None).ConfigureAwait(true);

        var response = await services.KeyServer.SignManifestAsync(
            CreateSignRequest("txn-disabled-publisher", sequence: 7, nonce: "nonce-disabled-publisher"),
            CancellationToken.None).ConfigureAwait(true);

        Assert.False(response.Success);
        Assert.Equal(TransactionFailureReason.DisabledParty, response.Reason);
    }

    /// <summary>Keyserver verification rejects disabled publisher parties with a deterministic reason.</summary>
    [Fact]
    public async Task VerifyManifest_WithDisabledPublisher_ReturnsDisabledParty()
    {
        using var services = CreateServices();
        await RegisterStandardPartiesAsync(services.KeyServer).ConfigureAwait(true);
        var manifest = await SignAsync(
            services.KeyServer,
            "txn-disabled-publisher-verify",
            sequence: 8,
            nonce: "nonce-disabled-publisher-verify").ConfigureAwait(true);
        await services.KeyServer.RegisterPartyAsync(
            new PartyRegistrationRequest { PartyId = "publisher-1", Role = "publisher", Status = "disabled" },
            CancellationToken.None).ConfigureAwait(true);

        var response = await services.KeyServer.VerifyManifestAsync(
            new TransactionManifestVerifyRequest { Manifest = manifest, ExpectedSubscriberPartyId = "subscriber-1" },
            CancellationToken.None).ConfigureAwait(true);

        Assert.False(response.IsValid);
        Assert.Equal(TransactionFailureReason.DisabledParty, response.Reason);
    }

    /// <summary>Subscriber commit accepts a signed manifest and exposes committed status.</summary>
    [Fact]
    public async Task CommitDiffgram_WithValidManifest_CommitsAndStatusReturnsCommitted()
    {
        using var services = CreateServices();
        await RegisterStandardPartiesAsync(services.KeyServer).ConfigureAwait(true);
        var manifest = await SignAsync(services.KeyServer, "txn-3", sequence: 3, nonce: "nonce-3").ConfigureAwait(true);
        var controller = new SubscriberController(services.Subscriber);

        var commit = await controller.CommitDiffgramAsync(
            CreateCommitRequest(manifest),
            CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(commit.Result);
        var response = Assert.IsType<DiffgramCommitResponse>(ok.Value);
        Assert.Equal("committed", response.Status);
        Assert.Equal("diffgram-txn-3", response.DiffgramId);

        var status = await controller.GetTransactionStatusAsync("txn-3", CancellationToken.None).ConfigureAwait(true);
        var statusOk = Assert.IsType<OkObjectResult>(status.Result);
        var statusResponse = Assert.IsType<TransactionStatusResponse>(statusOk.Value);
        Assert.Equal("committed", statusResponse.Status);
    }

    /// <summary>Subscriber commit is idempotent for the same transaction and manifest payload.</summary>
    [Fact]
    public async Task CommitDiffgram_WithDuplicatePayload_ReturnsDuplicate()
    {
        using var services = CreateServices();
        await RegisterStandardPartiesAsync(services.KeyServer).ConfigureAwait(true);
        var manifest = await SignAsync(services.KeyServer, "txn-4", sequence: 4, nonce: "nonce-4").ConfigureAwait(true);
        var controller = new SubscriberController(services.Subscriber);
        await controller.CommitDiffgramAsync(CreateCommitRequest(manifest), CancellationToken.None).ConfigureAwait(true);

        var duplicate = await controller.CommitDiffgramAsync(CreateCommitRequest(manifest), CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(duplicate.Result);
        var response = Assert.IsType<DiffgramCommitResponse>(ok.Value);
        Assert.Equal("duplicate", response.Status);
        Assert.Equal(TransactionFailureReason.None, response.Reason);
    }

    /// <summary>Subscriber commit rejects encrypted body hash mismatches.</summary>
    [Fact]
    public async Task CommitDiffgram_WithEncryptedHashMismatch_ReturnsBadRequest()
    {
        using var services = CreateServices();
        await RegisterStandardPartiesAsync(services.KeyServer).ConfigureAwait(true);
        var manifest = await SignAsync(services.KeyServer, "txn-5", sequence: 5, nonce: "nonce-5").ConfigureAwait(true);
        var controller = new SubscriberController(services.Subscriber);
        var request = CreateCommitRequest(manifest);
        request.EncryptedBodySha256 = Sha256Hex("other-encrypted-body");

        var commit = await controller.CommitDiffgramAsync(request, CancellationToken.None).ConfigureAwait(true);

        var badRequest = Assert.IsType<BadRequestObjectResult>(commit.Result);
        var response = Assert.IsType<DiffgramCommitResponse>(badRequest.Value);
        Assert.Equal("rejected", response.Status);
        Assert.Equal(TransactionFailureReason.EncryptedBodyHashMismatch, response.Reason);
    }

    /// <summary>Subscriber commit rejects a tampered encrypted body even when caller hash fields copy the manifest.</summary>
    [Fact]
    public async Task CommitDiffgram_WithTamperedEncryptedBody_ReturnsBadRequest()
    {
        using var services = CreateServices();
        await RegisterStandardPartiesAsync(services.KeyServer).ConfigureAwait(true);
        var manifest = await SignAsync(services.KeyServer, "txn-5b", sequence: 6, nonce: "nonce-5b").ConfigureAwait(true);
        var controller = new SubscriberController(services.Subscriber);
        var request = CreateCommitRequest(manifest);
        request.EncryptedDiffgramBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("tampered-encrypted-diffgram"));

        var commit = await controller.CommitDiffgramAsync(request, CancellationToken.None).ConfigureAwait(true);

        var badRequest = Assert.IsType<BadRequestObjectResult>(commit.Result);
        var response = Assert.IsType<DiffgramCommitResponse>(badRequest.Value);
        Assert.Equal("rejected", response.Status);
        Assert.Equal(TransactionFailureReason.EncryptedBodyHashMismatch, response.Reason);
    }

    /// <summary>Subscriber abort stores aborted status for an uncommitted transaction.</summary>
    [Fact]
    public async Task AbortTransaction_BeforeCommit_ReturnsAbortedStatus()
    {
        using var services = CreateServices();
        var controller = new SubscriberController(services.Subscriber);

        var abort = await controller.AbortTransactionAsync(
            "txn-6",
            new TransactionAbortRequest { Reason = TransactionFailureReason.Aborted, Actor = "test" },
            CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(abort.Result);
        var response = Assert.IsType<TransactionAbortResponse>(ok.Value);
        Assert.Equal("aborted", response.Status);

        var status = await controller.GetTransactionStatusAsync("txn-6", CancellationToken.None).ConfigureAwait(true);
        var statusOk = Assert.IsType<OkObjectResult>(status.Result);
        var statusResponse = Assert.IsType<TransactionStatusResponse>(statusOk.Value);
        Assert.Equal("aborted", statusResponse.Status);
    }

    /// <summary>Subscriber commit refuses a transaction after abort and reports the abort reason.</summary>
    [Fact]
    public async Task CommitDiffgram_AfterAbort_ReturnsAbortedReason()
    {
        using var services = CreateServices();
        await RegisterStandardPartiesAsync(services.KeyServer).ConfigureAwait(true);
        var manifest = await SignAsync(services.KeyServer, "txn-6b", sequence: 7, nonce: "nonce-6b").ConfigureAwait(true);
        var controller = new SubscriberController(services.Subscriber);
        await controller.AbortTransactionAsync(
            "txn-6b",
            new TransactionAbortRequest { Reason = TransactionFailureReason.Aborted, Actor = "test" },
            CancellationToken.None).ConfigureAwait(true);

        var commit = await controller.CommitDiffgramAsync(CreateCommitRequest(manifest), CancellationToken.None).ConfigureAwait(true);

        var badRequest = Assert.IsType<BadRequestObjectResult>(commit.Result);
        var response = Assert.IsType<DiffgramCommitResponse>(badRequest.Value);
        Assert.Equal("rejected", response.Status);
        Assert.Equal(TransactionFailureReason.Aborted, response.Reason);
    }

    private static TransactionSecurityTestServices CreateServices()
    {
        var canonicalizer = new TransactionManifestCanonicalizer();
        var keyServer = new InMemoryKeyServerService(
            Monitor(new KeyServerOptions { ManifestTtlSeconds = 300, MaxClockSkewSeconds = 300 }),
            canonicalizer);
        var subscriber = new InMemorySubscriberCommitService(
            keyServer,
            canonicalizer,
            Monitor(new SubscriberOptions { PartyId = "subscriber-1" }));
        return new TransactionSecurityTestServices(keyServer, subscriber);
    }

    private static async Task RegisterStandardPartiesAsync(IKeyServerPartyRegistry registry)
    {
        await registry.RegisterPartyAsync(
            new PartyRegistrationRequest { PartyId = "publisher-1", Role = "publisher" },
            CancellationToken.None).ConfigureAwait(true);
        await registry.RegisterPartyAsync(
            new PartyRegistrationRequest { PartyId = "subscriber-1", Role = "subscriber" },
            CancellationToken.None).ConfigureAwait(true);
    }

    private static async Task<TransactionManifestDto> SignAsync(
        IKeyServerManifestService keyServer,
        string transactionId,
        long sequence,
        string nonce)
    {
        var response = await keyServer.SignManifestAsync(
            CreateSignRequest(transactionId, sequence, nonce),
            CancellationToken.None).ConfigureAwait(true);
        Assert.True(response.Success);
        return response.Manifest!;
    }

    private static TransactionManifestSignRequest CreateSignRequest(string transactionId, long sequence, string nonce)
        => new()
        {
            TransactionId = transactionId,
            TurnId = "turn-1",
            PublisherPartyId = "publisher-1",
            SubscriberPartyId = "subscriber-1",
            Sequence = sequence,
            Nonce = nonce,
            DiffgramSha256 = Sha256Hex("plain-diffgram"),
            EncryptedBodySha256 = Sha256Hex("encrypted-diffgram"),
        };

    private static DiffgramCommitRequest CreateCommitRequest(TransactionManifestDto manifest)
        => new()
        {
            Manifest = manifest,
            EncryptedDiffgramBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-diffgram")),
            EncryptedBodySha256 = manifest.EncryptedBodySha256,
            DiffgramSha256 = manifest.DiffgramSha256,
        };

    private static IOptionsMonitor<TOptions> Monitor<TOptions>(TOptions options)
        where TOptions : class
    {
        var monitor = Substitute.For<IOptionsMonitor<TOptions>>();
        monitor.CurrentValue.Returns(options);
        return monitor;
    }

    private static string Sha256Hex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record TransactionSecurityTestServices(
        InMemoryKeyServerService KeyServer,
        InMemorySubscriberCommitService Subscriber) : IDisposable
    {
        public void Dispose()
        {
            KeyServer.Dispose();
        }
    }
}
