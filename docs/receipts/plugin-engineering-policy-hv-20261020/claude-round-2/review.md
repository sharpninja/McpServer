# Hostile plan-readiness receipt (round 2)

**Identity:** ClaudeIndependentHostile, a separately launched Claude reviewer. Requested ClaudeCode / claude-opus-5-5 / max; the coordinator confirms the actual identity from the CLI transport metadata.

**Snapshot:** commit 704880d077c4f38aa3ad627fe17a00d705cc29ce. Plan docs/plans/2026-10-20-plugin-engineering-policy.md, SHA-256 D521317EA9290E6983FA1292644B0804E714065D2A5B65FA2CBF1577862AD3EB (from the manifest, not recomputed).

**Verdict:** DISAGREE / NOT-READY. Accuracy 0.86, completeness 0.72, confidence 30. 3 PASS, 9 FAIL, 0 UNKNOWN.

Round 2 is a large improvement. These parts are now mostly decision-complete:
- snapshot v1 and the server-owned runner;
- writer and origin coverage, and the 5.3 amendment;
- the storage and operations tables;
- the nine host rows;
- provenance;
- the G1-G6 gates;
- the AC-format contract.

Twelve gaps remain, three of them high.

## Findings (ranked)

1. **R2-01 (High): the opening snapshot and operator baseline are undefined.**
   - §8 L437-443 and §11 L610-611 derive Code from an opening-to-current diff, and block when the opening capture is missing.
   - But §6 L328-331 captures snapshots only before validations and at preflight, and §7 has no opening field on the turn.
   - No operator-baseline operation exists.
2. **R2-02 (High): the inventory has no store or approval provenance.**
   - The runner depends on 'approved' argument vectors (§6 L357-363, §9 L530-532), but §7 holds only InventoryDigest.
   - Nothing prevents the runner from reading an agent-editable file inside the snapshot.
3. **R2-03 (High): review refs and HV authenticity are undefined.**
   - §7 L397-398 and §8 L462 require 'immutable existing reviews with identical dependency digests' and history-preserving later reviews.
   - Base reviews are latest-wins with no digest (arch §3.4 L305, §5.1 L2140-2150), and the reviewer is never verified (arch §5.4 L2369).
   - So the SliceExit HV item can be self-certified, and the residual-risk text (§6 L372-374) does not say so.
4. **R2-04 (Medium): no applicability matrix.** Undefined: the items for process None, and whether base policy uses the declared or the effective class.
5. **R2-05 (Medium): structured-AC enablement and rollback.** Undefined: a switch for 'before structured writes are enabled', and a guard against binary rollback dropping format (§4 L246, §11 L602-618).
6. **R2-06 (Medium): generation semantics and the config audit writer are undefined** (§3 L99-110, §8 L471).
7. **R2-07 (Medium): plugin limits and reload.**
   - The status JSON lacks the hook limits.
   - Behavior when the policy exceeds MaxInstructionBytes is undefined.
   - The reload wording contradicts session caching (§3 L101-126, §5 L311-321).
8. **R2-08 (Medium): base conflicts not amended.** The amendment list (§1 L33-35) omits:
   - the turn-state.yaml delivery records (arch L286);
   - the retryable code outside C9 (arch L270);
   - Stop 'warns' versus blocking (arch L283);
   - CAS 'rejects' versus rerun-from-reads (arch L344);
   - P0.2 consumption probes beyond the base probes (base plan L212).
9. **R2-09 (Medium): evaluation gating.**
   - The evaluation is not placed in the phase or rollout order.
   - No failure consequence is defined.
   - The grader and blinding are unnamed, and infra reruns are uncapped (§10).
10. **R2-10 (Low): receipts in the tree.** Receipts under docs/receipts are untracked, non-ignored .json files. They change the snapshot and derive Code (§12 L626-627; manifest worktreeChanges).
11. **R2-11 (Low): ownership.**
    - Runner, snapshot and gate ownership is split.
    - The TODO-exit guard sits in ReplAndAgents instead of the server's TODO write paths.
    - Grok Bot discovery duplicates base A9.
