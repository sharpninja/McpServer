# Hostile Validator Receipt

TimestampUtc: 2026-08-21T17:27:35Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN / P0-A completeness). Surface C applies to whether the plan captures FR/TR/TEST/AC. Surface D applies to this plan's own DoD.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (marker agent_plugins.Grok plugin_version 1.97.0)
Active plan: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a02413-e4ee-7513-bd22-9eddd82adb73\plan.md
Prior attack surface (not proof): docs/receipts/hostile-validator-20260821T161800Z.md; docs/receipts/hostile-validator-20260821T171357Z.md (DISAGREE, 7 FAIL)
OverallVerdict: DISAGREE

Default was FAIL or UNKNOWN until this pass re-read the current plan.md in full, BDPv4 / hostile-phase-gates, live todo_get of the eight child TODOs plus PLAN-PLUGINHANDOFF-001, on-disk Handoff tests, WorkspaceController create, drain timeout, and McpDbContext DbSets. Implementer chat was not trusted.

## Clock and live MCP

TimestampUtc is Get-Date -AsUTC 2026-08-21T17:27:35Z (receipt id 20260821T172735Z). Session open/begin used 20260821T172329Z. Live todo_get: PLAN-PLUGINHANDOFF-001 not found. Live todo_get succeeded for MCP-PLUGINCORE-004, MCP-PLUGININT-001, MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, MCP-HANDOFFREVIEW-001, MCP-HOSTILEREVIEW-001, MCP-WORKSPACEHYGIENE-002, MCP-WIKIEXPORT-001.

## Session log proof

Persisted. SessionId GrokSubagentHostile-20260821T172329Z-p0a-plan. Turn requestId req-20260821T172329Z-001-hostile-p0a-plan-completeness. sessionlog_open created=true. sessionlog_begin_turn success turnId 42678. sessionlog_dialog totalDialogItems 3. sessionlog_replace_section actions and designDecisions replaced=true. sessionlog_complete_turn status completed. sessionlog_query agent=GrokSubagentHostile from=2026-08-21T17:20:00Z totalCount=1, turn status completed, 5 actions, 4 designDecisions, 3 processingDialog items.

## Mandatory surface that could not be evaluated

None. Live eight-TODO bodies were retrieved. PLAN TODO absence was retrieved. Reviewer session-log turn is required and is persisted through MCP tools.

## Explicit FAIL list

- A3/C/D: TEST-HANDOFF-001..007 AC are not fully mapped to named methods. TEST-HANDOFF-002 AC2 (Markdown/text/JSON/YAML accepted) has on-disk `IngestAsync_SupportedFormats_AreAccepted` and zero plan hits. TEST-HANDOFF-005 AC2 `unless force=true` has on-disk `IngestAsync_ForceTrue_ExtractsAgain` and zero plan hits. Also unmapped on-disk reuse: `IngestAsync_TodoServiceFailure_RequiresReview`, `IngestAsync_AmbiguousHandoff_RequiresReview`. The four DraftOnly/LowConfidence/CreateWhenConfident/SameHash names are present as claimed.
- A9: The 171357Z FAIL list is only partially closed. Closed: mixed C/E/F/G hostiles, P19/P20 names, copy range 1-19, WorkspaceClient.CreateAsync dump binding, TR-MCP-REPL-012 exception text, HYGIENE-003/004 numbered AC, dual P0-A/P0-B. Open: TEST-HANDOFF AC map / D0 reuse inventory (same defect class as 171357Z DraftOnly omission).
- B1/D: BDPv4 playlist still incomplete. Phase B1 new-red list omits A5 named test `Get-ReplMethodTimeoutSeconds_CompleteTurnBeginTurn_RemainThirtySecondsWhileDrainSubmitUsesDrainTimeout`. D0 does not classify the on-disk SupportedFormats/ForceTrue/TodoServiceFailure/AmbiguousHandoff tests as reuse, so remaining-gap classification is not trustworthy.

## Explicit PASS (do not treat as AGREE)

