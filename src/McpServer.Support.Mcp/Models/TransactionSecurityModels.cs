using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Models;

/// <summary>Registration payload for a transaction-security party known to the keyserver. FR-MCP-118.</summary>
public sealed class PartyRegistrationRequest
{
    /// <summary>Stable party identifier used in manifests and key lookups.</summary>
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Party role, such as publisher or subscriber.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Optional active signing key identifier.</summary>
    [JsonPropertyName("activeSigningKeyId")]
    public string? ActiveSigningKeyId { get; set; }

    /// <summary>Optional active encryption key identifier.</summary>
    [JsonPropertyName("activeEncryptionKeyId")]
    public string? ActiveEncryptionKeyId { get; set; }

    /// <summary>Optional PEM-encoded signing public key.</summary>
    [JsonPropertyName("signingPublicKeyPem")]
    public string? SigningPublicKeyPem { get; set; }

    /// <summary>Optional PEM-encoded encryption public key.</summary>
    [JsonPropertyName("encryptionPublicKeyPem")]
    public string? EncryptionPublicKeyPem { get; set; }

    /// <summary>Registration status. Defaults to active.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";
}

/// <summary>Keyserver response describing a registered transaction-security party. FR-MCP-118.</summary>
public sealed class PartyRegistrationResponse
{
    /// <summary>Stable party identifier used in manifests and key lookups.</summary>
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Party role, such as publisher or subscriber.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Active signing key identifier for manifest signatures.</summary>
    [JsonPropertyName("activeSigningKeyId")]
    public string? ActiveSigningKeyId { get; set; }

    /// <summary>Active encryption key identifier for encrypted diffgrams.</summary>
    [JsonPropertyName("activeEncryptionKeyId")]
    public string? ActiveEncryptionKeyId { get; set; }

    /// <summary>Registration status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    /// <summary>UTC creation timestamp.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Optional UTC update timestamp.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

/// <summary>Public-key descriptor returned by the keyserver. TR-MCP-KEYSERVER-001.</summary>
public sealed class PartyKeyDescriptor
{
    /// <summary>Party identifier that owns the key.</summary>
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Stable key identifier.</summary>
    [JsonPropertyName("keyId")]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>Key purpose, such as signing or encryption.</summary>
    [JsonPropertyName("purpose")]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Cryptographic algorithm identifier.</summary>
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>PEM-encoded public key material.</summary>
    [JsonPropertyName("publicKeyPem")]
    public string PublicKeyPem { get; set; } = string.Empty;

    /// <summary>Key status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>UTC creation timestamp.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Optional UTC expiry timestamp.</summary>
    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}

/// <summary>Request to create and sign a canonical transaction manifest. FR-MCP-120.</summary>
public sealed class TransactionManifestSignRequest
{
    /// <summary>Stable transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Optional session-log turn identifier.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>Publisher party identifier.</summary>
    [JsonPropertyName("publisherPartyId")]
    public string PublisherPartyId { get; set; } = string.Empty;

    /// <summary>Subscriber party identifier.</summary>
    [JsonPropertyName("subscriberPartyId")]
    public string SubscriberPartyId { get; set; } = string.Empty;

    /// <summary>Optional publisher signing key identifier.</summary>
    [JsonPropertyName("publisherSigningKeyId")]
    public string? PublisherSigningKeyId { get; set; }

    /// <summary>Optional subscriber encryption key identifier.</summary>
    [JsonPropertyName("subscriberEncryptionKeyId")]
    public string? SubscriberEncryptionKeyId { get; set; }

    /// <summary>Monotonic publisher sequence.</summary>
    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }

    /// <summary>Replay-protection nonce.</summary>
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>Optional UTC issue timestamp.</summary>
    [JsonPropertyName("issuedAtUtc")]
    public DateTimeOffset? IssuedAtUtc { get; set; }

    /// <summary>Optional UTC expiry timestamp.</summary>
    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    /// <summary>Plaintext diffgram SHA-256 digest.</summary>
    [JsonPropertyName("diffgramSha256")]
    public string DiffgramSha256 { get; set; } = string.Empty;

    /// <summary>Encrypted body SHA-256 digest.</summary>
    [JsonPropertyName("encryptedBodySha256")]
    public string EncryptedBodySha256 { get; set; } = string.Empty;

    /// <summary>Manifest algorithm suite.</summary>
    [JsonPropertyName("algorithms")]
    public TransactionManifestAlgorithms Algorithms { get; set; } = new();
}

