# R12-F2 sibling sweep report (before hostile validation round 13)

The final report of the read-only R12-F2 sibling sweep subagent (session ClaudeCode-20261102T182906Z-plugin-session, request req-20261105T084458Z-r12f2-clock-sweep), returned 2026-11-05T03:11:11 local and exported verbatim from line 111132 of the session transcript by Export-SubagentReport.ps1 on 2026-11-05. The harness refused the subagent's own report file, so the transcript holds the only full copy. Slice Z4 (commit 4398cffc) fixed the siblings it lists; its clock type is SettableTimeProvider, not the ScriptedTimeProvider the report names.

# R12-F2 sibling sweep: live-clock age, expiry, staleness and lease decisions in unit tests

This was a read-only sweep. Nothing tracked was changed. Session ClaudeCode-20261102T182906Z-plugin-session, request req-20261105T084458Z-r12f2-clock-sweep. The turn was opened, updated with 6 actions and 3 decisions, and completed through the plugin scripts; all readbacks were OK.

The harness refused to write report-r12f2.md ("Subagents should return findings as text, not write report files"), so the full report is below. The completed turn holds only a short summary, written from sweep-r12f2/response-r12f2.txt.

## Scope and method
- **Unit scope** (from docs/receipts/hv/unitgreen-r12-trx-52e6d627/manifest.json): Client, Cqrs, Launcher, McpAgent, QBAgent, Repl.Core and Support.Mcp `.Tests`, minus Category=Integration cases. Traits were checked on both the method and the class.
- PluginIntegration.Tests and Review.Tests are excluded by build/Build.Test.cs and were not classified.
- **Production side first.** I listed every src line that reads the live clock (423), every clock seam (107), every time comparison (168) and every age-vocabulary line (399). I then read each decision in full and searched the unit tests for every caller.
- **Scripts:** sweep-r12f2/Search-ClockSites.ps1 and sweep-r12f2/Get-HitContext.ps1, both PSScriptAnalyzer-clean. The raw lists are the *.txt files in that folder.

## Counts
- SIBLING: 37 test methods (the 4 named cases plus 33 new)
- UNCLEAR: 1
- RECORDED-TIME: 10 (2 not scripted, 1 clock-independent, 7 already scripted)
- HANG-GUARD: 3
- INTEGRATION: 28 entries
- INERT: 44 entries, grouped by file

## Live-clock decisions that unit tests reach
- **D1** SessionLogService.cs:566-572 and :599-611.
  - The cutoff is DateTimeOffset.UtcNow.AddHours(-N); a turn matches when its Timestamp is <= the cutoff.
  - No clock seam (constructors at :47-60 and :72-77).
- **D2** GraphRagService.cs:151 (InitializeAsync), :177 (IndexAsync), :967-997.
  - A static cleanup deletes *.tmp and *.partial files when UtcNow minus the last-write time is more than 2 h.
  - The service already holds `_timeProvider` (:76, :88), but the cleanup ignores it.
- **D3** WorkspaceValidationService.cs:44 (default 48 h, allowed range 0 to 168) and :258-272.
  - A finding is raised when Timestamp < `_time.GetUtcNow()` minus the threshold.
  - Seam exists: `TimeProvider? time = null` (:17-21).
- **D4** FederationEnvelopeSigner.cs:47-55 and :85-89.
  - Sign stamps UtcNow plus the TTL.
  - Verify rejects as expired when ExpiresAtUtc < UtcNow, and as future-issued when IssuedAtUtc > UtcNow + 5 min. Both checks run before the target, body-hash and signature checks.
  - Reached through FederationController.cs:197-199 and FederationFanoutSyncService.cs:116-125.
  - No seam.