- A1: Hostile-after-red-before-implement order is now written for C groups P1,P4,P5,P7,P11,P14,P15,P16,P19,P20 and for E/F/G all-tests-first. D1.5 remains. Mixed red+green hostiles from 171357Z are gone from section 7/9/10/11/19.
- A2: P19/P20 named tests exist in plan.md lines 392-401.
- A4: P0-B / section 14 copy range is sections 1 through 19; section 19 exists.
- A5: `--dump` binds to WorkspaceClient.CreateAsync / POST /mcpserver/workspace / WorkspaceController.CreateAsync; plan forbids FederationClient.RegisterWorkspaceAsync.
- A6: Drain exception for SubmitAsync 120s is written; TEST-MCP-REPL-027 stays 30s for completeTurn/beginTurn; TEST-MCP-195 AC5-7 remain; the 30s-while-drain named test exists in A5 (not in B1).
- A7: FR-MCP-HYGIENE-003 and FR-MCP-HYGIENE-004 have AC1-AC6.
- A8: Dual P0-A / create / P0-B remain. PLAN-PLUGINHANDOFF-001 not in store.
- A10: Listed reuse tests were not relabeled as new reds. No skip placeholders. done:true still requires hostile AGREE and Failed 0 Skipped 0.
- B2: Section 3 current-state matched this pass's live todo_get and greps.
- B3: Plan forbids YAML TODO create and direct TODO.yaml writes.
- B4: pwsh.exe -NoProfile -NonInteractive; no Python in this review.

## Claims reviewed

### A Requested

#### A1. Hostile-after-red-before-implement is now complete

Verdict: PASS

Evidence: plan.md 341-352 and 764 (C-red then C-green per group P1,P4,P5,P7,P11,P14,P15,P16,P19,P20). Phase E 541-541, F 576, G 612 write every named test first, hostile E/F/G-red, implement, E/F/G-green. D1.5 at 484-486. 171357Z mixed "Hostile after P1-P4 red/green" language is absent.

Residual (scored under B1/D, not this claim): B1 omits the A5 30s-while-drain named test.

#### A2. PLUGININT P19/P20 named tests

Verdict: PASS

Evidence: plan.md 390-401 names `PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero`, `PluginNativeSuite_AfterSyncAgentPlugins_FailedZeroSkippedZero`, `PluginInt_P19_RecordsBranchAndSha_NoUnrelatedCommit`, `PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero`, `PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag`. TEST-MCP-PLUGININT-001 AC5 native-suite half is no longer an evidence-folder-only gate.

#### A3. TEST-HANDOFF-001..007 AC mapped; D0 includes four mode/replay tests

Verdict: FAIL

Evidence that the four named D0 tests exist in the plan (440): `IngestAsync_DraftOnly_DoesNotCreateTodo`, `IngestAsync_LowConfidence_RequiresReview`, `IngestAsync_CreateWhenConfident_CreatesExactlyOneTodo`, `IngestAsync_SameHashAndPrompt_ReplaysExistingRun`. TEST-HANDOFF-001..007 method blocks exist at 476-482 and are not deferred with "map after D1".

FAIL: wiki github Testing-Requirements.md 55-82 AC bullets are not fully mapped.

- TEST-HANDOFF-002 AC2: "Markdown, text, JSON, and YAML inputs are accepted when contained and within 8 MiB." On disk: `HandoffIngestionServiceTests.IngestAsync_SupportedFormats_AreAccepted` (tests/McpServer.Support.Mcp.Tests/Services/HandoffIngestionServiceTests.cs 51-73, InlineData md/txt/json/yaml). Plan grep of those names: no hits.
- TEST-HANDOFF-005 AC2: replay "unless force=true". On disk: `IngestAsync_ForceTrue_ExtractsAgain` (same file 404-406). Plan grep: no hits.
- TEST-HANDOFF-004 cover line includes ambiguous handoffs. On disk: `IngestAsync_AmbiguousHandoff_RequiresReview` (362). Plan grep: no hits.
- TEST-HANDOFF-005 title includes TODO-service failure. On disk: `IngestAsync_TodoServiceFailure_RequiresReview` (384). Plan grep: no hits.

This is the same D0-omission class 171357Z already FAILed for DraftOnly/CreateWhenConfident.

#### A4. PLAN TODO copy range is sections 1 through 19

