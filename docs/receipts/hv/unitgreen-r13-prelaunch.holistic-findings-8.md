# Eighth holistic read before hostile validation round 13

The read-only subagent report of session ClaudeCode-20261102T182906Z-plugin-session, request req-20261105T170800Z-holistic-r13h (parent turn req-20261104T231251Z-continue-pr87-completion-2), as the session transcript holds it. Exported 2026-11-05 by Export-SubagentReport.ps1, because the harness refused the subagent's own findings file. Report text unchanged through its last sentence's opening words (the folder path that ends it is the scratch folder holistic-r13h); the harness indentation removed. Findings g1 and b2 were fixed in slice Z6, which moves the code head past a3caa21d, so b2 was edited in code rather than declared as the report proposed.

2026-11-05T11:36:32 (measured local stamp)

**Eighth holistic read before hostile validation round 13: 23 findings, three of them material.** I changed nothing tracked.

**Session log:** session `ClaudeCode-20261102T182906Z-plugin-session`, request `req-20261105T170800Z-holistic-r13h`. The turn was opened, updated twice and completed through the plugin scripts. Readback: completed, 8 actions, 4 decisions. The harness refused `findings-r13h.md` ("Subagents should return findings as text"), so the full findings are below. It accepted the turn response file `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13h/turn-response-r13h.txt`.

**Counts:** (a) 3, (b) 2, (c) 5, (d) 5, (e) 3, (f) 4, (g) 1. Total 23. The material ones are g1, c1 and b1.

**What checked clean:**
- **Git:** HEAD, origin and the Windows child branch are all `a3caa21d`, with nothing after it. Part 3 touches 3 test-support and test files. Part 4 adds 8 `///` remarks lines in 5 src files and removes none. Slice Z5 changes the 4 named test files. `52e6d627..a3caa21d -- src build .nuke` lists 7 src files and no build file.
- **Part 3 order:** the contract came before the code it binds. The `ug-r13z4c-mockgate-1` pins for `TemporaryInstanceChecks.cs` (85E5CCC8) and `OwnedReplStart.cs` (73B39C29) equal the `1bb0d61f` blobs, and only the contract file was dirty. The z4c, z4d and z5-2 binding pins equal the `b79fd51f`, `ff7dcf51` and `a3caa21d` blobs. 29 mock-gate cases = 32 contract cases minus the 3 held-out bindings.
- **Citations:** FR-MCP-103 and TEST-MCP-TRIAGESTORE-007 are in the store.
- **Manifest:** 87A4D748 holds 34 files, every head `a3caa21d`, every hash equal, 0 failed and 0 not executed. Totals: 4832, 4832, 741, 718, 563, 467.
- **Receipts and store:** every cited receipt hash prefix matches. The unit runs hold exactly 12 more names than at `1bb0d61f`. The store compare from z4b to z5 shows only the ac-4 append and the task changes. The live TODO has 50 tasks, none checked. 14 src projects carry a PackageId.
- **Durations and events:** the hung-save, elapsed-bound, StorageCommandBudgetTests and Windows-rerun durations, and the 17890 timing, match the records.

