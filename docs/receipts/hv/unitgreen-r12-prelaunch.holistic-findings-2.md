# Second holistic read before round 12: findings as received

Written 2026-11-03T19:23 local by ClaudeCode. This file is the full findings text of the second holistic read before hostile validation round 12, as the read-only subagent returned it in its final report (session ClaudeCode-20261102T182906Z-plugin-session, turn req-20261104T001215Z-holistic-r12b, parent req-20261103T205846Z-resume-pr87). The harness refused the subagent's own findings file; its condensed copy is the turn's response. The text below is unedited except for this header.

---

2026-11-03T18:39:53 local. Second holistic read before HV round 12. I read everything listed end to end, read only. I changed nothing tracked.

**Findings file:** the harness refused `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r12b/findings-r12b.md` (subagents must return findings as text), so the full findings are below. A condensed copy is at `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r12b/turn-response-holistic-r12b.txt`; it was the response input for Complete-Turn.

**Session logging:** session `ClaudeCode-20261102T182906Z-plugin-session`, request `req-20261104T001215Z-holistic-r12b`. The turn was opened, patched twice and completed through the plugin scripts. The readback shows status completed, 6 actions, 3 decisions, response length 9396.

**Counts:** (a) 4, (b) 3, (c) 9, (d) 3, (e) 1, (f) 2; 22 in all. C1 is material: it is a timing problem in the unit suite, the same class slice Y fixed. The other 21 are record fixes.

**State checked:**
- HEAD, origin/fix/unit-suite-green-20261019 and origin/fix/unit-suite-green-20261019-windows are all ec58cf4a.
- `ec58cf4a..HEAD` is empty, and `1f51d4b2..ec58cf4a -- src build .nuke` lists no path.
- git status shows 0 tracked changes and 252 untracked paths, all under docs/receipts.

## (a) Superseded statements with no forward note

- **A1.** The TODO IMPL-UNITGREEN-001 metadata is stale (ug-r12y-store-readback-1.json lines 337, 354 and 363).
  - It still says "Draft PR https://github.com/sharpninja/McpServer/pull/87.".
  - It keeps "HV reviewer until Monday afternoon 2026-11-02 America/Chicago: Cline CLI, Beam, High effort ...".
  - It still names TR-MCP-NUKE-019 AC-1 to AC-4 for amendment G and AC-5 for amendment H1.
  - Against that: 27.19 says the Cline line does not apply, section 24 removed AC-4 and AC-5, and the PR leaves draft before round 12. UG7 asks the reviewer to check TODO metadata against the plan.
  - Fix: update the note and technicalDetails through the plugin when marking ready (dry run, read back), or add a correction notice, and cite it in 27.25.
- **A2.** Brief line 20: "TEST-MCP-TRANSCRIPT-008 and TEST-MCP-QBOLLAMA-001 (the two integration classes whose start this round changes)".
  - This is round 9 wording (section 26). Rounds 10 to 12 changed no integration class's start.
  - Fix: "whose start round 9 changed (section 26)".
- **A3.** Plan 27.24 line 1729, "Timing sweep receipt `ug-r11x-timing-sweep.txt` ...", has no forward note. The store readback line just above it (1728) has one.
  - Fix: append "(history since slice Y: ug-r12y-timing-sweep.txt, 27.25; its helper note at StubMarkerFileReaderTests.cs:267 was false, finding C7)".
- **A4.** Projected problems line 53 excepts round 11 item 59. That drops item 59's standing caveat: "a reviewer may still count a short ORDER wait as a wall-clock bound". The r12y sweep still has 5 ORDER unit hits.
  - Fix: carry that sentence forward.

## (b) Contradictions

- **B1.** Handoff top line 8 adds "After a DISAGREE verdict: take the PR back to draft while code is pending ...". The plan records no such rule: 27.24 line 1745 and 27.25 line 1798 only say HV launches with no code pending, the gates green and the PR out of draft.
  - Fix: record it in 27.25 as a decision within the guidelines, sourced to "If it needs code, it's not ready for HV." (2026-10-21) and "HV was to come after PR87 was  ready for review." (2026-11-03 13:11). Or drop it from the handoff.
- **B2.** Two lists are headed "Declared for round 12": 27.24 lines 1740 to 1742 and 27.25 lines 1782 to 1786. Neither says which one governs.
  - Fix: retitle the 27.24 list "Declared at 11406fdf (carried into 27.25)", and have 27.25 say it adds to that list.
- **B3.** Self-review line 34 says "Bash only launched pwsh -File with literal arguments". Line 49 of the same record discloses the 17:19 backtick pattern the shell partly ran.
  - Fix: add "except the 17:19 slip disclosed below".

