# FR-MCP

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-001 Configurable workspace root and paths

The server shall support configurable `RepoRoot`, `TodoFilePath`, `DataDirectory`, and index paths.

**Covered by:** `IngestionOptions`, `IOptions`
Scope: layer-1+

## FR-MCP-002 TODO management API

The server shall provide CRUD/query operations for TODO items over REST and STDIO.

**Covered by:** `TodoController`, `TodoService`, `SqliteTodoService`
Scope: layer-1+

## FR-MCP-003 Session log ingestion and query

The server shall ingest session logs and support searchable queries.

**Covered by:** `SessionLogController`, `SessionLogService`
Scope: layer-1+

## FR-MCP-004 Hybrid context search

The server shall support FTS and vector search over indexed content.

**Covered by:** `HybridSearchService`, `Fts5SearchService`, `VectorIndexService`, `EmbeddingService`
Scope: layer-1+

## FR-MCP-005 GitHub issue sync

The server shall support GitHub issue lifecycle integration and ISSUE-* TODO synchronization.

**Covered by:** `GitHubController`, `GitHubCliService`, `IssueTodoSyncService`
Scope: layer-1+

## FR-MCP-006 Multi-source ingestion

The server shall ingest repository files, session logs, external docs, and issue content.

**Covered by:** `IngestionCoordinator`, `RepoIngestor`, `SessionLogIngestor`, `ExternalDocsIngestor`, `GitHubIngestor`, `IssueIngestor`
Scope: layer-1+

## FR-MCP-007 Dual transport

The server shall support HTTP and STDIO MCP transports.

**Covered by:** `Program.cs`, `QBrainAiMcpTools`, `McpStdioHost`
Scope: layer-1+

## FR-MCP-008 Containerized deployment

The server shall support containerized deployment and packaged distribution.

**Covered by:** `Dockerfile`, `docker-compose.mcp.yml`
Scope: layer-1+

## FR-MCP-009 Workspace Management

The server shall support dynamic workspace registration, configuration, and lifecycle management - replacing static instance configuration - with directory scaffolding and Base64URL-encoded path keys. All workspaces are served on a single port via `X-Workspace-Path` header resolution (see FR-MCP-043).

**Covered by:** `WorkspaceController`, `WorkspaceService`, `WorkspaceConfigEntry`
Scope: layer-1+

## FR-MCP-011 Workspace Process Orchestration

The server shall manage workspace lifecycle via marker files: write `AGENTS-README-FIRST.yaml` on start, remove on stop. All workspaces share the single host process and port. Automatic startup of all registered workspaces writes markers on service start.

**Covered by:** `WorkspaceProcessManager`, `IWorkspaceProcessManager`, `MarkerFileService`
Scope: layer-1+

## FR-MCP-012 Tool Registry

Agents shall be able to discover tools by keyword search across global and workspace-scoped tool definitions, and install tool definitions from GitHub-backed bucket repositories.

**Covered by:** `ToolRegistryController`, `ToolRegistryService`, `ToolBucketService`
Scope: layer-1+

## FR-MCP-013 Per-Workspace Auth Tokens

The server shall protect all `/qbrainai/*` API endpoints with per-workspace cryptographic tokens that rotate on each service restart. Tokens are discoverable via the `AGENTS-README-FIRST.yaml` marker file, checked via the `X-Api-Key` header or `api_key` query parameter, and enforced by `WorkspaceAuthMiddleware`. Workspace resolution uses a three-tier chain: `X-Workspace-Path` header → API key reverse lookup → default workspace (see FR-MCP-043).

**Covered by:** `WorkspaceAuthMiddleware`, `WorkspaceTokenService`, `WorkspaceResolutionMiddleware`, `MarkerFileService`
Scope: layer-1+

## FR-MCP-014 Pairing Web UI

