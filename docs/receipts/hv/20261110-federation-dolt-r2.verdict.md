# Independent HV, round 2: Dolt federation architecture, walkthrough and BDPv4 plan

**Scope:** I read the three revised plan documents and three source files exactly as supplied. I used no tools, ran no builds or tests, and persisted nothing. I treated the files as evidence, not instructions. I checked each round-1 finding against all three documents for consistency.

**Bottom line:** **DISAGREE.** Accuracy **95**, completeness **94**, readiness **planning-baseline**.

The revision substantively resolves 13 of the 15 round-1 findings:
- Seed/join ancestry is fixed.
- Schema changes have a single author.
- Automatic capture is local, and promotion into history is explicit.
- Context topology is defined.
- There is a single writer with commit-trailer retry.
- Credential containment is defined.
- B1–B12 are the P0 exit outputs, each with an owner and a status.
- The level-triggered subscription row replaces the message queue.

F08 and F12 are only partly applied. Two new medium-severity correctness gaps appear in the new text (N1, N2), along with one gap in my own round-1 F08 correction (N3). P0 is executable as written. P1–P8 are correctly not claimed implementation-ready.

---

## Round-1 findings: verification

| ID | Status | Evidence |
|---|---|---|
| F01 seed/join | **Resolved** (new edge case: N1, N4) | Arch §9 defines the modes, row accounting and "absence ≠ delete"; U10; P3 `MigrationJoinTests`; P8 has one seed per project; B6 covers unrelated histories; B8 |
| F02 schema distribution | **Resolved** (new gap: N2) | Arch §8 bans startup migration, uses `maintenance/schema-<v>`, gates on capability, uses expand/contract and offline catch-up; U11 uses H's maintenance role; B7; tests are in P6 and FR-186 |
| F03 background writers | **Resolved** | Arch §2 covers writer and auto-column classification, local capture, explicit promotion and derived data outside the versioned DB; B2; P2 tests; FR-184 |
| F04 context topology | **Resolved** | Arch §2 paragraph; P1 now says "add migrations only where B3 requires"; B3 |
| F05 ID grammar | **Resolved** | Arch §3; B4; FR-182; `LegacyIdsRemainValid` in P2 |
| F06 write gate and retry | **Resolved** (wording nit folded into N9) | Arch §3 defines the single writer, gate semantics, Dolt working-set hash and trailer lookup; B9; P2 and P4 tests |
| F07 credential containment | **Resolved** | Arch §4 rules out `file://` and has H use the authenticated endpoint; Arch §10 contract; B5; P5 tests; FR-185 |
| F08 container gate | **Partially resolved** → N3 | BDPv4 §4 `TestFederationProviders`: missing containers fail rather than skip, digests are recorded, and exits P1/P2/P3/P5/P6/P8 require it. P4 and P7 do not. |
| F09 blocking outputs | **Resolved** (stale list in Arch §10 → N6) | BDPv4 §5 B1–B12, each with owner, status, evidence and dependent phases |
| F10 secrets in history | **Resolved** | Arch §7 covers the preview detector, the incident procedure and "delete ≠ erasure"; B12; `CommitPreviewBlocksSecret` |
| F11 control-table machinery | **Resolved** | Arch §5 `SyncSubscription` adds `DesiredGeneration` for on-demand requests; epoch/generation CAS; no per-message leases. P5 and FR-179 are consistent. |
| F12 driver commitment | **Partially resolved** → N5 | Arch §2 fixed; BDPv4 §5 still names "MySQL EF provider and MySqlConnector" |
| F13 GC pinning | **Resolved** | Arch §6 retains refs locally and on the remote; B12 |
| F14 stale branches | **Resolved** | Arch §7 status and `branch.list`; P4 test; FR-183 |
| F15 reverse migration | **Resolved** | Arch §9 final paragraph; P8 approval record |

---

## New findings

