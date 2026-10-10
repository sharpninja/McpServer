# Hostile validation: PLAN-SESSIONTURNREVIEW-001, round 25

TimestampUtc: 2026-10-10T02:10:01Z  
OverallVerdict: DISAGREE  
Accuracy: 96/100  
Completeness: 95/100  
Confidence: 98/100  
ReviewerSessionId: Codex-20261010T020253Z-plugin-session  
ReviewerRequestId: req-20261010T020303Z-hv-plan-r25  
HEAD: 05b6bf4451a33a288942cf6ea4b81492e9f166cf

## Identity and first-action proof

The live Codex rollout at `/home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T20-59-21-01a12389-45b0-7b40-9a02-039f681e4198.jsonl` has session id `01a12389-45b0-7b40-9a02-039f681e4198`. Its first turn context at 2026-10-10T01:59:23.038Z records model `gpt-6-sol` and reasoning effort `xhigh`; the runner response JSONL has the same thread id. This is live session metadata, not the implementer's claim.

I read `/home/sharpninja/.claude/skills/add-profile/SKILL.md` and all 19 non-skill Markdown files under `/home/sharpninja/.claude/profile/` in full. I read the 397-line main-workspace marker in full with the apiKey line and Authentication block filtered; the plugin marker resolver reported a valid signature and bootstrap. I called `workflow.memory.list` through the Codex plugin wrapper for the main workspace and read all 15 effective MCP memories (13 Global, 2 Workspace), including the full 91,423-character MEMORY-PROCESS-007 and MEMORY-PROCESS-008. No secret value is in this receipt.

## Audit coverage

I read all 650 plan lines (530 nonempty) end to end, the live main-workspace TODO's description, 30 implementation tasks, note and technicalDetails, the handoff, the r24 receipt, and r1-r23 findings. I compared the plan's source anchors with this worktree at HEAD and the unchanged source baseline, the nine named plugin repositories, docs/Project, docs/context, build targets, and the read-only MCP requirements and triage stores. The main-workspace TODO description matches all 530 nonempty source lines exactly and in order; the 30 tasks match the plan's task sequence. The handoff correctly says NOT APPROVED, 650/530, and round 25 pending. The Codex wrapper used `-ParamsObject` with its cache and failsafe directories under `/tmp`.

The live requirements store returned 354 FR, 482 TR, and 514 TEST records. All 52 numeric existing IDs extracted from the plan resolve; the historical invalid `FR-SUPPORT-010G` is explicitly scheduled for correction. All 67 proposed IDs are absent. Existing and proposed mappings match the requirements catalog and the docs/Project projection where checked. The nine plugin heads, versions, hook registration and package anchors agree with the plan's inventory. Named build targets and project paths exist. On this host, `dotnet --list-sdks` returns only 10.0.111, while `global.json` requires 10.0.201; Pester 5, bats, and dotnet-ef are absent. The plan's P0.1 toolchain gate accurately identifies those prerequisites, so I did not mislabel an unrun build as green.

## Claim results

