# Claude independent hostile receipt: plan readiness, round 4

**Identity:** ClaudeIndependentHostile (a separately launched Claude reviewer). Requested: ClaudeCode / claude-opus-5-5 / max. The coordinator confirms runtime identity from the transport metadata.
**Snapshot:** commit 1e250cd1cd00287e5548fb2a6f06050af025ee63. Plan: docs/plans/2026-10-20-plugin-engineering-policy.md, SHA-256 064737AD1AA8A02DF064AE39303A3B588243EE2C97665B2D2FE021469AF89759 (manifest value, not recomputed).
**Verdict:** DISAGREE / NOT-READY. Accuracy 0.91, completeness 0.82, confidence 44. Claims: 3 PASS (C1, C3, C5), 9 FAIL (C2, C4, C6 to C12), 0 UNKNOWN.

## Findings (ranked)

**EP-R4-01 High: derived Code deadlocks the base HV protocol.** A turn's opening is the certified pointer (§13.1 L710-722), and only RecordBaseline moves it (§14.7 L1406-1408). Any change outside docs therefore makes every later turn effective Code, including declared Chore/Docs turns (§8 L455-464, §13.3 L867-869).

The base HV protocol (base plan L197-198) runs the reviewer in the worktree, writes docs/receipts/hv, and requires the reviewer's own Chore turn to complete. Every phase/slice arrow (§9 L592-597) and every inventory or baseline attestation relies on that protocol. Under enforcement, that turn needs evidence and a TodoType it can never have. §13.5 amends only the receipt roots in arch 5.1/5.4, and the plan defines no baseline cadence after SliceExit.

*Fix:*
- Run HV and grader sessions in a separate registered review workspace (DevelopmentProcess=None) over a read-only bundle, with receipts kept only in the artifact store.
- Explicitly amend the base-plan HV protocol for engineering-gated workspaces.
- After operator approval of SliceExit, let RecordBaseline accept the matching Slice AGREE attestation (Slice profile adds Scope/Safety/Provenance claims); otherwise require a Baseline HV.
- Add tests.

**EP-R4-02 Medium: the SliceExit attestation binding is circular.** ReviewRefsJson is fixed when evidence is created (§7 L411-412, L430; §13.3 L842-845). The attestation comes later, and its subject includes that same evidence revision (§13.3 L823-826; §14.2 L1112-1115). The plan does not say how the gate finds attestations, how superseding or conflicting attestations are resolved, whether evidence must belong to the completing turn, or what TODO done re-verifies (§8 L485).

*Fix:* select attestations by recomputed SubjectDigest. The newest non-superseded attestation must be AGREE, with no non-superseded DISAGREE. ReviewRefsJson holds prerequisites only. Completion evidence must come from the same turn. TODO done repeats the DB-only checks plus preflight snapshot equality.

**EP-R4-03 Medium: runner layout and argv for multi-repository gates.** §9 L580-582 uses a per-job `<isolated-parent>`, but §13.2 L774-777 allows only two substitutions. Neither the layout nor the repositoryId-to-directory mapping is specified, even though Nuke discovers the exact mcpserver-*-plugin siblings (Build.SyncAgentPlugins.cs L11-29, L144-203).

*Fix:* use an inputs/<DirectoryName> layout and give G6/PowerShellOnly a literal --agent-plugin-parent .. (or a typed placeholder). Add a test on both operating systems.

**EP-R4-04 Medium: job allocation contradiction.** §6 L374-375 allocates one job per snapshot/gate/platform/attempt, while §7 L408-409 and §14.1 L1016-1031 allocate per evidence revision. Because revisions are turn-local, Fail memory is not enforced across revisions or turns. The new superseded columns have no types.

*Fix:* allocate per revision. A Fail on (SnapshotId, GateId, Platform, InventoryDigest) blocks a Pass on that tuple workspace-wide. Declare the column types.

**EP-R4-05 Medium: late-mint boundAtUtc has no interface.** §14.7 L1409-1416 binds openings to the ingress boundAtUtc, but SessionLogMintTurnRequest (arch L633) has no such field.

*Fix:* add an optional replay-only field. A live mint uses the server clock. Values outside [session open, now+5 s] make the opening Unavailable. Amend arch 4.2 and the golden contract.

**EP-R4-06 Medium: configuration channel and status host-session selection.** The plan defines the signed manifest but never says how it reaches ConfigurationReconciliationService (§13.1 L688-694; §14.5 L1299-1324). The plugin status fields host and hostSessionKey (§3 L130-133) have no rule for choosing a session.

