# Hostile Plan-Readiness Receipt: Claude independent validator

**Verdict: DISAGREE / NOT-READY.** Accuracy 0.80, Completeness 0.52, readiness confidence 15/100. Claims: PASS 0, FAIL 12, UNKNOWN 0.

The plan's direction is sound: one shared policy, guidance on by default, server enforcement for workspaces that select BDPv4, extending the planned CompletionGate rather than adding a parallel service. But many decisions that define the enforced behavior are deferred to Phase 1, and several statements conflict with the architecture it extends without an explicit amendment.

- **Identity:** ClaudeIndependentHostile. Requested ClaudeCode / claude-opus-5-5 / max. The session reports model claude-opus-5-5; reasoning effort is not observable from inside the session, so the transport metadata is authoritative.
- **Snapshot:** commit e7ec28f90cdcd5da355c9e2e4ece2accf3dc73c9; plan docs/plans/2026-10-20-plugin-engineering-policy.md, SHA-256 54C8F4B71E652A579E7D19472A33F9C3642B1001C5151EB1233A40F175BB24A2 (taken from the manifest, not recomputed). Round 1, PlanReadiness stage.

## Findings, ranked

1. **F1 Critical: no trust anchor for evidence** (plan L118-124, L136-138, L79-81).
   - Raw runner artifacts, receipts and requestClass are all written in the agent's own environment, and so is process selection unless restricted. requestClass is set by the model (arch L111), and in Phase B the server reads no receipt (arch L2157, L2301).
   - The gate therefore only checks that self-supplied evidence is consistent. Classifying a code change as Docs or Chore, or switching the process to None, bypasses it.
   - Fix: name the accepted receipt producer and the server-side re-derivation, add a class-bypass rule, and make process selection operator-only, stamped at completion.
2. **F2 Critical: snapshot and freshness are undefined and contradict the architecture** (plan L125-129, L161).
   - The architecture never reads the working copy (arch L451, L2747) and says uncommitted edits never make evidence stale (arch L2755, base plan L1446).
   - 'Affected', 'relevant untracked' and 'covered code' are undefined, and the algorithm is deferred.
   - Fix: put the algorithm, staleness scope, computing side and federation behavior in the plan, plus an explicit architecture amendment.
3. **F3 High: gate placement and contract are undecided.**
   - The gate runs inside a database-only SerializableWrite delegate (arch L344, L414), but the plan validates artifacts at the gate.
   - Open: the golden-contract CompletionPolicy, ICompletionPolicy and Get-CloseRemedy, and the fixed missing-item order requestClass, agentReview, hostileReview (arch L83, L2253, L2320-2351); also the QB-AGENT-001 C8 contract (arch L269).
   - Fix: a verification unit before the transaction, a database-only re-check, a separate policy, exact tokens and order, and the contract updates.
4. **F4 High: the completion unit conflicts with TDD and D10.**
   - RED-only Code turns and Code turns that complete before their work package's HV cannot complete (plan L118-121, L134-135).
   - The base process records RED as a step and completes such turns with a 'pending HV' Other review (base plan L177, L179, L195; arch L2323).
   - Fix: define the unit, a stage field for evidence, and the accepted review statuses, as an explicit amendment.
5. **F5 High: evidence storage cannot work by default and is not designed.**
   - Agent-context evidence and relationships are refused when Mode=Off (arch L327, L2685) and are scoped to responses (L2790, L2811-2819).
   - Phase 2 migrations contradict arch L5404. No entities, keys, root ids, dump policy, federation class or defaults are given.
   - Fix: a turn-scoped data model that is not mode-gated, migrations in Phase 4, and the units, routes, tools, REPL and client methods listed.
6. **F6 High: delivery to the nine hosts is not designed.**
   - No per-host channel, no definition of 'invocation', and no dedupe key. Codex has no compaction hooks (generate-wrappers.ps1 L24-34).
   - Restoration delivery conflicts with FR-MCP-115, TR-MCP-PLUGIN-009 and TEST-MCP-155: PostCompact emits no additionalContext (arch L1380).
   - The Stateless channel, mandatory status, budget and deadline outcome are undecided (arch L2663, L2690).
7. **F7 High: section selection cannot work at prompt time.** requestClass is not set when the hook runs (arch L111, L2257; plan L82-84).
8. **F8 High: configuration and status contract missing.** Process selection store and authority, guidance disable and reload, the enforcement switch, and status fields or surfaces are all unspecified (plan L13, L79-81, L90-91, L252-262; mcp-status.ps1 L91-105).
9. **F9 Medium: canonical asset under-specified.**
   - No path, format, version or hash rule; no server-side copy; links are unpinned blob/main; no license fallback.
   - The sync copies only lib-ps and skills/handoff (sync-plugin-core.ps1 L129-140), and a packaged-content word scan applies (Build.SyncAgentPlugins.cs L406-434).
