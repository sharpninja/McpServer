# P4 AM1 review record: docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md

PLAN-C4MODEL-001 phase P4, document code AM1. Source fences are those of docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md at commit b457ac22e4588912e38fd58ffda14a47cc967361; every line number below is a line of that commit. Model fragment docs/architecture/c4/model/am1.dsl, views fragment docs/architecture/c4/views/am1.dsl. Generated 2026-10-19T21:35:27 (local) by SCRATCH/p4-am1/gen-review.ps1 from the converter coverage report SCRATCH/p4-am1/convert-out/coverage.json, its reuse report convert-out/reused.txt, the element list convert-out/canonical-elements.txt and the notes in SCRATCH/p4-am1/review-notes.psd1.

## Summary

* Fences: 107 (activity flowchart 62, structure flowchart 1, delivery-order flowchart 1, use-case flowchart 13, sequence diagram 17, ER diagram 9, state diagram 4).
* Views: 707, one image and view-key pair each in the converted document.
* Activity flowcharts: 735 of 735 nodes and 744 of 744 edges shown.
* Sequence diagrams: 106 messages, 140 replies, 12 self messages, 22 notes, 46 blocks with 147 branches; 512 view steps of 512 expected (including 39 continuation steps of long self steps).
* ER diagrams: 35 of 35 entity occurrences and 26 of 26 relationship occurrences shown.
* State diagrams: 27 of 27 state elements and 42 of 42 transitions and substate links shown (42 source transitions).
* Structure, use-case and delivery-order flowcharts: 154 of 154 element edges shown.
* Coverage gaps: none (every fence below shows all of its source material)

## How the views are keyed and split

* A diagram the document names keeps its name as the view key (D6): 62 activity fences (the 8 shared components of section 11, FencedLease to OrderedEventReplay; the 25 units of section 14 with their 4 nested activities; the 9 policies of section 15 with their 2 nested activities; the 5 processes of section 16 with their 5 nested activities; the 4 HostileReview worker units), the two nested sequences (StreamOneAttempt, FinalizeOneRequest) and the 12 use-case diagrams (UC-HV-01 to UC-HV-12, the labels section 18 gives them).
* One named diagram collides with an existing view key: RecordModelGradeCommand is a SessionTurn (STR) view. The amended fence F42 (line 1265) is not that diagram (it replaces the foundation's receipt checks with an IdempotentCreate lookup, guards read inside FinalizeHvRequestCommand's transaction and DecideAgentResponseTrustCommand), so it is keyed AM1-RecordModelGradeCommand, as the brief requires for a name already used with another fence.
* Diagrams without a name are keyed AM1-<section>-<NN>: AM1-04-01 (section 4), AM1-07-01 to AM1-07-04 (section 7), AM1-17-01 to AM1-17-15 (section 17), AM1-18-01 (the use-case overview), AM1-20-01 (section 20.1) and AM1-B-01 to AM1-B-09 (Addendum B).
* Splits follow D6: a view that would not fit a letter page at 8 to 16 pt is split into -part2, -part3 and so on (453 keys); a sequence block gives one view per branch, -alt<k>-<i> (250 keys), -opt<k> (7) and -loop<k> (3); the document has no break or par block. keys-report.ps1, 21:02 local: 707 views for 107 fences.
* Views by kind: 323 custom (activities, states, ER groups, the delivery order), 305 dynamic (sequences), 52 container and 27 component (the section 4 structure and the use cases). Every fence is replaced by its image links (c4/rendered/<key>.svg), a blank line and "View: <key>" lines (D7); verify-doc.ps1: 107 of 107 fences replaced, 707 image pairs, 0 differences outside the fences and section 23.
* Section 23 (Diagram index) names the view keys: one lead paragraph with the view rule and the key scheme, as the foundation's section 9 does, and the keys of every unnamed diagram added to its bullet; the bullets themselves are unchanged.
* Mapping (D5): activity flowcharts are custom views of Activity elements (Start, Action, Decision, Error, End shapes; a double-bordered call node is an Action with meta "Activity: call"); sequences are dynamic views over one relationship per participant pair; ER diagrams are custom views of Entity elements with their drawn columns as properties; state diagrams are custom views of State elements with each self transition in a part of its own; the section 4 structure and the use cases are container and component views filtered by relationship tag (Am1Context, Am1UseCase); the delivery order of section 20.1 is a custom view of BoundedContext elements and three Activity actions.

## Reuse (D4) and existing views (D1)

* D1, existing views: none reused. compare-fences.ps1 (21:03:27 local) compared the 107 AM1 fence bodies with the 213 fence bodies behind the STR views: 0 identical bodies; the closest pairs share 0.33 to 0.44 of their lines (AM1 F101, F103, F104 against STR F212 and AM1 F106 against STR F211, ER groups that repeat column lines such as WorkspaceId PK). The only key collision (RecordModelGradeCommand) is a different diagram, keyed AM1-RecordModelGradeCommand.
* D4, elements: a node, participant or actor that is an existing element uses it. Sequence participants: Caller is qbrain.libs.client (QBrainAi.Client), API is qbrain.server.api (REST Controllers), Pipe is qbrain.server.strWorkspaceResolutionBehavior, DB is qbrain.db, TG is qbrain.server.triage, Model is strHostModel, Plugin and Plugin watcher are qbrain.plugins.pluginPs (lib-ps), Prompt hook is qbrain.plugins.pluginHosts (Host adapters); in F84 (17.15) H is the foundation's qbrain.server.strSessionLogHandlers. New participants are the HV components: H is am1HvHandlers (am1HvFinalizer in F76, FinalizeOneRequest), W, Worker A and Worker B are am1HvWorkerHost, AD is am1ReviewerAdapters.
* Section 4 (F1) nodes: PL lib-ps, RP REPL Host, DR Director CLI and TUI (mktDirector), MC AI Coding Agent (agent), API REST Controllers, PIPE the CQRS library (qbrain.server.cqrsLib, as the STR system context does), TR the two STR handler components (strSessionLogHandlers and strAgentContextHandlers), TRG triage, REV LLM Providers and Agent CLIs (llm), DB the MCP Database; HV, WK, AD and FIN are the new HV components and BUS is the as-built change event bus.
* Entities: SessionLog, SessionLogTurn, Requirement, TriageReport, HostileReviewRequest, HostileReviewExecution and HostileReviewDiagnostic are as-built entities (entSessionLog and so on); AgentResponse and AgentResponseGrade are the STR planned entities (strEntAgentResponse, strEntAgentResponseGrade). Their drawn columns are view properties, never a second element. The two as-built HostileReview foreign keys (request to execution, request to diagnostic) are reused by B.9.
* Relationships: a sequence pair that already has a relationship in either direction reuses it (11 pairs, listed below); 145 planned relationships carry the HV interactions of the sequence, structure and use-case views.
* Existing elements reused (25, from convert-out/reused.txt; identifier, then where the conversion first used it):
  * qbrain.plugins.pluginPs | element (F1)
  * qbrain.repl | element (F1)
  * mktDirector | element (F1)
  * agent | element (F1)
  * qbrain.server.api | element (F1)
  * qbrain.server.cqrsLib | element (F1)
  * qbrain.server.strSessionLogHandlers | element (F1)
  * qbrain.server.strAgentContextHandlers | element (F1)
  * qbrain.server.triage | element (F1)
  * llm | element (F1)
  * qbrain.db | element (F1)
  * developer | element (actor, F85)
  * strHostModel | element (actor, F85)
  * qbrain.libs.client | element (actor, F85)
  * entSessionLog | entity SessionLog
  * entSessionLogTurn | entity SessionLogTurn
  * entRequirement | entity Requirement
  * entTriageReport | entity TriageReport
  * strEntAgentResponse | entity AgentResponse
  * strEntAgentResponseGrade | entity AgentResponseGrade
  * entHostileReviewRequest | entity HostileReviewRequest
  * entHostileReviewExecution | entity HostileReviewExecution
  * entHostileReviewDiagnostic | entity HostileReviewDiagnostic
  * qbrain.server.strWorkspaceResolutionBehavior | element (sequence participant)
  * qbrain.plugins.pluginHosts | element (sequence participant)
* Existing relationships reused by sequence steps and entity diagrams (13):
  * relationship entHostileReviewRequest -> entHostileReviewExecution "1 to 0..n via RequestId" (F107)
  * relationship entHostileReviewRequest -> entHostileReviewDiagnostic "1 to 0..n via RequestId" (F107)
  * relationship qbrain.libs.client -> qbrain.server.api "Sends REST requests to /qbrainai routes" (sequence steps)
  * relationship qbrain.server.api -> qbrain.server.strWorkspaceResolutionBehavior "Dispatches every command and query into the pipeline" (sequence steps)
  * relationship qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "Runs its own steps (sequence self-messages and notes)" (sequence steps)
  * relationship strHostModel -> qbrain.plugins.pluginPs "Calls plugin verbs and tools" (sequence steps)
  * relationship qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Runs its own steps (sequence self-messages and notes)" (sequence steps)
  * relationship qbrain.plugins.pluginPs -> qbrain.server.api "Sends session-log writes and reads through the REPL" (sequence steps)
  * relationship qbrain.libs.client -> qbrain.libs.client "Runs its own steps (sequence self-messages and notes)" (sequence steps)
  * relationship qbrain.plugins.pluginHosts -> qbrain.plugins.pluginPs "Calls core" (sequence steps)
  * relationship qbrain.server.api -> qbrain.server.strSessionLogHandlers "Dispatches session-log commands and queries (SendAsync, QueryAsync)" (sequence steps)
  * relationship qbrain.server.strSessionLogHandlers -> qbrain.db "Reads and writes sessions, turns and reviews" (sequence steps)
  * relationship qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "Runs its own steps (sequence self-messages and notes)" (sequence steps)

## New canonical elements

* Counts (convert-out/canonical-elements.txt, list-elements.ps1 over model/am1.dsl): New elements: 889 | relationships: 968; Activity: 738; BoundedContext: 9; Component: 5; Entity: 14; State: 27; UseCase: 96; relationships tagged (none, as built): 2; relationships tagged Planned: 839; relationships tagged Planned,Am1Context: 20; relationships tagged Planned,Am1UseCase: 107; Activity by shape: Action 263, Decision 123, End 151, Error 139, Start 62.
* Component (identifier, name, tags):
  * qbrain.server.am1HvHandlers, Hostile Validation Handlers, Component,Planned
  * qbrain.server.am1HvFinalizer, HV Finalization Handler, Component,Planned
  * qbrain.server.am1HvWorkerHost, HV Worker Host, Component,Planned
  * qbrain.server.am1ReviewerAdapters, HV Reviewer Adapters, Component,Planned
  * qbrain.server.am1ChangeEventBus, Change Event Bus, Component
* Entity (identifier, name, tags):
  * am1EntHvRequest, HvRequest, Entity,Planned
  * am1EntHvRequestTurn, HvRequestTurn, Entity,Planned
  * am1EntHvRun, HvRun, Entity,Planned
  * am1EntHvRequestRequirementRef, HvRequestRequirementRef, Entity,Planned
  * am1EntHvIncident, HvIncident, Entity,Planned
  * am1EntHvReviewer, HvReviewer, Entity,Planned
  * am1EntHvWorkspaceState, HvWorkspaceState, Entity,Planned
  * am1EntHvRequestResponse, HvRequestResponse, Entity,Planned
  * am1EntHvRunOutputChunk, HvRunOutputChunk, Entity,Planned
  * am1EntHvTurnAssessment, HvTurnAssessment, Entity,Planned
  * am1EntHvFinding, HvFinding, Entity,Planned
  * am1EntHvEvidence, HvEvidence, Entity,Planned
  * am1EntHvNotification, HvNotification, Entity,Planned
  * am1EntHostileReviewAttempt, HostileReviewAttempt, Entity,Planned
* BoundedContext (identifier, name, tags):
  * am1BcHvStorageSchema, HvStorageSchema context, BoundedContext,Planned
  * am1BcHvSharedComponents, HvSharedComponents context, BoundedContext,Planned
  * am1BcHvReviewerPool, HvReviewerPool context, BoundedContext,Planned
  * am1BcHvAdmission, HvAdmission context, BoundedContext,Planned
  * am1BcHvExecution, HvExecution context, BoundedContext,Planned
  * am1BcHvFinalization, HvFinalization context, BoundedContext,Planned
  * am1BcHvRestSurfaces, HvRestSurfaces context, BoundedContext,Planned
  * am1BcHvStreamsAndOperations, HvStreamsAndOperations context, BoundedContext,Planned
  * am1BcHvClientParity, HvClientParity context, BoundedContext,Planned