- **D5** TransactionSecurityServices.cs:335-338 and :444-459 (InMemoryKeyServerService).
  - Sign uses the request's times or UtcNow plus ManifestTtlSeconds.
  - Verify returns ExpiredManifest when ExpiresAtUtc < UtcNow, and FutureManifest when IssuedAtUtc > UtcNow + skew. Both checks run before the party, key, signature, nonce and sequence checks.
  - The subscriber's CommitDiffgramAsync calls Verify at :872-887. That comes after the Base64 DecryptFailed check (:815) and the existing-record path (:825-834), and before the hash checks (:889-905) and the RequireEncryptedDiffgrams DecryptFailed check (:923-931).
  - No seam on the verify side. Sign requests already accept scripted IssuedAtUtc and ExpiresAtUtc.
- **D6** HandoffIngestionService.cs:667-686, :704-746 and :331-358.
  - Processing and approval leases of 30 s (HandoffProcessing.cs:63).
  - A lease is live when expires >= now; it is taken over when expires < now.
  - Seam exists: optional TimeProvider (:45, :57).
- **D7** MemoryConsolidateOperations.Locks.cs:55-79.
  - With forceOverlap, a held lock is taken over when UtcNow minus AcquiredAtUtc is more than LockTtl, which is 30 s (MemoryConsolidateOperations.cs:13).
  - Static method over a static map; no seam.

## Live-clock decisions that no unit test reaches
- **No unit callers:**
  - DurableTransactionPubSub claim lease and retention (TransactionPubSubServices.cs:1056-1057, TransactionSecurityStateStores.cs:574-589 and :1074-1140). Only the held-out TransactionSecurity.IntegrationTests reach them.
  - PairingSessionService.cs:19-34.
  - ApiKeyIssuanceGuard.cs:14-28.
  - LoginHandler.cs:53-57 and :592-594.
- **Reached, but the decision never fires in the unit case:**
  - Replay-worker cutoff (TransactionPubSubReplayWorker.cs:71): the unit test matches it with Arg.Any.
  - TurnTransactionsController default cutoff (:71): the unit test passes an explicit value.
  - VoiceConversationService.cs:465-480 and :1851-1864: no unit test calls FindSessionByDevice, and the cleanup timer first fires at 60 s.
  - GitHubCliService.cs:838: the unit case stores a null expiry.
  - Verified-marker cache (MarkerFileClientOptionsResolver.cs:585-590 and :670-677): the unit tests inject a recording file system.
  - Tunnel startup deadlines: only the Integration cases reach them.
- **Already scripted:**
  - TriageService uses a required TimeProvider, which TriageServiceTests script.
  - The one fixture that passes TimeProvider.System (TriageServiceCrossPlatformContractTests.cs:167) only runs submit, get and query, which stamp times but make no time decision.
  - PairingLoginAttemptGuard is scripted in its own tests.

## SIBLING (37)

### D1: SessionLogService stale filter
- **S1** SessionLogTriageStoreTests.cs:171 QueryAsync_TurnStatusInProgressAndStaleOlderThanHours_ReturnsOnlyStaleOpenTurns (named).
  - Seeds now-30h and now-1h at :173-175; threshold 24 h, so the margins are 6 h past and 23 h short.
  - **Seam:** add an optional TimeProvider (System default), read once at :570. The internal constructor at :72-77 already exists for a test-owned normalizer.
  - **Boundary rows** (the timestamp is a second-precision string, so use whole seconds):
    - reference-24h-1s: listed
    - reference-24h exactly: listed (only Timestamp > cutoff is excluded)
    - reference-24h+1s: not listed
    - null Timestamp: not listed
    - status mismatch: not listed
    - StaleOlderThanHours 0 or null: no age filter

### D2: GraphRAG temporary-artifact cleanup
- **S2** GraphRagServiceTests.cs:191 InitializeAsync_RemovesStaleTemporaryArtifacts (named).
  - Sets the file time to UtcNow-3h at :198; threshold 2 h, margin 1 h.
  - **Seam:** make CleanupStaleArtifacts and TryDeleteIfStale instance methods, or pass `_timeProvider.GetUtcNow()` in. The test then scripts the file's last-write time.
  - **Boundary rows** (whole seconds):
    - age 2h-1s: kept
    - age exactly 2h: kept (the check is strictly greater)
    - age 2h+1s: deleted
    - Cover *.tmp and *.partial, in the root, output and cache folders, through both InitializeAsync and IndexAsync.

