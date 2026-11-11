# Current implementation contracts and acceptance

This is the current specification for the new federation release. Each technical contract and acceptance criterion appears once here, with its canonical IDs. These are planned acceptance obligations, not claims of implementation or passing tests. C0/A1/B1/H1/M1/M2/S1 refer to exact hashes captured in the fixture manifest. Historical source versions and superseded behavior are excluded.

## 1. autonomous local execution

FR-MCP-174 / TR-MCP-FEDERATION-001 / TEST-MCP-222. Phase: Ph2. Workflows: U02, U03, U06.

Every node, including the hub, performs authorized local business operations and persists results without hub connectivity.

Local request routing binds immutable ProjectId, NodeId and Branch to one DbContext and branch write gate. Dolt-mode domain requests never invoke LocalProxy forwarding or wait for the hub. Existing local authentication remains mandatory; federation reference-token introspection is not a dependency of local work. Result fields distinguish local persistence, version commit and publication.

- **NodeWorksWithHubUnavailable** (`ac-NodeWorksWithHubUnavailable`): Given A/main=C0 and the hub link is down, when A changes TODO T1.title from old to local, then the local API succeeds, a local reread returns local, and no hub request or history commit occurs.
- **HubUsesSameLocalExecution** (`ac-HubUsesSameLocalExecution`): Given H/main=C0 and the gateway is stopped, when H edits T1.title, then H persists it locally while remote/main remains C0.
- **OfflineLocalReadAfterWrite** (`ac-OfflineLocalReadAfterWrite`): Given the prior offline edit, when the application and Dolt restart, then T1.title remains local and main remains C0 until an explicit version commit.

## 2. explicit version commits and publication boundary

FR-MCP-175 / TR-MCP-FEDERATION-002 / TEST-MCP-223. Phase: Ph4. Workflows: U02, U07, U09.

Only an explicit version-history commit triggers publication; drafts and SQL saves remain local. Commit retries recover the same history identity.

POST /qbrainai/federation/projects/{projectId}/branches/{branch}/commits accepts requestId UUID, expectedHead, expectedWorkingSet, leaseFence, author and message. Success is 201 with commitHash and publicationState=committed_locally. Persist requestId in the Dolt commit trailer. Retry returns the original hash. Head/fingerprint/fence mismatch is HTTP409 with head_changed/working_set_changed/lease_lost. SaveChanges and control.poll never create version commits.

- **SaveDoesNotPublish** (`ac-SaveDoesNotPublish`): Given main=C0 and remote=C0, when SaveChanges persists T1.title=draft, then main and remote stay C0 and no publication intent is emitted.
- **VersionCommitTriggersSync** (`ac-VersionCommitTriggersSync`): Given that draft and a valid preview, when commit request R1 succeeds, then main=A1, the reply says committed_locally, and one recoverable publication intent targets A1.
- **HubRequestDoesNotCommitDraft** (`ac-HubRequestDoesNotCommitDraft`): Given B has a draft and desired head A1, when B polls, then outcome=waiting_local_work, B/main=C0 and the draft value is unchanged.
- **OfflineCommitDiscoveredAfterRestart** (`ac-OfflineCommitDiscoveredAfterRestart`): Given A1 committed while disconnected and a crash before intent persistence, when A restarts, then history scanning rediscovers A1 exactly once and publishes it after reconnect.

## 3. logical project enrollment and initial clone

FR-MCP-176 / TR-MCP-FEDERATION-003 / TEST-MCP-224. Phase: Ph2/Ph5. Workflows: U01.

Nodes enroll a logical project under immutable shared identity while retaining host-local paths and refusing destructive clones.

ProjectEnrollment stores ProjectId UUID, NodeId, ownerSubjectId, normalized local path, schemaVersion and subscriptions. POST /qbrainai/federation/enroll accepts an authenticated pre-provisioned node client plus operator-authorized owner binding. Body ProjectId must be allowed by token scope fed.project.{ProjectId}.enroll and durable membership. Paths stay host-local; no default-workspace fallback. A nonempty clone destination returns409 target_not_empty.

- **DifferentHostPathsSameProject** (`ac-DifferentHostPathsSameProject`): Given LAB path /home/sharpninja/github/McpServer and LEGION path F:/github/McpServer, when both enroll as ProjectId Atlas, then both reference Atlas and retain their distinct local paths.
- **CloneRefusesNonemptyTarget** (`ac-CloneRefusesNonemptyTarget`): Given target has TODO X, when clone is requested, then409 target_not_empty leaves X and all target files unchanged.
- **UnauthorizedProjectDenied** (`ac-UnauthorizedProjectDenied`): Given node A has only Atlas membership, when it enrolls or clones SecretProject, then403 unauthorized_project returns no rows, objects or project existence details.

## 4. branches across server and plugin context

FR-MCP-177 / TR-MCP-FEDERATION-004 / TEST-MCP-225. Phase: Ph2/Ph4/Ph6. Workflows: U08, U12.

First-release project branches support isolated work, explicit sharing and reviewed merge; session selection never leaks between connections.

Branch APIs create/list/select/merge using ProjectId, branch and sessionId. Each pooled connection is opened for an explicit branch and verified before use. Merge accepts expectedSourceHead and expectedTargetHead; stale values yield409 head_changed. Reserved refs upload/, integration/ and review/ are inaccessible to ordinary editing credentials. Branch deletion fails409 branch_busy while leases, reviews or recovery pins reference it.

- **PooledConnectionDoesNotLeakBranch** (`ac-PooledConnectionDoesNotLeakBranch`): Given main T1.title=main and feature/search T1.title=feature, when alternating pooled reads run100 times, then each returns only its selected value.
- **FeaturePublicationLeavesMainUnchanged** (`ac-FeaturePublicationLeavesMainUnchanged`): Given main=C0 and feature/search=F1, when F1 publishes, then remote feature=F1 and all main heads remain C0.
- **BranchMergeRequiresExpectedHeads** (`ac-BranchMergeRequiresExpectedHeads`): Given a preview for main=C0 and feature=F1 but feature advances to F2, when merge submits the old preview, then409 head_changed creates no commit and changes no ref.

