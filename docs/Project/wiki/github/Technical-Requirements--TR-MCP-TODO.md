# TR-MCP-TODO

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-TODO-002

**Cross-Workspace TODO Move** — `TodoController.MoveAsync` at `POST /mcpserver/todo/{id}/move` reads the item from the source workspace (resolved via header/API key), creates it in the target workspace (resolved via `IWorkspaceService.GetAsync` + `TodoServiceResolver.Resolve`), then deletes from the source. Request body: `TodoMoveRequest { TargetWorkspacePath }`. Error responses: 400 (null request or unknown target workspace), 404 (item not found), 409 (create failed in target), 500 (created in target but delete from source failed). MCP STDIO parity via `todo_move` tool in `FwhMcpTools`.
**Covered by:** `TodoController`, `FwhMcpTools`, `TodoMoveRequest`, `TodoServiceResolver`, `IWorkspaceService`
**Status:** pending
Scope: layer-1+

## TR-MCP-TODO-003

**GitHub-Backed TODO Creation Alias** — The server SHALL accept `ISSUE-NEW` only on TODO create requests, SHALL immediately create the corresponding GitHub issue, SHALL rewrite the persisted TODO identifier to the canonical `ISSUE-{number}` value returned by GitHub, and SHALL return that canonical identifier to the caller. Persisted TODO validation SHALL accept both `^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+-\d{3}$` and `^ISSUE-\d+$`. Dependency validation SHALL use the same persisted-ID rule set.
The `ISSUE-NEW` flow SHALL be implemented through a shared creation path so HTTP, MCP/STDIO, and voice-driven TODO creation all apply the same rewrite and persistence behavior.

**Status:** ✅ Complete

**Covered by:** `TodoCreationService`, `TodoValidator`, `TodoController`, `FwhMcpTools`, `VoiceConversationService`, `TodoService`, `EfTodoService`
Scope: layer-1+

## TR-MCP-TODO-004

**Shared ISSUE-* TODO Update Orchestration** — All server-side TODO update entry points that can mutate existing TODO items SHALL route `ISSUE-{number}` updates through shared orchestration instead of writing directly to the TODO store. The shared path SHALL suppress description changes after first sync, SHALL reuse the existing TODO store for the local mutation, and SHALL trigger the GitHub sync/comment flow after a successful local update.
**Status:** ✅ Complete

**Covered by:** `TodoUpdateService`, `TodoController`, `FwhMcpTools`, `VoiceConversationService`
Scope: layer-1+

## TR-MCP-TODO-005

**Provider-Agnostic Database-Authoritative TODO Storage with Deterministic YAML Projection** — The TODO subsystem SHALL use the configured `Mcp:Database:Provider` (SQLite, SQL Server, or PostgreSQL) via `McpDatabaseProviderFactory` (TR-MCP-CFG-007) as the authoritative current-state store for workspace TODO items. Service initialization SHALL perform EF Core schema migration, one-time bootstrap import from an existing `TODO.yaml` when the authoritative database is empty, and deterministic projection back to the configured TODO YAML path after successful mutations. The authoritative store SHALL preserve projection metadata needed to rehydrate ordered sections, `code-review-remediation` phases, `notes`, `completed`, and the code-review reference without treating YAML as runtime source of truth.
Projection failures after a committed authoritative mutation SHALL surface an explicit failure result instead of silent success. The TODO storage provider setting SHALL accept `yaml` or `database`; the legacy `sqlite` value SHALL be accepted and aliased to `database` with a one-time warning log. No sqlite-specific settings SHALL be consulted when provider is `database`.

**Status:** ✅ Complete

**Covered by:** `EfTodoService`, `TodoItemEntity`, `TodoAuditHistoryEntity`, `TodoDocumentMetadataEntity`, `McpDbContext` (Todo DbSets), `McpDatabaseProviderFactory`, `TodoYamlFileSerializer`, `TodoServiceFactory`, `TodoStorageOptions`, `McpInstanceResolver`, `appsettings.yaml`, `appsettings.Staging.yaml`, `src/McpServer.Support.Mcp/appsettings.yaml`, `src/McpServer.Support.Mcp/appsettings.Staging.yaml`
Scope: layer-1+

## TR-MCP-TODO-006

**Append-Only TODO Audit History, Projection Failure Classification, and Repair Contract** — TODO create, update, delete, and bootstrap-import operations SHALL append reconstructable audit snapshots with monotonic per-item versions. The server SHALL expose `GET /mcpserver/todo/{id}/audit` together with typed client parity and MCP STDIO tool parity so callers can retrieve ordered tracked states for a TODO item even when the current row has been deleted but audit history still exists.
Mutation results SHALL include a machine-readable failure classification so callers can distinguish validation, not-found, projection-failure, conflict, and external-sync error shapes when TODO operations fail or only partially succeed. For database-backed TODO storage (the authoritative mode per TR-MCP-TODO-005), a projection failure SHALL preserve committed authoritative database state, record operator-visible projection failure metadata, and leave `TODO.yaml` repairable without replaying the mutation. The server SHALL expose `GET /mcpserver/todo/projection/status` and `POST /mcpserver/todo/projection/repair` together with typed client parity and MCP STDIO tool parity so operators can verify whether `TODO.yaml` matches authoritative database state and rebuild it on demand.

**Status:** ✅ Complete

**Covered by:** `ITodoService`, `ITodoStore`, `EfTodoService`, `TodoAuditHistoryEntity`, `TodoYamlFileSerializer`, `TodoController`, `McpServerMcpTools`, `TodoClient`, `TodoModels`, `TodoCreationService`, `TodoUpdateService`
Scope: layer-1+

