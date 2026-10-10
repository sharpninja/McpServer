# Hostile validator: PLAN-SESSIONTURNREVIEW-001, round 34

- TimestampUtc: 2026-10-10T05:49:00Z
- ValidatorIdentity: Codex CLI, gpt-6-sol, xhigh. Live proof: CODEX_SESSION_ID=01a12451-5c36-7d33-9e4d-b67f2f0e9bc4; /home/sharpninja/.codex/sessions/2026/10/10/rollout-2026-10-10T00-37-54-01a12451-5c36-7d33-9e4d-b67f2f0e9bc4.jsonl turn_context 2026-10-10T05:37:56.226Z reports model gpt-6-sol and effort xhigh.
- Mandatory first actions: add-profile skill and all 19 non-skill profile Markdown files read; 397-line main marker read with secrets filtered; all 15 effective main-workspace MCP memories read, including MEMORY-PROCESS-007 and 008.
- HEAD: 89aaef61ed726a80c2e93ab5ac090ca8d3e66c40. Plan 652 lines, 532 non-empty; TODO 30 tasks; handoff 244 lines.
- Reviewer sessionId: Codex-20261010T004320Z-plugin-session; requestId: req-20261010T053904Z-plan-r34-hv.

## Verdict

DISAGREE. Accuracy 95/100, completeness 92/100, confidence 97/100. Four claims PASS, six FAIL, zero UNKNOWN. Keep the plan NOT APPROVED.

## Claims

- **P1 PASS.** HEAD source remains unchanged from f56dcf70 in src/, tests/, build/, plugins/; dated nine-plugin heads and versions in plan lines 65-76 match read-only inventory. Current marker signer and PowerShell/Node verifiers were inspected directly.
- **P2 FAIL.** The 21 FR, 23 TR and 23 TEST proposed IDs are collision-free against live effective store (374/502/574); 27 updated requirement IDs exist. But the new marker capability affects live FR-MCP-140, TR-MCP-SEC-005 and TEST-MCP-189 (docs/Project mapping line 151), absent from P0 update list at plan lines 437-476.
- **P3 PASS.** D1-D12, partial clearance, licensing, skipped_duplicate removal and unsupported-session option 1 appear in locked decisions, contracts, slices and gates; no counter-ruling found.
- **P4 FAIL.** The plan covers named product/plugin/storage surfaces, but S10 omits concrete marker signature and verifier work for the new capability field across the server, PowerShell and Node cores, REPL/McpAgent and other marker trust readers.
- **P5 FAIL.** The implementer must choose whether and how to bind sessionTurnCompletionGate to the marker signature, preserve marker-v1 byte compatibility, and handle rollback to a marker without the field.
- **P6 FAIL.** S10's real-marker G4/G6 gate cannot prove a verified capability while the signer/verifiers omit it. Adding the field to marker-v1 naively conflicts with existing pinned byte-compatibility TEST-MCP-189; leaving it unsigned violates the claimed verified-marker activation.
- **P7 FAIL.** TEST-MCP-SESSIONCLASS-002 and S10 name real-marker activation cases, but no AC/test verifies capability integrity under field tampering, v1/v2 compatibility, or rollback. A passing signature over other fields is insufficient.
- **P8 FAIL.** Plan lines 285, 356, 362-363, 616 and 623 call the new field a verified activation contract, while the specified S8/S9 and S10 tasks never extend the signed canonical payload or verifier contract.
- **P9 PASS.** Main-workspace workflow.todo.get with worktree -CacheRoot returned done=false, 532 non-empty description lines byte-parity after blank-line normalization, and 30 section-label tasks. Handoff lines 11-17 and 206-209 match status, counts and round-35 changes.
- **P10 PASS.** All nine named report IDs in plan lines 629-630 resolved via live workflow.triage.getReport with matching titles; worktree triage group 804b... read back status failed. Plugin inventory fixes are assigned A9.

## FAIL finding

### F-R34-01 - Completion-gate marker capability is outside the signed trust contract

