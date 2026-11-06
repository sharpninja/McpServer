# Eighteenth holistic read before HV round 14 (subagent hand-back, exported by ClaudeCode; the harness refused the subagent's own findings file)

2026-11-06T14:34:02 Eighteenth holistic read before HV round 14 (read only)

**Ids:** session ClaudeCode-20261102T182906Z-plugin-session, request req-20261106T190000Z-holistic-r14d.
- Turn opened at 13:26:28 and completed at 14:33:40, with 5 actions and 4 decisions. Every plugin call succeeded.
- Eight read-only helpers each opened, logged and completed their own child turn, req-20261106T190000Z-holistic-r14d-h1 to -h8:
  - h1 to h5 read plan lines 1 to 512, 513 to 1206, 1207 to 1724, 1725 to 2024 and 2025 to 2317;
  - h6 read the projected problems;
  - h7 read the PR description and the PR body;
  - h8 read the self-review.
- I read these in full myself:
  - plan 27.38 to 27.41, the brief, the handoff and the seventeenth findings file;
  - the 288b03c3..548b1a28 diff and its commit message, and the changed code;
  - AC-006, MEMORY-HV-001 v4 and MEMORY-BESTPRACTICE-001 v2.
- The harness refused holistic-r14d/findings-r14d.md ("Subagents should return findings as text"). It also refused every helper's findings.md, so this report is the only full copy. A summary is in holistic-r14d/response-r14d.txt.

**Counts:** 74 findings: (a) 3, (b) 14, (c) 24, (d) 27, (e) 0, (f) 2, (g) 1, (h) 3.
- Material: G1.
- Moderate: C1, C2, C3, C24, D1, D2, D3, D11, F1, H1.
- All others are low.

**(g) Slice Z15 against AC-006, MEMORY-BESTPRACTICE-001 section 1 and BDPv4**