## 5. bidirectional committed-history exchange

FR-MCP-178 / TR-MCP-FEDERATION-005 / TEST-MCP-226. Phase: Ph5/Ph6. Workflows: U03, U04, U08.

Exchange committed history bidirectionally through guarded publication, preserving every accepted ancestor and the hub independent working copy.

Node upload authority is limited to its candidate namespace in an isolated upload repository. POST /qbrainai/federation/publication/accept accepts projectId, branch, requestId, candidateHead, expectedPublishedHead and hubEpoch. Gateway validates authorization, ancestry, schema and domain invariants before the sole fenced publisher advances the public ref without force. Token validation occurs at the gateway; native Dolt remotesapi is loopback-only and never trusts a node bearer token directly. Transport/procedure enforcement is a Ph0 proof gate.

- **ThreeWriterCleanConvergence** (`ac-ThreeWriterCleanConvergence`): Given C0 contains T1.title=old,T1.description=old,T2.priority=low, when A1 changes title=A, B1 changes description=B and H1 changes T2.priority=high, then merges M1(A1,B1), M2(M1,H1) converge all heads to M2 with values A,B,high and all three commits in ancestry.
- **HubDraftDoesNotBlockRemote** (`ac-HubDraftDoesNotBlockRemote`): Given H has an uncommitted title draft at C0, when A publishes A1, then remote=A1, H remains C0 with its draft, and B can accept A1.
- **ConcurrentPublishPreservesBothHistories** (`ac-ConcurrentPublishPreservesBothHistories`): Given A1 and B1 descend from C0, when both accept requests expect C0, then one succeeds and the other receives409 head_changed; after merge M1 publishes, both A1 and B1 remain ancestors.

## 6. hub control table and polling

FR-MCP-179 / TR-MCP-FEDERATION-006 / TEST-MCP-227. Phase: Ph5. Workflows: U02, U06, U09, U15.

Use a durable hub subscription table with polling, generation and worker fencing, plus a fresh hub epoch after restore.

SyncSubscription key=(NodeId,ProjectId,Branch) contains HubEpoch UUID, PublishedHead, PublishedGeneration int64, DesiredGeneration int64, ReportedGeneration int64, WorkerEpoch int64, AcceptedHead, Outcome and timestamps. POST control/poll is read-only. POST control/report includes all epochs and observed generation; stale worker=409 stale_worker_epoch, stale hub=409 hub_epoch_changed. Poll every30s with20% jitter; commit/reconnect/on-demand wake immediately. Backoff1s doubling to60s. Generation ordering is valid only within HubEpoch. Restore generates a new non-reused random epoch before listeners reopen.

- **PollReadDoesNotAcknowledge** (`ac-PollReadDoesNotAcknowledge`): Given desired=7 and reported=6, when poll runs twice, then both responses request7 and stored reported remains6.
- **AckOldGenerationPreservesNewRequest** (`ac-AckOldGenerationPreservesNewRequest`): Given desired becomes8 while report7 is in flight, when report7 arrives, then reported=7, desired=8 and the subscription remains pending.
- **MissedNotificationReconciles** (`ac-MissedNotificationReconciles`): Given remote=A1 but control still names C0 after a crash, when reconciliation runs, then it records A1 and advances desired generation without producing another commit.
- **HubRestoreFromOlderBackup** (`ac-HubRestoreFromOlderBackup`): Given a node remembers epoch E1 generation90 and the hub restores a backup at generation40, when the hub reopens, then epoch E2 differs from E1; E1 reports fail409 hub_epoch_changed and no head is force-reset. Node A1 is retained until verified ancestry recovery republishes missing history.

## 7. reviewed conflicts and valid merges

FR-MCP-180 / TR-MCP-FEDERATION-007 / TEST-MCP-228. Phase: Ph6/Ph7. Workflows: U05, U07, U08.

Conflicts and invalid merges preserve valid live data, require authorized review and remain visible with ageing while unrelated publication continues.

Review record key=ReviewId UUID; fields ProjectId,Branch,baseHeads[],localHead,incomingHead,candidateRef,conflictingCells[],validationErrors[],createdUtc,lastNotifiedUtc,resolution,actor. GET conflicts and status expose it. Resolve binds exact heads and returns409 stale_review if changed. Safe TODO policy: identical concurrent values collapse; edits to different cells may merge only if invariants pass; duplicate immutable child IDs deduplicate only when bytes agree. Status, priority, title, description, dependency deletions and same-cell disagreements always require review; no timestamp last-writer-wins. Notify immediately in status and durable operator inbox; warn at15min, escalate at24h, repeat at most hourly; no email/Slack sent implicitly. Same-branch dependent commits wait; unrelated branches/projects continue.

- **SameCellConflictNeedsReview** (`ac-SameCellConflictNeedsReview`): Given base priority=low, A1 priority=high and B1 priority=medium, when B integrates A1, then needs_review shows low/medium/high, B/main remains B1 and no public ref is overwritten.
- **DeleteUpdateNeedsReview** (`ac-DeleteUpdateNeedsReview`): Given A1 deletes T1 and B1 edits T1.title, when histories merge, then needs_review retains B1 and the deleted/edited alternatives for review.
- **CleanMergeCanFailDomainValidation** (`ac-CleanMergeCanFailDomainValidation`): Given separate commits add T1 dependsOn T2 and T2 dependsOn T1, when Dolt merges without cell conflicts, then domain validation records dependency_cycle and leaves the prior valid live head unchanged.
- **StaleReviewRejected** (`ac-StaleReviewRejected`): Given a resolution preview binds local=B1 and incoming=A1, when incoming advances to A2, then resolve returns409 stale_review with no merge commit.
- **ReviewAgeVisible** (`ac-ReviewAgeVisible`): Given unresolved needs_review at12:00, when the clock reaches12:15, then plugins/status and the operator inbox show warning with ReviewId and age900s; at24h severity is critical, with at most one repeat per hour.
- **UnrelatedPublicationContinues** (`ac-UnrelatedPublicationContinues`): Given Atlas/main is blocked by review R1, when Atlas/feature publishes F2 and another project publishes X1, then both succeed while Atlas/main remains unchanged.
- **SafeTodoMergeRules** (`ac-SafeTodoMergeRules`): Given A changes title and B changes description with no domain violation, when merge runs, then both values survive; competing priority or completion edits produce needs_review instead of choosing a timestamp winner.

