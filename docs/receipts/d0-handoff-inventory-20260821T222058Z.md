# D0 Handoff evidence inventory

TimestampUtc: 2026-08-21T22:20:58Z
Agent: GrokCode
TODO: PLAN-PLUGINHANDOFF-001
Plan: docs/plans/PLAN-PLUGINHANDOFF-001.md section 8 / D0
Phase: D0 evidence inventory only. No D1 reds. No product edits. No workflow.todo.update.

## Trust bootstrap (this process)

- Marker: F:\GitHub\McpServer\AGENTS-README-FIRST.yaml (port 7147, pid 16936, HMAC-SHA256 value 9CF9E31C7C1F4CA90A6367A1C9305EFD60C11307CB34517395C8AFDD84F59F02).
- HMAC recompute: not performed. This subagent had no pwsh/PowerShell.Mcp invoke surface.
- Health nonce d0inv-20260821T222058Z-grokcode: not echoed. GET to PAYTON-LEGION2 was rejected (hostname must have at least two dot-separated parts); GET to 127.0.0.1 was blocked as SSRF. No further MCP endpoints were probed. Inventory is local disk only.
- Session log: not opened (health/nonce not confirmed).
- Review file C:\Users\kingd\AppData\Local\Temp\PowerShell.MCP.Output\pwsh_output_20260816_214119_608_0b1a5xex.qrj.txt: not present. P1/P2/P3 remaining text taken from F:\GitHub\McpServer\docs\receipts\_hv-happly-hdone\05-todo-get-MCP-HANDOFFREVIEW-001.txt technicalDetails plus plan.md D0 reuse list.

## CancellationToken.None re-verify

- Pattern CancellationToken.None under src *Handoff*.cs, src/McpServer.Services/**/*Handoff*, and HandoffIngestionService.cs: 0 matches.
- Confirms the 2026-08-21 claim. Do not add CancellationRecovery_DoesNotUseUnboundedCancellationTokenNone.

## Remaining gaps (locked names; tests/ method-name matches = 0)

- PluginHandoffSkill_InvokeIngestGetApprove_UsesDocumentedWorkflowMethods. tests/ grep: 0. Pester handoff files: 0. PluginSync_HandoffSkill_MatchesCoreArtifact (HandoffMcpToolTests.cs:113) is checksum-only. PluginSkillWorkflow_InvokesTypedClientHandoffEndpoints (HandoffSkillDelegationTests.cs:67) is C# workflow dispatch, not skill-file invoke.
- ProcessingLease_RenewsAndFencesTerminalUpdates. tests/ grep: 0. Takeover tests exist (HandoffDurabilityTests). Implementation RenewProcessingLeaseLoopAsync exists at HandoffIngestionService.cs:972 (started at line 136). Remaining gap is a named test for heartbeat renewal plus fenced terminal updates, not a new takeover name.
- Provenance_IncludesEffectiveCustomPromptIdentityAndVersion. tests/ grep: 0. IngestAsync_CustomPromptTemplate_IsRejected (HandoffDurabilityTests.cs:536) rejects custom templates; MapEntity copies canonical PromptVersion/TemplateVersion (handoff-todo-draft/v1, handoff-todo-draft). That is not effective custom prompt identity.

Do not add AgentPool_DoesNotRetainRawHandoffSourceInPromptState. That finding is reuse of EnqueueOneShotAsync_HandoffContext_ExecutorGetsRawPromptAndSurfacesStayRedacted and Publish_HandoffContext_RedactsRawSource.

## MCP-HANDOFFREVIEW-001 P1/P2/P3 classification