- **G1** (material if AC-006's later-change clause is read literally; otherwise a disclosure gap).
  - AC-006 as appended at 10:09:47 requires the check "so that a later change that lets a removal await fails the test instead of leaving it green".
  - The service check only counts execution sessions created through the strategy resolver:
    - `public bool AnyRemovalCanAwait() => _sessionCounter.Created > 0;` (VoiceConversationServiceTests.cs:537);
    - D8's `Assert.True(sessionCounter.Created == 0, ...)` (:322).
  - The check rests on two code facts:
    - the resolver is the only source of an interactive session (VoiceConversationService.cs:779, :942 and :1473);
    - a removal awaits only through an interactive session (DeleteSessionAsync :487 to :502; CleanupIdleSessionsAsync :1946 to :1971).
  - A later change that adds any other awaited release to the removal path leaves both bindings green.
  - Plan 27.41 line 2421 ("Honest limit: the check proves no execution session was created; that the resolver is the only source of an interactive session is a fact about the code"), brief UG3 (line 294) and UG59 (line 350) state only the first fact.
  - Fix, recommended (the AC-strengthening option): append to AC-006 what the run-time check must cover, either every await on the trace's removal path or a direct observation that the triggered pass returned. Add a negative control that awaits a non-session resource, then a slice.
  - Alternative fix: declare both facts as the limit in 27.41, UG3, UG59, projected problems item 187, the PR description and the self-review.
- **Checked and holding (BDPv4 order, from the transcript):**
  - AC-006 append 10:09:47 and task 60 10:10:14, both before the first edit at 10:12:43;
  - red mock gate 10:16:30 to 10:16:51; its TRX says "No exception was thrown", so the Z14 trace accepted the pool;
  - trace-check edits 10:18:11 to 10:19:49;
  - green mock gate 10:20:24 to 10:20:45, before the production documentation edits at 10:21:05 and 10:21:08;
  - compile 10:24:06, binding 10:27:04, commit 10:30:42;
  - no Edit or Write into the worktree after 10:30;
  - dated entries are kept on every changed member; new nested types carry their type entry, as in slice Z12 and UG36.

**(f) Seventeenth-read findings said fixed that are not**

- **F1** (moderate; h5).
  - Plan 2413 closes a14 under the history rule ("(a1, a4 to a9, a11 to a14, a18 ...) are closed by it").
  - a14's lines 2051, 2092, 2102, 2148, 2182 and 2225 ("src, build and tests since `9b099716`") were false when written: v2 read only develop's build files (plan 2268 and 2282).
  - Fix: add "[27.41: v2 read only develop's build files; 27.36]" at each of the six lines, and move a14 to the corrected list.
- **F2.**
  - Brief line 29: "slice T (commit 6082b116, the last code head: ...)". Seventeenth c3 asked for until notes here; the ranges were bounded, but this phrase was not.
  - Fix: "the last code head until slice U".

**(h) Departures from MEMORY-HV-001 version 4**

- **H1** (moderate; h6, h7, h8).
  - Rule 2.1 marks a task "completed in the plan and in its TODO". Brief lines 70 and 358 (since 13:25) and plan 2437 say so; 2437 adds the rule 2.3 reopen clause.
  - These records give the TODO step only:
    - projected problems items 167 (line 22), 189 (line 44) and 194 (line 49);
    - self-review lines 43 and 85 (85 rests on brief reads at 12:50:43 and 12:50:51, before the 13:25 edit);
    - PR description line 478 and PR body line 93.
  - Fix: add "and recorded as completed in the newest section of 27 of the plan (rule 2.1)" and the reopen clause in each, and note the 13:25 brief edit.
- **H2** (h6).
  - Projected problems line 44, "and these bind round 14: rules 1.1 and 1.2 ...; and 6", leaves out rules 2.5 and 5.1 to 5.8. The brief cites rules 1 to 4 and 6 only (lines 19 and 352).
  - Fix: "rules 1 to 6 bind round 14", or name rules 5.1 to 5.8.
- **H3.**
  - Plan 2427, in the current section: "Dated XML entries per MEMORY-FACT-008, earlier entries kept." That memory is removed; the 548b1a28 commit message says the same and cannot change.
  - Fix: add "(the rule when committed at 10:30:42; since 2026-11-06 MEMORY-BESTPRACTICE-001 section 1)".

**(c) Statements a reviewer could show false**

- **C1** (moderate).
  - Brief line 352 dates all three later replies "about 11:05 local", the confirmation "This is correct.  An HV mid way through a plan ..." included.
  - Brief line 61 has "his later replies of about 11:05 on regressions, the requirements matrix and out-of-scope tasks". PR description lines 222 and 452 and projected problems line 44 say the same.
  - The transcript shows two replies typed at 11:06:01 (line 165907) and the confirmation at 11:13:05 (line 165981), after version 3. The error starts in the memory's own Sources.
  - Fix: "the first two at 11:06:01, the confirmation at 11:13:05 (the memory's Sources say about 11:05)".
- **C2** (moderate).
  - Plan 2421 says the resolver fact is "stated in the criterion's example and the documentation"; brief UG59 (line 350) and PR description line 486 say the same, and handoff line 20 "which the criterion and the documentation state".
  - AC-006's example is "(for example that no dependency handed it a resource whose release awaits)"; it states no such fact. Brief UG3 (line 294) says "stated in the documentation", so the brief contradicts itself.
  - Fix: "stated in the test documentation; the criterion's example names the kind of check".
- **C3** (moderate).
  - Plan 2442 (the slip) says "The service adapter's `AnyRemovalCanAwait` (binding code) was written with the interface before the red mock gate". Plan 2426 places the counting decorator under Bindings, after production. UG59, PR description 226, PR body 26 and the commit message declare only the adapter, while plan 2423 says "AnyRemovalCanAwait and its counter".
  - Transcript lines 164203 to 164206 (10:15:19 to 10:15:37), all before the red gate at 10:16:30, wrote:
    - CreateService's sessionCounter parameter and the CountingStrategyResolver registration;
    - the pool's _sessionCounter wiring;
    - AnyRemovalCanAwait;
    - the ExecutionSessionCounter, CountingStrategyResolver and CountingStrategy classes.
  - Both mock gates pin that file at E7CEE77D.
  - Fix: declare the full pre-red binding code in all these records and in projected problems item 188, and correct the step order at 2426.
- **C4.** Plan 2414 says the seventeenth export has "191 lines"; the file has 193 (a two-line header and 191 report lines). Fix: "191 report lines under a two-line header".
- **C5** (h8). Self-review line 79 says the gate checks policy 7.9's "three sentences". Invoke-HvRound.ps1:92 checks only the second and third; the manifest covers the first. Fix: say so.
- **C6** (h4).
  - Brief line 48 has ""Codex limit is reset" (13:09)", as do plan 1761 and 1766. It was enqueued at 13:08:27 and absorbed at 13:08:49, and the neighbouring stamps mix bases.
  - Fix: one stated basis in the brief, and brackets at 1761 and 1766.
- **C7** (h1). Plan 3, 169, 173, 182, 197, 200, 201, 217, 218, 221, 230, 248 and 250 date operator decisions by the question, not the answer:
  - 14:50 answered 14:51:19;
  - 16:22 answered 16:24:19;
  - 16:19 answered 16:22:12;
  - 17:03 answered 17:04:46;
  - 18:01 answered 18:20:06;
  - 18:47 answered 18:54:10;
  - 19:41 answered 19:42:49.
  - Fix: bracketed "asked ..., answered ..." corrections.
- **C8** (h1). Plan 343 says "six SQL Server test classes"; four open LocalDB, as line 351 says. Fix: "four".
- **C9** (h1). Plan 106 says "in 11 test files"; git shows 10. Fix: "10".
- **C10** (h2). Plan 724 and 823 say "every local gate runs alone". The hostile locale run (20:04:11 to 20:08:12) overlapped ug-r8b-download-mockgate-1 and -2 (20:06:29 to 20:07:51). Fix: a bracketed slip in 22.7.
- **C11** (h2). Plan 1064 and 1120 say "4 lane selection files"; two of the four belong to the Test runs. Fix: "4 selection files (two Test runs, two lanes)".
- **C12** (h2). Plan 1137 says "every earlier finding closed PASS or stay OVERRIDDEN"; round 1's F5 went from PASS to OVERRIDDEN in round 8b. Fix: say so.
- **C13** (h2). Plan 1106 and 1107 mix decimal (13.42 GB) and binary (3.28 and 12.35 GB) units. Fix: one unit.
- **C14** (h2). Plan 655 says the gates "run on a clean tree"; ug-r7-build-tests-2 and -traceability-2 (and the r6 and r7 -1 runs) list the two bench files as modified. Fix: brackets at 655 and 585 to 586.
- **C15** (h3). Plan 1665 says "The five cases ... through `StampedTimeContractTests.AssertStampsAsync`"; there are six cases, and only two use AssertStampsAsync. Fix: h3's bracket.
- **C16** (h3). Plan 1530 ("31 projected problems") and 1603 ("44") give the highest item number, not the counts (29 and 28). Fix: the counts.
- **C17** (h3). Plan 1671 says "written retrospectively at 10:26"; the times are 10:26:55 and 10:27:06. Fix: the measured times.
- **C18** (h4). Plan 1737, 1782 and 1840 say "position 1", which is the zero-based index; it is the second criterion of 12. Fix: a bracket.
- **C19** (h5).
  - Plan 2100 and 2122 say slice W placed the hung-save case in the lane; git shows 765490a9 did. Current siblings: projected problems lines 64 and 71.
  - Fix: name 765490a9 in both places.
- **C20** (h5). Plan 2100's "about 0.1 s" holds only for the failing run. Fix: limit it to that run.
- **C21** (h5). Plan 2103 and 2139 say "eight passing lanes", which includes the failed ug-r13z4c; projected problems line 74 repeats it. Fix: "lanes whose SQL Server cases all passed".
- **C22** (h5). Plan 2046's times 15:30:16, 17:43:48 and 19:40:53 are from 2026-11-04, not 11-02. Fix: add the date.
- **C23** (h5). Plan 2293 credits both PATHNORM records with three changes; only TEST holds the last two. Fix: name each change with its record.
- **C24** (moderate).
  - Plan 2413, the history rule, says "every statement in those sections records what was true when it was written".
  - 27.41's own 22 brackets contradict it (for example 1189's "[27.41 ... a10: false ...]"), and so do C7 to C23 and F1 above.
  - Fix: "records what I recorded as true when written; where a bracketed [27.x] correction or a later section says it was wrong, that governs; the newest section of 27 supersedes any current value an older line names".

