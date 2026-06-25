using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>
/// Registration payload for a transaction-security party known to the keyserver.
/// FR-MCP-118, FR-MCP-119.
/// </summary>
public sealed class PartyRegistrationRequest
{
    /// <summary>Stable party identifier used in manifests and key lookups.</summary>
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Party role, such as <c>publisher</c> or <c>subscriber</c>.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Optional active signing key identifier for manifest signatures.</summary>
    [JsonPropertyName("activeSigningKeyId")]
    public string? ActiveSigningKeyId { get; set; }

    /// <summary>Optional active encryption key identifier for diffgram encryption.</summary>
    [JsonPropertyName("activeEncryptionKeyId")]
    public string? ActiveEncryptionKeyId { get; set; }

    /// <summary>Optional PEM-encoded signing public key.</summary>
    [JsonPropertyName("signingPublicKeyPem")]
    public string? SigningPublicKeyPem { get; set; }

    /// <summary>Optional PEM-encoded signing private key for keyserver signing material import.</summary>
    [JsonPropertyName("signingPrivateKeyPem")]
    public string? SigningPrivateKeyPem { get; set; }

    /// <summary>Optional PEM-encoded encryption public key.</summary>
    [JsonPropertyName("encryptionPublicKeyPem")]
    public string? EncryptionPublicKeyPem { get; set; }

    /// <summary>Registration status. Defaults to <c>active</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";
}

/// <summary>
/// Keyserver response describing the registered transaction-security party.
/// FR-MCP-118, FR-MCP-119.
/// </summary>
public sealed class PartyRegistrationResponse
{
    /// <summary>Stable party identifier used in manifests and key lookups.</summary>
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Party role, such as <c>publisher</c> or <c>subscriber</c>.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Active signing key identifier for manifest signatures.</summary>
    [JsonPropertyName("activeSigningKeyId")]
    public string? ActiveSigningKeyId { get; set; }

    /// <summary>Active encryption key identifier for diffgram encryption.</summary>
    [JsonPropertyName("activeEncryptionKeyId")]
    public string? ActiveEncryptionKeyId { get; set; }

    /// <summary>Registration status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    /// <summary>UTC timestamp when the party was created.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Optional UTC timestamp when the party was last updated.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

/// <summary>
/// Public-key descriptor returned by the keyserver for a specific party key.
/// FR-MCP-118, TR-MCP-KEYSERVER-001.
/// </summary>
public sealed class PartyKeyDescriptor
{
    /// <summary>Stable party identifier that owns the key.</summary>
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Stable key identifier.</summary>
    [JsonPropertyName("keyId")]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>Key purpose, such as <c>signing</c> or <c>encryption</c>.</summary>
    [JsonPropertyName("purpose")]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Cryptographic algorithm identifier.</summary>
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>PEM-encoded public key material.</summary>
    [JsonPropertyName("publicKeyPem")]
    public string PublicKeyPem { get; set; } = string.Empty;

    /// <summary>Key status, such as <c>active</c> or <c>disabled</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the key was created.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Optional UTC timestamp after which the key must not be used.</summary>
    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}

/// <summary>
/// Request to create and sign a canonical transaction manifest.
/// FR-MCP-120, FR-MCP-121.
/// </summary>
public sealed class TransactionManifestSignRequest
{
    /// <summary>Stable transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Optional session-log turn identifier associated with this transaction.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>Party identifier for the publisher that signs the manifest.</summary>
    [JsonPropertyName("publisherPartyId")]
    public string PublisherPartyId { get; set; } = string.Empty;

    /// <summary>Party identifier for the subscriber that can decrypt the diffgram.</summary>
    [JsonPropertyName("subscriberPartyId")]
    public string SubscriberPartyId { get; set; } = string.Empty;

    /// <summary>Optional publisher signing key identifier.</summary>
    [JsonPropertyName("publisherSigningKeyId")]
    public string? PublisherSigningKeyId { get; set; }

    /// <summary>Optional subscriber encryption key identifier.</summary>
    [JsonPropertyName("subscriberEncryptionKeyId")]
    public string? SubscriberEncryptionKeyId { get; set; }

