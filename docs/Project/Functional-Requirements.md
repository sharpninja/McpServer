# Functional Requirements (MCP Server)

## FR-SUPPORT-010 MCP Context Unification

Local MCP server providing context retrieval, TODO management, repository access, session logging, and ingestion capabilities for AI agent integration.

**Covered by:** `ContextController`, `TodoController`, `RepoController`, `SessionLogController`, `McpServerMcpTools`, `McpDbContext`, `HybridSearchService`, `EmbeddingService`, `VectorIndexService`, `Fts5SearchService`, `RepoFileService`, `IngestionCoordinator`

## FR-MCP-001 Configurable workspace root and paths

The server shall support configurable `RepoRoot`, `TodoFilePath`, `DataDirectory`, and index paths.

**Covered by:** `IngestionOptions`, `IOptions`

## FR-MCP-002 TODO management API

The server shall provide CRUD/query operations for TODO items over REST and STDIO.

**Covered by:** `TodoController`, `TodoService`, `SqliteTodoService`

## FR-MCP-003 Session log ingestion and query

The server shall ingest session logs and support searchable queries.

**Covered by:** `SessionLogController`, `SessionLogService`

## FR-MCP-004 Hybrid context search

The server shall support FTS and vector search over indexed content.

**Covered by:** `HybridSearchService`, `Fts5SearchService`, `VectorIndexService`, `EmbeddingService`

## FR-MCP-005 GitHub issue sync

The server shall support GitHub issue lifecycle integration and ISSUE-* TODO synchronization.

**Covered by:** `GitHubController`, `GitHubCliService`, `IssueTodoSyncService`

## FR-MCP-006 Multi-source ingestion

The server shall ingest repository files, session logs, external docs, and issue content.

**Covered by:** `IngestionCoordinator`, `RepoIngestor`, `SessionLogIngestor`, `ExternalDocsIngestor`, `GitHubIngestor`, `IssueIngestor`

## FR-MCP-007 Dual transport

The server shall support HTTP and STDIO MCP transports.

**Covered by:** `Program.cs`, `McpServerMcpTools`, `McpStdioHost`

## FR-MCP-008 Containerized deployment

The server shall support containerized deployment and packaged distribution.

**Covered by:** `Dockerfile`, `docker-compose.mcp.yml`

## FR-MCP-009 Workspace Management

The server shall support dynamic workspace registration, configuration, and lifecycle management — replacing static instance configuration — with directory scaffolding and Base64URL-encoded path keys. All workspaces are served on a single port via `X-Workspace-Path` header resolution (see FR-MCP-043).

**Covered by:** `WorkspaceController`, `WorkspaceService`, `WorkspaceConfigEntry`

## FR-MCP-011 Workspace Process Orchestration

The server shall manage workspace lifecycle via marker files: write `AGENTS-README-FIRST.yaml` on start, remove on stop. All workspaces share the single host process and port. Automatic startup of all registered workspaces writes markers on service start.

**Covered by:** `WorkspaceProcessManager`, `IWorkspaceProcessManager`, `MarkerFileService`

## FR-MCP-012 Tool Registry

Agents shall be able to discover tools by keyword search across global and workspace-scoped tool definitions, and install tool definitions from GitHub-backed bucket repositories.

**Covered by:** `ToolRegistryController`, `ToolRegistryService`, `ToolBucketService`

## FR-MCP-013 Per-Workspace Auth Tokens

The server shall protect all `/mcpserver/*` API endpoints with per-workspace cryptographic tokens that rotate on each service restart. Tokens are discoverable via the `AGENTS-README-FIRST.yaml` marker file, checked via the `X-Api-Key` header or `api_key` query parameter, and enforced by `WorkspaceAuthMiddleware`. Workspace resolution uses a three-tier chain: `X-Workspace-Path` header → API key reverse lookup → default workspace (see FR-MCP-043).

**Covered by:** `WorkspaceAuthMiddleware`, `WorkspaceTokenService`, `WorkspaceResolutionMiddleware`, `MarkerFileService`

## FR-MCP-014 Pairing Web UI

