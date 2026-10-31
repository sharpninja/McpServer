# Pre-HV projected problems, IMPL-UNITGREEN-001 round 4 (2026-10-20)

Holistic read: plan sections 1 to 17 end to end (as it will be committed, 374 lines), the round 4 code diff `e0b078c8..26fc8348` in full (`801e6862` and `26fc8348`, 15 file changes), the TODO through the plugin (18 tasks), the round 4 brief, the round 3 verdict and its four findings, every round 4 receipt, and the full option text of "Structural fix, round 4 (Recommended)" and "Fix the race in PR 87 (Recommended)". What-if pass for AC-TR-MCP-WS-005-001 as replaced at 18:23:07 (each input as its own boundary, reading order, absent profile, the five agents at once, concurrency, each host, legacy and upgrade), for the scanner's scope rule (sibling, reverse, nested, file-scoped, static import, alias named like a platform type), and for the harness rule of the race fix (concurrency on one database, isolation per test, cleanup on Windows); plus a sibling sweep of each defect class (profile 7.6).

## Fixed before HV (implementer edits)

- R4-P1, R3-F1 by construction: one source seam, `AgentPluginVersionSources`, set once by the assembly fixture; resolution tests pass explicit sources; no unit test sets a plugin-root variable. Amendment I's hostile-environment gate failed the reviewer's 19 at `e0b078c8` (control) and is 0 failed at `801e6862` and `26fc8348`.
- R4-P2, R3-F2: aliases and static imports resolved in lexical scope, with the counterexample, reverse-order, nested, file-scoped and non-platform cases bound to the scanner.
- R4-P3, R3-F3: FR-MCP-USECASE-017 deleted through the plugin and read back absent (getter and effective view), rechecked at 18:58 to 18:59; task 9 stands.
- R4-P4, R3-F4: the round 3 self-review sentence corrected in both copies (`c31c2918`), originals in git history.
- R4-P5, red gates at the first round 4 head `801e6862`: Windows Test failed `HandoffDurabilityTests.ApproveAsync_LeaseExpiresDuringLiveCreate_SecondInstanceWins` (one in-memory connection shared by concurrent contexts; triage 9b96ada1; your "Fix the race in PR 87"), and both integration lanes failed the `Path.GetFullPath` ratchet (my move of a reviewed call into a new file). Both fixed in `26fc8348`; every gate there is green: Linux Test 4516/0/0, hostile gate 4516/0/0, Windows Test 4516/0/0, Windows lane 373/0/0 complete by name, Linux lane 397/0/0, Build.Tests 436/0/0, ValidateTraceability passed.

## Your decision (answered "Extend fix, then round 4 (Recommended)"; built in 97b78114, every gate green there)

- R4-P6, siblings of the race class (found by the sweep; none has failed in any run): `HandoffAuditDefectTests` runs two services concurrently over one shared in-memory connection (`:30`, `:73`, `:215`), and `MemoryS1Harness` hands one in-memory connection to every context, with `MemoryConcurrencyTests`, `MemoryConsolidateJobTests`, `MemoryConsolidateTests` and `MemoryMigrationTests` running `Task.WhenAll` on it. Your option named HandoffDurabilityTests only. Round 3 failed R2-F1 because the fix followed the observed failures instead of the dependency class, so a reviewer may fail the race fix the same way. Options: extend the same fix (own connection per context to one temporary file, the harness assertion bound to each) in PR 87, then all seven gates; or declare them with a triage report and launch round 4 now.

## Declared, operator decision only if you want it changed

- R4-P7, scope of the hostile gate: it covers the five plugin-root variables and HOME, as the chosen option says. Other unit tests set and restore unrelated process variables (MCP_INSTANCE, QBRAINAI_INSTANCE, XAI_API_KEY, OPENAI_API_KEY, MCP_REPOSITORY_ROOT, PUBLIC, the MCPSERVER temp variables); they stay as they are.
- R4-P8, the race cause is an inference: the harness check proves the shared-connection condition and its removal deterministically; a scheduling race cannot be reproduced on demand by a unit test, so one green Windows Test is evidence of absence at that run only.
- R4-P9, temp files in unit tests: the handoff durability tests now use a temporary database file; your option kept them unit tests (test-owned temp data), as other temp-folder unit tests are.
- R4-P10, requirements docs: the wiki export is still blocked (retried 20:19:08, triage a6927fca), so the committed exports do not show AC-TR-MCP-WS-005-001 as replaced; ValidateTraceability reads those exports; the TR-MCP-SESSIONLOG-006 store drift stays (triage 647e35bb).
- R4-P11, TODO history: tasks 14 and 15 name the `AgentPluginUserProfileOverride` seam as it was in round 3; task 18 records its replacement.
- R4-P12, `801e6862` was pushed red on Windows and the lanes: the push waits only for the committed-tree Linux Test, by design; the other gates run against the pushed head. Recorded in plan section 17.
- Carried from round 3: R3-P2 (links below a scanned root are not followed), R3-P4 (census limit of the import-path tags), R3-P12 (the LocalDB collection runs alone, after the parallel ones), R3-P13 (the version-read evidence is scripted).

## Disclosures for the round 4 self-review

- After this session's latest resume, add-profile ran at 18:35:30, after two tool calls (a write of `AgentPluginVersionSources.cs` and a read of `MarkerFileService.cs`), not first.
- At 18:48:30 I overwrote my own scratch script `Get-WorktreeStatus.ps1` without reading it; restored at 18:49:34 from its only Write in the transcript (sha256 58777F3F...E756); no replacement kept.
- The ratchet regression reached a pushed head because I ran no integration lane before the `801e6862` commit; the `26fc8348` content ran one first (`ug-r4-precommit-lane-1`).
- Mock gate `ug-r4-race-mockgate-1` did not compile (EF1002, my interpolated SQL); `ug-r4-race-mockgate-2` is the mock gate.
- `Submit-Triage.ps1` has no `-WhatIf`, so the triage submission of 9b96ada1 had no dry run.
- The turn id `req-20261021T003500Z-fix-handoff-race` was set before measuring; the measured open was 00:35:47Z. Added after the answer: `req-20261021T012700Z-extend-race-fix` was set from 20:27 local; the measured open was 02:08:57Z.
- Twice a search with two semicolon-separated file names in one include returned nothing; I reran per file, and no claim rested on the empty results.
