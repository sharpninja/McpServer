# Hostile Validator Receipt

TimestampUtc: 2026-08-21T23:04:57Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase B5 green gate). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin. Live workflow/client calls used this plugin Invoke-McpPlugin.ps1.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 6 Phase B (B3/B4/B5). Goal plan checklist item 1 still open.
TODO: PLAN-PLUGINHANDOFF-001 Done=false. MCP-PLUGINCORE-004 Done=false. MCP-WORKSPACEHYGIENE-002 Done=false.
PriorReceipt: docs/receipts/hostile-validator-20260821T220115Z.md (Phase B2 red AGREE); docs/receipts/hostile-validator-20260821T213854Z.md (Phase A A6 AGREE)
Collector: docs/receipts/_hv-b5-20260821T224147Z/
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass re-ran health nonce, Test-MarkerSignature, greps, independent hashes, focused and full named tests, live workflow.todo.get / client.Requirements.Get*Async, and client.SessionLog.QueryAsync. Implementer chat was not trusted.

## Clock and live MCP

Health GET /health?nonce=48fa278e0f764268842d4203dcef46ff returned HTTP 200, status Healthy, storage reachable, nonce echoed exactly (docs/receipts/_hv-b5-20260821T224147Z/health.json). Test-MarkerSignature returned True (docs/receipts/_hv-b5-20260821T224147Z/marker-signature.txt). Local timezone id Central Standard Time (CDT UTC-5).

## Session log proof

Persisted. SessionId GrokSubagentHostile-20260821T224147Z-b5-green. Turn requestId req-20260821T224147Z-001-phase-b5-green-gate. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42791. CompleteTurnAsync same turnId. client.SessionLog.QueryAsync agent=GrokSubagentHostile totalCount=18; this session is first item; turn status completed; 6 actions with unquoted integer order; 3 processingDialog items; 2 designDecisions. Proof: docs/receipts/_hv-b5-20260821T224147Z/sl-open.txt, sl-begin.txt, sl-complete.txt, sl-query-agent.txt.

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

None.

## Residual (not FAIL, not a reason to DISAGREE this B5 gate)

- Cited build.ps1 Test at 5:36:13 PM recorded Support.Mcp.Tests Passed 2071. A later in-flight Test that finished 17:54:25 CDT recorded Support.Mcp.Tests Passed 2070 Failed 0 Skipped 0. Both runs Failed 0 Skipped 0. This review treats the 2071 figure as true of the cited 165.log, not as the later tree count.
- Isolation tests inject a throwing passthrough for every method, including Coordinator_PrimarySuccess_NoPendingFailsafeArtifact. AC4 is still proven by Failsafe.DidNotReceive PersistAsync when coordinator primary PersistAsync succeeds, and by the production path that returns Type=result without PersistAsync when the Session Log client succeeds.
- Markdown projection docs/Project/Testing-Requirements.md still describes TEST-MCP-195 as AC1-4 only. Live store has AC5-7. Store is source of truth.
- First client.SessionLog.QueryAsync with agent+todoId+text returned totalCount 0. Agent-only query proved the session. Do not treat a tight filter miss as missing persistence.

## Claims reviewed

### A Requested

#### A1. Phase B2 red-test gate already has OverallVerdict AGREE; B3 started only after that AGREE

Verdict: PASS

Evidence: docs/receipts/hostile-validator-20260821T220115Z.md line 13 OverallVerdict AGREE, TimestampUtc 2026-08-21T22:01:15Z. B3 dispatch receipt docs/receipts/plan-pluginhandoff-b3-dispatch-20260821T222058Z.md TimestampUtc 2026-08-21T22:20:58Z cites that B2 AGREE. Product last-write after B2: repl-invoke.ps1 and ReplCommandDispatcher.cs 2026-08-21T22:31:34Z; sync-plugin-core.ps1 2026-08-21T22:16:00Z. B2 receipt recorded drain still `return 2` at that moment; this review's grep of repl-invoke.ps1 is NO_MATCH.