The server shall provide a browser-based login flow for authorized users to retrieve the server API key for MCP client configuration, backed by SHA-256 constant-time password verification and HttpOnly session cookies.

**Covered by:** `PairingHtml`, `PairingOptions`, `PairingSessionService`

## FR-MCP-015 Tunnel Providers

The server shall expose its HTTP interface to the internet via pluggable tunnel providers (ngrok, Cloudflare, FRP) configured through a strategy pattern and registered as hosted services.

**Covered by:** `NgrokTunnelProvider`, `CloudflareTunnelProvider`, `FrpTunnelProvider`

## FR-MCP-016 MCP Streamable HTTP Transport

The server shall expose a native MCP protocol endpoint at `/mcp-transport` coexisting with the REST API on the same port, enabling standard MCP client connections via `ModelContextProtocol.AspNetCore`.

**Covered by:** `Program.cs` (MapMcp), `ModelContextProtocol.AspNetCore`

## FR-MCP-017 Windows Service

The server shall run as a Windows service with automatic startup, failure recovery (restart on failure with 60 s delay), and PowerShell-based install/update/uninstall management.

**Covered by:** `Program.cs` (UseWindowsService), `Manage-McpService.ps1`

## FR-MCP-018 Marker File Agent Discovery

When a workspace is started, the server shall write an `AGENTS-README-FIRST.yaml` marker file to the workspace root containing the shared host port, all endpoint paths, a machine-readable prompt, and PID. All markers point to the same port; workspace identity is resolved via the `X-Workspace-Path` header. The marker shall be removed when the workspace is stopped.

**Covered by:** `MarkerFileService`, `WorkspaceProcessManager`

## FR-MCP-019 Workspace Host Controller Isolation

*Obsolete — replaced by single-app multi-tenant model (FR-MCP-043).* All controllers are available on the single host. Workspace lifecycle management endpoints on `WorkspaceController` remain admin-only.

## FR-MCP-020 Workspace Auto-Start on Service Startup

On service startup, the server shall automatically write marker files for all workspaces already registered, restoring agent discoverability without manual intervention. All workspaces share the single host port.

**Covered by:** `WorkspaceProcessManager` (IHostedService.StartAsync)

## FR-MCP-021 Workspace Auto-Init and Auto-Start on Creation

When a new workspace is registered, the server shall automatically initialize the workspace directory scaffold (todo.yaml, mcp.db, docs structure) and write its marker file, so the workspace is immediately operational on the shared port.

**Covered by:** `WorkspaceController` POST, `WorkspaceService.InitAsync`

## FR-MCP-022 Tool Registry Default Bucket Seeding

On first startup, the server shall seed default tool buckets from configuration (`Mcp:ToolRegistry:DefaultBuckets`) if they are not already registered, ensuring new installations have the primary tool repository available without manual setup.

**Covered by:** `ToolRegistryOptions`, `Program.cs`

## FR-MCP-023 AI-Assisted Requirements Analysis

The server shall provide a requirements analysis capability that invokes the Copilot CLI to examine a TODO item's title, description, and technical details, identify matching existing FR/TR IDs from the project docs, create new FR/TR entries for unaddressed functionality, and persist the assigned IDs back to the TODO item.

**Covered by:** `RequirementsService`, `IRequirementsService`, `ICopilotClient`

## FR-MCP-024 Markdown Session Log Ingestion

The ingestion pipeline shall parse legacy Markdown session log files (matching a `# Session Log – {title}` header pattern) into the unified session log schema alongside JSON session logs, enabling retroactive indexing of pre-existing agent session records.

**Covered by:** `MarkdownSessionLogParser`, `SessionLogIngestor`

## FR-MCP-025 Primary Workspace Detection and Deduplication

One workspace is designated as the **primary** workspace — served by the host process directly with no child `WebApplication` spun up. Only a marker file is written. Resolution order: (1) first enabled workspace with `IsPrimary = true` and lowest port; (2) enabled workspace with lowest port if none marked primary; (3) no primary if no workspaces enabled.

## FR-LOC-001 Localization Support

Localization and internationalization support for the MCP server. *(Planned — implementation scope TBD.)*

