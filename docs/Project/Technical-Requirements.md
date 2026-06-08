# Technical Requirements (MCP Server)

## TR-01

**TR-01** — Placeholder requirement backfilled by DB-FK-001.

## TR-02

**TR-02** — Placeholder requirement backfilled by DB-FK-001.

## TR-03

**TR-03** — Placeholder requirement backfilled by DB-FK-001.

## TR-04

**TR-04** — Placeholder requirement backfilled by DB-FK-001.

## TR-05

**TR-05** — Placeholder requirement backfilled by DB-FK-001.

## TR-06

**TR-06** — Placeholder requirement backfilled by DB-FK-001.

## TR-07

**TR-07** — Placeholder requirement backfilled by DB-FK-001.

## TR-08

**TR-08** — Placeholder requirement backfilled by DB-FK-001.

## TR-09

**TR-09** — Placeholder requirement backfilled by DB-FK-001.

## TR-10

**TR-10** — Placeholder requirement backfilled by DB-FK-001.

## TR-11

**TR-11** — Placeholder requirement backfilled by DB-FK-001.

## TR-12

**TR-12** — Placeholder requirement backfilled by DB-FK-001.

## TR-13

**TR-13** — Placeholder requirement backfilled by DB-FK-001.

## TR-14

**TR-14** — Placeholder requirement backfilled by DB-FK-001.

## TR-GRAPHRAG-ADHOC-001

**Ad-hoc text ingestion pipeline** — GraphRagService.IngestTextAsync shall accept raw text, chunk via Chunker (512 tokens), generate embeddings via IEmbeddingService (all-MiniLM-L6-v2, 384-dim), persist ContextDocumentEntity and ContextChunkEntity rows with workspace scoping, register vectors in IVectorIndexService, and optionally trigger IndexAsync. Document ID format: "adhoc-{Guid:N}". Content hash: SHA256.

## TR-GRAPHRAG-ADHOC-002

**Explicit graph entity and relationship storage** — New EF Core entities GraphEntityEntity (Id, WorkspaceId, Name, EntityType, Description, Metadata JSON, timestamps) and GraphRelationshipEntity (Id, WorkspaceId, SourceEntityId FK, TargetEntityId FK, RelationshipType, Description, Weight default 1.0, Metadata JSON, timestamps) with workspace query filters, cascade delete from entity to relationships, and indexes on Name, EntityType, SourceEntityId, TargetEntityId, RelationshipType.

## TR-GRAPHRAG-ADHOC-003

**Document lifecycle with cascade delete and vector cleanup** — DeleteDocumentAsync shall query chunk IDs for the document, call IVectorIndexService.RemoveVector for each chunk, then delete the ContextDocumentEntity (EF cascade removes chunks). RemoveVector removes the chunk from internal HNSW dictionaries making the node unreachable; full rebuild reclaims space. ListDocumentsAsync shall return paginated results with ChunkCount and TotalTokens computed via subquery.

## TR-LOC-001

**Localization Infrastructure** — Multi-language support for the MCP server. *(Planned - implementation scope TBD.)*

## TR-MCP-AGENT-001

**Agent EF Core Entities** — `AgentDefinitionEntity` (agent type definitions with defaults), `AgentWorkspaceEntity` (per-workspace agent configurations with overrides, banning, isolation strategy), and `AgentEventLogEntity` (lifecycle event audit log). All stored in primary instance SQLite via `McpDbContext`. Unique index on `(AgentDefinitionId, WorkspacePath)` for workspace configs. JSON serialization for list fields (`DefaultModelsJson`, `ModelsOverrideJson`, `InstructionFilesOverrideJson`).
**Covered by:** `AgentDefinitionEntity`, `AgentWorkspaceEntity`, `AgentEventLogEntity`, `McpDbContext`

## TR-MCP-AGENT-002

**Built-in Agent Type Defaults** — `AgentDefaults.GetBuiltInDefaults()` returns seed data for 7 built-in agent types: copilot, cline, cursor, windsurf, claude-code, aider, continue. Each includes default launch command, instruction file path, models, branch strategy, and seed prompt. `AgentService.SeedBuiltInDefaultsAsync` is idempotent - only inserts agents not already present. Built-in definitions cannot be deleted.
**Covered by:** `AgentDefaults`, `AgentService`

## TR-MCP-AGENT-003

**Agent REST API** — `AgentController` at `/mcpserver/agents` with endpoints for: definition CRUD (`/definitions`), workspace agent CRUD (root), ban/unban (`/{agentId}/ban`, `/{agentId}/unban`), lifecycle events (`/{agentId}/events`), and YAML validation (`/validate`). Mutation endpoints require `[Authorize(Policy = "AgentManager")]` (JWT). Read endpoints use standard workspace API key auth.
**Covered by:** `AgentController`, `IAgentService`, `AgentService`

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

## TR-MCP-AGENT-006

**Hosted .NET 9 Microsoft Agent Framework Library** — The solution SHALL provide a dedicated .NET 9 class library for hosting an MCP-aware agent inside external .NET applications built on Microsoft Agent Framework. The library SHALL expose DI-friendly registration and configuration APIs for MCP Server connectivity, agent construction, and host lifecycle integration so host applications do not need to assemble low-level MCP session-log or TODO plumbing themselves.
**Status:** ✅ Complete

**Covered by:** `ServiceCollectionExtensions`, `McpAgentOptions`, `McpAgentOptionsValidator`, `IMcpHostedAgent`, `IMcpHostedAgentFactory`, `McpHostedAgent`, `McpHostedAgentFactory`, `McpHostedAgentRegistration`

## TR-MCP-AGENT-007

**Built-In MCP Session Log, TODO, Repository, Desktop-Launch, and PowerShell Workflow for Hosted Agents** — The hosted agent library SHALL implement built-in workflow operations for session bootstrap, turn creation/update, TODO retrieval/update, TODO plan/status/implementation flows, repository read/list/write operations, local desktop process launch using the existing MCP Server contracts, and persistent in-process PowerShell sessions hosted directly inside the current .NET agent process. The workflow SHALL preserve canonical ID conventions for session IDs, request IDs, and TODO IDs, SHALL keep repository access scoped to repo-relative paths, SHALL expose desktop launch through the authenticated workspace context only when the server-side desktop-launch feature gate, executable allowlist, and privileged desktop-launch token requirements are satisfied, SHALL keep PowerShell session state local to the hosted agent instance, SHALL expose the same local PowerShell session manager to host applications through `IMcpHostedAgent.PowerShellSessions`, and SHALL prefer reuse of existing client abstractions where server contracts already exist instead of duplicating transport logic.
**Status:** ✅ Complete

**Covered by:** `ISessionLogWorkflow`, `SessionLogWorkflow`, `SessionLogWorkflowContext`, `SessionLogTurnContext`, `ITodoWorkflow`, `TodoWorkflow`, `IMcpHostedAgent.PowerShellSessions`, `IHostedPowerShellSessionManager`, `McpHostedAgentToolAdapter`, `HostedPowerShellSessionManager`, `HostedPowerShellSessionHost`, `PowerShellSessionCreateResult`, `PowerShellSessionCommandResult`, `PowerShellSessionCloseResult`, `McpServerClient`, `RepoClient`, `DesktopClient`, `IMcpSessionIdentifierFactory`, `McpSessionIdentifierFactory`

## TR-MCP-AGENT-008

**Agent Pool Orchestration** — Reserved/planned: orchestrates a pool of agents for parallel task processing. Not yet implemented; placeholder for FR-MCP-028 / FR-MCP-050 traceability.

## TR-MCP-AGENT-009

**Agent Plugin Discovery** — Reserved/planned: discovers installed agent plugins and validates their contracts. Not yet implemented; placeholder for FR-MCP-050 traceability.

## TR-MCP-AGENT-010

**Agent Process Lifecycle** — Reserved/planned: manages start/stop/health of agent host processes. Not yet implemented; placeholder for FR-MCP-050 traceability.

## TR-MCP-AGENT-011

**Agent State Synchronization** — Reserved/planned: synchronizes agent state across pool members. Not yet implemented; placeholder for FR-MCP-050 traceability.

## TR-MCP-AGENT-012

**Agent Notification Bus** — Reserved/planned: routes notifications between agents and the workspace event bus. Not yet implemented; placeholder for FR-MCP-050 traceability.

## TR-MCP-AGENT-013

**PowerShell McpSession Dual-Path Session Cache Resolution** — `tools/powershell/McpSession.psm1` SHALL persist the canonical current session object to `.mcpSession/current-session.json` whenever session state is saved, SHALL consult that current-session cache before falling back to the legacy `.mcpServer/session.yaml` wrapper when resolving the active session, and SHALL reuse the cached current-session `sessionId` during initialization when the cache matches the requested agent/model and the session is still active.
When a session is completed, the module SHALL remove both the legacy wrapper cache and the `.mcpSession` current-session cache so a later bootstrap does not accidentally reuse a completed session. The implementation SHALL continue supporting the legacy wrapper file for backward compatibility.

**Status:** ✅ Complete

**Covered by:** `tools/powershell/McpSession.psm1`

## TR-MCP-AGENT-014

**PowerShell McpSession Trust Bootstrap Parity** — `tools/powershell/McpSession.psm1`, `tools/powershell/McpTodo.psm1`, and `tools/powershell/McpContext.psm1` SHALL use the same marker-signature verification, `/health` nonce echo verification, and `MCP_UNTRUSTED` fallback semantics before any follow-on MCP calls are allowed. The trust flow SHALL be explicit enough that session bootstrap, TODO bootstrap, and context bootstrap behave identically when trust succeeds or fails, and the failure path SHALL stop additional endpoint probing.
**Status:** ✅ Complete

**Covered by:** `tools/powershell/McpSession.psm1`, `tools/powershell/McpTodo.psm1`, `tools/powershell/McpContext.psm1`, `docs/context/module-bootstrap.md`, `docs/USER-GUIDE.md`

## TR-MCP-AGENT-PARITY-010

**TR-MCP-AGENT-PARITY-010** — Placeholder requirement backfilled for TODO link TR-MCP-AGENT-PARITY-010.

## TR-MCP-AGENT-PARITY-011

**TR-MCP-AGENT-PARITY-011** — Placeholder requirement backfilled for TODO link TR-MCP-AGENT-PARITY-011.

## TR-MCP-AGENT-PARITY-012

**TR-MCP-AGENT-PARITY-012** — Placeholder requirement backfilled for TODO link TR-MCP-AGENT-PARITY-012.

## TR-MCP-AGENT-PARITY-013

**TR-MCP-AGENT-PARITY-013** — Placeholder requirement backfilled for TODO link TR-MCP-AGENT-PARITY-013.

## TR-MCP-AGENT-PARITY-020

**TR-MCP-AGENT-PARITY-020** — Placeholder requirement backfilled for TODO link TR-MCP-AGENT-PARITY-020.

## TR-MCP-AGENT-PARITY-020..027

**TR-MCP-AGENT-PARITY-020..027** — Placeholder requirement backfilled for TODO link TR-MCP-AGENT-PARITY-020..027.

## TR-MCP-AGENT-PARITY-020-027

**TR-MCP-AGENT-PARITY-020-027** — Placeholder requirement backfilled for TODO link TR-MCP-AGENT-PARITY-020-027.

## TR-MCP-AGENT-PARITY-030

**TR-MCP-AGENT-PARITY-030** — Placeholder requirement backfilled for TODO link TR-MCP-AGENT-PARITY-030.

## TR-MCP-API-001

REST routes for todo/session/context/repo/github with OpenAPI.

## TR-MCP-API-002

**One-Shot Submission Contract and Intent Routing** — One-shot APIs SHALL support explicit context values `Plan`, `Status`, `Implement`, and `AdHoc`.
When `AgentName` is omitted, the runtime SHALL resolve request intent from context/prompt and select the configured default agent for that intent.

Template-mode and ad-hoc-mode payload validation SHALL enforce:

- `promptTemplateId` and ad-hoc prompt text cannot both be supplied in explicit mode.
- At least one prompt source must be resolvable.
- `id` is required for template-resolved requests and optional for ad-hoc requests.

**Status:** 🔴 Planned

**Covered by:** `AgentPoolController` *(planned)*, `AgentPoolIntentResolver` *(planned)*, request DTO validators *(planned)*

