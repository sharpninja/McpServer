using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;

namespace McpServer.TransactionSecurity.Services;

/// <summary>TR-MCP-CRYPTO-001: Protects and decrypts transaction diffgram payloads.</summary>
public interface ITransactionDiffgramProtector
{
    /// <summary>Encrypts a plaintext diffgram for a subscriber public encryption key.</summary>
    /// <param name="plaintextJson">Plaintext diffgram JSON.</param>
    /// <param name="subscriberEncryptionKey">Subscriber public encryption key descriptor.</param>
    /// <returns>Protected diffgram payload and hashes.</returns>
    DiffgramProtectionResult Protect(string plaintextJson, PartyKeyDescriptor subscriberEncryptionKey);

    /// <summary>Attempts to decrypt a protected diffgram envelope for a subscriber.</summary>
    /// <param name="encryptedDiffgramBase64">Base64-encoded protected envelope or legacy placeholder body.</param>
    /// <param name="options">Subscriber options containing private key material.</param>
    /// <param name="expectedSubscriberPartyId">Expected subscriber party identifier.</param>
    /// <param name="expectedEncryptionKeyId">Expected subscriber encryption key identifier.</param>
    /// <returns>Decryption result.</returns>
    DiffgramUnprotectResult Unprotect(
        string encryptedDiffgramBase64,
        SubscriberOptions options,
        string expectedSubscriberPartyId,
        string? expectedEncryptionKeyId);
}

/// <summary>Protected diffgram payload returned by <see cref="ITransactionDiffgramProtector"/>.</summary>
public sealed class DiffgramProtectionResult
{
    /// <summary>Base64-encoded encrypted diffgram envelope.</summary>
    public string EncryptedDiffgramBase64 { get; set; } = string.Empty;

    /// <summary>SHA-256 digest of the protected envelope bytes.</summary>
    public string EncryptedBodySha256 { get; set; } = string.Empty;

    /// <summary>SHA-256 digest of the plaintext diffgram bytes.</summary>
    public string PlaintextSha256 { get; set; } = string.Empty;
}

/// <summary>Diffgram decryption result returned by <see cref="ITransactionDiffgramProtector"/>.</summary>
public sealed class DiffgramUnprotectResult
{
    /// <summary>Whether the body was a protected diffgram envelope.</summary>
    public bool IsProtectedEnvelope { get; set; }

    /// <summary>Whether decryption succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Structured failure reason when decryption fails.</summary>
    public TransactionFailureReason Reason { get; set; }

    /// <summary>Plaintext diffgram JSON when decryption succeeds.</summary>
    public string? PlaintextJson { get; set; }

    /// <summary>SHA-256 digest of the decrypted plaintext bytes.</summary>
    public string? PlaintextSha256 { get; set; }
}

/// <summary>ECDH/HKDF/AES-GCM implementation for transaction diffgram payloads.</summary>
public sealed class TransactionDiffgramProtector : ITransactionDiffgramProtector
{
    private const string Algorithm = "ECDH-P256-HKDF-SHA256-AES-256-GCM";
    private const string EnvelopeType = "mcp-transaction-diffgram-v1";
    private const int AesKeySizeBytes = 32;
    private const int AesNonceSizeBytes = 12;
    private const int AesTagSizeBytes = 16;

