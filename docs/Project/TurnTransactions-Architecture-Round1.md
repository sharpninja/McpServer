# Turn Transactions Architecture Round 1

Status: Phase 0 architecture artifact

Requirements: FR-MCP-118 through FR-MCP-128, TR-MCP-TXNARCH-001

## Component Boundaries

- `McpServer.Support.Mcp` remains the main MCP Server host. It owns user-turn lifecycle, mutation gating, diffgram generation, degraded-mode enforcement, session-log/audit actions, and typed-client configuration.
- `McpServer.KeyServer` is a new service. It owns keyserver signing keys, party public-key registry, manifest signing, manifest verification, replay detection, sequence validation, expiry validation, and keyserver audit rows.
- `McpServer.Subscriber` is a new service. It owns subscriber private decrypt/signing material, durable commit state, idempotency/conflict checks, abort state, rejection reasons, and subscriber audit rows.
- `McpServer.Client` owns public DTOs and typed clients for keyserver, subscriber, and transaction status surfaces.
- Quad-model execution, Curiosity research, AoT reconciliation execution, and weight updates are documented but disabled by default in this slice.

## Key And Crypto Ownership

- Keyserver owns only the private key used to sign transaction manifests.
- Keyserver stores registered party public keys and public key metadata.
- Publisher/MCP Server owns publisher private signing/encryption material when publisher-origin signatures are added.
- Subscriber owns subscriber private ECDH material and never sends it to keyserver or MCP Server.
- Manifests bind party IDs, key IDs, hashes, algorithms, nonce, sequence, issued UTC, and expiry UTC.
- Existing `Mcp:Federation` HMAC signing remains unchanged and separate.

## Storage Boundaries

- Keyserver uses service-local durable EF Core SQLite storage for parties, keys, manifests, replay nonces, sequence cursors, and audit events.
- Subscriber uses service-local durable EF Core SQLite storage for commits, aborts, rejection records, manifest hashes, sequence cursors, and audit events.
- MCP Server stores only transaction coordination state and audit/session-log references required for the user turn.
- Rollback never deletes audit rows for sign, verify, commit, reject, abort, degraded, or rollback actions.

## Trust Boundaries

- MCP Server calls keyserver to sign manifests before publishing mutating diffgrams.
- Subscriber verifies keyserver-signed manifests before commit.
- Subscriber commits must complete before MCP Server returns committed success.
- All cross-service calls have bounded timeouts. Signing failures and verification failures do not retry automatically because retries can mask stale sequence/replay defects. Health probes may retry outside mutation flow.
- Degraded mode is explicit and only permits health, status, and context reads.

## Threat Model

- Replay: nonce and transaction sequence are tracked by keyserver and subscriber.
- Stale sequence: manifests with old or non-monotonic sequence are rejected.
- Wrong subscriber: subscriber party/key ID mismatch rejects before decrypt.
- Manifest tamper: canonical signature verification rejects.
- Body tamper: encrypted body SHA-256 and plaintext diffgram SHA-256 checks reject.
- Keyserver outage: mutating turn rejects or enters degraded mode based on config.
- Subscriber outage: mutating turn enters degraded mode or aborts based on config.
- Duplicate commit: identical commit is idempotent; mismatched duplicate is conflict.
- Rollback: rollback preserves audit and user-visible transaction state.
- Federation compatibility: existing HMAC federation envelopes are not modified by transaction crypto.

## Round 1 Gap Analysis

- Private key ownership is explicit: keyserver does not own publisher/subscriber private keys.
- Subscriber ACID boundary is explicit: subscriber durable SQLite commit store is authoritative for commit state.
- Imported diagrams are preserved in `Quad-Model-Transactional-Diffgram-Plan.md`.
- aiUnit review is test-only and does not touch production services.
- Degraded mode allowlist is explicit: health/status/context reads only, plus audit of degradation.
- Endpoint/config defaults are fixed for implementation tests.
- Audit action names are fixed by TR-MCP-TXNAUDIT-001.
- Federation HMAC compatibility is an explicit requirement and test target.

## Decisions

- Decision: build keyserver and subscriber as solution projects in this repo, not separate repositories.
- Decision: use service-local SQLite stores for keyserver and subscriber in v1.
- Decision: keep transaction crypto additive and separate from federation HMAC envelopes.
- Decision: keep quad execution disabled until future requirements authorize it.
- Decision: require diagram-derived tests before implementation of each in-scope branch.
