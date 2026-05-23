# Plan: Provider-Agnostic TodoService via `McpDatabaseProviderFactory` (v1.0)

**Session turn**: `req-20260420T193358Z-prompt-64b9`
**Approved**: 2026-04-20
**Branch target**: `feat/todo-provider-agnostic` off `origin/develop` (Azure DevOps)

## Goal

Todos persist to whichever database `Mcp:Database:Provider` resolves (sqlite, sqlserver, or postgres), routed through the sanctioned `McpDatabaseProviderFactory`. Zero sqlite hardcoding in the TODO path.

## Context

TR-MCP-CFG-007 mandates factory-pattern DB provider selection for all three engines. TR-MCP-TODO-005 contradicts it by hardcoding "SHALL use SQLite as the authoritative current-state store". Both were marked Complete, which was a lie; the code in `SqliteTodoService`, `McpInstanceResolver.ValidateTodoStorage` (whitelist `yaml|sqlite`), and `TodoServiceFactory` prove it.

## Outcomes required to call it done

1. TR-MCP-TODO-005 rewritten to match CFG-007; `Covered by:` list purged of provider-specific types.
2. TR-MCP-TODO-006 audit contract preserved, wording de-sqlite-ified.
3. New TR-MCP-TODO-007: legacy sqlite TODO storage migration, one-shot idempotent.
4. EF entities + `McpDbContext` wiring + migrations for sqlite, sqlserver, postgres.
5. `EfTodoService` replaces `SqliteTodoService` as live impl; preserves YAML projection, audit history, change-event publish, projection-failure classification, SemaphoreSlim write serialization.
6. `TodoStorageOptions` + `McpInstanceResolver.ValidateTodoStorage` accept `{yaml, database}`; legacy `sqlite` aliases to `database` with warning log.
7. Legacy sqlite to configured-DB data migrator (one-shot, idempotent, behind flag).
8. Regression test that fails build when a ✅-Complete TR's `Covered by:` names provider-specific types that contradict CFG-007.
9. Live SQL Server LocalDB service Running, /health 200, 4446 pre-existing rows intact, TODO CRUD exercised end-to-end against sqlserver.

## Phases

### Preconditions (P0)

- P0.1 Commit or stash 8 dirty files on McpServer `develop`; at minimum commit the SqlServer `AddGraphEntitiesAndRelationships` migration already generated this session.
- P0.2 Create branch `feat/todo-provider-agnostic` off `origin/develop`.
- P0.3 Generate missing `AddGraphEntitiesAndRelationships` migration in `McpServer.Storage.PostgreSqlMigrations` so all three snapshots match before TODO migration adds.

### Phase 1: spec + contract + regression guard (~2h)

Byrd: tests first.

- P1.1 New tests:
  - `tests/Build.Tests/TrCoverageConsistencyTests.cs`:
    - `CfgComplete_WithCoveredByNamingSqliteTodoService_IsRejected`
    - `CfgComplete_WithProviderAgnosticEfTodoService_IsAccepted`
    - `Ignores_TRs_NotMarkedComplete`
  - `tests/McpServer.Support.Mcp.Tests/Options/McpInstanceResolverTests.cs` additions:
    - `ValidateTodoStorage_AcceptsDatabaseProvider`
    - `ValidateTodoStorage_AliasesSqliteToDatabase_LogsWarning`
    - `ValidateTodoStorage_RejectsUnknownProvider`
    - `ValidateTodoStorage_RequiresMcpDatabaseProviderWhenDatabase`
- P1.2 Doc edits to `docs/Project/Technical-Requirements.md`:
  - TR-MCP-TODO-005: swap "SQLite-Authoritative" for "Provider-Agnostic Database-Authoritative via McpDatabaseProviderFactory". Strike `SqliteTodoService`, `TodoStorageOptions.SqliteDataSource` from Covered-by. Add `EfTodoService`, `McpDatabaseProviderFactory`. Status flips to 🟡 In Progress until phase 5.
  - TR-MCP-TODO-006: drop "SQLite-backed". `Covered by` SqliteTodoService -> EfTodoService.
  - Add TR-MCP-TODO-007.
