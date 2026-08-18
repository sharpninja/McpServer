# MCP-HANDOFFPLAN-001 scope-corrected receipt

UTC: 2026-08-17T01:52:00Z
Session: GrokCode-20260816T175800Z-mcp-handoff-001
Turn: req-20260817T004104Z-008-correct-handoff-validation-scope
Agent: GrokCode
TODOs: MCP-HANDOFFPLAN-001 and MCP-HANDOFF-001 remain Done=false. No commit.

## Validation scope (corrected)

The four historical SQL Server LocalDB tests (SqlServerDecompose4nfBackfillMigrationTests x2, SqlServerRenameQuadBrainRolesMigrationTests x2) are not the handoff BDPv4 gate. They do not exercise AddHandoffIngestionStorage.

Actions taken:
- Reverted the two tracked historical test files to HEAD (157 insertions removed).
- Deleted untracked harness files SqlServerLocalDbFixture.cs and NoLockSqlServerHistoryRepository.cs.
- git status for those four paths is clean.

The new-migration gate is clean-head apply of AddHandoffIngestionStorage, HandoffIngestionRuns/HandoffDiagnostics round-trip, and focused downgrade to the immediately preceding migration then re-upgrade on SQLite, SQL Server, and PostgreSQL.

## New-migration evidence

dotnet test filter FullyQualifiedName~HandoffIngestionStorageMigrationTests|ProviderDatabaseIntegrationTests:
Failed 0, Passed 6, Skipped 0, EXIT=0.

That set includes:
- ProviderDatabaseIntegrationTests SQLite and SQL Server clean-head plus AddHandoffIngestionStorage applied and handoff round-trip.
- ProviderDatabaseIntegrationTests PostgreSQL clean-head plus handoff round-trip using EphemeralPostgresFixture / MCP_TEST_POSTGRES_CONNECTION.
- HandoffIngestionStorageMigrationTests downgrade-to-previous and re-upgrade on SQLite, SQL Server LocalDB, and PostgreSQL.

Full Support integration: Failed 0, Passed 266, Skipped 0, EXIT=0.

## Remaining defects addressed (live files)

1. WorkspaceServiceAccessor now PushWorkspace/IDisposable/IAsyncDisposable with nested restore. STDIO ApplyWorkspaceOverride returns a scope and every caller uses `using var`.
2. Ingest reserves with Processing lease/owner/expiry; live replay returns handoff_in_progress; stale lease takeover; exception/cancel persist Terminal Failed; cancel still throws.
3. Approval claims with ApprovalOwner/ApprovalLeaseExpiresAtUtc; stale Approving recoverable; concurrent approve still one TODO.
4. SaveRunAfterTodo uses provider-aware commit-ambiguity handling and a fresh context with a non-cancelled compensation token. It does not call ChangeTracker.Clear.
5. TodoCreationIntentId is persisted before ITodoService.CreateAsync. TodoCreateRequest/TodoItem.IdempotencyKey distinguishes this-run heal from caller-owned collision.
6. HandoffReplayKeys is SHA-256 hex over a length-prefixed canonical payload. Force still unique per run id.
7. HandoffDbExceptions uses SqlException 2601/2627, PostgresException SqlState 23505, SQLite extended 2067/1555.
8. PendingReview/Failed honesty: error diagnostics cannot report Success=true. Invalid RequireReview is Failed.
9. Collision and create-failure persist ErrorCode (todo_collision / todo_create_failed). GET preserves Success/Error/ErrorCode.
10. Plugin skill workflow test invokes real HandoffWorkflow ingest/get/approve HTTP paths, not inventory-only.
11. Shared LocalDB/NoLock harness removed. Historical tests restored.

## Required green evidence

- Focused handoff Support.Mcp tests (FullyQualifiedName~Handoff): Failed 0, Passed 66, Skipped 0.
- Workspace/replay/db-exception/durability included in the 54-test subset and the 66-test Handoff filter.
- Client: Failed 0, Passed 281, Skipped 0, EXIT=0.
- Repl.Core: Failed 0, Passed 825, Skipped 0, EXIT=0.
- Support.Mcp unit Category!=Integration: Failed 0, Passed 1893, Skipped 0, EXIT=0.
- Support integration: Failed 0, Passed 266, Skipped 0, EXIT=0.
- Repl integration: Failed 0, Passed 181, Skipped 0, EXIT=0.
- ./build.ps1 Compile EXIT=0.
- ./build.ps1 Test EXIT=0 (includes Support.Mcp.Tests 1893/1893/0).
- ./build.ps1 ValidateTraceability EXIT=0.
- ./build.ps1 SyncAgentPlugins EXIT=0.
- git diff --check on handoff source/test paths EXIT=0.
- Full-tree git diff --check EXIT=2 only on pre-existing docs/Project/Technical-Requirements.md "new blank line at EOF". That generated projection was not edited in this turn.

## Independent hostile rerun (not implementer self-check)

While the hostile validator was still writing its receipt, it independently reran the focused provider filter. Log: docs/receipts/_hv-testrun-20260817T020200Z.log.

That rerun reported Failed 1, Passed 4, Skipped 0, Total 5, EXIT=1.

The failing test was ProviderDatabaseIntegrationTests.SqlServer_LocalDb_CleanDatabase_AppliesMigrationsAndPersistsEntity with SqlException Execution Timeout Expired during MigrateAsync. This is a live LocalDB timeout on a later rerun, not the four historical 4NF/rename tests.

This receipt therefore does not claim the new-migration SQL Server provider test is unconditionally green under concurrent reruns. The earlier implementer-run counts remain: focused 6/6/0 and Support integration 266/266/0.

## Not claimed complete

Hostile OverallVerdict AGREE is still required before any done:true. Both MCP TODOs remain open for independent Codex review. No commit, merge, or push. The independent LocalDB timeout above is enough that this turn is not a completion claim.
