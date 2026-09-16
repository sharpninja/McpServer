# Hostile Validator Receipt

TimestampUtc: 2026-08-21T21:27:02Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation Phase A requirements gate PLAN-PLUGINHANDOFF-001 A6). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (marker agent_plugins.Grok plugin_version 1.97.0)
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md
TODO: PLAN-PLUGINHANDOFF-001
OverallVerdict: DISAGREE

Default was FAIL or UNKNOWN until this pass re-ran live GET /mcpserver/requirements/{fr|tr|test|mapping}/{id}, live todo_get, health nonce, git porcelain, and repl-invoke.ps1 drain timeout. Implementer chat was not trusted.

## Clock and live MCP

TimestampUtc is Get-Date -AsUTC 2026-08-21T21:27:02Z. Session open/begin used 2026-08-21T21:19:38Z. Health GET /health?nonce=15c3ffbfc3e74c2a8b87da8242ae0601 returned status Healthy, storage reachable, nonce echoed exactly. Live todo_get PLAN-PLUGINHANDOFF-001 Id present Done=false. Collector: docs/receipts/_hv-a6-20260821T211938Z/.

## Session log proof

Persisted. SessionId GrokSubagentHostile-20260821T211938Z-a6-reqs. Turn requestId req-20260821T211938Z-001-phase-a-requirements-gate. sessionlog_open created=true. sessionlog_begin_turn success turnId 42756. sessionlog_complete_turn status completed. sessionlog_query agent=GrokSubagentHostile todoId=PLAN-PLUGINHANDOFF-001 from=2026-08-21T21:00:00Z totalCount=1, turn status completed, 6 actions, 2 designDecisions, 3 processingDialog items.

## Mandatory surface that could not be evaluated

None. Live GET by id covered the claimed families plus A1 existing IDs, mappings, child TODOs, drain code, and git porcelain.

## Explicit FAIL list

- A1/C: New HOSTILEREVIEW 001-006, HYGIENE 001-005, and WIKIEXPORT 003-005 FR and TR records exist with 1:1 mappings and non-empty AC arrays, but every one of those 14 FR + 14 TR bodies is still `Placeholder requirement backfilled for TODO link {id}`. Titles are the IDs, not the plan titles. TR AC is a paste of the FR AC, not schema/storage technical criteria. TEST records exist with generic condition `Tests covering FR-... structured AC` and the same FR AC paste. That is not an adequate 1:1 FR/TR/TEST family.
- A1/B2/C: FR-MCP-172 GET returns acceptanceCriteria []. The body still has markdown AC bullets. Plan A1 required hydrate when the store AC array is empty while markdown has bullets.
- C/D: Plan A2 required linking the new HOSTILEREVIEW family onto MCP-HOSTILEREVIEW-001. Live todo_get FunctionalRequirements and TechnicalRequirements are null. Remaining still says `no FR/TR family`.
- C/D: MCP-WORKSPACEHYGIENE-002 FunctionalRequirements and TechnicalRequirements are still null. Remaining still says `no FR/TR family` even though FR-MCP-HYGIENE-001..005 exist and are linked on PLAN-PLUGINHANDOFF-001.
- C/D: Plan A5 required linking the drain amendment onto MCP-PLUGINCORE-004, PLAN-PLUGINHANDOFF-001, and BUG-TRIAGE-170. PLAN has TR-MCP-PERSIST-003 and TR-MCP-REPL-012. MCP-PLUGINCORE-004 is still only FR-MCP-PLUGINCORE-004 / FR-MCP-REPL-009 and TR-MCP-PLUGINCORE-004 / TR-MCP-REPL-010. BUG-TRIAGE-170 is still FR-MCP-TRIAGE-002 / TR-MCP-TRIAGE-004.

## Explicit PASS (do not treat as AGREE or as Phase A complete)

- A2: MCP-WIKIEXPORT-001 FunctionalRequirements are FR-MCP-WIKIEXPORT-003,004,005. TechnicalRequirements are TR-MCP-WIKIEXPORT-003,004,005. Completed 001/002 are not on that TODO.
- A3: TEST-MCP-195 has AC1-AC7 including drain AC5-7. TR-MCP-REPL-012 has AC3 drain SubmitAsync exception. TR-MCP-PERSIST-003 has AC3 never hardcoded 2.
- A4: TR-MCP-PLUGINCORE-005, TEST-MCP-PLUGINCORE-005, TR-MCP-REPL-011/012/013, TEST-MCP-REPL-026/027/028 all have non-empty structured AC.
- A5: No product implementation this phase. git status has no src/tests/plugins product diffs. Drain still `return 2` at plugins/core/lib-ps/repl-invoke.ps1:602 inside ReplFailsafeDraining SubmitAsync. TEST-MCP-REPL-041 GET is 404.
- A6: PLAN-PLUGINHANDOFF-001 Done=false. ImplementationTasks 3-8 (A1-A6) still Done=false. Only P0-A and P0-B tasks are true.
- B3/B4: Requirements and TODO reads used MCP GET / todo_get. Collector is pwsh.exe -NoProfile. No Python. No direct TODO.yaml or requirements file edits by this review.
- Mappings for the new FRs are 1:1 (one TR id, one TEST id each). Existing A1 families PLUGINCORE-004, REPL-009, PLUGININT-001 have non-empty AC.