- P1 processing lease renewal/takeover/fencing: mixed. Takeover/fencing reuse listed below. Remaining: ProcessingLease_RenewsAndFencesTerminalUpdates.
- P1 approval owner/version fencing: reuse (ApproveAsync_StaleApprovalLease_IsRecoveredBySecondInstance, ApproveAsync_TwoInstances_CreateExactlyOneTodo, ApproveAsync_LiveClaimant_RejectsStaleSecondClaim, ApproveAsync_LeaseExpiresDuringLiveCreate_SecondInstanceWins, ApproveAsync_ConcurrentApprovals_CreateOnce, ApproveAsync_SeparateContexts_CreatedWins).
- P1 TODO heal vs full payload: reuse (IngestAsync_ExistingTodoFromThisRun_HealsInsteadOfColliding, IngestAsync_SameKeyChangedPayload_IsCollisionNotHeal).
- P1 commit ambiguity and CancellationToken.None: reuse of SaveRunAfterTodo_* methods. CancellationToken.None absent in src Handoff. Not a new red.
- P1-5 AgentPool raw source: reuse (EnqueueOneShotAsync_HandoffContext_ExecutorGetsRawPromptAndSurfacesStayRedacted, Publish_HandoffContext_RedactsRawSource).
- P1 handle containment/UNC: reuse (NormalizeFinalPath_UncDevicePrefix_BecomesUncPath, NormalizeFinalPath_LocalDevicePrefix_IsStripped, TryGetFinalPath_InvalidHandle_FailsClosedOnWindows, ReadAsync_OpenShare_BlocksWritesAndDeletes, ReadBoundedAsync_GrowingStream_StopsAtLimit).
- P1 undefined/numeric modes: reuse (IngestAsync_UndefinedMode_DoesNotCreate, StrictEnumConverter_Numeric999_Throws, HandoffIngest_NumericMode999_ReturnsInvalidMode, Dispatcher_NumericMode999_ReturnsInvocationError, Ingest_NumericMode999_DoesNotCreate).
- P1 workspace path scope: reuse (Canonicalize_RelativeAndNested_MatchGetFullPath, IngestAsync_CrossWorkspace_DoesNotLeakRuns, IngestAsync_DifferentWorkspace_DoesNotReplay).
- P2 8 MiB before allocate: reuse (ResolveAsync_OversizedPath_FailsClosed, ResolveAsync_OversizedArtifactChunks_FailsBeforeJoin, ResolveAsync_ArtifactExactlyAtLimit_Succeeds, ReadBoundedAsync_GrowingStream_StopsAtLimit).
- P2 prompt identity: remaining Provenance_IncludesEffectiveCustomPromptIdentityAndVersion. IngestAsync_CustomPromptTemplate_IsRejected is reuse of the reject path only.
- P2 provenance sanitization: reuse (IngestAndApprove_CredentialBearingProvenance_IsSanitized, IngestAsync_SecretInDraft_IsRedactedOnPersist, IngestAsync_PersistsProvenanceWithoutSourceContent).
- P2 HTTP mapping: reuse (FromErrorCode_MapsStableStatuses, Controller_MapsErrorCodesAndRedactsInternalErrors, GetRunAsync_MissingRun_UsesErrorCodeForNotFound).
- P2 English provider fragments: reuse (IsUniqueViolation_EnglishMessageWithoutProviderCode_IsFalse, IsCommitAmbiguous_EnglishMessageOnly_IsFalse, plus provider-code uniqueness/ambiguity facts).
- P2-6 missing tests: live lease expiry, changed-payload heal, oversized Path/Artifact, plugin checksum/C# dispatch, and workspace isolation are reuse. Remaining plugin skill-file invoke: PluginHandoffSkill_InvokeIngestGetApprove_UsesDocumentedWorkflowMethods.
- P3 dead ReplayOfRunId and HandoffReviewState.Approved: reuse (HandoffReviewState_DoesNotDefineApproved, HandoffIngestionRunEntity_DoesNotExposeReplayOfRunId, SourceAndSchema_DoNotReintroduceReplayOfRunIdOrApproved). src *Handoff* ReplayOfRunId: 0. Enum values: None, PendingReview, Rejected, Created, Replayed, Failed, Approving. Approval request bool Approved is a command field, not the dead enum.

No extra Handoff* test methods were found beyond the plan D0 list. Handoff* files have 113 Fact/Theory methods (Dispose and nested helpers excluded). Plus three plan-named methods outside Handoff* files = 116 reuse methods.

## Reuse inventory (116)

