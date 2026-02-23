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

Dynamic workspace registration, configuration, and lifecycle management. Workspace state is persisted in `Mcp:Workspaces` within `appsettings.json` — not in the database.

**Covered by:**

- `WorkspaceController` — CRUD, init, start/stop, status endpoints
- `WorkspaceService` — auto-port assignment, init scaffolding, CRUD; writes to `appsettings.json`
- `WorkspaceConfigEntry` — internal configuration model bound from `Mcp:Workspaces[n]`

**`appsettings.json` schema — `Mcp:Workspaces` array:**

```jsonc
"Mcp": {
  "Workspaces": [
    {
      // Required. Absolute path to the workspace root directory (primary key).
      "WorkspacePath": "E:\\github\\MyProject",

      // Required. Human-readable name shown in status and marker file.
      "Name": "MyProject",

      // Required. HTTP port for this workspace's in-process Kestrel host.
      "WorkspacePort": 7148,

      // Path to the workspace TODO file.
      // Relative paths resolve from WorkspacePath. Default: "docs/todo.yaml".
      "TodoPath": "docs/todo.yaml",

      // Optional. Override directory for mcp.db and related data files.
      // Use when WorkspacePath is on a network share, WSL symlink, or read-only volume.
      // Null = data files are stored inside WorkspacePath.
      "DataDirectory": null,

      // Optional. Tunnel provider key: "ngrok", "cloudflare", "frp", or null.
      "TunnelProvider": null,

      // Optional. Windows identity for the child process (null = inherit service account).
      "RunAs": null,

      // When true, this workspace is the primary instance — the host process serves it
      // directly and no child app is spun up. Default: false.
      // If no workspace is marked primary, the enabled workspace with the lowest port wins.
      "IsPrimary": false,

      // When false, the workspace is skipped during auto-start. Default: true.
      // Disabled workspaces can still be started manually via POST /start.
      "IsEnabled": true,

      // Managed by the service — do not edit manually.
      "DateTimeCreated": "2026-02-23T00:00:00+00:00",
      "DateTimeModified": "2026-02-23T00:00:00+00:00"
    }
  ]
}
```

**Notes:**

- `WorkspacePath` is the primary key. Duplicate paths are rejected with 409 Conflict.
- Port uniqueness is enforced; new workspaces auto-assign `max(existing ports) + 1` if no port is supplied.
- `IsPrimary` marks a workspace as served by the host process — no child app is started (FR-MCP-025).
- `IsEnabled` (default `true`) controls auto-start; disabled workspaces can still be started manually.
- The file is written atomically via `JsonNode` patching; `IConfigurationRoot.Reload()` is called after each write.
- The production service writes to `C:\ProgramData\McpServer\appsettings.json` (ContentRootPath).

### FR-MCP-011: Workspace Process Orchestration

The main server spawns and manages child MCP processes per workspace with process lifecycle tracking.

**Covered by:**

- `WorkspaceProcessManager` — in-process Kestrel host lifecycle (start, stop, status per workspace port)
- `IWorkspaceProcessManager` — `IHostedService` for graceful shutdown

### FR-MCP-012: Tool Registry *(NEW)*

Agents can discover tools by keyword search across global and workspace-scoped tool definitions, with GitHub-backed bucket repositories for tool distribution.

**Covered by:**

- `ToolRegistryController` — search, CRUD, bucket endpoints
- `ToolRegistryService` — keyword search (tags, name, description), CRUD
- `ToolBucketService` — GitHub repo browsing, install, sync via `gh api`
- `ToolDefinitionEntity`, `ToolDefinitionTagEntity`, `ToolBucketEntity`

### FR-MCP-013: API Key Authentication *(UPDATED)*

Protect all `/mcp/*` API endpoints with per-workspace auth tokens. Tokens rotate on each service restart and are discoverable via the `AGENTS-README-FIRST.yaml` marker file.

**Covered by:**

- `WorkspaceAuthMiddleware` — pipeline middleware protecting all `/mcp/*` routes
- `WorkspaceTokenService` — per-workspace cryptographic token generation/validation
- Tokens written into marker files by `MarkerFileService`

### FR-MCP-014: Pairing Web UI *(NEW — needs update)*

Browser-based login flow for authorized users to retrieve auth credentials for MCP client configuration. Currently still uses the legacy `Mcp:ApiKey` config — needs migration to per-workspace tokens.

**Covered by:**

- `PairingHtml` — login form, API key display, not-configured HTML templates
- `PairingOptions` — binds `Mcp:ApiKey` and `Mcp:PairingUsers` (legacy — to be updated)
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

