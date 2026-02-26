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

**Workspace Process Manager** — Manages workspace marker file lifecycle. On startup, generates tokens and writes `AGENTS-README-FIRST.yaml` marker files for all registered workspaces — all pointing to the single shared host port. On stop, removes marker files. No longer spawns child `WebApplication` instances (replaced by single-app multi-tenant model, see TR-MCP-MT-001 through TR-MCP-MT-003).

## TR-MCP-WS-004

**Workspace Controller** — REST API at `/mcp/workspace` with Base64URL-encoded path keys. Provides create, read, update, delete, init, start, stop, status, and prompt (GET/PUT) endpoints. All `/mcp/*` routes protected by `WorkspaceAuthMiddleware` (per-workspace token).

## TR-MCP-WS-005

**Marker File Service** — `MarkerFileService.WriteMarkerAsync` writes `AGENTS-README-FIRST.yaml` to the workspace root. All markers point to the same shared host port. Uses Handlebars.Net templating with full workspace context. The YAML file contains port, `baseUrl`, all endpoint paths, process PID, `startedAt` timestamp, workspace name, per-workspace auth token (`apiKey`), and a machine-readable `prompt` block. Agents should send `X-Workspace-Path` header for workspace targeting.

## TR-MCP-WS-006

**Workspace Host Controller Isolation** — *Obsolete.* Replaced by single-app multi-tenant model (TR-MCP-MT-002). `ExcludeControllerFeatureProvider` can be removed.

## TR-MCP-WS-007

**Workspace Auto-Start on Service Startup** — `WorkspaceProcessManager`, as an `IHostedService`, queries all registered workspaces on `StartAsync` and writes marker files for each. Failures on individual workspace marker writes are logged and skipped rather than aborting global startup.

## TR-MCP-WS-008

**Workspace Auto-Init and Auto-Start on Creation** — `WorkspaceController` POST calls `WorkspaceService.InitAsync` to scaffold the directory structure, then calls `WorkspaceProcessManager.StartAsync` to bring the host online, all within a single request, before returning 201 Created.

## TR-MCP-TR-001

**Tool Registry Service** — Keyword search across tool tags (bidirectional singular/plural contains matching), name, and description. Results combine global tools (`WorkspacePath == null`) with workspace-scoped tools. Full CRUD for `ToolDefinitionEntity` and `ToolDefinitionTagEntity`.

## TR-MCP-TR-002

**Tool Bucket Service** — GitHub repository browsing via `gh api /repos/{owner}/{repo}/contents{path}?ref={branch}`. Reads and parses `stdio-tool-contract.json` manifests for install and sync operations. Persists bucket state to `ToolBucketEntity`.

## TR-MCP-TR-003

**Tool Registry Default Bucket Seeding** — On startup, `Program.cs` reads `Mcp:ToolRegistry:DefaultBuckets` and calls `IToolBucketService.EnsureDefaultBucketsAsync` to register any configured buckets not already in the database. Idempotent: existing buckets are not modified.

## TR-MCP-SEC-001

**Per-Workspace Auth Tokens** — `WorkspaceResolutionMiddleware` resolves workspace identity per-request using a three-tier chain: (1) `X-Workspace-Path` header, (2) API key reverse lookup via `WorkspaceTokenService`, (3) default workspace from config. `WorkspaceAuthMiddleware` then validates the token against the resolved workspace. `WorkspaceTokenService` generates per-workspace cryptographic tokens (32-byte base64url) on startup and maintains reverse-lookup maps for API key → workspace resolution.

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

## TR-MCP-REQ-002

**Requirements Document Management Service** — `RequirementsDocumentService` parses the four canonical requirements documents (`Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`, `TR-per-FR-Mapping.md`) into a strongly typed in-memory model on startup and provides CRUD operations for FR/TR/TEST entries and mapping rows. Mutations are serialized with `SemaphoreSlim` and persisted with atomic file swaps (temp file + `File.Replace`/fallback overwrite) to prevent document corruption under concurrent writes.

**Covered by:** `RequirementsDocumentService`, `RequirementsDocumentParser`, `RequirementsDocumentRenderer`, `RequirementsOptions`

## TR-MCP-REQ-003

