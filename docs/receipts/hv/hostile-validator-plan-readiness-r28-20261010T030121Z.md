# Hostile validator: PLAN-SESSIONTURNREVIEW-001, round 28

- TimestampUtc: 2026-10-10T03:01:23Z (review start); receipt finalized on 2026-10-10 UTC.
- ValidatorIdentity: Codex CLI, `gpt-6-sol`, `xhigh`. Live proof: own rollout `rollout-2026-10-09T22-01-22-01a123c2-09b0-7040-860f-bbba1362a9cb.jsonl`, `turn_context` at `2026-10-10T03:01:23.166Z`, model `gpt-6-sol`, effort `xhigh`, thread `01a123c2-09b0-7040-860f-bbba1362a9cb`.
- HeadSha: `2b2b4fd8ec2e593c8af35c5fbf93869c15141f19`.
- Reviewer sessionId: `Codex-20261006T202119Z-plugin-session`; requestId: `req-20261010T030449Z-plan-readiness-r28`.
- Add-profile: skill read and 19 non-skill Markdown profile files read in full.
- Marker: `/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml` read in full, 398 lines; SHA-256 `EB3600F618A95F424FF852DA4D099CF0803F249DFAAA48BAA7BF4929827E9C0F`; credential lines and Authentication block filtered from output.
- MCP memory: `workflow.memory.list` returned 15 effective records. `MEMORY-PROCESS-008` was read and applied (what-if, propagation, recurrence, phase boundaries). A bulk read of all memory texts was rejected by automatic approval review because it would export a large, sensitivity-unverified memory into tool output. I did not bypass that rejection. Thus the mandatory all-memory read was not completed; the ten plan claims were evaluated from the operator's authoritative instructions and other evidence.
- Runner JSONL: `docs/receipts/hv/20261010T030121Z-plan-readiness-r28.request.jsonl` and `.response.jsonl`. The response stream was checked against the marker credential in memory; no matching value was present.

## Verdict

**DISAGREE. Accuracy 93/100; completeness 88/100; confidence 95/100.** Four claims PASS, six FAIL, none UNKNOWN. The plan is not decision-complete or executable at the required 98/98 threshold. All six findings below are implementer edits to the plan, its derived TODO, and the handoff. No operator decision is needed.

## Claims

- **P1 PASS — source accuracy.** At HEAD, sampled source anchors and behaviors match the plan: `SessionLogService.ValidateTerminalTurnCompliance` at 1329; stop auto-close at `plugin-hook.ps1:1017`; supersede definition and call at 2131/2300; `CLAUDE_STOP_HOOK_ACTIVE` and `CLAUDE_SESSION_ID` reads at 944/165; scoped tarball assumption at `Build.SyncAgentPlugins.cs:550`. HEAD differs from `f56dcf70` in docs, receipts, handoff, and AGENTS paths, not product sources. All nine named plugin repo heads and dirty statuses matched the plan inventory. No false current-source statement was found in this pass.
- **P2 FAIL — requirements accuracy.** Existing referenced canonical ids and the 21/23/23 proposed id families were checked against the live store (FR 354, TR 482, TEST 514); no proposed collision was found. Four TEST-to-FR mapping edges are omitted (F-R28-02).
- **P3 PASS — locked decisions.** D1-D12, partial clearance, licensing, removal of `skipped_duplicate`, and the unsupported-session failsafe choice are represented. The S10 marker activation follows the operator's rollout decision; remaining phase contradictions are scored under P6/P8.
- **P4 PASS — scope.** Tasks cover server, typed client, REPL, MCP agent, PowerShell and Node cores, all eight named agent plugin families plus grok-bot, and SQLite/PostgreSQL/SQL Server migrations. Sync and compatibility paths are specified. Phase timing of several tests remains defective under P6/P7.
- **P5 FAIL — decision completeness.** The contradictory TEST AC rules and outbox ownership timing leave implementers to choose which contract and phase to follow (F-R28-01, F-R28-05).
- **P6 FAIL — executability.** Named build targets and test project paths were found, and missing SDK/Pester/bats/dotnet-ef prerequisites are explicitly gated by P0.1. BDPv4 steps are stated. A7, A8, and S9 require RED or green cases before the responsible implementation exists (F-R28-03 through F-R28-05).
- **P7 FAIL — testability.** The two versus three TEST AC rule, baseline-green RED obligation, missing mappings, and misplaced cases make AC satisfaction objectively inconsistent (F-R28-01 through F-R28-05). HV-P0/HV-A/HV-B and 98/98 are stated consistently.
- **P8 FAIL — internal consistency.** The dependency chain in P0.8 contradicts A7 and A8 case placement and S9 satisfaction timing; a wire contract says the gate activates after S8 although S10 exclusively activates it (F-R28-01, F-R28-03 through F-R28-06).
- **P9 FAIL — artifact parity.** Plan has 652 lines, 532 non-empty; TODO description's 532 entries match them exactly, and its 30 implementationTasks match the task list. Handoff counts, status NOT APPROVED, paths, and triage references are accurate. However TODO `technicalDetails` and P0.4 task say exactly two TEST ACs, as does the handoff, while plan line 371 says three (F-R28-01).
- **P10 PASS — risks.** Nine main-workspace triage ids in the risk section were verified through read-only `workflow.triage.getReport`. The exceptional `triage-group-804b21086f3019ab` fails lookup under the main workspace and resolves under the worktree, as the plan says. No unfiled plan-scope risk was found.