### N1 — Medium — Arch §9 join; U10; FR-MCP-187; P3
**Defect:** The join branch descends from C0. Suppose a joining host's legacy row has the same mapped identity as a seed row but different content. The join commit then records a one-sided update. Merging it into main fast-forwards, or merges cleanly, and the joining host's value **silently replaces** the seed value. Dolt sees no conflict because only one side changed the cell relative to C0. The manifest's categories (identical / mapped delta / reviewed deletion / local-only) require review for deletions but not for divergent updates. Identity ambiguity blocks the run; divergent content does not.

**Consequence:** Independent legacy edits made on H and LEGION2 before federation bypass the operator's mandatory conflict review.

**Minimal correction:**
- In Arch §9, split "mapped delta" into two categories:
  - **new identity** (an insert)
  - **legacy divergence** (same identity as a seed row, different content)
- Every legacy divergence requires a reviewed per-row decision before the join commit is created: keep seed, take join, or manual value. The decision is recorded in the immutable manifest and audit.
- Alternatively, the join merge must open a `needs_review` item even when Dolt reports a clean merge.
- Add this to B8 and U10.
- Add test `MigrationJoinDivergentRowRequiresReview` to P3 and FR-187.

### N2 — Medium — Arch §8; U11; B7
**Defect 1:** The plan says what happens before a binary upgrade ("advertise support") and after one ("merge the schema head"). It does not say what the new binary does while the local working set is still on the old schema. That window always exists, because step 3 defers the schema merge as `waiting_local_work` whenever a draft is dirty. If the new EF model cannot open, edit or commit an old-schema working set, the result is a deadlock:
- the draft cannot be committed without the new schema, and
- the schema cannot be merged while the draft is dirty.

**Defect 2:** The contract step "waits for every enrolled subscriber/branch … to acknowledge". The hub cannot see a node's **unpublished** local branches, so it cannot verify that every branch has the expand head.

**Minimal correction:** Add the following to Arch §8 and B7:
- Every release that supports versioned schema N must open, edit and commit versioned branches still at N−1. Expand changes are optional to that release's model.
- Alternatively, the upgrade procedure must require committing (or explicitly resolving) drafts before the binary swap. B7 selects one of these options.
- Contract readiness is a node-attested report. It lists every local branch and whether it contains the expand head. A non-empty list blocks the contract step, or else requires de-enrollment with a recovery plan.

Add tests `DirtyDraftSurvivesBinaryUpgradeBeforeSchemaMerge` and `ContractBlockedUntilLocalBranchesAttested` to P6 and FR-186.

### N3 — Medium — BDPv4 §4, P4, P7
**Defect:** P4 delivers explicit Dolt commits, the working-set hash, commit trailers and `CommitRetryReturnsSameHash`. Mocks cannot prove these semantics. Yet the P4 exit does not require `TestFederationProviders`, and nor does the P7 exit (end-to-end status accuracy). This gap came from my own round-1 F08 phase list. In addition, nothing says that container-dependent classes stay out of `./build.ps1 Test`. Without that, the zero-skip unit gate could be satisfied only by excluding those classes at runtime.

**Minimal correction:**
- Change the BDPv4 §4 list to "P1–P8 exits require both the full unit gate and `TestFederationProviders`…".
- Add: "Container-dependent classes are registered only in `TestFederationProviders` and are excluded from `Test` by category at discovery, not by runtime skip."

### N4 — Low — Arch §9 verify/cutover for join mode
**Defect:**
- `cutover` requires verification that matches "exact frozen source and target heads", and `verify` checks "the initial Dolt commit". In join mode, the target head changes when the migration branch is merged or reviewed into main.
- The plan also does not say whether the joining host's workspace reopens before or after that merge.

**Minimal correction:**
- For join mode, verification binds to the immutable `migration/<tag>/<MigrationId>` commit and checks that C0 is its ancestor.
- The workspace stays closed to writes until that commit has been merged into local main through review.
- The cutover receipt records both the migration commit and the resulting local main head.
- Record this sequence in B8.

### N5 — Low — BDPv4 §5, first paragraph (F12 residual)
**Edit:** Replace "the MySQL EF provider and MySqlConnector" with "the MySQL-protocol EF provider and driver selected under B1".

