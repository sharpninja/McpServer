Fourteenth holistic read before hostile validation round 13 (read-only subagent, session ClaudeCode-20261102T182906Z-plugin-session, turn req-20261106T043100Z-holistic-r13n). The harness refused the subagent's findings file, so this text is exported from the parent session transcript, unchanged except for the harness indentation.

2026-11-05T23:00:43 (measured local stamp)

# Fourteenth holistic read before hostile validation round 13

**Session log:** session `ClaudeCode-20261102T182906Z-plugin-session`, request `req-20261106T043100Z-holistic-r13n`, parent turn `req-20261104T231251Z-continue-pr87-completion-2`. Opened with Begin-Turn.ps1, updated twice with Update-Turn.ps1 and closed with Complete-Turn.ps1. Readback: completed, 4 actions, 3 decisions.

**Files:** the harness refused `holistic-r13n/findings-r13n.md` ("Subagents should return findings as text"), so the full findings are below. It accepted the response file `holistic-r13n/turn-response-r13n.txt`, which was used to close the turn.

**Counts:** 16 findings: (a) 6, (b) 2, (c) 1, (d) 5, (e) 1, (f) 1, (g) 0. Material: f1, b2 and e1. d2 may be held material.

## What checked clean

- **Git:**
  - HEAD, origin and the Windows child ref are all `53a210ca`, with no tracked change.
  - `baff082f..53a210ca` replaces 2 lines in `RequirementsWorkflow.cs`, both `///` remarks.
  - The commit message has the Signed line. There is no gpgsig header.
  - `52e6d627..53a210ca` touches nine src files and no build or `.nuke` file.
- **Slice Z11 code:**
  - Each 2026-11-02 sentence is exactly its text at `becb0742`.
  - Every later entry (slice Z8 part 2 and the slice Z10 corrections) is kept as written.
  - Each new 2026-11-05 entry cites only FR-MCP-040 ac-1, which the method's earlier entries already cite.
  - The accurate wording matches the slice P diff: before `611a5dc5`, `MappingItemAdapter.CreatedAt` returned `DateTimeOffset.UtcNow` on every read. After it, the adapter gets `mapping.CreatedAtUtc` from the list or the upsert.
  - The file's SHA-256 (`9BEAD3B7`) is the same in the comment-only check, the compile receipt's sources and the binding pin.
- **Gates at `53a210ca`:**
  - Every ug-r13z11 receipt matches the records: Linux 4838 / 741 / 563 with ValidateTraceability passed; Windows 4838 with ValidateTraceability, the lane 718, Build.Tests 467.
  - The Windows lane ran in session 1 at head `53a210ca`; its task was removed in the one saved read.
  - NU190x is 0.
  - Every by-name compare has 0 names on either side only.
  - The Linux receipts list no tracked dirty path.
  - Every hash prefix the plan cites for a ug-r13z11 file matches.
- **Manifest `14AE38B5`:** 34 files, every head `53a210ca`, every hash equal, 0 failed, 0 not executed.
- **Store (read through the plugin getters):**
  - The seven scrubbed records equal `ug-r13z11-store-scrub-readback-1.json`, field for field apart from the read-time timestamps, and hold no lab-name match.
  - TR-MCP-NUKE-016 AC-003 differs from its ug-r10 readback only in "On LAB-OMARCHY" becoming "On the Linux lab host".
  - The three reworded criteria are all unsatisfied.
  - The 36 non-fixture ids the store sweep listed as absent all return not_found from the exact getters.
  - The TODO has 56 tasks, none checked; task 56 and the task 55 notice are as stated.
- **Lab-name classification:**
  - The reasons in `lab-name-rules-r13z11.psd1` are correct and none of the 156 hits is misclassified.
  - My broader search of the 381 touched non-docs files found only generic fixture values and the flagged BugTriage139Eighteenth path. It covered private IP ranges, `.lan` and `.local` names, `DESKTOP-` and `LAPTOP-` names, and profile and home paths.
- **PR 87 on GitHub:**
  - Draft at `53a210ca`.
  - The ready_for_review (2026-11-03 09:59:32) and convert_to_draft (2026-11-05 05:08:10) events match the records.
  - The r16 summary body is 7287 characters, under 65536, and nothing in it contradicts the description or the plan.
- **The thirteenth read's findings:**
  - a1 to a9, b1 to b3, c1 to c3, d1, e1, g1 and g2 are fixed.
  - d2 was handled by decision; see b2 and d2 below.
  - No record still quotes the paraphrase as the operator's words.
  - f1 is not fixed (see f1 below).

