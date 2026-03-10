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

REST routes for todo/session/context/repo/github with OpenAPI.

## TR-MCP-OPS-001

Operational scripts for startup, health checks, packaging, config validation, and migration.

## TR-MCP-WS-002

**Workspace Service** — CRUD operations for workspace entities persisted in EF Core SQLite. Auto-port assignment starts at base 7147 and increments from the current maximum registered port. Init scaffolding creates the workspace directory, `docs/Project/TODO.yaml`, `docs/sessions/`, `docs/external/`, and `mcp.db`.

## TR-MCP-WS-003

**Workspace Process Manager** — Manages workspace marker file lifecycle. On startup, generates tokens and writes `AGENTS-README-FIRST.yaml` marker files for all registered workspaces — all pointing to the single shared host port. On stop, removes marker files. No longer spawns child `WebApplication` instances (replaced by single-app multi-tenant model, see TR-MCP-MT-001 through TR-MCP-MT-003).

## TR-MCP-WS-004

**Workspace Controller** — REST API at `/mcpserver/workspace` with Base64URL-encoded path keys. Provides create, read, update, delete, init, start, stop, status, and prompt (GET/PUT) endpoints. All `/mcpserver/*` routes protected by `WorkspaceAuthMiddleware` (per-workspace token).

## TR-MCP-WS-005

**Marker File Service** — `MarkerFileService.WriteMarkerAsync` writes `AGENTS-README-FIRST.yaml` to the workspace root. All markers point to the same shared host port. Uses Handlebars.Net templating with full workspace context. The marker template MUST be loaded from `templates/prompt-templates.yaml` via `PromptTemplateService` (id: `default-marker-prompt`). **CRITICAL**: If the template cannot be loaded (missing file, invalid YAML, or missing ID), the service must log a critical error and shut down the server immediately. Fallback to hardcoded templates is PROHIBITED. The YAML file contains port, `baseUrl`, all endpoint paths, process PID, `startedAt` timestamp, workspace name, per-workspace auth token (`apiKey`), and a machine-readable `prompt` block. Agents should send `X-Workspace-Path` header for workspace targeting.

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

**MCP Streamable HTTP Endpoint** — `app.MapMcp("/mcp-transport")` maps the native MCP protocol handler at a path separate from the REST routes (`/mcpserver/*`). The endpoint requires an `Accept: application/json, text/event-stream` header and returns HTTP 406 without it. Uses `ModelContextProtocol.AspNetCore` 0.9.0-preview.1.

## TR-MCP-SVC-001

**Windows Service Configuration** — `UseWindowsService(options => { options.ServiceName = "McpServer"; })` in `Program.cs` enables Windows Service hosting. The service is published as a self-contained single-file executable to `C:\ProgramData\McpServer`. The `Manage-McpService.ps1` script handles Install, Uninstall, Start, Stop, Restart, Status, and Publish operations with gsudo elevation. Recovery policy restarts the service on failure with a 60 s delay.

## TR-MCP-REQ-001

**AI Requirements Analysis Service** — `RequirementsService` invokes `ICopilotClient` with a structured prompt containing the TODO item's title, description, technical details, implementation tasks, and pre-existing FR/TR assignments. The prompt instructs Copilot to identify existing FRs/TRs from `docs/Project/` and create new entries for unaddressed functionality, then emit a JSON block with assigned IDs. Response parsing first attempts structured JSON extraction; falls back to regex (`FR-[A-Z]+-\d{3}` / `TR-[A-Z]+-\d{3}`) for robustness. Discovered IDs are merged (deduplicated, order-preserved) back into the TODO via `ITodoService.UpdateAsync`.

## TR-MCP-REQ-002

**Requirements Document Management Service** — `RequirementsDocumentService` parses the four canonical requirements documents (`Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`, `TR-per-FR-Mapping.md`) into a strongly typed in-memory model on startup and provides CRUD operations for FR/TR/TEST entries and mapping rows. Mutations are serialized with `SemaphoreSlim` and persisted with atomic file swaps (temp file + `File.Replace`/fallback overwrite) to prevent document corruption under concurrent writes.

**Covered by:** `RequirementsDocumentService`, `RequirementsDocumentParser`, `RequirementsDocumentRenderer`, `RequirementsOptions`

## TR-MCP-REQ-003

**Requirements REST + STDIO Tool Integration** — The requirements management feature is exposed over REST via `RequirementsController` at `/mcpserver/requirements/*` and over STDIO via MCP tools (`requirements_list`, `requirements_generate`, `requirements_create`, `requirements_update`, `requirements_delete`). Document generation supports individual Markdown documents and `doc=all` ZIP bundles with canonical filenames.

**Covered by:** `RequirementsController`, `FwhMcpTools`, `Program.cs` (DI/config registration), `RequirementsDocumentService`

## TR-MCP-INGEST-002

**Markdown Session Log Parser** — `MarkdownSessionLogParser.TryParse` recognizes Markdown files with a `# Session Log – {title}` or `# Copilot Session Log – {title}` header and parses them into `UnifiedSessionLogDto`. Extracts date, status, branch, model, duration, and known sections (Session Overview, Changes Made, Technical Requirements, Testing, etc.) as a summary entry. Individual `### Request` subsections are parsed as separate `UnifiedRequestEntryDto` entries. `NormalizeToStructuredText` produces a structured plain-text representation for FTS5 and vector embedding.

