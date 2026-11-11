# Dolt federation: BDPv4 implementation plan

Author: Codex. Date: 2026-11-10. Status: proposed planning baseline; implementation readiness remains gated by Ph0 evidence. Independent review results are retained under docs/receipts/hv. No implementation, package installation, schema migration, data migration or federation activation has occurred.

Architecture authority: [Dolt federation architecture](2026-11-10-federation-dolt-architecture.md). Workflow evidence and requirement inventory: [three-system walkthrough](2026-11-10-federation-three-system-workflows.md). This plan replaces the old forwarding/read-switch execution sequence for versioned federation. Retain the October and earlier November drafts as historical evidence, not competing instructions.

## 1. Scope and acceptance

The logical peers H, A and B must independently edit and commit the same logical project's data, exchange committed histories through the hub, preserve uncommitted work and branches, and review conflicts without pull requests. Add Dolt as a fourth supported database and provide a safe migration tool from SQLite, PostgreSQL and SQL Server. Existing providers and explicit DirectProxy remain supported. The second node is the isolated FED-ATLAS-B test peer; real-host acceptance uses LAB and LEGION, with the three-peer fixture provisioned in Ph0.

Success is demonstrated by every workflow U01-U15, every linked FR/TR/TEST criterion, all supported provider regressions and three-system restart/recovery tests. An empty control table is insufficient proof: compare accepted commit ancestry, project data and branch state, including pending drafts/reviews. Source-code Git, arbitrary file synchronization, PRs, automatic hub election, live cross-provider CDC and concurrent independent schema migrations are outside this release.

## 2. Fastest dependency sequence

Phase identifiers are Ph0 through Ph8; they do not denote priority levels.

1. Ph0: prove Dolt/EF10/provider/remote fit before investing in integration.
2. Ph1: add fourth provider and separate schema migration assembly; preserve original providers.
3. Ph2: split project-versioned and operational storage, branch-scoped access and editing ownership.
4. Ph3: deliver seed migration, audit-backfill preparation and verified all-domain project recovery export; keep targets isolated.
5. Ph4: expose explicit commits, branch/history/diff and local publication intent.
6. Ph5: implement enrollment, guarded remote publication and the hub table/poller.
7. Ph6: implement safe receiving, reviewed merges, join migration and end-to-end crash/restore recovery.
8. Ph7: expose complete client/MCP/REPL/plugin workflows and user-visible status.
9. Ph8: run two-real-host acceptance plus a named three-logical-peer fixture, then prepare a staged lab cutover under DEPLOY HOLD.

Prepare test fixtures, DTO/API documentation and plugin contract fixtures during earlier phases where independent, but do not move unfinished implementation past a Byrd gate. Provider feasibility, not old forwarding fixes, is now the critical path. Reuse existing identity, diagnostics, provider abstractions, audit and typed clients; replace forwarding/command-replay paths only for versioned projects.

## 3. Source-control and donor handling

Create an isolated integration worktree from the current accepted develop when implementation is authorized. Suggested branch `integration/federation-dolt-20261110`; use separate small phase PRs against it. Preserve all existing worktrees and unrelated working-tree edits. Do not create or clean worktrees merely to write this plan.

