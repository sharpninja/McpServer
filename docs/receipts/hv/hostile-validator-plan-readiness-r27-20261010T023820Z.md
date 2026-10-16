# Round 27 hostile validation — plan readiness

TimestampUtc: 2026-10-10T02:54:36.5473322Z. Codex live proof: /home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T21-38-21-01a123ac-f8ad-7d12-84d2-d9486c402558.jsonl:8 turn_context 2026-10-10T02:38:22.458Z. Reviewer turn: Codex-20261010T024208Z-plugin-session / req-20261010T024329Z-hv-r27.

**DISAGREE. Accuracy 96/100; completeness 94/100; confidence 98/100. Five PASS, five FAIL, zero UNKNOWN.** Plan remains NOT APPROVED.

First actions: add-profile skill and all 19 non-skill profile Markdown files read in full; 398-line main marker read in full with authentication secrets filtered from output; signature and bootstrap health nonce verified; workflow.memory.list returned 15 effective memories, all read, including MEMORY-PROCESS-007 and -008. No secret is included.

Scope: whole 651-line plan, live main-workspace TODO description/30 tasks/note/technicalDetails, handoff, HEAD 0498e4d5 source, docs/Project, docs/context, nine plugin repos, live read-only requirements and triage stores, R26 and R1-R25 findings. Historical conduct, infrastructure repair and Pi3 excluded. No product, plan, TODO, requirement, triage, memory, setting or other turn changed.

## Claims

- **P1 PASS:** Whole 651-line source checked at HEAD 0498e4d5; src/tests/build/plugins unchanged from verified baseline. Routes, types, hooks, build targets, migration providers, nine plugin heads and manifests match.
- **P2 PASS:** Read-only live store totals: 354 FR, 482 TR, 514 TEST. Proposed 21/23/23 IDs are absent and assigned to P0; cited existing IDs resolve; canonical mapping IDs and updated-FR edges checked.
- **P3 FAIL:** D1-D12 and later rulings stated. Phase A first and D10 all-green exit cannot coexist with A7 requiring S5/S10 behavior before Phase B. F-R27-01.
- **P4 PASS:** Tasks and tests cover server, client, REPL, MCP tools, PowerShell and Node cores, eight required plugin families plus grok-bot, SQLite/PostgreSQL/SQL Server schema, rollout and compatibility.
- **P5 FAIL:** Implementer must decide A7/S8/S9 gate staging, local pre-check activation and exact IDs/text of 23 TEST acceptance criteria. F-R27-01..03.
- **P6 FAIL:** Named G1-G6 commands/paths exist; P0.1 handles local toolchain; BDPv4 order explicit. A7/S8/S9 real gate cases cannot green before S10, and P0.4 cannot create exact TEST AC payloads. No product tests run in read-only review.
- **P7 FAIL:** HV-P0/A/B gpt-6-sol xhigh 98/98 consistent. TEST-SESSIONTURN-006 and TEST-SESSIONREVIEW-005/008 allocate S10-only assertions to earlier slices; 23 TEST rows lack self structured AC IDs/text.
- **P8 FAIL:** Thirty task headings/dependencies agree with TODO. A7 precedes S5/S10; S8/S9 precede S10 while requiring its gate rejection; active pre-check could refuse a server-accepted close; P0.4 conflicts with TEST catalog.
- **P9 PASS:** Plan 651 lines/531 nonempty; TODO description exact 531/531 nonempty-line parity, 30 tasks, note NOT APPROVED; technicalDetails and handoff status/counts/paths/prior round/triage accurate.
- **P10 PASS:** Nine plan-scope triage reports resolve; exceptional group exists under worktree only, failed with one grouped report; handoff extras and licensing report resolve. A9 schedules plugin defects.

## Findings

### F-R27-01

Plan location: FR-MCP-SESSIONTURN-005-AC001 line 225; TEST-MCP-SESSIONTURN-006 line 377; A7 line 589; BDPv4/phase gates lines 139-161; S5 line 605; S10 line 615.

Exact defect: A7 requires a drained Code close to be rejected by session_turn_completion_gate, followed by review through C5 and a later close. Review first appears S5, server gate S10. A7 must pass every gate and HV before Phase B.

Evidence: At HEAD rg finds no gate or RecordTurnReviewAsync in SessionLogService.cs, SessionLogController.cs or SessionLogClient.cs. A7 RED and TEST-006 require the end-to-end case.

Make PASS: Keep A7 RED/green on capture, Stop warning and successful pre-gate drain. Move rejection/review/later close and AC/Test allocation to S10 after S5; name S10 RED, integration and live proof; reconcile TODO tasks/technicalDetails and handoff.

Classification: Implementer plan edit; no new operator decision.

### F-R27-02

Plan location: FR-MCP-SESSIONCLASS-004-AC002/003 lines 284-285; TEST-MCP-SESSIONREVIEW-005/008 lines 389/392; S8/S9 lines 611/613; S10 line 615; dependencies line 178; rollout line 622.

Exact defect: S8/S9 enable local completion pre-check and require real drained-close gate rejection before S10 server gate. Before S10, server accepts missing-review Code closes, so local refusal violates AC002 promise to accept every server-accepted close. Real drain tests cannot pass at S8/S9 exits.

Evidence: S10 depends on S8/S9; only S10 adds server transition gate. Current server/client source has no gate symbol.

Make PASS: Specify pre-S10 disabled mode with explicit server gate capability and activation contract/tests. S8/S9 test future rule against a double; move real drain rejection and activation cases to S10 against its server. Update AC brackets, TEST, golden contract, rollout, TODO and handoff.

Classification: Implementer plan edit; no new operator decision.

### F-R27-03

Plan location: P0.4 line 174; AC convention line 183; 23 new TEST catalog rows lines 368-394.

Exact defect: P0.4 requires every new TEST to carry exact structured acceptanceCriteria and saved-versus-catalog AC parity. None of 23 TEST rows supplies its own TEST-...-AC### ID or exact AC text. Compound validations could be one AC or several.

Evidence: All 23 TEST rows read; zero contains a self AC entry; FR/TR rows enumerate ACs. P0.4 requires id/text/isSatisfied and stops on a differing AC.

Make PASS: Enumerate each TEST self AC ID and objective text with slice allocation, mapping and RED coverage. Define exact title/statement/AC extraction for createBatch and update TODO/handoff.

Classification: Implementer plan edit; no new operator decision.

## Propagation and recurrence

R26 F-R26-01 is textually repaired in FR-MCP-SESSIONCLASS-004-AC002, golden contract, S8/S9 and TEST mappings. A8 no-outbox-before-drain is in TR-MCP-SESSIONTURN-008 AC-006, A8 and TEST-008. R1-R25 finding classes swept across nonce identity, bridge lifecycle, D2/C5, C2/C9, stop gate, review, plugin adoption, mappings and parity; no separate old regression established. F-R27-01/02 recur at the R24/R26 phase boundary. F-R27-03 is a P0 catalog omission.

Reviewer-turn persistence proof is recorded in the MCP turn and runner response JSONL after this receipt write.

## MCP persistence proof

Read-only client.SessionLog.QueryAsync through the installed Codex wrapper found session Codex-20261010T024208Z-plugin-session and turn req-20261010T024329Z-hv-r27 with status completed, 14,875 response characters (the full Markdown and JSON verdict at completion), three actions and one decision dialog. The updateTurn and completeTurn responses both returned persisted:true. Read-back UTC: 2026-10-10T02:56:44.0242359Z.
