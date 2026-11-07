# R13-F1 sibling sweep report (before hostile validation round 14)

The final report of the read-only R13-F1 sibling sweep subagent (session ClaudeCode-20261102T182906Z-plugin-session, request req-20261106T081200Z-r13f1-background-sweep, parent turn req-20261104T231251Z-continue-pr87-completion-2), returned 2026-11-06T02:41:07 local and exported verbatim from the session transcript by Export-SubagentReport.ps1. It judges committed HEAD 4e992d30; slice Z12 (commit 8c944541) fixes the production site and the four siblings it lists.

2026-11-06T02:41:07 (measured by Complete-Turn.ps1)

# R13-F1 sibling sweep: unit-reachable background callbacks, deadlines and live-clock age decisions

**Answer:** only one production site has this failure class: the voice idle-cleanup timer. Four unit test methods are siblings, all in VoiceConversationServiceTests. Every other background loop, deadline or cache-expiry decision a unit test reaches is one of three things: blocked by the code or the test, a hang guard, or reached only by Integration cases.

This was a read-only sweep. No tracked file was changed. Scripts are in /home/sharpninja/.claude/scratch/pr82-review-20261019/sweep-r13f1/ and all are PSScriptAnalyzer-clean.

## Before you use this
- **The worktree was being edited while I swept (observed).** At 02:29 `git status` showed uncommitted changes to VoiceConversationService.cs, VoiceConversationServiceTests.cs, AgeBoundaryContractTests.cs and SettableTimeProvider.cs. It also showed new untracked files: TriggeredTimerTimeProvider.cs, its tests, IdleSessionLifecycleContractTests.cs and receipt ug-r14z12-mockgate-1.
- **Everything below is judged against committed HEAD 4e992d30.** I read the voice files with `git show HEAD:` and confirmed my earlier reads match HEAD (blob 9ca480df, 2061 lines; tests blob fb32a679, 422 lines). The in-flight fix is not counted as ENFORCED.
- **How I drew the line (my judgment, logged as a decision):**
  - SIBLING means an age, expiry, staleness or lifecycle decision on recorded state (delete, expire, renew, take over) that a background callback, deadline or live clock makes and that can change a unit assertion.
  - An operation timeout that only bounds how long an awaited step may run, and only turns a pass into a fail when that step overruns, is HANG-GUARD. I report its margin against the workspace's 100x rule (TR-MCP-USECASE-018 AC-006).

## Counts
- SIBLING: 4 test methods, 1 production site.
- Reached but result-invariant (INERT): 4 voice cases.
- HANG-GUARD: 13 entries.
- ENFORCED: 15 entries.
- INTEGRATION: 8 groups.
- INERT or not reached: 12 entries.

## SIBLING (4)
**Production site** (HEAD line numbers):
- VoiceConversationService.cs:68 arms `new Timer(OnIdleCleanupTick, null, 60 s, 60 s)`.
- CleanupIdleSessionsAsync at :1851-1908 takes `now = DateTimeOffset.UtcNow` at :1857. At :1862 it marks a session stale when `now - LastUpdatedUtc > SessionIdleTimeoutMinutes && !IsTurnActive`, then calls DeleteSessionAsync at :1900.
- There is no clock seam (UtcNow at :78, :195, :354, :379, :471, :1857) and no timer seam (:68).
- Only VoiceConversationServiceTests constructs the real service (:256). Every other unit caller uses a substitute (VoiceControllerTests:25, AgentPoolServiceTests:364, TransactionGatedVoiceConversationServiceTests:20). No unit project starts the Program host: there are 0 WebApplicationFactory or TestServer hits.

**Mechanism (inference):** a pause of at least 60 s lets the timer tick, and either a forward wall-clock step of more than 15 min or a pause longer than 15 min makes the age exceed the timeout. The tests set the 15 min timeout at :268.

**The four tests:**
- **S1 :19 CreateSessionAsync_SameAgentAndWorkspace_ReusesExistingIdleSession** (the finding). If the first session is deleted, the second create mints a new id, so Assert.Equal at :39 fails.
- **S2 :109 SubmitTurnAsync_IncludesConfiguredModelApiKeyInExecutionOptions.** If the session is deleted between create and submit, SubmitTurnAsync returns null (:181-182) and Assert.NotNull at :127 fails. A delete during submit disposes the Gate, which :184 then awaits.
- **S3 :136 SubmitTurnAsync_UsesInfiniteCopilotTimeoutInExecutionOptions.** Same mechanism; Assert.NotNull at :152.
- **S4 :158 SubmitTurnStreamingAsync_EmptyRuntimeStream_EmitsErrorAndRecordsAssistantError.**
  - A delete before streaming yields the "not found" error (:519-522), so the message assertion at :174 fails.
  - A delete after the turn makes GetStatusAsync return null (:374-375), so :178 fails.

**What a scripted fix needs:** one injected TimeProvider used for both GetUtcNow and CreateTimer, plus a test provider whose timers fire only when the test triggers them. It must cover every LastUpdatedUtc write (:100, :195, :354, :379, :484) and FindSessionByDevice (:470-480), which shares the rule but no unit test reaches.

