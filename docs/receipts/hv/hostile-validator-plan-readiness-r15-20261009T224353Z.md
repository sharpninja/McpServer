# Hostile validator: plan readiness round 15

TimestampUtc: 2026-10-09T22:56:22Z. **DISAGREE**. Accuracy 90/100; completeness 84/100; confidence 97/100. Claims: 2 PASS, 7 FAIL, 1 UNKNOWN. Approval requires 98/98 and every claim PASS.

## Identity, intake and scope

The live Codex rollout at /home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T17-43-54-01a122d6-54a4-73c1-ad97-906d9344245d.jsonl records turn_context at 2026-10-09T22:43:55.944Z with model gpt-6-sol, reasoning effort xhigh and this worktree as cwd. Reviewer session: Codex-20261009T032846Z-plugin-session; request: req-20261009T224633Z-hostile-plan-r15. HEAD: 306cb0369c3642c70ba7a4437db4f525afa76021.

I read add-profile SKILL.md, all 19 non-skill profile markdown files, the full parent AGENTS-README-FIRST.yaml with its apiKey line and Authentication block filtered, and all 15 effective live MCP memories. I applied MEMORY-PROCESS-008 what-if, propagation and recurrence checks. No marker secret is reproduced. I reviewed the complete 649-line, 529-nonempty-line plan; main-workspace TODO and all 30 implementation tasks; full handoff; r14 and r1-r13; docs/Project and docs/context; HEAD; all nine plugin repositories; and live read-only requirement and triage records. Historical implementer conduct, infrastructure repair and the Pi3 monitor were not scored. This review changes only these receipts and my own MCP turn.

## Claim assessment

- **P1 FAIL.** The host row's factual statement that a remove carries no record identity is inaccurate as worded; see F-R15-08. The source/index check also establishes F-R15-07.
- **P2 PASS.** The 21 FR, 23 TR and 23 TEST proposed IDs are marked for P0 creation and absent from the live store. Existing citations and mapping families were checked against docs/Project and the live store (353 FR, 481 TR, 513 TEST). The plan's 352/480/512 baseline is dated. No collision found.
- **P3 FAIL.** D1/D2/D5/D6/D9 are undermined by F-R15-01/02; the Stop lifecycle has F-R15-03. D3/D4/D7/D8/D10/D11/D12, licensing, partial clearance, 98/98 and removal of skipped_duplicate are represented consistently.
- **P4 FAIL.** Server, client, REPL, MCP, both plugin cores, all nine repositories and three storage providers are named. Cowork RED/live coverage is insufficient (F-R15-06), and all-provider migrations lack the uniqueness constraint required by AC001 (F-R15-07).
- **P5 FAIL.** Queue ambiguity, unknown queue records, post-Stop delivery and threshold lookup leave implementer choices open: F-R15-01..04.
- **P6 UNKNOWN.** Named build targets and test directories exist, and each slice states BDPv4 order. Full execution cannot be certified in this read-only review: only .NET SDK 10.0.111 is installed and Pester/bats are absent. P0.1 specifies those prerequisites. No gate is claimed green.
- **P7 FAIL.** A3 tests the ambiguous rule rather than correct host identity and lacks malformed fail-closed cases; A7 lacks enqueue/pass/deliver/Stop; S8/S9 lack nondefault-threshold cases; Cowork-specific RED/live tests are missing. HV model, xhigh and 98/98 gates are otherwise consistent.
- **P8 FAIL.** The 30 tasks and phase order broadly match, but invocation boundary differs between host/FR and TR/wire (F-R15-05), and queue, ledger and threshold behavior disagree across sections.
- **P9 FAIL.** TODO description exactly matches all 529 nonempty plan lines; 30 tasks including revised P0.1/P0.2 match the task list. Handoff status, counts and locations are accurate, but its all-r14-fixed statement is false (F-R15-09).
- **P10 PASS.** Ten listed triage reports resolve live. Group triage-group-804b21086f3019ab exists under the worktree, is failed and contains one report, as stated. Plugin repairs are scheduled in A9; remaining listed risks are filed or scheduled.

## FAIL findings

### F-R15-01 - equal-content queue removal still guesses a request