## FR-MCP-026 OIDC Authentication

The server shall support standards-based OIDC JWT Bearer authentication for management endpoints using a configurable open-source .NET OIDC provider. Optional external identity federation (for example, GitHub) may be configured through that provider. Management endpoints (agent mutations) require JWT; read endpoints use existing API key auth.

**Covered by:** `OidcAuthOptions`, `Program.cs`, `AgentController`, `Setup-McpKeycloak.ps1`, `setup-mcp-keycloak.sh`

## FR-MCP-027 Agent Definition Management

The server shall provide CRUD operations for agent type definitions with built-in defaults for well-known AI coding agents (copilot, cline, cursor, windsurf, claude-code, aider, continue). Built-in definitions are seeded on first run and cannot be deleted.

**Covered by:** `AgentController`, `AgentService`, `AgentDefaults`, `AgentDefinitionEntity`

## FR-MCP-028 Per-Workspace Agent Configuration

The server shall support per-workspace agent configuration with overrides for launch command, models, branch strategy, seed prompt, and instruction files. Agent pool definitions shall also support intent-default flags (`IsInteractiveDefault`, `IsTodoPlanDefault`, `IsTodoStatusDefault`, `IsTodoImplementDefault`) used for fallback routing when a request does not specify an agent name. Agents can be banned per-workspace or globally with optional PR-gated unbanning. All agent lifecycle events (add, launch, exit, ban, unban, delete, merge, init) are logged for audit.

**Covered by:** `AgentController`, `AgentService`, `AgentWorkspaceEntity`, `AgentEventLogEntity`

## FR-MCP-029 CQRS Framework

A standalone CQRS framework (`McpServer.Cqrs`) shall provide async command/query dispatch with a Result monad, decimal correlation IDs (`baseId.counter`), pipeline behaviors, and an `ILoggerProvider` implementation that auto-enriches structured logs with decomposed correlation context. The Dispatcher shall automatically log Result outcomes (success at Debug, errors at Error/Warning).

**Status:** ✅ Complete

**Covered by:** `McpServer.Cqrs` project (`Dispatcher`, `CallContext`, `CorrelationId`, `Result<T>`, `IPipelineBehavior`), `McpServer.Cqrs.Mvvm` (IViewModelRegistry, ViewModelRegistryExtensions), `McpServer.UI.Core` (WorkspaceListViewModel, WorkspacePolicyViewModel, AddUiCore DI extension)

**Implementation:** 37 unit tests passing. Provides `ICommand<T>`/`IQuery<T>` message types, `ICommandHandler<,>`/`IQueryHandler<,>` handlers, `Dispatcher` with pipeline behavior chain, `CallContext` with `CorrelationId` for structured logging, and `Result<T>` monad with success/error paths. MVVM layer adds `IViewModelRegistry` for CLI exec command support.

## FR-MCP-030 Director CLI

A console application (`McpServer.Director`) shall provide agent orchestration commands (init, add, launch, ban, unban, delete, merge, login, list, agents, validate, interactive) dispatched through the CQRS framework. Authentication uses OIDC Device Authorization Flow with the configured provider. Interactive mode uses Terminal.Gui v2 with ViewModel-bound screens.

**Status:** ✅ Complete

**Covered by:** `McpServer.Director` project — 15 source files: `Program.cs`, `McpHttpClient.cs`, `Auth/DirectorAuthOptions.cs`, `Auth/OidcAuthService.cs`, `Auth/TokenCache.cs`, `Commands/AuthCommands.cs`, `Commands/CommandHelpers.cs`, `Commands/DirectorCommands.cs`, `Commands/InteractiveCommand.cs`, `Screens/MainScreen.cs`, `Screens/HealthScreen.cs`, `Screens/AgentScreen.cs`, `Screens/TodoScreen.cs`, `Screens/SessionLogScreen.cs`, `Screens/WorkspaceListScreen.cs`, `Screens/WorkspacePolicyScreen.cs`, `Screens/LoginDialog.cs`, `Screens/ViewModelBinder.cs`

