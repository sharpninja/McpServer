# Hostile validator: PLAN-SESSIONTURNREVIEW-001, round 8

TimestampUtc: 2026-10-09T20:20:00Z  
OverallVerdict: DISAGREE  
Accuracy: 94/100  
Completeness: 86/100  
Confidence: 97/100  
HEAD: `76c19e15695563c2d60e5955dff120fc0c97c767`  
Reviewer sessionId: `Codex-20261009T200737Z-plugin-session`  
Reviewer requestId: `req-20261009T200740Z-prompt-4eb2`  
Reviewer turnId: `428`

## Validator identity and mandatory reads

Own Codex rollout `rollout-2026-10-09T15-05-26-01a12245-3e49-70b0-9be4-7e5280ba2c6e.jsonl` has a live `turn_context` at 2026-10-09T20:05:27Z: `model=gpt-6-sol`, `effort=xhigh`, and this worktree as cwd. Runner thread id is `01a12245-3e49-70b0-9be4-7e5280ba2c6e`. The runner request metadata agrees. Identity: Codex, independent hostile validator.

I read `/home/sharpninja/.claude/skills/add-profile/SKILL.md` and all 19 non-skill Markdown files in `/home/sharpninja/.claude/profile/` in full. I read the main-workspace `AGENTS-README-FIRST.yaml` in full, including after the 20:03:47Z restart, without copying its credential into this receipt. `workflow.memory.list` returned 14 effective memories; I read all 14, including `MEMORY-PROCESS-003`, `-005`, `-006`, `-007`, and `MEMORY-DEVPROCESS-001`. I also read `.github/copilot-instructions.md`, `docs/Project/Requirements-Matrix.md`, the whole 643-line plan, the entire 126-line handoff, and prior r1-r7 receipts. The aborted 19:42 round-8 response stream has no completed verdict. The current runner response stream was redacted after an accidental credential-bearing marker output; this procedural incident is excluded from the plan score. No credential value is reproduced here.

The live main-workspace TODO is `done=false`, note `NOT APPROVED`, with 30 ordered implementation tasks. Its 523 description lines match all 523 nonempty plan lines exactly. Live requirement totals are 352 FR, 480 TR, and 512 TEST. All proposed 21 FR, 23 TR, and 23 TEST IDs are free. The nine plugin checkout heads, branches, versions, and hook hashes match the plan's inventory. The nine reports named in the plan's risk ledger and the worktree-only failed triage group were found. Five extra reports named only in the handoff were also found. No product code, TODO, requirement, triage, plan, setting, or plugin file was changed.

## P1-P10 claim decisions

- **P1 Source accuracy — UNKNOWN.** Checked server routes, service methods, client methods, source and test paths, build targets, nine plugin checkout states, hook anchors, and current migration/provider configuration without finding a definite false code anchor. Runtime assertions for Cowork, Copilot, Grok Bot, and Cline remain empirical P0 probes, and the installed toolchain cannot execute all named gates here. This cannot substantiate *every* factual host behavior to the 98% standard. The source race in F-R8-01 is a design defect, not a statement that existing code has the proposed behavior.
- **P2 Requirements accuracy — FAIL.** New IDs are collision-free and store totals agree, but mapping rows assert TEST coverage that the named TEST records do not provide (F-R8-03).
- **P3 Locked operator decisions — FAIL.** D1/D5's exactly-one, enqueue-time mint has a hook-before-bridge race (F-R8-01). Other D1-D12 and the partial coordination clearance are reflected without a verified contradiction.
- **P4 Completeness of scope — FAIL.** P0 omits required host probes; the end-to-end host gate has no D6 unsupported branch (F-R8-02, F-R8-05). The server/client/REPL, PowerShell and Node cores, eight agent plugins plus grok-bot, and three migration providers otherwise have named slices.
- **P5 Decision completeness — FAIL.** `skipped_duplicate` has no defined trigger/cardinality, leaving an externally observable choice to the implementer (F-R8-06). F-R8-01 also needs an explicit race protocol.
- **P6 Executability — UNKNOWN.** Named source paths and Nuke targets exist. This execution host currently has SDK 10.0.111 rather than global.json's 10.0.201 band, no Pester 5 or bats, and no usable dotnet-ef; P0.1 describes provisioning with operator approval. I could not honestly mark all commands/builds/tests as working. G1-G6 and BDPv4 order are specified per slice, subject to the defects below.
- **P7 Testability — FAIL.** Mapping assertions and the S4 RED list omit or misassign tests for named ACs (F-R8-03, F-R8-04). The race's hook-first ordering lacks a test (F-R8-01).
- **P8 Internal consistency — FAIL.** The host binding table demands probes absent from P0.2; the universal live-smoke requirement contradicts D6 unsupported hosts (F-R8-02, F-R8-05). The 30 TODO tasks otherwise follow the stated A1-A9/S1-S11 dependency order and D10 per-slice HV gates.
- **P9 Artifact parity — FAIL.** TODO description and task order match the plan, and the handoff gets status, counts, paths, and prior-round status right. Handoff line 94 falsely says its five extra triage IDs are in the plan's Risks section (F-R8-07).
- **P10 Risks — PASS.** The plan's listed reports exist, plugin defects are scheduled in A9, and the failed worktree-only group is accurately disclosed as pending relocation. Relocation is an operator-directed triage housekeeping item outside this plan-readiness implementation score; it remains pending.

