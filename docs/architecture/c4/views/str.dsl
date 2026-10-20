# STR views (PLAN-C4MODEL-001 P4, D5 to D7): one or more views per Mermaid fence of
# docs/architecture/SessionTurn-Review-AgentContext-Architecture.md, split to the AC-5 letter page. Converted once by
# SCRATCH/p4-str/convert-str.ps1 (D9); edit here from now on.
custom "STR-03-01" "Tool-audit hook of the TypeScript hosts (activity), part 1 of 2" "Activity diagram converted from fence F2 (line 247)." {
    include strSTR0301_ToolAuditHookPhaseBeforeOr strSTR0301_ReadTheToolNameInputAnd strSTR0301_ProcessBindVerbToTurnAsAHookInvocation
    autolayout tb 100 200
}
custom "STR-03-01-part2" "Tool-audit hook of the TypeScript hosts (activity), part 2 of 2" "Activity diagram converted from fence F2 (line 247)." {
    include strSTR0301_ProcessBindVerbToTurnAsAHookInvocation strSTR0301_NothingWrittenExit0EmptyResult strSTR0301_ProcessPersistTurnWriteWithReceiptAppendDialogToolCallOr strSTR0301_Exit0EmptyResultTheTool strSTR0301_FailureWrittenToStderrExit0
    autolayout tb 100 100
}
custom "RequestPipeline" "RequestPipeline: WorkspaceResolutionBehavior and ModeGateBehavior (activity), part 1 of 2" "Activity diagram converted from fence F3 (line 330)." {
    include strRequestPipeline_CommandOrQueryDispatchedThroughIDispatcher strRequestPipeline_WorkspaceResolutionBehavior strRequestPipeline_400ValidationErrorDetailsFieldWorkspacePath strRequestPipeline_RecordImplementsIModeGatedRequest
    autolayout tb 100 100
}
custom "RequestPipeline-part2" "RequestPipeline: WorkspaceResolutionBehavior and ModeGateBehavior (activity), part 2 of 2" "Activity diagram converted from fence F3 (line 330)." {
    include strRequestPipeline_RecordImplementsIModeGatedRequest strRequestPipeline_HandlerRuns strRequestPipeline_McpAgentContextModeIsOff strRequestPipeline_409AgentContextDisabled
    autolayout tb 100 100
}
custom "SerializableWrite" "SerializableWrite (activity), part 1 of 3" "Activity diagram converted from fence F4 (line 346)." {
    include strSerializableWrite_SerializableWriteDelegateIdsToLock strSerializableWrite_BeginSerializableTransactionBumpStatusVersionOf strSerializableWrite_RunChecksAndSave
    autolayout tb 100 200
}
custom "SerializableWrite-part2" "SerializableWrite (activity), part 2 of 3" "Activity diagram converted from fence F4 (line 346)." {
    include strSerializableWrite_RunChecksAndSave strSerializableWrite_RolledBackRefusalReturned strSerializableWrite_Commit strSerializableWrite_RollBack strSerializableWrite_ResultReturned
    autolayout tb 100 100
}
custom "SerializableWrite-part3" "SerializableWrite (activity), part 3 of 3" "Activity diagram converted from fence F4 (line 346)." {
    include strSerializableWrite_RollBack strSerializableWrite_UnderTheRetryBoundThreeUnless strSerializableWrite_BeginSerializableTransactionBumpStatusVersionOf strSerializableWrite_409ConflictRetryableOrTheCaller
    autolayout tb 100 100
}
custom "IdempotentCreate" "IdempotentCreate (activity), part 1 of 5" "Activity diagram converted from fence F5 (line 362)." {
    include strIdempotentCreate_IdempotentCreateKeyPayloadFingerprintOfTheClient strIdempotentCreate_RecordWithThisKeyExists
    autolayout lr 250 150
}
custom "IdempotentCreate-part2" "IdempotentCreate (activity), part 2 of 5" "Activity diagram converted from fence F5 (line 362)." {
    include strIdempotentCreate_RecordWithThisKeyExists strIdempotentCreate_StoredFingerprintEqualsTheNewOne strIdempotentCreate_ReviveClearTheDeletionColumns strIdempotentCreate_CallerSRemainingChecksThenInsert
    autolayout tb 100 100
}
custom "IdempotentCreate-part3" "IdempotentCreate (activity), part 3 of 5" "Activity diagram converted from fence F5 (line 362)." {
    include strIdempotentCreate_StoredFingerprintEqualsTheNewOne strIdempotentCreate_StoredRecordReturnedCreatedFalse strIdempotentCreate_MismatchPolicy strIdempotentCreate_ReviveClearTheDeletionColumns strIdempotentCreate_CallerSRemainingChecksThenInsert
    autolayout tb 100 100
}
custom "IdempotentCreate-part4" "IdempotentCreate (activity), part 4 of 5" "Activity diagram converted from fence F5 (line 362)." {
    include strIdempotentCreate_CallerSRemainingChecksThenInsert strIdempotentCreate_NewRecordCreatedTrue strIdempotentCreate_AttemptEndsSerializableWriteRetriesAndThe
    autolayout lr 250 150
}
custom "IdempotentCreate-part5" "IdempotentCreate (activity), part 5 of 5" "Activity diagram converted from fence F5 (line 362)." {
    include strIdempotentCreate_MismatchPolicy strIdempotentCreate_409ConflictRetryableFalseKeyReused strIdempotentCreate_StoredRecordReturnedCreatedFalse strIdempotentCreate_StoredRecordReturnedForTheCaller
    autolayout lr 250 150
}
custom "PagedQuery" "PagedQuery (activity), part 1 of 2" "Activity diagram converted from fence F6 (line 381)." {
    include strPagedQuery_PagedQueryLimitOffsetPageBoundsStableOrder strPagedQuery_PageBoundsPolicy strPagedQuery_DefaultTheLimitWhenAbsentClamp strPagedQuery_LimitAndOffsetWithinTheBounds
    autolayout tb 100 100
}
custom "PagedQuery-part2" "PagedQuery (activity), part 2 of 2" "Activity diagram converted from fence F6 (line 381)." {
    include strPagedQuery_DefaultTheLimitWhenAbsentClamp strPagedQuery_CountTheMatchesReadOnePage strPagedQuery_LimitAndOffsetWithinTheBounds strPagedQuery_400ValidationErrorDetailsFieldLimit strPagedQuery_PageResultTotalCountLimitOffsetItems
    autolayout tb 100 100
}
custom "TurnLocator" "TurnLocator (activity), part 1 of 3" "Activity diagram converted from fence F7 (line 401)." {
    include strTurnLocator_TurnLocatorAgentSessionIdRequestId strTurnLocator_IdsValid strTurnLocator_InvalidIdentifier strTurnLocator_SessionFound
    autolayout tb 100 100
}
custom "TurnLocator-part2" "TurnLocator (activity), part 2 of 3" "Activity diagram converted from fence F7 (line 401)." {
    include strTurnLocator_SessionFound strTurnLocator_SessionNotFound strTurnLocator_TurnAddressed strTurnLocator_Session strTurnLocator_TurnFound
    autolayout tb 100 100
}
custom "TurnLocator-part3" "TurnLocator (activity), part 3 of 3" "Activity diagram converted from fence F7 (line 401)." {
    include strTurnLocator_TurnFound strTurnLocator_TurnNotFound strTurnLocator_SessionAndTurn
    autolayout lr 250 150
}
custom "TurnWritePipeline" "TurnWritePipeline (activity), part 1 of 6" "Activity diagram converted from fence F8 (line 416)." {
    include strTurnWritePipeline_TurnWritePipelineModeEntryOrigin strTurnWritePipeline_TurnLocator strTurnWritePipeline_TypedFailure strTurnWritePipeline_PolicyTerminalTurnOutcomeGuardSubmitModeOnly
    autolayout tb 100 100
}
custom "TurnWritePipeline-part2" "TurnWritePipeline (activity), part 2 of 6" "Activity diagram converted from fence F8 (line 416)." {
    include strTurnWritePipeline_PolicyTerminalTurnOutcomeGuardSubmitModeOnly strTurnWritePipeline_OutcomeOnlyNoWrite strTurnWritePipeline_ContextValidAndAttributed strTurnWritePipeline_InvalidPayload strTurnWritePipeline_TurnExists
    autolayout tb 100 100
}
custom "TurnWritePipeline-part3" "TurnWritePipeline (activity), part 3 of 6" "Activity diagram converted from fence F8 (line 416)." {
    include strTurnWritePipeline_TurnExists strTurnWritePipeline_TurnNotFound strTurnWritePipeline_IdempotentCreateMergeIntoInsertWithReviewDefaults strTurnWritePipeline_MergeOrReplaceCompleteAndFail
    autolayout tb 100 100
}
custom "TurnWritePipeline-part4" "TurnWritePipeline (activity), part 4 of 6" "Activity diagram converted from fence F8 (line 416)." {
    include strTurnWritePipeline_IdempotentCreateMergeIntoInsertWithReviewDefaults strTurnWritePipeline_MergeOrReplaceCompleteAndFail strTurnWritePipeline_PolicyReviewValidationWhenTheBodyCarries
    autolayout tb 100 200
}
custom "TurnWritePipeline-part5" "TurnWritePipeline (activity), part 5 of 6" "Activity diagram converted from fence F8 (line 416)." {
    include strTurnWritePipeline_PolicyReviewValidationWhenTheBodyCarries strTurnWritePipeline_ReviewInvalid strTurnWritePipeline_PolicyRequestClassificationWhenRequestClassIsSupplied strTurnWritePipeline_RequestClassInvalid strTurnWritePipeline_PolicyCompletionGateWhenTheWriteMoves
    autolayout tb 100 100
}
custom "TurnWritePipeline-part6" "TurnWritePipeline (activity), part 6 of 6" "Activity diagram converted from fence F8 (line 416)." {
    include strTurnWritePipeline_PolicyCompletionGateWhenTheWriteMoves strTurnWritePipeline_CompletionGateMissing strTurnWritePipeline_SingleTurnQBAgentCloseWithoutEvidence strTurnWritePipeline_InvalidPayload2 strTurnWritePipeline_ChildrenStampedSummaryRefreshedTurnId
    autolayout tb 100 100
}
custom "ResponseStatusTransition" "ResponseStatusTransition (activity), part 1 of 3" "Activity diagram converted from fence F9 (line 455)." {
    include strResponseStatusTransition_ResponseStatusTransitionTargetReasonReplacement strResponseStatusTransition_AlreadyApplied strResponseStatusTransition_Unchanged200 strResponseStatusTransition_LifecycleAllowsIt
    autolayout tb 100 100
}
custom "ResponseStatusTransition-part2" "ResponseStatusTransition (activity), part 2 of 3" "Activity diagram converted from fence F9 (line 455)." {
    include strResponseStatusTransition_LifecycleAllowsIt strResponseStatusTransition_409AgentContextTransitionInvalid strResponseStatusTransition_SetStatusAndTimestamps strResponseStatusTransition_MarkTheProjectionRow
    autolayout tb 100 100
}
custom "ResponseStatusTransition-part3" "ResponseStatusTransition (activity), part 3 of 3" "Activity diagram converted from fence F9 (line 455)." {
    include strResponseStatusTransition_MarkTheProjectionRow strResponseStatusTransition_RejectedOrSuperseded strResponseStatusTransition_ClosePendingHVItemsCompleteEmptied strResponseStatusTransition_SavedWithTheCaller
    autolayout tb 100 200
}
custom "IResponseStalenessRule" "IResponseStalenessRule.Evaluate (activity), part 1 of 3" "Activity diagram converted from fence F10 (line 471)." {
    include strIResponseStalenessRule_IResponseStalenessRuleEvaluateOneResponse strIResponseStalenessRule_StatusSubmittedGradingOrTrusted strIResponseStalenessRule_NotEvaluated strIResponseStalenessRule_CollectDependsOnDerivedFromAnd
    autolayout tb 100 100
}
custom "IResponseStalenessRule-part2" "IResponseStalenessRule.Evaluate (activity), part 2 of 3" "Activity diagram converted from fence F10 (line 471)." {
    include strIResponseStalenessRule_CollectDependsOnDerivedFromAnd strIResponseStalenessRule_EveryDependencyHashesAsRecordedFiles strIResponseStalenessRule_EveryResponseDependencyEffectivelyTrustedAnd strIResponseStalenessRule_StaleTheFirstChangedReferenceAnd
    autolayout tb 100 200
}
custom "IResponseStalenessRule-part3" "IResponseStalenessRule.Evaluate (activity), part 3 of 3" "Activity diagram converted from fence F10 (line 471)." {
    include strIResponseStalenessRule_EveryResponseDependencyEffectivelyTrustedAnd strIResponseStalenessRule_Fresh strIResponseStalenessRule_StaleTheFirstChangedReferenceAnd
    autolayout lr 250 150
}
custom "RelationshipWriter" "RelationshipWriter (activity), part 1 of 2" "Activity diagram converted from fence F11 (line 486)." {
    include strRelationshipWriter_RelationshipWriterInTheCallerSTransaction strRelationshipWriter_TypeSupersedesAndTheCallerIs strRelationshipWriter_400ValidationErrorDetailsFieldType strRelationshipWriter_ASupersedesLinkThatWouldClose
    autolayout tb 100 100
}
custom "RelationshipWriter-part2" "RelationshipWriter (activity), part 2 of 2" "Activity diagram converted from fence F11 (line 486)." {
    include strRelationshipWriter_ASupersedesLinkThatWouldClose strRelationshipWriter_409AgentContextSupersedeInvalid strRelationshipWriter_IdempotentCreateKeyedByTheLinkS strRelationshipWriter_CreatedFalse strRelationshipWriter_CreatedTrue
    autolayout tb 100 100
}
custom "Resolve-MintOutcome" "Resolve-MintOutcome (activity), part 1 of 4" "Activity diagram converted from fence F12 (line 503)." {
    include strResolveMintOutcome_ResolveMintOutcomeMintResultNewInvocation strResolveMintOutcome_Result
    autolayout lr 250 150
}
custom "Resolve-MintOutcome-part2" "Resolve-MintOutcome (activity), part 2 of 4" "Activity diagram converted from fence F12 (line 503)." {
    include strResolveMintOutcome_Result strResolveMintOutcome_Created strResolveMintOutcome_NewInvocationOnCopilotOrClassic strResolveMintOutcome_MergedTerminalReportStoredStatusNeverThe strResolveMintOutcome_OutagePendingIdPendingNonceFailsafe
    autolayout lr 200 50
}
custom "Resolve-MintOutcome-part3" "Resolve-MintOutcome (activity), part 3 of 4" "Activity diagram converted from fence F12 (line 503)." {
    include strResolveMintOutcome_Result strResolveMintOutcome_FinalTurnOpenFailedWithThe strResolveMintOutcome_SessionOpenConfirmedForThisEnvelope
    autolayout tb 100 200
}
custom "Resolve-MintOutcome-part4" "Resolve-MintOutcome (activity), part 4 of 4" "Activity diagram converted from fence F12 (line 503)." {
    include strResolveMintOutcome_NewInvocationOnCopilotOrClassic strResolveMintOutcome_UnsupportedIndistinguishableArrivalBySetSessionUnsupported strResolveMintOutcome_MergedInProgressSameRequestIdTurn strResolveMintOutcome_SessionOpenConfirmedForThisEnvelope strResolveMintOutcome_FinalTurnOpenFailedWithThe strResolveMintOutcome_OutagePendingIdPendingNonceFailsafe
    autolayout lr 200 50
}
custom "Invoke-SessionOpen" "Invoke-SessionOpen (activity), part 1 of 2" "Activity diagram converted from fence F13 (line 520)." {
    include strInvokeSessionOpen_InvokeSessionOpenEnvelopeMcpSessionId strInvokeSessionOpen_OpenResult
    autolayout lr 250 150
}
custom "Invoke-SessionOpen-part2" "Invoke-SessionOpen (activity), part 2 of 2" "Activity diagram converted from fence F13 (line 520)." {
    include strInvokeSessionOpen_OpenResult strInvokeSessionOpen_ConfirmedTheIdItsCallerRecorded strInvokeSessionOpen_OutageRecordsKeptForTheNext strInvokeSessionOpen_FinalInvalidSessionIdentifier
    autolayout lr 250 150
}
custom "Set-SessionUnsupported" "Set-SessionUnsupported (activity), part 1 of 2" "Activity diagram converted from fence F14 (line 530)." {
    include strSetSessionUnsupported_SetSessionUnsupportedReasonCauseOffsetOrCauseNonce strSetSessionUnsupported_ARequestAlreadyDeliveredInThis strSetSessionUnsupported_TurnLoggingUnsupportedReportedNothingRecorded strSetSessionUnsupported_UpdateTurnStateRecordUnsupportedSinceWithThe
    autolayout tb 100 100
}
custom "Set-SessionUnsupported-part2" "Set-SessionUnsupported (activity), part 2 of 2" "Activity diagram converted from fence F14 (line 530)." {
    include strSetSessionUnsupported_UpdateTurnStateRecordUnsupportedSinceWithThe strSetSessionUnsupported_HostSessionFailedClosedOpenTurns
    autolayout lr 250 150
}
custom "Update-TurnState" "Update-TurnState (activity), part 1 of 3" "Activity diagram converted from fence F15 (line 540)." {
    include strUpdateTurnState_UpdateTurnStateChangeToApply strUpdateTurnState_TurnOpenLockAcquired strUpdateTurnState_ContendedReturnedToTheCallerThe strUpdateTurnState_ReadTurnStateYamlAndApply
    autolayout tb 100 100
}
custom "Update-TurnState-part2" "Update-TurnState (activity), part 2 of 3" "Activity diagram converted from fence F15 (line 540)." {
    include strUpdateTurnState_ReadTurnStateYamlAndApply strUpdateTurnState_TempFileWrittenFlushedAndRenamed strUpdateTurnState_ReleaseTurnOpenLock strUpdateTurnState_ReleaseTurnOpenLock2
    autolayout tb 100 100
}
custom "Update-TurnState-part3" "Update-TurnState (activity), part 3 of 3" "Activity diagram converted from fence F15 (line 540)." {
    include strUpdateTurnState_ReleaseTurnOpenLock strUpdateTurnState_StateReplaced strUpdateTurnState_ReleaseTurnOpenLock2 strUpdateTurnState_ErrorReturnedFileUnchanged
    autolayout lr 250 150
}
custom "Add-McpAlias" "Add-McpAlias (activity), part 1 of 3" "Activity diagram converted from fence F16 (line 562)." {
    include strAddMcpAlias_AddMcpAliasPendingIdAndRequestIdFrom strAddMcpAlias_TurnOpenLockHeldOrAcquired strAddMcpAlias_ContendedReturnedToTheCallerWhich strAddMcpAlias_LookupEntryForPendingId
    autolayout tb 100 100
}
custom "Add-McpAlias-part2" "Add-McpAlias (activity), part 2 of 3" "Activity diagram converted from fence F16 (line 562)." {
    include strAddMcpAlias_LookupEntryForPendingId strAddMcpAlias_StoredEntryReturnedNothingWritten strAddMcpAlias_RequestAmbiguousFinalNothingWritten strAddMcpAlias_WriteTheLookupEntryAndThe
    autolayout lr 250 150
}
custom "Add-McpAlias-part3" "Add-McpAlias (activity), part 3 of 3" "Activity diagram converted from fence F16 (line 562)." {
    include strAddMcpAlias_WriteTheLookupEntryAndThe strAddMcpAlias_AliasCommittedTheCallerRewritesIts
    autolayout lr 250 150
}
custom "STR-04-01" "Request lifecycle in the plugin turn store (state diagram), part 1 of 6" "State diagram converted from fence F17 (line 597)." {
    include strSTR0401_Start strSTR0401_Queued strSTR0401_OpenFailed
    autolayout lr 300 300
}
custom "STR-04-01-part2" "Request lifecycle in the plugin turn store (state diagram), part 2 of 6" "State diagram converted from fence F17 (line 597)." {
    include strSTR0401_Start strSTR0401_TerminalAtMint strSTR0401_End
    autolayout tb 200 300
}
custom "STR-04-01-part3" "Request lifecycle in the plugin turn store (state diagram), part 3 of 6" "State diagram converted from fence F17 (line 597)." {
    include strSTR0401_OpenFailed strSTR0401_End strSTR0401_Queued
    autolayout lr 300 300
}
custom "STR-04-01-part4" "Request lifecycle in the plugin turn store (state diagram), part 4 of 6" "State diagram converted from fence F17 (line 597)." {
    include strSTR0401_Queued strSTR0401_Delivered strSTR0401_Withdrawn
    autolayout lr 300 300
}
custom "STR-04-01-part5" "Request lifecycle in the plugin turn store (state diagram), part 5 of 6" "State diagram converted from fence F17 (line 597)." {
    include strSTR0401_Queued strSTR0401_Closed strSTR0401_Delivered
    autolayout tb 200 300
}
custom "STR-04-01-part6" "Request lifecycle in the plugin turn store (state diagram), part 6 of 6" "State diagram converted from fence F17 (line 597)." {
    include strSTR0401_Withdrawn strSTR0401_Closed strSTR0401_End
    autolayout tb 200 300
}
custom "MintSessionTurnCommand" "MintSessionTurnCommand: mint a turn at ingress (activity), part 1 of 4" "Activity diagram converted from fence F18 (line 640)." {
    include strMintSessionTurnCommand_MintRequestReceived strMintSessionTurnCommand_IngressNoncePresentAndValid strMintSessionTurnCommand_400IngressNonceInvalid strMintSessionTurnCommand_PlanFileAndTodoIdValid
    autolayout tb 100 100
}
custom "MintSessionTurnCommand-part2" "MintSessionTurnCommand: mint a turn at ingress (activity), part 2 of 4" "Activity diagram converted from fence F18 (line 640)." {
    include strMintSessionTurnCommand_PlanFileAndTodoIdValid strMintSessionTurnCommand_400ValidationError strMintSessionTurnCommand_TurnLocatorAgentAndSessionIdNoTurn
    autolayout tb 100 200
}
custom "MintSessionTurnCommand-part3" "MintSessionTurnCommand: mint a turn at ingress (activity), part 3 of 4" "Activity diagram converted from fence F18 (line 640)." {
    include strMintSessionTurnCommand_TurnLocatorAgentAndSessionIdNoTurn strMintSessionTurnCommand_400ValidationError strMintSessionTurnCommand_404SessionNotOpen strMintSessionTurnCommand_IdempotentCreateInsideSerializableWriteKeySessionAnd
    autolayout lr 250 150
}
custom "MintSessionTurnCommand-part4" "MintSessionTurnCommand: mint a turn at ingress (activity), part 4 of 4" "Activity diagram converted from fence F18 (line 640)." {
    include strMintSessionTurnCommand_IdempotentCreateInsideSerializableWriteKeySessionAnd strMintSessionTurnCommand_200MergedExistingRequestIdAndStoredStatus strMintSessionTurnCommand_201CreatedRequestId strMintSessionTurnCommand_500MintCollisionExhausted strMintSessionTurnCommand_409ConflictRetryableTrueNothingSaved
    autolayout lr 200 50
}
custom "InsertMintedTurnWithRetry" "Nested activity: InsertMintedTurnWithRetry (insert step of MintSessionTurnCommand, rerun by SerializableWrite) (activity), part 1 of 3" "Activity diagram converted from fence F19 (line 658)." {
    include strInsertMintedTurnWithRetry_InsertRequestedInsideTheSerializableWriteDelegate strInsertMintedTurnWithRetry_ReadableKeyAllocatorStrategyRandomHexTheReqUTC strInsertMintedTurnWithRetry_InsertTheInProgressTurnWith strInsertMintedTurnWithRetry_DbErrorClassifierIsUniqueViolationWhichIndex
    autolayout tb 100 200
}
custom "InsertMintedTurnWithRetry-part2" "Nested activity: InsertMintedTurnWithRetry (insert step of MintSessionTurnCommand, rerun by SerializableWrite) (activity), part 2 of 3" "Activity diagram converted from fence F19 (line 658)." {
    include strInsertMintedTurnWithRetry_DbErrorClassifierIsUniqueViolationWhichIndex strInsertMintedTurnWithRetry_ReturnInserted strInsertMintedTurnWithRetry_ReturnLostRaceIdempotentCreateEndsThe strInsertMintedTurnWithRetry_FewerThanSixAttemptsUsed
    autolayout lr 250 150
}
custom "InsertMintedTurnWithRetry-part3" "Nested activity: InsertMintedTurnWithRetry (insert step of MintSessionTurnCommand, rerun by SerializableWrite) (activity), part 3 of 3" "Activity diagram converted from fence F19 (line 658)." {
    include strInsertMintedTurnWithRetry_FewerThanSixAttemptsUsed strInsertMintedTurnWithRetry_ReturnDeclaredKeyCollisionSerializableWriteReruns strInsertMintedTurnWithRetry_ReturnSixRequestIdAttemptsUsed
    autolayout lr 250 150
}
custom "SubmitSessionLogCommand" "SubmitSessionLogCommand: submit a session log with per-turn outcomes (activity), part 1 of 5" "Activity diagram converted from fence F20 (line 679)." {
    include strSubmitSessionLogCommand_SubmitReceived strSubmitSessionLogCommand_DocumentParsesEverySuppliedStatusWritable strSubmitSessionLogCommand_400ValidationError strSubmitSessionLogCommand_CollapseDuplicateRequestIdsLastWins
    autolayout tb 100 100
}
custom "SubmitSessionLogCommand-part2" "SubmitSessionLogCommand: submit a session log with per-turn outcomes (activity), part 2 of 5" "Activity diagram converted from fence F20 (line 679)." {
    include strSubmitSessionLogCommand_CollapseDuplicateRequestIdsLastWins strSubmitSessionLogCommand_SerializableWriteOneDelegateFromTheSession strSubmitSessionLogCommand_IdempotentCreateOnTheSessionKeyPolicy strSubmitSessionLogCommand_409ConflictRetryableTrueNothingSaved
    autolayout tb 100 100
}
custom "SubmitSessionLogCommand-part3" "SubmitSessionLogCommand: submit a session log with per-turn outcomes (activity), part 3 of 5" "Activity diagram converted from fence F20 (line 679)." {
    include strSubmitSessionLogCommand_IdempotentCreateOnTheSessionKeyPolicy strSubmitSessionLogCommand_ForEachTurnOfTheSubmission
    autolayout lr 250 150
}
custom "SubmitSessionLogCommand-part4" "SubmitSessionLogCommand: submit a session log with per-turn outcomes (activity), part 4 of 5" "Activity diagram converted from fence F20 (line 679)." {
    include strSubmitSessionLogCommand_ForEachTurnOfTheSubmission strSubmitSessionLogCommand_400ValidationError strSubmitSessionLogCommand_400RequestClassInvalidNothingSaved strSubmitSessionLogCommand_400ReviewCodeOr422Review strSubmitSessionLogCommand_AnyGateFailureRecorded
    autolayout lr 200 50
}
custom "SubmitSessionLogCommand-part5" "SubmitSessionLogCommand: submit a session log with per-turn outcomes (activity), part 5 of 5" "Activity diagram converted from fence F20 (line 679)." {
    include strSubmitSessionLogCommand_AnyGateFailureRecorded strSubmitSessionLogCommand_400ValidationErrorSessionTurnCompletion strSubmitSessionLogCommand_SaveSerializableWriteCommitsTheDelegate strSubmitSessionLogCommand_201WithTurnsOutcomeList
    autolayout tb 100 100
}
custom "EvaluateSubmittedTurn" "Nested activity: EvaluateSubmittedTurn (loop body of SubmitSessionLogCommand) (activity), part 1 of 4" "Activity diagram converted from fence F21 (line 699)." {
    include strEvaluateSubmittedTurn_NextTurnOfTheSubmission strEvaluateSubmittedTurn_TurnWritePipelineModeSubmitTerminalTurnOutcomeGuardTurnContext
    autolayout lr 250 150
}
custom "EvaluateSubmittedTurn-part2" "Nested activity: EvaluateSubmittedTurn (loop body of SubmitSessionLogCommand) (activity), part 2 of 4" "Activity diagram converted from fence F21 (line 699)." {
    include strEvaluateSubmittedTurn_TurnWritePipelineModeSubmitTerminalTurnOutcomeGuardTurnContext strEvaluateSubmittedTurn_ReturnAnInvalidPayloadTheSubmission strEvaluateSubmittedTurn_ReturnAClassInvalidTheSubmission strEvaluateSubmittedTurn_ReturnAReviewRefusalTheSubmission strEvaluateSubmittedTurn_RecordThisRequestIdAndItsMissing
    autolayout lr 200 50
}
custom "EvaluateSubmittedTurn-part3" "Nested activity: EvaluateSubmittedTurn (loop body of SubmitSessionLogCommand) (activity), part 3 of 4" "Activity diagram converted from fence F21 (line 699)." {
    include strEvaluateSubmittedTurn_TurnWritePipelineModeSubmitTerminalTurnOutcomeGuardTurnContext strEvaluateSubmittedTurn_TurnEvaluatedContinueWithTheNext
    autolayout lr 250 150
}
custom "EvaluateSubmittedTurn-part4" "Nested activity: EvaluateSubmittedTurn (loop body of SubmitSessionLogCommand) (activity), part 4 of 4" "Activity diagram converted from fence F21 (line 699)." {
    include strEvaluateSubmittedTurn_RecordThisRequestIdAndItsMissing strEvaluateSubmittedTurn_TurnEvaluatedContinueWithTheNext
    autolayout lr 250 150
}
custom "UpsertSessionTurnCommand" "UpsertSessionTurnCommand: append or merge one turn (activity), part 1 of 5" "Activity diagram converted from fence F22 (line 727)." {
    include strUpsertSessionTurnCommand_UpsertSessionTurnCommandDispatched strUpsertSessionTurnCommand_BodyPresent strUpsertSessionTurnCommand_400ProblemInvalidTurnBody strUpsertSessionTurnCommand_StatusOmittedOrWritableTurnStatus
    autolayout tb 100 100
}
custom "UpsertSessionTurnCommand-part2" "UpsertSessionTurnCommand: append or merge one turn (activity), part 2 of 5" "Activity diagram converted from fence F22 (line 727)." {
    include strUpsertSessionTurnCommand_StatusOmittedOrWritableTurnStatus strUpsertSessionTurnCommand_400ValidationErrorFieldStatus strUpsertSessionTurnCommand_TurnWritePipelineModeMergeInSerializableWrite
    autolayout lr 250 150
}
custom "UpsertSessionTurnCommand-part3" "UpsertSessionTurnCommand: append or merge one turn (activity), part 3 of 5" "Activity diagram converted from fence F22 (line 727)." {
    include strUpsertSessionTurnCommand_TurnWritePipelineModeMergeInSerializableWrite strUpsertSessionTurnCommand_201TurnIdAgentSessionIdRequestId strUpsertSessionTurnCommand_409ConflictRetryableTrue strUpsertSessionTurnCommand_Failure
    autolayout lr 250 150
}
custom "UpsertSessionTurnCommand-part4" "UpsertSessionTurnCommand: append or merge one turn (activity), part 4 of 5" "Activity diagram converted from fence F22 (line 727)." {
    include strUpsertSessionTurnCommand_Failure strUpsertSessionTurnCommand_400ProblemInvalidTurnPayload strUpsertSessionTurnCommand_404NotFound strUpsertSessionTurnCommand_PolicyRefusal
    autolayout lr 250 150
}
custom "UpsertSessionTurnCommand-part5" "UpsertSessionTurnCommand: append or merge one turn (activity), part 5 of 5" "Activity diagram converted from fence F22 (line 727)." {
    include strUpsertSessionTurnCommand_PolicyRefusal strUpsertSessionTurnCommand_400RequestClassInvalid strUpsertSessionTurnCommand_400ReviewCodeOr422Review strUpsertSessionTurnCommand_400CompletionGateMissingItems
    autolayout lr 250 150
}
custom "PatchSessionTurnCommand" "PatchSessionTurnCommand: change named fields of one turn (activity), part 1 of 5" "Activity diagram converted from fence F23 (line 752)." {
    include strPatchSessionTurnCommand_PatchSessionTurnCommandDispatched strPatchSessionTurnCommand_TurnBodyOrEmptyRequestIdFrom strPatchSessionTurnCommand_StatusOmittedOrWritableTurnStatus
    autolayout tb 100 200
}
custom "PatchSessionTurnCommand-part2" "PatchSessionTurnCommand: change named fields of one turn (activity), part 2 of 5" "Activity diagram converted from fence F23 (line 752)." {
    include strPatchSessionTurnCommand_StatusOmittedOrWritableTurnStatus strPatchSessionTurnCommand_400ValidationErrorFieldStatus strPatchSessionTurnCommand_TurnWritePipelineModeMergeInSerializableWrite
    autolayout lr 250 150
}
custom "PatchSessionTurnCommand-part3" "PatchSessionTurnCommand: change named fields of one turn (activity), part 3 of 5" "Activity diagram converted from fence F23 (line 752)." {
    include strPatchSessionTurnCommand_TurnWritePipelineModeMergeInSerializableWrite strPatchSessionTurnCommand_200TurnIdAgentSessionIdRequestIdStatus strPatchSessionTurnCommand_409ConflictRetryableTrue strPatchSessionTurnCommand_Failure
    autolayout lr 250 150
}
custom "PatchSessionTurnCommand-part4" "PatchSessionTurnCommand: change named fields of one turn (activity), part 4 of 5" "Activity diagram converted from fence F23 (line 752)." {
    include strPatchSessionTurnCommand_Failure strPatchSessionTurnCommand_400ProblemInvalidTurnPayload strPatchSessionTurnCommand_404NotFound strPatchSessionTurnCommand_PolicyRefusal
    autolayout lr 250 150
}
custom "PatchSessionTurnCommand-part5" "PatchSessionTurnCommand: change named fields of one turn (activity), part 5 of 5" "Activity diagram converted from fence F23 (line 752)." {
    include strPatchSessionTurnCommand_PolicyRefusal strPatchSessionTurnCommand_400RequestClassInvalid strPatchSessionTurnCommand_400ReviewCodeOr422Review strPatchSessionTurnCommand_400CompletionGateMissingItems
    autolayout lr 250 150
}
custom "ReplaceSessionTurnCommand" "ReplaceSessionTurnCommand: replace one turn (activity), part 1 of 5" "Activity diagram converted from fence F24 (line 777)." {
    include strReplaceSessionTurnCommand_ReplaceSessionTurnCommandDispatched strReplaceSessionTurnCommand_PrepareReplacementTurn strReplaceSessionTurnCommand_StatusOmittedOrWritableTurnStatus
    autolayout tb 100 200
}
custom "ReplaceSessionTurnCommand-part2" "ReplaceSessionTurnCommand: replace one turn (activity), part 2 of 5" "Activity diagram converted from fence F24 (line 777)." {
    include strReplaceSessionTurnCommand_StatusOmittedOrWritableTurnStatus strReplaceSessionTurnCommand_400ValidationErrorFieldStatus strReplaceSessionTurnCommand_TurnWritePipelineModeReplace
    autolayout lr 250 150
}
custom "ReplaceSessionTurnCommand-part3" "ReplaceSessionTurnCommand: replace one turn (activity), part 3 of 5" "Activity diagram converted from fence F24 (line 777)." {
    include strReplaceSessionTurnCommand_TurnWritePipelineModeReplace strReplaceSessionTurnCommand_200TurnIdAgentSessionIdRequestIdReplaced strReplaceSessionTurnCommand_409ConflictRetryableTrue strReplaceSessionTurnCommand_Failure
    autolayout lr 250 150
}
custom "ReplaceSessionTurnCommand-part4" "ReplaceSessionTurnCommand: replace one turn (activity), part 4 of 5" "Activity diagram converted from fence F24 (line 777)." {
    include strReplaceSessionTurnCommand_Failure strReplaceSessionTurnCommand_400ValidationError strReplaceSessionTurnCommand_404NotFoundSessionOrTurn strReplaceSessionTurnCommand_PolicyRefusal
    autolayout lr 250 150
}
custom "ReplaceSessionTurnCommand-part5" "ReplaceSessionTurnCommand: replace one turn (activity), part 5 of 5" "Activity diagram converted from fence F24 (line 777)." {
    include strReplaceSessionTurnCommand_PolicyRefusal strReplaceSessionTurnCommand_400RequestClassInvalid strReplaceSessionTurnCommand_400ReviewCodeOr422Review strReplaceSessionTurnCommand_400CompletionGateMissingItems
    autolayout lr 250 150
}
custom "PrepareReplacementTurn" "Nested activity: PrepareReplacementTurn (body step of ReplaceSessionTurnCommand, before TurnWritePipeline) (activity)" "Activity diagram converted from fence F25 (line 796)." {
    include strPrepareReplacementTurn_ReplacementBodyReceived strPrepareReplacementTurn_TurnBodyOrEmptyRequestIdFrom strPrepareReplacementTurn_SupersededHookPersistNoPlanFileOr strPrepareReplacementTurn_UseTheNoneSentinels strPrepareReplacementTurn_ReturnThePreparedTurn
    autolayout tb 100 200
}
custom "CompleteSessionTurnCommand" "CompleteSessionTurnCommand: complete one turn (activity), part 1 of 4" "Activity diagram converted from fence F26 (line 812)." {
    include strCompleteSessionTurnCommand_CompleteSessionTurnCommandDispatched strCompleteSessionTurnCommand_TurnBodyOrEmptyRouteRequestId strCompleteSessionTurnCommand_TurnWritePipelineModeCompleteInSerializableWrite
    autolayout tb 100 200
}
custom "CompleteSessionTurnCommand-part2" "CompleteSessionTurnCommand: complete one turn (activity), part 2 of 4" "Activity diagram converted from fence F26 (line 812)." {
    include strCompleteSessionTurnCommand_TurnWritePipelineModeCompleteInSerializableWrite strCompleteSessionTurnCommand_200TurnIdAgentSessionIdRequestIdCompleted strCompleteSessionTurnCommand_409ConflictRetryableTrue strCompleteSessionTurnCommand_Failure
    autolayout lr 250 150
}
custom "CompleteSessionTurnCommand-part3" "CompleteSessionTurnCommand: complete one turn (activity), part 3 of 4" "Activity diagram converted from fence F26 (line 812)." {
    include strCompleteSessionTurnCommand_Failure strCompleteSessionTurnCommand_400ProblemInvalidTurnPayload strCompleteSessionTurnCommand_404NotFound strCompleteSessionTurnCommand_PolicyRefusal
    autolayout lr 250 150
}
custom "CompleteSessionTurnCommand-part4" "CompleteSessionTurnCommand: complete one turn (activity), part 4 of 4" "Activity diagram converted from fence F26 (line 812)." {
    include strCompleteSessionTurnCommand_PolicyRefusal strCompleteSessionTurnCommand_400RequestClassInvalid strCompleteSessionTurnCommand_400ReviewCodeOr422Review strCompleteSessionTurnCommand_400CompletionGateMissingItemsIn
    autolayout lr 250 150
}
custom "FailSessionTurnCommand" "FailSessionTurnCommand: fail one turn (activity), part 1 of 4" "Activity diagram converted from fence F27 (line 834)." {
    include strFailSessionTurnCommand_FailSessionTurnCommandDispatched strFailSessionTurnCommand_TurnBodyOrEmptyRouteRequestId strFailSessionTurnCommand_TurnWritePipelineModeFailInSerializableWrite
    autolayout tb 100 200
}
custom "FailSessionTurnCommand-part2" "FailSessionTurnCommand: fail one turn (activity), part 2 of 4" "Activity diagram converted from fence F27 (line 834)." {
    include strFailSessionTurnCommand_TurnWritePipelineModeFailInSerializableWrite strFailSessionTurnCommand_200TurnIdAgentSessionIdRequestIdFailed strFailSessionTurnCommand_409ConflictRetryableTrue strFailSessionTurnCommand_Failure
    autolayout lr 250 150
}
custom "FailSessionTurnCommand-part3" "FailSessionTurnCommand: fail one turn (activity), part 3 of 4" "Activity diagram converted from fence F27 (line 834)." {
    include strFailSessionTurnCommand_Failure strFailSessionTurnCommand_400ProblemInvalidTurnPayload strFailSessionTurnCommand_404NotFound strFailSessionTurnCommand_PolicyRefusal
    autolayout lr 250 150
}
custom "FailSessionTurnCommand-part4" "FailSessionTurnCommand: fail one turn (activity), part 4 of 4" "Activity diagram converted from fence F27 (line 834)." {
    include strFailSessionTurnCommand_PolicyRefusal strFailSessionTurnCommand_400RequestClassInvalid strFailSessionTurnCommand_400ReviewCodeOr422Review strFailSessionTurnCommand_400CompletionGateMissingRequestClass
    autolayout lr 250 150
}
custom "OpenSessionLogCommand" "OpenSessionLogCommand: open (ensure) a session (activity), part 1 of 3" "Activity diagram converted from fence F28 (line 856)." {
    include strOpenSessionLogCommand_OpenSessionLogCommandDispatched strOpenSessionLogCommand_TurnLocatorAgentAndSessionIdNoTurn
    autolayout lr 250 150
}
custom "OpenSessionLogCommand-part2" "OpenSessionLogCommand: open (ensure) a session (activity), part 2 of 3" "Activity diagram converted from fence F28 (line 856)." {
    include strOpenSessionLogCommand_TurnLocatorAgentAndSessionIdNoTurn strOpenSessionLogCommand_400ProblemInvalidSessionIdentifier strOpenSessionLogCommand_200CreatedFalseNothingChanged strOpenSessionLogCommand_CreateTheSessionInProgressWith
    autolayout lr 250 150
}
custom "OpenSessionLogCommand-part3" "OpenSessionLogCommand: open (ensure) a session (activity), part 3 of 3" "Activity diagram converted from fence F28 (line 856)." {
    include strOpenSessionLogCommand_CreateTheSessionInProgressWith strOpenSessionLogCommand_200CreatedTrue
    autolayout lr 250 150
}
custom "AppendTurnDialogCommand" "AppendTurnDialogCommand: append processing dialog to one turn (activity), part 1 of 3" "Activity diagram converted from fence F29 (line 872)." {
    include strAppendTurnDialogCommand_AppendTurnDialogCommandDispatched strAppendTurnDialogCommand_AtLeastOneItem strAppendTurnDialogCommand_REST400ErrorMCPErrorItems strAppendTurnDialogCommand_TurnLocatorAgentSessionIdRequestId
    autolayout tb 100 100
}
custom "AppendTurnDialogCommand-part2" "AppendTurnDialogCommand: append processing dialog to one turn (activity), part 2 of 3" "Activity diagram converted from fence F29 (line 872)." {
    include strAppendTurnDialogCommand_TurnLocatorAgentSessionIdRequestId strAppendTurnDialogCommand_REST400ErrorMCPTheClassified strAppendTurnDialogCommand_REST404NotFoundMCPThe strAppendTurnDialogCommand_AppendTheItemsAfterTheHighest
    autolayout lr 250 150
}
custom "AppendTurnDialogCommand-part3" "AppendTurnDialogCommand: append processing dialog to one turn (activity), part 3 of 3" "Activity diagram converted from fence F29 (line 872)." {
    include strAppendTurnDialogCommand_AppendTheItemsAfterTheHighest strAppendTurnDialogCommand_Save strAppendTurnDialogCommand_TotalDialogCountREST200TotalDialogCount
    autolayout tb 100 200
}
custom "SetTurnTitleCommand" "SetTurnTitleCommand: retitle one turn (activity), part 1 of 3" "Activity diagram converted from fence F30 (line 890)." {
    include strSetTurnTitleCommand_SetTurnTitleCommandDispatched strSetTurnTitleCommand_TurnLocatorAgentSessionIdRequestId
    autolayout lr 250 150
}
custom "SetTurnTitleCommand-part2" "SetTurnTitleCommand: retitle one turn (activity), part 2 of 3" "Activity diagram converted from fence F30 (line 890)." {
    include strSetTurnTitleCommand_TurnLocatorAgentSessionIdRequestId strSetTurnTitleCommand_400InvalidTurnIdentifier strSetTurnTitleCommand_404NotFound strSetTurnTitleCommand_OverwriteThisTurnSQueryTitleOnly
    autolayout lr 250 150
}
custom "SetTurnTitleCommand-part3" "SetTurnTitleCommand: retitle one turn (activity), part 3 of 3" "Activity diagram converted from fence F30 (line 890)." {
    include strSetTurnTitleCommand_OverwriteThisTurnSQueryTitleOnly strSetTurnTitleCommand_Save strSetTurnTitleCommand_200TurnIdRetitledTrue
    autolayout tb 100 200
}
custom "SetSessionTitleCommand" "SetSessionTitleCommand: retitle a session (activity), part 1 of 3" "Activity diagram converted from fence F31 (line 906)." {
    include strSetSessionTitleCommand_SetSessionTitleCommandDispatched strSetSessionTitleCommand_TurnLocatorAgentAndSessionIdNoTurn
    autolayout lr 250 150
}
custom "SetSessionTitleCommand-part2" "SetSessionTitleCommand: retitle a session (activity), part 2 of 3" "Activity diagram converted from fence F31 (line 906)." {
    include strSetSessionTitleCommand_TurnLocatorAgentAndSessionIdNoTurn strSetSessionTitleCommand_400InvalidSessionIdentifier strSetSessionTitleCommand_404NotFound strSetSessionTitleCommand_OverwriteTheSessionTitle
    autolayout lr 250 150
}
custom "SetSessionTitleCommand-part3" "SetSessionTitleCommand: retitle a session (activity), part 3 of 3" "Activity diagram converted from fence F31 (line 906)." {
    include strSetSessionTitleCommand_OverwriteTheSessionTitle strSetSessionTitleCommand_Save strSetSessionTitleCommand_200RetitledTrue
    autolayout tb 100 200
}
custom "TerminalTurnOutcomeGuard" "Policy TerminalTurnOutcomeGuard: terminal-turn outcome guard (`ITerminalTurnOutcomeGuard`) (activity), part 1 of 2" "Activity diagram converted from fence F32 (line 924)." {
    include strTerminalTurnOutcomeGuard_GuardCalledWithStoredAndIncoming strTerminalTurnOutcomeGuard_StoredTurnExists strTerminalTurnOutcomeGuard_Created strTerminalTurnOutcomeGuard_StoredStatusTerminalByTurnStatusIsTerminal
    autolayout tb 100 100
}
custom "TerminalTurnOutcomeGuard-part2" "Policy TerminalTurnOutcomeGuard: terminal-turn outcome guard (`ITerminalTurnOutcomeGuard`) (activity), part 2 of 2" "Activity diagram converted from fence F32 (line 924)." {
    include strTerminalTurnOutcomeGuard_StoredStatusTerminalByTurnStatusIsTerminal strTerminalTurnOutcomeGuard_SkippedTerminalWithStoredStatusAndReason strTerminalTurnOutcomeGuard_IncomingEqualsStored strTerminalTurnOutcomeGuard_MergedReasonNoChange strTerminalTurnOutcomeGuard_MergedOmittedFieldsNeverClobberStored
    autolayout tb 100 100
}
custom "ApplyArrivalBridgeRecord" "Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 1 of 7" "Activity diagram converted from fence F33 (line 947)." {
    include strApplyArrivalBridgeRecord_TranscriptBytesPastAppliedThroughOffset strApplyArrivalBridgeRecord_TranscriptShorterThanTheOffset strApplyArrivalBridgeRecord_SetSessionUnsupportedFormatUnrecognized strApplyArrivalBridgeRecord_CompleteRecordWithinTheBoundLength
    autolayout tb 100 100
}
custom "ApplyArrivalBridgeRecord-part2" "Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 2 of 7" "Activity diagram converted from fence F33 (line 947)." {
    include strApplyArrivalBridgeRecord_SetSessionUnsupportedFormatUnrecognized strApplyArrivalBridgeRecord_StopBeforeTheRecordUnsupportedNo
    autolayout lr 250 150
}
custom "ApplyArrivalBridgeRecord-part3" "Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 3 of 7" "Activity diagram converted from fence F33 (line 947)." {
    include strApplyArrivalBridgeRecord_CompleteRecordWithinTheBoundLength strApplyArrivalBridgeRecord_StopPartialRecordResumeLater strApplyArrivalBridgeRecord_KnownOperationWithEveryRequiredField strApplyArrivalBridgeRecord_SetSessionUnsupportedFormatUnrecognized strApplyArrivalBridgeRecord_Operation
    autolayout tb 100 100
}
custom "ApplyArrivalBridgeRecord-part4" "Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 4 of 7" "Activity diagram converted from fence F33 (line 947)." {
    include strApplyArrivalBridgeRecord_Operation strApplyArrivalBridgeRecord_ApplyEnqueueRecord strApplyArrivalBridgeRecord_ApplyDequeueRecord strApplyArrivalBridgeRecord_ApplyRemoveRecord strApplyArrivalBridgeRecord_PopAllAttributable
    autolayout lr 200 50
}
custom "ApplyArrivalBridgeRecord-part5" "Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 5 of 7" "Activity diagram converted from fence F33 (line 947)." {
    include strApplyArrivalBridgeRecord_ApplyEnqueueRecord strApplyArrivalBridgeRecord_UpdateTurnStateChangeAndOffsetOne strApplyArrivalBridgeRecord_ApplyDequeueRecord strApplyArrivalBridgeRecord_SetSessionUnsupportedFormatUnrecognized strApplyArrivalBridgeRecord_ApplyRemoveRecord strApplyArrivalBridgeRecord_SetSessionUnsupportedQueueRemovalAmbiguous
    autolayout lr 250 150
}
custom "ApplyArrivalBridgeRecord-part6" "Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 6 of 7" "Activity diagram converted from fence F33 (line 947)." {
    include strApplyArrivalBridgeRecord_PopAllAttributable strApplyArrivalBridgeRecord_SetSessionUnsupportedQueueRemovalAmbiguous strApplyArrivalBridgeRecord_WithdrawQueuedRequestsDropTheRest
    autolayout lr 250 150
}
custom "ApplyArrivalBridgeRecord-part7" "Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 7 of 7" "Activity diagram converted from fence F33 (line 947)." {
    include strApplyArrivalBridgeRecord_UpdateTurnStateChangeAndOffsetOne strApplyArrivalBridgeRecord_RecordApplied strApplyArrivalBridgeRecord_SetSessionUnsupportedQueueRemovalAmbiguous strApplyArrivalBridgeRecord_StopBeforeTheRecordUnsupportedNo strApplyArrivalBridgeRecord_WithdrawQueuedRequestsDropTheRest
    autolayout tb 100 100
}
custom "ApplyEnqueueRecord" "Nested activity: ApplyEnqueueRecord (enqueue step of ApplyArrivalBridgeRecord) (activity), part 1 of 3" "Activity diagram converted from fence F34 (line 977)." {
    include strApplyEnqueueRecord_EnqueueRecord strApplyEnqueueRecord_SystemEvent strApplyEnqueueRecord_EntryKindSystemNoMint strApplyEnqueueRecord_MintWithTheBridgeNonceMintSessionTurnCommand
    autolayout tb 100 100
}
custom "ApplyEnqueueRecord-part2" "Nested activity: ApplyEnqueueRecord (enqueue step of ApplyArrivalBridgeRecord) (activity), part 2 of 3" "Activity diagram converted from fence F34 (line 977)." {
    include strApplyEnqueueRecord_MintWithTheBridgeNonceMintSessionTurnCommand strApplyEnqueueRecord_ResolveMintOutcomeNewEnqueueRecord
    autolayout lr 250 150
}
custom "ApplyEnqueueRecord-part3" "Nested activity: ApplyEnqueueRecord (enqueue step of ApplyArrivalBridgeRecord) (activity), part 3 of 3" "Activity diagram converted from fence F34 (line 977)." {
    include strApplyEnqueueRecord_ResolveMintOutcomeNewEnqueueRecord strApplyEnqueueRecord_EntryKindRequestQueuedRequestId strApplyEnqueueRecord_EntryKindRequestTerminalAtMint strApplyEnqueueRecord_EntryUnderPendingNonceFailsafeMint strApplyEnqueueRecord_EntryKindOpenFailedWithThe
    autolayout lr 200 50
}
custom "ApplyDequeueRecord" "Nested activity: ApplyDequeueRecord (dequeue step of ApplyArrivalBridgeRecord) (activity), part 1 of 2" "Activity diagram converted from fence F35 (line 993)." {
    include strApplyDequeueRecord_DequeueRecord strApplyDequeueRecord_QueueHasAHeadEntry strApplyDequeueRecord_NoQueueHeadFormatUnrecognized strApplyDequeueRecord_HeadEntryKind
    autolayout tb 100 100
}
custom "ApplyDequeueRecord-part2" "Nested activity: ApplyDequeueRecord (dequeue step of ApplyArrivalBridgeRecord) (activity), part 2 of 2" "Activity diagram converted from fence F35 (line 993)." {
    include strApplyDequeueRecord_HeadEntryKind strApplyDequeueRecord_DeliveredWriteTargetOpenListLedger strApplyDequeueRecord_EntryDroppedNothingDelivered
    autolayout lr 250 150
}
custom "ApplyRemoveRecord" "Nested activity: ApplyRemoveRecord (remove step of ApplyArrivalBridgeRecord) (activity), part 1 of 2" "Activity diagram converted from fence F36 (line 1006)." {
    include strApplyRemoveRecord_RemoveRecord strApplyRemoveRecord_ExactlyOneQueuedEntryMatches strApplyRemoveRecord_ZeroOrSeveralMatches strApplyRemoveRecord_MatchedEntryKind
    autolayout tb 100 100
}
custom "ApplyRemoveRecord-part2" "Nested activity: ApplyRemoveRecord (remove step of ApplyArrivalBridgeRecord) (activity), part 2 of 2" "Activity diagram converted from fence F36 (line 1006)." {
    include strApplyRemoveRecord_MatchedEntryKind strApplyRemoveRecord_EntryDroppedNothingDelivered strApplyRemoveRecord_ReasonAbsorbedMidTurn strApplyRemoveRecord_DeliveredBecomesTheWriteTarget strApplyRemoveRecord_WithdrawnWriteTargetUnchanged
    autolayout tb 100 100
}
custom "OpenTurnFromPromptHook" "Process OpenTurnFromPromptHook: open a turn from the prompt hook (hosts without a bridge) (activity), part 1 of 4" "Activity diagram converted from fence F37 (line 1029)." {
    include strOpenTurnFromPromptHook_PromptHookFires strOpenTurnFromPromptHook_PayloadCarriesTheHostSessionKey strOpenTurnFromPromptHook_HostSessionUnknownNothingWritten strOpenTurnFromPromptHook_HostSupportedForTurnLogging
    autolayout tb 100 100
}
custom "OpenTurnFromPromptHook-part2" "Process OpenTurnFromPromptHook: open a turn from the prompt hook (hosts without a bridge) (activity), part 2 of 4" "Activity diagram converted from fence F37 (line 1029)." {
    include strOpenTurnFromPromptHook_HostSupportedForTurnLogging strOpenTurnFromPromptHook_SetSessionUnsupportedTheFailingCheck strOpenTurnFromPromptHook_SystemEvent strOpenTurnFromPromptHook_TurnLoggingUnsupportedNoMint
    autolayout tb 100 100
}
custom "OpenTurnFromPromptHook-part3" "Process OpenTurnFromPromptHook: open a turn from the prompt hook (hosts without a bridge) (activity), part 3 of 4" "Activity diagram converted from fence F37 (line 1029)." {
    include strOpenTurnFromPromptHook_SystemEvent strOpenTurnFromPromptHook_SystemEventSkippedNoMint strOpenTurnFromPromptHook_ProcessDrainFailsafeWhenRecordsAreDrainable strOpenTurnFromPromptHook_UpdateTurnStateOneChangeNeverWaiting
    autolayout tb 100 100
}
custom "OpenTurnFromPromptHook-part4" "Process OpenTurnFromPromptHook: open a turn from the prompt hook (hosts without a bridge) (activity), part 4 of 4" "Activity diagram converted from fence F37 (line 1029)." {
    include strOpenTurnFromPromptHook_UpdateTurnStateOneChangeNeverWaiting strOpenTurnFromPromptHook_TurnOpenContendedDiagnosticNoMint strOpenTurnFromPromptHook_OpenTurnUnderLock strOpenTurnFromPromptHook_StateReplacedLockReleasedHookStatus
    autolayout tb 100 100
}
custom "OpenTurnUnderLock" "Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change) (activity), part 1 of 6" "Activity diagram converted from fence F38 (line 1049)." {
    include strOpenTurnUnderLock_LockHeldOpenThisPromptS strOpenTurnUnderLock_HostNonceFieldPresent strOpenTurnUnderLock_TurnOpenFailedNonceSourceMissing strOpenTurnUnderLock_EqualFieldHostNonceAlreadyRecorded
    autolayout tb 100 100
}
custom "OpenTurnUnderLock-part2" "Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change) (activity), part 2 of 6" "Activity diagram converted from fence F38 (line 1049)." {
    include strOpenTurnUnderLock_EqualFieldHostNonceAlreadyRecorded strOpenTurnUnderLock_SetSessionUnsupportedIndistinguishableArrival strOpenTurnUnderLock_WriteAheadFailsafeMintRecord strOpenTurnUnderLock_NothingBound strOpenTurnUnderLock_FirstIngressNoMcpSessionIdRecorded
    autolayout tb 100 100
}
custom "OpenTurnUnderLock-part3" "Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change) (activity), part 3 of 6" "Activity diagram converted from fence F38 (line 1049)." {
    include strOpenTurnUnderLock_FirstIngressNoMcpSessionIdRecorded strOpenTurnUnderLock_RecordANewMcpSessionIdInThis strOpenTurnUnderLock_MintWithTheHostNonceMintSessionTurnCommand strOpenTurnUnderLock_InvokeSessionOpenThatMcpSessionId strOpenTurnUnderLock_ResolveMintOutcomeNewInvocation
    autolayout tb 100 200
}
custom "OpenTurnUnderLock-part4" "Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change) (activity), part 4 of 6" "Activity diagram converted from fence F38 (line 1049)." {
    include strOpenTurnUnderLock_InvokeSessionOpenThatMcpSessionId strOpenTurnUnderLock_MintWithTheHostNonceMintSessionTurnCommand strOpenTurnUnderLock_TurnOpenedDegradedPendingNonceRecord strOpenTurnUnderLock_TurnOpenFailedServerCodeAttempt
    autolayout lr 250 150
}
custom "OpenTurnUnderLock-part5" "Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change) (activity), part 5 of 6" "Activity diagram converted from fence F38 (line 1049)." {
    include strOpenTurnUnderLock_ResolveMintOutcomeNewInvocation strOpenTurnUnderLock_TurnOpenedDegradedPendingNonceRecord strOpenTurnUnderLock_TurnOpenFailedServerCodeAttempt strOpenTurnUnderLock_Answer
    autolayout lr 250 150
}
custom "OpenTurnUnderLock-part6" "Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change) (activity), part 6 of 6" "Activity diagram converted from fence F38 (line 1049)." {
    include strOpenTurnUnderLock_Answer strOpenTurnUnderLock_TurnOpenedDelivered strOpenTurnUnderLock_TurnAlreadyOpenSameRequestId strOpenTurnUnderLock_StoredStatusReportedNotDelivered strOpenTurnUnderLock_NothingBound2
    autolayout lr 200 50
}
custom "BindVerbToTurn" "Process BindVerbToTurn: bind a session-log verb to its turn (activity), part 1 of 5" "Activity diagram converted from fence F39 (line 1086)." {
    include strBindVerbToTurn_VerbInvoked strBindVerbToTurn_SyncPluginArrivalLogUpToTheLength strBindVerbToTurn_ResolveVerbHostSession
    autolayout tb 100 200
}
custom "BindVerbToTurn-part2" "Process BindVerbToTurn: bind a session-log verb to its turn (activity), part 2 of 5" "Activity diagram converted from fence F39 (line 1086)." {
    include strBindVerbToTurn_ResolveVerbHostSession strBindVerbToTurn_RefuseHostSessionUnknown strBindVerbToTurn_RefuseHostSessionAmbiguous strBindVerbToTurn_UnsupportedSinceOnALiveHostSession
    autolayout lr 250 150
}
custom "BindVerbToTurn-part3" "Process BindVerbToTurn: bind a session-log verb to its turn (activity), part 3 of 5" "Activity diagram converted from fence F39 (line 1086)." {
    include strBindVerbToTurn_ResolveVerbHostSession strBindVerbToTurn_ResolveVerbTarget
    autolayout lr 250 150
}
custom "BindVerbToTurn-part4" "Process BindVerbToTurn: bind a session-log verb to its turn (activity), part 4 of 5" "Activity diagram converted from fence F39 (line 1086)." {
    include strBindVerbToTurn_UnsupportedSinceOnALiveHostSession strBindVerbToTurn_NotRecordReviewOpenAtFailClose strBindVerbToTurn_TargetLocallyTerminal strBindVerbToTurn_ResolveVerbTarget strBindVerbToTurn_RefuseWithTheCodeReturned
    autolayout tb 100 100
}
custom "BindVerbToTurn-part5" "Process BindVerbToTurn: bind a session-log verb to its turn (activity), part 5 of 5" "Activity diagram converted from fence F39 (line 1086)." {
    include strBindVerbToTurn_NotRecordReviewOpenAtFailClose strBindVerbToTurn_BindAndCaptureAsUnsupportedSession strBindVerbToTurn_RefuseTurnLoggingUnsupported strBindVerbToTurn_TargetLocallyTerminal strBindVerbToTurn_RefuseTurnImmutable strBindVerbToTurn_BindingCapturedOncePersist
    autolayout lr 200 50
}
custom "ResolveVerbHostSession" "Nested activity: ResolveVerbHostSession (host session step of BindVerbToTurn) (activity), part 1 of 3" "Activity diagram converted from fence F40 (line 1108)." {
    include strResolveVerbHostSession_ResolveTheVerbSHostSession strResolveVerbHostSession_HostSessionKeyInTheInvocation strResolveVerbHostSession_HostSessionLoaded strResolveVerbHostSession_TrustedPairSuppliedAndVerified
    autolayout tb 100 100
}
custom "ResolveVerbHostSession-part2" "Nested activity: ResolveVerbHostSession (host session step of BindVerbToTurn) (activity), part 2 of 3" "Activity diagram converted from fence F40 (line 1108)." {
    include strResolveVerbHostSession_TrustedPairSuppliedAndVerified strResolveVerbHostSession_TrustedPairItsTurn strResolveVerbHostSession_LiveHostSessionsOfThisAgent
    autolayout lr 250 150
}
custom "ResolveVerbHostSession-part3" "Nested activity: ResolveVerbHostSession (host session step of BindVerbToTurn) (activity), part 3 of 3" "Activity diagram converted from fence F40 (line 1108)." {
    include strResolveVerbHostSession_LiveHostSessionsOfThisAgent strResolveVerbHostSession_None strResolveVerbHostSession_MoreThanOne strResolveVerbHostSession_HostSessionLoadedHostSessionInferred
    autolayout lr 250 150
}
custom "ResolveVerbTarget" "Nested activity: ResolveVerbTarget (target step of BindVerbToTurn) (activity), part 1 of 5" "Activity diagram converted from fence F41 (line 1124)." {
    include strResolveVerbTarget_ResolveTheTargetInTheLoaded strResolveVerbTarget_ExplicitTarget
    autolayout lr 250 150
}
custom "ResolveVerbTarget-part2" "Nested activity: ResolveVerbTarget (target step of BindVerbToTurn) (activity), part 2 of 5" "Activity diagram converted from fence F41 (line 1124)." {
    include strResolveVerbTarget_ExplicitTarget strResolveVerbTarget_PairVerifiedAndRecordedHere strResolveVerbTarget_KnownInThisMCPSession strResolveVerbTarget_DeliveredRequestsOpen
    autolayout lr 250 150
}
custom "ResolveVerbTarget-part3" "Nested activity: ResolveVerbTarget (target step of BindVerbToTurn) (activity), part 3 of 5" "Activity diagram converted from fence F41 (line 1124)." {
    include strResolveVerbTarget_PairVerifiedAndRecordedHere strResolveVerbTarget_RefusedRequestUnknownAnotherConversation strResolveVerbTarget_TargetResolved strResolveVerbTarget_KnownInThisMCPSession strResolveVerbTarget_RefusedRequestUnknown
    autolayout lr 250 150
}
custom "ResolveVerbTarget-part4" "Nested activity: ResolveVerbTarget (target step of BindVerbToTurn) (activity), part 4 of 5" "Activity diagram converted from fence F41 (line 1124)." {
    include strResolveVerbTarget_DeliveredRequestsOpen strResolveVerbTarget_RefusedRequestAmbiguous strResolveVerbTarget_TargetTheWriteTarget strResolveVerbTarget_RefusedRequestUnknownNamingTheKey
    autolayout lr 250 150
}
custom "ResolveVerbTarget-part5" "Nested activity: ResolveVerbTarget (target step of BindVerbToTurn) (activity), part 5 of 5" "Activity diagram converted from fence F41 (line 1124)." {
    include strResolveVerbTarget_TargetTheWriteTarget strResolveVerbTarget_TargetResolved
    autolayout lr 250 150
}
custom "PersistTurnWriteWithReceipt" "Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 1 of 8" "Activity diagram converted from fence F42 (line 1150)." {
    include strPersistTurnWriteWithReceipt_BoundWriteReady strPersistTurnWriteWithReceipt_ThisHostSessionSFailsafeHolds strPersistTurnWriteWithReceipt_ProcessDrainFailsafe strPersistTurnWriteWithReceipt_CompleteTurnOrFailTurnNeverThePlugin
    autolayout tb 100 100
}
custom "PersistTurnWriteWithReceipt-part2" "Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 2 of 8" "Activity diagram converted from fence F42 (line 1150)." {
    include strPersistTurnWriteWithReceipt_ProcessDrainFailsafe strPersistTurnWriteWithReceipt_CaptureThisWriteInTheFailsafe strPersistTurnWriteWithReceipt_BoundIdPendingNonceWithNo
    autolayout tb 100 200
}
custom "PersistTurnWriteWithReceipt-part3" "Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 3 of 8" "Activity diagram converted from fence F42 (line 1150)." {
    include strPersistTurnWriteWithReceipt_CompleteTurnOrFailTurnNeverThePlugin strPersistTurnWriteWithReceipt_TestModelEvidenceAModelAuthoredAction strPersistTurnWriteWithReceipt_SendBoundWriteTheBoundWrite strPersistTurnWriteWithReceipt_CaptureThisWriteInTheFailsafe strPersistTurnWriteWithReceipt_ReceiptPersistedFalseCodeQueuedDegraded
    autolayout lr 250 150
}
custom "PersistTurnWriteWithReceipt-part4" "Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 4 of 8" "Activity diagram converted from fence F42 (line 1150)." {
    include strPersistTurnWriteWithReceipt_BoundIdPendingNonceWithNo strPersistTurnWriteWithReceipt_CaptureThisWriteInTheFailsafe strPersistTurnWriteWithReceipt_CompleteTurnOrFailTurnNeverThePlugin
    autolayout lr 250 150
}
custom "PersistTurnWriteWithReceipt-part5" "Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 5 of 8" "Activity diagram converted from fence F42 (line 1150)." {
    include strPersistTurnWriteWithReceipt_TestModelEvidenceAModelAuthoredAction strPersistTurnWriteWithReceipt_RefuseTurnMissingModelEvidenceTurn strPersistTurnWriteWithReceipt_CloseCarriesEvidence
    autolayout lr 250 150
}
custom "PersistTurnWriteWithReceipt-part6" "Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 6 of 8" "Activity diagram converted from fence F42 (line 1150)." {
    include strPersistTurnWriteWithReceipt_SendBoundWriteTheBoundWrite strPersistTurnWriteWithReceipt_ReceiptReturnedWithItsExitCode strPersistTurnWriteWithReceipt_CloseCarriesEvidence strPersistTurnWriteWithReceipt_SendBoundWriteTheEvidenceAsAnAdditive
    autolayout tb 100 200
}
custom "PersistTurnWriteWithReceipt-part7" "Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 7 of 8" "Activity diagram converted from fence F42 (line 1150)." {
    include strPersistTurnWriteWithReceipt_SendBoundWriteTheEvidenceAsAnAdditive strPersistTurnWriteWithReceipt_SendBoundWriteTheBoundWrite strPersistTurnWriteWithReceipt_FailsafeKeepsTheCloseAfterThe strPersistTurnWriteWithReceipt_EvidenceReceiptReturnedTheCloseIs
    autolayout lr 250 150
}
custom "PersistTurnWriteWithReceipt-part8" "Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 8 of 8" "Activity diagram converted from fence F42 (line 1150)." {
    include strPersistTurnWriteWithReceipt_FailsafeKeepsTheCloseAfterThe strPersistTurnWriteWithReceipt_ReceiptPersistedFalseCodeQueuedDegraded
    autolayout lr 250 150
}
custom "SendBoundWrite" "Nested activity: SendBoundWrite (send step of PersistTurnWriteWithReceipt, for the evidence write and the bound write, and of the drain's ReplayBoundRecord) (activity), part 1 of 5" "Activity diagram converted from fence F43 (line 1177)." {
    include strSendBoundWrite_WriteToSendWithItsBinding strSendBoundWrite_SameFingerprintAsTheLastConfirmed strSendBoundWrite_UnchangedNoSecondWriteExit0 strSendBoundWrite_WriteAheadFailsafeRecordSaved
    autolayout tb 100 100
}
custom "SendBoundWrite-part2" "Nested activity: SendBoundWrite (send step of PersistTurnWriteWithReceipt, for the evidence write and the bound write, and of the drain's ReplayBoundRecord) (activity), part 2 of 5" "Activity diagram converted from fence F43 (line 1177)." {
    include strSendBoundWrite_WriteAheadFailsafeRecordSaved strSendBoundWrite_LostPersistedFalseExit1 strSendBoundWrite_SendThroughTheREPLBoundOr strSendBoundWrite_Result
    autolayout tb 100 100
}
custom "SendBoundWrite-part3" "Nested activity: SendBoundWrite (send step of PersistTurnWriteWithReceipt, for the evidence write and the bound write, and of the drain's ReplayBoundRecord) (activity), part 3 of 5" "Activity diagram converted from fence F43 (line 1177)." {
    include strSendBoundWrite_Result strSendBoundWrite_QueuedRecordKeptExit0 strSendBoundWrite_ErrorCode strSendBoundWrite_Route
    autolayout lr 250 150
}
custom "SendBoundWrite-part4" "Nested activity: SendBoundWrite (send step of PersistTurnWriteWithReceipt, for the evidence write and the bound write, and of the drain's ReplayBoundRecord) (activity), part 4 of 5" "Activity diagram converted from fence F43 (line 1177)." {
    include strSendBoundWrite_ErrorCode strSendBoundWrite_RecordTheRefusalAndMissingItems strSendBoundWrite_RequestUnknownExit1 strSendBoundWrite_ServerCodeExit1
    autolayout lr 250 150
}
custom "SendBoundWrite-part5" "Nested activity: SendBoundWrite (send step of PersistTurnWriteWithReceipt, for the evidence write and the bound write, and of the drain's ReplayBoundRecord) (activity), part 5 of 5" "Activity diagram converted from fence F43 (line 1177)." {
    include strSendBoundWrite_Route strSendBoundWrite_PersistedRecordClearedExit0 strSendBoundWrite_MapSubmitOutcome strSendBoundWrite_RecordTheRefusalAndMissingItems strSendBoundWrite_ServerMessageAndMissingItemsExit
    autolayout lr 250 150
}
custom "MapSubmitOutcome" "Nested activity: MapSubmitOutcome (submit answer step of SendBoundWrite) (activity), part 1 of 3" "Activity diagram converted from fence F44 (line 1200)." {
    include strMapSubmitOutcome_SubmitAnswered201 strMapSubmitOutcome_TurnsOutcomeListPresent strMapSubmitOutcome_PersistedOutcomeVerifiedFalseCloseUnconfirmed strMapSubmitOutcome_OutcomeForTheBoundRequestId
    autolayout tb 100 100
}
custom "MapSubmitOutcome-part2" "Nested activity: MapSubmitOutcome (submit answer step of SendBoundWrite) (activity), part 2 of 3" "Activity diagram converted from fence F44 (line 1200)." {
    include strMapSubmitOutcome_OutcomeForTheBoundRequestId strMapSubmitOutcome_CacheStoredStatusAsTerminal strMapSubmitOutcome_RejectedRecordKeptExit1 strMapSubmitOutcome_AClose
    autolayout lr 250 150
}
custom "MapSubmitOutcome-part3" "Nested activity: MapSubmitOutcome (submit answer step of SendBoundWrite) (activity), part 3 of 3" "Activity diagram converted from fence F44 (line 1200)." {
    include strMapSubmitOutcome_CacheStoredStatusAsTerminal strMapSubmitOutcome_SkippedTerminalRecordClearedExit1 strMapSubmitOutcome_AClose strMapSubmitOutcome_RemoveFromOpenDeliveredAndTheLedger strMapSubmitOutcome_PersistedStoredStatusRecordClearedExit0
    autolayout tb 100 100
}
custom "DrainFailsafe" "Process DrainFailsafe: drain the failsafe (activity)" "Activity diagram converted from fence F45 (line 1230)." {
    include strDrainFailsafe_DrainStarts strDrainFailsafe_ForEachRecordOldestFirstDrainOneFailsafeRecord strDrainFailsafe_WalkStoppedNoAttemptCountedThe strDrainFailsafe_DrainComplete
    autolayout tb 100 100
}
custom "DrainOneFailsafeRecord" "Nested activity: DrainOneFailsafeRecord (loop body of DrainFailsafe) (activity), part 1 of 5" "Activity diagram converted from fence F46 (line 1241)." {
    include strDrainOneFailsafeRecord_NextFailsafeRecord strDrainOneFailsafeRecord_ReadAndRepairedMethodAndParams strDrainOneFailsafeRecord_QuarantinedMalformed strDrainOneFailsafeRecord_ItsTurnUnresolvedEarlierInThis
    autolayout tb 100 100
}
custom "DrainOneFailsafeRecord-part2" "Nested activity: DrainOneFailsafeRecord (loop body of DrainFailsafe) (activity), part 2 of 5" "Activity diagram converted from fence F46 (line 1241)." {
    include strDrainOneFailsafeRecord_ItsTurnUnresolvedEarlierInThis strDrainOneFailsafeRecord_SkippedNoAttemptCounted strDrainOneFailsafeRecord_DrainAttemptsAt50AfterA strDrainOneFailsafeRecord_QuarantinedAttemptBudgetUsed strDrainOneFailsafeRecord_CapturedBinding
    autolayout tb 100 100
}
custom "DrainOneFailsafeRecord-part3" "Nested activity: DrainOneFailsafeRecord (loop body of DrainFailsafe) (activity), part 3 of 5" "Activity diagram converted from fence F46 (line 1241)." {
    include strDrainOneFailsafeRecord_CapturedBinding strDrainOneFailsafeRecord_ParamsNameItsSessionAndRequest strDrainOneFailsafeRecord_UnsupportedSessionOfALiveHost strDrainOneFailsafeRecord_QuarantinedLegacyRecordNoRequest strDrainOneFailsafeRecord_ReplayItsMethodAndParamsAs
    autolayout tb 100 100
}
custom "DrainOneFailsafeRecord-part4" "Nested activity: DrainOneFailsafeRecord (loop body of DrainFailsafe) (activity), part 4 of 5" "Activity diagram converted from fence F46 (line 1241)." {
    include strDrainOneFailsafeRecord_UnsupportedSessionOfALiveHost strDrainOneFailsafeRecord_NotDrainedInThatHostSession strDrainOneFailsafeRecord_ReplayBoundRecord strDrainOneFailsafeRecord_ReplayItsMethodAndParamsAs strDrainOneFailsafeRecord_Answer
    autolayout tb 100 100
}
custom "DrainOneFailsafeRecord-part5" "Nested activity: DrainOneFailsafeRecord (loop body of DrainFailsafe) (activity), part 5 of 5" "Activity diagram converted from fence F46 (line 1241)." {
    include strDrainOneFailsafeRecord_Answer strDrainOneFailsafeRecord_ReturnStopNoAttemptCounted strDrainOneFailsafeRecord_RecordRemovedOutcomeReported strDrainOneFailsafeRecord_DrainAttemptsPlus1TurnUnresolved strDrainOneFailsafeRecord_QuarantinedWithTheReason
    autolayout lr 200 50
}
custom "ReplayBoundRecord" "Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 1 of 7" "Activity diagram converted from fence F47 (line 1268)." {
    include strReplayBoundRecord_BoundRecord strReplayBoundRecord_BoundToAPendingIdWhose strReplayBoundRecord_NotReplayableMintQuarantined strReplayBoundRecord_InvokeSessionOpenEnvelopeMcpSessionId
    autolayout tb 100 100
}
custom "ReplayBoundRecord-part2" "Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 2 of 7" "Activity diagram converted from fence F47 (line 1268)." {
    include strReplayBoundRecord_InvokeSessionOpenEnvelopeMcpSessionId strReplayBoundRecord_Retryable strReplayBoundRecord_Final strReplayBoundRecord_RecordKind
    autolayout lr 250 150
}
custom "ReplayBoundRecord-part3" "Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 3 of 7" "Activity diagram converted from fence F47 (line 1268)." {
    include strReplayBoundRecord_RecordKind strReplayBoundRecord_MintWithTheOriginalNonceMintSessionTurnCommand strReplayBoundRecord_TurnInProgressOnTheServer strReplayBoundRecord_SendBoundWriteAliasBoundIdOrPair strReplayBoundRecord_ResolveMintOutcomeStoredRetry
    autolayout tb 100 100
}
custom "ReplayBoundRecord-part4" "Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 4 of 7" "Activity diagram converted from fence F47 (line 1268)." {
    include strReplayBoundRecord_TurnInProgressOnTheServer strReplayBoundRecord_NotReplayableTurnNotInProgress strReplayBoundRecord_SendBoundWriteAliasBoundIdOrPair
    autolayout lr 250 150
}
custom "ReplayBoundRecord-part5" "Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 5 of 7" "Activity diagram converted from fence F47 (line 1268)." {
    include strReplayBoundRecord_SendBoundWriteAliasBoundIdOrPair strReplayBoundRecord_Retryable strReplayBoundRecord_Confirmed strReplayBoundRecord_Final
    autolayout lr 250 150
}
custom "ReplayBoundRecord-part6" "Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 6 of 7" "Activity diagram converted from fence F47 (line 1268)." {
    include strReplayBoundRecord_ResolveMintOutcomeStoredRetry strReplayBoundRecord_Retryable strReplayBoundRecord_Final strReplayBoundRecord_JournalTheAliasRewriteDeliverOnce
    autolayout lr 250 150
}
custom "ReplayBoundRecord-part7" "Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 7 of 7" "Activity diagram converted from fence F47 (line 1268)." {
    include strReplayBoundRecord_JournalTheAliasRewriteDeliverOnce strReplayBoundRecord_Confirmed
    autolayout lr 250 150
}
custom "EvaluateStopGate" "Process EvaluateStopGate: evaluate the Stop gate (activity), part 1 of 4" "Activity diagram converted from fence F48 (line 1299)." {
    include strEvaluateStopGate_StopEvent strEvaluateStopGate_SyncPluginArrivalLogRecordsUpToThe strEvaluateStopGate_LoadThisHostSessionSLedger strEvaluateStopGate_ComputeFindingsWithThePureFunction strEvaluateStopGate_ForEachTurnOfThisHost
    autolayout tb 100 200
}
custom "EvaluateStopGate-part2" "Process EvaluateStopGate: evaluate the Stop gate (activity), part 2 of 4" "Activity diagram converted from fence F48 (line 1299)." {
    include strEvaluateStopGate_ForEachTurnOfThisHost strEvaluateStopGate_SessionFindingsThePerHostSession strEvaluateStopGate_AnyBlockFinding
    autolayout tb 100 200
}
custom "EvaluateStopGate-part3" "Process EvaluateStopGate: evaluate the Stop gate (activity), part 3 of 4" "Activity diagram converted from fence F48 (line 1299)." {
    include strEvaluateStopGate_AnyBlockFinding strEvaluateStopGate_HostForcesTheStopAnywayOr strEvaluateStopGate_UpdateTurnStateClearConfirmedClosedLedger strEvaluateStopGate_UpdateTurnStateRecordStopGateOverridden strEvaluateStopGate_BlockWithTheMessageReEvaluated
    autolayout tb 100 100
}
custom "EvaluateStopGate-part4" "Process EvaluateStopGate: evaluate the Stop gate (activity), part 4 of 4" "Activity diagram converted from fence F48 (line 1299)." {
    include strEvaluateStopGate_UpdateTurnStateClearConfirmedClosedLedger strEvaluateStopGate_PassWithAndAnyWarnings strEvaluateStopGate_UpdateTurnStateRecordStopGateOverridden strEvaluateStopGate_StopNotHeldFindingsReturnedAs
    autolayout lr 250 150
}
custom "EvaluateTurnAtStop" "Nested activity: EvaluateTurnAtStop (loop body of EvaluateStopGate) (activity), part 1 of 3" "Activity diagram converted from fence F49 (line 1319)." {
    include strEvaluateTurnAtStop_NextTurnOfThisHostSession strEvaluateTurnAtStop_ServerReportsTheTurnTerminal strEvaluateTurnAtStop_TerminalStatusAndEvidence strEvaluateTurnAtStop_OpenWhenUnsupportedSinceWasRecorded
    autolayout tb 100 100
}
custom "EvaluateTurnAtStop-part2" "Nested activity: EvaluateTurnAtStop (loop body of EvaluateStopGate) (activity), part 2 of 3" "Activity diagram converted from fence F49 (line 1319)." {
    include strEvaluateTurnAtStop_TerminalStatusAndEvidence strEvaluateTurnAtStop_WarnModelCanceledLeavesTheOpen strEvaluateTurnAtStop_WarnClosedWithoutEvidenceLeavesThe strEvaluateTurnAtStop_LeavesTheOpenListEligibleTo
    autolayout lr 250 150
}
custom "EvaluateTurnAtStop-part3" "Nested activity: EvaluateTurnAtStop (loop body of EvaluateStopGate) (activity), part 3 of 3" "Activity diagram converted from fence F49 (line 1319)." {
    include strEvaluateTurnAtStop_OpenWhenUnsupportedSinceWasRecorded strEvaluateTurnAtStop_ItsCloseCapturedInTheFailsafe strEvaluateTurnAtStop_EvaluateOpenTurnAtStop strEvaluateTurnAtStop_WarnWithTheUnsupportedSinceReason strEvaluateTurnAtStop_BlockReducedRemedyOfGetCloseRemedy
    autolayout tb 100 100
}
custom "EvaluateOpenTurnAtStop" "Nested activity: EvaluateOpenTurnAtStop (open turn step of EvaluateTurnAtStop) (activity), part 1 of 4" "Activity diagram converted from fence F50 (line 1337)." {
    include strEvaluateOpenTurnAtStop_TurnNotTerminalOnTheServer strEvaluateOpenTurnAtStop_LastCloseRefusedByTheCompletion strEvaluateOpenTurnAtStop_BlockSessionTurnCompletionGate strEvaluateOpenTurnAtStop_Withdrawn
    autolayout tb 100 100
}
custom "EvaluateOpenTurnAtStop-part2" "Nested activity: EvaluateOpenTurnAtStop (open turn step of EvaluateTurnAtStop) (activity), part 2 of 4" "Activity diagram converted from fence F50 (line 1337)." {
    include strEvaluateOpenTurnAtStop_Withdrawn strEvaluateOpenTurnAtStop_ItsCloseHeldByAnOutage strEvaluateOpenTurnAtStop_Delivered strEvaluateOpenTurnAtStop_WarnDegradedWithdrawnCloseHeld strEvaluateOpenTurnAtStop_BlockTurnWithdrawnUnclosed
    autolayout tb 100 100
}
custom "EvaluateOpenTurnAtStop-part3" "Nested activity: EvaluateOpenTurnAtStop (open turn step of EvaluateTurnAtStop) (activity), part 3 of 4" "Activity diagram converted from fence F50 (line 1337)." {
    include strEvaluateOpenTurnAtStop_Delivered strEvaluateOpenTurnAtStop_ACloseHeldByAnOutage strEvaluateOpenTurnAtStop_QueuedLocally strEvaluateOpenTurnAtStop_WarnDegradedPersistenceLedgerKept strEvaluateOpenTurnAtStop_BlockTurnInProgress
    autolayout tb 100 100
}
custom "EvaluateOpenTurnAtStop-part4" "Nested activity: EvaluateOpenTurnAtStop (open turn step of EvaluateTurnAtStop) (activity), part 4 of 4" "Activity diagram converted from fence F50 (line 1337)." {
    include strEvaluateOpenTurnAtStop_QueuedLocally strEvaluateOpenTurnAtStop_WarnQueuedUndeliveredRequest strEvaluateOpenTurnAtStop_ClosedEligibleToClear
    autolayout lr 250 150
}
custom "ValidateHookRegistration" "Process ValidateHookRegistration: validate and repair hook registration (activity), part 1 of 3" "Activity diagram converted from fence F51 (line 1359)." {
    include strValidateHookRegistration_ValidatorRuns strValidateHookRegistration_TestPluginEnabledReadTheEnabledPluginsMap strValidateHookRegistration_GetDuplicateHookCommandsInventoryMCPHookCommands strValidateHookRegistration_AnyEventRegisteredMoreThanOnce
    autolayout tb 100 200
}
custom "ValidateHookRegistration-part2" "Process ValidateHookRegistration: validate and repair hook registration (activity), part 2 of 3" "Activity diagram converted from fence F51 (line 1359)." {
    include strValidateHookRegistration_AnyEventRegisteredMoreThanOnce strValidateHookRegistration_ExistingResultWithPluginEnabledAndPluginKey strValidateHookRegistration_CheckOnly strValidateHookRegistration_StatusDuplicateWithDuplicateHooksNoFile strValidateHookRegistration_SettingsFilesBackedUp
    autolayout tb 100 100
}
custom "ValidateHookRegistration-part3" "Process ValidateHookRegistration: validate and repair hook registration (activity), part 3 of 3" "Activity diagram converted from fence F51 (line 1359)." {
    include strValidateHookRegistration_SettingsFilesBackedUp strValidateHookRegistration_BackupFailureReportedNoSettingsFile strValidateHookRegistration_RemoveManagedDuplicateBridgeHooksKeep strValidateHookRegistration_StatusRepairedRestartTheHost
    autolayout tb 100 100
}
custom "StartHostSession" "Process StartHostSession: start a host session (activity), part 1 of 7" "Activity diagram converted from fence F52 (line 1382)." {
    include strStartHostSession_SessionStartPostCompactOrAPromptWith strStartHostSession_HostSessionKeyInThePayload strStartHostSession_HostSessionUnknownNothingWritten strStartHostSession_TrustBootstrapVerified
    autolayout tb 100 100
}
custom "StartHostSession-part2" "Process StartHostSession: start a host session (activity), part 2 of 7" "Activity diagram converted from fence F52 (line 1382)." {
    include strStartHostSession_TrustBootstrapVerified strStartHostSession_MCPUNTRUSTEDInSessionStateYaml strStartHostSession_EndedYamlPresentForThisKey
    autolayout lr 250 150
}
custom "StartHostSession-part3" "Process StartHostSession: start a host session (activity), part 3 of 7" "Activity diagram converted from fence F52 (line 1382)." {
    include strStartHostSession_EndedYamlPresentForThisKey strStartHostSession_UpdateTurnStateRemoveTheEndedMarker strStartHostSession_UpdateTurnStateRecordANewMcpSessionId strStartHostSession_InvokeSessionOpenTheRecordedMcpSessionId
    autolayout tb 100 200
}
custom "StartHostSession-part4" "Process StartHostSession: start a host session (activity), part 4 of 7" "Activity diagram converted from fence F52 (line 1382)." {
    include strStartHostSession_InvokeSessionOpenTheRecordedMcpSessionId strStartHostSession_SessionNotOpenedInvalidSessionIdentifier strStartHostSession_HostUsesTheArrivalBridge
    autolayout lr 250 150
}
custom "StartHostSession-part5" "Process StartHostSession: start a host session (activity), part 5 of 7" "Activity diagram converted from fence F52 (line 1382)." {
    include strStartHostSession_HostUsesTheArrivalBridge strStartHostSession_ProcessRunArrivalBridgeKeepTheLiveBridge strStartHostSession_SyncPluginArrivalLogRecordsUpToThe strStartHostSession_UnsupportedSinceCauseClearableByItsRule
    autolayout tb 100 200
}
custom "StartHostSession-part6" "Process StartHostSession: start a host session (activity), part 6 of 7" "Activity diagram converted from fence F52 (line 1382)." {
    include strStartHostSession_UnsupportedSinceCauseClearableByItsRule strStartHostSession_UpdateTurnStateClearTheCauseSo strStartHostSession_ProcessDrainFailsafe strStartHostSession_PhaseCModeReadAndCached strStartHostSession_ListTheOpenTurnsWithTheir
    autolayout tb 100 200
}
custom "StartHostSession-part7" "Process StartHostSession: start a host session (activity), part 7 of 7" "Activity diagram converted from fence F52 (line 1382)." {
    include strStartHostSession_ListTheOpenTurnsWithTheir strStartHostSession_SessionReady
    autolayout lr 250 150
}
custom "EndHostSession" "Process EndHostSession: end a host session (activity)" "Activity diagram converted from fence F53 (line 1409)." {
    include strEndHostSession_SessionEndHook strEndHostSession_WriteSessionsKeyEndedYamlWith strEndHostSession_ArrivalBridgeRunningForTheKey strEndHostSession_StopTheBridge strEndHostSession_EndedOnlyASessionStartForThe
    autolayout tb 100 200
}
custom "SyncAgentPluginsAtVersion" "Process SyncAgentPluginsAtVersion: sync the agent plugins at a fixed version (activity), part 1 of 3" "Activity diagram converted from fence F54 (line 1425)." {
    include strSyncAgentPluginsAtVersion_SyncAgentPluginsAtTheGivenPluginVersion strSyncAgentPluginsAtVersion_PluginParentResolvesToTheNine strSyncAgentPluginsAtVersion_FailReposMissing strSyncAgentPluginsAtVersion_ForEachOfTheNineRepos
    autolayout tb 100 100
}
custom "SyncAgentPluginsAtVersion-part2" "Process SyncAgentPluginsAtVersion: sync the agent plugins at a fixed version (activity), part 2 of 3" "Activity diagram converted from fence F54 (line 1425)." {
    include strSyncAgentPluginsAtVersion_ForEachOfTheNineRepos strSyncAgentPluginsAtVersion_FailNonPowerShellFileInA strSyncAgentPluginsAtVersion_WriteTheReceiptNineReposVersions strSyncAgentPluginsAtVersion_AllNineConvergedOnVersionAnd
    autolayout tb 100 100
}
custom "SyncAgentPluginsAtVersion-part3" "Process SyncAgentPluginsAtVersion: sync the agent plugins at a fixed version (activity), part 3 of 3" "Activity diagram converted from fence F54 (line 1425)." {
    include strSyncAgentPluginsAtVersion_AllNineConvergedOnVersionAnd strSyncAgentPluginsAtVersion_FailReRunUntilConverged strSyncAgentPluginsAtVersion_RefreshTheInstalledCachesOfThe strSyncAgentPluginsAtVersion_SyncPassesTrackedTreeCheckLater
    autolayout tb 100 100
}
custom "SyncOneAgentPlugin" "Nested activity: SyncOneAgentPlugin (loop body of SyncAgentPluginsAtVersion) (activity), part 1 of 4" "Activity diagram converted from fence F55 (line 1440)." {
    include strSyncOneAgentPlugin_NextPluginRepo strSyncOneAgentPlugin_CopyLibPsAndLibTool strSyncOneAgentPlugin_PackagedPathsPowerShellOnly
    autolayout tb 100 200
}
custom "SyncOneAgentPlugin-part2" "Nested activity: SyncOneAgentPlugin (loop body of SyncAgentPluginsAtVersion) (activity), part 2 of 4" "Activity diagram converted from fence F55 (line 1440)." {
    include strSyncOneAgentPlugin_PackagedPathsPowerShellOnly strSyncOneAgentPlugin_PackagedPathCheckFailedTheSync strSyncOneAgentPlugin_TypeScriptHostThatLoadsCompiledOutput
    autolayout lr 250 150
}
custom "SyncOneAgentPlugin-part3" "Nested activity: SyncOneAgentPlugin (loop body of SyncAgentPluginsAtVersion) (activity), part 3 of 4" "Activity diagram converted from fence F55 (line 1440)." {
    include strSyncOneAgentPlugin_TypeScriptHostThatLoadsCompiledOutput strSyncOneAgentPlugin_CompileTheAdapterWithTheHost strSyncOneAgentPlugin_VersionAlreadyTheTarget
    autolayout tb 100 200
}
custom "SyncOneAgentPlugin-part4" "Nested activity: SyncOneAgentPlugin (loop body of SyncAgentPluginsAtVersion) (activity), part 4 of 4" "Activity diagram converted from fence F55 (line 1440)." {
    include strSyncOneAgentPlugin_VersionAlreadyTheTarget strSyncOneAgentPlugin_RefreshContentOnlyNoBump strSyncOneAgentPlugin_WriteTheTargetVersionNpmVersion strSyncOneAgentPlugin_RepoSyncedContinueWithTheNext
    autolayout tb 100 100
}
custom "RunArrivalBridge" "Process RunArrivalBridge: arrival bridge lifecycle (claude-code, claude-cowork) (activity), part 1 of 3" "Activity diagram converted from fence F56 (line 1460)." {
    include strRunArrivalBridge_SessionStartForABridgeHost strRunArrivalBridge_ArrivalBridgePidForThisHost strRunArrivalBridge_KeepTheRunningBridge strRunArrivalBridge_StopABridgeOfAnotherVersion
    autolayout tb 100 100
}
custom "RunArrivalBridge-part2" "Process RunArrivalBridge: arrival bridge lifecycle (claude-code, claude-cowork) (activity), part 2 of 3" "Activity diagram converted from fence F56 (line 1460)." {
    include strRunArrivalBridge_StopABridgeOfAnotherVersion strRunArrivalBridge_ReadAppliedThroughOffsetKeptAndTheTranscript strRunArrivalBridge_TailTheTranscriptNestedTailTranscriptRecords
    autolayout tb 100 200
}
custom "RunArrivalBridge-part3" "Process RunArrivalBridge: arrival bridge lifecycle (claude-code, claude-cowork) (activity), part 3 of 3" "Activity diagram converted from fence F56 (line 1460)." {
    include strRunArrivalBridge_TailTheTranscriptNestedTailTranscriptRecords strRunArrivalBridge_BridgeStops strRunArrivalBridge_BridgeStopsBeforeTheRecordUnsupported
    autolayout lr 250 150
}
custom "TailTranscriptRecords" "Nested activity: TailTranscriptRecords (loop of RunArrivalBridge) (activity), part 1 of 4" "Activity diagram converted from fence F57 (line 1473)." {
    include strTailTranscriptRecords_TailFromAppliedThroughOffset strTailTranscriptRecords_NewCompleteRecordsPastTheOffset strTailTranscriptRecords_WaitForTheTranscriptToGrow strTailTranscriptRecords_ProcessApplyArrivalBridgeRecordTheNextRecord
    autolayout tb 100 100
}
custom "TailTranscriptRecords-part2" "Nested activity: TailTranscriptRecords (loop of RunArrivalBridge) (activity), part 2 of 4" "Activity diagram converted from fence F57 (line 1473)." {
    include strTailTranscriptRecords_WaitForTheTranscriptToGrow strTailTranscriptRecords_EndedYamlWrittenWithReasonSession strTailTranscriptRecords_ProcessApplyArrivalBridgeRecordTheNextRecord strTailTranscriptRecords_RecordApplied
    autolayout tb 100 200
}
custom "TailTranscriptRecords-part3" "Nested activity: TailTranscriptRecords (loop of RunArrivalBridge) (activity), part 3 of 4" "Activity diagram converted from fence F57 (line 1473)." {
    include strTailTranscriptRecords_EndedYamlWrittenWithReasonSession strTailTranscriptRecords_ReturnSessionEndMarkerWritten strTailTranscriptRecords_NewCompleteRecordsPastTheOffset
    autolayout lr 250 150
}
custom "TailTranscriptRecords-part4" "Nested activity: TailTranscriptRecords (loop of RunArrivalBridge) (activity), part 4 of 4" "Activity diagram converted from fence F57 (line 1473)." {
    include strTailTranscriptRecords_RecordApplied strTailTranscriptRecords_ReturnUnsupported strTailTranscriptRecords_WaitForTheTranscriptToGrow strTailTranscriptRecords_NewCompleteRecordsPastTheOffset
    autolayout tb 100 100
}
dynamic qbrain.plugins "STR-04-02" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, part 1 of 7"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: ref nested sequence PrepareHookMint (prompt hook only)"
    qbrain.plugins.pluginPs -> qbrain.repl "workflow.sessionlog.mintTurn (hook invocation)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-02-part2" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, part 2 of 7"
    qbrain.repl -> qbrain.server.api "POST /qbrainai/sessionlog/{agent}/{sessionId}/mint"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-02-part3" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, part 3 of 7"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(MintSessionTurnCommand)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "(1 of 2) Note: WorkspaceResolutionBehavior (400), then nonce,"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-02-part4" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, part 4 of 7"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "(2 of 2) planFile, todoId"
    qbrain.server.strSessionLogHandlers -> qbrain.server.mcpDbContext "SerializableWrite: TurnLocator, IdempotentCreate (session, nonce)"
    qbrain.server.mcpDbContext -> qbrain.server.strSessionLogHandlers "created, stored turn, or a typed failure"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-02-part5" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, part 5 of 7"
    qbrain.server.strSessionLogHandlers -> qbrain.server.api "result"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-02-part6" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, part 6 of 7"
    qbrain.repl -> qbrain.plugins.pluginPs "result, or a transport outage"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Resolve-MintOutcome"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-02-part7" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, part 7 of 7"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: ref nested sequence ApplyMintOutcome (below)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-02-alt1-1" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, alt 1 branch 1 of 6: created"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 6: created"
    qbrain.server.api -> qbrain.repl "201 requestId, created"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-02-alt1-2" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, alt 1 branch 2 of 6: nonce already minted in this session"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 6: nonce already minted in this session"
    qbrain.server.api -> qbrain.repl "200 requestId, merged, storedStatus"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-02-alt1-3" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, alt 1 branch 3 of 6: nonce, session id, planFile, or todoId invalid"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 3 of 6: nonce, session id, planFile, or todoId invalid"
    qbrain.server.api -> qbrain.repl "400 ingress_nonce_invalid or validation_error"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-02-alt1-4" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, alt 1 branch 4 of 6: SessionNotFound"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 4 of 6: SessionNotFound"
    qbrain.server.api -> qbrain.repl "404 session_not_open"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-02-alt1-5" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, alt 1 branch 5 of 6: four attempts used by a transient conflict"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 5 of 6: four attempts used by a transient conflict"
    qbrain.server.api -> qbrain.repl "409 conflict, retryable true"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-02-alt1-6" "Sequence diagram converted from fence F58 (line 1508)." {
    title "Sequence: submit MintSessionTurnCommand and process each response, alt 1 branch 6 of 6: six request-id attempts used"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 6 of 6: six request-id attempts used"
    qbrain.server.api -> qbrain.repl "500 mint_collision_exhausted"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Update-TurnState takes turn-open.lock for the open"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-part2" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-alt1-1" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 1 of 4: lock contended, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 4: lock contended"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-alt1-1-part2" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 1 of 4: lock contended, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: turn-open-contended: diagnostic, no mint"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-alt1-2" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 2 of 4: host nonce field missing, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 2 of 4: host nonce field missing"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-alt1-2-part2" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 2 of 4: host nonce field missing, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: turn-open-failed nonce_source_missing, no mint"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-alt1-3" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 3 of 4: equal-field host, nonce already recorded, part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 3 of 4: equal-field host, nonce already recorded"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-alt1-3-part2" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 3 of 4: equal-field host, nonce already recorded, part 2 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Set-SessionUnsupported indistinguishable_arrival"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-alt1-3-part3" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 3 of 4: equal-field host, nonce already recorded, part 3 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: nothing minted or bound"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-alt1-4" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 4 of 4: nonce derived, part 1 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 4 of 4: nonce derived"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-alt1-4-part2" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 4 of 4: nonce derived, part 2 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "write-ahead failsafe mint record under pending-nonce"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-alt1-4-part3" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 4 of 4: nonce derived, part 3 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) opt 1: first ingress on a host without a SessionStart event (next"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-alt1-4-part4" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 4 of 4: nonce derived, part 4 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) view)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-alt1-4-part5" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 4 of 4: nonce derived, part 5 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: continue with the mint call"
    autolayout lr 300 100
}
dynamic qbrain.plugins "PrepareHookMint-opt1" "Sequence diagram converted from fence F59 (line 1548)." {
    title "Nested sequence: PrepareHookMint (prompt hook steps before the mint call), opt 1: first ingress on a host without a SessionStart event"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "opt 1: first ingress on a host without a SessionStart event"
    qbrain.plugins.pluginPs -> qbrain.repl "Invoke-SessionOpen (sequence OpenSessionLogCommand)"
    qbrain.repl -> qbrain.plugins.pluginPs "confirmed, outage, or final"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome)"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-1" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 1 of 6: created, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 6: created"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-1-part2" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 1 of 6: created, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: bridge: entry queued. Hook: turn-opened, delivered"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-2" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 2 of 6: merged in progress, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 2 of 6: merged in progress"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-2-part2" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 2 of 6: merged in progress, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: same requestId. Hook: turn-already-open, rebound"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-3" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 3 of 6: unsupported (copilot or classic cline, new invocation merged), part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 1 branch 3 of 6: unsupported (copilot or classic cline,"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-3-part2" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 3 of 6: unsupported (copilot or classic cline, new invocation merged), part 2 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) new invocation merged)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-3-part3" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 3 of 6: unsupported (copilot or classic cline, new invocation merged), part 3 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Set-SessionUnsupported indistinguishable_arrival"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-4" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 4 of 6: merged terminal, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 4 of 6: merged terminal"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-4-part2" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 4 of 6: merged terminal, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: storedStatus reported, never delivered"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-5" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 5 of 6: outage, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 5 of 6: outage"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-5-part2" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 5 of 6: outage, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: pending-nonce open, mint record kept: turn-opened-degraded"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-6" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 6 of 6: final, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 6 of 6: final"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ApplyMintOutcome-alt1-6-part2" "Sequence diagram converted from fence F60 (line 1575)." {
    title "Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 6 of 6: final, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: turn-open-failed with the code, the Stop gate warns"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-03" "Sequence diagram converted from fence F61 (line 1598)." {
    title "Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "POST /qbrainai/sessionlog (UnifiedSessionLogDto)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-03-part2" "Sequence diagram converted from fence F61 (line 1598)." {
    title "Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, part 2 of 4"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(SubmitSessionLogCommand)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "(1 of 2) Note: WorkspaceResolutionBehavior (400), then document"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-03-part3" "Sequence diagram converted from fence F61 (line 1598)." {
    title "Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, part 3 of 4"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "(2 of 2) checks, last-wins"
    qbrain.server.strSessionLogHandlers -> qbrain.server.mcpDbContext "SerializableWrite: TurnWritePipeline mode Submit for each turn, then one save"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-03-part4" "Sequence diagram converted from fence F61 (line 1598)." {
    title "Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, part 4 of 4"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1: one of 5 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-03-alt1-1" "Sequence diagram converted from fence F61 (line 1598)." {
    title "Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 1 of 5: every turn evaluated, nothing refused, part 1 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1 branch 1 of 5: every turn evaluated, nothing refused"
    qbrain.server.strSessionLogHandlers -> qbrain.server.api "per-turn outcomes"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-03-alt1-1-part2" "Sequence diagram converted from fence F61 (line 1598)." {
    title "Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 1 of 5: every turn evaluated, nothing refused, part 2 of 3"
    qbrain.server.api -> qbrain.libs.client "201 turns [requestId, outcome, storedStatus, reason]"
    qbrain.libs.client -> qbrain.libs.client "(1 of 2) Note: created and merged (reason no_change for an identical"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-04-03-alt1-1-part3" "Sequence diagram converted from fence F61 (line 1598)." {
    title "Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 1 of 5: every turn evaluated, nothing refused, part 3 of 3"
    qbrain.libs.client -> qbrain.libs.client "(2 of 2) turn) count as persisted. skipped_terminal is reported, never retried"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-03-alt1-2" "Sequence diagram converted from fence F61 (line 1598)." {
    title "Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 2 of 5: document invalid, a status outside the agent set, InvalidIdentifier, or InvalidPayload, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 2 of 5: document invalid, a status outside the agent set,"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-03-alt1-2-part2" "Sequence diagram converted from fence F61 (line 1598)." {
    title "Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 2 of 5: document invalid, a status outside the agent set, InvalidIdentifier, or InvalidPayload, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) InvalidIdentifier, or InvalidPayload"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: report the code, nothing persisted"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-03-alt1-3" "Sequence diagram converted from fence F61 (line 1598)." {
    title "Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 3 of 5: a turn's RequestClassInvalid"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 3 of 5: a turn's RequestClassInvalid"
    qbrain.server.api -> qbrain.libs.client "400 request_class_invalid, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: correct the class and resend"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-03-alt1-4" "Sequence diagram converted from fence F61 (line 1598)." {
    title "Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 4 of 5: CompletionGateMissing for one or more turns"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 4 of 5: CompletionGateMissing for one or more turns"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, session_turn_completion_gate naming each failing requestId and its missing items, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: supply the missing items by the turn's pair, the turn stays open"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-03-alt1-5" "Sequence diagram converted from fence F61 (line 1598)." {
    title "Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 5 of 5: SerializableWrite's four attempts used"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 5 of 5: SerializableWrite's four attempts used"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: resend the same document"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, part 1 of 2"
    strHostModel -> qbrain.plugins.pluginPs "appendActions, appendDecisions, or updateTurn (optional requestId or original pair)"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: ref process BindVerbToTurn (section 4.4)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-part2" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt1-1" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 1 of 3: binding refused"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 3: binding refused"
    qbrain.plugins.pluginPs -> strHostModel "request_unknown, request_ambiguous, turn_immutable, host_session_unknown, host_session_ambiguous, or turn_logging_unsupported"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt1-2" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 2 of 3: unsupported session, target already open when it failed closed, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 1 branch 2 of 3: unsupported session, target already open"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt1-2-part2" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 2 of 3: unsupported session, target already open when it failed closed, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) when it failed closed"
    qbrain.plugins.pluginPs -> strHostModel "captured as an unsupported_session failsafe record, drained once that host session ends or its cause clears"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt1-3" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 1 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 3 of 3: bound"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt1-3-part2" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 2 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 3) Note over Plugin and Controller: ref process"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt1-3-part3" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 3 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 3) PersistTurnWriteWithReceipt: POST /qbrainai/sessionlog through"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt1-3-part4" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 4 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(3 of 3) the REPL with only the bound turn (sequence SubmitSessionLogCommand)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt1-3-part5" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 5 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2: one of 5 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt2-1" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 1 of 5: created or merged"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 1 of 5: created or merged"
    qbrain.plugins.pluginPs -> strHostModel "persisted true, exit 0"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt2-2" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 2 of 5: skipped_terminal"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 2 of 5: skipped_terminal"
    qbrain.plugins.pluginPs -> strHostModel "persisted false, code skipped_terminal, storedStatus cached as terminal, failsafe cleared, exit 1"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt2-3" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 3 of 5: 201 without per-turn outcomes"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 3 of 5: 201 without per-turn outcomes"
    qbrain.plugins.pluginPs -> strHostModel "outcomeVerified false, never a confirmed write"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt2-4" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 4 of 5: 400 validation_error or request_class_invalid, or the completion gate, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 2 branch 4 of 5: 400 validation_error or"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt2-4-part2" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 4 of 5: 400 validation_error or request_class_invalid, or the completion gate, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) request_class_invalid, or the completion gate"
    qbrain.plugins.pluginPs -> strHostModel "persisted false with the code, exit 1. A session_turn_completion_gate refusal is recorded for the Stop gate"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt2-5" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 5 of 5: transport outage, retryable 503, or 409 conflict retryable, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 2 branch 5 of 5: transport outage, retryable 503, or 409 conflict"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-04-alt2-5-part2" "Sequence diagram converted from fence F62 (line 1632)." {
    title "Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 5 of 5: transport outage, retryable 503, or 409 conflict retryable, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) retryable"
    qbrain.plugins.pluginPs -> strHostModel "failsafe record keyed by the bound requestId: persisted false, code queued, exit 0"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, part 1 of 2"
    qbrain.libs.client -> qbrain.server.api "POST /qbrainai/sessionlog/{agent}/{sessionId}/turn (UnifiedRequestEntryDto)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-part2" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, part 2 of 2"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(UpsertSessionTurnCommand)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "Note: WorkspaceResolutionBehavior (400), then the body check"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-alt1-1" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, alt 1 branch 1 of 2: no body"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: no body"
    qbrain.server.api -> qbrain.libs.client "400 problem Invalid turn body."
    qbrain.libs.client -> qbrain.libs.client "Note: send the turn"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-alt1-2" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, alt 1 branch 2 of 2: body present, part 1 of 2"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1 branch 2 of 2: body present"
    qbrain.server.strSessionLogHandlers -> qbrain.server.mcpDbContext "SerializableWrite: TurnWritePipeline mode Merge, insert when absent"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-alt1-2-part2" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, alt 1 branch 2 of 2: body present, part 2 of 2"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 2: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-alt2-1" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 1 of 6: turn written (inserted, or merged additively whatever its status), part 1 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "(1 of 2) alt 2 branch 1 of 6: turn written (inserted, or merged"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-alt2-1-part2" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 1 of 6: turn written (inserted, or merged additively whatever its status), part 2 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "(2 of 2) additively whatever its status)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.api "turnId"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-alt2-1-part3" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 1 of 6: turn written (inserted, or merged additively whatever its status), part 3 of 3"
    qbrain.server.api -> qbrain.libs.client "201 turnId, agent, sessionId, requestId"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-alt2-2" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 2 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 2 branch 2 of 6: InvalidIdentifier, or InvalidPayload"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-alt2-2-part2" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 2 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) (turn context, attribution, or QBAgent compliance)"
    qbrain.server.api -> qbrain.libs.client "400 problem Invalid turn payload."
    qbrain.libs.client -> qbrain.libs.client "Note: fix and resend"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-alt2-3" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 3 of 6: SessionNotFound"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 3 of 6: SessionNotFound"
    qbrain.server.api -> qbrain.libs.client "404 not_found"
    qbrain.libs.client -> qbrain.libs.client "Note: open the session first (OpenSessionLogCommand)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-alt2-4" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 4 of 6: RequestClassInvalid"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 4 of 6: RequestClassInvalid"
    qbrain.server.api -> qbrain.libs.client "400 request_class_invalid"
    qbrain.libs.client -> qbrain.libs.client "Note: correct the class and resend"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-alt2-5" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 5 of 6: CompletionGateMissing"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 5 of 6: CompletionGateMissing"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, session_turn_completion_gate and the missing items, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: supply the missing items, the turn stays open"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-05-alt2-6" "Sequence diagram converted from fence F63 (line 1664)." {
    title "Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 6 of 6: SerializableWrite's four attempts used"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 6 of 6: SerializableWrite's four attempts used"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: resend unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-06" "Sequence diagram converted from fence F64 (line 1705)." {
    title "Sequence: submit PatchSessionTurnCommand and process each response, part 1 of 3"
    qbrain.libs.client -> qbrain.server.api "PATCH /qbrainai/sessionlog/{agent}/{sessionId}/{requestId} (fields to change)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-06-part2" "Sequence diagram converted from fence F64 (line 1705)." {
    title "Sequence: submit PatchSessionTurnCommand and process each response, part 2 of 3"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(PatchSessionTurnCommand)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "Note: WorkspaceResolutionBehavior (400)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-06-part3" "Sequence diagram converted from fence F64 (line 1705)." {
    title "Sequence: submit PatchSessionTurnCommand and process each response, part 3 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.mcpDbContext "SerializableWrite: TurnWritePipeline mode Merge, insert when absent"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-06-alt1-1" "Sequence diagram converted from fence F64 (line 1705)." {
    title "Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 1 of 6: turn written (inserted, or merged additively whatever its status), part 1 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "(1 of 2) alt 1 branch 1 of 6: turn written (inserted, or merged"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-06-alt1-1-part2" "Sequence diagram converted from fence F64 (line 1705)." {
    title "Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 1 of 6: turn written (inserted, or merged additively whatever its status), part 2 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "(2 of 2) additively whatever its status)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.api "turnId"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-06-alt1-1-part3" "Sequence diagram converted from fence F64 (line 1705)." {
    title "Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 1 of 6: turn written (inserted, or merged additively whatever its status), part 3 of 3"
    qbrain.server.api -> qbrain.libs.client "200 turnId, agent, sessionId, requestId, status"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-06-alt1-2" "Sequence diagram converted from fence F64 (line 1705)." {
    title "Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 2 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 2 of 6: InvalidIdentifier, or InvalidPayload"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-06-alt1-2-part2" "Sequence diagram converted from fence F64 (line 1705)." {
    title "Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 2 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) (turn context, attribution, or QBAgent compliance)"
    qbrain.server.api -> qbrain.libs.client "400 problem Invalid turn payload."
    qbrain.libs.client -> qbrain.libs.client "Note: fix and resend"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-06-alt1-3" "Sequence diagram converted from fence F64 (line 1705)." {
    title "Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 3 of 6: SessionNotFound"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 3 of 6: SessionNotFound"
    qbrain.server.api -> qbrain.libs.client "404 not_found"
    qbrain.libs.client -> qbrain.libs.client "Note: open the session first (OpenSessionLogCommand)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-06-alt1-4" "Sequence diagram converted from fence F64 (line 1705)." {
    title "Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 4 of 6: RequestClassInvalid"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 4 of 6: RequestClassInvalid"
    qbrain.server.api -> qbrain.libs.client "400 request_class_invalid"
    qbrain.libs.client -> qbrain.libs.client "Note: correct the class and resend"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-06-alt1-5" "Sequence diagram converted from fence F64 (line 1705)." {
    title "Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 5 of 6: CompletionGateMissing"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 5 of 6: CompletionGateMissing"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, session_turn_completion_gate and the missing items, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: supply the missing items, the turn stays open"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-06-alt1-6" "Sequence diagram converted from fence F64 (line 1705)." {
    title "Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 6 of 6: SerializableWrite's four attempts used"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 6 of 6: SerializableWrite's four attempts used"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: resend unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-07" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, part 1 of 3"
    qbrain.libs.client -> qbrain.server.api "PUT /qbrainai/sessionlog/{agent}/{sessionId}/{requestId} (whole turn)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-07-part2" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, part 2 of 3"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(ReplaceSessionTurnCommand)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "Note: WorkspaceResolutionBehavior (400)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-07-part3" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, part 3 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.mcpDbContext "SerializableWrite: TurnWritePipeline mode Replace"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-07-alt1-1" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 1 of 6: replaced, part 1 of 2"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1 branch 1 of 6: replaced"
    qbrain.server.strSessionLogHandlers -> qbrain.server.api "turnId"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-07-alt1-1-part2" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 1 of 6: replaced, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "REST 200 turnId, agent, sessionId, requestId, replaced true, MCP success true with the same fields"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-07-alt1-2" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 2 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 2 of 6: InvalidIdentifier, or InvalidPayload"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-07-alt1-2-part2" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 2 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) (turn context, attribution, or QBAgent compliance)"
    qbrain.server.api -> qbrain.libs.client "400 validation_error"
    qbrain.libs.client -> qbrain.libs.client "Note: fix and resend"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-07-alt1-3" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 3 of 6: SessionNotFound or TurnNotFound"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 3 of 6: SessionNotFound or TurnNotFound"
    qbrain.server.api -> qbrain.libs.client "404 not_found"
    qbrain.libs.client -> qbrain.libs.client "Note: report: the turn must exist before a replace"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-07-alt1-4" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 4 of 6: RequestClassInvalid"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 4 of 6: RequestClassInvalid"
    qbrain.server.api -> qbrain.libs.client "400 request_class_invalid"
    qbrain.libs.client -> qbrain.libs.client "Note: correct the class and resend"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-07-alt1-5" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 5 of 6: CompletionGateMissing (the replacement carries completed or failed, over any stored status), part 1 of 3"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 5 of 6: CompletionGateMissing (the replacement"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-07-alt1-5-part2" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 5 of 6: CompletionGateMissing (the replacement carries completed or failed, over any stored status), part 2 of 3"
    qbrain.server.api -> qbrain.server.api "(2 of 2) carries completed or failed, over any stored status)"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, session_turn_completion_gate and the missing items, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "(1 of 2) Note: resend the replacement with the missing items, or with a"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-04-07-alt1-5-part3" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 5 of 6: CompletionGateMissing (the replacement carries completed or failed, over any stored status), part 3 of 3"
    qbrain.libs.client -> qbrain.libs.client "(2 of 2) status other than completed or failed"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-07-alt1-6" "Sequence diagram converted from fence F65 (line 1741)." {
    title "Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 6 of 6: SerializableWrite's four attempts used"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 6 of 6: SerializableWrite's four attempts used"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: resend unchanged"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, part 1 of 2"
    strHostModel -> qbrain.plugins.pluginPs "completeTurn, or failTurn with errorMessage"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: ref process BindVerbToTurn (section 4.4)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-part2" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt1-1" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 1 branch 1 of 3: binding refused"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 3: binding refused"
    qbrain.plugins.pluginPs -> strHostModel "the refusal code"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt1-2" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 1 branch 2 of 3: unsupported session, target open when it failed closed"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 2 of 3: unsupported session, target open when it failed closed"
    qbrain.plugins.pluginPs -> strHostModel "close captured as an unsupported_session record"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt1-3" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 1 branch 3 of 3: bound, part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 3 of 3: bound"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt1-3-part2" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 1 branch 3 of 3: bound, part 2 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Test-ModelEvidence"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt1-3-part3" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 1 branch 3 of 3: bound, part 3 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt2-1" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 1 of 2: no model-authored action, decision, or commit, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 2 branch 1 of 2: no model-authored action, decision, or"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt2-1-part2" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 1 of 2: no model-authored action, decision, or commit, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) commit"
    qbrain.plugins.pluginPs -> strHostModel "turn_missing_model_evidence, nothing sent, exit 1"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt2-2" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 2 of 2: evidence present, part 1 of 6"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 2 of 2: evidence present"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt2-2-part2" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 2 of 2: evidence present, part 2 of 6"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Get-CloseRemedy, advisory"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt2-2-part3" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 2 of 2: evidence present, part 3 of 6"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "opt 1: items missing (next view)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt2-2-part4" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 2 of 2: evidence present, part 4 of 6"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "opt 2: the close carries evidence (next view)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt2-2-part5" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 2 of 2: evidence present, part 5 of 6"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) Note over Plugin and Controller: ref PersistTurnWriteWithReceipt:"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-alt2-2-part6" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 2 of 2: evidence present, part 6 of 6"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) the close (sequence SubmitSessionLogCommand)"
    strHostModel -> strHostModel "Note over Model and Plugin: ref nested sequence ReceiveCloseAnswer (below)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-opt1" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, opt 1: items missing"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "opt 1: items missing"
    qbrain.plugins.pluginPs -> strHostModel "warning with the missing items, the close is still sent"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-08-opt2" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, opt 2: the close carries evidence, part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "opt 2: the close carries evidence"
    qbrain.plugins.pluginPs -> qbrain.server.api "evidence as an additive write without a status (REPL)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-opt2-part2" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, opt 2: the close carries evidence, part 2 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "break 1: final server code on the evidence write (next view)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-opt2-part3" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, opt 2: the close carries evidence, part 3 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "break 2: retryable answer on the evidence write (next view)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-break1" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, break 1: final server code on the evidence write"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "break 1: final server code on the evidence write"
    qbrain.plugins.pluginPs -> strHostModel "persisted false with the code, close not sent, exit 1"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-08-break2" "Sequence diagram converted from fence F66 (line 1777)." {
    title "Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, break 2: retryable answer on the evidence write"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "break 2: retryable answer on the evidence write"
    qbrain.plugins.pluginPs -> strHostModel "evidence and close kept in the failsafe, queued, exit 0"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReceiveCloseAnswer" "Sequence diagram converted from fence F67 (line 1817)." {
    title "Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence)"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReceiveCloseAnswer-alt1-1" "Sequence diagram converted from fence F67 (line 1817)." {
    title "Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 1 of 6: created or merged"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 6: created or merged"
    qbrain.plugins.pluginPs -> strHostModel "persisted true, removed from the open list and ledger, exit 0"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReceiveCloseAnswer-alt1-2" "Sequence diagram converted from fence F67 (line 1817)." {
    title "Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 2 of 6: completion gate refused"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 2 of 6: completion gate refused"
    qbrain.plugins.pluginPs -> strHostModel "server message and missing items, open and ledgered, exit 1"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReceiveCloseAnswer-alt1-3" "Sequence diagram converted from fence F67 (line 1817)." {
    title "Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 3 of 6: 400 validation_error or request_class_invalid, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 1 branch 3 of 6: 400 validation_error or"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReceiveCloseAnswer-alt1-3-part2" "Sequence diagram converted from fence F67 (line 1817)." {
    title "Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 3 of 6: 400 validation_error or request_class_invalid, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) request_class_invalid"
    qbrain.plugins.pluginPs -> strHostModel "persisted false with the code, nothing changed, exit 1"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReceiveCloseAnswer-alt1-4" "Sequence diagram converted from fence F67 (line 1817)." {
    title "Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 4 of 6: skipped_terminal"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 4 of 6: skipped_terminal"
    qbrain.plugins.pluginPs -> strHostModel "skipped_terminal, storedStatus cached as terminal, exit 1"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReceiveCloseAnswer-alt1-5" "Sequence diagram converted from fence F67 (line 1817)." {
    title "Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 5 of 6: 201 without per-turn outcomes"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 5 of 6: 201 without per-turn outcomes"
    qbrain.plugins.pluginPs -> strHostModel "outcomeVerified false, kept open and ledgered"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReceiveCloseAnswer-alt1-6" "Sequence diagram converted from fence F67 (line 1817)." {
    title "Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 6 of 6: transport outage, retryable 503, or 409 conflict retryable, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 1 branch 6 of 6: transport outage, retryable 503, or 409 conflict"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReceiveCloseAnswer-alt1-6-part2" "Sequence diagram converted from fence F67 (line 1817)." {
    title "Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 6 of 6: transport outage, retryable 503, or 409 conflict retryable, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) retryable"
    qbrain.plugins.pluginPs -> strHostModel "failsafe close kept, persisted false, queued, exit 0"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-09" "Sequence diagram converted from fence F68 (line 1841)." {
    title "Sequence: submit CompleteSessionTurnCommand and process each response, part 1 of 3"
    qbrain.libs.client -> qbrain.server.api "POST /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/complete (optional UnifiedRequestEntryDto)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-09-part2" "Sequence diagram converted from fence F68 (line 1841)." {
    title "Sequence: submit CompleteSessionTurnCommand and process each response, part 2 of 3"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(CompleteSessionTurnCommand)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "Note: WorkspaceResolutionBehavior (400)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-09-part3" "Sequence diagram converted from fence F68 (line 1841)." {
    title "Sequence: submit CompleteSessionTurnCommand and process each response, part 3 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.mcpDbContext "SerializableWrite: TurnWritePipeline mode Complete"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-09-alt1-1" "Sequence diagram converted from fence F68 (line 1841)." {
    title "Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 1 of 6: completed, or an already completed turn enriched, part 1 of 2"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1 branch 1 of 6: completed, or an already completed turn enriched"
    qbrain.server.strSessionLogHandlers -> qbrain.server.api "turnId"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-09-alt1-1-part2" "Sequence diagram converted from fence F68 (line 1841)." {
    title "Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 1 of 6: completed, or an already completed turn enriched, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "REST 200 turnId, agent, sessionId, requestId, status completed, MCP success true with the same fields"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-09-alt1-2" "Sequence diagram converted from fence F68 (line 1841)." {
    title "Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 2 of 6: CompletionGateMissing"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 6: CompletionGateMissing"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, session_turn_completion_gate and the missing items in order, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: supply the class or reviews, then close again"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-09-alt1-3" "Sequence diagram converted from fence F68 (line 1841)." {
    title "Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 3 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 3 of 6: InvalidIdentifier, or InvalidPayload"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-09-alt1-3-part2" "Sequence diagram converted from fence F68 (line 1841)." {
    title "Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 3 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) (turn context, attribution, or QBAgent compliance)"
    qbrain.server.api -> qbrain.libs.client "REST 400 problem Invalid turn payload., MCP the classified validation_error object"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-09-alt1-4" "Sequence diagram converted from fence F68 (line 1841)." {
    title "Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 4 of 6: RequestClassInvalid"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 4 of 6: RequestClassInvalid"
    qbrain.server.api -> qbrain.libs.client "400 request_class_invalid"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-09-alt1-5" "Sequence diagram converted from fence F68 (line 1841)." {
    title "Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 5 of 6: SessionNotFound"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 5 of 6: SessionNotFound"
    qbrain.server.api -> qbrain.libs.client "404 not_found"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-09-alt1-6" "Sequence diagram converted from fence F68 (line 1841)." {
    title "Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 6 of 6: SerializableWrite's four attempts used"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 6 of 6: SerializableWrite's four attempts used"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: resend unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-10" "Sequence diagram converted from fence F69 (line 1874)." {
    title "Sequence: submit FailSessionTurnCommand and process each response, part 1 of 3"
    qbrain.libs.client -> qbrain.server.api "POST /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/fail (optional UnifiedRequestEntryDto with failureNote)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-10-part2" "Sequence diagram converted from fence F69 (line 1874)." {
    title "Sequence: submit FailSessionTurnCommand and process each response, part 2 of 3"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(FailSessionTurnCommand)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "Note: WorkspaceResolutionBehavior (400)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-10-part3" "Sequence diagram converted from fence F69 (line 1874)." {
    title "Sequence: submit FailSessionTurnCommand and process each response, part 3 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.mcpDbContext "SerializableWrite: TurnWritePipeline mode Fail"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-10-alt1-1" "Sequence diagram converted from fence F69 (line 1874)." {
    title "Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 1 of 6: failed, or an already failed turn enriched, part 1 of 2"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1 branch 1 of 6: failed, or an already failed turn enriched"
    qbrain.server.strSessionLogHandlers -> qbrain.server.api "turnId"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-10-alt1-1-part2" "Sequence diagram converted from fence F69 (line 1874)." {
    title "Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 1 of 6: failed, or an already failed turn enriched, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "REST 200 turnId, agent, sessionId, requestId, status failed, MCP success true with the same fields"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-10-alt1-2" "Sequence diagram converted from fence F69 (line 1874)." {
    title "Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 2 of 6: CompletionGateMissing (class missing or Unclassified on the move into failed), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 2 of 6: CompletionGateMissing (class missing or"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-10-alt1-2-part2" "Sequence diagram converted from fence F69 (line 1874)." {
    title "Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 2 of 6: CompletionGateMissing (class missing or Unclassified on the move into failed), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) Unclassified on the move into failed)"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, session_turn_completion_gate, missing requestClass, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: write the class, then fail again"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-10-alt1-3" "Sequence diagram converted from fence F69 (line 1874)." {
    title "Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 3 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 3 of 6: InvalidIdentifier, or InvalidPayload"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-10-alt1-3-part2" "Sequence diagram converted from fence F69 (line 1874)." {
    title "Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 3 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) (turn context, attribution, or QBAgent compliance)"
    qbrain.server.api -> qbrain.libs.client "REST 400 problem Invalid turn payload., MCP the classified validation_error object"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-10-alt1-4" "Sequence diagram converted from fence F69 (line 1874)." {
    title "Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 4 of 6: RequestClassInvalid"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 4 of 6: RequestClassInvalid"
    qbrain.server.api -> qbrain.libs.client "400 request_class_invalid"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-10-alt1-5" "Sequence diagram converted from fence F69 (line 1874)." {
    title "Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 5 of 6: SessionNotFound"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 5 of 6: SessionNotFound"
    qbrain.server.api -> qbrain.libs.client "404 not_found"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-10-alt1-6" "Sequence diagram converted from fence F69 (line 1874)." {
    title "Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 6 of 6: SerializableWrite's four attempts used"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 6 of 6: SerializableWrite's four attempts used"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: resend unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-11" "Sequence diagram converted from fence F70 (line 1907)." {
    title "Sequence: submit OpenSessionLogCommand and process each response, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "POST /qbrainai/sessionlog/{agent}/{sessionId}/open (optional title, model)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-11-part2" "Sequence diagram converted from fence F70 (line 1907)." {
    title "Sequence: submit OpenSessionLogCommand and process each response, part 2 of 4"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(OpenSessionLogCommand)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "Note: WorkspaceResolutionBehavior (400)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-11-part3" "Sequence diagram converted from fence F70 (line 1907)." {
    title "Sequence: submit OpenSessionLogCommand and process each response, part 3 of 4"
    qbrain.server.strSessionLogHandlers -> qbrain.server.mcpDbContext "TurnLocator: the session by workspace, agent, and sessionId"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-11-part4" "Sequence diagram converted from fence F70 (line 1907)." {
    title "Sequence: submit OpenSessionLogCommand and process each response, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-11-alt1-1" "Sequence diagram converted from fence F70 (line 1907)." {
    title "Sequence: submit OpenSessionLogCommand and process each response, alt 1 branch 1 of 4: session found"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 4: session found"
    qbrain.server.api -> qbrain.libs.client "REST 200 created false, MCP success true and created false: nothing changed"
    qbrain.libs.client -> qbrain.libs.client "Note: confirmed: the open is idempotent"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-11-alt1-2" "Sequence diagram converted from fence F70 (line 1907)." {
    title "Sequence: submit OpenSessionLogCommand and process each response, alt 1 branch 2 of 4: SessionNotFound, part 1 of 2"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1 branch 2 of 4: SessionNotFound"
    qbrain.server.strSessionLogHandlers -> qbrain.server.mcpDbContext "create the session in_progress with the optional title and model"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-11-alt1-2-part2" "Sequence diagram converted from fence F70 (line 1907)." {
    title "Sequence: submit OpenSessionLogCommand and process each response, alt 1 branch 2 of 4: SessionNotFound, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "REST 200 agent, sessionId, created true, MCP success true with the same fields"
    qbrain.libs.client -> qbrain.libs.client "Note: confirmed"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-11-alt1-3" "Sequence diagram converted from fence F70 (line 1907)." {
    title "Sequence: submit OpenSessionLogCommand and process each response, alt 1 branch 3 of 4: InvalidIdentifier"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 3 of 4: InvalidIdentifier"
    qbrain.server.api -> qbrain.libs.client "REST 400 problem Invalid session identifier., MCP the classified error object"
    qbrain.libs.client -> qbrain.libs.client "Note: final: reported, never retried unchanged"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-04-11-alt1-4" "Sequence diagram converted from fence F70 (line 1907)." {
    title "Sequence: submit OpenSessionLogCommand and process each response, alt 1 branch 4 of 4: transport outage, part 1 of 2"
    qbrain.libs.client -> qbrain.libs.client "alt 1 branch 4 of 4: transport outage"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-04-11-alt1-4-part2" "Sequence diagram converted from fence F70 (line 1907)." {
    title "Sequence: submit OpenSessionLogCommand and process each response, alt 1 branch 4 of 4: transport outage, part 2 of 2"
    qbrain.libs.client -> qbrain.libs.client "Note: outage: the plugin keeps its records for the next drain, a drain stops"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, part 1 of 2"
    strHostModel -> qbrain.plugins.pluginPs "appendDialog (items, optional requestId or original pair)"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: ref process BindVerbToTurn (section 4.4)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-part2" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-alt1-1" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 1 of 3: binding refused"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 3: binding refused"
    qbrain.plugins.pluginPs -> strHostModel "request_unknown, request_ambiguous, turn_immutable, host_session_unknown, host_session_ambiguous, or turn_logging_unsupported"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-alt1-2" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 2 of 3: unsupported session, target already open when it failed closed, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 1 branch 2 of 3: unsupported session, target already open"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-alt1-2-part2" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 2 of 3: unsupported session, target already open when it failed closed, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) when it failed closed"
    qbrain.plugins.pluginPs -> strHostModel "captured as an unsupported_session failsafe record, drained once that host session ends or its cause clears"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-alt1-3" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 1 of 8"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 3 of 3: bound"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-alt1-3-part2" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 2 of 8"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 4) Note over Plugin and Controller: ref process"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-alt1-3-part3" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 3 of 8"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 4) PersistTurnWriteWithReceipt: POST"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-alt1-3-part4" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 4 of 8"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(3 of 4) /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/dialog through the"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-alt1-3-part5" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 5 of 8"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(4 of 4) REPL"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-12-alt1-3-part6" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 6 of 8"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(AppendTurnDialogCommand)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "Note: WorkspaceResolutionBehavior (400), then the items check"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-12-alt1-3-part7" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 7 of 8"
    qbrain.server.strSessionLogHandlers -> qbrain.server.mcpDbContext "TurnLocator: the session and the turn, then append after the highest ordinal and save"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-12-alt1-3-part8" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 8 of 8"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-12-alt2-1" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 1 of 4: appended, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 4: appended"
    qbrain.server.api -> qbrain.plugins.pluginPs "200 totalDialogCount"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-alt2-1-part2" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 1 of 4: appended, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "persisted true, exit 0"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-12-alt2-2" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 2 of 4: no item, or InvalidIdentifier, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 4: no item, or InvalidIdentifier"
    qbrain.server.api -> qbrain.plugins.pluginPs "400 { error }"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-alt2-2-part2" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 2 of 4: no item, or InvalidIdentifier, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "persisted false with the error, exit 1"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-12-alt2-3" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 3 of 4: SessionNotFound or TurnNotFound, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 3 of 4: SessionNotFound or TurnNotFound"
    qbrain.server.api -> qbrain.plugins.pluginPs "404"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-alt2-3-part2" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 3 of 4: SessionNotFound or TurnNotFound, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "persisted false, request_unknown, exit 1"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-12-alt2-4" "Sequence diagram converted from fence F71 (line 1937)." {
    title "Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 4 of 4: transport outage or a retryable 503"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 4 of 4: transport outage or a retryable 503"
    qbrain.plugins.pluginPs -> strHostModel "failsafe record keyed by the bound requestId: persisted false, code queued, exit 0"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, part 1 of 2"
    strHostModel -> qbrain.plugins.pluginPs "setTurnTitle (title, optional requestId)"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: ref process BindVerbToTurn (section 4.4)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-part2" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-alt1-1" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 1 of 3: binding refused"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 3: binding refused"
    qbrain.plugins.pluginPs -> strHostModel "request_unknown, request_ambiguous, turn_immutable, host_session_unknown, host_session_ambiguous, or turn_logging_unsupported"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-alt1-2" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 2 of 3: unsupported session, target already open when it failed closed, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 1 branch 2 of 3: unsupported session, target already open"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-alt1-2-part2" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 2 of 3: unsupported session, target already open when it failed closed, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) when it failed closed"
    qbrain.plugins.pluginPs -> strHostModel "captured as an unsupported_session failsafe record, drained once that host session ends or its cause clears"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-alt1-3" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 1 of 8"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 3 of 3: bound"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-alt1-3-part2" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 2 of 8"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 4) Note over Plugin and Controller: ref process"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-alt1-3-part3" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 3 of 8"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 4) PersistTurnWriteWithReceipt: POST"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-alt1-3-part4" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 4 of 8"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(3 of 4) /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/title through the"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-alt1-3-part5" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 5 of 8"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(4 of 4) REPL"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-13-alt1-3-part6" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 6 of 8"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(SetTurnTitleCommand)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "Note: WorkspaceResolutionBehavior (400)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-13-alt1-3-part7" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 7 of 8"
    qbrain.server.strSessionLogHandlers -> qbrain.server.mcpDbContext "TurnLocator: the session and the turn, then overwrite this turn's QueryTitle only and save"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-13-alt1-3-part8" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 8 of 8"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-13-alt2-1" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 1 of 4: retitled, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 4: retitled"
    qbrain.server.api -> qbrain.plugins.pluginPs "200 turnId, retitled true"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-alt2-1-part2" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 1 of 4: retitled, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "persisted true, exit 0"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-13-alt2-2" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 2 of 4: InvalidIdentifier, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 4: InvalidIdentifier"
    qbrain.server.api -> qbrain.plugins.pluginPs "400 Invalid turn identifier"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-alt2-2-part2" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 2 of 4: InvalidIdentifier, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "persisted false, exit 1"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-13-alt2-3" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 3 of 4: SessionNotFound or TurnNotFound, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 3 of 4: SessionNotFound or TurnNotFound"
    qbrain.server.api -> qbrain.plugins.pluginPs "404 not_found"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-alt2-3-part2" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 3 of 4: SessionNotFound or TurnNotFound, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "persisted false, request_unknown, exit 1"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-13-alt2-4" "Sequence diagram converted from fence F72 (line 1975)." {
    title "Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 4 of 4: transport outage or a retryable 503"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 4 of 4: transport outage or a retryable 503"
    qbrain.plugins.pluginPs -> strHostModel "failsafe record keyed by the bound requestId: persisted false, code queued, exit 0"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-14" "Sequence diagram converted from fence F73 (line 2013)." {
    title "Sequence: submit SetSessionTitleCommand and process each response, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "POST /qbrainai/sessionlog/{agent}/{sessionId}/title (title)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-14-part2" "Sequence diagram converted from fence F73 (line 2013)." {
    title "Sequence: submit SetSessionTitleCommand and process each response, part 2 of 4"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(SetSessionTitleCommand)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "Note: WorkspaceResolutionBehavior (400)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-14-part3" "Sequence diagram converted from fence F73 (line 2013)." {
    title "Sequence: submit SetSessionTitleCommand and process each response, part 3 of 4"
    qbrain.server.strSessionLogHandlers -> qbrain.server.mcpDbContext "TurnLocator: the session, then overwrite its Title and save"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-14-part4" "Sequence diagram converted from fence F73 (line 2013)." {
    title "Sequence: submit SetSessionTitleCommand and process each response, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-14-alt1-1" "Sequence diagram converted from fence F73 (line 2013)." {
    title "Sequence: submit SetSessionTitleCommand and process each response, alt 1 branch 1 of 3: retitled"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 3: retitled"
    qbrain.server.api -> qbrain.libs.client "200 turnId (the session row id), retitled true"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-14-alt1-2" "Sequence diagram converted from fence F73 (line 2013)." {
    title "Sequence: submit SetSessionTitleCommand and process each response, alt 1 branch 2 of 3: InvalidIdentifier"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 3: InvalidIdentifier"
    qbrain.server.api -> qbrain.libs.client "400 Invalid session identifier"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-04-14-alt1-3" "Sequence diagram converted from fence F73 (line 2013)." {
    title "Sequence: submit SetSessionTitleCommand and process each response, alt 1 branch 3 of 3: SessionNotFound"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 3 of 3: SessionNotFound"
    qbrain.server.api -> qbrain.libs.client "404 not_found"
    qbrain.libs.client -> qbrain.libs.client "Note: open the session first (OpenSessionLogCommand)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-15" "Sequence diagram converted from fence F74 (line 2038)." {
    title "Sequence: replay the failsafe (process DrainFailsafe) and process each response, part 1 of 2"
    qbrain.plugins.pluginHosts -> qbrain.plugins.pluginPs "drain"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) loop 1: records oldest first, until a retryable answer stops"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-15-part2" "Sequence diagram converted from fence F74 (line 2038)." {
    title "Sequence: replay the failsafe (process DrainFailsafe) and process each response, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) the walk (next view)"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginHosts "drain complete, or stopped with the rest queued"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-15-loop1" "Sequence diagram converted from fence F74 (line 2038)." {
    title "Sequence: replay the failsafe (process DrainFailsafe) and process each response, loop 1: records oldest first, until a retryable answer stops the walk, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "loop 1: records oldest first, until a retryable answer stops the walk"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-04-15-loop1-part2" "Sequence diagram converted from fence F74 (line 2038)." {
    title "Sequence: replay the failsafe (process DrainFailsafe) and process each response, loop 1: records oldest first, until a retryable answer stops the walk, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: ref nested sequence ReplayOneFailsafeRecord (below)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence)"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-1" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 1 of 6: malformed record, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 6: malformed record"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-1-part2" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 1 of 6: malformed record, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: quarantined with its .reason.txt"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-2" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 2 of 6: its turn unresolved earlier in this walk, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 2 of 6: its turn unresolved earlier in this walk"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-2-part2" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 2 of 6: its turn unresolved earlier in this walk, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: skipped, no attempt counted"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-3" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 3 of 6: drainAttempts at the budget of 5, record not repaired, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 3 of 6: drainAttempts at the budget of 5, record not repaired"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-3-part2" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 3 of 6: drainAttempts at the budget of 5, record not repaired, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: quarantined with its .reason.txt"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-4" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 4 of 6: legacy record that names no request, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 4 of 6: legacy record that names no request"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-4-part2" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 4 of 6: legacy record that names no request, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: quarantined with its .reason.txt"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-5" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 5 of 6: unsupported_session record of a live host session, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 5 of 6: unsupported_session record of a live host session"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-5-part2" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 5 of 6: unsupported_session record of a live host session, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: not drained during that host session"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-6" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 6 of 6: legacy record naming its request, or a bound record, part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 6 of 6: legacy record naming its request, or a bound record"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-6-part2" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 6 of 6: legacy record naming its request, or a bound record, part 2 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: ref nested sequence SendDrainedRecord (below)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt1-6-part3" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 6 of 6: legacy record naming its request, or a bound record, part 3 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt2-1" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 1 of 4: retryable answer, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 1 of 4: retryable answer"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt2-1-part2" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 1 of 4: retryable answer, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: walk stopped, no attempt counted, the rest queued"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt2-2" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 2 of 4: confirmed answer, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 2 of 4: confirmed answer"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt2-2-part2" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 2 of 4: confirmed answer, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: record removed, outcome reported"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt2-3" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 3 of 4: final answer, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 3 of 4: final answer"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt2-3-part2" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 3 of 4: final answer, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: drainAttempts plus 1, its turn unresolved in this walk"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt2-4" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 4 of 4: not replayable, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 4 of 4: not replayable"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ReplayOneFailsafeRecord-alt2-4-part2" "Sequence diagram converted from fence F75 (line 2054)." {
    title "Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 4 of 4: not replayable, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: quarantined with its .reason.txt"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord)"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt1-1" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 1 of 3: legacy record naming its request, part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 3: legacy record naming its request"
    qbrain.plugins.pluginPs -> qbrain.repl "its captured method and params, as today"
    autolayout lr 300 100
}
dynamic qbrain.server "SendDrainedRecord-alt1-1-part2" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 1 of 3: legacy record naming its request, part 2 of 3"
    qbrain.repl -> qbrain.server.api "the captured route"
    qbrain.server.api -> qbrain.repl "answer"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt1-1-part3" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 1 of 3: legacy record naming its request, part 3 of 3"
    qbrain.repl -> qbrain.plugins.pluginPs "result, or a transport outage"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt1-2" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 2 of 3: write bound to a pending id whose mint is quarantined, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 2 of 3: write bound to a pending id whose mint is quarantined"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt1-2-part2" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 2 of 3: write bound to a pending id whose mint is quarantined, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: not replayable"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt1-3" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 3 of 3: bound record, part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 3 of 3: bound record"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt1-3-part2" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 3 of 3: bound record, part 2 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Invoke-SessionOpen (envelope mcpSessionId)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt1-3-part3" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 3 of 3: bound record, part 3 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt2-1" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 1 of 4: outage, or final (invalid session identifier), part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 1 of 4: outage, or final (invalid session identifier)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt2-1-part2" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 1 of 4: outage, or final (invalid session identifier), part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: retryable, or final"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt2-2" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 2 of 4: mint record, part 1 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 2 of 4: mint record"
    qbrain.plugins.pluginPs -> qbrain.repl "workflow.sessionlog.mintTurn with the original nonce"
    autolayout lr 300 100
}
dynamic qbrain.server "SendDrainedRecord-alt2-2-part2" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 2 of 4: mint record, part 2 of 4"
    qbrain.repl -> qbrain.server.api "POST .../mint (sequence MintSessionTurnCommand)"
    qbrain.server.api -> qbrain.repl "answer"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt2-2-part3" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 2 of 4: mint record, part 3 of 4"
    qbrain.repl -> qbrain.plugins.pluginPs "result, or a transport outage"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Resolve-MintOutcome (stored retry)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt2-2-part4" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 2 of 4: mint record, part 4 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "opt 1: a turn returned (next view)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-opt1" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), opt 1: a turn returned, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "opt 1: a turn returned"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-opt1-part2" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), opt 1: a turn returned, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "journal the alias, rewrite, deliver once, remove"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt2-3" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 3 of 4: unsupported_session record, turn no longer in progress, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 3 of 4: unsupported_session record, turn no longer in progress"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt2-3-part2" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 3 of 4: unsupported_session record, turn no longer in progress, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: not replayable (status read by the original pair)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt2-4" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 4 of 4: write, close, or unsupported_session record in progress, part 1 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 2 branch 4 of 4: write, close, or unsupported_session record in"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt2-4-part2" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 4 of 4: write, close, or unsupported_session record in progress, part 2 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) progress"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt2-4-part3" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 4 of 4: write, close, or unsupported_session record in progress, part 3 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) Note over Plugin and Controller: ref PersistTurnWriteWithReceipt:"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SendDrainedRecord-alt2-4-part4" "Sequence diagram converted from fence F76 (line 2086)." {
    title "Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 4 of 4: write, close, or unsupported_session record in progress, part 4 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) alias, bound id, or pair"
    autolayout lr 300 100
}
custom "STR-05-01" "Review status lifecycle (state diagram), part 1 of 5" "State diagram converted from fence F77 (line 2134)." {
    include strSTR0501_Start strSTR0501_Unstarted strSTR0501_Unnecessary
    autolayout tb 200 300
}
custom "STR-05-01-part2" "Review status lifecycle (state diagram), part 2 of 5" "State diagram converted from fence F77 (line 2134)." {
    include strSTR0501_Unstarted strSTR0501_Written strSTR0501_Unnecessary
    exclude "strSTR0501_Written -> strSTR0501_Written"
    autolayout tb 200 300
}
custom "STR-05-01-part3" "Review status lifecycle (state diagram), part 3 of 5" "State diagram converted from fence F77 (line 2134)." {
    include strSTR0501_Written strSTR0501_Running strSTR0501_Pass
    exclude "strSTR0501_Written -> strSTR0501_Written"
    autolayout lr 300 300
}
custom "STR-05-01-part4" "Review status lifecycle (state diagram), part 4 of 5" "State diagram converted from fence F77 (line 2134)." {
    include strSTR0501_Written strSTR0501_Fail strSTR0501_Other
    exclude "strSTR0501_Written -> strSTR0501_Written"
    autolayout lr 300 300
}
custom "STR-05-01-part5" "Review status lifecycle (state diagram), part 5 of 5" "State diagram converted from fence F77 (line 2134)." {
    include strSTR0501_Written
    autolayout lr 300 300
}
custom "RecordSessionTurnReviewCommand" "RecordSessionTurnReviewCommand: record a review (activity), part 1 of 8" "Activity diagram converted from fence F78 (line 2168)." {
    include strRecordSessionTurnReviewCommand_ReviewWriteOnASurface strRecordSessionTurnReviewCommand_SectionOrKindAgentReviewHostileReviewOr strRecordSessionTurnReviewCommand_400ValidationError strRecordSessionTurnReviewCommand_ScoreLexemeJsonConverterScoreLexemesAtDeserialization
    autolayout tb 100 100
}
custom "RecordSessionTurnReviewCommand-part2" "RecordSessionTurnReviewCommand: record a review (activity), part 2 of 8" "Activity diagram converted from fence F78 (line 2168)." {
    include strRecordSessionTurnReviewCommand_ScoreLexemeJsonConverterScoreLexemesAtDeserialization strRecordSessionTurnReviewCommand_400ReviewScoreFormatReviewScore strRecordSessionTurnReviewCommand_HvVerdictSupplied
    autolayout lr 250 150
}
custom "RecordSessionTurnReviewCommand-part3" "RecordSessionTurnReviewCommand: record a review (activity), part 3 of 8" "Activity diagram converted from fence F78 (line 2168)." {
    include strRecordSessionTurnReviewCommand_HvVerdictSupplied strRecordSessionTurnReviewCommand_400ValidationErrorDetailsFieldHvVerdict strRecordSessionTurnReviewCommand_400ValidationErrorDetailsFieldStatus strRecordSessionTurnReviewCommand_PolicyHvVerdictMapping
    autolayout lr 250 150
}
custom "RecordSessionTurnReviewCommand-part4" "RecordSessionTurnReviewCommand: record a review (activity), part 4 of 8" "Activity diagram converted from fence F78 (line 2168)." {
    include strRecordSessionTurnReviewCommand_HvVerdictSupplied strRecordSessionTurnReviewCommand_PolicyReviewValidationOriginAgent
    autolayout lr 250 150
}
custom "RecordSessionTurnReviewCommand-part5" "RecordSessionTurnReviewCommand: record a review (activity), part 5 of 8" "Activity diagram converted from fence F78 (line 2168)." {
    include strRecordSessionTurnReviewCommand_PolicyHvVerdictMapping strRecordSessionTurnReviewCommand_400ReviewInvalidStatusOrReview strRecordSessionTurnReviewCommand_PolicyReviewValidationOriginAgent strRecordSessionTurnReviewCommand_400WithTheReviewCodeOr strRecordSessionTurnReviewCommand_SerializableWriteInReplaceTurnSectionAsync
    autolayout tb 100 100
}
custom "RecordSessionTurnReviewCommand-part6" "RecordSessionTurnReviewCommand: record a review (activity), part 6 of 8" "Activity diagram converted from fence F78 (line 2168)." {
    include strRecordSessionTurnReviewCommand_SerializableWriteInReplaceTurnSectionAsync strRecordSessionTurnReviewCommand_TurnLocatorAgentSessionIdRequestId strRecordSessionTurnReviewCommand_409ConflictRetryableTrueNothingSaved
    autolayout lr 250 150
}
custom "RecordSessionTurnReviewCommand-part7" "RecordSessionTurnReviewCommand: record a review (activity), part 7 of 8" "Activity diagram converted from fence F78 (line 2168)." {
    include strRecordSessionTurnReviewCommand_TurnLocatorAgentSessionIdRequestId strRecordSessionTurnReviewCommand_400ValidationError strRecordSessionTurnReviewCommand_404NotFound strRecordSessionTurnReviewCommand_ReplaceTheStoredRecordTextAs
    autolayout lr 250 150
}
custom "RecordSessionTurnReviewCommand-part8" "RecordSessionTurnReviewCommand: record a review (activity), part 8 of 8" "Activity diagram converted from fence F78 (line 2168)." {
    include strRecordSessionTurnReviewCommand_ReplaceTheStoredRecordTextAs strRecordSessionTurnReviewCommand_TurnReadProjection strRecordSessionTurnReviewCommand_200ReviewRecordDerivedStatusStamped
    autolayout tb 100 200
}
custom "ResetSessionTurnReviewCommand" "ResetSessionTurnReviewCommand: reset a review section (activity), part 1 of 5" "Activity diagram converted from fence F79 (line 2199)." {
    include strResetSessionTurnReviewCommand_DELETEOnATurnSection strResetSessionTurnReviewCommand_SectionAgentReviewHostileReviewOrAnAlias strResetSessionTurnReviewCommand_400ProblemInvalidSectionOrIdentifier strResetSessionTurnReviewCommand_SerializableWriteInClearTurnSectionAsync
    autolayout tb 100 100
}
custom "ResetSessionTurnReviewCommand-part2" "ResetSessionTurnReviewCommand: reset a review section (activity), part 2 of 5" "Activity diagram converted from fence F79 (line 2199)." {
    include strResetSessionTurnReviewCommand_SerializableWriteInClearTurnSectionAsync strResetSessionTurnReviewCommand_TurnLocatorAgentSessionIdRequestId strResetSessionTurnReviewCommand_409ConflictRetryableTrueNothingSaved
    autolayout lr 250 150
}
custom "ResetSessionTurnReviewCommand-part3" "ResetSessionTurnReviewCommand: reset a review section (activity), part 3 of 5" "Activity diagram converted from fence F79 (line 2199)." {
    include strResetSessionTurnReviewCommand_TurnLocatorAgentSessionIdRequestId strResetSessionTurnReviewCommand_400ProblemInvalidSectionOrIdentifier strResetSessionTurnReviewCommand_404NotFound strResetSessionTurnReviewCommand_TurnCompletedOrFailed
    autolayout lr 250 150
}
custom "ResetSessionTurnReviewCommand-part4" "ResetSessionTurnReviewCommand: reset a review section (activity), part 4 of 5" "Activity diagram converted from fence F79 (line 2199)." {
    include strResetSessionTurnReviewCommand_TurnCompletedOrFailed strResetSessionTurnReviewCommand_400ReviewSectionResetOnTerminal strResetSessionTurnReviewCommand_ReviewDefaultsKindCurrentClass strResetSessionTurnReviewCommand_SaveTheDefaultRecord
    autolayout tb 100 100
}
custom "ResetSessionTurnReviewCommand-part5" "ResetSessionTurnReviewCommand: reset a review section (activity), part 5 of 5" "Activity diagram converted from fence F79 (line 2199)." {
    include strResetSessionTurnReviewCommand_SaveTheDefaultRecord strResetSessionTurnReviewCommand_TurnReadProjection strResetSessionTurnReviewCommand_200ResetRecord
    autolayout tb 100 200
}
custom "GetSessionLogQuery" "GetSessionLogQuery: read one session with its turns and reviews (activity), part 1 of 3" "Activity diagram converted from fence F80 (line 2223)." {
    include strGetSessionLogQuery_GetSessionLogQueryAgentSessionId strGetSessionLogQuery_TurnLocatorSessionOnly
    autolayout lr 250 150
}
custom "GetSessionLogQuery-part2" "GetSessionLogQuery: read one session with its turns and reviews (activity), part 2 of 3" "Activity diagram converted from fence F80 (line 2223)." {
    include strGetSessionLogQuery_TurnLocatorSessionOnly strGetSessionLogQuery_400ProblemInvalidSessionIdentifier strGetSessionLogQuery_404NoBody strGetSessionLogQuery_LoadTurnsWithReviewColumnsAnd
    autolayout lr 250 150
}
custom "GetSessionLogQuery-part3" "GetSessionLogQuery: read one session with its turns and reviews (activity), part 3 of 3" "Activity diagram converted from fence F80 (line 2223)." {
    include strGetSessionLogQuery_LoadTurnsWithReviewColumnsAnd strGetSessionLogQuery_TurnReadProjection strGetSessionLogQuery_200TurnsWithRequestClassAgentReviewHostileReview
    autolayout tb 100 200
}
custom "QuerySessionLogsQuery" "QuerySessionLogsQuery: query sessions and turns with their reviews (activity), part 1 of 2" "Activity diagram converted from fence F81 (line 2242)." {
    include strQuerySessionLogsQuery_QuerySessionLogsQueryFiltersLimitOffset strQuerySessionLogsQuery_SessionLogSchemaCurrent strQuerySessionLogsQuery_503PersistenceErrorPendingMigration strQuerySessionLogsQuery_PagedQueryPageBoundsClampWorkspaceAndThe
    autolayout tb 100 100
}
custom "QuerySessionLogsQuery-part2" "QuerySessionLogsQuery: query sessions and turns with their reviews (activity), part 2 of 2" "Activity diagram converted from fence F81 (line 2242)." {
    include strQuerySessionLogsQuery_PagedQueryPageBoundsClampWorkspaceAndThe strQuerySessionLogsQuery_TurnReadProjectionForEachTurn strQuerySessionLogsQuery_200TotalCountLimitOffsetItems
    autolayout tb 100 200
}
custom "RequestClassification" "Policy RequestClassification: request classification and the Chore flip (activity), part 1 of 3" "Activity diagram converted from fence F82 (line 2262)." {
    include strRequestClassification_WriteCarriesRequestClass strRequestClassification_NormalizeCaseInsensitively strRequestClassification_CodeDocsOrChore
    autolayout tb 100 200
}
custom "RequestClassification-part2" "Policy RequestClassification: request classification and the Chore flip (activity), part 2 of 3" "Activity diagram converted from fence F82 (line 2262)." {
    include strRequestClassification_CodeDocsOrChore strRequestClassification_400RequestClassInvalidNothingChanged strRequestClassification_StoreTheCanonicalPascalCaseClass strRequestClassification_TurnNonterminalBeforeThisWriteBy
    autolayout tb 100 100
}
custom "RequestClassification-part3" "Policy RequestClassification: request classification and the Chore flip (activity), part 3 of 3" "Activity diagram converted from fence F82 (line 2262)." {
    include strRequestClassification_TurnNonterminalBeforeThisWriteBy strRequestClassification_ClassStoredNoReviewChangeCompletionGate strRequestClassification_ReviewDefaultsAHostileRecordStillAt strRequestClassification_ClassStoredWithTheHostileDefault
    autolayout tb 100 100
}
custom "ReviewValidation" "Policy ReviewValidation: validate a review record and derive its status (activity), part 1 of 5" "Activity diagram converted from fence F83 (line 2281)." {
    include strReviewValidation_ReviewValidationKindInputWriteOrigin strReviewValidation_StatusSupplied
    autolayout lr 250 150
}
custom "ReviewValidation-part2" "Policy ReviewValidation: validate a review record and derive its status (activity), part 2 of 5" "Activity diagram converted from fence F83 (line 2281)." {
    include strReviewValidation_StatusSupplied strReviewValidation_400ReviewUnnecessaryNotClientWritable strReviewValidation_400ReviewInvalidStatus strReviewValidation_BothScoresPresent strReviewValidation_PerStatusFieldsValid
    autolayout lr 200 50
}
custom "ReviewValidation-part3" "Policy ReviewValidation: validate a review record and derive its status (activity), part 3 of 5" "Activity diagram converted from fence F83 (line 2281)." {
    include strReviewValidation_BothScoresPresent strReviewValidation_400ReviewFieldRequiredForStatus strReviewValidation_ImportOrReplicationOriginWithA
    autolayout lr 250 150
}
custom "ReviewValidation-part4" "Policy ReviewValidation: validate a review record and derive its status (activity), part 4 of 5" "Activity diagram converted from fence F83 (line 2281)." {
    include strReviewValidation_PerStatusFieldsValid strReviewValidation_400WithTheFieldCodeIn strReviewValidation_RecordValidStatusAndTheThresholds
    autolayout lr 250 150
}
custom "ReviewValidation-part5" "Policy ReviewValidation: validate a review record and derive its status (activity), part 5 of 5" "Activity diagram converted from fence F83 (line 2281)." {
    include strReviewValidation_ImportOrReplicationOriginWithA strReviewValidation_PerStatusFieldsValid strReviewValidation_ScoreVerdictThresholdsInForceForThe strReviewValidation_422ReviewStatusConflict
    autolayout tb 100 100
}
custom "HvVerdictMapping" "Policy HvVerdictMapping: HV verdict mapping (server) (activity), part 1 of 3" "Activity diagram converted from fence F84 (line 2306)." {
    include strHvVerdictMapping_HvVerdictMappingHvVerdictScoresReasonsResponseJsonlPath strHvVerdictMapping_HvVerdictAGREEOrDISAGREE strHvVerdictMapping_400ReviewInvalidStatusDetailsField strHvVerdictMapping_BothScoresPresent
    autolayout tb 100 100
}
custom "HvVerdictMapping-part2" "Policy HvVerdictMapping: HV verdict mapping (server) (activity), part 2 of 3" "Activity diagram converted from fence F84 (line 2306)." {
    include strHvVerdictMapping_BothScoresPresent strHvVerdictMapping_400ReviewFieldRequiredForStatus strHvVerdictMapping_ScoreVerdictHostileThresholdsInForce strHvVerdictMapping_AGREEAndNoReasonStartsWith
    autolayout tb 100 100
}
custom "HvVerdictMapping-part3" "Policy HvVerdictMapping: HV verdict mapping (server) (activity), part 3 of 3" "Activity diagram converted from fence F84 (line 2306)." {
    include strHvVerdictMapping_AGREEAndNoReasonStartsWith strHvVerdictMapping_StatusDerivedPassOrFail strHvVerdictMapping_DerivedPass strHvVerdictMapping_StatusFail strHvVerdictMapping_StatusOtherOtherNotesFAILClaimsElse
    autolayout tb 100 100
}
custom "CompletionPolicy" "Policy CompletionPolicy: the completion rule (activity), part 1 of 3" "Activity diagram converted from fence F85 (line 2325)." {
    include strCompletionPolicy_CompletionPolicyTargetStatusRequestClassReviewStatuses strCompletionPolicy_RequestClassPresentAndNotUnclassified strCompletionPolicy_MissingRequestClass strCompletionPolicy_TargetCompleted
    autolayout tb 100 200
}
custom "CompletionPolicy-part2" "Policy CompletionPolicy: the completion rule (activity), part 2 of 3" "Activity diagram converted from fence F85 (line 2325)." {
    include strCompletionPolicy_TargetCompleted strCompletionPolicy_MissingItemsInOrderRequestClassAgentReview strCompletionPolicy_AgentReviewPassFailOrOther strCompletionPolicy_MissingAgentReview strCompletionPolicy_ClassChore
    autolayout tb 100 200
}
custom "CompletionPolicy-part3" "Policy CompletionPolicy: the completion rule (activity), part 3 of 3" "Activity diagram converted from fence F85 (line 2325)." {
    include strCompletionPolicy_ClassChore strCompletionPolicy_MissingItemsInOrderRequestClassAgentReview strCompletionPolicy_HostileReviewPassFailOrOther strCompletionPolicy_MissingHostileReview
    autolayout tb 100 200
}
custom "CompletionGate" "Policy CompletionGate: server enforcement of the completion rule (activity), part 1 of 2" "Activity diagram converted from fence F86 (line 2353)." {
    include strCompletionGate_CompletionGateTheTurnAsThisWrite strCompletionGate_WriteMovesTheTurnIntoCompleted strCompletionGate_NotGatedNoItems strCompletionGate_OriginServerInternalReplicationOrImport
    autolayout tb 100 200
}
custom "CompletionGate-part2" "Policy CompletionGate: server enforcement of the completion rule (activity), part 2 of 2" "Activity diagram converted from fence F86 (line 2353)." {
    include strCompletionGate_OriginServerInternalReplicationOrImport strCompletionGate_NotGatedNoItems strCompletionGate_PolicyCompletionPolicy strCompletionGate_MissingItemsReturnedTheCallerRejects
    autolayout tb 100 100
}
custom "ConvertHvReceiptToReview" "Process ConvertHvReceiptToReview: convert a hostile-validation receipt into a review write (plugin), then policy HvVerdictMapping (activity), part 1 of 2" "Activity diagram converted from fence F87 (line 2373)." {
    include strConvertHvReceiptToReview_HvReceiptPathAndTheResponseJsonl strConvertHvReceiptToReview_ReceiptValidFRMCPSESSIONREVIEW005 strConvertHvReceiptToReview_RefusedBeforeAnyWriteReviewInvalid strConvertHvReceiptToReview_MapScoresAccuracy100AndCompleteness
    autolayout tb 100 100
}
custom "ConvertHvReceiptToReview-part2" "Process ConvertHvReceiptToReview: convert a hostile-validation receipt into a review write (plugin), then policy HvVerdictMapping (activity), part 2 of 2" "Activity diagram converted from fence F87 (line 2373)." {
    include strConvertHvReceiptToReview_MapScoresAccuracy100AndCompleteness strConvertHvReceiptToReview_OverallVerdictAGREEAndEveryClaimPASS strConvertHvReceiptToReview_RecordReviewRecordSessionTurnReviewCommandWithPolicyHvVerdictMapping strConvertHvReceiptToReview_StoredStatusPerPolicyHvVerdictMappingOr
    autolayout tb 100 200
}
custom "ClearSession" "Process ClearSession: clear a session (clear-session coordinator) (activity)" "Activity diagram converted from fence F88 (line 2391)." {
    include strClearSession_ClearSessionRun strClearSession_EnumerateTheHostSessionSOpen strClearSession_ForEachOpenRequestTurnNested strClearSession_ReportEachTurnClosedClosedElsewhere
    autolayout tb 100 200
}
custom "ClearOneOpenTurn" "Nested activity: ClearOneOpenTurn (loop body of ClearSession) (activity), part 1 of 4" "Activity diagram converted from fence F89 (line 2402)." {
    include strClearOneOpenTurn_NextOpenTurnByItsOriginal strClearOneOpenTurn_ServerReportsItTerminal strClearOneOpenTurn_ReportClosedElsewhereNothingSent strClearOneOpenTurn_WithdrawnOrMintedAndStillQueued
    autolayout tb 100 100
}
custom "ClearOneOpenTurn-part2" "Nested activity: ClearOneOpenTurn (loop body of ClearSession) (activity), part 2 of 4" "Activity diagram converted from fence F89 (line 2402)." {
    include strClearOneOpenTurn_ReportClosedElsewhereNothingSent strClearOneOpenTurn_TurnHandledNextTurn strClearOneOpenTurn_WithdrawnOrMintedAndStillQueued strClearOneOpenTurn_GetCloseRemedyForTheChosenClose strClearOneOpenTurn_NonblankModelAuthoredBlocker
    autolayout tb 100 100
}
custom "ClearOneOpenTurn-part3" "Nested activity: ClearOneOpenTurn (loop body of ClearSession) (activity), part 3 of 4" "Activity diagram converted from fence F89 (line 2402)." {
    include strClearOneOpenTurn_GetCloseRemedyForTheChosenClose strClearOneOpenTurn_LeaveInProgressReportMissingItems strClearOneOpenTurn_BindVerbToTurnThenPersistTurnWriteWithReceipt strClearOneOpenTurn_NonblankModelAuthoredBlocker
    autolayout tb 100 100
}
custom "ClearOneOpenTurn-part4" "Nested activity: ClearOneOpenTurn (loop body of ClearSession) (activity), part 4 of 4" "Activity diagram converted from fence F89 (line 2402)." {
    include strClearOneOpenTurn_LeaveInProgressReportMissingItems strClearOneOpenTurn_TurnHandledNextTurn strClearOneOpenTurn_BindVerbToTurnThenPersistTurnWriteWithReceipt strClearOneOpenTurn_ReportCloseReceipt
    autolayout tb 100 100
}
custom "ReportCloseReceipt" "Nested activity: ReportCloseReceipt (receipt step of ClearOneOpenTurn) (activity), part 1 of 3" "Activity diagram converted from fence F90 (line 2423)." {
    include strReportCloseReceipt_CloseReceipt strReportCloseReceipt_ReceiptResult
    autolayout lr 250 150
}
custom "ReportCloseReceipt-part2" "Nested activity: ReportCloseReceipt (receipt step of ClearOneOpenTurn) (activity), part 2 of 3" "Activity diagram converted from fence F90 (line 2423)." {
    include strReportCloseReceipt_ReceiptResult strReportCloseReceipt_Closed strReportCloseReceipt_ClosedElsewhere strReportCloseReceipt_UnverifiedRerun strReportCloseReceipt_QueuedDrainedLater
    autolayout lr 200 50
}
custom "ReportCloseReceipt-part3" "Nested activity: ReportCloseReceipt (receipt step of ClearOneOpenTurn) (activity), part 3 of 3" "Activity diagram converted from fence F90 (line 2423)." {
    include strReportCloseReceipt_ReceiptResult strReportCloseReceipt_CapturedNextDrain strReportCloseReceipt_LeftInProgressCode
    autolayout lr 250 150
}
custom "AdvisoryCompletionPrecheck" "Process AdvisoryCompletionPrecheck: advisory completion pre-check (plugins, REPL, McpAgent) (activity)" "Activity diagram converted from fence F91 (line 2439)." {
    include strAdvisoryCompletionPrecheck_CloseRequestedCompleteTurnOrFailTurn strAdvisoryCompletionPrecheck_ReadTheTurnSClassAnd strAdvisoryCompletionPrecheck_PluginGetCloseRemedyAdvisoryREPLAnd strAdvisoryCompletionPrecheck_WarnWithTheExactGateMessage strAdvisoryCompletionPrecheck_DispatchTheCloseCompletionGateDecides
    autolayout tb 100 200
}
custom "PreserveReviewScoreLexemes" "Process PreserveReviewScoreLexemes: lossless score handling in the REPL (activity), part 1 of 2" "Activity diagram converted from fence F92 (line 2453)." {
    include strPreserveReviewScoreLexemes_NumberTokenInAREPLPayload strPreserveReviewScoreLexemes_CorrectnessOrCompletenessInsideAgentReviewHostileReview strPreserveReviewScoreLexemes_ExistingHandlingUnchanged strPreserveReviewScoreLexemes_ScoreLexemeOnTheRawLexemeJsonElement
    autolayout tb 100 100
}
custom "PreserveReviewScoreLexemes-part2" "Process PreserveReviewScoreLexemes: lossless score handling in the REPL (activity), part 2 of 2" "Activity diagram converted from fence F92 (line 2453)." {
    include strPreserveReviewScoreLexemes_ScoreLexemeOnTheRawLexemeJsonElement strPreserveReviewScoreLexemes_ErrorReviewScoreFormatReviewScore strPreserveReviewScoreLexemes_ForwardTheLexemeUnchangedAsA
    autolayout lr 250 150
}
dynamic qbrain.plugins "STR-05-02" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, part 1 of 2"
    strHostModel -> qbrain.plugins.pluginPs "recordReview (kind, input or hvReceiptPath)"
    strHostModel -> strHostModel "(1 of 2) Note over Model or coordinator and Plugin recordReview: ref process"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-02-part2" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, part 2 of 2"
    strHostModel -> strHostModel "(2 of 2) BindVerbToTurn, no failsafe"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 5 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-02-alt1-1" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 1 branch 1 of 5: binding refused"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 5: binding refused"
    qbrain.plugins.pluginPs -> strHostModel "refused with the binding code"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-02-alt1-2" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 1 branch 2 of 5: turn_logging_unsupported"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 2 of 5: turn_logging_unsupported"
    qbrain.plugins.pluginPs -> strHostModel "refused, recorded after the drain"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-02-alt1-3" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 1 branch 3 of 5: receipt invalid (ConvertHvReceiptToReview)"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 3 of 5: receipt invalid (ConvertHvReceiptToReview)"
    qbrain.plugins.pluginPs -> strHostModel "refused before any write"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-02-alt1-4" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 1 branch 4 of 5: score lexeme invalid (Protect-ReplLosslessScalars)"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 4 of 5: score lexeme invalid (Protect-ReplLosslessScalars)"
    qbrain.plugins.pluginPs -> strHostModel "refused with the score code"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-02-alt1-5" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 1 branch 5 of 5: bound, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 5 of 5: bound"
    qbrain.plugins.pluginPs -> qbrain.repl "recordReview, lexemes unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-02-alt1-5-part2" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 1 branch 5 of 5: bound, part 2 of 2"
    qbrain.repl -> qbrain.server.api "PUT .../sections/{kind}"
    qbrain.repl -> qbrain.repl "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-02-alt2-1" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 2 branch 1 of 2: transport outage or retryable 503, part 1 of 2"
    qbrain.repl -> qbrain.repl "alt 2 branch 1 of 2: transport outage or retryable 503"
    qbrain.repl -> qbrain.plugins.pluginPs "retryable answer"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-02-alt2-1-part2" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 2 branch 1 of 2: transport outage or retryable 503, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "code, retryable, not queued, rerun after drain"
    autolayout lr 300 100
}
dynamic * "STR-05-02-alt2-2" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 2 branch 2 of 2: answered, part 1 of 3"
    strHostModel -> strHostModel "alt 2 branch 2 of 2: answered"
    autolayout lr 300 100
}
dynamic * "STR-05-02-alt2-2-part2" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 2 branch 2 of 2: answered, part 2 of 3"
    strHostModel -> strHostModel "(1 of 2) Note over Model or coordinator and Controller or MCP tool: ref nested"
    autolayout lr 300 100
}
dynamic * "STR-05-02-alt2-2-part3" "Sequence diagram converted from fence F93 (line 2468)." {
    title "Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 2 branch 2 of 2: answered, part 3 of 3"
    strHostModel -> strHostModel "(2 of 2) sequence ServeReviewWrite"
    autolayout lr 300 100
}
dynamic qbrain.server "ServeReviewWrite" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence)"
    qbrain.plugins.pluginPs -> qbrain.server.api "PUT .../sections/{kind}"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ServeReviewWrite-alt1-1" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 1 branch 1 of 2: unknown section, or score invalid at deserialization, part 1 of 3"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 1 of 2: unknown section, or score invalid at"
    autolayout lr 300 100
}
dynamic qbrain.server "ServeReviewWrite-alt1-1-part2" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 1 branch 1 of 2: unknown section, or score invalid at deserialization, part 2 of 3"
    qbrain.server.api -> qbrain.server.api "(2 of 2) deserialization"
    qbrain.server.api -> qbrain.plugins.pluginPs "400 validation_error or score code"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ServeReviewWrite-alt1-1-part3" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 1 branch 1 of 2: unknown section, or score invalid at deserialization, part 3 of 3"
    qbrain.plugins.pluginPs -> strHostModel "code surfaced, nothing changed"
    autolayout lr 300 100
}
dynamic qbrain.server "ServeReviewWrite-alt1-2" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 1 branch 2 of 2: body bound, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 2: body bound"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(RecordSessionTurnReviewCommand)"
    autolayout lr 300 100
}
dynamic qbrain.server "ServeReviewWrite-alt1-2-part2" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 1 branch 2 of 2: body bound, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ServeReviewWrite-alt2-1" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 1 of 6: workspace unresolved (WorkspaceResolutionBehavior), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 6: workspace unresolved (WorkspaceResolutionBehavior)"
    qbrain.server.api -> qbrain.plugins.pluginPs "400 validation_error, workspacePath"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ServeReviewWrite-alt2-1-part2" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 1 of 6: workspace unresolved (WorkspaceResolutionBehavior), part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "code surfaced, nothing changed"
    autolayout lr 300 100
}
dynamic qbrain.server "ServeReviewWrite-alt2-2" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 2 of 6: saved (policies, then SerializableWrite), part 1 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 2 branch 2 of 6: saved (policies, then SerializableWrite)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.api "record through TurnReadProjection"
    autolayout lr 300 100
}
dynamic qbrain.server "ServeReviewWrite-alt2-2-part2" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 2 of 6: saved (policies, then SerializableWrite), part 2 of 3"
    qbrain.server.api -> qbrain.plugins.pluginPs "200 record"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ServeReviewWrite-alt2-2-part3" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 2 of 6: saved (policies, then SerializableWrite), part 3 of 3"
    qbrain.plugins.pluginPs -> strHostModel "recorded, exact scores echoed"
    autolayout lr 300 100
}
dynamic qbrain.server "ServeReviewWrite-alt2-3" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 3 of 6: 422 review_status_conflict (ScoreVerdict), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 3 of 6: 422 review_status_conflict (ScoreVerdict)"
    qbrain.server.api -> qbrain.plugins.pluginPs "422 thresholds, derived status"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ServeReviewWrite-alt2-3-part2" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 3 of 6: 422 review_status_conflict (ScoreVerdict), part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "resend without status or derived"
    autolayout lr 300 100
}
dynamic qbrain.server "ServeReviewWrite-alt2-4" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 4 of 6: 400 review code, hvVerdict, status, or id, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 4 of 6: 400 review code, hvVerdict, status, or id"
    qbrain.server.api -> qbrain.plugins.pluginPs "400 with the code"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ServeReviewWrite-alt2-4-part2" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 4 of 6: 400 review code, hvVerdict, status, or id, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "code surfaced, nothing changed"
    autolayout lr 300 100
}
dynamic qbrain.server "ServeReviewWrite-alt2-5" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 5 of 6: session or turn not found (TurnLocator), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 5 of 6: session or turn not found (TurnLocator)"
    qbrain.server.api -> qbrain.plugins.pluginPs "404 not_found"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ServeReviewWrite-alt2-5-part2" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 5 of 6: session or turn not found (TurnLocator), part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "request_unknown, nothing changed"
    autolayout lr 300 100
}
dynamic qbrain.server "ServeReviewWrite-alt2-6" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 6 of 6: SerializableWrite bound exhausted, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 6 of 6: SerializableWrite bound exhausted"
    qbrain.server.api -> qbrain.plugins.pluginPs "409 conflict, retryable"
    autolayout lr 300 100
}
dynamic qbrain.plugins "ServeReviewWrite-alt2-6-part2" "Sequence diagram converted from fence F94 (line 2501)." {
    title "Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 6 of 6: SerializableWrite bound exhausted, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "conflict, not queued, rerun after drain"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-03" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, part 1 of 6"
    qbrain.libs.client -> qbrain.server.api "DELETE .../sections/{section} (ClearTurnReviewAsync)"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    qbrain.libs.client -> qbrain.libs.client "(1 of 6) Note over Client and SessionLogController: a known non-review"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-05-03-part2" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, part 2 of 6"
    qbrain.libs.client -> qbrain.libs.client "(2 of 6) section goes to the existing clear-section handler, and DELETE"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-05-03-part3" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, part 3 of 6"
    qbrain.libs.client -> qbrain.libs.client "(3 of 6) .../sections/{section}/items/{itemKey} on a review section goes to the"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-05-03-part4" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, part 4 of 6"
    qbrain.libs.client -> qbrain.libs.client "(4 of 6) existing delete-item handler, which refuses it with 400. MCP"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-05-03-part5" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, part 5 of 6"
    qbrain.libs.client -> qbrain.libs.client "(5 of 6) sessionlog_clear_section with a review section dispatches the"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-05-03-part6" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, part 6 of 6"
    qbrain.libs.client -> qbrain.libs.client "(6 of 6) same command"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-03-alt1-1" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 1 branch 1 of 2: unknown section"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: unknown section"
    qbrain.server.api -> qbrain.libs.client "400 problem Invalid section or identifier."
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-03-alt1-2" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 1 branch 2 of 2: review section, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 2: review section"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(ResetSessionTurnReviewCommand)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-03-alt1-2-part2" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 1 branch 2 of 2: review section, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-03-alt2-1" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 1 of 6: workspace not resolved (WorkspaceResolutionBehavior)"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 6: workspace not resolved (WorkspaceResolutionBehavior)"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field workspacePath"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-03-alt2-2" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 2 of 6: invalid session or request id (TurnLocator)"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 6: invalid session or request id (TurnLocator)"
    qbrain.server.api -> qbrain.libs.client "400 problem Invalid section or identifier."
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-03-alt2-3" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 3 of 6: session or turn not found (TurnLocator)"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 3 of 6: session or turn not found (TurnLocator)"
    qbrain.server.api -> qbrain.libs.client "404 not_found"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-03-alt2-4" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 4 of 6: turn completed or failed"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 4 of 6: turn completed or failed"
    qbrain.server.api -> qbrain.libs.client "400 review_section_reset_on_terminal_turn"
    qbrain.libs.client -> qbrain.libs.client "report, record unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-03-alt2-5" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 5 of 6: turn in_progress, canceled, cancelled, or closed, part 1 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "(1 of 2) alt 2 branch 5 of 6: turn in_progress, canceled, cancelled, or"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-03-alt2-5-part2" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 5 of 6: turn in_progress, canceled, cancelled, or closed, part 2 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "(2 of 2) closed"
    qbrain.server.strSessionLogHandlers -> qbrain.server.api "record reset by ReviewDefaults for the kind and the current class, through TurnReadProjection"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-03-alt2-5-part3" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 5 of 6: turn in_progress, canceled, cancelled, or closed, part 3 of 3"
    qbrain.server.api -> qbrain.libs.client "200 reset record"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-03-alt2-6" "Sequence diagram converted from fence F95 (line 2539)." {
    title "Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 6 of 6: SerializableWrite bound exhausted"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 6 of 6: SerializableWrite bound exhausted"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable, record unchanged"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, part 1 of 4"
    strHostModel -> qbrain.plugins.pluginPs "updateTurn requestClass Chore (optional requestId)"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 3) Note: ref process BindVerbToTurn. A class-only update binds to"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-part2" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, part 2 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 3) a locally terminal turn, including a closed write target"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-part3" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, part 3 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(3 of 3) (FR-MCP-SESSIONTURN-004-AC004)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-part4" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, part 4 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-alt1-1" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 1 branch 1 of 2: binding refused"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 2: binding refused"
    qbrain.plugins.pluginPs -> strHostModel "refused with the binding code, nothing sent"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-alt1-2" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 1 branch 2 of 2: bound, part 1 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 2 of 2: bound"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-alt1-2-part2" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 1 branch 2 of 2: bound, part 2 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) Note over Plugin updateTurn and qbrain-ai-repl: ref process"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-alt1-2-part3" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 1 branch 2 of 2: bound, part 3 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) PersistTurnWriteWithReceipt, its receipt branches as drawn there"
    qbrain.plugins.pluginPs -> qbrain.repl "client.SessionLog.SubmitAsync with the bound turn carrying requestClass"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-04-alt1-2-part4" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 1 branch 2 of 2: bound, part 4 of 4"
    qbrain.repl -> qbrain.server.api "POST /qbrainai/sessionlog"
    qbrain.repl -> qbrain.repl "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-alt2-1" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 2 branch 1 of 2: transport outage or retryable 503, part 1 of 2"
    qbrain.repl -> qbrain.repl "alt 2 branch 1 of 2: transport outage or retryable 503"
    qbrain.repl -> qbrain.plugins.pluginPs "outage"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-alt2-1-part2" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 2 branch 1 of 2: transport outage or retryable 503, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "persisted false, code queued, failsafe record kept, exit 0"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-04-alt2-2" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 2 branch 2 of 2: answered, part 1 of 3"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 2: answered"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "SendAsync(SubmitSessionLogCommand)"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "(1 of 2) Note: TurnWritePipeline inside SerializableWrite applies"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-04-alt2-2-part2" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 2 branch 2 of 2: answered, part 2 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "(2 of 2) policy RequestClassification"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-04-alt2-2-part3" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 2 branch 2 of 2: answered, part 3 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 3: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-04-alt3-1" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 1 of 3: valid class, part 1 of 4"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 3 branch 1 of 3: valid class"
    qbrain.server.strSessionLogHandlers -> qbrain.server.api "class stored, hostile default applied on a turn nonterminal before the write, a terminal turn not re-gated"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-04-alt3-1-part2" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 1 of 3: valid class, part 2 of 4"
    qbrain.server.api -> qbrain.repl "201 turns outcome merged"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-alt3-1-part3" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 1 of 3: valid class, part 3 of 4"
    qbrain.repl -> qbrain.plugins.pluginPs "outcome"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-alt3-1-part4" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 1 of 3: valid class, part 4 of 4"
    qbrain.plugins.pluginPs -> strHostModel "persisted, hostile status shown"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-04-alt3-2" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 2 of 3: invalid class, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 2 of 3: invalid class"
    qbrain.server.api -> qbrain.repl "400 request_class_invalid"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-alt3-2-part2" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 2 of 3: invalid class, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "refused, nothing changed"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-04-alt3-3" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 3 of 3: SerializableWrite bound exhausted, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 3 of 3: SerializableWrite bound exhausted"
    qbrain.server.api -> qbrain.repl "409 conflict, retryable"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-05-04-alt3-3-part2" "Sequence diagram converted from fence F96 (line 2573)." {
    title "Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 3 of 3: SerializableWrite bound exhausted, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "persisted false, code queued, failsafe kept, exit 0 (outage)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-05" "Sequence diagram converted from fence F97 (line 2615)." {
    title "Sequence: submit GetSessionLogQuery and process the response, part 1 of 2"
    qbrain.libs.client -> qbrain.server.api "GET /qbrainai/sessionlog/{agent}/{sessionId}"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-05-part2" "Sequence diagram converted from fence F97 (line 2615)." {
    title "Sequence: submit GetSessionLogQuery and process the response, part 2 of 2"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "QueryAsync(GetSessionLogQuery)"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-05-alt1-1" "Sequence diagram converted from fence F97 (line 2615)." {
    title "Sequence: submit GetSessionLogQuery and process the response, alt 1 branch 1 of 4: workspace not resolved (WorkspaceResolutionBehavior)"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 4: workspace not resolved (WorkspaceResolutionBehavior)"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field workspacePath"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-05-alt1-2" "Sequence diagram converted from fence F97 (line 2615)." {
    title "Sequence: submit GetSessionLogQuery and process the response, alt 1 branch 2 of 4: session id invalid for the agent (TurnLocator)"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 4: session id invalid for the agent (TurnLocator)"
    qbrain.server.api -> qbrain.libs.client "400 problem Invalid session identifier."
    qbrain.libs.client -> qbrain.libs.client "fix the id, never retry unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-05-alt1-3" "Sequence diagram converted from fence F97 (line 2615)." {
    title "Sequence: submit GetSessionLogQuery and process the response, alt 1 branch 3 of 4: session not found (TurnLocator)"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 3 of 4: session not found (TurnLocator)"
    qbrain.server.api -> qbrain.libs.client "404, no body"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-05-alt1-4" "Sequence diagram converted from fence F97 (line 2615)." {
    title "Sequence: submit GetSessionLogQuery and process the response, alt 1 branch 4 of 4: found, part 1 of 3"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1 branch 4 of 4: found"
    qbrain.server.strSessionLogHandlers -> qbrain.server.api "session, each turn through TurnReadProjection"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-05-alt1-4-part2" "Sequence diagram converted from fence F97 (line 2615)." {
    title "Sequence: submit GetSessionLogQuery and process the response, alt 1 branch 4 of 4: found, part 2 of 3"
    qbrain.server.api -> qbrain.libs.client "200 turns with requestClass and both review records"
    qbrain.libs.client -> qbrain.libs.client "(1 of 2) read scores as exact decimals (lossless scalars in the PowerShell"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-05-05-alt1-4-part3" "Sequence diagram converted from fence F97 (line 2615)." {
    title "Sequence: submit GetSessionLogQuery and process the response, alt 1 branch 4 of 4: found, part 3 of 3"
    qbrain.libs.client -> qbrain.libs.client "(2 of 2) core, and the TypeScript-host adapters pass scores through as strings)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-06" "Sequence diagram converted from fence F98 (line 2639)." {
    title "Sequence: submit QuerySessionLogsQuery and process the response, part 1 of 2"
    qbrain.libs.client -> qbrain.server.api "GET /qbrainai/sessionlog?agent&text&planFile&todoId&turnStatus&limit&offset (and the other filters)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-06-part2" "Sequence diagram converted from fence F98 (line 2639)." {
    title "Sequence: submit QuerySessionLogsQuery and process the response, part 2 of 2"
    qbrain.server.api -> qbrain.server.strSessionLogHandlers "QueryAsync(QuerySessionLogsQuery)"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-06-alt1-1" "Sequence diagram converted from fence F98 (line 2639)." {
    title "Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 1 of 3: workspace not resolved (WorkspaceResolutionBehavior)"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 3: workspace not resolved (WorkspaceResolutionBehavior)"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field workspacePath"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-06-alt1-2" "Sequence diagram converted from fence F98 (line 2639)." {
    title "Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 2 of 3: session-log migration pending"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 3: session-log migration pending"
    qbrain.server.api -> qbrain.libs.client "503 persistence_error pending_migration"
    qbrain.libs.client -> qbrain.libs.client "report, retry after the migration is applied"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-06-alt1-3" "Sequence diagram converted from fence F98 (line 2639)." {
    title "Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 3 of 3: schema current, part 1 of 5"
    qbrain.server.strSessionLogHandlers -> qbrain.server.strSessionLogHandlers "alt 1 branch 3 of 3: schema current"
    qbrain.server.strSessionLogHandlers -> qbrain.server.api "page filtered on raw data (PagedQuery, Clamp), turns through TurnReadProjection"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-05-06-alt1-3-part2" "Sequence diagram converted from fence F98 (line 2639)." {
    title "Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 3 of 3: schema current, part 2 of 5"
    qbrain.server.api -> qbrain.libs.client "REST 200 totalCount, limit, offset, items, MCP totalCount and items only"
    qbrain.libs.client -> qbrain.libs.client "(1 of 4) REST and typed-client callers page with offset until complete,"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-05-06-alt1-3-part3" "Sequence diagram converted from fence F98 (line 2639)." {
    title "Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 3 of 3: schema current, part 3 of 5"
    qbrain.libs.client -> qbrain.libs.client "(2 of 4) an MCP caller cannot page (sessionlog_query has no offset) and"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-05-06-alt1-3-part4" "Sequence diagram converted from fence F98 (line 2639)." {
    title "Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 3 of 3: schema current, part 4 of 5"
    qbrain.libs.client -> qbrain.libs.client "(3 of 4) uses REST or the client when it needs a later page, then select the"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-05-06-alt1-3-part5" "Sequence diagram converted from fence F98 (line 2639)." {
    title "Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 3 of 3: schema current, part 5 of 5"
    qbrain.libs.client -> qbrain.libs.client "(4 of 4) session by its sessionId and read scores as exact decimals"
    autolayout lr 300 100
}
custom "STR-06-07" "Response lifecycle (state diagram), part 1 of 9" "State diagram converted from fence F105 (line 2919)." {
    include strSTR0607_Start strSTR0607_Submitted strSTR0607_Grading
    exclude "strSTR0607_Grading -> strSTR0607_Grading"
    autolayout tb 200 300
}
custom "STR-06-07-part2" "Response lifecycle (state diagram), part 2 of 9" "State diagram converted from fence F105 (line 2919)." {
    include strSTR0607_Grading strSTR0607_Trusted strSTR0607_Stale
    exclude "strSTR0607_Grading -> strSTR0607_Grading"
    exclude "strSTR0607_Stale -> strSTR0607_Stale"
    autolayout tb 200 300
}
custom "STR-06-07-part3" "Response lifecycle (state diagram), part 3 of 9" "State diagram converted from fence F105 (line 2919)." {
    include strSTR0607_Submitted strSTR0607_Stale strSTR0607_Rejected
    exclude "strSTR0607_Stale -> strSTR0607_Stale"
    autolayout tb 200 300
}
custom "STR-06-07-part4" "Response lifecycle (state diagram), part 4 of 9" "State diagram converted from fence F105 (line 2919)." {
    include strSTR0607_Grading strSTR0607_Rejected strSTR0607_Trusted
    exclude "strSTR0607_Grading -> strSTR0607_Grading"
    autolayout tb 200 300
}
custom "STR-06-07-part5" "Response lifecycle (state diagram), part 5 of 9" "State diagram converted from fence F105 (line 2919)." {
    include strSTR0607_Submitted strSTR0607_Superseded strSTR0607_Grading
    exclude "strSTR0607_Grading -> strSTR0607_Grading"
    autolayout tb 200 300
}
custom "STR-06-07-part6" "Response lifecycle (state diagram), part 6 of 9" "State diagram converted from fence F105 (line 2919)." {
    include strSTR0607_Trusted strSTR0607_Superseded strSTR0607_Stale
    exclude "strSTR0607_Stale -> strSTR0607_Stale"
    autolayout tb 200 300
}
custom "STR-06-07-part7" "Response lifecycle (state diagram), part 7 of 9" "State diagram converted from fence F105 (line 2919)." {
    include strSTR0607_Rejected strSTR0607_End strSTR0607_Superseded
    autolayout lr 300 300
}
custom "STR-06-07-part8" "Response lifecycle (state diagram), part 8 of 9" "State diagram converted from fence F105 (line 2919)." {
    include strSTR0607_Grading
    autolayout lr 300 300
}
custom "STR-06-07-part9" "Response lifecycle (state diagram), part 9 of 9" "State diagram converted from fence F105 (line 2919)." {
    include strSTR0607_Stale
    autolayout lr 300 300
}
custom "STR-06-08" "Projection row states (state diagram), part 1 of 6" "State diagram converted from fence F106 (line 2946)." {
    include strSTR0608_Start strSTR0608_Pending strSTR0608_ProjectOneResponseSave
    autolayout tb 200 300
}
custom "STR-06-08-part2" "Projection row states (state diagram), part 2 of 6" "State diagram converted from fence F106 (line 2946)." {
    include strSTR0608_Failed strSTR0608_ProjectOneResponseSave strSTR0608_Skipped
    autolayout tb 200 300
}
custom "STR-06-08-part3" "Projection row states (state diagram), part 3 of 6" "State diagram converted from fence F106 (line 2946)." {
    include strSTR0608_ProjectOneResponseSave strSTR0608_Projected strSTR0608_Removed
    autolayout lr 300 300
}
custom "STR-06-08-part4" "Projection row states (state diagram), part 4 of 6" "State diagram converted from fence F106 (line 2946)." {
    include strSTR0608_Projected strSTR0608_Pending strSTR0608_Removed
    autolayout tb 200 300
}
custom "STR-06-08-part5" "Projection row states (state diagram), part 5 of 6" "State diagram converted from fence F106 (line 2946)." {
    include strSTR0608_Skipped strSTR0608_Removed strSTR0608_Failed
    autolayout lr 300 300
}
custom "STR-06-08-part6" "Projection row states (state diagram), part 6 of 6" "State diagram converted from fence F106 (line 2946)." {
    include strSTR0608_Failed strSTR0608_Pending
    autolayout lr 300 300
}
custom "GetAgentContextStatusQuery" "GetAgentContextStatusQuery: get status (activity), part 1 of 2" "Activity diagram converted from fence F107 (line 3060)." {
    include strGetAgentContextStatusQuery_HandlerRunsGETStatus strGetAgentContextStatusQuery_ReadAgentContextOptionsInForce strGetAgentContextStatusQuery_IGraphRagServiceGetStatusAsyncEnabledAndState strGetAgentContextStatusQuery_IResponseStalenessRuleReadOnlySubmittedGradingTrusted strGetAgentContextStatusQuery_CountPackagesAndResponsesByEffective
    autolayout tb 100 200
}
custom "GetAgentContextStatusQuery-part2" "GetAgentContextStatusQuery: get status (activity), part 2 of 2" "Activity diagram converted from fence F107 (line 3060)." {
    include strGetAgentContextStatusQuery_CountPackagesAndResponsesByEffective strGetAgentContextStatusQuery_200AgentContextStatusResult
    autolayout lr 250 150
}
custom "BuildAgentContextPackageCommand" "BuildAgentContextPackageCommand: build a context package (activity), part 1 of 6" "Activity diagram converted from fence F108 (line 3083)." {
    include strBuildAgentContextPackageCommand_HandlerRunsPOSTPackages strBuildAgentContextPackageCommand_FieldsPresentTokenBudgetInRange strBuildAgentContextPackageCommand_400ValidationError strBuildAgentContextPackageCommand_IdempotentCreateLookupReadOnly
    autolayout tb 100 100
}
custom "BuildAgentContextPackageCommand-part2" "BuildAgentContextPackageCommand: build a context package (activity), part 2 of 6" "Activity diagram converted from fence F108 (line 3083)." {
    include strBuildAgentContextPackageCommand_IdempotentCreateLookupReadOnly strBuildAgentContextPackageCommand_StoredPackageStatus strBuildAgentContextPackageCommand_409ConflictRetryableOnlyWhenExhausted strBuildAgentContextPackageCommand_ReadBuildInputsNoTransactionOpen
    autolayout lr 250 150
}
custom "BuildAgentContextPackageCommand-part3" "BuildAgentContextPackageCommand: build a context package (activity), part 3 of 6" "Activity diagram converted from fence F108 (line 3083)." {
    include strBuildAgentContextPackageCommand_StoredPackageStatus strBuildAgentContextPackageCommand_200TheStoredPackage strBuildAgentContextPackageCommand_422AgentContextBudgetExceeded strBuildAgentContextPackageCommand_ReadBuildInputsNoTransactionOpen strBuildAgentContextPackageCommand_RefusalOfReadBuildInputsDrawnThere strBuildAgentContextPackageCommand_PackageAssemblerInMemory
    autolayout lr 200 50
}
custom "BuildAgentContextPackageCommand-part4" "BuildAgentContextPackageCommand: build a context package (activity), part 4 of 6" "Activity diagram converted from fence F108 (line 3083)." {
    include strBuildAgentContextPackageCommand_PackageAssemblerInMemory strBuildAgentContextPackageCommand_SavePackage
    autolayout lr 250 150
}
custom "BuildAgentContextPackageCommand-part5" "BuildAgentContextPackageCommand: build a context package (activity), part 5 of 6" "Activity diagram converted from fence F108 (line 3083)." {
    include strBuildAgentContextPackageCommand_SavePackage strBuildAgentContextPackageCommand_200TheStoredPackage strBuildAgentContextPackageCommand_422AgentContextBudgetExceeded strBuildAgentContextPackageCommand_409ConflictRetryableOnlyWhenExhausted
    autolayout lr 250 150
}
custom "BuildAgentContextPackageCommand-part6" "BuildAgentContextPackageCommand: build a context package (activity), part 6 of 6" "Activity diagram converted from fence F108 (line 3083)." {
    include strBuildAgentContextPackageCommand_SavePackage strBuildAgentContextPackageCommand_IdempotentCreateLookupReadOnly strBuildAgentContextPackageCommand_201PackageStatusBuilt
    autolayout lr 250 150
}
custom "ReadBuildInputs" "Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite) (activity), part 1 of 4" "Activity diagram converted from fence F109 (line 3107)." {
    include strReadBuildInputs_ReadBuildInputsNoTransactionOpen strReadBuildInputs_TurnLocatorAgentSessionIdRequestId
    autolayout lr 250 150
}
custom "ReadBuildInputs-part2" "Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite) (activity), part 2 of 4" "Activity diagram converted from fence F109 (line 3107)." {
    include strReadBuildInputs_TurnLocatorAgentSessionIdRequestId strReadBuildInputs_400ValidationErrorSessionIdOrRequestId strReadBuildInputs_404NotFoundSessionTurn strReadBuildInputs_SourceResolverReadCommitHEADCommit
    autolayout lr 250 150
}
custom "ReadBuildInputs-part3" "Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite) (activity), part 3 of 4" "Activity diagram converted from fence F109 (line 3107)." {
    include strReadBuildInputs_SourceResolverReadCommitHEADCommit strReadBuildInputs_409AgentContextCommitUnavailable strReadBuildInputs_AProfileFileAndARule strReadBuildInputs_409AgentContextProfileUnavailable strReadBuildInputs_CandidatesMandatoryItemsFocusRefsRetrieve
    autolayout tb 100 100
}
custom "ReadBuildInputs-part4" "Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite) (activity), part 4 of 4" "Activity diagram converted from fence F109 (line 3107)." {
    include strReadBuildInputs_CandidatesMandatoryItemsFocusRefsRetrieve strReadBuildInputs_IResponseStalenessRuleReadOnlyPriorResponses strReadBuildInputs_SourceResolverContentAndHashOfEach strReadBuildInputs_InputSnapshotWithInputsSha256
    autolayout tb 100 200
}
custom "SavePackage" "Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler) (activity), part 1 of 5" "Activity diagram converted from fence F110 (line 3126)." {
    include strSavePackage_SavePackageSnapshotAndAssembledPackage strSavePackage_SerializableWriteDatabaseOnly strSavePackage_409ConflictRetryableTrue strSavePackage_IdempotentCreateLookup
    autolayout tb 100 100
}
custom "SavePackage-part2" "Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler) (activity), part 2 of 5" "Activity diagram converted from fence F110 (line 3126)." {
    include strSavePackage_IdempotentCreateLookup strSavePackage_ReturnedTheStoredPackage strSavePackage_409ConflictRetryableFalse strSavePackage_TurnStatusAsTheReadsSaw
    autolayout lr 250 150
}
custom "SavePackage-part3" "Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler) (activity), part 3 of 5" "Activity diagram converted from fence F110 (line 3126)." {
    include strSavePackage_TurnStatusAsTheReadsSaw strSavePackage_ReturnedReadAgain strSavePackage_InsertTheOverflowPackage strSavePackage_InsertTheBuiltPackage
    autolayout lr 250 150
}
custom "SavePackage-part4" "Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler) (activity), part 4 of 5" "Activity diagram converted from fence F110 (line 3126)." {
    include strSavePackage_InsertTheOverflowPackage strSavePackage_422AgentContextBudgetExceeded strSavePackage_SerializableWriteDatabaseOnly strSavePackage_InsertTheBuiltPackage strSavePackage_RelationshipWriterDerivedFromRelevantTo
    autolayout lr 250 150
}
custom "SavePackage-part5" "Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler) (activity), part 5 of 5" "Activity diagram converted from fence F110 (line 3126)." {
    include strSavePackage_RelationshipWriterDerivedFromRelevantTo strSavePackage_201PackageStatusBuilt
    autolayout lr 250 150
}
custom "FillBudgetWithCandidates" "Nested activity: FillBudgetWithCandidates (loop of PackageAssembler, for BuildAgentContextPackageCommand and ReplayAgentContextPackageQuery) (activity), part 1 of 3" "Activity diagram converted from fence F111 (line 3155)." {
    include strFillBudgetWithCandidates_OptionalCandidatesAfterTheSecretAnd strFillBudgetWithCandidates_CandidateRankerScoreRoundedHalfEvenTo strFillBudgetWithCandidates_NextCandidateInRankOrder
    autolayout tb 100 200
}
custom "FillBudgetWithCandidates-part2" "Nested activity: FillBudgetWithCandidates (loop of PackageAssembler, for BuildAgentContextPackageCommand and ReplayAgentContextPackageQuery) (activity), part 2 of 3" "Activity diagram converted from fence F111 (line 3155)." {
    include strFillBudgetWithCandidates_NextCandidateInRankOrder strFillBudgetWithCandidates_ReturnEveryCandidatePlaced strFillBudgetWithCandidates_BeyondTheFirstMaxCandidatesPerKindOfIts strFillBudgetWithCandidates_OmitWithReasonBelowCandidateLimit strFillBudgetWithCandidates_ItsTokensAtMostTheRemaining
    autolayout tb 100 100
}
custom "FillBudgetWithCandidates-part3" "Nested activity: FillBudgetWithCandidates (loop of PackageAssembler, for BuildAgentContextPackageCommand and ReplayAgentContextPackageQuery) (activity), part 3 of 3" "Activity diagram converted from fence F111 (line 3155)." {
    include strFillBudgetWithCandidates_ItsTokensAtMostTheRemaining strFillBudgetWithCandidates_IncludeWithScoreRationaleAndRetrieval strFillBudgetWithCandidates_OmitWithReasonBudget strFillBudgetWithCandidates_NextCandidateInRankOrder
    autolayout tb 100 100
}
custom "GetAgentContextPackageQuery" "GetAgentContextPackageQuery: get a package (activity), part 1 of 2" "Activity diagram converted from fence F112 (line 3176)." {
    include strGetAgentContextPackageQuery_HandlerRunsGETPackage strGetAgentContextPackageQuery_PackageFoundInTheWorkspace strGetAgentContextPackageQuery_404NotFoundPackage strGetAgentContextPackageQuery_IncludeContent
    autolayout tb 100 100
}
custom "GetAgentContextPackageQuery-part2" "GetAgentContextPackageQuery: get a package (activity), part 2 of 2" "Activity diagram converted from fence F112 (line 3176)." {
    include strGetAgentContextPackageQuery_IncludeContent strGetAgentContextPackageQuery_LoadItemsWithContentOmissionsFindings strGetAgentContextPackageQuery_LoadItemsWithoutContentOmissionsFindings strGetAgentContextPackageQuery_200AgentContextPackageResult
    autolayout tb 100 100
}
custom "QueryAgentContextPackagesQuery" "QueryAgentContextPackagesQuery: query packages (activity), part 1 of 2" "Activity diagram converted from fence F113 (line 3194)." {
    include strQueryAgentContextPackagesQuery_HandlerRunsGETPackages strQueryAgentContextPackagesQuery_StatusFilterAbsentOrKnown strQueryAgentContextPackagesQuery_400ValidationError strQueryAgentContextPackagesQuery_PagedQueryPageBoundsRefuseFilterOrderCreatedUtc
    autolayout tb 100 200
}
custom "QueryAgentContextPackagesQuery-part2" "QueryAgentContextPackagesQuery: query packages (activity), part 2 of 2" "Activity diagram converted from fence F113 (line 3194)." {
    include strQueryAgentContextPackagesQuery_PagedQueryPageBoundsRefuseFilterOrderCreatedUtc strQueryAgentContextPackagesQuery_400ValidationError strQueryAgentContextPackagesQuery_200PageResultOfPackageSummaries
    autolayout lr 250 150
}
custom "ReplayAgentContextPackageQuery" "ReplayAgentContextPackageQuery: replay a package (activity), part 1 of 5" "Activity diagram converted from fence F114 (line 3210)." {
    include strReplayAgentContextPackageQuery_HandlerRunsPOSTReplay strReplayAgentContextPackageQuery_BuiltPackageLookupPackageId
    autolayout lr 250 150
}
custom "ReplayAgentContextPackageQuery-part2" "ReplayAgentContextPackageQuery: replay a package (activity), part 2 of 5" "Activity diagram converted from fence F114 (line 3210)." {
    include strReplayAgentContextPackageQuery_BuiltPackageLookupPackageId strReplayAgentContextPackageQuery_404NotFoundPackage strReplayAgentContextPackageQuery_409AgentContextPackageOverflow strReplayAgentContextPackageQuery_InputSnapshotStored
    autolayout lr 250 150
}
custom "ReplayAgentContextPackageQuery-part3" "ReplayAgentContextPackageQuery: replay a package (activity), part 3 of 5" "Activity diagram converted from fence F114 (line 3210)." {
    include strReplayAgentContextPackageQuery_InputSnapshotStored strReplayAgentContextPackageQuery_409AgentContextReplaySnapshotUnavailable strReplayAgentContextPackageQuery_BuilderVersionEqualsTheRunningBuilderS strReplayAgentContextPackageQuery_409AgentContextReplayBuilderChanged strReplayAgentContextPackageQuery_PackageAssemblerOnTheStoredInputSnapshot
    autolayout tb 100 100
}
custom "ReplayAgentContextPackageQuery-part4" "ReplayAgentContextPackageQuery: replay a package (activity), part 4 of 5" "Activity diagram converted from fence F114 (line 3210)." {
    include strReplayAgentContextPackageQuery_PackageAssemblerOnTheStoredInputSnapshot strReplayAgentContextPackageQuery_CompareInputsSha256ContentSha256AndItemsBy strReplayAgentContextPackageQuery_Identical
    autolayout tb 100 200
}
custom "ReplayAgentContextPackageQuery-part5" "ReplayAgentContextPackageQuery: replay a package (activity), part 5 of 5" "Activity diagram converted from fence F114 (line 3210)." {
    include strReplayAgentContextPackageQuery_Identical strReplayAgentContextPackageQuery_200IdenticalTrue strReplayAgentContextPackageQuery_200IdenticalFalseWithDifferencesAdded
    autolayout lr 250 150
}
custom "SubmitAgentResponseCommand" "SubmitAgentResponseCommand: submit a response (activity), part 1 of 6" "Activity diagram converted from fence F115 (line 3238)." {
    include strSubmitAgentResponseCommand_HandlerRunsPOSTResponses strSubmitAgentResponseCommand_SubmissionWellFormed strSubmitAgentResponseCommand_400ValidationErrorDetailsField strSubmitAgentResponseCommand_IdempotentCreateLookupReadOnly
    autolayout tb 100 100
}
custom "SubmitAgentResponseCommand-part2" "SubmitAgentResponseCommand: submit a response (activity), part 2 of 6" "Activity diagram converted from fence F115 (line 3238)." {
    include strSubmitAgentResponseCommand_IdempotentCreateLookupReadOnly strSubmitAgentResponseCommand_200TheStoredResponse strSubmitAgentResponseCommand_409ConflictRetryableFalse strSubmitAgentResponseCommand_ReadSubmissionInputsNoTransactionOpen
    autolayout lr 250 150
}
custom "SubmitAgentResponseCommand-part3" "SubmitAgentResponseCommand: submit a response (activity), part 3 of 6" "Activity diagram converted from fence F115 (line 3238)." {
    include strSubmitAgentResponseCommand_ReadSubmissionInputsNoTransactionOpen strSubmitAgentResponseCommand_RefusalOfReadSubmissionInputsDrawnThere strSubmitAgentResponseCommand_SecretRedactorThenContentSha256 strSubmitAgentResponseCommand_SerializableWriteDatabaseOnly
    autolayout tb 100 100
}
custom "SubmitAgentResponseCommand-part4" "SubmitAgentResponseCommand: submit a response (activity), part 4 of 6" "Activity diagram converted from fence F115 (line 3238)." {
    include strSubmitAgentResponseCommand_SerializableWriteDatabaseOnly strSubmitAgentResponseCommand_409ConflictRetryableTrue strSubmitAgentResponseCommand_IdempotentCreateLookup
    autolayout lr 250 150
}
custom "SubmitAgentResponseCommand-part5" "SubmitAgentResponseCommand: submit a response (activity), part 5 of 6" "Activity diagram converted from fence F115 (line 3238)." {
    include strSubmitAgentResponseCommand_IdempotentCreateLookup strSubmitAgentResponseCommand_200TheStoredResponse strSubmitAgentResponseCommand_409ConflictRetryableFalse strSubmitAgentResponseCommand_InsertTheResponseClaimsEvidence
    autolayout lr 250 150
}
custom "SubmitAgentResponseCommand-part6" "SubmitAgentResponseCommand: submit a response (activity), part 6 of 6" "Activity diagram converted from fence F115 (line 3238)." {
    include strSubmitAgentResponseCommand_InsertTheResponseClaimsEvidence strSubmitAgentResponseCommand_SerializableWriteDatabaseOnly strSubmitAgentResponseCommand_RelationshipWriterDerivedFromSupports strSubmitAgentResponseCommand_201ResponseStatusSubmitted
    autolayout tb 100 100
}
custom "ReadSubmissionInputs" "Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite) (activity), part 1 of 4" "Activity diagram converted from fence F116 (line 3268)." {
    include strReadSubmissionInputs_ReadSubmissionInputsNoTransactionOpen strReadSubmissionInputs_BuiltPackageLookupPackageId
    autolayout lr 250 150
}
custom "ReadSubmissionInputs-part2" "Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite) (activity), part 2 of 4" "Activity diagram converted from fence F116 (line 3268)." {
    include strReadSubmissionInputs_BuiltPackageLookupPackageId strReadSubmissionInputs_404NotFoundPackage strReadSubmissionInputs_409AgentContextPackageOverflow strReadSubmissionInputs_AgentModeAndEvidenceFitThe
    autolayout lr 250 150
}
custom "ReadSubmissionInputs-part3" "Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite) (activity), part 3 of 4" "Activity diagram converted from fence F116 (line 3268)." {
    include strReadSubmissionInputs_AgentModeAndEvidenceFitThe strReadSubmissionInputs_400ValidationErrorDetailsField strReadSubmissionInputs_SourceResolverReadCommitThenEachEvidenceHash
    autolayout tb 100 200
}
custom "ReadSubmissionInputs-part4" "Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite) (activity), part 4 of 4" "Activity diagram converted from fence F116 (line 3268)." {
    include strReadSubmissionInputs_SourceResolverReadCommitThenEachEvidenceHash strReadSubmissionInputs_409AgentContextCommitUnavailable strReadSubmissionInputs_400ValidationErrorDetailsField strReadSubmissionInputs_PackageAndEvidenceHashes
    autolayout lr 250 150
}
custom "GetAgentResponseQuery" "GetAgentResponseQuery: get a response (activity), part 1 of 2" "Activity diagram converted from fence F117 (line 3288)." {
    include strGetAgentResponseQuery_HandlerRunsGETResponse strGetAgentResponseQuery_ResponseFoundInTheWorkspace strGetAgentResponseQuery_404NotFoundResponse strGetAgentResponseQuery_IResponseStalenessRuleReadOnlyEffectiveStatusNothing
    autolayout tb 100 100
}
custom "GetAgentResponseQuery-part2" "GetAgentResponseQuery: get a response (activity), part 2 of 2" "Activity diagram converted from fence F117 (line 3288)." {
    include strGetAgentResponseQuery_IResponseStalenessRuleReadOnlyEffectiveStatusNothing strGetAgentResponseQuery_LoadClaimsEvidenceGradesWithFindings strGetAgentResponseQuery_200AgentResponseResult
    autolayout tb 100 200
}
custom "QueryAgentResponsesQuery" "QueryAgentResponsesQuery: query responses (activity), part 1 of 3" "Activity diagram converted from fence F118 (line 3305)." {
    include strQueryAgentResponsesQuery_HandlerRunsGETResponses strQueryAgentResponsesQuery_StatusFilterAbsentOrKnown strQueryAgentResponsesQuery_400ValidationError strQueryAgentResponsesQuery_IResponseStalenessRuleReadOnlyEffectiveStatusOf
    autolayout tb 100 100
}
custom "QueryAgentResponsesQuery-part2" "QueryAgentResponsesQuery: query responses (activity), part 2 of 3" "Activity diagram converted from fence F118 (line 3305)." {
    include strQueryAgentResponsesQuery_IResponseStalenessRuleReadOnlyEffectiveStatusOf strQueryAgentResponsesQuery_TrustedOnly strQueryAgentResponsesQuery_RestrictToEffectiveStatusTrusted strQueryAgentResponsesQuery_EveryEffectiveStatusOrTheRequested
    autolayout tb 100 100
}
custom "QueryAgentResponsesQuery-part3" "QueryAgentResponsesQuery: query responses (activity), part 3 of 3" "Activity diagram converted from fence F118 (line 3305)." {
    include strQueryAgentResponsesQuery_RestrictToEffectiveStatusTrusted strQueryAgentResponsesQuery_PagedQueryPageBoundsRefuseOrderSubmittedUtcDescending strQueryAgentResponsesQuery_EveryEffectiveStatusOrTheRequested strQueryAgentResponsesQuery_400ValidationError strQueryAgentResponsesQuery_200PageResultOfResponseSummaries
    autolayout tb 100 100
}
custom "RunDeterministicGradeCommand" "RunDeterministicGradeCommand: run the deterministic grade (activity), part 1 of 6" "Activity diagram converted from fence F119 (line 3331)." {
    include strRunDeterministicGradeCommand_HandlerRunsPOSTGradesDeterministic strRunDeterministicGradeCommand_ResponseFoundAndGradable
    autolayout lr 250 150
}
custom "RunDeterministicGradeCommand-part2" "RunDeterministicGradeCommand: run the deterministic grade (activity), part 2 of 6" "Activity diagram converted from fence F119 (line 3331)." {
    include strRunDeterministicGradeCommand_ResponseFoundAndGradable strRunDeterministicGradeCommand_404NotFoundResponse strRunDeterministicGradeCommand_409AgentContextResponseNotGradable strRunDeterministicGradeCommand_EachEvidenceItemNestedCheckOneEvidenceItem
    autolayout lr 250 150
}
custom "RunDeterministicGradeCommand-part3" "RunDeterministicGradeCommand: run the deterministic grade (activity), part 3 of 6" "Activity diagram converted from fence F119 (line 3331)." {
    include strRunDeterministicGradeCommand_EachEvidenceItemNestedCheckOneEvidenceItem strRunDeterministicGradeCommand_SecretRedactorContainsSecretContentClaimsRefs strRunDeterministicGradeCommand_DispatchEvaluateStalenessCommandForIt
    autolayout tb 100 200
}
custom "RunDeterministicGradeCommand-part4" "RunDeterministicGradeCommand: run the deterministic grade (activity), part 4 of 6" "Activity diagram converted from fence F119 (line 3331)." {
    include strRunDeterministicGradeCommand_DispatchEvaluateStalenessCommandForIt strRunDeterministicGradeCommand_409ConflictRetryableTrue strRunDeterministicGradeCommand_StalenessFindingExactScores strRunDeterministicGradeCommand_ScoreVerdictMaxOfConfiguredAnd0 strRunDeterministicGradeCommand_SerializableWriteLockingTheResponse
    autolayout tb 100 200
}
custom "RunDeterministicGradeCommand-part5" "RunDeterministicGradeCommand: run the deterministic grade (activity), part 5 of 6" "Activity diagram converted from fence F119 (line 3331)." {
    include strRunDeterministicGradeCommand_SerializableWriteLockingTheResponse strRunDeterministicGradeCommand_409ConflictRetryableTrue strRunDeterministicGradeCommand_ResponseAsTheReadsSawIt strRunDeterministicGradeCommand_ResponseFoundAndGradable strRunDeterministicGradeCommand_SaveTheGradeAndItsFindings
    autolayout tb 100 100
}
custom "RunDeterministicGradeCommand-part6" "RunDeterministicGradeCommand: run the deterministic grade (activity), part 6 of 6" "Activity diagram converted from fence F119 (line 3331)." {
    include strRunDeterministicGradeCommand_SaveTheGradeAndItsFindings strRunDeterministicGradeCommand_SerializableWriteLockingTheResponse strRunDeterministicGradeCommand_DispatchDecideAgentResponseTrustCommand strRunDeterministicGradeCommand_201GradeWithTheResponseStatus
    autolayout tb 100 100
}
custom "CheckOneEvidenceItem" "Nested activity: CheckOneEvidenceItem (loop body of RunDeterministicGradeCommand) (activity), part 1 of 3" "Activity diagram converted from fence F120 (line 3359)." {
    include strCheckOneEvidenceItem_NextEvidenceItemOfTheClaim strCheckOneEvidenceItem_EvidenceKind strCheckOneEvidenceItem_ServerComputedSha256Present strCheckOneEvidenceItem_SourceResolverKindCommitRefAgainstThe
    autolayout tb 100 100
}
custom "CheckOneEvidenceItem-part2" "Nested activity: CheckOneEvidenceItem (loop body of RunDeterministicGradeCommand) (activity), part 2 of 3" "Activity diagram converted from fence F120 (line 3359)." {
    include strCheckOneEvidenceItem_ServerComputedSha256Present strCheckOneEvidenceItem_FindingUNKNOWN strCheckOneEvidenceItem_CitedSha256AbsentOrEqualToSha256 strCheckOneEvidenceItem_SourceResolverKindCommitRefAgainstThe strCheckOneEvidenceItem_FindingPASS strCheckOneEvidenceItem_FindingFAIL
    autolayout tb 100 100
}
custom "CheckOneEvidenceItem-part3" "Nested activity: CheckOneEvidenceItem (loop body of RunDeterministicGradeCommand) (activity), part 3 of 3" "Activity diagram converted from fence F120 (line 3359)." {
    include strCheckOneEvidenceItem_FindingUNKNOWN strCheckOneEvidenceItem_FindingRecordedContinueWithTheNext strCheckOneEvidenceItem_CitedSha256AbsentOrEqualToSha256 strCheckOneEvidenceItem_FindingPASS strCheckOneEvidenceItem_FindingFAIL
    autolayout tb 100 100
}
custom "RecordModelGradeCommand" "RecordModelGradeCommand: record a model grade from an HV review (activity), part 1 of 5" "Activity diagram converted from fence F121 (line 3388)." {
    include strRecordModelGradeCommand_BodyDeserializedPOSTGradesModel strRecordModelGradeCommand_ScoreLexemeJsonConverterBothScores strRecordModelGradeCommand_400ReviewScoreFormatPrecisionOr strRecordModelGradeCommand_ReceiptFieldsFindingsAndGraderValid
    autolayout tb 100 100
}
custom "RecordModelGradeCommand-part2" "RecordModelGradeCommand: record a model grade from an HV review (activity), part 2 of 5" "Activity diagram converted from fence F121 (line 3388)." {
    include strRecordModelGradeCommand_ReceiptFieldsFindingsAndGraderValid strRecordModelGradeCommand_400AgentContextReceiptPathsRequired strRecordModelGradeCommand_400ValidationError strRecordModelGradeCommand_403AgentContextGraderNotAllowed strRecordModelGradeCommand_IdempotentCreateLookupReadOnly
    autolayout lr 200 50
}
custom "RecordModelGradeCommand-part3" "RecordModelGradeCommand: record a model grade from an HV review (activity), part 3 of 5" "Activity diagram converted from fence F121 (line 3388)." {
    include strRecordModelGradeCommand_IdempotentCreateLookupReadOnly strRecordModelGradeCommand_200TheStoredGrade strRecordModelGradeCommand_409ConflictRetryableOnlyWhenExhausted strRecordModelGradeCommand_ReadModelGradeInputsNoTransactionOpen
    autolayout lr 250 150
}
custom "RecordModelGradeCommand-part4" "RecordModelGradeCommand: record a model grade from an HV review (activity), part 4 of 5" "Activity diagram converted from fence F121 (line 3388)." {
    include strRecordModelGradeCommand_ReadModelGradeInputsNoTransactionOpen strRecordModelGradeCommand_RefusalOfReadModelGradeInputsDrawnThere strRecordModelGradeCommand_SaveModelGrade
    autolayout lr 250 150
}
custom "RecordModelGradeCommand-part5" "RecordModelGradeCommand: record a model grade from an HV review (activity), part 5 of 5" "Activity diagram converted from fence F121 (line 3388)." {
    include strRecordModelGradeCommand_SaveModelGrade strRecordModelGradeCommand_200TheStoredGrade strRecordModelGradeCommand_409ConflictRetryableOnlyWhenExhausted strRecordModelGradeCommand_IdempotentCreateLookupReadOnly strRecordModelGradeCommand_201GradeResponseStatusHvRequestStatus
    autolayout tb 100 100
}
custom "ReadModelGradeInputs" "Nested activity: ReadModelGradeInputs (reads of RecordModelGradeCommand, before its SerializableWrite) (activity), part 1 of 3" "Activity diagram converted from fence F122 (line 3412)." {
    include strReadModelGradeInputs_ReadModelGradeInputsNoTransactionOpen strReadModelGradeInputs_CheckGradeTarget strReadModelGradeInputs_RefusalOfCheckGradeTargetDrawnThere strReadModelGradeInputs_TheEightReceiptChecksPass
    autolayout tb 100 100
}
custom "ReadModelGradeInputs-part2" "Nested activity: ReadModelGradeInputs (reads of RecordModelGradeCommand, before its SerializableWrite) (activity), part 2 of 3" "Activity diagram converted from fence F122 (line 3412)." {
    include strReadModelGradeInputs_TheEightReceiptChecksPass strReadModelGradeInputs_422AgentContextReceiptUnverified strReadModelGradeInputs_DispatchEvaluateStalenessCommandForIt strReadModelGradeInputs_409ConflictRetryableTrue strReadModelGradeInputs_ScoreVerdictOverTheFindings
    autolayout tb 100 100
}
custom "ReadModelGradeInputs-part3" "Nested activity: ReadModelGradeInputs (reads of RecordModelGradeCommand, before its SerializableWrite) (activity), part 3 of 3" "Activity diagram converted from fence F122 (line 3412)." {
    include strReadModelGradeInputs_ScoreVerdictOverTheFindings strReadModelGradeInputs_ChecksPassedVerdictDerived
    autolayout lr 250 150
}
custom "CheckGradeTarget" "Nested activity: CheckGradeTarget (response, claim, HV request, and item checks of ReadModelGradeInputs) (activity), part 1 of 3" "Activity diagram converted from fence F123 (line 3428)." {
    include strCheckGradeTarget_CheckGradeTargetNoTransactionOpen strCheckGradeTarget_ResponseInTheWorkspace strCheckGradeTarget_404NotFoundResponse strCheckGradeTarget_EachClaimIdAClaimOfIt
    autolayout tb 100 100
}
custom "CheckGradeTarget-part2" "Nested activity: CheckGradeTarget (response, claim, HV request, and item checks of ReadModelGradeInputs) (activity), part 2 of 3" "Activity diagram converted from fence F123 (line 3428)." {
    include strCheckGradeTarget_EachClaimIdAClaimOfIt strCheckGradeTarget_400ValidationError strCheckGradeTarget_HVRequestInTheWorkspace strCheckGradeTarget_404NotFoundHvRequest strCheckGradeTarget_GradableInThatHVRequest
    autolayout tb 100 100
}
custom "CheckGradeTarget-part3" "Nested activity: CheckGradeTarget (response, claim, HV request, and item checks of ReadModelGradeInputs) (activity), part 3 of 3" "Activity diagram converted from fence F123 (line 3428)." {
    include strCheckGradeTarget_GradableInThatHVRequest strCheckGradeTarget_409AgentContextResponseNotGradable strCheckGradeTarget_409AgentContextHvRequestClosed strCheckGradeTarget_409AgentContextGradeNotIn strCheckGradeTarget_GradableAnItemOfAnOpen
    autolayout lr 200 50
}
custom "SaveModelGrade" "Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs) (activity), part 1 of 4" "Activity diagram converted from fence F124 (line 3447)." {
    include strSaveModelGrade_SaveModelGradeChecksPassedVerdictDerived strSaveModelGrade_SerializableWriteLockingTheResponse strSaveModelGrade_409ConflictRetryableTrue strSaveModelGrade_IdempotentCreateLookup
    autolayout tb 100 100
}
custom "SaveModelGrade-part2" "Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs) (activity), part 2 of 4" "Activity diagram converted from fence F124 (line 3447)." {
    include strSaveModelGrade_IdempotentCreateLookup strSaveModelGrade_200TheStoredGrade strSaveModelGrade_409ConflictRetryableFalse strSaveModelGrade_ResponseHVRequestItemAsRead
    autolayout lr 250 150
}
custom "SaveModelGrade-part3" "Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs) (activity), part 3 of 4" "Activity diagram converted from fence F124 (line 3447)." {
    include strSaveModelGrade_ResponseHVRequestItemAsRead strSaveModelGrade_ReturnedReadAgain strSaveModelGrade_InsertTheGradeItemGraded strSaveModelGrade_SerializableWriteLockingTheResponse strSaveModelGrade_DispatchDecideAgentResponseTrustCommand
    autolayout tb 100 100
}
custom "SaveModelGrade-part4" "Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs) (activity), part 4 of 4" "Activity diagram converted from fence F124 (line 3447)." {
    include strSaveModelGrade_DispatchDecideAgentResponseTrustCommand strSaveModelGrade_201GradeResponseStatusHvRequestStatus
    autolayout lr 250 150
}
custom "DecideAgentResponseTrustCommand" "DecideAgentResponseTrustCommand: decide trust (internal, after every grade) (activity), part 1 of 2" "Activity diagram converted from fence F125 (line 3474)." {
    include strDecideAgentResponseTrustCommand_DispatchedInAGradeUnitS strDecideAgentResponseTrustCommand_StoredStatusStaleRejectedOrSuperseded strDecideAgentResponseTrustCommand_AgentResponseTrustDecisionReturnedStatusUnchanged strDecideAgentResponseTrustCommand_ReadTheLatestDeterministicAndModel
    autolayout tb 100 100
}
custom "DecideAgentResponseTrustCommand-part2" "DecideAgentResponseTrustCommand: decide trust (internal, after every grade) (activity), part 2 of 2" "Activity diagram converted from fence F125 (line 3474)." {
    include strDecideAgentResponseTrustCommand_ReadTheLatestDeterministicAndModel strDecideAgentResponseTrustCommand_ScoreVerdictReDeriveAtMaxOf strDecideAgentResponseTrustCommand_BothGradesExistBothVerdictsPass strDecideAgentResponseTrustCommand_ResponseStatusTransitionToTheTarget strDecideAgentResponseTrustCommand_AgentResponseTrustDecisionReturned
    autolayout tb 100 200
}
custom "RejectAgentResponseCommand" "RejectAgentResponseCommand: reject a response (activity), part 1 of 3" "Activity diagram converted from fence F126 (line 3493)." {
    include strRejectAgentResponseCommand_HandlerRunsPOSTReject strRejectAgentResponseCommand_ReasonNonblank strRejectAgentResponseCommand_400ValidationError strRejectAgentResponseCommand_SerializableWriteLockingTheResponse
    autolayout tb 100 100
}
custom "RejectAgentResponseCommand-part2" "RejectAgentResponseCommand: reject a response (activity), part 2 of 3" "Activity diagram converted from fence F126 (line 3493)." {
    include strRejectAgentResponseCommand_SerializableWriteLockingTheResponse strRejectAgentResponseCommand_409ConflictRetryableTrue strRejectAgentResponseCommand_ResponseFoundInTheWorkspace strRejectAgentResponseCommand_404NotFoundResponse strRejectAgentResponseCommand_ResponseStatusTransitionToRejectedWithTheReason
    autolayout tb 100 100
}
custom "RejectAgentResponseCommand-part3" "RejectAgentResponseCommand: reject a response (activity), part 3 of 3" "Activity diagram converted from fence F126 (line 3493)." {
    include strRejectAgentResponseCommand_ResponseStatusTransitionToRejectedWithTheReason strRejectAgentResponseCommand_200AgentResponseResultUnchanged strRejectAgentResponseCommand_409AgentContextTransitionInvalid strRejectAgentResponseCommand_200AgentResponseResultStatusRejected
    autolayout lr 250 150
}
custom "SupersedeAgentResponseCommand" "SupersedeAgentResponseCommand: supersede a response (activity), part 1 of 5" "Activity diagram converted from fence F127 (line 3520)." {
    include strSupersedeAgentResponseCommand_HandlerRunsPOSTSupersede strSupersedeAgentResponseCommand_ReplacementDiffersReasonNonblank strSupersedeAgentResponseCommand_400ValidationError strSupersedeAgentResponseCommand_SerializableWriteLockingBothByResponseId
    autolayout tb 100 100
}
custom "SupersedeAgentResponseCommand-part2" "SupersedeAgentResponseCommand: supersede a response (activity), part 2 of 5" "Activity diagram converted from fence F127 (line 3520)." {
    include strSupersedeAgentResponseCommand_SerializableWriteLockingBothByResponseId strSupersedeAgentResponseCommand_409ConflictRetryableTrue strSupersedeAgentResponseCommand_BothFoundReplacementValid
    autolayout lr 250 150
}
custom "SupersedeAgentResponseCommand-part3" "SupersedeAgentResponseCommand: supersede a response (activity), part 3 of 5" "Activity diagram converted from fence F127 (line 3520)." {
    include strSupersedeAgentResponseCommand_BothFoundReplacementValid strSupersedeAgentResponseCommand_404NotFoundResponse strSupersedeAgentResponseCommand_409AgentContextSupersedeInvalid strSupersedeAgentResponseCommand_ResponseStatusTransitionOriginalToSuperseded
    autolayout lr 250 150
}
custom "SupersedeAgentResponseCommand-part4" "SupersedeAgentResponseCommand: supersede a response (activity), part 4 of 5" "Activity diagram converted from fence F127 (line 3520)." {
    include strSupersedeAgentResponseCommand_ResponseStatusTransitionOriginalToSuperseded strSupersedeAgentResponseCommand_200ResponseUnchanged strSupersedeAgentResponseCommand_409AgentContextTransitionInvalid strSupersedeAgentResponseCommand_RelationshipWriterSupersedesCycleRule
    autolayout lr 250 150
}
custom "SupersedeAgentResponseCommand-part5" "SupersedeAgentResponseCommand: supersede a response (activity), part 5 of 5" "Activity diagram converted from fence F127 (line 3520)." {
    include strSupersedeAgentResponseCommand_RelationshipWriterSupersedesCycleRule strSupersedeAgentResponseCommand_409AgentContextSupersedeInvalid strSupersedeAgentResponseCommand_200ResponseStatusSuperseded
    autolayout lr 250 150
}
custom "EvaluateStalenessCommand" "EvaluateStalenessCommand: evaluate staleness (activity), part 1 of 2" "Activity diagram converted from fence F128 (line 3551)." {
    include strEvaluateStalenessCommand_HandlerRunsPOSTStalenessEvaluate strEvaluateStalenessCommand_ResponseIdListValidateAndSelectNoneListed strEvaluateStalenessCommand_400ValidationError strEvaluateStalenessCommand_ForEachSelectedResponseNestedEvaluateOneResponseStaleness
    autolayout tb 100 100
}
custom "EvaluateStalenessCommand-part2" "EvaluateStalenessCommand: evaluate staleness (activity), part 2 of 2" "Activity diagram converted from fence F128 (line 3551)." {
    include strEvaluateStalenessCommand_ForEachSelectedResponseNestedEvaluateOneResponseStaleness strEvaluateStalenessCommand_200EvaluateStalenessResultEvaluatedCountNewlyStaleNotFound strEvaluateStalenessCommand_409ConflictRetryableTrue
    autolayout lr 250 150
}
custom "EvaluateOneResponseStaleness" "Nested activity: EvaluateOneResponseStaleness (loop body of EvaluateStalenessCommand) (activity), part 1 of 3" "Activity diagram converted from fence F129 (line 3564)." {
    include strEvaluateOneResponseStaleness_NextSelectedResponse strEvaluateOneResponseStaleness_IResponseStalenessRuleReadOnlyNoTransaction strEvaluateOneResponseStaleness_ContinueWithTheNextResponse strEvaluateOneResponseStaleness_SerializableWriteLockingTheResponse
    autolayout tb 100 100
}
custom "EvaluateOneResponseStaleness-part2" "Nested activity: EvaluateOneResponseStaleness (loop body of EvaluateStalenessCommand) (activity), part 2 of 3" "Activity diagram converted from fence F129 (line 3564)." {
    include strEvaluateOneResponseStaleness_SerializableWriteLockingTheResponse strEvaluateOneResponseStaleness_StopTheLoopTheUnitReturns strEvaluateOneResponseStaleness_ResponseAsTheEvaluationSawIt strEvaluateOneResponseStaleness_IResponseStalenessRuleReadOnlyNoTransaction strEvaluateOneResponseStaleness_ResponseStatusTransitionToStale
    autolayout tb 100 100
}
custom "EvaluateOneResponseStaleness-part3" "Nested activity: EvaluateOneResponseStaleness (loop body of EvaluateStalenessCommand) (activity), part 3 of 3" "Activity diagram converted from fence F129 (line 3564)." {
    include strEvaluateOneResponseStaleness_ResponseStatusTransitionToStale strEvaluateOneResponseStaleness_ContinueWithTheNextResponse
    autolayout lr 250 150
}
custom "CreateAgentContextRelationshipCommand" "CreateAgentContextRelationshipCommand: create a relationship (activity), part 1 of 3" "Activity diagram converted from fence F130 (line 3587)." {
    include strCreateAgentContextRelationshipCommand_HandlerRunsPOSTRelationships strCreateAgentContextRelationshipCommand_KnownKindsAndTypeAndNot strCreateAgentContextRelationshipCommand_400AgentContextRelationshipInvalid strCreateAgentContextRelationshipCommand_SourceResolverFromRefToRefToSha256OfThe
    autolayout tb 100 100
}
custom "CreateAgentContextRelationshipCommand-part2" "CreateAgentContextRelationshipCommand: create a relationship (activity), part 2 of 3" "Activity diagram converted from fence F130 (line 3587)." {
    include strCreateAgentContextRelationshipCommand_SourceResolverFromRefToRefToSha256OfThe strCreateAgentContextRelationshipCommand_400ValidationErrorDetailsFieldFromRef strCreateAgentContextRelationshipCommand_SerializableWriteDatabaseOnly strCreateAgentContextRelationshipCommand_409ConflictRetryableTrue strCreateAgentContextRelationshipCommand_RelationshipWriterCreatedByTheCallerKeyRel
    autolayout tb 100 100
}
custom "CreateAgentContextRelationshipCommand-part3" "CreateAgentContextRelationshipCommand: create a relationship (activity), part 3 of 3" "Activity diagram converted from fence F130 (line 3587)." {
    include strCreateAgentContextRelationshipCommand_RelationshipWriterCreatedByTheCallerKeyRel strCreateAgentContextRelationshipCommand_400ValidationErrorDetailsFieldType strCreateAgentContextRelationshipCommand_200TheStoredLinkCreatedFalse strCreateAgentContextRelationshipCommand_201LinkCreatedTrue
    autolayout lr 250 150
}
custom "DeleteAgentContextRelationshipCommand" "DeleteAgentContextRelationshipCommand: delete a relationship (activity), part 1 of 2" "Activity diagram converted from fence F131 (line 3611)." {
    include strDeleteAgentContextRelationshipCommand_HandlerRunsDELETERelationship strDeleteAgentContextRelationshipCommand_LinkFoundInTheWorkspace strDeleteAgentContextRelationshipCommand_404NotFoundRelationship strDeleteAgentContextRelationshipCommand_RelationshipWriterProtectionCreatedByNamesACommand
    autolayout tb 100 100
}
custom "DeleteAgentContextRelationshipCommand-part2" "DeleteAgentContextRelationshipCommand: delete a relationship (activity), part 2 of 2" "Activity diagram converted from fence F131 (line 3611)." {
    include strDeleteAgentContextRelationshipCommand_RelationshipWriterProtectionCreatedByNamesACommand strDeleteAgentContextRelationshipCommand_409AgentContextRelationshipProtected strDeleteAgentContextRelationshipCommand_SoftDeleteTheLink strDeleteAgentContextRelationshipCommand_204
    autolayout tb 100 100
}
custom "QueryAgentContextRelationshipsQuery" "QueryAgentContextRelationshipsQuery: query relationships (activity), part 1 of 4" "Activity diagram converted from fence F132 (line 3628)." {
    include strQueryAgentContextRelationshipsQuery_HandlerRunsGETRelationships strQueryAgentContextRelationshipsQuery_FiltersValidNodeKindWithNodeRefKnown strQueryAgentContextRelationshipsQuery_400ValidationError strQueryAgentContextRelationshipsQuery_Direction
    autolayout tb 100 100
}
custom "QueryAgentContextRelationshipsQuery-part2" "QueryAgentContextRelationshipsQuery: query relationships (activity), part 2 of 4" "Activity diagram converted from fence F132 (line 3628)." {
    include strQueryAgentContextRelationshipsQuery_Direction strQueryAgentContextRelationshipsQuery_LinksWhereTheNodeIsFrom strQueryAgentContextRelationshipsQuery_LinksWhereTheNodeIsTo strQueryAgentContextRelationshipsQuery_UnionOfBoth
    autolayout lr 250 150
}
custom "QueryAgentContextRelationshipsQuery-part3" "QueryAgentContextRelationshipsQuery: query relationships (activity), part 3 of 4" "Activity diagram converted from fence F132 (line 3628)." {
    include strQueryAgentContextRelationshipsQuery_LinksWhereTheNodeIsFrom strQueryAgentContextRelationshipsQuery_PagedQueryPageBoundsRefuseFilterByType strQueryAgentContextRelationshipsQuery_LinksWhereTheNodeIsTo strQueryAgentContextRelationshipsQuery_UnionOfBoth
    autolayout lr 250 150
}
custom "QueryAgentContextRelationshipsQuery-part4" "QueryAgentContextRelationshipsQuery: query relationships (activity), part 4 of 4" "Activity diagram converted from fence F132 (line 3628)." {
    include strQueryAgentContextRelationshipsQuery_PagedQueryPageBoundsRefuseFilterByType strQueryAgentContextRelationshipsQuery_400ValidationError strQueryAgentContextRelationshipsQuery_200PageResultOfAgentContextRelationshipDto
    autolayout lr 250 150
}
custom "RetrieveAgentContextCandidatesQuery" "RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 1 of 10" "Activity diagram converted from fence F133 (line 3651)." {
    include strRetrieveAgentContextCandidatesQuery_HandlerRunsPOSTRetrieve strRetrieveAgentContextCandidatesQuery_RequestValidAgentSessionIdAndRequestId strRetrieveAgentContextCandidatesQuery_400ValidationError strRetrieveAgentContextCandidatesQuery_TurnNamed
    autolayout tb 100 100
}
custom "RetrieveAgentContextCandidatesQuery-part2" "RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 2 of 10" "Activity diagram converted from fence F133 (line 3651)." {
    include strRetrieveAgentContextCandidatesQuery_TurnNamed strRetrieveAgentContextCandidatesQuery_TurnLocatorAgentSessionIdRequestId strRetrieveAgentContextCandidatesQuery_PreferGraphRag
    autolayout tb 100 200
}
custom "RetrieveAgentContextCandidatesQuery-part3" "RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 3 of 10" "Activity diagram converted from fence F133 (line 3651)." {
    include strRetrieveAgentContextCandidatesQuery_TurnLocatorAgentSessionIdRequestId strRetrieveAgentContextCandidatesQuery_400ValidationError strRetrieveAgentContextCandidatesQuery_404NotFoundSessionTurn strRetrieveAgentContextCandidatesQuery_PreferGraphRag
    autolayout lr 250 150
}
custom "RetrieveAgentContextCandidatesQuery-part4" "RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 4 of 10" "Activity diagram converted from fence F133 (line 3651)." {
    include strRetrieveAgentContextCandidatesQuery_PreferGraphRag strRetrieveAgentContextCandidatesQuery_RelationalPathCandidatesOtherThanThe strRetrieveAgentContextCandidatesQuery_GraphRagUseForRetrieval strRetrieveAgentContextCandidatesQuery_ScoreLinkedByRelationshipsToThe
    autolayout tb 100 100
}
custom "RetrieveAgentContextCandidatesQuery-part5" "RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 5 of 10" "Activity diagram converted from fence F133 (line 3651)." {
    include strRetrieveAgentContextCandidatesQuery_GraphRagUseForRetrieval strRetrieveAgentContextCandidatesQuery_FallbackReasonRetrievalDisabled strRetrieveAgentContextCandidatesQuery_KindsNamesOnlyPriorResponse strRetrieveAgentContextCandidatesQuery_ScoreLinkedByRelationshipsToThe strRetrieveAgentContextCandidatesQuery_IResponseStalenessRuleReadOnlyKeepPriorResponseCandidates
    autolayout lr 250 150
}
custom "RetrieveAgentContextCandidatesQuery-part6" "RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 6 of 10" "Activity diagram converted from fence F133 (line 3651)." {
    include strRetrieveAgentContextCandidatesQuery_FallbackReasonRetrievalDisabled strRetrieveAgentContextCandidatesQuery_RelationalPathCandidatesOtherThanThe strRetrieveAgentContextCandidatesQuery_KindsNamesOnlyPriorResponse strRetrieveAgentContextCandidatesQuery_FallbackReasonGraphragKindsUnsupported strRetrieveAgentContextCandidatesQuery_IGraphRagServiceGetStatusAsync
    autolayout tb 100 100
}
custom "RetrieveAgentContextCandidatesQuery-part7" "RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 7 of 10" "Activity diagram converted from fence F133 (line 3651)." {
    include strRetrieveAgentContextCandidatesQuery_IResponseStalenessRuleReadOnlyKeepPriorResponseCandidates strRetrieveAgentContextCandidatesQuery_CandidateRankerThenCutToLimit strRetrieveAgentContextCandidatesQuery_FallbackReasonGraphragKindsUnsupported strRetrieveAgentContextCandidatesQuery_RelationalPathCandidatesOtherThanThe strRetrieveAgentContextCandidatesQuery_IGraphRagServiceGetStatusAsync strRetrieveAgentContextCandidatesQuery_EnabledAndIndexed
    autolayout lr 250 150
}
custom "RetrieveAgentContextCandidatesQuery-part8" "RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 8 of 10" "Activity diagram converted from fence F133 (line 3651)." {
    include strRetrieveAgentContextCandidatesQuery_CandidateRankerThenCutToLimit strRetrieveAgentContextCandidatesQuery_200AgentContextRetrieveResultRetrievalPathFallbackReasonWhenA strRetrieveAgentContextCandidatesQuery_EnabledAndIndexed strRetrieveAgentContextCandidatesQuery_FallbackReasonFromTheStatus strRetrieveAgentContextCandidatesQuery_IGraphRagServiceQueryAsyncOverTheProjectedResponses
    autolayout lr 250 150
}
custom "RetrieveAgentContextCandidatesQuery-part9" "RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 9 of 10" "Activity diagram converted from fence F133 (line 3651)." {
    include strRetrieveAgentContextCandidatesQuery_FallbackReasonFromTheStatus strRetrieveAgentContextCandidatesQuery_RelationalPathCandidatesOtherThanThe strRetrieveAgentContextCandidatesQuery_IGraphRagServiceQueryAsyncOverTheProjectedResponses strRetrieveAgentContextCandidatesQuery_QueryResult
    autolayout lr 250 150
}
custom "RetrieveAgentContextCandidatesQuery-part10" "RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 10 of 10" "Activity diagram converted from fence F133 (line 3651)." {
    include strRetrieveAgentContextCandidatesQuery_QueryResult strRetrieveAgentContextCandidatesQuery_FallbackReasonFromTheResultOrGraphrag strRetrieveAgentContextCandidatesQuery_KeepHitsNamingAResponseOf strRetrieveAgentContextCandidatesQuery_RelationalPathCandidatesOtherThanThe strRetrieveAgentContextCandidatesQuery_IResponseStalenessRuleReadOnlyKeepPriorResponseCandidates
    autolayout tb 100 100
}
custom "ProjectAgentResponsesCommand" "ProjectAgentResponsesCommand: project responses into GraphRAG (activity), part 1 of 2" "Activity diagram converted from fence F134 (line 3693)." {
    include strProjectAgentResponsesCommand_HandlerRunsPOSTProjectionGraphrag strProjectAgentResponsesCommand_ResponseIdListValidateAndSelectNoneListed strProjectAgentResponsesCommand_400ValidationError strProjectAgentResponsesCommand_ForEachSelectedResponseNestedProjectOneResponse
    autolayout tb 100 100
}
custom "ProjectAgentResponsesCommand-part2" "ProjectAgentResponsesCommand: project responses into GraphRAG (activity), part 2 of 2" "Activity diagram converted from fence F134 (line 3693)." {
    include strProjectAgentResponsesCommand_ForEachSelectedResponseNestedProjectOneResponse strProjectAgentResponsesCommand_ReconcileProjectedDocumentsNestedBelow strProjectAgentResponsesCommand_409ConflictRetryableTrue strProjectAgentResponsesCommand_200ProjectAgentResponsesResultProjectedRemovedSkippedSkippedReason
    autolayout tb 100 100
}
custom "ProjectOneResponse" "Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand) (activity), part 1 of 5" "Activity diagram converted from fence F135 (line 3706)." {
    include strProjectOneResponse_NextSelectedResponse strProjectOneResponse_DispatchEvaluateStalenessCommandForThisResponseThrough strProjectOneResponse_StopTheLoopTheUnitReturns strProjectOneResponse_ResponseTrusted
    autolayout tb 100 100
}
custom "ProjectOneResponse-part2" "Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand) (activity), part 2 of 5" "Activity diagram converted from fence F135 (line 3706)." {
    include strProjectOneResponse_ResponseTrusted strProjectOneResponse_GraphRAGEnabledAndProjectionEnabled strProjectOneResponse_RowHoldsAGraphRagDocumentId strProjectOneResponse_SerializableWriteLockingTheResponseReRead strProjectOneResponse_ProjectedContentHashEqualsContentSha256
    autolayout tb 100 100
}
custom "ProjectOneResponse-part3" "Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand) (activity), part 3 of 5" "Activity diagram converted from fence F135 (line 3706)." {
    include strProjectOneResponse_RowHoldsAGraphRagDocumentId strProjectOneResponse_SerializableWriteLockingTheResponseReRead strProjectOneResponse_GraphRAGEnabled strProjectOneResponse_ContinueWithTheNextResponse strProjectOneResponse_StopTheLoopTheUnitReturns
    autolayout tb 100 100
}
custom "ProjectOneResponse-part4" "Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand) (activity), part 4 of 5" "Activity diagram converted from fence F135 (line 3706)." {
    include strProjectOneResponse_ProjectedContentHashEqualsContentSha256 strProjectOneResponse_ContinueWithTheNextResponse strProjectOneResponse_IGraphRagServiceIngestTextAsyncInGraphRagStorageScopeWorkspaceWith strProjectOneResponse_GraphRAGEnabled strProjectOneResponse_IGraphRagServiceDeleteDocumentAsync
    autolayout lr 250 150
}
custom "ProjectOneResponse-part5" "Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand) (activity), part 5 of 5" "Activity diagram converted from fence F135 (line 3706)." {
    include strProjectOneResponse_IGraphRagServiceIngestTextAsyncInGraphRagStorageScopeWorkspaceWith strProjectOneResponse_SerializableWriteLockingTheResponseReRead strProjectOneResponse_IGraphRagServiceDeleteDocumentAsync
    autolayout lr 250 150
}
custom "ReconcileProjectedDocuments" "Nested activity: ReconcileProjectedDocuments (after the loop of ProjectAgentResponsesCommand) (activity), part 1 of 3" "Activity diagram converted from fence F136 (line 3729)." {
    include strReconcileProjectedDocuments_LoopDoneT0IsThePass strReconcileProjectedDocuments_GraphRAGEnabled strReconcileProjectedDocuments_NothingReconciledRemovalsWaitUntilGraphRAG strReconcileProjectedDocuments_ListDocumentsAsyncWithSourceTypeAgentResponsePage
    autolayout tb 100 100
}
custom "ReconcileProjectedDocuments-part2" "Nested activity: ReconcileProjectedDocuments (after the loop of ProjectAgentResponsesCommand) (activity), part 2 of 3" "Activity diagram converted from fence F136 (line 3729)." {
    include strReconcileProjectedDocuments_ListDocumentsAsyncWithSourceTypeAgentResponsePage strReconcileProjectedDocuments_ReadTheProjectedRowsResponseId strReconcileProjectedDocuments_DocumentsThatNoProjectedRowNames strReconcileProjectedDocuments_DeleteDocumentAsyncEachAnErrorIsLogged strReconcileProjectedDocuments_ProjectedRowsWhoseDocumentTheListing
    autolayout tb 100 200
}
custom "ReconcileProjectedDocuments-part3" "Nested activity: ReconcileProjectedDocuments (after the loop of ProjectAgentResponsesCommand) (activity), part 3 of 3" "Activity diagram converted from fence F136 (line 3729)." {
    include strReconcileProjectedDocuments_ProjectedRowsWhoseDocumentTheListing strReconcileProjectedDocuments_Reconciled strReconcileProjectedDocuments_SerializableWritePerRowLockingItsResponse strReconcileProjectedDocuments_TheUnitReturns409ConflictRetryable
    autolayout tb 100 100
}
custom "GetAgentContextPilotReportQuery" "GetAgentContextPilotReportQuery: pilot report (activity), part 1 of 2" "Activity diagram converted from fence F137 (line 3752)." {
    include strGetAgentContextPilotReportQuery_HandlerRunsGETPilotReport strGetAgentContextPilotReportQuery_FromUtcAndToUtcValidUTCAnd strGetAgentContextPilotReportQuery_400ValidationError strGetAgentContextPilotReportQuery_IResponseStalenessRuleReadOnlyEffectiveStatusOf
    autolayout tb 100 100
}
custom "GetAgentContextPilotReportQuery-part2" "GetAgentContextPilotReportQuery: pilot report (activity), part 2 of 2" "Activity diagram converted from fence F137 (line 3752)." {
    include strGetAgentContextPilotReportQuery_IResponseStalenessRuleReadOnlyEffectiveStatusOf strGetAgentContextPilotReportQuery_GroupTheResponsesByInvocationMode strGetAgentContextPilotReportQuery_PerModeCountsByEffectiveStatus strGetAgentContextPilotReportQuery_CountOverflowPackagesPackagesWithoutResponseRefusedPrompts strGetAgentContextPilotReportQuery_200AgentContextPilotReport
    autolayout tb 100 200
}
custom "CreateAgentContextHvRequestCommand" "CreateAgentContextHvRequestCommand: create an HV request for a range of turns (activity), part 1 of 4" "Activity diagram converted from fence F138 (line 3776)." {
    include strCreateAgentContextHvRequestCommand_HandlerRunsPOSTHvRequests strCreateAgentContextHvRequestCommand_EveryRequestFieldNonblank strCreateAgentContextHvRequestCommand_400ValidationError strCreateAgentContextHvRequestCommand_SerializableWriteLockingTheRangeSResponses
    autolayout tb 100 100
}
custom "CreateAgentContextHvRequestCommand-part2" "CreateAgentContextHvRequestCommand: create an HV request for a range of turns (activity), part 2 of 4" "Activity diagram converted from fence F138 (line 3776)." {
    include strCreateAgentContextHvRequestCommand_SerializableWriteLockingTheRangeSResponses strCreateAgentContextHvRequestCommand_409ConflictRetryableTrue strCreateAgentContextHvRequestCommand_IdempotentCreateLookup
    autolayout lr 250 150
}
custom "CreateAgentContextHvRequestCommand-part3" "CreateAgentContextHvRequestCommand: create an HV request for a range of turns (activity), part 3 of 4" "Activity diagram converted from fence F138 (line 3776)." {
    include strCreateAgentContextHvRequestCommand_IdempotentCreateLookup strCreateAgentContextHvRequestCommand_200TheStoredRequestCurrentItems strCreateAgentContextHvRequestCommand_409ConflictRetryableFalse strCreateAgentContextHvRequestCommand_SelectHvRequestItems
    autolayout lr 250 150
}
custom "CreateAgentContextHvRequestCommand-part4" "CreateAgentContextHvRequestCommand: create an HV request for a range of turns (activity), part 4 of 4" "Activity diagram converted from fence F138 (line 3776)." {
    include strCreateAgentContextHvRequestCommand_SelectHvRequestItems strCreateAgentContextHvRequestCommand_RefusalOfSelectHvRequestItemsDrawnThere strCreateAgentContextHvRequestCommand_InsertTheHVRequestOpenAnd strCreateAgentContextHvRequestCommand_SerializableWriteLockingTheRangeSResponses strCreateAgentContextHvRequestCommand_201AgentContextHvRequestResult
    autolayout tb 100 100
}
custom "SelectHvRequestItems" "Nested activity: SelectHvRequestItems (turn, range, and item selection inside the SerializableWrite delegate of CreateAgentContextHvRequestCommand) (activity), part 1 of 3" "Activity diagram converted from fence F139 (line 3801)." {
    include strSelectHvRequestItems_SelectHvRequestItemsInTheDelegate strSelectHvRequestItems_TurnLocatorSessionThenBothEndpointTurns
    autolayout lr 250 150
}
custom "SelectHvRequestItems-part2" "Nested activity: SelectHvRequestItems (turn, range, and item selection inside the SerializableWrite delegate of CreateAgentContextHvRequestCommand) (activity), part 2 of 3" "Activity diagram converted from fence F139 (line 3801)." {
    include strSelectHvRequestItems_TurnLocatorSessionThenBothEndpointTurns strSelectHvRequestItems_400ValidationError strSelectHvRequestItems_400AgentContextHvRequestInvalid strSelectHvRequestItems_FromNotAfterToWithinMaxTurns strSelectHvRequestItems_KeepTheGradableResponsesEachPending
    autolayout tb 100 100
}
custom "SelectHvRequestItems-part3" "Nested activity: SelectHvRequestItems (turn, range, and item selection inside the SerializableWrite delegate of CreateAgentContextHvRequestCommand) (activity), part 3 of 3" "Activity diagram converted from fence F139 (line 3801)." {
    include strSelectHvRequestItems_KeepTheGradableResponsesEachPending strSelectHvRequestItems_AtLeastOneResponseKept strSelectHvRequestItems_400AgentContextHvRequestInvalid strSelectHvRequestItems_ItemsSelectedEachPending
    autolayout tb 100 100
}
custom "GetAgentContextHvRequestQuery" "GetAgentContextHvRequestQuery: get an HV request (activity), part 1 of 2" "Activity diagram converted from fence F140 (line 3821)." {
    include strGetAgentContextHvRequestQuery_HandlerRunsGETHvRequestsHvRequestId strGetAgentContextHvRequestQuery_HVRequestFoundInTheWorkspace strGetAgentContextHvRequestQuery_404NotFoundHvRequest strGetAgentContextHvRequestQuery_LoadItemsWithTheirDispositions
    autolayout tb 100 100
}
custom "GetAgentContextHvRequestQuery-part2" "GetAgentContextHvRequestQuery: get an HV request (activity), part 2 of 2" "Activity diagram converted from fence F140 (line 3821)." {
    include strGetAgentContextHvRequestQuery_LoadItemsWithTheirDispositions strGetAgentContextHvRequestQuery_200AgentContextHvRequestResult
    autolayout lr 250 150
}
custom "QueryAgentContextHvRequestsQuery" "QueryAgentContextHvRequestsQuery: query HV requests (activity), part 1 of 2" "Activity diagram converted from fence F141 (line 3836)." {
    include strQueryAgentContextHvRequestsQuery_HandlerRunsGETHvRequests strQueryAgentContextHvRequestsQuery_StatusFilterAbsentOrKnown strQueryAgentContextHvRequestsQuery_400ValidationError strQueryAgentContextHvRequestsQuery_PagedQueryPageBoundsRefuseFilterBySessionId
    autolayout tb 100 200
}
custom "QueryAgentContextHvRequestsQuery-part2" "QueryAgentContextHvRequestsQuery: query HV requests (activity), part 2 of 2" "Activity diagram converted from fence F141 (line 3836)." {
    include strQueryAgentContextHvRequestsQuery_PagedQueryPageBoundsRefuseFilterBySessionId strQueryAgentContextHvRequestsQuery_400ValidationError strQueryAgentContextHvRequestsQuery_200PageResultOfHVRequestsWithout
    autolayout lr 250 150
}
custom "RunStatelessInvocation" "Process RunStatelessInvocation: run a stateless invocation (plugin process) (activity), part 1 of 6" "Activity diagram converted from fence F142 (line 3863)." {
    include strRunStatelessInvocation_PromptHookOrADeliveryAbsorbed strRunStatelessInvocation_ResolveInvocationTurn
    autolayout lr 250 150
}
custom "RunStatelessInvocation-part2" "Process RunStatelessInvocation: run a stateless invocation (plugin process) (activity), part 2 of 6" "Activity diagram converted from fence F142 (line 3863)." {
    include strRunStatelessInvocation_ResolveInvocationTurn strRunStatelessInvocation_StatefulTurnAsToday strRunStatelessInvocation_PromptRefusedNoTurnToClose strRunStatelessInvocation_FailInvocation strRunStatelessInvocation_BuildAgentContextPackageCommand
    autolayout lr 200 50
}
custom "RunStatelessInvocation-part3" "Process RunStatelessInvocation: run a stateless invocation (plugin process) (activity), part 3 of 6" "Activity diagram converted from fence F142 (line 3863)." {
    include strRunStatelessInvocation_FailInvocation strRunStatelessInvocation_InvocationFailedNoAnswerDrawnThere
    autolayout lr 250 150
}
custom "RunStatelessInvocation-part4" "Process RunStatelessInvocation: run a stateless invocation (plugin process) (activity), part 4 of 6" "Activity diagram converted from fence F142 (line 3863)." {
    include strRunStatelessInvocation_BuildAgentContextPackageCommand strRunStatelessInvocation_CacheModeOffThroughUpdateTurnState strRunStatelessInvocation_AbsorbedRequest strRunStatelessInvocation_AnswerAndSubmit
    autolayout tb 100 100
}
custom "RunStatelessInvocation-part5" "Process RunStatelessInvocation: run a stateless invocation (plugin process) (activity), part 5 of 6" "Activity diagram converted from fence F142 (line 3863)." {
    include strRunStatelessInvocation_CacheModeOffThroughUpdateTurnState strRunStatelessInvocation_StatefulTurnAsToday strRunStatelessInvocation_AbsorbedRequest strRunStatelessInvocation_FailInvocation strRunStatelessInvocation_FailureInThePerTurnFile
    autolayout lr 250 150
}
custom "RunStatelessInvocation-part6" "Process RunStatelessInvocation: run a stateless invocation (plugin process) (activity), part 6 of 6" "Activity diagram converted from fence F142 (line 3863)." {
    include strRunStatelessInvocation_AnswerAndSubmit strRunStatelessInvocation_CacheModeOffThroughUpdateTurnState strRunStatelessInvocation_OutcomeOfAnswerAndSubmitDrawnThere
    autolayout lr 250 150
}
custom "ResolveInvocationTurn" "Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build) (activity), part 1 of 5" "Activity diagram converted from fence F143 (line 3885)." {
    include strResolveInvocationTurn_ResolveInvocationTurn strResolveInvocationTurn_LoggingSupportedModeNotOff strResolveInvocationTurn_StatefulTurnAsToday strResolveInvocationTurn_HookCanRefuseProbeJ
    autolayout tb 100 100
}
custom "ResolveInvocationTurn-part2" "Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build) (activity), part 2 of 5" "Activity diagram converted from fence F143 (line 3885)." {
    include strResolveInvocationTurn_HookCanRefuseProbeJ strResolveInvocationTurn_StatefulTurnAsToday2 strResolveInvocationTurn_Trigger
    autolayout lr 250 150
}
custom "ResolveInvocationTurn-part3" "Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build) (activity), part 3 of 5" "Activity diagram converted from fence F143 (line 3885)." {
    include strResolveInvocationTurn_Trigger strResolveInvocationTurn_TurnBoundBuildAsPilot strResolveInvocationTurn_SyncPluginArrivalLog strResolveInvocationTurn_ResolveMintOutcome
    autolayout lr 250 150
}
custom "ResolveInvocationTurn-part4" "Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build) (activity), part 4 of 5" "Activity diagram converted from fence F143 (line 3885)." {
    include strResolveInvocationTurn_SyncPluginArrivalLog strResolveInvocationTurn_PromptRefusedNoTurn strResolveInvocationTurn_TurnBoundBuild
    autolayout lr 250 150
}
custom "ResolveInvocationTurn-part5" "Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build) (activity), part 5 of 5" "Activity diagram converted from fence F143 (line 3885)." {
    include strResolveInvocationTurn_ResolveMintOutcome strResolveInvocationTurn_PromptRefusedNoTurn2 strResolveInvocationTurn_StatefulTurn strResolveInvocationTurn_FailTheInvocation strResolveInvocationTurn_TurnBoundBuild2
    autolayout lr 200 50
}
custom "FailInvocation" "Nested activity: FailInvocation (refusal and canceled close of RunStatelessInvocation) (activity), part 1 of 2" "Activity diagram converted from fence F144 (line 3907)." {
    include strFailInvocation_FailInvocationTheFailureSCodeAnd strFailInvocation_RefuseThePromptCodeMessageCounts strFailInvocation_ProcessPersistTurnWriteWithReceiptCloseCanceled
    autolayout tb 100 200
}
custom "FailInvocation-part2" "Nested activity: FailInvocation (refusal and canceled close of RunStatelessInvocation) (activity), part 2 of 2" "Activity diagram converted from fence F144 (line 3907)." {
    include strFailInvocation_ProcessPersistTurnWriteWithReceiptCloseCanceled strFailInvocation_FailedClearedFromOpenDeliveredAndThe strFailInvocation_FailedCloseInTheFailsafeStop strFailInvocation_FailedReceiptShownTurnStaysOpen
    autolayout lr 250 150
}
custom "AnswerAndSubmit" "Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation) (activity), part 1 of 5" "Activity diagram converted from fence F145 (line 3920)." {
    include strAnswerAndSubmit_AnswerAndSubmitTheBuiltPackage strAnswerAndSubmit_KeepPackageIdInTurnsRequestIdYaml strAnswerAndSubmit_InjectThePackageForThisRequest
    autolayout tb 100 200
}
custom "AnswerAndSubmit-part2" "Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation) (activity), part 2 of 5" "Activity diagram converted from fence F145 (line 3920)." {
    include strAnswerAndSubmit_InjectThePackageForThisRequest strAnswerAndSubmit_NoInjectionInvocationModeStateful strAnswerAndSubmit_ModelInputPackageRequestMemories strAnswerAndSubmit_ModelCallsAgentContextSubmitResponse
    autolayout tb 100 100
}
custom "AnswerAndSubmit-part3" "Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation) (activity), part 3 of 5" "Activity diagram converted from fence F145 (line 3920)." {
    include strAnswerAndSubmit_ModelCallsAgentContextSubmitResponse strAnswerAndSubmit_SubmitResult
    autolayout lr 250 150
}
custom "AnswerAndSubmit-part4" "Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation) (activity), part 4 of 5" "Activity diagram converted from fence F145 (line 3920)." {
    include strAnswerAndSubmit_SubmitResult strAnswerAndSubmit_ReturnedModeOffToCache strAnswerAndSubmit_InTheToolResultTurnUntouched strAnswerAndSubmit_RequestRunDeterministicGradeCommand
    autolayout lr 250 150
}
custom "AnswerAndSubmit-part5" "Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation) (activity), part 5 of 5" "Activity diagram converted from fence F145 (line 3920)." {
    include strAnswerAndSubmit_RequestRunDeterministicGradeCommand strAnswerAndSubmit_GradeResult strAnswerAndSubmit_GradingUntilAnHVModelGrade strAnswerAndSubmit_InTheToolResultTurnUntouched2
    autolayout tb 100 100
}
dynamic qbrain.server "RequestPipeline-Sequence" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "request with X-Workspace-Path or workspacePath"
    autolayout lr 300 100
}
dynamic qbrain.server "RequestPipeline-Sequence-part2" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), part 2 of 4"
    qbrain.server.api -> qbrain.server.strWorkspaceResolutionBehavior "SendAsync(command) or QueryAsync(query)"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "(1 of 2) Note over WorkspaceResolutionBehavior and"
    autolayout lr 300 100
}
dynamic qbrain.server "RequestPipeline-Sequence-part3" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), part 3 of 4"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "(2 of 2) ModeGateBehavior: inside IDispatcher, before the handler"
    autolayout lr 300 100
}
dynamic qbrain.server "RequestPipeline-Sequence-part4" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), part 4 of 4"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "RequestPipeline-Sequence-alt1-1" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), alt 1 branch 1 of 2: path unregistered, or no workspace and not allowed, part 1 of 2"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "alt 1 branch 1 of 2: path unregistered, or no workspace and not allowed"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.api "Failure, ClassifiedError 400"
    autolayout lr 300 100
}
dynamic qbrain.server "RequestPipeline-Sequence-alt1-1-part2" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), alt 1 branch 1 of 2: path unregistered, or no workspace and not allowed, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field workspacePath"
    qbrain.libs.client -> qbrain.libs.client "Note: fix X-Workspace-Path or workspacePath"
    autolayout lr 300 100
}
dynamic qbrain.server "RequestPipeline-Sequence-alt1-2" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), alt 1 branch 2 of 2: resolved into the scope's WorkspaceContext"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior "alt 1 branch 2 of 2: resolved into the scope's WorkspaceContext"
    qbrain.server.strWorkspaceResolutionBehavior -> qbrain.server.strModeGateBehavior "next"
    qbrain.server.strModeGateBehavior -> qbrain.server.strModeGateBehavior "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "RequestPipeline-Sequence-alt2-1" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), alt 2 branch 1 of 2: record implements IModeGatedRequest and Mode is Off, part 1 of 3"
    qbrain.server.strModeGateBehavior -> qbrain.server.strModeGateBehavior "(1 of 2) alt 2 branch 1 of 2: record implements IModeGatedRequest and Mode is"
    autolayout lr 300 100
}
dynamic qbrain.server "RequestPipeline-Sequence-alt2-1-part2" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), alt 2 branch 1 of 2: record implements IModeGatedRequest and Mode is Off, part 2 of 3"
    qbrain.server.strModeGateBehavior -> qbrain.server.strModeGateBehavior "(2 of 2) Off"
    qbrain.server.strModeGateBehavior -> qbrain.server.api "Failure, ClassifiedError 409"
    autolayout lr 300 100
}
dynamic qbrain.server "RequestPipeline-Sequence-alt2-1-part3" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), alt 2 branch 1 of 2: record implements IModeGatedRequest and Mode is Off, part 3 of 3"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_disabled"
    qbrain.libs.client -> qbrain.libs.client "Note: a plugin caches Off for the host session and continues statefully"
    autolayout lr 300 100
}
dynamic qbrain.server "RequestPipeline-Sequence-alt2-2" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), alt 2 branch 2 of 2: not gated, or Mode is Pilot or Stateless, part 1 of 3"
    qbrain.server.strModeGateBehavior -> qbrain.server.strModeGateBehavior "alt 2 branch 2 of 2: not gated, or Mode is Pilot or Stateless"
    qbrain.server.strModeGateBehavior -> qbrain.server.strAgentContextHandlers "Handle(record)"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: request validation, then the unit's steps"
    autolayout lr 300 100
}
dynamic qbrain.server "RequestPipeline-Sequence-alt2-2-part2" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), alt 2 branch 2 of 2: not gated, or Mode is Pilot or Stateless, part 2 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.api "Result, success or ClassifiedErrorException"
    autolayout lr 300 100
}
dynamic qbrain.server "RequestPipeline-Sequence-alt2-2-part3" "Sequence diagram converted from fence F146 (line 3957)." {
    title "Sequence: RequestPipeline (every Phase C dispatch), alt 2 branch 2 of 2: not gated, or Mode is Pilot or Stateless, part 3 of 3"
    qbrain.server.api -> qbrain.libs.client "status and body"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-09" "Sequence diagram converted from fence F147 (line 3991)." {
    title "Sequence: submit GetAgentContextStatusQuery"
    qbrain.libs.client -> qbrain.server.api "GET /status"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-09-alt1-1" "Sequence diagram converted from fence F147 (line 3991)." {
    title "Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 1 of 7"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: server answers"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "QueryAsync(GetAgentContextStatusQuery)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-09-alt1-1-part2" "Sequence diagram converted from fence F147 (line 3991)." {
    title "Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 2 of 7"
    qbrain.server.api -> qbrain.server.api "(1 of 2) Note over Controller and Handler: ref RequestPipeline, not gated:"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-09-alt1-1-part3" "Sequence diagram converted from fence F147 (line 3991)." {
    title "Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 3 of 7"
    qbrain.server.api -> qbrain.server.api "(2 of 2) readable in Mode Off"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-09-alt1-1-part4" "Sequence diagram converted from fence F147 (line 3991)." {
    title "Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 4 of 7"
    qbrain.server.strAgentContextHandlers -> qbrain.server.graphrag "GetStatusAsync"
    qbrain.server.graphrag -> qbrain.server.strAgentContextHandlers "enabled, state"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-09-alt1-1-part5" "Sequence diagram converted from fence F147 (line 3991)." {
    title "Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 5 of 7"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "count packages, and responses by stored status"
    qbrain.server.mcpDbContext -> qbrain.server.strAgentContextHandlers "counts, the Submitted, Grading, Trusted responses"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "IResponseStalenessRule, read-only, each dependency read once"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-09-alt1-1-part6" "Sequence diagram converted from fence F147 (line 3991)." {
    title "Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 6 of 7"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: counts by effective status, nothing written"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-09-alt1-1-part7" "Sequence diagram converted from fence F147 (line 3991)." {
    title "Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 7 of 7"
    qbrain.server.api -> qbrain.libs.client "200 AgentContextStatusResult"
    qbrain.libs.client -> qbrain.libs.client "Note: a plugin caches the Mode for the host session, Off means stateful"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-09-alt1-2" "Sequence diagram converted from fence F147 (line 3991)." {
    title "Sequence: submit GetAgentContextStatusQuery, alt 1 branch 2 of 2: outage, any error, or no answer within the hook bound, part 1 of 2"
    qbrain.libs.client -> qbrain.libs.client "alt 1 branch 2 of 2: outage, any error, or no answer within the hook bound"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-09-alt1-2-part2" "Sequence diagram converted from fence F147 (line 3991)." {
    title "Sequence: submit GetAgentContextStatusQuery, alt 1 branch 2 of 2: outage, any error, or no answer within the hook bound, part 2 of 2"
    qbrain.libs.client -> qbrain.libs.client "Note: a plugin caches Off for the host session"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-10" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, part 1 of 3"
    qbrain.libs.client -> qbrain.server.api "POST /packages (BuildAgentContextPackageRequest)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-10-part2" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, part 2 of 3"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "SendAsync(BuildAgentContextPackageCommand)"
    qbrain.server.api -> qbrain.server.api "Note over Controller and Handler: ref RequestPipeline, gated"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-10-part3" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, part 3 of 3"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-10-alt1-1" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, alt 1 branch 1 of 2: request invalid (ids, tokenBudget)"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: request invalid (ids, tokenBudget)"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    qbrain.libs.client -> qbrain.libs.client "Note: fix the field, never resend unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-10-alt1-2" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, alt 1 branch 2 of 2: request valid, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 2: request valid"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "IdempotentCreate.Find, read-only, before any source read"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-10-alt1-2-part2" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, alt 1 branch 2 of 2: request valid, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-10-alt2-1" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 1 of 4: stored package Built (a retry or a duplicate delivery)"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 4: stored package Built (a retry or a duplicate delivery)"
    qbrain.server.api -> qbrain.libs.client "200 AgentContextPackageResult, status Built"
    qbrain.libs.client -> qbrain.libs.client "Note: use the returned package"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-10-alt2-2" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 2 of 4: stored package Overflow (a retry)"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 4: stored package Overflow (a retry)"
    qbrain.server.api -> qbrain.libs.client "422 agent_context_budget_exceeded, stored diagnostics"
    qbrain.libs.client -> qbrain.libs.client "Note: refuse the prompt, close the turn canceled"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-10-alt2-3" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 3 of 4: key reused with other inputs"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 3 of 4: key reused with other inputs"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable false"
    qbrain.libs.client -> qbrain.libs.client "Note: new key, or the same inputs"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-10-alt2-4" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 4 of 4: absent, part 1 of 5"
    qbrain.libs.client -> qbrain.libs.client "alt 2 branch 4 of 4: absent"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-10-alt2-4-part2" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 4 of 4: absent, part 2 of 5"
    qbrain.libs.client -> qbrain.libs.client "(1 of 2) Note over Caller and Store: ref ReadBuildInputs (below), each"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-10-alt2-4-part3" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 4 of 4: absent, part 3 of 5"
    qbrain.libs.client -> qbrain.libs.client "(2 of 2) refusal there answers the caller"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "PackageAssembler: assemble the candidate snapshot"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-10-alt2-4-part4" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 4 of 4: absent, part 4 of 5"
    qbrain.libs.client -> qbrain.libs.client "(1 of 2) Note over Caller and Store: ref SaveBuiltPackage (below),"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-10-alt2-4-part5" "Sequence diagram converted from fence F148 (line 4020)." {
    title "Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 4 of 4: absent, part 5 of 5"
    qbrain.libs.client -> qbrain.libs.client "(2 of 2) SerializableWrite, database only"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadBuildInputs-Sequence" "Sequence diagram converted from fence F149 (line 4056)." {
    title "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "TurnLocator(agent, sessionId, requestId)"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadBuildInputs-Sequence-part2" "Sequence diagram converted from fence F149 (line 4056)." {
    title "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadBuildInputs-Sequence-alt1-1" "Sequence diagram converted from fence F149 (line 4056)." {
    title "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 1 branch 1 of 3: InvalidIdentifier"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 3: InvalidIdentifier"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadBuildInputs-Sequence-alt1-2" "Sequence diagram converted from fence F149 (line 4056)." {
    title "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 1 branch 2 of 3: SessionNotFound or TurnNotFound"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 3: SessionNotFound or TurnNotFound"
    qbrain.server.api -> qbrain.libs.client "404 not_found, details.resource session_turn"
    qbrain.libs.client -> qbrain.libs.client "Note: open or mint the turn first"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadBuildInputs-Sequence-alt1-3" "Sequence diagram converted from fence F149 (line 4056)." {
    title "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 1 branch 3 of 3: turn found, with its status, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 3 of 3: turn found, with its status"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadBuildInputs-Sequence-alt1-3-part2" "Sequence diagram converted from fence F149 (line 4056)." {
    title "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 1 branch 3 of 3: turn found, with its status, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "SourceResolver: ReadCommit, profile and rule sources"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadBuildInputs-Sequence-alt2-1" "Sequence diagram converted from fence F149 (line 4056)." {
    title "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 2 branch 1 of 3: commit unreadable"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 3: commit unreadable"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_commit_unavailable"
    qbrain.libs.client -> qbrain.libs.client "Note: refuse the prompt with the code, close the turn canceled"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadBuildInputs-Sequence-alt2-2" "Sequence diagram converted from fence F149 (line 4056)." {
    title "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 2 branch 2 of 3: no profile file or no rule file resolves"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 3: no profile file or no rule file resolves"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_profile_unavailable"
    qbrain.libs.client -> qbrain.libs.client "Note: refuse the prompt with the code, close the turn canceled"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadBuildInputs-Sequence-alt2-3" "Sequence diagram converted from fence F149 (line 4056)." {
    title "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 2 branch 3 of 3: commit, profile, and rules read, part 1 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2 branch 3 of 3: commit, profile, and rules read"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadBuildInputs-Sequence-alt2-3-part2" "Sequence diagram converted from fence F149 (line 4056)." {
    title "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 2 branch 3 of 3: commit, profile, and rules read, part 2 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "QueryAsync(RetrieveAgentContextCandidatesQuery)"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadBuildInputs-Sequence-alt2-3-part3" "Sequence diagram converted from fence F149 (line 4056)." {
    title "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 2 branch 3 of 3: commit, profile, and rules read, part 3 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: staleness read-only, nothing written"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadBuildInputs-Sequence-alt2-3-part4" "Sequence diagram converted from fence F149 (line 4056)." {
    title "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 2 branch 3 of 3: commit, profile, and rules read, part 4 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "SourceResolver: resolve and hash every candidate"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "IdempotentCreate.Find, a concurrent winner"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-part2" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 6 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt1-1" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 1 of 6: stored package Built"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 6: stored package Built"
    qbrain.server.api -> qbrain.libs.client "200 AgentContextPackageResult, status Built"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt1-2" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 2 of 6: stored package Overflow"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 6: stored package Overflow"
    qbrain.server.api -> qbrain.libs.client "422 agent_context_budget_exceeded, stored diagnostics"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt1-3" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 3 of 6: key reused with other inputs"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 3 of 6: key reused with other inputs"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable false"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt1-4" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 4 of 6: absent, and the turn status changed since ReadBuildInputs, part 1 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 4 of 6: absent, and the turn status changed since ReadBuildInputs"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt1-4-part2" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 4 of 6: absent, and the turn status changed since ReadBuildInputs, part 2 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(1 of 2) Note over Handler and Store: the attempt ends, Build runs"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt1-4-part3" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 4 of 6: absent, and the turn status changed since ReadBuildInputs, part 3 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(2 of 2) again from its reads"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt1-5" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 5 of 6: absent, and the turn status unchanged"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 5 of 6: absent, and the turn status unchanged"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt2-1" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 2 branch 1 of 2: PackageAssembler returned Overflow, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2 branch 1 of 2: PackageAssembler returned Overflow"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "insert the Overflow package, mandatory items, snapshot"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt2-1-part2" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 2 branch 1 of 2: PackageAssembler returned Overflow, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "422 agent_context_budget_exceeded, token counts"
    qbrain.libs.client -> qbrain.libs.client "Note: refuse the prompt with the token counts, close the turn canceled"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt2-2" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 2 branch 2 of 2: assembled, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2 branch 2 of 2: assembled"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "insert the package, its children, its snapshot"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "RelationshipWriter: derived_from and relevant_to links"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt2-2-part2" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 2 branch 2 of 2: assembled, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "201 AgentContextPackageResult, Location"
    qbrain.libs.client -> qbrain.libs.client "Note: keep the packageId, inject in Stateless"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt1-6" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 6 of 6: retries exhausted (SerializableWrite or key allocation), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 6 of 6: retries exhausted (SerializableWrite or key"
    autolayout lr 300 100
}
dynamic qbrain.server "SaveBuiltPackage-alt1-6-part2" "Sequence diagram converted from fence F150 (line 4089)." {
    title "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 6 of 6: retries exhausted (SerializableWrite or key allocation), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) allocation)"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: retry the same request within the bounded retries of C9"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-11" "Sequence diagram converted from fence F151 (line 4124)." {
    title "Sequence: submit GetAgentContextPackageQuery, part 1 of 5"
    qbrain.libs.client -> qbrain.server.api "GET /packages/{packageId}?includeContent"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-11-part2" "Sequence diagram converted from fence F151 (line 4124)." {
    title "Sequence: submit GetAgentContextPackageQuery, part 2 of 5"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "QueryAsync(GetAgentContextPackageQuery)"
    qbrain.server.api -> qbrain.server.api "(1 of 2) Note over Controller and Handler: ref RequestPipeline, not gated:"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-11-part3" "Sequence diagram converted from fence F151 (line 4124)." {
    title "Sequence: submit GetAgentContextPackageQuery, part 3 of 5"
    qbrain.server.api -> qbrain.server.api "(2 of 2) readable in Mode Off"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-11-part4" "Sequence diagram converted from fence F151 (line 4124)." {
    title "Sequence: submit GetAgentContextPackageQuery, part 4 of 5"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "load the package in the workspace"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-11-part5" "Sequence diagram converted from fence F151 (line 4124)." {
    title "Sequence: submit GetAgentContextPackageQuery, part 5 of 5"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-11-alt1-1" "Sequence diagram converted from fence F151 (line 4124)." {
    title "Sequence: submit GetAgentContextPackageQuery, alt 1 branch 1 of 2: not found"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: not found"
    qbrain.server.api -> qbrain.libs.client "404 not_found, details.resource package"
    qbrain.libs.client -> qbrain.libs.client "Note: report the id"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-11-alt1-2" "Sequence diagram converted from fence F151 (line 4124)." {
    title "Sequence: submit GetAgentContextPackageQuery, alt 1 branch 2 of 2: found, Built or Overflow, part 1 of 3"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(1 of 2) alt 1 branch 2 of 2: found,"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-11-alt1-2-part2" "Sequence diagram converted from fence F151 (line 4124)." {
    title "Sequence: submit GetAgentContextPackageQuery, alt 1 branch 2 of 2: found, Built or Overflow, part 2 of 3"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(2 of 2) Built or Overflow"
    qbrain.server.mcpDbContext -> qbrain.server.strAgentContextHandlers "package, items, omissions, findings, content"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-11-alt1-2-part3" "Sequence diagram converted from fence F151 (line 4124)." {
    title "Sequence: submit GetAgentContextPackageQuery, alt 1 branch 2 of 2: found, Built or Overflow, part 3 of 3"
    qbrain.server.api -> qbrain.libs.client "200 AgentContextPackageResult"
    qbrain.libs.client -> qbrain.libs.client "Note: verify contentSha256 of renderedContent before use"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-12" "Sequence diagram converted from fence F152 (line 4147)." {
    title "Sequence: submit QueryAgentContextPackagesQuery, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "GET /packages?agent&sessionId&requestId&status&limit&offset"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-12-part2" "Sequence diagram converted from fence F152 (line 4147)." {
    title "Sequence: submit QueryAgentContextPackagesQuery, part 2 of 4"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "QueryAsync(QueryAgentContextPackagesQuery)"
    qbrain.server.api -> qbrain.server.api "(1 of 2) Note over Controller and Handler: ref RequestPipeline, not gated:"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-12-part3" "Sequence diagram converted from fence F152 (line 4147)." {
    title "Sequence: submit QueryAgentContextPackagesQuery, part 3 of 4"
    qbrain.server.api -> qbrain.server.api "(2 of 2) readable in Mode Off"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-12-part4" "Sequence diagram converted from fence F152 (line 4147)." {
    title "Sequence: submit QueryAgentContextPackagesQuery, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-12-alt1-1" "Sequence diagram converted from fence F152 (line 4147)." {
    title "Sequence: submit QueryAgentContextPackagesQuery, alt 1 branch 1 of 2: unknown status, or limit or offset outside PageBounds"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: unknown status, or limit or offset outside PageBounds"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-12-alt1-2" "Sequence diagram converted from fence F152 (line 4147)." {
    title "Sequence: submit QueryAgentContextPackagesQuery, alt 1 branch 2 of 2: filters valid, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 2: filters valid"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "PagedQuery, CreatedUtc descending then PackageId"
    qbrain.server.mcpDbContext -> qbrain.server.strAgentContextHandlers "PageResult"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-12-alt1-2-part2" "Sequence diagram converted from fence F152 (line 4147)." {
    title "Sequence: submit QueryAgentContextPackagesQuery, alt 1 branch 2 of 2: filters valid, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "200 PageResult of package summaries"
    qbrain.libs.client -> qbrain.libs.client "Note: PagedQuery paging"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-13" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, part 1 of 5"
    qbrain.libs.client -> qbrain.server.api "POST /packages/{packageId}/replay"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-13-part2" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, part 2 of 5"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "QueryAsync(ReplayAgentContextPackageQuery)"
    qbrain.server.api -> qbrain.server.api "(1 of 2) Note over Controller and Handler: ref RequestPipeline, not gated:"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-13-part3" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, part 3 of 5"
    qbrain.server.api -> qbrain.server.api "(2 of 2) readable in Mode Off"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-13-part4" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, part 4 of 5"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "BuiltPackageLookup(packageId)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-13-part5" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, part 5 of 5"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 5 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-13-alt1-1" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 1 of 5: not found"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 5: not found"
    qbrain.server.api -> qbrain.libs.client "404 not_found, details.resource package"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-13-alt1-2" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 2 of 5: Overflow"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 5: Overflow"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_package_overflow"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-13-alt1-3" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 3 of 5: no input snapshot stored"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 3 of 5: no input snapshot stored"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_replay_snapshot_unavailable"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-13-alt1-4" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 4 of 5: built by another builder version"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 4 of 5: built by another builder version"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_replay_builder_changed"
    qbrain.libs.client -> qbrain.libs.client "Note: report recorded and current, replay needs the same builder"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-13-alt1-5" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 5 of 5: replayable, part 1 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 5 of 5: replayable"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strPackageAssembler "assemble(stored snapshot, OptionsSnapshotJson)"
    qbrain.server.strPackageAssembler -> qbrain.server.strAgentContextHandlers "items, InputsSha256, ContentSha256"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-13-alt1-5-part2" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 5 of 5: replayable, part 2 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: compare by ordinal, nothing stored"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-13-alt1-5-part3" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 5 of 5: replayable, part 3 of 3"
    qbrain.server.api -> qbrain.libs.client "200 AgentContextReplayResult"
    qbrain.libs.client -> qbrain.libs.client "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-13-alt2-1" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, alt 2 branch 1 of 2: identical, part 1 of 2"
    qbrain.libs.client -> qbrain.libs.client "alt 2 branch 1 of 2: identical"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-13-alt2-1-part2" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, alt 2 branch 1 of 2: identical, part 2 of 2"
    qbrain.libs.client -> qbrain.libs.client "Note: deterministic replay proven"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-13-alt2-2" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, alt 2 branch 2 of 2: differences, part 1 of 2"
    qbrain.libs.client -> qbrain.libs.client "alt 2 branch 2 of 2: differences"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-13-alt2-2-part2" "Sequence diagram converted from fence F153 (line 4171)." {
    title "Sequence: submit ReplayAgentContextPackageQuery, alt 2 branch 2 of 2: differences, part 2 of 2"
    qbrain.libs.client -> qbrain.libs.client "Note: report each added, removed, hashChanged, reordered item"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, part 1 of 3"
    qbrain.libs.client -> qbrain.server.api "POST /responses (SubmitAgentResponseRequest)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-part2" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, part 2 of 3"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "SendAsync(SubmitAgentResponseCommand)"
    qbrain.server.api -> qbrain.server.api "Note over Controller and Handler: ref RequestPipeline, gated"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-part3" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, part 3 of 3"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt1-1" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 1 branch 1 of 2: content, claims, evidence, format, or mode invalid, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 1 of 2: content, claims, evidence, format, or mode"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt1-1-part2" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 1 branch 1 of 2: content, claims, evidence, format, or mode invalid, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) invalid"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    qbrain.libs.client -> qbrain.libs.client "Note: fix and resend"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt1-2" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 1 branch 2 of 2: request valid, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 2: request valid"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "IdempotentCreate.Find, read-only, before any source read"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt1-2-part2" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 1 branch 2 of 2: request valid, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt2-1" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 2 branch 1 of 3: stored, same submission"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 3: stored, same submission"
    qbrain.server.api -> qbrain.libs.client "200 AgentResponseResult, the stored response"
    qbrain.libs.client -> qbrain.libs.client "Note: treat as submitted"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt2-2" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 2 branch 2 of 3: key reused with another submission (any field)"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 3: key reused with another submission (any field)"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable false"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-14-alt2-3" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 2 branch 3 of 3: absent, part 1 of 5"
    qbrain.libs.client -> qbrain.libs.client "alt 2 branch 3 of 3: absent"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-14-alt2-3-part2" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 2 branch 3 of 3: absent, part 2 of 5"
    qbrain.libs.client -> qbrain.libs.client "(1 of 2) Note over Caller and Store: ref ReadSubmissionInputs (below),"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt2-3-part3" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 2 branch 3 of 3: absent, part 3 of 5"
    qbrain.libs.client -> qbrain.libs.client "(2 of 2) each refusal there answers the caller"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "SecretRedactor: redact the content, then ContentSha256"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt2-3-part4" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 2 branch 3 of 3: absent, part 4 of 5"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note over Handler and Store: SerializableWrite, database only"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "IdempotentCreate.Find, a concurrent winner"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt2-3-part5" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 2 branch 3 of 3: absent, part 5 of 5"
    qbrain.server.api -> qbrain.server.api "alt 3: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt3-1" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 3 branch 1 of 4: winner with the same submission"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 1 of 4: winner with the same submission"
    qbrain.server.api -> qbrain.libs.client "200 AgentResponseResult, the stored response"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt3-2" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 3 branch 2 of 4: winner with another submission"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 2 of 4: winner with another submission"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable false"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt3-3" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 3 branch 3 of 4: absent, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 3 branch 3 of 4: absent"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "insert the response, claims, evidence, and links"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt3-3-part2" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 3 branch 3 of 4: absent, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "201 AgentResponseResult, status Submitted"
    qbrain.libs.client -> qbrain.libs.client "Note: the plugin requests the deterministic grade"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt3-4" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 3 branch 4 of 4: retries exhausted (SerializableWrite or key allocation), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 3 branch 4 of 4: retries exhausted (SerializableWrite or key"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-14-alt3-4-part2" "Sequence diagram converted from fence F154 (line 4209)." {
    title "Sequence: submit SubmitAgentResponseCommand, alt 3 branch 4 of 4: retries exhausted (SerializableWrite or key allocation), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) allocation)"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: retry the same request within the bounded retries of C9"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadSubmissionInputs-Sequence" "Sequence diagram converted from fence F155 (line 4254)." {
    title "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "BuiltPackageLookup(packageId)"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadSubmissionInputs-Sequence-part2" "Sequence diagram converted from fence F155 (line 4254)." {
    title "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadSubmissionInputs-Sequence-alt1-1" "Sequence diagram converted from fence F155 (line 4254)." {
    title "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 1 branch 1 of 4: package not found"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 4: package not found"
    qbrain.server.api -> qbrain.libs.client "404 not_found, details.resource package"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadSubmissionInputs-Sequence-alt1-2" "Sequence diagram converted from fence F155 (line 4254)." {
    title "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 1 branch 2 of 4: package Overflow"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 4: package Overflow"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_package_overflow"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadSubmissionInputs-Sequence-alt1-3" "Sequence diagram converted from fence F155 (line 4254)." {
    title "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 1 branch 3 of 4: agent, mode, or own-turn evidence inconsistent with the package, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 3 of 4: agent, mode, or own-turn evidence"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadSubmissionInputs-Sequence-alt1-3-part2" "Sequence diagram converted from fence F155 (line 4254)." {
    title "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 1 branch 3 of 4: agent, mode, or own-turn evidence inconsistent with the package, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) inconsistent with the package"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadSubmissionInputs-Sequence-alt1-4" "Sequence diagram converted from fence F155 (line 4254)." {
    title "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 1 branch 4 of 4: package Built, submission consistent, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 4 of 4: package Built, submission consistent"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadSubmissionInputs-Sequence-alt1-4-part2" "Sequence diagram converted from fence F155 (line 4254)." {
    title "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 1 branch 4 of 4: package Built, submission consistent, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "SourceResolver: ReadCommit, hash each evidence ref"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadSubmissionInputs-Sequence-alt2-1" "Sequence diagram converted from fence F155 (line 4254)." {
    title "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 2 branch 1 of 3: commit unreadable"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 3: commit unreadable"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_commit_unavailable"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadSubmissionInputs-Sequence-alt2-2" "Sequence diagram converted from fence F155 (line 4254)." {
    title "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 2 branch 2 of 3: a ref names no existing source"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 3: a ref names no existing source"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field the ref"
    qbrain.libs.client -> qbrain.libs.client "Note: cite an existing source and resend"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadSubmissionInputs-Sequence-alt2-3" "Sequence diagram converted from fence F155 (line 4254)." {
    title "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 2 branch 3 of 3: every ref resolved, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2 branch 3 of 3: every ref resolved"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadSubmissionInputs-Sequence-alt2-3-part2" "Sequence diagram converted from fence F155 (line 4254)." {
    title "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 2 branch 3 of 3: every ref resolved, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: Sha256 and CitedSha256 kept per evidence item"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-15" "Sequence diagram converted from fence F156 (line 4285)." {
    title "Sequence: submit GetAgentResponseQuery, part 1 of 5"
    qbrain.libs.client -> qbrain.server.api "GET /responses/{responseId}?includeContent"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-15-part2" "Sequence diagram converted from fence F156 (line 4285)." {
    title "Sequence: submit GetAgentResponseQuery, part 2 of 5"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "QueryAsync(GetAgentResponseQuery)"
    qbrain.server.api -> qbrain.server.api "(1 of 2) Note over Controller and Handler: ref RequestPipeline, not gated:"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-15-part3" "Sequence diagram converted from fence F156 (line 4285)." {
    title "Sequence: submit GetAgentResponseQuery, part 3 of 5"
    qbrain.server.api -> qbrain.server.api "(2 of 2) readable in Mode Off"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-15-part4" "Sequence diagram converted from fence F156 (line 4285)." {
    title "Sequence: submit GetAgentResponseQuery, part 4 of 5"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "load the response, claims, evidence, grades"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-15-part5" "Sequence diagram converted from fence F156 (line 4285)." {
    title "Sequence: submit GetAgentResponseQuery, part 5 of 5"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-15-alt1-1" "Sequence diagram converted from fence F156 (line 4285)." {
    title "Sequence: submit GetAgentResponseQuery, alt 1 branch 1 of 2: not found"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: not found"
    qbrain.server.api -> qbrain.libs.client "404 not_found, details.resource response"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-15-alt1-2" "Sequence diagram converted from fence F156 (line 4285)." {
    title "Sequence: submit GetAgentResponseQuery, alt 1 branch 2 of 2: found, part 1 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 2: found"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-15-alt1-2-part2" "Sequence diagram converted from fence F156 (line 4285)." {
    title "Sequence: submit GetAgentResponseQuery, alt 1 branch 2 of 2: found, part 2 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "IResponseStalenessRule, read-only"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-15-alt1-2-part3" "Sequence diagram converted from fence F156 (line 4285)." {
    title "Sequence: submit GetAgentResponseQuery, alt 1 branch 2 of 2: found, part 3 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: effective status, nothing written"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-15-alt1-2-part4" "Sequence diagram converted from fence F156 (line 4285)." {
    title "Sequence: submit GetAgentResponseQuery, alt 1 branch 2 of 2: found, part 4 of 4"
    qbrain.server.api -> qbrain.libs.client "200 AgentResponseResult, effective status"
    qbrain.libs.client -> qbrain.libs.client "Note: use as context only when Trusted"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-16" "Sequence diagram converted from fence F157 (line 4310)." {
    title "Sequence: submit QueryAgentResponsesQuery, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "GET /responses?requestId&packageId&status&trustedOnly&limit&offset"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-16-part2" "Sequence diagram converted from fence F157 (line 4310)." {
    title "Sequence: submit QueryAgentResponsesQuery, part 2 of 4"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "QueryAsync(QueryAgentResponsesQuery)"
    qbrain.server.api -> qbrain.server.api "(1 of 2) Note over Controller and Handler: ref RequestPipeline, not gated:"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-16-part3" "Sequence diagram converted from fence F157 (line 4310)." {
    title "Sequence: submit QueryAgentResponsesQuery, part 3 of 4"
    qbrain.server.api -> qbrain.server.api "(2 of 2) readable in Mode Off"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-16-part4" "Sequence diagram converted from fence F157 (line 4310)." {
    title "Sequence: submit QueryAgentResponsesQuery, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-16-alt1-1" "Sequence diagram converted from fence F157 (line 4310)." {
    title "Sequence: submit QueryAgentResponsesQuery, alt 1 branch 1 of 2: unknown status, or limit or offset outside PageBounds"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: unknown status, or limit or offset outside PageBounds"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-16-alt1-2" "Sequence diagram converted from fence F157 (line 4310)." {
    title "Sequence: submit QueryAgentResponsesQuery, alt 1 branch 2 of 2: filters valid, part 1 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 2: filters valid"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-16-alt1-2-part2" "Sequence diagram converted from fence F157 (line 4310)." {
    title "Sequence: submit QueryAgentResponsesQuery, alt 1 branch 2 of 2: filters valid, part 2 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "IResponseStalenessRule, read-only"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-16-alt1-2-part3" "Sequence diagram converted from fence F157 (line 4310)." {
    title "Sequence: submit QueryAgentResponsesQuery, alt 1 branch 2 of 2: filters valid, part 3 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: effective status of every match, nothing written"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "PagedQuery by effective status"
    qbrain.server.mcpDbContext -> qbrain.server.strAgentContextHandlers "PageResult, SubmittedUtc descending then ResponseId"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-16-alt1-2-part4" "Sequence diagram converted from fence F157 (line 4310)." {
    title "Sequence: submit QueryAgentResponsesQuery, alt 1 branch 2 of 2: filters valid, part 4 of 4"
    qbrain.server.api -> qbrain.libs.client "200 PageResult of response summaries"
    qbrain.libs.client -> qbrain.libs.client "Note: PagedQuery paging"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "POST /responses/{responseId}/grades/deterministic"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-part2" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, part 2 of 4"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "SendAsync(RunDeterministicGradeCommand)"
    qbrain.server.api -> qbrain.server.api "Note over Controller and Handler: ref RequestPipeline, gated"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-part3" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, part 3 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "load the response"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-part4" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-1" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 1 of 4: not found"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 4: not found"
    qbrain.server.api -> qbrain.libs.client "404 not_found, details.resource response"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-2" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 2 of 4: Rejected or Superseded"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 4: Rejected or Superseded"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_response_not_gradable"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-3" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 1 of 9"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 3 of 4: gradable"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-3-part2" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 2 of 9"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "SourceResolver: each evidence item, the commit ancestry"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-3-part3" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 3 of 9"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: secret scan of content, claims, and evidence refs"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-3-part4" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 4 of 9"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "SendAsync(EvaluateStalenessCommand), this response"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-3-part5" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 5 of 9"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: global checks, exact scores, ScoreVerdict"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-3-part6" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 6 of 9"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(1 of 2) Note over Handler and Store: SerializableWrite, response"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-3-part7" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 7 of 9"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(2 of 2) locked, database only"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "re-check StatusVersion, save the grade and findings"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-3-part8" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 8 of 9"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "SendAsync(DecideAgentResponseTrustCommand)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-3-part9" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 9 of 9"
    qbrain.server.api -> qbrain.libs.client "201 AgentResponseGradeResult, responseStatus"
    qbrain.libs.client -> qbrain.libs.client "alt 2: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-17-alt2-1" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 2 branch 1 of 3: responseStatus Stale, part 1 of 2"
    qbrain.libs.client -> qbrain.libs.client "alt 2 branch 1 of 3: responseStatus Stale"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-17-alt2-1-part2" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 2 branch 1 of 3: responseStatus Stale, part 2 of 2"
    qbrain.libs.client -> qbrain.libs.client "Note: never trusted, submit a new response"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-17-alt2-2" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 2 branch 2 of 3: verdict Pass, responseStatus Grading, part 1 of 2"
    qbrain.libs.client -> qbrain.libs.client "alt 2 branch 2 of 3: verdict Pass, responseStatus Grading"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-17-alt2-2-part2" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 2 branch 2 of 3: verdict Pass, responseStatus Grading, part 2 of 2"
    qbrain.libs.client -> qbrain.libs.client "Note: await the model grade of an HV request"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-17-alt2-3" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 2 branch 3 of 3: verdict Fail, part 1 of 2"
    qbrain.libs.client -> qbrain.libs.client "alt 2 branch 3 of 3: verdict Fail"
    autolayout lr 300 100
}
dynamic qbrain.libs "STR-06-17-alt2-3-part2" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 2 branch 3 of 3: verdict Fail, part 2 of 2"
    qbrain.libs.client -> qbrain.libs.client "Note: fix each FAIL and UNKNOWN, submit anew"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-4" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 4 of 4: retries exhausted (here or in EvaluateStalenessCommand), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 4 of 4: retries exhausted (here or in"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-17-alt1-4-part2" "Sequence diagram converted from fence F158 (line 4336)." {
    title "Sequence: submit RunDeterministicGradeCommand, alt 1 branch 4 of 4: retries exhausted (here or in EvaluateStalenessCommand), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) EvaluateStalenessCommand)"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: retry the same request within the bounded retries of C9"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-18" "Sequence diagram converted from fence F159 (line 4377)." {
    title "Sequence: submit RecordModelGradeCommand from an HV review, part 1 of 8"
    agent -> qbrain.server.api "POST /hv-requests (CreateAgentContextHvRequestCommand)"
    qbrain.server.api -> agent "201 hvRequestId with its response items"
    autolayout lr 300 100
}
dynamic qbrain.devTools "STR-06-18-part2" "Sequence diagram converted from fence F159 (line 4377)." {
    title "Sequence: submit RecordModelGradeCommand from an HV review, part 2 of 8"
    agent -> qbrain.devTools.evidenceScripts "HV brief naming hvRequestId"
    autolayout lr 300 100
}
dynamic qbrain.devTools "STR-06-18-part3" "Sequence diagram converted from fence F159 (line 4377)." {
    title "Sequence: submit RecordModelGradeCommand from an HV review, part 3 of 8"
    qbrain.devTools.evidenceScripts -> llm "step 1: codex exec (review)"
    llm -> llm "(1 of 2) Note over Reviewer and Controller: reviews, emits the VERDICT"
    autolayout lr 300 100
}
dynamic qbrain.devTools "STR-06-18-part4" "Sequence diagram converted from fence F159 (line 4377)." {
    title "Sequence: submit RecordModelGradeCommand from an HV review, part 4 of 8"
    llm -> llm "(2 of 2) JSON, stores its result on its own turn"
    llm -> qbrain.devTools.evidenceScripts "run completes (turn.completed)"
    qbrain.devTools.evidenceScripts -> qbrain.devTools.evidenceScripts "(1 of 2) Note over Runner and Reviewer: freezes the request,"
    autolayout lr 300 100
}
dynamic qbrain.devTools "STR-06-18-part5" "Sequence diagram converted from fence F159 (line 4377)." {
    title "Sequence: submit RecordModelGradeCommand from an HV review, part 5 of 8"
    qbrain.devTools.evidenceScripts -> qbrain.devTools.evidenceScripts "(2 of 2) response, and rollout jsonl receipts"
    qbrain.devTools.evidenceScripts -> llm "step 2: codex exec resume reviewerThreadId"
    autolayout lr 300 100
}
dynamic qbrain.devTools "STR-06-18-part6" "Sequence diagram converted from fence F159 (line 4377)." {
    title "Sequence: submit RecordModelGradeCommand from an HV review, part 6 of 8"
    qbrain.devTools.evidenceScripts -> qbrain.devTools.evidenceScripts "(1 of 2) Note over Runner and Reviewer: grade-only continuation"
    autolayout lr 300 100
}
dynamic qbrain.devTools "STR-06-18-part7" "Sequence diagram converted from fence F159 (line 4377)." {
    title "Sequence: submit RecordModelGradeCommand from an HV review, part 7 of 8"
    qbrain.devTools.evidenceScripts -> qbrain.devTools.evidenceScripts "(2 of 2) naming the frozen receipt paths"
    llm -> llm "loop 1: each response item of the HV request (next view)"
    llm -> qbrain.devTools.evidenceScripts "continuation completes"
    autolayout lr 300 100
}
dynamic qbrain.devTools "STR-06-18-part8" "Sequence diagram converted from fence F159 (line 4377)." {
    title "Sequence: submit RecordModelGradeCommand from an HV review, part 8 of 8"
    qbrain.devTools.evidenceScripts -> qbrain.devTools.evidenceScripts "Note: an interrupted continuation is resumed again with the same prompt"
    autolayout lr 300 100
}
dynamic * "STR-06-18-loop1" "Sequence diagram converted from fence F159 (line 4377)." {
    title "Sequence: submit RecordModelGradeCommand from an HV review, loop 1: each response item of the HV request, part 1 of 2"
    llm -> llm "loop 1: each response item of the HV request"
    autolayout lr 300 100
}
dynamic * "STR-06-18-loop1-part2" "Sequence diagram converted from fence F159 (line 4377)." {
    title "Sequence: submit RecordModelGradeCommand from an HV review, loop 1: each response item of the HV request, part 2 of 2"
    llm -> llm "Note over Reviewer and Controller: ref SubmitOneModelGrade (below)"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence)"
    llm -> qbrain.server.api "POST /responses/{responseId}/grades/model"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt1-1" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 1 branch 1 of 2: score lexeme invalid (ScoreLexemeJsonConverter)"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: score lexeme invalid (ScoreLexemeJsonConverter)"
    qbrain.server.api -> llm "400 review_score_format, _precision, or _out_of_range"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt1-2" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 1 branch 2 of 2: lexemes valid, part 1 of 3"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 2: lexemes valid"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "SendAsync(RecordModelGradeCommand)"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt1-2-part2" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 1 branch 2 of 2: lexemes valid, part 2 of 3"
    qbrain.server.api -> qbrain.server.api "Note over Controller and Handler: ref RequestPipeline, gated"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt1-2-part3" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 1 branch 2 of 2: lexemes valid, part 3 of 3"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt2-1" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 1 of 4: hvRequestId, a jsonl path, a reviewer turn id, or reviewerThreadId missing, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 2 branch 1 of 4: hvRequestId, a jsonl path, a reviewer turn"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt2-1-part2" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 1 of 4: hvRequestId, a jsonl path, a reviewer turn id, or reviewerThreadId missing, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) id, or reviewerThreadId missing"
    qbrain.server.api -> llm "400 agent_context_receipt_paths_required"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt2-2" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 2 of 4: findings invalid"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 4: findings invalid"
    qbrain.server.api -> llm "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt2-3" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 3 of 4: grader not the configured one"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 3 of 4: grader not the configured one"
    qbrain.server.api -> llm "403 agent_context_grader_not_allowed"
    autolayout lr 300 100
}
dynamic * "SubmitOneModelGrade-alt2-4" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 4 of 4: request valid, part 1 of 5"
    llm -> llm "alt 2 branch 4 of 4: request valid"
    autolayout lr 300 100
}
dynamic * "SubmitOneModelGrade-alt2-4-part2" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 4 of 4: request valid, part 2 of 5"
    llm -> llm "(1 of 2) Note over Reviewer and Store: ref ReadModelGradeInputs (below),"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt2-4-part3" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 4 of 4: request valid, part 3 of 5"
    llm -> llm "(2 of 2) each refusal there answers the reviewer"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(1 of 2) Note over Handler and Store: SerializableWrite, response"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt2-4-part4" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 4 of 4: request valid, part 4 of 5"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(2 of 2) locked, database only"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "IdempotentCreate.Find, a concurrent continuation"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt2-4-part5" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 4 of 4: request valid, part 5 of 5"
    qbrain.server.api -> qbrain.server.api "alt 3: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt3-1" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 3 branch 1 of 4: equal grade stored meanwhile"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 1 of 4: equal grade stored meanwhile"
    qbrain.server.api -> llm "200 the stored AgentResponseGradeResult"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt3-2" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 3 branch 2 of 4: a different grade stored meanwhile"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 2 of 4: a different grade stored meanwhile"
    qbrain.server.api -> llm "409 conflict, retryable false"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt3-3" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 3 branch 3 of 4: absent, part 1 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 3 branch 3 of 4: absent"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "re-check, insert the grade, item Graded"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt3-3-part2" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 3 branch 3 of 4: absent, part 2 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "SendAsync(DecideAgentResponseTrustCommand)"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt3-3-part3" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 3 branch 3 of 4: absent, part 3 of 3"
    qbrain.server.api -> llm "201 AgentResponseGradeResult, responseStatus, hvRequestStatus"
    llm -> llm "alt 4: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic * "SubmitOneModelGrade-alt4-1" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 1 of 3: responseStatus Trusted, part 1 of 3"
    llm -> llm "alt 4 branch 1 of 3: responseStatus Trusted"
    autolayout lr 300 100
}
dynamic * "SubmitOneModelGrade-alt4-1-part2" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 1 of 3: responseStatus Trusted, part 2 of 3"
    llm -> llm "(1 of 2) Note over Reviewer and Controller: the response becomes"
    autolayout lr 300 100
}
dynamic * "SubmitOneModelGrade-alt4-1-part3" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 1 of 3: responseStatus Trusted, part 3 of 3"
    llm -> llm "(2 of 2) retrievable context"
    autolayout lr 300 100
}
dynamic * "SubmitOneModelGrade-alt4-2" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 2 of 3: responseStatus Grading, part 1 of 2"
    llm -> llm "alt 4 branch 2 of 3: responseStatus Grading"
    autolayout lr 300 100
}
dynamic * "SubmitOneModelGrade-alt4-2-part2" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 2 of 3: responseStatus Grading, part 2 of 2"
    llm -> llm "Note over Reviewer and Controller: report each FAIL and UNKNOWN finding"
    autolayout lr 300 100
}
dynamic * "SubmitOneModelGrade-alt4-3" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 3 of 3: responseStatus Stale, part 1 of 2"
    llm -> llm "alt 4 branch 3 of 3: responseStatus Stale"
    autolayout lr 300 100
}
dynamic * "SubmitOneModelGrade-alt4-3-part2" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 3 of 3: responseStatus Stale, part 2 of 2"
    llm -> llm "Note over Reviewer and Controller: report the changed dependency"
    autolayout lr 300 100
}
dynamic qbrain.server "SubmitOneModelGrade-alt3-4" "Sequence diagram converted from fence F160 (line 4404)." {
    title "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 3 branch 4 of 4: retries exhausted"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 4 of 4: retries exhausted"
    qbrain.server.api -> llm "409 conflict, retryable true, nothing saved"
    llm -> llm "Note: resubmit the same grade"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "IdempotentCreate.Find, read-only"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-part2" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt1-1" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 1 branch 1 of 3: equal grade stored (a retried continuation)"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 3: equal grade stored (a retried continuation)"
    qbrain.server.api -> llm "200 the stored AgentResponseGradeResult"
    llm -> llm "Note: continue with the next item"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt1-2" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 1 branch 2 of 3: a different grade stored"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 3: a different grade stored"
    qbrain.server.api -> llm "409 conflict, retryable false"
    llm -> llm "Note: report it, never overwrite a stored grade"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt1-3" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 1 branch 3 of 3: absent, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 3 of 3: absent"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "load the response, the HV request, and its item"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt1-3-part2" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 1 branch 3 of 3: absent, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 7 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt2-1" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 1 of 7: response not found"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 7: response not found"
    qbrain.server.api -> llm "404 not_found, details.resource response"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt2-2" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 2 of 7: a finding names a claim the response does not have"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 7: a finding names a claim the response does not have"
    qbrain.server.api -> llm "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt2-3" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 3 of 7: HV request not found"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 3 of 7: HV request not found"
    qbrain.server.api -> llm "404 not_found, details.resource hv_request"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt2-4" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 4 of 7: response Rejected or Superseded"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 4 of 7: response Rejected or Superseded"
    qbrain.server.api -> llm "409 agent_context_response_not_gradable"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt2-5" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 5 of 7: HV request Completed"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 5 of 7: HV request Completed"
    qbrain.server.api -> llm "409 agent_context_hv_request_closed"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt2-6" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 6 of 7: response not an item of the HV request"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 6 of 7: response not an item of the HV request"
    qbrain.server.api -> llm "409 agent_context_grade_not_in_hv_request"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt2-7" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 7 of 7: gradable item of an Open HV request, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2 branch 7 of 7: gradable item of an Open HV request"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt2-7-part2" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 7 of 7: gradable item of an Open HV request, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "read the receipts, run the eight checks of section 6.3"
    qbrain.server.api -> qbrain.server.api "alt 3: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt3-1" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 3 branch 1 of 2: a check fails"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 1 of 2: a check fails"
    qbrain.server.api -> llm "422 agent_context_receipt_unverified, details.check"
    llm -> llm "Note: match the verdict entry and turn, or rerun the HV"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt3-2" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 3 branch 2 of 2: all eight pass, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 3 branch 2 of 2: all eight pass"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt3-2-part2" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 3 branch 2 of 2: all eight pass, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "SendAsync(EvaluateStalenessCommand), this response"
    qbrain.server.api -> qbrain.server.api "alt 4: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt4-1" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 4 branch 1 of 2: its write exhausted the retries"
    qbrain.server.api -> qbrain.server.api "alt 4 branch 1 of 2: its write exhausted the retries"
    qbrain.server.api -> llm "409 conflict, retryable true, nothing saved"
    llm -> llm "Note: resubmit the same grade"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt4-2" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 4 branch 2 of 2: evaluated: fresh, or Stale persisted, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 4 branch 2 of 2: evaluated: fresh, or Stale persisted"
    autolayout lr 300 100
}
dynamic qbrain.server "ReadModelGradeInputs-Sequence-alt4-2-part2" "Sequence diagram converted from fence F161 (line 4454)." {
    title "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 4 branch 2 of 2: evaluated: fresh, or Stale persisted, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: the status and StatusVersion the write re-checks"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-19" "Sequence diagram converted from fence F162 (line 4504)." {
    title "Sequence: submit RejectAgentResponseCommand, part 1 of 3"
    qbrain.libs.client -> qbrain.server.api "POST /responses/{responseId}/reject (reason)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-19-part2" "Sequence diagram converted from fence F162 (line 4504)." {
    title "Sequence: submit RejectAgentResponseCommand, part 2 of 3"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "SendAsync(RejectAgentResponseCommand)"
    qbrain.server.api -> qbrain.server.api "Note over Controller and Handler: ref RequestPipeline, gated"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-19-part3" "Sequence diagram converted from fence F162 (line 4504)." {
    title "Sequence: submit RejectAgentResponseCommand, part 3 of 3"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-19-alt1-1" "Sequence diagram converted from fence F162 (line 4504)." {
    title "Sequence: submit RejectAgentResponseCommand, alt 1 branch 1 of 2: blank reason"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: blank reason"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field reason"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-19-alt1-2" "Sequence diagram converted from fence F162 (line 4504)." {
    title "Sequence: submit RejectAgentResponseCommand, alt 1 branch 2 of 2: reason given, part 1 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 2: reason given"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-19-alt1-2-part2" "Sequence diagram converted from fence F162 (line 4504)." {
    title "Sequence: submit RejectAgentResponseCommand, alt 1 branch 2 of 2: reason given, part 2 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note over Handler and Store: SerializableWrite, responseId locked"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "ResponseStatusTransition(responseId, Rejected, reason)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-19-alt1-2-part3" "Sequence diagram converted from fence F162 (line 4504)." {
    title "Sequence: submit RejectAgentResponseCommand, alt 1 branch 2 of 2: reason given, part 3 of 3"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 5 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-19-alt2-1" "Sequence diagram converted from fence F162 (line 4504)." {
    title "Sequence: submit RejectAgentResponseCommand, alt 2 branch 1 of 5: response not found"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 5: response not found"
    qbrain.server.api -> qbrain.libs.client "404 not_found, details.resource response"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-19-alt2-2" "Sequence diagram converted from fence F162 (line 4504)." {
    title "Sequence: submit RejectAgentResponseCommand, alt 2 branch 2 of 5: already Rejected (same transition)"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 5: already Rejected (same transition)"
    qbrain.server.api -> qbrain.libs.client "200 AgentResponseResult, unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-19-alt2-3" "Sequence diagram converted from fence F162 (line 4504)." {
    title "Sequence: submit RejectAgentResponseCommand, alt 2 branch 3 of 5: Superseded (the lifecycle does not allow it)"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 3 of 5: Superseded (the lifecycle does not allow it)"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_transition_invalid"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-19-alt2-4" "Sequence diagram converted from fence F162 (line 4504)." {
    title "Sequence: submit RejectAgentResponseCommand, alt 2 branch 4 of 5: transitioned"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 4 of 5: transitioned"
    qbrain.server.api -> qbrain.libs.client "200 AgentResponseResult, status Rejected"
    qbrain.libs.client -> qbrain.libs.client "Note: excluded from trusted retrieval"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-19-alt2-5" "Sequence diagram converted from fence F162 (line 4504)." {
    title "Sequence: submit RejectAgentResponseCommand, alt 2 branch 5 of 5: retries exhausted"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 5 of 5: retries exhausted"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: retry the same request within the bounded retries of C9"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, part 1 of 3"
    qbrain.libs.client -> qbrain.server.api "POST /responses/{responseId}/supersede"
    qbrain.libs.client -> qbrain.libs.client "Note over Caller and Controller: supersededByResponseId, reason"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-part2" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, part 2 of 3"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "SendAsync(SupersedeAgentResponseCommand)"
    qbrain.server.api -> qbrain.server.api "Note over Controller and Handler: ref RequestPipeline, gated"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-part3" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, part 3 of 3"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt1-1" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 1 branch 1 of 2: self-reference or blank reason"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: self-reference or blank reason"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt1-2" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 1 branch 2 of 2: request valid, part 1 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 2: request valid"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt1-2-part2" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 1 branch 2 of 2: request valid, part 2 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(1 of 2) Note over Handler and Store: SerializableWrite, both"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt1-2-part3" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 1 branch 2 of 2: request valid, part 3 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(2 of 2) responses locked"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "load the original and the replacement"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt1-2-part4" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 1 branch 2 of 2: request valid, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt2-1" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 2 branch 1 of 4: either not found"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 4: either not found"
    qbrain.server.api -> qbrain.libs.client "404 not_found, details.resource response"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt2-2" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 2 branch 2 of 4: other request, or replacement Rejected or Superseded"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 4: other request, or replacement Rejected or Superseded"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_supersede_invalid"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt2-3" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 2 branch 3 of 4: both valid, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2 branch 3 of 4: both valid"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "ResponseStatusTransition(original, Superseded, replacement)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt2-3-part2" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 2 branch 3 of 4: both valid, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 3: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt3-1" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 3 branch 1 of 3: already superseded by this replacement"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 1 of 3: already superseded by this replacement"
    qbrain.server.api -> qbrain.libs.client "200 AgentResponseResult, unchanged"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt3-2" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 3 branch 2 of 3: original Rejected, or superseded by another response, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 3 branch 2 of 3: original Rejected, or superseded by another"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt3-2-part2" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 3 branch 2 of 3: original Rejected, or superseded by another response, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) response"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_transition_invalid"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt3-3" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 3 branch 3 of 3: transitioned, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 3 branch 3 of 3: transitioned"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "RelationshipWriter: replacement supersedes original"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt3-3-part2" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 3 branch 3 of 3: transitioned, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 4: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt4-1" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 4 branch 1 of 2: the link would close a cycle"
    qbrain.server.api -> qbrain.server.api "alt 4 branch 1 of 2: the link would close a cycle"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_supersede_invalid"
    qbrain.libs.client -> qbrain.libs.client "Note: rolled back, nothing saved"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt4-2" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 4 branch 2 of 2: link written"
    qbrain.server.api -> qbrain.server.api "alt 4 branch 2 of 2: link written"
    qbrain.server.api -> qbrain.libs.client "200 AgentResponseResult, status Superseded"
    qbrain.libs.client -> qbrain.libs.client "Note: retrieval now returns the replacement only"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-20-alt2-4" "Sequence diagram converted from fence F163 (line 4539)." {
    title "Sequence: submit SupersedeAgentResponseCommand, alt 2 branch 4 of 4: retries exhausted"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 4 of 4: retries exhausted"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: retry the same request within the bounded retries of C9"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "POST /staleness/evaluate (responseIds)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-part2" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, part 2 of 4"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "SendAsync(EvaluateStalenessCommand)"
    qbrain.server.api -> qbrain.server.api "Note over Controller and Handler: ref RequestPipeline, gated"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-part3" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, part 3 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "ResponseIdList(responseIds)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-part4" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-alt1-1" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, alt 1 branch 1 of 2: malformed, over 1000 ids, or an id without rsp-"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: malformed, over 1000 ids, or an id without rsp-"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field responseIds"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-alt1-2" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, alt 1 branch 2 of 2: selected (an empty list selects every Submitted, Grading, Trusted), part 1 of 3"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 2 of 2: selected (an empty list selects every Submitted,"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-alt1-2-part2" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, alt 1 branch 2 of 2: selected (an empty list selects every Submitted, Grading, Trusted), part 2 of 3"
    qbrain.server.api -> qbrain.server.api "(2 of 2) Grading, Trusted)"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "loop 1: each selected response (next view)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-alt1-2-part3" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, alt 1 branch 2 of 2: selected (an empty list selects every Submitted, Grading, Trusted), part 3 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-loop1" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, loop 1: each selected response, part 1 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "loop 1: each selected response"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-loop1-part2" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, loop 1: each selected response, part 2 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(1 of 2) Note over Handler and Store: ref EvaluateOneResponseStaleness:"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-loop1-part3" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, loop 1: each selected response, part 3 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(2 of 2) read, then SerializableWrite"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-alt2-1" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, alt 2 branch 1 of 2: every response evaluated, part 1 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2 branch 1 of 2: every response evaluated"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-alt2-1-part2" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, alt 2 branch 1 of 2: every response evaluated, part 2 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(1 of 2) Note: each newly Stale response: EvaluateStalenessCommand"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-alt2-1-part3" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, alt 2 branch 1 of 2: every response evaluated, part 3 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(2 of 2) dispatched for its dependents after commit"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-alt2-1-part4" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, alt 2 branch 1 of 2: every response evaluated, part 4 of 4"
    qbrain.server.api -> qbrain.libs.client "200 evaluatedCount, newlyStale, notFound"
    qbrain.libs.client -> qbrain.libs.client "Note: list each stale response with its changed ref"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-21-alt2-2" "Sequence diagram converted from fence F164 (line 4586)." {
    title "Sequence: submit EvaluateStalenessCommand, alt 2 branch 2 of 2: a response's write exhausted its retries"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 2: a response's write exhausted its retries"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true"
    qbrain.libs.client -> qbrain.libs.client "Note: retry the same request within the bounded retries of C9"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, part 1 of 3"
    qbrain.libs.client -> qbrain.server.api "POST /relationships (CreateAgentContextRelationshipRequest)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-part2" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, part 2 of 3"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "SendAsync(CreateAgentContextRelationshipCommand)"
    qbrain.server.api -> qbrain.server.api "Note over Controller and Handler: ref RequestPipeline, gated"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-part3" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, part 3 of 3"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-alt1-1" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, alt 1 branch 1 of 2: unknown kind or type, or a self-link"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: unknown kind or type, or a self-link"
    qbrain.server.api -> qbrain.libs.client "400 agent_context_relationship_invalid"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-alt1-2" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, alt 1 branch 2 of 2: link well formed, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 2: link well formed"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strSharedComponents "resolve both refs, hash the target"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-alt1-2-part2" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, alt 1 branch 2 of 2: link well formed, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-alt2-1" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, alt 2 branch 1 of 2: a ref names no existing source"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 2: a ref names no existing source"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field the ref"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-alt2-2" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, alt 2 branch 2 of 2: both refs resolve, part 1 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2 branch 2 of 2: both refs resolve"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-alt2-2-part2" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, alt 2 branch 2 of 2: both refs resolve, part 2 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note over Handler and Store: SerializableWrite, database only"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "RelationshipWriter(link, createdBy the agent)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-alt2-2-part3" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, alt 2 branch 2 of 2: both refs resolve, part 3 of 3"
    qbrain.server.api -> qbrain.server.api "alt 3: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-alt3-1" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, alt 3 branch 1 of 4: type supersedes"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 1 of 4: type supersedes"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field type"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-alt3-2" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, alt 3 branch 2 of 4: identical link stored"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 2 of 4: identical link stored"
    qbrain.server.api -> qbrain.libs.client "200 link, created false"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-alt3-3" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, alt 3 branch 3 of 4: new link"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 3 of 4: new link"
    qbrain.server.api -> qbrain.libs.client "201 link, created true"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-22-alt3-4" "Sequence diagram converted from fence F165 (line 4618)." {
    title "Sequence: submit CreateAgentContextRelationshipCommand, alt 3 branch 4 of 4: retries exhausted"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 4 of 4: retries exhausted"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: retry the same request within the bounded retries of C9"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-23" "Sequence diagram converted from fence F166 (line 4656)." {
    title "Sequence: submit DeleteAgentContextRelationshipCommand, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "DELETE /relationships/{relationshipId}"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-23-part2" "Sequence diagram converted from fence F166 (line 4656)." {
    title "Sequence: submit DeleteAgentContextRelationshipCommand, part 2 of 4"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "SendAsync(DeleteAgentContextRelationshipCommand)"
    qbrain.server.api -> qbrain.server.api "Note over Controller and Handler: ref RequestPipeline, gated"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-23-part3" "Sequence diagram converted from fence F166 (line 4656)." {
    title "Sequence: submit DeleteAgentContextRelationshipCommand, part 3 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "load the link"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-23-part4" "Sequence diagram converted from fence F166 (line 4656)." {
    title "Sequence: submit DeleteAgentContextRelationshipCommand, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-23-alt1-1" "Sequence diagram converted from fence F166 (line 4656)." {
    title "Sequence: submit DeleteAgentContextRelationshipCommand, alt 1 branch 1 of 3: not found"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 3: not found"
    qbrain.server.api -> qbrain.libs.client "404 not_found, details.resource relationship"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-23-alt1-2" "Sequence diagram converted from fence F166 (line 4656)." {
    title "Sequence: submit DeleteAgentContextRelationshipCommand, alt 1 branch 2 of 3: made by a command (CreatedBy names it)"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 3: made by a command (CreatedBy names it)"
    qbrain.server.api -> qbrain.libs.client "409 agent_context_relationship_protected"
    qbrain.libs.client -> qbrain.libs.client "Note: report the link as protected"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-23-alt1-3" "Sequence diagram converted from fence F166 (line 4656)." {
    title "Sequence: submit DeleteAgentContextRelationshipCommand, alt 1 branch 3 of 3: made by a client, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 3 of 3: made by a client"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "delete the link"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-23-alt1-3-part2" "Sequence diagram converted from fence F166 (line 4656)." {
    title "Sequence: submit DeleteAgentContextRelationshipCommand, alt 1 branch 3 of 3: made by a client, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "204"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-24" "Sequence diagram converted from fence F167 (line 4680)." {
    title "Sequence: submit QueryAgentContextRelationshipsQuery, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "GET /relationships?nodeKind&nodeRef&type&direction&limit&offset"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-24-part2" "Sequence diagram converted from fence F167 (line 4680)." {
    title "Sequence: submit QueryAgentContextRelationshipsQuery, part 2 of 4"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "QueryAsync(QueryAgentContextRelationshipsQuery)"
    qbrain.server.api -> qbrain.server.api "(1 of 2) Note over Controller and Handler: ref RequestPipeline, not gated:"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-24-part3" "Sequence diagram converted from fence F167 (line 4680)." {
    title "Sequence: submit QueryAgentContextRelationshipsQuery, part 3 of 4"
    qbrain.server.api -> qbrain.server.api "(2 of 2) readable in Mode Off"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-24-part4" "Sequence diagram converted from fence F167 (line 4680)." {
    title "Sequence: submit QueryAgentContextRelationshipsQuery, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-24-alt1-1" "Sequence diagram converted from fence F167 (line 4680)." {
    title "Sequence: submit QueryAgentContextRelationshipsQuery, alt 1 branch 1 of 2: kind without ref, unknown type or direction, or outside PageBounds, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 1 of 2: kind without ref, unknown type or direction, or"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-24-alt1-1-part2" "Sequence diagram converted from fence F167 (line 4680)." {
    title "Sequence: submit QueryAgentContextRelationshipsQuery, alt 1 branch 1 of 2: kind without ref, unknown type or direction, or outside PageBounds, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) outside PageBounds"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-24-alt1-2" "Sequence diagram converted from fence F167 (line 4680)." {
    title "Sequence: submit QueryAgentContextRelationshipsQuery, alt 1 branch 2 of 2: filters valid, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 2: filters valid"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "PagedQuery: out, in, or both, by type"
    qbrain.server.mcpDbContext -> qbrain.server.strAgentContextHandlers "PageResult, CreatedUtc then RelationshipId"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-24-alt1-2-part2" "Sequence diagram converted from fence F167 (line 4680)." {
    title "Sequence: submit QueryAgentContextRelationshipsQuery, alt 1 branch 2 of 2: filters valid, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "200 PageResult of relationships"
    qbrain.libs.client -> qbrain.libs.client "Note: PagedQuery paging"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "POST /retrieve (RetrieveAgentContextCandidatesRequest)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-part2" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, part 2 of 4"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "QueryAsync(RetrieveAgentContextCandidatesQuery)"
    qbrain.server.api -> qbrain.server.api "(1 of 2) Note over Controller and Handler: ref RequestPipeline, not gated:"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-part3" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, part 3 of 4"
    qbrain.server.api -> qbrain.server.api "(2 of 2) readable in Mode Off"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-part4" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-alt1-1" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, alt 1 branch 1 of 2: trustedOnly false for PriorResponse, partial turn key, or limit outside PageBounds, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 1 of 2: trustedOnly false for PriorResponse,"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-alt1-1-part2" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, alt 1 branch 1 of 2: trustedOnly false for PriorResponse, partial turn key, or limit outside PageBounds, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) partial turn key, or limit outside PageBounds"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-alt1-2" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, alt 1 branch 2 of 2: request valid, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 2: request valid"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "opt 1: turn named (next view)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-alt1-2-part2" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, alt 1 branch 2 of 2: request valid, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-opt1" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, opt 1: turn named"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "opt 1: turn named"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "TurnLocator(agent, sessionId, requestId)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-alt2-1" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 1 of 3: InvalidIdentifier"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 3: InvalidIdentifier"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-alt2-2" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 2 of 3: SessionNotFound or TurnNotFound"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 3: SessionNotFound or TurnNotFound"
    qbrain.server.api -> qbrain.libs.client "404 not_found, details.resource session_turn"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-alt2-3" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 3 of 3: no turn named, or the turn found, part 1 of 6"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2 branch 3 of 3: no turn named, or the turn found"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-alt2-3-part2" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 3 of 3: no turn named, or the turn found, part 2 of 6"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(1 of 2) Note over Handler and Store: ref ChooseRetrievalPath (below):"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-alt2-3-part3" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 3 of 3: no turn named, or the turn found, part 3 of 6"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(2 of 2) candidates, retrievalPath, fallbackReason"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-alt2-3-part4" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 3 of 3: no turn named, or the turn found, part 4 of 6"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "IResponseStalenessRule, read-only"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-alt2-3-part5" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 3 of 3: no turn named, or the turn found, part 5 of 6"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "CandidateRanker, then cut to limit"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-25-alt2-3-part6" "Sequence diagram converted from fence F168 (line 4704)." {
    title "Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 3 of 3: no turn named, or the turn found, part 6 of 6"
    qbrain.server.api -> qbrain.libs.client "200 retrievalPath, fallbackReason when one applied"
    qbrain.libs.client -> qbrain.libs.client "Note: use the candidates in rank order"
    autolayout lr 300 100
}
dynamic qbrain.server "ChooseRetrievalPath" "Sequence diagram converted from fence F169 (line 4738)." {
    title "Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), part 1 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(1 of 2) opt 1: preferGraphRag, GraphRag:UseForRetrieval, and kinds only"
    autolayout lr 300 100
}
dynamic qbrain.server "ChooseRetrievalPath-part2" "Sequence diagram converted from fence F169 (line 4738)." {
    title "Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), part 2 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(2 of 2) PriorResponse (next view)"
    autolayout lr 300 100
}
dynamic qbrain.server "ChooseRetrievalPath-part3" "Sequence diagram converted from fence F169 (line 4738)." {
    title "Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), part 3 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ChooseRetrievalPath-opt1" "Sequence diagram converted from fence F169 (line 4738)." {
    title "Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), opt 1: preferGraphRag, GraphRag:UseForRetrieval, and kinds only PriorResponse"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "opt 1: preferGraphRag, GraphRag:UseForRetrieval, and kinds only PriorResponse"
    qbrain.server.strAgentContextHandlers -> qbrain.server.graphrag "GetStatusAsync"
    qbrain.server.graphrag -> qbrain.server.strAgentContextHandlers "enabled and indexed, or a fallbackReason"
    autolayout lr 300 100
}
dynamic qbrain.server "ChooseRetrievalPath-alt1-1" "Sequence diagram converted from fence F169 (line 4738)." {
    title "Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), alt 1 branch 1 of 2: GraphRAG enabled and indexed"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 1 of 2: GraphRAG enabled and indexed"
    qbrain.server.strAgentContextHandlers -> qbrain.server.graphrag "QueryAsync, workspace scope"
    qbrain.server.graphrag -> qbrain.server.graphrag "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ChooseRetrievalPath-alt2-1" "Sequence diagram converted from fence F169 (line 4738)." {
    title "Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), alt 2 branch 1 of 2: result ok"
    qbrain.server.graphrag -> qbrain.server.graphrag "alt 2 branch 1 of 2: result ok"
    qbrain.server.graphrag -> qbrain.server.strAgentContextHandlers "hits"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: one hit per response of this workspace"
    autolayout lr 300 100
}
dynamic qbrain.server "ChooseRetrievalPath-alt2-2" "Sequence diagram converted from fence F169 (line 4738)." {
    title "Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), alt 2 branch 2 of 2: FallbackUsed or an exception, part 1 of 2"
    qbrain.server.graphrag -> qbrain.server.graphrag "alt 2 branch 2 of 2: FallbackUsed or an exception"
    qbrain.server.graphrag -> qbrain.server.strAgentContextHandlers "fallbackReason, or graphrag_error"
    autolayout lr 300 100
}
dynamic qbrain.server "ChooseRetrievalPath-alt2-2-part2" "Sequence diagram converted from fence F169 (line 4738)." {
    title "Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), alt 2 branch 2 of 2: FallbackUsed or an exception, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "relational path"
    qbrain.server.mcpDbContext -> qbrain.server.strAgentContextHandlers "candidates"
    autolayout lr 300 100
}
dynamic qbrain.server "ChooseRetrievalPath-alt1-2" "Sequence diagram converted from fence F169 (line 4738)." {
    title "Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), alt 1 branch 2 of 2: not preferred, or a fallbackReason from the request or the status, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(1 of 2) alt 1 branch 2 of 2: not preferred, or a fallbackReason from the"
    autolayout lr 300 100
}
dynamic qbrain.server "ChooseRetrievalPath-alt1-2-part2" "Sequence diagram converted from fence F169 (line 4738)." {
    title "Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), alt 1 branch 2 of 2: not preferred, or a fallbackReason from the request or the status, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(2 of 2) request or the status"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "relational path: links first, then hybrid search"
    qbrain.server.mcpDbContext -> qbrain.server.strAgentContextHandlers "candidates"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "POST /projection/graphrag (responseIds)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-part2" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, part 2 of 4"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "SendAsync(ProjectAgentResponsesCommand)"
    qbrain.server.api -> qbrain.server.api "Note over Controller and Handler: ref RequestPipeline, gated"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-part3" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, part 3 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "ResponseIdList(responseIds)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-part4" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-alt1-1" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 1 of 2: malformed, over 1000 ids, or an id without rsp-"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: malformed, over 1000 ids, or an id without rsp-"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field responseIds"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-alt1-2" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 1 of 8"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(1 of 2) alt 1 branch 2 of 2: selected (an empty list selects every Pending and"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-alt1-2-part2" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 2 of 8"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(2 of 2) Failed row, and Skipped rows once ingest is enabled)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-alt1-2-part3" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 3 of 8"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "loop 1: each selected response (next view)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-alt1-2-part4" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 4 of 8"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(1 of 5) Note over Handler and Store: ref ReconcileProjectedDocuments"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-alt1-2-part5" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 5 of 8"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(2 of 5) (nested activity, section 6.5): delete agent-response"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-alt1-2-part6" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 6 of 8"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(3 of 5) documents no Projected row names that were ingested before the pass"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-alt1-2-part7" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 7 of 8"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(4 of 5) started, and return Projected rows whose document is gone to"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-alt1-2-part8" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 8 of 8"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(5 of 5) Pending"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-loop1" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, loop 1: each selected response, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "loop 1: each selected response"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-loop1-part2" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, loop 1: each selected response, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note over Handler and Store: ref ProjectOneResponse (below)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-alt2-1" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, alt 2 branch 1 of 2: every response handled"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 2: every response handled"
    qbrain.server.api -> qbrain.libs.client "200 projected, removed, skipped, failed, notFound"
    qbrain.libs.client -> qbrain.libs.client "Note: Failed rows are selected again on the next pass"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-26-alt2-2" "Sequence diagram converted from fence F170 (line 4768)." {
    title "Sequence: submit ProjectAgentResponsesCommand, alt 2 branch 2 of 2: a row write exhausted its retries"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 2: a row write exhausted its retries"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true"
    qbrain.libs.client -> qbrain.libs.client "Note: retry the same request within the bounded retries of C9"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), part 1 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "SendAsync(EvaluateStalenessCommand), this response"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-part2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), part 2 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: fresh, or Stale persisted"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "read the response status and its projection row"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-part3" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), part 3 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt1-1" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 1 of 4: Trusted, not projected at its ContentSha256, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 1 of 4: Trusted, not projected at its ContentSha256"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt1-1-part2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 1 of 4: Trusted, not projected at its ContentSha256, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt2-1" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 2 branch 1 of 2: ProjectionEnabled and GraphRAG enabled, part 1 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2 branch 1 of 2: ProjectionEnabled and GraphRAG enabled"
    qbrain.server.strAgentContextHandlers -> qbrain.server.graphrag "IngestTextAsync, workspace scope, root ids"
    qbrain.server.graphrag -> qbrain.server.strAgentContextHandlers "document id, or an error"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt2-1-part2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 2 branch 1 of 2: ProjectionEnabled and GraphRAG enabled, part 2 of 3"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "SerializableWrite: re-read the status and the row, save the row"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(1 of 2) alt 3: one of 4 branches, drawn"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt2-1-part3" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 2 branch 1 of 2: ProjectionEnabled and GraphRAG enabled, part 3 of 3"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(2 of 2) in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-1" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 1 of 4: still Trusted, ingest ok, part 1 of 4"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(1 of 2) alt 3 branch 1 of 4: still"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-1-part2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 1 of 4: still Trusted, ingest ok, part 2 of 4"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(2 of 2) Trusted, ingest ok"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-1-part3" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 1 of 4: still Trusted, ingest ok, part 3 of 4"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(1 of 2) Note: Projected,"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-1-part4" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 1 of 4: still Trusted, ingest ok, part 4 of 4"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(2 of 2) document id and hash stored"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 2 of 4: row already Projected by an overlapping pass, same content, part 1 of 6"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(1 of 4) alt 3 branch 2 of 4: row"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-2-part2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 2 of 4: row already Projected by an overlapping pass, same content, part 2 of 6"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(2 of 4) already Projected by an"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-2-part3" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 2 of 4: row already Projected by an overlapping pass, same content, part 3 of 6"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(3 of 4) overlapping pass, same"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-2-part4" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 2 of 4: row already Projected by an overlapping pass, same content, part 4 of 6"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(4 of 4) content"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-2-part5" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 2 of 4: row already Projected by an overlapping pass, same content, part 5 of 6"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "Note: the stored document kept"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-2-part6" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 2 of 4: row already Projected by an overlapping pass, same content, part 6 of 6"
    qbrain.server.strAgentContextHandlers -> qbrain.server.graphrag "DeleteDocumentAsync, the document this pass ingested"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-3" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 3 of 4: no longer Trusted, ingest ok, part 1 of 4"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(1 of 2) alt 3 branch 3 of 4: no longer"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-3-part2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 3 of 4: no longer Trusted, ingest ok, part 2 of 4"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(2 of 2) Trusted, ingest ok"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-3-part3" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 3 of 4: no longer Trusted, ingest ok, part 3 of 4"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(1 of 2) Note: keeps the document id,"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-3-part4" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 3 of 4: no longer Trusted, ingest ok, part 4 of 4"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(2 of 2) Pending for removal"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-4" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 4 of 4: ingest error, part 1 of 2"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "alt 3 branch 4 of 4: ingest error"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt3-4-part2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 4 of 4: ingest error, part 2 of 2"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "Note: Failed with LastError"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt2-2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 2 branch 2 of 2: ProjectionEnabled false, or GraphRAG disabled"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2 branch 2 of 2: ProjectionEnabled false, or GraphRAG disabled"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "SerializableWrite: Skipped with skippedReason"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt1-2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 2 of 4: not Trusted, the row holds a GraphRagDocumentId, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 4: not Trusted, the row holds a GraphRagDocumentId"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt1-2-part2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 2 of 4: not Trusted, the row holds a GraphRagDocumentId, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 4: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt4-1" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 4 branch 1 of 2: GraphRAG enabled, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 4 branch 1 of 2: GraphRAG enabled"
    qbrain.server.strAgentContextHandlers -> qbrain.server.graphrag "DeleteDocumentAsync"
    qbrain.server.graphrag -> qbrain.server.strAgentContextHandlers "ok, already gone (Success false), or an error"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt4-1-part2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 4 branch 1 of 2: GraphRAG enabled, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "SerializableWrite: Removed when ok or already gone, Failed with LastError on an error"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt4-2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 4 branch 2 of 2: GraphRAG disabled, part 1 of 4"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(1 of 2) alt 4 branch 2 of 2: GraphRAG"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt4-2-part2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 4 branch 2 of 2: GraphRAG disabled, part 2 of 4"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(2 of 2) disabled"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt4-2-part3" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 4 branch 2 of 2: GraphRAG disabled, part 3 of 4"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(1 of 2) Note: stays Pending until"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt4-2-part4" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 4 branch 2 of 2: GraphRAG disabled, part 4 of 4"
    qbrain.server.mcpDbContext -> qbrain.server.mcpDbContext "(2 of 2) GraphRAG is enabled"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt1-3" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 3 of 4: not Trusted, no document"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 3 of 4: not Trusted, no document"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "SerializableWrite: Removed, never selected again"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt1-4" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 4 of 4: nothing to change, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 4 of 4: nothing to change"
    autolayout lr 300 100
}
dynamic qbrain.server "ProjectOneResponse-Sequence-alt1-4-part2" "Sequence diagram converted from fence F171 (line 4800)." {
    title "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 4 of 4: nothing to change, part 2 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "Note: continue with the next response"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), part 1 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) Note over Plugin and Server: host session start:"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-part2" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), part 2 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) GetAgentContextStatusQuery, Mode cached, Off when unreadable"
    autolayout lr 300 100
}
dynamic * "STR-06-27-part3" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), part 3 of 4"
    developer -> agent "prompt"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-part4" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), part 4 of 4"
    agent -> qbrain.plugins.pluginPs "prompt hook"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 2 branches, drawn in the next views"
    agent -> agent "alt 2: one of 5 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt1-1" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 1 branch 1 of 2: bridge host (claude-code, claude-cowork), part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 2: bridge host (claude-code, claude-cowork)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt1-1-part2" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 1 branch 1 of 2: bridge host (claude-code, claude-cowork), part 2 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) Note over Plugin and Server: ref ApplyArrivalBridgeRecord"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt1-1-part3" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 1 branch 1 of 2: bridge host (claude-code, claude-cowork), part 3 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) through Sync-PluginArrivalLog: bridge-minted requestId"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt1-2" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 1 branch 2 of 2: other hosts, part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 2 of 2: other hosts"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt1-2-part2" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 1 branch 2 of 2: other hosts, part 2 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) Note over Plugin and Server: ref OpenTurnFromPromptHook:"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt1-2-part3" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 1 branch 2 of 2: other hosts, part 3 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) MintSessionTurnCommand, Resolve-MintOutcome"
    autolayout lr 300 100
}
dynamic * "STR-06-27-alt2-1" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 1 of 5: Mode Off, D6 unsupported, unsupported or terminal merge, or the host cannot refuse, part 1 of 2"
    agent -> agent "(1 of 2) alt 2 branch 1 of 5: Mode Off, D6 unsupported, unsupported or terminal merge,"
    autolayout lr 300 100
}
dynamic * "STR-06-27-alt2-1-part2" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 1 of 5: Mode Off, D6 unsupported, unsupported or terminal merge, or the host cannot refuse, part 2 of 2"
    agent -> agent "(2 of 2) or the host cannot refuse"
    agent -> strHostModel "prompt with history (stateful, as today)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt2-2" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 2 of 5: mint pending (outage), part 1 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 2 of 5: mint pending (outage)"
    qbrain.plugins.pluginPs -> agent "refuse the prompt with the outage"
    autolayout lr 300 100
}
dynamic * "STR-06-27-alt2-2-part2" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 2 of 5: mint pending (outage), part 2 of 4"
    agent -> developer "prompt refused, no answer"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt2-2-part3" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 2 of 5: mint pending (outage), part 3 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) Note over Plugin and Server: ref PersistTurnWriteWithReceipt:"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt2-2-part4" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 2 of 5: mint pending (outage), part 4 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) canceled close queued behind the mint record"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt2-3" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 3 of 5: bridge host, enqueue record not yet in the transcript, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 3 of 5: bridge host, enqueue record not yet in the transcript"
    qbrain.plugins.pluginPs -> agent "refuse the prompt, as for a build failure"
    autolayout lr 300 100
}
dynamic * "STR-06-27-alt2-3-part2" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 3 of 5: bridge host, enqueue record not yet in the transcript, part 2 of 2"
    agent -> developer "prompt refused, no answer"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt2-4" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 4 of 5: final mint refusal, or no mint (nonce_source_missing, turn-open-contended), part 1 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 2 branch 4 of 5: final mint refusal, or no mint"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt2-4-part2" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 4 of 5: final mint refusal, or no mint (nonce_source_missing, turn-open-contended), part 2 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) (nonce_source_missing, turn-open-contended)"
    qbrain.plugins.pluginPs -> agent "refuse the prompt, no turn exists"
    autolayout lr 300 100
}
dynamic * "STR-06-27-alt2-4-part3" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 4 of 5: final mint refusal, or no mint (nonce_source_missing, turn-open-contended), part 3 of 4"
    agent -> developer "prompt refused, no answer"
    autolayout lr 300 100
}
dynamic qbrain.plugins "STR-06-27-alt2-4-part4" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 4 of 5: final mint refusal, or no mint (nonce_source_missing, turn-open-contended), part 4 of 4"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: the Stop gate warns the refusal, naming the prompt"
    autolayout lr 300 100
}
dynamic * "STR-06-27-alt2-5" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 5 of 5: delivered, or a duplicate delivery, in Pilot or Stateless (absorbed_mid_turn runs as Pilot), part 1 of 5"
    developer -> developer "(1 of 2) alt 2 branch 5 of 5: delivered, or a duplicate delivery, in"
    autolayout lr 300 100
}
dynamic * "STR-06-27-alt2-5-part2" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 5 of 5: delivered, or a duplicate delivery, in Pilot or Stateless (absorbed_mid_turn runs as Pilot), part 2 of 5"
    developer -> developer "(2 of 2) Pilot or Stateless (absorbed_mid_turn runs as Pilot)"
    autolayout lr 300 100
}
dynamic * "STR-06-27-alt2-5-part3" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 5 of 5: delivered, or a duplicate delivery, in Pilot or Stateless (absorbed_mid_turn runs as Pilot), part 3 of 5"
    developer -> developer "(1 of 2) Note over Operator and Model: ref BuildAndDeliverPackage"
    autolayout lr 300 100
}
dynamic * "STR-06-27-alt2-5-part4" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 5 of 5: delivered, or a duplicate delivery, in Pilot or Stateless (absorbed_mid_turn runs as Pilot), part 4 of 5"
    developer -> developer "(2 of 2) (below), a failed build ends the invocation there"
    agent -> agent "(1 of 2) Note over Host and Model: ref SubmitAndGradeResponse (below), after"
    autolayout lr 300 100
}
dynamic * "STR-06-27-alt2-5-part5" "Sequence diagram converted from fence F172 (line 4855)." {
    title "Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 5 of 5: delivered, or a duplicate delivery, in Pilot or Stateless (absorbed_mid_turn runs as Pilot), part 5 of 5"
    agent -> agent "(2 of 2) a Built package"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence)"
    qbrain.plugins.pluginPs -> qbrain.server "BuildAgentContextPackageCommand, idempotencyKey inv- plus the turn key hash"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt1-1" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 1 of 3: 201, or 200 with package status Built, part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 3: 201, or 200 with package status Built"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt1-1-part2" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 1 of 3: 201, or 200 with package status Built, part 2 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "keep packageId in the per-turn file"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt1-1-part3" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 1 of 3: 201, or 200 with package status Built, part 3 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt2-1" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 2 branch 1 of 2: Stateless prompt delivery, host with history exclusion, part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 2 branch 1 of 2: Stateless prompt delivery, host with history"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt2-1-part2" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 2 branch 1 of 2: Stateless prompt delivery, host with history exclusion, part 2 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) exclusion"
    qbrain.plugins.pluginPs -> agent "output.messages: package, request, required memories"
    autolayout lr 300 100
}
dynamic * "BuildAndDeliverPackage-alt2-1-part3" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 2 branch 1 of 2: Stateless prompt delivery, host with history exclusion, part 3 of 3"
    agent -> strHostModel "that message list, no history"
    autolayout lr 300 100
}
dynamic * "BuildAndDeliverPackage-alt2-2" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 2 branch 2 of 2: Pilot, absorbed_mid_turn, or no history exclusion, part 1 of 2"
    agent -> agent "(1 of 2) alt 2 branch 2 of 2: Pilot, absorbed_mid_turn, or no history"
    autolayout lr 300 100
}
dynamic * "BuildAndDeliverPackage-alt2-2-part2" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 2 branch 2 of 2: Pilot, absorbed_mid_turn, or no history exclusion, part 2 of 2"
    agent -> agent "(2 of 2) exclusion"
    agent -> strHostModel "prompt with history (stateful)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt1-2" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 2 of 3: 409 agent_context_disabled, part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 2 of 3: 409 agent_context_disabled"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt1-2-part2" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 2 of 3: 409 agent_context_disabled, part 2 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: caches Off: no refusal, no canceled close"
    autolayout lr 300 100
}
dynamic * "BuildAndDeliverPackage-alt1-2-part3" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 2 of 3: 409 agent_context_disabled, part 3 of 3"
    agent -> strHostModel "prompt with history (stateful)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt1-3" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 3 of 3: 422, any other error, an outage, or a package not Built, part 1 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) alt 1 branch 3 of 3: 422, any other error, an outage, or a package not"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt1-3-part2" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 3 of 3: 422, any other error, an outage, or a package not Built, part 2 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) Built"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt1-3-part3" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 3 of 3: 422, any other error, an outage, or a package not Built, part 3 of 3"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 3: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt3-1" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 1 of 2: absorbed_mid_turn, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 3 branch 1 of 2: absorbed_mid_turn"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt3-1-part2" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 1 of 2: absorbed_mid_turn, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: failure kept in the per-turn file, nothing refused"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt3-2" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 2 of 2: prompt delivery, part 1 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 3 branch 2 of 2: prompt delivery"
    qbrain.plugins.pluginPs -> agent "refuse the prompt with the code and message"
    autolayout lr 300 100
}
dynamic * "BuildAndDeliverPackage-alt3-2-part2" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 2 of 2: prompt delivery, part 2 of 5"
    agent -> developer "prompt refused, no answer"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt3-2-part3" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 2 of 2: prompt delivery, part 3 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(1 of 2) Note over Plugin and Server: ref PersistTurnWriteWithReceipt:"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt3-2-part4" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 2 of 2: prompt delivery, part 4 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "(2 of 2) own turn canceled, no Test-ModelEvidence"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-alt3-2-part5" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 2 of 2: prompt delivery, part 5 of 5"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "opt 1: close queued: an outage or a retryable answer (next view)"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-opt1" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), opt 1: close queued: an outage or a retryable answer, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "opt 1: close queued: an outage or a retryable answer"
    autolayout lr 300 100
}
dynamic qbrain.plugins "BuildAndDeliverPackage-opt1-part2" "Sequence diagram converted from fence F173 (line 4894)." {
    title "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), opt 1: close queued: an outage or a retryable answer, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: Stop gate warns degraded persistence, never blocks"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SubmitAndGradeResponse" "Sequence diagram converted from fence F174 (line 4932)." {
    title "Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), part 1 of 2"
    strHostModel -> qbrain.plugins.pluginPs "agent_context_submit_response tool call"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SubmitAndGradeResponse-part2" "Sequence diagram converted from fence F174 (line 4932)." {
    title "Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.server "SubmitAgentResponseCommand, kept packageId, invocationMode"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SubmitAndGradeResponse-alt1-1" "Sequence diagram converted from fence F174 (line 4932)." {
    title "Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 1 branch 1 of 3: 201 or 200"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 1 of 3: 201 or 200"
    qbrain.plugins.pluginPs -> qbrain.server "RunDeterministicGradeCommand"
    qbrain.server -> qbrain.server "alt 2: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SubmitAndGradeResponse-alt2-1" "Sequence diagram converted from fence F174 (line 4932)." {
    title "Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 2 branch 1 of 2: 201, part 1 of 2"
    qbrain.server -> qbrain.server "alt 2 branch 1 of 2: 201"
    qbrain.server -> qbrain.plugins.pluginPs "grade, responseStatus Grading or Stale"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SubmitAndGradeResponse-alt2-1-part2" "Sequence diagram converted from fence F174 (line 4932)." {
    title "Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 2 branch 1 of 2: 201, part 2 of 2"
    qbrain.plugins.pluginPs -> strHostModel "tool result: the model grade comes from a later HV request"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SubmitAndGradeResponse-alt2-2" "Sequence diagram converted from fence F174 (line 4932)." {
    title "Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 2 branch 2 of 2: an error or an outage"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 2 branch 2 of 2: an error or an outage"
    qbrain.plugins.pluginPs -> strHostModel "tool result with the code, the grade is re-run on request"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SubmitAndGradeResponse-alt1-2" "Sequence diagram converted from fence F174 (line 4932)." {
    title "Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 1 branch 2 of 3: 409 agent_context_disabled, part 1 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 2 of 3: 409 agent_context_disabled"
    qbrain.plugins.pluginPs -> strHostModel "tool result, agent_context_disabled"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SubmitAndGradeResponse-alt1-2-part2" "Sequence diagram converted from fence F174 (line 4932)." {
    title "Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 1 branch 2 of 3: 409 agent_context_disabled, part 2 of 2"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "Note: caches Off, the turn closes as any stateful turn"
    autolayout lr 300 100
}
dynamic qbrain.plugins "SubmitAndGradeResponse-alt1-3" "Sequence diagram converted from fence F174 (line 4932)." {
    title "Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 1 branch 3 of 3: any other error or an outage"
    qbrain.plugins.pluginPs -> qbrain.plugins.pluginPs "alt 1 branch 3 of 3: any other error or an outage"
    qbrain.plugins.pluginPs -> strHostModel "tool result with the code and message"
    strHostModel -> strHostModel "Note: resubmits or reports it"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-28" "Sequence diagram converted from fence F175 (line 4961)." {
    title "Sequence: submit GetAgentContextPilotReportQuery, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "GET /pilot/report?fromUtc&toUtc"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-28-part2" "Sequence diagram converted from fence F175 (line 4961)." {
    title "Sequence: submit GetAgentContextPilotReportQuery, part 2 of 4"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "QueryAsync(GetAgentContextPilotReportQuery)"
    qbrain.server.api -> qbrain.server.api "(1 of 2) Note over Controller and Handler: ref RequestPipeline, not gated:"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-28-part3" "Sequence diagram converted from fence F175 (line 4961)." {
    title "Sequence: submit GetAgentContextPilotReportQuery, part 3 of 4"
    qbrain.server.api -> qbrain.server.api "(2 of 2) readable in Mode Off"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-28-part4" "Sequence diagram converted from fence F175 (line 4961)." {
    title "Sequence: submit GetAgentContextPilotReportQuery, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-28-alt1-1" "Sequence diagram converted from fence F175 (line 4961)." {
    title "Sequence: submit GetAgentContextPilotReportQuery, alt 1 branch 1 of 2: fromUtc or toUtc missing or not UTC, or from not before to, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 1 branch 1 of 2: fromUtc or toUtc missing or not UTC, or from"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-28-alt1-1-part2" "Sequence diagram converted from fence F175 (line 4961)." {
    title "Sequence: submit GetAgentContextPilotReportQuery, alt 1 branch 1 of 2: fromUtc or toUtc missing or not UTC, or from not before to, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) not before to"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-28-alt1-2" "Sequence diagram converted from fence F175 (line 4961)." {
    title "Sequence: submit GetAgentContextPilotReportQuery, alt 1 branch 2 of 2: window valid, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 2: window valid"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "group the window's responses by invocationMode"
    qbrain.server.mcpDbContext -> qbrain.server.strAgentContextHandlers "counts, latest grades by RecordedUtc"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-28-alt1-2-part2" "Sequence diagram converted from fence F175 (line 4961)." {
    title "Sequence: submit GetAgentContextPilotReportQuery, alt 1 branch 2 of 2: window valid, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "200 AgentContextPilotReport"
    qbrain.libs.client -> qbrain.libs.client "Note: compare the modes before changing any default"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, part 1 of 3"
    qbrain.libs.client -> qbrain.server.api "POST /hv-requests (CreateAgentContextHvRequestRequest)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-part2" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, part 2 of 3"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "SendAsync(CreateAgentContextHvRequestCommand)"
    qbrain.server.api -> qbrain.server.api "Note over Controller and Handler: ref RequestPipeline, gated"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-part3" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, part 3 of 3"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt1-1" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 1 branch 1 of 2: blank field"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: blank field"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt1-2" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 1 branch 2 of 2: fields given, part 1 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 2: fields given"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt1-2-part2" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 1 branch 2 of 2: fields given, part 2 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(1 of 2) Note over Handler and Store: SerializableWrite: one"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt1-2-part3" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 1 branch 2 of 2: fields given, part 3 of 4"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "(2 of 2) delegate from the key lookup to the save"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "IdempotentCreate.Find(idempotencyKey, request fingerprint)"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt1-2-part4" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 1 branch 2 of 2: fields given, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 2: one of 4 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt2-1" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 2 branch 1 of 4: same inputs (a retry or a concurrent winner)"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 1 of 4: same inputs (a retry or a concurrent winner)"
    qbrain.server.api -> qbrain.libs.client "200 the stored AgentContextHvRequestResult"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt2-2" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 2 branch 2 of 4: other inputs"
    qbrain.server.api -> qbrain.server.api "alt 2 branch 2 of 4: other inputs"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable false"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt2-3" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 2 branch 3 of 4: absent, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 2 branch 3 of 4: absent"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "TurnLocator(agent, sessionId), both endpoint turns"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt2-3-part2" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 2 branch 3 of 4: absent, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "alt 3: one of 3 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt3-1" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 3 branch 1 of 3: InvalidIdentifier"
    qbrain.server.api -> qbrain.server.api "alt 3 branch 1 of 3: InvalidIdentifier"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt3-2" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 3 branch 2 of 3: unknown session, bad range, too long, or no gradable response, part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 3 branch 2 of 3: unknown session, bad range, too long, or no"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt3-2-part2" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 3 branch 2 of 3: unknown session, bad range, too long, or no gradable response, part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) gradable response"
    qbrain.server.api -> qbrain.libs.client "400 agent_context_hv_request_invalid, reason"
    qbrain.libs.client -> qbrain.libs.client "Note: correct the range"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt3-3" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 3 branch 3 of 3: range valid, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 3 branch 3 of 3: range valid"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "lock candidates in ascending ResponseId, re-read status"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "IdempotentCreate.Insert request (Open), Pending items"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt3-3-part2" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 3 branch 3 of 3: range valid, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "201 AgentContextHvRequestResult with items"
    qbrain.libs.client -> qbrain.libs.client "Note: hand hvRequestId to the HV agent in its brief"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt2-4" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 2 branch 4 of 4: retries exhausted (SerializableWrite or key allocation), part 1 of 2"
    qbrain.server.api -> qbrain.server.api "(1 of 2) alt 2 branch 4 of 4: retries exhausted (SerializableWrite or key"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-29-alt2-4-part2" "Sequence diagram converted from fence F176 (line 4985)." {
    title "Sequence: submit CreateAgentContextHvRequestCommand, alt 2 branch 4 of 4: retries exhausted (SerializableWrite or key allocation), part 2 of 2"
    qbrain.server.api -> qbrain.server.api "(2 of 2) allocation)"
    qbrain.server.api -> qbrain.libs.client "409 conflict, retryable true, nothing saved"
    qbrain.libs.client -> qbrain.libs.client "Note: retry with the same idempotencyKey within the bounded retries of C9"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-30" "Sequence diagram converted from fence F177 (line 5026)." {
    title "Sequence: submit GetAgentContextHvRequestQuery, part 1 of 5"
    qbrain.libs.client -> qbrain.server.api "GET /hv-requests/{hvRequestId}"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-30-part2" "Sequence diagram converted from fence F177 (line 5026)." {
    title "Sequence: submit GetAgentContextHvRequestQuery, part 2 of 5"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "QueryAsync(GetAgentContextHvRequestQuery)"
    qbrain.server.api -> qbrain.server.api "(1 of 2) Note over Controller and Handler: ref RequestPipeline, not gated:"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-30-part3" "Sequence diagram converted from fence F177 (line 5026)." {
    title "Sequence: submit GetAgentContextHvRequestQuery, part 3 of 5"
    qbrain.server.api -> qbrain.server.api "(2 of 2) readable in Mode Off"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-30-part4" "Sequence diagram converted from fence F177 (line 5026)." {
    title "Sequence: submit GetAgentContextHvRequestQuery, part 4 of 5"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "load the HV request with its items"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-30-part5" "Sequence diagram converted from fence F177 (line 5026)." {
    title "Sequence: submit GetAgentContextHvRequestQuery, part 5 of 5"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-30-alt1-1" "Sequence diagram converted from fence F177 (line 5026)." {
    title "Sequence: submit GetAgentContextHvRequestQuery, alt 1 branch 1 of 2: not found"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: not found"
    qbrain.server.api -> qbrain.libs.client "404 not_found, details.resource hv_request"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-30-alt1-2" "Sequence diagram converted from fence F177 (line 5026)." {
    title "Sequence: submit GetAgentContextHvRequestQuery, alt 1 branch 2 of 2: found"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 2 of 2: found"
    qbrain.server.api -> qbrain.libs.client "200 AgentContextHvRequestResult"
    qbrain.libs.client -> qbrain.libs.client "Note: list Pending items still awaiting the HV agent"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-31" "Sequence diagram converted from fence F178 (line 5047)." {
    title "Sequence: submit QueryAgentContextHvRequestsQuery, part 1 of 4"
    qbrain.libs.client -> qbrain.server.api "GET /hv-requests?sessionId&status&limit&offset"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-31-part2" "Sequence diagram converted from fence F178 (line 5047)." {
    title "Sequence: submit QueryAgentContextHvRequestsQuery, part 2 of 4"
    qbrain.server.api -> qbrain.server.strAgentContextHandlers "QueryAsync(QueryAgentContextHvRequestsQuery)"
    qbrain.server.api -> qbrain.server.api "(1 of 2) Note over Controller and Handler: ref RequestPipeline, not gated:"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-31-part3" "Sequence diagram converted from fence F178 (line 5047)." {
    title "Sequence: submit QueryAgentContextHvRequestsQuery, part 3 of 4"
    qbrain.server.api -> qbrain.server.api "(2 of 2) readable in Mode Off"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-31-part4" "Sequence diagram converted from fence F178 (line 5047)." {
    title "Sequence: submit QueryAgentContextHvRequestsQuery, part 4 of 4"
    qbrain.server.api -> qbrain.server.api "alt 1: one of 2 branches, drawn in the next views"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-31-alt1-1" "Sequence diagram converted from fence F178 (line 5047)." {
    title "Sequence: submit QueryAgentContextHvRequestsQuery, alt 1 branch 1 of 2: unknown status, or limit or offset outside PageBounds"
    qbrain.server.api -> qbrain.server.api "alt 1 branch 1 of 2: unknown status, or limit or offset outside PageBounds"
    qbrain.server.api -> qbrain.libs.client "400 validation_error, details.field"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-31-alt1-2" "Sequence diagram converted from fence F178 (line 5047)." {
    title "Sequence: submit QueryAgentContextHvRequestsQuery, alt 1 branch 2 of 2: filters valid, part 1 of 2"
    qbrain.server.strAgentContextHandlers -> qbrain.server.strAgentContextHandlers "alt 1 branch 2 of 2: filters valid"
    qbrain.server.strAgentContextHandlers -> qbrain.server.mcpDbContext "PagedQuery, CreatedUtc descending then HvRequestId"
    qbrain.server.mcpDbContext -> qbrain.server.strAgentContextHandlers "PageResult"
    autolayout lr 300 100
}
dynamic qbrain.server "STR-06-31-alt1-2-part2" "Sequence diagram converted from fence F178 (line 5047)." {
    title "Sequence: submit QueryAgentContextHvRequestsQuery, alt 1 branch 2 of 2: filters valid, part 2 of 2"
    qbrain.server.api -> qbrain.libs.client "200 PageResult of HV requests without items"
    qbrain.libs.client -> qbrain.libs.client "Note: PagedQuery paging"
    autolayout lr 300 100
}
custom "STR-08-01" "Phase order (activity), part 1 of 2" "Activity diagram converted from fence F195 (line 5381)." {
    include strSTR0801_AnalyzerBranchChoreNetanalyzersAllMerged strSTR0801_PrerequisitesSections82And8 strSTR0801_Phase1RequirementsTheWholeCatalog strSTR0801_Phase2InterfacesAndCodeStubs strSTR0801_Phase3TDDUnitTestsWith
    autolayout tb 100 200
}
custom "STR-08-01-part2" "Phase order (activity), part 2 of 2" "Activity diagram converted from fence F195 (line 5381)." {
    include strSTR0801_Phase3TDDUnitTestsWith strSTR0801_Phase4ImplementationACodingAgent strSTR0801_IntegrationLiveChecksPhaseWideHV
    autolayout tb 100 200
}
custom "STR-08-02" "Bounded contexts in merge order, part 1 of 5" "Bounded-context graph converted from fence F196 (line 5412)." {
    include strBcStorageSchema strBcSharedComponents strBcSessionTurnLogging strBcTurnReviews
    autolayout tb 100 100
}
custom "STR-08-02-part2" "Bounded contexts in merge order, part 2 of 5" "Bounded-context graph converted from fence F196 (line 5412)." {
    include strBcSharedComponents strBcContextPackages strBcResponsesAndTrust strBcContextGraph strBcReplAndAgents
    autolayout lr 200 50
}
custom "STR-08-02-part3" "Bounded contexts in merge order, part 3 of 5" "Bounded-context graph converted from fence F196 (line 5412)." {
    include strBcPluginCorePowerShell strBcHostPluginAdoption strBcBuildTooling strBcDocumentation
    autolayout tb 100 100
}
custom "STR-08-02-part4" "Bounded contexts in merge order, part 4 of 5" "Bounded-context graph converted from fence F196 (line 5412)." {
    include strBcSessionTurnLogging strBcDocumentation strBcTurnReviews strBcContextPackages strBcResponsesAndTrust
    autolayout lr 200 50
}
custom "STR-08-02-part5" "Bounded contexts in merge order, part 5 of 5" "Bounded-context graph converted from fence F196 (line 5412)." {
    include strBcContextGraph strBcDocumentation strBcReplAndAgents
    autolayout lr 250 150
}
custom "RunPlanPhases" "Process RunPlanPhases: run the four phases (activity), part 1 of 3" "Activity diagram converted from fence F197 (line 5462)." {
    include strRunPlanPhases_MergedPlanApproved strRunPlanPhases_AnalyzerBranchMergedFirstSection8 strRunPlanPhases_RunThePrerequisitesEachUnderIts strRunPlanPhases_ForEachOfPhases12
    autolayout tb 100 200
}
custom "RunPlanPhases-part2" "Process RunPlanPhases: run the four phases (activity), part 2 of 3" "Activity diagram converted from fence F197 (line 5462)." {
    include strRunPlanPhases_ForEachOfPhases12 strRunPlanPhases_StopWaitForTheOperator strRunPlanPhases_Phase4WorkPackagesUntilEvery strRunPlanPhases_IntegrationGatesPluginSessionLogIntegrationLiveChecksOn strRunPlanPhases_PhaseWideHVAndOperatorApproval
    autolayout tb 100 200
}
custom "RunPlanPhases-part3" "Process RunPlanPhases: run the four phases (activity), part 3 of 3" "Activity diagram converted from fence F197 (line 5462)." {
    include strRunPlanPhases_PhaseWideHVAndOperatorApproval strRunPlanPhases_PlanComplete strRunPlanPhases_StopWaitForTheOperator
    autolayout lr 250 150
}
custom "RunOnePlanPhase" "Nested activity: RunOnePlanPhase (loop body of RunPlanPhases) (activity), part 1 of 2" "Activity diagram converted from fence F198 (line 5477)." {
    include strRunOnePlanPhase_NextPhaseRequirementsInterfacesAndStubs strRunOnePlanPhase_DoThePhaseSWorkFor strRunOnePlanPhase_PhaseGatesGreenWithZeroFailures
    autolayout tb 100 200
}
custom "RunOnePlanPhase-part2" "Nested activity: RunOnePlanPhase (loop body of RunPlanPhases) (activity), part 2 of 2" "Activity diagram converted from fence F198 (line 5477)." {
    include strRunOnePlanPhase_PhaseGatesGreenWithZeroFailures strRunOnePlanPhase_FixWithinThePhase strRunOnePlanPhase_PhaseHVAndOperatorApprovalNested strRunOnePlanPhase_ReturnPhaseApprovedContinueWithThe strRunOnePlanPhase_ReturnApprovalWithheld
    autolayout tb 100 100
}
custom "ValidateAndApprove" "Nested activity: ValidateAndApprove (used by RunOnePlanPhase and RunPlanPhases) (activity), part 1 of 3" "Activity diagram converted from fence F199 (line 5490)." {
    include strValidateAndApprove_ValidationRequested strValidateAndApprove_LaunchTheHVCodexGpt6 strValidateAndApprove_AGREEBothScoresAtLeast98
    autolayout tb 100 200
}
custom "ValidateAndApprove-part2" "Nested activity: ValidateAndApprove (used by RunOnePlanPhase and RunPlanPhases) (activity), part 2 of 3" "Activity diagram converted from fence F199 (line 5490)." {
    include strValidateAndApprove_AGREEBothScoresAtLeast98 strValidateAndApprove_ShowEveryFAILAndUNKNOWNTo strValidateAndApprove_OperatorApproves strValidateAndApprove_LaunchTheHVCodexGpt6
    autolayout tb 100 100
}
custom "ValidateAndApprove-part3" "Nested activity: ValidateAndApprove (used by RunOnePlanPhase and RunPlanPhases) (activity), part 3 of 3" "Activity diagram converted from fence F199 (line 5490)." {
    include strValidateAndApprove_OperatorApproves strValidateAndApprove_ReturnApproved strValidateAndApprove_ReturnApprovalWithheld
    autolayout lr 250 150
}
custom "RunPhase4WorkPackages" "Nested activity: RunPhase4WorkPackages (loop of RunPlanPhases) (activity)" "Activity diagram converted from fence F200 (line 5505)." {
    include strRunPhase4WorkPackages_Phase4Starts strRunPhase4WorkPackages_LaunchACodingAgentPerReady strRunPhase4WorkPackages_MergeEachReadyContextInThe strRunPhase4WorkPackages_EveryContextMergedInTheRollout strRunPhase4WorkPackages_ReturnEveryContextMerged
    autolayout tb 100 200
}
custom "RunDomainCodingAgent" "Process RunDomainCodingAgent: one Phase 4 work package (activity), part 1 of 2" "Activity diagram converted from fence F201 (line 5518)." {
    include strRunDomainCodingAgent_ContextTODOReadyItsDependenciesMerged strRunDomainCodingAgent_BranchFromTheIntegrationHeadPhase strRunDomainCodingAgent_BriefTheCodingAgentFilesStubs strRunDomainCodingAgent_ImplementUntilTheContextSTests
    autolayout tb 100 200
}
custom "RunDomainCodingAgent-part2" "Process RunDomainCodingAgent: one Phase 4 work package (activity), part 2 of 2" "Activity diagram converted from fence F201 (line 5518)." {
    include strRunDomainCodingAgent_ImplementUntilTheContextSTests strRunDomainCodingAgent_StopRaiseAPhase2Amendment strRunDomainCodingAgent_ReadyToMergeInTheRollout
    autolayout lr 250 150
}
custom "ImplementBoundedContext" "Nested activity: ImplementBoundedContext (loop of RunDomainCodingAgent) (activity)" "Activity diagram converted from fence F202 (line 5529)." {
    include strImplementBoundedContext_ImplementationStarts strImplementBoundedContext_OneImplementationPassNestedImplementationPass strImplementBoundedContext_ReturnChangeOutsideTheContextS strImplementBoundedContext_ReturnAGREE
    autolayout tb 100 100
}
custom "ImplementationPass" "Nested activity: ImplementationPass (loop body of ImplementBoundedContext) (activity), part 1 of 4" "Activity diagram converted from fence F203 (line 5539)." {
    include strImplementationPass_PassStarts strImplementationPass_ReplaceStubsWithTheRealImplementation strImplementationPass_ChangeNeededOutsideTheContextS
    autolayout tb 100 200
}
custom "ImplementationPass-part2" "Nested activity: ImplementationPass (loop body of ImplementBoundedContext) (activity), part 2 of 4" "Activity diagram converted from fence F203 (line 5539)." {
    include strImplementationPass_ChangeNeededOutsideTheContextS strImplementationPass_ReturnChangeOutsideTheContextS strImplementationPass_ContextSPhase3TestsGreen strImplementationPass_ReturnNotYetPassing strImplementationPass_AddTheContextSIntegrationTests
    autolayout tb 100 100
}
custom "ImplementationPass-part3" "Nested activity: ImplementationPass (loop body of ImplementBoundedContext) (activity), part 3 of 4" "Activity diagram converted from fence F203 (line 5539)." {
    include strImplementationPass_AddTheContextSIntegrationTests strImplementationPass_ContextGatesGreenZeroFailuresAnd strImplementationPass_ReturnNotYetPassing strImplementationPass_ContextHVCodexGpt6Astra strImplementationPass_AGREEAt9898EveryClaim
    autolayout tb 100 200
}
custom "ImplementationPass-part4" "Nested activity: ImplementationPass (loop body of ImplementBoundedContext) (activity), part 4 of 4" "Activity diagram converted from fence F203 (line 5539)." {
    include strImplementationPass_AGREEAt9898EveryClaim strImplementationPass_ReturnNotYetPassing strImplementationPass_ReturnAGREE
    autolayout lr 250 150
}
custom "STR-06-01" "Packages and their build inputs (section 6.3) (entities), part 1 of 3" "Entity diagram converted from fence F99 (line 2860)." {
    include entSessionLogTurn strEntAgentContextPackage strEntAgentContextPackageItem
    exclude "relationship.tag!=Planned"
    autolayout tb 200 300
}
custom "STR-06-01-part2" "Packages and their build inputs (section 6.3) (entities), part 2 of 3" "Entity diagram converted from fence F99 (line 2860)." {
    include strEntAgentContextPackage strEntAgentContextPackageFocusRef strEntAgentContextPackageInput
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-06-01-part3" "Packages and their build inputs (section 6.3) (entities), part 3 of 3" "Entity diagram converted from fence F99 (line 2860)." {
    include strEntAgentContextPackage strEntAgentContextPackageInputLink strEntAgentContextRelationship
    exclude "relationship.tag!=Planned"
    autolayout tb 200 300
}
custom "STR-06-02" "Package omissions and findings (section 6.3) (entities), part 1 of 2" "Entity diagram converted from fence F100 (line 2872)." {
    include strEntAgentContextPackage strEntAgentContextOmission strEntAgentContextFinding
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-06-02-part2" "Package omissions and findings (section 6.3) (entities), part 2 of 2" "Entity diagram converted from fence F100 (line 2872)." {
    include strEntAgentContextRelationship strEntAgentContextFinding
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-06-03" "Responses and evidence (section 6.3) (entities), part 1 of 2" "Entity diagram converted from fence F101 (line 2881)." {
    include entSessionLogTurn strEntAgentResponse strEntAgentContextPackage
    exclude "relationship.tag!=Planned"
    autolayout tb 200 300
}
custom "STR-06-03-part2" "Responses and evidence (section 6.3) (entities), part 2 of 2" "Entity diagram converted from fence F101 (line 2881)." {
    include strEntAgentResponse strEntAgentResponseClaim strEntAgentResponseEvidence
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-06-04" "Grades (section 6.3) (entities), part 1 of 2" "Entity diagram converted from fence F102 (line 2891)." {
    include strEntAgentResponse strEntAgentResponseGrade strEntAgentContextHvRequest
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-06-04-part2" "Grades (section 6.3) (entities), part 2 of 2" "Entity diagram converted from fence F102 (line 2891)." {
    include entSessionLogTurn strEntAgentResponseGrade strEntAgentResponseGradeFinding
    exclude "relationship.tag!=Planned"
    autolayout tb 200 300
}
custom "STR-06-05" "HV requests (section 6.3) (entities), part 1 of 2" "Entity diagram converted from fence F103 (line 2901)." {
    include entSessionLog strEntAgentContextHvRequest strEntAgentContextHvRequestItem
    exclude "relationship.tag!=Planned"
    autolayout tb 200 300
}
custom "STR-06-05-part2" "HV requests (section 6.3) (entities), part 2 of 2" "Entity diagram converted from fence F103 (line 2901)." {
    include strEntAgentResponse strEntAgentContextHvRequestItem
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-06-06" "Relationships and the GraphRAG projection (section 6.3) (entities), part 1 of 2" "Entity diagram converted from fence F104 (line 2910)." {
    include strEntAgentContextRelationship strEntAgentContextPackage strEntAgentResponse
    exclude "relationship.tag!=Planned"
    autolayout tb 200 300
}
custom "STR-06-06-part2" "Relationships and the GraphRAG projection (section 6.3) (entities), part 2 of 2" "Entity diagram converted from fence F104 (line 2910)." {
    include strEntAgentResponse strEntAgentContextProjection
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-01" "A.1 References between the groups (entities), part 1 of 9" "Entity diagram converted from fence F204 (line 5645)." {
    include entSessionLog strEntAgentContextHvRequest
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-01-part2" "A.1 References between the groups (entities), part 2 of 9" "Entity diagram converted from fence F204 (line 5645)." {
    include entSessionLogTurn strEntAgentContextPackage strEntAgentResponse
    exclude "relationship.tag!=Planned"
    autolayout tb 200 300
}
custom "STR-A-01-part3" "A.1 References between the groups (entities), part 3 of 9" "Entity diagram converted from fence F204 (line 5645)." {
    include entSessionLogTurn strEntAgentResponseGrade entTodoItem
    exclude "relationship.tag!=Planned"
    autolayout tb 200 300
}
custom "STR-A-01-part4" "A.1 References between the groups (entities), part 4 of 9" "Entity diagram converted from fence F204 (line 5645)." {
    include strEntAgentContextPackageItem entRequirement entSessionLogTurn
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-01-part5" "A.1 References between the groups (entities), part 5 of 9" "Entity diagram converted from fence F204 (line 5645)." {
    include strEntAgentContextPackageItem entTodoItem strEntAgentResponse
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-01-part6" "A.1 References between the groups (entities), part 6 of 9" "Entity diagram converted from fence F204 (line 5645)." {
    include strEntAgentContextFinding strEntAgentContextRelationship strEntAgentContextPackageInputLink
    exclude "relationship.tag!=Planned"
    autolayout tb 200 300
}
custom "STR-A-01-part7" "A.1 References between the groups (entities), part 7 of 9" "Entity diagram converted from fence F204 (line 5645)." {
    include strEntAgentContextRelationship strEntAgentContextPackage strEntAgentResponse
    exclude "relationship.tag!=Planned"
    autolayout tb 200 300
}
custom "STR-A-01-part8" "A.1 References between the groups (entities), part 8 of 9" "Entity diagram converted from fence F204 (line 5645)." {
    include strEntAgentContextRelationship entRequirement entSessionLogTurn
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-01-part9" "A.1 References between the groups (entities), part 9 of 9" "Entity diagram converted from fence F204 (line 5645)." {
    include strEntAgentContextRelationship entTodoItem
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-02" "A.2 Sessions and turns (Phases A and B) (entities)" "Entity diagram converted from fence F205 (line 5676)." {
    include entSessionLog entSessionLogTurn strEntSessionLogTurnReviewReason
    autolayout tb 200 300
    properties {
        "column.SessionLog.Id" "long PK"
        "column.SessionLog.WorkspaceId" "string UK"
        "column.SessionLog.SourceType" "string UK"
        "column.SessionLog.SessionId" "string UK"
        "column.SessionLogTurn.Id" "long PK"
        "column.SessionLogTurn.WorkspaceId" "string UK"
        "column.SessionLogTurn.SessionLogId" "long FK,UK"
        "column.SessionLogTurn.RequestId" "string UK"
        "column.SessionLogTurn.IngressNonce" "string UK"
        "column.SessionLogTurn.TodoId" "string"
        "column.SessionLogTurn.RequestClass" "string default Unclassified"
        "column.SessionLogTurn.AgentReviewStatus" "string default Unstarted"
        "column.SessionLogTurn.HostileReviewStatus" "string default Unstarted"
    }
}
custom "STR-A-03" "A.3 Turn records (entities), part 1 of 3" "Entity diagram converted from fence F206 (line 5713)." {
    include entSessionLogTurn entSessionLogAction entSessionLogProcessingDialog
    autolayout lr 300 200
    properties {
        "column.SessionLogAction.Id" "long PK"
        "column.SessionLogAction.SessionLogTurnId" "long FK"
        "column.SessionLogProcessingDialog.Id" "long PK"
        "column.SessionLogProcessingDialog.SessionLogTurnId" "long FK"
    }
}
custom "STR-A-03-part2" "A.3 Turn records (entities), part 2 of 3" "Entity diagram converted from fence F206 (line 5713)." {
    include entSessionLogTurn entSessionLogTurnStringList entSessionLogCommit
    autolayout lr 300 200
    properties {
        "column.SessionLogTurnStringList.Id" "long PK"
        "column.SessionLogTurnStringList.SessionLogTurnId" "long FK"
        "column.SessionLogCommit.Id" "long PK"
        "column.SessionLogCommit.SessionLogTurnId" "long FK"
    }
}
custom "STR-A-03-part3" "A.3 Turn records (entities), part 3 of 3" "Entity diagram converted from fence F206 (line 5713)." {
    include entSessionLogTurn entSessionLogTurnContext
    autolayout lr 300 200
    properties {
        "column.SessionLogTurnContext.Id" "long PK"
        "column.SessionLogTurnContext.SessionLogTurnId" "long FK"
    }
}
custom "STR-A-04" "A.4 Requirements, TODOs, and use cases (existing entities the plan references) (entities), part 1 of 3" "Entity diagram converted from fence F207 (line 5747)." {
    include entTodoItem entTodoRequirementLink entRequirement
    autolayout lr 300 200
    properties {
        "column.TodoItem.WorkspaceId" "string PK"
        "column.TodoItem.Id" "string PK"
        "column.TodoRequirementLink.WorkspaceId" "string PK,FK"
        "column.TodoRequirementLink.TodoId" "string PK,FK"
        "column.TodoRequirementLink.RequirementKind" "string PK,FK"
        "column.TodoRequirementLink.RequirementId" "string PK,FK"
        "column.Requirement.WorkspaceId" "string PK"
        "column.Requirement.Kind" "string PK"
        "column.Requirement.Id" "string PK"
    }
}
custom "STR-A-04-part2" "A.4 Requirements, TODOs, and use cases (existing entities the plan references) (entities), part 2 of 3" "Entity diagram converted from fence F207 (line 5747)." {
    include entRequirement entRequirementAcceptanceCriterion entRequirementTraceabilityLink
    autolayout lr 300 200
    properties {
        "column.Requirement.WorkspaceId" "string PK"
        "column.Requirement.Kind" "string PK"
        "column.Requirement.Id" "string PK"
        "column.RequirementAcceptanceCriterion.Id" "long PK"
        "column.RequirementAcceptanceCriterion.WorkspaceId" "string FK"
        "column.RequirementAcceptanceCriterion.RequirementKind" "string FK"
        "column.RequirementAcceptanceCriterion.RequirementId" "string FK"
        "column.RequirementTraceabilityLink.WorkspaceId" "string PK,FK"
        "column.RequirementTraceabilityLink.SourceKind" "string FK"
        "column.RequirementTraceabilityLink.FrId" "string PK,FK"
        "column.RequirementTraceabilityLink.TargetKind" "string PK,FK"
        "column.RequirementTraceabilityLink.TargetId" "string PK,FK"
    }
}
custom "STR-A-04-part3" "A.4 Requirements, TODOs, and use cases (existing entities the plan references) (entities), part 3 of 3" "Entity diagram converted from fence F207 (line 5747)." {
    include entUseCase entUseCaseFrLink entRequirement
    autolayout lr 300 200
    properties {
        "column.Requirement.WorkspaceId" "string PK"
        "column.Requirement.Kind" "string PK"
        "column.Requirement.Id" "string PK"
        "column.UseCase.UseCaseId" "long PK"
        "column.UseCase.WorkspaceId" "string FK"
        "column.UseCaseFrLink.LinkId" "long PK"
        "column.UseCaseFrLink.WorkspaceId" "string FK,UK"
        "column.UseCaseFrLink.UseCaseId" "long FK,UK"
        "column.UseCaseFrLink.FrKind" "string FK"
        "column.UseCaseFrLink.FrId" "string FK,UK"
    }
}
custom "STR-A-05" "A.5 Packages and their build inputs (Phase C) (entities), part 1 of 2" "Entity diagram converted from fence F208 (line 5809)." {
    include strEntAgentContextPackage strEntAgentContextPackageItem strEntAgentContextPackageFocusRef
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-05-part2" "A.5 Packages and their build inputs (Phase C) (entities), part 2 of 2" "Entity diagram converted from fence F208 (line 5809)." {
    include strEntAgentContextPackage strEntAgentContextPackageInput strEntAgentContextPackageInputLink
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-06" "A.6 Package omissions and findings (Phase C) (entities)" "Entity diagram converted from fence F209 (line 5859)." {
    include strEntAgentContextPackage strEntAgentContextOmission strEntAgentContextFinding
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-07" "A.7 Responses and evidence (Phase C) (entities), part 1 of 2" "Entity diagram converted from fence F210 (line 5887)." {
    include strEntAgentContextPackage strEntAgentResponse strEntAgentResponseClaim
    exclude "relationship.tag!=Planned"
    autolayout tb 200 300
}
custom "STR-A-07-part2" "A.7 Responses and evidence (Phase C) (entities), part 2 of 2" "Entity diagram converted from fence F210 (line 5887)." {
    include strEntAgentResponse strEntAgentResponseEvidence
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-08" "A.8 Grades (Phase C) (entities), part 1 of 2" "Entity diagram converted from fence F211 (line 5926)." {
    include strEntAgentResponse strEntAgentResponseGrade strEntAgentContextHvRequest
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-08-part2" "A.8 Grades (Phase C) (entities), part 2 of 2" "Entity diagram converted from fence F211 (line 5926)." {
    include strEntAgentResponseGrade strEntAgentResponseGradeFinding
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-09" "A.9 HV requests (Phase C) (entities)" "Entity diagram converted from fence F212 (line 5956)." {
    include strEntAgentContextHvRequest strEntAgentContextHvRequestItem strEntAgentResponse
    exclude "relationship.tag!=Planned"
    autolayout lr 300 200
}
custom "STR-A-10" "A.10 Relationships, the GraphRAG projection, and retrieval stores (Phase C) (entities), part 1 of 3" "Entity diagram converted from fence F213 (line 5985)." {
    include strEntAgentResponse strEntAgentContextProjection entContextDocument
    exclude "relationship.tag!=Planned"
    autolayout tb 200 300
    properties {
        "column.ContextDocument.Id" "string PK"
    }
}
custom "STR-A-10-part2" "A.10 Relationships, the GraphRAG projection, and retrieval stores (Phase C) (entities), part 2 of 3" "Entity diagram converted from fence F213 (line 5985)." {
    include entContextDocument entContextChunk strEntAgentContextRelationship
    autolayout lr 300 200
    properties {
        "column.ContextDocument.Id" "string PK"
        "column.ContextChunk.Id" "string PK"
        "column.ContextChunk.DocumentId" "string FK"
    }
}
custom "STR-A-10-part3" "A.10 Relationships, the GraphRAG projection, and retrieval stores (Phase C) (entities), part 3 of 3" "Entity diagram converted from fence F213 (line 5985)." {
    include entGraphEntity entGraphRelationship
    autolayout lr 300 200
    properties {
        "column.GraphEntity.Id" "string PK"
        "column.GraphRelationship.Id" "string PK"
        "column.GraphRelationship.SourceEntityId" "string FK"
        "column.GraphRelationship.TargetEntityId" "string FK"
    }
}
component qbrain.plugins "STR-02-01" "Structure diagram converted from fence F1 (line 26)." {
    title "System context: hosts, plugins, clients, server and store, part 1 of 11"
    include agent qbrain.plugins.pluginPs qbrain.plugins.pluginHosts
    exclude "relationship.tag!=SystemContext"
    autolayout tb 250 100
}
component qbrain.server "STR-02-01-part2" "Structure diagram converted from fence F1 (line 26)." {
    title "System context: hosts, plugins, clients, server and store, part 2 of 11"
    include agent qbrain.server.mcpTools
    exclude "relationship.tag!=SystemContext"
    autolayout tb 250 100
}
component qbrain.plugins "STR-02-01-part3" "Structure diagram converted from fence F1 (line 26)." {
    title "System context: hosts, plugins, clients, server and store, part 3 of 11"
    include qbrain.plugins.pluginPs qbrain.repl
    exclude "relationship.tag!=SystemContext"
    autolayout tb 250 100
}
component qbrain.libs "STR-02-01-part4" "Structure diagram converted from fence F1 (line 26)." {
    title "System context: hosts, plugins, clients, server and store, part 4 of 11"
    include qbrain.repl qbrain.libs.client qbrain.libs.mcpAgent
    exclude "relationship.tag!=SystemContext"
    autolayout tb 250 100
}
component qbrain.server "STR-02-01-part5" "Structure diagram converted from fence F1 (line 26)." {
    title "System context: hosts, plugins, clients, server and store, part 5 of 11"
    include qbrain.libs.client qbrain.server.api
    exclude "relationship.tag!=SystemContext"
    autolayout tb 250 100
}
component qbrain.server "STR-02-01-part6" "Structure diagram converted from fence F1 (line 26)." {
    title "System context: hosts, plugins, clients, server and store, part 6 of 11"
    include qbrain.server.api qbrain.server.cqrsLib qbrain.server.mcpTools
    exclude "relationship.tag!=SystemContext"
    autolayout tb 250 100
}
component qbrain.server "STR-02-01-part7" "Structure diagram converted from fence F1 (line 26)." {
    title "System context: hosts, plugins, clients, server and store, part 7 of 11"
    include qbrain.server.cqrsLib qbrain.server.strSessionLogHandlers qbrain.server.strAgentContextHandlers
    exclude "relationship.tag!=SystemContext"
    autolayout tb 250 100
}
component qbrain.server "STR-02-01-part8" "Structure diagram converted from fence F1 (line 26)." {
    title "System context: hosts, plugins, clients, server and store, part 8 of 11"
    include qbrain.server.strSessionLogHandlers qbrain.server.strCompletionGate qbrain.server.strSharedComponents
    exclude "relationship.tag!=SystemContext"
    autolayout tb 250 100
}
component qbrain.server "STR-02-01-part9" "Structure diagram converted from fence F1 (line 26)." {
    title "System context: hosts, plugins, clients, server and store, part 9 of 11"
    include qbrain.server.strAgentContextHandlers qbrain.server.strSharedComponents qbrain.server.graphrag
    exclude "relationship.tag!=SystemContext"
    autolayout tb 250 100
}
component qbrain.server "STR-02-01-part10" "Structure diagram converted from fence F1 (line 26)." {
    title "System context: hosts, plugins, clients, server and store, part 10 of 11"
    include qbrain.server.strSharedComponents qbrain.db
    exclude "relationship.tag!=SystemContext"
    autolayout tb 250 100
}
component qbrain.server "STR-02-01-part11" "Structure diagram converted from fence F1 (line 26)." {
    title "System context: hosts, plugins, clients, server and store, part 11 of 11"
    include qbrain.server.strSessionLogHandlers qbrain.db qbrain.server.strAgentContextHandlers
    exclude "relationship.tag!=SystemContext"
    autolayout tb 250 100
}
container qbrain "STR-07-01" "Use-case diagram converted from fence F179 (line 5073)." {
    title "Use cases: overview (use cases), part 1 of 12"
    include developer strUcLogEachUserRequestAsExactly strUcConfigureReviewPassThresholds agent
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-01-part2" "Use-case diagram converted from fence F179 (line 5073)." {
    title "Use cases: overview (use cases), part 2 of 12"
    include developer strUcRunAStatelessOrPilotInvocation agent
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.plugins "STR-07-01-part3" "Use-case diagram converted from fence F179 (line 5073)." {
    title "Use cases: overview (use cases), part 3 of 12"
    include qbrain.plugins.pluginPs strUcLogEachUserRequestAsExactly strUcCloseEveryRequestTurnBeforeStopping strHostModel
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.plugins "STR-07-01-part4" "Use-case diagram converted from fence F179 (line 5073)." {
    title "Use cases: overview (use cases), part 4 of 12"
    include agent strUcCloseEveryRequestTurnBeforeStopping strUcBuildAContextPackageForA qbrain.plugins.pluginPs
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.plugins "STR-07-01-part5" "Use-case diagram converted from fence F179 (line 5073)." {
    title "Use cases: overview (use cases), part 5 of 12"
    include qbrain.plugins.pluginPs strUcRunAStatelessOrPilotInvocation strHostModel
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-01-part6" "Use-case diagram converted from fence F179 (line 5073)." {
    title "Use cases: overview (use cases), part 6 of 12"
    include strHostModel strUcPersistSessionLogWritesWithTruthful strUcClassifyARequest
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-01-part7" "Use-case diagram converted from fence F179 (line 5073)." {
    title "Use cases: overview (use cases), part 7 of 12"
    include strHostModel strUcCompleteOrFailATurnUnder strUcRecordAnAgentSelfReview qbrain.server
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-01-part8" "Use-case diagram converted from fence F179 (line 5073)." {
    title "Use cases: overview (use cases), part 8 of 12"
    include strHostModel strUcSubmitAndGradeAnAgentResponse llm
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-01-part9" "Use-case diagram converted from fence F179 (line 5073)." {
    title "Use cases: overview (use cases), part 9 of 12"
    include qbrain.server strUcProjectResponsesIntoGraphRAG
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-01-part10" "Use-case diagram converted from fence F179 (line 5073)." {
    title "Use cases: overview (use cases), part 10 of 12"
    include llm strUcRecordAHostileValidationReviewWith
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.libs "STR-07-01-part11" "Use-case diagram converted from fence F179 (line 5073)." {
    title "Use cases: overview (use cases), part 11 of 12"
    include qbrain.libs.client strUcReadReviewScoresLosslessly strUcRetrieveTrustedPriorContext
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.libs "STR-07-01-part12" "Use-case diagram converted from fence F179 (line 5073)." {
    title "Use cases: overview (use cases), part 12 of 12"
    include qbrain.libs.client strUcMaintainRelationshipsStalenessAndSupersession
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-1" "Use-case diagram converted from fence F180 (line 5125)." {
    title "Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 1 of 8"
    include developer strUcLogEachUserRequestAsExactly agent
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.plugins "UC-1-part2" "Use-case diagram converted from fence F180 (line 5125)." {
    title "Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 2 of 8"
    include qbrain.plugins.pluginPs strUcLogEachUserRequestAsExactly strHostModel
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-1-part3" "Use-case diagram converted from fence F180 (line 5125)." {
    title "Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 3 of 8"
    include qbrain.server strUcLogEachUserRequestAsExactly strUcMintAtIngressMintSessionTurnCommand
    exclude "relationship.tag!=UseCase"
    autolayout tb 200 100
}
container qbrain "UC-1-part4" "Use-case diagram converted from fence F180 (line 5125)." {
    title "Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 4 of 8"
    include strUcLogEachUserRequestAsExactly strUcBindEachVerbBindVerbToTurn strUcRetryReplayOrKilledHookOne
    exclude "relationship.tag!=UseCase"
    autolayout tb 200 100
}
container qbrain "UC-1-part5" "Use-case diagram converted from fence F180 (line 5125)." {
    title "Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 5 of 8"
    include strUcQueuedMintedAtEnqueue strUcLogEachUserRequestAsExactly strUcWithdrawnBeforeDeliveryFailIt
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-1-part6" "Use-case diagram converted from fence F180 (line 5125)." {
    title "Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 6 of 8"
    include strUcBackgroundOrSubagentEventSkipped strUcLogEachUserRequestAsExactly strUcTwoRequestsOpenRequestAmbiguous
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-1-part7" "Use-case diagram converted from fence F180 (line 5125)." {
    title "Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 7 of 8"
    include strUcNoArrivalMechanismUnsupported strUcLogEachUserRequestAsExactly strUcOutagePendingIdReboundOnDrain
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-1-part8" "Use-case diagram converted from fence F180 (line 5125)." {
    title "Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 8 of 8"
    include strUcResumeOrCompactionMCPSessionKept strUcLogEachUserRequestAsExactly strUcContendedLockOrFinalRefusalNo
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.plugins "UC-2" "Use-case diagram converted from fence F181 (line 5148)." {
    title "Use case: Persist session-log writes with truthful receipts (plan UC-2) (use cases), part 1 of 4"
    include strHostModel strUcPersistSessionLogWritesWithTruthful qbrain.plugins.pluginPs
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-2-part2" "Use-case diagram converted from fence F181 (line 5148)." {
    title "Use case: Persist session-log writes with truthful receipts (plan UC-2) (use cases), part 2 of 4"
    include qbrain.server strUcPersistSessionLogWritesWithTruthful strUcPerTurnOutcomePersistTurnWriteWithReceipt
    exclude "relationship.tag!=UseCase"
    autolayout tb 200 100
}
container qbrain "UC-2-part3" "Use-case diagram converted from fence F181 (line 5148)." {
    title "Use case: Persist session-log writes with truthful receipts (plan UC-2) (use cases), part 3 of 4"
    include strUcTerminalTargetSkippedTerminalExit1 strUcPersistSessionLogWritesWithTruthful strUcLegacyServerOutcomeVerifiedFalse
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-2-part4" "Use-case diagram converted from fence F181 (line 5148)." {
    title "Use case: Persist session-log writes with truthful receipts (plan UC-2) (use cases), part 4 of 4"
    include strUcRetryableAnswerOrOutageQueuedExit strUcPersistSessionLogWritesWithTruthful strUcFinalDrainAnswersQuarantinedAt5
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-3" "Use-case diagram converted from fence F182 (line 5164)." {
    title "Use case: Close every request turn before stopping (plan UC-3) (use cases), part 1 of 5"
    include strHostModel strUcCloseEveryRequestTurnBeforeStopping agent
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-3-part2" "Use-case diagram converted from fence F182 (line 5164)." {
    title "Use case: Close every request turn before stopping (plan UC-3) (use cases), part 2 of 5"
    include strUcCloseEveryRequestTurnBeforeStopping strUcStopGateEvaluateStopGateGetCloseRemedy strUcEvidenceTestModelEvidence
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-3-part3" "Use-case diagram converted from fence F182 (line 5164)." {
    title "Use case: Close every request turn before stopping (plan UC-3) (use cases), part 3 of 5"
    include strUcOpenTurnBlockWithRemedy strUcCloseEveryRequestTurnBeforeStopping strUcCloseRefusedByTheGateBlock
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-3-part4" "Use-case diagram converted from fence F182 (line 5164)." {
    title "Use case: Close every request turn before stopping (plan UC-3) (use cases), part 4 of 5"
    include strUcUnsupportedSessionCapturedWarn strUcCloseEveryRequestTurnBeforeStopping strUcCloseHeldByAnOutageWarn
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-3-part5" "Use-case diagram converted from fence F182 (line 5164)." {
    title "Use case: Close every request turn before stopping (plan UC-3) (use cases), part 5 of 5"
    include strUcNoTurnOpenedWarnNamingThe strUcCloseEveryRequestTurnBeforeStopping strUcQuarantinedRecordOfAnOpenTurn
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-4" "Use-case diagram converted from fence F183 (line 5182)." {
    title "Use case: Classify a request (plan UC-4) (use cases), part 1 of 3"
    include strHostModel strUcClassifyARequest strUcChoreFlipsAnUnstartedHostileReview
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-4-part2" "Use-case diagram converted from fence F183 (line 5182)." {
    title "Use case: Classify a request (plan UC-4) (use cases), part 2 of 3"
    include strUcClassChangesAwayUnnecessaryReturnsTo strUcClassifyARequest strUcInvalidValueRequestClassInvalid
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-4-part3" "Use-case diagram converted from fence F183 (line 5182)." {
    title "Use case: Classify a request (plan UC-4) (use cases), part 3 of 3"
    include strUcClassOnlyAdditiveWriteOnA strUcClassifyARequest strUcFlipRacingAReviewWriteOr
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-5" "Use-case diagram converted from fence F184 (line 5196)." {
    title "Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 1 of 8"
    include strHostModel strUcCompleteOrFailATurnUnder qbrain.server
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.server "UC-5-part2" "Use-case diagram converted from fence F184 (line 5196)." {
    title "Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 2 of 8"
    include qbrain.server.brain strUcCompleteOrFailATurnUnder qbrain.server.workspace
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.server "UC-5-part3" "Use-case diagram converted from fence F184 (line 5196)." {
    title "Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 3 of 8"
    include qbrain.server.federation strUcCompleteOrFailATurnUnder qbrain.server.sessionLog
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-5-part4" "Use-case diagram converted from fence F184 (line 5196)." {
    title "Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 4 of 8"
    include qbrain.qbagent strUcCompleteOrFailATurnUnder qbrain.repl
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-5-part5" "Use-case diagram converted from fence F184 (line 5196)." {
    title "Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 5 of 8"
    include strUcCompleteOrFailATurnUnder strUcAdvisoryPreCheckWarnsNeverRefuses strUcGateUnmet400WithMissingItems
    exclude "relationship.tag!=UseCase"
    autolayout tb 200 100
}
container qbrain "UC-5-part6" "Use-case diagram converted from fence F184 (line 5196)." {
    title "Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 6 of 8"
    include strUcFailRequiresClassOnly strUcCompleteOrFailATurnUnder strUcExemptOriginBypassesTheGate
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-5-part7" "Use-case diagram converted from fence F184 (line 5196)." {
    title "Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 7 of 8"
    include strUcCloseRacingAReviewResetSerialized strUcCompleteOrFailATurnUnder strUcStatusClosedOrUnknown400
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-5-part8" "Use-case diagram converted from fence F184 (line 5196)." {
    title "Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 8 of 8"
    include strUcImportRefusedByTheGateItems strUcCompleteOrFailATurnUnder
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-6" "Use-case diagram converted from fence F185 (line 5215)." {
    title "Use case: Record an agent self-review (plan UC-6) (use cases), part 1 of 5"
    include strHostModel strUcRecordAnAgentSelfReview qbrain.server
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-6-part2" "Use-case diagram converted from fence F185 (line 5215)." {
    title "Use case: Record an agent self-review (plan UC-6) (use cases), part 2 of 5"
    include strUcRecordAnAgentSelfReview strUcDerivePassOrFailAndStamp strUcConflictingSuppliedStatus422
    exclude "relationship.tag!=UseCase"
    autolayout tb 200 100
}
container qbrain "UC-6-part3" "Use-case diagram converted from fence F185 (line 5215)." {
    title "Use case: Record an agent self-review (plan UC-6) (use cases), part 3 of 5"
    include strUcInvalidFields400WithTheField strUcRecordAnAgentSelfReview strUcReviewRecordedAfterCompletion
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-6-part4" "Use-case diagram converted from fence F185 (line 5215)." {
    title "Use case: Record an agent self-review (plan UC-6) (use cases), part 4 of 5"
    include strUcUnsupportedHostTurnLoggingUnsupported strUcRecordAnAgentSelfReview strUcConcurrentWriteConflict409RetryableNot
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-6-part5" "Use-case diagram converted from fence F185 (line 5215)." {
    title "Use case: Record an agent self-review (plan UC-6) (use cases), part 5 of 5"
    include strUcResetOnACompletedOrFailed strUcRecordAnAgentSelfReview
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-7" "Use-case diagram converted from fence F186 (line 5232)." {
    title "Use case: Record a hostile validation review with receipts (plan UC-7) (use cases), part 1 of 5"
    include llm strUcRecordAHostileValidationReviewWith agent
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-7-part2" "Use-case diagram converted from fence F186 (line 5232)." {
    title "Use case: Record a hostile validation review with receipts (plan UC-7) (use cases), part 2 of 5"
    include strUcRecordAHostileValidationReviewWith strUcRunnerWritesRequestAndResponseJsonl strUcMapTheReceiptToAReview
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-7-part3" "Use-case diagram converted from fence F186 (line 5232)." {
    title "Use case: Record a hostile validation review with receipts (plan UC-7) (use cases), part 3 of 5"
    include strUcDISAGREEWithPassingScoresOtherWith strUcRecordAHostileValidationReviewWith strUcMissingReceiptPathsRejected
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-7-part4" "Use-case diagram converted from fence F186 (line 5232)." {
    title "Use case: Record a hostile validation review with receipts (plan UC-7) (use cases), part 4 of 5"
    include strUcWrongLiveModelOrEffortNot strUcRecordAHostileValidationReviewWith strUcBackfillByOriginalPair
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-7-part5" "Use-case diagram converted from fence F186 (line 5232)." {
    title "Use case: Record a hostile validation review with receipts (plan UC-7) (use cases), part 5 of 5"
    include strUcHvVerdictWithAnExplicitStatus400 strUcRecordAHostileValidationReviewWith
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-8" "Use-case diagram converted from fence F187 (line 5249)." {
    title "Use case: Configure review pass thresholds (plan UC-8) (use cases), part 1 of 2"
    include developer strUcConfigureReviewPassThresholds strUcStartupValidationOfValuesIn0
    exclude "relationship.tag!=UseCase"
    autolayout tb 200 100
}
container qbrain "UC-8-part2" "Use-case diagram converted from fence F187 (line 5249)." {
    title "Use case: Configure review pass thresholds (plan UC-8) (use cases), part 2 of 2"
    include strUcPerInstanceOverride strUcConfigureReviewPassThresholds strUcValueOutside01StartupRejected
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.libs "UC-9" "Use-case diagram converted from fence F188 (line 5261)." {
    title "Use case: Read review scores losslessly (plan UC-9) (use cases), part 1 of 2"
    include agent strUcReadReviewScoresLosslessly qbrain.libs.client
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "UC-9-part2" "Use-case diagram converted from fence F188 (line 5261)." {
    title "Use case: Read review scores losslessly (plan UC-9) (use cases), part 2 of 2"
    include strUcReadReviewScoresLosslessly strUcExactDecimalLexemeOnRESTMCP
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.plugins "STR-07-02" "Use-case diagram converted from fence F189 (line 5272)." {
    title "Use case: Build a context package for a request (use cases), part 1 of 5"
    include agent strUcBuildAContextPackageForA qbrain.plugins.pluginPs
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-02-part2" "Use-case diagram converted from fence F189 (line 5272)." {
    title "Use case: Build a context package for a request (use cases), part 2 of 5"
    include qbrain.server strUcBuildAContextPackageForA strUcRetrieveCandidatesRetrieveAgentContextCandidatesQuery
    exclude "relationship.tag!=UseCase"
    autolayout tb 200 100
}
container qbrain "STR-07-02-part3" "Use-case diagram converted from fence F189 (line 5272)." {
    title "Use case: Build a context package for a request (use cases), part 3 of 5"
    include strUcBuildAContextPackageForA strUcAssembleSecretsOmissionsFindingsBudgetRendering strUcMandatoryOverBudget422InvocationFails
    exclude "relationship.tag!=UseCase"
    autolayout tb 200 100
}
container qbrain "STR-07-02-part4" "Use-case diagram converted from fence F189 (line 5272)." {
    title "Use case: Build a context package for a request (use cases), part 4 of 5"
    include strUcRepeatedKeySamePackageIdempotentCreate strUcBuildAContextPackageForA strUcReplayProvesDeterminismReplayAgentContextPackageQuery
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-02-part5" "Use-case diagram converted from fence F189 (line 5272)." {
    title "Use case: Build a context package for a request (use cases), part 5 of 5"
    include strUcCommitOrProfileUnavailable409 strUcBuildAContextPackageForA strUcModeSwitchedToOff409Agent
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.plugins "STR-07-03" "Use-case diagram converted from fence F190 (line 5291)." {
    title "Use case: Submit and grade an agent response (use cases), part 1 of 7"
    include strHostModel strUcSubmitAndGradeAnAgentResponse qbrain.plugins.pluginPs
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-03-part2" "Use-case diagram converted from fence F190 (line 5291)." {
    title "Use case: Submit and grade an agent response (use cases), part 2 of 7"
    include llm strUcSubmitAndGradeAnAgentResponse qbrain.server
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-03-part3" "Use-case diagram converted from fence F190 (line 5291)." {
    title "Use case: Submit and grade an agent response (use cases), part 3 of 7"
    include strUcSubmitAndGradeAnAgentResponse strUcSubmitSubmitAgentResponseCommand strUcDeterministicGradeRunDeterministicGradeCommand
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-03-part4" "Use-case diagram converted from fence F190 (line 5291)." {
    title "Use case: Submit and grade an agent response (use cases), part 4 of 7"
    include strUcSubmitAndGradeAnAgentResponse strUcModelGradeRecordModelGradeCommand strUcTrustDecideAgentResponseTrustCommand
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-03-part5" "Use-case diagram converted from fence F190 (line 5291)." {
    title "Use case: Submit and grade an agent response (use cases), part 5 of 7"
    include strUcGradeFailsStaysGrading strUcSubmitAndGradeAnAgentResponse strUcUnconfiguredGrader403
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-03-part6" "Use-case diagram converted from fence F190 (line 5291)." {
    title "Use case: Submit and grade an agent response (use cases), part 6 of 7"
    include strUcOperatorRejectOfASupersededOne strUcSubmitAndGradeAnAgentResponse strUcDependencyChangedStaleNotTrusted
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-03-part7" "Use-case diagram converted from fence F190 (line 5291)." {
    title "Use case: Submit and grade an agent response (use cases), part 7 of 7"
    include strUcHVRequestRetriedStoredRequest strUcSubmitAndGradeAnAgentResponse strUcNoSubmissionNotAStopGate
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.libs "STR-07-04" "Use-case diagram converted from fence F191 (line 5313)." {
    title "Use case: Retrieve trusted prior context (use cases), part 1 of 3"
    include qbrain.server strUcRetrieveTrustedPriorContext qbrain.libs.client
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-04-part2" "Use-case diagram converted from fence F191 (line 5313)." {
    title "Use case: Retrieve trusted prior context (use cases), part 2 of 3"
    include strUcRetrieveTrustedPriorContext strUcReadOnlyStalenessCheckOfEach strUcGraphRAGUnavailableOrAskedForOther
    exclude "relationship.tag!=UseCase"
    autolayout tb 200 100
}
container qbrain "STR-07-04-part3" "Use-case diagram converted from fence F191 (line 5313)." {
    title "Use case: Retrieve trusted prior context (use cases), part 3 of 3"
    include strUcModeOffStillAnswersReadOnly strUcRetrieveTrustedPriorContext
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-05" "Use-case diagram converted from fence F192 (line 5326)." {
    title "Use case: Maintain relationships, staleness, and supersession (use cases), part 1 of 4"
    include strHostModel strUcMaintainRelationshipsStalenessAndSupersession developer
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-05-part2" "Use-case diagram converted from fence F192 (line 5326)." {
    title "Use case: Maintain relationships, staleness, and supersession (use cases), part 2 of 4"
    include qbrain.server strUcMaintainRelationshipsStalenessAndSupersession strUcCreateDeleteQueryLinksCreateAgentContextRelationshipCommandTo
    exclude "relationship.tag!=UseCase"
    autolayout tb 200 100
}
container qbrain "STR-07-05-part3" "Use-case diagram converted from fence F192 (line 5326)." {
    title "Use case: Maintain relationships, staleness, and supersession (use cases), part 3 of 4"
    include strUcMaintainRelationshipsStalenessAndSupersession strUcEvaluateStalenessAtTheHEADCommit strUcSupersedeAResponseSupersedeAgentResponseCommand
    exclude "relationship.tag!=UseCase"
    autolayout tb 200 100
}
container qbrain "STR-07-05-part4" "Use-case diagram converted from fence F192 (line 5326)." {
    title "Use case: Maintain relationships, staleness, and supersession (use cases), part 4 of 4"
    include strUcInvalidLinkCycleClientSupersedesLink strUcMaintainRelationshipsStalenessAndSupersession strUcSupersedeOfARejectedOrAlready
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-06" "Use-case diagram converted from fence F193 (line 5344)." {
    title "Use case: Run a stateless or pilot invocation (use cases), part 1 of 7"
    include developer strUcRunAStatelessOrPilotInvocation agent
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
component qbrain.plugins "STR-07-06-part2" "Use-case diagram converted from fence F193 (line 5344)." {
    title "Use case: Run a stateless or pilot invocation (use cases), part 2 of 7"
    include qbrain.plugins.pluginPs strUcRunAStatelessOrPilotInvocation strHostModel
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-06-part3" "Use-case diagram converted from fence F193 (line 5344)." {
    title "Use case: Run a stateless or pilot invocation (use cases), part 3 of 7"
    include strUcRunAStatelessOrPilotInvocation strUcBuildAContextPackageForA strUcSubmitAndGradeAnAgentResponse
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-06-part4" "Use-case diagram converted from fence F193 (line 5344)." {
    title "Use case: Run a stateless or pilot invocation (use cases), part 4 of 7"
    include strUcModeOffOr409DisabledStateful strUcRunAStatelessOrPilotInvocation strUcBuildFailsOrMintPendingRefused
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-06-part5" "Use-case diagram converted from fence F193 (line 5344)." {
    title "Use case: Run a stateless or pilot invocation (use cases), part 5 of 7"
    include strUcPilotReportReadableInOff strUcRunAStatelessOrPilotInvocation strUcBridgeHostHookSyncsTheTranscript
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-06-part6" "Use-case diagram converted from fence F193 (line 5344)." {
    title "Use case: Run a stateless or pilot invocation (use cases), part 6 of 7"
    include strUcAbsorbedMidTurnRunsAsPilot strUcRunAStatelessOrPilotInvocation strUcHostCannotRefuseAPromptRuns
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-06-part7" "Use-case diagram converted from fence F193 (line 5344)." {
    title "Use case: Run a stateless or pilot invocation (use cases), part 7 of 7"
    include strUcCanceledCloseHeldByAnOutage strUcRunAStatelessOrPilotInvocation
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-07" "Use-case diagram converted from fence F194 (line 5364)." {
    title "Use case: Project responses into GraphRAG (use cases), part 1 of 4"
    include qbrain.server strUcProjectResponsesIntoGraphRAG developer
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-07-part2" "Use-case diagram converted from fence F194 (line 5364)." {
    title "Use case: Project responses into GraphRAG (use cases), part 2 of 4"
    include strUcGraphRAGDisabledIngestSkippedRemovalStays strUcProjectResponsesIntoGraphRAG strUcIngestOrDeleteErrorFailedRetried
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-07-part3" "Use-case diagram converted from fence F194 (line 5364)." {
    title "Use case: Project responses into GraphRAG (use cases), part 3 of 4"
    include strUcDependencyChangedRemovedNeverProjected strUcProjectResponsesIntoGraphRAG strUcProjectionEnabledFalseIngestSkippedRemovalStill
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
container qbrain "STR-07-07-part4" "Use-case diagram converted from fence F194 (line 5364)." {
    title "Use case: Project responses into GraphRAG (use cases), part 4 of 4"
    include strUcRejectDuringAnIngestDocumentRemoved strUcProjectResponsesIntoGraphRAG
    exclude "relationship.tag!=UseCase"
    autolayout tb 400 250
}