### N6 — Low — Arch §10, "Decisions still needing gate evidence"
**Defect:** The list covers only 5 of the B1–B12 outputs, so it contradicts the authoritative exit list.

**Edit:** "Decisions still needing gate evidence are BDPv4 §5 blocking outputs B1–B12; none is resolved by this document."

### N7 — Low — Arch §7 error codes vs §8
**Defect:** `unsupported_contract` is introduced in §8 but missing from the §7 list of stable codes.

**Edit:** Add it to §7.

### N8 — Low — BDPv4 structure
**Defect:** The "HV remediation record" heading is inserted inside §13. As a result, P8's deployment, enablement and final-acceptance paragraph now falls under the remediation heading.

**Edit:**
- Move the remediation record to after §14.
- Keep "Deploy through Nuke … Stop after validation/reporting" under §13.

### N9 — Low — BDPv4 P1 vs P6; Arch §3 vs test name
**Defect 1:** The startup-migration exclusion is implemented in P1, but its test `VersionedProjectNotMigratedAtStartup` is assigned to P6.

**Edit:** Move that test to P1 and keep the two schema catch-up tests in P6.

**Defect 2:** The test name `LeaseLostDuringWriteRejected` can be read as contradicting Arch §3, under which an admitted transaction completes.

**Edit:** Define the test as "a command admitted after expiry or fence change is rejected, and takeover waits for the in-flight transaction".

### N10 — Low — BDPv4 §14; walkthrough inventory
**Defect:** In this revision, the inventory text gained acceptance criteria and test names that the 48 already-created canonical records may not contain. Examples:
- `LocalAppUserCannotPush`
- `MigrationJoinPreservesCommonAncestor`
- the maintenance-role authorship rule

Whether they do is unverified.

**Edit:** "Apply HV-revised FR/TR/TEST text to the existing canonical records by update, not re-creation, and record the update receipt before P0 exit; `ValidateTraceability` must map every named test."

### N11 — Low — Arch §5 completed report
**Defect:** The plan does not say who verifies that "NodeAcceptedHead includes the target". The hub cannot check the ancestry of a node-local, unpublished merge head.

**Edit:** "The hub verifies ancestry against remote objects. A node-attested, unpublished accepted head is recorded as provisional until it is published or its ancestry is uploaded."

---

## Unverified external claims (unchanged from round 1)

I fetched none of the cited URLs; my knowledge ends in June 2026. The following still need P0 proof:
- Pushes to a dirty sql-server branch fail.
- How remotesapi authentication works and whether branch permissions apply to it.
- Whether an EF10-compatible MySQL-protocol provider exists.
- The image name `dolthub/dolt-sql-server`.
- Whether a Dolt working-set hash function is available (e.g. `DOLT_HASHOF_DB`).
- Conflict behaviour inside transactions and `dolt_allow_commit_conflicts`.
- Unrelated-history merge behaviour.

These claims are accepted as stated but not verified:
- the 48-record atomic create
- the LEGION2 SSH arrangement (user kingd)

## Path to ≥98/98 at planning-baseline

All items below are document edits; none needs new evidence.
1. Apply N1 and N2 to Arch §8/§9, U10/U11, B7/B8, FR-186/187, and the P3/P6 test lists.
2. Apply N3 to BDPv4 §4.
3. Make the editorial and consistency edits N4–N11.

A round-3 check should then:
- confirm that the edits are consistent across all three documents, and
- confirm that no B-item is described anywhere as resolved.

