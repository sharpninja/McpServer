# Technical Requirements

Canonical MCP projection of the amended federation records, exported by Codex on 2026-11-10 from confirmed plugin responses. Criteria are pending. The current-scope coverage index identifies applicable obligations; original host records are retained separately as audit evidence. This slice does not replace unrelated requirements.

## TR-MCP-AUDIT-004: Append-only unversioned audit delivery and export policy

Status: pending

Project history excludes secrets and automatic operational audit. This does NOT make audit local-only: every host retains permanent local audit and an append-only outbox delivers sanitized records to the hub unversioned ledger independently of project commits. AuditRecord key=(OriginNodeId,RecordId UUID); fields ProjectId,EntityKind,EntityId,OriginSequence int64,OccurredUtc,Actor,Action,PayloadVersion,PayloadHash,Payload. POST audit/batches validates token/project, inserts atomically by key and returns per-record durable receipts. Same key/hash is already_received; same key/different hash=409 audit_id_conflict. No UPDATE/DELETE permission. Local pruning is forbidden. Migration backfills TODO and generic audit history with counts/hashes and receipts. Credential values are excluded at capture, not silently replicated.

- [ ] review20261110-1: Given batchE1 was accepted and its reply lost, when retried, then one row and the same payload-hash receipt remain.
- [ ] review20261110-2: Given E1 key with a changed payload, when submitted, then409 audit_id_conflict performs no overwrite.
- [ ] review20261110-3: Given a wiki export or project clone, when generated, then it excludes credentials and operational audit rows while independent hub delivery remains enabled.

## TR-MCP-FED-001: Autonomous federation transport and compatibility contract

Status: pending

Use TR-MCP-FEDERATION-001..016 as the current versioned contract. Configure immutable ProjectId,NodeId,branch,IdentityServer client and hub gateway; never forward ordinary Dolt-project requests. Preserve legacy DirectProxy routing and loop protection in a distinct mode. Never run legacy mutation replay and versioned publication for the same project.

- [ ] review20261110-1: Given versioned mode, when a TODO request arrives, then LocalProxy middleware is not invoked.
- [ ] review20261110-2: Given explicit DirectProxy mode, when a foreign workspace is addressed, then correct routing and loop protection remain intact.

## TR-MCP-FED-BACKFILL-001: Seed, join and audit backfill contracts

Status: pending

Use TR-MCP-FEDERATION-014 seed in Ph3 and join after review/publication in Ph6. Audit delivery follows TR-MCP-AUDIT-004. Manifest preserves typed values, logical IDs/aliases, versions, timestamps and full session graph. Per-table stable keyset batches and immutable source fingerprint support resume. Import never fans out original commands; only the reviewed version commit may publish.

- [ ] review20261110-1: Given migration J1 contains all source rows and audit records, when verify executes, then source accounting at J1 and final invariants at M1 both pass, with durable audit receipts.

## TR-MCP-FED-MEMORY-001: Memory scope and history contract

Status: pending

Route the existing memory API locally. Version workspace-scoped memory rows and version graph through the Dolt project context; keep global/machine memory in operational storage. Validate memory identity and scope on local writes and merge. No separate replay adapter is required for Dolt mode; retain explicit legacy adapter compatibility only.

- [ ] review20261110-1: Given MemoryId M1 with versions V1,V2 in Atlas, when A1 is cloned, then both versions and their links match and no global memory is included.

## TR-MCP-FED-ROUTE-001: Scoped project resolution and legacy route isolation

Status: pending. Revision audit amended 2026-11-10.
Versioned APIs require ProjectId and derive NodeId from IdentityServer client membership. Missing project=400 invalid_request; foreign project=403 unauthorized_project. Request paths use the syntax-aware normalizer, not host Path.GetFullPath. Legacy header lookup remains mode-scoped and never supplies fallback identity to versioned routes.

**Acceptance Criteria:**
- [ ] review20261110-1: Given a federation hop with unresolved workspace in legacy mode, when routed, then a4xx diagnostic is returned and no primary workspace fallback occurs.

