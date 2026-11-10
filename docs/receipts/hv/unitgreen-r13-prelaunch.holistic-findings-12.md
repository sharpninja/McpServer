Twelfth holistic read before hostile validation round 13 (read-only subagent, session ClaudeCode-20261102T182906Z-plugin-session, turn req-20261106T005000Z-holistic-r13l). The harness refused the subagent's findings file, so this text is exported from the parent session transcript, unchanged except for the harness indentation.

2026-11-05T19:14:35 (measured local stamp)

# Twelfth holistic read before hostile validation round 13

I found 18 problems. Four are material: g1, g2, e1 and f3. The worst is a lab user name ("lab-wsman") still in a test file, on a line this branch added. Nothing tracked was changed.

**Counts:** (a) 5, (b) 2, (c) 3, (d) 1, (e) 1, (f) 3, (g) 3.

**Session log:** session `ClaudeCode-20261102T182906Z-plugin-session`, request `req-20261106T005000Z-holistic-r13l`.
- Opened with Begin-Turn.ps1, updated twice with Update-Turn.ps1, closed with Complete-Turn.ps1.
- Readback: completed, 6 actions, 4 decisions.
- The harness refused `holistic-r13l/findings-r13l.md` ("Subagents should return findings as text"), so the full findings are below. The response file `holistic-r13l/turn-response-r13l.txt` was written.

## What checked clean

- **Git:** HEAD, origin and the Windows child ref are all `94dc5b69`.
  - The commit is at 18:03:23, carries the marker's signing line, and has no gpgsig.
  - `becb0742..94dc5b69` touches 5 files: 59 lines added, 50 removed.
  - `52e6d627..94dc5b69 -- src build .nuke` lists nine src files and no build file.
  - The worktree has no tracked change.
- **The four federation files:** only the two lab machine names and documentation changed.
  - `PAYTON-LEGION2` became `proxy-a` and `PAYTON-DESKTOP` became `proxy-b`, the same mapping as `b21c21ec`.
  - The one literal-comparing assertion (FederationControllerTests.cs:355) still checks the same relation.
  - No new id collides with an existing one (only `proxy-1` exists, in another case).
  - Each new class entry cites only ids its class documentation already cites (MEMORY-FACT-008 holds).
- **CreateMappingAsync:** the corrected entry matches slice P (`611a5dc5`).
  - Before slice P, the adapter returned `UtcNow` on each read of `CreatedAt`.
  - After it, the adapter takes `mapping.CreatedAtUtc` from the upsert of the merged mapping.
- **Hashes and receipts:** every hash prefix 27.35 cites matches its file.
  - The compile and binding receipts pin the five blob hashes.
  - Gate times, counts and heads match the records. Every by-name compare shows 0/0. No gate receipt lists a tracked dirty path.
- **Manifest `DD3FD2BD`:** 34 files, every head `94dc5b69`, every hash equal, 0 failed, 0 not executed.
- **PR 87:** draft at `94dc5b69`. Its ready_for_review (09:59:32) and convert_to_draft (05:08:10) events match the records.
- **d3 event-log numbers:** they match the three named receipts.
  - All 769 events of the failed window carry the source QBrainAi (223 errors, 546 warnings).
- **Eleventh-read findings:** a1 to a7, b1, c1, c3 to c5, d1 to d4, e1, f1, f2 and g2 are fixed. c2, c6 and c7 are only partly fixed (f1 to f3). g1 is fixed for the four files, but its class is not closed (g1 and g2 below).

## (a) Superseded statements with no forward note

- **a1.** Plan line 2058 (27.30): "and the current one is 27.34's (tenth holistic read finding a3)".
  - Since slice Z9 the current D-A line is 27.35's.
  - Fix: say 27.35's.
- **a2.** Plan line 2182 (27.33 D-B): "27.34 amends it to 53 tasks".
  - Lines 2104 and 2148 run through 27.35.
  - Fix: append "and 27.35 to 54".
