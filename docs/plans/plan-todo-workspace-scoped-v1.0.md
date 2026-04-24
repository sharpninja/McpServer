# Plan — Workspace-Scoped TODO Storage + Per-Workspace YAML Bootstrap

**Branch:** `feat/todo-workspace-scoped` (cut from `develop` after #15 merges)
**Depends on:** PR #15 (`feat/todo-provider-agnostic`) merged to `develop`.
**New TR:** TR-MCP-TODO-008 (drafted below, added to `docs/Project/Technical-Requirements.md` as part of this branch).

## Context

TR-MCP-TODO-005 made TODO storage provider-agnostic via `McpDbContext` + `McpDatabaseProviderFactory`, but missed the TR-MCP-MT-003 multi-tenant discriminator that 12+ other entities already have. Result: all workspaces share one TODO pool; per-workspace queries return the union; the server has no way to route per-workspace YAML projection.

Empirical confirmation:
- `TodoItemEntity` / `TodoAuditHistoryEntity` / `TodoDocumentMetadataEntity` have no `WorkspaceId`.
- `McpDbContext.OnModelCreating` lines 276-310 install query filters for 12 entities; skips the three Todo entities.
- `REST /mcpserver/todo` with `X-Workspace: <name>` for each of 9 workspaces returns the same 1-item result.
- `TodoServiceFactory.CreateForWorkspace` comment (line 71-73): "Database provider is process-wide; workspace path is preserved for future projection hooks".

## New TR text (to paste into Technical-Requirements.md)

```markdown
## TR-MCP-TODO-008

**Workspace-Scoped Database-Backed TODO Storage with Per-Workspace YAML Bootstrap** —
Database-backed TODO storage (TR-MCP-TODO-005) SHALL scope every TODO row,
audit-history row, and document-metadata row to the active workspace via a
`WorkspaceId` column populated from the resolved `WorkspaceContext.WorkspacePath`,
matching the TR-MCP-MT-003 multi-tenant pattern used by context, session-log,
agent, tool, and graph entities. `McpDbContext` SHALL install a global query
filter on all three Todo entities so reads, updates, and deletes never cross
workspace boundaries. `TodoItemEntity` SHALL use composite primary key
`(WorkspaceId, Id)` so the same canonical TODO id MAY exist in multiple
workspaces without collision. `TodoDocumentMetadataEntity` SHALL use composite
primary key `(WorkspaceId, SingletonId = 1)` so each workspace owns exactly
one document-metadata singleton. `TodoAuditHistoryEntity` SHALL carry
`WorkspaceId` as a filter column and index; the audit primary key remains
`(TodoId, Version)` scoped implicitly by the query filter.

Bootstrap SHALL import from the per-workspace `TodoFilePath` YAML into the
authoritative database when that workspace's TODO rows are empty, running
exactly once per workspace per marker-file lifetime. The bootstrap path SHALL
preserve ordered sections, completed items, notes, code-review reference, and
projection metadata identically to the single-workspace bootstrap shape used
by `TodoService`. After bootstrap, YAML projection SHALL write to the
workspace-specific `TodoFilePath`; no other workspace's YAML SHALL be touched.

The `LegacyTodoSqliteMigrator` (TR-MCP-TODO-007) SHALL stamp imported rows
with the active workspace's `WorkspacePath`. REST routes `/mcpserver/todo/*`
and MCP STDIO `todo_*` tools SHALL honor the workspace resolved by the
existing `WorkspaceAuthMiddleware` / `X-Workspace` header path without
additional caller changes beyond what TR-MCP-MT-003 already mandates.

**Status:** 🔴 Planned

**Covered by:** `TodoItemEntity`, `TodoAuditHistoryEntity`,
`TodoDocumentMetadataEntity`, `McpDbContext` (query filters + composite keys),
`EfTodoService`, `LegacyTodoSqliteMigrator`, `TodoBootstrapImporter` (new),
`TodoServiceFactory.CreateForWorkspace`, per-provider migration assemblies.
```

## Phases (Byrd process: tests first, then impl)

Every phase gate: entire test suite green before moving forward.

### Phase 0 — New branch + failing-test stubs

- Cut `feat/todo-workspace-scoped` from `develop` (after #15 merges).
- Add `docs/Project/Technical-Requirements.md` TR-MCP-TODO-008 block.
- Add failing test stubs (xUnit `Skip` or `throw new NotImplementedException`):
  - `tests/McpServer.Support.Mcp.Tests/Storage/TodoItemEntity_WorkspaceScopingTests.cs`
  - `tests/McpServer.Support.Mcp.Tests/Storage/TodoDocumentMetadata_CompositePkTests.cs`
  - `tests/McpServer.Support.Mcp.Tests/Services/EfTodoService_WorkspaceIsolationTests.cs`
  - `tests/McpServer.Support.Mcp.Tests/Services/TodoBootstrapImporterTests.cs`
- Commit 0: "test(todo): failing stubs for workspace-scoped TODO (TR-MCP-TODO-008)".

### Phase 1 — Entity + DbContext changes

- Add `public string WorkspaceId { get; set; } = string.Empty;` to the three Todo entities.
- Change `TodoItemEntity` PK to composite `(WorkspaceId, Id)` via Fluent API in `OnModelCreating`. Existing `[Key]` attribute on `Id` removed; use `modelBuilder.Entity<TodoItemEntity>().HasKey(e => new { e.WorkspaceId, e.Id })`.
- Change `TodoDocumentMetadataEntity` PK to `(WorkspaceId, SingletonId)`; check constraint becomes `CK_TodoDocumentMetadata_Singleton` = `"SingletonId" = 1` (per workspace); keep `ValueGeneratedNever` on `SingletonId`.
- `TodoAuditHistoryEntity`: add `WorkspaceId`; PK stays `(TodoId, Version)`; add `HasIndex(e => e.WorkspaceId)`.
- Install global query filters + indexes matching the TR-MCP-MT-003 pattern (lines 276-310 of `McpDbContext`).
- Teach `StampWorkspaceId` to recognize the three Todo entities.
- Phase 1 tests green: entity shape, PK composition, query-filter behavior (against InMemory + SQLite).

### Phase 2 — Generate migrations (4 provider assemblies)

- `dotnet ef migrations add AddTodoWorkspaceScoping --context McpDbContext` against:
  - default `McpServer.Storage`
  - `McpServer.Storage.SqliteMigrations` (env `MCP_EF_PROVIDER=sqlite`)
  - `McpServer.Storage.SqlServerMigrations` (env `MCP_EF_PROVIDER=sqlserver`)
  - `McpServer.Storage.PostgreSqlMigrations` (env `MCP_EF_PROVIDER=postgresql`)
- Hand-edit each migration's `Up()` body to backfill existing rows before the PK change (all existing Todo rows → the McpServer workspace path `F:\GitHub\McpServer`). Example:
  ```sql
  UPDATE TodoItems SET WorkspaceId = 'F:\GitHub\McpServer' WHERE WorkspaceId = '';
  UPDATE TodoAuditHistory SET WorkspaceId = 'F:\GitHub\McpServer' WHERE WorkspaceId = '';
  UPDATE TodoDocumentMetadata SET WorkspaceId = 'F:\GitHub\McpServer' WHERE WorkspaceId = '';
  ```
- SQLite composite PK change: EF emits rebuild-table migration automatically; verify it preserves data.
- SQL Server composite PK change: drop-and-add key; rename check constraint for new composite scope.
- Phase 2 tests: all 4 migration projects build; migration-round-trip test for each provider applies `AddTodoWorkspaceScoping` over `AddTodoStorage` without data loss.

### Phase 3 — EfTodoService + legacy migrator updates

- `EfTodoService`: no query changes expected (global query filter handles it); verify audit queries also pick up the filter. Adjust `CreateAsync` duplicate-id check — it currently queries by `Id` alone; scope to current workspace.
- `LegacyTodoSqliteMigrator`: populate `WorkspaceId` on imported rows from resolved workspace context. For the current service deployment this stamps the existing `PLAN-CLAUDEPLUGIN-001` with the McpServer workspace path (no behavior change post-migration).
- Phase 3 tests: `EfTodoService_WorkspaceIsolationTests` — two workspaces, same `PLAN-BITNETINTEGRATION-001`, both survive; GET from workspace A returns only A's rows; DELETE in A does not affect B.

### Phase 4 — TodoBootstrapImporter (new)

- New class `McpServer.Services.Services.TodoBootstrapImporter` (hosted service or on-demand).
- On `EfTodoService.EnsureWorkspaceBootstrappedAsync(workspacePath)`: if workspace has zero TODO rows AND per-workspace marker file absent, parse the workspace's `TodoFilePath` YAML via existing `TodoYamlFileSerializer`, insert each item with `WorkspaceId = workspacePath`, write marker to `{workspaceDataFolder}/todo-bootstrap.marker`.
- Invoked once per workspace on first REST / MCP touch (cheap per-workspace check in `EfTodoService` constructor or request pipeline).
- Phase 4 tests: `TodoBootstrapImporterTests` — bootstrap runs once, is idempotent, preserves ordered sections, stamps WorkspaceId correctly, no-ops when marker present.

### Phase 5 — Live deploy + import verification

- Redeploy via `Update-McpService.ps1`.
- For each of the 8 workspaces with a TODO YAML, hit `GET /mcpserver/todo?limit=200` with appropriate workspace routing to trigger bootstrap.
- Expected post-bootstrap counts:
  - AspNetServices: 9
  - bitnet-b1.58-sharp: 10
  - CBM-Command: 0
  - FunWasHad: 20
  - McpServer: 36 (migrates in place — existing 1 item retained + 35 new)
  - McpServerManager: 17
  - TruckMate: 14
  - VICE-Sharp: 1
  - Snippets: YAML MISSING → bootstrap no-op, 0 items (confirm with user whether to create empty YAML).
- Total: 107 TODO items across 8 workspaces. `PLAN-BITNETINTEGRATION-001` exists in both `bitnet-b1.58-sharp` and `TruckMate` — no collision under composite PK.

### Phase 6 — Flip TR-MCP-TODO-008 to Complete + PR

- Update TR-MCP-TODO-008 status to ✅ Complete with live verification summary.
- Commit.
- PR against `develop` on Azure DevOps. Target reviewers: same as #15.

## Non-goals

- True normalized `Workspaces` DbSet / FK. Current TR-MCP-MT-003 pattern is a string discriminator; we stay consistent.
- Retroactively re-scoping non-Todo entities — already handled by TR-MCP-MT-003.
- Multi-workspace cross-queries (`admin list all TODOs across workspaces`) — out of scope; can be a future TR.
- Changing YAML projection logic beyond "route to current workspace's YAML path".

## Rollback plan

- Revert the migration: `dotnet ef database update AddTodoStorage --context McpDbContext` drops the `WorkspaceId` column + composite key on each provider.
- Revert entities + DbContext changes.
- Git: revert the PR commit on `develop`; drop `feat/todo-workspace-scoped` branch.

## Critical files

**New:**
- `tests/McpServer.Support.Mcp.Tests/Storage/TodoItemEntity_WorkspaceScopingTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Storage/TodoDocumentMetadata_CompositePkTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Services/EfTodoService_WorkspaceIsolationTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Services/TodoBootstrapImporterTests.cs`
- `src/McpServer.Services/Services/TodoBootstrapImporter.cs`
- 4 new migrations `<timestamp>_AddTodoWorkspaceScoping`

**Edited:**
- `src/McpServer.Storage/Entities/TodoItemEntity.cs`
- `src/McpServer.Storage/Entities/TodoAuditHistoryEntity.cs`
- `src/McpServer.Storage/Entities/TodoDocumentMetadataEntity.cs`
- `src/McpServer.Storage/McpDbContext.cs` (PK Fluent config + query filters + indexes + StampWorkspaceId)
- `src/McpServer.Services/Services/EfTodoService.cs` (duplicate-id check scoping + bootstrap trigger)
- `src/McpServer.Support.Mcp/Services/LegacyTodoSqliteMigrator.cs` (stamp WorkspaceId)
- `docs/Project/Technical-Requirements.md` (add TR-MCP-TODO-008)

## Verification checklist

- [ ] All 4 migration projects build clean (NuGetAudit gate)
- [ ] TR-MCP-TODO-008 tests green on InMemory, SQLite, and SQL Server LocalDB
- [ ] `PLAN-BITNETINTEGRATION-001` coexists in bitnet + TruckMate
- [ ] Each workspace's REST GET returns only that workspace's items
- [ ] Bootstrap marker written per workspace, no re-bootstrap on restart
- [ ] Legacy migrator still passes existing `LegacyTodoSqliteMigratorTests`
- [ ] `PLAN-CLAUDEPLUGIN-001` retained as McpServer-workspace TODO post-migration
- [ ] Full 107-TODO live count matches per-workspace YAML source counts