**Requirements REST + STDIO Tool Integration** — The requirements management feature is exposed over REST via `RequirementsController` at `/mcp/requirements/*` and over STDIO via MCP tools (`requirements_list`, `requirements_generate`, `requirements_create`, `requirements_update`, `requirements_delete`). Document generation supports individual Markdown documents and `doc=all` ZIP bundles with canonical filenames.

**Covered by:** `RequirementsController`, `FwhMcpTools`, `Program.cs` (DI/config registration), `RequirementsDocumentService`

## TR-MCP-INGEST-002

**Markdown Session Log Parser** — `MarkdownSessionLogParser.TryParse` recognizes Markdown files with a `# Session Log – {title}` or `# Copilot Session Log – {title}` header and parses them into `UnifiedSessionLogDto`. Extracts date, status, branch, model, duration, and known sections (Session Overview, Changes Made, Technical Requirements, Testing, etc.) as a summary entry. Individual `### Request` subsections are parsed as separate `UnifiedRequestEntryDto` entries. `NormalizeToStructuredText` produces a structured plain-text representation for FTS5 and vector embedding.

## TR-MCP-WS-009

**Primary Workspace Detection and IsEnabled Gating** — `WorkspaceProcessManager.IHostedService.StartAsync` resolves the primary workspace: first by `IsPrimary = true` + lowest port among enabled workspaces; then by lowest-port enabled workspace if none is marked primary. For the primary workspace, only a marker file is written — no child `WebApplication` is created. Workspaces with `IsEnabled = false` are skipped during auto-start but can be started manually.

## TR-MCP-DRY-001

**DRY — No Duplication in Code or Scripts** *(DIRECTIVE)* — All code and scripts must follow the DRY principle without exception. Shared logic must be extracted into a single reusable location (service, helper, function, shared script module). Inline duplication of validation, parsing, formatting, or business logic across files is prohibited. Scripts must share common operations via parameterized functions or a shared module.

**Covered by:** `TodoValidator`, `MarkerFileService`, `ExcludeControllerFeatureProvider`, `Update-McpService.ps1`

## TR-LOC-001

**Localization Infrastructure** — Multi-language support for the MCP server. *(Planned — implementation scope TBD.)*

## TR-MCP-AUTH-001

**Keycloak OIDC JWT Bearer Authentication** — ASP.NET Core JWT Bearer middleware configured with Keycloak realm authority, audience (`mcp-server-api`), and client secret. `OidcAuthOptions` bound from `Mcp:Auth` configuration section. Management endpoints (agent mutations) require `[Authorize(Policy = "AgentManager")]`; read endpoints fall back to existing API key auth. `RequireHttpsMetadata` configurable for local development.

**Covered by:** `OidcAuthOptions`, `Program.cs`, `AgentController`

## TR-MCP-AUTH-002

**GitHub Identity Provider in Keycloak** — Keycloak realm setup scripts configure GitHub as a social Identity Provider with `user:email read:org` scopes. First-login flow auto-creates Keycloak users from GitHub accounts. GitHub username mapped to `github_username` user attribute. Setup scripts accept `--GitHubClientId` / `--GitHubClientSecret` parameters; GitHub IdP is optional.

**Covered by:** `Setup-McpKeycloak.ps1`, `setup-mcp-keycloak.sh`

## TR-MCP-AUTH-003

**Device Authorization Flow for CLI Clients** — Keycloak `mcp-director` client configured as public with OAuth 2.0 Device Authorization Grant enabled. Director CLI initiates device flow, displays user code and verification URI, polls for token completion. Audience mapper ensures `mcp-server-api` appears in token audience. Realm roles mapper includes `realm_roles` claim.

**Covered by:** `Setup-McpKeycloak.ps1`, `setup-mcp-keycloak.sh`, `McpServer.Director`

## TR-MCP-AGENT-001

**Agent EF Core Entities** — `AgentDefinitionEntity` (agent type definitions with defaults), `AgentWorkspaceEntity` (per-workspace agent configurations with overrides, banning, isolation strategy), and `AgentEventLogEntity` (lifecycle event audit log). All stored in primary instance SQLite via `McpDbContext`. Unique index on `(AgentDefinitionId, WorkspacePath)` for workspace configs. JSON serialization for list fields (`DefaultModelsJson`, `ModelsOverrideJson`, `InstructionFilesOverrideJson`).

