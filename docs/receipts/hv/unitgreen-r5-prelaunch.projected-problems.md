# Pre-HV projected problems, IMPL-UNITGREEN-001 round 5 (2026-10-21)

Holistic read: the plan sections 1 to 18 end to end (451 lines, as committed in `8b7a49ea`), the round 5 code diff `97b78114..810ee8aa` in full (`8c254695`, 1 file; `ccc78b7a`, 2 files; `810ee8aa`, 1 file; documentation comments and one test reclassification), TODO IMPL-UNITGREEN-001 (21 tasks, none checked; notices on tasks 9, 15 and 18) and IMPL-SCANNERALIAS-001 through the plugin, the round 5 brief, the round 4 verdict and its three findings, every round 5 receipt, and the full text of every operator answer and order of 2026-10-21.

## Fixed or decided before HV

- R5-P1, R4-F3: the memory binding cites FR-MCP-MEMORY-002, TR-MCP-MEMORY-002 and TEST-MCP-MEMORY-002, present and mapped (`8c254695`); TEST-MCP-MEMORY-010 is absent from both lab stores.
- R5-P2, R4-F2: the round 4 brief and self-review copies carry corrections with notices; the round 4 request JSONL keeps the bytes as sent.
- R5-P3, the scanner: declared limit, corrected documentation (`ccc78b7a`), IMPL-SCANNERALIAS-001 as an open record, and your 06:06:46 override in the brief; the "no unit test depends on the platform" outcome stays scored, as a workspace rule judged on the sources and the runs.
- R5-P4, Build.Tests: the `/tmp` user quota failure (`ug-r5-build-tests-1`) is recorded; every later Build.Tests run uses TMPDIR on disk, which is not a system change by your 07:12:22 statement.
- R5-P5, Azure: MEMORY-PROCESS-009; the wiki export failure and the stale exports are information, not a gate.
- R5-P6, the hostile gate widened at your "If it needs to be done, do it.": about 90 scripted variables (every literal name the unit projects or production read, both forms of every GetLegacy name, two configuration-shaped variables). It found one environment-dependent unit test (`TryParse_UsesEnvironmentInstance_WhenCliInstanceIsOmitted`, which set only `MCP_INSTANCE` while `QBRAINAI_INSTANCE` wins); fixed test-only in `810ee8aa` (Integration, both variables controlled, assertions unchanged).
- R5-P7, receipts: the two runs without a receipt are recorded as failed, not evidence; every gate at `810ee8aa` has its receipt; the missing Windows dry run was run.
- R5-P8, carried items: TODO tasks 9 and 15 carry notices; R3-P4 is covered by the hostile gate's scripted HOME history; R3-P13 needs no code; R3-P12 and the `801e6862` push are not problems. All dropped.
- All seven gates are green at `810ee8aa`: Linux Test 4517/0/0, widened hostile gate 4517/0/0, Windows Test 4517/0/0, Windows lane 374/0/0 complete by name, Linux lane 398/0/0, Build.Tests 436/0/0, ValidateTraceability passed.

## Declared limits and risks

- The race cause is an inference (flagged risk, your "Then flag the risk and roll on.").
- The widened gate leaves PATH, TMPDIR, NUGET_PACKAGES and DOTNET_CLI_HOME real (the build needs PATH and the package cache; TMPDIR is the official temporary folder).
- The TR-MCP-SESSIONLOG-006 store drift (647e35bb): the store holds 1 criterion where the committed export lists 8; a requirement-store repair outside this PR.
- The `ccc78b7a` commit message cites a pre-commit run that has no receipt; that citation does not count, and the commit cannot be rewritten without a force push.

## Disclosures for the round 5 self-review

- add-profile ran after one tool call after the resume, not first.
- Two runs without a test-run id, one Windows Test run before its dry run, four Bash-line slips (unquoted semicolons, two backtick cases, an unquoted glob; nothing extra ran), three timing-script path misses.
- My round 4 claim that tasks 14 and 15 named the seam was wrong; R3-P12 was carried three rounds without being a problem; the first widened variable list missed the GetLegacy pairs.
- MEMORY-PROCESS-008 already existed and was not touched; the memory listing previewed 160 characters of MEMORY-PROCESS-007.