Plan: host binding table line 83; FR-MCP-SESSIONTURN-001-AC010 line 202; A3 line 579. An unkeyed equal-content remove does not identify which queued request was removed. Marking all candidates delivered for absorbed_mid_turn fabricates deliveries. Withdrawing the oldest can withdraw the wrong token; popAll has no equal-content tie rule. Live Claude remove records have content, reason, commandUuid and deliveryId, but enqueue records have no corresponding UUID or delivery ID. If equal queued R1 and R2 exist and the host removes R2, the plan withdraws R1; a later dequeue of R1 is assigned R2. A3 asserts the chosen rule, not host identity. **Make PASS:** fail closed or hold all ambiguous candidates without changing individual state unless P0.2 proves a stable shared key or removal order; define popAll, add adversarial and replay RED, propagate through host row, AC010, A3, UC-1 and matrix. **Classification:** implementer plan/AC/test edit under D1/D2/D6/D9; no operator decision.

### F-R15-02 - unknown queue record can be skipped forever

Plan: host row line 83; AC009 line 201; FR-MCP-SESSIONTURN-005-AC005 line 227; A3 line 579. An unrecognized queue-operation shape produces only a warning and no state change, while the same atomic replacement advances appliedThroughOffset. Replay skips the record forever even if it represented an arrival or delivery. A3 tests only the warning. **Make PASS:** explicitly identify benign operations; stop before any potentially relevant unknown shape, do not advance past it, and mark turn logging unsupported until a verified adapter handles it. Add malformed, partial and replay RED plus capability disposition. **Classification:** implementer edit under D5/D6; no operator decision.

### F-R15-03 - queued turn can fall out of Stop ledger

Plan: FR-MCP-SESSIONTURN-005-AC001/006 lines 223/228; UC-1 line 477; wire store line 551; A7 line 587. UC-1 adds the minted queued token to the ledger, queued tokens do not block a passing Stop, and passing Stop clears the ledger. Later delivery has no specified re-enrollment or authoritative open-delivered scan. Counterexample: enqueue R2, pass Stop for R1, dequeue R2, Stop before R2 closes. **Make PASS:** specify re-enrollment or derivation from open-delivered state, keep host isolation, and add this exact two-Stop RED with restart. **Classification:** implementer plan/AC/test edit; no operator decision.

### F-R15-04 - effective threshold read contract absent

Plan: review decisions lines 43-48; FR-MCP-SESSIONREVIEW-005-AC002 line 315; TRs lines 360-363; S8/S9 lines 609-611. HV mapper must distinguish Fail versus Other using thresholds read from server, but no read operation, client/REPL route, timing or race rule is specified. Current source has no ReviewThreshold read method; S1/S4 add an internal provider, S5/S6 no read contract. For 0.97/0.99 scores and an override of 0.95, using default 0.98 chooses the wrong status. **Make PASS:** define authenticated effective-threshold read through all surfaces with override/race semantics, or atomic server-side verdict mapping; add nondefault override RED and golden cases. **Classification:** implementer contract edit, no new operator decision.

### F-R15-05 - r14 invocation boundary not propagated

Plan: TR-MCP-SESSIONTURN-001 AC-006 line 343; compare host row 83, FR-MCP-SESSIONTURN-007-AC006 line 245, A3 line 579 and wire line 551. Host/FR cap transcript application at the verb's invocation-start length. TR AC-006 still says Sync-PluginArrivalLog applies every pending record before binding, and wire line 551 omits the cap. A dequeue appended after entry but before lock acquisition can retarget the verb if the TR is followed. **Make PASS:** put invocation-start byte-length, complete-record boundary and lock ordering in TR and wire; link existing interleaving RED. **Classification:** implementer propagation edit; no operator decision under D2.

### F-R15-06 - Cowork support lacks host-specific verification

Plan: P0.2(b) line 170; AC009 line 201; A3 line 579; A9 line 591; live gate line 635. P0.2 now names Cowork prompt-form/FIFO probes, but A3 has generic bridge fixtures and a ClaudeCode-specific case, A9 only says Cowork connector, and live smoke sends one prompt. The installed Cowork host is not checked across image-only, large, multiline and FIFO bridge behavior before support. **Make PASS:** name Cowork RED fixtures and live multi-form/FIFO assertion when available; on failed/unavailable probes require unsupported and zero mint/write receipts. **Classification:** implementer test/gate edit under D5/D6/D7; no operator decision.

### F-R15-07 - cross-session request ID uniqueness is not enforced

