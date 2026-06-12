using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace McpServer.TransactionSecurity.IntegrationTests;

/// <summary>
/// TEST-MCP-160: Real separate-host keyserver/subscriber integration tests derived from SD-DIFFGRAM-001.
/// </summary>
public sealed class SeparateTransactionServiceIntegrationTests
{
    /// <summary>Valid signed manifests commit through the subscriber after HTTP keyserver verification.</summary>
    [Fact]
    public async Task SeparateHosts_CommitSignedDiffgram_UsesHttpKeyserverVerification()
    {
        using var harness = CreateHarness();
        await harness.RegisterStandardPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-separate-valid", sequence: 10, nonce: "nonce-separate-valid")
            .ConfigureAwait(true);

        var commit = await harness.Subscriber.CommitDiffgramAsync(CreateCommitRequest(manifest)).ConfigureAwait(true);
        var status = await harness.Subscriber.GetTransactionStatusAsync("txn-separate-valid").ConfigureAwait(true);

        Assert.Equal("committed", commit.Status);
        Assert.Equal(TransactionFailureReason.None, commit.Reason);
        Assert.Equal("diffgram-txn-separate-valid", commit.DiffgramId);
        Assert.Equal("committed", status.Status);
    }

    /// <summary>Tampered manifests are rejected by the subscriber through the separate keyserver verification path.</summary>
    [Fact]
    public async Task SeparateHosts_RejectTamperedManifestThroughKeyserverHttpVerification()
    {
        using var harness = CreateHarness();
        await harness.RegisterStandardPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-separate-tampered", sequence: 11, nonce: "nonce-separate-tampered")
            .ConfigureAwait(true);
        manifest.DiffgramSha256 = Sha256Hex("tampered-diffgram");

        var response = await harness.SubscriberHttp.PostAsJsonAsync(
            "mcpserver/subscriber/diffgrams/commit",
            CreateCommitRequest(manifest)).ConfigureAwait(true);
        var body = await response.Content.ReadFromJsonAsync<DiffgramCommitResponse>().ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("rejected", body.Status);
        Assert.Equal(TransactionFailureReason.ManifestSignatureMismatch, body.Reason);
    }

    /// <summary>Stale publisher/subscriber sequences are rejected across separate hosts after a previous valid commit.</summary>
    [Fact]
    public async Task SeparateHosts_RejectStaleSequenceAfterPriorCommit()
    {
        using var harness = CreateHarness();
        await harness.RegisterStandardPartiesAsync().ConfigureAwait(true);
        var stale = await harness.SignManifestAsync("txn-separate-sequence-1", sequence: 12, nonce: "nonce-separate-sequence-1")
            .ConfigureAwait(true);
        var first = await harness.SignManifestAsync("txn-separate-sequence-2", sequence: 13, nonce: "nonce-separate-sequence-2")
            .ConfigureAwait(true);
        await harness.Subscriber.CommitDiffgramAsync(CreateCommitRequest(first)).ConfigureAwait(true);

        var response = await harness.SubscriberHttp.PostAsJsonAsync(
            "mcpserver/subscriber/diffgrams/commit",
            CreateCommitRequest(stale)).ConfigureAwait(true);
        var body = await response.Content.ReadFromJsonAsync<DiffgramCommitResponse>().ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("rejected", body.Status);
        Assert.Equal(TransactionFailureReason.StaleSequence, body.Reason);
    }

    /// <summary>Separate keyserver host rejects reused manifest signing nonces.</summary>
    [Fact]
    public async Task SeparateKeyServer_RejectsReplayNonceOnSign()
    {
        using var harness = CreateHarness();
        await harness.RegisterStandardPartiesAsync().ConfigureAwait(true);
        await harness.SignManifestAsync("txn-separate-sign-replay-1", sequence: 20, nonce: "nonce-separate-sign-replay")
            .ConfigureAwait(true);

        var response = await harness.KeyServerHttp.PostAsJsonAsync(
            "mcpserver/keyserver/manifests/sign",
            CreateSignRequest("txn-separate-sign-replay-2", sequence: 21, nonce: "nonce-separate-sign-replay"))
            .ConfigureAwait(true);
        var body = await response.Content.ReadFromJsonAsync<TransactionManifestSignResponse>().ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal(TransactionFailureReason.ReplayNonce, body.Reason);
    }