**(g) Code that does not meet stored ac-4**
- **g1. The instance's own host-prefixed settings have no host-prefix spelling rule.**
  - `SetsOwnedKey` (`tests/TestSupport.Shared/TemporaryInstanceChecks.cs:549-553`) compares `ConfigurationKeyIdentity`, which only maps "__" to ":" (`:169-173`).
  - Stored ac-4 says "a DOTNET_ or ASPNETCORE_ prefix places the rest of the name in the host's configuration, so every ... spelling of a key, alone or host-prefixed, sets that key". The 06:17:32 append says such a competing name is replaced by the instance's setting.
  - The instance sets DOTNET_NOLOGO and DOTNET_CLI_TELEMETRY_OPTOUT (`ScratchQBrainAi.cs:265-266`, `OwnedReplStart.cs:34-35`). An inherited NOLOGO or CLI_TELEMETRY_OPTOUT is kept by the builder, and the two-name rule never groups it.
  - The contract's own oracle (`OwnedInstanceKeyIdentityContractTests.cs:190-217`, where the later environment layer wins) gives the child's key NOLOGO the inherited value. `Governed` judges NOLOGO, so both production and the scripted reference would fail such a row. No row has one.
  - Practical risk is low: none of database, workspace, marker or address is reachable this way. But section 6 line 150, PR body line 267, handoff line 9 and brief UG48 claim "every inherited spelling" is removed.
  - Fix (the strengthening option): the builder removes inherited names that match an owned key once any host prefix is removed. The two-name refusal stays on separators or case only, so the instance's own DOTNET_ENVIRONMENT and ASPNETCORE_ENVIRONMENT pair is still accepted.
  - New tests: rows for owned DOTNET_NOLOGO with inherited NOLOGO, and owned Logging__LogLevel__Default with inherited DOTNET_Logging__LogLevel__Default. Add a wrong builder "host prefix ignored for owned keys" (today's rule). Then mock gate, binding and gates.
  - Narrowing ac-4 instead is an operator decision, not recommended.

**(a) Superseded statements with no forward note**
- **a1.** Plan line 1472 (27.20): "the current one is the "Docs only after `1bb0d61f`" line of 27.30 (`1bb0d61f..HEAD`)". The current D-A line is 27.31's `a3caa21d` (line 2101). Fix: append "since 27.31, the "Docs only after `a3caa21d`" line (`a3caa21d..HEAD`)".
- **a2.** Plan line 1518 (27.21) has the same text. Same fix.
- **a3.** Timing chains at plan lines 1734 and 1784 end "`ug-r13z4c-timing-sweep.txt`, 27.31". 27.31 (line 2089) makes `ug-r13z5-timing-sweep.txt` current. Fix: append "and since slice Z5: `ug-r13z5-timing-sweep.txt`, 27.31".

**(b) Contradictions**
- **b1. The plan says the hung-save case proves what TEST-MCP-TRIAGESTORE-007 states.**
  - Plan line 2088: "the hung-save case proves what TEST-MCP-TRIAGESTORE-007 states without timing the host".
  - The store says "Session-log SaveChanges and triage intake fail within about 5 seconds as backend_unavailable". The case now asserts only the configured 1 s budget's message and a 20 s hang guard. It ran 5.11 s of TRX time on Linux at `a3caa21d`.
  - PR body line 269, brief UG48, projected problems 137 and handoff line 19 declare the opposite ("no longer prove that the budget fires promptly ... the budget's own timing stays with the two StorageCommandBudgetTests lane cases"). The handoff cites 27.31 for that declaration, and 27.31 does not carry it.
  - Fix: replace the sentence. The case proves which budget fired, that SaveChanges was reached, and that it fired before 20 s. The about-5-seconds outcome holds only together with StorageCommandBudgetTests, because `ExecuteAsync` uses one `limit` for both `CancelAfter` and the message (`StorageCommandBudget.cs:28-41`). Declare this in 27.31.
- **b2. Stale classification reason in the four slice Z5 cases.** Each case's remarks still open "Integration: asserts an elapsed wall-clock bound on actual scheduling and timer execution (TR-MCP-USECASE-018 AC-006)". The dated entry under it says only a hang guard remains, and AC-006 (10:22:56) says a hang guard is not a wall-clock bound.
  - Locations: `RequirementsRecoveryTests.cs:477`, `SessionLogDenseGraphTests.cs:70`, `SessionLogReplaceSectionRetryableTests.cs:123`, `SessionLogTriageStoreTests.cs:135`.
  - Fix: declare it as kept wording in 27.31. Editing the comments would move the code head.

**(c) Statements a reviewer can show false**
- **c1. The SQL Server duration range is wrong.** Plan line 2090 says "against 6 to 17 s for the same cases in eight passing lanes", and projected problems 140 repeats it. `ug-r13z5-win-lane-sqlserver-durations-1.txt` line 71 shows RenameQuadBrainRoles at 52.87 s in the passing lane `ug-r12z-final-win-lane-1`. Other passing values are 5.67, 5.8 and 5.92 s, below 6. This bears on the host-speed reading. Fix: "5.3 to 17.24 s, except RenameQuadBrainRoles at 52.87 s in ug-r12z-final-win-lane-1".
- **c2. A store readback that does not exist.** Plan line 1782 and projected problems line 34 name `ug-r13z4c-store-readback-1.json`, which is not in the receipts folder. The round 13 readbacks are `ug-r13z4b-store-readback-1.json` and `ug-r13z5-store-readback-1.json`. This is the seventh read's a4 fix done wrong. Fix: name `ug-r13z5-store-readback-1.json`.
- **c3. Commit time.** Plan line 2088 says "Committed signed at 08:10:27". Git shows author and commit time 08:10:28 -0600. Fix: 08:10:28.
- **c4. Task 50 notice time.** Brief line 19 says "a notice on task 50 at 08:02", and plan 2088 says "(08:02, read back)". The live notice cites `ug-r13z5-elapsed-bound-sweep-1.txt` (written 08:03:15) and `ug-r13z5-linux-lane-elapsed-bound-durations-2.txt` (written 08:05:02), so it was written after 08:05:02. 08:02 is only its text stamp. Fix: "text stamped 08:02, written after 08:05:02; first in ug-r13z5-store-readback-1.json, 08:40:43".
- **c5. Self-review what-if overstated.** Self-review line 13 lists "a stall that never reached SaveChanges (Entered stays false, fails)" for slice Z5 as a whole. Only the triage-store interceptor records entry; the replace_section and recovery hung-save cases do not. Fix: limit the line to that case and declare the other two.

**(d) Missing disclosures**
- **d1. The elapsed-bound sweep receipt was saved after the fix.** `ug-r13z5-elapsed-bound-sweep-1.txt` (08:03:12 to 08:03:15, HEAD `ff7dcf51`, no dirty-path list) came after the three sibling edits (compile-3 started 08:01:28). Its lines 11, 14, 15 and 18 show the new 20 s and 30 s guards, not the 3, 3 and 8 s bounds found reached. The Linux -2 duration receipt (08:05:02) also came after. Only the Windows duration receipt (07:58:20) and git show the state before the fix. This is the class of third holistic finding d1. Fix: declare it.
- **d2. TEST-MCP-TRIAGESTORE-007 is missing from the brief.** The requirement list at brief line 20 does not name it, though the slice Z5 change logs and plan line 152 cite it. Its "within about 5 seconds" and its testType "unit" (the cases are Integration) are not disclosed. Fix: add it with its text and the b1 composition statement.
- **d3. 769 Application error/warning events go unmentioned.** `ug-r13z5-win-lane-host-diagnostics-1.txt` line 9 shows 769 such events in the lane window, all from provider QBrainAi (EF Core 20102 and 10100, GlobalExceptionHandlerMiddleware, TodoBootstrapImporter, TriageQueueWorker), plus 2088 information events (line 780).
  - The plan's diagnosis summary and the brief's and PR body's "no host event naming the cause" leave them out.
  - The records do not say whether the lane's own temporary instances or a host service wrote them. If the lane's instances did, that bears on FR-MCP-TESTRESOURCE-001's generated-artifact criterion and RINT-9 (logs inside the owned root).
  - Fix: classify the source and state it.
- **d4. The task 50 correction is omitted.** Brief line 19 and UG7 omit the 08:42:05 correction. Projected problems line 34 omits both the task 50 notice and the correction. Fix: add them.
- **d5. Handoff no-set list incomplete.** Handoff line 13's "(no set was made at `4398cffc`, `b3f26862`, `cb174ac8` or `2e121015`)" omits `b79fd51f` and `ff7dcf51`. Fix: add both.

**(e) Brief claims the evidence cannot support**
- **e1. The 17890 events are not LocalDB's.** Brief line 72 says "SQL Server's own report of its memory paged out from about 08:12:52", and PR body line 272 says the same. The events come from the full MSSQLSERVER instance, not the LocalDB instance the cases ran on. LocalDB started at 08:27:46 and its log shows no stall. Plan 2091 and projected problems 140 already say "full instance". Fix: say so in the brief and PR body.
- **e2. "Below the 30 s submit default" is wrong for one case.** Brief UG48 says the hang guards are "20 s, or 30 s for replace_section, below the 30 s submit default and the interceptor's 60 s stall". 30 s is not below 30 s, and replace_section uses the 5 s intake budget, not the submit default. Fix: "20 s (below the 30 s submit default), or 30 s for replace_section (below its 60 s stall)".
- **e3. "Every inherited spelling" is overstated.** Brief UG48 says the builder "removes every inherited spelling of a key the instance sets". The host-prefix spelling is not removed (g1).

**(f) Seventh-read findings that 27.31 says are fixed but are not**
- **f1 (c6).** Plan line 1942 still says "until 2026-11-05 05:08:09, 27.30", and self-review line 55 says "until 05:08:09". 27.31 line 2082 and self-review line 58 claim every 05:08:09 was corrected. Fix: 05:08:10.
- **f2 (c2).** Plan line 2033 and projected problems 123 say "27.28 line 1917 carries the forward note". The note is now on line 1918 (1917 is the AC-006 append of 15:29:24). Fix: cite 1918, or cite by text.
- **f3 (c3).** Plan line 2033 says "(27.25, line 1780 ...)". The claim is now on line 1781 (1780 is "Commit `ec58cf4a`"). Fix: 1781, or cite by text.
- **f4 (c7).** Self-review line 16 again claims forward notes for every statement `a3caa21d` supersedes, naming "the two D-A range lines (27.20 and 27.21)" and "the timing-sweep and store-readback chains". a1 to a3 and c2 show that is false. Fix: apply them, then reword the line.

I wrote 10 analyzer-clean read-only scripts and their outputs (git facts, the three diffs, blob hashes, receipt summary, manifest check, saved store and TODO reads), all in
