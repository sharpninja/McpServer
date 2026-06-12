# Turn Transactions Design Round 2

Status: Phase 0 implementable design artifact, updated after the durable-storage, protected-envelope crypto, external signing-key material, keyserver signing-key rotation, signed manifest trace ledger, and subscriber key-ring rotation slices

Requirements: FR-MCP-118 through FR-MCP-128, TR-MCP-TXNDESIGN-001

Current implemented scope: transaction keyserver, subscriber, and coordinator behavior is implemented through shared core services under `src/McpServer.TransactionSecurity`, Support.Mcp compatibility controllers under `src/McpServer.Support.Mcp`, public DTO/client contracts under `src/McpServer.Client`, separate hosts under `src/McpServer.KeyServer` and `src/McpServer.Subscriber`, real separate-host integration coverage under `tests/McpServer.TransactionSecurity.IntegrationTests`, durable service-local SQLite keyserver/subscriber storage, keyserver signing/verification replay nonce and sequence hardening, protected subscriber diffgram envelopes, coordinator protected-envelope handoff for configured subscriber keys, external key material support for subscriber private ECDH decrypt keys and keyserver publisher signing private PEM re-provisioning, keyserver signing-key rotation that preserves prior public descriptors for historic manifest verification while old private signing material remains verify-only, signed manifest trace persistence with keyserver/controller/client lookup coverage, subscriber encryption private key rings that decrypt old and rotated protected envelopes, and a test-only aiUnit plan-review gate under `tests/McpServer.PlanReview.Tests`. External pub-sub, global mutation adapters, production key-lifecycle automation, and recovery/degraded rollback smoke coverage are deferred.

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

## Entity Model

Keyserver durable records implemented in the service-local SQLite state store:

- `KeyServerPartyEntity`: party ID, role, status, active signing key ID, active encryption key ID, created/updated UTC.
- `KeyServerPartyKeyEntity`: key ID, party ID, key purpose, algorithm, public key material, status, created UTC, expires UTC.
- `KeyServerManifestEntity`: transaction ID, turn ID, publisher ID, subscriber ID, sequence, nonce, hashes, issued/expiry UTC, signature metadata, manifest hash, status, created UTC.
- `KeyServerReplayNonceEntity`: scoped nonce, party pair, transaction ID, first seen UTC.
- `KeyServerSequenceEntity`: scoped party pair, last accepted sequence, updated UTC.
- `KeyServerAuditEventEntity`: action, reason code, transaction ID, party IDs, timestamp, details JSON.

Subscriber durable records implemented in the service-local SQLite state store:

- `SubscriberCommitEntity`: transaction ID, diffgram ID, manifest hash, sequence, status, committed UTC, aborted UTC.
- `SubscriberRejectionEntity`: transaction ID, diffgram ID, reason code, reason text, timestamp.
- `SubscriberAbortEntity`: transaction ID, reason code, requested UTC, actor.
- `SubscriberReplayNonceEntity`: nonce, transaction ID, first seen UTC.
- `SubscriberAuditEventEntity`: action, reason code, transaction ID, timestamp, details JSON.

## Canonicalization

- Manifest canonicalization version: `transaction-manifest-v1`.
- Payload encoding: UTF-8.
- Property order: fixed by canonicalizer, not reflection or serializer default order.
- Hash format: lowercase hexadecimal SHA-256.
- Signature algorithm label: `ECDSA-P256-SHA256`.
- Diffgram encryption label: `ECDH-P256-HKDF-SHA256-AES-256-GCM` for protected subscriber envelopes and coordinator protected-envelope handoff; global adapter encryption handoff remains deferred.
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
- `GET /mcpserver/keyserver/parties/{partyId}/keys/{keyId}`
- `GET /health`

Subscriber endpoints:

- `POST /mcpserver/subscriber/diffgrams/commit`
- `GET /mcpserver/subscriber/transactions/{transactionId}/status`
- `POST /mcpserver/subscriber/transactions/{transactionId}/abort`
- `GET /health`

## Options

Keyserver options:

- Section: `Mcp:KeyServer`
- `DatabasePath`
- `ManifestTtlSeconds`
- `MaxClockSkewSeconds`
- `SigningKeyPath`
- `AuditEnabled`

Subscriber options:

- Section: `Mcp:Subscriber`
- `DatabasePath`
- `PartyId`
- `EncryptionKeyId`
- `EncryptionPrivateKeyPem`
- `EncryptionKeys[]` with `KeyId` and `PrivateKeyPem` for key-ring rotation.
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

The Support.Mcp compatibility host keeps in-process wiring for existing endpoints. The separate subscriber host uses `Mcp:Subscriber:KeyServerBaseUrl` through an HTTP-backed keyserver verifier; integration tests inject a TestServer-backed keyserver `HttpClient` to prove cross-host behavior without relying on Kestrel ports.

## Test Mapping

- `TEST-MCP-158`: partial keyserver unit/contract coverage for the shared-core and separate-host first slice.
- `TEST-MCP-159`: partial subscriber unit/contract coverage for the shared-core and separate-host first slice.
- `TEST-MCP-160`: complete real keyserver/subscriber integration coverage for valid commit plus tampered, stale, and encrypted-body-mismatch rejection.
- `TEST-MCP-161`: partial MCP transaction coordinator coverage for focused commit/degraded paths and protected-envelope handoff.
- `TEST-MCP-164`: complete aiUnit plan review for FR-MCP-124 with run-log evidence under `artifacts/aiunit-plan-review`.
- `TEST-MCP-165`: complete imported diagram preservation coverage.
- `TEST-MCP-166` through `TEST-MCP-169`: partial diagram-derived coverage for focused first-slice paths.
- `TEST-MCP-170`: planned deferred-scope enforcement coverage.
- `TEST-MCP-171` through `TEST-MCP-173`: partial architecture/design conformance coverage; complete automated gate coverage remains deferred.

## Round 2 Gap Analysis

- DTOs, interfaces, entities, options, endpoints, reason codes, canonicalization, and audit payloads are named before implementation.
- Each public model and service surface has an XMLDoc obligation.
- Each acceptance criterion has a corresponding test record, validation artifact, or explicit deferred state.
- Config defaults are safe: transaction execution is disabled by default and no secret material is logged.
- aiUnit remains test-only through `tests/McpServer.PlanReview.Tests`; no production project references `SharpNinja.aiUnit`.
