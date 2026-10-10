# Hostile Validator Receipt

TimestampUtc: 2026-08-21T21:38:54Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation Phase A requirements gate PLAN-PLUGINHANDOFF-001 A6). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (plugin.json / .version 1.97.0; marker agent_plugins.Grok plugin_version is not the source of truth)
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md
TODO: PLAN-PLUGINHANDOFF-001
PriorReceipt: docs/receipts/hostile-validator-20260821T212702Z.md (DISAGREE)
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass re-ran live GET /mcpserver/requirements/{fr|tr|test|mapping}/{id}, live todo_get, REST GET /mcpserver/todo/{id}, health nonce, marker signature, git porcelain, and repl-invoke.ps1 drain timeout. Implementer chat was not trusted.

## Clock and live MCP

TimestampUtc is collector stamp 20260821T213854Z. Health GET /health?nonce=fecdc27482a544d8b7c79c421cdbbb72 returned HTTP 200, status Healthy, storage reachable, nonce echoed exactly. Test-MarkerSignature on AGENTS-README-FIRST.yaml returned True. Live todo_get PLAN-PLUGINHANDOFF-001 Id present Done=false. Collector: docs/receipts/_hv-a6-20260821T213854Z/.

## Session log proof

Persisted. SessionId GrokSubagentHostile-20260821T213854Z-a6-reqs. Turn requestId req-20260821T213854Z-001-phase-a-a6-reqs-gate. sessionlog_open created=true. sessionlog_begin_turn success turnId 42765. sessionlog_complete_turn status completed. sessionlog_query agent=GrokSubagentHostile todoId=PLAN-PLUGINHANDOFF-001 from=2026-08-21T21:00:00Z totalCount=2; this session turn status completed, 6 actions, 2 designDecisions, 4 processingDialog items.

## Mandatory surface that could not be evaluated

None. Live GET by id covered the claimed families plus A1 existing IDs, mappings, child TODOs, drain code, and git porcelain.

## Explicit FAIL list

None.

## Residual (not FAIL, not a reason to DISAGREE)

- MCP-HOSTILEREVIEW-001 Remaining still contains `OrphanReason: no FR/TR family` (HEAD 2026-08-20T101500Z). FunctionalRequirements and TechnicalRequirements arrays are 6 and 6. Plan A2 required link onto the arrays. Remaining is stale operator text, not an unlink.
- MCP-WORKSPACEHYGIENE-002 Remaining still contains `OrphanReason: no FR/TR family`. Arrays are 5 and 5.
- New TEST records have one structured AC row that names the plan tests rather than one AC per FR bullet. Condition strings carry the plan-named tests. A6 requires structured AC and named tests; both are present.

Do not treat residual Remaining as permission to skip Phase B. Do not mark PLAN task 8 or any child `done: true` without citing this receipt path.

## Explicit PASS

- A1/C: New HOSTILEREVIEW 001-006, HYGIENE 001-005, WIKIEXPORT 003-005 FR and TR records exist with 1:1 mappings. Placeholder string absent from GET bodies. FR titles are plan titles (example FR-MCP-HOSTILEREVIEW-001 title `Enqueue bounded hostile review request`). TR titles and bodies are schema/storage, not FR paste (example TR-MCP-HOSTILEREVIEW-001: Persist ReviewRequest and QueueItem in EF with three-provider migrations). TEST conditions name the plan tests.
- A1/B2/C: FR-MCP-172 GET acceptanceCriteria length 4 (AC1-AC4). Body markdown AC matches the array.
- C/D: MCP-HOSTILEREVIEW-001 FunctionalRequirements 6, TechnicalRequirements 6.
- C/D: MCP-WORKSPACEHYGIENE-002 FunctionalRequirements 5, TechnicalRequirements 5.
- A5: MCP-PLUGINCORE-004 FunctionalRequirements include FR-MCP-172. TechnicalRequirements include TR-MCP-PERSIST-003 and TR-MCP-REPL-012. BUG-TRIAGE-170 FunctionalRequirements include FR-MCP-172. TechnicalRequirements include TR-MCP-PERSIST-003 and TR-MCP-REPL-012. PLAN-PLUGINHANDOFF-001 includes FR-MCP-172, TR-MCP-PERSIST-003, TR-MCP-REPL-012.
- A3: TEST-MCP-195 acceptanceCriteria length 7 including AC5-7. TR-MCP-REPL-012 AC3 drain SubmitAsync exception present. TR-MCP-PERSIST-003 AC3 never hardcoded 2.
- A5: Drain still `return 2` at plugins/core/lib-ps/repl-invoke.ps1:602 inside ReplFailsafeDraining SubmitAsync. TEST-MCP-REPL-041 GET is 404. git status has no src/tests/plugins product diffs.
- A6: PLAN-PLUGINHANDOFF-001 Done=false. ImplementationTasks 3-8 (A1-A6) still Done=false. Only P0-A and P0-B tasks are true.

