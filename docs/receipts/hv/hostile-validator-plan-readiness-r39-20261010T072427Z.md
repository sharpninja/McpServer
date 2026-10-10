# Round 39 hostile plan-readiness review

TimestampUtc: 2026-10-10T07:41:47.2323772Z. HEAD: f57809ad530f9f2bcd7a3270687f7371d67a2d07.
Validator: Codex gpt-6-sol xhigh; live rollout turn_context at 2026-10-10T07:24:29.544Z, session 01a124b2-eb72-7df2-920c-2e7849b873ae.
First actions: add-profile skill and 19 profile Markdown files read; root marker 397 lines read with credential and Authentication suppression; workflow.memory.list returned 15 effective memories on cache retry; PROCESS-007/008 read.
Reviewer turn: Codex-20261010T004320Z-plugin-session / req-20261010T072722Z-plan-r39-hv.

**DISAGREE. Accuracy 96, completeness 93, confidence 98. PASS 5, FAIL 5, UNKNOWN 0.**

## Claims

- **P1 PASS:** All 652 plan lines/532 nonempty read at HEAD f57809ad; current file, target, route, type, host and line anchors spot-checked; no product-source delta since r38.
- **P2 FAIL:** 21 proposed FR, 23 TR, 23 TEST IDs have zero collisions. Existing TR-MCP-CLEARSESSION-001 eight-plugin/byte-identical/fail-if-blocked contract is not reconciled with new nine-host selection.
- **P3 PASS:** D1-D12, partial clearance, licensing, skipped_duplicate removal, option-1 failsafe, C1-C12 ACKs and 98/98 rule represented. R38 queue fix respects D6.
- **P4 FAIL:** Server/client/REPL/cores/nine hosts and three storage providers have tasks; S9 omits the existing cline, cline-v2, opencode clear-session skill text and nine-file parity.
- **P5 FAIL:** Blocked/withdrawn turns have unresolved clear-session fail-versus-leave-open/complete paths. P0.2 is both read-only and a TODO writer.
- **P6 PASS:** Static verification: build.ps1, targets, projects, paths and G1-G6 gates exist; future --plugin-version introduced/tested A9 before S8; P0.1 gates unavailable host prerequisites. Each slice states BDPv4 steps and HV. No future suite executed while unapproved.
- **P7 FAIL:** Only negative gate-unmet clear-session cases; no ready/mixed original-pair selection, no-fabrication or blocked/withdrawn tests at S8/S9; no all-nine skill parity test. HV-P0/A/B 98/98 otherwise consistent.
- **P8 FAIL:** Never-fail clear-session instruction conflicts with existing fail-if-blocked TR/skill and planned withdrawn failTurn. P0.2 read-only label conflicts with TODO mutation. D6 overview overstates adapter rollout.
- **P9 PASS:** Main-workspace TODO description exactly matches 532 nonempty plan lines; 30 tasks equal 1 approval+9 P0+9 A+11 S; note NOT APPROVED, technicalDetails and handoff current.
- **P10 PASS:** All nine cited risk triage IDs resolve with matching titles; worktree restore/clone group failed and main absent as described; other risks scheduled or filed.

## Findings

### F-R39-01 — implementer edit

Plan: line 474; TEST rows 390, 393-394; S8 612; S9 614; S11 618.

Defect: S8 changes six PowerShell clear-session skills, but S9 does not change existing skills in cline, cline-v2, opencode. Existing TR requires byte-identical skill text across eight plugins, while rollout has nine. SyncAgentPlugins does not copy clear-session skill.

Evidence: Nine sibling clear-session skills currently share a SHA256 and still say completeTurn or failTurn if blocked. plugins/core/sync/sync-plugin-core.ps1 syncs handoff skill only. Live TR and docs/Project/Technical-Requirements.md:661-665 retain eight-plugin byte-parity wording.

Make PASS: Update existing TR/TEST to nine-host contract; schedule all three Node skill edits by S9; require nine-file byte-parity and behavioral-content test in G6.

### F-R39-02 — operator decision

Plan: lines 54, 234, 271, 474, 612, 614.

