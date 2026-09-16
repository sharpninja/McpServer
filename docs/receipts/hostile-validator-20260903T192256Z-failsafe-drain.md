# Hostile validation receipt: failsafe drain after MCP storage recovery (class-2 ops)

TimestampUtc: 2026-09-03T19:31:21Z
ValidatorIdentity: GrokSubagentHostile
Agent: GrokCode
SessionId: GrokCode-20260903T192500Z-hostile-failsafe-drain
RequestId: req-20260903T192500Z-001-hostile-validate-failsafe-drain
add-profile: executed yes. Profile file count read: 18 (every non-skill `*.md` under `C:\Users\kingd\.claude\profile`; excluded `add-profile.grok.md`). Independent listing at 2026-09-03T19:31:21Z: AllMd=19, NonSkill=18.

Work class: class 2 (user-directed operator action). Operator directed upload/drain of failsafe records after MCP storage recovered. No product UI implementation claim. No PLAN-WEB-ORCH-001 done claim.

Surface C: N/A
Surface D: N/A

Default was FAIL or UNKNOWN until this pass independently: executed add-profile (18 files); opened a dedicated hostile session; listed live failsafe pending YAML; deserialized remaining records with `Read-McpYamlObject`; queried native `requirements_list` type=fr/tr/test/mapping; queried `todo_get PLAN-WEB-ORCH-001`; queried session logs; read the drain-1 `pwsh` tool_result JSON at 2026-09-03T19:22:56.6625423Z. Did not re-run `workflow.failsafe.drain` (that would mutate remaining files). Did not delete remaining failsafe files.

Accuracy rating: 97/100. Live FR-WEB-001..020, AC on FR-WEB-003/010, 14 TR-WEB, 20 TEST-WEB, 20 FR-WEB mappings, drain-1 stdout, and remaining YAML error text all reproduced. DrainAttempts and remaining-file count drifted after drain-1 because later drains ran.
Completeness rating: 94/100. Drain-1 counts come from the implementer's machine `pwsh` tool_result (not a second drain). Live queue is 2 files, not the drain-1 set of 4. Tonkotsu pending YAML (3 invalid-session-id records) was not in the GrokCode drain directory and was not claimed.

## A Requested claims