### D3: Workspace validation stale turn
- **S3** WorkspaceHygieneG5OverlayTests.cs:224 Rule_InProgressTurn_OlderThan48h_UtcClock (named). Seed -49h at :226; threshold 48 h, margin 1 h.
- **S4** WorkspaceHygieneG5OverlayTests.cs:245 Rule_StaleThresholdOverride_BoundedAuthenticatedRecorded (named). Seed -10h at :247; threshold 8 h, margin 2 h. The 200 h out-of-range branch does not depend on the clock.
- **Seam:** already present, so only the test changes. CreateService at :408-409 passes TimeProvider.System; pass TestSupport/ScriptedTimeProvider.cs instead and seed turns relative to its reference time.
- **Boundary rows:**
  - reference-threshold-1tick: finding
  - reference-threshold exactly: no finding (the check is strict <)
  - reference-threshold+1tick: no finding
  - null Timestamp or a status other than in_progress: no finding
  - Thresholds 0, 168 and above 168 do not depend on the clock.
  - Use whole seconds if the SQLite converter truncates.

### D4: Federation envelope expiry (TTL 300 s)
A valid outcome needs less than 300 s between Sign and Verify, and no backward clock step of more than 5 min. Because the expiry check runs first, any other expected rejection turns into "Envelope has expired." once the margin passes.

- **S5** FederationEnvelopeSignerTests.cs:13 Sign_ProducesVerifiableEnvelope
- **S6** :35 Verify_RejectsBodyTampering (expects "body hash")
- **S7** :55 Verify_RejectsMalformedBodyBase64 (expects "base64")
- **S8** :75 Verify_RejectsWrongTarget (expects "target proxy")
- **S9** :93 Verify_RejectsExpiredEnvelope. ExpiresAtUtc is set to UtcNow-1s at :102, so a backward clock step of more than 1 s flips it.
- **S10** FederationFanoutSyncServiceTests.cs:16 SyncOnceAsync_VerifiesEnvelopeAppliesAndAcknowledgesRecipientSequence
- **S11** :114 SyncOnceAsync_LocalExecutionEnvelopeExecutesAndAcknowledges
- **S12** :160 SyncOnceAsync_LocalExecutionDisabledRejectsEnvelope (expects "conflict", which only happens after Verify passes)
- **S13** FederationControllerTests.cs:119 RecordEnvelope_AppliesAcceptedOperation (read in full)
- **S14** :169 RecordEnvelope_AppliesExistingAcceptedOperation
- **S15** :208 RecordEnvelope_WaitsForTransactionalApplyBeforeAcknowledgingApplied
- **S16** :265 RecordEnvelope_WhenTransactionalApplyReportsDegraded_AcknowledgesConflict
- **S17** :645 RecordEnvelope_WhenTransactionsRequired_StillReachesTransactionalApply
  - S14 to S17 were read by outline only: each signs, calls RecordEnvelope, and asserts an outcome past the BadRequest gate at :197-199.
- **S18** MemoryFederationStateAdapterTests.cs:237 MemoryFederation_SignedEnvelopeAppliesMemoryOperation
- **S19** :261 MemoryFederation_StaleBaseVersionCreatesConflictAndDoesNotOverwrite
- **S20** :305 MemoryFederation_OperationCreatesFanoutAndRecipientApplyWorks. The signer comes from DI (:431).

