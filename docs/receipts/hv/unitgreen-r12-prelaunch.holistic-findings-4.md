# Fourth holistic read before hostile validation round 12 (relaunch)

Read-only subagent turn req-20261104T204416Z-holistic-r12d2 (parent req-20261104T203928Z-resume-after-update) in session ClaudeCode-20261102T182906Z-plugin-session. Its final report as received, exported from the session transcript by Export-SubagentReport.ps1, with the harness indentation removed; the harness refused its findings file. The stopped first launch, req-20261104T193009Z-holistic-r12d, reported nothing.

2026-11-04T15:14:48 local (from Get-LocalStamp.ps1). This is the fourth holistic read before HV round 12, the relaunch. It was read only and changed nothing tracked.

**Session log:** session `ClaudeCode-20261102T182906Z-plugin-session`, request `req-20261104T204416Z-holistic-r12d2`. The turn was opened, patched twice and completed through the plugin scripts. Readback: status completed, 8 actions, 4 decisions. The harness refused `holistic-r12d2/findings-r12d2.md` (subagents must return findings as text), so the full findings are below. It accepted the response file `holistic-r12d2/turn-response-r12d2.txt`, which Complete-Turn used.

**Counts:** (a) 11, (b) 2, (c) 5, (d) 4, (e) 4, (f) 1, (g) 1. That is 28 in all. g1 is an inference, not a confirmed case. Under AC-006 as now stated (the sum, read from the sources), no unit case is confirmed below 100 times.

**Most important:**
- **c1, material.** GitHub shows PR 87 left draft at 2026-11-03 09:59:32 local. That is before the 13:11 ruling the records give as the reason, and the PR stayed ready for review through slices W to Z2.
- **b1.** The records disagree on how many bounds sit at exactly 100 times: five in some places, six in others.
- **e1.** UG43 asks the reviewer to pass slice Z's margins, which the current AC-006 contradicts.
- **a1.** Plan section 6 leaves out slice Z2.

## (a) Superseded statements with no forward note
- **a1. Plan line 146 (section 6, round 12 entry).** Quote: "two unit hang guards rise to 10 s (slice Y) and six more to 100 s, 5 s, 10 s and 25 s (slice Z), each at least 100 times the delay of the step it waits on."
  - Line 144 says the entry covers slices X, Y, Z and Z2, but this line leaves out Z2.
  - Under the sum, the 10 s and 25 s bounds were 95 and 94 times, and the WaitUntilAsync bound was 83 times.
  - Fix: add "under the sum three were below 100 times and slice Z2 raised them (WaitUntilAsync callers to 10 s, cancelled opens to 30 s, withheld open to 15 s)".
- **a2. Plan line 1586 (27.22).** Quote: "27.26 (slice Z), which names the current last code head." 27.27 names it (121992b3).
  - Fix: "... 27.26 (slice Z, a4e8d45f) and 27.27 (slice Z2), which names the current last code head 121992b3."
  - This D-A line is the same kind as lines 234, 1467 and 1513 from third-read finding c2, but it was not fixed.
- **a3. Plan line 1539.** Quote: "27.25 now records the gates of record". Fix: "27.27 now records the gates of record (at 121992b3)".
- **a4. Plan line 1570.** Quote: "(40 since 27.23, 41 since 27.24, 42 since 27.25, 43 since 27.26)". Fix: append "44 since 27.27".
- **a5. Plan lines 1740 and 1782.** Quotes: "27.25 and 27.26 add to this list" and "27.26 adds to both". Both leave out 27.27, although line 1877 says 27.27 adds to all three. Fix: name 27.27 in both.
- **a6. Plan lines 1729, 1777 and 1779.** Each history chain stops at r12z: "itself history since slice Z: `ug-r12z-timing-sweep.txt`, 27.26" and "history since slice Z (`ug-r12z-store-readback-1.json`, 27.26)". Fix: add the r12z2 step (27.27) to each.
- **a7. Brief line 23.** Quotes: "The changes since round 11 are in plan sections 27.24 to 27.26" (the list that follows includes the 27.27 work) and "the findings of both holistic reads that 27.25 and 27.26 fix". Fix: "27.24 to 27.27" and "the three (then four) holistic reads that 27.25 to 27.27 fix".
- **a8. Projected problems item 85.** Quotes: "The record corrections of both holistic reads" and "(ug-r12z-doc-claim-checks-1.txt for the gate claims)". Fix: count the reads correctly and name ug-r12z2-doc-claim-checks-1.txt.
- **a9. Projected problems "47 to 62".** Quote: "which items 73, 74, 89 and 90 supersede". Items 73 and 90 are themselves history, and item 96 holds the gates of record. Fix: "items 74, 89 and 96".
- **a10. Projected problems item 75.** Quote: "Slices X, Y and Z cost a fourth, a fifth and a sixth code head (item 93 adds the slips found since)".
  - Slice Z2's seventh code head is stated nowhere, and item 98 holds the latest slips.
  - Fix: "Slices X, Y, Z and Z2 cost a fourth to a seventh code head (items 93 and 98 ...)".
