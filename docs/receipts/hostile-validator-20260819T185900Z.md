# Hostile validator receipt

TimestampUtc: 2026-08-19T18:59:00Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation closeout of leftover BUG-TRIAGE-120)
ActivePlan: docs/plans/triage-cluster-002.md (G8 closeout-first)
GitBranch: develop
GitSha: 0620078259d0be441d953fbaf457b0fdb670dbbc
add-profile: executed yes; profile file count read: 18 (excluded add-profile.grok.md)

SessionLog:
- sessionId: GrokCode-20260819T185507Z-hostile-g8-120
- requestId: req-20260819T185507Z-001-hostile-g8-120-closeout
- persistence: sessionlog_query planFile=docs/plans/triage-cluster-002.md returned this session; turn status completed with dialog, designDecisions, and actions present (queried 2026-08-19T18:58:49Z)

OverallVerdict: DISAGREE

Counts: PASS 12, FAIL 3, UNKNOWN 0, N/A 0

## Claims reviewed

### A Requested validation

A1. Cluster 131 shipped degraded/queued handling and unified retryable envelope; named tests re-run Failed 0 Skipped 0.
Verdict: PASS
Evidence:
- Named Pester file plugins/core/test-fixtures/pester/TriagePluginIdentity.Tests.ps1 includes BeginTurn.SubmitTimeoutAfterFailsafe_ReturnsDegradedQueued and classified retryable shim.
- Pester NUnit xml docs/receipts/_hv-g8-120-pester.xml: total=10 errors=0 failures=0 skipped=0 not-run=0. All 10 Success including BeginTurn.SubmitTimeoutAfterFailsafe_ReturnsDegradedQueued.
- Support.Mcp trx docs/receipts/_hv-g8-120-dotnet.trx ResultSummary counters total=16 executed=16 passed=16 failed=0. Classes: SessionLogControllerErrorTests, McpToolErrorEnvelopeTests, McpToolBackendUnavailableErrorTests, McpErrorClassifierTests, GlobalExceptionHandlerBackendUnavailableTests.
- Repl.Core docs/receipts/_hv-g8-120-02.json: Failed 0 Passed 34 Skipped 0 Total 34 ExitCode 0. Filter ReplMcpErrorClassifierTests, SessionLogPersistenceDispatcherTests, SessionLogPersistenceStrategyTests, ContractCorrectnessTests.
- Production path exists: plugins/core/lib-ps/repl-invoke.ps1 Invoke-ReplPersistTurn timeout branch sets degraded/queued; Invoke-WorkflowBeginTurn calls Complete-ReplBeginTurnAfterPersist and returns true on degraded.

A2. Live plugin Status / short sessionlog_begin_turn with valid planFile/todoId completes or returns classified retryable without hanging this review.
Verdict: FAIL
Evidence:
- Plugin Status completed: docs/receipts/_hv-g8-120-04.json TimedOut=false ExitCode=0 ElapsedSec=3.405 status=available pendingCount=12.
- MCP-native sessionlog_begin_turn succeeded (turnId 42073) with planFile docs/plans/triage-cluster-002.md and todoId BUG-TRIAGE-120. That is not the 120 plugin persist path.
- Plugin workflow.sessionlog.beginTurn with the same planFile/todoId: docs/receipts/_hv-g8-120-06.json BeginTurn TimedOut=false ExitCode=1 ElapsedSec=40.809 BeginExceeded30s=true BeginClassified=failed-unclassified. StdErr: Plugin command timed out after 40s (Invoke-McpPlugin.ps1:289). Not degraded, not queued, not retryable.
- Combined collector earlier was killed at 300s while still in plugin Status/beginTurn sequence after tests finished. Wrapper-capped 40s is still an unclassified timeout, not the classified path.

A3. Failsafe pendingCount on the grok cache is not itself proof that 120 is unfixed; 503 drain is BUG-TRIAGE-159 (S2), not 120.
Verdict: PASS
Evidence:
- Plugin Status cacheDir F:\GitHub\McpServer\.mcpServer\grok pendingCount=12 failsafeCount=12 failsafeQuarantineCount=48.
- Live inventory docs/receipts/_hv-g8-120-05.json: 12 live YAML all KindGuess=session_submit (client.SessionLog.SubmitAsync). KindCounts timeout=0 and 503-or-backend_unavailable=0 in payload heads. Most payloads are superseded/canceled turns, not beginTurn timeout markers.
- Quarantine reason sample 20260816T174019Z-session_submit-4e3a.yaml.reason.txt: drain attempt budget / method_invocation_error internal_server_error on SubmitAsync, not a 30s beginTurn hang.
- MCP todo_get BUG-TRIAGE-159 Done=false; title is 503 backend_unavailable treated as failsafe record failure (plan G4 / S2).

A4. BUG-TRIAGE-120 still Done=false. PLAN-TRIAGELEFTOVER-001 still Done=false.
Verdict: PASS
Evidence:
- MCP todo_get BUG-TRIAGE-120 Done=false.
- MCP todo_get PLAN-TRIAGELEFTOVER-001 Done=false.
- MCP todo_get BUG-TRIAGE-131 Done=true (overlap item; not flipped by this review).