Verdict: PASS

Evidence: plan.md 82 "sections 1 through 19"; 647 "sections 1-19"; section 19 heading at 756. No leftover "sections 1-18" in the current plan.

#### A5. add-workspace `--dump` binds to WorkspaceClient.CreateAsync

Verdict: PASS

Evidence: plan.md 43 decision 20 and 614. Live code: WorkspaceController.cs 23 route `mcpserver/workspace`, 117-136 CreateAsync HttpPost. WorkspaceClient.cs 37 CreateAsync. FederationClient.RegisterWorkspaceAsync still posts `mcpserver/federation/proxies/{proxyId}/workspaces` (FederationClient.cs 68) and the plan names that as out of scope.

#### A6. Drain timeout amends TR-MCP-REPL-012 with explicit exception

Verdict: PASS

Evidence: plan.md 30 decision 7; 261-276 A5. TEST-MCP-REPL-027 remains 30s for completeTurn/beginTurn. Only `client.SessionLog.SubmitAsync` while ReplFailsafeDraining uses REPL_FAILSAFE_DRAIN_TIMEOUT 120. Named test `Get-ReplMethodTimeoutSeconds_CompleteTurnBeginTurn_RemainThirtySecondsWhileDrainSubmitUsesDrainTimeout` is in A5 line 276. TEST-MCP-195 AC5-7 remain. On-disk still `return 2` at plugins/core/lib-ps/repl-invoke.ps1 601-603 (expected until Phase B). Live TR-MCP-REPL-012 text in Technical-Requirements.md 2170 still has no exception (Phase A5 is the amend, not already applied).

#### A7. FR-MCP-HYGIENE-003 and FR-MCP-HYGIENE-004 numbered AC1-ACn

Verdict: PASS

Evidence: plan.md 189-209. HYGIENE-003 AC1-AC6. HYGIENE-004 AC1-AC6.

#### A8. Dual P0-A / create / P0-B remain; PLAN-PLUGINHANDOFF-001 not in store

Verdict: PASS

Evidence: plan.md 25-26, 63-82, 711-718. todo_get PLAN-PLUGINHANDOFF-001: TODO 'PLAN-PLUGINHANDOFF-001' not found.

#### A9. 171357Z FAIL list and AGREE-required items are closed

Verdict: FAIL

171357Z explicit FAIL vs this plan:

1. Mixed C/E/F/G hostiles: CLOSED (A1 PASS).
2. P19/P20 unnamed; TEST-HANDOFF map deferred to D1; D0 omitted DraftOnly: PARTIAL. P19/P20 named. DraftOnly quartet now in D0. TEST-HANDOFF map still misses SupportedFormats and ForceTrue (A3 FAIL).
3. Copy 1-18 vs 19; FederationClient dump bind; TR-MCP-REPL-012 unamended: CLOSED in plan text (A4, A5, A6).
4. 161800Z remainder: CLOSED except the TEST-HANDOFF/D0 inventory hole that 171357Z already named.

171357Z "what would be required for a later AGREE" item 3 is not satisfied.

#### A10. Existing green tests classified as reuse; no skip placeholders; done:true requires AGREE

Verdict: PASS

Evidence: B1 reuse vs new-red split; D0 reuse list; section 2 items 4 and 10; section 13/18 task 31. Parser Pester still at PluginPowerShellRuntime.Tests.ps1 2984. Listed Coordinator_* names are new, not relabeled greens. The SupportedFormats/ForceTrue tests were omitted from D0 rather than relabeled as new reds; omission is scored under A3/D, not as a relabel.

### B Workspace rules

#### B1. Byrd v4 for this class-1 plan-completeness gate

Verdict: FAIL

hostile-phase-gates.md and BDPv4 apply because this is P0-A. Mixed red+green hostiles are gone (A1). Remaining process holes: D0 inventory is not complete against on-disk Handoff tests that already cover remaining AC, so D1 "remaining gaps only" cannot be executed faithfully; B1 new-red list omits the A5 TR-MCP-REPL-012 named test that must be red before drain-timeout implementation.

#### B2. Receipts / honesty of plan current-state

Verdict: PASS

