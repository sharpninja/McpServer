# Hostile Validator Receipt

TimestampUtc: 2026-08-21T22:01:15Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation Phase B2 red-test gate PLAN-PLUGINHANDOFF-001). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.97.0 and .version 1.97.0; marker agent_plugins.Grok plugin_version is not the source of truth)
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 6 B2
TODO: PLAN-PLUGINHANDOFF-001 (Done=false). Child MCP-PLUGINCORE-004 Done=false.
PriorReceipt: docs/receipts/hostile-validator-20260821T213854Z.md (Phase A A6 AGREE)
Collector: docs/receipts/_hv-b2-20260821T215535Z/
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass re-ran health nonce, Test-MarkerSignature, git porcelain, live todo_get, GET /mcpserver/requirements/{fr|tr|test}/{id}, focused dotnet test, and focused Pester. Implementer chat was not trusted.

## Clock and live MCP

TimestampUtc is collector stamp 20260821T220115Z. Health GET /health?nonce=632c3fbec35648738c30b81ee6f7f630 returned HTTP 200, status Healthy, storage reachable, nonce echoed exactly (docs/receipts/_hv-b2-20260821T215535Z/health.txt). Test-MarkerSignature -MarkerFile AGENTS-README-FIRST.yaml returned True. Live todo_get PLAN-PLUGINHANDOFF-001 Id present Done=false DoneSummary=null. ImplementationTasks 1-8 (P0-A through A6) Done=true. Task 9 B1 Done=false. Task 10 B2 Done=false. Task 11 B3 Done=false.

## Session log proof

Persisted. SessionId GrokSubagentHostile-20260821T215535Z-b2-red. Turn requestId req-20260821T215535Z-001-phase-b2-red-test-gate. sessionlog_open created=true. sessionlog_begin_turn success turnId 42774. sessionlog_complete_turn status completed. sessionlog_query agent=GrokSubagentHostile todoId=PLAN-PLUGINHANDOFF-001 from=2026-08-21T21:50:00Z totalCount=1; this session turn status completed, 7 actions, 3 designDecisions, 3 processingDialog items.

## Mandatory surface that could not be evaluated

None. Focused filters were the B2 gate (red tests plus named reuse greens). Full `./build.ps1 Test` was not required to enter B3.

## Explicit FAIL list

None.

## Residual (not FAIL, not a reason to DISAGREE this B2 gate)

- Coordinator_AppendActions_PrimaryFailFailsafeOk_PluginSeesSuccess is currently green. ReplCommandDispatcher.TryDispatchStatelessLifecycleAsync returns Type=result for appendActions without calling passthrough or failsafe (src/McpServer.Repl.Core/ReplCommandDispatcher.cs around the AppendActionsMethod early return). The test only asserts Type=result, so it does not prove failsafe PersistAsync. FR-MCP-REPL-009 AC1 as a whole still has currently failing tests (Open, Begin, Update, AppendDialog). B3 must still wire appendActions through the coordinator even though this one method name is already green.
- Invoke-ReplFailsafeDrainOnFirstSuccess_WhileReplRawInFlight_DoesNotRun is currently green because plugins/core/lib-ps/repl-invoke.ps1 already sets ReplFailsafeDrainDeferred when ReplRawInFlight is true (TEST-MCP-195 AC7 already implemented). Existing TEST-MCP-195 getFr-before-30s example still Passed in this review.
- AC1 coordinator tests do not assert failsafe.PersistAsync was received. They will go green if dispatch returns result without writing failsafe. B3 must not treat Type=result as isolation.
- Get-McpFailsafeDir_MigratesLegacyPluginQueue_ChecksumMatchThenDeleteSource currently fails on missing destination file. It does not assert a checksum match. After B3, add or keep a checksum assertion; do not treat a silent copy as enough.
- PLAN task 8 A6 is Done=true after A6 AGREE. Do not mark task 10 B2 or MCP-PLUGINCORE-004 done:true until this receipt is cited. Do not start B3 until the parent records this OverallVerdict.

## Explicit PASS (machine evidence)