#### A2. Drain SubmitAsync is no longer hardcoded return 2; Get-ReplMethodTimeoutSeconds uses REPL_FAILSAFE_DRAIN_TIMEOUT default 120, or REPL_TIMEOUT when greater, only while ReplFailsafeDraining and method is client.SessionLog.SubmitAsync

Verdict: PASS

Evidence: plugins/core/lib-ps/repl-invoke.ps1 Get-ReplMethodTimeoutSeconds lines 601-608. Select-String `return 2` on that file: NO_MATCH (docs/receipts/_hv-b5-20260821T224147Z/grep-return2.txt). Focused Pester Get-ReplMethodTimeoutSeconds_WhileDrainingSubmitAsync_IsNotTwoSeconds Passed.

#### A3. workflow.sessionlog.completeTurn and beginTurn remain 30s while drain Submit uses drain timeout

Verdict: PASS

Evidence: same function keeps sessionlog methods on $default (30 unless REPL_TIMEOUT). Focused Pester Get-ReplMethodTimeoutSeconds_CompleteTurnBeginTurn_RemainThirtySecondsWhileDrainSubmitUsesDrainTimeout Passed.

#### A4. Get-McpFailsafeDir resolves V4 pending path, migrates legacy YAML by SHA-256 match then source delete, env overrides still win

Verdict: PASS

Evidence: plugins/core/lib-ps/resolve-cache-dir.ps1 Get-McpFailsafeDir lines 304-313; Invoke-McpFailsafeLegacyQueueMigration lines 238-285. Env MCPSERVER_FAILSAFE_DIR then MCP_FAILSAFE_DIR return first. Pester Get-McpFailsafeDir_MatchesV4FailsafeAgentWorkspacesPending and Get-McpFailsafeDir_MigratesLegacyPluginQueue_ChecksumMatchThenDeleteSource Passed. Get-ReplFailsafeDir delegates to Get-McpFailsafeDir (repl-invoke.ps1:822-826).

#### A5. ReplCommandDispatcher Open/Begin/Update/AppendDialog/AppendActions return type=result when primary fails and failsafe succeeds; CompleteTurn/FailTurn include degraded=true, persistenceStrategy=filesystem-failsafe, fully-qualified failsafePath; primary success does not write pending failsafe

Verdict: PASS

Evidence: src/McpServer.Repl.Core/ReplCommandDispatcher.cs TryDispatchStatelessLifecycleAsync AppendActions wired through passthrough then TryPersistSessionLogWhenPrimaryFailsAsync (lines 460-523). Host DI registers FailoverSessionLogPersistenceStrategy into ReplCommandDispatcher (ServiceCollectionExtensions.cs:113-151). SessionLogPersistenceCoordinatorIsolationTests Passed 7 Failed 0 Skipped 0 (docs/receipts/_hv-b5-20260821T224147Z/dotnet-coordinator.txt).

#### A6. SyncAgentPlugins Copy-CoreFile copies exact bytes; official plugin lib SHA256 matches canonical plugins/core/lib-ps

Verdict: PASS

Evidence: plugins/core/sync/sync-plugin-core.ps1 Copy-CoreFile is Copy-Item -LiteralPath -Force (lines 103-113), no LF rewrite. SyncAgentPlugins_OfficialPluginLibChecksumsMatchCanonicalCore Passed 1 Failed 0 Skipped 0. Independent Get-FileHash of every canonical lib-ps file except GAPS.md against five official plugin lib copies: CHECKSUM_MISMATCHES=0 (docs/receipts/_hv-b5-20260821T224147Z/plugin-checksums.json).

#### A7. Named B1 tests green: coordinator 7/0/0; focused Pester drain/V4 6/0/0 then full PluginPowerShellRuntime 124/0/0; SessionLogPersistence* 13/0/0

Verdict: PASS

Evidence: this review, same collector: coordinator 7/0/0; persist filter 13/0/0; focused Pester 6/0/0; full PluginPowerShellRuntime.Tests.ps1 Tests Passed: 124, Failed: 0, Skipped: 0, PESTER_FULL_EXIT=0.

#### A8. B4 gates: Compile, Pester 124/0/0, Repl.Core 847/0/0, build.ps1 Test Failed 0 Skipped 0 on cited projects

