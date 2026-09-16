# Hostile Validator Receipt

TimestampUtc: 2026-08-21T20:18:13Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN / P0-A completeness). Surfaces A+B+C+D all apply. C is plan capture of FR/TR/TEST/AC. D is this plan's DoD.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (marker agent_plugins.Grok plugin_version 1.97.0)
Active plan: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a02413-e4ee-7513-bd22-9eddd82adb73\plan.md
Prior attack surface (not proof): docs/receipts/hostile-validator-20260821T200636Z.md (DISAGREE). Implementer claims those FAILs are closed by inlining all 21 HandoffIngestionServiceTests facts, AgentPool/one-shot redaction tests, PostgreSql_CleanDatabase_AppliesHandoffMigrationAndPersistsEntity, dropping AgentPool new-red, and rewriting section 18 task 21.
OverallVerdict: DISAGREE

Default was FAIL or UNKNOWN until this pass re-read the current plan.md in full (LastWriteTimeUtc 2026-08-21T20:11:55.5811851Z), mechanically diffed every Handoff* test-file Fact/Theory name against the D0 reuse block, grepped D1 new-red names under tests/, live todo_get of eight children plus PLAN-PLUGINHANDOFF-001, and compared wiki tables[] to McpDbContext DbSets. Implementer chat was not trusted.

## Clock and live MCP

TimestampUtc is Get-Date -AsUTC 2026-08-21T20:18:13Z (receipt id 20260821T201813Z). Session open/begin used 20260821T201324Z. Health GET /health?nonce=59813eb01c4b4ddbb53289d951ec89ca returned 200 Healthy and echoed that nonce. Live todo_get: PLAN-PLUGINHANDOFF-001 not found. Live todo_get succeeded for MCP-PLUGINCORE-004, MCP-PLUGININT-001, MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, MCP-HANDOFFREVIEW-001, MCP-HOSTILEREVIEW-001, MCP-WORKSPACEHYGIENE-002, MCP-WIKIEXPORT-001.

## Session log proof

Persisted. SessionId GrokSubagentHostile-20260821T201324Z-p0a-plan. Turn requestId req-20260821T201324Z-001-hostile-p0a-closed-fails. sessionlog_open created=true. sessionlog_begin_turn success turnId 42724. sessionlog_complete_turn status completed. sessionlog_query agent=GrokSubagentHostile from=2026-08-21T20:10:00Z totalCount=1, turn status completed, 7 actions, 4 designDecisions, 5 processingDialog items.

## Mandatory surface that could not be evaluated

None. Live eight-TODO bodies were retrieved. PLAN TODO absence was retrieved. Reviewer session-log turn is required and is persisted through MCP tools. Product test suites were not executed (review-only).

## Explicit FAIL list

- A5: 200636Z FAIL list is not closed. Named 200636Z omissions are now in D0 or rewritten (21 ingestion facts inlined; AgentPool/Publish/PostgreSql named; task 21 no longer lists FR-HANDOFF-003 field diagnostics as a D1 remaining red; AgentPool is reuse). The FAIL class remains: D0 is still not the complete Handoff* inventory it contracts to be. Mechanical diff of *Handoff*.cs Fact/Theory methods vs the D0 reuse block (plan.md 429-449) is 112 present, 1 missing: HandoffAuditDefectTests.IngestAsync_SecretInDraft_IsRedactedOnPersist (tests/McpServer.Support.Mcp.Tests/Services/HandoffAuditDefectTests.cs:152). D0 Audit line 442 lists six sibling audit facts and omits SecretInDraft. SecretInDraft appears only in the TEST-HANDOFF-007 AC2 map (plan.md 485), which is not the D0 inventory.
- A-expand: D0 contract (plan.md 427) is "the complete Handoff* test method inventory under tests/ (excluding Dispose and nested helper members). If a later grep finds another Handoff* test, add it here as reuse." Later grep found IngestAsync_SecretInDraft_IsRedactedOnPersist in a Handoff* file. D0 does not name it. Same defect class as 200636Z "listed previously" collapse, now one remaining audit fact instead of three ingestion facts.
- B1: BDPv4 remaining-gap classification is still not trustworthy while D0's own complete-inventory contract is false. Decision 22 requires D0/B1 to classify reuse vs remaining gap before new tests are written. Audit facts are enumerated as reuse except SecretInDraft, which is an on-disk green covering TEST-HANDOFF-007 AC2.
- D: P0-A DoD requires existing green Handoff tests classified reuse vs remaining gap via the D0 inventory before new tests. That DoD is not met while D0 Audit omits SecretInDraft. Task 21 vs D1 consistency is now PASS and does not save D.