## TR-MCP-INGEST-003

**Direct Website URL Ingestion** — Add `WebsiteIngestor` with a dedicated `HttpClient` and bounded crawl behavior. Only `http`/`https` URLs are allowed. SSRF protections block localhost, loopback, RFC1918, and link-local targets (including DNS-resolved IPs). Redirects are bounded and re-validated at each hop. Per-request controls include max pages, max depth, max bytes per page, force refresh, and optional GraphRAG index trigger. Ingested pages upsert as `SourceType=external-web` with canonical URL source keys and deterministic document IDs.

## TR-MCP-WS-009

**Primary Workspace Detection and IsEnabled Gating** — `WorkspaceProcessManager.IHostedService.StartAsync` resolves the primary workspace: first by `IsPrimary = true` + lowest port among enabled workspaces; then by lowest-port enabled workspace if none is marked primary. For the primary workspace, only a marker file is written — no child `WebApplication` is created. Workspaces with `IsEnabled = false` are skipped during auto-start but can be started manually.

## TR-MCP-DRY-001

**DRY — No Duplication in Code or Scripts** *(DIRECTIVE)* — All code and scripts must follow the DRY principle without exception. Shared logic must be extracted into a single reusable location (service, helper, function, shared script module). Inline duplication of validation, parsing, formatting, or business logic across files is prohibited. Scripts must share common operations via parameterized functions or a shared module.

**Covered by:** `TodoValidator`, `MarkerFileService`, `ExcludeControllerFeatureProvider`, `Update-McpService.ps1`

## TR-LOC-001

**Localization Infrastructure** — Multi-language support for the MCP server. *(Planned — implementation scope TBD.)*

## TR-MCP-AUTH-001

**OIDC JWT Bearer Authentication** — ASP.NET Core JWT Bearer middleware configured with OIDC authority/issuer, audience (`mcp-server-api`), and optional client secret based on provider requirements. `OidcAuthOptions` bound from `Mcp:Auth` configuration section. Management endpoints (agent mutations) require `[Authorize(Policy = "AgentManager")]`; read endpoints fall back to existing API key auth. `RequireHttpsMetadata` configurable for local development.

**Covered by:** `OidcAuthOptions`, `Program.cs`, `AgentController`

## TR-MCP-AUTH-002

**GitHub Federation via OIDC Provider** — OIDC provider setup may configure GitHub as a social Identity Provider with `user:email read:org` scopes. First-login flow may auto-create users from GitHub accounts. GitHub username mapped to `github_username` user attribute. Setup scripts accept `--GitHubClientId` / `--GitHubClientSecret` parameters; GitHub federation is optional.

**Covered by:** `Setup-McpKeycloak.ps1`, `setup-mcp-keycloak.sh`

## TR-MCP-AUTH-003

**Device Authorization Flow for CLI Clients** — OIDC `mcp-director` client configured as public with OAuth 2.0 Device Authorization Grant enabled. Director CLI initiates device flow, displays user code and verification URI, polls for token completion. Provider claim mapping ensures `mcp-server-api` appears in token audience and includes `realm_roles`.

**Covered by:** `Setup-McpKeycloak.ps1`, `setup-mcp-keycloak.sh`, `McpServer.Director`

## TR-MCP-AGENT-001

**Agent EF Core Entities** — `AgentDefinitionEntity` (agent type definitions with defaults), `AgentWorkspaceEntity` (per-workspace agent configurations with overrides, banning, isolation strategy), and `AgentEventLogEntity` (lifecycle event audit log). All stored in primary instance SQLite via `McpDbContext`. Unique index on `(AgentDefinitionId, WorkspacePath)` for workspace configs. JSON serialization for list fields (`DefaultModelsJson`, `ModelsOverrideJson`, `InstructionFilesOverrideJson`).

**Covered by:** `AgentDefinitionEntity`, `AgentWorkspaceEntity`, `AgentEventLogEntity`, `McpDbContext`

## TR-MCP-AGENT-002

**Built-in Agent Type Defaults** — `AgentDefaults.GetBuiltInDefaults()` returns seed data for 7 built-in agent types: copilot, cline, cursor, windsurf, claude-code, aider, continue. Each includes default launch command, instruction file path, models, branch strategy, and seed prompt. `AgentService.SeedBuiltInDefaultsAsync` is idempotent — only inserts agents not already present. Built-in definitions cannot be deleted.

**Covered by:** `AgentDefaults`, `AgentService`

## TR-MCP-AGENT-003

**Agent REST API** — `AgentController` at `/mcpserver/agents` with endpoints for: definition CRUD (`/definitions`), workspace agent CRUD (root), ban/unban (`/{agentId}/ban`, `/{agentId}/unban`), lifecycle events (`/{agentId}/events`), and YAML validation (`/validate`). Mutation endpoints require `[Authorize(Policy = "AgentManager")]` (JWT). Read endpoints use standard workspace API key auth.

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

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-001)*

## TR-MCP-DIR-002

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-002)*

## TR-MCP-COMP-001

