# TR-MCP-PERSIST

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-PERSIST-001

**Plugin appendDialog uses incremental dialog POST** — Invoke-WorkflowAppendDialog SHALL call SessionLogClient.AppendDialogAsync (or equivalent POST .../dialog) when a current turn exists. Failsafe for incremental dialog SHALL replay the dialog POST, not a full-session SubmitAsync upsert.

Acceptance Criteria:
- Pester proves appendDialog does not invoke client.SessionLog.SubmitAsync for an existing turn.
- Missing turn is not-found, retryable false.
**Covered by:** FR: FR-MCP-170; TEST: TEST-MCP-195, TEST-MCP-196, TEST-MCP-SESSIONLIFE-002, TEST-MCP-SESSIONLIFE-004
**Status:** pending
Scope: layer-1+

## TR-MCP-PERSIST-002

**PersistTurn maps 503 to timeout degrade-queue** — Invoke-ReplPersistTurn SHALL treat HTTP 503 and payload code backend_unavailable the same as timeout|timed out|command_timeout: return false, set LastReplPersistenceDetails degraded/queued, retain failsafe, do not throw.

Acceptance Criteria:
- Pester PersistTurn 503 does not throw and leaves failsafe on disk.
- current-turn.yaml remains in_progress.
**Covered by:** FR: FR-MCP-171; TEST: TEST-MCP-195
**Status:** pending
Scope: layer-1+

## TR-MCP-PERSIST-003

**Failsafe drain inspects errors without Write-Error Stop skip** — Invoke-ReplFailsafeDrain SHALL inspect Invoke-ReplRaw failures without Write-Error under ErrorActionPreference Stop skipping Test-ReplFailsafeBackendUnreachable. Drain of SessionLog.SubmitAsync SHALL abort on timeout/503 without incrementing drainAttempts and without latching ReplFailsafeDrainCompleted. Drain MUST NOT block the successful caller for the full 30s SubmitAsync timeout (bounded drain timeout or drain after return).

Acceptance Criteria:
- Pester: timeout/503 abort, no drain failed stderr, later replay works.
- getFr returns before 30s when a queued session_submit 503s.
**Covered by:** FR: FR-MCP-172; TEST: TEST-MCP-195
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Failsafe drain on SubmitAsync timeout or 503 aborts without incrementing drainAttempts and without latching ReplFailsafeDrainCompleted. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] getFr returns before 30s when a queued session_submit 503s. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Get-ReplMethodTimeoutSeconds during drain for client.SessionLog.SubmitAsync is REPL_FAILSAFE_DRAIN_TIMEOUT default 120, or REPL_TIMEOUT when greater, never hardcoded 2. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## TR-MCP-PERSIST-004

**SQLITE_BUSY under budget is persist contention not storage-down** — SessionLog SaveChanges SQLITE_BUSY or lock wait inside the storage command budget SHALL NOT be classified as backend_unavailable/storage-down when subsequent TODO/requirements reads succeed. Prefer SQLite busy-timeout/retry. GET /health remains process liveness plus nonce echo. Do not change /health liveness semantics.

Acceptance Criteria:
- Concurrent TODO query during SubmitAsync does not yield backend_unavailable when the DB file is valid.
- /health?nonce= still echoes the nonce.
**Covered by:** FR: FR-MCP-170; TEST: TEST-MCP-195, TEST-MCP-196, TEST-MCP-SESSIONLIFE-002, TEST-MCP-SESSIONLIFE-004
**Status:** pending
Scope: layer-1+

