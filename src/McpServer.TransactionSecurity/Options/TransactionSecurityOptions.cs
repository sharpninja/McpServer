namespace McpServer.TransactionSecurity.Options;

/// <summary>
/// FR-MCP-118: Keyserver runtime options bound from <c>Mcp:KeyServer</c>.
/// The first implementation stores generated keys in memory and uses these options
/// for manifest time windows and clock-skew validation.
/// </summary>
public sealed class KeyServerOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:KeyServer";

    /// <summary>Default manifest lifetime in seconds.</summary>
    public int ManifestTtlSeconds { get; set; } = 300;

    /// <summary>Maximum tolerated manifest clock skew in seconds.</summary>
    public int MaxClockSkewSeconds { get; set; } = 300;

    /// <summary>Whether keyserver audit event capture is enabled.</summary>
    public bool AuditEnabled { get; set; } = true;

    /// <summary>
    /// Optional SQLite database path for durable keyserver party, key descriptor, and audit state.
    /// Empty uses process-local in-memory state.
    /// </summary>
    public string? DatabasePath { get; set; }

    /// <summary>
    /// Optional parties to register from deployment configuration at service startup.
    /// This supports re-provisioning external private signing material without storing it in durable state.
    /// </summary>
    public List<KeyServerProvisionedPartyOptions> ProvisionedParties { get; set; } = [];
}

/// <summary>Deployment-configured keyserver party registration material. TR-MCP-KEYSERVER-001.</summary>
public sealed class KeyServerProvisionedPartyOptions
{
    /// <summary>Stable party identifier used in manifests and key lookups.</summary>
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Party role, such as publisher or subscriber.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Optional active signing key identifier.</summary>
    public string? ActiveSigningKeyId { get; set; }

    /// <summary>Optional active encryption key identifier.</summary>
    public string? ActiveEncryptionKeyId { get; set; }

    /// <summary>Optional PEM-encoded signing public key.</summary>
    public string? SigningPublicKeyPem { get; set; }

    /// <summary>Optional path to PEM-encoded signing public key material.</summary>
    public string? SigningPublicKeyPemFile { get; set; }

    /// <summary>Optional PEM-encoded ECDSA private key used by the keyserver to sign manifests for this party.</summary>
    public string? SigningPrivateKeyPem { get; set; }

    /// <summary>Optional path to PEM-encoded ECDSA private signing key material.</summary>
    public string? SigningPrivateKeyPemFile { get; set; }

    /// <summary>Optional PEM-encoded encryption public key.</summary>
    public string? EncryptionPublicKeyPem { get; set; }

    /// <summary>Optional path to PEM-encoded encryption public key material.</summary>
    public string? EncryptionPublicKeyPemFile { get; set; }

    /// <summary>Registration status. Defaults to active.</summary>
    public string Status { get; set; } = "active";
}

/// <summary>
/// FR-MCP-119: Subscriber runtime options bound from <c>Mcp:Subscriber</c>.
/// </summary>
public sealed class SubscriberOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:Subscriber";

    /// <summary>Local subscriber party identifier. Empty means the manifest subscriber is accepted.</summary>
    public string? PartyId { get; set; }

    /// <summary>Commit timeout in seconds.</summary>
    public int CommitTimeoutSeconds { get; set; } = 30;

    /// <summary>Base URL for the separate keyserver host used by HTTP-backed subscriber verification.</summary>
    public string KeyServerBaseUrl { get; set; } = "http://localhost:7167";

    /// <summary>Whether subscriber audit event capture is enabled.</summary>
    public bool AuditEnabled { get; set; } = true;

    /// <summary>
    /// Optional SQLite database path for durable transaction, nonce, sequence, and audit state.
    /// Empty uses process-local in-memory state.
    /// </summary>
    public string? DatabasePath { get; set; }

    /// <summary>Optional subscriber encryption key identifier that corresponds to <see cref="EncryptionPrivateKeyPem"/>.</summary>
    public string? EncryptionKeyId { get; set; }

    /// <summary>Optional PEM-encoded ECDH private key used to decrypt protected diffgram envelopes.</summary>
    public string? EncryptionPrivateKeyPem { get; set; }

    /// <summary>Optional path to PEM-encoded ECDH private key material used to decrypt protected diffgram envelopes.</summary>
    public string? EncryptionPrivateKeyPemFile { get; set; }

    /// <summary>
    /// Optional key ring of PEM-encoded ECDH private keys used to decrypt protected diffgram envelopes
    /// across subscriber encryption key rotations.
    /// </summary>
    public List<SubscriberEncryptionKeyMaterial> EncryptionKeys { get; set; } = [];

    /// <summary>Whether commits must carry protected diffgram envelopes instead of legacy placeholder bodies.</summary>
    public bool RequireEncryptedDiffgrams { get; set; }

    /// <summary>
    /// FR-MCP-SUBLOG-001: High-performance message-log sink for received transaction messages (Parseable).
    /// Bound from <c>Mcp:Subscriber:Parseable</c>. Disabled by default.
    /// </summary>
    public SubscriberParseableOptions Parseable { get; set; } = new();
}