**(b) Contradictions**

- **B1.** The dirty-path check at 548b1a28 has four times across the records:
  - plan 2432: 12:41:21, no receipt;
  - brief line 74: "12:41:21 and again at 13:00:13";
  - PR description 234 and PR body 41: 13:03:18;
  - self-review 101 and projected problems line 46: 13:04:17.
  - Fix: one wording naming each check, saying none wrote a receipt, and pointing to ug-r14z15-doc-claim-checks-1.txt.
- **B2.** Windows Build.Tests is "11:44:54 to 11:45:26" in PR description 240 and body 47 (the TEST lines), but "11:44:53 to 11:45:27" in plan 2431 and brief line 80 (START and END). Fix: one basis.
- **B3** (with a (d) element). Projected problems items 186 (line 41) and 192 (line 47), self-review line 47 and brief line 21 frame task 60's "12:31" note as "later than the 12:30:56 read".
  - Plan 2445 says it is an estimate written before the 12:30:24 write; the transcript has the note in the data at 12:30:00.
  - Fix: "corrected at 12:30:24; the note's 12:31 is an estimate, 36 s late".
- **B4.** Plan 2415 ("every other finding is a record correction"), brief lines 19 and 61 ("its other 47 findings are record corrections") and projected problems line 39 all call the rest record corrections. The same 27.41 lines say a1, a2 (in the plan), a4 to a9, a11 to a14 and a18 are "closed by it, not by edits". Fix: name those exceptions.
- **B5.** Plan 2416 says MEMORY-HV-001 was "added 10:51:35" and reached "version 4 at 11:14:37". The getter says created 10:51:49 and updated 11:14:41, as brief line 12 does. Fix: the store times, or say what the plan's times measure.
- **B6.** Plan 2378: the bracket says "so slice Z14 did not fix item 173", but the line still ends "(item 173 is fixed in slice Z14, 27.40)". Fix: extend the bracket.
- **B7** (h1). Plan 149 says "none of those changed"; QBrainAi.Repl.Core (line 148) did. Fix: "none in slice Z4; Repl.Core comment text later".
- **B8** (h1). Plan 52 says "these five"; line 51 names six. Fix: "six".
- **B9** (h2). Plan 723 ("over 850 test files") and 729 ("825") describe the same tree; git shows 825. Fix: one measured count.
- **B10** (h3). Plan 1518 says "(36 when this section was written ...)", against 1517's 35. Fix: "(36 when 27.22 first amended this line)".
- **B11** (h3). Plan 1485's D-A chain skips `878c366f..HEAD` (1531). Fix: add it.
- **B12** (h3). Plan 1557's gates chain skips 11406fdf, ec58cf4a, a4e8d45f and 121992b3. It is a sibling of seventeenth b3, which was fixed only at 1118. Fix: insert them as at 1118.
- **B13** (h5). Plan 2122 says "four Windows lanes ran it near the bound before one failed"; there were three before the fourth failed. Fix: correct the count.
- **B14** (h5). Plan 2104 says "the lanes at `1bb0d61f` and `b79fd51f` passed"; 2098 says b79fd51f failed. Fix: "the SQL Server cases passed".

