# TR-MCP-MT

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-MT-001

**WorkspaceContext Scoped Per-Request Service** — `WorkspaceContext` is a scoped service holding resolved workspace identity: `WorkspacePath`, `WorkspaceName`, `DataDirectory`, `TodoFilePath`, `SessionsPath`, `ExternalDocsPath`, `IsDefaultKey`, `IsResolved`. Populated by `WorkspaceResolutionMiddleware` before downstream services execute. Downstream services inject `WorkspaceContext` instead of reading `IConfiguration["Mcp:RepoRoot"]`.
**Covered by:** `WorkspaceContext`, `WorkspaceResolutionMiddleware`
**Status:** pending
Scope: layer-1+

## TR-MCP-MT-002

**WorkspaceResolutionMiddleware** — Runs before `WorkspaceAuthMiddleware` in the pipeline. Only activates for `/mcpserver/*` and `/mcp-transport` routes. Resolution chain: (1) `X-Workspace-Path` header validated against registered workspaces - returns 400 for unregistered paths; (2) API key reverse lookup via `WorkspaceTokenService.ResolveWorkspaceByToken()`; (3) `Mcp:RepoRoot` config fallback; (4) primary workspace from workspace list. Populates `WorkspaceContext` scoped service.
**Covered by:** `WorkspaceResolutionMiddleware`, `WorkspaceContext`, `WorkspaceTokenService`
**Status:** pending
Scope: layer-1+

## TR-MCP-MT-003

**EF Core Global Query Filter for WorkspaceId** — `McpDbContext` accepts optional `WorkspaceContext` to capture `_workspaceId` per-instance. `OnModelCreating` applies `.HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId)` on all 14 entity types. Empty `_workspaceId` disables filtering (backward compatible). `IgnoreQueryFilters()` escapes for cross-workspace admin queries. `WorkspaceId TEXT NOT NULL DEFAULT ''` column with indexes on all entity tables.
**Covered by:** `McpDbContext`, all entity types (`WorkspaceId` property)
**Status:** pending
Scope: layer-1+

## TR-MCP-MT-003A

`SessionLogService` injects an optional `WorkspaceContext` and stamps `WorkspaceId` on every entity it persists. When the context is null (ingestion / batch import path), the service skips stamping and relies on `McpDbContext.SaveChangesAsync` to auto-fill `WorkspaceId` for Added entities from the DbContext's resolved `_workspaceId`. This ensures POST/GET round-trips work under the same workspace context AND existing rows with empty WorkspaceId remain visible when no workspace header is set.
**Covered by:** FR: FR-SUPPORT-010A, FR-SUPPORT-010C, FR-SUPPORT-011; TEST: TEST-SUPPORT-010A-1, TEST-SUPPORT-010A-2, TEST-SUPPORT-010C-1, TEST-SUPPORT-010C-2, TEST-SUPPORT-010C-3
**Status:** pending
Scope: layer-1+

## TR-MCP-MT-004

**WorkspaceId stamping in SessionLogService** — SessionLogService injects an optional WorkspaceContext and stamps WorkspaceId on every entity it persists. When the context is null (ingestion/batch import path), the service skips stamping and relies on McpDbContext.SaveChangesAsync to auto-fill WorkspaceId for Added entities from the DbContext's resolved _workspaceId. This ensures POST/GET round-trips work under the same workspace context AND existing rows with empty WorkspaceId remain visible when no workspace header is set.
**Covered by:** FR: FR-SUPPORT-011; TEST: TEST-SUPPORT-010A-1, TEST-SUPPORT-010A-2
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] SessionLogService stamps WorkspaceId on entities when WorkspaceContext is injected
- [ ] SessionLogService skips stamping and delegates to McpDbContext when WorkspaceContext is null
- [ ] POST/GET round-trips work correctly under the same workspace context
- [ ] Existing rows with empty WorkspaceId remain visible when no workspace header is set

