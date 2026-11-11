# Three-system federation walkthrough and requirements

Author: Codex. Date: 2026-11-10. Proposed behavior for [the new architecture](2026-11-10-federation-dolt-architecture.md), not a report of deployed behavior. H=LAB-OMARCHY, A=PAYTON-LEGION2, B=isolated test peer FED-ATLAS-B. Project Atlas and symbolic commit names C0/A1/B1/H1/M1 are examples, not real records or hashes. The hub has a local working copy and a separate shared committed-history remote.

## Standard workflows

### U01: enroll and start the same project

H registers Atlas with immutable ProjectId and an initial approved C0. A and B enroll with distinct immutable node tags, map their own filesystem paths to Atlas, receive authorized subscriptions and clone C0. Each can now read and edit locally. An existing nonempty node database is inventoried/migrated, never overwritten by the clone. Server: enrollment, project identity, initial snapshot, scoped credentials. Plugins: discover/select project and branch; show local/remote heads. Requirements: FR-MCP-176,182,185,187.

### U02: node A edits, then commits

A edits a TODO. Its local draft survives restart, but H and B remain at C0 and no sync is triggered. A previews and explicitly commits A1; this wakes synchronization. After validated publication, the hub remote is A1 and H/B receive control requests. Their workers fetch and accept A1 on clean branches; their local reads now include the TODO. Server: explicit commits, publication and control generation. Plugins: diff/commit/status; distinguish local commit from publication. Requirements: FR-MCP-174,175,178,179,183,189.

### U03: work directly on the hub

H edits in its local working copy while A and B continue independently. H commits H1 and its normal worker publishes it to the shared remote; A/B receive requests. H's uncommitted draft before H1 neither travels nor prevents A/B publication because the hub remote is a separate repository. Requirements: FR-MCP-174,175,178,184.

### U04: simultaneous clean edits on all three systems

Starting at C0, A commits a title edit A1, B commits an unrelated description edit B1, and H commits a separate requirement H1. A publishes first. B fetches A1, Dolt merges A1+B1 into M1, validation passes and M1 publishes. H fetches M1, merges H1 into M2 and publishes. A/B accept M2. Every original commit remains an ancestor. A publication race returns head_changed and repeats fetch/merge; no force push. This assumes nonconflicting cells and valid domain relationships. Requirements: FR-MCP-178,181,188.

### U05: conflicting edits to the same field

A and B both change one requirement's priority from the same base to different values and commit independently. A publishes A1. B receives A1 while holding B1; merge reports a conflict. B retains its last valid local state, and H/A retain A1. B's plugin displays base/local/incoming values and requests review. A reviewed resolution creates M1 including both histories, passes validation and publishes; H/A then accept it. Changed heads invalidate an old resolution. Delete/update and schema conflicts use the same visible review boundary. Requirements: FR-MCP-180,181,183.

### U06: hub unreachable, then reconnect

The A/B-to-hub link fails; H retains local access to its own gateway and remote. H, A and B still execute local business operations and make H1/A1/B1 commits. H may publish H1 locally while A/B record pending publication; they do not return queued-only results for local business writes. On reconnect A merges H1+A1 into M1 and publishes, then B merges M1+B1 into M2 and publishes; H/A catch up to M2. They authenticate, reconcile remote heads and process requests plus their own unpublished commits. Merge/review follows U04/U05. An empty hub queue does not strand local commits. No automatic hub failover is claimed. Requirements: FR-MCP-174,175,178,179,181,185.

### U07: incoming commit while node B has a draft

B has uncommitted edits at C0. A publishes A1; B polls and fetches history, but records waiting_local_work instead of committing, discarding or overwriting its draft. B's user finishes the draft and explicitly commits B1 (or separately chooses another authorized disposition). Then reconciliation proceeds. Fetch succeeded does not mean the branch was synchronized. Requirements: FR-MCP-175,180,183,189.

### U08: divergent feature work and deliberate merge

A creates feature/search from C0 and commits F1. Publication shares feature/search; main remains C0 on all systems. B selects feature/search to inspect or continue it and commits F2. H can review the branch data. An authorized explicit merge into main creates M1 and triggers normal publication. Branch selection is per session, never a process-wide switch. No PR is created. Requirements: FR-MCP-177,180,183,189.

