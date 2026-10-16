# Hostile validator — plan readiness round 21

TimestampUtc: 2026-10-10T00:34:59.851Z
ValidatorIdentity: Codex, gpt-6-sol, xhigh. Live proof: own rollout session_meta id 01a12331-74e8-72f2-a7ea-c8ec8c291d6a and turn_context model/effort at /home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T19-23-26-01a12331-74e8-72f2-a7ea-c8ec8c291d6a.jsonl.
Reviewer sessionId: Codex-20261010T002620Z-plugin-session; requestId: req-20261010T002622Z-plan-r21-hv. HEAD: 4e54986eb5294d6d7b8e22b5d8b8fb154fd5bf2f.

## Intake and scope

Read add-profile SKILL.md and all 19 non-skill profile Markdown files in full; read the 397-line main-workspace marker in full with its apiKey line and Authentication block redacted; listed and read all 15 effective MCP memories, including MEMORY-PROCESS-007 and MEMORY-PROCESS-008. Reviewed the full 649-line plan, all sections, the main-workspace TODO via workflow.todo.get, the 169-line handoff, r20, and r1-r19 receipts. No product, TODO, requirement, triage, memory, or plan changes were made. Historical implementer conduct, infrastructure repair, and Pi3 were not scored.

Plan/TODO: 529/529 non-empty lines equal; 30 tasks; note NOT APPROVED; technicalDetails records the round-21 sync. Handoff locations, status, counts, and triage statements match. Live requirements totals: FR 354, TR 482, TEST 514. The review checks readiness, not green implementation gates.

## Claims P1–P10

- **P1 PASS.** All 649 source lines (529 non-empty) were reviewed. Source baseline f56dcf70 to 4e54986 changed documentation/receipts only; no src, tests, build, or plugins path changed. Checked the five-entry MarkerFileService generator at lines 532-611, FR-SUPPORT-010G's 36 citations in nine files, named routes and source anchors, and nine plugin checkout inventory. No false current-source assertion established.
- **P2 PASS.** Live main-workspace store totals are 354 FR, 482 TR, 514 TEST. The 120 cited canonical IDs were cross-checked against the live store or explicitly identified as P0 creations; 21 new FR, 23 new TR, and 23 new TEST IDs have no collision. Mapping additions and TEST allocations were reviewed against the catalog and docs/Project; the alias lifecycle defect below is a contract/test gap, not an ID collision.
- **P3 PASS.** D1-D12, the partial-clearance and licensing rulings, removal of skipped_duplicate, and C1-C12 acknowledgments are represented. Re-attacked C9 at lines 126, 200, 346, 579, 589; A3/A8 restrict the exception to trusted exact mint adapters and have negative tests. D10 appears in the process and phase gates.
- **P4 PASS.** A1-A9 and S1-S11 cover server, client, REPL, MCP tools, both shared plugin cores, the eight named plugins plus grok-bot, and SQLite/PostgreSQL/SQL Server migrations. D6 unsupported branches, host tests, and G6 suites are allocated. This score concerns surface coverage; F-R21-01 concerns an inconsistent Node contract.
- **P5 FAIL.** F-R21-01. After all registered consumers acknowledge an alias, TR-MCP-SESSIONTURN-008 AC-007 allows it to be pruned, while C2 and FR-MCP-SESSIONTURN-001-AC008 require lookup by either ID. The implementer must choose an alias-retention lifetime.
- **P6 PASS.** Named Nuke targets and test-project paths exist; P0.1 explicitly provisions missing SDK, Pester, bats, dotnet-ef and provider readiness before gates. The common BDPv4 section orders contracts/stubs, RED, mocks-green plus mutant negative check, self-review, real green, refactor, G1-G6, then slice HV. This was a read-only readiness check; no claim is made that current gates ran green.
- **P7 FAIL.** F-R21-01. A8 names partial-ack, all-ack replay-stop, and either-ID lookup tests, but no test performs pending-ID resolution after the last required acknowledgment/prune; TEST-MCP-SESSIONTURN-008 therefore cannot prove the unconditional lookup AC. HV-P0/A/B and every-slice Codex gpt-6-sol xhigh 98/98 gates otherwise align.
- **P8 FAIL.** F-R21-01. C2/FR require durable either-ID resolution, whereas TR-008's pruning removes the alias mapping that resolves the pending ID. The 30 TODO tasks and P0.1-P0.9/A1-A9/S1-S11 dependency order otherwise agree.
- **P9 PASS.** MCP TODO description matches the 529 non-empty plan lines exactly; all 30 implementationTasks match the plan phase list; note remains NOT APPROVED; technicalDetails calls out the 00:22:45Z round-21 sync and four fixes. Handoff is 169 lines and accurately states 649/529 counts, paths, status, phase gates, and filed triage context.
- **P10 PASS.** The nine report IDs in Risks plus the licensing report resolve in the main workspace. The exceptional triage group resolves under the worktree workspacePath with one grouped report and failed group status, as the plan says; main-workspace lookup is not found. Risks are explicitly scheduled or filed. No new unfiled incidental source bug was established.

