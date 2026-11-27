# TEST-MCP-SESSIONLIFE

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-SESSIONLIFE

### TEST-MCP-SESSIONLIFE-001

SessionLogLifecycle.Tests.ps1 covers retained fields, hook status, 404 recovery, empty-query verbs, and same-request degraded beginTurn.

**Acceptance Criteria:**
- [ ] Mock-backed consumer tests prove degraded complete-turn preservation, truthful hook status, empty-query omission, bounded missing-turn dialog recovery, and same-request cached metadata.
- [ ] At least one real builder or shim test runs with MCP_PLUGIN_PERSIST_LOG unset and asserts the complete cached object and serialized persistence payload rather than only fixture echo.
- [ ] The accepted test scope exits with zero failures and zero skips and records the concrete test names and evidence paths in the acceptance manifest.

### TEST-MCP-SESSIONLIFE-002

SessionLogLifecycleMetadata, quarantine repair, audit reconcile, child deadline, and stop-hook pin tests.

**Acceptance Criteria:**
- [ ] Named Pester tests cover metadata precedence, immutable identity, ordinary first persist, canceled/cancelled supersession, durable reopen, and all three contract documents.
- [ ] Named process tests cover primary, queued, and lost wrapper exits, structured result serialization, single child deadline and cleanup, Stop ordering, quarantine repair, audit reconciliation, and dialog classification.
- [ ] Every mock-backed consumer case has a corresponding real implementation assertion and recorded process exit or serialized receipt; unaccepted candidate tests remain explicitly not accepted.
- [ ] The accepted cumulative test scope exits with zero failures, zero skips, no missing reports, and no zero-discovery projects.

### TEST-MCP-SESSIONLIFE-003

Reuse TransactionGatedSessionLogServiceTests bypass coverage and add query plus repeated additive complete proofs under FR-MCP-173.

**Acceptance Criteria:**
- [ ] Transaction-gated session-log tests prove non-QuadBrain action, dialog, update, and additive complete persistence without coordinator or keyserver invocation.
- [ ] Exact durable readback verifies every child collection and repeated additive complete remains duplicate-free.
- [ ] The subset proof is linked to FR-MCP-173 but does not mark its broader first-party adapter obligations satisfied.

### TEST-MCP-SESSIONLIFE-004

Schema guard, DbUpdateException details.inner, uniqueness retry, contention versus outage, dialog classification, and ImportRecoveryEnvelopeTests.

**Acceptance Criteria:**
- [ ] Named C# tests cover schema predecessor detection, credential-safe provider details.inner, the bounded same-turn unique race retry, contention versus outage including health.storage=unreachable only for actual storage outage, and dialog storage classification.
- [ ] Import recovery tests start from canonical and reconstructed bundles and cover missing source, traversal, reparse escape, malformed schema, size bounds, cancellation, concurrent replay, content-hash drift, and delete-only-after-verified-persistence.
- [ ] Provider-specific uniqueness and schema tests use disposable databases and leave no remnant; an approved prior migration is not reopened solely because infrastructure is degraded.
- [ ] The accepted cumulative test scope exits with zero failures, zero skips, no missing reports, and no zero-discovery projects.
- [ ] Provider-backed tests seed a dense session graph, update it through SubmitAsync, and prove bounded completion, exact child-collection readback, and no Cartesian query shape. Concurrent same-session submissions prove bounded retry or classified retryable failure with no partial or duplicate rows.
- [ ] A zero-skip matrix executes dense SubmitAsync and concurrent same-session updates against SQLite, SQL Server, and PostgreSQL. The SQL Server row covers error 1205 deadlock and five-second command-budget expiry, proves one normalized retryable outcome without false primary success or partial state, then proves exact readback after successful retry.

### TEST-MCP-SESSIONLIFE-005

Existing PluginPowerShellRuntime drain-timeout coverage and session_dialog method dispatch. Not expected red. Governed by FR-MCP-REPL-011.

**Acceptance Criteria:**
- [ ] Existing PluginPowerShellRuntime coverage proves drain SubmitAsync uses a 120-second default and honors a larger REPL_TIMEOUT instead of the ordinary two-second nested helper budget, automatic nested drain defers while a REPL call is active, and timeout leaves attempt and latch state unchanged.
- [ ] Existing dispatch coverage proves retained dialog recovery records use method client.SessionLog.AppendDialogAsync and preserve exact workspace, agent, session, and request identity.
- [ ] This proof-first scope stays green and does not authorize rewriting already-correct timeout behavior without a newly accepted failing real-code test.