* UseCase (identifier, name, tags):
  * am1UcManageTheReviewerPool, Manage the reviewer pool, UseCase,Planned
  * am1UcNONCEAReviewer, NONCE a reviewer, UseCase,Planned
  * am1UcSubmitAnHVRequest, Submit an HV request, UseCase,Planned
  * am1UcRunAReviewLive, Run a review live, UseCase,Planned
  * am1UcFinalizeTheHVResult, Finalize the HV result, UseCase,Planned
  * am1UcWatchAnHVLive, Watch an HV live, UseCase,Planned
  * am1UcReceiveCompletionNotifications, Receive completion notifications, UseCase,Planned
  * am1UcCancelAnHV, Cancel an HV, UseCase,Planned
  * am1UcRecoverFromFailures, Recover from failures, UseCase,Planned
  * am1UcRetrieveTheResultAndEvidence, Retrieve the result and evidence, UseCase,Planned
  * am1UcRemoveEvidenceOnInstruction, Remove evidence on instruction, UseCase,Planned
  * am1UcRemediateAfterAReviewedTurn, Remediate after a reviewed turn, UseCase,Planned
  * am1UcListAndReadReviewers, List and read reviewers, UseCase,Planned
  * am1UcUpdateWithTheStateVersionUpdateHvReviewerCommand, Update with the state version (UpdateHvReviewerCommand), UseCase,Planned
  * am1UcLaunchChangedUnverifiedThenNONCE, Launch changed: Unverified, then NONCE, UseCase,Planned
  * am1UcStaleStateVersion409Conflict, Stale state version: 409 conflict, UseCase,Planned
  * am1UcAuditLedgerUnavailable503, Audit ledger unavailable: 503, UseCase,Planned
  * am1UcProbeRunWithAFreshNonce, Probe run with a fresh nonce (RunReviewerProcess), UseCase,Planned
  * am1UcDecideReadinessProbeVerdict, Decide (ReadinessProbeVerdict), UseCase,Planned
  * am1UcAlreadyRequestedOrRunningNoSecond, Already requested or running: no second probe, UseCase,Planned
  * am1UcDisabledReviewer409, Disabled reviewer: 409, UseCase,Planned
  * am1UcProbeFailedOfflineIncidentFiled, Probe failed: Offline, incident filed, UseCase,Planned
  * am1UcWorkerDiedProbeRequestedAgain, Worker died: probe requested again, UseCase,Planned
  * am1UcStoredLogCorruptServerIncidentReadiness, Stored log corrupt: server incident, readiness unchanged, UseCase,Planned
  * am1UcReconfiguredWhileProbingStaleProbeReadiness, Reconfigured while probing: stale probe, readiness unchanged, UseCase,Planned
  * am1UcRestartAfterASettledProbeNo, Restart after a settled probe: no automatic retry, UseCase,Planned
  * am1UcResolveMembersHvScopeResolution, Resolve members (HvScopeResolution), UseCase,Planned
  * am1UcCheckEligibilityHvAdmissionEligibility, Check eligibility (HvAdmissionEligibility), UseCase,Planned
  * am1UcAssignRoundRobinSubmitterAndAuthor, Assign round-robin,, UseCase,Planned
  * am1UcStoreTheAcceptedRequestAndFreeze, Store the accepted request and freeze the turns, UseCase,Planned
  * am1UcSameKeyAndInputsStoredRequest, Same key and inputs: stored request, UseCase,Planned
  * am1UcBadOversizedSelfIncludingOrEmpty, Bad, oversized, self-including, or empty scope: 400, UseCase,Planned
  * am1UcNotTerminalPendingOrReviewed409, Not terminal, pending, or reviewed: 409 listing every turn, UseCase,Planned
  * am1UcNoEligibleReviewer409WithReviewer, No eligible reviewer: 409 with reviewer context, UseCase,Planned
  * am1UcUnnecessaryTurnIncludedAndGraded, Unnecessary turn: included and graded, UseCase,Planned
  * am1UcSelfReviewAbsentOrAnyGrade, Self-review absent or any grade: same admission, UseCase,Planned
  * am1UcAuditLedgerUnavailable503NothingWritten, Audit ledger unavailable: 503, nothing written, UseCase,Planned
  * am1UcClaimWithLeaseAndFenceClaimHvRunCommand, Claim with lease and fence (ClaimHvRunCommand), UseCase,Planned
  * am1UcStreamEveryLineAsItIs, Stream every line as it is written, UseCase,Planned
  * am1UcHeartbeatRecordHvRunHeartbeatCommand, Heartbeat (RecordHvRunHeartbeatCommand), UseCase,Planned
  * am1UcCloseTheOutputCloseHvRunOutputCommand, Close the output (CloseHvRunOutputCommand), UseCase,Planned
  * am1UcAssigneeIneligibleReassignedAtClaim, Assignee ineligible: reassigned at claim, UseCase,Planned
  * am1UcCancelReviewerStopped, Cancel: reviewer stopped, UseCase,Planned
  * am1UcLaunchFailureOrNonzeroExitUC, Launch failure or nonzero exit: UC-HV-09, UseCase,Planned
  * am1UcLeaseLostStaleFenceWorkerStops, Lease lost: stale fence, worker stops, UseCase,Planned
  * am1UcNoEligibleReviewerWaitsQueuedETA, No eligible reviewer: waits Queued, ETA unknown, UseCase,Planned
  * am1UcAcceptedRequestCorruptAtTheBrief, Accepted request corrupt at the brief:, UseCase,Planned
  * am1UcVerifyV1ToV8, Verify V-1 to V-8 (HvFinalVerification), UseCase,Planned
  * am1UcCommitEverythingAtOnceCommitFinalResult, Commit everything at once (CommitFinalResult), UseCase,Planned
  * am1UcHostileReviewPerTurnHvVerdictMapping, Hostile review per turn (HvVerdictMapping), UseCase,Planned
  * am1UcVerificationFailedAttemptFailsRequestRequeued, Verification failed:, UseCase,Planned
  * am1UcStateChangedTransactionRunsAgain, State changed: transaction runs again, UseCase,Planned
  * am1UcAuditUnavailableStaysFinalizingForThe, Audit unavailable: stays Finalizing for the sweep, UseCase,Planned
  * am1UcAlreadyFinalizedStoredOutcome, Already finalized: stored outcome, UseCase,Planned
  * am1UcAgentContextOffResponsesNotGraded, Agent context Off: responses NotGraded, UseCase,Planned
  * am1UcResultCompressionFailedRolledBackAttempt, Result compression failed:, UseCase,Planned
  * am1UcListRunningHVsQueryHvRequestsQuery, List running HVs (QueryHvRequestsQuery), UseCase,Planned
  * am1UcProgressETALastOutputGetHvRequestQuery, Progress, ETA, last output (GetHvRequestQuery), UseCase,Planned
  * am1UcAttachToTheOutputStreamStreamHvRunOutputQuery, Attach to the output stream (StreamHvRunOutputQuery), UseCase,Planned
  * am1UcDroppedReconnectWithLastEventID, Dropped: reconnect with Last-Event-ID, UseCase,Planned
  * am1UcNoProgressReportedETAUnknown, No progress reported: ETA Unknown, UseCase,Planned
  * am1UcNewAttemptStreamContinues, New attempt: stream continues, UseCase,Planned
  * am1UcFollowTheStreamFromTheLast, Follow the stream from the last sequence, UseCase,Planned
  * am1UcInboxAndDeliverAtTheNext, Inbox and deliver at the next prompt (WatchHvNotifications), UseCase,Planned
  * am1UcDroppedReconnectFromTheRecordedSequence, Dropped: reconnect from the recorded sequence, UseCase,Planned
  * am1UcNoStreamsPollQueryHvNotificationsQuery, No streams: poll (QueryHvNotificationsQuery), UseCase,Planned
  * am1UcDeliveredTwiceDeduplicatedBySequence, Delivered twice: deduplicated by sequence, UseCase,Planned
  * am1UcCancelCancelHvRequestCommand, Cancel (CancelHvRequestCommand), UseCase,Planned
  * am1UcRunningWorkerStopsTheReviewerAt, Running: worker stops the reviewer at its heartbeat, UseCase,Planned
  * am1UcAlreadyCanceledUnchanged, Already canceled: unchanged, UseCase,Planned
  * am1UcFinalizingOrTerminal409, Finalizing or terminal: 409, UseCase,Planned
  * am1UcWorkerDeadLeaseExpiryCompletesThe, Worker dead: lease expiry completes the cancel, UseCase,Planned
  * am1UcExitedBeforeTheHeartbeatTheClose, Exited before the heartbeat: the close completes the cancel, UseCase,Planned
  * am1UcRecordTheFailureFailHvRunCommand, Record the failure (FailHvRunCommand), UseCase,Planned
  * am1UcFileTheIncidentFileHvIncidentCommand, File the incident (FileHvIncidentCommand), UseCase,Planned
  * am1UcRequeueAndReassignHvRetryDecisionRoundRobinAssignment, Requeue and reassign (HvRetryDecision, RoundRobinAssignment), UseCase,Planned
  * am1UcAttemptsSpentRequestFailedTurnsReleased, Attempts spent: request Failed, turns released, UseCase,Planned
  * am1UcTriageUnreachableFilingRetried, Triage unreachable: filing retried, UseCase,Planned
  * am1UcDeadWorkerLeasesSweptRunsFenced, Dead worker: leases swept, runs fenced out, UseCase,Planned
  * am1UcGracefulShutdownNotCounted, Graceful shutdown: not counted, UseCase,Planned
  * am1UcTwoSweepsOnOneExpiredRun, Two sweeps on one expired run: the second skips it, UseCase,Planned
  * am1UcResultDocumentGetHvResultQuery, Result document (GetHvResultQuery), UseCase,Planned
  * am1UcEvidenceDownloadGetHvEvidenceQuery, Evidence download (GetHvEvidenceQuery), UseCase,Planned
  * am1UcAuditEveryReadAuditLedgerGuard, Audit every read (AuditLedgerGuard), UseCase,Planned
  * am1UcNotReady409, Not ready: 409, UseCase,Planned
  * am1UcRemoved410WithHashes, Removed: 410 with hashes, UseCase,Planned
  * am1UcAuditUnavailable503, Audit unavailable: 503, UseCase,Planned
  * am1UcRemoveWithReasonAndNameRemoveHvEvidenceCommand, Remove with reason and name (RemoveHvEvidenceCommand), UseCase,Planned
  * am1UcRequestActive409, Request active: 409, UseCase,Planned
  * am1UcAlreadyRemovedUnchanged, Already removed: unchanged, UseCase,Planned
  * am1UcReadTheFailuresAndRemediation, Read the failures and remediation, UseCase,Planned
  * am1UcNewTurnsNewHVRequest, New turns, new HV request, UseCase,Planned
  * am1UcWriteToAReviewedTurn409, Write to a reviewed turn: 409 turn_immutable, UseCase,Planned
  * am1UcOwnHostileReview403, Own hostile review: 403, UseCase,Planned
  * am1UcSameTurnsAgain409AlreadyReviewed, Same turns again: 409 already reviewed, UseCase,Planned
  * am1UcAGREETODOUnchangedCoordinatorDecides, AGREE: TODO unchanged, coordinator decides, UseCase,Planned
* State (identifier, name, tags):
  * am1AM10701_Start, HV request: start, State,Start,Planned
  * am1AM10701_Queued, HV request: Queued, State,Planned
  * am1AM10701_Running, HV request: Running, State,Planned
  * am1AM10701_Canceled, HV request: Canceled, State,Planned
  * am1AM10701_CancelRequested, HV request: CancelRequested, State,Planned
  * am1AM10701_Finalizing, HV request: Finalizing, State,Planned
  * am1AM10701_Completed, HV request: Completed, State,Planned
  * am1AM10701_Failed, HV request: Failed, State,Planned
  * am1AM10701_End, HV request: end, State,End,Planned
  * am1AM10702_Start, Run attempt: start, State,Start,Planned
  * am1AM10702_Claimed, Run attempt: Claimed, State,Planned
  * am1AM10702_Running, Run attempt: Running, State,Planned
  * am1AM10702_OutputClosed, Run attempt: OutputClosed, State,Planned
  * am1AM10702_Failed, Run attempt: Failed, State,Planned
  * am1AM10702_Canceled, Run attempt: Canceled, State,Planned
  * am1AM10702_Abandoned, Run attempt: Abandoned, State,Planned
  * am1AM10702_Succeeded, Run attempt: Succeeded, State,Planned
  * am1AM10702_End, Run attempt: end, State,End,Planned
  * am1AM10703_Start, Reviewer readiness: start, State,Start,Planned
  * am1AM10703_Unverified, Reviewer readiness: Unverified, State,Planned
  * am1AM10703_Online, Reviewer readiness: Online, State,Planned
  * am1AM10703_Offline, Reviewer readiness: Offline, State,Planned
  * am1AM10704_Start, Turn HV state: start, State,Start,Planned
  * am1AM10704_None, Turn HV state: None, State,Planned
  * am1AM10704_Pending, Turn HV state: Pending, State,Planned
  * am1AM10704_Final, Turn HV state: Final, State,Planned
  * am1AM10704_End, Turn HV state: end, State,End,Planned

