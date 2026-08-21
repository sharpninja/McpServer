# Hostile validator receipt

TimestampUtc: 2026-08-20T12:07:03Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (MCP-store hygiene S0 leftover-get reattack). Not product implementation. Not user-directed ops.

add-profile: executed yes. Profile markdown files read in full: 18 (excluded add-profile.grok.md).

Locked scope: H0 leftover provenance. AGREE only if s0-leftover-get.json is a compact leftover todo_get and live leftover is Done true. Surface C N/A. S4-S7 not required.

Session log: beginTurn/completeTurn failed persist HTTP 503 backend_unavailable then 400 planFile omitted. Failsafe YAML under C:\Users\kingd\AppData\Local\Temp\grok-hostile-h0-s0-d1b\plugin-cache\failsafe. queryHistory does not contain this reattack title. Local current-turn.yaml in_progress for req-20260820T120443Z-001-hostile-h0-leftover-get. Retry on a fresh cache also beginTurn exit 1.

Trust: marker HMAC match true (verify.ps1). Health nonce echoed during leftover/ALIGN gets.

## OverallVerdict

DISAGREE

PASS: 12. FAIL: 1. UNKNOWN: 0. N/A: 1 (surface C).

## Explicit FAIL list

- B6 Reviewer session-log turn did not persist. workflow.sessionlog.beginTurn failed HTTP 503 backend_unavailable (failsafe 20260820T120452Z-session_submit-b09c.yaml). Later append/complete 400 planFile omitted. Retry beginTurn also exit 1. queryHistory has no "Hostile H0 leftover compact get" / leftover-get reattack title. Per adversarial-review-global a review without a persisted turn is incomplete even when leftover claims pass.

## A Requested validation

### A1 s0-leftover-get.json is compact leftover get, not a 900KB done:true list

Verdict: PASS

Evidence: F:\GitHub\McpServer\docs\receipts\todo-audit-20260820T101500Z\s0-leftover-get.json length 306 bytes. JSON object properties Id, Done, Title, Source, Note. Id PLAN-TRIAGELEFTOVER-001. Done true. No items array. Scratch twin same 306 bytes. Not a 965KB list.

### A2 Live MCP todo_get leftover still Done true

Verdict: PASS

Evidence: plugin workflow.todo.get id PLAN-TRIAGELEFTOVER-001 exit 0. YAML result id PLAN-TRIAGELEFTOVER-001 done: true. Not type: error.

### A3 PLAN-TODOALIGN-001 still Done false

Verdict: PASS

Evidence: workflow.todo.get PLAN-TODOALIGN-001 done: false.

### A4 HEAD 20db61aa

Verdict: PASS

Evidence: git rev-parse HEAD 20db61aa0dd70f2d4f94da06d2a133ecfe6967a8. Branch develop.

## B Workspace rules

### B1 Byrd v4 product tests-first

Verdict: PASS (N/A for S0 leftover receipt)

### B2 Receipts

Verdict: PASS

File size, JSON shape, live leftover/ALIGN gets, git HEAD re-ran.

### B3 MCP-only storage

Verdict: PASS

Leftover proof is MCP get plus compact JSON receipt. No TODO.yaml write.

### B4 No Python

Verdict: PASS

This reattack used pwsh and plugin Invoke. No python.

### B5 Honesty

Verdict: PASS

Compact leftover get matches live leftover done true. Not a dump of the done:true list.

### B6 Reviewer session-log obligation

Verdict: FAIL

See FAIL list. Persist 503/400. queryHistory no matching new turn.

## C Requirement violations

Verdict: N/A

No FR/TR completed. ALIGN still done false.

## D Current plan holistically

### D1 Leftover provenance after prior DISAGREE

Verdict: PASS

Prior D1 was schema_validation_failed leftover get plus force-flag, and possible 965KB wrong file. Current durable leftover file is a 306-byte compact get with Id PLAN-TRIAGELEFTOVER-001 Done true. Live leftover get agrees. That leftover H0 item is closed.

### D2 Not claiming S4-S7 / ALIGN done

Verdict: PASS

### D3 Leftover stays done; ALIGN stays open

Verdict: PASS

## Accuracy and completeness of this review

Accuracy: 93. File bytes and JSON fields read on disk. Live leftover and ALIGN gets. git HEAD. Session persist failure captured from plugin stdout/failsafe path.

Completeness: 86. Leftover AGREE-gate claims scored. Session-log persist failed so B6 FAIL. Did not dump failsafe YAML bodies into this receipt.

## Evidence index

- C:\Users\kingd\AppData\Local\Temp\grok-hostile-h0-s0-d1b\leftover-file.json
- C:\Users\kingd\AppData\Local\Temp\grok-hostile-h0-s0-d1b\parse.json
- C:\Users\kingd\AppData\Local\Temp\grok-hostile-h0-s0-d1b\git.json
- C:\Users\kingd\AppData\Local\Temp\grok-hostile-h0-s0-d1b\session-log.json
- docs/receipts/todo-audit-20260820T101500Z/s0-leftover-get.json
