# Hostile validator: PLAN-SESSIONTURNREVIEW-001, round 37

- TimestampUtc: 2026-10-10T06:53:45Z
- ValidatorIdentity: Codex CLI, `gpt-6-sol`, effort `xhigh`. Live proof: response stream `thread.started.thread_id=01a12489-b132-7550-adb9-542385042b57`; matching local rollout `turn_context` has `model=gpt-6-sol`, `effort=xhigh`.
- HeadSha: `c60be14746b2bddd6fa3c81bf57a62e493c319da`
- Reviewer sessionId: `Codex-20261010T004320Z-plugin-session`
- Reviewer requestId: `req-20261010T064304Z-plan-r37-hv`
- MCP persistence proof: read-only `client.SessionLog.QueryAsync` through the Codex wrapper returned this session and requestId, the full F-R37-04 result, the decision dialog, and the receipt-creation action after their writes. The local turn cache recorded 3 actions, 1 dialog, and 1 design decision before completion.
- OverallVerdict: **DISAGREE**. Accuracy 96; completeness 93; confidence 98. PASS 4, FAIL 5, UNKNOWN 1.
- Scope: readiness of the unapproved plan, its MCP TODO, and the handoff. Historical conduct, infrastructure repair, and Pi3 were excluded.

## Mandatory preparation and artifact checks

Read the add-profile skill and all 19 non-skill Markdown files in `/home/sharpninja/.claude/profile/` in full. Read the main-workspace marker's 397 lines in full; marker output excluded every line containing `apiKey`, `api_key`, or `X-Api-Key` and the Authentication block. Called `workflow.memory.list` through the Codex wrapper with the worktree cache and main-workspace path; read all 15 returned memory texts, including the full long `MEMORY-PROCESS-007` and `MEMORY-PROCESS-008`. The latter supplied the what-if, propagation, recurrence, and phase-boundary checklist. No credential value is in this receipt or the runner response stream; a local comparison against four marker credential fields returned false for the response stream.

Read all 652 plan lines (532 non-whitespace), the handoff, round 36, and the round 1–35 receipt set (one JSON receipt for each round). The main-workspace `workflow.todo.get` returned 30 implementation tasks and `NOT APPROVED`; its 532 description entries match the plan's non-empty lines byte-for-byte as strings. The handoff's status, counts, paths, chronology, and risk IDs match those artifacts. `workflow.requirements.effective` was read from the main workspace; the 21 proposed FR, 23 TR, and 23 TEST IDs do not collide with the effective store. Contextual mentions of invalid or stale historical IDs are identified as such in the plan. The source tree has no changes since the earlier source-anchor audit, and the current HEAD was checked for the named server, client, build, plugin-core, and host paths. All nine risk-section triage report IDs and the worktree triage group returned records through read-only Codex-wrapper calls.

Round 36's three specific repairs are present: the PowerShell withdrawn-before-delivery unsupported-session case, deterministic `otherNotes` fallback in S4 and UNKNOWN-only plugin cases, and the corrected wire Stop clause. The new findings below are sibling/propagation gaps, not rejections of those repairs. The dependency chain and BDPv4 slice order were reviewed against every phase; no newly assigned test was found before its named dependency. The S10 real-gate cases remain in S10. The plan source and TODO technicalDetails were checked for rule propagation; the TODO description is current, but it repeats the plan defects by parity.

## Claim verdicts