## TR-MCP-API-003

**Agent Pool Monitoring and Control APIs** — REST endpoints SHALL provide:
- Pooled agent availability snapshots.
- Runtime controls (connect, start, stop, immediate recycle).
- Queue operations (list, enqueue, cancel/remove, queued-item move up/down).
- Separate SSE notification stream emitting queue/agent lifecycle transitions with payload fields `AgentName`, `LastRequestPrompt`, and `SessionId`.
- Read-only response stream attachment supporting multiple concurrent subscribers.

**Status:** 🔴 Planned

**Covered by:** `AgentPoolController` *(planned)*, `AgentPoolNotificationService` *(planned)*, `AgentPoolStreamService` *(planned)*

## TR-MCP-ARCH-001

ASP.NET Core 9 server with HTTP and STDIO MCP transport.

## TR-MCP-ARCH-002

**DI Single Source of Truth and Pull-Based Change Notification** — Architecture audit and remediation across `McpServer.Support.Mcp` SHALL enforce:
- Stateful services, registries, managers, and providers must be DI-owned (`singleton` or `scoped`) and must not be instantiated via `new` or `ActivatorUtilities.CreateInstance` outside composition-root registration paths.
- Authoritative mutable state must have a single owner in DI; peer services must pull current state from that owner instead of receiving pushed state payloads.
- Observable state contracts must expose change signaling via `INotifyPropertyChanged` for data-availability/change notification, without embedding mutable payload transfer in event arguments.
- Race-condition remediation must prioritize ownership/lifetime design in DI (single owner + pull model); fire-and-forget propagation and ad-hoc synchronization used as state-sharing mechanisms are prohibited.
- Automated validation must cover DI registration lifetimes and notification semantics for remediated services.

**Status:** 🔴 Planned

## TR-MCP-AUDIT-001

**Audited Copilot Client** — `AuditedCopilotClient` decorates `ICopilotClient`. Before each Copilot invocation: determines affected workspaces, creates `in_progress` session log entries per workspace. After invocation: logs `completed` entries with result and actions taken. Action type: `copilot_invocation`. Registered as DI decorator so all server-initiated Copilot calls are audited.
**Status:** ✅ Complete

**Covered by:** `AuditedCopilotClient`, `Program.cs` (`ICopilotClient` decorator wiring), `McpStdioHost` (`ICopilotClient` decorator wiring), `CopilotServiceCollectionExtensions`

## TR-MCP-AUTH-001

**OIDC JWT Bearer Authentication** — ASP.NET Core JWT Bearer middleware configured with OIDC authority/issuer, audience (`mcp-server-api`), and optional client secret based on provider requirements. `OidcAuthOptions` bound from `Mcp:Auth` configuration section. Management endpoints (agent mutations) require `[Authorize(Policy = "AgentManager")]`; read endpoints fall back to existing API key auth. `RequireHttpsMetadata` configurable for local development.
**Covered by:** `OidcAuthOptions`, `Program.cs`, `AgentController`

## TR-MCP-AUTH-002

**GitHub Federation via OIDC Provider** — OIDC provider setup may configure GitHub as a social Identity Provider with `user:email read:org` scopes. First-login flow may auto-create users from GitHub accounts. GitHub username mapped to `github_username` user attribute. Setup scripts accept `--GitHubClientId` / `--GitHubClientSecret` parameters; GitHub federation is optional.
**Covered by:** `Setup-McpKeycloak.ps1`, `setup-mcp-keycloak.sh`

## TR-MCP-AUTH-003

**Device Authorization Flow for CLI Clients** — OIDC `mcp-director` client configured as public with OAuth 2.0 Device Authorization Grant enabled. Director CLI initiates device flow, displays user code and verification URI, polls for token completion. Provider claim mapping ensures `mcp-server-api` appears in token audience and includes `realm_roles`.
**Covered by:** `Setup-McpKeycloak.ps1`, `setup-mcp-keycloak.sh`, `McpServer.Director`

## TR-MCP-BATCH-001

**Robust Bash plugin batch records normalization** — Bash-style MCP server plugin wrappers SHALL normalize requirement batch records from unindented YAML sequences, indented YAML sequences, and inline JSON arrays before schema validation and typed request conversion.

## TR-MCP-BATCH-109

**Requirements batch endpoint and workflow support** — REST controllers, RequirementsClient, repository implementations, and REPL workflow dispatch shall expose atomic per-kind and mixed requirements batch create/update operations with all-or-nothing validation and structured batch result errors.

## TR-MCP-BATCHTS-001

**Robust TypeScript plugin batch records normalization** — TypeScript MCP server plugin tools SHALL normalize requirement batch records from object arrays and string YAML or JSON arrays before bridge request conversion while preserving nested acceptanceCriteria booleans.

## TR-MCP-BYRD-001

**Workspace-Scoped Byrd Execution Store** — The server SHALL persist Byrd iteration phases, execution TODOs, and TODO checkpoints in a workspace-scoped durable store under `.mcpServer`, with stable IDs for phases, TODOs, and checkpoints. The execution store SHALL coexist with the existing TODO providers without breaking legacy TODO CRUD behavior.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Services/Models/TodoExecutionModels.cs`, `src/McpServer.Services/Services/ITodoExecutionService.cs`, `src/McpServer.Services/Services/TodoExecutionService.cs`

## TR-MCP-BYRD-002

**Bounded Hydration and Delta Queries** — The server SHALL hydrate a bounded execution context for the active Byrd TODO using requirement snippets, recent session-turn summaries, relevant files, artifacts, validation state, and execution pointers. It SHALL also return checkpoint-based delta context that reports only the new turns, artifacts, commits, and next action since a specified checkpoint.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Services/Services/TodoExecutionService.cs`, `src/McpServer.Support.Mcp/Controllers/TodoExecutionController.cs`, `src/McpServer.Support.Mcp/McpStdio/McpServerMcpTools.cs`, `src/McpServer.Client/TodoClient.cs`

## TR-MCP-BYRD-003

**Byrd Progression Enforcement** — The execution service SHALL enforce Byrd progression rules so implementation cannot begin before unit tests are defined, validation cannot begin without implementation evidence, blocked TODOs require an explicit resume reason, and completion requires passing validation plus satisfied acceptance criteria. Test-plan updates, checkpoints, validation results, and session-turn linking SHALL update the persisted execution pointers used for resumption.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Services/Services/TodoExecutionService.cs`, `src/McpServer.Support.Mcp/Controllers/TodoExecutionController.cs`

## TR-MCP-BYRD-004

**Structured TODO Execution Surfaces** — The server SHALL expose the Byrd execution workflow through REST endpoints, STDIO MCP tools, and typed client methods, including the safe `adb_step` action surface for Android validation. The exposed contracts SHALL remain structured and bounded for iteration phase creation, plan decomposition, active TODO selection, execution context hydration, checkpoint append, validation result recording, status progression, session-turn linking, and device actions.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Support.Mcp/Controllers/TodoExecutionController.cs`, `src/McpServer.Support.Mcp/McpStdio/McpServerMcpTools.cs`, `src/McpServer.Client/Models/TodoModels.cs`, `src/McpServer.Client/TodoClient.cs`

## TR-MCP-BYRD-005

**Byrd process plan creation requirements** — The Byrd Development Process V3 document must define plan creation requirements for decision-complete frontier-model handoff plans, including required FR/TR/TEST capture, TDD tests, expected red state, green criteria, validation scope, and acceptance criteria before implementation begins.

## TR-MCP-CFG-001

IOptions-based configuration for all filesystem and runtime settings.

## TR-MCP-CFG-002

Port selection from `Mcp:Port` with `PORT` env override.

## TR-MCP-CFG-003

**Workspace Configuration Schema** — Workspace state is persisted in `appsettings.json` under `Mcp:Workspaces` (not in EF/SQLite). Each entry includes: `WorkspacePath` (required, absolute path, primary key), `Name` (required), `WorkspacePort` (required), `TodoPath` (default: `docs/todo.yaml`), `DataDirectory` (optional override for mcp.db), `TunnelProvider` (optional: `ngrok`/`cloudflare`/`frp`), `RunAs` (optional Windows identity), `IsPrimary` (default: false), `IsEnabled` (default: true), `DateTimeCreated`, `DateTimeModified`. Port uniqueness enforced; auto-assignment from `max(existing) + 1`. File written atomically via `JsonNode` patching with `IConfigurationRoot.Reload()`.

## TR-MCP-CFG-004

**YAML Configuration Support** — `Program.cs` calls `builder.Configuration.AddYamlFile("appsettings.yaml", optional: true, reloadOnChange: true)` using `NetEscapades.Configuration.Yaml`. YAML configuration merges with and can override `appsettings.json` values. Intended for local-only overrides not committed to source control.
**Covered by:** `Program.cs`, `NetEscapades.Configuration.Yaml`

## TR-MCP-CFG-005

**System-Wide Default Copilot Model Propagation** — Setting the default Copilot model for all session types requires updates to three locations:
- `CopilotClientOptions.Model` default value (in `McpServer.Common.Copilot`) - controls server-initiated CLI invocations via `ICopilotClient`. Configurable at runtime via `Mcp:Copilot:Model`.
- `VoiceConversationOptions.CopilotModel` default value (in `McpServer.Support.Mcp/Options/`) - controls voice conversation session model. Configurable via `Mcp:Voice:CopilotModel`.
- `AgentDefaults.GetBuiltInDefaults()` (in `McpServer.Support.Mcp/Services/`) - seed data for built-in agent type definitions including the `copilot` agent's `DefaultModelsJson`. Only affects new installations (existing agent definitions are not re-seeded).

All three share the pattern of a compile-time default overridable via `IOptions<T>` configuration binding. No new infrastructure is required - this is a default-value update propagated through existing `IOptions`-based configuration (TR-MCP-CFG-001).

**Status:** 🔴 Planned

**Covered by:** `CopilotClientOptions`, `VoiceConversationOptions`, `AgentDefaults`

## TR-MCP-CFG-006

**Administrative Configuration Snapshot and YAML Patch API** — `ConfigurationController` SHALL expose `GET /mcpserver/configuration` returning the current flattened `IConfiguration` view as `section:key` pairs, and `PATCH /mcpserver/configuration` accepting a flattened dictionary that patches only the submitted keys into `appsettings.yaml`.
Persistence SHALL be delegated to a dedicated helper service that resolves the correct loaded `appsettings` file path, serializes concurrent mutations across the full read-modify-write cycle, writes YAML or JSON via temp-file-plus-atomic-replace semantics, and reloads `IConfigurationRoot` after successful updates. `WorkspaceController` global-prompt updates SHALL reuse the same helper so shared configuration writes obey the same durability and reload guarantees. The endpoints SHALL use standard JWT Bearer admin authorization and remain closed when OIDC is disabled.

**Status:** ✅ Complete

**Covered by:** `ConfigurationController`, `AppSettingsFileService`, `Program.cs` (JWT Bearer auth setup), `WorkspaceController` (shared appsettings helper reuse)

## TR-MCP-CFG-007