The prior PR83/P1.5 work is a donor only for applicable security, identity, cross-platform path and regression fixes. Verify each current diff and test before porting. Do not merge old backfill, forwarding, read-switch or custom database synchronization machinery wholesale. C4 p4/*, namespace/analyzer work and historical worktree cleanup are not dependencies. Preserve explicit existing legacy modes through their named regression contracts; they do not authorize LocalProxy forwarding for versioned projects.

## 4. Byrd gate contract

For each small slice, first define the acceptance cases, fixtures and negative controls. Prove tests pass against controlled mocks and fail against deliberately wrong behavior. Then run the same contract against the missing/incorrect real implementation to record red, implement the smallest behavior, and refactor to green. Mocks must not simply return the expected result without testing the orchestration boundary.

Use targeted `./build.ps1 TestGroups --test-groups '<project>:<class>' --test-run-id '<phase-slice>'` after changes. At each phase exit run the entire current-plus-prior unit suite once with `./build.ps1 Test`, requiring zero failures and zero skips. `./build.ps1 Compile` and `./build.ps1 ValidateTraceability` are mandatory gate commands. Add real-container integration tests where a mock cannot prove SQL, migration, locking or remote semantics. Do not broaden/repeat a passing gate without changes or new evidence requiring it. Receipts include SHA/tree state, image/package versions, commands, TRX, fail/skip counts, AC mapping and negative controls.

The proposed test-class names below are implementation targets, not claims they exist today. Register new projects/classes in Nuke discovery as part of their owning phase. Do not add skipped placeholders for later phases; keep deferred coverage in requirements/TODO state. New federation test projects target xUnit v3 and use its native skip (Assert.Skip, Assert.SkipWhen, Assert.SkipUnless); no Xunit.SkippableFact or v2 xunit packages, per MEMORY-BESTPRACTICE-001 section 10.

Retain the operator's complete phase-exit gate on both LAB and LEGION: Nuke Test, MigrationIntegrationTests, the full Build.Tests suite, and ValidateTraceability at the same accepted head. Test currently excludes Build.Tests; do not infer its coverage from a green Test receipt. Ph0 records the complete discovered Build.Tests class inventory and its Nuke TestGroups invocation; every listed class and test must execute, with zero failures and zero skips. MigrationIntegrationTests and the new TestFederationProviders suite are complementary and neither replaces the other. Keep unit, integration and build-suite receipts distinct and run the full set once per final phase head, repeating only after changes or failed gates.

Add Nuke target `TestFederationProviders` with the planned command `./build.ps1 TestFederationProviders --providers sqlite,postgresql,sqlserver,dolt --test-run-id '<phase>-providers'`. It is not an existing command today. Ph0 records exact PostgreSQL/SQL Server/Dolt image versions and digests and the SQLite package version; Ph1 implements the target and refuses missing/unhealthy containers as a gate failure, never a skip. Every Ph1-Ph8 exit requires both the full unit gate and this real-provider/remote suite with zero failures and zero skips. Container-dependent classes are registered only in TestFederationProviders and excluded from Test by category at discovery, never runtime skipping. Receipts include image digests and environment readiness evidence.

## 5. Ph0: feasibility and exact dependency selection

Deliver a version-pinned compatibility receipt and license/dependency record for Dolt and the MySQL-protocol EF provider and driver selected under B1. Use current net10.0/EF10.0.0 as the baseline. Read the dependency compliance rules before additions. Existing Dolt EF samples establish possibility, not compatibility.

Prove on LAB Linux and LEGION native Windows against real databases (containers where appropriate): full current model creation; representative CRUD/LINQ/raw SQL; concurrency tokens; transaction rollback; decimal/date/text/index/FK mappings; migrations; branch connection isolation; explicit SQL commit versus Dolt commit; merge conflicts/constraints; clean and dirty remote pushes; lost responses; authenticated project isolation; inability to bypass guarded publication through raw remote requests. Verify canonical-ref advancement and remote recovery under process death. Compare candidate clean merges against domain validators.

Classes: `DoltProviderCompatibilityTests`, `DoltRemoteAuthorizationTests`, `DoltBranchIsolationTests`, `DoltCommitBoundaryTests`. Red conditions: unsupported mappings/query translation, ordinary SaveChanges creating history, pool branch leakage, unauthorized ref mutation or missing recovery guarantees. Green: all named contracts pass on pinned versions, no ad hoc provider fork or custom merge engine.

Ph0 runs a disposable harness and records exact commands, digests, TRX and fail/skip counts. Ph1 carries the proven Ph0 contract assertions into maintained classes under TestFederationProviders and passes them at Ph1 exit. If Ph0 changes repository code, the full Test gate also applies at Ph0 exit; disposable prototypes do not become an undocumented production dependency.

Ph0 exit consists of the following numbered **blocking outputs**. The implementation owner produces each evidence record; the operator accepts it, with independent HV by an agent family other than the author, assessing substantive accuracy/completeness. Each record stores owner, status (unproven/proven/accepted), evidence paths and dependent phases. Initial status for all B1-B12 is unproven. Every B1-B12 receipt has separate LAB and LEGION evidence rows; Windows is not inferred from Linux. Ph0 verifies the current-scope coverage index and binds each applicable acceptance criterion and workflow to its implementation phase. All must be accepted before Ph1 production integration begins; Ph0 prototypes remain disposable.

1. B1: exact provider/driver/image versions, digests, licenses and all-four-provider fixture versions; LAB /var/lib/dolt mount and native LEGION hosting inventory. Verify actual Windows service name/account/executable/start/recovery policy, data ACLs and child-process lifecycle before accepting the proposed host. If absent or unsuitable, obtain approval for a concrete supported alternative (Ph1-Ph8).
2. B2: every entity, relationship, interactive/automatic writer and auto-updated column; session capture remains operational and only explicit promoted snapshots become versioned (Ph2-Ph8).
3. B3: full standalone McpDbContext versus Dolt-only project/control context topology, additive migration chains and cross-database soft-reference validators (Ph1-Ph3).
4. B4: exact ID grammar per generated/human-assigned family, legacy compatibility, sort/display, parser/plugin/traceability changes and restored allocator behavior (Ph2-Ph8).
5. B5: exact restored Duende 7.4 patch version and matching source-tag/API references, with issuance/introspection/revocation tests on that package (v8 documentation is insufficient); existing IdentityServer client-per-node/owner binding, reference tokens and scopes, gateway introspection/revocation, backend-only Dolt credentials, enforceable remote publication permissions and credential containment, worker-only remote procedures, gated worker live-branch mutation path and rotation/revocation (Ph2/Ph5-Ph8).
6. B6: real merge/constraint/conflict-transaction semantics, retained review evidence, safe conflict persistence including whether `dolt_allow_commit_conflicts` is needed, and unrelated-history behavior. Never enable unresolved conflicts on live project branches (Ph4-Ph6).
7. B7: single-author schema distribution, startup migration exclusion, expand/contract rules, mandatory pre-binary-swap draft disposition, all-local-branch attestation/fencing, offline old-schema commits and feature catch-up, worker protocol negotiation (Ph1/Ph3/Ph5/Ph6/Ph8).
8. B8: seed/join/recovery-export manifest, audit backfill, duplicate detection and permanent aliases, one common root, mandatory legacy-divergence review over B2-classified versioned columns after canonical normalization; reviewed scoped rules may expand to enumerated per-row decisions with no wildcard default. Include identity/deletion accounting, canonical hashes, immutable migration-commit plus final-main verification and closed-until-integrated cutover (Ph3 seed/export, Ph6 join, Ph8 acceptance).
9. B9: single writer ownership, write gate/fence transitions including every worker live-branch mutation, working-set hash API, requestId commit trailers and crash reconciliation (Ph2/Ph4-Ph6).
10. B10: measured fixture-scale storage/latency/history growth, resource budgets and explicit operating limits; no unsupported throughput/SLA claim (Ph1/Ph5/Ph8).
11. B11: polling/retry/stale-branch and15minute/24hour review-age thresholds, subscription-state schema and monotonic generation/epoch semantics (Ph5/Ph7).
12. B12: local/remote candidate/review refs, GC/retention, hub-restore epoch/reconciliation, network loopback isolation, named CA/pins and secret-safe receipts, plus secret-detection and disclosure response (Ph4-Ph8).

Per the operator decision of 2026-11-11 (MEMORY-PREFERENCE-002; architecture section 8), a prerelease or community-fork EF10 provider, patched by this project and contributed upstream as needed, is an accepted dependency for the Dolt project context, so the absence of a GA-maintained provider does not block B1. If no EF10 provider can be made to drive Dolt's required semantics even with reasonable patching, report concrete options for approval; do not downgrade the repository silently or ship a bespoke from-scratch provider without the operator's agreement. This plan is executable through Ph0 but is not implementation-ready for later slices until these evidence-dependent choices are locked.

## 6. Ph1: fourth provider and schema migrations

Requirements: FR-MCP-186. Extend `McpDatabaseProviderKind`, `McpDatabaseProviderFactory`, `McpDbContextFactory`, runtime options/configuration validation, health output and setup tooling with canonical `dolt`. Add `DoltMcpDatabaseProviderStrategy` and `QBrainAi.Storage.DoltMigrations`, with provider-specific snapshot/baseline. Never edit existing migrations; add migrations only where B3 requires. Include provider selections in build/package/admin surfaces. Exclude versioned project databases from automatic startup migration; standalone/control contexts retain their approved migration path.

Classes: `DoltProviderResolutionTests`, `DoltMigrationTests`, `FourProviderContractTests`. Include VersionedProjectNotMigratedAtStartup in this owning phase. First prove unknown-provider rejection, unchanged defaults and distinct migration assembly selection. Real-provider tests cover empty database creation, baseline-to-next upgrade, interrupted startup/retry, idempotent reapplication, schema drift detection and application CRUD after upgrade. Test all original providers for regression. No production data copy in this phase.

## 7. Ph2: project routing, data boundaries and editing ownership

Requirements: FR-MCP-174,176,177,182,184,185,189. Add immutable ProjectId/local-path enrollment mapping, per-project provider selection, project and control contexts, full table classification, branch-scoped DbContext/query/index keys, local ID allocation and fenced edit leases. A versioned project's ordinary requests must never enter LocalProxy forwarding. Preserve DirectProxy behavior for explicit legacy configuration; no ambiguous combination on one project.

Fold MCP-FEDIDENTITY-001 owner binding and user-management requirements into this phase. Audit/outbox storage remains unversioned, but its delivery obligation is hub-bound, not local-only. Define scoped permanent aliases and local provisional IDs; host paths and credentials remain local. Classes: `FederationLocalExecutionTests`, `ProjectBranchContextTests`, `FederationDataBoundaryTests`, `NodeTaggedIdentityTests`, `BranchEditLeaseTests`. Cases include BackgroundWritersDoNotDirtyVersionedBranch, SecondAgentLoggingNeedsNoLease, LegacyIdsRemainValid and LeaseLostDuringWriteRejected. Red: offline request waits for hub, cross-project row visible, secret or branchable audit in clone, duplicate IDs after branch/restart, pooled connection changes branch, or one agent commits another's work. Green: local read-after-write/restart, complete entity/writer classification, no branch/project leakage, safe expired-lease recovery and stable replicated IDs. Provider schema changes introduced here require migrations for each affected supported provider, not Dolt alone.

LeaseLostDuringWriteRejected means admission after expiry/fence change is rejected and takeover waits for an already-admitted transaction; it does not demand aborting a transaction legitimately holding the gate through completion.

## 8. Ph3: seed migration and project recovery export

Requirements: FR-MCP-187, FR-MCP-182/184, FR-MCP-AUDIT-001 and the reconciled backfill contracts. Implement the shared migration planner, immutable source manifest, duplicate-candidate review, permanent alias mapping, seed-only run/resume/verify/status/abort and frozen-head Dolt-to-new-SQLite project export. Audit backfill preparation accounts for every historical TODO/generic audit row; actual hub delivery/receipt binding joins Ph5. Ph3 never invokes publication or reviewed join functionality that does not yet exist.

Classes: MigrationPlannerTests, MigrationResumeTests, MigrationIdentityMappingTests, MigrationVerificationTests, ThreeProviderToDoltMigrationTests, TodoRecoveryExportTests and ProjectRecoveryExportTests. Reuse TEST-MCP-235's exact Given/When/Then cases. SeedPhaseRejectsJoin returns HTTP 501 unsupported_capability with no writes when mode=join is requested. ThreeSourceProviderRoundTrip covers SQLite,PostgreSQL,SQL Server fixtures; both live lab sources are SQLite. DoltTodoExportPreservesNewWrites proves a post-cutover TODO and its child/link/alias graph survive export while retained source bytes remain unchanged. FullProjectRecoveryPreservesAllDomains additionally proves every versioned domain, unpublished branch and dirty working set survives per-branch SQLite export plus the retained Dolt archive; operational stores are backed up at the same quiescence boundary and audit is reconciled. A missing domain or branch blocks routing changes. Same-work/different-ID candidates require review; never rewrite historical text references.

Migration acceptance also covers multi-project separation, soft-deleted rows, decimal/UTC/binary/Unicode edge cases, complete promoted session graphs, hub-bound audit, derived-index rebuild and inert imported operational records. Inject death after target batch commit before acknowledgement, before provider switch and after switch before reopening. Dry-run and failed verification must preserve source bytes and active routing. Ph6 applies the same matrix to joins; source accounting and reviewed-main verification are separate checks.

Exit: disposable seed migrations and verified recovery exports pass all four-provider gates. Keep source SQLite snapshots read-only. No live cutover, join commit, public-ref update or source deletion occurs. Join-specific cases remain planned for Ph6, not skipped tests in Ph3.
## 9. Ph4: explicit history commits and local publication intent

Requirements: FR-MCP-175,177,183,184,189. Implement status/diff/history/commit preview, idempotent explicit commit, branch lifecycle and local unpublished-head scanning. No save hook implicitly creates Dolt history. A hub request cannot auto-commit drafts. Protect preview fingerprints, author provenance and branch edit ownership.

Classes: `FederationCommitTests`, `FederationBranchLifecycleTests`, `PublicationIntentRecoveryTests`. Include CommitRetryReturnsSameHash, CommitPreviewBlocksSecret and StaleBranchReportedNotDeleted. Red: edits trigger sync, commit bundles unauthorized changes, restart loses an unreported commit, or fetched remote commits cause endless republishing. Green: durable explicit commit triggers exactly the needed sync intent, history reconstruction repairs crash windows without duplicate commits, feature publication does not merge main.

## 10. Ph5: hub control table and publication

Requirements: FR-MCP-176,178,179,181,182,184,185. Provision the hub shared remote separately from its local working copy. Implement enrollment and project/branch subscription, candidate upload/validation/acceptance, expected-head publication, SyncSubscription state-table migrations, polling/reporting, durable receipts, worker epochs and remote-head reconciliation. One row per subscription replaces command-message generations/leases/coalescing. Both nodes initiate communication; H's local worker uses the same authenticated gateway. Add existing IdentityServer clients/scopes, audit/batches ingestion with idempotent receipts, human-ID reservation, restore epoch and polling. Test AuditReachesHub, AuditRetryIdempotent, SimultaneousHumanIdReservation, SqlPortUnreachableAcrossLink and HubRestoreFromOlderBackup. No native remote endpoint is exposed to node credentials.

Classes: `FederationEnrollmentTests`, `FederationPublicationTests`, `FederationControlTableTests`, `FederationPollingTests`. Include LocalAppUserCannotPush, RevokedCredentialFailsFetchAndPublish and OldWorkerEpochCannotReportSuccess. Red: node can publish unauthorized branch/project, hub drafts block publication, polling marks success, reporting generation g loses desired g+1, missing state update strands a published head, or simultaneous publishers overwrite history. Green: all controls work against real remotes and all control-store providers; no force pushes; stale heads yield deterministic retries.

## 11. Ph6: incoming integration, review, join migration and recovery

Requirements: FR-MCP-178,180,181,186,187,188,189. Implement temporary integration branches, clean-merge validation, dirty-branch deferral, gated live-branch application, conflict review with head-bound resolution, recovery pins/receipts, maintenance preflight/attestation/status and derived-state refresh. Incoming data must not re-execute business commands or external effects. Include IncomingMergeCannotRaceAdmittedEdit, covering an isolated worker that must use the owning server's gate API.

Classes: `FederationMergeReviewTests`, `FederationDirtyWorkingSetTests`, `FederationRecoveryTests`, `FederationEffectIsolationTests`. Cases include all-three concurrent nonconflicting commits, same-cell conflict, delete/update, invalid clean merge, missing ancestry, stale review, hub/node restart at each state transition, disconnect during upload, lost acknowledgement, concurrent resolution and unavailable derived indexes. Include OldSchemaUnpublishedCommitMergesAfterUpgrade, FeatureBranchSchemaCatchUp, DirtyDraftBlocksBinaryUpgrade and ContractBlockedUntilLocalBranchesAttested. The dirty-draft case proves the attempted swap is blocked and preserves the draft before schema merge. Preserve valid live data and all history; never report synchronized on conflict, dirty deferment or stale observation. Test older-schema node quarantine separately from network failure.

After Ph4 commit/branch and Ph5 publication services exist, implement join mode here: clone C0, stage duplicate/legacy-divergence decisions, import J1, review/merge into M1, validate audit-backfill receipts and verify both J1 source accounting and M1 domain state before cutover readiness. Classes MigrationJoinTests and MigrationCutoverTests own MigrationJoinPreservesCommonAncestor, MigrationJoinDivergentRowRequiresReview and JoinCutoverRequiresReviewedMain. Ph3 did not claim these dependencies were available.

Add ReviewAgeVisible, UnrelatedPublicationContinues, SafeTodoMergeRules and AuditBackfillVerified. Ph5 first owns HubRestoreFromOlderBackup for epoch/remote/control recovery; Ph6 extends that same contract through local merge, review and audit resend. It does not create a second owner or postpone the Ph5 case. Permit safe identical/independent-cell merges only under the explicit TODO policy. Preserve unresolved branch history and permit unrelated project/branch publication. The single writer sentence applies to live branches; worker integration refs use their narrower rights and every live move uses the owner's gate.
## 12. Ph7: server and plugin workflows

Requirements: FR-MCP-183 and every workflow's plugin surface. Add typed DTO/client methods, REST/MCP/REPL adapters and equivalent audited plugin wrappers for installed first-party agents. Inventory actual plugin repositories and versions at phase entry; do not edit another plugin through the wrong agent identity. Expose capabilities, project/branch context, status/diff/history/commit/sync, branches, review, audit delivery status, identity enrollment/owner management, human-ID reservation, aliases, admin migration/export commands and maintenance.preflight/attest/status as defined by the architecture and TRs. Surface review/wait ages immediately, with15minute warning and24hour escalation in the operator inbox; no external messaging is sent automatically.

Classes: `FederationClientContractTests`, `FederationMcpToolTests`, `FederationReplContractTests`, `FederationPluginConformanceTests`. Shared contract fixtures run against each plugin, including enrollment diagnostics, provenance/collision diagnostics, least-privilege denial, resume/status/cancel without false rollback, synchronization versus external effects, and editing-owner request/release/alternate-branch controls. Each obligation is explicit in the corresponding FR/TR/TEST, not inferred from a generic status command. Verify accurate local-committed/published/received status, unsupported-version errors, reconnect context revalidation, no implicit commits in turn hooks, real agent identity, normalized errors and safe cancellation. Human walkthrough uses the same service contracts as agents; no hidden raw-SQL path.

## 13. Ph8: two-host acceptance, three-peer fixture and held cutover

Build from the accepted integration SHA. Real-host acceptance requires LAB-OMARCHY(10.42.0.214/Linux) and PAYTON-LEGION2(10.42.0.143/Windows), both starting from retained SQLite copies. It does not depend on an unprovisioned third machine. Separately, Nuke provisions an ephemeral three-logical-peer fixture named FED-ATLAS-H, FED-ATLAS-A and FED-ATLAS-B with isolated databases, loopback dynamic ports and disposable roots under TestResults/{runId}; Ph0 proves that provisioning path on both OSs. These are test processes, never discovered hosts or production enrollment. Execute U01-U15; use H/A for real cross-link tests and the fixture for three-writer interleavings. Collect full requirements/AC/UC coverage and compare observed heads/rows/errors to the canonical GWT contracts.

Ph8 owns the isolated real-host execution of DeploymentPreflightBlocksDirtyOrUnattestedBranch, WindowsWriterLockAndRestart, HubRestoreFromOlderBackup, SqlPortUnreachableAcrossLink, UnknownCertificatePinRejected and ReceiptsAndLogsContainNoCredentials. Their implementation contracts remain owned by their earlier phase; HubRestoreFromOlderBackup first passes in Ph5 and is extended in Ph6. All B1-B12 evidence must include LEGION. An absent third physical host is not a blocker; absent fixture evidence is a failed gate.

Ph8 uses disposable project data, isolated databases/ports and dedicated fixture roots on both real hosts. Existing unrelated lab work is not a substitute for isolated staging. For every applicable control, publication/history, audit and IdentityServer path, partition and reconnect the path independently; also exercise simultaneous A/B partitions in the three-peer fixture. Restart each application/worker, Dolt process, gateway, IdentityServer and control/audit store at the documented commit/publication/report boundaries. Compare captured branch heads, ancestor sets, refs, row values, retained drafts, audit delivery/receipts, secret exclusions and control outcomes before/after each fault. Record the matrix by host, component/path, fault, expected state, observed state and receipt. Network isolation must not disrupt unrelated projects.

Collect all-four-provider regression and all-three-source migration receipts, reviewed join and all-domain recovery-export receipts, plus an actual isolated restore of the complete backup set. A missing component/path/provider row fails acceptance; neither unit mocks nor an empty queue substitute for these receipts. RealHostAcceptanceMatrix covers this requirement. Review-age, cancellation and plugin diagnostics use the same isolated scenarios. Live cutover remains a separate action under DEPLOY HOLD.
Before live cutover, inventory and back up both current lab hosts' provider databases, identity/configuration, legacy queue/fanout/conflicts and unpublished data. Run the migration planner on every participating legacy project. Designate one seed per ProjectId; every other source uses the clone-plus-delta join path, preserving common ancestry. Resolve identity collisions and pending legacy operations explicitly; do not drop them or run both legacy replay and Dolt publication for the same project. Verify target snapshots before switching. Keep federation disabled until this gate passes and the concrete cutover is approved under the operator's deployment policy. Approval records the verified all-domain/all-branch recovery manifest and operational backup/restore receipts. Cut over one host first, observe a 48-hour soak with successful offline/reconnect, audit delivery and recovery-export checks, then authorize the second host.

DEPLOY HOLD remains active until the operator explicitly lifts it for this exact rollout. Plan approval, historical 98/98 or test success is not that release. Deploy through Nuke using the Linux container/native Windows package choices, protected configuration and persistent storage. Its deploy target calls maintenance preflight and refuses the binary swap on any dirty or unattested branch; it consumes the maintenance epoch/readiness receipt. SSH to LEGION2 as kingd using the configured authorized key. Enable only accepted projects; verify local operations before first publication, then enable subscriptions and polling. Cutover approval names planned join downtime from source quiescence through reviewed integration. Final acceptance includes one hub-authored commit and one commit from each node, both branch and conflict workflows, offline commits, and verified source retention. Stop after validation/reporting; do not extend into PR features or unrelated worktree cleanup.

## 14. Traceability, review and readiness

The [current-scope coverage index](2026-11-10-federation-reconciliation.md) identifies applicable requirements recovered from the gap check. The workflow inventory maps the 16 release contracts to phases, U01-U15 and 99 named acceptance cases. [Current implementation contracts and acceptance](2026-11-10-federation-current-contracts.md) includes the canonical schemas, endpoints, error codes and full Given/When/Then outcomes once in the PDF. Test names alone are not acceptance coverage. Keep existing IDs and update their current text rather than creating competing requirements. Export canonical requirements through the supported plugin, preserving unrelated working-tree changes; never regenerate an entire root file from a partial federation slice. Each substantive review amendment is persisted through the MCP plugin, read back and exported before phase exit. Historical source copies and unrelated obligations are not required plan reading or phase-exit work.

Before a phase exits, every applicable AC must have executable evidence and every implemented workflow must satisfy its preconditions, success and failure paths. ValidateTraceability checks the FR/TR/TEST links. All amended criteria remain pending until the implementation passes the defined gates.

Independent HV by an agent family other than the author targets accuracy and completeness of at least 98 each on the exact current architecture, plan, workflow and contract packet. This revision awaits that review and Ph0 feasibility evidence. Record source/PDF hashes and findings separately from the plan. DEPLOY HOLD requires explicit operator release after acceptance; neither plan approval nor a review score releases it.
### Review history and evidence boundaries

Historical Claude rounds R1-R4 scored accuracy/completeness 89/84, 95/94, 97/97 and 98/98 (AGREE on the original six-file packet). Full request/response/verdict receipts are retained under docs/receipts/hv/20261110-federation-dolt-r{1..4}.*. R1-R3 tracked F01-F15, N1-N11, R3-M1 and R3-L1-L5; the receipt findings, not the scores alone, establish what was reviewed. R4-L1 is now covered by Ph7 maintenance surfaces and Ph8 deployment preflight; R4-L2 by the live-branch writer rule and restricted integration-worker rights. Those are author dispositions, not a fresh reviewer acceptance.

The historical reviewer saw an isolated approved packet, lacked live operator decisions, builds/tests and browsing, and did not review the compiled PDF. The recorded runtime model was claude-opus-5-5; high effort was requested but not independently verified. Review infrastructure incidents are recorded separately from substantive scores. Historical final hashes/readback and the original 45-state visualization receipt remain evidence of that old packet only. The original 48-record create and first-update receipts were successful; later reconciliation amended 62 LAB records and restored six, and amended 20 LEGION records. These historical persistence counts are not current acceptance counts.

The supplied revision audit compared the original 76-page PDF with the 48-page revision: 15 findings addressed, two partial, plus lost obligations and new inconsistencies. Its report and this revision's item-by-item dispositions are retained as receipts. The earlier source-to-PDF checks could not prove preservation of obligations removed from the source. This revision requires both a retention/disposition check and PDF inclusion checks. The exact original 39-page PDF remains unavailable; no comparison to that artifact is claimed.

Future independent HV must receive the exact current source packet, rendered PDF and embedded-source hashes, the applicable operator decisions and audit dispositions, with full secret-safe request/response receipts. It must separately assess restored content and contradictions; old 98/98 cannot transfer to a new packet. No HV is launched or passed merely by editing this plan.

## 15. Amendment 2026-11-11: Task-Workflow breakdown (Plan, Task Groups, Tasks)

Status: additive amendment requested by Payton on 2026-11-11 (t613u, t617u, t619u). It restates sections 5 to 13 as one Plan, ordered Task Groups and Tasks using the model in [Architecture: TODO Plans, Tasks, Task Groups, and Session Work Dispatch](https://github.com/sharpninja/McpServer/blob/worktree-session-20261009/docs/architecture/Task-Workflow-Architecture.md) (PLAN-TODOTASK-001) as if that system were already built. Nothing above is removed or changed; where this section and sections 1 to 14 differ, sections 1 to 14 remain the authority for scope and acceptance. No implementation, HV run, commit or deployment is implied by this amendment.

### 15.1 Model applied

- Plan: the Dolt federation plan is one Plan, a TODO specialized with ItemKind=plan: `PLAN-FEDDOLT-001`, Reference `docs/plans/2026-11-10-federation-dolt-bdpv4.md`.
- Task: a unit of work and a child of the Plan TODO, with FR/AC coverage (FrAcSelection) and TR constraints. States are Planning, Ready, Started, Success and Failure. Failure is remediated back to Planning with a recorded reason; Success is immutable. A Task may span several handler calls and session turns.
- Task Group: a TODO-defined group that orders its member Tasks by group ordinal. Group status is calculated, never stored, in this order: Failing (any member Failure), Blocked (any non-Success member has an incomplete dependency), Completed (nonempty and every member Success), Unstarted (empty or all Planning/Ready), otherwise Incomplete.
- Dependencies: typed Task or Group targets. A Group target is complete only when that group calculates Completed. A Task starts (implementation) only when every direct prerequisite is complete and its group barrier (architecture 3.5.11) is Open; dependencies do not gate design and mock work. Cross-group order is expressed by the first Tasks of each group depending on the prior Group.
- Dispatch: groups are delegated to an agent session and dispatched FIFO by receipt order; NextGroupTask starts at most one Started Task per group, skipping Success, Failure, Planning and blocked members.
- AC identity: AC rows are never deleted and recreated to implement an edit; FR/AC citations below use the canonical AC names from [current contracts](2026-11-10-federation-current-contracts.md).
- Byrd gate: each phase keeps its exit gate from section 4 as an explicit Task immediately before the group HV gate.
- Source of both group gates: [Task-Workflow-Architecture.md](https://github.com/sharpninja/McpServer/blob/worktree-session-20261009/docs/architecture/Task-Workflow-Architecture.md) sections 3.5.11, 3.5.12 and 8.6 (amendment 2026-11-11, uncommitted on branch worktree-session-20261009). This plan applies those architecture rules; it does not define a Federation-only gate.

### 15.2 Group gates from the architecture

1. Design and mock-green barrier, Task-Workflow-Architecture.md section 3.5.11 GroupImplementationBarrierPolicy (operator rule 2026-11-11): within a Task Group no Task may start implementation (Ready to Started) until every member is designed and its contract unit tests are green against mocks with a negative control; Planning to Ready requires that current-version evidence. Design and mock work may proceed in parallel and is not gated by dependencies; implementation may not. The barrier is calculated (Open when every member is Ready, Started or Success) and is separate from and earlier than the HV gate. Mock suites run when a member's design is complete; the full suite runs once at slice or phase exit.
2. Group completion HV gate, Task-Workflow-Architecture.md section 3.5.12 GroupCompletionHvGatePolicy (operator rule 2026-11-11): the last member of every group is its HV gate Task, depending on all other members; reviewer from a different agent family and model family; AGREE at max(configured threshold, 0.98) for both scores (98/98); the group calculates Completed only after the gate succeeds, so nothing depending on the group starts earlier; DISAGREE fails the gate and findings against Success work become correction Tasks (correctsTaskId) before the gate, then the gate is remediated and re-run. Delivery order: section 8.6.

### 15.3 Dispatch order

1. `GRP-FEDPH0-001` Ph0 feasibility and exact dependency selection (plan section 5). Prerequisite group: none. Calculated status at filing: Blocked.
2. `GRP-FEDPH1-001` Ph1 fourth provider and schema migrations (plan section 6). Prerequisite group: GRP-FEDPH0-001. Calculated status at filing: Blocked.
3. `GRP-FEDPH2-001` Ph2 project routing, data boundaries and editing ownership (plan section 7). Prerequisite group: GRP-FEDPH1-001. Calculated status at filing: Blocked.
4. `GRP-FEDPH3-001` Ph3 seed migration and project recovery export (plan section 8). Prerequisite group: GRP-FEDPH2-001. Calculated status at filing: Blocked.
5. `GRP-FEDPH4-001` Ph4 explicit history commits and local publication intent (plan section 9). Prerequisite group: GRP-FEDPH3-001. Calculated status at filing: Blocked.
6. `GRP-FEDPH5-001` Ph5 hub control table and publication (plan section 10). Prerequisite group: GRP-FEDPH4-001. Calculated status at filing: Blocked.
7. `GRP-FEDPH6-001` Ph6 incoming integration, review and recovery (plan section 11). Prerequisite group: GRP-FEDPH5-001. Calculated status at filing: Blocked.
8. `GRP-FEDPH6-002` Ph6 join migration and cutover readiness (plan section 11). Prerequisite group: GRP-FEDPH6-001. Calculated status at filing: Blocked.
9. `GRP-FEDPH7-001` Ph7 server and plugin workflows (plan section 12). Prerequisite group: GRP-FEDPH6-002. Calculated status at filing: Blocked.
10. `GRP-FEDPH8-001` Ph8 two-host acceptance and three-peer fixture (plan section 13). Prerequisite group: GRP-FEDPH7-001. Calculated status at filing: Blocked.
11. `GRP-FEDCUT-001` Held live cutover (requires explicit DEPLOY HOLD release) (plan section 13). Prerequisite group: GRP-FEDPH8-001. Calculated status at filing: Blocked.

Initial states at filing: every Task is Planning, because no Task yet has design and mock-green evidence (3.5.11); every group's implementation barrier is Closed. Design and mock work may begin now for any member of any group, in parallel; no member may start implementation until its own group's barrier opens and its dependencies are complete. Calculated group status at filing is Blocked for every group, because each has a non-Success member with an incomplete dependency (3.5.4). No Task is Ready, Started, Success or Failure.

### 15.4 Groups and Tasks

#### GRP-FEDPH0-001: Ph0 feasibility and exact dependency selection

Group key `ph0-feasibility`; phase Ph0; plan section 5. Prove Dolt/EF10/provider/remote fit and produce accepted B1-B12 blocking outputs before any production integration. Disposable harness only.

- `TASK-FEDPH0-001` (group ordinal 1): B1: pin Dolt, EF10 MySQL-protocol provider, driver and container images; host inventory. Initial state: Planning. Depends on: none.
  - FR/AC: FR-MCP-186: FourProvidersResolve.
  - TR: TR-MCP-FEDERATION-013. TEST: TEST-MCP-234.
- `TASK-FEDPH0-002` (group ordinal 2): Dolt compatibility harness: model, CRUD/LINQ, migrations, branch isolation, commit boundary. Initial state: Planning. Depends on: TASK-FEDPH0-001.
  - FR/AC: FR-MCP-186: DoltBaselineAndUpgrade; FR-MCP-177: PooledConnectionDoesNotLeakBranch; FR-MCP-175: SaveDoesNotPublish.
  - TR: TR-MCP-FEDERATION-002, TR-MCP-FEDERATION-004, TR-MCP-FEDERATION-013. TEST: TEST-MCP-223, TEST-MCP-225, TEST-MCP-234. Classes: DoltProviderCompatibilityTests, DoltBranchIsolationTests, DoltCommitBoundaryTests.
- `TASK-FEDPH0-003` (group ordinal 3): Dolt remote authorization and guarded publication proof. Initial state: Planning. Depends on: TASK-FEDPH0-001.
  - FR/AC: FR-MCP-185: RawRemoteCannotBypassPublicationPolicy.
  - TR: TR-MCP-FEDERATION-012. TEST: TEST-MCP-233. Classes: DoltRemoteAuthorizationTests.
- `TASK-FEDPH0-004` (group ordinal 4): B2-B4: writer/entity classification, context topology, node-tagged ID grammar. Initial state: Planning. Depends on: none.
  - FR/AC: FR-MCP-184: BackgroundWritersDoNotDirtyVersionedBranch; FR-MCP-182: TwoNodesCreateDistinctIds, LegacyIdsRemainValid.
  - TR: TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-011. TEST: TEST-MCP-230, TEST-MCP-232.
- `TASK-FEDPH0-005` (group ordinal 5): B5: identity evidence on restored Duende 7.4 (absorbs MCP-FEDIDENTITY-001 design inputs). Initial state: Planning. Depends on: none.
  - FR/AC: FR-MCP-185: RevokedNodeCannotPublish, LocalAppUserCannotPush.
  - TR: TR-MCP-FEDERATION-012. TEST: TEST-MCP-233.
  - Linked TODO: MCP-FEDIDENTITY-001.
- `TASK-FEDPH0-006` (group ordinal 6): B6-B9: merge/conflict semantics, schema distribution, migration manifest, write gate. Initial state: Planning. Depends on: TASK-FEDPH0-002.
  - FR/AC: FR-MCP-180: SameCellConflictNeedsReview; FR-MCP-186: ContractBlockedUntilLocalBranchesAttested; FR-MCP-187: SeedPhaseRejectsJoin; FR-MCP-189: LeaseLostDuringWriteRejected.
  - TR: TR-MCP-FEDERATION-007, TR-MCP-FEDERATION-013, TR-MCP-FEDERATION-014, TR-MCP-FEDERATION-016. TEST: TEST-MCP-228, TEST-MCP-234, TEST-MCP-235, TEST-MCP-237.
- `TASK-FEDPH0-007` (group ordinal 7): B10-B12: operating limits, polling thresholds, refs/GC/restore epoch and secret safety. Initial state: Planning. Depends on: TASK-FEDPH0-002, TASK-FEDPH0-003.
  - FR/AC: FR-MCP-179: HubRestoreFromOlderBackup; FR-MCP-184: ReceiptsAndLogsContainNoCredentials; FR-MCP-185: UnknownCertificatePinRejected.
  - TR: TR-MCP-FEDERATION-006, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-012. TEST: TEST-MCP-227, TEST-MCP-232, TEST-MCP-233.
- `TASK-FEDPH0-008` (group ordinal 8): Ph0 exit: B1-B12 operator acceptance, coverage binding, Build.Tests inventory, three-peer fixture proof. Initial state: Planning. Depends on: TASK-FEDPH0-001, TASK-FEDPH0-002, TASK-FEDPH0-003, TASK-FEDPH0-004, TASK-FEDPH0-005, TASK-FEDPH0-006, TASK-FEDPH0-007.
  - FR/AC: FR-MCP-186: RealHostAcceptanceMatrix.
  - TR: TR-MCP-FEDERATION-013. TEST: TEST-MCP-234.
- `TASK-FEDPH0-009` (group ordinal 9): Ph0 group HV gate: Ph0 feasibility and exact dependency selection. Initial state: Planning. Depends on: TASK-FEDPH0-008.
  - FR/AC: FR-MCP-186: FourProvidersResolve, DoltBaselineAndUpgrade, ContractBlockedUntilLocalBranchesAttested, RealHostAcceptanceMatrix; FR-MCP-177: PooledConnectionDoesNotLeakBranch; FR-MCP-175: SaveDoesNotPublish; FR-MCP-185: RawRemoteCannotBypassPublicationPolicy, RevokedNodeCannotPublish, LocalAppUserCannotPush, UnknownCertificatePinRejected; FR-MCP-184: BackgroundWritersDoNotDirtyVersionedBranch, ReceiptsAndLogsContainNoCredentials; FR-MCP-182: TwoNodesCreateDistinctIds, LegacyIdsRemainValid; FR-MCP-180: SameCellConflictNeedsReview; FR-MCP-187: SeedPhaseRejectsJoin; FR-MCP-189: LeaseLostDuringWriteRejected; FR-MCP-179: HubRestoreFromOlderBackup.
  - TR: TR-MCP-FEDERATION-002, TR-MCP-FEDERATION-004, TR-MCP-FEDERATION-006, TR-MCP-FEDERATION-007, TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-012, TR-MCP-FEDERATION-013, TR-MCP-FEDERATION-014, TR-MCP-FEDERATION-016. TEST: TEST-MCP-223, TEST-MCP-225, TEST-MCP-227, TEST-MCP-228, TEST-MCP-230, TEST-MCP-232, TEST-MCP-233, TEST-MCP-234, TEST-MCP-235, TEST-MCP-237.

#### GRP-FEDPH1-001: Ph1 fourth provider and schema migrations

Group key `ph1-provider`; phase Ph1; plan section 6. Add canonical dolt provider and QBrainAi.Storage.DoltMigrations; preserve SQLite, PostgreSQL and SQL Server.

- `TASK-FEDPH1-001` (group ordinal 1): Canonical dolt provider kind, factory, options, health and setup tooling. Initial state: Planning. Depends on: GRP-FEDPH0-001.
  - FR/AC: FR-MCP-186: FourProvidersResolve, UnsupportedSchemaRejected.
  - TR: TR-MCP-FEDERATION-013. TEST: TEST-MCP-234. Classes: DoltProviderResolutionTests, FourProviderContractTests.
- `TASK-FEDPH1-002` (group ordinal 2): QBrainAi.Storage.DoltMigrations baseline and startup exclusion for versioned projects. Initial state: Planning. Depends on: TASK-FEDPH1-001.
  - FR/AC: FR-MCP-186: DoltBaselineAndUpgrade, OriginalProviderMigrationsUnchanged, VersionedProjectNotMigratedAtStartup.
  - TR: TR-MCP-FEDERATION-013. TEST: TEST-MCP-234. Classes: DoltMigrationTests.
- `TASK-FEDPH1-003` (group ordinal 3): Nuke TestFederationProviders target carrying the Ph0 contracts. Initial state: Planning. Depends on: GRP-FEDPH0-001.
  - FR/AC: FR-MCP-186: FourProvidersResolve.
  - TR: TR-MCP-FEDERATION-013. TEST: TEST-MCP-234. Classes: DoltProviderCompatibilityTests, DoltRemoteAuthorizationTests, DoltBranchIsolationTests, DoltCommitBoundaryTests.
- `TASK-FEDPH1-004` (group ordinal 4): Ph1 exit gate on LAB and LEGION (provider and migration slice). Initial state: Planning. Depends on: TASK-FEDPH1-002, TASK-FEDPH1-003.
  - FR/AC: FR-MCP-186: FourProvidersResolve, UnsupportedSchemaRejected, DoltBaselineAndUpgrade, OriginalProviderMigrationsUnchanged, VersionedProjectNotMigratedAtStartup.
  - TR: TR-MCP-FEDERATION-013. TEST: TEST-MCP-234.
- `TASK-FEDPH1-005` (group ordinal 5): Ph1 group HV gate: Ph1 fourth provider and schema migrations. Initial state: Planning. Depends on: TASK-FEDPH1-004.
  - FR/AC: FR-MCP-186: FourProvidersResolve, UnsupportedSchemaRejected, DoltBaselineAndUpgrade, OriginalProviderMigrationsUnchanged, VersionedProjectNotMigratedAtStartup.
  - TR: TR-MCP-FEDERATION-013. TEST: TEST-MCP-234.

#### GRP-FEDPH2-001: Ph2 project routing, data boundaries and editing ownership

Group key `ph2-routing`; phase Ph2; plan section 7. Autonomous local execution on every node including the hub; branch-scoped contexts; node-tagged IDs; fenced edit leases; identity owner binding.

- `TASK-FEDPH2-001` (group ordinal 1): Immutable ProjectId enrollment mapping, per-project provider, local execution without LocalProxy. Initial state: Planning. Depends on: GRP-FEDPH1-001.
  - FR/AC: FR-MCP-174: NodeWorksWithHubUnavailable, HubUsesSameLocalExecution, OfflineLocalReadAfterWrite; FR-MCP-176: DifferentHostPathsSameProject.
  - TR: TR-MCP-FEDERATION-001, TR-MCP-FEDERATION-003. TEST: TEST-MCP-222, TEST-MCP-224. Classes: FederationLocalExecutionTests.
- `TASK-FEDPH2-002` (group ordinal 2): Full table classification and branch-scoped DbContext/query/index keys. Initial state: Planning. Depends on: TASK-FEDPH2-001.
  - FR/AC: FR-MCP-177: PooledConnectionDoesNotLeakBranch; FR-MCP-184: CloneOmitsSecrets, BackgroundWritersDoNotDirtyVersionedBranch, SecondAgentLoggingNeedsNoLease.
  - TR: TR-MCP-FEDERATION-004, TR-MCP-FEDERATION-011. TEST: TEST-MCP-225, TEST-MCP-232. Classes: ProjectBranchContextTests, FederationDataBoundaryTests.
- `TASK-FEDPH2-003` (group ordinal 3): Two-character node-tagged local ID allocation and scoped permanent aliases. Initial state: Planning. Depends on: TASK-FEDPH2-002.
  - FR/AC: FR-MCP-182: TwoNodesCreateDistinctIds, BranchRevertDoesNotReuseSequence, LegacyIdsRemainValid.
  - TR: TR-MCP-FEDERATION-009. TEST: TEST-MCP-230. Classes: NodeTaggedIdentityTests.
- `TASK-FEDPH2-004` (group ordinal 4): Fenced branch edit leases. Initial state: Planning. Depends on: TASK-FEDPH2-002.
  - FR/AC: FR-MCP-189: CommitCannotCaptureOtherAgentDraft, ExpiredLeaseFenced, LeaseLostDuringWriteRejected.
  - TR: TR-MCP-FEDERATION-016. TEST: TEST-MCP-237. Classes: BranchEditLeaseTests.
- `TASK-FEDPH2-005` (group ordinal 5): Identity owner binding and host user management (folds MCP-FEDIDENTITY-001). Initial state: Planning. Depends on: TASK-FEDPH2-001.
  - FR/AC: FR-MCP-185: OfflineLocalAuthorizationStillApplies, ForeignProjectCloneDenied.
  - TR: TR-MCP-FEDERATION-012. TEST: TEST-MCP-233. Classes: FederationLocalExecutionTests.
  - Linked TODO: MCP-FEDIDENTITY-001.
- `TASK-FEDPH2-006` (group ordinal 6): Hub-bound audit/outbox obligation and federation effect boundary. Initial state: Planning. Depends on: TASK-FEDPH2-002.
  - FR/AC: FR-MCP-184: AuditReachesHub; FR-MCP-188: FederationBypassesQuadBrainKeyserver, ImportedOperationalRowsRemainInert.
  - TR: TR-MCP-AUDIT-004, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-015. TEST: TEST-MCP-232, TEST-MCP-236. Classes: FederationDataBoundaryTests.
- `TASK-FEDPH2-007` (group ordinal 7): Ph2 exit gate on LAB and LEGION (routing and boundary slice). Initial state: Planning. Depends on: TASK-FEDPH2-001, TASK-FEDPH2-002, TASK-FEDPH2-003, TASK-FEDPH2-004, TASK-FEDPH2-005, TASK-FEDPH2-006.
  - FR/AC: FR-MCP-174: NodeWorksWithHubUnavailable, HubUsesSameLocalExecution, OfflineLocalReadAfterWrite; FR-MCP-176: DifferentHostPathsSameProject; FR-MCP-177: PooledConnectionDoesNotLeakBranch; FR-MCP-184: CloneOmitsSecrets, BackgroundWritersDoNotDirtyVersionedBranch, SecondAgentLoggingNeedsNoLease, AuditReachesHub; FR-MCP-182: TwoNodesCreateDistinctIds, BranchRevertDoesNotReuseSequence, LegacyIdsRemainValid; FR-MCP-189: CommitCannotCaptureOtherAgentDraft, ExpiredLeaseFenced, LeaseLostDuringWriteRejected; FR-MCP-185: OfflineLocalAuthorizationStillApplies, ForeignProjectCloneDenied; FR-MCP-188: FederationBypassesQuadBrainKeyserver, ImportedOperationalRowsRemainInert.
  - TR: TR-MCP-FEDERATION-001, TR-MCP-FEDERATION-003, TR-MCP-FEDERATION-004, TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-012, TR-MCP-FEDERATION-015, TR-MCP-FEDERATION-016. TEST: TEST-MCP-222, TEST-MCP-224, TEST-MCP-225, TEST-MCP-230, TEST-MCP-232, TEST-MCP-233, TEST-MCP-236, TEST-MCP-237.
- `TASK-FEDPH2-008` (group ordinal 8): Ph2 group HV gate: Ph2 project routing, data boundaries and editing ownership. Initial state: Planning. Depends on: TASK-FEDPH2-007.
  - FR/AC: FR-MCP-174: NodeWorksWithHubUnavailable, HubUsesSameLocalExecution, OfflineLocalReadAfterWrite; FR-MCP-176: DifferentHostPathsSameProject; FR-MCP-177: PooledConnectionDoesNotLeakBranch; FR-MCP-184: CloneOmitsSecrets, BackgroundWritersDoNotDirtyVersionedBranch, SecondAgentLoggingNeedsNoLease, AuditReachesHub; FR-MCP-182: TwoNodesCreateDistinctIds, BranchRevertDoesNotReuseSequence, LegacyIdsRemainValid; FR-MCP-189: CommitCannotCaptureOtherAgentDraft, ExpiredLeaseFenced, LeaseLostDuringWriteRejected; FR-MCP-185: OfflineLocalAuthorizationStillApplies, ForeignProjectCloneDenied; FR-MCP-188: FederationBypassesQuadBrainKeyserver, ImportedOperationalRowsRemainInert.
  - TR: TR-MCP-FEDERATION-001, TR-MCP-FEDERATION-003, TR-MCP-FEDERATION-004, TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-012, TR-MCP-FEDERATION-015, TR-MCP-FEDERATION-016. TEST: TEST-MCP-222, TEST-MCP-224, TEST-MCP-225, TEST-MCP-230, TEST-MCP-232, TEST-MCP-233, TEST-MCP-236, TEST-MCP-237.

#### GRP-FEDPH3-001: Ph3 seed migration and project recovery export

Group key `ph3-seed`; phase Ph3; plan section 8. Shared migration planner, seed-only runs and verified all-domain recovery export; targets isolated; no publication or join.

- `TASK-FEDPH3-001` (group ordinal 1): Migration planner, immutable source manifest, duplicate-candidate review and permanent aliases. Initial state: Planning. Depends on: GRP-FEDPH2-001.
  - FR/AC: FR-MCP-187: ChangedSnapshotRejected; FR-MCP-182: LegacyCollisionPreservesReferences, DifferentIdsSameTodoDetected.
  - TR: TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-014. TEST: TEST-MCP-230, TEST-MCP-235. Classes: MigrationPlannerTests, MigrationIdentityMappingTests.
- `TASK-FEDPH3-002` (group ordinal 2): Seed run/resume/verify/status/abort from SQLite, PostgreSQL and SQL Server. Initial state: Planning. Depends on: TASK-FEDPH3-001.
  - FR/AC: FR-MCP-187: ThreeSourceProviderRoundTrip, ResumeAfterBatchCommit, FailedVerifyCannotCutover, SourceRetainedAfterCutover, SeedPhaseRejectsJoin.
  - TR: TR-MCP-FEDERATION-014. TEST: TEST-MCP-235. Classes: MigrationResumeTests, MigrationVerificationTests, ThreeProviderToDoltMigrationTests.
- `TASK-FEDPH3-003` (group ordinal 3): Audit backfill preparation for every historical TODO/generic audit row. Initial state: Planning. Depends on: TASK-FEDPH3-001.
  - FR/AC: FR-MCP-184: FullProjectSessionGraphPreserved.
  - TR: TR-MCP-FEDERATION-011. TEST: TEST-MCP-232. Classes: MigrationVerificationTests.
- `TASK-FEDPH3-004` (group ordinal 4): Frozen-head Dolt-to-SQLite project recovery export. Initial state: Planning. Depends on: TASK-FEDPH3-002.
  - FR/AC: FR-MCP-187: DoltTodoExportPreservesNewWrites, FullProjectRecoveryPreservesAllDomains.
  - TR: TR-MCP-FEDERATION-014. TEST: TEST-MCP-235. Classes: TodoRecoveryExportTests, ProjectRecoveryExportTests.
- `TASK-FEDPH3-005` (group ordinal 5): Ph3 exit gate on LAB and LEGION (seed and export slice). Initial state: Planning. Depends on: TASK-FEDPH3-001, TASK-FEDPH3-002, TASK-FEDPH3-003, TASK-FEDPH3-004.
  - FR/AC: FR-MCP-187: ChangedSnapshotRejected, ThreeSourceProviderRoundTrip, ResumeAfterBatchCommit, FailedVerifyCannotCutover, SourceRetainedAfterCutover, SeedPhaseRejectsJoin, DoltTodoExportPreservesNewWrites, FullProjectRecoveryPreservesAllDomains; FR-MCP-182: LegacyCollisionPreservesReferences, DifferentIdsSameTodoDetected; FR-MCP-184: FullProjectSessionGraphPreserved.
  - TR: TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-014. TEST: TEST-MCP-230, TEST-MCP-232, TEST-MCP-235.
- `TASK-FEDPH3-006` (group ordinal 6): Ph3 group HV gate: Ph3 seed migration and project recovery export. Initial state: Planning. Depends on: TASK-FEDPH3-005.
  - FR/AC: FR-MCP-187: ChangedSnapshotRejected, ThreeSourceProviderRoundTrip, ResumeAfterBatchCommit, FailedVerifyCannotCutover, SourceRetainedAfterCutover, SeedPhaseRejectsJoin, DoltTodoExportPreservesNewWrites, FullProjectRecoveryPreservesAllDomains; FR-MCP-182: LegacyCollisionPreservesReferences, DifferentIdsSameTodoDetected; FR-MCP-184: FullProjectSessionGraphPreserved.
  - TR: TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-014. TEST: TEST-MCP-230, TEST-MCP-232, TEST-MCP-235.

#### GRP-FEDPH4-001: Ph4 explicit history commits and local publication intent

Group key `ph4-commits`; phase Ph4; plan section 9. Commits, not saves, create history and trigger sync; branch lifecycle; publication intent recovery.

- `TASK-FEDPH4-001` (group ordinal 1): Status/diff/history/commit preview and idempotent explicit commit. Initial state: Planning. Depends on: GRP-FEDPH3-001.
  - FR/AC: FR-MCP-175: SaveDoesNotPublish, VersionCommitTriggersSync, HubRequestDoesNotCommitDraft; FR-MCP-189: CommitRetryReturnsSameHash, PreviewChangeRequiresReview; FR-MCP-184: CommitPreviewBlocksSecret.
  - TR: TR-MCP-FEDERATION-002, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-016. TEST: TEST-MCP-223, TEST-MCP-232, TEST-MCP-237. Classes: FederationCommitTests.
- `TASK-FEDPH4-002` (group ordinal 2): Branch lifecycle across server context. Initial state: Planning. Depends on: TASK-FEDPH4-001.
  - FR/AC: FR-MCP-177: FeaturePublicationLeavesMainUnchanged, BranchMergeRequiresExpectedHeads; FR-MCP-183: StaleBranchReportedNotDeleted.
  - TR: TR-MCP-FEDERATION-004, TR-MCP-FEDERATION-010. TEST: TEST-MCP-225, TEST-MCP-231. Classes: FederationBranchLifecycleTests.
- `TASK-FEDPH4-003` (group ordinal 3): Local unpublished-head scanning and publication intent recovery. Initial state: Planning. Depends on: TASK-FEDPH4-001.
  - FR/AC: FR-MCP-175: OfflineCommitDiscoveredAfterRestart.
  - TR: TR-MCP-FEDERATION-002. TEST: TEST-MCP-223. Classes: PublicationIntentRecoveryTests.
- `TASK-FEDPH4-004` (group ordinal 4): Ph4 exit gate on LAB and LEGION (commit and branch slice). Initial state: Planning. Depends on: TASK-FEDPH4-001, TASK-FEDPH4-002, TASK-FEDPH4-003.
  - FR/AC: FR-MCP-175: SaveDoesNotPublish, VersionCommitTriggersSync, HubRequestDoesNotCommitDraft, OfflineCommitDiscoveredAfterRestart; FR-MCP-189: CommitRetryReturnsSameHash, PreviewChangeRequiresReview; FR-MCP-184: CommitPreviewBlocksSecret; FR-MCP-177: FeaturePublicationLeavesMainUnchanged, BranchMergeRequiresExpectedHeads; FR-MCP-183: StaleBranchReportedNotDeleted.
  - TR: TR-MCP-FEDERATION-002, TR-MCP-FEDERATION-004, TR-MCP-FEDERATION-010, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-016. TEST: TEST-MCP-223, TEST-MCP-225, TEST-MCP-231, TEST-MCP-232, TEST-MCP-237.
- `TASK-FEDPH4-005` (group ordinal 5): Ph4 group HV gate: Ph4 explicit history commits and local publication intent. Initial state: Planning. Depends on: TASK-FEDPH4-004.
  - FR/AC: FR-MCP-175: SaveDoesNotPublish, VersionCommitTriggersSync, HubRequestDoesNotCommitDraft, OfflineCommitDiscoveredAfterRestart; FR-MCP-189: CommitRetryReturnsSameHash, PreviewChangeRequiresReview; FR-MCP-184: CommitPreviewBlocksSecret; FR-MCP-177: FeaturePublicationLeavesMainUnchanged, BranchMergeRequiresExpectedHeads; FR-MCP-183: StaleBranchReportedNotDeleted.
  - TR: TR-MCP-FEDERATION-002, TR-MCP-FEDERATION-004, TR-MCP-FEDERATION-010, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-016. TEST: TEST-MCP-223, TEST-MCP-225, TEST-MCP-231, TEST-MCP-232, TEST-MCP-237.

#### GRP-FEDPH5-001: Ph5 hub control table and publication

Group key `ph5-hub`; phase Ph5; plan section 10. Hub shared remote, enrollment, guarded publication, SyncSubscription control table, polling, audit ingestion and human-ID reservation.

- `TASK-FEDPH5-001` (group ordinal 1): Hub shared remote provisioning, node enrollment and project/branch subscription. Initial state: Planning. Depends on: GRP-FEDPH4-001.
  - FR/AC: FR-MCP-176: CloneRefusesNonemptyTarget, UnauthorizedProjectDenied; FR-MCP-185: ForeignProjectCloneDenied, RevokedNodeCannotPublish.
  - TR: TR-MCP-FEDERATION-003, TR-MCP-FEDERATION-012. TEST: TEST-MCP-224, TEST-MCP-233. Classes: FederationEnrollmentTests.
- `TASK-FEDPH5-002` (group ordinal 2): IdentityServer clients/scopes, gateway introspection and credential containment. Initial state: Planning. Depends on: TASK-FEDPH5-001.
  - FR/AC: FR-MCP-185: LocalAppUserCannotPush, RevokedCredentialFailsFetchAndPublish, SqlPortUnreachableAcrossLink, UnknownCertificatePinRejected, RawRemoteCannotBypassPublicationPolicy.
  - TR: TR-MCP-FEDERATION-012. TEST: TEST-MCP-233. Classes: FederationEnrollmentTests, FederationPublicationTests.
- `TASK-FEDPH5-003` (group ordinal 3): Candidate upload, validation and expected-head guarded publication. Initial state: Planning. Depends on: TASK-FEDPH5-002.
  - FR/AC: FR-MCP-178: HubDraftDoesNotBlockRemote, ConcurrentPublishPreservesBothHistories; FR-MCP-181: LostPushAckDoesNotDuplicateCommit.
  - TR: TR-MCP-FEDERATION-005, TR-MCP-FEDERATION-008. TEST: TEST-MCP-226, TEST-MCP-229. Classes: FederationPublicationTests.
- `TASK-FEDPH5-004` (group ordinal 4): SyncSubscription control table, polling/reporting, worker epochs and restore epoch. Initial state: Planning. Depends on: TASK-FEDPH5-003.
  - FR/AC: FR-MCP-179: PollReadDoesNotAcknowledge, AckOldGenerationPreservesNewRequest, MissedNotificationReconciles, HubRestoreFromOlderBackup; FR-MCP-181: OldWorkerEpochCannotReportSuccess.
  - TR: TR-MCP-FEDERATION-006, TR-MCP-FEDERATION-008. TEST: TEST-MCP-227, TEST-MCP-229. Classes: FederationControlTableTests, FederationPollingTests.
- `TASK-FEDPH5-005` (group ordinal 5): Audit batch ingestion with idempotent receipts and human-ID reservation. Initial state: Planning. Depends on: TASK-FEDPH5-002.
  - FR/AC: FR-MCP-184: AuditReachesHub, AuditRetryIdempotent; FR-MCP-182: SimultaneousHumanIdReservation, OfflineHumanIdPublicationDenied.
  - TR: TR-MCP-AUDIT-004, TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-011. TEST: TEST-MCP-230, TEST-MCP-232. Classes: FederationControlTableTests.
- `TASK-FEDPH5-006` (group ordinal 6): Ph5 exit gate on LAB and LEGION (hub and publication slice). Initial state: Planning. Depends on: TASK-FEDPH5-001, TASK-FEDPH5-002, TASK-FEDPH5-003, TASK-FEDPH5-004, TASK-FEDPH5-005.
  - FR/AC: FR-MCP-176: CloneRefusesNonemptyTarget, UnauthorizedProjectDenied; FR-MCP-185: ForeignProjectCloneDenied, RevokedNodeCannotPublish, LocalAppUserCannotPush, RevokedCredentialFailsFetchAndPublish, SqlPortUnreachableAcrossLink, UnknownCertificatePinRejected, RawRemoteCannotBypassPublicationPolicy; FR-MCP-178: HubDraftDoesNotBlockRemote, ConcurrentPublishPreservesBothHistories; FR-MCP-181: LostPushAckDoesNotDuplicateCommit, OldWorkerEpochCannotReportSuccess; FR-MCP-179: PollReadDoesNotAcknowledge, AckOldGenerationPreservesNewRequest, MissedNotificationReconciles, HubRestoreFromOlderBackup; FR-MCP-184: AuditReachesHub, AuditRetryIdempotent; FR-MCP-182: SimultaneousHumanIdReservation, OfflineHumanIdPublicationDenied.
  - TR: TR-MCP-FEDERATION-003, TR-MCP-FEDERATION-005, TR-MCP-FEDERATION-006, TR-MCP-FEDERATION-008, TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-012. TEST: TEST-MCP-224, TEST-MCP-226, TEST-MCP-227, TEST-MCP-229, TEST-MCP-230, TEST-MCP-232, TEST-MCP-233.
- `TASK-FEDPH5-007` (group ordinal 7): Ph5 group HV gate: Ph5 hub control table and publication. Initial state: Planning. Depends on: TASK-FEDPH5-006.
  - FR/AC: FR-MCP-176: CloneRefusesNonemptyTarget, UnauthorizedProjectDenied; FR-MCP-185: ForeignProjectCloneDenied, RevokedNodeCannotPublish, LocalAppUserCannotPush, RevokedCredentialFailsFetchAndPublish, SqlPortUnreachableAcrossLink, UnknownCertificatePinRejected, RawRemoteCannotBypassPublicationPolicy; FR-MCP-178: HubDraftDoesNotBlockRemote, ConcurrentPublishPreservesBothHistories; FR-MCP-181: LostPushAckDoesNotDuplicateCommit, OldWorkerEpochCannotReportSuccess; FR-MCP-179: PollReadDoesNotAcknowledge, AckOldGenerationPreservesNewRequest, MissedNotificationReconciles, HubRestoreFromOlderBackup; FR-MCP-184: AuditReachesHub, AuditRetryIdempotent; FR-MCP-182: SimultaneousHumanIdReservation, OfflineHumanIdPublicationDenied.
  - TR: TR-MCP-FEDERATION-003, TR-MCP-FEDERATION-005, TR-MCP-FEDERATION-006, TR-MCP-FEDERATION-008, TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-012. TEST: TEST-MCP-224, TEST-MCP-226, TEST-MCP-227, TEST-MCP-229, TEST-MCP-230, TEST-MCP-232, TEST-MCP-233.

#### GRP-FEDPH6-001: Ph6 incoming integration, review and recovery

Group key `ph6-integration`; phase Ph6; plan section 11. Temporary integration branches, clean-merge validation, reviewed conflicts, dirty deferral, schema catch-up, recovery and effect isolation.

- `TASK-FEDPH6-001` (group ordinal 1): Temporary integration branches, clean-merge validation and gated live-branch apply. Initial state: Planning. Depends on: GRP-FEDPH5-001.
  - FR/AC: FR-MCP-178: ThreeWriterCleanConvergence; FR-MCP-180: CleanMergeCanFailDomainValidation; FR-MCP-189: IncomingMergeCannotRaceAdmittedEdit.
  - TR: TR-MCP-FEDERATION-005, TR-MCP-FEDERATION-007, TR-MCP-FEDERATION-016. TEST: TEST-MCP-226, TEST-MCP-228, TEST-MCP-237. Classes: FederationMergeReviewTests.
- `TASK-FEDPH6-002` (group ordinal 2): Head-bound conflict review. Initial state: Planning. Depends on: TASK-FEDPH6-001.
  - FR/AC: FR-MCP-180: SameCellConflictNeedsReview, DeleteUpdateNeedsReview, StaleReviewRejected, ReviewAgeVisible, UnrelatedPublicationContinues, SafeTodoMergeRules.
  - TR: TR-MCP-FEDERATION-007. TEST: TEST-MCP-228. Classes: FederationMergeReviewTests.
- `TASK-FEDPH6-003` (group ordinal 3): Dirty working-set deferral and schema catch-up. Initial state: Planning. Depends on: TASK-FEDPH6-001.
  - FR/AC: FR-MCP-181: DirtyBranchDefersApply, IncompatibleSchemaDoesNotApply; FR-MCP-186: OldSchemaUnpublishedCommitMergesAfterUpgrade, FeatureBranchSchemaCatchUp, DirtyDraftBlocksBinaryUpgrade, ContractBlockedUntilLocalBranchesAttested.
  - TR: TR-MCP-FEDERATION-008, TR-MCP-FEDERATION-013. TEST: TEST-MCP-229, TEST-MCP-234. Classes: FederationDirtyWorkingSetTests.
- `TASK-FEDPH6-004` (group ordinal 4): Recovery pins/receipts, maintenance preflight/attest/status, derived-state refresh. Initial state: Planning. Depends on: TASK-FEDPH6-002, TASK-FEDPH6-003.
  - FR/AC: FR-MCP-181: LostPushAckDoesNotDuplicateCommit; FR-MCP-179: HubRestoreFromOlderBackup; FR-MCP-184: AcceptedHeadRebuildsBranchIndex.
  - TR: TR-MCP-FEDERATION-006, TR-MCP-FEDERATION-008, TR-MCP-FEDERATION-011. TEST: TEST-MCP-227, TEST-MCP-229, TEST-MCP-232. Classes: FederationRecoveryTests.
- `TASK-FEDPH6-005` (group ordinal 5): Imported state does not repeat external effects. Initial state: Planning. Depends on: TASK-FEDPH6-001.
  - FR/AC: FR-MCP-188: ImportDoesNotSendExternalAction, RetryDoesNotRepeatEffect, ImportedOperationalRowsRemainInert.
  - TR: TR-MCP-FEDERATION-015. TEST: TEST-MCP-236. Classes: FederationEffectIsolationTests.
- `TASK-FEDPH6-006` (group ordinal 6): Ph6 integration slice gate (targeted classes plus TestFederationProviders). Initial state: Planning. Depends on: TASK-FEDPH6-001, TASK-FEDPH6-002, TASK-FEDPH6-003, TASK-FEDPH6-004, TASK-FEDPH6-005.
  - FR/AC: FR-MCP-180: SameCellConflictNeedsReview; FR-MCP-181: DirtyBranchDefersApply.
  - TR: TR-MCP-FEDERATION-007, TR-MCP-FEDERATION-008. TEST: TEST-MCP-228, TEST-MCP-229.
- `TASK-FEDPH6-007` (group ordinal 7): Ph6 group HV gate: Ph6 incoming integration, review and recovery. Initial state: Planning. Depends on: TASK-FEDPH6-006.
  - FR/AC: FR-MCP-178: ThreeWriterCleanConvergence; FR-MCP-180: CleanMergeCanFailDomainValidation, SameCellConflictNeedsReview, DeleteUpdateNeedsReview, StaleReviewRejected, ReviewAgeVisible, UnrelatedPublicationContinues, SafeTodoMergeRules; FR-MCP-189: IncomingMergeCannotRaceAdmittedEdit; FR-MCP-181: DirtyBranchDefersApply, IncompatibleSchemaDoesNotApply, LostPushAckDoesNotDuplicateCommit; FR-MCP-186: OldSchemaUnpublishedCommitMergesAfterUpgrade, FeatureBranchSchemaCatchUp, DirtyDraftBlocksBinaryUpgrade, ContractBlockedUntilLocalBranchesAttested; FR-MCP-179: HubRestoreFromOlderBackup; FR-MCP-184: AcceptedHeadRebuildsBranchIndex; FR-MCP-188: ImportDoesNotSendExternalAction, RetryDoesNotRepeatEffect, ImportedOperationalRowsRemainInert.
  - TR: TR-MCP-FEDERATION-005, TR-MCP-FEDERATION-006, TR-MCP-FEDERATION-007, TR-MCP-FEDERATION-008, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-013, TR-MCP-FEDERATION-015, TR-MCP-FEDERATION-016. TEST: TEST-MCP-226, TEST-MCP-227, TEST-MCP-228, TEST-MCP-229, TEST-MCP-232, TEST-MCP-234, TEST-MCP-236, TEST-MCP-237.

#### GRP-FEDPH6-002: Ph6 join migration and cutover readiness

Group key `ph6-join`; phase Ph6; plan section 11. Clone-plus-delta join path using Ph4 commits, Ph5 publication and Ph6 review.

- `TASK-FEDPH6J-001` (group ordinal 1): Join mode: clone C0, stage decisions, import J1, review/merge to M1. Initial state: Planning. Depends on: GRP-FEDPH6-001.
  - FR/AC: FR-MCP-187: MigrationJoinPreservesCommonAncestor, MigrationJoinDivergentRowRequiresReview, JoinCutoverRequiresReviewedMain.
  - TR: TR-MCP-FEDERATION-014. TEST: TEST-MCP-235. Classes: MigrationJoinTests, MigrationCutoverTests.
- `TASK-FEDPH6J-002` (group ordinal 2): Audit backfill verification. Initial state: Planning. Depends on: TASK-FEDPH6J-001.
  - FR/AC: FR-MCP-184: AuditBackfillVerified.
  - TR: TR-MCP-FEDERATION-011. TEST: TEST-MCP-232. Classes: MigrationJoinTests.
- `TASK-FEDPH6J-003` (group ordinal 3): Ph6 exit gate on LAB and LEGION (full phase including GRP-FEDPH6-001). Initial state: Planning. Depends on: TASK-FEDPH6J-001, TASK-FEDPH6J-002.
  - FR/AC: FR-MCP-187: MigrationJoinPreservesCommonAncestor, MigrationJoinDivergentRowRequiresReview, JoinCutoverRequiresReviewedMain; FR-MCP-184: AuditBackfillVerified.
  - TR: TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-014. TEST: TEST-MCP-232, TEST-MCP-235.
- `TASK-FEDPH6J-004` (group ordinal 4): Ph6 group HV gate: Ph6 join migration and cutover readiness. Initial state: Planning. Depends on: TASK-FEDPH6J-003.
  - FR/AC: FR-MCP-187: MigrationJoinPreservesCommonAncestor, MigrationJoinDivergentRowRequiresReview, JoinCutoverRequiresReviewedMain; FR-MCP-184: AuditBackfillVerified.
  - TR: TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-014. TEST: TEST-MCP-232, TEST-MCP-235.

#### GRP-FEDPH7-001: Ph7 server and plugin workflows

Group key `ph7-surfaces`; phase Ph7; plan section 12. Typed client, REST/MCP/REPL adapters and audited first-party plugin wrappers with shared conformance fixtures.

- `TASK-FEDPH7-001` (group ordinal 1): Plugin repo/version inventory and typed DTO/client methods. Initial state: Planning. Depends on: GRP-FEDPH6-002.
  - FR/AC: FR-MCP-183: OldServerReportsUnsupportedCapability, ReconnectRevalidatesContext.
  - TR: TR-MCP-FEDERATION-010. TEST: TEST-MCP-231. Classes: FederationClientContractTests.
- `TASK-FEDPH7-002` (group ordinal 2): REST, MCP and REPL adapters for every federation workflow. Initial state: Planning. Depends on: TASK-FEDPH7-001.
  - FR/AC: FR-MCP-183: EveryPluginUsesSameContract.
  - TR: TR-MCP-FEDERATION-010. TEST: TEST-MCP-231. Classes: FederationMcpToolTests, FederationReplContractTests.
- `TASK-FEDPH7-003` (group ordinal 3): First-party plugin wrappers and shared conformance fixtures. Initial state: Planning. Depends on: TASK-FEDPH7-002.
  - FR/AC: FR-MCP-176: PluginEnrollmentDiagnostics; FR-MCP-182: PluginIdentityDiagnostics; FR-MCP-185: PluginLeastPrivilegeDenial; FR-MCP-181: PluginRecoveryControls; FR-MCP-188: PluginEffectBoundary; FR-MCP-189: PluginEditingOwnership.
  - TR: TR-MCP-FEDERATION-003, TR-MCP-FEDERATION-008, TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-012, TR-MCP-FEDERATION-015, TR-MCP-FEDERATION-016. TEST: TEST-MCP-224, TEST-MCP-229, TEST-MCP-230, TEST-MCP-233, TEST-MCP-236, TEST-MCP-237. Classes: FederationPluginConformanceTests.
- `TASK-FEDPH7-004` (group ordinal 4): Review and wait-age surfacing with 15 minute warning and 24 hour escalation. Initial state: Planning. Depends on: TASK-FEDPH7-002.
  - FR/AC: FR-MCP-180: ReviewAgeVisible.
  - TR: TR-MCP-FEDERATION-007. TEST: TEST-MCP-228. Classes: FederationClientContractTests.
- `TASK-FEDPH7-005` (group ordinal 5): Ph7 exit gate on LAB and LEGION (client and plugin slice). Initial state: Planning. Depends on: TASK-FEDPH7-001, TASK-FEDPH7-002, TASK-FEDPH7-003, TASK-FEDPH7-004.
  - FR/AC: FR-MCP-183: OldServerReportsUnsupportedCapability, ReconnectRevalidatesContext, EveryPluginUsesSameContract; FR-MCP-176: PluginEnrollmentDiagnostics; FR-MCP-182: PluginIdentityDiagnostics; FR-MCP-185: PluginLeastPrivilegeDenial; FR-MCP-181: PluginRecoveryControls; FR-MCP-188: PluginEffectBoundary; FR-MCP-189: PluginEditingOwnership; FR-MCP-180: ReviewAgeVisible.
  - TR: TR-MCP-FEDERATION-003, TR-MCP-FEDERATION-007, TR-MCP-FEDERATION-008, TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-010, TR-MCP-FEDERATION-012, TR-MCP-FEDERATION-015, TR-MCP-FEDERATION-016. TEST: TEST-MCP-224, TEST-MCP-228, TEST-MCP-229, TEST-MCP-230, TEST-MCP-231, TEST-MCP-233, TEST-MCP-236, TEST-MCP-237.
- `TASK-FEDPH7-006` (group ordinal 6): Ph7 group HV gate: Ph7 server and plugin workflows. Initial state: Planning. Depends on: TASK-FEDPH7-005.
  - FR/AC: FR-MCP-183: OldServerReportsUnsupportedCapability, ReconnectRevalidatesContext, EveryPluginUsesSameContract; FR-MCP-176: PluginEnrollmentDiagnostics; FR-MCP-182: PluginIdentityDiagnostics; FR-MCP-185: PluginLeastPrivilegeDenial; FR-MCP-181: PluginRecoveryControls; FR-MCP-188: PluginEffectBoundary; FR-MCP-189: PluginEditingOwnership; FR-MCP-180: ReviewAgeVisible.
  - TR: TR-MCP-FEDERATION-003, TR-MCP-FEDERATION-007, TR-MCP-FEDERATION-008, TR-MCP-FEDERATION-009, TR-MCP-FEDERATION-010, TR-MCP-FEDERATION-012, TR-MCP-FEDERATION-015, TR-MCP-FEDERATION-016. TEST: TEST-MCP-224, TEST-MCP-228, TEST-MCP-229, TEST-MCP-230, TEST-MCP-231, TEST-MCP-233, TEST-MCP-236, TEST-MCP-237.

#### GRP-FEDPH8-001: Ph8 two-host acceptance and three-peer fixture

Group key `ph8-acceptance`; phase Ph8; plan section 13. Real-host acceptance on LAB-OMARCHY and PAYTON-LEGION2 plus FED-ATLAS-H/A/B fixture; cutover preparation only.

- `TASK-FEDPH8-001` (group ordinal 1): Three-logical-peer fixture run of U01-U15 from the accepted integration SHA. Initial state: Planning. Depends on: GRP-FEDPH7-001.
  - FR/AC: FR-MCP-178: ThreeWriterCleanConvergence; FR-MCP-186: RealHostAcceptanceMatrix.
  - TR: TR-MCP-FEDERATION-005, TR-MCP-FEDERATION-013. TEST: TEST-MCP-226, TEST-MCP-234.
- `TASK-FEDPH8-002` (group ordinal 2): Two-real-host acceptance matrix on LAB and LEGION. Initial state: Planning. Depends on: GRP-FEDPH7-001.
  - FR/AC: FR-MCP-186: RealHostAcceptanceMatrix, DeploymentPreflightBlocksDirtyOrUnattestedBranch, WindowsWriterLockAndRestart; FR-MCP-179: HubRestoreFromOlderBackup; FR-MCP-185: SqlPortUnreachableAcrossLink, UnknownCertificatePinRejected; FR-MCP-184: ReceiptsAndLogsContainNoCredentials.
  - TR: TR-MCP-FEDERATION-006, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-012, TR-MCP-FEDERATION-013. TEST: TEST-MCP-227, TEST-MCP-232, TEST-MCP-233, TEST-MCP-234.
- `TASK-FEDPH8-003` (group ordinal 3): Partition and restart fault matrix. Initial state: Planning. Depends on: TASK-FEDPH8-001, TASK-FEDPH8-002.
  - FR/AC: FR-MCP-181: LostPushAckDoesNotDuplicateCommit; FR-MCP-174: NodeWorksWithHubUnavailable.
  - TR: TR-MCP-FEDERATION-001, TR-MCP-FEDERATION-008. TEST: TEST-MCP-222, TEST-MCP-229.
- `TASK-FEDPH8-004` (group ordinal 4): Four-provider regression, three-source migration, reviewed join, recovery export and isolated restore receipts. Initial state: Planning. Depends on: TASK-FEDPH8-001, TASK-FEDPH8-002.
  - FR/AC: FR-MCP-187: ThreeSourceProviderRoundTrip, FullProjectRecoveryPreservesAllDomains; FR-MCP-186: OriginalProviderMigrationsUnchanged.
  - TR: TR-MCP-FEDERATION-013, TR-MCP-FEDERATION-014. TEST: TEST-MCP-234, TEST-MCP-235.
- `TASK-FEDPH8-005` (group ordinal 5): Cutover preparation under DEPLOY HOLD (no live change). Initial state: Planning. Depends on: TASK-FEDPH8-004.
  - FR/AC: FR-MCP-187: SourceRetainedAfterCutover.
  - TR: TR-MCP-FEDERATION-014. TEST: TEST-MCP-235.
- `TASK-FEDPH8-006` (group ordinal 6): Ph8 exit gate on LAB and LEGION (release candidate). Initial state: Planning. Depends on: TASK-FEDPH8-001, TASK-FEDPH8-002, TASK-FEDPH8-003, TASK-FEDPH8-004, TASK-FEDPH8-005.
  - FR/AC: FR-MCP-178: ThreeWriterCleanConvergence; FR-MCP-186: RealHostAcceptanceMatrix, DeploymentPreflightBlocksDirtyOrUnattestedBranch, WindowsWriterLockAndRestart, OriginalProviderMigrationsUnchanged; FR-MCP-179: HubRestoreFromOlderBackup; FR-MCP-185: SqlPortUnreachableAcrossLink, UnknownCertificatePinRejected; FR-MCP-184: ReceiptsAndLogsContainNoCredentials; FR-MCP-181: LostPushAckDoesNotDuplicateCommit; FR-MCP-174: NodeWorksWithHubUnavailable; FR-MCP-187: ThreeSourceProviderRoundTrip, FullProjectRecoveryPreservesAllDomains, SourceRetainedAfterCutover.
  - TR: TR-MCP-FEDERATION-001, TR-MCP-FEDERATION-005, TR-MCP-FEDERATION-006, TR-MCP-FEDERATION-008, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-012, TR-MCP-FEDERATION-013, TR-MCP-FEDERATION-014. TEST: TEST-MCP-222, TEST-MCP-226, TEST-MCP-227, TEST-MCP-229, TEST-MCP-232, TEST-MCP-233, TEST-MCP-234, TEST-MCP-235.
- `TASK-FEDPH8-007` (group ordinal 7): Ph8 group HV gate: Ph8 two-host acceptance and three-peer fixture. Initial state: Planning. Depends on: TASK-FEDPH8-006.
  - FR/AC: FR-MCP-178: ThreeWriterCleanConvergence; FR-MCP-186: RealHostAcceptanceMatrix, DeploymentPreflightBlocksDirtyOrUnattestedBranch, WindowsWriterLockAndRestart, OriginalProviderMigrationsUnchanged; FR-MCP-179: HubRestoreFromOlderBackup; FR-MCP-185: SqlPortUnreachableAcrossLink, UnknownCertificatePinRejected; FR-MCP-184: ReceiptsAndLogsContainNoCredentials; FR-MCP-181: LostPushAckDoesNotDuplicateCommit; FR-MCP-174: NodeWorksWithHubUnavailable; FR-MCP-187: ThreeSourceProviderRoundTrip, FullProjectRecoveryPreservesAllDomains, SourceRetainedAfterCutover.
  - TR: TR-MCP-FEDERATION-001, TR-MCP-FEDERATION-005, TR-MCP-FEDERATION-006, TR-MCP-FEDERATION-008, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-012, TR-MCP-FEDERATION-013, TR-MCP-FEDERATION-014. TEST: TEST-MCP-222, TEST-MCP-226, TEST-MCP-227, TEST-MCP-229, TEST-MCP-232, TEST-MCP-233, TEST-MCP-234, TEST-MCP-235.

#### GRP-FEDCUT-001: Held live cutover (requires explicit DEPLOY HOLD release)

Group key `held-cutover`; phase Ph8; plan section 13. Staged live cutover after Ph8 acceptance and an explicit operator release for this exact rollout.

- `TASK-FEDCUT-001` (group ordinal 1): Cut over the first host and observe a 48-hour soak. Initial state: Planning. Depends on: GRP-FEDPH8-001.
  - FR/AC: FR-MCP-186: DeploymentPreflightBlocksDirtyOrUnattestedBranch; FR-MCP-184: AuditReachesHub.
  - TR: TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-013. TEST: TEST-MCP-232, TEST-MCP-234.
- `TASK-FEDCUT-002` (group ordinal 2): Cut over the second host and final acceptance. Initial state: Planning. Depends on: TASK-FEDCUT-001.
  - FR/AC: FR-MCP-178: ThreeWriterCleanConvergence; FR-MCP-187: SourceRetainedAfterCutover.
  - TR: TR-MCP-FEDERATION-005, TR-MCP-FEDERATION-014. TEST: TEST-MCP-226, TEST-MCP-235.
- `TASK-FEDCUT-003` (group ordinal 3): Ph8 group HV gate: Held live cutover (requires explicit DEPLOY HOLD release). Initial state: Planning. Depends on: TASK-FEDCUT-002.
  - FR/AC: FR-MCP-186: DeploymentPreflightBlocksDirtyOrUnattestedBranch; FR-MCP-184: AuditReachesHub; FR-MCP-178: ThreeWriterCleanConvergence; FR-MCP-187: SourceRetainedAfterCutover.
  - TR: TR-MCP-FEDERATION-005, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-013, TR-MCP-FEDERATION-014. TEST: TEST-MCP-226, TEST-MCP-232, TEST-MCP-234, TEST-MCP-235.

### 15.5 Simulated queue in the current TODO store

Until PLAN-TODOTASK-001 ships, the queue is simulated with ordinary MCP TODOs in workspace F:\GitHub\McpServer on PAYTON-LEGION2 (LAB-OMARCHY to follow when reachable): one Plan TODO, one TODO per Task Group and one TODO per Task, all with section Federation and priority high. Each description carries ItemKind, parent TODO, group and ordinals, Task status, eligibility, typed dependencies, FR/AC coverage and TR constraints; `dependsOn` mirrors the typed dependencies; `note` carries the simulated status. TODO done stays false until a Task reaches Success; a Group TODO is marked done only when its calculated status is Completed. Old forwarding-era TODOs IMPL-FEDPATH-001, IMPL-FEDROUTE-001, IMPL-FEDWSSYNC-001 and IMPL-FEDBACKFILL-001 are not part of this Plan; MCP-FEDIDENTITY-001 is linked from TASK-FEDPH0-005 and TASK-FEDPH2-005.
