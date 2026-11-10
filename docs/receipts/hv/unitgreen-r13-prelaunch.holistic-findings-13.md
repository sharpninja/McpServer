Thirteenth holistic read before hostile validation round 13 (read-only subagent, session ClaudeCode-20261102T182906Z-plugin-session, turn req-20261106T022500Z-holistic-r13m). The harness refused the subagent's findings file, so this text is exported from the parent session transcript, unchanged except for the harness indentation.

2026-11-05T20:52:27 (measured local stamp)

# Thirteenth holistic read before hostile validation round 13

The records are close, but not ready for the records commit. I found 21 problems, four of them material (f1, d2, c1, g1). The code and the gates check clean: slice Z10 is comment text only, and every gate at baff082f matches its receipt.

**Counts:** (a) 9, (b) 3, (c) 3, (d) 2, (e) 1, (f) 1, (g) 2.

**Session log:** session `ClaudeCode-20261102T182906Z-plugin-session`, request `req-20261106T022500Z-holistic-r13m`. Opened with Begin-Turn.ps1, updated twice with Update-Turn.ps1, closed with Complete-Turn.ps1. Readback: completed, 7 actions, 4 decisions.

**Files:** the harness refused `holistic-r13m/findings-r13m.md` ("Subagents should return findings as text"), so the full findings are below. The response file `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13m/turn-response-r13m.txt` was written and used to close the turn.

## What checked clean

- **Git:** HEAD, origin and the Windows child ref are all `baff082f`; the worktree has no tracked change.
  - `94dc5b69..baff082f` touches 2 files: 6 documentation lines added, 3 removed, no code line.
  - The commit message carries the marker's signing line; there is no gpgsig header.
  - `52e6d627..baff082f -- src build .nuke` lists nine src files and no build file, so section 6 holds.
  - All 13 first-parent commits since `52e6d627` appear in the PR description.
- **Slice Z10's dated entries:** each cites only ids its member already cites (TR-MCP-HOSTILEREVIEW-003; FR-MCP-040 ac-1).
  - The corrected wording matches slice P: before `611a5dc5` the mapping adapter's CreatedAt returned `UtcNow` on every read.
  - Both quoted earlier wordings match git. The test case is unchanged.
- **Receipts:** every `ug-r13z10` hash prefix 27.36 cites matches, as do `3D5B84D7` (rules) and `E1D473A0` (twelfth read export).
  - Linux unit 4838 and Windows unit 4838 with ValidateTraceability passed.
  - Windows lane 718 (proof session 1, head `baff082f`, task removed); Linux lane 741.
  - Build.Tests 563 and 467; NU190x 0.
  - Store compare: 54 to 55 tasks, no criterion change.
  - No Linux gate receipt lists a tracked dirty path, and the binding pins equal the committed files.
- **Manifest `FD6C5BE2`:** 34 files, every head `baff082f`, every hash equal, 0 failed, 0 not executed.
- **PR 87** (gh, read only): draft at `baff082f`; the ready and draft events match the records.
  - The published body is still 59533 characters.
  - The r16 summary is 6132 characters, under the limit, and both sections it points to exist in the description.
- **Classified lab-name sweep:** the counts add up (4 + 2 + 15 + 16 + 119 = 156) and no hit is misclassified.
  - My broader search of the same 381 files found only generic fixture values (`/home/user`, `/home/example`, `C:\Users\fixture-user`, `C:\Users\example`).
  - No lab machine, user, address or lab path is missed in a non-docs file this PR touches.
- **The twelfth read's findings:** a1 to a5, b1, b2, c1 to c3, d1, e1, f1, f2, g1 and g3 are fixed. g2 is recorded (see c1, b2 and d2 below). f3 is not fixed (f1 below).
- **The d3 event-log record** is unchanged since the twelfth read checked it.

## (a) Superseded statements with no forward note

- **a1.** Plan line 460 (section 18): "since 27.34, all 53; since 27.35, all 54."
  - Fix: append "; since 27.36, all 55".
