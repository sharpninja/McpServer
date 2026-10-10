# Hostile validator — plan readiness round 38

TimestampUtc: 2026-10-10T07:18:39Z
ValidatorIdentity: Codex CLI gpt-6-sol xhigh. Own rollout `/home/sharpninja/.codex/sessions/2026/10/10/rollout-2026-10-10T02-01-51-01a1249e-3731-7d90-99f9-60eec9cebcf4.jsonl` has `turn_context.payload.model=gpt-6-sol` and `payload.effort=xhigh`; thread id `01a1249e-3731-7d90-99f9-60eec9cebcf4`.
ReviewerSessionId: `Codex-20261010T004320Z-plugin-session`. ReviewerRequestId: `req-20261010T070512Z-plan-r38-hv`.
Preparation: add-profile skill and all 19 non-skill global profile Markdown files read in full; root marker read in full, suppressing credential lines and the Authentication block from output; `workflow.memory.list` returned 15 effective memories, all read, including MEMORY-PROCESS-007/008.
Scope: plan readiness only. No plan step executed. No product code, TODO, requirement, triage, plugin repository, memory, or settings edit.
HeadSha: `b69c18870c456fac1efbf903ec2bfedb0bb7f2b6`. Product source diff from `f56dcf70`: zero paths under src, tests, build, plugins.
Artifacts: plan 652 lines / 532 non-empty. Live main-workspace TODO is NOT APPROVED, done=false, 30 implementationTasks; its description exactly equals the plan's 532 non-empty lines. Handoff status, counts, source/TODO paths and round chronology agree.
OverallVerdict: **DISAGREE**. Accuracy **97**, Completeness **94**, Confidence **98**. PASS 6, FAIL 4, UNKNOWN 0. The all-claims-PASS and 98/98 rules are not met.

## Claims

- P1 Source accuracy **PASS**. Current build, server, plugin and host source anchors were checked; all nine sibling plugin repositories exist, with dated inventory separated from current state. No product-source delta from the audited baseline.
- P2 Requirements accuracy **PASS**. Effective store confirms cited existing FR-MCP-SESSIONLIFE-005, TR-MCP-CLEARSESSION-001 and TEST-MCP-PLUGIN-011. Proposed FR-MCP-SESSIONTURN-005 and TEST-MCP-SESSIONREVIEW-005 return not_found, as P0 creations. The full proposed ID audit found 21 FR, 23 TR, 23 TEST and zero collisions; mapping rows align with the generated docs and store.
- P3 Locked decisions **PASS**. D1-D12, partial clearance, licensing, skipped_duplicate removal and option-1 unsupported-session failsafe remain reflected. Round-37 Stop, withdrawn Node, receipt and evidence-first repairs are present.
- P4 Scope **FAIL**, F-R38-01 and F-R38-02. Conditional queue adapters lack implementation contracts; S8 clear-session work lacks the required selection path.
- P5 Decision completeness **FAIL**, F-R38-01 and F-R38-02. A positive queue event and pre-S10 clear-session behavior still require design choices.
- P6 Executability **PASS, static**. G1-G6 named current paths and targets exist; the A9 `--plugin-version` parameter is explicitly introduced/tested before first use. P0.1 gates host prerequisites. Every slice specifies contract/stubs, RED, mocks-green with negative check, self-review, real green, refactor and gates. No future suite was run because the plan is unapproved.
- P7 Testability **FAIL**, F-R38-01 and F-R38-02. Positive queue-event adapters have no per-host fixture tests. The clear-session test is in TEST-MCP-SESSIONREVIEW-005 but the S8 task omits the behavior and its RED case before the S8 gate.
- P8 Internal consistency **FAIL**, F-R38-02. S8 requires selective clear-session closure while the only specified pre-check never refuses a close and the real server gate first lands in S10. F-R38-01 also contradicts P0.2's claim that no new design is made.
- P9 Artifact parity **PASS**. 532 non-empty source lines match the TODO description; 30 tasks equal 1 approval + 9 P0 + 9 A + 11 S. Handoff and TODO note say NOT APPROVED; technicalDetails includes the latest repairs.
- P10 Risks **PASS**. Nine cited triage reports resolved read-only under the main workspace; restore/clone group resolves in the worktree with status failed and is absent under the main workspace as stated. Provider, bridge-format and file-lock risks are described or scheduled.

## FAIL findings

### F-R38-01 — positive enqueue-event branch has no adapter contract

Plan locations: host binding table lines 79, 84, 88-93; P0.2 line 173; A3 line 582, A8 line 592, A9 line 594; TEST-MCP-SESSIONTURN-001/008/009 lines 373, 380-381.