## 8. recovery without lost or repeated work

FR-MCP-181 / TR-MCP-FEDERATION-008 / TEST-MCP-229. Phase: Ph5/Ph6. Workflows: U06, U07, U09, U15.

Recover synchronization, receipts and restored hub state without losing local work, inventing success or replaying business effects.

OperationReceipt binds requestId, node, project, branch, hubEpoch, candidate/result hashes and outcome. Durable refs pin candidates/reviews before receipt completion. Worker claims are fenced by WorkerEpoch; reconnect never trusts stored generation across HubEpoch. Fetch may advance remote-tracking refs, never dirty live refs. Restore first disables ingress, restores compatible data/control/audit checkpoints, creates new epoch, then re-advertises verified heads; node-held missing commits are uploaded and ancestry-validated before normal synchronization resumes.

- **LostPushAckDoesNotDuplicateCommit** (`ac-LostPushAckDoesNotDuplicateCommit`): Given remote accepted A1 and reply was lost, when request R1 retries, then it returns A1 and the existing receipt; no A2 commit is created.
- **OldWorkerEpochCannotReportSuccess** (`ac-OldWorkerEpochCannotReportSuccess`): Given worker epoch changes4 to5, when epoch4 reports success, then409 stale_worker_epoch leaves current receipt and desired state unchanged.
- **DirtyBranchDefersApply** (`ac-DirtyBranchDefersApply`): Given B/main=C0 with title=draft, when A1 is fetched, then outcome=waiting_local_work, main stays C0 and title remains draft; at15min status/inbox shows the age warning.
- **IncompatibleSchemaDoesNotApply** (`ac-IncompatibleSchemaDoesNotApply`): Given B supports schema1 and incoming S1 requires schema2, when sync runs, then409 incompatible_schema leaves B's live data/head unchanged and records the required version.

## 9. collision-resistant and preserved identity

FR-MCP-182 / TR-MCP-FEDERATION-009 / TEST-MCP-230. Phase: Ph2/Ph3/Ph5/Ph6. Workflows: U01, U10, U14.

Prevent generated-ID collisions, preserve permanent legacy aliases, detect duplicate work and atomically reserve human-assigned IDs.

Generated IDs use immutable two-character creator tags and durable branch-independent sequences with per-family grammar fixed in Ph0. Alias key=(ProjectId,OriginNodeId,EntityKind,LegacyId) maps permanently to CanonicalId; ambiguous unqualified lookup returns409 ambiguous_legacy_id. Historical text/commits are not rewritten. Migration duplicate candidates use normalized title/description/dependency features, returning similarity score plus evidence, never auto-merging. Human ID reservations are unique(ProjectId,EntityKind,HumanId) at the hub, bound to node/requestId. Offline nodes use generated provisional IDs; an unreserved human ID is not publishable.

- **TwoNodesCreateDistinctIds** (`ac-TwoNodesCreateDistinctIds`): Given tags LA and LG and both counters start1, when both create a TODO, then generated IDs differ and both records survive reconciliation.
- **BranchRevertDoesNotReuseSequence** (`ac-BranchRevertDoesNotReuseSequence`): Given LA allocated sequence42, when main reverts and another branch creates a record, then its sequence is greater than42.
- **LegacyCollisionPreservesReferences** (`ac-LegacyCollisionPreservesReferences`): Given LAB and LEGION have different entities named TODO-7, when migration resolves the collision, then scoped aliases resolve each original identity and every relational reference follows its reviewed canonical mapping; historical text remains byte-identical.
- **LegacyIdsRemainValid** (`ac-LegacyIdsRemainValid`): Given existing IMPL-UNITGREEN-001 and old TODO references, when imported and read by their original IDs, then they resolve through permanent aliases without rewriting commits, PR text, session logs, memories or documents.
- **DifferentIdsSameTodoDetected** (`ac-DifferentIdsSameTodoDetected`): Given LAB TODO-7 and LEGION TODO-91 share normalized title Fix login, description and dependency set, when migration plan runs, then it emits a duplicate candidate with both IDs and evidence; until explicit review, neither is merged or deleted.
- **SimultaneousHumanIdReservation** (`ac-SimultaneousHumanIdReservation`): Given human ID IMPL-UNITGREEN-001 is unreserved, when two nodes reserve it concurrently, then exactly one receives201 and the other409 human_id_reserved; retry by the winning requestId returns the same reservation.
- **OfflineHumanIdPublicationDenied** (`ac-OfflineHumanIdPublicationDenied`): Given an offline-created provisional TODO requesting an unreserved human ID, when it reconnects and publishes, then409 human_id_unreserved retains the local record until reservation or reviewed alias resolution.

## 10. first-party plugin federation contract

FR-MCP-183 / TR-MCP-FEDERATION-010 / TEST-MCP-231. Phase: Ph7. Workflows: U01, U02, U05, U07, U08, U11, U12, U13, U14, U15.

Expose consistent typed federation, review, identity, audit, migration and maintenance workflows through every first-party plugin.

All plugins call typed server/client/MCP/REPL contracts; no raw SQL/CLI fallback. Status DTO includes projectId, branch, localHead, publishedHead, observedUtc, dirty, ahead, behind, publicationState, incomingTarget, connectivity, reviewCount, oldestReviewAgeSeconds, waitingLocalWorkAgeSeconds, auditPendingCount, hubEpoch, workerEpoch. Capability contract version1 is negotiated; unknown version=409 unsupported_contract. Administrative maintenance.preflight/attest/status and migration commands share the same typed service as Nuke. Stable error envelope={code, message, requestId, projectId, branch, retryable, details}; status codes are locked in the technical contract appendix.

