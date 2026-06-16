# Functional Requirements (MCP Server)

## FR-01 FR-01

Legacy imported functional identifier retained for historical traceability. Status: reserved/superseded by MCP-specific functional requirements; no active implementation work is tracked under this stub.

## FR-02 FR-02

Legacy imported functional identifier retained for historical traceability. Status: reserved/superseded by MCP-specific functional requirements; no active implementation work is tracked under this stub.

## FR-03 FR-03

Legacy imported functional identifier retained for historical traceability. Status: reserved/superseded by MCP-specific functional requirements; no active implementation work is tracked under this stub.

## FR-04 FR-04

Legacy imported functional identifier retained for historical traceability. Status: reserved/superseded by MCP-specific functional requirements; no active implementation work is tracked under this stub.

## FR-05 FR-05

Legacy imported functional identifier retained for historical traceability. Status: reserved/superseded by MCP-specific functional requirements; no active implementation work is tracked under this stub.

## FR-06 FR-06

Legacy imported functional identifier retained for historical traceability. Status: reserved/superseded by MCP-specific functional requirements; no active implementation work is tracked under this stub.

## FR-07 FR-07

Legacy imported functional identifier retained for historical traceability. Status: reserved/superseded by MCP-specific functional requirements; no active implementation work is tracked under this stub.

## FR-08 FR-08

Legacy imported functional identifier retained for historical traceability. Status: reserved/superseded by MCP-specific functional requirements; no active implementation work is tracked under this stub.

## FR-09 FR-09

Legacy imported functional identifier retained for historical traceability. Status: reserved/superseded by MCP-specific functional requirements; no active implementation work is tracked under this stub.

## FR-10 FR-10

Legacy imported functional identifier retained for historical traceability. Status: reserved/superseded by MCP-specific functional requirements; no active implementation work is tracked under this stub.

## FR-LOC-001 Localization Support

Localization and internationalization support for the MCP server. *(Planned - implementation scope TBD.)*

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

The server shall support dynamic workspace registration, configuration, and lifecycle management - replacing static instance configuration - with directory scaffolding and Base64URL-encoded path keys. All workspaces are served on a single port via `X-Workspace-Path` header resolution (see FR-MCP-043).

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

*Obsolete - replaced by single-app multi-tenant model (FR-MCP-043).* All controllers are available on the single host. Workspace lifecycle management endpoints on `WorkspaceController` remain admin-only.

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

The ingestion pipeline shall parse legacy Markdown session log files (matching a `# Session Log - {title}` header pattern) into the unified session log schema alongside JSON session logs, enabling retroactive indexing of pre-existing agent session records.

**Covered by:** `MarkdownSessionLogParser`, `SessionLogIngestor`

## FR-MCP-025 Primary Workspace Detection and Deduplication

One workspace is designated as the **primary** workspace - served by the host process directly with no child `WebApplication` spun up. Only a marker file is written. Resolution order: (1) first enabled workspace with `IsPrimary = true` and lowest port; (2) enabled workspace with lowest port if none marked primary; (3) no primary if no workspaces enabled.

**Status:** ✅ Complete

**Covered by:** `WorkspaceProcessManager`, `WorkspaceConfigEntry`, `Program.cs` primary-workspace resolution block

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

**Covered by:** `McpServer.Cqrs` project (`Dispatcher`, `CallContext`, `CorrelationId`, `Result<T>`, `IPipelineBehavior`), `McpServer.Cqrs.Mvvm` (IViewModelRegistry, ViewModelRegistryExtensions). Workspace and policy view-models moved to the separate `McpServerManager` repository (former `McpServer.UI.Core` / `WorkspaceListViewModel` / `WorkspacePolicyViewModel` / `AddUiCore` DI extension).

**Implementation:** 37 unit tests passing. Provides `ICommand<T>`/`IQuery<T>` message types, `ICommandHandler<,>`/`IQueryHandler<,>` handlers, `Dispatcher` with pipeline behavior chain, `CallContext` with `CorrelationId` for structured logging, and `Result<T>` monad with success/error paths. MVVM layer adds `IViewModelRegistry` for CLI exec command support.

## FR-MCP-030 Director CLI

