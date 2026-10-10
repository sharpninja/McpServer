# Hostile Validator Receipt

TimestampUtc: 2026-08-21T20:06:36Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN / P0-A completeness). Surfaces A+B+C+D all apply. C is plan capture of FR/TR/TEST/AC. D is this plan's DoD.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (marker agent_plugins.Grok plugin_version 1.97.0)
Active plan: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a02413-e4ee-7513-bd22-9eddd82adb73\plan.md
Prior attack surface (not proof): docs/receipts/hostile-validator-20260821T161800Z.md; docs/receipts/hostile-validator-20260821T171357Z.md; docs/receipts/hostile-validator-20260821T172735Z.md; docs/receipts/hostile-validator-20260821T173914Z.md (DISAGREE). Implementer claims those FAILs are closed by rewriting D0 as the complete Handoff* test inventory and mapping TEST-HANDOFF AC to covering methods.
OverallVerdict: DISAGREE

Default was FAIL or UNKNOWN until this pass re-read the current plan.md in full, Testing-Requirements.md TEST-HANDOFF-001..007 AC bullets, live todo_get of eight children plus PLAN-PLUGINHANDOFF-001, on-disk Handoff-related Fact/Theory names, D0 vs D1 substrings, and McpDbContext DbSets. Implementer chat was not trusted.

## Clock and live MCP

TimestampUtc is Get-Date -AsUTC 2026-08-21T20:06:36Z (receipt id 20260821T200636Z). Session open/begin used 20260821T195919Z. Health GET /health?nonce=68fc152acf124eb999750c0394c30945 returned 200 Healthy and echoed that nonce. Live todo_get: PLAN-PLUGINHANDOFF-001 not found. Live todo_get succeeded for MCP-PLUGINCORE-004, MCP-PLUGININT-001, MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, MCP-HANDOFFREVIEW-001, MCP-HOSTILEREVIEW-001, MCP-WORKSPACEHYGIENE-002, MCP-WIKIEXPORT-001.

## Session log proof

Persisted. SessionId GrokSubagentHostile-20260821T195919Z-p0a-plan. Turn requestId req-20260821T195919Z-001-hostile-p0a-d0-d1-claims. sessionlog_open created=true. sessionlog_begin_turn success turnId 42719. sessionlog_dialog totalDialogItems 3. sessionlog_replace_section actions and designDecisions replaced=true. sessionlog_complete_turn status completed. sessionlog_query agent=GrokSubagentHostile from=2026-08-21T19:50:00Z totalCount=1, turn status completed, 6 actions, 4 designDecisions, 3 processingDialog items.

## Mandatory surface that could not be evaluated

None. Live eight-TODO bodies were retrieved. PLAN TODO absence was retrieved. Reviewer session-log turn is required and is persisted through MCP tools.

## Explicit FAIL list

