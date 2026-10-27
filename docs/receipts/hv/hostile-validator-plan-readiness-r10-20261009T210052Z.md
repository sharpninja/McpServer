# Round 10 hostile validation: PLAN-SESSIONTURNREVIEW-001

Timestamp UTC: 2026-10-09T21:12:11Z
Verdict: DISAGREE; accuracy 93/100; completeness 86/100; confidence 0.96
Claim totals: 2 PASS, 8 FAIL, 0 UNKNOWN
Reviewer: Codex; gpt-6-sol; effort xhigh
Live proof: /home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T16-00-52-01a12278-00f3-7761-be36-fcd99e99b839.jsonl turn_context: model=gpt-6-sol; collaboration_mode.settings.reasoning_effort=xhigh; cwd is the reviewed worktree
Reviewer session/request: Codex-20261009T210305Z-plugin-session / req-20261009T210325Z-plan-readiness-r10

## Mandatory context

- Add-profile skill read; 19 non-skill profile Markdown files read in full.
- Main workspace marker read in full, with apiKey line and Authentication block filtered from output. No key is copied here.
- 14 effective MCP memories read in full, including MEMORY-PROCESS-006 and -007.
- Source plan: 645 lines, 525 non-empty; SHA-256 074A8ED6F2F748C18C37F2EC08FD2A979C5893B830B4C45A89F2793D0D7A0859.
- TODO: NOT APPROVED; done=false; description parity 525 of 525 exact ordinal matches; 30 implementationTasks.
- HEAD: 199ecf2d1557253b4a0f0956c2dfbb277cf4d32c. Handoff and all nine plugin checkouts were inspected. Live FR/TR/TEST totals were 352/480/512; proposed ids showed no collision.
- Runner JSONL: docs/receipts/hv/20261009T210052Z-plan-readiness-r10.request.jsonl and docs/receipts/hv/20261009T210052Z-plan-readiness-r10.response.jsonl.

## P1-P10

### P1 FAIL

Most code anchors and plugin heads match HEAD, but plan line 115 says all C1-C12 contracts are agreed and line 119 treats the callback as settled. MEMORY-PROCESS-007 has a later 21:01:48Z C2 counter requiring durable alias enumeration/reconciliation; no ACK to that counter was found.

### P2 FAIL

21 new FR, 23 new TR and 23 new TEST ids have no live-store collisions; existing ids checked. However mapping rows 398 and 403 link FR-MCP-SESSIONTURN-002 and -007 to TEST-MCP-SESSIONTURN-008, whose validation sentence at 376 names neither FR nor AC; the edge lacks a TEST statement.

### P3 FAIL

D1-D12, partial clearance, licensing, and MEMORY-PROCESS-006 largely appear in the plan. C5 at line 122 and D2 at line 34 require an original trusted MCP sessionId/requestId pair for earlier-session backfill, but S8/S9 lines 605/607 specify only D2 binding or requestId override. The handoff line 45 also states only major-phase HV despite D10's each-slice gate.

### P4 FAIL

Server, client, REPL, PowerShell/Node cores, nine plugin repos including all eight named hosts, and SQLite/PostgreSQL/SQL Server migrations are scheduled. Node outbox alias crash recovery and trusted earlier-session review backfill are not executable tasks/tests.

### P5 FAIL

The public skipped_duplicate meaning remains an explicit unanswered operator decision. C2 recovery remains under a 21:01:48Z CONTRACT counter without ACK. S8/S9 omit the trusted-pair review-backfill design and negative cases.

### P6 PASS

Named build targets/test projects/receipt paths and G1-G6 were checked in source. The plan specifies per-slice BDPv4 order: contracts/stubs, RED, mocks-green with negative check, self-review, real green, refactor; P0.1 explicitly provisions missing local SDK/Pester/bats/ef preconditions. This is a static executability check; gates were not run in this review-only round.

### P7 FAIL

TEST-MCP-SESSIONTURN-008 at 376 omits mapped FR-002/007; A8 RED at 585 names C3 generically and skipped_terminal but lacks positive receipt identity/hash/revision mismatch, skipped_duplicate read-back, and outcomeVerified:false cases for every Node path. C2 crash-after-journal/before-callback and replay-to-QB tests are absent.

### P8 FAIL