- **a2.** Plan line 1108 (25.7): "since 27.34, at `becb0742`; since 27.35, at `94dc5b69`".
  - Fix: append "; since 27.36, at `baff082f`".
- **a3.** Plan line 1547 (27.22 gates header): "since 27.34 at `becb0742` and since 27.35 they are at `94dc5b69`".
  - Fix: "since 27.35 at `94dc5b69` and since 27.36 they are at `baff082f`".
- **a4.** Plan line 1594 (27.22 D-A line): "and 27.35 (slice Z9), which names the current last code head `94dc5b69`".
  - Fix: "27.35 (slice Z9, `94dc5b69`) and 27.36 (slice Z10), which names the current last code head `baff082f`".
- **a5.** Plan line 1952 (27.28 D-B): "of 27.34, all 53, and of 27.35, all 54."
  - Fix: append "and of 27.36, all 55".
- **a6.** Plan line 2006 (27.29 D-B): "and of 27.34 and 27.35, all 53 and then all 54)".
  - Fix: "of 27.34 to 27.36, all 53, 54 and then 55".
- **a7.** Brief line 230 (UG1) cites "ug-r13z9-doc-claim-checks-1.txt".
  - That receipt checked the records at `94dc5b69`. Projected problems line 44 already says the receipt is now `ug-r13z10-doc-claim-checks-1.txt`.
  - Fix: cite the r13z10 receipt, or a fill marker until it exists.
- **a8.** Brief line 232 (UG3): "every duration of 60 s or less that a unit test configures is listed in ug-r13z9-configured-durations.txt". Its comparison list also stops at `...-compare-z8b-z9.txt`.
  - Fix: cite `ug-r13z10-configured-durations.txt` and add `ug-r13z10-configured-durations-compare-z9-z10.txt`.
- **a9.** Plan line 2260 cites "projected problems lines 42 and 64".
  - Those are the twelfth read's line numbers. The fixes now sit at lines 44 and 66; line 42 is a heading and line 64 is item 38.
  - Fix: cite by item, or give the current lines.

## (b) Contradictions

- **b1.** Four places say slice Z10 corrected both mapping entries:
  - PR body summary line 13: "two dated entries on the requirement mapping methods are corrected and dated".
  - Handoff line 9: "the ListMappingsAsync and CreateMappingAsync entries corrected and dated".
  - Brief line 17: "...entries corrected and their corrections dated".
  - Brief line 23: "...entries corrected and dated".
  - The plan (section 6 line 155, 27.36 line 2254), description line 165 and the commit message say otherwise. Slice Z10 corrected only ListMappingsAsync; CreateMappingAsync was corrected in slice Z9, and slice Z10 only dated that correction.
  - Fix: "the ListMappingsAsync entry corrected, and both mapping corrections dated".
- **b2.** Two places still give four exceptions as the whole set:
  - Projected problems item 10 (line 54): "The four identifier-scrub assertions stay as flagged lab-bound exceptions."
  - Description line 403: the same sentence.
  - Item 149, description lines 166 and 359, the brief and plan 27.36 record a fifth.
  - Fix: add "; a fifth, McpServer_Omarchy, is recorded under the same ruling (27.36)".
- **b3.** Description line 7 says "the PR is marked ready after this file is committed". The same paragraph then says "and marked it ready again after the gates at the last code head were green (27.36)".
  - When the file is committed that has not happened, and 27.36 records no ready mark.
  - Fix: "and marks it ready again after this file is committed".

## (c) Statements a reviewer can show false

- **c1 (material).** The operator's 2026-11-02 ruling is misquoted.
  - Plan 27.36 line 2250 says the ruling "states that "historical evidence and shipped configuration are not changed in PR 87"".
  - The same quotation appears at brief lines 53 and 282, projected problems item 149, description lines 166 and 329, and PR body summary line 14.
  - The chosen option text, in the transcript (2026-11-02T15:24:31Z, question "Scrub hits"), is: "Leave the four assertions exact and record each as a flagged lab-bound exception with its reason. Historical evidence and shipped configuration stay unchanged in PR 87."
  - The quoted words are the implementer's paraphrase, from the disposition field of `source-identifier-audit-20261102.json`.
  - Fix: quote the option text verbatim.
  - **My judgment on the decision:** the second sentence does support keeping McpServer_Omarchy unchanged. The option counted four assertions, so a fifth is an extension, but a defensible one. It is stronger with TR-MCP-QBRAIN-006 (see d2), and it stays the operator's to overrule.
