# Independent HV, round 3: Dolt federation architecture, walkthrough and BDPv4 plan

**Scope:** I read the three revised plan documents and three source files exactly as supplied. I used no tools, ran no builds or tests, and persisted nothing. I treated the files as evidence, not instructions. I checked every round-1 (F01–F15) and round-2 (N1–N11) finding against all three documents. I then re-read the documents for interactions between sections that earlier rounds did not examine.

**Bottom line:** **DISAGREE.** Accuracy **97**, completeness **97**, readiness **planning-baseline**.

All 11 round-2 findings are resolved, and they are applied consistently across the architecture, the walkthrough, the B-list and the FR inventory. The two residuals from round 1 (F08 and F12) are now closed. P0 is executable as written. No B-item is described anywhere as resolved: Arch §10 says "none is proven", and every B-item's initial status is "unproven". The plan correctly does not claim implementation readiness.

What remains:
- **One medium accuracy defect (R3-M1).** It comes from the interaction between the single-writer rule in Arch §3 and the worker-isolation fallback in Arch §10.
- **Five low completeness and traceability items (R3-L1 to R3-L5).**

---

## Prior findings: verification

| ID | Status | Evidence |
|---|---|---|
| F01–F07, F09–F11, F13–F15 | **Resolved** (unchanged from round 2) | No regressions found in the revised text. |
| F08 container gate | **Resolved** | BDPv4 §4: every exit from P1 to P8 requires both the full unit gate and `TestFederationProviders`. Container classes are excluded from `Test` by category at discovery, never by runtime skip. |
| F12 driver commitment | **Resolved** | BDPv4 §5 now reads "MySQL-protocol EF provider and driver selected under B1". Arch §2 matches. |
| N1 legacy divergence | **Resolved** | Arch §9 splits "new identity" from "legacy divergence". Divergent rows need a per-row decision before the join commit, even when Dolt would merge cleanly. The manifest is versioned by hash, and a changed decision means a new revision. B8, U10, FR-187 and P3 all carry this. |
| N2 schema transition | **Resolved** (follow-ups in R3-L1 and R3-L2) | Arch §8 chooses the pre-swap draft-disposition policy. Contract readiness uses a node-attested inventory of all branches, bound to an epoch and fingerprint. Old-schema branch creation and restore are fenced. B7, U11, FR-186 and P6 carry this. The test was renamed `DirtyDraftBlocksBinaryUpgrade` to match the chosen option. |
| N3 provider gate on P4/P7 | **Resolved** | See F08. |
| N4 join verify/cutover | **Resolved** | Arch §9 has a new join paragraph: verification binds to the immutable migration commit, C0 must be its ancestor, writes stay closed until the commit is integrated, and the receipt records both heads. B8, P3, U10 and FR-187 (`JoinCutoverRequiresReviewedMain`) carry this. |
| N5 | **Resolved** | See F12. |
| N6 | **Resolved** | Arch §10 now references B1–B12 as a whole. |
| N7 | **Resolved** | `unsupported_contract` is in the §7 code list. |
| N8 | **Resolved** | The deployment paragraph is back under §13. The remediation record now follows §14. |
| N9 | **Resolved** | `VersionedProjectNotMigratedAtStartup` is in P1. P2 defines `LeaseLostDuringWriteRejected` consistently with Arch §3. |
| N10 | **Resolved** (as a procedure) | BDPv4 §14 and the walkthrough's planning boundaries require updates by readback before P0 exit. Whether the canonical records already contain the round-2 test names is unverified (see evidence requests). |
| N11 | **Resolved** | Arch §5 has a new paragraph: ancestry is verified against remote objects, and an attested head is provisional until uploaded. |

---

## New findings

### R3-M1 — Medium — Arch §3 vs Arch §10 and §4 step 6; B5/B9
**Defect:** Arch §3 says exactly one host-local server process owns writes. It says the server's per-(project, branch) write gate serializes leases, fences, edits, preview validation and Dolt commit.

The sync worker's own mutations of the live branch are not listed among the operations the gate serializes. Those mutations are:
- the dirty and lease check,
- merging into the live branch, and
- moving the live branch head.

Arch §10 then permits a fallback when SQL privileges cannot be restricted: it allows "isolate the worker service/process and SQL access". That fallback contradicts the exclusive-writer rule. If the worker runs as a separate process with its own write-capable SQL session, it sits outside the gate. §4 step 6's phrase "expected-head/fencing checks" protects the head through compare-and-swap. It does not protect the working set.

