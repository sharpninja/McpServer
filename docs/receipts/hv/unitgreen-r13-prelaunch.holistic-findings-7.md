# Seventh holistic read before hostile validation round 13

The read-only subagent report of session ClaudeCode-20261102T182906Z-plugin-session, request req-20261105T113100Z-holistic-r13g (parent turn req-20261104T231251Z-continue-pr87-completion-2), as the session transcript holds it. Exported 2026-11-05 by Export-SubagentReport.ps1, because the harness refused the subagent's own findings file. Report text unchanged; the harness indentation is removed.

2026-11-05T05:59:01 (measured local stamp; turn completed at 06:00:21)

**Session `ClaudeCode-20261102T182906Z-plugin-session`, request `req-20261105T113100Z-holistic-r13g`.** The turn was opened, updated twice and completed through the plugin scripts; the readback shows status completed, 9 actions, 2 decisions. The harness refused `findings-r13g.md` ("Subagents should return findings as text, not write report files"), so the full findings are below. It accepted the turn response file `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13g/turn-response-r13g.txt`.

**Counts:** (a) 6, (b) 3, (c) 7, (d) 3, (e) 2, (f) 1, (g) 1. Total 23. One is material: g1. Finding g1 needs a code change, so the last code head moves and every gate reruns.

**What checked clean:**
- Git: HEAD and both origin branches are `1bb0d61f`, with no commit after it. `52e6d627..4398cffc` touches 27 code paths (7 in src, 20 in tests). `4398cffc..1bb0d61f` changes one comment line in each of 4 files, so part 2 is comment text only.
- Hashes: every pinned hash in `ug-r13z4-mockgate-a-1`, `-b-1`, `binding-a-1`, `binding-b-2` and `ug-r13z4b-binding-1` equals the blob at its stated commit. Every receipt hash prefix 27.30 cites matches its file.
- TRX manifest: SHA-256 `5AC0AD97...`, 34 files. Every hash matches, every head is `1bb0d61f`, every file has 0 failed and 0 not executed. Counts are 4820, 4820, 741, 718, 563 and 467.
- Linux gate receipts at `1bb0d61f` list only untracked paths.
- The by-name unit comparison shows 81 new cases, split 20 + 9 + 49 + 3.
- The store comparison shows two appends plus tasks 47 and 48; its script also compares the TODO fields, so "nothing else" holds.
- The service-update chain and the SQL Server durations (7.63 s and 6.99 s) match the records.
- Live reads: the TODO has 48 tasks, none checked. GitHub shows PR 87 in draft.
- All ten pinned at-threshold verdicts match their production comparisons. No unit case leaves a checked decision on the system clock among the constructions of the clock-bearing services.
- `SettableTimeProvider` refuses non-zero offsets, the stopwatch and timers.
- The database resolver reads only `Mcp:` keys, `ConnectionStrings` and flat `MCP_`/`QBRAINAI_` names, so the scrub covers it.
- There are five `TryAddSingleton(TimeProvider.System)` registrations, as stated.

## (g) Code or tests that do not meet ac-4 or AC-006 as stored (material)

- **g1, unrelated case-variant variables are not kept unchanged.**
  - **Code:** `tests/TestSupport.Shared/OwnedInstanceProcess.cs:39-45` copies the inherited environment into an ordinal map. `TemporaryInstanceChecks.BuildOwnedInstanceEnvironment` (`TemporaryInstanceChecks.cs:198-203`) then writes it into `new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)`.
  - **Effect on Linux:** the process environment is case-sensitive there. Two unrelated inherited names that differ only in case (for example `http_proxy` and `HTTP_PROXY`) collapse into one entry: the earlier name's spelling with the later value. Which value survives depends on the inherited enumeration order.
  - **Stored ac-4 says:** "No inherited spelling ... in either enumeration order of the inherited environment ... ; unrelated variables are kept unchanged". Round 12's proposed test also asks that "unrelated variables remain intact". The code meets neither, so R12-F1's closure is incomplete.
  - **The contract cannot catch it:** `BothOrders`, `Child` and `OwnedWith` in `OwnedInstanceKeyIdentityContractTests.cs` build ignore-case maps, so no row can hold a case-variant pair.
  - **The case clause of the refusal is unreachable:** the check runs on the collapsed child, so "two names of one product key that differ only in ... case" can never be refused, and no row has such a pair. Row "two owned names for one key" differs in separators only.
  - **Records:** the 26.3 stated limit ("on Linux two inherited non-product variables whose names differ only in case collapse to one") predates the 2026-11-05 ac-4 text and is not re-declared in 27.30.
  - **Proposed fix (code):**
    - Build the child with an ordinal map, so inherited names survive exactly and only product keys are removed. Apply the two-name rule over the ordinal key set.
    - Add contract rows: an unrelated pair `http_proxy`/`HTTP_PROXY` kept in both orders; a case-only pair of one product key refused; a wrong builder "collapses case variants" rejected.
    - Run the mock gate, then the binding, then the full gates.
  - **Alternative:** narrowing ac-4 is a criterion change, which is the operator's decision. It is not the recommended option.

