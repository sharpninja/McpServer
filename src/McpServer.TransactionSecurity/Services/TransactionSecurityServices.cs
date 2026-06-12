using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

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

    private readonly object _gate = new();
    private readonly IOptionsMonitor<KeyServerOptions> _options;
    private readonly ITransactionManifestCanonicalizer _canonicalizer;
    private readonly Dictionary<string, RegisteredParty> _parties = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Initializes a new instance of the <see cref="InMemoryKeyServerService"/> class.</summary>
    /// <param name="options">Keyserver options.</param>
    /// <param name="canonicalizer">Manifest canonicalizer.</param>
    public InMemoryKeyServerService(
        IOptionsMonitor<KeyServerOptions> options,
        ITransactionManifestCanonicalizer canonicalizer)
    {
        _options = options;
        _canonicalizer = canonicalizer;
    }

    /// <inheritdoc />
    public Task<PartyRegistrationResponse> RegisterPartyAsync(
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
        var signingKey = CreateSigningKey(request.SigningPublicKeyPem);
        var encryptionPublicKeyPem = string.IsNullOrWhiteSpace(request.EncryptionPublicKeyPem)
            ? CreateEncryptionPublicKeyPem()
            : request.EncryptionPublicKeyPem.Trim();

        lock (_gate)
        {
            var createdAt = _parties.TryGetValue(partyId, out var existing)
                ? existing.State.CreatedAtUtc
                : now;
            existing?.Dispose();

            var state = new PartyRegistrationResponse
            {
                PartyId = partyId,
                Role = request.Role.Trim(),
                ActiveSigningKeyId = signingKeyId,
                ActiveEncryptionKeyId = encryptionKeyId,
                Status = status,
                CreatedAtUtc = createdAt,
                UpdatedAtUtc = createdAt == now ? null : now,
            };
            var party = new RegisteredParty(state);
            party.Keys[signingKeyId] = new PartyKeyDescriptor
            {
                PartyId = partyId,
                KeyId = signingKeyId,
                Purpose = "signing",
                Algorithm = SignatureAlgorithm,
                PublicKeyPem = signingKey.PublicKeyPem,
                Status = status,
                CreatedAtUtc = now,
            };
            if (signingKey.PrivateKey is not null)
                party.SigningKeys[signingKeyId] = signingKey.PrivateKey;
            party.Keys[encryptionKeyId] = new PartyKeyDescriptor
            {
                PartyId = partyId,
                KeyId = encryptionKeyId,
                Purpose = "encryption",
                Algorithm = EncryptionAlgorithm,
                PublicKeyPem = encryptionPublicKeyPem,
                Status = status,
                CreatedAtUtc = now,
            };
            _parties[partyId] = party;
            return Task.FromResult(Clone(state));
        }
    }

    /// <inheritdoc />
    public Task<PartyKeyDescriptor?> GetPartyKeyAsync(
        string partyId,
        string keyId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(
                _parties.TryGetValue(partyId, out var party) && party.Keys.TryGetValue(keyId, out var key)
                    ? Clone(key)
                    : null);
        }
    }

    /// <inheritdoc />
    public Task<TransactionManifestSignResponse> SignManifestAsync(
        TransactionManifestSignRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!TryGetActiveParty(request.PublisherPartyId, out var publisher, out var publisherFailure))
                return Task.FromResult(FailedSign(publisherFailure));
            if (!TryGetActiveParty(request.SubscriberPartyId, out var subscriber, out var subscriberFailure))
                return Task.FromResult(FailedSign(subscriberFailure));

            var signingKeyId = string.IsNullOrWhiteSpace(request.PublisherSigningKeyId)
                ? publisher.State.ActiveSigningKeyId
                : request.PublisherSigningKeyId.Trim();
            var encryptionKeyId = string.IsNullOrWhiteSpace(request.SubscriberEncryptionKeyId)
                ? subscriber.State.ActiveEncryptionKeyId
                : request.SubscriberEncryptionKeyId.Trim();

            if (string.IsNullOrWhiteSpace(signingKeyId) ||
                !publisher.Keys.TryGetValue(signingKeyId, out var signingDescriptor) ||
                !publisher.SigningKeys.TryGetValue(signingKeyId, out var privateKey))
            {
                return Task.FromResult(FailedSign(TransactionFailureReason.UnknownKey));
            }

            if (!IsActive(signingDescriptor.Status))
                return Task.FromResult(FailedSign(TransactionFailureReason.DisabledKey));
            if (string.IsNullOrWhiteSpace(encryptionKeyId) ||
                !subscriber.Keys.TryGetValue(encryptionKeyId, out var encryptionDescriptor))
            {
                return Task.FromResult(FailedSign(TransactionFailureReason.UnknownKey));
            }

            if (!IsActive(encryptionDescriptor.Status))
                return Task.FromResult(FailedSign(TransactionFailureReason.DisabledKey));

            var now = DateTimeOffset.UtcNow;
            var issuedAt = request.IssuedAtUtc ?? now;
            var ttl = Math.Max(1, _options.CurrentValue.ManifestTtlSeconds);
            var expiresAt = request.ExpiresAtUtc ?? issuedAt.AddSeconds(ttl);
            var algorithms = NormalizeAlgorithms(request.Algorithms);
            var manifest = new TransactionManifestDto
            {
                TransactionId = request.TransactionId.Trim(),
                TurnId = NormalizeOptional(request.TurnId),
                PublisherPartyId = publisher.State.PartyId,
                SubscriberPartyId = subscriber.State.PartyId,
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
            manifest.Signature = new TransactionManifestSignatureDto
            {
                Algorithm = algorithms.Signature,
                KeyId = signingKeyId,
                Value = Convert.ToBase64String(privateKey.SignData(payload, HashAlgorithmName.SHA256)),
                SignedAtUtc = now,
            };

            return Task.FromResult(new TransactionManifestSignResponse
            {
                Success = true,
                Reason = TransactionFailureReason.None,
                Manifest = manifest,
            });
        }
    }

    /// <inheritdoc />
    public Task<TransactionManifestVerifyResponse> VerifyManifestAsync(
        TransactionManifestVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = request.Manifest ?? new TransactionManifestDto();

        lock (_gate)
        {
            var manifestHash = _canonicalizer.ComputeManifestHash(manifest);
            var now = DateTimeOffset.UtcNow;
            var skew = TimeSpan.FromSeconds(Math.Max(0, _options.CurrentValue.MaxClockSkewSeconds));

            if (!string.IsNullOrWhiteSpace(request.ExpectedSubscriberPartyId) &&
                !string.Equals(manifest.SubscriberPartyId, request.ExpectedSubscriberPartyId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Invalid(TransactionFailureReason.WrongSubscriber, manifestHash));
            }

            if (manifest.ExpiresAtUtc < now)
                return Task.FromResult(Invalid(TransactionFailureReason.ExpiredManifest, manifestHash));
            if (manifest.IssuedAtUtc > now.Add(skew))
                return Task.FromResult(Invalid(TransactionFailureReason.FutureManifest, manifestHash));
            if (!TryGetActiveParty(manifest.PublisherPartyId, out var publisher, out var publisherFailure))
                return Task.FromResult(Invalid(publisherFailure, manifestHash));
            if (manifest.Signature is null ||
                string.IsNullOrWhiteSpace(manifest.Signature.Value) ||
                string.IsNullOrWhiteSpace(manifest.Signature.KeyId))
            {
                return Task.FromResult(Invalid(TransactionFailureReason.MalformedSignature, manifestHash));
            }

            if (!publisher.Keys.TryGetValue(manifest.Signature.KeyId, out var key) || !string.Equals(key.Purpose, "signing", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(Invalid(TransactionFailureReason.UnknownKey, manifestHash));
            if (!IsActive(key.Status))
                return Task.FromResult(Invalid(TransactionFailureReason.DisabledKey, manifestHash));
            if (!string.Equals(manifest.Signature.Algorithm, SignatureAlgorithm, StringComparison.Ordinal))
                return Task.FromResult(Invalid(TransactionFailureReason.MalformedSignature, manifestHash));

            byte[] signature;
            try
            {
                signature = Convert.FromBase64String(manifest.Signature.Value);
            }
            catch (FormatException)
            {
                return Task.FromResult(Invalid(TransactionFailureReason.MalformedSignature, manifestHash));
            }

            var payload = Encoding.UTF8.GetBytes(_canonicalizer.CanonicalizeUnsigned(manifest));
            using var publicKey = ECDsa.Create();
            publicKey.ImportFromPem(key.PublicKeyPem);
            var verified = publicKey.VerifyData(payload, signature, HashAlgorithmName.SHA256);
            return Task.FromResult(verified
                ? new TransactionManifestVerifyResponse
                {
                    IsValid = true,
                    Reason = TransactionFailureReason.None,
                    ManifestHashSha256 = manifestHash,
                }
                : Invalid(TransactionFailureReason.ManifestSignatureMismatch, manifestHash));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var party in _parties.Values)
                party.Dispose();
            _parties.Clear();
        }
    }

    private static TransactionManifestSignResponse FailedSign(TransactionFailureReason reason)
        => new() { Success = false, Reason = reason };

    private static TransactionManifestVerifyResponse Invalid(TransactionFailureReason reason, string manifestHash)
        => new() { IsValid = false, Reason = reason, ManifestHashSha256 = manifestHash };

    private bool TryGetActiveParty(
        string partyId,
        out RegisteredParty party,
        out TransactionFailureReason failureReason)
    {
        if (!_parties.TryGetValue(partyId.Trim(), out party!))
        {
            failureReason = TransactionFailureReason.UnknownParty;
            return false;
        }

        if (!IsActive(party.State.Status))
        {
            failureReason = TransactionFailureReason.DisabledParty;
            return false;
        }

        failureReason = TransactionFailureReason.None;
        return true;
    }

    private static bool IsActive(string? status)
        => string.IsNullOrWhiteSpace(status) || string.Equals(status, "active", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeKeyId(string? keyId, string partyId, string purpose)
        => string.IsNullOrWhiteSpace(keyId)
            ? $"{partyId}:{purpose}:1"
            : keyId.Trim();

    private static string NormalizeStatus(string? status)
        => string.IsNullOrWhiteSpace(status) ? "active" : status.Trim();

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

    private static GeneratedSigningKey CreateSigningKey(string? suppliedPublicKeyPem)
    {
        if (!string.IsNullOrWhiteSpace(suppliedPublicKeyPem))
            return new GeneratedSigningKey(suppliedPublicKeyPem.Trim(), null);

        var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new GeneratedSigningKey(key.ExportSubjectPublicKeyInfoPem(), key);
    }

    private static string CreateEncryptionPublicKeyPem()
    {
        using var key = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        return key.ExportSubjectPublicKeyInfoPem();
    }

    private sealed record GeneratedSigningKey(string PublicKeyPem, ECDsa? PrivateKey);

    private sealed class RegisteredParty : IDisposable
    {
        public RegisteredParty(PartyRegistrationResponse state)
        {
            State = state;
        }

        public PartyRegistrationResponse State { get; }
        public Dictionary<string, PartyKeyDescriptor> Keys { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, ECDsa> SigningKeys { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Dispose()
        {
            foreach (var key in SigningKeys.Values)
                key.Dispose();
            SigningKeys.Clear();
        }
    }
}

/// <summary>FR-MCP-123 and FR-MCP-124: In-memory subscriber commit implementation.</summary>
public sealed class InMemorySubscriberCommitService : ISubscriberCommitService
{
    private readonly IKeyServerManifestService _keyServer;
    private readonly ITransactionManifestCanonicalizer _canonicalizer;
    private readonly IOptionsMonitor<SubscriberOptions> _options;
    private readonly ConcurrentDictionary<string, CommitRecord> _transactions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _lastSequences = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _nonces = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Initializes a new instance of the <see cref="InMemorySubscriberCommitService"/> class.</summary>
    /// <param name="keyServer">Keyserver manifest verifier.</param>
    /// <param name="canonicalizer">Manifest canonicalizer.</param>
    /// <param name="options">Subscriber options.</param>
    public InMemorySubscriberCommitService(
        IKeyServerManifestService keyServer,
        ITransactionManifestCanonicalizer canonicalizer,
        IOptionsMonitor<SubscriberOptions> options)
    {
        _keyServer = keyServer;
        _canonicalizer = canonicalizer;
        _options = options;
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
            return Rejected(transactionId, TransactionFailureReason.DecryptFailed);

        if (_transactions.TryGetValue(transactionId, out var existing))
            return ExistingCommitResponse(existing, manifestHash, request.EncryptedBodySha256, actualEncryptedBodySha256);

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
            return Rejected(transactionId, verify.Reason);

        if (!string.Equals(request.EncryptedBodySha256, manifest.EncryptedBodySha256, StringComparison.OrdinalIgnoreCase))
            return Rejected(transactionId, TransactionFailureReason.EncryptedBodyHashMismatch);
        if (!string.Equals(actualEncryptedBodySha256, manifest.EncryptedBodySha256, StringComparison.OrdinalIgnoreCase))
            return Rejected(transactionId, TransactionFailureReason.EncryptedBodyHashMismatch);
        if (!string.Equals(request.DiffgramSha256, manifest.DiffgramSha256, StringComparison.OrdinalIgnoreCase))
            return Rejected(transactionId, TransactionFailureReason.PlaintextDiffgramHashMismatch);

        var pairKey = $"{manifest.PublisherPartyId}\n{manifest.SubscriberPartyId}";
        if (_lastSequences.TryGetValue(pairKey, out var lastSequence) && manifest.Sequence <= lastSequence)
            return Rejected(transactionId, TransactionFailureReason.StaleSequence);

        var nonceKey = $"{pairKey}\n{manifest.Nonce}";
        if (!_nonces.TryAdd(nonceKey, transactionId))
            return Rejected(transactionId, TransactionFailureReason.ReplayNonce);

        var now = DateTimeOffset.UtcNow;
        var record = new CommitRecord(
            transactionId,
            "committed",
            TransactionFailureReason.None,
            manifestHash,
            actualEncryptedBodySha256,
            $"diffgram-{transactionId}",
            now,
            null);
        if (!_transactions.TryAdd(transactionId, record))
            return ExistingCommitResponse(_transactions[transactionId], manifestHash, request.EncryptedBodySha256, actualEncryptedBodySha256);

        _lastSequences.AddOrUpdate(pairKey, manifest.Sequence, (_, current) => Math.Max(current, manifest.Sequence));
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
    public Task<TransactionStatusResponse?> GetTransactionStatusAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _transactions.TryGetValue(transactionId, out var record)
                ? ToStatus(record)
                : null);
    }

    /// <inheritdoc />
    public Task<TransactionAbortResponse> AbortTransactionAsync(
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
        var record = new CommitRecord(
            transactionId.Trim(),
            "aborted",
            reason,
            string.Empty,
            string.Empty,
            null,
            null,
            now);
        _transactions.AddOrUpdate(transactionId.Trim(), record, (_, existing) =>
            string.Equals(existing.Status, "committed", StringComparison.OrdinalIgnoreCase) ? existing : record);
        var current = _transactions[transactionId.Trim()];
        return Task.FromResult(new TransactionAbortResponse
        {
            TransactionId = current.TransactionId,
            Status = current.Status,
            Reason = current.Status == "committed" ? TransactionFailureReason.DuplicateConflict : reason,
            AbortedAtUtc = current.AbortedAtUtc ?? now,
        });
    }

    private static DiffgramCommitResponse ExistingCommitResponse(
        CommitRecord existing,
        string manifestHash,
        string requestedEncryptedBodyHash,
        string actualEncryptedBodyHash)
    {
        if (string.Equals(existing.Status, "aborted", StringComparison.OrdinalIgnoreCase))
            return Rejected(existing.TransactionId, TransactionFailureReason.Aborted);

        if (string.Equals(existing.ManifestHashSha256, manifestHash, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.EncryptedBodySha256, requestedEncryptedBodyHash, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.EncryptedBodySha256, actualEncryptedBodyHash, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.Status, "committed", StringComparison.OrdinalIgnoreCase))
        {
            return new DiffgramCommitResponse
            {
                TransactionId = existing.TransactionId,
                Status = "duplicate",
                Reason = TransactionFailureReason.None,
                DiffgramId = existing.DiffgramId,
                CommittedAtUtc = existing.CommittedAtUtc,
            };
        }

        return Rejected(existing.TransactionId, TransactionFailureReason.DuplicateConflict);
    }

    private static DiffgramCommitResponse Rejected(string transactionId, TransactionFailureReason reason)
        => new()
        {
            TransactionId = transactionId,
            Status = "rejected",
            Reason = reason,
        };

    private static TransactionStatusResponse ToStatus(CommitRecord record)
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

    private sealed record CommitRecord(
        string TransactionId,
        string Status,
        TransactionFailureReason Reason,
        string ManifestHashSha256,
        string EncryptedBodySha256,
        string? DiffgramId,
        DateTimeOffset? CommittedAtUtc,
        DateTimeOffset? AbortedAtUtc);
}