## Claims reviewed

### A Requested

#### A1. FR/TR bodies are no longer Placeholder; new families 1:1 with structured AC

Verdict: PASS

Evidence: GET 200 for FR/TR/TEST MCP-HOSTILEREVIEW-001..006, MCP-HYGIENE-001..005, MCP-WIKIEXPORT-003..005. summary.tsv Placeholder=False TitleEqualsId=False on those FR/TR. Grep of collector JSON for `Placeholder requirement backfilled` returned no matches. Mapping GET 200 with matching TR/TEST ids (summary.tsv mapping rows). FR AC counts HOSTILEREVIEW 4/3/3/3/3/3, HYGIENE 3/5/6/6/5, WIKIEXPORT 3/3/3. tr-vs-fr.json AcTextsIdentical=false. TEST conditions list plan-named tests (example TEST-MCP-HOSTILEREVIEW-001 condition HostileReviewEntity_RoundTrip_SqlitePgSqlServer; HostileReviewSubmit_ValidRequest_CreatesQueueItem; HostileReviewSubmit_OversizedPayload_Rejected; HostileReviewSubmit_ForeignWorkspace_403).

#### A2. MCP-WIKIEXPORT-001 FunctionalRequirements are 003,004,005 not completed 001,002

Verdict: PASS

Evidence: live todo_get and REST GET todo-MCP-WIKIEXPORT-001.json FunctionalRequirements FR-MCP-WIKIEXPORT-003,004,005. TechnicalRequirements TR-MCP-WIKIEXPORT-003,004,005. GET FR-MCP-WIKIEXPORT-001/002 still exist as completed wiki-export work and are not on that TODO array.

#### A3. TEST-MCP-195 AC1-AC7; TR-MCP-REPL-012 AC3; TR-MCP-PERSIST-003 AC3

Verdict: PASS

Evidence: GET TEST-MCP-195 acceptanceCriteria length 7. AC5 never 2 / REPL_FAILSAFE_DRAIN_TIMEOUT default 120. AC6 replay and yaml removed. AC7 nested drain ReplRawInFlight deferred. GET TR-MCP-REPL-012 AC3 SubmitAsync while ReplFailsafeDraining uses REPL_FAILSAFE_DRAIN_TIMEOUT default 120 or REPL_TIMEOUT when greater; does not raise completeTurn/beginTurn above 30s. GET TR-MCP-PERSIST-003 AC3 never hardcoded 2.

#### A4. Previously empty AC now non-empty on named PLUGINCORE/REPL records

Verdict: PASS

Evidence: GET AC counts TR-MCP-PLUGINCORE-005=2, TEST-MCP-PLUGINCORE-005=3, TR-MCP-REPL-011=2, TR-MCP-REPL-012=3, TR-MCP-REPL-013=1, TEST-MCP-REPL-026=2, TEST-MCP-REPL-027=3, TEST-MCP-REPL-028=1. FR-MCP-PLUGINCORE-004 AC count 3, FR-MCP-REPL-009 AC count 4, FR-MCP-PLUGININT-001 AC count 5.

#### A5. MCP-PLUGINCORE-004 and BUG-TRIAGE-170 include drain IDs; no product implementation; drain still return 2

Verdict: PASS

Evidence: todo-link-summary.tsv MCP-PLUGINCORE-004 FR includes FR-MCP-172; TR includes TR-MCP-PERSIST-003,TR-MCP-REPL-012. BUG-TRIAGE-170 FR includes FR-MCP-172; TR includes TR-MCP-PERSIST-003,TR-MCP-REPL-012. git status --short has no src/, tests/, or plugins/ product edits (worktrees 0-line, untracked docs/plans and docs/receipts). git diff --stat is two worktrees with 0 line changes. Select-String repl-invoke.ps1:602 `return 2` inside ReplFailsafeDraining SubmitAsync. Repo grep for class HostileReview / WorkspaceValidation / WikiDump in cs/ps1 hits only collector scripts. TEST-MCP-REPL-041 HTTP 404.

#### A6. PLAN-PLUGINHANDOFF-001 done=false; A6 DoD now met

Verdict: PASS

