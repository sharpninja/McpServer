<!-- SPDX-License-Identifier: Apache-2.0 -->
# Recon lane code-server: new and changed requirements against existing server, client, REPL, and storage code and tests

Baseline: `origin/develop` at `a8d3934b`. Every `path:line` below is at `a8d3934b` unless it says "branch", which means `chore/netanalyzers-all` HEAD `fe5879ba` in `/home/sharpninja/github/McpServer/.claude/worktrees/netanalyzers-all`. Architecture means `docs/architecture/SessionTurn-Review-AgentContext-Architecture.md` (ARCH) in the session worktree; plan means `docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md` (PLAN). Store means the exported requirement store under `scratchpad/recon/store/`. Read-only work: `git show`, `git grep`, `git log` through pwsh. Inference is labeled.

Method: for each unit of ARCH 4.2, 5.2, 6.5, each policy of 4.3 and 5.3, and each shared component of 3.5, I located the develop code it changes, wraps, or replaces, then every caller of the same members (controllers, MCP tools, client, REPL, McpAgent, QuadBrain internal tools, decorators, ingestion, federation), then every test that exercises those members. Test counts are `[Fact]` and `[Theory]` attributes counted with `git grep` (theory rows not expanded).

## Tests that break or change

### 1. The `closed` status refusal on agent surfaces (ARCH 3.1 line 145, 3.5.3 line 320)

- No develop writer produces a turn status `closed`: the only `"closed"` literal on the session-log path is the terminal predicate `SessionLogService.cs:1359` (`IsTerminalTurnStatus`), and `plugins/core/lib-ps` has none. Inference: "server-internal writers and existing rows keep `closed`" protects existing rows only; no internal writer exists to protect.
- The only tests that write `closed` call the service directly: `SessionLogSupersessionTests.SubmitAsync_StaleCancellation_PreservesTerminalTurn` (`tests/QBrainAi.Support.Mcp.Tests/Services/SessionLogSupersessionTests.cs:49`, row `[InlineData("closed", "CANCELED")]` at `:48`) and `SubmitAsync_StaleActiveSnapshot_PreservesTerminalTurn` (`:71`, row at `:68`). They survive only if the refusal stays in the handlers (as the Submit, Upsert, Patch, Replace diagrams draw it, ARCH 4.2) and never moves into `TurnWritePipeline`, which the service runs.
- Gap: `QuadBrainInternalToolExecutor.SessionTurnUpdateAsync` deserializes the whole turn from model-supplied arguments and calls `ISessionLogService.UpsertTurnAsync` directly (`src/QBrainAi.Support.Mcp/Services/QuadBrainInternalToolExecutor.cs:299-312`), bypassing every handler. ARCH lists QuadBrainInternalToolExecutor as gated (5.3 line 2048) but its `mcp_session_turn_update` tool can still store `closed`. No existing test covers it (the executor tests use a substituted `ISessionLogService`, `tests/QBrainAi.Support.Mcp.Tests/Services/QuadBrainExecutorTestFixture.cs:125`).

### 2. CompletionGate (S10) on every agent-origin close, including PUT

The gate refuses `completed` without `requestClass` and terminal reviews, and `failed` without `requestClass` (ARCH 5.3 lines 2021-2059). Develop accepts both for every non-QBAgent source (`SessionLogService.cs:1329-1353`). Every existing test that writes a `completed` or `failed` turn through an Agent-origin path goes red at S10 unless the plan's `WithCompletionEvidence` fixture helper (PLAN:645) is applied.

- Scale (heuristic scan of test methods that write `completed` or `failed` directly or through a file-local fixture): 196 tests in 43 files across `tests/`. The server-path subset that the real gate reaches (service, controller, MCP tool on a real service, integration) is at least: `SessionLogServiceTests.cs` 58 tests through fixtures `CreateTestDto` and `CreateRelationalTurn`; `SessionLogServiceReplaceDeleteTests.cs` 14 through `SeedFullSession`; `SessionLogControllerTests.cs` (integration) 20 through `CreateTestDto`, whose turn is `Status = "completed"` (`tests/QBrainAi.Support.Mcp.IntegrationTests/Controllers/SessionLogControllerTests.cs:818-844`, status at `:838`); `SessionLogReplaceDeleteControllerTests.cs` 8; `SessionLogSupersessionTests.cs` 8; `TransactionGatedSessionLogServiceTests.cs` 5; `SessionLogImportedSessionDeleteTests.cs` 3; `SessionLogResubmissionReviveTests.cs` 2; `SessionLogServiceAgentLinkTests.cs` 2; `SessionLogTriageStoreTests.cs` 2; `SessionLog.Validation` 10 (`QuerySessionLogTests.cs:74,109,204`, `SubmitSessionLogTests.cs:106,162,205`, `SessionLogErrorTests.cs:50,76`). Inference: the REPL and McpAgent counts (about 40) use fake clients and do not reach the server gate.
- Opposite assertions that must be rewritten, not just migrated:
  - `SessionLogServiceTests.UpsertTurnAsync_CompletedEmptyTurn_AcceptedForClaudeCode_RejectedForQBAgent` (`tests/QBrainAi.Support.Mcp.Tests/Services/SessionLogServiceTests.cs:538`) asserts a ClaudeCode `completed` turn with nothing else is accepted.
  - `SessionLogServiceTests.UpsertTurnAsync_StandardAgentClosingTurnWithoutEvidence_Succeeds` (`:1261`).
  - `SessionLogControllerTests.WhenStandardAgentClosingTurnWithoutComplianceItemsThenSucceeds` (integration `:420`) and `CompleteTurn_StandardAgentWithoutEvidence_Succeeds` (`:733`); `CompleteTurn_WithEvidence_FinalizesTurn` (`:701`); `FailTurn_SetsFailedStatusWithNote` (`:777`, `failed` without class).
- Gate order changes the QBAgent refusal body. `TurnWritePipeline` runs CompletionGate before the QBAgent compliance check (ARCH 3.5.3 diagram lines 356-359). A QBAgent close with neither class nor evidence now gets the gate refusal, not the compliance message:
  - `SessionLogControllerTests.WhenAcidAgentClosingTurnWithoutComplianceItemsThenReturns400` (integration `:386`) asserts the body contains "no decision, action, or commit items" and "Compliance with Session Logging Requirements is not optional." (`:410-411`).
  - `SessionLogServiceTests.UpsertTurnAsync_AcidAgentClosingTurnWithoutEvidence_ThrowsArgumentException` (`:1230`) asserts `ArgumentException` with the same text (`:1247-1252`).
  - `CompleteTurn_AcidAgentWithoutEvidence_Returns400` (integration `:755`) still gets 400, with a different body.
  - PLAN TR-MCP-SESSIONCLASS-002 AC-002 places the gate "beside `ValidateTerminalTurnCompliance`" (PLAN:362) and does not fix this order; ARCH fixes it gate-first.