## FAIL findings and exact repairs

### F-R8-01 — Hook-before-bridge double mint (P3, P5, P7)

**Plan location:** Locked decisions lines 29-30; host binding table line 82; TR-MCP-SESSIONTURN-001 AC-006 line 340; A3 RED line 573. **Defect:** The bridge is detached from SessionStart and mints at transcript `enqueue`, while the prompt hook claims only an *already present* queued bridge record; if none exists, it mints with `prompt_id`. The plan observes ordinary enqueue/dequeue within milliseconds. If the prompt hook takes `turn-open.lock` before the bridge processes that enqueue, it mints `prompt_id`; the bridge later mints a distinct `q-` nonce for the same message. Two turns result despite D1/D5's one-turn rule. A3 tests bridge-first claim but never hook-first delivery. **Evidence:** Exact algorithm at line 82; detached launch at line 340; test list at line 573; D1/D5 promises at lines 29-30. **Make PASS:** Define a race-safe correlation and reconciliation protocol for both arrival orders under the lock, including offset ownership, identical text in separate events, restart/replay, and the no-double-mint invariant; add hook-first, bridge-first, concurrent, and crash-boundary RED tests. **Classification:** implementer plan edit under locked D1/D5; no new operator decision.

### F-R8-02 — P0 host probes do not implement the host matrix (P4, P8)

**Plan location:** host binding table lines 85, 87-89; P0.2 line 169; P0.7 line 174; TODO task P0.2. **Defect:** The table expressly sends Copilot two-delivery timestamps, Grok Bot cloud payload/session/queue, classic Cline native hook execution/install/payload/queue, and cline-v2 queue behavior to P0.2. P0.2 says there are only four checks: Cowork hooks, Codex thread env, Grok env, and cline-v2 queued runs. A generic P0.7 recheck does not prescribe the omitted live probes or record unavailable probes. **Evidence:** Those lines and the live TODO's matching P0.2 task. **Make PASS:** Add each table probe to P0.2 and its TODO task, with a receipt, pass/fail/unavailable outcome, and the D6/D7 capability disposition. **Classification:** implementer plan/TODO edit applying existing D6/D7.

### F-R8-03 — Mapping rows promise tests not named by their TEST records (P2, P7)

**Plan location:** catalog TEST-MCP-SESSIONCLASS-004 line 379, TEST-MCP-SESSIONREVIEW-003 line 382, TEST-SUPPORT-023 line 389; mappings lines 394, 406, 410. **Defect:** Line 406 maps FR-MCP-SESSIONCLASS-004 to TEST-MCP-SESSIONCLASS-004, but that TEST validates only FR-MCP-SESSIONCLASS-001-AC006 and TR-MCP-SESSIONCLASS-004. Line 394 maps FR-SUPPORT-016 to TEST-MCP-SESSIONREVIEW-003, but that TEST never names FR-SUPPORT-016 ac-5; TEST-SUPPORT-023 does. Conversely, line 410 maps FR-MCP-SESSIONREVIEW-004's lossless decimal ACs to several TESTs but omits TEST-MCP-SESSIONREVIEW-003 even though its named client, MCP, and REST cases are the relevant S5 surface tests. A `createMapping` can accept these links, but the traceability claim is false. **Evidence:** Exact TEST validation sentences and mapping rows; S5 line 597 requires lossless controller round trips. **Make PASS:** Align each mapping edge and TEST validation statement with the ACs it actually tests; explicitly include the lossless REST/MCP/client AC in TEST-MCP-SESSIONREVIEW-003 if that is the intended coverage, or remove incorrect edges and name a different TEST that covers them. **Classification:** implementer plan edit; no operator decision.

### F-R8-04 — Catalog ACs absent from slice RED cases (P7)

**Plan location:** TEST-SUPPORT-023 line 389; S4 RED line 595; updated FR-MCP-SESSIONLIFE-001-AC007 line 439; TEST-MCP-SESSIONTURN-001 line 367; A3 RED line 573. **Defect:** TEST-SUPPORT-023 names `SessionLogTurnReviewSectionTests` in S4, but S4's RED list omits that class and its alias, clear, and item-delete assertions. The updated FR-MCP-SESSIONLIFE-001-AC007 requires both `system-event-skipped` and `turn-open-contended` without minting; TEST-MCP-SESSIONTURN-001 names only `SystemEvents_HookReportsSkipped_NoMint`, while A3 says only "contended lock" and never specifies the no-mint assertion. **Evidence:** The catalog, allocation, and slice lines above. **Make PASS:** Put the exact section test class and cases in S4 RED and its green gate; add a named contended-lock/no-mint test to TEST-MCP-SESSIONTURN-001 and A3 RED, with the matching AC allocation. **Classification:** implementer plan edit.