Technical contract appendix (TR-MCP-FEDERATION-010): all below routes are under /qbrainai/federation unless the commits route specifies the project/branch path. Every mutation requires requestId UUID and authenticated subject; node network requests additionally carry the project-scoped reference token. Responses carry contractVersion=1 and the stable error envelope. Path/body ProjectId mismatch is403 unauthorized_project. No endpoint accepts credentials in URL, argv or persisted receipts.

Read/status routes: GET capabilities returns200 {contractVersion,capabilities}; GET projects/{projectId}/status returns200 the status DTO; GET projects/{projectId}/branches returns200 {items:[{name,head,dirty,publishedHead}]}; GET conflicts and GET conflicts/{reviewId} return200 review records filtered by membership. POST control/poll returns200 {hubEpoch,subscriptions}; reads never acknowledge work. POST control/report validates observed generation and worker epoch, returns200 the durable receipt or409 the named fence error. Poll/report use control scope; reads use read scope.

Mutation routes: POST enroll returns201 the enrollment (or200 the same idempotent enrollment); POST projects/{projectId}/branches creates201 {branch,head}; POST projects/{projectId}/branches/select returns200 {sessionId,branch,head} after local lease validation; POST projects/{projectId}/branches/{branch}/commits returns201 {commitHash,publicationState}, or200 the same commit on retry. POST publication/accept returns200 {requestId,hubEpoch,publishedHead,publishedGeneration,receiptId} only after the guarded ref update is durable. POST branches/merge and POST conflicts/{reviewId}/resolve bind expected heads and return201 the new commit; a conflict returns202 {outcome:needs_review,reviewId} while the live head stays unchanged. Share/merge/publication require publish scope plus branch permission. Enrollment requires enroll scope and the operator owner-binding approval.

POST audit/batches accepts {requestId,projectId,records:[AuditRecord]}, returns200 {receipts:[{originNodeId,recordId,payloadHash,outcome,receivedUtc}]}, and requires audit scope. Every record must match the token/project and batch ProjectId. Any invalid record rejects the whole batch before insert. Exact duplicate is already_received; payload mismatch is409 audit_id_conflict. POST ids/reservations accepts {requestId,projectId,entityKind,humanId,nodeId}, requires publish scope and returns201 the reservation or200 its exact retry; another owner gets409 human_id_reserved. POST maintenance/preflight and POST maintenance/attest, GET maintenance/status and POST migration/{plan|dry-run|run|resume|verify|export-todos|cutover|abort}, GET migration/status are operator-only local service operations. They are exposed through typed clients and Nuke, never public node-admin capabilities. A long migration returns202 {migrationId,status}; status/verify return200 a receipt, and a denied readiness check never performs cutover.

Stable failure mapping:401 invalid_token;403 unauthorized_project;409 head_changed, working_set_changed, lease_lost, branch_busy, stale_review, stale_worker_epoch, hub_epoch_changed, ambiguous_legacy_id, human_id_reserved, human_id_unreserved, target_not_empty, maintenance_not_ready, migration_source_changed, migration_verification_failed, incompatible_schema, unsupported_contract and publication_required;422 secret_detected, dependency_cycle and unsupported_provider;501 unsupported_capability;503 identity_unavailable or remote_unavailable. TLS rejection produces the local transport code tls_peer_untrusted with no HTTP response and no request sent. Missing required fields return400 invalid_request. Missing authorized resources return404 not_found; project authorization precedes existence disclosure. Malformed/unsupported token is401; valid token lacking project rights is403.

Outcome fields are not HTTP errors: committed_locally, already_received, waiting_local_work and needs_review remain visible through200 status/report responses. A requested live mutation blocked by drafts returns409 waiting_local_work; an integration that durably creates review returns202 needs_review. dependency_cycle is a validationErrors entry on that review as well as422 for a directly invalid local mutation. retryable=true is restricted to transient503/transport failures; all stale-head/fence/review errors require reread and a fresh validated action. Idempotent retries with the same requestId but different payload return409 request_id_conflict. No automatic retry creates a different commit.

Test fixtures capture actual content hashes as symbols C0,A1,B1,H1,M1,M2,S1 rather than hardcoding unstable engine hashes. Each symbol is immutable within the run manifest. Given/When/Then compares exact hash equality, specified parent sets, row values, status codes and error codes against that manifest; a successful result must never be inferred solely from an empty queue. Capture UTC timestamps and a fake clock for exact15-minute/24-hour age boundaries. All plan phase IDs use Ph0-Ph8; priority values remain separate.

- **EveryPluginUsesSameContract** (`ac-EveryPluginUsesSameContract`): Given one fixture for commit R1, conflict R2 and migration R3, when each installed first-party plugin calls its wrapper, then normalized IDs, heads, error codes and audit agent identity match the typed client result.
- **OldServerReportsUnsupportedCapability** (`ac-OldServerReportsUnsupportedCapability`): Given server lacks federation contract1, when a plugin invokes commit, then unsupported_contract is shown and no raw SQL or shell fallback executes.
- **ReconnectRevalidatesContext** (`ac-ReconnectRevalidatesContext`): Given a cached leaseFence4 and session branch feature, when reconnect discovers fence5, then the plugin retains visible context but refuses writes with lease_lost until reacquired.
- **StaleBranchReportedNotDeleted** (`ac-StaleBranchReportedNotDeleted`): Given a branch observation is older than24h, when status is displayed, then stale=true and observedUtc are visible; the branch, drafts and refs are not deleted.

## 11. portable data versus host-local state

FR-MCP-184 / TR-MCP-FEDERATION-011 / TEST-MCP-232. Phase: Ph2/Ph3/Ph5/Ph7. Workflows: U10, U13.

Separate versioned project content from operational state while delivering permanent audit to the hub independently of commits and excluding secrets.

Project history excludes secrets and automatic operational audit. This does NOT make audit local-only: every host retains permanent local audit and an append-only outbox delivers sanitized records to the hub unversioned ledger independently of project commits. AuditRecord key=(OriginNodeId,RecordId UUID); fields ProjectId,EntityKind,EntityId,OriginSequence int64,OccurredUtc,Actor,Action,PayloadVersion,PayloadHash,Payload. POST audit/batches validates token/project, inserts atomically by key and returns per-record durable receipts. Same key/hash is already_received; same key/different hash=409 audit_id_conflict. No UPDATE/DELETE permission. Local pruning is forbidden. Migration backfills TODO and generic audit history with counts/hashes and receipts. Credential values are excluded at capture, not silently replicated.