## Fences

* F1, line 90, structure flowchart (section 4. System context and component ownership)
  * Views (13): AM1-04-01, AM1-04-01-part2, AM1-04-01-part3, AM1-04-01-part4, AM1-04-01-part5, AM1-04-01-part6, AM1-04-01-part7, AM1-04-01-part8, AM1-04-01-part9, AM1-04-01-part10, AM1-04-01-part11, AM1-04-01-part12, AM1-04-01-part13
  * Source 15 nodes and 18 edges; drawn on 16 model elements (a node that names an existing element or a use case drawn twice is one element) with 20 element edges; views show 16 of 16 elements and 20 of 20 element edges.
* F2, line 196, state diagram (section 7.1 HV request)
  * Views (7): AM1-07-01, AM1-07-01-part2, AM1-07-01-part3, AM1-07-01-part4, AM1-07-01-part5, AM1-07-01-part6, AM1-07-01-part7
  * Source 7 states, 2 start or end pseudo states and 14 transitions; model 9 State elements and 14 relationships (0 substate links); views show 9 of 9 elements and 14 of 14 transitions and links.
* F3, line 219, state diagram (section 7.2 Run attempt)
  * Views (8): AM1-07-02, AM1-07-02-part2, AM1-07-02-part3, AM1-07-02-part4, AM1-07-02-part5, AM1-07-02-part6, AM1-07-02-part7, AM1-07-02-part8
  * Source 7 states, 2 start or end pseudo states and 16 transitions; model 9 State elements and 16 relationships (0 substate links); views show 9 of 9 elements and 16 of 16 transitions and links.
* F4, line 243, state diagram (section 7.3 Reviewer readiness)
  * Views (2): AM1-07-03, AM1-07-03-part2
  * Source 3 states, 1 start or end pseudo states and 7 transitions; model 4 State elements and 7 relationships (0 substate links); views show 4 of 4 elements and 7 of 7 transitions and links.
* F5, line 258, state diagram (section 7.4 HV state of a turn)
  * Views (2): AM1-07-04, AM1-07-04-part2
  * Source 3 states, 2 start or end pseudo states and 5 transitions; model 5 State elements and 5 relationships (0 substate links); views show 5 of 5 elements and 5 of 5 transitions and links.
* F6, line 333, activity flowchart (section 11.1 FencedLease (namespace `QBrainAi.Support.Mcp.Storage.Leases`))
  * Views (6): FencedLease, FencedLease-part2, FencedLease-part3, FencedLease-part4, FencedLease-part5, FencedLease-part6
  * Source 18 nodes and 17 edges; model 18 Activity elements (Start 1, Action 4, Decision 5, Error 2, End 6) and 17 relationships; views show 18 of 18 nodes and 17 of 17 edges.
* F7, line 358, activity flowchart (section 11.2 GzipCodec (namespace `QBrainAi.Support.Mcp.Storage.Compression`))
  * Views (4): GzipCodec, GzipCodec-part2, GzipCodec-part3, GzipCodec-part4
  * Source 12 nodes and 11 edges; model 12 Activity elements (Start 1, Action 3, Decision 2, Error 3, End 3) and 11 relationships; views show 12 of 12 nodes and 11 of 11 edges.
* F8, line 377, activity flowchart (section 11.3 AuditLedgerGuard (namespace `QBrainAi.Support.Mcp.Storage.Auditing`))
  * Views (2): AuditLedgerGuard, AuditLedgerGuard-part2
  * Source 7 nodes and 6 edges; model 7 Activity elements (Start 1, Action 1, Decision 2, Error 1, End 2) and 6 relationships; views show 7 of 7 nodes and 6 of 6 edges.
* F9, line 391, activity flowchart (section 11.4 AgentIdentity (namespace `QBrainAi.Client.Agents`))
  * Views (2): AgentIdentity, AgentIdentity-part2
  * Source 7 nodes and 6 edges; model 7 Activity elements (Start 1, Action 1, Decision 2, Error 0, End 3) and 6 relationships; views show 7 of 7 nodes and 6 of 6 edges.
* F10, line 405, activity flowchart (section 11.5 TurnSnapshotBuilder (namespace `QBrainAi.Support.Mcp.HostileValidation`))
  * Views (2): TurnSnapshotBuilder, TurnSnapshotBuilder-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 4, Decision 0, Error 0, End 1) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F11, line 418, activity flowchart (section 11.6 RequirementReferenceExtractor (namespace `QBrainAi.Support.Mcp.Requirements`))
  * Views (2): RequirementReferenceExtractor, RequirementReferenceExtractor-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 2, Decision 1, Error 0, End 2) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F12, line 431, activity flowchart (section 11.7 NotificationOutbox (namespace `QBrainAi.Support.Mcp.HostileValidation`))
  * Views (1): NotificationOutbox
  * Source 5 nodes and 4 edges; model 5 Activity elements (Start 1, Action 3, Decision 0, Error 0, End 1) and 4 relationships; views show 5 of 5 nodes and 4 of 4 edges.
* F13, line 443, activity flowchart (section 11.8 OrderedEventReplay (namespace `QBrainAi.Support.Mcp.Streaming`))
  * Views (4): OrderedEventReplay, OrderedEventReplay-part2, OrderedEventReplay-part3, OrderedEventReplay-part4
  * Source 10 nodes and 10 edges; model 10 Activity elements (Start 1, Action 3, Decision 3, Error 1, End 2) and 10 relationships; views show 10 of 10 nodes and 10 of 10 edges.
* F14, line 615, activity flowchart (section ListHvReviewersQuery: list the reviewer pool [FR-MCP-HVPIPE-001, -003])
  * Views (1): ListHvReviewersQuery
  * Source 4 nodes and 3 edges; model 4 Activity elements (Start 1, Action 2, Decision 0, Error 0, End 1) and 3 relationships; views show 4 of 4 nodes and 3 of 3 edges.
* F15, line 627, activity flowchart (section GetHvReviewerQuery: get one reviewer [FR-MCP-HVPIPE-001, -003])
  * Views (2): GetHvReviewerQuery, GetHvReviewerQuery-part2
  * Source 5 nodes and 4 edges; model 5 Activity elements (Start 1, Action 2, Decision 0, Error 1, End 1) and 4 relationships; views show 5 of 5 nodes and 4 of 4 edges.
* F16, line 643, activity flowchart (section UpdateHvReviewerCommand: manage a reviewer in real time [FR-MCP-HVPIPE-001])
  * Views (6): UpdateHvReviewerCommand, UpdateHvReviewerCommand-part2, UpdateHvReviewerCommand-part3, UpdateHvReviewerCommand-part4, UpdateHvReviewerCommand-part5, UpdateHvReviewerCommand-part6
  * Source 18 nodes and 19 edges; model 18 Activity elements (Start 1, Action 7, Decision 4, Error 5, End 1) and 19 relationships; views show 18 of 18 nodes and 19 of 19 edges.
* F17, line 672, activity flowchart (section StartHvReviewerNonceCommand: request a NONCE probe [FR-MCP-HVPIPE-003])
  * Views (4): StartHvReviewerNonceCommand, StartHvReviewerNonceCommand-part2, StartHvReviewerNonceCommand-part3, StartHvReviewerNonceCommand-part4
  * Source 14 nodes and 13 edges; model 14 Activity elements (Start 1, Action 3, Decision 3, Error 4, End 3) and 13 relationships; views show 14 of 14 nodes and 13 of 13 edges.
* F18, line 705, activity flowchart (section SubmitHvRequestCommand: admit an HV request [FR-MCP-HVPIPE-002, -004, -005, -006, -007, -011, -013, -018])
  * Views (7): SubmitHvRequestCommand, SubmitHvRequestCommand-part2, SubmitHvRequestCommand-part3, SubmitHvRequestCommand-part4, SubmitHvRequestCommand-part5, SubmitHvRequestCommand-part6, SubmitHvRequestCommand-part7
  * Source 17 nodes and 17 edges; model 17 Activity elements (Start 1, Action 6, Decision 1, Error 7, End 2) and 17 relationships; views show 17 of 17 nodes and 17 of 17 edges.
* F19, line 728, activity flowchart (section Nested activity: AdmitRequestChecks (the checks of SubmitHvRequestCommand, inside its delegate))
  * Views (3): AdmitRequestChecks, AdmitRequestChecks-part2, AdmitRequestChecks-part3
  * Source 11 nodes and 11 edges; model 11 Activity elements (Start 1, Action 4, Decision 1, Error 4, End 1) and 11 relationships; views show 11 of 11 nodes and 11 of 11 edges.
* F20, line 745, activity flowchart (section Nested activity: InsertAdmittedRequest (the inserts of SubmitHvRequestCommand, inside its delegate))
  * Views (4): InsertAdmittedRequest, InsertAdmittedRequest-part2, InsertAdmittedRequest-part3, InsertAdmittedRequest-part4
  * Source 14 nodes and 13 edges; model 14 Activity elements (Start 1, Action 10, Decision 0, Error 1, End 2) and 13 relationships; views show 14 of 14 nodes and 13 of 13 edges.
* F21, line 768, activity flowchart (section GetHvRequestQuery: get an HV request with its progress [FR-MCP-HVPIPE-015, -016])
  * Views (4): GetHvRequestQuery, GetHvRequestQuery-part2, GetHvRequestQuery-part3, GetHvRequestQuery-part4
  * Source 12 nodes and 11 edges; model 12 Activity elements (Start 1, Action 3, Decision 2, Error 3, End 3) and 11 relationships; views show 12 of 12 nodes and 11 of 11 edges.
* F22, line 789, activity flowchart (section QueryHvRequestsQuery: list HV requests, running ones included [FR-MCP-HVPIPE-015])
  * Views (2): QueryHvRequestsQuery, QueryHvRequestsQuery-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 1, Decision 1, Error 2, End 1) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F23, line 804, activity flowchart (section CancelHvRequestCommand: cancel any HV [FR-MCP-HVPIPE-008])
  * Views (6): CancelHvRequestCommand, CancelHvRequestCommand-part2, CancelHvRequestCommand-part3, CancelHvRequestCommand-part4, CancelHvRequestCommand-part5, CancelHvRequestCommand-part6
  * Source 16 nodes and 15 edges; model 16 Activity elements (Start 1, Action 6, Decision 2, Error 4, End 3) and 15 relationships; views show 16 of 16 nodes and 15 of 15 edges.
* F24, line 831, activity flowchart (section GetHvResultQuery: the standard result document [FR-MCP-HVPIPE-010, -015])
  * Views (4): GetHvResultQuery, GetHvResultQuery-part2, GetHvResultQuery-part3, GetHvResultQuery-part4
  * Source 14 nodes and 13 edges; model 14 Activity elements (Start 1, Action 3, Decision 3, Error 5, End 2) and 13 relationships; views show 14 of 14 nodes and 13 of 13 edges.
* F25, line 854, activity flowchart (section GetHvEvidenceQuery: download evidence [FR-MCP-HVPIPE-011, -015])
  * Views (5): GetHvEvidenceQuery, GetHvEvidenceQuery-part2, GetHvEvidenceQuery-part3, GetHvEvidenceQuery-part4, GetHvEvidenceQuery-part5
  * Source 17 nodes and 16 edges; model 17 Activity elements (Start 1, Action 2, Decision 4, Error 7, End 3) and 16 relationships; views show 17 of 17 nodes and 16 of 16 edges.
* F26, line 880, activity flowchart (section RemoveHvEvidenceCommand: remove evidence on instruction [FR-MCP-HVPIPE-019])
  * Views (4): RemoveHvEvidenceCommand, RemoveHvEvidenceCommand-part2, RemoveHvEvidenceCommand-part3, RemoveHvEvidenceCommand-part4
  * Source 12 nodes and 11 edges; model 12 Activity elements (Start 1, Action 2, Decision 3, Error 5, End 1) and 11 relationships; views show 12 of 12 nodes and 11 of 11 edges.