## (a) Superseded statements with no forward note

- **a1. Task-count chains stop at 27.36 and 55 tasks.** Only plan lines 460, 1952 and 2006 reach 27.37. The others:
  - Line 1374 (27.16): "54 since 27.35 and 55 since 27.36".
  - Line 1508 (27.21): "27.35 (all 54 tasks) and 27.36 (all 55 tasks)."
  - Line 1546 (27.22): "27.35 (all 54) and 27.36 (all 55)."
  - Line 1578 (27.22): "54 since 27.35, 55 since 27.36)."
  - Line 1685 (27.23): "55 since 27.36)."
  - Line 1755 (27.24): "27.35 with 54 (eleventh holistic read finding a4) and 27.36 with 55."
  - Lines 1805, 1851 and 1901 (27.25 to 27.27): "55 since slice Z10, 27.36)."
  - Lines 2105 (27.31) and 2149 (27.32): "27.35 to 54 and 27.36 to 55)".
  - Line 2183 (27.33): "27.35 to 54 and 27.36 to 55".
  - Line 2217 (27.34): "27.35 amends it to 54 tasks and 27.36 to 55".
  - Line 2243 (27.35): "27.36 amends it to 55 tasks".
  - Line 2268 (27.36): "with all 55 tasks checked after AGREE", with no note at all.
  - Fix: append the 27.37 / 56 step to each, and mark 2268 as history.
- **a2. Pointers to the TRX set of record still name 27.36's set.**
  - Line 2057 (27.30): "the TRX set of record is the one 27.36 names".
  - Line 2214 (27.34): "the TRX set of record is the one 27.36 names (27.35's at `94dc5b69` is history since slice Z10)".
  - Line 2241 (27.35): "history since (27.36 names the current one)".
  - Line 2266 (27.36): "SHA-256 `FD6C5BE2...`. The TRX set of record."
  - Fix: point each to 27.37 and `unitgreen-r13-trx-53a210ca`, and mark the `baff082f` set as history.
- **a3. Last-code-head and D-A pointers.**
  - Line 2252: "Slice Z10, commit `baff082f`** (...), the last code head:" has no "until slice Z11 (27.37)".
  - Line 2059: "and since slice Z10 the current one is 27.36's".
  - Line 2193: "(27.35 named the head from slice Z9, and 27.36 names it since slice Z10; ...)".
  - Fix: add the 27.37 / `53a210ca` step to each.
- **a4. Receipt chains stop at slice Z10.**
  - Timing-sweep chains at lines 1737 and 1787 end at "`ug-r13z10-timing-sweep.txt`, 27.36".
  - The store-readback chain at line 1785 ends at "slice Z10 (`ug-r13z10-store-readback-1.json`, 27.36)."
  - Brief line 20 ends "ug-r13z10-store-readback-1.json with ug-r13z10-store-compare-z9-z10.txt none since then, slice Z10 having written none)".
  - Fix: append `ug-r13z11-timing-sweep.txt` and `ug-r13z11-store-readback-1.json` with its compare.
- **a5. "Corrected" statements about the two mapping entries carry no slice Z11 note.** Slice Z11 put both 2026-11-02 sentences back in their first wording.
  - Plan section 6 line 154: "CreateMappingAsync's dated entry corrected".
  - Plan section 6 line 155: "ListMappingsAsync's dated entry corrected". Section 6 also has no slice Z11 bullet.
  - Line 2201 (27.34): "which slice Z9 corrects (finding g2)".
  - Line 2232 (27.35): "g2: `RequirementsWorkflow.CreateMappingAsync`'s dated entry now says ...".
  - Brief UG51: "slice Z9 corrects it".
  - Description line 155: "(slice Z9 corrects it)".
  - Description line 160: "the CreateMappingAsync entry now says ...".
  - Description line 423: its MEMORY-FACT-008 list stops at "Z10 (... both mapping corrections dated ...)". Projected problems item 5 already includes Z11.
  - Fix: add "(slice Z11 restores the first wording of the 2026-11-02 sentence and gives the accurate wording in a 2026-11-05 entry, 27.37)" to each, and add a section 6 bullet for slice Z11.
- **a6. Plan section 4 line 73 stops at 27.36.** It reads "(none in 27.26 or 27.32 to 27.36; ...)" and records none of 27.37's requirement text changes.
  - Those changes are three criteria reworded (TR-MCP-HOSTILEREVIEW-003 AC-001, TR-MCP-NUKE-016 AC-003, TEST-MCP-QBOLLAMA-001 AC-004) and four description or notes fields.
  - Fix: add a sub-bullet listing them, with "no outcome or satisfied flag changed".

