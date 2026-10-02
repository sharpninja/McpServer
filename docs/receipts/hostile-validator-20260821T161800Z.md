# Hostile Validator Receipt

TimestampUtc: 2026-08-21T16:18:00Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN). Surface C applies to whether the plan captures FR/TR/TEST/AC. Surface D applies to this plan's own DoD. Surface A is the nine implementer claims. Surface B is workspace rules for the plan artifact.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.97.0)
Active plan: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a02413-e4ee-7513-bd22-9eddd82adb73\plan.md
OverallVerdict: DISAGREE

Default was FAIL or UNKNOWN until this pass re-read the plan, BDPv4, requirements projections, on-disk tests, and 2026-08-21 TODO-id inventory. Implementer chat was not trusted.

## Clock and live MCP note

This Grok Build subagent hosted tool list did not include invoke_expression, todo_get, or sessionlog_*. Live `todo_get` of the eight bodies was not re-run in this process. TODO identity evidence is from `docs/receipts/_hv-20260821T113500Z/extract-todo-mem.json` (36 open items; all eight IDs present) plus `docs/receipts/todo-audit-20260820T101500Z/s0-inventory.json` and `s3-matrix.json`. P1-P20 PLUGININT task text is UNKNOWN (not inlined in the plan; live body not retrieved). TimestampUtc is the receipt id for this review, not a Get-Date receipt.

## Session log proof

Not persisted. Native sessionlog_* / workflow.sessionlog.* were not callable from this hosted tool list. Reviewer session-log obligation is UNKNOWN/incomplete. Verdict is already DISAGREE on plan content.

## Mandatory surface that could not be evaluated

- Live `todo_get` payloads for MCP-PLUGININT-001 P1-P20 task list.
- Reviewer MCP session-log turn persistence.

## Explicit FAIL list

- A1/D: PLUGININT is given Phase C but P1-P20 are not inlined. The plan says execute them from the TODO. That is a pointer, not a BDPv4 playlist. Gate P0-B forbids a stub TODO that says see plan.md; the inverse (plan says see TODO for the slices) still leaves PLAN-PLUGINHANDOFF-001 incomplete.
- A2/B/D: BDPv4 inter-phase hostile is missing after Handoff red tests (D1 goes to D2 implement with no D1.5 hostile). PLUGININT is not decomposed into red-mock-implement slices. Mocks-first is named only for Phase B.
- A2/B5: Phase B lists new red test names for parser and REPL persistence while canonical parser tests and SessionLogPersistenceStrategyTests already exist and, for persistence, already pass the coordinator/failsafe/V4/cancel/dual-fail shapes.
- A4/C: HOSTILEREVIEW proposes 6 FRs and only 5 TRs/TESTs. HYGIENE proposes 5 FRs and only 4 TRs/TESTs. Phase A6 says every new FR has TR and TEST; A2/A3 do not assign them.
- A4/C: Drain timeout is already specified on TR-MCP-PERSIST-003 and TEST-MCP-195 (abort timeout/503, no drainAttempts increment, getFr not blocked 30s). Plan A5 adds TEST-MCP-REPL-041 "or next free TEST id" instead of amending that family.
- A7/C/D: AC lack named tests for PLUGININT, HYGIENE, WIKIEXPORT dump/import, HOSTILEREVIEW request-quality/execution-capture/surface parity, FR-MCP-REPL-009 open/begin/update/append methods, and nested ReplRawInFlight deferral.
- A9/D: Decision-incomplete: drain timeout env not locked (REPL_TIMEOUT vs REPL_FAILSAFE_DRAIN_TIMEOUT); failsafe path `{workspace}/.mcpServer/{agent}/failsafe` conflicts with existing test path `.mcpServer/failsafe/{agent}/workspaces`; wiki `tables[]` and add-workspace parameter names deferred to inspect-do-not-guess; hygiene Director exit severity not locked; PLUGININT eight plugins vs marker five agents; HANDOFFREVIEW remaining still names independent Codex APPROVED while D5 substitutes hostile AGREE; MCP-WIKIEXPORT-001 is not instructed to relink from completed 001/002 to 003-005.

## Explicit PASS (do not treat as AGREE)