**Workspace Compliance Ban Lists** — `WorkspaceDto`, `WorkspaceCreateRequest`, and `WorkspaceUpdateRequest` include four `List<string>` properties: `BannedLicenses`, `BannedCountriesOfOrigin`, `BannedOrganizations`, `BannedIndividuals`. `MarkerFileService.BuildTemplateContext` exposes these as Handlebars context (null when empty). `DefaultPromptTemplate` uses `{{#if}}` / `{{#each}}` blocks to conditionally render compliance sections. Recognized action types: `license_violation`, `origin_violation`, `origin_review`, `entity_violation`, `dependency_add`.

**Covered by:** `IWorkspaceService.cs`, `MarkerFileService.cs`

## TR-MCP-COMP-002

**Agent Values Prompt Sections** — `DefaultPromptTemplate` includes five mandatory non-configurable sections: (1) Absolute Honesty, (2) Correctness Above All, (3) Complete Decision Documentation, (4) Professional Representation and Audit Trail, (5) Source Attribution. Each section specifies required session log action types (`commit`, `pr_comment`, `issue_comment`, `web_reference`, `design_decision`).

**Covered by:** `MarkerFileService.DefaultPromptTemplate`

## TR-MCP-COMP-003

**Session Continuity Protocol** — The `default-marker-prompt` template (YAML) includes Requirements Tracking, Design Decision Logging, and Session Continuity sections. Agents must: read marker file at session start, query recent session logs, query TODOs, read Requirements-Matrix.md, post updated session logs every ~10 interactions, and capture requirements/decisions as they emerge.

**Covered by:** `templates/prompt-templates.yaml` (`default-marker-prompt`), `PromptTemplateService`

## TR-MCP-AUDIT-001

**Audited Copilot Client** — `AuditedCopilotClient` decorates `ICopilotClient`. Before each Copilot invocation: determines affected workspaces, creates `in_progress` session log entries per workspace. After invocation: logs `completed` entries with result and actions taken. Action type: `copilot_invocation`. Registered as DI decorator so all server-initiated Copilot calls are audited.

**Status:** ✅ Complete

**Covered by:** `AuditedCopilotClient`, `Program.cs` (`ICopilotClient` decorator wiring), `McpStdioHost` (`ICopilotClient` decorator wiring), `CopilotServiceCollectionExtensions`

## TR-MCP-POL-001

**Natural Language Policy Management** — `PolicyManagementTool` MCP STDIO tool + `POST /mcpserver/workspace/policy` REST endpoint. Accepts natural language directives, parses intent (action, category, value, scope) via LLM, applies workspace config mutations via `IWorkspaceService.UpdateAsync`, logs `policy_change` actions per affected workspace session log.

**Status:** ✅ Complete

**Covered by:** `WorkspaceController` (`POST /mcpserver/workspace/policy`), `WorkspacePolicyService`, `WorkspacePolicyDirectiveParser`, `McpServerMcpTools.workspace_policy_apply`

## TR-MCP-DIR-003

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-003)*

## TR-MCP-DTO-001

**Extended Session Log Entry Fields** — `UnifiedRequestEntryDto` extended with: `designDecisions` (`List<string>`), `requirementsDiscovered` (`List<string>` of requirement IDs), `filesModified` (`List<string>` of file paths), `blockers` (`List<string>`). All fields are REQUIRED in the marker prompt session logging instructions except `blockers` which is RECOMMENDED.

**Covered by:** `UnifiedSessionLogDto.cs`

## TR-MCP-CTX-001

**New Project Context Indexing** — Ingestion configuration must include `src/McpServer.Cqrs/**/*.cs`, `src/McpServer.Cqrs.Mvvm/**/*.cs`, `src/McpServer.UI.Core/**/*.cs`, and `src/McpServer.Director/**/*.cs` in file patterns. Marker prompt Available Capabilities section lists all four projects with descriptions.

**Status:** ✅ Complete

**Covered by:** `Program.cs` / `McpStdioHost` `PostConfigure<IngestionOptions>` required-pattern merge, `appsettings.yaml` `Mcp:RepoAllowlist`, `MarkerFileService.DefaultPromptTemplate`

## TR-MCP-MT-001

**WorkspaceContext Scoped Per-Request Service** — `WorkspaceContext` is a scoped service holding resolved workspace identity: `WorkspacePath`, `WorkspaceName`, `DataDirectory`, `TodoFilePath`, `SessionsPath`, `ExternalDocsPath`, `IsDefaultKey`, `IsResolved`. Populated by `WorkspaceResolutionMiddleware` before downstream services execute. Downstream services inject `WorkspaceContext` instead of reading `IConfiguration["Mcp:RepoRoot"]`.

**Covered by:** `WorkspaceContext`, `WorkspaceResolutionMiddleware`

## TR-MCP-MT-002

**WorkspaceResolutionMiddleware** — Runs before `WorkspaceAuthMiddleware` in the pipeline. Only activates for `/mcpserver/*` and `/mcp-transport` routes. Resolution chain: (1) `X-Workspace-Path` header validated against registered workspaces — returns 400 for unregistered paths; (2) API key reverse lookup via `WorkspaceTokenService.ResolveWorkspaceByToken()`; (3) `Mcp:RepoRoot` config fallback; (4) primary workspace from workspace list. Populates `WorkspaceContext` scoped service.