**Implementation:** 17 CLI commands registered via System.CommandLine. All commands communicate with the MCP server via `McpHttpClient` (reads connection details from `AGENTS-README-FIRST.yaml`). Auth uses OIDC Device Authorization Flow with token caching to `~/.mcpserver/tokens.json`. Interactive mode (`director interactive|tui|ui`) launches Terminal.Gui v2 with 6 tabs (Health, Workspaces, Agents, TODO, Sessions, Policy) plus a Login dialog, menu bar, auth status indicator, and keyboard shortcuts (F2 Login, F5 Refresh, Ctrl+Q Quit). ViewModels from `McpServer.UI.Core` are bound to Terminal.Gui controls via `ViewModelBinder` (INotifyPropertyChanged → Application.Invoke).

## FR-MCP-031 McpServer Management Web UI

A web-based management UI for McpServer providing workspace management, agent configuration, session log viewing, todo management, and system health monitoring. Integrates with the platform-wide open-source .NET OIDC provider for authentication. *(Planned — tracked as high-priority TODO.)*

## FR-MCP-032 Enhanced GitHub Integration

Enhanced GitHub integration capabilities including GitHub federation through the configured OIDC provider for user authentication, and GitHub OAuth for agent workspace management and PR workflows. *(Planned — tracked as high-priority TODO.)*

## FR-MCP-033 Natural Language Policy Management

A Copilot-integrated prompt tool that accepts natural language policy directives (e.g. "Ban chinese sources from all workspaces") and translates them into workspace configuration changes across all or targeted workspaces. Each policy change is session-logged per affected workspace with action type `policy_change`.

**Covered by:** `PolicyManagementTool` in `McpServer.Support.Mcp` *(planned; tracked by TODO `REQ-GAP-001`)*

## FR-MCP-034 Workspace Compliance Configuration

Per-workspace compliance configuration supporting four ban lists: `BannedLicenses` (SPDX identifiers), `BannedCountriesOfOrigin` (ISO 3166-1 alpha-2 codes), `BannedOrganizations`, and `BannedIndividuals`. Ban lists are conditionally rendered into the AGENTS-README-FIRST.yaml marker prompt via Handlebars templates. Agents must verify compliance before adding dependencies and log violations.

**Covered by:** `WorkspaceDto`, `WorkspaceCreateRequest`, `WorkspaceUpdateRequest`, `MarkerFileService`

## FR-MCP-035 Agent Values and Conduct Enforcement

The marker prompt shall include mandatory sections for: absolute honesty, correctness above speed, complete decision documentation, professional representation and audit trail (commits, PRs, issues logged in full), and source attribution (web references logged). These are non-configurable and always present.

**Covered by:** `MarkerFileService.DefaultPromptTemplate`

## FR-MCP-036 Audited Copilot Interactions

Every server-initiated Copilot interaction must be session-logged in every affected workspace. An `AuditedCopilotClient` decorator wraps `ICopilotClient` to create session log entries before and after each call, with action type `copilot_invocation`.

**Covered by:** `AuditedCopilotClient` decorator *(planned; tracked by TODO `REQ-GAP-001`)*

## FR-MCP-037 Director CLI Exec Command

The Director CLI shall support a `director exec <ViewModelName>` command that instantiates the named ViewModel from the registry, populates properties from JSON input (stdin or `--input` flag), executes the primary `IRelayCommand`, and returns the result as JSON to stdout. Exit code 0 = success, 1 = failure.

**Covered by:** `McpServer.Director` project, `IViewModelRegistry`

## FR-MCP-038 Session Continuity Protocol

Agents must follow a session continuity protocol: at session start, read the marker file, query recent session logs (limit=5), query current TODOs, and read Requirements-Matrix.md. During long sessions, post updated session logs every ~10 interactions. Requirements and design decisions must be captured as they emerge, not deferred.

**Covered by:** `MarkerFileService.DefaultPromptTemplate`

## FR-MCP-039 MCP Context Indexing for New Projects

All source files from `McpServer.Cqrs`, `McpServer.Cqrs.Mvvm`, `McpServer.UI.Core`, and `McpServer.Director` shall be indexed into the MCP context store for semantic search. The marker prompt lists these projects in the Available Capabilities section.

