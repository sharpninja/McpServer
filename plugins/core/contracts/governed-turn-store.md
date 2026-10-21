<!-- SPDX-License-Identifier: Apache-2.0 -->
# A8 durable turn store

This contract implements the existing QB-AGENT-001 A8 obligations under
FR-QB-002/003/005, TR-QB-TRUST-001/AUDIT-001/RUNTIME-001 and
TEST-QB-005/008/009/014/069. It does not close those criteria. The ordinary
binding selection contract remains `governed-turn-binding.md`.

`Invoke-McpGovernedTurnStore` takes a trusted `Directory`, `Context` and an
operation with native-object `InputObject`. Bootstrap supplies the directory,
workspacePath, agentIdentity and a nonempty, duplicate-free `aliasConsumers`
array containing `qb-audit-outbox-v1`. These are not model-controlled tool
arguments. A directory belongs to exactly that context and consumer set.
Different contexts use different directories. No environment, mutable module
session, legacy current-turn cache, network or transport is consulted.

Every operation takes the directory's interprocess lock, reloads and validates
the complete snapshot, performs selection and mutation together, and publishes
one new snapshot only if state changes. Readers take the same lock. The stored
schema is version 1 with `workspacePath`, `agentIdentity`, `aliasConsumers`,
integer `revision`, `hosts` and `aliases`. Each host contains `hostSessionKey`
and `turns` as specified by the binding contract. Alias records contain the
eight original binding fields, plus `pendingId`, `aliasedAtUtc` and `acks`;
their requestId is the minted ID. Acks are consumer IDs, not event receipts.

Operations:

- `Register`: receives exactly the eight captured binding fields. Adds an
  open, undelivered turn without cancelling any other turn. A pending ID must
  equal `pending-<ingressNonce>`. Repeated identical ingress is unchanged,
  including replay under the old ID after its alias commits. Changed captured
  fields for an existing ingress, or an ID already owned by another ingress
  in that host, fail without mutation. A host cannot reuse the same ingress
  nonce for a different MCP session.
- `Deliver`: hostSessionKey plus explicit requestId marks actual delivery of
  ingress to the host. Minting/registration alone never does so. Both old and
  minted IDs address the same turn. Repeated delivery is unchanged. Terminal
  turns cannot be delivered again.
- `Resolve`: hostSessionKey and optional requestId delegate to the existing
  pure selector after alias resolution. Omitting requestId is distinct from
  supplying null. Multiple delivered open turns remain ambiguous. Explicit
  targeting does not make a mint-only turn ready for ordinary execution.
- `Alias`: receives all eight binding fields (requestId now minted), pendingId
  and aliasedAtUtc. All original fields other than requestId must match.
  Commit the alias and effective turn ID in one transaction before returning
  or publishing a wakeup. The old ID must be pending for this nonce, the new
  ID non-pending, the target open, and neither ID may belong to another turn.
  Identical replies are unchanged, preserving the first timestamp; a different
  minted ID or changed original binding is a final conflict. No chained alias.
- `ListAliases`: returns every committed alias for the supplied hostSessionKey,
  optionally filtered to those unacknowledged by a known consumerId. Every
  record includes full root attribution. A caller restart uses this durable
  list; notifications are only hints and never exactly-once evidence.
- `Acknowledge`: receives hostSessionKey, pendingId, minted requestId,
  mcpSessionId and consumerId. Requires an exact committed match and a known
  consumer. Append its acknowledgement once. The consumer calls this only
  after its own durable application. Aliases remain retained even after all
  consumers acknowledge; this implementation does not garbage-collect them.
- `Terminal`: hostSessionKey and explicit requestId mark only the selected
  turn terminal. Repetition is unchanged. This is an internal state transition
  after the independent persistence/completion gate, not a completion grant.

Each result contains exactly `ok`, `code`, `retryable`, `binding`, `aliases`
and `state`. State is an independent snapshot; binding uses the selector's
read-only eight-field dictionary. Expected refusals preserve state and never
report success. The selector's codes are retained. Additional final codes are
`ingress_nonce_invalid`, `alias_conflict`, `consumer_unknown`, `store_corrupt`
and `context_mismatch`. Storage I/O failures and lock contention return
`store_unavailable` with retryable true; the host's existing bounded degraded
persistence policy applies. A corrupt existing snapshot is never empty state.

Snapshot bytes are serialized from native objects, flushed to a same-directory
staging file, and atomically published. Failed staging evidence is retained.
The directory and state/lock files cannot be symbolic links. Storage and lock
paths are derived only from the trusted directory, never from identifiers.
The process-crash contract is old complete state before publication or new
complete state after publication. A committed alias survives a crash before
its reply; restart enumerates it and does not remint. Machine power-loss
guarantees beyond the filesystem's flush/atomic-publication contract are not
inferred from process-crash tests.

`GovernedTurnStore.Contract.ps1` first exercises independently specified
scenario responses and rejected negative controls through the same assertions
used for real storage. It then verifies real restart, interrupted publication,
corruption refusal and concurrent writers in isolated directories. The store
does not rewrite audit event payloads, counters or filenames; the outbox
consumer must implement and prove those existing invariants before P1 closes.
Legacy A3/A5/A8 cache migration, S6 mint/review transport, decision triples and
application integration remain required subsequent work, with no legacy-cache
or Node-host fallback authorized by this contract.
