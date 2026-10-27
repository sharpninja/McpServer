# Hostile validator: PLAN-SESSIONTURNREVIEW-001, round 19

TimestampUtc: 2026-10-10T00:05:51Z.  
ValidatorIdentity: Codex CLI, gpt-6-sol, xhigh. Live proof: `/home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T18-53-14-01a12315-ceee-7ef0-be5c-63d5f2296d71.jsonl`; own `session_meta` thread `01a12315-ceee-7ef0-be5c-63d5f2296d71` and first `turn_context` record the model and effort.  
HEAD: `61251a0bc5bf8dacbf5df7df10c0105ea0c195a6`.  
Reviewer sessionId: `Codex-20261009T235651Z-plugin-session`; requestId: `req-20261009T235650Z-hv-r19`.

**OverallVerdict: DISAGREE. Accuracy 94; completeness 89; confidence 96.** Approval requires 98/98 and every applicable claim PASS.

## Mandatory intake and audit scope

- Read the add-profile skill and all 19 non-skill Markdown profile files in full.
- Read `/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml` in full, filtering its key line and Authentication block. Read all 13 memories returned by `workflow.memory.list` in full, including MEMORY-PROCESS-003, -005, -007, and -008.
- Reviewed all 649 plan lines (529 nonempty), all 30 live TODO tasks and technicalDetails, the 159-line handoff, round 18, and r1-r17 finding catalogs. The TODO description's 529 lines exactly match the plan's 529 nonempty lines. TODO status is NOT APPROVED.
- Verified the HEAD, nine plugin checkouts, cited repo paths and build targets, requirements store, and triage reports read-only. No build or test was run for this plan review.

## P1-P10

- **P1 PASS.** Rechecked factual source inventory at HEAD, named server/client/REPL paths and routes, build targets, plugin manifests and checkout heads. No newly false source fact found in the plan. The A1 outbox clause is an invalid phase dependency, detailed below.
- **P2 PASS.** 120 canonical cited FR/TR/TEST IDs were found in the live store or explicitly proposed in P0; zero new-ID collisions. Live counts: FR 354, TR 482, TEST 514. Mappings agree with docs/Project and the live store.
- **P3 PASS.** D1-D12, partial clearance, licensing, skipped_duplicate removal, and the operator's working rules are represented in phases and acceptance text. The failures below concern execution contracts rather than a reversal of those rulings.
- **P4 PASS.** Server, client, REPL, MCP tools, PowerShell and Node cores, all eight requested agent plugin families, and SQLite/PostgreSQL/SQL Server migrations have concrete phase tasks and tests. Compatibility handling is specified but has a retry contradiction (F-R19-03).
- **P5 FAIL.** F-R19-01, F-R19-03, and F-R19-04 leave system-entry shape, retry classification, and alias API acquisition to the implementer.
- **P6 FAIL.** F-R19-02 makes the A1 gate depend on a Node outbox scheduled for A8; F-R19-03 leaves rollback retry behavior undefined. Named G1-G6 target paths otherwise exist. BDPv4 order is stated per slice.
- **P7 FAIL.** F-R19-01 makes the system-entry root-id criterion unsatisfiable; F-R19-03 lacks discriminating 404 cases; F-R19-02 misplaces a Node outbox assertion. HV-P0, HV-A, HV-B and slice HV use Codex gpt-6-sol xhigh and 98/98 consistently.
- **P8 FAIL.** F-R19-01 contradicts the wire contract, F-R19-02 contradicts A8 and the TODO/handoff, F-R19-03 contradicts C9, and F-R19-04 leaves the public context-bound interface undefined.
- **P9 FAIL.** Description parity and the 30 implementationTasks pass. Handoff counts/status and triage IDs pass. Handoff line 121 and TODO technicalDetails falsely say the Node outbox no-retry test moved out of A1 (F-R19-02).
- **P10 PASS.** Risks checked against source; ten cited report IDs resolve in main-workspace triage, and `triage-group-804b21086f3019ab` resolves in the worktree triage store. No unfiled risk found.

## FAIL findings

### F-R19-01 — system queue entry has an impossible turn envelope

Plan location: host binding table line 83; FR-MCP-SESSIONTURN-007-AC009 line 248; wire queue contract line 551; A3 RED line 579.

Defect and evidence: line 83 makes a `kind: system` enqueue an unminted queue entry and says it carries the root-id envelope. Lines 248 and 551 require *every* queue entry to carry `{workspacePath, agent, mcpSessionId, hostSessionKey, ingressNonce, requestId}`. A system event has no ingress nonce or requestId under the same host rule. Minting or inventing one would contradict the user-turn mint rules. The A3 interleaving tests do not settle how this record is attributable after replay.

To PASS: define a stable session-level parent envelope for system entries; restrict the six-field turn envelope to request-kind entries in the host rule, FR-007 AC009, wire shape, and A3 write/replay tests. Every system entry still needs a traceable parent relationship. **Implementer edit; no operator decision.**