Section 3 vs live todo_get 2026-08-21T17:27Z: PLUGINCORE-004 open, tasks 1-8 Done=false, FR-MCP-PLUGINCORE-004 and FR-MCP-REPL-009 pending. PLUGININT P0 Done=true, P1-P20 Done=false; sln grep PluginIntegration: no matches. HANDOFF-001 tasks 1-10 Done=true, task 11 Done=false; FR-HANDOFF-001..007 linked. HANDOFFPLAN same code; Note still Codex NOT APPROVED. HOSTILEREVIEW FunctionalRequirements null. HYGIENE FunctionalRequirements null, keep open. WIKIEXPORT still FR-MCP-WIKIEXPORT-001/002. Drain hardcoded 2 still present.

#### B3. MCP-only storage instruction

Verdict: PASS

Plan forbids YAML TODO create and direct TODO.yaml writes. Requirements via MCP store. Markdown is projection.

#### B4. PowerShell / no Python

Verdict: PASS

Section 2 item 9. This review used pwsh.exe and MCP tools only. No python invocation.

### C Requirements (plan capture, not product-done)

Verdict: FAIL

Plan captures HOSTILEREVIEW 001-006 1:1, HYGIENE 001-005 1:1 with numbered AC including 003/004, WIKIEXPORT 003-005 not 001/002 as dump AC, and TR-MCP-REPL-012 exception language.

FAIL is TEST-HANDOFF AC coverage in the P0-A text: TEST-HANDOFF-002 AC2 and TEST-HANDOFF-005 force=true are structured AC with existing named tests that the plan neither maps nor lists as D0 reuse.

Wiki dump tables[] vs McpDbContext: DbSetCount 62, PlanSetCount 62, MissingFromPlan empty, ExtraInPlan empty (pwsh regex compare 2026-08-21T17:27:35Z).

### D Current plan holistically (this plan's DoD)

Verdict: FAIL

P0-A DoD in plan sections 2, 4, 14, 17, 19: every TODO has a phase (PASS), named tests for remaining AC (FAIL A3), BDPv4 hostile between phases (PASS A1, FAIL B1 playlist), decision-complete dump surface and copy range (PASS), no implementation before P0-B (PASS), wiki 003-005 (PASS), HANDOFFPLAN not a second product (PASS). A P0-A AGREE is not available.

Residual decision note (not a separate FAIL): WorkspaceCreateRequest still has no dump field (WorkspaceModels.cs 112+). Decision 20 plus G0 first-red-consumer is an allowed tests-first lock if G0 checkpoints the DTO name before G1.

## Accuracy and completeness

Accuracy: 92. Live todo_get, on-disk greps, DbSet 62/62 compare, WorkspaceController.CreateAsync, drain return 2, and HandoffIngestionServiceTests method names were re-run.
Completeness: 90. Did not execute product test suites (review-only). Did not dump the full requirements_list store (existing family capture used live TODO links plus docs/Project projections).

## What would be required for a later AGREE

- Put D0 reuse and TEST-HANDOFF method-map entries for `IngestAsync_SupportedFormats_AreAccepted`, `IngestAsync_ForceTrue_ExtractsAgain`, `IngestAsync_TodoServiceFailure_RequiresReview`, `IngestAsync_AmbiguousHandoff_RequiresReview`, and other on-disk tests that already cover remaining AC (including `HandoffControllerTests.GetAndApprove_DelegateToSharedService` for TEST-HANDOFF-006 get/approve).
- Put `Get-ReplMethodTimeoutSeconds_CompleteTurnBeginTurn_RemainThirtySecondsWhileDrainSubmitUsesDrainTimeout` on the Phase B1 new-red list, not only A5.
- Keep P0-A / P0-B. Do not create PLAN-PLUGINHANDOFF-001 until a later P0-A AGREE.

## Session identifiers

- SessionId: GrokSubagentHostile-20260821T172329Z-p0a-plan
- RequestId: req-20260821T172329Z-001-hostile-p0a-plan-completeness
- TurnId: 42678
- Receipt: docs/receipts/hostile-validator-20260821T172735Z.md
- Twin: docs/receipts/hostile-validator-20260821T172735Z.json