Exact defect: P0.2 asks whether a processing-time host has an enqueue event. Line 84 then selects an arrival adapter and line 173 says no new design is made. For Codex, Grok, Grok Bot, Copilot and Cline v2, the plan defines no enqueue-event payload, stable event identity, correlation to later UserPromptSubmit/beforeRun, delivery/withdrawal mapping, replay cursor, or named positive-branch fixture test. The rows' nonces come from processing-hook fields that an enqueue event need not expose. A positive probe therefore leaves the implementer to design the adapter and its support disposition.

Evidence: Claude Code has a fully specified bridge at line 86; the other host rows only name prompt-hook fields or a generic queue adapter. P0.2 records existence rather than the fields needed to bind two disparate events. A3/A8/A9 tests cover general mint/state and unsupported outcomes, not each positive queue-event path. This is the what-if and propagation gap identified using MEMORY-PROCESS-008.

Change to PASS: for each conditional host, specify observed event payload, nonce/correlation, host-session key, enqueue/delivery/withdrawal/replay rules, owner slice and RED/green fixtures; or deterministically classify a processing-time host as unsupported until a separate, specified adapter is approved. P0.2 must collect the evidence required for that branch and propagate its result to the feature matrix, TODO tasks and HV gates.

Classification: implementer edit to plan/TODO/handoff under D5/D6. No new operator decision is required if the supported branch is concretely specified or the host is marked unsupported. Processing-time minting as supported would require an operator reversal and is not assumed.

### F-R38-02 — S8 clear-session criterion has no pre-S10 implementation path

Plan locations: FR-MCP-SESSIONCLASS-004-AC002 line 285; updated TR-MCP-CLEARSESSION-001-AC001 line 474; TEST-MCP-SESSIONREVIEW-005 line 390; TDD allocation line 537; S8 line 612; S10 line 616; S11 line 618.

Exact defect: At S8, clear-session must leave every turn with unmet completion criteria in progress and report each missing item. The server gate is first implemented in S10; the only S8 plugin pre-check is explicitly advisory and never refuses a close. S8 names shim, REPL invocation and session-skill work, but neither a clear-session coordinator nor an update to its skill nor its named RED case. The current Claude plugin `skills/clear-session/SKILL.md:21-39` tells the model to complete an open turn and confirm none remain. Following the S8 task on a pre-S10 server can close an unready turn, failing the S8 AC and test.

Evidence: lines cited above; current skill lines 21, 24 and 39; TEST-005 names `ClearSession_GateUnmetTurn_StaysInProgress_Reported` but S8 does not. S11 only schedules later skill-text validation.

Change to PASS: add an S8 clear-session workflow task that enumerates every open turn by original pair, reads class/evidence/reviews, applies the S1 golden completion policy as a selection step, sends close only for ready turns, and reports unready ids with exact missing items. State that ordinary close verbs retain advisory pre-checks. Put the named RED case, plugin skill test and clear-session skill edit in S8/G6. Alternatively move the AC/test to S10 with a documented interim behavior and dependency update, but that would change the promised S8 allocation.

Classification: implementer edit to plan/TODO/handoff. Existing AC already chooses selective closure; no operator decision is needed.

## Prior rounds, propagation and phase boundaries

F-R37-01..04 are repaired: three-branch Stop text is in lines 226/464/551/590; A8/TEST-008 include the withdrawn Node case; S8/S9/TEST-005/008 include no-listed-claims receipts; C8/AC003/S8/S9/S10 provide evidence-first verified write, retained replay parts and restart readback. The implementer's broader assertion that the S10 restart test is mapped in TEST-005/008 is imprecise: direct real-gate restart mapping is TEST-MCP-SESSIONCLASS-002 line 383; TEST-005/008 map the S8/S9 server-double cases. That narrower mapping is sufficient.

Reviewed r1-r36 receipts (r1 unsuffixed) and re-attacked recurring identity, queue, alias, score, marker, migration, sync and gate themes; no additional independently supported regression was found. Recurrence signal: a cross-surface AC was mapped in a TEST row without its executing slice task, and a conditional supported path was asserted without an adapter contract. P0.8 otherwise puts A3 after A1, A4 after A3, A7 after A3-A6, A9 after A2-A8 and S1-S11 in order. F-R38-01 needs P0.2 evidence and adapter work in A3/A8/A9; F-R38-02 needs a selective workflow in S8 or a corrected phase boundary.

Reviewer turn persistence proof: Read-only client.SessionLog.QueryAsync through the Codex wrapper found the reviewer turn in_progress with 3 actions and 2 dialog items; the observation includes P1, P10, F-R38-01 and F-R38-02. appendDialog returned code persisted and persisted true.
Marker credential scan of runner response JSONL: One credential field found in the root marker; zero occurrences of its value in the runner response JSONL.

Final read-only QueryAsync after completeTurn found status completed, 3 dialog items, 5 actions, and the last observation containing P10, F-R38-01 and F-R38-02.