**Covered by:** Ingestion configuration, `MarkerFileService.DefaultPromptTemplate` *(planned; tracked by TODO `REQ-GAP-001`)*

## FR-MCP-040 Requirements Document CRUD Management

The server shall support CRUD operations for Functional Requirements (FR), Technical Requirements (TR), Testing Requirements (TEST), and FR-to-TR mapping rows backed by the canonical project requirements Markdown files.

**Covered by:** `RequirementsController`, `RequirementsDocumentService`, `IRequirementsRepository`

## FR-MCP-041 Requirements Document Generation

The server shall expose a requirements document generation endpoint that renders any canonical requirements document as Markdown and can return all documents together as a ZIP archive with canonical filenames.

**Covered by:** `RequirementsController` (`/mcpserver/requirements/generate`), `RequirementsDocumentService`, `RequirementsDocumentRenderer`

## FR-MCP-042 Requirements Management MCP Tools

The STDIO MCP tool surface shall expose requirements management tools for listing, generating, creating, updating, and deleting requirements entries so AI agents can manage requirements directly from a conversation.

**Covered by:** `FwhMcpTools` (`requirements_list`, `requirements_generate`, `requirements_create`, `requirements_update`, `requirements_delete`), `RequirementsDocumentService`

## FR-MCP-043 Multi-Tenant Workspace Resolution

The server shall resolve the target workspace per-request using a three-tier resolution chain: (1) `X-Workspace-Path` header (highest priority), (2) API key reverse lookup via `WorkspaceTokenService`, (3) default/primary workspace from configuration. All workspaces are served on a single port; no per-workspace Kestrel hosts are spawned.

**Covered by:** `WorkspaceResolutionMiddleware`, `WorkspaceContext`, `WorkspaceTokenService`, `WorkspaceAuthMiddleware`

## FR-MCP-044 Shared Database Multi-Tenancy

All workspace data shall be stored in a single shared SQLite database with a `WorkspaceId` discriminator column on every entity table. EF Core global query filters ensure workspace data isolation per-request. Cross-workspace queries use `IgnoreQueryFilters()` for admin operations.

**Covered by:** `McpDbContext`, `WorkspaceContext`, all entity types (`WorkspaceId` property)

## FR-MCP-045 Cross-Workspace TODO Move

The server shall support moving a TODO item from one workspace to another via REST (`POST /mcpserver/todo/{id}/move`) and STDIO (`todo_move` MCP tool), preserving all item fields including implementation tasks, requirements, and metadata. The move is implemented as create-in-target then delete-from-source.

**Covered by:** `TodoController.MoveAsync`, `FwhMcpTools.TodoMove`, `TodoMoveRequest`, `TodoServiceResolver`

## FR-MCP-046 Voice Conversation Sessions

The server shall provide voice-enabled agent interaction via Copilot CLI, supporting session creation with device binding, voice turn processing (synchronous and SSE streaming), transcript retrieval, session interruption, ESC-key injection for generation cancellation, and automatic idle session cleanup with configurable timeout. Voice connections can attach to running pooled agents, including agents currently processing one-shot work. One active session per device is enforced.

**Covered by:** `VoiceController`, `VoiceConversationService`, `VoiceConversationOptions`, `CopilotInteractiveSession`

## FR-MCP-047 Desktop Process Launch

The server shall support launching interactive desktop processes from a Windows service (LocalSystem) context using `CreateProcessAsUser` with WTS session token negotiation, enabling Copilot CLI and other GUI/console tools to run on the interactive desktop with stdio pipe redirection or visible console windows.

**Covered by:** `DesktopProcessLauncher`, `NativeMethods`

## FR-MCP-048 YAML Configuration Support

The server shall support `appsettings.yaml` as an optional configuration source loaded after `appsettings.json` with hot reload, enabling YAML-format configuration alongside JSON for local-only overrides.

**Covered by:** `Program.cs` (`AddYamlFile`), `NetEscapades.Configuration.Yaml`

## FR-MCP-049 Prompt Template Registry

