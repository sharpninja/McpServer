# MCP Server Endpoint Audit Summary

**Date:** 2026-02-21  
**Service:** MCP Server on `http://localhost:7147`  
**Auditor:** Cline / Claude Sonnet 4

## Overview

| Controller | Route | Endpoints | Tests | Result |
|-----------|-------|-----------|-------|--------|
| [WorkspaceController](#workspace) | `mcp/workspace` | 9 | 40 | ✅ All passed |
| [TodoController](#todo) | `mcp/todo` | 6 | 33 | ✅ All passed |
| [ToolRegistryController](#tool-registry) | `mcp/tools` | 12 | 38 | ✅ All passed |
| [SessionLogController](#session-log) | `mcp/sessionlog` | 3 | 21 | ✅ All passed |
| **Total** | | **30** | **132** | **✅ All passed** |

### Remaining (not yet audited)

| Controller | Route | Endpoints |
|-----------|-------|-----------|
| ContextController | `mcp/context` | 4 |
| GitHubController | `mcp/github` | 13 |
| RepoController | `mcp/repo` | 3 |
| SyncController | `mcp/sync` | 2 |

---

## Workspace

**Controller:** `WorkspaceController` at `mcp/workspace`  
**Test Project:** `tests/McpServer.Workspace.Validation`  
**Full Report:** [tests/McpServer.Workspace.Validation/AUDIT_REPORT.md](../tests/McpServer.Workspace.Validation/AUDIT_REPORT.md)

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `GET` | `/mcp/workspace` | None | ✅ |
| 2 | `POST` | `/mcp/workspace` | None | ✅ |
| 3 | `GET` | `/mcp/workspace/{key}` | None | ✅ |
| 4 | `PUT` | `/mcp/workspace/{key}` | None | ✅ |
| 5 | `DELETE` | `/mcp/workspace/{key}` | None | ✅ |
| 6 | `POST` | `/mcp/workspace/{key}/init` | None | ✅ |
| 7 | `POST` | `/mcp/workspace/{key}/start` | None | ✅ |
| 8 | `POST` | `/mcp/workspace/{key}/stop` | None | ✅ |
| 9 | `GET` | `/mcp/workspace/{key}/status` | None | ✅ |

**Key Findings:** All 9 endpoints respond correctly. Full lifecycle creates + deletes cleanly. Keys are base64-encoded directory paths.

---

## Todo

**Controller:** `TodoController` at `mcp/todo`  
**Test Project:** `tests/McpServer.Todo.Validation`  
**Full Report:** [tests/McpServer.Todo.Validation/AUDIT_REPORT.md](../tests/McpServer.Todo.Validation/AUDIT_REPORT.md)

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `GET` | `/mcp/todo` | None | ✅ |
| 2 | `GET` | `/mcp/todo/{id}` | None | ✅ |
| 3 | `POST` | `/mcp/todo` | None | ✅ |
| 4 | `PUT` | `/mcp/todo/{id}` | None | ✅ |
| 5 | `DELETE` | `/mcp/todo/{id}` | None | ✅ |
| 6 | `POST` | `/mcp/todo/{id}/requirements` | None | ✅ |

**Key Findings:** All 6 endpoints respond correctly. Section validation enforces valid sections (`mvp-app`, `mvp-legal`, `mvp-marketing`, `mvp-support`, `staging-and-infrastructure`). Requirements endpoint returns 422 when Copilot CLI unavailable.

---

## Tool Registry

**Controller:** `ToolRegistryController` at `mcp/tools`  
**Test Project:** `tests/McpServer.ToolRegistry.Validation`  
**Full Report:** [tests/McpServer.ToolRegistry.Validation/AUDIT_REPORT.md](../tests/McpServer.ToolRegistry.Validation/AUDIT_REPORT.md)

### Tool CRUD

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `GET` | `/mcp/tools` | Public | ✅ |
| 2 | `GET` | `/mcp/tools/search` | Public | ✅ |
| 3 | `GET` | `/mcp/tools/{id}` | Public | ✅ |
| 4 | `POST` | `/mcp/tools` | API Key | ✅ |
| 5 | `PUT` | `/mcp/tools/{id}` | API Key | ✅ |
| 6 | `DELETE` | `/mcp/tools/{id}` | API Key | ✅ |

### Bucket Management

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 7 | `GET` | `/mcp/tools/buckets` | Public | ✅ |
| 8 | `POST` | `/mcp/tools/buckets` | API Key | ✅ |
| 9 | `DELETE` | `/mcp/tools/buckets/{name}` | API Key | ✅ |
| 10 | `GET` | `/mcp/tools/buckets/{name}/browse` | Public | ✅ |
| 11 | `POST` | `/mcp/tools/buckets/{name}/install` | API Key | ✅ |
| 12 | `POST` | `/mcp/tools/buckets/{name}/sync` | API Key | ✅ |

**Key Findings:** All 12 endpoints respond correctly. Read endpoints are public, write endpoints require API key. Tag-based search works. Bucket browse/sync return 404 gracefully when manifests don't exist at specified path.

---

## Session Log

**Controller:** `SessionLogController` at `mcp/sessionlog`  
**Test Project:** `tests/McpServer.SessionLog.Validation`  
**Full Report:** [tests/McpServer.SessionLog.Validation/AUDIT_REPORT.md](../tests/McpServer.SessionLog.Validation/AUDIT_REPORT.md)

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `POST` | `/mcp/sessionlog` | None | ✅ |
| 2 | `GET` | `/mcp/sessionlog` | None | ✅ |
| 3 | `POST` | `/mcp/sessionlog/{agent}/{sessionId}/{requestId}/dialog` | None | ✅ |

**Key Findings:** All 3 endpoints respond correctly. Submit supports upsert by SourceType+SessionId. Query returns paginated `{totalCount, limit, offset, items}`. Dialog append accumulates items and returns running count. Validation rejects missing/empty required fields with descriptive 400 errors.
