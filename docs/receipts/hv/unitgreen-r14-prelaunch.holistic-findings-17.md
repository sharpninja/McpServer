Seventeenth holistic read before HV round 14 (read-only subagent, session ClaudeCode-20261102T182906Z-plugin-session, turn req-20261106T150000Z-holistic-r14c, parent turn req-20261104T231251Z-continue-pr87-completion-2). Hand-back report exported from the session transcript because the harness refused the subagent's findings file.

2026-11-06T09:55:28 Seventeenth holistic read before HV round 14 (read only).

Session ClaudeCode-20261102T182906Z-plugin-session, request req-20261106T150000Z-holistic-r14c. I opened the turn at 09:15:58, logged 8 actions and 5 decisions, and completed it at 09:58:03 (response file holistic-r14c/response-r14c.txt). The harness refused holistic-r14c/findings-r14c.md ("Subagents should return findings as text"), so this hand-back is the only full copy.

**Counts:** 50 findings: (a) 18, (b) 6, (c) 11, (d) 7, (e) 2, (f) 4, (g) 2.
- Material: g1, e1, c1, d2.
- Moderate: g2, a4, c3.

**How the read was done**
- I read myself, end to end: plan 27.38 to 27.40 (lines 2318 to 2409), the handoff, the self-review, the sixteenth findings file, the 288b03c3 commit message, the f9b6bde8..288b03c3 diff and the changed code at head, AC-006 through the getter (LENGTH 9314), MEMORY-FACT-008 through the getter, the comment-only receipt, and the receipt and script listings.
- Nine read-only helpers read the rest, each with its own completed child turn under this one: req-20261106T152000Z-holistic-r14c-p1, -p2, -p3, -p4 and -p5 (plan lines 1 to 2317 in five ranges), -briefa and -briefb (the brief), -pp (projected problems and PR body) and -prd (the full PR description).
- Every plugin call succeeded.

**(g) Slice Z14 code and tests**
- **g1 (material).** VoiceConversationService.cs:76 at 288b03c3 says: `<exception cref="NullReferenceException">On Windows, <paramref name="loggerFactory"/> is null`.
  - The call at :105, `loggerFactory.CreateLogger<DesktopProcessLauncher>()`, is LoggerFactoryExtensions.CreateLogger<T>, which null-checks its receiver.
  - Helper briefb's probe (holistic-r14c/briefb/Test-NullFactoryException.ps1, which I read) threw System.ArgumentNullException with ParamName "factory" in the 8.0.0, 9.0.0 and 10.0.9 assemblies. The project has no overload of its own.
  - So the slice's only production change documents an exception the code does not raise. MEMORY-FACT-008 ("exceptions as applicable") is still not met; the sixteenth read's g1 started from a wrong premise.
  - Lines repeating the false exception:
    - plan 164, 2359, 2378 and 2389;
    - commit message line 22;
    - handoff lines 9 and 20;
    - self-review lines 14 and 97;
    - brief lines 17, 23, 334 and 335;
    - PR description lines 199, 212 and 458;
    - PR body line 22;
    - projected problems items 173 and 181.
  - Fix: a documentation-only change. Add loggerFactory to the ArgumentNullException entry ("on Windows, reported as parameter factory"), drop the NullReferenceException entry, and add a dated entry, keeping the earlier ones. That moves the last code head, so the gate set reruns (D-A). The alternative is to declare it.
- **g2 (moderate).** AC-006 as appended (store offset about 8400) says "and the code or the test enforces that ... the test either reads the decision after the pass's last removal or shows that no removal in its trace can await".
  - Slice Z14 states the condition only in documentation and the two assertion messages. No assertion checks it, so a later change that gives a create an interactive session would leave both bindings green with an unsound observation.
  - The condition is true at 288b03c3:
    - InteractiveSession is assigned only at :780 and :943, in turn paths no trace calls;
    - DeleteSessionAsync awaits only when InteractiveSession is not null (:488 to :503);
    - the shutdown stream runs only under IsAlive (:1947).
  - Self-review line 26 and projected problems item 181 declare this.
  - Fix (recommended, the AC-strengthening option): state in AC-006 what "shows" requires, and add a run-time assertion to the two bindings that no session in the trace holds an interactive session before the trigger.

