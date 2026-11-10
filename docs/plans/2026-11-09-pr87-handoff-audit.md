# PR87 handoff audit

Author: Codex. Observation window: 2026-11-09T09:12:58 to 2026-11-09T09:20:03 local.

Audited document: `docs/plans/2026-11-09-unit-suite-green-codex-handoff.md`.
Audited code head: `03eea74797827aacf7030ac577cc19d46ebee5d2`.
Checkout at audit start: `c8b231c13164956ac98d6234fb2c6fb459c0cefb`.
QBrain session: `Codex-20261109T150912Z-plugin-session`.
Audit turn: `req-20261109T151258Z-turn-5649`.

## Conclusion and limits

The handoff's principal repository, test-result, task-count, quarantine and unfinished-record claims are supported by the inspected artifacts. It is a credible recovery document. It does not establish that PR87 is ready for acceptance or merge. In particular, zero failing TRX results does not establish that every test made a meaningful assertion, and its reconciliation still reports FAIL.

This is a source and evidence audit, not a hostile acceptance review, a new test run, or an assertion of end-to-end parity for every moved case. The original host executions were not repeated. Historical operator dialog, every session-log closure and the claimed weekly-limit event were not independently authenticated. No implementation, TODO, requirement, PR or branch mutation was performed for this audit. The audit receipt and this report are new documentation.

There are 19 verified claim groups, one current-state drift, and four explicitly unverified claim groups below. No fabricated gate counts or missing manifest artifacts were found. The incomplete records and vacuous-test risk are defects already disclosed by Claude, not evidence that the reported TRX counts were fabricated.

## Verified claim groups

1. **Branch, parent and docs-only handoff commit.** `git rev-parse --abbrev-ref HEAD` returns `fix/unit-suite-green-20261019`. `git log -4` places `c8b231c` directly after `03eea747`. `git diff-tree --no-commit-id --name-only -r HEAD` lists 73 paths, all under `docs/`: 33 in the round15 TRX evidence directory, 38 other gate receipt paths, and two handoff documents. Thus the 71 receipt paths claimed in the handoff really were committed. `git status --short` was empty before this audit wrote its own receipt.

2. **PR metadata.** A live GitHub `get_pr_info` read for `sharpninja/McpServer#87` returned open, draft, mergeable, unmerged, base `develop` at `9b099716e3f4b773787a1caef44353e112bda556`, head `c8b231c`, and the title quoted by the handoff. The live PR body still describes the round14 evidence and is stale relative to the handoff. This is independent of local tracking refs.

3. **Commit sequence.** `git log f9b90579..03eea747` contains all 14 commits listed in handoff section 5, in the stated ancestry order. The final changes include project separation, renames, quarantine, helper reference and runtimeconfig repair. This confirms the sequence, not every historical procedure claim about those commits.

4. **Manifest integrity.** The round15 manifest SHA256 is exactly `572FD28A3D60E93BBD92357D002B9C490A5A4192A60AB98E11B9AEA82D94DDCC`. All 32 entries in its `files` array match their recorded hashes and lengths: 28 TRX plus four selection JSON files. Total bytes are 18,777,067. The 28 TRX alone total 18,769,924 bytes; confusing these denominators would produce a false mismatch.

5. **Retained originals.** All 32 `files[].sourcePath` originals exist and match the committed copies by SHA256, including the Windows artifacts reachable through the mounted source share. No original was moved or removed by this audit.

6. **Unit gates.** Parsing the actual 14 unit TRX files, including result rows rather than trusting summary JSON, gives 4,835 passed on each host, zero failed and zero not executed. Per host: Client 311; Cqrs 33; Launcher 20; McpAgent 64; QBAgent 90; Repl.Core 1,035; Support.Mcp 3,282. All result rows are Passed, and result-row counts equal TRX totals.

7. **Integration gates.** The actual 12 lane TRX files give Linux 743 passed and Windows 720 passed, zero failed/not executed. Linux/Windows project totals: EnvironmentIntegrationTests 456/444; McpAgent 1/1; QBAgent 3/3; NukeBuild 96/85; Repl 181/181; Support.Mcp.IntegrationTests 6/6. All result rows are Passed.