    /// <summary>Monotonic publisher sequence number used for replay protection.</summary>
    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }

    /// <summary>Nonce used for replay detection and audit correlation.</summary>
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>Optional UTC issue timestamp. The server may assign one when omitted.</summary>
    [JsonPropertyName("issuedAtUtc")]
    public DateTimeOffset? IssuedAtUtc { get; set; }

    /// <summary>Optional UTC expiry timestamp. The server may assign one when omitted.</summary>
    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    /// <summary>SHA-256 digest of the plaintext diffgram payload.</summary>
    [JsonPropertyName("diffgramSha256")]
    public string DiffgramSha256 { get; set; } = string.Empty;

    /// <summary>SHA-256 digest of the encrypted diffgram payload.</summary>
    [JsonPropertyName("encryptedBodySha256")]
    public string EncryptedBodySha256 { get; set; } = string.Empty;

    /// <summary>Algorithm suite used to canonicalize, sign, and encrypt the transaction.</summary>
    [JsonPropertyName("algorithms")]
    public TransactionManifestAlgorithms Algorithms { get; set; } = new();
}

/// <summary>
/// Algorithm identifiers used by the transactional diffgram manifest.
/// FR-MCP-120, TR-MCP-CRYPTO-001.
/// </summary>
public sealed class TransactionManifestAlgorithms
{
    /// <summary>Signature algorithm identifier.</summary>
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = "ECDSA-P256-SHA256";

    /// <summary>Encryption algorithm identifier.</summary>
    [JsonPropertyName("encryption")]
    public string Encryption { get; set; } = "ECDH-P256-HKDF-SHA256-AES-256-GCM";

    /// <summary>Canonicalization profile used before signing.</summary>
    [JsonPropertyName("canonicalization")]
    public string Canonicalization { get; set; } = "transaction-manifest-v1";
}

/// <summary>
/// Keyserver response for a manifest signing request.
/// FR-MCP-120, FR-MCP-121.
/// </summary>
public sealed class TransactionManifestSignResponse
{
    /// <summary>Whether signing succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Signed manifest when <see cref="Success"/> is <see langword="true"/>.</summary>
    [JsonPropertyName("manifest")]
    public TransactionManifestDto? Manifest { get; set; }

    /// <summary>Failure reason when signing did not succeed.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason Reason { get; set; }
}

/// <summary>
/// Request to verify a signed transaction manifest before accepting a diffgram.
/// FR-MCP-121, FR-MCP-123.
/// </summary>
public sealed class TransactionManifestVerifyRequest
{
    /// <summary>Signed transaction manifest to verify.</summary>
    [JsonPropertyName("manifest")]
    public TransactionManifestDto Manifest { get; set; } = new();

    /// <summary>Optional expected subscriber identifier used to reject wrong-recipient manifests.</summary>
    [JsonPropertyName("expectedSubscriberPartyId")]
    public string? ExpectedSubscriberPartyId { get; set; }
}

/// <summary>
/// Keyserver response for a manifest verification request.
/// FR-MCP-121, FR-MCP-123.
/// </summary>
public sealed class TransactionManifestVerifyResponse
{
    /// <summary>Whether the manifest is valid for the requested recipient and time window.</summary>
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    /// <summary>Structured verification reason.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason Reason { get; set; }

    /// <summary>Optional SHA-256 digest of the canonical manifest used for audit.</summary>
    [JsonPropertyName("manifestHashSha256")]
    public string? ManifestHashSha256 { get; set; }
}

/// <summary>
/// Persisted public trace metadata for a signed transaction manifest.
/// FR-MCP-120, FR-MCP-121.
/// </summary>
public sealed class TransactionManifestTraceRecord
{
    /// <summary>Stable transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Optional session-log turn identifier associated with this transaction.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>Party identifier for the publisher that signed the manifest.</summary>
    [JsonPropertyName("publisherPartyId")]
    public string PublisherPartyId { get; set; } = string.Empty;

    /// <summary>Publisher signing key identifier used to verify the manifest.</summary>
    [JsonPropertyName("publisherSigningKeyId")]
    public string? PublisherSigningKeyId { get; set; }

