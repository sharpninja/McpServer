# Hostile Validator Receipt

TimestampUtc: 2026-08-21T17:39:14Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN / P0-A completeness). Surfaces A+B+C+D all apply. C is plan capture of FR/TR/TEST/AC. D is this plan's DoD.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (marker agent_plugins.Grok plugin_version 1.97.0)
Active plan: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a02413-e4ee-7513-bd22-9eddd82adb73\plan.md
Prior attack surface (not proof): docs/receipts/hostile-validator-20260821T161800Z.md; docs/receipts/hostile-validator-20260821T171357Z.md; docs/receipts/hostile-validator-20260821T172735Z.md (DISAGREE, 5 FAIL)
OverallVerdict: DISAGREE

Default was FAIL or UNKNOWN until this pass re-read the current plan.md in full, Testing-Requirements.md TEST-HANDOFF-001..007 AC bullets, live todo_get of eight children plus PLAN-PLUGINHANDOFF-001, on-disk Handoff test method names, B1 new-red list, and McpDbContext DbSets. Implementer chat was not trusted.

## Clock and live MCP

TimestampUtc is Get-Date -AsUTC 2026-08-21T17:39:14Z (receipt id 20260821T173914Z). Session open/begin used 20260821T173330Z. Live todo_get: PLAN-PLUGINHANDOFF-001 not found. Live todo_get succeeded for MCP-PLUGINCORE-004, MCP-PLUGININT-001, MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, MCP-HANDOFFREVIEW-001, MCP-HOSTILEREVIEW-001, MCP-WORKSPACEHYGIENE-002, MCP-WIKIEXPORT-001.

## Session log proof

Persisted. SessionId GrokSubagentHostile-20260821T173330Z-p0a-plan. Turn requestId req-20260821T173330Z-001-hostile-p0a-plan-completeness. sessionlog_open created=true. sessionlog_begin_turn success turnId 42680. sessionlog_dialog totalDialogItems 3. sessionlog_replace_section actions and designDecisions replaced=true. sessionlog_complete_turn status completed. sessionlog_query agent=GrokSubagentHostile from=2026-08-21T17:30:00Z totalCount=1, turn status completed, 6 actions, 4 designDecisions, 3 processingDialog items.

## Mandatory surface that could not be evaluated

None. Live eight-TODO bodies were retrieved. PLAN TODO absence was retrieved. Reviewer session-log turn is required and is persisted through MCP tools.

## Explicit FAIL list

- A4: 172735Z FAIL list is not closed. The four named 172735Z zero-hit tests are now in the map and D0, and the 30s-while-drain test is now on B1. The FAIL class remains: TEST-HANDOFF AC mapping / D0 remaining-gap inventory is still incomplete against on-disk covering tests. 172735Z AGREE extras named `HandoffControllerTests.GetAndApprove_DelegateToSharedService` for TEST-HANDOFF-006 get/approve. Current plan.md grep of that name is zero hits.
- A-expand / C: TEST-HANDOFF-004 AC1 and TEST-HANDOFF-006 AC1 are not faithfully mapped to existing covering methods. TEST-HANDOFF-004 AC1 maps eight D1 new-red names (`HandoffDraft_InvalidId_FieldDiagnostic` through `HandoffDraft_InvalidRequirementLinks_FieldDiagnostic`) and does not name `HandoffTodoDraftValidatorTests.Validate_InvalidFields_ProduceFieldDiagnostics`, which already asserts field diagnostics for id, title, section, priority, estimate, dependsOn, functionalRequirements, technicalRequirements, and implementationTasks (`tests/McpServer.Support.Mcp.Tests/Services/HandoffTodoDraftValidatorTests.cs` 13-42). TEST-HANDOFF-006 AC1 (ingest, get, and approve on API, client, REPL, Director, MCP tools, and plugin skill) names MCP ingest, Director ingest, plugin workflow, and a D1 plugin skill test. It does not name `HandoffControllerTests.IngestAsync_DelegatesToSharedService`, `HandoffControllerTests.GetAndApprove_DelegateToSharedService`, `HandoffMcpToolTests.PublicSurfaces_ExposeIngestGetAndApprove`, or `HandoffWorkflowTests.Dispatcher_RoutesHandoffWorkflowMethods`.
- B1: BDPv4 remaining-gap classification is still not trustworthy. D0 inventories the 21 `HandoffIngestionServiceTests` facts and does not classify the on-disk covering tests named above as reuse. D1 therefore treats TEST-HANDOFF-004 AC1 as eight new reds. The 172735Z B1 30s-while-drain omission is closed (plan.md 305).
- D: P0-A DoD requires named tests for remaining AC and that existing green Handoff tests are classified reuse vs remaining gap before new tests are written. That DoD is not met while TEST-HANDOFF-006 get/approve API/REPL covering tests and TEST-HANDOFF-004 validator field diagnostics are invisible to D0/D1.