- PUT is gated (FR-MCP-SESSIONCLASS-002-AC004, ARCH 4.2 line 601). Baseline-green evidence that PLAN relies on goes red: `SessionLogServiceReplaceDeleteTests.ReplaceTurnAsync_ClearsOmittedScalarsAndCollections` (`tests/QBrainAi.Support.Mcp.Tests/Services/SessionLogServiceReplaceDeleteTests.cs:26`, replacement `Status = "completed"` at `:36` over an `in_progress` seed at `:317`) and `ReplaceTurnAsync_PreservesWorkspaceStampOnChildren` (`:61`, `:71`). PLAN names both as the baseline-green proof of FR-SUPPORT-016 ac-1 that "must stay green at every later gate" (PLAN:195, :550, :592). Same for integration `SessionLogReplaceDeleteControllerTests.PutTurn_ReplacesTurn_ClearsOmittedSections` (`tests/QBrainAi.Support.Mcp.IntegrationTests/Controllers/SessionLogReplaceDeleteControllerTests.cs:27`, `:34`) and `SessionLogControllerTests.WhenPuttingTurnWithValidRequestIdThenReplacesAndReturns200` (`:450`, `:460`).
- Gated producers with no class or review input at all (production regressions with no failing test, because their tests use substitutes):
  - QBAgent's `mcp_session_turn_complete` sets `completed` and nothing else (`QuadBrainInternalToolExecutor.cs:314-328`; exposed by `src/QBrainAi.McpAgent/Hosting/McpHostedAgentToolAdapter.cs:68` and `src/QBrainAi.McpAgent/QBAgentDefinition.cs:15`). ARCH gates this origin (line 2048), and no unit or TR adds class or reviews to this tool. TR-MCP-SESSIONCLASS-004 covers only `src/QBrainAi.McpAgent/SessionLog/SessionLogWorkflow.cs`.
  - REPL `importRecovery` derives `completed`/`failed` from transcripts (`src/QBrainAi.Repl.Core/SessionLogWorkflow.cs:326`, `:680-746`) and is gated (ARCH line 2048).
  - The plugin integration adapter calls `workflow.sessionlog.completeTurn` per host against the real server fixture (`tests/QBrainAi.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs:185`, `:757`). All 37 PluginIntegration tests that complete turns go red at S10 unless the scenario records class and reviews first. PLAN names new `RealGate_*` cases but not this change to the existing scenarios.
- Exempt origins must be wrapped (TR-MCP-SESSIONCLASS-003). Exempt writers and their call sites at baseline: `AuditedAgentCliClient.cs:282,336`, `WorkspacePolicyService.cs:255-291` (writes `completed`), `QuadBrainOpenAiChatService.cs:252-270` (writes `completed`), `FederationStateAdapterImplementations.cs:1023` (Replication; it passes no `sourceFilePath`, so develop treats it as a non-import write), `SessionLogIngestor.cs:219,264` and `TranscriptSessionLogPersister.cs:31` (Import). `WorkspacePolicyServiceTests.ApplyAsync_ValidDirective_UpdatesWorkspaceAndLogsPolicyChange` (`tests/QBrainAi.Support.Mcp.Tests/Services/WorkspacePolicyServiceTests.cs:110`) runs the real service, so it breaks if the ServerInternal scope is missed.

### 3. Error bodies moving to ClassifiedError (`ClassifiedErrorException`, `McpErrorClassifier`, the gate token)

- Develop has no `ClassifiedErrorException`. "ClassifiedError" today is a private controller helper (`src/QBrainAi.Support.Mcp/Controllers/SessionLogController.cs:714-733`) that maps an exception through `McpErrorClassifier.Classify` (`src/QBrainAi.Support.Mcp/Services/McpErrorClassifier.cs:44-148`). The new one-branch mapping is additive to that file.
- Controller and MCP surfaces now dispatch, so construction breaks:
  - `new SessionLogController(service, logger)` appears 13 times in `tests/QBrainAi.Support.Mcp.Tests/Controllers/SessionLogControllerErrorTests.cs` (`:32,67,94,125,145,170,202,234`) and `SessionLogControllerReceiptTests.cs` (`:32,58,77,92,114`). The constructor today takes only `ISessionLogService` and a logger (`SessionLogController.cs:25-30`).
  - Mocks set up `ISessionLogService.SubmitAsync`, but SubmitSessionLogCommand delegates to `SubmitWithOutcomesAsync` (ARCH 4.2 line 558), so the setup is never hit: `SessionLogControllerErrorTests.SubmitAsync_DbUpdateException_ReturnsPersistenceProblem` (`:23`, setup `:27`), `SubmitAsync_StorageBudgetExceeded_ReturnsBackendUnavailableEnvelope` (`:139`, setup `:142`), and all five `SessionLogControllerReceiptTests` (`:26,52,71,85,106`).
  - `FwhMcpTools` takes `IDispatcher? dispatcher = null` (`src/QBrainAi.Support.Mcp/McpStdio/QBrainAiMcpTools.cs:110`). 13 MCP tool test files construct it without a dispatcher; the session-log tool tests among them break once the tools dispatch: `McpToolErrorEnvelopeTests` (`:45,73,95`), `SessionLogLifecycleToolErrorTests` (`:47,65,81,110,144`), `McpToolBackendUnavailableErrorTests` (`SessionLogReplaceSection` call at `:122`), `SessionLogStdioSanitizationTests` (`:48`), `TransactionGatedStdioRoutingTests`.
- REPL envelope cannot carry the new codes (gap with a projected defect). ARCH says `ErrorCodes` is read by "the server, the REPL envelope, and the plugin's receipt mapping" (3.5.2 line 314; C9 line 195). The REPL classifier, which ARCH never names:
  - It propagates the server `code` and `retryable` only for HTTP 503 (`src/QBrainAi.Repl.Core/ReplMcpErrorClassifier.cs:70-95`).
  - It hard-codes `Retryable: false` for every `McpConflictException` (`:60-67`), although the client parses the server's `retryable` (`src/QBrainAi.Client/McpClientBase.cs:626-629`, `:667-672`). So a 409 `conflict` with `retryable: true` reaches the plugin as final.
  - A server 400 or 422 (`McpValidationException`, which does not derive from `ArgumentException`: `src/QBrainAi.Client/McpClientExceptions.cs:15,89`) becomes `method_invocation_error` (`src/QBrainAi.Repl.Core/ReplCommandDispatcher.cs:3360-3362`), losing `review_*`, `request_class_invalid`, and `validation_error`.
  - Inference: `Resolve-MintOutcome` (ARCH 3.5.6 line 431) would classify a retryable mint conflict as final.
  - Existing tests pin only the false case: `RequirementsRecoveryDispatcherTests.ApplyRecovery_Conflict_ReturnsClassifiedConflict` (`tests/QBrainAi.Repl.Core.Tests/RequirementsRecoveryDispatcherTests.cs:20`). No existing or planned test covers 409 retryable true or a 4xx code passing through the REPL.
  - A third code table, `SessionLogErrorCodes` (`src/QBrainAi.Repl.Core/SessionLogErrorEnvelope.cs:77-147`), coexists with the proposed `ErrorCodes`.
- Deadlock semantics change on the turn writes. Develop turns SQL Server 1205 during graph load into `StorageGraphMaterializationException`, retryable `backend_unavailable` (`SessionLogService.cs:345-350`). `DbErrorClassifier` makes 1205 a transient conflict that `SerializableWrite` retries, then 409 `conflict` retryable (ARCH 3.5.2 lines 267-268).
  - `SessionLogDenseGraphTests.UpsertTurnAsync_SqlDeadlock1205_DoesNotReportMissingSession` (`tests/QBrainAi.Support.Mcp.Tests/Services/SessionLogDenseGraphTests.cs:100`, TEST-MCP-TRIAGESTORE-007) asserts the throw. Its interceptor fails only the first reader (`:286-293`), so a retried write succeeds and the test fails.
  - `ReplMcpErrorClassifierTests.FromException_GraphMaterializationDeadlock_IsBackendUnavailable` (`tests/QBrainAi.Repl.Core.Tests/ReplMcpErrorClassifierTests.cs:77`) pins the old code.
- A unique violation surfaced to a tool or controller is 409 `conflict` with `retryable: false` today (`McpErrorClassifier.cs:112-127`; asserted by `McpToolErrorEnvelopeTests.SessionLogCompleteTurn_DbUpdateException_ReturnsFourFieldEnvelopeWithInner`, `tests/QBrainAi.Support.Mcp.Tests/McpStdio/McpToolErrorEnvelopeTests.cs:45`, `:66`). Under SerializableWrite a declared-key violation is rerun and ends as `retryable: true`. These mock-level tests stay green but now assert a contract the real path no longer has.
- Today's 503 `persistence_error` for SQLite busy (`McpErrorClassifier.cs:129-140`; `McpErrorClassifierTests.Classify_SqliteBusy_IsRetryablePersistenceError`, `tests/QBrainAi.Support.Mcp.Tests/Services/McpErrorClassifierTests.cs:47`) stays on routes outside SerializableWrite (dialog, titles). Routes inside it answer 409 `conflict` retryable. Inference: callers see two retryable codes for the same contention.

