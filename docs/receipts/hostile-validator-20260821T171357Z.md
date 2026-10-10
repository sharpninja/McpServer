# Hostile Validator Receipt

TimestampUtc: 2026-08-21T17:13:57Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN / P0-A completeness). Surface C applies to whether the plan captures FR/TR/TEST/AC. Surface D applies to this plan's own DoD.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (marker agent_plugins.Grok plugin_version 1.97.0)
Active plan: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a02413-e4ee-7513-bd22-9eddd82adb73\plan.md
Prior attack surface (not proof): docs/receipts/hostile-validator-20260821T161800Z.md (DISAGREE, 9 FAIL)
OverallVerdict: DISAGREE

Default was FAIL or UNKNOWN until this pass re-read the current plan.md, BDPv4, live todo_get of the eight child TODOs, on-disk tests, McpDbContext DbSets, WorkspaceController create, and TR-MCP-REPL-012. Implementer chat was not trusted.

## Clock and live MCP

Live todo_get succeeded for MCP-PLUGINCORE-004, MCP-PLUGININT-001, MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, MCP-HANDOFFREVIEW-001, MCP-HOSTILEREVIEW-001, MCP-WORKSPACEHYGIENE-002, MCP-WIKIEXPORT-001. todo_get PLAN-PLUGINHANDOFF-001 returned not found. TimestampUtc is Get-Date -AsUTC 2026-08-21T171357Z.

## Session log proof

Persisted. SessionId GrokSubagentHostile-20260821T170652Z-p0a-plan. Turn requestId req-20260821T170652Z-001-p0a-pluginhandoff-plan. sessionlog_open created=true. sessionlog_begin_turn success turnId 42676. sessionlog_dialog totalDialogItems 3. sessionlog_replace_section actions and designDecisions replaced=true. sessionlog_complete_turn status completed. sessionlog_query agent=GrokSubagentHostile from=2026-08-21T17:00:00Z totalCount=1, turn status completed, 4 actions, 3 designDecisions, 3 processingDialog items.

## Mandatory surface that could not be evaluated

None. Live eight-TODO bodies were retrieved. Reviewer session-log turn persisted.

## Explicit FAIL list

- A2/B1/D: BDPv4 hostile-after-red before implement is not complete. Phase C hostiles fire after mixed P1-P4 / P5-P10 / P11-P15 / P16-P18 red and green. Phase E/F/G hostile after schema/registry red, then after later slices already green, with no after-red AGREE before those implementations. D1.5 exists. Grouped PLUGININT hostiles do not satisfy hostile-phase-gates.md step 2.
- A5/C: Named tests are still missing for remaining AC that the claim said were covered. PLUGININT P19 and P20 have no named tests; TEST-MCP-PLUGININT-001 AC5 native-suite half is an evidence folder, not a failing test. TEST-HANDOFF-001..007 AC remain a D1 instruction ("do not leave without a method name after D1") rather than a P0-A map. D0 reuse omits already-on-disk mode tests such as IngestAsync_DraftOnly_DoesNotCreateTodo.
- A7/D: Decision-incomplete leftovers. P0-B / section 14 copy range is "sections 1-18" while section 19 (inter-phase hostile checklist) exists. add-workspace `--dump` is locked to FederationClient.RegisterWorkspaceAsync (POST federation/proxies/{proxyId}/workspaces) while the live register API is WorkspaceController.CreateAsync / WorkspaceClient.CreateAsync (POST /mcpserver/workspace). Drain timeout amends TEST-MCP-195 / TR-MCP-PERSIST-003 but does not amend or except TR-MCP-REPL-012, which still requires Get-ReplMethodTimeoutSeconds = 30 for sessionlog methods.
- A10: The revised plan does not close the 161800Z FAIL list or the "what would be required for a later AGREE" items. Closed: PLUGININT P1-P20 inline, D1.5, mocks for D/E/F/G, B1 reuse vs new-red split, HOSTILEREVIEW 6/6/6, HYGIENE 5/5/5, TEST-MCP-195 not TEST-MCP-REPL-041, WIKIEXPORT relink instruction, many named tests, drain env/path/wiki tables/hygiene exit/eight-vs-five/Codex+hostile both. Open: the four bullets above.

## Explicit PASS (do not treat as AGREE)

