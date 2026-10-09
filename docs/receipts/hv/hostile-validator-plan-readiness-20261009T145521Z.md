# Hostile validator: PLAN-SESSIONTURNREVIEW-001 readiness

TimestampUtc: 2026-10-09T15:11:43.6654009Z
Identity: Codex CLI, model gpt-6-sol, effort xhigh. Live proof: /home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T09-55-22-01a12129-5f29-76c1-bf8e-364b80cb2965.jsonl. Its session_meta ordinal 0 reports codex_exec, thread 01a12129-5f29-76c1-bf8e-364b80cb2965 and HEAD edbba968eecea07e8e96bd250c93065c07f3a2e3; turn_context ordinal 7 reports model gpt-6-sol and effort xhigh.
Reviewer sessionId: Codex-20261009T145732Z-plugin-session
Reviewer requestId: req-20261009T145734Z-plan-readiness-hv
Request JSONL: docs/receipts/hv/20261009T145521Z-plan-readiness-r1.request.jsonl
Response JSONL: docs/receipts/hv/20261009T145521Z-plan-readiness-r1.response.jsonl

First actions completed: read /home/sharpninja/.claude/skills/add-profile/SKILL.md and all 19 non-skill markdown files in /home/sharpninja/.claude/profile/ in full; read /home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml in full; ran workflow.memory.list through the Codex plugin and read all 13 returned memories (11 Global and 2 Workspace). Installed plugin Status was available. A private /tmp plugin cache isolates this reviewer turn.

Scope: plan readiness only. Historical implementer conduct, infrastructure repair, and the Pi3 monitor were excluded. No product code, plan, TODO, requirement, triage, memory, setting, or other session-log turn was changed.

OverallVerdict: DISAGREE. Accuracy: 84/100. Completeness: 68/100. Confidence: 93/100. Confirmed false source facts, incomplete requirement/test contracts, and impossible or incomplete gates prevent approval. Ten claims FAIL; zero PASS; zero UNKNOWN.

## Claims

- P1 Source accuracy: FAIL. Findings F01, F07, F13.
- P2 Requirements accuracy: FAIL. F02, F03, F04.
- P3 Locked operator decisions: FAIL. F05, F06, F12.
- P4 Scope completeness: FAIL. F07.
- P5 Decision completeness: FAIL. F05, F06, F07, F08, F12.
- P6 Executability: FAIL. F02, F09, F10, F13.
- P7 Testability: FAIL. F03, F08, F11, F14.
- P8 Internal consistency: FAIL. F04, F05, F06, F09, F11.
- P9 Artifact parity: FAIL. F15. The TODO description and task list themselves pass parity.
- P10 Risks: FAIL. F16.

## Findings, evidence, and exact fixes

F01. Context line 5; P1. The plan presents f56dcf70 as the worktree HEAD. At review git rev-parse HEAD was edbba968eecea07e8e96bd250c93065c07f3a2e3, while origin/develop remained f56dcf70e7e84f2d286e1ca9e0e6680133f09a82. Label f56dcf70 as a dated baseline and identify the current approval HEAD. Implementer plan edit.

F02. P0 lines 119-127 and Updated existing requirements lines 353-354; P2/P6. FR-SUPPORT-010G is absent from the live 352-FR store and docs and is to be created, but the plan gives it no FR-to-TR/TEST mapping. The 20-FR mapping list is lines 300-321. build/TraceabilityValidator.cs:139-149 counts unmapped FRs as errors; build/Build.ValidateTraceability.cs:69-74 fails on them even without strict flags. Define its creation record, explicit TR/TEST mapping, and scoped gate evidence. Implementer plan edit.

F03. New technical requirements line 275 versus Updated existing requirements line 339 and allocation line 416; P2/P7. TR-MCP-SESSIONTURN-003 defines AC-001 through AC-004 only, then the plan cites AC-005 for title isolation. Add AC-005 to the P0 TR record and its TEST statement and A4 RED test. Implementer plan edit.

F04. New FR heading line 133, Updated existing requirements lines 323 and 353-354, P0 task line 122; P2/P8. Twenty SESSIONTURN/CLASS/REVIEW FRs plus the newly created FR-SUPPORT-010G equal 21 new FRs. The plan and TODO P0.4 call for 20, and call the missing record an existing update. Reclassify it as a creation and change counts to 21 new FRs and 33 existing updates across plan, TODO, and handoff. Implementer plan edit.

