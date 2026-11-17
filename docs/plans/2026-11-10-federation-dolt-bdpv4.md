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

For each small slice, first define the acceptance cases, fixtures and negative controls. Prove every required assertion passes against correct controlled mocks and rejects deliberately wrong behavior as expected negative controls inside a passing harness. Establish zero failures, zero skips and no unexecuted required cases before production implementation. Then implement the smallest behavior and bind the same cases, inputs, assertions and interaction checks to the real implementation; remediate until the production-bound and full applicable exit gates pass. A deliberately red production suite is not the mock gate. Mocks must exercise the asserted orchestration boundary.

Use targeted `./build.ps1 TestGroups --test-groups '<project>:<class>' --test-run-id '<phase-slice>'` after changes. At each phase exit run the entire current-plus-prior unit suite once with `./build.ps1 Test`, requiring zero failures and zero skips. `./build.ps1 Compile` and `./build.ps1 ValidateTraceability` are mandatory gate commands. Add real-container integration tests where a mock cannot prove SQL, migration, locking or remote semantics. Do not broaden/repeat a passing gate without changes or new evidence requiring it. Receipts include SHA/tree state, image/package versions, commands, TRX, fail/skip counts, AC mapping and negative controls.

The proposed test-class names below are implementation targets, not claims they exist today. Register new projects/classes in Nuke discovery as part of their owning phase. Do not add skipped placeholders for later phases; keep deferred coverage in requirements/TODO state. New federation test projects target xUnit v3 and use its native skip (Assert.Skip, Assert.SkipWhen, Assert.SkipUnless); no Xunit.SkippableFact or v2 xunit packages, per MEMORY-BESTPRACTICE-001 section 10.

Retain the operator's complete phase-exit gate on both LAB and LEGION: Nuke Test, MigrationIntegrationTests, the full Build.Tests suite, and ValidateTraceability at the same accepted head. Test currently excludes Build.Tests; do not infer its coverage from a green Test receipt. Ph0 records the complete discovered Build.Tests class inventory and its Nuke TestGroups invocation; every listed class and test must execute, with zero failures and zero skips. MigrationIntegrationTests and the new TestFederationProviders suite are complementary and neither replaces the other. Keep unit, integration and build-suite receipts distinct and run the full set once per final phase head, repeating only after changes or failed gates.

Add Nuke target `TestFederationProviders` with the planned command `./build.ps1 TestFederationProviders --providers sqlite,postgresql,sqlserver,dolt --test-run-id '<phase>-providers'`. It is not an existing command today. Ph0 records exact PostgreSQL/SQL Server/Dolt image versions and digests and the SQLite package version; Ph1 implements the target and refuses missing/unhealthy containers as a gate failure, never a skip. Every Ph1-Ph8 exit requires both the full unit gate and this real-provider/remote suite with zero failures and zero skips. Container-dependent classes are registered only in TestFederationProviders and excluded from Test by category at discovery, never runtime skipping. Receipts include image digests and environment readiness evidence.

## 5. Ph0: feasibility and exact dependency selection

Deliver a version-pinned compatibility receipt and license/dependency record for Dolt and the MySQL-protocol EF provider and driver selected under B1. Use current net10.0/EF10.0.0 as the baseline. Read the dependency compliance rules before additions. Existing Dolt EF samples establish possibility, not compatibility.

Prove on LAB Linux and LEGION native Windows against real databases (containers where appropriate): full current model creation; representative CRUD/LINQ/raw SQL; concurrency tokens; transaction rollback; decimal/date/text/index/FK mappings; migrations; branch connection isolation; explicit SQL commit versus Dolt commit; merge conflicts/constraints; clean and dirty remote pushes; lost responses; authenticated project isolation; inability to bypass guarded publication through raw remote requests. Verify canonical-ref advancement and remote recovery under process death. Compare candidate clean merges against domain validators.

Classes: `DoltProviderCompatibilityTests`, `DoltRemoteAuthorizationTests`, `DoltBranchIsolationTests`, `DoltCommitBoundaryTests`. Red conditions: unsupported mappings/query translation, ordinary SaveChanges creating history, pool branch leakage, unauthorized ref mutation or missing recovery guarantees. Green: all named contracts pass on pinned versions using the operator-accepted EF10 provider or patched community fork under B1, with no bespoke from-scratch provider or custom merge engine.

Ph0 runs a disposable harness and records exact commands, digests, TRX and fail/skip counts. Ph1 carries the proven Ph0 contract assertions into maintained classes under TestFederationProviders and passes them at Ph1 exit. If Ph0 changes repository code, the full Test gate also applies at Ph0 exit; disposable prototypes do not become an undocumented production dependency.

Ph0 exit consists of the following numbered **blocking outputs**. The implementation owner produces each evidence record; the operator accepts it, with independent HV selected under section 14 and current Global reviewer policy, assessing substantive accuracy/completeness. Each record stores owner, status (unproven/proven/accepted), evidence paths and dependent phases. Initial status for all B1-B12 is unproven. Every B1-B12 receipt has separate LAB and LEGION evidence rows; Windows is not inferred from Linux. Ph0 verifies the current-scope coverage index and binds each applicable acceptance criterion and workflow to its implementation phase. All must be accepted before Ph1 production integration begins; Ph0 prototypes remain disposable.

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

Independent HV targets accuracy and completeness of at least 98 each on the exact current architecture, plan, workflow and contract packet. Reviewer selection follows current Global MEMORY-HV-100 section 7: until explicitly revoked, a request from LEGION runs on LAB-OMARCHY using GPT-5.6-SOL at Max, and a request from LAB runs on LEGION using the same model and effort; this temporary cross-machine rule overrides the older different-family wording throughout this packet. Other HV obligations remain in force. This revision awaits that review and Ph0 feasibility evidence. Record source/PDF hashes and findings separately from the plan. DEPLOY HOLD requires explicit operator release after acceptance; neither plan approval nor a review score releases it.
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
2. Group completion HV gate, Task-Workflow-Architecture.md section 3.5.12 GroupCompletionHvGatePolicy (operator rule 2026-11-11): the last member of every group is its HV gate Task, depending on all other members; reviewer selected under section 14 and current Global reviewer policy; AGREE at max(configured threshold, 0.98) for both scores (98/98); the group calculates Completed only after the gate succeeds, so nothing depending on the group starts earlier; DISAGREE fails the gate and findings against Success work become correction Tasks (correctsTaskId) before the gate, then the gate is remediated and re-run. Delivery order: section 8.6.

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
- `TASK-FEDPH0-009` (group ordinal 9): Ph0 group HV gate: Ph0 feasibility and exact dependency selection. Initial state: Planning. Depends on: TASK-FEDPH0-001, TASK-FEDPH0-002, TASK-FEDPH0-003, TASK-FEDPH0-004, TASK-FEDPH0-005, TASK-FEDPH0-006, TASK-FEDPH0-007, TASK-FEDPH0-008.
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
- `TASK-FEDPH1-005` (group ordinal 5): Ph1 group HV gate: Ph1 fourth provider and schema migrations. Initial state: Planning. Depends on: TASK-FEDPH1-001, TASK-FEDPH1-002, TASK-FEDPH1-003, TASK-FEDPH1-004.
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
- `TASK-FEDPH2-008` (group ordinal 8): Ph2 group HV gate: Ph2 project routing, data boundaries and editing ownership. Initial state: Planning. Depends on: TASK-FEDPH2-001, TASK-FEDPH2-002, TASK-FEDPH2-003, TASK-FEDPH2-004, TASK-FEDPH2-005, TASK-FEDPH2-006, TASK-FEDPH2-007.
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
- `TASK-FEDPH3-006` (group ordinal 6): Ph3 group HV gate: Ph3 seed migration and project recovery export. Initial state: Planning. Depends on: TASK-FEDPH3-001, TASK-FEDPH3-002, TASK-FEDPH3-003, TASK-FEDPH3-004, TASK-FEDPH3-005.
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
- `TASK-FEDPH4-005` (group ordinal 5): Ph4 group HV gate: Ph4 explicit history commits and local publication intent. Initial state: Planning. Depends on: TASK-FEDPH4-001, TASK-FEDPH4-002, TASK-FEDPH4-003, TASK-FEDPH4-004.
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
- `TASK-FEDPH5-007` (group ordinal 7): Ph5 group HV gate: Ph5 hub control table and publication. Initial state: Planning. Depends on: TASK-FEDPH5-001, TASK-FEDPH5-002, TASK-FEDPH5-003, TASK-FEDPH5-004, TASK-FEDPH5-005, TASK-FEDPH5-006.
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
- `TASK-FEDPH6-007` (group ordinal 7): Ph6 group HV gate: Ph6 incoming integration, review and recovery. Initial state: Planning. Depends on: TASK-FEDPH6-001, TASK-FEDPH6-002, TASK-FEDPH6-003, TASK-FEDPH6-004, TASK-FEDPH6-005, TASK-FEDPH6-006.
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
- `TASK-FEDPH6J-004` (group ordinal 4): Ph6 group HV gate: Ph6 join migration and cutover readiness. Initial state: Planning. Depends on: TASK-FEDPH6J-001, TASK-FEDPH6J-002, TASK-FEDPH6J-003.
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
- `TASK-FEDPH7-006` (group ordinal 6): Ph7 group HV gate: Ph7 server and plugin workflows. Initial state: Planning. Depends on: TASK-FEDPH7-001, TASK-FEDPH7-002, TASK-FEDPH7-003, TASK-FEDPH7-004, TASK-FEDPH7-005.
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
- `TASK-FEDPH8-007` (group ordinal 7): Ph8 group HV gate: Ph8 two-host acceptance and three-peer fixture. Initial state: Planning. Depends on: TASK-FEDPH8-001, TASK-FEDPH8-002, TASK-FEDPH8-003, TASK-FEDPH8-004, TASK-FEDPH8-005, TASK-FEDPH8-006.
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
- `TASK-FEDCUT-003` (group ordinal 3): Ph8 group HV gate: Held live cutover (requires explicit DEPLOY HOLD release). Initial state: Planning. Depends on: TASK-FEDCUT-001, TASK-FEDCUT-002.
  - FR/AC: FR-MCP-186: DeploymentPreflightBlocksDirtyOrUnattestedBranch; FR-MCP-184: AuditReachesHub; FR-MCP-178: ThreeWriterCleanConvergence; FR-MCP-187: SourceRetainedAfterCutover.
  - TR: TR-MCP-FEDERATION-005, TR-MCP-FEDERATION-011, TR-MCP-FEDERATION-013, TR-MCP-FEDERATION-014. TEST: TEST-MCP-226, TEST-MCP-232, TEST-MCP-234, TEST-MCP-235.

### 15.5 Simulated queue in the current TODO store

Until PLAN-TODOTASK-001 ships, the queue is simulated with ordinary MCP TODOs in workspace F:\GitHub\McpServer on PAYTON-LEGION2 (LAB-OMARCHY to follow when reachable): one Plan TODO, one TODO per Task Group and one TODO per Task, all with section Federation and priority high. Each description carries ItemKind, parent TODO, group and ordinals, Task status, eligibility, typed dependencies, FR/AC coverage and TR constraints; `dependsOn` mirrors the typed dependencies; `note` carries the simulated status. TODO done stays false until a Task reaches Success; a Group TODO is marked done only when its calculated status is Completed. Old forwarding-era TODOs IMPL-FEDPATH-001, IMPL-FEDROUTE-001, IMPL-FEDWSSYNC-001 and IMPL-FEDBACKFILL-001 are not part of this Plan; MCP-FEDIDENTITY-001 is linked from TASK-FEDPH0-005 and TASK-FEDPH2-005.

## 16. Canonical requirement definitions (operator instruction 2026-11-17)

This section makes the definitions part of this plan. QBrain requirement definitions must be populated from this section, through the Codex plugin, after the applicable-definition inventory is complete. The phase, architecture and workflow contracts above remain in force. No implementation or deployment acceptance is claimed.

### 16.1 Provenance and write rules

- The 68 definitions in section 16.2 are the current release contracts and amended compatibility obligations recovered from docs/Project/federation-dolt at source head 911cb810fbf1e005e2cbbbc426daeea7300319a2. Section 16.4 adds 18 explicitly retained dependencies, for 86 definitions in this plan. They are current requirements, not the historical 223-record archive. Each entry names its source projection.
- Originating Omarchy QBrain session: Codex-20261110T152714Z-plugin-session. User request req-20261110T181140Z-prompt-61a6 asks for server/plugin requirements satisfying the workflows; req-20261110T182914Z-prompt-45d8 asks for the new plan; req-20261110T182958Z-prompt-4aa8 explicitly requires Dolt as a fourth database with migrations; req-20261110T183020Z-prompt-3f53 explicitly requires original-provider data migration. These are operator requests; the detailed derived contracts are authored planning work.
- User correction req-20261111T010827Z-prompt-e501 excludes irrelevant historical bulk. Raw source archives remain evidence and do not expand this release.
- R5 plan-readiness receipt: docs/receipts/hv/20261111T114239-federation-r5-plan-readiness.verdict.md, AGREE 98/98. It accepts the reviewed planning amendments, including a patchable prerelease/community EF10 provider and xUnit v3. It does not certify runtime feasibility or this new catalog amendment.
- Preserve canonical record IDs, named acceptance-criterion identities and unrelated links. No deletes or placeholder creation. Preserve existing status, priority, scope and satisfaction/evidence for unchanged criteria unless the plan explicitly changes them. Changed or new criteria remain unsatisfied without acceptance evidence.
- QBrain writes follow the completed plan definitions. Capture pre-write records, explicit field deltas, plugin responses and server readback for each host. A placeholder read is not permission to assume its cause. Do not resume Phase 0 while definitions remain unresolved.

### 16.2 Full current and amended definitions

#### FR-MCP-103: Autonomous committed-history federation

Status: pending

Enrolled nodes execute and persist authorized business work locally while offline and exchange explicit version commits through LAB-OMARCHY. The hub coordinates project enrollment, guarded publication and reconciliation; it is not the executor of ordinary node requests. DirectProxy remains an explicitly selected legacy capability under FR-MCP-077. LocalProxy forwarding, outage command replay and read-switch are superseded for versioned federation.

- [ ] review20261110-1: Given A cannot reach the hub, when it edits and commits T1, then local read returns the change and publication remains pending.
- [ ] review20261110-2: Given H and A commit concurrently, when synchronization completes, then both histories survive or a visible review blocks only that branch.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-125: Legacy federation compatibility without general keyserver gating

Status: pending

Keep legacy HMAC envelope compatibility separate from transaction signing and protected diffgram encryption. Under the later FR-MCP-173 decision, general federation apply/control and audit do not invoke the QuadBrain coordinator or fail closed merely because it is degraded. Actual QuadBrain operations retain their coordinator rules.

- [ ] review20261110-1: Given legacy signed envelopes, when explicit legacy-mode tests run, then compatibility remains intact.
- [ ] review20261110-2: Given degraded QuadBrain coordinator, when authorized general federation control or audit runs, then no keyserver call occurs and the operation is not blocked by coordinator state.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-174: autonomous local execution

Status: pending. Revision audit amended 2026-11-10.
Every node, including the hub, performs authorized local business operations and persists results without hub connectivity.

**Acceptance Criteria:**
- [ ] ac-NodeWorksWithHubUnavailable: Given A/main=C0 and the hub link is down, when A changes TODO T1.title from old to local, then the local API succeeds, a local reread returns local, and no hub request or history commit occurs.
- [ ] ac-HubUsesSameLocalExecution: Given H/main=C0 and the gateway is stopped, when H edits T1.title, then H persists it locally while remote/main remains C0.
- [ ] ac-OfflineLocalReadAfterWrite: Given the prior offline edit, when the application and Dolt restart, then T1.title remains local and main remains C0 until an explicit version commit.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-175: explicit version commits and publication boundary

Status: pending. Revision audit amended 2026-11-10.
Only an explicit version-history commit triggers publication; drafts and SQL saves remain local. Commit retries recover the same history identity.

**Acceptance Criteria:**
- [ ] ac-SaveDoesNotPublish: Given main=C0 and remote=C0, when SaveChanges persists T1.title=draft, then main and remote stay C0 and no publication intent is emitted.
- [ ] ac-VersionCommitTriggersSync: Given that draft and a valid preview, when commit request R1 succeeds, then main=A1, the reply says committed_locally, and one recoverable publication intent targets A1.
- [ ] ac-HubRequestDoesNotCommitDraft: Given B has a draft and desired head A1, when B polls, then outcome=waiting_local_work, B/main=C0 and the draft value is unchanged.
- [ ] ac-OfflineCommitDiscoveredAfterRestart: Given A1 committed while disconnected and a crash before intent persistence, when A restarts, then history scanning rediscovers A1 exactly once and publishes it after reconnect.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-176: logical project enrollment and initial clone

Status: pending. Revision audit amended 2026-11-10.
Nodes enroll a logical project under immutable shared identity while retaining host-local paths and refusing destructive clones.

Server: project discovery, scoped enrollment and structured refusal diagnostics. Plugin: project discovery/selection, enrollment progress and explicit actionable diagnostics.

**Acceptance Criteria:**
- [ ] ac-DifferentHostPathsSameProject: Given LAB path /home/sharpninja/github/McpServer and LEGION path F:/github/McpServer, when both enroll as ProjectId Atlas, then both reference Atlas and retain their distinct local paths.
- [ ] ac-CloneRefusesNonemptyTarget: Given target has TODO X, when clone is requested, then409 target_not_empty leaves X and all target files unchanged.
- [ ] ac-UnauthorizedProjectDenied: Given node A has only Atlas membership, when it enrolls or clones SecretProject, then403 unauthorized_project returns no rows, objects or project existence details.
- [ ] ac-PluginEnrollmentDiagnostics: Given Atlas is discoverable but A has a nonempty destination, when each plugin selects Atlas and attempts enrollment/clone, then it displays ProjectId, selected branch and409 target_not_empty with remediation; no database is overwritten and no different project is selected.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-177: branches across server and plugin context

Status: pending. Revision audit amended 2026-11-10.
First-release project branches support isolated work, explicit sharing and reviewed merge; session selection never leaks between connections.