## Claims reviewed

### A Requested

#### A1. New families exist 1:1 with structured AC; mappings FR to TR to TEST 1:1

Verdict: FAIL

Evidence: GET 200 for FR/TR/TEST MCP-HOSTILEREVIEW-001..006, MCP-HYGIENE-001..005, MCP-WIKIEXPORT-003..005. Mapping GET 200 with matching TR/TEST ids (summary.tsv rows 67-80). AC counts match the plan bullets (HOSTILEREVIEW 4/3/3/3/3/3, HYGIENE 3/5/6/6/5, WIKIEXPORT 3/3/3). Bodies: 28 FR+TR files contain `Placeholder requirement backfilled for TODO link`. Example GET body FR-MCP-HOSTILEREVIEW-001 and TR-MCP-HOSTILEREVIEW-001. TEST-MCP-HOSTILEREVIEW-001 condition is `Tests covering FR-MCP-HOSTILEREVIEW-001 structured AC`, title `FR-MCP-HOSTILEREVIEW-001 tests`, AC copied from the FR. Plan A2 TR was `schema/storage` plus named tests; store TR is not that.

#### A2. MCP-WIKIEXPORT-001 FunctionalRequirements are 003,004,005 not completed 001,002

Verdict: PASS

Evidence: live todo_get MCP-WIKIEXPORT-001 FunctionalRequirements FR-MCP-WIKIEXPORT-003,004,005. TechnicalRequirements TR-MCP-WIKIEXPORT-003,004,005. GET FR-MCP-WIKIEXPORT-001/002 still exist as completed wiki-export work (status completed, isSatisfied true) and are not on this TODO array.

#### A3. TEST-MCP-195 AC1-AC7; TR-MCP-REPL-012 AC3; TR-MCP-PERSIST-003 AC3

Verdict: PASS

Evidence: GET TEST-MCP-195 acceptanceCriteria length 7. AC5 never 2 / REPL_FAILSAFE_DRAIN_TIMEOUT default 120. AC6 replay and yaml removed. AC7 nested drain ReplRawInFlight deferred. GET TR-MCP-REPL-012 AC3 SubmitAsync while ReplFailsafeDraining uses REPL_FAILSAFE_DRAIN_TIMEOUT default 120 or REPL_TIMEOUT when greater; does not raise completeTurn/beginTurn above 30s. GET TR-MCP-PERSIST-003 AC3 never hardcoded 2.

#### A4. Previously empty AC now non-empty on named PLUGINCORE/REPL records

Verdict: PASS

Evidence: GET AC counts TR-MCP-PLUGINCORE-005=2, TEST-MCP-PLUGINCORE-005=3, TR-MCP-REPL-011=2, TR-MCP-REPL-012=3, TR-MCP-REPL-013=1, TEST-MCP-REPL-026=2, TEST-MCP-REPL-027=3, TEST-MCP-REPL-028=1. Note: TR-MCP-PLUGINCORE-005 AC is the historical PowerShell.MCP cross-volume text-edit TR, not dialog parsing. That is the live TR body. Non-empty is true.

#### A5. No product implementation this phase; drain still return 2 is OK

Verdict: PASS

Evidence: git status --short has no src/, tests/, or plugins/ product edits (only worktrees, plan file, receipts). git diff --stat is the two worktrees with 0 line changes. Select-String repl-invoke.ps1:602 `return 2` inside ReplFailsafeDraining SubmitAsync. No HostileReview / WorkspaceValidation / WikiDump product files under src or tests. TEST-MCP-REPL-041 HTTP 404.

#### A6. PLAN-PLUGINHANDOFF-001 done=false

Verdict: PASS

Evidence: live todo_get Done=false DoneSummary=null. ImplementationTasks 1-2 Done=true (P0-A, P0-B). Tasks 3-8 A1-A6 Done=false. FrCount=25 includes HOSTILEREVIEW 001-006, HYGIENE 001-005, WIKIEXPORT 003-005, FR-MCP-172.

