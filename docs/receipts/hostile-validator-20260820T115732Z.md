# Hostile validator receipt

TimestampUtc: 2026-08-20T11:57:32Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (MCP-store hygiene S0 freeze reattack after D1 DISAGREE). Not product implementation. Not user-directed ops.

add-profile: executed yes. Profile markdown files read in full: 18 (excluded add-profile.grok.md).

Locked scope: H0 S0 only. Surface C N/A. Do not require S4-S7. Prior FAIL D1 from docs/receipts/hostile-validator-20260820T114439Z.md.

Session log: plugin bootstrap/open/begin/appendDialog/appendActions/complete/queryHistory exit 0. queryHistory title "Hostile H0 D1 reattack" sessionId GrokCode-20260820T115415Z-plugin-session turnCount 1 status in_progress. requestId req-20260820T115415Z-ish from first get; beginTurn used req-20260820T115732-range. Local cache reused plugin-session. Isolated failsafe empty.

Trust: marker HMAC match true. GET /health nonce 3318cef7ae3d4828a31f8aedd508be3a echoed, HTTP 200, version 1.4.29+20db61aa0dd70f2d4f94da06d2a133ecfe6967a8.

## OverallVerdict

DISAGREE

PASS: 13. FAIL: 1. UNKNOWN: 0. N/A: 1 (surface C).

## Explicit FAIL list

- D1 leftover provenance still not a successful freeze `todo_get`. `s0-complete.ps1` leftover invoke returned `schema_validation_failed` (`payload.params.id is required`). That error is stored as `leftoverGetSnippet` in `s0-req-histograms.json`. Then `fix-leftover-flag.ps1` force-set `leftoverDone = true` on scratch and durable JSON. Independent live `workflow.todo.get` PLAN-TRIAGELEFTOVER-001 is `done: true`, so the flag is factually correct, but D1's hardcoded-leftover defect is not closed. Missing file set and live git HEAD are now present.

## A Requested validation

### A1 Durable and scratch twins of the named S0 files

Verdict: PASS

Evidence: docs/receipts/todo-audit-20260820T101500Z contains s0-inventory.json, s0-drift.json, s0-git.json, s0-git-status-short.txt, s0-req-histograms.json, s0-done-true.json, s0-mappings.json (plus later s3/s6 files). Scratch twins exist under C:\Users\kingd\AppData\Local\Temp\grok-goal-498d465c218e\implementer. SHA256 equal for all seven named pairs.

### A2 s0-done-true.json doneTrueCount is live done:true count (hundreds). leftoverDone true via MCP todo_get

Verdict: PASS (state). Method smell scored under D1.

Evidence: s0-done-true.json doneTrueCount 253 leftoverDone true leftoverId PLAN-TRIAGELEFTOVER-001. Independent recount of source MCP JSON call-4b22264e-b699-4775-876f-f9b77dad0500-164.json items 253. Live plugin todo.query done:true regex-counted 253 ids; leftover in that list; ALIGN not in that list. Live workflow.todo.get PLAN-TRIAGELEFTOVER-001 done: true. Implementer leftover get during s0-complete failed schema validation; leftoverDone was then force-set. Validator did not treat that failed get as the leftover proof.

### A3 s0-req-histograms.json trCount 422 testCount 448. s0-mappings.json mappingCount 293

Verdict: PASS

Evidence: Durable histograms trCount 422 testCount 448. Independent ConvertFrom-Json of source TR JSON 422 items and TEST JSON 448 items. s0-mappings.json mappingCount 293 source path call-4b22264e-...-165.json. Independent mapping items.Count 293. First mapping object keys FrId, TrIds, TestIds, WorkspaceId. mappingCount equals FR count from the earlier freeze; the mapping payload is still a mapping list, not a copy of FR records.

### A4 s0-git.json head matches git rev-parse. statusShort is git status --short

Verdict: PASS