- **CloneOmitsSecrets** (`ac-CloneOmitsSecrets`): Given credentials and local machine paths exist in operational storage, when a project is cloned, then none appears in its versioned tables or history.
- **AuditReachesHub** (`ac-AuditReachesHub`): Given A performs an offline TODO edit and creates audit E1, when connectivity returns without a Dolt commit, then hub audit contains exactly E1, A retains E1, and main/remote remain C0.
- **AuditRetryIdempotent** (`ac-AuditRetryIdempotent`): Given E1 was stored but its receipt was lost, when the same batch retries, then exactly one E1 exists and the same hash receipt returns; changed payload under E1 returns409 audit_id_conflict.
- **AuditBackfillVerified** (`ac-AuditBackfillVerified`): Given source has100 TODO audit and20 generic audit rows, when seed/join audit backfill verifies, then all120 records have matching payload hashes and hub receipts; missing one blocks cutover.
- **FullProjectSessionGraphPreserved** (`ac-FullProjectSessionGraphPreserved`): Given an explicitly promoted sanitized session with turns, actions, dialog, commits/files, PlanFile and TodoId, when cloned, then every promoted child and relationship matches the source snapshot; automatic capture still uses the audit channel.
- **AcceptedHeadRebuildsBranchIndex** (`ac-AcceptedHeadRebuildsBranchIndex`): Given accepted head changes C0 to A1, when the derived index rebuild completes, then queries read A1 values and no C0 cache is served under the A1 key.
- **BackgroundWritersDoNotDirtyVersionedBranch** (`ac-BackgroundWritersDoNotDirtyVersionedBranch`): Given main=C0 is clean, when heartbeat, audit and automatic session capture run, then Dolt working-set fingerprint remains unchanged.
- **SecondAgentLoggingNeedsNoLease** (`ac-SecondAgentLoggingNeedsNoLease`): Given agent1 owns the editing lease, when agent2 logs a session action, then its local audit persists without acquiring or changing agent1's lease.
- **CommitPreviewBlocksSecret** (`ac-CommitPreviewBlocksSecret`): Given a promoted snapshot contains a fixture secret detected by the configured detector, when commit preview runs, then422 secret_detected creates no commit and outputs no secret value.
- **ReceiptsAndLogsContainNoCredentials** (`ac-ReceiptsAndLogsContainNoCredentials`): Given unique canary values are injected through protected secret references, when commands, retries and failures execute on both hosts, then scanning argv, receipts, logs, chat/audit payloads and generated files finds zero canary values.

## 12. scoped authorization and offline behavior

FR-MCP-185 / TR-MCP-FEDERATION-012 / TEST-MCP-233. Phase: Ph0/Ph2/Ph5. Workflows: U01, U06, U13.

Reuse integrated IdentityServer for single-user node ownership, per-node client credentials, project scopes and revocation; preserve authorized offline work.

Reuse existing Duende IdentityServer. Provision client fed.node.{NodeId} per single-user node, bound to ownerSubjectId through authenticated operator enrollment. Client credentials issue short-lived reference access tokens; audience qbrain-federation and scopes fed.project.{ProjectId}.{read|publish|control|audit|enroll}; protected actions additionally require server membership/branch permission. Gateway introspects without positive caching for each new request and rechecks before publication commit; unavailable identity returns503 identity_unavailable. Revoke through IdentityServer: disable client and remove its persisted reference grants. Gateway rejects inactive tokens401 invalid_token; do not invent a revocation ledger. Native remotesapi uses gateway-held credentials over loopback; clients cannot access it directly. Ph0 proves token injection/relay including redirects/object URLs, project isolation and no public-ref bypass.

- **RawRemoteCannotBypassPublicationPolicy** (`ac-RawRemoteCannotBypassPublicationPolicy`): Given valid A read credentials, when A attempts direct backend access or public-main push, then the backend is unreachable across the link or403 publication_required; remote/main remains C0.
- **ForeignProjectCloneDenied** (`ac-ForeignProjectCloneDenied`): Given token has Atlas.read but no SecretProject scope, when a remote fetch requests SecretProject, then403 unauthorized_project sends zero objects.
- **RevokedNodeCannotPublish** (`ac-RevokedNodeCannotPublish`): Given A holds a reference token, when IdentityServer disables A and revokes its grants before acceptance, then accept returns401 invalid_token and remote/main is unchanged.
- **OfflineLocalAuthorizationStillApplies** (`ac-OfflineLocalAuthorizationStillApplies`): Given hub/IdentityServer is unreachable and a valid authorized local user, when a local TODO edit is requested, then it succeeds; an unauthorized local user still receives403.
- **LocalAppUserCannotPush** (`ac-LocalAppUserCannotPush`): Given ordinary editing SQL credentials, when remote administrative procedures or public-ref writes are attempted, then permission is denied and no object publication occurs.
- **RevokedCredentialFailsFetchAndPublish** (`ac-RevokedCredentialFailsFetchAndPublish`): Given IdentityServer revocation completes, when A starts new fetch and publish requests with its old token, then both return401 invalid_token before transfer or ref mutation.
- **SqlPortUnreachableAcrossLink** (`ac-SqlPortUnreachableAcrossLink`): Given both services run, when LAB probes10.42.0.143:3306 and LEGION probes10.42.0.214:3306, then both connections fail; local127.0.0.1:3306 health succeeds.
- **UnknownCertificatePinRejected** (`ac-UnknownCertificatePinRejected`): Given the peer uses an unapproved SPKI or wrong SAN, when control/remote TLS connects, then tls_peer_untrusted occurs before bearer transmission.

## 13. Dolt fourth-provider support and schema migration

FR-MCP-186 / TR-MCP-FEDERATION-013 / TEST-MCP-234. Phase: Ph0/Ph1/Ph6/Ph8. Workflows: U11.

Support Dolt as the fourth provider with dedicated schema migration, controlled schema-history distribution, Windows/Linux lifecycle and original-provider regressions.