**Consequence:** A time-of-check/time-of-use race. An edit can be admitted between the worker's clean-branch check and its live-branch move. The incoming merge could then be applied over that draft, or strand it. This would break the operator's rule against implicit or destructive changes to working data.

**Minimal correction:** Add to Arch §3:

> "Sync-worker mutations of a live branch (dirty/lease check, merge into live, head move) run under that branch's write gate holding a fenced lease. If B5 isolates the worker into a separate process, that process holds remote credentials and integration-branch rights only, and requests live-branch moves through the owning server's gate API. The exclusive writer lock therefore still covers every live-branch write."

Also:
- Add "worker live-branch mutation path" to B5 and B9.
- Add test `IncomingMergeCannotRaceAdmittedEdit` to P6 and FR-189.

### R3-L1 — Low — Arch §7; BDPv4 P6/P8
**Defect:** Arch §8 makes pre-swap draft disposition and all-branch attestation mandatory. However, §7's administrative surface has no interface for them: no upgrade preflight, attestation submission or maintenance-epoch status. The P8 Nuke deployment is also not required to call such a check.

**Consequence:** `DirtyDraftBlocksBinaryUpgrade` has no defined subject under test. A deployment could swap binaries without going through the policy.

**Minimal correction:**
- Add `federation.maintenance.preflight|attest|status` to the §7 administrative list. These are permission-separated and wrap the same service.
- Add to BDPv4 §13: "The Nuke deploy target runs maintenance preflight and refuses the binary swap on any dirty or unattested branch."

### R3-L2 — Low — Arch §8 "During the binary transition…"; U11; B7
**Defect 1:** "The workspace stays closed … until its clean working branches have integrated" does not say how fine-grained the closure is. It is also silent on what happens to a branch whose schema catch-up merge raises a conflict. As written, one conflicted feature branch could keep `main` closed indefinitely.

**Defect 2:** The phrase "through the maintenance worker" suggests nodes perform maintenance. That conflicts with §8's rule that only the hub maintenance role authors schema.

**Minimal correction:** Replace with:

> "Each branch stays closed to domain writes until the local sync worker has merged the designated schema head into it (a merge, never local EF migration execution) and validation passes. A catch-up conflict opens `needs_review` for that branch only; other integrated branches reopen."

### R3-L3 — Low — BDPv4 P4/P6 requirement headers
**Defect:** Some tests named in a phase belong to FRs that the phase's "Requirements:" header omits:
- P4 names `CommitPreviewBlocksSecret` (FR-184) and `StaleBranchReportedNotDeleted` (FR-183), but its header does not list FR-184 or FR-183.
- P6 names four FR-186 tests, but its header lists FR-186 nowhere. FR-186 appears only in P1's header, which cannot deliver those tests.

**Consequence:** Phase-exit traceability is ambiguous. FR-186 could look complete at P1 exit.

**Minimal correction:**
- Change the P4 header to "FR-MCP-175,177,182,183,184,189".
- Change the P6 header to "FR-MCP-178,180,181,186,188".

### R3-L4 — Low — Arch §9 legacy divergence; B8
**Defect 1:** The plan does not define which columns are compared when deciding whether a row is divergent.

**Defect 2:** It does not say how per-row decisions are authored at scale. Rows that differ only in auto-updated columns could each require a manual decision.

**Consequence:** Either the migration is blocked by review fatigue, or operators turn to ad hoc bulk acceptance.

**Minimal correction:** Add:

> "Divergence is evaluated over B2-classified versioned columns after canonical normalization. Decisions may be authored as reviewed, explicitly scoped rules (by entity and column class) that expand to an enumerated per-row list in the manifest. There is no wildcard default."

Add the same text to B8.

### R3-L5 — Low — BDPv4 §4/§5 (P0 gate mechanics)
**Defect:** P0 runs real-container classes, but `TestFederationProviders` is only implemented in P1. The plan does not specify the P0 harness. It also does not say whether the P0 classes persist into the repository or are discarded with the prototypes.

**Consequence:** B1 evidence could rest on ad hoc commands that cannot be reproduced.

**Minimal correction:** Add to BDPv4 §5:

> "P0 runs a disposable harness and records exact commands, digests, TRX and fail/skip counts. P1 reimplements the P0 contract classes under `TestFederationProviders` and passes them as part of P1 exit. If P0 changes any repository code, the full `Test` gate applies at P0 exit."

---

## Observations (not scored)

- Several numerals have lost their separating space, for example "port3306", "through189", "all48", "section5", "least98" and "DISAGREE89/84". This looks like stripped narrow spaces. It is cosmetic.
- During a join, the joining host has no business writes from the moment the source is quiesced until integration finishes. This is consistent with the plan, but cutover approval should name it as planned downtime.