**(a) Superseded statements in current documents**

- **A1** (h6). Projected problems line 33 counts "the three slice Z12 to Z14" temp-folder receipts. ug-r14z15-final-linux-buildtests-1.tempdir.json is the fourth. Fix: add it.
- **A2** (h8). Self-review line 29 and brief UG3 (line 294) cite ":780 and :943 at 288b03c3"; at 548b1a28 the lines are :779 and :942. Fix: add the 548b1a28 lines.
- **A3** (h8). Self-review line 42's stale sweep (11:09:41, "66 matches on 31 lines") predates the later edits; a re-run at 13:48:10 gives 75 matches on 34 lines. Fix: re-run and restate the counts with the time.

**(d) Missing disclosures**

- **D1** (moderate; h7).
  - PR description 234, PR body 41, brief line 74 and plan 2432 conclude "so the tested tree is the committed code head". The receipts list the tree at write time, not run time (plan 1944):
    - unit receipt written 10:46:23 for a run that ended 10:44:13;
    - lane receipt written 11:46:39 for a run that ended 11:21:25;
    - Build.Tests receipt written 12:13:46 for a run that ended 11:51:49.
  - Fix: declare this, and add that no commit follows 548b1a28 and the transcript shows no worktree Edit or Write after 10:30.
- **D2** (moderate). 27.41's slips (2440 to 2446, with the delegated slips at 2415 and 2433) are only partly in projected problems item 188 (line 43), PR description 486 and self-review 131. The 12:31 estimate and the transcript-search slip are missing from all three. Fix: list them, or scope the memory-work slips as lab work.
- **D3** (moderate; h6). Projected problems item 187 (line 42) states "the only source of an interactive session" with no limit; items 172 and 174 call the condition checked at run time. Fix: add the limit, with G1's second fact.
- **D4** (h6, h3).
  - Before the sync, ids meant different rules on the Windows store:
    - MEMORY-FACT-008 was the unit-versus-integration rule;
    - MEMORY-PROCESS-013 was the NuGet rule;
    - MEMORY-PROCESS-014 was the accountability rule;
    - the infrastructure rule was MEMORY-PROCESS-011.
  - Plan 1470, 1471, 1498 and 2416, brief line 12 and projected problems line 48 give each id one meaning.
  - Fix: one 27.41 sentence naming the store.
