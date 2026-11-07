# Sixth holistic read before hostile validation round 12

Read-only subagent turn req-20261105T022815Z-holistic-r12f (parent req-20261104T231251Z-continue-pr87-completion-2) in session ClaudeCode-20261102T182906Z-plugin-session. Its final report as received, exported from the session transcript by Export-SubagentReport.ps1, with the harness indentation removed; the harness refused its findings file.

2026-11-04T20:51:58 local (from Get-LocalStamp.ps1). This is the sixth holistic read before hostile validation round 12. It was read only and changed nothing tracked.

**Session log:** session ClaudeCode-20261102T182906Z-plugin-session, request req-20261105T022815Z-holistic-r12f. I opened the turn, patched it twice and completed it through the plugin scripts. The readback shows status completed, 9 actions and 4 decisions. The harness refused holistic-r12f/findings-r12f.md ("Subagents should return findings as text"), so the full findings are below. It accepted the response file holistic-r12f/turn-response-r12f.txt, which Complete-Turn used.

**Counts:** (a) 8, (b) 2, (c) 4, (d) 3, (e) 2, (f) 2, (g) 3, so 24 in all. The material ones are f1, g1, a1, a2, e1 and c2. Under the stored text, the slice Z3 part 3 recovery meets failed-update-startable for the six layouts its rows state. g1 and g2 are what-if gaps outside those rows.

## (g) Slice Z3 part 3 build code, and the declared gaps of the startup wait

**g1. failed-update-startable is proved only for layouts where the legacy name sits in an ExecStart line, but the code rewrites every occurrence of the name.**
- **What the code does.** `RetargetLegacyExecStart` (LinuxServiceHelper.cs:361-389) decides from the effective ExecStart path, read before the update. It then runs `text.Replace(LegacyAppHost, AppHost)` over the whole text of the fragment and of every DropInPaths file. The recovery now calls it after the restore (:163-164).
- **What-ifs with no contract row and no binding:**
  1. A legacy name in a non-ExecStart line is rewritten too (Description, Environment, EnvironmentFile, ReadWritePaths). If the install folder's path holds the name (for example /opt/McpServer.Support.Mcp), WorkingDirectory and the ExecStart folder are rewritten to a folder that does not exist. The unit is then not startable on either path.
  2. Two drop-ins that both name the legacy apphost. The code loops over all of them, but the rows and the binding use at most one drop-in.
  3. A fragment whose legacy ExecStart is overridden by a drop-in that names the current apphost. The code returns false and leaves the fragment's legacy text. The stored text says "a unit file or drop-in that names the legacy apphost ... is retargeted".
  4. A unit left at systemd's start limit. The manual start ran `reset-failed` (service-start log line 16); the recovery does not.
- **Why it matters.** 27.29 D-B says the criterion is "covered row by row". `ServiceRecoveryContractTests.Rows` (lines 179-193) holds only ExecStart lines and at most one drop-in.
- **Fix, criterion first.** Strengthen the criterion to say which text is retargeted: the effective ExecStart executable path `<install>/McpServer.Support.Mcp`. Say how an overridden fragment and several drop-ins count. Then add these rows:
  - a Description or EnvironmentFile line that must stay unchanged;
  - two legacy drop-ins;
  - an overridden legacy fragment.
  - Alternatively, declare the four what-ifs in 27.29 and UG46.

**g2. A recovery-step failure after a successful restore is reported as a restoration failure, and no row covers it.**
- If the retarget throws (:386-387) or daemon-reload fails (:164), the catch at :166-169 reports "Update and live-state restoration failed", although the restore completed.
- At the runtime-configuration failure point, systemd still holds the old legacy unit. A failed reload therefore leaves a unit that will not start until someone reloads by hand, and the message does not say so.
- The binding uses `Assert.ThrowsAny<Exception>` (ServiceUpdateTests.cs:692), so it does not show that the original failure is the one reported. The command double's daemon-reload always succeeds.
- **Fix:** add rows and bindings for a failing reload and a failing retarget, and assert the exception's type and message. Alternatively, declare it.