## (b) Contradictions

- **b1. Brief UG12 (line 252) says "the TODO tasks to be checked (all 55)".**
  - Against it: brief line 63 says "After AGREE all 56 tasks", plan 27.37 line 2300 says "all 56 tasks", and the store holds 56.
  - Fix: "(all 56)".
- **b2 (material). The records disagree on who decides criterion text.**
  - 27.37 line 2278 rewords three criteria without an operator answer: "the read's "changing their text is the operator's decision" is settled by that ruling". Projected problems item 151, description line 173 and brief line 54 say the same.
  - Other lines still state the opposite as a general rule:
    - Plan 27.26 line 1836: "Changing criterion text is the operator's decision, so they stay as stored".
    - Description line 404: "changing criterion text is the operator's decision, so they stay as stored (declared)".
    - Handoff line 12: "(strengthen criteria first; a criterion change is the operator's decision)".
    - Handoff line 20: "(criterion text is the operator's; declared, 27.26)". The same line says "their text made generic in the 27.37 store scrub".
  - Fix: reconcile everywhere. For example: criterion text is the operator's decision unless an operator ruling already covers the change, as "Scrub everything in PR 87" covers lab names in the in-scope store text; the 27.26 authority citations have no such ruling and stay as stored. Or put the three rewordings to the operator.

## (c) Statements a reviewer could show false

- **c1. The sibling sweep's result is credited to a receipt that says otherwise.**
  - 27.37 line 2281 says "`ug-r13z11-dated-entry-sweep-1.txt` (`146BCECC...`) finds no other entry rewritten in place: the other hits are entries moved into a dated list ...". The 53a210ca commit message, UG54, projected problems item 151 and self-review line 12 say the same.
  - The receipt's own last line is "END MOVED=0 EXTENDED=12 REFLOWED=4 REWRITTEN=20".
  - My read of the 18 hits other than the two mapping entries agrees with the plan's judgment, so the substance holds:
    - 12 single-line entries moved into list form, text unchanged.
    - 3 entries removed together with the members slice U deleted.
    - 2 summaries.
    - 1 hunk that pairs ScriptedInside's kept entry with the removed IsWindowsForm entry.
  - Fix: say those 18 were classified by hand and give each one's class, or save that classification as a receipt.

## (d) Missing disclosures

- **d1. One record of the store trial is unaccounted for.**
  - 27.37 line 2282 and projected problems item 151 say the trial "found 24 hits in 9 records" and that seven records were rewritten.
  - `ug-r13z11-store-lab-names-1.txt` leaves 2 hits, both in TR-MCP-QBRAIN-004. That accounts for eight records.
  - The ninth record and its hits are not named, and the trial has no receipt.
  - Fix: name the ninth record and say why its hits disappeared, or correct the count.
- **d2 (may be held material). Two lab-bound criteria are kept in reworded form, with no stated reason.**
  - The description itself quotes the rule at line 359: "That inherently excludes ENVIRONMENT specific criteria." Section 24 applied it by removing TR-MCP-NUKE-019 AC-4 (provisioning).
  - TR-MCP-NUKE-016 AC-003 ("On the Linux lab host the target leaves a portable Ollama ...") is the same class, and TR-MCP-NUKE-016 has been linked to the TODO since 27.22. TEST-MCP-QBOLLAMA-001 AC-004 is also the same class.
  - No record says why these two stay rather than go, and the store carries no lab-bound flag on either.
  - The thirteenth read also asked for TR-MCP-HOSTILEREVIEW-003 AC-001 to be declared lab-bound. 27.37 rewords it without saying whether it is (judged by its outcome, it is not).
  - Fix: say why the section 24 removal does not apply or put it to the operator, say where the lab-bound flag lives, and state AC-001's status.
- **d3. The fifth exception's basis names the lab itself.**
  - TR-MCP-QBRAIN-006 criterion 3 is now quoted as the basis of the fifth exception: "McpServer_Omarchy and PAYTON-LEGION2 remain ...".
  - TR-MCP-QBRAIN-006 is outside the store sweep's scope (28 linked, 19 mapped tests, 74 cited), so it was neither swept nor scrubbed.
  - No record says this lab-naming criterion lies outside the in-scope store text.
  - Fix: one sentence where the basis is stated.
