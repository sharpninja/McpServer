using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>
/// Unit tests for keyserver and subscriber clients used by transactional diffgram exchange.
/// Covers activity-diagram endpoints for party registration, manifest signing/verification,
/// diffgram commit, status lookup, and abort handling.
/// FR-MCP-118, FR-MCP-120, FR-MCP-121, FR-MCP-122, FR-MCP-123, FR-MCP-124.
/// </summary>
public sealed class TransactionSecurityClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
        WorkspacePath = @"F:\GitHub\McpServer"
    };

    /// <summary>Registering a party posts the expected route and serializes the key metadata.</summary>
    [Fact]
    public async Task KeyServerClient_RegisterPartyAsync_PostsExpectedRouteAndBody()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"partyId":"publisher-1","role":"publisher","activeSigningKeyId":"sign-1","activeEncryptionKeyId":"enc-1","status":"active","createdAtUtc":"2026-06-11T12:00:00Z"}
            """);
        using var http = new HttpClient(handler);
        var client = new KeyServerClient(http, DefaultOptions);

        var result = await client.RegisterPartyAsync(new PartyRegistrationRequest
        {
            PartyId = "publisher-1",
            Role = "publisher",
            ActiveSigningKeyId = "sign-1",
            ActiveEncryptionKeyId = "enc-1",
            SigningPublicKeyPem = "-----BEGIN PUBLIC KEY-----",
            SigningPrivateKeyPem = "-----BEGIN PRIVATE KEY-----",
            EncryptionPublicKeyPem = "-----BEGIN PUBLIC KEY-----"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/keyserver/parties", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"partyId\":\"publisher-1\"", handler.LastRequestBody);
        Assert.Contains("\"activeSigningKeyId\":\"sign-1\"", handler.LastRequestBody);
        Assert.Contains("\"signingPrivateKeyPem\":\"-----BEGIN PRIVATE KEY-----\"", handler.LastRequestBody);
        Assert.Equal("publisher-1", result.PartyId);
        Assert.Equal("active", result.Status);
    }

    /// <summary>Signing a manifest posts the canonical manifest request and deserializes the signed manifest.</summary>
    [Fact]
    public async Task KeyServerClient_SignManifestAsync_PostsExpectedRouteAndDeserializes()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"success":true,"reason":0,"manifest":{"transactionId":"txn-1","publisherPartyId":"publisher-1","subscriberPartyId":"subscriber-1","publisherSigningKeyId":"sign-1","subscriberEncryptionKeyId":"enc-1","sequence":42,"nonce":"nonce-1","issuedAtUtc":"2026-06-11T12:00:00Z","expiresAtUtc":"2026-06-11T12:05:00Z","diffgramSha256":"plain-hash","encryptedBodySha256":"encrypted-hash","algorithms":{"signature":"ECDSA-P256-SHA256","encryption":"ECDH-P256-HKDF-SHA256-AES-256-GCM","canonicalization":"transaction-manifest-v1"},"signature":{"algorithm":"ECDSA-P256-SHA256","keyId":"sign-1","value":"sig","signedAtUtc":"2026-06-11T12:00:01Z"}}}
            """);
        using var http = new HttpClient(handler);
        var client = new KeyServerClient(http, DefaultOptions);

        var result = await client.SignManifestAsync(CreateSignRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/keyserver/manifests/sign", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"transactionId\":\"txn-1\"", handler.LastRequestBody);
        Assert.Contains("\"diffgramSha256\":\"plain-hash\"", handler.LastRequestBody);
        Assert.True(result.Success);
        Assert.NotNull(result.Manifest);
        Assert.Equal("txn-1", result.Manifest.TransactionId);
        Assert.Equal("sign-1", result.Manifest.Signature!.KeyId);
    }

    /// <summary>Verifying a manifest posts the signed manifest and returns the verification hash.</summary>
    [Fact]
    public async Task KeyServerClient_VerifyManifestAsync_PostsExpectedRouteAndDeserializes()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"isValid":true,"reason":0,"manifestHashSha256":"manifest-hash"}""");
        using var http = new HttpClient(handler);
        var client = new KeyServerClient(http, DefaultOptions);

        var result = await client.VerifyManifestAsync(new TransactionManifestVerifyRequest
        {
            Manifest = CreateManifest(),
            ExpectedSubscriberPartyId = "subscriber-1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/keyserver/manifests/verify", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"expectedSubscriberPartyId\":\"subscriber-1\"", handler.LastRequestBody);
        Assert.True(result.IsValid);
        Assert.Equal(TransactionFailureReason.None, result.Reason);
        Assert.Equal("manifest-hash", result.ManifestHashSha256);
    }

    /// <summary>Key lookups URL-encode party and key identifiers.</summary>
    [Fact]
    public async Task KeyServerClient_GetPartyKeyAsync_EncodesPartyAndKey()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"partyId":"publisher A","keyId":"key/1","purpose":"signing","algorithm":"ECDSA-P256-SHA256","publicKeyPem":"pem","status":"active","createdAtUtc":"2026-06-11T12:00:00Z"}
            """);
        using var http = new HttpClient(handler);
        var client = new KeyServerClient(http, DefaultOptions);

        var result = await client.GetPartyKeyAsync("publisher A", "key/1", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/keyserver/parties/publisher%20A/keys/key%2F1", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("key/1", result.KeyId);
    }

    /// <summary>Manifest trace lookups URL-encode transaction identifiers and deserialize audit metadata.</summary>
    [Fact]
    public async Task KeyServerClient_GetManifestAsync_EncodesTransactionAndDeserializes()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"transactionId":"txn/1","turnId":"turn-1","publisherPartyId":"publisher-1","publisherSigningKeyId":"sign-1","subscriberPartyId":"subscriber-1","subscriberEncryptionKeyId":"enc-1","sequence":42,"nonce":"nonce-1","issuedAtUtc":"2026-06-11T12:00:00Z","expiresAtUtc":"2026-06-11T12:05:00Z","diffgramSha256":"plain-hash","encryptedBodySha256":"encrypted-hash","signatureAlgorithm":"ECDSA-P256-SHA256","signatureKeyId":"sign-1","signatureValue":"sig","signedAtUtc":"2026-06-11T12:00:01Z","manifestHashSha256":"manifest-hash","status":"signed","createdAtUtc":"2026-06-11T12:00:01Z"}
            """);
        using var http = new HttpClient(handler);
        var client = new KeyServerClient(http, DefaultOptions);

        var result = await client.GetManifestAsync("txn/1", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/keyserver/manifests/txn%2F1", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("txn/1", result.TransactionId);
        Assert.Equal("sign-1", result.SignatureKeyId);
        Assert.Equal("manifest-hash", result.ManifestHashSha256);
        Assert.Equal("signed", result.Status);
    }

    /// <summary>Manifest trace reports include filter query values and deserialize ledger summaries.</summary>
    [Fact]
    public async Task KeyServerClient_GetManifestReportAsync_AppendsFiltersAndDeserializes()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"generatedAtUtc":"2026-06-11T12:01:00Z","publisherPartyId":"publisher A","subscriberPartyId":"subscriber/1","status":"signed","limit":25,"totalCount":1,"returnedCount":1,"records":[{"transactionId":"txn/1","turnId":"turn-1","publisherPartyId":"publisher A","publisherSigningKeyId":"sign-1","subscriberPartyId":"subscriber/1","subscriberEncryptionKeyId":"enc-1","sequence":42,"nonce":"nonce-1","issuedAtUtc":"2026-06-11T12:00:00Z","expiresAtUtc":"2026-06-11T12:05:00Z","diffgramSha256":"plain-hash","encryptedBodySha256":"encrypted-hash","signatureAlgorithm":"ECDSA-P256-SHA256","signatureKeyId":"sign-1","signatureValue":"sig","signedAtUtc":"2026-06-11T12:00:01Z","manifestHashSha256":"manifest-hash","status":"signed","createdAtUtc":"2026-06-11T12:00:01Z"}]}
            """);
        using var http = new HttpClient(handler);
        var client = new KeyServerClient(http, DefaultOptions);

        var result = await client.GetManifestReportAsync(new TransactionManifestTraceReportRequest
        {
            PublisherPartyId = "publisher A",
            SubscriberPartyId = "subscriber/1",
            Status = "signed",
            Limit = 25,
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/keyserver/manifests/report", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("publisherPartyId=publisher%20A", handler.LastRequest.RequestUri.Query);
        Assert.Contains("subscriberPartyId=subscriber%2F1", handler.LastRequest.RequestUri.Query);
        Assert.Contains("status=signed", handler.LastRequest.RequestUri.Query);
        Assert.Contains("limit=25", handler.LastRequest.RequestUri.Query);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.ReturnedCount);
        Assert.Equal("txn/1", Assert.Single(result.Records).TransactionId);
    }

    /// <summary>Committing a diffgram posts the signed manifest and encrypted payload.</summary>
    [Fact]
    public async Task SubscriberClient_CommitDiffgramAsync_PostsExpectedRouteAndBody()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"status":"committed","reason":0,"transactionId":"txn-1","diffgramId":"diff-1","committedAtUtc":"2026-06-11T12:00:02Z"}""");
        using var http = new HttpClient(handler);
        var client = new SubscriberClient(http, DefaultOptions);

        var result = await client.CommitDiffgramAsync(new DiffgramCommitRequest
        {
            Manifest = CreateManifest(),
            EncryptedDiffgramBase64 = "encrypted-body",
            EncryptedBodySha256 = "encrypted-hash",
            DiffgramSha256 = "plain-hash"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/subscriber/diffgrams/commit", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"encryptedDiffgramBase64\":\"encrypted-body\"", handler.LastRequestBody);
        Assert.Contains("\"transactionId\":\"txn-1\"", handler.LastRequestBody);
        Assert.Equal("committed", result.Status);
        Assert.Equal("diff-1", result.DiffgramId);
    }

    /// <summary>Status lookups URL-encode the transaction identifier.</summary>
    [Fact]
    public async Task SubscriberClient_GetTransactionStatusAsync_EncodesTransaction()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"transactionId":"txn/1","status":"committed","reason":0,"committedAtUtc":"2026-06-11T12:00:02Z"}""");
        using var http = new HttpClient(handler);
        var client = new SubscriberClient(http, DefaultOptions);

        var result = await client.GetTransactionStatusAsync("txn/1", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/subscriber/transactions/txn%2F1/status", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("committed", result.Status);
    }

    /// <summary>Abort requests post structured failure reasons and return abort metadata.</summary>
    [Fact]
    public async Task SubscriberClient_AbortTransactionAsync_PostsExpectedRouteAndBody()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"transactionId":"txn-1","status":"aborted","reason":17,"abortedAtUtc":"2026-06-11T12:00:03Z"}""");
        using var http = new HttpClient(handler);
        var client = new SubscriberClient(http, DefaultOptions);

        var result = await client.AbortTransactionAsync("txn-1", new TransactionAbortRequest
        {
            Reason = TransactionFailureReason.Aborted,
            Actor = "coordinator"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/subscriber/transactions/txn-1/abort", handler.LastRequest.RequestUri!.AbsolutePath);
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal((int)TransactionFailureReason.Aborted, body.RootElement.GetProperty("reason").GetInt32());
        Assert.Equal("coordinator", body.RootElement.GetProperty("actor").GetString());
        Assert.Equal(TransactionFailureReason.Aborted, result.Reason);
        Assert.Equal("aborted", result.Status);
    }

    /// <summary>The facade exposes transaction clients and propagates retargeting settings.</summary>
    [Fact]
    public async Task McpServerClient_ExposesTransactionClientsAndPropagatesSettings()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"partyId":"publisher-1","keyId":"sign-1","purpose":"signing","algorithm":"ECDSA-P256-SHA256","publicKeyPem":"pem","status":"active","createdAtUtc":"2026-06-11T12:00:00Z"}
            """);
        using var http = new HttpClient(handler);
        var client = new McpServerClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            WorkspacePath = @"F:\GitHub\McpServer"
        });

        client.ApiKey = "rotated-key";
        client.Port = 7155;

        var result = await client.KeyServer.GetPartyKeyAsync("publisher-1", "sign-1", cancellationToken: TestContext.Current.CancellationToken);

        Assert.IsType<KeyServerClient>(client.KeyServer);
        Assert.IsType<SubscriberClient>(client.Subscriber);
        Assert.IsType<TurnTransactionsClient>(client.TurnTransactions);
        Assert.Equal(7155, handler.LastRequest!.RequestUri!.Port);
        Assert.True(handler.LastRequest.Headers.TryGetValues("X-Api-Key", out var apiKeys));
        Assert.Contains("rotated-key", apiKeys);
        Assert.Equal("sign-1", result.KeyId);
    }

    /// <summary>Turn transaction status reads the gate-state endpoint and deserializes failure metadata.</summary>
    [Fact]
    public async Task TurnTransactionsClient_GetStatusAsync_GetsStatusEndpoint()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"enabled":true,"degraded":false,"lastReason":0,"lastTransactionId":"txn-1","message":"ok"}""");
        using var http = new HttpClient(handler);
        var client = new TurnTransactionsClient(http, DefaultOptions);

        var result = await client.GetStatusAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/turntransactions/status", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Enabled);
        Assert.Equal("txn-1", result.LastTransactionId);
    }

    /// <summary>Pub/sub diagnostics append the maxMessages query value and deserialize message status rows.</summary>
    [Fact]
    public async Task TurnTransactionsClient_GetPubSubStatusAsync_AppendsMaxMessages()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """[{"operationId":"op-1","transactionId":"txn-1","kind":"commit","topicName":"turns","subscriberId":"sub-1","status":"pending","attemptCount":2,"reason":20,"createdAtUtc":"2026-06-11T12:00:00Z","updatedAtUtc":"2026-06-11T12:01:00Z"}]""");
        using var http = new HttpClient(handler);
        var client = new TurnTransactionsClient(http, DefaultOptions);

        var result = await client.GetPubSubStatusAsync(maxMessages: 7, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/turntransactions/pubsub/status", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("maxMessages=7", handler.LastRequest.RequestUri.Query);
        var message = Assert.Single(result);
        Assert.Equal("op-1", message.OperationId);
        Assert.Equal(TransactionFailureReason.CommitTimeout, message.Reason);
    }

    /// <summary>Pub/sub replay posts to the replay endpoint and deserializes replay counts.</summary>
    [Fact]
    public async Task TurnTransactionsClient_ReplayPubSubAsync_PostsReplayEndpoint()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"attemptedCount":3,"acknowledgedCount":2,"pendingCount":1}""");
        using var http = new HttpClient(handler);
        var client = new TurnTransactionsClient(http, DefaultOptions);

        var result = await client.ReplayPubSubAsync(maxMessages: 3, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/turntransactions/pubsub/replay", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("maxMessages=3", handler.LastRequest.RequestUri.Query);
        Assert.Equal(2, result.AcknowledgedCount);
    }

    /// <summary>Pub/sub retention purge posts cutoff and limit query values.</summary>
    [Fact]
    public async Task TurnTransactionsClient_PurgePubSubRetentionAsync_PostsRetentionEndpoint()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"completedBeforeUtc":"2026-06-11T12:00:00Z","maxMessages":5,"purgedCount":4,"retainedPendingCount":1}""");
        using var http = new HttpClient(handler);
        var client = new TurnTransactionsClient(http, DefaultOptions);

        var result = await client.PurgePubSubRetentionAsync(
            DateTimeOffset.Parse("2026-06-11T12:00:00Z"),
            maxMessages: 5, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/turntransactions/pubsub/retention/purge", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("completedBeforeUtc=", handler.LastRequest.RequestUri.Query);
        Assert.Contains("maxMessages=5", handler.LastRequest.RequestUri.Query);
        Assert.Equal(4, result.PurgedCount);
    }

    private static TransactionManifestSignRequest CreateSignRequest()
        => new()
        {
            TransactionId = "txn-1",
            TurnId = "turn-1",
            PublisherPartyId = "publisher-1",
            SubscriberPartyId = "subscriber-1",
            PublisherSigningKeyId = "sign-1",
            SubscriberEncryptionKeyId = "enc-1",
            Sequence = 42,
            Nonce = "nonce-1",
            IssuedAtUtc = DateTimeOffset.Parse("2026-06-11T12:00:00Z"),
            ExpiresAtUtc = DateTimeOffset.Parse("2026-06-11T12:05:00Z"),
            DiffgramSha256 = "plain-hash",
            EncryptedBodySha256 = "encrypted-hash"
        };

    private static TransactionManifestDto CreateManifest()
        => new()
        {
            TransactionId = "txn-1",
            TurnId = "turn-1",
            PublisherPartyId = "publisher-1",
            SubscriberPartyId = "subscriber-1",
            PublisherSigningKeyId = "sign-1",
            SubscriberEncryptionKeyId = "enc-1",
            Sequence = 42,
            Nonce = "nonce-1",
            IssuedAtUtc = DateTimeOffset.Parse("2026-06-11T12:00:00Z"),
            ExpiresAtUtc = DateTimeOffset.Parse("2026-06-11T12:05:00Z"),
            DiffgramSha256 = "plain-hash",
            EncryptedBodySha256 = "encrypted-hash",
            Signature = new TransactionManifestSignatureDto
            {
                Algorithm = "ECDSA-P256-SHA256",
                KeyId = "sign-1",
                Value = "sig",
                SignedAtUtc = DateTimeOffset.Parse("2026-06-11T12:00:01Z")
            }
        };
}