**(f) Sixteenth-read findings said fixed that are not**
- **f1 (e1).** The SentinelRequest summary, VoiceConversationServiceTests.cs:369 to 370, was not touched by slice Z14: "so no reuse case reuses it and a fired cleanup pass that removed it has run to its end." e1 named SentinelRequest explicitly. Fix: add the no-await condition in the g1 documentation change, with a dated entry.
- **f2 (b4).** Handoff line 15, "the fifteenth holistic read's d7, g1 and g2 are fixed in slice Z13", still leaves out c3. Its next sentence, "e1, g1, g2 and g3 are fixed in slice Z14", leaves out the code halves of c3 and c4 that 27.40 line 2383 names. Fix: name them.
- **f3 (d5).** Plan 1016 still cites no temp-folder receipt for ug-r8b-build-tests-2, although ug-r8b-build-tests-2.tempdir.json is committed. Also, d5's claim that ug-r8b-build-tests-3 has "no temp-folder record anywhere" is false: scratch runs-r8b/ug-r8b-build-tests-3.tempdir.json exists (21:01:40, "/home btrfs"). Fix: cite the receipt at 1016, and name the runs-r8b record for line 1115.
- **f4 (a11).** Brief UG26 (line 303) says "the v3 sweeps up to f9b6bde8 ... the only ones that read the build files". ug-r14z14-citation-sweep-all-1.txt is also v3 and also reads 374 files. Fix: "every v3 sweep from 8c944541 on".

**(c) Statements a reviewer could show false**
- **c1 (material).** Brief lines 21, 278 (UG1) and 334 (UG57) cite docs/receipts/unit-suite-green-20261019/ug-r14z13-doc-claim-checks-1.txt.
  - That file does not exist: the only doc-claim receipts under docs/ are ug-r10, ug-r11, ug-r12z3c and ug-r13z11.
  - Self-review line 3 itself says it was "planned ... and not written before slice Z14".
  - Fix: say no doc-claim check ran at f9b6bde8, and drop the three citations.
- **c2.** Plan 2400 says "the statements "TMPDIR on /home" for runs before slice Z12 rest on the run records `runs-r9/*.tempdir.json` ... `ug-r8b-build-tests-3` has no such record" and "The slice Z12, Z13 and Z14 runs have committed temp-folder receipts." Three things are wrong:
  - nine runs of rounds 5 to 8b have tracked receipts: ug-r5-build-tests-2 and -3, ug-r6-build-tests-1, ug-r7-build-tests-1 and -2, ug-r8-build-tests-1 and -2, and ug-r8b-build-tests-1 and -2;
  - r8b-3's record is in runs-r8b;
  - the r14z12 to r14z14 receipts are still untracked.
  - Siblings: self-review line 94, projected problems item 178 (line 33), handoff line 20.
  - Fix: limit the statement to the round 9 to 13 runs, name the runs-r8b record, and say the three r14 receipts go into the records commit.
- **c3 (moderate).** Brief lines 25 and 26 say "the docs-only commits after 52e6d627 (`git log --first-parent 52e6d627..{HEAD}`)", and the same for 1f51d4b2, each called "the last code head" with no "until".
  - Once {HEAD} is filled, both ranges include every later code slice: 52e6d627..HEAD lists 10 src files.
  - Line 27 calls 6082b116 "the last code head".
  - Fix: bound the ranges (52e6d627..3fe29d1f and 1f51d4b2..142a93f0) and add "until" notes. This removes two {HEAD} tokens, so the launcher's expected count of 19 brace tokens has to change too.
