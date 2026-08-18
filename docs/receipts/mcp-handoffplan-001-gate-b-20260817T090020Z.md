# GATE B: integration RED then two full greens

Written: 2026-08-17T09:00:20Z
Session: GrokCode-20260816T175800Z-mcp-handoff-001
Turn: req-20260817T082548Z-013-gate-b-only
Isolated console: PowerShell.Mcp Window #55840 Cheddar at F:\GitHub\McpServer.
A standby console landed in `.mcpServer/worktrees/bug-triage-139-remediation`; Gate B commands were not run from that worktree.

## Honesty

Prior Gate B evidence in this session: MarkerRegeneration timed out under suite load (FSW Changed/Created missed atomic replace and dropped events). QuadBrain ArbiterOfTruth empty output came from Record vs RecordOutput split across await. RequirementScopeLayerRepl WaitForMarkerAsync 60s coincided with SQLite `no such column: s0.AgentExecutablePath`. Those were not relabeled as complete.

Dirty integration fixes already on disk (Renamed+poll, atomic QuadBrain record) were kept and given deterministic tests that fail the prior races. They were not replaced with longer timeouts.

## Focused RED then GREEN

### Marker FSW race
- Tests: `MarkerFileChangeObserverTests.ChangedCreatedOnly_DroppedEvents_TimesOut` (prior race), `MarkerFileChangeObserverTests.RenamedAndPoll_DroppedEvents_ObservesAtomicReplace` (current contract)
- RED of prior mode: TimeoutException when EnableRaisingEvents is false.
- GREEN of current mode: observes File.Replace without raising watcher events.
- MarkerRegenerationIntegrationTests.WatchForMarkerChange now delegates to `MarkerFileChangeObserver.Mode.RenamedAndPoll`.

### QuadBrain zip race
- Tests: `AtomicBrainSlotInvocationRecorderTests.SplitRecord_OverlappingCompletions_ZipsMismatchedPairs`, `AtomicBrainSlotInvocationRecorderTests.AtomicRecord_OverlappingCompletions_KeepsRoleOutputPairs`
- Split RED: roles `[Creativity, Logic]` pair with outputs `[Logic-out, Creativity-out]`.
- Atomic GREEN: each role pairs with `{role}-out`.
- Live QuadBrain factory already recorded atomically after CompleteAsync; that behavior is now locked by the focused tests.

### Scratch SessionLogs.AgentExecutablePath
- Root cause: `McpServer.Storage.SqliteMigrations` designers mention `AgentExecutablePath`, but no Sqlite Up() adds the four agent-header columns from Storage `20260722214500_AddAgentSessionHeaderFields`. Fresh `MigrateAsync` therefore creates SessionLogs without the column. Host backfill/query then throws `no such column`.
- RED: `ScratchSqliteSchemaTests.Backfill_LegacySessionLogsMissingAgentExecutablePath_Throws` (`no such column`). `EnsureAgentExecutablePath_LegacySchema_ThrowsBeforeHostStart`.
- GREEN: `ApplyAndVerify_EmptyDatabase_CreatesAgentExecutablePath` after migrate plus idempotent ALTER of the four header columns.
- Scratch `StageWorkspaceDatabaseAsync` now calls `ScratchSqliteSchema.ApplyAndVerifyAsync` before seeding the workspace row. IntegrationTests references `McpServer.Storage.SqliteMigrations`.
- A dedicated provider migration for that gap is deferred with the provider suite.

### SessionLog todoId query under suite load
- First full run Failed 1: `SessionLogControllerTests.Query_FilterByTodoId_ReturnsOnlyMatches` (Expected session missing from a 1-item collection). Isolated rerun Passed 1. Shared fixture plus fixed `MCP-SESSIONLOG-002` and `limit=50` was not exclusive under class-wide session volume.
- Test now uses a unique `ISSUE-{n}`, asserts the begin persist, `limit=200`, and that every hit turn carries that todoId.
- Isolated GREEN after the change: Failed 0 / Passed 1.

## Two consecutive full IntegrationTests runs

Filter (provider/handoff migration deferred): `FullyQualifiedName!~HandoffIngestionStorageMigrationTests&FullyQualifiedName!~ProviderDatabaseIntegrationTests`

Discovered via `--list-tests`: 276 total. 6 excluded (3 HandoffIngestionStorageMigrationTests + 3 ProviderDatabaseIntegrationTests). 270 executed.

- INT1 (`handoff-gate-b-int1b.log`): Failed 0 / Passed 270 / Skipped 0 / Total 270. Duration 3 m 29 s. Exit 0.
- INT2 (`handoff-gate-b-int2.log`): Failed 0 / Passed 270 / Skipped 0 / Total 270. Duration 4 m 50 s. Exit 0.

RequirementScopeLayerRepl and four MarkerRegeneration tests were in the executed 270 both times.

An earlier INT1 (`handoff-gate-b-int1.log`) was Failed 1 / Passed 269 on the old todoId test and is not counted as a green.

## Provider deferral

BUG-TRIAGE-139 worktree exists on disk. No testhost from that worktree was running. This invocation still did not run the 6 Handoff migration / provider-database tests (SQLite down-up, SQL Server LocalDB, PostgreSQL). Report them deferred until instructed.

## Unit scope

Not rerun. Gate B edits are in the IntegrationTests project (plus the already-dirty QuadBrain/Marker helpers). No shared production unit assembly was changed.

## Changed files

- `tests/McpServer.Support.Mcp.IntegrationTests/MarkerFileChangeObserver.cs`
- `tests/McpServer.Support.Mcp.IntegrationTests/MarkerFileChangeObserverTests.cs`
- `tests/McpServer.Support.Mcp.IntegrationTests/AtomicBrainSlotInvocationRecorder.cs`
- `tests/McpServer.Support.Mcp.IntegrationTests/AtomicBrainSlotInvocationRecorderTests.cs`
- `tests/McpServer.Support.Mcp.IntegrationTests/ScratchSqliteSchema.cs`
- `tests/McpServer.Support.Mcp.IntegrationTests/ScratchSqliteSchemaTests.cs`
- `tests/McpServer.Support.Mcp.IntegrationTests/Controllers/MarkerRegenerationIntegrationTests.cs`
- `tests/McpServer.Support.Mcp.IntegrationTests/RequirementScopeLayerReplIntegrationTests.cs`
- `tests/McpServer.Support.Mcp.IntegrationTests/Controllers/SessionLogControllerTests.cs`
- `tests/McpServer.Support.Mcp.IntegrationTests/McpServer.Support.Mcp.IntegrationTests.csproj`

MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, and MCP-HANDOFFREVIEW-001 remain open. No commit, merge, or push.