### FR-MCP-018: Marker File Agent Discovery *(NEW)*

When a workspace Kestrel host starts, write a `.mcp-server.yaml` marker file to the workspace root so agents can discover the correct port, endpoint paths, and connection prompt without manual configuration. Remove the marker file when the host stops.

**Covered by:**

- `MarkerFileService` — `WriteMarkerAsync` / `RemoveMarker` static helpers
- `WorkspaceProcessManager` — calls `MarkerFileService` on start and stop

### FR-MCP-019: Workspace Host Controller Isolation *(NEW)*

Workspace-scoped Kestrel hosts expose all API controllers except `WorkspaceController`. Workspace lifecycle management is available on the primary host only.

**Covered by:**

- `ExcludeControllerFeatureProvider` — `IApplicationFeatureProvider<ControllerFeature>` removing `WorkspaceController`
- `WorkspaceAppFactory` — registers the provider during workspace host build

### FR-MCP-020: Workspace Auto-Start on Service Startup *(UPDATED)*

On service startup, automatically start Kestrel hosts for all **enabled** workspaces registered in `appsettings.json`. Workspaces with `IsEnabled = false` are skipped. The primary workspace (see FR-MCP-025) only gets a marker file — no child app is spun up.

**Covered by:**

- `WorkspaceProcessManager` (`IHostedService.StartAsync`) — resolves the primary workspace, iterates all registered workspaces, skips disabled ones, calls `StartAsync` per enabled workspace

### FR-MCP-021: Workspace Auto-Init and Auto-Start on Creation *(NEW)*

When a new workspace is registered, automatically initialize its directory scaffold and start its Kestrel host in the same request so the workspace is immediately operational.

**Covered by:**

- `WorkspaceController` POST — calls `WorkspaceService.InitAsync` then `WorkspaceProcessManager.StartAsync`
- `WorkspaceService.InitAsync` — creates directories, `todo.yaml`, `mcp.db`

### FR-MCP-025: Primary Workspace Detection and Deduplication *(NEW)*

One workspace is designated as the **primary** workspace — it is served by the host process directly and no child `WebApplication` is spun up. Only a marker file (`AGENTS-README-FIRST.yaml`) is written.

**Resolution order:**

1. The first **enabled** workspace with `IsPrimary = true` and the **lowest port** wins.
2. If no workspace has `IsPrimary = true`, the **enabled** workspace with the **lowest port** is used.
3. If no workspaces are enabled, no primary is set and only the host management API runs.

The primary workspace's `ContentRootPath` is set on the host process at startup so relative paths resolve correctly.

**Covered by:**

- `WorkspaceProcessManager` (`IHostedService.StartAsync`) — resolves primary via `IsPrimary` flag then lowest-port fallback; `IsPrimaryWorkspace()` check in `StartAsync`/`StopAsync`/`GetStatus`
- `WorkspaceConfigEntry.IsPrimary` / `WorkspaceConfigEntry.IsEnabled` — persisted in `Mcp:Workspaces`
- `WorkspaceDto`, `WorkspaceCreateRequest`, `WorkspaceUpdateRequest` — expose both fields via API
- `Program.cs` — sets `ContentRootPath` from workspace config

### FR-MCP-022: Tool Registry Default Bucket Seeding *(NEW)*

On first startup, seed default tool buckets from `Mcp:ToolRegistry:DefaultBuckets` configuration if they are not already registered, ensuring new installations have the primary tool repository available without manual setup.

**Covered by:**

- `ToolRegistryOptions.DefaultBuckets` — configuration model
- `Program.cs` startup — calls `IToolBucketService.EnsureDefaultBucketsAsync`

### FR-MCP-023: AI-Assisted Requirements Analysis *(NEW)*

Provide a service that invokes the Copilot CLI to examine a TODO item, identify matching existing FR/TR IDs from project docs, create new FR/TR entries for unaddressed functionality, and persist assigned IDs back to the TODO item.

**Covered by:**

- `RequirementsService` — prompt construction, Copilot invocation, ID extraction (JSON + regex fallback), TODO update
- `IRequirementsService` — interface
- `ICopilotClient` — `McpServer.Common.Copilot` integration

### FR-MCP-024: Markdown Session Log Ingestion *(NEW)*

The ingestion pipeline shall parse legacy Markdown session log files (matching a `# Session Log – {title}` header) into the unified session log schema, enabling retroactive indexing of pre-existing agent session records.

**Covered by:**

- `MarkdownSessionLogParser` — `TryParse`, `NormalizeToStructuredText`
- `SessionLogIngestor` — integrates parser into ingestion pipeline

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

