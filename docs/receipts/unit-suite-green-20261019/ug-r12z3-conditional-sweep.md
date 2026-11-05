# Slice Z3 conditional-wait sibling sweep (TR-MCP-USECASE-018 AC-006 as appended 2026-11-04 15:29:24)

Read-only subagent turn req-20261104T213028Z-z3-conditional-sweep (parent req-20261104T210910Z-prompt-6985) in session ClaudeCode-20261102T182906Z-plugin-session. Its final report as received, exported from the session transcript by Export-SubagentReport.ps1 on 2026-11-04 after the fifth holistic read found it had no saved receipt (finding d2), with the harness indentation removed.

2026-11-04T15:46:29 Slice Z3 sibling sweep (read only, worktree at 121992b3). I checked the 42 HANG-GUARD rows, the helper rows that carry a hang-guard bound, and the unit and helper configured-duration rows that a finite delay must beat, against TR-MCP-USECASE-018 AC-006 as stored at 15:29:24. Nothing tracked changed.

**Below the 100 times margin**

- `HandoffDurabilityTests.ProcessingLease_RenewsAndFencesTerminalUpdates`, the renewed-expiry wait (`WaitUntilAsync` call at line 731, bound 10 s at 737). Your computation is confirmed: the sum is 360 ms, so 10 s is about 28 times. The bound needs to be 36 s.
  - 50 ms: the heartbeat's real poll of the adjustable clock (`HandoffIngestionService.cs:1025-1031`). After the advance at `HandoffDurabilityTests.cs:730`, it wakes within one poll.
  - 150 ms: the renewal UPDATE can find the database busy. It runs on a fresh context (`HandoffIngestionService.cs:994-1004`, through `TestDbFactory` at `HandoffDurabilityTests.cs:887` and `:1052-1064`), which has its own connection to the same file (`Pooling=False`, `:41-49`). The helper reads at the same time.
  - 10 ms: the helper's poll (`:845`).
  - 150 ms: the helper's read through a new context (`:734-735`) can find the database busy while the renewal writes.
  - One correction to your brief: this case has one ingest, not several. The connections that compete are that ingest's heartbeat context and the helper's contexts, one per poll.
- The other four `WaitUntilAsync` callers are not below the margin on their own paths. Each passes its own 10 s argument (`timeout` is a parameter at `:839-847`). No other connection can write while they wait, so there is no conditional wait to count, and 10 s is 1000 times their 10 ms poll. Only the records' rule of one bound for all five would carry 36 s to them.
  - Line 370: the reservation commits before the signal at 360. The ShortLease heartbeat (1 h on the adjustable clock) never writes. The second service starts only after the wait.
  - Line 724: the heartbeat does not write until the advance at 730.
  - Line 413: the first approval is parked on CreateGate (`:1002-1003`), and the second approval's claim commits before `secondTask` returns.
  - Line 866 (`WaitForApprovalOwnerAsync`, called at 401 and 439): the claim commits before the task returns.

**Changed under the new clause, still at or above 100 times**

- None.

**Not touched by the clause**

- **No SQLite on the path and no other conditional wait.**
  - `TodoWorkflowTests:232`, `QBAgentHeartbeatOverlayTests:112`, `QBAgentRoleStreamChatClientTests:33` and `:44`.
  - `StubMarkerFileReaderTests:151` and its helper `:273`.
  - `FederationControllerTests:247` and its helper `:752`; the helpers `TransactionSecurityControllerTests:628` and `FederationOperationApplyServiceTests:348`.
  - `AgentHealthMonitorServiceTests:57`, `:59`, `:103`, `:105`: backoff 0 skips the delay (`AgentHealthMonitorService.cs:110-112`).
  - `AppSettingsFileServiceTests:222`, `ChannelChangeEventBusTests:33` and `:77`, `KeyedAsyncLockTests:30` and `:45`, `TurnTransactionCoordinatorTests:481` and `:524`.
  - The helpers `AgentPoolServiceTests:355` (in-memory queue; its own 20 ms poll is 200 times) and `DispatcherTests:54`.