### F-R8-05 — Universal live smoke contradicts D6 (P4, P8)

**Plan location:** D6 line 31; host rows 83, 87-89; FR-MCP-SESSIONTURN-008 AC004 line 251; end-to-end verification line 629. **Defect:** The plan explicitly permits an installed host with no arrival mechanism to report `turn_logging_unsupported` on every session-log verb and write nothing. Yet line 629 requires *every installed host* to produce one turn, round-trip a review, and show a completion gate message. An unsupported installed Cowork, Cline, or cline-v2 cannot satisfy both. **Evidence:** Contradictory requirements at cited lines. **Make PASS:** Split the live gate by the recorded capability disposition: supported/adapted hosts get positive turn/review/gate smoke; D6-unsupported installed hosts get negative `turn_logging_unsupported`, zero-mint, zero-write smoke and a capability-matrix receipt. **Classification:** implementer plan edit applying D6; no new operator decision.

### F-R8-06 — `skipped_duplicate` is an unresolved wire decision (P5, P7)

**Plan location:** C3 line 119, FR-MCP-SESSIONTURN-004-AC001 line 215, Phase A wire contract line 540, A1 line 569. **Defect:** The plan exposes `skipped_duplicate` and maps it to Node `unchanged` but never defines its trigger, whether a repeated `requestId` within one submit produces its own outcome, whether an identical stored turn does, or what `storedStatus` and `reason` are in either case. Current `SessionLogService.UpsertTurns` deduplicates request IDs inside an incoming submission with last-wins at `src/QBrainAi.Support.Mcp/Services/SessionLogService.cs:1589-1600`; a no-op merge of a stored turn is a distinct possible case. A1 RED does not distinguish them. **Make PASS:** Operator chooses the public meaning: **A** duplicate input entries in one submission are `skipped_duplicate`, with per-input outcomes, or **B** only an identical no-op against a persisted turn is `skipped_duplicate`; define the other case's outcome, cardinality, status/reason, all surfaces, and tests. **Classification:** operator decision, then implementer plan edit.

### F-R8-07 — Handoff overstates risk-ledger parity (P9)

**Plan location:** Risks and incidental bugs lines 617-623; handoff `docs/handoffs/sessionturn-review-plan-20261009T022141Z.md:94`. **Defect:** The handoff says the plan's Risks section "now lists these filed ids" after it lists five extra reports: dialog ordinals, slash-bearing item keys, `CLAUDE_PLUGIN_ROOT` validator behavior, memory action type/order, and dangling `CODEX-HANDOFF.md` references. None of these five IDs appears in plan lines 617-623. All five reports exist in the main live triage store. **Make PASS:** Correct the handoff to say those reports are handoff-only context, or add their IDs and dispositions to the plan Risks section and keep TODO description parity. **Classification:** implementer document edit; no operator decision.

## Cross-section and prior-round check

The stated D2/D9 explicit requestId rules, `request_ambiguous` and `request_unknown`, C9 typed-code list, C1 full IDs, partial QB-AGENT-001 clearance and NOTICE, D10 slice HV timing and verdict storage, D11 Copilot single-hook gate, D12 Node 18/20 raw-token score path, numeric AC ordering, S4 allocation of FR-MCP-SESSIONREVIEW-007-AC003, and stop-gate wording are present. P0 requirements counts, UC-1..UC-9, updated FRs, storage provider migrations, and A1-A9/S1-S11 sequence were read end to end. The earlier r1-r7 receipts were checked for regressions; the defects above are the remaining independently verified defects, not acceptance of the implementer's self-review. No plan approval is warranted.

## Receipt and persistence paths

- Reviewer markdown: `docs/receipts/hv/hostile-validator-plan-readiness-r8-20261009T200525Z.md`
- Reviewer JSON: `docs/receipts/hv/hostile-validator-plan-readiness-r8-20261009T200525Z.json`
- Runner request: `docs/receipts/hv/20261009T200525Z-plan-readiness-r8.request.jsonl`
- Runner response: `docs/receipts/hv/20261009T200525Z-plan-readiness-r8.response.jsonl`
- Own MCP turn: session `Codex-20261009T200737Z-plugin-session`, request `req-20261009T200740Z-prompt-4eb2`, turn 428. Persistence verified by client.SessionLog.QueryAsync after completion: own turn 428 is completed and its response contains the full Markdown and JSON blocks, all ten claim decisions, all seven finding IDs, three actions, one design decision, and two dialog entries (21,926 response characters).