- **Seam:** add an optional TimeProvider constructor parameter (System default), read once in Sign and once in Verify. DI still works because a defaulted optional parameter is honored when TimeProvider is not registered.
- **Boundary rows:**
  - ExpiresAtUtc reference-1tick: expired
  - ExpiresAtUtc exactly reference: valid (the check is strict <)
  - ExpiresAtUtc reference+1tick: valid
  - IssuedAtUtc reference+5min: valid; +5min+1tick: future
  - Sign at a scripted instant gives exact IssuedAtUtc and ExpiresAtUtc = instant + TTL. Verify at that expiry is valid; one tick later is expired.
  - A TTL below 1 is clamped to 1 s (:48).

### D5: Transaction manifest expiry (TTL 300 s, skew 300 s)
These tests use CreateServices (TransactionSecurityControllerTests.cs:512-523, options at :516) and CreateSignRequest (:548-559), which sets no times, so everything runs on the live clock.

- **S21** TransactionSecurityControllerTests.cs:46 SignManifest_ThenVerifyManifest_ReturnsValidManifestHash
- **S22** :176 VerifyManifest_WithTamperedHash_ReturnsBadRequest (expects ManifestSignatureMismatch)
- **S23** :196 VerifyManifest_WithReplayNonce_ReturnsBadRequest
- **S24** :223 VerifyManifest_WithStaleSequence_ReturnsBadRequest
- **S25** :275 VerifyManifest_WithDisabledPublisher_ReturnsDisabledParty
- **S26** :298 VerifyManifest_WithUnknownSubscriberEncryptionKey_ReturnsUnknownKey
- **S27** :319 CommitDiffgram_WithValidManifest_CommitsAndStatusReturnsCommitted
- **S28** :343 CommitDiffgram_WhileVerificationInFlight_StatusReturnsPendingThenCommitted
- **S29** :392 CommitDiffgram_WithDuplicatePayload_ReturnsCommitted
- **S30** :410 CommitDiffgram_WithEncryptedHashMismatch_ReturnsBadRequest
- **S31** :429 CommitDiffgram_WithTamperedEncryptedBody_ReturnsBadRequest
- **S32** :448 CommitDiffgram_WithPlaceholderBodyWhenEncryptionRequired_ReturnsBadRequest. DecryptFailed comes from :923-931, after Verify.
- **S33** TurnTransactionCoordinatorTests.cs:142 ExecuteAsync_WhenProtectDiffgramsEnabled_SendsProtectedEnvelopeToSubscriber. Uses the real key server (:146) and real subscriber (:157), and expects "committed".
- **S34** BrainSlotAdoptedPartyKeySigningTests.cs:54 RenamedPartyAfterReconciliation_ReportsReadyAndSignsTurnTransaction. SigningFixture builds the chain at :210, :224 and :228-232.
- **S35** :126 PartyWithNoAdoptedKeyRow_SelfHealsAndCommitsTurnTransaction

- **Seam:** add an optional TimeProvider to InMemoryKeyServerService (System default), read at :335 and :444. The subscriber needs no change.
- **Boundary rows:**
  - ExpiresAtUtc reference-1tick: ExpiredManifest
  - ExpiresAtUtc exactly reference: valid
  - ExpiresAtUtc reference+1tick: valid
  - IssuedAtUtc reference+skew: valid; +skew+1tick: FutureManifest
  - Skew 0, and negative skew (clamped at :445)
  - Sign with no times gives expiry = instant + TTL exactly.
  - Repeat the same rows through CommitDiffgramAsync.

### D6: Handoff processing lease (30 s)
- **S36** HandoffAuditDefectTests.cs:63 IngestAsync_ConcurrentSameHash_ReservesOnce.
  - CreateService (:242-283) passes no clock, so it runs on the system clock.
  - The second ingest must see a live lease (:704-746). A forward clock step or a scheduling gap of more than 30 s lets it take the lease over and extract again, which fails Received(1) at :96.
  - The Task.Delay(50) at :69 only keeps the first run in progress; it is not a hang guard.
  - **Seam:** already present, so only the test changes. Use ManualTimeProvider, as HandoffIngestionServiceTests.cs:27 does, or AdjustableTimeProvider, as HandoffDurabilityTests.cs:33 does. HandoffDurabilityTests already has scripted lease rows at :161, :220, :346, :402 and :481.
  - **Boundary rows:**
    - expiry-1tick: InProgress
    - exactly at expiry: InProgress (the check is >=)
    - expiry+1tick: takeover
    - same owner: not InProgress
    - terminal run: Replayed