### U09: lost response or crash during synchronization

A publishes A1 but loses the success response. H has A1 and unsatisfied subscription state. A restarts, checks remote ancestry and its request receipt, and recognizes publication without creating A2 or repeating an external business action. A missed hub state update is repaired by head-generation reconciliation. A new worker epoch rejects stale reports from the old worker. Reporting generation g must not clear desired g+1. Requirements: FR-MCP-179,181,188.

### U10: migrate an existing project

Both real lab stores are SQLite. The migration tool additionally supports PostgreSQL and SQL Server fixture sources. It plans those sources, inventories local-only versus shared data, detects legacy-ID collisions and checks the Dolt schema. Ph3 delivers seed and all-domain/all-branch project recovery export (including the TODO subset); Ph6 adds join only after branch/commit/publication/review exist. Exactly one seed establishes C0 per project. Other nodes clone C0 and import their frozen legacy differences on migration branches descended from C0, then merge/review them normally. Verification accounts for source rows already in C0, mapped deltas and explicitly reviewed exclusions/deletions. Include TODO/generic audit history in the independent hub backfill and verify every durable receipt. Detect same-work/different-ID duplicates for review and preserve permanent origin-scoped aliases, without rewriting historical text. A failed verification leaves the original provider active. No unrelated root histories or implicit deletion of seed rows are allowed. Requirements: FR-MCP-182,184,186,187.

Same-identity rows with different legacy contents require a recorded keep-seed/take-join/manual-value review before the join commit, even if Dolt would merge cleanly. Verify the immutable migration commit and the reviewed main result; keep destination writes closed until integration and verification finish.

### U11: node running an older schema/application

H's maintenance role publishes a supported schema upgrade. B advertises an older capability and records incompatible_schema; it does not apply it or claim to be current. B may continue supported local work while publication is held. After preflight and binary upgrade, B merges S1 with its retained unpublished B1 to form M1, validates and publishes M1, then H/A accept M1. Both B1 and S1 remain ancestors; B does not simply replace B1 with S1. Each feature branch catches up separately through review/validation. Nodes never independently run EF migrations on versioned project startup. Incompatible contract changes wait for subscriber acknowledgement or explicit de-enrollment/recovery. Requirements: FR-MCP-181,183,186.

Before swapping B's binary, its owners explicitly commit or resolve all drafts and the schema head must be available locally; otherwise upgrade waits without touching drafts. B attests every local branch, including unpublished ones. Maintenance preflight gates deployment. Each branch reopens only after its local worker merges the designated schema history and validation passes; a catch-up conflict blocks that branch only. The new EF model never edits an unsupported dirty working set or independently executes project migrations.

### U12: two agents on one host

Two plugins select Atlas/main on A. Only the editing lease owner may mutate its working set. The other agent reads or selects a separate branch. The first agent's commit preview binds to its expected working-set fingerprint; takeover cannot silently absorb its draft. Plugin reconnect revalidates the lease and context. Requirements: FR-MCP-177,183,189.

### U13: audit reaches the hub without a project commit

Precondition: A is enrolled in Atlas; main=C0 and A has authorized local audit storage. A edits T1 while offline, appending immutable audit E1 and its outbox entry. On reconnect, audit/batches sends E1 through the IdentityServer gateway, independent of Dolt commits. The hub stores it append-only and returns a hash-bound receipt. A lost receipt causes retry, not duplication; the node retains E1 permanently. Postcondition: one E1 at each origin/hub ledger, main and remote still C0, local draft preserved. Errors: invalid_token, unauthorized_project, audit_id_conflict. Requirements: FR-MCP-AUDIT-001, FR-MCP-184/185/188. Ph2/Ph3/Ph5/Ph7.

### U14: duplicate work, permanent aliases and human IDs