- A6: 173914Z FAIL list is not closed. The four named covering tests from 173914Z are now in D0 and the TEST-HANDOFF map (GetAndApprove_DelegateToSharedService, Validate_InvalidFields_ProduceFieldDiagnostics, PublicSurfaces_ExposeIngestGetAndApprove, Dispatcher_RoutesHandoffWorkflowMethods). The FAIL class remains: D0 is still not a complete Handoff* inventory against on-disk covering tests. D0 line 437 says "the 21 facts listed previously including" five names; "listed previously" is a dangling pointer with no 21-name list in this plan file. Three HandoffIngestionServiceTests facts have zero hits in the entire plan: IngestAsync_UnknownSourceNotes_AreNotDiscarded, IngestAsync_ExtractorCancelled_DoesNotCreateTodo, IngestAsync_ArtifactSource_UsesDocumentChunks. Additional Handoff-named covering tests also have zero plan hits: EnqueueOneShotAsync_HandoffContext_ExecutorGetsRawPromptAndSurfacesStayRedacted, Publish_HandoffContext_RedactsRawSource, PostgreSql_CleanDatabase_AppliesHandoffMigrationAndPersistsEntity. Section 18 task 21 still says "D1 remaining reds including FR-HANDOFF-003 field diagnostics", which contradicts D1's rewrite that TEST-HANDOFF-004 AC1 is already covered by Validate_InvalidFields.
- A-expand / C: TEST-HANDOFF-004 AC1 and TEST-HANDOFF-006 AC1 are now mapped to the 173914Z covering methods (PASS for those two AC rows). Remaining C hole is incomplete reuse classification for on-disk covering tests that still affect remaining-gap trust, including FR-HANDOFF-002 unknown-source-notes (IngestAsync_UnknownSourceNotes_AreNotDiscarded, zero plan hits) and P1-5 AgentPool raw-source tests that D1 does not classify as reuse.
- B1: BDPv4 remaining-gap classification is still not trustworthy. D0's own contract (plan.md 427) is "the complete Handoff* test method inventory under tests/ (excluding Dispose and nested helper members)". D0 does not list the 21 ingestion facts by name. D1 names AgentPool_DoesNotRetainRawHandoffSourceInPromptState as a new red with zero on-disk method-name matches, while on-disk greens EnqueueOneShotAsync_HandoffContext_ExecutorGetsRawPromptAndSurfacesStayRedacted (AgentPoolServiceTests.cs:222) and Publish_HandoffContext_RedactsRawSource (OneShotSensitivePromptPolicyTests.cs:12) already assert rendered-prompt / published-surface redaction and are invisible to D0/D1. The existing green even locks executor raw-prompt delivery (Assert.Equal(raw, captured.UserTranscriptText)), so the D1 name can fight a classified-green behavior.
- D: P0-A DoD requires named tests for remaining AC and that existing green Handoff tests are classified reuse vs remaining gap before new tests are written. That DoD is not met while D0 omits on-disk Handoff covering tests and while task 21 still treats FR-HANDOFF-003 field diagnostics as a D1 remaining red.

## Explicit PASS (do not treat as AGREE)

- A1: D0 now names GetAndApprove_DelegateToSharedService, Validate_InvalidFields_ProduceFieldDiagnostics, PublicSurfaces_ExposeIngestGetAndApprove, and Dispatcher_RoutesHandoffWorkflowMethods (plan.md 432-436). Those four strings have non-zero plan hits (3/3/3/3).
- A2: TEST-HANDOFF-004 AC1 maps to Validate_InvalidFields_ProduceFieldDiagnostics plus Validate_BlankDescriptionAndTechnicalDetails_ProduceFieldDiagnostics plus IngestAsync_BlankDescriptionAndTechnicalDetails_ReturnFieldDiagnosticsAndDoNotCreateTodo (plan.md 451, 474). HandoffDraft_Invalid* is not a D1 new-red list. The single plan hit is the prohibition "Do not add HandoffDraft_Invalid* new reds." On-disk HandoffDraft_InvalidId_FieldDiagnostic is ZERO.
- A3: TEST-HANDOFF-006 AC1 maps IngestAsync_DelegatesToSharedService, GetAndApprove_DelegateToSharedService, PublicSurfaces_ExposeIngestGetAndApprove, Dispatcher_RoutesHandoffWorkflowMethods, HandoffIngestDirectorCommand_PrimaryCommand_DelegatesToExecutor, PluginSkillWorkflow_InvokesTypedClientHandoffEndpoints, remaining PluginHandoffSkill_InvokeIngestGetApprove_UsesDocumentedWorkflowMethods (plan.md 453, 479). Client ingest/get/approve names remain in D0 Client (plan.md 431).
- A4: D1 "New reds with zero on-disk method-name matches" are PluginHandoffSkill_InvokeIngestGetApprove_UsesDocumentedWorkflowMethods, ProcessingLease_RenewsAndFencesTerminalUpdates, AgentPool_DoesNotRetainRawHandoffSourceInPromptState, Provenance_IncludesEffectiveCustomPromptIdentityAndVersion. Exact-name grep under tests/ is ZERO for all four. CancellationToken.None in main src Handoff*.cs is ZERO, so the conditional CancellationRecovery red is not added.
- A5: Hostile-after-red-before-implement remains for C groups P1,P4,P5,P7,P11,P14,P15,P16,P19,P20 and for E/F/G all-tests-first. Dual P0-A / create / P0-B remain. PLAN-PLUGINHANDOFF-001 still not in store.
- B2: Section 3 current-state matched this pass's live todo_get: PLUGINCORE-004 open tasks 1-8 Done=false; PLUGININT P0 Done=true P1-P20 Done=false; sln PluginIntegration no matches; HANDOFF-001 tasks 1-10 Done=true task 11 Done=false; HANDOFFPLAN Codex NOT APPROVED; HOSTILEREVIEW FunctionalRequirements null; HYGIENE FunctionalRequirements null keep open; WIKIEXPORT still FR-MCP-WIKIEXPORT-001/002. Drain hardcoded return 2 still at plugins/core/lib-ps/repl-invoke.ps1:602.
- B3: Plan forbids YAML TODO create and direct TODO.yaml writes.
- B4: pwsh.exe -NoProfile -NonInteractive; no Python in this review.
- Wiki dump tables[] vs McpDbContext: 62 DbSet CLR property names (McpDbContext.cs 83-266, `=> Set<>()` form). Plan locked tables[] CSV has 62 names ending ProductWorkspaceMemberships. Sets match.