Evidence: live todo_get Done=false DoneSummary=null. ImplementationTasks 1-2 Done=true (P0-A, P0-B). Tasks 3-8 A1-A6 Done=false. FrCount=25 includes HOSTILEREVIEW 001-006, HYGIENE 001-005, WIKIEXPORT 003-005, FR-MCP-172. TrCount=30 includes HOSTILEREVIEW/HYGIENE/WIKIEXPORT families plus TR-MCP-PERSIST-003 and TR-MCP-REPL-012. Every new FR has TR and TEST with structured AC and 1:1 mapping. No product files this phase.

### B Workspace rules

#### B1. Byrd v4 at the Phase A gate (requirements exist and are adequate)

Verdict: PASS

Evidence: This is the A6 inter-phase gate. Live store now has non-placeholder FR/TR bodies, TR schema/storage distinct from FR, TEST named tests, hydrated FR-MCP-172 AC, mappings, and child TODO arrays. Not scored by FR createdAt vs file mtime.

#### B2. Always bring the receipts

Verdict: PASS

Evidence: this review re-ran GET, todo_get, health nonce, marker signature, git, and grep. Evidence dir docs/receipts/_hv-a6-20260821T213854Z/.

#### B3. MCP-only storage

Verdict: PASS

Evidence: todo_get MCP tool and GET /mcpserver/todo/{id} plus GET /mcpserver/requirements/{type}/{id}. Requirements get-by-id used authenticated REST only because native MCP tools expose list/create/update, not get-by-id. No TODO.yaml or requirements markdown store writes by this review.

#### B4. PowerShell only / no Python

Verdict: PASS

Evidence: collectors are pwsh.exe -NoProfile -NonInteractive -File. No python/python3/py invoked.

#### B5. Honesty

Verdict: PASS

Evidence: Implementer claims match live GET and todo_get: placeholder gone, FR-MCP-172 AC count 4, HOSTILEREVIEW 6/6, HYGIENE 5/5, PLUGINCORE-004 and BUG-TRIAGE-170 carry drain IDs, TEST-MCP-195 AC1-7 and TR-MCP-REPL-012 AC3 present, drain still return 2, PLAN Done=false.

### C Requirements

#### C1. New family AC appropriate and TR distinct from FR

Verdict: PASS

Evidence: FR AC bullets remain testable and match plan A2-A4. TR bodies add EF/migrations/DTOs/enums/indexes/adapters, not FR paste. tr-vs-fr.json AcTextsIdentical=false for all 14 families. TEST conditions name the plan tests (HostileReviewEntity_RoundTrip_SqlitePgSqlServer and siblings; WorkspaceValidation_*; WikiExport_*; TodoYaml_*).

#### C2. A1 existing family hydration

Verdict: PASS

Evidence: FR-MCP-172 GET acceptanceCriteria length 4. FR-MCP-PLUGINCORE-004 AC count 3, FR-MCP-REPL-009 AC count 4, FR-MCP-PLUGININT-001 AC count 5.

#### C3. Child TODO requirement arrays

Verdict: PASS

Evidence: MCP-HOSTILEREVIEW-001 FunctionalRequirements FR-MCP-HOSTILEREVIEW-001..006 and matching TR. MCP-WORKSPACEHYGIENE-002 FunctionalRequirements FR-MCP-HYGIENE-001..005 and matching TR. MCP-WIKIEXPORT-001 remains 003-005. Residual stale Remaining text recorded above; arrays are the A2/A3 link surface.

### D Current plan holistically

#### D1. Phase A A6 DoD

Verdict: PASS

Evidence: Plan section 5 A6: every new FR has TR and TEST; every TEST has structured AC; mappings complete; no product files except requirements store / optional wiki export. Live store matches. PLAN task 8 A6 remains Done=false, which is correct until this AGREE is cited. Phase B red tests may start. Do not mark PLAN or children done without a later green-gate AGREE.

## Ratings

Accuracy: 96. Live GET, todo_get, health nonce, signature, and file grep match the verdicts above. Residual risk is stale Remaining text on two child TODOs, which does not unlink the families.

Completeness: 97. All six parent claims plus B/C/D and the five prior FAIL closeouts were scored. Named-test regex on the collector missed some HYGIENE/WIKIEXPORT names; raw TEST JSON conditions were read and contain the plan names.

## What this AGREE does and does not allow

1. Phase A requirements gate is closed. Phase B red tests that encode new behavior may start.
2. Do not flip PLAN-PLUGINHANDOFF-001 or any child TODO to done:true on this receipt.
3. Drain `return 2` remains in-scope for Phase B. TEST-MCP-REPL-041 must stay absent.
4. Optional later cleanup: rewrite HOSTILEREVIEW-001 and HYGIENE-002 Remaining so they do not still say no FR/TR family.