Verdict: PASS

Evidence: cited Compile log call-6eec1795-465f-4937-ae46-861bb5afcf9b-133.log ends `Build succeeded on 8/21/2026 5:21:24 PM` with Compile Succeeded. Cited Pester call-b2e1be93-636d-47d2-bf24-b9ad64223231-164.log Passed 124 Failed 0 Skipped 0. Cited Test call-b2e1be93-636d-47d2-bf24-b9ad64223231-165.log Build succeeded on 8/21/2026 5:36:13 PM with Support.Mcp 2071, Client 284, Cqrs 33, Launcher 20, McpAgent 63, Repl.Core 847, QBAgent 50, each Failed 0 Skipped 0. This review independently re-ran Pester 124/0/0 and Repl.Core 847/0/0. A later Test that this review watched finish at 17:54:25 CDT: 2070/284/33/20/63/847/50 all Failed 0 Skipped 0 (F:\GitHub\McpServer\.nuke\temp\build.log). Independent Compile later OnTargetSucceeded at 18:02:03 CDT (same nuke log target_name Compile).

#### A9. AppendActions is wired through coordinator when sessionLogPersistenceStrategy is registered; B2 residual addressed

Verdict: PASS

Evidence: dispatcher case AppendActionsMethod comments FR-MCP-REPL-009 AC1 and calls TryMirrorSessionLogWorkflowAsync then passthrough then TryPersistSessionLogWhenPrimaryFailsAsync. Coordinator_AppendActions_PrimaryFailFailsafeOk_PluginSeesSuccess now asserts Failsafe.Received(1).PersistAsync and Passed in the 7/0/0 class run.

#### A10. ReplRawInFlight deferral still present; drain success removes yaml when timeout > 2

Verdict: PASS

Evidence: repl-invoke.ps1 lines 1126-1130 set ReplFailsafeDrainDeferred when ReplRawInFlight. Pester Invoke-ReplFailsafeDrainOnFirstSuccess_WhileReplRawInFlight_DoesNotRun and Invoke-ReplFailsafeDrain_SuccessfulSubmitWithinDrainTimeout_RemovesYaml Passed.

#### A11. PLAN-PLUGINHANDOFF-001 not done; MCP-WORKSPACEHYGIENE-002 not done; MCP-PLUGINCORE-004 still Done=false

Verdict: PASS

Evidence: live workflow.todo.get PLAN-PLUGINHANDOFF-001 done: false. MCP-PLUGINCORE-004 done: false. MCP-WORKSPACEHYGIENE-002 done: false. PLAN implementationTasks B3/B4/B5 still done: false (docs/receipts/_hv-b5-20260821T224147Z/todo-plan.txt). No B5 receipt existed before this file.

#### A12. Attack: hardcoded return 2 still present; Get-McpFailsafeDir still cache/failsafe; checksum still drifting; coordinator still error on Open; Pester/Test skipped or failed; B3 started before B2 AGREE

Verdict: PASS (attack did not land)

Evidence: A2-A8 and A1. Coordinator_Open is among the 7 passing facts. No Pester or named-gate skips in this review.

### B Workspace rules

#### B1. Honesty / receipts

Verdict: PASS

Evidence: cited terminal logs exist and contain the claimed summaries. This review re-ran the named gates instead of accepting chat. The later 2070 vs cited 2071 is disclosed as residual, not hidden.

#### B2. Byrd v4 inter-phase order (not post-hoc timestamps)

Verdict: PASS

Evidence: B2 hostile AGREE at 22:01:15Z exists. B3 dispatch at 22:20:58Z cites it. Product drain/V4/dispatcher writes are after that AGREE. This is the B5 green gate after those reds.

#### B3. MCP-only TODO/session/requirements storage

Verdict: PASS

Evidence: live todo_get and requirements Get*Async via plugin. git porcelain has no todo.yaml or session-log storage edits by this review or as the B3 product set. Requirements were not hand-edited in this review.

#### B4. PowerShell-only / no Python

Verdict: PASS

Evidence: this review used pwsh.exe -NoProfile only. No python process was started. Implementer logs are Nuke/dotnet/Pester.

