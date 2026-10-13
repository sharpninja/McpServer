# Hostile validator plan readiness: round 29

TimestampUtc: 2026-10-10T03:42:46.2553689Z. HEAD: c0bc118e2d609a9877990b22b7e37794bc7d58c5.
**DISAGREE. Accuracy 97/100; completeness 95/100; confidence 98/100. Five PASS, five FAIL, zero UNKNOWN.** Approval threshold 98/98 and all applicable claims PASS is not met.

## Identity and mandatory reads

- Live Codex proof: Own rollout /home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T22-26-05-01a123d8-ae9a-71c0-9114-e445578a0b31.jsonl: session_meta id 01a123d8-ae9a-71c0-9114-e445578a0b31 timestamp 2026-10-10T03:26:05Z cli_version 0.160.1; turn_context model gpt-6-sol effort xhigh.
- add-profile skill and all 19 non-skill profile Markdown files read in full. Main marker read in full (397 lines) with secret fields and Authentication block filtered from output. workflow.memory.list returned 15 effective memories with text; MEMORY-PROCESS-007 and -008 were individually examined. MEMORY-PROCESS-008 drove what-if, propagation, recurrence, and phase-boundary checks.
- Reviewer session $sessionId; request $requestId.

## Claims

- **P1 PASS:** Entire 651-line plan reviewed against HEAD c0bc118e2d609a9877990b22b7e37794bc7d58c5. Current-source anchors and behaviors sampled in server/client/REPL/build and all nine plugin repos match; source diff from f56dcf70 is docs/receipts/handoff only. No false present-tense source claim identified. The future deploy command defect is scored in P6.
- **P2 PASS:** Live main-workspace store: FR 354, TR 482, TEST 514. All 21 proposed FR, 23 TR, 23 TEST IDs are free; cited existing IDs resolve except the explicitly historical invalid rename and family shorthand. Independent forward/reverse lint found zero omitted edges among the new 21 FR mapping rows and 23 TEST rows. Phase allocation defects are scored in P7/P8.
- **P3 PASS:** D1-D12, partial clearance, licensing, skipped_duplicate removal, unsupported-session failsafe option 1, 98/98 and traceability rule are reflected in contracts, phases, and gates. C1-C12 ACKs and the C2 notices agree with MEMORY-PROCESS-007. No new contradiction to a locked operator choice found.
- **P4 PASS:** A1-A9 and S1-S11 name concrete server, client, REPL, MCP, PowerShell/Node-core and all nine plugin tasks (the eight named hosts plus grok-bot); G6 covers host suites. S3 covers SQLite, PostgreSQL and SQL Server migrations, backfill, Up/Down, and integration tests; rollout and compatibility paths are specified.
- **P5 FAIL:** F-R29-01: line 370 span/satisfaction rule cannot be applied to TEST-SESSIONREVIEW-008 and TEST-SESSIONCLASS-001 as written. The implementer must choose different TEST ownership or change TR slices; the plan does not choose.
- **P6 FAIL:** F-R29-01: S1/S8 cases are assigned to an S9-only TEST, so its slice gate cannot satisfy its listed ACs under line 370. F-R29-02: rollout/HV live checks prescribe bare ./build.ps1 UpdateService on Linux, while build/Build.UpdateService.cs:40,223-227 requires root; current uid is 1000. Other named targets, projects and G1-G6 definitions were found; P0.1 handles missing toolchain prerequisites; BDPv4 order is explicit.
- **P7 FAIL:** F-R29-01: TEST-SESSIONREVIEW-008 at line 392 lists S1 C# and S8 Pester cases although its TR is [S9], and TEST-SESSIONCLASS-001 at line 381 validates FR-CLASS-001-AC006 [S5+S6+S7] despite its TR ending S4 and no task line naming its later cases. The three TEST ACs would be marked satisfied too early. HV-P0/HV-A/HV-B Codex gpt-6-sol xhigh and 98/98 are otherwise consistent.
- **P8 FAIL:** F-R29-01: TR brackets (lines 354,365), TEST rows (381,392), FR AC phase tags (266,339), S1/S5/S6/S8/S9 task lines (597,605,607,611,613), and the span rule (370) disagree. The P0.8 dependency chain itself is ordered, but the stated TEST ownership contradicts it.
- **P9 FAIL:** TODO description exactly matches all 531 nonempty plan lines; 30 implementationTasks and NOT APPROVED status match; handoff counts and paths match. But TODO technicalDetails claims TEST rows name only owned cases, and handoff line 167 repeats that false claim (F-R29-01). Thus parity of substantive status/evidence fails.
- **P10 PASS:** Risks at plan lines 625-629 compared with source and live triage. Nine named main-workspace reports exist and are grouped; triage-group-804b21086f3019ab is absent from main but present in the worktree with the described failed/grouped state and relocation scheduled. No unfiled incidental issue was identified.

## Findings and exact fixes

### F-R29-01 — Implementer edit