/// <summary>Algorithm identifiers used by transaction manifests. TR-MCP-CRYPTO-001.</summary>
public sealed class TransactionManifestAlgorithms
{
    /// <summary>Signature algorithm identifier.</summary>
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = "ECDSA-P256-SHA256";

    /// <summary>Encryption algorithm identifier.</summary>
    [JsonPropertyName("encryption")]
    public string Encryption { get; set; } = "ECDH-P256-HKDF-SHA256-AES-256-GCM";

    /// <summary>Canonicalization profile identifier.</summary>
    [JsonPropertyName("canonicalization")]
    public string Canonicalization { get; set; } = "transaction-manifest-v1";
}

/// <summary>Keyserver response for a manifest signing request. FR-MCP-120.</summary>
public sealed class TransactionManifestSignResponse
{
    /// <summary>Whether signing succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Signed manifest when signing succeeds.</summary>
    [JsonPropertyName("manifest")]
    public TransactionManifestDto? Manifest { get; set; }

    /// <summary>Structured failure reason.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason Reason { get; set; }
}

/// <summary>Request to verify a signed transaction manifest. FR-MCP-121.</summary>
public sealed class TransactionManifestVerifyRequest
{
    /// <summary>Signed transaction manifest.</summary>
    [JsonPropertyName("manifest")]
    public TransactionManifestDto Manifest { get; set; } = new();

    /// <summary>Optional expected subscriber party identifier.</summary>
    [JsonPropertyName("expectedSubscriberPartyId")]
    public string? ExpectedSubscriberPartyId { get; set; }
}

/// <summary>Keyserver response for manifest verification. FR-MCP-121.</summary>
public sealed class TransactionManifestVerifyResponse
{
    /// <summary>Whether the manifest is valid.</summary>
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    /// <summary>Structured verification reason.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason Reason { get; set; }

    /// <summary>SHA-256 digest of the canonical manifest.</summary>
    [JsonPropertyName("manifestHashSha256")]
    public string? ManifestHashSha256 { get; set; }
}

/// <summary>Canonical signed manifest that accompanies an encrypted transaction diffgram. FR-MCP-120.</summary>
public sealed class TransactionManifestDto
{
    /// <summary>Stable transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Optional session-log turn identifier.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>Publisher party identifier.</summary>
    [JsonPropertyName("publisherPartyId")]
    public string PublisherPartyId { get; set; } = string.Empty;

    /// <summary>Subscriber party identifier.</summary>
    [JsonPropertyName("subscriberPartyId")]
    public string SubscriberPartyId { get; set; } = string.Empty;

    /// <summary>Publisher signing key identifier.</summary>
    [JsonPropertyName("publisherSigningKeyId")]
    public string? PublisherSigningKeyId { get; set; }

    /// <summary>Subscriber encryption key identifier.</summary>
    [JsonPropertyName("subscriberEncryptionKeyId")]
    public string? SubscriberEncryptionKeyId { get; set; }

    /// <summary>Monotonic publisher sequence.</summary>
    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }

    /// <summary>Replay-protection nonce.</summary>
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>UTC issue timestamp.</summary>
    [JsonPropertyName("issuedAtUtc")]
    public DateTimeOffset IssuedAtUtc { get; set; }

    /// <summary>UTC expiry timestamp.</summary>
    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>Plaintext diffgram SHA-256 digest.</summary>
    [JsonPropertyName("diffgramSha256")]
    public string DiffgramSha256 { get; set; } = string.Empty;

    /// <summary>Encrypted body SHA-256 digest.</summary>
    [JsonPropertyName("encryptedBodySha256")]
    public string EncryptedBodySha256 { get; set; } = string.Empty;

    /// <summary>Manifest algorithm suite.</summary>
    [JsonPropertyName("algorithms")]
    public TransactionManifestAlgorithms Algorithms { get; set; } = new();

    /// <summary>Signature block over the canonical manifest.</summary>
    [JsonPropertyName("signature")]
    public TransactionManifestSignatureDto? Signature { get; set; }
}

/// <summary>Signature metadata attached to a transaction manifest. TR-MCP-CRYPTO-001.</summary>
public sealed class TransactionManifestSignatureDto
{
    /// <summary>Signature algorithm identifier.</summary>
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>Signing key identifier.</summary>
    [JsonPropertyName("keyId")]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>Base64-encoded signature value.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>UTC signature timestamp.</summary>
    [JsonPropertyName("signedAtUtc")]
    public DateTimeOffset SignedAtUtc { get; set; }
}