### 4. Serializable writes and 409 `conflict` retryable (SerializableWrite, ARCH 3.5.2)

- Caller-owned transaction rule missing from ARCH. `SessionLogService.SubmitAsync` refuses to run inside a caller-owned EF or `System.Transactions` transaction (`SessionLogService.cs:119-128`, "Session submission cannot acknowledge a durable write inside a caller-owned transaction."). ARCH cites only `:153-157` as the SerializableWrite pattern. It adds "a command dispatched from inside a delegate ... joins the caller's transaction" (line 268) and says nothing about this refusal. Tests that pin it:
  - `SessionLogSupersessionTests.SubmitAsync_CallerTransaction_RejectsBeforeMutation` (`:192`)
  - `SubmitAsync_AmbientTransaction_RejectsBeforeMutation` (`:204`, asserts "caller-owned")
  - `SubmitAsync_PendingTrackedEdit_RejectsWithoutDiscardingEdit` (`:178`)
  - `SessionLogControllerReceiptTests.SubmitAsync_OuterRollback_NeverReturnsDurableReceipt` (`:106`)
- EF InMemory provider. Develop opens its Serializable transaction only when `IsRelational()` (`SessionLogService.cs:155`), and Upsert, Replace, and Append run with no transaction. ARCH says SerializableWrite runs "on SQLite, PostgreSQL, and SQL Server" and says nothing about InMemory. 15 test files (151 tests) run the real `SessionLogService` on `UseInMemoryDatabase`:
  - `SessionLogServiceTests` 63, `SessionLogServiceTurnContextTests` 20, `SessionLogIngestorImportTests` 14, `FederatedSessionLogServiceTests` 10, `SessionLogSessionAttrTests` 7, `SessionLogTriageStoreTests` 7, `SessionLogSubmitBudgetTests` 6
  - `SessionLogLifecycleToolErrorTests` 5, `SessionLogControllerReceiptTests` 5, `InvokeWorkflowBeginTurnTests` 4, `SessionLogIngestorChunkingTests` 3, `SessionLogReplaceSectionRetryableTests` 2, `SessionLogServiceAgentLinkTests` 2, `TranscriptSessionLogPersisterTests` 2, `WorkspacePolicyServiceTests` 1
  - Inference: an unguarded `BeginTransaction` throws on InMemory, so these break unless SerializableWrite keeps develop's `IsRelational()` guard.
  - The integration factory uses SQLite files (`tests/QBrainAi.Support.Mcp.IntegrationTests/CustomWebApplicationFactory.cs:94-96,146-147`). CLAUDE.md's "in-memory EF" note for integration tests is stale.
- Race test with an exact exception type. `SessionLogSupersessionTests.SubmitAsync_ConcurrentCompletion_PreservesCompletedResponse` (`:216`, both theory rows) runs the completion inside the other writer's `SavingChanges` interceptor (`:238-248`). It asserts any completion error is a `SqliteException` with code 5 or 6 (`:251-254`). Under SerializableWrite that error becomes the 409 `ClassifiedErrorException` after three retries, and the `explicitTurnWriter` row (Upsert) gains a transaction it does not have today.
- New three-provider cases can reuse existing plumbing:
  - `EphemeralPostgresFixture` (`tests/QBrainAi.Support.Mcp.Tests/Storage/EphemeralPostgresFixture.cs:27`)
  - `MCP_TEST_SQLSERVER_CONNECTION` with LocalDB (`SqlServerDecompose4nfBackfillMigrationTests.cs:32`)
  - `ProviderIntegrationTestSupport.cs:411`
  - The 12 `Category=Integration` classes in Support.Mcp.Tests run only in `MigrationIntegrationTests` (`build/Build.TestDependencies.cs:70-74`).

### 5. Upsert race retry replaced by IdempotentCreate `MergeInto`

- Code replaced: the message-text race retry `SessionLogService.cs:764-797` with predicate `:812-817`, and the session-level retry on "UNIQUE constraint failed" at `:234-253`.
- Test that must change: `SessionLogServiceTests.UpsertTurnAsync_UniqueSessionLogTurnsRace_RetriesOnceAndUpdates` (`:1840`, TEST-MCP-SESSIONLIFE-005). Its `RivalTurnInsertInterceptor` inserts a rival row on a second SQLite connection from inside `SavingChanges` (`:1911-1931`). Inference: inside a Serializable transaction on a file database, the rival writer blocks on the first transaction's lock or gets SQLITE_BUSY, and the rerun fires the interceptor again. The asserted outcome (one row, winner text) is compatible, but the mechanism must be rewritten. PLAN names no replacement case for the single-turn race beyond the forced-interleaving set.
- No test covers the session-level race at `:234-253` (risk).
- Tombstone gap (Inference, design risk). McpDbContext gives every `*Entity` a `SoftDelete` query filter and soft-delete columns (`src/QBrainAi.Storage/McpDbContext.cs:1302-1331`). The unique indexes are not filtered by `IsDeleted`: `(WorkspaceId, SourceType, SessionId)` at `:391` and `(SessionLogId, RequestId)` at `:404`.
  - Develop revives a tombstoned session before inserting (`SessionLogService.cs:161-169`), covered by `SessionLogResubmissionReviveTests` (`:33`, `:56`).
  - ARCH's IdempotentCreate looks the key up (ARCH 3.5.2 line 284) with no `IgnoreQueryFilters` or revive step.
  - A soft-deleted turn (DELETE turn route) whose requestId or `IngressNonce` is written again misses the lookup, violates the index, and is rerun until 409 `conflict` retryable. For a mint, the plugin then keeps its failsafe record and redrains forever.
  - No test covers re-writing a soft-deleted turn.

### 6. Review reset rule and review sections

- No develop test touches review sections, so nothing breaks directly. `TurnSection` and `ParseSection` (`SessionLogService.cs:1287-1317`) gain two members, and the unknown-section message text changes (`:1313-1315`). No test asserts that text. `ReplaceTurnSectionAsync_UnknownSection_Throws` (`SessionLogServiceReplaceDeleteTests.cs:272`, "nonsense") and `PutSection_UnknownSection_Returns400` (integration `SessionLogReplaceDeleteControllerTests.cs:83`, "bogus") stay valid.
- Surface gap: develop's MCP tools `sessionlog_replace_section`, `sessionlog_clear_section`, and `sessionlog_delete_item` (`src/QBrainAi.Support.Mcp/McpStdio/FwhMcpTools.SessionLog.cs:203-277`) call `ReplaceTurnSectionAsync`, `ClearTurnSectionAsync`, and `DeleteTurnItemAsync` directly.
  - ARCH lists only REST PUT/DELETE, `sessionlog_record_review`, and the client as review surfaces (5.2 lines 1867, 1899).
  - PLAN TR-SUPPORT-CORE-016 ac-2 makes the service's `TurnSection` route review sections (PLAN:351), and PLAN case `SessionLogRecordReviewToolTests.SectionReplaceAndClear_AgentAndHostile` expects the MCP section path to work.
  - Inference: ReviewValidation, SerializableWrite, and the terminal reset refusal must live in the service, not only in the RecordSessionTurnReviewCommand and ResetSessionTurnReviewCommand handlers. Otherwise the MCP section tools bypass them.
- Delegation gap: ARCH says Phase A and B handlers delegate to named `ISessionLogService` members (4.2 line 512). The two review units, GetSessionLogQuery, and QuerySessionLogsQuery name none (only Mint, SubmitWithOutcomes, Upsert, Replace, Open, Append, and the title members are named at ARCH lines 479-770).
  - PLAN TR-SUPPORT-CORE-014 ac-5 says "record review uses UpsertTurnAsync" (case `RecordReview_WritesThroughUpsertTurnAsync`, PLAN:436, :517).
  - Inference: a handler that writes `McpDbContext` directly skips `TransactionGatedSessionLogService` snapshot restore (`src/QBrainAi.Support.Mcp/Services/TransactionGatedSessionLogService.cs:280-320`, `:470-535`), `FederatedSessionLogService`, and `SessionLogSanitizingService`.