- **P1 Source accuracy — PASS.** The current HEAD matches the audited source anchors; spot checks confirmed the server/build paths and the Node core `node >=20` engine. No code-file delta from the audited baseline was found. Dated host observations are explicitly dated.
- **P2 Requirements accuracy — PASS.** Proposed IDs are P0 creations with no live collisions; operative referenced IDs and mapping edges were checked against project docs and the effective store. Historical invalid/stale examples are labelled.
- **P3 Locked operator decisions — FAIL.** F-R37-01 leaves D5's queued warning contradicted in the updated existing FR; F-R37-04 leaves option 1's captured evidence without a durable path after a rejected drain.
- **P4 Completeness of scope — FAIL.** F-R37-02 omits the Node-core equivalent of the newly promised withdrawn-turn unsupported-session transition, or a supported-host exclusion grounded in P0.2.
- **P5 Decision completeness — FAIL.** F-R37-04 leaves rejected close evidence handling to the executing agent.
- **P6 Executability — UNKNOWN.** Named source files, build targets, projects, and receipt directories exist, and the per-slice contract/stubs → RED → mocks-green with negative check → self-review → real green → refactor sequence is written. This review-only run did not execute G1–G6 across the nine external plugin repos or Node version matrix, so the claim that every command works cannot be proved here. This is an execution-evidence limit, not permission to skip a gate.
- **P7 Testability — FAIL.** F-R37-02 and F-R37-03 leave accepted edge cases without a plugin-core test in the applicable slice; F-R37-04 has no restart/readback assertion for rejected close evidence.
- **P8 Internal consistency — FAIL.** F-R37-01 is a direct AC/wire contradiction; F-R37-04 conflicts with C8's preservation contract and S10's atomic rejection. The task count and dependencies otherwise align.
- **P9 Artifact parity — PASS.** TODO description, 30 task labels, not-approved note, and handoff metadata match the plan. The TODO's chronological technicalDetails notes are consistent with the latest sync.
- **P10 Risks — PASS.** The nine cited reports and the misplaced worktree triage group exist. The bridge-format and database-provider risks are stated, and no unfiled incidental issue was found in that section.

## FAIL findings

### F-R37-01 — queued turn rule is still contradictory

- Plan location: `Updated existing requirements`, line 464 (`FR-MCP-SESSIONLIFE-005-AC007`); also FR-MCP-SESSIONTURN-005-AC001 line 226 and its mapped TEST-MCP-SESSIONTURN-006 line 378 and A7 line 590.
- Defect: AC007 requires blocking while **any** request turn opened since the last Stop is `in_progress`. A queued request is minted as a server turn at enqueue, so it is opened and in progress. The corrected wire Stop clause at line 551 and D5 require that exact state to warn and not block before delivery. The first sentence of AC001 and the test name `StopGate_BlocksWhileTurnOpenedSinceLastStopIsInProgress` preserve the broad reading.
- Evidence: line 551 explicitly names `StopGate_QueuedInProgressServerTurn_NoBlockBeforeDelivery_Warns`; line 378 maps the broad old AC and the narrow new case to one suite. No qualifier in AC007 confines its domain to the delivered ledger or withdrawn scan.
- PASS change: amend AC007 and AC001's first sentence to say a delivered ledger entry still in progress blocks, a withdrawn entry still in progress blocks under its separate code, and a queued undelivered server turn warns. Rename or refine the broad mapped test in TEST-006/A7 to assert all three branches, then sync the TODO and handoff if their descriptions change.
- Classification: implementer edit to plan and updated requirement; no new operator decision. D5 already chose the behavior.

### F-R37-02 — Node core lacks the withdrawn unsupported-session transition

- Plan location: common unsupported-session rule line 81; FR-MCP-SESSIONTURN-005-AC001 line 226; TEST-MCP-SESSIONTURN-008 line 380; A8 line 592.
- Defect: the common rule covers a `withdrawn` queue entry still `in_progress` when `unsupportedSince` is recorded, and the plan maps AC001 to the Node core. A7/TEST-006 has the combined transition with restart and other-session negatives. A8/TEST-008 names only `NodeStopEquivalent_UnsupportedSession_CloseCapturedToFailsafe_ThenWarnsOnly`, without a withdrawn-before-delivery original-pair case or an explicit P0.2 reason that no supported Node host can reach that state.
- Evidence: a plan-wide search for Node plus withdrawn finds the shared AC but no Node case. The named Node test proves the earlier delivered-open branch, not the newly added withdrawn branch.
- PASS change: add an A8 jest case and TEST-008 mapping for withdrawn before delivery → later unsupported → evidence and `failTurn` captured by original pair → Stop-equivalent warning → restart/drain close, with another-host-session isolation; or state and substantiate a P0.2 exclusion for every supported Node host.
- Classification: implementer edit; no new operator decision.