* F27, line 901, activity flowchart (section StreamHvRunOutputQuery: attach to the live output of any HV [FR-MCP-HVPIPE-016])
  * Views (3): StreamHvRunOutputQuery, StreamHvRunOutputQuery-part2, StreamHvRunOutputQuery-part3
  * Source 12 nodes and 11 edges; model 12 Activity elements (Start 1, Action 2, Decision 2, Error 5, End 2) and 11 relationships; views show 12 of 12 nodes and 11 of 11 edges.
* F28, line 921, activity flowchart (section StreamHvNotificationsQuery: the HV notification stream [FR-MCP-HVPIPE-017])
  * Views (1): StreamHvNotificationsQuery
  * Source 4 nodes and 3 edges; model 4 Activity elements (Start 1, Action 1, Decision 0, Error 1, End 1) and 3 relationships; views show 4 of 4 nodes and 3 of 3 edges.
* F29, line 933, activity flowchart (section QueryHvNotificationsQuery: poll notifications [FR-MCP-HVPIPE-017])
  * Views (2): QueryHvNotificationsQuery, QueryHvNotificationsQuery-part2
  * Source 5 nodes and 4 edges; model 5 Activity elements (Start 1, Action 1, Decision 1, Error 1, End 1) and 4 relationships; views show 5 of 5 nodes and 4 of 4 edges.
* F30, line 946, activity flowchart (section QueryHvIncidentsQuery: list incidents [FR-MCP-HVPIPE-009])
  * Views (2): QueryHvIncidentsQuery, QueryHvIncidentsQuery-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 1, Decision 1, Error 2, End 1) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F31, line 966, activity flowchart (section ClaimHvRunCommand: claim the next probe or review [FR-MCP-HVPIPE-003, -007])
  * Views (11): ClaimHvRunCommand, ClaimHvRunCommand-part2, ClaimHvRunCommand-part3, ClaimHvRunCommand-part4, ClaimHvRunCommand-part5, ClaimHvRunCommand-part6, ClaimHvRunCommand-part7, ClaimHvRunCommand-part8, ClaimHvRunCommand-part9, ClaimHvRunCommand-part10, ClaimHvRunCommand-part11
  * Source 17 nodes and 20 edges; model 17 Activity elements (Start 1, Action 8, Decision 3, Error 2, End 3) and 20 relationships; views show 17 of 17 nodes and 20 of 20 edges.
* F32, line 998, activity flowchart (section RecordHvRunStartCommand: record that the reviewer process started [FR-MCP-HVPIPE-007, -008])
  * Views (4): RecordHvRunStartCommand, RecordHvRunStartCommand-part2, RecordHvRunStartCommand-part3, RecordHvRunStartCommand-part4
  * Source 10 nodes and 9 edges; model 10 Activity elements (Start 1, Action 3, Decision 1, Error 3, End 2) and 9 relationships; views show 10 of 10 nodes and 9 of 9 edges.
* F33, line 1017, activity flowchart (section RecordHvRunHeartbeatCommand: renew the lease and read cancellation [FR-MCP-HVPIPE-007, -008])
  * Views (3): RecordHvRunHeartbeatCommand, RecordHvRunHeartbeatCommand-part2, RecordHvRunHeartbeatCommand-part3
  * Source 8 nodes and 7 edges; model 8 Activity elements (Start 1, Action 3, Decision 0, Error 3, End 1) and 7 relationships; views show 8 of 8 nodes and 7 of 7 edges.
* F34, line 1035, activity flowchart (section AppendHvRunOutputCommand: persist live output [FR-MCP-HVPIPE-008, -011, -016])
  * Views (6): AppendHvRunOutputCommand, AppendHvRunOutputCommand-part2, AppendHvRunOutputCommand-part3, AppendHvRunOutputCommand-part4, AppendHvRunOutputCommand-part5, AppendHvRunOutputCommand-part6
  * Source 18 nodes and 17 edges; model 18 Activity elements (Start 1, Action 5, Decision 3, Error 7, End 2) and 17 relationships; views show 18 of 18 nodes and 17 of 17 edges.
* F35, line 1063, activity flowchart (section CloseHvRunOutputCommand: close a run's output [FR-MCP-HVPIPE-003, -008, -011])
  * Views (10): CloseHvRunOutputCommand, CloseHvRunOutputCommand-part2, CloseHvRunOutputCommand-part3, CloseHvRunOutputCommand-part4, CloseHvRunOutputCommand-part5, CloseHvRunOutputCommand-part6, CloseHvRunOutputCommand-part7, CloseHvRunOutputCommand-part8, CloseHvRunOutputCommand-part9, CloseHvRunOutputCommand-part10
  * Source 27 nodes and 28 edges; model 27 Activity elements (Start 1, Action 11, Decision 4, Error 5, End 6) and 28 relationships; views show 27 of 27 nodes and 28 of 28 edges.
* F36, line 1101, activity flowchart (section FailHvRunCommand: record an execution failure [FR-MCP-HVPIPE-009])
  * Views (10): FailHvRunCommand, FailHvRunCommand-part2, FailHvRunCommand-part3, FailHvRunCommand-part4, FailHvRunCommand-part5, FailHvRunCommand-part6, FailHvRunCommand-part7, FailHvRunCommand-part8, FailHvRunCommand-part9, FailHvRunCommand-part10
  * Source 24 nodes and 26 edges; model 24 Activity elements (Start 1, Action 8, Decision 5, Error 3, End 7) and 26 relationships; views show 24 of 24 nodes and 26 of 26 edges.
* F37, line 1137, activity flowchart (section ExpireHvRunLeasesCommand: recover from dead workers [FR-MCP-HVPIPE-007])
  * Views (7): ExpireHvRunLeasesCommand, ExpireHvRunLeasesCommand-part2, ExpireHvRunLeasesCommand-part3, ExpireHvRunLeasesCommand-part4, ExpireHvRunLeasesCommand-part5, ExpireHvRunLeasesCommand-part6, ExpireHvRunLeasesCommand-part7
  * Source 12 nodes and 15 edges; model 12 Activity elements (Start 1, Action 7, Decision 2, Error 1, End 1) and 15 relationships; views show 12 of 12 nodes and 15 of 15 edges.
* F38, line 1164, activity flowchart (section FinalizeHvRequestCommand: verify and commit the result atomically [FR-MCP-HVPIPE-010, -011, -012, -013, -014, -021])
  * Views (7): FinalizeHvRequestCommand, FinalizeHvRequestCommand-part2, FinalizeHvRequestCommand-part3, FinalizeHvRequestCommand-part4, FinalizeHvRequestCommand-part5, FinalizeHvRequestCommand-part6, FinalizeHvRequestCommand-part7
  * Source 17 nodes and 19 edges; model 17 Activity elements (Start 1, Action 4, Decision 2, Error 3, End 7) and 19 relationships; views show 17 of 17 nodes and 19 of 19 edges.
* F39, line 1189, activity flowchart (section Nested activity: ReadFinalizationInputs (reads of FinalizeHvRequestCommand, no transaction open))
  * Views (3): ReadFinalizationInputs, ReadFinalizationInputs-part2, ReadFinalizationInputs-part3
  * Source 9 nodes and 9 edges; model 9 Activity elements (Start 1, Action 4, Decision 1, Error 2, End 1) and 9 relationships; views show 9 of 9 nodes and 9 of 9 edges.
* F40, line 1204, activity flowchart (section Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate))
  * Views (6): CommitFinalResult, CommitFinalResult-part2, CommitFinalResult-part3, CommitFinalResult-part4, CommitFinalResult-part5, CommitFinalResult-part6
  * Source 16 nodes and 17 edges; model 16 Activity elements (Start 1, Action 10, Decision 1, Error 2, End 2) and 17 relationships; views show 16 of 16 nodes and 17 of 17 edges.
* F41, line 1231, activity flowchart (section FileHvIncidentCommand: file an incident through the triage endpoint [FR-MCP-HVPIPE-009])
  * Views (5): FileHvIncidentCommand, FileHvIncidentCommand-part2, FileHvIncidentCommand-part3, FileHvIncidentCommand-part4, FileHvIncidentCommand-part5
  * Source 15 nodes and 15 edges; model 15 Activity elements (Start 1, Action 8, Decision 1, Error 2, End 3) and 15 relationships; views show 15 of 15 nodes and 15 of 15 edges.
* F42, line 1265, activity flowchart (section RecordModelGradeCommand: record a model grade inside finalization (amended, internal) [FR-MCP-HVPIPE-021])
  * Views (4): AM1-RecordModelGradeCommand, AM1-RecordModelGradeCommand-part2, AM1-RecordModelGradeCommand-part3, AM1-RecordModelGradeCommand-part4
  * Source 11 nodes and 10 edges; model 11 Activity elements (Start 1, Action 5, Decision 1, Error 2, End 2) and 10 relationships; views show 11 of 11 nodes and 10 of 10 edges.
* F43, line 1285, activity flowchart (section Policy HvScopeResolution: resolve the member turns [FR-MCP-HVPIPE-004])
  * Views (7): HvScopeResolution, HvScopeResolution-part2, HvScopeResolution-part3, HvScopeResolution-part4, HvScopeResolution-part5, HvScopeResolution-part6, HvScopeResolution-part7
  * Source 18 nodes and 22 edges; model 18 Activity elements (Start 1, Action 7, Decision 5, Error 4, End 1) and 22 relationships; views show 18 of 18 nodes and 22 of 22 edges.
* F44, line 1315, activity flowchart (section Policy HvAdmissionEligibility: admit only reviewable turns [FR-MCP-HVPIPE-005, -013])
  * Views (3): HvAdmissionEligibility, HvAdmissionEligibility-part2, HvAdmissionEligibility-part3
  * Source 9 nodes and 8 edges; model 9 Activity elements (Start 1, Action 1, Decision 3, Error 3, End 1) and 8 relationships; views show 9 of 9 nodes and 8 of 8 edges.
* F45, line 1331, activity flowchart (section Policy HvReviewerEligibility: who may review [FR-MCP-HVPIPE-002, -003])
  * Views (2): HvReviewerEligibility, HvReviewerEligibility-part2
  * Source 8 nodes and 7 edges; model 8 Activity elements (Start 1, Action 1, Decision 2, Error 0, End 4) and 7 relationships; views show 8 of 8 nodes and 7 of 7 edges.
* F46, line 1346, activity flowchart (section Policy RoundRobinAssignment: pick the next reviewer [FR-MCP-HVPIPE-002, -007])
  * Views (5): RoundRobinAssignment, RoundRobinAssignment-part2, RoundRobinAssignment-part3, RoundRobinAssignment-part4, RoundRobinAssignment-part5
  * Source 9 nodes and 10 edges; model 9 Activity elements (Start 1, Action 3, Decision 2, Error 1, End 2) and 10 relationships; views show 9 of 9 nodes and 10 of 10 edges.
* F47, line 1365, activity flowchart (section Policy HvTurnFreeze: freeze, release, finalize, and guard [FR-MCP-HVPIPE-012])
  * Views (2): HvTurnFreeze, HvTurnFreeze-part2
  * Source 6 nodes and 6 edges; model 6 Activity elements (Start 1, Action 0, Decision 2, Error 2, End 1) and 6 relationships; views show 6 of 6 nodes and 6 of 6 edges.
* F48, line 1388, activity flowchart (section Policy HvFinalVerification: the eight verification steps [FR-MCP-HVPIPE-010, -011, -021])
  * Views (2): HvFinalVerification, HvFinalVerification-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 2, Decision 0, Error 2, End 1) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F49, line 1399, activity flowchart (section Nested activity: VerifyLogAndBinding (V-1, V-8, V-2, V-3 of HvFinalVerification))
  * Views (4): VerifyLogAndBinding, VerifyLogAndBinding-part2, VerifyLogAndBinding-part3, VerifyLogAndBinding-part4
  * Source 11 nodes and 10 edges; model 11 Activity elements (Start 1, Action 0, Decision 4, Error 5, End 1) and 10 relationships; views show 11 of 11 nodes and 10 of 10 edges.
* F50, line 1415, activity flowchart (section Nested activity: VerifyVerdictAndReviewer (V-4 to V-7 of HvFinalVerification))
  * Views (3): VerifyVerdictAndReviewer, VerifyVerdictAndReviewer-part2, VerifyVerdictAndReviewer-part3
  * Source 10 nodes and 9 edges; model 10 Activity elements (Start 1, Action 0, Decision 4, Error 4, End 1) and 9 relationships; views show 10 of 10 nodes and 9 of 9 edges.