**Acceptance Criteria:**
- [ ] ac-PooledConnectionDoesNotLeakBranch: Given main T1.title=main and feature/search T1.title=feature, when alternating pooled reads run100 times, then each returns only its selected value.
- [ ] ac-FeaturePublicationLeavesMainUnchanged: Given main=C0 and feature/search=F1, when F1 publishes, then remote feature=F1 and all main heads remain C0.
- [ ] ac-BranchMergeRequiresExpectedHeads: Given a preview for main=C0 and feature=F1 but feature advances to F2, when merge submits the old preview, then409 head_changed creates no commit and changes no ref.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-178: bidirectional committed-history exchange

Status: pending. Revision audit amended 2026-11-10.
Exchange committed history bidirectionally through guarded publication, preserving every accepted ancestor and the hub independent working copy.

**Acceptance Criteria:**
- [ ] ac-ThreeWriterCleanConvergence: Given C0 contains T1.title=old,T1.description=old,T2.priority=low, when A1 changes title=A, B1 changes description=B and H1 changes T2.priority=high, then merges M1(A1,B1), M2(M1,H1) converge all heads to M2 with values A,B,high and all three commits in ancestry.
- [ ] ac-HubDraftDoesNotBlockRemote: Given H has an uncommitted title draft at C0, when A publishes A1, then remote=A1, H remains C0 with its draft, and B can accept A1.
- [ ] ac-ConcurrentPublishPreservesBothHistories: Given A1 and B1 descend from C0, when both accept requests expect C0, then one succeeds and the other receives409 head_changed; after merge M1 publishes, both A1 and B1 remain ancestors.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-179: hub control table and polling

Status: pending. Revision audit amended 2026-11-10.
Use a durable hub subscription table with polling, generation and worker fencing, plus a fresh hub epoch after restore.

**Acceptance Criteria:**
- [ ] ac-PollReadDoesNotAcknowledge: Given desired=7 and reported=6, when poll runs twice, then both responses request7 and stored reported remains6.
- [ ] ac-AckOldGenerationPreservesNewRequest: Given desired becomes8 while report7 is in flight, when report7 arrives, then reported=7, desired=8 and the subscription remains pending.
- [ ] ac-MissedNotificationReconciles: Given remote=A1 but control still names C0 after a crash, when reconciliation runs, then it records A1 and advances desired generation without producing another commit.
- [ ] ac-HubRestoreFromOlderBackup: Given a node remembers epoch E1 generation90 and the hub restores a backup at generation40, when the hub reopens, then epoch E2 differs from E1; E1 reports fail409 hub_epoch_changed and no head is force-reset. Node A1 is retained until verified ancestry recovery republishes missing history.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-180: reviewed conflicts and valid merges

Status: pending. Revision audit amended 2026-11-10.
Conflicts and invalid merges preserve valid live data, require authorized review and remain visible with ageing while unrelated publication continues.

**Acceptance Criteria:**
- [ ] ac-SameCellConflictNeedsReview: Given base priority=low, A1 priority=high and B1 priority=medium, when B integrates A1, then needs_review shows low/medium/high, B/main remains B1 and no public ref is overwritten.
- [ ] ac-DeleteUpdateNeedsReview: Given A1 deletes T1 and B1 edits T1.title, when histories merge, then needs_review retains B1 and the deleted/edited alternatives for review.
- [ ] ac-CleanMergeCanFailDomainValidation: Given separate commits add T1 dependsOn T2 and T2 dependsOn T1, when Dolt merges without cell conflicts, then domain validation records dependency_cycle and leaves the prior valid live head unchanged.
- [ ] ac-StaleReviewRejected: Given a resolution preview binds local=B1 and incoming=A1, when incoming advances to A2, then resolve returns409 stale_review with no merge commit.
- [ ] ac-ReviewAgeVisible: Given unresolved needs_review at12:00, when the clock reaches12:15, then plugins/status and the operator inbox show warning with ReviewId and age900s; at24h severity is critical, with at most one repeat per hour.
- [ ] ac-UnrelatedPublicationContinues: Given Atlas/main is blocked by review R1, when Atlas/feature publishes F2 and another project publishes X1, then both succeed while Atlas/main remains unchanged.
- [ ] ac-SafeTodoMergeRules: Given A changes title and B changes description with no domain violation, when merge runs, then both values survive; competing priority or completion edits produce needs_review instead of choosing a timestamp winner.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-181: recovery without lost or repeated work

Status: pending. Revision audit amended 2026-11-10.
Recover synchronization, receipts and restored hub state without losing local work, inventing success or replaying business effects.

Server: durable operation resume/status/safe cancellation. Plugin: expose resume, status and cancel with completed-versus-pending effects; never claim cancellation reverses published history.

**Acceptance Criteria:**
- [ ] ac-LostPushAckDoesNotDuplicateCommit: Given remote accepted A1 and reply was lost, when request R1 retries, then it returns A1 and the existing receipt; no A2 commit is created.
- [ ] ac-OldWorkerEpochCannotReportSuccess: Given worker epoch changes4 to5, when epoch4 reports success, then409 stale_worker_epoch leaves current receipt and desired state unchanged.
- [ ] ac-DirtyBranchDefersApply: Given B/main=C0 with title=draft, when A1 is fetched, then outcome=waiting_local_work, main stays C0 and title remains draft; at15min status/inbox shows the age warning.
- [ ] ac-IncompatibleSchemaDoesNotApply: Given B supports schema1 and incoming S1 requires schema2, when sync runs, then409 incompatible_schema leaves B's live data/head unchanged and records the required version.
- [ ] ac-PluginRecoveryControls: Given operation R1 published A1 but its local reply was lost, when a plugin resumes, reads status and requests cancellation, then it shows publishedHead=A1 and a durable cancellation outcome for remaining work; it never claims A1 was rolled back or creates a second commit.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-182: collision-resistant and preserved identity

Status: pending. Revision audit amended 2026-11-10.
Prevent generated-ID collisions, preserve permanent legacy aliases, detect duplicate work and atomically reserve human-assigned IDs.

Server: provenance, scoped aliases, collision evidence and reservation outcomes. Plugin: expose provenance/collision diagnostics and the authorized resolution path without rewriting historical references.

**Acceptance Criteria:**
- [ ] ac-TwoNodesCreateDistinctIds: Given tags LA and LG and both counters start1, when both create a TODO, then generated IDs differ and both records survive reconciliation.
- [ ] ac-BranchRevertDoesNotReuseSequence: Given LA allocated sequence42, when main reverts and another branch creates a record, then its sequence is greater than42.
- [ ] ac-LegacyCollisionPreservesReferences: Given LAB and LEGION have different entities named TODO-7, when migration resolves the collision, then scoped aliases resolve each original identity and every relational reference follows its reviewed canonical mapping; historical text remains byte-identical.
- [ ] ac-LegacyIdsRemainValid: Given existing IMPL-UNITGREEN-001 and old TODO references, when imported and read by their original IDs, then they resolve through permanent aliases without rewriting commits, PR text, session logs, memories or documents.
- [ ] ac-DifferentIdsSameTodoDetected: Given LAB TODO-7 and LEGION TODO-91 share normalized title Fix login, description and dependency set, when migration plan runs, then it emits a duplicate candidate with both IDs and evidence; until explicit review, neither is merged or deleted.
- [ ] ac-SimultaneousHumanIdReservation: Given human ID IMPL-UNITGREEN-001 is unreserved, when two nodes reserve it concurrently, then exactly one receives201 and the other409 human_id_reserved; retry by the winning requestId returns the same reservation.
- [ ] ac-OfflineHumanIdPublicationDenied: Given an offline-created provisional TODO requesting an unreserved human ID, when it reconnects and publishes, then409 human_id_unreserved retains the local record until reservation or reviewed alias resolution.
- [ ] ac-PluginIdentityDiagnostics: Given LAB TODO-7 and LEGION TODO-91 are a duplicate candidate, when each plugin opens identity review, then both origin IDs, canonical/provisional IDs, similarity evidence and reservation outcome are visible; no alias or row changes before an authorized decision.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-183: first-party plugin federation contract

Status: pending. Revision audit amended 2026-11-10.
Expose consistent typed federation, review, identity, audit, migration and maintenance workflows through every first-party plugin.

**Acceptance Criteria:**
- [ ] ac-EveryPluginUsesSameContract: Given one fixture for commit R1, conflict R2 and migration R3, when each installed first-party plugin calls its wrapper, then normalized IDs, heads, error codes and audit agent identity match the typed client result.
- [ ] ac-OldServerReportsUnsupportedCapability: Given server lacks federation contract1, when a plugin invokes commit, then unsupported_contract is shown and no raw SQL or shell fallback executes.
- [ ] ac-ReconnectRevalidatesContext: Given a cached leaseFence4 and session branch feature, when reconnect discovers fence5, then the plugin retains visible context but refuses writes with lease_lost until reacquired.
- [ ] ac-StaleBranchReportedNotDeleted: Given a branch observation is older than24h, when status is displayed, then stale=true and observedUtc are visible; the branch, drafts and refs are not deleted.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-184: portable data versus host-local state

Status: pending. Revision audit amended 2026-11-10.
Separate versioned project content from operational state while delivering permanent audit to the hub independently of commits and excluding secrets.

**Acceptance Criteria:**
- [ ] ac-CloneOmitsSecrets: Given credentials and local machine paths exist in operational storage, when a project is cloned, then none appears in its versioned tables or history.
- [ ] ac-AuditReachesHub: Given A performs an offline TODO edit and creates audit E1, when connectivity returns without a Dolt commit, then hub audit contains exactly E1, A retains E1, and main/remote remain C0.
- [ ] ac-AuditRetryIdempotent: Given E1 was stored but its receipt was lost, when the same batch retries, then exactly one E1 exists and the same hash receipt returns; changed payload under E1 returns409 audit_id_conflict.
- [ ] ac-AuditBackfillVerified: Given source has100 TODO audit and20 generic audit rows, when seed/join audit backfill verifies, then all120 records have matching payload hashes and hub receipts; missing one blocks cutover.
- [ ] ac-FullProjectSessionGraphPreserved: Given an explicitly promoted sanitized session with turns, actions, dialog, commits/files, PlanFile and TodoId, when cloned, then every promoted child and relationship matches the source snapshot; automatic capture still uses the audit channel.
- [ ] ac-AcceptedHeadRebuildsBranchIndex: Given accepted head changes C0 to A1, when the derived index rebuild completes, then queries read A1 values and no C0 cache is served under the A1 key.
- [ ] ac-BackgroundWritersDoNotDirtyVersionedBranch: Given main=C0 is clean, when heartbeat, audit and automatic session capture run, then Dolt working-set fingerprint remains unchanged.
- [ ] ac-SecondAgentLoggingNeedsNoLease: Given agent1 owns the editing lease, when agent2 logs a session action, then its local audit persists without acquiring or changing agent1's lease.
- [ ] ac-CommitPreviewBlocksSecret: Given a promoted snapshot contains a fixture secret detected by the configured detector, when commit preview runs, then422 secret_detected creates no commit and outputs no secret value.
- [ ] ac-ReceiptsAndLogsContainNoCredentials: Given unique fixture canaries are injected through protected secret references, when commands, retries and failures execute on both hosts, then scans of argv, receipts, logs, chat/audit payloads, memory stores, generated files, Git staged blobs/new commits and Dolt working sets/new history find zero canary values. Scanner diagnostics and rejected commit previews also contain zero values; no secret is added to history to test the detector.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-185: scoped authorization and offline behavior

Status: pending. Revision audit amended 2026-11-10.
Reuse integrated IdentityServer for single-user node ownership, per-node client credentials, project scopes and revocation; preserve authorized offline work.

Server: least-privilege capabilities plus server-side authorization on every call. Plugin: show available rights and explicit denial; capability display never substitutes for authorization.

**Acceptance Criteria:**
- [ ] ac-RawRemoteCannotBypassPublicationPolicy: Given A has a valid Atlas token, when A addresses the private backend across the link, then the connection fails; when a read-only token attempts a public-main mutation, then403 unauthorized_project; when a publish-authorized token bypasses publication.accept through a raw public-ref push, then409 publication_required. In all three cases remote/main remains C0.
- [ ] ac-ForeignProjectCloneDenied: Given token has Atlas.read but no SecretProject scope, when a remote fetch requests SecretProject, then403 unauthorized_project sends zero objects.
- [ ] ac-RevokedNodeCannotPublish: Given A holds a reference token, when IdentityServer disables A and revokes its grants before acceptance, then accept returns401 invalid_token and remote/main is unchanged.
- [ ] ac-OfflineLocalAuthorizationStillApplies: Given hub/IdentityServer is unreachable and a valid authorized local user, when a local TODO edit is requested, then it succeeds; an unauthorized local user still receives403.
- [ ] ac-LocalAppUserCannotPush: Given ordinary editing SQL credentials, when remote administrative procedures or public-ref writes are attempted, then permission is denied and no object publication occurs.
- [ ] ac-RevokedCredentialFailsFetchAndPublish: Given IdentityServer revocation completes, when A starts new fetch and publish requests with its old token, then both return401 invalid_token before transfer or ref mutation.
- [ ] ac-SqlPortUnreachableAcrossLink: Given both services run, when LAB probes10.42.0.143:3306 and LEGION probes10.42.0.214:3306, then both connections fail; local127.0.0.1:3306 health succeeds.
- [ ] ac-UnknownCertificatePinRejected: Given the peer uses an unapproved SPKI or wrong SAN, when control/remote TLS connects, then tls_peer_untrusted occurs before bearer transmission.
- [ ] ac-PluginLeastPrivilegeDenial: Given a plugin session has Atlas.read but lacks publish, when capabilities load and a publication is nevertheless attempted, then publish is unavailable in the capability display, the request returns403 unauthorized_project, the denial is explicit and no SQL/CLI fallback or head change occurs.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-186: Dolt fourth-provider support and schema migration

Status: pending. Revision audit amended 2026-11-10.
Support Dolt as the fourth provider with dedicated schema migration, controlled schema-history distribution, Windows/Linux lifecycle and original-provider regressions.

**Acceptance Criteria:**
- [ ] ac-FourProvidersResolve: Given keys sqlite,postgresql,sqlserver,dolt, when runtime and design-time factories resolve them, then each selects its own strategy/migration history; unknown key returns unsupported_provider.
- [ ] ac-DoltBaselineAndUpgrade: Given empty standalone Dolt then baseline schema1, when baseline and additive schema2 apply, then CRUD succeeds, data remains intact and repeated apply makes no further changes.
- [ ] ac-OriginalProviderMigrationsUnchanged: Given recorded hashes of the three original migration chains, when Dolt support is added, then those hashes match and all three real-provider upgrade fixtures pass.
- [ ] ac-UnsupportedSchemaRejected: Given application supports schema1, when opening schema2 for domain writes, then incompatible_schema is returned before mutation.
- [ ] ac-VersionedProjectNotMigratedAtStartup: Given a versioned project at schema1, when application starts, then no EF migration executes and its Dolt head/schema remain unchanged.
- [ ] ac-OldSchemaUnpublishedCommitMergesAfterUpgrade: Given B1 is unpublished on schema1 and hub S1 adds schema2 from C0, when preflight/upgrade and merge succeed, then B/main=M1 with both B1 and S1 ancestors and B1 row values preserved.
- [ ] ac-FeatureBranchSchemaCatchUp: Given main accepts S1 but feature still uses schema1, when feature is selected for writes, then it remains closed until its own merge with S1 validates.
- [ ] ac-DirtyDraftBlocksBinaryUpgrade: Given any local branch has a draft, when Nuke preflight runs, then409 maintenance_not_ready prevents binary replacement and preserves the draft.
- [ ] ac-ContractBlockedUntilLocalBranchesAttested: Given unpublished feature lacks an expand-schema attestation, when contract migration is requested, then it is refused even if all public remote branches are current.
- [ ] ac-DeploymentPreflightBlocksDirtyOrUnattestedBranch: Given a ready public main but an unattested local feature, when deployment runs, then no binary/service/schema change occurs; explicit branch attestation is required.
- [ ] ac-WindowsWriterLockAndRestart: Given one Windows worker owns Atlas, when a second process starts or the owner crashes, then concurrent ownership is denied and restart recovers only after the OS releases its lock; committed rows and unpublished heads survive.
- [ ] ac-RealHostAcceptanceMatrix: Given isolated disposable Atlas stores on LAB and LEGION and FED-ATLAS-H/A/B fixtures, when Ph8 partitions/reconnects each control, publication/history, audit and identity path and restarts each application/worker, Dolt, gateway, identity and control/audit component at commit/publication/report boundaries, then every matrix row records exact heads/ancestor sets/rows/drafts/refs/epoch and audit receipts matching the workflow outcome, with zero missing provider/migration/recovery rows. Missing any component/path row fails acceptance; unrelated live projects retain their provider mappings and data.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-187: original-provider-to-Dolt migration tool

Status: pending. Revision audit amended 2026-11-10.
Migrate all three original providers using verified seed then reviewed join, permanent identities, audit backfill and a source-retaining all-domain/all-branch project-to-SQLite recovery export.

**Acceptance Criteria:**
- [ ] ac-ThreeSourceProviderRoundTrip: Given the same fixture in SQLite,PostgreSQL,SQL Server, when seed migration runs, then counts, typed hashes, IDs, nulls, Unicode, decimals, UTC precision and FK relationships match in Dolt.
- [ ] ac-ResumeAfterBatchCommit: Given target batch1 committed before a crash but its acknowledgement was lost, when resume runs, then batch1 is recognized by journal and no duplicate rows appear.
- [ ] ac-ChangedSnapshotRejected: Given manifest source hash S0, when source is replaced by S1 before resume, then409 migration_source_changed writes no additional target rows.
- [ ] ac-FailedVerifyCannotCutover: Given one row hash differs, when cutover is requested, then409 migration_verification_failed leaves source routing active and writes closed on the target.
- [ ] ac-SourceRetainedAfterCutover: Given verified cutover completes, when target receives a new TODO, then source backup remains present, read-only and byte-identical; the new TODO exists only in Dolt until an explicit recovery export.
- [ ] ac-MigrationJoinPreservesCommonAncestor: Given seed C0 is published and join imports J1, when join completes in Ph6, then C0 is an ancestor of J1 and reviewed main M1; no second root is created.
- [ ] ac-MigrationJoinDivergentRowRequiresReview: Given seed T1.title=A and frozen join T1.title=B, when join planning runs, then an explicit keep/take/manual decision is required before J1 even if Dolt would fast-forward.
- [ ] ac-JoinCutoverRequiresReviewedMain: Given J1 is verified but main integration is unresolved, when cutover runs, then409 migration_verification_failed preserves source routing; only verified reviewed M1 permits cutover.
- [ ] ac-DoltTodoExportPreservesNewWrites: Given A1 adds TODO T2 after cutover, when export-sqlite targets A1, then the new SQLite DB contains T2 and all TODO children/aliases with matching hashes, while the original source remains unchanged.
- [ ] ac-SeedPhaseRejectsJoin: Given only Ph3 is implemented, when mode=join is requested, then501 unsupported_capability creates no target commit or routing change.
- [ ] ac-FullProjectRecoveryPreservesAllDomains: Given Atlas has main=A1 with post-cutover TODO T1, requirement R1/mapping, use case U1/actor/flow/step, memory W1, portable setting K1 and promoted session S1/turn/action, plus unpublished feature=F1 and draft T2.title=draft, when export-project freezes both branches and verifies fresh SQLite snapshots plus operational backup, then all IDs/typed values/links/soft deletes match each captured working set, feature still resolves F1, T2 remains draft without a new Dolt commit, audit receipts reconcile, the retained Dolt archive contains A1/F1 ancestry and original SQLite bytes are unchanged. Omit W1 or feature in a negative control: migration_verification_failed prevents routing switch.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-188: imported state does not repeat external effects

