using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ClientPartyKeyDescriptor = McpServer.Client.Models.PartyKeyDescriptor;
using SecurityPartyKeyDescriptor = McpServer.TransactionSecurity.Models.PartyKeyDescriptor;

namespace McpServer.TransactionSecurity.IntegrationTests;

/// <summary>
/// TEST-MCP-160: Real separate-host keyserver/subscriber integration tests derived from SD-DIFFGRAM-001.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SeparateTransactionServiceIntegrationTests
{
    private const string PublisherPartyId = "publisher-1";
    private const string SubscriberPartyId = "subscriber-1";
    private const string ExternalPublisherSigningKeyId = "publisher-1:signing:external";
    private const string SubscriberEncryptionKeyId = "subscriber-1:encryption:1";
    private const string RotatedSubscriberEncryptionKeyId = "subscriber-1:encryption:2";

    /// <summary>Valid signed manifests commit through the subscriber after HTTP keyserver verification.</summary>
    [Fact]
    public async Task SeparateHosts_CommitSignedDiffgram_UsesHttpKeyserverVerification()
    {
        using var harness = CreateHarness();
        await harness.RegisterStandardPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-separate-valid", sequence: 10, nonce: "nonce-separate-valid")
            .ConfigureAwait(true);

        var commit = await harness.Subscriber.CommitDiffgramAsync(CreateCommitRequest(manifest), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var status = await harness.Subscriber.GetTransactionStatusAsync("txn-separate-valid", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
            CreateCommitRequest(manifest), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var body = await response.Content.ReadFromJsonAsync<DiffgramCommitResponse>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
        await harness.Subscriber.CommitDiffgramAsync(CreateCommitRequest(first), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = await harness.SubscriberHttp.PostAsJsonAsync(
            "mcpserver/subscriber/diffgrams/commit",
            CreateCommitRequest(stale), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var body = await response.Content.ReadFromJsonAsync<DiffgramCommitResponse>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
            CreateSignRequest("txn-separate-sign-replay-2", sequence: 21, nonce: "nonce-separate-sign-replay"), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var body = await response.Content.ReadFromJsonAsync<TransactionManifestSignResponse>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
            CreateSignRequest("txn-separate-sign-stale-2", sequence: 29, nonce: "nonce-separate-sign-stale-2"), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var body = await response.Content.ReadFromJsonAsync<TransactionManifestSignResponse>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal(TransactionFailureReason.StaleSequence, body.Reason);
    }

    /// <summary>Separate keyserver host reports filtered signed manifest trace records.</summary>
    [Fact]
    public async Task SeparateKeyServer_ManifestReport_ReturnsFilteredSignedTraceRecords()
    {
        using var harness = CreateHarness();
        await harness.RegisterStandardPartiesAsync().ConfigureAwait(true);
        await harness.SignManifestAsync("txn-separate-report-1", sequence: 31, nonce: "nonce-separate-report-1")
            .ConfigureAwait(true);
        await harness.SignManifestAsync("txn-separate-report-2", sequence: 32, nonce: "nonce-separate-report-2")
            .ConfigureAwait(true);

        var report = await harness.KeyServerHttp.GetFromJsonAsync<TransactionManifestTraceReport>(
            "mcpserver/keyserver/manifests/report?publisherPartyId=publisher-1&subscriberPartyId=subscriber-1&status=signed&limit=1", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.NotNull(report);
        var trace = Assert.Single(report.Records);
        Assert.Equal(PublisherPartyId, report.PublisherPartyId);
        Assert.Equal(SubscriberPartyId, report.SubscriberPartyId);
        Assert.Equal("signed", report.Status);
        Assert.Equal(1, report.Limit);
        Assert.Equal(2, report.TotalCount);
        Assert.Equal(1, report.ReturnedCount);
        Assert.Equal("txn-separate-report-1", trace.TransactionId);
        Assert.Equal("signed", trace.Status);
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
            request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var body = await response.Content.ReadFromJsonAsync<DiffgramCommitResponse>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("rejected", body.Status);
        Assert.Equal(TransactionFailureReason.EncryptedBodyHashMismatch, body.Reason);
    }

    /// <summary>Separate subscriber host binds an encryption key ring from configuration and commits old plus rotated protected envelopes.</summary>
    [Fact]
    public async Task SeparateHosts_WithSubscriberEncryptionKeyRingConfiguration_CommitsOldAndRotatedProtectedEnvelopes()
    {
        using var firstKeyPair = EncryptionKeyPair.Create();
        using var rotatedKeyPair = EncryptionKeyPair.Create();
        using var harness = CreateHarness(subscriberConfiguration: new Dictionary<string, string?>
        {
            ["Mcp:Subscriber:PartyId"] = SubscriberPartyId,
            ["Mcp:Subscriber:RequireEncryptedDiffgrams"] = "true",
            ["Mcp:Subscriber:EncryptionKeys:0:KeyId"] = SubscriberEncryptionKeyId,
            ["Mcp:Subscriber:EncryptionKeys:0:PrivateKeyPem"] = firstKeyPair.PrivateKeyPem,
            ["Mcp:Subscriber:EncryptionKeys:1:KeyId"] = RotatedSubscriberEncryptionKeyId,
            ["Mcp:Subscriber:EncryptionKeys:1:PrivateKeyPem"] = rotatedKeyPair.PrivateKeyPem,
        });
        var protector = new TransactionDiffgramProtector();
        await harness.RegisterStandardPartiesAsync(firstKeyPair, SubscriberEncryptionKeyId).ConfigureAwait(true);
        var firstEncryptionKey = await harness.KeyServer.GetPartyKeyAsync(SubscriberPartyId, SubscriberEncryptionKeyId, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(firstEncryptionKey);
        var firstProtectedDiffgram = protector.Protect(
            CreatePlaintextDiffgram("txn-separate-key-ring-first"),
            ToSecurityPartyKey(firstEncryptionKey));
        var firstManifest = await harness.SignManifestAsync(
            "txn-separate-key-ring-first",
            sequence: 40,
            nonce: "nonce-separate-key-ring-first",
            firstProtectedDiffgram.PlaintextSha256,
            firstProtectedDiffgram.EncryptedBodySha256,
            SubscriberEncryptionKeyId).ConfigureAwait(true);

        await harness.RegisterSubscriberAsync(rotatedKeyPair, RotatedSubscriberEncryptionKeyId).ConfigureAwait(true);
        var rotatedEncryptionKey = await harness.KeyServer.GetPartyKeyAsync(SubscriberPartyId, RotatedSubscriberEncryptionKeyId, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(rotatedEncryptionKey);
        var rotatedProtectedDiffgram = protector.Protect(
            CreatePlaintextDiffgram("txn-separate-key-ring-rotated"),
            ToSecurityPartyKey(rotatedEncryptionKey));
        var rotatedManifest = await harness.SignManifestAsync(
            "txn-separate-key-ring-rotated",
            sequence: 41,
            nonce: "nonce-separate-key-ring-rotated",
            rotatedProtectedDiffgram.PlaintextSha256,
            rotatedProtectedDiffgram.EncryptedBodySha256,
            RotatedSubscriberEncryptionKeyId).ConfigureAwait(true);

        var firstCommit = await harness.Subscriber.CommitDiffgramAsync(CreateCommitRequest(firstManifest, firstProtectedDiffgram), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var rotatedCommit = await harness.Subscriber.CommitDiffgramAsync(CreateCommitRequest(rotatedManifest, rotatedProtectedDiffgram), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal("committed", firstCommit.Status);
        Assert.Equal(TransactionFailureReason.None, firstCommit.Reason);
        Assert.Equal("committed", rotatedCommit.Status);
        Assert.Equal(TransactionFailureReason.None, rotatedCommit.Reason);
    }

    /// <summary>Separate hosts can provision production key material from PEM file paths without manual registration.</summary>
    [Fact]
    public async Task SeparateHosts_WithProvisionedKeyFiles_CommitsProtectedEnvelopeWithoutManualRegistration()
    {
        using var workspace = TempWorkspace.Create();
        using var publisherSigningKey = SigningKeyPair.Create();
        using var subscriberEncryptionKey = EncryptionKeyPair.Create();
        var publisherSigningPrivatePath = workspace.GetPath("publisher-signing-private.pem");
        var subscriberEncryptionPublicPath = workspace.GetPath("subscriber-encryption-public.pem");
        var subscriberEncryptionPrivatePath = workspace.GetPath("subscriber-encryption-private.pem");
        await File.WriteAllTextAsync(publisherSigningPrivatePath, publisherSigningKey.PrivateKeyPem, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await File.WriteAllTextAsync(subscriberEncryptionPublicPath, subscriberEncryptionKey.PublicKeyPem, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await File.WriteAllTextAsync(subscriberEncryptionPrivatePath, subscriberEncryptionKey.PrivateKeyPem, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        using var harness = CreateHarness(
            keyServerConfiguration: new Dictionary<string, string?>
            {
                ["Mcp:KeyServer:ProvisionedParties:0:PartyId"] = PublisherPartyId,
                ["Mcp:KeyServer:ProvisionedParties:0:Role"] = "publisher",
                ["Mcp:KeyServer:ProvisionedParties:0:ActiveSigningKeyId"] = ExternalPublisherSigningKeyId,
                ["Mcp:KeyServer:ProvisionedParties:0:SigningPrivateKeyPemFile"] = publisherSigningPrivatePath,
                ["Mcp:KeyServer:ProvisionedParties:1:PartyId"] = SubscriberPartyId,
                ["Mcp:KeyServer:ProvisionedParties:1:Role"] = "subscriber",
                ["Mcp:KeyServer:ProvisionedParties:1:ActiveEncryptionKeyId"] = SubscriberEncryptionKeyId,
                ["Mcp:KeyServer:ProvisionedParties:1:EncryptionPublicKeyPemFile"] = subscriberEncryptionPublicPath,
            },
            subscriberConfiguration: new Dictionary<string, string?>
            {
                ["Mcp:Subscriber:PartyId"] = SubscriberPartyId,
                ["Mcp:Subscriber:RequireEncryptedDiffgrams"] = "true",
                ["Mcp:Subscriber:EncryptionKeys:0:KeyId"] = SubscriberEncryptionKeyId,
                ["Mcp:Subscriber:EncryptionKeys:0:PrivateKeyPemFile"] = subscriberEncryptionPrivatePath,
            });
        var protector = new TransactionDiffgramProtector();
        var configuredEncryptionKey = await harness.KeyServer.GetPartyKeyAsync(SubscriberPartyId, SubscriberEncryptionKeyId, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(configuredEncryptionKey);
        var protectedDiffgram = protector.Protect(
            CreatePlaintextDiffgram("txn-separate-provisioned-files"),
            ToSecurityPartyKey(configuredEncryptionKey));
        var manifest = await harness.SignManifestAsync(
            "txn-separate-provisioned-files",
            sequence: 42,
            nonce: "nonce-separate-provisioned-files",
            protectedDiffgram.PlaintextSha256,
            protectedDiffgram.EncryptedBodySha256,
            SubscriberEncryptionKeyId,
            ExternalPublisherSigningKeyId).ConfigureAwait(true);

        var commit = await harness.Subscriber.CommitDiffgramAsync(CreateCommitRequest(manifest, protectedDiffgram), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal("committed", commit.Status);
        Assert.Equal(TransactionFailureReason.None, commit.Reason);
        Assert.Equal(ExternalPublisherSigningKeyId, manifest.PublisherSigningKeyId);
        Assert.DoesNotContain("PRIVATE KEY", JsonSerializer.Serialize(configuredEncryptionKey), StringComparison.Ordinal);
    }

    /// <summary>Turn coordinator can publish commit delivery through HTTP pub-sub to a separate subscriber host.</summary>
    [Fact]
    public async Task SeparateSubscriberHost_TurnCoordinatorWithHttpPubSub_CommitsThroughExternalSubscriberHost()
    {
        var canonicalizer = new TransactionManifestCanonicalizer();
        using var keyServer = new InMemoryKeyServerService(
            new FixedOptionsMonitor<KeyServerOptions>(new KeyServerOptions()),
            canonicalizer);
        await keyServer.RegisterPartyAsync(new McpServer.TransactionSecurity.Models.PartyRegistrationRequest
        {
            PartyId = PublisherPartyId,
            Role = "publisher",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await keyServer.RegisterPartyAsync(new McpServer.TransactionSecurity.Models.PartyRegistrationRequest
        {
            PartyId = SubscriberPartyId,
            Role = "subscriber",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var subscriberFactory = new WebApplicationFactory<SubscriberEntryPoint>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IKeyServerManifestService>();
                    services.AddSingleton<IKeyServerManifestService>(keyServer);
                });
            });
        using var subscriberHttp = subscriberFactory.CreateClient();
        var options = new TurnTransactionOptions
        {
            Enabled = true,
            PubSubTransport = TransactionPubSubTransport.Http,
            SubscriberBaseUrl = subscriberHttp.BaseAddress?.ToString() ?? "http://localhost",
        };
        var coordinator = new TurnTransactionCoordinator(
            new FixedOptionsMonitor<TurnTransactionOptions>(options),
            keyServer,
            keyServer,
            new HttpSubscriberTransactionPubSub(subscriberHttp),
            new JsonDiffgramBuilder(),
            new TransactionDegradedModePolicy(new FixedOptionsMonitor<TurnTransactionOptions>(options)),
            new InMemoryTransactionAuditWriter());

        var result = await coordinator.ExecuteAsync(
            new McpServer.TransactionSecurity.Models.TurnTransactionRequest
            {
                TransactionId = "txn-http-pubsub-host",
                TurnId = "turn-http-pubsub-host",
                OperationName = "todo.update",
                OperationBodyJson = "{\"id\":\"PLAN-TURNTRANSACTIONS-001\"}",
                PublisherPartyId = PublisherPartyId,
                SubscriberPartyId = SubscriberPartyId,
                Sequence = 70,
                Mutating = true,
            },
            _ => Task.FromResult(new McpServer.TransactionSecurity.Models.TurnMutationResult
            {
                Success = true,
                ResultJson = "{\"updated\":true}",
            }),
            CancellationToken.None).ConfigureAwait(true);
        var status = await subscriberHttp
            .GetFromJsonAsync<McpServer.TransactionSecurity.Models.TransactionStatusResponse>(
                "mcpserver/subscriber/transactions/txn-http-pubsub-host/status", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.False(result.Degraded);
        Assert.NotNull(status);
        Assert.Equal("committed", status.Status);
    }

    /// <summary>HTTP pub-sub coordinator execution does not return committed before the subscriber host acknowledges commit.</summary>
    [Fact]
    public async Task SeparateSubscriberHost_TurnCoordinatorWithHttpPubSub_WaitsForSubscriberAcknowledgementBeforeReturningCommitted()
    {
        var canonicalizer = new TransactionManifestCanonicalizer();
        using var keyServer = new InMemoryKeyServerService(
            new FixedOptionsMonitor<KeyServerOptions>(new KeyServerOptions()),
            canonicalizer);
        await keyServer.RegisterPartyAsync(new McpServer.TransactionSecurity.Models.PartyRegistrationRequest
        {
            PartyId = PublisherPartyId,
            Role = "publisher",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await keyServer.RegisterPartyAsync(new McpServer.TransactionSecurity.Models.PartyRegistrationRequest
        {
            PartyId = SubscriberPartyId,
            Role = "subscriber",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var blockingSubscriber = new BlockingSubscriberCommitService();
        using var subscriberFactory = new WebApplicationFactory<SubscriberEntryPoint>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ISubscriberCommitService>();
                    services.AddSingleton<ISubscriberCommitService>(blockingSubscriber);
                });
            });
        using var subscriberHttp = subscriberFactory.CreateClient();
        var options = new TurnTransactionOptions
        {
            Enabled = true,
            PubSubTransport = TransactionPubSubTransport.Http,
            SubscriberBaseUrl = subscriberHttp.BaseAddress?.ToString() ?? "http://localhost",
        };
        var coordinator = new TurnTransactionCoordinator(
            new FixedOptionsMonitor<TurnTransactionOptions>(options),
            keyServer,
            keyServer,
            new HttpSubscriberTransactionPubSub(subscriberHttp),
            new JsonDiffgramBuilder(),
            new TransactionDegradedModePolicy(new FixedOptionsMonitor<TurnTransactionOptions>(options)),
            new InMemoryTransactionAuditWriter());
        var mutationReturned = false;

        var execution = coordinator.ExecuteAsync(
            new McpServer.TransactionSecurity.Models.TurnTransactionRequest
            {
                TransactionId = "txn-http-pubsub-blocking-ack",
                TurnId = "turn-http-pubsub-blocking-ack",
                OperationName = "todo.update",
                OperationBodyJson = "{\"id\":\"PLAN-TURNTRANSACTIONS-001\"}",
                PublisherPartyId = PublisherPartyId,
                SubscriberPartyId = SubscriberPartyId,
                Sequence = 71,
                Mutating = true,
            },
            _ =>
            {
                mutationReturned = true;
                return Task.FromResult(new McpServer.TransactionSecurity.Models.TurnMutationResult
                {
                    Success = true,
                    ResultJson = "{\"updated\":true}",
                });
            },
            CancellationToken.None);
        var commitRequest = await blockingSubscriber.WaitForCommitAsync().ConfigureAwait(true);

        Assert.True(mutationReturned);
        Assert.Equal("txn-http-pubsub-blocking-ack", commitRequest.Manifest.TransactionId);
        Assert.False(execution.IsCompleted);

        blockingSubscriber.ReleaseCommit(new McpServer.TransactionSecurity.Models.DiffgramCommitResponse
        {
            TransactionId = commitRequest.Manifest.TransactionId,
            Status = "committed",
            Reason = McpServer.TransactionSecurity.Models.TransactionFailureReason.None,
            DiffgramId = $"diffgram-{commitRequest.Manifest.TransactionId}",
            CommittedAtUtc = DateTimeOffset.UtcNow,
        });
        var result = await execution.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.False(result.Degraded);
        Assert.Equal("diffgram-txn-http-pubsub-blocking-ack", result.DiffgramId);
    }

    /// <summary>HTTP pub-sub degraded subscriber rejection invokes coordinator rollback compensation while preserving audit.</summary>
    [Fact]
    public async Task SeparateSubscriberHost_TurnCoordinatorWithHttpPubSub_RollsBackMutationWhenSubscriberRejects()
    {
        var canonicalizer = new TransactionManifestCanonicalizer();
        using var keyServer = new InMemoryKeyServerService(
            new FixedOptionsMonitor<KeyServerOptions>(new KeyServerOptions()),
            canonicalizer);
        await keyServer.RegisterPartyAsync(new McpServer.TransactionSecurity.Models.PartyRegistrationRequest
        {
            PartyId = PublisherPartyId,
            Role = "publisher",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await keyServer.RegisterPartyAsync(new McpServer.TransactionSecurity.Models.PartyRegistrationRequest
        {
            PartyId = SubscriberPartyId,
            Role = "subscriber",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var subscriberFactory = new WebApplicationFactory<SubscriberEntryPoint>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ISubscriberCommitService>();
                    services.AddSingleton<ISubscriberCommitService>(new UnavailableSubscriberCommitService());
                });
            });
        using var subscriberHttp = subscriberFactory.CreateClient();
        var options = new TurnTransactionOptions
        {
            Enabled = true,
            DegradedModeEnabled = true,
            PubSubTransport = TransactionPubSubTransport.Http,
            SubscriberBaseUrl = subscriberHttp.BaseAddress?.ToString() ?? "http://localhost",
        };
        var audit = new InMemoryTransactionAuditWriter();
        var coordinator = new TurnTransactionCoordinator(
            new FixedOptionsMonitor<TurnTransactionOptions>(options),
            keyServer,
            keyServer,
            new HttpSubscriberTransactionPubSub(subscriberHttp),
            new JsonDiffgramBuilder(),
            new TransactionDegradedModePolicy(new FixedOptionsMonitor<TurnTransactionOptions>(options)),
            audit);
        var appliedValues = new List<string>();

        var result = await coordinator.ExecuteAsync(
            new McpServer.TransactionSecurity.Models.TurnTransactionRequest
            {
                TransactionId = "txn-http-pubsub-rollback",
                TurnId = "turn-http-pubsub-rollback",
                OperationName = "todo.update",
                OperationBodyJson = "{\"id\":\"PLAN-TURNTRANSACTIONS-001\"}",
                PublisherPartyId = PublisherPartyId,
                SubscriberPartyId = SubscriberPartyId,
                Sequence = 72,
                Mutating = true,
            },
            _ =>
            {
                appliedValues.Add("mutated");
                return Task.FromResult(new McpServer.TransactionSecurity.Models.TurnMutationResult
                {
                    Success = true,
                    ResultJson = "{\"updated\":true}",
                    RollbackAsync = ct =>
                    {
                        appliedValues.Clear();
                        return Task.CompletedTask;
                    },
                });
            },
            CancellationToken.None).ConfigureAwait(true);
        var auditEvents = audit.Snapshot();

        Assert.Equal("degraded", result.Status);
        Assert.True(result.Degraded);
        Assert.True(result.RollbackAttempted);
        Assert.True(result.RollbackSucceeded);
        Assert.Empty(appliedValues);
        Assert.Equal(McpServer.TransactionSecurity.Models.TransactionFailureReason.SubscriberUnavailable, result.Reason);
        Assert.Contains(auditEvents, item => item.EventName == "transaction_rollback_completed");
        Assert.Contains(auditEvents, item => item.EventName == "transaction_degraded");
    }

    private static SeparateHostHarness CreateHarness(
        IReadOnlyDictionary<string, string?>? keyServerConfiguration = null,
        IReadOnlyDictionary<string, string?>? subscriberConfiguration = null)
    {
        var keyServerFactory = new WebApplicationFactory<KeyServerEntryPoint>()
            .WithWebHostBuilder(builder =>
            {
                if (keyServerConfiguration is not null)
                {
                    builder.ConfigureAppConfiguration((_, configuration) =>
                        configuration.AddInMemoryCollection(keyServerConfiguration));
                }
            });
        var keyServerHttp = keyServerFactory.CreateClient();
        var subscriberFactory = new WebApplicationFactory<SubscriberEntryPoint>()
            .WithWebHostBuilder(builder =>
            {
                if (subscriberConfiguration is not null)
                {
                    builder.ConfigureAppConfiguration((_, configuration) =>
                        configuration.AddInMemoryCollection(subscriberConfiguration));
                }

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
            PublisherPartyId = PublisherPartyId,
            SubscriberPartyId = SubscriberPartyId,
            Sequence = sequence,
            Nonce = nonce,
            DiffgramSha256 = Sha256Hex("plain-diffgram"),
            EncryptedBodySha256 = Sha256Hex("encrypted-diffgram"),
        };

    private static TransactionManifestSignRequest CreateSignRequest(
        string transactionId,
        long sequence,
        string nonce,
        string diffgramSha256,
        string encryptedBodySha256,
        string subscriberEncryptionKeyId,
        string? publisherSigningKeyId = null)
        => new()
        {
            TransactionId = transactionId,
            TurnId = "turn-separate-host",
            PublisherPartyId = PublisherPartyId,
            PublisherSigningKeyId = publisherSigningKeyId,
            SubscriberPartyId = SubscriberPartyId,
            SubscriberEncryptionKeyId = subscriberEncryptionKeyId,
            Sequence = sequence,
            Nonce = nonce,
            DiffgramSha256 = diffgramSha256,
            EncryptedBodySha256 = encryptedBodySha256,
        };

    private static DiffgramCommitRequest CreateCommitRequest(TransactionManifestDto manifest)
        => new()
        {
            Manifest = manifest,
            EncryptedDiffgramBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-diffgram")),
            EncryptedBodySha256 = manifest.EncryptedBodySha256,
            DiffgramSha256 = manifest.DiffgramSha256,
        };

    private static DiffgramCommitRequest CreateCommitRequest(
        TransactionManifestDto manifest,
        DiffgramProtectionResult protectedDiffgram)
        => new()
        {
            Manifest = manifest,
            EncryptedDiffgramBase64 = protectedDiffgram.EncryptedDiffgramBase64,
            EncryptedBodySha256 = protectedDiffgram.EncryptedBodySha256,
            DiffgramSha256 = protectedDiffgram.PlaintextSha256,
        };

    private static string CreatePlaintextDiffgram(string transactionId)
        => $$"""{"transactionId":"{{transactionId}}","operation":"todo.update","value":42}""";

    private static string Sha256Hex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static SecurityPartyKeyDescriptor ToSecurityPartyKey(ClientPartyKeyDescriptor descriptor)
        => new()
        {
            PartyId = descriptor.PartyId,
            KeyId = descriptor.KeyId,
            Purpose = descriptor.Purpose,
            Algorithm = descriptor.Algorithm,
            PublicKeyPem = descriptor.PublicKeyPem,
            Status = descriptor.Status,
            CreatedAtUtc = descriptor.CreatedAtUtc,
            ExpiresAtUtc = descriptor.ExpiresAtUtc,
        };

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
            await RegisterStandardPartiesAsync(null, null).ConfigureAwait(true);
        }

        public async Task RegisterStandardPartiesAsync(
            EncryptionKeyPair? subscriberKeyPair,
            string? subscriberEncryptionKeyId)
        {
            await KeyServer.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = PublisherPartyId, Role = "publisher" })
                .ConfigureAwait(true);
            var subscriber = new PartyRegistrationRequest { PartyId = SubscriberPartyId, Role = "subscriber" };
            if (subscriberKeyPair is not null)
            {
                subscriber.ActiveEncryptionKeyId = subscriberEncryptionKeyId ?? SubscriberEncryptionKeyId;
                subscriber.EncryptionPublicKeyPem = subscriberKeyPair.PublicKeyPem;
            }

            await KeyServer.RegisterPartyAsync(subscriber)
                .ConfigureAwait(true);
        }

        public Task<PartyRegistrationResponse> RegisterSubscriberAsync(
            EncryptionKeyPair subscriberKeyPair,
            string subscriberEncryptionKeyId)
            => KeyServer.RegisterPartyAsync(new PartyRegistrationRequest
            {
                PartyId = SubscriberPartyId,
                Role = "subscriber",
                ActiveEncryptionKeyId = subscriberEncryptionKeyId,
                EncryptionPublicKeyPem = subscriberKeyPair.PublicKeyPem,
            });

        public async Task<TransactionManifestDto> SignManifestAsync(string transactionId, long sequence, string nonce)
        {
            var response = await KeyServer.SignManifestAsync(CreateSignRequest(transactionId, sequence, nonce))
                .ConfigureAwait(true);
            Assert.True(response.Success);
            Assert.NotNull(response.Manifest);
            return response.Manifest;
        }

        public async Task<TransactionManifestDto> SignManifestAsync(
            string transactionId,
            long sequence,
            string nonce,
            string diffgramSha256,
            string encryptedBodySha256,
            string subscriberEncryptionKeyId,
            string? publisherSigningKeyId = null)
        {
            var response = await KeyServer.SignManifestAsync(CreateSignRequest(
                    transactionId,
                    sequence,
                    nonce,
                    diffgramSha256,
                    encryptedBodySha256,
                    subscriberEncryptionKeyId,
                    publisherSigningKeyId))
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

    private sealed class EncryptionKeyPair : IDisposable
    {
        private readonly ECDiffieHellman _key;

        private EncryptionKeyPair(ECDiffieHellman key)
        {
            _key = key;
            PublicKeyPem = key.ExportSubjectPublicKeyInfoPem();
            PrivateKeyPem = key.ExportPkcs8PrivateKeyPem();
        }

        public string PublicKeyPem { get; }

        public string PrivateKeyPem { get; }

        public static EncryptionKeyPair Create()
            => new(ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256));

        public void Dispose()
            => _key.Dispose();
    }

    private sealed class SigningKeyPair : IDisposable
    {
        private readonly ECDsa _key;

        private SigningKeyPair(ECDsa key)
        {
            _key = key;
            PublicKeyPem = key.ExportSubjectPublicKeyInfoPem();
            PrivateKeyPem = key.ExportPkcs8PrivateKeyPem();
        }

        public string PublicKeyPem { get; }

        public string PrivateKeyPem { get; }

        public static SigningKeyPair Create()
            => new(ECDsa.Create(ECCurve.NamedCurves.nistP256));

        public void Dispose()
            => _key.Dispose();
    }

    private sealed class BlockingSubscriberCommitService : ISubscriberCommitService
    {
        private readonly TaskCompletionSource<McpServer.TransactionSecurity.Models.DiffgramCommitRequest> _commitStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<McpServer.TransactionSecurity.Models.DiffgramCommitResponse> _allowCommit =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<McpServer.TransactionSecurity.Models.DiffgramCommitResponse> CommitDiffgramAsync(
            McpServer.TransactionSecurity.Models.DiffgramCommitRequest request,
            CancellationToken cancellationToken = default)
        {
            _commitStarted.TrySetResult(request);
            return await _allowCommit.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task<McpServer.TransactionSecurity.Models.TransactionStatusResponse?> GetTransactionStatusAsync(
            string transactionId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<McpServer.TransactionSecurity.Models.TransactionStatusResponse?>(null);

        public Task<McpServer.TransactionSecurity.Models.TransactionAbortResponse> AbortTransactionAsync(
            string transactionId,
            McpServer.TransactionSecurity.Models.TransactionAbortRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new McpServer.TransactionSecurity.Models.TransactionAbortResponse
            {
                TransactionId = transactionId,
                Status = "aborted",
                Reason = request.Reason,
                AbortedAtUtc = DateTimeOffset.UtcNow,
            });

        public async Task<McpServer.TransactionSecurity.Models.DiffgramCommitRequest> WaitForCommitAsync()
        {
            return await _commitStarted.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }

        public void ReleaseCommit(McpServer.TransactionSecurity.Models.DiffgramCommitResponse response)
            => _allowCommit.TrySetResult(response);
    }

    private sealed class UnavailableSubscriberCommitService : ISubscriberCommitService
    {
        public Task<McpServer.TransactionSecurity.Models.DiffgramCommitResponse> CommitDiffgramAsync(
            McpServer.TransactionSecurity.Models.DiffgramCommitRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new McpServer.TransactionSecurity.Models.DiffgramCommitResponse
            {
                TransactionId = request.Manifest.TransactionId,
                Status = "rejected",
                Reason = McpServer.TransactionSecurity.Models.TransactionFailureReason.SubscriberUnavailable,
            });

        public Task<McpServer.TransactionSecurity.Models.TransactionStatusResponse?> GetTransactionStatusAsync(
            string transactionId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<McpServer.TransactionSecurity.Models.TransactionStatusResponse?>(null);

        public Task<McpServer.TransactionSecurity.Models.TransactionAbortResponse> AbortTransactionAsync(
            string transactionId,
            McpServer.TransactionSecurity.Models.TransactionAbortRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new McpServer.TransactionSecurity.Models.TransactionAbortResponse
            {
                TransactionId = transactionId,
                Status = "aborted",
                Reason = request.Reason,
                AbortedAtUtc = DateTimeOffset.UtcNow,
            });
    }

    private sealed class TempWorkspace : IDisposable
    {
        private TempWorkspace(string rootPath)
        {
            RootPath = rootPath;
        }

        public string RootPath { get; }

        public static TempWorkspace Create()
        {
            var rootPath = Path.Combine(
                Path.GetTempPath(),
                "mcpserver-separate-transaction-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootPath);
            return new TempWorkspace(rootPath);
        }

        public string GetPath(string fileName)
            => Path.Combine(RootPath, fileName);

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, recursive: true);
        }
    }

    private sealed class FixedOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
        where TOptions : class
    {
        public FixedOptionsMonitor(TOptions currentValue)
        {
            CurrentValue = currentValue;
        }

        public TOptions CurrentValue { get; }

        public TOptions Get(string? name)
            => CurrentValue;

        public IDisposable? OnChange(Action<TOptions, string?> listener)
            => null;
    }
}