- **D5** (h6). records-r14/pattern-r14z15-current.txt contains "\b60\b", which matches "60 s" durations, with no note. Fix: the Z15 counterpart of item 178's note.
- **D6** (h8).
  - Self-review POWERSHELL-ONLY (lines 86 to 96) stops at 10:49:34 but still claims "every script written or run this round". Missing:
    - an analyzer time for Copy-HvTrxEvidence-r14z15.ps1;
    - records-r14/Get-LineEndingCount.ps1;
    - eleven memory-work scripts.
  - Fix: list or scope them.
- **D7** (h8). Self-review lines 18, 36 and 41 omit turn req-20261106T183100Z-r14-records-hv001. Neither the self-review nor 27.41 records the 13:25 brief edit. Fix: name both.
- **D8.**
  - Plan 2417 says "the merged sources" are archived under profile-archive/20261106T1140-global-memory-removals/. The folder (16 files) has no copy of MEMORY-HV-001's sources, MEMORY-PROCESS-010 v2 and MEMORY-PROCESS-013 v1; only scratch memory-process-010-v2.txt (10:36:11) exists.
  - Fix: say where they are, or archive them.
- **D9.** Plan 2420 and the commit message say task 60 came "before the slice Z15 edits" without saying the order rests on the transcript (brief line 21 does). The order holds: the first edit was at 10:12:43. Fix: add the note.
- **D10.** No record declares that the pushed 288b03c3 commit message keeps the false NullReferenceException claim, as UG47 does for 4398cffc. Self-review line 3 relies on "the slice Z14 commit message". Fix: one declaration.
- **D11** (moderate; h1). Plan section 10 omits six runs:
  - unit-green-reclass-20261019T1742 (exit 255, xUnit2008);
  - the dirty-tree runs T1745 and unit-green-desktop-20261019T1824;
  - unit-green-g-buildtests-20261019T1635 (422/421/1);
  - unit-green-linux-f-compile-20261019T1546 (exit 255);
  - unit-green-r2-sandbox-test-20261019T2115 (exit 2).
  - Fix: one bullet declaring them, in line 238's form.