Precondition: frozen LAB TODO-7 and LEGION TODO-91 describe the same task. Planning emits similarity evidence; it does not infer identity automatically. An authorized review chooses keep-both or one canonical entity with both scoped IDs retained as aliases. Historical commits, PRs, session logs, memories and documents remain unchanged. For new human labels, two concurrent reservations of IMPL-UNITGREEN-001 yield one owner and one human_id_reserved response. Offline work uses a tagged provisional ID until reservation succeeds. Postcondition: every old reference remains resolvable with origin context, no unreviewed row disappears, and an ambiguous unqualified ID reports ambiguous_legacy_id. Requirements: FR-MCP-182/187. Ph2/Ph3/Ph5/Ph6.

### U15: restore a hub older than a node

Precondition: A1 was published under epoch E1/generation90 and remains on A/B, but the backup contains C0/generation40. Restore keeps ingress disabled, installs a fresh E2 and advertises only verified restored heads. A node's E1 report receives hub_epoch_changed; it retains A1. The hub requests candidate history and validates ancestry before republishing A1 under E2. Audit inventories reconcile independently and permanent node ledgers resend missing records. Postcondition: no force push or forced node rewind, all recovered history preserved, receipts refer to E2 and no missing audit row is falsely acknowledged. Requirements: FR-MCP-179/181/184/188. Ph5/Ph6/Ph8.

## UC preconditions, success and exception coverage

U01 starts with authenticated owner/node enrollment and empty target; succeeds with shared C0 and distinct local paths; nonempty/foreign target is denied. U02 starts with editable C0; succeeds with explicit A1 and eventual peer acceptance; draft or failed publication remains local. U03 is U02 on H with a separate shared remote. U04 starts at common C0 and three distinct edits; succeeds at M2 preserving H1/A1/B1/M1; conflict takes U05. U05 starts with same-base conflicting commits; succeeds only at an authorized valid merge; stale preview is rejected. U06 starts with enrolled peers and link loss; succeeds after M1/M2 reconciliation without queued-only local writes. U07 starts with a dirty B branch; succeeds only after the owner explicitly disposes the draft and merge validates; waiting age remains visible. U08 starts with C0 and authorized feature creation; sharing leaves main unchanged and explicit merge creates M1. U09 starts with an accepted commit and lost reply; retry returns the same hash/receipt without replay. U10 starts with retained frozen sources; success requires seed/join accounting, reviewed aliases, audit receipts and verified main; any failed verify blocks cutover. U11 starts with an unsupported incoming schema and unpublished work; success preserves B1 plus S1 ancestry after preflight and M1 validation; dirty/unattested branches block upgrade. U12 starts with agent1's edit lease; agent2 reads or selects another branch; no commit captures agent1's draft. U13-U15 state their own preconditions/postconditions above. Exact row/head/error assertions are canonical TEST-MCP-222..237 criteria, not inferred from diagram labels.
## New requirement and feature inventory

The IDs below are the intended canonical records; the registration receipt establishes whether persistence succeeded. Status is pending throughout. Each FR maps one-to-one in order to TR-MCP-FEDERATION-001..016 and TEST-MCP-222..237. This inventory is additive; conflicting legacy-mode descriptions require explicit reconciliation, not blanket deletion.

### FR-MCP-174: autonomous local execution

Every node, including the hub, performs authorized local business operations and persists results without hub connectivity.

Phase: Ph2. Workflows: U02, U03, U06. Tests: NodeWorksWithHubUnavailable; HubUsesSameLocalExecution; OfflineLocalReadAfterWrite.

### FR-MCP-175: explicit version commits and publication boundary

Only an explicit version-history commit triggers publication; drafts and SQL saves remain local. Commit retries recover the same history identity.

Phase: Ph4. Workflows: U02, U07, U09. Tests: SaveDoesNotPublish; VersionCommitTriggersSync; HubRequestDoesNotCommitDraft; OfflineCommitDiscoveredAfterRestart.

### FR-MCP-176: logical project enrollment and initial clone

Nodes enroll a logical project under immutable shared identity while retaining host-local paths and refusing destructive clones.

Server: project discovery, scoped enrollment and structured refusal diagnostics. Plugin: project discovery/selection, enrollment progress and explicit actionable diagnostics.

Phase: Ph2/Ph5. Workflows: U01. Tests: DifferentHostPathsSameProject; CloneRefusesNonemptyTarget; UnauthorizedProjectDenied; PluginEnrollmentDiagnostics.

### FR-MCP-177: branches across server and plugin context