### F-R37-03 — no-listed-claims receipt is untested in both plugin parsers

- Plan location: FR-MCP-SESSIONREVIEW-005-AC002/AC003 lines 318–319; TEST-MCP-SESSIONREVIEW-005 line 390, TEST-MCP-SESSIONREVIEW-008 line 393; S8 line 612 and S9 line 614.
- Defect: a DISAGREE receipt with empty FAIL, UNKNOWN, and reasons lists is expressly valid and must yield nonempty `otherNotes` containing the verdict-level reason and response receipt path. S4 tests server mapping with `HvVerdictMapping_DisagreeNoListedClaims_PassScores_OtherVerdictLevelReason`. S8 and S9 test UNKNOWN-only empty-reasons receipts, but neither plugin parser is tested with no listed claims at all. A parser could reject or omit the required fallback before the server mapping is reached.
- Evidence: `NoListedClaims` appears only in AC002, TEST-007, and S4; it does not appear in TEST-005/008 or the S8/S9 cases.
- PASS change: add PowerShell and Node receipt-input cases using empty lists and no reasons, assert the generated `hvVerdict`, empty `reasons`, response path, and stored nonempty `otherNotes`; map them in TEST-005/008 and keep them in S8/S9, after S4 exists.
- Classification: implementer edit; no new operator decision.

### F-R37-04 — rejected drain clears the sole named carrier of audit evidence

- Plan location: C8 line 128; FR-MCP-SESSIONCLASS-002-AC005 line 274; FR-MCP-SESSIONCLASS-004-AC003 line 286; S10 line 616.
- Defect: C8 promises that audit evidence in a rejected completion is preserved. The server rejects an entire submission atomically with no turn persisted. AC003 then clears a drained `unsupported_session` close's failsafe record and permits its carried actions, decisions, requirements, and blockers merely to be “reported unpersisted by name.” It specifies no durable remaining record, replay owner, or crash/restart recovery after that clear. A crash before a later accepted write can permanently lose the carried evidence while satisfying the listed close/review test.
- Evidence: AC005 says no turn from the rejected submission is persisted; AC003 says the failsafe record is cleared; S10 tests rejected drain → reviews via pair → later close, but does not require evidence readback or a restart between rejection and correction.
- PASS change: choose and specify one preservation mechanism in the plan, such as retaining the rejected payload under its original pair until an accepted evidence write is verified, or persisting the carried evidence in a separate accepted write before attempting close. Define idempotence, the visible `unpersisted` result, the clear condition, and a S10 PowerShell/Node restart test that reads back every carried field after correction. Reconcile C8, AC003, and the drain tests, then sync the TODO.
- Classification: implementer edit to make the existing C8/operator option-1 contract executable; no new operator decision is needed if the edit preserves that contract. If the author instead intends loss after a reported refusal, that would require an explicit operator decision between durable preservation and report-only loss.

## Unknown and disposition

- **U-R37-01 / P6:** actual G1–G6 and Node 18/20/22/latest command outcomes were not executed in a review-only run. Static target and path checks passed. Execute and retain gate receipts during implementation; this does not waive the four plan edits above.
- Implementer fixes: F-R37-01 through F-R37-04.
- Operator-decision fails: none under the existing D5, D6, option-1, and C8 decisions. The loss-permitting alternative in F-R37-04 would be a new operator decision and is not presumed authorized.
- Recurrence signal: F-R37-01 is another incomplete propagation of a repaired Stop rule; F-R37-02 and F-R37-03 are the missing sibling tests of round-36 repairs. The round 1–35 receipt set and round 36 were re-attacked; no additional independently supported regression was found.

The plan remains **NOT APPROVED**. Approval threshold requires AGREE and both scores at least 98; these findings prevent it.