### 7. Sanitize on read and in search text

- Existing integration test and ARCH disagree. `SessionLogSanitizationControllerTests.S16_QueryTextFilter_SecretContainingRawRecordStillParticipates_AndPagingMetadataUnchanged` (`tests/QBrainAi.Support.Mcp.IntegrationTests/Controllers/SessionLogSanitizationControllerTests.cs:95`) asserts that a text filter matching a raw secret still finds the record.
  - ARCH's QuerySessionLogsQuery says filtering is computed "from the raw stored data, before redaction" (line 1941).
  - ARCH 3.4 Search (line 232) and TR-MCP-SESSIONREVIEW-002 AC-005 require that a redacted review value "matches no turn".
  - Search runs `BuildSearchText` in memory (`SessionLogService.cs:533`, `:1367`), so the review fields need a redacted path while every other field stays raw. This is not stated as a rule, and S16's raw-participation principle is not reconciled.
- `SessionLogSanitizingService` already sanitizes every `QueryAsync` and `GetAsync` result (decorator chain `src/QBrainAi.Support.Mcp/Program.cs:642-665`; STDIO `McpStdio/McpStdioHost.cs:293-306`). `TurnReadProjection` redacts review text again (ARCH line 363). The double pass is safe because sanitizing is idempotent (`SessionLogSanitizerTests.SanitizeString_IsIdempotent`, `tests/QBrainAi.Support.Mcp.Tests/Services/SessionLogSanitizerTests.cs:138`). Inference: it is a DRY overlap ARCH does not mention.
- `SessionLogSanitizingServiceTests.MutationMethods_DelegateWithoutSanitizingInputs` (`tests/QBrainAi.Support.Mcp.Tests/Services/SessionLogSanitizingServiceTests.cs:58`) enumerates today's write members only. New members (`SubmitWithOutcomesAsync`, `MintTurnAsync`, and any review member) are uncovered until extended (TR-MCP-SESSIONTURN-004 AC-004).

### 8. Paging Clamp versus Refuse

- No break. QuerySessionLogsQuery keeps develop's clamp (`SessionLogService.cs:476-477`). That is pinned by `SessionLogServiceTests.WhenQueryingWithLimitExceedingMaxThenLimitIsClamped` (`:450`) and `WhenQueryingWithLimitAndOffsetThenPaginationIsApplied` (`:432`), and by the S16 paging assertions (integration `:114-131`).
- `Refuse` touches only new agent-context queries.
- QuerySessionLogsQuery keeps `SessionLogQueryResult`, while agent-context queries use the new `PageResult<TItem>`. Inference: two page wire types for one component.

### 9. WorkspaceResolutionBehavior registered for every command and query

- What exists:
  - No pipeline behavior is registered anywhere in `src` today; the only one is the test `PrefixBehavior` (`tests/QBrainAi.Cqrs.Tests/DispatcherTests.cs:74`, exercised by `SendAsync_WithBehavior_BehaviorExecutes` at `:143`).
  - The dispatcher is a singleton (`src/QBrainAi.Cqrs/ServiceCollectionExtensions.cs:22-27`). It resolves handlers and behaviors from its constructor `IServiceProvider` (`src/QBrainAi.Cqrs/Dispatcher.cs:93-96`), and `IPipelineBehavior.HandleAsync` receives `object request` (`src/QBrainAi.Cqrs/IPipelineBehavior.cs:21`).
  - Inference: a behavior that writes "the scoped `WorkspaceContext`" (ARCH line 250) gets the root-scope instance, not the request's, unless the dispatcher changes. ARCH does not say how the behavior reads `WorkspacePath` from `object` (every existing record has `WorkspacePath` as its first parameter, but no interface marks it).
  - ARCH also never defines when a workspace is "not resolved": `WorkspaceIdentity` is a lexical normalizer (`src/QBrainAi.Storage/WorkspaceIdentity.cs:61-70`).
- Composition roots: `Program.cs:489-490` and `McpStdio/McpStdioHost.cs:319-320` both register the dispatcher. ARCH names only `AddCqrsBehavior`. Tests that build their own DI (`AddCqrs` in `MemoryGlobalDefaultWorkspaceTests.cs:198`, `MemoryMigratedSchemaOperationsTests.cs:118`, `MemoryS1Harness.CoreB.cs:171`; `AddCqrsDispatcher` in `UseCaseExpandedScopeTests.cs:36`) will not see the behaviors unless registration moves into the shared extension. That would hide production regressions.
- Existing handlers that start seeing it:
  - Memory: 13 records dispatched from `MemoryController.cs` (7), `FwhMcpTools.Memory.cs` (6), `TransactionGatedMemoryService.cs` (4), and nested `MemoryExploreOperations.cs:250`.
  - Use cases: 19 handlers (`src/QBrainAi.Services/UseCases/ServiceCollectionExtensions.cs:28-48`), dispatched from `UseCasesController` (22 sites) and `FwhMcpTools.UseCases` (15).
  - Products: 10 records, dispatched from `ProductsController` (8), `FwhMcpTools.Products` (8), `ContextController.cs:242`, `RequirementsController.cs:136`, `FwhMcpTools.Context.cs:121`, `FwhMcpTools.Requirements.cs:75`.
  - GraphRAG: 17 records are registered (`src/QBrainAi.GraphRag/ServiceCollectionExtensions.cs:31-48`), but grep found no production dispatch site, only direct handler calls in `GraphRagCqrsTests.cs:33-106`.
- Behavior and body changes:
  - Global memory regression. `MemoryController` dispatches `RememberMemoryCommand(string.Empty, ...)` when the workspace is empty (`src/QBrainAi.Support.Mcp/Controllers/MemoryController.cs:125`, `:298-299`), and FR-MCP-MEMORY-001 requires Global memories with no workspace. `MemoryGlobalDefaultWorkspaceTests.Controller_GlobalCreate_EmptyWorkspace_ReturnsCreated` (`tests/QBrainAi.Support.Mcp.Tests/Memory/MemoryGlobalDefaultWorkspaceTests.cs:100`) dispatches exactly that and expects 201 Global. A behavior that refuses an unresolved workspace returns 404 `not_found` (`details.resource` `workspace`). Inference: the test breaks if it sees the behavior, and production regresses silently if it does not.
  - Use cases fall back to the ambient `WorkspaceContext` when `WorkspacePath` is blank (`src/QBrainAi.Services/UseCases/UseCaseCqrsHelpers.cs:52-66`); otherwise they throw "Workspace path is required for use case operations." No test asserts that text.
  - MCP tools already resolve the workspace themselves: `ApplyWorkspaceOverride` (`QBrainAiMcpTools.cs:156`, untested by name), and memory tools prefer the ambient context over the tool argument (`FwhMcpTools.Memory.cs:63,178,217,246`). ARCH says "on MCP the tool's `workspacePath`" (line 250). Inference: a second resolution path ARCH neither cites nor retires.

### 10. ModeGateBehavior

- No existing record implements `IModeGatedRequest`, so no existing test changes. The only mechanics coverage is `DispatcherTests.SendAsync_WithBehavior_BehaviorExecutes` (`:143`).
- Behavior order is registration order (`ServiceCollectionExtensions.cs:74-84`, "first registered = outermost"). ARCH requires WorkspaceResolution before ModeGate (line 252), which depends on call order in both composition roots.
- No named test exists or is planned for ModeGateBehavior or WorkspaceResolutionBehavior beyond "one test class named for it" (ARCH 3.5 line 244). No regression case is planned for the existing memory, use-case, or product units under the new behavior.

### 11. New DTO definitions in QBrainAi.Client used by the server; interface growth