Status: pending. Revision audit amended 2026-11-10.
Imported project or audit state never replays external effects and general federation remains outside QuadBrain-only keyserver gating.

Server: synchronization applies data without executing external operations. Plugin: distinguish history/data synchronization from separately authorized external effects in preview, status and receipts.

**Acceptance Criteria:**
- [ ] ac-ImportDoesNotSendExternalAction: Given A1 records an already-sent GitHub update, when B integrates A1, then B state includes the result and outbound GitHub/email invocation count is0.
- [ ] ac-RetryDoesNotRepeatEffect: Given accepted commit A1 and effect E1, when sync retries10 times, then the effect ledger still has one E1 and no new external dispatch occurs.
- [ ] ac-ImportedOperationalRowsRemainInert: Given audit backfill contains command names and receipt payloads, when the hub ingests them, then they are stored as evidence and no business command executes.
- [ ] ac-FederationBypassesQuadBrainKeyserver: Given QuadBrain coordinator is degraded but local authorization passes, when federation commit/control/audit operations execute, then none invokes the keyserver and valid local work succeeds.
- [ ] ac-PluginEffectBoundary: Given incoming A1 contains a TODO completed state and inert historical external-action metadata, when the plugin shows synchronization success, then it labels data received with zero external operations executed; requesting an external operation requires a separate explicit authorized command and its own receipt.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-189: safe shared-host editing and commit ownership

Status: pending. Revision audit amended 2026-11-10.
Enforce branch edit ownership and fenced live-write serialization so no agent or sync worker captures or overwrites another agent draft.

Server: editing owner, request/release and fenced takeover contracts. Plugin: display editing owner, request/release context and offer another branch without capturing another agent draft.

**Acceptance Criteria:**
- [ ] ac-CommitCannotCaptureOtherAgentDraft: Given agent1 owns a draft, when agent2 commits the same branch, then409 branch_busy creates no commit and the draft remains agent1-owned.
- [ ] ac-ExpiredLeaseFenced: Given fence4 expires and fence5 is issued, when a fence4 edit arrives, then409 lease_lost persists no row changes.
- [ ] ac-PreviewChangeRequiresReview: Given preview fingerprint W1, when the working set becomes W2, then commit with W1 returns409 working_set_changed and requires a fresh preview.
- [ ] ac-LeaseLostDuringWriteRejected: Given a write admitted under fence4, when expiry occurs mid-transaction, then that write finishes under the gate before fence5 takeover; a later fence4 write is rejected.
- [ ] ac-CommitRetryReturnsSameHash: Given request R1 created A1 and reply was lost, when R1 retries after restart, then A1 is returned by its commit trailer and history contains no duplicate commit.
- [ ] ac-IncomingMergeCannotRaceAdmittedEdit: Given an edit holds the branch gate, when the integration worker requests a live head move, then it waits; after the edit finishes, fingerprint revalidation detects the draft and returns waiting_local_work without overwriting it.
- [ ] ac-PluginEditingOwnership: Given agent1 owns A/main fence4 with a draft, when agent2 views context and requests editing, then it sees the owner and branch, receives409 branch_busy, and may select a separate feature branch; owner release/takeover requires explicit authorization and preserves the main draft without commit or discard.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-AUDIT-001: Permanent audit with append-only hub delivery

Status: pending

Audit remains permanent locally and is delivered to the hub append-only unversioned store independently of project commits. Retain immutable runtime roles, readable legacy payloads and versioned compressed snapshots/diffs/metadata. Audit is excluded from branchable Dolt project history and wiki exports, not from hub delivery. Backfill TODO and generic audit history with verified hashes and receipts.

- [ ] review20261110-1: Given offline audit E1, when reconnect occurs without a version commit, then the hub stores E1 once and the node retains it.
- [ ] review20261110-2: Given runtime storage credentials, when update/delete of any existing audit row is attempted, then it is denied.
- [ ] review20261110-3: Given legacy and new compressed payload fixtures, when read/migrated on all supported providers, then their decoded values and historical text remain unchanged.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-FED-BACKFILL-001: Complete verified project and audit migration backfill

Status: pending

Inventory every prior domain: TODO and children/links/doc metadata/audit; complete session graph; requirements/AC/mappings/scope layers; memory/versions; use cases/actors; source documents and derived chunks; triage; tools/buckets; agents; brain slots; handoff. Each maps to versioned data, hub-bound unversioned audit, host-local operational state or regenerable derived state with explicit counts/reason. Seed/join replace signed command backfill for versioned mode. Reads and writes always remain local; no read-switch.

- [ ] review20261110-1: Given the domain manifest, when migration planning runs, then every source table/writer has a classified destination or explicit exclusion and no unknown domain passes.
- [ ] review20261110-2: Given completed batches, when rerun, then no duplicate rows or external effects occur and already_applied counts are reported.
- [ ] review20261110-3: Given a missing source row or audit receipt, when verify runs, then migration_verification_failed blocks cutover.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-FED-ROUTE-001: Federation project routing fails closed

Status: pending. Revision audit amended 2026-11-10.
Current versioned control/remote endpoints resolve immutable ProjectId and authenticated NodeId membership; local paths are host-specific mappings, never authority or fallback to the hub primary workspace. Preserve the old proxy-header routing contract only for explicitly configured legacy DirectProxy paths.

**Acceptance Criteria:**
- [ ] review20261110-1: Given A is enrolled in Atlas, when its control request omits ProjectId or requests another project, then400 invalid_request or403 unauthorized_project returns no default-workspace data.
- [ ] review20261110-2: Given legacy proxy routing is explicitly selected, when valid normalized path or GlobalWorkspaceId is supplied, then both resolve the same scoped workspace.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-FED-WSSYNC-001: Portable project configuration and local machine boundaries

Status: pending

Version portable project configuration through explicit commits; retain host RunAs, executable paths, DataDirectory, tunnel settings and local filesystem mappings locally. CurrentRequirementLayerKey remains protected hub-admin-owned policy: heartbeat cannot overwrite it and drift is visible. Local offline work uses the last authorized policy. No blanket host-configuration replication.

- [ ] review20261110-1: Given A changes host RunAs or DataDirectory, when heartbeat/clone runs, then those values never replace H configuration.
- [ ] review20261110-2: Given CurrentRequirementLayerKey differs, when heartbeat runs, then hub-owned value remains and drift is reported.
- [ ] review20261110-3: Given an authorized portable prompt-template edit is committed, when synced, then peers receive its sanitized value.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### FR-MCP-MEMORY-008: Versioned workspace memory federation

Status: pending

Workspace-scoped portable memories and their versions are committed project data; local edits work offline and explicit version commits trigger exchange. Global/machine-specific memory remains operational. Conflicting updates/deletes require review. Preserve valid MEMORY-* identity, explicit scope and sanitizer boundaries. LocalProxy forwarding and signed command replay are superseded for this mode.

- [ ] review20261110-1: Given workspace memory M1 edited offline, when saved, then the local read changes but remote C0 does not; after explicit commit and sync the peer receives the version.
- [ ] review20261110-2: Given A deletes and B edits M1, when merged, then needs_review preserves both histories.
- [ ] review20261110-3: Given a global memory containing machine configuration, when cloning a project, then that memory is absent.

Definition source: docs/Project/federation-dolt/Functional-Requirements.md.

#### TR-MCP-AUDIT-004: Append-only unversioned audit delivery and export policy

Status: pending

Project history excludes secrets and automatic operational audit. This does NOT make audit local-only: every host retains permanent local audit and an append-only outbox delivers sanitized records to the hub unversioned ledger independently of project commits. AuditRecord key=(OriginNodeId,RecordId UUID); fields ProjectId,EntityKind,EntityId,OriginSequence int64,OccurredUtc,Actor,Action,PayloadVersion,PayloadHash,Payload. POST audit/batches validates token/project, inserts atomically by key and returns per-record durable receipts. Same key/hash is already_received; same key/different hash=409 audit_id_conflict. No UPDATE/DELETE permission. Local pruning is forbidden. Migration backfills TODO and generic audit history with counts/hashes and receipts. Credential values are excluded at capture, not silently replicated.

- [ ] review20261110-1: Given batchE1 was accepted and its reply lost, when retried, then one row and the same payload-hash receipt remain.
- [ ] review20261110-2: Given E1 key with a changed payload, when submitted, then409 audit_id_conflict performs no overwrite.
- [ ] review20261110-3: Given a wiki export or project clone, when generated, then it excludes credentials and operational audit rows while independent hub delivery remains enabled.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FED-001: Autonomous federation transport and compatibility contract

Status: pending

Use TR-MCP-FEDERATION-001..016 as the current versioned contract. Configure immutable ProjectId,NodeId,branch,IdentityServer client and hub gateway; never forward ordinary Dolt-project requests. Preserve legacy DirectProxy routing and loop protection in a distinct mode. Never run legacy mutation replay and versioned publication for the same project.

- [ ] review20261110-1: Given versioned mode, when a TODO request arrives, then LocalProxy middleware is not invoked.
- [ ] review20261110-2: Given explicit DirectProxy mode, when a foreign workspace is addressed, then correct routing and loop protection remain intact.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FED-BACKFILL-001: Seed, join and audit backfill contracts

Status: pending

Use TR-MCP-FEDERATION-014 seed in Ph3 and join after review/publication in Ph6. Audit delivery follows TR-MCP-AUDIT-004. Manifest preserves typed values, logical IDs/aliases, versions, timestamps and full session graph. Per-table stable keyset batches and immutable source fingerprint support resume. Import never fans out original commands; only the reviewed version commit may publish.

- [ ] review20261110-1: Given migration J1 contains all source rows and audit records, when verify executes, then source accounting at J1 and final invariants at M1 both pass, with durable audit receipts.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FED-MEMORY-001: Memory scope and history contract

Status: pending

Route the existing memory API locally. Version workspace-scoped memory rows and version graph through the Dolt project context; keep global/machine memory in operational storage. Validate memory identity and scope on local writes and merge. No separate replay adapter is required for Dolt mode; retain explicit legacy adapter compatibility only.

- [ ] review20261110-1: Given MemoryId M1 with versions V1,V2 in Atlas, when A1 is cloned, then both versions and their links match and no global memory is included.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FED-ROUTE-001: Scoped project resolution and legacy route isolation

Status: pending. Revision audit amended 2026-11-10.
Versioned APIs require ProjectId and derive NodeId from IdentityServer client membership. Missing project=400 invalid_request; foreign project=403 unauthorized_project. Request paths use the syntax-aware normalizer, not host Path.GetFullPath. Legacy header lookup remains mode-scoped and never supplies fallback identity to versioned routes.

**Acceptance Criteria:**
- [ ] review20261110-1: Given a federation hop with unresolved workspace in legacy mode, when routed, then a4xx diagnostic is returned and no primary workspace fallback occurs.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FED-WSSYNC-001: Configuration partition and protected policy contract

Status: pending

Project manifest classifies fields individually. Portable Name/prompt-template content may be versioned; IsPrimary,RunAs,AgentPath,DataDirectory,TunnelProvider and resolved TodoPath are host-local. Protected CurrentRequirementLayerKey changes require hub-admin permission and a versioned policy commit, never heartbeat overwrite. Legacy payload compatibility is tested only in legacy mode.

- [ ] review20261110-1: Given older legacy registration lacks fields, when processed, then its existing mode defaults remain without mutating versioned project settings.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-001: autonomous local execution contract

Status: pending. Revision audit amended 2026-11-10.
Local request routing binds immutable ProjectId, NodeId and Branch to one DbContext and branch write gate. Dolt-mode domain requests never invoke LocalProxy forwarding or wait for the hub. Existing local authentication remains mandatory; federation reference-token introspection is not a dependency of local work. Result fields distinguish local persistence, version commit and publication.

**Acceptance Criteria:**
- [ ] ac-NodeWorksWithHubUnavailable: Given A/main=C0 and the hub link is down, when A changes TODO T1.title from old to local, then the local API succeeds, a local reread returns local, and no hub request or history commit occurs.
- [ ] ac-HubUsesSameLocalExecution: Given H/main=C0 and the gateway is stopped, when H edits T1.title, then H persists it locally while remote/main remains C0.
- [ ] ac-OfflineLocalReadAfterWrite: Given the prior offline edit, when the application and Dolt restart, then T1.title remains local and main remains C0 until an explicit version commit.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-002: explicit version commits and publication boundary contract

Status: pending. Revision audit amended 2026-11-10.
POST /qbrainai/federation/projects/{projectId}/branches/{branch}/commits accepts requestId UUID, expectedHead, expectedWorkingSet, leaseFence, author and message. Success is 201 with commitHash and publicationState=committed_locally. Persist requestId in the Dolt commit trailer. Retry returns the original hash. Head/fingerprint/fence mismatch is HTTP409 with head_changed/working_set_changed/lease_lost. SaveChanges and control.poll never create version commits.

**Acceptance Criteria:**
- [ ] ac-SaveDoesNotPublish: Given main=C0 and remote=C0, when SaveChanges persists T1.title=draft, then main and remote stay C0 and no publication intent is emitted.
- [ ] ac-VersionCommitTriggersSync: Given that draft and a valid preview, when commit request R1 succeeds, then main=A1, the reply says committed_locally, and one recoverable publication intent targets A1.
- [ ] ac-HubRequestDoesNotCommitDraft: Given B has a draft and desired head A1, when B polls, then outcome=waiting_local_work, B/main=C0 and the draft value is unchanged.
- [ ] ac-OfflineCommitDiscoveredAfterRestart: Given A1 committed while disconnected and a crash before intent persistence, when A restarts, then history scanning rediscovers A1 exactly once and publishes it after reconnect.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-003: logical project enrollment and initial clone contract

Status: pending. Revision audit amended 2026-11-10.
ProjectEnrollment stores ProjectId UUID, NodeId, ownerSubjectId, normalized local path, schemaVersion and subscriptions. POST /qbrainai/federation/enroll accepts an authenticated pre-provisioned node client plus operator-authorized owner binding. Body ProjectId must be allowed by token scope fed.project.{ProjectId}.enroll and durable membership. Paths stay host-local; no default-workspace fallback. A nonempty clone destination returns409 target_not_empty.

Server: project discovery, scoped enrollment and structured refusal diagnostics. Plugin: project discovery/selection, enrollment progress and explicit actionable diagnostics.

**Acceptance Criteria:**
- [ ] ac-DifferentHostPathsSameProject: Given LAB path /home/sharpninja/github/McpServer and LEGION path F:/github/McpServer, when both enroll as ProjectId Atlas, then both reference Atlas and retain their distinct local paths.
- [ ] ac-CloneRefusesNonemptyTarget: Given target has TODO X, when clone is requested, then409 target_not_empty leaves X and all target files unchanged.
- [ ] ac-UnauthorizedProjectDenied: Given node A has only Atlas membership, when it enrolls or clones SecretProject, then403 unauthorized_project returns no rows, objects or project existence details.
- [ ] ac-PluginEnrollmentDiagnostics: Given Atlas is discoverable but A has a nonempty destination, when each plugin selects Atlas and attempts enrollment/clone, then it displays ProjectId, selected branch and409 target_not_empty with remediation; no database is overwritten and no different project is selected.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-004: branches across server and plugin context contract

Status: pending. Revision audit amended 2026-11-10.
Branch APIs create/list/select/merge using ProjectId, branch and sessionId. Each pooled connection is opened for an explicit branch and verified before use. Merge accepts expectedSourceHead and expectedTargetHead; stale values yield409 head_changed. Reserved refs upload/, integration/ and review/ are inaccessible to ordinary editing credentials. Branch deletion fails409 branch_busy while leases, reviews or recovery pins reference it.

**Acceptance Criteria:**
- [ ] ac-PooledConnectionDoesNotLeakBranch: Given main T1.title=main and feature/search T1.title=feature, when alternating pooled reads run100 times, then each returns only its selected value.
- [ ] ac-FeaturePublicationLeavesMainUnchanged: Given main=C0 and feature/search=F1, when F1 publishes, then remote feature=F1 and all main heads remain C0.
- [ ] ac-BranchMergeRequiresExpectedHeads: Given a preview for main=C0 and feature=F1 but feature advances to F2, when merge submits the old preview, then409 head_changed creates no commit and changes no ref.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-005: bidirectional committed-history exchange contract

Status: pending. Revision audit amended 2026-11-10.
Node upload authority is limited to its candidate namespace in an isolated upload repository. POST /qbrainai/federation/publication/accept accepts projectId, branch, requestId, candidateHead, expectedPublishedHead and hubEpoch. Gateway validates authorization, ancestry, schema and domain invariants before the sole fenced publisher advances the public ref without force. Token validation occurs at the gateway; native Dolt remotesapi is loopback-only and never trusts a node bearer token directly. Transport/procedure enforcement is a Ph0 proof gate.

**Acceptance Criteria:**
- [ ] ac-ThreeWriterCleanConvergence: Given C0 contains T1.title=old,T1.description=old,T2.priority=low, when A1 changes title=A, B1 changes description=B and H1 changes T2.priority=high, then merges M1(A1,B1), M2(M1,H1) converge all heads to M2 with values A,B,high and all three commits in ancestry.
- [ ] ac-HubDraftDoesNotBlockRemote: Given H has an uncommitted title draft at C0, when A publishes A1, then remote=A1, H remains C0 with its draft, and B can accept A1.
- [ ] ac-ConcurrentPublishPreservesBothHistories: Given A1 and B1 descend from C0, when both accept requests expect C0, then one succeeds and the other receives409 head_changed; after merge M1 publishes, both A1 and B1 remain ancestors.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-006: hub control table and polling contract