**Covered by:** `WorkspaceResolutionMiddleware`, `WorkspaceContext`, `WorkspaceTokenService`

## TR-MCP-MT-003

**EF Core Global Query Filter for WorkspaceId** — `McpDbContext` accepts optional `WorkspaceContext` to capture `_workspaceId` per-instance. `OnModelCreating` applies `.HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId)` on all 14 entity types. Empty `_workspaceId` disables filtering (backward compatible). `IgnoreQueryFilters()` escapes for cross-workspace admin queries. `WorkspaceId TEXT NOT NULL DEFAULT ''` column with indexes on all entity tables.

**Covered by:** `McpDbContext`, all entity types (`WorkspaceId` property)

## TR-MCP-LOG-001

**Exception Logging in Catch Blocks** *(DIRECTIVE)* — Every `catch` block that handles an exception must log the exception. Unexpected exceptions must use `LogError` with `ex.ToString()` as the message body. Expected/anticipated exceptions (e.g., `OperationCanceledException` on shutdown, `InvalidOperationException` for process-already-exited races, validation exceptions returned as HTTP 4xx) must use `LogWarning` with `ex.ToString()`. Catch blocks must not silently swallow exceptions with empty bodies or comments-only. The only permitted exception is re-throwing (`throw;`) without logging, where the exception will be logged by an outer handler.

## TR-MCP-TODO-002

**Cross-Workspace TODO Move** — `TodoController.MoveAsync` at `POST /mcpserver/todo/{id}/move` reads the item from the source workspace (resolved via header/API key), creates it in the target workspace (resolved via `IWorkspaceService.GetAsync` + `TodoServiceResolver.Resolve`), then deletes from the source. Request body: `TodoMoveRequest { TargetWorkspacePath }`. Error responses: 400 (null request or unknown target workspace), 404 (item not found), 409 (create failed in target), 500 (created in target but delete from source failed). MCP STDIO parity via `todo_move` tool in `FwhMcpTools`.

**Covered by:** `TodoController`, `FwhMcpTools`, `TodoMoveRequest`, `TodoServiceResolver`, `IWorkspaceService`

## TR-MCP-VOICE-001

**Voice Conversation Service** — `VoiceConversationService` manages the full voice session lifecycle: session creation with `CopilotInteractiveSession` spawned via `DesktopProcessLauncher` (or standard `Process.Start`), turn processing with tool-call loop (max `MaxToolSteps` iterations), in-memory transcript storage, tool-call record tracking, and session cleanup. Configurable via `VoiceConversationOptions` bound from `Mcp:Voice` configuration section (model, timeouts, rate limits for writes/deletes per turn, transcript context limit).

**Covered by:** `VoiceConversationService`, `VoiceConversationOptions`, `CopilotInteractiveSession`

## TR-MCP-VOICE-002

**Voice Controller REST API** — `VoiceController` at `/mcpserver/voice/session/*` exposes 8 endpoints: `POST /` (create session with `DeviceId`/`Language`/`ClientName`), `GET /?deviceId=` (find by device), `POST /{id}/turn` (synchronous turn), `POST /{id}/turn/stream` (SSE streaming turn), `POST /{id}/interrupt` (cancel active turn), `POST /{id}/escape` (send ESC chars to Copilot stdin), `GET /{id}` (session status), `GET /{id}/transcript` (transcript entries), `DELETE /{id}` (destroy session). DTOs: `VoiceSessionCreateRequest/Response`, `VoiceTurnRequest/Response`, `VoiceInterruptResponse`, `VoiceSessionStatusDto`, `VoiceTranscriptEntryDto/Response`, `VoiceToolCallRecordDto`, `VoiceTurnStreamEvent`.

**Covered by:** `VoiceController`, `VoiceConversationContracts`

## TR-MCP-VOICE-003

**Voice Session Lifecycle Management** — One active session per device enforced via `DeviceId` lookup; creating a new session for a device with an active session returns the existing session. Idle timeout (`SessionIdleTimeoutMinutes`, default 15) triggers `IdleShutdownCommand` sent to Copilot, waits for `IdleShutdownSentinel` response, then terminates the session. `UseDesktopLaunch` option (default true) selects `CreateProcessAsUser` for Windows service context.

**Covered by:** `VoiceConversationService`, `VoiceConversationOptions`

## TR-MCP-CFG-004

**YAML Configuration Support** — `Program.cs` calls `builder.Configuration.AddYamlFile("appsettings.yaml", optional: true, reloadOnChange: true)` using `NetEscapades.Configuration.Yaml`. YAML configuration merges with and can override `appsettings.json` values. Intended for local-only overrides not committed to source control.

**Covered by:** `Program.cs`, `NetEscapades.Configuration.Yaml`

## TR-MCP-DESKTOP-001

**Desktop Process Launcher** — `DesktopProcessLauncher` in `Native/` uses P/Invoke (`WTSQueryUserToken`, `DuplicateTokenEx`, `CreateProcessAsUser`) to launch processes on the interactive desktop from a LocalSystem service context. Two launch modes: `LaunchWithStdio` (redirected stdin/stdout/stderr pipes for Copilot CLI integration) and `LaunchVisible` (visible console window, no pipes). `ResolveCommandPathAsync` resolves WinGet shim paths via desktop PowerShell to find actual executable locations. Uses `CreateProcessAsUser` (not `CreateProcessWithTokenW`, which causes `STATUS_DLL_INIT_FAILED` under LocalSystem).