*Moved to [Requirements-Director.md](Requirements-Director.md#fr-mcp-030-director-cli)*

## FR-MCP-031 McpServer Management Web UI

*Moved to [Requirements-WebUI.md](Requirements-WebUI.md#fr-mcp-031-mcpserver-management-web-ui)*

## FR-MCP-032 Enhanced GitHub Integration

Enhanced GitHub integration capabilities including GitHub federation through the configured OIDC provider for user authentication, and GitHub OAuth for agent workspace management and PR workflows. *(Planned - tracked as high-priority TODO.)*

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

Repo-local source files from McpServer.Cqrs and McpServer.Cqrs.Mvvm shall be indexed into the MCP context store for semantic search. McpServer.UI.Core and McpServer.Director have moved to the separate McpServerManager repository and must not be advertised as indexed capabilities in this repository marker.

## FR-MCP-040 Requirements Document CRUD Management

The server shall support CRUD operations for Functional Requirements (FR), Technical Requirements (TR), Testing Requirements (TEST), and FR-to-TR mapping rows backed by the canonical project requirements Markdown files.

**Covered by:** `RequirementsController`, `RequirementsDocumentService`, `IRequirementsRepository`

## FR-MCP-041 Requirements Document Generation

The server shall expose a requirements document generation endpoint that renders any canonical requirements document as Markdown, including the `Requirements-Matrix.md` traceability matrix, and exports all canonical documents directly to the workspace with canonical filenames.

**Status:** ✅ Complete

**Covered by:** `RequirementsController` (`POST /mcpserver/requirements/generate`), `RequirementsDocumentService`, `RequirementsDocumentRenderer`

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

**Covered by:** `PromptTemplateController`, `PromptTemplateService`, `PromptTemplateRenderer`, `FwhMcpTools` (6 template tools), `TemplateClient`. The Director template-browsing TUI surface (`TemplatesScreen`) moved to the separate `McpServerManager` repository.

## FR-MCP-050 Per-Agent Workspace Runtime Management

The server shall provide runtime management for workspace-bound agents, including process launch/stop/status, configurable isolation modes (`none`, `worktree`, `clone`), branch strategy handling (`direct`, `feature-branch`, `worktree`), session-log linkage to known agent definitions, marker-file agent-specific instruction sections, and restart-policy-driven health monitoring.

**Covered by:** `AgentService`, `AgentController`, `IAgentProcessManager`, `AgentProcessManager`, `IAgentIsolationStrategy`, `AgentIsolationStrategyResolver`, `IAgentBranchStrategy`, `AgentBranchStrategyResolver`, `WorkspaceProcessManager`, `SessionLogService`, `AgentHealthMonitorService`

## FR-MCP-051 System-Wide Default Copilot Model

The server SHALL allow configuration of a system-wide default Copilot model (e.g., `gpt-5.3-codex`) that is applied consistently across all Copilot session types - server-initiated CLI invocations (`CopilotClientOptions.Model`), voice conversation sessions (`VoiceConversationOptions.CopilotModel`), and built-in agent type defaults (`AgentDefaults`). The configured model SHALL be overridable per-workspace via agent configuration and per-invocation via explicit parameters.

**Technical Implementation:** [TR-MCP-CFG-005](./Technical-Requirements.md#tr-mcp-cfg-005) | [Mapping](./TR-per-FR-Mapping.md)

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

**Technical Implementation:** [TR-MCP-ARCH-002](./Technical-Requirements.md#tr-mcp-arch-002) | [Mapping](./TR-per-FR-Mapping.md)

## FR-MCP-060 Director MVVM/CQRS Full Endpoint Coverage

*Moved to [Requirements-Director.md](Requirements-Director.md#fr-mcp-060-director-mvvmcqrs-full-endpoint-coverage)*

## FR-MCP-061 Canonical TODO and Session Identifier Conventions

The server shall enforce canonical identifier conventions for newly created TODO and session log payloads:

- Persisted TODO IDs must match either uppercase kebab-case ending in a three-digit sequence suffix (for example `PHASE0-REMOTE-001` or `MCP-TODO-CREATE-001`) or `ISSUE-{number}` for canonical GitHub-backed TODOs.
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

## FR-MCP-064 Marketing and Adoption Documentation

The system SHALL provide marketing-oriented documentation that clearly explains what McpServer is, its key feature set, why adopters need it, and the currently supported UI tooling surfaces (including VS extension and Web UI experiences).

**Technical Implementation:** [TR-MCP-DOC-001](./Technical-Requirements.md#tr-mcp-doc-001) | [Mapping](./TR-per-FR-Mapping.md)

## FR-MCP-065 Direct Website URL Ingestion

The server shall ingest remote website content directly from one URL (with optional bounded same-host crawling) into the context store and GraphRAG pipeline without pre-downloading files into `docs/external`.

**Covered by:** `ContextController` (`POST /mcpserver/context/ingest-website`), `WebsiteIngestor`, `IngestionCoordinator`, `FwhMcpTools` (`context_ingest_website`), `ContextClient.IngestWebsiteAsync`

## FR-MCP-066 Hosted Microsoft Agent Framework Library

The system SHALL provide a .NET 9 class library that packages an MCP-aware agent for hosting inside external .NET applications built on Microsoft Agent Framework.

The hosted agent SHALL include a built-in workflow that treats MCP Server session logging, TODO management, repository file access, local desktop process launch, and stateful in-process PowerShell sessions as first-class primitives, allowing host applications to bootstrap/continue session logs, create and update turns, inspect and mutate TODO items, browse repository files, launch local programs, run PowerShell commands inside persistent local runspaces, drive those runspaces directly through the host-facing agent contract when needed, and execute plan/status/implementation task flows without reimplementing those integrations.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-AGENT-006](./Technical-Requirements.md#tr-mcp-agent-006) | [TR-MCP-AGENT-007](./Technical-Requirements.md#tr-mcp-agent-007) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `McpServer.McpAgent` (`ServiceCollectionExtensions`, `McpAgentOptions`, `Hosting/*`, `PowerShellSessions/*`, `SessionLog/*`, `Todo/*`), `McpServer.Client` (`McpServerClient`, `RepoClient`, `DesktopClient`), `McpServer.McpAgent.SampleHost` (`Program.cs`, `SampleHostPreviewFactory.cs`)

## FR-MCP-067 Detailed Internal Server Error Responses

The system SHALL return a detailed client-visible error description for every endpoint response that fails with HTTP 500.

Detailed 500 responses SHALL describe the failed operation clearly enough for callers to diagnose the failure path and distinguish server faults from client mistakes, while remaining sanitized so secrets, tokens, and other sensitive internals are not exposed in the response body.

**Technical Implementation:** [TR-MCP-HTTP-002](./Technical-Requirements.md#tr-mcp-http-002) | [Mapping](./TR-per-FR-Mapping.md)

## FR-MCP-068 Administrative Configuration Management API

The server SHALL provide an admin-only configuration API that returns the current effective configuration as flattened key-value pairs and supports patching selected values back into `appsettings.yaml` without rewriting unrelated settings or serializing values that originate only from non-file configuration providers.

The configuration-management endpoints SHALL require standard JWT Bearer authentication with the `admin` role. When OIDC is not configured, the endpoints SHALL remain unavailable.

**Technical Implementation:** [TR-MCP-CFG-006](./Technical-Requirements.md#tr-mcp-cfg-006) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `ConfigurationController`, `AppSettingsFileService`, `Program.cs` (`ConfigurationAdmin` policy), `WorkspaceController` (shared appsettings helper reuse)

## FR-MCP-069 Immediate GitHub-Backed TODO Creation

The server shall support a create-time TODO identifier of `ISSUE-NEW` that immediately creates a GitHub issue, determines the resulting issue number, and persists the local TODO using the canonical `ISSUE-{number}` identifier returned by GitHub.

This behavior shall be available through all server-side TODO creation entry points that already support normal TODO creation. Callers shall receive the canonical persisted identifier rather than the temporary `ISSUE-NEW` alias.

**Technical Implementation:** [TR-MCP-TODO-003](./Technical-Requirements.md#tr-mcp-todo-003) | [TR-MCP-GH-005](./Technical-Requirements.md#tr-mcp-gh-005) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `TodoCreationService`, `GitHubCliService`, `TodoController`, `FwhMcpTools`, `VoiceConversationService`

## FR-MCP-070 Authoritative ISSUE-* Update Sync and Immutable Descriptions

The server shall treat MCP TODO updates as authoritative for existing `ISSUE-{number}` TODO items. When an `ISSUE-*` TODO is updated through a server TODO update surface, the server shall push the authoritative MCP state to GitHub, append a GitHub issue comment describing the applied change set, and keep the ISSUE description/body immutable after the first sync.

Priority synchronization shall use canonical GitHub labels in the form `priority: HIGH`, `priority: MEDIUM`, or `priority: LOW`. GitHub-to-TODO refreshes for existing `ISSUE-*` items shall preserve the local priority and description that were already established by the first sync.

**Technical Implementation:** [TR-MCP-TODO-004](./Technical-Requirements.md#tr-mcp-todo-004) | [TR-MCP-GH-006](./Technical-Requirements.md#tr-mcp-gh-006) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `TodoUpdateService`, `IssueTodoSyncService`, `TodoController`, `FwhMcpTools`, `VoiceConversationService`

## FR-MCP-071 ISSUE Comment Round-Trip and GitHub-Driven Closure Reconciliation

The server shall round-trip `ISSUE-{number}` discussion between GitHub and MCP TODOs without mutating the established TODO description. GitHub-origin issue comments shall sync into the TODO note inside a generated GitHub-comments section, while user-authored TODO note content outside that generated section shall remain preserved across subsequent syncs.

When an `ISSUE-*` TODO is updated locally with new note text, the server shall propagate the appended TODO-authored comment back to GitHub as an issue comment. When the GitHub issue is later closed outside MCP, a GitHub-to-TODO sync shall mark the corresponding TODO as done.

**Technical Implementation:** [TR-MCP-GH-007](./Technical-Requirements.md#tr-mcp-gh-007) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `IssueTodoSyncService`, `TodoUpdateService`, `GitHubController`, `TodoController`

## FR-MCP-072 Database-Authoritative TODO Storage with YAML Projection and Audit History

The server shall treat SQLite as the authoritative current-state store for workspace TODO items. When a configured workspace TODO document already exists and the authoritative database is empty, initialization shall import the current YAML document once into SQLite and thereafter keep `docs/Project/TODO.yaml` synchronized as a deterministic projection of authoritative database state rather than as the live writable source of truth.

The server shall preserve TODO document metadata such as `notes`, `completed`, and `code-review-remediation.reference`, retain append-only audit history for TODO state mutations, and expose that audit history through HTTP, typed client, and MCP tool surfaces so callers can retrieve tracked TODO states even after deletion when history exists.

**Technical Implementation:** [TR-MCP-TODO-005](./Technical-Requirements.md#tr-mcp-todo-005) | [TR-MCP-TODO-006](./Technical-Requirements.md#tr-mcp-todo-006) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `SqliteTodoService`, `TodoYamlFileSerializer`, `TodoController`, `TodoClient`, `McpServerMcpTools`, `TodoServiceFactory`

## FR-MCP-073 Parseable Log Event Field Cap

The server shall cap each Parseable log event payload to a maximum of 250 top-level fields so Parseable ingest remains within the supported field-count envelope for a single event.

When application log events contain more structured properties than the Parseable limit allows, the server shall preserve the canonical Parseable metadata fields (`timestamp`, `level`, `message`, and `exception` when present) and omit excess user properties rather than emitting an oversized payload.

**Technical Implementation:** [TR-MCP-LOG-003](./Technical-Requirements.md#tr-mcp-log-003) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `ParseableEventFormatter`, `ParseableBatchFormatter`

## FR-MCP-074 Azure DevOps Repository Pipeline Migration

The repository SHALL define Azure DevOps YAML pipelines as the source of truth for repository CI/CD instead of the retired GitHub Actions workflow files.

The Azure DevOps pipeline SHALL preserve the current repository automation intent for branch/path-filtered validation, published server artifacts, documentation artifacts, Windows MSIX packaging, and branch-conditional client package publication, while ignoring any separate Copilot coding agent pipeline.

**Technical Implementation:** [TR-MCP-CI-001](./Technical-Requirements.md#tr-mcp-ci-001) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `azure-pipelines.yml`, `docs/AZURE-PIPELINES.md`

## FR-MCP-075 PowerShell Session Cache Discovery from `.mcpSession`

The PowerShell `McpSession` module SHALL discover and reuse the current session object cached in the workspace `.mcpSession` folder so follow-on commands can resolve the active session even when the caller does not pass an explicit session object.

This cache-discovery behavior SHALL remain backward compatible with the existing `.mcpServer/session.yaml` slug/state wrapper and SHALL prefer the current-session cache when both representations are available.

**Technical Implementation:** [TR-MCP-AGENT-013](./Technical-Requirements.md#tr-mcp-agent-013) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `tools/powershell/McpSession.psm1`

## FR-MCP-076 Marker File Trust Bootstrap and Session Authenticity Validation

The server shall render a trust-bootstrap contract into `AGENTS-README-FIRST.yaml` that instructs agents to verify the marker signature, perform a nonce-based `/health` challenge, and treat any signature or nonce mismatch as `MCP_UNTRUSTED`.

When trust validation fails, the bootstrap flow shall stop using MCP services and shall not probe additional MCP endpoints. The marker contract shall remain explicit enough for `McpSession`, `McpTodo`, and `McpContext` to follow the same verified bootstrap sequence without diverging on trust semantics.

**Technical Implementation:** [TR-MCP-SEC-003](./Technical-Requirements.md#tr-mcp-sec-003) | [TR-MCP-AGENT-014](./Technical-Requirements.md#tr-mcp-agent-014) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `src/McpServer.Services/Services/MarkerFileService.cs`, `templates/prompt-templates.yaml`, `src/McpServer.ServiceDefaults/Extensions.cs`, `tools/powershell/McpSession.psm1`, `tools/powershell/McpTodo.psm1`, `tools/powershell/McpContext.psm1`, `docs/context/module-bootstrap.md`, `docs/USER-GUIDE.md`

## FR-MCP-077 Server Federation and Request Proxying

The server shall support an opt-in federation mode that proxies incoming requests to a configured remote MCP server instance. Routing shall support a global default target and per-workspace overrides. The feature shall include anti-loop protection (via `X-Mcp-Federation-Hop` header with a configurable maximum hop count), transparent SSE/streaming forwarding for `/mcp-transport`, a runtime management REST API at `/mcpserver/federation`, and auto-discovery of federation targets from running tunnel providers.

**Status:** ✅ Complete

**Technical Implementation:** `FederationOptions`, `FederationRegistry`, `FederationProxyService`, `FederationMiddleware`, `FederationController`

**Configuration:** `Mcp:Federation:Enabled`, `Mcp:Federation:Targets`, `Mcp:Federation:DefaultTarget`, `Mcp:Federation:WorkspaceRoutes`, `Mcp:Federation:MaxHops`

## FR-MCP-078 GraphRAG Ad-Hoc Document Ingestion

The server shall accept raw text or markdown content via a REST endpoint and MCP tool, chunk it, generate embeddings, store it in the context database and vector index, and optionally trigger a GraphRAG re-index. Documents ingested this way shall use source type "adhoc-text" by default and support caller-specified title, source type, and source key metadata.

**Status:** ✅ Complete

**Covered by:** `GraphRagController` (`POST /mcpserver/graphrag/documents/ingest`), `McpServer.GraphRag` (`GraphRagService` ad-hoc ingestion path), `FwhMcpTools` (graphrag ingest tool)

## FR-MCP-079 GraphRAG Entity and Relationship CRUD

The server shall provide full CRUD operations for explicit graph entity nodes and relationship edges, persisted in workspace-scoped EF Core tables. Entities shall have name, type, description, and extensible JSON metadata. Relationships shall link two entities with a typed, weighted, described edge. Deleting an entity shall cascade to all its relationships. All operations shall be available via REST endpoints, MCP tools, and REPL commands.

**Status:** ✅ Complete

**Covered by:** `GraphRagController` (entity + relationship endpoints), `McpServer.GraphRag` (`GraphRagService` entity / relationship CRUD), `GraphEntityEntity`, `GraphRelationshipEntity`, `FwhMcpTools` (graphrag entity/relationship tools), `McpServer.Repl.Core` (graphrag command shapes)

## FR-MCP-080 GraphRAG Document Management

The server shall provide endpoints to list indexed documents with chunk counts and token totals, retrieve chunks for a specific document ordered by chunk index, and delete a document with cascade removal of its chunks and corresponding vector index entries. All operations shall be workspace-scoped and available via REST endpoints, MCP tools, and REPL commands.

**Status:** ✅ Complete

**Covered by:** `GraphRagController` (document list/get/delete endpoints), `McpServer.GraphRag` (`GraphRagService` document management), `ContextDocumentEntity`, `ContextChunkEntity`, `FwhMcpTools` (graphrag document tools)

## FR-MCP-081 Byrd Iteration Phase and TODO Execution Persistence

The server shall persist Byrd iteration phases, decomposed execution TODOs, and TODO checkpoints so agents can resume multi-step work from MCP state instead of chat history. Persisted execution TODOs shall carry goal, summary, acceptance criteria, constraints, requirement links, relevant files, next action, test plan state, validation state, and linked session turn identifiers.

**Status:** ✅ Complete

**Covered by:** `TodoExecutionController` (`/mcpserver/todo-execution/*`), `TodoExecutionService`, Byrd phase + checkpoint entities, `McpDbContext`

## FR-MCP-082 Bounded Byrd Execution Context Hydration

The server shall return a bounded active TODO execution context and a checkpoint-based delta context for the current execution TODO. Hydration shall prefer concise requirement snippets, concise recent session-turn summaries, relevant files, artifacts, test state, validation state, and execution pointers, and shall not return full plan markdown or broad session-log history when compact state is sufficient.

**Status:** ✅ Complete

**Covered by:** `TodoExecutionController` (`active`, `next-ready`, `{todoId}`, `{todoId}/delta` endpoints), `TodoExecutionService`

## FR-MCP-083 Structured Android Validation for Byrd TODOs

The server shall expose a structured `adb_step` surface for safe Android validation actions used during Byrd execution. Supported actions shall be limited to fixed safe operations such as screenshot, tap, swipe, text input, keyevent, wait, app launch, and focus inspection, and their results shall be storable as validation evidence and TODO checkpoint artifacts.

**Status:** ✅ Complete

**Covered by:** `TodoExecutionController` (`POST /mcpserver/todo-execution/adb/step`), `TodoExecutionService.AdbStepAsync`, `AdbStepAction` / `AdbStepRequest` models, `FwhMcpTools` (`adb_step` tool)

## FR-MCP-084 Requirements Wiki Workspace Export/Import

The server, REPL, and agent plugins shall support requirements export and import in wiki format. Wiki export shall write both Azure DevOps Wiki and GitHub Wiki document folders directly under docs/Project/wiki. Wiki import shall detect wiki document folders, select the authoritative platform source using manifest and file modified timestamps, and create, update, delete, or ignore requirements and mappings to match the selected source.

**Status:** ✅ Complete

**Covered by:** `RequirementsController` (wiki export + ingest endpoints), `RequirementsDocumentService` (wiki renderer + parser), `McpServer.Repl.Core` (requirements wiki workflow commands)

## FR-MCP-085 QA Workspace Question CRUD

Create / read / update / delete a Question (title, body, tags[], author).

## FR-MCP-086 QA Workspace Answer CRUD

Create / read / update / delete an Answer attached to a Question.

## FR-MCP-087 QA Accepted Answer Invariant

Accept exactly one Answer per Question (idempotent; re-accept clears prior accepted answer).

## FR-MCP-088 QA Question Tag Filtering

Tag filter on Question list with AND semantics (`?tag=foo&tag=bar`).

## FR-MCP-089 QA Per User Voting

Vote (+1 / -1) on Question and Answer with one-vote-per-user enforcement. A given actor may hold at most one active vote per entity (Question or Answer). Submitting the same vote a second time is idempotent (returns the current state, no counter change, no new audit row). Submitting the opposite vote replaces the prior vote (counter adjusts by +/- 2; an audit row is written for the change). An actor may explicitly revoke their vote (counter adjusts by -1 or +1; audit row recorded). Voter identity is the value resolved by `IQaAuthorResolver`. The Question/Answer rows still store a denormalized counter for fast reads; the authoritative per-voter state lives in a `QaVoteEntity` table with a unique constraint (TR-MCP-QA-032). Voter identity and every vote transition (apply, change, revoke) appear in the audit history (TR-MCP-QA-019), and a voter-history endpoint exposes them (TR-MCP-QA-031).

## FR-MCP-090 QA Threaded Comments

Threaded comments on Questions and Answers with depth cap (<= 3).

## FR-MCP-091 QA FAQ Projection

`GET /mcpserver/qa/faq` returns questions that have an accepted answer, projected as `{ questionId, title, tags[], voteCount, acceptedAnswer: { body, author, voteCount }, deeplink }` where `deeplink = "/mcpserver/qa/questions/{id}"`. Default sort: accepted-answer voteCount desc, then question voteCount desc.

## FR-MCP-092 QA Author Resolution

Author identity resolves via request-body `author` if provided, falling back to API-key -> workspace-identity (via `WorkspaceTokenService`), then JWT `sub` claim.

## FR-MCP-093 QA Workspace Isolation

All Q&A data is workspace-scoped via composite PK `(WorkspaceId, Id)` and EF global query filter (TR-MCP-MT-003 pattern).

## FR-MCP-094 QA REST MCP STDIO and Client Parity

Q&A is reachable from REST, MCP STDIO tools, and the typed C# client library with feature parity.

## FR-MCP-095 QA REPL and PowerShell Parity

Q&A is reachable from the REPL via a `workflow.qa.*` command namespace (interactive REPL + `--agent-stdio` YAML envelope protocol) and from a PowerShell module (`McpQa.psm1`) following the `Verb-Noun` pattern of `McpTodo.psm1`.

## FR-MCP-096 QA User and Agent Documentation

Documentation surfaces the Q&A subsystem for end users, integrators, and agents (USER-GUIDE, CLIENT-INTEGRATION, api-capabilities, AGENTS, FAQ, README, plus a new `qa-schema.md` context doc).

## FR-MCP-097 QA Plugin Skill

Each Claude Code plugin (sibling repos `mcpserver-claude-code-plugin`, `mcpserver-claude-cowork-plugin`, `mcpserver-codex-plugin`, `mcpserver-cline-plugin`, `mcpserver-cline-v2-plugin`, `mcpserver-copilot-plugin`) gets a `skills/qa/SKILL.md` skill that lets agents use Q&A as a knowledge source - ask, answer, accept, and read FAQ via the REPL workflow.

## FR-MCP-098 QA Append Only Audit

Full audit tracking: every Q&A mutation (question create/update/delete, answer create/update/delete, accept/un-accept, vote +1/-1, comment add/delete) writes an append-only audit row capturing actor, action, version, full pre-mutation snapshot, and timestamp. Audit history is queryable per entity (`GET /mcpserver/qa/questions/{id}/audit`, `/answers/{id}/audit`, `/comments/{id}/audit`) and at workspace scope (`GET /mcpserver/qa/audit`) with paging, matching the TODO audit endpoint contract.

## FR-MCP-099 QA Web Research Capture

Mandatory web-research-to-Q&A capture: whenever a plugin/agent performs an internet search (web fetch, web search, MCP web tool, browser MCP, or any external HTTP retrieval) in order to answer a workspace-scoped question, the agent MUST post a new Question to `/mcpserver/qa/questions` containing the original prompt, then post an Answer containing the synthesized response with every source URL cited inline (markdown links) and as a `sources[]` array in the answer body's JSON sidecar. If the agent verified the synthesized answer, it MUST also call accept-answer. This is enforced by the `qa/SKILL.md` in every sibling plugin repo (mandatory rule at top of body) and surfaced in `AGENTS.md` / `CLAUDE.md` so agents cannot opt out. Captured `web_reference` actions in the session log point at the resulting Q&A entry's deeplink.

## FR-MCP-100 QA Close and Duplicate Flags

Close / duplicate flags: a Question can be closed with a reason and reopened; a Question can be marked as a duplicate of another Question (canonical) with both directions navigable. Closed questions are excluded from FAQ by default and from the default Question list (opt-in via `?includeClosed=true`); duplicates redirect FAQ consumers to the canonical Question via the deeplink. Close and duplicate transitions are full audit events (TR-MCP-QA-018 family).

## FR-MCP-101 QA Markdown Rendering and Sanitization

HTML / Markdown sanitization and rendering: every Question.Body, Answer.Body, and Comment.Body is authored as Markdown, stored verbatim, and additionally rendered to sanitized HTML at write time. Both representations are returned in the DTOs (`body` raw Markdown, `bodyHtml` sanitized HTML). The sanitizer strips script, iframe, on-* attributes, javascript: URLs, and other XSS vectors per a whitelist (allow paragraph, headings <=3, lists, code/pre, bold/italic, links with `rel="nofollow noopener"`, inline code, blockquote, tables). Sanitization runs on every write and on every audit-snapshot read so historical bodies are also safe to render.

## FR-MCP-102 QA Generated FAQ Wiki Page

FAQ wiki page: the documentation wiki output (Azure DevOps wiki under `docs/Project/wiki/azure/` and GitHub wiki under `docs/Project/wiki/github/`) includes a generated `FAQ.md` page rendered from the live `/mcpserver/qa/faq` endpoint of a configured workspace at build time. The page lists every accepted Q&A with the question title (linked to its detail endpoint), tags, vote counts, the rendered HTML body of the accepted answer, and the `sources[]` array as a sources section. The wiki Home / Sidebar / .order files reference the new FAQ page. The page is regenerated by the Nuke build during the wiki publication target.

## FR-MCP-103 Hub-and-Spoke Federation

The server shall support hub-and-spoke federation where a Hub instance is authoritative for enrolled LocalProxy servers, global workspace inventory, operation intake, sync fanout, queue status, and conflicts. Existing point-to-point federation remains supported as DirectProxy; Standalone continues to serve only local workspaces. A LocalProxy shall forward MCP requests to the configured hub, queue mutating requests durably during hub outages, replay queued operations when connectivity returns, and expose role, hub URL, proxy id, queue depth, stale/queued status, and conflict status to agents and operators.
**Acceptance Criteria:**
- [ ] Adapter diagnostics enumerate every required mutable state domain and distinguish covered, local-only, and apply-supported domains.
- [ ] LocalProxy live forwarding continues to route /mcpserver/* requests, including /mcpserver/memory, through existing proxy middleware when the hub is reachable.
- [ ] Mutating LocalProxy requests that are eligible for replay are queued durably during hub outages with operation id, domain, resource id, workspace id, headers, body, and base version.
- [ ] Queued operations replay through signed envelopes when signing is configured.
- [ ] Stale base-version operations create federation conflicts and do not overwrite authoritative hub state.

## FR-MCP-104 Decision-complete agent plans

All generated plans must be detailed enough for a less expensive implementation model to execute successfully without resolving unstated product, architecture, interface, testing, or rollout decisions.

## FR-MCP-105 Database integrity and durable audit enforcement

The MCP Server must preserve database integrity across all supported providers by enforcing workspace, TODO, requirement, federation, soft-delete, and audit relationships as durable relational contracts.

## FR-MCP-106 Reliable plugin requirement updates

Agent plugins must reliably update existing FR, TR, and TEST requirement records through workflow.requirements.update* commands, including priority changes, without failing inside the wrapper path.

## FR-MCP-107 Outstanding session work consolidation

MCP Server must support an audited consolidation plan that inventories outstanding work from a long session, separates validated changes from unresolved dirty work, and sequences completion through Byrd gates before deploy or publish.

## FR-MCP-108 TODO Markdown description preservation

TODO create, update, read, audit, and projection paths shall preserve description Markdown formatting and whitespace instead of normalizing or stripping meaningful content.

## FR-MCP-109 Batch requirements mutation

The MCP Server shall allow agents and API clients to create or update multiple functional, technical, and test requirements in one atomic request by submitting a records array.

## FR-MCP-110 Deterministic Nuke PowerShell execution

Nuke build and deployment automation SHALL run PowerShell child hosts with non-interactive, no-profile flags so user profiles, prompts, aliases, and interactive host behavior cannot alter scripted execution.

## FR-MCP-111 Package workflow closeout skills across McpServer plugins

All McpServer plugin distributions expose sync-logs, commit-sync, and wrap-up as packaged skills so agents can synchronize logs, commit and push interrupted work, and close out MCP-backed work consistently across plugin families.

## FR-MCP-112 Requirements export description and AC formatting

Requirements exports shall render requirement descriptions as readable Markdown text and shall display structured acceptance criteria as a bulleted list instead of hiding or flattening AC into dense table cells.
**Acceptance Criteria:**
- [x] Wiki exports render TEST requirement descriptions outside dense table cells so long descriptions remain readable. (evidence: RequirementsDocumentServiceTests.GenerateWikiAsync_RendersTestingRequirementsAsGroupedSectionsWithAcceptanceCriteria asserts per-TEST headings and paragraphs.)
- [x] Wiki exports render structured acceptance criteria under an Acceptance Criteria label as Markdown checklist bullets. (evidence: The same focused test asserts unchecked and checked AC bullets, including evidence text.)
- [x] Azure and GitHub wiki exports use the same requirement description and AC formatting. (evidence: The focused service test asserts GitHub and Azure Testing-Requirements.md output are equal.)

## FR-MCP-113 Plugin requirement batch payload parsing

The system SHALL accept valid YAML and JSON records arrays for requirement batch operations while preserving nested acceptance criteria.
**Acceptance Criteria:**
- [ ] workflow.requirements.updateFrBatch accepts unindented records YAML and preserves nested acceptance criteria.

## FR-MCP-114 Parent TODO completion isolation

The system SHALL keep a TODO item's top-level done state independent from nested implementation task done state. Updating implementationTasks[].done SHALL NOT mark the parent TODO done unless the update request explicitly sets the parent done field or the TODO completion workflow deliberately satisfies the parent completion criteria.
**Acceptance Criteria:**
- [x] Updating a nested implementation task done value does not include or change the top-level TODO done field when the caller did not explicitly set it. (evidence: Focused Bats test TODO HTTP body does not promote implementation task done to parent done passed in all five Bash plugin repos.)
- [x] A live TODO update can mark one implementation task done while the parent TODO remains not done. (evidence: Temporary live TODO TEMP-ISSUE39-001 remained done=false after updating one implementation task to done=true, then was deleted.)

## FR-MCP-115 Session and compaction hook output schema compliance

MCP plugin session and compaction hooks SHALL emit schema-valid hook outputs for their event type. Status-only SessionStart, SessionEnd, PreCompact, and PostCompact outcomes SHALL emit an empty JSON object; hooks that emit hookSpecificOutput SHALL include the matching hookEventName and only fields supported by that event. PostCompact SHALL NOT emit additionalContext because that event cannot inject context.
**Acceptance Criteria:**
- [x] SessionStart, SessionEnd, PreCompact, and PostCompact status-only hook scripts emit {} instead of non-spec hookSpecificOutput status payloads. (evidence: Hook Bats suites assert exact {} output for the affected session and compact hooks.)
- [x] PostCompact does not emit additionalContext. (evidence: Hook Bats suites assert post-compact output is {} and does not contain additionalContext.)

## FR-MCP-116 GitHub CLI service-account ownership resilience

The system SHALL allow GitHub-backed TODO and issue operations to run under service accounts even when Git rejects the workspace repository as dubious ownership, without requiring an untracked global Git configuration mutation.

## FR-MCP-117 Codex plugin wrapper bounded execution

The Codex MCP plugin wrapper SHALL return successful workflow responses promptly or fail within a caller-controlled bounded timeout with actionable diagnostics instead of forcing raw REST fallback.

## FR-MCP-118 Keyserver trust service

McpServer SHALL provide keyserver trust services that register trusted parties, manage public key metadata, sign transaction manifests, verify signed manifests, reject invalid or replayed manifests, preserve historical signing descriptors for verification, and persist audit evidence without exposing private key material.
**Acceptance Criteria:**
- [x] Publisher, subscriber, arbiter, and future model parties can be provisioned with party ID, role, active signing key ID, active encryption key ID, status, created/updated UTC, and audit metadata.
- [x] Manifest signing binds transaction ID, turn ID, publisher party, subscriber party, key IDs, sequence, nonce, issued UTC, expiry UTC, algorithms, diffgram SHA-256, and encrypted body SHA-256.
- [x] Manifest verification succeeds for an unchanged canonical payload and valid keyserver signature and rejects unknown parties, disabled parties, unknown keys, disabled keys, expired manifests, replayed nonces, stale sequences, malformed signatures, and hash mismatches with deterministic reason codes.
- [x] Keyserver signing never returns, serializes, or logs private key material; external signing private PEM can be provisioned from file-backed startup configuration.
- [x] Key rotation supports a new active signing key while retaining old public descriptors for historic verification; full lifecycle automation beyond file-backed startup provisioning is deferred and explicit future scope.

## FR-MCP-119 Subscriber diffgram commit service

McpServer SHALL provide subscriber services that verify keyserver-signed manifests, decrypt intended diffgrams, validate encrypted and plaintext hashes, durably commit accepted diffgrams, expose abort/status flows, and reject invalid or conflicting commits.
**Acceptance Criteria:**
- [x] Subscriber accepts a commit only when manifest verification succeeds against keyserver trust and the protected payload is addressed to the subscriber party/key ID.
- [x] Subscriber validates encrypted body SHA-256 before decrypt and plaintext diffgram SHA-256 after decrypt.
- [x] First valid commit persists transaction ID, diffgram ID, manifest hash, sequence, status, committed UTC, reason details, and audit metadata durably.
- [x] Duplicate commit with identical transaction/diffgram/hash is idempotent and returns committed; duplicate commit with mismatched payload, manifest, hash, or sequence is rejected as conflict.
- [x] Abort records aborted status, refuses later commit, and status exposes pending, committed, rejected, aborted, and reason details without exposing secrets.
- [x] Subscriber encryption private key rings decrypt old and rotated protected envelopes and bind separate-host configuration to the configured subscriber key material.

## FR-MCP-120 MCP Server transaction gating

McpServer SHALL gate first-party mutating user-turn paths behind transaction manifest signing and subscriber commit confirmation before returning committed success, or fail closed before uncompensated side effects when a safe compensation boundary is not yet available.
**Acceptance Criteria:**
- [x] Federation adapter apply, memory add/update/delete, TODO create/update/delete/move, repository write, prompt-template mutations, TODO execution state/plan mutations, requirements repository/export writes, session-log writes, tool registry mutations, voice/external interactive mutations, agent-pool lifecycle mutations, GraphRAG mutations, GitHub external side-effect mutations, context rebuild/website/sync mutations, and federation control-plane mutations either route through compensation-capable coordinator gates or fail closed while required transactions are active.
- [x] Memory add rollback restores or preserves the created memory record, clears soft-delete metadata when exact EF restoration is available, and same-ID retry conflicts after rollback.
- [x] Server-side TODO compensation captures update/delete/move restore points under the provider write lock, deletes uncommitted local create rows, restores source snapshots on move rollback, and rejects ISSUE-backed TODO mutation while GitHub side-effect compensation remains future scope.
- [x] Session-log rollback restores or preserves add-style created session records and restores captured full session graphs for replace/delete inside an explicit database transaction.
- [x] Generic REPL client passthrough blocks unsafe protected namespaces and unclassified client namespaces while required transaction gating is active, while explicitly service-gated namespaces remain routed to server-side gates.
- [x] Complete provider-level isolation across delayed subscriber rejection and bucket/GitHub whole-orchestration compensation remain documented future enhancements, not committed-success claims for this slice.

## FR-MCP-121 Degraded mode and rollback

McpServer SHALL enter explicit degraded mode when transaction dependencies are unavailable or commit cannot complete, allow only safe reads or explicitly fail-closed operations, and preserve audit records through rollback.
**Acceptance Criteria:**
- [x] Coordinator status exposes enabled/degraded state, last reason, message, and transaction dependency health.
- [x] Required transaction gates reject mutation before side effects when the coordinator is degraded or required transaction dependencies cannot sign or commit.
- [x] Commit failure invokes available rollback compensation and reports rollback success or rollback failure without deleting audit evidence.
- [x] Durable pending commit handoffs are canceled after successful rollback compensation or commit timeout settlement.
- [x] Runtime/control-plane endpoints without complete remote compensation fail closed or remain explicitly deferred until future compensation slices.

## FR-MCP-122 Byrd v4 execution control

The transactional diffgram implementation SHALL follow Byrd Development Process v4 with requirements-first, failing-test-first, mocks-first, refactor, explicit validation, and zero-failure zero-skip validation gates for each executed scope.
**Acceptance Criteria:**
- [x] The plan records FR/TR/TEST identifiers and mappings before implementation closeout.
- [x] Each implementation slice names focused tests and validates zero failures and zero skips for the executed scope.
- [x] Deferred work is tracked as TODO/requirements state instead of skipped test placeholders.
- [x] Final closeout validation includes focused transaction tests, full affected unit suites, traceability validation, and solution build evidence.

## FR-MCP-123 Quad-model future scaffolding

The transaction foundation SHALL preserve the imported quad-model architecture while keeping Curiosity execution, AoT reconciliation execution, quad-model orchestration, and weight-update execution disabled by default until later requirements authorize them.
**Acceptance Criteria:**
- [x] Imported diagrams retain stable IDs and future branch annotations for Curiosity, AoT reconciliation, weight redistribution, and quad orchestration.
- [x] Current production code implements transaction security and mutation gating only; direct model execution and weight-update execution remain disabled/deferred.
- [x] Deferred future scope is explicitly documented in the plan, mutation audit, design rounds, TEST-MCP-163, and TEST-MCP-170.

## FR-MCP-124 Imported document and diagram preservation

The imported Quad-Model transactional diffgram document SHALL be preserved as local project-contract artifacts, including its diagrams, branch IDs, source references, and implementation-scope annotations.
**Acceptance Criteria:**
- [x] All six imported Mermaid diagram IDs are present in `docs/Project/Quad-Model-Transactional-Diffgram-Plan.md`.
- [x] Each imported diagram section includes a Mermaid block, imported source-section reference, and repository annotations.
- [x] Diagram-derived implemented and deferred branches map to TEST-MCP-165 through TEST-MCP-173.

## FR-MCP-125 Federation and compatibility preservation

The transactional diffgram implementation SHALL remain additive to existing federation behavior and SHALL preserve existing HMAC federation envelope compatibility while adding transaction-specific trust boundaries.
**Acceptance Criteria:**
- [x] Federation HMAC envelopes remain separate from transaction manifest signing and protected diffgram encryption.
- [x] Global federation mutation-adapter apply paths are transaction-gated through the coordinator.
- [x] Federation control-plane mutations fail closed while required transaction gating is active until full compensation is designed.
- [x] Existing proxy/federation compatibility expectations remain documented separately from transaction crypto.

## FR-MCP-126 aiUnit plan review

The transaction plan SHALL include a test-only aiUnit plan review gate that fails on critical or high findings and records run-log evidence before closeout.
**Acceptance Criteria:**
- [x] `tests/McpServer.PlanReview.Tests` validates aiUnit plan-review run-log evidence for PLAN-TURNTRANSACTIONS-001.
- [x] The gate fails when run-log findings contain critical or high severity findings.
- [x] The run-log artifact records prompt scope, provider/model metadata, findings status, and reviewed scope.
- [x] Explicit future-disabled work is treated as non-blocking only when it does not hide a safety, correctness, or validation gap.

## FR-MCP-127 Diagram-derived implementation tests

In-scope branches from imported activity and sequence diagrams SHALL have implementation-test coverage or an explicit deferred requirement before related code is written.
**Acceptance Criteria:**
- [x] SD-DIFFGRAM-001 valid, invalid, signing, verification, encryption, and hash branches are covered by keyserver/subscriber/client/integration tests.
- [x] AD-TXN-001 commit, subscriber unavailable, degraded, rollback, timeout, and audit branches are covered by coordinator, pub-sub, federation, and mutation-gating tests.
- [x] Deferred diagram branches for Curiosity, AoT, weight redistribution, and full quad execution are explicitly documented as disabled future scope.
- [x] TEST-MCP-165 through TEST-MCP-173 preserve diagram IDs, branch IDs, design mappings, and traceability closeout evidence.

## FR-MCP-128 Two-round architecture and design review

The implementation SHALL include two architecture/design rounds before closeout: a first architecture round defining boundaries, ownership, trust, storage, threat model, and gap analysis, and a second implementable design round defining DTOs, entities, options, interfaces, endpoint contracts, reason codes, audit payloads, XMLDoc obligations, and test mappings.
**Acceptance Criteria:**
- [x] `TurnTransactions-Architecture-Round1.md` captures component boundaries, trust model, storage boundaries, rollback/audit rules, threat model, and gap analysis.
- [x] `TurnTransactions-Design-Round2.md` captures implementable DTOs, entities, options, interfaces, endpoint contracts, reason codes, canonicalization, audit payloads, XMLDoc obligations, and TEST-MCP-158 through TEST-MCP-173 mappings.
- [x] `TurnTransactions-Mutation-Endpoint-Audit.md` captures current gated, fail-closed, and explicitly deferred mutation surfaces.
- [x] Requirements matrix and FR/TR mapping rows link FR-MCP-118 through FR-MCP-128 to transaction TR and TEST records.

## FR-MCP-129 Durable external brain-slot registry and live invocation

The MCP runtime SHALL provide durable workspace-scoped external brain-slot definitions for LeftHemisphere, RightHemisphere, CuriosityEngine, and ArbiterOfTruth, including CRUD, readiness status projection, and individually gated live model invocation.
**Acceptance Criteria:**
- [x] Brain-slot definitions are durable CRUD records scoped by workspace and role.
- [x] Exactly one enabled slot per workspace and role is allowed; enabling a replacement requires replaceExisting=true and writes an audit entry.
- [x] A workspace is quad-ready only when all four roles have enabled slots with valid provider, model, endpoint policy, credential reference, and trusted party mapping.
- [x] CRUD stores and returns credentialReference only; raw API keys are never persisted or returned.
- [x] REST, client, and STDIO/MCP surfaces expose list/get/upsert/delete/enable/disable/status/invoke parity.
- [x] Invocation is rejected unless brain-slot execution is enabled, the slot is enabled, endpoint policy passes, credentials resolve, and required turn transactions are enabled.
- [x] No implicit fallback model is used; fallback behavior remains fail-closed with structured reason codes unless a configured slot is invoked and committed.

## FR-MCP-130 Transaction-gated Curiosity external result admission

The CuriosityEngine slot MAY request GraphRAG or context admission for external model output only after the related brain-slot invocation transaction commits successfully through the subscriber path.
**Acceptance Criteria:**
- [x] No model output is returned to the caller until the subscriber commit succeeds.
- [x] If commit fails, times out, or degrades, output is discarded from the response and is not injected into cache or GraphRAG.
- [x] Only CuriosityEngine may request GraphRAG/context admission.
- [x] Left, Right, and Arbiter invocations may return committed results but never mutate cache or GraphRAG.
- [x] Curiosity admission records the committed transaction and admission metadata for audit.

## FR-MCP-131 Quad containment and authorization boundary

The MCP runtime SHALL keep Quad-Brain branches fail-closed unless explicit branch requirements authorize them. FR-MCP-134 and FR-MCP-135 now authorize AoT reconciliation execution, durable weight updates, and full Quad-Brain orchestration; unauthorized cache mutation and implicit fallback remain contained.
**Acceptance Criteria:**
- [x] AoT reconciliation execution is authorized only through the transaction-gated ArbiterOfTruth slot path.
- [x] Weight update execution is authorized only through the safety-gated, transaction-coordinated weight update path.
- [x] Full automatic quad orchestration is authorized only when the workspace is quad-ready and all role outputs commit successfully.
- [x] Individual brain-slot invocation authorization still does not authorize non-Curiosity GraphRAG/cache mutation or implicit fallback model behavior.

## FR-MCP-134 Full Quad-Brain orchestration and AoT reconciliation

The MCP runtime SHALL execute the full four-role Quad-Brain decision loop when the workspace is quad-ready, including LeftHemisphere, RightHemisphere, CuriosityEngine, and ArbiterOfTruth invocation, transaction-gated AoT reconciliation, committed final output return, and fail-closed rejection when any required slot, transaction, endpoint, credential, or party gate is unavailable.
**Acceptance Criteria:**
- [x] Orchestration rejects before provider calls unless all four roles have exactly one enabled, valid, trusted, transaction-ready slot.
- [x] LeftHemisphere, RightHemisphere, and CuriosityEngine outputs are collected through existing transaction-gated slot invocation before ArbiterOfTruth reconciliation runs.
- [x] AoT reconciliation returns the final committed decision only after subscriber commit; failed or degraded commits discard model output from the caller response.
- [x] No implicit fallback model is used; fallback remains explicit fail-closed unless a configured slot is invoked and committed.

## FR-MCP-135 Quad-Brain weight update controls

The MCP runtime SHALL support durable, audited Quad-Brain role weight updates with AoT approval, admin approval, explicit safety-gate acknowledgement, version checks, before/after snapshots, and rollback metadata, while rejecting unsafe or stale updates without mutating slot state.
**Acceptance Criteria:**
- [x] Weight updates require AoT approval, admin approval, safety gates passed, and a non-empty reason before slot weights change.
- [x] Each accepted update increments slot weight versions and records previous and new values in DataAuditLogs.
- [x] Stale expected versions, missing roles, disabled slots, invalid weights, or missing approvals return structured fail-closed reason codes.
- [x] Full orchestration can apply an explicit approved weight update but never performs an implicit self-improvement update.

## FR-MCP-136 ACID-coupled Microsoft Agent Framework agent definition

The system SHALL expose an explicit ACID-compliant, tightly coupled hosted-agent definition for Microsoft Agent Framework hosts, extending the existing ChatClientAgent integration while requiring authenticated workspace binding, serialized tool invocation, durable audit/session-log boundaries, and fail-closed tool exposure.
**Acceptance Criteria:**
- [x] The ACID profile has stable public metadata including id, name, source type, consistency model, coupling mode, and Microsoft Agent Framework extension type.
- [x] The ACID profile requires authentication, workspace scoping, session-turn boundaries, durable audit, transaction-required mutation policy, and serialized model tool invocation.
- [x] Generic passthrough, shell, desktop, repository-write, TODO-mutation, and GraphRAG-mutation tools are not exposed by default in the ACID profile.
- [x] Default hosted-agent registration remains backward compatible when the ACID profile is not selected.

## FR-MCP-AGENT-PARITY-001 FR-MCP-AGENT-PARITY-001

Legacy agent-parity functional TODO link retained for historical traceability. Status: superseded by concrete plugin/core parity requirements and matrix rows; no active implementation work is tracked under this stub.

## FR-MCP-AGENT-PARITY-002 FR-MCP-AGENT-PARITY-002

Legacy agent-parity functional TODO link retained for historical traceability. Status: superseded by concrete plugin/core parity requirements and matrix rows; no active implementation work is tracked under this stub.

## FR-MCP-BATCH-001 Plugin requirement batch payload parsing

All MCP server plugins SHALL accept valid YAML and JSON records arrays for requirement batch operations without schema-validation rejection.

## FR-MCP-LIVE-CODEX-20260603T2014Z Live Codex plugin acceptanceCriteria verification

Temporary live verification for plugin acceptanceCriteria rollout.

## FR-MCP-LIVE-CODEX-20260603T2015Z Live Codex plugin acceptanceCriteria verification

Temporary live verification for plugin acceptanceCriteria rollout.

## FR-MCP-MEMORY-001 Global and workspace memory storage

The MCP Server SHALL store raw-text memories with a stable memory ID, explicit scope, and exact text round-trip semantics. Supported scopes are Global and Workspace. Global memories are visible to every workspace, while Workspace memories are visible only to their owning workspace.
**Acceptance Criteria:**
- [x] A memory record has a stable ID, scope, category, raw text, version, author metadata, and created/updated timestamps.
- [x] Global memories are returned for every workspace and are not stamped with a workspace owner.
- [x] Workspace memories require an owning workspace and are not returned for any other workspace.
- [x] Removed memories are hidden from normal reads and lists without physically deleting the row.
- [x] Memory text round-trips exactly, including internal newlines and punctuation.

## FR-MCP-MEMORY-002 Memory CRUD tools

The MCP Server SHALL expose add, list, update, and remove operations for raw-text memories through REST, typed client, MCP stdio, and REPL workflow surfaces.
**Acceptance Criteria:**
- [x] Add creates a memory and returns ID, text, scope, category, version, and timestamps.
- [x] List returns the effective memory set for the active workspace by default.
- [x] Update replaces raw text, increments version, and can change scope under validated rules.
- [x] Remove soft-deletes a memory by ID.
- [x] Invalid IDs, duplicate active IDs, invalid scopes, empty text, and unauthorized scope changes return clear errors.

## FR-MCP-MEMORY-003 Required memories injection format

Supported agents SHALL receive active memories at each host-supported request boundary in a deterministic `REQUIRED MEMORIES` block. Memory text SHALL be raw text and SHALL NOT be summarized, decorated, or rewritten.
**Acceptance Criteria:**
- [x] The injected block header is exactly `REQUIRED MEMORIES`.
- [x] Each memory renders as `- MEMORY-CATEGORY-001: Raw memory text`.
- [x] An empty memory set renders exactly `REQUIRED MEMORIES` followed by `- None`.
- [x] Scope labels, timestamps, source metadata, confidence, and summaries do not appear in the injected block.
- [x] Hosts without request-boundary injection document the limitation and expose the best available explicit memory-list fallback without claiming automatic injection.

## FR-MCP-MEMORY-004 Marker template memory guidance

Generated `AGENTS-README-FIRST.yaml` instructions SHALL explain MCP memories, the memory tools, the ID format, scope behavior, and the exact `REQUIRED MEMORIES` rendering contract.
**Acceptance Criteria:**
- [x] templates/prompt-templates.yaml contains an MCP Memories section in the default marker prompt. (evidence: templates/prompt-templates.yaml MCP Memories section.)
- [x] The section names memory_add, memory_list, memory_update, and memory_remove. (evidence: templates/prompt-templates.yaml MCP Memories tool list.)
- [x] The section documents Global versus Workspace scope and the MEMORY-{CATEGORY}-{NNN} ID format. (evidence: templates/prompt-templates.yaml scope and ID guidance.)
- [x] The section states that memory text is raw text, not a secret store, and must not be silently generated. (evidence: templates/prompt-templates.yaml memory rules.)
- [x] The section documents agent-local memory import safeguards, updatedBy attribution, and session-log action recording for memory mutations. (evidence: docs/context/memory.md and templates/prompt-templates.yaml import and attribution rules.)
- [x] A marker-template contract test fails if the required section, tools, render format, import safeguards, or attribution rules are missing. (evidence: MemoryContractArtifactTests.MemoryContextDocumentation_IncludesImportAndAttributionRules.)

## FR-MCP-MEMORY-005 REPL memory namespace

The REPL SHALL expose typed `workflow.memory.*` operations for memory add, list, update, and remove.
**Acceptance Criteria:**
- [x] `workflow.memory.add`, `workflow.memory.list`, `workflow.memory.update`, and `workflow.memory.remove` dispatch through typed workflow code.
- [x] Valid REPL memory commands return standard result envelopes.
- [x] Invalid memory method names return the normal method-not-found envelope.
- [x] Invalid request payloads return standard structured error envelopes without crashing the REPL process.

## FR-MCP-MEMORY-006 Memory scope management and ordering

The MCP Server SHALL allow memories to be marked Global or Workspace and SHALL return Global memories before Workspace memories, with each group sorted by ID ascending.
**Acceptance Criteria:**
- [x] Add supports explicit `Scope` and defaults to Workspace only when omitted.
- [x] Update can change a memory between Global and Workspace under validated rules.
- [x] List APIs, REPL methods, stdio tools, and required-memory injection all use Global-first grouping.
- [x] Each scope group sorts by ID ascending using ordinal string comparison.
- [x] Global rows never appear after workspace rows, and workspace rows from other workspaces are excluded.

## FR-MCP-MEMORY-007 Agent plugin memory integration

Official McpServer agent plugins SHALL expose memory tools and SHALL inject the `REQUIRED MEMORIES` block at host-supported request boundaries, or SHALL document a host limitation and expose an explicit memory-list fallback.
**Acceptance Criteria:**
- [x] Codex, Claude Code, Claude Cowork, Copilot, Grok, Cline v1, Cline v2, and OpenCode plugin lanes validate memory tool availability. (evidence: Shell plugin Bats memory tests and TypeScript memory tests.)
- [x] Plugins with request-boundary injection hooks render REQUIRED MEMORIES on every supported user prompt boundary. (evidence: Plugin validation and marker memory injection coverage.)
- [x] Plugins without a usable request-boundary injection hook document the host limitation without claiming automatic injection. (evidence: Plugin docs and validation expectations.)
- [x] Plugin validation identifies the host hook or fallback, workspace marker path, and example REQUIRED MEMORIES output. (evidence: Plugin validation coverage.)
- [x] Plugin memory mutations append session-log actions and clear local failsafe entries after server acknowledgement. (evidence: Shell Bats and TypeScript memory tests for mutation action append and failsafe cleanup.)

## FR-MCP-MEMORY-008 Federated Memory State

The MCP Server SHALL federate Memory as a first-class mutable federation domain. Memory federation uses the existing /mcpserver/memory REST surface and federation operation envelope pipeline, not new public memory endpoints. Adapter diagnostics, LocalProxy outage queueing, signed replay, hub conflict detection, and fanout apply semantics SHALL include the memory domain.
**Acceptance Criteria:**
- [ ] Federation adapter diagnostics list memory as covered, non-local-only, and apply-supported.
- [ ] /mcpserver/memory maps to federation domain memory.
- [ ] LocalProxy live forwarding of memory REST requests continues through existing proxy middleware.
- [ ] Offline queued memory creates require an explicit valid MEMORY-* ID.
- [ ] Offline memory creates without an explicit valid ID are not queued.
- [ ] Offline queued memory update and delete operations replay through signed envelopes.
- [ ] Stale memory base-version operations create federation conflicts and do not overwrite hub state.

## FR-MCP-PLUGIN-BATCH-001 Plugin requirement batch payload parsing

All MCP server plugins SHALL accept valid YAML and JSON records arrays for requirement batch operations without schema-validation rejection.

## FR-MCP-PLUGINCORE-001 Canonical plugin core with sync and checksum guard

A single canonical plugin core SHALL be distributed into plugin repos by a sync tool that writes a per-file sha256 manifest, and a checksum guard SHALL fail CI when a synced file is edited locally.

## FR-MCP-PLUGINCORE-002 No-duplication CI guard for plugin lib files

Plugin CI SHALL fail when a lib file is neither covered by the core manifest nor declared as plugin residual.

## FR-MCP-PLUGINCORE-003 Persistent REPL daemon

A persistent REPL daemon SHALL serve many requests from one long-lived repl child over NDJSON framing, with auto-start, crash-restart, and spawn-per-call fallback.

## FR-MCP-PLUGIN-SKILLS-001 Package workflow closeout skills across McpServer plugins

All McpServer plugin distributions expose sync-logs, commit-sync, and wrap-up as packaged skills so agents can synchronize logs, commit/push interrupted work, and close out MCP-backed work consistently across plugin families.

## FR-MCP-REPL-001 YAML Protocol STDIO REPL Host

The server shall provide a YAML-envelope STDIO REPL host that accepts structured commands over standard input, executes operations against workspace services, and returns structured YAML responses over standard output. The REPL host shall support the same trust bootstrap, authentication, and workspace resolution semantics as the HTTP and MCP STDIO transports.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-REPL-001](./Technical-Requirements.md#tr-mcp-repl-001) | [TR-MCP-REPL-002](./Technical-Requirements.md#tr-mcp-repl-002) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `McpServer.Repl.Core` (`IReplProtocol`, `IYamlEnvelope`, `IYamlSerializer`, `IMarkerFileReader`, `ITrustBootstrapService`, `IAuthRotationHandler`, `IWorkspaceSelector`), `McpServer.Repl.Host` (`Program.cs`, `AgentStdioHandler`, `InteractiveHandler`, `ServiceCollectionExtensions`)

## FR-MCP-REPL-002 REPL Lifecycle Management

The REPL host shall support graceful startup, interactive command loop, structured error handling with typed error codes, and clean shutdown on EOF or explicit exit commands. The host shall maintain session context across commands within a single process invocation and emit lifecycle events for observability.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-REPL-003](./Technical-Requirements.md#tr-mcp-repl-003) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `McpServer.Repl.Host` (`Program.cs`, `AgentStdioHandler`, `InteractiveHandler`), `McpServer.Repl.Core` (`SessionLogErrorEnvelope`)

## FR-MCP-REPL-003 Command Namespace Parity

The REPL command surface shall provide namespace-organized commands with functional parity to HTTP REST endpoints and MCP STDIO tools for TODO operations, session log operations, context operations, requirements management, workspace management, and agent pool operations. Command routing shall reuse existing service contracts without duplicating business logic.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-REPL-004](./Technical-Requirements.md#tr-mcp-repl-004) | [TR-MCP-REPL-005](./Technical-Requirements.md#tr-mcp-repl-005) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `McpServer.Repl.Core` (`ITodoWorkflow`, `TodoCommandShapes`, `ISessionLogWorkflow`, `SessionLogCommandShapes`, `SessionLogModels`, `IRequirementsWorkflow`, `RequirementsCommandShapes`, `RequirementsCommandModels`, `IGenericClientPassthrough`, `ClientCommandShapes`), `McpServer.Repl.Host` (`TodoWorkflow`, `RequirementsWorkflow`, `SessionLogWorkflow`, `GenericClientPassthrough`)

## FR-MCP-REPL-004 Trust Bootstrap and Auth Rotation

The REPL host shall implement marker-file trust bootstrap with signature verification and health nonce challenge before accepting operational commands. API key authentication shall use the same per-workspace token semantics as HTTP endpoints. The host shall detect API key rotation between commands and emit warnings when tokens become stale.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-REPL-006](./Technical-Requirements.md#tr-mcp-repl-006) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `McpServer.Repl.Core` (`ITrustBootstrapService`, `IMarkerFileReader`, `IAuthRotationHandler`), `McpServer.Repl.Host` (`AgentStdioHandler`)

## FR-MCP-REPL-005 Orchestration State Visibility

The REPL host shall expose commands for querying agent pool state, active voice sessions, queued one-shot requests, and workspace notification subscriptions. State queries shall return current snapshots without blocking on long-running operations.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-REPL-007](./Technical-Requirements.md#tr-mcp-repl-007) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `McpServer.Repl.Core` (`IGenericClientPassthrough`, `ClientCommandShapes`), `McpServer.Repl.Host` (`GenericClientPassthrough`)

---

## REPL v1.0 Requirements Freeze

**Freeze Tag:** `REPL-v1.0-FREEZE` | **Date:** 2025-01-04

All REPL functional requirements (FR-MCP-REPL-001 through FR-MCP-REPL-005) are complete and frozen for v1.0 delivery. Full source code traceability comments have been added to all `McpServer.Repl.Core` and `McpServer.Repl.Host` files. All iteration 1-6 unit tests and integration tests pass. No defects remain.


---

## FR-MCP-REPL-006 Deprecate workflow.* REPL namespaces in favor of client.* passthrough

The REPL SHALL mark every workflow.* response deprecated and route workflow.sessionlog lifecycle verbs with explicit identifiers statelessly through the SessionLog client.

## FR-MCP-REPL-007 REPL Credential Discovery Diagnostics

`mcpserver-repl --agent-stdio` shall expose `--workspace-path` and `--marker-file` CLI overrides for credential resolution. When marker discovery fails, the diagnostic message shall enumerate every directory searched and distinguish "marker not found" from "marker signature mismatch". The diagnostic is forwarded into the `McpServerClient` and appended to the "Authentication required" exception so callers see the root cause rather than the generic message.

**Covered by:** `MarkerFileClientOptionsResolver.TryResolveWithDiagnostics`, `Program.cs` (`--workspace-path` / `--marker-file`), `McpClientBase.EnsureAuthenticated`

## FR-MCP-REQAC-001 Structured acceptance criteria on requirements

FR/TR/TEST requirements support structured acceptance criteria using the same {id,text,isSatisfied,evidence} shape as TODO acceptance criteria, settable on create/update and returned on get.

## FR-MCP-REQAC-002 Copy acceptance criteria from a TODO to a requirement

A copy operation populates a requirement's acceptance criteria from a TODO's acceptance criteria verbatim (field-for-field).

## FR-MCP-REQAC-PLUGIN-001 Plugin acceptanceCriteria rollout

All Bash and TypeScript MCP plugin families expose structured acceptanceCriteria on FR/TR/TEST create and update flows, including copy-from-TODO support where applicable.
**Acceptance Criteria:**
- [x] Bash and TypeScript plugin live gates preserve structured acceptanceCriteria through workflow and REST round-trip verification. (evidence: Live FR-CODEXBIND-001 workflow create/get plus REST GET returned AC-CODEXBIND-001 after service and REPL tool updates.)

## FR-MCP-REQACPLUGIN-001 Plugins expose AcceptanceCriteria on requirements surface

Every MCP server plugin (bash and TypeScript families) lets its agent set and read structured acceptanceCriteria ({id,text,isSatisfied,evidence}) on FR/TR/TEST create/update via the plugin command surface, including hydration on partial updates.
**Acceptance Criteria:**
- [ ] Bash family typed-params builder emits acceptanceCriteria block on create+update for FR/TR/TEST
- [ ] TypeScript family tool inputSchema declares acceptanceCriteria + typedParams forwards it
- [ ] Plugin tests cover both create and partial-update hydration paths

## FR-MCP-REQACPLUGIN-002 Plugins fail on proven AcceptanceCriteria loss

Every MCP server plugin that accepts caller-supplied requirement acceptanceCriteria must either preserve the supplied criteria or fail loudly when the mutation response proves the criteria were dropped.
**Acceptance Criteria:**
- [x] Criteria-only requirement updates preserve the caller-supplied acceptanceCriteria block instead of replacing it with an empty or stale hydrated list. (evidence: Direct sourced shell assertions passed for Codex, Claude Code, Claude Cowork, Copilot, and Grok; Jest criteria-only update tests passed for Cline, Cline v2, and OpenCode.)
- [x] If a caller supplies acceptanceCriteria and a successful mutation response explicitly returns acceptanceCriteria empty, the plugin reports requirements_acceptance_criteria_not_captured instead of success. (evidence: Direct sourced shell assertions and focused Jest tests exercise the explicit empty-response case.)
- [x] Requirement create/update calls without acceptanceCriteria continue to work without injecting an empty criteria list. (evidence: Shell no-AC create assertions passed and existing TypeScript focused tests remain green.)

## FR-SUPPORT-010 MCP Context Unification

Local MCP server providing context retrieval, TODO management, repository access, session logging, and ingestion capabilities for AI agent integration.

**Covered by:** `ContextController`, `TodoController`, `RepoController`, `SessionLogController`, `McpServerMcpTools`, `McpDbContext`, `HybridSearchService`, `EmbeddingService`, `VectorIndexService`, `Fts5SearchService`, `RepoFileService`, `IngestionCoordinator`

## FR-SUPPORT-010A SessionLog Workspace Stamping

Session log POST shall stamp the resolved workspace ID on every persisted row (parent SessionLog plus all child entities: turns, actions, tags, context items, processing dialog, commits, string-list items) so a POST followed by a GET under the same workspace context returns the same record. When no workspace context is resolved (ingestion / batch import paths), WorkspaceId defaults to empty string and the DbContext-level auto-stamp populates it from `_workspaceId` if available.

**Covered by:** `SessionLogService.StampWorkspaceId`, `McpDbContext.StampWorkspaceId`, `SessionLogControllerTests.WhenPostingThenGetBySessionIdReturnsRecord`

## FR-SUPPORT-010B SessionLog ProblemDetails Errors

Session log POST shall return RFC 7807 ProblemDetails on body-binding or validation failure. Error responses cite the offending JSON path under `errors`, never the action-parameter name. Content-Type is `application/problem+json`. The accepted top-level shape is documented in the response `detail`.

**Covered by:** `Program.cs` (`InvalidModelStateResponseFactory`), `SessionLogController.SubmitAsync` (`ValidationProblem` calls), `SessionLogControllerTests.WhenPostingMalformedWorkspaceFieldThenReturnsProblemDetailsWithoutDtoKey`

## FR-SUPPORT-010C SessionLog REST Surface Completion

Session log REST shall expose `GET /mcpserver/sessionlog/{agent}/{sessionId}` (single-record fetch under tenancy) and `POST /mcpserver/sessionlog/{agent}/{sessionId}/turn` (turn-append by RequestId). Unsupported verbs on either route return 405 Method Not Allowed with an `Allow` header.

**Covered by:** `SessionLogController.GetByIdAsync`, `SessionLogController.UpsertTurnAsync`, `SessionLogService.GetAsync`, `SessionLogService.UpsertTurnAsync`

## FR-SUPPORT-010E Stateless session lifecycle endpoints

The session-log API SHALL expose stateless open/begin/complete/fail lifecycle operations keyed by (agent, sessionId, requestId) requiring no in-process active-session state.

## FR-SUPPORT-010F Additive partial session-log submits

Whole-session submit SHALL merge additively: omitted session and turn fields never overwrite previously persisted values.

## FR-TEST-002 FR-TEST-002

Legacy test-planning functional import stub retained for historical traceability. Status: reserved; active test requirements are tracked under concrete TEST-* IDs in Testing-Requirements.md.