- **a11. TODO task 43** (ug-r12z2-store-readback-1.json, line 334). Quote: "six unit cases ... raised to 100 times".
  - Its 2026-11-04 notice adds only the launch condition. Under the sum, three of the six were 95 and 94 times.
  - Fix: extend the notice through the plugin (dry run first, then read back): "under AC-006 as appended 2026-11-04 12:20:22 the 10 s and two 25 s bounds were 95 and 94 times; slice Z2 (task 44) raised them".

## (b) Contradictions
- **b1. Five versus six bounds at exactly 100 times.**
  - Six: plan 27.27 line 1863 lists TurnTransactionCoordinatorTests, TodoWorkflowTests, StubMarkerFileReaderTests, DispatcherTests, the line 779 drain and the compensation read. The self-review (line 12) agrees, and the r12z2 sweep marks 7 rows (lines 66, 69, 106, 107, 200, 202, 222) "exactly 100 times".
  - Five: projected problems item 95 ("Five more bounds are exactly 100 times"), PR body line 204 ("Five bounds are exactly 100 times that sum"), brief UG44 ("the five at exactly 100 times as 27.27 lists"), the r12z2 classification header line 9 ("the five at exactly 100 times say so") and the 121992b3 commit message (which leaves out the compensation case).
  - Fix: say six (seven cases) in all of these, and declare that the pushed commit message leaves one out (history is not rewritten).
- **b2. Change-log coverage.** Projected problems item 5 lists MEMORY-FACT-008 coverage for "slices Q, R, P, S, T, U, V, W, X, Y and Z". PR body line 225 adds Z2, and the Z2 diff does carry dated entries. Fix: add Z2 to item 5.

## (c) Statements a reviewer could show false
- **c1. Material: the PR's draft history.**
  - What the records say:
    - Plan 27.24 records the 13:11 choice "Mark ready, then HV" as: finish the gates at 11406fdf, commit the records, take PR 87 out of draft on GitHub, then launch HV.
    - Brief line 17: "taken out of draft before this round under the operator's 2026-11-03 ruling, plan 27.24".
    - Self-review lines 24 and 37 ("the ready mark", Set-PrReady), and the PR body line 5.
  - What GitHub shows (`gh api repos/sharpninja/McpServer/issues/87/timeline`, read only):
    - Exactly one ready_for_review event: actor sharpninja, 2026-11-03T15:59:32Z, which is 09:59:32 local. That is before round 11 launched (11:31) and 3 h 12 min before the 13:11 ruling.
    - The chatgpt-codex-connector bot commented 25 s later, which fits the operator's 09:59 "ignore review comments on PR".
    - There is no convert_to_draft event, and isDraft is false.
  - So any ready mark after 13:11 did nothing, and the PR was ready for review the whole time slices W, X, Y, Z and Z2 changed code.
  - Fix: state the actual time and actor in 27.24, the brief, the PR body and the self-review. If the operator wants the ruling's order, convert to draft and back after the records commit (operator decision).
- **c2. "265 ms in all" for the cancelled opens.** This appears in plan 27.27 line 1861, the 121992b3 doc comments (SqliteBoundedOpenFourteenthTests.cs:39 and :265), sweep lines 185 and 194, the commit message, PR body line 106, projected problems item 95 and brief UG44.
  - Read from the source, while the worker is held the open throws at the 250 ms deadline (SqliteBoundedConnectionOpener.cs:220-228). It does not wait for `completion`, which the exit tracker sets only after the thread exits (:272-291).
  - Each 10 ms WhenAny also waits on the deadline (:208-217), so the loop adds nothing beyond 250 ms.
  - The true sum is 250 ms, so 30 s is 120 times. The over-count is conservative and no margin fails, but the stated path is wrong.
  - Fix: declare the over-count in 27.27 and leave the doc comments, since changing them would move the code head.
- **c3. Plan 27.27 line 1873:** "the three changed rows at their new values".
  - Eight rows changed: HandoffDurabilityTests.cs:370, :413, :724, :737 and :866 at 10 s; SqliteBoundedOpenFourteenthTests.cs:79 and :315 at 30 s; BugTriage139SixteenthSqliteConsumerTests.cs:159 at 15 s.
  - Fix: "the eight rows of the three raised bounds".
