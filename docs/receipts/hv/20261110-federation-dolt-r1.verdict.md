# Independent HV: Dolt federation architecture, three-system walkthrough and BDPv4 plan

**Scope:** I read the three plan documents and three source files exactly as supplied. I used no tools, ran no builds or tests, and persisted nothing. I treated the files as evidence, not instructions.

**Bottom line:** **DISAGREE.** Accuracy 89, completeness 84, readiness **planning-baseline**. P0 can be executed once the blocking outputs below are adopted. P1 through P8 are not implementation-ready.

The architecture gets most hard choices right:
- It reuses Dolt for history, merge and transfer instead of building its own.
- It keeps the hub's working copy separate from the shared remote.
- It keeps the control and audit store unversioned and never cloned.
- Publication is node-initiated, uses fast-forward only and never force-pushes.
- Delivery is defined by ancestry ("accepted head includes target").
- Merges run on a temporary integration branch.
- Conflict resolution is bound to expected heads.
- Requests never commit drafts implicitly.
- P0 is honestly framed as a feasibility gate.

Three issues could defeat the design if left unaddressed:
- **F01:** Each host migrates its own data into a separate new history, so the histories cannot be merged.
- **F02:** The plan does not say how schema changes reach the other hosts.
- **F03:** Automatic background writers could keep branches permanently dirty.

Several other critical decisions are missing from the P0 blocking outputs, so they look resolved when they are not.

---

## Findings

### F01 — High — Arch §9 `run`; Arch §2, U01, U10; BDPv4 P3, P8
**Defect:** `run` "creates fresh isolated target databases", applies the schema and makes an initial commit. If H, A and B each migrate their own legacy data this way, the result is three unrelated root histories. A Dolt merge needs a common ancestor; I know of no equivalent of Git's `--allow-unrelated-histories`, and this must be verified in P0. Even if such a merge were forced, every overlapping key would become an add/add conflict. U01 and U10 say existing node data is "reconciled explicitly" but never say how.

**Consequence:** The three-system topology cannot be built from the existing lab data.

**Minimal correction:** Define two migration modes:
- **`seed`:** Exactly one host per ProjectId. It creates root C0. It is the only mode that may create a new history.
- **`join`:**
  - The target is a clone of the published C0.
  - Legacy rows are imported, using the identity map, onto `migration/<tag>/<MigrationId>` as one ordinary commit.
  - That branch reaches main through the normal merge, review and publication path.
  - `verify` accounts for source rows as either identical in C0 or mapped into the delta.

Also:
- Add `MigrationJoinTests`.
- Require the join path in P8.
- Add "unrelated-history behaviour" to the P0 merge semantics output.

### F02 — High — Arch §8 schema; U11; BDPv4 P1, P5, P6
**Defects:**
- The plan never says versioned project databases must **not** be migrated at application startup. If each host runs EF `Migrate()`, each one authors its own schema change and `__EFMigrationsHistory` commits, and those diverge.
- "Quiesce writers" cannot be enforced on offline nodes.
- There is no path for unpublished commits made on the old schema, or for feature branches created on it.
- U11 has node A author the upgrade, which conflicts with the "designated schema commit".
- The protocol version between node worker and hub is not negotiated separately from the plugin contract.

**Minimal correction:** Add the following to the architecture:
- Versioned project databases are never migrated at startup.
- Only the hub maintenance role may apply Dolt project migrations. It does so on `maintenance/schema-<v>`, commits schema plus history together, validates and publishes.
- Subscribers apply that schema only through fetch/merge, and only after their binary advertises the capability.
- Versioned migrations must be expand/contract and merge-compatible. Contract steps ship in a later release, after all subscribers have acknowledged.
- After a binary upgrade, old-schema unpublished work and feature branches merge in the schema head through normal review.
- The worker↔hub contract version is negotiated; unsupported versions get `incompatible_schema` or `unsupported_contract`.

U11 should read "H's maintenance role publishes". Add these tests:
- `VersionedProjectNotMigratedAtStartup`
- `OldSchemaUnpublishedCommitMergesAfterUpgrade`
- `FeatureBranchSchemaCatchUp`

