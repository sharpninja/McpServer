# Projected problems before hostile validation round 8b (IMPL-UNITGREEN-001), 2026-10-28, at head fbc2da86

Prepared by ClaudeCode (claude-opus-5-5, effort xhigh). This list supersedes `pre-hv-projected-problems-20261027-r8b-2.md`, which you confirmed at 15:32:53 ("Confirm, launch HV 8b (Recommended)"). It keeps R8b-P1 to R8b-P17 as they stood and adds the items found since.

Inputs:
- a holistic read of plan sections 1 to 25 (1046 lines);
- the round 8b brief, the PR body r10 and the round 8 self-review;
- the TODO, read back with 28 tasks;
- the requirement records in scope, read through the plugin at 19:21 and 19:25;
- the TRX manifest, and the citation lint over the lines this branch adds.

The code head is still `433ce191`. Every gate of the plan's section 23 set is green there, and nothing was rerun since:
- Linux Test `ug-r8b-linux-test-4`: 4592/0/0.
- Windows Test `ug-r8b-win-test-ssh-3`: 4592/0/0.
- Windows lane `ug-r8b-win-lane-4`: 589/0/0.
- Linux lane `ug-r8b-linux-lane-3`: 612/0/0.
- Build.Tests `ug-r8b-build-tests-2`: 470/0/0.
- ValidateTraceability `ug-r8b-traceability-2`: passed.

After `433ce191`, three commits are docs only and pushed:
- `4634c73c`;
- the develop merge `d43c1d12`, 99 docs files;
- `fbc2da86`, the TRX evidence (27 TRX files, byte for byte) and plan section 25.

## Needs your decision before launch

- **R8b-P18. One test citation that is not in the store and not labeled historical.** The citation lint (`Get-AddedLineCitations.ps1` over the test lines this branch adds or changes) finds the same 10 absent ids as round 7, each labeled historical or a criterion id, plus one new one:
  - `TEST-MCP-REQSCOPE-REPL-001` at `tests/QBrainAi.Support.Mcp.IntegrationTests/RequirementScopeLayerReplIntegrationTests.cs:8`.
  - The base already cited it (line 14 at `3da2db93`). Slice C (`8c651f13`) rewrote that summary line, so it now counts as a line this branch changed.
  - The id is in the committed `docs/Project` exports (Requirements-Matrix, "Planned") but not in this store.
  - The brief's UG7 says every id on such a line is in the store or labeled historical, so as it stands a reviewer can FAIL UG7.
  - The fix that matches the R6-F2 precedent is a historical label in that one doc comment. It is a test-file change, so the code head moves and the section 23 gates rerun on both hosts.

## Found since your confirmation (declared)

- **R8b-P19. Develop drift and the docs-only merge.**
  - Develop had moved past this branch's base `3da2db93`; I had not looked at its content before recommending "Merge develop first, then HV".
  - It is now `9b099716`: PR 88 plus the two cherry-picked docs commits. This branch merged it docs only (`d43c1d12`), so `433ce191` stays the last code head.
  - The merge base with develop is now `9b099716`, and the change under review is `git diff 9b099716...HEAD`.
- **R8b-P20. The cline-qb move and develop's history (plan 25.2).**
  - Done under your force-push approval: develop was rewritten, PR 88 restored by a fast-forward, and `826773fe` and `4eae21cd` cherry-picked as new SHAs (`71155e7c`, `9b099716`).
  - All 25 original commits stay on `cline-qb` (`d3df5353`).
  - The QB-AGENT-001 checkpoint and its authorization are out of this PR's scope (the brief says so).
  - A clone that still holds develop at `d3df5353` could restore it with a plain push. Both lab main checkouts are reset.
- **R8b-P21. The ClineQb clash and your 18:15:38 choice.**
  - You chose "Strengthen AC, use sources (Recommended)" for develop's ClineQb environment read. It was not carried out: develop no longer carries that code, and this branch never merged it.
  - AC-TR-MCP-WS-005-001 is unchanged since 2026-10-27T22:25:32. The brief records the departure so the reviewer does not score it as an ignored option.