- **c4. Plan 27.27 line 1867:** "the single-line slice Z remarks moved into the list form". The remark moved on ProcessingLease_RenewsAndFencesTerminalUpdates (HandoffDurabilityTests.cs around line 672) is slice W's 2026-11-03 remark, not a slice Z one. Fix: name it.
- **c5. Low: the line 779 drain.** It is classed HANG-GUARD and counted "exactly 100 times", but it sits inside a finally whose `catch (Exception)` swallows the timeout (HandoffDurabilityTests.cs:777-784).
  - Its firing can never fail the test, so it is a cleanup bound, not a hang guard as AC-006 defines one.
  - The 50 ms heartbeat delay it is measured against is cancelled, not waited through.
  - Fix: state this in 27.27 or reclass it in the next classification copy.

## (d) Missing disclosures
- **d1. The fourth holistic read has no place in the records.**
  - Missing from: the brief's claim paragraph, its receipt list (line 21 says "the three holistic reads before this round") and its UG claims; the projected problems (the header says "after four holistic reads" but no item records it); the PR body.
  - Only the self-review has <<FILL-HOLISTIC4>>. The self-review's HOLISTIC-REVIEW block ("four read-only subagent reads") also leaves out the stopped first launch req-20261104T193009Z-holistic-r12d.
  - Fix: add the fourth read (its text, counts and how each finding was handled) to the brief claim, a UG, a projected-problems item and the PR body.
- **d2. PR 87 stayed ready while code changed (see c1).** The 27.26 return-to-draft step covers only a DISAGREE, and no record says the PR was ready for review during slices Y, Z and Z2. Fix: declare it in 27.27 and the projected problems.
- **d3. The Z2 compile and binding receipts were made before the commit.** Both record head a4e8d45f with the three test files modified (" M tests/..."), and the plan does not say the bound content equals the commit. It does: 7A7BB808..., FC10B620... and 1E7100E1... equal the files at 121992b3. Fix: add that sentence to 27.27.
- **d4. A lab drive path in a file this branch touches.**
  - tests/QBrainAi.McpAgent.Tests/TodoWorkflowTests.cs entered the branch only in slice Y (ec58cf4a), after the slice N scrub. Line 286 still holds `WorkspacePath = @"E:\github\QBrainAi"` (from 6de989f3).
  - 27.7 names "E:\github" as a lab drive path to scrub "in every file this branch touches". It is the only lab identifier in the 50 code and test files changed since 878c366f.
  - UG30 narrows the scrub to "a changed line".
  - Fix: declare it in 27.27 as inherited data, or replace it (that moves the code head, so the operator decides).

## (e) Brief claims the evidence cannot support
- **e1. UG43.** Quote: "at a4e8d45f the six cases 27.26 names wait at least 100 times the delay of the step each races (... 10 s open timeout ..., two cases 25 s ...)".
  - Under the stored AC-006 sum, 27.27 itself says three were 95 and 94 times, so a reviewer judging against the stored criterion must FAIL this claim.
  - The same unqualified wording appears in PR body lines 100 and 203 and handoff top line 5 ("raised to the AC-006 margin").
  - Fix: qualify each as "measured as the single largest timer; under the sum since 2026-11-04 12:20:22 three were 94 and 95 times and slice Z2 raised them".
- **e2. UG44** says "the five at exactly 100 times as 27.27 lists", but 27.27 lists six. Fix: "six".
- **e3. UG3** says "every timer-bounded wait ... is classified in ug-r12z2-timing-sweep.txt".
  - The v2 finder matches only certain forms. The third read listed the forms it misses: WaitForExit, WaitOne, WaitAll, WaitAny, named bounds and deadline loops, found only in Integration cases.
  - Production timeouts a test passes as arguments (the 15 s, 10 s and 100 ms open and resolve timeouts) appear only in the configured-durations list.
  - Fix: "every timer-bounded wait the v2 finder matches (the forms it misses occur only in Integration cases, third read)".
- **e4. UG44** says "restates the reasons 27.27 names", but 27.27 gives only a count ("12 reasons").
  - The 12 are the sweep rows citing 2026-11-04 12:20:22: lines 66, 67, 69, 106, 107, 182, 185, 194, 200, 202, 208 and 222.
  - Fix: name them, or describe them by that citation.

## (f) Third-read findings that 27.27 says are fixed but are not
- **f1. c1, the propagation claim.** The self-review (line 14) concludes "None states a superseded value as current", resting on ug-r12z2-propagation-search-1.txt.
  - That search uses four patterns and counts a whole line as "marked" if any marker appears on it.
  - So it misses plan lines 146, 1539, 1570 and 1586 entirely.
  - It excuses brief line 23 as "marked pointer2726" because the same long line also names 27.27, yet its "27.24 to 27.26" and "both holistic reads" are stale.
  - Fix: apply a1 to a10, widen the search (any 27.2N pointer, and "names the current" or "now records", judged per sentence), rerun it, and restate the self-review sentence from the new output.