- **a3.** Projected problems line 42, item 89's readbacks: the list stops at "plan 27.34 ug-r13z8-store-readback-1.json".
  - Fix: add "plan 27.35 ug-r13z9-store-readback-1.json".
- **a4.** Projected problems line 64: "5 ORDER unit hits in ug-r13z7-timing-sweep.txt, as in ug-r13z6-, ...".
  - The sweep of record is `ug-r13z9-timing-sweep.txt`, still ORDER=5.
  - Fix: name it.
- **a5.** PR body line 383 and projected problems item 5 (line 49): the MEMORY-FACT-008 coverage list stops at "Z5, Z6 and Z7".
  - The PR body has no Z8, Z8 part 2 or Z9.
  - Item 5 credits "Slice Z8" with dating the two slice P methods. That was Z8 part 2. It has no Z9.
  - Fix: extend both lists through Z9.

## (b) Contradictions

- **b1.** Brief line 21: "the five holistic reads ... -7.md, ... -11.md (the last two exported from the transcript because the harness refused their findings files)".
  - UG48 to UG52, plan 27.31 to 27.35 and the headers of all five files say each one was exported from the transcript.
  - Fix: "(each exported from the transcript ...)".
- **b2.** Plan line 2232: "no dry run is needed for a check whose only write is its own receipt".
  - Plans 27.30 and 27.33 disclose receipt writes without a dry run as slips (the `ug-r13z4-mockgate-b-1` receipt and the slice Z7 compile receipt).
  - Fix: drop the exemption and say the print-only first run was the dry run.

## (c) Statements a reviewer can show false

- **c1.** `ug-r13z9-citation-compare-z8b-z9.txt` line 6 reads "OLD ids=101 NEW ids=104".
  - The sweeps that 27.35 and UG7 cite beside it give 98 and 101. No record explains the difference of three.
  - Fix: explain what the compare counts, or cite only its ONLY-NEW lines.
- **c2.** "The four federation test files this PR touches" appears in plan line 154, the brief's SCOPE and UG52, handoff line 9 and projected problems item 147.
  - The PR touches six federation test files. The other two are FederatedSessionLogServiceTests.cs and FederationTopologyServiceTests.cs, which hold no lab name.
  - Fix: "the four ... that named lab machines".
- **c3.** Projected problems item 5 says "Slice Z8 ... dated the two slice P mapping methods". It was slice Z8 part 2 (`becb0742`).

## (d) Missing disclosure

- **d1.** The Bash backtick slip (between 18:19:46 and 18:21:46) is disclosed in self-review line 88 and projected problems item 147, but not in plan 27.35's "Process slips, disclosed" (line 2244).
  - Fix: add it to 27.35.

## (e) Brief claims the evidence cannot support

- **e1 (material).** These claims cannot be supported:
  - UG52, plan line 2233, PR body line 158, projected problems item 147 and self-review lines 10 and 20 say `ug-r13z9-lab-names-touched-1.txt` leaves "exactly the four flagged identifier-scrub exceptions ... and five published package names".
  - Self-review line 20 adds "over every line of every touched file".
  - The receipt's pattern is case-sensitive with eight tokens: `PAYTON|LEGION|OMARCHY|kingd|sharpninja|10\.42\.0\.|F:..GitHub|E:..github`. It cannot see lab-wsman or "Omarchy" (g1, g2).
  - With case ignored there are also 10 more SharpNinja published-identity hits: Repl.Host.csproj:14, 18 and 19; BuildProjectSetTests:33; BuildTargetTests:472 and 484; QBrainAiRebrandPhase1Tests:11, 14 and 123; Directory.Packages.props:51.
  - The audit behind the four exceptions (`source-identifier-audit-20261102.json`) uses `lab-omarchy` and has no lab-wsman token either.
  - Fix: rerun with a case-insensitive pattern that adds lab-wsman, bare host names, LEG-VICE and /mnt/legion2. Classify every hit and restate the claim with the pattern named.

## (f) Eleventh-read findings 27.35 says are fixed but are not