- **c2.** Plan line 2258 and self-review line 11 say Find-GlobPathspecUse.ps1 found "no other script that hands git a wildcard pathspec; the rest pass folder names or explicit file paths."
  - Rerunning that script with its own pattern lists two kinds of exception:
    - `Show-DiffFormDifference.ps1:28` and `Show-AddedLineCountSources.ps1:24` and `:36` splat `'src/*.cs'`, `'build/*.cs'` and `'tests/*.cs'` to git.
    - Five scripts pass quoted wildcards, which is safe: `Compare-CitationIdSets.ps1:29`, `Show-CitationPatternDifference.ps1:32`, `Find-UndocumentedAddedMembers-v2.ps1:29`, `Find-UndocumentedMembersRoslyn.ps1:67` and `Get-AddedLineCitations.ps1:33`.
  - The substance holds: the callers I checked passed folder names (for example `ug-r13z8-lab-name-sweep-1.txt`, "-Paths:src,tests,build").
  - Fix: "no other receipt-writing script splats a wildcard pathspec; two diagnostics splat on purpose to show the expansion, and five pass quoted wildcards, which PowerShell does not expand".
- **c3.** Self-review line 22 says "each correction is its own dated entry, the earlier wording kept". UG53 ("the earlier text kept") and description line 165 say the same.
  - In fact the 2026-11-02 sentences were rewritten in place, by slice Z10 for ListMappingsAsync and by slice Z9 for CreateMappingAsync. The new entries only quote the old words.
  - Fix: say the 2026-11-02 sentence was corrected in place and the new entry quotes its earlier wording. See g1.

## (d) Missing disclosures

- **d1.** The `docs/` exclusion's stated reason covers only receipts and records.
  - The reason appears at plan line 2256, projected problems item 149, description line 167 and PR body summary line 15.
  - The branch also touches five `docs/Project` projections. Three hold lab-name lines by the receipt's own pattern:
    - `Functional-Requirements.md`: 13 lines.
    - `Technical-Requirements.md`: 56 lines, for example TR-MCP-RUNAS-001's `/home/sharpninja` paths and TR-MCP-QBRAIN-006's "Do not rename database McpServer_Omarchy or host PAYTON-LEGION2".
    - `Testing-Requirements.md`: 20 lines.
  - These files are generated from the store and never hand-edited (27.17, 27.19), so that is the real reason they stay, and it is not stated.
  - Fix: state it, and say the store text they project is out of scope except the in-scope records in d2.
- **d2 (material).** Two in-scope records name lab hosts in their criteria. The store text was not swept after slice N, and neither criterion is flagged. Plan 27.7 covers "the in-scope store text", but the classified sweep reads files only.
  - I read both through the plugin getters at 20:45 to 20:46:
    - TR-MCP-NUKE-016 AC-003: "On LAB-OMARCHY the target leaves a portable Ollama ...". Slice T strengthened this record's AC-001, and 27.22 linked the record to IMPL-UNITGREEN-001.
    - TR-MCP-HOSTILEREVIEW-003 AC-001: "... measured on PAYTON-LEGION2 2026-10-15; ... HostileReviewWorker_InvalidOptions_FailStartup ...". That is the test slice Z10 scrubbed, and its new dated entry cites this id.
  - Only TEST-MCP-QBOLLAMA-001 AC-004 is declared lab-bound (plan line 1505, description line 353).
  - TR-MCP-QBRAIN-006 criterion 3, "McpServer_Omarchy and PAYTON-LEGION2 remain where they identify that database and that host.", is the stored requirement behind the fifth exception. The records do not cite it.
  - Fix:
    - Sweep the in-scope records (the TODO's linked FR, TR and TEST, and every id the PR's changed lines cite) with the same pattern, and keep the receipt.
    - Declare both criteria lab-bound and not set; changing their text is the operator's decision.
    - Cite TR-MCP-QBRAIN-006 in 27.36 and item 149.