Status: pending. Revision audit amended 2026-11-10.
SyncSubscription key=(NodeId,ProjectId,Branch) contains HubEpoch UUID, PublishedHead, PublishedGeneration int64, DesiredGeneration int64, ReportedGeneration int64, WorkerEpoch int64, AcceptedHead, Outcome and timestamps. POST control/poll is read-only. POST control/report includes all epochs and observed generation; stale worker=409 stale_worker_epoch, stale hub=409 hub_epoch_changed. Poll every30s with20% jitter; commit/reconnect/on-demand wake immediately. Backoff1s doubling to60s. Generation ordering is valid only within HubEpoch. Restore generates a new non-reused random epoch before listeners reopen.

**Acceptance Criteria:**
- [ ] ac-PollReadDoesNotAcknowledge: Given desired=7 and reported=6, when poll runs twice, then both responses request7 and stored reported remains6.
- [ ] ac-AckOldGenerationPreservesNewRequest: Given desired becomes8 while report7 is in flight, when report7 arrives, then reported=7, desired=8 and the subscription remains pending.
- [ ] ac-MissedNotificationReconciles: Given remote=A1 but control still names C0 after a crash, when reconciliation runs, then it records A1 and advances desired generation without producing another commit.
- [ ] ac-HubRestoreFromOlderBackup: Given a node remembers epoch E1 generation90 and the hub restores a backup at generation40, when the hub reopens, then epoch E2 differs from E1; E1 reports fail409 hub_epoch_changed and no head is force-reset. Node A1 is retained until verified ancestry recovery republishes missing history.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-007: reviewed conflicts and valid merges contract

Status: pending. Revision audit amended 2026-11-10.
Review record key=ReviewId UUID; fields ProjectId,Branch,baseHeads[],localHead,incomingHead,candidateRef,conflictingCells[],validationErrors[],createdUtc,lastNotifiedUtc,resolution,actor. GET conflicts and status expose it. Resolve binds exact heads and returns409 stale_review if changed. Safe TODO policy: identical concurrent values collapse; edits to different cells may merge only if invariants pass; duplicate immutable child IDs deduplicate only when bytes agree. Status, priority, title, description, dependency deletions and same-cell disagreements always require review; no timestamp last-writer-wins. Notify immediately in status and durable operator inbox; warn at15min, escalate at24h, repeat at most hourly; no email/Slack sent implicitly. Same-branch dependent commits wait; unrelated branches/projects continue.

**Acceptance Criteria:**
- [ ] ac-SameCellConflictNeedsReview: Given base priority=low, A1 priority=high and B1 priority=medium, when B integrates A1, then needs_review shows low/medium/high, B/main remains B1 and no public ref is overwritten.
- [ ] ac-DeleteUpdateNeedsReview: Given A1 deletes T1 and B1 edits T1.title, when histories merge, then needs_review retains B1 and the deleted/edited alternatives for review.
- [ ] ac-CleanMergeCanFailDomainValidation: Given separate commits add T1 dependsOn T2 and T2 dependsOn T1, when Dolt merges without cell conflicts, then domain validation records dependency_cycle and leaves the prior valid live head unchanged.
- [ ] ac-StaleReviewRejected: Given a resolution preview binds local=B1 and incoming=A1, when incoming advances to A2, then resolve returns409 stale_review with no merge commit.
- [ ] ac-ReviewAgeVisible: Given unresolved needs_review at12:00, when the clock reaches12:15, then plugins/status and the operator inbox show warning with ReviewId and age900s; at24h severity is critical, with at most one repeat per hour.
- [ ] ac-UnrelatedPublicationContinues: Given Atlas/main is blocked by review R1, when Atlas/feature publishes F2 and another project publishes X1, then both succeed while Atlas/main remains unchanged.
- [ ] ac-SafeTodoMergeRules: Given A changes title and B changes description with no domain violation, when merge runs, then both values survive; competing priority or completion edits produce needs_review instead of choosing a timestamp winner.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-008: recovery without lost or repeated work contract

Status: pending. Revision audit amended 2026-11-10.
OperationReceipt binds requestId, node, project, branch, hubEpoch, candidate/result hashes and outcome. Durable refs pin candidates/reviews before receipt completion. Worker claims are fenced by WorkerEpoch; reconnect never trusts stored generation across HubEpoch. Fetch may advance remote-tracking refs, never dirty live refs. Restore first disables ingress, restores compatible data/control/audit checkpoints, creates new epoch, then re-advertises verified heads; node-held missing commits are uploaded and ancestry-validated before normal synchronization resumes.

Server: durable operation resume/status/safe cancellation. Plugin: expose resume, status and cancel with completed-versus-pending effects; never claim cancellation reverses published history.

HubRestoreFromOlderBackup is first owned by Ph5 (epoch/remote/control), extended by Ph6 (integration/review/audit resend), and repeated as isolated real-host acceptance in Ph8. Restore is tested, never assumed from successful backup creation.

**Acceptance Criteria:**
- [ ] ac-LostPushAckDoesNotDuplicateCommit: Given remote accepted A1 and reply was lost, when request R1 retries, then it returns A1 and the existing receipt; no A2 commit is created.
- [ ] ac-OldWorkerEpochCannotReportSuccess: Given worker epoch changes4 to5, when epoch4 reports success, then409 stale_worker_epoch leaves current receipt and desired state unchanged.
- [ ] ac-DirtyBranchDefersApply: Given B/main=C0 with title=draft, when A1 is fetched, then outcome=waiting_local_work, main stays C0 and title remains draft; at15min status/inbox shows the age warning.
- [ ] ac-IncompatibleSchemaDoesNotApply: Given B supports schema1 and incoming S1 requires schema2, when sync runs, then409 incompatible_schema leaves B's live data/head unchanged and records the required version.
- [ ] ac-PluginRecoveryControls: Given operation R1 published A1 but its local reply was lost, when a plugin resumes, reads status and requests cancellation, then it shows publishedHead=A1 and a durable cancellation outcome for remaining work; it never claims A1 was rolled back or creates a second commit.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-009: collision-resistant and preserved identity contract

Status: pending. Revision audit amended 2026-11-10.
Generated IDs use immutable two-character creator tags and durable branch-independent sequences with per-family grammar fixed in Ph0. Alias key=(ProjectId,OriginNodeId,EntityKind,LegacyId) maps permanently to CanonicalId; ambiguous unqualified lookup returns409 ambiguous_legacy_id. Historical text/commits are not rewritten. Migration duplicate candidates use normalized title/description/dependency features, returning similarity score plus evidence, never auto-merging. Human ID reservations are unique(ProjectId,EntityKind,HumanId) at the hub, bound to node/requestId. Offline nodes use generated provisional IDs; an unreserved human ID is not publishable.

Server: provenance, scoped aliases, collision evidence and reservation outcomes. Plugin: expose provenance/collision diagnostics and the authorized resolution path without rewriting historical references.

**Acceptance Criteria:**
- [ ] ac-TwoNodesCreateDistinctIds: Given tags LA and LG and both counters start1, when both create a TODO, then generated IDs differ and both records survive reconciliation.
- [ ] ac-BranchRevertDoesNotReuseSequence: Given LA allocated sequence42, when main reverts and another branch creates a record, then its sequence is greater than42.
- [ ] ac-LegacyCollisionPreservesReferences: Given LAB and LEGION have different entities named TODO-7, when migration resolves the collision, then scoped aliases resolve each original identity and every relational reference follows its reviewed canonical mapping; historical text remains byte-identical.
- [ ] ac-LegacyIdsRemainValid: Given existing IMPL-UNITGREEN-001 and old TODO references, when imported and read by their original IDs, then they resolve through permanent aliases without rewriting commits, PR text, session logs, memories or documents.
- [ ] ac-DifferentIdsSameTodoDetected: Given LAB TODO-7 and LEGION TODO-91 share normalized title Fix login, description and dependency set, when migration plan runs, then it emits a duplicate candidate with both IDs and evidence; until explicit review, neither is merged or deleted.
- [ ] ac-SimultaneousHumanIdReservation: Given human ID IMPL-UNITGREEN-001 is unreserved, when two nodes reserve it concurrently, then exactly one receives201 and the other409 human_id_reserved; retry by the winning requestId returns the same reservation.
- [ ] ac-OfflineHumanIdPublicationDenied: Given an offline-created provisional TODO requesting an unreserved human ID, when it reconnects and publishes, then409 human_id_unreserved retains the local record until reservation or reviewed alias resolution.
- [ ] ac-PluginIdentityDiagnostics: Given LAB TODO-7 and LEGION TODO-91 are a duplicate candidate, when each plugin opens identity review, then both origin IDs, canonical/provisional IDs, similarity evidence and reservation outcome are visible; no alias or row changes before an authorized decision.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-010: first-party plugin federation contract contract

Status: pending. Revision audit amended 2026-11-10.
All plugins call typed server/client/MCP/REPL contracts; no raw SQL/CLI fallback. Status DTO includes projectId, branch, localHead, publishedHead, observedUtc, dirty, ahead, behind, publicationState, incomingTarget, connectivity, reviewCount, oldestReviewAgeSeconds, waitingLocalWorkAgeSeconds, auditPendingCount, hubEpoch, workerEpoch. Capability contract version1 is negotiated; unknown version=409 unsupported_contract. Administrative maintenance.preflight/attest/status and migration commands share the same typed service as Nuke. Stable error envelope={code, message, requestId, projectId, branch, retryable, details}; status codes are locked in the technical contract appendix.

Technical contract appendix (TR-MCP-FEDERATION-010): all below routes are under /qbrainai/federation unless the commits route specifies the project/branch path. Every mutation requires requestId UUID and authenticated subject; node network requests additionally carry the project-scoped reference token. Responses carry contractVersion=1 and the stable error envelope. Path/body ProjectId mismatch is403 unauthorized_project. No endpoint accepts credentials in URL, argv or persisted receipts.

Read/status routes: GET capabilities returns200 {contractVersion,capabilities}; GET projects/{projectId}/status returns200 the status DTO; GET projects/{projectId}/branches returns200 {items:[{name,head,dirty,publishedHead}]}; GET conflicts and GET conflicts/{reviewId} return200 review records filtered by membership. POST control/poll returns200 {hubEpoch,subscriptions}; reads never acknowledge work. POST control/report validates observed generation and worker epoch, returns200 the durable receipt or409 the named fence error. Poll/report use control scope; reads use read scope.

Mutation routes: POST enroll returns201 the enrollment (or200 the same idempotent enrollment); POST projects/{projectId}/branches creates201 {branch,head}; POST projects/{projectId}/branches/select returns200 {sessionId,branch,head} after local lease validation; POST projects/{projectId}/branches/{branch}/commits returns201 {commitHash,publicationState}, or200 the same commit on retry. POST publication/accept returns200 {requestId,hubEpoch,publishedHead,publishedGeneration,receiptId} only after the guarded ref update is durable. POST branches/merge and POST conflicts/{reviewId}/resolve bind expected heads and return201 the new commit; a conflict returns202 {outcome:needs_review,reviewId} while the live head stays unchanged. Share/merge/publication require publish scope plus branch permission. Enrollment requires enroll scope and the operator owner-binding approval.

POST audit/batches accepts {requestId,projectId,records:[AuditRecord]}, returns200 {receipts:[{originNodeId,recordId,payloadHash,outcome,receivedUtc}]}, and requires audit scope. Every record must match the token/project and batch ProjectId. Any invalid record rejects the whole batch before insert. Exact duplicate is already_received; payload mismatch is409 audit_id_conflict. POST ids/reservations accepts {requestId,projectId,entityKind,humanId,nodeId}, requires publish scope and returns201 the reservation or200 its exact retry; another owner gets409 human_id_reserved. POST maintenance/preflight and POST maintenance/attest, GET maintenance/status and POST migration/{plan|dry-run|run|resume|verify|export-project|export-todos|cutover|abort}, GET migration/status are operator-only local service operations. They are exposed through typed clients and Nuke, never public node-admin capabilities. A long migration returns202 {migrationId,status}; status/verify return200 a receipt, and a denied readiness check never performs cutover.

Stable failure mapping:401 invalid_token;403 unauthorized_project;409 head_changed, working_set_changed, lease_lost, branch_busy, stale_review, stale_worker_epoch, hub_epoch_changed, ambiguous_legacy_id, human_id_reserved, human_id_unreserved, target_not_empty, maintenance_not_ready, migration_source_changed, migration_verification_failed, incompatible_schema, unsupported_contract, publication_required, request_id_conflict, audit_id_conflict and waiting_local_work (the last also a 200 outcome in poll/report; see outcome fields);422 secret_detected, dependency_cycle and unsupported_provider;501 unsupported_capability;503 identity_unavailable or remote_unavailable. TLS rejection produces the local transport code tls_peer_untrusted with no HTTP response and no request sent. Missing required fields return400 invalid_request. Missing authorized resources return404 not_found; project authorization precedes existence disclosure. Malformed/unsupported token is401; valid token lacking project rights is403.

Outcome fields are not HTTP errors: committed_locally, already_received, already_applied, waiting_local_work and needs_review remain visible through200 status/report responses (already_applied is the migration-journal idempotency outcome for a rerun completed batch, distinct from the audit-ledger already_received). A requested live mutation blocked by drafts returns409 waiting_local_work; an integration that durably creates review returns202 needs_review. dependency_cycle is a validationErrors entry on that review as well as422 for a directly invalid local mutation. retryable=true is restricted to transient503/transport failures; all stale-head/fence/review errors require reread and a fresh validated action. Idempotent retries with the same requestId but different payload return409 request_id_conflict. No automatic retry creates a different commit.

Test fixtures capture actual content hashes as symbols C0,A1,B1,H1,M1,M2,S1 rather than hardcoding unstable engine hashes. Each symbol is immutable within the run manifest. Given/When/Then compares exact hash equality, specified parent sets, row values, status codes and error codes against that manifest; a successful result must never be inferred solely from an empty queue. Capture UTC timestamps and a fake clock for exact15-minute/24-hour age boundaries. All plan phase IDs use Ph0-Ph8; priority values remain separate.

Plugin completeness is checked by PluginEnrollmentDiagnostics, PluginRecoveryControls, PluginIdentityDiagnostics, PluginLeastPrivilegeDenial, PluginEffectBoundary and PluginEditingOwnership in their mapped contracts. Ph7 owns their plugin bindings; earlier phases own server behavior. Standalone and explicit DirectProxy compatibility remain under FR-MCP-077/TR-MCP-FED-001; this does not reinstate versioned LocalProxy forwarding.

**Acceptance Criteria:**
- [ ] ac-EveryPluginUsesSameContract: Given one fixture for commit R1, conflict R2 and migration R3, when each installed first-party plugin calls its wrapper, then normalized IDs, heads, error codes and audit agent identity match the typed client result.
- [ ] ac-OldServerReportsUnsupportedCapability: Given server lacks federation contract1, when a plugin invokes commit, then unsupported_contract is shown and no raw SQL or shell fallback executes.
- [ ] ac-ReconnectRevalidatesContext: Given a cached leaseFence4 and session branch feature, when reconnect discovers fence5, then the plugin retains visible context but refuses writes with lease_lost until reacquired.
- [ ] ac-StaleBranchReportedNotDeleted: Given a branch observation is older than24h, when status is displayed, then stale=true and observedUtc are visible; the branch, drafts and refs are not deleted.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-011: portable data versus host-local state contract

Status: pending. Revision audit amended 2026-11-10.
Project history excludes secrets and automatic operational audit. This does NOT make audit local-only: every host retains permanent local audit and an append-only outbox delivers sanitized records to the hub unversioned ledger independently of project commits. AuditRecord key=(OriginNodeId,RecordId UUID); fields ProjectId,EntityKind,EntityId,OriginSequence int64,OccurredUtc,Actor,Action,PayloadVersion,PayloadHash,Payload. POST audit/batches validates token/project, inserts atomically by key and returns per-record durable receipts. Same key/hash is already_received; same key/different hash=409 audit_id_conflict. No UPDATE/DELETE permission. Local pruning is forbidden. Migration backfills TODO and generic audit history with counts/hashes and receipts. Credential values are excluded at capture, not silently replicated.

**Acceptance Criteria:**
- [ ] ac-CloneOmitsSecrets: Given credentials and local machine paths exist in operational storage, when a project is cloned, then none appears in its versioned tables or history.
- [ ] ac-AuditReachesHub: Given A performs an offline TODO edit and creates audit E1, when connectivity returns without a Dolt commit, then hub audit contains exactly E1, A retains E1, and main/remote remain C0.
- [ ] ac-AuditRetryIdempotent: Given E1 was stored but its receipt was lost, when the same batch retries, then exactly one E1 exists and the same hash receipt returns; changed payload under E1 returns409 audit_id_conflict.
- [ ] ac-AuditBackfillVerified: Given source has100 TODO audit and20 generic audit rows, when seed/join audit backfill verifies, then all120 records have matching payload hashes and hub receipts; missing one blocks cutover.
- [ ] ac-FullProjectSessionGraphPreserved: Given an explicitly promoted sanitized session with turns, actions, dialog, commits/files, PlanFile and TodoId, when cloned, then every promoted child and relationship matches the source snapshot; automatic capture still uses the audit channel.
- [ ] ac-AcceptedHeadRebuildsBranchIndex: Given accepted head changes C0 to A1, when the derived index rebuild completes, then queries read A1 values and no C0 cache is served under the A1 key.
- [ ] ac-BackgroundWritersDoNotDirtyVersionedBranch: Given main=C0 is clean, when heartbeat, audit and automatic session capture run, then Dolt working-set fingerprint remains unchanged.
- [ ] ac-SecondAgentLoggingNeedsNoLease: Given agent1 owns the editing lease, when agent2 logs a session action, then its local audit persists without acquiring or changing agent1's lease.
- [ ] ac-CommitPreviewBlocksSecret: Given a promoted snapshot contains a fixture secret detected by the configured detector, when commit preview runs, then422 secret_detected creates no commit and outputs no secret value.
- [ ] ac-ReceiptsAndLogsContainNoCredentials: Given unique fixture canaries are injected through protected secret references, when commands, retries and failures execute on both hosts, then scans of argv, receipts, logs, chat/audit payloads, memory stores, generated files, Git staged blobs/new commits and Dolt working sets/new history find zero canary values. Scanner diagnostics and rejected commit previews also contain zero values; no secret is added to history to test the detector.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-012: scoped authorization and offline behavior contract