**Encryption Configuration and Provider Settings Surface** — `Mcp:Database:Provider` and related connection-string settings SHALL support SQLite, PostgreSQL, and SQL Server selection through appsettings and environment-variable overrides. The configuration surface SHALL expose an explicit optional encryption-enabled flag plus the provider-specific connection, key, and prerequisite settings needed by the selected native at-rest encryption facility. Configuration resolution SHALL be centralized so runtime startup and design-time EF tooling can resolve the same effective provider and encryption inputs.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Support.Mcp/Options/McpDatabaseConfigurationResolver.cs`, `src/McpServer.Storage/McpDbContextFactory.cs`, `src/McpServer.Support.Mcp/Program.cs`, `src/McpServer.Support.Mcp/McpStdio/McpStdioHost.cs`, `src/McpServer.Support.Mcp/appsettings.yaml`, `src/McpServer.Support.Mcp/appsettings.Staging.yaml`

## TR-MCP-CI-001

**Azure DevOps Repository Pipeline Definition** — The repository SHALL use `azure-pipelines.yml` as the CI/CD definition for the core repo workflow. The pipeline SHALL trigger on `main` and `develop` pushes and pull requests with path filters matching the tracked source, test, docs, script, template, and pipeline-definition files. The pipeline SHALL run repository config validation, restore/build/test the support MCP test project, compute package versioning from GitVersion, publish the server build artifact, lint and link-check documentation, build the DocFX site artifact, run Windows MSIX packaging as a non-blocking job, and pack the client NuGet package.
Package publication SHALL be branch-conditional: `main` publishes to `nuget.org` only when `NuGetApiKey` is configured, while non-`main` branches publish to Azure Artifacts only when `AzureArtifactsFeedUrl` is configured. Optional docs deployment to Azure static website storage SHALL be gated behind explicit pipeline variables so the repo pipeline remains portable when deployment infrastructure is absent. The retired GitHub Actions workflow YAML files SHALL be removed from the repository as part of the migration, and retention of stale runs/artifacts SHALL move to Azure DevOps retention policy configuration rather than a repository-hosted cleanup workflow.

**Status:** ✅ Complete

**Covered by:** `azure-pipelines.yml`, `docs/AZURE-PIPELINES.md`, `README.md`, `docs/MCP-SERVER.md`, `docs/RELEASE-CHECKLIST.md`

## TR-MCP-COMP-001

**Workspace Compliance Ban Lists** — `WorkspaceDto`, `WorkspaceCreateRequest`, and `WorkspaceUpdateRequest` include four `List<string>` properties: `BannedLicenses`, `BannedCountriesOfOrigin`, `BannedOrganizations`, `BannedIndividuals`. `MarkerFileService.BuildTemplateContext` exposes these as Handlebars context (null when empty). `DefaultPromptTemplate` uses `{{#if}}` / `{{#each}}` blocks to conditionally render compliance sections. Recognized action types: `license_violation`, `origin_violation`, `origin_review`, `entity_violation`, `dependency_add`.
**Covered by:** `IWorkspaceService.cs`, `MarkerFileService.cs`

## TR-MCP-COMP-002

**Agent Values Prompt Sections** — `DefaultPromptTemplate` includes five mandatory non-configurable sections: (1) Absolute Honesty, (2) Correctness Above All, (3) Complete Decision Documentation, (4) Professional Representation and Audit Trail, (5) Source Attribution. Each section specifies required session log action types (`commit`, `pr_comment`, `issue_comment`, `web_reference`, `design_decision`).
**Covered by:** `MarkerFileService.DefaultPromptTemplate`

## TR-MCP-COMP-003

**Session Continuity Protocol** — The `default-marker-prompt` template (YAML) includes Requirements Tracking, Design Decision Logging, and Session Continuity sections. Agents must: read marker file at session start, query recent session logs, query TODOs, read Requirements-Matrix.md, post updated session logs every ~10 interactions, and capture requirements/decisions as they emerge.
**Covered by:** `templates/prompt-templates.yaml` (`default-marker-prompt`), `PromptTemplateService`

## TR-MCP-CQRS-001

**Standalone CQRS Library** — `McpServer.Cqrs` published as NuGet package `SharpNinja.McpServer.Cqrs`. Targets `net9.0`. Zero external dependencies beyond `Microsoft.Extensions.Logging.Abstractions` and `Microsoft.Extensions.DependencyInjection.Abstractions`. Provides: `ICommand<TResult>`, `IQuery<TResult>`, `ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>`, `Dispatcher`, `CallContext`, `CorrelationId`, `Result<T>`, `IPipelineBehavior`, and DI registration extensions. All dispatched calls are async (`Task<Result<T>>`).
**Status:** ✅ Complete - 37 unit tests passing

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

## TR-MCP-CTX-001

**New Project Context Indexing** — Ingestion configuration must include `src/McpServer.Cqrs/**/*.cs`, `src/McpServer.Cqrs.Mvvm/**/*.cs`, `src/McpServer.UI.Core/**/*.cs`, and `src/McpServer.Director/**/*.cs` in file patterns. Marker prompt Available Capabilities section lists all four projects with descriptions.
**Status:** ✅ Complete

**Covered by:** `Program.cs` / `McpStdioHost` `PostConfigure<IngestionOptions>` required-pattern merge, `appsettings.yaml` `Mcp:RepoAllowlist`, `MarkerFileService.DefaultPromptTemplate`

## TR-MCP-DATA-001

SQLite persistence for MCP metadata and optional TODO backend.

## TR-MCP-DATA-002

HNSW vector index with ONNX embeddings.

## TR-MCP-DATA-003

SQLite FTS5 full-text search support and hybrid ranking.

## TR-MCP-DB-001

**Database-authoritative workspace registry** — Workspaces must be stored in a canonical Workspaces table as the source of truth, with appsettings workspace entries generated only as informational projections after successful database commits.

## TR-MCP-DB-002

**Workspace foreign-key integrity** — Every persistent table with WorkspaceId must have a required FK to Workspaces, including global rows through a reserved empty WorkspaceId row and federation workspace mappings.

## TR-MCP-DB-003

**Soft deletes for persistent MCP data** — Persistent MCP domain deletes must be logical deletes with deletion metadata and Restrict or NoAction relationships, never physical row removal or cascade delete for durable domain state.

## TR-MCP-DB-004

**Generic audit ledger for mutable data** — Every mutable persistent database entity must emit append-only audit rows with workspace, entity key, action, actor/source, timestamps, and previous/current snapshots, while TODO-specific audit history remains compatible.

## TR-MCP-DB-005

**TODO and requirement relational links** — TODO requirement references and requirement traceability links must be stored as relational rows with FKs to TODO lifecycle anchors and Requirements, with missing referenced requirements backfilled before FK enforcement.

## TR-MCP-DESKTOP-001

**Desktop Process Launcher** — `DesktopProcessLauncher` in `Native/` uses P/Invoke (`WTSQueryUserToken`, `DuplicateTokenEx`, `CreateProcessAsUser`) to launch processes on the interactive desktop from a LocalSystem service context. Two launch modes: `LaunchWithStdio` (redirected stdin/stdout/stderr pipes for Copilot CLI integration) and `LaunchVisible` (visible console window, no pipes). `ResolveCommandPathAsync` resolves WinGet shim paths via desktop PowerShell to find actual executable locations. Uses `CreateProcessAsUser` (not `CreateProcessWithTokenW`, which causes `STATUS_DLL_INIT_FAILED` under LocalSystem).
**Covered by:** `DesktopProcessLauncher`, `NativeMethods`

## TR-MCP-DIR-001

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-001)*

## TR-MCP-DIR-002

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-002)*

## TR-MCP-DIR-003

