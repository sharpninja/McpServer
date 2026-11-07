Eleventh holistic read before hostile validation round 13 (read-only subagent, session ClaudeCode-20261102T182906Z-plugin-session, turn req-20261105T221700Z-holistic-r13k). The harness refused the subagent's findings file, so this text is exported from the parent session transcript, unchanged except for the harness indentation.

2026-11-05T17:45:54 (measured local stamp)

# Eleventh holistic read before hostile validation round 13

This was a read-only review. I changed nothing tracked. It found 24 problems. The material ones are g1 (with b1 and d1), a1, c1 and c2.

**Counts:** (a) 7, (b) 1, (c) 7, (d) 4, (e) 1, (f) 2, (g) 2.

**Session log:** session `ClaudeCode-20261102T182906Z-plugin-session`, request `req-20261105T221700Z-holistic-r13k`.
- I opened it with Begin-Turn.ps1, updated it three times and completed it with Complete-Turn.ps1.
- Readback: completed, 8 actions, 4 decisions. Response file: `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13k/turn-response-r13k.txt`.
- The harness refused `findings-r13k.md` ("Subagents should return findings as text"), so the full findings are below.

## What checked clean

- **Git:** HEAD and both origin refs are `becb0742`. `becb0742..HEAD` is empty, and the worktree has no tracked change (only untracked receipts).
  - `1d77a79c..b21c21ec` touches 12 test files. `b21c21ec..becb0742` adds 2 comment lines to `RequirementsWorkflow.cs`.
  - Commit times are 16:09:41 and 16:24:00. Both commits carry the marker's signing line and no gpgsig.
- **FederationEnvelopeSignerTests:** only these lines changed, and every Assert line is unchanged:
  - the four Sign/Verify id arguments and the `Operation()` ProxyId;
  - three replaced documentation lines and one new class remark item.
  - `Operation()` is called only by the two D4 bindings, and its body is the base64 of `{}`.
- **The nineteen new summaries:** I read each against its body. None says more than the code does.
- **Cited ids:** every id the new entries cite is canonical and in the store (TR-MCP-USECASE-018 AC-006, FR-MCP-TESTRESOURCE-001 ac-4, FR-MCP-103, FR-MCP-040 ac-1).
- **Manifest `BFFB1C9F`:** 34 files, every head `becb0742`, every hash equal, 0 failed, 0 not executed.
- **Gate receipts** at head `becb0742`, each with 0 tracked dirty paths and the times plan 27.34 states:
  - Linux unit `7E547265`, Linux lane `122D055F`, Linux Build.Tests `4DF07774`.
  - The Windows unit, lane and Build.Tests results match. The lane's proof head is `becb0742`, session 1, the runner hash matched, and the task was removed.
  - Every by-name compare shows 0 only before and 0 only after.
- **Compile and binding receipts:** `6AE8ED1D`, `42EFBAF8`, `D2430092` (432/0/0) and `3B2F0C82` (77/0/0). Their pinned hashes equal the 13 files at `becb0742`.
- **Roslyn sweep `B20B5EDF`:**
  - NODATE 29: 25 with typeDated True, and 4 False (StartupHealthInterval, StartupHealthProbes, ListMappingsAsync, CreateMappingAsync).
  - UNDOC-PRE-RULE 365. Both CHANGED counts 0. UNDOC-SINCE-RULE 6, all local functions.
  - The 25 type-dated members' type entries do describe their changes.
- **Store readback and sweeps:**
  - Readback `2DE679E9` and compare `CFE677C0`: 53 tasks, no criterion change.
  - Timing sweep `EB6A7A0B`: 211 hits, 0 unclassified, 0 unused, ORDER=5.
  - Configured durations: 314 hits, 255 listed. Citation sweep `CC605E65`: 98 ids, 39 absent.
  - Tenth-read export `4CC0F70E` matches.
- **d3 numbers:** they match the z6 and z7 receipts and the held receipt `70DEC943` (gapA held for 27 minutes, 49.4 a minute; gapB 18.4; the lanes 26.8 and 4.1).
- **PR 87:** draft at `becb0742`. The ready_for_review (09:59:32) and convert_to_draft (05:08:10) times are consistent with the records.
- **Format:** no en or em dash and no table row in the six records.
- **Tenth-read findings:** a1 to a5, b1, c1, c3 to c6, d1 and g1 are fixed where 27.34 says. c2 and e1 are only partly fixed (f1 and f2).

## (a) Superseded statements with no forward note

- **a1 (material).** Plan section 6, line 148, has no slice Z8 entry.
  - It reads: "Added in round 13 (27.30 to 27.33, ..., and slices Z5 to Z7), from git (`git diff 52e6d627..1d77a79c -- src build .nuke` lists eight src files ...), the last code head `1d77a79c`".
  - `git diff --name-only 52e6d627 becb0742 -- src build .nuke` lists nine src files: Z8 part 2 added `RequirementsWorkflow.cs`, and Z8 changed 12 test files.
  - Fix: extend the entry to 27.34, `becb0742` and nine files, with a Z8 test line and a Z8 part 2 documentation line.