**Covered by:** `AgentDefinitionEntity`, `AgentWorkspaceEntity`, `AgentEventLogEntity`, `McpDbContext`

## TR-MCP-AGENT-002

**Built-in Agent Type Defaults** — `AgentDefaults.GetBuiltInDefaults()` returns seed data for 7 built-in agent types: copilot, cline, cursor, windsurf, claude-code, aider, continue. Each includes default launch command, instruction file path, models, branch strategy, and seed prompt. `AgentService.SeedBuiltInDefaultsAsync` is idempotent — only inserts agents not already present. Built-in definitions cannot be deleted.

**Covered by:** `AgentDefaults`, `AgentService`

## TR-MCP-AGENT-003

**Agent REST API** — `AgentController` at `/mcp/agents` with endpoints for: definition CRUD (`/definitions`), workspace agent CRUD (root), ban/unban (`/{agentId}/ban`, `/{agentId}/unban`), lifecycle events (`/{agentId}/events`), and YAML validation (`/validate`). Mutation endpoints require `[Authorize(Policy = "AgentManager")]` (JWT). Read endpoints use standard workspace API key auth.

**Covered by:** `AgentController`, `IAgentService`, `AgentService`

## TR-MCP-CQRS-001

**Standalone CQRS Library** — `McpServer.Cqrs` published as NuGet package `SharpNinja.McpServer.Cqrs`. Targets `net9.0`. Zero external dependencies beyond `Microsoft.Extensions.Logging.Abstractions` and `Microsoft.Extensions.DependencyInjection.Abstractions`. Provides: `ICommand<TResult>`, `IQuery<TResult>`, `ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>`, `Dispatcher`, `CallContext`, `CorrelationId`, `Result<T>`, `IPipelineBehavior`, and DI registration extensions. All dispatched calls are async (`Task<Result<T>>`).

**Status:** ✅ Complete — 37 unit tests passing

**Covered by:** `McpServer.Cqrs` project

## TR-MCP-CQRS-002

**Decimal Correlation IDs** — `CorrelationId` uses format `{baseId}.{counter}` where `baseId` is a random 8-digit long (stable for the entire call tree) and `counter` is a thread-safe (`Interlocked.Increment`) incrementing integer. Each pipeline step or handler call advances the counter. `CorrelationId.Parse(string)` reconstitutes from string. Propagated via HTTP headers (`X-Correlation-Id`).

**Status:** ✅ Complete

**Covered by:** `CorrelationId`

## TR-MCP-CQRS-003

**Dispatcher as ILoggerProvider with Context Registry** — `Dispatcher` implements `ILoggerProvider` and maintains a `ConcurrentDictionary<long, CallContext>` of active contexts keyed by `CorrelationId.BaseId`. `DispatcherLogger` (created by the provider) extracts correlation IDs from log scopes, looks up the `CallContext`, and enriches structured log entries with decomposed fields: `correlationId`, `correlationBaseId`, `correlationStep`, `operationName`, `userId`, `roles`, `elapsed`. `CallContext` implements `ILogger` and captures log entries to an internal list.

**Status:** ✅ Complete

**Covered by:** `Dispatcher`, `DispatcherLogger`, `CallContext`

## TR-MCP-CQRS-004

**Automatic Result Monad Logging** — After handler execution, the Dispatcher inspects the `Result<T>`: success results logged at `Debug` level with elapsed time; failures with `Exception` logged at `Error` level with exception details; failures without exception logged at `Warning` level. Dispatch calls themselves logged at `Debug` with full call context. All logging includes decomposed correlation ID fields.

**Status:** ✅ Complete

**Covered by:** `Dispatcher`

## TR-MCP-CQRS-005

**Pipeline Behaviors** — `IPipelineBehavior` wraps handler execution with pre/post processing. Behaviors receive the request, `CallContext`, and a `next` delegate. Behaviors can short-circuit by returning `Result<T>.Failure()` without calling `next`. Registration order determines execution order (outermost first). Built-in behaviors: `LoggingBehavior`, `ValidationBehavior`.

**Status:** ✅ Complete