8. **Infrastructure and traceability gates.** Both Build.Tests TRX contain 530 passed and zero failed/not executed. Both traceability receipts report exit zero; the Linux exit file and output agree. The Windows output explicitly reports `UseCaseFrLinks coverage source: none (no SQLite path resolved)`. Therefore a successful traceability target is verified, but it is not proof of complete live-store traceability coverage.

9. **Windows recheck and interactive lane evidence.** The extra recheck TRX hashes to `7B3B919CE3B58366596E881FA2565C3876AC303465601D5A54828BF50A2A4954` and contains five passed results. The retained Windows lane start/result receipts pin `03eea747`, the expected runner hash, `interactive=True`, session 1, exit zero, and subsequent scheduled-task removal. These are retained execution receipts, not a fresh Windows process inspection.

10. **NuGet warning receipts.** Linux's `ug-r14f4-final-linux-nu190x-2.txt` reports zero matches and zero missing logs across four gates. Each of the four Windows `ug-r14f4-final-win-*-2.nu190x.txt` receipts reports zero. This verifies the retained scans; it is not a current package-advisory refresh.

11. **Live TODO state.** Codex plugin `workflow.todo.get`, `IMPL-UNITGREEN-001`, returns 72 tasks, 45 done, TODO not done. Open positions exactly match the handoff: 8, 10, 12-15, 21-25, 29, 30, 35, 40, 60, 62-72. Tasks 66 and 67 still name the pre-rename projects. No task explicitly covers quarantine AC-8. This read did not alter task state.

12. **Live requirement state.** Plugin getters confirm TR-MCP-NUKE-019 has AC-1, AC-2, AC-3, AC-6, AC-7 and AC-8, all unsatisfied, with ownership and quarantine wording. TR-MCP-USECASE-018 AC-006 is unsatisfied. The effective requirements read identifies active `layer-1`, with 399 FR, 548 TR, 622 TEST and 399 mapping records. FR-MCP-004 currently has no acceptance criteria; its citation alone does not supply an independently pinned embedding acceptance contract.

13. **Recorded S0 rewording counts.** Scratch `records-r14/s0-reword/receipt-writes.jsonl` contains 253 rows over 113 distinct record IDs: 43 text, 154 evidence, 47 description and nine notes fields; every row reports `readbackOk=true`. This authenticates the retained write receipts' counts, not an independent present-day re-comparison of every before/after value.

14. **Layout, selection and quarantine source.** `build/IntegrationLaneProjects.cs` declares seven unit-owned projects, eight integration-owned projects including two held out, and Build.Tests as infrastructure. It selects the six lane projects in the handoff's order and restricts Support.Mcp.IntegrationTests to two classes. `build/Build.Test.cs` uses `Category!=AiReview&Category!=Integration&Quarantine!=NoMatchingCriterion`; `build/IntegrationLaneFilter.cs` appends the same quarantine exclusion. Source search finds exactly 22 method quarantine attributes, including three in QBAgent. The recorded reconciliation identifies 29 excluded cases per host, none executed in the new gate sets. TestGroups remains a separate class-selection path.

15. **Windows source fixes.** The environment integration csproj references ProcessTree.TestHelper. `Documentation/BugTriage139SixteenthWindowsProcessTests.cs:30` derives its runtimeconfig filename from the running assembly and uses that value at all three call sites. The five-case Windows recheck and complete lane provide execution evidence for these source changes.

16. **Scratch recovery artifacts.** The fact sheet exists at 16,183 bytes. The draft plan exists at 924,141 bytes and 2,541 lines. Its change log has 14 lines. All six h1-h5 edit-data files named by the handoff exist, including both h3 outputs. The detailed receipt records their hashes. The draft contains section 27.46 and unresolved FILL tokens; the tracked plan ends at 27.45. Thus “applied” in handoff section 7 means applied to the scratch draft, not to the tracked plan.