- Hand-written double breaks at compile time when `ISessionLogService` gains members (16 members today, `src/QBrainAi.Services/Services/ISessionLogService.cs`): `RecordingSessionLogService : ISessionLogService` (`tests/QBrainAi.Support.Mcp.Tests/Services/QuadBrainOpenAiChatServiceTests.cs:840`). 24 other test files substitute the interface (NSubstitute) and compile.
- The server already reaches `QBrainAi.Client` (`src/QBrainAi.Storage/QBrainAi.Storage.csproj:43`; `QBrainAi.Services.csproj` references Storage and McpAgent, and McpAgent references Client). New server-used types in `QBrainAi.Client.*` become public API of the NuGet package `QBrainAI.Client` (`src/QBrainAi.Client/QBrainAi.Client.csproj`, PackageId).
- `UnifiedSessionLogDtoContractTests.UnifiedSessionLogModels_DoNotExposePublicListProperties` (`tests/QBrainAi.Support.Mcp.Tests/Models/UnifiedSessionLogDtoContractTests.cs:18`) constrains the Phase B fields on the server copy of `UnifiedRequestEntryDto` (no public `List<T>`).
- Integration code already deserializes the submit 201 into the client's `SessionLogSubmitResult` (`SessionLogControllerTests.cs:38`; type at `src/QBrainAi.Client/Models/SessionLogModels.cs:396`). The controller returns an anonymous object today (`SessionLogController.cs:73-85`), so the `turns` addition can reuse the client type.
- Untested shape change: `sessionlog_submit` returns `{ success = true, id }` (`FwhMcpTools.SessionLog.cs:35-36`), and no test calls `SessionLogSubmit`. The `{ success, id, turns }` change has no existing guard.
- Untested limit: the controller's 5000-turn cap and its `ValidationProblem` bodies ("Invalid session log body.", "Invalid sessionId.", "Too many turns.") at `SessionLogController.cs:19,43-67` are not in SubmitSessionLogCommand's error list (ARCH line 563). No test covers the cap.

### 12. Renamed seams (PLAN name versus ARCH name versus develop)

- TR-MCP-SESSIONTURN-004 AC-002: `ISessionLogTurnUpsertGuard` (PLAN:355) versus `ITerminalTurnOutcomeGuard` (ARCH 4.1 line 479, 4.3 line 790). Test class `SessionLogTerminalTurnGuardContractTests` (PLAN:421).
- TR-MCP-SESSIONCLASS-002: `ISessionTurnCompletionGate` "placed beside ValidateTerminalTurnCompliance" (PLAN:362) versus policy CompletionGate inside `TurnWritePipeline`, running before the QBAgent check (ARCH 3.5.3).
  - AC-003 `SessionTurnCompletionGateException` and a classifier mapping versus `ClassifiedErrorException` with "no per-feature branch" (ARCH line 314).
  - AC-004 `ChangeTracker.Clear()` versus SerializableWrite rollback (ARCH line 2049).
  - PLAN tests `McpErrorClassifier` gate cases (PLAN:428) and `McpErrorClassifierReviewTests` with "classifier mappings (400, 422)" (TR-MCP-SESSIONREVIEW-003 AC-004, PLAN:367) presuppose per-feature branches.
- TR-MCP-SESSIONREVIEW-002 AC-001 "server and client `SessionLogTurnReviewDto` parity" and test `SessionLogDtoParityTests` (PLAN:366, :432) versus "defined once ... no server copy and no parity test" (ARCH 3.5 line 243, 5.1 line 1833, 6.4 line 2586).
- AC-002 converter `SessionLogReviewScoreJsonConverter` (PLAN test `SessionLogReviewScoreJsonConverterContractTests`, PLAN:432; ARCH itself at 3.4 line 227) versus `ScoreLexemeJsonConverter` (ARCH 3.5.4 line 367).
- TR-MCP-SESSIONREVIEW-006 `ISessionTurnReviewDefaults` and test `SessionTurnReviewDefaultsContractTests` (PLAN:370, :436) versus `ReviewDefaults` (ARCH line 369) and its one class `ReviewDefaultsTests` (ARCH line 244).
- TR-MCP-SESSIONREVIEW-007 `IReviewThresholdProvider` and `IReviewStatusDeriver`, test `ReviewStatusDeriverContractTests` (PLAN:371, :437) versus `ScoreVerdict` (ARCH line 368).
- TR-MCP-SESSIONREVIEW-004 `IReplJsonNumberNormalizer`, test `ReplJsonNumberNormalizerContractTests` (PLAN:368, :434) versus the REPL normalizer calling `ScoreLexeme` (ARCH line 367).
- Develop helpers ARCH retires but does not list by test:
  - `IsTerminalTurnStatus` (`SessionLogService.cs:1355`) becomes `TurnStatus.IsTerminal`.
  - `IsSessionLogTurnUniqueRace` (`:812`), `IsSqlDeadlock` (`:357`), and the `:234` text predicate become `DbErrorClassifier`.
  - `McpErrorClassifier.IsConflict` and `IsBusy` (`McpErrorClassifier.cs:160-190`) and `ReplMcpErrorClassifier.IsConflict` and `IsBusy` (`:154-182`) keep text heuristics. Inference: two DB-error classifiers with different answers for SQLite 5 and 6.

### 13. Marker contracts

- `MarkerFileService.BuildDefaultAgentPlugins` builds five entries (codex, claude-code, copilot, cline, grok: `src/QBrainAi.Services/Services/MarkerFileService.cs:547-607`). The fallback `SyncedAgentPluginVersion = "1.26.0"` is at `:34`.
- `MarkerFileServiceTests.BuildDefaultAgentPlugins_UsesCurrentSyncedPluginVersion` (`tests/QBrainAi.Support.Mcp.Tests/Services/MarkerFileServiceTests.cs:781`, asserts literal "1.26.0" at `:789`) breaks if the A9 nine-entry change also moves the constant to the 1.119.0 sync version (ARCH 8.5).

## Existing coverage of reused code

Components ARCH reuses that already have direct tests the new seams can rely on:

- Dispatcher pipeline: `DispatcherTests` (10 tests, `tests/QBrainAi.Cqrs.Tests/DispatcherTests.cs:99-215`) and `ResultTests` (`Result<T>.Failure(Exception)`, `src/QBrainAi.Cqrs/Result.cs:50`).
- Terminal guard rule replaced by `ITerminalTurnOutcomeGuard`: `SessionLogSupersessionTests`, 12 test methods (`:49,71,83,99,113,125,140,155,178,192,204,216`), plus integration `SubmitReceipt_ConfirmsReadableSession_AndReplayPreservesTerminalHistory` (`SessionLogControllerTests.cs:27`).
- Single-turn status correction (no outcome guard on Upsert): `SessionLogSupersessionTests.UpsertTurnAsync_ExplicitStatus_CanCorrectTerminalTurn` (`:125`).
- QBAgent compliance check: `SessionLogServiceTests` `:538`, `:1230`, and integration `:386`, `:755` (their bodies change at S10, section 2).
- Serializable precedents: `SessionLogControllerReceiptTests` (`:106`) and `RequirementsRecoveryTests` (asserts `IsolationLevel.Serializable`, `tests/QBrainAi.Support.Mcp.Tests/Requirements/RequirementsRecoveryTests.cs:66`).
- Tombstone revive (the pattern IdempotentCreate needs): `SessionLogResubmissionReviveTests` (`:33`, `:56`).
- `HandoffDbExceptions.IsCommitAmbiguous`: `HandoffDbExceptionsTests`. `TodoPayloadFingerprint`: `TodoPayloadFingerprintTests`. `ApiKeyRedaction`: `ApiKeyRedactionTests` (3). `SessionLogSanitizer`: `SessionLogSanitizerTests` (10) and `SessionLogSanitizerProjectionTests` (5).
- `McpErrorClassifier`: `McpErrorClassifierTests` (6), `McpToolErrorEnvelopeTests` (3), `SessionLogControllerErrorTests` (8). REPL envelope: `ReplMcpErrorClassifierTests` (15).
- `WorkspaceIdentity`: 17 test files. `WorkspaceResolutionMiddleware` (TR-MCP-MT-002 body kept): `MultiTenantIntegrationTests`, `McpTransportMultiTenantTests`, and two more integration files. `McpInstanceResolver` (per-instance thresholds): `McpInstanceResolverTests`.
- Paging clamp: `SessionLogServiceTests` `:432`, `:450`.
- Turn context and attribution: `SessionLogTurnContextValidatorTests` (16), `SessionLogServiceTurnContextTests` (20), `SessionLogSessionAttrTests` (7).
- Storage budget: `SessionLogSubmitBudgetTests` (6), `SessionLogReplaceSectionRetryableTests` (2). Schema pending: `SessionLogSchemaGuardTests` (6).
- EF conventions every new entity inherits automatically: the required Restrict FK to Workspaces (`McpDbContext.cs:1272-1300`) and soft-delete columns and filter (`:1302-1331`). They are pinned by `DbFkContractTests.DBFK_WorkspaceIdEntities_HaveRequiredWorkspacesForeignKeys` (`tests/QBrainAi.Support.Mcp.Tests/Storage/DbFkContractTests.cs:38`), `DBFK_DurableEntities_ExposeSoftDeleteMetadata` (`:72`), and `DBFK_DurableRelationships_DoNotCascadeDelete` (`:95`). Inference: all 17 agent-context `*Entity` types and `SessionLogTurnReviewReasonEntity` get `IsDeleted`, `DeletedAtUtc`, `DeletedBy`, and `DeleteReason` columns and a `SoftDelete` filter. ARCH's entity rules (6.3 lines 2359-2366, 5.1 line 1831) do not mention them, and the migrations will carry them.
- Migration pattern: `AddSessionLogTurnPlanFileAndTodoIdMigrationTests` (3), `SessionLogTurnPlanFileTodoIdMigrationApplyTests` (3), and per-provider `SessionLogAgentSessionHeaderMigrationTests` (7, PostgreSQL and SQL Server at `:215-316`).
- Decorator restore: `TransactionGatedSessionLogServiceTests.ReplaceTurnAsync_WhenCommitFailsAfterMutation_RestoresPriorTurnGraph` (`:98`), the base for TR-MCP-SESSIONREVIEW-001 AC-004 transaction-restore graph paths.
- REPL passthrough: `GenericClientPassthroughValidClientNamesTests.InvokeAsync_EveryAdvertisedClientName_ResolvesThroughThePassthrough` (`tests/QBrainAi.Repl.Core.Tests/GenericClientPassthroughValidClientNamesTests.cs:69`) automatically checks the new `AGENTCONTEXT` allow-list entry. `GenericClientPassthroughTests` (46) covers binding by parameter name (`GenericClientPassthrough.cs:649`).
- Client source-gen round trips (pattern for new DTO registrations): `HandoffClientTests.cs:147`, `HostileReviewClientTests.cs:104`. Client typed exceptions: `ErrorHandlingTests.cs:17-57`, `McpClientExceptionCtorCompatTests.cs:22-46`.
- GraphRAG members ProjectAgentResponsesCommand reuses (`IGraphRagService.IngestTextAsync`, `DeleteDocumentAsync`, `GetStatusAsync`, `src/QBrainAi.GraphRag/Services/IGraphRagService.cs`): 12, 11, and 29 test files respectively (for example `GraphRagCqrsTests`, `GraphRagMcpToolTests`, `GraphRagControllerAdHocTests`).
- Requirement update and delete paths that dispatch EvaluateStalenessCommand: `RequirementsDatabaseDocumentService.cs:593,600,674,681,738,745` and `RequirementsDocumentService.cs:226,244,319,337,413,431`, covered by `RequirementsDocumentServiceTests` (13) among 22 requirement test files (153 tests).
- Marker plugin contracts: `MarkerFileServiceTests` (27).

## Untested reused code

No test names these, so new components that call them carry the risk:

- `SessionLogIdentifierValidator` (`src/QBrainAi.Services/Services/SessionLogIdentifierValidator.cs:8`, `internal`), which TurnLocator wraps. Covered only indirectly through invalid-id controller tests (`SessionLogControllerTests.cs:199,207,216`).
- `CanonicalSectionProjectionProvider` (`src/QBrainAi.Services/Options/CanonicalSectionProjectionProvider.cs`), which `AgentContextOptions` depends on (ARCH 6.2 line 2342). No test references it.
- `ApplyWorkspaceOverride` (`QBrainAiMcpTools.cs:156`), `ValidateWorkspaceAttribution`, `IsSupersededHookPersist` (private), and `McpToolErrors`: indirect coverage only.
- The submit session-level UNIQUE race retry (`SessionLogService.cs:234-253`): no test, and IdempotentCreate `MergeInto` replaces it.
- Client `PatchTurnAsync` (`src/QBrainAi.Client/SessionLogClient.cs:225`): no test anywhere. Client `CompleteTurnAsync`, `FailTurnAsync`, `ReplaceTurnAsync`, `ReplaceTurnSectionAsync`, `ClearTurnSectionAsync`, and `DeleteTurnItemAsync` (`:199-302`) have no client-level test (REPL and McpAgent mock them).
- MCP `sessionlog_submit` tool (`FwhMcpTools.SessionLog.cs:24-42`): no test.
- Controller 5000-turn cap (`SessionLogController.cs:19,66-67`): no test.
- `QuadBrainInternalToolExecutor` session-turn tools against a real service: substitute only.
- `ReplMcpErrorClassifier` for non-503 server codes and for `McpConflictException` with `retryable: true` (section 3).
- Re-write of a soft-deleted turn or nonce (section 5).
- `FederatedSessionLogService` merge of peer turns that lack the new fields: `FederatedSessionLogServiceTests` (10) cover develop fields only.

## Duplicate or conflicting planned tests

- Planned `SessionLogSanitizerReviewTests.Search_RedactedReviewValue_NoMatch_ReplacedReasonNoMatch` (ARCH line 232, PLAN:366) asserts the opposite principle to existing `SessionLogSanitizationControllerTests.S16_QueryTextFilter_SecretContainingRawRecordStillParticipates_AndPagingMetadataUnchanged` (integration `:95`). It also conflicts with ARCH's own QuerySessionLogsQuery rule (line 1941). Both can hold only with an explicit field-scoped rule.
- Planned `SessionTurnCompletionGateServiceTests.StandardAgent_CompleteWithoutClass_Rejected`, `LifecycleComplete_NoClass_Returns400`, `ClaudeCode_ClassAndReviewsNoEvidence_Accepted_NoClass_Rejected`, and `EvidenceGate_QbAgentOnly_StandardAgentNoEvidenceItems_CompletesWithClassAndReviews` (PLAN:428) assert the opposite of four live tests: `SessionLogServiceTests` `:538`, `:1261`, and integration `SessionLogControllerTests` `:420`, `:733`. PLAN changes TEST-MCP-SESSIONLOG-003 and TEST-SUPPORT-014 (PLAN:516-517) but does not list these four methods for rewrite.
- Planned `SingleTurnRoutes_TerminalTurnStatusCorrection_TodayBodies_NoOutcomeGuard` (PLAN:421) duplicates `SessionLogSupersessionTests.UpsertTurnAsync_ExplicitStatus_CanCorrectTerminalTurn` (`:125`) on the service.
- Planned `SessionLogSubmitOutcomeTests.InProgressOntoCanceledTurn_ReportsSkippedTerminal_ActionsRemainZero` (PLAN:421) repeats the guard behavior of `SessionLogSupersessionTests.SubmitAsync_StaleActiveSnapshot_PreservesTerminalTurn` (`:71`, row "canceled") and adds only the outcome.
- Planned forced-interleaving and retry cases (`TurnWrite_ForcedInterleaving_*`, `SerializableWrite_RetriesExhausted_409ConflictRetryable_NothingSaved`, ARCH lines 268, 1862) overlap `SessionLogSupersessionTests.SubmitAsync_ConcurrentCompletion_PreservesCompletedResponse` (`:216`). That test asserts the opposite surface (a raw `SqliteException` busy, `:253-254`) where the plan expects 409 `conflict` retryable.
- The SerializableWrite join rule (ARCH line 268, "joins the caller's transaction") versus the live caller-owned-transaction refusal tests (`SessionLogSupersessionTests` `:192`, `:204`; `SessionLogControllerReceiptTests` `:106`). No planned test states which wins.
- Planned TEST-SUPPORT-024 treats `SessionLogServiceReplaceDeleteTests` as baseline-green for the whole plan (PLAN:440, :550). Planned CompletionGate PUT cases (FR-MCP-SESSIONCLASS-002-AC004) make `:26` and `:61` red at S10 (section 2).
- Planned `TurnDto_ClassAndReviews_ServerAndClient` and `SessionLogDtoParityTests` (PLAN:432) conflict with ARCH's "no server copy and no parity test" for new DTOs (line 2586). Both hold only for `UnifiedRequestEntryDto`, the one type ARCH keeps twice.
- Planned `Status_AfterDependencyChange_CountsStaleNotTrusted_WritesNothing` (ARCH 6.5 line 2665, diagram line 2672: effective status) conflicts with ARCH 6.4's `AgentContextStatusResult.responsesByStatus`, which "counts the stored status, so the read a plugin makes at every host session start evaluates no dependency" (line 2603). This is internal to the new design, but the planned test and the DTO contract assert opposite counts.
- Planned per-feature classifier tests (`McpErrorClassifierReviewTests`; "McpErrorClassifier gate cases", PLAN:428, :433) conflict with ARCH's single `ClassifiedErrorException` branch (line 314).
- Planned `ReviewThresholds_OutsideZeroToOne_StartupRejects` (server, PLAN:371) and `ConfigValidatorReviewThresholdTests` (Build.Tests, PLAN:375) assert the same range in two places. This is by design (Nuke under TR only), not a conflict.

