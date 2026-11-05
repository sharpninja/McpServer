# Fifth holistic read before hostile validation round 12

Read-only subagent turn req-20261105T002902Z-holistic-r12e (parent req-20261104T231251Z-continue-pr87-completion-2) in session ClaudeCode-20261102T182906Z-plugin-session. Its final report as received, exported from the session transcript by Export-SubagentReport.ps1, with the harness indentation removed; the harness refused its findings file.

2026-11-04T18:55:05 local (from Get-LocalStamp.ps1). This was the fifth holistic read before hostile validation round 12. It was read only and changed nothing tracked.

**Session log:** session `ClaudeCode-20261102T182906Z-plugin-session`, request `req-20261105T002902Z-holistic-r12e`. The turn was opened, patched twice and completed through the plugin scripts. Readback: status completed, 7 actions, 4 decisions. The harness refused `holistic-r12e/findings-r12e.md` ("Subagents should return findings as text"), so the full findings are below. It accepted the response file `holistic-r12e/turn-response-r12e.txt`, which Complete-Turn used.

**Counts:** (a) 3, (b) 1, (c) 7, (d) 6, (e) 2, (f) 4, (g) 1. That is 24 in all. The material ones are g1, c1, c2, e1, f1 and d3. Under AC-006 as stored, no unit case is below the 100 times margin.

## (g) The slice Z3 build code against its criterion (material)
- **g1. TR-MCP-SERVICEUPDATE-001 startup-health-wait is not met as stored and not proved whole.**
  - Stored text (ug-r12z3-store-readback-2.json line 51): "keeps checking HTTP health while the unit stays active, for up to 10 minutes ... it fails as soon as the unit leaves the active state or the 10 minutes are spent, and never stops a unit that is still starting within them."
  - **Time bound.** `EvaluateStartupHealth` (LinuxServiceHelper.cs:204-218) counts 120 probes, not elapsed time. `ProbeHealth` allows each probe 3 s (:232-238), and each probe also runs `systemctl show`. A server that accepts connections but stalls makes the wait about 16 minutes (120 x 3 s plus 119 x 5 s). So "up to 10 minutes" (PR body line 221, self-review line 31, Build.UpdateService.cs:231) is false in that case.
  - **No test binds the 10 minutes or the call.** `LinuxStartupHealth_WaitsAsTheContractRows` passes each row's MaxProbes and a literal 5 s. No test reads `StartupHealthProbes`, `StartupHealthInterval` or `ProbeHealth`. No test covers the call at Build.UpdateService.cs:284. A 12-probe constant or a deleted call would pass every Build.Tests case.
  - **Unit states.** Any ActiveState other than "active" (:209) ends the wait, including "activating" (an auto-restart or a Type=notify start) and "reloading". The catch at :141-145 then stops the unit. That conflicts with "never stops a unit that is still starting".
    - The self-review (line 15) gives this reading "as the criterion says".
    - No contract row has activating, reloading or deactivating, so a wrong wait that accepts them passes all six rows.
  - **Exceptions.** A failing `systemctl show`, or a probe that throws something other than HttpRequestException or TaskCanceledException, propagates. The failure path then stops a unit that may still be starting. No row covers it.
  - **Fix.** Strengthen the criterion first; that is the operator's decision, and strengthening is the recommended option. It should state the deadline as elapsed time and how activating, reloading and a failed state read count. Then:
    - add rows on a scripted clock where probes take time;
    - add rows for activating and reloading;
    - add rows for a throwing state read and a throwing probe;
    - bind the deadline or constants, and bind the UpdateLinuxService call through a seam.
  - Otherwise keep the criterion out of D-B.

## (c) Statements a reviewer could show false
- **c1. Plan section 6, line 144.** Quote: "(`git diff 1f51d4b2..<last code head> -- src build .nuke` lists only `build/LinuxServiceHelper.cs`, slice Z3 ...)".
  - Git also lists `build/Build.UpdateService.cs`.
  - Line 147 names only the repeated-properties reader and leaves out the startup health wait. That is a behavior change in the target that deploys the live service.
  - 27.28 line 1935 says fourth-read a1 is fixed ("now naming slices Z2 and Z3 and the build change"), but section 6 names only part 1.
  - Fix: list both files and add the startup-health-wait behavior.
- **c2. The four deploy and start logs are ignored by `.gitignore:108:*.log`** (git check-ignore; none are tracked). Plan line 1910 says they are "in the receipts folder"; brief line 21 lists them among the committed receipts; projected problems item 102 cites them. This is the class of first-read C3.
  - Fix: copy them as `.out.txt`, hash-verified, cite those names, and include them in the secret scan. The 1731 log holds workspace paths and 64-hex values.
- **c3. Projected problems item 85** cites "ug-r12z-doc-claim-checks-1.txt and ug-r12z2-doc-claim-checks-1.txt are history". Neither file exists in docs/receipts or anywhere under the scratch folder (recursive search). The newest such receipt is ug-r11-doc-claim-checks-1.txt.
  - Fix: drop the two names, or write them and label them retrospective.