Status: pending. Revision audit amended 2026-11-10.
Reuse existing Duende IdentityServer. Provision client fed.node.{NodeId} per single-user node, bound to ownerSubjectId through authenticated operator enrollment. Client credentials issue short-lived reference access tokens; audience qbrain-federation and scopes fed.project.{ProjectId}.{read|publish|control|audit|enroll}; protected actions additionally require server membership/branch permission. Gateway introspects without positive caching for each new request and rechecks before publication commit; unavailable identity returns503 identity_unavailable. Revoke through IdentityServer: disable client and remove its persisted reference grants. Gateway rejects inactive tokens401 invalid_token; do not invent a revocation ledger. Native remotesapi uses gateway-held credentials over loopback; clients cannot access it directly. Ph0 proves token injection/relay including redirects/object URLs, project isolation and no public-ref bypass.

Server: least-privilege capabilities plus server-side authorization on every call. Plugin: show available rights and explicit denial; capability display never substitutes for authorization.

Ph0 B5 binds the exact restored Duende 7.4 patch and matching source-tag/API references, and proves issuance, introspection, grant removal and revocation against that binary. General/v8 documentation alone cannot close B5.

**Acceptance Criteria:**
- [ ] ac-RawRemoteCannotBypassPublicationPolicy: Given A has a valid Atlas token, when A addresses the private backend across the link, then the connection fails; when a read-only token attempts a public-main mutation, then403 unauthorized_project; when a publish-authorized token bypasses publication.accept through a raw public-ref push, then409 publication_required. In all three cases remote/main remains C0.
- [ ] ac-ForeignProjectCloneDenied: Given token has Atlas.read but no SecretProject scope, when a remote fetch requests SecretProject, then403 unauthorized_project sends zero objects.
- [ ] ac-RevokedNodeCannotPublish: Given A holds a reference token, when IdentityServer disables A and revokes its grants before acceptance, then accept returns401 invalid_token and remote/main is unchanged.
- [ ] ac-OfflineLocalAuthorizationStillApplies: Given hub/IdentityServer is unreachable and a valid authorized local user, when a local TODO edit is requested, then it succeeds; an unauthorized local user still receives403.
- [ ] ac-LocalAppUserCannotPush: Given ordinary editing SQL credentials, when remote administrative procedures or public-ref writes are attempted, then permission is denied and no object publication occurs.
- [ ] ac-RevokedCredentialFailsFetchAndPublish: Given IdentityServer revocation completes, when A starts new fetch and publish requests with its old token, then both return401 invalid_token before transfer or ref mutation.
- [ ] ac-SqlPortUnreachableAcrossLink: Given both services run, when LAB probes10.42.0.143:3306 and LEGION probes10.42.0.214:3306, then both connections fail; local127.0.0.1:3306 health succeeds.
- [ ] ac-UnknownCertificatePinRejected: Given the peer uses an unapproved SPKI or wrong SAN, when control/remote TLS connects, then tls_peer_untrusted occurs before bearer transmission.
- [ ] ac-PluginLeastPrivilegeDenial: Given a plugin session has Atlas.read but lacks publish, when capabilities load and a publication is nevertheless attempted, then publish is unavailable in the capability display, the request returns403 unauthorized_project, the denial is explicit and no SQL/CLI fallback or head change occurs.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-013: Dolt fourth-provider support and schema migration contract

Status: pending. Revision audit amended 2026-11-10.
Provider key dolt has a dedicated migration assembly; original provider migrations remain unchanged. Versioned project schema migrations are authored only by hub maintenance on maintenance/schema-N and distributed as history. MaintenanceReadiness binds epoch, binary/schema versions, all local branches including unpublished ones, heads, working-set hashes and attestations. Preflight must pass before Nuke swaps binaries; dirty/unattested branch=409 maintenance_not_ready. LAB uses pinned Linux image; LEGION uses checksum-pinned native Windows dolt.exe under a Windows host whose actual service identity, account, ACLs and child-process start/stop/crash cleanup must be evidenced in Ph0 B1/B9; no existing suitable service is assumed, local paths on F: and exclusive Windows file handle/named mutex ownership. All B1-B12 are evidenced on both OSs.

**Acceptance Criteria:**
- [ ] ac-FourProvidersResolve: Given keys sqlite,postgresql,sqlserver,dolt, when runtime and design-time factories resolve them, then each selects its own strategy/migration history; unknown key returns unsupported_provider.
- [ ] ac-DoltBaselineAndUpgrade: Given empty standalone Dolt then baseline schema1, when baseline and additive schema2 apply, then CRUD succeeds, data remains intact and repeated apply makes no further changes.
- [ ] ac-OriginalProviderMigrationsUnchanged: Given recorded hashes of the three original migration chains, when Dolt support is added, then those hashes match and all three real-provider upgrade fixtures pass.
- [ ] ac-UnsupportedSchemaRejected: Given application supports schema1, when opening schema2 for domain writes, then incompatible_schema is returned before mutation.
- [ ] ac-VersionedProjectNotMigratedAtStartup: Given a versioned project at schema1, when application starts, then no EF migration executes and its Dolt head/schema remain unchanged.
- [ ] ac-OldSchemaUnpublishedCommitMergesAfterUpgrade: Given B1 is unpublished on schema1 and hub S1 adds schema2 from C0, when preflight/upgrade and merge succeed, then B/main=M1 with both B1 and S1 ancestors and B1 row values preserved.
- [ ] ac-FeatureBranchSchemaCatchUp: Given main accepts S1 but feature still uses schema1, when feature is selected for writes, then it remains closed until its own merge with S1 validates.
- [ ] ac-DirtyDraftBlocksBinaryUpgrade: Given any local branch has a draft, when Nuke preflight runs, then409 maintenance_not_ready prevents binary replacement and preserves the draft.
- [ ] ac-ContractBlockedUntilLocalBranchesAttested: Given unpublished feature lacks an expand-schema attestation, when contract migration is requested, then it is refused even if all public remote branches are current.
- [ ] ac-DeploymentPreflightBlocksDirtyOrUnattestedBranch: Given a ready public main but an unattested local feature, when deployment runs, then no binary/service/schema change occurs; explicit branch attestation is required.
- [ ] ac-WindowsWriterLockAndRestart: Given one Windows worker owns Atlas, when a second process starts or the owner crashes, then concurrent ownership is denied and restart recovers only after the OS releases its lock; committed rows and unpublished heads survive.
- [ ] ac-RealHostAcceptanceMatrix: Given isolated disposable Atlas stores on LAB and LEGION and FED-ATLAS-H/A/B fixtures, when Ph8 partitions/reconnects each control, publication/history, audit and identity path and restarts each application/worker, Dolt, gateway, identity and control/audit component at commit/publication/report boundaries, then every matrix row records exact heads/ancestor sets/rows/drafts/refs/epoch and audit receipts matching the workflow outcome, with zero missing provider/migration/recovery rows. Missing any component/path row fails acceptance; unrelated live projects retain their provider mappings and data.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-014: original-provider-to-Dolt migration tool contract

Status: pending. Revision audit amended 2026-11-10.
MigrationManifest binds MigrationId, mode, ProjectId, source provider/snapshot hash, schema versions, table counts, typed row hashes, alias decisions, audit backfill and immutable destination commit. Ph3 implements seed, dry-run/resume/verify and Dolt-to-new-SQLite project recovery; join is unavailable until Ph6 review/publication exist. Both lab sources are SQLite; PostgreSQL and SQL Server remain supported fixture sources. Retain originals read-only. Recovery quiesces the owner and captures every branch, including unpublished heads and dirty working sets, without creating history commits. Export one new compatible SQLite DB per branch containing ALL B2-classified versioned domains: TODO child/link/document graphs, requirements/mappings, use-case actors/flows/steps, workspace memories, portable configuration, promoted session graphs and permanent aliases. Preserve IDs, values, timestamps, versions, soft deletes and audit linkage. Manifest binds branch/head/working-set/schema/count/hash evidence; retain immutable Dolt objects/refs to preserve ancestry outside SQLite. export-todos remains a subset, never whole-project rollback acceptance. Missing domain/branch or failed validation blocks routing changes. Operator selects the verified active snapshot; others remain readable recovery artifacts. Capture host-local control/audit/outbox/IdentityServer backups at the same quiescence boundary and verify restore plus hub audit resend. Cutover requires verified receipts, explicit DEPLOY HOLD release and one-node soak before the second host.

**Acceptance Criteria:**
- [ ] ac-ThreeSourceProviderRoundTrip: Given the same fixture in SQLite,PostgreSQL,SQL Server, when seed migration runs, then counts, typed hashes, IDs, nulls, Unicode, decimals, UTC precision and FK relationships match in Dolt.
- [ ] ac-ResumeAfterBatchCommit: Given target batch1 committed before a crash but its acknowledgement was lost, when resume runs, then batch1 is recognized by journal and no duplicate rows appear.
- [ ] ac-ChangedSnapshotRejected: Given manifest source hash S0, when source is replaced by S1 before resume, then409 migration_source_changed writes no additional target rows.
- [ ] ac-FailedVerifyCannotCutover: Given one row hash differs, when cutover is requested, then409 migration_verification_failed leaves source routing active and writes closed on the target.
- [ ] ac-SourceRetainedAfterCutover: Given verified cutover completes, when target receives a new TODO, then source backup remains present, read-only and byte-identical; the new TODO exists only in Dolt until an explicit recovery export.
- [ ] ac-MigrationJoinPreservesCommonAncestor: Given seed C0 is published and join imports J1, when join completes in Ph6, then C0 is an ancestor of J1 and reviewed main M1; no second root is created.
- [ ] ac-MigrationJoinDivergentRowRequiresReview: Given seed T1.title=A and frozen join T1.title=B, when join planning runs, then an explicit keep/take/manual decision is required before J1 even if Dolt would fast-forward.
- [ ] ac-JoinCutoverRequiresReviewedMain: Given J1 is verified but main integration is unresolved, when cutover runs, then409 migration_verification_failed preserves source routing; only verified reviewed M1 permits cutover.
- [ ] ac-DoltTodoExportPreservesNewWrites: Given A1 adds TODO T2 after cutover, when export-sqlite targets A1, then the new SQLite DB contains T2 and all TODO children/aliases with matching hashes, while the original source remains unchanged.
- [ ] ac-SeedPhaseRejectsJoin: Given only Ph3 is implemented, when mode=join is requested, then501 unsupported_capability creates no target commit or routing change.
- [ ] ac-FullProjectRecoveryPreservesAllDomains: Given Atlas has main=A1 with post-cutover TODO T1, requirement R1/mapping, use case U1/actor/flow/step, memory W1, portable setting K1 and promoted session S1/turn/action, plus unpublished feature=F1 and draft T2.title=draft, when export-project freezes both branches and verifies fresh SQLite snapshots plus operational backup, then all IDs/typed values/links/soft deletes match each captured working set, feature still resolves F1, T2 remains draft without a new Dolt commit, audit receipts reconcile, the retained Dolt archive contains A1/F1 ancestry and original SQLite bytes are unchanged. Omit W1 or feature in a negative control: migration_verification_failed prevents routing switch.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-015: imported state does not repeat external effects contract

Status: pending. Revision audit amended 2026-11-10.
History import and audit ingestion apply validated state, never dispatch original domain commands or external effects. EffectIdentity is stable across retries/import. Imported operational metadata is inert evidence, not an execution request. General federation apply/control is outside QuadBrain keyserver transaction gating per FR-MCP-173; actual QuadBrain operations retain their separate coordinator contract. Legacy HMAC envelopes remain isolated compatibility behavior.

Server: synchronization applies data without executing external operations. Plugin: distinguish history/data synchronization from separately authorized external effects in preview, status and receipts.

**Acceptance Criteria:**
- [ ] ac-ImportDoesNotSendExternalAction: Given A1 records an already-sent GitHub update, when B integrates A1, then B state includes the result and outbound GitHub/email invocation count is0.
- [ ] ac-RetryDoesNotRepeatEffect: Given accepted commit A1 and effect E1, when sync retries10 times, then the effect ledger still has one E1 and no new external dispatch occurs.
- [ ] ac-ImportedOperationalRowsRemainInert: Given audit backfill contains command names and receipt payloads, when the hub ingests them, then they are stored as evidence and no business command executes.
- [ ] ac-FederationBypassesQuadBrainKeyserver: Given QuadBrain coordinator is degraded but local authorization passes, when federation commit/control/audit operations execute, then none invokes the keyserver and valid local work succeeds.
- [ ] ac-PluginEffectBoundary: Given incoming A1 contains a TODO completed state and inert historical external-action metadata, when the plugin shows synchronization success, then it labels data received with zero external operations executed; requesting an external operation requires a separate explicit authorized command and its own receipt.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-FEDERATION-016: safe shared-host editing and commit ownership contract

Status: pending. Revision audit amended 2026-11-10.
BranchLease key=(ProjectId,Branch), OwnerSessionId, Fence int64, ExpiresUtc and WorkingSetFingerprint. Every live-branch mutation holds the same host-owner write gate across lease validation and transaction/commit. Expiry rejects new admission; an admitted write finishes before takeover. Isolated worker rights cover only non-live integration refs and it invokes owner API for live moves. Dirty takeover needs explicit review. Commit requestId persists in history for idempotency; preview binds exact head/fingerprint.

Server: editing owner, request/release and fenced takeover contracts. Plugin: display editing owner, request/release context and offer another branch without capturing another agent draft.

**Acceptance Criteria:**
- [ ] ac-CommitCannotCaptureOtherAgentDraft: Given agent1 owns a draft, when agent2 commits the same branch, then409 branch_busy creates no commit and the draft remains agent1-owned.
- [ ] ac-ExpiredLeaseFenced: Given fence4 expires and fence5 is issued, when a fence4 edit arrives, then409 lease_lost persists no row changes.
- [ ] ac-PreviewChangeRequiresReview: Given preview fingerprint W1, when the working set becomes W2, then commit with W1 returns409 working_set_changed and requires a fresh preview.
- [ ] ac-LeaseLostDuringWriteRejected: Given a write admitted under fence4, when expiry occurs mid-transaction, then that write finishes under the gate before fence5 takeover; a later fence4 write is rejected.
- [ ] ac-CommitRetryReturnsSameHash: Given request R1 created A1 and reply was lost, when R1 retries after restart, then A1 is returned by its commit trailer and history contains no duplicate commit.
- [ ] ac-IncomingMergeCannotRaceAdmittedEdit: Given an edit holds the branch gate, when the integration worker requests a live head move, then it waits; after the edit finishes, fingerprint revalidation detects the draft and returns waiting_local_work without overwriting it.
- [ ] ac-PluginEditingOwnership: Given agent1 owns A/main fence4 with a draft, when agent2 views context and requests editing, then it sees the owner and branch, receives409 branch_busy, and may select a separate feature branch; owner release/takeover requires explicit authorization and preserves the main draft without commit or discard.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TR-MCP-TXNCOMPAT-001: Federation and QuadBrain trust separation

Status: pending

FR-MCP-173 supersedes the older requirement that all federation mutations invoke the coordinator. Keep HMAC legacy transport compatibility distinct from IdentityServer network authentication, versioned history and QuadBrain transaction signing. Do not change the live TurnTransactions flag to implement this separation.

- [ ] review20261110-1: Given federation apply/control operations and RequiredForMutations=true, when executed, then ITurnTransactionCoordinator invocation count is0; brain-slot.invoke remains gated.

Definition source: docs/Project/federation-dolt/Technical-Requirements.md.

#### TEST-MCP-136: Autonomous federation and explicit legacy-mode tests

Status: pending

Test local read/write/restart during outage, version commit publication, project isolation and reviewed convergence using TEST-MCP-222..237. Retain explicit DirectProxy loop-protection and route tests. Replace LocalProxy hub-authority, queued-write and read-switch assertions for Dolt mode.

