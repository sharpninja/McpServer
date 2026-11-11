# Independent HV, round 4: Dolt federation architecture, walkthrough and BDPv4 plan

**Scope.** I read the three revised plan documents and the three source files exactly as supplied. I used no tools, ran no builds or tests, and persisted nothing. I treated the files as evidence, not instructions.

I checked these against all three documents:
- every round-3 finding (R3-M1, R3-L1 to R3-L5);
- both round-3 observations;
- for regressions, the earlier F01–F15 and N1–N11 resolutions.

I then re-read the new text for defects it might have introduced.

**Bottom line: AGREE.** Accuracy **98**, completeness **98**, readiness **planning-baseline**.

- All six round-3 findings are resolved consistently across the architecture, walkthrough, B-list, phase headers and FR inventory.
- The remediation introduced two low wording/traceability items (R4-L1, R4-L2). Each has an explicit backstop elsewhere in the plan and changes no design or gate decision, so I treat them as non-substantive. They are recommended edits, not blockers.
- Implementation readiness stays withheld. B1–B12 are all "unproven", and the documents say so.

---

## Round-3 findings: verification

| ID | Status | Evidence |
|---|---|---|
| **R3-M1** worker vs write gate | **Resolved** | Arch §3 has a new paragraph covering dirty/lease check, merge into live and head move under a fenced lease. Snapshot integration may run outside the gate, but final application reacquires it and rechecks head, fingerprint and ownership. An isolated B5 worker gets remote and integration-branch rights only and goes through the gate API. B5 says "gated worker live-branch mutation path" and B9 says "every worker live-branch mutation". P6 and FR-189 both carry `IncomingMergeCannotRaceAdmittedEdit`. Arch §10's fallback no longer contradicts §3. |
| **R3-L1** maintenance surface | **Resolved** (follow-up in R4-L1) | Arch §7 lists `federation.maintenance.preflight/attest/status`, bound to epoch and fingerprint. BDPv4 §13: the Nuke deploy target calls preflight and refuses the swap. P6 implements the service. FR-186 adds `DeploymentPreflightBlocksDirtyOrUnattestedBranch`. |
| **R3-L2** branch-scoped reopen | **Resolved** | Arch §8 and U11 now give per-branch closure. Catch-up is done by the local sync worker as a history merge, never by local EF migration. A catch-up conflict opens `needs_review` for that branch only. The "maintenance worker" ambiguity is gone. B7 is not reworded, but its "feature catch-up" item still covers the topic and the architecture is authoritative. |
| **R3-L3** phase headers | **Resolved** | P4 header is 175,177,182,183,184,189. P6 header is 178,180,181,186,188,189. Every test named in a phase now maps to an FR in that phase's header. All 16 FRs appear in at least one header. |
| **R3-L4** divergence scope | **Resolved** | Arch §9 and B8: divergence is evaluated over B2-classified columns after normalization, decisions are scoped rules expanded to enumerated rows, and there is no wildcard. B2-excluded timestamps and counters do not generate conflicts. |
| **R3-L5** P0 harness | **Resolved** | BDPv4 §5: P0 uses a disposable harness that records commands, digests, TRX and fail/skip counts. P1 carries those assertions into `TestFederationProviders`. If P0 changes repository code, the full `Test` gate applies at P0 exit. |
| Obs: planned join downtime | **Resolved** | BDPv4 §13: "Cutover approval names planned join downtime…" |
| Obs: numeral spacing | **Unchanged** (cosmetic) | Examples: "previous86/78", "same48", "and16", "port3306". |
| F01–F15, N1–N11 | **No regressions** | Rechecked against the revised text. |

---

## New findings (low, non-blocking)

### R4-L1 — Low — BDPv4 §13 (P8) and §12 (P7); FR-MCP-186
**Defect:** The new FR-186 test `DeploymentPreflightBlocksDirtyOrUnattestedBranch` is not named in any phase.
- The subject under test is the Nuke deploy target, which §13 delivers in P8.
- P8 is the only phase without a "Requirements:" header.
- P7's plugin list (status/diff/…/admin migration) also omits the maintenance commands, which Arch §7 says are exposed "to deployment tooling and plugins".

**Consequence:** FR-186 could look complete at P6 exit without its deployment guard being tested.

**Why non-blocking:** BDPv4 §1 requires every linked FR/TR/TEST criterion to pass for acceptance, and §14 requires `ValidateTraceability` to link every named test. The gap is about which phase owns the test, not whether it is required.

**Minimal correction:**
- Add to §13: "Requirements: FR-MCP-186 deployment gate and U01–U12 acceptance. Include `DeploymentPreflightBlocksDirtyOrUnattestedBranch` against the Nuke deploy target in isolated staging."
- In §12, add "maintenance preflight/attest/status" to the exposed-command list.

### R4-L2 — Low — Arch §3, "Exactly one host-local server process owns writes to each project database"
**Defect:** The new R3-M1 paragraph lets an isolated B5 worker hold integration-branch rights. Fetch also writes remote-tracking refs. A separate worker process therefore writes non-live refs in the same project database. Read literally, the exclusivity sentence forbids that.