### D7: Consolidate job lock TTL (30 s)
- **S37** MemoryConsolidateJobTests.cs:18 PerWorkspaceLock.
  - Expects one success and one lock-held result. A gap of more than 30 s lets the forced job take the lock over, so both succeed.
  - It also depends on scheduling regardless of the clock, and the lock map is static per workspace.
  - **Seam:** pass a TimeProvider into MemoryConsolidateOperations (supplied by RunConsolidateJobCommandHandler, System default) and on to TryAcquireLock.
  - **Boundary rows:**
    - 30s-1tick: LockHeld
    - exactly 30s: LockHeld (the check is strict >)
    - 30s+1tick: takeover
    - forceOverlap false: LockHeld at any age
    - lock not held: success

## UNCLEAR (1)
- **U1** HandoffAuditDefectTests.cs:204 ApproveAsync_SeparateContexts_CreatedWins.
  - Two concurrent approvals on the system clock reach the 30 s approval lease at :331-358.
  - The assertions at :235-239 look true on both branches. I did not trace the create path past :366, so I could not confirm that a takeover still ends with ReviewState Created and no result that is both PendingReview and Created.
  - The same scripted-provider fix as S36 removes the question.

## RECORDED-TIME (10)
- **R1** TodoExecutionServiceSqliteTests.cs:125-126. The test seeds two timestamps from two separate live clock reads (-10 min, then now) and asserts their order through TodoExecutionService.cs:1457. Not scripted; a backward step of more than 10 min reverses it.
- **R2** TransactionSecurityControllerTests.cs:103 GetManifestReport_AfterSign. The order comes from the production live CreatedAtUtc, then Sequence (TransactionSecurityStateStores.cs:164-166 and :753-755). Not scripted.
- **R3** OpenCodeSqliteTranscriptTests.cs:47/:75. Compares a file's last-write time with its own earlier reading, so it is exact and does not depend on the clock.
- **R4** AuthRotationTests.cs:217/:233. Already scripted (fixed RotationTime).
- **R5** StubAuthRotationHandlerTests.cs:19/:249, SessionLogTestHelpers.cs:20-41, SessionLogWorkflowIntegration2Tests.cs:445/:468, SessionLogWorkflowMockValidationTests.cs:389. ScriptedClock.
- **R6** GraphRagEntityCrudTests.cs:76 and GraphRagEntityStorageTests.cs:38. Scripted through StampedTimeContractTests.
- **R7** TriageServiceTests.cs:27 (ManualTimeProvider), with exact deadline asserts and the stale-run case at :838. Scripted.
- **R8** HandoffIngestionServiceTests.cs:27 and HandoffDurabilityTests.cs:33. Scripted, including the lease decisions.
- **R9** PairingLoginAttemptGuardTests.cs:21/:44/:67. Scripted Func.
- **R10** McpAgent and QBAgent FixedTimeProvider fixtures: GraphRagToolAdapterTests:494, HostedAgentQuadBrainAbsenceTests:154, HostedAgentWorkflowIntegrationTests:605, McpHostedAgentAdapterTests:760, McpSessionIdentifierFactoryTests:133, SessionLogWorkflowTests:359, QBAgentRunLoopTests:241, QBAgentSessionLogTests:88.

## HANG-GUARD (3)
- **H1** RepositoryEvidenceWindowsProcess.cs:268 WaitForEmptyJobAsync. A Stopwatch bound whose only effect is a TimeoutException (:96-106).
- **H2** TurnTransactionCoordinatorTests.cs:475. A 100 s substitute delay against a 1 s commit timeout; the remarks at :473 already document it.
- **H3** :501. Same pattern; remarks at :499.