17. **Reconciliation FAIL is honestly disclosed.** The 705-line report hashes exactly to `A847021888360DBC7470D5510A0A7B0FC6B3B1886F1327CA160D9B167E8C9CC1` and reports `FAIL gone=2`, one case per host. Its host counts, 29 quarantined cases per host, zero quarantined-but-run, zero duplicates and zero new nonpassed results match the handoff. Source history documents the change from DropsBuildTests to DropsBuildIntegrationTests to DropsNukeBuildIntegrationTests. This is a credible semantic-successor explanation, not a literal name-identity PASS. Preserve the FAIL report and add an explicit reviewed mapping rather than rewriting its result.

18. **Vacuous embedding, plugin tags and stale command examples.** All four integration EmbeddingServiceTests return on unavailable model before asserting. Their base constructs default EmbeddingOptions; EmbeddingService's constructor only tries loading existing model/vocab. The default model directory is absent at audit time. The historical Linux vacuous execution remains an inference because TRX does not record assertion execution. The nine plugin methods named in the handoff have PluginInt=Deterministic class tags and no Category=Integration tags. Both workspace instruction files still show the stale TodoServiceTests examples. These disclosed source defects are real.

19. **Uncited inventory and launcher limitation.** The named scratch inventory hashes to `B610556845E3093E3307ACFE671DDE18E2DFA8864906F4CCA67F0E70B9456169` and reports 1,915 uncited methods out of 5,408 and 68 methods in classification-only classes. Those are inventory-report populations, not fresh independently reproduced counts. The existing HV launcher checks uppercase brace placeholders and PENDING items but has no FILL-token refusal. It is explicitly Codex-specific and cannot be reused unchanged for the operator's newly selected Claude reviewer.

## Current-state drift

20. **Windows child ref has advanced.** Local `origin/fix/unit-suite-green-20261019-windows` and its OMARCHY worktree now point to `c8b231c`, not `03eea747`. This is a docs-only advancement and does not invalidate the retained code-head gate evidence. The handoff was written before its own commit and its snapshot needs a current-head annotation. No fresh claim is made here about either Windows checkout's current HEAD.

## Unverified claim groups

21. **Historical session and operator events.** The weekly-limit error, precise prior operator-answer times, all parent/child turn closures, and every procedural slip were not independently read back from their complete originating turns. Their presence in Claude's fact sheet is corroboration, not independent proof.

22. **Complete semantic parity and classification.** This audit did not inspect every moved test's assertions against its original or reclassify every dependency. Matching TRX names/counts and ownership declarations do not prove either property. The pre-HV holistic review and explicit renamed-case reconciliation remain necessary.

23. **Historical host state and timing causes.** No current SSH process/desktop query was made. Past host process counts, the 24-minute quiet spell, and a first-run/load explanation for the slowdown are not independently established. The handoff labels the slowdown cause as inference, appropriately.

24. **Every process-order and security-scan claim.** No comprehensive chronological validation of all slice mock-before-production steps, script dry runs, earlier scans or commit signatures was performed. This audit authenticates the final evidence set and selected source/history claims. It does not retrospectively certify the complete process.

## Receipts and assessment

- Mechanical output: `docs/receipts/unit-suite-green-20261019/2026-11-09-codex-handoff-audit.txt`, especially its VERIFICATION summary at line 38.
- Manifest and all original paths: `docs/receipts/hv/unitgreen-r15-trx-03eea747/manifest.json`.
- Live reads: Codex plugin `workflow.todo.get`, two requirement getters and `workflow.requirements.effective`; GitHub connector `get_pr_info`, PR87. These are also described in the audit QBrain turn. Temporary read copies are not treated as canonical storage.
- The read-only PowerShell audit script was parsed and passed Invoke-ScriptAnalyzer at Warning/Error with no diagnostics before execution. No build or test was started.
- Accuracy assessment: 98%, based on direct XML parsing, file hashes, source reads and live task/requirement/PR reads. This is an author assessment, not independent HV.
- Completeness assessment: 90% for the entire handoff, because the four groups above remain outside this worktree-focused verification. No acceptance claim follows from this score.