- All eight TODOs have a phase letter (B, C, D, D, D, E, F, G). The "lacks a phase" FAIL trigger does not fire.
- Dual pre-implementation gates P0-A then create PLAN-PLUGINHANDOFF-001 then P0-B are written. Implementation is not allowed before P0-B.
- Wiki dump AC are proposed as FR-MCP-WIKIEXPORT-003..005. Completed 001/002 are not used as dump AC.
- HANDOFFPLAN is labeled an execution prompt, not a second product.
- Drain 2s hardcoded `return 2` in plugins/core/lib-ps/repl-invoke.ps1:601-603 is in-scope for Phase B.
- No skipped-test placeholders. Skip equals fail for the PLUGININT Nuke gate. done:true requires hostile AGREE and receipt path.
- Nuke UpdateService, SyncAgentPlugins, three-provider migrations (when schema changes), pwsh-only, no Python are named.

## Claims reviewed

### A Requested

#### A1. The plan covers all eight TODOs

Verdict: FAIL

Evidence:

Phase map in plan.md sections 6-11:

- MCP-PLUGINCORE-004: Phase B
- MCP-PLUGININT-001: Phase C
- MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, MCP-HANDOFFREVIEW-001: Phase D
- MCP-HOSTILEREVIEW-001: Phase E
- MCP-WORKSPACEHYGIENE-002: Phase F
- MCP-WIKIEXPORT-001: Phase G

2026-08-21 extract-todo-mem.json lists all eight IDs among 36 open TODOs.

FAIL is not missing phases. FAIL is PLUGININT coverage: section 7 says "Execute P1-P20 from the TODO as BDPv4 slices" and names artifacts (project, scenarios json, fixture, adapters, Theory, Nuke target) but does not name P1-P20 tasks or per-AC tests. 2026-08-20 s0-inventory: MCP-PLUGININT-001 taskCount 21, tasksDone 1, remaining P1-P20 open. A plan that cannot be executed without another TODO's unpublished task list does not cover that TODO.

#### A2. BDPv4: requirements first, red tests, mocks, implement, Failed 0 Skipped 0, hostile between phases

Verdict: FAIL

Evidence:

Plan sections 2, 4, 5, 17 state the order and inter-phase hostile. Phase B has B2 hostile after red and B5 after green, plus mocks for ISessionLogPersistenceStrategy.

Defects:

- Phase D: D1 red tests, D2 implement, no hostile after red.
- Phase C: one hostile after catalog+project red, then fixture green and eight rows green. P1-P20 are not each a red-mock-implement-hostile slice.
- Mocks-first is not specified for E/F/G/D.
- Phase B1 "write first; show red" includes parser and REPL persistence names that already have on-disk tests: PluginPowerShellRuntime.Tests.ps1 Describe TEST-MCP-PLUGINCORE-004 It 'parses dictionary-backed dialogItems YAML and property-backed JSON'; SessionLogPersistenceStrategyTests PersistAsync_PrimaryFails_ReturnsFailsafeResult, PersistAsync_CallerCancels_DoesNotInvokeFailsafe, PersistAsync_BothStrategiesFail_ThrowsPersistenceException, FilesystemPersistAsync_WritesReplayableV4ScopedEnvelope. Showing those red requires mutating or ignoring existing green tests.

#### A3. Dual pre-implementation gates

Verdict: PASS

Evidence: plan.md section 2 item 2, section 4 Gate P0-A and P0-B, section 17 steps 1-6. P0-B AGREE is required before Phase A product tests or implementation. A summary TODO with "see plan.md" is a FAIL. This planning turn says no implementation.

#### A4. New FR/TR/TEST families; existing families reused not duplicated

Verdict: FAIL

Evidence:

New IDs are proposed: FR-MCP-HOSTILEREVIEW-001..006, TR/TEST 001..005; FR-MCP-HYGIENE-001..005, TR/TEST 001..004; FR-MCP-WIKIEXPORT-003..005 with TR/TEST 003..005. docs/Project has no HOSTILEREVIEW or HYGIENE or WIKIEXPORT-003 rows (grep empty). WIKIEXPORT-001/002 exist and are completed in Functional-Requirements.md 2187-2209.

Reuse of PLUGINCORE/REPL/PLUGININT/HANDOFF IDs in A1 is stated.

Defects:

- 6 HOSTILEREVIEW FRs vs 5 TRs and 5 TESTs. FR-MCP-HOSTILEREVIEW-006 (no auto-mutation) has no dedicated TR/TEST.
- 5 HYGIENE FRs vs 4 TRs and 4 TESTs.
- Drain timeout already lives on TR-MCP-PERSIST-003 / TEST-MCP-195 / FR-MCP-172, not only FR-MCP-REPL-011. A5 invents TEST-MCP-REPL-041 "or next free" instead of amending the persist family.
- MCP-WIKIEXPORT-001 s3-matrix still links FR-MCP-WIKIEXPORT-001/002. Plan never says to replace those links with 003-005.

#### A5. Drain 2s timeout is in-scope

Verdict: PASS

Evidence: plan.md section 2 item 7; A5; B1 named drain tests; B3 remove hardcoded return 2. On-disk: plugins/core/lib-ps/repl-invoke.ps1 Get-ReplMethodTimeoutSeconds lines 601-603 `if ($script:ReplFailsafeDraining -and $Method -eq 'client.SessionLog.SubmitAsync') { return 2 }`.

#### A6. Handoff is remediation plus docs, not greenfield rewrite

Verdict: PASS

Evidence: plan.md section 2 item 6; section 3 current state (code exists, isSatisfied false, task 11 docs/wiki open); section 8 "Do not rewrite the feature." HANDOFFPLAN title in s0-inventory is "Grok execution prompt: Implement MCP-HANDOFF-001".

Caveat (not a second-product FAIL): D5 "independent APPROVED" is ambiguous against MCP-HANDOFFREVIEW-001 remaining "fresh independent Codex review reports APPROVED" (s0-inventory). Substituting Grok hostile AGREE is not locked.

#### A7. Named tests exist for PLUGINCORE/REPL, HOSTILEREVIEW, HYGIENE, WIKIEXPORT, HANDOFF review findings

Verdict: FAIL

Evidence of names that do exist:

- Phase B Pester and SessionLogPersistenceCoordinatorTests / FailsafeStrategyTests method names.
- Phase D1 fifteen method names.
- Phase E seven method names.

Missing method names:

- Phase C PLUGININT: artifacts only. TEST-MCP-PLUGININT-001 has five AC (eight rows, workflow, AiTheory, PLUGIN_ROOT_OVERRIDE, zero skip) with no test method names.
- Phase F: "One unit/property test per rule." No method names.
- Phase G: bullet behaviors, no method names.
- Phase E: no named test for FR-MCP-HOSTILEREVIEW-003 execution capture, request-quality scoring, REPL/Director/plugin parity (slice 7).
- A5 nested drain deferral behind in-flight getFr/getTr: no named test.
- FR-MCP-REPL-009 AC1 lists open, begin, update, appendDialog, appendActions; B1 names completeTurn, PrimaryFailure, BothFail, PrimarySuccess only.

HANDOFF D1 names also fail current-state mapping: existing tests already cover several findings (HandoffDeadContractInventoryTests HandoffReviewState_DoesNotDefineApproved / HandoffIngestionRunEntity_DoesNotExposeReplayOfRunId; HandoffBoundedSourceTests 8 MiB; HandoffDurabilityTests lease takeover / heal vs collision / undefined mode / sanitization). Plan D1 does not cite those names.

#### A8. No skipped-test placeholders. No done:true without hostile AGREE

Verdict: PASS

Evidence: plan.md section 2 items 4, 10; section 7 "skip = fail"; section 13 closeout requires Failed 0 Skipped 0, hostile receipt, OverallVerdict AGREE, doneSummary cites receipts. Phase I keeps PLAN open if HYGIENE owner-keep-open remains.

#### A9. Decision-complete: contracts, three-provider migrations, Nuke deploy, plugin sync, blast radius

Verdict: FAIL

Locked well: HOSTILEREVIEW entity list and tool/REST names; hygiene read-only and 48h clock; wiki dump flag default off; migrations Sqlite/Pg/SqlServer when schema changes; Nuke UpdateService; SyncAgentPlugins; out-of-scope QuadBrain/FILETOOLS/Octopus.

Not locked:

- Drain timeout source: REPL_TIMEOUT or REPL_FAILSAFE_DRAIN_TIMEOUT.
- Failsafe path in B3 `{workspace}/.mcpServer/{agent}/failsafe` vs existing SessionLogPersistenceStrategyTests path `.mcpServer/failsafe/Codex/workspaces` plus pending.
- Wiki dump tables[] membership, hash algorithm, add-workspace parameter name ("inspect, do not guess").
- Hygiene Director "nonzero exit for configured severity" without the severity.
- Hostile review oversized payload numeric bound.
- todo.yaml deprecation breaking-change flag.
- Plugin Pester exact Nuke target deferred to B1.
- PLUGININT catalog eight plugins (Codex, Claude Code, Claude Cowork, Copilot, Grok, Cline, Cline v2, OpenCode) vs marker agent_plugins five (Codex, Claude, Copilot, Cline, Grok).

### B Workspace rules

#### B1. Byrd v4 for this class-1 plan

Verdict: FAIL

Same evidence as A2. Phase-order defects are scored here because this is a plan-completeness gate, not a post-hoc timestamp check.

#### B2. Receipts / honesty of plan current-state

Verdict: FAIL

Plan section 3 claims store receipts from todo_get on 2026-08-21. Parser and REPL persistence remaining work is overstated relative to on-disk tests cited in A2. Handoff "map each P1/P2/P3 finding to a red test" ignores tests already on disk. This is plan honesty, not an implementer done-claim.

#### B3. MCP-only storage instruction

Verdict: PASS

Plan forbids YAML TODO create and direct TODO.yaml writes. Requirements via MCP store. Markdown is projection.

#### B4. PowerShell / no Python

Verdict: PASS

Section 2 item 9: pwsh.exe -NoProfile -NonInteractive; no Python.

### C Requirements (plan capture, not product-done)

Verdict: FAIL

Plan captures many FR/TR/TEST IDs and some AC bullets. It does not capture 1:1 FR-TR-TEST for new HOSTILEREVIEW/HYGIENE families, does not name tests per AC for PLUGININT/HYGIENE/WIKIEXPORT, duplicates persist drain AC under a new TEST id, and does not relink MCP-WIKIEXPORT-001 off completed 001/002.

Wiki dump reuses 001/002 as dump AC: PASS (not used). Missing store relink remains a C FAIL.

HANDOFF FR-HANDOFF-001..007 AC remain unchecked in Testing-Requirements.md (example TEST-HANDOFF-002..006 still `[ ]`). Plan D says satisfy those AC but D1 names review-finding tests, not TEST-HANDOFF-* method mapping. Prior hostile receipt docs/receipts/hostile-validator-20260816T212536Z.md already FAILed FR-HANDOFF-003 field-specific diagnostics and TR-HANDOFF-SURFACE-001 plugin skill invocation. This plan does not name tests that close those two gaps.

### D Current plan holistically (this plan's DoD)

Verdict: FAIL

Plan DoD (sections 2, 4, 14, 17): operator approval, P0-A AGREE, create full-text PLAN-PLUGINHANDOFF-001, P0-B AGREE, then Phase A. This review is P0-A.

DoD also requires: every listed TODO has a phase (PASS), named tests (FAIL), no implementation before P0-B (PASS), wiki 003-005 not 001/002 as dump AC (PASS), HANDOFFPLAN not a second product (PASS), decision-complete (FAIL), BDPv4 hostile between phases (FAIL).

A P0-A AGREE is not available.

## Accuracy and completeness

Accuracy: 88. Plan-text defects and on-disk test/requirement citations were re-read. Live todo_get was not re-run.
Completeness: 72. PLUGININT P1-P20 task list and reviewer session-log persistence were not retrieved from the live store.

## What would be required for a later AGREE

- Inline PLUGININT P1-P20 as BDPv4 slices with named tests per TEST-MCP-PLUGININT-001 AC.
- Name tests for hygiene rules, wiki dump/import AC, remaining HOSTILEREVIEW AC, REPL-009 non-terminal methods, nested drain deferral.
- Map Handoff D1 names to existing tests or remaining gaps only. Name tests for FR-HANDOFF-003 field diagnostics and TR-HANDOFF-SURFACE-001 actual skill invocation.
- Give every new FR a TR and TEST. Relink MCP-WIKIEXPORT-001 to 003-005. Amend TR-MCP-PERSIST-003 / TEST-MCP-195 for the 2s drain bug instead of an unlocked new TEST id.
- Lock drain timeout env, failsafe path vs existing V4 tests, wiki dump schema tables, hygiene exit severity, PLUGININT plugin set vs marker, Codex vs hostile APPROVED.
- Add hostile after Handoff red tests. Keep P0-A/P0-B.