- **c4. "It listens after about 95 s"** appears in plan line 1907; "the server startup (about 95 s)" in the task 45 notice; "95 s at 5 s, the measured startup" in self-review line 15 and the contract row name.
  - No receipt measures 95 s. service-start-20261104T1646.log shows start at 16:50:21 and healthy at 16:51:06, which is 45 s. The third run answered on probe 13. The second run never listened. The 93 s (14:27, old build) is an unreceipted journal reading.
  - The row's 19th probe comes after 18 sleeps, 90 s after the first probe.
  - Fix: cite the receipted times and label 93 s and 95 s as journal readings.
- **c5. Plan line 1908** says "(what the target's `RetargetLegacyExecStart` does on success)". In the source the retarget runs before daemon-reload and start (LinuxServiceHelper.cs:134-136), and the failure path's Restore (:145) reverts it. Fix: state what the source does (see d3).
- **c6. "Down from 16:18:54 to 16:51:06"** appears in plan 1907, PR body 110, projected problems 102 and self-review 65. The update stopped the old unit before archiving, and archive folder 20261104T221831196Z is 16:18:31 local. Fix: "from about 16:18:31".
- **c7. Dated change logs.** The new `StartupHealthInterval` and `StartupHealthProbes` (LinuxServiceHelper.cs:155-159) have no dated change log, yet brief UG45 says every changed member carries one (MEMORY-FACT-008). Fix: declare it, or add the entries (a code change).

## (d) Missing disclosures
- **d1. The b3f26862 Linux lane has no durable receipt.** It exists only as scratch `runs-r9/ug-r12z3-final-linux-lane-1.*`, with no docs/receipts JSON and no TRX copy. Yet plan 1926, brief 69 and projected problems 103 cite its 741/0/0 and argue from it that the lane "does not depend on the live server". Fix: write a retrospective receipt, or drop the count and the argument ("No recipt is a fail.").
- **d2. The conditional-wait sibling sweep has no saved receipt.** Its turn is `req-20261104T213028Z-z3-conditional-sweep`. Plan 1915, brief 21 and UG45 ("the conditional-wait sibling sweep is complete") and projected problems 101 rest on that session-log turn alone. This is the class of first-read D2. Fix: save its report, or state the limit.
- **d3. The failure path leaves the unit unstartable.**
  - On failure, Restore (:145) puts the unit and drop-ins back with the legacy apphost name, after ReplaceFiles has removed that binary.
  - The service-start log shows `legacyPresent=False` and `ActiveState=failed`, and its Phase 2 had to retarget the unit files again.
  - That, not only "the failure path stopped the unit", is why the server stayed down until the manual retarget. Slice Z3 does not change it, and no triage or TODO names it.
  - Fix: declare it and file triage. Whether to fix it in PR 87 is the operator's decision.
- **d4. Unswept sibling: the Windows update keeps the 18 s window.** It starts the service, then calls `CheckHealth` with its defaults of 10 attempts 2 s apart with a 3 s timeout (Build.UpdateService.cs:193-198, WindowsServiceHelper.cs:340). No record says whether the Windows server's startup fits that window. Fix: declare it as a sibling or as out of scope, with a reason.
- **d5. The Linux Test gate of record overlapped the third redeploy on the same host.** The gate ran 17:31:55 to 17:39:49. The redeploy ran 17:31:53 to 17:36:22: a root `dotnet publish` into /tmp plus a service restart. That departs from the "each local suite alone" practice of 21.5 to 27.21. The overlap is undisclosed; the run itself is green.
- **d6. An unchecked inference.** With 4920a8ea (slice P) on the server, the getters still return read times. That the REPL tool is a develop build stays "not checked" (plan 1945, brief 20, PR body 210), although its version can be read without writing anything. Fix: read it, or state in UG33 that slice P is unverified on any live path.

## (e) Brief claims the evidence cannot support
- **e1. "Proved whole".** D-B (plan 1944), UG12 and UG45 say startup-health-wait is proved whole by its contract and bindings. Per g1, the 10-minute clause, the wiring and the state reading have no test.
- **e2. Receipts for TR-MCP-SERVICEUPDATE-001.** UG45 says the two criteria hold "with the mapping to FR-MCP-SERVICEUPDATE-001 intact (ug-r12z3-store-readback-1.json with ug-r12z3-store-compare-z2-z3.txt, and ug-r12z3-store-readback-2.json)". None of those receipts shows that:
  - readback-1 does not hold the record; its note points to ug-r12z3-criteria-readback-1.json;
  - the comparison reads only FR-MCP-TESTRESOURCE-001, TR-MCP-USECASE-018 and the TODO;
  - no receipt holds the mapping;
  - no before-readback exists for the record's five older criteria, so "only that criterion changed" rests on the transcript.
  - Fix: cite criteria-readback-1 and readback-2, name ValidateTraceability as the mapping evidence, and declare that there is no before-readback.