- Plan locations: Span/satisfaction rule: plan line 370; FR-MCP-SESSIONCLASS-001-AC006: line 266; FR-MCP-SESSIONREVIEW-008-AC004: line 339; TR brackets: lines 354 and 365; TEST rows: lines 381 and 392; Slice tasks: lines 597,605,607,611,613; TODO technicalDetails round-29 summary; Handoff line 167.
- Defect: The new ownership rule assigns a TEST to its TR bracket slices plus explicitly named later slices. TEST-MCP-SESSIONREVIEW-008 names SessionTurnReviewGoldenContractTests in S1 and a Pester golden case in S8, but TR-MCP-SESSIONREVIEW-008 is [S9] only: neither earlier case belongs to that TEST. TEST-MCP-SESSIONCLASS-001 lists REST/MCP/client cases in S5 and REPL cases in S6 and claims FR-MCP-SESSIONCLASS-001-AC006 [S5+S6+S7], while its TR bracket is [S1+S3+S4]; S5/S6 task lines do not name those TEST row cases or suites as the later-slice rule requires. Its three TEST ACs can therefore be marked satisfied at S4 before the surfaces exist. TODO technicalDetails and handoff falsely state that every row now names only owned cases.
- Evidence: Direct comparison of plan lines 266,339,354,365,370,381,392,597,605,607,611,613 and live TODO technicalDetails/handoff line 167. The S1 C# and S8 Pester cases are expressly created/run before S9. This is a recurrence of the phase-boundary ownership class found in r28 F-R28-03, despite the new lint claim.
- Change to PASS: Make TR-MCP-SESSIONREVIEW-008 explicitly span S1+S8+S9 and specify its cross-runtime golden-contract AC/cases, or move S1 and S8 cases into S1/S8 TR/TEST records and add the FR mapping edges. Remove S5/S6 surface cases and AC006 from TEST-MCP-SESSIONCLASS-001; name and allocate REST/MCP/client to an S5 TEST and REPL to an S6 TEST, updating FR-CLASS-001 mappings and each TEST statement. Re-run ownership and mapping lint; update plan, TODO description/technicalDetails, and handoff together. Set each TEST AC satisfied only after its last actual owned slice.
- Operator decision: none. The locked decisions already settle policy; these are implementer corrections to the plan and its derived artifacts.

### F-R29-02 — Implementer edit

- Plan locations: Rollout: plan line 622; Phase A live check: plan line 635; Phase B live check: plan line 636.
- Defect: The plan prescribes ./build.ps1 UpdateService on this Linux host with approval but omits the required root elevation. The active uid is 1000, so the named command throws before deploying.
- Evidence: build/Build.UpdateService.cs:40 documents sudo --chdir . pwsh -NoProfile -ExecutionPolicy Bypass -File ./build.ps1 UpdateService; lines 223-227 explicitly throw unless id -u returns 0. Get-Command pwsh.exe resolves /home/sharpninja/.local/bin/pwsh.exe on this host; id -u returned 1000. The deploy was not executed, as this is a read-only review.
- Change to PASS: In rollout and both HV live checks, require the already stated operator approval and an explicit elevated PowerShell invocation using the resolved absolute pwsh.exe path (for example sudo --chdir . /home/sharpninja/.local/bin/pwsh.exe -NoProfile -ExecutionPolicy Bypass -File ./build.ps1 UpdateService), verify uid 0 and the Nuke outcome, and record that receipt. Keep the command adaptable to the execution host without silently using bare ./build.ps1.
- Operator decision: none. The locked decisions already settle policy; these are implementer corrections to the plan and its derived artifacts.

## Holistic and prior-round audit

- Read all 651 lines and every section of the plan, 30 TODO tasks plus technicalDetails, handoff, live requirement records, source, and nine plugin repos. Independently compared TODO description to all 531 non-empty plan lines with zero difference. Verified 21/23/23 proposed IDs are unoccupied and zero new mapping omissions.
- Re-attacked r28 F-R28-01..06: three-AC extraction, mapping edges, synthetic versus real duplicate-hook cases, outbox port-double versus QB integration, and S10 marker activation are repaired. F-R28-03 recurs as F-R29-01 because the new span rule was not applied to all TEST rows. Surveyed available r2-r27 receipts and r1 handoff history for regressions; earlier unsupported-session and C2 alias issues remain resolved in the written contracts.
- The execution toolchain is gated by P0.1 and not installed for a product test run in this read-only review. No deploy, product edit, TODO mutation, requirement mutation, triage mutation, or plugin edit was performed. The Linux deploy failure follows directly from the root guard in source and current uid 1000.

## Persistence proof

Own reviewer turn opened in the worktree MCP store. Typed client.SessionLog.QueryAsync readback before completion found this turn in_progress with the full 9,973-character result, both findings, the receipt path, three actions, and two dialog entries. Typed client.SessionLog.QueryAsync readback at 2026-10-10T03:44:59.1996910Z then found session Codex-20261010T033026Z-plugin-session, request req-20261010T033026Z-turn-a0bd, status completed, full 10,100-character response with F-R29-01, F-R29-02 and the receipt path, three actions, and two dialog entries. The request and response JSONL are runner-owned paths. The JSON receipt includes the same findings and claim statuses.

## Receipts

- /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-plan-readiness-r29-20261010T032605Z.md
- /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-plan-readiness-r29-20261010T032605Z.json
- /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261010T032605Z-plan-readiness-r29.request.jsonl
- /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261010T032605Z-plan-readiness-r29.response.jsonl