- [ ] review20261110-1: Given a local commit during outage, when reconnect runs, then history is exchanged without replaying the original command.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-222: autonomous local execution acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-174 and TR-MCP-FEDERATION-001 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-NodeWorksWithHubUnavailable: Given A/main=C0 and the hub link is down, when A changes TODO T1.title from old to local, then the local API succeeds, a local reread returns local, and no hub request or history commit occurs.
- [ ] ac-HubUsesSameLocalExecution: Given H/main=C0 and the gateway is stopped, when H edits T1.title, then H persists it locally while remote/main remains C0.
- [ ] ac-OfflineLocalReadAfterWrite: Given the prior offline edit, when the application and Dolt restart, then T1.title remains local and main remains C0 until an explicit version commit.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-223: explicit version commits and publication boundary acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-175 and TR-MCP-FEDERATION-002 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-SaveDoesNotPublish: Given main=C0 and remote=C0, when SaveChanges persists T1.title=draft, then main and remote stay C0 and no publication intent is emitted.
- [ ] ac-VersionCommitTriggersSync: Given that draft and a valid preview, when commit request R1 succeeds, then main=A1, the reply says committed_locally, and one recoverable publication intent targets A1.
- [ ] ac-HubRequestDoesNotCommitDraft: Given B has a draft and desired head A1, when B polls, then outcome=waiting_local_work, B/main=C0 and the draft value is unchanged.
- [ ] ac-OfflineCommitDiscoveredAfterRestart: Given A1 committed while disconnected and a crash before intent persistence, when A restarts, then history scanning rediscovers A1 exactly once and publishes it after reconnect.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-224: logical project enrollment and initial clone acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-176 and TR-MCP-FEDERATION-003 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-DifferentHostPathsSameProject: Given LAB path /home/sharpninja/github/McpServer and LEGION path F:/github/McpServer, when both enroll as ProjectId Atlas, then both reference Atlas and retain their distinct local paths.
- [ ] ac-CloneRefusesNonemptyTarget: Given target has TODO X, when clone is requested, then409 target_not_empty leaves X and all target files unchanged.
- [ ] ac-UnauthorizedProjectDenied: Given node A has only Atlas membership, when it enrolls or clones SecretProject, then403 unauthorized_project returns no rows, objects or project existence details.
- [ ] ac-PluginEnrollmentDiagnostics: Given Atlas is discoverable but A has a nonempty destination, when each plugin selects Atlas and attempts enrollment/clone, then it displays ProjectId, selected branch and409 target_not_empty with remediation; no database is overwritten and no different project is selected.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-225: branches across server and plugin context acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-177 and TR-MCP-FEDERATION-004 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-PooledConnectionDoesNotLeakBranch: Given main T1.title=main and feature/search T1.title=feature, when alternating pooled reads run100 times, then each returns only its selected value.
- [ ] ac-FeaturePublicationLeavesMainUnchanged: Given main=C0 and feature/search=F1, when F1 publishes, then remote feature=F1 and all main heads remain C0.
- [ ] ac-BranchMergeRequiresExpectedHeads: Given a preview for main=C0 and feature=F1 but feature advances to F2, when merge submits the old preview, then409 head_changed creates no commit and changes no ref.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-226: bidirectional committed-history exchange acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-178 and TR-MCP-FEDERATION-005 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-ThreeWriterCleanConvergence: Given C0 contains T1.title=old,T1.description=old,T2.priority=low, when A1 changes title=A, B1 changes description=B and H1 changes T2.priority=high, then merges M1(A1,B1), M2(M1,H1) converge all heads to M2 with values A,B,high and all three commits in ancestry.
- [ ] ac-HubDraftDoesNotBlockRemote: Given H has an uncommitted title draft at C0, when A publishes A1, then remote=A1, H remains C0 with its draft, and B can accept A1.
- [ ] ac-ConcurrentPublishPreservesBothHistories: Given A1 and B1 descend from C0, when both accept requests expect C0, then one succeeds and the other receives409 head_changed; after merge M1 publishes, both A1 and B1 remain ancestors.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-227: hub control table and polling acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-179 and TR-MCP-FEDERATION-006 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-PollReadDoesNotAcknowledge: Given desired=7 and reported=6, when poll runs twice, then both responses request7 and stored reported remains6.
- [ ] ac-AckOldGenerationPreservesNewRequest: Given desired becomes8 while report7 is in flight, when report7 arrives, then reported=7, desired=8 and the subscription remains pending.
- [ ] ac-MissedNotificationReconciles: Given remote=A1 but control still names C0 after a crash, when reconciliation runs, then it records A1 and advances desired generation without producing another commit.
- [ ] ac-HubRestoreFromOlderBackup: Given a node remembers epoch E1 generation90 and the hub restores a backup at generation40, when the hub reopens, then epoch E2 differs from E1; E1 reports fail409 hub_epoch_changed and no head is force-reset. Node A1 is retained until verified ancestry recovery republishes missing history.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-228: reviewed conflicts and valid merges acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-180 and TR-MCP-FEDERATION-007 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-SameCellConflictNeedsReview: Given base priority=low, A1 priority=high and B1 priority=medium, when B integrates A1, then needs_review shows low/medium/high, B/main remains B1 and no public ref is overwritten.
- [ ] ac-DeleteUpdateNeedsReview: Given A1 deletes T1 and B1 edits T1.title, when histories merge, then needs_review retains B1 and the deleted/edited alternatives for review.
- [ ] ac-CleanMergeCanFailDomainValidation: Given separate commits add T1 dependsOn T2 and T2 dependsOn T1, when Dolt merges without cell conflicts, then domain validation records dependency_cycle and leaves the prior valid live head unchanged.
- [ ] ac-StaleReviewRejected: Given a resolution preview binds local=B1 and incoming=A1, when incoming advances to A2, then resolve returns409 stale_review with no merge commit.
- [ ] ac-ReviewAgeVisible: Given unresolved needs_review at12:00, when the clock reaches12:15, then plugins/status and the operator inbox show warning with ReviewId and age900s; at24h severity is critical, with at most one repeat per hour.
- [ ] ac-UnrelatedPublicationContinues: Given Atlas/main is blocked by review R1, when Atlas/feature publishes F2 and another project publishes X1, then both succeed while Atlas/main remains unchanged.
- [ ] ac-SafeTodoMergeRules: Given A changes title and B changes description with no domain violation, when merge runs, then both values survive; competing priority or completion edits produce needs_review instead of choosing a timestamp winner.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-229: recovery without lost or repeated work acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-181 and TR-MCP-FEDERATION-008 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-LostPushAckDoesNotDuplicateCommit: Given remote accepted A1 and reply was lost, when request R1 retries, then it returns A1 and the existing receipt; no A2 commit is created.
- [ ] ac-OldWorkerEpochCannotReportSuccess: Given worker epoch changes4 to5, when epoch4 reports success, then409 stale_worker_epoch leaves current receipt and desired state unchanged.
- [ ] ac-DirtyBranchDefersApply: Given B/main=C0 with title=draft, when A1 is fetched, then outcome=waiting_local_work, main stays C0 and title remains draft; at15min status/inbox shows the age warning.
- [ ] ac-IncompatibleSchemaDoesNotApply: Given B supports schema1 and incoming S1 requires schema2, when sync runs, then409 incompatible_schema leaves B's live data/head unchanged and records the required version.
- [ ] ac-PluginRecoveryControls: Given operation R1 published A1 but its local reply was lost, when a plugin resumes, reads status and requests cancellation, then it shows publishedHead=A1 and a durable cancellation outcome for remaining work; it never claims A1 was rolled back or creates a second commit.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-230: collision-resistant and preserved identity acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-182 and TR-MCP-FEDERATION-009 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-TwoNodesCreateDistinctIds: Given tags LA and LG and both counters start1, when both create a TODO, then generated IDs differ and both records survive reconciliation.
- [ ] ac-BranchRevertDoesNotReuseSequence: Given LA allocated sequence42, when main reverts and another branch creates a record, then its sequence is greater than42.
- [ ] ac-LegacyCollisionPreservesReferences: Given LAB and LEGION have different entities named TODO-7, when migration resolves the collision, then scoped aliases resolve each original identity and every relational reference follows its reviewed canonical mapping; historical text remains byte-identical.
- [ ] ac-LegacyIdsRemainValid: Given existing IMPL-UNITGREEN-001 and old TODO references, when imported and read by their original IDs, then they resolve through permanent aliases without rewriting commits, PR text, session logs, memories or documents.
- [ ] ac-DifferentIdsSameTodoDetected: Given LAB TODO-7 and LEGION TODO-91 share normalized title Fix login, description and dependency set, when migration plan runs, then it emits a duplicate candidate with both IDs and evidence; until explicit review, neither is merged or deleted.
- [ ] ac-SimultaneousHumanIdReservation: Given human ID IMPL-UNITGREEN-001 is unreserved, when two nodes reserve it concurrently, then exactly one receives201 and the other409 human_id_reserved; retry by the winning requestId returns the same reservation.
- [ ] ac-OfflineHumanIdPublicationDenied: Given an offline-created provisional TODO requesting an unreserved human ID, when it reconnects and publishes, then409 human_id_unreserved retains the local record until reservation or reviewed alias resolution.
- [ ] ac-PluginIdentityDiagnostics: Given LAB TODO-7 and LEGION TODO-91 are a duplicate candidate, when each plugin opens identity review, then both origin IDs, canonical/provisional IDs, similarity evidence and reservation outcome are visible; no alias or row changes before an authorized decision.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-231: first-party plugin federation contract acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-183 and TR-MCP-FEDERATION-010 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-EveryPluginUsesSameContract: Given one fixture for commit R1, conflict R2 and migration R3, when each installed first-party plugin calls its wrapper, then normalized IDs, heads, error codes and audit agent identity match the typed client result.
- [ ] ac-OldServerReportsUnsupportedCapability: Given server lacks federation contract1, when a plugin invokes commit, then unsupported_contract is shown and no raw SQL or shell fallback executes.
- [ ] ac-ReconnectRevalidatesContext: Given a cached leaseFence4 and session branch feature, when reconnect discovers fence5, then the plugin retains visible context but refuses writes with lease_lost until reacquired.
- [ ] ac-StaleBranchReportedNotDeleted: Given a branch observation is older than24h, when status is displayed, then stale=true and observedUtc are visible; the branch, drafts and refs are not deleted.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-232: portable data versus host-local state acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-184 and TR-MCP-FEDERATION-011 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-CloneOmitsSecrets: Given credentials and local machine paths exist in operational storage, when a project is cloned, then none appears in its versioned tables or history.
- [ ] ac-AuditReachesHub: Given A performs an offline TODO edit and creates audit E1, when connectivity returns without a Dolt commit, then hub audit contains exactly E1, A retains E1, and main/remote remain C0.
- [ ] ac-AuditRetryIdempotent: Given E1 was stored but its receipt was lost, when the same batch retries, then exactly one E1 exists and the same hash receipt returns; changed payload under E1 returns409 audit_id_conflict.
- [ ] ac-AuditBackfillVerified: Given source has100 TODO audit and20 generic audit rows, when seed/join audit backfill verifies, then all120 records have matching payload hashes and hub receipts; missing one blocks cutover.
- [ ] ac-FullProjectSessionGraphPreserved: Given an explicitly promoted sanitized session with turns, actions, dialog, commits/files, PlanFile and TodoId, when cloned, then every promoted child and relationship matches the source snapshot; automatic capture still uses the audit channel.
- [ ] ac-AcceptedHeadRebuildsBranchIndex: Given accepted head changes C0 to A1, when the derived index rebuild completes, then queries read A1 values and no C0 cache is served under the A1 key.
- [ ] ac-BackgroundWritersDoNotDirtyVersionedBranch: Given main=C0 is clean, when heartbeat, audit and automatic session capture run, then Dolt working-set fingerprint remains unchanged.
- [ ] ac-SecondAgentLoggingNeedsNoLease: Given agent1 owns the editing lease, when agent2 logs a session action, then its local audit persists without acquiring or changing agent1's lease.
- [ ] ac-CommitPreviewBlocksSecret: Given a promoted snapshot contains a fixture secret detected by the configured detector, when commit preview runs, then422 secret_detected creates no commit and outputs no secret value.
- [ ] ac-ReceiptsAndLogsContainNoCredentials: Given unique fixture canaries are injected through protected secret references, when commands, retries and failures execute on both hosts, then scans of argv, receipts, logs, chat/audit payloads, memory stores, generated files, Git staged blobs/new commits and Dolt working sets/new history find zero canary values. Scanner diagnostics and rejected commit previews also contain zero values; no secret is added to history to test the detector.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-233: scoped authorization and offline behavior acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-185 and TR-MCP-FEDERATION-012 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-RawRemoteCannotBypassPublicationPolicy: Given A has a valid Atlas token, when A addresses the private backend across the link, then the connection fails; when a read-only token attempts a public-main mutation, then403 unauthorized_project; when a publish-authorized token bypasses publication.accept through a raw public-ref push, then409 publication_required. In all three cases remote/main remains C0.
- [ ] ac-ForeignProjectCloneDenied: Given token has Atlas.read but no SecretProject scope, when a remote fetch requests SecretProject, then403 unauthorized_project sends zero objects.
- [ ] ac-RevokedNodeCannotPublish: Given A holds a reference token, when IdentityServer disables A and revokes its grants before acceptance, then accept returns401 invalid_token and remote/main is unchanged.
- [ ] ac-OfflineLocalAuthorizationStillApplies: Given hub/IdentityServer is unreachable and a valid authorized local user, when a local TODO edit is requested, then it succeeds; an unauthorized local user still receives403.
- [ ] ac-LocalAppUserCannotPush: Given ordinary editing SQL credentials, when remote administrative procedures or public-ref writes are attempted, then permission is denied and no object publication occurs.
- [ ] ac-RevokedCredentialFailsFetchAndPublish: Given IdentityServer revocation completes, when A starts new fetch and publish requests with its old token, then both return401 invalid_token before transfer or ref mutation.
- [ ] ac-SqlPortUnreachableAcrossLink: Given both services run, when LAB probes10.42.0.143:3306 and LEGION probes10.42.0.214:3306, then both connections fail; local127.0.0.1:3306 health succeeds.
- [ ] ac-UnknownCertificatePinRejected: Given the peer uses an unapproved SPKI or wrong SAN, when control/remote TLS connects, then tls_peer_untrusted occurs before bearer transmission.
- [ ] ac-PluginLeastPrivilegeDenial: Given a plugin session has Atlas.read but lacks publish, when capabilities load and a publication is nevertheless attempted, then publish is unavailable in the capability display, the request returns403 unauthorized_project, the denial is explicit and no SQL/CLI fallback or head change occurs.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-234: Dolt fourth-provider support and schema migration acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-186 and TR-MCP-FEDERATION-013 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-FourProvidersResolve: Given keys sqlite,postgresql,sqlserver,dolt, when runtime and design-time factories resolve them, then each selects its own strategy/migration history; unknown key returns unsupported_provider.
- [ ] ac-DoltBaselineAndUpgrade: Given empty standalone Dolt then baseline schema1, when baseline and additive schema2 apply, then CRUD succeeds, data remains intact and repeated apply makes no further changes.
- [ ] ac-OriginalProviderMigrationsUnchanged: Given recorded hashes of the three original migration chains, when Dolt support is added, then those hashes match and all three real-provider upgrade fixtures pass.
- [ ] ac-UnsupportedSchemaRejected: Given application supports schema1, when opening schema2 for domain writes, then incompatible_schema is returned before mutation.
- [ ] ac-VersionedProjectNotMigratedAtStartup: Given a versioned project at schema1, when application starts, then no EF migration executes and its Dolt head/schema remain unchanged.
- [ ] ac-OldSchemaUnpublishedCommitMergesAfterUpgrade: Given B1 is unpublished on schema1 and hub S1 adds schema2 from C0, when preflight/upgrade and merge succeed, then B/main=M1 with both B1 and S1 ancestors and B1 row values preserved.
- [ ] ac-FeatureBranchSchemaCatchUp: Given main accepts S1 but feature still uses schema1, when feature is selected for writes, then it remains closed until its own merge with S1 validates.
- [ ] ac-DirtyDraftBlocksBinaryUpgrade: Given any local branch has a draft, when Nuke preflight runs, then409 maintenance_not_ready prevents binary replacement and preserves the draft.
- [ ] ac-ContractBlockedUntilLocalBranchesAttested: Given unpublished feature lacks an expand-schema attestation, when contract migration is requested, then it is refused even if all public remote branches are current.
- [ ] ac-DeploymentPreflightBlocksDirtyOrUnattestedBranch: Given a ready public main but an unattested local feature, when deployment runs, then no binary/service/schema change occurs; explicit branch attestation is required.
- [ ] ac-WindowsWriterLockAndRestart: Given one Windows worker owns Atlas, when a second process starts or the owner crashes, then concurrent ownership is denied and restart recovers only after the OS releases its lock; committed rows and unpublished heads survive.
- [ ] ac-RealHostAcceptanceMatrix: Given isolated disposable Atlas stores on LAB and LEGION and FED-ATLAS-H/A/B fixtures, when Ph8 partitions/reconnects each control, publication/history, audit and identity path and restarts each application/worker, Dolt, gateway, identity and control/audit component at commit/publication/report boundaries, then every matrix row records exact heads/ancestor sets/rows/drafts/refs/epoch and audit receipts matching the workflow outcome, with zero missing provider/migration/recovery rows. Missing any component/path row fails acceptance; unrelated live projects retain their provider mappings and data.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-235: original-provider-to-Dolt migration tool acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-187 and TR-MCP-FEDERATION-014 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-ThreeSourceProviderRoundTrip: Given the same fixture in SQLite,PostgreSQL,SQL Server, when seed migration runs, then counts, typed hashes, IDs, nulls, Unicode, decimals, UTC precision and FK relationships match in Dolt.
- [ ] ac-ResumeAfterBatchCommit: Given target batch1 committed before a crash but its acknowledgement was lost, when resume runs, then batch1 is recognized by journal and no duplicate rows appear.
- [ ] ac-ChangedSnapshotRejected: Given manifest source hash S0, when source is replaced by S1 before resume, then409 migration_source_changed writes no additional target rows.
- [ ] ac-FailedVerifyCannotCutover: Given one row hash differs, when cutover is requested, then409 migration_verification_failed leaves source routing active and writes closed on the target.
- [ ] ac-SourceRetainedAfterCutover: Given verified cutover completes, when target receives a new TODO, then source backup remains present, read-only and byte-identical; the new TODO exists only in Dolt until an explicit recovery export.
- [ ] ac-MigrationJoinPreservesCommonAncestor: Given seed C0 is published and join imports J1, when join completes in Ph6, then C0 is an ancestor of J1 and reviewed main M1; no second root is created.
- [ ] ac-MigrationJoinDivergentRowRequiresReview: Given seed T1.title=A and frozen join T1.title=B, when join planning runs, then an explicit keep/take/manual decision is required before J1 even if Dolt would fast-forward.
- [ ] ac-JoinCutoverRequiresReviewedMain: Given J1 is verified but main integration is unresolved, when cutover runs, then409 migration_verification_failed preserves source routing; only verified reviewed M1 permits cutover.
- [ ] ac-DoltTodoExportPreservesNewWrites: Given A1 adds TODO T2 after cutover, when export-sqlite targets A1, then the new SQLite DB contains T2 and all TODO children/aliases with matching hashes, while the original source remains unchanged.
- [ ] ac-SeedPhaseRejectsJoin: Given only Ph3 is implemented, when mode=join is requested, then501 unsupported_capability creates no target commit or routing change.
- [ ] ac-FullProjectRecoveryPreservesAllDomains: Given Atlas has main=A1 with post-cutover TODO T1, requirement R1/mapping, use case U1/actor/flow/step, memory W1, portable setting K1 and promoted session S1/turn/action, plus unpublished feature=F1 and draft T2.title=draft, when export-project freezes both branches and verifies fresh SQLite snapshots plus operational backup, then all IDs/typed values/links/soft deletes match each captured working set, feature still resolves F1, T2 remains draft without a new Dolt commit, audit receipts reconcile, the retained Dolt archive contains A1/F1 ancestry and original SQLite bytes are unchanged. Omit W1 or feature in a negative control: migration_verification_failed prevents routing switch.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-236: imported state does not repeat external effects acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-188 and TR-MCP-FEDERATION-015 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-ImportDoesNotSendExternalAction: Given A1 records an already-sent GitHub update, when B integrates A1, then B state includes the result and outbound GitHub/email invocation count is0.
- [ ] ac-RetryDoesNotRepeatEffect: Given accepted commit A1 and effect E1, when sync retries10 times, then the effect ledger still has one E1 and no new external dispatch occurs.
- [ ] ac-ImportedOperationalRowsRemainInert: Given audit backfill contains command names and receipt payloads, when the hub ingests them, then they are stored as evidence and no business command executes.
- [ ] ac-FederationBypassesQuadBrainKeyserver: Given QuadBrain coordinator is degraded but local authorization passes, when federation commit/control/audit operations execute, then none invokes the keyserver and valid local work succeeds.
- [ ] ac-PluginEffectBoundary: Given incoming A1 contains a TODO completed state and inert historical external-action metadata, when the plugin shows synchronization success, then it labels data received with zero external operations executed; requesting an external operation requires a separate explicit authorized command and its own receipt.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-237: safe shared-host editing and commit ownership acceptance tests