- P1.3 Code:
  - Rewrite `src/McpServer.Services/Options/TodoStorageOptions.cs` (Provider default `"database"`, accept `yaml|database`, alias sqlite->database, `MigrateFromLegacySqlite: bool`, `[Obsolete]` `SqliteDataSource`).
  - Rewrite `src/McpServer.Services/Options/McpInstanceResolver.cs:129-145`.
  - New `build/TrCoverageConsistency.cs` helper (parses md, yields `{TrId, Status, CoveredBy[]}`).
- P1.4 Gate: `dotnet build`, full `dotnet test` green.
- Commit: `feat(todo): amend TR docs + validator for provider-agnostic TODO storage`.

### Phase 2: EF entities + DbContext + migrations (~3h)

Byrd: tests first.

- P2.1 New tests: `tests/McpServer.Storage.Tests/TodoEntityConfigurationTests.cs`:
  - `TodoItemEntity_Has_IdAsPrimaryKey_And_SectionPriorityDoneIndexes`
  - `TodoAuditHistoryEntity_Has_AuditIdAutoIncrement_And_UniqueTodoIdVersion`
  - `TodoDocumentMetadataEntity_Has_SingletonIdKey_NoQueryFilter`
  - `TodoEntities_HaveNoWorkspaceQueryFilter`
- P2.2 New files:
  - `src/McpServer.Storage/Entities/TodoItemEntity.cs` (22 properties)
  - `src/McpServer.Storage/Entities/TodoAuditHistoryEntity.cs` (8 properties)
  - `src/McpServer.Storage/Entities/TodoDocumentMetadataEntity.cs` (8 properties)
- P2.3 Edit `src/McpServer.Storage/McpDbContext.cs`: three DbSets, FluentAPI config, PKs/indexes/unique constraints, ValueGeneratedOnAdd on AuditId. Exclude from `_workspaceId` global filter.
- P2.4 Migrations (one per provider), each named `AddTodoStorage`:
  ```
  $env:NuGetAudit='false'
  $env:MCP_EF_PROVIDER='sqlite';     dotnet ef migrations add AddTodoStorage -p src/McpServer.Storage.SqliteMigrations     -s src/McpServer.Support.Mcp -c McpDbContext
  $env:MCP_EF_PROVIDER='sqlserver';  dotnet ef migrations add AddTodoStorage -p src/McpServer.Storage.SqlServerMigrations  -s src/McpServer.Support.Mcp -c McpDbContext
  $env:MCP_EF_PROVIDER='postgresql'; dotnet ef migrations add AddTodoStorage -p src/McpServer.Storage.PostgreSqlMigrations -s src/McpServer.Support.Mcp -c McpDbContext
  ```
- P2.5 Integration test `tests/McpServer.Storage.IntegrationTests/AddTodoStorageMigrationTests.cs` (sqlite in-memory; sqlserver LocalDB, skippable; postgres skippable).
- Commit: `feat(todo): add TodoItem/AuditHistory/DocumentMetadata EF entities + migrations`.

### Phase 3: EfTodoService (~4h)

Byrd: tests first, heavy.

- P3.1 New `tests/McpServer.Support.Mcp.Tests/Services/EfTodoServiceTests.cs`. Port 12 existing SqliteTodoServiceTests method-for-method. Parameterize via abstract `EfTodoServiceTestBase<TFixture>` with concrete subclasses per provider fixture:
  - `SqliteEfFixture` (temp file)
  - `SqlServerEfFixture` (LocalDB per-test DB, Skip if absent)
  - `PostgresEfFixture` (Respawn+TestContainers, Skip if Docker absent)
  Extra: `EfTodoService_MatchesSqliteTodoServiceOrdering_AcrossProviders`, `EfTodoService_AuditVersionsMonotonicPerTodo`, `EfTodoService_ProjectionFailure_DoesNotRollBackMutation`.