## INTEGRATION (28)
- **I1** SessionLogTriageStoreTests.cs:136 (method trait :134)
- **I2** RequirementsRecoveryTests.cs:486 (:477)
- **I3** RequirementsDocumentServiceTests.cs:95, :192 (:68, :103)
- **I4** SessionLogDenseGraphTests.cs:81 (:70)
- **I5** SessionLogReplaceSectionRetryableTests.cs:157 (:123)
- **I6** SessionLogSubmitBudgetTests.cs:117 (:100)
- **I7** StorageCommandBudgetTests.cs:20, :55 (:16, :51)
- **I8** TriageServiceTests.cs:56 (:50)
- **I9** SqliteBoundedOpenFourteenthTests.cs:136, :226 (:113, :201)
- **I10** WorkspaceIdentityFourteenthNativeTests.cs:69, :100, :256 (:61, :96, :235)
- **I11** UseCaseGetWorkspaceResolutionContractTests.cs:105 (:70)
- **I12** UseCaseComponentUpdateContractTests.cs:73, :94 (:71, :92). AssertAggregateInvariantsAsync at :771 compares the production live UpdatedAtUtc with a UtcNow-1h seed (:321). That is an unscripted check of the R10-F3 kind, but its only caller is this Integration case.
- **I13** TunnelProviderTests.cs:74, :99, :165, :187 (:70, :92, :161, :183)
- **I14** FileGitHubWorkspaceTokenStoreTests.cs:41 (class trait :15)
- **I15** TranscriptMcpStdioHostTests.cs:117 (class :14)
- **I16** RequirementsFourteenthLinuxNativeTests.cs:34 (class :16)
- **I17** RequirementsWikiExportConfigTests.cs:195/:207 (class :14)
- **I18** BugTriage139FourteenthHarnessTests.cs:138 (class)
- **I19** BugTriage139SixteenthWindowsProcessTests.cs:171 (class)
- **I20** BugTriage139EighteenthTraversalTests.cs:32 (class)
- **I21** BugTriage139EighteenthLinuxTests.cs:322 to :806 (class)
- **I22** BugTriage139EighteenthSqliteTests.cs:45 (method)
- **I23** BugTriage139SixteenthIdentityWindowsTests.cs:42 (class)
- **I24** BugTriage139SixteenthLinuxTests.cs:38 to :907 (class :13)
- **I25** BugTriage139SixteenthPostgreSqlTests.cs:293 (BoundaryTests class)
- **I26** BugTriage139SixteenthSqliteConsumerTests.cs:37 (method)
- **I27** BugTriage139FifteenthNativeTests.cs:278/:450/:624, helpers whose callers are all Integration: FifteenthLinuxNative, Shared and WindowsNative classes
- **I28** WorkspaceHygieneG5OverlayTests.cs:293, :550 (:291, :548)