Evidence: Live git rev-parse HEAD 20db61aa0dd70f2d4f94da06d2a133ecfe6967a8 equals s0-git.json head. Branch develop. s0-complete.ps1 calls git rev-parse and git status --short. s0-git-status-short.txt exists length 14130, SHA-equal to scratch. txt vs json statusShort differs by 2 bytes (line ending), not a different command. Live status line count 229 (tree drifted after freeze). srcOrTestsTouched empty.

### A5 PLAN-TODOALIGN-001 still done false. No QuadBrain/FILETOOLS/Handoff src/tests edits this slice

Verdict: PASS

Evidence: Live todo.get PLAN-TODOALIGN-001 done: false. ALIGN absent from live done:true query. git status --short src/ and tests/ empty. pythonInStatus false.

## B Workspace rules

### B1 Byrd v4 product tests-first

Verdict: PASS (N/A for S0 freeze)

### B2 Receipts

Verdict: PASS

Validator re-ran git, hashes, source JSON counts, live leftover/ALIGN gets, live done:true query, health, signature.

### B3 MCP-only storage

Verdict: PASS

No docs/Project/TODO.yaml in this slice's product edits. ALIGN remains MCP todo.

### B4 PowerShell / no Python

Verdict: PASS

s0-complete.ps1 and fix-leftover-flag.ps1 are pwsh. git status has no .py from this slice.

### B5 Honesty

Verdict: PASS with observation

leftoverDone true matches live leftover done and leftover membership in the 253 done:true list. They did not invent leftover closed. They did force the flag after a failed leftover get. That leftover provenance defect is D1, not a false leftover state.

### B6 Reviewer session-log

Verdict: PASS

queryHistory GrokCode-20260820T115415Z-plugin-session title Hostile H0 D1 reattack turnCount 1.

## C Requirement violations

Verdict: N/A

No FR/TR completed claim. ALIGN still done false.

## D Current plan holistically

### D1 S0 snapshot completeness after prior DISAGREE

Verdict: FAIL

Fixed since 114439Z: durable TR/TEST histograms, mapping count snapshot, done-true count, git status --short, live git HEAD in s0-git.json.

Not fixed: leftover still not proven by a successful freeze `workflow.todo.get`. leftoverGetSnippet is schema_validation_failed. fix-leftover-flag.ps1 writes leftoverDone=$true. Prior D1 named hardcoded leftover. That item remains.

### D2 Not claiming S4-S7 / ALIGN done

Verdict: PASS

### D3 Leftover stays done; ALIGN stays open; no product implementation

Verdict: PASS

Matches live leftover done true, ALIGN done false, empty src/tests git status.

## Accuracy and completeness of this review

Accuracy: 91. Live MCP leftover/ALIGN/done:true, git HEAD, source JSON recounts, file hashes. Deduction: query-done YAML Contains('type: error') true while id count still 253 (likely a TODO body token); leftover freeze get failure is in the histogram snippet.

Completeness: 90. Surfaces A-D scored. Mapping store not re-fetched live (source mapping JSON + count 293 recounted). Live git status drifted from freeze snapshot as expected.

## Evidence index

- C:\Users\kingd\AppData\Local\Temp\grok-hostile-h0-s0-d1\files.json
- C:\Users\kingd\AppData\Local\Temp\grok-hostile-h0-s0-d1\git-compare.json
- C:\Users\kingd\AppData\Local\Temp\grok-hostile-h0-s0-d1\source-counts.json
- C:\Users\kingd\AppData\Local\Temp\grok-hostile-h0-s0-d1\parse-gets.json
- C:\Users\kingd\AppData\Local\Temp\grok-hostile-h0-s0-d1\session-log.json
- C:\Users\kingd\AppData\Local\Temp\grok-goal-498d465c218e\implementer\fix-leftover-flag.ps1
- C:\Users\kingd\AppData\Local\Temp\grok-goal-498d465c218e\implementer\s0-complete.ps1