## Explicit PASS (do not treat as AGREE)

- A1: Every TEST-HANDOFF-001..007 AC bullet in `docs/Project/Testing-Requirements.md` now has at least one method name in the P0-A map (plan.md 476-490). The four named 172735Z omissions are present: TEST-HANDOFF-002 AC2 `IngestAsync_SupportedFormats_AreAccepted`, TEST-HANDOFF-005 AC2 `IngestAsync_ForceTrue_ExtractsAgain`, TEST-HANDOFF-004 AC2 `IngestAsync_AmbiguousHandoff_RequiresReview`, TEST-HANDOFF-005 `IngestAsync_TodoServiceFailure_RequiresReview`.
- A2: D0 reuse inventories all 21 `HandoffIngestionServiceTests` facts: SupportedFormats, UnsafeSources, MalformedExtractorJson, UnknownSourceNotes, DraftOnly, LowConfidence, CreateWhenConfident, SameHashAndPrompt, DuplicateTodoId, PersistsProvenanceWithoutSourceContent, ApproveAsync_RevalidatesThenCreates, ExtractorCancelled, MissingExtractorFields, AmbiguousHandoff, TodoServiceFailure, ForceTrue, ArtifactSource, ReparseEscape, DifferentWorkspace, ConcurrentApprovals, BlankDescriptionAndTechnicalDetails. Class method count is 1 Theory + 20 Fact. Dispose is not a fact.
- A3: B1 new-red list includes `Get-ReplMethodTimeoutSeconds_CompleteTurnBeginTurn_RemainThirtySecondsWhileDrainSubmitUsesDrainTimeout` (plan.md 305, also A5 line 276).
- A4b: Dual P0-A / create / P0-B remain (plan.md 25-26, 63-82, 711-726). PLAN-PLUGINHANDOFF-001 still not in store.
- A4c: No skipped-test placeholders. Skip hits in plan.md are Failed 0 Skipped 0 gates, `TreatsSkipAsFail`, and `NukeTarget_SkipIsFailure`.
- A4d: Hostile-after-red-before-implement remains written for C groups P1,P4,P5,P7,P11,P14,P15,P16,P19,P20 and for E/F/G all-tests-first. D1.5 remains.
- A5: Listed existing green tests are classified reuse, not placed on D1/B1 new-red lists. The D1 `HandoffDraft_Invalid*` names are new names, not relabels of `Validate_InvalidFields_ProduceFieldDiagnostics`. Omission of that green is scored under A-expand/C/D, not as a relabel.
- B2: Section 3 current-state matched this pass's live todo_get: PLUGINCORE-004 open tasks 1-8 Done=false; PLUGININT P0 Done=true P1-P20 Done=false; sln PluginIntegration no matches; HANDOFF-001 tasks 1-10 Done=true task 11 Done=false; HANDOFFPLAN Codex NOT APPROVED; HOSTILEREVIEW FunctionalRequirements null; HYGIENE FunctionalRequirements null keep open; WIKIEXPORT still FR-MCP-WIKIEXPORT-001/002. Drain hardcoded `return 2` still at plugins/core/lib-ps/repl-invoke.ps1 602.
- B3: Plan forbids YAML TODO create and direct TODO.yaml writes.
- B4: pwsh.exe -NoProfile -NonInteractive; no Python in this review.
- Wiki dump tables[] vs McpDbContext: DbSetCount 62, PlanSetCount 62. Regex split left a trailing period on ProductWorkspaceMemberships; after stripping sentence punctuation the sets match.

## Claims reviewed

### A Requested

#### A1. TEST-HANDOFF-001 through 007 AC mapped by AC number including the four named tests

Verdict: PASS