## (a) Superseded statements with no forward note

- **a1.** Plan line 239 (section 13 D-B) ends "Superseded again in 27.29: ... every gate it names is the gate at the last code head `52e6d627` that 27.29 records". It has no 27.30 note. Only D-A (line 238) was updated, although self-review line 13 says section 13 was.
  - Fix: append "Superseded again in 27.30: as amended by 27.22 to 27.30, ... the last code head `1bb0d61f` that 27.30 records".
- **a2.** These task-count chains end at 46:
  - line 456: "since 27.29, all 46."
  - line 1370: "46 since 27.29"
  - line 1504: "27.29 (all 46 tasks; ...)"
  - line 1542: "27.29 (all 46)"
  - lines 1574 and 1681: "46 since 27.29"
  - line 1751: "27.29 with 46"
  - lines 1801, 1847 and 1897: "46 since slice Z3 part 3, 27.29"
  - line 1948: "Superseded by 27.29: ... all 46 tasks are checked"
  - line 2002 (the 27.29 D-B): "with all 46 tasks checked after AGREE". It does not point to the round 13 D-B at line 2056.
  - Fix: append "48 since 27.30" to each, and at line 2002 add "superseded by the round 13 D-B of 27.30".
- **a3.** These pointers still name `52e6d627` as current:
  - line 1104: "since 27.29, at `52e6d627`."
  - line 1543: "27.29 now records the gates of record, at `52e6d627`"
  - line 1590: "which names the current last code head `52e6d627`"
  - Fix: add "since 27.30, at `1bb0d61f`".
- **a4.** Receipt history chains stop at round 12:
  - line 1733: "... and since slice Z3: `ug-r12z3-timing-sweep.txt`, 27.28"
  - line 1783: "... and `ug-r12z3-timing-sweep.txt`, 27.28"
  - line 1781: "... slice Z3 part 3 (`ug-r12z3c-store-readback-1.json`, 27.29 ...)"
  - Fix: append the `ug-r13z4-timing-sweep.txt` and `ug-r13z4b-store-readback-1.json` steps (27.30).
- **a5.** These lines still treat the PR as ready the whole time:
  - line 2003: "PR 87 has been ready for review since 2026-11-03 09:59:32."
  - line 1898: "with the PR ready for review since 2026-11-03 09:59:32"
  - line 1950: "PR 87 is already out of draft (27.24)."
  - line 1370: "PR 87 had in fact left draft at 2026-11-03 09:59:32 ... (27.28)"
  - Fix: append "(until 2026-11-05 05:08, when it returned to draft, 27.30)".
- **a6.** Brief line 21 calls `ug-r12y-citation-sweep.txt` "the citation sweep receipt" with no history mark; the r12z3c and r13z4 sweeps supersede it. The same item's store-readback list starts at `ug-r12z3c-store-readback-1.json` and omits `ug-r13z4b-store-readback-1.json` and its comparison.
  - Fix: name the r13z4 sweeps as current, mark the older ones as history, and put the ug-r13z4b pair first.

## (b) Contradictions

- **b1.** Plan line 2057 reads "Round 13 launches only after the gates are green at `1bb0d61f`, a seventh holistic read over these records and the records commit". That omits the ready mark, which line 2044, the handoff HV order (line 12) and the brief SCOPE all require. TODO tasks 47 and 48 (live) also end "...the records commit; then round 13 on Codex" with no ready step.
  - Fix: add the ready mark to line 2057, and a plugin notice on task 48.
- **b2.** "The five production files" names two different sets.
  - Plan line 2027 says "the five production files pinned as they were before the change". mockgate-b-1 pins SessionLogService, GraphRagService, FederationEnvelopeSigner, TransactionSecurityServices and MemoryConsolidateOperations.Locks.
  - Brief item 10 and section 6 name "the five production files with the new optional clock". That set includes MemoryConsolidateOperations.cs and RunConsolidateJobCommandHandler.cs, which no receipt pins. Section 6 line 148 counts seven src files.
  - Only mockgate-b-1's dirty paths show those two files unchanged before the change.
  - Fix: "five of the seven changed production files are pinned; the other two are shown unchanged by the receipt's dirty paths".
- **b3.** Self-review line 9 says "unrelated inherited variables, duplicates included, kept unchanged". That contradicts the plan's 26.3 stated limit (line 1153), which the code matches.
  - Fix: follow g1, or state the limit there.

