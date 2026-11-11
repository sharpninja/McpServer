# Functional Requirements

Canonical MCP projection of the amended federation records, exported by Codex on 2026-11-10 from confirmed plugin responses. Criteria are pending. The current-scope coverage index identifies applicable obligations; original host records are retained separately as audit evidence. This slice does not replace unrelated requirements.

## FR-MCP-103: Autonomous committed-history federation

Status: pending

Enrolled nodes execute and persist authorized business work locally while offline and exchange explicit version commits through LAB-OMARCHY. The hub coordinates project enrollment, guarded publication and reconciliation; it is not the executor of ordinary node requests. DirectProxy remains an explicitly selected legacy capability under FR-MCP-077. LocalProxy forwarding, outage command replay and read-switch are superseded for versioned federation.

- [ ] review20261110-1: Given A cannot reach the hub, when it edits and commits T1, then local read returns the change and publication remains pending.
- [ ] review20261110-2: Given H and A commit concurrently, when synchronization completes, then both histories survive or a visible review blocks only that branch.

## FR-MCP-125: Legacy federation compatibility without general keyserver gating

Status: pending

Keep legacy HMAC envelope compatibility separate from transaction signing and protected diffgram encryption. Under the later FR-MCP-173 decision, general federation apply/control and audit do not invoke the QuadBrain coordinator or fail closed merely because it is degraded. Actual QuadBrain operations retain their coordinator rules.

- [ ] review20261110-1: Given legacy signed envelopes, when explicit legacy-mode tests run, then compatibility remains intact.
- [ ] review20261110-2: Given degraded QuadBrain coordinator, when authorized general federation control or audit runs, then no keyserver call occurs and the operation is not blocked by coordinator state.

## FR-MCP-174: autonomous local execution

Status: pending

Every node, including the hub, performs authorized local business operations and persists results without hub connectivity.

- [ ] ac-NodeWorksWithHubUnavailable: Given A/main=C0 and the hub link is down, when A changes TODO T1.title from old to local, then the local API succeeds, a local reread returns local, and no hub request or history commit occurs.
- [ ] ac-HubUsesSameLocalExecution: Given H/main=C0 and the gateway is stopped, when H edits T1.title, then H persists it locally while remote/main remains C0.
- [ ] ac-OfflineLocalReadAfterWrite: Given the prior offline edit, when the application and Dolt restart, then T1.title remains local and main remains C0 until an explicit version commit.

## FR-MCP-175: explicit version commits and publication boundary

Status: pending

Only an explicit version-history commit triggers publication; drafts and SQL saves remain local. Commit retries recover the same history identity.

- [ ] ac-SaveDoesNotPublish: Given main=C0 and remote=C0, when SaveChanges persists T1.title=draft, then main and remote stay C0 and no publication intent is emitted.
- [ ] ac-VersionCommitTriggersSync: Given that draft and a valid preview, when commit request R1 succeeds, then main=A1, the reply says committed_locally, and one recoverable publication intent targets A1.
- [ ] ac-HubRequestDoesNotCommitDraft: Given B has a draft and desired head A1, when B polls, then outcome=waiting_local_work, B/main=C0 and the draft value is unchanged.
- [ ] ac-OfflineCommitDiscoveredAfterRestart: Given A1 committed while disconnected and a crash before intent persistence, when A restarts, then history scanning rediscovers A1 exactly once and publishes it after reconnect.

## FR-MCP-176: logical project enrollment and initial clone

Status: pending

Nodes enroll a logical project under immutable shared identity while retaining host-local paths and refusing destructive clones.

- [ ] ac-DifferentHostPathsSameProject: Given LAB path /home/sharpninja/github/McpServer and LEGION path F:/github/McpServer, when both enroll as ProjectId Atlas, then both reference Atlas and retain their distinct local paths.
- [ ] ac-CloneRefusesNonemptyTarget: Given target has TODO X, when clone is requested, then409 target_not_empty leaves X and all target files unchanged.
- [ ] ac-UnauthorizedProjectDenied: Given node A has only Atlas membership, when it enrolls or clones SecretProject, then403 unauthorized_project returns no rows, objects or project existence details.

## FR-MCP-177: branches across server and plugin context

Status: pending

