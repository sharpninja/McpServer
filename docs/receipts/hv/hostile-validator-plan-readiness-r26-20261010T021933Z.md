# Hostile validator plan readiness — round 26

TimestampUtc: 2026-10-10T02:29:39.1257633Z. Verdict: **DISAGREE**. Accuracy **97/100**, completeness **96/100**, confidence **98/100**. P1-P10: 5 PASS, 5 FAIL, 0 UNKNOWN.

Validator: Codex CLI gpt-6-sol xhigh. Live proof: /home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T21-19-34-01a1239b-c635-79d0-80f7-45a06dfc6a04.jsonl: turn_context payload.model=gpt-6-sol, payload.effort=xhigh; runner response JSONL thread.started=01a1239b-c635-79d0-80f7-45a06dfc6a04. Reviewer turn: Codex-20261010T022137Z-plugin-session / req-20261010T022308Z-hv-r26. HEAD: a8193c84ee698ca65b7f77a4bf63bd8b84c648cd.

First actions: add-profile skill read; 19 non-skill profile Markdown files read in full; 398-line main-workspace marker read in full with secret values excluded from output; workflow.memory.list run, all 15 memories read (13 Global, 2 Workspace), including MEMORY-PROCESS-008. No marker secret is included in these receipts.

Scope: 651 plan lines, 531 nonempty; plan SHA256 01C037EF7BC01372499872219ED8AF71DE8BF299F0929AC63E99704579C71473; main-workspace TODO has 30 implementationTasks and is NOT APPROVED. Review only.

## Section-by-section and propagation audit

- Overview, locked decisions, current-state inventory: checked against HEAD, operator rulings and live plugin versions; no new baseline error.
- Host binding table and D6 what-if paths: checked equal-field, bridge ambiguity, unknown/partial/truncated transcript, duplicate hook after delivery, first-event unsupported, open-turn failsafe, review refusal and drain. Round-25 F-R25-01/02 are substantively propagated in Phase A.
- Requirements and mappings: checked existing and proposed IDs, P0 creation, FR/TR/TEST cross-links, support rename and store counts.
- A1-A9 and S1-S11: checked contracts, RED allocation, gates and task order. S8/S9 local completion pre-check conflicts with Phase A unsupported-session close capture (F-R26-01).
- Rollout, backfill, risk and handoff: checked nine-repo sync order, three migration providers, triage records, 531-line description parity and 30 tasks.

## Claims

- **P1 PASS:** At HEAD a8193c84ee698ca65b7f77a4bf63bd8b84c648cd, audited the whole 651-line/531-nonempty plan against source, file and line anchors, routes, hooks, plugin manifests, current build target definitions, and the nine plugin worktrees. Baseline code and plugin inventories match; no false present-tense source assertion found. The F-R26-01 defect is a future-contract contradiction, scored under P3/P5-P8.
- **P2 PASS:** Compared plan IDs with docs/Project and live read-only requirements queries. All 46 extracted existing citations were found; 67 proposed FR/TR/TEST IDs have zero collisions and are assigned to P0. The TODO FR/TR arrays correctly remain empty until creation. Mapping edges and S4 SUPPORT renames were checked.
- **P3 FAIL:** The locked 2026-10-10 unsupported-session option 1 is represented in A7 and FR-SESSIONTURN-005, but Phase B local pre-check in FR-SESSIONCLASS-004 and S8/S9 prevents its close-capture path for a Code turn missing reviews. See F-R26-01.
- **P4 PASS:** Concrete tasks and RED tests cover server, client, REPL, MCP tools, PowerShell and Node cores, eight named agent plugin families plus grok-bot, and SQLite/PostgreSQL/SQL Server migrations. Rollout, backfill and legacy compatibility are explicitly scheduled. This scope inventory does not cure F-R26-01.
- **P5 FAIL:** The implementer must choose whether to obey the local fail-closed pre-check or the already-locked unsupported-session failsafe. The plan does not resolve that choice in the S8/S9 contract or golden contract; see F-R26-01.
- **P6 FAIL:** G1-G6, HV gates, build targets and named test projects exist as planned; P0.1 explicitly handles missing SDK 10.0.201, bats, Pester 5 and dotnet-ef on this host. BDPv4 RED/mocks-green/negative/self-review/real-green/refactor order is specified. However the Phase B local pre-check makes its unsupported Code-turn acceptance path impossible to execute; see F-R26-01.
- **P7 FAIL:** The new Code-turn drain cases in A7 and TEST-SESSIONTURN-006/SESSIONREVIEW-005/008 do not assert that S8/S9 local pre-check permits the unsupported-session close into failsafe. Named pre-check tests cover external writers and genuinely missing data, leaving the key acceptance criterion untestable as written; see F-R26-01. HV-P0/HV-A/HV-B Codex gpt-6-sol xhigh 98/98 instructions are otherwise consistent.
- **P8 FAIL:** A7/FR-SESSIONTURN-005 require capturing a missing-review Code close while S8/S9/FR-SESSIONCLASS-004 require refusing it before any persist. The 30 task order therefore carries a cross-phase contradiction. See F-R26-01.
- **P9 PASS:** Read-only workflow.todo.get from main workspace: description has 531 lines and matches every nonempty plan line exactly; 30 implementationTasks match the plan task list; note says NOT APPROVED. Handoff identifies source, location, status, counts and filed triage IDs accurately. technicalDetails records the round-26 changes as pending HV, without claiming approval.
- **P10 PASS:** Verified plan risk and triage section against source and read-only workflow.triage.getReport for all nine named reports. The relocation group triage-group-804b21086f3019ab exists only under the worktree workspace and remains failed with one grouped report, exactly as disclosed. A9 schedules plugin defects. No unfiled risk item found.