- **f1 (c2).** Self-review line 19 still says "each new entry copies its class entry's ids, or for MemoryS1Harness the ids of the Clock property".
  - Three helpers do not: RequirementsWorkflowTests.CreateMockSelectionState, TodoWorkflowTests.CreateMockSelectionState and StubAuthRotationHandlerTests.CreateMarkerData.
  - Fix: reword the line as UG51 does.
- **f2 (c6).** Two places still call all 365 "members"; 10 of them are local functions.
  - Plan line 2159 (27.33): "the syntax-parse count of 27.34 is 365 member declarations".
  - Projected problems item 143 (line 33): "the undocumented members ... 365".
  - Fix: "365 declarations (355 members and 10 local functions)".
- **f3 (c7, material).** Self-review line 23 says the forward notes "are now carried through 27.35, 54 tasks and 94dc5b69". a1 to a5 show they are not.
  - This is the sixth recurrence of the structural cause declared in projected problems item 145.
  - Fix: apply a1 to a5.

## (g) Slice Z9 code and the plan 27.7 scrub

- **g1 (material).** `tests/QBrainAi.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs:466` reads: "/// and an account without Event Log write access (TEST-NODE-A lab-wsman) failed startup with".
  - lab-wsman is the lab's WSMan account on PAYTON-LEGION2 (plan sections 2, 3E and 11). Plan 27.7 names lab users.
  - The line is ADDED against develop. `git log -S` shows b2319488 added the token, and the slice N scrub (765490a9) put in TEST-NODE-A but left lab-wsman.
  - It is not one of the four flagged exceptions.
  - The 2026-11-02 audit, the Z8 sweep and the Z9 sweep all missed it.
  - Fix: replace it with generic text, for example "a test node's service account". That is a comment change, so the code head moves and the gate set reruns. Then correct e1.
- **g2 (material, needs a disposition).** `tests/Build.Tests/QBrainAiRebrandPhase1Tests.cs` names "Omarchy" at line 194 (doc) and line 209 (`Assert.Contains("Mcp" + "Server_Omarchy", installer, ...)`).
  - Both lines come from develop (2 hits at `9b099716`), in a file this branch touches. Lines 224 and 225 of the same file are two of the four flagged exceptions.
  - Omarchy names LAB-OMARCHY, and is also its OS distribution, but no record classifies it.
  - Line 209 asserts shipped configuration, the same class as the flagged E:\github lines.
  - Fix: either declare why it is not a lab name, or put a fifth flagged exception to the operator. The "Keep as flagged exceptions (Recommended)" ruling covered four, so a fifth is the operator's decision. Then correct the "exactly the four" claims.
- **g3 (low).** `RequirementsWorkflow.cs:420` (ListMappingsAsync, slice Z8 part 2) says "instead of the time of the read".
  - That is g2's unchecked sibling: before slice P the value was the time each CreatedAt was read, not the time of the list read. 27.35 records no sibling check.
  - Fix: use "instead of the time its CreatedAt was read". Fold it into the g1 comment fix.

## Scripts

All are read only and were analyzer clean before they ran, in `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13l/`:
- Get-GitFactsL.ps1, Get-Z9HashFacts.ps1, Find-LabNamesWide.ps1 (with pattern-l-lab-names-wide.txt), Get-PickaxeL.ps1, Find-NamedFiles.ps1 and Measure-SectionSources.ps1.
- Copies of the eleventh read's scripts with the output folder changed: Show-ReceiptSummaryL.ps1 and Test-ManifestL.ps1.
- Get-PrStateL.ps1, a copy with the UTC double conversion fixed.
- The pattern file pattern-l-stale.txt.

The wide sweep's "WorkingDirectory" (it contains "kingD") and "https://github" hits are false positives, and I counted none of them. One Find-TextInFiles call failed before reading anything because I passed comma-joined paths; I reran it with semicolons. Bash only launched `pwsh -File`. I made no contact with PAYTON-LEGION2 and printed no secret.