First-release project branches support isolated work, explicit sharing and reviewed merge; session selection never leaks between connections.

- [ ] ac-PooledConnectionDoesNotLeakBranch: Given main T1.title=main and feature/search T1.title=feature, when alternating pooled reads run100 times, then each returns only its selected value.
- [ ] ac-FeaturePublicationLeavesMainUnchanged: Given main=C0 and feature/search=F1, when F1 publishes, then remote feature=F1 and all main heads remain C0.
- [ ] ac-BranchMergeRequiresExpectedHeads: Given a preview for main=C0 and feature=F1 but feature advances to F2, when merge submits the old preview, then409 head_changed creates no commit and changes no ref.

## FR-MCP-178: bidirectional committed-history exchange

Status: pending

Exchange committed history bidirectionally through guarded publication, preserving every accepted ancestor and the hub independent working copy.

- [ ] ac-ThreeWriterCleanConvergence: Given C0 contains T1.title=old,T1.description=old,T2.priority=low, when A1 changes title=A, B1 changes description=B and H1 changes T2.priority=high, then merges M1(A1,B1), M2(M1,H1) converge all heads to M2 with values A,B,high and all three commits in ancestry.
- [ ] ac-HubDraftDoesNotBlockRemote: Given H has an uncommitted title draft at C0, when A publishes A1, then remote=A1, H remains C0 with its draft, and B can accept A1.
- [ ] ac-ConcurrentPublishPreservesBothHistories: Given A1 and B1 descend from C0, when both accept requests expect C0, then one succeeds and the other receives409 head_changed; after merge M1 publishes, both A1 and B1 remain ancestors.

## FR-MCP-179: hub control table and polling

Status: pending

Use a durable hub subscription table with polling, generation and worker fencing, plus a fresh hub epoch after restore.

- [ ] ac-PollReadDoesNotAcknowledge: Given desired=7 and reported=6, when poll runs twice, then both responses request7 and stored reported remains6.
- [ ] ac-AckOldGenerationPreservesNewRequest: Given desired becomes8 while report7 is in flight, when report7 arrives, then reported=7, desired=8 and the subscription remains pending.
- [ ] ac-MissedNotificationReconciles: Given remote=A1 but control still names C0 after a crash, when reconciliation runs, then it records A1 and advances desired generation without producing another commit.
- [ ] ac-HubRestoreFromOlderBackup: Given a node remembers epoch E1 generation90 and the hub restores a backup at generation40, when the hub reopens, then epoch E2 differs from E1; E1 reports fail409 hub_epoch_changed and no head is force-reset. Node A1 is retained until verified ancestry recovery republishes missing history.

## FR-MCP-180: reviewed conflicts and valid merges

Status: pending

Conflicts and invalid merges preserve valid live data, require authorized review and remain visible with ageing while unrelated publication continues.

- [ ] ac-SameCellConflictNeedsReview: Given base priority=low, A1 priority=high and B1 priority=medium, when B integrates A1, then needs_review shows low/medium/high, B/main remains B1 and no public ref is overwritten.
- [ ] ac-DeleteUpdateNeedsReview: Given A1 deletes T1 and B1 edits T1.title, when histories merge, then needs_review retains B1 and the deleted/edited alternatives for review.
- [ ] ac-CleanMergeCanFailDomainValidation: Given separate commits add T1 dependsOn T2 and T2 dependsOn T1, when Dolt merges without cell conflicts, then domain validation records dependency_cycle and leaves the prior valid live head unchanged.
- [ ] ac-StaleReviewRejected: Given a resolution preview binds local=B1 and incoming=A1, when incoming advances to A2, then resolve returns409 stale_review with no merge commit.
- [ ] ac-ReviewAgeVisible: Given unresolved needs_review at12:00, when the clock reaches12:15, then plugins/status and the operator inbox show warning with ReviewId and age900s; at24h severity is critical, with at most one repeat per hour.
- [ ] ac-UnrelatedPublicationContinues: Given Atlas/main is blocked by review R1, when Atlas/feature publishes F2 and another project publishes X1, then both succeed while Atlas/main remains unchanged.
- [ ] ac-SafeTodoMergeRules: Given A changes title and B changes description with no domain violation, when merge runs, then both values survive; competing priority or completion edits produce needs_review instead of choosing a timestamp winner.

