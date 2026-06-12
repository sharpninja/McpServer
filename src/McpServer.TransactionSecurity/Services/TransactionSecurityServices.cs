using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using Microsoft.Extensions.Options;

namespace McpServer.TransactionSecurity.Services;

/// <summary>FR-MCP-118: Registry for keyserver parties and public key descriptors.</summary>
public interface IKeyServerPartyRegistry
{
    /// <summary>Registers or updates a transaction-security party.</summary>
    /// <param name="request">Party registration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registered party response.</returns>
    Task<PartyRegistrationResponse> RegisterPartyAsync(
        PartyRegistrationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a public key descriptor for a registered party.</summary>
    /// <param name="partyId">Party identifier.</param>
    /// <param name="keyId">Key identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Public key descriptor, or <see langword="null"/> when not found.</returns>
    Task<PartyKeyDescriptor?> GetPartyKeyAsync(
        string partyId,
        string keyId,
        CancellationToken cancellationToken = default);
}

/// <summary>FR-MCP-120 and FR-MCP-121: Keyserver manifest signing and verification service.</summary>
public interface IKeyServerManifestService
{
    /// <summary>Signs a canonical transaction manifest.</summary>
    /// <param name="request">Manifest signing request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Signed manifest response.</returns>
    Task<TransactionManifestSignResponse> SignManifestAsync(
        TransactionManifestSignRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Verifies a signed transaction manifest.</summary>
    /// <param name="request">Manifest verification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification response.</returns>
    Task<TransactionManifestVerifyResponse> VerifyManifestAsync(
        TransactionManifestVerifyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Gets persisted public trace metadata for a signed manifest.</summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Manifest trace record, or <see langword="null"/> when unknown.</returns>
    Task<TransactionManifestTraceRecord?> GetManifestAsync(
        string transactionId,
        CancellationToken cancellationToken = default);
}

/// <summary>TR-MCP-CRYPTO-001: Canonicalizes manifests for signing and hashing.</summary>
public interface ITransactionManifestCanonicalizer
{
    /// <summary>Builds the canonical unsigned manifest payload.</summary>
    /// <param name="manifest">Manifest to canonicalize.</param>
    /// <returns>Canonical UTF-8 JSON string.</returns>
    string CanonicalizeUnsigned(TransactionManifestDto manifest);

    /// <summary>Computes the SHA-256 digest of the canonical unsigned manifest.</summary>
    /// <param name="manifest">Manifest to hash.</param>
    /// <returns>Lowercase hexadecimal SHA-256 digest.</returns>
    string ComputeManifestHash(TransactionManifestDto manifest);
}

/// <summary>FR-MCP-123 and FR-MCP-124: Subscriber transaction commit/status/abort service.</summary>
public interface ISubscriberCommitService
{
    /// <summary>Commits a signed encrypted diffgram.</summary>
    /// <param name="request">Commit request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Commit response.</returns>
    Task<DiffgramCommitResponse> CommitDiffgramAsync(
        DiffgramCommitRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the current status for a transaction.</summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status response, or <see langword="null"/> when the transaction is unknown.</returns>
    Task<TransactionStatusResponse?> GetTransactionStatusAsync(
        string transactionId,
        CancellationToken cancellationToken = default);

    /// <summary>Aborts a transaction before commit.</summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="request">Abort request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Abort response.</returns>
    Task<TransactionAbortResponse> AbortTransactionAsync(
        string transactionId,
        TransactionAbortRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>TR-MCP-CRYPTO-001: Fixed-order JSON canonicalizer for transaction manifests.</summary>
public sealed class TransactionManifestCanonicalizer : ITransactionManifestCanonicalizer
{
    /// <inheritdoc />
    public string CanonicalizeUnsigned(TransactionManifestDto manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            WriteString(writer, "transactionId", manifest.TransactionId);
            WriteStringOrNull(writer, "turnId", manifest.TurnId);
            WriteString(writer, "publisherPartyId", manifest.PublisherPartyId);
            WriteString(writer, "subscriberPartyId", manifest.SubscriberPartyId);
            WriteStringOrNull(writer, "publisherSigningKeyId", manifest.PublisherSigningKeyId);
            WriteStringOrNull(writer, "subscriberEncryptionKeyId", manifest.SubscriberEncryptionKeyId);
            writer.WriteNumber("sequence", manifest.Sequence);
            WriteString(writer, "nonce", manifest.Nonce);
            WriteString(writer, "issuedAtUtc", FormatTimestamp(manifest.IssuedAtUtc));
            WriteString(writer, "expiresAtUtc", FormatTimestamp(manifest.ExpiresAtUtc));
            WriteString(writer, "diffgramSha256", manifest.DiffgramSha256);
            WriteString(writer, "encryptedBodySha256", manifest.EncryptedBodySha256);
            writer.WriteStartObject("algorithms");
            WriteString(writer, "signature", manifest.Algorithms.Signature);
            WriteString(writer, "encryption", manifest.Algorithms.Encryption);
            WriteString(writer, "canonicalization", manifest.Algorithms.Canonicalization);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <inheritdoc />
    public string ComputeManifestHash(TransactionManifestDto manifest)
        => HashHex(Encoding.UTF8.GetBytes(CanonicalizeUnsigned(manifest)));

    private static void WriteString(Utf8JsonWriter writer, string name, string value)
        => writer.WriteString(name, value ?? string.Empty);

    private static void WriteStringOrNull(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
            writer.WriteNull(name);
        else
            writer.WriteString(name, value);
    }

    private static string FormatTimestamp(DateTimeOffset timestamp)
        => timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    private static string HashHex(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}

/// <summary>FR-MCP-118 through FR-MCP-121: In-memory keyserver implementation.</summary>
public sealed class InMemoryKeyServerService : IKeyServerPartyRegistry, IKeyServerManifestService, IDisposable
{
    private const string SignatureAlgorithm = "ECDSA-P256-SHA256";
    private const string EncryptionAlgorithm = "ECDH-P256-HKDF-SHA256-AES-256-GCM";
    private const string Canonicalization = "transaction-manifest-v1";
    private const string ManifestSigningReplayScope = "sign";
    private const string ManifestVerificationReplayScope = "verify";

    private readonly object _gate = new();
    private readonly IOptionsMonitor<KeyServerOptions> _options;
    private readonly ITransactionManifestCanonicalizer _canonicalizer;
    private readonly IKeyServerStateStore _stateStore;
    private readonly Dictionary<string, Dictionary<string, ECDsa>> _signingKeys = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Initializes a new instance of the <see cref="InMemoryKeyServerService"/> class.</summary>
    /// <param name="options">Keyserver options.</param>
    /// <param name="canonicalizer">Manifest canonicalizer.</param>
    public InMemoryKeyServerService(
        IOptionsMonitor<KeyServerOptions> options,
        ITransactionManifestCanonicalizer canonicalizer)
    {
        _options = options;
        _canonicalizer = canonicalizer;
        _stateStore = CreateKeyServerStateStore(options.CurrentValue);
    }

    /// <inheritdoc />
    public async Task<PartyRegistrationResponse> RegisterPartyAsync(
        PartyRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PartyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Role);

        var now = DateTimeOffset.UtcNow;
        var partyId = request.PartyId.Trim();
        var signingKeyId = NormalizeKeyId(request.ActiveSigningKeyId, partyId, "signing");
        var encryptionKeyId = NormalizeKeyId(request.ActiveEncryptionKeyId, partyId, "encryption");
        var status = NormalizeStatus(request.Status);
        var signingKey = CreateSigningKey(request.SigningPublicKeyPem, request.SigningPrivateKeyPem);
        var encryptionPublicKeyPem = string.IsNullOrWhiteSpace(request.EncryptionPublicKeyPem)
            ? CreateEncryptionPublicKeyPem()
            : request.EncryptionPublicKeyPem.Trim();

        var existing = await _stateStore.GetPartyAsync(partyId, cancellationToken).ConfigureAwait(false);
        var createdAt = existing?.Party.CreatedAtUtc ?? now;
        var state = new PartyRegistrationResponse
        {
            PartyId = partyId,
            Role = request.Role.Trim(),
            ActiveSigningKeyId = signingKeyId,
            ActiveEncryptionKeyId = encryptionKeyId,
            Status = status,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = existing is null ? null : now,
        };
        var keys = new[]
        {
            new PartyKeyDescriptor
            {
                PartyId = partyId,
                KeyId = signingKeyId,
                Purpose = "signing",
                Algorithm = SignatureAlgorithm,
                PublicKeyPem = signingKey.PublicKeyPem,
                Status = status,
                CreatedAtUtc = now,
            },
            new PartyKeyDescriptor
            {
                PartyId = partyId,
                KeyId = encryptionKeyId,
                Purpose = "encryption",
                Algorithm = EncryptionAlgorithm,
                PublicKeyPem = encryptionPublicKeyPem,
                Status = status,
                CreatedAtUtc = now,
            },
        };

        Dictionary<string, ECDsa>? staleSigningKeys = null;
        lock (_gate)
        {
            if (_signingKeys.TryGetValue(partyId, out var existingSigningKeys))
                staleSigningKeys = existingSigningKeys;

            var activeSigningKeys = new Dictionary<string, ECDsa>(StringComparer.OrdinalIgnoreCase);
            if (signingKey.PrivateKey is not null)
                activeSigningKeys[signingKeyId] = signingKey.PrivateKey;
            _signingKeys[partyId] = activeSigningKeys;
        }

        DisposeKeys(staleSigningKeys);
        await _stateStore.SavePartyAsync(state, keys, cancellationToken).ConfigureAwait(false);
        await RecordKeyServerAuditAsync(
            "keyserver.party.registered",
            null,
            TransactionFailureReason.None,
            partyId,
            cancellationToken).ConfigureAwait(false);
        return Clone(state);
    }

    /// <inheritdoc />
    public Task<PartyKeyDescriptor?> GetPartyKeyAsync(
        string partyId,
        string keyId,
        CancellationToken cancellationToken = default)
        => _stateStore.GetPartyKeyAsync(partyId.Trim(), keyId.Trim(), cancellationToken);

    /// <inheritdoc />
    public async Task<TransactionManifestSignResponse> SignManifestAsync(
        TransactionManifestSignRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var (publisher, publisherFailure) = await GetActivePartyAsync(request.PublisherPartyId, cancellationToken)
            .ConfigureAwait(false);
        if (publisher is null)
            return await FailedSignAsync(publisherFailure, request.TransactionId, cancellationToken).ConfigureAwait(false);
        var (subscriber, subscriberFailure) = await GetActivePartyAsync(request.SubscriberPartyId, cancellationToken)
            .ConfigureAwait(false);
        if (subscriber is null)
            return await FailedSignAsync(subscriberFailure, request.TransactionId, cancellationToken).ConfigureAwait(false);

        var signingKeyId = string.IsNullOrWhiteSpace(request.PublisherSigningKeyId)
            ? publisher.Party.ActiveSigningKeyId
            : request.PublisherSigningKeyId.Trim();
        var encryptionKeyId = string.IsNullOrWhiteSpace(request.SubscriberEncryptionKeyId)
            ? subscriber.Party.ActiveEncryptionKeyId
            : request.SubscriberEncryptionKeyId.Trim();

        var signingDescriptor = string.IsNullOrWhiteSpace(signingKeyId)
            ? null
            : publisher.Keys.FirstOrDefault(
                key => string.Equals(key.KeyId, signingKeyId, StringComparison.OrdinalIgnoreCase));
        ECDsa? privateKey = null;
        if (!string.IsNullOrWhiteSpace(signingKeyId))
        {
            lock (_gate)
            {
                if (_signingKeys.TryGetValue(publisher.Party.PartyId, out var keySet))
                    keySet.TryGetValue(signingKeyId, out privateKey);
            }
        }

        if (signingDescriptor is null || privateKey is null)
            return await FailedSignAsync(TransactionFailureReason.UnknownKey, request.TransactionId, cancellationToken).ConfigureAwait(false);
        if (!IsActive(signingDescriptor.Status))
            return await FailedSignAsync(TransactionFailureReason.DisabledKey, request.TransactionId, cancellationToken).ConfigureAwait(false);

        var encryptionDescriptor = string.IsNullOrWhiteSpace(encryptionKeyId)
            ? null
            : subscriber.Keys.FirstOrDefault(
                key => string.Equals(key.KeyId, encryptionKeyId, StringComparison.OrdinalIgnoreCase));
        if (encryptionDescriptor is null)
            return await FailedSignAsync(TransactionFailureReason.UnknownKey, request.TransactionId, cancellationToken).ConfigureAwait(false);
        if (!IsActive(encryptionDescriptor.Status))
            return await FailedSignAsync(TransactionFailureReason.DisabledKey, request.TransactionId, cancellationToken).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var issuedAt = request.IssuedAtUtc ?? now;
        var ttl = Math.Max(1, _options.CurrentValue.ManifestTtlSeconds);
        var expiresAt = request.ExpiresAtUtc ?? issuedAt.AddSeconds(ttl);
        var algorithms = NormalizeAlgorithms(request.Algorithms);
        var manifest = new TransactionManifestDto
        {
            TransactionId = request.TransactionId.Trim(),
            TurnId = NormalizeOptional(request.TurnId),
            PublisherPartyId = publisher.Party.PartyId,
            SubscriberPartyId = subscriber.Party.PartyId,
            PublisherSigningKeyId = signingKeyId,
            SubscriberEncryptionKeyId = encryptionKeyId,
            Sequence = request.Sequence,
            Nonce = request.Nonce.Trim(),
            IssuedAtUtc = issuedAt,
            ExpiresAtUtc = expiresAt,
            DiffgramSha256 = request.DiffgramSha256.Trim(),
            EncryptedBodySha256 = request.EncryptedBodySha256.Trim(),
            Algorithms = algorithms,
        };

        var payload = Encoding.UTF8.GetBytes(_canonicalizer.CanonicalizeUnsigned(manifest));
        byte[] signature;
        lock (_gate)
        {
            signature = privateKey.SignData(payload, HashAlgorithmName.SHA256);
        }

        manifest.Signature = new TransactionManifestSignatureDto
        {
            Algorithm = algorithms.Signature,
            KeyId = signingKeyId!,
            Value = Convert.ToBase64String(signature),
            SignedAtUtc = now,
        };

        var pairKey = BuildPairKey(manifest.PublisherPartyId, manifest.SubscriberPartyId);
        var nonceKey = BuildNonceKey(pairKey, manifest.Nonce);
        var replayReservation = await _stateStore.TryReserveManifestReplayAsync(
            ManifestSigningReplayScope,
            pairKey,
            manifest.Sequence,
            nonceKey,
            manifest.TransactionId,
            cancellationToken).ConfigureAwait(false);
        if (replayReservation != TransactionFailureReason.None)
            return await FailedSignAsync(replayReservation, manifest.TransactionId, cancellationToken).ConfigureAwait(false);

        await _stateStore.SaveManifestAsync(
            BuildManifestTraceRecord(manifest, _canonicalizer.ComputeManifestHash(manifest), now),
            cancellationToken).ConfigureAwait(false);

        await RecordKeyServerAuditAsync(
            "keyserver.manifest.signed",
            manifest.TransactionId,
            TransactionFailureReason.None,
            manifest.PublisherPartyId,
            cancellationToken).ConfigureAwait(false);
        return new TransactionManifestSignResponse
        {
            Success = true,
            Reason = TransactionFailureReason.None,
            Manifest = manifest,
        };
    }

    /// <inheritdoc />
    public Task<TransactionManifestTraceRecord?> GetManifestAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
        => string.IsNullOrWhiteSpace(transactionId)
            ? Task.FromResult<TransactionManifestTraceRecord?>(null)
            : _stateStore.GetManifestAsync(transactionId.Trim(), cancellationToken);

    /// <inheritdoc />
    public async Task<TransactionManifestVerifyResponse> VerifyManifestAsync(
        TransactionManifestVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = request.Manifest ?? new TransactionManifestDto();
        var manifestHash = _canonicalizer.ComputeManifestHash(manifest);
        var now = DateTimeOffset.UtcNow;
        var skew = TimeSpan.FromSeconds(Math.Max(0, _options.CurrentValue.MaxClockSkewSeconds));

        if (!string.IsNullOrWhiteSpace(request.ExpectedSubscriberPartyId) &&
            !string.Equals(manifest.SubscriberPartyId, request.ExpectedSubscriberPartyId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return await InvalidAsync(TransactionFailureReason.WrongSubscriber, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);
        }

        if (manifest.ExpiresAtUtc < now)
            return await InvalidAsync(TransactionFailureReason.ExpiredManifest, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);
        if (manifest.IssuedAtUtc > now.Add(skew))
            return await InvalidAsync(TransactionFailureReason.FutureManifest, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);
        var (publisher, publisherFailure) = await GetActivePartyAsync(manifest.PublisherPartyId, cancellationToken)
            .ConfigureAwait(false);
        if (publisher is null)
            return await InvalidAsync(publisherFailure, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);
        var (subscriber, subscriberFailure) = await GetActivePartyAsync(manifest.SubscriberPartyId, cancellationToken)
            .ConfigureAwait(false);
        if (subscriber is null)
            return await InvalidAsync(subscriberFailure, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);
        var encryptionKey = string.IsNullOrWhiteSpace(manifest.SubscriberEncryptionKeyId)
            ? null
            : subscriber.Keys.FirstOrDefault(
                candidate => string.Equals(candidate.KeyId, manifest.SubscriberEncryptionKeyId, StringComparison.OrdinalIgnoreCase));
        if (encryptionKey is null || !string.Equals(encryptionKey.Purpose, "encryption", StringComparison.OrdinalIgnoreCase))
            return await InvalidAsync(TransactionFailureReason.UnknownKey, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);
        if (!IsActive(encryptionKey.Status))
            return await InvalidAsync(TransactionFailureReason.DisabledKey, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);
        if (manifest.Signature is null ||
            string.IsNullOrWhiteSpace(manifest.Signature.Value) ||
            string.IsNullOrWhiteSpace(manifest.Signature.KeyId))
        {
            return await InvalidAsync(TransactionFailureReason.MalformedSignature, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);
        }

        var key = publisher.Keys.FirstOrDefault(
            candidate => string.Equals(candidate.KeyId, manifest.Signature.KeyId, StringComparison.OrdinalIgnoreCase));
        if (key is null || !string.Equals(key.Purpose, "signing", StringComparison.OrdinalIgnoreCase))
            return await InvalidAsync(TransactionFailureReason.UnknownKey, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);
        if (!IsActive(key.Status))
            return await InvalidAsync(TransactionFailureReason.DisabledKey, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);
        if (!string.Equals(manifest.Signature.Algorithm, SignatureAlgorithm, StringComparison.Ordinal))
            return await InvalidAsync(TransactionFailureReason.MalformedSignature, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(manifest.Signature.Value);
        }
        catch (FormatException)
        {
            return await InvalidAsync(TransactionFailureReason.MalformedSignature, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);
        }

        var payload = Encoding.UTF8.GetBytes(_canonicalizer.CanonicalizeUnsigned(manifest));
        using var publicKey = ECDsa.Create();
        publicKey.ImportFromPem(key.PublicKeyPem);
        var verified = publicKey.VerifyData(payload, signature, HashAlgorithmName.SHA256);
        if (!verified)
            return await InvalidAsync(TransactionFailureReason.ManifestSignatureMismatch, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);

        var pairKey = BuildPairKey(manifest.PublisherPartyId, manifest.SubscriberPartyId);
        var nonceKey = BuildNonceKey(pairKey, manifest.Nonce);
        var replayReservation = await _stateStore.TryReserveManifestReplayAsync(
            ManifestVerificationReplayScope,
            pairKey,
            manifest.Sequence,
            nonceKey,
            manifest.TransactionId,
            cancellationToken).ConfigureAwait(false);
        if (replayReservation != TransactionFailureReason.None)
            return await InvalidAsync(replayReservation, manifest.TransactionId, manifestHash, cancellationToken)
                .ConfigureAwait(false);

        await RecordKeyServerAuditAsync(
            "keyserver.manifest.verified",
            manifest.TransactionId,
            TransactionFailureReason.None,
            manifest.PublisherPartyId,
            cancellationToken).ConfigureAwait(false);
        return new TransactionManifestVerifyResponse
        {
            IsValid = true,
            Reason = TransactionFailureReason.None,
            ManifestHashSha256 = manifestHash,
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var party in _signingKeys.Values)
                DisposeKeys(party);
            _signingKeys.Clear();
        }

        _stateStore.Dispose();
    }

    private static TransactionManifestSignResponse FailedSign(TransactionFailureReason reason)
        => new() { Success = false, Reason = reason };

    private static TransactionManifestVerifyResponse Invalid(TransactionFailureReason reason, string manifestHash)
        => new() { IsValid = false, Reason = reason, ManifestHashSha256 = manifestHash };

    private async Task<TransactionManifestSignResponse> FailedSignAsync(
        TransactionFailureReason reason,
        string? transactionId,
        CancellationToken cancellationToken)
    {
        await RecordKeyServerAuditAsync(
            "keyserver.manifest.sign_rejected",
            transactionId,
            reason,
            null,
            cancellationToken).ConfigureAwait(false);
        return FailedSign(reason);
    }

    private async Task<TransactionManifestVerifyResponse> InvalidAsync(
        TransactionFailureReason reason,
        string? transactionId,
        string manifestHash,
        CancellationToken cancellationToken)
    {
        await RecordKeyServerAuditAsync(
            "keyserver.manifest.verify_rejected",
            transactionId,
            reason,
            null,
            cancellationToken).ConfigureAwait(false);
        return Invalid(reason, manifestHash);
    }

    private Task RecordKeyServerAuditAsync(
        string eventName,
        string? transactionId,
        TransactionFailureReason reason,
        string? details,
        CancellationToken cancellationToken)
        => _options.CurrentValue.AuditEnabled
            ? _stateStore.RecordAuditAsync(eventName, transactionId, reason, details, cancellationToken)
            : Task.CompletedTask;

    private static IKeyServerStateStore CreateKeyServerStateStore(KeyServerOptions options)
        => string.IsNullOrWhiteSpace(options.DatabasePath)
            ? new InMemoryKeyServerStateStore()
            : new SqliteTransactionSecurityStateStore(options.DatabasePath);

    private async Task<(KeyServerPartyState? Party, TransactionFailureReason FailureReason)> GetActivePartyAsync(
        string partyId,
        CancellationToken cancellationToken)
    {
        var party = await _stateStore.GetPartyAsync(partyId.Trim(), cancellationToken).ConfigureAwait(false);
        if (party is null)
            return (null, TransactionFailureReason.UnknownParty);

        return IsActive(party.Party.Status)
            ? (party, TransactionFailureReason.None)
            : (null, TransactionFailureReason.DisabledParty);
    }

    private static bool IsActive(string? status)
        => string.IsNullOrWhiteSpace(status) || string.Equals(status, "active", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeKeyId(string? keyId, string partyId, string purpose)
        => string.IsNullOrWhiteSpace(keyId)
            ? $"{partyId}:{purpose}:1"
            : keyId.Trim();

    private static string NormalizeStatus(string? status)
        => string.IsNullOrWhiteSpace(status) ? "active" : status.Trim();

    private static TransactionManifestTraceRecord BuildManifestTraceRecord(
        TransactionManifestDto manifest,
        string manifestHash,
        DateTimeOffset createdAtUtc)
        => new()
        {
            TransactionId = manifest.TransactionId,
            TurnId = manifest.TurnId,
            PublisherPartyId = manifest.PublisherPartyId,
            PublisherSigningKeyId = manifest.PublisherSigningKeyId,
            SubscriberPartyId = manifest.SubscriberPartyId,
            SubscriberEncryptionKeyId = manifest.SubscriberEncryptionKeyId,
            Sequence = manifest.Sequence,
            Nonce = manifest.Nonce,
            IssuedAtUtc = manifest.IssuedAtUtc,
            ExpiresAtUtc = manifest.ExpiresAtUtc,
            DiffgramSha256 = manifest.DiffgramSha256,
            EncryptedBodySha256 = manifest.EncryptedBodySha256,
            SignatureAlgorithm = manifest.Signature?.Algorithm ?? string.Empty,
            EncryptionAlgorithm = manifest.Algorithms.Encryption,
            CanonicalizationProfile = manifest.Algorithms.Canonicalization,
            SignatureKeyId = manifest.Signature?.KeyId ?? string.Empty,
            SignatureValue = manifest.Signature?.Value ?? string.Empty,
            SignedAtUtc = manifest.Signature?.SignedAtUtc ?? createdAtUtc,
            ManifestHashSha256 = manifestHash,
            Status = "signed",
            CreatedAtUtc = createdAtUtc,
        };

    private static string BuildPairKey(string publisherPartyId, string subscriberPartyId)
        => $"{publisherPartyId.Trim()}\n{subscriberPartyId.Trim()}";

    private static string BuildNonceKey(string pairKey, string nonce)
        => $"{pairKey}\n{nonce.Trim()}";

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TransactionManifestAlgorithms NormalizeAlgorithms(TransactionManifestAlgorithms? algorithms)
        => new()
        {
            Signature = string.IsNullOrWhiteSpace(algorithms?.Signature) ? SignatureAlgorithm : algorithms.Signature.Trim(),
            Encryption = string.IsNullOrWhiteSpace(algorithms?.Encryption) ? EncryptionAlgorithm : algorithms.Encryption.Trim(),
            Canonicalization = string.IsNullOrWhiteSpace(algorithms?.Canonicalization) ? Canonicalization : algorithms.Canonicalization.Trim(),
        };

    private static PartyRegistrationResponse Clone(PartyRegistrationResponse response)
        => new()
        {
            PartyId = response.PartyId,
            Role = response.Role,
            ActiveSigningKeyId = response.ActiveSigningKeyId,
            ActiveEncryptionKeyId = response.ActiveEncryptionKeyId,
            Status = response.Status,
            CreatedAtUtc = response.CreatedAtUtc,
            UpdatedAtUtc = response.UpdatedAtUtc,
        };

    private static PartyKeyDescriptor Clone(PartyKeyDescriptor descriptor)
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

    private static GeneratedSigningKey CreateSigningKey(string? suppliedPublicKeyPem, string? suppliedPrivateKeyPem)
    {
        if (!string.IsNullOrWhiteSpace(suppliedPrivateKeyPem))
        {
            var importedPrivateKey = ECDsa.Create();
            try
            {
                importedPrivateKey.ImportFromPem(suppliedPrivateKeyPem.Trim());
                var publicKeyPem = importedPrivateKey.ExportSubjectPublicKeyInfoPem();
                if (!string.IsNullOrWhiteSpace(suppliedPublicKeyPem) &&
                    !PublicKeysMatch(suppliedPublicKeyPem, publicKeyPem))
                    throw new ArgumentException("Signing public key does not match the supplied signing private key.", nameof(suppliedPublicKeyPem));

                return new GeneratedSigningKey(publicKeyPem, importedPrivateKey);
            }
            catch
            {
                importedPrivateKey.Dispose();
                throw;
            }
        }

        if (!string.IsNullOrWhiteSpace(suppliedPublicKeyPem))
            return new GeneratedSigningKey(suppliedPublicKeyPem.Trim(), null);

        var generatedPrivateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new GeneratedSigningKey(generatedPrivateKey.ExportSubjectPublicKeyInfoPem(), generatedPrivateKey);
    }

    private static bool PublicKeysMatch(string suppliedPublicKeyPem, string derivedPublicKeyPem)
    {
        using var suppliedPublicKey = ECDsa.Create();
        suppliedPublicKey.ImportFromPem(suppliedPublicKeyPem.Trim());
        using var derivedPublicKey = ECDsa.Create();
        derivedPublicKey.ImportFromPem(derivedPublicKeyPem);
        return suppliedPublicKey.ExportSubjectPublicKeyInfo().AsSpan()
            .SequenceEqual(derivedPublicKey.ExportSubjectPublicKeyInfo());
    }

    private static string CreateEncryptionPublicKeyPem()
    {
        using var key = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        return key.ExportSubjectPublicKeyInfoPem();
    }

    private sealed record GeneratedSigningKey(string PublicKeyPem, ECDsa? PrivateKey);

    private static void DisposeKeys(Dictionary<string, ECDsa>? keys)
    {
        if (keys is null)
            return;

        foreach (var key in keys.Values)
            key.Dispose();
        keys.Clear();
    }
}

/// <summary>FR-MCP-123 and FR-MCP-124: Subscriber commit implementation.</summary>
public sealed class InMemorySubscriberCommitService : ISubscriberCommitService, IDisposable
{
    private readonly IKeyServerManifestService _keyServer;
    private readonly ITransactionManifestCanonicalizer _canonicalizer;
    private readonly IOptionsMonitor<SubscriberOptions> _options;
    private readonly ITransactionDiffgramProtector _diffgramProtector;
    private readonly ISubscriberStateStore _stateStore;

    /// <summary>Initializes a new instance of the <see cref="InMemorySubscriberCommitService"/> class.</summary>
    /// <param name="keyServer">Keyserver manifest verifier.</param>
    /// <param name="canonicalizer">Manifest canonicalizer.</param>
    /// <param name="options">Subscriber options.</param>
    /// <param name="diffgramProtector">Diffgram protector.</param>
    public InMemorySubscriberCommitService(
        IKeyServerManifestService keyServer,
        ITransactionManifestCanonicalizer canonicalizer,
        IOptionsMonitor<SubscriberOptions> options,
        ITransactionDiffgramProtector? diffgramProtector = null)
    {
        _keyServer = keyServer;
        _canonicalizer = canonicalizer;
        _options = options;
        _diffgramProtector = diffgramProtector ?? new TransactionDiffgramProtector();
        _stateStore = CreateSubscriberStateStore(options.CurrentValue);
    }

    /// <inheritdoc />
    public async Task<DiffgramCommitResponse> CommitDiffgramAsync(
        DiffgramCommitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = request.Manifest;
        var transactionId = manifest.TransactionId?.Trim() ?? string.Empty;
        var manifestHash = _canonicalizer.ComputeManifestHash(manifest);
        if (!TryComputeEncryptedBodySha256(request.EncryptedDiffgramBase64, out var actualEncryptedBodySha256))
        {
            return await RejectedAsync(
                transactionId,
                TransactionFailureReason.DecryptFailed,
                manifestHash,
                string.Empty,
                cancellationToken).ConfigureAwait(false);
        }

        var existing = await _stateStore.GetTransactionAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return await ExistingCommitResponseAsync(
                existing,
                manifestHash,
                request.EncryptedBodySha256,
                actualEncryptedBodySha256,
                cancellationToken).ConfigureAwait(false);
        }

        var expectedSubscriber = string.IsNullOrWhiteSpace(_options.CurrentValue.PartyId)
            ? manifest.SubscriberPartyId
            : _options.CurrentValue.PartyId!.Trim();
        var verify = await _keyServer.VerifyManifestAsync(
            new TransactionManifestVerifyRequest
            {
                Manifest = manifest,
                ExpectedSubscriberPartyId = expectedSubscriber,
            },
            cancellationToken).ConfigureAwait(false);
        if (!verify.IsValid)
        {
            return await RejectedAsync(
                transactionId,
                verify.Reason,
                manifestHash,
                actualEncryptedBodySha256,
                cancellationToken).ConfigureAwait(false);
        }

        if (!string.Equals(request.EncryptedBodySha256, manifest.EncryptedBodySha256, StringComparison.OrdinalIgnoreCase))
        {
            return await RejectedAsync(
                transactionId,
                TransactionFailureReason.EncryptedBodyHashMismatch,
                manifestHash,
                actualEncryptedBodySha256,
                cancellationToken).ConfigureAwait(false);
        }
        if (!string.Equals(actualEncryptedBodySha256, manifest.EncryptedBodySha256, StringComparison.OrdinalIgnoreCase))
        {
            return await RejectedAsync(
                transactionId,
                TransactionFailureReason.EncryptedBodyHashMismatch,
                manifestHash,
                actualEncryptedBodySha256,
                cancellationToken).ConfigureAwait(false);
        }

        var unprotect = _diffgramProtector.Unprotect(
            request.EncryptedDiffgramBase64,
            _options.CurrentValue,
            expectedSubscriber,
            manifest.SubscriberEncryptionKeyId);
        if (unprotect.IsProtectedEnvelope && !unprotect.Success)
        {
            return await RejectedAsync(
                transactionId,
                unprotect.Reason,
                manifestHash,
                actualEncryptedBodySha256,
                cancellationToken).ConfigureAwait(false);
        }

        if (!unprotect.IsProtectedEnvelope && _options.CurrentValue.RequireEncryptedDiffgrams)
        {
            return await RejectedAsync(
                transactionId,
                TransactionFailureReason.DecryptFailed,
                manifestHash,
                actualEncryptedBodySha256,
                cancellationToken).ConfigureAwait(false);
        }

        var actualPlaintextSha256 = unprotect.IsProtectedEnvelope
            ? unprotect.PlaintextSha256
            : request.DiffgramSha256;
        if (!string.Equals(request.DiffgramSha256, manifest.DiffgramSha256, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(actualPlaintextSha256, manifest.DiffgramSha256, StringComparison.OrdinalIgnoreCase))
        {
            return await RejectedAsync(
                transactionId,
                TransactionFailureReason.PlaintextDiffgramHashMismatch,
                manifestHash,
                actualEncryptedBodySha256,
                cancellationToken).ConfigureAwait(false);
        }

        var pairKey = $"{manifest.PublisherPartyId}\n{manifest.SubscriberPartyId}";
        var lastSequence = await _stateStore.GetLastSequenceAsync(pairKey, cancellationToken).ConfigureAwait(false);
        if (lastSequence is not null && manifest.Sequence <= lastSequence)
        {
            return await RejectedAsync(
                transactionId,
                TransactionFailureReason.StaleSequence,
                manifestHash,
                actualEncryptedBodySha256,
                cancellationToken).ConfigureAwait(false);
        }

        var nonceKey = $"{pairKey}\n{manifest.Nonce}";
        if (!await _stateStore.TryAddNonceAsync(nonceKey, transactionId, cancellationToken).ConfigureAwait(false))
        {
            return await RejectedAsync(
                transactionId,
                TransactionFailureReason.ReplayNonce,
                manifestHash,
                actualEncryptedBodySha256,
                cancellationToken).ConfigureAwait(false);
        }

        var now = DateTimeOffset.UtcNow;
        var record = new SubscriberTransactionState(
            transactionId,
            "committed",
            TransactionFailureReason.None,
            manifestHash,
            actualEncryptedBodySha256,
            $"diffgram-{transactionId}",
            now,
            null);
        if (!await _stateStore.TryAddTransactionAsync(record, cancellationToken).ConfigureAwait(false))
        {
            existing = await _stateStore.GetTransactionAsync(transactionId, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                return await ExistingCommitResponseAsync(
                    existing,
                    manifestHash,
                    request.EncryptedBodySha256,
                    actualEncryptedBodySha256,
                    cancellationToken).ConfigureAwait(false);
            }

            return await RejectedAsync(
                transactionId,
                TransactionFailureReason.DuplicateConflict,
                manifestHash,
                actualEncryptedBodySha256,
                cancellationToken).ConfigureAwait(false);
        }

        await _stateStore.SetLastSequenceAsync(pairKey, manifest.Sequence, cancellationToken).ConfigureAwait(false);
        await RecordSubscriberAuditAsync(
            "subscriber.transaction.committed",
            transactionId,
            TransactionFailureReason.None,
            record.DiffgramId,
            cancellationToken).ConfigureAwait(false);
        return new DiffgramCommitResponse
        {
            TransactionId = transactionId,
            Status = record.Status,
            Reason = record.Reason,
            DiffgramId = record.DiffgramId,
            CommittedAtUtc = record.CommittedAtUtc,
        };
    }

    /// <inheritdoc />
    public async Task<TransactionStatusResponse?> GetTransactionStatusAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = await _stateStore.GetTransactionAsync(transactionId.Trim(), cancellationToken).ConfigureAwait(false);
        return record is null ? null : ToStatus(record);
    }

    /// <inheritdoc />
    public async Task<TransactionAbortResponse> AbortTransactionAsync(
        string transactionId,
        TransactionAbortRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
        var reason = request.Reason == TransactionFailureReason.None
            ? TransactionFailureReason.Aborted
            : request.Reason;
        var now = DateTimeOffset.UtcNow;
        var record = new SubscriberTransactionState(
            transactionId.Trim(),
            "aborted",
            reason,
            string.Empty,
            string.Empty,
            null,
            null,
            now);
        var current = await _stateStore.AddOrKeepAbortAsync(record, cancellationToken).ConfigureAwait(false);
        var responseReason = current.Status == "committed" ? TransactionFailureReason.DuplicateConflict : reason;
        await RecordSubscriberAuditAsync(
            current.Status == "committed" ? "subscriber.transaction.abort_rejected" : "subscriber.transaction.aborted",
            current.TransactionId,
            responseReason,
            request.Actor,
            cancellationToken).ConfigureAwait(false);
        return new TransactionAbortResponse
        {
            TransactionId = current.TransactionId,
            Status = current.Status,
            Reason = responseReason,
            AbortedAtUtc = current.AbortedAtUtc ?? now,
        };
    }

    private async Task<DiffgramCommitResponse> ExistingCommitResponseAsync(
        SubscriberTransactionState existing,
        string manifestHash,
        string requestedEncryptedBodyHash,
        string actualEncryptedBodyHash,
        CancellationToken cancellationToken)
    {
        if (string.Equals(existing.Status, "aborted", StringComparison.OrdinalIgnoreCase))
        {
            await RecordSubscriberAuditAsync(
                "subscriber.transaction.commit_rejected",
                existing.TransactionId,
                TransactionFailureReason.Aborted,
                existing.Status,
                cancellationToken).ConfigureAwait(false);
            return Rejected(existing.TransactionId, TransactionFailureReason.Aborted);
        }

        if (string.Equals(existing.ManifestHashSha256, manifestHash, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.EncryptedBodySha256, requestedEncryptedBodyHash, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.EncryptedBodySha256, actualEncryptedBodyHash, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.Status, "committed", StringComparison.OrdinalIgnoreCase))
        {
            await RecordSubscriberAuditAsync(
                "subscriber.transaction.duplicate",
                existing.TransactionId,
                TransactionFailureReason.None,
                existing.DiffgramId,
                cancellationToken).ConfigureAwait(false);
            return new DiffgramCommitResponse
            {
                TransactionId = existing.TransactionId,
                Status = "committed",
                Reason = TransactionFailureReason.None,
                DiffgramId = existing.DiffgramId,
                CommittedAtUtc = existing.CommittedAtUtc,
            };
        }

        await RecordSubscriberAuditAsync(
            "subscriber.transaction.commit_rejected",
            existing.TransactionId,
            TransactionFailureReason.DuplicateConflict,
            existing.Status,
            cancellationToken).ConfigureAwait(false);
        return Rejected(existing.TransactionId, TransactionFailureReason.DuplicateConflict);
    }

    private async Task<DiffgramCommitResponse> RejectedAsync(
        string transactionId,
        TransactionFailureReason reason,
        string manifestHash,
        string encryptedBodySha256,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(transactionId))
        {
            var rejected = new SubscriberTransactionState(
                transactionId,
                "rejected",
                reason,
                manifestHash,
                encryptedBodySha256,
                null,
                null,
                null);
            await _stateStore.TryAddTransactionAsync(rejected, cancellationToken).ConfigureAwait(false);
        }

        await RecordSubscriberAuditAsync(
            "subscriber.transaction.rejected",
            transactionId,
            reason,
            null,
            cancellationToken).ConfigureAwait(false);
        return Rejected(transactionId, reason);
    }

    private static DiffgramCommitResponse Rejected(string transactionId, TransactionFailureReason reason)
        => new()
        {
            TransactionId = transactionId,
            Status = "rejected",
            Reason = reason,
        };

    private static TransactionStatusResponse ToStatus(SubscriberTransactionState record)
        => new()
        {
            TransactionId = record.TransactionId,
            Status = record.Status,
            Reason = record.Reason,
            CommittedAtUtc = record.CommittedAtUtc,
            AbortedAtUtc = record.AbortedAtUtc,
        };

    private static bool TryComputeEncryptedBodySha256(string value, out string hash)
    {
        hash = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        try
        {
            var bytes = Convert.FromBase64String(value);
            hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private Task RecordSubscriberAuditAsync(
        string eventName,
        string? transactionId,
        TransactionFailureReason reason,
        string? details,
        CancellationToken cancellationToken)
        => _options.CurrentValue.AuditEnabled
            ? _stateStore.RecordAuditAsync(eventName, transactionId, reason, details, cancellationToken)
            : Task.CompletedTask;

    private static ISubscriberStateStore CreateSubscriberStateStore(SubscriberOptions options)
        => string.IsNullOrWhiteSpace(options.DatabasePath)
            ? new InMemorySubscriberStateStore()
            : new SqliteTransactionSecurityStateStore(options.DatabasePath);

    /// <inheritdoc />
    public void Dispose()
        => _stateStore.Dispose();
}