- Plan location: FR-MCP-SESSIONCLASS-004-AC002 line 285; TR-MCP-SESSIONCLASS-002 AC-005 line 356; TR-MCP-SESSIONREVIEW-004 AC-004 line 362; TR-MCP-SESSIONREVIEW-005 AC-004 line 363; TEST-MCP-SESSIONCLASS-002 line 383; S8/S9 lines 612-614; S10 line 616; rollout line 623.
- Exact defect: The plan activates local completion prechecks from a 'verified marker' field, sessionTurnCompletionGate, but never requires that field to be signed or the trust readers to verify it. The implementer must choose an incompatible marker-v1 change or an unsigned capability. Both leave the stated rollback/precheck contract unproven.
- Evidence: MarkerFileService.cs lines 24, 50-83 and 497-529 hardcode marker-v1 and its 29 signed fields, excluding sessionTurnCompletionGate. plugins/core/lib-ps/marker-resolver.ps1 lines 127-160 and lib-node/src/discovery/marker-resolver.ts lines 109-151 reconstruct only those fields. docs/Project/Technical-Requirements.md:2641-2644 and Testing-Requirements.md:530 plus MarkerFileServiceTests.cs:1092-1099,1133-1178 pin marker-v1 bytes and independent verifier compatibility. Plan search for signature/canonicalization finds no S8/S9/S10 task.
- Specific change to PASS: In P0 strengthen the existing FR-MCP-140/TR-MCP-SEC-005/TEST-MCP-189 contract and map new completion-gate ACs to tests. Specify a versioned signed capability contract (preserve v1 bytes; sign the gate field in a new version), exact activation and rollback behavior, and source updates to the server and every marker trust reader. Allocate verifier support before S10 activates the field; add RED tests for tampering, pre-S10 inert behavior, S10 activation on all nine hosts, and rollback to v1, then include them in S8/S9/S10 gates and handoff/TODO sync.
- Classification: Implementer plan edit. Operator decision: None; the existing verified-marker and rollback requirements determine the outcome.

## Holistic and recurrence audit

Rechecked signature prior rounds' repaired structures: single bridge minter, queue-position system entries, permanent alias lookup with final acks, equal-field fail-close, unsupported-session failsafe, dependency chain P0.8, idempotent --plugin-version, TEST three-AC rule, conditional P0.2 cases, and round-33 fixes. No earlier finding was established to recur; F-R34-01 is a new trust/phase-boundary gap.

If a pre-S10 or rolled-back marker gains the unsigned field, local prechecks can activate against a server that accepts the close. If an S10 marker loses the field without a signature failure, local Stop remedies deactivate despite the server gate. The plan requires both cases to fail closed or stay inert as appropriate.

## Session-log persistence

Full-result updateTurn persisted=true; client.SessionLog.QueryAsync read back reviewer requestId, status in_progress, and all 7421 response characters; response matches the Markdown receipt: True. Final completeTurn and post-completion query are verified separately before the verdict is issued.

## Receipts

- docs/receipts/hv/hostile-validator-plan-readiness-r34-20261010T053753Z.md
- docs/receipts/hv/hostile-validator-plan-readiness-r34-20261010T053753Z.json
- docs/receipts/hv/20261010T053753Z-plan-readiness-r34.request.jsonl
- docs/receipts/hv/20261010T053753Z-plan-readiness-r34.response.jsonl

=== VERDICT JSON ===
{"overallVerdict":"DISAGREE","accuracy":95,"completeness":92,"confidence":97,"passCount":4,"failCount":6,"unknownCount":0,"failList":["F-R34-01"],"unknownList":[],"implementerFixes":["F-R34-01"],"operatorDecisionFails":[],"headSha":"89aaef61ed726a80c2e93ab5ac090ca8d3e66c40","receiptPaths":["docs/receipts/hv/hostile-validator-plan-readiness-r34-20261010T053753Z.md","docs/receipts/hv/hostile-validator-plan-readiness-r34-20261010T053753Z.json","docs/receipts/hv/20261010T053753Z-plan-readiness-r34.request.jsonl","docs/receipts/hv/20261010T053753Z-plan-readiness-r34.response.jsonl"],"reviewerSessionId":"Codex-20261010T004320Z-plugin-session","reviewerRequestId":"req-20261010T053904Z-plan-r34-hv"}