Defect: Clear-session says never fail, while current skill/TR say fail if blocked and planned withdrawn AC mandates failTurn. Generic ready-selection could complete a withdrawn turn or leave it open. No status-specific choice is recorded.

Evidence: Current skill line 21 says completeTurn or failTurn if blocked. Plan 474 says never failing. Plan 234 requires failTurn withdrawn by original pair. Plan 54 gives failed a different completion gate from completed.

Make PASS: Operator chooses A: status-specific fail of blocked/withdrawn turns when failed-state class/evidence met, normal ready completes; or B: never fail in clear-session, leave blocked/withdrawn open for later explicit failTurn and revise existing TR/skill/withdrawn remedy. Propagate chosen rule to ACs/tasks/tests.

Operator decision: Clear-session treatment of blocked and withdrawn turns. Options: A status-specific fail/complete; B leave blocked/withdrawn open for explicit later failTurn.

### F-R39-03 — implementer edit after operator decision

Plan: TEST rows 390,393-394; TR update 474; S8 612; S9 614.

Defect: Only gate-unmet negative clear-session cases are named. A coordinator closing zero turns can pass. No mixed positive case proves ready turns close by original pair while unready turns remain and reviews are not fabricated.

Evidence: S8 ClearSession_GateUnmetTurn_StaysInProgress_Reported and S9 clearSession_gateUnmetTurn_staysInProgress_reported are negative. TEST-009 checks only that skill text lists steps.

Make PASS: At S8 and S9 add RED/green mixed-turn positive cases: two ready close by original pairs, one unready stays with exact missing items, no review fabricated; add blocked/withdrawn case after F-R39-02 decision; map every relevant AC.

### F-R39-04 — implementer edit

Plan: D6 summary line 32; table line 84; P0.2 line 173; TODO P0.2 task.

Defect: P0.2 is called read-only but files an MCP follow-up TODO. D6 overview says every host gets an arrival adapter wherever a mechanism exists, whereas only Claude bridge is in this plan and other processing-time hosts remain unsupported.

Evidence: Plan line 173 begins Read-only spike, later files TODO. TODO task retains read-only label. Lines 84/173 explicitly limit the built adapter to Claude bridge.

Make PASS: Keep P0.2 read-only and file traced TODO in P0.8, or relabel P0.2 and TODO task as probe-plus-TODO. Rewrite line 32 to state other hosts stay unsupported pending separately approved adapters.

## Whole-plan and phase-boundary audit

- Opening/decisions: D6 overview overbroad; other locked decisions represented.
- Host table/P0: r38 queue repair present, P0.2 read-only contradiction.
- Phase A: bridge/identity/withdraw and nine-host adoption dependencies otherwise placed.
- Phase B requirements/tests: clear-session existing TR migration, mixed tests and status choice missing.
- S1-S11: S8 selection before S10 gate is specified, but S9 Node skill text absent.
- Risks/rollout: triage verified; HV gates consistent.

P0.8, A1-A9 and S1-S11 dependencies reviewed. S8/S9 clear-session ACs lack positive behavior tests and S9 skill migration; other gate order coherent.
TODO description is exactly the 532 nonempty plan lines; 30 tasks are 1 approval + 9 P0 + 9 A + 11 S. Round 38 F-R38-01 is repaired; F-R38-02 gained a selection path but remains incompletely propagated. Recurrence signal: cross-host AC updated in core but not every skill or positive test.

## Audit

Runner JSONL: docs/receipts/hv/20261010T072427Z-plan-readiness-r39.request.jsonl; docs/receipts/hv/20261010T072427Z-plan-readiness-r39.response.jsonl. No marker credential copied. No plan, TODO, requirement, triage, memory, settings, source, commit or push mutation. These receipts are the only validator file additions.
Reviewer-turn persistence and complete-result readback are recorded separately after receipt creation.
Reviewer-turn QueryAsync readback before completion: session/request matched; all 10 claim IDs, 4 finding IDs and SectionAudit present. Credential-value scan: 7 marker field lines; 0 runner response matches; 0 receipt matches.
Final read-only QueryAsync found the reviewer turn completed with 3 dialog items, 3 actions, all 10 claim IDs, all 4 finding IDs and its pre-completion persistence proof.