## Claims reviewed

### A Requested

#### A1. D0 inventories Handoff* tests including GetAndApprove, Validate_InvalidFields, PublicSurfaces, Dispatcher

Verdict: PASS (narrow including-clause). Completeness scored under A-expand / B1 / D.

Evidence: plan.md D0 Controller line names GetAndApprove_DelegateToSharedService. Validator line names HandoffTodoDraftValidatorTests.Validate_InvalidFields_ProduceFieldDiagnostics. MCP tools line names PublicSurfaces_ExposeIngestGetAndApprove. REPL line names Dispatcher_RoutesHandoffWorkflowMethods. On-disk: HandoffControllerTests.cs:34, HandoffTodoDraftValidatorTests.cs:13, HandoffMcpToolTests.cs:79, HandoffWorkflowTests.cs:15.

#### A2. TEST-HANDOFF-004 AC1 maps to Validate_InvalidFields, not eight new HandoffDraft_Invalid* reds

Verdict: PASS

Evidence: plan.md 451 and 474. Testing-Requirements.md 35 AC1 "Invalid or conflicting draft fields produce field-specific diagnostics." Validate_InvalidFields_ProduceFieldDiagnostics asserts field diagnostics for id, title, section, priority, estimate, confidence, dependsOn, functionalRequirements, technicalRequirements, implementationTasks (HandoffTodoDraftValidatorTests.cs 13-42). D1 does not list HandoffDraft_Invalid* as new reds. Task 21 stale wording is scored under D, not this claim.

#### A3. TEST-HANDOFF-006 AC1 maps API/client/REPL/Director/MCP covering tests plus remaining PluginHandoffSkill invoke red

Verdict: PASS

Evidence: plan.md 453 and 479. On-disk covering tests exist and are named. Remaining PluginHandoffSkill_InvokeIngestGetApprove_UsesDocumentedWorkflowMethods is ZERO on disk. PluginSync_HandoffSkill_MatchesCoreArtifact is checksum-only (HandoffMcpToolTests.cs 113-123). PluginSkillWorkflow_InvokesTypedClientHandoffEndpoints is C# workflow invoke (HandoffSkillDelegationTests.cs 67-91), classified reuse, not the remaining Pester skill-file invoke.

Residual (not a FAIL of this claim): Director get/approve execution is only PublicSurfaces NotNull PrimaryCommand plus aliases; HandoffIngestDirectorCommand is the only Director execute test. AC1 says "exist on" those surfaces; PublicSurfaces asserts handoff-get and handoff-approve aliases.

#### A4. D1 new reds are only the four named methods with zero on-disk matches

Verdict: PASS (exact names). Remaining-gap honesty of the AgentPool name is scored under B1/D.

Evidence: tests/ grep ZERO for the four names. ProcessingLease renewal loop exists in src (HandoffIngestionService.cs 136, 972) without a named renewal test; takeover tests are in D0. Provenance remaining vs IngestAsync_CustomPromptTemplate_IsRejected is written. PluginHandoffSkill remaining vs checksum/C# workflow reuse is written.