## (g) Timing dependence under the sum
- **g1. Inference, not confirmed.** AC-006 counts "the production waits ... the step passes through". HandoffDurabilityTests uses a temporary SQLite file without pooling (HandoffDurabilityTests.cs:41-49).
  - Every WaitUntilAsync poll opens a new connection and reads every 10 ms while the heartbeat and the ingests write.
  - A search of src (QBrainAi.Storage, QBrainAi.Services, QBrainAi.Support.Mcp) finds no journal_mode or busy_timeout setting.
  - As I recall the library, Microsoft.Data.Sqlite retries a statement that returns SQLITE_BUSY with a 150 ms sleep; I did not verify this here.
  - One retry would make the renewal path 210 ms, so the 10 s bound would be 47 times.
  - The records never say whether conditional contention retries count. Fix: declare a reading in 27.27 and the projected problems. Mine: they are conditional, so outside the sum. If the operator reads them as inside, the bounds need headroom (an operator criterion question).
- **Checked against source and consistent:**
  - **WaitUntilAsync callers:** all five call sites (lines 361, 405, 706, 731, 856). The heartbeat's due time is computed synchronously before the extractor runs (HandoffIngestionService.cs:136, :1015-1033), with a cancellable 50 ms poll. That gives 60 ms, 167 times.
  - **Withheld open:** 105 ms, 143 times. Its 2 s guards are 400 times the 5 ms exit poll.
  - **Cancelled opens:** 250 ms, 120 times. The 2 s SpinUntil bounds are 400 times. The 10 s open timeouts at lines 70 and 296 need only beat the cancel (:701-712).
  - **Others:** TurnTransactionCoordinatorTests (:582, per call); DispatcherTests (:84); the TodoWorkflowTests cancellation (no retry or delay in QBrainAi.Client); StubMarkerFileReaderTests; the compensation read (:876); QBAgentHeartbeatOverlayTests at 125 times (QBAgentRunLoop.cs:139-156, :232).
  - **Configured-duration rows of 2 s or less:** clean, for example the WorkspaceIdentity 100 ms timeout returns before any native work.
  - **YIELD cases:** they run their callbacks synchronously.
  - **Memory consolidate:** its 100 ms delay comes after the lock, and no unit case bounds it with a timer.

## Checked and consistent
- **Git:**
  - HEAD and both origin refs are 121992b3. a4e8d45f..121992b3 touches only the 3 test files, and 1f51d4b2..121992b3 lists no src, build or .nuke path. No commit after 121992b3 yet.
  - Assertions are unchanged, every changed member has a dated 2026-11-04 entry, and WaitForApprovalOwnerAsync gained XML documentation.
- **Receipts:**
  - Every cited hash prefix matches: 15332B7A, 27197DAF, 2170CBCF, C14694C4, 495C98EB, 72113226, 74F57AF6, 8424279A, 9F38BFFE, 11011AD9, D15A6ABF, 96DCAFE5, 07B6A9BE, 7938C09C, 24E1FE40.
  - The AC-006 readback text matches the quote.
  - Compile ran 12:30:54 to 12:32:56 and its receipt was written at 12:33:28. Binding 36/0/0.
  - Store compare: only AC-006, task 43 and the new task 44 changed; 44 tasks, 0 checked.
  - Sweep: 212 hits (60 unit, 60 helper, 5 clock, 87 not-unit), 42 hang guards, 5 ORDER. Configured durations: 302 hits, 249 listed, the same as at a4e8d45f. The 17 moved keys reconcile with the diff.
- **TRX manifest:** 34 files, 30 TRX, every head 121992b3, every hash equal, 0 failed and 0 not executed. Run totals: 4739 and 4739 (unit), 741 and 718 (lanes), 529 and 433 (Build.Tests).
- **Windows and by-name checks:** Windows ref and HEAD matched, and the lane ran in session 1, interactive, at 121992b3. Every Z2 by-name comparison has 0 names only before and 0 only after.
- **Third-read findings:** a1 to a10, b1 to b3, c2, c3, d1 to d4, e1 to e4 and f1 are handled as 27.27 says; c1 is f1 above.
- **Formatting:** no en dash, em dash or table row in the 7 records.

Everything is in /home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r12d2/. The read-only scripts (all analyzer clean):
- Get-GitFacts.ps1
- Find-InFiles.ps1
- Select-ConfiguredRows.ps1
- Get-Hashes.ps1
- Test-Manifest.ps1
- Test-Dashes.ps1
- Get-FileHistory.ps1
- Find-LabNamesInChanged.ps1
- Get-PrState.ps1
- Get-PrTimeline.ps1