* F51, line 1432, activity flowchart (section Policy ReadinessProbeVerdict: the NONCE decision [FR-MCP-HVPIPE-003])
  * Views (3): ReadinessProbeVerdict, ReadinessProbeVerdict-part2, ReadinessProbeVerdict-part3
  * Source 10 nodes and 9 edges; model 10 Activity elements (Start 1, Action 0, Decision 4, Error 4, End 1) and 9 relationships; views show 10 of 10 nodes and 9 of 9 edges.
* F52, line 1449, activity flowchart (section Policy HvProbeOutcome: settle a probe run and its reviewer [FR-MCP-HVPIPE-003, -009])
  * Views (5): HvProbeOutcome, HvProbeOutcome-part2, HvProbeOutcome-part3, HvProbeOutcome-part4, HvProbeOutcome-part5
  * Source 15 nodes and 14 edges; model 15 Activity elements (Start 1, Action 7, Decision 2, Error 0, End 5) and 14 relationships; views show 15 of 15 nodes and 14 of 14 edges.
* F53, line 1471, activity flowchart (section Policy HvRetryDecision: requeue, fail, or cancel [FR-MCP-HVPIPE-007, -009])
  * Views (5): HvRetryDecision, HvRetryDecision-part2, HvRetryDecision-part3, HvRetryDecision-part4, HvRetryDecision-part5
  * Source 15 nodes and 14 edges; model 15 Activity elements (Start 1, Action 7, Decision 3, Error 0, End 4) and 14 relationships; views show 15 of 15 nodes and 14 of 14 edges.
* F54, line 1499, activity flowchart (section 16.1 Process HvWorkerHost: the durable worker [FR-MCP-HVPIPE-007, -008, -009])
  * Views (8): HvWorkerHost, HvWorkerHost-part2, HvWorkerHost-part3, HvWorkerHost-part4, HvWorkerHost-part5, HvWorkerHost-part6, HvWorkerHost-part7, HvWorkerHost-part8
  * Source 14 nodes and 19 edges; model 14 Activity elements (Start 1, Action 9, Decision 2, Error 0, End 2) and 19 relationships; views show 14 of 14 nodes and 19 of 19 edges.
* F55, line 1526, activity flowchart (section 16.2 Process RunReviewerProcess: one attempt, live [FR-MCP-HVPIPE-008, -016])
  * Views (17): RunReviewerProcess, RunReviewerProcess-part2, RunReviewerProcess-part3, RunReviewerProcess-part4, RunReviewerProcess-part5, RunReviewerProcess-part6, RunReviewerProcess-part7, RunReviewerProcess-part8, RunReviewerProcess-part9, RunReviewerProcess-part10, RunReviewerProcess-part11, RunReviewerProcess-part12, RunReviewerProcess-part13, RunReviewerProcess-part14, RunReviewerProcess-part15, RunReviewerProcess-part16, RunReviewerProcess-part17
  * Source 36 nodes and 42 edges; model 36 Activity elements (Start 1, Action 19, Decision 3, Error 0, End 13) and 42 relationships; views show 36 of 36 nodes and 42 of 42 edges.
* F56, line 1574, activity flowchart (section Nested activity: FailAttempt (every FailHvRunCommand call of the worker, drawn once))
  * Views (1): FailAttempt
  * Source 4 nodes and 4 edges; model 4 Activity elements (Start 1, Action 1, Decision 0, Error 0, End 2) and 4 relationships; views show 4 of 4 nodes and 4 of 4 edges.
* F57, line 1584, activity flowchart (section Nested activity: AppendBatch (every AppendHvRunOutputCommand call of the worker, drawn once))
  * Views (2): AppendBatch, AppendBatch-part2
  * Source 5 nodes and 5 edges; model 5 Activity elements (Start 1, Action 1, Decision 0, Error 1, End 2) and 5 relationships; views show 5 of 5 nodes and 5 of 5 edges.
* F58, line 1595, activity flowchart (section Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above))
  * Views (10): StreamReviewerOutput, StreamReviewerOutput-part2, StreamReviewerOutput-part3, StreamReviewerOutput-part4, StreamReviewerOutput-part5, StreamReviewerOutput-part6, StreamReviewerOutput-part7, StreamReviewerOutput-part8, StreamReviewerOutput-part9, StreamReviewerOutput-part10
  * Source 10 nodes and 15 edges; model 10 Activity elements (Start 1, Action 3, Decision 2, Error 1, End 3) and 15 relationships; views show 10 of 10 nodes and 15 of 15 edges.
* F59, line 1629, activity flowchart (section 16.4 Process SubmitHvFromPlugin: the plugin verb [FR-MCP-HVPIPE-005, -020])
  * Views (4): SubmitHvFromPlugin, SubmitHvFromPlugin-part2, SubmitHvFromPlugin-part3, SubmitHvFromPlugin-part4
  * Source 10 nodes and 9 edges; model 10 Activity elements (Start 1, Action 4, Decision 0, Error 4, End 1) and 9 relationships; views show 10 of 10 nodes and 9 of 9 edges.
* F60, line 1646, activity flowchart (section 16.5 Process WatchHvNotifications: completions into the plugin [FR-MCP-HVPIPE-017, -020])
  * Views (5): WatchHvNotifications, WatchHvNotifications-part2, WatchHvNotifications-part3, WatchHvNotifications-part4, WatchHvNotifications-part5
  * Source 10 nodes and 12 edges; model 10 Activity elements (Start 1, Action 4, Decision 3, Error 0, End 2) and 12 relationships; views show 10 of 10 nodes and 12 of 12 edges.
* F61, line 1685, activity flowchart (section 16.7 Process HostileReviewWorkerHost: the artifact worker of FR-MCP-HOSTILEREVIEW-003 (P1 to P3, D-02))
  * Views (4): HostileReviewWorkerHost, HostileReviewWorkerHost-part2, HostileReviewWorkerHost-part3, HostileReviewWorkerHost-part4
  * Source 9 nodes and 13 edges; model 9 Activity elements (Start 1, Action 5, Decision 2, Error 0, End 1) and 13 relationships; views show 9 of 9 nodes and 13 of 13 edges.
* F62, line 1704, activity flowchart (section Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost))
  * Views (12): RunHostileReviewAttempt, RunHostileReviewAttempt-part2, RunHostileReviewAttempt-part3, RunHostileReviewAttempt-part4, RunHostileReviewAttempt-part5, RunHostileReviewAttempt-part6, RunHostileReviewAttempt-part7, RunHostileReviewAttempt-part8, RunHostileReviewAttempt-part9, RunHostileReviewAttempt-part10, RunHostileReviewAttempt-part11, RunHostileReviewAttempt-part12
  * Source 24 nodes and 25 edges; model 24 Activity elements (Start 1, Action 11, Decision 5, Error 0, End 7) and 25 relationships; views show 24 of 24 nodes and 25 of 25 edges.
* F63, line 1735, activity flowchart (section Nested activity: CompleteAttempt (every CompleteHostileReviewAttemptCommand call of the worker, drawn once))
  * Views (1): CompleteAttempt
  * Source 4 nodes and 4 edges; model 4 Activity elements (Start 1, Action 1, Decision 0, Error 0, End 2) and 4 relationships; views show 4 of 4 nodes and 4 of 4 edges.
* F64, line 1749, activity flowchart (section ClaimHostileReviewAttemptCommand: claim the next queued artifact review [FR-MCP-HOSTILEREVIEW-003])
  * Views (4): ClaimHostileReviewAttemptCommand, ClaimHostileReviewAttemptCommand-part2, ClaimHostileReviewAttemptCommand-part3, ClaimHostileReviewAttemptCommand-part4
  * Source 10 nodes and 9 edges; model 10 Activity elements (Start 1, Action 4, Decision 1, Error 2, End 2) and 9 relationships; views show 10 of 10 nodes and 9 of 9 edges.
* F65, line 1768, activity flowchart (section RenewHostileReviewAttemptCommand: keep a running artifact review's lease [FR-MCP-HOSTILEREVIEW-003])
  * Views (4): RenewHostileReviewAttemptCommand, RenewHostileReviewAttemptCommand-part2, RenewHostileReviewAttemptCommand-part3, RenewHostileReviewAttemptCommand-part4
  * Source 9 nodes and 9 edges; model 9 Activity elements (Start 1, Action 3, Decision 1, Error 3, End 1) and 9 relationships; views show 9 of 9 nodes and 9 of 9 edges.
* F66, line 1787, activity flowchart (section CompleteHostileReviewAttemptCommand: accept one terminal result [FR-MCP-HOSTILEREVIEW-003])
  * Views (6): CompleteHostileReviewAttemptCommand, CompleteHostileReviewAttemptCommand-part2, CompleteHostileReviewAttemptCommand-part3, CompleteHostileReviewAttemptCommand-part4, CompleteHostileReviewAttemptCommand-part5, CompleteHostileReviewAttemptCommand-part6
  * Source 20 nodes and 19 edges; model 20 Activity elements (Start 1, Action 9, Decision 3, Error 3, End 4) and 19 relationships; views show 20 of 20 nodes and 19 of 19 edges.
* F67, line 1816, activity flowchart (section ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers [FR-MCP-HOSTILEREVIEW-003])
  * Views (7): ExpireHostileReviewLeasesCommand, ExpireHostileReviewLeasesCommand-part2, ExpireHostileReviewLeasesCommand-part3, ExpireHostileReviewLeasesCommand-part4, ExpireHostileReviewLeasesCommand-part5, ExpireHostileReviewLeasesCommand-part6, ExpireHostileReviewLeasesCommand-part7
  * Source 11 nodes and 15 edges; model 11 Activity elements (Start 1, Action 5, Decision 3, Error 1, End 1) and 15 relationships; views show 11 of 11 nodes and 15 of 15 edges.
* F68, line 1841, sequence diagram (section 17.1 ListHvReviewersQuery and GetHvReviewerQuery)
  * Views (8): AM1-17-01, AM1-17-01-part2, AM1-17-01-alt1-1, AM1-17-01-alt1-2, AM1-17-01-alt1-2-part2, AM1-17-01-alt2-1, AM1-17-01-alt2-2, AM1-17-01-alt2-2-part2
  * Participants 5: Caller=Caller -> qbrain.libs.client; API=API -> qbrain.server.api; Pipe=Pipe -> qbrain.server.strWorkspaceResolutionBehavior; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db.
  * Source 4 messages, 4 replies, 1 self messages, 0 notes, 2 blocks with 4 branches; views carry 15 steps of 15 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 4 branch views for 4 branches.
* F69, line 1867, sequence diagram (section 17.2 UpdateHvReviewerCommand)
  * Views (14): AM1-17-02, AM1-17-02-part2, AM1-17-02-alt1-1, AM1-17-02-alt1-2, AM1-17-02-alt2-1, AM1-17-02-alt2-2, AM1-17-02-alt2-3, AM1-17-02-alt3-1, AM1-17-02-alt3-1-part2, AM1-17-02-alt3-2, AM1-17-02-alt3-3, AM1-17-02-alt3-4, AM1-17-02-alt3-4-part2, AM1-17-02-alt3-4-part3
  * Participants 6: Caller=Caller -> qbrain.libs.client; API=API -> qbrain.server.api; Pipe=Pipe -> qbrain.server.strWorkspaceResolutionBehavior; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db; W=W -> qbrain.server.am1HvWorkerHost.
  * Source 5 messages, 9 replies, 0 self messages, 0 notes, 3 blocks with 9 branches; views carry 26 steps of 26 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 9 branch views for 9 branches.
* F70, line 1905, sequence diagram (section 17.3 StartHvReviewerNonceCommand and the probe it starts)
  * Views (27): AM1-17-03, AM1-17-03-part2, AM1-17-03-alt1-1, AM1-17-03-alt1-2, AM1-17-03-alt1-3, AM1-17-03-alt1-4, AM1-17-03-alt1-5, AM1-17-03-alt1-6, AM1-17-03-alt1-6-part2, AM1-17-03-alt1-6-part3, AM1-17-03-alt1-6-part4, AM1-17-03-alt1-6-part5, AM1-17-03-alt1-6-part6, AM1-17-03-alt1-6-part7, AM1-17-03-alt1-6-part8, AM1-17-03-alt1-6-part9, AM1-17-03-alt1-6-part10, AM1-17-03-alt1-6-part11, AM1-17-03-alt2-1, AM1-17-03-alt2-2, AM1-17-03-alt2-3, AM1-17-03-alt2-3-part2, AM1-17-03-alt2-3-part3, AM1-17-03-alt2-4, AM1-17-03-alt2-4-part2, AM1-17-03-alt2-5, AM1-17-03-alt2-5-part2
  * Participants 7: Caller=Caller -> qbrain.libs.client; API=API -> qbrain.server.api; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db; W=W -> qbrain.server.am1HvWorkerHost; AD=AD -> qbrain.server.am1ReviewerAdapters; TG=TG -> qbrain.server.triage.
  * Source 10 messages, 12 replies, 0 self messages, 2 notes, 2 blocks with 11 branches; views carry 44 steps of 44 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 7 continuation steps of long self steps); 11 branch views for 11 branches.