- **P1 Source accuracy — PASS.** The worktree has no source, test, build, or plugin changes since the cited baseline; the checked anchors, routes, targets, migrations, plugin heads, and current toolchain facts agree with source. No newly false source assertion found.
- **P2 Requirements accuracy — PASS.** Live store counts and ID collision check above; the 21 FR, 23 TR, 23 TEST proposed records and updated-record mappings are accounted for. `FR-SUPPORT-010G` is identified as historical invalid text, not a proposed ID.
- **P3 Locked operator decisions — FAIL.** The 2026-10-10 choice to capture writes for an already open turn in an unsupported host session is stated in the locked decisions and FR-005, but review plugin tasks forbid that capture and other D6 fail-closed transitions omit it. See F-R25-01 and F-R25-02. The r24 duplicate-delivery qualification is correctly propagated to FR-001 AC002/003, TEST-PLUGIN-013 AC001, and UC-1.
- **P4 Completeness of scope — PASS.** Server, client, REPL, MCP tools, PowerShell and Node cores, all eight requested agent plugins plus grok-bot, and SQLite/PostgreSQL/SQL Server migrations have allocated tasks and test suites. The two missing cross-path behaviors are scored under P3/P5/P7/P8.
- **P5 Decision completeness — FAIL.** The implementer must decide how a captured review is replayed without overwriting a newer review, and how all unsupported transitions preserve the accepted Stop remedy. The plan currently gives incompatible or absent instructions. See both findings.
- **P6 Executability — FAIL.** G1-G6 targets and receipt paths exist, and the BDPv4 order is specified per slice. Yet A7's literal-remedy gate cannot pass for an already open turn when an unknown bridge record makes every verb unsupported without the failsafe exception. P0.1 correctly gates the missing local toolchain; I did not run product builds.
- **P7 Testability — FAIL.** No S8/S9 RED case covers the mandatory captured review and ordered replay, and A3/A7 tests cover bridge ambiguity but not an unknown-format record after a delivered open turn. An AC and its named tests are needed for both scenarios. HV-P0, HV-A and HV-B consistently specify Codex gpt-6-sol xhigh and 98/98.
- **P8 Internal consistency — FAIL.** FR-005's capture of reviews conflicts with TR-REVIEW-005 and S8/S9's `no failsafe`; the exhaustive two-cause `unsupportedSince` rule conflicts with the bridge's broader unsupported behavior and Stop's accepted-remedy guarantee. Phase and 30-task ordering otherwise agree.
- **P9 Artifact parity — PASS.** The 530-line TODO description comparison is exact; all 30 task names and sequence agree, and the technicalDetails and handoff accurately report status, counts, artifact locations, prior rounds, and filed identifiers. Their round-25 summary repeats the plan's intended failsafe but does not resolve F-R25-01/02.
- **P10 Risks — PASS.** The risks section is either scheduled or filed. All nine named triage reports resolve in the main store. `triage-group-804b21086f3019ab` resolves as failed, one report, under the worktree marker and is not found under the main marker, exactly the handoff's pending relocation. I used the worktree marker to verify its actual workspace rather than passing an ignored `workspacePath` parameter to `getGroup`.

## Failures and required changes

### F-R25-01 — Review writes are both mandatory failsafe records and forbidden failsafe records

**Plan locations:** locked unsupported-session decision line 29; FR-MCP-SESSIONTURN-005-AC001 line 224; FR-MCP-SESSIONREVIEW-001-AC003 line 289; TR-MCP-SESSIONREVIEW-005 AC-002 line 361; S8 line 610; S9 line 612; TEST-MCP-SESSIONREVIEW-005/008 lines 388/391.

**Exact defect:** FR-005 explicitly includes `reviews` among every write captured as an `unsupported_session` failsafe record for an already delivered open turn. The PowerShell TR and S8 say `Invoke-WorkflowRecordReview` has **no failsafe**, and S9 says Node `recordReview` has **no failsafe replay**. S8 gives the reason: an older queued review must not overwrite a newer one. An unsupported session with an open Code turn that needs its agent review cannot both capture that review and refuse its capture. A subsequent replay also needs an exact stale-write ordering rule to preserve the stated latest-write-wins intent. The TODO description contains both sides verbatim; its 30-task list has no repair step.

**Make PASS:** Revise TR-REVIEW-005, TR-REVIEW-008, S8, S9, and their TEST/RED allocations to allow the narrowly scoped `unsupported_session` capture mandated by FR-005. Specify a durable causal order or conditional revision rule that rejects an older captured review after a newer review has committed, including restart/replay and original-pair recovery. Add PowerShell and Node cases for captured agent/hostile review, later server review, stale replay, and completion-gate results. Propagate the exact rule into the TODO description, technicalDetails, and handoff. **Classification: implementer plan edit.** Existing latest-write-wins language and S8's explicit older-review protection establish the desired precedence; if the author instead proposes that drain-time arrival may overwrite a newer review, that change needs an operator decision between original-write order and drain-arrival order.

### F-R25-02 — Other fail-closed causes recreate the round-24 Stop deadlock

**Plan locations:** Claude bridge row line 84; FR-MCP-SESSIONTURN-001-AC009 line 202; FR-MCP-SESSIONTURN-005-AC001/AC002/AC005 lines 224-228; FR-MCP-SESSIONTURN-008-AC004 line 255; Stop wire line 549; A3 line 580; A7 line 588; Copilot D11 line 37 and host row line 87.