**g3. The declared gaps of the startup wait are partly stated wrongly** (27.29 "Declared at 52e6d627", PR body line 223, projected problems item 108, handoff top).
- **"A probe exception other than a refused or timed-out request propagates."** `ProbeHealth` (:252-258) swallows every HttpRequestException (reset, name resolution, protocol and TLS errors) and every TaskCanceledException. A non-success status is also treated as "not yet".
- **"Near 16 minutes."** Each probe also runs `systemctl show` (:198), and RunCommand bounds that only at 15 minutes (:57). So 16 minutes is not an upper bound.
- **Kept wording the declaration does not list:**
  - the StartupHealthProbes summary "10 minutes at the interval" (:178);
  - the WaitForStartupHealth remark "(93 s at 14:27)" (:193), an unreceipted journal reading;
  - in the stored criterion, "93 s on 2026-11-04 at 14:27" and "an 18 s health window".
- **Fix:** restate the declaration to match the code, and add these wordings to the kept list.

## (f) Fifth-read findings that 27.29 calls fixed but are not

**f1. d1: the retrospective receipt for the b3f26862 Linux lane misstates the run's head.**
- **The head field.** ug-r12z3-final-linux-lane-1.json (9D79EAB9) line 6 reads "head": "52e6d627b78df808bb469269f0093d8fc1815701". The run went from 16:04:34 to 16:37:13, but 52e6d627 was committed at 19:37:01. The run's own meta (runs-r9/ug-r12z3-final-linux-lane-1.meta.json line 9) records b3f26862.
- **The dirty paths.** They show the tree at 19:43, not at the run. They list files written after the run ended: ug-r12z3c-mockgate-1.json, -binding-1.json, -store-readback-1.json, the ug-r12z3b-* receipts and the deploy .out.txt copies.
- **No retrospective marker.** The receipt has no "retrospective" field or note.
- **What still holds.** All seven TRX copies match their SHA-256, and their windows (16:05:15 to 16:37:13) fit the run, so the counts are right.
- **Records that state otherwise:** plan 1926 ("from the run's own meta and TRX files"), brief line 80, projected problems items 103 and 112, and PR body line 140.
- **Fix:** rebuild the receipt from the run meta, with head b3f26862, the meta's dirty paths and "retrospective": true. Alternatively, declare the wrong fields.

**f2. f2: the v3 propagation search still passes stale pointers.**
- A sentence with any marker word counts as handled, even when it names a superseded head as current.
- ug-r12z3c-propagation-search-1.txt lists all of these as "marked" with the head4920 or headB3f2 pattern:
  - L234s23 (lines 26 and 27);
  - L235s24 (lines 31 to 33);
  - L1467s6 (line 62);
  - L1513s6 (line 68);
  - L1876s1 (lines 114 to 116).
- These are a1 to a4 below. Self-review line 20 judged only the unmarked sentences.
- **Fix:** fix a1 to a4. Treat a superseded-head hit as unmarked unless its sentence carries a forward note to 27.29.

## (a) Superseded statements with no forward note

- **a1.** Plan section 13 D-A (line 234) ends with: "Superseded again in 27.28: ... 27.28 names the last code head `4920a8ea`, records every gate there, and states the D-A range (its "Docs only after `4920a8ea`" line)." Fix: add a 27.29 sentence naming `52e6d627` and its "Docs only after `52e6d627`" line.
- **a2.** Plan section 13 D-B (line 235) says: "Superseded again in 27.28: as amended by 27.22 to 27.28, and every gate it names is the gate at the last code head `4920a8ea` that 27.28 records." Fix: add the 27.29 note (27.22 to 27.29, the gates at `52e6d627`).
- **a3.** Plan lines 1467 (27.20) and 1513 (27.21) say: "the current one is the "Docs only after `4920a8ea`" line of 27.28 (`4920a8ea..HEAD`)." Fix: name the 27.29 line (`52e6d627..HEAD`).
- **a4.** Plan line 1876 (27.27) says: "slice Z3 changed code in `b3f26862` and then `4920a8ea`, the last code head; the D-A line of 27.28 holds the check." Fix: add that 27.29 (`52e6d627`) supersedes it.
- **a5.** Two declared-list headers lack 27.29:
  - Line 1877 says "(adds to the lists of 27.24 to 27.26; 27.28 adds to all four)". This is the sibling of fifth-read a1.
  - Line 1936 says "(adds to the lists of 27.24 to 27.27)" and has no forward note.
  - Fix: "27.28 and 27.29 add to all of them".