- **a2.** Plan 27.22, line 1591: "27.33 (slice Z7), which names the current last code head `1d77a79c`".
  - Fix: append "then 27.34 (slice Z8 and its part 2), which names the current last code head `becb0742`".
- **a3.** Plan 27.24, line 1750: "the ready step after the gates at the last code head (`1d77a79c`, 27.33; ...) is a real step."
  - Fix: name `becb0742`, 27.34.
- **a4.** Plan 27.24, line 1752: "27.32 with 51 and 27.33 with 52 (ninth holistic read finding a5)."
  - Fix: add "and 27.34 with 53".
- **a5.** Plan section 4, line 73: "The criteria added or strengthened in 27.1, 27.12, 27.19 to 27.25 and 27.27 to 27.29 (none in 27.26)."
  - This omits the 27.30 appends (ac-4 at 02:50:54 with its narrowings, and AC-006 at 03:11:51) and the 27.31 ac-4 append at 06:17:32.
  - Fix: "27.27 to 27.31".
- **a6.** PR body line 69: "`62c5882d`: dated XML change logs per MEMORY-FACT-008 on every member and test class slices Q, R and P changed".
  - The same claim at plan 27.20 now has its forward note; this copy has none.
  - In fact the slice P and R fields had no entry until Z7, and ListMappingsAsync and CreateMappingAsync none until `becb0742`.
  - Fix: add the forward note.
- **a7.** Brief UG36: "every member and test class changed by slices Q, R, P, S and T carries XML documentation with a dated change log ...".
  - It has no forward note for Z7 (the fields) or Z8 part 2 (the two methods).
  - It does not mention the 25 members whose dated entry sits on their type.
  - Fix: add both, as UG47 now does.

## (b) Contradictions

- **b1 (pairs with g1).** The records restate plan 27.7's scrub as covering changed lines only:
  - Plan 2193: "unchanged by this pull request, outside the changed-line scope of the 27.7 scrub".
  - Projected problems item 145: "(the 27.7 scrub covers changed lines)".
  - PR body 320, handoff line 19 and brief UG51 say the same.
  - Plan 27.7 (line 1292) says "Every lab name ... in every file this branch touches ...: replaced by generic statements", and the operator's ruling is "Scrub everything in PR 87".
  - Fix: either scrub the files (g1) or get an operator ruling on exceptions. Then quote 27.7 as written.

## (c) Statements a reviewer can show false

- **c1 (material).** Plan 2196, brief UG51 and the `b21c21ec` message say: "eleven files documentation only (96 lines added, none removed)".
  - In the receipt, the eleven documentation-only files add 92 lines. 96 is its total over all twelve files ("END doc=96").
  - FederationEnvelopeSignerTests adds 9 lines (5 code, 4 documentation) and removes 8 (5 code, 3 documentation).
  - Fix: "92 lines added; FederationEnvelopeSignerTests 9 added, 8 removed (96 documentation lines in all)". Declare the commit message, which stays as pushed.
- **c2 (material).** Several records claim each new entry cites "only the ids its class entry cites": plan 2196, UG51, self-review line 18 ("copies its class entry's ids"), TODO task 53 and the `b21c21ec` message.
  - This is false for three helpers:
    - `RequirementsWorkflowTests.CreateMockSelectionState`: the class summary (`:15-23`) cites no id.
    - `TodoWorkflowTests.CreateMockSelectionState`: the class summary (`:8-15`) cites no id.
    - `StubAuthRotationHandlerTests.CreateMarkerData`: its class (line 7) has no XML documentation at all.
  - Their TR-MCP-USECASE-018 AC-006 comes from the slice U methods and fields of the same classes (`RequirementsWorkflowTests.cs:852` and `:869`, `TodoWorkflowTests.cs:349`, `StubAuthRotationHandlerTests.cs:10` and `:229`). The id is canonical; the stated rule is not what was done.
  - Fix: reword the rule in each record and add a notice on task 53.
- **c3.** Plan 2193 and UG51 say the older lab names are "unchanged by this pull request".
  - `Verify_RejectsExpiredEnvelope` (`FederationEnvelopeSignerTests.cs:105-120`) was changed in slice Z4 (line 114) and holds PAYTON-LEGION2 at lines 111 and 113.
  - Every case of the class also runs through `CreateSigner`, which slice Z4 changed.
  - Fix: say only the lab-name lines are unchanged.
- **c4.** Self-review line 34 says: "Slice Z8 part 2 ...: the task 53 notice first (16:18:25)".
  - `RequirementsWorkflow.cs` was last written at 16:17:54, before the notice's dry run (16:18:11) and write (16:18:25). The notice text is stamped 16:17.
  - Fix: correct the order.
- **c5.** Self-review line 18: "only documentation added outside the five FederationEnvelopeSignerTests id lines, nothing else removed".
  - Three documentation lines were removed (the old `Operation()` summary, returns and remarks).