Evidence: Testing-Requirements.md 19-52 has 16 AC bullets. Plan.md 476-490 maps each AC number to at least one method name. The four 172735Z examples are in that map (002 AC2 SupportedFormats, 005 AC2 ForceTrue, 004 AC2 AmbiguousHandoff, 005 TodoServiceFailure as also-reuse under 005 AC3). User floor "if any AC still has no method name, FAIL" is not triggered. Incomplete mapping of 004 AC1 and 006 AC1 is scored under A-expand/C, not this claim.

#### A2. D0 inventories every HandoffIngestionServiceTests fact

Verdict: PASS

Evidence: live method list from `tests/McpServer.Support.Mcp.Tests/Services/HandoffIngestionServiceTests.cs` matches the 21 names on plan.md 440. BlankDescription is also listed earlier at 437.

#### A3. B1 new-red list includes the 30s-while-drain named test

Verdict: PASS

Evidence: plan.md 305. 172735Z B1 omission of that name is closed.

#### A4. Prior 172735Z FAIL list is closed

Verdict: FAIL

172735Z explicit FAIL vs this plan:

1. TEST-HANDOFF-002 AC2 / 005 force=true / AmbiguousHandoff / TodoServiceFailure unmapped: CLOSED as named examples (A1 PASS).
2. A9 171357Z remaining TEST-HANDOFF AC map / D0 inventory: NOT CLOSED. Same class remains via Validate_InvalidFields and TEST-HANDOFF-006 get/approve covering tests.
3. B1 omit 30s-while-drain test: CLOSED (A3 PASS). D0 incomplete half of that B1 FAIL is NOT CLOSED.

172735Z "what would be required for a later AGREE" item 1 explicitly required D0/map entries for `HandoffControllerTests.GetAndApprove_DelegateToSharedService`. Current plan.md has zero hits for that string.

#### A4b. Dual P0-A/P0-B remain. PLAN-PLUGINHANDOFF-001 still not in store

Verdict: PASS

Evidence: todo_get PLAN-PLUGINHANDOFF-001: TODO 'PLAN-PLUGINHANDOFF-001' not found. Plan sections 4 and 17 still require P0-A AGREE before create, then P0-B.

#### A4c. No skip placeholders

Verdict: PASS

Evidence: plan.md Skip hits are gates and skip-is-failure tests, not `[Fact(Skip=...)]` placeholders.

#### A4d. Hostile-after-red-before-implement for C/E/F/G remains as previously PASS

Verdict: PASS

Evidence: plan.md 341-352, 549-549, 584, 620, 764-778. Mixed red+green hostiles from 171357Z remain absent.

#### A5. Existing green tests are reuse, not new reds

Verdict: PASS

Evidence: D0 reuse list and B1 reuse vs new-red split. The 21 HandoffIngestionServiceTests facts are not on the D1 new-red list. D1 `HandoffDraft_Invalid*` are new names. Omission of other greens is A-expand/C/D.

#### A-expand. TEST-HANDOFF-004 AC1 and TEST-HANDOFF-006 AC1 covering tests

Verdict: FAIL

Observation: `Validate_InvalidFields_ProduceFieldDiagnostics` exists and is tagged TEST-HANDOFF-004. Plan D1 still says field-specific diagnostics "still need named tests beyond blank description/technicalDetails" and lists eight new reds. Plan grep of `HandoffTodoDraftValidator` and `Validate_InvalidFields` is empty.

Observation: TEST-HANDOFF-006 AC1 requires ingest, get, and approve on API, client, REPL, Director, MCP tools, and plugin skill. On-disk covering tests exist: `HandoffControllerTests.IngestAsync_DelegatesToSharedService` (API ingest), `GetAndApprove_DelegateToSharedService` (API get/approve), `PublicSurfaces_ExposeIngestGetAndApprove` (MCP tools plus Director aliases plus skill ingest string), `Dispatcher_RoutesHandoffWorkflowMethods` (REPL ingest/get/approve). None of those names appear in plan.md.

### B Workspace rules

#### B1. Byrd v4 for this class-1 plan-completeness gate

Verdict: FAIL

hostile-phase-gates.md and BDPv4 apply because this is P0-A. Mixed red+green hostiles remain gone. The 30s-while-drain red is now on B1. Remaining process hole: D0 does not classify on-disk tests that already cover remaining TEST-HANDOFF AC, so D1 "remaining gaps only" cannot be executed faithfully. Decision 22 (existing green tests are never relabeled as new reds; D0/B1 classifies reuse vs remaining gap before any new test file is written) is violated by omission, which produces eight D1 new reds for AC already covered by `Validate_InvalidFields_ProduceFieldDiagnostics`.