## Failures and exact repairs

### F-R28-01 — incompatible TEST extraction contracts

- Plan location: **New testing requirements**, lines 369 and 371; **P0.4**, line 174; **AC convention**, line 500; **TEST-SUPPORT-023**, line 395.
- Defect: line 369 creates exactly two fixed TEST ACs, whereas line 371 creates three and names `TEST-SUPPORT-023` `ac-1..ac-3`. Line 369's `X-AC002` demands RED for every validated requirement AC, including five existing baseline-green ACs that line 500 expressly says need no RED. `TEST-SUPPORT-023` validates those five.
- Evidence: direct comparison of the cited plan lines; TODO technicalDetails and handoff both adopt two ACs, so line 371 also breaks artifact parity.
- Make PASS: remove or rewrite line 371 to the single two-AC extraction rule; state a baseline-green exception in `X-AC002` requiring its existing green receipt instead of RED; give `TEST-SUPPORT-023` the same two AC ids/texts; update P0.4, TODO technicalDetails/task, and handoff to the same exact contract. **Implementer edit; no operator decision.**

### F-R28-02 — four missing TEST-to-FR mappings

- Plan location: **New testing requirements**, lines 378, 381, 383; **Mappings**, lines 401, 405, 412.
- Defect and evidence: `TEST-MCP-SESSIONTURN-006` validates `FR-MCP-SESSIONCLASS-004` but is absent from that FR's mapping row; `TEST-MCP-SESSIONTURN-009` validates `FR-MCP-SESSIONTURN-001` but is absent from that row; `TEST-MCP-SESSIONCLASS-002` validates `FR-MCP-SESSIONCLASS-004` and `FR-MCP-SESSIONTURN-005` but is absent from both rows. The associated TR links are absent as well. A direct forward comparison found four omissions; reverse mapping had none.
- Make PASS: add each TEST and its appropriate TR to the corresponding full mapping row, or narrow the TEST statement if it does not really validate that FR; regenerate the TODO and handoff mapping descriptions. **Implementer edit; no operator decision.**

### F-R28-03 — TEST ACs allocated before their cases can run

- Plan location: **TEST extraction rule**, line 369; **TR-MCP-SESSIONTURN-006**, line 350; **TEST-MCP-SESSIONTURN-006**, line 378; **TR-MCP-SESSIONREVIEW-009**, line 366; **TEST-MCP-SESSIONREVIEW-009**, line 394; **dependency chain**, line 178.
- Defect: line 369 assigns both TEST ACs to the TR's bracketed slices. `TR-MCP-SESSIONTURN-006` is `[A7]`, yet its TEST row names S8-only review/precheck behavior and S10 real-gate behavior. `TR-MCP-SESSIONREVIEW-009` is `[S8 +S9]`, yet its TEST row names the S11 `SessionLogReviewDocsContractTests` and `FR-MCP-SESSIONREVIEW-008-AC005` (`[S11]`, line 340). A7 precedes S8/S10; S9 precedes S11. These TEST ACs cannot be green at the declared slice exits.
- Make PASS: split or reallocate later cases to TEST records owned by their actual S8/S9/S10/S11 slices, state when each TEST AC becomes satisfied, and update mappings, TODO tasks, and handoff accordingly. Preserve A7's pre-S10 capture/warning/drain scope and S10's real-gate drain. **Implementer edit; no operator decision.**