- **c4 (low).** Brief line 37 ("not part of AC-006, AC-008 or AC-TR-MCP-WS-005-001") and UG26 ("AC-TR-MCP-WS-005-001 unchanged since 2026-10-27T22:25:32") name a criterion the store no longer holds under that id. TR-MCP-WS-005 holds only -002; the criterion has been FR-MCP-018 ac-1 since 27.1. Fix: the c6 note.
- **c5 (low).** Plan 1881 and 1957 say "line 790 from `4920a8ea` on". At 288b03c3 the drain wait is at line 821; slice Z4 moved it. Fix: pin each line to its commit.
- **c6 (low).** Plan 1910 says "Secret scan before the records commit: the one of 27.28." 27.28 holds no scan; line 1952 says it is in 27.29. Fix: 27.29.
- **c7 (low).** Plan 1752 says "after the operator's resume (above)", but the resume is recorded below, at 1764. Fix: "(below)".
- **c8 (low).** Plan 253 says "Four criteria are set satisfied only with complete platform-rule evidence:" and then lists five. Fix: "Five".
- **c9 (low).** Plan 161 says "the constructor documents its exceptions"; at f9b6bde8 it documented eight of nine. Fix: say so.
- **c10 (low).** Plan 2227 and 2253 read "27.37 to 56 and 27.38 to 57, fourteenth holistic read finding a1". That credits the 27.38 step to a read that ran inside 27.37. Fix: the order used at 2278.
- **c11 (low).** PR description line 173 presents the 23:08 words as the moment he stopped the removal; memory no-ac-for-environment-setup dates them after the stop. Fix: "stopped ..., and at about 23:08 wrote ...".

**(b) Contradictions**
- **b1.** Plan 2383 says the condition is one "which the code and the tests enforce". Self-review line 26 says "no test asserts the condition at run time", and projected problems item 181 says it is "stated in their documentation rather than checked by an assertion or a control". Fix: say the code's structure meets it and the documentation states it, not asserted (declared, item 181), or close g2.
- **b2.** The b2 fix at plan 997 (one runtime message changed in 433ce191) is contradicted by plan 116 ("documentation comments only"), plan 932 ("the documentation-comment commit") and PR description line 61 ("docs, documentation comments"). Fix: use the 997 wording in each.
- **b3.** Plan 1118's gates-of-record chain goes straight to 6082b116. It skips 62c5882d (1474), 878c366f (1519) and 11406fdf (1748). Fix: insert them, or keep only the final pointer.
- **b4.** Plan 2069 says "27.31 held the check until slice Z6, 27.34's followed". That skips 27.32's and 27.33's D-A lines, which 2114 and 2158 name. Fix: add them.
- **b5.** Plan 1671 ("after the holistic read found them missing") now sits under the bullet about the first read before round 11 (1651), while 1683 gives the finding to the second read. Fix: say "the second read".
- **b6.** Self-review line 3 says the round 14 doc-claim receipt will be written "under the name the brief's UG1 gives", but UG1 says no name is given yet. Fix: name it in UG1, or reword line 3.

**(a) Superseded statements with no note or pointer**
- **a1.** Plan 27.38, which 27.40 says I updated for slice Z14, still names 27.39's values as current:
  - 2342: "(D-A; since slice Z13, after `f9b6bde8`, 27.39)";
  - 2343: "superseded in turn by 27.39: 58 tasks, and AC-006 as appended through slice Z13";
  - 2344: "(since slice Z13, at `f9b6bde8`, 27.39)".
  - Fix: the pointer form.
- **a2.** "8432 now" for AC-006, which has been 9314 since 07:54:53. It appears in:
  - brief line 20 and UG57 (line 334);
  - self-review lines 40 and 61;
  - projected problems item 169 (line 24);
  - PR description line 194.
  - Fix: "8432 then (9314 since the 07:54:53 append)".
- **a3.** Handoff line 20 has three stale points:
  - "classified in `ug-r14z13-timing-sweep.txt`" (the current one is ug-r14z14);
  - its list of AC-006 appends ends at 05:35:27;
  - its dirty-path receipts are named only at 8c944541 and f9b6bde8.
  - Fix: update all three.