## Explicit PASS (do not treat as AGREE)

- A1: D0 names IngestAsync_UnknownSourceNotes_AreNotDiscarded, IngestAsync_ExtractorCancelled_DoesNotCreateTodo, IngestAsync_ArtifactSource_UsesDocumentChunks, EnqueueOneShotAsync_HandoffContext_ExecutorGetsRawPromptAndSurfacesStayRedacted, Publish_HandoffContext_RedactsRawSource, PostgreSql_CleanDatabase_AppliesHandoffMigrationAndPersistsEntity (plan.md 437-439). All six strings have D0 hits. On-disk: HandoffIngestionServiceTests.cs:132/323/429, AgentPoolServiceTests.cs:222, OneShotSensitivePromptPolicyTests.cs:12, ProviderDatabaseIntegrationTests.cs:83.
- A2: AgentPool_DoesNotRetainRawHandoffSourceInPromptState is not a D1 new red. D1 new-red list is three names only (PluginHandoffSkill_InvokeIngestGetApprove_UsesDocumentedWorkflowMethods, ProcessingLease_RenewsAndFencesTerminalUpdates, Provenance_IncludesEffectiveCustomPromptIdentityAndVersion). Plan.md 463: "Do not add AgentPool_DoesNotRetainRawHandoffSourceInPromptState as a new red. That finding is reuse of EnqueueOneShotAsync_HandoffContext_ExecutorGetsRawPromptAndSurfacesStayRedacted and Publish_HandoffContext_RedactsRawSource." tests/ grep of AgentPool_DoesNotRetainRawHandoffSourceInPromptState is ZERO.
- A3: Section 18 task 21 is "D1 remaining reds only: PluginHandoffSkill invoke, lease renewal, effective prompt identity. Field diagnostics stay D0 reuse (`Validate_InvalidFields_ProduceFieldDiagnostics`). AgentPool redaction stays D0 reuse." Literal substring "FR-HANDOFF-003 field diagnostics" is absent from the plan. "listed previously" is absent.
- A4: Dual P0-A / create / P0-B remain (plan.md 64-82, 716-718, 727-728, 763-764). todo_get PLAN-PLUGINHANDOFF-001: TODO 'PLAN-PLUGINHANDOFF-001' not found.
- B2: Section 3 current-state matched this pass's live todo_get: PLUGINCORE-004 tasks 1-8 Done=false; PLUGININT P0 Done=true P1-P20 Done=false; sln PluginIntegration no matches; HANDOFF-001 tasks 1-10 Done=true task 11 Done=false; HANDOFFPLAN Codex NOT APPROVED; HOSTILEREVIEW FunctionalRequirements null; HYGIENE FunctionalRequirements null keep open; WIKIEXPORT still FR-MCP-WIKIEXPORT-001/002. Drain hardcoded return 2 still at plugins/core/lib-ps/repl-invoke.ps1:602.
- B3: Plan forbids YAML TODO create and direct TODO.yaml writes.
- B4: pwsh.exe -NoProfile -NonInteractive; no Python in this review.
- C: 200636Z C holes are closed. UnknownSourceNotes is D0 reuse. AgentPool/Publish are D0 reuse, not D1 new reds. TEST-HANDOFF-001..007 numbered AC bullets still have method names in plan.md 471-485, including 004 AC1 and 006 AC1 covering methods plus remaining PluginHandoffSkill. Residual (not FAIL): UnknownSourceNotes is not in the AC-to-method map; MarkerPromptTemplate_ContainsDecisionCompletePlanHandoffGuidance is a Handoff-named marker test with zero plan hits (same residual as 200636Z).
- Wiki dump tables[] vs McpDbContext: 62/62. Compare-Object empty. Ends ProductWorkspaceMemberships.