    private static readonly JsonSerializerOptions EnvelopeJsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public DiffgramProtectionResult Protect(string plaintextJson, PartyKeyDescriptor subscriberEncryptionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintextJson);
        ArgumentNullException.ThrowIfNull(subscriberEncryptionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriberEncryptionKey.PublicKeyPem);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriberEncryptionKey.PartyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriberEncryptionKey.KeyId);

        var plaintext = Encoding.UTF8.GetBytes(plaintextJson);
        var salt = RandomNumberGenerator.GetBytes(32);
        var nonce = RandomNumberGenerator.GetBytes(AesNonceSizeBytes);
        var tag = new byte[AesTagSizeBytes];
        byte[] key = [];
        byte[] rawSecret = [];
        byte[] ciphertext;

        using var recipient = ECDiffieHellman.Create();
        recipient.ImportFromPem(subscriberEncryptionKey.PublicKeyPem);
        using var ephemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        try
        {
            rawSecret = ephemeral.DeriveRawSecretAgreement(recipient.PublicKey);
            key = DeriveAesKey(rawSecret, salt, subscriberEncryptionKey.PartyId, subscriberEncryptionKey.KeyId);
            ciphertext = new byte[plaintext.Length];
            using var aes = new AesGcm(key, AesTagSizeBytes);
            aes.Encrypt(
                nonce,
                plaintext,
                ciphertext,
                tag,
                BuildAssociatedData(subscriberEncryptionKey.PartyId, subscriberEncryptionKey.KeyId));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(rawSecret);
        }

        var envelope = new ProtectedDiffgramEnvelope
        {
            Type = EnvelopeType,
            SchemaVersion = 1,
            Algorithm = Algorithm,
            SubscriberPartyId = subscriberEncryptionKey.PartyId,
            SubscriberEncryptionKeyId = subscriberEncryptionKey.KeyId,
            EphemeralPublicKeyPem = ephemeral.ExportSubjectPublicKeyInfoPem(),
            SaltBase64 = Convert.ToBase64String(salt),
            NonceBase64 = Convert.ToBase64String(nonce),
            CiphertextBase64 = Convert.ToBase64String(ciphertext),
            TagBase64 = Convert.ToBase64String(tag),
        };
        var envelopeBytes = JsonSerializer.SerializeToUtf8Bytes(envelope, EnvelopeJsonOptions);
        return new DiffgramProtectionResult
        {
            EncryptedDiffgramBase64 = Convert.ToBase64String(envelopeBytes),
            EncryptedBodySha256 = HashHex(envelopeBytes),
            PlaintextSha256 = HashHex(plaintext),
        };
    }

    /// <inheritdoc />
    public DiffgramUnprotectResult Unprotect(
        string encryptedDiffgramBase64,
        SubscriberOptions options,
        string expectedSubscriberPartyId,
        string? expectedEncryptionKeyId)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSubscriberPartyId);

        if (!TryDecodeEnvelope(encryptedDiffgramBase64, out var envelope, out var envelopeBytes))
        {
            return new DiffgramUnprotectResult
            {
                IsProtectedEnvelope = false,
                Success = false,
                Reason = TransactionFailureReason.DecryptFailed,
            };
        }

        if (envelope is null)
            return new DiffgramUnprotectResult { IsProtectedEnvelope = false };

        if (!string.Equals(envelope.SubscriberPartyId, expectedSubscriberPartyId, StringComparison.OrdinalIgnoreCase))
            return Failed(TransactionFailureReason.WrongSubscriber);

        if (!string.IsNullOrWhiteSpace(expectedEncryptionKeyId) &&
            !string.Equals(envelope.SubscriberEncryptionKeyId, expectedEncryptionKeyId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Failed(TransactionFailureReason.UnknownKey);
        }

        var privateKeyPem = ResolvePrivateKeyPem(options, envelope.SubscriberEncryptionKeyId);
        if (string.IsNullOrWhiteSpace(privateKeyPem))
        {
            return options.EncryptionKeys.Count > 0
                ? Failed(TransactionFailureReason.UnknownKey)
                : Failed(TransactionFailureReason.DecryptFailed);
        }

        var configuredSingleKeyId = string.IsNullOrWhiteSpace(options.EncryptionKeyId)
            ? null
            : options.EncryptionKeyId.Trim();
        if (options.EncryptionKeys.Count == 0 &&
            !string.IsNullOrWhiteSpace(configuredSingleKeyId) &&
            !string.Equals(envelope.SubscriberEncryptionKeyId, configuredSingleKeyId, StringComparison.OrdinalIgnoreCase))
        {
            return Failed(TransactionFailureReason.UnknownKey);
        }

        byte[] key = [];
        byte[] rawSecret = [];
        try
        {
            var salt = Convert.FromBase64String(envelope.SaltBase64);
            var nonce = Convert.FromBase64String(envelope.NonceBase64);
            var ciphertext = Convert.FromBase64String(envelope.CiphertextBase64);
            var tag = Convert.FromBase64String(envelope.TagBase64);
            var plaintext = new byte[ciphertext.Length];

            using var privateKey = ECDiffieHellman.Create();
            privateKey.ImportFromPem(privateKeyPem);
            using var ephemeral = ECDiffieHellman.Create();
            ephemeral.ImportFromPem(envelope.EphemeralPublicKeyPem);
            rawSecret = privateKey.DeriveRawSecretAgreement(ephemeral.PublicKey);
            key = DeriveAesKey(rawSecret, salt, envelope.SubscriberPartyId, envelope.SubscriberEncryptionKeyId);
            using var aes = new AesGcm(key, AesTagSizeBytes);
            aes.Decrypt(
                nonce,
                ciphertext,
                tag,
                plaintext,
                BuildAssociatedData(envelope.SubscriberPartyId, envelope.SubscriberEncryptionKeyId));

            return new DiffgramUnprotectResult
            {
                IsProtectedEnvelope = true,
                Success = true,
                Reason = TransactionFailureReason.None,
                PlaintextJson = Encoding.UTF8.GetString(plaintext),
                PlaintextSha256 = HashHex(plaintext),
            };
        }
        catch (FormatException)
        {
            return Failed(TransactionFailureReason.DecryptFailed);
        }
        catch (CryptographicException)
        {
            return Failed(TransactionFailureReason.DecryptFailed);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(rawSecret);
            CryptographicOperations.ZeroMemory(envelopeBytes);
        }

        DiffgramUnprotectResult Failed(TransactionFailureReason reason)
            => new()
            {
                IsProtectedEnvelope = true,
                Success = false,
                Reason = reason,
            };
    }

    private static string? ResolvePrivateKeyPem(SubscriberOptions options, string envelopeKeyId)
    {
        if (options.EncryptionKeys.Count == 0)
            return options.EncryptionPrivateKeyPem;

        foreach (var key in options.EncryptionKeys)
        {
            if (string.Equals(key.KeyId?.Trim(), envelopeKeyId, StringComparison.OrdinalIgnoreCase))
                return key.PrivateKeyPem;
        }

        return null;
    }

    private static bool TryDecodeEnvelope(
        string encryptedDiffgramBase64,
        out ProtectedDiffgramEnvelope? envelope,
        out byte[] envelopeBytes)
    {
        envelope = null;
        envelopeBytes = [];
        if (string.IsNullOrWhiteSpace(encryptedDiffgramBase64))
            return false;

        try
        {
            envelopeBytes = Convert.FromBase64String(encryptedDiffgramBase64);
            envelope = JsonSerializer.Deserialize<ProtectedDiffgramEnvelope>(envelopeBytes, EnvelopeJsonOptions);
            if (envelope is null ||
                !string.Equals(envelope.Type, EnvelopeType, StringComparison.Ordinal) ||
                envelope.SchemaVersion != 1 ||
                !string.Equals(envelope.Algorithm, Algorithm, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(envelope.SubscriberPartyId) ||
                string.IsNullOrWhiteSpace(envelope.SubscriberEncryptionKeyId) ||
                string.IsNullOrWhiteSpace(envelope.EphemeralPublicKeyPem) ||
                string.IsNullOrWhiteSpace(envelope.SaltBase64) ||
                string.IsNullOrWhiteSpace(envelope.NonceBase64) ||
                string.IsNullOrWhiteSpace(envelope.CiphertextBase64) ||
                string.IsNullOrWhiteSpace(envelope.TagBase64))
            {
                envelope = null;
            }

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            envelope = null;
            return true;
        }
    }

    private static byte[] DeriveAesKey(
        byte[] rawSecret,
        byte[] salt,
        string subscriberPartyId,
        string subscriberEncryptionKeyId)
    {
        var info = BuildAssociatedData(subscriberPartyId, subscriberEncryptionKeyId);
        var pseudoRandomKey = Hmac(salt, rawSecret);
        try
        {
            var output = new byte[AesKeySizeBytes];
            var previous = Array.Empty<byte>();
            var offset = 0;
            byte counter = 1;
            while (offset < output.Length)
            {
                var input = previous
                    .Concat(info)
                    .Concat(new[] { counter })
                    .ToArray();
                previous = Hmac(pseudoRandomKey, input);
                var copy = Math.Min(previous.Length, output.Length - offset);
                Buffer.BlockCopy(previous, 0, output, offset, copy);
                offset += copy;
                counter++;
            }

            return output;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pseudoRandomKey);
        }
    }

    private static byte[] Hmac(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    private static byte[] BuildAssociatedData(string subscriberPartyId, string subscriberEncryptionKeyId)
        => Encoding.UTF8.GetBytes($"{EnvelopeType}\n{Algorithm}\n{subscriberPartyId}\n{subscriberEncryptionKeyId}");

    private static string HashHex(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed class ProtectedDiffgramEnvelope
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonPropertyName("algorithm")]
        public string Algorithm { get; set; } = string.Empty;

        [JsonPropertyName("subscriberPartyId")]
        public string SubscriberPartyId { get; set; } = string.Empty;

        [JsonPropertyName("subscriberEncryptionKeyId")]
        public string SubscriberEncryptionKeyId { get; set; } = string.Empty;

        [JsonPropertyName("ephemeralPublicKeyPem")]
        public string EphemeralPublicKeyPem { get; set; } = string.Empty;

        [JsonPropertyName("saltBase64")]
        public string SaltBase64 { get; set; } = string.Empty;

        [JsonPropertyName("nonceBase64")]
        public string NonceBase64 { get; set; } = string.Empty;

        [JsonPropertyName("ciphertextBase64")]
        public string CiphertextBase64 { get; set; } = string.Empty;

        [JsonPropertyName("tagBase64")]
        public string TagBase64 { get; set; } = string.Empty;
    }
}