- **a4 (moderate).** Plan 2193 (27.33's D-B) says "proved by the owned-instance and clock contracts and bindings in the gates at `1d77a79c`", with no pointer.
  - 27.40's D-B (2403) is "as 27.33 states", so read literally, the 1d77a79c gates prove round 14's criteria, including text appended on 2026-11-06.
  - Fix: "(then; the newest section of 27 names the gates and TRX set of record)".
- **a5.** The D-B chain pointers are incomplete.
  - Line 253: the amendment range stops at 27.38, the pointer covers gates only, and an older 27.30/27.31 step follows it in the present tense.
  - Lines 1518, 1556, 1695, 1765, 1815 and 1861 point to the task count only, not the AC-006 extent.
  - Fix: the 1010 form ("the D-B line of the newest section of 27 names the current chain").
- **a6.** The declared-list ranges are fixed at 27.30 (1486, 1758, 1800, 1844, 1895, 1954, 1994), while 27.31 to 27.40 declare items in place. Fix: "from 27.31 on, each section declares its items in place".
- **a7.** The round 13 order lines never say round 13 ran.
  - 2194, 2228, 2254 and 2279; their pointer leads to 2404, which states only the round 14 order.
  - 2228 also omits the fourteenth read.
  - 2312 says "No fifteenth read runs".
  - Fix: add "round 13 ran and returned DISAGREE (27.38)", and make 2312 "No fifteenth read ran before round 13".
- **a8.** The recurrence list at plan 2203 stops at the twelfth read. It omits the thirteenth, fourteenth and sixteenth reads and 27.40's structural fix. Fix: extend it.
- **a9.** Older head and gate lines with no note:
  - 424, 441 and 500 ("the last code, test or build change");
  - 581 ("v4 is the hostile gate of record");
  - 653 ("the last code head is `ccd9e91f`"), and 732 and 737;
  - 960 ("every gate it names is now the round 8b gate at `3e487b6b`");
  - 962 to 969 (the exit gate set);
  - 1241 ("until round 9 AGREE").
  - Fix: an "until" note or the pointer on each.
- **a10.** Plan 1189's Program.cs:805 claim is shown false at 1210, but 1189 has no pointer to that correction. Fix: add one.
- **a11 (low).** Hostile-gate statements still in the present tense at 675, 692, 733 (contradicted by 1083), 762, 764 and 769. Fix: add "(removed in section 23)".
- **a12.** Siblings of the sixteenth read's a8:
  - plan line citations at 1953, 1986 and 2019;
  - unpinned source lines at 1973 and 2037, which point to other code at 288b03c3.
  - Fix: "when this was written", or pin each to its commit.
- **a13.** Plan 2034 says ac-4 is "now ending ...", but 27.31 appended to it again. Fix: "then ending".
- **a14.** The v2 citation sweeps are said to cover "src, build and tests" with no note that v2 missed the 19 build files the branch adds: lines 2051, 2092, 2102, 2148, 2182 and 2225. Line 1992 also lacks the a9 history note. Fix: add both notes.
- **a15 (low).** The brief's line 21 list of temp-folder receipts stops at f9b6bde8. Fix: add ug-r14z14-final-linux-buildtests-1.tempdir.json.
- **a16 (low).** The brief chains at UG47 (line 324) and UG46 (line 323) stop early, though their claims hold. Fix: end each with a pointer.
- **a17 (low).** Projected problems item 145 still describes the forward-step copies, and item 178 never mentions the 27.40 pointer remedy. Fix: a forward note.
- **a18 (low).** Plan 2355 (27.39) says "58 tasks, none checked" without the count pointer that 2333 gained. Fix: add it.

**(d) Missing disclosures**
- **d1.** 27.40's slips (lines 2406 to 2409) are missing from:
  - projected problems item 166;
  - the PR description's slice Z14 declared list (line 458);
  - the self-review slip list, where line 109 omits 2408 and 2409;
  - self-review POWERSHELL-ONLY line 72, which omits the 2409 Bash slip (ls, wc and head run through Bash with a pipe, `&&` and a glob).
  - Fix: add them.
- **d2 (material).** PR body lines 5 and 81 ("Every open and declared item is in the description file's section") are still false. The description lacks:
  - item 181's decision (no control for a pool that removes the sentinel and then suspends; the condition documented, not asserted) and its reviewer counter;
  - item 179's reviewer note;
  - item 161's HandoffDurabilityTests note;
  - plan 2401's d6 declaration.
  - Handoff line 20 also omits that the condition is not asserted.
  - Fix: add them, or narrow the body sentence.
- **d3 (the question you added).** The self-review POWERSHELL-ONLY list (lines 72 to 80) stops at the 08:17:40 analyzer call. It leaves out:
  - Copy-HvTrxEvidence-r14z14.ps1, written 08:19:28. Plan 2398 and self-review line 81 call it analyzer clean, with no analyzer time.
  - The slice Z14 Linux traceability wrapper, written 08:54:04. Line 77 says only "the gate wrappers written after that listing are not in it".
  - records-r14 Show-LineChunks.ps1 (08:18:24), Test-ReplacementsApplied.ps1 (08:42:30) and Measure-LineEnds.ps1 (09:07:20).
  - The plan-history subagent's seven scripts under plan-history-r14 (08:13:47 to 08:38:12).
  - Line 74's "add none to the scratch folder or records-r14" is now stale.
  - Fix: list each script with its analyzer result and time, or declare the ones never analyzed.
- **d4.** Siblings of the sixteenth read's d6:
  - PR description lines 84 and 85 call two different reads "the holistic read before round 11"; the fix was made in the plan only.
  - The round 10 holistic read (plan 1575 to 1576) has no findings receipt, and no record says so.
  - Fix: name both reads, and declare the missing receipts.
- **d5 (low).** PR description line 445 states the round 14 launch conditions without the gates at 288b03c3 or this read and its fixes. Fix: use the list at plan 2404.
- **d6 (low).** Two current receipts go uncited:
  - brief line 20 does not cite ug-r14z14-store-compare-z13-z14.txt;
  - line 75 states the runner match without citing ug-r14z14-final-win-lane-1.start.txt, where it is recorded.
  - Fix: cite both.
- **d7 (my judgment of the a1 pointer form).** The form is accurate today: 27.40 states the head, 59 tasks, the gates, the TRX set, D-A, D-B and the sweeps, so each pointer resolves. But its rule is never stated.
  - Every "the newest section of 27 ..." pointer stays true only while each newest section restates all of those.
  - A records-only section, for this read or for the round 14 verdict, would make every pointer false at once.
  - Line 2383's "every older chain" also does not hold for six bounded notes that have no pointer: 1746, 1814, 1830, 1840, 1841 and 1859.
  - Fix: state the rule once in 27.40, and record this read inside 27.40.

**(e) Brief claims the evidence cannot support**
- **e1 (material).** UG58 (line 335) says "the constructor documents the NullReferenceException a null loggerFactory raises on Windows, so its exception list covers all nine required arguments". UG57 (line 334) says the same. Both are false (g1). Fix: restate once g1 is resolved.
- **e2.** UG58 says e1 and g2 "are closed by a criterion and a documentation slice, not a declaration". But the condition is declared, not asserted (self-review line 26, item 181). Fix: say it holds by the code's structure, is documented and is declared, or close g2.

**Checked and holding (not counted)**
- 288b03c3: parent f9b6bde8, 3 files, signed line present, origin and the Windows child branch at it.
- The diff is documentation plus exactly the two sentinel assertion messages, and ug-r14z14-comment-only-diff-1.txt matches git.
- The no-await condition is true in the code at 288b03c3.
- Every changed member has a new dated entry with the earlier ones kept, apart from g1's wrong content and f1's untouched summary.
- BDPv4 order: the AC-006 append (07:54:53) and task 59 (07:55:05) come before the earliest slice Z14 receipt (08:01:22); that order rests on the transcript, as 27.40 says. Then compile 08:03:31, binding 08:06:08, commit 08:07:46, fast-forward 08:08:04 and gates from 08:08:27. No mock gate was due.
- AC-006 is 9314 characters, 7 criteria, not set. TODO has 59 tasks, none checked.
- Every cited ug-r14z14 hash prefix matches its file. The TRX manifest has 34 entries, all at head 288b03c3.
- PR 87 is still draft (convert_to_draft at 02:47:11).
- The PR body is 23,397 characters, under the 65,536 limit.
- The only FILL tokens are FILL-READY and FILL-RECORDSHEAD, and the brief has 19 brace tokens.
- The four operator quotes are verbatim. The c1 fix quotes the 23:08 words exactly as memory no-ac-for-environment-setup gives them.
- The commit message repeats the g1 claim and gives the task 59 order without the transcript limit, but 27.40 line 2385 discloses that limit.

**Disclosed**
- I edited no tracked file and ran no builds or tests.
- Everything I wrote is in holistic-r14c/:
  - Show-CumulativeChars.ps1 (analyzer clean);
  - subagent-common-r14c.md;
  - pattern-docclaim.txt;
  - turn-r14c-1.psd1 to turn-r14c-4.psd1;
  - response-r14c.txt;
  - each helper's folder.
- Helper p1 disclosed one Bash call with a backtick inside a quoted argument. Bash refused it before anything ran.