**Exact defect:** The new failsafe applies only when `unsupportedSince` has cause `queue_removal_ambiguous` or `indistinguishable_arrival`. The same bridge row says an unknown operation or missing required field stops the bridge and makes **every** session-log verb unsupported, but it does not set an `unsupportedSince` record covered by FR-005. AC009 also makes a shortened transcript unsupported. Start with delivered turn R1 open, then encounter an unknown queue record: Stop blocks on R1 and instructs the model to provide evidence and close it; every instructed verb is refused, no close is captured, and next SessionStart has nothing to drain. The warning `arrival_bridge_format_unrecognized` does not clear that block. The same unaddressed transition is possible if a duplicate-hook check begins failing after an earlier turn opened. A3 tests unknown shape only as unsupported, and A7 tests the two named causes only. This is the recurrence of F-R24-01 under a sibling cause.

**Make PASS:** Give every D6 transition after a delivered turn an explicit, atomic `unsupportedSince` cause and the same bounded existing-turn failsafe path, including unknown/missing bridge fields, truncated transcript, and duplicate-check failure after an earlier open. Keep mints and writes for other turns refused. Update the bridge row, FR-001 AC009/010, FR-005, TR-006, FR-008, Stop wire, A3/A7/A8/A9, TEST-001/006/008/009, and the feature matrix. Name RED cases beginning with R1 open, each cause, accepted evidence/review/close remedies, Stop warning, restart, ordered drain, and refusal of other-turn writes. Propagate to TODO and handoff. **Classification: implementer plan edit.** The operator's option 1 already chooses capture for an open turn in a D6 fail-closed host session; no new policy choice is required.

## Previous-round re-attack and recurrence

F-R24-02's stable-ID/bridge qualification and Copilot/classic-Cline equal-field exception are present in the named normative ACs and tests. F-R24-01 is repaired for queue ambiguity and equal-field causes, but F-R25-02 shows the same accepted-remedy failure for unknown bridge format and other unsupported transitions. The r1-r23 receipts were scanned for prior failure classes and the plan was rechecked for the repaired anchors: bridge-only minting and byte-offset nonce, system queue positions, atomic delivery/ledger replace, permanent C2 alias lookup with final acknowledgements, six-attempt C9 collision bound, pure Copilot/Cline nonce without `arrivalSeq`, classic-Cline RED cases, phase order, and exact review validation. No separate regression in those repaired paths was found. F-R25-01 is a propagation miss from the new r25 decision across Phase B; F-R25-02 is a recurrence of the r24 deadlock across a sibling fail-closed cause, consistent with MEMORY-PROCESS-008's what-if and propagation checklist.

## Verdict

**DISAGREE.** Five claims PASS, five FAIL, none UNKNOWN. Accuracy 96/100, completeness 95/100, confidence 98/100. Both findings are implementer plan edits under already stated operator policy. No product files, plan, TODO, requirements, triage, memories, settings, or other turns were changed. The matching JSON receipt contains the machine-readable result and persistence proof after MCP turn readback.

=== VERDICT JSON ===

{"overallVerdict":"DISAGREE","accuracy":96,"completeness":95,"confidence":98,"passCount":5,"failCount":5,"unknownCount":0,"failList":["F-R25-01","F-R25-02"],"unknownList":[],"implementerFixes":["F-R25-01","F-R25-02"],"operatorDecisionFails":[],"headSha":"05b6bf4451a33a288942cf6ea4b81492e9f166cf","receiptPaths":["docs/receipts/hv/hostile-validator-plan-readiness-r25-20261010T015920Z.md","docs/receipts/hv/hostile-validator-plan-readiness-r25-20261010T015920Z.json","docs/receipts/hv/20261010T015920Z-plan-readiness-r25.request.jsonl","docs/receipts/hv/20261010T015920Z-plan-readiness-r25.response.jsonl"],"reviewerSessionId":"Codex-20261010T020253Z-plugin-session","reviewerRequestId":"req-20261010T020303Z-hv-plan-r25"}