* F71, line 1955, sequence diagram (section 17.4 SubmitHvRequestCommand from the plugin verb)
  * Views (27): AM1-17-04, AM1-17-04-part2, AM1-17-04-alt1-1, AM1-17-04-alt1-2, AM1-17-04-alt1-2-part2, AM1-17-04-alt2-1, AM1-17-04-alt2-2, AM1-17-04-alt2-2-part2, AM1-17-04-alt2-2-part3, AM1-17-04-alt2-2-part4, AM1-17-04-alt3-1, AM1-17-04-alt3-2, AM1-17-04-alt3-3, AM1-17-04-alt3-3-part2, AM1-17-04-alt3-4, AM1-17-04-alt3-4-part2, AM1-17-04-alt3-5, AM1-17-04-alt3-5-part2, AM1-17-04-alt3-5-part3, AM1-17-04-alt3-6, AM1-17-04-alt3-7, AM1-17-04-alt3-8, AM1-17-04-alt3-8-part2, AM1-17-04-alt3-9, AM1-17-04-alt3-9-part2, AM1-17-04-alt3-9-part3, AM1-17-04-alt3-9-part4
  * Participants 7: Model=Model -> strHostModel; Plugin=Plugin -> qbrain.plugins.pluginPs; API=API -> qbrain.server.api; Pipe=Pipe -> qbrain.server.strWorkspaceResolutionBehavior; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db; W=W -> qbrain.server.am1HvWorkerHost.
  * Source 6 messages, 15 replies, 3 self messages, 1 notes, 3 blocks with 13 branches; views carry 44 steps of 44 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 3 continuation steps of long self steps); 13 branch views for 13 branches.
* F72, line 2009, sequence diagram (section 17.5 GetHvRequestQuery and QueryHvRequestsQuery)
  * Views (13): AM1-17-05, AM1-17-05-part2, AM1-17-05-alt1-1, AM1-17-05-alt1-2, AM1-17-05-alt1-2-part2, AM1-17-05-alt2-1, AM1-17-05-alt2-2, AM1-17-05-alt2-3, AM1-17-05-alt2-4, AM1-17-05-alt1-3, AM1-17-05-alt1-3-part2, AM1-17-05-alt1-4, AM1-17-05-alt1-4-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; API=API -> qbrain.server.api; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db.
  * Source 4 messages, 7 replies, 0 self messages, 0 notes, 2 blocks with 8 branches; views carry 22 steps of 22 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 8 branch views for 8 branches.
* F73, line 2040, sequence diagram (section 17.6 CancelHvRequestCommand)
  * Views (17): AM1-17-06, AM1-17-06-part2, AM1-17-06-part3, AM1-17-06-alt1-1, AM1-17-06-alt1-2, AM1-17-06-alt1-3, AM1-17-06-alt1-4, AM1-17-06-alt1-5, AM1-17-06-alt1-5-part2, AM1-17-06-alt1-6, AM1-17-06-alt1-6-part2, AM1-17-06-alt1-6-part3, AM1-17-06-alt1-6-part4, AM1-17-06-alt1-6-part5, AM1-17-06-alt2-1, AM1-17-06-alt2-2, AM1-17-06-alt2-2-part2
  * Participants 6: Caller=Caller -> qbrain.libs.client; API=API -> qbrain.server.api; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db; W=W -> qbrain.server.am1HvWorkerHost; AD=AD -> qbrain.server.am1ReviewerAdapters.
  * Source 6 messages, 11 replies, 0 self messages, 1 notes, 2 blocks with 8 branches; views carry 29 steps of 29 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 8 branch views for 8 branches.
* F74, line 2080, sequence diagram (section 17.7 A review attempt, from claim to finalization)
  * Views (46): AM1-17-07, AM1-17-07-part2, AM1-17-07-alt1-1, AM1-17-07-alt1-2, AM1-17-07-alt1-2-part2, AM1-17-07-alt1-2-part3, AM1-17-07-alt2-1, AM1-17-07-alt2-1-part2, AM1-17-07-alt2-1-part3, AM1-17-07-alt2-2, AM1-17-07-alt2-3, AM1-17-07-alt2-3-part2, AM1-17-07-alt3-1, AM1-17-07-alt3-1-part2, AM1-17-07-alt3-1-part3, AM1-17-07-alt3-1-part4, AM1-17-07-alt3-1-part5, AM1-17-07-alt3-1-part6, AM1-17-07-alt3-1-part7, AM1-17-07-alt3-2, AM1-17-07-alt4-1, AM1-17-07-alt4-1-part2, AM1-17-07-alt4-2, AM1-17-07-alt4-2-part2, AM1-17-07-alt4-3, AM1-17-07-alt4-3-part2, AM1-17-07-alt4-3-part3, AM1-17-07-alt4-3-part4, AM1-17-07-alt4-3-part5, AM1-17-07-alt4-3-part6, AM1-17-07-alt4-3-part7, AM1-17-07-alt5-1, AM1-17-07-alt5-1-part2, AM1-17-07-alt5-2, AM1-17-07-alt5-2-part2, AM1-17-07-alt5-3, AM1-17-07-alt5-3-part2, AM1-17-07-alt5-3-part3, AM1-17-07-alt5-4, AM1-17-07-alt5-5, AM1-17-07-alt6-1, AM1-17-07-alt6-2, AM1-17-07-alt6-2-part2, AM1-17-07-alt6-3, AM1-17-07-alt6-4, AM1-17-07-alt6-5
  * Participants 4: W=W -> qbrain.server.am1HvWorkerHost; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db; AD=AD -> qbrain.server.am1ReviewerAdapters.
  * Source 18 messages, 10 replies, 0 self messages, 12 notes, 6 blocks with 20 branches; views carry 77 steps of 77 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 11 continuation steps of long self steps); 20 branch views for 20 branches.
* F75, line 2156, sequence diagram (section Nested sequence: StreamOneAttempt (the live part of the review attempt))
  * Views (23): StreamOneAttempt, StreamOneAttempt-part2, StreamOneAttempt-loop1, StreamOneAttempt-loop1-part2, StreamOneAttempt-loop1-part3, StreamOneAttempt-alt1-1, StreamOneAttempt-alt1-1-part2, StreamOneAttempt-opt1, StreamOneAttempt-opt1-part2, StreamOneAttempt-opt1-part3, StreamOneAttempt-alt1-2, StreamOneAttempt-alt1-2-part2, StreamOneAttempt-alt2-1, StreamOneAttempt-alt2-1-part2, StreamOneAttempt-alt2-2, StreamOneAttempt-alt2-3, StreamOneAttempt-alt2-4, StreamOneAttempt-alt2-4-part2, StreamOneAttempt-alt1-3, StreamOneAttempt-alt3-1, StreamOneAttempt-alt3-2, StreamOneAttempt-alt3-3, StreamOneAttempt-alt3-4
  * Participants 4: W=W -> qbrain.server.am1HvWorkerHost; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db; AD=AD -> qbrain.server.am1ReviewerAdapters.
  * Source 6 messages, 9 replies, 1 self messages, 1 notes, 5 blocks with 13 branches; views carry 40 steps of 40 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 5 continuation steps of long self steps); 13 branch views for 13 branches.
* F76, line 2201, sequence diagram (section Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt))
  * Views (28): FinalizeOneRequest, FinalizeOneRequest-alt1-1, FinalizeOneRequest-alt1-2, FinalizeOneRequest-alt1-3, FinalizeOneRequest-alt1-3-part2, FinalizeOneRequest-alt2-1, FinalizeOneRequest-alt2-2, FinalizeOneRequest-alt2-2-part2, FinalizeOneRequest-alt2-2-part3, FinalizeOneRequest-alt3-1, FinalizeOneRequest-alt3-1-part2, FinalizeOneRequest-alt3-1-part3, FinalizeOneRequest-alt4-1, FinalizeOneRequest-alt4-2, FinalizeOneRequest-alt4-2-part2, FinalizeOneRequest-alt4-3, FinalizeOneRequest-alt3-2, FinalizeOneRequest-alt3-2-part2, FinalizeOneRequest-opt1, FinalizeOneRequest-opt1-part2, FinalizeOneRequest-alt5-1, FinalizeOneRequest-alt5-2, FinalizeOneRequest-alt5-3, FinalizeOneRequest-alt5-3-part2, FinalizeOneRequest-alt5-3-part3, FinalizeOneRequest-alt5-4, FinalizeOneRequest-alt5-5, FinalizeOneRequest-alt5-5-part2
  * Participants 3: W=W -> qbrain.server.am1HvWorkerHost; H=H -> qbrain.server.am1HvFinalizer; DB=DB -> qbrain.db.
  * Source 4 messages, 12 replies, 4 self messages, 0 notes, 6 blocks with 16 branches; views carry 45 steps of 45 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 3 continuation steps of long self steps); 16 branch views for 16 branches.
* F77, line 2252, sequence diagram (section 17.8 An execution failure, its incident, and the reviewer going offline)
  * Views (22): AM1-17-08, AM1-17-08-part2, AM1-17-08-alt1-1, AM1-17-08-alt1-2, AM1-17-08-alt1-2-part2, AM1-17-08-alt1-2-part3, AM1-17-08-alt1-3, AM1-17-08-alt1-3-part2, AM1-17-08-alt1-3-part3, AM1-17-08-alt1-3-part4, AM1-17-08-alt1-3-part5, AM1-17-08-alt1-3-part6, AM1-17-08-alt1-3-part7, AM1-17-08-alt2-1, AM1-17-08-alt2-2, AM1-17-08-alt2-3, AM1-17-08-alt3-1, AM1-17-08-alt3-2, AM1-17-08-alt4-1, AM1-17-08-alt4-1-part2, AM1-17-08-alt4-2, AM1-17-08-alt4-2-part2
  * Participants 5: W=W -> qbrain.server.am1HvWorkerHost; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db; TG=TG -> qbrain.server.triage; Caller=Caller -> qbrain.libs.client.
  * Source 8 messages, 10 replies, 0 self messages, 2 notes, 4 blocks with 10 branches; views carry 37 steps of 37 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 3 continuation steps of long self steps); 10 branch views for 10 branches.
* F78, line 2297, sequence diagram (section 17.9 Recovery after a worker crash)
  * Views (16): AM1-17-09, AM1-17-09-part2, AM1-17-09-part3, AM1-17-09-part4, AM1-17-09-part5, AM1-17-09-part6, AM1-17-09-alt1-1, AM1-17-09-alt1-1-part2, AM1-17-09-alt1-2, AM1-17-09-alt1-2-part2, AM1-17-09-alt2-1, AM1-17-09-alt2-2, AM1-17-09-alt2-3, AM1-17-09-alt2-4, AM1-17-09-opt1, AM1-17-09-opt1-part2
  * Participants 4: W1=Worker A, crashed -> qbrain.server.am1HvWorkerHost; W2=Worker B -> qbrain.server.am1HvWorkerHost; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db.
  * Source 6 messages, 7 replies, 1 self messages, 1 notes, 3 blocks with 7 branches; views carry 28 steps of 28 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 3 continuation steps of long self steps); 7 branch views for 7 branches.
* F79, line 2332, sequence diagram (section 17.10 GetHvResultQuery and GetHvEvidenceQuery)
  * Views (13): AM1-17-10, AM1-17-10-part2, AM1-17-10-alt1-1, AM1-17-10-alt1-2, AM1-17-10-alt1-3, AM1-17-10-alt1-4, AM1-17-10-alt1-5, AM1-17-10-alt1-6, AM1-17-10-alt1-7, AM1-17-10-alt1-7-part2, AM1-17-10-alt2-1, AM1-17-10-alt2-2, AM1-17-10-alt2-3
  * Participants 4: Caller=Caller -> qbrain.libs.client; API=API -> qbrain.server.api; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db.
  * Source 3 messages, 10 replies, 0 self messages, 0 notes, 2 blocks with 10 branches; views carry 25 steps of 25 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 10 branch views for 10 branches.