- **d4. The `docs/Project` projections are only partly declared.**
  - The description (lines 167 and 377) and the body (line 17) say only that the projections are generated from the store and never hand-edited. Line 377 still gives only the two older reasons.
  - Neither carries what 27.37 line 2288 declares: the projections still hold lab-name lines (13, 56 and 20 in three of them) and differ from the store.
  - Neither says why the projections were not regenerated after the scrub through `client.Requirements.GenerateAsync`, the route 27.16 and 27.20 used.
  - Fix: carry the 27.37 declaration into both, with the reason no regeneration ran.
- **d5. No TODO task records the store scrub.**
  - Task 56 was written at 21:05:23, before the scrub writes (about 21:37), and covers slice Z11 only.
  - D-B checks "all 56 tasks", so the seven record rewrites sit under no task.
  - Fix: a notice on task 56, or a new task with the count carried through everywhere.

## (e) Brief claims the evidence cannot support

- **e1 (material). Brief UG52 (line 292) is false at the head.**
  - It says "g2's CreateMappingAsync entry now matches the slice P diff and the upsert".
  - At `53a210ca` the 2026-11-02 sentence again reads "the returned mapping item carries the created mapping's stored creation time instead of the time of the call". A reviewer judging UG52 at the head will FAIL it.
  - UG53 carries a slice Z11 note; UG51 and UG52 do not.
  - Fix: say it matched at `94dc5b69`; since slice Z11 the 2026-11-02 sentence is back in its first wording and the 2026-11-05 entry gives the accurate wording (UG54). Add the same note to UG51.

## (f) Thirteenth-read finding 27.37 says is fixed but is not

- **f1 (material; the thirteenth read's f1, an eighth recurrence of the structural cause in projected problems item 145).**
  - 27.37 line 2292 says f1 is fixed "by a1 to a6 and the open-fix pattern `pattern-r13z11-forward-tails.txt`, ..., generalized to any 27.34 to 27.36 tail that stops before 27.37". Self-review line 28 says "Those are now carried through 27.37, 56 tasks and 53a210ca".
  - The pattern file only holds these forms:
    - "since 27.3[456], all 5[345]"
    - "and of 27.3[56], all 5[45]"
    - "and then all 5[45]"
    - "since 27.3[56], at"
    - "since 27.3[56] they are"
    - "which names the current last code head"
    - "carried through 27.3[56]"
    - "through 27.36, 55 tasks"
    - "now carried through"
  - So it cannot match "27.36 (all 55 tasks)", "55 since 27.36", "27.36 to 55", "27.36 with 55", "55 since slice Z10, 27.36", "the one 27.36 names", "is 27.36's", "names it since slice Z10" or "The TRX set of record.".
  - My stale-tail search found 28 such matches on lines that never mention 27.37, 56 or `53a210ca`: the 24 plan lines of a1 to a4 plus brief UG12.
  - Fix: apply a1 to a4 and b1. Widen the pattern to any line that names 27.36, 55 tasks or `baff082f` without 27.37, 56 or `53a210ca` on the same line. Correct the two claims.

## (g) Slice Z11, the store text and the classification

- None found:
  - Slice Z11 meets MEMORY-FACT-008 as 27.37 reads it: earlier entries kept, canonical id only, wording that matches slice P.
  - After the scrub, the seven records hold no lab-name match, and the only in-scope hits left are the two kept in TR-MCP-QBRAIN-004.
  - No classification hit is wrong, and no lab name is missed in the touched non-docs files.

## Scripts

Everything I wrote is in `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13n/`. It is all read only and passed the analyzer before it ran. Bash only launched `pwsh -NoProfile -NonInteractive -File` with literal arguments.

- **My own scripts:** Save-RequirementReadN.ps1, Find-StaleTailsN.ps1, Compare-ScrubReadbackN.ps1, Find-InFolderN.ps1, Find-BroadLabTokensN.ps1, Show-TodoSummaryN.ps1 and Show-ManifestTimesN.ps1, with pattern files.
- **Copies of holistic-r13m scripts with the output folder changed:** Show-ReceiptSummaryN.ps1, Test-ManifestN.ps1 and Get-PrStateN.ps1.
- **Shared helpers run unchanged:** Get-GitFactsM, Get-GitRangeDiff, Show-RangeChangedFiles, Find-TextInFiles, Show-MatchContext, Get-FileSha256, Measure-FileChars, Find-FilesByName and Get-Requirement.

Nothing tracked changed, no secret was printed, and I did not contact PAYTON-LEGION2.