## FR-MCP-181: recovery without lost or repeated work

Status: pending

Recover synchronization, receipts and restored hub state without losing local work, inventing success or replaying business effects.

- [ ] ac-LostPushAckDoesNotDuplicateCommit: Given remote accepted A1 and reply was lost, when request R1 retries, then it returns A1 and the existing receipt; no A2 commit is created.
- [ ] ac-OldWorkerEpochCannotReportSuccess: Given worker epoch changes4 to5, when epoch4 reports success, then409 stale_worker_epoch leaves current receipt and desired state unchanged.
- [ ] ac-DirtyBranchDefersApply: Given B/main=C0 with title=draft, when A1 is fetched, then outcome=waiting_local_work, main stays C0 and title remains draft; at15min status/inbox shows the age warning.
- [ ] ac-IncompatibleSchemaDoesNotApply: Given B supports schema1 and incoming S1 requires schema2, when sync runs, then409 incompatible_schema leaves B's live data/head unchanged and records the required version.

## FR-MCP-182: collision-resistant and preserved identity

Status: pending

Prevent generated-ID collisions, preserve permanent legacy aliases, detect duplicate work and atomically reserve human-assigned IDs.

- [ ] ac-TwoNodesCreateDistinctIds: Given tags LA and LG and both counters start1, when both create a TODO, then generated IDs differ and both records survive reconciliation.
- [ ] ac-BranchRevertDoesNotReuseSequence: Given LA allocated sequence42, when main reverts and another branch creates a record, then its sequence is greater than42.
- [ ] ac-LegacyCollisionPreservesReferences: Given LAB and LEGION have different entities named TODO-7, when migration resolves the collision, then scoped aliases resolve each original identity and every relational reference follows its reviewed canonical mapping; historical text remains byte-identical.
- [ ] ac-LegacyIdsRemainValid: Given existing IMPL-UNITGREEN-001 and old TODO references, when imported and read by their original IDs, then they resolve through permanent aliases without rewriting commits, PR text, session logs, memories or documents.
- [ ] ac-DifferentIdsSameTodoDetected: Given LAB TODO-7 and LEGION TODO-91 share normalized title Fix login, description and dependency set, when migration plan runs, then it emits a duplicate candidate with both IDs and evidence; until explicit review, neither is merged or deleted.
- [ ] ac-SimultaneousHumanIdReservation: Given human ID IMPL-UNITGREEN-001 is unreserved, when two nodes reserve it concurrently, then exactly one receives201 and the other409 human_id_reserved; retry by the winning requestId returns the same reservation.
- [ ] ac-OfflineHumanIdPublicationDenied: Given an offline-created provisional TODO requesting an unreserved human ID, when it reconnects and publishes, then409 human_id_unreserved retains the local record until reservation or reviewed alias resolution.

## FR-MCP-183: first-party plugin federation contract

Status: pending

Expose consistent typed federation, review, identity, audit, migration and maintenance workflows through every first-party plugin.

- [ ] ac-EveryPluginUsesSameContract: Given one fixture for commit R1, conflict R2 and migration R3, when each installed first-party plugin calls its wrapper, then normalized IDs, heads, error codes and audit agent identity match the typed client result.
- [ ] ac-OldServerReportsUnsupportedCapability: Given server lacks federation contract1, when a plugin invokes commit, then unsupported_contract is shown and no raw SQL or shell fallback executes.
- [ ] ac-ReconnectRevalidatesContext: Given a cached leaseFence4 and session branch feature, when reconnect discovers fence5, then the plugin retains visible context but refuses writes with lease_lost until reacquired.
- [ ] ac-StaleBranchReportedNotDeleted: Given a branch observation is older than24h, when status is displayed, then stale=true and observedUtc are visible; the branch, drafts and refs are not deleted.

## FR-MCP-184: portable data versus host-local state

Status: pending

Separate versioned project content from operational state while delivering permanent audit to the hub independently of commits and excluding secrets.