* F80, line 2367, sequence diagram (section 17.11 RemoveHvEvidenceCommand)
  * Views (8): AM1-17-11, AM1-17-11-part2, AM1-17-11-alt1-1, AM1-17-11-alt1-2, AM1-17-11-alt1-3, AM1-17-11-alt1-4, AM1-17-11-alt1-5, AM1-17-11-alt1-5-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; API=API -> qbrain.server.api; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db.
  * Source 3 messages, 5 replies, 0 self messages, 0 notes, 1 blocks with 5 branches; views carry 14 steps of 14 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 5 branch views for 5 branches.
* F81, line 2391, sequence diagram (section 17.12 StreamHvRunOutputQuery with a reconnect)
  * Views (17): AM1-17-12, AM1-17-12-part2, AM1-17-12-alt1-1, AM1-17-12-alt1-2, AM1-17-12-alt1-3, AM1-17-12-alt1-4, AM1-17-12-alt1-5, AM1-17-12-alt1-5-part2, AM1-17-12-alt1-5-part3, AM1-17-12-alt1-5-part4, AM1-17-12-alt1-5-part5, AM1-17-12-alt1-5-part6, AM1-17-12-alt1-5-part7, AM1-17-12-alt1-5-part8, AM1-17-12-alt1-5-part9, AM1-17-12-alt2-1, AM1-17-12-alt2-2
  * Participants 5: Caller=Caller -> qbrain.libs.client; API=API -> qbrain.server.api; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db; W=W -> qbrain.server.am1HvWorkerHost.
  * Source 8 messages, 9 replies, 0 self messages, 1 notes, 2 blocks with 7 branches; views carry 27 steps of 27 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 7 branch views for 7 branches.
* F82, line 2429, sequence diagram (section 17.13 StreamHvNotificationsQuery through the plugin watcher)
  * Views (10): AM1-17-13, AM1-17-13-part2, AM1-17-13-alt1-1, AM1-17-13-alt1-2, AM1-17-13-alt1-2-part2, AM1-17-13-alt1-2-part3, AM1-17-13-alt1-2-part4, AM1-17-13-alt1-2-part5, AM1-17-13-alt1-2-part6, AM1-17-13-alt1-2-part7
  * Participants 5: Watcher=Plugin watcher -> qbrain.plugins.pluginPs; API=API -> qbrain.server.api; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db; Hook=Prompt hook -> qbrain.plugins.pluginHosts.
  * Source 5 messages, 3 replies, 2 self messages, 1 notes, 1 blocks with 2 branches; views carry 16 steps of 16 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 2 branch views for 2 branches.
* F83, line 2454, sequence diagram (section 17.14 QueryHvNotificationsQuery and QueryHvIncidentsQuery)
  * Views (5): AM1-17-14, AM1-17-14-part2, AM1-17-14-alt1-1, AM1-17-14-alt1-2, AM1-17-14-alt1-2-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; API=API -> qbrain.server.api; H=H -> qbrain.server.am1HvHandlers; DB=DB -> qbrain.db.
  * Source 3 messages, 2 replies, 0 self messages, 0 notes, 1 blocks with 2 branches; views carry 8 steps of 8 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 2 branch views for 2 branches.
* F84, line 2472, sequence diagram (section 17.15 A write to a frozen turn, and a hostile review from an agent surface)
  * Views (11): AM1-17-15, AM1-17-15-part2, AM1-17-15-part3, AM1-17-15-part4, AM1-17-15-part5, AM1-17-15-part6, AM1-17-15-part7, AM1-17-15-part8, AM1-17-15-part9, AM1-17-15-alt1-1, AM1-17-15-alt1-2
  * Participants 5: Model=Model -> strHostModel; Plugin=Plugin -> qbrain.plugins.pluginPs; API=API -> qbrain.server.api; H=H -> qbrain.server.strSessionLogHandlers; DB=DB -> qbrain.db.
  * Source 7 messages, 5 replies, 0 self messages, 0 notes, 1 blocks with 2 branches; views carry 15 steps of 15 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 2 branch views for 2 branches.
* F85, line 2502, use-case flowchart (section 18.1 Overview)
  * Views (10): AM1-18-01, AM1-18-01-part2, AM1-18-01-part3, AM1-18-01-part4, AM1-18-01-part5, AM1-18-01-part6, AM1-18-01-part7, AM1-18-01-part8, AM1-18-01-part9, AM1-18-01-part10
  * Source 18 nodes and 18 edges; drawn on 18 model elements (a node that names an existing element or a use case drawn twice is one element) with 18 element edges; views show 18 of 18 elements and 18 of 18 element edges.
* F86, line 2546, use-case flowchart (section 18.2 UC-HV-01 Manage the reviewer pool [FR-MCP-HVPIPE-001])
  * Views (3): UC-HV-01, UC-HV-01-part2, UC-HV-01-part3
  * Source 7 nodes and 6 edges; drawn on 7 model elements (a node that names an existing element or a use case drawn twice is one element) with 6 element edges; views show 7 of 7 elements and 6 of 6 element edges.
* F87, line 2560, use-case flowchart (section 18.3 UC-HV-02 NONCE a reviewer [FR-MCP-HVPIPE-003])
  * Views (7): UC-HV-02, UC-HV-02-part2, UC-HV-02-part3, UC-HV-02-part4, UC-HV-02-part5, UC-HV-02-part6, UC-HV-02-part7
  * Source 13 nodes and 12 edges; drawn on 14 model elements (a node that names an existing element or a use case drawn twice is one element) with 13 element edges; views show 14 of 14 elements and 13 of 13 element edges.
* F88, line 2580, use-case flowchart (section 18.4 UC-HV-03 Submit an HV request [FR-MCP-HVPIPE-002, -004, -005, -006, -013, -018])
  * Views (7): UC-HV-03, UC-HV-03-part2, UC-HV-03-part3, UC-HV-03-part4, UC-HV-03-part5, UC-HV-03-part6, UC-HV-03-part7
  * Source 14 nodes and 13 edges; drawn on 14 model elements (a node that names an existing element or a use case drawn twice is one element) with 13 element edges; views show 14 of 14 elements and 13 of 13 element edges.
* F89, line 2601, use-case flowchart (section 18.5 UC-HV-04 Run a review live [FR-MCP-HVPIPE-007, -008])
  * Views (6): UC-HV-04, UC-HV-04-part2, UC-HV-04-part3, UC-HV-04-part4, UC-HV-04-part5, UC-HV-04-part6
  * Source 13 nodes and 12 edges; drawn on 13 model elements (a node that names an existing element or a use case drawn twice is one element) with 12 element edges; views show 13 of 13 elements and 12 of 12 element edges.
* F90, line 2621, use-case flowchart (section 18.6 UC-HV-05 Finalize the HV result [FR-MCP-HVPIPE-010, -011, -012, -014, -021])
  * Views (5): UC-HV-05, UC-HV-05-part2, UC-HV-05-part3, UC-HV-05-part4, UC-HV-05-part5
  * Source 11 nodes and 10 edges; drawn on 11 model elements (a node that names an existing element or a use case drawn twice is one element) with 10 element edges; views show 11 of 11 elements and 10 of 10 element edges.
* F91, line 2639, use-case flowchart (section 18.7 UC-HV-06 Watch an HV live [FR-MCP-HVPIPE-015, -016])
  * Views (4): UC-HV-06, UC-HV-06-part2, UC-HV-06-part3, UC-HV-06-part4
  * Source 8 nodes and 7 edges; drawn on 8 model elements (a node that names an existing element or a use case drawn twice is one element) with 7 element edges; views show 8 of 8 elements and 7 of 7 element edges.
* F92, line 2654, use-case flowchart (section 18.8 UC-HV-07 Receive completion notifications [FR-MCP-HVPIPE-017, -020])
  * Views (5): UC-HV-07, UC-HV-07-part2, UC-HV-07-part3, UC-HV-07-part4, UC-HV-07-part5
  * Source 8 nodes and 7 edges; drawn on 10 model elements (a node that names an existing element or a use case drawn twice is one element) with 9 element edges; views show 10 of 10 elements and 9 of 9 element edges.
* F93, line 2669, use-case flowchart (section 18.9 UC-HV-08 Cancel an HV [FR-MCP-HVPIPE-008])
  * Views (4): UC-HV-08, UC-HV-08-part2, UC-HV-08-part3, UC-HV-08-part4
  * Source 8 nodes and 7 edges; drawn on 8 model elements (a node that names an existing element or a use case drawn twice is one element) with 7 element edges; views show 8 of 8 elements and 7 of 7 element edges.
* F94, line 2684, use-case flowchart (section 18.10 UC-HV-09 Recover from failures [FR-MCP-HVPIPE-007, -009])
  * Views (5): UC-HV-09, UC-HV-09-part2, UC-HV-09-part3, UC-HV-09-part4, UC-HV-09-part5
  * Source 11 nodes and 10 edges; drawn on 11 model elements (a node that names an existing element or a use case drawn twice is one element) with 10 element edges; views show 11 of 11 elements and 10 of 10 element edges.
* F95, line 2702, use-case flowchart (section 18.11 UC-HV-10 Retrieve the result and evidence [FR-MCP-HVPIPE-011, -015])
  * Views (4): UC-HV-10, UC-HV-10-part2, UC-HV-10-part3, UC-HV-10-part4
  * Source 8 nodes and 7 edges; drawn on 8 model elements (a node that names an existing element or a use case drawn twice is one element) with 7 element edges; views show 8 of 8 elements and 7 of 7 element edges.
* F96, line 2717, use-case flowchart (section 18.12 UC-HV-11 Remove evidence on instruction [FR-MCP-HVPIPE-019])
  * Views (2): UC-HV-11, UC-HV-11-part2
  * Source 5 nodes and 4 edges; drawn on 5 model elements (a node that names an existing element or a use case drawn twice is one element) with 4 element edges; views show 5 of 5 elements and 4 of 4 element edges.
* F97, line 2729, use-case flowchart (section 18.13 UC-HV-12 Remediate after a reviewed turn [FR-MCP-HVPIPE-012, -014])
  * Views (4): UC-HV-12, UC-HV-12-part2, UC-HV-12-part3, UC-HV-12-part4
  * Source 8 nodes and 7 edges; drawn on 8 model elements (a node that names an existing element or a use case drawn twice is one element) with 7 element edges; views show 8 of 8 elements and 7 of 7 element edges.
* F98, line 2957, delivery-order flowchart (section 20.1 Phases)
  * Views (4): AM1-20-01, AM1-20-01-part2, AM1-20-01-part3, AM1-20-01-part4
  * Source 12 nodes and 11 edges; model 9 BoundedContext elements and 3 Activity actions with 11 relationships; views show 12 of 12 nodes and 11 of 11 edges.
* F99, line 3071, ER diagram (section B.1 References between the groups)
  * Views (5): AM1-B-01, AM1-B-01-part2, AM1-B-01-part3, AM1-B-01-part4, AM1-B-01-part5
  * Source 10 entities and 8 relationships (0 reuse an as-built EF relationship); views show 10 of 10 entities and 8 of 8 relationships.
  * Entities: SessionLog -> entSessionLog; HvRequest -> am1EntHvRequest; SessionLogTurn -> entSessionLogTurn; HvRequestTurn -> am1EntHvRequestTurn; HvRun -> am1EntHvRun; Requirement -> entRequirement; HvRequestRequirementRef -> am1EntHvRequestRequirementRef; TriageReport -> entTriageReport; HvIncident -> am1EntHvIncident; HvReviewer -> am1EntHvReviewer.
* F100, line 3087, ER diagram (section B.2 Reviewer pool)
  * Views (1): AM1-B-02
  * Source 2 entities and 1 relationships (0 reuse an as-built EF relationship); views show 2 of 2 entities and 1 of 1 relationships.
  * Entities: HvWorkspaceState -> am1EntHvWorkspaceState; HvReviewer -> am1EntHvReviewer.
* F101, line 3109, ER diagram (section B.3 Requests and membership)
  * Views (2): AM1-B-03, AM1-B-03-part2
  * Source 5 entities and 4 relationships (0 reuse an as-built EF relationship); views show 5 of 5 entities and 4 of 4 relationships.
  * Entities: HvRequest -> am1EntHvRequest; HvRequestTurn -> am1EntHvRequestTurn; HvRequestRequirementRef -> am1EntHvRequestRequirementRef; HvRequestResponse -> am1EntHvRequestResponse; AgentResponse -> strEntAgentResponse.
* F102, line 3161, ER diagram (section B.4 Runs and output)
  * Views (1): AM1-B-04
  * Source 3 entities and 2 relationships (0 reuse an as-built EF relationship); views show 3 of 3 entities and 2 of 2 relationships.
  * Entities: HvRun -> am1EntHvRun; HvRunOutputChunk -> am1EntHvRunOutputChunk; HvRequest -> am1EntHvRequest.