### F03 — High — Arch §2 (session logs and derived data are versioned), §3 single editing owner, §4 step 3
**Defect:** Any automatic writer into versioned tables keeps the working set permanently dirty. Examples:
- Per-agent session-log capture
- Access counters or timestamps
- Background ingestion
- Derived indexes stored as tables

**Consequence:** All incoming sync stalls as `waiting_local_work`. Either the single-owner lease is broken, or secondary agents cannot log at all. The packet does not show how session logs are written, so this risk is unverified, but nothing in the plan rules it out.

**Minimal correction:**
- The P0 manifest classifies every **writer** (interactive or automatic) and every auto-updated column, not just every table.
- Automatic writers may not target versioned tables.
- Session-log capture goes to local operational storage. It enters history only through an explicit promotion step that appears in the commit preview.
- Derived indexes live outside the versioned database.

Add tests `BackgroundWritersDoNotDirtyVersionedBranch` and `SecondAgentLoggingNeedsNoLease`.

### F04 — Medium — BDPv4 P1 "chains untouched" vs P2 "project and control contexts"; Arch §2, §8
**Defects:**
- The plan does not say whether the original providers get split contexts. That would mean new chains, which contradicts "untouched".
- It does not say whether standalone, non-federated Dolt uses the full `McpDbContext`.
- Relationships that cross the versioned/local boundary (for example audit→TODO, or session→agent) lose their FKs and joins once they sit in separate databases.

**Correction:** Add P0 blocking output **context topology**:
1. Original providers and standalone Dolt keep the full `McpDbContext` and their existing or baseline chains.
2. Versioned mode uses a Dolt-only project context, plus a control context with additive chains for each of the four providers.
3. The manifest lists every relationship that crosses the boundary, with a replacement soft reference and validator.

Reword P1 to: "existing migrations are not edited; additive migrations only where the manifest requires."

### F05 — Medium — Arch §3 IDs; FR-MCP-182
**Defect:** The ID grammar is unspecified. Changing ID formats affects:
- Parsers
- `ValidateTraceability`
- Plugin regexes
- Sorting
- Human-assigned families, such as `FR-MCP-nnn`

**Correction:** Add a P0/P2 blocking output covering:
- The exact grammar for each entity family
- Which families are system-generated and which are human-assigned
- Updates to validators and plugins
- Display and sort rules

Add test `LegacyIdsRemainValid`. The 48 canonical records created before federation remain legacy IDs.

### F06 — Medium — Arch §3 lease, fingerprint and commit; FR-MCP-189
**Defects:**
- Lease tokens live in a separate control store, so they cannot be checked atomically with the Dolt write.
- The fingerprint check and `DOLT_COMMIT` are not atomic.
- The plan does not say how a retried commit is recognised after a lost response.

**Correction:**
- One host-local server process is the only writer to each project database.
- That process holds a per-(project, branch) write gate. Under the gate it checks the fence, performs the writes, checks the fingerprint and calls `DOLT_COMMIT`.
- For the fingerprint, reuse Dolt's working-set hash, e.g. `DOLT_HASHOF_DB`; verify availability in P0.
- Put the requestId in a Dolt commit trailer, and on retry look it up in `dolt_log`.
- Local DB credentials are server-only.

Add tests `LeaseLostDuringWriteRejected` and `CommitRetryReturnsSameHash`.

### F07 — Medium — Arch §4, §10 transport; FR-MCP-185
**Defects:**
- SQL-initiated `DOLT_PUSH`/`DOLT_FETCH` appears to take remote credentials from the sql-server process environment (unverified). If so, any local SQL session that can call these procedures acts as the node.
- The credential type for the control API is unspecified.
- Having H push via `file://` to a repository served by a live sql-server would be unsafe.

**Correction:** Add P0 output **credential containment**:
- Where the remote and control credentials live
- Which local SQL users may run `DOLT_PUSH/FETCH/REMOTE/BRANCH`: the worker user only, not the editing user
- How credentials are rotated and revoked