F05. Locked decisions lines 27-28, FR-MCP-SESSIONTURN-001 lines 135-140, and wire contract line 438; P3/P5/P8. The dedupe key SHA256(hook session id + normalized prompt) with a 15-second window merges two intentionally identical user requests sent within that window, and treats a retry after the window as another request. This contradicts exactly one turn per request. Specify a stable delivery identity and fallback, then test both intentional repeats and delayed retries. Operator decision D1: host event ID, ingress-generated request token, or an explicit relaxation of exactly-once semantics.

F06. Locked decisions lines 27-28, FR-MCP-SESSIONTURN-007 lines 176-181, wire contract line 438, A4 line 467; P3/P5/P8. RequestId remains optional and a verb may use the shared current-turn pointer. Once a queued request opens a second turn, a write caused by the first can target the second. Per-turn files do not determine attribution. Specify mandatory requestId for concurrent turns or immutable invocation-to-request binding, including legacy behavior and interleaving tests. Operator decision D2: explicit requestId versus host-context binding.

F07. Plugin matrix lines 63-65 and 70-71, P0 line 125; P1/P4/P5. OpenCode exports createMcpServerPlugin, not the claimed createQBrainAiPlugin. The public repository README and master/src/index.ts confirm that. Its event-handler plan says "where the SDK provides them"; Cline V2 host hooks are also to be inventoried in P0. Thus host behavior remains to be designed after approval. Correct the export name and lock each host's exact event payload, skip/open/stop strategy, fallback, and host-owned tests. Implementer plan edit. Primary source: https://github.com/sharpninja/mcpserver-opencode-plugin .

F08. New testing requirements lines 296-298 and P0 create instruction line 122; P5/P7. The 22 TEST records are one generic template, without exact titles, statements, named test classes, or individual FR and TR AC lists. P0 cannot create them "exactly as listed." Enumerate all 22 complete records with per-AC RED test names and mappings. Implementer plan edit.

F09. P0 UC gate line 127 versus phase gates lines 99, 501, 520; P6/P8. The plan correctly scopes P0 because the live store has 352 FRs, zero use cases, and zero linked FRs, but later requires global --strict-use-case-fr-coverage after planning only nine UCs for this workstream. client.UseCases.GetCoverageAsync returned totalUseCases=0, totalFunctionalRequirements=352, linkedFunctionalRequirements=0. build/TraceabilityValidator.cs:158-172 reports every unlinked FR, and build/Build.ValidateTraceability.cs:69-74 fails under the strict flag. Align all phase gates with a feasible policy. Operator decision D3: cover the entire pre-existing FR baseline with UCs or approve a scoped/baseline-aware strict gate.

F10. BDP gate lines 94-100, S4 line 487, S11 line 501; P6. S4 names ConfigValidatorReviewThresholdTests in tests/Build.Tests, but build/Build.Test.cs:22-24 expressly excludes Build.Tests. No S4 G1/G3 command names that project. Add dotnet test tests/Build.Tests/Build.Tests.csproj with its filter and zero-fail/zero-skip evidence to S4 and phase exits. Implementer plan edit.

F11. FR-MCP-SESSIONREVIEW-008 AC004 line 269, allocation line 401, S1 line 481, S9 line 497; P7/P8. S1 must RED-test the shared golden contract, but the contract file docs/context/session-turn-review-contract.json is first introduced by S9. Create and freeze the file in P0 or S1 before S1 RED; S9 must consume it. Implementer plan edit.

F12. Locked review/completion rules lines 34, 38, 42-45 and error contract line 454; P3/P5. Other is a terminal review status accepted by the completion gate, but the plan permits an empty Other record without reviewer, reviewedAt, reasons, or hostile receipt paths. A Code/Docs turn can be completed with no auditable review. Define minimum Other evidence, or explicitly authorize that exception. Operator decision D4: audited Other versus unaudited Other.