First-release project branches support isolated work, explicit sharing and reviewed merge; session selection never leaks between connections.

Phase: Ph2/Ph4/Ph6. Workflows: U08, U12. Tests: PooledConnectionDoesNotLeakBranch; FeaturePublicationLeavesMainUnchanged; BranchMergeRequiresExpectedHeads.

### FR-MCP-178: bidirectional committed-history exchange

Exchange committed history bidirectionally through guarded publication, preserving every accepted ancestor and the hub independent working copy.

Phase: Ph5/Ph6. Workflows: U03, U04, U08. Tests: ThreeWriterCleanConvergence; HubDraftDoesNotBlockRemote; ConcurrentPublishPreservesBothHistories.

### FR-MCP-179: hub control table and polling

Use a durable hub subscription table with polling, generation and worker fencing, plus a fresh hub epoch after restore.

Phase: Ph5. Workflows: U02, U06, U09, U15. Tests: PollReadDoesNotAcknowledge; AckOldGenerationPreservesNewRequest; MissedNotificationReconciles; HubRestoreFromOlderBackup.

### FR-MCP-180: reviewed conflicts and valid merges

Conflicts and invalid merges preserve valid live data, require authorized review and remain visible with ageing while unrelated publication continues.

Phase: Ph6/Ph7. Workflows: U05, U07, U08. Tests: SameCellConflictNeedsReview; DeleteUpdateNeedsReview; CleanMergeCanFailDomainValidation; StaleReviewRejected; ReviewAgeVisible; UnrelatedPublicationContinues; SafeTodoMergeRules.

### FR-MCP-181: recovery without lost or repeated work

Recover synchronization, receipts and restored hub state without losing local work, inventing success or replaying business effects.

Server: durable operation resume/status/safe cancellation. Plugin: expose resume, status and cancel with completed-versus-pending effects; never claim cancellation reverses published history.

Phase: Ph5/Ph6. Workflows: U06, U07, U09, U15. Tests: LostPushAckDoesNotDuplicateCommit; OldWorkerEpochCannotReportSuccess; DirtyBranchDefersApply; IncompatibleSchemaDoesNotApply; PluginRecoveryControls.

### FR-MCP-182: collision-resistant and preserved identity

Prevent generated-ID collisions, preserve permanent legacy aliases, detect duplicate work and atomically reserve human-assigned IDs.

Server: provenance, scoped aliases, collision evidence and reservation outcomes. Plugin: expose provenance/collision diagnostics and the authorized resolution path without rewriting historical references.

Phase: Ph2/Ph3/Ph5/Ph6. Workflows: U01, U10, U14. Tests: TwoNodesCreateDistinctIds; BranchRevertDoesNotReuseSequence; LegacyCollisionPreservesReferences; LegacyIdsRemainValid; DifferentIdsSameTodoDetected; SimultaneousHumanIdReservation; OfflineHumanIdPublicationDenied; PluginIdentityDiagnostics.

### FR-MCP-183: first-party plugin federation contract

Expose consistent typed federation, review, identity, audit, migration and maintenance workflows through every first-party plugin.

Phase: Ph4/Ph7. Workflows: U01, U02, U05, U07, U08, U11, U12, U13, U14, U15. Tests: EveryPluginUsesSameContract; OldServerReportsUnsupportedCapability; ReconnectRevalidatesContext; StaleBranchReportedNotDeleted.

### FR-MCP-184: portable data versus host-local state

Separate versioned project content from operational state while delivering permanent audit to the hub independently of commits and excluding secrets.

Phase: Ph2/Ph3/Ph4/Ph5/Ph6/Ph7. Workflows: U10, U13. Tests: CloneOmitsSecrets; AuditReachesHub; AuditRetryIdempotent; AuditBackfillVerified; FullProjectSessionGraphPreserved; AcceptedHeadRebuildsBranchIndex; BackgroundWritersDoNotDirtyVersionedBranch; SecondAgentLoggingNeedsNoLease; CommitPreviewBlocksSecret; ReceiptsAndLogsContainNoCredentials.

### FR-MCP-185: scoped authorization and offline behavior

Reuse integrated IdentityServer for single-user node ownership, per-node client credentials, project scopes and revocation; preserve authorized offline work.

