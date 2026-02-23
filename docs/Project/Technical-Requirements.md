# Technical Requirements (MCP Server)

## TR-MCP-ARCH-001

ASP.NET Core 9 server with HTTP and STDIO MCP transport.

## TR-MCP-DATA-001

SQLite persistence for MCP metadata and optional TODO backend.

## TR-MCP-DATA-002

HNSW vector index with ONNX embeddings.

## TR-MCP-DATA-003

SQLite FTS5 full-text search support and hybrid ranking.

## TR-MCP-CFG-001

IOptions-based configuration for all filesystem and runtime settings.

## TR-MCP-CFG-002

Port selection from `Mcp:Port` with `PORT` env override.

## TR-MCP-CFG-003

**Workspace Configuration Schema** — Workspace state is persisted in `appsettings.json` under `Mcp:Workspaces` (not in EF/SQLite). Each entry includes: `WorkspacePath` (required, absolute path, primary key), `Name` (required), `WorkspacePort` (required), `TodoPath` (default: `docs/todo.yaml`), `DataDirectory` (optional override for mcp.db), `TunnelProvider` (optional: `ngrok`/`cloudflare`/`frp`), `RunAs` (optional Windows identity), `IsPrimary` (default: false), `IsEnabled` (default: true), `DateTimeCreated`, `DateTimeModified`. Port uniqueness enforced; auto-assignment from `max(existing) + 1`. File written atomically via `JsonNode` patching with `IConfigurationRoot.Reload()`.

## TR-MCP-INGEST-001

Pluggable ingestors for repo/session/external/github/issues.

## TR-MCP-API-001

REST routes for todo/session/context/repo/github/sync with OpenAPI.

## TR-MCP-OPS-001

Operational scripts for startup, health checks, packaging, config validation, and migration.

## TR-MCP-WS-002

**Workspace Service** — CRUD operations for workspace entities persisted in EF Core SQLite. Auto-port assignment starts at base 7148 and increments from the current maximum registered port. Init scaffolding creates the workspace directory, `docs/Project/TODO.yaml`, `docs/sessions/`, `docs/external/`, and `mcp.db`.

## TR-MCP-WS-003

**Workspace Process Manager** — In-process Kestrel host management via `WorkspaceAppFactory`. Each workspace gets its own `WebApplication`, isolated DI container, `McpDbContext`, and Kestrel listener on its assigned port, all within the primary service process. Implements `IHostedService` for graceful shutdown of all workspace hosts on app exit.

## TR-MCP-WS-004

**Workspace Controller** — REST API at `/mcp/workspace` with Base64URL-encoded path keys. Provides create, read, update, delete, init, start, stop, status, and prompt (GET/PUT) endpoints. All `/mcp/*` routes protected by `WorkspaceAuthMiddleware` (per-workspace token).

## TR-MCP-WS-005

**Marker File Service** — `MarkerFileService.WriteMarkerAsync` writes `AGENTS-README-FIRST.yaml` to the workspace root when a workspace Kestrel host starts; `RemoveMarker` deletes it on stop. Uses Handlebars.Net templating with full workspace context. The YAML file contains port, `baseUrl`, all endpoint paths, process PID, `startedAt` timestamp, workspace name, per-workspace auth token (`apiKey`), and a machine-readable `prompt` block with glossary, workspace definition table, and protocol instructions.

## TR-MCP-WS-006

**Workspace Host Controller Isolation** — `ExcludeControllerFeatureProvider` (an `IApplicationFeatureProvider<ControllerFeature>`) is registered in workspace `WebApplication` instances to remove `WorkspaceController` from the MVC feature provider, preventing workspace lifecycle endpoints from being accessible on workspace-scoped ports.

## TR-MCP-WS-007

**Workspace Auto-Start on Service Startup** — `WorkspaceProcessManager`, as an `IHostedService`, queries all registered workspaces from the database on `StartAsync` and starts a Kestrel host for each. Failures on individual workspace starts are logged and skipped rather than aborting global startup.

## TR-MCP-WS-008

**Workspace Auto-Init and Auto-Start on Creation** — `WorkspaceController` POST calls `WorkspaceService.InitAsync` to scaffold the directory structure, then calls `WorkspaceProcessManager.StartAsync` to bring the host online, all within a single request, before returning 201 Created.

## TR-MCP-TR-001

**Tool Registry Service** — Keyword search across tool tags (bidirectional singular/plural contains matching), name, and description. Results combine global tools (`WorkspacePath == null`) with workspace-scoped tools. Full CRUD for `ToolDefinitionEntity` and `ToolDefinitionTagEntity`.

## TR-MCP-TR-002

**Tool Bucket Service** — GitHub repository browsing via `gh api /repos/{owner}/{repo}/contents{path}?ref={branch}`. Reads and parses `stdio-tool-contract.json` manifests for install and sync operations. Persists bucket state to `ToolBucketEntity`.

## TR-MCP-TR-003

**Tool Registry Default Bucket Seeding** — On startup, `Program.cs` reads `Mcp:ToolRegistry:DefaultBuckets` and calls `IToolBucketService.EnsureDefaultBucketsAsync` to register any configured buckets not already in the database. Idempotent: existing buckets are not modified.