Provider key dolt has a dedicated migration assembly; original provider migrations remain unchanged. Versioned project schema migrations are authored only by hub maintenance on maintenance/schema-N and distributed as history. MaintenanceReadiness binds epoch, binary/schema versions, all local branches including unpublished ones, heads, working-set hashes and attestations. Preflight must pass before Nuke swaps binaries; dirty/unattested branch=409 maintenance_not_ready. LAB uses pinned Linux image; LEGION uses checksum-pinned native Windows dolt.exe supervised by its existing Windows service, local paths on F: and exclusive Windows file handle/named mutex ownership. All B1-B12 are evidenced on both OSs.

- **FourProvidersResolve** (`ac-FourProvidersResolve`): Given keys sqlite,postgresql,sqlserver,dolt, when runtime and design-time factories resolve them, then each selects its own strategy/migration history; unknown key returns unsupported_provider.
- **DoltBaselineAndUpgrade** (`ac-DoltBaselineAndUpgrade`): Given empty standalone Dolt then baseline schema1, when baseline and additive schema2 apply, then CRUD succeeds, data remains intact and repeated apply makes no further changes.
- **OriginalProviderMigrationsUnchanged** (`ac-OriginalProviderMigrationsUnchanged`): Given recorded hashes of the three original migration chains, when Dolt support is added, then those hashes match and all three real-provider upgrade fixtures pass.
- **UnsupportedSchemaRejected** (`ac-UnsupportedSchemaRejected`): Given application supports schema1, when opening schema2 for domain writes, then incompatible_schema is returned before mutation.
- **VersionedProjectNotMigratedAtStartup** (`ac-VersionedProjectNotMigratedAtStartup`): Given a versioned project at schema1, when application starts, then no EF migration executes and its Dolt head/schema remain unchanged.
- **OldSchemaUnpublishedCommitMergesAfterUpgrade** (`ac-OldSchemaUnpublishedCommitMergesAfterUpgrade`): Given B1 is unpublished on schema1 and hub S1 adds schema2 from C0, when preflight/upgrade and merge succeed, then B/main=M1 with both B1 and S1 ancestors and B1 row values preserved.
- **FeatureBranchSchemaCatchUp** (`ac-FeatureBranchSchemaCatchUp`): Given main accepts S1 but feature still uses schema1, when feature is selected for writes, then it remains closed until its own merge with S1 validates.
- **DirtyDraftBlocksBinaryUpgrade** (`ac-DirtyDraftBlocksBinaryUpgrade`): Given any local branch has a draft, when Nuke preflight runs, then409 maintenance_not_ready prevents binary replacement and preserves the draft.
- **ContractBlockedUntilLocalBranchesAttested** (`ac-ContractBlockedUntilLocalBranchesAttested`): Given unpublished feature lacks an expand-schema attestation, when contract migration is requested, then it is refused even if all public remote branches are current.
- **DeploymentPreflightBlocksDirtyOrUnattestedBranch** (`ac-DeploymentPreflightBlocksDirtyOrUnattestedBranch`): Given a ready public main but an unattested local feature, when deployment runs, then no binary/service/schema change occurs; explicit branch attestation is required.
- **WindowsWriterLockAndRestart** (`ac-WindowsWriterLockAndRestart`): Given one Windows worker owns Atlas, when a second process starts or the owner crashes, then concurrent ownership is denied and restart recovers only after the OS releases its lock; committed rows and unpublished heads survive.

## 14. original-provider-to-Dolt migration tool

FR-MCP-187 / TR-MCP-FEDERATION-014 / TEST-MCP-235. Phase: Ph3 seed/export; Ph6 join. Workflows: U10, U14.

Migrate all three original providers using verified seed then reviewed join, permanent identities, audit backfill and a source-retaining TODO-to-SQLite recovery export.

MigrationManifest binds MigrationId, mode, ProjectId, source provider/snapshot hash, schema versions, table counts, typed row hashes, alias decisions, audit backfill and immutable destination commit. Ph3 implements seed, dry-run/resume/verify and Dolt-to-SQLite TODO export; join is unavailable until Ph6 review/publication exist. Lab sources are both SQLite; PostgreSQL and SQL Server are supported fixture sources. Retain source SQLite read-only. Export freezes a chosen Dolt head and writes a NEW SQLite recovery DB containing TODO items/tasks/lists/links/document metadata/aliases and audit linkage, with compatible schema and verification receipt; never overwrite retained source. Export is not full-database reverse migration. Cutover uses a verified receipt, explicit DEPLOY HOLD release and one-node soak before the second host.

- **ThreeSourceProviderRoundTrip** (`ac-ThreeSourceProviderRoundTrip`): Given the same fixture in SQLite,PostgreSQL,SQL Server, when seed migration runs, then counts, typed hashes, IDs, nulls, Unicode, decimals, UTC precision and FK relationships match in Dolt.
- **ResumeAfterBatchCommit** (`ac-ResumeAfterBatchCommit`): Given target batch1 committed before a crash but its acknowledgement was lost, when resume runs, then batch1 is recognized by journal and no duplicate rows appear.
- **ChangedSnapshotRejected** (`ac-ChangedSnapshotRejected`): Given manifest source hash S0, when source is replaced by S1 before resume, then409 migration_source_changed writes no additional target rows.
- **FailedVerifyCannotCutover** (`ac-FailedVerifyCannotCutover`): Given one row hash differs, when cutover is requested, then409 migration_verification_failed leaves source routing active and writes closed on the target.
- **SourceRetainedAfterCutover** (`ac-SourceRetainedAfterCutover`): Given verified cutover completes, when target receives a new TODO, then source backup remains present, read-only and byte-identical; the new TODO exists only in Dolt until an explicit recovery export.
- **MigrationJoinPreservesCommonAncestor** (`ac-MigrationJoinPreservesCommonAncestor`): Given seed C0 is published and join imports J1, when join completes in Ph6, then C0 is an ancestor of J1 and reviewed main M1; no second root is created.
- **MigrationJoinDivergentRowRequiresReview** (`ac-MigrationJoinDivergentRowRequiresReview`): Given seed T1.title=A and frozen join T1.title=B, when join planning runs, then an explicit keep/take/manual decision is required before J1 even if Dolt would fast-forward.
- **JoinCutoverRequiresReviewedMain** (`ac-JoinCutoverRequiresReviewedMain`): Given J1 is verified but main integration is unresolved, when cutover runs, then409 migration_verification_failed preserves source routing; only verified reviewed M1 permits cutover.
- **DoltTodoExportPreservesNewWrites** (`ac-DoltTodoExportPreservesNewWrites`): Given A1 adds TODO T2 after cutover, when export-sqlite targets A1, then the new SQLite DB contains T2 and all TODO children/aliases with matching hashes, while the original source remains unchanged.
- **SeedPhaseRejectsJoin** (`ac-SeedPhaseRejectsJoin`): Given only Ph3 is implemented, when mode=join is requested, then409 unsupported_capability creates no target commit or routing change.

