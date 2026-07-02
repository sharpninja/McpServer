using System.Security.Cryptography;
using System.Text;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.TransactionSecurity.IntegrationTests;

/// <summary>
/// TEST-MCP-159 and TEST-MCP-167: Subscriber encryption/decryption tests derived from SD-DIFFGRAM-001.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DiffgramEncryptionIntegrationTests
{
    private const string PublisherPartyId = "publisher-1";
    private const string SubscriberPartyId = "subscriber-1";
    private const string SubscriberEncryptionKeyId = "subscriber-1:encryption:1";
    private const string RotatedSubscriberEncryptionKeyId = "subscriber-1:encryption:2";

    /// <summary>Subscriber decrypts a protected diffgram addressed to its party/key and commits it.</summary>
    [Fact]
    public async Task CommitDiffgram_WithProtectedEnvelopeForSubscriber_DecryptsAndCommits()
    {
        using var keyPair = EncryptionKeyPair.Create();
        using var keyServer = CreateKeyServer();
        var protector = new TransactionDiffgramProtector();
        await RegisterStandardPartiesAsync(keyServer, keyPair).ConfigureAwait(true);
        var encryptionKey = await keyServer.GetPartyKeyAsync(SubscriberPartyId, SubscriberEncryptionKeyId)
            .ConfigureAwait(true);
        Assert.NotNull(encryptionKey);
        var protectedDiffgram = protector.Protect(CreatePlaintextDiffgram("txn-encrypted-valid"), encryptionKey);
        var manifest = await SignAsync(
            keyServer,
            "txn-encrypted-valid",
            sequence: 500,
            nonce: "nonce-encrypted-valid",
            protectedDiffgram.PlaintextSha256,
            protectedDiffgram.EncryptedBodySha256).ConfigureAwait(true);
        using var subscriber = CreateSubscriber(keyServer, keyPair.PrivateKeyPem, protector);

        var commit = await subscriber.CommitDiffgramAsync(CreateCommitRequest(manifest, protectedDiffgram))
            .ConfigureAwait(true);

        Assert.Equal("committed", commit.Status);
        Assert.Equal(TransactionFailureReason.None, commit.Reason);
        Assert.Equal("diffgram-txn-encrypted-valid", commit.DiffgramId);
    }

    /// <summary>Subscriber validates the decrypted plaintext hash, not only the caller-supplied hash fields.</summary>
    [Fact]
    public async Task CommitDiffgram_WithProtectedEnvelopePlaintextMismatch_RejectsAfterDecrypt()
    {
        using var keyPair = EncryptionKeyPair.Create();
        using var keyServer = CreateKeyServer();
        var protector = new TransactionDiffgramProtector();
        await RegisterStandardPartiesAsync(keyServer, keyPair).ConfigureAwait(true);
        var encryptionKey = await keyServer.GetPartyKeyAsync(SubscriberPartyId, SubscriberEncryptionKeyId)
            .ConfigureAwait(true);
        Assert.NotNull(encryptionKey);
        var protectedDiffgram = protector.Protect(CreatePlaintextDiffgram("txn-encrypted-plaintext-mismatch"), encryptionKey);
        var signedPlaintextHash = Sha256Hex("different plaintext body");
        var manifest = await SignAsync(
            keyServer,
            "txn-encrypted-plaintext-mismatch",
            sequence: 501,
            nonce: "nonce-encrypted-plaintext-mismatch",
            signedPlaintextHash,
            protectedDiffgram.EncryptedBodySha256).ConfigureAwait(true);
        using var subscriber = CreateSubscriber(keyServer, keyPair.PrivateKeyPem, protector);

        var commit = await subscriber.CommitDiffgramAsync(new DiffgramCommitRequest
        {
            Manifest = manifest,
            EncryptedDiffgramBase64 = protectedDiffgram.EncryptedDiffgramBase64,
            EncryptedBodySha256 = protectedDiffgram.EncryptedBodySha256,
            DiffgramSha256 = signedPlaintextHash,
        }).ConfigureAwait(true);

        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.PlaintextDiffgramHashMismatch, commit.Reason);
    }

    /// <summary>Subscriber refuses protected envelopes addressed to a different subscriber party.</summary>
    [Fact]
    public async Task CommitDiffgram_WithProtectedEnvelopeForWrongSubscriber_Rejects()
    {
        using var keyPair = EncryptionKeyPair.Create();
        using var keyServer = CreateKeyServer();
        var protector = new TransactionDiffgramProtector();
        await RegisterStandardPartiesAsync(keyServer, keyPair).ConfigureAwait(true);
        var wrongPartyKey = new PartyKeyDescriptor
        {
            PartyId = "subscriber-2",
            KeyId = SubscriberEncryptionKeyId,
            Purpose = "encryption",
            Algorithm = "ECDH-P256-HKDF-SHA256-AES-256-GCM",
            PublicKeyPem = keyPair.PublicKeyPem,
            Status = "active",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        var protectedDiffgram = protector.Protect(CreatePlaintextDiffgram("txn-encrypted-wrong-subscriber"), wrongPartyKey);
        var manifest = await SignAsync(
            keyServer,
            "txn-encrypted-wrong-subscriber",
            sequence: 502,
            nonce: "nonce-encrypted-wrong-subscriber",
            protectedDiffgram.PlaintextSha256,
            protectedDiffgram.EncryptedBodySha256).ConfigureAwait(true);
        using var subscriber = CreateSubscriber(keyServer, keyPair.PrivateKeyPem, protector);

        var commit = await subscriber.CommitDiffgramAsync(CreateCommitRequest(manifest, protectedDiffgram))
            .ConfigureAwait(true);

        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.WrongSubscriber, commit.Reason);
    }

    /// <summary>Subscriber key-ring rotation keeps old and current protected envelopes decryptable.</summary>
    [Fact]
    public async Task CommitDiffgram_WithSubscriberEncryptionKeyRing_DecryptsOldAndRotatedKeys()
    {
        using var firstKeyPair = EncryptionKeyPair.Create();
        using var rotatedKeyPair = EncryptionKeyPair.Create();
        using var keyServer = CreateKeyServer();
        var protector = new TransactionDiffgramProtector();
        await RegisterStandardPartiesAsync(keyServer, firstKeyPair).ConfigureAwait(true);
        var firstEncryptionKey = await keyServer.GetPartyKeyAsync(SubscriberPartyId, SubscriberEncryptionKeyId)
            .ConfigureAwait(true);
        Assert.NotNull(firstEncryptionKey);
        var firstProtectedDiffgram = protector.Protect(CreatePlaintextDiffgram("txn-key-ring-first"), firstEncryptionKey);
        var firstManifest = await SignAsync(
            keyServer,
            "txn-key-ring-first",
            sequence: 503,
            nonce: "nonce-key-ring-first",
            firstProtectedDiffgram.PlaintextSha256,
            firstProtectedDiffgram.EncryptedBodySha256,
            SubscriberEncryptionKeyId).ConfigureAwait(true);

        await RegisterSubscriberAsync(keyServer, rotatedKeyPair, RotatedSubscriberEncryptionKeyId).ConfigureAwait(true);
        var rotatedEncryptionKey = await keyServer.GetPartyKeyAsync(SubscriberPartyId, RotatedSubscriberEncryptionKeyId)
            .ConfigureAwait(true);
        Assert.NotNull(rotatedEncryptionKey);
        var rotatedProtectedDiffgram = protector.Protect(CreatePlaintextDiffgram("txn-key-ring-rotated"), rotatedEncryptionKey);
        var rotatedManifest = await SignAsync(
            keyServer,
            "txn-key-ring-rotated",
            sequence: 504,
            nonce: "nonce-key-ring-rotated",
            rotatedProtectedDiffgram.PlaintextSha256,
            rotatedProtectedDiffgram.EncryptedBodySha256,
            RotatedSubscriberEncryptionKeyId).ConfigureAwait(true);
        using var subscriber = CreateSubscriber(
            keyServer,
            new[]
            {
                new SubscriberEncryptionKeyMaterial
                {
                    KeyId = SubscriberEncryptionKeyId,
                    PrivateKeyPem = firstKeyPair.PrivateKeyPem,
                },
                new SubscriberEncryptionKeyMaterial
                {
                    KeyId = RotatedSubscriberEncryptionKeyId,
                    PrivateKeyPem = rotatedKeyPair.PrivateKeyPem,
                },
            },
            protector);

        var firstCommit = await subscriber.CommitDiffgramAsync(CreateCommitRequest(firstManifest, firstProtectedDiffgram))
            .ConfigureAwait(true);
        var rotatedCommit = await subscriber.CommitDiffgramAsync(CreateCommitRequest(rotatedManifest, rotatedProtectedDiffgram))
            .ConfigureAwait(true);

        Assert.Equal("committed", firstCommit.Status);
        Assert.Equal(TransactionFailureReason.None, firstCommit.Reason);
        Assert.Equal("committed", rotatedCommit.Status);
        Assert.Equal(TransactionFailureReason.None, rotatedCommit.Reason);
    }

    private static InMemoryKeyServerService CreateKeyServer()
        => new(
            new FixedOptionsMonitor<KeyServerOptions>(new KeyServerOptions()),
            new TransactionManifestCanonicalizer());

    private static InMemorySubscriberCommitService CreateSubscriber(
        IKeyServerManifestService keyServer,
        string privateKeyPem,
        ITransactionDiffgramProtector protector)
        => new(
            keyServer,
            new TransactionManifestCanonicalizer(),
            new FixedOptionsMonitor<SubscriberOptions>(new SubscriberOptions
            {
                PartyId = SubscriberPartyId,
                EncryptionKeyId = SubscriberEncryptionKeyId,
                EncryptionPrivateKeyPem = privateKeyPem,
                RequireEncryptedDiffgrams = true,
            }),
            protector);

    private static InMemorySubscriberCommitService CreateSubscriber(
        IKeyServerManifestService keyServer,
        IReadOnlyCollection<SubscriberEncryptionKeyMaterial> encryptionKeys,
        ITransactionDiffgramProtector protector)
        => new(
            keyServer,
            new TransactionManifestCanonicalizer(),
            new FixedOptionsMonitor<SubscriberOptions>(new SubscriberOptions
            {
                PartyId = SubscriberPartyId,
                EncryptionKeys = encryptionKeys.ToList(),
                RequireEncryptedDiffgrams = true,
            }),
            protector);

    private static async Task RegisterStandardPartiesAsync(
        IKeyServerPartyRegistry registry,
        EncryptionKeyPair subscriberKey)
    {
        await registry.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = PublisherPartyId, Role = "publisher" })
            .ConfigureAwait(false);
        await registry.RegisterPartyAsync(new PartyRegistrationRequest
        {
            PartyId = SubscriberPartyId,
            Role = "subscriber",
            ActiveEncryptionKeyId = SubscriberEncryptionKeyId,
            EncryptionPublicKeyPem = subscriberKey.PublicKeyPem,
        }).ConfigureAwait(false);
    }

    private static Task<PartyRegistrationResponse> RegisterSubscriberAsync(
        IKeyServerPartyRegistry registry,
        EncryptionKeyPair subscriberKey,
        string encryptionKeyId)
        => registry.RegisterPartyAsync(new PartyRegistrationRequest
        {
            PartyId = SubscriberPartyId,
            Role = "subscriber",
            ActiveEncryptionKeyId = encryptionKeyId,
            EncryptionPublicKeyPem = subscriberKey.PublicKeyPem,
        });

    private static Task<TransactionManifestDto> SignAsync(
        IKeyServerManifestService keyServer,
        string transactionId,
        long sequence,
        string nonce,
        string plaintextSha256,
        string encryptedBodySha256)
        => SignAsync(
            keyServer,
            transactionId,
            sequence,
            nonce,
            plaintextSha256,
            encryptedBodySha256,
            SubscriberEncryptionKeyId);

    private static async Task<TransactionManifestDto> SignAsync(
        IKeyServerManifestService keyServer,
        string transactionId,
        long sequence,
        string nonce,
        string plaintextSha256,
        string encryptedBodySha256,
        string subscriberEncryptionKeyId)
    {
        var response = await keyServer.SignManifestAsync(new TransactionManifestSignRequest
        {
            TransactionId = transactionId,
            TurnId = "turn-encrypted-diffgram",
            PublisherPartyId = PublisherPartyId,
            SubscriberPartyId = SubscriberPartyId,
            SubscriberEncryptionKeyId = subscriberEncryptionKeyId,
            Sequence = sequence,
            Nonce = nonce,
            DiffgramSha256 = plaintextSha256,
            EncryptedBodySha256 = encryptedBodySha256,
        }).ConfigureAwait(false);

        Assert.True(response.Success);
        Assert.NotNull(response.Manifest);
        return response.Manifest;
    }

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

    private sealed class FixedOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly T _value;

        public FixedOptionsMonitor(T value)
        {
            _value = value;
        }

        public T CurrentValue => _value;

        public T Get(string? name) => _value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
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
}