## (e) Brief claims the evidence cannot support

- **e1.** Brief line 259 (UG30): "the slice N scrub and citation pass leave every lab identifier on a changed line either replaced or one of the four flagged lab-bound exceptions".
  - Slice N left lab-wsman on a line this branch added; only slice Z10 fixed it.
  - The receipt marks all four exceptions KEPT, so none of them is on a changed line.
  - Plan 27.7 covers every file the branch touches, not just changed lines.
  - Fix: "since slice Z10, every lab identifier in the non-docs files this branch touches is replaced, one of the four flagged exceptions, or the fifth (27.36); slice N left one on an added line, fixed in Z10".

## (f) A twelfth-read finding 27.36 says is fixed but is not

- **f1 (material; the twelfth read's f3).** Plan line 2260 says "with every forward note above carried through 27.36, 55 tasks and `baff082f`", and self-review line 25 says "Those are now carried through 27.36, 55 tasks and baff082f".
  - a1 to a6 contradict both. This is the seventh recurrence of the structural cause declared in projected problems item 145.
  - Fix: apply a1 to a6. Add these tails to the open-fix pattern: "since 27.35, all 54", "and of 27.35, all 54", "and then all 54", "since 27.35, at", "since 27.35 they are" and "which names the current last code head".

## (g) Slice Z10 code and the lab-name classification

- **g1 (material; it turns on how MEMORY-FACT-008 is read).** Slice Z10 removed words from ListMappingsAsync's 2026-11-02 entry ("-...instead of the time of the read." became "+...instead of the time its CreatedAt was read."). Slice Z9 did the same to CreateMappingAsync.
  - The earlier wording survives only as a quote inside the new 2026-11-05 entry.
  - That goes against plan 27.30's reading, "earlier entries kept", and against slice Z4 part 2's precedent: "No existing word is removed".
  - Fix, by the operator's reading, one of two:
    - Restore the 2026-11-02 sentences verbatim and keep the corrected wording only in the dated corrections. This moves the code head, so the gates run again.
    - Declare in-place correction with the old wording quoted as the chosen reading, and correct the statements in c3.
- **g2 (low).** The PUBLISHED-IDENTITY rule's regex is broader than its reason.
  - `(?<!/home/)\bsharpninja(?=[./-])` with path (any) would also classify a host such as "sharpninja-pc" or "sharpninja.lan" as published identity.
  - No current hit is misclassified: all 15 are package ids, the repository URL, the author or the plugin core tarball.
  - Fix: list the published forms, or require one of them as the following token.

## Scripts

Everything I wrote is in `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13m/`. All of it is read only and was analyzer clean before it ran. Bash only launched `pwsh -NoProfile -NonInteractive -File` with literal arguments.

- **My own scripts:**
  - Get-GitFactsM.ps1.
  - Find-LabNamesBroadM.ps1, with pattern-m-lab-broad.txt.
  - Find-TranscriptContextM.ps1, which redacts every run of 24 or more token characters.
  - Get-DocsTouchedM.ps1 and Find-RecordOfLabLinesM.ps1, with pattern-m-lab-wide.txt.
- **Copies of holistic-r13l scripts with the output folder changed:** Show-ReceiptSummaryM.ps1, Test-ManifestM.ps1 and Get-PrStateM.ps1.
- **Pattern files:** pattern-m-stale.txt and pattern-m-nuke016.txt.
- **Shared helpers run unchanged:** Find-TextInFiles, Show-MatchContext, Get-GitRangeDiff, Get-FileSha256, Measure-FileChars, Show-DirFiles, Get-Requirement (plugin getters, read only) and Find-GlobPathspecUse.

Nothing tracked changed, no secret was printed, and I did not contact PAYTON-LEGION2. I did not run `/add-profile`, because this brief was my complete task.