### B Workspace rules

B1. Byrd v4 phase-order scored at this closeout gate, not FR-vs-file timestamps.
Verdict: PASS
Evidence: This run is the G8 closeout hostile. No post-hoc createdAt vs LastWriteTime FAIL. 131 already Done=true; this review re-ran named tests and live AC.

B2. Receipts re-verified; old receipts not trusted.
Verdict: PASS
Evidence: Collectors under docs/receipts/_hv-g8-120-*.ps1 re-ran tests, HMAC, health, Status, beginTurn, failsafe inventory.

B3. MCP-only TODO/session/requirements storage.
Verdict: PASS
Evidence: TODO reads via todo_get. Session via sessionlog_* tools. No direct edit of todo.yaml or session-log files.

B4. PowerShell only; no Python.
Verdict: PASS
Evidence: pwsh.exe collectors only.

B5. Honesty: implementer test-count claim matched re-run artifacts.
Verdict: PASS
Evidence: A1 counts match xml/trx/json. Overclaim of live beginTurn is scored as A2, not fabricated test output.

### C Requirements

C1. Applicable FR/TR/TEST identified.
Verdict: PASS
Evidence: BUG-TRIAGE-120 FunctionalRequirements FR-MCP-TRIAGE-002, TechnicalRequirements TR-MCP-TRIAGE-004. Overlap FR-MCP-TRIAGEPLUGIN-001, TEST-MCP-TRIAGEPLUGIN-001/004, FR-MCP-TRIAGEERR-001, TR-MCP-TRIAGEPLUGIN-001. Plan reuses TRIAGEPLUGIN/TRIAGEERR for 120 closeout.

C2. Structured AC exist.
Verdict: PASS
Evidence: docs/Project/Functional-Requirements.md FR-MCP-TRIAGEPLUGIN-001 AC: beginTurn Submit timeout is degraded or queued with failsafe retained. Testing-Requirements.md TEST-MCP-TRIAGEPLUGIN-004 AC: beginTurn persist timeout after failsafe returns degraded/queued and retains failsafe. TODO 120 TechnicalDetails AC: no hang / failsafe / Status / drain.

C3. AC are testable for this scope.
Verdict: PASS
Evidence: The AC name timeout, degraded/queued, failsafe retain, and caller return. Testable.

C4. Tests cover each AC (suite green is not coverage).
Verdict: FAIL
Evidence: TEST-MCP-TRIAGEPLUGIN-004 AC requires persist timeout after failsafe to return degraded/queued. Pester It BeginTurn.SubmitTimeoutAfterFailsafe_ReturnsDegradedQueued calls Complete-ReplBeginTurnAfterPersist with Persisted=false Degraded=true; it does not time out SubmitAsync. Live plugin beginTurn did not return degraded/queued; it threw Plugin command timed out after 40s. FR-MCP-TRIAGEPLUGIN-001 AC checkbox remains unchecked in docs/Project/Functional-Requirements.md. Existing suite green does not prove the live persist-timeout AC.

C5. No missing leftover FR invent required for this closeout (reuse TRIAGEPLUGIN).
Verdict: PASS
Evidence: Plan S0 says reuse existing TRIAGEPLUGIN/TRIAGEERR TESTs for 120 closeout; do not duplicate IDs.

### D Plan holistically

D1. G8 closeout-first original AC on current develop.
Verdict: FAIL
Evidence: Plan G8 120: closeout-first; if DISAGREE only the FAIL list. Original AC (TODO plus brief): beginTurn/Submit over ~30s must not hang the caller indefinitely; classified retryable/degraded/queued is acceptable. On develop 06200782, plugin beginTurn with valid planFile/todoId exceeded 30s and failed unclassified at 40s. Closeout is not ready. Do not mark BUG-TRIAGE-120 done.

## Explicit FAIL list

1. A2: Live plugin workflow.sessionlog.beginTurn with planFile=docs/plans/triage-cluster-002.md and todoId=BUG-TRIAGE-120 did not complete and did not return classified retryable/degraded/queued. Invoke-McpPlugin threw after 40.809s (TimeoutSeconds 40). Evidence: docs/receipts/_hv-g8-120-06.json. S2 must make that caller return classified retryable or degraded/queued (or durable open) inside the short sessionlog budget without an unclassified wrapper timeout.
2. C4: TEST-MCP-TRIAGEPLUGIN-004 / FR-MCP-TRIAGEPLUGIN-001 persist-timeout AC is not covered by a real SubmitAsync timeout. Pester injects degraded flags. Live plugin path is unclassified 40s timeout. Suite green is not AC coverage.
3. D1: G8 120 closeout original AC is not met on current develop. Do not mark BUG-TRIAGE-120 or PLAN-TRIAGELEFTOVER-001 done from this review.

## Mandatory surfaces not evaluated

None. UNKNOWN count is 0.

## Trust bootstrap

- Marker signature Test-MarkerSignature True (docs/receipts/_hv-g8-120-01.json).
- Health nonce hv-20260819T184955Z-18988 echoed. storage=reachable. version 1.4.28+f4060f037e62e64974026aff9d24e11b2f481952.

Do not mark any MCP TODO done. Do not merge.