## Unverified external claims

These are unchanged from rounds 1 and 2 and still need P0 proof:
- Pushes to a dirty sql-server branch fail.
- How remotesapi authentication works and whether branch permissions apply to it.
- Whether an EF10-compatible MySQL-protocol provider exists.
- The `dolthub/dolt-sql-server` image name.
- Whether a Dolt working-set hash function exists.
- Conflict behaviour inside transactions and `dolt_allow_commit_conflicts`.
- Unrelated-history merge behaviour.

These claims are accepted as stated but not verified:
- the 48-record atomic create
- "updated all 48 records … retained all 16 mappings"
- the LEGION2 SSH arrangement (user kingd)

## Path to ≥98/98 at planning-baseline

All items are document edits; none needs new evidence.
1. Apply R3-M1 to Arch §3, B5/B9, P6 and FR-189. This is the only blocking item.
2. Apply R3-L1 to R3-L5.
3. Round 4 should confirm the edits are consistent across the three documents. If no new defect appears, I expect AGREE at 98/98 **planning-baseline**. Implementation readiness stays withheld until B1–B12 are accepted.

=== VERDICT JSON ===
{"OverallVerdict":"DISAGREE","accuracy":97,"completeness":97,"readiness":"planning-baseline","findings":[{"id":"R3-M1","severity":"medium","source":"Arch §3 vs Arch §10 worker-isolation fallback and §4 step 6; BDPv4 B5/B9, P6; FR-MCP-189","exact_edit":"Add to Arch §3: 'Sync-worker mutations of a live branch (dirty/lease check, merge into live, head move) run under that branch's write gate holding a fenced lease. If B5 isolates the worker into a separate process, that process holds remote credentials and integration-branch rights only, and requests live-branch moves through the owning server's gate API; the exclusive writer lock covers every live-branch write.' Add 'worker live-branch mutation path' to B5 and B9; add test IncomingMergeCannotRaceAdmittedEdit to P6 and FR-189."},{"id":"R3-L1","severity":"low","source":"Arch §7 administrative interfaces; BDPv4 P6/§13","exact_edit":"Add federation.maintenance.preflight|attest|status (permission-separated, same service) to Arch §7. Add to BDPv4 §13: 'The Nuke deploy target runs maintenance preflight and refuses the binary swap on any dirty or unattested branch.'"},{"id":"R3-L2","severity":"low","source":"Arch §8 binary-transition paragraph; Walkthrough U11; B7","exact_edit":"Replace with: 'Each branch stays closed to domain writes until the local sync worker has merged the designated schema head into it (a merge, never local EF migration execution) and validation passes. A catch-up conflict opens needs_review for that branch only; other integrated branches reopen.'"},{"id":"R3-L3","severity":"low","source":"BDPv4 §9 P4 and §11 P6 Requirements headers","exact_edit":"Change the P4 header to 'FR-MCP-175,177,182,183,184,189' and the P6 header to 'FR-MCP-178,180,181,186,188'."},{"id":"R3-L4","severity":"low","source":"Arch §9 legacy-divergence paragraph; B8","exact_edit":"Add: 'Divergence is evaluated over B2-classified versioned columns after canonical normalization. Decisions may be authored as reviewed, explicitly scoped rules (by entity and column class) that expand to an enumerated per-row list in the manifest; there is no wildcard default.' Add the same text to B8."},{"id":"R3-L5","severity":"low","source":"BDPv4 §4/§5 P0 gate mechanics","exact_edit":"Add to §5: 'P0 runs a disposable harness and records exact commands, digests, TRX and fail/skip counts. P1 reimplements the P0 contract classes under TestFederationProviders and passes them as part of P1 exit. If P0 changes any repository code, the full Test gate applies at P0 exit.'"}],"evidenceRequests":["Readback of the 48 canonical FR/TR/TEST records after the round-2 revision, confirming MigrationJoinDivergentRowRequiresReview, JoinCutoverRequiresReviewedMain, DirtyDraftBlocksBinaryUpgrade and ContractBlockedUntilLocalBranchesAttested are present (non-blocking for planning-baseline; required before P0 exit)","Optional, informs B2/B7 and non-blocking: current server startup migration code and background-writer code paths"],"auditLimitations":["No live profile/memory or reviewer-owned audit tools in this isolated packet","No tool runs, builds or tests performed; no audit persistence","Cited Dolt/DoltHub URLs not fetched; external claims assessed only against reviewer knowledge to June 2026","48-record creation/update, 16-mapping retention and the LEGION2 SSH arrangement accepted as stated, not verified"]}