## TR-MCP-SEC-001

**Per-Workspace Auth Tokens** — `WorkspaceAuthMiddleware` intercepts all `/mcp/*` requests at the pipeline level. `WorkspaceTokenService` generates per-workspace cryptographic tokens (32-byte base64url) on startup — not persisted, rotating on each restart. Tokens are validated via the `X-Api-Key` header or `api_key` query parameter. On 401, the response instructs the agent to re-read the `AGENTS-README-FIRST.yaml` marker file for the updated token.

## TR-MCP-SEC-002

**Pairing Session Security** — `PairingSessionService` verifies passwords using SHA-256 with `CryptographicOperations.FixedTimeEquals` for constant-time comparison. Session state is stored in HttpOnly cookies with the Secure flag enabled on HTTPS. `PairingOptions` binds `Mcp:ApiKey` and `Mcp:PairingUsers` from configuration.

## TR-MCP-TUN-001

**Tunnel Strategy Pattern** — DI registration in `Program.cs` reads `Mcp:Tunnel:Provider`, normalizes to uppercase, and uses `ActivatorUtilities.CreateInstance<T>` to instantiate the matching provider (`NgrokTunnelProvider`, `CloudflareTunnelProvider`, or `FrpTunnelProvider`). The provider is registered as both a singleton and an `IHostedService`, conditionally on the provider name being non-empty.

## TR-MCP-TUN-002

**Tunnel Process Lifecycle** — `Process.Kill()` is wrapped in a try-catch for `InvalidOperationException` to handle races. `WaitForExit(5000)` enforces a 5 s shutdown timeout. FRP config files written to temp storage are deleted on stop. All three providers log start, stop, and error events.

## TR-MCP-TUN-003

**Ngrok Auth Token Security** — The ngrok auth token is passed via the `NGROK_AUTHTOKEN` environment variable on the child process, rather than as a CLI argument, to prevent exposure in process listings and shell history.

## TR-MCP-HTTP-001

**MCP Streamable HTTP Endpoint** — `app.MapMcp("/mcp-transport")` maps the native MCP protocol handler at a path separate from the REST routes (`/mcp/*`). The endpoint requires an `Accept: application/json, text/event-stream` header and returns HTTP 406 without it. Uses `ModelContextProtocol.AspNetCore` 0.9.0-preview.1.

## TR-MCP-SVC-001

**Windows Service Configuration** — `UseWindowsService(options => { options.ServiceName = "McpServer"; })` in `Program.cs` enables Windows Service hosting. The service is published as a self-contained single-file executable to `C:\ProgramData\McpServer`. The `Manage-McpService.ps1` script handles Install, Uninstall, Start, Stop, Restart, Status, and Publish operations with gsudo elevation. Recovery policy restarts the service on failure with a 60 s delay.

## TR-MCP-REQ-001

**AI Requirements Analysis Service** — `RequirementsService` invokes `ICopilotClient` with a structured prompt containing the TODO item's title, description, technical details, implementation tasks, and pre-existing FR/TR assignments. The prompt instructs Copilot to identify existing FRs/TRs from `docs/Project/` and create new entries for unaddressed functionality, then emit a JSON block with assigned IDs. Response parsing first attempts structured JSON extraction; falls back to regex (`FR-[A-Z]+-\d{3}` / `TR-[A-Z]+-\d{3}`) for robustness. Discovered IDs are merged (deduplicated, order-preserved) back into the TODO via `ITodoService.UpdateAsync`.

## TR-MCP-INGEST-002

**Markdown Session Log Parser** — `MarkdownSessionLogParser.TryParse` recognizes Markdown files with a `# Session Log – {title}` or `# Copilot Session Log – {title}` header and parses them into `UnifiedSessionLogDto`. Extracts date, status, branch, model, duration, and known sections (Session Overview, Changes Made, Technical Requirements, Testing, etc.) as a summary entry. Individual `### Request` subsections are parsed as separate `UnifiedRequestEntryDto` entries. `NormalizeToStructuredText` produces a structured plain-text representation for FTS5 and vector embedding.

## TR-MCP-WS-009

**Primary Workspace Detection and IsEnabled Gating** — `WorkspaceProcessManager.IHostedService.StartAsync` resolves the primary workspace: first by `IsPrimary = true` + lowest port among enabled workspaces; then by lowest-port enabled workspace if none is marked primary. For the primary workspace, only a marker file is written — no child `WebApplication` is created. Workspaces with `IsEnabled = false` are skipped during auto-start but can be started manually.

## TR-MCP-DRY-001

**DRY — No Duplication in Code or Scripts** *(DIRECTIVE)* — All code and scripts must follow the DRY principle without exception. Shared logic must be extracted into a single reusable location (service, helper, function, shared script module). Inline duplication of validation, parsing, formatting, or business logic across files is prohibited. Scripts must share common operations via parameterized functions or a shared module.

**Covered by:** `TodoValidator`, `MarkerFileService`, `ExcludeControllerFeatureProvider`, `Update-McpService.ps1`

## TR-LOC-001

**Localization Infrastructure** — Multi-language support for the MCP server. *(Planned — implementation scope TBD.)*