- P3.2 New `src/McpServer.Services/Services/EfTodoService.cs`. Implements ITodoService, ITodoStore, IAsyncDisposable. Semantics identical to SqliteTodoService. Ctor via `IDbContextFactory<McpDbContext>`. In-memory sort after ToListAsync for ordering portability. JSON columns stored as `string`.
- P3.3 DI wiring in `src/McpServer.Support.Mcp/Program.cs`: `AddDbContextFactory<McpDbContext>()`, `AddScoped<EfTodoService>`.
- P3.4 Edit `TodoServiceFactory.CreatePrimary()`/`CreateForWorkspace` to branch YAML/DATABASE.
- P3.5 SqliteTodoService marked `[Obsolete]`, retained for phase-4 migrator use only.
- Commit: `feat(todo): EfTodoService provider-agnostic implementation`.

### Phase 4: legacy migrator (~2h)

Byrd: tests first.

- P4.1 New `tests/McpServer.Support.Mcp.Tests/Services/LegacyTodoSqliteMigratorTests.cs`:
  - `Migrator_CopiesAllRowsPreservingIdsVersionsAndMetadata`
  - `Migrator_IsIdempotent_WhenTargetTableNonempty`
  - `Migrator_IsNoop_WhenLegacyDbMissing`
  - `Migrator_IsNoop_WhenFlagFalse`
  - `Migrator_WritesMarkerFile_ToPreventRerun`
- P4.2 New `src/McpServer.Services/Services/LegacyTodoSqliteMigrator.cs` (IHostedService). Reads `Mcp:TodoStorage:MigrateFromLegacySqlite`, checks target empty, legacy file present. Copies three tables preserving ids/versions via IDENTITY_INSERT (sqlserver) / OVERRIDING SYSTEM VALUE (postgres) / normal (sqlite). Writes marker to DataFolder.
- P4.3 `AddHostedService<LegacyTodoSqliteMigrator>()` in Program.cs.
- Commit: `feat(todo): one-shot legacy sqlite->configured-DB migrator`.

### Phase 5: deploy + live verification (~1h)

- P5.1 Build + publish:
  ```
  $env:NuGetAudit='false'
  dotnet build McpServer.sln -c Release
  dotnet test  McpServer.sln -c Release --no-build
  dotnet publish src/McpServer.Support.Mcp -c Release -o F:/GitHub/McpServer/_publish
  dotnet publish src/McpServer.Launcher   -c Release -o F:/GitHub/McpServer/_publish-launcher
  Copy-Item F:/GitHub/McpServer/_publish-launcher/McpServer.Launcher.exe F:/GitHub/McpServer/_publish/
  ```
- P5.2 Deploy (gsudo, user-confirmed):
  ```
  gsudo pwsh -ExecutionPolicy Bypass -c "& F:\GitHub\McpServer\scripts\Update-McpService.ps1 -SkipBuild -SkipVersionBump -PublishSource 'F:/GitHub/McpServer/_publish'"
  ```
- P5.3 Verify: sqlcmd lists 3 Todo* tables; legacy migrator report; existing 4446 rows intact (AgentDefinitions=7, SessionLogs=186 spot); /health 200; REST CRUD + audit + projection-status via X-Api-Key.
- P5.4 Flip TR-MCP-TODO-005 back to ✅ Complete in the verification commit.
- Commit: `chore(todo): verify live sqlserver deployment + close TR-MCP-TODO-005/006/007`.

### Phase 6: PR

- `git push origin feat/todo-provider-agnostic`
- Open PR against `develop` on Azure DevOps only.
- Body: phases, test counts, live-verification output, TR diff summary.

## Risks + mitigations

| Risk | Mitigation |
|---|---|
| Postgres fixture unavailable locally | Skip with reason; require CI to run before merge |
| EF JSON-as-string column sizes differ per provider | `HasColumnType` branch on `Database.IsSqlServer()` etc. in FluentAPI |
| Legacy migrator breaks on partial history | Wrap each table copy in its own SaveChanges; continue-on-error + warn |
| Ordering divergence between providers | In-memory sort post-ToListAsync; golden-data test |
| 30s SCM start timeout on first boot after migrator | Migrator runs as background HostedService, not inline startup |
| Service session-log API currently 500s on LocalDB access | Unrelated to this plan; resolved by phase 5 deploy if it persists |

## Effort

Phase 1: 2h. Phase 2: 3h. Phase 3: 4h. Phase 4: 2h. Phase 5: 1h. Total ~12h.
