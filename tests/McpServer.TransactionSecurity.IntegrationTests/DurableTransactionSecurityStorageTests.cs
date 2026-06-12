using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace McpServer.TransactionSecurity.IntegrationTests;

/// <summary>
/// TEST-MCP-158 and TEST-MCP-159: Durable transaction-security storage coverage derived from SD-KEYSERVER-001 and SD-DIFFGRAM-001.
/// </summary>
public sealed class DurableTransactionSecurityStorageTests
{
    private const string PublisherPartyId = "publisher-1";
    private const string SubscriberPartyId = "subscriber-1";
    private const string ExternalPublisherSigningKeyId = "publisher-1:signing:external";

    /// <summary>Keyserver public descriptors and audit rows survive service recreation.</summary>
    [Fact]
    public async Task KeyServerSqliteStore_VerifiesManifestAfterServiceRecreation()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("keyserver.db");
        TransactionManifestDto manifest;

        using (var keyServer = CreateKeyServer(databasePath))
        {
            await RegisterStandardPartiesAsync(keyServer).ConfigureAwait(true);
            manifest = await SignManifestAsync(keyServer, "txn-keyserver-durable-verify", 100, "nonce-keyserver-durable")
                .ConfigureAwait(true);
        }

        using var recreatedKeyServer = CreateKeyServer(databasePath);
        var descriptor = await recreatedKeyServer
            .GetPartyKeyAsync(PublisherPartyId, $"{PublisherPartyId}:signing:1")
            .ConfigureAwait(true);
        var verify = await recreatedKeyServer.VerifyManifestAsync(
            new TransactionManifestVerifyRequest
            {
                Manifest = manifest,
                ExpectedSubscriberPartyId = SubscriberPartyId,
            }).ConfigureAwait(true);
        var audit = await ReadAuditEventsAsync(databasePath).ConfigureAwait(true);