The server shall provide a global prompt template registry with REST API endpoints (`/mcpserver/templates`) and MCP tools for CRUD operations (list, get, create, update, delete) and test/render operations. Templates are stored as YAML files, support Handlebars rendering with declared variables, and are filterable by category, tag, and keyword. A Director TUI tab shall enable template browsing and preview.

**Covered by:** `PromptTemplateController`, `PromptTemplateService`, `PromptTemplateRenderer`, `FwhMcpTools` (6 template tools), `TemplateClient`, `TemplatesScreen`

## FR-MCP-050 Template Externalization

The server shall load system prompt templates (marker prompt, TODO prompts, pairing HTML pages) from external YAML files via provider interfaces, with graceful fallback to built-in inline defaults when files are missing. Configuration overrides (`Mcp:MarkerPromptTemplate`, `Mcp:TodoPrompts`) take precedence over file-loaded templates. This enables runtime template customization without recompilation.

**Covered by:** `IMarkerPromptProvider`, `FileMarkerPromptProvider`, `ITodoPromptProvider`, `TodoPromptProvider`, `PairingHtmlRenderer`

## FR-MCP-051 System-Wide Default Copilot Model

The server SHALL allow configuration of a system-wide default Copilot model (e.g., `gpt-5.3-codex`) that is applied consistently across all Copilot session types — server-initiated CLI invocations (`CopilotClientOptions.Model`), voice conversation sessions (`VoiceConversationOptions.CopilotModel`), and built-in agent type defaults (`AgentDefaults`). The configured model SHALL be overridable per-workspace via agent configuration and per-invocation via explicit parameters.

