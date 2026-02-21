# Requirements Traceability

This document lists all functional requirements (FR) and technical requirements (TR) for MCP Server, including traceability to source code.

---

## Functional Requirements

### FR-SUPPORT-010: MCP Context Unification

Local MCP server providing context retrieval, TODO management, repository access, session logging, and sync capabilities for AI agent integration.

**Covered by:**

- `ContextController` — hybrid search, context packs, index rebuild
- `TodoController` — CRUD and query for TODO items
- `RepoController` — file read/write/list with path allowlist and audit
- `SessionLogController` — session log submit, query, and dialog append
- `SyncController` — ingestion trigger and status
- `McpServerMcpTools` — STDIO transport exposing same capabilities
- `McpDbContext` — SQLite persistence for documents, chunks, session logs
- `HybridSearchService` — FTS5 + HNSW vector fusion with BM25 scoring
- `EmbeddingService` — ONNX all-MiniLM-L6-v2 embeddings (384-dim)
- `VectorIndexService` — HNSW approximate nearest-neighbor
- `Fts5SearchService` — SQLite FTS5 full-text search
- `RepoFileService` — path allowlist enforcement and write audit
- `WriteAuditLog` — in-memory audit trail for repo writes
- `IngestionCoordinator` — orchestrates repo, session log, GitHub, and external docs ingestion

### FR-SUPPORT-013: GitHub Integration

Automatic TODO tracking with GitHub Issues, bidirectional sync, and GitHub metadata indexing for semantic search.

**Covered by:**

- `GitHubController` — issue/PR CRUD, sync, label endpoints
- `GitHubCliService` — `gh` CLI wrapper
- `IssueTodoSyncService` — bidirectional issue ↔ TODO sync with `ISSUE-{number}` IDs
- `IssueIngestor` — indexes GitHub issues for context search
- `GitHubIngestor` — ingests GitHub issues and PRs

### FR-MCP-009: Workspace Management

Dynamic workspace registration, configuration, and lifecycle management replacing static instance configuration.

**Covered by:**

- `WorkspaceController` — CRUD, init, start/stop, status endpoints
- `WorkspaceService` — auto-port assignment, init scaffolding, CRUD
- `WorkspaceEntity` — EF Core entity with WorkspacePath PK

### FR-MCP-011: Workspace Process Orchestration

The main server spawns and manages child MCP processes per workspace with process lifecycle tracking.

**Covered by:**

- `WorkspaceProcessManager` — child process spawn, stop, status (PID, uptime, port)
- `IWorkspaceProcessManager` — `IHostedService` for graceful shutdown

### FR-MCP-012: Tool Registry *(NEW)*

Agents can discover tools by keyword search across global and workspace-scoped tool definitions, with GitHub-backed bucket repositories for tool distribution.

**Covered by:**

- `ToolRegistryController` — search, CRUD, bucket endpoints
- `ToolRegistryService` — keyword search (tags, name, description), CRUD
- `ToolBucketService` — GitHub repo browsing, install, sync via `gh api`
- `ToolDefinitionEntity`, `ToolDefinitionTagEntity`, `ToolBucketEntity`

### FR-MCP-013: API Key Authentication *(NEW)*

Protect mutating API endpoints with an API key while keeping read endpoints publicly accessible.

**Covered by:**

- `ApiKeyAuthFilter` — `IAsyncActionFilter` checking `Mcp:ApiKey` config
- `SkipApiKeyAuthAttribute` — bypass marker for public endpoints
- Applied to `WorkspaceController` and `ToolRegistryController`

### FR-MCP-014: Pairing Web UI *(NEW)*

Browser-based login flow for authorized users to retrieve the server API key for MCP client configuration.

**Covered by:**

- `PairingHtml` — login form, API key display, not-configured HTML templates
- `PairingOptions` — binds `Mcp:ApiKey` and `Mcp:PairingUsers`
- `PairingSessionService` — session cookie management
- `/pair`, `/pair/key` endpoints in `Program.cs`

### FR-MCP-015: Tunnel Providers *(NEW)*