The 30-task phase order is broadly coherent and the explicit QB dependency graph is acyclic, but line 585 promises onAliasCommitted fires exactly once while C2's crash replay requires recoverable delivery; S8/S9's requestId-only wording conflicts with C5. The handoff HV cadence conflicts with source D10.

### P9 FAIL

TODO description is exact 525/525 and task count is 30. Handoff lines 13/17 correctly date the round-10 counts, but line 45 omits every-slice HV, line 91 says all F-R9-02..09 addressed despite a pending C2 counter, TODO technicalDetails calls the 20:59Z round-10 sync round-9, and the P0.2 task omits the new opencode arrival probe.

### P10 PASS

Risks and incidental-bug claims compared with source and live triage.getReport: all 10 cited report ids exist: triage-report-8509a6345e6c408a981ade489513fa2b, triage-report-d3bcb648a72642f0a52c68c801316a0f, triage-report-1a283182658245f3a28a7bf36a4d0fe2, triage-report-629497bab74c4676823aebbccfb58cc0, triage-report-a03b85ac70594a298fa9d56eca566492, triage-report-41d7d27c53f2464f8339fe963079a55e, triage-report-953f44d7574942419e6d5d3123acb971, triage-report-d8c40d80a4ba454e8b5b96b9b5762105, triage-report-ab06ac3befed4ff99725ef6047715158, triage-report-04659db176a8473bbf2aa9b6caf1f98a. The separate group triage-group-804b21086f3019ab is found in the worktree with one report and failed group status, not found in main workspace, exactly as disclosed; P0.1 schedules its read-only recheck. Existing defects are assigned to A2/A9 or triage.

## FAIL findings

### F-R10-01 (P5 P7)

- Plan location: Turn outcomes and contract, plan lines 120, 216, 542; A1 line 571
- Defect: Public meaning of skipped_duplicate is unresolved, so one outcome and its acceptance tests cannot be final.
- Evidence: MEMORY-PROCESS-007 REQUEST 2026-10-09T20:35:25Z presents A per-input duplicate, B identical stored resubmit, C remove outcome; no operator answer found. Round 9 F-R9-01 remains open.
- Make PASS: Operator selects A, B, or C; then update outcome DTO, wire contract, A1 RED, C3 mapping, and all affected acceptance criteria consistently.
- Classification: operator decision. Public meaning of skipped_duplicate: A per-input duplicate; B identical stored resubmit; C remove outcome

### F-R10-02 (P1 P4 P5 P7 P8)

- Plan location: Coordination/C2 plan lines 115 and 119; TR-MCP-SESSIONTURN-008 line 349; A8 line 585
- Defect: The A8 alias seam is callback-only and claims exactly once, leaving QB outbox aliases unrecoverable if the process crashes after journal commit but before callback delivery or QB apply.
- Evidence: MEMORY-PROCESS-007 2026-10-09T21:01:48Z Codex ACK/COUNTER explicitly asks for durable committed-alias enumeration/lookup, callback as wakeup, idempotent QB reconciliation before flush, and crash/restart/conflict/concurrency tests. The plan names only onAliasCommitted_firesOncePerAlias and local alias replay; its 'all contracts agreed' statement is stale.
- Make PASS: Amend C2, TR AC-007, TEST-008, A8 and QB coordination: expose durable read-only alias enumeration/lookup; reconcile unapplied aliases idempotently on startup and before outbox flush; define acknowledgment/ordering and conflict behavior; add RED cases at each journal/callback/QB apply crash boundary and duplicate/restart/concurrent aliases; obtain the other agent's CONTRACT ACK before dependent integration.
- Classification: implementer plan edit plus coordination ACK. No new operator decision unless the agents cannot agree on the amended contract

### F-R10-03 (P3 P4 P5 P7 P8)

- Plan location: D2/C5 plan lines 34 and 122; wire line 566; S8 line 605; S9 line 607
- Defect: Review backfill for an earlier host session is described in S8/S9 as a requestId override without carrying the trusted original MCP sessionId/requestId pair C5 requires.
- Evidence: C5 explicitly includes D10 slice-verdict backfill. FR-MCP-SESSIONTURN-007-AC007 line 246 refuses requestId alone on no-key paths. The S8/S9 recordReview contracts and RED lists contain no trusted-pair acceptance or cross-conversation refusal tests.
- Make PASS: Specify sessionId and requestId together on both PowerShell and Node recordReview/backfill surfaces, original-session verification against workspace and agent, immutable target capture, and RED cases for valid earlier-session pair, requestId alone, wrong pair, and concurrent sessions; align wire/UC/HV steps.
- Classification: implementer plan edit. None; applies D2 and C5