- **a6.** Projected problems item "47 to 62" (line 82) says "which items 74, 89 and 103 supersede (items 73, 90 and 96 are themselves history)". Item 103 is history since item 110. Fix: "74, 89 and 110", and list 103 as history.
- **a7.** PR body line 236 (Slice Z3, parts 1 to 3) says "Build.Tests gains the two new contract classes and the bindings". Part 3 added a third class, ServiceRecoveryContractTests. Fix: "three".
- **a8 (low).** Plan line 1777: the store-readback history chain ends at `ug-r12z3-store-readback-1.json` (27.28). The current one is ug-r12z3c-store-readback-1.json (27.29). Fix: extend the chain.

## (b) Contradictions

- **b1.** Projected problems carried item 5 (line 67) says "MEMORY-FACT-008's dated change logs cover the code of slices Q, ... Z2 and Z3". Item 108 and PR body line 257 say the two Z3 constants have none, and LinuxServiceHelper.cs:175-179 confirms it. Fix: add the same exception to item 5.
- **b2 (low).** The task 45 notice for the health-wait commit is timed two ways:
  - "17:43" in plan line 1924 and brief line 19;
  - "17:42" in plan line 1974 and projected problems item 112.
  - The stored text says "Notice 2026-11-04 17:42", and readback-2 dates the write 17:43. Fix: say once that the text stamp is 17:42 and the write was 17:43.

## (c) Statements a reviewer could show false

- **c1.** Self-review line 35 (NO-NEW-MECHANISM) says "the fixes stay inside CheckTestOutputFolder, the test bounds and LinuxServiceHelper". 4920a8ea also changed build/Build.UpdateService.cs (the call at :284; `git diff --name-only 1f51d4b2 52e6d627 -- src build .nuke` lists both files). This is the sibling of fifth-read c1. Fix: add Build.UpdateService.cs.
- **c2.** Plan 1926 and projected problems item 103 say the b3f26862 lane "ran across the server outage, so it does not depend on the live server". The TRX windows show these ran before the stop at about 16:18:31:
  - Support.Mcp.Tests, 16:05:15 to 16:07:06;
  - McpAgent, Repl.Core and QBAgent, 16:07;
  - Build.Tests, 16:07:32 to 16:11:48;
  - Repl.IntegrationTests, from 16:11:56.
  - Only the cases after 16:18:31 ran during the outage. Fix: narrow the claim to those cases, or drop it.
- **c3.** Projected problems item 102 places "Then TR-MCP-SERVICEUPDATE-001 gained two criteria ... (repeated-properties 15:29:35, ...)" after the 16:51:06 start. repeated-properties was written before the second run (16:16:44). Fix: order the two writes correctly.
- **c4.** The stored task 45 notice reads "Notice 2026-11-04 19:40" (ug-r12z3c-store-readback-1.json line 437). Plan 1966, the readback note and brief line 19 date that write 19:39:20. A stored stamp later than its own write is not a measured stamp. Fix: disclose it in 27.29's slips.

## (d) Missing disclosures