## Suites and gates by bounded context

Gate facts at baseline:

- G2 `./build.ps1 Test` runs every `*.Tests` project except names containing `IntegrationTests`, `PluginIntegration`, `Review.Tests` (this also drops `QBrainAi.PlanReview.Tests`, 1 test), and `Build.Tests`, with filter `Category!=AiReview&Category!=Integration` (`build/Build.Test.cs:16-24`, `:50`).
- `*.Validation` projects are excluded from G2 (`:20`).
- No Nuke target runs `*.IntegrationTests` or `*.Validation`. The only Nuke integration runners are `MigrationIntegrationTests` (Support.Mcp.Tests and Build.Tests with `Category=Integration`, `build/Build.TestDependencies.cs:70-74`) and `PluginSessionLogIntegration`.
- PLAN's G3 lists `tests/QBrainAi.Support.Mcp.IntegrationTests`, `tests/QBrainAi.SessionLog.Validation`, `tests/QBrainAi.Repl.IntegrationTests`, and `tests/QBrainAi.Acid.IntegrationTests` (PLAN:154), while its Nuke rule forbids raw `dotnet test` (PLAN:155), and TR-MCP-NUKE-011 `--test-projects` only narrows the projects `Test` already selects (PLAN:541). Inference: G3 has no runnable Nuke route for those four projects; this is a missing prerequisite.
- `QBrainAi.TransactionSecurity.IntegrationTests` (48) is in no gate list, although `TransactionGatedSessionLogService` depends on its coordinator.
- Validation projects are plain xUnit, with no `.feature` files.

Test counts per project at `a8d3934b` (`[Fact]` plus `[Theory]`): Support.Mcp.Tests 2735, Repl.Core.Tests 723, Client.Tests 305, Support.Mcp.IntegrationTests 279, Build.Tests 208, Repl.IntegrationTests 178, QBAgent.Tests 86, McpAgent.Tests 63, TransactionSecurity.IntegrationTests 48, ToolRegistry.Validation 38, PluginIntegration.Tests 37, Workspace.Validation 36, Acid.IntegrationTests 34, Cqrs.Tests 33, Todo.Validation 33, SessionLog.Validation 21, GitHub.Validation 15, Launcher.Tests 15, Context.Validation 9, Repo.Validation 8, Review.Tests 4, PlanReview.Tests 1. Total 4909. Pester under `plugins/core/test-fixtures`: 229 `It` blocks in 13 files. Four `.bats` files remain.

Per bounded context (ARCH 8.3):

- StorageSchema:
  - G2: `Support.Mcp.Tests/Storage` (155 tests, including `DbFkContractTests`, `McpDbContextMultiTenantTests` 10, `QueryIndexCoverageTests`, `EfNavigationCollectionContractTests`, and migration tests).
  - G3: the PostgreSQL and SQL Server migration classes tagged `Category=Integration` (12 classes in Support.Mcp.Tests; 108 tagged tests including Build.Tests) through `MigrationIntegrationTests`; Support.Mcp.IntegrationTests root (27, `ProviderIntegrationTestSupport`, `ScratchSqliteSchemaTests`).
- SharedComponents:
  - G2: `Cqrs.Tests` (33); Client.Tests (305) for the new `QBrainAi.Client.Errors`, `.Paging`, and `.Scoring` types; and in Support.Mcp.Tests `McpErrorClassifierTests`, `SessionLogSanitizerTests`, `ApiKeyRedactionTests`, `HandoffDbExceptionsTests`, `TodoPayloadFingerprintTests`, and `Middleware` (54).
  - Consumers that see the behaviors (G2): Memory-named files 78 with 390 tests, UseCase-named 15 with 80, Product-named 9 with 54.
  - G3: three-provider cases.
- SessionTurnLogging:
  - G2: Support.Mcp.Tests files named `*SessionLog*` (42 files, 314 tests), `McpStdio` (93), the session-log controller unit tests (13), `MarkerFileServiceTests` (27).
  - G3: Support.Mcp.IntegrationTests `*SessionLog*` (4 files, 50 tests); SessionLog.Validation (21; no Nuke target).
- TurnReviews:
  - G2: new classes in Support.Mcp.Tests and Client.Tests; existing `Options` (61, threshold validation neighbors), sanitizer tests, and the review section cases.
  - Infrastructure step: Build.Tests `ConfigValidatorReviewThresholdTests`.
- ContextPackages, ResponsesAndTrust, ContextGraph:
  - G2: new classes; touched suites are GraphRAG-named in Support.Mcp.Tests (13 files, 137), `GraphRagToolAdapterTests` (McpAgent, 17), `GraphRagClientTests`, requirement-named (22 files, 153) for the staleness dispatch, and the Repl.Core passthrough suites (54).
  - G3: the new agent-context controller and integration tests in Support.Mcp.IntegrationTests.
- ReplAndAgents:
  - G2: Repl.Core.Tests (723; session-log files 9 with 155), McpAgent.Tests (63), QBAgent.Tests (86, the `mcp_session_turn_*` tool users).
  - G3: Repl.IntegrationTests (178). They target `http://localhost:5177` with no server fixture in the project (`AuthKeyAndWorkspaceTests.cs:27`). Inference: they do not reach the real gate.
- PluginCorePowerShell: G4 Pester (229 `It` in 13 files); G3 `PluginSessionLogIntegration` (PluginIntegration.Tests, 37).
- BuildTooling: Build.Tests (208) in the direct infrastructure step. On the branch it lives at `build/Build.Tests` (section 5).
- HostPluginAdoption: G6 in the nine plugin repos (outside this repo).
- Documentation: G2 `Support.Mcp.Tests/Documentation` (24), where the docs contract tests would land.

## Analyzer branch interaction

- Branch state: `chore/netanalyzers-all` HEAD `fe5879ba` sits on top of `a8d3934b`.
  - Commits: `0a6453d7` IDE0130 and file-scoped namespaces; `6f883f0d` CA fixes batch 1 with CA1707 renames; `1de665e4` CA2007; `fe5879ba` moves Build.Tests.
  - 515 uncommitted paths, including the Meziantou.NET.Sdk switch (`global.json` `msbuild-sdks`, `src/Directory.Build.props`).
  - `src/Directory.Build.props` keeps `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` on, and `.editorconfig:157-159` sets IDE0130 to warning. So an IDE0130 or CA1707 finding fails the build there.