## (c) Statements a reviewer could show false

- **C1 (material).** The margin pass missed three unit cases.
  - The claims: plan 27.25 lines 1765 to 1766, "The margin applied, read against source, to every unit and helper timer ...: Two hang guards fell below 100 times", and projected problem 79.
  - At least three more unit cases race a finite fake delay against a production timeout the test asserts, all below 100 times:
    - TurnTransactionCoordinatorTests.ExecuteAsync_WhenSubscriberCommitTimesOut_EntersDegradedMode and ExecuteAsync_WhenConcurrentSubscriberCommitsTimeOut_AllReturnCommitTimeout. `Task.Delay(TimeSpan.FromSeconds(30), ...)` at :480 and :522 races `CommitTimeoutSeconds = 1` at :484 and :527, which is 30 times. Sweep lines 98 and 99 class them CANCEL with "the substitute blocks until the coordinator commit timeout cancels it". That misdescribes a delay that ends on its own after 30 s.
    - HandoffDurabilityTests.SaveRunAfterTodo_CompensationTimeout_DoesNotReportCreatedFromMemory (:516 to :529). `GetDelay = TimeSpan.FromSeconds(3)` makes GetByIdAsync run `Task.Delay(delay, ...)` (:980 to :981). It races `CompensationTimeout = TimeSpan.FromMilliseconds(50)`, which production arms as a real `new CancellationTokenSource(_lease.CompensationTimeout)` (src/QBrainAi.Services/Services/HandoffIngestionService.cs:876). That is 60 times.
    - That case appears only as helper hit line 61, with no consumer or margin. It took 2.17 s in the Linux unit TRX and 1.05 s in the Windows one.
  - Either reading fails:
    - Under the reading 27.25 states, each delay is a hang guard below the margin.
    - Read literally, sentence B of the 16:37:35 append makes each a wall-clock wait in a unit test.
    - So a reviewer can fail UG3 and UG42 on them.
  - Fix, test only and BDPv4 as slice Y did:
    - Make each fake wait for cancellation with `Timeout.InfiniteTimeSpan`, as BrainSlotInvocationTransactionTests.cs:314 and TodoWorkflowTests.cs:369 already do, or raise it to at least 100 times the timeout it races.
    - Sweep every finite fake delay that a production timeout must beat (the CANCEL and HELPER classes), and correct the sweep reasons.
    - This means a new code head and the full gate set before round 12.
  - Cqrs DispatcherTests (5 s against 50 ms, exactly 100 times) already meets the margin.
- **C2.** Sweep line 91 is misattributed. It reads "UNIT TIMER ... SessionLogTriageStoreTests.cs:237 ... UpsertTurnAsync_OmittedPlanFileTodoId_WritesNoneAndCanceled class=CANCEL: ... the assertion is the canceled outcome".
  - Line 237 is inside the nested HungSaveChangesInterceptor (:229). Only the Category=Integration case SubmitAsync_HungSaveChanges_FailsFastWithStorageUnavailable uses it (:134 to :140). The unit case at :208 uses no interceptor, and its "canceled" is a turn status string.
  - So the owner, the UNIT status and the reason are all wrong. The header counts on line 18 (UNIT=53, CANCEL=9, HELPER=40) should be 52, 8 and 41.
  - Line 61 also names the owner "TrackingTodoService.helper:SeedExisting". The delay is in GetByIdAsync (:978 to :981). The same mapping rule, the nearest preceding method, produced both errors.
  - Fix: correct both entries, regenerate the receipt, and restate the counts in 27.25, in the brief's UG3 and UG42, and in item 79.
- **C3.** Slice Y's compile receipts are retrospective, and nothing says so.
  - Plan 27.25 line 1778 reads "Compile receipts: `ug-r12y-compile-1.json` and `-2.json`, and retrospectively `ug-r11x-compile-1.json` and `-2.json`". That implies the r12y receipts were written at run time.
  - Both hold `"retrospective": true` and "Written after the run ... because the run had no durable receipt". They were written at 16:55:46 and 16:55:47, after the binding (16:52:45 to 16:54:08).
  - Brief line 95, item 82, PR body line 169 and the self-review name only slice X's as retrospective. The first read's C4 problem recurred, undisclosed (this is also a class (d) gap).
  - Fix: disclose it in all five records and list it as a process slip.