## 15. imported state does not repeat external effects

FR-MCP-188 / TR-MCP-FEDERATION-015 / TEST-MCP-236. Phase: Ph2/Ph6. Workflows: U04, U09, U13.

Imported project or audit state never replays external effects and general federation remains outside QuadBrain-only keyserver gating.

History import and audit ingestion apply validated state, never dispatch original domain commands or external effects. EffectIdentity is stable across retries/import. Imported operational metadata is inert evidence, not an execution request. General federation apply/control is outside QuadBrain keyserver transaction gating per FR-MCP-173; actual QuadBrain operations retain their separate coordinator contract. Legacy HMAC envelopes remain isolated compatibility behavior.

- **ImportDoesNotSendExternalAction** (`ac-ImportDoesNotSendExternalAction`): Given A1 records an already-sent GitHub update, when B integrates A1, then B state includes the result and outbound GitHub/email invocation count is0.
- **RetryDoesNotRepeatEffect** (`ac-RetryDoesNotRepeatEffect`): Given accepted commit A1 and effect E1, when sync retries10 times, then the effect ledger still has one E1 and no new external dispatch occurs.
- **ImportedOperationalRowsRemainInert** (`ac-ImportedOperationalRowsRemainInert`): Given audit backfill contains command names and receipt payloads, when the hub ingests them, then they are stored as evidence and no business command executes.
- **FederationBypassesQuadBrainKeyserver** (`ac-FederationBypassesQuadBrainKeyserver`): Given QuadBrain coordinator is degraded but local authorization passes, when federation commit/control/audit operations execute, then none invokes the keyserver and valid local work succeeds.

## 16. safe shared-host editing and commit ownership

FR-MCP-189 / TR-MCP-FEDERATION-016 / TEST-MCP-237. Phase: Ph2/Ph4/Ph6. Workflows: U07, U12.

Enforce branch edit ownership and fenced live-write serialization so no agent or sync worker captures or overwrites another agent draft.

BranchLease key=(ProjectId,Branch), OwnerSessionId, Fence int64, ExpiresUtc and WorkingSetFingerprint. Every live-branch mutation holds the same host-owner write gate across lease validation and transaction/commit. Expiry rejects new admission; an admitted write finishes before takeover. Isolated worker rights cover only non-live integration refs and it invokes owner API for live moves. Dirty takeover needs explicit review. Commit requestId persists in history for idempotency; preview binds exact head/fingerprint.

- **CommitCannotCaptureOtherAgentDraft** (`ac-CommitCannotCaptureOtherAgentDraft`): Given agent1 owns a draft, when agent2 commits the same branch, then409 branch_busy creates no commit and the draft remains agent1-owned.
- **ExpiredLeaseFenced** (`ac-ExpiredLeaseFenced`): Given fence4 expires and fence5 is issued, when a fence4 edit arrives, then409 lease_lost persists no row changes.
- **PreviewChangeRequiresReview** (`ac-PreviewChangeRequiresReview`): Given preview fingerprint W1, when the working set becomes W2, then commit with W1 returns409 working_set_changed and requires a fresh preview.
- **LeaseLostDuringWriteRejected** (`ac-LeaseLostDuringWriteRejected`): Given a write admitted under fence4, when expiry occurs mid-transaction, then that write finishes under the gate before fence5 takeover; a later fence4 write is rejected.
- **CommitRetryReturnsSameHash** (`ac-CommitRetryReturnsSameHash`): Given request R1 created A1 and reply was lost, when R1 retries after restart, then A1 is returned by its commit trailer and history contains no duplicate commit.
- **IncomingMergeCannotRaceAdmittedEdit** (`ac-IncomingMergeCannotRaceAdmittedEdit`): Given an edit holds the branch gate, when the integration worker requests a live head move, then it waits; after the edit finishes, fingerprint revalidation detects the draft and returns waiting_local_work without overwriting it.

## Applicable compatibility and migration acceptance

The following current criteria close requirements recovered in the gap check. They are limited to applicable behavior, source-data preservation and explicit existing-mode regressions. They do not reinstate business-command forwarding or read switching for Dolt projects. A criterion shared by records is printed once with all source IDs.