Status: pending. Revision audit amended 2026-11-10.
Verify FR-MCP-189 and TR-MCP-FEDERATION-016 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] ac-CommitCannotCaptureOtherAgentDraft: Given agent1 owns a draft, when agent2 commits the same branch, then409 branch_busy creates no commit and the draft remains agent1-owned.
- [ ] ac-ExpiredLeaseFenced: Given fence4 expires and fence5 is issued, when a fence4 edit arrives, then409 lease_lost persists no row changes.
- [ ] ac-PreviewChangeRequiresReview: Given preview fingerprint W1, when the working set becomes W2, then commit with W1 returns409 working_set_changed and requires a fresh preview.
- [ ] ac-LeaseLostDuringWriteRejected: Given a write admitted under fence4, when expiry occurs mid-transaction, then that write finishes under the gate before fence5 takeover; a later fence4 write is rejected.
- [ ] ac-CommitRetryReturnsSameHash: Given request R1 created A1 and reply was lost, when R1 retries after restart, then A1 is returned by its commit trailer and history contains no duplicate commit.
- [ ] ac-IncomingMergeCannotRaceAdmittedEdit: Given an edit holds the branch gate, when the integration worker requests a live head move, then it waits; after the edit finishes, fingerprint revalidation detects the draft and returns waiting_local_work without overwriting it.
- [ ] ac-PluginEditingOwnership: Given agent1 owns A/main fence4 with a draft, when agent2 views context and requests editing, then it sees the owner and branch, receives409 branch_busy, and may select a separate feature branch; owner release/takeover requires explicit authorization and preserves the main draft without commit or discard.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-AUDIT-003: Audit delivery, secret exclusion and backfill verification

Status: pending

AuditReachesHub, AuditRetryIdempotent and AuditBackfillVerified exercise permanent node and hub ledgers. CloneOmitsSecrets verifies project history exclusions; wiki export remains free of operational audit. Preserve exact compression and runtime-role tests in TEST-MCP-AUDIT-001/002.

- [ ] review20261110-1: Given source has100 TODO audit and20 generic audit records, when backfill verifies, then all120 matching hashes and receipts exist at the hub; missing one blocks cutover.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-FED-BACKFILL-001: Complete seed/join/audit backfill tests

Status: pending

Cover all explicitly classified domains, idempotent batch resume, no business-command fanout, typed hash parity, full session graph, identity aliases and audit receipts. Remove legacy local-read/hub-write and read-switch assertions in Dolt mode.

- [ ] review20261110-1: Given100 source TODO rows and120 audit records, when migration repeats after acknowledgement loss, then destination counts remain100/120, original timestamps/IDs resolve and external dispatch count is0.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-FED-ROUTE-001: Project isolation and legacy routing tests

Status: pending. Revision audit amended 2026-11-10.
Test missing/foreign project denial, different-host paths mapping to one logical project, token-to-project authorization and legacy fail-closed path/hash routing. Current ordinary TODO traffic is local, not forwarded.

**Acceptance Criteria:**
- [ ] review20261110-1: Given A Atlas and B SecretProject, when A supplies B path or hash, then403 unauthorized_project sends no B rows and performs no mutation.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-FED-WSSYNC-001: Portable configuration and drift tests

Status: pending

Test explicit committed portable configuration, host-value exclusion, hub-owned scope-layer drift and old legacy payload compatibility. Replace blanket equality of every host field and heartbeat-driven versioned mutation.

- [ ] review20261110-1: Given H layer release and A layer draft, when A heartbeats, then H remains release and drift=true; only a separately authorized policy commit changes it.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.

#### TEST-MCP-MEMORY-FED-001: Versioned memory acceptance tests

Status: pending

Test scoped memory/version round trip, local offline CRUD, explicit-commit boundary, delete/update review and global-memory exclusion; retain legacy mode tests under explicit configuration.

- [ ] review20261110-1: Given different same-base edits to M1.text, when sync merges, then needs_review returns base/local/incoming values and retains the valid local head.

Definition source: docs/Project/federation-dolt/Testing-Requirements.md.


### 16.3 Core traceability mappings

- FR-MCP-174 -> TR-MCP-FEDERATION-001 -> TEST-MCP-222
- FR-MCP-175 -> TR-MCP-FEDERATION-002 -> TEST-MCP-223
- FR-MCP-176 -> TR-MCP-FEDERATION-003 -> TEST-MCP-224
- FR-MCP-177 -> TR-MCP-FEDERATION-004 -> TEST-MCP-225
- FR-MCP-178 -> TR-MCP-FEDERATION-005 -> TEST-MCP-226
- FR-MCP-179 -> TR-MCP-FEDERATION-006 -> TEST-MCP-227
- FR-MCP-180 -> TR-MCP-FEDERATION-007 -> TEST-MCP-228
- FR-MCP-181 -> TR-MCP-FEDERATION-008 -> TEST-MCP-229
- FR-MCP-182 -> TR-MCP-FEDERATION-009 -> TEST-MCP-230
- FR-MCP-183 -> TR-MCP-FEDERATION-010 -> TEST-MCP-231
- FR-MCP-184 -> TR-MCP-FEDERATION-011 -> TEST-MCP-232
- FR-MCP-185 -> TR-MCP-FEDERATION-012 -> TEST-MCP-233
- FR-MCP-186 -> TR-MCP-FEDERATION-013 -> TEST-MCP-234
- FR-MCP-187 -> TR-MCP-FEDERATION-014 -> TEST-MCP-235
- FR-MCP-188 -> TR-MCP-FEDERATION-015 -> TEST-MCP-236
- FR-MCP-189 -> TR-MCP-FEDERATION-016 -> TEST-MCP-237

### 16.4 Retained scoped dependency definitions

These 18 records complete the explicitly retained audit, path and session-family dependencies. Full definitions are recovered from the named 2026-11-16 host snapshots and the 2026-11-10 source register. Their original requirements remain applicable only in the scope allocated by the current federation coverage index. In particular FR-MCP-077 is the explicit legacy DirectProxy regression boundary; it does not restore hub-forwarded ordinary work for versioned projects.

Detailed LEGION path-resolver and test-matrix wording is retained where OMARCHY abbreviates it; OMARCHY's additional named criteria are preserved. Namespace/route spelling follows QBrain. Generic example substitutions do not alter path behavior. Coverage annotations are retained as traceability metadata. The root-relative Combine criterion is corroborated by PR82 commits f676835997df7d398928a58c146cff24b97f0c28 and dcd0269acd232a74037c01ae02a2da77b2fd9b0a. Historical test-run claims are not current acceptance evidence. These are recoveries of retained obligations, not newly passing results.

#### FR-MCP-077: Server Federation and Request Proxying

Status: not reassessed by this plan amendment.

The server shall support an opt-in federation mode that proxies incoming requests to a configured remote MCP server instance. Routing shall support a global default target and per-workspace overrides. The feature shall include anti-loop protection (via `X-Mcp-Federation-Hop` header with a configurable maximum hop count), transparent SSE/streaming forwarding for `/mcp-transport`, a runtime management REST API at `/qbrainai/federation`, and auto-discovery of federation targets from running tunnel providers.


**Technical Implementation:** `FederationOptions`, `FederationRegistry`, `FederationProxyService`, `FederationMiddleware`, `FederationController`

**Configuration:** `Mcp:Federation:Enabled`, `Mcp:Federation:Targets`, `Mcp:Federation:DefaultTarget`, `Mcp:Federation:WorkspaceRoutes`, `Mcp:Federation:MaxHops`

**Acceptance Criteria:**
The source record expresses these obligations in prose. The criteria below make those same obligations explicit. AC1/AC2 preserve the identifiers already present in sanitizer prose; ac-catalog-* identifiers are authored by this amendment and are not represented as recovered source identities. No passing result is claimed.

- [ ] ac-catalog-LegacyOptInRouting: Given explicit legacy DirectProxy mode, global default routing and per-workspace overrides select the configured remote MCP instance; versioned project work remains under FR-MCP-103.
- [ ] ac-catalog-LegacyLoopProtection: Forwarded legacy requests carry X-Mcp-Federation-Hop and enforce the configured maximum hop count.
- [ ] ac-catalog-LegacyStreamingAndManagement: Legacy /mcp-transport forwarding preserves SSE/streaming; /qbrainai/federation manages runtime routing, and running tunnel providers supply discoverable targets.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record FR-MCP-077; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### FR-MCP-173: Keyserver signs QuadBrain transactions only

Status: not reassessed by this plan amendment.

Keyserver manifest signing SHALL be used only for QuadBrain transactions: publisher party id prefix brain-slot:, or operation name prefix brain-slot. or quadbrain. All other first-party mutations (TODO, requirements, session-log for every source type including QBAgent, memory, repo, prompt templates, tool registry, GitHub, GraphRAG, voice, agent pool, issue-todo-sync, tool buckets, REPL TODO workflow, requirements ingest, context rebuild, and federation apply/control) SHALL persist without calling ITurnTransactionCoordinator or the keyserver even when Mcp:TurnTransactions:Enabled=true and RequiredForMutations=true. Do not disable the live TurnTransactions flag as the fix. QuadBrain brain-slot.invoke and brain-slot.weight-update remain coordinator-gated and may call keyserver.

The canonical structured criterion identities are ac-fr173-001 through ac-fr173-005 below.

**Acceptance Criteria:**
- [ ] ac-fr173-001: With TurnTransactions.Enabled=true and RequiredForMutations=true, general-agent mutations (todo.update, requirements.fr.update, sessionlog.submit for GrokCode and QBAgent, memory.add, repo.write, github.cli) do not invoke ITurnTransactionCoordinator.ExecuteAsync and still succeed.
- [ ] ac-fr173-002: brain-slot.invoke and brain-slot.weight-update still invoke the coordinator and may call SignManifestAsync.
- [ ] ac-fr173-003: Coordinator degraded state does not block general-agent first-party mutations.
- [ ] ac-fr173-004: Live TurnTransactions.Enabled remains true for QuadBrain; the repo default may stay false.
- [ ] ac-fr173-005: Focused TransactionGated* tests plus TurnTransactionCoordinatorTests, TurnTransactionKeyserverScopeTests, and BrainSlotInvocationTransactionTests complete with zero failures and zero skips.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record FR-MCP-173; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### FR-MCP-SESSIONLOGCTX-001: Session turns record current plan file and MCP TODO id

Status: not reassessed by this plan amendment.

Every session-log turn SHALL store planFile and todoId. After persist, query/get SHALL return both fields. They are never null in API output. When no plan or TODO is active, the stored value SHALL be the exact sentinel None (case-sensitive). The first ordinary persist of a turn SHALL reject omitted, null, empty, or whitespace planFile or todoId; canceled/cancelled supersession preserves the bounded None-sentinel exception in AC-FR-MCP-SESSIONLOGCTX-001-003. planFile SHALL accept a workspace-relative path, an exact absolute path, or a ~/ home-relative path. .. is rejected. Query SHALL support exact filters on planFile and todoId, and text search SHALL match those fields. Existing rows SHALL be backfilled from turn contents and agent history under ~. Import, transcript ingest, and federation apply SHALL persist a validated pair (None if extraction finds nothing). Children: AC-FR-MCP-SESSIONLOGCTX-001-001, AC-FR-MCP-SESSIONLOGCTX-001-002, AC-FR-MCP-SESSIONLOGCTX-001-003, AC-FR-MCP-SESSIONLOGCTX-001-004, AC-FR-MCP-SESSIONLOGCTX-001-005, AC-FR-MCP-SESSIONLOGCTX-001-006, AC-FR-MCP-SESSIONLOGCTX-001-007.

**Acceptance Criteria:**
- [ ] AC-FR-MCP-SESSIONLOGCTX-001-001: Every session-log turn SHALL store planFile and todoId. After persist, query/get SHALL return both fields. They are never null in API output.
- [ ] AC-FR-MCP-SESSIONLOGCTX-001-002: When no plan or TODO is active, the stored value SHALL be the exact sentinel None (case-sensitive).
- [ ] AC-FR-MCP-SESSIONLOGCTX-001-003: The first ordinary persist of a turn SHALL reject omitted, null, empty, or whitespace planFile or todoId and insert no turn row. A canceled or cancelled supersession may omit the pair, stores the exact sentinel None for each missing value, and a durable reopen preserves that bounded exception.
- [ ] AC-FR-MCP-SESSIONLOGCTX-001-004: planFile SHALL accept a workspace-relative path, an exact absolute path, or a ~/ home-relative path. .. is rejected.
- [ ] AC-FR-MCP-SESSIONLOGCTX-001-005: Query SHALL support exact filters on planFile and todoId, and text search SHALL match those fields.
- [ ] AC-FR-MCP-SESSIONLOGCTX-001-006: Existing rows SHALL be backfilled from turn contents and agent history under ~. Uncertain results stay None. Invented ids/paths are forbidden.
- [ ] AC-FR-MCP-SESSIONLOGCTX-001-007: Import, transcript ingest, and federation apply SHALL persist a validated planFile and todoId (None if extraction finds nothing). Null is never stored.
- [ ] AC-FR-MCP-SESSIONLOGCTX-001-008: The agent-history scan under ~ (.grok, .claude, .codex, .cursor) skips any directory it cannot traverse (a link or other reparse point, an untrusted mount point, an inaccessible folder), and that covers each history root itself: a history root that is a link contributes no file, the same as a link inside it, while a home folder that is itself a link is still scanned; the scan never fails a submit or an import; when nothing usable is found the turn keeps None (operator 2026-10-20: "Fix scans in PR 87 (Recommended)"; operator 2026-10-21: "Fix both, strengthen ACs", hostile validation round 5 finding R5-F1; IMPL-UNITGREEN-001, triage-report-b0e41c36f93c4c91b1bf2524eb579ef8). Moved from AC-TR-MCP-SESSIONLOG-006-009 (operator 2026-10-29: "Move criteria that are misplaced.  Unit tests are for FR, Integration tests are for TR."); its real file-system binding is AC-TR-MCP-SESSIONLOG-006-010.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record FR-MCP-SESSIONLOGCTX-001; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### FR-MCP-SESSIONLOGSAN-001: Sanitized session-log read responses

Status: not reassessed by this plan amendment.

Session-log query and single-session read responses must redact built-in credential forms and workspace-configured regular-expression matches before data leaves the server while preserving the raw persisted session log.

**Acceptance Criteria:**
- [ ] ac-1: Query and single-session GET responses redact secrets from session metadata, turns, actions, processing dialog, contexts, files, blockers, requirements, commits, raw context, and original entry payloads.
- [ ] ac-2: Stored entities and submitted DTOs remain unchanged; sanitization is an outbound projection only.
- [ ] ac-3: Default credential detectors are active without custom configuration and custom workspace rules may add bounded regex matches.
- [ ] ac-4: Filtering, total count, ordering, offset, and limit are computed from raw data and remain unchanged after redaction.
- [ ] ac-5: Federated and stdio read surfaces apply the same sanitization behavior.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record FR-MCP-SESSIONLOGSAN-001; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TEST-MCP-AUDIT-001: Audit payload migration across three providers

Status: not reassessed by this plan amendment.

AuditPayloadMigrationTests and AuditPayloadProviderMigrationTests apply the real SQLite, PostgreSQL, and SQL Server migrations in scratch databases, verify legacy text reads and all four compressed payload round trips, and confirm the new migration ID is applied.

**Acceptance Criteria:**
The source record expresses these obligations in prose. The criteria below make those same obligations explicit. AC1/AC2 preserve the identifiers already present in sanitizer prose; ac-catalog-* identifiers are authored by this amendment and are not represented as recovered source identities. No passing result is claimed.

- [ ] ac-catalog-ProviderMigrationRoundTrips: AuditPayloadMigrationTests and AuditPayloadProviderMigrationTests apply actual migrations in scratch SQLite, PostgreSQL and SQL Server databases; each verifies legacy text reads, all four compressed-payload round trips, and the applied migration ID.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TEST-MCP-AUDIT-001; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TEST-MCP-AUDIT-002: Audit append-only runtime and migration guard

Status: not reassessed by this plan amendment.

DbFkBehaviorTests reject tracked audit updates. SqlServerAuditPayloadMigrationTests assert seven SQL Server runtime-role DENY permissions. AuditPayloadMigrationTests verify AutoMigrate false rejects pending schema and accepts migrated schema.

**Acceptance Criteria:**
The source record expresses these obligations in prose. The criteria below make those same obligations explicit. AC1/AC2 preserve the identifiers already present in sanitizer prose; ac-catalog-* identifiers are authored by this amendment and are not represented as recovered source identities. No passing result is claimed.

- [ ] ac-catalog-AuditUpdateRejected: DbFkBehaviorTests reject tracked audit updates.
- [ ] ac-catalog-SevenSqlServerDenials: SqlServerAuditPayloadMigrationTests verify the seven runtime-role DENY permissions.
- [ ] ac-catalog-MigrationGuardBehavior: AuditPayloadMigrationTests prove AutoMigrate=false rejects pending schema and accepts migrated schema.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TEST-MCP-AUDIT-002; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TEST-MCP-FED-PATH-001: Cross-platform workspace path normalizer matrix

Status: not reassessed by this plan amendment.