Expose the local MCP server to the internet via pluggable tunnel providers for remote agent access.

**Covered by:**

- `ITunnelProvider` — strategy interface (`IHostedService` + `GetStatusAsync`)
- `NgrokTunnelProvider` — ngrok CLI integration with env-var auth
- `CloudflareTunnelProvider` — cloudflared quick/named tunnel
- `FrpTunnelProvider` — FRP client with generated TOML config
- `TunnelOptions` — configuration for all three providers

### FR-MCP-016: MCP Streamable HTTP Transport *(NEW)*

Native MCP protocol endpoint coexisting with REST API on the same port, enabling standard MCP client connections.

**Covered by:**

- `app.MapMcp("/mcp-transport")` in `Program.cs`
- `ModelContextProtocol.AspNetCore` package integration
- `McpServerMcpTools` — shared tool implementations

### FR-MCP-017: Windows Service *(NEW)*

Run the MCP server as a Windows service with automatic startup, failure recovery, and gsudo-based management.

**Covered by:**

- `UseWindowsService(options => { options.ServiceName = "McpServer"; })` in `Program.cs`
- `scripts/Manage-McpService.ps1` — Install/Uninstall/Start/Stop/Restart/Status/Publish

### FR-LOC-001: Localization Support

Localization and internationalization support for the MCP server.

**Status:** Referenced in codebase, implementation scope TBD.

---

## Technical Requirements

### TR-PLANNED-013: MCP Server Core Implementation

Core technical implementation covering all MCP server infrastructure: middleware, storage, indexing, ingestion, logging, and service architecture.

**Covers:**

- ASP.NET Core middleware pipeline (Serilog, interaction logging, Swagger)
- EF Core SQLite with migrations (documents, chunks, session logs, workspaces, tools)
- FTS5 full-text indexing and HNSW vector indexing
- ONNX embedding generation (all-MiniLM-L6-v2)
- Content chunking and ingestion pipeline
- YAML and SQLite TODO storage backends
- Process runner abstraction for external CLI tools
- Channel-based async interaction log submission
- Session log file watcher (hosted service)
- Parseable log sink (optional Docker-based log aggregation)

### TR-GH-013-001: GitHub Issue Detail Model

Full issue detail model including body, labels, assignees, timestamps, and comments for MCP integration.

### TR-GH-013-002: GitHub to TODO Sync

Sync engine that pulls GitHub issues into TODO items with `ISSUE-{number}` ID convention.

### TR-GH-013-003: TODO to GitHub Sync

Reverse sync that pushes TODO status changes (done/not-done) back to GitHub (close/reopen).

### TR-GH-013-004: GitHub Issue Ingestion

Indexes GitHub issues with full detail (title, body, comments) into the context store for semantic search.

### TR-GH-013-005: Issue Note Frontmatter

Frontmatter convention for `ISSUE-*` TODO note fields linking back to GitHub issue metadata.

### TR-GH-013-006: GitHub Sync Controller Endpoints

REST endpoints for triggering issue pull/push sync operations.

### TR-MCP-WS-002: Workspace Service

Workspace CRUD operations, auto-port assignment (base 7148, increment from max), and init scaffolding (directory creation, todo.yaml, mcp.db).

### TR-MCP-WS-003: Workspace Process Manager

Child process management using `System.Diagnostics.Process` with `IHostedService` for graceful shutdown. Tracks PID, uptime, and port per workspace.

### TR-MCP-WS-004: Workspace Controller

REST API at `/mcp/workspace` with Base64URL-encoded path keys, 9 endpoints, and `[ApiKeyAuthFilter]` with `[SkipApiKeyAuth]` on read endpoints.

### TR-MCP-TR-001: Tool Registry Service *(NEW)*

Keyword search across tags (bidirectional contains for singular/plural tolerance), name, and description. Results combine global tools (`WorkspacePath == null`) with workspace-scoped tools.

### TR-MCP-TR-002: Tool Bucket Service *(NEW)*

GitHub repository browsing via `gh api /repos/{owner}/{repo}/contents{path}?ref={branch}`. Manifest parsing, install, and sync operations.