Server: least-privilege capabilities plus server-side authorization on every call. Plugin: show available rights and explicit denial; capability display never substitutes for authorization.

Phase: Ph0/Ph2/Ph5. Workflows: U01, U06, U13. Tests: RawRemoteCannotBypassPublicationPolicy; ForeignProjectCloneDenied; RevokedNodeCannotPublish; OfflineLocalAuthorizationStillApplies; LocalAppUserCannotPush; RevokedCredentialFailsFetchAndPublish; SqlPortUnreachableAcrossLink; UnknownCertificatePinRejected; PluginLeastPrivilegeDenial.

### FR-MCP-186: Dolt fourth-provider support and schema migration

Support Dolt as the fourth provider with dedicated schema migration, controlled schema-history distribution, Windows/Linux lifecycle and original-provider regressions.

Phase: Ph0/Ph1/Ph6/Ph8. Workflows: U11. Tests: FourProvidersResolve; DoltBaselineAndUpgrade; OriginalProviderMigrationsUnchanged; UnsupportedSchemaRejected; VersionedProjectNotMigratedAtStartup; OldSchemaUnpublishedCommitMergesAfterUpgrade; FeatureBranchSchemaCatchUp; DirtyDraftBlocksBinaryUpgrade; ContractBlockedUntilLocalBranchesAttested; DeploymentPreflightBlocksDirtyOrUnattestedBranch; WindowsWriterLockAndRestart; RealHostAcceptanceMatrix.

### FR-MCP-187: original-provider-to-Dolt migration tool

Migrate all three original providers using verified seed then reviewed join, permanent identities, audit backfill and a source-retaining all-domain/all-branch project-to-SQLite recovery export.

Phase: Ph3 seed/export; Ph6 join. Workflows: U10, U14. Tests: ThreeSourceProviderRoundTrip; ResumeAfterBatchCommit; ChangedSnapshotRejected; FailedVerifyCannotCutover; SourceRetainedAfterCutover; MigrationJoinPreservesCommonAncestor; MigrationJoinDivergentRowRequiresReview; JoinCutoverRequiresReviewedMain; DoltTodoExportPreservesNewWrites; SeedPhaseRejectsJoin; FullProjectRecoveryPreservesAllDomains.

### FR-MCP-188: imported state does not repeat external effects

Imported project or audit state never replays external effects and general federation remains outside QuadBrain-only keyserver gating.

Server: synchronization applies data without executing external operations. Plugin: distinguish history/data synchronization from separately authorized external effects in preview, status and receipts.

Phase: Ph2/Ph6. Workflows: U04, U09, U13. Tests: ImportDoesNotSendExternalAction; RetryDoesNotRepeatEffect; ImportedOperationalRowsRemainInert; FederationBypassesQuadBrainKeyserver; PluginEffectBoundary.

### FR-MCP-189: safe shared-host editing and commit ownership

Enforce branch edit ownership and fenced live-write serialization so no agent or sync worker captures or overwrites another agent draft.

Server: editing owner, request/release and fenced takeover contracts. Plugin: display editing owner, request/release context and offer another branch without capturing another agent draft.

Phase: Ph2/Ph4/Ph6. Workflows: U07, U12. Tests: CommitCannotCaptureOtherAgentDraft; ExpiredLeaseFenced; PreviewChangeRequiresReview; LeaseLostDuringWriteRejected; CommitRetryReturnsSameHash; IncomingMergeCannotRaceAdmittedEdit; PluginEditingOwnership.

## Planning boundaries

The operator requested Dolt support and the migration tool; no packages or deployments are authorized by this document alone. Exact provider/version compatibility and full entity partition remain Ph0 evidence gates. The table/poller/executor, plugin UX and validation are application work; the Dolt storage/history/merge algorithms are reused. PRs remain out of scope. Test names above are required behavior, not claims of existing or passing tests.

The new-contract slice retains its48 IDs and 16 mappings; the reconciliation register additionally covers legacy and cross-cutting requirements from both hosts. Each HV revision updates existing FR/TR/TEST records through the plugin and refreshes the export before Ph0 exit; no duplicate requirement set is created.