H uses the same authenticated endpoint as the nodes and never `file://`. Add tests `LocalAppUserCannotPush` and `RevokedCredentialFailsFetchAndPublish`.

### F08 — Medium — BDPv4 §4
**Defect:** Tests that need containers (Dolt, PostgreSQL, SQL Server) will either:
- skip when Docker is absent, which breaks the zero-skip rule, or
- sit outside `./build.ps1 Test`, which leaves "all provider regressions" unproven at phase exits.

**Correction:**
- Name the integration target and pinned images, e.g. `./build.ps1 IntegrationTest --providers sqlite,postgresql,sqlserver,dolt`.
- An unavailable environment counts as a **failure**, not a skip.
- Exits for P1, P2, P3, P5, P6 and P8 require both the unit suite and that container suite at 0 failures and 0 skips.
- Receipts record image digests.

### F09 — Medium — BDPv4 §5 exit vs Arch §10
**Defect:** Arch §10 blocks on measured storage, latency and capacity, and on final polling and retry values. Neither appears in the P0 exit list, and none of the decisions from F01–F07 do either.

**Correction:** Replace the exit list with numbered blocking outputs. Each has an owner and an approved or unapproved status, and dependent phases cannot start until their items are approved:

- **B1:** Versions, digests and licences
- **B2:** Entity and writer manifest
- **B3:** Context topology
- **B4:** ID grammar
- **B5:** Remote permissions and credential containment
- **B6:** Merge, constraint and conflict semantics, including `dolt_allow_commit_conflicts` and unrelated histories
- **B7:** Schema distribution
- **B8:** Migration seed/join
- **B9:** Write gate and leases
- **B10:** Measured storage, latency and history growth
- **B11:** Polling and retry values
- **B12:** GC and ref retention

### F10 — Medium — Arch §2, §7 history immutability
**Defect:** Secrets that slip through sanitisation, and "forgotten" memories, stay in every node's history permanently. The no-force-push rule leaves no way to purge them.

**Correction:**
- The commit preview runs the existing sanitizer or secret detector over the diff, and a hit blocks the commit.
- Document an incident procedure: rotate the secret, revert, and optionally run an operator-approved, coordinated history rewrite in which all nodes re-clone.
- State that a delete in a versioned project is not erasure.

Add test `CommitPreviewBlocksSecret`.

### F11 — Medium (excess machinery) — Arch §5 SyncRequest
**Defect:** The control table uses edge-triggered rows with generations, coalescing, leases and notification repair. But the plan already defines delivery in level-triggered terms: a node has it when its accepted head includes the target. The extra layer adds failure modes, such as the g/g+1 acknowledgement problem and lost notifications.

**Correction:** Use one state row per subscription:

| Column group | Fields |
|---|---|
| Key | NodeId, ProjectId, Branch |
| Published state | PublishedHead, PublishedGeneration |
| Sync request | SyncRequestedUtc |
| Node report | NodeAcceptedHead, NodeReportedGeneration, Outcome |
| Liveness and fencing | LastSeenUtc, WorkerEpoch |

How it works:
- The publisher updates the published fields when it publishes.
- A poll returns rows where the published generation is ahead of the node's reported generation, or where a sync was requested.
- A report is a monotonic compare-and-set on generation and epoch.
- A node knows it has unpublished work when its local head is not an ancestor of the published head.

If the plan keeps the current design, it should record why.

### F12 — Low — Arch §2
**Defect:** "MySQL EF provider and MySqlConnector" commits to a driver before P0 has chosen one. Oracle's provider uses MySql.Data, which as far as I know is GPLv2 with a FOSS exception (unverified).

**Correction:** "A MySQL-protocol EF provider and its driver selected in P0, subject to licence review."

### F13 — Low — Arch §6
**Defect:** "Pinned against GC" does not say how.

**Correction:** Pin by keeping `review/<conflictId>` and candidate refs, both locally and on the remote. Release them after the resolution receipt. Policy goes in B12.

### F14 — Low — Arch §6, §7 stale branches and drafts
**Correction:**
- `branch.list` and `status` show ahead/behind counts against the published head, last commit time and how long the branch has been in `waiting_local_work`.
- Stale branches above a set threshold are flagged.
- No automatic deletion or rebase.

