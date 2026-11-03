# Round 9 projected problems, updated after the operator's rulings (ClaudeCode, 2026-11-02, written 2026-11-02T15:57:39)

Supersedes the 13:40 list (kept unchanged beside it). Last code head 878c366f (slice S); head under review 46270931 plus the prelaunch-records commit (docs only after 878c366f, D-A). Gate unitgreen-r9, reviewer Codex CLI gpt-6-astra max, merged OMARCHY worktree only. Each item keeps its 13:40 number; the operator's ruling or the outcome follows it.

## Resolved by the operator's rulings of 2026-11-02 (plan 27.21)

1. A stored time with no offset: ruled "UTC". FR-MCP-040 ac-1 gained the sentence; RequirementStoredTimestampContractTests gained the row and a wrong reader with a fixed -06:00 offset (slice S, 878c366f). Fixed.
7. SQL error 1223 on releasing the EF migration lock: out of scope, stated explicitly in the plan and the brief.
11. Invoke-CodeVerify reporting success without compiling a file outside a project: already triaged (f3f6a63e), out of scope.
12. The memory-bench stub cases rewriting docs/benchmarks/results: "Fix in scope". Slice S writes them to a checked temporary folder (FR-MCP-TESTRESOURCE-001, second criterion); after the full Linux lane at 878c366f git status listed no tracked change, so the two files stayed as committed (receipt ug-r9-final4-linux-lane-1.json, dirtyPaths). Fixed.
13. FR-MCP-USECASE-016/017 coming back: deleting again is not the remedy (soft delete; any create revives the row). BUG-TRIAGE-139's links repointed, retirement notices on TR-MCP-USECASE-018/019, Workspace memory MEMORY-PROCESS-014. That the link prompted the restorations is an inference.
15. Hook session submits landing in failsafe: already triaged (0d5b6229), out of scope. Each request's turn is opened through the plugin when the hook's submit fails.
16. The requirements wiki export's Azure-path rejection and the stale docs/Project pages: out of scope (MEMORY-PROCESS-009).
17. The running server reports read-time createdAt: "Fix in scope" by a post-merge step ("Deploy after merge, verify live (Recommended)"): after the merge, deploy the server and the REPL tool through the Nuke deployment target and show a live getFr or getTr returning the stored createdAt. FR-MCP-040 ac-1 is set satisfied only after that read.
21 (part). The held-out integration projects and cases (QBrainAi.Support.Mcp.IntegrationTests outside the two lane classes, QBrainAi.Acid.IntegrationTests, QBrainAi.TransactionSecurity.IntegrationTests; IMPL-TXNINTEGRATION-001): out of scope, stated explicitly.

## Still declared, unchanged