## TR-MCP-FED-WSSYNC-001: Configuration partition and protected policy contract

Status: pending

Project manifest classifies fields individually. Portable Name/prompt-template content may be versioned; IsPrimary,RunAs,AgentPath,DataDirectory,TunnelProvider and resolved TodoPath are host-local. Protected CurrentRequirementLayerKey changes require hub-admin permission and a versioned policy commit, never heartbeat overwrite. Legacy payload compatibility is tested only in legacy mode.

- [ ] review20261110-1: Given older legacy registration lacks fields, when processed, then its existing mode defaults remain without mutating versioned project settings.

## TR-MCP-FEDERATION-001: autonomous local execution contract

Status: pending. Revision audit amended 2026-11-10.
Local request routing binds immutable ProjectId, NodeId and Branch to one DbContext and branch write gate. Dolt-mode domain requests never invoke LocalProxy forwarding or wait for the hub. Existing local authentication remains mandatory; federation reference-token introspection is not a dependency of local work. Result fields distinguish local persistence, version commit and publication.

**Acceptance Criteria:**
- [ ] ac-NodeWorksWithHubUnavailable: Given A/main=C0 and the hub link is down, when A changes TODO T1.title from old to local, then the local API succeeds, a local reread returns local, and no hub request or history commit occurs.
- [ ] ac-HubUsesSameLocalExecution: Given H/main=C0 and the gateway is stopped, when H edits T1.title, then H persists it locally while remote/main remains C0.
- [ ] ac-OfflineLocalReadAfterWrite: Given the prior offline edit, when the application and Dolt restart, then T1.title remains local and main remains C0 until an explicit version commit.

## TR-MCP-FEDERATION-002: explicit version commits and publication boundary contract

Status: pending. Revision audit amended 2026-11-10.
POST /qbrainai/federation/projects/{projectId}/branches/{branch}/commits accepts requestId UUID, expectedHead, expectedWorkingSet, leaseFence, author and message. Success is 201 with commitHash and publicationState=committed_locally. Persist requestId in the Dolt commit trailer. Retry returns the original hash. Head/fingerprint/fence mismatch is HTTP409 with head_changed/working_set_changed/lease_lost. SaveChanges and control.poll never create version commits.

**Acceptance Criteria:**
- [ ] ac-SaveDoesNotPublish: Given main=C0 and remote=C0, when SaveChanges persists T1.title=draft, then main and remote stay C0 and no publication intent is emitted.
- [ ] ac-VersionCommitTriggersSync: Given that draft and a valid preview, when commit request R1 succeeds, then main=A1, the reply says committed_locally, and one recoverable publication intent targets A1.
- [ ] ac-HubRequestDoesNotCommitDraft: Given B has a draft and desired head A1, when B polls, then outcome=waiting_local_work, B/main=C0 and the draft value is unchanged.
- [ ] ac-OfflineCommitDiscoveredAfterRestart: Given A1 committed while disconnected and a crash before intent persistence, when A restarts, then history scanning rediscovers A1 exactly once and publishes it after reconnect.

## TR-MCP-FEDERATION-003: logical project enrollment and initial clone contract

Status: pending. Revision audit amended 2026-11-10.
ProjectEnrollment stores ProjectId UUID, NodeId, ownerSubjectId, normalized local path, schemaVersion and subscriptions. POST /qbrainai/federation/enroll accepts an authenticated pre-provisioned node client plus operator-authorized owner binding. Body ProjectId must be allowed by token scope fed.project.{ProjectId}.enroll and durable membership. Paths stay host-local; no default-workspace fallback. A nonempty clone destination returns409 target_not_empty.

Server: project discovery, scoped enrollment and structured refusal diagnostics. Plugin: project discovery/selection, enrollment progress and explicit actionable diagnostics.