## (f) Fourth-read findings that 27.28 says are fixed but are not
- **f1. a2 is stale again.** Plan line 1586: "27.28 (slice Z3), which names the current last code head `b3f26862`". The last code head is 4920a8ea.
- **f2. The f1 fix is not done.** Self-review line 17 says "None states a superseded value as current". But ug-r12z3b-propagation-search-1.txt lines 69 to 71 marked L1586s3 as carrying a marker, and no pattern treats b3f26862 as a superseded head. Fix: add that pattern, rerun, and restate the sentence.
- **f3. An e1 sibling.** PR body line 219 still says "Slice Z changes: six unit hang guards raised to 100 times the delay of the step each races", unqualified. Fix: add "measured then as the single largest timer; under the sum three were 94 and 95 times, raised in slice Z2".
- **f4. b1 did not reach the classification (low).** timing-sweep-classification-r12z3.psd1 (DEED8454, the file the r12z3 sweep names) still carries "the five at exactly 100 times say so" at header line 17, with no correction in its r12z3 header.

## (a) Superseded statements with no forward note
- **a1.** Plan line 1826 (27.26) says "(adds to the lists of 27.24 and 27.25; 27.27 adds to all three)" and leaves out 27.28.
- **a2.** Brief line 20: the AC-006 history "... and 2026-11-04 at 12:20:22, 27.27" stops before the 15:29:24 append.
- **a3.** Projected problems item 89 treats ug-r12z2-store-readback-1.json as the latest. It never names ug-r12z3-criteria-readback-1.json, ug-r12z3-store-readback-1.json with its comparison, or readback-2. It does not say the 27.27 and 27.28 writes have no write-time receipt.

## (b) Contradictions
- **b1. The drain's line number.** Plan 27.27 line 1863 says "line 779"; plan 1939, projected problems 106, the self-review and UG44 say "line 790", and none names a head. UG44 applies 790 to the 121992b3 state, where it was 779. At 4920a8ea it is 790. Fix: name the head with each line number.

## Checked and consistent
- **Git:**
  - HEAD, origin and the child branch are all 4920a8ea, with no commit after it.
  - 121992b3..4920a8ea touches 7 paths, 5 in b3f26862 and 4 in 4920a8ea.
  - The build change touches only the Linux update path; the Windows path is unchanged.
  - Commit times are 15:57:01 and 17:30:25, and both messages carry the Signed line.
- **BDPv4 order:**
  - The criteria came first (15:29:35 and 17:14:10).
  - Each mock gate's dirty paths list only the new untracked contract, with no build change (15:34:25 and 17:15:52).
  - The pinned contract hashes 82A819B1 and 71B3BA54 equal the committed blobs.
- **Receipts:** every hash prefix the plan cites matches: E1F4F227, C288EC37, 5A8324A0, 1752982C, F4F6DAB5, 563DB981, A54DD935, E9BE9526, 2C54E8DF, 35B35E59, 2506FC0F, 5EA2A276, 9D5B55E7, DA0D498A, BB92384F, 5E1E55CA, E2800DDA, 402AE30E, C4343B8F, 8F38B633, 550863D3, 61A96E83 and DEED8454.
- **TRX manifest:**
  - SHA-256 1C4AB4AC; 34 files, 30 of them TRX; every head is 4920a8ea, every hash equal, 0 problems.
  - Totals: 4739 and 4739, 741 and 718, 549 and 453.
  - No gate receipt lists a tracked change. There are 20 new Build.Tests cases.
- **Deploy logs:**
  - The first run failed on the EnvironmentFiles key, and the second after 10 attempts.
  - The third run answered after 13 probes: HTTP 200, version 1.4.43-pr87.4920a8ea, PID 718431, archive 98F0688C.
- **PR and TODO:**
  - PR 87 has `isDraft` false and head 4920a8ea. It has one ready_for_review event, 2026-11-03T15:59:32Z, by sharpninja.
  - The TODO has 45 tasks, none checked, with the a11, c1 and g1 notices present.
- **Timing under AC-006 as stored:**
  - The WaitUntilAsync renewal path holds the 50 ms heartbeat clock poll (HandoffIngestionService.cs:1015-1031), a busy retry on the renewal write (:994-1004), the 10 ms helper poll and a busy retry on the read. That is 360 ms, so 36 s is 100 times.
  - The other four callers see no concurrent writer: the reserve and approval claims commit before their tasks return, and the heartbeat interval is 1 h or 10 min.
  - No other unit hang guard touches a shared SQLite file. Microsoft.Data.Sqlite is pinned at 10.0.0.
- **Formatting:** no en dash, em dash or table row in the six records.
- **Fourth-read findings handled as 27.28 says:** a3 to a10, b2, c1 to c5, d1 to d4, e2 to e4 and g1.

Scripts and outputs are in /home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r12e/. Every script there is read only and analyzer clean: Get-GitFactsE, Find-Lines, Get-ReceiptList, Test-Ignored, Test-ManifestE, Get-HashesE, Get-PrStateE, Test-DashesE and Find-FileNames.
