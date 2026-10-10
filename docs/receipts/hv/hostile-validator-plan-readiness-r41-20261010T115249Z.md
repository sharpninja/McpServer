# Hostile validator plan readiness round 41

TimestampUtc: 2026-10-10T12:03:36.5481676Z
HEAD: 62990ba777d5da92d33b0413f026a2ddc271e5b2
Validator: Codex CLI  ; live turn_context ; rollout /home/sharpninja/.codex/sessions/2026/10/10/rollout-2026-10-10T06-52-50-01a125a8-9db8-7d90-a3c4-32e3934c3354.jsonl
First actions: add-profile skill and 19 profile files read; full main marker (398 lines) read with all credential lines and Authentication block suppressed before output; workflow.memory.list returned 15 memories, including MEMORY-PROCESS-007 and -008.
Scope: review-only. The plan is NOT APPROVED, so G1-G6 were judged statically and no plan step was run.

## Claim results

- P1 UNKNOWN: All 653 plan lines reviewed by section; HEAD and nine plugin heads checked and key anchors verified. Historical transcript counts and every individual line anchor were not independently recomputed, so universal source accuracy is not certified.
- P2 PASS: Live workflow.requirements lists 354 FR, 482 TR, 514 TEST. Catalog has 21/23/23 new IDs with no collision. A 127-ID reference scan found only two expressly identified Pester test labels and the expressly stale TEST-SUPPORT-023 outside the live store or P0 catalog. Updated clear-session records exist.
- P3 FAIL: D1-D12 appear, but clear-session option A cannot be guaranteed for withdrawn-unclassified turns after S10. F-R41-02.
- P4 PASS: Tasks cover server, client, REPL, MCP tools, PowerShell and Node cores, all nine existing plugin repos and SQLite/PostgreSQL/SQL Server migrations.
- P5 FAIL: Mandatory Codex P0 probe has no method before hook installation; unclassified withdrawn clear-session behavior is unspecified. F-R41-01/02.
- P6 FAIL: G1-G6 commands and BDPv4 order are statically present, but the P0 Codex probe gate precedes its hook registration without a specified observer. F-R41-01.
- P7 FAIL: No S8/S9/S10 case covers a withdrawn-unclassified turn failing under the real gate, and no concrete Codex P0 callback probe command is supplied. F-R41-01/02.
- P8 FAIL: The P0.2 to A9 hook dependency and option-A to S10 class dependency conflict. Round-40 repairs are present. F-R41-01/02.
- P9 PASS: Main-workspace workflow.todo.get returns NOT APPROVED, 30 tasks, and 533 description lines exactly matching the 533 nonempty plan lines. Handoff gives current source, TODO, counts and round-42 correction section.
- P10 PASS: Nine cited triage report IDs resolve with matching titles. Worktree group triage-group-804b21086f3019ab resolves failed with one report; main workspace not found, as described. Other risks are scheduled.

## Findings

### F-R41-01 - implementer edit

Plan location: Host matrix lines 70, 83, 89; P0.2 line 174; P0.9 line 181; A9 line 595

Defect: A passing Codex UserPromptSubmit arrival probe is mandatory in read-only P0, but the Codex plugin has no registered hook until A9 and the plan specifies no temporary observer for P0.

Evidence: At the current Codex plugin head and origin/main, hooks/hooks.json is absent and .codex-plugin/plugin.json has no hooks property. A9 creates the hook. P0.2 must establish enqueue versus processing and stops P0 on failed/unavailable or processing-only Codex results. The manual Codex wrapper can persist reviewer turns without a prompt hook, so the stated HV-persistence rationale does not itself provide callback evidence.

Make PASS: Specify an isolated temporary Codex hook observer for P0.2, with exact registration, launch, timestamp capture, cleanup, command, and pass receipt, without persistent settings or repo edits. If this cannot be done, explicitly defer the timing gate to A9 and define the manual-wrapper HV path until then. Propagate the chosen gate to P0.2, P0.9, A9, TODO and handoff.

### F-R41-02 - implementer edit

Plan location: Locked decision line 30; class line 42; completion line 55; queue AC line 206; gate AC line 272; clear-session line 475; S8 line 613; S9 line 615; S10 line 617

Defect: A message withdrawn before delivery may still be Unclassified. Operator option A says clear-session fails every withdrawn turn, while the S10 gate rejects failed turns without requestClass. The coordinator only reads class and selects failTurn; it has no classify-before-fail step or real-gate test for this case.

Evidence: Ingress mints before the model classifies (lines 31, 42); popAll/remove can withdraw before delivery (lines 32, 206). FR-MCP-SESSIONCLASS-002-AC002 requires class for failed. S8 and S9 list no class write before failTurn, so S10 can return session_turn_completion_gate and leave that turn open, violating option A.

Make PASS: Strengthen TR-MCP-CLEARSESSION-001-AC001: for withdrawn or blocked turns lacking class, the model reads original queryText, writes Code/Docs/Chore by original pair, verifies it, then fails by that pair with the specified reason. Add S8 Pester and S9 Node RED cases for withdrawn-unclassified and blocked-unclassified, plus S10 real-gate cases. Propagate to TEST, allocation, skill, TODO and handoff.

## Round-40, prior-round, and propagation audit

F-R40-01 repaired: S8 has PowerShell coordinator only; S9 owns Node coordinator, nine skill edits and Build.Tests byte parity. F-R40-02 repaired: two TEST-shaped labels are explicitly Pester test names. F-R40-03 repaired: P0.2 read-only and P0.8 owns feature-matrix writes. F-R40-04 repaired: blockers predicate and withdrawn > blocked > ready precedence with mixed cases. All r2-r39 receipt filenames exist. The phase-boundary reattack found F-R41-01/02. Recurrence: clear-session rule propagation again missed the class consequence at S10.

## Verdict

DISAGREE. Accuracy 96%, completeness 94%, confidence 95%. PASS 4, FAIL 5, UNKNOWN 1. Both findings are implementer plan edits under existing operator rulings; no operator decision is required.
Reviewer turn: Codex-20261010T115828Z-plugin-session / req-20261010T115249Z-plan-r41-hv. Persistence proof: read-only `client.SessionLog.QueryAsync` from the worktree MCP store returned the reviewer session and request, turn `status: completed`, response present with P1-P10 and both F-R41 findings, lastUpdated 2026-10-10T12:04:00Z. A persistence-proof dialog was appended to the same reviewer turn and confirmed by readback.
Request JSONL: /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261010T115249Z-plan-readiness-r41.request.jsonl
Response JSONL: /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261010T115249Z-plan-readiness-r41.response.jsonl

=== VERDICT JSON ===
{"overallVerdict":"DISAGREE","accuracy":96,"completeness":94,"confidence":95,"passCount":4,"failCount":5,"unknownCount":1,"failList":["F-R41-01","F-R41-02"],"unknownList":["P1: historical transcript counts and every individual source anchor not independently recomputed"],"implementerFixes":["F-R41-01","F-R41-02"],"operatorDecisionFails":[],"headSha":"62990ba777d5da92d33b0413f026a2ddc271e5b2","receiptPaths":["/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-plan-readiness-r41-20261010T115249Z.md","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-plan-readiness-r41-20261010T115249Z.json","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261010T115249Z-plan-readiness-r41.request.jsonl","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261010T115249Z-plan-readiness-r41.response.jsonl"],"reviewerSessionId":"Codex-20261010T115828Z-plugin-session","reviewerRequestId":"req-20261010T115249Z-plan-r41-hv"}