**Acceptance Criteria:**
- [ ] ac-DifferentHostPathsSameProject: Given LAB path /home/sharpninja/github/McpServer and LEGION path F:/github/McpServer, when both enroll as ProjectId Atlas, then both reference Atlas and retain their distinct local paths.
- [ ] ac-CloneRefusesNonemptyTarget: Given target has TODO X, when clone is requested, then409 target_not_empty leaves X and all target files unchanged.
- [ ] ac-UnauthorizedProjectDenied: Given node A has only Atlas membership, when it enrolls or clones SecretProject, then403 unauthorized_project returns no rows, objects or project existence details.
- [ ] ac-PluginEnrollmentDiagnostics: Given Atlas is discoverable but A has a nonempty destination, when each plugin selects Atlas and attempts enrollment/clone, then it displays ProjectId, selected branch and409 target_not_empty with remediation; no database is overwritten and no different project is selected.

## TR-MCP-FEDERATION-004: branches across server and plugin context contract

Status: pending. Revision audit amended 2026-11-10.
Branch APIs create/list/select/merge using ProjectId, branch and sessionId. Each pooled connection is opened for an explicit branch and verified before use. Merge accepts expectedSourceHead and expectedTargetHead; stale values yield409 head_changed. Reserved refs upload/, integration/ and review/ are inaccessible to ordinary editing credentials. Branch deletion fails409 branch_busy while leases, reviews or recovery pins reference it.

**Acceptance Criteria:**
- [ ] ac-PooledConnectionDoesNotLeakBranch: Given main T1.title=main and feature/search T1.title=feature, when alternating pooled reads run100 times, then each returns only its selected value.
- [ ] ac-FeaturePublicationLeavesMainUnchanged: Given main=C0 and feature/search=F1, when F1 publishes, then remote feature=F1 and all main heads remain C0.
- [ ] ac-BranchMergeRequiresExpectedHeads: Given a preview for main=C0 and feature=F1 but feature advances to F2, when merge submits the old preview, then409 head_changed creates no commit and changes no ref.

## TR-MCP-FEDERATION-005: bidirectional committed-history exchange contract

Status: pending. Revision audit amended 2026-11-10.
Node upload authority is limited to its candidate namespace in an isolated upload repository. POST /qbrainai/federation/publication/accept accepts projectId, branch, requestId, candidateHead, expectedPublishedHead and hubEpoch. Gateway validates authorization, ancestry, schema and domain invariants before the sole fenced publisher advances the public ref without force. Token validation occurs at the gateway; native Dolt remotesapi is loopback-only and never trusts a node bearer token directly. Transport/procedure enforcement is a Ph0 proof gate.

**Acceptance Criteria:**
- [ ] ac-ThreeWriterCleanConvergence: Given C0 contains T1.title=old,T1.description=old,T2.priority=low, when A1 changes title=A, B1 changes description=B and H1 changes T2.priority=high, then merges M1(A1,B1), M2(M1,H1) converge all heads to M2 with values A,B,high and all three commits in ancestry.
- [ ] ac-HubDraftDoesNotBlockRemote: Given H has an uncommitted title draft at C0, when A publishes A1, then remote=A1, H remains C0 with its draft, and B can accept A1.
- [ ] ac-ConcurrentPublishPreservesBothHistories: Given A1 and B1 descend from C0, when both accept requests expect C0, then one succeeds and the other receives409 head_changed; after merge M1 publishes, both A1 and B1 remain ancestors.

## TR-MCP-FEDERATION-006: hub control table and polling contract

Status: pending. Revision audit amended 2026-11-10.
SyncSubscription key=(NodeId,ProjectId,Branch) contains HubEpoch UUID, PublishedHead, PublishedGeneration int64, DesiredGeneration int64, ReportedGeneration int64, WorkerEpoch int64, AcceptedHead, Outcome and timestamps. POST control/poll is read-only. POST control/report includes all epochs and observed generation; stale worker=409 stale_worker_epoch, stale hub=409 hub_epoch_changed. Poll every30s with20% jitter; commit/reconnect/on-demand wake immediately. Backoff1s doubling to60s. Generation ordering is valid only within HubEpoch. Restore generates a new non-reused random epoch before listeners reopen.