- **C4.** Plan 27.25 line 1773 says the dated entries correct the slice X entries "without editing them".
  - The diff deletes and re-adds three slice X entries to put them in list form: StubMarkerFileReaderTests.cs old line 127, the SyntaxRuleCheck remarks in TestOutputFolderContractTests.cs, and the IsPathSeparator remarks in TemporaryInstanceChecks.cs. The wording is unchanged; the lines are not.
  - Fix: "with their wording unchanged (three single-line remarks moved into list form)".
- **C5.** The DRY claim overreaches.
  - TemporaryInstanceChecks.cs:340 to 342 calls IsPathSeparator "the one separator test of this class". 27.25, item 80 and the PR body say "the duplicate is removed".
  - `PathIdentity.Split` (:444) still states the separator set itself: `text.Split(['/', '\\'], ...)`.
  - Fix: qualify the claim, or route Split through one shared array (a code change, which C1 needs anyway).
- **C6.** Brief line 115 (UG13) says slice Y "refined the R11-F1 and R11-F2 fixes". Handoff top line 11 says R11-F1 to R11-F3 were "fixed in slice X and refined in slice Y".
  - Slice Y touched StubMarkerFileReaderTests.cs only with a documentation entry that ends "No code change". It did not touch R11-F3's file at all.
  - Fix: "refined the R11-F1 fix and recorded the R11-F2 case's hang-guard margin (documentation only)".
- **C7.** Self-review line 14 claims "the PR body and the handoff say the same" about D-B never holding a ready step. The handoff top section never says that.
  - Fix: drop "and the handoff", or add the sentence to the handoff's D-B line.
- **C8.** Handoff top line 9 says "The gates at 11406fdf, 1f51d4b2, cb174ac8, 2e121015 and 6082b116 and their TRX sets are history". No TRX set exists at cb174ac8 or 2e121015; the folder listing of docs/receipts/hv confirms it.
  - Fix: name only the sets that exist.
- **C9.** The manifest (unitgreen-r12-trx-ec58cf4a/manifest.json lines 675, 685 and 695) says of QBrainAi.Client.Tests, QBrainAi.Cqrs.Tests and QBrainAi.Launcher.Tests: "its Category=Integration cases, where it has any, run in both MigrationIntegrationTests lanes".
  - None of the three is a lane project; each selected-projects.json lists seven projects without them. None holds an Integration case today, so the clause is empty but false as stated.
  - Fix: change the copier text, rewrite the manifest (dry run first), and restate its SHA-256 everywhere it is cited.

## (d) Missing disclosures

- **D1.** Write-time and dry-run evidence is not saved.
  - Nothing saved at write time shows that only the target criterion changed in the four criterion writes at 12:43:50, 12:44:03, 16:37:35 and 16:37:48. The two criteria readback receipts are later readbacks, and ug-r12y's says so. Round 11 item 55 disclosed the same gap for slices U to W; the round 12 records do not.
  - "Every run below had a dry run first" (plan line 1787, brief line 57) also has no receipt; only the transcript shows it.
  - What I verified by script: ug-r12-store-readback-1.json (15:13:57) against ug-r12y-store-readback-1.json (18:10:37) shows exactly two criteria changed, each by an append (position 1 and AC-006). It also shows task 41 changed and task 42 added, 42 tasks, none checked.
  - Fix: disclose both gaps, and cite that comparison, saved as a receipt.
- **D2.** The round 11 receipts are still untracked: the request and response JSONL (3.8 MB), the self-review copy, and about 87 hv-ug-r11-* files, including memory and store dumps. They enter in the records commit, yet 27.24 and item 63 already say "committed as received" and no secret-scan result is stated.
  - Fix: run the secret scan (dry run first) before the records commit, and state the result in 27.25 and the PR body.
- **D3.** Every Linux restore in ug-r12y-final-linux-nu190x.txt was a no-op ("All projects are up-to-date for restore"). The Linux 0 NU190x therefore rests on the inference 27.22 declared (PLAN-2), and neither the round 12 projected problems nor the PR body carry it.
  - Fix: carry it as a declared item.

## (e) Brief claims the evidence cannot support

- **E1.** Brief line 105 (UG3) says "every timer-bounded wait ... is classified in ug-r12y-timing-sweep.txt, each hang guard with its margin". Line 144 (UG42) says "every hang guard in the unit projects is at least 100 times ..." and that the sweep "classifies every unit and helper hit with a correct reason and margin". C1 and C2 contradict both.
  - Fix: restate both claims against the sweep regenerated after the fixes.

## (f) First-read findings that 27.25 says are fixed but are not