#### B2. Receipts / honesty of plan current-state

Verdict: PASS

Section 3 vs live todo_get 2026-08-21T17:39Z matches the child TODO open/done flags, PLUGININT P0 done, missing PluginIntegration project, HANDOFF task 11 open, HOSTILEREVIEW/HYGIENE null FR arrays, WIKIEXPORT still linked to 001/002.

#### B3. MCP-only storage instruction

Verdict: PASS

Plan forbids YAML TODO create and direct TODO.yaml writes. Requirements via MCP store. Markdown is projection.

#### B4. PowerShell / no Python

Verdict: PASS

Section 2 item 9. This review used pwsh.exe and MCP tools only. No python invocation. Temp compare script `docs/receipts/_tmp-hostile-dbset-compare.ps1` was created then deleted (DELETED at 2026-08-21T17:39:14Z).

### C Requirements (plan capture, not product-done)

Verdict: FAIL

Plan still captures HOSTILEREVIEW 001-006 1:1, HYGIENE 001-005 1:1 with numbered AC, WIKIEXPORT 003-005, and TR-MCP-REPL-012 exception language.

FAIL is TEST-HANDOFF AC coverage in the P0-A text: every AC now has a method name, but TEST-HANDOFF-004 AC1 and TEST-HANDOFF-006 AC1 are not mapped to the on-disk methods that already cover those AC bullets. That is the same C defect class as 172735Z, narrowed from zero-hit ACs to remaining unmapped covering tests.

Wiki dump tables[] vs McpDbContext: 62/62 after punctuation trim (2026-08-21T17:38:30Z compare script).

### D Current plan holistically (this plan's DoD)

Verdict: FAIL

P0-A DoD in plan sections 2, 4, 14, 17, 19: every TODO has a phase (PASS), named tests for remaining AC (FAIL A-expand), BDPv4 hostile between phases (PASS A4d, FAIL B1 remaining-gap inventory), decision-complete dump surface and copy range (PASS, copy still 1-19), no implementation before P0-B (PASS), wiki 003-005 (PASS), HANDOFFPLAN not a second product (PASS). A P0-A AGREE is not available.

Residual decision note (not a separate FAIL): WorkspaceCreateRequest dump field and G0 first-red-consumer lock remain an allowed tests-first path if G0 checkpoints the DTO name before G1.

## Accuracy and completeness

Accuracy: 93. Live todo_get, on-disk greps, DbSet 62/62 compare, drain return 2, HandoffIngestionServiceTests 21/21, and the unmapped covering test files were re-read.
Completeness: 91. Did not execute product test suites (review-only). Did not dump the full requirements_list store (existing family capture used live TODO links plus docs/Project projections). Did not re-run every Handoff*Tests class as a suite.

## What would be required for a later AGREE

- Put D0 reuse and TEST-HANDOFF-004 AC1 map entries for `HandoffTodoDraftValidatorTests.Validate_InvalidFields_ProduceFieldDiagnostics`. Drop or reclassify the eight D1 `HandoffDraft_Invalid*` names if they would duplicate that green; keep them only if D0 records a remaining ingest-pipeline gap that the validator test does not cover.
- Put D0 reuse and TEST-HANDOFF-006 AC1 map entries for `HandoffControllerTests.IngestAsync_DelegatesToSharedService`, `HandoffControllerTests.GetAndApprove_DelegateToSharedService`, `HandoffMcpToolTests.PublicSurfaces_ExposeIngestGetAndApprove`, and `HandoffWorkflowTests.Dispatcher_RoutesHandoffWorkflowMethods`.
- Keep the four previously missing HandoffIngestionServiceTests names and the B1 30s-while-drain test.
- Keep P0-A / P0-B. Do not create PLAN-PLUGINHANDOFF-001 until a later P0-A AGREE.

## Session identifiers

- SessionId: GrokSubagentHostile-20260821T173330Z-p0a-plan
- RequestId: req-20260821T173330Z-001-hostile-p0a-plan-completeness
- TurnId: 42680
- Receipt: docs/receipts/hostile-validator-20260821T173914Z.md
- Twin: docs/receipts/hostile-validator-20260821T173914Z.json