## TR-MCP-TODO-007

**Legacy SQLite TODO Storage One-Shot Migration** — When TR-MCP-TODO-005 provider-agnostic storage is enabled and a pre-existing legacy `mcp.db` SQLite TODO store is present at the deprecated `Mcp:TodoStorage:SqliteDataSource` path, the server SHALL copy rows from `todo_items`, `todo_item_history`, and `todo_document_metadata` into the configured authoritative database on first boot, preserving primary keys, audit identifiers, and monotonic per-item versions. The migrator SHALL be idempotent: subsequent starts SHALL be no-ops when the target TODO tables are non-empty or the completion marker file exists in the effective data folder. The migrator SHALL honor the `Mcp:TodoStorage:MigrateFromLegacySqlite` feature flag and SHALL run as a background hosted service so it never blocks the SCM 30-second service-start window. Failures SHALL log per-row context and continue with the next row rather than aborting the whole migration.
**Status:** ✅ Superseded by TR-MCP-TODO-008

**Note:** In practice the provider-agnostic rewrite (TR-MCP-TODO-005) was shipped alongside the workspace-scoped schema (TR-MCP-TODO-008), whose per-workspace YAML bootstrap (`TodoBootstrapImporter`) replaces the need for a legacy `mcp.db` row copy. Live Phase 5 deploys on LEGION2 and PAYTON-DESKTOP (2026-04-21) reconstructed authoritative state from workspace YAML directly; legacy SQLite TODO rows are out-of-band archived to CSV when present (see `C:\Users\Public\mcpserver-postgres-archive-20260421` on PAYTON for the postgres/SQLite snapshot archived during deploy).

**Covered by:** `TodoBootstrapImporter` (replaces legacy migrator), per-workspace YAML bootstrap path
Scope: layer-1+

## TR-MCP-TODO-008

**Workspace-Scoped Database-Backed TODO Storage with Per-Workspace YAML Bootstrap** — Database-backed TODO storage (TR-MCP-TODO-005) SHALL scope every TODO row, audit-history row, and document-metadata row to the active workspace via a `WorkspaceId` column populated from the resolved `WorkspaceContext.WorkspacePath`, matching the TR-MCP-MT-003 multi-tenant pattern used by context, session-log, agent, tool, and graph entities. `McpDbContext` SHALL install a global query filter on all three Todo entities so reads, updates, and deletes never cross workspace boundaries. `TodoItemEntity` SHALL use composite primary key `(WorkspaceId, Id)` so the same canonical TODO id MAY exist in multiple workspaces without collision. `TodoDocumentMetadataEntity` SHALL use composite primary key `(WorkspaceId, SingletonId = 1)` so each workspace owns exactly one document-metadata singleton. `TodoAuditHistoryEntity` SHALL carry `WorkspaceId` as a filter column and index; the audit primary key remains `(TodoId, Version)` scoped implicitly by the query filter.
Bootstrap SHALL import from the per-workspace `TodoFilePath` YAML into the authoritative database when that workspace's TODO rows are empty, running exactly once per workspace per marker-file lifetime. The bootstrap path SHALL preserve ordered sections, completed items, notes, code-review reference, and projection metadata identically to the single-workspace bootstrap shape used by `TodoService`. After bootstrap, YAML projection SHALL write to the workspace-specific `TodoFilePath`; no other workspace's YAML SHALL be touched.

The `LegacyTodoSqliteMigrator` (TR-MCP-TODO-007) SHALL stamp imported rows with the active workspace's `WorkspacePath`. REST routes `/mcpserver/todo/*` and MCP STDIO `todo_*` tools SHALL honor the workspace resolved by the existing `WorkspaceAuthMiddleware` / `X-Workspace` header path without additional caller changes beyond what TR-MCP-MT-003 already mandates.

**Status:** ✅ Complete

**Note:** Phase 5 live deploy verified on 2026-04-21: LEGION2 bootstrapped 130 items across 7 workspaces; PAYTON-DESKTOP bootstrapped 96 items across 5 workspaces (workspace scoping confirmed via `TodoBootstrapImporter summary` log lines with per-workspace `Imported:N` outcomes).

**Covered by:** `TodoItemEntity`, `TodoAuditHistoryEntity`, `TodoDocumentMetadataEntity`, `McpDbContext` (query filters + composite keys), `EfTodoService`, `TodoBootstrapImporter`, `TodoServiceFactory.CreateForWorkspace`, per-provider migration assemblies
Scope: layer-1+

## TR-MCP-TODO-009

**Preserve TODO description Markdown** — TODO persistence, plugin/client update paths, database storage, audit rows, and informational projections must treat description as Markdown, preserving blank lines, indentation, code fences, list spacing, and trailing content without trimming meaningful formatting.
**Covered by:** FR: FR-MCP-108; TEST: TEST-MCP-144
**Status:** pending
Scope: layer-1+

## TR-MCP-TODO-010

**Root-scoped TODO done serialization** — TODO update serializers in MCP plugin wrappers SHALL read the parent done field only from the request root and SHALL NOT derive it from nested implementationTasks[].done values. Structured root-level parsing is required for boolean root fields when building HTTP or workflow update bodies.
**Covered by:** FR: FR-MCP-114; TEST: TEST-MCP-154
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] Root-level done serialization ignores nested implementationTasks[].done values. (evidence: Plugin tests/repl-invoke-shim.bats now asserts no top-level done is emitted when only implementation task done values are present.)

