# Hostile validator: plan readiness round 5

TimestampUtc: 2026-10-09T18:14:14Z (request stamp; evaluation continued afterward).  
Verdict: **DISAGREE**. Accuracy 87/100; completeness 80/100; confidence 97/100. Threshold: 98/98 and every applicable claim PASS. Results: 2 PASS, 8 FAIL, 0 UNKNOWN.

## Identity, intake, and scope

- Live session metadata: Codex rollout `01a121df-759a-7012-b373-07c5b9f4f82a`, `session_meta` timestamp `2026-10-09T18:14:15.756Z`, `originator=codex_exec`, cwd this worktree; `turn_context` says `model=gpt-6-sol`, `effort=xhigh`. Proof: `/home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T13-14-15-01a121df-759a-7012-b373-07c5b9f4f82a.jsonl`.
- Read the add-profile skill and all 19 non-skill Markdown files in `/home/sharpninja/.claude/profile/` in full; profile load notice was emitted at `18:14:52Z`. Read `/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml` in full; its secret is omitted. Ran `workflow.memory.list` and read all memories: 12 global in the worktree view, 14 effective in the main-workspace view.
- HEAD `4fd86b88306b3becf02cd69ca6fbd6191fff5aed`. Reviewer session `Codex-20261009T032846Z-plugin-session`; request `req-20261009T181414Z-plan-readiness-r5`. Reviewed only plan readiness; historical conduct, network/SQL/MCP infrastructure, and Pi3 monitor are excluded.
- Live main-workspace TODO status is NOT APPROVED. Its description is byte-equal to all 508 non-empty plan lines; 30 implementationTasks. Handoff counts match 625 total/508 non-empty lines.

## Claim results

- **P1 FAIL:** Current-state plugin inventory contains false known-list, hash, checkout, and branch-count assertions (F-R5-03).
- **P2 FAIL:** The new SESSIONTURN/SESSIONCLASS/SESSIONREVIEW families are collision-free in the 352 FR/480 TR/512 TEST live store, and 21 FR/23 TR/23 TEST with 21 mappings are listed. The three new SUPPORT-010G IDs fail the actual createBatch ID grammar (F-R5-01).
- **P3 FAIL:** D1-D5 and D7-D9 largely appear, but the no-model-mint promise and unconditional Cowork arrival AC conflict with D6 and permitted unsupported outcomes (F-R5-04, F-R5-06).
- **P4 PASS:** The plan contains concrete server, client, REPL, MCP, PowerShell/Node core, all nine plugin, three-provider migration, test, and compatibility work. The stale TODO and handoff are scored under P9.
- **P5 FAIL:** The policy for Code/Docs slice turns between phase hostile reviews remains an explicit operator decision (F-R5-05).
- **P6 FAIL:** P0's createBatch IDs and generateDocument docTypes cannot execute as written (F-R5-01, F-R5-02). Named build targets and test project paths exist, and BDPv4 sequence is written per slice. Current SDK 10.0.201/Pester/bats provisioning is assigned to P0.1, so was not scored.
- **P7 FAIL:** Cowork AC009 is unsatisfiable under a permitted unsupported-host result. The phase live check does not require D9 ambiguous-write refusal (F-R5-06, F-R5-08). HV-P0/A/B and the 98/98 threshold otherwise appear.
- **P8 FAIL:** The model-facing mint contradiction, slice review gap, phase merge dependency, and eight/nine task drift conflict (F-R5-04, F-R5-05, F-R5-07, F-R5-09).
- **P9 FAIL:** TODO description parity, count, and status pass; its A9 task says eight plugins and handoff line 31 enumerates eight, while the plan and handoff line 37 say nine (F-R5-07).
- **P10 PASS:** Source-backed risks and nine cited main-workspace triage reports were verified. The corrected restore/clone group exists under the worktree with one grouped report and group status failed; relocation to main remains pending as stated. No listed risk was found unfiled or unscheduled.

## Failures and exact corrections

### F-R5-01 — implementer edit