**Boundary rows** (the comparison is strict `>`):
- Age = timeout - 1 tick: kept.
- Age = timeout exactly: kept.
- Age = timeout + 1 tick: deleted.
- Turn active, any age: kept.
- Negative age (backward clock step): kept.
- Timeout 0 with age 0: kept.
- No timer tick: no decision at any age.

## Reached but result-invariant (INERT, 4)
- **VoiceConversationServiceTests :43:** NotEqual cannot flip, because ids carry a GUID (:133).
- **:67:** SendSessionMessageAsync returns false both for a missing session (:341-342) and for a one-shot session (:344-345).
- **:82 and :95:** they assert only on the returned create response.

## HANG-GUARD (13)
- **G1 AgentHealthMonitorServiceTests.cs:18 and :66 (borderline, your call).** A 2 s CTS (:57, :103) is linked into BackgroundService's stopping token, which gates `while (!stoppingToken.IsCancellationRequested)` at AgentHealthMonitorService.cs:40. A 1 s WaitAsync (:59, :105) follows. If the loop's first iteration is delayed past 2 s, the loop skips its work. The only effect is a TimeoutException; neither case can falsely pass. The project is net10.0 (QBrainAi.Services.csproj:3); that ExecuteAsync runs on the thread pool under .NET 10 is my inference, not verified from the package. It is borderline because the deadline makes the production loop skip its work rather than only bounding a wait.
- **G2 StorageConnectivityHealthCheckTests.cs:44.** A 5 s production probe (StorageConnectivityHealthCheck.cs:24, :47-48) would turn Healthy into Unhealthy only if it overruns. Cases :32 and :67 are Unhealthy either way.
- **G3 DispatcherTests.cs:132.** A 50 ms timeout (:46) against a 5000 ms handler delay (:54), a 100x margin. The ordering is argued from elapsed time, not enforced.
- **G4 (H2, H3) TurnTransactionCoordinatorTests.cs:487 and :513.** A 1 s commit timeout against a 100 s substitute delay (:493, :536), 100x; ordering argued. Coordinator degraded mode has no expiry (the clock search found none).
- **G5 QBAgentHeartbeatOverlayTests.cs:90.** A 10 s guard against an 80 ms delay (125x). Extra heartbeat frames cannot change the result: the first paint comes before the first await and Hide runs in finally (QBAgentRunLoop.cs:228-241).
- **G6 VoiceConversationServiceTests.cs:193.** AgentCliInteractiveSession's 250 ms and 500 ms diagnostic waits (:410, :432) bound a drain from an in-memory stream. If they fire, the stderr tail is lost.
- **G7 ConfiguredTriageResearchRunner tests.** MaxRunTime (30 min default, TriageModels.cs:374; 42 s in one test) bounds the run; the `timedOut` flag at :94 flips only after the bound.
- **G8 ReplCommandTimeoutCallSiteTests "answers at once" rows.** 2 min default (ReplCommandTimeout.cs:17) or larger.
- **G9 Bounded SQLite open unit cases.** BugTriage139SixteenthSqliteConsumerTests.cs:134 uses a 15 s bound (143x by its own remarks). SqliteBoundedOpenFourteenthTests.cs:43 and :269 margins were not measured individually.
- **G10 StorageCommandBudget 5 s** (StorageCommandBudget.cs:12) for unit tests that reach SessionLogService.cs:358 and :1881, TriageService.cs:112 or RequirementsRecoveryService.cs:110.
- **G11 GraphRag index timeout,** 600 s (GraphRagOptions.cs:31; GraphRagService.cs:202).
- **G12 HttpClient timeouts** under mocked handlers (Client and TransactionSecurity registrations).
- **G13 (H1) RepositoryEvidenceWindowsProcess.cs:268-275.** A Stopwatch bound; its only effect is a TimeoutException.

## ENFORCED (15)
- **Handoff lease heartbeat:**
  - Decisions read the scripted clock (HandoffIngestionService.cs:1017-1018).
  - The due time is computed synchronously before extraction (:136).
  - ShortLease's 1 h heartbeat (HandoffDurabilityTests.cs:873-879) exceeds the scripted advances at :407 and :443.
  - The renewal case at :722 is designed for the decision (:770-779).