#### A5. Hostile-after-red-before-implement remains for C/E/F/G. Dual P0-A/P0-B remain. PLAN-PLUGINHANDOFF-001 still not in store

Verdict: PASS

Evidence: plan.md 341-352, 541, 576, 612, 764-768. Sections 4 and 17 still require P0-A AGREE before create, then P0-B. todo_get PLAN-PLUGINHANDOFF-001: TODO 'PLAN-PLUGINHANDOFF-001' not found. Skip hits in plan.md are Failed 0 Skipped 0 gates and TreatsSkipAsFail / NukeTarget_SkipIsFailure. No Fact(Skip=) placeholders. Mixed "Hostile after P1-P4" language is absent.

#### A6. 173914Z FAIL list is closed

Verdict: FAIL

173914Z explicit FAIL vs this plan:

1. GetAndApprove_DelegateToSharedService zero plan hits: CLOSED (A1 PASS).
2. TEST-HANDOFF-004 AC1 eight HandoffDraft_Invalid* new reds / omitted Validate_InvalidFields: CLOSED (A2 PASS).
3. TEST-HANDOFF-006 AC1 omitted API/REPL/PublicSurfaces covering tests: CLOSED (A3 PASS).
4. FAIL class "TEST-HANDOFF AC mapping / D0 remaining-gap inventory is still incomplete against on-disk covering tests": NOT CLOSED. D0 still omits a complete method inventory. Three of the 21 HandoffIngestionServiceTests facts that 173914Z already required as inventoried are now absent from the whole plan file. AgentPool/Publish covering tests for P1-5 are also absent. Task 21 still advertises FR-HANDOFF-003 field diagnostics as a D1 remaining red.

173914Z "what would be required for a later AGREE" named the four covering tests. Those names are present. Completeness of D0 inventory was also required by the plan's own D0 contract (plan.md 427) and by the 173914Z FAIL class. That half is still open.

#### A-expand. D0 complete Handoff* inventory

Verdict: FAIL

Observation: D0 contract (plan.md 427): "D0 reuse is the complete Handoff* test method inventory under tests/ (excluding Dispose and nested helper members). If a later grep finds another Handoff* test, add it here as reuse."

Observation: HandoffIngestionServiceTests has 21 Fact/Theory methods. D0 names only five of them plus "listed previously". Whole-plan Contains is false for UnknownSourceNotes, ExtractorCancelled, ArtifactSource. DraftOnly / CreateWhenConfident / RevalidatesThenCreates appear in the D1 AC map (plan.md 474-476) but not in the D0 reuse inventory block.

Observation: EnqueueOneShotAsync_HandoffContext_ExecutorGetsRawPromptAndSurfacesStayRedacted, Publish_HandoffContext_RedactsRawSource, and PostgreSql_CleanDatabase_AppliesHandoffMigrationAndPersistsEntity are on-disk Handoff-named tests with zero plan hits. MarkerPromptTemplate_ContainsDecisionCompletePlanHandoffGuidance also matches Handoff* but is marker planning-standard text, not Handoff ingest; residual only.

### B Workspace rules

#### B1. Byrd v4 for this class-1 plan-completeness gate

Verdict: FAIL

hostile-phase-gates.md and BDPv4 apply because this is P0-A. Mixed red+green hostiles remain gone. B1 30s-while-drain named test remains on plan.md 276 and 305. Remaining process hole: D0/D1 cannot be executed faithfully as "remaining gaps only" while on-disk covering tests are omitted from D0. Decision 22 (existing green tests are never relabeled as new reds; D0/B1 classifies reuse vs remaining gap before any new test file is written) is violated by omission for the AgentPool/Publish greens and the three unlisted ingestion facts.

#### B2. Receipts / honesty of plan current-state

Verdict: PASS

Section 3 vs live todo_get 2026-08-21T19:59Z matches the child TODO open/done flags, PLUGININT P0 done, missing PluginIntegration project, HANDOFF task 11 open, HOSTILEREVIEW/HYGIENE null FR arrays, WIKIEXPORT still linked to 001/002, drain return 2.