**Covered by:** `IPipelineBehavior`, `Dispatcher`

## TR-MCP-DIR-001

**Director Console App with CQRS** — `McpServer.Director` console application using `System.CommandLine` for CLI parsing and `McpServer.Cqrs` for all action dispatch. CLI commands: `health`, `list`, `agents` (defs/ws/events), `add`, `ban`, `unban`, `delete`, `validate`, `init`, `sync` (status/run), `todo`, `session-log`, `login`, `logout`, `whoami`, `interactive` (aliases: `tui`, `ui`), `exec`, `list-viewmodels`. Interactive mode uses Terminal.Gui v2 with 7 tabbed screens (Health, Workspaces, Agents, TODO, Sessions, Sync, Policy) plus LoginDialog, menu bar, auth status indicator, and keyboard shortcuts (F2 Login, F5 Refresh, Ctrl+Q Quit).

**Status:** ✅ Complete — 18 CLI commands, 9 Terminal.Gui screens, solution builds with 0 warnings

**Covered by:** `McpServer.Director` project (`Program.cs`, `DirectorCommands.cs`, `AuthCommands.cs`, `InteractiveCommand.cs`, `McpHttpClient.cs`, `MainScreen.cs`, `HealthScreen.cs`, `AgentScreen.cs`, `TodoScreen.cs`, `SessionLogScreen.cs`, `SyncScreen.cs`, `WorkspaceListScreen.cs`, `WorkspacePolicyScreen.cs`, `LoginDialog.cs`, `ViewModelBinder.cs`)

## TR-MCP-DIR-002

**Director OIDC Authentication** — `OidcAuthService` implements Keycloak Device Authorization Flow. Initiates device flow, displays user code and verification URI, polls for token. Tokens cached to `~/.mcpserver/tokens.json` via `TokenCache`. `McpHttpClient.TrySetCachedBearerToken()` loads cached tokens on startup. CLI commands: `login`, `logout`, `whoami`. TUI: `LoginDialog` with Device Flow UI, authority/client-id fields, user code display, polling status, and whoami frame. Token includes `sub`, `preferred_username`, `email`, `realm_roles` claims.

**Status:** ✅ Complete

**Covered by:** `McpServer.Director` project (`Auth/OidcAuthService.cs`, `Auth/TokenCache.cs`, `Auth/DirectorAuthOptions.cs`, `Commands/AuthCommands.cs`, `Screens/LoginDialog.cs`)

## TR-MCP-COMP-001

**Workspace Compliance Ban Lists** — `WorkspaceDto`, `WorkspaceCreateRequest`, and `WorkspaceUpdateRequest` include four `List<string>` properties: `BannedLicenses`, `BannedCountriesOfOrigin`, `BannedOrganizations`, `BannedIndividuals`. `MarkerFileService.BuildTemplateContext` exposes these as Handlebars context (null when empty). `DefaultPromptTemplate` uses `{{#if}}` / `{{#each}}` blocks to conditionally render compliance sections. Recognized action types: `license_violation`, `origin_violation`, `origin_review`, `entity_violation`, `dependency_add`.

**Covered by:** `IWorkspaceService.cs`, `MarkerFileService.cs`

## TR-MCP-COMP-002

**Agent Values Prompt Sections** — `DefaultPromptTemplate` includes five mandatory non-configurable sections: (1) Absolute Honesty, (2) Correctness Above All, (3) Complete Decision Documentation, (4) Professional Representation and Audit Trail, (5) Source Attribution. Each section specifies required session log action types (`commit`, `pr_comment`, `issue_comment`, `web_reference`, `design_decision`).

**Covered by:** `MarkerFileService.DefaultPromptTemplate`

## TR-MCP-COMP-003

**Session Continuity Protocol** — `DefaultPromptTemplate` includes Requirements Tracking, Design Decision Logging, and Session Continuity sections. Agents must: read marker file at session start, query recent session logs, query TODOs, read Requirements-Matrix.md, post updated session logs every ~10 interactions, and capture requirements/decisions as they emerge.

**Covered by:** `MarkerFileService.DefaultPromptTemplate`

## TR-MCP-AUDIT-001

