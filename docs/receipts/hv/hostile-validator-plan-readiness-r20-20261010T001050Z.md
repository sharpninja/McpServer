# Hostile validator: PLAN-SESSIONTURNREVIEW-001, round 20

TimestampUtc: 2026-10-10T00:17:40Z. ValidatorIdentity: Codex CLI, gpt-6-sol, xhigh. Live proof: `/home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T19-10-51-01a12325-efdc-7942-bc76-6f1fc5822bac.jsonl`, own `session_meta` id `01a12325-efdc-7942-bc76-6f1fc5822bac` and `turn_context` `model=gpt-6-sol`, `effort=xhigh`. HEAD: `9c40ebbfe3ae72d8322a6b22b005d490f61153fb`. Reviewer sessionId: `Codex-20261009T032846Z-plugin-session`; requestId: `req-20261010T001313Z-hv-r20`.

**OverallVerdict: DISAGREE. Accuracy 95; completeness 91; confidence 96.** Four unresolved defects leave the plan below the 98/98 approval bar. Review class: project implementation plan readiness. No implementation, build, test, TODO, requirement, triage, setting, or plan mutation was performed.

## Mandatory intake and scope

- Executed add-profile: read its skill and 19 non-skill Markdown profile files. Read the complete main-workspace `AGENTS-README-FIRST.yaml` with the apiKey line and Authentication block filtered from output. Called `workflow.memory.list` through the Codex plugin, read all 13 effective memories, including the complete long `MEMORY-PROCESS-007` channel and `MEMORY-PROCESS-008` checklist. No secret value is in this receipt.
- Read the 649-line plan (529 non-empty) section by section, the 159-line handoff, round 19's full receipt, and the r1-r18 finding catalogs. Re-attacked the four round-19 findings and used the MEMORY-PROCESS-008 what-if and propagation checks. The round-19 repairs exist in the plan, but two introduced a new contract/test propagation gap.
- The first main-workspace plugin reads returned `False` with the old default cache. A fresh plugin cache at `/tmp/hv-r20-main-plugin-cache` made the required `workflow.todo.get` and requirements/triage reads succeed. This was read-only diagnosis, outside the scored plan. The live TODO is `done=false`, note `NOT APPROVED`; description matches all 529 non-empty plan lines byte-for-byte after line splitting; all 30 implementationTasks match the operator, P0.1-P0.9, A1-A9, S1-S11 task list.
- Live requirements counts: FR 354, TR 482, TEST 514. The plan cites 120 canonical FR/TR/TEST IDs; all exist or are among the 67 explicitly created in P0 (21 FR, 23 TR, 23 TEST); zero proposed-ID collisions. Nine plugin checkouts exist. The nine main-workspace triage report IDs in the plan resolve; `triage-group-804b21086f3019ab` resolves in the worktree with one grouped report and group status failed.
- Request JSONL: `docs/receipts/hv/20261010T001050Z-plan-readiness-r20.request.jsonl` (11,451 bytes at read). Response JSONL: `docs/receipts/hv/20261010T001050Z-plan-readiness-r20.response.jsonl` (runner stream, present; byte count grows during this run). The complete result is stored on this reviewer's MCP turn and queried back before final response.

## P1-P10