- **HandoffAuditDefectTests:** SettableTimeProvider at :299 is never advanced.
- **HandoffIngestionServiceTests:** ManualTimeProvider at :27.
- **MemoryConsolidateJobTests.PerWorkspaceLock after Z4:** scripted clock at :24-27, sequential runs.
- **BrainSlotInvocationTransactionTests.cs:164:** Task.Delay(Infinite) at :314.
- **ReplCommandTimeoutCallSiteTests timed-out rows:** infinite delays at :249 and :309.
- **SessionLogSanitizerTimeoutTests:** a scripted timeout seam at :74-80.
- **Tunnel provider unit cases:** substitute runners report the CLI missing (TunnelProviderTests.cs:49, :140, :225; NgrokTunnelProviderEnvironmentTests.cs:136-138, :168-170) or validation fails first.
- **Frp reconcile loop:** never started (FrpTunnelProvider.cs:95-105).
- **Federation background services and TransactionPubSubReplayWorker:** the loops are never started. Tests call ReplayOnceAsync (:24, :49) with an Arg.Any cutoff (:37, :52).
- **TurnTransactionsController:** an explicit cutoff (TurnTransactionsControllerTests.cs:68-86).
- **Verified-marker cache:** tests inject the file system (MarkerIsolationBindingTests.cs:18, MarkerWalkTests.cs:37). The live-clock static is reachable only through PhysicalMarkerFileSystem.cs:27-32 and TryLoadTrustedMarker (:206), which have no unit callers.
- **GitHubCliService.cs:838:** no ExpiresAtUtc is set in GitHubCliServiceTests.cs (search receipt), so the expiry is null (inference).
- **TriageService:** a required, scripted TimeProvider.
- **Caller-cancellation tests:** infinite waits (BugTriage139SixteenthIdentityConsumerTests.cs:38, :222). QBAgentRunLoopTests with an infinite heartbeat (:30, :77, :100) are blocked by QBAgentRunLoop.cs:220-221.

## INTEGRATION (8)
- SessionLogFileWatcherTests.cs:63-65.
- QBAgentHeartbeatOverlayTests.cs:43-45.
- TunnelProviderTests :70, :92, :161, :183; FrpTunnelProviderTests :185, :241.
- CodexCliAgentExecutionStrategyTests :124-126.
- StorageCommandBudgetTests :21, :56.
- TriageServiceTests :50-52.
- SqliteBoundedOpenFourteenthTests :113, :201, :346; BugTriage139SixteenthSqliteConsumerTests :20.
- The BugTriage139 traversal, identity and Linux classes.

## INERT or not reached (12)
- **QBAgentRunLoopTests with the default 120 ms heartbeat** (:44, :148, :166, :195, :210, :223): they use Contains checks and counts.
- **HandoffDurabilityTests.cs:560:** CompensationFailed comes out whether the 50 ms timeout fires or the 5 s read finishes (:829-842, :866-869). Adequacy note: the test does not tell the timeout branch from the hidden-TODO branch.
- **HandoffWorkspacePathsTests:** a single lease holder, so renewal changes nothing it asserts.
- **WorkspaceProcessManager StartAsync:** no timer (:153-208).
- **One-shot hosted-service tests:** AgentPoolSeed, LegacyTodoSqliteMigrator, TodoBootstrapImporter, BrainSlotStartupSeeder.
- **McpDatabaseEncryption:** the tests are dry runs; the wait loop at :541 is not reached.
- **Retry delays and Stopwatch duration recordings:** delays only, no time-based decision.
- **TodoController SSE heartbeat (:429):** no unit test drives the stream.
- **LoginHandler:** 0 test hits.
- **TriageQueueWorker:** 0 test hits.
- **FindSessionByDevice:** no unit caller.
- **Test-only doubles:** StubMarkerFileReaderTests (1 s against 10 ms) and MarkerFileTrustTests.

## Re-check of the previous sweep's exclusions
- **Replay-worker cutoff:** enforced (Arg.Any).
- **TurnTransactionsController cutoff:** enforced (explicit value).
- **Voice entry:** split.
  - The FindSessionByDevice half is enforced, because no unit test calls it.
  - The cleanup-timer half was argued from elapsed time. That is R13-F1.
- **GitHubCliService:** enforced by test data (inference).
- **Marker cache:** enforced by the injected file system.
- **Tunnel deadlines:** enforced by substitutes.
- **H1:** a hang guard; argued from elapsed time, and its only effect is a TimeoutException.
- **H2 and H3:** hang guards whose ordering is argued from elapsed time at a 100x margin. They could be made enforced with an infinite substitute delay, as BrainSlotInvocationTransactionTests.cs:314 does.
- **N34:** enforced after Z4.
- **N35:** argued from elapsed time, not enforced. This is the finding itself.
- **N36:** enforced by substitutes.

## Session log readbacks
- Begin: `BeginTurn exit=0`, `READBACK req-20261106T081200Z-r13f1-background-sweep status=in_progress`. The session showed turns=68, inProgress=4.
- Update 1: `PatchTurn exit=0`, actions 4, designDecisions 2.
- Update 2: `PatchTurn exit=0`, actions 9, designDecisions 7.
- Complete: `CompleteTurn exit=0`, `READBACK ... status=completed actions=9 decisions=7 responseLength=2537`.

Profile load: 1 profile file and 12 Global plus 3 Workspace MCP memories, MEMORY-PROCESS-007 excluded; marker trusted, plugin mcpserver 1.121.0.

## Ratings
- **Accuracy 93%:** every claim is cited from HEAD source. The .NET 10 BackgroundService threading and the GitHub null expiry are inferences.
- **Completeness 90%:** the margins of SqliteBoundedOpenFourteenthTests :43 and :269, the WorkspaceIdentityPath fire-and-forget at :446, and MemoryConsolidateTests were not read individually.