### F-R19-02 — A1 still requires the deferred Node outbox assertion

Plan location: A1 RED line 575; A8 RED line 589; handoff line 121; TODO technicalDetails.

Defect and evidence: A1's six-collision `MintRace_SixCollisions_500FinalDetailCode` text still says the error is one “that the Node outbox does not retry.” A8 separately schedules `nodeOutbox_mintCollisionExhausted_final_noRetry` against the QB outbox after integration. `plugins/core/lib-node/src/cache/audit-outbox.ts` is absent at HEAD. The handoff and TODO technicalDetails claim this assertion moved, but it remains in A1. Thus A1 cannot meet its own RED/green gate at its scheduled time.

To PASS: make A1 assert only the server's final HTTP 500/detail code after six collisions. Keep the actual Node outbox no-retry test solely in A8 after QB integration. Correct TODO technicalDetails and handoff. **Implementer edit; no operator decision.**

### F-R19-03 — unsupported mint and closed session share an undiscriminated 404

Plan location: C9 line 126; FR-MCP-SESSIONTURN-001-AC008 line 200; TR-MCP-SESSIONTURN-004 AC005 line 346; A8 RED line 589; rollout line 620.

Defect and evidence: AC008 retains and later retries a mint rejected because an older server/REPL lacks the route or verb, but treats a 404 closed-session mint as final. TR-004 specifies 404 for the latter without a detail-code or version discriminator. A missing route on an older server also returns 404. C9 allows retry only for transport outage and degraded persistence and treats unknown codes as final. Rollback explicitly depends on the unsupported-mint retry path.

To PASS: define the exact capability or typed-error discriminator, reconcile its retry class and bounded drain behavior with C9, and add named A3/A8 tests for both 404 variants and rollback replay. **Implementer edit; no operator decision.**

### F-R19-04 — alias consumer interface lacks a context acquisition contract

Plan location: TR-MCP-SESSIONTURN-008 AC007 line 350; A8 contract line 589.

Defect and evidence: line 350 promises exported `registerAliasConsumer(consumerId)`, `listUnacknowledgedAliases(consumerId)`, `getAlias(id)`, `acknowledgeAlias(consumerId,pendingId)`, and `onAliasCommitted(handler)` without a context parameter, then requires every call to bind to immutable workspace/agent/MCP-session/host-session context and forbids process-global state. There is no factory, receiver, or acquisition method in the contract. At HEAD `src/index.ts` exports `createQBrainAiPluginCore(config): HostContext` and no alias service; `HostContext` has mutable active session fields. An implementer still must choose the public API shape.

To PASS: name the factory or HostContext method that returns the context-bound alias service, define its arguments and immutable fields, and specify when each consumer obtains it before registering. Test the exact API across two host sessions in A8 and the QB fixture. **Implementer edit; no operator decision.**

## Re-attack and recurrence

- R18 F-R18-01: FIFO positioning and system-entry removal/crash cases are present, but the turn-envelope conflict is F-R19-01.
- R18 F-R18-02: consumer set, late registration, callback signature, and fixture names are present; acquisition gap remains F-R19-04.
- R18 F-R18-03: A1's asserted Node outbox behavior remains despite the claimed move (F-R19-02). The same inaccurate claim propagated to the TODO technicalDetails and handoff; this is the recurrence signal under MEMORY-PROCESS-008.
- R18 F-R18-04: Stop gate writer and crash-preservation case are present; no residual defect found in that repair.
- R1-r17 finding catalogs: no independent regression found beyond the queue and C2 gaps identified above.

MCP persistence proof: installed Codex plugin client.SessionLog.SubmitAsync returned id 107, persisted true, degraded false, queued false. Readback through client.SessionLog.QueryAsync returned this Codex session with one completed turn, the matching requestId, and the complete 9,532-character result (the store normalizes four em dashes to hyphens).

All four FAILs need implementer edits. No new operator decision is required. Unknown list is empty. Runner request and response JSONL are `docs/receipts/hv/20261009T235313Z-plan-readiness-r19.request.jsonl` and `docs/receipts/hv/20261009T235313Z-plan-readiness-r19.response.jsonl`.

=== VERDICT JSON ===
{"overallVerdict":"DISAGREE","accuracy":94,"completeness":89,"confidence":96,"passCount":5,"failCount":5,"unknownCount":0,"failList":["F-R19-01","F-R19-02","F-R19-03","F-R19-04"],"unknownList":[],"implementerFixes":["F-R19-01","F-R19-02","F-R19-03","F-R19-04"],"operatorDecisionFails":[],"headSha":"61251a0bc5bf8dacbf5df7df10c0105ea0c195a6","receiptPaths":["/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-plan-readiness-r19-20261009T235313Z.md","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-plan-readiness-r19-20261009T235313Z.json"],"reviewerSessionId":"Codex-20261009T235651Z-plugin-session","reviewerRequestId":"req-20261009T235650Z-hv-r19"}