2. The REPL interfaces of the packaged QBrainAI.Repl.Core (IFrItem, ITrItem, ITestItem, IMappingItem) change CreatedAt/UpdatedAt from string to string? (binary compatible); callers get null where nothing is stored instead of the read time. A behavior change, not breaking.
3. Build.Tests run inside Nuke targets (the lane since round 1, TestGroups now) although the operator ruled on 2026-10-11 that Nuke does not run its own tests; both TRs are operator approved.
4. TestGroups ran live only on Linux; on Windows its composition and checks are proven by the Build.Tests rows (427 cases in ug-r9-final4-win-buildtests-1).
5. MEMORY-FACT-008's dated change logs cover the code of slices Q, R, P and S; earlier PR 87 code has none, per the memory's "not ... an unrelated retrospective sweep".
8. The incomplete lane ug-r9-final-linux-lane-1 is history, not evidence.
9. The 417 heuristic physical-I/O candidates stay out of scope.
10. The four identifier-scrub assertions stay as flagged lab-bound exceptions.
14. FR-MCP-SERVICEUPDATE-001's empty criterion ids (triage d88dc14a).
18. TR-MCP-FED-PATH-001 has no criterion for the no-direct-GetFullPath rule.
19. The failed binding ug-r9-binding-r-1 (my class selection; not evidence).
20. Reviewer independence: the requester is Claude and the reviewer Codex; Codex authored slices I, J, M, N and O earlier in this PR (the operator's choice).
21 (rest). The other items of plan 25.5 that still stand (scanner override, race inference, TR-MCP-SESSIONLOG-006 drift, the 98.19 MB round 5 file in history, writer list not discovered, TEST-MCP-REPL-007-1 text, ReadVariable default, criterion-less fixes, 130 environment-worded criteria outside the PR).

## New since 13:40 (item 29 was decided by the operator; the rest are declared)

Operator confirmation of this list: "Confirmed, launch (Recommended)" (2026-11-02, answered with "Strengthen ac-6 (Recommended)" for item 29).

22. Slice S made 878c366f the last code head, so the gates at 62c5882d are history: the whole gate set ran again at 878c366f on both hosts (Test 4704/0/0 on each, Linux lane 738/0/0, Windows lane 715/0/0, Build.Tests 522/0/0 and 427/0/0, ValidateTraceability passed on both, 0 NU190x; each lane equal by name to final3), and the HV evidence set is recopied to docs/receipts/hv/unitgreen-r9-trx-878c366f (the 62c5882d set is kept as history).
23. D-B (plan 27.21) does not set FR-MCP-QBOPENAI-001 ac-5 and ac-6, TEST-MCP-QBOPENAI-001 AC-001 or TEST-MCP-QBOLLAMA-001 AC-001 to AC-004: their B13 clauses belong to the paused PLAN-SESSIONTRIAGEFIX-001, whose port IMPL-TXNINTEGRATION-001 records (27.9 PLAN-3). FR-MCP-QBOPENAI-001 ac-7 is set: the slice K unit bindings prove all of it.
24. D-B corrections found by reading every listed criterion back (15:16:40): AC-TR-MCP-SESSIONLOG-006-009 no longer exists (27.1 split it), TR-MCP-REPL-008 holds no criterion and an empty title (store state after the 27.1 move), and the brief's requirement list named three pre-move ids; the plan and the brief now state the current ids.
25. The Windows TRX copied from the read-only mount carry file mode 100755 (a copy artifact; content and hashes equal to the originals).
26. Missing receipts, found by checking every run id the plan cites against docs/receipts: 21 runs (section 26 on 2026-10-29, slices H, K and L and Codex's I and J runs on 2026-10-30, and three runs of 2026-11-02) and the sibling sweep had no durable receipt. They are now written retrospectively from each run's own meta, exit and TRX files, labeled as such, with every TRX inside its run's window (plan 27.21). Under "No recipt is a fail." the reviewer may still judge a retrospective receipt weaker than one written at the time.
27. Slice H was never recorded in the plan: on 2026-10-29, after the pre-commit lane ug-r9-precommit-lane-1 failed two live-model QuadBrain rows, the FR-MCP-QBOPENAI-001 ac-5/ac-6 port ("Port ac-5/ac-6 into PR 87") ran mock gate first (ug-r9-mockgate-h-1), then the production change (ug-r9-binding-h-1). Now recorded in 27.21. The second pre-commit lane failed on SQL error 1223 (slice O).
28. D-B corrected for the QuadBrain records (27.21): my earlier draft excluded ac-5 and ac-6 as "IMPL-TXNINTEGRATION-001's port", but PR 87 carries that port. Set after AGREE: ac-5, ac-7, TEST-MCP-QBOPENAI-001 AC-001, TEST-MCP-QBOLLAMA-001 AC-001 to AC-003. Not set: TEST-MCP-QBOLLAMA-001 AC-004 (names a lab host). ac-6: item 29.
29. A criterion question for you: FR-MCP-QBOPENAI-001 ac-6 says that when the budget ends, "the raw output when formatting leaves nothing and that output is not an Arbiter envelope" is the content. Production (QuadBrainOpenAiChatService.ResolveDisplayContent, lines 313 to 324) returns the plain budget message there, which is what ac-7 says, and no contract row covers the case. Recommended: strengthen ac-6 so that case defers to ac-7 (the AC changes; the code and tests stay). Alternative: keep ac-6 and add a row plus a production change, which would be a new code head and a full gate rerun. Either way ac-6 stays unset until it is proven. Operator: "Strengthen ac-6 (Recommended)". ac-6 was replaced through the plugin at 19:02:43 (dry run first, read back, only ac-6 changed); with the case deferred to ac-7, the budget-end binding and the display-content rows bound to the production resolver prove all of it, so D-B sets it after AGREE.
31. Process slips since 13:40, disclosed: I completed TODO turn req-20261102T202901Z-prompt-73d0 without its decision and logged that decision on turn req-20261102T201626Z-prompt-fe16; I called the read-only Get-Todo.ps1 once with a parameter it does not have (it failed before running; rerun plain); the Windows Test run at 878c366f had no dry run of its own; my first wait on the Linux lane watched a relative folder that did not resolve and never saw the exit file (stopped, re-read with absolute paths); several of my chat status lines carried estimated, not measured, timestamps (corrected at 15:20:39).