- **P1 PASS.** The source facts used in the plan match the unchanged implementation baseline at HEAD: `git diff --name-only f56dcf70..HEAD` contains documentation/receipts and no `src/`, `tests/`, `build/`, or `plugins/` path; named build targets, test projects, routes, current five-entry marker generator, and all nine plugin paths were checked. F-R20-04 is a future gate omission, not a false assertion about current code.
- **P2 PASS.** 120 cited canonical IDs checked against live FR/TR/TEST lists and P0 catalog; 67 new IDs are free. Mapping additions and existing-ID references are consistent with `docs/Project` and the live lists.
- **P3 PASS.** D1-D12, partial clearance, licensing, skipped_duplicate removal, scoped use-case coverage, and 98/98 remain reflected faithfully. No operator choice is reopened by the findings.
- **P4 FAIL.** F-R20-04: the nine-plugin marker-version acceptance criterion has no implementation path for four missing marker contracts.
- **P5 FAIL.** F-R20-01 and F-R20-03 leave the exact mint fallback boundary and idle-ended session revival to implementer choice.
- **P6 PASS.** Named targets/projects/commands are present, and P0.1 makes missing SDK/Pester/bats/provider prerequisites explicit. G1-G6 and BDPv4 contract/stubs, RED, mocks-green plus negative check, self-review, real green, refactor are stated per slice. No product code or tests were run in this review.
- **P7 FAIL.** F-R20-02 omits the claimed A3 404/rollback cases from A3 RED and misplaces them on a system-event AC; F-R20-03 lacks a keyed-verb idle revival case; F-R20-04 lacks a nine-entry marker test and change task. HV-P0, HV-A, HV-B and slice HV use Codex gpt-6-sol xhigh and 98/98 consistently.
- **P8 FAIL.** F-R20-01 conflicts with the later accepted C9 refinement, and F-R20-03 contradicts the `ended.yaml` writer list. F-R20-02 is a cross-section test-allocation contradiction.
- **P9 PASS.** TODO description 529/529, 30 tasks, note/status, technicalDetails, handoff line counts, status, locations and filed triage IDs match the source and live store. The handoff's 00:10Z statement that C2/C9 refinements were posted for ACK was historically true; the channel received the ACK at 00:12:18Z, so the next handoff refresh should record it.
- **P10 PASS.** Risks are source-grounded or expressly identified as risks; nine main-workspace report IDs and the worktree group were read back. The failed group remains filed but unprocessed, as the plan says.

## FAIL findings

### F-R20-01: C9 mint capability exception is broader than the accepted contract

Plan locations: C9 line 126; FR-MCP-SESSIONTURN-001-AC008 line 200; TR-MCP-SESSIONTURN-004 AC-005 line 346; A8 line 589; rollout line 620.

Defect: the plan classifies any mint-route 404 without a detail code or generic REPL unknown-method result as a transport outage. The live `MEMORY-PROCESS-007` ACK at 2026-10-10T00:12:18Z accepts only an authenticated, bound response for the exact mint endpoint with absent detail code, or the structured `unknown_method` / `method_not_found` result for the expected mint verb. It explicitly forbids inference from prose, malformed detail metadata, arbitrary 404s, or unrelated operations, and requires the scoped exception in the trusted mint adapter, never the generic C9 classifier or the audit outbox in-call retry loop. Those discriminators and negative cases are absent from the plan. An implementer following the current text could retain a final refusal indefinitely and falsely call it an outage.

To PASS: strengthen FR-001-AC008 and C9 with the ACK's exact operation, authentication, binding, structured-code and malformed-metadata boundaries; assign the exception to the A3/A8 trusted mint adapters with later-drain-only scheduling; add named negative tests for unrelated 404, malformed detail metadata, prose-only unknown method, and generic outbox no-hot-retry; propagate to rollout and TODO/handoff copies. **Implementer plan edit; no operator decision.**

### F-R20-02: the claimed A3 404 and rollback tests are attached to system-event classification

Plan locations: FR-MCP-SESSIONTURN-002-AC001 line 205; TEST-MCP-SESSIONTURN-001 line 370; A3 RED line 579; A8 RED line 589.

Defect: line 205 lists `Mint404_NoDetailCode_OutageClass_RetainedThenDrainedAfterServerUpgrade`, `Mint404_SessionNotOpen_Final_TurnOpenFailed`, and `Rollback_OlderServer_MintsRetained_DrainAfterRestore` as tests for background/system events. They exercise FR-MCP-SESSIONTURN-001-AC008's mint fallback. `rg -n` finds those exact names only at line 205; A3 RED at line 579 does not name or describe the 404 and rollback cases. A8 has only its separate lowercase 404 cases. The round-19 repair claim that A3 and A8 test both variants, including rollback replay, is therefore unfulfilled.

To PASS: move those case names to FR-001-AC008 and its TEST-MCP-SESSIONTURN-001 statement; put all three explicitly in A3 RED (and scoped negative cases from F-R20-01); leave FR-002-AC001 with system-event tests only. **Implementer plan edit; no operator decision.**

### F-R20-03: idle-ended host session has no defined revival transition

Plan locations: host live-session rule line 82; FR-MCP-SESSIONTURN-001-AC009 line 201; TR-MCP-SESSIONTURN-003 AC-007 line 345; A3 RED line 579; wire state line 551.

