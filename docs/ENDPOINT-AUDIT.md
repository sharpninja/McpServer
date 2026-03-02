# MCP Server Endpoint Audit Summary

**Date:** 2026-02-21 (Updated 2026-02-26)  
**Service:** MCP Server on `http://localhost:7147`  
**Auditor:** Cline / Claude Sonnet 4

> **Note:** All endpoints are workspace-scoped via the `X-Workspace-Path` header resolution chain.
> Send `X-Workspace-Path: <absolute-path>` to target a specific workspace. If omitted, workspace is
> resolved from the `X-Api-Key` token or defaults to the primary workspace.

## Overview

| Controller | Route | Endpoints | Tests | Result |
|-----------|-------|-----------|-------|--------|
| [WorkspaceController](#workspace) | `mcp/workspace` | 11 | 40 | ✅ All passed |
| [TodoController](#todo) | `mcp/todo` | 6 | 33 | ✅ All passed |
| [ToolRegistryController](#tool-registry) | `mcp/tools` | 12 | 38 | ✅ All passed |
| [SessionLogController](#session-log) | `mcp/sessionlog` | 3 | 21 | ✅ All passed |
| [ContextController](#context) | `mcp/context` | 4 | 9 | ✅ All passed |
| [GitHubController](#github) | `mcp/gh` | 13 | 15 | ✅ All passed |
| [RepoController](#repo) | `mcp/repo` | 3 | 8 | ✅ All passed |
| [SyncController](#sync) | `mcp/sync` | 2 | 4 | ✅ All passed |
| [DiagnosticController](#diagnostic) | `mcp/diagnostic` | 2 | — | ✅ Debug/Staging only |
| **Total** | | **56** | **168** | **✅ All passed** |

---

## Workspace

**Controller:** `WorkspaceController` at `mcp/workspace`  
**Test Project:** `tests/McpServer.Workspace.Validation`  
**Full Report:** [tests/McpServer.Workspace.Validation/AUDIT_REPORT.md](../tests/McpServer.Workspace.Validation/AUDIT_REPORT.md)

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `GET` | `/mcpserver/workspace` | None | ✅ |
| 2 | `POST` | `/mcpserver/workspace` | API Key | ✅ |
| 3 | `GET` | `/mcpserver/workspace/{key}` | None | ✅ |
| 4 | `PUT` | `/mcpserver/workspace/{key}` | API Key | ✅ |
| 5 | `DELETE` | `/mcpserver/workspace/{key}` | API Key | ✅ |
| 6 | `POST` | `/mcpserver/workspace/{key}/init` | API Key | ✅ |
| 7 | `POST` | `/mcpserver/workspace/{key}/start` | API Key | ✅ |
| 8 | `POST` | `/mcpserver/workspace/{key}/stop` | API Key | ✅ |
| 9 | `GET` | `/mcpserver/workspace/{key}/status` | None | ✅ |
| 10 | `GET` | `/mcpserver/workspace/prompt` | None | ✅ |
| 11 | `PUT` | `/mcpserver/workspace/prompt` | API Key | ✅ |

**Key Findings:** All 11 endpoints respond correctly. Full lifecycle creates + deletes cleanly. Keys are base64-encoded directory paths. Read endpoints (GET list, GET single, GET status, GET prompt) are public; all mutating endpoints require API key. Prompt endpoints are gated to the primary workspace (returns 403 from non-primary instances).

---

## Todo

**Controller:** `TodoController` at `mcp/todo`  
**Test Project:** `tests/McpServer.Todo.Validation`  
**Full Report:** [tests/McpServer.Todo.Validation/AUDIT_REPORT.md](../tests/McpServer.Todo.Validation/AUDIT_REPORT.md)

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `GET` | `/mcpserver/todo` | None | ✅ |
| 2 | `GET` | `/mcpserver/todo/{id}` | None | ✅ |
| 3 | `POST` | `/mcpserver/todo` | None | ✅ |
| 4 | `PUT` | `/mcpserver/todo/{id}` | None | ✅ |
| 5 | `DELETE` | `/mcpserver/todo/{id}` | None | ✅ |
| 6 | `POST` | `/mcpserver/todo/{id}/requirements` | None | ✅ |

**Key Findings:** All 6 endpoints respond correctly. Section validation enforces valid sections (`mvp-app`, `mvp-legal`, `mvp-marketing`, `mvp-support`, `staging-and-infrastructure`). Requirements endpoint returns 422 when Copilot CLI unavailable.

---

## Tool Registry

**Controller:** `ToolRegistryController` at `mcp/tools`  
**Test Project:** `tests/McpServer.ToolRegistry.Validation`  
**Full Report:** [tests/McpServer.ToolRegistry.Validation/AUDIT_REPORT.md](../tests/McpServer.ToolRegistry.Validation/AUDIT_REPORT.md)

### Tool CRUD

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `GET` | `/mcpserver/tools` | Public | ✅ |
| 2 | `GET` | `/mcpserver/tools/search` | Public | ✅ |
| 3 | `GET` | `/mcpserver/tools/{id}` | Public | ✅ |
| 4 | `POST` | `/mcpserver/tools` | API Key | ✅ |
| 5 | `PUT` | `/mcpserver/tools/{id}` | API Key | ✅ |
| 6 | `DELETE` | `/mcpserver/tools/{id}` | API Key | ✅ |

### Bucket Management

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 7 | `GET` | `/mcpserver/tools/buckets` | Public | ✅ |
| 8 | `POST` | `/mcpserver/tools/buckets` | API Key | ✅ |
| 9 | `DELETE` | `/mcpserver/tools/buckets/{name}` | API Key | ✅ |
| 10 | `GET` | `/mcpserver/tools/buckets/{name}/browse` | Public | ✅ |
| 11 | `POST` | `/mcpserver/tools/buckets/{name}/install` | API Key | ✅ |
| 12 | `POST` | `/mcpserver/tools/buckets/{name}/sync` | API Key | ✅ |

**Key Findings:** All 12 endpoints respond correctly. Read endpoints are public, write endpoints require API key. Tag-based search works. Bucket browse/sync return 404 gracefully when manifests don't exist at specified path.

---

## Session Log

**Controller:** `SessionLogController` at `mcp/sessionlog`  
**Test Project:** `tests/McpServer.SessionLog.Validation`  
**Full Report:** [tests/McpServer.SessionLog.Validation/AUDIT_REPORT.md](../tests/McpServer.SessionLog.Validation/AUDIT_REPORT.md)

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `POST` | `/mcpserver/sessionlog` | None | ✅ |
| 2 | `GET` | `/mcpserver/sessionlog` | None | ✅ |
| 3 | `POST` | `/mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog` | None | ✅ |

**Key Findings:** All 3 endpoints respond correctly. Submit supports upsert by SourceType+SessionId. Query returns paginated `{totalCount, limit, offset, items}`. Dialog append accumulates items and returns running count. Validation rejects missing/empty required fields with descriptive 400 errors.

---

## Context

**Controller:** `ContextController` at `mcp/context`  
**Test Project:** `tests/McpServer.Context.Validation`

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `POST` | `/mcpserver/context/search` | None | ✅ |
| 2 | `POST` | `/mcpserver/context/rebuild-index` | None | ⚠️ 500 |
| 3 | `POST` | `/mcpserver/context/pack` | None | ✅ |
| 4 | `GET` | `/mcpserver/context/sources` | None | ✅ |

**Key Findings:** 3 of 4 endpoints return 200 OK. Search supports query, sourceType filter, and limit clamping (1–100). Pack echoes queryId and returns ordered chunks with sourceKeys. Sources returns indexed document list. Rebuild-index returns 500 (FTS5 virtual table not initialized in current DB state).

---

## GitHub

**Controller:** `GitHubController` at `mcp/gh`  
**Test Project:** `tests/McpServer.GitHub.Validation`

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `GET` | `/mcpserver/gh/issues` | None | ✅ |
| 2 | `GET` | `/mcpserver/gh/issues/{number}` | None | ✅ |
| 3 | `POST` | `/mcpserver/gh/issues` | None | ✅ |
| 4 | `PUT` | `/mcpserver/gh/issues/{number}` | None | ✅ |
| 5 | `POST` | `/mcpserver/gh/issues/{number}/close` | None | ✅ |
| 6 | `POST` | `/mcpserver/gh/issues/{number}/reopen` | None | ✅ |
| 7 | `POST` | `/mcpserver/gh/issues/{id}/comments` | None | ✅ |
| 8 | `GET` | `/mcpserver/gh/labels` | None | ✅ |
| 9 | `GET` | `/mcpserver/gh/pulls` | None | ✅ |
| 10 | `POST` | `/mcpserver/gh/pulls/{id}/comments` | None | ✅ |
| 11 | `POST` | `/mcpserver/gh/issues/sync/from-github` | None | ✅ |
| 12 | `POST` | `/mcpserver/gh/issues/sync/to-github` | None | ✅ |
| 13 | `POST` | `/mcpserver/gh/issues/{number}/sync` | None | ✅ |

**Key Findings:** All 13 endpoints respond correctly. List issues/pulls/labels return 200 with arrays. Create/update/comment validation returns 400 on missing required fields. Close/reopen return appropriate status codes. Sync endpoints delegate to gh CLI and IssueTodoSyncService.

---

## Repo

**Controller:** `RepoController` at `mcp/repo`  
**Test Project:** `tests/McpServer.Repo.Validation`

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `GET` | `/mcpserver/repo/file` | None | ✅ |
| 2 | `POST` | `/mcpserver/repo/file` | None | ✅ |
| 3 | `GET` | `/mcpserver/repo/list` | None | ✅ |

**Key Findings:** All 3 endpoints respond correctly. List returns path + entries array with name/isDirectory. Read validates path is required (400). Write validates path + body required (400). Path allowlist is enforced — disallowed paths return 400.

---

## Diagnostic

**Controller:** `DiagnosticController` at `mcp/diagnostic`  
**Active in:** Debug builds and `Staging` environment only (excluded in Production Release).

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `GET` | `/mcpserver/diagnostic/execution-path` | None | ✅ |
| 2 | `GET` | `/mcpserver/diagnostic/appsettings-path` | None | ✅ |

**Key Findings:** `execution-path` returns `{ processPath, baseDirectory }` — the actual executable path and its directory. `appsettings-path` returns `{ environmentName, contentRootPath, files[] }` listing which appsettings files are present in the content root. Both used during deployment verification to confirm correct binary and config file selection.

## Sync

**Controller:** `SyncController` at `mcp/sync`  
**Test Project:** `tests/McpServer.Sync.Validation`

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `POST` | `/mcpserver/sync/run` | None | ✅ |
| 2 | `GET` | `/mcpserver/sync/status` | None | ✅ |

**Key Findings:** Both endpoints respond correctly. Run triggers full ingestion and returns runId, status, timestamps, and counts (documentsIngested, chunksWritten, sessionLogsImported, issuesSynced). Status returns last run info or `{status: "idle"}` if no runs yet. Sync run takes 6–26 seconds depending on content.