* F103, line 3195, ER diagram (section B.5 Results and findings)
  * Views (1): AM1-B-05
  * Source 3 entities and 2 relationships (0 reuse an as-built EF relationship); views show 3 of 3 entities and 2 of 2 relationships.
  * Entities: HvTurnAssessment -> am1EntHvTurnAssessment; HvFinding -> am1EntHvFinding; HvRequest -> am1EntHvRequest.
* F104, line 3227, ER diagram (section B.6 Evidence)
  * Views (1): AM1-B-06
  * Source 2 entities and 1 relationships (0 reuse an as-built EF relationship); views show 2 of 2 entities and 1 of 1 relationships.
  * Entities: HvEvidence -> am1EntHvEvidence; HvRequest -> am1EntHvRequest.
* F105, line 3247, ER diagram (section B.7 Notifications and incidents)
  * Views (1): AM1-B-07
  * Source 3 entities and 2 relationships (0 reuse an as-built EF relationship); views show 3 of 3 entities and 2 of 2 relationships.
  * Entities: HvNotification -> am1EntHvNotification; HvIncident -> am1EntHvIncident; HvRun -> am1EntHvRun.
* F106, line 3278, ER diagram (section B.8 Changed foundation entities)
  * Views (1): AM1-B-08
  * Source 3 entities and 2 relationships (0 reuse an as-built EF relationship); views show 3 of 3 entities and 2 of 2 relationships.
  * Entities: SessionLogTurn -> entSessionLogTurn; AgentResponseGrade -> strEntAgentResponseGrade; HvRequest -> am1EntHvRequest.
* F107, line 3303, ER diagram (section B.9 HostileReview worker storage (section 16.7))
  * Views (2): AM1-B-09, AM1-B-09-part2
  * Source 4 entities and 4 relationships (2 reuse an as-built EF relationship); views show 4 of 4 entities and 4 of 4 relationships.
  * Entities: HostileReviewRequest -> entHostileReviewRequest; HostileReviewAttempt -> am1EntHostileReviewAttempt; HostileReviewExecution -> entHostileReviewExecution; HostileReviewDiagnostic -> entHostileReviewDiagnostic.

## Render inspection

* Batch3 renders (20261019T203924-p4-am1-batch3.log, 703 views): all 79 contact sheets (SCRATCH/p4-am1/sheets/sheet-001.png to sheet-079.png, 9 views each, index in sheets/index.txt) were read as images, so every view was looked at once. Checked on each: every name, description and label legible at the sheet scale; no text outside its shape; no edge label over a node; the part shows the nodes and edges its fence draws in that region; terminal shapes (End grey, Error red) as the fence means them; participant names right for the sequence.
* Findings from that pass and what was done: a view-key qualifier placed after a plain word break read as part of the phrase ("Request in the (RemoveHvEvidenceCommand)" over "workspace?", sheet 12), fixed in the converter; ClaimHvRunCommand-part5 equalled part9 and RoundRobinAssignment-part3 equalled part4, duplicates dropped; RunReviewerProcess-part5 crowded its stop labels over the StreamReviewerOutput call (sheet 28), F55 moved to four nodes per part; AM1-18-01-part3 put two "participates in" labels on one crossing (sheet 70), the overview now takes three elements per part.
* Accepted as legible after inspection: self-step loops of activity parts that pass beside a neighbour (HostileReviewWorkerHost-part4, RunHostileReviewAttempt-part6, StreamReviewerOutput-part3); section 4 labels that touch a boundary name in AM1-04-01-part6, part10 and part11; sequence notes split over several self steps ("(1 of 3)" and so on), each still readable.
* Re-inspection after the last converter changes: diff-renders.ps1 (21:16:48 local) compared the batch5 renders with the inspected batch3 copies after masking the renderer's running node numbers (j_ and v- ids, which shift for every view rendered after a changed one; svg-first-diff.ps1 on AM1-B-02 showed only those numbers differing): 663 views unchanged, 39 changed, 5 new. Those 44 (ClaimHvRunCommand with its parts 2 to 4 and 6 to 11, HvScopeResolution-part3, RemoveHvEvidenceCommand-part2 and part3, RoundRobinAssignment with its parts 2, 4 and 5, all 17 parts of RunReviewerProcess and all 10 parts of AM1-18-01) were read as images on sheets b5-changed1.png to b5-changed5.png, and 9 more views on b4-check1.png (StreamHvRunOutputQuery, RemoveHvEvidenceCommand, GetHvResultQuery among them). Every one legible, no covered label, no text outside a shape, the qualified decision "Request in the workspace? (RemoveHvEvidenceCommand)" inside its diamond.
* Measured checks on the final renders (batch5): svg-label-overlap.ps1 found 0 labels covering element text of 3036 labels checked (batch1 167, batch2 18, batch3 3, batch4 1); svg-text-fit.ps1 found 73 overflowing element instances of 2644, 68 of them the as-built REPL Host and MCP Database (existing elements) and 5 converted nodes by 4 to 28 units (AM1-20-01, InsertAdmittedRequest-part2, CommitFinalResult, RunHostileReviewAttempt-part7 in two views), all legible on the sheets.

## Judgment calls

* HV components (Planned, D3): grep-repo.ps1 over src, tests and plugins of the worktree (21:13 local) found 0 hits for HvRequest, HvRun, HvWorkerHost, IHvReviewerAdapter, HostileValidationController, HvReviewer, SubmitHvRequestCommand, FinalizeHvRequestCommand and HostileReviewAttempt, so the HostileValidation handlers, the finalization handler, the worker host and the reviewer adapters are Planned components of the MCP Context Server. FinalizeHvRequestCommand is its own component (HV Finalization Handler) because section 4 draws it apart (FIN) and it alone dispatches the foundation units; the other units share Hostile Validation Handlers. In F78 (17.9) "FinalizeHvRequestCommand for each Finalizing request" stays on the handlers, because that fence's H is the handlers participant.
* Change Event Bus is as built: IChangeEventBus (src/QBrainAi.Services/Notifications/IChangeEventBus.cs line 4) and ChannelChangeEventBus (ChannelChangeEventBus.cs line 13) exist in QBrainAi.Services, Program.cs line 321 registers it, and EventStreamController streams it (line 31), so the element carries no Planned tag and two as-built relationships record where it lives. It was not in the as-built c4 views before; the implementer may prefer it in the as-built model fragment.
* The section 19.1 work packages of the delivery order are BoundedContext elements (HvStorageSchema context and so on, Planned), because section 20.1 orders bounded contexts; the foundation step, H0 and the closing validation are Activity actions of that diagram only.
* The shared components of section 11 (FencedLease and the rest) and HostileValidationOptions are not model elements: the fences draw them as call nodes and message text inside the units, and a class-level element would duplicate those diagrams.
* Use-case actors: an actor label that names two parties maps to both elements ("Operator or agent" to developer and agent, "Plugin watcher and model" to lib-ps and the host model, "Director or any client" to Director and QBrainAi.Client); "Model, through its plugin" is the host model.
* Terminal shapes: 18 exits were classed by reading their text (TerminalKinds in am1-config.psd1): an exit that names a refusal or failure code is Error, a normal result of the unit is End.
* Text-fit residuals accepted: five converted nodes run 4 to 28 units over their 220-unit shape (svg-text-fit.ps1), the largest the foundation node of AM1-20-01, whose name starts with the unbreakable PLAN-SESSIONTURNREVIEW-001; all stay legible. The other overflowing instances are the as-built REPL Host and MCP Database descriptions, existing elements this conversion does not own.
* Notation prose left unchanged, as the brief requires (lines at b457ac22, then in the converted document): line 10 (10) describes Mermaid flowcharts and double-bordered call nodes; line 1837 (2014) names the sequence participants Caller, API, Pipe, H, DB, W, AD and TG, which the views show by element name; line 2498 (3272) describes stadium actors and oval use cases; line 3065 (3916) and line 3301 (4004) describe solid and dotted ER lines, which the views carry as the technology "EF Core foreign key" or "Logical reference". The operator may want these sentences reworded for the C4 views.

## Layout adjustments and calibration receipts

* No style edit: workspace.dsl changes by exactly the two include lines of the brief; every layout choice is a view's autolayout line.
* Limits and layout rules start from the SessionTurn calibration (SCRATCH/p4-str/str-config.psd1). AM1 additions, each from a receipt: static3 (tb 300 100) for the section 4 structure (batch1 label of lib-ps to REPL Host on the Agent Plugin Core boundary name); delivery (a chain of at most four, batch1 PageOverflow 550 x 1863 and 550 x 2000); customLong (four nodes, wider separations) for F31, F46, F54, F55, F58 and F62 (batch2 covering labels 118.9 units and less); one more rank for a part whose two nodes link both ways (batch1 HvWorkerHost-part4 rendered 1634 x 1604); staticUc (three elements) for the overview F85 (batch3 covering label 31.3 units).
* Per-view layouts: AM1-07-03-part2 tb 250 500 (batch2 transition labels over the Online state); AM1-04-01 lr 300 100 (batch2 label on the boundary name); RunReviewerProcess-part5, part7 and part14 tb 200 250 (batch4 PageOverflow 1598 x 1362 and 1504 x 1362); HvScopeResolution-part3 tb 100 200 (batch3 and batch4 covering label 9.6 units).
* Batch receipts (docs/receipts/c4model): 20261019T202057-p4-am1-batch1.log, PageOverflow 6, covering labels 167, overflowing text instances 412; 20261019T203141-p4-am1-batch2.log, PageOverflow 0, covering labels 18, text 73; 20261019T203924-p4-am1-batch3.log, 703 views, covering labels 3 of 3046; 20261019T205952-p4-am1-batch4.log, a -WhatIf dry run (WHATIF render lines, nothing written); 20261019T210111-p4-am1-batch4-run.log, 707 views, PageOverflow 3, covering labels 1 of 3036; 20261019T210958-p4-am1-batch5-whatif.log, dry run; 20261019T211034-p4-am1-batch5.log, 707 views, every finding 0 except the 109 Stateless fences, covering labels 0 of 3036.

## Gate G4 receipts

* Dry run first: 20261019T212224-p4-am1-g4-whatif.log (21:22:24 to 21:22:45 local, ./build.ps1 ArchitectureDiagrams -WhatIf, WHATIF render lines, nothing written): every count 0 except MermaidFence 109.
* Run 1: 20261019T212300-p4-am1-g4-r1.log (21:23:00 to 21:27:20 local). diagram-findings.ps1: ValidationFailed 0, InspectionErrors 0, RenderFailed 0, MermaidFile 0, DanglingViewLink 0, MissingRender 0, OrphanRender 0, TextSizeSpread 0, PageOverflow 0, InvalidSvg 0, render lines 2384, SVG files 2384; MermaidFence 109, every one in docs/Stateless-Agent-Context-Management.md (the other stage 2 document; a search of the log for Mermaid findings in any other file: 0 hits). ExitCode 255 comes from those 109 fences only.
* Manifest after run 1: hash-dir.ps1 over docs/architecture/c4/rendered (21:27:30 local), 2384 files, SCRATCH/p4-am1/g4-r1-manifest.txt.
* Run 2: 20261019T212739-p4-am1-g4-r2.log (21:27:39 to 21:31:39 local): the same counts, MermaidFence 109 again only in the Stateless document. hash-dir.ps1 -Compare (21:31:53 local): Identical 2384 of 2384, no file added, changed or removed. Gate G4 holds for this document.
* Tracked renders against the base commit (normalize-compare.ps1, 21:32:01 local, JointJS ids renumbered per file): of the 259 renders b457ac22 tracks, 250 differ only in renumbered ids and 9 in content, all marketing views (MKT-ARCHITECTURE-01 and its parts 2 to 7, MKT-UITOOLING-01 and its part 2). svg-text-diff.ps1 shows the one added text in each: the label "Runs its own steps (sequence self-messages and notes)" [Internal step], a planned self relationship of agent, qbrain.server.api or qbrain.server that the SessionTurn fragment defines (str.dsl lines 3227, 3252 and 3256); the AM1 fragment defines no self relationship on those elements, and the later commit 463ffbe4 makes the marketing views exclude Planned edges. The as-built views exclude Planned elements and relationships (c4.dsl), so the AM1 planned self steps on qbrain.db, triage and the host adapters do not reach them.
* Measured beyond the gate on the same renders: svg-label-overlap.ps1 0 covering labels of 3036; svg-text-fit.ps1 73 instances over their shape, as listed under Render inspection.
