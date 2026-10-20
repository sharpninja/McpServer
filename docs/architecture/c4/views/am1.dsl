# AM1 views (PLAN-C4MODEL-001 P4, D5 to D7): one or more views per Mermaid fence of
# docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md, split to the AC-5 letter page.
# Converted once by SCRATCH/p4-am1/convert-am1.ps1 (D9); edit here from now on.
custom "AM1-07-01" "HV request lifecycle (state diagram), part 1 of 7" "State diagram converted from fence F2 (line 196)." {
    include am1AM10701_Start am1AM10701_Queued am1AM10701_Running
    autolayout tb 200 300
}
custom "AM1-07-01-part2" "HV request lifecycle (state diagram), part 2 of 7" "State diagram converted from fence F2 (line 196)." {
    include am1AM10701_Queued am1AM10701_Canceled am1AM10701_CancelRequested
    autolayout lr 300 300
}
custom "AM1-07-01-part3" "HV request lifecycle (state diagram), part 3 of 7" "State diagram converted from fence F2 (line 196)." {
    include am1AM10701_Running am1AM10701_CancelRequested am1AM10701_Finalizing
    autolayout lr 300 300
}
custom "AM1-07-01-part4" "HV request lifecycle (state diagram), part 4 of 7" "State diagram converted from fence F2 (line 196)." {
    include am1AM10701_Finalizing am1AM10701_Completed am1AM10701_Queued
    autolayout lr 300 300
}
custom "AM1-07-01-part5" "HV request lifecycle (state diagram), part 5 of 7" "State diagram converted from fence F2 (line 196)." {
    include am1AM10701_Running am1AM10701_Failed am1AM10701_Finalizing
    autolayout tb 200 300
}
custom "AM1-07-01-part6" "HV request lifecycle (state diagram), part 6 of 7" "State diagram converted from fence F2 (line 196)." {
    include am1AM10701_Canceled am1AM10701_End am1AM10701_Failed
    autolayout lr 300 300
}
custom "AM1-07-01-part7" "HV request lifecycle (state diagram), part 7 of 7" "State diagram converted from fence F2 (line 196)." {
    include am1AM10701_Completed am1AM10701_End
    autolayout lr 300 300
}
custom "AM1-07-02" "Run attempt lifecycle (state diagram), part 1 of 8" "State diagram converted from fence F3 (line 219)." {
    include am1AM10702_Start am1AM10702_Claimed am1AM10702_Running
    exclude "am1AM10702_Running -> am1AM10702_Running"
    autolayout tb 200 300
}
custom "AM1-07-02-part2" "Run attempt lifecycle (state diagram), part 2 of 8" "State diagram converted from fence F3 (line 219)." {
    include am1AM10702_Running am1AM10702_OutputClosed am1AM10702_Failed
    exclude "am1AM10702_Running -> am1AM10702_Running"
    autolayout tb 200 300
}
custom "AM1-07-02-part3" "Run attempt lifecycle (state diagram), part 3 of 8" "State diagram converted from fence F3 (line 219)." {
    include am1AM10702_Claimed am1AM10702_Failed am1AM10702_Abandoned
    autolayout lr 300 300
}
custom "AM1-07-02-part4" "Run attempt lifecycle (state diagram), part 4 of 8" "State diagram converted from fence F3 (line 219)." {
    include am1AM10702_Running am1AM10702_Canceled am1AM10702_OutputClosed
    exclude "am1AM10702_Running -> am1AM10702_Running"
    autolayout tb 200 300
}
custom "AM1-07-02-part5" "Run attempt lifecycle (state diagram), part 5 of 8" "State diagram converted from fence F3 (line 219)." {
    include am1AM10702_Running am1AM10702_Abandoned am1AM10702_End
    exclude "am1AM10702_Running -> am1AM10702_Running"
    autolayout tb 200 300
}
custom "AM1-07-02-part6" "Run attempt lifecycle (state diagram), part 6 of 8" "State diagram converted from fence F3 (line 219)." {
    include am1AM10702_OutputClosed am1AM10702_Succeeded am1AM10702_End
    autolayout tb 200 300
}
custom "AM1-07-02-part7" "Run attempt lifecycle (state diagram), part 7 of 8" "State diagram converted from fence F3 (line 219)." {
    include am1AM10702_Failed am1AM10702_End am1AM10702_Canceled
    autolayout lr 300 300
}
custom "AM1-07-02-part8" "Run attempt lifecycle (state diagram), part 8 of 8" "State diagram converted from fence F3 (line 219)." {
    include am1AM10702_Running
    autolayout lr 300 300
}
custom "AM1-07-03" "Reviewer readiness (state diagram), part 1 of 2" "State diagram converted from fence F4 (line 243)." {
    include am1AM10703_Start am1AM10703_Unverified am1AM10703_Online
    autolayout tb 200 300
}
custom "AM1-07-03-part2" "Reviewer readiness (state diagram), part 2 of 2" "State diagram converted from fence F4 (line 243)." {
    include am1AM10703_Unverified am1AM10703_Offline am1AM10703_Online
    autolayout tb 250 500
}
custom "AM1-07-04" "HV state of a turn (state diagram), part 1 of 2" "State diagram converted from fence F5 (line 258)." {
    include am1AM10704_Start am1AM10704_None am1AM10704_Pending
    autolayout tb 200 300
}
custom "AM1-07-04-part2" "HV state of a turn (state diagram), part 2 of 2" "State diagram converted from fence F5 (line 258)." {
    include am1AM10704_Pending am1AM10704_Final am1AM10704_End
    autolayout tb 200 300
}
custom "FencedLease" "FencedLease (activity), part 1 of 6" "Activity diagram converted from fence F6 (line 333)." {
    include am1FencedLease_FencedLeaseOperationRowOwnerFence am1FencedLease_Operation
    autolayout lr 250 150
}
custom "FencedLease-part2" "FencedLease (activity), part 2 of 6" "Activity diagram converted from fence F6 (line 333)." {
    include am1FencedLease_Operation am1FencedLease_LeaseAbsentOrExpired am1FencedLease_OwnerAndFenceEqualTheStored am1FencedLease_RowStillInALiveStatus
    autolayout lr 250 150
}
custom "FencedLease-part3" "FencedLease (activity), part 3 of 6" "Activity diagram converted from fence F6 (line 333)." {
    include am1FencedLease_LeaseAbsentOrExpired am1FencedLease_HeldAnotherOwnerHoldsALive am1FencedLease_SetOwnerAndExpiryIncrementThe am1FencedLease_OwnerAndFenceEqualTheStored am1FencedLease_StaleFenceNothingChanged am1FencedLease_Operation2
    autolayout lr 200 50
}
custom "FencedLease-part4" "FencedLease (activity), part 4 of 6" "Activity diagram converted from fence F6 (line 333)." {
    include am1FencedLease_RowStillInALiveStatus am1FencedLease_SkippedRenewedSettledOrTakenOver am1FencedLease_IncrementTheFenceTokenClearOwner am1FencedLease_SetOwnerAndExpiryIncrementThe am1FencedLease_ClaimedTheNewFenceToken
    autolayout lr 250 150
}
custom "FencedLease-part5" "FencedLease (activity), part 5 of 6" "Activity diagram converted from fence F6 (line 333)." {
    include am1FencedLease_Operation2 am1FencedLease_ExtendTheExpiry am1FencedLease_GuardedTheCallerSWriteProceeds am1FencedLease_ClearOwnerAndExpiry am1FencedLease_IncrementTheFenceTokenClearOwner am1FencedLease_ExpiredTheCallerEndsTheRow
    autolayout lr 200 50
}
custom "FencedLease-part6" "FencedLease (activity), part 6 of 6" "Activity diagram converted from fence F6 (line 333)." {
    include am1FencedLease_ExtendTheExpiry am1FencedLease_Renewed am1FencedLease_ClearOwnerAndExpiry am1FencedLease_Released
    autolayout lr 250 150
}
custom "GzipCodec" "GzipCodec (activity), part 1 of 4" "Activity diagram converted from fence F7 (line 358)." {
    include am1GzipCodec_GzipCodecOperationBytes am1GzipCodec_Operation
    autolayout lr 250 150
}
custom "GzipCodec-part2" "GzipCodec (activity), part 2 of 4" "Activity diagram converted from fence F7 (line 358)." {
    include am1GzipCodec_Operation am1GzipCodec_WriteOneGZIPMemberAtThe am1GzipCodec_ValidGZIPMember am1GzipCodec_DecompressCompareLengthAndSHA256
    autolayout lr 250 150
}
custom "GzipCodec-part3" "GzipCodec (activity), part 3 of 4" "Activity diagram converted from fence F7 (line 358)." {
    include am1GzipCodec_WriteOneGZIPMemberAtThe am1GzipCodec_GzipCodecRoundTripOnTheResult am1GzipCodec_ValidGZIPMember am1GzipCodec_EvidenceCorrupt2 am1GzipCodec_OriginalBytes
    autolayout lr 250 150
}
custom "GzipCodec-part4" "GzipCodec (activity), part 4 of 4" "Activity diagram converted from fence F7 (line 358)." {
    include am1GzipCodec_DecompressCompareLengthAndSHA256 am1GzipCodec_Equal am1GzipCodec_EvidenceCorrupt3 am1GzipCodec_GzipCodecRoundTripOnTheResult am1GzipCodec_CompressedBytes am1GzipCodec_EvidenceCorrupt
    autolayout lr 200 50
}
custom "AuditLedgerGuard" "AuditLedgerGuard (activity), part 1 of 2" "Activity diagram converted from fence F8 (line 377)." {
    include am1AuditLedgerGuard_AuditLedgerGuardOperation am1AuditLedgerGuard_DataAuditLogsWritable am1AuditLedgerGuard_503AuditUnavailableRetryableNothingWritten am1AuditLedgerGuard_Operation
    autolayout tb 100 100
}
custom "AuditLedgerGuard-part2" "AuditLedgerGuard (activity), part 2 of 2" "Activity diagram converted from fence F8 (line 377)." {
    include am1AuditLedgerGuard_Operation am1AuditLedgerGuard_Available am1AuditLedgerGuard_AddOneLedgerRowActionRead am1AuditLedgerGuard_ReadRecordedAndCommittedBeforeThe
    autolayout tb 100 100
}
custom "AgentIdentity" "AgentIdentity.Normalize (activity), part 1 of 2" "Activity diagram converted from fence F9 (line 391)." {
    include am1AgentIdentity_AgentIdentityNormalizeName am1AgentIdentity_Blank am1AgentIdentity_Unknown am1AgentIdentity_LowerCaseRemoveSeparators
    autolayout tb 100 100
}
custom "AgentIdentity-part2" "AgentIdentity.Normalize (activity), part 2 of 2" "Activity diagram converted from fence F9 (line 391)." {
    include am1AgentIdentity_LowerCaseRemoveSeparators am1AgentIdentity_InTheAliasTable am1AgentIdentity_ReviewerIdClineCopilotClaudeCodex am1AgentIdentity_UnknownWithThePascalCaseForm
    autolayout tb 100 100
}
custom "TurnSnapshotBuilder" "TurnSnapshotBuilder (activity), part 1 of 2" "Activity diagram converted from fence F10 (line 405)." {
    include am1TurnSnapshotBuilder_TurnSnapshotBuilderTurn am1TurnSnapshotBuilder_ProjectTheListedFieldsInStored am1TurnSnapshotBuilder_SecretRedactorOnEveryTextField am1TurnSnapshotBuilder_SerializeAsCanonicalJSON am1TurnSnapshotBuilder_ContentHasherSHA256OfTheStored
    autolayout tb 100 200
}
custom "TurnSnapshotBuilder-part2" "TurnSnapshotBuilder (activity), part 2 of 2" "Activity diagram converted from fence F10 (line 405)." {
    include am1TurnSnapshotBuilder_ContentHasherSHA256OfTheStored am1TurnSnapshotBuilder_SnapshotJSONAndSnapshotSha256
    autolayout lr 250 150
}
custom "RequirementReferenceExtractor" "RequirementReferenceExtractor (activity), part 1 of 2" "Activity diagram converted from fence F11 (line 418)." {
    include am1RequirementReferenceExtractor_RequirementReferenceExtractorContextText am1RequirementReferenceExtractor_ScanForFRTRTESTIds am1RequirementReferenceExtractor_AnyReferenceFound
    autolayout tb 100 200
}
custom "RequirementReferenceExtractor-part2" "RequirementReferenceExtractor (activity), part 2 of 2" "Activity diagram converted from fence F11 (line 418)." {
    include am1RequirementReferenceExtractor_AnyReferenceFound am1RequirementReferenceExtractor_EmptyList am1RequirementReferenceExtractor_ResolveEachAgainstTheEffectiveRequirements am1RequirementReferenceExtractor_OrderedListKindIdResolvedTitle
    autolayout tb 100 100
}
custom "NotificationOutbox" "NotificationOutbox.Append (activity)" "Activity diagram converted from fence F12 (line 431)." {
    include am1NotificationOutbox_NotificationOutboxAppendInTheCallerS am1NotificationOutbox_ReadAndIncrementHvWorkspaceStateNextNotificationSequence am1NotificationOutbox_InsertHvNotificationWithTheSequenceKind am1NotificationOutbox_RegisterThePostCommitWakeUp am1NotificationOutbox_AppendedSequenceVisibleOnlyWhenThe
    autolayout tb 100 200
}
custom "OrderedEventReplay" "OrderedEventReplay (activity), part 1 of 4" "Activity diagram converted from fence F13 (line 443)." {
    include am1OrderedEventReplay_OrderedEventReplaySourceLastEventID am1OrderedEventReplay_LastEventIDValidOrAbsent am1OrderedEventReplay_400ValidationErrorDetailsFieldLast am1OrderedEventReplay_ReadStoredEventsAfterThePosition
    autolayout tb 100 100
}
custom "OrderedEventReplay-part2" "OrderedEventReplay (activity), part 2 of 4" "Activity diagram converted from fence F13 (line 443)." {
    include am1OrderedEventReplay_ReadStoredEventsAfterThePosition am1OrderedEventReplay_SendEachEventWithItsId am1OrderedEventReplay_SourceTerminalAndAllEventsSent
    autolayout tb 100 200
}
custom "OrderedEventReplay-part3" "OrderedEventReplay (activity), part 3 of 4" "Activity diagram converted from fence F13 (line 443)." {
    include am1OrderedEventReplay_SourceTerminalAndAllEventsSent am1OrderedEventReplay_StreamEndedWithTheTerminalEvent am1OrderedEventReplay_WaitForAWakeUpOr am1OrderedEventReplay_CallerDisconnected
    autolayout tb 100 100
}
custom "OrderedEventReplay-part4" "OrderedEventReplay (activity), part 4 of 4" "Activity diagram converted from fence F13 (line 443)." {
    include am1OrderedEventReplay_CallerDisconnected am1OrderedEventReplay_StreamClosed am1OrderedEventReplay_ReadStoredEventsAfterThePosition
    autolayout lr 250 150
}
custom "ListHvReviewersQuery" "ListHvReviewersQuery: list the reviewer pool (activity)" "Activity diagram converted from fence F14 (line 615)." {
    include am1ListHvReviewersQuery_HandlerRunsGETReviewers am1ListHvReviewersQuery_LoadTheWorkspaceSHvReviewerRows am1ListHvReviewersQuery_CountEachReviewerSRunsIn am1ListHvReviewersQuery_200TheFiveHvReviewerDto
    autolayout tb 100 200
}
custom "GetHvReviewerQuery" "GetHvReviewerQuery: get one reviewer (activity), part 1 of 2" "Activity diagram converted from fence F15 (line 627)." {
    include am1GetHvReviewerQuery_HandlerRunsGETReviewersReviewerId am1GetHvReviewerQuery_AgentIdentityNormalize am1GetHvReviewerQuery_404NotFoundDetailsResourceReviewer am1GetHvReviewerQuery_LoadTheRowAndCountIts
    autolayout tb 100 100
}
custom "GetHvReviewerQuery-part2" "GetHvReviewerQuery: get one reviewer (activity), part 2 of 2" "Activity diagram converted from fence F15 (line 627)." {
    include am1GetHvReviewerQuery_LoadTheRowAndCountIts am1GetHvReviewerQuery_200HvReviewerDto
    autolayout lr 250 150
}
custom "UpdateHvReviewerCommand" "UpdateHvReviewerCommand: manage a reviewer in real time (activity), part 1 of 6" "Activity diagram converted from fence F16 (line 643)." {
    include am1UpdateHvReviewerCommand_HandlerRunsPATCHReviewersReviewerId am1UpdateHvReviewerCommand_FieldsValidAndExpectedStateVersionPresent am1UpdateHvReviewerCommand_400ValidationErrorNamingTheField am1UpdateHvReviewerCommand_AgentIdentityNormalize
    autolayout tb 100 100
}
custom "UpdateHvReviewerCommand-part2" "UpdateHvReviewerCommand: manage a reviewer in real time (activity), part 2 of 6" "Activity diagram converted from fence F16 (line 643)." {
    include am1UpdateHvReviewerCommand_AgentIdentityNormalize am1UpdateHvReviewerCommand_404NotFoundDetailsResourceReviewer am1UpdateHvReviewerCommand_SerializableWriteWithAuditLedgerGuard
    autolayout lr 250 150
}
custom "UpdateHvReviewerCommand-part3" "UpdateHvReviewerCommand: manage a reviewer in real time (activity), part 3 of 6" "Activity diagram converted from fence F16 (line 643)." {
    include am1UpdateHvReviewerCommand_SerializableWriteWithAuditLedgerGuard am1UpdateHvReviewerCommand_503AuditUnavailable am1UpdateHvReviewerCommand_409ConflictRetryableTrue am1UpdateHvReviewerCommand_StateVersionEqualsExpectedStateVersion
    autolayout lr 250 150
}
custom "UpdateHvReviewerCommand-part4" "UpdateHvReviewerCommand: manage a reviewer in real time (activity), part 4 of 6" "Activity diagram converted from fence F16 (line 643)." {
    include am1UpdateHvReviewerCommand_StateVersionEqualsExpectedStateVersion am1UpdateHvReviewerCommand_409ConflictRetryableFalseDetailsField am1UpdateHvReviewerCommand_ApplyTheFields am1UpdateHvReviewerCommand_StrategyAgentPathModelOrEffort
    autolayout tb 100 100
}
custom "UpdateHvReviewerCommand-part5" "UpdateHvReviewerCommand: manage a reviewer in real time (activity), part 5 of 6" "Activity diagram converted from fence F16 (line 643)." {
    include am1UpdateHvReviewerCommand_StrategyAgentPathModelOrEffort am1UpdateHvReviewerCommand_ConfigurationVersionPlus1ReadinessUnverifiedReason am1UpdateHvReviewerCommand_EnabledAndUnverifiedNow am1UpdateHvReviewerCommand_NotificationOutboxReviewerUnverified
    autolayout tb 100 200
}
custom "UpdateHvReviewerCommand-part6" "UpdateHvReviewerCommand: manage a reviewer in real time (activity), part 6 of 6" "Activity diagram converted from fence F16 (line 643)." {
    include am1UpdateHvReviewerCommand_EnabledAndUnverifiedNow am1UpdateHvReviewerCommand_SetProbeRequestedUtcADurableProbeRequest am1UpdateHvReviewerCommand_IncrementStateVersion am1UpdateHvReviewerCommand_NotificationOutboxReviewerUnverified am1UpdateHvReviewerCommand_200HvReviewerDtoTheWorkerIsWoken
    autolayout tb 100 200
}
custom "StartHvReviewerNonceCommand" "StartHvReviewerNonceCommand: request a NONCE probe (activity), part 1 of 4" "Activity diagram converted from fence F17 (line 672)." {
    include am1StartHvReviewerNonceCommand_HandlerRunsPOSTReviewersReviewerIdNonce am1StartHvReviewerNonceCommand_AgentIdentityNormalize am1StartHvReviewerNonceCommand_404NotFoundDetailsResourceReviewer am1StartHvReviewerNonceCommand_SerializableWriteWithAuditLedgerGuard
    autolayout tb 100 100
}
custom "StartHvReviewerNonceCommand-part2" "StartHvReviewerNonceCommand: request a NONCE probe (activity), part 2 of 4" "Activity diagram converted from fence F17 (line 672)." {
    include am1StartHvReviewerNonceCommand_SerializableWriteWithAuditLedgerGuard am1StartHvReviewerNonceCommand_503AuditUnavailable am1StartHvReviewerNonceCommand_409ConflictRetryableTrue am1StartHvReviewerNonceCommand_ReviewerEnabled
    autolayout lr 250 150
}
custom "StartHvReviewerNonceCommand-part3" "StartHvReviewerNonceCommand: request a NONCE probe (activity), part 3 of 4" "Activity diagram converted from fence F17 (line 672)." {
    include am1StartHvReviewerNonceCommand_ReviewerEnabled am1StartHvReviewerNonceCommand_409HvReviewerDisabled am1StartHvReviewerNonceCommand_AProbeRunClaimedOrRunning am1StartHvReviewerNonceCommand_202RunningWithItsRunId am1StartHvReviewerNonceCommand_ProbeRequestedUtcAlreadySet
    autolayout tb 100 100
}
custom "StartHvReviewerNonceCommand-part4" "StartHvReviewerNonceCommand: request a NONCE probe (activity), part 4 of 4" "Activity diagram converted from fence F17 (line 672)." {
    include am1StartHvReviewerNonceCommand_ProbeRequestedUtcAlreadySet am1StartHvReviewerNonceCommand_202AlreadyRequestedNothingWritten am1StartHvReviewerNonceCommand_SetProbeRequestedUtc am1StartHvReviewerNonceCommand_202RequestedTheWorkerIsWoken
    autolayout tb 100 100
}
custom "SubmitHvRequestCommand" "SubmitHvRequestCommand: admit an HV request (activity), part 1 of 7" "Activity diagram converted from fence F18 (line 705)." {
    include am1SubmitHvRequestCommand_HandlerRunsPOSTRequests am1SubmitHvRequestCommand_ScopeSubmitterKeyAndContextWell
    autolayout lr 250 150
}
custom "SubmitHvRequestCommand-part2" "SubmitHvRequestCommand: admit an HV request (activity), part 2 of 7" "Activity diagram converted from fence F18 (line 705)." {
    include am1SubmitHvRequestCommand_ScopeSubmitterKeyAndContextWell am1SubmitHvRequestCommand_400HvScopeInvalidDetailsField am1SubmitHvRequestCommand_400ValidationErrorDetailsField am1SubmitHvRequestCommand_RequirementReferenceExtractorOnTheContext
    autolayout lr 250 150
}
custom "SubmitHvRequestCommand-part3" "SubmitHvRequestCommand: admit an HV request (activity), part 3 of 7" "Activity diagram converted from fence F18 (line 705)." {
    include am1SubmitHvRequestCommand_RequirementReferenceExtractorOnTheContext am1SubmitHvRequestCommand_SerializableWriteWithAuditLedgerGuard
    autolayout lr 250 150
}
custom "SubmitHvRequestCommand-part4" "SubmitHvRequestCommand: admit an HV request (activity), part 4 of 7" "Activity diagram converted from fence F18 (line 705)." {
    include am1SubmitHvRequestCommand_SerializableWriteWithAuditLedgerGuard am1SubmitHvRequestCommand_503AuditUnavailable am1SubmitHvRequestCommand_409ConflictRetryableTrue am1SubmitHvRequestCommand_IdempotentCreateLookupPolicyConflict
    autolayout lr 250 150
}
custom "SubmitHvRequestCommand-part5" "SubmitHvRequestCommand: admit an HV request (activity), part 5 of 7" "Activity diagram converted from fence F18 (line 705)." {
    include am1SubmitHvRequestCommand_IdempotentCreateLookupPolicyConflict am1SubmitHvRequestCommand_GetHvRequestQueryForTheStoredRequestAfter am1SubmitHvRequestCommand_409ConflictRetryableFalse am1SubmitHvRequestCommand_AdmitRequestChecks
    autolayout lr 250 150
}
custom "SubmitHvRequestCommand-part6" "SubmitHvRequestCommand: admit an HV request (activity), part 6 of 7" "Activity diagram converted from fence F18 (line 705)." {
    include am1SubmitHvRequestCommand_GetHvRequestQueryForTheStoredRequestAfter am1SubmitHvRequestCommand_200TheStoredRequestItsOutput am1SubmitHvRequestCommand_ThatAnswerOfGetHvRequestQuery am1SubmitHvRequestCommand_AdmitRequestChecks am1SubmitHvRequestCommand_RefusalOfAdmitRequestChecksDrawnThere am1SubmitHvRequestCommand_InsertAdmittedRequest
    autolayout lr 200 50
}
custom "SubmitHvRequestCommand-part7" "SubmitHvRequestCommand: admit an HV request (activity), part 7 of 7" "Activity diagram converted from fence F18 (line 705)." {
    include am1SubmitHvRequestCommand_InsertAdmittedRequest am1SubmitHvRequestCommand_SerializableWriteWithAuditLedgerGuard am1SubmitHvRequestCommand_201HvRequestResultTheWorkerIsWoken
    autolayout lr 250 150
}
custom "AdmitRequestChecks" "Nested activity: AdmitRequestChecks (the checks of SubmitHvRequestCommand, inside its delegate) (activity), part 1 of 3" "Activity diagram converted from fence F19 (line 728)." {
    include am1AdmitRequestChecks_AdmitRequestChecks am1AdmitRequestChecks_SubmitterKindAgent am1AdmitRequestChecks_TurnLocatorTheSubmittingTurn am1AdmitRequestChecks_PolicyHvScopeResolution
    autolayout tb 100 200
}
custom "AdmitRequestChecks-part2" "Nested activity: AdmitRequestChecks (the checks of SubmitHvRequestCommand, inside its delegate) (activity), part 2 of 3" "Activity diagram converted from fence F19 (line 728)." {
    include am1AdmitRequestChecks_TurnLocatorTheSubmittingTurn am1AdmitRequestChecks_400ValidationErrorDetailsFieldSubmitter am1AdmitRequestChecks_PolicyHvScopeResolution am1AdmitRequestChecks_400HvScopeInvalidHvScope am1AdmitRequestChecks_PolicyHvAdmissionEligibility
    autolayout tb 100 100
}
custom "AdmitRequestChecks-part3" "Nested activity: AdmitRequestChecks (the checks of SubmitHvRequestCommand, inside its delegate) (activity), part 3 of 3" "Activity diagram converted from fence F19 (line 728)." {
    include am1AdmitRequestChecks_PolicyHvAdmissionEligibility am1AdmitRequestChecks_409HvTurnsNotTerminalHv am1AdmitRequestChecks_PolicyRoundRobinAssignment am1AdmitRequestChecks_409HvNoReviewerAvailableDetails am1AdmitRequestChecks_MembersAndReviewer
    autolayout tb 100 100
}
custom "InsertAdmittedRequest" "Nested activity: InsertAdmittedRequest (the inserts of SubmitHvRequestCommand, inside its delegate) (activity), part 1 of 4" "Activity diagram converted from fence F20 (line 745)." {
    include am1InsertAdmittedRequest_InsertAdmittedRequestMembersReviewerReferences am1InsertAdmittedRequest_ReadableKeyAllocatorHvrKey am1InsertAdmittedRequest_AttemptEndsSerializableWriteRunsTheDelegate am1InsertAdmittedRequest_InsertHvRequestQueuedWithTheAssigned
    autolayout tb 100 100
}
custom "InsertAdmittedRequest-part2" "Nested activity: InsertAdmittedRequest (the inserts of SubmitHvRequestCommand, inside its delegate) (activity), part 2 of 4" "Activity diagram converted from fence F20 (line 745)." {
    include am1InsertAdmittedRequest_InsertHvRequestQueuedWithTheAssigned am1InsertAdmittedRequest_TurnSnapshotBuilderForEachMemberTurn am1InsertAdmittedRequest_InsertOneHvRequestTurnPerMemberOrdinal am1InsertAdmittedRequest_InsertTheRequirementReferences am1InsertAdmittedRequest_InsertHvRequestResponsePendingForEachGradable
    autolayout tb 100 200
}
custom "InsertAdmittedRequest-part3" "Nested activity: InsertAdmittedRequest (the inserts of SubmitHvRequestCommand, inside its delegate) (activity), part 3 of 4" "Activity diagram converted from fence F20 (line 745)." {
    include am1InsertAdmittedRequest_InsertHvRequestResponsePendingForEachGradable am1InsertAdmittedRequest_GzipCodecCompressTheAcceptedRequest am1InsertAdmittedRequest_AttemptFails500EvidenceCorruptNothing am1InsertAdmittedRequest_InsertHvEvidenceRequestWithItsSHA
    autolayout tb 100 100
}
custom "InsertAdmittedRequest-part4" "Nested activity: InsertAdmittedRequest (the inserts of SubmitHvRequestCommand, inside its delegate) (activity), part 4 of 4" "Activity diagram converted from fence F20 (line 745)." {
    include am1InsertAdmittedRequest_InsertHvEvidenceRequestWithItsSHA am1InsertAdmittedRequest_PolicyHvTurnFreezeFreezeEachMemberTurn am1InsertAdmittedRequest_NotificationOutboxHvQueued am1InsertAdmittedRequest_Inserted
    autolayout tb 100 200
}
custom "GetHvRequestQuery" "GetHvRequestQuery: get an HV request with its progress (activity), part 1 of 4" "Activity diagram converted from fence F21 (line 768)." {
    include am1GetHvRequestQuery_HandlerRunsGETRequestsHvRequestId am1GetHvRequestQuery_RequestInTheWorkspace am1GetHvRequestQuery_404NotFoundDetailsResourceHv am1GetHvRequestQuery_LoadTurnsReferencesResponsesAndThe
    autolayout tb 100 100
}
custom "GetHvRequestQuery-part2" "GetHvRequestQuery: get an HV request with its progress (activity), part 2 of 4" "Activity diagram converted from fence F21 (line 768)." {
    include am1GetHvRequestQuery_LoadTurnsReferencesResponsesAndThe am1GetHvRequestQuery_CurrentRunHasOutput
    autolayout lr 250 150
}
custom "GetHvRequestQuery-part3" "GetHvRequestQuery: get an HV request with its progress (activity), part 3 of 4" "Activity diagram converted from fence F21 (line 768)." {
    include am1GetHvRequestQuery_CurrentRunHasOutput am1GetHvRequestQuery_200HvRequestResultLastOutputEmptyEtaStatusAs am1GetHvRequestQuery_200HvRequestResultLastOutputEmptyOutputRemovedTrue am1GetHvRequestQuery_AuditLedgerGuardRecordRead
    autolayout lr 250 150
}
custom "GetHvRequestQuery-part4" "GetHvRequestQuery: get an HV request with its progress (activity), part 4 of 4" "Activity diagram converted from fence F21 (line 768)." {
    include am1GetHvRequestQuery_AuditLedgerGuardRecordRead am1GetHvRequestQuery_503AuditUnavailable am1GetHvRequestQuery_GzipCodecDecompressTheLastChunksTo am1GetHvRequestQuery_500EvidenceCorrupt am1GetHvRequestQuery_200HvRequestResultWithLastOutput
    autolayout tb 100 100
}
custom "QueryHvRequestsQuery" "QueryHvRequestsQuery: list HV requests, running ones included (activity), part 1 of 2" "Activity diagram converted from fence F22 (line 789)." {
    include am1QueryHvRequestsQuery_HandlerRunsGETRequests am1QueryHvRequestsQuery_StatusAndReviewerFiltersKnown am1QueryHvRequestsQuery_400ValidationErrorDetailsFieldStatus am1QueryHvRequestsQuery_PagedQueryPageBoundsRefuseANDFiltersCreatedUtc
    autolayout tb 100 100
}
custom "QueryHvRequestsQuery-part2" "QueryHvRequestsQuery: list HV requests, running ones included (activity), part 2 of 2" "Activity diagram converted from fence F22 (line 789)." {
    include am1QueryHvRequestsQuery_PagedQueryPageBoundsRefuseANDFiltersCreatedUtc am1QueryHvRequestsQuery_400ValidationErrorDetailsFieldLimit am1QueryHvRequestsQuery_200PageResultOfHVRequestSummaries
    autolayout lr 250 150
}
custom "CancelHvRequestCommand" "CancelHvRequestCommand: cancel any HV (activity), part 1 of 6" "Activity diagram converted from fence F23 (line 804)." {
    include am1CancelHvRequestCommand_HandlerRunsPOSTRequestsHvRequestIdCancel am1CancelHvRequestCommand_SerializableWriteWithAuditLedgerGuard
    autolayout lr 250 150
}
custom "CancelHvRequestCommand-part2" "CancelHvRequestCommand: cancel any HV (activity), part 2 of 6" "Activity diagram converted from fence F23 (line 804)." {
    include am1CancelHvRequestCommand_SerializableWriteWithAuditLedgerGuard am1CancelHvRequestCommand_503AuditUnavailable am1CancelHvRequestCommand_409ConflictRetryableTrue am1CancelHvRequestCommand_RequestInTheWorkspace
    autolayout lr 250 150
}
custom "CancelHvRequestCommand-part3" "CancelHvRequestCommand: cancel any HV (activity), part 3 of 6" "Activity diagram converted from fence F23 (line 804)." {
    include am1CancelHvRequestCommand_RequestInTheWorkspace am1CancelHvRequestCommand_404NotFoundDetailsResourceHv am1CancelHvRequestCommand_Status
    autolayout lr 250 150
}
custom "CancelHvRequestCommand-part4" "CancelHvRequestCommand: cancel any HV (activity), part 4 of 6" "Activity diagram converted from fence F23 (line 804)." {
    include am1CancelHvRequestCommand_Status am1CancelHvRequestCommand_CanceledWithCompletedUtcPendingResponsesClosed am1CancelHvRequestCommand_CancelRequestedWithCancelRequestedUtc am1CancelHvRequestCommand_200Unchanged am1CancelHvRequestCommand_409HvRequestNotCancelableDetails
    autolayout lr 200 50
}
custom "CancelHvRequestCommand-part5" "CancelHvRequestCommand: cancel any HV (activity), part 5 of 6" "Activity diagram converted from fence F23 (line 804)." {
    include am1CancelHvRequestCommand_CanceledWithCompletedUtcPendingResponsesClosed am1CancelHvRequestCommand_PolicyHvTurnFreezeRelease am1CancelHvRequestCommand_CancelRequestedWithCancelRequestedUtc am1CancelHvRequestCommand_NotificationOutboxHvCancelRequested am1CancelHvRequestCommand_NotificationOutboxHvCanceled am1CancelHvRequestCommand_200CancelRequested
    autolayout tb 100 100
}
custom "CancelHvRequestCommand-part6" "CancelHvRequestCommand: cancel any HV (activity), part 6 of 6" "Activity diagram converted from fence F23 (line 804)." {
    include am1CancelHvRequestCommand_NotificationOutboxHvCanceled am1CancelHvRequestCommand_200Canceled
    autolayout lr 250 150
}
custom "GetHvResultQuery" "GetHvResultQuery: the standard result document (activity), part 1 of 4" "Activity diagram converted from fence F24 (line 831)." {
    include am1GetHvResultQuery_HandlerRunsGETRequestsHvRequestIdResult am1GetHvResultQuery_RequestInTheWorkspace am1GetHvResultQuery_404NotFoundDetailsResourceHv am1GetHvResultQuery_StatusCompleted
    autolayout tb 100 100
}
custom "GetHvResultQuery-part2" "GetHvResultQuery: the standard result document (activity), part 2 of 4" "Activity diagram converted from fence F24 (line 831)." {
    include am1GetHvResultQuery_StatusCompleted am1GetHvResultQuery_409HvResultNotReadyDetails am1GetHvResultQuery_EvidenceRemoved am1GetHvResultQuery_410HvEvidenceRemovedHashesAnd am1GetHvResultQuery_AuditLedgerGuardRecordRead
    autolayout tb 100 100
}
custom "GetHvResultQuery-part3" "GetHvResultQuery: the standard result document (activity), part 3 of 4" "Activity diagram converted from fence F24 (line 831)." {
    include am1GetHvResultQuery_AuditLedgerGuardRecordRead am1GetHvResultQuery_503AuditUnavailable am1GetHvResultQuery_GzipCodecDecompressResultAndRequestEvidence am1GetHvResultQuery_500EvidenceCorrupt am1GetHvResultQuery_StreamTheFinalizedRunSChunks
    autolayout tb 100 100
}
custom "GetHvResultQuery-part4" "GetHvResultQuery: the standard result document (activity), part 4 of 4" "Activity diagram converted from fence F24 (line 831)." {
    include am1GetHvResultQuery_StreamTheFinalizedRunSChunks am1GetHvResultQuery_200HvResultDocumentGzipWhenTheCaller am1GetHvResultQuery_ResponseEndsIncompleteTheChainHash
    autolayout lr 250 150
}
custom "GetHvEvidenceQuery" "GetHvEvidenceQuery: download evidence (activity), part 1 of 5" "Activity diagram converted from fence F25 (line 854)." {
    include am1GetHvEvidenceQuery_HandlerRunsGETRequestsHvRequestIdEvidence am1GetHvEvidenceQuery_KindRequestResponseLogOrResult am1GetHvEvidenceQuery_400ValidationErrorDetailsFieldKind am1GetHvEvidenceQuery_RequestInTheWorkspaceAndRunId
    autolayout tb 100 100
}
custom "GetHvEvidenceQuery-part2" "GetHvEvidenceQuery: download evidence (activity), part 2 of 5" "Activity diagram converted from fence F25 (line 854)." {
    include am1GetHvEvidenceQuery_RequestInTheWorkspaceAndRunId am1GetHvEvidenceQuery_404NotFoundDetailsResourceHv am1GetHvEvidenceQuery_404NotFoundDetailsResourceRun am1GetHvEvidenceQuery_EvidenceProduced
    autolayout lr 250 150
}
custom "GetHvEvidenceQuery-part3" "GetHvEvidenceQuery: download evidence (activity), part 3 of 5" "Activity diagram converted from fence F25 (line 854)." {
    include am1GetHvEvidenceQuery_EvidenceProduced am1GetHvEvidenceQuery_404HvEvidenceNotAvailable am1GetHvEvidenceQuery_410HvEvidenceRemoved am1GetHvEvidenceQuery_AuditLedgerGuardRecordRead
    autolayout lr 250 150
}
custom "GetHvEvidenceQuery-part4" "GetHvEvidenceQuery: download evidence (activity), part 4 of 5" "Activity diagram converted from fence F25 (line 854)." {
    include am1GetHvEvidenceQuery_AuditLedgerGuardRecordRead am1GetHvEvidenceQuery_503AuditUnavailable am1GetHvEvidenceQuery_DecompressedRequested am1GetHvEvidenceQuery_200ApplicationGzipTheStoredMember am1GetHvEvidenceQuery_GzipCodecDecompressStreamed
    autolayout tb 100 100
}
custom "GetHvEvidenceQuery-part5" "GetHvEvidenceQuery: download evidence (activity), part 5 of 5" "Activity diagram converted from fence F25 (line 854)." {
    include am1GetHvEvidenceQuery_GzipCodecDecompressStreamed am1GetHvEvidenceQuery_500EvidenceCorrupt am1GetHvEvidenceQuery_200TheOriginalBytes am1GetHvEvidenceQuery_BodyEndsIncompleteTheStoredSHA
    autolayout lr 250 150
}
custom "RemoveHvEvidenceCommand" "RemoveHvEvidenceCommand: remove evidence on instruction (activity), part 1 of 4" "Activity diagram converted from fence F26 (line 880)." {
    include am1RemoveHvEvidenceCommand_HandlerRunsPOSTRequestsHvRequestIdEvidence am1RemoveHvEvidenceCommand_ReasonAndRemovedByPresentAndWithin am1RemoveHvEvidenceCommand_400ValidationErrorDetailsField am1RemoveHvEvidenceCommand_SerializableWriteWithAuditLedgerGuard
    autolayout tb 100 100
}
custom "RemoveHvEvidenceCommand-part2" "RemoveHvEvidenceCommand: remove evidence on instruction (activity), part 2 of 4" "Activity diagram converted from fence F26 (line 880)." {
    include am1RemoveHvEvidenceCommand_SerializableWriteWithAuditLedgerGuard am1RemoveHvEvidenceCommand_503AuditUnavailable am1RemoveHvEvidenceCommand_409ConflictRetryableTrue am1RemoveHvEvidenceCommand_RequestInTheWorkspace
    autolayout lr 250 150
}
custom "RemoveHvEvidenceCommand-part3" "RemoveHvEvidenceCommand: remove evidence on instruction (activity), part 3 of 4" "Activity diagram converted from fence F26 (line 880)." {
    include am1RemoveHvEvidenceCommand_RequestInTheWorkspace am1RemoveHvEvidenceCommand_404NotFoundDetailsResourceHv am1RemoveHvEvidenceCommand_CompletedFailedOrCanceled am1RemoveHvEvidenceCommand_409HvRequestActiveDetailsStatus am1RemoveHvEvidenceCommand_NullEveryEvidenceAndChunkPayload
    autolayout tb 100 100
}
custom "RemoveHvEvidenceCommand-part4" "RemoveHvEvidenceCommand: remove evidence on instruction (activity), part 4 of 4" "Activity diagram converted from fence F26 (line 880)." {
    include am1RemoveHvEvidenceCommand_NullEveryEvidenceAndChunkPayload am1RemoveHvEvidenceCommand_200RemoveHvEvidenceResultRemovedKindsAndChunk
    autolayout lr 250 150
}
custom "StreamHvRunOutputQuery" "StreamHvRunOutputQuery: attach to the live output of any HV (activity), part 1 of 3" "Activity diagram converted from fence F27 (line 901)." {
    include am1StreamHvRunOutputQuery_HandlerRunsGETRequestsHvRequestIdOutput am1StreamHvRunOutputQuery_RequestInTheWorkspace am1StreamHvRunOutputQuery_404NotFoundDetailsResourceHv am1StreamHvRunOutputQuery_EvidenceRemoved
    autolayout tb 100 100
}
custom "StreamHvRunOutputQuery-part2" "StreamHvRunOutputQuery: attach to the live output of any HV (activity), part 2 of 3" "Activity diagram converted from fence F27 (line 901)." {
    include am1StreamHvRunOutputQuery_EvidenceRemoved am1StreamHvRunOutputQuery_410HvEvidenceRemoved am1StreamHvRunOutputQuery_AuditLedgerGuardRecordRead am1StreamHvRunOutputQuery_503AuditUnavailable am1StreamHvRunOutputQuery_OrderedEventReplayRunsInAttemptOrderChunks
    autolayout tb 100 100
}
custom "StreamHvRunOutputQuery-part3" "StreamHvRunOutputQuery: attach to the live output of any HV (activity), part 3 of 3" "Activity diagram converted from fence F27 (line 901)." {
    include am1StreamHvRunOutputQuery_OrderedEventReplayRunsInAttemptOrderChunks am1StreamHvRunOutputQuery_400ValidationErrorDetailsFieldLast am1StreamHvRunOutputQuery_StreamEndedAfterHvRunClosed am1StreamHvRunOutputQuery_StreamClosed am1StreamHvRunOutputQuery_HvStreamErrorEvidenceCorruptStream
    autolayout lr 200 50
}
custom "StreamHvNotificationsQuery" "StreamHvNotificationsQuery: the HV notification stream (activity)" "Activity diagram converted from fence F28 (line 921)." {
    include am1StreamHvNotificationsQuery_HandlerRunsGETNotificationsStream am1StreamHvNotificationsQuery_OrderedEventReplayHvNotificationRowsBySequence am1StreamHvNotificationsQuery_400ValidationErrorDetailsFieldLast am1StreamHvNotificationsQuery_StreamClosed
    autolayout tb 100 100
}
custom "QueryHvNotificationsQuery" "QueryHvNotificationsQuery: poll notifications (activity), part 1 of 2" "Activity diagram converted from fence F29 (line 933)." {
    include am1QueryHvNotificationsQuery_HandlerRunsGETNotifications am1QueryHvNotificationsQuery_AfterSequenceAtLeast0Limit1 am1QueryHvNotificationsQuery_400ValidationErrorDetailsField am1QueryHvNotificationsQuery_ReadRowsAfterAfterSequenceInSequence
    autolayout tb 100 100
}
custom "QueryHvNotificationsQuery-part2" "QueryHvNotificationsQuery: poll notifications (activity), part 2 of 2" "Activity diagram converted from fence F29 (line 933)." {
    include am1QueryHvNotificationsQuery_ReadRowsAfterAfterSequenceInSequence am1QueryHvNotificationsQuery_200ItemsAndLastSequence
    autolayout lr 250 150
}
custom "QueryHvIncidentsQuery" "QueryHvIncidentsQuery: list incidents (activity), part 1 of 2" "Activity diagram converted from fence F30 (line 946)." {
    include am1QueryHvIncidentsQuery_HandlerRunsGETIncidents am1QueryHvIncidentsQuery_FiltersKnown am1QueryHvIncidentsQuery_400ValidationErrorDetailsField am1QueryHvIncidentsQuery_PagedQueryPageBoundsRefuseANDFiltersCreatedUtc
    autolayout tb 100 100
}
custom "QueryHvIncidentsQuery-part2" "QueryHvIncidentsQuery: list incidents (activity), part 2 of 2" "Activity diagram converted from fence F30 (line 946)." {
    include am1QueryHvIncidentsQuery_PagedQueryPageBoundsRefuseANDFiltersCreatedUtc am1QueryHvIncidentsQuery_400ValidationErrorDetailsFieldLimit am1QueryHvIncidentsQuery_200PageResultOfHvIncidentDto
    autolayout lr 250 150
}
custom "ClaimHvRunCommand" "ClaimHvRunCommand: claim the next probe or review (activity), part 1 of 11" "Activity diagram converted from fence F31 (line 966)." {
    include am1ClaimHvRunCommand_ClaimHvRunCommandWorkerId am1ClaimHvRunCommand_SerializableWriteWithAuditLedgerGuard
    autolayout lr 350 200
}
custom "ClaimHvRunCommand-part2" "ClaimHvRunCommand: claim the next probe or review (activity), part 2 of 11" "Activity diagram converted from fence F31 (line 966)." {
    include am1ClaimHvRunCommand_SerializableWriteWithAuditLedgerGuard am1ClaimHvRunCommand_503AuditUnavailable am1ClaimHvRunCommand_409ConflictRetryableTrue am1ClaimHvRunCommand_AnEnabledReviewerWithARequested
    autolayout lr 350 200
}
custom "ClaimHvRunCommand-part3" "ClaimHvRunCommand: claim the next probe or review (activity), part 3 of 11" "Activity diagram converted from fence F31 (line 966)." {
    include am1ClaimHvRunCommand_AnEnabledReviewerWithARequested am1ClaimHvRunCommand_InsertTheProbeRunWithA am1ClaimHvRunCommand_OldestQueuedRequestWhoseAssigneeIs am1ClaimHvRunCommand_FencedLeaseClaim
    autolayout tb 300 250
}
custom "ClaimHvRunCommand-part4" "ClaimHvRunCommand: claim the next probe or review (activity), part 4 of 11" "Activity diagram converted from fence F31 (line 966)." {
    include am1ClaimHvRunCommand_OldestQueuedRequestWhoseAssigneeIs am1ClaimHvRunCommand_InsertTheReviewRunAttemptAttemptCount am1ClaimHvRunCommand_PolicyRoundRobinAssignmentWithTheCapacityFilter
    autolayout tb 300 250
}
custom "ClaimHvRunCommand-part5" "ClaimHvRunCommand: claim the next probe or review (activity), part 5 of 11" "Activity diagram converted from fence F31 (line 966)." {
    include am1ClaimHvRunCommand_OldestQueuedRequestWhoseAssigneeIs am1ClaimHvRunCommand_AnotherQueuedRequest
    autolayout tb 300 250
}
custom "ClaimHvRunCommand-part6" "ClaimHvRunCommand: claim the next probe or review (activity), part 6 of 11" "Activity diagram converted from fence F31 (line 966)." {
    include am1ClaimHvRunCommand_OldestQueuedRequestWhoseAssigneeIs am1ClaimHvRunCommand_NullNothingClaimable
    autolayout lr 350 200
}
custom "ClaimHvRunCommand-part7" "ClaimHvRunCommand: claim the next probe or review (activity), part 7 of 11" "Activity diagram converted from fence F31 (line 966)." {
    include am1ClaimHvRunCommand_FencedLeaseClaim am1ClaimHvRunCommand_ProbeClaim am1ClaimHvRunCommand_InsertTheReviewRunAttemptAttemptCount am1ClaimHvRunCommand_FencedLeaseClaim2
    autolayout lr 350 200
}
custom "ClaimHvRunCommand-part8" "ClaimHvRunCommand: claim the next probe or review (activity), part 8 of 11" "Activity diagram converted from fence F31 (line 966)." {
    include am1ClaimHvRunCommand_PolicyRoundRobinAssignmentWithTheCapacityFilter am1ClaimHvRunCommand_AnotherQueuedRequest am1ClaimHvRunCommand_InsertTheReviewRunAttemptAttemptCount
    autolayout lr 350 200
}
custom "ClaimHvRunCommand-part9" "ClaimHvRunCommand: claim the next probe or review (activity), part 9 of 11" "Activity diagram converted from fence F31 (line 966)." {
    include am1ClaimHvRunCommand_AnotherQueuedRequest am1ClaimHvRunCommand_NullNothingClaimable
    autolayout lr 350 200
}
custom "ClaimHvRunCommand-part10" "ClaimHvRunCommand: claim the next probe or review (activity), part 10 of 11" "Activity diagram converted from fence F31 (line 966)." {
    include am1ClaimHvRunCommand_FencedLeaseClaim2 am1ClaimHvRunCommand_RequestRunningAttemptCountPlus1StartedUtc am1ClaimHvRunCommand_NotificationOutboxHvStarted
    autolayout tb 300 250
}
custom "ClaimHvRunCommand-part11" "ClaimHvRunCommand: claim the next probe or review (activity), part 11 of 11" "Activity diagram converted from fence F31 (line 966)." {
    include am1ClaimHvRunCommand_NotificationOutboxHvStarted am1ClaimHvRunCommand_ReviewClaim
    autolayout lr 350 200
}
custom "RecordHvRunStartCommand" "RecordHvRunStartCommand: record that the reviewer process started (activity), part 1 of 4" "Activity diagram converted from fence F32 (line 998)." {
    include am1RecordHvRunStartCommand_RecordHvRunStartCommandRunWorkerFenceProcessId am1RecordHvRunStartCommand_SerializableWriteWithAuditLedgerGuard
    autolayout lr 250 150
}
custom "RecordHvRunStartCommand-part2" "RecordHvRunStartCommand: record that the reviewer process started (activity), part 2 of 4" "Activity diagram converted from fence F32 (line 998)." {
    include am1RecordHvRunStartCommand_SerializableWriteWithAuditLedgerGuard am1RecordHvRunStartCommand_503AuditUnavailable am1RecordHvRunStartCommand_409ConflictRetryableTrue am1RecordHvRunStartCommand_FencedLeaseGuard
    autolayout lr 250 150
}
custom "RecordHvRunStartCommand-part3" "RecordHvRunStartCommand: record that the reviewer process started (activity), part 3 of 4" "Activity diagram converted from fence F32 (line 998)." {
    include am1RecordHvRunStartCommand_FencedLeaseGuard am1RecordHvRunStartCommand_StaleFenceNothingChanged am1RecordHvRunStartCommand_RunRunningWithThisProcessId am1RecordHvRunStartCommand_StartedTheStoredStartNothingWritten am1RecordHvRunStartCommand_RunClaimedBecomesRunningStartedUtcProcessId
    autolayout tb 100 100
}
custom "RecordHvRunStartCommand-part4" "RecordHvRunStartCommand: record that the reviewer process started (activity), part 4 of 4" "Activity diagram converted from fence F32 (line 998)." {
    include am1RecordHvRunStartCommand_RunClaimedBecomesRunningStartedUtcProcessId am1RecordHvRunStartCommand_StartedStartedUtc
    autolayout lr 250 150
}
custom "RecordHvRunHeartbeatCommand" "RecordHvRunHeartbeatCommand: renew the lease and read cancellation (activity), part 1 of 3" "Activity diagram converted from fence F33 (line 1017)." {
    include am1RecordHvRunHeartbeatCommand_RecordHvRunHeartbeatCommandRunWorkerFence am1RecordHvRunHeartbeatCommand_SerializableWriteWithAuditLedgerGuard
    autolayout lr 250 150
}
custom "RecordHvRunHeartbeatCommand-part2" "RecordHvRunHeartbeatCommand: renew the lease and read cancellation (activity), part 2 of 3" "Activity diagram converted from fence F33 (line 1017)." {
    include am1RecordHvRunHeartbeatCommand_SerializableWriteWithAuditLedgerGuard am1RecordHvRunHeartbeatCommand_503AuditUnavailable am1RecordHvRunHeartbeatCommand_409ConflictRetryableTrue am1RecordHvRunHeartbeatCommand_FencedLeaseRenew
    autolayout lr 250 150
}
custom "RecordHvRunHeartbeatCommand-part3" "RecordHvRunHeartbeatCommand: renew the lease and read cancellation (activity), part 3 of 3" "Activity diagram converted from fence F33 (line 1017)." {
    include am1RecordHvRunHeartbeatCommand_FencedLeaseRenew am1RecordHvRunHeartbeatCommand_StaleFenceNothingChanged am1RecordHvRunHeartbeatCommand_SetLastHeartbeatUtcReadWhetherTheRequest am1RecordHvRunHeartbeatCommand_RenewedLeaseExpiresUtcCancelRequested
    autolayout tb 100 100
}
custom "AppendHvRunOutputCommand" "AppendHvRunOutputCommand: persist live output (activity), part 1 of 6" "Activity diagram converted from fence F34 (line 1035)." {
    include am1AppendHvRunOutputCommand_AppendHvRunOutputCommandRunFenceSequenceLines am1AppendHvRunOutputCommand_SequenceAtLeast1AndEvery am1AppendHvRunOutputCommand_400ValidationError am1AppendHvRunOutputCommand_SerializableWriteWithAuditLedgerGuard
    autolayout tb 100 100
}
custom "AppendHvRunOutputCommand-part2" "AppendHvRunOutputCommand: persist live output (activity), part 2 of 6" "Activity diagram converted from fence F34 (line 1035)." {
    include am1AppendHvRunOutputCommand_SerializableWriteWithAuditLedgerGuard am1AppendHvRunOutputCommand_503AuditUnavailable am1AppendHvRunOutputCommand_409ConflictRetryableTrue am1AppendHvRunOutputCommand_FencedLeaseGuard
    autolayout lr 250 150
}
custom "AppendHvRunOutputCommand-part3" "AppendHvRunOutputCommand: persist live output (activity), part 3 of 6" "Activity diagram converted from fence F34 (line 1035)." {
    include am1AppendHvRunOutputCommand_FencedLeaseGuard am1AppendHvRunOutputCommand_StaleFenceNothingChanged am1AppendHvRunOutputCommand_AChunkWithThisSequenceStored
    autolayout lr 250 150
}
custom "AppendHvRunOutputCommand-part4" "AppendHvRunOutputCommand: persist live output (activity), part 4 of 6" "Activity diagram converted from fence F34 (line 1035)." {
    include am1AppendHvRunOutputCommand_AChunkWithThisSequenceStored am1AppendHvRunOutputCommand_UnchangedTheStoredChunk am1AppendHvRunOutputCommand_409ConflictRetryableFalse am1AppendHvRunOutputCommand_SequenceIsOutputChunkCountPlus1
    autolayout lr 250 150
}
custom "AppendHvRunOutputCommand-part5" "AppendHvRunOutputCommand: persist live output (activity), part 5 of 6" "Activity diagram converted from fence F34 (line 1035)." {
    include am1AppendHvRunOutputCommand_SequenceIsOutputChunkCountPlus1 am1AppendHvRunOutputCommand_409ConflictDetailsFieldSequence am1AppendHvRunOutputCommand_GzipCodecCompress am1AppendHvRunOutputCommand_500EvidenceCorruptNothingSaved am1AppendHvRunOutputCommand_InsertTheChunkSHA256Chain
    autolayout tb 100 100
}
custom "AppendHvRunOutputCommand-part6" "AppendHvRunOutputCommand: persist live output (activity), part 6 of 6" "Activity diagram converted from fence F34 (line 1035)." {
    include am1AppendHvRunOutputCommand_InsertTheChunkSHA256Chain am1AppendHvRunOutputCommand_SetProgressFromTheLastCompleted am1AppendHvRunOutputCommand_AppendedSequenceChainSha256StreamsWokenAfter
    autolayout tb 100 200
}
custom "CloseHvRunOutputCommand" "CloseHvRunOutputCommand: close a run's output (activity), part 1 of 10" "Activity diagram converted from fence F35 (line 1063)." {
    include am1CloseHvRunOutputCommand_CloseHvRunOutputCommandRunFenceChunkCountProvenance am1CloseHvRunOutputCommand_ProbeRun am1CloseHvRunOutputCommand_AuditLedgerGuardRecordReadTheProbeSChunks am1CloseHvRunOutputCommand_SerializableWriteWithAuditLedgerGuard
    autolayout tb 100 100
}
custom "CloseHvRunOutputCommand-part2" "CloseHvRunOutputCommand: close a run's output (activity), part 2 of 10" "Activity diagram converted from fence F35 (line 1063)." {
    include am1CloseHvRunOutputCommand_AuditLedgerGuardRecordReadTheProbeSChunks am1CloseHvRunOutputCommand_503AuditUnavailableNothingWritten am1CloseHvRunOutputCommand_ReadTheChunksV1And
    autolayout lr 250 150
}
custom "CloseHvRunOutputCommand-part3" "CloseHvRunOutputCommand: close a run's output (activity), part 3 of 10" "Activity diagram converted from fence F35 (line 1063)." {
    include am1CloseHvRunOutputCommand_SerializableWriteWithAuditLedgerGuard am1CloseHvRunOutputCommand_503AuditUnavailable am1CloseHvRunOutputCommand_409ConflictRetryableTrue am1CloseHvRunOutputCommand_FencedLeaseGuard
    autolayout lr 250 150
}
custom "CloseHvRunOutputCommand-part4" "CloseHvRunOutputCommand: close a run's output (activity), part 4 of 10" "Activity diagram converted from fence F35 (line 1063)." {
    include am1CloseHvRunOutputCommand_ReadTheChunksV1And am1CloseHvRunOutputCommand_SerializableWriteWithAuditLedgerGuard
    autolayout lr 250 150
}
custom "CloseHvRunOutputCommand-part5" "CloseHvRunOutputCommand: close a run's output (activity), part 5 of 10" "Activity diagram converted from fence F35 (line 1063)." {
    include am1CloseHvRunOutputCommand_FencedLeaseGuard am1CloseHvRunOutputCommand_StaleFenceNothingChanged am1CloseHvRunOutputCommand_ChunkCountEqualsOutputChunkCountAndForA
    autolayout lr 250 150
}
custom "CloseHvRunOutputCommand-part6" "CloseHvRunOutputCommand: close a run's output (activity), part 6 of 10" "Activity diagram converted from fence F35 (line 1063)." {
    include am1CloseHvRunOutputCommand_ChunkCountEqualsOutputChunkCountAndForA am1CloseHvRunOutputCommand_409ConflictDetailsFieldChunkCount am1CloseHvRunOutputCommand_CloseHvRunOutputCommandRunFenceChunkCountProvenance am1CloseHvRunOutputCommand_RunOutputClosedEndedUtcExit0Runtime
    autolayout lr 250 150
}
custom "CloseHvRunOutputCommand-part7" "CloseHvRunOutputCommand: close a run's output (activity), part 7 of 10" "Activity diagram converted from fence F35 (line 1063)." {
    include am1CloseHvRunOutputCommand_RunOutputClosedEndedUtcExit0Runtime am1CloseHvRunOutputCommand_FencedLeaseRelease am1CloseHvRunOutputCommand_RunKind
    autolayout tb 100 200
}
custom "CloseHvRunOutputCommand-part8" "CloseHvRunOutputCommand: close a run's output (activity), part 8 of 10" "Activity diagram converted from fence F35 (line 1063)." {
    include am1CloseHvRunOutputCommand_RunKind am1CloseHvRunOutputCommand_RequestCancelRequested am1CloseHvRunOutputCommand_PolicyHvProbeOutcomeWithTheVerdictThe am1CloseHvRunOutputCommand_RunCanceledWithCodeCanceled am1CloseHvRunOutputCommand_RequestFinalizing
    autolayout tb 100 100
}
custom "CloseHvRunOutputCommand-part9" "CloseHvRunOutputCommand: close a run's output (activity), part 9 of 10" "Activity diagram converted from fence F35 (line 1063)." {
    include am1CloseHvRunOutputCommand_PolicyHvProbeOutcomeWithTheVerdictThe am1CloseHvRunOutputCommand_ClosedStaleProbeReviewerUnchanged am1CloseHvRunOutputCommand_ClosedProbePassedReviewerOnline am1CloseHvRunOutputCommand_ClosedProbeFailedReviewerOfflineIncident am1CloseHvRunOutputCommand_ClosedProbeFailedServerFaultReadiness
    autolayout lr 200 50
}
custom "CloseHvRunOutputCommand-part10" "CloseHvRunOutputCommand: close a run's output (activity), part 10 of 10" "Activity diagram converted from fence F35 (line 1063)." {
    include am1CloseHvRunOutputCommand_RunCanceledWithCodeCanceled am1CloseHvRunOutputCommand_PolicyHvRetryDecisionRequestCanceledTurnsReleased am1CloseHvRunOutputCommand_RequestFinalizing am1CloseHvRunOutputCommand_NotificationOutboxHvFinalizing am1CloseHvRunOutputCommand_ClosedRequestCanceled am1CloseHvRunOutputCommand_ClosedFinalizingTheWorkerDispatchesFinalizeHvRequestCommand
    autolayout tb 100 100
}
custom "FailHvRunCommand" "FailHvRunCommand: record an execution failure (activity), part 1 of 10" "Activity diagram converted from fence F36 (line 1101)." {
    include am1FailHvRunCommand_FailHvRunCommandRunFenceFailureCodeDetail am1FailHvRunCommand_SerializableWriteWithAuditLedgerGuard
    autolayout lr 250 150
}
custom "FailHvRunCommand-part2" "FailHvRunCommand: record an execution failure (activity), part 2 of 10" "Activity diagram converted from fence F36 (line 1101)." {
    include am1FailHvRunCommand_SerializableWriteWithAuditLedgerGuard am1FailHvRunCommand_503AuditUnavailable am1FailHvRunCommand_409ConflictRetryableTrue am1FailHvRunCommand_FencedLeaseGuard
    autolayout lr 250 150
}
custom "FailHvRunCommand-part3" "FailHvRunCommand: record an execution failure (activity), part 3 of 10" "Activity diagram converted from fence F36 (line 1101)." {
    include am1FailHvRunCommand_FencedLeaseGuard am1FailHvRunCommand_StaleFenceNothingChanged am1FailHvRunCommand_FinalizerSCall
    autolayout lr 250 150
}
custom "FailHvRunCommand-part4" "FailHvRunCommand: record an execution failure (activity), part 4 of 10" "Activity diagram converted from fence F36 (line 1101)." {
    include am1FailHvRunCommand_FinalizerSCall am1FailHvRunCommand_RunOutputClosedAndRequestFinalizingAt am1FailHvRunCommand_RunFailedOrCanceledForCanceled am1FailHvRunCommand_AlreadyCompletedOrNotFinalizableNothing
    autolayout tb 100 100
}
custom "FailHvRunCommand-part5" "FailHvRunCommand: record an execution failure (activity), part 5 of 10" "Activity diagram converted from fence F36 (line 1101)." {
    include am1FailHvRunCommand_RunFailedOrCanceledForCanceled am1FailHvRunCommand_RunKind am1FailHvRunCommand_PolicyHvProbeOutcomeConfigurationCheckThenThe am1FailHvRunCommand_CodeCanceledOrWorkerShutdown
    autolayout tb 100 100
}
custom "FailHvRunCommand-part6" "FailHvRunCommand: record an execution failure (activity), part 6 of 10" "Activity diagram converted from fence F36 (line 1101)." {
    include am1FailHvRunCommand_PolicyHvProbeOutcomeConfigurationCheckThenThe am1FailHvRunCommand_FailedStaleProbeReviewerUnchanged am1FailHvRunCommand_FailedProbeSettledReviewerAsAttributed am1FailHvRunCommand_FailedProbeOfTheCurrentConfiguration
    autolayout lr 250 150
}
custom "FailHvRunCommand-part7" "FailHvRunCommand: record an execution failure (activity), part 7 of 10" "Activity diagram converted from fence F36 (line 1101)." {
    include am1FailHvRunCommand_CodeCanceledOrWorkerShutdown am1FailHvRunCommand_PolicyHvRetryDecisionDecidesAndWritesThe am1FailHvRunCommand_FailureAttributableToTheReviewer
    autolayout lr 250 150
}
custom "FailHvRunCommand-part8" "FailHvRunCommand: record an execution failure (activity), part 8 of 10" "Activity diagram converted from fence F36 (line 1101)." {
    include am1FailHvRunCommand_PolicyHvRetryDecisionDecidesAndWritesThe am1FailHvRunCommand_FailedRequestQueued am1FailHvRunCommand_FailedRequestFailedTurnsReleasedHv am1FailHvRunCommand_FailedRequestCanceledTurnsReleasedHv
    autolayout lr 250 150
}
custom "FailHvRunCommand-part9" "FailHvRunCommand: record an execution failure (activity), part 9 of 10" "Activity diagram converted from fence F36 (line 1101)." {
    include am1FailHvRunCommand_FailureAttributableToTheReviewer am1FailHvRunCommand_InsertHvIncidentPendingKeyHvReviewer am1FailHvRunCommand_InsertHvIncidentPendingKeyHvServer am1FailHvRunCommand_NotificationOutboxReviewerOffline
    autolayout tb 100 100
}
custom "FailHvRunCommand-part10" "FailHvRunCommand: record an execution failure (activity), part 10 of 10" "Activity diagram converted from fence F36 (line 1101)." {
    include am1FailHvRunCommand_InsertHvIncidentPendingKeyHvServer am1FailHvRunCommand_PolicyHvRetryDecisionDecidesAndWritesThe am1FailHvRunCommand_NotificationOutboxReviewerOffline
    autolayout lr 250 150
}
custom "ExpireHvRunLeasesCommand" "ExpireHvRunLeasesCommand: recover from dead workers (activity), part 1 of 7" "Activity diagram converted from fence F37 (line 1137)." {
    include am1ExpireHvRunLeasesCommand_ExpireHvRunLeasesCommand am1ExpireHvRunLeasesCommand_ListRunsClaimedOrRunningWith am1ExpireHvRunLeasesCommand_NextExpiredRun
    autolayout tb 100 200
}
custom "ExpireHvRunLeasesCommand-part2" "ExpireHvRunLeasesCommand: recover from dead workers (activity), part 2 of 7" "Activity diagram converted from fence F37 (line 1137)." {
    include am1ExpireHvRunLeasesCommand_NextExpiredRun am1ExpireHvRunLeasesCommand_ListFinalizingRequests am1ExpireHvRunLeasesCommand_SerializableWriteWithAuditLedgerGuardOnePerRun
    autolayout tb 100 100
}
custom "ExpireHvRunLeasesCommand-part3" "ExpireHvRunLeasesCommand: recover from dead workers (activity), part 3 of 7" "Activity diagram converted from fence F37 (line 1137)." {
    include am1ExpireHvRunLeasesCommand_ListFinalizingRequests am1ExpireHvRunLeasesCommand_SweepResultAbandonedRunsFinalizationPending
    autolayout lr 250 150
}
custom "ExpireHvRunLeasesCommand-part4" "ExpireHvRunLeasesCommand: recover from dead workers (activity), part 4 of 7" "Activity diagram converted from fence F37 (line 1137)." {
    include am1ExpireHvRunLeasesCommand_SerializableWriteWithAuditLedgerGuardOnePerRun am1ExpireHvRunLeasesCommand_503AuditUnavailableSweepEnds am1ExpireHvRunLeasesCommand_NextExpiredRun
    autolayout tb 100 100
}
custom "ExpireHvRunLeasesCommand-part5" "ExpireHvRunLeasesCommand: recover from dead workers (activity), part 5 of 7" "Activity diagram converted from fence F37 (line 1137)." {
    include am1ExpireHvRunLeasesCommand_SerializableWriteWithAuditLedgerGuardOnePerRun am1ExpireHvRunLeasesCommand_FencedLeaseExpireStillClaimedOrRunning
    autolayout lr 250 150
}
custom "ExpireHvRunLeasesCommand-part6" "ExpireHvRunLeasesCommand: recover from dead workers (activity), part 6 of 7" "Activity diagram converted from fence F37 (line 1137)." {
    include am1ExpireHvRunLeasesCommand_FencedLeaseExpireStillClaimedOrRunning am1ExpireHvRunLeasesCommand_NextExpiredRun am1ExpireHvRunLeasesCommand_RunAbandonedLeaseExpiredInsertServer am1ExpireHvRunLeasesCommand_RunKind
    autolayout tb 100 100
}
custom "ExpireHvRunLeasesCommand-part7" "ExpireHvRunLeasesCommand: recover from dead workers (activity), part 7 of 7" "Activity diagram converted from fence F37 (line 1137)." {
    include am1ExpireHvRunLeasesCommand_RunKind am1ExpireHvRunLeasesCommand_PolicyHvRetryDecisionRequeueFailOrCancel am1ExpireHvRunLeasesCommand_PolicyHvProbeOutcomeLeaseExpiredProbeRequested am1ExpireHvRunLeasesCommand_NextExpiredRun
    autolayout tb 100 100
}
custom "FinalizeHvRequestCommand" "FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 1 of 7" "Activity diagram converted from fence F38 (line 1164)." {
    include am1FinalizeHvRequestCommand_FinalizeHvRequestCommandHvRequestId am1FinalizeHvRequestCommand_RequestStatus
    autolayout lr 250 150
}
custom "FinalizeHvRequestCommand-part2" "FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 2 of 7" "Activity diagram converted from fence F38 (line 1164)." {
    include am1FinalizeHvRequestCommand_RequestStatus am1FinalizeHvRequestCommand_AlreadyCompletedTheStoredOutcome am1FinalizeHvRequestCommand_NotFinalizableNothingWritten am1FinalizeHvRequestCommand_ReadFinalizationInputs
    autolayout lr 250 150
}
custom "FinalizeHvRequestCommand-part3" "FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 3 of 7" "Activity diagram converted from fence F38 (line 1164)." {
    include am1FinalizeHvRequestCommand_ReadFinalizationInputs am1FinalizeHvRequestCommand_503AuditUnavailableRequestStaysFinalizing am1FinalizeHvRequestCommand_FailHvRunCommandWithTheStoredFenceAnd am1FinalizeHvRequestCommand_SerializableWriteWithAuditLedgerGuard
    autolayout lr 250 150
}
custom "FinalizeHvRequestCommand-part4" "FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 4 of 7" "Activity diagram converted from fence F38 (line 1164)." {
    include am1FinalizeHvRequestCommand_FailHvRunCommandWithTheStoredFenceAnd am1FinalizeHvRequestCommand_VerificationFailedRequestQueuedOrFailed am1FinalizeHvRequestCommand_ThatOutcomeNothingChanged am1FinalizeHvRequestCommand_NotFinalizableNothingChanged am1FinalizeHvRequestCommand_ThatRefusalRequestStaysFinalizingFor
    autolayout lr 200 50
}
custom "FinalizeHvRequestCommand-part5" "FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 5 of 7" "Activity diagram converted from fence F38 (line 1164)." {
    include am1FinalizeHvRequestCommand_SerializableWriteWithAuditLedgerGuard am1FinalizeHvRequestCommand_503AuditUnavailableRequestStaysFinalizing2 am1FinalizeHvRequestCommand_409ConflictRetryableTrueTheSweep am1FinalizeHvRequestCommand_RequestRunChunkManifestTurnStates
    autolayout lr 250 150
}
custom "FinalizeHvRequestCommand-part6" "FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 6 of 7" "Activity diagram converted from fence F38 (line 1164)." {
    include am1FinalizeHvRequestCommand_RequestRunChunkManifestTurnStates am1FinalizeHvRequestCommand_RequestStatus am1FinalizeHvRequestCommand_CommitFinalResult
    autolayout tb 100 200
}
custom "FinalizeHvRequestCommand-part7" "FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 7 of 7" "Activity diagram converted from fence F38 (line 1164)." {
    include am1FinalizeHvRequestCommand_CommitFinalResult am1FinalizeHvRequestCommand_FailHvRunCommandWithTheStoredFenceAnd am1FinalizeHvRequestCommand_RequestStatus am1FinalizeHvRequestCommand_CompletedOverallVerdictPostCommitGrade
    autolayout lr 250 150
}
custom "ReadFinalizationInputs" "Nested activity: ReadFinalizationInputs (reads of FinalizeHvRequestCommand, no transaction open) (activity), part 1 of 3" "Activity diagram converted from fence F39 (line 1189)." {
    include am1ReadFinalizationInputs_ReadFinalizationInputs am1ReadFinalizationInputs_AuditLedgerGuardRecordReadTheRunSChunks am1ReadFinalizationInputs_ReadRefused503AuditUnavailable am1ReadFinalizationInputs_LoadTheOutputClosedRunAndIts
    autolayout tb 100 100
}
custom "ReadFinalizationInputs-part2" "Nested activity: ReadFinalizationInputs (reads of FinalizeHvRequestCommand, no transaction open) (activity), part 2 of 3" "Activity diagram converted from fence F39 (line 1189)." {
    include am1ReadFinalizationInputs_LoadTheOutputClosedRunAndIts am1ReadFinalizationInputs_PolicyHvFinalVerificationV1ToV am1ReadFinalizationInputs_CheckFailedCode am1ReadFinalizationInputs_LinkedResponsesAndModeNotOff
    autolayout tb 100 100
}
custom "ReadFinalizationInputs-part3" "Nested activity: ReadFinalizationInputs (reads of FinalizeHvRequestCommand, no transaction open) (activity), part 3 of 3" "Activity diagram converted from fence F39 (line 1189)." {
    include am1ReadFinalizationInputs_LinkedResponsesAndModeNotOff am1ReadFinalizationInputs_EvaluateStalenessCommandForTheLinkedResponses am1ReadFinalizationInputs_VerifiedRecordsResponseStatesRead
    autolayout tb 100 200
}
custom "CommitFinalResult" "Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate) (activity), part 1 of 6" "Activity diagram converted from fence F40 (line 1204)." {
    include am1CommitFinalResult_CommitFinalResult am1CommitFinalResult_InsertHvTurnAssessmentRowsAndTheHvFinding am1CommitFinalResult_ScoreVerdictAtMaxThresholdAnd0 am1CommitFinalResult_RecordSessionTurnReviewCommandPerMemberTurnOriginHvFinalization am1CommitFinalResult_PolicyHvTurnFreezeFinalizeEachMemberTurn
    autolayout tb 100 200
}
custom "CommitFinalResult-part2" "Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate) (activity), part 2 of 6" "Activity diagram converted from fence F40 (line 1204)." {
    include am1CommitFinalResult_PolicyHvTurnFreezeFinalizeEachMemberTurn am1CommitFinalResult_EachHvRequestTurnReviewed am1CommitFinalResult_PendingLinkedResponses
    autolayout tb 100 200
}
custom "CommitFinalResult-part3" "Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate) (activity), part 3 of 6" "Activity diagram converted from fence F40 (line 1204)." {
    include am1CommitFinalResult_PendingLinkedResponses am1CommitFinalResult_RecordModelGradeCommandPerResponseOriginHvFinalizationWith am1CommitFinalResult_ItemsNotGradedAgentContextDisabled am1CommitFinalResult_GzipCodecCompressTheResultEvidence
    autolayout tb 100 100
}
custom "CommitFinalResult-part4" "Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate) (activity), part 4 of 6" "Activity diagram converted from fence F40 (line 1204)." {
    include am1CommitFinalResult_RecordModelGradeCommandPerResponseOriginHvFinalizationWith am1CommitFinalResult_AStoredGradeWithOtherInput am1CommitFinalResult_AGuardFailedTheAttemptEnds am1CommitFinalResult_GzipCodecCompressTheResultEvidence am1CommitFinalResult_ItemsNotGradedAgentContextDisabled
    autolayout lr 250 150
}
custom "CommitFinalResult-part5" "Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate) (activity), part 5 of 6" "Activity diagram converted from fence F40 (line 1204)." {
    include am1CommitFinalResult_GzipCodecCompressTheResultEvidence am1CommitFinalResult_RoundTripFailedTheTransactionRolls am1CommitFinalResult_RequestCompletedWithCompletedUtcAndFinalizedRunId am1CommitFinalResult_NotificationOutboxHvCompleted
    autolayout tb 100 100
}
custom "CommitFinalResult-part6" "Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate) (activity), part 6 of 6" "Activity diagram converted from fence F40 (line 1204)." {
    include am1CommitFinalResult_NotificationOutboxHvCompleted am1CommitFinalResult_Committed
    autolayout lr 250 150
}
custom "FileHvIncidentCommand" "FileHvIncidentCommand: file an incident through the triage endpoint (activity), part 1 of 5" "Activity diagram converted from fence F41 (line 1231)." {
    include am1FileHvIncidentCommand_FileHvIncidentCommandIncidentId am1FileHvIncidentCommand_IncidentFiledAlready am1FileHvIncidentCommand_Unchanged am1FileHvIncidentCommand_SerializableWriteWithAuditLedgerGuardTheAttempt
    autolayout tb 100 100
}
custom "FileHvIncidentCommand-part2" "FileHvIncidentCommand: file an incident through the triage endpoint (activity), part 2 of 5" "Activity diagram converted from fence F41 (line 1231)." {
    include am1FileHvIncidentCommand_SerializableWriteWithAuditLedgerGuardTheAttempt am1FileHvIncidentCommand_503AuditUnavailableTriageIsNot am1FileHvIncidentCommand_FilingAttemptsPlus1LastFilingAttemptUtc am1FileHvIncidentCommand_ITriageServiceSubmitDedupeKeyErrorSignatureSanitizedEvidence
    autolayout tb 100 100
}
custom "FileHvIncidentCommand-part3" "FileHvIncidentCommand: file an incident through the triage endpoint (activity), part 3 of 5" "Activity diagram converted from fence F41 (line 1231)." {
    include am1FileHvIncidentCommand_ITriageServiceSubmitDedupeKeyErrorSignatureSanitizedEvidence am1FileHvIncidentCommand_SerializableWriteWithAuditLedgerGuardTheOutcome am1FileHvIncidentCommand_SerializableWriteWithAuditLedgerGuardTheOutcome2 am1FileHvIncidentCommand_FiledReportAndGroupIdsFiledUtc am1FileHvIncidentCommand_503AuditUnavailableTheIncidentStays
    autolayout tb 100 100
}
custom "FileHvIncidentCommand-part4" "FileHvIncidentCommand: file an incident through the triage endpoint (activity), part 4 of 5" "Activity diagram converted from fence F41 (line 1231)." {
    include am1FileHvIncidentCommand_SerializableWriteWithAuditLedgerGuardTheOutcome2 am1FileHvIncidentCommand_LastFilingErrorSet am1FileHvIncidentCommand_503AuditUnavailableTheIncidentStays am1FileHvIncidentCommand_FiledReportAndGroupIdsFiledUtc am1FileHvIncidentCommand_NotificationOutboxIncidentFiled
    autolayout lr 250 150
}
custom "FileHvIncidentCommand-part5" "FileHvIncidentCommand: file an incident through the triage endpoint (activity), part 5 of 5" "Activity diagram converted from fence F41 (line 1231)." {
    include am1FileHvIncidentCommand_LastFilingErrorSet am1FileHvIncidentCommand_FilingPendingTheSweepTriesAgain am1FileHvIncidentCommand_NotificationOutboxIncidentFiled am1FileHvIncidentCommand_FiledCommittedWithItsNotification
    autolayout lr 250 150
}
custom "AM1-RecordModelGradeCommand" "RecordModelGradeCommand: record a model grade inside finalization (amended, internal) (activity), part 1 of 4" "Activity diagram converted from fence F42 (line 1265)." {
    include am1AM1RecordModelGradeCommand_RecordModelGradeCommandInsideFinalizeHvRequestCommandSTransaction am1AM1RecordModelGradeCommand_IdempotentCreateLookupResponseHVRequestKind
    autolayout lr 250 150
}
custom "AM1-RecordModelGradeCommand-part2" "RecordModelGradeCommand: record a model grade inside finalization (amended, internal) (activity), part 2 of 4" "Activity diagram converted from fence F42 (line 1265)." {
    include am1AM1RecordModelGradeCommand_IdempotentCreateLookupResponseHVRequestKind am1AM1RecordModelGradeCommand_TheStoredGradeNothingWritten am1AM1RecordModelGradeCommand_409ConflictRetryableFalseTheCaller am1AM1RecordModelGradeCommand_ResponseGradableItsItemPendingRequest
    autolayout lr 250 150
}
custom "AM1-RecordModelGradeCommand-part3" "RecordModelGradeCommand: record a model grade inside finalization (amended, internal) (activity), part 3 of 4" "Activity diagram converted from fence F42 (line 1265)." {
    include am1AM1RecordModelGradeCommand_ResponseGradableItsItemPendingRequest am1AM1RecordModelGradeCommand_409ConflictRetryableTrueTheCaller am1AM1RecordModelGradeCommand_ReadableKeyAllocatorGrdKey am1AM1RecordModelGradeCommand_InsertTheGradeRunReferenceEvidence
    autolayout tb 100 100
}
custom "AM1-RecordModelGradeCommand-part4" "RecordModelGradeCommand: record a model grade inside finalization (amended, internal) (activity), part 4 of 4" "Activity diagram converted from fence F42 (line 1265)." {
    include am1AM1RecordModelGradeCommand_InsertTheGradeRunReferenceEvidence am1AM1RecordModelGradeCommand_ItemGradedWithTheGradeId am1AM1RecordModelGradeCommand_DecideAgentResponseTrustCommandInTheSameTransaction am1AM1RecordModelGradeCommand_GradeRecordedTheHVRequestIs
    autolayout tb 100 200
}
custom "HvScopeResolution" "Policy HvScopeResolution: resolve the member turns (activity), part 1 of 7" "Activity diagram converted from fence F43 (line 1285)." {
    include am1HvScopeResolution_HvScopeResolutionScopeSubmittingTurn am1HvScopeResolution_TurnLocatorTheReviewedSession am1HvScopeResolution_400HvScopeInvalid am1HvScopeResolution_ScopeKind
    autolayout tb 100 100
}
custom "HvScopeResolution-part2" "Policy HvScopeResolution: resolve the member turns (activity), part 2 of 7" "Activity diagram converted from fence F43 (line 1285)." {
    include am1HvScopeResolution_ScopeKind am1HvScopeResolution_TurnLocatorTheTurn am1HvScopeResolution_TurnLocatorBothEndpointTurns am1HvScopeResolution_MembersEveryTurnOfTheSession
    autolayout lr 250 150
}
custom "HvScopeResolution-part3" "Policy HvScopeResolution: resolve the member turns (activity), part 3 of 7" "Activity diagram converted from fence F43 (line 1285)." {
    include am1HvScopeResolution_TurnLocatorTheTurn am1HvScopeResolution_400HvScopeInvalid am1HvScopeResolution_MembersThatTurn am1HvScopeResolution_TurnLocatorBothEndpointTurns am1HvScopeResolution_StartNotAfterEnd
    autolayout tb 100 200
}
custom "HvScopeResolution-part4" "Policy HvScopeResolution: resolve the member turns (activity), part 4 of 7" "Activity diagram converted from fence F43 (line 1285)." {
    include am1HvScopeResolution_MembersEveryTurnOfTheSession am1HvScopeResolution_RemoveTheSubmittingTurnWhenPresent am1HvScopeResolution_MembersThatTurn am1HvScopeResolution_SubmittingTurnAmongTheMembers
    autolayout lr 250 150
}
custom "HvScopeResolution-part5" "Policy HvScopeResolution: resolve the member turns (activity), part 5 of 7" "Activity diagram converted from fence F43 (line 1285)." {
    include am1HvScopeResolution_StartNotAfterEnd am1HvScopeResolution_400HvScopeInvalid am1HvScopeResolution_MembersEveryTurnFromStartTo am1HvScopeResolution_RemoveTheSubmittingTurnWhenPresent am1HvScopeResolution_MembersLeft
    autolayout lr 250 150
}
custom "HvScopeResolution-part6" "Policy HvScopeResolution: resolve the member turns (activity), part 6 of 7" "Activity diagram converted from fence F43 (line 1285)." {
    include am1HvScopeResolution_SubmittingTurnAmongTheMembers am1HvScopeResolution_400HvScopeIncludesSubmittingTurn am1HvScopeResolution_AtMostMaxTurnsPerRequest am1HvScopeResolution_MembersEveryTurnFromStartTo am1HvScopeResolution_MembersLeft am1HvScopeResolution_400HvScopeEmpty
    autolayout tb 100 100
}
custom "HvScopeResolution-part7" "Policy HvScopeResolution: resolve the member turns (activity), part 7 of 7" "Activity diagram converted from fence F43 (line 1285)." {
    include am1HvScopeResolution_AtMostMaxTurnsPerRequest am1HvScopeResolution_400HvScopeTooLargeDetails am1HvScopeResolution_MembersInSessionOrder
    autolayout lr 250 150
}
custom "HvAdmissionEligibility" "Policy HvAdmissionEligibility: admit only reviewable turns (activity), part 1 of 3" "Activity diagram converted from fence F44 (line 1315)." {
    include am1HvAdmissionEligibility_HvAdmissionEligibilityMembers am1HvAdmissionEligibility_ForEachMemberReadStatusAnd am1HvAdmissionEligibility_AnyMemberNotTerminal
    autolayout tb 100 200
}
custom "HvAdmissionEligibility-part2" "Policy HvAdmissionEligibility: admit only reviewable turns (activity), part 2 of 3" "Activity diagram converted from fence F44 (line 1315)." {
    include am1HvAdmissionEligibility_AnyMemberNotTerminal am1HvAdmissionEligibility_409HvTurnsNotTerminalDetails am1HvAdmissionEligibility_AnyMemberPending am1HvAdmissionEligibility_409HvTurnsPendingWithTheir am1HvAdmissionEligibility_AnyMemberFinal
    autolayout tb 100 100
}
custom "HvAdmissionEligibility-part3" "Policy HvAdmissionEligibility: admit only reviewable turns (activity), part 3 of 3" "Activity diagram converted from fence F44 (line 1315)." {
    include am1HvAdmissionEligibility_AnyMemberFinal am1HvAdmissionEligibility_409HvTurnsAlreadyReviewedWith am1HvAdmissionEligibility_Eligible
    autolayout lr 250 150
}
custom "HvReviewerEligibility" "Policy HvReviewerEligibility: who may review (activity), part 1 of 2" "Activity diagram converted from fence F45 (line 1331)." {
    include am1HvReviewerEligibility_HvReviewerEligibilityReviewerSubmitterIdAuthorId am1HvReviewerEligibility_Enabled am1HvReviewerEligibility_NotEligibleDisabled am1HvReviewerEligibility_Online
    autolayout tb 100 100
}
custom "HvReviewerEligibility-part2" "Policy HvReviewerEligibility: who may review (activity), part 2 of 2" "Activity diagram converted from fence F45 (line 1331)." {
    include am1HvReviewerEligibility_Online am1HvReviewerEligibility_NotEligibleReadinessAndReason am1HvReviewerEligibility_AgentIdentityIsSameReviewerWithTheSubmitterId am1HvReviewerEligibility_NotEligibleSubmitterOrAuthor am1HvReviewerEligibility_Eligible
    autolayout tb 100 100
}
custom "RoundRobinAssignment" "Policy RoundRobinAssignment: pick the next reviewer (activity), part 1 of 5" "Activity diagram converted from fence F46 (line 1346)." {
    include am1RoundRobinAssignment_RoundRobinAssignmentSubmitterIdAuthorIdCapacity am1RoundRobinAssignment_StartAfterTheCursorInPool am1RoundRobinAssignment_NextReviewerInCyclicOrderFive
    autolayout tb 300 250
}
custom "RoundRobinAssignment-part2" "Policy RoundRobinAssignment: pick the next reviewer (activity), part 2 of 5" "Activity diagram converted from fence F46 (line 1346)." {
    include am1RoundRobinAssignment_NextReviewerInCyclicOrderFive am1RoundRobinAssignment_409HvNoReviewerAvailableDetails am1RoundRobinAssignment_NoneNothingWrittenCursorAndRequest
    autolayout lr 350 200
}
custom "RoundRobinAssignment-part3" "Policy RoundRobinAssignment: pick the next reviewer (activity), part 3 of 5" "Activity diagram converted from fence F46 (line 1346)." {
    include am1RoundRobinAssignment_NextReviewerInCyclicOrderFive am1RoundRobinAssignment_PolicyHvReviewerEligibility
    autolayout tb 300 250
}
custom "RoundRobinAssignment-part4" "Policy RoundRobinAssignment: pick the next reviewer (activity), part 4 of 5" "Activity diagram converted from fence F46 (line 1346)." {
    include am1RoundRobinAssignment_PolicyHvReviewerEligibility am1RoundRobinAssignment_CapacityFilterPassedOrNoneAsked
    autolayout lr 350 200
}
custom "RoundRobinAssignment-part5" "Policy RoundRobinAssignment: pick the next reviewer (activity), part 5 of 5" "Activity diagram converted from fence F46 (line 1346)." {
    include am1RoundRobinAssignment_CapacityFilterPassedOrNoneAsked am1RoundRobinAssignment_NextReviewerInCyclicOrderFive am1RoundRobinAssignment_SetLastAssignedReviewerIdToTheReviewer am1RoundRobinAssignment_AssignedReviewer
    autolayout tb 300 250
}
custom "HvTurnFreeze" "Policy HvTurnFreeze: freeze, release, finalize, and guard (activity), part 1 of 2" "Activity diagram converted from fence F47 (line 1365)." {
    include am1HvTurnFreeze_HvTurnFreezeGuardTurnWriteOriginRequest am1HvTurnFreeze_TurnHvState
    autolayout lr 250 150
}
custom "HvTurnFreeze-part2" "Policy HvTurnFreeze: freeze, release, finalize, and guard (activity), part 2 of 2" "Activity diagram converted from fence F47 (line 1365)." {
    include am1HvTurnFreeze_TurnHvState am1HvTurnFreeze_Allowed am1HvTurnFreeze_409TurnImmutableReasonHvFinal am1HvTurnFreeze_OriginHvFinalizationForThisTurnS am1HvTurnFreeze_409TurnImmutableReasonHvPending
    autolayout tb 100 100
}
custom "HvFinalVerification" "Policy HvFinalVerification: the eight verification steps (activity), part 1 of 2" "Activity diagram converted from fence F48 (line 1388)." {
    include am1HvFinalVerification_HvFinalVerificationRunChunksRequestMembersResponses am1HvFinalVerification_VerifyLogAndBindingV1V8V am1HvFinalVerification_TheStepSCodeDrawnThere am1HvFinalVerification_VerifyVerdictAndReviewerV4V5V
    autolayout tb 100 100
}
custom "HvFinalVerification-part2" "Policy HvFinalVerification: the eight verification steps (activity), part 2 of 2" "Activity diagram converted from fence F48 (line 1388)." {
    include am1HvFinalVerification_VerifyVerdictAndReviewerV4V5V am1HvFinalVerification_TheStepSCodeDrawnThere2 am1HvFinalVerification_VerifiedRecords
    autolayout lr 250 150
}
custom "VerifyLogAndBinding" "Nested activity: VerifyLogAndBinding (V-1, V-8, V-2, V-3 of HvFinalVerification) (activity), part 1 of 4" "Activity diagram converted from fence F49 (line 1399)." {
    include am1VerifyLogAndBinding_VerifyLogAndBinding am1VerifyLogAndBinding_V1ChunksContiguousHashesAnd am1VerifyLogAndBinding_EvidenceCorrupt am1VerifyLogAndBinding_V8OneVerdictLastOne
    autolayout tb 100 100
}
custom "VerifyLogAndBinding-part2" "Nested activity: VerifyLogAndBinding (V-1, V-8, V-2, V-3 of HvFinalVerification) (activity), part 2 of 4" "Activity diagram converted from fence F49 (line 1399)." {
    include am1VerifyLogAndBinding_V8OneVerdictLastOne am1VerifyLogAndBinding_ReviewerOutputInvalid am1VerifyLogAndBinding_V2AssessmentsBoundToMembers
    autolayout lr 250 150
}
custom "VerifyLogAndBinding-part3" "Nested activity: VerifyLogAndBinding (V-1, V-8, V-2, V-3 of HvFinalVerification) (activity), part 3 of 4" "Activity diagram converted from fence F49 (line 1399)." {
    include am1VerifyLogAndBinding_V2AssessmentsBoundToMembers am1VerifyLogAndBinding_VerdictBindingFailed am1VerifyLogAndBinding_SnapshotChanged am1VerifyLogAndBinding_V3VerdictNamesThisRequest
    autolayout lr 250 150
}
custom "VerifyLogAndBinding-part4" "Nested activity: VerifyLogAndBinding (V-1, V-8, V-2, V-3 of HvFinalVerification) (activity), part 4 of 4" "Activity diagram converted from fence F49 (line 1399)." {
    include am1VerifyLogAndBinding_V3VerdictNamesThisRequest am1VerifyLogAndBinding_VerdictBindingFailed2 am1VerifyLogAndBinding_BoundRecords
    autolayout lr 250 150
}
custom "VerifyVerdictAndReviewer" "Nested activity: VerifyVerdictAndReviewer (V-4 to V-7 of HvFinalVerification) (activity), part 1 of 3" "Activity diagram converted from fence F50 (line 1415)." {
    include am1VerifyVerdictAndReviewer_VerifyVerdictAndReviewerBoundRecords am1VerifyVerdictAndReviewer_V4VerdictsConsistentWithScores am1VerifyVerdictAndReviewer_ReviewerOutputInvalid am1VerifyVerdictAndReviewer_V5ReviewerIdDiffersFrom
    autolayout tb 100 100
}
custom "VerifyVerdictAndReviewer-part2" "Nested activity: VerifyVerdictAndReviewer (V-4 to V-7 of HvFinalVerification) (activity), part 2 of 3" "Activity diagram converted from fence F50 (line 1415)." {
    include am1VerifyVerdictAndReviewer_V5ReviewerIdDiffersFrom am1VerifyVerdictAndReviewer_ReviewerNotIndependent am1VerifyVerdictAndReviewer_V6ReviewerTurnACompleted am1VerifyVerdictAndReviewer_ReviewerTurnUnverified am1VerifyVerdictAndReviewer_V7WorkerProvenancePresentEvery
    autolayout tb 100 100
}
custom "VerifyVerdictAndReviewer-part3" "Nested activity: VerifyVerdictAndReviewer (V-4 to V-7 of HvFinalVerification) (activity), part 3 of 3" "Activity diagram converted from fence F50 (line 1415)." {
    include am1VerifyVerdictAndReviewer_V7WorkerProvenancePresentEvery am1VerifyVerdictAndReviewer_RuntimeIdentityMismatch am1VerifyVerdictAndReviewer_Passed
    autolayout lr 250 150
}
custom "ReadinessProbeVerdict" "Policy ReadinessProbeVerdict: the NONCE decision (activity), part 1 of 3" "Activity diagram converted from fence F51 (line 1432)." {
    include am1ReadinessProbeVerdict_ReadinessProbeVerdictProbeRun am1ReadinessProbeVerdict_ChunksPassV1 am1ReadinessProbeVerdict_FailedEvidenceCorrupt am1ReadinessProbeVerdict_ExactlyOneHvProbeWithThe
    autolayout tb 100 100
}
custom "ReadinessProbeVerdict-part2" "Policy ReadinessProbeVerdict: the NONCE decision (activity), part 2 of 3" "Activity diagram converted from fence F51 (line 1432)." {
    include am1ReadinessProbeVerdict_ExactlyOneHvProbeWithThe am1ReadinessProbeVerdict_FailedProbeNonceMismatch am1ReadinessProbeVerdict_ProvenanceNamesTheExpectedModelAnd am1ReadinessProbeVerdict_FailedRuntimeIdentityMismatch am1ReadinessProbeVerdict_PersistedReviewerTurnForThisReviewer
    autolayout tb 100 100
}
custom "ReadinessProbeVerdict-part3" "Policy ReadinessProbeVerdict: the NONCE decision (activity), part 3 of 3" "Activity diagram converted from fence F51 (line 1432)." {
    include am1ReadinessProbeVerdict_PersistedReviewerTurnForThisReviewer am1ReadinessProbeVerdict_FailedReviewerTurnLoggingUnsupported am1ReadinessProbeVerdict_PassedResolvedModelAndEffort
    autolayout lr 250 150
}
custom "HvProbeOutcome" "Policy HvProbeOutcome: settle a probe run and its reviewer (activity), part 1 of 5" "Activity diagram converted from fence F52 (line 1449)." {
    include am1HvProbeOutcome_HvProbeOutcomeProbeRunOutcomeInThe am1HvProbeOutcome_RunOfTheReviewerSCurrent am1HvProbeOutcome_ReviewerUnchangedNoReviewerIncidentNo am1HvProbeOutcome_Outcome
    autolayout tb 100 100
}
custom "HvProbeOutcome-part2" "Policy HvProbeOutcome: settle a probe run and its reviewer (activity), part 2 of 5" "Activity diagram converted from fence F52 (line 1449)." {
    include am1HvProbeOutcome_ReviewerUnchangedNoReviewerIncidentNo am1HvProbeOutcome_StaleTheNewerConfigurationAndIts
    autolayout lr 250 150
}
custom "HvProbeOutcome-part3" "Policy HvProbeOutcome: settle a probe run and its reviewer (activity), part 3 of 5" "Activity diagram converted from fence F52 (line 1449)." {
    include am1HvProbeOutcome_Outcome am1HvProbeOutcome_RunSucceededReviewerOnlineWithResolved am1HvProbeOutcome_ReviewerOfflineWithTheCodeInsert am1HvProbeOutcome_InsertHvIncidentPendingKeyHvServer am1HvProbeOutcome_SetProbeRequestedUtcTheSameConfigurationIs
    autolayout lr 200 50
}
custom "HvProbeOutcome-part4" "Policy HvProbeOutcome: settle a probe run and its reviewer (activity), part 4 of 5" "Activity diagram converted from fence F52 (line 1449)." {
    include am1HvProbeOutcome_RunSucceededReviewerOnlineWithResolved am1HvProbeOutcome_NotificationOutboxReviewerOnline am1HvProbeOutcome_ReviewerOfflineWithTheCodeInsert am1HvProbeOutcome_NotificationOutboxReviewerOffline am1HvProbeOutcome_InsertHvIncidentPendingKeyHvServer am1HvProbeOutcome_SettledServerFaultNoNewProbe
    autolayout lr 250 150
}
custom "HvProbeOutcome-part5" "Policy HvProbeOutcome: settle a probe run and its reviewer (activity), part 5 of 5" "Activity diagram converted from fence F52 (line 1449)." {
    include am1HvProbeOutcome_SetProbeRequestedUtcTheSameConfigurationIs am1HvProbeOutcome_NotSettledProbeRequestedAgain am1HvProbeOutcome_NotificationOutboxReviewerOnline am1HvProbeOutcome_SettledOnline am1HvProbeOutcome_NotificationOutboxReviewerOffline am1HvProbeOutcome_SettledOffline
    autolayout lr 250 150
}
custom "HvRetryDecision" "Policy HvRetryDecision: requeue, fail, or cancel (activity), part 1 of 5" "Activity diagram converted from fence F53 (line 1471)." {
    include am1HvRetryDecision_HvRetryDecisionRequestInTheCallerS am1HvRetryDecision_CallerSRunTheRequestS am1HvRetryDecision_UnchangedANewerAttemptOrA am1HvRetryDecision_CancelRequested
    autolayout tb 100 100
}
custom "HvRetryDecision-part2" "Policy HvRetryDecision: requeue, fail, or cancel (activity), part 2 of 5" "Activity diagram converted from fence F53 (line 1471)." {
    include am1HvRetryDecision_CancelRequested am1HvRetryDecision_RequestCanceledWithCompletedUtcPendingResponses am1HvRetryDecision_CountedAttemptsWorkerShutdownExcludedBelow am1HvRetryDecision_PolicyHvTurnFreezeRelease
    autolayout tb 100 100
}
custom "HvRetryDecision-part3" "Policy HvRetryDecision: requeue, fail, or cancel (activity), part 3 of 5" "Activity diagram converted from fence F53 (line 1471)." {
    include am1HvRetryDecision_CountedAttemptsWorkerShutdownExcludedBelow am1HvRetryDecision_RequestQueuedAssigneeReEvaluatedAt am1HvRetryDecision_RequestFailedAttemptsExhaustedWithCompletedUtc am1HvRetryDecision_PolicyHvTurnFreezeRelease am1HvRetryDecision_NotificationOutboxHvCanceled
    autolayout lr 250 150
}
custom "HvRetryDecision-part4" "Policy HvRetryDecision: requeue, fail, or cancel (activity), part 4 of 5" "Activity diagram converted from fence F53 (line 1471)." {
    include am1HvRetryDecision_RequestQueuedAssigneeReEvaluatedAt am1HvRetryDecision_RequeueRequestQueued am1HvRetryDecision_RequestFailedAttemptsExhaustedWithCompletedUtc am1HvRetryDecision_PolicyHvTurnFreezeRelease2 am1HvRetryDecision_NotificationOutboxHvCanceled am1HvRetryDecision_CancelRequestCanceled
    autolayout lr 250 150
}
custom "HvRetryDecision-part5" "Policy HvRetryDecision: requeue, fail, or cancel (activity), part 5 of 5" "Activity diagram converted from fence F53 (line 1471)." {
    include am1HvRetryDecision_PolicyHvTurnFreezeRelease2 am1HvRetryDecision_NotificationOutboxHvFailed am1HvRetryDecision_FailRequestFailed
    autolayout tb 100 200
}
custom "HvWorkerHost" "Process HvWorkerHost: the durable worker (activity), part 1 of 8" "Activity diagram converted from fence F54 (line 1499)." {
    include am1HvWorkerHost_HvWorkerHostStarts am1HvWorkerHost_WorkerEnabled am1HvWorkerHost_IdleNoClaims am1HvWorkerHost_StartHvReviewerNonceCommandForEachEnabledUnverifiedReviewer
    autolayout tb 300 250
}
custom "HvWorkerHost-part2" "Process HvWorkerHost: the durable worker (activity), part 2 of 8" "Activity diagram converted from fence F54 (line 1499)." {
    include am1HvWorkerHost_StartHvReviewerNonceCommandForEachEnabledUnverifiedReviewer am1HvWorkerHost_WaitForAWakeUpOr am1HvWorkerHost_ShuttingDown
    autolayout tb 300 250
}
custom "HvWorkerHost-part3" "Process HvWorkerHost: the durable worker (activity), part 3 of 8" "Activity diagram converted from fence F54 (line 1499)." {
    include am1HvWorkerHost_ShuttingDown am1HvWorkerHost_FailAttemptWorkerShutdownForEachOwn am1HvWorkerHost_ExpireHvRunLeasesCommand am1HvWorkerHost_Stopped
    autolayout tb 300 250
}
custom "HvWorkerHost-part4" "Process HvWorkerHost: the durable worker (activity), part 4 of 8" "Activity diagram converted from fence F54 (line 1499)." {
    include am1HvWorkerHost_ExpireHvRunLeasesCommand am1HvWorkerHost_WaitForAWakeUpOr am1HvWorkerHost_FinalizeHvRequestCommandForEachFinalizingRequest
    autolayout tb 300 250
}
custom "HvWorkerHost-part5" "Process HvWorkerHost: the durable worker (activity), part 5 of 8" "Activity diagram converted from fence F54 (line 1499)." {
    include am1HvWorkerHost_FinalizeHvRequestCommandForEachFinalizingRequest am1HvWorkerHost_WaitForAWakeUpOr am1HvWorkerHost_FileHvIncidentCommandForEachPendingIncident
    autolayout tb 300 250
}
custom "HvWorkerHost-part6" "Process HvWorkerHost: the durable worker (activity), part 6 of 8" "Activity diagram converted from fence F54 (line 1499)." {
    include am1HvWorkerHost_FileHvIncidentCommandForEachPendingIncident am1HvWorkerHost_WaitForAWakeUpOr am1HvWorkerHost_StartHvReviewerNonceCommandAgainForEachStartupRequest
    autolayout tb 300 250
}
custom "HvWorkerHost-part7" "Process HvWorkerHost: the durable worker (activity), part 7 of 8" "Activity diagram converted from fence F54 (line 1499)." {
    include am1HvWorkerHost_StartHvReviewerNonceCommandAgainForEachStartupRequest am1HvWorkerHost_WaitForAWakeUpOr am1HvWorkerHost_ClaimHvRunCommand
    autolayout tb 300 250
}
custom "HvWorkerHost-part8" "Process HvWorkerHost: the durable worker (activity), part 8 of 8" "Activity diagram converted from fence F54 (line 1499)." {
    include am1HvWorkerHost_ClaimHvRunCommand am1HvWorkerHost_WaitForAWakeUpOr am1HvWorkerHost_ProcessRunReviewerProcessStartedForThisClaim
    autolayout tb 300 250
}
custom "RunReviewerProcess" "Process RunReviewerProcess: one attempt, live (activity), part 1 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_RunReviewerProcessClaim am1RunReviewerProcess_RunKind am1RunReviewerProcess_AuditLedgerGuardRecordReadTheRequestEvidence am1RunReviewerProcess_BuildTheProbeBriefWithIts
    autolayout tb 300 250
}
custom "RunReviewerProcess-part2" "Process RunReviewerProcess: one attempt, live (activity), part 2 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_AuditLedgerGuardRecordReadTheRequestEvidence am1RunReviewerProcess_AttemptEndedBeforeLaunchNoFurther am1RunReviewerProcess_BuildTheBriefAcceptedRequestIn
    autolayout lr 350 200
}
custom "RunReviewerProcess-part3" "Process RunReviewerProcess: one attempt, live (activity), part 3 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_BuildTheProbeBriefWithIts am1RunReviewerProcess_ReviewerAdapterBuildLaunch am1RunReviewerProcess_BuildTheBriefAcceptedRequestIn am1RunReviewerProcess_FailAttemptEvidenceCorruptAServerFault
    autolayout lr 350 200
}
custom "RunReviewerProcess-part4" "Process RunReviewerProcess: one attempt, live (activity), part 4 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_ReviewerAdapterBuildLaunch am1RunReviewerProcess_ProcessStarted am1RunReviewerProcess_FailAttemptEvidenceCorruptAServerFault am1RunReviewerProcess_AttemptEndedBeforeLaunchAsFailAttempt
    autolayout lr 350 200
}
custom "RunReviewerProcess-part5" "Process RunReviewerProcess: one attempt, live (activity), part 5 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_ProcessStarted am1RunReviewerProcess_FailAttemptReviewerLaunchFailed am1RunReviewerProcess_RecordHvRunStartCommandRunningStartedUtcProcessId am1RunReviewerProcess_AttemptEndedAsFailAttemptAnswers
    autolayout tb 200 250
}
custom "RunReviewerProcess-part6" "Process RunReviewerProcess: one attempt, live (activity), part 6 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_RecordHvRunStartCommandRunningStartedUtcProcessId am1RunReviewerProcess_KillTheProcess am1RunReviewerProcess_AppendBatchSequence1TheHvBrief am1RunReviewerProcess_AttemptEndedNoFurtherWrite
    autolayout tb 300 250
}
custom "RunReviewerProcess-part7" "Process RunReviewerProcess: one attempt, live (activity), part 7 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_AppendBatchSequence1TheHvBrief am1RunReviewerProcess_KillTheProcess2 am1RunReviewerProcess_KillTheProcess3 am1RunReviewerProcess_StreamReviewerOutputLinesFlushesHeartbeatsUntilExit
    autolayout tb 200 250
}
custom "RunReviewerProcess-part8" "Process RunReviewerProcess: one attempt, live (activity), part 8 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_KillTheProcess2 am1RunReviewerProcess_AttemptEndedNoFurtherWrite2 am1RunReviewerProcess_KillTheProcess3 am1RunReviewerProcess_FailAttemptEvidenceCorruptAServerFault2
    autolayout lr 350 200
}
custom "RunReviewerProcess-part9" "Process RunReviewerProcess: one attempt, live (activity), part 9 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_StreamReviewerOutputLinesFlushesHeartbeatsUntilExit am1RunReviewerProcess_KillTheProcess2 am1RunReviewerProcess_KillTheProcess4 am1RunReviewerProcess_KillTheProcess3
    autolayout lr 350 200
}
custom "RunReviewerProcess-part10" "Process RunReviewerProcess: one attempt, live (activity), part 10 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_StreamReviewerOutputLinesFlushesHeartbeatsUntilExit am1RunReviewerProcess_AppendBatchRemainingLinesThenTheAdapter
    autolayout lr 350 200
}
custom "RunReviewerProcess-part11" "Process RunReviewerProcess: one attempt, live (activity), part 11 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_FailAttemptEvidenceCorruptAServerFault2 am1RunReviewerProcess_AttemptEndedAsFailAttemptAnswers2 am1RunReviewerProcess_KillTheProcess4 am1RunReviewerProcess_FailAttemptCanceled
    autolayout lr 350 200
}
custom "RunReviewerProcess-part12" "Process RunReviewerProcess: one attempt, live (activity), part 12 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_AppendBatchRemainingLinesThenTheAdapter am1RunReviewerProcess_AttemptEndedNoFurtherWrite3 am1RunReviewerProcess_FailAttemptEvidenceCorruptAServerFault2 am1RunReviewerProcess_ExitCode0
    autolayout lr 350 200
}
custom "RunReviewerProcess-part13" "Process RunReviewerProcess: one attempt, live (activity), part 13 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_FailAttemptCanceled am1RunReviewerProcess_AttemptEndedAsFailAttemptAnswersRequest
    autolayout lr 350 200
}
custom "RunReviewerProcess-part14" "Process RunReviewerProcess: one attempt, live (activity), part 14 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_ExitCode0 am1RunReviewerProcess_FailAttemptReviewerExitedNonzeroTheExit am1RunReviewerProcess_CloseHvRunOutputCommand am1RunReviewerProcess_AttemptEndedAsFailAttemptAnswers3
    autolayout tb 200 250
}
custom "RunReviewerProcess-part15" "Process RunReviewerProcess: one attempt, live (activity), part 15 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_CloseHvRunOutputCommand am1RunReviewerProcess_AppendBatchRemainingLinesThenTheAdapter am1RunReviewerProcess_FinalizeHvRequestCommand am1RunReviewerProcess_AttemptEndedRequestCanceled
    autolayout lr 350 200
}
custom "RunReviewerProcess-part16" "Process RunReviewerProcess: one attempt, live (activity), part 16 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_CloseHvRunOutputCommand am1RunReviewerProcess_AttemptEndedReviewerOnlineOfflineOr am1RunReviewerProcess_AttemptEndedNoFurtherWrite4
    autolayout lr 350 200
}
custom "RunReviewerProcess-part17" "Process RunReviewerProcess: one attempt, live (activity), part 17 of 17" "Activity diagram converted from fence F55 (line 1526)." {
    include am1RunReviewerProcess_FinalizeHvRequestCommand am1RunReviewerProcess_AttemptEndedRequestCompletedQueuedOr
    autolayout lr 350 200
}
custom "FailAttempt" "Nested activity: FailAttempt (every FailHvRunCommand call of the worker, drawn once) (activity)" "Activity diagram converted from fence F56 (line 1574)." {
    include am1FailAttempt_FailAttemptRunFenceFailureCodeDetail am1FailAttempt_FailHvRunCommand am1FailAttempt_AttemptEndedTheStateFailHvRunCommandWrote am1FailAttempt_AttemptEndedNoFurtherWriteThe
    autolayout tb 100 100
}
custom "AppendBatch" "Nested activity: AppendBatch (every AppendHvRunOutputCommand call of the worker, drawn once) (activity), part 1 of 2" "Activity diagram converted from fence F57 (line 1584)." {
    include am1AppendBatch_AppendBatchRunFenceSequenceLinesCompleted am1AppendBatch_AppendHvRunOutputCommand
    autolayout lr 250 150
}
custom "AppendBatch-part2" "Nested activity: AppendBatch (every AppendHvRunOutputCommand call of the worker, drawn once) (activity), part 2 of 2" "Activity diagram converted from fence F57 (line 1584)." {
    include am1AppendBatch_AppendHvRunOutputCommand am1AppendBatch_AppendedTheCallerGoesOnWith am1AppendBatch_StopNoFurtherWrite am1AppendBatch_StopForIntegrityTheCallerEnds
    autolayout lr 250 150
}
custom "StreamReviewerOutput" "Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 1 of 10" "Activity diagram converted from fence F58 (line 1595)." {
    include am1StreamReviewerOutput_StreamReviewerOutput am1StreamReviewerOutput_NextEvent
    autolayout lr 350 200
}
custom "StreamReviewerOutput-part2" "Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 2 of 10" "Activity diagram converted from fence F58 (line 1595)." {
    include am1StreamReviewerOutput_NextEvent am1StreamReviewerOutput_AddTheJSONLLineToThe am1StreamReviewerOutput_AppendBatchTheBufferAsTheNext
    autolayout tb 300 250
}
custom "StreamReviewerOutput-part3" "Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 3 of 10" "Activity diagram converted from fence F58 (line 1595)." {
    include am1StreamReviewerOutput_NextEvent am1StreamReviewerOutput_RecordHvRunHeartbeatCommand am1StreamReviewerOutput_StopExitedWithTheExitCode
    autolayout tb 300 250
}
custom "StreamReviewerOutput-part4" "Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 4 of 10" "Activity diagram converted from fence F58 (line 1595)." {
    include am1StreamReviewerOutput_AddTheJSONLLineToThe am1StreamReviewerOutput_FlushBytesReached
    autolayout lr 350 200
}
custom "StreamReviewerOutput-part5" "Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 5 of 10" "Activity diagram converted from fence F58 (line 1595)." {
    include am1StreamReviewerOutput_AppendBatchTheBufferAsTheNext am1StreamReviewerOutput_NextEvent am1StreamReviewerOutput_StopStaleFenceOrAuditFailure
    autolayout tb 300 250
}
custom "StreamReviewerOutput-part6" "Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 6 of 10" "Activity diagram converted from fence F58 (line 1595)." {
    include am1StreamReviewerOutput_AppendBatchTheBufferAsTheNext am1StreamReviewerOutput_StopTheLogSIntegrityFails
    autolayout lr 350 200
}
custom "StreamReviewerOutput-part7" "Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 7 of 10" "Activity diagram converted from fence F58 (line 1595)." {
    include am1StreamReviewerOutput_RecordHvRunHeartbeatCommand am1StreamReviewerOutput_NextEvent am1StreamReviewerOutput_StopCancelRequested
    autolayout tb 300 250
}
custom "StreamReviewerOutput-part8" "Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 8 of 10" "Activity diagram converted from fence F58 (line 1595)." {
    include am1StreamReviewerOutput_RecordHvRunHeartbeatCommand am1StreamReviewerOutput_StopStaleFenceOrAuditFailure
    autolayout lr 350 200
}
custom "StreamReviewerOutput-part9" "Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 9 of 10" "Activity diagram converted from fence F58 (line 1595)." {
    include am1StreamReviewerOutput_FlushBytesReached am1StreamReviewerOutput_NextEvent
    autolayout lr 350 200
}
custom "StreamReviewerOutput-part10" "Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 10 of 10" "Activity diagram converted from fence F58 (line 1595)." {
    include am1StreamReviewerOutput_FlushBytesReached am1StreamReviewerOutput_AppendBatchTheBufferAsTheNext
    autolayout lr 350 200
}
custom "SubmitHvFromPlugin" "Process SubmitHvFromPlugin: the plugin verb (activity), part 1 of 4" "Activity diagram converted from fence F59 (line 1629)." {
    include am1SubmitHvFromPlugin_WorkflowHvSubmitScopeContext am1SubmitHvFromPlugin_ProcessBindVerbToTurn am1SubmitHvFromPlugin_RefusalOfBindVerbToTurnDrawnThere am1SubmitHvFromPlugin_SubmitterTheBoundTurnKeyIdempotencyKey
    autolayout tb 100 100
}
custom "SubmitHvFromPlugin-part2" "Process SubmitHvFromPlugin: the plugin verb (activity), part 2 of 4" "Activity diagram converted from fence F59 (line 1629)." {
    include am1SubmitHvFromPlugin_SubmitterTheBoundTurnKeyIdempotencyKey am1SubmitHvFromPlugin_REPLSubmitHvRequestCommand
    autolayout lr 250 150
}
custom "SubmitHvFromPlugin-part3" "Process SubmitHvFromPlugin: the plugin verb (activity), part 3 of 4" "Activity diagram converted from fence F59 (line 1629)." {
    include am1SubmitHvFromPlugin_REPLSubmitHvRequestCommand am1SubmitHvFromPlugin_ReportedIdStatusReviewer am1SubmitHvFromPlugin_AddReviewerStatusesOpenIncidentsAnd am1SubmitHvFromPlugin_ReportedCodeRetryableTrueExit1 am1SubmitHvFromPlugin_ReportedCodeFinalExit1
    autolayout lr 200 50
}
custom "SubmitHvFromPlugin-part4" "Process SubmitHvFromPlugin: the plugin verb (activity), part 4 of 4" "Activity diagram converted from fence F59 (line 1629)." {
    include am1SubmitHvFromPlugin_AddReviewerStatusesOpenIncidentsAnd am1SubmitHvFromPlugin_ReportedRefusalWithContextExit1
    autolayout lr 250 150
}
custom "WatchHvNotifications" "Process WatchHvNotifications: completions into the plugin (activity), part 1 of 5" "Activity diagram converted from fence F60 (line 1646)." {
    include am1WatchHvNotifications_WatchHvNotificationsHostSession am1WatchHvNotifications_WatcherAlreadyRunningForThisHost am1WatchHvNotifications_ExitOneInstance am1WatchHvNotifications_REPLWorkflowHvWatchNotificationsFromLastSequence
    autolayout tb 100 100
}
custom "WatchHvNotifications-part2" "Process WatchHvNotifications: completions into the plugin (activity), part 2 of 5" "Activity diagram converted from fence F60 (line 1646)." {
    include am1WatchHvNotifications_REPLWorkflowHvWatchNotificationsFromLastSequence am1WatchHvNotifications_NextEvent
    autolayout lr 250 150
}
custom "WatchHvNotifications-part3" "Process WatchHvNotifications: completions into the plugin (activity), part 3 of 5" "Activity diagram converted from fence F60 (line 1646)." {
    include am1WatchHvNotifications_NextEvent am1WatchHvNotifications_TerminalKindSubmitterSessionThisHost am1WatchHvNotifications_WaitWithBackoff am1WatchHvNotifications_Stopped
    autolayout lr 250 150
}
custom "WatchHvNotifications-part4" "Process WatchHvNotifications: completions into the plugin (activity), part 4 of 5" "Activity diagram converted from fence F60 (line 1646)." {
    include am1WatchHvNotifications_TerminalKindSubmitterSessionThisHost am1WatchHvNotifications_InboxEntryBySequenceAndLastSequence am1WatchHvNotifications_RecordLastSequenceOneAtomicReplaceOf am1WatchHvNotifications_WaitWithBackoff am1WatchHvNotifications_REPLWorkflowHvWatchNotificationsFromLastSequence
    autolayout lr 250 150
}
custom "WatchHvNotifications-part5" "Process WatchHvNotifications: completions into the plugin (activity), part 5 of 5" "Activity diagram converted from fence F60 (line 1646)." {
    include am1WatchHvNotifications_InboxEntryBySequenceAndLastSequence am1WatchHvNotifications_NextEvent am1WatchHvNotifications_RecordLastSequenceOneAtomicReplaceOf
    autolayout lr 250 150
}
custom "HostileReviewWorkerHost" "Process HostileReviewWorkerHost: the artifact worker of FR-MCP-HOSTILEREVIEW-003 (P1 to P3, D-02) (activity), part 1 of 4" "Activity diagram converted from fence F61 (line 1685)." {
    include am1HostileReviewWorkerHost_HostileReviewWorkerHostStarts am1HostileReviewWorkerHost_WaitForAWakeUpOr am1HostileReviewWorkerHost_ShuttingDown
    autolayout tb 100 200
}
custom "HostileReviewWorkerHost-part2" "Process HostileReviewWorkerHost: the artifact worker of FR-MCP-HOSTILEREVIEW-003 (P1 to P3, D-02) (activity), part 2 of 4" "Activity diagram converted from fence F61 (line 1685)." {
    include am1HostileReviewWorkerHost_ShuttingDown am1HostileReviewWorkerHost_ForEachOwnRunningAttemptStop am1HostileReviewWorkerHost_ExpireHostileReviewLeasesCommand am1HostileReviewWorkerHost_Stopped
    autolayout tb 100 100
}
custom "HostileReviewWorkerHost-part3" "Process HostileReviewWorkerHost: the artifact worker of FR-MCP-HOSTILEREVIEW-003 (P1 to P3, D-02) (activity), part 3 of 4" "Activity diagram converted from fence F61 (line 1685)." {
    include am1HostileReviewWorkerHost_ExpireHostileReviewLeasesCommand am1HostileReviewWorkerHost_WaitForAWakeUpOr am1HostileReviewWorkerHost_FewerThanMaximumConcurrentReviewsOwnAttemptsRunning am1HostileReviewWorkerHost_ClaimHostileReviewAttemptCommand
    autolayout tb 100 200
}
custom "HostileReviewWorkerHost-part4" "Process HostileReviewWorkerHost: the artifact worker of FR-MCP-HOSTILEREVIEW-003 (P1 to P3, D-02) (activity), part 4 of 4" "Activity diagram converted from fence F61 (line 1685)." {
    include am1HostileReviewWorkerHost_ClaimHostileReviewAttemptCommand am1HostileReviewWorkerHost_WaitForAWakeUpOr am1HostileReviewWorkerHost_RunHostileReviewAttemptStartedForThisAttempt am1HostileReviewWorkerHost_FewerThanMaximumConcurrentReviewsOwnAttemptsRunning
    autolayout tb 100 200
}
custom "RunHostileReviewAttempt" "Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 1 of 12" "Activity diagram converted from fence F62 (line 1704)." {
    include am1RunHostileReviewAttempt_RunHostileReviewAttemptClaim am1RunHostileReviewAttempt_EveryLinkReauthorizedAndWithinThe am1RunHostileReviewAttempt_CompleteAttemptFailureWithTheLinkS am1RunHostileReviewAttempt_SelectedAgentHasAReviewerAdapter
    autolayout tb 300 250
}
custom "RunHostileReviewAttempt-part2" "Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 2 of 12" "Activity diagram converted from fence F62 (line 1704)." {
    include am1RunHostileReviewAttempt_CompleteAttemptFailureWithTheLinkS am1RunHostileReviewAttempt_AttemptEndedNoProcessLaunchedAs
    autolayout lr 350 200
}
custom "RunHostileReviewAttempt-part3" "Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 3 of 12" "Activity diagram converted from fence F62 (line 1704)." {
    include am1RunHostileReviewAttempt_SelectedAgentHasAReviewerAdapter am1RunHostileReviewAttempt_CompleteAttemptFailureRuntimeIdentityUnavailableWith am1RunHostileReviewAttempt_AssembleThePromptInMemoryCreate am1RunHostileReviewAttempt_AttemptEndedNoProcessLaunchedAs2
    autolayout tb 300 250
}
custom "RunHostileReviewAttempt-part4" "Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 4 of 12" "Activity diagram converted from fence F62 (line 1704)." {
    include am1RunHostileReviewAttempt_AssembleThePromptInMemoryCreate am1RunHostileReviewAttempt_ProcessStarted am1RunHostileReviewAttempt_CompleteAttemptFailureLaunchFailed am1RunHostileReviewAttempt_NextEvent
    autolayout tb 300 250
}
custom "RunHostileReviewAttempt-part5" "Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 5 of 12" "Activity diagram converted from fence F62 (line 1704)." {
    include am1RunHostileReviewAttempt_CompleteAttemptFailureLaunchFailed am1RunHostileReviewAttempt_AttemptEndedAsCompleteAttemptAnswers
    autolayout lr 350 200
}
custom "RunHostileReviewAttempt-part6" "Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 6 of 12" "Activity diagram converted from fence F62 (line 1704)." {
    include am1RunHostileReviewAttempt_NextEvent am1RunHostileReviewAttempt_RenewHostileReviewAttemptCommandProcessStarted am1RunHostileReviewAttempt_StopTheReviewerSProcessTree2
    autolayout tb 300 250
}
custom "RunHostileReviewAttempt-part7" "Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 7 of 12" "Activity diagram converted from fence F62 (line 1704)." {
    include am1RunHostileReviewAttempt_NextEvent am1RunHostileReviewAttempt_ReadTheOutputLinesOnlyTo
    autolayout lr 350 200
}
custom "RunHostileReviewAttempt-part8" "Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 8 of 12" "Activity diagram converted from fence F62 (line 1704)." {
    include am1RunHostileReviewAttempt_RenewHostileReviewAttemptCommandProcessStarted am1RunHostileReviewAttempt_NextEvent am1RunHostileReviewAttempt_StopTheReviewerSProcessTree
    autolayout tb 300 250
}
custom "RunHostileReviewAttempt-part9" "Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 9 of 12" "Activity diagram converted from fence F62 (line 1704)." {
    include am1RunHostileReviewAttempt_StopTheReviewerSProcessTree2 am1RunHostileReviewAttempt_CompleteAttemptCanceledCounted am1RunHostileReviewAttempt_ReadTheOutputLinesOnlyTo am1RunHostileReviewAttempt_ProvenanceShowsTheModelAndEffort
    autolayout lr 350 200
}
custom "RunHostileReviewAttempt-part10" "Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 10 of 12" "Activity diagram converted from fence F62 (line 1704)." {
    include am1RunHostileReviewAttempt_StopTheReviewerSProcessTree am1RunHostileReviewAttempt_AttemptEndedTheSweepOrThe am1RunHostileReviewAttempt_CompleteAttemptCanceledCounted am1RunHostileReviewAttempt_AttemptEndedAsCompleteAttemptAnswers2
    autolayout lr 350 200
}
custom "RunHostileReviewAttempt-part11" "Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 11 of 12" "Activity diagram converted from fence F62 (line 1704)." {
    include am1RunHostileReviewAttempt_ProvenanceShowsTheModelAndEffort am1RunHostileReviewAttempt_CompleteAttemptTheParsedResultOrThe am1RunHostileReviewAttempt_CompleteAttemptFailureRuntimeIdentityUnavailableNo am1RunHostileReviewAttempt_AttemptEndedAsCompleteAttemptAnswers3
    autolayout tb 300 250
}
custom "RunHostileReviewAttempt-part12" "Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 12 of 12" "Activity diagram converted from fence F62 (line 1704)." {
    include am1RunHostileReviewAttempt_CompleteAttemptFailureRuntimeIdentityUnavailableNo am1RunHostileReviewAttempt_AttemptEndedAsCompleteAttemptAnswers4
    autolayout lr 350 200
}
custom "CompleteAttempt" "Nested activity: CompleteAttempt (every CompleteHostileReviewAttemptCommand call of the worker, drawn once) (activity)" "Activity diagram converted from fence F63 (line 1735)." {
    include am1CompleteAttempt_CompleteAttemptAttemptFenceOutcome am1CompleteAttempt_CompleteHostileReviewAttemptCommand am1CompleteAttempt_AttemptEndedWithTheStateThat am1CompleteAttempt_AttemptEndedNoFurtherWriteLeft
    autolayout tb 100 100
}
custom "ClaimHostileReviewAttemptCommand" "ClaimHostileReviewAttemptCommand: claim the next queued artifact review (activity), part 1 of 4" "Activity diagram converted from fence F64 (line 1749)." {
    include am1ClaimHostileReviewAttemptCommand_ClaimHostileReviewAttemptCommandWorkerId am1ClaimHostileReviewAttemptCommand_SerializableWriteWithAuditLedgerGuard
    autolayout lr 250 150
}
custom "ClaimHostileReviewAttemptCommand-part2" "ClaimHostileReviewAttemptCommand: claim the next queued artifact review (activity), part 2 of 4" "Activity diagram converted from fence F64 (line 1749)." {
    include am1ClaimHostileReviewAttemptCommand_SerializableWriteWithAuditLedgerGuard am1ClaimHostileReviewAttemptCommand_503AuditUnavailable am1ClaimHostileReviewAttemptCommand_409ConflictRetryableTrue am1ClaimHostileReviewAttemptCommand_OldestQueuedRequestWithoutAnAccepted
    autolayout lr 250 150
}
custom "ClaimHostileReviewAttemptCommand-part3" "ClaimHostileReviewAttemptCommand: claim the next queued artifact review (activity), part 3 of 4" "Activity diagram converted from fence F64 (line 1749)." {
    include am1ClaimHostileReviewAttemptCommand_OldestQueuedRequestWithoutAnAccepted am1ClaimHostileReviewAttemptCommand_NullNothingClaimable am1ClaimHostileReviewAttemptCommand_InsertTheAttemptClaimedAttemptIdAttempt am1ClaimHostileReviewAttemptCommand_FencedLeaseClaim
    autolayout tb 100 100
}
custom "ClaimHostileReviewAttemptCommand-part4" "ClaimHostileReviewAttemptCommand: claim the next queued artifact review (activity), part 4 of 4" "Activity diagram converted from fence F64 (line 1749)." {
    include am1ClaimHostileReviewAttemptCommand_FencedLeaseClaim am1ClaimHostileReviewAttemptCommand_RequestRunningAttemptCountPlus1 am1ClaimHostileReviewAttemptCommand_ClaimRequestAttemptFenceLeaseExpiry
    autolayout tb 100 200
}
custom "RenewHostileReviewAttemptCommand" "RenewHostileReviewAttemptCommand: keep a running artifact review's lease (activity), part 1 of 4" "Activity diagram converted from fence F65 (line 1768)." {
    include am1RenewHostileReviewAttemptCommand_RenewHostileReviewAttemptCommandAttemptWorkerFenceProcessStarted am1RenewHostileReviewAttemptCommand_SerializableWriteWithAuditLedgerGuard
    autolayout lr 250 150
}
custom "RenewHostileReviewAttemptCommand-part2" "RenewHostileReviewAttemptCommand: keep a running artifact review's lease (activity), part 2 of 4" "Activity diagram converted from fence F65 (line 1768)." {
    include am1RenewHostileReviewAttemptCommand_SerializableWriteWithAuditLedgerGuard am1RenewHostileReviewAttemptCommand_503AuditUnavailable am1RenewHostileReviewAttemptCommand_409ConflictRetryableTrue am1RenewHostileReviewAttemptCommand_FencedLeaseRenew
    autolayout lr 250 150
}
custom "RenewHostileReviewAttemptCommand-part3" "RenewHostileReviewAttemptCommand: keep a running artifact review's lease (activity), part 3 of 4" "Activity diagram converted from fence F65 (line 1768)." {
    include am1RenewHostileReviewAttemptCommand_FencedLeaseRenew am1RenewHostileReviewAttemptCommand_StaleFenceNothingChanged am1RenewHostileReviewAttemptCommand_ProcessStartedAndAttemptClaimed
    autolayout lr 250 150
}
custom "RenewHostileReviewAttemptCommand-part4" "RenewHostileReviewAttemptCommand: keep a running artifact review's lease (activity), part 4 of 4" "Activity diagram converted from fence F65 (line 1768)." {
    include am1RenewHostileReviewAttemptCommand_ProcessStartedAndAttemptClaimed am1RenewHostileReviewAttemptCommand_AttemptRunning am1RenewHostileReviewAttemptCommand_RenewedLeaseExpiresUtc
    autolayout tb 100 200
}
custom "CompleteHostileReviewAttemptCommand" "CompleteHostileReviewAttemptCommand: accept one terminal result (activity), part 1 of 6" "Activity diagram converted from fence F66 (line 1787)." {
    include am1CompleteHostileReviewAttemptCommand_CompleteHostileReviewAttemptCommandAttemptFenceOutcome am1CompleteHostileReviewAttemptCommand_SerializableWriteWithAuditLedgerGuard
    autolayout lr 250 150
}
custom "CompleteHostileReviewAttemptCommand-part2" "CompleteHostileReviewAttemptCommand: accept one terminal result (activity), part 2 of 6" "Activity diagram converted from fence F66 (line 1787)." {
    include am1CompleteHostileReviewAttemptCommand_SerializableWriteWithAuditLedgerGuard am1CompleteHostileReviewAttemptCommand_503AuditUnavailable am1CompleteHostileReviewAttemptCommand_409ConflictRetryableTrue am1CompleteHostileReviewAttemptCommand_RequestAlreadyHasAnAcceptedResult
    autolayout lr 250 150
}
custom "CompleteHostileReviewAttemptCommand-part3" "CompleteHostileReviewAttemptCommand: accept one terminal result (activity), part 3 of 6" "Activity diagram converted from fence F66 (line 1787)." {
    include am1CompleteHostileReviewAttemptCommand_RequestAlreadyHasAnAcceptedResult am1CompleteHostileReviewAttemptCommand_AlreadyAcceptedTheAcceptedResultNothingWritten am1CompleteHostileReviewAttemptCommand_FencedLeaseGuard am1CompleteHostileReviewAttemptCommand_StaleFenceNothingChanged am1CompleteHostileReviewAttemptCommand_StoreTheOutcomeSSanitizedDiagnostics
    autolayout tb 100 100
}
custom "CompleteHostileReviewAttemptCommand-part4" "CompleteHostileReviewAttemptCommand: accept one terminal result (activity), part 4 of 6" "Activity diagram converted from fence F66 (line 1787)." {
    include am1CompleteHostileReviewAttemptCommand_StoreTheOutcomeSSanitizedDiagnostics am1CompleteHostileReviewAttemptCommand_Outcome am1CompleteHostileReviewAttemptCommand_WriteTheResultOnceAndSet am1CompleteHostileReviewAttemptCommand_AttemptFailedWithItsCodeOr
    autolayout tb 100 100
}
custom "CompleteHostileReviewAttemptCommand-part5" "CompleteHostileReviewAttemptCommand: accept one terminal result (activity), part 5 of 6" "Activity diagram converted from fence F66 (line 1787)." {
    include am1CompleteHostileReviewAttemptCommand_WriteTheResultOnceAndSet am1CompleteHostileReviewAttemptCommand_FencedLeaseRelease am1CompleteHostileReviewAttemptCommand_AttemptFailedWithItsCodeOr am1CompleteHostileReviewAttemptCommand_FencedLeaseRelease2 am1CompleteHostileReviewAttemptCommand_Accepted am1CompleteHostileReviewAttemptCommand_CountedAttemptsBelowMaximumAttempts
    autolayout tb 100 100
}
custom "CompleteHostileReviewAttemptCommand-part6" "CompleteHostileReviewAttemptCommand: accept one terminal result (activity), part 6 of 6" "Activity diagram converted from fence F66 (line 1787)." {
    include am1CompleteHostileReviewAttemptCommand_CountedAttemptsBelowMaximumAttempts am1CompleteHostileReviewAttemptCommand_RequestQueuedAgain am1CompleteHostileReviewAttemptCommand_RequestFailed am1CompleteHostileReviewAttemptCommand_Requeued am1CompleteHostileReviewAttemptCommand_Failed
    autolayout tb 100 100
}
custom "ExpireHostileReviewLeasesCommand" "ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 1 of 7" "Activity diagram converted from fence F67 (line 1816)." {
    include am1ExpireHostileReviewLeasesCommand_ExpireHostileReviewLeasesCommand am1ExpireHostileReviewLeasesCommand_NextAttemptReadClaimedOrRunning
    autolayout lr 250 150
}
custom "ExpireHostileReviewLeasesCommand-part2" "ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 2 of 7" "Activity diagram converted from fence F67 (line 1816)." {
    include am1ExpireHostileReviewLeasesCommand_NextAttemptReadClaimedOrRunning am1ExpireHostileReviewLeasesCommand_SweepResult am1ExpireHostileReviewLeasesCommand_SerializableWriteWithAuditLedgerGuardOnePerAttempt
    autolayout tb 100 100
}
custom "ExpireHostileReviewLeasesCommand-part3" "ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 3 of 7" "Activity diagram converted from fence F67 (line 1816)." {
    include am1ExpireHostileReviewLeasesCommand_SerializableWriteWithAuditLedgerGuardOnePerAttempt am1ExpireHostileReviewLeasesCommand_503AuditUnavailableSweepEnds am1ExpireHostileReviewLeasesCommand_NextAttemptReadClaimedOrRunning
    autolayout tb 100 100
}
custom "ExpireHostileReviewLeasesCommand-part4" "ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 4 of 7" "Activity diagram converted from fence F67 (line 1816)." {
    include am1ExpireHostileReviewLeasesCommand_SerializableWriteWithAuditLedgerGuardOnePerAttempt am1ExpireHostileReviewLeasesCommand_FencedLeaseExpireStillClaimedOrRunning
    autolayout lr 250 150
}
custom "ExpireHostileReviewLeasesCommand-part5" "ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 5 of 7" "Activity diagram converted from fence F67 (line 1816)." {
    include am1ExpireHostileReviewLeasesCommand_FencedLeaseExpireStillClaimedOrRunning am1ExpireHostileReviewLeasesCommand_NextAttemptReadClaimedOrRunning am1ExpireHostileReviewLeasesCommand_AttemptAbandonedWithEndedUtcAndLease am1ExpireHostileReviewLeasesCommand_RequestHasAnAcceptedResult
    autolayout tb 100 200
}
custom "ExpireHostileReviewLeasesCommand-part6" "ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 6 of 7" "Activity diagram converted from fence F67 (line 1816)." {
    include am1ExpireHostileReviewLeasesCommand_RequestHasAnAcceptedResult am1ExpireHostileReviewLeasesCommand_NextAttemptReadClaimedOrRunning am1ExpireHostileReviewLeasesCommand_CountedAttemptsBelowMaximumAttempts
    autolayout lr 250 150
}
custom "ExpireHostileReviewLeasesCommand-part7" "ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 7 of 7" "Activity diagram converted from fence F67 (line 1816)." {
    include am1ExpireHostileReviewLeasesCommand_CountedAttemptsBelowMaximumAttempts am1ExpireHostileReviewLeasesCommand_RequestQueuedAgain am1ExpireHostileReviewLeasesCommand_RequestFailed am1ExpireHostileReviewLeasesCommand_NextAttemptReadClaimedOrRunning
    autolayout tb 100 100
}
dynamic qbrain.server "AM1-17-01" "Sequence diagram converted from fence F68 (line 1841)." {
    title "ListHvReviewersQuery and GetHvReviewerQuery, part 1 of 2"
    qbrain.libs.client -> qbrain.server.api "GET reviewers, or GET reviewers/{reviewerId}"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-01-part2" "Sequence diagram converted from fence F68 (line 1841)." {
    title "ListHvReviewersQuery and GetHvReviewerQuery, part 2 of 2"
    qbrain.server.api -> qbrain.server.strWorkspaceResolutionBehavior "QueryAsync"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-01-alt1-1" "Sequence diagram converted from fence F68 (line 1841)." {
    title "ListHvReviewersQuery and GetHvReviewerQuery, alt 1 branch 1 of 2: workspace unresolved"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "alt 1 branch 1 of 2: workspace unresolved"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.libs.client "400 validation_error, details.field workspacePath"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-01-alt1-2" "Sequence diagram converted from fence F68 (line 1841)." {
    title "ListHvReviewersQuery and GetHvReviewerQuery, alt 1 branch 2 of 2: resolved, part 1 of 2"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "alt 1 branch 2 of 2: resolved"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.am1HvHandlers "handler runs"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "AgentIdentity.Normalize, for one reviewer"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-01-alt1-2-part2" "Sequence diagram converted from fence F68 (line 1841)." {
    title "ListHvReviewersQuery and GetHvReviewerQuery, alt 1 branch 2 of 2: resolved, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-01-alt2-1" "Sequence diagram converted from fence F68 (line 1841)." {
    title "ListHvReviewersQuery and GetHvReviewerQuery, alt 2 branch 1 of 2: unknown reviewer id"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 1 of 2: unknown reviewer id"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "404 not_found, reviewer"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-01-alt2-2" "Sequence diagram converted from fence F68 (line 1841)." {
    title "ListHvReviewersQuery and GetHvReviewerQuery, alt 2 branch 2 of 2: known, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 2 of 2: known"
    qbrain.server.am1HvHandlers -> qbrain.db "load reviewers and count active runs"
    qbrain.db -> qbrain.server.am1HvHandlers "rows"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-01-alt2-2-part2" "Sequence diagram converted from fence F68 (line 1841)." {
    title "ListHvReviewersQuery and GetHvReviewerQuery, alt 2 branch 2 of 2: known, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "200 HvReviewerDto list, or one HvReviewerDto"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, part 1 of 2"
    qbrain.libs.client -> qbrain.server.api "PATCH reviewers/{reviewerId} with expectedStateVersion"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-part2" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, part 2 of 2"
    qbrain.server.api -> qbrain.server.strWorkspaceResolutionBehavior "SendAsync"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-alt1-1" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, alt 1 branch 1 of 2: workspace unresolved"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "alt 1 branch 1 of 2: workspace unresolved"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.libs.client "400 validation_error, workspacePath"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-alt1-2" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, alt 1 branch 2 of 2: resolved"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "alt 1 branch 2 of 2: resolved"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.am1HvHandlers "handler runs"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-alt2-1" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, alt 2 branch 1 of 3: invalid field or no expectedStateVersion"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 1 of 3: invalid field or no expectedStateVersion"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-alt2-2" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, alt 2 branch 2 of 3: unknown reviewer id"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 2 of 3: unknown reviewer id"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "404 not_found, reviewer"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-alt2-3" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, alt 2 branch 3 of 3: valid"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 3 of 3: valid"
    qbrain.server.am1HvHandlers -> qbrain.db "SerializableWrite with AuditLedgerGuard"
    qbrain.db -> qbrain.db "alt 3: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-alt3-1" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, alt 3 branch 1 of 4: audit ledger unavailable, part 1 of 2"
    qbrain.db -> qbrain.db "alt 3 branch 1 of 4: audit ledger unavailable"
    qbrain.db -> qbrain.server.am1HvHandlers "refused"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-alt3-1-part2" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, alt 3 branch 1 of 4: audit ledger unavailable, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "503 audit_unavailable"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-alt3-2" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, alt 3 branch 2 of 4: stale expectedStateVersion"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 2 of 4: stale expectedStateVersion"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "409 conflict, retryable false, details.field expectedStateVersion"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-alt3-3" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, alt 3 branch 3 of 4: attempts exhausted"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 3 of 4: attempts exhausted"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "409 conflict, retryable true"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-alt3-4" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, alt 3 branch 4 of 4: committed, part 1 of 3"
    qbrain.db -> qbrain.db "alt 3 branch 4 of 4: committed"
    qbrain.db -> qbrain.server.am1HvHandlers "updated row, readiness Unverified when the launch changed"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-alt3-4-part2" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, alt 3 branch 4 of 4: committed, part 2 of 3"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "200 HvReviewerDto"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-02-alt3-4-part3" "Sequence diagram converted from fence F69 (line 1867)." {
    title "UpdateHvReviewerCommand, alt 3 branch 4 of 4: committed, part 3 of 3"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "wake, NONCE the Unverified reviewer"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, part 1 of 2"
    qbrain.libs.client -> qbrain.server.api "POST reviewers/{reviewerId}/nonce"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-part2" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, part 2 of 2"
    qbrain.server.api -> qbrain.server.am1HvHandlers "SendAsync, after the workspace behavior"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-1" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 1 of 6: unknown reviewer"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 1 of 6: unknown reviewer"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "404 not_found, reviewer"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-2" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 2 of 6: disabled"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 2 of 6: disabled"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "409 hv_reviewer_disabled"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-3" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 3 of 6: probe already running"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 3 of 6: probe already running"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "202 Running with its runId"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-4" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 4 of 6: probe already requested"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 4 of 6: probe already requested"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "202 AlreadyRequested"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-5" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 5 of 6: audit ledger unavailable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 5 of 6: audit ledger unavailable"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "503 audit_unavailable"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-6" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 1 of 11"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 6 of 6: requested"
    qbrain.server.am1HvHandlers -> qbrain.db "set ProbeRequestedUtc"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-6-part2" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 2 of 11"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "202 Requested"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-6-part3" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 3 of 11"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "wake"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-6-part4" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 4 of 11"
    qbrain.server.am1HvWorkerHost -> qbrain.db "ClaimHvRunCommand creates the Probe run with a fresh nonce"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-6-part5" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 5 of 11"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1ReviewerAdapters "launch with the probe brief"
    qbrain.server.am1ReviewerAdapters -> qbrain.server.am1HvWorkerHost "live lines, the hv_probe record, then exit"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-6-part6" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 6 of 11"
    qbrain.server.am1HvWorkerHost -> qbrain.db "AppendHvRunOutputCommand for each batch, then the hv_provenance lines"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 6) Note over the HV worker and the store: the probe runs through"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-6-part7" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 7 of 11"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 6) RunReviewerProcess, 16.2, so its start, heartbeats, and appends, each"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-6-part8" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 8 of 11"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(3 of 6) append through AppendBatch, have the outcomes drawn in 17.7, a"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-6-part9" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 9 of 11"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(4 of 6) stale fence or audit refusal ends the attempt for the lease expiry,"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-6-part10" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 10 of 11"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(5 of 6) and an integrity refusal fails the probe"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt1-6-part11" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 11 of 11"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(6 of 6) evidence_corrupt as in the V-1 branch below"
    qbrain.server.am1HvWorkerHost -> qbrain.db "on exit 0 CloseHvRunOutputCommand, with RecordRead of the chunks committed, the chunks read and ReadinessProbeVerdict decided before its transaction, then policy HvProbeOutcome in it, otherwise FailHvRunCommand with policy HvProbeOutcome"
    qbrain.db -> qbrain.db "alt 2: one of 5 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt2-1" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 1 of 5: reviewer reconfigured while the probe ran"
    qbrain.db -> qbrain.db "alt 2 branch 1 of 5: reviewer reconfigured while the probe ran"
    qbrain.db -> qbrain.server.am1HvWorkerHost "run Failed probe_stale_configuration, readiness and the newer probe request unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt2-2" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 2 of 5: probe passed"
    qbrain.db -> qbrain.db "alt 2 branch 2 of 5: probe passed"
    qbrain.db -> qbrain.server.am1HvWorkerHost "reviewer Online, notification reviewer_online"
    autolayout lr 300 100
}
dynamic qbrain "AM1-17-03-alt2-3" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 3 of 5: nonce, provenance, or reviewer turn failed, or the launch failed or the process exited nonzero, part 1 of 3"
    qbrain.db -> qbrain.db "(1 of 2) alt 2 branch 3 of 5: nonce, provenance, or reviewer turn failed, or the"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt2-3-part2" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 3 of 5: nonce, provenance, or reviewer turn failed, or the launch failed or the process exited nonzero, part 2 of 3"
    qbrain.db -> qbrain.db "(2 of 2) launch failed or the process exited nonzero"
    qbrain.db -> qbrain.server.am1HvWorkerHost "reviewer Offline, incident Pending, notification reviewer_offline"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt2-3-part3" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 3 of 5: nonce, provenance, or reviewer turn failed, or the launch failed or the process exited nonzero, part 3 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.server.triage "FileHvIncidentCommand at the next sweep"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt2-4" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 4 of 5: stored chunks fail V-1, part 1 of 2"
    qbrain.db -> qbrain.db "alt 2 branch 4 of 5: stored chunks fail V-1"
    qbrain.db -> qbrain.server.am1HvWorkerHost "run Failed evidence_corrupt, server incident Pending, readiness unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt2-4-part2" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 4 of 5: stored chunks fail V-1, part 2 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.triage "FileHvIncidentCommand at the next sweep"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt2-5" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 5 of 5: the read of the chunks cannot be recorded, part 1 of 2"
    qbrain.db -> qbrain.db "alt 2 branch 5 of 5: the read of the chunks cannot be recorded"
    qbrain.db -> qbrain.server.am1HvWorkerHost "503 audit_unavailable, nothing written"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 2) Note over the HV worker and the store: the attempt ends, and the lease"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-03-alt2-5-part2" "Sequence diagram converted from fence F70 (line 1905)." {
    title "StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 5 of 5: the read of the chunks cannot be recorded, part 2 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 2) expiry later requests the probe again, HvProbeOutcome lease_expired"
    autolayout lr 300 100
}
dynamic qbrain.plugins "AM1-17-04" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, part 1 of 2"
    strHostModel -> qbrain.plugins.pluginPs "workflow.hv.submit with a scope and context"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "process BindVerbToTurn, then the idempotency key"
    autolayout lr 300 100
}
dynamic qbrain.plugins "AM1-17-04-part2" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "AM1-17-04-alt1-1" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 1 branch 1 of 2: binding refused"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 2: binding refused"
    qbrain.plugins.pluginPs -> strHostModel "the refusal of BindVerbToTurn"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt1-2" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 1 branch 2 of 2: bound, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 2 of 2: bound"
    qbrain.plugins.pluginPs -> qbrain.server.api "REPL SubmitHvRequestCommand with the submitter turn key"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt1-2-part2" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 1 branch 2 of 2: bound, part 2 of 2"
    qbrain.server.api -> qbrain.server.strWorkspaceResolutionBehavior "SendAsync"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt2-1" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 2 branch 1 of 2: workspace unresolved"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "alt 2 branch 1 of 2: workspace unresolved"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.plugins.pluginPs "400 validation_error, workspacePath"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt2-2" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 2 branch 2 of 2: resolved, part 1 of 4"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "alt 2 branch 2 of 2: resolved"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.am1HvHandlers "handler runs"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "shape checks, RequirementReferenceExtractor"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt2-2-part2" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 2 branch 2 of 2: resolved, part 2 of 4"
    qbrain.server.am1HvHandlers -> qbrain.db "SerializableWrite with AuditLedgerGuard"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "(1 of 2) Note over the handler and the store: ref AdmitRequestChecks and"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt2-2-part3" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 2 branch 2 of 2: resolved, part 3 of 4"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "(2 of 2) InsertAdmittedRequest, section 14.2"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt2-2-part4" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 2 branch 2 of 2: resolved, part 4 of 4"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3: one of 9 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-1" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 1 of 9: malformed scope or submitter"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 1 of 9: malformed scope or submitter"
    qbrain.server.am1HvHandlers -> qbrain.plugins.pluginPs "400 hv_scope_invalid or validation_error"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-2" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 2 of 9: scope too large, includes the submitting turn, or empty"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 2 of 9: scope too large, includes the submitting turn, or empty"
    qbrain.server.am1HvHandlers -> qbrain.plugins.pluginPs "400 hv_scope_too_large, hv_scope_includes_submitting_turn, or hv_scope_empty"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-3" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 3 of 9: turns not terminal, pending, or already reviewed, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "(1 of 2) alt 3 branch 3 of 9: turns not terminal, pending, or already"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-3-part2" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 3 of 9: turns not terminal, pending, or already reviewed, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "(2 of 2) reviewed"
    qbrain.server.am1HvHandlers -> qbrain.plugins.pluginPs "409 with every affected turn listed"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-4" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 4 of 9: no eligible reviewer, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 4 of 9: no eligible reviewer"
    qbrain.server.am1HvHandlers -> qbrain.plugins.pluginPs "409 hv_no_reviewer_available with details.reviewers"
    autolayout lr 300 100
}
dynamic qbrain.plugins "AM1-17-04-alt3-4-part2" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 4 of 9: no eligible reviewer, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "refusal with reviewer statuses, open incidents, and the NONCE remedy"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-5" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 5 of 9: same key and same inputs, part 1 of 3"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 5 of 9: same key and same inputs"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-5-part2" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 5 of 9: same key and same inputs, part 2 of 3"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "(1 of 2) nothing written, then GetHvRequestQuery for the stored request,"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-5-part3" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 5 of 9: same key and same inputs, part 3 of 3"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "(2 of 2) its output read recorded first"
    qbrain.server.am1HvHandlers -> qbrain.plugins.pluginPs "200 the stored request, or that query's 500 evidence_corrupt or 503 audit_unavailable"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-6" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 6 of 9: the accepted request fails its GZIP round trip"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 6 of 9: the accepted request fails its GZIP round trip"
    qbrain.server.am1HvHandlers -> qbrain.plugins.pluginPs "500 evidence_corrupt, nothing saved"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-7" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 7 of 9: key reused with other inputs"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 7 of 9: key reused with other inputs"
    qbrain.server.am1HvHandlers -> qbrain.plugins.pluginPs "409 conflict, retryable false"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-8" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 8 of 9: audit unavailable or attempts exhausted, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 8 of 9: audit unavailable or attempts exhausted"
    qbrain.server.am1HvHandlers -> qbrain.plugins.pluginPs "503 audit_unavailable or 409 conflict, retryable true"
    autolayout lr 300 100
}
dynamic qbrain.plugins "AM1-17-04-alt3-8-part2" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 8 of 9: audit unavailable or attempts exhausted, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "retryable true, exit 1, not queued"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-9" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 9 of 9: admitted, part 1 of 4"
    qbrain.db -> qbrain.db "alt 3 branch 9 of 9: admitted"
    qbrain.db -> qbrain.server.am1HvHandlers "request Queued, turns Pending, notification hv_queued"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-9-part2" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 9 of 9: admitted, part 2 of 4"
    qbrain.server.am1HvHandlers -> qbrain.plugins.pluginPs "201 HvRequestResult"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-04-alt3-9-part3" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 9 of 9: admitted, part 3 of 4"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "wake, auto-start"
    autolayout lr 300 100
}
dynamic qbrain.plugins "AM1-17-04-alt3-9-part4" "Sequence diagram converted from fence F71 (line 1955)." {
    title "SubmitHvRequestCommand from the plugin verb, alt 3 branch 9 of 9: admitted, part 4 of 4"
    qbrain.plugins.pluginPs -> strHostModel "id, status, assigned reviewer"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, part 1 of 2"
    qbrain.libs.client -> qbrain.server.api "GET requests/{hvRequestId}, or GET requests with filters"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05-part2" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, part 2 of 2"
    qbrain.server.api -> qbrain.server.am1HvHandlers "QueryAsync, after the workspace behavior"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05-alt1-1" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 1 of 4: one request, not found"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 1 of 4: one request, not found"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "404 not_found, hv_request"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05-alt1-2" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 2 of 4: one request, found, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 2 of 4: one request, found"
    qbrain.server.am1HvHandlers -> qbrain.db "load turns, references, responses, current run, last chunks"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05-alt1-2-part2" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 2 of 4: one request, found, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05-alt2-1" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, alt 2 branch 1 of 4: output present and audit ledger unavailable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 1 of 4: output present and audit ledger unavailable"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "503 audit_unavailable"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05-alt2-2" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, alt 2 branch 2 of 4: evidence removed"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 2 of 4: evidence removed"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "200 HvRequestResult, lastOutput empty, outputRemoved true, no read recorded"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05-alt2-3" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, alt 2 branch 3 of 4: read recorded, and a last chunk fails to decompress"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 3 of 4: read recorded, and a last chunk fails to decompress"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "500 evidence_corrupt"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05-alt2-4" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, alt 2 branch 4 of 4: read recorded or no output"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 4 of 4: read recorded or no output"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "200 HvRequestResult with progress, etaStatus, and the last 20 lines"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05-alt1-3" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 3 of 4: query with an unknown filter or out-of-bounds page, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "(1 of 2) alt 1 branch 3 of 4: query with an unknown filter or out-of-bounds"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05-alt1-3-part2" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 3 of 4: query with an unknown filter or out-of-bounds page, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "(2 of 2) page"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05-alt1-4" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 4 of 4: query, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 4 of 4: query"
    qbrain.server.am1HvHandlers -> qbrain.db "PagedQuery, CreatedUtc descending"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-05-alt1-4-part2" "Sequence diagram converted from fence F72 (line 2009)." {
    title "GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 4 of 4: query, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "200 PageResult of summaries"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, part 1 of 3"
    qbrain.libs.client -> qbrain.server.api "POST requests/{hvRequestId}/cancel"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-part2" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, part 2 of 3"
    qbrain.server.api -> qbrain.server.am1HvHandlers "SendAsync, after the workspace behavior"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-part3" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, part 3 of 3"
    qbrain.server.am1HvHandlers -> qbrain.db "SerializableWrite with AuditLedgerGuard"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt1-1" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 1 branch 1 of 6: not found"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 1 of 6: not found"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "404 not_found, hv_request"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt1-2" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 1 branch 2 of 6: Finalizing, Completed, or Failed"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 2 of 6: Finalizing, Completed, or Failed"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "409 hv_request_not_cancelable"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt1-3" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 1 branch 3 of 6: already CancelRequested or Canceled"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 3 of 6: already CancelRequested or Canceled"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "200 unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt1-4" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 1 branch 4 of 6: audit unavailable or attempts exhausted"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 4 of 6: audit unavailable or attempts exhausted"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "503 audit_unavailable or 409 conflict, retryable true"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt1-5" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 1 branch 5 of 6: Queued, part 1 of 2"
    qbrain.db -> qbrain.db "alt 1 branch 5 of 6: Queued"
    qbrain.db -> qbrain.server.am1HvHandlers "Canceled, turns released, hv_canceled"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt1-5-part2" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 1 branch 5 of 6: Queued, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "200 Canceled"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt1-6" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 1 branch 6 of 6: Running, part 1 of 5"
    qbrain.db -> qbrain.db "alt 1 branch 6 of 6: Running"
    qbrain.db -> qbrain.server.am1HvHandlers "CancelRequested, hv_cancel_requested"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt1-6-part2" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 1 branch 6 of 6: Running, part 2 of 5"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "200 CancelRequested"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt1-6-part3" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 1 branch 6 of 6: Running, part 3 of 5"
    qbrain.server.am1HvWorkerHost -> qbrain.db "RecordHvRunHeartbeatCommand"
    qbrain.db -> qbrain.server.am1HvWorkerHost "cancelRequested true"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt1-6-part4" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 1 branch 6 of 6: Running, part 4 of 5"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1ReviewerAdapters "kill the process"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt1-6-part5" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 1 branch 6 of 6: Running, part 5 of 5"
    qbrain.server.am1HvWorkerHost -> qbrain.db "FailHvRunCommand canceled, through FailAttempt"
    qbrain.db -> qbrain.db "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt2-1" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 2 branch 1 of 2: failed"
    qbrain.db -> qbrain.db "alt 2 branch 1 of 2: failed"
    qbrain.db -> qbrain.server.am1HvWorkerHost "run Canceled, request Canceled, turns released, hv_canceled"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt2-2" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 2 branch 2 of 2: stale_fence or audit_unavailable, part 1 of 2"
    qbrain.db -> qbrain.db "alt 2 branch 2 of 2: stale_fence or audit_unavailable"
    qbrain.db -> qbrain.server.am1HvWorkerHost "nothing written, the request stays CancelRequested"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 2) Note over the HV worker and the store: the lease expiry completes the"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-06-alt2-2-part2" "Sequence diagram converted from fence F73 (line 2040)." {
    title "CancelHvRequestCommand, alt 2 branch 2 of 2: stale_fence or audit_unavailable, part 2 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 2) cancel, ExpireHvRunLeasesCommand with HvRetryDecision"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, part 1 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "ClaimHvRunCommand"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-part2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.db "SerializableWrite: the oldest claimable Queued request in the order of 14.4, a reviewer with capacity, FencedLease Claim"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt1-1" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 1 branch 1 of 2: nothing claimable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 1 of 2: nothing claimable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "null"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt1-2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 1 branch 2 of 2: claimed, part 1 of 3"
    qbrain.db -> qbrain.db "alt 1 branch 2 of 2: claimed"
    qbrain.db -> qbrain.server.am1HvHandlers "run Claimed, request Running, hv_started"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt1-2-part2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 1 branch 2 of 2: claimed, part 2 of 3"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "claim with fence token"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt1-2-part3" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 1 branch 2 of 2: claimed, part 3 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.db "AuditLedgerGuard RecordRead, then the Request evidence for the brief"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 2: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt2-1" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 2 branch 1 of 3: read refused, part 1 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 2 branch 1 of 3: read refused"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt2-1-part2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 2 branch 1 of 3: read refused, part 2 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 2) Note: attempt ends before launch without another write, the lease"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt2-1-part3" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 2 branch 1 of 3: read refused, part 3 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 2) expires later, 17.9"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt2-2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 2 branch 2 of 3: Request evidence fails to decompress"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 2 branch 2 of 3: Request evidence fails to decompress"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "FailHvRunCommand evidence_corrupt, a server incident, the reviewer unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt2-3" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 2 branch 3 of 3: brief built, part 1 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 2 branch 3 of 3: brief built"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1ReviewerAdapters "BuildLaunch and start the process inside a containment bound to the worker"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt2-3-part2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 2 branch 3 of 3: brief built, part 2 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 3: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt3-1" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 1 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 3 branch 1 of 2: launch failed"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "FailHvRunCommand reviewer_launch_failed"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt3-1-part2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 2 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 3) Note over the HV worker and the handler: ref the execution"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt3-1-part3" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 3 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 3) failure sequence, 17.8, and every FailHvRunCommand of this"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt3-1-part4" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 4 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(3 of 3) sequence is handled by FailAttempt, 16.2"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt3-1-part5" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 5 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 3) Note over the HV worker and the handler: failed ends the attempt with"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt3-1-part6" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 6 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 3) the state written, stale_fence or audit_unavailable ends it with"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt3-1-part7" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 7 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(3 of 3) no further write"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt3-2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 3 branch 2 of 2: started"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 3 branch 2 of 2: started"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "RecordHvRunStartCommand with the process id"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 4: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt4-1" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 4 branch 1 of 3: stale fence or audit unavailable at the start, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 4 branch 1 of 3: stale fence or audit unavailable at the start"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "stale_fence or 503 audit_unavailable"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt4-1-part2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 4 branch 1 of 3: stale fence or audit unavailable at the start, part 2 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1ReviewerAdapters "kill the process"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "Note: attempt ends without another write, the lease expires later, 17.9"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt4-2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 4 branch 2 of 3: retryable conflict at the start, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 4 branch 2 of 3: retryable conflict at the start"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "409 conflict, retryable true"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 2) Note over the HV worker and the handler: the worker repeats the"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt4-2-part2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 4 branch 2 of 3: retryable conflict at the start, part 2 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 2) same call with the same process id"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt4-3" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 1 of 7"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 4 branch 3 of 3: start recorded"
    qbrain.server.am1HvHandlers -> qbrain.db "run Running, StartedUtc, ProcessId"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt4-3-part2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 2 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "AppendHvRunOutputCommand sequence 1, the hv_brief record"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 3) Note over the HV worker and the handler: every append, sequence 1 and"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt4-3-part3" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 3 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 3) the final flush included, goes through AppendBatch, 16.2, whose"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt4-3-part4" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 4 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(3 of 3) refusals are the branches below"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt4-3-part5" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 5 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 2) Note over the HV worker and the reviewer adapter: ref"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt4-3-part6" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 6 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 2) StreamOneAttempt (below)"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt4-3-part7" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 7 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 5: one of 5 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt5-1" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 5 branch 1 of 5: stale fence or audit unavailable during the run, part 1 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 5 branch 1 of 5: stale fence or audit unavailable during the run"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1ReviewerAdapters "kill the process"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt5-1-part2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 5 branch 1 of 5: stale fence or audit unavailable during the run, part 2 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "Note: attempt ends without another write, the lease expires later, 17.9"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt5-2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 5 branch 2 of 5: cancel requested, part 1 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 5 branch 2 of 5: cancel requested"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1ReviewerAdapters "kill the process"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt5-2-part2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 5 branch 2 of 5: cancel requested, part 2 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "FailHvRunCommand canceled"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt5-3" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 5 branch 3 of 5: evidence_corrupt, a non-retryable conflict, or a 400 validation_error from an append, part 1 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 2) alt 5 branch 3 of 5: evidence_corrupt, a non-retryable"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt5-3-part2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 5 branch 3 of 5: evidence_corrupt, a non-retryable conflict, or a 400 validation_error from an append, part 2 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 2) conflict, or a 400 validation_error from an append"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1ReviewerAdapters "kill the process"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt5-3-part3" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 5 branch 3 of 5: evidence_corrupt, a non-retryable conflict, or a 400 validation_error from an append, part 3 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "FailHvRunCommand evidence_corrupt, a server incident, the reviewer unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt5-4" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 5 branch 4 of 5: exited with a nonzero code"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 5 branch 4 of 5: exited with a nonzero code"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "FailHvRunCommand reviewer_exited_nonzero"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt5-5" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 5 branch 5 of 5: exited with code 0"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 5 branch 5 of 5: exited with code 0"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "AppendHvRunOutputCommand, the remaining lines and the hv_provenance lines"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "CloseHvRunOutputCommand with the chunk count"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 6: one of 5 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt6-1" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 6 branch 1 of 5: request CancelRequested at close"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 6 branch 1 of 5: request CancelRequested at close"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "run Canceled, request Canceled, turns released"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt6-2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 6 branch 2 of 5: 409 conflict with details.field chunkCount, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 6 branch 2 of 5: 409 conflict with details.field chunkCount"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "conflict, chunkCount"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 2) Note over the HV worker and the handler: the worker appends what is"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt6-2-part2" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 6 branch 2 of 5: 409 conflict with details.field chunkCount, part 2 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 2) missing and closes again"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt6-3" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 6 branch 3 of 5: retryable conflict at the close"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 6 branch 3 of 5: retryable conflict at the close"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "409 conflict, retryable true"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "Note over the HV worker and the handler: the worker repeats the same close"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt6-4" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 6 branch 4 of 5: stale fence or audit unavailable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 6 branch 4 of 5: stale fence or audit unavailable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "stale_fence or 503 audit_unavailable"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "Note: attempt ends without another write, the lease expires later, 17.9"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-07-alt6-5" "Sequence diagram converted from fence F74 (line 2080)." {
    title "A review attempt, from claim to finalization, alt 6 branch 5 of 5: otherwise"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 6 branch 5 of 5: otherwise"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "request Finalizing"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "Note over the HV worker and the handler: ref FinalizeOneRequest (below)"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), part 1 of 2"
    qbrain.server.am1ReviewerAdapters -> qbrain.server.am1ReviewerAdapters "(1 of 2) loop 1: until the process exits, a stale fence, an audit failure,"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-part2" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), part 2 of 2"
    qbrain.server.am1ReviewerAdapters -> qbrain.server.am1ReviewerAdapters "(2 of 2) evidence_corrupt, or a cancel (next view)"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-loop1" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), loop 1: until the process exits, a stale fence, an audit failure, evidence_corrupt, or a cancel, part 1 of 3"
    qbrain.server.am1ReviewerAdapters -> qbrain.server.am1ReviewerAdapters "(1 of 2) loop 1: until the process exits, a stale fence, an audit failure,"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-loop1-part2" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), loop 1: until the process exits, a stale fence, an audit failure, evidence_corrupt, or a cancel, part 2 of 3"
    qbrain.server.am1ReviewerAdapters -> qbrain.server.am1ReviewerAdapters "(2 of 2) evidence_corrupt, or a cancel"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-loop1-part3" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), loop 1: until the process exits, a stale fence, an audit failure, evidence_corrupt, or a cancel, part 3 of 3"
    qbrain.server.am1ReviewerAdapters -> qbrain.server.am1ReviewerAdapters "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt1-1" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 1 branch 1 of 3: a line arrives, part 1 of 2"
    qbrain.server.am1ReviewerAdapters -> qbrain.server.am1ReviewerAdapters "alt 1 branch 1 of 3: a line arrives"
    qbrain.server.am1ReviewerAdapters -> qbrain.server.am1HvWorkerHost "output line, at once"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "add to the buffer, start the flush timer if the buffer was empty"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt1-1-part2" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 1 branch 1 of 3: a line arrives, part 2 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "opt 1: flush bytes reached (next view)"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-opt1" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), opt 1: flush bytes reached, part 1 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "opt 1: flush bytes reached"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "AppendHvRunOutputCommand with the next sequence"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-opt1-part2" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), opt 1: flush bytes reached, part 2 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 2) Note over the HV worker and the handler: the same append outcomes as a"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-opt1-part3" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), opt 1: flush bytes reached, part 3 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 2) timer flush, below"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt1-2" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 1 branch 2 of 3: the flush timer fires with lines buffered, whether or not another line came, part 1 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 2) alt 1 branch 2 of 3: the flush timer fires with lines buffered,"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt1-2-part2" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 1 branch 2 of 3: the flush timer fires with lines buffered, whether or not another line came, part 2 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 2) whether or not another line came"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "AppendHvRunOutputCommand with the next sequence"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt2-1" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 2 branch 1 of 4: appended, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 1 of 4: appended"
    qbrain.server.am1HvHandlers -> qbrain.db "chunk stored, manifest and progress updated"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt2-1-part2" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 2 branch 1 of 4: appended, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "appended, streams woken"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt2-2" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 2 branch 2 of 4: retryable conflict"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 2 of 4: retryable conflict"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "409 conflict, retryable true"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "the same append again"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt2-3" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 2 branch 3 of 4: stale fence or audit unavailable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 3 of 4: stale fence or audit unavailable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "stale_fence or 503 audit_unavailable"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt2-4" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 2 branch 4 of 4: round trip failed, a non-retryable conflict, or a malformed batch, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "(1 of 2) alt 2 branch 4 of 4: round trip failed, a non-retryable conflict, or a"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt2-4-part2" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 2 branch 4 of 4: round trip failed, a non-retryable conflict, or a malformed batch, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "(2 of 2) malformed batch"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "500 evidence_corrupt, 409 conflict retryable false, or 400 validation_error, nothing saved"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt1-3" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 1 branch 3 of 3: the heartbeat timer fires"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "alt 1 branch 3 of 3: the heartbeat timer fires"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "RecordHvRunHeartbeatCommand"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt3-1" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 3 branch 1 of 4: renewed"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 1 of 4: renewed"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "lease extended, cancelRequested false"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt3-2" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 3 branch 2 of 4: cancel requested"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 2 of 4: cancel requested"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "cancelRequested true"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt3-3" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 3 branch 3 of 4: retryable conflict"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 3 of 4: retryable conflict"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "409 conflict, retryable true"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "the same heartbeat again"
    autolayout lr 300 100
}
dynamic qbrain.server "StreamOneAttempt-alt3-4" "Sequence diagram converted from fence F75 (line 2156)." {
    title "Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 3 branch 4 of 4: stale fence or audit unavailable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 4 of 4: stale fence or audit unavailable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "stale_fence or 503 audit_unavailable"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt)"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvFinalizer "FinalizeHvRequestCommand"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt1-1" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 1 branch 1 of 3: already Completed"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 1 branch 1 of 3: already Completed"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvWorkerHost "already_completed, nothing written"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt1-2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 1 branch 2 of 3: not Finalizing"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 1 branch 2 of 3: not Finalizing"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvWorkerHost "not_finalizable, nothing written"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt1-3" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 1 branch 3 of 3: Finalizing, part 1 of 2"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 1 branch 3 of 3: Finalizing"
    qbrain.server.am1HvFinalizer -> qbrain.db "AuditLedgerGuard RecordRead of the run's chunks"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt1-3-part2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 1 branch 3 of 3: Finalizing, part 2 of 2"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt2-1" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 2 branch 1 of 2: read refused"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 2 branch 1 of 2: read refused"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvWorkerHost "503 audit_unavailable, the request stays Finalizing"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt2-2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 2 branch 2 of 2: read recorded, part 1 of 3"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 2 branch 2 of 2: read recorded"
    qbrain.server.am1HvFinalizer -> qbrain.db "read the run and its chunks, no transaction"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt2-2-part2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 2 branch 2 of 2: read recorded, part 2 of 3"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "policy HvFinalVerification V-1 to V-8"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt2-2-part3" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 2 branch 2 of 2: read recorded, part 3 of 3"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 3: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt3-1" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 3 branch 1 of 2: a step failed, part 1 of 3"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 3 branch 1 of 2: a step failed"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt3-1-part2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 3 branch 1 of 2: a step failed, part 2 of 3"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "FailHvRunCommand with the stored fence and the code"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt3-1-part3" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 3 branch 1 of 2: a step failed, part 3 of 3"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 4: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt4-1" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 4 branch 1 of 3: failed"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 4 branch 1 of 3: failed"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvWorkerHost "verification_failed, request Queued or Failed"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt4-2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 4 branch 2 of 3: already_completed, not_finalizable, or stale_fence, part 1 of 2"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "(1 of 2) alt 4 branch 2 of 3: already_completed,"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt4-2-part2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 4 branch 2 of 3: already_completed, not_finalizable, or stale_fence, part 2 of 2"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "(2 of 2) not_finalizable, or stale_fence"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvWorkerHost "already_completed or not_finalizable, nothing changed"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt4-3" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 4 branch 3 of 3: 503 audit_unavailable or 409 conflict"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 4 branch 3 of 3: 503 audit_unavailable or 409 conflict"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvWorkerHost "that refusal, the request stays Finalizing for the sweep"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt3-2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 3 branch 2 of 2: verified, part 1 of 2"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 3 branch 2 of 2: verified"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt3-2-part2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 3 branch 2 of 2: verified, part 2 of 2"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "opt 1: linked responses and Mode not Off (next view)"
    qbrain.server.am1HvFinalizer -> qbrain.db "SerializableWrite with AuditLedgerGuard: re-check, then CommitFinalResult"
    qbrain.db -> qbrain.db "alt 5: one of 5 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-opt1" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), opt 1: linked responses and Mode not Off, part 1 of 2"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "opt 1: linked responses and Mode not Off"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-opt1-part2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), opt 1: linked responses and Mode not Off, part 2 of 2"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "EvaluateStalenessCommand for the linked responses"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt5-1" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 1 of 5: state differs from the reads"
    qbrain.db -> qbrain.db "alt 5 branch 1 of 5: state differs from the reads"
    qbrain.db -> qbrain.server.am1HvFinalizer "attempt ends, the unit starts again from its status check"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt5-2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 2 of 5: audit unavailable or attempts exhausted"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "alt 5 branch 2 of 5: audit unavailable or attempts exhausted"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvWorkerHost "503 audit_unavailable or 409 conflict, the request stays Finalizing"
    autolayout lr 300 100
}
dynamic qbrain "FinalizeOneRequest-alt5-3" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 3 of 5: result compression failed its round trip, or a stored model grade with other input, part 1 of 3"
    qbrain.db -> qbrain.db "(1 of 2) alt 5 branch 3 of 5: result compression failed its round trip, or"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt5-3-part2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 3 of 5: result compression failed its round trip, or a stored model grade with other input, part 2 of 3"
    qbrain.db -> qbrain.db "(2 of 2) a stored model grade with other input"
    qbrain.db -> qbrain.server.am1HvFinalizer "rolled back, nothing visible"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "(1 of 2) FailHvRunCommand evidence_corrupt or"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt5-3-part3" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 3 of 5: result compression failed its round trip, or a stored model grade with other input, part 3 of 3"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvFinalizer "(2 of 2) model_grade_conflict, a server fault, outcomes as above"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt5-4" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 4 of 5: a model grade guard failed, retryable"
    qbrain.db -> qbrain.db "alt 5 branch 4 of 5: a model grade guard failed, retryable"
    qbrain.db -> qbrain.server.am1HvFinalizer "attempt ends, the unit starts again from its status check"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt5-5" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 5 of 5: committed, part 1 of 2"
    qbrain.db -> qbrain.db "alt 5 branch 5 of 5: committed"
    qbrain.db -> qbrain.server.am1HvFinalizer "assessments, findings of every scope, hostile reviews, turns Final, grades, Completed, hv_completed"
    autolayout lr 300 100
}
dynamic qbrain.server "FinalizeOneRequest-alt5-5-part2" "Sequence diagram converted from fence F76 (line 2201)." {
    title "Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 5 of 5: committed, part 2 of 2"
    qbrain.server.am1HvFinalizer -> qbrain.server.am1HvWorkerHost "completed with the overall verdict"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, part 1 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "FailHvRunCommand with a reviewer-attributable code"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-part2" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.db "SerializableWrite with AuditLedgerGuard and FencedLease Guard"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt1-1" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 1 branch 1 of 3: stale fence"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 1 of 3: stale fence"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "stale_fence, nothing changed"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt1-2" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 1 branch 2 of 3: audit unavailable or attempts exhausted, part 1 of 3"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 2 of 3: audit unavailable or attempts exhausted"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "503 audit_unavailable or 409 conflict, retryable true, nothing changed"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 3) Note over the HV worker and the handler: FailAttempt ends the"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt1-2-part2" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 1 branch 2 of 3: audit unavailable or attempts exhausted, part 2 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 3) attempt with no further write, or repeats the call on the retryable"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt1-2-part3" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 1 branch 2 of 3: audit unavailable or attempts exhausted, part 3 of 3"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(3 of 3) conflict"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt1-3" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 1 of 7"
    qbrain.db -> qbrain.db "alt 1 branch 3 of 3: guarded"
    qbrain.db -> qbrain.server.am1HvHandlers "run Failed, incident Pending, reviewer Offline, reviewer_offline"
    autolayout lr 300 100
}
dynamic qbrain "AM1-17-08-alt1-3-part2" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 2 of 7"
    qbrain.db -> qbrain.db "alt 2: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt1-3-part3" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 3 of 7"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "failed with the request's next state"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "FileHvIncidentCommand at the sweep"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt1-3-part4" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 4 of 7"
    qbrain.server.am1HvHandlers -> qbrain.db "SerializableWrite with AuditLedgerGuard: FilingAttempts plus 1, LastFilingAttemptUtc"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt1-3-part5" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 5 of 7"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "ClaimHvRunCommand reassigns the Queued request to the next eligible reviewer with capacity, or writes nothing and leaves it waiting when none has capacity"
    autolayout lr 300 100
}
dynamic qbrain.libs "AM1-17-08-alt1-3-part6" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 6 of 7"
    qbrain.libs.client -> qbrain.libs.client "(1 of 2) Note over the caller and the handler: later, after the cause is fixed:"
    autolayout lr 300 100
}
dynamic qbrain.libs "AM1-17-08-alt1-3-part7" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 7 of 7"
    qbrain.libs.client -> qbrain.libs.client "(2 of 2) StartHvReviewerNonceCommand, 17.3"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt2-1" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 2 branch 1 of 3: request CancelRequested"
    qbrain.db -> qbrain.db "alt 2 branch 1 of 3: request CancelRequested"
    qbrain.db -> qbrain.server.am1HvHandlers "request Canceled, turns released, responses closed, hv_canceled"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt2-2" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 2 branch 2 of 3: attempts left"
    qbrain.db -> qbrain.db "alt 2 branch 2 of 3: attempts left"
    qbrain.db -> qbrain.server.am1HvHandlers "request Queued, through policy HvRetryDecision"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt2-3" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 2 branch 3 of 3: attempts exhausted"
    qbrain.db -> qbrain.db "alt 2 branch 3 of 3: attempts exhausted"
    qbrain.db -> qbrain.server.am1HvHandlers "request Failed, turns released, responses closed, hv_failed"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt3-1" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 3 branch 1 of 2: audit ledger unavailable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 1 of 2: audit ledger unavailable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "503 audit_unavailable, triage not called, incident Pending"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt3-2" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 3 branch 2 of 2: attempt recorded"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 3 branch 2 of 2: attempt recorded"
    qbrain.server.am1HvHandlers -> qbrain.server.triage "submit the report with dedupeKey hv-reviewer-id-code"
    qbrain.server.triage -> qbrain.server.triage "alt 4: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt4-1" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 4 branch 1 of 2: triage accepted, part 1 of 2"
    qbrain.server.triage -> qbrain.server.triage "alt 4 branch 1 of 2: triage accepted"
    qbrain.server.triage -> qbrain.server.am1HvHandlers "report id and group id"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt4-1-part2" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 4 branch 1 of 2: triage accepted, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.db "incident Filed, incident_filed"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt4-2" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 4 branch 2 of 2: triage refused or unreachable, part 1 of 2"
    qbrain.server.triage -> qbrain.server.triage "alt 4 branch 2 of 2: triage refused or unreachable"
    qbrain.server.triage -> qbrain.server.am1HvHandlers "error"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-08-alt4-2-part2" "Sequence diagram converted from fence F77 (line 2252)." {
    title "An execution failure, its incident, and the reviewer going offline, alt 4 branch 2 of 2: triage refused or unreachable, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.db "LastFilingError, retried at the next sweep"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, part 1 of 6"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(1 of 2) Note over Worker A, crashed: crashed while its run was"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-part2" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, part 2 of 6"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "(2 of 2) Running, its process gone with it"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-part3" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, part 3 of 6"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "Worker B: ExpireHvRunLeasesCommand at its sweep"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-part4" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, part 4 of 6"
    qbrain.server.am1HvHandlers -> qbrain.db "runs Claimed or Running with an expired lease"
    qbrain.db -> qbrain.db "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-part5" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, part 5 of 6"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "to Worker B: abandoned runs and Finalizing requests"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "Worker B: FinalizeHvRequestCommand for each Finalizing request"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "Worker B: ClaimHvRunCommand"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-part6" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, part 6 of 6"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "opt 1: worker A was alive but paused past its lease (next view)"
    autolayout lr 300 100
}
dynamic qbrain "AM1-17-09-alt1-1" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, alt 1 branch 1 of 2: renewed, settled, or taken over by another sweep in the meantime, part 1 of 2"
    qbrain.db -> qbrain.db "(1 of 2) alt 1 branch 1 of 2: renewed, settled, or taken over by another sweep"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-alt1-1-part2" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, alt 1 branch 1 of 2: renewed, settled, or taken over by another sweep in the meantime, part 2 of 2"
    qbrain.db -> qbrain.db "(2 of 2) in the meantime"
    qbrain.db -> qbrain.server.am1HvHandlers "skipped by FencedLease Expire, the request untouched"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-alt1-2" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, alt 1 branch 2 of 2: still Claimed or Running with the fence as read and its lease expired, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "(1 of 2) alt 1 branch 2 of 2: still Claimed or Running with the fence as"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-alt1-2-part2" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, alt 1 branch 2 of 2: still Claimed or Running with the fence as read and its lease expired, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "(2 of 2) read and its lease expired"
    qbrain.server.am1HvHandlers -> qbrain.db "run Abandoned lease_expired, fence incremented, server incident, policy HvRetryDecision"
    qbrain.db -> qbrain.db "alt 2: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-alt2-1" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, alt 2 branch 1 of 4: the request has a newer attempt or is terminal"
    qbrain.db -> qbrain.db "alt 2 branch 1 of 4: the request has a newer attempt or is terminal"
    qbrain.db -> qbrain.server.am1HvHandlers "request untouched"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-alt2-2" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, alt 2 branch 2 of 4: request CancelRequested"
    qbrain.db -> qbrain.db "alt 2 branch 2 of 4: request CancelRequested"
    qbrain.db -> qbrain.server.am1HvHandlers "request Canceled, turns released, responses closed, hv_canceled"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-alt2-3" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, alt 2 branch 3 of 4: attempts left"
    qbrain.db -> qbrain.db "alt 2 branch 3 of 4: attempts left"
    qbrain.db -> qbrain.server.am1HvHandlers "request Queued"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-alt2-4" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, alt 2 branch 4 of 4: attempts exhausted"
    qbrain.db -> qbrain.db "alt 2 branch 4 of 4: attempts exhausted"
    qbrain.db -> qbrain.server.am1HvHandlers "request Failed, turns released, responses closed, hv_failed"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-opt1" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, opt 1: worker A was alive but paused past its lease, part 1 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "opt 1: worker A was alive but paused past its lease"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "Worker A, crashed: any fenced write with the old fence"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvWorkerHost "to Worker A, crashed: stale_fence"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-09-opt1-part2" "Sequence diagram converted from fence F78 (line 2297)." {
    title "Recovery after a worker crash, opt 1: worker A was alive but paused past its lease, part 2 of 2"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvWorkerHost "Worker A, crashed: kill its process, end the attempt"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, part 1 of 2"
    qbrain.libs.client -> qbrain.server.api "GET requests/{hvRequestId}/result, or GET requests/{hvRequestId}/evidence/{kind}"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10-part2" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, part 2 of 2"
    qbrain.server.api -> qbrain.server.am1HvHandlers "QueryAsync, after the workspace behavior"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1: one of 7 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10-alt1-1" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 1 of 7: unknown kind"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 1 of 7: unknown kind"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "400 validation_error, details.field kind"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10-alt1-2" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 2 of 7: request or run not found"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 2 of 7: request or run not found"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "404 not_found, hv_request or run"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10-alt1-3" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 3 of 7: result before Completed"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 3 of 7: result before Completed"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "409 hv_result_not_ready"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10-alt1-4" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 4 of 7: evidence not produced yet"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 4 of 7: evidence not produced yet"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "404 hv_evidence_not_available"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10-alt1-5" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 5 of 7: evidence removed"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 5 of 7: evidence removed"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "410 hv_evidence_removed with hashes and sizes"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10-alt1-6" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 6 of 7: audit ledger unavailable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 6 of 7: audit ledger unavailable"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "503 audit_unavailable"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10-alt1-7" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 7 of 7: available, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 7 of 7: available"
    qbrain.server.am1HvHandlers -> qbrain.db "AuditLedgerGuard RecordRead, then read the evidence"
    qbrain.db -> qbrain.server.am1HvHandlers "GZIP members and chunks"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10-alt1-7-part2" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 7 of 7: available, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10-alt2-1" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, alt 2 branch 1 of 3: a payload it decompresses fails before the first byte"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 1 of 3: a payload it decompresses fails before the first byte"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "500 evidence_corrupt"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10-alt2-2" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, alt 2 branch 2 of 3: it fails after the body started"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 2 of 3: it fails after the body started"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "the body ends incomplete, which its stored hash shows"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-10-alt2-3" "Sequence diagram converted from fence F79 (line 2332)." {
    title "GetHvResultQuery and GetHvEvidenceQuery, alt 2 branch 3 of 3: decompressed or served compressed"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 3 of 3: decompressed or served compressed"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "200 gzip, or decompressed when asked, streamed"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-11" "Sequence diagram converted from fence F80 (line 2367)." {
    title "RemoveHvEvidenceCommand, part 1 of 2"
    qbrain.libs.client -> qbrain.server.api "POST requests/{hvRequestId}/evidence/remove with reason and removedBy"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-11-part2" "Sequence diagram converted from fence F80 (line 2367)." {
    title "RemoveHvEvidenceCommand, part 2 of 2"
    qbrain.server.api -> qbrain.server.am1HvHandlers "SendAsync, after the workspace behavior"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1: one of 5 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-11-alt1-1" "Sequence diagram converted from fence F80 (line 2367)." {
    title "RemoveHvEvidenceCommand, alt 1 branch 1 of 5: reason or removedBy missing or too long"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 1 of 5: reason or removedBy missing or too long"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-11-alt1-2" "Sequence diagram converted from fence F80 (line 2367)." {
    title "RemoveHvEvidenceCommand, alt 1 branch 2 of 5: not found"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 2 of 5: not found"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "404 not_found, hv_request"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-11-alt1-3" "Sequence diagram converted from fence F80 (line 2367)." {
    title "RemoveHvEvidenceCommand, alt 1 branch 3 of 5: request not terminal"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 3 of 5: request not terminal"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "409 hv_request_active"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-11-alt1-4" "Sequence diagram converted from fence F80 (line 2367)." {
    title "RemoveHvEvidenceCommand, alt 1 branch 4 of 5: audit unavailable or attempts exhausted"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 4 of 5: audit unavailable or attempts exhausted"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "503 audit_unavailable or 409 conflict, retryable true"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-11-alt1-5" "Sequence diagram converted from fence F80 (line 2367)." {
    title "RemoveHvEvidenceCommand, alt 1 branch 5 of 5: removed or nothing left, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 5 of 5: removed or nothing left"
    qbrain.server.am1HvHandlers -> qbrain.db "null the payloads, stamp the removal, ledger rows"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-11-alt1-5-part2" "Sequence diagram converted from fence F80 (line 2367)." {
    title "RemoveHvEvidenceCommand, alt 1 branch 5 of 5: removed or nothing left, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "200 removed kinds and chunk count, empty on a repeat"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, part 1 of 2"
    qbrain.libs.client -> qbrain.server.api "GET requests/{hvRequestId}/output/stream"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-part2" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, part 2 of 2"
    qbrain.server.api -> qbrain.server.am1HvHandlers "QueryAsync, after the workspace behavior"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1: one of 5 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-1" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 1 of 5: not found"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 1 of 5: not found"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "404 not_found, hv_request"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-2" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 2 of 5: evidence removed"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 2 of 5: evidence removed"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "410 hv_evidence_removed"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-3" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 3 of 5: malformed Last-Event-ID"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 3 of 5: malformed Last-Event-ID"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "400 validation_error, Last-Event-ID"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-4" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 4 of 5: audit ledger unavailable"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 4 of 5: audit ledger unavailable"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "503 audit_unavailable"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-5" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 1 of 9"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 5 of 5: attached"
    qbrain.server.am1HvHandlers -> qbrain.db "RecordRead, then stored chunks after the position"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-5-part2" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 2 of 9"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "hv_output events with id runId and sequence"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-5-part3" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 3 of 9"
    qbrain.server.am1HvWorkerHost -> qbrain.server.am1HvHandlers "wake after each append"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-5-part4" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 4 of 9"
    qbrain.server.am1HvHandlers -> qbrain.db "new chunks"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-5-part5" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 5 of 9"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "hv_output events, live while the reviewer runs"
    qbrain.libs.client -> qbrain.libs.client "Note over the caller and the API: the connection drops"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-5-part6" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 6 of 9"
    qbrain.libs.client -> qbrain.server.api "GET the same stream with Last-Event-ID runId and sequence"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-5-part7" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 7 of 9"
    qbrain.server.api -> qbrain.server.am1HvHandlers "QueryAsync"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-5-part8" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 8 of 9"
    qbrain.server.am1HvHandlers -> qbrain.db "chunks after that sequence, then later runs"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt1-5-part9" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 9 of 9"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "the missed events, then live events"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt2-1" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 2 branch 1 of 2: a stored chunk fails to decompress"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 1 of 2: a stored chunk fails to decompress"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "hv_stream_error with code evidence_corrupt, then the stream ends"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-12-alt2-2" "Sequence diagram converted from fence F81 (line 2391)." {
    title "StreamHvRunOutputQuery with a reconnect, alt 2 branch 2 of 2: every chunk sent"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 2 branch 2 of 2: every chunk sent"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "hv_run_closed, then the stream ends when the request is terminal"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-13" "Sequence diagram converted from fence F82 (line 2429)." {
    title "StreamHvNotificationsQuery through the plugin watcher, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.server.api "REPL workflow.hv.watchNotifications from lastSequence"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-13-part2" "Sequence diagram converted from fence F82 (line 2429)." {
    title "StreamHvNotificationsQuery through the plugin watcher, part 2 of 2"
    qbrain.server.api -> qbrain.server.am1HvHandlers "QueryAsync, after the workspace behavior"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-13-alt1-1" "Sequence diagram converted from fence F82 (line 2429)." {
    title "StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 1 of 2: malformed Last-Event-ID"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 1 of 2: malformed Last-Event-ID"
    qbrain.server.am1HvHandlers -> qbrain.plugins.pluginPs "400 validation_error, Last-Event-ID"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-13-alt1-2" "Sequence diagram converted from fence F82 (line 2429)." {
    title "StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 1 of 7"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 2 of 2: attached"
    qbrain.server.am1HvHandlers -> qbrain.db "notifications after lastSequence"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-13-alt1-2-part2" "Sequence diagram converted from fence F82 (line 2429)." {
    title "StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 2 of 7"
    qbrain.server.am1HvHandlers -> qbrain.plugins.pluginPs "events in sequence order"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) record lastSequence and inbox the completions its session"
    autolayout lr 300 100
}
dynamic qbrain.plugins "AM1-17-13-alt1-2-part3" "Sequence diagram converted from fence F82 (line 2429)." {
    title "StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 3 of 7"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) submitted, one atomic replace"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-13-alt1-2-part4" "Sequence diagram converted from fence F82 (line 2429)." {
    title "StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 4 of 7"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note over Plugin watcher and the API: the connection drops"
    qbrain.plugins.pluginPs -> qbrain.server.api "reconnect with Last-Event-ID lastSequence, with backoff"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-13-alt1-2-part5" "Sequence diagram converted from fence F82 (line 2429)." {
    title "StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 5 of 7"
    qbrain.server.am1HvHandlers -> qbrain.plugins.pluginPs "the missed events, then live events"
    autolayout lr 300 100
}
dynamic qbrain.plugins "AM1-17-13-alt1-2-part6" "Sequence diagram converted from fence F82 (line 2429)." {
    title "StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 6 of 7"
    qbrain.plugins.pluginHosts -> qbrain.plugins.pluginPs "read the inbox at the next prompt"
    qbrain.plugins.pluginHosts -> qbrain.plugins.pluginHosts "(1 of 2) add each undelivered completion to the prompt context, then"
    autolayout lr 300 100
}
dynamic qbrain.plugins "AM1-17-13-alt1-2-part7" "Sequence diagram converted from fence F82 (line 2429)." {
    title "StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 7 of 7"
    qbrain.plugins.pluginHosts -> qbrain.plugins.pluginHosts "(2 of 2) mark it delivered (at least once)"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-14" "Sequence diagram converted from fence F83 (line 2454)." {
    title "QueryHvNotificationsQuery and QueryHvIncidentsQuery, part 1 of 2"
    qbrain.libs.client -> qbrain.server.api "GET notifications with afterSequence and limit, or GET incidents with filters"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-14-part2" "Sequence diagram converted from fence F83 (line 2454)." {
    title "QueryHvNotificationsQuery and QueryHvIncidentsQuery, part 2 of 2"
    qbrain.server.api -> qbrain.server.am1HvHandlers "QueryAsync, after the workspace behavior"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-14-alt1-1" "Sequence diagram converted from fence F83 (line 2454)." {
    title "QueryHvNotificationsQuery and QueryHvIncidentsQuery, alt 1 branch 1 of 2: a parameter out of range or an unknown filter"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 1 of 2: a parameter out of range or an unknown filter"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-14-alt1-2" "Sequence diagram converted from fence F83 (line 2454)." {
    title "QueryHvNotificationsQuery and QueryHvIncidentsQuery, alt 1 branch 2 of 2: valid, part 1 of 2"
    qbrain.server.am1HvHandlers -> qbrain.server.am1HvHandlers "alt 1 branch 2 of 2: valid"
    qbrain.server.am1HvHandlers -> qbrain.db "rows after the sequence, or PagedQuery of incidents"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-14-alt1-2-part2" "Sequence diagram converted from fence F83 (line 2454)." {
    title "QueryHvNotificationsQuery and QueryHvIncidentsQuery, alt 1 branch 2 of 2: valid, part 2 of 2"
    qbrain.server.am1HvHandlers -> qbrain.libs.client "200 items and lastSequence, or a PageResult of incidents"
    autolayout lr 300 100
}
dynamic qbrain.plugins "AM1-17-15" "Sequence diagram converted from fence F84 (line 2472)." {
    title "A write to a frozen turn, and a hostile review from an agent surface, part 1 of 9"
    strHostModel -> qbrain.plugins.pluginPs "any session-log verb for a turn under HV"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-15-part2" "Sequence diagram converted from fence F84 (line 2472)." {
    title "A write to a frozen turn, and a hostile review from an agent surface, part 2 of 9"
    qbrain.plugins.pluginPs -> qbrain.server.api "the verb's route, through the REPL"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-15-part3" "Sequence diagram converted from fence F84 (line 2472)." {
    title "A write to a frozen turn, and a hostile review from an agent surface, part 3 of 9"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "the foundation unit, through TurnWritePipeline"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-15-part4" "Sequence diagram converted from fence F84 (line 2472)." {
    title "A write to a frozen turn, and a hostile review from an agent surface, part 4 of 9"
    qbrain.server.strSessionLogHandlers -> qbrain.db "TurnLocator, then policy HvTurnFreeze Guard"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "AM1-17-15-part5" "Sequence diagram converted from fence F84 (line 2472)." {
    title "A write to a frozen turn, and a hostile review from an agent surface, part 5 of 9"
    qbrain.plugins.pluginPs -> strHostModel "final refusal: record later changes in a later turn"
    strHostModel -> qbrain.plugins.pluginPs "recordReview hostile with status Pass, Fail, Running, or an hvVerdict"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-15-part6" "Sequence diagram converted from fence F84 (line 2472)." {
    title "A write to a frozen turn, and a hostile review from an agent surface, part 6 of 9"
    qbrain.plugins.pluginPs -> qbrain.server.api "RecordSessionTurnReviewCommand"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-15-part7" "Sequence diagram converted from fence F84 (line 2472)." {
    title "A write to a frozen turn, and a hostile review from an agent surface, part 7 of 9"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "handler runs"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-15-part8" "Sequence diagram converted from fence F84 (line 2472)." {
    title "A write to a frozen turn, and a hostile review from an agent surface, part 8 of 9"
    qbrain.server.strSessionLogHandlers -> qbrain.plugins.pluginPs "403 review_hostile_pipeline_only"
    autolayout lr 300 100
}
dynamic qbrain.plugins "AM1-17-15-part9" "Sequence diagram converted from fence F84 (line 2472)." {
    title "A write to a frozen turn, and a hostile review from an agent surface, part 9 of 9"
    qbrain.plugins.pluginPs -> strHostModel "final refusal: request an HV instead"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-15-alt1-1" "Sequence diagram converted from fence F84 (line 2472)." {
    title "A write to a frozen turn, and a hostile review from an agent surface, alt 1 branch 1 of 2: turn Pending"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1 branch 1 of 2: turn Pending"
    qbrain.server.strSessionLogHandlers -> qbrain.plugins.pluginPs "409 turn_immutable, reason hv_pending, hvRequestId"
    autolayout lr 300 100
}
dynamic qbrain.server "AM1-17-15-alt1-2" "Sequence diagram converted from fence F84 (line 2472)." {
    title "A write to a frozen turn, and a hostile review from an agent surface, alt 1 branch 2 of 2: turn Final"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1 branch 2 of 2: turn Final"
    qbrain.server.strSessionLogHandlers -> qbrain.plugins.pluginPs "409 turn_immutable, reason hv_final, hvRequestId"
    autolayout lr 300 100
}
custom "AM1-20-01" "Delivery order of the HV phases, part 1 of 4" "Delivery order converted from fence F98 (line 2957)." {
    include am1AM12001_PLANSESSIONTURNREVIEW001DeliversTheFoundation am1AM12001_H0Phases1To3For am1BcHvStorageSchema am1BcHvSharedComponents
    autolayout tb 100 200
}
custom "AM1-20-01-part2" "Delivery order of the HV phases, part 2 of 4" "Delivery order converted from fence F98 (line 2957)." {
    include am1BcHvSharedComponents am1BcHvReviewerPool am1BcHvAdmission am1BcHvExecution
    autolayout tb 100 200
}
custom "AM1-20-01-part3" "Delivery order of the HV phases, part 3 of 4" "Delivery order converted from fence F98 (line 2957)." {
    include am1BcHvExecution am1BcHvFinalization am1BcHvRestSurfaces am1BcHvStreamsAndOperations
    autolayout tb 100 200
}
custom "AM1-20-01-part4" "Delivery order of the HV phases, part 4 of 4" "Delivery order converted from fence F98 (line 2957)." {
    include am1BcHvStreamsAndOperations am1BcHvClientParity am1AM12001_IntegrationLiveChecksIndependentValidationOperator
    autolayout tb 100 200
}
custom "AM1-B-01" "References between the groups (entities), part 1 of 5" "Entity diagram converted from fence F99 (line 3071)." {
    include entSessionLog am1EntHvRequest entSessionLogTurn
    exclude "am1EntHvRequest -> entSessionLogTurn"
    exclude "entSessionLog -> entSessionLogTurn"
    autolayout tb 200 300
}
custom "AM1-B-01-part2" "References between the groups (entities), part 2 of 5" "Entity diagram converted from fence F99 (line 3071)." {
    include entSessionLogTurn am1EntHvRequestTurn am1EntHvRun
    autolayout lr 300 200
}
custom "AM1-B-01-part3" "References between the groups (entities), part 3 of 5" "Entity diagram converted from fence F99 (line 3071)." {
    include entRequirement am1EntHvRequestRequirementRef
    autolayout lr 300 200
}
custom "AM1-B-01-part4" "References between the groups (entities), part 4 of 5" "Entity diagram converted from fence F99 (line 3071)." {
    include entTriageReport am1EntHvIncident
    autolayout lr 300 200
}
custom "AM1-B-01-part5" "References between the groups (entities), part 5 of 5" "Entity diagram converted from fence F99 (line 3071)." {
    include am1EntHvReviewer am1EntHvRequest am1EntHvRun
    exclude "am1EntHvRequest -> am1EntHvRun"
    autolayout tb 200 300
}
custom "AM1-B-02" "Reviewer pool (entities)" "Entity diagram converted from fence F100 (line 3087)." {
    include am1EntHvWorkspaceState am1EntHvReviewer
    autolayout lr 300 200
}
custom "AM1-B-03" "Requests and membership (entities), part 1 of 2" "Entity diagram converted from fence F101 (line 3109)." {
    include am1EntHvRequest am1EntHvRequestTurn am1EntHvRequestRequirementRef
    autolayout lr 300 200
}
custom "AM1-B-03-part2" "Requests and membership (entities), part 2 of 2" "Entity diagram converted from fence F101 (line 3109)." {
    include am1EntHvRequest am1EntHvRequestResponse strEntAgentResponse
    exclude "strEntAgentResponse -> strEntAgentResponse"
    autolayout lr 300 200
    properties {
        "column.AgentResponse.ResponseId" "string PK"
    }
}
custom "AM1-B-04" "Runs and output (entities)" "Entity diagram converted from fence F102 (line 3161)." {
    include am1EntHvRequest am1EntHvRun am1EntHvRunOutputChunk
    autolayout tb 200 300
}
custom "AM1-B-05" "Results and findings (entities)" "Entity diagram converted from fence F103 (line 3195)." {
    include am1EntHvRequest am1EntHvTurnAssessment am1EntHvFinding
    autolayout lr 300 200
}
custom "AM1-B-06" "Evidence (entities)" "Entity diagram converted from fence F104 (line 3227)." {
    include am1EntHvRequest am1EntHvEvidence
    autolayout lr 300 200
}
custom "AM1-B-07" "Notifications and incidents (entities)" "Entity diagram converted from fence F105 (line 3247)." {
    include am1EntHvRun am1EntHvIncident am1EntHvNotification
    autolayout lr 300 200
}
custom "AM1-B-08" "Changed foundation entities (entities)" "Entity diagram converted from fence F106 (line 3278)." {
    include am1EntHvRequest entSessionLogTurn strEntAgentResponseGrade
    exclude "entSessionLogTurn -> am1EntHvRequest"
    exclude "entSessionLogTurn -> strEntAgentResponseGrade"
    autolayout tb 200 300
    properties {
        "column.SessionLogTurn.Id" "long PK"
        "column.SessionLogTurn.RequestId" "string UK"
        "column.SessionLogTurn.HvState" "string default None"
        "column.SessionLogTurn.HvRequestId" "string"
        "column.AgentResponseGrade.GradeId" "string PK"
        "column.AgentResponseGrade.ResponseId" "string FK"
        "column.AgentResponseGrade.HvRequestId" "string FK"
        "column.AgentResponseGrade.RunId" "string"
    }
}
custom "AM1-B-09" "HostileReview worker storage (section 16.7) (entities), part 1 of 2" "Entity diagram converted from fence F107 (line 3303)." {
    include entHostileReviewRequest am1EntHostileReviewAttempt entHostileReviewExecution
    autolayout tb 200 300
    properties {
        "column.HostileReviewRequest.RequestId" "string PK"
        "column.HostileReviewRequest.WorkspaceId" "string"
        "column.HostileReviewRequest.Status" "string"
        "column.HostileReviewRequest.AcceptedResultId" "string"
        "column.HostileReviewRequest.AttemptCount" "int"
        "column.HostileReviewRequest.IsDeleted" "bool"
        "column.HostileReviewExecution.ExecutionId" "string PK"
        "column.HostileReviewExecution.RequestId" "string FK"
    }
}
custom "AM1-B-09-part2" "HostileReview worker storage (section 16.7) (entities), part 2 of 2" "Entity diagram converted from fence F107 (line 3303)." {
    include entHostileReviewRequest entHostileReviewDiagnostic
    autolayout lr 300 200
    properties {
        "column.HostileReviewRequest.RequestId" "string PK"
        "column.HostileReviewRequest.WorkspaceId" "string"
        "column.HostileReviewRequest.Status" "string"
        "column.HostileReviewRequest.AcceptedResultId" "string"
        "column.HostileReviewRequest.AttemptCount" "int"
        "column.HostileReviewRequest.IsDeleted" "bool"
        "column.HostileReviewDiagnostic.DiagnosticId" "long PK"
        "column.HostileReviewDiagnostic.RequestId" "string FK"
        "column.HostileReviewDiagnostic.Code" "string"
    }
}
component qbrain.plugins "AM1-04-01" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 1 of 13"
    include qbrain.plugins.pluginPs qbrain.repl
    exclude "relationship.tag!=Am1Context"
    autolayout lr 300 100
}
component qbrain.server "AM1-04-01-part2" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 2 of 13"
    include qbrain.repl qbrain.server.api mktDirector
    exclude "relationship.tag!=Am1Context"
    autolayout tb 300 100
}
component qbrain.server "AM1-04-01-part3" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 3 of 13"
    include agent qbrain.server.api
    exclude "relationship.tag!=Am1Context"
    autolayout tb 300 100
}
component qbrain.server "AM1-04-01-part4" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 4 of 13"
    include qbrain.server.api qbrain.server.cqrsLib qbrain.server.am1HvWorkerHost
    exclude "relationship.tag!=Am1Context"
    autolayout tb 300 100
}
component qbrain.server "AM1-04-01-part5" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 5 of 13"
    include qbrain.server.cqrsLib qbrain.server.am1HvHandlers qbrain.server.am1HvFinalizer
    exclude "relationship.tag!=Am1Context"
    autolayout tb 300 100
}
component qbrain.server "AM1-04-01-part6" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 6 of 13"
    include qbrain.server.am1HvHandlers qbrain.db qbrain.server.strSessionLogHandlers
    exclude "relationship.tag!=Am1Context"
    autolayout tb 300 100
}
component qbrain.server "AM1-04-01-part7" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 7 of 13"
    include qbrain.server.am1HvWorkerHost qbrain.server.am1ReviewerAdapters qbrain.server.triage
    exclude "relationship.tag!=Am1Context"
    autolayout tb 300 100
}
component qbrain.server "AM1-04-01-part8" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 8 of 13"
    include qbrain.server.am1ReviewerAdapters llm
    exclude "relationship.tag!=Am1Context"
    autolayout tb 300 100
}
component qbrain.server "AM1-04-01-part9" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 9 of 13"
    include qbrain.server.am1HvFinalizer qbrain.server.strSessionLogHandlers qbrain.server.strAgentContextHandlers
    exclude "relationship.tag!=Am1Context"
    autolayout tb 300 100
}
component qbrain.server "AM1-04-01-part10" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 10 of 13"
    include qbrain.server.strAgentContextHandlers qbrain.db
    exclude "relationship.tag!=Am1Context"
    autolayout tb 300 100
}
component qbrain.server "AM1-04-01-part11" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 11 of 13"
    include qbrain.server.am1HvFinalizer qbrain.db
    exclude "relationship.tag!=Am1Context"
    autolayout tb 300 100
}
component qbrain.server "AM1-04-01-part12" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 12 of 13"
    include qbrain.server.am1HvHandlers qbrain.server.am1ChangeEventBus
    exclude "relationship.tag!=Am1Context"
    autolayout tb 300 100
}
component qbrain.server "AM1-04-01-part13" "Structure diagram converted from fence F1 (line 90)." {
    title "System context and component ownership, part 13 of 13"
    include qbrain.server.am1ChangeEventBus qbrain.server.am1HvWorkerHost
    exclude "relationship.tag!=Am1Context"
    autolayout tb 300 100
}
container qbrain "AM1-18-01" "Use-case diagram converted from fence F85 (line 2502)." {
    title "Use cases: overview (use cases), part 1 of 10"
    include developer am1UcManageTheReviewerPool am1UcNONCEAReviewer
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "AM1-18-01-part2" "Use-case diagram converted from fence F85 (line 2502)." {
    title "Use cases: overview (use cases), part 2 of 10"
    include developer am1UcCancelAnHV am1UcRemoveEvidenceOnInstruction
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "AM1-18-01-part3" "Use-case diagram converted from fence F85 (line 2502)." {
    title "Use cases: overview (use cases), part 3 of 10"
    include strHostModel am1UcSubmitAnHVRequest am1UcReceiveCompletionNotifications
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "AM1-18-01-part4" "Use-case diagram converted from fence F85 (line 2502)." {
    title "Use cases: overview (use cases), part 4 of 10"
    include strHostModel am1UcRemediateAfterAReviewedTurn
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.libs "AM1-18-01-part5" "Use-case diagram converted from fence F85 (line 2502)." {
    title "Use cases: overview (use cases), part 5 of 10"
    include qbrain.libs.client am1UcSubmitAnHVRequest am1UcWatchAnHVLive
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.libs "AM1-18-01-part6" "Use-case diagram converted from fence F85 (line 2502)." {
    title "Use cases: overview (use cases), part 6 of 10"
    include qbrain.libs.client am1UcCancelAnHV am1UcRetrieveTheResultAndEvidence
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.server "AM1-18-01-part7" "Use-case diagram converted from fence F85 (line 2502)." {
    title "Use cases: overview (use cases), part 7 of 10"
    include qbrain.server.am1HvWorkerHost am1UcNONCEAReviewer am1UcRunAReviewLive
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.server "AM1-18-01-part8" "Use-case diagram converted from fence F85 (line 2502)." {
    title "Use cases: overview (use cases), part 8 of 10"
    include qbrain.server.am1HvWorkerHost am1UcFinalizeTheHVResult am1UcRecoverFromFailures
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "AM1-18-01-part9" "Use-case diagram converted from fence F85 (line 2502)." {
    title "Use cases: overview (use cases), part 9 of 10"
    include llm am1UcNONCEAReviewer am1UcRunAReviewLive
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.server "AM1-18-01-part10" "Use-case diagram converted from fence F85 (line 2502)." {
    title "Use cases: overview (use cases), part 10 of 10"
    include qbrain.server.triage am1UcRecoverFromFailures
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-01" "Use-case diagram converted from fence F86 (line 2546)." {
    title "UC-HV-01 Manage the reviewer pool (use cases), part 1 of 3"
    include developer am1UcManageTheReviewerPool am1UcListAndReadReviewers
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 200 100
}
container qbrain "UC-HV-01-part2" "Use-case diagram converted from fence F86 (line 2546)." {
    title "UC-HV-01 Manage the reviewer pool (use cases), part 2 of 3"
    include am1UcManageTheReviewerPool am1UcUpdateWithTheStateVersionUpdateHvReviewerCommand am1UcLaunchChangedUnverifiedThenNONCE
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 200 100
}
container qbrain "UC-HV-01-part3" "Use-case diagram converted from fence F86 (line 2546)." {
    title "UC-HV-01 Manage the reviewer pool (use cases), part 3 of 3"
    include am1UcStaleStateVersion409Conflict am1UcManageTheReviewerPool am1UcAuditLedgerUnavailable503
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-02" "Use-case diagram converted from fence F87 (line 2560)." {
    title "UC-HV-02 NONCE a reviewer (use cases), part 1 of 7"
    include developer am1UcNONCEAReviewer agent
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.server "UC-HV-02-part2" "Use-case diagram converted from fence F87 (line 2560)." {
    title "UC-HV-02 NONCE a reviewer (use cases), part 2 of 7"
    include qbrain.server.am1HvWorkerHost am1UcNONCEAReviewer llm
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-02-part3" "Use-case diagram converted from fence F87 (line 2560)." {
    title "UC-HV-02 NONCE a reviewer (use cases), part 3 of 7"
    include am1UcNONCEAReviewer am1UcProbeRunWithAFreshNonce am1UcDecideReadinessProbeVerdict
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-02-part4" "Use-case diagram converted from fence F87 (line 2560)." {
    title "UC-HV-02 NONCE a reviewer (use cases), part 4 of 7"
    include am1UcAlreadyRequestedOrRunningNoSecond am1UcNONCEAReviewer am1UcDisabledReviewer409
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-02-part5" "Use-case diagram converted from fence F87 (line 2560)." {
    title "UC-HV-02 NONCE a reviewer (use cases), part 5 of 7"
    include am1UcProbeFailedOfflineIncidentFiled am1UcNONCEAReviewer am1UcWorkerDiedProbeRequestedAgain
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-02-part6" "Use-case diagram converted from fence F87 (line 2560)." {
    title "UC-HV-02 NONCE a reviewer (use cases), part 6 of 7"
    include am1UcStoredLogCorruptServerIncidentReadiness am1UcNONCEAReviewer am1UcReconfiguredWhileProbingStaleProbeReadiness
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-02-part7" "Use-case diagram converted from fence F87 (line 2560)." {
    title "UC-HV-02 NONCE a reviewer (use cases), part 7 of 7"
    include am1UcRestartAfterASettledProbeNo am1UcNONCEAReviewer
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-03" "Use-case diagram converted from fence F88 (line 2580)." {
    title "UC-HV-03 Submit an HV request (use cases), part 1 of 7"
    include strHostModel am1UcSubmitAnHVRequest developer
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-03-part2" "Use-case diagram converted from fence F88 (line 2580)." {
    title "UC-HV-03 Submit an HV request (use cases), part 2 of 7"
    include am1UcSubmitAnHVRequest am1UcResolveMembersHvScopeResolution am1UcCheckEligibilityHvAdmissionEligibility
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-03-part3" "Use-case diagram converted from fence F88 (line 2580)." {
    title "UC-HV-03 Submit an HV request (use cases), part 3 of 7"
    include am1UcSubmitAnHVRequest am1UcAssignRoundRobinSubmitterAndAuthor am1UcStoreTheAcceptedRequestAndFreeze
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-03-part4" "Use-case diagram converted from fence F88 (line 2580)." {
    title "UC-HV-03 Submit an HV request (use cases), part 4 of 7"
    include am1UcSameKeyAndInputsStoredRequest am1UcSubmitAnHVRequest am1UcBadOversizedSelfIncludingOrEmpty
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-03-part5" "Use-case diagram converted from fence F88 (line 2580)." {
    title "UC-HV-03 Submit an HV request (use cases), part 5 of 7"
    include am1UcNotTerminalPendingOrReviewed409 am1UcSubmitAnHVRequest am1UcNoEligibleReviewer409WithReviewer
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-03-part6" "Use-case diagram converted from fence F88 (line 2580)." {
    title "UC-HV-03 Submit an HV request (use cases), part 6 of 7"
    include am1UcUnnecessaryTurnIncludedAndGraded am1UcSubmitAnHVRequest am1UcSelfReviewAbsentOrAnyGrade
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-03-part7" "Use-case diagram converted from fence F88 (line 2580)." {
    title "UC-HV-03 Submit an HV request (use cases), part 7 of 7"
    include am1UcAuditLedgerUnavailable503NothingWritten am1UcSubmitAnHVRequest
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.server "UC-HV-04" "Use-case diagram converted from fence F89 (line 2601)." {
    title "UC-HV-04 Run a review live (use cases), part 1 of 6"
    include qbrain.server.am1HvWorkerHost am1UcRunAReviewLive llm
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-04-part2" "Use-case diagram converted from fence F89 (line 2601)." {
    title "UC-HV-04 Run a review live (use cases), part 2 of 6"
    include am1UcRunAReviewLive am1UcClaimWithLeaseAndFenceClaimHvRunCommand am1UcStreamEveryLineAsItIs
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-04-part3" "Use-case diagram converted from fence F89 (line 2601)." {
    title "UC-HV-04 Run a review live (use cases), part 3 of 6"
    include am1UcRunAReviewLive am1UcHeartbeatRecordHvRunHeartbeatCommand am1UcCloseTheOutputCloseHvRunOutputCommand
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-04-part4" "Use-case diagram converted from fence F89 (line 2601)." {
    title "UC-HV-04 Run a review live (use cases), part 4 of 6"
    include am1UcAssigneeIneligibleReassignedAtClaim am1UcRunAReviewLive am1UcCancelReviewerStopped
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-04-part5" "Use-case diagram converted from fence F89 (line 2601)." {
    title "UC-HV-04 Run a review live (use cases), part 5 of 6"
    include am1UcLaunchFailureOrNonzeroExitUC am1UcRunAReviewLive am1UcLeaseLostStaleFenceWorkerStops
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-04-part6" "Use-case diagram converted from fence F89 (line 2601)." {
    title "UC-HV-04 Run a review live (use cases), part 6 of 6"
    include am1UcNoEligibleReviewerWaitsQueuedETA am1UcRunAReviewLive am1UcAcceptedRequestCorruptAtTheBrief
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.server "UC-HV-05" "Use-case diagram converted from fence F90 (line 2621)." {
    title "UC-HV-05 Finalize the HV result (use cases), part 1 of 5"
    include qbrain.server.am1HvWorkerHost am1UcFinalizeTheHVResult am1UcVerifyV1ToV8
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 200 100
}
container qbrain "UC-HV-05-part2" "Use-case diagram converted from fence F90 (line 2621)." {
    title "UC-HV-05 Finalize the HV result (use cases), part 2 of 5"
    include am1UcFinalizeTheHVResult am1UcCommitEverythingAtOnceCommitFinalResult am1UcHostileReviewPerTurnHvVerdictMapping
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-05-part3" "Use-case diagram converted from fence F90 (line 2621)." {
    title "UC-HV-05 Finalize the HV result (use cases), part 3 of 5"
    include am1UcVerificationFailedAttemptFailsRequestRequeued am1UcFinalizeTheHVResult am1UcStateChangedTransactionRunsAgain
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-05-part4" "Use-case diagram converted from fence F90 (line 2621)." {
    title "UC-HV-05 Finalize the HV result (use cases), part 4 of 5"
    include am1UcAuditUnavailableStaysFinalizingForThe am1UcFinalizeTheHVResult am1UcAlreadyFinalizedStoredOutcome
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-05-part5" "Use-case diagram converted from fence F90 (line 2621)." {
    title "UC-HV-05 Finalize the HV result (use cases), part 5 of 5"
    include am1UcAgentContextOffResponsesNotGraded am1UcFinalizeTheHVResult am1UcResultCompressionFailedRolledBackAttempt
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.libs "UC-HV-06" "Use-case diagram converted from fence F91 (line 2639)." {
    title "UC-HV-06 Watch an HV live (use cases), part 1 of 4"
    include qbrain.libs.client am1UcWatchAnHVLive am1UcListRunningHVsQueryHvRequestsQuery
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 200 100
}
container qbrain "UC-HV-06-part2" "Use-case diagram converted from fence F91 (line 2639)." {
    title "UC-HV-06 Watch an HV live (use cases), part 2 of 4"
    include am1UcWatchAnHVLive am1UcProgressETALastOutputGetHvRequestQuery am1UcAttachToTheOutputStreamStreamHvRunOutputQuery
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-06-part3" "Use-case diagram converted from fence F91 (line 2639)." {
    title "UC-HV-06 Watch an HV live (use cases), part 3 of 4"
    include am1UcDroppedReconnectWithLastEventID am1UcWatchAnHVLive am1UcNoProgressReportedETAUnknown
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-06-part4" "Use-case diagram converted from fence F91 (line 2639)." {
    title "UC-HV-06 Watch an HV live (use cases), part 4 of 4"
    include am1UcNewAttemptStreamContinues am1UcWatchAnHVLive
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.plugins "UC-HV-07" "Use-case diagram converted from fence F92 (line 2654)." {
    title "UC-HV-07 Receive completion notifications (use cases), part 1 of 5"
    include qbrain.plugins.pluginPs am1UcReceiveCompletionNotifications strHostModel
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.libs "UC-HV-07-part2" "Use-case diagram converted from fence F92 (line 2654)." {
    title "UC-HV-07 Receive completion notifications (use cases), part 2 of 5"
    include mktDirector am1UcReceiveCompletionNotifications qbrain.libs.client
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-07-part3" "Use-case diagram converted from fence F92 (line 2654)." {
    title "UC-HV-07 Receive completion notifications (use cases), part 3 of 5"
    include am1UcReceiveCompletionNotifications am1UcFollowTheStreamFromTheLast am1UcInboxAndDeliverAtTheNext
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-07-part4" "Use-case diagram converted from fence F92 (line 2654)." {
    title "UC-HV-07 Receive completion notifications (use cases), part 4 of 5"
    include am1UcDroppedReconnectFromTheRecordedSequence am1UcReceiveCompletionNotifications am1UcNoStreamsPollQueryHvNotificationsQuery
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-07-part5" "Use-case diagram converted from fence F92 (line 2654)." {
    title "UC-HV-07 Receive completion notifications (use cases), part 5 of 5"
    include am1UcDeliveredTwiceDeduplicatedBySequence am1UcReceiveCompletionNotifications
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.libs "UC-HV-08" "Use-case diagram converted from fence F93 (line 2669)." {
    title "UC-HV-08 Cancel an HV (use cases), part 1 of 4"
    include qbrain.libs.client am1UcCancelAnHV am1UcCancelCancelHvRequestCommand
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 200 100
}
container qbrain "UC-HV-08-part2" "Use-case diagram converted from fence F93 (line 2669)." {
    title "UC-HV-08 Cancel an HV (use cases), part 2 of 4"
    include am1UcRunningWorkerStopsTheReviewerAt am1UcCancelAnHV am1UcAlreadyCanceledUnchanged
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-08-part3" "Use-case diagram converted from fence F93 (line 2669)." {
    title "UC-HV-08 Cancel an HV (use cases), part 3 of 4"
    include am1UcFinalizingOrTerminal409 am1UcCancelAnHV am1UcWorkerDeadLeaseExpiryCompletesThe
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-08-part4" "Use-case diagram converted from fence F93 (line 2669)." {
    title "UC-HV-08 Cancel an HV (use cases), part 4 of 4"
    include am1UcExitedBeforeTheHeartbeatTheClose am1UcCancelAnHV
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.server "UC-HV-09" "Use-case diagram converted from fence F94 (line 2684)." {
    title "UC-HV-09 Recover from failures (use cases), part 1 of 5"
    include qbrain.server.am1HvWorkerHost am1UcRecoverFromFailures qbrain.server.triage
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-09-part2" "Use-case diagram converted from fence F94 (line 2684)." {
    title "UC-HV-09 Recover from failures (use cases), part 2 of 5"
    include am1UcRecoverFromFailures am1UcRecordTheFailureFailHvRunCommand am1UcFileTheIncidentFileHvIncidentCommand
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-09-part3" "Use-case diagram converted from fence F94 (line 2684)." {
    title "UC-HV-09 Recover from failures (use cases), part 3 of 5"
    include am1UcRecoverFromFailures am1UcRequeueAndReassignHvRetryDecisionRoundRobinAssignment am1UcAttemptsSpentRequestFailedTurnsReleased
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 200 100
}
container qbrain "UC-HV-09-part4" "Use-case diagram converted from fence F94 (line 2684)." {
    title "UC-HV-09 Recover from failures (use cases), part 4 of 5"
    include am1UcTriageUnreachableFilingRetried am1UcRecoverFromFailures am1UcDeadWorkerLeasesSweptRunsFenced
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-09-part5" "Use-case diagram converted from fence F94 (line 2684)." {
    title "UC-HV-09 Recover from failures (use cases), part 5 of 5"
    include am1UcGracefulShutdownNotCounted am1UcRecoverFromFailures am1UcTwoSweepsOnOneExpiredRun
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
component qbrain.libs "UC-HV-10" "Use-case diagram converted from fence F95 (line 2702)." {
    title "UC-HV-10 Retrieve the result and evidence (use cases), part 1 of 4"
    include qbrain.libs.client am1UcRetrieveTheResultAndEvidence am1UcResultDocumentGetHvResultQuery
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 200 100
}
container qbrain "UC-HV-10-part2" "Use-case diagram converted from fence F95 (line 2702)." {
    title "UC-HV-10 Retrieve the result and evidence (use cases), part 2 of 4"
    include am1UcRetrieveTheResultAndEvidence am1UcEvidenceDownloadGetHvEvidenceQuery am1UcAuditEveryReadAuditLedgerGuard
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-10-part3" "Use-case diagram converted from fence F95 (line 2702)." {
    title "UC-HV-10 Retrieve the result and evidence (use cases), part 3 of 4"
    include am1UcNotReady409 am1UcRetrieveTheResultAndEvidence am1UcRemoved410WithHashes
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-10-part4" "Use-case diagram converted from fence F95 (line 2702)." {
    title "UC-HV-10 Retrieve the result and evidence (use cases), part 4 of 4"
    include am1UcAuditUnavailable503 am1UcRetrieveTheResultAndEvidence
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-11" "Use-case diagram converted from fence F96 (line 2717)." {
    title "UC-HV-11 Remove evidence on instruction (use cases), part 1 of 2"
    include developer am1UcRemoveEvidenceOnInstruction am1UcRemoveWithReasonAndNameRemoveHvEvidenceCommand
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 200 100
}
container qbrain "UC-HV-11-part2" "Use-case diagram converted from fence F96 (line 2717)." {
    title "UC-HV-11 Remove evidence on instruction (use cases), part 2 of 2"
    include am1UcRequestActive409 am1UcRemoveEvidenceOnInstruction am1UcAlreadyRemovedUnchanged
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-12" "Use-case diagram converted from fence F97 (line 2729)." {
    title "UC-HV-12 Remediate after a reviewed turn (use cases), part 1 of 4"
    include strHostModel am1UcRemediateAfterAReviewedTurn am1UcReadTheFailuresAndRemediation
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 200 100
}
container qbrain "UC-HV-12-part2" "Use-case diagram converted from fence F97 (line 2729)." {
    title "UC-HV-12 Remediate after a reviewed turn (use cases), part 2 of 4"
    include am1UcRemediateAfterAReviewedTurn am1UcNewTurnsNewHVRequest am1UcWriteToAReviewedTurn409
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 200 100
}
container qbrain "UC-HV-12-part3" "Use-case diagram converted from fence F97 (line 2729)." {
    title "UC-HV-12 Remediate after a reviewed turn (use cases), part 3 of 4"
    include am1UcOwnHostileReview403 am1UcRemediateAfterAReviewedTurn am1UcSameTurnsAgain409AlreadyReviewed
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
container qbrain "UC-HV-12-part4" "Use-case diagram converted from fence F97 (line 2729)." {
    title "UC-HV-12 Remediate after a reviewed turn (use cases), part 4 of 4"
    include am1UcAGREETODOUnchangedCoordinatorDecides am1UcRemediateAfterAReviewedTurn
    exclude "relationship.tag!=Am1UseCase"
    autolayout tb 400 250
}
