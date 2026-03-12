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

*Moved to [Requirements-WebUI.md](Requirements-WebUI.md#fr-mcp-014-pairing-web-ui)*

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

The server shall support per-workspace agent configuration with overrides for launch command, models, branch strategy, seed prompt, instruction files, isolation strategy, restart policy, and marker additions. Agents can be banned per-workspace or globally with optional PR-gated unbanning. All agent lifecycle events (add, launch, exit, ban, unban, delete, merge, init) are logged for audit.

**Covered by:** `AgentController`, `AgentService`, `AgentWorkspaceEntity`, `AgentEventLogEntity`, `AgentHealthMonitorService`

## FR-MCP-029 CQRS Framework

A standalone CQRS framework (`McpServer.Cqrs`) shall provide async command/query dispatch with a Result monad, decimal correlation IDs (`baseId.counter`), pipeline behaviors, and an `ILoggerProvider` implementation that auto-enriches structured logs with decomposed correlation context. The Dispatcher shall automatically log Result outcomes (success at Debug, errors at Error/Warning).

**Status:** ✅ Complete

**Covered by:** `McpServer.Cqrs` project (`Dispatcher`, `CallContext`, `CorrelationId`, `Result<T>`, `IPipelineBehavior`), `McpServer.Cqrs.Mvvm` (IViewModelRegistry, ViewModelRegistryExtensions), `McpServer.UI.Core` (WorkspaceListViewModel, WorkspacePolicyViewModel, AddUiCore DI extension)

**Implementation:** 37 unit tests passing. Provides `ICommand<T>`/`IQuery<T>` message types, `ICommandHandler<,>`/`IQueryHandler<,>` handlers, `Dispatcher` with pipeline behavior chain, `CallContext` with `CorrelationId` for structured logging, and `Result<T>` monad with success/error paths. MVVM layer adds `IViewModelRegistry` for CLI exec command support.

## FR-MCP-030 Director CLI

*Moved to [Requirements-Director.md](Requirements-Director.md#fr-mcp-030-director-cli)*

## FR-MCP-031 McpServer Management Web UI

*Moved to [Requirements-WebUI.md](Requirements-WebUI.md#fr-mcp-031-mcpserver-management-web-ui)*

## FR-MCP-032 Enhanced GitHub Integration

Enhanced GitHub integration capabilities including GitHub federation through the configured OIDC provider for user authentication, and GitHub OAuth for agent workspace management and PR workflows. *(Planned — tracked as high-priority TODO.)*

## FR-MCP-033 Natural Language Policy Management

A Copilot-integrated prompt tool that accepts natural language policy directives (e.g. "Ban chinese sources from all workspaces") and translates them into workspace configuration changes across all or targeted workspaces. Each policy change is session-logged per affected workspace with action type `policy_change`.

**Covered by:** `WorkspaceController` (`POST /mcpserver/workspace/policy`), `WorkspacePolicyService`, `WorkspacePolicyDirectiveParser`, `McpServerMcpTools.workspace_policy_apply`

## FR-MCP-034 Workspace Compliance Configuration

Per-workspace compliance configuration supporting four ban lists: `BannedLicenses` (SPDX identifiers), `BannedCountriesOfOrigin` (ISO 3166-1 alpha-2 codes), `BannedOrganizations`, and `BannedIndividuals`. Ban lists are conditionally rendered into the AGENTS-README-FIRST.yaml marker prompt via Handlebars templates. Agents must verify compliance before adding dependencies and log violations.

**Covered by:** `WorkspaceDto`, `WorkspaceCreateRequest`, `WorkspaceUpdateRequest`, `MarkerFileService`

## FR-MCP-035 Agent Values and Conduct Enforcement

The marker prompt shall include mandatory sections for: absolute honesty, correctness above speed, complete decision documentation, professional representation and audit trail (commits, PRs, issues logged in full), and source attribution (web references logged). These are non-configurable and always present.

**Covered by:** `templates/prompt-templates.yaml` (default-marker-prompt)

## FR-MCP-036 Audited Copilot Interactions

Every server-initiated Copilot interaction must be session-logged in every affected workspace. An `AuditedCopilotClient` decorator wraps `ICopilotClient` to create session log entries before and after each call, with action type `copilot_invocation`.

**Covered by:** `AuditedCopilotClient`, `Program.cs` DI registration, `McpStdioHost` DI registration, `CopilotServiceCollectionExtensions`

## FR-MCP-037 Director CLI Exec Command

*Moved to [Requirements-Director.md](Requirements-Director.md#fr-mcp-037-director-cli-exec-command)*

## FR-MCP-038 Session Continuity Protocol

Agents must follow a session continuity protocol: at session start, read the marker file, query recent session logs (limit=5), query current TODOs, and read Requirements-Matrix.md. During long sessions, post updated session logs every ~10 interactions. Requirements and design decisions must be captured as they emerge, not deferred.

**Covered by:** `templates/prompt-templates.yaml` (default-marker-prompt)

## FR-MCP-039 MCP Context Indexing for New Projects

All source files from `McpServer.Cqrs`, `McpServer.Cqrs.Mvvm`, `McpServer.UI.Core`, and `McpServer.Director` shall be indexed into the MCP context store for semantic search. The marker prompt lists these projects in the Available Capabilities section.

**Covered by:** `Program.cs` / `McpStdioHost` `PostConfigure<IngestionOptions>` allowlist merge, `appsettings.yaml` `Mcp:RepoAllowlist`, `templates/prompt-templates.yaml` (default-marker-prompt)

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

## FR-MCP-050 Per-Agent Workspace Runtime Management

The server shall provide runtime management for workspace-bound agents, including process launch/stop/status, configurable isolation modes (`none`, `worktree`, `clone`), branch strategy handling (`direct`, `feature-branch`, `worktree`), session-log linkage to known agent definitions, marker-file agent-specific instruction sections, and restart-policy-driven health monitoring.

**Covered by:** `AgentService`, `AgentController`, `IAgentProcessManager`, `AgentProcessManager`, `IAgentIsolationStrategy`, `AgentIsolationStrategyResolver`, `IAgentBranchStrategy`, `AgentBranchStrategyResolver`, `WorkspaceProcessManager`, `SessionLogService`, `AgentHealthMonitorService`

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

*Moved to [Requirements-Director.md](Requirements-Director.md#fr-mcp-057-director-agent-pool-management-ui)*

## FR-MCP-058 Interactive Presence Signaling

When a user disconnects from an interactive response stream, the server shall send `User is AFK.` to the agent session.

When a user reestablishes an interactive response stream connection, the server shall send `User is here.` to the agent after stream establishment.

These presence messages do not apply to one-shot sessions.

**Covered by:** `AgentPoolStreamService` *(planned)*, `VoiceConversationService` *(planned extension)*

## FR-MCP-059 DI-Centered Single Source of Truth State Flow

The system SHALL enforce a DI-centered Single Source of Truth architecture across `McpServer.Support.Mcp`: authoritative mutable data sources must be owned by DI-registered singleton or scoped services, services shall notify state availability/changes via `INotifyPropertyChanged`, and consumers shall pull current state from the owning service rather than receiving pushed data payloads.

**Technical Implementation:** [TR-MCP-ARCH-002](./Technical-Requirements.md#tr-mcp-arch-002) | [Details](./TR-per-FR-Mapping.md#fr-mcp-059)

## FR-MCP-060 Director MVVM/CQRS Full Endpoint Coverage

*Moved to [Requirements-Director.md](Requirements-Director.md#fr-mcp-060-director-mvvmcqrs-full-endpoint-coverage)*

## FR-MCP-061 Canonical TODO and Session Identifier Conventions

The server shall enforce canonical identifier conventions for newly created TODO and session log payloads:

- Persisted TODO IDs must match either `<SDLC-PHASE>-<AREA>-###` using uppercase kebab-case or `ISSUE-{number}` for canonical GitHub-backed TODOs.
- Create requests may use `ISSUE-NEW` only as a temporary server-side alias for immediate GitHub-backed TODO creation; persisted TODO IDs must still be canonical.
- Session IDs must match `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>` and be prefixed by the exact `sourceType`/`agent`.
- Request IDs must match `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`.

Validation failures return client-visible errors without mutating persisted data.

**Covered by:** `TodoValidator`, `TodoService`, `SqliteTodoService`, `TodoCreationService`, `SessionLogIdentifierValidator`, `SessionLogController`, `SessionLogService`

## FR-MCP-062 Workspace Change Notifications

The server shall provide a real-time workspace change notification system that publishes create/update/delete domain events for workspace mutations (TODOs, session logs, repo files, context sync, tool registry, tool buckets, workspaces, GitHub operations, marker lifecycle, agents, and requirements) over Server-Sent Events at `GET /mcpserver/events`, with optional category filtering.

**Covered by:** `IChangeEventBus`, `ChannelChangeEventBus`, `EventStreamController`, `TodoService`, `SqliteTodoService`, `SessionLogService`, `RepoFileService`, `ToolRegistryService`, `ToolBucketService`, `WorkspaceService`, `WorkspaceController`, `AgentService`, `RequirementsDocumentService`, `IngestionCoordinator`, `GitHubController`, `WorkspaceProcessManager`

## FR-MCP-063 Workspace GitHub OAuth Bootstrap, Token Lifecycle, and Actions Control

The server shall provide workspace-scoped GitHub authentication controls and workflow operations that support OAuth bootstrap and secure token usage without breaking existing gh CLI compatibility.

Functional behavior shall include:

- OAuth bootstrap discovery endpoints exposing configured client ID, redirect URI, authorize endpoint, and scopes.
- Workspace-scoped token lifecycle endpoints to set, inspect, and revoke GitHub tokens.
- Authenticated GitHub execution path that prefers stored workspace token credentials and falls back to ambient gh auth only when policy allows it.
- GitHub Actions workflow run management endpoints for list/detail/rerun/cancel operations.
- Typed client parity for all new GitHub auth and workflow run endpoints.

**Technical Implementation:** [TR-MCP-GH-001](./Technical-Requirements.md#tr-mcp-gh-001) | [TR-MCP-GH-002](./Technical-Requirements.md#tr-mcp-gh-002) | [TR-MCP-GH-003](./Technical-Requirements.md#tr-mcp-gh-003) | [TR-MCP-GH-004](./Technical-Requirements.md#tr-mcp-gh-004)

**Covered by:** `GitHubIntegrationOptions`, `FileGitHubWorkspaceTokenStore`, `GitHubController`, `GitHubCliService`, `ProcessRunner`, `GitHubClient`

#### FR-MCP-064: Marketing and Adoption Documentation
The system SHALL provide marketing-oriented documentation that clearly explains what McpServer is, its key feature set, why adopters need it, and the currently supported UI tooling surfaces (including VS extension and Web UI experiences).
**Technical Implementation:** [TR-MCP-DOC-001](./Technical-Requirements.md#tr-mcp-doc-001) | [Details](./TR-per-FR-Mapping.md#fr-mcp-064)

## FR-MCP-065 Direct Website URL Ingestion

The server shall ingest remote website content directly from one URL (with optional bounded same-host crawling) into the context store and GraphRAG pipeline without pre-downloading files into `docs/external`.

**Covered by:** `ContextController` (`POST /mcpserver/context/ingest-website`), `WebsiteIngestor`, `IngestionCoordinator`, `FwhMcpTools` (`context_ingest_website`), `ContextClient.IngestWebsiteAsync`

## FR-MCP-066 Hosted Microsoft Agent Framework Library

The system SHALL provide a .NET 9 class library that packages an MCP-aware agent for hosting inside external .NET applications built on Microsoft Agent Framework.

The hosted agent SHALL include a built-in workflow that treats MCP Server session logging, TODO management, repository file access, local desktop process launch, and stateful in-process PowerShell sessions as first-class primitives, allowing host applications to bootstrap/continue session logs, create and update turns, inspect and mutate TODO items, browse repository files, launch local programs, run PowerShell commands inside persistent local runspaces, drive those runspaces directly through the host-facing agent contract when needed, and execute plan/status/implementation task flows without reimplementing those integrations.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-AGENT-006](./Technical-Requirements.md#tr-mcp-agent-006) | [TR-MCP-AGENT-007](./Technical-Requirements.md#tr-mcp-agent-007) | [Details](./TR-per-FR-Mapping.md#fr-mcp-066)

**Covered by:** `McpServer.McpAgent` (`ServiceCollectionExtensions`, `McpAgentOptions`, `Hosting/*`, `PowerShellSessions/*`, `SessionLog/*`, `Todo/*`), `McpServer.Client` (`McpServerClient`, `RepoClient`, `DesktopClient`), `McpServer.McpAgent.SampleHost` (`Program.cs`, `SampleHostPreviewFactory.cs`)

## FR-MCP-067 Detailed Internal Server Error Responses

The system SHALL return a detailed client-visible error description for every endpoint response that fails with HTTP 500.

Detailed 500 responses SHALL describe the failed operation clearly enough for callers to diagnose the failure path and distinguish server faults from client mistakes, while remaining sanitized so secrets, tokens, and other sensitive internals are not exposed in the response body.

**Technical Implementation:** [TR-MCP-HTTP-002](./Technical-Requirements.md#tr-mcp-http-002) | [Details](./TR-per-FR-Mapping.md#fr-mcp-067)

## FR-MCP-068 Administrative Configuration Management API

The server SHALL provide an admin-only configuration API that returns the current effective configuration as flattened key-value pairs and supports patching selected values back into `appsettings.yaml` without rewriting unrelated settings or serializing values that originate only from non-file configuration providers.

The configuration-management endpoints SHALL require standard JWT Bearer authentication with the `admin` role. When OIDC is not configured, the endpoints SHALL remain unavailable.

**Technical Implementation:** [TR-MCP-CFG-006](./Technical-Requirements.md#tr-mcp-cfg-006) | [Details](./TR-per-FR-Mapping.md#fr-mcp-068)

**Covered by:** `ConfigurationController`, `AppSettingsFileService`, `Program.cs` (`ConfigurationAdmin` policy), `WorkspaceController` (shared appsettings helper reuse)

## FR-MCP-069 Immediate GitHub-Backed TODO Creation

The server shall support a create-time TODO identifier of `ISSUE-NEW` that immediately creates a GitHub issue, determines the resulting issue number, and persists the local TODO using the canonical `ISSUE-{number}` identifier returned by GitHub.

This behavior shall be available through all server-side TODO creation entry points that already support normal TODO creation. Callers shall receive the canonical persisted identifier rather than the temporary `ISSUE-NEW` alias.

**Technical Implementation:** [TR-MCP-TODO-003](./Technical-Requirements.md#tr-mcp-todo-003) | [TR-MCP-GH-005](./Technical-Requirements.md#tr-mcp-gh-005) | [Details](./TR-per-FR-Mapping.md#fr-mcp-069)

**Covered by:** `TodoCreationService`, `GitHubCliService`, `TodoController`, `FwhMcpTools`, `VoiceConversationService`

## FR-MCP-070 Authoritative ISSUE-* Update Sync and Immutable Descriptions

The server shall treat MCP TODO updates as authoritative for existing `ISSUE-{number}` TODO items. When an `ISSUE-*` TODO is updated through a server TODO update surface, the server shall push the authoritative MCP state to GitHub, append a GitHub issue comment describing the applied change set, and keep the ISSUE description/body immutable after the first sync.

Priority synchronization shall use canonical GitHub labels in the form `priority: HIGH`, `priority: MEDIUM`, or `priority: LOW`. GitHub-to-TODO refreshes for existing `ISSUE-*` items shall preserve the local priority and description that were already established by the first sync.

**Technical Implementation:** [TR-MCP-TODO-004](./Technical-Requirements.md#tr-mcp-todo-004) | [TR-MCP-GH-006](./Technical-Requirements.md#tr-mcp-gh-006) | [Details](./TR-per-FR-Mapping.md#fr-mcp-070)

**Covered by:** `TodoUpdateService`, `IssueTodoSyncService`, `TodoController`, `FwhMcpTools`, `VoiceConversationService`

## FR-MCP-071 ISSUE Comment Round-Trip and GitHub-Driven Closure Reconciliation

The server shall round-trip `ISSUE-{number}` discussion between GitHub and MCP TODOs without mutating the established TODO description. GitHub-origin issue comments shall sync into the TODO note inside a generated GitHub-comments section, while user-authored TODO note content outside that generated section shall remain preserved across subsequent syncs.

When an `ISSUE-*` TODO is updated locally with new note text, the server shall propagate the appended TODO-authored comment back to GitHub as an issue comment. When the GitHub issue is later closed outside MCP, a GitHub-to-TODO sync shall mark the corresponding TODO as done.

**Technical Implementation:** [TR-MCP-GH-007](./Technical-Requirements.md#tr-mcp-gh-007) | [Details](./TR-per-FR-Mapping.md#fr-mcp-071)

**Covered by:** `IssueTodoSyncService`, `TodoUpdateService`, `GitHubController`, `TodoController`