- **TEST-MCP-FED-ROUTE-001/review20261110-1**: Given A Atlas and B SecretProject, when A supplies B path or hash, then403 unauthorized_project sends no B rows and performs no mutation.
- **FR-MCP-103/review20261110-1**: Given A cannot reach the hub, when it edits and commits T1, then local read returns the change and publication remains pending.
- **FR-MCP-FED-WSSYNC-001/review20261110-1**: Given A changes host RunAs or DataDirectory, when heartbeat/clone runs, then those values never replace H configuration.
- **FR-MCP-MEMORY-008/review20261110-2**: Given A deletes and B edits M1, when merged, then needs_review preserves both histories.
- **TR-MCP-FED-ROUTE-001/review20261110-1**: Given a federation hop with unresolved workspace in legacy mode, when routed, then a4xx diagnostic is returned and no primary workspace fallback occurs.
- **FR-MCP-MEMORY-008/review20261110-3**: Given a global memory containing machine configuration, when cloning a project, then that memory is absent.
- **FR-MCP-FED-ROUTE-001/review20261110-1**: Given A is enrolled in Atlas, when its control request omits ProjectId or requests another project, then400 project_required or403 unauthorized_project returns no default-workspace data.
- **TEST-MCP-136/review20261110-1**: Given a local commit during outage, when reconnect runs, then history is exchanged without replaying the original command.
- **FR-MCP-FED-BACKFILL-001/review20261110-3**: Given a missing source row or audit receipt, when verify runs, then migration_verification_failed blocks cutover.
- **TR-MCP-AUDIT-004/review20261110-3**: Given a wiki export or project clone, when generated, then it excludes credentials and operational audit rows while independent hub delivery remains enabled.
- **FR-MCP-FED-WSSYNC-001/review20261110-3**: Given an authorized portable prompt-template edit is committed, when synced, then peers receive its sanitized value.
- **TR-MCP-AUDIT-004/review20261110-1**: Given batchE1 was accepted and its reply lost, when retried, then one row and the same payload-hash receipt remain.
- **FR-MCP-FED-BACKFILL-001/review20261110-2**: Given completed batches, when rerun, then no duplicate rows or external effects occur and already_applied counts are reported.
- **FR-MCP-FED-WSSYNC-001/review20261110-2**: Given CurrentRequirementLayerKey differs, when heartbeat runs, then hub-owned value remains and drift is reported.
- **FR-MCP-125/review20261110-2**: Given degraded QuadBrain coordinator, when authorized general federation control or audit runs, then no keyserver call occurs and the operation is not blocked by coordinator state.
- **TEST-MCP-MEMORY-FED-001/review20261110-1**: Given different same-base edits to M1.text, when sync merges, then needs_review returns base/local/incoming values and retains the valid local head.
- **TR-MCP-AUDIT-004/review20261110-2**: Given E1 key with a changed payload, when submitted, then409 audit_id_conflict performs no overwrite.
- **TR-MCP-FED-001/review20261110-2**: Given explicit DirectProxy mode, when a foreign workspace is addressed, then correct routing and loop protection remain intact.
- **TR-MCP-TXNCOMPAT-001/review20261110-1**: Given federation apply/control operations and RequiredForMutations=true, when executed, then ITurnTransactionCoordinator invocation count is0; brain-slot.invoke remains gated.
- **FR-MCP-103/review20261110-2**: Given H and A commit concurrently, when synchronization completes, then both histories survive or a visible review blocks only that branch.
- **TEST-MCP-FED-WSSYNC-001/review20261110-1**: Given H layer release and A layer draft, when A heartbeats, then H remains release and drift=true; only a separately authorized policy commit changes it.
- **FR-MCP-AUDIT-001/review20261110-3**: Given legacy and new compressed payload fixtures, when read/migrated on all supported providers, then their decoded values and historical text remain unchanged.
- **FR-MCP-FED-ROUTE-001/review20261110-2**: Given legacy proxy routing is explicitly selected, when valid normalized path or GlobalWorkspaceId is supplied, then both resolve the same scoped workspace.
- **FR-MCP-125/review20261110-1**: Given legacy signed envelopes, when explicit legacy-mode tests run, then compatibility remains intact.
- **TR-MCP-FED-MEMORY-001/review20261110-1**: Given MemoryId M1 with versions V1,V2 in Atlas, when A1 is cloned, then both versions and their links match and no global memory is included.
- **TR-MCP-FED-BACKFILL-001/review20261110-1**: Given migration J1 contains all source rows and audit records, when verify executes, then source accounting at J1 and final invariants at M1 both pass, with durable audit receipts.
- **FR-MCP-AUDIT-001/review20261110-1**: Given offline audit E1, when reconnect occurs without a version commit, then the hub stores E1 once and the node retains it.
- **TR-MCP-FED-WSSYNC-001/review20261110-1**: Given older legacy registration lacks fields, when processed, then its existing mode defaults remain without mutating versioned project settings.
- **FR-MCP-AUDIT-001/review20261110-2**: Given runtime storage credentials, when update/delete of any existing audit row is attempted, then it is denied.
- **TEST-MCP-AUDIT-003/review20261110-1**: Given source has100 TODO audit and20 generic audit records, when backfill verifies, then all120 matching hashes and receipts exist at the hub; missing one blocks cutover.
- **FR-MCP-FED-BACKFILL-001/review20261110-1**: Given the domain manifest, when migration planning runs, then every source table/writer has a classified destination or explicit exclusion and no unknown domain passes.
- **TR-MCP-FED-001/review20261110-1**: Given versioned mode, when a TODO request arrives, then LocalProxy middleware is not invoked.
- **FR-MCP-MEMORY-008/review20261110-1**: Given workspace memory M1 edited offline, when saved, then the local read changes but remote C0 does not; after explicit commit and sync the peer receives the version.
- **TEST-MCP-FED-BACKFILL-001/review20261110-1**: Given100 source TODO rows and120 audit records, when migration repeats after acknowledgement loss, then destination counts remain100/120, original timestamps/IDs resolve and external dispatch count is0.

### Required fixture coverage

Ph3 migration fixtures cover every classified project domain and its child/link graph, soft-deleted rows, multiple projects, decimal/UTC/binary/Unicode edge cases, promoted session snapshots, audit records, derived-index rebuild and inert operational metadata. ResumeAfterBatchCommit injects death after target-batch commit before acknowledgement. FailedVerifyCannotCutover additionally tests death before provider mapping switch and after switch before reopening: recovery opens exactly one verified provider, retains source bytes and never loses Dolt-only writes. Dry-run and failed verification cannot alter source or routing. Ph6 repeats these invariants for reviewed joins.

Path fixtures retain syntax-aware Windows/UNC/Linux normalization and containment, no host-native reinterpretation of a foreign path, and backed-up transactional source rekey where required. Session fixtures preserve every field/child, PlanFile/TodoId, unknown/missing-session behavior and source sanitization parity. Memory fixtures preserve IDs, scope, ownership, versions and soft deletion. Configuration fixtures retain hub-admin requirement-layer policy, report drift, and keep RunAs/paths/tunnels host-local. These are applicable migration/security constraints, not additional historical feature work.