Defect: after 12 hours without a mint or verb, the rule ends a host session and writes `sessions/<key>/ended.yaml`; AC009 says it is live again at the next mint **or keyed verb**. AC007 allows only the session-end hook or idle rule to write `ended.yaml`, and no rule removes, replaces, or ignores an idle-ended marker on revival. A keyed verb before a fresh mint has no specified target or reopening behavior. The only named idle case exercises mint (`IdleEnded_ThenMidTurnMessage_MintedAtEnqueue`), not the keyed verb or restart boundary. Each independent sentence can be implemented, but together they leave a durable-state contradiction.

To PASS: define the observable distinction between an idle-ended marker and a real SessionEnd marker, the atomic revival transition on a valid keyed mint or verb, and the target/response when no delivered turn exists; update AC007's writer list and wire state; add named tests for idle marker plus keyed verb, restart, and real SessionEnd. **Implementer plan edit; no operator decision.**

### F-R20-04: nine-plugin marker gate has only five marker contracts

Plan locations: plugin matrix line 98; FR-MCP-SESSIONTURN-008-AC005 line 255; TR-MCP-SESSIONTURN-009 line 351; A9 line 591.

Defect: the plan requires all nine plugin versions to be reported by `agent_plugins` after restart, but `MarkerFileService.BuildDefaultAgentPlugins` at HEAD has exactly five entries (`Codex`, `Claude`, `Copilot`, `Cline`, `Grok`; `src/QBrainAi.Services/Services/MarkerFileService.cs:532-611`). A9 adds grok-bot to `SyncAgentPlugins` and host wrappers, but no task adds marker contracts for claude-cowork, grok-bot, cline-v2, or opencode, no identity mapping is locked, and no marker-generation test asserts nine entries. The present A9 gate can pass sync while AC005 is impossible to demonstrate.

To PASS: keep AC005 and specify the four additional marker contract keys, sourceType/plugin mapping, version lookup, startup expectations, marker generator/template updates, and a nine-plugin marker readback test in A9/G6. Include the generator source file in the critical-file list. **Implementer plan edit; no operator decision.**

## Round-19 re-attack and recurrence

- F-R19-01: repaired. Host line 83 and FR-007-AC009 distinguish a session envelope for `kind: system` and `unsupportedSince`; wire line 551 refers to the root-id rule; A3 names the write/replay case. No invented nonce or requestId is required.
- F-R19-02: repaired. A1's six-collision case asserts only server HTTP 500 and `mint_collision_exhausted`; the Node outbox no-retry case is solely in A8. TODO and handoff say so.
- F-R19-03: partially repaired. The `session_not_open` discriminator and A8 cases exist; F-R20-01 captures the narrower accepted C9 boundary and F-R20-02 captures missing/misplaced A3 tests. This is a propagation recurrence, not a request to reopen the operator decision.
- F-R19-04: repaired as to public API: `createAliasService(context: AliasContext): AliasService`, immutable context, creation before registration/ingress/mint, two-session isolation appear in TR-008 AC007 and A8. Codex ACKed this at 00:12:18Z.
- r1-r18 regression sweep: the prior system queue, alias acknowledgement, stop-ledger writer, and per-turn outcome repairs remain in the source. No separate older finding was reopened. F-R20-02 shows the same cross-section propagation failure class observed in round 12; F-R20-03 is a lifecycle sibling exposed by the full what-if pass.

FAIL list: F-R20-01, F-R20-02, F-R20-03, F-R20-04. UNKNOWN list: none. All four are implementer fixes to the plan; no operator decision is needed. This DISAGREE does not approve implementation or a TODO state change.

MCP persistence proof: see the reviewer's `workflow.sessionlog.completeTurn` and `client.SessionLog.QueryAsync` readback for `Codex-20261009T032846Z-plugin-session` / `req-20261010T001313Z-hv-r20`; the turn holds this entire result, including this verdict object and JSONL paths.

=== VERDICT JSON ===
{"overallVerdict":"DISAGREE","accuracy":95,"completeness":91,"confidence":96,"passCount":6,"failCount":4,"unknownCount":0,"failList":["F-R20-01","F-R20-02","F-R20-03","F-R20-04"],"unknownList":[],"implementerFixes":["F-R20-01","F-R20-02","F-R20-03","F-R20-04"],"operatorDecisionFails":[],"headSha":"9c40ebbfe3ae72d8322a6b22b005d490f61153fb","receiptPaths":["/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-plan-readiness-r20-20261010T001050Z.md","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-plan-readiness-r20-20261010T001050Z.json"],"reviewerSessionId":"Codex-20261009T032846Z-plugin-session","reviewerRequestId":"req-20261010T001313Z-hv-r20"}
