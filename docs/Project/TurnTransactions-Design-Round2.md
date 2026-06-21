# Turn Transactions Design Round 2

Status: Phase 0 implementable design artifact, updated after the durable-storage, protected-envelope crypto, external signing-key material, keyserver signing-key rotation, signed manifest trace ledger/reporting, subscriber key-ring rotation, separate-host subscriber key-ring configuration, pending subscriber status, global federation adapter transaction-gating, production key-file provisioning, durable pub-sub outbox/replay, and external broker/fan-out/replay-retention slices

Requirements: FR-MCP-118 through FR-MCP-128, TR-MCP-TXNDESIGN-001

Current implemented scope: transaction keyserver, subscriber, and coordinator behavior is implemented through shared core services under `src/McpServer.TransactionSecurity`, Support.Mcp compatibility controllers under `src/McpServer.Support.Mcp`, public DTO/client contracts under `src/McpServer.Client`, separate hosts under `src/McpServer.KeyServer` and `src/McpServer.Subscriber`, real separate-host integration coverage under `tests/McpServer.TransactionSecurity.IntegrationTests`, durable service-local SQLite keyserver/subscriber storage, keyserver signing/verification replay nonce and sequence hardening, protected subscriber diffgram envelopes, coordinator protected-envelope handoff for configured subscriber keys, external key material support for subscriber private ECDH decrypt keys and keyserver publisher signing private PEM re-provisioning, keyserver signing-key rotation that preserves prior public descriptors for historic manifest verification while old private signing material remains verify-only, signed manifest trace persistence with keyserver/controller/client lookup and filtered report coverage, subscriber encryption private key rings that decrypt old and rotated protected envelopes, separate-host subscriber key-ring configuration binding coverage, separate-host startup provisioning for file-backed publisher signing and subscriber encryption key material, subscriber transaction status lifecycle reporting for pending/committed/rejected/aborted states, a broker-neutral transaction pub-sub seam with direct, HTTP external subscriber, and external process/topic broker adapters for commit/abort handoff, configured multi-subscriber fan-out, durable local broker-backed pub-sub outbox/replay for commit and abort handoffs through in-memory or SQLite state, durable topic/subscriber status identity, stale in-progress durable pub-sub replay lease recovery, Support.Mcp pub-sub status/replay/retention endpoints, a background replay/retention worker, deterministic high-volume/high-contention durable pub-sub stress coverage, concurrent durable replay backlog coverage, concurrent coordinator timeout stress coverage, durable timeout rollback cancellation coverage, optional mutation rollback compensation for post-mutation subscriber/degraded failures with additive audit evidence, cancellation of durable pending commit handoffs after successful rollback compensation, global federation mutation-adapter apply gating through the turn transaction coordinator, first native Support.Mcp memory add/update/delete mutation gating for REST, typed REPL-over-HTTP, and in-process MCP stdio paths, typed REPL TODO create/update/updateSelected/delete/deleteSelected transaction gating through `TransactionalTodoWorkflow`, server-side TODO create/update/delete/move transaction gating for compensation-capable providers, generic REPL protected namespace blocking for unsafe federation/keyserver/subscriber calls, federation control-plane fail-closed gating, and a test-only aiUnit plan-review gate under `tests/McpServer.PlanReview.Tests`. Surfaces without a compensation contract either fail closed while required mutation transactions are active or remain explicitly deferred as future scope.

## Public DTOs

Add transaction security models under `McpServer.Client.Models`:

- `PartyRegistrationRequest`
- `PartyRegistrationResponse`
- `PartyKeyDescriptor`
- `TransactionManifestSignRequest`
- `TransactionManifestSignResponse`
- `TransactionManifestVerifyRequest`
- `TransactionManifestVerifyResponse`
- `TransactionManifestTraceRecord`
- `TransactionManifestTraceReportRequest`
- `TransactionManifestTraceReport`
- `TransactionManifestDto`
- `TransactionManifestSignatureDto`
- `DiffgramCommitRequest`
- `DiffgramCommitResponse`
- `TransactionStatusResponse`
- `TransactionAbortRequest`
- `TransactionAbortResponse`
- `TransactionFailureReason`

Every public model and member must have XMLDocs and requirement references.

## Service Interfaces

Keyserver:

- `IKeyServerManifestService`
- `IKeyServerPartyRegistry`
- `ITransactionManifestCanonicalizer`
- `ITransactionManifestSigner`
- `ITransactionReplayGuard`
- `IKeyServerAuditSink`

Subscriber:

- `ISubscriberCommitService`
- `ITransactionManifestVerifier`
- `ITransactionDiffgramProtector`
- `ISubscriberReplayGuard`
- `ISubscriberAuditSink`

MCP Server:

- `ITurnTransactionCoordinator`
- `IDiffgramBuilder`
- `ITransactionDegradedModePolicy`
- `ITransactionAuditWriter`
- `ITransactionPubSub`
- `ITransactionPubSubReplayService`

## Entity Model

Keyserver durable records implemented in the service-local SQLite state store:

- `KeyServerPartyEntity`: party ID, role, status, active signing key ID, active encryption key ID, created/updated UTC.
- `KeyServerPartyKeyEntity`: key ID, party ID, key purpose, algorithm, public key material, status, created UTC, expires UTC.
- `KeyServerManifestEntity`: transaction ID, turn ID, publisher ID, subscriber ID, sequence, nonce, hashes, issued/expiry UTC, signature metadata, manifest hash, status, created UTC.
- `KeyServerReplayNonceEntity`: scoped nonce, party pair, transaction ID, first seen UTC.
- `KeyServerSequenceEntity`: scoped party pair, last accepted sequence, updated UTC.
- `KeyServerAuditEventEntity`: action, reason code, transaction ID, party IDs, timestamp, details JSON.

Subscriber durable records implemented in the service-local SQLite state store:

- `SubscriberTransactionEntity`: transaction ID, status (`pending`, `committed`, `rejected`, or `aborted`), reason code, manifest hash, encrypted body hash, diffgram ID, committed UTC, aborted UTC.
- `SubscriberSequenceEntity`: scoped party pair, last accepted sequence, updated UTC.
- `SubscriberReplayNonceEntity`: scoped nonce, transaction ID, first seen UTC.
- `SubscriberAuditEventEntity`: action, reason code, transaction ID, timestamp, details JSON.

Pub-sub durable records implemented in the service-local SQLite state store:

- `TransactionPubSubMessageEntity`: deterministic operation ID (`commit:{transactionId}` or `abort:{transactionId}`), transaction ID, kind, status (`pending` or `acknowledged`), serialized request JSON, optional acknowledgement JSON, attempt count, last reason code, created UTC, updated UTC.

## Canonicalization

- Manifest canonicalization version: `transaction-manifest-v1`.
- Payload encoding: UTF-8.
- Property order: fixed by canonicalizer, not reflection or serializer default order.
- Hash format: lowercase hexadecimal SHA-256.
- Signature algorithm label: `ECDSA-P256-SHA256`.
- Diffgram encryption label: `ECDH-P256-HKDF-SHA256-AES-256-GCM` for protected subscriber envelopes and coordinator protected-envelope handoff; global federation adapter applies use the same coordinator handoff when routed through transactions.
- Verification compares signatures and hashes in constant-time APIs where applicable.

## Reason Codes

Initial `TransactionFailureReason` values:

- `None`
- `UnknownParty`
- `DisabledParty`
- `UnknownKey`
- `DisabledKey`
- `ExpiredManifest`
- `FutureManifest`
- `ReplayNonce`
- `StaleSequence`
- `MalformedSignature`
- `ManifestSignatureMismatch`
- `EncryptedBodyHashMismatch`
- `PlaintextDiffgramHashMismatch`
- `WrongSubscriber`
- `DecryptFailed`
- `DuplicateConflict`
- `Aborted`
- `KeyServerUnavailable`
- `SubscriberUnavailable`
- `CommitTimeout`
- `TransactionsDisabled`
- `DeferredFeatureDisabled`

## Endpoint Contracts

Keyserver endpoints:

- `POST /mcpserver/keyserver/parties`
- `POST /mcpserver/keyserver/manifests/sign`
- `POST /mcpserver/keyserver/manifests/verify`
- `GET /mcpserver/keyserver/manifests/{transactionId}`
- `GET /mcpserver/keyserver/manifests/report`
- `GET /mcpserver/keyserver/parties/{partyId}/keys/{keyId}`
- `GET /health`

Subscriber endpoints:

- `POST /mcpserver/subscriber/diffgrams/commit`
- `GET /mcpserver/subscriber/transactions/{transactionId}/status`
- `POST /mcpserver/subscriber/transactions/{transactionId}/abort`
- `GET /health`

`TransactionStatusResponse.Status` reports `pending` while the subscriber has accepted a transaction ID for validation but has not reached a terminal result; normal completion transitions the durable row to `committed`, `rejected`, or `aborted`.

## Options

Keyserver options:

- Section: `Mcp:KeyServer`
- `DatabasePath`
- `ManifestTtlSeconds`
- `MaxClockSkewSeconds`
- `AuditEnabled`
- `ProvisionedParties[]` with party ID, role, active key IDs, public PEM values or public PEM file paths, and signing private PEM values or signing private PEM file paths for startup provisioning.

Subscriber options:

- Section: `Mcp:Subscriber`
- `DatabasePath`
- `PartyId`
- `EncryptionKeyId`
- `EncryptionPrivateKeyPem`
- `EncryptionPrivateKeyPemFile`
- `EncryptionKeys[]` with `KeyId`, `PrivateKeyPem`, and `PrivateKeyPemFile` for key-ring rotation.
- `RequireEncryptedDiffgrams`
- `KeyServerBaseUrl`
- `CommitTimeoutSeconds`
- `AuditEnabled`

MCP Server:

- Section: `Mcp:TurnTransactions`
- `Enabled=false`
- `RequiredForMutations=true`
- `DegradedModeEnabled=true`
- `CommitTimeoutSeconds=30`
- `KeyServerBaseUrl=http://localhost:7167`
- `SubscriberBaseUrl=http://localhost:7168`
- `PubSubTransport=Direct`
- `DurablePubSubEnabled=false`
- `PubSubDatabasePath`
- `PubSubInProgressClaimLeaseSeconds=300`

The Support.Mcp compatibility host keeps in-process wiring for existing endpoints. The separate subscriber host uses `Mcp:Subscriber:KeyServerBaseUrl` through an HTTP-backed keyserver verifier; integration tests inject a TestServer-backed keyserver `HttpClient` to prove cross-host behavior without relying on Kestrel ports.

## Test Mapping

- `TEST-MCP-158`: complete keyserver unit/contract coverage for the shared-core and separate-host first slice.
- `TEST-MCP-159`: complete subscriber unit/contract coverage for commit, abort, replay, protected envelopes, key-ring rotation, durable status, in-flight pending status, high-contention duplicate commits, abort/commit race handling, high-volume durable pub-sub commit settlement, high-contention duplicate durable pending attempt accounting, and concurrent durable replay backlog draining.
- `TEST-MCP-160`: complete real keyserver/subscriber integration coverage for valid commit, tampered, stale, encrypted-body-mismatch rejection, configuration-bound subscriber key-ring rotation, and file-backed production key provisioning.
- `TEST-MCP-161`: complete MCP transaction coordinator coverage for focused commit/degraded paths, concurrent commit timeout handling, durable timeout rollback cancellation, protected-envelope handoff, direct, HTTP, and external broker transaction pub-sub handoff, required-subscriber fan-out, durable pub-sub outbox/replay, stale in-progress replay lease recovery, durable topic/subscriber persistence, replay management endpoints, replay/retention worker behavior, retention purge behavior, durable pub-sub high-volume/high-contention stress behavior, concurrent durable replay backlog draining, global federation adapter apply gating, federation control-plane fail-closed gating, Support.Mcp memory add/update/delete gating, typed REPL TODO create/update/updateSelected/delete/deleteSelected gating, server-side TODO create/update/delete/move gating, EF TODO compensation snapshot restore, atomic EF capture/update, rollback-failure reporting for commit rejection and local partial-failure aborts, ISSUE-backed update rejection, HTTP PUT routing, stdio `todo_update` routing, and stdio coordinator registration.
- `TEST-MCP-162`: complete traceability/import coverage proving FR-MCP-118 through FR-MCP-128, transaction TR records, TEST-MCP-158 through TEST-MCP-173, and live TODO references resolve without placeholder transaction-plan entries.
- `TEST-MCP-163`: complete deferred-scope documentation coverage proving remaining future autonomous Quad-Model, runtime/control-plane, delayed-rollback isolation, remote/runtime compensation, quarantine/fine-tuning automation, implicit fallback, and key-rotation automation work remains explicit instead of being silently claimed complete.
- `TEST-MCP-164`: complete aiUnit plan review for FR-MCP-126 with run-log evidence under `artifacts/aiunit-plan-review`.
- `TEST-MCP-165`: complete imported diagram preservation coverage.
- `TEST-MCP-166` through `TEST-MCP-169`: complete diagram-derived coverage for focused first-slice paths.
- `TEST-MCP-170`: complete authorization-scope enforcement coverage proving implemented Quad-Brain branches execute only through FR-MCP-129 through FR-MCP-135 gates and remaining future branches stay fail-closed.
- `TEST-MCP-171` through `TEST-MCP-173`: complete architecture/design conformance and traceability closeout coverage for PLAN-TURNTRANSACTIONS-001.

## Round 2 Gap Analysis

- DTOs, interfaces, entities, options, endpoints, reason codes, canonicalization, and audit payloads are named before implementation.
- Each public model and service surface has an XMLDoc obligation.
- Each acceptance criterion has a corresponding test record, validation artifact, or explicit deferred state.
- Config defaults are safe: transaction execution is disabled by default and no secret material is logged.
- aiUnit remains test-only through `tests/McpServer.PlanReview.Tests`; no production project references `SharpNinja.aiUnit`.