Client (F:\GitHub\McpServer\tests\McpServer.Client.Tests\HandoffClientTests.cs):
- IngestHandoffAsync_PostsIngestContract (line 26)
- GetHandoffRunAsync_SendsCorrectUrl (line 52)
- ApproveHandoffAsync_PostsApprovalContract (line 71)
- HandoffContracts_RoundTripThroughClientJsonContext (line 95)
- HandoffEnums_ExposeDocumentedValues (line 165)

REPL (F:\GitHub\McpServer\tests\McpServer.Repl.Core.Tests\HandoffWorkflowTests.cs):
- Dispatcher_RoutesHandoffWorkflowMethods (line 15)
- Dispatcher_NumericMode999_ReturnsInvocationError (line 53)

REPL skill (F:\GitHub\McpServer\tests\McpServer.Repl.Core.Tests\HandoffSkillDelegationTests.cs):
- SkillDocumentedMethods_DispatchThroughWorkflowToHandoffClient (line 25)
- PluginSkillWorkflow_InvokesTypedClientHandoffEndpoints (line 67)

HTTP enum integration (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.IntegrationTests\HandoffHttpEnumIntegrationTests.cs):
- Ingest_NumericMode999_DoesNotCreate (line 22)

Migrations (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.IntegrationTests\HandoffIngestionStorageMigrationTests.cs):
- Sqlite_HandoffMigration_DowngradeAndReupgrade (line 19)
- SqlServer_HandoffMigration_DowngradeAndReupgrade (line 45)
- PostgreSql_HandoffMigration_DowngradeAndReupgrade (line 62)

Empty-db apply (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.IntegrationTests\Controllers\ProviderDatabaseIntegrationTests.cs):
- PostgreSql_CleanDatabase_AppliesHandoffMigrationAndPersistsEntity (line 83)

Controller (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Controllers\HandoffControllerTests.cs):
- IngestAsync_DelegatesToSharedService (line 16)
- GetAndApprove_DelegateToSharedService (line 34)
- GetRunAsync_MissingRun_UsesErrorCodeForNotFound (line 57)

MCP tools (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\McpStdio\HandoffMcpToolTests.cs):
- HandoffIngest_DelegatesToSharedService (line 37)
- HandoffIngest_NumericMode999_ReturnsInvalidMode (line 61)
- PublicSurfaces_ExposeIngestGetAndApprove (line 79)
- PluginSync_HandoffSkill_MatchesCoreArtifact (line 113)
- HandoffIngest_InvalidMode_ReturnsError (line 127)

Audit (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffAuditDefectTests.cs):
- IngestAsync_ConcurrentSameHash_ReservesOnce (line 47)
- GetRunAsync_FailedExtraction_DoesNotReportSuccess (line 85)
- RequireReview_MalformedDraft_IsFailedAndNotApprovable (line 109)
- IngestAsync_Cancelled_ThrowsWithoutSuccessfulPersist (line 136)
- IngestAsync_SecretInDraft_IsRedactedOnPersist (line 152)
- Parser_UnknownField_IsNotSuccess (line 178)
- ApproveAsync_SeparateContexts_CreatedWins (line 188)

Bounded source (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffBoundedSourceTests.cs):
- ResolveAsync_OversizedPath_FailsClosed (line 40)
- ResolveAsync_OversizedArtifactChunks_FailsBeforeJoin (line 58)
- ResolveAsync_ArtifactExactlyAtLimit_Succeeds (line 87)
- ResolveAsync_ArtifactCancelled_Throws (line 116)

Contained reader (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffContainedFileReaderTests.cs):
- NormalizeFinalPath_UncDevicePrefix_BecomesUncPath (line 13)
- NormalizeFinalPath_LocalDevicePrefix_IsStripped (line 22)
- TryGetFinalPath_InvalidHandle_FailsClosedOnWindows (line 30)
- ReadBoundedAsync_ExactlyAtLimit_ReturnsText (line 42)
- ReadBoundedAsync_GrowingStream_StopsAtLimit (line 53)
- ReadBoundedAsync_Cancelled_Throws (line 62)
- ReadAsync_OpenShare_BlocksWritesAndDeletes (line 73)
- ReadAsync_LongContainedPath_SucceedsWhenSupported (line 93)