### F-R10-04 (P2 P7)

- Plan location: TEST-MCP-SESSIONTURN-008 plan line 376; mappings lines 398 and 403; A8 allocation line 506
- Defect: Two FR-to-TEST mapping edges are not asserted by the TEST statement.
- Evidence: Mappings link FR-MCP-SESSIONTURN-002 and FR-MCP-SESSIONTURN-007 to TEST-MCP-SESSIONTURN-008. Its validation sentence names FR-MCP-SESSIONTURN-008 and generic 'Node-core extensions allocated to A8', but not FR-002/007 or their ACs. Allocation shorthand is not a TEST statement.
- Make PASS: Enumerate the specific FR-MCP-SESSIONTURN-002 and -007 ACs tested by TEST-008, with named Node RED cases, or remove the unsupported edges and map them to TEST records that explicitly validate them.
- Classification: implementer plan edit. None

### F-R10-05 (P4 P7)

- Plan location: C3 plan line 120; TR-MCP-SESSIONTURN-008-AC006 line 349; TEST-MCP-SESSIONTURN-008 line 376; A8 RED line 585
- Defect: C3's positive and negative persistence receipt matrix is not allocated to objective Node RED cases.
- Evidence: A8 RED says 'C3 outcome mapping through every Node persistence path' and explicitly tests only skipped_terminal. It omits mismatched eventId/payloadHash/revision for created/merged, matching persisted read-back for skipped_duplicate, and outcomeVerified:false on a legacy server. These are independent observable branches in C3.
- Make PASS: Name Node-core and QB outbox integration RED cases for each C3 branch on each applicable path, including receipt mismatch, read-back mismatch, legacy outcomeVerified:false, terminal no-retry, and idempotent replay; make TEST-008 and G6 coverage explicit.
- Classification: implementer plan edit. The skipped_duplicate branch awaits F-R10-01; all other cases are implementer edits

### F-R10-06 (P3 P8 P9)

- Plan location: Plan status/handoff pointer line 3, D10 protocol lines 153-159, P0.2 line 170; handoff lines 45 and 91; TODO technicalDetails and P0.2 implementationTask
- Defect: The handoff and TODO metadata do not faithfully carry the current plan's gate cadence and probe list.
- Evidence: Handoff line 45 says HV at major phases only while D10 requires HV after every green slice; line 91 says all F-R9-02..09 addressed while C2 counter remains pending. TODO technicalDetails labels the 20:59Z round-10 sync 'round-9 sync'; its P0.2 task omits opencode's enqueue-versus-processing probe at plan line 170(i). Source/TODO description parity and handoff counts are otherwise correct.
- Make PASS: Update handoff HV cadence and C2 status; correct TODO sync label and P0.2 task to include opencode and the arrival probe; preserve 525-line description parity and 30-task order.
- Classification: implementer plan/handoff/TODO edit. None

## Reattack and limits

F-R9-01 remains open. F-R9-02..08 have matching plan edits: A9 tarball/host/wrapper fixes; P0.2 probes; C4 field set; C5 no-key pair; UC-1 D9; arrival tombstone; handoff counts. F-R9-09 has local journal ownership/tests but is reopened by the 21:01:48Z C2 recovery counter. R1-R8 receipts checked for regressions; no additional regression established beyond the mapping/test and handoff issues reported here.

Review-only; no build, migrations, plugin install, network repair, or live host smoke was executed. Toolchain preconditions are stated in P0.1. Source and store were read-only.

Persistence proof: workflow.sessionlog.appendActions, appendDialog, updateTurn and completeTurn each returned code=persisted for reviewer requestId req-20261009T210325Z-plan-readiness-r10. Read-only client.SessionLog.QueryAsync returned the same sessionId/requestId; the own turn block was 18,490 characters, status completed, and contained the complete verdict, findings, receipt actions, and prior persistence proof. stop-gate.ps1 exited 0.

Receipt JSON contains the same structured verdict, claim evidence, findings, classification, and reviewer identifiers.