/// <summary>
/// FR-MCP-SUBLOG-001: Parseable sink options for high-performance subscriber message logging,
/// bound from <c>Mcp:Subscriber:Parseable</c>.
/// </summary>
public sealed class SubscriberParseableOptions
{
    /// <summary>Whether received-message logging to Parseable is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Parseable base URL (for example <c>http://localhost:8000</c>). Required when enabled.</summary>
    public string? Url { get; set; }

    /// <summary>Parseable stream name (<c>X-P-Stream</c> header).</summary>
    public string StreamName { get; set; } = "mcp-subscriber";

    /// <summary>Basic-auth username.</summary>
    public string Username { get; set; } = "admin";

    /// <summary>Basic-auth password.</summary>
    public string Password { get; set; } = "admin";
}

/// <summary>Subscriber private encryption key material for one key-ring entry. FR-MCP-119.</summary>
public sealed class SubscriberEncryptionKeyMaterial
{
    /// <summary>Subscriber encryption key identifier that matches manifest and envelope metadata.</summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>PEM-encoded ECDH private key used to decrypt envelopes for <see cref="KeyId"/>.</summary>
    public string PrivateKeyPem { get; set; } = string.Empty;

    /// <summary>Optional path to PEM-encoded ECDH private key material used to decrypt envelopes for <see cref="KeyId"/>.</summary>
    public string? PrivateKeyPemFile { get; set; }
}

/// <summary>
/// FR-MCP-120 and FR-MCP-121: Turn transaction coordinator options bound from
/// <c>Mcp:TurnTransactions</c>. The coordinator is disabled by default so existing
/// mutation paths remain unchanged until explicit adapter slices opt in.
/// </summary>
public sealed class TurnTransactionOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:TurnTransactions";

    /// <summary>Whether turn transaction gating is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Whether mutations require committed subscriber confirmation when enabled.</summary>
    public bool RequiredForMutations { get; set; } = true;

    /// <summary>Whether dependency failures enter degraded mode instead of hard rejection.</summary>
    public bool DegradedModeEnabled { get; set; } = true;

    /// <summary>Commit timeout in seconds.</summary>
    public int CommitTimeoutSeconds { get; set; } = 30;

    /// <summary>Default publisher party identifier used by the coordinator.</summary>
    public string PublisherPartyId { get; set; } = "mcpserver";

    /// <summary>Default subscriber party identifier used by the coordinator.</summary>
    public string SubscriberPartyId { get; set; } = "subscriber-1";

    /// <summary>Optional subscriber encryption key identifier used when protecting coordinator diffgrams.</summary>
    public string? SubscriberEncryptionKeyId { get; set; }

    /// <summary>Whether the coordinator should encrypt diffgram bodies before subscriber commit.</summary>
    public bool ProtectDiffgrams { get; set; }

    /// <summary>Future external keyserver base URL. Current implementation uses the in-process service.</summary>
    public string KeyServerBaseUrl { get; set; } = "http://localhost:7167";

    /// <summary>Future external subscriber base URL. Current implementation uses the in-process service.</summary>
    public string SubscriberBaseUrl { get; set; } = "http://localhost:7168";

    /// <summary>
    /// Optional external subscriber base URLs used for HTTP fan-out delivery.
    /// When empty, <see cref="SubscriberBaseUrl"/> remains the single HTTP subscriber target.
    /// </summary>
    public List<string> SubscriberBaseUrls { get; set; } = [];

    /// <summary>Transaction pub-sub delivery transport. Defaults to direct in-process subscriber delivery.</summary>
    public TransactionPubSubTransport PubSubTransport { get; set; } = TransactionPubSubTransport.Direct;

    /// <summary>External broker process-launch options used by the process/topic pub-sub adapter.</summary>
    public TransactionPubSubBrokerProcessOptions PubSubBrokerProcess { get; set; } = new();

    /// <summary>Logical broker topics used for commit, abort, acknowledgement, and dead-letter envelopes.</summary>
    public TransactionPubSubTopicOptions PubSubTopics { get; set; } = new();

    /// <summary>Configured pub-sub subscribers used for HTTP fan-out or external broker subscriber envelopes.</summary>
    public List<TransactionPubSubSubscriberOptions> PubSubSubscribers { get; set; } = [];

    /// <summary>Fan-out acknowledgement policy for multi-subscriber delivery.</summary>
    public TransactionPubSubFanOutMode PubSubFanOutMode { get; set; } = TransactionPubSubFanOutMode.AllRequired;

    /// <summary>
    /// Whether commit and abort handoff messages are persisted before delivery and replayable after transient failures.
    /// </summary>
    public bool DurablePubSubEnabled { get; set; }

    /// <summary>
    /// Optional SQLite database path for durable pub-sub commit and abort message state.
    /// Empty uses process-local in-memory state for the durable wrapper.
    /// </summary>
    public string? PubSubDatabasePath { get; set; }

    /// <summary>
    /// Number of seconds before an in-progress durable pub-sub replay claim is considered stale and reclaimable.
    /// </summary>
    public int PubSubInProgressClaimLeaseSeconds { get; set; } = 300;

    /// <summary>Whether the Support.Mcp background worker should replay durable pub-sub messages.</summary>
    public bool PubSubReplayWorkerEnabled { get; set; } = true;

    /// <summary>Number of seconds between durable pub-sub replay worker cycles.</summary>
    public int PubSubReplayIntervalSeconds { get; set; } = 15;

    /// <summary>Maximum durable pub-sub messages attempted by one replay worker cycle or default endpoint call.</summary>
    public int PubSubReplayBatchSize { get; set; } = 100;

    /// <summary>Whether terminal durable pub-sub message retention purging is enabled.</summary>
    public bool PubSubRetentionEnabled { get; set; } = true;

    /// <summary>Number of seconds to retain acknowledged or canceled durable pub-sub messages.</summary>
    public int PubSubTerminalRetentionSeconds { get; set; } = 604800;

    /// <summary>Maximum completed durable pub-sub messages purged by one retention cycle.</summary>
    public int PubSubRetentionBatchSize { get; set; } = 500;
}