**Acceptance Criteria:**
- [ ] ac-PollReadDoesNotAcknowledge: Given desired=7 and reported=6, when poll runs twice, then both responses request7 and stored reported remains6.
- [ ] ac-AckOldGenerationPreservesNewRequest: Given desired becomes8 while report7 is in flight, when report7 arrives, then reported=7, desired=8 and the subscription remains pending.
- [ ] ac-MissedNotificationReconciles: Given remote=A1 but control still names C0 after a crash, when reconciliation runs, then it records A1 and advances desired generation without producing another commit.
- [ ] ac-HubRestoreFromOlderBackup: Given a node remembers epoch E1 generation90 and the hub restores a backup at generation40, when the hub reopens, then epoch E2 differs from E1; E1 reports fail409 hub_epoch_changed and no head is force-reset. Node A1 is retained until verified ancestry recovery republishes missing history.

## TR-MCP-FEDERATION-007: reviewed conflicts and valid merges contract

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

## TR-MCP-FEDERATION-008: recovery without lost or repeated work contract

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

## TR-MCP-FEDERATION-009: collision-resistant and preserved identity contract

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

## TR-MCP-FEDERATION-010: first-party plugin federation contract contract

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

## TR-MCP-FEDERATION-011: portable data versus host-local state contract

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

## TR-MCP-FEDERATION-012: scoped authorization and offline behavior contract

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

## TR-MCP-FEDERATION-013: Dolt fourth-provider support and schema migration contract

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

## TR-MCP-FEDERATION-014: original-provider-to-Dolt migration tool contract

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

## TR-MCP-FEDERATION-015: imported state does not repeat external effects contract

Status: pending. Revision audit amended 2026-11-10.
History import and audit ingestion apply validated state, never dispatch original domain commands or external effects. EffectIdentity is stable across retries/import. Imported operational metadata is inert evidence, not an execution request. General federation apply/control is outside QuadBrain keyserver transaction gating per FR-MCP-173; actual QuadBrain operations retain their separate coordinator contract. Legacy HMAC envelopes remain isolated compatibility behavior.

Server: synchronization applies data without executing external operations. Plugin: distinguish history/data synchronization from separately authorized external effects in preview, status and receipts.

**Acceptance Criteria:**
- [ ] ac-ImportDoesNotSendExternalAction: Given A1 records an already-sent GitHub update, when B integrates A1, then B state includes the result and outbound GitHub/email invocation count is0.
- [ ] ac-RetryDoesNotRepeatEffect: Given accepted commit A1 and effect E1, when sync retries10 times, then the effect ledger still has one E1 and no new external dispatch occurs.
- [ ] ac-ImportedOperationalRowsRemainInert: Given audit backfill contains command names and receipt payloads, when the hub ingests them, then they are stored as evidence and no business command executes.
- [ ] ac-FederationBypassesQuadBrainKeyserver: Given QuadBrain coordinator is degraded but local authorization passes, when federation commit/control/audit operations execute, then none invokes the keyserver and valid local work succeeds.
- [ ] ac-PluginEffectBoundary: Given incoming A1 contains a TODO completed state and inert historical external-action metadata, when the plugin shows synchronization success, then it labels data received with zero external operations executed; requesting an external operation requires a separate explicit authorized command and its own receipt.

## TR-MCP-FEDERATION-016: safe shared-host editing and commit ownership contract

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

## TR-MCP-TXNCOMPAT-001: Federation and QuadBrain trust separation

Status: pending

FR-MCP-173 supersedes the older requirement that all federation mutations invoke the coordinator. Keep HMAC legacy transport compatibility distinct from IdentityServer network authentication, versioned history and QuadBrain transaction signing. Do not change the live TurnTransactions flag to implement this separation.

- [ ] review20261110-1: Given federation apply/control operations and RequiredForMutations=true, when executed, then ITurnTransactionCoordinator invocation count is0; brain-slot.invoke remains gated.