## Claims reviewed

### A Requested

#### A1. D0 names the six 200636Z omitted covering tests

Verdict: PASS

Evidence: plan.md 437-439. pwsh needle scan: all six plan=True d0=True. HandoffIngestionServiceTests still has 21 Fact/Theory methods and D0 names all 21 (no "listed previously").

#### A2. AgentPool_DoesNotRetainRawHandoffSourceInPromptState is reuse, not a D1 new red

Verdict: PASS

Evidence: D1_NEW_BLOCK contains three bullets only. Line 463 forbids the AgentPool new-red name. On-disk greens remain AgentPoolServiceTests.cs:222 (Assert.Equal(raw, captured.UserTranscriptText) plus rendered-prompt redaction) and OneShotSensitivePromptPolicyTests.cs:12.

#### A3. Section 18 task 21 no longer says FR-HANDOFF-003 field diagnostics as D1 remaining reds

Verdict: PASS

Evidence: plan.md line 747 (0-based 746). Whole-plan Contains of "FR-HANDOFF-003 field diagnostics" is false.

#### A4. Dual P0-A/P0-B remain. PLAN-PLUGINHANDOFF-001 not in store

Verdict: PASS

Evidence: live todo_get not found. Plan sections 4, 17, 18, 19 still require P0-A AGREE before create, then P0-B.

#### A5. 200636Z FAIL list closed

Verdict: FAIL

200636Z explicit FAIL vs this plan:

1. Three HandoffIngestionServiceTests facts zero plan hits: CLOSED (A1 PASS).
2. EnqueueOneShot / Publish / PostgreSql_CleanDatabase zero plan hits: CLOSED (A1 PASS).
3. Section 18 task 21 lists FR-HANDOFF-003 field diagnostics as D1 remaining red: CLOSED (A3 PASS).
4. AgentPool D1 new-red vs existing greens: CLOSED (A2 PASS).
5. FAIL class "D0 is still not a complete Handoff* inventory against on-disk covering tests": NOT CLOSED. D0 Audit still omits IngestAsync_SecretInDraft_IsRedactedOnPersist.

200636Z "what would be required for a later AGREE" named the 21 ingestion facts, AgentPool/Publish reuse, PostgreSql, and task 21 rewrite. Those named items are present. Completeness of D0 inventory was also required by plan.md 427 and by the 200636Z FAIL class. That half is still open.

#### A-expand. D0 complete Handoff* inventory

Verdict: FAIL

Observation: pwsh inventory of tests/**/*Handoff*.cs public Task/void methods, excluding Dispose and nested Todo/clock helpers: 113 classified methods, 112 in D0, 1 missing.

Observation: MISSING|HandoffAuditDefectTests.cs::IngestAsync_SecretInDraft_IsRedactedOnPersist. File has seven [Fact] methods; D0 Audit names six.

Observation: Handoff-named methods outside Handoff* files: EnqueueOneShot and Publish and PostgreSql are in D0. Only MarkerPromptTemplate_ContainsDecisionCompletePlanHandoffGuidance has zero plan hits; residual (marker planning-standard text, not Handoff ingest), same as 200636Z.

Observation: Sqlite_CleanDatabase_AppliesMigrationsAndPersistsEntity and SqlServer_LocalDb_CleanDatabase_AppliesMigrationsAndPersistsEntity call AssertHandoffIngestionStorageAsync but method names are not Handoff*. Residual only; 200636Z required the PostgreSql Handoff-named method, which is now in D0.

### B Workspace rules

#### B1. Byrd v4 for this class-1 plan-completeness gate

Verdict: FAIL

hostile-phase-gates.md and BDPv4 apply because this is P0-A. Mixed red+green hostiles remain gone. B1 30s-while-drain named test remains. Remaining process hole: D0 contract is complete inventory; Audit enumeration is incomplete by one on-disk green.