Plan: FR-MCP-SESSIONTURN-001-AC001 line 193; TR-MCP-SESSIONTURN-004 AC-006 line 346; A1 line 575. AC001 promises workspace-and-agent-unique minted requestIds, but new unique index covers nonce within SessionLogId, and existing McpDbContext.cs:404 uniquely indexes (SessionLogId, RequestId). Two concurrent sessions can mint the same 8-hex suffix and both commit; a pre-insert lookup does not close the race. A1 has only a sequential collision test. **Make PASS:** specify a database-enforced workspace-wide or workspace-plus-agent RequestId constraint, preflight historical duplicates, retry unique violations and test forced cross-session concurrent collisions on SQLite, PostgreSQL and SQL Server. **Classification:** implementer service/migration/test edit; no operator decision.

### F-R15-08 - remove identity sentence is overbroad

Plan: host row line 83. It says a remove carries no record identity. In the inspected transcript all 19 remove records have commandUuid and deliveryId, while 27 enqueue records have neither. The accurate fact is lack of an established enqueue-shared key. **Make PASS:** state that distinction and inspect those fields in P0.2; do not assume they solve pairing. **Classification:** implementer factual correction; no operator decision.

### F-R15-09 - handoff overstates r14 resolution

Plan: handoff line 91; host row 83, TR 343, A3 579. Handoff says all F-R14-01..07 are fixed, while F-R14-02 remains F-R15-01, F-R14-03 remains F-R15-05 and F-R14-06 remains F-R15-06. The r14 receipt lines 41-59 and 81-89 specify these corrections. **Make PASS:** correct plan/test issues and then revise the handoff claim, retaining accurate status, counts and paths. **Classification:** implementer plan/handoff edit; no operator decision.

## Prior rounds and recurrence

R14-01 offset nonce, R14-04 UC-1 split, R14-05 accumulated remedy and R14-07 TODO P0.2 are fixed. The added P0.1 G2 baseline rule appears in plan and TODO. R14-02/03/06 remain as above. R1-R13 were re-attacked for stale hook-to-bridge pairing, prompt_id assumptions, model mint, skipped_duplicate, broad use-case gating, C2 acknowledgements and root-id envelope. Those older stale mechanisms are absent. The equal-content queue-removal design is the recurrence signal from R4/R13/R14: an unkeyed record still chooses a request identity.

## Runtime and persistence

No src, tests, build or plugins path changed from f56dcf70 to HEAD. Named targets and directories exist. P6 runtime result remains UNKNOWN until P0.1 supplies the toolchain and runs gates. Official Codex plugin hooks/trust reference consulted: https://developers.openai.com/plugins/build/plugins. Runner JSONL paths: docs/receipts/hv/20261009T224353Z-plan-readiness-r15.request.jsonl and .response.jsonl. Read-only client.SessionLog.QueryAsync returned my reviewer request in the server store with five actions, three dialog items, and byte-exact copies of the then-current Markdown and native PowerShell JSON receipts; the final receipt is re-persisted before completion. No apiKey, X-Api-Key or api_key value is in the receipts.

## Disposition

All nine findings are implementer edits under existing locked decisions; no operator decision is required. The plan remains NOT APPROVED. P6 is UNKNOWN. AGREE requires at least 98/98 and all claims PASS.

=== VERDICT JSON ===
{"overallVerdict":"DISAGREE","accuracy":90,"completeness":84,"confidence":97,"passCount":2,"failCount":7,"unknownCount":1,"failList":["F-R15-01","F-R15-02","F-R15-03","F-R15-04","F-R15-05","F-R15-06","F-R15-07","F-R15-08","F-R15-09"],"unknownList":["P6: full G1-G6 execution awaits P0.1 toolchain"],"implementerFixes":["F-R15-01","F-R15-02","F-R15-03","F-R15-04","F-R15-05","F-R15-06","F-R15-07","F-R15-08","F-R15-09"],"operatorDecisionFails":[],"headSha":"306cb0369c3642c70ba7a4437db4f525afa76021","receiptPaths":["/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-plan-readiness-r15-20261009T224353Z.md","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-plan-readiness-r15-20261009T224353Z.json"],"reviewerSessionId":"Codex-20261009T032846Z-plugin-session","reviewerRequestId":"req-20261009T224633Z-hostile-plan-r15"}