    /// <summary>Party identifier for the subscriber that can decrypt the diffgram.</summary>
    [JsonPropertyName("subscriberPartyId")]
    public string SubscriberPartyId { get; set; } = string.Empty;

    /// <summary>Subscriber encryption key identifier used to decrypt the diffgram.</summary>
    [JsonPropertyName("subscriberEncryptionKeyId")]
    public string? SubscriberEncryptionKeyId { get; set; }

    /// <summary>Monotonic publisher sequence number used for replay protection.</summary>
    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }

    /// <summary>Nonce used for replay detection and audit correlation.</summary>
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>UTC issue timestamp.</summary>
    [JsonPropertyName("issuedAtUtc")]
    public DateTimeOffset IssuedAtUtc { get; set; }

    /// <summary>UTC expiry timestamp.</summary>
    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>SHA-256 digest of the plaintext diffgram payload.</summary>
    [JsonPropertyName("diffgramSha256")]
    public string DiffgramSha256 { get; set; } = string.Empty;

    /// <summary>SHA-256 digest of the encrypted diffgram payload.</summary>
    [JsonPropertyName("encryptedBodySha256")]
    public string EncryptedBodySha256 { get; set; } = string.Empty;

    /// <summary>Signature algorithm identifier.</summary>
    [JsonPropertyName("signatureAlgorithm")]
    public string SignatureAlgorithm { get; set; } = string.Empty;

    /// <summary>Encryption algorithm identifier.</summary>
    [JsonPropertyName("encryptionAlgorithm")]
    public string EncryptionAlgorithm { get; set; } = string.Empty;

    /// <summary>Canonicalization profile identifier.</summary>
    [JsonPropertyName("canonicalizationProfile")]
    public string CanonicalizationProfile { get; set; } = string.Empty;

    /// <summary>Signing key identifier recorded in the manifest signature.</summary>
    [JsonPropertyName("signatureKeyId")]
    public string SignatureKeyId { get; set; } = string.Empty;

    /// <summary>Base64-encoded signature value.</summary>
    [JsonPropertyName("signatureValue")]
    public string SignatureValue { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the signature was generated.</summary>
    [JsonPropertyName("signedAtUtc")]
    public DateTimeOffset SignedAtUtc { get; set; }

    /// <summary>SHA-256 digest of the canonical unsigned manifest.</summary>
    [JsonPropertyName("manifestHashSha256")]
    public string ManifestHashSha256 { get; set; } = string.Empty;

    /// <summary>Trace status, such as <c>signed</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "signed";

    /// <summary>UTC timestamp when the trace record was created.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// Filter request for a transaction manifest traceability report.
/// FR-MCP-120, FR-MCP-121.
/// </summary>
public sealed class TransactionManifestTraceReportRequest
{
    /// <summary>Optional publisher party filter.</summary>
    [JsonPropertyName("publisherPartyId")]
    public string? PublisherPartyId { get; set; }

    /// <summary>Optional subscriber party filter.</summary>
    [JsonPropertyName("subscriberPartyId")]
    public string? SubscriberPartyId { get; set; }

    /// <summary>Optional trace status filter, such as <c>signed</c>.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional inclusive lower created-at timestamp filter.</summary>
    [JsonPropertyName("fromUtc")]
    public DateTimeOffset? FromUtc { get; set; }

    /// <summary>Optional inclusive upper created-at timestamp filter.</summary>
    [JsonPropertyName("toUtc")]
    public DateTimeOffset? ToUtc { get; set; }

    /// <summary>Maximum number of trace records to return.</summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; set; }
}

/// <summary>
/// Traceability report over signed transaction manifest ledger records.
/// FR-MCP-120, FR-MCP-121.
/// </summary>
public sealed class TransactionManifestTraceReport
{
    /// <summary>UTC timestamp when the report was generated.</summary>
    [JsonPropertyName("generatedAtUtc")]
    public DateTimeOffset GeneratedAtUtc { get; set; }

    /// <summary>Publisher party filter applied to the report, when supplied.</summary>
    [JsonPropertyName("publisherPartyId")]
    public string? PublisherPartyId { get; set; }

    /// <summary>Subscriber party filter applied to the report, when supplied.</summary>
    [JsonPropertyName("subscriberPartyId")]
    public string? SubscriberPartyId { get; set; }

    /// <summary>Status filter applied to the report, when supplied.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Inclusive lower created-at timestamp filter applied to the report, when supplied.</summary>
    [JsonPropertyName("fromUtc")]
    public DateTimeOffset? FromUtc { get; set; }

    /// <summary>Inclusive upper created-at timestamp filter applied to the report, when supplied.</summary>
    [JsonPropertyName("toUtc")]
    public DateTimeOffset? ToUtc { get; set; }

    /// <summary>Maximum number of records requested after filtering.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    /// <summary>Total number of records matching the filters before the limit is applied.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>Number of records returned in this report.</summary>
    [JsonPropertyName("returnedCount")]
    public int ReturnedCount { get; set; }

    /// <summary>Trace records included in the report.</summary>
    [JsonPropertyName("records")]
    public List<TransactionManifestTraceRecord> Records { get; set; } = [];
}

/// <summary>
/// Canonical signed manifest that accompanies an encrypted transaction diffgram.
/// FR-MCP-120, FR-MCP-123.
/// </summary>
public sealed class TransactionManifestDto
{
    /// <summary>Stable transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Optional session-log turn identifier associated with this transaction.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>Party identifier for the publisher that signs the manifest.</summary>
    [JsonPropertyName("publisherPartyId")]
    public string PublisherPartyId { get; set; } = string.Empty;

    /// <summary>Party identifier for the subscriber that can decrypt the diffgram.</summary>
    [JsonPropertyName("subscriberPartyId")]
    public string SubscriberPartyId { get; set; } = string.Empty;

    /// <summary>Publisher signing key identifier used to verify the manifest.</summary>
    [JsonPropertyName("publisherSigningKeyId")]
    public string? PublisherSigningKeyId { get; set; }

    /// <summary>Subscriber encryption key identifier used to decrypt the diffgram.</summary>
    [JsonPropertyName("subscriberEncryptionKeyId")]
    public string? SubscriberEncryptionKeyId { get; set; }

    /// <summary>Monotonic publisher sequence number used for replay protection.</summary>
    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }

    /// <summary>Nonce used for replay detection and audit correlation.</summary>
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>UTC issue timestamp.</summary>
    [JsonPropertyName("issuedAtUtc")]
    public DateTimeOffset IssuedAtUtc { get; set; }

    /// <summary>UTC expiry timestamp.</summary>
    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>SHA-256 digest of the plaintext diffgram payload.</summary>
    [JsonPropertyName("diffgramSha256")]
    public string DiffgramSha256 { get; set; } = string.Empty;

    /// <summary>SHA-256 digest of the encrypted diffgram payload.</summary>
    [JsonPropertyName("encryptedBodySha256")]
    public string EncryptedBodySha256 { get; set; } = string.Empty;

    /// <summary>Algorithm suite used to canonicalize, sign, and encrypt the transaction.</summary>
    [JsonPropertyName("algorithms")]
    public TransactionManifestAlgorithms Algorithms { get; set; } = new();

    /// <summary>Signature block over the canonical manifest.</summary>
    [JsonPropertyName("signature")]
    public TransactionManifestSignatureDto? Signature { get; set; }
}