### B Workspace rules

#### B1. Byrd v4 at the Phase A gate (requirements exist and are adequate)

Verdict: FAIL

Evidence: This is the A6 inter-phase gate. Adequate FR/TR/TEST/AC is the exit. Placeholder bodies, empty FR-MCP-172 AC array, and unlinked child TODOs fail that gate. Not scored by FR createdAt vs file mtime.

#### B2. Always bring the receipts

Verdict: PASS

Evidence: this review re-ran GET, todo_get, health nonce, git, and grep. Evidence dir docs/receipts/_hv-a6-20260821T211938Z/.

#### B3. MCP-only storage

Verdict: PASS

Evidence: todo_get and GET /mcpserver/requirements/* only. No TODO.yaml or requirements markdown store writes.

#### B4. PowerShell only / no Python

Verdict: PASS

Evidence: collectors are pwsh.exe -NoProfile -NonInteractive -File. No python/python3/py invoked.

#### B5. Honesty

Verdict: FAIL

Evidence: Claim 1 presented new families as 1:1 structured AC complete. Live FR/TR bodies still say Placeholder requirement backfilled. Child HOSTILEREVIEW/HYGIENE TODOs still advertise no FR/TR family. That is an overclaim of Phase A completeness, even though PLAN A1-A6 tasks were not flipped to Done=true.

### C Requirements

#### C1. New family AC appropriate and TR distinct from FR

Verdict: FAIL

Evidence: AC bullets on FRs are testable and mostly match plan A2-A4. TR records do not add technical schema, storage, migrations, or named tests. TEST records do not name the plan tests (HostileReviewEntity_RoundTrip_SqlitePgSqlServer and siblings). Body field left as auto-backfill placeholder.

#### C2. A1 existing family hydration

Verdict: FAIL

Evidence: FR-MCP-172 GET acceptanceCriteria []. Body markdown still lists four AC bullets. Plan A1: if live store AC array is empty while markdown has bullets, hydrate through requirements update. FR-MCP-PLUGINCORE-004 AC count 3, FR-MCP-REPL-009 AC count 4, FR-MCP-PLUGININT-001 AC count 5: those existing families do have structured AC.

#### C3. Child TODO requirement arrays

Verdict: FAIL

Evidence: MCP-HOSTILEREVIEW-001 and MCP-WORKSPACEHYGIENE-002 FunctionalRequirements null. Plan A2 explicit link onto MCP-HOSTILEREVIEW-001. WIKIEXPORT was relinked (PASS A2) which shows the implementer knew how to update TODO arrays and did not do it for HOSTILEREVIEW/HYGIENE.

### D Current plan holistically

#### D1. Phase A A6 DoD

Verdict: FAIL

Evidence: Plan section 5 A6: every new FR has TR and TEST; every TEST has structured AC; mappings complete; no product files except requirements store / optional wiki export. ID-level that is mostly true. Holistic DoD also includes A1 hydrate, A2 link onto MCP-HOSTILEREVIEW-001, A5 link onto PLUGINCORE-004 and BUG-TRIAGE-170, and requirements that are not placeholders. Those are open. PLAN task 8 A6 remains Done=false, which matches this DISAGREE. Do not start Phase B red tests.

## Ratings

Accuracy: 95. Live GET, todo_get, health nonce, and file grep match the verdicts above. Residual risk is only wording of HYGIENE TODO link (A3 did not use the words `Link onto`; remaining text and PLAN linkage still make the gap real).

Completeness: 94. All six parent claims plus B/C/D were scored. Named-test strings were checked on TEST records and found absent; they are plan-locked for later slices and were not used as a standalone FAIL.

## What would be required for a later AGREE

1. Replace placeholder FR/TR titles and bodies with the plan text (FR titles and TR schema/storage bodies). Keep structured AC. Put named tests on TEST conditions or AC evidence, not only on the plan markdown.
2. Hydrate FR-MCP-172 structured AC from its body bullets (and keep TEST-MCP-195 / TR-MCP-PERSIST-003 as already amended).
3. Set MCP-HOSTILEREVIEW-001 and MCP-WORKSPACEHYGIENE-002 FunctionalRequirements/TechnicalRequirements to the new IDs. Update Remaining so it does not still say no FR/TR family.
4. Link drain IDs onto MCP-PLUGINCORE-004 and BUG-TRIAGE-170 as A5 states.
5. Re-run this A6 gate. Do not mark PLAN task 8 or any child done without a later AGREE receipt path.