- **R8b-P22. The lab line-ending attribute.**
  - Both hosts now carry `* text=auto eol=lf` (lab configuration).
  - The gates at `433ce191` ran before it. Checkouts of LF-stored files are unchanged, and CRLF-stored files are left as stored.
  - The TRX evidence was staged byte for byte around it, each blob checked against `git hash-object --no-filters`.
  - New commits on any branch now store LF.
- **R8b-P23. By-name completeness of the lanes was missing until today.**
  - Section 22.5 required each round 8b lane to be complete by name against round 8. I had not produced it.
  - Now produced (`ug-r8b-win-lane-compare-r8l3-r8bl4.txt`, `ug-r8b-linux-lane-compare-r8l2-r8bl3.txt`): each lane holds every round 8 result plus exactly the 179 QBrainAi.Repl.IntegrationTests cases. Nothing lost.
- **R8b-P24. Store timestamps are read times.**
  - The requirement getters return `createdAt` and `updatedAt` equal to the moment of the read; seen again at 19:21 and 19:25, and filed earlier as `triage-report-21c5497a`.
  - So criterion write times come from the write receipts, as R8b-P11 already says.
  - My 18:10 session-log note that the records had been "recreated" was a wrong inference; corrected in the log.
  - `TR-MCP-REPL-008` reads back with an empty title (not changed by this branch).
- **R8b-P25. Process disclosures since your confirmation (mine):**
  - add-profile was not my first action after the latest compaction (two edits and one analysis of `Show-SideDiff.ps1` came first).
  - Two estimated timestamps: the 18:58:40 chat stamp, and the request id `req-20261028T231900Z` (the turn opened at 23:29:13Z).
  - `git fetch --prune` removed 25 stale local remote-tracking refs for branches already deleted on GitHub.
  - I moved 15 non-QB commits, PR 88 among them, before listing the range; repaired by your choices.
  - My option text for `* text=auto` omitted the Windows CRLF checkout effect; I asked again.
  - My first transcript search printed UTC times as local; fixed before any time was written.
  - One call to a script that does not exist (`Replace-InFile.ps1`) failed before anything ran.
  - The second and third plan copies into the worktree: the second ran without its own dry run, and the third had one.

## Carried (as confirmed at 15:32:53)

- **R8b-P1:** the two rulings applied through the plugin: hostile runs out of scope (section 23), and environment-specific criteria removed (section 24).
- **R8b-P2:** the 589/1 correction for `ug-r8b-win-lane-2`.
- **Withdrawn:** R8b-P3 and R8b-P7.
- **R8b-P4:** the REPL timeout `ReadVariable` default reader. Its evidence is its one-line source and the runs on both hosts.
- **R8b-P5:** the census limit of the 12 import-path tests.
- **R8b-P6:** no TRX for QBrainAi.Support.Mcp.IntegrationTests, Acid or TransactionSecurity (AC-2, IMPL-TXNINTEGRATION-001); the reviewer may FAIL that under policy 7.9.
- **R8b-P8:** the exported TEST-MCP-REPL-007-1 text; its id does not match the locked format.
- **R8b-P9:** the failed runs of round 8b, none of them evidence.
- **R8b-P10:** the process disclosures of round 8b.
- **R8b-P11:** the carried items: the race inference, 647e35bb, replace times from write receipts, the writer list, the scanner override, the Azure export, the 98.19 MB file, the redacted hostile metas.
- **R8b-P12:** round 8's sandboxed Build.Tests is not evidence.
- **R8b-P13:** WindowsJunction's unenforced precondition.
- **R8b-P14:** the Windows TRX copied read-only from the mount, each copy hash-verified; now committed in `fbc2da86`.
- **R8b-P15:** code with no criterion of its own after section 24 stays as fixes.
- **R8b-P16:** 130 of 1300 criteria in the store have environment-bound wording; outside this PR.
- **R8b-P17:** the D-B set of section 24. The TODO now has 28 tasks, all to be checked after AGREE.

## What happens next if you confirm

- R8b-P18 as you choose.
- Then the prelaunch records, committed signed with a normal push:
  - the brief;
  - the self-review, whose PROJECTED-PROBLEMS-CONFIRMED item quotes your answer;
  - this list;
  - the two lane comparisons.
- Then PR body r10.
- Then round 8b: Codex gpt-6-astra max, gate `unitgreen-r8b`, unbounded review time, the TRX supplied under policy 7.9.