**Covered by:** `DesktopProcessLauncher`, `NativeMethods`

## TR-MCP-TPL-001

**Prompt Template YAML Storage** — `PromptTemplateService` persists templates in a single YAML file (default `templates/prompt-templates.yaml`) using YamlDotNet with `HyphenatedNamingConvention`. Root structure: `templates:` → map of template-id → entry object (title, category, tags, description, engine, variables, content). Read/write serialization uses `SemaphoreSlim(1,1)` for write safety. Templates are loaded on-demand and not cached (file is source of truth).

**Covered by:** `PromptTemplateService`, `TemplateStorageOptions`

## TR-MCP-TPL-002

**Prompt Template Rendering** — `PromptTemplateRenderer` compiles Handlebars templates via `HandlebarsDotNet` with content-hash-based caching in a `ConcurrentDictionary`. Variable validation checks required variables against supplied data and reports missing values. `RenderAsync` returns `PromptTemplateTestResult` with `RenderedContent` on success or `MissingVariables`/`Error` on failure. Thread-safe for concurrent rendering.

**Covered by:** `PromptTemplateRenderer`

## TR-MCP-TPL-003

**Prompt Template REST + MCP Endpoints** — `PromptTemplateController` exposes 7 REST endpoints at `/mcpserver/templates` (list/filter with query params, CRUD by ID, test stored template, test inline template). `FwhMcpTools` exposes 6 MCP tools (`prompt_template_list`, `prompt_template_get`, `prompt_template_create`, `prompt_template_update`, `prompt_template_delete`, `prompt_template_test`). Both delegate to `IPromptTemplateService`.

**Covered by:** `PromptTemplateController`, `FwhMcpTools`

## TR-MCP-TPL-004

**Prompt Template CQRS + Director UI** — Full 4-layer CQRS stack: `TemplateMessages.cs` defines queries/commands/results, 6 handlers (`ListTemplatesQueryHandler`, `GetTemplateQueryHandler`, `TestTemplateQueryHandler`, `CreateTemplateCommandHandler`, `UpdateTemplateCommandHandler`, `DeleteTemplateCommandHandler`) delegate to `ITemplateApiClient`. `TemplateApiClientAdapter` bridges to `McpServerClient.Template`. `TemplateListViewModel` and `TemplateDetailViewModel` drive `TemplatesScreen` in Director TUI. Authorization: `McpArea.Templates` with Viewer (read) and Admin (write) roles.

**Covered by:** `TemplateMessages`, `\*TemplateQueryHandler`, `\*TemplateCommandHandler`, `ITemplateApiClient`, `TemplateApiClientAdapter`, `TemplateListViewModel`, `TemplateDetailViewModel`, `TemplatesScreen`

## TR-MCP-TPL-005

**System Template Externalization** — Three provider interfaces decouple system prompt templates from inline C# constants: (1) `IMarkerPromptProvider` / `FileMarkerPromptProvider` reads `templates/prompt-templates.yaml` via `IPromptTemplateService` (id: `default-marker-prompt`), throwing a critical exception on file-missing. Fallback to `MarkerFileService.DefaultPromptTemplate` is REMOVED. Injected into `WorkspaceProcessManager` with precedence: config override (`Mcp:MarkerPromptTemplate`) > file template. (2) `ITodoPromptProvider` / `TodoPromptProvider` looks up templates from `IPromptTemplateService` by well-known IDs (`todo-status-prompt`, `todo-implement-prompt`, `todo-plan-prompt`), falling back to `TodoPromptDefaults` constants. Injected into `TodoPromptService` with precedence: `IOptionsMonitor<TodoPromptOptions>` > file template > built-in default. (3) `PairingHtmlRenderer` replaces static `PairingHtml` calls with DI-injected instance class, loading templates from `IPromptTemplateService` by well-known IDs (`pairing-login-page`, `pairing-key-page`, `pairing-not-configured-page`) using `string.Replace` token substitution (`{errorBanner}`, `{apiKey}`, `{serverUrl}`), falling back to `PairingHtml` static methods. Template YAML files ship via `.csproj` Content items and are preserved across deployments.

**Covered by:** `IMarkerPromptProvider`, `FileMarkerPromptProvider`, `ITodoPromptProvider`, `TodoPromptProvider`, `PairingHtmlRenderer`, `templates/prompt-templates.yaml`

## TR-MCP-CFG-005

**System-Wide Default Copilot Model Propagation** — Setting the default Copilot model for all session types requires updates to three locations:

- `CopilotClientOptions.Model` default value (in `McpServer.Common.Copilot`) — controls server-initiated CLI invocations via `ICopilotClient`. Configurable at runtime via `Mcp:Copilot:Model`.
- `VoiceConversationOptions.CopilotModel` default value (in `McpServer.Support.Mcp/Options/`) — controls voice conversation session model. Configurable via `Mcp:Voice:CopilotModel`.
- `AgentDefaults.GetBuiltInDefaults()` (in `McpServer.Support.Mcp/Services/`) — seed data for built-in agent type definitions including the `copilot` agent's `DefaultModelsJson`. Only affects new installations (existing agent definitions are not re-seeded).