- **EF Core InMemory provider, so no SQLite.**
  - `BrainSlotInvocationTransactionTests:203`.
  - `QuadBrainOrchestrationServiceTests:196`, `:197`, `:205`, `:206`.
  - The 30 s slot-timeout rows of the BrainSlot and QuadBrainLive classes, and `QuadBrainOrchestrationServiceTests:571`.
- **Private `:memory:` SQLite with one connection, which cannot meet busy.**
  - `BugTriage139SixteenthIdentityConsumerTests:62`, `:69`, `:83`, `:250`, `:258`, `:277` (`ProviderTestDatabase.cs:37`; no statement runs before the resolver).
  - `BugTriage139SixteenthSqliteConsumerTests:161` and `:178`, and its 15 s open timeout at `:159`.
  - `SqliteBoundedOpenFourteenthTests:72`, `:78`, `:97`, `:101`, `:298`, `:314`, `:331`, `:335`, and the 10 s open timeouts at `:70` and `:296`. Their 265 ms sum (`SqliteBoundedConnectionOpener.cs:20`, `:208-217`, `:278-279`) already counts the conditional cleanup deadline in full.
- **SQLite file, but no other connection writes during the step.**
  - `HandoffDurabilityTests:88`, `:360`, `:701`.
  - The drain at `:779`: still exactly 100 times the 50 ms heartbeat poll.
  - The helper at `:994`: only the 50 ms compensation timer runs before the cancel (`HandoffIngestionService.cs:876-880`).
  - The 5 s CompensationTimeout rows (`:697`, `:836`).
- **Not a hang guard:** `SessionLogSupersessionTests.cs:219` (`Default Timeout=1`). The case causes the busy condition on purpose, and either outcome passes (`:251-263`).
- **Other retry loops searched.** WebsiteIngestor's backoff (`WebsiteIngestor.cs:260-319`) cannot be met by its row at `WebsiteIngestorTests.cs:221`, because the stub always succeeds (`:224-236`). The other retry loops in src are on no unit hang-guard path. There is no Polly in src, and nothing sets `busy_timeout` or adds a waiting interceptor (`McpDbContext.cs:296-309`, `:1216-1236`). The remaining configured rows are scripted-clock values, data, literals, or timeouts that are the input under test.

**Inference**

- **How I counted:** 150 ms for each poll whose iteration runs, or is followed by, a statement that can meet busy. This is your reading too. The clause does not say what a path with no poll counts.
- **Why the renewal and the helper's read can meet busy:** this comes from SQLite's locking rules, not from this repo. In rollback-journal mode, a reader's shared lock blocks a writer's commit. In WAL mode, the last connection to close runs a checkpoint, and with `Pooling=False` each helper poll opens and closes a connection. I did not read which journal mode EnsureCreated leaves in place; either mode allows busy here.
- **Lines 413 and 866:** that their claims commit before the wait rests on Microsoft.Data.Sqlite running async calls synchronously, which is documented. If that is wrong, those paths are still under 360 ms, so 36 s would cover them.
- **Line 779 and the 5 s compensation rows:** "no concurrent writer" is read from the passing-run path. On a failure path with a heartbeat renewal in flight, the drain could meet busy; I did not count that.

**Files and logging**

- The harness refused the findings file `/home/sharpninja/.claude/scratch/pr82-review-20261019/z3-sweep/findings-z3-sweep.md` ("Subagents should return findings as text"), so the findings are only in this report.
- Short response file used for the turn: `/home/sharpninja/.claude/scratch/pr82-review-20261019/z3-sweep/response-z3.txt`.
- Search helper (PSScriptAnalyzer 0 findings): `/home/sharpninja/.claude/scratch/pr82-review-20261019/z3-sweep/Find-Text.ps1`.
- Session `ClaudeCode-20261102T182906Z-plugin-session`, request `req-20261104T213028Z-z3-conditional-sweep`. The turn was opened, updated twice (8 actions, 3 decisions) and completed through the plugin scripts; every call exited 0.