*Moved to [Requirements-Director.md](Requirements-Director.md#tr-mcp-dir-003)*

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

## TR-MCP-DOC-001

**Marketing documentation coverage** — Marketing and agent-facing documentation shall explain McpServer purpose, supported UI and agent surfaces, plugin acquisition through the MCP tool registry, single-line JSON stdio guidance, current pipeline references, and generated requirements wiki parity.

## TR-MCP-DOC-002

**Test XML Documentation Completeness** *(DIRECTIVE)* - All test projects SHALL include XML documentation comments on test classes and test methods. Each test XML doc SHALL explicitly specify: what behavior is being tested, what test data/fixtures are used, why that data/fixtures are used, and which requirement IDs are being validated. No test project is exempt from this requirement.

**Status:** ✅ Active directive

**Covered by:** `.github/copilot-instructions.md`, `AGENTS.md`

## TR-MCP-DRY-001

**DRY - No Duplication in Code or Scripts** *(DIRECTIVE)* - All code and scripts must follow the DRY principle without exception. Shared logic must be extracted into a single reusable location (service, helper, function, shared script module). Inline duplication of validation, parsing, formatting, or business logic across files is prohibited. Scripts must share common operations via parameterized functions or a shared module.

**Covered by:** `TodoValidator`, `MarkerFileService`, `ExcludeControllerFeatureProvider`, `Update-McpService.ps1`

## TR-MCP-DTO-001

**Extended Session Log Entry Fields** — `UnifiedRequestEntryDto` extended with: `designDecisions` (`List<string>`), `requirementsDiscovered` (`List<string>` of requirement IDs), `filesModified` (`List<string>` of file paths), `blockers` (`List<string>`). All fields are REQUIRED in the marker prompt session logging instructions except `blockers` which is RECOMMENDED.
**Covered by:** `UnifiedSessionLogDto.cs`

## TR-MCP-EVT-001

**In-Process Change Event Bus** — `ChannelChangeEventBus` SHALL be registered as a singleton `IChangeEventBus` and provide fan-out publish/subscribe semantics to independent subscribers using bounded channels (capacity 1000) with non-blocking publish behavior. When a subscriber buffer is full, delivery to that subscriber SHALL be rejected and logged at warning level instead of silently discarding already queued events.
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

## TR-MCP-FED-001

**Hub Proxy Federation Contract** — Federation configuration SHALL include `Role`, `HubBaseUrl`, `ProxyId`, `EnrollmentToken`, queue settings, and sync settings while preserving existing target/route configuration. Durable storage SHALL track proxies, proxy-hosted workspaces, operations, outbox fanout rows, and conflicts across SQLite, PostgreSQL, and SQL Server providers. Hub endpoints SHALL support proxy enrollment, heartbeat, proxy/workspace inventory, operation intake, acknowledgement, queue status, conflicts, sync, and adapter coverage. LocalProxy routing SHALL forward MCP traffic to the hub with loop-protection and operation headers, while local infrastructure and federation diagnostic endpoints remain local. Mutating LocalProxy requests SHALL queue durably when the hub is unreachable and replay through the hub intake endpoint.
**Covered by:** `FederationOptions`, `FederationRegistry`, `FederationHeaders`, `FederationTopologyService`, `FederationQueuedOperationReplayService`, `FederationController`, `McpDbContext`, `Federation*Entity`, provider migrations, `FederationMiddleware`, `FederationProxyService`

## TR-MCP-GH-001

**GitHub OAuth Bootstrap Configuration Contract** — The server SHALL bind GitHub integration settings from `Mcp:GitHub`, including OAuth client metadata (`ClientId`, `RedirectUri`, `AuthorizeEndpoint`, `Scopes`) and token store path/fallback policy flags. REST endpoints under `/mcpserver/gh/oauth/*` SHALL expose the effective bootstrap configuration and authorize URL composition.
**Status:** ✅ Complete

**Covered by:** `GitHubIntegrationOptions`, `Program.cs` options binding/post-configure, `McpStdioHost` options binding/post-configure, `GitHubController` (`/oauth/config`, `/oauth/authorize-url`)

## TR-MCP-GH-002

**Encrypted Workspace GitHub Token Persistence** — Workspace GitHub tokens SHALL be stored encrypted-at-rest using ASP.NET Core Data Protection with atomic file writes and normalized workspace-path keys. The server SHALL expose `/mcpserver/gh/auth/status`, `/mcpserver/gh/auth/token` (PUT), and `/mcpserver/gh/auth/token` (DELETE) for token lifecycle management.
**Status:** ✅ Complete

**Covered by:** `IGitHubWorkspaceTokenStore`, `FileGitHubWorkspaceTokenStore`, `GitHubController` auth endpoints, `Program.cs` DI registration

## TR-MCP-GH-003

**Authenticated GitHub CLI Execution Path with Policy-Governed Fallback** — GitHub CLI execution SHALL support per-call token overrides so workspace-stored tokens can be applied as `GH_TOKEN` when present. The execution path SHALL prefer stored tokens when configured, emit telemetry indicating selected auth mode, and reject/allow fallback based on `AllowCliFallback`. When a workspace path is known, gh commands SHALL execute with that workspace root as the working directory.
**Status:** ✅ Complete

**Covered by:** `IProcessRunner` (`ProcessRunRequest` overload), `ProcessRunner`, `GitHubCliService` token resolution + auth-mode selection logs, `GitHubIntegrationOptions`

## TR-MCP-GH-004

**GitHub Actions Workflow Run API Surface** — The server SHALL support workflow run list/detail/rerun/cancel operations via gh CLI and expose them at `/mcpserver/gh/actions/runs*` with typed model contracts and client parity.
**Status:** ✅ Complete

**Covered by:** `IGitHubCliService`, `GitHubCliService`, `GitHubController` actions endpoints, `McpServer.Client` (`GitHubClient`, `Models/GitHubModels.cs`)

## TR-MCP-GH-005

**Workspace-Scoped gh Repository Execution** — GitHub issue and sync operations that rely on the local gh CLI SHALL execute inside the resolved workspace root so repository-scoped gh commands run against the correct checkout. This SHALL apply to both stored-token and fallback-auth execution modes.
**Status:** ✅ Complete

**Covered by:** `WorkspaceServiceAccessor`, `GitHubCliService`, `ProcessRunRequest`

## TR-MCP-GH-006

**Canonical GitHub Priority Labels, MCP-Authoritative Priority Sync, and ISSUE Change Comments** — TODO-to-GitHub issue sync SHALL canonicalize priority labels to `priority: HIGH|MEDIUM|LOW`, SHALL remove stale or non-canonical priority labels, and SHALL treat the MCP TODO priority as authoritative even if GitHub labels drift. GitHub-to-TODO refresh for existing `ISSUE-*` items SHALL preserve the current local priority and description, and endpoint-triggered ISSUE updates SHALL add a GitHub issue comment that summarizes the applied local change set after sync completes.
**Status:** ✅ Complete

**Covered by:** `IssueTodoSyncService`, `GitHubCliService`

## TR-MCP-GH-007

**Generated GitHub Comment Note Sections and TODO Comment Round-Trip** — GitHub-to-TODO sync for existing `ISSUE-*` items SHALL rebuild a generated note section that contains GitHub issue comments inside explicit begin/end markers, SHALL preserve user-authored TODO note text outside that generated section, and SHALL continue to avoid mutating the established TODO description. TODO-to-GitHub comment export SHALL detect newly appended user-authored note text and publish that text as a GitHub issue comment rather than collapsing the change to a generic note-update summary. When GitHub marks the issue closed, the next GitHub-to-TODO sync SHALL reconcile the TODO as done.
**Status:** ✅ Complete

**Covered by:** `IssueTodoSyncService`

## TR-MCP-HTTP-001

**MCP Streamable HTTP Endpoint** — `app.MapMcp("/mcp-transport")` maps the native MCP protocol handler at a path separate from the REST routes (`/mcpserver/*`). The endpoint requires an `Accept: application/json, text/event-stream` header and returns HTTP 406 without it. Uses `ModelContextProtocol.AspNetCore` 0.9.0-preview.1.

## TR-MCP-HTTP-002

**Detailed and Sanitized HTTP 500 Error Contract** — All HTTP endpoints that return status code 500 SHALL emit a structured response body containing a non-empty human-readable error description that identifies the failing operation and provides actionable diagnostic context for the caller. The contract SHALL be applied centrally so endpoint implementations do not duplicate exception-to-response formatting. Response detail SHALL be sanitized to avoid leaking secrets, tokens, connection strings, or raw stack traces, while server-side logs SHALL retain the full exception detail needed for root-cause analysis.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Support.Mcp/Program.cs` `InvalidModelStateResponseFactory` (centralized RFC 7807 ProblemDetails emission for binder/validation failures, paired with `ValidationProblem` / `Problem` controller helpers for domain errors); `SessionLogController.SubmitAsync` and `GetByIdAsync` route through the centralized path. Sanitization defers to ASP.NET Core's default ProblemDetails serialization, which omits stack traces outside the Development environment.

## TR-MCP-INGEST-001

Pluggable ingestors for repo/session/external/github/issues.

## TR-MCP-INGEST-002

**Markdown Session Log Parser** — `MarkdownSessionLogParser.TryParse` recognizes Markdown files with a `# Session Log - {title}` or `# Copilot Session Log - {title}` header and parses them into `UnifiedSessionLogDto`. Extracts date, status, branch, model, duration, and known sections (Session Overview, Changes Made, Technical Requirements, Testing, etc.) as a summary entry. Individual `### Request` subsections are parsed as separate `UnifiedRequestEntryDto` entries. `NormalizeToStructuredText` produces a structured plain-text representation for FTS5 and vector embedding.

## TR-MCP-INGEST-003

**Direct Website URL Ingestion** — Add `WebsiteIngestor` with a dedicated `HttpClient` and bounded crawl behavior. Only `http`/`https` URLs are allowed. SSRF protections block localhost, loopback, RFC1918, and link-local targets (including DNS-resolved IPs). Redirects are bounded and re-validated at each hop. Per-request controls include max pages, max depth, max bytes per page, force refresh, and optional GraphRAG index trigger. Ingested pages upsert as `SourceType=external-web` with canonical URL source keys and deterministic document IDs.

## TR-MCP-LOG-001

**Exception Logging in Catch Blocks** *(DIRECTIVE)* - Every `catch` block that handles an exception must log the exception. Unexpected exceptions must use `LogError` with `ex.ToString()` as the message body. Expected/anticipated exceptions (e.g., `OperationCanceledException` on shutdown, `InvalidOperationException` for process-already-exited races, validation exceptions returned as HTTP 4xx) must use `LogWarning` with `ex.ToString()`. Catch blocks must not silently swallow exceptions with empty bodies or comments-only. The only permitted exception is re-throwing (`throw;`) without logging, where the exception will be logged by an outer handler.

## TR-MCP-LOG-002

**Identifier Naming Validation** — `TodoValidator` SHALL validate persisted TODO IDs against the canonical regex set `^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+-\d{3}$` or `^ISSUE-\d+$` for create/update dependency paths across all configured TODO storage providers (`yaml` and `database` per TR-MCP-TODO-005). `ISSUE-NEW` SHALL remain a create-time alias handled before persistence, not a persisted TODO identifier. `SessionLogIdentifierValidator` SHALL validate session/request IDs using canonical timestamped patterns and enforce exact source-type prefix parity (`SessionId` starts with `{sourceType}-` or `{agent}-`). Invalid values return HTTP 400 at controller boundaries and `ArgumentException` for direct service invocation.
**Status:** ✅ Complete

**Covered by:** `TodoValidator`, `TodoService`, `EfTodoService`, `TodoCreationService`, `SessionLogIdentifierValidator`, `SessionLogController`, `SessionLogService`

## TR-MCP-LOG-003

**Parseable Event Field-Cap Enforcement** — `ParseableEventFormatter` SHALL emit no more than 250 top-level fields for any individual Parseable event payload. The formatter SHALL always preserve the canonical Parseable metadata keys (`timestamp`, `level`, `message`, and `exception` when present), SHALL prevent user-supplied structured properties from overwriting those reserved keys, and SHALL drop excess non-reserved properties once the remaining field budget is exhausted. Property selection for retained non-reserved fields SHALL be deterministic so tests and operational analysis can reason about which fields survive truncation.
**Status:** ✅ Complete

**Covered by:** `ParseableEventFormatter`, `ParseableBatchFormatter`

## TR-MCP-MEMORY-001

**TR-MCP-MEMORY-001** — Placeholder requirement backfilled for TODO link TR-MCP-MEMORY-001.

## TR-MCP-MEMORY-002

**TR-MCP-MEMORY-002** — Placeholder requirement backfilled for TODO link TR-MCP-MEMORY-002.

## TR-MCP-MEMORY-003

**TR-MCP-MEMORY-003** — Placeholder requirement backfilled for TODO link TR-MCP-MEMORY-003.

## TR-MCP-MEMORY-004

**TR-MCP-MEMORY-004** — Placeholder requirement backfilled for TODO link TR-MCP-MEMORY-004.

## TR-MCP-MEMORY-005

**TR-MCP-MEMORY-005** — Placeholder requirement backfilled for TODO link TR-MCP-MEMORY-005.

## TR-MCP-MEMORY-006

**TR-MCP-MEMORY-006** — Placeholder requirement backfilled for TODO link TR-MCP-MEMORY-006.

## TR-MCP-MEMORY-007

**TR-MCP-MEMORY-007** — Placeholder requirement backfilled for TODO link TR-MCP-MEMORY-007.

## TR-MCP-MEMORY-008

**TR-MCP-MEMORY-008** — Placeholder requirement backfilled for TODO link TR-MCP-MEMORY-008.

## TR-MCP-MT-001

**WorkspaceContext Scoped Per-Request Service** — `WorkspaceContext` is a scoped service holding resolved workspace identity: `WorkspacePath`, `WorkspaceName`, `DataDirectory`, `TodoFilePath`, `SessionsPath`, `ExternalDocsPath`, `IsDefaultKey`, `IsResolved`. Populated by `WorkspaceResolutionMiddleware` before downstream services execute. Downstream services inject `WorkspaceContext` instead of reading `IConfiguration["Mcp:RepoRoot"]`.
**Covered by:** `WorkspaceContext`, `WorkspaceResolutionMiddleware`

## TR-MCP-MT-002

**WorkspaceResolutionMiddleware** — Runs before `WorkspaceAuthMiddleware` in the pipeline. Only activates for `/mcpserver/*` and `/mcp-transport` routes. Resolution chain: (1) `X-Workspace-Path` header validated against registered workspaces - returns 400 for unregistered paths; (2) API key reverse lookup via `WorkspaceTokenService.ResolveWorkspaceByToken()`; (3) `Mcp:RepoRoot` config fallback; (4) primary workspace from workspace list. Populates `WorkspaceContext` scoped service.
**Covered by:** `WorkspaceResolutionMiddleware`, `WorkspaceContext`, `WorkspaceTokenService`

## TR-MCP-MT-003

**EF Core Global Query Filter for WorkspaceId** — `McpDbContext` accepts optional `WorkspaceContext` to capture `_workspaceId` per-instance. `OnModelCreating` applies `.HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId)` on all 14 entity types. Empty `_workspaceId` disables filtering (backward compatible). `IgnoreQueryFilters()` escapes for cross-workspace admin queries. `WorkspaceId TEXT NOT NULL DEFAULT ''` column with indexes on all entity tables.
**Covered by:** `McpDbContext`, all entity types (`WorkspaceId` property)

## TR-MCP-MT-003A

`SessionLogService` injects an optional `WorkspaceContext` and stamps `WorkspaceId` on every entity it persists. When the context is null (ingestion / batch import path), the service skips stamping and relies on `McpDbContext.SaveChangesAsync` to auto-fill `WorkspaceId` for Added entities from the DbContext's resolved `_workspaceId`. This ensures POST/GET round-trips work under the same workspace context AND existing rows with empty WorkspaceId remain visible when no workspace header is set.

## TR-MCP-NUKE-001

**Non-interactive PowerShell hosts for Nuke automation** — The root Nuke PowerShell bootstrap and any build-owned pwsh.exe or powershell.exe child process SHALL include -NoLogo, -NoProfile, and -NonInteractive unless an invocation is explicitly documented as interactive. Live deployment guidance SHALL use the same flags.

## TR-MCP-OPS-001

Operational scripts for startup, health checks, packaging, config validation, and migration.

## TR-MCP-PLAN-001

**Safe session wrap-up and deploy sequencing** — Wrap-up plans must inventory dirty state across affected workspaces, preserve unrelated work, require Nuke or repo-supported deployment paths, and block publish or service updates until the intended slice is cleanly isolated and validated with zero failures and zero skips.

## TR-MCP-PLUGIN-008

**Codex requirements update command fallback parity** — The Codex plugin requirements fallback must pass updateFr, updateTr, and updateTest payloads to the REPL/client without dropping fields or invoking unsupported command aliases.

## TR-MCP-PLUGIN-009

**Session and compaction hook output contract** — Bash-family MCP plugins SHALL implement SessionStart, SessionEnd, PreCompact, and PostCompact scripts so that status-only execution paths return {}. Hook-specific output may be emitted only for event schemas that support it, and every hookSpecificOutput payload SHALL include the matching hookEventName. PostCompact history reload side effects SHALL NOT attempt context injection via additionalContext.
**Acceptance Criteria:**
- [x] Affected session and compact hook scripts no longer contain hookSpecificOutput or additionalContext emissions for status-only paths. (evidence: Targeted rg search over the affected session and compact scripts found no hookSpecificOutput or additionalContext after the fix.)

## TR-MCP-PLUGIN-SKILLS-001

**Probe TR id pattern** — Probe only; should not be created if id validation fails or duplicate cleanup is needed.

## TR-MCP-POL-001

**Natural Language Policy Management** — `PolicyManagementTool` MCP STDIO tool + `POST /mcpserver/workspace/policy` REST endpoint. Accepts natural language directives, parses intent (action, category, value, scope) via LLM, applies workspace config mutations via `IWorkspaceService.UpdateAsync`, logs `policy_change` actions per affected workspace session log.
**Status:** ✅ Complete

**Covered by:** `WorkspaceController` (`POST /mcpserver/workspace/policy`), `WorkspacePolicyService`, `WorkspacePolicyDirectiveParser`, `McpServerMcpTools.workspace_policy_apply`

## TR-MCP-QA-001

**QA Entity Tenancy** — `QuestionEntity`, `AnswerEntity`, `CommentEntity` use composite PK `(WorkspaceId, Id)` plus global query filter (mirrors TR-MCP-MT-003).

## TR-MCP-QA-002

**QA Question Tags JSON** — Tags stored as `TagsJson` string column on `QuestionEntity` (no separate Tag table), serialized via the same pattern as `TodoItemEntity.DescriptionJson`.

## TR-MCP-QA-003

**QA Provider Migrations** — Q&A storage works on all three providers: SQLite, PostgreSQL, SQL Server. One migration per provider project.

## TR-MCP-QA-004

**QA Denormalized Vote Counters** — `VoteCount` is an `int` column on Question and Answer; vote endpoints use atomic `UPDATE ... SET VoteCount = VoteCount + @delta` via raw SQL or EF interceptor pattern. Voter identity is NOT stored on the Question/Answer row itself - it is captured per vote in the audit history (TR-MCP-QA-019 records `Actor` on every `vote_up` / `vote_down` audit row), preserving full provenance without bloating the hot read path.

## TR-MCP-QA-005

**QA Accepted Answer Storage** — `AcceptedAnswerId` is a nullable string FK on `QuestionEntity`; accepting writes `AcceptedAnswerId` + `AcceptedAt`; un-accepting clears both.

## TR-MCP-QA-006

**QA Service Shape** — `EfQaService` follows the `EfTodoService` shape: `IServiceScopeFactory` for scoped DbContext access, `IWriteAuditLog` for audit, optional `IChangeEventBus` for events, internal `SemaphoreSlim` for write serialization.

## TR-MCP-QA-007

**QA REST Surface** — `QaController` routes at `/mcpserver/qa`; `WorkspaceResolutionMiddleware.WorkspaceIndependentPrefixes` adds `"/mcpserver/qa"` (joining `/mcpserver/todo` and `/mcpserver/sessionlog`). Auth is enforced by existing `WorkspaceAuthMiddleware`.

## TR-MCP-QA-008

**QA Search Indexing** — Q&A search integration: `IQaSearchIndexer` writes a `ContextDocumentEntity` (`SourceType = "qa-question"` or `"qa-answer"`, `SourceKey = $"qa/{kind}/{id}"`) and a `ContextChunkEntity` (`Content = Title + "\n\n" + Body + "\n\nTags: " + tags`) on create/update, and removes both on delete. Existing FTS5 triggers and `EmbeddingService` handle the rest; no new FTS virtual table.

## TR-MCP-QA-009

**QA Author Resolver** — `IQaAuthorResolver` reads `HttpContext.User.FindFirst("sub")`, the `X-Api-Key` header via `WorkspaceTokenService`, and a request-body `author` field, applying the precedence in FR-MCP-QA-008.

## TR-MCP-QA-010

**QA MCP STDIO Tools** — MCP STDIO tools live on the existing `FwhMcpTools` class (file `src/McpServer.Support.Mcp/McpStdio/McpServerMcpTools.cs`); each Q&A tool accepts optional `workspacePath` and calls the existing `ApplyWorkspaceOverride` helper.

## TR-MCP-QA-011

**QA FAQ Query Projection** — FAQ projection executes as a single EF query: `Questions.Where(q => q.AcceptedAnswerId != null).Include(q => q.AcceptedAnswer).OrderByDescending(q => q.AcceptedAnswer!.VoteCount).ThenByDescending(q => q.VoteCount).Take(limit)`.

## TR-MCP-QA-012

**QA Typed Client** — `QaClient` ships in `McpServer.Client` (NuGet `SharpNinja.McpServer.Client`); wired into `McpServerClient.Qa` via `McpServerClientFactory`.

## TR-MCP-QA-013

**QA XML Documentation** — XML docs on every new public type and member (CS1591 enforced). Test classes cite TR-PLANNED-013 plus the FR/TR/TEST IDs they validate.

## TR-MCP-QA-014

**QA REPL Workflow** — REPL exposure: `IQaWorkflow` in `McpServer.Repl.Core` wraps `McpServerClient.Qa`; `QaWorkflow` registered as singleton in `McpServer.Repl.Core/ServiceCollectionExtensions.cs`; `QaCommandShapes` defines `MethodNamespace = "workflow.qa"` and per-method constants; `ReplCommandDispatcher` constructor takes `IQaWorkflow` and switches on `workflow.qa.*` methods.

## TR-MCP-QA-015

**QA PowerShell Module** — PowerShell module `tools/powershell/McpQa.psm1` reads `AGENTS-README-FIRST.yaml` via existing `Find-McpMarkerFile`/`ConvertFrom-McpMarkerContent` helpers, exports `Get-McpQuestion`, `Search-McpQuestion`, `New-McpQuestion`, `Set-McpQuestion`, `Remove-McpQuestion`, `Add-McpAnswer`, `Approve-McpAnswer`, `Add-McpQaVote`, `Add-McpQaComment`, `Get-McpFaq`.

## TR-MCP-QA-016

**QA Plugin Skill** — Plugin skills: each sibling plugin repo adds `skills/qa/SKILL.md` with the standard YAML frontmatter (`name`, `description` with trigger phrases, `version`). Body documents the `workflow.qa.*` command namespace, request envelope shape, response shape, and explicitly positions Q&A as a workspace knowledge source (read FAQ first; if no accepted answer matches, ask; if a peer answers and accepts, future agents inherit the answer via FAQ + hybrid search).

## TR-MCP-QA-017

**QA Documentation Surface** — Documentation: a new `docs/context/qa-schema.md` defines the on-demand schema reference (entities, IDs, FAQ projection); `docs/USER-GUIDE.md`, `docs/CLIENT-INTEGRATION.md`, `docs/context/api-capabilities.md`, `docs/REPL-USER-GUIDE.md`, `docs/REPL-AGENT-GUIDE.md`, `docs/FAQ.md`, `AGENTS.md`, and the root `README.md` are updated to surface the subsystem; `CLAUDE.md` `Context Loading by Task Type` adds a Q&A row.

## TR-MCP-QA-018

**QA Audit Storage** — Audit storage: `QaAuditHistoryEntity` (composite PK `(WorkspaceId, Id)`, columns `EntityKind` enum `question`/`answer`/`comment`, `EntityId`, `Action` enum `create`/`update`/`delete`/`accept`/`unaccept`/`vote_up`/`vote_down`/`comment_add`/`comment_delete`, `Version` int, `Actor`, `SnapshotJson`, `CreatedAt`) lives in `src/McpServer.Storage/Entities/` with the same global query filter as Q&A entities. Composite index on `(EntityKind, EntityId, Version)`.

## TR-MCP-QA-019

**QA Audit Emission** — Audit emission: `EfQaService` injects `IWriteAuditLog` and calls it before persisting each mutation, capturing the pre-mutation snapshot (post-mutation for create) and computing `Version = MAX(Version) + 1` for that `(EntityKind, EntityId)` pair (mirrors `EfTodoService` audit pattern at line ~374).

## TR-MCP-QA-020

**QA Audit Query** — Audit query: `IQaService.GetAuditAsync` returns `QaAuditQueryResult` with `TotalCount` + paged `QaAuditEntryDto` rows; filterable by `entityKind`, `entityId`, `action`, `from`, `to`, `actor`. `EfQaService.GetAuditAsync` uses an EF query mirroring `EfTodoService.GetAuditAsync`.

## TR-MCP-QA-021

**QA Audit Surfaces** — Audit surfaces: `QaController` adds `[HttpGet("questions/{id}/audit")]`, `[HttpGet("answers/{id}/audit")]`, `[HttpGet("comments/{id}/audit")]`, and `[HttpGet("audit")]`; MCP tool `qa_get_audit`; client `QaClient.GetAuditAsync`; REPL `workflow.qa.audit`; PowerShell `Get-McpQaAudit`; plugin skill documents audit access as a knowledge-provenance tool.

## TR-MCP-QA-022

**QA Answer Sources JSON** — Answer payload supports a structured `sources[]` array (each entry: `{ url, title?, fetchedAt, contentHash? }`) so web-search-derived answers carry their citations. Stored on `AnswerEntity` as `SourcesJson` (nullable string column). DTO `AnswerDto.Sources`, request `CreateAnswerRequest.Sources`. FAQ projection surfaces sources alongside the accepted answer.

## TR-MCP-QA-023

**QA Mandatory Web Capture Skill Rule** — Plugin skill mandate: every `skills/qa/SKILL.md` opens with a mandatory rule block, formatted as a callout (`> [!IMPORTANT]`), stating: "Any time you invoke an internet search, web fetch, browser MCP, or any external HTTP retrieval to help answer a workspace question, you MUST create a Q&A entry capturing the question, the synthesized answer, and the source URLs in the `sources[]` array. If you verified the answer, accept it. This is required, not optional." Skill also documents the exact `workflow.qa.create` + `workflow.qa.createAnswer` + `workflow.qa.accept` sequence and the `sources[]` schema.

## TR-MCP-QA-024

**QA Companion Web Skill Cross References** — Companion skills in each plugin that perform web research (`web-research`, `browser`, `search`, or equivalents already shipped in each plugin) get an inline "Required follow-up" section pointing back at the Q&A skill, so the mandate is visible from both directions. Implementation step: enumerate each sibling plugin's existing skills and add the cross-reference where applicable.

## TR-MCP-QA-025

**QA Close and Duplicate Storage** — Close / duplicate storage: extend `QuestionEntity` with `IsClosed` (bool), `ClosedAt` (DateTimeOffset?), `ClosedByActor` (string?), `CloseReason` (enum string `closed`/`duplicate`/`off-topic`/`resolved`/`other`), `DuplicateOfQuestionId` (nullable, with a self-referencing FK + index). FAQ projection filters out closed questions unless `?includeClosed=true`. Search indexer emits a `closed` flag in the `ContextChunkEntity.Metadata` JSON so hybrid-search consumers can filter.

## TR-MCP-QA-026

**QA Close and Duplicate Surfaces** — Close / duplicate endpoints: `POST /mcpserver/qa/questions/{id}/close` (body `{ reason, duplicateOfQuestionId? }`), `POST /mcpserver/qa/questions/{id}/reopen`, both writing audit rows with action `close` / `reopen` / `mark_duplicate`. Surface in MCP tool (`qa_close_question`, `qa_reopen_question`), client (`CloseQuestionAsync`, `ReopenQuestionAsync`), REPL (`workflow.qa.close`, `workflow.qa.reopen`), PowerShell (`Close-McpQuestion`, `Open-McpQuestion`), and skill body.

## TR-MCP-QA-027

**QA Body Rendering** — Sanitization pipeline: add NuGet packages `Markdig` (markdown -> HTML) and `Ganss.Xss` to `Directory.Packages.props` (central package management). New service `IQaBodyRenderer` (impl `QaBodyRenderer`) renders + sanitizes using a strict allow-list: tags `p, h1, h2, h3, ul, ol, li, code, pre, strong, em, a, blockquote, table, thead, tbody, tr, th, td, hr, br, img`; attributes `href, src, alt, title, class` (with `class` restricted to a set of code-highlight classes); URLs limited to `http`, `https`, `mailto`; force `rel="nofollow noopener"` on all `<a>`; drop `script`, `iframe`, `object`, `embed`, `form`, all `on*` attributes, and `javascript:` URLs. Renderer is called by `EfQaService` on every Create / Update for Question, Answer, and Comment, populating sibling columns `TitleHtml?` (Question only), `BodyHtml`, plus answer/comment equivalents. Audit snapshots also store the rendered HTML so historical views are safe.

## TR-MCP-QA-028

**QA Sanitization Tests** — Sanitization tests: `tests/McpServer.Support.Mcp.Tests/Services/QaBodyRendererTests.cs` covers a canonical XSS-payload corpus (script tags, `<img onerror>`, `javascript:` href, data URLs, nested HTML in markdown, comment-out attacks, html entities). Same corpus runs against the live controller via `tests/McpServer.Qa.Validation/ErrorTests/SanitizationTests.cs`.

## TR-MCP-QA-029

**QA FAQ Wiki Generation Target** — FAQ wiki page generation: add a Nuke build target (e.g. `BuildFaqWikiPage`) in `build/Build.cs` (or whatever the existing target file is) that POSTs `GET /mcpserver/qa/faq?limit=500&includeSources=true` to a configured workspace (env-var-driven endpoint + API key, mirroring existing wiki publication patterns), formats the response into Markdown, writes `docs/Project/wiki/azure/FAQ.md` and `docs/Project/wiki/github/FAQ.md`, and updates `Home.md`, `_Sidebar.md`, and `.order` entries to list the FAQ page. Target is wired into the existing publication target so `./build.ps1` rebuilds the FAQ page alongside the requirements wiki.

## TR-MCP-QA-030

**QA FAQ Wiki Snapshot Tests** — FAQ wiki content tests: a `tests/Build.Tests/FaqWikiPageTests.cs` test invokes `BuildFaqWikiPage` against a fixture FAQ JSON payload, snapshot-compares the generated Markdown to a checked-in expected output to catch unintended formatting changes, and asserts the wiki index files reference the new page.

## TR-MCP-QA-031

**QA Voter History** — Voter-history endpoints (derived from audit): `GET /mcpserver/qa/questions/{id}/voters` and `GET /mcpserver/qa/answers/{id}/voters` return the audit rows for that entity filtered to `Action IN ('vote_up','vote_down','vote_change','vote_revoke')`, projected as `{ actor, action, createdAt }` with paging. Same surface exposed through MCP (`qa_get_voters`), client (`QaClient.GetVotersAsync`), REPL (`workflow.qa.voters`), and PowerShell (`Get-McpQaVoters`). The plugin skill documents this endpoint as the canonical way to answer "who voted on X". A companion `GET .../votes` endpoint returns the current per-voter state (one row per active voter) from `QaVoteEntity` for "what is each voter's current position" queries.

## TR-MCP-QA-032

**QA Vote State Storage** — Per-voter state storage: new `QaVoteEntity` (composite PK `(WorkspaceId, Id)`; columns `EntityKind` (`question`/`answer`), `EntityId`, `VoterActor`, `VoteValue` (`1` or `-1`), `CreatedAt`, `UpdatedAt`). Unique index `(WorkspaceId, EntityKind, EntityId, VoterActor)` enforces one-vote-per-user-per-entity at the database layer (prevents race conditions even under concurrent vote calls). Global query filter on `WorkspaceId` matches other Q&A entities.

## TR-MCP-QA-033

**QA Vote State Machine** — Vote state machine: `EfQaService.VoteAsync(entityKind, entityId, delta, actor)` runs in a single transaction that (a) looks up the existing `QaVoteEntity` row, (b) applies one of `no-op` (same vote already exists), `apply` (no existing vote -> insert + counter +/- 1), `change` (opposite vote exists -> update row + counter +/- 2), or `revoke` (delta is 0 and a vote exists -> delete row + counter -/+ 1), (c) writes the corresponding audit row with action `vote_up` / `vote_down` / `vote_change` / `vote_revoke` (no audit row on `no-op`), (d) updates the Question/Answer counter via atomic `UPDATE`. Returns the resulting state so callers know which branch ran.

## TR-MCP-QA-034

**QA Vote Audit Actions** — Vote audit-action enum extends to: `vote_up`, `vote_down`, `vote_change`, `vote_revoke`. Migration adds the new values to the audit action enum check constraint (where one exists - SQLite stores as string, SqlServer / Postgres via check constraint).

## TR-MCP-REPL-001

**YAML Envelope Protocol** — The REPL host SHALL parse incoming STDIO lines as YAML-formatted command envelopes containing `type`, `payload` with method-specific parameters, and optional `correlationId`/`requestId`. Response envelopes SHALL contain `type` (`result`/`error`/`event`), `payload` with result data or error details, and echoed identifiers. Malformed YAML SHALL emit structured error responses rather than crashing the process.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Core` (`IYamlEnvelope`, `IYamlSerializer`, `IReplProtocol`)

## TR-MCP-REPL-002

**DI-Integrated REPL Host** — The REPL host SHALL use DI composition for workflow and service registration. The command loop SHALL inject scoped service instances per command invocation and SHALL NOT instantiate services via `new` or `ActivatorUtilities.CreateInstance` outside DI registration paths. Workflows SHALL be registered as scoped services and resolved from the service provider.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Host` (`ServiceCollectionExtensions`, `Program.cs`), `McpServer.Repl.Core` workflow interfaces

## TR-MCP-REPL-003

**Command Loop Lifecycle** — The REPL host SHALL support graceful startup with command loop initialization, interactive STDIO processing, structured error handling with typed error codes, and clean shutdown on EOF or explicit exit. The command loop SHALL read YAML envelopes from stdin, dispatch to workflow handlers, serialize responses as YAML to stdout, and maintain session context across commands. Unhandled exceptions SHALL emit structured error responses and continue the loop.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Host` (`Program.cs`, `AgentStdioHandler`, `InteractiveHandler`), `McpServer.Repl.Core` (`SessionLogErrorEnvelope`)

## TR-MCP-REPL-004

**Command Registry and Dispatcher** — Workflow handlers SHALL implement typed interfaces (`ITodoWorkflow`, `ISessionLogWorkflow`, `IRequirementsWorkflow`, `IGenericClientPassthrough`) with async operation methods. Command dispatch SHALL resolve workflow instances from DI per invocation and SHALL pass deserialized parameters as strongly typed method arguments via YamlDotNet model binding. Command routing SHALL map YAML method names to workflow operations.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Core` (`ITodoWorkflow`, `ISessionLogWorkflow`, `IRequirementsWorkflow`, `IGenericClientPassthrough`), `McpServer.Repl.Host` (`TodoWorkflow`, `SessionLogWorkflow`, `RequirementsWorkflow`, `GenericClientPassthrough`)

## TR-MCP-REPL-005

**Namespace Organization and Handler Parity** — Command names SHALL use dot-delimited namespaces: `workflow.todo.*`, `workflow.session.*`, `workflow.requirements.*`, `client.*`. Handler implementations SHALL delegate to existing client contracts (`TodoClient`, `SessionLogClient`, `RequirementsClient`, `ContextClient`, `RepoClient`, `DesktopClient`) without duplicating business logic. Workflows SHALL maintain stateful context (TODO selection, session state) within the REPL process.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Core` (`TodoCommandShapes`, `SessionLogCommandShapes`, `RequirementsCommandShapes`, `ClientCommandShapes`), `McpServer.Repl.Host` (`TodoWorkflow`, `SessionLogWorkflow`, `RequirementsWorkflow`, `GenericClientPassthrough`)

## TR-MCP-REPL-006

**Trust Bootstrap and Token Validation** — The REPL host SHALL implement marker-file trust bootstrap with signature verification and health nonce challenge before accepting operational commands. API key authentication SHALL use per-workspace token semantics from marker files. The host SHALL detect API key rotation between commands via marker file watch and SHALL emit warnings when tokens become stale. Trust verification SHALL use the same contract as PowerShell modules.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Core` (`ITrustBootstrapService`, `IMarkerFileReader`, `IAuthRotationHandler`), `McpServer.Repl.Host` (`AgentStdioHandler`)

## TR-MCP-REPL-007

**State Query Commands** — The REPL host SHALL expose commands for querying workspace state via generic client passthrough: context search, repository operations, desktop launch validation, and requirements operations. Handlers SHALL query current service state snapshots through typed client interfaces without blocking on long-running operations. All client operations SHALL support the generic passthrough pattern for extensibility.
**Status:** ✅ Complete

**Covered by:** `McpServer.Repl.Core` (`IGenericClientPassthrough`, `ClientCommandShapes`), `McpServer.Repl.Host` (`GenericClientPassthrough`)

## TR-MCP-REPL-008

`MarkerFileClientOptionsResolver.TryResolveWithDiagnostics(workspacePathOverride, markerPathOverride, out options, out error)` returns success/failure plus a human-readable diagnostic. The diagnostic enumerates every directory walked, names the marker file when found, and distinguishes "not found" from "malformed" and "signature mismatch". `FindMarkerFile(startPath, out searchedPaths)` exposes the same path list for callers that want raw enumeration. The legacy parameterless `Resolve()` remains for back-compat.

## TR-MCP-REQ-001

**AI Requirements Analysis Service** — `RequirementsService` invokes `ICopilotClient` with a structured prompt containing the TODO item's title, description, technical details, implementation tasks, and pre-existing FR/TR assignments. The prompt instructs Copilot to identify existing FRs/TRs from `docs/Project/` and create new entries for unaddressed functionality, then emit a JSON block with assigned IDs. Response parsing first attempts structured JSON extraction; falls back to regex (`FR-[A-Z]+-\d{3}` / `TR-[A-Z]+-\d{3}`) for robustness. Discovered IDs are merged (deduplicated, order-preserved) back into the TODO via `ITodoService.UpdateAsync`.

## TR-MCP-REQ-002

**Requirements Document Management Service** — `RequirementsDocumentService` parses the canonical requirements documents (`Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`, `TR-per-FR-Mapping.md`) into a strongly typed in-memory model on startup and provides CRUD operations for FR/TR/TEST entries and mapping rows. It renders `Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`, `TR-per-FR-Mapping.md`, and `Requirements-Matrix.md` for exports. Matrix rendering preserves existing matrix rows and appends missing FR/TR/TEST identifiers so generated exports satisfy traceability validation without discarding hand-maintained status/source metadata.
**Covered by:** `RequirementsDocumentService`, `RequirementsDocumentParser`, `RequirementsDocumentRenderer`, `RequirementsOptions`

## TR-MCP-REQ-003

**Requirements REST + STDIO Tool Integration** — The requirements management feature is exposed over REST via RequirementsController at /mcpserver/requirements/* and over STDIO via MCP tools (requirements_list, requirements_generate, requirements_create, requirements_update, requirements_delete). Document generation supports individual Markdown documents, including `doc=matrix` / `docType=matrix` for `Requirements-Matrix.md`, and `doc=all` workspace exports with canonical filenames including `Requirements-Matrix.md`.

## TR-MCP-REQ-004

**Dual Wiki Workspace Renderer** — Requirements document generation SHALL support format=wiki with doc=all, writing both azure/ and github/ folders under docs/Project/wiki and returning workspace export metadata. Each platform folder SHALL include canonical requirements markdown documents, `Requirements-Matrix.md`, and `.mcp-requirements-manifest.json` with generatedAtUtc. Azure Wiki output SHALL include `.order`; GitHub Wiki output SHALL include `_Sidebar.md` and `_Footer.md`. Status: Complete. Covered by `RequirementsWikiDocumentRenderer`, `RequirementsDocumentService`, `RequirementsDatabaseDocumentService`, `RequirementsController`, `RequirementsClient`, `RequirementsWorkflow`, `McpServerMcpTools`.

## TR-MCP-REQ-005

**Wiki Import Selection and Authoritative Sync** — Requirements ingest SHALL accept sourceFormat=auto|canonical|wiki, preferredWikiFormat=azure|github, path-keyed documents, and optional per-document lastModifiedUtc. Wiki import SHALL compare both platform manifest generatedAtUtc values and latest file modified UTC values, fail on disagreement unless a preferred wiki format is supplied, and authoritatively create, update, delete, or ignore FR/TR/TEST/mapping records from the selected folder.

## TR-MCP-REQAC-001

**Acceptance criteria persistence** — Persist requirement acceptance criteria as a nullable AcceptanceCriteriaJson column on RequirementEntity (JSON-column pattern like TodoItemEntity); add migrations for SQLite, SqlServer, PostgreSql; reuse the existing AcceptanceCriterion type.
**Acceptance Criteria:**
- [ ] Source criterion 1
- [ ] Source criterion 2

## TR-MCP-REQAC-002

**Acceptance criteria markdown rendering** — Render acceptance criteria into Functional/Technical/Testing-Requirements.md; the parser remains tolerant of the block; the database remains authoritative.

## TR-MCP-REQAC-PLUGIN-001

**Plugin typed request shaping preserves acceptanceCriteria** — Plugin typed-parameter builders and REPL passthrough binding must emit structured acceptanceCriteria without flattening or dropping nested boolean/list fields.
**Acceptance Criteria:**
- [x] Nested YAML acceptanceCriteria is normalized into typed client request models and persisted by the requirements REST API. (evidence: GenericClientPassthroughYamlBindingTests passed and live workflow/REST round-trip returned AC-CODEXBIND-001.)

## TR-MCP-REQACPLUGIN-001

**Plugin-side schema + shaper changes for AcceptanceCriteria** — Bash plugins gain _repl_emit_acceptance_criteria_block helper and per-method emit/hydrate calls in _repl_requirements_typed_params for createFr/createTr/createTest/updateFr/updateTr/updateTest. TS plugins gain shared AcceptanceCriterion JSON schemas and typedParams pass-through for the same six methods plus per-kind/mixed batch records items.

## TR-MCP-REQEXPORT-001

**Wiki requirement document renderer emits Markdown sections for TEST descriptions and AC** — The requirements wiki renderer shall preserve TEST grouping while emitting each TEST requirement as a heading with description text and a nested Acceptance Criteria checklist generated from the structured acceptanceCriteria field.
**Acceptance Criteria:**
- [x] Wiki testing export output contains per-TEST headings and description paragraphs. (evidence: RequirementsWikiDocumentRenderer now renders grouped TEST entries as Markdown sections headed by TEST requirement IDs.)
- [x] Structured acceptanceCriteria entries render as bullet/checklist list items with evidence when supplied. (evidence: RequirementsDocumentRenderer.AppendAcceptanceCriteria is reused by wiki rendering and focused tests assert checklist output.)

## TR-MCP-SCHEMA-109

**REPL request schema enforcement** — Every YAML or JSON request message exposed through the REPL shall have a published JSON Schema and shall be validated by the REPL before endpoint-backed workflow calls are invoked.

## TR-MCP-SEC-001

**Per-Workspace Auth Tokens** — `WorkspaceResolutionMiddleware` resolves workspace identity per-request using a three-tier chain: (1) `X-Workspace-Path` header, (2) API key reverse lookup via `WorkspaceTokenService`, (3) default workspace from config. `WorkspaceAuthMiddleware` then validates the token against the resolved workspace. `WorkspaceTokenService` generates per-workspace cryptographic tokens (32-byte base64url) on startup and maintains reverse-lookup maps for API key → workspace resolution.

## TR-MCP-SEC-002

**Pairing Session Security** — `PairingSessionService` verifies passwords using SHA-256 with `CryptographicOperations.FixedTimeEquals` for constant-time comparison. Session state is stored in HttpOnly cookies with the Secure flag enabled on HTTPS. `PairingOptions` binds `Mcp:ApiKey` and `Mcp:PairingUsers` from configuration.

## TR-MCP-SEC-003

**Signed Marker Bootstrap and Health Nonce Verification** — `MarkerFileService` SHALL render a top-level marker signature block and a top-level `trust_bootstrap` block into `AGENTS-README-FIRST.yaml` using a deterministic canonical payload. The rendered marker SHALL instruct agents to verify the signature first, generate a nonce for `/health`, require the response to echo the nonce exactly, and stop using MCP endpoints when verification fails. `McpSession`, `McpTodo`, and `McpContext` SHALL share the same trust-verification contract so bootstrap parity is preserved across the public PowerShell modules.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Services/Services/MarkerFileService.cs`, `templates/prompt-templates.yaml`, `src/McpServer.ServiceDefaults/Extensions.cs`, `tools/powershell/McpSession.psm1`, `tools/powershell/McpTodo.psm1`, `tools/powershell/McpContext.psm1`

## TR-MCP-SEC-004

**Provider-Native At-Rest Encryption with No-Loss Transition Procedures** — The storage layer SHALL support optional at-rest encryption using only provider-native or provider-extension facilities: SQLite SEE, PostgreSQL `pg_tde` on Percona Server for PostgreSQL, and native SQL Server TDE. The implementation SHALL detect desired-versus-actual encryption state at startup, SHALL refuse to silently continue when the configured state and live state differ, and SHALL require explicit no-data-loss enable/disable/rotation procedures that preserve existing data when configuration changes. SQL Server LocalDB may be used for provider and migration coverage, but SQL Server TDE validation requires a non-LocalDB SQL Server target.
**Status:** ✅ In Progress

**Covered by:** `src/McpServer.Storage/Database/McpDatabaseProviderFactory.cs`, `src/McpServer.Storage/McpDbContextFactory.cs`, `src/McpServer.Storage/Database/SqliteMcpDatabaseProviderStrategy.cs`, `src/McpServer.Storage/Database/PostgreSqlMcpDatabaseProviderStrategy.cs`, `src/McpServer.Storage/Database/SqlServerMcpDatabaseProviderStrategy.cs`, `src/McpServer.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionCommand.cs`, `src/McpServer.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionRunner.cs`, `scripts/Invoke-McpDatabaseEncryptionTransition.ps1`, `src/McpServer.Storage.SqliteMigrations`, `src/McpServer.Storage.PostgreSqlMigrations`, `src/McpServer.Storage.SqlServerMigrations`

## TR-MCP-SKILLS-001

**Use supported plugin MCP bridge paths** — Skill content uses each plugin's supported MCP bridge or wrapper path and forbids raw REST for normal MCP mutations.

## TR-MCP-SKILLS-002

**Preserve commit-sync pause acknowledgement contract** — commit-sync skill content preserves the pause-and-acknowledge contract before staging, committing, or pushing.

## TR-MCP-SKILLS-003

**Package skills through existing plugin distribution metadata** — Plugin manifests and package metadata expose or package the new skills according to each plugin's existing distribution model.

## TR-MCP-STDIO-109

**Plugin stdio JSON request envelopes** — Codex, Claude, Copilot, and Cline plugins shall instruct direct stdio callers to send one single-line JSON request envelope per message, and plugin bridges that write stdio shall emit that shape.

## TR-MCP-SVC-001

**Windows Service Configuration** — `UseWindowsService(options => { options.ServiceName = "McpServer"; })` in `Program.cs` enables Windows Service hosting. The service is published as a self-contained single-file executable to `C:\ProgramData\McpServer`. The `Manage-McpService.ps1` script handles Install, Uninstall, Start, Stop, Restart, Status, and Publish operations with gsudo elevation. Recovery policy restarts the service on failure with a 60 s delay.

## TR-MCP-TODO-002

**Cross-Workspace TODO Move** — `TodoController.MoveAsync` at `POST /mcpserver/todo/{id}/move` reads the item from the source workspace (resolved via header/API key), creates it in the target workspace (resolved via `IWorkspaceService.GetAsync` + `TodoServiceResolver.Resolve`), then deletes from the source. Request body: `TodoMoveRequest { TargetWorkspacePath }`. Error responses: 400 (null request or unknown target workspace), 404 (item not found), 409 (create failed in target), 500 (created in target but delete from source failed). MCP STDIO parity via `todo_move` tool in `FwhMcpTools`.
**Covered by:** `TodoController`, `FwhMcpTools`, `TodoMoveRequest`, `TodoServiceResolver`, `IWorkspaceService`

## TR-MCP-TODO-003

**GitHub-Backed TODO Creation Alias** — The server SHALL accept `ISSUE-NEW` only on TODO create requests, SHALL immediately create the corresponding GitHub issue, SHALL rewrite the persisted TODO identifier to the canonical `ISSUE-{number}` value returned by GitHub, and SHALL return that canonical identifier to the caller. Persisted TODO validation SHALL accept both `^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+-\d{3}$` and `^ISSUE-\d+$`. Dependency validation SHALL use the same persisted-ID rule set.
The `ISSUE-NEW` flow SHALL be implemented through a shared creation path so HTTP, MCP/STDIO, and voice-driven TODO creation all apply the same rewrite and persistence behavior.

**Status:** ✅ Complete

**Covered by:** `TodoCreationService`, `TodoValidator`, `TodoController`, `FwhMcpTools`, `VoiceConversationService`, `TodoService`, `EfTodoService`

## TR-MCP-TODO-004

**Shared ISSUE-* TODO Update Orchestration** — All server-side TODO update entry points that can mutate existing TODO items SHALL route `ISSUE-{number}` updates through shared orchestration instead of writing directly to the TODO store. The shared path SHALL suppress description changes after first sync, SHALL reuse the existing TODO store for the local mutation, and SHALL trigger the GitHub sync/comment flow after a successful local update.
**Status:** ✅ Complete

**Covered by:** `TodoUpdateService`, `TodoController`, `FwhMcpTools`, `VoiceConversationService`

## TR-MCP-TODO-005

**Provider-Agnostic Database-Authoritative TODO Storage with Deterministic YAML Projection** — The TODO subsystem SHALL use the configured `Mcp:Database:Provider` (SQLite, SQL Server, or PostgreSQL) via `McpDatabaseProviderFactory` (TR-MCP-CFG-007) as the authoritative current-state store for workspace TODO items. Service initialization SHALL perform EF Core schema migration, one-time bootstrap import from an existing `TODO.yaml` when the authoritative database is empty, and deterministic projection back to the configured TODO YAML path after successful mutations. The authoritative store SHALL preserve projection metadata needed to rehydrate ordered sections, `code-review-remediation` phases, `notes`, `completed`, and the code-review reference without treating YAML as runtime source of truth.
Projection failures after a committed authoritative mutation SHALL surface an explicit failure result instead of silent success. The TODO storage provider setting SHALL accept `yaml` or `database`; the legacy `sqlite` value SHALL be accepted and aliased to `database` with a one-time warning log. No sqlite-specific settings SHALL be consulted when provider is `database`.

**Status:** ✅ Complete

**Covered by:** `EfTodoService`, `TodoItemEntity`, `TodoAuditHistoryEntity`, `TodoDocumentMetadataEntity`, `McpDbContext` (Todo DbSets), `McpDatabaseProviderFactory`, `TodoYamlFileSerializer`, `TodoServiceFactory`, `TodoStorageOptions`, `McpInstanceResolver`, `appsettings.yaml`, `appsettings.Staging.yaml`, `src/McpServer.Support.Mcp/appsettings.yaml`, `src/McpServer.Support.Mcp/appsettings.Staging.yaml`

## TR-MCP-TODO-006

**Append-Only TODO Audit History, Projection Failure Classification, and Repair Contract** — TODO create, update, delete, and bootstrap-import operations SHALL append reconstructable audit snapshots with monotonic per-item versions. The server SHALL expose `GET /mcpserver/todo/{id}/audit` together with typed client parity and MCP STDIO tool parity so callers can retrieve ordered tracked states for a TODO item even when the current row has been deleted but audit history still exists.
Mutation results SHALL include a machine-readable failure classification so callers can distinguish validation, not-found, projection-failure, conflict, and external-sync error shapes when TODO operations fail or only partially succeed. For database-backed TODO storage (the authoritative mode per TR-MCP-TODO-005), a projection failure SHALL preserve committed authoritative database state, record operator-visible projection failure metadata, and leave `TODO.yaml` repairable without replaying the mutation. The server SHALL expose `GET /mcpserver/todo/projection/status` and `POST /mcpserver/todo/projection/repair` together with typed client parity and MCP STDIO tool parity so operators can verify whether `TODO.yaml` matches authoritative database state and rebuild it on demand.

**Status:** ✅ Complete

**Covered by:** `ITodoService`, `ITodoStore`, `EfTodoService`, `TodoAuditHistoryEntity`, `TodoYamlFileSerializer`, `TodoController`, `McpServerMcpTools`, `TodoClient`, `TodoModels`, `TodoCreationService`, `TodoUpdateService`

## TR-MCP-TODO-007

**Legacy SQLite TODO Storage One-Shot Migration** — When TR-MCP-TODO-005 provider-agnostic storage is enabled and a pre-existing legacy `mcp.db` SQLite TODO store is present at the deprecated `Mcp:TodoStorage:SqliteDataSource` path, the server SHALL copy rows from `todo_items`, `todo_item_history`, and `todo_document_metadata` into the configured authoritative database on first boot, preserving primary keys, audit identifiers, and monotonic per-item versions. The migrator SHALL be idempotent: subsequent starts SHALL be no-ops when the target TODO tables are non-empty or the completion marker file exists in the effective data folder. The migrator SHALL honor the `Mcp:TodoStorage:MigrateFromLegacySqlite` feature flag and SHALL run as a background hosted service so it never blocks the SCM 30-second service-start window. Failures SHALL log per-row context and continue with the next row rather than aborting the whole migration.
**Status:** ✅ Superseded by TR-MCP-TODO-008

**Note:** In practice the provider-agnostic rewrite (TR-MCP-TODO-005) was shipped alongside the workspace-scoped schema (TR-MCP-TODO-008), whose per-workspace YAML bootstrap (`TodoBootstrapImporter`) replaces the need for a legacy `mcp.db` row copy. Live Phase 5 deploys on LEGION2 and PAYTON-DESKTOP (2026-04-21) reconstructed authoritative state from workspace YAML directly; legacy SQLite TODO rows are out-of-band archived to CSV when present (see `C:\Users\Public\mcpserver-postgres-archive-20260421` on PAYTON for the postgres/SQLite snapshot archived during deploy).

**Covered by:** `TodoBootstrapImporter` (replaces legacy migrator), per-workspace YAML bootstrap path

## TR-MCP-TODO-008

**Workspace-Scoped Database-Backed TODO Storage with Per-Workspace YAML Bootstrap** — Database-backed TODO storage (TR-MCP-TODO-005) SHALL scope every TODO row, audit-history row, and document-metadata row to the active workspace via a `WorkspaceId` column populated from the resolved `WorkspaceContext.WorkspacePath`, matching the TR-MCP-MT-003 multi-tenant pattern used by context, session-log, agent, tool, and graph entities. `McpDbContext` SHALL install a global query filter on all three Todo entities so reads, updates, and deletes never cross workspace boundaries. `TodoItemEntity` SHALL use composite primary key `(WorkspaceId, Id)` so the same canonical TODO id MAY exist in multiple workspaces without collision. `TodoDocumentMetadataEntity` SHALL use composite primary key `(WorkspaceId, SingletonId = 1)` so each workspace owns exactly one document-metadata singleton. `TodoAuditHistoryEntity` SHALL carry `WorkspaceId` as a filter column and index; the audit primary key remains `(TodoId, Version)` scoped implicitly by the query filter.
Bootstrap SHALL import from the per-workspace `TodoFilePath` YAML into the authoritative database when that workspace's TODO rows are empty, running exactly once per workspace per marker-file lifetime. The bootstrap path SHALL preserve ordered sections, completed items, notes, code-review reference, and projection metadata identically to the single-workspace bootstrap shape used by `TodoService`. After bootstrap, YAML projection SHALL write to the workspace-specific `TodoFilePath`; no other workspace's YAML SHALL be touched.

The `LegacyTodoSqliteMigrator` (TR-MCP-TODO-007) SHALL stamp imported rows with the active workspace's `WorkspacePath`. REST routes `/mcpserver/todo/*` and MCP STDIO `todo_*` tools SHALL honor the workspace resolved by the existing `WorkspaceAuthMiddleware` / `X-Workspace` header path without additional caller changes beyond what TR-MCP-MT-003 already mandates.

**Status:** ✅ Complete

**Note:** Phase 5 live deploy verified on 2026-04-21: LEGION2 bootstrapped 130 items across 7 workspaces; PAYTON-DESKTOP bootstrapped 96 items across 5 workspaces (workspace scoping confirmed via `TodoBootstrapImporter summary` log lines with per-workspace `Imported:N` outcomes).

**Covered by:** `TodoItemEntity`, `TodoAuditHistoryEntity`, `TodoDocumentMetadataEntity`, `McpDbContext` (query filters + composite keys), `EfTodoService`, `TodoBootstrapImporter`, `TodoServiceFactory.CreateForWorkspace`, per-provider migration assemblies

## TR-MCP-TODO-009

**Preserve TODO description Markdown** — TODO persistence, plugin/client update paths, database storage, audit rows, and informational projections must treat description as Markdown, preserving blank lines, indentation, code fences, list spacing, and trailing content without trimming meaningful formatting.

## TR-MCP-TODO-010

**Root-scoped TODO done serialization** — TODO update serializers in MCP plugin wrappers SHALL read the parent done field only from the request root and SHALL NOT derive it from nested implementationTasks[].done values. Structured root-level parsing is required for boolean root fields when building HTTP or workflow update bodies.
**Acceptance Criteria:**
- [x] Root-level done serialization ignores nested implementationTasks[].done values. (evidence: Plugin tests/repl-invoke-shim.bats now asserts no top-level done is emitted when only implementation task done values are present.)

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

## TR-MCP-TPL-007

**Marker template requires actionable requirements-backed plans** — The default marker prompt must instruct every agent in every workspace to make plans decision-complete, capture FR/TR/TEST requirements, include explicit TDD unit-test expectations, and preserve Byrd gates so implementation agents can execute the plan directly.

## TR-MCP-TR-001

**Tool Registry Service** — Keyword search across tool tags (bidirectional singular/plural contains matching), name, and description. Results combine global tools (`WorkspacePath == null`) with workspace-scoped tools. Full CRUD for `ToolDefinitionEntity` and `ToolDefinitionTagEntity`.

## TR-MCP-TR-002

**Tool Bucket Service** — GitHub repository browsing via `gh api /repos/{owner}/{repo}/contents{path}?ref={branch}`. Reads and parses `stdio-tool-contract.json` manifests for install and sync operations. Persists bucket state to `ToolBucketEntity`.

## TR-MCP-TR-003

**Tool Registry Default Bucket Seeding** — On startup, `Program.cs` reads `Mcp:ToolRegistry:DefaultBuckets` and calls `IToolBucketService.EnsureDefaultBucketsAsync` to register any configured buckets not already in the database. Idempotent: existing buckets are not modified.

## TR-MCP-TUN-001

**Tunnel Strategy Pattern** — DI registration in `Program.cs` reads `Mcp:Tunnel:Provider`, normalizes to uppercase, and uses `ActivatorUtilities.CreateInstance<T>` to instantiate the matching provider (`NgrokTunnelProvider`, `CloudflareTunnelProvider`, or `FrpTunnelProvider`). The provider is registered as both a singleton and an `IHostedService`, conditionally on the provider name being non-empty.

## TR-MCP-TUN-002

**Tunnel Process Lifecycle** — `Process.Kill()` is wrapped in a try-catch for `InvalidOperationException` to handle races. `WaitForExit(5000)` enforces a 5 s shutdown timeout. FRP config files written to temp storage are deleted on stop. All three providers log start, stop, and error events.

## TR-MCP-TUN-003

**Ngrok Auth Token Security** — The ngrok auth token is passed via the `NGROK_AUTHTOKEN` environment variable on the child process, rather than as a CLI argument, to prevent exposure in process listings and shell history.

## TR-MCP-VOICE-001

**Voice Conversation Service** — `VoiceConversationService` manages the full voice session lifecycle: session creation with `CopilotInteractiveSession` spawned via `DesktopProcessLauncher` (or standard `Process.Start`), turn processing with tool-call loop (max `MaxToolSteps` iterations), in-memory transcript storage, tool-call record tracking, and session cleanup. Configurable via `VoiceConversationOptions` bound from `Mcp:Voice` configuration section (model, timeouts, rate limits for writes/deletes per turn, transcript context limit).
**Covered by:** `VoiceConversationService`, `VoiceConversationOptions`, `CopilotInteractiveSession`

## TR-MCP-VOICE-002

**Voice Controller REST API** — `VoiceController` at `/mcpserver/voice/session/*` exposes 8 endpoints: `POST /` (create session with `DeviceId`/`Language`/`ClientName`), `GET /?deviceId=` (find by device), `POST /{id}/turn` (synchronous turn), `POST /{id}/turn/stream` (SSE streaming turn), `POST /{id}/interrupt` (cancel active turn), `POST /{id}/escape` (send ESC chars to Copilot stdin), `GET /{id}` (session status), `GET /{id}/transcript` (transcript entries), `DELETE /{id}` (destroy session). DTOs: `VoiceSessionCreateRequest/Response`, `VoiceTurnRequest/Response`, `VoiceInterruptResponse`, `VoiceSessionStatusDto`, `VoiceTranscriptEntryDto/Response`, `VoiceToolCallRecordDto`, `VoiceTurnStreamEvent`.
**Covered by:** `VoiceController`, `VoiceConversationContracts`

## TR-MCP-VOICE-003

**Voice Session Lifecycle Management** — One active session per device enforced via `DeviceId` lookup; creating a new session for a device with an active session returns the existing session. Idle timeout (`SessionIdleTimeoutMinutes`, default 15) triggers the configured idle-shutdown prompt sent to Copilot, waits for the configured sentinel response, then terminates the session. `UseDesktopLaunch` option (default true) selects `CreateProcessAsUser` for Windows service context.
**Covered by:** `VoiceConversationService.OnIdleCleanupTick` / `CleanupIdleSessionsAsync` (60s timer-driven idle-shutdown orchestrator), `VoiceConversationOptions.SessionIdleTimeoutMinutes`, `VoiceConversationOptions.IdleShutdownCommand` (config string sent to Copilot), `VoiceConversationOptions.IdleShutdownSentinel` (config string awaited as the shutdown confirmation). Note: `IdleShutdownCommand` and `IdleShutdownSentinel` are configured strings on `VoiceConversationOptions`, not CLR message types.

## TR-MCP-VOICE-004

**Interactive Presence Signals on Stream State Changes** — On interactive stream disconnect, the runtime SHALL send `User is AFK.` to the associated interactive agent session.
On interactive stream reconnect, after response stream establishment, the runtime SHALL send `User is here.` to the associated interactive agent session.

Presence signaling SHALL be excluded from one-shot sessions.

**Status:** 🔴 Planned

**Covered by:** `VoiceConversationService` *(planned extension)*, `AgentPoolStreamService` *(planned)*

## TR-MCP-WEB-001

**TR-MCP-WEB-001** — Placeholder requirement backfilled by DB-FK-001.

## TR-MCP-WEB-002

**TR-MCP-WEB-002** — Placeholder requirement backfilled by DB-FK-001.

## TR-MCP-WEB-003

**TR-MCP-WEB-003** — Placeholder requirement backfilled by DB-FK-001.

## TR-MCP-WEB-004

**TR-MCP-WEB-004** — Placeholder requirement backfilled by DB-FK-001.

## TR-MCP-WS-002

**Workspace Service** — CRUD operations for workspace entities persisted in EF Core SQLite. Auto-port assignment starts at base 7147 and increments from the current maximum registered port. Init scaffolding creates the workspace directory, `docs/Project/TODO.yaml`, `docs/sessions/`, `docs/external/`, and `mcp.db`.

## TR-MCP-WS-003

**Workspace Process Manager** — Manages workspace marker file lifecycle. On startup, generates tokens and writes `AGENTS-README-FIRST.yaml` marker files for all registered workspaces - all pointing to the single shared host port. On stop, removes marker files. No longer spawns child `WebApplication` instances (replaced by single-app multi-tenant model, see TR-MCP-MT-001 through TR-MCP-MT-003).

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

## TR-MCP-WS-009

**Primary Workspace Detection and IsEnabled Gating** — `WorkspaceProcessManager.IHostedService.StartAsync` resolves the primary workspace: first by `IsPrimary = true` + lowest port among enabled workspaces; then by lowest-port enabled workspace if none is marked primary. For the primary workspace, only a marker file is written - no child `WebApplication` is created. Workspaces with `IsEnabled = false` are skipped during auto-start but can be started manually.

## TR-MCP-WS-UI-001

**McpServer Management Web UI** — Reserved/planned: web-based management UI for workspace and server administration. Tracks FR-MCP-031.

## TR-PLANNED-013A

`AddControllers().ConfigureApiBehaviorOptions` installs an `InvalidModelStateResponseFactory` that produces `application/problem+json` responses for body-binding failures on `/mcpserver/*` endpoints. The factory strips the action parameter name (`dto`, `body`, `turn`) from the `errors` keys, replacing them with `$` so callers see the canonical JSON root marker instead of a misleading wrapper field name. `SessionLogController.SubmitAsync` and `GetByIdAsync` use `ValidationProblem` for domain validation to keep the response shape uniform.

## TR-TEST-001

**TR-TEST-001** — Placeholder requirement backfilled for TODO link TR-TEST-001.