All three share the pattern of a compile-time default overridable via `IOptions<T>` configuration binding. No new infrastructure is required — this is a default-value update propagated through existing `IOptions`-based configuration (TR-MCP-CFG-001).

**Status:** 🔴 Planned

**Covered by:** `CopilotClientOptions`, `VoiceConversationOptions`, `AgentDefaults`

## TR-MCP-AGENT-004

**Agent Pool Configuration Contract** — Agent pool settings SHALL bind from configuration into a validated options model that includes `AgentName`, `AgentPath`, `AgentModel`, `AgentSeed`, `AgentParameters`, `IsInteractiveDefault`, `IsTodoPlanDefault`, `IsTodoStatusDefault`, and `IsTodoImplementDefault`.

Validation SHALL enforce unique `AgentName` values (case-insensitive), required launch path, and unambiguous default-agent assignment for each intent-default flag.

**Status:** 🔴 Planned

**Covered by:** `AgentPoolOptions` *(planned)*, `AgentPoolDefinitionOptions` *(planned)*, `Program.cs` *(planned extension)*

## TR-MCP-AGENT-005

**Pooled Runtime and Queue Dispatcher** — All agent execution SHALL flow through a singleton pool runtime service that maintains lifecycle state per configured pooled agent and dispatches queued one-shot jobs to eligible idle agents.

Pool runtime SHALL support start/stop/recycle operations, busy/idle transitions, one-shot queue states (`queued`, `processing`, `completed`, `failed`, `canceled`), and concurrent interactive attachment to agents currently processing one-shot requests.

No alternate direct-launch path is permitted for pooled workloads; pooled agents launch through the voice interactive session mechanism.

**Status:** 🔴 Planned

**Covered by:** `IAgentPoolService` *(planned)*, `AgentPoolService` *(planned)*, `AgentPoolQueueService` *(planned)*

## TR-MCP-API-002

**One-Shot Submission Contract and Intent Routing** — One-shot APIs SHALL support explicit context values `Plan`, `Status`, `Implement`, and `AdHoc`.

When `AgentName` is omitted, the runtime SHALL resolve request intent from context/prompt and select the configured default agent for that intent.

Template-mode and ad-hoc-mode payload validation SHALL enforce:

- `promptTemplateId` and ad-hoc prompt text cannot both be supplied in explicit mode.
- At least one prompt source must be resolvable.
- `id` is required for template-resolved requests and optional for ad-hoc requests.

**Status:** 🔴 Planned

**Covered by:** `AgentPoolController` *(planned)*, `AgentPoolIntentResolver` *(planned)*, request DTO validators *(planned)*

## TR-MCP-TPL-006

**Template Resolution for One-Shot Requests** — Template rendering SHALL support:

- Explicit template mode: `promptTemplateId` + optional values dictionary.
- Context resolution mode: context-based template selection when template ID is omitted.
- Value precedence: caller-provided values override workspace-context-derived values on key collision.
- Placeholder binding: request `id` injected into render variables for `{id}` substitution.

For `AdHoc` context with no template ID, explicit ad-hoc prompt text is required.

The server SHALL provide a prompt resolution endpoint returning the populated prompt for a given template ID and values dictionary.

**Status:** 🔴 Planned

**Covered by:** `PromptTemplateController` *(planned extension)*, `PromptTemplateRenderer`, `AgentPoolController` *(planned)*

## TR-MCP-API-003

**Agent Pool Monitoring and Control APIs** — REST endpoints SHALL provide:

- Pooled agent availability snapshots.
- Runtime controls (connect, start, stop, immediate recycle).
- Queue operations (list, enqueue, cancel/remove, queued-item move up/down).
- Separate SSE notification stream emitting queue/agent lifecycle transitions with payload fields `AgentName`, `LastRequestPrompt`, and `SessionId`.
- Read-only response stream attachment supporting multiple concurrent subscribers.

**Status:** 🔴 Planned

**Covered by:** `AgentPoolController` *(planned)*, `AgentPoolNotificationService` *(planned)*, `AgentPoolStreamService` *(planned)*

## TR-MCP-VOICE-004

**Interactive Presence Signals on Stream State Changes** — On interactive stream disconnect, the runtime SHALL send `User is AFK.` to the associated interactive agent session.

On interactive stream reconnect, after response stream establishment, the runtime SHALL send `User is here.` to the associated interactive agent session.

Presence signaling SHALL be excluded from one-shot sessions.

**Status:** 🔴 Planned

**Covered by:** `VoiceConversationService` *(planned extension)*, `AgentPoolStreamService` *(planned)*

## TR-MCP-DIR-004

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-004)*

## TR-MCP-DIR-005

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-005)*

## TR-MCP-DIR-006

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-006)*

## TR-MCP-DIR-007

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-007)*

## TR-MCP-DIR-008

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-008)*

## TR-MCP-ARCH-002

**DI Single Source of Truth and Pull-Based Change Notification** — Architecture audit and remediation across `McpServer.Support.Mcp` SHALL enforce:

- Stateful services, registries, managers, and providers must be DI-owned (`singleton` or `scoped`) and must not be instantiated via `new` or `ActivatorUtilities.CreateInstance` outside composition-root registration paths.
- Authoritative mutable state must have a single owner in DI; peer services must pull current state from that owner instead of receiving pushed state payloads.
- Observable state contracts must expose change signaling via `INotifyPropertyChanged` for data-availability/change notification, without embedding mutable payload transfer in event arguments.
- Race-condition remediation must prioritize ownership/lifetime design in DI (single owner + pull model); fire-and-forget propagation and ad-hoc synchronization used as state-sharing mechanisms are prohibited.
- Automated validation must cover DI registration lifetimes and notification semantics for remediated services.

**Status:** 🔴 Planned

## TR-MCP-LOG-002

**Identifier Naming Validation** — `TodoValidator` SHALL validate TODO IDs with regex `^[A-Z]+-[A-Z0-9]+-\d{3}$` for create/update dependency paths in both YAML and SQLite providers. `SessionLogIdentifierValidator` SHALL validate session/request IDs using canonical timestamped patterns and enforce exact source-type prefix parity (`SessionId` starts with `{sourceType}-` or `{agent}-`). Invalid values return HTTP 400 at controller boundaries and `ArgumentException` for direct service invocation.

**Status:** ✅ Complete

**Covered by:** `TodoValidator`, `TodoService`, `SqliteTodoService`, `SessionLogIdentifierValidator`, `SessionLogController`, `SessionLogService`

## TR-MCP-EVT-001

**In-Process Change Event Bus** — `ChannelChangeEventBus` SHALL be registered as a singleton `IChangeEventBus` and provide fan-out publish/subscribe semantics to independent subscribers using bounded channels (capacity 1000, `DropOldest` overflow mode).

**Covered by:** `ChannelChangeEventBus`, `IChangeEventBus`, `Program.cs`

## TR-MCP-EVT-002

**Service-Layer Mutation Publishing** — Mutating service operations SHALL publish change events after successful persistence, with event emission wrapped in defensive try/catch and warning-level logging on publish failures.

**Covered by:** `TodoService`, `SqliteTodoService`, `SessionLogService`, `RepoFileService`, `ToolRegistryService`, `ToolBucketService`, `WorkspaceService`, `AgentService`, `RequirementsDocumentService`, `IngestionCoordinator`, `WorkspaceProcessManager`

## TR-MCP-EVT-003

**SSE Delivery Endpoint** — `EventStreamController` SHALL stream notifications as `text/event-stream` with `Cache-Control: no-cache` and support optional category filtering via `?category=` query parameter.

**Covered by:** `EventStreamController`

## TR-MCP-EVT-004

**Change Event Contract** — Change events SHALL include `Category`, `Action`, optional `EntityId`, optional `ResourceUri`, and UTC `Timestamp` to support correlation by consumers.

**Covered by:** `ChangeEvent`, `ChangeEventActions`, `ChangeEventCategories`

## TR-MCP-EVT-005

**Workspace Notification Category Coverage** — The notification system SHALL support at minimum the categories: `todo`, `session_log`, `repo`, `context`, `tool_registry`, `tool_bucket`, `workspace`, `github`, `marker`, `agent`, and `requirements`.

**Covered by:** `ChangeEventCategories` and all publishing call sites in mutation services/controllers

## TR-MCP-GH-001

**GitHub OAuth Bootstrap Configuration Contract** — The server SHALL bind GitHub integration settings from `Mcp:GitHub`, including OAuth client metadata (`ClientId`, `RedirectUri`, `AuthorizeEndpoint`, `Scopes`) and token store path/fallback policy flags. REST endpoints under `/mcpserver/gh/oauth/*` SHALL expose the effective bootstrap configuration and authorize URL composition.

**Status:** ✅ Complete

**Covered by:** `GitHubIntegrationOptions`, `Program.cs` options binding/post-configure, `McpStdioHost` options binding/post-configure, `GitHubController` (`/oauth/config`, `/oauth/authorize-url`)

## TR-MCP-GH-002

**Encrypted Workspace GitHub Token Persistence** — Workspace GitHub tokens SHALL be stored encrypted-at-rest using ASP.NET Core Data Protection with atomic file writes and normalized workspace-path keys. The server SHALL expose `/mcpserver/gh/auth/status`, `/mcpserver/gh/auth/token` (PUT), and `/mcpserver/gh/auth/token` (DELETE) for token lifecycle management.

**Status:** ✅ Complete

**Covered by:** `IGitHubWorkspaceTokenStore`, `FileGitHubWorkspaceTokenStore`, `GitHubController` auth endpoints, `Program.cs` DI registration

## TR-MCP-GH-003

**Authenticated GitHub CLI Execution Path with Policy-Governed Fallback** — GitHub CLI execution SHALL support per-call token overrides so workspace-stored tokens can be applied as `GH_TOKEN` when present. The execution path SHALL prefer stored tokens when configured, emit telemetry indicating selected auth mode, and reject/allow fallback based on `AllowCliFallback`.

**Status:** ✅ Complete

**Covered by:** `IProcessRunner` (`ProcessRunRequest` overload), `ProcessRunner`, `GitHubCliService` token resolution + auth-mode selection logs, `GitHubIntegrationOptions`

## TR-MCP-GH-004