## F-R26-01 — implementer edit

Location: Locked decision line 29; FR-MCP-SESSIONTURN-005-AC001/003 lines 225/227; FR-MCP-SESSIONCLASS-004-AC002/003 lines 284-285; TEST-MCP-SESSIONTURN-006 and TEST-MCP-SESSIONREVIEW-005/008 lines 377/389/392; A7 line 589; S8/S9 lines 611/613.

Defect: The operator-selected unsupported-session option 1 requires an already open Code turn to capture its close in the plugin failsafe while terminal reviews are unavailable, then let the server judge that close at drain. Phase B simultaneously requires the PowerShell and Node plugin cores to run a local completion pre-check that fails closed before any persist. It gives that pre-check no exception for unsupportedSince. A Code turn without terminal reviews therefore cannot submit the close to the failsafe; Stop continues to block on a remedy that the plugin rejects. The drain, server rejection, listed-turn recovery, and later review through C5 never start.

Evidence: Plan line 225 expressly says reviews are refused in an unsupported host session and close calls for previously delivered turns are captured; line 227 excepts only the Stop completion-gate block. Line 284 requires a local pre-check before any persist; S8 and S9 mandate that pre-check without an unsupportedSince bypass. Their RED lists test external-writer and missing-data behavior, but not an unsupported-session close passing to failsafe. The Code-turn drain test at lines 377 and 389 presupposes that missing close can be captured. This is a cross-phase contradiction, not a current-code claim.

Required fix: Specify in FR-MCP-SESSIONCLASS-004-AC002/003 and the golden contract that, only for a turn delivered and still open when unsupportedSince was atomically recorded, plugin close goes to the unsupported_session failsafe without a local terminal-review rejection; keep the model-evidence check and all other-turn refusals. Apply this exception in S8 and S9 and their host adoption tasks. Add RED PowerShell and Node cases that start with a Code turn missing reviews, enter unsupportedSince, prove the close is captured with no server write, Stop warns, the drain returns session_turn_completion_gate and keeps the original pair listed/in progress, review through C5 succeeds, and a later close succeeds. Map the cases to FR-SESSIONTURN-005-AC001/003, FR-SESSIONCLASS-004-AC002/003, and TEST-SESSIONTURN-006/SESSIONREVIEW-005/008; sync the TODO description and implementationTasks and the handoff.

Operator decision: None. Operator option 1 on 2026-10-10 already decides this path; the implementer must propagate it.

## Prior rounds and recurrence

Re-attacked F-R25-01 and F-R25-02: reviews now stay out of failsafe and all listed mid-session D6 causes record unsupportedSince and use option 1; their direct cases appear in A7/S8/S9/TEST. Swept r1-r24 receipts (r1 uses the unsuffixed 20261009T145521Z receipt), including prior source, requirement, nonce, bridge, stop, C2/C5/C9, host adoption and parity findings. No independently revived earlier defect found; F-R26-01 is a new cross-phase recurrence of the r24 unsupported-session deadlock after Phase B pre-check.

## Receipts and persistence

- docs/receipts/hv/hostile-validator-plan-readiness-r26-20261010T021933Z.md
- docs/receipts/hv/hostile-validator-plan-readiness-r26-20261010T021933Z.json
- docs/receipts/hv/20261010T021933Z-plan-readiness-r26.request.jsonl
- docs/receipts/hv/20261010T021933Z-plan-readiness-r26.response.jsonl

Reviewer turn updateTurn persisted:true before receipt write; terminal read-back to be recorded after completion.

Terminal persistence proof: workflow.sessionlog.updateTurn and completeTurn returned persisted:true. Read-only client.SessionLog.QueryAsync at 2026-10-10T02:32:05.7439921Z found reviewer turn req-20261010T022308Z-hv-r26 in session Codex-20261010T022137Z-plugin-session, status completed, response 9,023 characters with P1-P10 and F-R26-01, three actions and one dialog.