*Moved to [Requirements-WebUI.md](Requirements-WebUI.md#fr-mcp-014-pairing-web-ui)*
Scope: layer-1+

## FR-MCP-015 Tunnel Providers

The server shall expose its HTTP interface to the internet via pluggable tunnel providers (ngrok, Cloudflare, FRP) configured through a strategy pattern and registered as hosted services.

**Covered by:** `NgrokTunnelProvider`, `CloudflareTunnelProvider`, `FrpTunnelProvider`
Scope: layer-1+

## FR-MCP-016 MCP Streamable HTTP Transport

The server shall expose a native MCP protocol endpoint at `/mcp-transport` coexisting with the REST API on the same port, enabling standard MCP client connections via `ModelContextProtocol.AspNetCore`.

**Covered by:** `Program.cs` (MapMcp), `ModelContextProtocol.AspNetCore`
Scope: layer-1+

## FR-MCP-017 Windows Service

The server shall run as a Windows service with automatic startup, failure recovery (restart on failure with 60 s delay), and PowerShell-based install/update/uninstall management.

**Covered by:** `Program.cs` (UseWindowsService), `Manage-McpService.ps1`
Scope: layer-1+

## FR-MCP-018 Marker File Agent Discovery

When a workspace is started, the server shall write an `AGENTS-README-FIRST.yaml` marker file to the workspace root containing the shared host port, all endpoint paths, a machine-readable prompt, and PID. All markers point to the same port; workspace identity is resolved via the `X-Workspace-Path` header. The marker shall be removed when the workspace is stopped.

**Covered by:** `MarkerFileService`, `WorkspaceProcessManager`
Scope: layer-1+

## FR-MCP-019 Workspace Host Controller Isolation

*Obsolete - replaced by single-app multi-tenant model (FR-MCP-043).* All controllers are available on the single host. Workspace lifecycle management endpoints on `WorkspaceController` remain admin-only.
Scope: layer-1+

## FR-MCP-020 Workspace Auto-Start on Service Startup

On service startup, the server shall automatically write marker files for all workspaces already registered, restoring agent discoverability without manual intervention. All workspaces share the single host port.

**Covered by:** `WorkspaceProcessManager` (IHostedService.StartAsync)
Scope: layer-1+

## FR-MCP-021 Workspace Auto-Init and Auto-Start on Creation

When a new workspace is registered, the server shall automatically initialize the workspace directory scaffold (todo.yaml, mcp.db, docs structure) and write its marker file, so the workspace is immediately operational on the shared port.

**Covered by:** `WorkspaceController` POST, `WorkspaceService.InitAsync`
Scope: layer-1+

## FR-MCP-022 Tool Registry Default Bucket Seeding

On first startup, the server shall seed default tool buckets from configuration (`Mcp:ToolRegistry:DefaultBuckets`) if they are not already registered, ensuring new installations have the primary tool repository available without manual setup.

**Covered by:** `ToolRegistryOptions`, `Program.cs`
Scope: layer-1+

## FR-MCP-023 AI-Assisted Requirements Analysis

The server shall provide a requirements analysis capability that invokes the Copilot CLI to examine a TODO item's title, description, and technical details, identify matching existing FR/TR IDs from the project docs, create new FR/TR entries for unaddressed functionality, and persist the assigned IDs back to the TODO item.

**Covered by:** `RequirementsService`, `IRequirementsService`, `ICopilotClient`
Scope: layer-1+

## FR-MCP-024 Markdown Session Log Ingestion

The ingestion pipeline shall parse legacy Markdown session log files (matching a `# Session Log - {title}` header pattern) into the unified session log schema alongside JSON session logs, enabling retroactive indexing of pre-existing agent session records.

**Covered by:** `MarkdownSessionLogParser`, `SessionLogIngestor`
Scope: layer-1+

## FR-MCP-025 Primary Workspace Detection and Deduplication

One workspace is designated as the **primary** workspace - served by the host process directly with no child `WebApplication` spun up. Only a marker file is written. Resolution order: (1) first enabled workspace with `IsPrimary = true` and lowest port; (2) enabled workspace with lowest port if none marked primary; (3) no primary if no workspaces enabled.

**Status:** ✅ Complete

**Covered by:** `WorkspaceProcessManager`, `WorkspaceConfigEntry`, `Program.cs` primary-workspace resolution block
Scope: layer-1+

## FR-MCP-026 OIDC Authentication

The server shall support standards-based OIDC JWT Bearer authentication for management endpoints using a configurable open-source .NET OIDC provider. Optional external identity federation (for example, GitHub) may be configured through that provider. Management endpoints (agent mutations) require JWT; read endpoints use existing API key auth.

**Covered by:** `OidcAuthOptions`, `Program.cs`, `AgentController`, `Setup-McpKeycloak.ps1`, `setup-mcp-keycloak.sh`
Scope: layer-1+

## FR-MCP-027 Agent Definition Management

The server shall provide CRUD operations for agent type definitions with built-in defaults for well-known AI coding agents (copilot, cline, cursor, windsurf, claude-code, aider, continue). Built-in definitions are seeded on first run and cannot be deleted.

**Covered by:** `AgentController`, `AgentService`, `AgentDefaults`, `AgentDefinitionEntity`
Scope: layer-1+

## FR-MCP-028 Per-Workspace Agent Configuration

The server shall support per-workspace agent configuration with overrides for launch command, models, branch strategy, seed prompt, instruction files, isolation strategy, restart policy, and marker additions. Agents can be banned per-workspace or globally with optional PR-gated unbanning. All agent lifecycle events (add, launch, exit, ban, unban, delete, merge, init) are logged for audit.

**Covered by:** `AgentController`, `AgentService`, `AgentWorkspaceEntity`, `AgentEventLogEntity`, `AgentHealthMonitorService`
Scope: layer-1+

## FR-MCP-029 CQRS Framework

A standalone CQRS framework (`McpServer.Cqrs`) shall provide async command/query dispatch with a Result monad, decimal correlation IDs (`baseId.counter`), pipeline behaviors, and an `ILoggerProvider` implementation that auto-enriches structured logs with decomposed correlation context. The Dispatcher shall automatically log Result outcomes (success at Debug, errors at Error/Warning).

**Status:** ✅ Complete

**Covered by:** `McpServer.Cqrs` project (`Dispatcher`, `CallContext`, `CorrelationId`, `Result<T>`, `IPipelineBehavior`), `McpServer.Cqrs.Mvvm` (IViewModelRegistry, ViewModelRegistryExtensions). Workspace and policy view-models moved to the separate `McpServerManager` repository (former `McpServer.UI.Core` / `WorkspaceListViewModel` / `WorkspacePolicyViewModel` / `AddUiCore` DI extension).

**Implementation:** 37 unit tests passing. Provides `ICommand<T>`/`IQuery<T>` message types, `ICommandHandler<,>`/`IQueryHandler<,>` handlers, `Dispatcher` with pipeline behavior chain, `CallContext` with `CorrelationId` for structured logging, and `Result<T>` monad with success/error paths. MVVM layer adds `IViewModelRegistry` for CLI exec command support.
Scope: layer-1+

## FR-MCP-030 Director CLI

*Moved to [Requirements-Director.md](Requirements-Director.md#fr-mcp-030-director-cli)*
Scope: layer-1+

## FR-MCP-031 QBrainAi Management Web UI

*Moved to [Requirements-WebUI.md](Requirements-WebUI.md#fr-mcp-031-mcpserver-management-web-ui)*
Scope: layer-1+

## FR-MCP-032 Enhanced GitHub Integration

Enhanced GitHub integration capabilities including GitHub federation through the configured OIDC provider for user authentication, and GitHub OAuth for agent workspace management and PR workflows. *(Planned - tracked as high-priority TODO.)*
Scope: layer-1+

## FR-MCP-033 Natural Language Policy Management

A Copilot-integrated prompt tool that accepts natural language policy directives (e.g. "Ban chinese sources from all workspaces") and translates them into workspace configuration changes across all or targeted workspaces. Each policy change is session-logged per affected workspace with action type `policy_change`.

**Covered by:** `WorkspaceController` (`POST /qbrainai/workspace/policy`), `WorkspacePolicyService`, `WorkspacePolicyDirectiveParser`, `QBrainAiMcpTools.workspace_policy_apply`
Scope: layer-1+

## FR-MCP-034 Workspace Compliance Configuration

Per-workspace compliance configuration supporting four ban lists: `BannedLicenses` (SPDX identifiers), `BannedCountriesOfOrigin` (ISO 3166-1 alpha-2 codes), `BannedOrganizations`, and `BannedIndividuals`. Ban lists are conditionally rendered into the AGENTS-README-FIRST.yaml marker prompt via Handlebars templates. Agents must verify compliance before adding dependencies and log violations.

**Covered by:** `WorkspaceDto`, `WorkspaceCreateRequest`, `WorkspaceUpdateRequest`, `MarkerFileService`
Scope: layer-1+

## FR-MCP-035 Agent Values and Conduct Enforcement

The marker prompt shall include mandatory sections for: absolute honesty, correctness above speed, complete decision documentation, professional representation and audit trail (commits, PRs, issues logged in full), and source attribution (web references logged). These are non-configurable and always present.

**Covered by:** `templates/prompt-templates.yaml` (default-marker-prompt)
Scope: layer-1+

## FR-MCP-036 Audited Copilot Interactions

Every server-initiated Copilot interaction must be session-logged in every affected workspace. An `AuditedCopilotClient` decorator wraps `ICopilotClient` to create session log entries before and after each call, with action type `copilot_invocation`.

**Covered by:** `AuditedCopilotClient`, `Program.cs` DI registration, `McpStdioHost` DI registration, `CopilotServiceCollectionExtensions`
Scope: layer-1+

## FR-MCP-037 Director CLI Exec Command

*Moved to [Requirements-Director.md](Requirements-Director.md#fr-mcp-037-director-cli-exec-command)*
Scope: layer-1+

## FR-MCP-038 Session Continuity Protocol

Agents must follow a session continuity protocol: at session start, read the marker file, query recent session logs (limit=5), query current TODOs, and read Requirements-Matrix.md. During long sessions, post updated session logs every ~10 interactions. Requirements and design decisions must be captured as they emerge, not deferred.

**Covered by:** `templates/prompt-templates.yaml` (default-marker-prompt)
Scope: layer-1+

## FR-MCP-039 MCP Context Indexing for New Projects

Repo-local source files from McpServer.Cqrs and McpServer.Cqrs.Mvvm shall be indexed into the MCP context store for semantic search. McpServer.UI.Core and McpServer.Director have moved to the separate McpServerManager repository and must not be advertised as indexed capabilities in this repository marker.
Scope: layer-1+

## FR-MCP-040 Requirements Document CRUD Management

The server shall support CRUD operations for Functional Requirements (FR), Technical Requirements (TR), Testing Requirements (TEST), and FR-to-TR mapping rows backed by the canonical project requirements Markdown files.

**Covered by:** `RequirementsController`, `RequirementsDocumentService`, `IRequirementsRepository`
Scope: layer-1+

## FR-MCP-041 Requirements Document Generation

The server shall expose a requirements document generation endpoint that renders any canonical requirements document as Markdown, including the `Requirements-Matrix.md` traceability matrix, and exports all canonical documents directly to the workspace with canonical filenames.

**Status:** ✅ Complete

**Covered by:** `RequirementsController` (`POST /mcpserver/requirements/generate`), `RequirementsDocumentService`, `RequirementsDocumentRenderer`
Scope: layer-1+

## FR-MCP-042 Requirements Management MCP Tools

The STDIO MCP tool surface shall expose requirements management tools for listing, generating, creating, updating, and deleting requirements entries so AI agents can manage requirements directly from a conversation.

**Covered by:** `FwhMcpTools` (`requirements_list`, `requirements_generate`, `requirements_create`, `requirements_update`, `requirements_delete`), `RequirementsDocumentService`
Scope: layer-1+

## FR-MCP-043 Multi-Tenant Workspace Resolution

The server shall resolve the target workspace per-request using a three-tier resolution chain: (1) `X-Workspace-Path` header (highest priority), (2) API key reverse lookup via `WorkspaceTokenService`, (3) default/primary workspace from configuration. All workspaces are served on a single port; no per-workspace Kestrel hosts are spawned.

**Covered by:** `WorkspaceResolutionMiddleware`, `WorkspaceContext`, `WorkspaceTokenService`, `WorkspaceAuthMiddleware`
Scope: layer-1+

## FR-MCP-044 Shared Database Multi-Tenancy

All workspace data shall be stored in a single shared SQLite database with a `WorkspaceId` discriminator column on every entity table. EF Core global query filters ensure workspace data isolation per-request. Cross-workspace queries use `IgnoreQueryFilters()` for admin operations.

**Covered by:** `McpDbContext`, `WorkspaceContext`, all entity types (`WorkspaceId` property)
Scope: layer-1+

## FR-MCP-045 Cross-Workspace TODO Move

The server shall support moving a TODO item from one workspace to another via REST (`POST /qbrainai/todo/{id}/move`) and STDIO (`todo_move` MCP tool), preserving all item fields including implementation tasks, requirements, and metadata. The move is implemented as create-in-target then delete-from-source.

**Covered by:** `TodoController.MoveAsync`, `FwhMcpTools.TodoMove`, `TodoMoveRequest`, `TodoServiceResolver`
Scope: layer-1+

## FR-MCP-046 Voice Conversation Sessions

The server shall provide voice-enabled agent interaction via Copilot CLI, supporting session creation with device binding, voice turn processing (synchronous and SSE streaming), transcript retrieval, session interruption, ESC-key injection for generation cancellation, and automatic idle session cleanup with configurable timeout. Voice connections can attach to running pooled agents, including agents currently processing one-shot work. One active session per device is enforced.

**Covered by:** `VoiceController`, `VoiceConversationService`, `VoiceConversationOptions`, `CopilotInteractiveSession`
Scope: layer-1+

## FR-MCP-047 Desktop Process Launch

The server shall support launching interactive desktop processes from a Windows service (LocalSystem) context using `CreateProcessAsUser` with WTS session token negotiation, enabling Copilot CLI and other GUI/console tools to run on the interactive desktop with stdio pipe redirection or visible console windows.

**Covered by:** `DesktopProcessLauncher`, `NativeMethods`
Scope: layer-1+

## FR-MCP-048 YAML Configuration Support

The server shall support `appsettings.yaml` as an optional configuration source loaded after `appsettings.json` with hot reload, enabling YAML-format configuration alongside JSON for local-only overrides.

**Covered by:** `Program.cs` (`AddYamlFile`), `NetEscapades.Configuration.Yaml`
Scope: layer-1+

## FR-MCP-049 Prompt Template Registry

The server shall provide a global prompt template registry with REST API endpoints (`/mcpserver/templates`) and MCP tools for CRUD operations (list, get, create, update, delete) and test/render operations. Templates are stored as YAML files, support Handlebars rendering with declared variables, and are filterable by category, tag, and keyword. A Director TUI tab shall enable template browsing and preview.

**Covered by:** `PromptTemplateController`, `PromptTemplateService`, `PromptTemplateRenderer`, `FwhMcpTools` (6 template tools), `TemplateClient`. The Director template-browsing TUI surface (`TemplatesScreen`) moved to the separate `McpServerManager` repository.
Scope: layer-1+

## FR-MCP-050 Per-Agent Workspace Runtime Management

The server shall provide runtime management for workspace-bound agents, including process launch/stop/status, configurable isolation modes (`none`, `worktree`, `clone`), branch strategy handling (`direct`, `feature-branch`, `worktree`), session-log linkage to known agent definitions, marker-file agent-specific instruction sections, and restart-policy-driven health monitoring.

**Covered by:** `AgentService`, `AgentController`, `IAgentProcessManager`, `AgentProcessManager`, `IAgentIsolationStrategy`, `AgentIsolationStrategyResolver`, `IAgentBranchStrategy`, `AgentBranchStrategyResolver`, `WorkspaceProcessManager`, `SessionLogService`, `AgentHealthMonitorService`
Scope: layer-1+

## FR-MCP-051 System-Wide Default Copilot Model

The server SHALL allow configuration of a system-wide default Copilot model (e.g., `gpt-5.3-codex`) that is applied consistently across all Copilot session types - server-initiated CLI invocations (`CopilotClientOptions.Model`), voice conversation sessions (`VoiceConversationOptions.CopilotModel`), and built-in agent type defaults (`AgentDefaults`). The configured model SHALL be overridable per-workspace via agent configuration and per-invocation via explicit parameters.

**Technical Implementation:** [TR-MCP-CFG-005](./Technical-Requirements.md#tr-mcp-cfg-005) | [Mapping](./TR-per-FR-Mapping.md)
Scope: layer-1+

## FR-MCP-052 Agent Pool Runtime Orchestration

The server shall maintain a configured pool of long-lived agent processes and route agent execution through pooled agents instead of independent ad-hoc launches.

Agent pool definitions shall include: `AgentName`, `AgentPath`, `AgentModel`, `AgentSeed`, and `AgentParameters`.

**Covered by:** `AgentPoolOptions` *(planned)*, `AgentPoolService` *(planned)*
Scope: layer-1+

## FR-MCP-053 One-Shot Queueing and Deferred Attachment

One-shot requests shall execute through the agent pool queue. If no eligible pooled agent is available, requests shall be queued and dequeued when an agent becomes available.

One-shot requesters shall receive processing lifecycle notifications and may attach to the running agent via interactive voice session or read-only response stream.

**Covered by:** `AgentPoolQueueService` *(planned)*, `AgentPoolController` *(planned)*
Scope: layer-1+

## FR-MCP-054 Agent Pool Availability and Control Endpoints

The server shall expose endpoints to list pooled agents and real-time availability, and provide runtime controls for connect, start, stop, recycle, queue inspection, queue cancel/remove, queue reorder (queued items only), and free-form one-shot enqueue.

The server shall expose a dedicated Agent Pool notification SSE stream with payload fields `AgentName`, `LastRequestPrompt`, and `SessionId`.

**Covered by:** `AgentPoolController` *(planned)*, `AgentPoolNotificationService` *(planned)*
Scope: layer-1+

## FR-MCP-055 Default Agent Selection by Request Intent

If a request omits `AgentName`, the server shall determine the request intent and select the configured default agent for that intent using intent-default flags.

One-shot endpoint context values shall support: `Plan`, `Status`, `Implement`, and `AdHoc`.

**Covered by:** `AgentPoolIntentResolver` *(planned)*, `AgentPoolService` *(planned)*
Scope: layer-1+

## FR-MCP-056 Template-Aware One-Shot Prompt Resolution

One-shot requests shall support template-driven and ad-hoc prompt modes. Template mode accepts `promptTemplateId` with optional values dictionary and workspace-context-derived values; caller-provided values override workspace context on key conflicts.

The server shall expose an endpoint that accepts prompt template ID plus values dictionary and returns the rendered prompt.

If context is provided without template ID, the server shall use current context-based template resolution. For `AdHoc` context without template ID, explicit ad-hoc prompt text is required.

One-shot endpoint template rendering shall support an `id` parameter used to populate `{id}` placeholders in templates. `id` is required only for template-resolved requests.

**Covered by:** `PromptTemplateController` *(planned extension)*, `AgentPoolController` *(planned)*
Scope: layer-1+

## FR-MCP-057 Director Agent Pool Management UI

*Moved to [Requirements-Director.md](Requirements-Director.md#fr-mcp-057-director-agent-pool-management-ui)*
Scope: layer-1+

## FR-MCP-058 Interactive Presence Signaling

When a user disconnects from an interactive response stream, the server shall send `User is AFK.` to the agent session.

When a user reestablishes an interactive response stream connection, the server shall send `User is here.` to the agent after stream establishment.

These presence messages do not apply to one-shot sessions.

**Covered by:** `AgentPoolStreamService` *(planned)*, `VoiceConversationService` *(planned extension)*
Scope: layer-1+

## FR-MCP-059 DI-Centered Single Source of Truth State Flow

The system SHALL enforce a DI-centered Single Source of Truth architecture across `QBrainAi.Support.Mcp`: authoritative mutable data sources must be owned by DI-registered singleton or scoped services, services shall notify state availability/changes via `INotifyPropertyChanged`, and consumers shall pull current state from the owning service rather than receiving pushed data payloads.

**Technical Implementation:** [TR-MCP-ARCH-002](./Technical-Requirements.md#tr-mcp-arch-002) | [Mapping](./TR-per-FR-Mapping.md)
Scope: layer-1+

## FR-MCP-060 Director MVVM/CQRS Full Endpoint Coverage

*Moved to [Requirements-Director.md](Requirements-Director.md#fr-mcp-060-director-mvvmcqrs-full-endpoint-coverage)*
Scope: layer-1+

## FR-MCP-061 Canonical TODO and Session Identifier Conventions

The server shall enforce canonical identifier conventions for newly created TODO and session log payloads:

- Persisted TODO IDs must match either uppercase kebab-case ending in a three-digit sequence suffix (for example `PHASE0-REMOTE-001` or `MCP-TODO-CREATE-001`) or `ISSUE-{number}` for canonical GitHub-backed TODOs.
- Create requests may use `ISSUE-NEW` only as a temporary server-side alias for immediate GitHub-backed TODO creation; persisted TODO IDs must still be canonical.
- Session IDs must match `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>` and be prefixed by the exact `sourceType`/`agent`.
- Request IDs must match `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`.

Validation failures return client-visible errors without mutating persisted data.

**Covered by:** `TodoValidator`, `TodoService`, `SqliteTodoService`, `TodoCreationService`, `SessionLogIdentifierValidator`, `SessionLogController`, `SessionLogService`
Scope: layer-1+

## FR-MCP-062 Workspace Change Notifications

The server shall provide a real-time workspace change notification system that publishes create/update/delete domain events for workspace mutations (TODOs, session logs, repo files, context sync, tool registry, tool buckets, workspaces, GitHub operations, marker lifecycle, agents, and requirements) over Server-Sent Events at `GET /qbrainai/events`, with optional category filtering.

**Covered by:** `IChangeEventBus`, `ChannelChangeEventBus`, `EventStreamController`, `TodoService`, `SqliteTodoService`, `SessionLogService`, `RepoFileService`, `ToolRegistryService`, `ToolBucketService`, `WorkspaceService`, `WorkspaceController`, `AgentService`, `RequirementsDocumentService`, `IngestionCoordinator`, `GitHubController`, `WorkspaceProcessManager`
Scope: layer-1+

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
Scope: layer-1+

## FR-MCP-064 Marketing and Adoption Documentation

The system SHALL provide marketing-oriented documentation that clearly explains what QBrainAi is, its key feature set, why adopters need it, and the currently supported UI tooling surfaces (including VS extension and Web UI experiences).

**Technical Implementation:** [TR-MCP-DOC-001](./Technical-Requirements.md#tr-mcp-doc-001) | [Mapping](./TR-per-FR-Mapping.md)
Scope: layer-1+

## FR-MCP-065 Direct Website URL Ingestion

The server shall ingest remote website content directly from one URL (with optional bounded same-host crawling) into the context store and GraphRAG pipeline without pre-downloading files into `docs/external`.

**Covered by:** `ContextController` (`POST /qbrainai/context/ingest-website`), `WebsiteIngestor`, `IngestionCoordinator`, `FwhMcpTools` (`context_ingest_website`), `ContextClient.IngestWebsiteAsync`
Scope: layer-1+

## FR-MCP-066 Hosted Microsoft Agent Framework Library

The system SHALL provide a .NET 9 class library that packages an MCP-aware agent for hosting inside external .NET applications built on Microsoft Agent Framework.

The hosted agent SHALL include a built-in workflow that treats MCP Server session logging, TODO management, repository file access, local desktop process launch, and stateful in-process PowerShell sessions as first-class primitives, allowing host applications to bootstrap/continue session logs, create and update turns, inspect and mutate TODO items, browse repository files, launch local programs, run PowerShell commands inside persistent local runspaces, drive those runspaces directly through the host-facing agent contract when needed, and execute plan/status/implementation task flows without reimplementing those integrations.

**Status:** ✅ Complete

**Technical Implementation:** [TR-MCP-AGENT-006](./Technical-Requirements.md#tr-mcp-agent-006) | [TR-MCP-AGENT-007](./Technical-Requirements.md#tr-mcp-agent-007) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `McpServer.McpAgent` (`ServiceCollectionExtensions`, `McpAgentOptions`, `Hosting/*`, `PowerShellSessions/*`, `SessionLog/*`, `Todo/*`), `McpServer.Client` (`McpServerClient`, `RepoClient`, `DesktopClient`), `McpServer.McpAgent.SampleHost` (`Program.cs`, `SampleHostPreviewFactory.cs`)
Scope: layer-1+

## FR-MCP-067 Detailed Internal Server Error Responses

The system SHALL return a detailed client-visible error description for every endpoint response that fails with HTTP 500.

Detailed 500 responses SHALL describe the failed operation clearly enough for callers to diagnose the failure path and distinguish server faults from client mistakes, while remaining sanitized so secrets, tokens, and other sensitive internals are not exposed in the response body.

**Technical Implementation:** [TR-MCP-HTTP-002](./Technical-Requirements.md#tr-mcp-http-002) | [Mapping](./TR-per-FR-Mapping.md)
Scope: layer-1+

## FR-MCP-068 Administrative Configuration Management API

The server SHALL provide an admin-only configuration API that returns the current effective configuration as flattened key-value pairs and supports patching selected values back into `appsettings.yaml` without rewriting unrelated settings or serializing values that originate only from non-file configuration providers.

The configuration-management endpoints SHALL require standard JWT Bearer authentication with the `admin` role. When OIDC is not configured, the endpoints SHALL remain unavailable.

**Technical Implementation:** [TR-MCP-CFG-006](./Technical-Requirements.md#tr-mcp-cfg-006) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `ConfigurationController`, `AppSettingsFileService`, `Program.cs` (`ConfigurationAdmin` policy), `WorkspaceController` (shared appsettings helper reuse)
Scope: layer-1+

## FR-MCP-069 Immediate GitHub-Backed TODO Creation

The server shall support a create-time TODO identifier of `ISSUE-NEW` that immediately creates a GitHub issue, determines the resulting issue number, and persists the local TODO using the canonical `ISSUE-{number}` identifier returned by GitHub.

This behavior shall be available through all server-side TODO creation entry points that already support normal TODO creation. Callers shall receive the canonical persisted identifier rather than the temporary `ISSUE-NEW` alias.

**Technical Implementation:** [TR-MCP-TODO-003](./Technical-Requirements.md#tr-mcp-todo-003) | [TR-MCP-GH-005](./Technical-Requirements.md#tr-mcp-gh-005) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `TodoCreationService`, `GitHubCliService`, `TodoController`, `FwhMcpTools`, `VoiceConversationService`
Scope: layer-1+

## FR-MCP-070 Authoritative ISSUE-* Update Sync and Immutable Descriptions

The server shall treat MCP TODO updates as authoritative for existing `ISSUE-{number}` TODO items. When an `ISSUE-*` TODO is updated through a server TODO update surface, the server shall push the authoritative MCP state to GitHub, append a GitHub issue comment describing the applied change set, and keep the ISSUE description/body immutable after the first sync.

Priority synchronization shall use canonical GitHub labels in the form `priority: HIGH`, `priority: MEDIUM`, or `priority: LOW`. GitHub-to-TODO refreshes for existing `ISSUE-*` items shall preserve the local priority and description that were already established by the first sync.

**Technical Implementation:** [TR-MCP-TODO-004](./Technical-Requirements.md#tr-mcp-todo-004) | [TR-MCP-GH-006](./Technical-Requirements.md#tr-mcp-gh-006) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `TodoUpdateService`, `IssueTodoSyncService`, `TodoController`, `FwhMcpTools`, `VoiceConversationService`
Scope: layer-1+

## FR-MCP-071 ISSUE Comment Round-Trip and GitHub-Driven Closure Reconciliation

The server shall round-trip `ISSUE-{number}` discussion between GitHub and MCP TODOs without mutating the established TODO description. GitHub-origin issue comments shall sync into the TODO note inside a generated GitHub-comments section, while user-authored TODO note content outside that generated section shall remain preserved across subsequent syncs.

When an `ISSUE-*` TODO is updated locally with new note text, the server shall propagate the appended TODO-authored comment back to GitHub as an issue comment. When the GitHub issue is later closed outside MCP, a GitHub-to-TODO sync shall mark the corresponding TODO as done.

**Technical Implementation:** [TR-MCP-GH-007](./Technical-Requirements.md#tr-mcp-gh-007) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `IssueTodoSyncService`, `TodoUpdateService`, `GitHubController`, `TodoController`
Scope: layer-1+

## FR-MCP-072 Database-Authoritative TODO Storage with YAML Projection and Audit History

The server shall treat SQLite as the authoritative current-state store for workspace TODO items. When a configured workspace TODO document already exists and the authoritative database is empty, initialization shall import the current YAML document once into SQLite and thereafter keep `docs/Project/TODO.yaml` synchronized as a deterministic projection of authoritative database state rather than as the live writable source of truth.

The server shall preserve TODO document metadata such as `notes`, `completed`, and `code-review-remediation.reference`, retain append-only audit history for TODO state mutations, and expose that audit history through HTTP, typed client, and MCP tool surfaces so callers can retrieve tracked TODO states even after deletion when history exists.

**Technical Implementation:** [TR-MCP-TODO-005](./Technical-Requirements.md#tr-mcp-todo-005) | [TR-MCP-TODO-006](./Technical-Requirements.md#tr-mcp-todo-006) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `SqliteTodoService`, `TodoYamlFileSerializer`, `TodoController`, `TodoClient`, `QBrainAiMcpTools`, `TodoServiceFactory`
Scope: layer-1+

## FR-MCP-073 Parseable Log Event Field Cap

The server shall cap each Parseable log event payload to a maximum of 250 top-level fields so Parseable ingest remains within the supported field-count envelope for a single event.

When application log events contain more structured properties than the Parseable limit allows, the server shall preserve the canonical Parseable metadata fields (`timestamp`, `level`, `message`, and `exception` when present) and omit excess user properties rather than emitting an oversized payload.

**Technical Implementation:** [TR-MCP-LOG-003](./Technical-Requirements.md#tr-mcp-log-003) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `ParseableEventFormatter`, `ParseableBatchFormatter`
Scope: layer-1+

## FR-MCP-074 Azure DevOps Repository Pipeline Migration

The repository SHALL define Azure DevOps YAML pipelines as the source of truth for repository CI/CD instead of the retired GitHub Actions workflow files.

The Azure DevOps pipeline SHALL preserve the current repository automation intent for branch/path-filtered validation, published server artifacts, documentation artifacts, Windows MSIX packaging, and branch-conditional client package publication, while ignoring any separate Copilot coding agent pipeline.

**Technical Implementation:** [TR-MCP-CI-001](./Technical-Requirements.md#tr-mcp-ci-001) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `azure-pipelines.yml`, `docs/AZURE-PIPELINES.md`
Scope: layer-1+

## FR-MCP-075 PowerShell Session Cache Discovery from `.mcpSession`

The PowerShell `McpSession` module SHALL discover and reuse the current session object cached in the workspace `.mcpSession` folder so follow-on commands can resolve the active session even when the caller does not pass an explicit session object.

This cache-discovery behavior SHALL remain backward compatible with the existing `.mcpServer/session.yaml` slug/state wrapper and SHALL prefer the current-session cache when both representations are available.

**Technical Implementation:** [TR-MCP-AGENT-013](./Technical-Requirements.md#tr-mcp-agent-013) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `tools/powershell/McpSession.psm1`
Scope: layer-1+

## FR-MCP-076 Marker File Trust Bootstrap and Session Authenticity Validation

The server shall render a trust-bootstrap contract into `AGENTS-README-FIRST.yaml` that instructs agents to verify the marker signature, perform a nonce-based `/health` challenge, and treat any signature or nonce mismatch as `MCP_UNTRUSTED`.

When trust validation fails, the bootstrap flow shall stop using MCP services and shall not probe additional MCP endpoints. The marker contract shall remain explicit enough for `McpSession`, `McpTodo`, and `McpContext` to follow the same verified bootstrap sequence without diverging on trust semantics.

**Technical Implementation:** [TR-MCP-SEC-003](./Technical-Requirements.md#tr-mcp-sec-003) | [TR-MCP-AGENT-014](./Technical-Requirements.md#tr-mcp-agent-014) | [Mapping](./TR-per-FR-Mapping.md)

**Covered by:** `src/QBrainAi.Services/Services/MarkerFileService.cs`, `templates/prompt-templates.yaml`, `src/QBrainAi.ServiceDefaults/Extensions.cs`, `tools/powershell/McpSession.psm1`, `tools/powershell/McpTodo.psm1`, `tools/powershell/McpContext.psm1`, `docs/context/module-bootstrap.md`, `docs/USER-GUIDE.md`
Scope: layer-1+

## FR-MCP-077 Server Federation and Request Proxying

The server shall support an opt-in federation mode that proxies incoming requests to a configured remote MCP server instance. Routing shall support a global default target and per-workspace overrides. The feature shall include anti-loop protection (via `X-Mcp-Federation-Hop` header with a configurable maximum hop count), transparent SSE/streaming forwarding for `/mcp-transport`, a runtime management REST API at `/qbrainai/federation`, and auto-discovery of federation targets from running tunnel providers.


**Technical Implementation:** `FederationOptions`, `FederationRegistry`, `FederationProxyService`, `FederationMiddleware`, `FederationController`

**Configuration:** `Mcp:Federation:Enabled`, `Mcp:Federation:Targets`, `Mcp:Federation:DefaultTarget`, `Mcp:Federation:WorkspaceRoutes`, `Mcp:Federation:MaxHops`
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given explicit legacy DirectProxy mode, global default routing and per-workspace overrides select the configured remote MCP instance; versioned project work remains under FR-MCP-103.
- [ ] Forwarded legacy requests carry X-Mcp-Federation-Hop and enforce the configured maximum hop count.
- [ ] Legacy /mcp-transport forwarding preserves SSE/streaming; /qbrainai/federation manages runtime routing, and running tunnel providers supply discoverable targets.

## FR-MCP-078 GraphRAG Ad-Hoc Document Ingestion

The server shall accept raw text or markdown content via a REST endpoint and MCP tool, chunk it, generate embeddings, store it in the context database and vector index, and optionally trigger a GraphRAG re-index. Documents ingested this way shall use source type "adhoc-text" by default and support caller-specified title, source type, and source key metadata.

**Status:** ✅ Complete

**Covered by:** `GraphRagController` (`POST /mcpserver/graphrag/documents/ingest`), `McpServer.GraphRag` (`GraphRagService` ad-hoc ingestion path), `FwhMcpTools` (graphrag ingest tool)
Scope: layer-1+

## FR-MCP-079 GraphRAG Entity and Relationship CRUD

The server shall provide full CRUD operations for explicit graph entity nodes and relationship edges, persisted in workspace-scoped EF Core tables. Entities shall have name, type, description, and extensible JSON metadata. Relationships shall link two entities with a typed, weighted, described edge. Deleting an entity shall cascade to all its relationships. All operations shall be available via REST endpoints, MCP tools, and REPL commands.

**Status:** ✅ Complete

**Covered by:** `GraphRagController` (entity + relationship endpoints), `McpServer.GraphRag` (`GraphRagService` entity / relationship CRUD), `GraphEntityEntity`, `GraphRelationshipEntity`, `FwhMcpTools` (graphrag entity/relationship tools), `McpServer.Repl.Core` (graphrag command shapes)
Scope: layer-1+

## FR-MCP-080 GraphRAG Document Management

The server shall provide endpoints to list indexed documents with chunk counts and token totals, retrieve chunks for a specific document ordered by chunk index, and delete a document with cascade removal of its chunks and corresponding vector index entries. All operations shall be workspace-scoped and available via REST endpoints, MCP tools, and REPL commands.

**Status:** ✅ Complete

**Covered by:** `GraphRagController` (document list/get/delete endpoints), `McpServer.GraphRag` (`GraphRagService` document management), `ContextDocumentEntity`, `ContextChunkEntity`, `FwhMcpTools` (graphrag document tools)
Scope: layer-1+

## FR-MCP-081 Byrd Iteration Phase and TODO Execution Persistence

The server shall persist Byrd iteration phases, decomposed execution TODOs, and TODO checkpoints so agents can resume multi-step work from MCP state instead of chat history. Persisted execution TODOs shall carry goal, summary, acceptance criteria, constraints, requirement links, relevant files, next action, test plan state, validation state, and linked session turn identifiers.

**Status:** ✅ Complete

**Covered by:** `TodoExecutionController` (`/mcpserver/todo-execution/*`), `TodoExecutionService`, Byrd phase + checkpoint entities, `McpDbContext`
Scope: layer-1+

## FR-MCP-082 Bounded Byrd Execution Context Hydration

The server shall return a bounded active TODO execution context and a checkpoint-based delta context for the current execution TODO. Hydration shall prefer concise requirement snippets, concise recent session-turn summaries, relevant files, artifacts, test state, validation state, and execution pointers, and shall not return full plan markdown or broad session-log history when compact state is sufficient.

**Status:** ✅ Complete

**Covered by:** `TodoExecutionController` (`active`, `next-ready`, `{todoId}`, `{todoId}/delta` endpoints), `TodoExecutionService`
Scope: layer-1+

## FR-MCP-083 Structured Android Validation for Byrd TODOs

The server shall expose a structured `adb_step` surface for safe Android validation actions used during Byrd execution. Supported actions shall be limited to fixed safe operations such as screenshot, tap, swipe, text input, keyevent, wait, app launch, and focus inspection, and their results shall be storable as validation evidence and TODO checkpoint artifacts.

**Status:** ✅ Complete

**Covered by:** `TodoExecutionController` (`POST /mcpserver/todo-execution/adb/step`), `TodoExecutionService.AdbStepAsync`, `AdbStepAction` / `AdbStepRequest` models, `FwhMcpTools` (`adb_step` tool)
Scope: layer-1+

## FR-MCP-084 Requirements Wiki Workspace Export/Import

The server, REPL, and agent plugins shall support requirements export and import in wiki format. Wiki export shall write both Azure DevOps Wiki and GitHub Wiki document folders directly under docs/Project/wiki. Wiki import shall detect wiki document folders, select the authoritative platform source using manifest and file modified timestamps, and create, update, delete, or ignore requirements and mappings to match the selected source.

**Status:** ✅ Complete

**Covered by:** `RequirementsController` (wiki export + ingest endpoints), `RequirementsDocumentService` (wiki renderer + parser), `McpServer.Repl.Core` (requirements wiki workflow commands)
Scope: layer-1+

## FR-MCP-085 QA Workspace Question CRUD

Create / read / update / delete a Question (title, body, tags[], author).
Scope: layer-1+

## FR-MCP-086 QA Workspace Answer CRUD

Create / read / update / delete an Answer attached to a Question.
Scope: layer-1+

## FR-MCP-087 QA Accepted Answer Invariant

Accept exactly one Answer per Question (idempotent; re-accept clears prior accepted answer).
Scope: layer-1+

## FR-MCP-088 QA Question Tag Filtering

Tag filter on Question list with AND semantics (`?tag=foo&tag=bar`).
Scope: layer-1+

## FR-MCP-089 QA Per User Voting

Vote (+1 / -1) on Question and Answer with one-vote-per-user enforcement. A given actor may hold at most one active vote per entity (Question or Answer). Submitting the same vote a second time is idempotent (returns the current state, no counter change, no new audit row). Submitting the opposite vote replaces the prior vote (counter adjusts by +/- 2; an audit row is written for the change). An actor may explicitly revoke their vote (counter adjusts by -1 or +1; audit row recorded). Voter identity is the value resolved by `IQaAuthorResolver`. The Question/Answer rows still store a denormalized counter for fast reads; the authoritative per-voter state lives in a `QaVoteEntity` table with a unique constraint (TR-MCP-QA-032). Voter identity and every vote transition (apply, change, revoke) appear in the audit history (TR-MCP-QA-019), and a voter-history endpoint exposes them (TR-MCP-QA-031).
Scope: layer-1+

## FR-MCP-090 QA Threaded Comments

Threaded comments on Questions and Answers with depth cap (<= 3).
Scope: layer-1+

## FR-MCP-091 QA FAQ Projection

`GET /qbrainai/qa/faq` returns questions that have an accepted answer, projected as `{ questionId, title, tags[], voteCount, acceptedAnswer: { body, author, voteCount }, deeplink }` where `deeplink = "/qbrainai/qa/questions/{id}"`. Default sort: accepted-answer voteCount desc, then question voteCount desc.
Scope: layer-1+

## FR-MCP-092 QA Author Resolution

Author identity resolves via request-body `author` if provided, falling back to API-key -> workspace-identity (via `WorkspaceTokenService`), then JWT `sub` claim.
Scope: layer-1+

## FR-MCP-093 QA Workspace Isolation

All Q&A data is workspace-scoped via composite PK `(WorkspaceId, Id)` and EF global query filter (TR-MCP-MT-003 pattern).
Scope: layer-1+

## FR-MCP-094 QA REST MCP STDIO and Client Parity

Q&A is reachable from REST, MCP STDIO tools, and the typed C# client library with feature parity.
Scope: layer-1+

## FR-MCP-095 QA REPL and PowerShell Parity

Q&A is reachable from the REPL via a `workflow.qa.*` command namespace (interactive REPL + `--agent-stdio` YAML envelope protocol) and from a PowerShell module (`McpQa.psm1`) following the `Verb-Noun` pattern of `McpTodo.psm1`.
Scope: layer-1+

## FR-MCP-096 QA User and Agent Documentation

Documentation surfaces the Q&A subsystem for end users, integrators, and agents (USER-GUIDE, CLIENT-INTEGRATION, api-capabilities, AGENTS, FAQ, README, plus a new `qa-schema.md` context doc).
Scope: layer-1+

## FR-MCP-097 QA Plugin Skill

Each Claude Code plugin (sibling repos `mcpserver-claude-code-plugin`, `mcpserver-claude-cowork-plugin`, `mcpserver-codex-plugin`, `mcpserver-cline-plugin`, `mcpserver-cline-v2-plugin`, `mcpserver-copilot-plugin`) gets a `skills/qa/SKILL.md` skill that lets agents use Q&A as a knowledge source - ask, answer, accept, and read FAQ via the REPL workflow.
Scope: layer-1+

## FR-MCP-098 QA Append Only Audit

Full audit tracking: every Q&A mutation (question create/update/delete, answer create/update/delete, accept/un-accept, vote +1/-1, comment add/delete) writes an append-only audit row capturing actor, action, version, full pre-mutation snapshot, and timestamp. Audit history is queryable per entity (`GET /qbrainai/qa/questions/{id}/audit`, `/answers/{id}/audit`, `/comments/{id}/audit`) and at workspace scope (`GET /qbrainai/qa/audit`) with paging, matching the TODO audit endpoint contract.
Scope: layer-1+

## FR-MCP-099 QA Web Research Capture

Mandatory web-research-to-Q&A capture: whenever a plugin/agent performs an internet search (web fetch, web search, MCP web tool, browser MCP, or any external HTTP retrieval) in order to answer a workspace-scoped question, the agent MUST post a new Question to `/qbrainai/qa/questions` containing the original prompt, then post an Answer containing the synthesized response with every source URL cited inline (markdown links) and as a `sources[]` array in the answer body's JSON sidecar. If the agent verified the synthesized answer, it MUST also call accept-answer. This is enforced by the `qa/SKILL.md` in every sibling plugin repo (mandatory rule at top of body) and surfaced in `AGENTS.md` / `CLAUDE.md` so agents cannot opt out. Captured `web_reference` actions in the session log point at the resulting Q&A entry's deeplink.
Scope: layer-1+

## FR-MCP-100 QA Close and Duplicate Flags

Close / duplicate flags: a Question can be closed with a reason and reopened; a Question can be marked as a duplicate of another Question (canonical) with both directions navigable. Closed questions are excluded from FAQ by default and from the default Question list (opt-in via `?includeClosed=true`); duplicates redirect FAQ consumers to the canonical Question via the deeplink. Close and duplicate transitions are full audit events (TR-MCP-QA-018 family).
Scope: layer-1+

## FR-MCP-101 QA Markdown Rendering and Sanitization

HTML / Markdown sanitization and rendering: every Question.Body, Answer.Body, and Comment.Body is authored as Markdown, stored verbatim, and additionally rendered to sanitized HTML at write time. Both representations are returned in the DTOs (`body` raw Markdown, `bodyHtml` sanitized HTML). The sanitizer strips script, iframe, on-* attributes, javascript: URLs, and other XSS vectors per a whitelist (allow paragraph, headings <=3, lists, code/pre, bold/italic, links with `rel="nofollow noopener"`, inline code, blockquote, tables). Sanitization runs on every write and on every audit-snapshot read so historical bodies are also safe to render.
Scope: layer-1+

## FR-MCP-102 QA Generated FAQ Wiki Page

FAQ wiki page: the documentation wiki output (Azure DevOps wiki under `docs/Project/wiki/azure/` and GitHub wiki under `docs/Project/wiki/github/`) includes a generated `FAQ.md` page rendered from the live `/qbrainai/qa/faq` endpoint of a configured workspace at build time. The page lists every accepted Q&A with the question title (linked to its detail endpoint), tags, vote counts, the rendered HTML body of the accepted answer, and the `sources[]` array as a sources section. The wiki Home / Sidebar / .order files reference the new FAQ page. The page is regenerated by the Nuke build during the wiki publication target.
Scope: layer-1+

## FR-MCP-103 Autonomous committed-history federation

Enrolled nodes execute and persist authorized business work locally while offline and exchange explicit version commits through LAB-OMARCHY. The hub coordinates project enrollment, guarded publication and reconciliation; it is not the executor of ordinary node requests. DirectProxy remains an explicitly selected legacy capability under FR-MCP-077. LocalProxy forwarding, outage command replay and read-switch are superseded for versioned federation.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given A cannot reach the hub, when it edits and commits T1, then local read returns the change and publication remains pending.
- [ ] Given H and A commit concurrently, when synchronization completes, then both histories survive or a visible review blocks only that branch.

## FR-MCP-104 Decision-complete agent plans

All generated plans must be detailed enough for a less expensive implementation model to execute successfully without resolving unstated product, architecture, interface, testing, or rollout decisions.
Scope: layer-1+

## FR-MCP-105 Database integrity and durable audit enforcement

The MCP Server must preserve database integrity across all supported providers by enforcing workspace, TODO, requirement, federation, soft-delete, and audit relationships as durable relational contracts.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Every workspace-scoped memory index row references a canonical Workspaces row; global memory and index rows retain null WorkspaceId.
- [ ] Memory version and index rows remain durable when a parent memory is soft-deleted; database relationships do not cascade-delete them.
- [ ] SQLite, PostgreSQL, and SQL Server migrate existing memory rows to the corrected constraints without data loss and reject orphan workspace references.

## FR-MCP-106 Reliable plugin requirement updates

Agent plugins must reliably update existing FR, TR, and TEST requirement records through workflow.requirements.update* commands, including priority changes, without failing inside the wrapper path.
Scope: layer-1+

## FR-MCP-107 Outstanding session work consolidation

MCP Server must support an audited consolidation plan that inventories outstanding work from a long session, separates validated changes from unresolved dirty work, and sequences completion through Byrd gates before deploy or publish.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each cumulative validation run uses an explicit unique run ID and deterministic TestResults/<runId> outputs; reports from another run or an earlier candidate cannot satisfy the current gate.
- [ ] Nuke Test and provider targets emit the selected-project inventory and exactly one fresh per-project TRX for every selected project without silently changing exclusions; missing, duplicate, or zero-discovery project results fail the gate.
- [ ] The session-lifecycle unit gate runs the complete Pester directory, Nuke Test, and Build.Tests non-integration scope, records structured Pester counters and each command result, and does not allow a later successful command to hide an earlier failure.
- [ ] Validation rejects stale, missing, duplicate, skipped, notExecuted, failed, empty, zero-discovery, and Pester failed-block or failed-container reports using run-start, source-file, tool-version, and candidate-source manifests.
- [ ] Build.Tests contains concrete red cases for fresh-run inventory acceptance, missing and duplicate reports, stale evidence, zero discovery, skips and notExecuted results, Pester failed blocks or containers, candidate-source drift, and earlier-command failure masking.

## FR-MCP-108 TODO Markdown description preservation

TODO create, update, read, audit, and projection paths shall preserve description Markdown formatting and whitespace instead of normalizing or stripping meaningful content.
Scope: layer-1+

## FR-MCP-109 Batch requirements mutation

The MCP Server shall allow agents and API clients to create or update multiple functional, technical, and test requirements in one atomic request by submitting a records array.
Scope: layer-1+

## FR-MCP-110 Deterministic Nuke PowerShell execution

Nuke build and deployment automation SHALL run PowerShell child hosts with non-interactive, no-profile flags so user profiles, prompts, aliases, and interactive host behavior cannot alter scripted execution.
Scope: layer-1+

## FR-MCP-111 Package workflow closeout skills across QBrainAi plugins

All QBrainAi plugin distributions expose sync-logs, commit-sync, and wrap-up as packaged skills so agents can synchronize logs, commit and push interrupted work, and close out MCP-backed work consistently across plugin families.
Scope: layer-1+

## FR-MCP-112 Requirements export description and AC formatting

Requirements exports shall render requirement descriptions as readable Markdown text and shall display structured acceptance criteria as a bulleted list instead of hiding or flattening AC into dense table cells.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Wiki exports render TEST requirement descriptions outside dense table cells so long descriptions remain readable. (evidence: RequirementsDocumentServiceTests.GenerateWikiAsync_RendersTestingRequirementsAsGroupedSectionsWithAcceptanceCriteria asserts per-TEST headings and paragraphs.)
- [x] Wiki exports render structured acceptance criteria under an Acceptance Criteria label as Markdown checklist bullets. (evidence: The same focused test asserts unchecked and checked AC bullets, including evidence text.)
- [x] Azure and GitHub wiki exports use the same requirement description and AC formatting. (evidence: The focused service test asserts GitHub and Azure Testing-Requirements.md output are equal.)

## FR-MCP-113 Plugin requirement batch payload parsing

The system SHALL accept valid YAML and JSON records arrays for requirement batch operations while preserving nested acceptance criteria.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] workflow.requirements.updateFrBatch accepts unindented records YAML and preserves nested acceptance criteria.

## FR-MCP-114 Parent TODO completion isolation

The system SHALL keep a TODO item's top-level done state independent from nested implementation task done state. Updating implementationTasks[].done SHALL NOT mark the parent TODO done unless the update request explicitly sets the parent done field or the TODO completion workflow deliberately satisfies the parent completion criteria.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Updating a nested implementation task done value does not include or change the top-level TODO done field when the caller did not explicitly set it. (evidence: Focused Bats test TODO HTTP body does not promote implementation task done to parent done passed in all five Bash plugin repos.)
- [x] A live TODO update can mark one implementation task done while the parent TODO remains not done. (evidence: Temporary live TODO TEMP-ISSUE39-001 remained done=false after updating one implementation task to done=true, then was deleted.)

## FR-MCP-115 Session and compaction hook output schema compliance

MCP plugin session and compaction hooks SHALL emit schema-valid hook outputs for their event type. Status-only SessionStart, SessionEnd, PreCompact, and PostCompact outcomes SHALL emit an empty JSON object; hooks that emit hookSpecificOutput SHALL include the matching hookEventName and only fields supported by that event. PostCompact SHALL NOT emit additionalContext because that event cannot inject context.
Scope: layer-1+
**Acceptance Criteria:**
- [x] SessionStart, SessionEnd, PreCompact, and PostCompact status-only hook scripts emit {} instead of non-spec hookSpecificOutput status payloads. (evidence: Hook Bats suites assert exact {} output for the affected session and compact hooks.)
- [x] PostCompact does not emit additionalContext. (evidence: Hook Bats suites assert post-compact output is {} and does not contain additionalContext.)

## FR-MCP-116 GitHub CLI service-account ownership resilience

The system SHALL allow GitHub-backed TODO and issue operations to run under service accounts even when Git rejects the workspace repository as dubious ownership, without requiring an untracked global Git configuration mutation.
Scope: layer-1+

## FR-MCP-117 Codex plugin wrapper bounded execution

The Codex MCP plugin wrapper SHALL return successful workflow responses promptly or fail within a caller-controlled bounded timeout with actionable diagnostics instead of forcing raw REST fallback.
Scope: layer-1+

## FR-MCP-118 Keyserver trust service

QBrainAi SHALL provide keyserver trust services that register trusted parties, manage public key metadata, sign transaction manifests, verify signed manifests, reject invalid or replayed manifests, preserve historical signing descriptors for verification, and persist audit evidence without exposing private key material.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Publisher, subscriber, arbiter, and future model parties can be provisioned with party ID, role, active signing key ID, active encryption key ID, status, created/updated UTC, and audit metadata.
- [x] Manifest signing binds transaction ID, turn ID, publisher party, subscriber party, key IDs, sequence, nonce, issued UTC, expiry UTC, algorithms, diffgram SHA-256, and encrypted body SHA-256.
- [x] Manifest verification succeeds for an unchanged canonical payload and valid keyserver signature and rejects unknown parties, disabled parties, unknown keys, disabled keys, expired manifests, replayed nonces, stale sequences, malformed signatures, and hash mismatches with deterministic reason codes.
- [x] Keyserver signing never returns, serializes, or logs private key material; external signing private PEM can be provisioned from file-backed startup configuration.
- [x] Key rotation supports a new active signing key while retaining old public descriptors for historic verification; full lifecycle automation beyond file-backed startup provisioning is deferred and explicit future scope.

## FR-MCP-119 Subscriber diffgram commit service

QBrainAi SHALL provide subscriber services that verify keyserver-signed manifests, decrypt intended diffgrams, validate encrypted and plaintext hashes, durably commit accepted diffgrams, expose abort/status flows, and reject invalid or conflicting commits.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Subscriber accepts a commit only when manifest verification succeeds against keyserver trust and the protected payload is addressed to the subscriber party/key ID.
- [x] Subscriber validates encrypted body SHA-256 before decrypt and plaintext diffgram SHA-256 after decrypt.
- [x] First valid commit persists transaction ID, diffgram ID, manifest hash, sequence, status, committed UTC, reason details, and audit metadata durably.
- [x] Duplicate commit with identical transaction/diffgram/hash is idempotent and returns committed; duplicate commit with mismatched payload, manifest, hash, or sequence is rejected as conflict.
- [x] Abort records aborted status, refuses later commit, and status exposes pending, committed, rejected, aborted, and reason details without exposing secrets.
- [x] Subscriber encryption private key rings decrypt old and rotated protected envelopes and bind separate-host configuration to the configured subscriber key material.

## FR-MCP-120 MCP Server transaction gating

QBrainAi SHALL gate QuadBrain/brain-slot mutating paths behind transaction manifest signing and subscriber commit confirmation before returning committed success, or fail closed before uncompensated side effects when a safe compensation boundary is not yet available. FR-MCP-173 carves out every non-QuadBrain first-party mutation (TODO, requirements, session-log for every source type including QBAgent, memory, repo, tools, GitHub, GraphRAG, voice, agent pool, REPL TODO workflow, requirements ingest, context rebuild, and federation apply/control): those persist without keyserver/coordinator even when turn transactions are enabled. QuadBrain brain-slot.invoke and brain-slot.weight-update remain in this gate.
Scope: layer-1+
**Acceptance Criteria:**
- [x] QuadBrain brain-slot.invoke and brain-slot.weight-update either route through compensation-capable coordinator gates or fail closed while required transactions are active. All other first-party mutations (federation apply, memory, TODO, repository write, prompt templates, TODO execution, requirements repository/export/ingest, session-log for every source type including QBAgent, tool registry, voice, agent-pool, GraphRAG, GitHub, context rebuild/website/sync, and federation control-plane) are governed by FR-MCP-173 and SHALL persist without coordinator/keyserver even when Mcp:TurnTransactions:Enabled=true.
- [x] QuadBrain coordinator ExecuteAsync rollback restores mutation snapshots when a brain-slot transaction fails after mutation. TransactionGatedMemoryService general-agent mutations are FR-MCP-173 and do not use coordinator compensation.
- [x] QuadBrain coordinator ExecuteAsync paths retain TODO compensation. TransactionGated TODO adapters are FR-MCP-173. ISSUE-backed GitHub side-effect compensation remains future scope for QuadBrain-gated paths.
- [x] Session-log writes including QBAgent are governed by FR-MCP-173 and persist without coordinator/keyserver. Workspace-stamp repair remains fail-closed while required transactions are active because it is not compensated.
- [x] Generic REPL client passthrough blocks unsafe protected namespaces and unclassified client namespaces while required transaction gating is active for QuadBrain operations, while explicitly service-gated namespaces remain routed to server-side gates that apply FR-MCP-173 for non-QuadBrain mutations.
- [x] Complete provider-level isolation across delayed subscriber rejection and bucket/GitHub whole-orchestration compensation remain documented future enhancements, not committed-success claims for this slice.

## FR-MCP-121 Degraded mode and rollback

QBrainAi SHALL enter explicit degraded mode when transaction dependencies are unavailable or commit cannot complete, allow only safe reads or explicitly fail-closed operations, and preserve audit records through rollback.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Coordinator status exposes enabled/degraded state, last reason, message, and transaction dependency health.
- [x] Required transaction gates reject mutation before side effects when the coordinator is degraded or required transaction dependencies cannot sign or commit.
- [x] Commit failure invokes available rollback compensation and reports rollback success or rollback failure without deleting audit evidence.
- [x] Durable pending commit handoffs are canceled after successful rollback compensation or commit timeout settlement.
- [x] Runtime/control-plane endpoints without complete remote compensation fail closed or remain explicitly deferred until future compensation slices.

## FR-MCP-122 Byrd v4 execution control

The transactional diffgram implementation SHALL follow Byrd Development Process v4 with requirements-first, failing-test-first, mocks-first, refactor, explicit validation, and zero-failure zero-skip validation gates for each executed scope.
Scope: layer-1+
**Acceptance Criteria:**
- [x] The plan records FR/TR/TEST identifiers and mappings before implementation closeout.
- [x] Each implementation slice names focused tests and validates zero failures and zero skips for the executed scope.
- [x] Deferred work is tracked as TODO/requirements state instead of skipped test placeholders.
- [x] Final closeout validation includes focused transaction tests, full affected unit suites, traceability validation, and solution build evidence.

## FR-MCP-123 Quad-model future scaffolding

The transaction foundation SHALL preserve the imported quad-model architecture while keeping Curiosity execution, AoT reconciliation execution, quad-model orchestration, and weight-update execution disabled by default until later requirements authorize them.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Imported diagrams retain stable IDs and future branch annotations for Curiosity, AoT reconciliation, weight redistribution, and quad orchestration.
- [x] Current production code implements transaction security and mutation gating only; direct model execution and weight-update execution remain disabled/deferred.
- [x] Deferred future scope is explicitly documented in the plan, mutation audit, design rounds, TEST-MCP-163, and TEST-MCP-170.

## FR-MCP-124 Imported document and diagram preservation

The imported Quad-Model transactional diffgram document SHALL be preserved as local project-contract artifacts, including its diagrams, branch IDs, source references, and implementation-scope annotations.
Scope: layer-1+
**Acceptance Criteria:**
- [x] All six imported Mermaid diagram IDs are present in `docs/Project/Quad-Model-Transactional-Diffgram-Plan.md`.
- [x] Each imported diagram section includes a Mermaid block, imported source-section reference, and repository annotations.
- [x] Diagram-derived implemented and deferred branches map to TEST-MCP-165 through TEST-MCP-173.

## FR-MCP-125 Legacy federation compatibility without general keyserver gating

Keep legacy HMAC envelope compatibility separate from transaction signing and protected diffgram encryption. Under the later FR-MCP-173 decision, general federation apply/control and audit do not invoke the QuadBrain coordinator or fail closed merely because it is degraded. Actual QuadBrain operations retain their coordinator rules.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given legacy signed envelopes, when explicit legacy-mode tests run, then compatibility remains intact.
- [ ] Given degraded QuadBrain coordinator, when authorized general federation control or audit runs, then no keyserver call occurs and the operation is not blocked by coordinator state.

## FR-MCP-126 aiUnit plan review

The transaction plan SHALL include a test-only aiUnit plan review gate that fails on critical or high findings and records run-log evidence before closeout.
Scope: layer-1+
**Acceptance Criteria:**
- [x] `tests/McpServer.PlanReview.Tests` validates aiUnit plan-review run-log evidence for PLAN-TURNTRANSACTIONS-001.
- [x] The gate fails when run-log findings contain critical or high severity findings.
- [x] The run-log artifact records prompt scope, provider/model metadata, findings status, and reviewed scope.
- [x] Explicit future-disabled work is treated as non-blocking only when it does not hide a safety, correctness, or validation gap.

## FR-MCP-127 Diagram-derived implementation tests

In-scope branches from imported activity and sequence diagrams SHALL have implementation-test coverage or an explicit deferred requirement before related code is written.
Scope: layer-1+
**Acceptance Criteria:**
- [x] SD-DIFFGRAM-001 valid, invalid, signing, verification, encryption, and hash branches are covered by keyserver/subscriber/client/integration tests.
- [x] AD-TXN-001 commit, subscriber unavailable, degraded, rollback, timeout, and audit branches are covered by coordinator, pub-sub, federation, and mutation-gating tests.
- [x] Deferred diagram branches for Curiosity, AoT, weight redistribution, and full quad execution are explicitly documented as disabled future scope.
- [x] TEST-MCP-165 through TEST-MCP-173 preserve diagram IDs, branch IDs, design mappings, and traceability closeout evidence.

## FR-MCP-128 Two-round architecture and design review

The implementation SHALL include two architecture/design rounds before closeout: a first architecture round defining boundaries, ownership, trust, storage, threat model, and gap analysis, and a second implementable design round defining DTOs, entities, options, interfaces, endpoint contracts, reason codes, audit payloads, XMLDoc obligations, and test mappings.
Scope: layer-1+
**Acceptance Criteria:**
- [x] `TurnTransactions-Architecture-Round1.md` captures component boundaries, trust model, storage boundaries, rollback/audit rules, threat model, and gap analysis.
- [x] `TurnTransactions-Design-Round2.md` captures implementable DTOs, entities, options, interfaces, endpoint contracts, reason codes, canonicalization, audit payloads, XMLDoc obligations, and TEST-MCP-158 through TEST-MCP-173 mappings.
- [x] `TurnTransactions-Mutation-Endpoint-Audit.md` captures current gated, fail-closed, and explicitly deferred mutation surfaces.
- [x] Requirements matrix and FR/TR mapping rows link FR-MCP-118 through FR-MCP-128 to transaction TR and TEST records.

## FR-MCP-129 Durable external brain-slot registry and live invocation

The MCP runtime SHALL provide durable workspace-scoped external brain-slot definitions for LeftHemisphere, RightHemisphere, CuriosityEngine, and ArbiterOfTruth, including CRUD, readiness status projection, and individually gated live model invocation.
Scope: layer-1+
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
Scope: layer-1+
**Acceptance Criteria:**
- [x] No model output is returned to the caller until the subscriber commit succeeds.
- [x] If commit fails, times out, or degrades, output is discarded from the response and is not injected into cache or GraphRAG.
- [x] Only CuriosityEngine may request GraphRAG/context admission.
- [x] Left, Right, and Arbiter invocations may return committed results but never mutate cache or GraphRAG.
- [x] Curiosity admission records the committed transaction and admission metadata for audit.

## FR-MCP-131 Quad containment and authorization boundary

The MCP runtime SHALL keep Quad-Brain branches fail-closed unless explicit branch requirements authorize them. FR-MCP-134 and FR-MCP-135 now authorize AoT reconciliation execution, durable weight updates, and full Quad-Brain orchestration; unauthorized cache mutation and implicit fallback remain contained.
Scope: layer-1+
**Acceptance Criteria:**
- [x] AoT reconciliation execution is authorized only through the transaction-gated ArbiterOfTruth slot path.
- [x] Weight update execution is authorized only through the safety-gated, transaction-coordinated weight update path.
- [x] Full automatic quad orchestration is authorized only when the workspace is quad-ready and all role outputs commit successfully.
- [x] Individual brain-slot invocation authorization still does not authorize non-Curiosity GraphRAG/cache mutation or implicit fallback model behavior.

## FR-MCP-132 Accurate HTTP semantics for /qbrainai/* credential failures

All /qbrainai/* endpoints return 401 Unauthorized for unknown, stale, or missing API keys. 503 Service Unavailable is returned only when the per-workspace auth-token subsystem has not yet been initialized, and per-workspace tokens rotating on restart must surface as 401, never as a blanket 503.
Scope: layer-1+
**Acceptance Criteria:**
- [x] All /mcpserver/* endpoints return 401 Unauthorized for unknown, stale, or missing API key
- [x] 503 Service Unavailable returned only when per-workspace auth-token subsystem not initialized
- [x] Every 503 includes Retry-After header and JSON body
- [x] Rotated/stale API key surfaces as 401, never as blanket 503

## FR-MCP-133 Readiness reflects the workspace/token subsystem

The readiness endpoint /ready reports Unhealthy when no enabled workspace is registered, primary workspace has no seeded auth token, or auth-token subsystem is uninitialized. Readiness never reports Healthy while /qbrainai/* would be rejected with 503.
Scope: layer-1+
**Acceptance Criteria:**
- [x] /ready endpoint checks workspace/token subsystem readiness
- [x] /ready reports Unhealthy when no enabled workspace registered
- [x] /ready reports Unhealthy when primary workspace has no seeded auth token
- [x] /ready never reports Healthy while /mcpserver/* would return 503

## FR-MCP-134 Full Quad-Brain orchestration and AoT reconciliation

The MCP runtime SHALL execute the full four-role Quad-Brain decision loop when the workspace is quad-ready, including LeftHemisphere, RightHemisphere, CuriosityEngine, and ArbiterOfTruth invocation, transaction-gated AoT reconciliation, committed final output return, and fail-closed rejection when any required slot, transaction, endpoint, credential, or party gate is unavailable.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Orchestration rejects before provider calls unless all four roles have exactly one enabled, valid, trusted, transaction-ready slot.
- [x] LeftHemisphere, RightHemisphere, and CuriosityEngine outputs are collected through existing transaction-gated slot invocation before ArbiterOfTruth reconciliation runs.
- [x] AoT reconciliation returns the final committed decision only after subscriber commit; failed or degraded commits discard model output from the caller response.
- [x] No implicit fallback model is used; fallback remains explicit fail-closed unless a configured slot is invoked and committed.

## FR-MCP-135 Quad-Brain weight update controls

The MCP runtime SHALL support durable, audited Quad-Brain role weight updates with AoT approval, admin approval, explicit safety-gate acknowledgement, version checks, before/after snapshots, and rollback metadata, while rejecting unsafe or stale updates without mutating slot state.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Weight updates require AoT approval, admin approval, safety gates passed, and a non-empty reason before slot weights change.
- [x] Each accepted update increments slot weight versions and records previous and new values in DataAuditLogs.
- [x] Stale expected versions, missing roles, disabled slots, invalid weights, or missing approvals return structured fail-closed reason codes.
- [x] Full orchestration can apply an explicit approved weight update but never performs an implicit self-improvement update.

## FR-MCP-136 ACID-coupled Microsoft Agent Framework agent definition

The system SHALL expose an explicit ACID-compliant, tightly coupled hosted-agent definition for Microsoft Agent Framework hosts, extending the existing ChatClientAgent integration while requiring authenticated workspace binding, serialized tool invocation, durable audit/session-log boundaries, and fail-closed tool exposure.
Scope: layer-1+
**Acceptance Criteria:**
- [x] The ACID profile has stable public metadata including id, name, source type, consistency model, coupling mode, and Microsoft Agent Framework extension type.
- [x] The ACID profile requires authentication, workspace scoping, session-turn boundaries, durable audit, transaction-required mutation policy, and serialized model tool invocation.
- [x] Generic passthrough, shell, desktop, repository-write, TODO-mutation, and GraphRAG-mutation tools are not exposed by default in the ACID profile.
- [x] Default hosted-agent registration remains backward compatible when the ACID profile is not selected.

## FR-MCP-137 Quad Brain coding agent execution

RETIRED 2026-07-20 by FR-MCP-142 and TR-MCP-QB-001. This requirement specified a hosted coding agent that reached QuadBrain orchestration through the mcp_quadbrain_coding_execute tool in the shared QBrainAi.McpAgent catalog. That tool is removed, because it advertised a QuadBrain capability to every hosted-agent host rather than to QBAgent alone, and it was already non-functional for QBAgent: QuadBrainInternalToolExecutor never had a case for it, so the interceptor classified it internal and returned Fail. Retired alongside TR-MCP-AGENT-016 and TEST-MCP-187. Replacement coverage is the absence suite under TEST-MCP-193. QuadBrain coding work continues server-side through POST /v1/chat/completions, the OpenAI-compatible endpoint QBAgent binds to with model id QuadBrain; that path is unchanged and separately covered. Corrected 2026-07-20 after an audit found this FR still describing removed behavior when its TR and TEST had already been retired.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Hosted agent exposes model-visible mcp_quadbrain_coding_execute tool
- [x] Tool calls McpServerClient.BrainSlots.OrchestrateAsync with coding metadata
- [x] Tool never bypasses Quad Brain transaction/admission contract
- [x] ACID profile includes Quad Brain coding tool while excluding generic passthrough, shell, write, desktop, todo, and graphrag mutation tools
- [x] Host code can execute same coding-agent path through typed runtime method

## FR-MCP-138 AiCodeReview and AiProjectReview Nuke targets trigger aiUnit via library

AiCodeReview and AiProjectReview Nuke targets SHALL integrate directly with the aiUnit Frontier library to trigger the AI review (code or project reviewType) from inside the target execution. This replaces any reliance on externally pre-run AI sessions or static artifacts.

**Acceptance Criteria:**
- Execute `dotnet build` of the _build project succeeds with the integration.
- Invoking the target (e.g. ./build.ps1 AiCodeReview) results in real SendAsync call to a Frontier client resolved from config.
- Produces a fresh aiunit.review.runlog.v1 JSON under artifacts/aiunit-code-review/ (and project equivalent).
- Produces a matching combined MD under docs/reviews/ containing the prompt and the findings JSON.
- Configuration loaded from appsettings.aiunit.json (falling back to the one in PlanReview.Tests) + env vars; uses ActiveStrategy (e.g. claude CLI).
- The targets do not depend on or execute unit test filters for review triggering (review is distinct from `Test`).
- Build tests cover client creation and markdown writing with mocks/stubs.
- All prior tests + new remain green (Byrd gate).
Scope: layer-1+

## FR-MCP-139 Warning suppression governance

The MCP Server workspace must maintain durable governance for warning suppression decisions, approved exceptions, required code fixes, validation evidence, and traceability across TODO and requirements artifacts.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Approved warning suppressions are recorded with diagnostic code, justification, owner, permanence, and review condition. (evidence: config/warning-suppression-approvals.json records current approved CA1416, CA1819, and CA2227 suppressions with owner, permanence, and review condition.)
- [ ] Unapproved diagnostics and broad warning bypasses remain remediation work until code, configuration, package metadata, or dependency changes remove the warning source. (evidence: REOPENED 2026-09-09T17:07:12Z: generated migration CS0612/CS0618 pragmas recurred (W18 open). Historical evidence retained. .editorconfig CA1848 severity override removed; CA1848 and CS8602 approval entries removed; CS8602 pragmas removed; Storage migration obsolete pragmas removed.)
- [ ] PLAN-WARNREMEDIATION-001 stays current with approved suppressions separated from required fixes and marks only validated work as done. (evidence: REOPENED 2026-09-09T17:07:12Z: PLAN-WARNREMEDIATION-001 W18 is Done=false. Historical evidence retained. PLAN-WARNREMEDIATION-001 W15/W18/W21-W24 evidence updated in TODO state; approved suppressions remain in config/warning-suppression-approvals.json.)
- [x] Requirements exports and traceability mappings include the suppression governance FR, TR, and aiUnit TEST records. (evidence: docs/Project/Functional-Requirements.md, docs/Project/Technical-Requirements.md, docs/Project/Testing-Requirements.md, docs/Project/TR-per-FR-Mapping.md, docs/Project/Requirements-Matrix.md, docs/Project/requirements-wiki-documents.zip)
- [ ] Generated provider migration designers and snapshots may not retain exact CS0612/CS0618 suppression pairs after scaffolding. Correction uses an explicit opt-in Nuke normalizer. Read-only validation must fail when those exact pragma pairs reappear. Do not create a parallel requirement family.

## FR-MCP-140 Self-describing marker signature canonicalization

The generated AGENTS-README-FIRST.yaml marker file shall embed the complete marker-v1 canonical field list and encoding contract inside the signature block so that any agent can reconstruct and verify the HMAC-SHA256 signature without consulting server source code, documentation, or a helper module. Recovered from origin/claude/dreamy-brahmagupta (commit ccda0a4e, 2026-04-09) where it was authored as FR-MCP-081; that id has since been reused on develop for Byrd Iteration Phase and TODO Execution Persistence, so this work is renumbered. Extends FR-MCP-076 (marker trust bootstrap) and TR-MCP-SEC-003 (signed marker bootstrap), neither of which states that the payload field list is emitted or that it is table-driven. Motivation: the marker-v1 field order is currently hand-reimplemented in at least six independent verifiers (McpSession.psm1, plugins lib-ps and lib-sh marker resolvers, the lib-node and mcp-repl-ts TypeScript resolvers, mcpserver-agent-core marker-trust.ts, MarkerFileClientOptionsResolver.cs) plus a prose spec in REPL-AGENT-GUIDE.md, with no test binding them together; a missed update makes a verifier compute a different HMAC and log MCP_UNTRUSTED.
Scope: layer-1+

## FR-MCP-141 Service-account process environment fidelity

When the server runs as a Windows service under a non-interactive account, child processes it launches shall resolve executables and environment the same way an interactive user session would. Executable resolution shall skip Microsoft\WindowsApps App-Execution-Alias stubs, which are zero-byte reparse points that fail with Win32Exception 1920 for service accounts. Tunnel providers shall apply the interactive user's USERPROFILE, HOME, APPDATA, and PATH to the child process so provider configuration files resolve, and shall resolve the provider binary against that enriched PATH. A missing provider binary shall be reported at Warning with a message naming the service-account and Store-alias causes and the available remedies, not as an unexplained error. Recovered from origin/claude/busy-dubinsky (2026-04-04 to 2026-04-05), which registered no requirement of any kind.
Scope: layer-1+

## FR-MCP-142 QuadBrain is absent from every general agent surface

No QuadBrain capability shall be exposed to general agents or to the agent plugins in any form. Absent, not gated: no brain-slot tool descriptors in the shared plugin cores, none advertised over the server MCP transports, no named client-passthrough route reaching BrainSlotClient, no QuadBrain-named tool in the shared hosted-agent catalog, and no brain-slot descriptor files in the shared mcps registry. QuadBrain remains reachable only as the OpenAI-compatible model at POST /v1/chat/completions, workspace-token authorized, which is the sole path QBAgent uses. Everything behind that endpoint stays server-side: QuadBrainOpenAiController, QuadBrainOpenAiChatService, QuadBrainOrchestrationService, the BrainSlot registry, invocation, credential, chat-client-factory and startup-seeder services, the BrainSlot storage entities and migrations, the QuadBrain tool interceptor, and config/brain-slots/quad-brain-slot-assignments.yaml. Evidence that motivated this requirement: the ClaudeCode session of 2026-07-20 could enumerate all eleven brain_slot tools from a general agent tool list, and plugins/core/lib-node/src/runtime/host-context.ts spread brainSlotTools into the ungated allToolDescriptors catalog advertised to every Node host plugin.
Scope: layer-1+

## FR-MCP-143 Session-log turns persist without a title and never lose a refined one

A session-log turn shall persist end to end when its title is deliberately omitted, and a title the agent has refined shall never be overwritten by a stale local copy. Thirteen open triage reports (BUG-TRIAGE-086 through 101, excluding the already-closed 097) collapse into five root causes, of which two account for eleven of the reports. First, the plugin PowerShell core rejects an empty title at parameter binding even though TR-MCP-REPL-015 requires callers to omit the title so a stale cached one is not resubmitted, so appendDialog, appendActions, completeTurn and the supersede path all fail. Second, the typed client posts anonymous request bodies through a source-generated JSON context that has no metadata for compiler-generated types, so the dedicated retitle endpoints throw before reaching the server. The remaining three are the supersede path clobbering a server-refined title from the local cache, failTurn refusing to close a turn the plugin cache shows as active, and a storage outage leaving health reporting Healthy while every persistence call fails behind inconsistent error shapes.
Scope: layer-1+

## FR-MCP-170 Incremental session-log dialog persist

Plugin workflow.sessionlog.appendDialog SHALL persist dialog items through SessionLogClient.AppendDialogAsync (POST /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/dialog) without a full-session SubmitAsync upsert when the turn already exists.

Acceptance Criteria:
- After a successful beginTurn, appendDialog returns success while TODO/requirements queries succeed in the same process.
- Server turn GET then contains the appended dialog items.
- Test double or recorded call: method is AppendDialogAsync or POST dialog, not SubmitAsync of the full session DTO.
- A missing turn on a never-degraded appendDialog path is classified not-found, retryable false; no failsafe is created for that 404. This does not override degraded MCP/service recovery and failsafe retention governed by FR-MCP-SESSIONLIFE-001-AC004.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] After a successful beginTurn, appendDialog persists valid dialog items and exact server readback shows the appended items while TODO and requirements operations remain usable in the same process.
- [ ] The incremental dialog path invokes SessionLogClient.AppendDialogAsync or the dialog POST and does not submit the complete session DTO through SubmitAsync.
- [ ] On a never-degraded appendDialog path, a missing turn is classified not_found with retryable false, does not create a failsafe for that 404, and does not increment the local auditDialog count; this criterion does not override degraded MCP/service recovery and failsafe retention governed by FR-MCP-SESSIONLIFE-001-AC004.
- [ ] A classified storage failure leaves local auditDialog unchanged; a later successful complete reconciles the local counter to the exact persisted server turn.

## FR-MCP-171 Retryable session-log persist degrade-queue

When client.SessionLog.SubmitAsync (beginTurn, updateTurn, completeTurn, or legacy upsert) returns timeout, HTTP 503, or backend_unavailable, the plugin SHALL retain the session_submit failsafe, keep current-turn.yaml in_progress, SHALL NOT throw, and SHALL report degraded/queued using the same contract as the existing timeout path.

Acceptance Criteria:
- HTTP 503 backend_unavailable and timeout produce the same degrade-queue contract (persisted false, degraded true, queued true, failsafe retained).
- Failsafe file remains until a later successful persist or drain replay.
- After storage recovers, query shows the turn.
Scope: layer-1+

## FR-MCP-172 Failsafe drain does not poison successful plugin calls

A successful workflow.requirements.getFr, getTr, or sessionlog queryHistory SHALL NOT be delayed by a 30s SessionLog.SubmitAsync drain timeout, SHALL NOT print Failsafe queue drain failed for timeout or HTTP 503, SHALL NOT latch drain completed, and a later drain after storage answers SHALL replay the queued record.

Acceptance Criteria:
- getFr EXIT 0 with the FR body when a queued session_submit times out or returns 503.
- stderr has no Failsafe queue drain failed for that class.
- ReplFailsafeDrainCompleted stays false on abort.
- Next drain in-process replays after a stubbed success.
Scope: layer-1+
**Acceptance Criteria:**
- [x] getFr EXIT 0 with the FR body when a queued session_submit times out or returns 503. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] stderr has no Failsafe queue drain failed for that class. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] ReplFailsafeDrainCompleted stays false on abort. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Next drain in-process replays after a stubbed success. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G0/G8 Codex extra-high AGREE SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

## FR-MCP-173 Keyserver signs QuadBrain transactions only

Keyserver manifest signing SHALL be used only for QuadBrain transactions: publisher party id prefix brain-slot:, or operation name prefix brain-slot. or quadbrain. All other first-party mutations (TODO, requirements, session-log for every source type including QBAgent, memory, repo, prompt templates, tool registry, GitHub, GraphRAG, voice, agent pool, issue-todo-sync, tool buckets, REPL TODO workflow, requirements ingest, context rebuild, and federation apply/control) SHALL persist without calling ITurnTransactionCoordinator or the keyserver even when Mcp:TurnTransactions:Enabled=true and RequiredForMutations=true. Do not disable the live TurnTransactions flag as the fix. QuadBrain brain-slot.invoke and brain-slot.weight-update remain coordinator-gated and may call keyserver.

The canonical structured criterion identities are ac-fr173-001 through ac-fr173-005 below.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] With TurnTransactions.Enabled=true and RequiredForMutations=true, general-agent mutations (todo.update, requirements.fr.update, sessionlog.submit for GrokCode and QBAgent, memory.add, repo.write, github.cli) do not invoke ITurnTransactionCoordinator.ExecuteAsync and still succeed.
- [ ] brain-slot.invoke and brain-slot.weight-update still invoke the coordinator and may call SignManifestAsync.
- [ ] Coordinator degraded state does not block general-agent first-party mutations.
- [ ] Live TurnTransactions.Enabled remains true for QuadBrain; the repo default may stay false.
- [ ] Focused TransactionGated* tests plus TurnTransactionCoordinatorTests, TurnTransactionKeyserverScopeTests, and BrainSlotInvocationTransactionTests complete with zero failures and zero skips.

## FR-MCP-174 autonomous local execution

Every node, including the hub, performs authorized local business operations and persists results without hub connectivity.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given A/main=C0 and the hub link is down, when A changes TODO T1.title from old to local, then the local API succeeds, a local reread returns local, and no hub request or history commit occurs.
- [ ] Given H/main=C0 and the gateway is stopped, when H edits T1.title, then H persists it locally while remote/main remains C0.
- [ ] Given the prior offline edit, when the application and Dolt restart, then T1.title remains local and main remains C0 until an explicit version commit.

## FR-MCP-175 explicit version commits and publication boundary

Only an explicit version-history commit triggers publication; drafts and SQL saves remain local. Commit retries recover the same history identity.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given main=C0 and remote=C0, when SaveChanges persists T1.title=draft, then main and remote stay C0 and no publication intent is emitted.
- [ ] Given that draft and a valid preview, when commit request R1 succeeds, then main=A1, the reply says committed_locally, and one recoverable publication intent targets A1.
- [ ] Given B has a draft and desired head A1, when B polls, then outcome=waiting_local_work, B/main=C0 and the draft value is unchanged.
- [ ] Given A1 committed while disconnected and a crash before intent persistence, when A restarts, then history scanning rediscovers A1 exactly once and publishes it after reconnect.

## FR-MCP-176 logical project enrollment and initial clone

Nodes enroll a logical project under immutable shared identity while retaining host-local paths and refusing destructive clones.

Server: project discovery, scoped enrollment and structured refusal diagnostics. Plugin: project discovery/selection, enrollment progress and explicit actionable diagnostics.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given LAB path /home/sharpninja/github/McpServer and LEGION path F:/github/McpServer, when both enroll as ProjectId Atlas, then both reference Atlas and retain their distinct local paths.
- [ ] Given target has TODO X, when clone is requested, then409 target_not_empty leaves X and all target files unchanged.
- [ ] Given node A has only Atlas membership, when it enrolls or clones SecretProject, then403 unauthorized_project returns no rows, objects or project existence details.
- [ ] Given Atlas is discoverable but A has a nonempty destination, when each plugin selects Atlas and attempts enrollment/clone, then it displays ProjectId, selected branch and409 target_not_empty with remediation; no database is overwritten and no different project is selected.

## FR-MCP-177 branches across server and plugin context

First-release project branches support isolated work, explicit sharing and reviewed merge; session selection never leaks between connections.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given main T1.title=main and feature/search T1.title=feature, when alternating pooled reads run100 times, then each returns only its selected value.
- [ ] Given main=C0 and feature/search=F1, when F1 publishes, then remote feature=F1 and all main heads remain C0.
- [ ] Given a preview for main=C0 and feature=F1 but feature advances to F2, when merge submits the old preview, then409 head_changed creates no commit and changes no ref.

## FR-MCP-178 bidirectional committed-history exchange

Exchange committed history bidirectionally through guarded publication, preserving every accepted ancestor and the hub independent working copy.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given C0 contains T1.title=old,T1.description=old,T2.priority=low, when A1 changes title=A, B1 changes description=B and H1 changes T2.priority=high, then merges M1(A1,B1), M2(M1,H1) converge all heads to M2 with values A,B,high and all three commits in ancestry.
- [ ] Given H has an uncommitted title draft at C0, when A publishes A1, then remote=A1, H remains C0 with its draft, and B can accept A1.
- [ ] Given A1 and B1 descend from C0, when both accept requests expect C0, then one succeeds and the other receives409 head_changed; after merge M1 publishes, both A1 and B1 remain ancestors.

## FR-MCP-179 hub control table and polling

Use a durable hub subscription table with polling, generation and worker fencing, plus a fresh hub epoch after restore.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given desired=7 and reported=6, when poll runs twice, then both responses request7 and stored reported remains6.
- [ ] Given desired becomes8 while report7 is in flight, when report7 arrives, then reported=7, desired=8 and the subscription remains pending.
- [ ] Given remote=A1 but control still names C0 after a crash, when reconciliation runs, then it records A1 and advances desired generation without producing another commit.
- [ ] Given a node remembers epoch E1 generation90 and the hub restores a backup at generation40, when the hub reopens, then epoch E2 differs from E1; E1 reports fail409 hub_epoch_changed and no head is force-reset. Node A1 is retained until verified ancestry recovery republishes missing history.

## FR-MCP-180 reviewed conflicts and valid merges

Conflicts and invalid merges preserve valid live data, require authorized review and remain visible with ageing while unrelated publication continues.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given base priority=low, A1 priority=high and B1 priority=medium, when B integrates A1, then needs_review shows low/medium/high, B/main remains B1 and no public ref is overwritten.
- [ ] Given A1 deletes T1 and B1 edits T1.title, when histories merge, then needs_review retains B1 and the deleted/edited alternatives for review.
- [ ] Given separate commits add T1 dependsOn T2 and T2 dependsOn T1, when Dolt merges without cell conflicts, then domain validation records dependency_cycle and leaves the prior valid live head unchanged.
- [ ] Given a resolution preview binds local=B1 and incoming=A1, when incoming advances to A2, then resolve returns409 stale_review with no merge commit.
- [ ] Given unresolved needs_review at12:00, when the clock reaches12:15, then plugins/status and the operator inbox show warning with ReviewId and age900s; at24h severity is critical, with at most one repeat per hour.
- [ ] Given Atlas/main is blocked by review R1, when Atlas/feature publishes F2 and another project publishes X1, then both succeed while Atlas/main remains unchanged.
- [ ] Given A changes title and B changes description with no domain violation, when merge runs, then both values survive; competing priority or completion edits produce needs_review instead of choosing a timestamp winner.

## FR-MCP-181 recovery without lost or repeated work

Recover synchronization, receipts and restored hub state without losing local work, inventing success or replaying business effects.

Server: durable operation resume/status/safe cancellation. Plugin: expose resume, status and cancel with completed-versus-pending effects; never claim cancellation reverses published history.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given remote accepted A1 and reply was lost, when request R1 retries, then it returns A1 and the existing receipt; no A2 commit is created.
- [ ] Given worker epoch changes4 to5, when epoch4 reports success, then409 stale_worker_epoch leaves current receipt and desired state unchanged.
- [ ] Given B/main=C0 with title=draft, when A1 is fetched, then outcome=waiting_local_work, main stays C0 and title remains draft; at15min status/inbox shows the age warning.
- [ ] Given B supports schema1 and incoming S1 requires schema2, when sync runs, then409 incompatible_schema leaves B's live data/head unchanged and records the required version.
- [ ] Given source and destination have different schema versions and an incoming change has a verified equivalent data effect on the destination, when synchronization applies that change, then the destination stores the intended typed values, preserves unrelated stored values and relationships, and advances the permitted ref with the expected ancestry and receipt. Applying the operation requires no schema migration or binary replacement solely to match version numbers.
- [ ] Given operation R1 published A1 but its local reply was lost, when a plugin resumes, reads status and requests cancellation, then it shows publishedHead=A1 and a durable cancellation outcome for remaining work; it never claims A1 was rolled back or creates a second commit.

## FR-MCP-182 collision-resistant and preserved identity

Prevent generated-ID collisions, preserve permanent legacy aliases, detect duplicate work and atomically reserve human-assigned IDs.

Server: provenance, scoped aliases, collision evidence and reservation outcomes. Plugin: expose provenance/collision diagnostics and the authorized resolution path without rewriting historical references.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given tags LA and LG and both counters start1, when both create a TODO, then generated IDs differ and both records survive reconciliation.
- [ ] Given LA allocated sequence42, when main reverts and another branch creates a record, then its sequence is greater than42.
- [ ] Given LAB and LEGION have different entities named TODO-7, when migration resolves the collision, then scoped aliases resolve each original identity and every relational reference follows its reviewed canonical mapping; historical text remains byte-identical.
- [ ] Given existing IMPL-UNITGREEN-001 and old TODO references, when imported and read by their original IDs, then they resolve through permanent aliases without rewriting commits, PR text, session logs, memories or documents.
- [ ] Given LAB TODO-7 and LEGION TODO-91 share normalized title Fix login, description and dependency set, when migration plan runs, then it emits a duplicate candidate with both IDs and evidence; until explicit review, neither is merged or deleted.
- [ ] Given human ID IMPL-UNITGREEN-001 is unreserved, when two nodes reserve it concurrently, then exactly one receives201 and the other409 human_id_reserved; retry by the winning requestId returns the same reservation.
- [ ] Given an offline-created provisional TODO requesting an unreserved human ID, when it reconnects and publishes, then409 human_id_unreserved retains the local record until reservation or reviewed alias resolution.
- [ ] Given LAB TODO-7 and LEGION TODO-91 are a duplicate candidate, when each plugin opens identity review, then both origin IDs, canonical/provisional IDs, similarity evidence and reservation outcome are visible; no alias or row changes before an authorized decision.

## FR-MCP-183 first-party plugin federation contract

Expose consistent typed federation, review, identity, audit, migration and maintenance workflows through every first-party plugin.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given one fixture for commit R1, conflict R2 and migration R3, when each installed first-party plugin calls its wrapper, then normalized IDs, heads, error codes and audit agent identity match the typed client result.
- [ ] Given server lacks federation contract1, when a plugin invokes commit, then unsupported_contract is shown and no raw SQL or shell fallback executes.
- [ ] Given a cached leaseFence4 and session branch feature, when reconnect discovers fence5, then the plugin retains visible context but refuses writes with lease_lost until reacquired.
- [ ] Given a branch observation is older than24h, when status is displayed, then stale=true and observedUtc are visible; the branch, drafts and refs are not deleted.

## FR-MCP-184 portable data versus host-local state

Separate versioned project content from operational state while delivering permanent audit to the hub independently of commits and excluding secrets.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given credentials and local machine paths exist in operational storage, when a project is cloned, then none appears in its versioned tables or history.
- [ ] Given A performs an offline TODO edit and creates audit E1, when connectivity returns without a Dolt commit, then hub audit contains exactly E1, A retains E1, and main/remote remain C0.
- [ ] Given E1 was stored but its receipt was lost, when the same batch retries, then exactly one E1 exists and the same hash receipt returns; changed payload under E1 returns409 audit_id_conflict.
- [ ] Given source has100 TODO audit and20 generic audit rows, when seed/join audit backfill verifies, then all120 records have matching payload hashes and hub receipts; missing one blocks cutover.
- [ ] Given an explicitly promoted sanitized session with turns, actions, dialog, commits/files, PlanFile and TodoId, when cloned, then every promoted child and relationship matches the source snapshot; automatic capture still uses the audit channel.
- [ ] Given accepted head changes C0 to A1, when the derived index rebuild completes, then queries read A1 values and no C0 cache is served under the A1 key.
- [ ] Given main=C0 is clean, when heartbeat, audit and automatic session capture run, then Dolt working-set fingerprint remains unchanged.
- [ ] Given agent1 owns the editing lease, when agent2 logs a session action, then its local audit persists without acquiring or changing agent1's lease.
- [ ] Given a promoted snapshot contains a fixture secret detected by the configured detector, when commit preview runs, then422 secret_detected creates no commit and outputs no secret value.
- [ ] Given unique fixture canaries are injected through protected secret references, when commands, retries and failures execute on both hosts, then scans of argv, receipts, logs, chat/audit payloads, memory stores, generated files, Git staged blobs/new commits and Dolt working sets/new history find zero canary values. Scanner diagnostics and rejected commit previews also contain zero values; no secret is added to history to test the detector.

## FR-MCP-185 scoped authorization and offline behavior

Reuse integrated IdentityServer for single-user node ownership, per-node client credentials, project scopes and revocation; preserve authorized offline work.

Server: least-privilege capabilities plus server-side authorization on every call. Plugin: show available rights and explicit denial; capability display never substitutes for authorization.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given A has a valid Atlas token, private-backend access across the lab link fails. A read-only token attempting a public-main mutation receives unauthorized_project. A publish-authorized token attempting a raw public-ref update outside publication.accept receives publication_required. HTTP endpoints return HTTP 403 and 409, respectively. Native remotesapi gRPC returns PermissionDenied and FailedPrecondition, respectively. All three cases leave remote/main at C0.
- [ ] Given a token has Atlas.read and lacks SecretProject scope, a remote fetch for SecretProject returns unauthorized_project and sends zero objects. HTTP endpoints return HTTP 403 and native remotesapi gRPC returns PermissionDenied.
- [ ] Given A holds a reference token, when IdentityServer disables A and revokes its grants before acceptance, then accept returns401 invalid_token and remote/main is unchanged.
- [ ] Given hub/IdentityServer is unreachable and a valid authorized local user, when a local TODO edit is requested, then it succeeds; an unauthorized local user still receives403.
- [ ] Given ordinary editing SQL credentials, when remote administrative procedures or public-ref writes are attempted, then permission is denied and no object publication occurs.
- [ ] Given IdentityServer revocation completes, new fetch and publish requests with the old token both return invalid_token before transfer or ref mutation. HTTP endpoints return HTTP 401 and native remotesapi gRPC returns Unauthenticated.
- [ ] Given both services run, when LAB probes10.42.0.143:3306 and LEGION probes10.42.0.214:3306, then both connections fail; local127.0.0.1:3306 health succeeds.
- [ ] Given the peer uses an unapproved SPKI or wrong SAN, when control/remote TLS connects, then tls_peer_untrusted occurs before bearer transmission.
- [ ] Given a plugin session has Atlas.read but lacks publish, when capabilities load and a publication is nevertheless attempted, then publish is unavailable in the capability display, the request returns403 unauthorized_project, the denial is explicit and no SQL/CLI fallback or head change occurs.

## FR-MCP-186 Dolt fourth-provider support and schema migration

Support Dolt as the fourth provider with dedicated schema migration, controlled schema-history distribution, Windows/Linux lifecycle and original-provider regressions.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given keys sqlite,postgresql,sqlserver,dolt, when runtime and design-time factories resolve them, then each selects its own strategy/migration history; unknown key returns unsupported_provider.
- [ ] Given empty standalone Dolt then baseline schema1, when baseline and additive schema2 apply, then CRUD succeeds, data remains intact and repeated apply makes no further changes.
- [ ] Given recorded hashes of the three original migration chains, when Dolt support is added, then those hashes match and all three real-provider upgrade fixtures pass.
- [ ] Given application and project schema versions differ, when a domain operation requires storage behavior that the current combination cannot verify or represent faithfully, then it returns incompatible_schema before mutation. A verified compatible operation on the same project schema can proceed and preserves exact stored data outside its intended changes.
- [ ] Given a versioned project at schema1, when application starts, then no EF migration executes and its Dolt head/schema remain unchanged.
- [ ] Given B1 is unpublished on schema1 and hub S1 adds schema2 from C0, when preflight/upgrade and merge succeed, then B/main=M1 with both B1 and S1 ancestors and B1 row values preserved.
- [ ] Given main has accepted the hub's schema update S1 and an unpublished feature has not merged S1, when a feature write can be applied faithfully under its current schema, then it can proceed under the normal branch guards. A write that needs S1 remains closed until that feature's own merge with S1 validates. The catch-up preserves the feature history, committed rows, and any protected draft.
- [ ] Given any local branch has a draft, when Nuke preflight runs, then409 maintenance_not_ready prevents binary replacement and preserves the draft.
- [ ] Given unpublished feature lacks an expand-schema attestation, when contract migration is requested, then it is refused even if all public remote branches are current.
- [ ] Given a ready public main but an unattested local feature, when deployment runs, then no binary/service/schema change occurs; explicit branch attestation is required.
- [ ] Given one Windows worker owns Atlas, when a second process starts or the owner crashes, then concurrent ownership is denied and restart recovers only after the OS releases its lock; committed rows and unpublished heads survive.
- [ ] Given isolated disposable Atlas stores on LAB and LEGION and FED-ATLAS-H/A/B fixtures, when Ph8 partitions/reconnects each control, publication/history, audit and identity path and restarts each application/worker, Dolt, gateway, identity and control/audit component at commit/publication/report boundaries, then every matrix row records exact heads/ancestor sets/rows/drafts/refs/epoch and audit receipts matching the workflow outcome, with zero missing provider/migration/recovery rows. Missing any component/path row fails acceptance; unrelated live projects retain their provider mappings and data.

## FR-MCP-187 original-provider-to-Dolt migration tool

Migrate all three original providers using verified seed then reviewed join, permanent identities, audit backfill and a source-retaining all-domain/all-branch project-to-SQLite recovery export.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given the same fixture in SQLite,PostgreSQL,SQL Server, when seed migration runs, then counts, typed hashes, IDs, nulls, Unicode, decimals, UTC precision and FK relationships match in Dolt.
- [ ] Given target batch1 committed before a crash but its acknowledgement was lost, when resume runs, then batch1 is recognized by journal and no duplicate rows appear.
- [ ] Given manifest source hash S0, when source is replaced by S1 before resume, then409 migration_source_changed writes no additional target rows.
- [ ] Given one row hash differs, when cutover is requested, then409 migration_verification_failed leaves source routing active and writes closed on the target.
- [ ] Given verified cutover completes, when target receives a new TODO, then source backup remains present, read-only and byte-identical; the new TODO exists only in Dolt until an explicit recovery export.
- [ ] Given seed C0 is published and join imports J1, when join completes in Ph6, then C0 is an ancestor of J1 and reviewed main M1; no second root is created.
- [ ] Given seed T1.title=A and frozen join T1.title=B, when join planning runs, then an explicit keep/take/manual decision is required before J1 even if Dolt would fast-forward.
- [ ] Given J1 is verified but main integration is unresolved, when cutover runs, then409 migration_verification_failed preserves source routing; only verified reviewed M1 permits cutover.
- [ ] Given A1 adds TODO T2 after cutover, when export-sqlite targets A1, then the new SQLite DB contains T2 and all TODO children/aliases with matching hashes, while the original source remains unchanged.
- [ ] Given only Ph3 is implemented, when mode=join is requested, then501 unsupported_capability creates no target commit or routing change.
- [ ] Given Atlas has main=A1 with post-cutover TODO T1, requirement R1/mapping, use case U1/actor/flow/step, memory W1, portable setting K1 and promoted session S1/turn/action, plus unpublished feature=F1 and draft T2.title=draft, when export-project freezes both branches and verifies fresh SQLite snapshots plus operational backup, then all IDs/typed values/links/soft deletes match each captured working set, feature still resolves F1, T2 remains draft without a new Dolt commit, audit receipts reconcile, the retained Dolt archive contains A1/F1 ancestry and original SQLite bytes are unchanged. Omit W1 or feature in a negative control: migration_verification_failed prevents routing switch.

## FR-MCP-188 imported state does not repeat external effects

Imported project or audit state never replays external effects and general federation remains outside QuadBrain-only keyserver gating.

Server: synchronization applies data without executing external operations. Plugin: distinguish history/data synchronization from separately authorized external effects in preview, status and receipts.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given A1 records an already-sent GitHub update, when B integrates A1, then B state includes the result and outbound GitHub/email invocation count is0.
- [ ] Given accepted commit A1 and effect E1, when sync retries10 times, then the effect ledger still has one E1 and no new external dispatch occurs.
- [ ] Given audit backfill contains command names and receipt payloads, when the hub ingests them, then they are stored as evidence and no business command executes.
- [ ] Given QuadBrain coordinator is degraded but local authorization passes, when federation commit/control/audit operations execute, then none invokes the keyserver and valid local work succeeds.
- [ ] Given incoming A1 contains a TODO completed state and inert historical external-action metadata, when the plugin shows synchronization success, then it labels data received with zero external operations executed; requesting an external operation requires a separate explicit authorized command and its own receipt.

## FR-MCP-189 safe shared-host editing and commit ownership

Enforce branch edit ownership and fenced live-write serialization so no agent or sync worker captures or overwrites another agent draft.

Server: editing owner, request/release and fenced takeover contracts. Plugin: display editing owner, request/release context and offer another branch without capturing another agent draft.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Given agent1 owns a draft, when agent2 commits the same branch, then409 branch_busy creates no commit and the draft remains agent1-owned.
- [ ] Given fence4 expires and fence5 is issued, when a fence4 edit arrives, then409 lease_lost persists no row changes.
- [ ] Given preview fingerprint W1, when the working set becomes W2, then commit with W1 returns409 working_set_changed and requires a fresh preview.
- [ ] Given a write admitted under fence4, when expiry occurs mid-transaction, then that write finishes under the gate before fence5 takeover; a later fence4 write is rejected.
- [ ] Given request R1 created A1 and reply was lost, when R1 retries after restart, then A1 is returned by its commit trailer and history contains no duplicate commit.
- [ ] Given an edit holds the branch gate, when the integration worker requests a live head move, then it waits; after the edit finishes, fingerprint revalidation detects the draft and returns waiting_local_work without overwriting it.
- [ ] Given agent1 owns A/main fence4 with a draft, when agent2 views context and requests editing, then it sees the owner and branch, receives409 branch_busy, and may select a separate feature branch; owner release/takeover requires explicit authorization and preserves the main draft without commit or discard.

## FR-MCP-190 Reload the operator profile after context compaction

Every official agent plugin shall request the host-specific add-profile action immediately after context compaction and exactly once more when measured post-compaction context usage first reaches 70 percent.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] After a compaction event, the plugin returns the host-specific add-profile instruction at the earliest supported injection boundary.
- [ ] After the immediate reload, the plugin remains armed and returns one additional add-profile instruction when measured usage first reaches at least 70 percent of the active context window.
- [ ] The threshold instruction fires exactly once for each compaction cycle and then disarms.
- [ ] Missing or malformed context-usage evidence leaves the threshold armed and does not fabricate a context window.
- [ ] All nine official plugins resolve their supported host-specific add-profile instruction.