/// <summary>Subscriber commit request carrying a signed manifest and encrypted diffgram body. FR-MCP-123.</summary>
public sealed class DiffgramCommitRequest
{
    /// <summary>Signed transaction manifest.</summary>
    [JsonPropertyName("manifest")]
    public TransactionManifestDto Manifest { get; set; } = new();

    /// <summary>Base64-encoded encrypted diffgram body.</summary>
    [JsonPropertyName("encryptedDiffgramBase64")]
    public string EncryptedDiffgramBase64 { get; set; } = string.Empty;

    /// <summary>Encrypted body SHA-256 digest.</summary>
    [JsonPropertyName("encryptedBodySha256")]
    public string EncryptedBodySha256 { get; set; } = string.Empty;

    /// <summary>Plaintext diffgram SHA-256 digest.</summary>
    [JsonPropertyName("diffgramSha256")]
    public string DiffgramSha256 { get; set; } = string.Empty;
}

/// <summary>Subscriber commit response for a transactional diffgram. FR-MCP-123.</summary>
public sealed class DiffgramCommitResponse
{
    /// <summary>Commit status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured result reason.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason Reason { get; set; }

    /// <summary>Stable transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Optional persisted diffgram identifier.</summary>
    [JsonPropertyName("diffgramId")]
    public string? DiffgramId { get; set; }

    /// <summary>Optional UTC commit timestamp.</summary>
    [JsonPropertyName("committedAtUtc")]
    public DateTimeOffset? CommittedAtUtc { get; set; }
}

/// <summary>Subscriber status response for a known transaction. FR-MCP-122.</summary>
public sealed class TransactionStatusResponse
{
    /// <summary>Stable transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Current transaction status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Optional structured result reason.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason? Reason { get; set; }

    /// <summary>Optional UTC commit timestamp.</summary>
    [JsonPropertyName("committedAtUtc")]
    public DateTimeOffset? CommittedAtUtc { get; set; }

    /// <summary>Optional UTC abort timestamp.</summary>
    [JsonPropertyName("abortedAtUtc")]
    public DateTimeOffset? AbortedAtUtc { get; set; }
}

/// <summary>Request to abort a transaction before subscriber commit. FR-MCP-122.</summary>
public sealed class TransactionAbortRequest
{
    /// <summary>Structured abort reason.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason Reason { get; set; }

    /// <summary>Optional actor that requested the abort.</summary>
    [JsonPropertyName("actor")]
    public string? Actor { get; set; }
}

/// <summary>Subscriber response after a transaction abort. FR-MCP-122.</summary>
public sealed class TransactionAbortResponse
{
    /// <summary>Stable transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Current transaction status after abort.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured abort reason.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason Reason { get; set; }

    /// <summary>UTC abort timestamp.</summary>
    [JsonPropertyName("abortedAtUtc")]
    public DateTimeOffset AbortedAtUtc { get; set; }
}

/// <summary>Request metadata for a turn transaction coordinator execution. FR-MCP-120.</summary>
public sealed class TurnTransactionRequest
{
    /// <summary>Optional transaction identifier. When omitted the coordinator assigns one.</summary>
    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    /// <summary>Optional session-log turn identifier.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>Logical operation name, such as todo.update or sessionlog.append.</summary>
    [JsonPropertyName("operationName")]
    public string OperationName { get; set; } = string.Empty;

    /// <summary>Serialized operation payload used to build the diffgram evidence body.</summary>
    [JsonPropertyName("operationBodyJson")]
    public string OperationBodyJson { get; set; } = "{}";

    /// <summary>Optional publisher party override.</summary>
    [JsonPropertyName("publisherPartyId")]
    public string? PublisherPartyId { get; set; }

    /// <summary>Optional subscriber party override.</summary>
    [JsonPropertyName("subscriberPartyId")]
    public string? SubscriberPartyId { get; set; }

    /// <summary>Monotonic operation sequence supplied by the adapter.</summary>
    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }

    /// <summary>Whether this operation changes durable state.</summary>
    [JsonPropertyName("mutating")]
    public bool Mutating { get; set; } = true;
}