- **d1. No citation sweep covers the test lines slices Z to Z3 added.** The newest sweep is ug-r12y-citation-sweep.txt, at ec58cf4a. The new lines cite TR-MCP-USECASE-018 AC-006, TR-MCP-SERVICEUPDATE-001 and TEST-MCP-SERVICEUPDATE-001 (ServiceRecoveryContractTests.cs:109). UG7 and UG26 still claim full coverage. TEST-MCP-SERVICEUPDATE-001 does appear in traceability-reconcile-20261102T111536.json. Fix: save a sweep at 52e6d627, or declare the gap.
- **d2 (low).** ug-r12z3c-repl-tool-version-1.txt line 5 shows the tool shim was written 2026-11-04T12:07:16. The records give only "written 2026-10-06" (the package and DLL dates). The conclusion still holds. Fix: state both dates.
- **d3 (low).** Plan line 1908 omits `reset-failed` from the manual start, which the receipt shows (line 16). Fix: add it.

## (e) Brief claims the evidence cannot support

- **e1.** Brief line 49 says "(the D-A line of plan 27.29; plan section 13 points to it)". Section 13 points to 27.28 and names 4920a8ea (see a1).
- **e2.** Brief UG45 (line 192) says "every changed member and test class carries the dated change log MEMORY-FACT-008 requires". The two constants have none: 27.29 declares fifth-read finding c7, but that declaration never reached the brief. Fix: add the exception.

## Checked and consistent

- **Git.**
  - HEAD, origin and the child branch are all 52e6d627, with no commit after it.
  - 4920a8ea..52e6d627 touches 3 paths.
  - The diff touches only `Update` and `RetargetLegacyExecStart`. The success path keeps its behavior, and the Windows path is unchanged.
  - The commit message carries the Signed line.
- **BDPv4 order.**
  - The criterion was written at 19:26:23.
  - The mock gate ran 19:29:09 to 19:29:18. Only the contract was untracked, and no tracked file was changed.
  - Its pinned hashes match: 25BE649B (LinuxServiceHelper.cs) and AF49839B (ServiceUpdateTests.cs) equal the 4920a8ea blobs, and 7EF7B33F equals the contract at 52e6d627.
  - The compile ran 19:31:21 to 19:34:17.
  - The binding ran 19:35:03 to 19:35:24 with 97/0/0, and its hashes equal 52e6d627.
  - The commit is at 19:37:01.
- **Tar double.** The command double's tar snapshot really restores the unit files. Dated change logs are present on `Update`, `RetargetLegacyExecStart`, the contract class and the binding.
- **Receipts.** Every hash prefix the records cite matches. Gate counts and times match. The by-name comparisons give 4739, 739 and 716 common names, and the same 14 new Build.Tests names on both hosts (563 and 467).
- **TRX manifest.** It has SHA-256 8C969A27 and 34 files, 30 of them TRX. Every head is 52e6d627, and there are 0 problems.
- **Store.** failed-update-startable holds the quoted text. Only position 7 is new, and the TODO has 46 tasks, none checked.
- **Deploy logs.** They match the plan, including archive 20261104T221831196Z and the third run's 13 probes, 1.4.43-pr87.4920a8ea, PID 718431 and archive 98F0688C.
- **PR 87.** It is open and not draft, at head 52e6d627, with one ready_for_review event, 2026-11-03T15:59:32Z.
- **Formatting.** None of the six records has an en dash, an em dash or a table row.
- **Fifth-read findings handled as 27.29 says:** c1 to c6 (c7 declared), d2 to d6, e1, e2, a1 to a3, b1, f1, f3, f4 and g1. d3 is fixed in slice Z3 part 3.

## Process slip of mine

One call of the read-only holistic-r12e/Find-Lines.ps1 wrote r12f-sut-lines.txt into the holistic-r12e folder, because that script's output folder is fixed. That is outside my folder. I did not delete it, and every later search used copies in my own folder.

## Files

My scripts and outputs are in /home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r12f/. All the scripts are read only and were analyzer clean before they ran:
- Get-GitFactsF.ps1
- Find-LinesF.ps1
- Get-BlobHashesF.ps1
- Get-ReceiptSummaryF.ps1
- Test-ManifestF.ps1
- Test-ReceiptTrxF.ps1
- Find-InFolderF.ps1
- Get-PrStateF.ps1
- Test-DashesF.ps1