### TR-MCP-SEC-001: API Key Authentication Filter *(NEW)*

`IAsyncActionFilter` that reads `Mcp:ApiKey` from config, checks `X-Api-Key` header or `api_key` query parameter. `SkipApiKeyAuthAttribute` checked via endpoint metadata for bypass.

### TR-MCP-SEC-002: Pairing Session Security *(NEW)*

SHA-256 password verification using `CryptographicOperations.FixedTimeEquals` for constant-time comparison. HttpOnly session cookies with Secure flag on HTTPS.

### TR-MCP-TUN-001: Tunnel Strategy Pattern *(NEW)*

Factory delegate in DI registration reads `Mcp:Tunnel:Provider`, uppercases, and switches to `ActivatorUtilities.CreateInstance<T>`. Registered as singleton + `IHostedService` conditionally.

### TR-MCP-TUN-002: Tunnel Process Lifecycle *(NEW)*

TOCTOU-safe process management: `Process.Kill()` wrapped in try-catch for `InvalidOperationException`. `WaitForExit(5000)` timeout on stop. Config file cleanup for FRP.

### TR-MCP-TUN-003: Ngrok Auth Token Security *(NEW)*

Auth token passed via `NGROK_AUTHTOKEN` environment variable instead of CLI arguments to prevent exposure in process listings.

### TR-MCP-HTTP-001: MCP Streamable HTTP Endpoint *(NEW)*

`app.MapMcp("/mcp-transport")` at a separate path from REST routes (`/mcp/*`). Requires `Accept: application/json, text/event-stream` header; returns 406 without it.

### TR-MCP-SVC-001: Windows Service Configuration *(NEW)*

`UseWindowsService()` with explicit `ServiceName = "McpServer"`. Self-contained single-file publish to `C:\ProgramData\McpServer`. Recovery policy: restart on failure with 60s delay.

### TR-LOC-001: Localization Infrastructure

Localization infrastructure for multi-language support.

**Status:** Referenced in codebase, implementation scope TBD.

---

## Requirements Matrix

| Requirement | Status | Source Files |
|-------------|--------|-------------|
| FR-SUPPORT-010 | ✅ Complete | Controllers/*, Services/*, Indexing/*, Ingestion/* |
| FR-SUPPORT-013 | ✅ Complete | GitHubController, GitHubCliService, IssueTodoSyncService |
| FR-MCP-009 | ✅ Complete | WorkspaceController, WorkspaceService, WorkspaceEntity |
| FR-MCP-011 | ✅ Complete | WorkspaceProcessManager |
| FR-MCP-012 | ✅ Complete | ToolRegistryController, ToolRegistryService, ToolBucketService |
| FR-MCP-013 | ✅ Complete | ApiKeyAuthFilter, SkipApiKeyAuthAttribute |
| FR-MCP-014 | ✅ Complete | PairingHtml, PairingOptions, Program.cs (/pair) |
| FR-MCP-015 | ✅ Complete | NgrokTunnelProvider, CloudflareTunnelProvider, FrpTunnelProvider |
| FR-MCP-016 | ✅ Complete | Program.cs (MapMcp), ModelContextProtocol.AspNetCore |
| FR-MCP-017 | ✅ Complete | Program.cs (UseWindowsService), Manage-McpService.ps1 |
| FR-LOC-001 | 🔲 Planned | — |
| TR-PLANNED-013 | ✅ Complete | Core infrastructure |
| TR-GH-013-001–006 | ✅ Complete | GitHub integration |
| TR-MCP-WS-002–004 | ✅ Complete | Workspace management |
| TR-MCP-TR-001–002 | ✅ Complete | Tool registry |
| TR-MCP-SEC-001–002 | ✅ Complete | Security |
| TR-MCP-TUN-001–003 | ✅ Complete | Tunneling |
| TR-MCP-HTTP-001 | ✅ Complete | MCP transport |
| TR-MCP-SVC-001 | ✅ Complete | Windows service |
| TR-LOC-001 | 🔲 Planned | — |