/// <summary>
/// Signature metadata attached to a transaction manifest.
/// FR-MCP-120, TR-MCP-CRYPTO-001.
/// </summary>
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

    /// <summary>UTC timestamp when the signature was generated.</summary>
    [JsonPropertyName("signedAtUtc")]
    public DateTimeOffset SignedAtUtc { get; set; }
}

/// <summary>
/// Subscriber commit request carrying a signed manifest and encrypted diffgram body.
/// FR-MCP-123, FR-MCP-124.
/// </summary>
public sealed class DiffgramCommitRequest
{
    /// <summary>Signed transaction manifest.</summary>
    [JsonPropertyName("manifest")]
    public TransactionManifestDto Manifest { get; set; } = new();

    /// <summary>Base64-encoded encrypted diffgram body.</summary>
    [JsonPropertyName("encryptedDiffgramBase64")]
    public string EncryptedDiffgramBase64 { get; set; } = string.Empty;

    /// <summary>SHA-256 digest of the encrypted diffgram payload.</summary>
    [JsonPropertyName("encryptedBodySha256")]
    public string EncryptedBodySha256 { get; set; } = string.Empty;

    /// <summary>SHA-256 digest of the plaintext diffgram payload.</summary>
    [JsonPropertyName("diffgramSha256")]
    public string DiffgramSha256 { get; set; } = string.Empty;
}