- A1: All eight TODOs have phases. PLUGININT P1-P20 are inlined and match live todo_get task text for P1-P18. Handoff is remediation. HOSTILEREVIEW/HYGIENE/WIKIEXPORT/PLUGINCORE have phases.
- A3: Dual gates P0-A then create PLAN-PLUGINHANDOFF-001 then P0-B remain. Live PLAN-PLUGINHANDOFF-001 not found.
- A4: Plan text assigns HOSTILEREVIEW FR/TR/TEST 001-006, HYGIENE 001-005, WIKIEXPORT 003-005. TEST-MCP-REPL-041 is absent from the plan. Relink of MCP-WIKIEXPORT-001 off 001/002 onto 003-005 is instructed. Live WIKIEXPORT FunctionalRequirements are still 001/002.
- A6: Listed reuse tests exist on disk. Parser Pester, SessionLogPersistenceStrategyTests coordinator/failsafe/V4 facts, and listed Handoff dead-contract/bounded-source/durability/field-blank/skill-workflow tests were grepped present.
- A8: No skipped-test placeholders. done:true requires hostile AGREE, Failed 0 Skipped 0, receipt path in doneSummary.
- A9: Blast radius, three-provider migrations, Nuke UpdateService, SyncAgentPlugins, pwsh-only, no Python are named.
- B2: Section 3 current-state claims matched this pass's live todo_get (parser tests exist; PluginIntegration project absent from sln; Handoff tasks 1-10 done task 11 open; HOSTILEREVIEW/HYGIENE have null FR arrays).
- B3: Plan forbids YAML TODO create and direct TODO.yaml writes.
- B4: pwsh.exe -NoProfile -NonInteractive; no Python.

## Claims reviewed

### A Requested

#### A1. Eight TODOs with inlined executable slices

Verdict: PASS

Evidence: plan.md sections 6-11. Live todo_get 2026-08-21T17:07Z: PLUGININT ImplementationTasks P0 Done=true, P1-P20 Done=false; plan Phase C names P1-P20 with matching red/green intent and named tests for P1-P18. Handoff labeled remediation not rewrite (section 2 item 6; section 8). HOSTILEREVIEW Phase E, HYGIENE Phase F, WIKIEXPORT Phase G, PLUGINCORE Phase B.

Caveat (not a FAIL of coverage): P19/P20 are inlined as ops gates without named tests (scored under A5).

#### A2. BDPv4 order complete including D1.5 and grouped PLUGININT hostiles

Verdict: FAIL

Evidence: plan.md section 19 lists P0-A/P0-B, A6, B2/B5, C after P1-P4 red/green, D1.5, E schema red then later greens. hostile-phase-gates.md requires hostile after red tests exist before implementation. Phase C: "Hostile after P1-P4 red/green" implements before any after-red AGREE. Same for P5-P10, P11-P15, P16-P18. Phase E/F/G: after-red hostile only on schema/registry; submit/rules/export slices go red-then-green then hostile. D1.5 after Handoff red is present (closes that 161800Z bullet). Mocks are now named for B/C/D/E/F/G.

#### A3. Dual pre-implementation gates P0-A then create then P0-B

Verdict: PASS

Evidence: plan.md section 2 item 2, section 4, section 17 steps 1-6. Implementation forbidden before P0-B. todo_get PLAN-PLUGINHANDOFF-001: TODO 'PLAN-PLUGINHANDOFF-001' not found.

#### A4. New FR/TR/TEST families 1:1; drain amends TEST-MCP-195; WIKIEXPORT relink

Verdict: PASS

Evidence: plan.md A2 six FR plus TR-MCP-HOSTILEREVIEW-001..006 and TEST-MCP-HOSTILEREVIEW-001..006. A3 five HYGIENE FR/TR/TEST. A4 WIKIEXPORT 003-005. A5: "Do not create TEST-MCP-REPL-041." Grep TEST-MCP-REPL-041 in plan: no hits (only in the old receipt). Decision 15 and A4 last paragraph instruct relink of MCP-WIKIEXPORT-001. Live todo_get still has FunctionalRequirements FR-MCP-WIKIEXPORT-001,002 as current store state.

Related C defect (TR-MCP-REPL-012) is scored under A7/C, not as a 1:1 family-count miss.

#### A5. Named tests for every remaining AC

Verdict: FAIL

Evidence of names that do exist: HOSTILEREVIEW request-quality, execution-capture, surface parity, oversized payload; hygiene rule methods; wiki dump/import methods; five Coordinator_Open/Begin/Update/AppendDialog/AppendActions tests; Invoke-ReplFailsafeDrainOnFirstSuccess_WhileReplRawInFlight_DoesNotRun; HandoffDraft_InvalidId/Title/Section/Priority/Estimate/ImplementationTasks/Dependencies/RequirementLinks; PluginHandoffSkill_InvokeIngestGetApprove_UsesDocumentedWorkflowMethods.

Missing:

- PLUGININT P19/P20: no method names. TEST-MCP-PLUGININT-001 AC5 (wiki github Testing-Requirements.md 1572) requires focused target AND each plugin native suite Failed 0 Skipped 0. P18 names NukeTarget_SkipIsFailure only.
- TEST-HANDOFF-001..007 AC are all still `[ ]` in docs/Project/wiki/github/Testing-Requirements.md 43-99. Plan D1 says map them after D1. D0 reuse list does not include IngestAsync_DraftOnly_DoesNotCreateTodo or IngestAsync_CreateWhenConfident_CreatesExactlyOneTodo (HandoffIngestionServiceTests.cs 148, 187) even though those tests exist.

#### A6. Existing green tests classified as reuse, not new reds

Verdict: PASS

Evidence: plan.md B1 reuse vs new red. Grep confirmed: parses dictionary-backed dialogItems YAML (PluginPowerShellRuntime.Tests.ps1 2984); rejects appendDialog when no dialog items can be parsed (3077); PersistAsync_PrimaryFails_ReturnsFailsafeResult / CallerCancels / BothStrategiesFail / FilesystemPersistAsync_WritesReplayableV4ScopedEnvelope (SessionLogPersistenceStrategyTests.cs); HandoffDeadContractInventoryTests including SourceAndSchema_DoNotReintroduceReplayOfRunIdOrApproved; HandoffBoundedSourceTests oversized path/artifact; HandoffContainedFileReaderTests.ReadBoundedAsync_GrowingStream_StopsAtLimit; HandoffDurabilityTests lease/heal/mode/sanitization methods; HandoffSkillDelegationTests two facts; blank description/technicalDetails; migration down-up. New Coordinator_* names have zero on-disk matches (not relabeled).

#### A7. Decision-complete locks listed in the claim

Verdict: FAIL

Locked and verified:

- REPL_FAILSAFE_DRAIN_TIMEOUT default 120 or REPL_TIMEOUT if greater (decision 7). On-disk still `return 2` at repl-invoke.ps1 601-603.
- V4 path `{workspace}/.mcpServer/failsafe/{agent}/workspaces/{key}/pending/` matches SessionLogPersistenceStrategyTests.cs 149-153. Get-McpFailsafeDir currently Join-Path cache 'failsafe' (resolve-cache-dir.ps1 145).
- Wiki dump tables[]: pwsh regex compare McpDbContext.cs vs plan lock line: DbSetCount 62, PlanSetCount 62, MissingFromPlan empty, ExtraInPlan empty.
- SHA-256; `--include-dump` and `--dump`; hygiene Director exit 1 on Error/Critical; 1 MiB; PLUGININT eight catalog vs marker five (Codex, Claude, Copilot, Cline, Grok in AGENTS-README-FIRST.yaml 74-190); HANDOFFREVIEW requires both Codex APPROVED and hostile AGREE; todo.yaml deprecation breaking.

Not locked / locked wrong:

- PLAN TODO "entire plan" copy is sections 1-18 (section 4 P0-B, section 14). Section 19 exists.
- add-workspace `--dump` bound to FederationClient.RegisterWorkspaceAsync (plan decision 20, section 11 G0). Live add-workspace is WorkspaceController.CreateAsync POST /mcpserver/workspace (WorkspaceController.cs 117-136) and WorkspaceClient.CreateAsync (WorkspaceClient.cs 37-39). Federation RegisterWorkspaceAsync posts mcpserver/federation/proxies/{proxyId}/workspaces (FederationClient.cs 68-75). MCP-WIKIEXPORT-001 remaining is hydrate a repository on a new MCP Server instance, not hub/proxy federation.
- TR-MCP-REPL-012 still: sessionlog methods keep REPL_TIMEOUT default 30s; TEST-MCP-REPL-027 asserts 30 for completeTurn/beginTurn. Plan A5 does not amend that TR.

#### A8. No skipped-test placeholders; no done:true without hostile AGREE

Verdict: PASS

Evidence: plan.md section 2 items 4 and 10, section 7 skip=fail, section 13/18 task 31. Phase I keeps PLAN open if HYGIENE owner-keep-open remains.

#### A9. Blast radius, three-provider migrations, Nuke UpdateService, SyncAgentPlugins, pwsh-only, no Python

Verdict: PASS

Evidence: plan.md section 2 item 9, section 16, section 15 deploy line. Blast radius lists plugins/core, Repl.Core, Support.Mcp, Storage, Client, Services, tests, docs.

#### A10. Revised plan closes 161800Z FAIL list and AGREE-required items

Verdict: FAIL

161800Z explicit FAIL list vs this plan:

1. PLUGININT P1-P20 not inlined: CLOSED (Phase C; live todo_get matched).
2. No D1.5; PLUGININT not decomposed; mocks only B: PARTIAL. D1.5 present. P1-P20 red/green labeled. Mocks named for C-G. After-red-before-green still missing for C/E/F/G groups.
3. Phase B relabels existing greens: CLOSED (A6).
4. HOSTILEREVIEW 6 vs 5 TR/TEST; HYGIENE 5 vs 4: CLOSED (A4).
5. TEST-MCP-REPL-041 instead of TEST-MCP-195: CLOSED (A4).
6. Named tests missing: PARTIAL. Many named. P19/P20 and TEST-HANDOFF map remain (A5).
7. Decision-incomplete list: PARTIAL. Drain env, V4 path, wiki tables, hygiene exit, eight-vs-five, Codex+hostile, relink, 1 MiB, SHA-256, Pester command, todo.yaml breaking: closed. add-workspace surface, copy range 1-18 vs 19, TR-MCP-REPL-012: open.

161800Z "what would be required for a later AGREE" is therefore not satisfied.

### B Workspace rules

#### B1. Byrd v4 for this class-1 plan-completeness gate

Verdict: FAIL

Same evidence as A2. Scored here because this is an inter-phase / plan-completeness gate, not post-hoc FR-vs-file timestamps.

#### B2. Receipts / honesty of plan current-state

Verdict: PASS

Section 3 claims matched live todo_get and on-disk grep: parser tests exist; no PluginIntegration project in sln; Handoff code exists with isSatisfied false and task 11 open; HOSTILEREVIEW/HYGIENE have no FR family; WIKIEXPORT still linked to completed 001/002.

#### B3. MCP-only storage instruction

Verdict: PASS

Plan forbids YAML TODO create and direct TODO.yaml writes. Requirements via MCP store. Markdown is projection.

#### B4. PowerShell / no Python

Verdict: PASS

Section 2 item 9. This review used pwsh.exe and MCP tools only. No python invocation.

### C Requirements (plan capture, not product-done)

Verdict: FAIL

Plan captures proposed HOSTILEREVIEW/HYGIENE/WIKIEXPORT 003-005 families with 1:1 TR/TEST IDs and many AC bullets. Failures:

- TEST-HANDOFF-001..007 AC are not mapped to method names in this P0-A text.
- FR-MCP-HYGIENE-003 and FR-MCP-HYGIENE-004 lack numbered AC1-ACn (prose lists plus named tests).
- Drain timeout does not reconcile TR-MCP-REPL-012 / TEST-MCP-REPL-027.
- Wiki import `--dump` is captured against the wrong public surface (federation proxy register vs workspace create).

Wiki dump does not reuse completed WIKIEXPORT-001/002 as dump AC: that 161800Z item is closed.

### D Current plan holistically (this plan's DoD)

Verdict: FAIL

P0-A DoD in plan sections 2, 4, 14, 17, 19: every TODO has a phase (PASS), named tests for remaining AC (FAIL), BDPv4 hostile between phases (FAIL), decision-complete (FAIL), no implementation before P0-B (PASS), wiki 003-005 not 001/002 as dump AC (PASS), HANDOFFPLAN not a second product (PASS), P0-A AGREE required before PLAN-PLUGINHANDOFF-001 create.

A P0-A AGREE is not available.

## Accuracy and completeness

Accuracy: 90. Live todo_get, on-disk greps, DbSet 62/62 compare, WorkspaceClient.CreateAsync, drain return 2, and sessionlog_query proof were re-run.
Completeness: 88. Did not execute product test suites (review-only). Did not dump the full requirements_list store (existing family capture used live TODO links plus docs/Project projections).

## What would be required for a later AGREE

- Put a hostile gate after red tests and before implement for every C/E/F/G slice group, not only after mixed red+green.
- Name tests for PLUGININT P19/P20 native-suite/deploy AC, or split AC5 so native suites are an explicit named gate test.
- Map every TEST-HANDOFF-001..007 AC to a reuse method or a new red name in the plan text (include existing DraftOnly/CreateWhenConfident tests in D0).
- Change PLAN TODO copy scope to the entire file including section 19.
- Bind add-workspace `--dump` to WorkspaceClient.CreateAsync / POST /mcpserver/workspace (or a Director/REPL wrapper of that), not FederationClient.RegisterWorkspaceAsync, unless G0 produces evidence that federation is the intended hydration path.
- Amend or explicitly except TR-MCP-REPL-012 / TEST-MCP-REPL-027 for drain SubmitAsync 120s.
- Keep P0-A / P0-B. Do not create PLAN-PLUGINHANDOFF-001 until a later P0-A AGREE.

## Session identifiers

- SessionId: GrokSubagentHostile-20260821T170652Z-p0a-plan
- RequestId: req-20260821T170652Z-001-p0a-pluginhandoff-plan
- TurnId: 42676
- Receipt: docs/receipts/hostile-validator-20260821T171357Z.md
- Twin: docs/receipts/hostile-validator-20260821T171357Z.json