- [ ] ac-CloneOmitsSecrets: Given credentials and local machine paths exist in operational storage, when a project is cloned, then none appears in its versioned tables or history.
- [ ] ac-AuditReachesHub: Given A performs an offline TODO edit and creates audit E1, when connectivity returns without a Dolt commit, then hub audit contains exactly E1, A retains E1, and main/remote remain C0.
- [ ] ac-AuditRetryIdempotent: Given E1 was stored but its receipt was lost, when the same batch retries, then exactly one E1 exists and the same hash receipt returns; changed payload under E1 returns409 audit_id_conflict.
- [ ] ac-AuditBackfillVerified: Given source has100 TODO audit and20 generic audit rows, when seed/join audit backfill verifies, then all120 records have matching payload hashes and hub receipts; missing one blocks cutover.
- [ ] ac-FullProjectSessionGraphPreserved: Given an explicitly promoted sanitized session with turns, actions, dialog, commits/files, PlanFile and TodoId, when cloned, then every promoted child and relationship matches the source snapshot; automatic capture still uses the audit channel.
- [ ] ac-AcceptedHeadRebuildsBranchIndex: Given accepted head changes C0 to A1, when the derived index rebuild completes, then queries read A1 values and no C0 cache is served under the A1 key.
- [ ] ac-BackgroundWritersDoNotDirtyVersionedBranch: Given main=C0 is clean, when heartbeat, audit and automatic session capture run, then Dolt working-set fingerprint remains unchanged.
- [ ] ac-SecondAgentLoggingNeedsNoLease: Given agent1 owns the editing lease, when agent2 logs a session action, then its local audit persists without acquiring or changing agent1's lease.
- [ ] ac-CommitPreviewBlocksSecret: Given a promoted snapshot contains a fixture secret detected by the configured detector, when commit preview runs, then422 secret_detected creates no commit and outputs no secret value.
- [ ] ac-ReceiptsAndLogsContainNoCredentials: Given unique canary values are injected through protected secret references, when commands, retries and failures execute on both hosts, then scanning argv, receipts, logs, chat/audit payloads and generated files finds zero canary values.

## FR-MCP-185: scoped authorization and offline behavior

Status: pending

Reuse integrated IdentityServer for single-user node ownership, per-node client credentials, project scopes and revocation; preserve authorized offline work.

- [ ] ac-RawRemoteCannotBypassPublicationPolicy: Given valid A read credentials, when A attempts direct backend access or public-main push, then the backend is unreachable across the link or403 publication_required; remote/main remains C0.
- [ ] ac-ForeignProjectCloneDenied: Given token has Atlas.read but no SecretProject scope, when a remote fetch requests SecretProject, then403 unauthorized_project sends zero objects.
- [ ] ac-RevokedNodeCannotPublish: Given A holds a reference token, when IdentityServer disables A and revokes its grants before acceptance, then accept returns401 invalid_token and remote/main is unchanged.
- [ ] ac-OfflineLocalAuthorizationStillApplies: Given hub/IdentityServer is unreachable and a valid authorized local user, when a local TODO edit is requested, then it succeeds; an unauthorized local user still receives403.
- [ ] ac-LocalAppUserCannotPush: Given ordinary editing SQL credentials, when remote administrative procedures or public-ref writes are attempted, then permission is denied and no object publication occurs.
- [ ] ac-RevokedCredentialFailsFetchAndPublish: Given IdentityServer revocation completes, when A starts new fetch and publish requests with its old token, then both return401 invalid_token before transfer or ref mutation.
- [ ] ac-SqlPortUnreachableAcrossLink: Given both services run, when LAB probes10.42.0.143:3306 and LEGION probes10.42.0.214:3306, then both connections fail; local127.0.0.1:3306 health succeeds.
- [ ] ac-UnknownCertificatePinRejected: Given the peer uses an unapproved SPKI or wrong SAN, when control/remote TLS connects, then tls_peer_untrusted occurs before bearer transmission.

## FR-MCP-186: Dolt fourth-provider support and schema migration

Status: pending

Support Dolt as the fourth provider with dedicated schema migration, controlled schema-history distribution, Windows/Linux lifecycle and original-provider regressions.

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

## FR-MCP-187: original-provider-to-Dolt migration tool

Status: pending

Migrate all three original providers using verified seed then reviewed join, permanent identities, audit backfill and a source-retaining TODO-to-SQLite recovery export.