*Fix:* name the channel and its lifecycle. Status reports the invoking session; otherwise it returns an array.

**EP-R4-07 Low: evaluation contamination.** The baseline condition depends on a successful status read, and on timeout packaged guidance is delivered anyway (§3 L133-136). GuidanceEnabled is a protected instance setting (§14.5 L1299).

*Fix:* run two identical instances that differ only in GuidanceEnabled. Treat a run as infrastructure-invalid when the ledger outcome does not match its condition.

**EP-R4-08 Low: Requirement hash compatibility.** §14.2 L1093-1097 changes the hash input, but base Phase C ships first (arch §8.5 L5578; plan §11 L630).

*Fix:* adopt the amendment in the base before Phase C persists hashes, or version the hash.

**EP-R4-09 Low: residual inconsistencies.**
- Inventory v1 fields are scattered across §13.2, §14.4, §14.5 and §14.6.
- The remedy code is record_evidence in §13.3 L885-889 but approve_todo_type in §14.1 L1040.
- acceptance_criterion_format_required has no status (§4 L259-261).
- tools/powershell scripts, embedded docs/schemas, the listeners and ConfigurationReconciliationService have no owner.

*Fix:* add one normative shape, align the remedy list, return a 400 ClassifiedError, and assign owners.

## Claim verdicts
- **C1 PASS:** §1 L8-42, §3 L96-120, §5 L290-319, §11 L637-640.
- **C2 FAIL:** the planned-vs-implemented distinction is accurate (SessionLogService.cs:1329 exists; no gate types in src). Fails on EP-R4-01 and EP-R4-09.
- **C3 PASS:** §2 L46-89; sync-plugin-core.ps1 L115-151 copies lib-ps/policy byte-identically with per-file hashes; PowerShell-only rules and the dash ban hold.
- **C4 FAIL:** settings and status are otherwise complete (§3, §13.1, §14.5). Fails on EP-R4-06.
- **C5 PASS:** §5, §13.4 L894-916, and §14.7 L1418-1430, consistent with base §3.1 L221.
- **C6 FAIL:** §4, §7 and §13.4 are detailed. Fails on EP-R4-05 and EP-R4-09.
- **C7 FAIL:** §8, §13.3 and §14.1 cover writers, origins, CAS and refusals. Fails on EP-R4-01, EP-R4-02 and EP-R4-04.
- **C8 FAIL:** §6, §13.2, §14.2 and §14.4 are strong. Fails on EP-R4-02, EP-R4-03, EP-R4-04 and EP-R4-09.
- **C9 FAIL:** §9 and §14.6 match the base G5 rule (base plan L189) and the TestRunId path (Build.Test.cs L27-37). Fails on EP-R4-03.
- **C10 FAIL:** refusal, rollback and census are concrete (§8 L515-522, §14.6). Fails on EP-R4-01, EP-R4-05, EP-R4-06 and EP-R4-08.
- **C11 FAIL:** the evaluation is bounded and honest (§10, §13.5, §14.3). Fails on EP-R4-07, and on EP-R4-01 because the HV cannot complete.
- **C12 FAIL:** approvals are retained (L3-4, L1442-1444), but EP-R4-01 to EP-R4-09 still leave decisions to implementers.

## EP-R3 verification
- Resolved: 06, 07, 08, 09, 12, 14.
- Resolved with a residual: 01 (EP-R4-04), 02 (EP-R4-03), 03 (EP-R4-09), 04 (EP-R4-08), 05 (EP-R4-07), 10 (EP-R4-06), 11 (EP-R4-09), 13 (EP-R4-05, EP-R4-01).

## Path to threshold
Apply the nine fixes in rank order; they cover every consequential gap found in this pass. Then run a fresh review. With these fixes in place and no new contradictions introduced, all 12 claims are projected to PASS.

## Limitations
- Read-only packet review only: no builds, tests or hash recomputation.
- Base documents were read selectively.
- Not inspected (outside the packet): Build.UpdateService.cs, Build.TestDependencies.cs, TodoExecutionService, the RequirementAcceptanceCriterionEntity Text capacity, the plugin cache location, and whether the --gate-started-at-utc parameter exists.
- Prior-round receipts were not in the packet.
- TimestampUtc carries the session date only.

## Commands
Glob, Read and Grep only, as listed in Receipt.CommandsExecuted. No shell, build or test commands were run.