#### B3. MCP-only storage instruction

Verdict: PASS

Plan forbids YAML TODO create and direct TODO.yaml writes. Requirements via MCP store. Markdown is projection.

#### B4. PowerShell / no Python

Verdict: PASS

Section 2 item 9. This review used pwsh.exe and MCP tools only. No python invocation.

### C Requirements (plan capture, not product-done)

Verdict: FAIL

Plan still captures HOSTILEREVIEW 001-006 1:1, HYGIENE 001-005 1:1 with numbered AC, WIKIEXPORT 003-005, and TR-MCP-REPL-012 exception language. Numbered TEST-HANDOFF-001..007 AC bullets in docs/Project/Testing-Requirements.md 19-52 each still have at least one method name in plan.md 468-482, including 004 AC1 and 006 AC1 covering methods.

FAIL is remaining-gap / covering-test fidelity: FR-HANDOFF-002 unknown-or-missing source notes has on-disk IngestAsync_UnknownSourceNotes_AreNotDiscarded with zero plan hits. P1-5 AgentPool raw-source covering tests are unmapped while D1 invents a new name. That is the same C defect class as 173914Z, shifted off 004/006 onto other on-disk covering tests.

Wiki dump tables[] vs McpDbContext: 62/62 (expression-bodied DbSet properties).

### D Current plan holistically (this plan's DoD)

Verdict: FAIL

P0-A DoD in plan sections 2, 4, 14, 17, 19: every TODO has a phase (PASS), named tests for remaining AC (004/006 named covering tests PASS; remaining-gap inventory FAIL), BDPv4 hostile between phases (PASS A5, FAIL B1 remaining-gap inventory), decision-complete dump surface and copy range 1-19 (PASS), no implementation before P0-B (PASS), wiki 003-005 (PASS), HANDOFFPLAN not a second product (PASS). Cross-step consistency FAIL: section 18 task 21 still says "D1 remaining reds including FR-HANDOFF-003 field diagnostics and plugin skill invocation" after D1 deleted those field-diagnostic new reds. A P0-A AGREE is not available.

## Accuracy and completeness

Accuracy: 94. Live todo_get, health nonce echo, on-disk greps, D0 vs D1 substring Contains, HandoffIngestionServiceTests 21/21 fact names, DbSet 62/62, drain return 2, and the 173914Z named covering test files were re-read.
Completeness: 90. Did not execute product test suites (review-only). Did not dump the full requirements_list store (existing family capture used live TODO links plus docs/Project projections). Did not re-run every Handoff*Tests class as a suite.

## What would be required for a later AGREE

- Put every HandoffIngestionServiceTests fact name into D0 by name. Do not say "listed previously". Restore UnknownSourceNotes, ExtractorCancelled, and ArtifactSource at minimum.
- Put D0 reuse entries for EnqueueOneShotAsync_HandoffContext_ExecutorGetsRawPromptAndSurfacesStayRedacted and Publish_HandoffContext_RedactsRawSource. Either drop AgentPool_DoesNotRetainRawHandoffSourceInPromptState or write an explicit remaining gap that those greens do not cover (and that does not fight executor-gets-raw).
- Put D0 reuse for PostgreSql_CleanDatabase_AppliesHandoffMigrationAndPersistsEntity or record why it is out of the Handoff ingest inventory.
- Rewrite section 18 task 21 so it no longer lists FR-HANDOFF-003 field diagnostics as a D1 remaining red.
- Keep the four 173914Z covering tests in D0 and the 004/006 maps. Keep the four D1 exact-name new reds only if they remain zero-hit after the reuse classification. Keep P0-A / P0-B. Do not create PLAN-PLUGINHANDOFF-001 until a later P0-A AGREE.

## Session identifiers

- SessionId: GrokSubagentHostile-20260821T195919Z-p0a-plan
- RequestId: req-20260821T195919Z-001-hostile-p0a-d0-d1-claims
- TurnId: 42719
- Receipt: docs/receipts/hostile-validator-20260821T200636Z.md
- Twin: docs/receipts/hostile-validator-20260821T200636Z.json