- [ ] ac-ThreeSourceProviderRoundTrip: Given the same fixture in SQLite,PostgreSQL,SQL Server, when seed migration runs, then counts, typed hashes, IDs, nulls, Unicode, decimals, UTC precision and FK relationships match in Dolt.
- [ ] ac-ResumeAfterBatchCommit: Given target batch1 committed before a crash but its acknowledgement was lost, when resume runs, then batch1 is recognized by journal and no duplicate rows appear.
- [ ] ac-ChangedSnapshotRejected: Given manifest source hash S0, when source is replaced by S1 before resume, then409 migration_source_changed writes no additional target rows.
- [ ] ac-FailedVerifyCannotCutover: Given one row hash differs, when cutover is requested, then409 migration_verification_failed leaves source routing active and writes closed on the target.
- [ ] ac-SourceRetainedAfterCutover: Given verified cutover completes, when target receives a new TODO, then source backup remains present, read-only and byte-identical; the new TODO exists only in Dolt until an explicit recovery export.
- [ ] ac-MigrationJoinPreservesCommonAncestor: Given seed C0 is published and join imports J1, when join completes in Ph6, then C0 is an ancestor of J1 and reviewed main M1; no second root is created.
- [ ] ac-MigrationJoinDivergentRowRequiresReview: Given seed T1.title=A and frozen join T1.title=B, when join planning runs, then an explicit keep/take/manual decision is required before J1 even if Dolt would fast-forward.
- [ ] ac-JoinCutoverRequiresReviewedMain: Given J1 is verified but main integration is unresolved, when cutover runs, then409 migration_verification_failed preserves source routing; only verified reviewed M1 permits cutover.
- [ ] ac-DoltTodoExportPreservesNewWrites: Given A1 adds TODO T2 after cutover, when export-sqlite targets A1, then the new SQLite DB contains T2 and all TODO children/aliases with matching hashes, while the original source remains unchanged.
- [ ] ac-SeedPhaseRejectsJoin: Given only Ph3 is implemented, when mode=join is requested, then409 unsupported_capability creates no target commit or routing change.

## FR-MCP-188: imported state does not repeat external effects

Status: pending

Imported project or audit state never replays external effects and general federation remains outside QuadBrain-only keyserver gating.

- [ ] ac-ImportDoesNotSendExternalAction: Given A1 records an already-sent GitHub update, when B integrates A1, then B state includes the result and outbound GitHub/email invocation count is0.
- [ ] ac-RetryDoesNotRepeatEffect: Given accepted commit A1 and effect E1, when sync retries10 times, then the effect ledger still has one E1 and no new external dispatch occurs.
- [ ] ac-ImportedOperationalRowsRemainInert: Given audit backfill contains command names and receipt payloads, when the hub ingests them, then they are stored as evidence and no business command executes.
- [ ] ac-FederationBypassesQuadBrainKeyserver: Given QuadBrain coordinator is degraded but local authorization passes, when federation commit/control/audit operations execute, then none invokes the keyserver and valid local work succeeds.

## FR-MCP-189: safe shared-host editing and commit ownership

Status: pending

Enforce branch edit ownership and fenced live-write serialization so no agent or sync worker captures or overwrites another agent draft.

- [ ] ac-CommitCannotCaptureOtherAgentDraft: Given agent1 owns a draft, when agent2 commits the same branch, then409 branch_busy creates no commit and the draft remains agent1-owned.
- [ ] ac-ExpiredLeaseFenced: Given fence4 expires and fence5 is issued, when a fence4 edit arrives, then409 lease_lost persists no row changes.
- [ ] ac-PreviewChangeRequiresReview: Given preview fingerprint W1, when the working set becomes W2, then commit with W1 returns409 working_set_changed and requires a fresh preview.
- [ ] ac-LeaseLostDuringWriteRejected: Given a write admitted under fence4, when expiry occurs mid-transaction, then that write finishes under the gate before fence5 takeover; a later fence4 write is rejected.
- [ ] ac-CommitRetryReturnsSameHash: Given request R1 created A1 and reply was lost, when R1 retries after restart, then A1 is returned by its commit trailer and history contains no duplicate commit.
- [ ] ac-IncomingMergeCannotRaceAdmittedEdit: Given an edit holds the branch gate, when the integration worker requests a live head move, then it waits; after the edit finishes, fingerprint revalidation detects the draft and returns waiting_local_work without overwriting it.