## INERT (44)
- **N1** Client RequirementTimestampJsonContractTests.cs:53. The "read time" fault row is rejected unless the live clock equals the scripted instant.
- **N2** Client SessionLogClientTests.cs:83. Only checks the query string.
- **N3** Repl.Core AuthRotationTests.cs:126/:272. The mock time is copied through.
- **N4** Iteration1_IntegrationTests.cs:100/:133/:315/:408/:421-422. Has no Integration trait, so it is a unit test. Mock times are copied through, and Repl.Core makes no age decision on them.
- **N5** MarkerFileTrustTests:222, MockTrustBootstrapServiceTests:288/:319/:331, OrchestrationRulesTests:340/:353-354, StubMarkerFileReaderTests:248/:257/:280/:303, WorkspaceSelectionTests:295. Same reason as N4.
- **N6** RequirementsWorkflowTests.cs:942, :1067 to :1377. Mock strings copied through.
- **N7** RequirementTimestampAdapterContractTests.cs:49. Fault row, as N1.
- **N8** SessionLogDeprecationTests:218/:300, SessionLogPersistenceStrategyTests:139, SessionLogWorkflowProductionTests:27/:156/:407-408/:584-585. TimeProvider.System into Repl.Core code that only stamps (SessionLogPersistence.cs:214).
- **N9** SessionLogWorkflowTests.cs:990 to :1033. Mocks only.
- **N10** TodoWorkflowTests.cs:1134 to :1251, :1395, :1412. IsStale is RepairRequired (TodoWorkflow.cs:761), not a time check.
- **N11** MarkerIsolationContractTests:126-140, MarkerWalkContract:121-125, MarkerIsolationBindingTests:18. Injected cache doubles.
- **N12** ContextControllerTransactionGateTests:179, MemoryControllerTests:31-132, RequirementsControllerGenerateTests:70/:79. DTO stamps.
- **N13** TriageControllerTests.cs:30 to :357. Substituted service.
- **N14** TurnTransactionsControllerTests.cs:17/:45/:68. The cutoff is passed and matched by equality.
- **N15** TransactionPubSubReplayWorkerTests.cs:27-52. Arg.Any.
- **N16** SyncStatusStoreMultiTenantTests.cs:25-75. Round trip.
- **N17** Memory MemoryIndexerTests:225, MemoryIsolationTests:64-83, MemoryMigrationTests:49, MemoryPromoteTests:256-267, MemoryRecallTests:505, MemoryS1PortMockTests:61, MemorySessionLogTests:29-39, MemoryTxnTests:26. Stamps only; the memory services make no recency or decay decision.
- **N18** Products GetProductEffectiveRequirementsQueryHandlerTests:296-355, ProductRequirementContextSurfaceTests:364-371, ProductRequirementContextTests:158-165. Seeds.
- **N19** RequirementStoredTimestampContractTests.cs:76. Fault row.
- **N20** StampedTimeContractTests.cs:120. "system clock" fault row.
- **N21** FederationFanoutSyncServiceTests.cs:55/:88. Rejected for target mismatch or a missing envelope whatever the clock says.
- **N22** FederationControllerTests.cs:317. Sign only.
- **N23** FederationQueuedOperationReplayServiceTests.cs:20 to :171. Sign and send; never verified.
- **N24** FederationLocalProxyEnrollmentServiceTests:53-120, FederationTopologyServiceTests:90. Neither service compares times.
- **N25** TransactionSecurityControllerTests.cs:22/:78/:138/:157/:255/:471/:493. Registration, sign-time failures, abort, and the existing-record path, which runs before Verify.
- **N26** TurnTransactionCoordinatorTests.cs:21/:39/:67/:92/:200/:231/:258/:305/:332/:354/:384/:435/:569 and CreateManifest :656-677. Bypassed, or a substituted key server or pub/sub; nothing is verified.
- **N27** BrainSlotAdoptedPartyKeySigningTests.cs:82 (direct ECDsa check), :151, :278.
- **N28** BrainSlotPartyKeyReconcilerTests, BrainSlotRegistryServiceTests, BrainSlotStartupSeederTests, QuadBrainLiveOrchestrationTests, BrainSlotInvocationTransactionTests, QuadBrainOrchestrationServiceTests. No Verify call, or a fake coordinator.
- **N29** TransactionPubSubTests.cs:31/:57/:215. Substituted subscriber.
- **N30** TriageServiceCrossPlatformContractTests.cs:167. The path only stamps (TriageService.cs:107-222, :226-268).
- **N31** ConfiguredTriageResearchRunnerTests and ConfiguredTriageResearchRunnerFallbackTests. Values passed through.
- **N32** HandoffAuditDefectTests.cs:101/:125/:152/:168, HandoffWorkspacePathsTests.cs:40. Only one lease holder.
- **N33** HandoffBoundedSourceTests:66-124, HandoffDurabilityTests:303/:1062/:1065, HandoffIngestionServiceTests:439. Stamps.
- **N34** MemoryConsolidateJobTests.cs:30/:43/:56/:77/:93/:106/:118 and MemoryConsolidateTests. The TTL branch is never taken. LockExpiry_Recovers at :106 never holds a lock, so it does not exercise the TTL recovery its acceptance criterion names.
- **N35** VoiceConversationServiceTests.cs:268. The timeout is configured, but the decision is never reached.
- **N36** TunnelProviderTests unit cases, NgrokTunnelProviderEnvironmentTests, FrpTunnelProviderTests. These end before the deadline loops.
- **N37** GitHubCliServiceTests.cs:411, TransactionGatedGitHubWorkspaceTokenStoreTests.cs:44. Null expiry.
- **N38** GraphRagDocumentManagementTests.cs:149 (one clock read with fixed offsets), GraphRagEntityStorageTests:78-195, GraphRagRelationshipStorageTests:44-187, and the other GraphRagServiceTests cases.
- **N39** SessionLogTriageStoreTests.cs:46/:75/:94/:115/:208. No stale filter used.
- **N40** The other WorkspaceHygieneG5OverlayTests unit cases, including SeedTriage :526/:541. No turn is seeded, and the triage rules are status-based.
- **N41** ProcessPolicySnapshotReaderContractTests.cs:124-141. "STALE" there is based on digest, version and scope (ProcessPolicySnapshotReader.cs:42-58).
- **N42** MemoryFederationStateAdapterTests.cs:397-398 and :30 to :197. Conflicts are version-based.
- **N43** Services stamp seeds: AgentPoolSeedServiceTests, AgentServiceRuntimeTests, BrainSlotChatClientFactoryTests, BrainSlotLlmStrategyTests, Fts5SearchServiceTests, HybridSearchServiceTests, InteractionLogSubmissionChannelTests, MarkerFileServiceTests, RequirementAcceptanceCriteriaTests, RequirementsDatabaseDocumentServiceTests, SessionLogResubmissionReviveTests:160, SessionLogServiceAgentLinkTests, TodoExecutionServiceTests, ToolBucketServiceTests, ToolRegistryScopeTests, TransactionGated Memory/RequirementsDocument/TodoExecution/TodoMutation/ToolBucket, UseCaseAuditEmissionTests, UseCaseComponentUpdateWorkspaceResolutionContractTests:86, UseCaseCqrsTests, UseCaseDiagramUmlContractTests, UseCaseExpandedScopeTests, WikiDumpG6/G7, WindowsServiceDeploymentGuardTests:103 (generatedUtc is never read), WorkspacePolicyServiceTests, WorkspaceProcessManagerStartupTests:122.
- **N44** Storage seeds: AuditPayloadMigrationTests, AuditPayloadProviderMigrationTests, CanonicalToolBucketMigrationTests, DbFkBehaviorTests, EfNavigationCollectionContractTests, McpDbContextMultiTenantTests, UseCaseStorageTests, WorkspaceDiscriminatorRealTests.

## What the fix needs
- **Test-side change only:**
  - S3 and S4 (WorkspaceValidationService).
  - S36 and U1 (HandoffIngestionService).
- **Small production change, existing clock:** S2. GraphRagService already holds a clock; only the cleanup has to start reading it.
- **New optional clock parameter (System default) on:**
  - SessionLogService (S1)
  - FederationEnvelopeSigner (S5 to S20)
  - InMemoryKeyServerService (S21 to S35)
  - MemoryConsolidateOperations (S37)
- One scripted provider per test is enough; TestSupport/ScriptedTimeProvider.cs already exists. Set a reference instant and place the recorded times relative to it.
- The reviewer's proposed acceptance-criterion sentence covers all 37 siblings. The boundary rows above give the "just before, at and just after" rows for each decision, and say whether each comparison is strict or inclusive so the "at" row expects the right outcome.