- All 14 plan B1 new-red names exist on disk (7 C# coordinator/checksum + 7 Pester, of which one Pester is nested-drain).
- Coordinator filter FullyQualifiedName~Coordinator_ : Failed 6 Passed 1 Skipped 0. Failed: Open, Begin, Update, AppendDialog, CompleteTurnAndFailTurn, PrimarySuccess. Passed: AppendActions (focused re-run Passed 1).
- Coordinator_Open failed Expected result Actual error at SessionLogPersistenceCoordinatorIsolationTests.cs:90.
- Build.Tests checksum: Failed 1 Passed 0. Drift on agent-runtime-header.ps1 and repl-invoke.ps1 for all five official plugins.
- Existing C# greens filter FilesystemPersistAsync_WritesReplayableV4ScopedEnvelope plus PersistAsync_PrimaryFails_ReturnsFailsafeResult, PersistAsync_CallerCancels_DoesNotInvokeFailsafe, PersistAsync_BothStrategiesFail_ThrowsPersistenceException: Failed 0 Passed 4 Skipped 0.
- Focused Pester 8 tests: Failed 5 Passed 3 Skipped 0. Passed: parses dictionary-backed dialogItems YAML and property-backed JSON; rejects appendDialog when no dialog items can be parsed; Invoke-ReplFailsafeDrainOnFirstSuccess_WhileReplRawInFlight_DoesNotRun. Failed: Get-ReplMethodTimeoutSeconds_WhileDrainingSubmitAsync_IsNotTwoSeconds (got 2); CompleteTurnBeginTurn drain-timeout (SubmitAsync got 2 not >=120); Invoke-ReplFailsafeDrain_SuccessfulSubmitWithinDrainTimeout_RemovesYaml (replayed 0 not 1); Get-McpFailsafeDir_MatchesV4FailsafeAgentWorkspacesPending (actual ...\.mcpServer\default\failsafe); Get-McpFailsafeDir_MigratesLegacyPluginQueue_ChecksumMatchThenDeleteSource (migrated path missing).
- Additional existing green: TEST-MCP-195 getFr returns before a 30s SubmitAsync drain timeout when queued session_submit 503s Passed.
- git status: M PluginPowerShellRuntime.Tests.ps1 (+129, additive at EOF); ?? SyncAgentPluginsChecksumTests.cs; ?? SessionLogPersistenceCoordinatorIsolationTests.cs. No src/ product diffs. Drain still `return 2` at plugins/core/lib-ps/repl-invoke.ps1:602. Get-McpFailsafeDir still Join-Path (Resolve-McpCacheDir) failsafe at resolve-cache-dir.ps1:145. REPL_FAILSAFE_DRAIN_TIMEOUT absent from product ps1/cs. TEST-MCP-REPL-041 GET 404.
- GET FR-MCP-REPL-009 AC count 4; FR-MCP-PLUGINCORE-004 AC count 3; TEST-MCP-195 AC count 7 including AC5 never 2 / AC6 yaml removed / AC7 nested drain; TR-MCP-REPL-012 AC3 drain SubmitAsync 120; TR-MCP-PERSIST-003 AC3 never hardcoded 2.

## Claims reviewed

### A Requested

#### A1. New tests exist with plan names

Verdict: PASS

Evidence: grep hits for every B1 name in tests/McpServer.Repl.Core.Tests/SessionLogPersistenceCoordinatorIsolationTests.cs, tests/Build.Tests/SyncAgentPluginsChecksumTests.cs, and plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1 (lines 4952-5079). Names match plan section 6, not the abbreviated parent list only.

#### A2. Those tests currently fail (red), or at least drain timeout, V4 path, and Coordinator_Open fail

Verdict: PASS

Evidence: parent claim was hedged. Coordinator_Open, drain timeout tests, and both V4 path tests failed in this review (outputs under docs/receipts/_hv-b2-20260821T215535Z/dotnet-coordinator.txt, pester-b1-new.json). Two listed new tests are green: Coordinator_AppendActions and nested-drain deferral. Those greens are residual, not a miss of the hedged claim.

#### A3. Existing greens were not relabeled

Verdict: PASS

Evidence: git diff PluginPowerShellRuntime.Tests.ps1 is +129 lines only, appended after the existing TEST-MCP-195 getFr example. It names `parses dictionary-backed dialogItems YAML and property-backed JSON` (line 2984) and `rejects appendDialog when no dialog items can be parsed` (line 3077) still present and Passed. FilesystemPersistAsync_WritesReplayableV4ScopedEnvelope still at SessionLogPersistenceStrategyTests.cs:128 and Passed with three sibling PersistAsync facts.

#### A4. Drain still return 2. Get-McpFailsafeDir still cache/failsafe. No B3 product implementation

Verdict: PASS

Evidence: Select-String repl-invoke.ps1:602 `return 2` inside ReplFailsafeDraining SubmitAsync. Get-McpFailsafeDir returns Join-Path (Resolve-McpCacheDir) 'failsafe', which resolved in the failing V4 test to `{temp}\.mcpServer\default\failsafe`. git diff --stat has no src/ or plugins/core/lib-ps product edits. Dispatcher uses _sessionLogPersistenceStrategy only in DispatchSessionLogPersistenceRequestAsync (PersistTurn), not Open/Begin/Update/AppendDialog/CompleteTurn.

#### A5. Phase A A6 AGREE already recorded

Verdict: PASS

Evidence: docs/receipts/hostile-validator-20260821T213854Z.md OverallVerdict AGREE, WorkClass 1 Phase A A6.

### B Workspace rules

#### B1. Byrd v4 at the Phase B2 red-test gate

Verdict: PASS

Evidence: This is the inter-phase gate after A6 AGREE. New tests exist for remaining unsatisfied Phase B ACs and were shown red except where the AC is already satisfied (AC7 nested drain) or one method of AC1 is a false-green (AppendActions residual). Not scored by FR createdAt vs file mtime. No B3 implementation in this review.

#### B2. Always bring the receipts

Verdict: PASS

Evidence: this review re-ran health nonce, marker signature, git, grep, todo_get, requirements GET, dotnet, and Pester. Collector docs/receipts/_hv-b2-20260821T215535Z/.

#### B3. MCP-only storage

Verdict: PASS

Evidence: todo_get MCP tool. Requirements get-by-id used authenticated REST because native MCP tools expose list/create/update, not get-by-id. No TODO.yaml or session-log file writes except this receipt.

#### B4. PowerShell only / no Python

Verdict: PASS

Evidence: pwsh.exe collectors and Pester. No python/python3/py invoked.

#### B5. Honesty

Verdict: PASS

Evidence: Claims match artifacts. Two listed new tests are green; that is stated, not buried. Drain still 2. PLAN Done=false. B2 task still false.

### C Requirements

#### C1. Phase B AC exist and named tests map

Verdict: PASS

Evidence: GET 200 FR-MCP-REPL-009 AC1-4, FR-MCP-PLUGINCORE-004 AC1-3, FR-MCP-172 AC1-4, TEST-MCP-195 AC1-7, TR-MCP-REPL-012 AC1-3, TR-MCP-PERSIST-003 AC1-3. Plan B1 names map onto those ACs. TEST-MCP-REPL-041 remains 404 (must not be invented).

#### C2. Remaining unsatisfied Phase B AC have currently failing new tests

Verdict: PASS

Evidence (unsatisfied -> failing test):
- FR-MCP-PLUGINCORE-004 AC3 checksum: SyncAgentPlugins_OfficialPluginLibChecksumsMatchCanonicalCore Failed.
- FR-MCP-REPL-009 AC1 isolation: Coordinator_Open/Begin/Update/AppendDialog Failed (AppendActions residual green).
- FR-MCP-REPL-009 AC3 terminal degraded report: Coordinator_CompleteTurnAndFailTurn Failed.
- FR-MCP-REPL-009 AC4 no pending on primary success: Coordinator_PrimarySuccess Failed.
- Path unification: both Get-McpFailsafeDir_* Failed. Actual path `.mcpServer\default\failsafe`.
- TEST-MCP-195 AC5 never 2 / TR-MCP-PERSIST-003 AC3 / TR-MCP-REPL-012 AC3 drain 120: Get-ReplMethodTimeoutSeconds_* Failed (got 2).
- TEST-MCP-195 AC6 yaml removed: Invoke-ReplFailsafeDrain_SuccessfulSubmitWithinDrainTimeout_RemovesYaml Failed (replayed 0).
Satisfied, not remaining: FR-MCP-REPL-009 AC2 (FilesystemPersistAsync Passed); PLUGINCORE AC1/AC2 (dictionary parse and empty-parse reject Passed); TEST-MCP-195 AC7 nested drain (product defers; new test Passed; getFr-before-30s Passed).

### D Current plan holistically

#### D1. Phase B2 DoD

Verdict: PASS

Evidence: Plan section 6 B2 requires AGREE that every remaining unsatisfied AC has a currently failing new test, and that existing greens were not relabeled. Both hold at AC level. This AGREE allows B3 to start. It does not complete Phase B, MCP-PLUGINCORE-004, or PLAN-PLUGINHANDOFF-001.

#### D2. No premature done or B3 mix-in

Verdict: PASS

Evidence: PLAN Done=false. Task 9 B1 and task 10 B2 still false. Task 11 B3 false. MCP-PLUGINCORE-004 Done=false and all eight child tasks false. git has test-only product-adjacent diffs (Pester + two new test files).

## Ratings

Accuracy: 94. Live test output, git porcelain, GET bodies, and drain/failsafe line numbers match the verdicts. Residual risk is the AppendActions false-green and coordinator tests that do not assert PersistAsync.

Completeness: 96. All five parent claims plus B/C/D and the B2 DoD were scored. Full unit suite was not re-run; B2 does not require it.

## What this AGREE does and does not allow

1. Phase B2 red-test gate is closed. Phase B3 implementation of parser, coordinator isolation, drain timeout, V4 path unification, and SyncAgentPlugins may start.
2. Do not flip PLAN-PLUGINHANDOFF-001, MCP-PLUGINCORE-004, or B2/B3/B4/B5 tasks to done:true on this receipt.
3. Do not treat Coordinator_AppendActions green as proof that appendActions isolation is done.
4. Drain `return 2` and plugin failsafe path `.mcpServer\{agent}\failsafe` remain in-scope for B3.
5. Existing reuse greens must stay green during B3. Relabeling them as new reds remains forbidden.