Unit matrix {host: Windows, simulated Linux} x {input: C:\x, C:/x, \\srv\sh\x, //srv/sh/x, /home/x, relative, mixed separators} for IWorkspacePathNormalizer: foreign-platform inputs are never prefixed with the process cwd and keep their platform. TriageService on simulated Linux: submit with C:\Users\kingd stores OriginalWorkspacePath C:/Users/kingd (no /opt/... prefix), getReport and queryGroups by the original path succeed. WorkspaceService.Init on simulated Linux with a Windows path does not create directories. Architecture test: no direct Path.GetFullPath on request-supplied workspace paths under src/ outside the allowlist. Mocks first: host-platform and file-system abstractions mocked.

**Acceptance Criteria:**
- [ ] AC1: The root-relative Combine cases of TR-MCP-FED-PATH-001 AC1 run in the normalizer contract base against the scripted MockData fixture and the production normalizer on both simulated hosts, with negative controls that reject an appended segment, a dropped drive, and a dropped UNC share.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TEST-MCP-FED-PATH-001; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TEST-MCP-PATHNORM-001: Workspace path storage, rekey and native-boundary validation

Status: not reassessed by this plan amendment.

Per BDP v4 (MockData fixture green before production code, then Real fixture). Storage-key equivalence: C:\Users\example, C:/Users/example, c:/users/EXAMPLE -> one key windows:C:/USERS/EXAMPLE; \\srv\share\x and //srv/share/x -> one key keeping leading //; \\?\C:\x -> C:/x. Remote-path detection treats //srv/share as remote. ToNativePath returns backslash paths on Windows. Rekey: value mapping f(x) touches only Windows-shaped values and is idempotent; planner builds the mapping and statement plan from a scripted snapshot; executors per provider (SQLite in-memory; SQL Server and PostgreSQL provider fixtures) rewrite all workspace columns and recompute hashed values with FK validation; report counts unchanged per table and per workspace; startup guard rejects an unconverted store; rehearsal on a restored copy of the Windows lab host's store recorded as receipts. GlobalWorkspaceId for (EXAMPLE-HOST, C:/Repos/Example) is stable across both input separators.

**Acceptance Criteria:**
- [ ] unitgreen-native-host: The native-boundary (ToNativePath) behavior for a non-Windows host is proven against a simulated host, on Windows as well, without a host-conditional skip; the Windows-host cases keep running on a Windows host.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TEST-MCP-PATHNORM-001; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TEST-MCP-SESSIONLOG-FED-001: Session-log federation snapshot carries the full session graph

Status: not reassessed by this plan amendment.

Unit tests (SessionLogFederationStateAdapter with a SQLite store and the local session-log service) SHALL prove TR-MCP-FED-SESSIONLOG-001: full-graph equality with the local read, apply round trip, sanitizer parity, local-only snapshot, and unchanged missing-session and version behavior, each with a negative control that a partial or unsanitized payload fails.

**Acceptance Criteria:**
- [ ] AC-001: TR-MCP-FED-SESSIONLOG-001 AC-001: full-graph equality; negative control: the pre-change 8-field turn payload fails the comparison.
- [ ] AC-002: TR-MCP-FED-SESSIONLOG-001 AC-002: apply round trip to an empty store.
- [ ] AC-003: TR-MCP-FED-SESSIONLOG-001 AC-003: sanitizer parity; negative control: an unsanitized payload fails.
- [ ] AC-004: TR-MCP-FED-SESSIONLOG-001 AC-004: no federation data call while snapshotting (a federation data client double records zero calls).
- [ ] AC-005: TR-MCP-FED-SESSIONLOG-001 AC-005: missing and soft-deleted sessions, and explicit version unchanged.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TEST-MCP-SESSIONLOG-FED-001; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TEST-MCP-SESSIONLOGSAN-001: Session-log sanitization unit and transport validation

Status: not reassessed by this plan amendment.

Tests must prove default and configured redaction across the complete session-log DTO graph and all supported read transports without changing persisted raw data or query semantics.

**Acceptance Criteria:**
- [ ] ac-1: Unit tests cover every default detector, overlapping rules, deterministic replacements, recursive object payloads, invalid patterns, duplicate IDs, timeouts, and non-mutation.
- [ ] ac-2: Service tests cover QueryAsync and GetAsync for every DTO field and preserve total count, ordering, offset, and limit.
- [ ] ac-3: HTTP, stdio, and federated integration tests return redacted payloads while direct database verification retains raw values.
- [ ] ac-4: Executed current-plus-prior test scope reports zero failures and zero skips.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TEST-MCP-SESSIONLOGSAN-001; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TEST-MCP-SESSIONLOGSAN-002: Deterministic sanitizer timeout contract validation

Status: not reassessed by this plan amendment.

Verify TR-MCP-SESSIONLOGSAN-002 using the injected RegexReplaceInvoker to raise RegexMatchTimeoutException deterministically for the pathological large field, without wall-clock timing. The single-string case must return the rule timeout token without logging input; the whole-session case must continue sanitizing other fields. Repeated isolated runs must be stable, and production must retain the default Regex.Replace invoker.

**Acceptance Criteria:**
The source record expresses these obligations in prose. The criteria below make those same obligations explicit. AC1/AC2 preserve the identifiers already present in sanitizer prose; ac-catalog-* identifiers are authored by this amendment and are not represented as recovered source identities. No passing result is claimed.

- [ ] ac-catalog-DeterministicTimeoutBinding: The single-string and whole-session tests inject RegexMatchTimeoutException for the pathological field, assert the timeout redaction token without logging the input, and prove other fields continue to be sanitized.
- [ ] ac-catalog-StableDefaultInvoker: Repeated isolated runs are stable without wall-clock timing, and the production default remains Regex.Replace.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TEST-MCP-SESSIONLOGSAN-002; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TR-MCP-AUDIT-002: Versioned compressed generic audit payloads

Status: not reassessed by this plan amendment.

Add nullable versioned GZip UTF-8 columns for the four generic audit JSON payloads on SQLite, SQL Server, and PostgreSQL. Preserve existing text columns and read legacy rows without a historical rewrite.
**Covered by:** FR: FR-MCP-AUDIT-001; TEST: TEST-MCP-AUDIT-001, TEST-MCP-AUDIT-002, TEST-MCP-AUDIT-003

**Acceptance Criteria:**
The source record expresses these obligations in prose. The criteria below make those same obligations explicit. AC1/AC2 preserve the identifiers already present in sanitizer prose; ac-catalog-* identifiers are authored by this amendment and are not represented as recovered source identities. No passing result is claimed.

- [ ] ac-catalog-CompressedPayloadSchema: SQLite, PostgreSQL and SQL Server migrations add nullable versioned GZip UTF-8 storage for all four generic audit JSON payloads.
- [ ] ac-catalog-LegacyAuditReadPreserved: Existing text columns remain readable, and migration does not rewrite historical text rows.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TR-MCP-AUDIT-002; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TR-MCP-AUDIT-003: Restricted SQL Server audit runtime role

Status: not reassessed by this plan amendment.

The SQL Server migration creates a runtime role with ordinary application DML but denies update and delete on both audit ledgers and writes to EF migration history. With AutoMigrate false, HTTP and stdio startup reject pending migrations before serving runtime work.
**Covered by:** FR: FR-MCP-AUDIT-001; TEST: TEST-MCP-AUDIT-001, TEST-MCP-AUDIT-002, TEST-MCP-AUDIT-003

**Acceptance Criteria:**
The source record expresses these obligations in prose. The criteria below make those same obligations explicit. AC1/AC2 preserve the identifiers already present in sanitizer prose; ac-catalog-* identifiers are authored by this amendment and are not represented as recovered source identities. No passing result is claimed.

- [ ] ac-catalog-AuditRolePermissions: The SQL Server migration creates a runtime role with ordinary application DML and denies UPDATE and DELETE on both audit ledgers and writes to EF migration history.
- [ ] ac-catalog-PendingMigrationStartupGuard: With AutoMigrate=false, HTTP and stdio startup reject pending migrations before serving runtime work.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TR-MCP-AUDIT-003; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TR-MCP-FED-PATH-001: Single request-path normalization entry point

Status: not reassessed by this plan amendment.

Introduce one request-path normalization entry point (IWorkspacePathNormalizer backed by WorkspaceIdentityPath) with an injectable host-platform abstraction so tests can simulate a Linux host on Windows. Every resolver that receives a request-supplied workspace path uses it: TriageService.ResolveSubmittingWorkspace/ResolveEffectiveWorkspaceAsync and query methods, WorkspaceService.NormalizePath (GetAsync/Create/Init), WorkspaceResolutionMiddleware.PopulateContext, HandoffWorkspacePaths, QBrainAiMcpTools.ApplyWorkspaceOverride, WorkspaceServiceAccessor, HandoffIngestionService, WorkspaceTokenService.Normalize, TodoServiceResolver, TodoServiceFactory, EfTodoService, RequirementsDatabaseDocumentService.CreateScope/TryGetRequestWorkspacePath, HostileReviewService, TodoExecutionService, FileGitHubWorkspaceTokenStore, AgentPoolService.BuildPooledDeviceId, AuditedAgentCliClient, RepoFileService, MarkerFileService, WikiDumpService, WorkspacePolicyDirectiveParser, AgentHelpPinnedPathResolver, HandoffSourceResolver, SessionLogWorkspaceAttributionValidator. Path.GetFullPath is applied only to host-native paths. An architecture test fails on any new direct Path.GetFullPath over request-supplied workspace paths outside an explicit allowlist.

**Acceptance Criteria:**
- [ ] AC1: IWorkspacePathNormalizer.Combine treats a segment with one leading backslash as root-relative on Windows: it keeps the current root's volume, the drive (C:/) or the UNC server and share (//srv/share/), and replaces the rest of the path, on a Linux hub and a Windows host (PR 81 finding P12, PR 82 review).

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TR-MCP-FED-PATH-001; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TR-MCP-FED-SESSIONLOG-001: Session-log federation snapshot carries the full session graph

Status: not reassessed by this plan amendment.

SessionLogFederationStateAdapter.SnapshotAsync SHALL return, for an existing session of the local store, a payload in the UnifiedSessionLogDto shape that the adapter's ApplyAsync accepts, carrying every session field and every turn with all of its fields and child collections (actions, tags, context items, processing dialog, commits with their files, design decisions, requirements discovered, files modified, blockers), equal to what the local session-log read returns for that session after the outbound session-log sanitizer. Building a snapshot reads only the local store. The snapshot version stays the session's explicit version (ContentHash, else LastUpdated in Unix milliseconds, else TurnCount).

**Acceptance Criteria:**
- [ ] AC-001: For a session whose turn carries a value in every turn field and at least one item in every child collection (commits with files), the snapshot payload deserializes as UnifiedSessionLogDto and equals the local session-log read of that session field by field, child collections in order.
- [ ] AC-002: A snapshot payload is applyable: applying it through the adapter's ApplyAsync to a store without that session and then reading the session there yields a DTO equal to the source read, for values the sanitizer leaves unchanged.
- [ ] AC-003: A value the outbound session-log sanitizer redacts in the session-log read is redacted the same way in the snapshot payload.
- [ ] AC-004: Building a snapshot makes no federation data call: a session that exists only on a peer is not snapshotted, and a local session's snapshot carries only local turns.
- [ ] AC-005: A missing or soft-deleted session yields PayloadJson "null" and Version null, and an existing session's Version equals its explicit version (ContentHash, else LastUpdated in Unix milliseconds, else TurnCount), as before this requirement.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TR-MCP-FED-SESSIONLOG-001; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

Provenance attribution: the retained source record notes the 2026-10-13 operator decision, "Complete the snapshot", under triage-report-ab06ac3b / PLAN-SESSIONTRIAGEFIX-001. This attribution is from that record; it is not presented as a newly verified raw chat quote.

#### TR-MCP-PATHNORM-001: Forward-slash storage keys, rekey maintenance command and startup guard

Status: not reassessed by this plan amendment.

WorkspaceIdentityPath.NormalizeLexicalPath emits '/' for Windows paths (UNC keeps leading '//'; \\?\ device prefixes stripped to drive or // form); BuildStorageKey becomes windows:C:/WORKSPACES/EXAMPLE style. IsRemoteWindowsPath, IsNormalizedPathWithinRoot and NormalizeLocalAdministrativeShareAlias accept both separators. A ToNativePath helper produces backslash paths only for Win32 CreateFile/GetFinalPathNameByHandle, \\?\ long-path prefixes and cmd.exe arguments. Persisted data is rewritten by an explicit maintenance command --workspace-path-rekey (decision D5, 2026-10-07) modelled on McpDatabaseEncryptionTransitionCommand: required backup unless --dry-run, report with old->new mapping and before/after counts; one transaction per database with foreign keys deferred or suspended and fully re-validated (decision D6: SQLite defer_foreign_keys, SQL Server NOCHECK then WITH CHECK CHECK, PostgreSQL DEFERRABLE + SET CONSTRAINTS ALL DEFERRED); set-based UPDATE per workspace column from a mapping table limited to Windows-shaped values; C# recomputation of hashed values (federation GlobalWorkspaceId/CanonicalWorkspaceId and hub hash-keyed Workspaces rows, TriageGroups GroupKey/GroupId with repointed children, HandoffIngestionRuns ReplayIdentity); FileGitHubWorkspaceTokenStore keys rewritten. No legacy dual-key lookup (decision D7): a startup guard refuses to start while any Windows-shaped workspace id still contains a backslash. Enrolled proxies are re-enrolled once after deploy (the Windows proxy in the original deployment). Plan: docs/plans/2026-10-07-federation-fixes.md section 4c.

**Acceptance Criteria:**
- [ ] p15-policy-a: Reuse P1.4 Windows lexical normalization, preserve case, and reject distinct old keys converging before any database mutation.
- [ ] p15-safety: Apply requires successful verified backup; all database changes and FK validation occur in one transaction; repeat apply is a no-op.
- [ ] p15-preview: Dry-run inventory and planning run only on a database copy; report mappings, table and workspace counts, and retained manual triage hashes without secrets.
- [ ] p15-guard: HTTP and STDIO startup refuse legacy Windows workspace ids and point to --workspace-path-rekey.
- [ ] p15-derived: Recompute only derivable triage, handoff, and federation identities; repoint children; preserve manual triage hashes and rewrite token-store keys with file backup.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TR-MCP-PATHNORM-001; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TR-MCP-SESSIONLOGSAN-001: Bounded outbound session-log sanitizer

Status: not reassessed by this plan amendment.

A single sanitizer decorator must clone session-log DTO graphs, apply validated default and configured regex rules with deterministic replacement tokens and finite timeouts, and wrap the final local or federated ISessionLogService read result.
**Covered by:** FR: FR-MCP-SESSIONLOGSAN-001; TEST: TEST-MCP-SESSIONLOGSAN-001, TEST-MCP-SESSIONLOGSAN-002

**Acceptance Criteria:**
- [ ] ac-1: ISessionLogSanitizer exposes DTO and string sanitization without mutating caller-owned objects.
- [ ] ac-2: SessionLogSanitizationOptions limits rule count, pattern length, and per-match timeout; invalid or duplicate rule IDs fail options validation with the offending ID.
- [ ] ac-3: Default rules cover bearer tokens, JWTs, private keys, common API-key and secret assignments, connection-string passwords, and known provider token prefixes.
- [ ] ac-4: Replacement is deterministic as [REDACTED:<rule-id>] and does not expose captured secret fragments.
- [ ] ac-5: SessionLogSanitizingService is the outermost ISessionLogService read decorator for HTTP/federation and is also registered in stdio.
- [ ] ac-6: Regex timeout during a read fails closed for the affected value, emits rule diagnostics without the input value, and never returns the unsanitized value.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TR-MCP-SESSIONLOGSAN-001; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

#### TR-MCP-SESSIONLOGSAN-002: Deterministic sanitizer timeout redaction and continuation

Status: not reassessed by this plan amendment.

The session-log sanitizer per-rule regex evaluation SHALL be exposed through an injectable seam (RegexReplaceInvoker, defaulting to Regex.Replace in production) so affected-field timeout redaction and continuation of other fields can be verified deterministically - forcing exactly one field/rule to raise RegexMatchTimeoutException - independent of catastrophic-backtracking wall-clock jitter. Acceptance Criteria: (AC1) with an injected invoker that raises RegexMatchTimeoutException for the pathological field, SanitizeString returns [REDACTED:{ruleId}:timeout] for that field while other fields continue to be sanitized by their rules, across repeated isolated runs (no flake); (AC2) production behavior is unchanged (default invoker is Regex.Replace). Origin: BUG-TRIAGE-081 (SessionLogSanitizerTimeoutTests flaked under scheduler jitter; the deployed 1ms->100ms budget was a mitigation, not deterministic). Validated by TEST-MCP-SESSIONLOGSAN-002.
**Covered by:** FR: FR-MCP-SESSIONLOGSAN-001; TEST: TEST-MCP-SESSIONLOGSAN-001, TEST-MCP-SESSIONLOGSAN-002

**Acceptance Criteria:**
The source record expresses these obligations in prose. The criteria below make those same obligations explicit. AC1/AC2 preserve the identifiers already present in sanitizer prose; ac-catalog-* identifiers are authored by this amendment and are not represented as recovered source identities. No passing result is claimed.

- [ ] AC1: With an injected RegexReplaceInvoker raising RegexMatchTimeoutException for the pathological field, SanitizeString returns [REDACTED:{ruleId}:timeout] for that field while other fields continue to be sanitized; repeated isolated runs do not depend on wall-clock jitter.
- [ ] AC2: The production RegexReplaceInvoker defaults to Regex.Replace and preserves production sanitization behavior.

Definition source: docs/receipts/requirements-reconciliation/20261116T161236-post-handoff-007/legion2-requirements.json and omarchy-requirements.json, record TR-MCP-SESSIONLOGSAN-002; docs/receipts/federation/20261110-source-reconciliation.json. Existing satisfaction/evidence is not reset by this pending verification view.

### 16.5 Definition and population gate

The catalog contains 86 full requirement definitions (the 68 current/amended definitions plus 18 explicitly retained dependencies). SESSIONLOGCTX/SESSIONLOGSAN means exactly the six named members in section16.4, not an unnamed wildcard backlog. No historical record outside this catalog is imported by this amendment. Before any record is written, re-read the current host record, preserve unrelated metadata/links, and populate only the definition and criterion fields actually governed here. Matching criteria retain existing identity, satisfaction and evidence. New or materially changed criteria require new evidence and remain unsatisfied. No record is deleted, no existing canonical ID is replaced, and no task or requirement is marked complete. Server readback must verify definition text and every criterion after each batch. Phase0 execution remains stopped until the operator resumes it.