In-process Kestrel host management via `WorkspaceAppFactory` with `IHostedService` for graceful shutdown. Each workspace gets its own `WebApplication`, DI container, and listener on its assigned port — all within the primary service process.

### TR-MCP-WS-004: Workspace Controller

REST API at `/mcp/workspace` with Base64URL-encoded path keys, 11 endpoints. All `/mcp/*` routes protected by `WorkspaceAuthMiddleware` (per-workspace token via `X-Api-Key` header or `api_key` query param).

### TR-MCP-TR-001: Tool Registry Service *(NEW)*

Keyword search across tags (bidirectional contains for singular/plural tolerance), name, and description. Results combine global tools (`WorkspacePath == null`) with workspace-scoped tools.

### TR-MCP-TR-002: Tool Bucket Service *(NEW)*

GitHub repository browsing via `gh api /repos/{owner}/{repo}/contents{path}?ref={branch}`. Manifest parsing, install, and sync operations.

### TR-MCP-SEC-001: Per-Workspace Auth Tokens *(UPDATED)*

`WorkspaceAuthMiddleware` intercepts all `/mcp/*` requests at the pipeline level. Per-workspace cryptographic tokens are generated by `WorkspaceTokenService` on startup (not persisted — rotate on restart). Validated via `X-Api-Key` header or `api_key` query parameter. On 401, response instructs the agent to re-read the `AGENTS-README-FIRST.yaml` marker file for the updated token.

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

### TR-MCP-WS-005: Marker File Service *(NEW)*

`MarkerFileService.WriteMarkerAsync` writes `AGENTS-README-FIRST.yaml` to the workspace root on host start. `RemoveMarker` deletes it on stop, also cleaning up legacy `.mcp-server.yaml` and `.mcp-server.json` files. The YAML includes port, `baseUrl`, all endpoint paths, PID, `startedAt`, workspace name, and a machine-readable `prompt` block.

### TR-MCP-WS-006: Workspace Host Controller Isolation *(NEW)*

`ExcludeControllerFeatureProvider` implements `IApplicationFeatureProvider<ControllerFeature>` and removes specified controller types from workspace `WebApplication` instances. `WorkspaceAppFactory` registers it to exclude `WorkspaceController`, preventing workspace lifecycle endpoints from being routed on workspace-scoped ports.

### TR-MCP-WS-007: Workspace Auto-Start on Startup *(UPDATED)*

`WorkspaceProcessManager.StartAsync` (as `IHostedService`) queries all workspace registrations at service startup, resolves the primary workspace (see TR-MCP-WS-009), skips disabled workspaces, and calls `StartAsync` for each enabled workspace. The primary workspace only gets a marker file. Individual failures are caught and logged without aborting overall startup.

### TR-MCP-WS-008: Workspace Auto-Init and Auto-Start on Creation *(NEW)*

`WorkspaceController` POST calls `WorkspaceService.InitAsync` for directory scaffolding, then `WorkspaceProcessManager.StartAsync` to start the Kestrel host, all within the same HTTP request. Returns 201 Created only after both steps succeed.

### TR-MCP-WS-009: Primary Workspace Detection and IsEnabled Gating *(NEW)*

`WorkspaceProcessManager.IHostedService.StartAsync` resolves the primary workspace from workspace config: first by `IsPrimary = true` + lowest port among enabled workspaces; then by lowest-port enabled workspace if none is marked primary. `_primaryWorkspaceKey` is stored for the lifetime of the process. `IsPrimaryWorkspace(key)` returns true only for the resolved primary.

For the primary workspace, `StartAsync` writes only the marker file and returns `IsRunning = true` without creating a child `WebApplication`. `StopAsync` removes the marker but cannot stop the host process. `GetStatus` always returns `IsRunning = true`.

Workspaces with `IsEnabled = false` are logged as skipped during auto-start. They can still be started manually via `POST /mcp/workspace/{key}/start`.

`WorkspaceConfigEntry.IsEnabled` defaults to `true`; `IsPrimary` defaults to `false`.

### TR-MCP-TR-003: Tool Registry Default Bucket Seeding *(NEW)*

`ToolRegistryOptions.DefaultBuckets` (section `Mcp:ToolRegistry:DefaultBuckets`) holds a list of `DefaultBucketEntry` records (name, owner, repo, branch, manifestPath). `Program.cs` calls `IToolBucketService.EnsureDefaultBucketsAsync` on startup; the method is idempotent and skips existing buckets.

### TR-MCP-REQ-001: AI Requirements Analysis Service *(NEW)*