F13. P0 per-plugin acquisition line 67; P1/P6. The unconditional instruction to fast-forward origin/main fails for OpenCode. git ls-remote --symref https://github.com/sharpninja/mcpserver-opencode-plugin.git HEAD returned refs/heads/master, and --heads listed no main. Resolve origin/HEAD for each plugin; name OpenCode origin/master. Implementer plan edit. Primary source: https://github.com/sharpninja/mcpserver-opencode-plugin .

F14. Lossless score AC lines 240-243, S6 lines 490-491, wire contract line 453; P7. The proposed "Int64, else exact decimal, else double" normalizer can round forbidden input. Current ReplCommandDispatcher.cs:2549-2550 and GenericClientPassthrough.cs:834-835 use double. A .NET probe of 0.12345678901234567890123456789 returned TryGetDecimal=True but rounded to 0.1234567890123456789012345679; GetDouble returned 0.12345678901234568. Require raw lexeme preservation and score-specific precision rejection before conversion, plus RED tests across JSON and YAML forms. Implementer plan edit.

F15. Plan source Context line 3 points to the handoff; P9. Handoff Status and Local source sections report 421 non-empty lines and 530 total. PowerShell counted 422 non-empty and 531 total. workflow.todo.get description matched all 422 non-empty source lines byte for byte and held 30 implementationTasks. Correct the handoff counts while preserving TODO/source parity. Implementer handoff edit.

F16. Risks and incidental bugs lines 509-513; P10. The "To file" list is stale: the handoff identifies filed reports and workflow.triage.getReport verified the reports for gated commit files (41d7d27c...), revive children (953f44d...), restamp (d8c40d80...), federation fields (ab06ac3...), and filesModified case (04659db...). Source confirms the listed defects at TransactionGatedSessionLogService.cs:441-457, :645-663, :698-725 and TodoExecutionService.cs:1472-1478 versus SessionLogService.cs:1676. The corrected PlanFile/TodoId group triage-group-804b21086f3019ab was not found through this main-workspace plugin route; the handoff says it was filed in the worktree workspace. Replace "To file" with verified IDs, identify that group and workspace, or schedule any truly unfiled item. Implementer plan/handoff edit.

## Verified portions and limits

Live requirements counts were FR 352, TR 480, TEST 512, with no collisions in the proposed SESSIONTURN/SESSIONCLASS/SESSIONREVIEW families. Sampled docs/Project line anchors, terminal guard, controller receipt, plugin supersede, stop auto-close, and hook validator line 72 matched the plan. Four plugins are local with the stated versions and dirty counts; the four absent local repositories exist remotely. The remote test inventories and package scripts support the proposed Pester/bats and npm commands. Named Nuke targets and the three provider migration directories exist. Product builds/tests were not run under the review-only constraint. The corrected triage group could not be confirmed through this store route.

=== VERDICT JSON ===
{"overallVerdict":"DISAGREE","accuracy":84,"completeness":68,"confidence":93,"passCount":0,"failCount":10,"unknownCount":0,"failList":["P1","P2","P3","P4","P5","P6","P7","P8","P9","P10"],"unknownList":[],"implementerFixes":["F01","F02","F03","F04","F07","F08","F10","F11","F13","F14","F15","F16"],"operatorDecisionFails":["F05:D1","F06:D2","F09:D3","F12:D4"],"headSha":"edbba968eecea07e8e96bd250c93065c07f3a2e3","receiptPaths":["docs/receipts/hv/hostile-validator-plan-readiness-20261009T145521Z.md","docs/receipts/hv/hostile-validator-plan-readiness-20261009T145521Z.json","docs/receipts/hv/20261009T145521Z-plan-readiness-r1.request.jsonl","docs/receipts/hv/20261009T145521Z-plan-readiness-r1.response.jsonl"],"reviewerSessionId":"Codex-20261009T145732Z-plugin-session","reviewerRequestId":"req-20261009T145734Z-plan-readiness-hv"}

Persistence proof (2026-10-09T15:15:14.3350395Z): client.SessionLog.QueryAsync through the Codex plugin returned reviewer session Codex-20261009T145732Z-plugin-session and turn req-20261009T145734Z-plan-readiness-hv in_progress with six dialog items and six actions. The four consecutive report dialog contents concatenate exactly to the 11,691-character Markdown verdict. The same proof was appended to that turn before completion.