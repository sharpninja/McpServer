# TR-MCP-SESSIONLIFE

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-SESSIONLIFE-001

**Plugin keeps degraded turn cache and honest hook status** — Complete-ReplBeginTurnAfterPersist must not replace the turn file. Open-PluginTurn emits turn-opened-degraded while degraded. appendDialog 404 on a degraded local turn resubmits or queues session_dialog.
**Covered by:** FR: FR-MCP-SESSIONLIFE-001; TEST: TEST-MCP-SESSIONLIFE-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] One real begin-turn builder or shim preserves the complete cached turn on degraded persistence and exposes a shared, truthful degraded transition used by all hooks.
- [ ] Prompt, complete, update, and dialog hooks consume the shared transition without reduced-object replacement, empty-query overwrite, or false server-persisted status.
- [ ] Degraded missing-turn dialog recovery is bounded to one submit attempt and one retained dialog failsafe, while unrelated not-found remains nonretryable.

## TR-MCP-SESSIONLIFE-002

**Metadata durability and freshness rebind** — Snapshot cached metadata before replacing turn state. Same-request beginTurn is omit-unbound only when durable. Assert-ReplCurrentTurnFresh must not rewrite a cached session id. Three documentation files state ordinary rejection, supersede None, and durable reopen omission.
**Covered by:** FR: FR-MCP-SESSIONLIFE-002; TEST: TEST-MCP-SESSIONLIFE-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A single durability-first metadata resolver implements explicit, verified-cache, and None precedence for planFile and todoId.
- [ ] Workspace, agent, session, and request identity are immutable after binding; freshness checks diagnose drift and never rebind from markers or inherited variables.
- [ ] Ordinary first persistence, canceled/cancelled supersession, durable reopen, and all three contract documents use the same resolver semantics.

## TR-MCP-SESSIONLIFE-003

**Wrapper three-outcome exit contract** — Invoke-McpPlugin reports primary success, classified queued success, or nonzero failure. appendActions honors requestId. updateTurn dedupes. Child REPL has one deadline with process-tree cleanup.
**Covered by:** FR: FR-MCP-SESSIONLIFE-003; TEST: TEST-MCP-SESSIONLIFE-002, TEST-MCP-SESSIONLIFE-004
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The wrapper exposes a typed primary, queued, or failed outcome contract and no session-log mutation collapses that result to an unclassified boolean.
- [ ] Result serialization includes identity, method, durability, queue, retryability, diagnostic, and failsafe fields without claiming primary success for queued work.
- [ ] Child REPL execution uses one operation deadline with deterministic process-tree cleanup, and dedupe preserves sequential method labels and queue counts.
- [ ] Existing-session graph materialization avoids multiplicative collection joins, remains update-tracked, and bounds transient SQL Server deadlock or timeout handling inside the storage command budget without converting retryable contention into primary success.

## TR-MCP-SESSIONLIFE-004

**Quarantine repair, import recovery, and contention drain** — Repair the three named quarantine files. Transcript ingestion scans importRecovery yaml and deletes only the envelope after durable success. McpErrorClassifier and both drain entry points keep contention retryable and unlatched.
**Covered by:** FR: FR-MCP-SESSIONLIFE-004; TEST: TEST-MCP-SESSIONLIFE-002, TEST-MCP-SESSIONLIFE-004, TEST-MCP-SESSIONLIFE-005
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A shared replay and quarantine repair core parses supported historical object shapes, validates canonical identity and paths, and preserves recovery artifacts on any failure.
- [ ] Both drain entry points use one contention classifier and identical attempt, latch, ordering, and delete-after-durable-success rules; true storage outage also drives health.storage=unreachable while contention remains retryable.
- [ ] Import recovery follows the canonical session persistence path, supports validated canonical-only or reconstructed bundles, and never exposes transcript ingestion through agent plugins.
- [ ] Replay enforces workspace containment, reparse containment, file and record bounds, cancellation, content identity, idempotency, and concurrency safety before destructive cleanup.

## TR-MCP-SESSIONLIFE-005

**Stop-hook stale pin and upsert uniqueness** — Close-PluginTurnIfNeeded runs build and audit gates first and allows a stale pin only with authoritative server completion. SessionLogSchemaGuard, SessionLogService.UpsertTurnAsync, and SessionLogController implement predecessor-schema detection, details.inner, and one uniqueness retry.
**Covered by:** FR: FR-MCP-SESSIONLIFE-005; TEST: TEST-MCP-SESSIONLIFE-002, TEST-MCP-SESSIONLIFE-004
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Stop enforcement obtains exact-turn server proof through the supported typed client after build and audit gates and before any state mutation.
- [ ] A shared cache-state lock snapshots identity and content hashes, releases for bounded network I/O, then conditionally mutates only if the same state remains current.
- [ ] Schema predecessor detection and provider error classification occur before mutation and preserve useful credential-safe details.inner.
- [ ] Upsert retries once only for the known same-session/request unique race and merges the winning row without duplicate child collections.