**GitHub Actions Workflow Run API Surface** — The server SHALL support workflow run list/detail/rerun/cancel operations via gh CLI and expose them at `/mcpserver/gh/actions/runs*` with typed model contracts and client parity.

**Status:** ✅ Complete

**Covered by:** `IGitHubCliService`, `GitHubCliService`, `GitHubController` actions endpoints, `McpServer.Client` (`GitHubClient`, `Models/GitHubModels.cs`)

### TR-MCP-DOC-001: Marketing Documentation Coverage
- Define a marketing-focused McpServer narrative that explains platform purpose, problem/need, and adopter value proposition.
- Document key capabilities and differentiators in concise adoption-oriented language aligned with existing FR feature areas.
- Maintain a supported UI tooling section covering available user surfaces (including VS extension, Web UI, and Director/TUI where applicable) with current support status.
- Keep the documentation in version control under `docs/` so updates are reviewed and traceable with product changes.
**Status:** 🔴 Planned

## TR-MCP-DOC-002

**Test XML Documentation Completeness** *(DIRECTIVE)* — All test projects SHALL include XML documentation comments on test classes and test methods. Each test XML doc SHALL explicitly specify: what behavior is being tested, what test data/fixtures are used, why that data/fixtures are used, and which requirement IDs are being validated. No test project is exempt from this requirement.

**Status:** ✅ Active directive

**Covered by:** `.github/copilot-instructions.md`, `AGENTS.md`

## TR-MCP-AGENT-006

**Hosted .NET 9 Microsoft Agent Framework Library** — The solution SHALL provide a dedicated .NET 9 class library for hosting an MCP-aware agent inside external .NET applications built on Microsoft Agent Framework. The library SHALL expose DI-friendly registration and configuration APIs for MCP Server connectivity, agent construction, and host lifecycle integration so host applications do not need to assemble low-level MCP session-log or TODO plumbing themselves.

**Status:** ✅ Complete

**Covered by:** `ServiceCollectionExtensions`, `McpAgentOptions`, `McpAgentOptionsValidator`, `IMcpHostedAgent`, `IMcpHostedAgentFactory`, `McpHostedAgent`, `McpHostedAgentFactory`, `McpHostedAgentRegistration`

## TR-MCP-AGENT-007

**Built-In MCP Session Log, TODO, Repository, Desktop-Launch, and PowerShell Workflow for Hosted Agents** — The hosted agent library SHALL implement built-in workflow operations for session bootstrap, turn creation/update, TODO retrieval/update, TODO plan/status/implementation flows, repository read/list/write operations, local desktop process launch using the existing MCP Server contracts, and persistent in-process PowerShell sessions hosted directly inside the current .NET agent process. The workflow SHALL preserve canonical ID conventions for session IDs, request IDs, and TODO IDs, SHALL keep repository access scoped to repo-relative paths, SHALL expose desktop launch through the authenticated workspace context, SHALL keep PowerShell session state local to the hosted agent instance, SHALL expose the same local PowerShell session manager to host applications through `IMcpHostedAgent.PowerShellSessions`, and SHALL prefer reuse of existing client abstractions where server contracts already exist instead of duplicating transport logic.

**Status:** ✅ Complete

**Covered by:** `ISessionLogWorkflow`, `SessionLogWorkflow`, `SessionLogWorkflowContext`, `SessionLogTurnContext`, `ITodoWorkflow`, `TodoWorkflow`, `IMcpHostedAgent.PowerShellSessions`, `IHostedPowerShellSessionManager`, `McpHostedAgentToolAdapter`, `HostedPowerShellSessionManager`, `HostedPowerShellSessionHost`, `PowerShellSessionCreateResult`, `PowerShellSessionCommandResult`, `PowerShellSessionCloseResult`, `McpServerClient`, `RepoClient`, `DesktopClient`, `IMcpSessionIdentifierFactory`, `McpSessionIdentifierFactory`

## TR-MCP-HTTP-002

**Detailed and Sanitized HTTP 500 Error Contract** — All HTTP endpoints that return status code 500 SHALL emit a structured response body containing a non-empty human-readable error description that identifies the failing operation and provides actionable diagnostic context for the caller. The contract SHALL be applied centrally so endpoint implementations do not duplicate exception-to-response formatting. Response detail SHALL be sanitized to avoid leaking secrets, tokens, connection strings, or raw stack traces, while server-side logs SHALL retain the full exception detail needed for root-cause analysis.

**Status:** 🔴 Planned

## TR-MCP-CFG-006

**Administrative Configuration Snapshot and YAML Patch API** — `ConfigurationController` SHALL expose `GET /mcpserver/configuration` returning the current flattened `IConfiguration` view as `section:key` pairs, and `PATCH /mcpserver/configuration` accepting a flattened dictionary that patches only the submitted keys into `appsettings.yaml`.

Persistence SHALL be delegated to a dedicated helper service that resolves the correct `appsettings` file path, loads and writes YAML safely, and reloads `IConfigurationRoot` after updates. The endpoints SHALL use standard JWT Bearer admin authorization and remain closed when OIDC is disabled.

**Status:** ✅ Complete

**Covered by:** `ConfigurationController`, `AppSettingsFileService`, `Program.cs` (JWT Bearer auth setup), `WorkspaceController` (shared appsettings helper reuse)