/// <summary>
/// Subscriber commit response for a transactional diffgram.
/// FR-MCP-123, FR-MCP-124.
/// </summary>
public sealed class DiffgramCommitResponse
{
    /// <summary>Commit status, such as <c>committed</c>, <c>duplicate</c>, or <c>rejected</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured result or failure reason.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason Reason { get; set; }

    /// <summary>Stable transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Optional persisted diffgram identifier.</summary>
    [JsonPropertyName("diffgramId")]
    public string? DiffgramId { get; set; }

    /// <summary>Optional UTC timestamp when the diffgram was committed.</summary>
    [JsonPropertyName("committedAtUtc")]
    public DateTimeOffset? CommittedAtUtc { get; set; }
}

/// <summary>
/// Subscriber status response for a known transaction.
/// FR-MCP-122, FR-MCP-124.
/// </summary>
public sealed class TransactionStatusResponse
{
    /// <summary>Stable transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Current transaction status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured result or failure reason.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason? Reason { get; set; }

    /// <summary>Optional UTC timestamp when the diffgram was committed.</summary>
    [JsonPropertyName("committedAtUtc")]
    public DateTimeOffset? CommittedAtUtc { get; set; }

    /// <summary>Optional UTC timestamp when the transaction was aborted.</summary>
    [JsonPropertyName("abortedAtUtc")]
    public DateTimeOffset? AbortedAtUtc { get; set; }
}

/// <summary>
/// Request to abort a transaction before the subscriber commits it.
/// FR-MCP-122, FR-MCP-124.
/// </summary>
public sealed class TransactionAbortRequest
{
    /// <summary>Structured reason for aborting the transaction.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason Reason { get; set; }

    /// <summary>Optional actor or component that requested the abort.</summary>
    [JsonPropertyName("actor")]
    public string? Actor { get; set; }
}

/// <summary>
/// Subscriber response after a transaction abort.
/// FR-MCP-122, FR-MCP-124.
/// </summary>
public sealed class TransactionAbortResponse
{
    /// <summary>Stable transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Current transaction status after abort.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured reason for the abort.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason Reason { get; set; }

    /// <summary>UTC timestamp when the transaction was aborted.</summary>
    [JsonPropertyName("abortedAtUtc")]
    public DateTimeOffset AbortedAtUtc { get; set; }
}

/// <summary>
/// Turn-transaction gate status returned by <c>/mcpserver/turntransactions/status</c>.
/// </summary>
public sealed class TurnTransactionStatusResponse
{
    /// <summary>Whether turn transactions are enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Whether the transaction pipeline is degraded.</summary>
    [JsonPropertyName("degraded")]
    public bool Degraded { get; set; }

    /// <summary>Last recorded transaction failure reason.</summary>
    [JsonPropertyName("lastReason")]
    public TransactionFailureReason LastReason { get; set; }

    /// <summary>Last transaction identifier associated with the current status.</summary>
    [JsonPropertyName("lastTransactionId")]
    public string? LastTransactionId { get; set; }

    /// <summary>Human-readable status message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Persisted transaction pub/sub message state returned by diagnostics endpoints.
/// </summary>
public sealed class TransactionPubSubMessageStatus
{
    /// <summary>Stable operation identifier.</summary>
    [JsonPropertyName("operationId")]
    public string OperationId { get; set; } = string.Empty;