- **c6.** "365 undocumented member declarations" (plan 2194, UG51, item 145, PR body 320, handoff).
  - Ten of the 365 are local functions (receipt lines 51, 116, 207, 242, 347 to 351, 377).
  - Fix: "355 members and 10 local functions".
- **c7.** Self-review line 21 says the forward notes "are now carried through 27.34, 53 tasks and becb0742". a1 to a7 remain.

## (d) Missing disclosures

- **d1 (material, pairs with g1).** Three more federation test files this PR touched name lab machines (PAYTON-LEGION2, PAYTON-DESKTOP), and no record says so:
  - `FederationControllerTests.cs`: 14 lines (132 to 663, among them the `Assert.Equal` at 349).
  - `FederationFanoutSyncServiceTests.cs`: 14 lines (34 to 233), inside the five methods whose signer line slice Z4 changed.
  - `MemoryFederationStateAdapterTests.cs`: 9 lines (251 to 370).
  - Slice Z4 changed all three, and slice Z8 changed MemoryFederationStateAdapterTests again. The 27.34 sweep read added lines only.
  - My sweep over every src, tests and build file in `9b099716..becb0742` (`holistic-r13k/lab-names-touched.txt`) found 58 hits in 9 files, 0 on added lines. 49 are PAYTON names in the four federation files. The rest are the four flagged exceptions and published sharpninja identities.
- **d2.** The task 53 notice text is stamped "16:17" and was written at 16:18:25. 27.31 to 27.33 state the stamp against the write for every notice; 27.34 does not.
- **d3.** The two comment-only receipts ran on uncommitted trees at HEAD `1d77a79c` and `b21c21ec`, and neither pins hashes. 27.33 said so for Z7; 27.34 does not. The compile and binding pins tie the tree to the blobs, so the outcome holds.
- **d4.** The operator-ruling lists stop at 27.33:
  - the brief's OPERATOR DIRECTIVES;
  - the PR body heading "Operator rulings (plan 27.19 to 27.33)";
  - handoff line 11.
  - None says that no answer was asked after the tenth read.

## (e) Brief claims the evidence cannot support

- **e1.** UG49 grounds the "rate separated" on `ug-r13z7-win-qbrainai-event-windows-1.txt`. That receipt's gapA rate is 16.1 a minute over the full 83 minutes, the understated basis. Only UG51 cites the held receipt.

## (f) Tenth-read findings 27.34 says are fixed but are not

- **f1 (c2).** 27.34 line 2203 lists c2 as fixed "at 27.33 ... the PR body".
  - Plan 2158 still says "2 local functions", with no mention of six.
  - PR body line 147 still says "the two local functions are covered by their methods' entries".
- **f2 (e1).** 27.34 declares that Z7's receipts pinned no hashes, but UG50 still says Z7's compile and binding "ran on the final tree". The receipts show head `88c5abcc` with the 18 files modified and no hashes, so they cannot support that.

## (g) Slice Z8 code against MEMORY-FACT-008 or the no-lab-names rule

- **g1 (material).** Two files slice Z8 changed still name lab machines in test code:
  - `FederationEnvelopeSignerTests.cs`, 12 lines: 32, 36, 38, 53, 56, 73, 76, 93, 95, 97, 111 and 113.
  - `MemoryFederationStateAdapterTests.cs`, 9 lines.
  - Slice Z8's own class remark cites the 27.7 scrub. None of these lines is one of the four flagged exceptions.
  - Fix: generic proxy ids in all four federation files with the assertions unchanged. That is test-only but moves the code head, so the gates rerun. The alternative is an operator ruling on exceptions. Either way, correct b1, c3 and d1.
- **g2 (low).** `RequirementsWorkflow.cs:465`: "instead of the time of the call".
  - Before slice P the adapter returned `UtcNow` each time `CreatedAt` was read, not the time of the call.
  - `CreateMappingAsync` upserts into any existing mapping (`:520-537`), so the time is that mapping's stored, earliest link time, not a newly created one's.
  - Fix: "the upserted mapping's stored creation time instead of the time its CreatedAt was read".

**Scripts.** Everything is in `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13k/`. All scripts are read only and were analyzer clean before they ran.
- **New scripts:** Get-GitFactsK.ps1, Test-ManifestK.ps1, Show-ReceiptSummaryK.ps1, Find-LabNamesInTouchedFiles.ps1, Show-RangeHunks.ps1, Find-StalePointers.ps1 and Get-PrStateK.ps1.
- **Get-PrStateK.ps1 has a defect:** its UTC column double-converts the dates, so its times read six hours off. The event order and the records' local times still hold.
- **Bash rule:** Bash only launched `pwsh -File`. A few early Find-TextInFiles calls carried a `|` inside a quoted regex argument. No shell pipe ran, and later calls used pattern files.
- I made no contact with PAYTON-LEGION2 and printed no secret.