- CA1707 renames:
  - Commit `6f883f0d` removed every underscore from 4,468 test names in 650 files (message: "each underscore removed, the next character upper-cased"). Only 8 Windows-only methods keep underscores under `tests/`.
  - Samples: `UpsertTurnAsync_UniqueSessionLogTurnsRace_RetriesOnceAndUpdates` became `UpsertTurnAsyncUniqueSessionLogTurnsRaceRetriesOnceAndUpdates` (branch `SessionLogServiceTests.cs:1841`); `SubmitAsync_StaleCancellation_PreservesTerminalTurn` became `SubmitAsyncStaleCancellationPreservesTerminalTurn` (branch `SessionLogSupersessionTests.cs:50`); `Controller_GlobalCreate_EmptyWorkspace_ReturnsCreated` became `ControllerGlobalCreateEmptyWorkspaceReturnsCreated` (branch `:104`); `SessionLogCompleteTurn_DbUpdateException_ReturnsFourFieldEnvelopeWithInner` became `SessionLogCompleteTurnDbUpdateExceptionReturnsFourFieldEnvelopeWithInner` (branch `:48`).
  - Plan versus code: PLAN and ARCH cite 475 distinct underscore case names (176 in ARCH, 378 in PLAN; 31 are class-qualified, so certainly C#). New C# product test names in that form fail CA1707 once the branch's analyzer configuration lands. Pester `It` strings and Build.Tests are unaffected: Build.Tests moved out of analyzer scope and keeps 211 underscore names on the branch.
  - 13 existing C# tests that PLAN or ARCH cite by underscore name are renamed on the branch, for example `SessionLogServiceReplaceDeleteTests.ReplaceTurnAsync_ClearsOmittedScalarsAndCollections` to `ReplaceTurnAsyncClearsOmittedScalarsAndCollections`, `PluginHostProcessAdapterTests.Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr` to `AdapterCapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr`, and `HostileReviewWorker_InvalidOptions_FailStartup` to `HostileReviewWorkerInvalidOptionsFailStartup`.
  - Store versus code: the branch updated the generated projections in `docs/Project` (sample `AcidRuntime_ExposesNoQuadBrainSurface`: 3 files at baseline, 0 on the branch, the new name in 3), but not the store. The store still cites 29 (listFr), 9 (listTr), and 92 (listTest) distinct pre-rename C# test names. Inference: after the merge the store evidence names nonexistent methods, the next projection regenerated from the store reintroduces old names, and P0's new records written with underscore names repeat the problem.
- IDE0130 versus ARCH namespaces (conflict). IDE0130 expects RootNamespace plus folder path, and the branch says so explicitly at `.editorconfig:157`. ARCH 3.5 says a new type's namespace is "its assembly name followed by its folder path" and calls that IDE0130 (line 241). The two disagree for three projects:
  - `QBrainAi.Services` keeps RootNamespace `QBrainAi.Support.Mcp` at baseline and on the branch (`src/QBrainAi.Services/QBrainAi.Services.csproj:6`).
  - `QBrainAi.Storage` is `QBrainAi.Support.Mcp` at baseline and `QBrainAi.Support.Mcp.Storage` on the branch (`src/QBrainAi.Storage/QBrainAi.Storage.csproj:6`).
  - `QBrainAi.Support.Mcp` is `QBrainAi.Support.Mcp` (`:9`).
  - So IDE0130 on the branch expects `QBrainAi.Support.Mcp.SessionLog.Commands`, not ARCH's `QBrainAi.Services.SessionLog.Commands`. Likewise `QBrainAi.Support.Mcp.Workspaces`, `.AgentContext`, `.Redaction`, `.Hashing`, `.Scoring`, `.Reviews(.Commands)`, `.SessionLog(.Queries)`, and `.AgentContext.Packages`, `.Responses`, `.Graph`, `.Sources` (with `.Commands` and `.Queries`); and `QBrainAi.Support.Mcp.Storage.Transactions`, `.Errors`, `.Idempotency`, `.Keys`, `.Paging`, `.Entities.AgentContext`, not `QBrainAi.Storage.*`.
  - Every server-side namespace in ARCH 3.5.1-3.5.5, 4.1, 5.2, 6.2, 6.3, 6.5, and 8.3 is affected. Client, Cqrs, Repl.Core, and McpAgent are unaffected because their RootNamespace equals the assembly name (`QBrainAi.Client.Errors`, `.Paging`, `.Scoring`, `.Models.SessionLog`, `.Models.AgentContext` match).
  - Round 5 flagged root namespace versus assembly (`scratchpad/review-arch-r5/dry-c.md:253`), but the adopted rule does not match the analyzer's.
  - Inference: either the two RootNamespaces change (which re-flags every existing file the branch just moved to `QBrainAi.Support.Mcp.*`) or ARCH's namespaces change.
- Existing namespaces moved on the branch; ARCH cites the develop names, which it says is context:
  - `WorkspaceContext`: `QBrainAi.Support.Mcp.Services` to `QBrainAi.Support.Mcp.Storage.Services`
  - `RememberMemoryCommand`: `QBrainAi.Support.Mcp.Services` to `QBrainAi.Support.Mcp.Memory.Commands`
  - `IGraphRagService`: `QBrainAi.Support.Mcp.Services` to `QBrainAi.GraphRag.Services`
  - Inference: Phase 2 stubs written against develop's `using` lines will not compile after the branch merges, so the merge order matters.
- Build.Tests path. The branch moved `tests/Build.Tests` to `build/Build.Tests` (`fe5879ba`) and repointed `MigrationIntegrationTests` (it did not remove Build.Tests from the list, so P0.1c is still open). PLAN cites `tests/Build.Tests` 21 times on 10 lines (`:47,127,153,177,520,544,623,633,643,647`) and ARCH once (line 4772). The infrastructure-step command `dotnet test tests/Build.Tests/Build.Tests.csproj` (PLAN:153, :633) points at a path that no longer exists on the branch.
- CA2007 on the branch (`1de665e4`) puts `ConfigureAwait` on every await. ARCH code samples do not matter here, but new Phase 2 stubs must comply. Inference.

## Summary for the coordinator

- Biggest projected break: the S10 completion gate. About 196 existing tests write `completed` or `failed` turns without class or reviews; at least 120 of them reach the real server path. Eight tests assert the opposite outcome or a body the gate now preempts (`SessionLogServiceTests:538,1230,1261`; integration `SessionLogControllerTests:386,420,701,733,777`). The QBAgent `mcp_session_turn_complete` tool (`QuadBrainInternalToolExecutor.cs:314-328`) and REPL `importRecovery` have no way to supply class or reviews.
- PLAN relies on baseline-green `SessionLogServiceReplaceDeleteTests:26,61` "at every later gate", but both PUT `completed` and go red under the gated PUT.
- ARCH omits develop rules that live tests pin:
  - Submit refuses caller-owned transactions (`SessionLogService.cs:119-128`; 4 tests).
  - The transaction is opened only on relational providers (`:155`); 151 tests run the real service on EF InMemory.
  - The SQL 1205 contract (`SessionLogDenseGraphTests:100`) and the busy-exception race test (`SessionLogSupersessionTests:216`) also change.
- The REPL envelope cannot carry the new error contract: `ReplMcpErrorClassifier.cs:60-95` keeps server codes only for 503 and forces `conflict` to `retryable: false`.
- WorkspaceResolutionBehavior conflicts with Global memory (`MemoryController.cs:125`; `MemoryGlobalDefaultWorkspaceTests:100`). It cannot reach a request-scoped `WorkspaceContext` through the singleton dispatcher, and "unresolved" is undefined.
- IdempotentCreate ignores tombstones; the unique indexes are unfiltered (`McpDbContext.cs:391,404`).
- The MCP section tools bypass the review units.
- IDE0130 uses RootNamespace (`QBrainAi.Support.Mcp` for Services and Storage), so every server-side namespace ARCH names fails the branch analyzer.
- CA1707: 475 planned underscore names; the store still cites 130 pre-rename names. Build.Tests moved to `build/Build.Tests`, against 22 plan and architecture citations.
- G3 has no Nuke runner for IntegrationTests or Validation.