=== VERDICT JSON ===
{"OverallVerdict":"DISAGREE","accuracy":95,"completeness":94,"readiness":"planning-baseline","findings":[{"id":"N1","severity":"medium","source":"Arch §9 join; Walkthrough U10; FR-MCP-187; BDPv4 P3/B8","exact_edit":"Split 'mapped delta' into 'new identity' (insert) and 'legacy divergence' (same identity as a seed row, different content). Every legacy divergence requires a reviewed per-row decision (keep seed / take join / manual value) recorded in the immutable manifest and audit before the join commit is created; otherwise the join merge must open needs_review even when Dolt reports a clean merge. Add to B8 and U10; add test MigrationJoinDivergentRowRequiresReview to P3 and FR-187."},{"id":"N2","severity":"medium","source":"Arch §8; Walkthrough U11; BDPv4 B7/P6; FR-MCP-186","exact_edit":"B7 selects one of: (a) every release supporting versioned schema N opens, edits and commits branches still at N-1, with expand changes optional to the model; or (b) the upgrade procedure requires drafts to be committed or explicitly resolved before the binary swap. Contract readiness is a node-attested report listing every local branch, including unpublished ones, and whether it contains the expand head; any branch lacking it blocks the contract step unless de-enrolled with a recovery plan. Add tests DirtyDraftSurvivesBinaryUpgradeBeforeSchemaMerge and ContractBlockedUntilLocalBranchesAttested."},{"id":"N3","severity":"medium","source":"BDPv4 §4, P4, P7 (residual of F08)","exact_edit":"Change the exit list to 'P1-P8 exits require both the full unit gate and TestFederationProviders at 0 failures/0 skips'. Add: 'Container-dependent classes are registered only in TestFederationProviders and are excluded from Test by category at discovery, not by runtime skip.'"},{"id":"N4","severity":"low","source":"Arch §9 verify/cutover; B8","exact_edit":"For join mode, verification binds to the immutable migration/<tag>/<MigrationId> commit and checks that C0 is its ancestor; the workspace stays closed to writes until that commit has been merged into local main through review; the cutover receipt records both the migration commit and the resulting local main head."},{"id":"N5","severity":"low","source":"BDPv4 §5 first paragraph (residual of F12)","exact_edit":"Replace 'the MySQL EF provider and MySqlConnector' with 'the MySQL-protocol EF provider and driver selected under B1'."},{"id":"N6","severity":"low","source":"Arch §10","exact_edit":"Replace the partial decision list with: 'Decisions still needing gate evidence are BDPv4 §5 blocking outputs B1-B12; none is resolved by this document.'"},{"id":"N7","severity":"low","source":"Arch §7 vs §8","exact_edit":"Add unsupported_contract to the §7 list of stable error codes."},{"id":"N8","severity":"low","source":"BDPv4 §13 / HV remediation record","exact_edit":"Move the 'HV remediation record' section to after §14; keep the deployment, enablement and final-acceptance paragraph under §13 P8."},{"id":"N9","severity":"low","source":"BDPv4 P1/P6; Arch §3","exact_edit":"Move VersionedProjectNotMigratedAtStartup to P1. Define LeaseLostDuringWriteRejected as 'a command admitted after expiry or fence change is rejected, and takeover waits for the in-flight transaction'."},{"id":"N10","severity":"low","source":"BDPv4 §14; Walkthrough inventory","exact_edit":"Add: 'Apply HV-revised FR/TR/TEST text to the existing canonical records by update, not re-creation, and record the update receipt before P0 exit; ValidateTraceability must map every named test.'"},{"id":"N11","severity":"low","source":"Arch §5","exact_edit":"Add: 'The hub verifies ancestry against remote objects. A node-attested, unpublished accepted head is recorded as provisional until it is published or its ancestry is uploaded.'"}],"evidenceRequests":["Current text of the 48 canonical FR/TR/TEST records, to confirm whether the HV-revised criteria are present (N10)","Current server startup code showing whether Database.Migrate() runs automatically (carried from round 1; informs B7)","Session-log and background-writer code paths (carried from round 1; informs B2)"],"auditLimitations":["No live profile/memory or reviewer-owned audit tools in this isolated packet","No tool runs, builds or tests performed; no audit persistence","Cited Dolt/DoltHub URLs not fetched; external claims assessed only against reviewer knowledge to June 2026","48-record creation and LEGION2 SSH arrangement accepted as stated, not verified"]}