#### B2. Receipts / honesty of plan current-state

Verdict: PASS

Section 3 vs live todo_get 2026-08-21T20:13Z matches the child TODO open/done flags, PLUGININT P0 done, missing PluginIntegration project, HANDOFF task 11 open, HOSTILEREVIEW/HYGIENE null FR arrays, WIKIEXPORT still linked to 001/002, drain return 2.

#### B3. MCP-only storage instruction

Verdict: PASS

Plan forbids YAML TODO create and direct TODO.yaml writes. Requirements via MCP store. Markdown is projection.

#### B4. PowerShell / no Python

Verdict: PASS

Section 2 item 9. This review used pwsh.exe and MCP tools only. No python invocation.

### C Requirements (plan capture, not product-done)

Verdict: PASS

Plan still captures HOSTILEREVIEW 001-006 1:1, HYGIENE 001-005 1:1 with numbered AC, WIKIEXPORT 003-005, and TR-MCP-REPL-012 exception language. Numbered TEST-HANDOFF-001..007 AC bullets in docs/Project/Testing-Requirements.md 19-52 each still have at least one method name in plan.md 471-485, including 004 AC1 and 006 AC1 covering methods.

200636Z C hole (UnknownSourceNotes zero plan hits; AgentPool covering tests unmapped while D1 invents a name) is closed: UnknownSourceNotes is D0 reuse; AgentPool/Publish are D0 reuse.

Residual (not FAIL of C): UnknownSourceNotes is not attached in the locked AC-to-method map (plan.md 471-485). Remaining-gap is still classified reuse via D0, so it does not reopen the 200636Z remaining-gap-trust FAIL.

Wiki dump tables[] vs McpDbContext: 62/62.

### D Current plan holistically (this plan's DoD)

Verdict: FAIL

P0-A DoD in plan sections 2, 4, 14, 17, 19: every TODO has a phase (PASS), named tests for remaining AC (004/006 named covering tests PASS; D1 remaining reds are three zero-hit names PASS), BDPv4 hostile between phases (PASS A4, FAIL B1 remaining-gap inventory), decision-complete dump surface and copy range 1-19 (PASS), no implementation before P0-B (PASS), wiki 003-005 (PASS), HANDOFFPLAN not a second product (PASS). Cross-step task 21 vs D1 is now consistent (PASS vs 200636Z). Completeness FAIL: D0 omits an on-disk HandoffAuditDefectTests green. A P0-A AGREE is not available.

## Accuracy and completeness

Accuracy: 96. Live todo_get, health nonce echo, mechanical D0 vs *Handoff*.cs name diff (112/113), D1 new-red tests/ grep ZERO, DbSet 62/62 Compare-Object empty, drain return 2, and the 200636Z named covering tests were re-read.
Completeness: 91. Did not execute product test suites (review-only). Did not dump the full requirements_list store (existing family capture used live TODO links plus docs/Project projections). Did not re-run every Handoff*Tests class as a suite.

## What would be required for a later AGREE

- Put `IngestAsync_SecretInDraft_IsRedactedOnPersist` into the D0 Audit reuse line by name. Keep it as covering TEST-HANDOFF-007 AC2, not a D1 new red.
- Keep the 21 ingestion facts, AgentPool/Publish/PostgreSql D0 reuse, AgentPool not a D1 new red, and task 21 field-diagnostics-as-reuse wording.
- Keep the three D1 exact-name new reds only if they remain zero-hit. Keep P0-A / P0-B. Do not create PLAN-PLUGINHANDOFF-001 until a later P0-A AGREE.
- Residual, not currently FAIL: optionally inventory MarkerPromptTemplate as non-ingest residual; optionally attach UnknownSourceNotes to TEST-HANDOFF-003 in the AC map.

## Session identifiers

- SessionId: GrokSubagentHostile-20260821T201324Z-p0a-plan
- RequestId: req-20260821T201324Z-001-hostile-p0a-closed-fails
- TurnId: 42724
- Receipt: docs/receipts/hostile-validator-20260821T201813Z.md
- Twin: docs/receipts/hostile-validator-20260821T201813Z.json