- Plan: P0 steps 3-4, lines 166-167; catalog/mapping lines 180, 335, 385, 390. Claims P2, P6.
- Defect: `FR-SUPPORT-010G`, `TR-SUPPORT-010G`, `TEST-SUPPORT-010G` cannot be created by the prescribed `workflow.requirements.createBatch`; the final suffix must be three digits, and `010G` fails. The P0 `getFr/getTr/getTest` not-found check also cannot validate them as valid new IDs.
- Evidence: `src/QBrainAi.Repl.Core/RequirementsWorkflow.cs:32-34,400-406,774-790,808-849`. A read-only regex check returned `valid=False` for all three. Valid, free candidates checked against the live store include `FR-SUPPORT-016`, `TR-SUPPORT-CORE-016`, `TEST-SUPPORT-023`. Existing source/tests already cite `FR-SUPPORT-010G` 36 times in nine files.
- To PASS: Select valid unoccupied IDs and update the P0 catalog, mappings, AC references, and existing source/test citations, with an explicit P0 exception for those citation edits. Alternatively deliberately expand the ID grammar and test that before creation. This is an implementer correction unless the operator specifically wants a new ID scheme.

### F-R5-02 — implementer edit

- Plan: P0 step 6, line 169. Claim P6.
- Defect: `workflow.requirements.generateDocument` is called with `functional|technical|testing|mapping`; it accepts `fr|tr|test|matrix|all` only.
- Evidence: `RequirementsWorkflow.cs:606-623,873-879`. The typed `RequirementsClient.GenerateAsync`/controller separately accepts `mapping` (`RequirementsController.cs:935-947,1417-1430`).
- To PASS: Use workflow docTypes `fr`, `tr`, `test`, `matrix`; name the typed-client call for mapping, or add and test workflow mapping support first.

### F-R5-03 — implementer edit

- Plan: plugin inventory lines 61, 64, 66, 68, 71-72, 90. Claim P1.
- Defect: The current-state list says `Build.SyncAgentPlugins` knows nine repos, every local `plugin-hook.ps1` has SHA prefix `e4cd6116`, four checkouts are absent, and seven default branches are main. In fact the build list has eight; grok-bot's actual hook hash begins `D917FEB6`, while its manifest declares `e4cd6116`; cowork, copilot, cline-v2, and opencode exist locally; eight repos default to main. Line 68 and A9 themselves contradict line 61 by scheduling grok-bot's addition.
- Evidence: `build/Build.SyncAgentPlugins.cs:19-28`; read-only git/hash inventory under `/home/sharpninja/github`. Grok-bot is local at `99fcee9` on main, its `CORE-MANIFEST.yaml:14` declares the other hash; opencode alone uses master.
- To PASS: Refresh the dated inventory and P0 acquisition branches; distinguish actual file hash from manifest declaration; address grok-bot core drift.

### F-R5-04 — implementer edit, or operator decision if model mint is intentional

- Plan: coordination C12 line 124, TR-MCP-SESSIONTURN-004 line 339, A1 line 551, A4 line 557. Claims P3, P8.
- Defect: A4 asserts the model-facing verb/tool lists contain no mint verb and only hooks/adapters call `mintTurn`. C12, TR, and A1 explicitly expose `workflow.sessionlog.mintTurn` and MCP `sessionlog_mint_turn` with no private boundary or capability guard.
- Evidence: Those plan lines and the current model-visible `[McpServerTool]` pattern in `src/QBrainAi.Support.Mcp/McpStdio/FwhMcpTools.SessionLog.cs`.
- To PASS: Specify hook/adapter-only ingress, or enforce and test a host-only capability that refuses model calls. If the author intends a model-invocable mint, the operator must decide whether to relax D6 and the round-5 no-model-mint constraint.

### F-R5-05 — operator decision: slice-turn hostileReview policy