/// <summary>Mutation callback result captured by the transaction coordinator. FR-MCP-120.</summary>
public sealed class TurnMutationResult
{
    /// <summary>Whether the mutation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Optional mutation result body serialized as JSON.</summary>
    [JsonPropertyName("resultJson")]
    public string? ResultJson { get; set; }

    /// <summary>Optional failure message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>Coordinator result for a turn transaction attempt. FR-MCP-120, FR-MCP-121.</summary>
public sealed class TurnTransactionResult
{
    /// <summary>Transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Status such as bypassed, committed, aborted, rejected, or degraded.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured reason code for the status.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason Reason { get; set; }

    /// <summary>Whether the mutation callback was executed.</summary>
    [JsonPropertyName("mutationApplied")]
    public bool MutationApplied { get; set; }

    /// <summary>Whether the coordinator is currently in degraded mode.</summary>
    [JsonPropertyName("degraded")]
    public bool Degraded { get; set; }

    /// <summary>Optional manifest hash from verification or commit evidence.</summary>
    [JsonPropertyName("manifestHashSha256")]
    public string? ManifestHashSha256 { get; set; }

    /// <summary>Optional committed diffgram identifier.</summary>
    [JsonPropertyName("diffgramId")]
    public string? DiffgramId { get; set; }

    /// <summary>Optional coordinator message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>Optional mutation callback result.</summary>
    [JsonPropertyName("mutationResult")]
    public TurnMutationResult? MutationResult { get; set; }
}

/// <summary>Current turn transaction coordinator status. FR-MCP-121.</summary>
public sealed class TurnTransactionStatusResponse
{
    /// <summary>Whether turn transaction gating is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Whether degraded mode is currently active.</summary>
    [JsonPropertyName("degraded")]
    public bool Degraded { get; set; }

    /// <summary>Last recorded degraded or failure reason.</summary>
    [JsonPropertyName("lastReason")]
    public TransactionFailureReason LastReason { get; set; }

    /// <summary>Last transaction identifier processed by the coordinator.</summary>
    [JsonPropertyName("lastTransactionId")]
    public string? LastTransactionId { get; set; }

    /// <summary>Human-readable status message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>Structured transaction failure reason codes shared by keyserver and subscriber APIs. FR-MCP-124.</summary>
public enum TransactionFailureReason
{
    /// <summary>No failure occurred.</summary>
    None = 0,
    /// <summary>The failure was not categorized.</summary>
    Unknown = 1,
    /// <summary>The referenced party is unknown.</summary>
    UnknownParty = 2,
    /// <summary>The referenced party is disabled.</summary>
    DisabledParty = 3,
    /// <summary>The referenced key is unknown.</summary>
    UnknownKey = 4,
    /// <summary>The referenced key is disabled.</summary>
    DisabledKey = 5,
    /// <summary>The manifest has expired.</summary>
    ExpiredManifest = 6,
    /// <summary>The manifest issue timestamp is too far in the future.</summary>
    FutureManifest = 7,
    /// <summary>The nonce has already been observed.</summary>
    ReplayNonce = 8,
    /// <summary>The sequence is stale for the publisher/subscriber pair.</summary>
    StaleSequence = 9,
    /// <summary>The signature block is malformed.</summary>
    MalformedSignature = 10,
    /// <summary>The manifest signature does not match.</summary>
    ManifestSignatureMismatch = 11,
    /// <summary>The encrypted body hash does not match.</summary>
    EncryptedBodyHashMismatch = 12,
    /// <summary>The plaintext diffgram hash does not match.</summary>
    PlaintextDiffgramHashMismatch = 13,
    /// <summary>The manifest targets a different subscriber.</summary>
    WrongSubscriber = 14,
    /// <summary>The subscriber could not decrypt the body.</summary>
    DecryptFailed = 15,
    /// <summary>The transaction conflicts with a previous payload.</summary>
    DuplicateConflict = 16,
    /// <summary>The transaction was aborted.</summary>
    Aborted = 17,
    /// <summary>The keyserver was unavailable.</summary>
    KeyServerUnavailable = 18,
    /// <summary>The subscriber was unavailable.</summary>
    SubscriberUnavailable = 19,
    /// <summary>The commit timed out.</summary>
    CommitTimeout = 20,
    /// <summary>The transaction pipeline is disabled.</summary>
    TransactionsDisabled = 21,
    /// <summary>The requested behavior is intentionally deferred.</summary>
    DeferredFeatureDisabled = 22,
}
