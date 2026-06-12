# Turn Transactions Design Round 2

Status: Phase 0 implementable design artifact, updated after first slice

Requirements: FR-MCP-118 through FR-MCP-128, TR-MCP-TXNDESIGN-001

First-slice scope: transaction keyserver, subscriber, and coordinator behavior is implemented in-process under `src/McpServer.Support.Mcp`, with public DTO/client contracts under `src/McpServer.Client` and a test-only aiUnit plan-review gate under `tests/McpServer.PlanReview.Tests`. Separate `src/McpServer.KeyServer` and `src/McpServer.Subscriber` projects, durable DB-backed transaction storage/audit, real encryption/decryption/key management, and global mutation adapters are deferred.

## Public DTOs

Add transaction security models under `McpServer.Client.Models`:

- `PartyRegistrationRequest`
- `PartyRegistrationResponse`
- `PartyKeyDescriptor`
- `TransactionManifestSignRequest`
- `TransactionManifestSignResponse`
- `TransactionManifestVerifyRequest`
- `TransactionManifestVerifyResponse`
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
- `IDiffgramDecryptor`
- `ISubscriberReplayGuard`
- `ISubscriberAuditSink`

MCP Server:

- `ITurnTransactionCoordinator`
- `IDiffgramBuilder`
- `ITransactionDegradedModePolicy`
- `ITransactionAuditWriter`

## Entity Model

Keyserver durable entities (deferred after the first in-process slice):

- `KeyServerPartyEntity`: party ID, role, status, active signing key ID, active encryption key ID, created/updated UTC.
- `KeyServerPartyKeyEntity`: key ID, party ID, key purpose, algorithm, public key material, status, created UTC, expires UTC.
- `TransactionManifestEntity`: transaction ID, turn ID, publisher ID, subscriber ID, sequence, nonce, hashes, issued/expiry UTC, signature, status.
- `KeyServerReplayNonceEntity`: nonce, party pair, transaction ID, first seen UTC.
- `KeyServerAuditEventEntity`: action, reason code, transaction ID, party IDs, timestamp, details JSON.

Subscriber durable entities (deferred after the first in-process slice):

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
- Diffgram encryption label: `ECDH-P256-HKDF-SHA256-AES-256-GCM` (contract label reserved in the first slice; real encryption/decryption is deferred).
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
- `GET /mcpserver/keyserver/parties/{partyId}/keys/{keyId}`
- `GET /health`

Subscriber endpoints:

- `POST /mcpserver/subscriber/diffgrams/commit`
- `GET /mcpserver/subscriber/transactions/{transactionId}/status`
- `POST /mcpserver/subscriber/transactions/{transactionId}/abort`
- `GET /health`

## Options

Keyserver options (separate service extraction deferred):

- Section: `Mcp:KeyServer`
- `DatabasePath`
- `ManifestTtlSeconds`
- `MaxClockSkewSeconds`
- `SigningKeyPath`
- `AuditEnabled`

Subscriber options (separate service extraction deferred):

- Section: `Mcp:Subscriber`
- `DatabasePath`
- `PartyId`
- `PrivateKeyPath`
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

In the first slice, keyserver/subscriber behavior is in-process, so external base URLs are compatibility/extraction options rather than proof that separate services exist.

## Test Mapping

- `TEST-MCP-158`: partial keyserver unit/contract coverage for the in-process first slice.
- `TEST-MCP-159`: partial subscriber unit/contract coverage for the in-process first slice.
- `TEST-MCP-160`: planned real keyserver/subscriber integration coverage after separate services exist.
- `TEST-MCP-161`: partial MCP transaction coordinator coverage for focused commit/degraded paths.
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