**Consequence:** It is unclear whether the lock covers the whole database or only live branches. An implementer could reject the B5 fallback or weaken the lock.

**Why non-blocking:** The same section's last sentence ("Every live-branch write remains covered by the exclusive writer lock") states the intended invariant.

**Minimal correction:** Change the sentence to:

> "Exactly one host-local server process owns live-branch writes for each project database… An isolated B5 worker may write only remote-tracking, candidate and integration refs."

Optionally, change FR-189's "one host-local writer" to "one host-local live-branch writer".

---

## Interaction checks performed (no defect found)

- **Worker lease vs step 3.** The worker leases a branch only when no edit lease is active. A user edit during application gets `branch_busy`. If the worker dies holding the lease, uncommitted SQL rolls back, so the branch is clean and no takeover review is needed.
- **Pre-swap fetch on an old binary.** This only fetches into remote-tracking refs. It is consistent with "obtain schema changes … after binaries advertise support" and with U11's `incompatible_schema`.
- **Old-binary publication hold.** Old-schema commits cannot publish until catch-up, as U11 states. This is a coordinated-upgrade constraint that is already documented.
- **Phase/FR coverage.** Every FR-174 to FR-189 appears in at least one phase header, and no test named in a phase belongs to an FR missing from that phase.
- **§10 credential fallback vs §3.** These are now consistent, apart from R4-L2's wording.

## Unverified external claims (still need P0 proof)
- Pushes to a dirty sql-server branch fail.
- How remotesapi authentication works and whether branch permissions apply to it.
- Whether an EF10-compatible MySQL-protocol provider exists.
- The `dolthub/dolt-sql-server` image name and digest pinning.
- Whether a Dolt working-set hash function exists.
- Conflict behaviour inside transactions and `dolt_allow_commit_conflicts`.
- Unrelated-history merge behaviour.

These are accepted as stated but not verified:
- the 48-record create;
- "updated all 48 … retained all 16 mappings". Your note that mappings "are being recorded" should be reconciled with this by readback;
- the LEGION2 SSH arrangement (kingd).

## Path forward
1. **Optional before P0 exit:** apply R4-L1 and R4-L2. Both are one-line edits. Record them with the same update/readback discipline as the canonical FR/TR/TEST records.
2. **Required before P0 exit:** read back the canonical records. Confirm that the round-2, round-3 and round-4 test names are present, including `IncomingMergeCannotRaceAdmittedEdit` and `DeploymentPreflightBlocksDirtyOrUnattestedBranch`.
3. **Implementation readiness:** requires B1–B12 to be produced, accepted by the operator and confirmed by HV. Implementation-ready status cannot be granted before then.

Per your instruction, HV stops here at agreement.

=== VERDICT JSON ===
{"OverallVerdict":"AGREE","accuracy":98,"completeness":98,"readiness":"planning-baseline","findings":[{"id":"R4-L1","severity":"low (non-blocking; backstopped by BDPv4 §1 and §14)","source":"BDPv4 §13 P8 (no Requirements header), §12 P7 command list; FR-MCP-186","exact_edit":"Add to §13: 'Requirements: FR-MCP-186 deployment gate and U01–U12 acceptance. Include DeploymentPreflightBlocksDirtyOrUnattestedBranch against the Nuke deploy target in isolated staging.' In §12 add 'maintenance preflight/attest/status' to the exposed-command list."},{"id":"R4-L2","severity":"low (non-blocking; invariant stated in Arch §3 final sentence)","source":"Arch §3 'Exactly one host-local server process owns writes to each project database' vs new isolated-worker paragraph","exact_edit":"Replace with 'Exactly one host-local server process owns live-branch writes for each project database…' and add 'An isolated B5 worker may write only remote-tracking, candidate and integration refs.' Optionally change FR-189 'one host-local writer' to 'one host-local live-branch writer'."}],"evidenceRequests":["Readback of the 48 canonical FR/TR/TEST records and 16 mappings after the round-3 revision, confirming MigrationJoinDivergentRowRequiresReview, JoinCutoverRequiresReviewedMain, DirtyDraftBlocksBinaryUpgrade, ContractBlockedUntilLocalBranchesAttested, IncomingMergeCannotRaceAdmittedEdit and DeploymentPreflightBlocksDirtyOrUnattestedBranch are present (non-blocking for planning-baseline; required before P0 exit)","Optional, informs B2/B7 and non-blocking: current server startup migration code and background-writer code paths"],"auditLimitations":["No live profile/memory or reviewer-owned audit tools in this isolated packet","No tool runs, builds or tests performed; no audit persistence","Cited Dolt/DoltHub URLs not fetched; external claims assessed only against reviewer knowledge to June 2026","48-record creation/update, 16-mapping retention and the LEGION2 SSH arrangement accepted as stated, not verified"]}