10. **F10 Medium: gates and phase exits incomplete.**
    - The validation inventory is deferred (L161).
    - Not named: G5 ValidateConfig/ValidateTraceability, TestPluginCore, TestAgentPlugins, Test --test-projects, and three-provider migration tests.
    - Phases 2-4 have no HV or operator approval (arch L88, L5391; base plan L182-200).
11. **F11 Medium: recovery and rollout specifics missing.**
    - Replay of a completion whose evidence went stale, against the 5-attempt quarantine (arch L170).
    - Stop gate and clear-session handling of the new items; re-gating of PUT on completed turns (arch L2346).
    - Plugin version against the single 1.120.0 sync (arch L5578), and binary-rollback defaults (L5580).
12. **F12 Medium: evaluation and review criteria unbounded.** No task set, runs, grader, rubric or decision rule (plan L142-145, L241-248).
13. **F13 Medium: no precedence order** among user/operator instructions, workspace rules, the selected process and default-on guidance (plan L13-22, L52-67).
14. **F14 Low: provenance and ownership.** The plan cites inspection commit 5187b7b6, not the reviewed e7ec28f9. The base plan is NOT APPROVED (base plan L4) and this is not acknowledged. Slices have no owners (arch L5444-5458), and slice 3's dependency on Phase C is unstated.

## Claim verdicts

- **C1 FAIL.** Scope, default-on guidance and the nine hosts are correct (L13-22, L93-140). Missing: precedence (F13) and per-host limits (F6).
- **C2 FAIL.** The plan correctly reports the foundations as missing (verified: SessionLogService.cs L1329-1361; SessionLogModels.cs L108-213). But six conflicts with the architecture are unresolved, and ownership and ordering have gaps (F2, F3, F5, F6, F10, F14).
- **C3 FAIL.** See F9. The Grok Bot inventory gap is correctly identified (Build.SyncAgentPlugins.cs L19-29; generate-wrappers.ps1 L8).
- **C4 FAIL.** See F7 and F8.
- **C5 FAIL.** See F6.
- **C6 FAIL.** See F5.
- **C7 FAIL.** Writers, origins and terminal states are covered (L118, L132-140). Failing parts: F1, F3, F4, and Replace re-gating (F11).
- **C8 FAIL.** See F2. The review record has no snapshot field (arch L114, L2163). Artifact formats are undefined, and the default Test target writes TRX only when TestRunId is set (Build.Test.cs L44-68). The acceptance mapping is undefined.
- **C9 FAIL.** The phase order matches BDPv4 (Development-Process-draft-v4.md L62, L76, L94), but see F10.
- **C10 FAIL.** The open-turn inventory with no backfill and the staged enablement are sound (L256-259). Failing parts: F8, F11.
- **C11 FAIL.** There are no efficiency claims and the disclaimers are honest (L29-30, L241-244, L268-270), but see F12.
- **C12 FAIL.** Implementation is gated at the Phase 1 exit (L3, L163-165), but F1-F13 leave consequential choices to implementers, and nothing requires approval after Phase 1.

## What to keep

- One shared PowerShell core, with BDPv4 selected per workspace.
- Extending CompletionGate rather than adding a parallel service.
- Keeping the failed/canceled rules and the import/replication exemptions.
- No enforcement while work moves from red to green.
- No backfill of passing receipts.
- The Grok Bot inventory fix.
- No assumed efficiency gains.

## Path to threshold (minimal plan edits, in rank order)

1. Evidence trust model (F1).
2. Snapshot algorithm and architecture amendment (F2).
3. Gate placement and contract (F3).
4. Completion unit and TDD stages (F4).
5. Turn-scoped data model and surfaces; migrations in Phase 4 (F5).
6. Nine-host delivery table and compaction, Stateless and deadline rules (F6).
7. Section-selection rule (F7).
8. Settings, authority and status contract (F8).
9. Asset specification and license fallback (F9).
10. Per-phase gates, HV and approvals (F10).
11. Recovery and rollout specifics (F11).
12. Evaluation protocol (F12).
13. Precedence order (F13).
14. Provenance and ownership (F14).

## Limitations

- Read-only review of the packet only. The architecture and base plan were read in targeted sections plus searches; unread sections may hold resolutions.
- Hashes come from the manifest and were not recomputed.
- The transcript is not in the packet, so intent was checked only against the contract recorded in the plan.
- No builds or tests were executed.
- No clock was available, so the time in the timestamp is nominal.
- Plan mode asked for a plan file and ExitPlanMode, but neither tool was available; nothing was written.
- The confidence figure is readiness confidence, not confidence in this verdict.

## Commands executed

Only read-only Glob, Grep and Read calls over the packet (listed in Receipt.CommandsExecuted). No shell, build, test, hash or network command was run.