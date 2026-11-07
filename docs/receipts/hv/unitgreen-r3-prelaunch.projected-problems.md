# Pre-HV projected problems, IMPL-UNITGREEN-001 round 3 (2026-10-20)

Holistic read: plan sections 1 to 16 end to end, the TODO (16 tasks), the r4 PR body, the round 3 brief, the round 2 brief and self-review, every round 3 receipt, and the full option text of every operator answer of 2026-10-20 from the transcript. What-if pass per new criterion (AC-TR-MCP-WS-005-001, AC-TR-MCP-SESSIONLOG-006-009) and for TR-MCP-NUKE-019 AC-2 across crash and replay, concurrency, boundary input, each host, lifecycle, isolation, legacy and upgrade, and operator-visible output.

## Fixed before HV (implementer edits)

- R3-P1, a race in my own new test: `HermeticUserProfileFixtureTests` read the live override while `MarkerFileServiceTests` can point it at its own temp folder and delete it. Fixed in `56d14d7c`.
- R3-P6, the pre-existing LocalDB flake: the connect timeout (`6042dbb3`, your "Fix the flake in PR 87") fixed the connect phase only; lane-4 then failed a migration command past 120 s under parallel load, so the four LocalDB classes share one collection (`0210244d`, your "Serialize SQL Server tests"). Every SQL Server test now runs in 5 to 9 s.
- R3-P7, silently lost results: lanes 1, 3 and 5 each dropped 1 or 2 results with no outcome. Root cause found (your "Diagnose the loss in PR 87"): pg_ctl, started without output redirection, wrote into the test process's standard output, which carries the xUnit v3 JSON messages, and a garbled test-passed was dropped. Fixed in `6ea425ab` (pg_ctl `-s`); diag-3 shows 0 unparseable lines and a result for every start; lanes 6 and 7 are complete by name.
- R3-P8, a departure from your chosen option: `0210244d` defined the collection without `DisableParallelization`, though the option said "with parallelization disabled". I disclosed it in the commit but did not ask you. Fixed in `68fa7b39` under BDPv4 (definition assertion and negative controls, mock gate 4456/0/0, then the flag).
- R3-P9, a stale statement: the plan's D-C, the PR body and the brief still said the two source-reading guards stay unit tests, while round 3 made them integration tests as your "Structural fix, round 3" option required. Corrected in all three.
- R3-P3, folded in by your answer at the first pre-HV confirmation ("Fold R3-P3 in first"): a plugin version file that exists but cannot be read no longer fails the marker. AC-TR-MCP-WS-005-001 strengthened first (16:32), PluginVersionFileReadTests green in mock gate ug-r3-versionread-mockgate-1 (4468/0/0) before the fix, then `e0b078c8` and its binding (4479/0/0).
- Every gate reran at `68fa7b39` (all green) and again at `e0b078c8`, the last code head: Linux Test 4479/0/0, Windows Test 4479/0/0 (per project equal to Linux), Windows lane 373/0/0 complete by name, Linux lane 397/0/0, Build.Tests 436/0/0, ValidateTraceability passed.

## Declared, operator decision only if you want it changed

- R3-P2, behavior change: a directory link or other reparse point below a scanned root is no longer followed (for example `~/.claude/projects` moved to another drive behind a link); marker resolution falls back to the synced version and turn context stays None. The roots themselves are still walked.
- R3-P4, census limit: the 12 import-path tests tagged Integration are the ones that failed as kingd; a unit test that reaches the history scan with the real home and still passes as kingd would not appear. None is known.
- R3-P5, requirements docs: the committed exports do not list the two new ACs (wiki export blocked on Linux, triage a6927fca), and this store holds 1 criterion for TR-MCP-SESSIONLOG-006 where the export lists 8 (triage 647e35bb).
- R3-P10, option-text readings: "Fix the flake" said "no unit test changes"; no existing unit test changed, but the mock checks added new pure unit tests (SqlServerTestConnectionTests, later SqlServerLocalDbCollectionTests and EphemeralPostgresFixtureArgumentsTests). "Diagnose the loss" predicted "likely ... new criteria"; none was needed, because TR-MCP-NUKE-019 AC-2 already requires every integration case to run. "Rerun for a complete set" kept the cause outside PR 87; your later "Diagnose the loss in PR 87" superseded that.
- R3-P11, Windows Test cadence: Windows Test was not run at `6042dbb3` or `0210244d`; it was held while the lane diagnosis moved the head, and ran at `6ea425ab` (4454/0/0), `68fa7b39` (4456/0/0) and `e0b078c8`.
- R3-P13, the R3-P3 evidence is scripted: the chosen option named a scripted test, so there is no real-file integration test for a locked version file; the resolve-level bindings read a real temporary profile with a scripted lock on named files.
- R3-P12, the collection flag's side effect: `SessionLogAgentSessionHeaderMigrationTests` also holds SQLite unit tests, which now run in the non-parallel phase of the unit run; the unit run took about the same time (test-6 about 4:00, test-7 about 3:56).

## Disclosures for the round 3 self-review

- add-profile was not the first action after either resume (criterion writes and dry runs the first time; the diag-3 dry run and start the second).
- Counts I stated before checking: 17 slice paths (15), "about 86 test files" (84 plus tests/Shared and one build file).
- TestRepositoryRoot was written in the same pass as its policy tests.
- ug-r3-win-lane-1 reported as complete before a by-name comparison showed a lost result.
- An estimated chat stamp and request id (11:14:30; measured 12:07:45).
- A wrong inference at 14:11:44 (tests in-process) that set aside the hypothesis that turned out to be the cause.
- The unasked departure from the serialize option (R3-P8) and the stale guards sentence (R3-P9).
- At 15:06 I overwrote my own scratch script Search-Text.ps1 without reading it; restored from the transcript by hash at 15:09; the new script is Search-TextTree.ps1.
- The docs commit 395b4cd8 claims the traceability logs, but `*.log` is git-ignored, so they were missing; the next docs commit adds `.out.txt` copies (as rounds 1 and 2 did).
- The committed-tree Linux Test runs ug-r3-linux-test-7 and -8 were started without a dry run of that exact invocation (the same Start-NukeRun.ps1 call for the Test target had been dry-run earlier in the session).
- The interactive lane's task uses `C:\Program Files\PowerShell\7-LabWsman\pwsh.exe`, the first pwsh the starter finds; unchanged since round 1.