    /// <summary>Separate keyserver host rejects non-monotonic manifest signing sequences.</summary>
    [Fact]
    public async Task SeparateKeyServer_RejectsStaleSequenceOnSign()
    {
        using var harness = CreateHarness();
        await harness.RegisterStandardPartiesAsync().ConfigureAwait(true);
        await harness.SignManifestAsync("txn-separate-sign-stale-1", sequence: 30, nonce: "nonce-separate-sign-stale-1")
            .ConfigureAwait(true);

        var response = await harness.KeyServerHttp.PostAsJsonAsync(
            "mcpserver/keyserver/manifests/sign",
            CreateSignRequest("txn-separate-sign-stale-2", sequence: 29, nonce: "nonce-separate-sign-stale-2"))
            .ConfigureAwait(true);
        var body = await response.Content.ReadFromJsonAsync<TransactionManifestSignResponse>().ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal(TransactionFailureReason.StaleSequence, body.Reason);
    }

    /// <summary>Encrypted body mismatches are rejected by the separate subscriber host.</summary>
    [Fact]
    public async Task SeparateHosts_RejectEncryptedBodyMismatch()
    {
        using var harness = CreateHarness();
        await harness.RegisterStandardPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-separate-hash", sequence: 13, nonce: "nonce-separate-hash")
            .ConfigureAwait(true);
        var request = CreateCommitRequest(manifest);
        request.EncryptedDiffgramBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("different-encrypted-body"));

        var response = await harness.SubscriberHttp.PostAsJsonAsync(
            "mcpserver/subscriber/diffgrams/commit",
            request).ConfigureAwait(true);
        var body = await response.Content.ReadFromJsonAsync<DiffgramCommitResponse>().ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("rejected", body.Status);
        Assert.Equal(TransactionFailureReason.EncryptedBodyHashMismatch, body.Reason);
    }

    private static SeparateHostHarness CreateHarness()
    {
        var keyServerFactory = new WebApplicationFactory<KeyServerEntryPoint>();
        var keyServerHttp = keyServerFactory.CreateClient();
        var subscriberFactory = new WebApplicationFactory<SubscriberEntryPoint>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IKeyServerManifestService>();
                    services.AddSingleton<IKeyServerManifestService>(_ => new HttpKeyServerManifestService(keyServerHttp));
                });
            });
        var subscriberHttp = subscriberFactory.CreateClient();
        return new SeparateHostHarness(
            keyServerFactory,
            subscriberFactory,
            keyServerHttp,
            subscriberHttp,
            new KeyServerClient(keyServerHttp, CreateOptions(keyServerHttp)),
            new SubscriberClient(subscriberHttp, CreateOptions(subscriberHttp)));
    }

    private static McpServerClientOptions CreateOptions(HttpClient http)
        => new()
        {
            ApiKey = "separate-host-test",
            BaseUrl = http.BaseAddress ?? new Uri("http://localhost"),
        };

    private static TransactionManifestSignRequest CreateSignRequest(string transactionId, long sequence, string nonce)
        => new()
        {
            TransactionId = transactionId,
            TurnId = "turn-separate-host",
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

    private static string Sha256Hex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record SeparateHostHarness(
        WebApplicationFactory<KeyServerEntryPoint> KeyServerFactory,
        WebApplicationFactory<SubscriberEntryPoint> SubscriberFactory,
        HttpClient KeyServerHttp,
        HttpClient SubscriberHttp,
        KeyServerClient KeyServer,
        SubscriberClient Subscriber) : IDisposable
    {
        public async Task RegisterStandardPartiesAsync()
        {
            await KeyServer.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = "publisher-1", Role = "publisher" })
                .ConfigureAwait(true);
            await KeyServer.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = "subscriber-1", Role = "subscriber" })
                .ConfigureAwait(true);
        }

        public async Task<TransactionManifestDto> SignManifestAsync(string transactionId, long sequence, string nonce)
        {
            var response = await KeyServer.SignManifestAsync(CreateSignRequest(transactionId, sequence, nonce))
                .ConfigureAwait(true);
            Assert.True(response.Success);
            Assert.NotNull(response.Manifest);
            return response.Manifest;
        }

        public void Dispose()
        {
            SubscriberHttp.Dispose();
            SubscriberFactory.Dispose();
            KeyServerHttp.Dispose();
            KeyServerFactory.Dispose();
        }
    }
}