## (c) Statements a reviewer can show false

- **c1.** Plan line 149 says "(only `QBrainAi.Client`, unchanged, has a PackageId)". QBrainAI.Cqrs, Cqrs.Mvvm, McpAgent, QBAgent (a tool), Repl.Core, Repl (a tool) and seven SharpNinja.McpServer.* projects also carry one. The conclusion still holds: the three assemblies that hold the changed classes have none.
  - Fix: say exactly that.
- **c2.** Plan line 2032 and projected problems item 123 say "27.28 line 1913 carries the forward note". The note is at line 1917; line 1913 is "Third run, after the health-wait fix".
- **c3.** Plan line 2032 says "the other first-receipt claim (27.24, line 1776) holds". That claim is in 27.25, at line 1780.
- **c4.** Self-review line 17 says "binding ug-r13z4-binding-a-1 (80/0/0) with the contract hash unchanged". The contract file was `37694B16` at the mock gate and `D36DE8AC` at the binding (the 4398cffc blob): the two binding facts were appended to the same file.
  - Fix: declare that change, rows and assertions unchanged, and show it (the 18 mock-gate names are a subset of the binding TRX).
- **c5.** Four records say every changed member carries a dated change log: the 4398cffc commit message ("Every changed member and class carries a dated change log"), PR body line 291, projected problems item 5 and brief UG47. Seven changed members have none of their own:
  - FederationEnvelopeSigner.Sign and Verify (`<inheritdoc />`)
  - InMemoryKeyServerService.SignManifestAsync and VerifyManifestAsync (`<inheritdoc />`)
  - SessionLogService.QueryAsync (`<inheritdoc />` plus an inline `//` comment)
  - MemoryConsolidateOperations.ConsolidateAsync and RunJobAsync (summary only)
  - The logs sit on the constructors. Fix: declare that and correct the four claims; adding member logs is a code change.
- **c6.** The records give 05:08:09 as the return to draft (plan lines 1749, 2044 and 2065, brief SCOPE and UG41, PR body line 5, handoff line 14, projected problems line 3 and item 130, self-review line 3). GitHub's convert_to_draft event is 2026-11-05T11:08:10Z, which is 05:08:10 local, and UG41 claims the history "as GitHub shows it".
- **c7.** Self-review line 13 says "Forward notes added by hand for every statement that 4398cffc and 1bb0d61f supersede". Findings a1 to a5 show that is false.

## (d) Missing disclosures

- **d1.** Section 6 line 150, projected problems item 121 and PR body line 248 describe the slice Z4 test-support change as refusing an unowned or duplicated product key before start. In the start path the builder runs first, so inherited spellings are removed, not refused.
  - What actually changes for every temporary instance: inherited colon or mixed-separator spellings of product keys, and every inherited Kestrel endpoint setting (also under ASPNETCORE_ and DOTNET_), are now stripped silently.
  - The refusal is reachable only through the instance's own settings.
  - Fix: state both.
- **d2.** `ug-r13z4-configured-durations-compare-z3-z4.txt` compares only the rows of 60 s or less. Hits rose from 302 to 314 and longerThanMax from 53 to 59, so six additions over 60 s are unstated; "six added" covers the listed rows only.
- **d3.** 27.30 line 2028 and brief UG47 list the services that read the new clocks. The D3 and D6 bindings instead use the pre-existing clocks of WorkspaceValidationService and HandoffIngestionService, which needed no production change. Only the sweep report says so.

## (e) Brief claims the evidence cannot support

- **e1.** UG47 says binding-a-1 and binding-b-2 "ran the same rows ... with the contract hashes unchanged". This is false for a-1 (see c4).
- **e2.** UG26 says `ug-r13z4-citation-sweep-all-3.txt` covers the lines "on the 1bb0d61f tree". The receipt records HEAD 4398cffc with no dirty-path list or source hashes, and the part 2 edits were still uncommitted at 04:33:49.
  - Fix: rerun the read-only sweep on the committed tree, or say "the working tree that became 1bb0d61f".

## (f) A round 12 finding 27.30 says is fixed but is not

- **f1, R12-F3.** Projected problems line 26 carries round 12 items 63 to 118 "as written", adding only a readback to item 89. Item 89 (`unitgreen-r12-prelaunch.projected-problems.md:35`, the location R12-F3 cited) still says "ug-r12z3-store-readback-2.json (the first receipt that holds TR-MCP-SERVICEUPDATE-001)". Item 123 and plan line 2032 say this was corrected.
  - Fix: on line 26, add "and item 89's first-receipt claim, which item 123 corrects (R12-F3)".

Everything I wrote, including the scripts and query outputs, is in `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13g/`. Nothing tracked was changed.