- **D12** (h1). Plan 187 marks the ZERO-platform words "Operator, verbatim", but they came as a relay (15:54:28), confirmed at 15:59:56. Fix: say so.
- **D13** (h1). Section 6 (blast radius) has no slice Z15 entry. Fix: a pointer line.
- **D14** (h1). The 27.41 rule's list omits the HV rule set and the TODO done condition (lines 3, 89 and 470). Fix: add them.
- **D15** (h1). Run id unit-green-h-mockgate-20261019T1849 started at 18:55:03. Fix: give the start time.
- **D16** (h2). Plan 849 never records the TR-MCP-NUKE-019 AC-2 and AC-1 writes at 11:52:24 and 11:52:35. Fix: a bracket in 22.6.
- **D17** (h2). Plan 891's four v6 runs "in progress" are never closed; no round 8b hostile run has a committed receipt. Fix: a close-out with a scratch-only note at 891, 910 and 948.
- **D18** (h2). Five receipted runs (ug-r8b-laneprojects-mockgate-1 and -binding-1, ug-r8b-precommit-lane-c1 and -b1, ug-r8b-citations-buildtests-1) and 433ce191's compile order are not named. Fix: brackets at 22.2 or 22.6, 22.5 and 24.
- **D19** (h2). Plan 1011 cites a scratch receipt without saying it is not committed. Fix: the full path and that note.
- **D20** (h2). Sections 23 and 24 do not record the writes of tasks 26 and 27. Fix: one bracket each.
- **D21** (h3). Plan 1323's slice order (executed K, L, I, J, M, O, N) has no note. Fix: a bracket.
- **D22** (h4). Plan 1938: ug-r12z3-binding-1 ran before any Compile. Fix: declare it in the 27.28 slips.
- **D23** (h4). Plan 1925: the second redeploy overlapped the b3f26862 Linux lane, undeclared. Fix: declare it.
- **D24** (h4). Plan 1899 names one "continue working" message; there were two (12:12:09 and 12:12:12). Fix: name both.
- **D25** (h5). Plan 2100's nine-lane duration scope is not declared. Fix: declare it.
- **D26** (h5). Plan 2031 does not say that the 15 hv-ug-r12-*.ps1 scripts are ignored by .gitignore:527. Fix: add it.
- **D27** (h5). Plan 2135 does not say when the g1 sweep ran; its receipt (12:26:18) postdates the slice commit. Fix: state the time.

**Checked and holding (not counted)**

- **Plan and placeholders:**
  - the plan's hash is C06FBD16 and it holds 22 "[27.41," brackets;
  - the brief holds 17 launcher brace tokens;
  - only FILL-READY and FILL-RECORDSHEAD remain;
  - no record cites a brief hash, so none is stale.
- **Operator quotes:**
  - every quoted operator span is verbatim against MEMORY-HV-001's Sources and the transcript;
  - "byte-for-byte on global memories" (11:49:17) and "That means both machines ..." (12:15:21) are his typed words.
- **Store, TODO and receipts:**
  - AC-006 is 10022 characters, 7 criteria, not set;
  - the TODO has 60 tasks, none done, equal to the snapshot of record;
  - the manifest 6A00AD7B lists 34 entries, all at 548b1a28;
  - every ug-r14z15 hash prefix matches (h6, h7, h8);
  - the handoff's top section is current.
- **Seventeenth-read findings:** the rest are fixed where 27.41 says, or closed by the rule, except F1 (a14) and F2 (part of c3).

**Disclosed**
- I edited no tracked file and ran no build or test.
- My scripts are under holistic-r14d/; each was analyzer clean before running.
- Find-TranscriptEdits.ps1's first run printed times shifted by six hours (ConvertFrom-Json turns the timestamp into a DateTime). I fixed and re-analyzed it before the run I cite.
- I reused h1/Find-QuoteOrigins.ps1 unchanged.