/// <summary>External broker process-launch options for transaction pub-sub. FR-MCP-121.</summary>
public sealed class TransactionPubSubBrokerProcessOptions
{
    /// <summary>Broker executable path. Empty disables process-backed broker publishing.</summary>
    public string? ExecutablePath { get; set; }

    /// <summary>Optional broker command-line arguments.</summary>
    public string? Arguments { get; set; }

    /// <summary>Optional broker working directory.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Broker publish timeout in seconds.</summary>
    public int PublishTimeoutSeconds { get; set; } = 30;
}

/// <summary>Logical broker topics for transaction pub-sub envelopes. FR-MCP-121.</summary>
public sealed class TransactionPubSubTopicOptions
{
    /// <summary>Commit envelope topic.</summary>
    public string CommitTopic { get; set; } = "mcp.turntransactions.commit";

    /// <summary>Abort envelope topic.</summary>
    public string AbortTopic { get; set; } = "mcp.turntransactions.abort";

    /// <summary>Acknowledgement envelope topic.</summary>
    public string AcknowledgementTopic { get; set; } = "mcp.turntransactions.ack";

    /// <summary>Dead-letter envelope topic.</summary>
    public string DeadLetterTopic { get; set; } = "mcp.turntransactions.deadletter";
}

/// <summary>Configured subscriber target for transaction pub-sub fan-out. FR-MCP-121.</summary>
public sealed class TransactionPubSubSubscriberOptions
{
    /// <summary>Stable subscriber identifier used in broker envelopes and status rows.</summary>
    public string SubscriberId { get; set; } = string.Empty;

    /// <summary>Optional transaction-security party identifier represented by this subscriber.</summary>
    public string? PartyId { get; set; }

    /// <summary>HTTP subscriber base URL for direct HTTP fan-out delivery.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Optional commit topic override for this subscriber.</summary>
    public string? CommitTopic { get; set; }

    /// <summary>Optional abort topic override for this subscriber.</summary>
    public string? AbortTopic { get; set; }

    /// <summary>Whether this subscriber must acknowledge successfully for the fan-out operation to be accepted.</summary>
    public bool Required { get; set; } = true;
}

/// <summary>Fan-out acknowledgement policy for multi-subscriber transaction pub-sub. FR-MCP-121.</summary>
public enum TransactionPubSubFanOutMode
{
    /// <summary>All required subscribers must acknowledge before the operation is considered accepted.</summary>
    AllRequired,
}

/// <summary>Supported transaction pub-sub delivery transports. FR-MCP-121.</summary>
public enum TransactionPubSubTransport
{
    /// <summary>Deliver commit and abort messages directly to the configured subscriber service.</summary>
    Direct,

    /// <summary>Deliver commit and abort messages to an external subscriber host over HTTP.</summary>
    Http,

    /// <summary>Publish commit and abort envelopes to an external process/topic broker adapter.</summary>
    ExternalBroker,
}