Add test `StaleBranchReportedNotDeleted`.

### F15 — Low — Arch §9 cutover
**Correction:** State that export from Dolt back to an original provider is out of scope for this release. Cutover approval must record that rolling back after writes means manual reconciliation.

---

## Unverified external claims

I did not fetch any of the cited URLs, and my knowledge ends in June 2026. These claims are plausible but need P0 proof:
- Pushes to a dirty sql-server branch fail.
- Remotesapi authentication and whether branch permissions apply to it.
- Whether an EF10-compatible MySQL provider exists.
- The image name `dolthub/dolt-sql-server`.
- `DOLT_HASHOF_DB`.
- Behaviour of conflicts inside transactions.
- Unrelated-history merge behaviour.

The creation of the 48 records and the SSH arrangement for LEGION2 are accepted as stated; I did not verify either.

## Path to ≥98/98
1. Apply F01–F03 to the architecture, U01, U10, U11 and BDPv4 P3, P6 and P8.
2. Adopt F09 as the P0 exit list and absorb F04–F07 and F10 into it.
3. Fix the gate commands per F08.
4. Resolve or justify F11.
5. Make the text edits for F12–F15.

These are all document edits; none needs new evidence. A re-review should then check consistency across the three documents.

=== VERDICT JSON ===
{"OverallVerdict":"DISAGREE","accuracy":89,"completeness":84,"readiness":"planning-baseline","findings":[{"id":"F01","severity":"high","source":"Arch §9 run, §2; Walkthrough U01/U10; BDPv4 P3/P8","exact_edit":"Split migration into 'seed' (one host per ProjectId; only mode creating root C0) and 'join' (target = clone of published C0; legacy rows imported with identity map onto migration/<tag>/<MigrationId> as an ordinary commit, merged via normal merge/review/publication; verify accounts rows as identical-in-C0 or mapped delta). Add MigrationJoinTests; require join in P8; add unrelated-history behaviour to P0 merge-semantics output."},{"id":"F02","severity":"high","source":"Arch §8; Walkthrough U11; BDPv4 P1/P5/P6","exact_edit":"State versioned project DBs are never migrated at startup; only the hub maintenance role applies Dolt project migrations on maintenance/schema-<v>, commits schema+__EFMigrationsHistory, validates, publishes; subscribers apply only by fetch/merge after advertising capability; versioned migrations are expand/contract and merge-compatible; old-schema unpublished commits and feature branches merge the schema head after binary upgrade via normal review; negotiate worker<->hub contract version. Change U11 to H maintenance role. Add VersionedProjectNotMigratedAtStartup, OldSchemaUnpublishedCommitMergesAfterUpgrade, FeatureBranchSchemaCatchUp."},{"id":"F03","severity":"high","source":"Arch §2, §3, §4 step 3","exact_edit":"P0 manifest classifies every writer (interactive vs automatic) and auto-updated column; automatic writers may not target versioned tables; session-log capture writes local-operational storage and enters history only by explicit promotion shown in commit preview; derived indexes stored outside versioned DB. Add BackgroundWritersDoNotDirtyVersionedBranch and SecondAgentLoggingNeedsNoLease."},{"id":"F04","severity":"medium","source":"BDPv4 P1 vs P2; Arch §2/§8","exact_edit":"Add P0 output 'context topology': original providers and standalone Dolt keep full McpDbContext chains; versioned mode uses a Dolt-only project context plus a control context with additive chains for all four providers; manifest lists cross-boundary relationships with soft-reference validators. Reword P1 to 'existing migrations not edited; additive migrations only where manifest requires'."},{"id":"F05","severity":"medium","source":"Arch §3; FR-MCP-182","exact_edit":"Add blocking output: exact ID grammar per entity family, system-generated vs human-assigned families, validator/ValidateTraceability/plugin regex updates, sort/display rules; test LegacyIdsRemainValid."},{"id":"F06","severity":"medium","source":"Arch §3; FR-MCP-189","exact_edit":"Specify a single host-local writer process per project DB with a per-(project,branch) write gate; fence check, writes, working-set hash check (Dolt-provided, verified in P0) and DOLT_COMMIT occur under the gate; requestId stored as a Dolt commit trailer and looked up in dolt_log on retry; local DB credentials server-only. Tests LeaseLostDuringWriteRejected, CommitRetryReturnsSameHash."},{"id":"F07","severity":"medium","source":"Arch §4/§10; FR-MCP-185","exact_edit":"Add P0 output 'credential containment': location of remote and control-API credentials, which local SQL users may execute DOLT_PUSH/FETCH/REMOTE/BRANCH (worker only), rotation/revocation; H uses the authenticated remote endpoint, never file:// to a live served repo. Tests LocalAppUserCannotPush, RevokedCredentialFailsFetchAndPublish."},{"id":"F08","severity":"medium","source":"BDPv4 §4","exact_edit":"Name the container integration target and pinned images for sqlite/postgresql/sqlserver/dolt; environment unavailability is a failure not a skip; P1/P2/P3/P5/P6/P8 exits require unit suite and that suite at 0 fail/0 skip; receipts record image digests."},{"id":"F09","severity":"medium","source":"BDPv4 §5 exit vs Arch §10","exact_edit":"Replace P0 exit list with numbered blocking outputs B1-B12 (versions/digests/licences; entity+writer manifest; context topology; ID grammar; remote permissions+credential containment; merge/constraint/conflict semantics incl. unrelated histories; schema distribution; migration seed/join; write gate/leases; measured storage/latency/history growth; polling/retry values; GC/ref retention), each with owner and approval status gating dependent phases."},{"id":"F10","severity":"medium","source":"Arch §2/§7","exact_edit":"Commit preview runs existing sanitizer/secret detector over the diff and blocks on hits; document incident procedure (rotate, revert, optional operator-approved coordinated history rewrite with re-clone); state delete is not erasure in versioned projects. Test CommitPreviewBlocksSecret."},{"id":"F11","severity":"medium","source":"Arch §5","exact_edit":"Replace edge-triggered SyncRequest generations/coalescing with one per-subscription state row (NodeId, ProjectId, Branch, PublishedHead, PublishedGeneration, SyncRequestedUtc, NodeAcceptedHead, NodeReportedGeneration, Outcome, LastSeenUtc, WorkerEpoch); poll returns rows where published generation exceeds reported generation or sync requested; report is monotonic CAS on generation and epoch; detect unpublished work by ancestry. Otherwise record the rationale for keeping the current design."},{"id":"F12","severity":"low","source":"Arch §2","exact_edit":"Replace 'MySQL EF provider and MySqlConnector' with 'a MySQL-protocol EF provider and its driver selected in P0, subject to licence review'."},{"id":"F13","severity":"low","source":"Arch §6","exact_edit":"Pin conflict and candidate commits by retaining review/<conflictId> and candidate refs locally and on the remote until the resolution receipt; GC policy in B12."},{"id":"F14","severity":"low","source":"Arch §6/§7","exact_edit":"branch.list and status report ahead/behind vs published head, last commit time and waiting_local_work age; flag stale branches above a threshold; no automatic deletion or rebase. Test StaleBranchReportedNotDeleted."},{"id":"F15","severity":"low","source":"Arch §9","exact_edit":"State that export from Dolt back to an original provider is out of scope; cutover approval records that post-write rollback is manual reconciliation."}],"evidenceRequests":["Current server startup code showing whether Database.Migrate() runs automatically (F02)","Session-log and other background writer code paths and frequencies (F03)","Current ID generators and validators, including ValidateTraceability rules (F05)","Current test infrastructure for PostgreSQL/SQL Server container tests and skip behaviour (F08)"],"auditLimitations":["No live profile/memory or reviewer-owned audit tools in this isolated packet","No tool runs, builds or tests performed; no audit persistence","Cited Dolt/DolthHub URLs not fetched; external claims assessed only against reviewer knowledge to June 2026","Creation of the 48 canonical records and the LEGION2 SSH arrangement accepted as stated, not verified"]}