DB exceptions (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffDbExceptionsTests.cs):
- IsUniqueViolation_SqliteExtendedUniqueCode_IsTrue (line 17)
- IsUniqueViolation_PostgresSqlState23505_IsTrue (line 26)
- IsUniqueViolation_SqlServerNumbers_AreTrue (line 37)
- IsUniqueViolation_EnglishMessageWithoutProviderCode_IsFalse (line 46)
- IsCommitAmbiguous_SqlServerTimeoutNumber_IsTrue (line 54)
- IsCommitAmbiguous_PostgresSqlState40001_IsTrue (line 61)
- IsCommitAmbiguous_SqliteBusyCode_IsTrue (line 69)
- IsCommitAmbiguous_EnglishMessageOnly_IsFalse (line 76)
- IsCommitAmbiguous_TimeoutException_IsTrue (line 83)

Dead contracts (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffDeadContractInventoryTests.cs):
- HandoffReviewState_DoesNotDefineApproved (line 21)
- HandoffIngestionRunEntity_DoesNotExposeReplayOfRunId (line 31)
- SourceAndSchema_DoNotReintroduceReplayOfRunIdOrApproved (line 39)

Durability (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffDurabilityTests.cs):
- IngestAsync_ConcurrentReplayOfLiveLease_ReturnsInProgressNotReplay (line 54)
- IngestAsync_StaleLease_IsTakenOverBySecondInstance (line 85)
- IngestAsync_CancelledAfterReserve_PersistsFailureThenThrows (line 109)
- ApproveAsync_StaleApprovalLease_IsRecoveredBySecondInstance (line 134)
- ApproveAsync_TwoInstances_CreateExactlyOneTodo (line 158)
- ApproveAsync_Rejection_DoesNotCreateTodo (line 176)
- IngestAsync_ExistingTodoFromThisRun_HealsInsteadOfColliding (line 190)
- IngestAsync_CallerOwnedTodoId_IsNonSuccessCollision (line 218)
- IngestAsync_InvalidRequireReviewDraft_IsFailedAndNotSuccess (line 238)
- GetRunAsync_AfterCollision_PreservesPersistedSuccessAndErrorCode (line 253)
- SaveRunAfterTodo_CommitFailure_HealsAndPreservesUnrelatedTrackedEntities (line 269)
- SaveRunAfterTodo_CancellationAfterTodo_PersistsCreatedThenThrows (line 304)
- IngestAsync_LeaseExpiresDuringLiveExtraction_TakeoverWinsAndFirstCannotCreate (line 323)
- ApproveAsync_LeaseExpiresDuringLiveCreate_SecondInstanceWins (line 373)
- ApproveAsync_LiveClaimant_RejectsStaleSecondClaim (line 412)
- IngestAsync_SameKeyChangedPayload_IsCollisionNotHeal (line 439)
- SaveRunAfterTodo_TodoAbsent_DoesNotReportCreatedFromMemory (line 468)
- SaveRunAfterTodo_CallerCancellation_PersistsCreatedReceipt (line 482)
- SaveRunAfterTodo_CompensationTimeout_DoesNotReportCreatedFromMemory (line 499)
- IngestAsync_UndefinedMode_DoesNotCreate (line 517)
- IngestAsync_CustomPromptTemplate_IsRejected (line 536)
- IngestAndApprove_CredentialBearingProvenance_IsSanitized (line 553)