- **F1. B3, the stored criterion texts.** The stored texts still cite the ruling as slice X's authority:
  - The FR-MCP-TESTRESOURCE-001 generated-artifact criterion (readback line 22): "... strengthened under the operator's "Strengthening Acceptance Criteria should always be the recommended option" and "don't gate on me between rounds")".
  - AC-006 (line 120): "(finding of the hostile validation round 11 that stopped without a verdict, 2026-11-03, under the same rulings)".
  - The slice Y appends do not correct them, and 27.25 line 1786 and item 81 declare only the change logs and task 41.
  - Fix: declare both stored citations in 27.25 and item 81. Changing criterion text is the operator's decision, so either ask him or leave them declared.
- **F2. D4, the store readback.** The projected problems still name no store readback; the word "readback" appears nowhere in the file. Line 53 says round 11 item 62 (its store readback) is superseded by items 73, 74 and 84, and none of those names one.
  - Fix: add an item for ug-r12y-store-readback-1.json (18:10:25 to 18:10:37, 6144F7B5..., 12 and 7 criteria, 42 tasks, 0 checked; a readback, not write output), and point line 53 to it.

## Checked and consistent (no finding)

- **Slice Y diff:** exactly 5 test paths (TodoWorkflowTests.cs in McpAgent.Tests, QBAgentHeartbeatOverlayTests.cs, StubMarkerFileReaderTests.cs, TestOutputFolderContractTests.cs, TemporaryInstanceChecks.cs). The commit is at 16:56:33 and its message matches the plan.
- **Order and times:**
  - The criteria readback at 16:38:29 follows the 16:37:35 and 16:37:48 appends.
  - Compile 1 ran 16:44:23 to 16:47:01 with only the contract dirty.
  - The mock gate ran 16:47:37 to 16:48:00: 18/0/0 (2 positive controls, 16 wrong checks), 7 hashes pinned.
  - Compile 2 ran 16:49:14 to 16:51:36.
  - The binding ran 16:52:45 to 16:54:08: 124/0/0 (Repl.Core 92, Support.Mcp 18, McpAgent 9, QBAgent 5), covering every unit consumer of TemporaryInstanceChecks.
  - The contract, ScriptedPathIdentity.cs and TestOutputFolderBindingTests.cs hashes are equal at the mock gate, the binding and on disk at ec58cf4a; the other four pinned files changed.
- **Contract and code:**
  - The contract has 39 rows (32 refused, 7 accepted) and 16 wrong checks, and the five new rows are as listed.
  - AliasCheckedNames and the removal of IsSeparator match the plan; containment (ac-4) is unchanged.
  - RunAsync_RunnerThrows_HidesGlyph does not depend on the 20 ms heartbeat: the first frame is painted before any await (QBAgentRunLoop.cs:140 and :228 to :241), and "still working" occurs nowhere in src.
- **Gates at ec58cf4a match their receipts:**
  - Test: Linux 4739/0/0. Windows 4739/0/0 with ValidateTraceability passed, ref and HEAD matched. Both hosts ran the same names.
  - Lanes: Linux 741/0/0. Windows 718/0/0 (proof session 1, interactive, head ec58cf4a; runner SHA-256 matched; task removed).
  - Build.Tests: 529/0/0 on Linux and 433/0/0 on Windows.
  - Linux ValidateTraceability passed, with its .out.txt and .exit.txt. NU190x is 0.
  - No gate receipt lists a tracked change.
  - The manifest SHA-256 is 79BCD6FB...0893, with 30 TRX and 4 selection files; the Linux TRX hashes equal their receipts.
- **TODO:** 42 tasks, none checked; task 41's correction and task 42's text are as the plan says.
- **First-read findings:** A1 to A11, B1, B2, B4 to B6, C1 to C4 and C6 to C9, D1 to D3, E1 to E3 and the DRY note are handled as 27.25 says, apart from F1 and F2 above. C3 holds: the r11w and r11x .out.txt and .exit.txt exist. C4 holds: the r11x compile receipts are retrospective, written at 16:55:45. C5 holds only if `<<FILL-PROPAGATION>>` cites a saved receipt for the forward-note search.
- **Expected placeholders:** not counted as findings, as instructed, and neither is ug-r12-doc-claim-checks-1.txt.

**Scripts I wrote,** all analyzer clean and read only, in `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r12b/`: Get-SliceYGit.ps1, Get-ReceiptFacts.ps1, Get-SourceFacts.ps1, Compare-StoreReadbacks.ps1, Get-JsonReceiptSummary.ps1 and Get-TrxSetFolders.ps1. Their outputs are in the same folder, including slicey-diff.txt, readback-compare.txt and json-receipts.txt.