#### B5. Look-before-delete on failsafe migration

Verdict: PASS

Evidence: legacy YAML is copied, SHA-256 compared, source deleted only on match (resolve-cache-dir.ps1:269-282). Pester asserts source gone and hashes equal.

### C Requirements

#### C1. FR-MCP-PLUGINCORE-004 AC1-3 exist and have tests

Verdict: PASS

Evidence: live GetFrAsync FR-MCP-PLUGINCORE-004 AC001-AC003. Parser reuse tests still in PluginPowerShellRuntime.Tests.ps1 and Passed inside 124/0/0. Audit increment is after server success (repl-invoke.ps1:1883-1886). AC3 covered by SyncAgentPlugins_OfficialPluginLibChecksumsMatchCanonicalCore 1/0/0 plus independent hashes.

#### C2. FR-MCP-REPL-009 AC1-4 exist and have tests

Verdict: PASS

Evidence: live GetFrAsync FR-MCP-REPL-009 AC001-AC004. Coordinator tests cover AC1 (five non-terminal methods), AC3 (CompleteTurn/FailTurn degraded fields), AC4 (no failsafe PersistAsync on primary success). AC2 envelope covered by SessionLogPersistenceStrategyTests.FilesystemPersistAsync_WritesReplayableV4ScopedEnvelope inside SessionLogPersistence* 13/0/0.

#### C3. TEST-MCP-195 AC5-7, TR-MCP-REPL-010, TR-MCP-REPL-012 AC3, mappings

Verdict: PASS

Evidence: live GetTestAsync TEST-MCP-195 AC1-7 including AC5 never 2, AC6 yaml removed, AC7 nested drain. Live TR-MCP-REPL-012 AC3 drain Submit timeout. Live TR-MCP-REPL-010 AC001-AC004. Focused Pester and coordinator tests map onto those ACs. docs/Project/TR-per-FR-Mapping.md still maps FR-MCP-REPL-009 to TEST-MCP-REPL-025..028 and FR-MCP-PLUGINCORE-004 to TEST-MCP-PLUGINCORE-004/005. TEST-MCP-REPL-041 remains the thing not to invent; it was 404 at B2 and was not created as a product TEST.

#### C4. Suite green is not treated as AC coverage

Verdict: PASS

Evidence: this review required the named B1 tests (coordinator class, checksum fact, drain/V4 Its) in addition to full Pester/Repl.Core/Test. Those named tests executed and passed.

### D Plan holistically

#### D1. Implementer claims Phase B (B3+B4) is green and ready for B5 AGREE so Phase C may start; does not claim PLAN or hygiene or PLUGINCORE done

Verdict: PASS

Evidence: live TODOs Done=false for PLAN-PLUGINHANDOFF-001, MCP-PLUGINCORE-004, MCP-WORKSPACEHYGIENE-002. PLAN tasks B3/B4/B5 still false. Goal plan.md checklist item 1 still `- [ ] Finish Phase B`. Plan section 6 B5: AGREE on parser, coordinator (all five non-terminal methods), drain timeout, checksum sync, failsafe path unification, then may start Phase C. Those items are the A/C tests that passed in this review. MCP-PLUGINCORE-004 may be marked done only after this AGREE plus the B4 gate, with receipt path in doneSummary. This validator did not flip any TODO.

## Ratings

Accuracy: 93. Cited logs, live store, and independent re-runs matched the B3/B4 green claim. The 2071 vs later 2070 Support.Mcp count is the only numeric drift, disclosed.

Completeness: 95. All locked surfaces A+B+C+D scored. Named re-run list completed. Full Test was verified from the cited 165.log plus a later 0-fail 0-skip run this review watched finish. Compile verified from cited 133.log plus this review's Compile OnTargetSucceeded.

## Exit

OverallVerdict AGREE. Parent may start Phase C. Parent may mark MCP-PLUGINCORE-004 done only with this receipt path in doneSummary and the Failed 0 Skipped 0 gates cited. Do not mark PLAN-PLUGINHANDOFF-001 or MCP-WORKSPACEHYGIENE-002 done.