Ingestion service 21 facts (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffIngestionServiceTests.cs):
- IngestAsync_SupportedFormats_AreAccepted (line 57)
- IngestAsync_UnsafeSources_ProduceDiagnosticsAndNoTodo (line 77)
- IngestAsync_MalformedExtractorJson_DoesNotCreateTodo (line 106)
- IngestAsync_UnknownSourceNotes_AreNotDiscarded (line 132)
- IngestAsync_DraftOnly_DoesNotCreateTodo (line 148)
- IngestAsync_LowConfidence_RequiresReview (line 167)
- IngestAsync_CreateWhenConfident_CreatesExactlyOneTodo (line 187)
- IngestAsync_SameHashAndPrompt_ReplaysExistingRun (line 220)
- IngestAsync_DuplicateTodoId_RequiresReview (line 242)
- IngestAsync_PersistsProvenanceWithoutSourceContent (line 263)
- ApproveAsync_RevalidatesThenCreates (line 285)
- IngestAsync_ExtractorCancelled_DoesNotCreateTodo (line 323)
- IngestAsync_MissingExtractorFields_DoesNotCreateTodo (line 342)
- IngestAsync_AmbiguousHandoff_RequiresReview (line 362)
- IngestAsync_TodoServiceFailure_RequiresReview (line 384)
- IngestAsync_ForceTrue_ExtractsAgain (line 406)
- IngestAsync_ArtifactSource_UsesDocumentChunks (line 429)
- IngestAsync_ReparseEscape_IsRejected (line 468)
- IngestAsync_DifferentWorkspace_DoesNotReplay (line 501)
- ApproveAsync_ConcurrentApprovals_CreateOnce (line 533)
- IngestAsync_BlankDescriptionAndTechnicalDetails_ReturnFieldDiagnosticsAndDoNotCreateTodo (line 581)

Extractor (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffOneShotExtractorTests.cs):
- ExtractAsync_UsesHandoffTodoDraftContextAndVersionedPrompt (line 15)

Replay keys (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffReplayKeysTests.cs):
- Create_LongWorkspace_ReturnsFixedLengthSha256 (line 14)
- Create_UnicodeWorkspace_IsStableAndFixedLength (line 24)
- Create_DelimiterCollisionInputs_ProduceDistinctIdentities (line 34)
- Create_ForceSemantics_PreserveDeterministicReplayAndUniqueForceRows (line 43)

Director/DI (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffServiceRegistrationTests.cs):
- AddHandoffServices_WithoutAgentPool_ResolvesUnavailableExtractor (line 19)
- HandoffIngestDirectorCommand_PrimaryCommand_DelegatesToExecutor (line 40)

HTTP/enum (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffStrictEnumAndHttpTests.cs):
- StrictEnumConverter_Numeric999_Throws (line 15)
- StrictEnumConverter_UndefinedName_Throws (line 25)
- FromErrorCode_MapsStableStatuses (line 42)
- Controller_MapsErrorCodesAndRedactsInternalErrors (line 47)

Validator (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffTodoDraftValidatorTests.cs):
- Validate_InvalidFields_ProduceFieldDiagnostics (line 13)
- Validate_BlankDescriptionAndTechnicalDetails_ProduceFieldDiagnostics (line 46)
- Validate_ValidDraft_NormalizesValues (line 68)
- Validate_MultiSegmentRequirementIds_AreAccepted (line 90)

Workspace paths (F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffWorkspacePathsTests.cs):
- Canonicalize_RelativeAndNested_MatchGetFullPath (line 20)
- TryCanonicalize_Blank_Fails (line 30)
- IngestAsync_CrossWorkspace_DoesNotLeakRuns (line 38)

AgentPool/one-shot redaction (not Handoff* files; plan-named reuse):
- F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\AgentPoolServiceTests.cs EnqueueOneShotAsync_HandoffContext_ExecutorGetsRawPromptAndSurfacesStayRedacted (line 222)
- F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\OneShotSensitivePromptPolicyTests.cs Publish_HandoffContext_RedactsRawSource (line 12)

## D1 recommendation

D1 should write exactly three new red tests and no product code: PluginHandoffSkill_InvokeIngestGetApprove_UsesDocumentedWorkflowMethods as Pester against plugins/core/skills/handoff/SKILL.md plus F:\GitHub\mcpserver-grok-plugin\skills\handoff\SKILL.md workflow.handoff.ingest/get/approve; ProcessingLease_RenewsAndFencesTerminalUpdates against heartbeat renewal plus owner/version-fenced terminal updates using lease/clock fakes (do not relabel existing takeover tests); and Provenance_IncludesEffectiveCustomPromptIdentityAndVersion asserting effective prompt identity and version on persisted provenance rather than only rejecting custom PromptTemplateId. Do not add AgentPool_DoesNotRetainRawHandoffSourceInPromptState or CancellationRecovery_DoesNotUseUnboundedCancellationTokenNone. Keep the 116 reuse methods green and do not invent alternate red names for them.