`RequirementsService` builds a structured prompt from the TODO's title, description, technical details, and pre-existing FR/TR IDs, then invokes `ICopilotClient` with a 5-minute timeout. Response parsing first attempts JSON extraction via `JsonDocument`; falls back to regex (`FR-[A-Z]+-\d{3}` / `TR-[A-Z]+-\d{3}`). Discovered IDs are merged into the existing TODO via `ITodoService.UpdateAsync`.

### TR-MCP-INGEST-002: Markdown Session Log Parser *(NEW)*

`MarkdownSessionLogParser.TryParse` recognizes files with a `# [Copilot ]Session Log – {title}` header and maps known Markdown sections to `UnifiedSessionLogDto`. Individual `### Request` sub-sections become separate `UnifiedRequestEntryDto` entries. `NormalizeToStructuredText` produces a flat structured-text representation for FTS5 and vector embedding, matching the format used for JSON session logs.

### TR-MCP-DRY-001: DRY — No Duplication in Code or Scripts *(DIRECTIVE)*

All code and scripts **must** follow the DRY (Don't Repeat Yourself) principle without exception.

- Shared logic must be extracted into a single reusable location (service, helper, function, shared script module).
- Inline duplication of validation, parsing, formatting, or business logic across files is prohibited.
- Scripts must share common operations (backup, publish, health-check) via parameterized functions or a shared module — never by copy-pasting blocks between scripts.
- Violation: any logic that exists verbatim or near-verbatim in more than one location without a shared abstraction.

**Covered by:**

- `TodoValidator` — single priority validator consumed by all TODO backends
- `MarkerFileService` — single marker write/remove implementation shared by primary host and workspace manager
- `ExcludeControllerFeatureProvider` — single feature provider used by both `Program.cs` and `WorkspaceAppFactory`
- `Update-McpService.ps1` — parameterized functions (`Write-Step`, `Wait-ProcessExit`) used throughout the script

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
| FR-MCP-013 | ✅ Complete | WorkspaceAuthMiddleware, WorkspaceTokenService, MarkerFileService |
| FR-MCP-014 | ✅ Complete | PairingHtml, PairingOptions, Program.cs (/pair) |
| FR-MCP-015 | ✅ Complete | NgrokTunnelProvider, CloudflareTunnelProvider, FrpTunnelProvider |
| FR-MCP-016 | ✅ Complete | Program.cs (MapMcp), ModelContextProtocol.AspNetCore |
| FR-MCP-017 | ✅ Complete | Program.cs (UseWindowsService), Manage-McpService.ps1 |
| FR-MCP-018 | ✅ Complete | MarkerFileService, WorkspaceProcessManager |
| FR-MCP-019 | ✅ Complete | ExcludeControllerFeatureProvider, WorkspaceAppFactory |
| FR-MCP-020 | ✅ Complete | WorkspaceProcessManager (IHostedService.StartAsync) |
| FR-MCP-021 | ✅ Complete | WorkspaceController POST, WorkspaceService.InitAsync |
| FR-MCP-025 | ✅ Complete | WorkspaceProcessManager, WorkspaceConfigEntry, Program.cs |
| FR-MCP-022 | ✅ Complete | ToolRegistryOptions, Program.cs (EnsureDefaultBucketsAsync) |
| FR-MCP-023 | ✅ Complete | RequirementsService, IRequirementsService, ICopilotClient |
| FR-MCP-024 | ✅ Complete | MarkdownSessionLogParser, SessionLogIngestor |
| FR-LOC-001 | 🔲 Planned | — |
| TR-PLANNED-013 | ✅ Complete | Core infrastructure |
| TR-GH-013-001–006 | ✅ Complete | GitHub integration |
| TR-MCP-WS-002–004 | ✅ Complete | Workspace management |
| TR-MCP-WS-005–009 | ✅ Complete | Workspace lifecycle enhancements |
| TR-MCP-TR-001–003 | ✅ Complete | Tool registry |
| TR-MCP-SEC-001–002 | ✅ Complete | Security |
| TR-MCP-TUN-001–003 | ✅ Complete | Tunneling |
| TR-MCP-HTTP-001 | ✅ Complete | MCP transport |
| TR-MCP-SVC-001 | ✅ Complete | Windows service |
| TR-MCP-REQ-001 | ✅ Complete | AI requirements analysis |
| TR-MCP-INGEST-002 | ✅ Complete | Markdown session log parser |
| TR-MCP-DRY-001 | ✅ Active directive | All code and scripts |
| TR-LOC-001 | 🔲 Planned | — |