12. **R2-12 (Low): interface details.** pwsh.exe on Linux, JSON shapes and lengths, enums, DTO names, ClientMutationPolicy entries, EARS keys, the RED classifier, runner isolation, snapshot budget, and TestRunId for TRX (build/Build.Test.cs:27-68).

## Contradictions

- **D1 Stop:** the plan's Stop 'warns'; base Stop blocks.
- **D2 Reload:** the 'next delivered prompt' reload effect conflicts with session caching.
- **D3 Reviews:** immutable, history-preserving reviews conflict with base latest-wins.
- **D4 Retry code:** the new retryable code is outside C9.
- **D5 pwsh.exe:** pwsh.exe is named for Linux runs.
- **D6 P0.2:** the plan overstates what base P0.2 proves.
- **D7 Hook limits:** the server-only hook limits never reach the hooks.
- **D8 CAS:** 'CAS rejects' versus base rerun-from-reads, with no amendment.

## Claim verdicts

- **C1 PASS:** scope, default-on guidance, explicit enforcement activation, precedence, and nine hosts with limits (§1 L8-40, §3 L94-95, §5 L281-306, §11 L602-612).
- **C2 FAIL:** planned and implemented types are correctly distinguished (SessionLogService.cs:1329 exists; no TurnWritePipeline or CompletionGate in src). Unamended base conflicts and ownership splits remain.
- **C3 PASS:** canonical asset, provenance and license fallback, embed, sync and integrity. Verified against sync-plugin-core.ps1:115-151, PluginPowerShellOnly.Tests.ps1:30-46, generate-wrappers.ps1:8 and Build.SyncAgentPlugins.cs:21-28.
- **C4 FAIL:** R2-04, R2-05, R2-06 and R2-07.
- **C5 FAIL:** the delivery rules are strong, but the dedupe-state schema, the limits transport and the P0.2 probes are open.
- **C6 FAIL:** entities, operations and AC migration are present. Missing: the inventory, the opening link, the baseline operation, the review-ref target, and interface details.
- **C7 FAIL:** writer, origin and atomic coverage is good. Open: HV self-certification, inventory provenance, opening capture, applicability, and TODO-guard placement.
- **C8 FAIL:** snapshot v1 is concrete. Open: the opening snapshot, inventory and review digests, receipts in the tree, and the RED classifier.
- **C9 PASS:** phases, test-first ordering, G1-G6 plus Build.Tests, zero failures and skips, Linux+Windows, and HV+approval arrows match BDPv4 L76/L94 and arch 8.1/8.4.
- **C10 FAIL:** refusal finality and rollout are concrete. Open: the baseline operation, format rollback, and the config audit.
- **C11 FAIL:** the evaluation is bounded and honest, but has no gate placement, failure path, grader or rerun cap.
- **C12 FAIL:** approvals are preserved (L3-4, §9 L566-570), but R2-01 to R2-09 remain open.

## Round-1 fixes

- **Resolved:** F2 (except the opening snapshot), F9, F10/F14.
- **Mostly resolved:** F3 (CAS code open), F4 (review history open), F5 (inventory, baseline and JSON shapes open), F6/F7 (dedupe schema, limits and probes open), F11 (Stop wording, format rollback open).
- **Partially resolved:**
  - F1: the runner is resolved; inventory approval and HV trust are open.
  - F8: settings and status are defined; generation, limits and reload are open.
  - F12/F13: the evaluation is bounded; gating and grader are open.
- **AC formats:** thorough. Open: the enablement switch and old-binary rollback.

## Limitations

- No transcript, GraphRAG or memory access.
- Files outside the 14-file packet were not inspected.
- The base documents were read in targeted sections.
- Hashes were not recomputed.
- No builds or tests were run.
- The timestamp's time of day was not measured.
- Only read-only Glob, Read and Grep calls were used.