**Audited Copilot Client** — `AuditedCopilotClient` decorates `ICopilotClient`. Before each Copilot invocation: determines affected workspaces, creates `in_progress` session log entries per workspace. After invocation: logs `completed` entries with result and actions taken. Action type: `copilot_invocation`. Registered as DI decorator so all server-initiated Copilot calls are audited.

**Covered by:** `AuditedCopilotClient` (planned)

## TR-MCP-POL-001

**Natural Language Policy Management** — `PolicyManagementTool` MCP STDIO tool + `POST /mcp/workspace/policy` REST endpoint. Accepts natural language directives, parses intent (action, category, value, scope) via LLM, applies workspace config mutations via `IWorkspaceService.UpdateAsync`, logs `policy_change` actions per affected workspace session log.

**Covered by:** `PolicyManagementTool` (planned)

## TR-MCP-DIR-003

**Director Exec Command** — `director exec <ViewModelName>` CLI command. `IViewModelRegistry` maps ViewModel names/aliases to types. `ExecCliCommand` resolves ViewModel from DI, deserializes JSON input to properties via `System.Text.Json`, executes primary `IRelayCommand`, serializes `Result<T>` to JSON stdout. `[ViewModelCommand("alias")]` attribute for CLI aliases. Exit code 0/1 maps to Result success/failure.

**Status:** ✅ Complete

**Covered by:** `McpServer.Director` project (`Program.cs` exec/list-viewmodels commands), `McpServer.UI.Core` (`IViewModelRegistry`)

## TR-MCP-DTO-001

**Extended Session Log Entry Fields** — `UnifiedRequestEntryDto` extended with: `designDecisions` (List<string>), `requirementsDiscovered` (List<string> of requirement IDs), `filesModified` (List<string> of file paths), `blockers` (List<string>). All fields are REQUIRED in the marker prompt session logging instructions except `blockers` which is RECOMMENDED.

**Covered by:** `UnifiedSessionLogDto.cs`

## TR-MCP-CTX-001

**New Project Context Indexing** — Ingestion configuration must include `src/McpServer.Cqrs/**/*.cs`, `src/McpServer.Cqrs.Mvvm/**/*.cs`, `src/McpServer.UI.Core/**/*.cs`, and `src/McpServer.Director/**/*.cs` in file patterns. Marker prompt Available Capabilities section lists all four projects with descriptions.

**Covered by:** Ingestion configuration (planned)

## TR-MCP-MT-001

**WorkspaceContext Scoped Per-Request Service** — `WorkspaceContext` is a scoped service holding resolved workspace identity: `WorkspacePath`, `WorkspaceName`, `DataDirectory`, `TodoFilePath`, `SessionsPath`, `ExternalDocsPath`, `IsDefaultKey`, `IsResolved`. Populated by `WorkspaceResolutionMiddleware` before downstream services execute. Downstream services inject `WorkspaceContext` instead of reading `IConfiguration["Mcp:RepoRoot"]`.

**Covered by:** `WorkspaceContext`, `WorkspaceResolutionMiddleware`

## TR-MCP-MT-002

**WorkspaceResolutionMiddleware** — Runs before `WorkspaceAuthMiddleware` in the pipeline. Only activates for `/mcp/*` and `/mcp-transport` routes. Resolution chain: (1) `X-Workspace-Path` header validated against registered workspaces — returns 400 for unregistered paths; (2) API key reverse lookup via `WorkspaceTokenService.ResolveWorkspaceByToken()`; (3) `Mcp:RepoRoot` config fallback; (4) primary workspace from workspace list. Populates `WorkspaceContext` scoped service.

**Covered by:** `WorkspaceResolutionMiddleware`, `WorkspaceContext`, `WorkspaceTokenService`

## TR-MCP-MT-003

**EF Core Global Query Filter for WorkspaceId** — `McpDbContext` accepts optional `WorkspaceContext` to capture `_workspaceId` per-instance. `OnModelCreating` applies `.HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId)` on all 14 entity types. Empty `_workspaceId` disables filtering (backward compatible). `IgnoreQueryFilters()` escapes for cross-workspace admin queries. `WorkspaceId TEXT NOT NULL DEFAULT ''` column with indexes on all entity tables.

**Covered by:** `McpDbContext`, all entity types (`WorkspaceId` property)