### F-R28-04 — duplicate-hook transition placed before detector adoption

- Plan location: **A7 RED**, line 590; **TEST-MCP-SESSIONTURN-006**, line 378; **A9**, line 594; **P0.8 dependency chain**, line 178.
- Defect: A7 RED includes `DuplicateHookAfterDelivery_UnsupportedSince_OpenTurnCloseCaptured`, an actual copilot/classic-cline duplicate registration transition. The host detector/adoption and those host G6 suites are A9 work; A9 depends on A7. A7 can exercise generic `unsupportedSince` stop handling with a double, but cannot test the live duplicate detector transition before A9.
- Make PASS: move the real duplicate transition RED/case to A9 and its host TEST row; keep a synthetic unsupportedSince state-handling case in A7. Update TEST allocations and TODO task text. **Implementer edit; no operator decision.**

### F-R28-05 — A8 outbox test depends on later QB integration

- Plan location: **Coordination ownership**, line 117; **TR-MCP-SESSIONTURN-008 AC-006**, line 352; **A8 RED**, line 592.
- Defect: QB-AGENT-001 owns the durable outbox and its P1 lifecycle integration builds on A8. HEAD lacks `plugins/core/lib-node/src/cache/audit-outbox.ts` and `audit-alias-consumer.ts`; those names appear only in an unmerged QB candidate worktree. A8 nevertheless requires a spy proving admission precedes `AuditOutbox.enqueue` and `AuditAliasConsumer.enqueue`, and an outbox collision case that runs once QB integration lands. This puts an A8 test after the integration that depends on A8.
- Make PASS: at A8, test the admission contract against a defined outbox port/double before any enqueue; place real `AuditOutbox`/`AuditAliasConsumer` spy and collision tests in QB P1 integration after A8, with a named post-integration gate and TEST/AC allocation. Alternatively revise the ownership/dependency only if actual earlier integration is established. **Implementer edit under agreed C1-C12; no new operator decision.**

### F-R28-06 — stale S8 completion-gate activation statement

- Plan location: **Wire contracts: Stop gate**, line 551; compare **FR-MCP-SESSIONCLASS-004-AC002**, line 284; **TR-MCP-SESSIONCLASS-002-AC005**, line 355; **S10**, line 616; **rollout**, line 623.
- Defect: line 551 says `session_turn_completion_gate` blocks “after S8,” while the plan's activation contract says only S10's registered gate writes the verified marker field and rollback removes it. Thus a pre-S10 S8 plugin must be inert.
- Make PASS: replace “after S8” with “while a verified S10 marker states `sessionTurnCompletionGate: enforced`,” including the pre-S10/rollback inert behavior; propagate to TODO and handoff. **Implementer edit; no operator decision.**

## Holistic and regression checks

- Read the entire 652-line plan end to end. Compared P0, A1-A9, S1-S11, wire contracts, requirements catalog, mappings, tests, rollout, risks, and handoff. Used MEMORY-PROCESS-008's what-if, propagation, and recurrence checklist. Phase-boundary audit identified F-R28-03 through F-R28-05.
- Re-attacked r27 F-R27-01..03. A7 now limits pre-S10 gate assertions and moves real drain rejection to S10; S8/S9 marker-absent tests and S10 marker activation address F-R27-02. F-R27-03 persists through line 371 and baseline-green contradiction (F-R28-01).
- Scanned r1-r26 finding summaries for recurrence. No separate older finding was proven to recur beyond the propagation and phase issues above.
- No source code, plan, TODO, requirement, triage, memory, setting, commit, or plugin repo was changed by this validator.

## Persistence

- Own MCP turn `req-20261010T030449Z-plan-readiness-r28` was opened with `code: persisted`, `persisted: true`. Full-result update returned `code: persisted`, `persisted: true`; typed `client.SessionLog.QueryAsync` readback found this requestId, receipt filename, F-R28-01, and F-R28-06 in the turn payload. Completion is verified separately below.


- `workflow.sessionlog.completeTurn` returned `code: persisted`, `persisted: true`. A subsequent typed `client.SessionLog.QueryAsync` readback found the reviewer requestId, F-R28-01, F-R28-06, the receipt name, and `completed` status adjacent to the turn.