- Plan: completion rule line 50; per-slice process line 136; HV/transition lines 149, 156; gate line 589. Claims P5, P8.
- Defect: Code/Docs completion requires terminal `hostileReview`, while the plan says no per-slice hostile review and provides only phase HV-P0/A/B. It does not say how intermediate Code/Docs slice turns can complete truthfully.
- Evidence: `MEMORY-PROCESS-007` at `18:08:08Z` explicitly says this is unsettled and will be asked of the operator before approval.
- To PASS: Operator chooses **A** review each Code/Docs slice turn before completion; **B** keep slice turns open until phase HV and complete with that result; or **C** define a truthful terminal `Other` policy with required reason. Then align gates, tests, and transitions.

### F-R5-06 — implementer edit

- Plan: host table line 81, P0.2 line 165, `FR-MCP-SESSIONTURN-001-AC009` line 191. Claims P3, P7.
- Defect: AC009 unconditionally requires Cowork arrival mint, while P0.2 permits Cowork to lack executable hooks and be `turn_logging_unsupported` under D6. AC009 cannot pass in that permitted outcome.
- Evidence: The three plan locations and locked D6.
- To PASS: Qualify AC009 for supported Cowork installations and specify an unsupported-outcome AC, test, and feature-matrix entry; or require a verified Cowork adapter before approval.

### F-R5-07 — implementer edit

- Plan: A9 line 567; live TODO implementationTask A9; handoff line 31, contrasted with handoff line 37. Claims P8, P9.
- Defect: Plan A9/D8 require exactly nine plugins, but the TODO A9 task still says all eight, and handoff scope lists eight without grok-bot.
- Evidence: Read-only `workflow.todo.get` from the main workspace returned 30 tasks and the stale A9; handoff `docs/handoffs/sessionturn-review-plan-20261009T022141Z.md:31,37`; plan line 567. Description parity remains 508/508.
- To PASS: Resync task and handoff to nine, naming grok-bot, while retaining description parity and 30-task count.

### F-R5-08 — implementer edit

- Plan: Phase A live check line 609. Claim P7.
- Defect: “every write ... on the turn the D2 rule assigns” does not require D9 `request_ambiguous` refusal while R1 and R2 are delivered and open. A silent wrong binding could satisfy that live check.
- Evidence: Operator D9 and A4 unit cases line 557 explicitly require ambiguous refusal and R1,R2,R1, but the live check omits them.
- To PASS: Add a live R1,R2,R1 test: unqualified write is refused with zero writes, explicit IDs bind correctly, and after one closes an unqualified write binds the sole open request.

### F-R5-09 — implementer edit

- Plan: HV/PR gate lines 148-156 and A9 transition line 567. Claim P8.
- Defect: The plan says a phase PR can merge only after HV AGREE and approval, but does not require that all phase PRs actually merge before the next phase begins. `MEMORY-PROCESS-006` requires that order.
- Evidence: Those plan lines and live memory.
- To PASS: Make next-phase start depend on HV AGREE, approval, all phase PR merges, and verified merged heads; update implementationTasks and handoff.

## Round-4 and earlier re-attack

Read all earlier plan-readiness receipts. Round 4 was DISAGREE 88/76. F-R4-01 through -05 and -07 are corrected in plan text: explicit plugin parent/nine receipts, repeated Stop gate/host override, atomic offset state/crash tests, D9 ambiguity refusal, Copilot duplicate-source handling, and accurate worktree triage group. F-R4-06 remains F-R5-04. D4 Other reason, D5 arrival bridge, D7/D8 nine-plugin matrix, and earlier triage issues were rechecked. New failures above independently block approval.

## Limits and persistence

No build was run during this read-only review. Named targets/project paths were inspected; no network, SQL, or Pi3 issue was scored. Full receipt text is copied into the reviewer MCP turn, then queried back for persistence proof. Runner request and response JSONL live alongside this receipt with prefix `20261009T181414Z-plan-readiness-r5`.

Server read-back at 2026-10-09T18:37:01Z: own reviewer turn status in_progress, 2 dialog items, 4 actions. Full receipt content returned with 11227 characters; exact match after the plugin's nine em-dash-to-hyphen conversions. Session Codex-20261009T032846Z-plugin-session, request req-20261009T181414Z-plan-readiness-r5.