    /// <summary>Transaction identifier associated with the message.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Message kind.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Pub/sub topic name.</summary>
    [JsonPropertyName("topicName")]
    public string TopicName { get; set; } = string.Empty;

    /// <summary>Subscriber identifier.</summary>
    [JsonPropertyName("subscriberId")]
    public string SubscriberId { get; set; } = string.Empty;

    /// <summary>Current persisted message status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Number of replay or delivery attempts.</summary>
    [JsonPropertyName("attemptCount")]
    public int AttemptCount { get; set; }

    /// <summary>Structured result or failure reason.</summary>
    [JsonPropertyName("reason")]
    public TransactionFailureReason Reason { get; set; }

    /// <summary>UTC timestamp when the message was created.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the message was last updated.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>
/// Result returned after replaying persisted transaction pub/sub messages.
/// </summary>
public sealed class TransactionPubSubReplayResult
{
    /// <summary>Number of messages attempted during replay.</summary>
    [JsonPropertyName("attemptedCount")]
    public int AttemptedCount { get; set; }

    /// <summary>Number of messages acknowledged during replay.</summary>
    [JsonPropertyName("acknowledgedCount")]
    public int AcknowledgedCount { get; set; }

    /// <summary>Number of messages still pending after replay.</summary>
    [JsonPropertyName("pendingCount")]
    public int PendingCount { get; set; }
}

/// <summary>
/// Result returned after purging completed transaction pub/sub messages.
/// </summary>
public sealed class TransactionPubSubRetentionResult
{
    /// <summary>Cutoff timestamp used for the purge.</summary>
    [JsonPropertyName("completedBeforeUtc")]
    public DateTimeOffset CompletedBeforeUtc { get; set; }

    /// <summary>Maximum number of messages considered for purge.</summary>
    [JsonPropertyName("maxMessages")]
    public int MaxMessages { get; set; }

    /// <summary>Number of completed messages purged.</summary>
    [JsonPropertyName("purgedCount")]
    public int PurgedCount { get; set; }

    /// <summary>Number of pending messages retained.</summary>
    [JsonPropertyName("retainedPendingCount")]
    public int RetainedPendingCount { get; set; }
}

/// <summary>
/// Structured transaction failure reason codes shared by keyserver and subscriber APIs.
/// FR-MCP-124, TR-MCP-TXNAUDIT-001.
/// </summary>
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

    /// <summary>The manifest expiry timestamp has passed.</summary>
    ExpiredManifest = 6,

    /// <summary>The manifest issue timestamp is too far in the future.</summary>
    FutureManifest = 7,

    /// <summary>The manifest nonce has already been observed.</summary>
    ReplayNonce = 8,

    /// <summary>The publisher sequence is not newer than the last accepted sequence.</summary>
    StaleSequence = 9,

    /// <summary>The signature block is malformed.</summary>
    MalformedSignature = 10,

    /// <summary>The manifest signature does not match the canonical manifest.</summary>
    ManifestSignatureMismatch = 11,

    /// <summary>The encrypted body hash does not match the manifest.</summary>
    EncryptedBodyHashMismatch = 12,

    /// <summary>The plaintext diffgram hash does not match the manifest after decryption.</summary>
    PlaintextDiffgramHashMismatch = 13,

    /// <summary>The manifest targets a different subscriber.</summary>
    WrongSubscriber = 14,

    /// <summary>The subscriber could not decrypt the diffgram body.</summary>
    DecryptFailed = 15,

    /// <summary>The transaction conflicts with a different payload for the same identifier.</summary>
    DuplicateConflict = 16,

    /// <summary>The transaction was explicitly aborted.</summary>
    Aborted = 17,

    /// <summary>The keyserver was unavailable.</summary>
    KeyServerUnavailable = 18,

    /// <summary>The subscriber was unavailable.</summary>
    SubscriberUnavailable = 19,

    /// <summary>The commit operation timed out.</summary>
    CommitTimeout = 20,

    /// <summary>The transaction pipeline is currently disabled by configuration.</summary>
    TransactionsDisabled = 21,

    /// <summary>The requested quad-model behavior is intentionally deferred.</summary>
    DeferredFeatureDisabled = 22
}