        Assert.NotNull(descriptor);
        Assert.Equal(PublisherPartyId, descriptor.PartyId);
        Assert.True(verify.IsValid);
        Assert.Equal(TransactionFailureReason.None, verify.Reason);
        Assert.Contains(audit, entry => entry.EventName == "keyserver.party.registered");
        Assert.Contains(audit, entry => entry.EventName == "keyserver.manifest.signed");
        Assert.Contains(audit, entry => entry.EventName == "keyserver.manifest.verified");
    }

    /// <summary>Externally supplied signing private material can be re-provisioned after service recreation.</summary>
    [Fact]
    public async Task KeyServerSqliteStore_ReprovisionsExternalSigningPrivateKeyAfterServiceRecreation()
    {
        using var workspace = TempWorkspace.Create();
        using var signingKey = SigningKeyPair.Create();
        var databasePath = workspace.GetPath("keyserver-external-signing.db");
        TransactionManifestDto firstManifest;

        using (var keyServer = CreateKeyServer(databasePath))
        {
            await RegisterExternalSigningPublisherAsync(keyServer, signingKey).ConfigureAwait(true);
            await RegisterSubscriberAsync(keyServer).ConfigureAwait(true);
            firstManifest = await SignManifestAsync(
                keyServer,
                "txn-external-signing-first",
                500,
                "nonce-external-signing-first",
                ExternalPublisherSigningKeyId).ConfigureAwait(true);
        }

        using (var recreatedWithoutMaterial = CreateKeyServer(databasePath))
        {
            var descriptor = await recreatedWithoutMaterial.GetPartyKeyAsync(PublisherPartyId, ExternalPublisherSigningKeyId)
                .ConfigureAwait(true);
            var missingPrivateMaterial = await recreatedWithoutMaterial.SignManifestAsync(
                new TransactionManifestSignRequest
                {
                    TransactionId = "txn-external-signing-missing-private",
                    TurnId = "turn-external-key-material",
                    PublisherPartyId = PublisherPartyId,
                    PublisherSigningKeyId = ExternalPublisherSigningKeyId,
                    SubscriberPartyId = SubscriberPartyId,
                    Sequence = 501,
                    Nonce = "nonce-external-signing-missing-private",
                    DiffgramSha256 = Sha256Hex("plain-diffgram"),
                    EncryptedBodySha256 = Sha256Hex("encrypted-diffgram"),
                }).ConfigureAwait(true);
            var verifyExisting = await recreatedWithoutMaterial.VerifyManifestAsync(
                new TransactionManifestVerifyRequest
                {
                    Manifest = firstManifest,
                    ExpectedSubscriberPartyId = SubscriberPartyId,
                }).ConfigureAwait(true);

            Assert.NotNull(descriptor);
            Assert.Equal(signingKey.PublicKeyPem, descriptor.PublicKeyPem);
            Assert.False(missingPrivateMaterial.Success);
            Assert.Equal(TransactionFailureReason.UnknownKey, missingPrivateMaterial.Reason);
            Assert.True(verifyExisting.IsValid);
        }

        using var recreatedWithMaterial = CreateKeyServer(databasePath);
        var registration = await RegisterExternalSigningPublisherAsync(recreatedWithMaterial, signingKey).ConfigureAwait(true);
        var secondManifest = await SignManifestAsync(
            recreatedWithMaterial,
            "txn-external-signing-second",
            501,
            "nonce-external-signing-second",
            ExternalPublisherSigningKeyId).ConfigureAwait(true);
        var publicDescriptor = await recreatedWithMaterial.GetPartyKeyAsync(PublisherPartyId, ExternalPublisherSigningKeyId)
            .ConfigureAwait(true);
        var registrationJson = JsonSerializer.Serialize(registration);

        Assert.Equal(ExternalPublisherSigningKeyId, secondManifest.PublisherSigningKeyId);
        Assert.NotNull(publicDescriptor);
        Assert.Equal(signingKey.PublicKeyPem, publicDescriptor.PublicKeyPem);
        Assert.DoesNotContain("PRIVATE KEY", registrationJson, StringComparison.Ordinal);
    }

    /// <summary>Registration rejects mismatched public/private signing material instead of publishing a false descriptor.</summary>
    [Fact]
    public async Task RegisterParty_WithConflictingExternalSigningPublicKey_Throws()
    {
        using var privateMaterial = SigningKeyPair.Create();
        using var conflictingPublicMaterial = SigningKeyPair.Create();
        using var keyServer = CreateKeyServer();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => keyServer.RegisterPartyAsync(
                new PartyRegistrationRequest
                {
                    PartyId = PublisherPartyId,
                    Role = "publisher",
                    ActiveSigningKeyId = ExternalPublisherSigningKeyId,
                    SigningPrivateKeyPem = privateMaterial.PrivateKeyPem,
                    SigningPublicKeyPem = conflictingPublicMaterial.PublicKeyPem,
                })).ConfigureAwait(true);

        Assert.Contains("does not match", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Keyserver replay nonce and sequence cursors survive SQLite store recreation.</summary>
    [Fact]
    public async Task KeyServerSqliteStore_PersistsReplayStateAcrossStoreRecreation()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("keyserver-replay.db");
        const string pairKey = $"{PublisherPartyId}\n{SubscriberPartyId}";
        const string nonceKey = $"{pairKey}\nnonce-keyserver-durable-replay";

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        {
            var first = await store.TryReserveManifestReplayAsync(
                "sign",
                pairKey,
                sequence: 300,
                nonceKey,
                transactionId: "txn-keyserver-replay-first",
                CancellationToken.None).ConfigureAwait(true);

            Assert.Equal(TransactionFailureReason.None, first);
        }

        using var recreatedStore = new SqliteTransactionSecurityStateStore(databasePath);
        var stale = await recreatedStore.TryReserveManifestReplayAsync(
            "sign",
            pairKey,
            sequence: 299,
            $"{pairKey}\nnonce-keyserver-durable-stale",
            transactionId: "txn-keyserver-replay-stale",
            CancellationToken.None).ConfigureAwait(true);
        var replay = await recreatedStore.TryReserveManifestReplayAsync(
            "sign",
            pairKey,
            sequence: 301,
            nonceKey,
            transactionId: "txn-keyserver-replay-duplicate",
            CancellationToken.None).ConfigureAwait(true);
        var next = await recreatedStore.TryReserveManifestReplayAsync(
            "sign",
            pairKey,
            sequence: 301,
            $"{pairKey}\nnonce-keyserver-durable-next",
            transactionId: "txn-keyserver-replay-next",
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(TransactionFailureReason.StaleSequence, stale);
        Assert.Equal(TransactionFailureReason.ReplayNonce, replay);
        Assert.Equal(TransactionFailureReason.None, next);
    }

    /// <summary>Keyserver verification replay nonce and sequence cursors use a durable scope separate from signing.</summary>
    [Fact]
    public async Task KeyServerSqliteStore_PersistsVerificationReplayStateAcrossStoreRecreation()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("keyserver-verify-replay.db");
        const string pairKey = $"{PublisherPartyId}\n{SubscriberPartyId}";
        const string nonceKey = $"{pairKey}\nnonce-keyserver-verify-replay";

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        {
            var first = await store.TryReserveManifestReplayAsync(
                "verify",
                pairKey,
                sequence: 400,
                nonceKey,
                transactionId: "txn-keyserver-verify-first",
                CancellationToken.None).ConfigureAwait(true);

            Assert.Equal(TransactionFailureReason.None, first);
        }

        using var recreatedStore = new SqliteTransactionSecurityStateStore(databasePath);
        var stale = await recreatedStore.TryReserveManifestReplayAsync(
            "verify",
            pairKey,
            sequence: 399,
            $"{pairKey}\nnonce-keyserver-verify-stale",
            transactionId: "txn-keyserver-verify-stale",
            CancellationToken.None).ConfigureAwait(true);
        var replay = await recreatedStore.TryReserveManifestReplayAsync(
            "verify",
            pairKey,
            sequence: 401,
            nonceKey,
            transactionId: "txn-keyserver-verify-duplicate",
            CancellationToken.None).ConfigureAwait(true);
        var signingScope = await recreatedStore.TryReserveManifestReplayAsync(
            "sign",
            pairKey,
            sequence: 400,
            nonceKey,
            transactionId: "txn-keyserver-sign-separate",
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(TransactionFailureReason.StaleSequence, stale);
        Assert.Equal(TransactionFailureReason.ReplayNonce, replay);
        Assert.Equal(TransactionFailureReason.None, signingScope);
    }

    /// <summary>Subscriber commits, duplicate idempotency, sequence cursors, and audit rows survive service recreation.</summary>
    [Fact]
    public async Task SubscriberSqliteStore_PersistsCommitStatusAndReplayStateAcrossServiceRecreation()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("subscriber.db");
        using var keyServer = CreateKeyServer();
        await RegisterStandardPartiesAsync(keyServer).ConfigureAwait(true);
        var staleManifest = await SignManifestAsync(keyServer, "txn-subscriber-durable-stale", 200, "nonce-subscriber-durable-1")
            .ConfigureAwait(true);
        var firstManifest = await SignManifestAsync(keyServer, "txn-subscriber-durable-commit", 201, "nonce-subscriber-durable-2")
            .ConfigureAwait(true);
        var firstRequest = CreateCommitRequest(firstManifest);

        using (var subscriber = CreateSubscriber(keyServer, databasePath))
        {
            var commit = await subscriber.CommitDiffgramAsync(firstRequest).ConfigureAwait(true);

            Assert.Equal("committed", commit.Status);
            Assert.Equal(TransactionFailureReason.None, commit.Reason);
        }

        using var recreatedSubscriber = CreateSubscriber(keyServer, databasePath);
        var status = await recreatedSubscriber.GetTransactionStatusAsync(firstManifest.TransactionId).ConfigureAwait(true);
        var duplicate = await recreatedSubscriber.CommitDiffgramAsync(firstRequest).ConfigureAwait(true);
        var stale = await recreatedSubscriber.CommitDiffgramAsync(CreateCommitRequest(staleManifest)).ConfigureAwait(true);
        var staleStatus = await recreatedSubscriber.GetTransactionStatusAsync(staleManifest.TransactionId).ConfigureAwait(true);
        var audit = await ReadAuditEventsAsync(databasePath).ConfigureAwait(true);

        Assert.NotNull(status);
        Assert.Equal("committed", status.Status);
        Assert.Equal("committed", duplicate.Status);
        Assert.Equal(TransactionFailureReason.None, duplicate.Reason);
        Assert.Equal("rejected", stale.Status);
        Assert.Equal(TransactionFailureReason.StaleSequence, stale.Reason);
        Assert.NotNull(staleStatus);
        Assert.Equal("rejected", staleStatus.Status);
        Assert.Equal(TransactionFailureReason.StaleSequence, staleStatus.Reason);
        Assert.Contains(audit, entry => entry.EventName == "subscriber.transaction.committed");
        Assert.Contains(audit, entry => entry.EventName == "subscriber.transaction.duplicate");
        Assert.Contains(audit, entry => entry.EventName == "subscriber.transaction.rejected");
    }

    private static InMemoryKeyServerService CreateKeyServer(string? databasePath = null)
        => new(
            new FixedOptionsMonitor<KeyServerOptions>(new KeyServerOptions { DatabasePath = databasePath }),
            new TransactionManifestCanonicalizer());

    private static InMemorySubscriberCommitService CreateSubscriber(
        IKeyServerManifestService keyServer,
        string? databasePath = null)
        => new(
            keyServer,
            new TransactionManifestCanonicalizer(),
            new FixedOptionsMonitor<SubscriberOptions>(
                new SubscriberOptions
                {
                    DatabasePath = databasePath,
                    PartyId = SubscriberPartyId,
                }));

    private static async Task RegisterStandardPartiesAsync(IKeyServerPartyRegistry registry)
    {
        await registry.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = PublisherPartyId, Role = "publisher" })
            .ConfigureAwait(false);
        await RegisterSubscriberAsync(registry).ConfigureAwait(false);
    }

    private static Task<PartyRegistrationResponse> RegisterExternalSigningPublisherAsync(
        IKeyServerPartyRegistry registry,
        SigningKeyPair signingKey)
        => registry.RegisterPartyAsync(
            new PartyRegistrationRequest
            {
                PartyId = PublisherPartyId,
                Role = "publisher",
                ActiveSigningKeyId = ExternalPublisherSigningKeyId,
                SigningPrivateKeyPem = signingKey.PrivateKeyPem,
            });

    private static Task<PartyRegistrationResponse> RegisterSubscriberAsync(IKeyServerPartyRegistry registry)
        => registry.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = SubscriberPartyId, Role = "subscriber" });

    private static async Task<TransactionManifestDto> SignManifestAsync(
        IKeyServerManifestService keyServer,
        string transactionId,
        long sequence,
        string nonce,
        string publisherSigningKeyId)
    {
        var response = await keyServer.SignManifestAsync(new TransactionManifestSignRequest
        {
            TransactionId = transactionId,
            TurnId = "turn-durable-storage",
            PublisherPartyId = PublisherPartyId,
            PublisherSigningKeyId = publisherSigningKeyId,
            SubscriberPartyId = SubscriberPartyId,
            Sequence = sequence,
            Nonce = nonce,
            DiffgramSha256 = Sha256Hex("plain-diffgram"),
            EncryptedBodySha256 = Sha256Hex("encrypted-diffgram"),
        }).ConfigureAwait(false);

        Assert.True(response.Success);
        Assert.NotNull(response.Manifest);
        return response.Manifest;
    }

    private static async Task<TransactionManifestDto> SignManifestAsync(
        IKeyServerManifestService keyServer,
        string transactionId,
        long sequence,
        string nonce)
    {
        var response = await keyServer.SignManifestAsync(new TransactionManifestSignRequest
        {
            TransactionId = transactionId,
            TurnId = "turn-durable-storage",
            PublisherPartyId = PublisherPartyId,
            SubscriberPartyId = SubscriberPartyId,
            Sequence = sequence,
            Nonce = nonce,
            DiffgramSha256 = Sha256Hex("plain-diffgram"),
            EncryptedBodySha256 = Sha256Hex("encrypted-diffgram"),
        }).ConfigureAwait(false);

        Assert.True(response.Success);
        Assert.NotNull(response.Manifest);
        return response.Manifest;
    }

    private static DiffgramCommitRequest CreateCommitRequest(TransactionManifestDto manifest)
        => new()
        {
            Manifest = manifest,
            EncryptedDiffgramBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-diffgram")),
            EncryptedBodySha256 = manifest.EncryptedBodySha256,
            DiffgramSha256 = manifest.DiffgramSha256,
        };

    private static async Task<IReadOnlyList<TransactionAuditEntity>> ReadAuditEventsAsync(string databasePath)
    {
        var options = new DbContextOptionsBuilder<TransactionSecurityDbContext>()
            .UseSqlite($"Data Source={Path.GetFullPath(databasePath)}")
            .Options;
        await using var db = new TransactionSecurityDbContext(options);
        return await db.TransactionAuditEvents
            .AsNoTracking()
            .OrderBy(entry => entry.Id)
            .ToArrayAsync()
            .ConfigureAwait(false);
    }

    private static string Sha256Hex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class FixedOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly T _value;

        public FixedOptionsMonitor(T value)
        {
            _value = value;
        }

        public T CurrentValue => _value;

        public T Get(string? name) => _value;

        public IDisposable? OnChange(Action<T, string?> listener) => NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
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
                "mcpserver-transaction-security-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootPath);
            return new TempWorkspace(rootPath);
        }

        public string GetPath(string fileName)
            => Path.Combine(RootPath, fileName);

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, recursive: true);
        }
    }
}