## FR-MCP-AUDIT-001: Permanent audit with append-only hub delivery

Status: pending

Audit remains permanent locally and is delivered to the hub append-only unversioned store independently of project commits. Retain immutable runtime roles, readable legacy payloads and versioned compressed snapshots/diffs/metadata. Audit is excluded from branchable Dolt project history and wiki exports, not from hub delivery. Backfill TODO and generic audit history with verified hashes and receipts.

- [ ] review20261110-1: Given offline audit E1, when reconnect occurs without a version commit, then the hub stores E1 once and the node retains it.
- [ ] review20261110-2: Given runtime storage credentials, when update/delete of any existing audit row is attempted, then it is denied.
- [ ] review20261110-3: Given legacy and new compressed payload fixtures, when read/migrated on all supported providers, then their decoded values and historical text remain unchanged.

## FR-MCP-FED-BACKFILL-001: Complete verified project and audit migration backfill

Status: pending

Inventory every prior domain: TODO and children/links/doc metadata/audit; complete session graph; requirements/AC/mappings/scope layers; memory/versions; use cases/actors; source documents and derived chunks; triage; tools/buckets; agents; brain slots; handoff. Each maps to versioned data, hub-bound unversioned audit, host-local operational state or regenerable derived state with explicit counts/reason. Seed/join replace signed command backfill for versioned mode. Reads and writes always remain local; no read-switch.

- [ ] review20261110-1: Given the domain manifest, when migration planning runs, then every source table/writer has a classified destination or explicit exclusion and no unknown domain passes.
- [ ] review20261110-2: Given completed batches, when rerun, then no duplicate rows or external effects occur and already_applied counts are reported.
- [ ] review20261110-3: Given a missing source row or audit receipt, when verify runs, then migration_verification_failed blocks cutover.

## FR-MCP-FED-ROUTE-001: Federation project routing fails closed

Status: pending

Current versioned control/remote endpoints resolve immutable ProjectId and authenticated NodeId membership; local paths are host-specific mappings, never authority or fallback to the hub primary workspace. Preserve the old proxy-header routing contract only for explicitly configured legacy DirectProxy paths.

- [ ] review20261110-1: Given A is enrolled in Atlas, when its control request omits ProjectId or requests another project, then400 project_required or403 unauthorized_project returns no default-workspace data.
- [ ] review20261110-2: Given legacy proxy routing is explicitly selected, when valid normalized path or GlobalWorkspaceId is supplied, then both resolve the same scoped workspace.

## FR-MCP-FED-WSSYNC-001: Portable project configuration and local machine boundaries

Status: pending

Version portable project configuration through explicit commits; retain host RunAs, executable paths, DataDirectory, tunnel settings and local filesystem mappings locally. CurrentRequirementLayerKey remains protected hub-admin-owned policy: heartbeat cannot overwrite it and drift is visible. Local offline work uses the last authorized policy. No blanket host-configuration replication.

- [ ] review20261110-1: Given A changes host RunAs or DataDirectory, when heartbeat/clone runs, then those values never replace H configuration.
- [ ] review20261110-2: Given CurrentRequirementLayerKey differs, when heartbeat runs, then hub-owned value remains and drift is reported.
- [ ] review20261110-3: Given an authorized portable prompt-template edit is committed, when synced, then peers receive its sanitized value.

## FR-MCP-MEMORY-008: Versioned workspace memory federation

Status: pending

Workspace-scoped portable memories and their versions are committed project data; local edits work offline and explicit version commits trigger exchange. Global/machine-specific memory remains operational. Conflicting updates/deletes require review. Preserve valid MEMORY-* identity, explicit scope and sanitizer boundaries. LocalProxy forwarding and signed command replay are superseded for this mode.

- [ ] review20261110-1: Given workspace memory M1 edited offline, when saved, then the local read changes but remote C0 does not; after explicit commit and sync the peer receives the version.
- [ ] review20261110-2: Given A deletes and B edits M1, when merged, then needs_review preserves both histories.
- [ ] review20261110-3: Given a global memory containing machine configuration, when cloning a project, then that memory is absent.