**Technical Implementation:** [TR-MCP-CFG-005](./Technical-Requirements.md#tr-mcp-cfg-005) | [Details](./TR-per-FR-Mapping.md#fr-mcp-051)

## FR-MCP-052 Agent Pool Runtime Orchestration

The server shall maintain a configured pool of long-lived agent processes and route agent execution through pooled agents instead of independent ad-hoc launches.

Agent pool definitions shall include: `AgentName`, `AgentPath`, `AgentModel`, `AgentSeed`, and `AgentParameters`.

**Covered by:** `AgentPoolOptions` *(planned)*, `AgentPoolService` *(planned)*

## FR-MCP-053 One-Shot Queueing and Deferred Attachment

One-shot requests shall execute through the agent pool queue. If no eligible pooled agent is available, requests shall be queued and dequeued when an agent becomes available.

One-shot requesters shall receive processing lifecycle notifications and may attach to the running agent via interactive voice session or read-only response stream.

**Covered by:** `AgentPoolQueueService` *(planned)*, `AgentPoolController` *(planned)*

## FR-MCP-054 Agent Pool Availability and Control Endpoints

The server shall expose endpoints to list pooled agents and real-time availability, and provide runtime controls for connect, start, stop, recycle, queue inspection, queue cancel/remove, queue reorder (queued items only), and free-form one-shot enqueue.

The server shall expose a dedicated Agent Pool notification SSE stream with payload fields `AgentName`, `LastRequestPrompt`, and `SessionId`.

**Covered by:** `AgentPoolController` *(planned)*, `AgentPoolNotificationService` *(planned)*

## FR-MCP-055 Default Agent Selection by Request Intent

If a request omits `AgentName`, the server shall determine the request intent and select the configured default agent for that intent using intent-default flags.

One-shot endpoint context values shall support: `Plan`, `Status`, `Implement`, and `AdHoc`.

**Covered by:** `AgentPoolIntentResolver` *(planned)*, `AgentPoolService` *(planned)*

## FR-MCP-056 Template-Aware One-Shot Prompt Resolution

One-shot requests shall support template-driven and ad-hoc prompt modes. Template mode accepts `promptTemplateId` with optional values dictionary and workspace-context-derived values; caller-provided values override workspace context on key conflicts.

The server shall expose an endpoint that accepts prompt template ID plus values dictionary and returns the rendered prompt.

If context is provided without template ID, the server shall use current context-based template resolution. For `AdHoc` context without template ID, explicit ad-hoc prompt text is required.

One-shot endpoint template rendering shall support an `id` parameter used to populate `{id}` placeholders in templates. `id` is required only for template-resolved requests.

**Covered by:** `PromptTemplateController` *(planned extension)*, `AgentPoolController` *(planned)*

## FR-MCP-057 Director Agent Pool Management UI

Director shall provide an Agent Pool tab to monitor pooled agents and one-shot queue state, connect to an agent, recycle an agent immediately, stop/start an agent, cancel/remove/reorder queued requests, and enqueue free-form one-shot requests.

**Covered by:** `AgentPoolScreen` *(planned)*, `AgentPoolViewModel` *(planned)*

## FR-MCP-058 Interactive Presence Signaling

When a user disconnects from an interactive response stream, the server shall send `User is AFK.` to the agent session.

When a user reestablishes an interactive response stream connection, the server shall send `User is here.` to the agent after stream establishment.

These presence messages do not apply to one-shot sessions.

**Covered by:** `AgentPoolStreamService` *(planned)*, `VoiceConversationService` *(planned extension)*

## FR-MCP-059 DI-Centered Single Source of Truth State Flow

The system SHALL enforce a DI-centered Single Source of Truth architecture across `McpServer.Support.Mcp`: authoritative mutable data sources must be owned by DI-registered singleton or scoped services, services shall notify state availability/changes via `INotifyPropertyChanged`, and consumers shall pull current state from the owning service rather than receiving pushed data payloads.

**Technical Implementation:** [TR-MCP-ARCH-002](./Technical-Requirements.md#tr-mcp-arch-002) | [Details](./TR-per-FR-Mapping.md#fr-mcp-059)

## FR-MCP-060 Director MVVM/CQRS Full Endpoint Coverage

Director SHALL expose complete administrative endpoint coverage through the shared `McpServer.UI.Core` MVVM/CQRS layer so interactive tabs and `director exec` operations use the same command/query contracts, handlers, and authorization rules.

Each covered administration area SHALL provide ViewModel-first orchestration (list/detail or operation-focused ViewModel patterns), and Director screens SHALL remain presentation-only shells that delegate state and workflows to ViewModels and CQRS dispatch.

Tab composition SHALL be role-aware and declarative, with registration metadata separated from shell rendering logic and enforced via shared authorization policy checks.

**Technical Implementation:** [TR-MCP-DIR-005](./Technical-Requirements.md#tr-mcp-dir-005) | [TR-MCP-DIR-006](./Technical-Requirements.md#tr-mcp-dir-006) | [TR-MCP-DIR-007](./Technical-Requirements.md#tr-mcp-dir-007) | [TR-MCP-DIR-008](./Technical-Requirements.md#tr-mcp-dir-008) | [Details](./TR-per-FR-Mapping.md#fr-mcp-060)

## FR-MCP-061 Canonical TODO and Session Identifier Conventions

The server shall enforce canonical identifier conventions for newly created TODO and session log payloads:

- TODO IDs must match `<SDLC-PHASE>-<AREA>-###` using uppercase kebab-case.
- Session IDs must match `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>` and be prefixed by the exact `sourceType`/`agent`.
- Request IDs must match `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`.

Validation failures return client-visible errors without mutating persisted data.

**Covered by:** `TodoValidator`, `TodoService`, `SqliteTodoService`, `SessionLogIdentifierValidator`, `SessionLogController`, `SessionLogService`

## FR-MCP-062 Workspace Change Notifications

The server shall provide a real-time workspace change notification system that publishes create/update/delete domain events for workspace mutations (TODOs, session logs, repo files, context sync, tool registry, tool buckets, workspaces, GitHub operations, marker lifecycle, agents, and requirements) over Server-Sent Events at `GET /mcpserver/events`, with optional category filtering.

**Covered by:** `IChangeEventBus`, `ChannelChangeEventBus`, `EventStreamController`, `TodoService`, `SqliteTodoService`, `SessionLogService`, `RepoFileService`, `ToolRegistryService`, `ToolBucketService`, `WorkspaceService`, `WorkspaceController`, `AgentService`, `RequirementsDocumentService`, `IngestionCoordinator`, `GitHubController`, `WorkspaceProcessManager`