A1. `workflow.failsafe.drain` via plugin repl-invoke scanned 37, replayed 33, failed 4, quarantined 0, aborted false, drainExit 0 at 2026-09-03T19:22:56Z.
Verdict: PASS
Evidence: Parent session tool_result `call-ea1bfc49-da08-43c6-a1df-8289e4841747-199` (Duration 54.40s, Window #14192). JSON: utc=2026-09-03T19:22:56.6625423Z drainExit=0 drainOut=`failsafeDir: F:\GitHub\McpServer\.mcpServer\failsafe\GrokCode\workspaces\RjpcR2l0SHViXE1jcFNlcnZlcg\pending` scanned=37 replayed=33 failed=4 quarantined=0 skipped=0 aborted=false abortReason empty. pendingCountAfter=4. pendingNames: `20260902T145530Z-session_recovery-3abc.yaml`, `20260902T145530Z-session_submit-5f02.yaml`, `20260902T150548Z-session_actions-a466.yaml`, `20260902T150548Z-session_submit-72ac.yaml`. quarantineCountAfter=0. Command was `pwsh.exe -NoProfile -NonInteractive -File ...\repl-invoke.ps1 -Method workflow.failsafe.drain`. This validator did not re-invoke drain.

A2. Requirements createBatch and 20 mapping yaml files are gone from pending (replayed).
Verdict: PASS
Evidence: Recursive listing of `F:\GitHub\McpServer\.mcpServer\failsafe` pending `*.yaml` at review: createBatch HITS=0, mapping HITS=0. Historical paths from `docs/receipts/_web-orch-20260902/failsafe-write-receipt.json` (`20260902T150548Z-requirements_create_batch-990e.yaml` plus 20 `requirements_create_mapping-01..20-*.yaml`) are absent. Live `requirements_list` type=mapping: MAP_TOTAL=330, FR_WEB_MAP_COUNT=20, MAP_MISSING empty. Drain deletes a record only after submit success.

A3. Live store has FR-WEB-001 through FR-WEB-020 (20). FR-WEB-003 has ac-1..ac-4. FR-WEB-010 has ac-1..ac-5.
Verdict: PASS
Evidence: Independent `mcpserver__requirements_list` type=fr parsed at 2026-09-03T19:26:46Z. FR_TOTAL=330. FR_WEB_COUNT=20. MISSING empty. EXTRA empty. Consecutive ids FR-WEB-001..020, all status=pending. FR-WEB-003 title=`Agent fleet lifecycle and status` acIds=ac-1,ac-2,ac-3,ac-4. FR-WEB-010 title=`Token burn and daily progress` acIds=ac-1,ac-2,ac-3,ac-4,ac-5. Corroboration: type=tr TR_WEB=14 (API, BUDGET, CTX, GIT, OBS, ORCH, PRIV, SCHED, SEC-001, SEC-002, SESS, TODO, UI-001, UI-002). type=test TEST_WEB=20 TEST-WEB-001..020 TEST_MISSING empty.

A4. Four pending files remain with drainAttempts 1: session_recovery-3abc (no schema for workflow.sessionlog.recovery), session_submit-5f02 and session_submit-72ac (HTTP 400 planFile omitted), session_actions-a466 (No active session exists). Not quarantined.
Verdict: PASS
Evidence: Drain-1 inventory in the 19:22:56.6625423Z tool_result listed exactly those four names, pendingCountAfter=4, quarantineCountAfter=0. Live deserialize at 2026-09-03T19:27:00Z of remaining GrokCode pending YAML: recovery method=`workflow.sessionlog.recovery` lastDrainError contains `No YAML schema is registered for method 'workflow.sessionlog.recovery'`; actions method=`workflow.sessionlog.appendActions` lastDrainError contains `No active session exists`. Quarantine directories under `.mcpServer/failsafe`: FILECOUNT 0. session_submit-5f02 and session_submit-72ac HITS=0 on disk now.
Residual (not a FAIL of the timestamped drain-1 claim): live GrokCode pending is 2 files, not 4. drainAttempts on both remaining files is 3, not 1. lastDrainError requestIds are `req-20260903T192659Z-7d75` and `req-20260903T192659Z-f10d` (later drain, including auto-drain around 19:26:59Z). Implementer drain-2 after this hostile spawn patched planFile/todoId on the two session_submit records and replayed them. Tonkotsu still has 3 unrelated invalid-session-id YAML files outside the GrokCode drain dir.

A5. Did not claim product UI implemented or PLAN done.
Verdict: PASS
Evidence: Implementer completeTurn `req-20260903T191900Z-002-upload-failsafe-records` response: `PLAN-WEB-ORCH-001 not implemented`. `todo_get PLAN-WEB-ORCH-001` returned `TODO 'PLAN-WEB-ORCH-001' not found`. Plan file `docs/plans/PLAN-WEB-ORCH-001.md` Status=`requirements captured; waiting for operator approval before implementation`. Grep of that plan for `- [x]` / `- [X]`: no matches.

## B Workspace rules

B1. Honesty / no fabricated results.
Verdict: PASS
Evidence: Drain-1 counts match the 19:22:56 tool_result. Live FR-WEB set matches A3. Remaining error text matches recovery/actions claims. Implementer session turn already disclosed drain-2 replay of the two session_submit files. Live 2-file queue matches that later disclosure, not a hidden delete.

B2. Always bring the receipts.
Verdict: PASS
Evidence: This review re-queried live requirements, listed pending YAML, deserialized remaining records, and cited the drain-1 JSON stdout. Durable receipt pair written under `docs/receipts/`. Did not treat chat narrative as the gate.

B3. MCP-only TODO/session/requirements storage.
Verdict: PASS
Evidence: This review did not read or write `todo.yaml` or requirements markdown as a store substitute. Session log used MCP tools (`sessionlog_open`, `sessionlog_begin_turn`, `sessionlog_dialog`, `sessionlog_complete_turn`, `sessionlog_query`). Failsafe pending YAML was inspected, not deleted. Implementer drain used plugin `workflow.failsafe.drain` plus object YAML mutation of failsafe queue files, not TODO.yaml.

B4. Lab PowerShell / no Python.
Verdict: PASS
Evidence: Drain command was `pwsh.exe -NoProfile -NonInteractive -File ...repl-invoke.ps1`. This review used `pwsh` / PowerShell.MCP and native MCP tools. No `python` / `python3` / `py`.

B5. Byrd Development Process v4 phase-order.
Verdict: N/A
Evidence: Class-2 operator action (failsafe drain). Byrd v4 applies only to project implementation code/docs. Not scored as FAIL. Plan still says wait for approval; no implementation-done claim.

B6. Look-before-delete.
Verdict: PASS
Evidence: Remaining failsafe files were not deleted by this review. Drain code deletes a record only after successful replay. Quarantine count 0. Two session_submit files left pending after successful later replay, which is the documented success path.

## C Requirement violations

Verdict: N/A
Evidence: User-directed ops (upload/drain failsafe after storage recovery). Not project requirement work. Do not FAIL for missing FR/TR/TEST on the drain action itself. Live FR-WEB set was verified as claim A3, not as a product-complete gate.

## D Current plan holistically

Verdict: N/A
Evidence: Implementer did not claim a plan step complete. `docs/plans/PLAN-WEB-ORCH-001.md` is still waiting for operator approval. Ops drain does not have to satisfy product plan DoD.

## FAIL list

(none)

## UNKNOWN list

(none)

## Residual list (not FAIL)

- Live GrokCode pending is 2 YAML files with drainAttempts=3, not the drain-1 set of 4 with drainAttempts=1.
- `session_submit-5f02` and `session_submit-72ac` are gone after later replay.
- Three Tonkotsu invalid-session-id pending files remain outside the drained GrokCode directory.

## Counts

PASS: 11 (A1-A5, B1-B4, B6)
FAIL: 0
UNKNOWN: 0
N/A: 3 (B5, C, D)

## OverallVerdict

AGREE

All applicable surfaces of A+B+C+D PASS. Live FR-WEB-001 through FR-WEB-020 is proven. Remaining GrokCode failures still match the claimed error types (no YAML schema for `workflow.sessionlog.recovery`; no active session for `appendActions`) and are not quarantined. C and D are N/A for this class-2 action and do not block AGREE.

## Session log proof

SessionId: GrokCode-20260903T192500Z-hostile-failsafe-drain
RequestId: req-20260903T192500Z-001-hostile-validate-failsafe-drain
Turn opened via `sessionlog_begin_turn` (turnId 43182, status in_progress at begin). Persistence proof is the post-complete `sessionlog_query` result recorded in the JSON twin.

## Receipt files

- Markdown: `docs/receipts/hostile-validator-20260903T192256Z-failsafe-drain.md`
- JSON: `docs/receipts/hostile-validator-20260903T192256Z-failsafe-drain.json`