## FAIL finding

### F-R21-01 — alias lookup after final acknowledgement

Plan location: Shared contracts C2 line 119; FR-MCP-SESSIONTURN-001-AC008 line 200; TR-MCP-SESSIONTURN-008 AC-007 line 350; A8 RED line 589; TEST-MCP-SESSIONTURN-008 line 377.

Exact defect: The plan promises that a provisional pending ID and minted ID both resolve to one turn, yet the Node alias contract prunes the alias after every consumer present at commit acknowledges it. Once pruned, getAlias(pendingId) and resolveRequestId(pendingId) have no mapping; the accepted lifetime of the provisional ID is undefined. A8 checks all-ack replay cessation but not either-ID lookup after all acknowledgments.

Evidence: C2 explicitly says readers resolve either ID through the journal; FR-001-AC008 says either ID resolves to the same turn. TR-008 AC-007 explicitly says the alias is pruned only when acks contains every member of the persisted required set and getAlias returns null when absent. The test list includes resolveRequestId_pendingOrMinted_sameTurn and aliasAllAcked_replayStops as separate cases, without a combined after-ack lookup case. What-if: register C1/C2, commit alias pending-P -> req-R, both apply and ack, prune, then a reader holding pending-P calls getAlias; the specified absent result contradicts the FR.

Change to PASS: Retain a durable pending-to-minted lookup after all acknowledgments while retiring only the consumer-delivery obligation. Specify its storage, lifetime, replay/restart behavior, and separation from the prunable notification set in C2, FR-001-AC008, TR-008 AC-007, the A8 task and its TEST statement. Add a RED case that registers two consumers, acknowledges both, restarts, resolves both IDs to the same turn, and verifies no more notifications. Propagate the clarified contract to the TODO description and technicalDetails if the latter summarizes C2; update the handoff if it summarizes the lifecycle.

Classification: Implementer edit to plan and synchronized TODO; no operator decision needed under agreed C2. If the author wants pending-ID lookup to expire after acknowledgment, obtain an explicit operator decision between permanent lookup and bounded lookup, and revise C2/FR accordingly.

## Round-20 re-attack

- F-R20-01: PASS: scoped C9 trusted mint-adapter exception and negatives in C9, FR-001-AC008, TR-004, A3/A8.
- F-R20-02: PASS: mint cases reassigned to FR-001-AC008/A3/TEST-001; system envelope remains in A3.
- F-R20-03: PASS: ended.yaml reason idle/session_end, atomic idle revival and request_unknown no-target cases in host row, AC009, TR-003, A3.
- F-R20-04: PASS: four missing marker contracts and nine-entry readback in FR-008-AC005, TR-009, A9, critical files.

## Earlier rounds and MEMORY-PROCESS-008

Reviewed r1-r19 receipts and their finding catalog. No separate regression established in the repaired Copilot, Cowork, queue/FIFO, C9, C2 factory, requirement mapping, gate order, and marker areas. F-R21-01 is a new C2 propagation counterexample: all-ack pruning versus durable either-ID lookup.

What-if: Two consumers acknowledge, alias pruned, later pending-ID lookup loses mapping. Propagation: Conflict persists through C2, FR, TR, TEST, A8, and exact TODO description; TODO technicalDetails notes C2 agreement but not alias lifetime. Handoff does not resolve it. Recurrence: Earlier rounds repeatedly found contracts fixed in one section but not propagated to tests and tasks. This repeats that pattern in the final acknowledgment transition.

## Verdict and proof

OverallVerdict: **DISAGREE**. Accuracy 97/100; completeness 96/100; confidence 97/100. P1-P10: 7 PASS, 3 FAIL, 0 UNKNOWN. FAIL: P5, P7, P8. UNKNOWN: none. Implementer fix: F-R21-01. Operator-decision failures: none. The 98/98 and every-claim-PASS approval threshold is not met.

Runner JSONL: /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261010T002325Z-plan-readiness-r21.request.jsonl; /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261010T002325Z-plan-readiness-r21.response.jsonl. Reviewer turn persistence proof: updateTurn and completeTurn returned successfully. A read-only client.SessionLog.QueryAsync through the Codex plugin wrapper returned the reviewer session and request IDs, the complete response ending with the Codex hooks source line, both receipt paths, F-R21-01, and turn status completed; queryHistory returned the same reviewer session with turnCount=1.

Source checked for Codex hook semantics: [OpenAI Codex hooks](https://learn.chatgpt.com/docs/hooks).
