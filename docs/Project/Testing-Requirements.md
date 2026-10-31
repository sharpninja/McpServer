# Testing Requirements (MCP Server)

- TEST-GRAPHRAG-ADHOC-001: GraphEntityEntity/GraphRelationshipEntity persist with all fields, workspace isolation, cascade delete, FK validation, and RemoveVector correctness.
  Scope: layer-1+
- TEST-GRAPHRAG-ADHOC-002: IngestTextAsync creates document + chunks, generates embeddings, registers vectors, handles empty content, defaults SourceType/SourceKey, and optionally triggers reindex.
  Scope: layer-1+
- TEST-GRAPHRAG-ADHOC-003: ListDocumentsAsync pagination and filtering, GetDocumentChunksAsync ordering, DeleteDocumentAsync cascade and vector cleanup.
  Scope: layer-1+
- TEST-GRAPHRAG-ADHOC-004: Create/Get/Update/List/Delete for entities and relationships with ID generation, timestamp management, FK validation, cascade behavior.
  Scope: layer-1+
- TEST-GRAPHRAG-ADHOC-005: All CQRS command and query handlers delegate to IGraphRagService and wrap results in Result<T>.
  Scope: layer-1+
- TEST-GRAPHRAG-ADHOC-006: All 14 controller actions return correct HTTP status codes, content types, and error responses.
  Scope: layer-1+
- TEST-GRAPHRAG-ADHOC-007: MCP tools serialize correctly, REPL workflow delegates to ContextClient, McpAgent tool adapter exposes all 14 tools.
  Scope: layer-1+
- TEST-HANDOFF-001: Cover documented request, result, draft, provenance, and diagnostic contracts, including typed-client serialization.
  Scope: layer-1+
- TEST-HANDOFF-002: Cover bounded readers, workspace containment, and rejection of missing, unsupported, oversized, traversal, external, and reparse-escaping sources.
  Scope: layer-1+
- TEST-HANDOFF-003: Cover versioned HandoffTodoDraft extraction and strict JSON parsing, including malformed AI output.
  Scope: layer-1+
- TEST-HANDOFF-004: Cover draft validation, field-specific diagnostics, DraftOnly, RequireReview, CreateWhenConfident, low confidence, and ambiguous handoffs.
  Scope: layer-1+
- TEST-HANDOFF-005: Cover exclusive TODO-service persistence, exact-one creation, deterministic replay, ID collisions, approval races, and TODO-service failure.
  Scope: layer-1+
- TEST-HANDOFF-006: Cover API, client, REPL, Director, MCP-tool, and plugin-skill inventory and invocation parity, including workspace isolation.
  Scope: layer-1+
- TEST-HANDOFF-007: Cover normalized run storage, diagnostic persistence, cancellation, and the prohibition on logging credentials or raw source content.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Sqlite_HandoffMigration_DowngradeAndReupgrade, PostgreSql_HandoffMigration_DowngradeAndReupgrade and SqlServer_HandoffMigration_DowngradeAndReupgrade: on a clean database migrated only up to AddHandoffIngestionStorage, a downgrade to the immediately preceding migration removes AddHandoffIngestionStorage from the applied migrations and a re-upgrade restores it; proven first by HandoffCycleAssertion_AcceptsReversibleCycleAndRejectsMissingOrForwardOnlyCrossing over applied-migration lists (negative controls: the handoff migration still applied after the downgrade, absent after the re-upgrade, and a downgrade target older than 20260929140000_VersionedAuditPayloads) (PLAN-SESSIONTRIAGEFIX-001 B12, triage-report-61c2a2b2).
  - [ ] The same tests then migrate the database to head, where a handoff ingestion run and its diagnostic persist and read back (PLAN-SESSIONTRIAGEFIX-001 B12).
  - [ ] VersionedAuditPayloads_DownIsForwardOnly_OnEveryProvider: 20260929140000_VersionedAuditPayloads' Down raises NotSupportedException on SQLite, PostgreSQL and SQL Server, the forward-only boundary that migration declares, so no downgrade test crosses it (PLAN-SESSIONTRIAGEFIX-001 B12).
- TEST-MCP-001: Given configurable RepoRoot/Todo paths, when service starts, then path resolution is correct.
  Scope: layer-1+
- TEST-MCP-002: Given TODO API operations, when create/update/delete/query run, then contracts remain stable.
  Scope: layer-1+
- TEST-MCP-003: Given multi-tenant workspace configuration, when requests are made with different `X-Workspace-Path` headers, then data remains isolated per workspace on the single shared port.
  Scope: layer-1+
- TEST-MCP-004: Given vector + FTS data, when context search executes, then hybrid results are returned.
  Scope: layer-1+
- TEST-MCP-005: Given GitHub sync enabled, when issue sync runs, then ISSUE-* mapping is consistent.
  Scope: layer-1+
- TEST-MCP-006: Given STDIO mode, when tool requests are sent, then parity with HTTP behavior is preserved.
  Scope: layer-1+
- TEST-MCP-007: Given workspace registration, when a workspace is created, then its directory scaffold is created and an `AGENTS-README-FIRST.yaml` marker file is written to its root pointing to the shared host port.
  Scope: layer-1+
- TEST-MCP-008: Given tool registry with tags, when keyword search runs with a singular or plural term, then matching tools from both global and workspace scopes are returned. Given default buckets in config, when the server starts for the first time, then buckets are seeded and idempotent on subsequent starts.
  Scope: layer-1+
- TEST-MCP-009: Given per-workspace auth tokens and `X-Workspace-Path` header resolution, when a request to any `/mcpserver/*` endpoint lacks `X-Api-Key`, then the server returns 401. When a valid token is provided, workspace resolution uses the three-tier chain: `X-Workspace-Path` header → API key reverse lookup → default workspace.
  Scope: layer-1+
- TEST-MCP-010: Given valid pairing credentials, when the `/pair` login flow completes, then an HttpOnly session cookie is issued and the API key is returned. Given constant-time comparison, when two passwords of the same length differ by one character, then timing side-channel is not exploitable.
  Scope: layer-1+
- TEST-MCP-011: Given a configured tunnel provider, when the hosted service starts, then the tunnel process launches and `GetStatusAsync` returns a public URL. When the service stops, the process is terminated within 5 s.
  Scope: layer-1+
- TEST-MCP-012: Given an MCP client connecting to `/mcp-transport`, when a tool call is made, then the response is semantically equivalent to the corresponding REST endpoint result. Given a request without the required `Accept` header, then the endpoint returns 406.
  Scope: layer-1+
- TEST-MCP-013: Given a workspace, when `StartAsync` completes, then `AGENTS-README-FIRST.yaml` exists at the workspace root with the shared host port, endpoint paths, and auth token. When `StopAsync` completes, then the marker file is removed.
  Scope: layer-1+
- TEST-MCP-014: Given a TODO item with a title and description, when `RequirementsService.AnalyzeAsync` is called, then `ExtractRequirementIds` correctly parses both JSON-block and regex-fallback response formats and returns distinct, non-empty FR/TR ID lists.
  Scope: layer-1+
- TEST-MCP-015: Given a Markdown file with a `# Session Log - {title}` header, when `MarkdownSessionLogParser.TryParse` is called, then it returns a `UnifiedSessionLogDto` with matching title, model, status, and at least one entry. Given a file without the header, then `TryParse` returns null.
  Scope: layer-1+
- TEST-MCP-026: Given a CQRS Dispatcher with a registered command handler, when `SendAsync` is called with a valid command, then the handler is invoked and `Result<T>.IsSuccess` is true with the expected value.
  Scope: layer-1+
- TEST-MCP-027: Given a CQRS command handler that returns `Result.Failure(error)`, when the Dispatcher processes the result, then it logs at Warning level with the error message and correlation context.
  Scope: layer-1+
- TEST-MCP-028: Given a CQRS command handler that throws an exception, when the Dispatcher catches it, then `Result<T>.IsFailure` is true, `Result<T>.Exception` is set, and the Dispatcher logs at Error level with exception details.
  Scope: layer-1+
- TEST-MCP-029: Given a new `CorrelationId`, when `Next()` is called multiple times, then the base ID remains stable and the counter increments sequentially. Given a correlation string `"12345678.3"`, when `CorrelationId.Parse` is called, then `BaseId` is `12345678` and the counter is `3`.
  Scope: layer-1+
- TEST-MCP-030: Given a `CallContext` used as `ILogger`, when `LogInformation` is called, then the log entry is captured in `CallContext.LogEntries` with the correct level, message, and timestamp. Given the Dispatcher's `ILoggerProvider`, when a `DispatcherLogger` emits a log with a correlation scope, then the structured output includes `correlationBaseId` and `correlationStep` as separate fields.
  Scope: layer-1+
- TEST-MCP-031: Given two pipeline behaviors registered in order, when a command is dispatched, then the first behavior's pre-processing runs before the second, and the second's post-processing runs before the first's. Given a behavior that returns `Result.Failure` without calling `next`, then the handler is never invoked.
  Scope: layer-1+
- TEST-MCP-032: Given an empty database, when `AgentService.SeedBuiltInDefaultsAsync` is called, then 7 built-in agent definitions are created. Given a database already containing built-in agents, when `SeedBuiltInDefaultsAsync` is called again, then no duplicates are created (idempotent).
  Scope: layer-1+
- TEST-MCP-033: Given the AgentService, when `UpsertDefinitionAsync` creates a new definition and `GetDefinitionAsync` retrieves it, then all fields match. When `DeleteDefinitionAsync` is called on a non-built-in definition, then it succeeds. When called on a built-in definition, then it returns `IsFailure` with an appropriate error.
  Scope: layer-1+
- TEST-MCP-034: Given a workspace with an agent configured, when `BanAgentAsync` is called with `Global = false`, then only that workspace's agent is banned. When called with `Global = true`, then all workspaces with that agent are banned. When `UnbanAgentAsync` is called, then the agent is re-enabled.
  Scope: layer-1+
- TEST-MCP-035: Given a Director `LoginHandler`, when the device authorization flow completes successfully, then `Result<LoginResult>.IsSuccess` is true and the token is cached. When the flow times out, then `Result<LoginResult>.IsFailure` with an appropriate error.
  Scope: layer-1+
- TEST-MCP-036: Given a Director `LaunchAgentHandler`, when the agent is enabled and not banned, then the agent process is spawned and `Result<LaunchResult>.IsSuccess`. When the agent is banned, then `Result<LaunchResult>.IsFailure` with a ban reason.
  Scope: layer-1+
- TEST-MCP-037: Given a Director `InitWorkspaceHandler`, when called on a valid workspace path, then `agents.yaml` is created and agents are registered via the MCP Server API. When the workspace path doesn't exist, then `Result.IsFailure`.
  Scope: layer-1+
- TEST-MCP-038: Given a Director `BanAgentHandler`, when called with a valid agent ID and reason, then the MCP Server API is called to ban the agent and a Ban event is logged. When the agent doesn't exist, then `Result.IsFailure`.
  Scope: layer-1+
- TEST-MCP-039: Given seeded canonical requirements docs, when `RequirementsDocumentService` loads them, then FR/TR/TEST/mapping entries are parsed correctly and generated Markdown preserves the canonical header and entry formats.
  Scope: layer-1+
- TEST-MCP-040: Given `/mcpserver/requirements` CRUD endpoints, when an FR entry is created, generated via `/mcpserver/requirements/generate?doc=functional`, and deleted, then the generated document reflects each mutation and the deleted entry is no longer returned.
  Scope: layer-1+
- TEST-MCP-041: Given /mcpserver/requirements/generate?doc=all, when the endpoint is called, then it writes Functional-Requirements.md, Technical-Requirements.md, Testing-Requirements.md, TR-per-FR-Mapping.md, and Requirements-Matrix.md to the workspace, preserves existing matrix rows, appends missing FR/TR/TEST IDs, and returns export metadata.
  Scope: layer-1+
- TEST-MCP-042: Given concurrent requirement mutations, when `RequirementsDocumentService` persists updates, then writes remain atomic and the resulting Markdown files remain parseable without temp-file residue.
  Scope: layer-1+
- TEST-MCP-043: Given MCP STDIO requirements tools (`requirements_list`, `requirements_generate`, `requirements_create`, `requirements_update`, `requirements_delete`), when agents invoke them, then results are semantically equivalent to the corresponding REST requirements endpoints.
  Scope: layer-1+
- TEST-MCP-044: Given the three-tier workspace resolution chain, when `X-Workspace-Path` header is present, then it takes priority over API key reverse lookup; when absent, API key resolves workspace; when neither is present, the default workspace is used.
  Scope: layer-1+
- TEST-MCP-045: Given EF Core global query filter on `WorkspaceId`, when entities are inserted for workspace A and queried from workspace B context, then workspace A's entities are not visible from workspace B.
  Scope: layer-1+
- TEST-MCP-046: Given the Director client with `WorkspacePath` set, when API calls are made, then the `X-Workspace-Path` header is sent on all requests and workspace switching only changes the header, not the base URL.
  Scope: layer-1+
- TEST-MCP-047: Given the typed client library with `McpServerClientOptions.WorkspacePath` set, when requests are sent, then the `X-Workspace-Path` header is present alongside `X-Api-Key`.
  Scope: layer-1+
- TEST-MCP-048: Given a TODO item in workspace A, when `POST /mcpserver/todo/{id}/move` is called with `targetWorkspacePath` pointing to workspace B, then the item is created in workspace B with all fields preserved and deleted from workspace A. Given an invalid target workspace path, then the endpoint returns 400.
  Scope: layer-1+
- TEST-MCP-049: Given voice conversation endpoints, when a session is created with a `DeviceId`, then `GET /mcpserver/voice/session?deviceId=` returns the active session. When `DELETE` is called, then the session is destroyed and subsequent status queries return 404.
  Scope: layer-1+
- TEST-MCP-050: Given an active voice session, when `POST /mcpserver/voice/session/{id}/turn` is called with transcript text, then a `VoiceTurnResponse` is returned with assistant text. When `POST /mcpserver/voice/session/{id}/turn/stream` is called, then SSE events are streamed with `type` values of `chunk`, `tool_status`, `done`, or `error`.
  Scope: layer-1+
- TEST-MCP-051: Given a voice session with `SessionIdleTimeoutMinutes` configured, when the session is idle beyond the timeout, then the `IdleShutdownCommand` is sent and the session is terminated. Given a device with an active session, when a new session is requested for the same device, then the existing session is returned (one-per-device enforcement).
  Scope: layer-1+
- TEST-MCP-052: Given a Windows service running as LocalSystem, when `DesktopProcessLauncher.LaunchWithStdio` is called, then a process is created on the interactive desktop with redirected stdio pipes via `CreateProcessAsUser`. Given `LaunchVisible`, then a visible console window is created.
  Scope: layer-1+
- TEST-MCP-053: Given an `appsettings.yaml` file with configuration overrides, when the server starts, then YAML values override `appsettings.json` values for matching keys. Given the YAML file is absent, then startup succeeds using JSON configuration only.
  Scope: layer-1+
- TEST-MCP-054: Given the template service with a YAML file, when `GET /mcpserver/templates` is called with optional `category`, `tag`, and `keyword` query parameters, then a filtered list of `PromptTemplate` items is returned. When no filters are provided, all templates are returned.
  Scope: layer-1+
- TEST-MCP-055: Given a valid `PromptTemplateCreateRequest`, when `POST /mcpserver/templates` is called, then a new template is persisted to YAML and returned with 201 Created. When a duplicate ID is submitted, then 409 Conflict is returned. When `PUT /mcpserver/templates/{id}` is called with partial update fields, only specified fields are changed. When `DELETE /mcpserver/templates/{id}` is called, the template is removed.
  Scope: layer-1+
- TEST-MCP-056: Given a stored template with Handlebars content and declared variables, when `POST /mcpserver/templates/{id}/test` is called with variable values, then `PromptTemplateTestResult` contains the rendered content. When required variables are missing, then `MissingVariables` is populated and `Success` is false. When `POST /mcpserver/templates/test` is called with inline template content, the inline content is rendered without requiring a stored template.
  Scope: layer-1+
- TEST-MCP-057: Given the Director TUI with a Templates tab, when `TemplateListViewModel.RefreshCommand` executes, then the CQRS `ListTemplatesQuery` flows through `ListTemplatesQueryHandler` → `ITemplateApiClient` → `TemplateClient` → REST API and populates the table view. When a template is selected and the test action is invoked via `TemplateDetailViewModel`, then `TestTemplateQuery` renders the template and displays output.
  Scope: layer-1+
- TEST-MCP-058: Given `FileMarkerPromptProvider` with a valid `templates/default-marker-prompt.hbs.yaml` file, when `GetGlobalPromptTemplateAsync` is called, then the template content is returned and cached. When the file is missing, then `null` is returned and `MarkerFileService.DefaultPromptTemplate` is used as fallback.
  Scope: layer-1+
- TEST-MCP-059: Given `TodoPromptProvider` with todo prompt templates stored in `IPromptTemplateService` by well-known IDs (`todo-status-prompt`, `todo-implement-prompt`, `todo-plan-prompt`), when the provider is queried, then file-loaded content is returned. When templates are missing from the store, then `TodoPromptDefaults` built-in constants are returned as fallback.
  Scope: layer-1+
- TEST-MCP-060: Given `PairingHtmlRenderer` with pairing HTML templates stored in `IPromptTemplateService` by well-known IDs (`pairing-login-page`, `pairing-key-page`, `pairing-not-configured-page`), when rendering methods are called with substitution parameters, then `{errorBanner}`, `{apiKey}`, and `{serverUrl}` tokens are replaced. When templates are missing, then `PairingHtml` static method output is returned as fallback.
  Scope: layer-1+
- TEST-MCP-061: Given `Mcp:AgentPool:Agents` configuration, when options bind and validate, then each definition accepts `AgentName`, `AgentPath`, `AgentModel`, `AgentSeed`, `AgentParameters`, `IsInteractiveDefault`, `IsTodoPlanDefault`, `IsTodoStatusDefault`, and `IsTodoImplementDefault`. Duplicate `AgentName` values (case-insensitive) or ambiguous defaults fail validation.
  Scope: layer-1+
- TEST-MCP-062: Given one-shot or interactive requests with no `AgentName`, when context is `Plan`, `Status`, `Implement`, or `AdHoc`, then the default agent mapped for that intent is selected. Given an explicit `AgentName`, explicit assignment overrides default routing.
  Scope: layer-1+
- TEST-MCP-063: Given one-shot request payloads, when both `promptTemplateId` and ad-hoc `promptText` are supplied or both are missing without resolvable context template, then the API returns 400. Given template-resolved mode, missing `id` returns 400.
  Scope: layer-1+
- TEST-MCP-064: Given one-shot context without template ID, when context is `Plan`, `Status`, or `Implement`, then existing context-based template resolution is used. Given context `AdHoc` without template ID and no ad-hoc prompt text, then the API returns 400.
  Scope: layer-1+
- TEST-MCP-065: Given prompt resolution requests with template ID, caller values, workspace-context values, and `id`, when rendering executes, then output includes `{id}` substitution and caller values override workspace-context values on key conflicts.
  Scope: layer-1+
- TEST-MCP-066: Given no eligible pooled agent is idle, when one-shot requests are enqueued, then requests remain `queued` and transition to `processing` once an eligible agent becomes available, followed by terminal states (`completed`, `failed`, or `canceled`).
  Scope: layer-1+
- TEST-MCP-067: Given queue operations, when move up/down is requested for queued items, then order changes correctly; when requested for the currently processing item, the operation is rejected. Cancel/remove semantics correctly update queue state and persisted metadata.
  Scope: layer-1+
- TEST-MCP-068: Given pool lifecycle transitions, when queued/processing/completed/failed events occur, then notification SSE emits events in order with payload fields `AgentName`, `LastRequestPrompt`, and `SessionId`.
  Scope: layer-1+
- TEST-MCP-069: Given multiple clients connected to a read-only response stream, when one client disconnects, then remaining subscribers continue receiving stream data and active pooled work is unaffected.
  Scope: layer-1+
- TEST-MCP-070: Given a pooled agent processing a one-shot request, when an interactive voice connection targets that agent, then interactive linkage is established without canceling or reassigning the one-shot operation.
  Scope: layer-1+
- TEST-MCP-071: Given an interactive stream connection, when the client disconnects, then `User is AFK.` is sent to the agent. When the client reconnects and stream establishment completes, then `User is here.` is sent. These messages are not sent for one-shot sessions.
  Scope: layer-1+
- TEST-MCP-072: Given Director Agent Pool tab actions (connect, recycle, stop/start, queue move up/down, cancel/remove, free-form enqueue), when invoked from UI commands, then the correct REST endpoints are called and UI state refreshes from server snapshots and notifications.
  Scope: layer-1+
- TEST-MCP-073: Given `McpServer.Support.Mcp` stateful services and registries, when architecture validation runs, then each authoritative data source is DI-owned (`singleton`/`scoped`), no stateful service is created outside DI, change notifications use `INotifyPropertyChanged`, and consumers pull current state from the source-of-truth service.
  Scope: layer-1+
- TEST-MCP-074: Given TODO create/update and session log submit/append requests, when IDs violate canonical naming formats (`TODO persisted ids: ^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+-\d{3}$ or ^ISSUE-\d+$`, `sessionId: <Agent>-<yyyyMMddTHHmmssZ>-<suffix>`, `requestId: req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`), then APIs reject with validation errors and no data mutation occurs; valid IDs are accepted across YAML and SQLite TODO backends.
  Scope: layer-1+
- TEST-MCP-075: Given `ChannelChangeEventBus`, when events are published with zero, one, or multiple subscribers, then publish does not throw and each active subscriber receives events independently; canceled subscriptions stop enumeration.
  Scope: layer-1+
- TEST-MCP-076: Given TODO, session log, and repo mutation services, when create/update/delete-style operations succeed, then each service publishes one change event with the expected category/action/entityId values.
  Scope: layer-1+
- TEST-MCP-077: Given extended mutation services (`ToolRegistryService`, `WorkspaceService`, `AgentService`), when representative create/update operations succeed, then each service publishes the expected category/action event.
  Scope: layer-1+
- TEST-MCP-078: Given `GET /mcpserver/events`, when a client subscribes, then the response content type is `text/event-stream`.
  Scope: layer-1+
- TEST-MCP-079: Given `GET /mcpserver/events?category=todo`, when a TODO change event is published, then the stream includes an `event: todo` payload containing the matching entity ID.
  Scope: layer-1+
- TEST-MCP-080: Given category filtering on `/mcpserver/events`, when non-matching categories are published, then filtered subscribers do not receive those non-matching domain events.
  Scope: layer-1+
- TEST-MCP-081: Given workspace-scoped GitHub auth endpoints, when a token is set via `PUT /mcpserver/gh/auth/token`, then `GET /mcpserver/gh/auth/status` reports `hasStoredToken=true`; when `DELETE /mcpserver/gh/auth/token` is called, the token is removed.
  Scope: layer-1+
- TEST-MCP-082: Given GitHub OAuth bootstrap endpoints, when `GET /mcpserver/gh/oauth/config` is called, then effective configuration fields are returned; when OAuth is not fully configured, `GET /mcpserver/gh/oauth/authorize-url` returns 400 with a clear error.
  Scope: layer-1+
- TEST-MCP-083: Given `GitHubCliService` with a stored workspace token, when GitHub commands are executed, then `IProcessRunner` receives a `ProcessRunRequest` containing `GitHubTokenOverride`; when no token exists and fallback is enabled, standard CLI execution is used.
  Scope: layer-1+
- TEST-MCP-084: Given GitHub Actions workflow operations, when list/detail/rerun/cancel paths are invoked, then gh CLI commands and REST/client contracts for `/mcpserver/gh/actions/runs*` remain consistent and parse expected run/job/step metadata.
  Scope: layer-1+
- TEST-MCP-085: Given natural-language workspace policy directives, when `POST /mcpserver/workspace/policy` or `workspace_policy_apply` is invoked with valid directives, then targeted workspace ban lists are mutated and invalid directives return structured 400 errors.
  Scope: layer-1+
- TEST-MCP-086: Given audited copilot decoration, when invoke and streaming operations execute, then session-log submissions include `copilot_invocation` actions and completed status records.
  Scope: layer-1+
- TEST-MCP-087: Given ingestion options and marker prompt generation for this repository, when host configuration post-processing runs, then repo-local src/McpServer.Cqrs and src/McpServer.Cqrs.Mvvm glob patterns are enforced, and marker output includes the Available Capabilities section without McpServer.UI.Core or McpServer.Director entries.
  Scope: layer-1+
- TEST-MCP-088: Given direct website URL ingestion requests, when `POST /mcpserver/context/ingest-website` or `context_ingest_website` runs, then valid HTTP/HTTPS pages ingest as `external-web` sources, URL outcomes are returned, SSRF/private/link-local targets are blocked, redirects are bounded, and source dedup/update behavior is preserved by source key.
  Scope: layer-1+
- TEST-MCP-089: Given a .NET 9 host application that registers the hosted Microsoft Agent Framework library against an MCP Server workspace, when the built-in agent workflow runs, then session log turns are created/updated through canonical identifiers, TODO plan/status/implementation operations execute through the existing MCP Server contracts, repository read/list/write tools browse repo-relative paths without host-specific glue code, local desktop process launch reuses the authenticated workspace desktop-launch contract, `mcp_powershell_session_*` tools execute commands inside a persistent in-process PowerShell session hosted by the agent itself, and host applications can drive the same local runspace interactively through `IMcpHostedAgent.PowerShellSessions`.
  Scope: layer-1+
- TEST-MCP-090: Given representative controller and middleware failure paths across the server, when an unhandled exception produces HTTP 500, then the response body contains a non-empty detailed error description for the failed operation, excludes secrets and raw stack traces, and remains consistent across endpoints through the shared error-handling path.
  Scope: layer-1+
- TEST-MCP-091: Given the admin configuration management surface, when configuration values are read or patched through the configuration controller and YAML helper, then effective settings are exposed as flattened key-value pairs, submitted YAML-backed keys are persisted and reloaded, and standard JWT Bearer admin authorization keeps the endpoints unavailable when OIDC is disabled.
  Scope: layer-1+
- TEST-MCP-092: Given a TODO create request with id `ISSUE-NEW`, when GitHub issue creation succeeds, then the server persists the TODO using the canonical `ISSUE-{number}` id returned by GitHub, includes GitHub correlation metadata in the TODO note, and returns the canonical id from the create surface instead of the temporary alias.
  Scope: layer-1+
- TEST-MCP-093: Given workspace-scoped GitHub CLI execution, when gh commands run with either a stored workspace token or fallback authentication, then the process runner receives the resolved workspace root as the working directory.
  Scope: layer-1+
- TEST-MCP-094: Given an existing `ISSUE-{number}` TODO updated through any server TODO update surface, when the local update succeeds, then the server preserves the existing description, syncs title/state/priority metadata back to GitHub using canonical `priority: HIGH|MEDIUM|LOW` labels, and posts a GitHub issue comment describing the applied change set.
  Scope: layer-1+
- TEST-MCP-095: Given an `ISSUE-NEW` TODO created through the HTTP API, when GitHub-origin comments are added and GitHub-to-TODO sync runs, then the TODO note gains the generated GitHub comment section without altering the TODO description. When the TODO priority changes and a TODO-authored note comment is appended, then the GitHub issue receives the canonical updated priority label and a GitHub comment containing the appended note text. When the GitHub issue is later closed externally and GitHub-to-TODO sync runs again, then the TODO is marked done.
  Scope: layer-1+
- TEST-MCP-096: Given an empty authoritative SQLite TODO store and an existing `TODO.yaml`, when initialization runs and later authoritative mutations project back to YAML, then bootstrap import, deterministic projection ordering, projection-only YAML behavior, and preservation of `notes`, `completed`, and `code-review-remediation` metadata are all verified by automated tests.
  Scope: layer-1+
- TEST-MCP-097: Given a TODO item mutated through create/update/delete and queried through storage, REST, typed client, and integration surfaces, when audit history is requested, then append-only ordered states, delete-history retention, not-found behavior, and explicit projection-failure classification are all verified by automated tests.
  Scope: layer-1+
- TEST-MCP-098: Given a Parseable-bound log event with more than 250 structured properties and user properties that collide with reserved Parseable field names, when `ParseableEventFormatter` serializes the event, then the payload contains at most 250 top-level fields, canonical reserved metadata is preserved, and overflow non-reserved properties are deterministically omitted.
  Scope: layer-1+
- TEST-MCP-099: Given the repository Azure DevOps pipeline definition, when tracked paths change on `main` or `develop` or through a pull request, then `azure-pipelines.yml` triggers the core CI workflow, runs config validation/build/test/docs/MSIX/package jobs with the documented branch and variable gates, and skips optional feed/docs publication safely when the required Azure DevOps variables are absent.
  Scope: layer-1+
- TEST-MCP-100: Given a PowerShell `McpSession` workspace with an active session cached in `.mcpSession/current-session.json`, when the legacy `.mcpServer/session.yaml` wrapper is missing and the module is reinitialized or resolves a session without an explicit `Session` argument, then it reuses the cached current session object and session ID. When the session is completed, both cache files are removed.
  Scope: layer-1+
- TEST-MCP-101: Given trust-bootstrap marker rendering and the public PowerShell bootstrap modules, when the marker signature is valid and `/health` echoes the submitted nonce exactly, then `McpSession`, `McpTodo`, and `McpContext` initialize successfully and proceed with MCP usage. When the signature is invalid or the nonce does not match, then each bootstrap module emits `MCP_UNTRUSTED`, does not probe additional endpoints, and aborts MCP usage before session-log or TODO traffic continues.
  Scope: layer-1+
- TEST-MCP-102: Given the provider-factory, native encryption, and maintenance-command workstreams, when SQLite, PostgreSQL, and SQL Server are configured for clean-database integration runs, then provider-specific migrations apply successfully and the live encryption state matches the configured state. When encryption is enabled or disabled on an existing database, then the provider-specific transition workflow shall expose a no-data-loss maintenance procedure and dry-run plan before mutation, with automated dry-run tests covering SQLite SEE, PostgreSQL `pg_tde`, and SQL Server TDE command generation. SQL Server provider and migration integration coverage shall use a self-managed SQL Server LocalDB instance created and torn down by the test harness where LocalDB is installed, and otherwise a temporary SQL Server 2025 Developer instance the harness starts for the run and removes afterwards (operator decision 2026-10-15: "allow a temporary sql server 2025 dev instance when necessary."), while SQL Server TDE validation shall run against a separate non-LocalDB SQL Server target because LocalDB cannot validate TDE.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] SqlServerSandboxPlanAssertion_AcceptsLocalDbOrTemporaryInstanceAndRejectsSharedOrNone, then the harness itself: the SQL Server cases use a self-managed LocalDB instance when SqlLocalDB is installed and otherwise a temporary SQL Server 2025 Developer instance the harness starts for the run and removes afterwards, each case on its own uniquely named database that is dropped; a shared server, no server, and a skip are rejected (PLAN-SESSIONTRIAGEFIX-001 B11, triage-report-ead2fdaa).
  - [ ] SqlServer_CleanDatabase_AppliesMigrationsAndPersistsEntity (renamed from SqlServer_LocalDb_CleanDatabase_AppliesMigrationsAndPersistsEntity), HandoffIngestionStorageMigrationTests.SqlServer_HandoffMigration_DowngradeAndReupgrade and HostileReviewG4OverlayTests.HostileReviewEntity_RoundTrip_SqlitePgSqlServer pass on LAB-OMARCHY through the temporary instance and on PAYTON-LEGION2 through LocalDB (PLAN-SESSIONTRIAGEFIX-001 B11).
  - [ ] RecoveryRunDeletedAtAssertion_AcceptsDatetime2UtcRoundTripAndRejectsOffsetColumnOrShiftedInstant, then SqlServer_RequirementsRecoveryRun_SoftDelete_RoundTripsUtcDeletedAt: on a clean SQL Server database migrated to head, RequirementsRecoveryRuns.DeletedAtUtc is datetime2 (the AddHostileReviewTables AlterColumn) and a soft-deleted recovery run reads back its DeletedAtUtc as the same UTC instant (TR-MCP-DB-DTO-001; PLAN-SESSIONTRIAGEFIX-001 B8, triage-report-71d801ad). The gate accepts a datetime2 column whose read-back equals the written instant at offset zero, and rejects a datetimeoffset column, a shifted instant, a non-zero offset and a missing value.
- TEST-MCP-103: Given a Byrd execution TODO, when unit tests are not defined, then the service rejects transition to `Implementing`; when unit tests are defined through the test-plan API, then the TODO advances to `TestReady`.
  Scope: layer-1+
- TEST-MCP-104: Given a Byrd execution TODO linked to requirements, session turns, and modified files, when bounded execution context or checkpoint delta context is requested, then the server returns only concise snippets, recent turn summaries, relevant files, artifacts, commits, and updated next action for that TODO.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Files modified that the session-log writer records on a TODO's linked turns appear among the relevant files of that TODO's bounded execution context: the reader and every writer use the one string-list type definition SessionLogTurnStringListTypes.FileModified (PLAN-SESSIONTRIAGEFIX-001 A6, triage-report-04659db1; FilesModifiedListType_WriterStoresTheSharedDefinition, FilesModifiedFixture_SessionLogWriterStoresOneRowForTheTurnFile, GetExecutionContextAsync_WithLinkedSessionTurns_OrdersByTimestampOnSqlite).
- TEST-MCP-105: Given the Byrd execution REST controller, STDIO MCP tools, typed client, and `adb_step` surface, when representative phase creation, active TODO lookup, status progression, and screenshot validation calls are executed, then structured contracts remain stable and Android validation results are returned without arbitrary shell passthrough.
  Scope: layer-1+
- TEST-MCP-106: Given requirements export with doc=all and format=wiki, when generation runs, then docs/Project/wiki contains both azure/ and github/ folders, each manifest includes generatedAtUtc, Azure includes `.order`, and GitHub includes `_Sidebar.md` and `_Footer.md`.
  Scope: layer-1+
- TEST-MCP-107: Given wiki ingest with Azure and GitHub document folders, when manifest and file modified timestamps identify a newer source, then import selects that source; when the two checks disagree, import fails unless preferredWikiFormat is supplied.
  Scope: layer-1+
- TEST-MCP-108: Given the REPL requirements workflow, when wiki export or import is invoked, then export returns format, docType, generatedAtUtc, outputRoot, and written file metadata, and import accepts path-keyed documents with per-document timestamps.
  Scope: layer-1+
- TEST-MCP-109: Given Codex, Claude Code, Copilot, and Cline agent plugins, when requirements wiki workflows are used, then each plugin exposes the wiki requirements contract and routes generate/ingest envelopes without expecting archive bytes.
  Scope: layer-1+
- TEST-MCP-110: Question CRUD (happy, validation, 404).
  Scope: layer-1+
- TEST-MCP-111: Answer CRUD (orphan rejection on deleted question, cascade delete).
  Scope: layer-1+
- TEST-MCP-112: Accept-answer flow (single-accept invariant, un-accept clears).
  Scope: layer-1+
- TEST-MCP-113: Tag filter AND-semantics + empty-result case.
  Scope: layer-1+
- TEST-MCP-114: Vote increment / decrement on Question and Answer; concurrency atomicity.
  Scope: layer-1+
- TEST-MCP-115: Comment thread create / list / delete with depth-cap enforcement.
  Scope: layer-1+
- TEST-MCP-116: FAQ endpoint projection shape, ordering, deeplink format.
  Scope: layer-1+
- TEST-MCP-117: Search: created question/answer text is found via `IContextSearchService`; removed on delete.
  Scope: layer-1+
- TEST-MCP-118: Author resolution precedence (body > API key > JWT > anonymous-rejected).
  Scope: layer-1+
- TEST-MCP-119: Workspace isolation (mirror `EfTodoService_WorkspaceIsolationTests`).
  Scope: layer-1+
- TEST-MCP-120: MCP STDIO tool parity for each REST endpoint.
  Scope: layer-1+
- TEST-MCP-121: `QaClient` end-to-end against `CustomWebApplicationFactory`.
  Scope: layer-1+
- TEST-MCP-122: `QaWorkflow` unit tests in `tests/McpServer.Repl.Core.Tests` (NSubstitute over `QaClient`); REPL agent-stdio integration test in `tests/McpServer.Repl.IntegrationTests` that spawns the host with `ReplChildProcessHelper`, sends a `workflow.qa.*` YAML envelope, asserts the response shape.
  Scope: layer-1+
- TEST-MCP-123: PowerShell module Pester tests (if a `tools/powershell/tests/` pattern exists, otherwise smoke-script invoked from `./build.ps1 Test` or a new `ValidatePowerShell` target).
  Scope: layer-1+
- TEST-MCP-124: Skill smoke test: each new `qa/SKILL.md` is loaded by the plugin packager and its frontmatter passes the standard skill validation script in each plugin repo.
  Scope: layer-1+
- TEST-MCP-125: Audit emission tests: one audit row per mutation, correct `Action`, `Version` monotonic per `(EntityKind, EntityId)`, `Actor` populated via `IQaAuthorResolver`, `SnapshotJson` round-trips.
  Scope: layer-1+
- TEST-MCP-126: Audit query tests: paging contract, filter combinations, empty-result case, workspace isolation (audits from workspace A invisible to workspace B).
  Scope: layer-1+
- TEST-MCP-127: Vote audit: an `UPDATE ... SET VoteCount = VoteCount + @delta` plus an audit row are emitted in a single transaction (both succeed or both rollback). Audit row's `Actor` is populated from `IQaAuthorResolver`, so voter identity is captured per event.
  Scope: layer-1+
- TEST-MCP-128: Answer-with-sources: round-trip `CreateAnswerRequest.Sources` -> `AnswerEntity.SourcesJson` -> `AnswerDto.Sources`; FAQ projection includes the sources array; deletion of an answer hard-deletes the sources via the existing cascade.
  Scope: layer-1+
- TEST-MCP-129: Skill mandate text test: each sibling-plugin `skills/qa/SKILL.md` contains the exact mandatory rule block (regex match on the callout) and the `sources[]` schema example. Validation is a small PowerShell or `dotnet test` content-check (`tests/McpServer.Qa.Validation/SkillMandateTests.cs` or a `tools/plugin-skill-check.ps1` invoked from `./build.ps1 Test`).
  Scope: layer-1+
- TEST-MCP-130: Close / duplicate flow tests: close-with-reason, reopen, mark-as-duplicate (canonical link both ways), FAQ excludes closed by default, FAQ surfaces duplicate redirect when requested with `?includeClosed=true`, audit rows captured per transition.
  Scope: layer-1+
- TEST-MCP-131: Sanitization test corpus: XSS-payload corpus validates that every common attack vector is stripped on Question/Answer/Comment write; `bodyHtml` contains only allow-listed tags/attributes; raw `body` is preserved verbatim; round-trip Markdown -> HTML matches snapshot.
  Scope: layer-1+
- TEST-MCP-132: FAQ wiki page generation test: build target produces deterministic Markdown matching the snapshot fixture, wiki index files updated, generated page renders cleanly in both Azure DevOps and GitHub wiki conventions (e.g., `_Sidebar.md` / `.order` references present).
  Scope: layer-1+
- TEST-MCP-133: Voter-history endpoint: posting N votes from M distinct actors produces N audit rows; `GET /questions/{id}/voters` returns exactly those rows projected to `{ actor, action, createdAt }`; same for answers; workspace isolation enforced.
  Scope: layer-1+
- TEST-MCP-134: One-vote-per-user enforcement: same actor posts vote_up twice -> second call is no-op (no counter change, no second audit row); actor posts vote_up then vote_down -> counter delta is -2, audit row recorded with action `vote_change`; actor revokes vote -> counter delta is -1, audit row `vote_revoke`; unique index prevents duplicate `QaVoteEntity` rows under concurrent calls (test with parallel writes against in-memory SQLite using `Task.WhenAll`).
  Scope: layer-1+
- TEST-MCP-135: Current vote state endpoint: `GET /questions/{id}/votes` returns one row per active voter from `QaVoteEntity` after a sequence of apply / change / revoke calls; revoked voters do not appear; workspace isolation enforced.
  Scope: layer-1+
- TEST-MCP-136: Hub-and-spoke federation tests cover config role defaults, durable proxy/workspace/operation storage, hub enrollment and status, LocalProxy /mcp-transport routing, operation headers, queued write fallback, replay candidate persistence, stale-version conflict creation, and provider migration compilation.
  Scope: layer-1+
- TEST-MCP-137: Given templates/prompt-templates.yaml, when the marker-template contract tests run, then default-marker-prompt contains the frontier-to-implementation planning guidance, explicit requirements capture guidance, and TDD unit-test planning guidance.
  Scope: layer-1+
- TEST-MCP-138: Unit tests must fail red until WorkspaceService is database-authoritative and DbForeignKeyContractTests prove every WorkspaceId entity has a Workspaces FK with non-cascade delete behavior.
  Scope: layer-1+
- TEST-MCP-139: Unit tests must fail red until persistent delete paths preserve rows through soft-delete metadata and every mutable entity writes DataAuditLog rows for create, update, and soft-delete operations.
  Scope: layer-1+
- TEST-MCP-140: Unit and provider tests must fail red until TODO requirement links and requirement traceability links enforce FKs, missing requirements are backfilled, and SQLite, SQL Server, and PostgreSQL migrations preserve data.
  Scope: layer-1+
- TEST-MCP-141: Add or update a documentation contract test proving docs/Development-Process-draft-v3.md captures the plan creation requirements for decision-complete frontier-model handoff plans, FR/TR/TEST traceability, TDD-first red/green behavior, and zero-failure zero-skip Byrd gates.
  Scope: layer-1+
- TEST-MCP-142: Bats coverage must prove workflow.requirements.updateFr, updateTr, and updateTest accept priority changes and do not fail inside the Codex plugin wrapper.
  Scope: layer-1+
- TEST-MCP-143: Validate that outstanding-session consolidation creates MCP-backed requirements and TODO traceability, inventories dirty workspaces, preserves unrelated changes, blocks unsafe deploys, and records zero-failure zero-skip validation gates before completion.
  Scope: layer-1+
- TEST-MCP-144: Given a TODO description containing Markdown headings, lists, code fences, blank lines, leading indentation, and trailing content, create, update, read, audit, and projection paths preserve the exact meaningful formatting with zero failures and zero skips.
  Scope: layer-1+
- TEST-MCP-145: Automated tests shall verify client request serialization, controller mixed-batch acceptance and whole-batch rejection, repository transaction rollback, and REPL schema validation for requirements batch commands.
  Scope: layer-1+
- TEST-MCP-146: Plugin validation shall include shell syntax checks, Cline bridge JSON-stdio tests, Cline schema preflight tests, and JSON Schema parse checks for the published REPL request schema.
  Scope: layer-1+
- TEST-MCP-147: Given agent-facing documentation, marker templates, and generated requirements wiki outputs, automated tests shall verify single-line JSON stdio guidance, current plugin registry guidance, and Azure/GitHub wiki output file parity.
  Scope: layer-1+
- TEST-MCP-148: Build.Tests SHALL verify build.ps1 relaunches through a PowerShell host with -NoLogo, -NoProfile, and -NonInteractive, Build.Tests pwsh.exe helpers include those flags, and live deployment guidance examples include those flags.
  Scope: layer-1+
- TEST-MCP-149: Bats validation covers AC-SKILLS-001 through AC-SKILLS-006 for mcpserver-codex-plugin, mcpserver-claude-code-plugin, mcpserver-claude-cowork-plugin, mcpserver-copilot-plugin, and mcpserver-grok-plugin.
  Scope: layer-1+
- TEST-MCP-150: Jest validation covers AC-SKILLS-001 through AC-SKILLS-006 for mcpserver-cline-plugin, mcpserver-cline-v2-plugin, and mcpserver-opencode-plugin.
  Scope: layer-1+
- TEST-MCP-151: Process validation covers red/green/final evidence and the zero-failure zero-skip focused validation gate for the workflow-skill rollout.
  Scope: layer-1+
- TEST-MCP-152: Unit and integration validation shall prove generated requirements exports render TEST descriptions readably and display acceptance criteria as a bulleted list for both GitHub and Azure wiki outputs.
  Scope: layer-1+
- TEST-MCP-153: Regression tests SHALL verify all plugin batch requirement methods accept unindented YAML records, indented YAML records, and inline JSON-array records while preserving nested acceptanceCriteria arrays and boolean isSatisfied fields.
  Scope: layer-1+
- TEST-MCP-154: Given a TODO update payload that changes implementationTasks[].done and omits top-level done, when the plugin wrapper builds the TODO update HTTP body, then the body preserves nested task done values and omits parent done so the server cannot accidentally complete the TODO.
  Scope: layer-1+
- TEST-MCP-155: Given mocked successful and failed session/compact hook dependencies, when SessionStart, SessionEnd, PreCompact, and PostCompact scripts run, then each status-only path emits {} and PostCompact emits no additionalContext.
  Scope: layer-1+
- TEST-MCP-156: Verify GitHubCliService passes command-scoped safe.directory environment variables for workspace-scoped calls and uses gh --repo without workspace cwd when a repository is configured.
  Scope: layer-1+
- TEST-MCP-157: Verify the PowerShell wrapper returns a non-zero result with a timeout diagnostic when a plugin helper command hangs beyond TimeoutSeconds, while normal helper wrapper tests still pass.
  Scope: layer-1+
- TEST-MCP-158: Keyserver trust and audit tests SHALL verify party registration, manifest signing, manifest verification, replay nonce rejection, stale sequence rejection, expiry rejection, disabled/unknown party or key rejection, key rotation descriptor preservation, signed manifest trace lookup/reporting, and audit persistence.
  Scope: layer-1+
- TEST-MCP-159: Subscriber commit and abort tests SHALL verify manifest trust, protected diffgram decrypt/hash validation, idempotent duplicate commit, conflict rejection, abort semantics, transaction status reporting, key-ring rotation, durable status, and concurrent commit/replay behavior.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] DurableTransactionSecurityStorageTests TurnCoordinator_WithDurablePubSub_WhenSubscriberCommitTimesOutAndRollbackSucceeds_CancelsPendingCommit and TurnCoordinator_WithDurablePubSub_WhenSubscriberUnavailableAfterRollback_DoesNotReplayRolledBackCommit drive the coordinator with brain-slot.invoke (FR-MCP-173 ac-2), so the durable subscriber paths they assert are reached (PLAN-SESSIONTRIAGEFIX-001 A12, triage-report-ff1be8ee). The operation is shown in scope first by TransactionOperationScopeAssertion_AcceptsBrainSlotAndRejectsGeneralAgentOperations, which accepts brain-slot.invoke and rejects todo.update, sessionlog.dialog and an empty name.
- TEST-MCP-160: Real keyserver/subscriber integration tests SHALL validate the separate keyserver and subscriber hosts without mocks, including valid commit, tampered manifest, stale sequence, encrypted-body mismatch, subscriber key-ring configuration, and file-backed key provisioning.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] SeparateTransactionServiceIntegrationTests SeparateSubscriberHost_TurnCoordinatorWithHttpPubSub_CommitsThroughExternalSubscriberHost, SeparateSubscriberHost_TurnCoordinatorWithHttpPubSub_RollsBackMutationWhenSubscriberRejects and SeparateSubscriberHost_TurnCoordinatorWithHttpPubSub_WaitsForSubscriberAcknowledgementBeforeReturningCommitted drive the coordinator with brain-slot.invoke (FR-MCP-173 ac-2) against the separate key server and subscriber hosts (PLAN-SESSIONTRIAGEFIX-001 A12, triage-report-ff1be8ee).
- TEST-MCP-161: MCP transaction gating tests SHALL verify coordinator commit, degraded, rollback, durable timeout cancellation, and pub-sub handoff/replay/retention for QuadBrain brain-slot.invoke and brain-slot.weight-update (and other RequiresKeyserver operations). Generic REPL client protected-namespace policy remains in this TEST. First-party non-QuadBrain mutations (TODO, requirements, session-log including QBAgent, memory, repo, prompt templates, tool registry, GitHub, GraphRAG, voice, agent pool, ingest, context, federation apply/control) are governed by FR-MCP-173 / TEST-MCP-221 and SHALL persist without coordinator/keyserver; they are not fail-closed under this TEST. Covered by TurnTransactionCoordinatorTests, BrainSlotInvocationTransactionTests, QuadBrainOrchestrationServiceTests, and remaining TEST-MCP-168 pub-sub tests. Zero failures and zero skips in the executed QuadBrain/coordinator scope.
  Scope: layer-1+
- TEST-MCP-162: Transaction traceability/import tests SHALL prove FR-MCP-118 through FR-MCP-128, transaction TR records, TEST-MCP-158 through TEST-MCP-173, and live TODO references resolve without placeholder transaction-plan entries.
  Scope: layer-1+
- TEST-MCP-163: Deferred-scope documentation tests SHALL prove remaining future autonomous Quad-Model branches, direct agent execution, desktop launch, tunnels, workspace/auth/server configuration, full remote/runtime compensation, complete delayed-rollback isolation, bucket/GitHub compensation, quarantine/fine-tuning automation, implicit fallback behavior, and full key-rotation lifecycle automation remain explicit future work rather than silently reported as complete.
  Scope: layer-1+
- TEST-MCP-164: aiUnit plan review tests SHALL validate committed aiUnit run-log evidence for PLAN-TURNTRANSACTIONS-001 and fail on critical/high findings.
  Scope: layer-1+
- TEST-MCP-165: Imported diagram preservation tests SHALL validate all six imported diagram views by key in the workspace, one rendered image link per diagram section of the Quad-Model plan, imported source references, and repo annotations.
  Scope: layer-1+
- TEST-MCP-166: Keyserver manifest diagram tests SHALL derive coverage from SD-DIFFGRAM-001 signing, verification, invalid, and valid branches.
  Scope: layer-1+
- TEST-MCP-167: Subscriber diagram tests SHALL derive coverage from SD-DIFFGRAM-001 subscriber decrypt, hash, commit, reject, duplicate, abort, and status branches.
  Scope: layer-1+
- TEST-MCP-168: End-to-end turn transaction diagram tests SHALL derive coverage from AD-TXN-001 commit, subscriber unavailable, degraded, and no-success-before-ack branches.
  Scope: layer-1+
- TEST-MCP-169: Degraded rollback reconciliation tests SHALL validate AD-TXN-001 and AD-AOT-001 in-scope rollback/audit behavior while requiring AoT execution to use the authorized ArbiterOfTruth transaction path.
  Scope: layer-1+
- TEST-MCP-170: Quad scope enforcement tests SHALL prove Curiosity admission, weight updates, AoT execution, and quad orchestration execute only through FR-MCP-129 through FR-MCP-135 authorization gates, while unrelated autonomous branches remain fail-closed.
  Scope: layer-1+
- TEST-MCP-171: Quad architecture conformance tests SHALL validate ARCH-QUAD-001 component mapping, trust boundaries, authorized quad branches, and remaining future branches.
  Scope: layer-1+
- TEST-MCP-172: Architecture Round 1 conformance tests SHALL validate trust model, component boundaries, storage boundaries, threat model, rollback/audit decisions, and gap analysis.
  Scope: layer-1+
- TEST-MCP-173: Design Round 2 conformance tests SHALL validate DTOs, entities, options, interfaces, endpoint contracts, reason codes, audit payloads, XMLDoc obligations, and AC-to-test mapping.
  Scope: layer-1+
- TEST-MCP-174: Brain slot documentation and plan artifact coverage SHALL prove FR-MCP-129 through FR-MCP-135, TR-MCP-QUAD-001 through TR-MCP-QUAD-007, TEST-MCP-174 through TEST-MCP-185, diagram annotations, and the AD-CURIOSITY-001-BR-EXTERNAL, AD-AOT-001, AD-WEIGHT-001, and full quad orchestration implementation split are present.
  Scope: layer-1+
- TEST-MCP-175: Brain slot durable registry coverage SHALL prove workspace-scoped CRUD, role validation, one enabled slot per workspace and role, replaceExisting replacement audit, soft delete/disable semantics, credentialReference-only persistence, status projection, and workspace isolation.
  Scope: layer-1+
- TEST-MCP-176: Brain slot REST, client, and STDIO contract coverage SHALL prove list/get/upsert/delete/enable/disable/status/invoke/orchestrate/aot-reconcile/weight-update routes serialize the public DTOs correctly, hide raw credentials, enforce AgentManager policy behavior, and preserve workspace/auth propagation.
  Scope: layer-1+
- TEST-MCP-177: Brain slot provider and credential coverage SHALL prove env:, config:, and file: credential references resolve without persistence or response leakage; OpenAI and OpenAI-compatible client creation validates endpoint policy; disallowed hosts, loopback without explicit allowance, timeout, and cancellation fail closed without live network dependency.
  Scope: layer-1+
- TEST-MCP-178: Brain slot transaction admission coverage SHALL prove execution is rejected unless brain-slot execution and required turn transactions are enabled, no output is returned before subscriber commit, commit failure/timeout/degradation discards output, and diffgrams contain slot metadata and hashes.
  Scope: layer-1+
- TEST-MCP-179: Curiosity admission coverage SHALL prove only CuriosityEngine can request GraphRAG/context admission, admission happens only after committed subscriber acknowledgement, failed commits do not inject model output into cache/GraphRAG, and Left/Right/Arbiter invocations never mutate cache.
  Scope: layer-1+
- TEST-MCP-180: Quad containment coverage SHALL prove AoT reconciliation execution, weight update execution, and full automatic quad orchestration are available only through explicit FR-MCP-134/FR-MCP-135 gates, while non-Curiosity GraphRAG mutation and implicit fallback model behavior remain fail-closed.
  Scope: layer-1+
- TEST-MCP-181: Quad orchestration service coverage SHALL prove full Quad-Brain orchestration rejects non-ready workspaces, invokes roles through transaction-gated slots in the required order, and returns final committed AoT output with role transaction metadata.
  Scope: layer-1+
- TEST-MCP-182: AoT reconciliation execution coverage SHALL prove AoT reconciliation executes through the ArbiterOfTruth slot, includes Left/Right/Curiosity evidence, returns committed output only after subscriber acknowledgement, and fails closed without fallback.
  Scope: layer-1+
- TEST-MCP-183: Quad weight update coverage SHALL prove approved weight updates persist weights and versions with audits, while missing approvals, stale versions, invalid roles, disabled slots, and invalid weights are rejected without mutation.
  Scope: layer-1+
- TEST-MCP-184: Quad public contract parity coverage SHALL prove REST, client, STDIO, and Node plugin surfaces expose orchestration, AoT reconciliation, and weight update contracts consistently.
  Scope: layer-1+
- TEST-MCP-185: Quad traceability closure coverage SHALL prove FR-MCP-134 through FR-MCP-135, TR-MCP-QUAD-005 through TR-MCP-QUAD-007, TEST-MCP-181 through TEST-MCP-185, and the imported diagrams no longer describe AoT, weight updates, or full quad orchestration as deferred once implemented.
  Scope: layer-1+
- TEST-MCP-186: Tests SHALL verify the ACID tightly coupled Microsoft Agent Framework profile metadata, strict option defaults, filtered model-visible tools, serialized function invocation settings, DI compatibility, and backward compatibility for default hosted-agent registration.
  Scope: layer-1+
- TEST-MCP-187: RETIRED 2026-07-20 by FR-MCP-142 and TR-MCP-QB-001. This test covered the hosted coding agent reaching QuadBrain orchestration through the mcp_quadbrain_coding_execute tool. That tool has been removed from the shared hosted-agent catalog because it exposed a QuadBrain capability to every McpServer.McpAgent host rather than to QBAgent alone, and it was already dead for QBAgent since QuadBrainInternalToolExecutor had no case for it. Its coverage is replaced by absence assertions under TEST-MCP-193: McpHostedAgentAdapterTests.AcidRuntime_ExposesNoQuadBrainSurface, HostedAgentQuadBrainAbsenceTests (registration functions, attached run-option tools, and QBAgentDefinition tool-name lists), and HostedAgentWorkflowIntegrationTests.AddMcpServerMcpAgent_SharedToolCatalog_ContainsNoQuadBrainTool. QuadBrain coding work continues to run server-side through POST /v1/chat/completions, which QBAgent invokes as an OpenAI-compatible model; that path keeps its own integration coverage.
  Scope: layer-1+
- TEST-MCP-188: Registered 2026-07-20 to close a pre-existing traceability drift: this id was cited at tests/Build.Tests/DocumentationGuidanceTests.cs:35 but existed in neither the MCP requirements store nor docs/Project/Testing-Requirements.md, so a naive next-free-id calculation would have collided with live code. The test verifies that the workspace marker template pins the PowerShell and Node runtime guidance agents depend on: DocumentationGuidanceTests asserts the generated marker prompt instructs agents to invoke PowerShell as pwsh.exe from PATH rather than hard-coding an absolute install path, and that Node usage guidance is present, so that agents on hosts with WindowsApps or MSIX PowerShell layouts still resolve a working runtime. Fixture: the rendered marker prompt template read from the repository. This entry documents existing coverage; no behavior changed when it was registered.
  Scope: layer-1+
- TEST-MCP-189: Validates FR-MCP-140 and TR-MCP-SEC-005. Unit: the emitted AGENTS-README-FIRST.yaml signature block contains a fields array and a format string, and the fields array lists exactly the canonical marker-v1 payload field names in the order BuildSignaturePayload emits them. Unit, byte-compatibility gate: the HMAC-SHA256 signature value computed for a fixed known marker is unchanged by the refactor, proving the six existing independent verifiers (McpSession.psm1, plugins lib-ps and lib-sh marker resolvers, lib-node and mcp-repl-ts TypeScript resolvers, mcpserver-agent-core marker-trust.ts, MarkerFileClientOptionsResolver.cs) continue to validate. Unit, conditional tail: a marker written with an agentPlugins contract emits the agentPlugins.policy and agentPlugins.contractDigest names in fields, and a marker written without one does not. Unit, drift guard: the canonical key order documented in docs/REPL-AGENT-GUIDE.md matches SignaturePayloadFields exactly, so the prose specification cannot drift from the code.
  Scope: layer-1+
- TEST-MCP-190: Validates FR-MCP-141 and TR-MCP-SVC-002. Unit: ResolveExecutable skips a PATH entry under Microsoft\WindowsApps that contains a zero-byte alias stub of the requested name, and returns a real executable from a later PATH entry instead. Unit, false-positive guard: a PATH entry under C:\Program Files\WindowsApps, the genuine MSIX install root, is NOT skipped, so packaged executables such as the Store PowerShell remain resolvable; the guard matches only the Microsoft\WindowsApps alias directory. Unit: when the only candidate lies under Microsoft\WindowsApps, resolution reports not-found rather than returning the unusable stub path.
  Scope: layer-1+
- TEST-MCP-191: Validates FR-MCP-141 and TR-MCP-TUN-004. Unit: NgrokTunnelProvider constructed with an IProcessEnvironmentService applies the interactive user's USERPROFILE, HOME, APPDATA, and PATH to the child ProcessStartInfo it builds for the ngrok process. Unit: the provider resolves the ngrok binary through ResolveExecutable against that enriched PATH rather than passing the literal string ngrok as the file name. Unit: when the binary cannot be resolved the provider logs at Warning rather than Error, and the emitted message names both the Windows-service or Store-alias cause and the remedies. Note that these paths are dormant under the shipped configuration (Tunnel Provider empty, Ngrok.Enabled false), so the coverage is unit-level rather than end to end.
  Scope: layer-1+
- TEST-MCP-192: Validates TR-MCP-SEC-006 under FR-MCP-129 and FR-MCP-134. Unit: a brain slot whose party id was renamed to brain-slot:creativity, with an active signing key still registered only under the legacy brain-slot:left-hemisphere party, becomes ready after reconciliation, and the key material under the new key id brain-slot:creativity:signing:1 is byte-identical to the legacy key material. Unit, preservation: the legacy party row and legacy key row still exist and remain active after reconciliation, so historical diffgram signatures that reference the legacy key id stay verifiable; the operation is a copy, never a move. Unit, idempotence: running reconciliation twice performs no second write and leaves exactly one key per party id. Unit, no-op paths: reconciliation does nothing when the new party already has an active signing key, and does nothing when no legacy party or key exists, and it never generates new key material in either case. Unit: the same mapping holds for Logic from the legacy brain-slot:right-hemisphere party, while CuriosityEngine and ArbiterOfTruth are untouched.
  Scope: layer-1+
- TEST-MCP-193: Validates FR-MCP-142 and TR-MCP-QB-001. Absence tests, each written to fail if the surface returns. Node: the shared plugin core exports no brainSlotTools symbol and allToolDescriptors contains no descriptor whose name starts with brain_slot, asserted over the built package rather than the source. Node: dispatching a brain_slot name through the host context throws Unknown tool. Server: the MCP tool surface assembled from the Support.Mcp assembly contains no tool named brain_slot_ anything, asserted by scanning the registered McpServerTool names so the test survives a future file reappearing under a different name. Repl: resolving the client passthrough name BRAINSLOTS fails to resolve rather than returning BrainSlotClient. Agent framework: the shared hosted-agent tool catalog contains no tool name containing quadbrain. Registry: no file matching brain_slot_*.json exists under mcps. Positive control that the server path survives: POST /v1/chat/completions still reaches QuadBrain orchestration and QBAgentChatClientFactory still targets model id QuadBrain at the marker baseUrl, so the removal cannot be satisfied by breaking QBAgent.
  Scope: layer-1+
- TEST-MCP-194: Validates TR-MCP-SYNC-001. Build.Tests source-convention checks over build/Build.SyncAgentPlugins.cs, following the existing BuildTargetTests read-the-source idiom: (1) the source names the version-less stable vendor file sharpninja-mcpserver-plugin-core.tgz; (2) the source contains no versioned tarball literal matching sharpninja-mcpserver-plugin-core-digits.digits.digits.tgz, so a future hard-coded version cannot return; (3) the source asserts the packed tarball version against plugins/core/lib-node/package.json, pinned by requiring the assertion code to reference the package.json version read. Red state before the fix: the versioned constant sharpninja-mcpserver-plugin-core-0.1.0.tgz is present and the stable name is absent.
  Scope: layer-1+
- TEST-MCP-195: Pester in plugins/core covering FR-MCP-170/171/172: (1) Invoke-WorkflowAppendDialog for an existing current-turn does not call client.SessionLog.SubmitAsync and does call AppendDialogAsync or POST dialog. (2) Invoke-ReplPersistTurn on HTTP 503 backend_unavailable returns false, sets degraded/queued details, leaves failsafe, does not throw. (3) Failsafe drain on SubmitAsync timeout/503 aborts without drainAttempts increment, without ReplFailsafeDrainCompleted latch, without Failsafe queue drain failed on stderr, and a later drain replays. (4) getFr EXIT 0 with body before 30s when a queued session_submit 503s.
  Scope: layer-1+
- TEST-MCP-196: C# tests covering FR-MCP-170 and TR-MCP-PERSIST-004: AppendProcessingDialogAsync appends items and GET returns them. Missing turn is 404 classified not-found retryable false. Concurrent TODO query during SubmitAsync does not yield backend_unavailable when the SQLite file is valid. GET /health?nonce= still echoes nonce.
  Scope: layer-1+
- TEST-MCP-221: Unit tests SHALL prove: (1) TurnTransactionKeyserverScope.RequiresKeyserver is true only for brain-slot:/brain-slot./quadbrain. and false for todo.update, requirements.fr.update, sessionlog.submit, memory.add, repo.write, github.cli, workflow.todo.update, federation.control, context.mutate, and requirements.ingest; (2) TransactionGated adapters and TransactionalTodoWorkflow persist/succeed while CapturingCoordinator.Request stays null even when TurnTransactions.Enabled and RequiredForMutations are true and the coordinator would reject or is degraded; (3) brain-slot.invoke still hits the coordinator; (4) TurnTransactionCoordinator.ExecuteAsync bypasses SignManifestAsync for todo.update. Covered by TurnTransactionKeyserverScopeTests, TransactionGated*Tests, TransactionalTodoWorkflowTests, TurnTransactionCoordinatorTests, BrainSlotInvocationTransactionTests. Zero failures and zero skips in those classes.
  Scope: layer-1+
- TEST-MCP-ACID-001: Baseline full ACID turn-transaction lifecycle with key server and subscriber mocked in-process and the coordinator as system under test; happy commit, mutation-abort+rollback, subscriber-unavailable degraded+rollback, and all published-message rejections.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] AcidFullLifecycleTests FullLifecycle_HappyPath_Commits, MutationFailure_AbortsAndRollsBack and SubscriberUnavailable_DegradesAndRollsBack drive the lifecycle with the QuadBrain operation brain-slot.invoke, which FR-MCP-173 ac-2 keeps coordinator-gated, and observe committed, aborted with rollback, and degraded with rollback; a general-agent operation such as todo.update is bypassed (FR-MCP-173 ac-1) and cannot exercise the lifecycle (PLAN-SESSIONTRIAGEFIX-001 A12, triage-report-ff1be8ee). The operation is shown in scope first by AcidOperationScopeAssertion_AcceptsBrainSlotAndRejectsGeneralAgentOperations, which accepts brain-slot.invoke and rejects todo.update, sessionlog.dialog and an empty name.
- TEST-MCP-ACID-002: Same lifecycle commits with key server and subscriber as real spun-up WebApplicationFactory hosts torn down after the test; coordinator drives sign and commit over the HTTP transports.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] AcidRunningInstanceTests.FullLifecycle_RunningKeyServerAndSubscriber_Commits drives the lifecycle with brain-slot.invoke (FR-MCP-173 ac-2) against the spun-up key server and subscriber hosts and observes committed (PLAN-SESSIONTRIAGEFIX-001 A12, triage-report-ff1be8ee).
- TEST-MCP-ACID-003: SignManifest success plus rejections - unknown party, unknown key, replay nonce, stale sequence.
  Scope: layer-1+
- TEST-MCP-ACID-004: VerifyManifest valid plus rejections relying parties act on - signature mismatch, wrong subscriber.
  Scope: layer-1+
- TEST-MCP-ACID-005: Commit, idempotent re-commit, and rejections - signature mismatch, encrypted-body mismatch, plaintext mismatch, stale sequence, wrong subscriber, decrypt-required failure. Subscriber validates the key server verification result.
  Scope: layer-1+
- TEST-MCP-ACID-006: Committed, bypassed (disabled/non-mutating), aborted, rejected on key-server sign failure fail-closed before mutation, rejected on subscriber commit rejection with rollback, degraded on subscriber unavailable. Coordinator validates key server and subscriber results.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] CoordinatorOutcomeMatrixTests Coordinator_HappyPath_Committed, Coordinator_MutationFails_Aborted, Coordinator_KeyServerRejectsSign_RejectedWithoutMutation, Coordinator_SubscriberRejectsCommit_RejectedAndRolledBack and Coordinator_SubscriberUnavailable_Degraded drive the matrix with brain-slot.invoke (FR-MCP-173 ac-2) and observe each named outcome (PLAN-SESSIONTRIAGEFIX-001 A12, triage-report-ff1be8ee).
  - [ ] Coordinator_GeneralAgentOperation_Bypassed: with transactions enabled and required for mutations, todo.update answers bypassed with no SignManifest call and no subscriber commit (FR-MCP-173 ac-1, TR-MCP-TXNKEY-001 ac-3; PLAN-SESSIONTRIAGEFIX-001 A12).
- TEST-MCP-ACKRECOVERY-001: Applicable regression matrix is authoritative: complete client/REPL core, all platform-applicable session service/controller checks, preserved Grok tests, deployment helpers and relevant HTTP integration. Baseline failures remain visible with source-backed applicability and portable replacements. Independent predeploy HV. Verified marker/bootstrap after restart; real begin/actions/dialog/title/complete/next prompt; explicit stale supersession preserves all terminal data; no new failsafe records. Archive and drain historical failsafes, restore historical response snapshots, verify all exact IDs.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Compatible deployed server and typed REPL return valid persisted and retitled receipts through unchanged plugin. (evidence: MCP lifecycle and terminal history recovery accepted by final HV AGREE, 10 PASS/0 FAIL/0 UNKNOWN, accuracy 99 and completeness 98. Request /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.request.jsonl; response /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.response.jsonl; full verdict /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.verdict.md. Completed MCP review Codex-20261006T210855Z-hv-resolution-final / req-20261006T210855Z-hv-resolution-final, exact body verified. Live receipt /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/live/result.json; history and queue /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/reconciliation-result.json. Nuke short startup health window remains separately triaged as triage-group-521951095c164f2e; Nuke UpdateService gate is not claimed successful. The exact Nuke-deployed candidate was recovered by normal service start and verified healthy.)
  - [x] Atomic server protection preserves terminal response/status/title/data under stale supersession, including concurrent completion. (evidence: MCP lifecycle and terminal history recovery accepted by final HV AGREE, 10 PASS/0 FAIL/0 UNKNOWN, accuracy 99 and completeness 98. Request /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.request.jsonl; response /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.response.jsonl; full verdict /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.verdict.md. Completed MCP review Codex-20261006T210855Z-hv-resolution-final / req-20261006T210855Z-hv-resolution-final, exact body verified. Live receipt /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/live/result.json; history and queue /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/reconciliation-result.json. Nuke short startup health window remains separately triaged as triage-group-521951095c164f2e; Nuke UpdateService gate is not claimed successful. The exact Nuke-deployed candidate was recovered by normal service start and verified healthy.)
  - [x] Normal live lifecycle opens, updates, retitles, completes and starts next turn without new failsafes. (evidence: MCP lifecycle and terminal history recovery accepted by final HV AGREE, 10 PASS/0 FAIL/0 UNKNOWN, accuracy 99 and completeness 98. Request /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.request.jsonl; response /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.response.jsonl; full verdict /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.verdict.md. Completed MCP review Codex-20261006T210855Z-hv-resolution-final / req-20261006T210855Z-hv-resolution-final, exact body verified. Live receipt /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/live/result.json; history and queue /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/reconciliation-result.json. Nuke short startup health window remains separately triaged as triage-group-521951095c164f2e; Nuke UpdateService gate is not claimed successful. The exact Nuke-deployed candidate was recovered by normal service start and verified healthy.)
  - [x] Retained failsafes are drained and previously verified terminal history is restored; restart trust and private marker readability verified. (evidence: MCP lifecycle and terminal history recovery accepted by final HV AGREE, 10 PASS/0 FAIL/0 UNKNOWN, accuracy 99 and completeness 98. Request /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.request.jsonl; response /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.response.jsonl; full verdict /home/sharpninja/.config/powershell/Snippets/docs/receipts/hv/20261006T210700Z-resolution-final.verdict.md. Completed MCP review Codex-20261006T210855Z-hv-resolution-final / req-20261006T210855Z-hv-resolution-final, exact body verified. Live receipt /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/live/result.json; history and queue /home/sharpninja/.config/powershell/Snippets/.mcpServer/receipts/resolution/reconciliation-result.json. Nuke short startup health window remains separately triaged as triage-group-521951095c164f2e; Nuke UpdateService gate is not claimed successful. The exact Nuke-deployed candidate was recovered by normal service start and verified healthy.)
- TEST-MCP-AIUNIT-001: Add tests in tests/Build.Tests/ covering:
- Build target properties AiCodeReview and AiProjectReview exist.
- WriteAiUnitReviewMarkdownFromData produces correct MD file with prompt + response sections.
- CreateAiUnitClient (internal or exposed) constructs without throw when given valid IConfiguration stub (mocked config for ActiveStrategy, Strategies section); returns non-null client-like object.
- Optional: stubbed SendAsync test verifies that the returned client is called with correct FrontierRequest shape for given reviewType.
These tests must pass with mocks before the real client construction logic is filled (BDP).
  Scope: layer-1+
- TEST-MCP-AIUNIT-002: aiUnit must independently review warning suppression governance decisions, requirements traceability, TODO state, generated artifacts, and source suppression inventory.
  Scope: layer-1+
- TEST-MCP-AUDIT-001: AuditPayloadMigrationTests and AuditPayloadProviderMigrationTests apply the real SQLite, PostgreSQL, and SQL Server migrations in scratch databases, verify legacy text reads and all four compressed payload round trips, and confirm the new migration ID is applied.
  Scope: layer-1+
- TEST-MCP-AUDIT-002: DbFkBehaviorTests reject tracked audit updates. SqlServerAuditPayloadMigrationTests assert seven SQL Server runtime-role DENY permissions. AuditPayloadMigrationTests verify AutoMigrate false rejects pending schema and accepts migrated schema.
  Scope: layer-1+
- TEST-MCP-AUDIT-003: FederationStateAdapterRegistryTests verify TODO snapshots omit audit history; WikiDumpG7OverlayTests verify both audit tables are marked OMIT-LOCAL and audit sentinels are absent from exported payloads.
  Scope: layer-1+
- TEST-MCP-AUDITACTION-001: Exercise real SessionLogService writes and readback: valid tool_execution metadata and tool_call/tool_result dialog survive without design-decision promotion; malformed order/description/status selected for persistence reject on submit/upsert/replace/section paths before any tracked or persisted mutation; mixed batches remain atomic; unrelated legacy/custom actions remain accepted. Use SQLite persistence so a later save cannot accidentally commit a rejected mutation. P1 named cases: DuplicateRequestIds_LastWinsBeforeActionValidation checks both new/existing sessions and invalid winners/losers; LateSnapshot_InvalidActionsRemainIgnored checks retained terminal history; ZeroExecutionOrder_RoundTripsWithoutPositionalRewrite checks the zero boundary; OtherSection_IgnoresUnwrittenActions checks section isolation. InvalidLaterTurn_RejectsWholeSubmission checks atomic rejection for new and existing sessions; InvalidResubmission_LeavesDeletedSessionDeleted checks rejected tombstone recovery, including readback after clearing the change tracker.
  Scope: layer-1+
- TEST-MCP-AUTH-010: Given the auth-token subsystem is initialized, when a request hits a workspace-independent /mcpserver/* route with an unknown or missing API key and no X-Workspace-Path, then WorkspaceAuthMiddleware returns 401. This is a regression test (previously returned 503).
  Scope: layer-1+
- TEST-MCP-AUTH-011: When WorkspaceTokenService.IsInitialized is false, WorkspaceAuthMiddleware returns 503 with a Retry-After header and a JSON body.
  Scope: layer-1+
- TEST-MCP-AUTH-012: WorkspaceTokenService.IsInitialized is false before any token is generated and true after GenerateToken.
  Scope: layer-1+
- TEST-MCP-BATCH-001: Regression tests SHALL verify all plugin batch requirement methods accept unindented YAML records, indented YAML records, and inline JSON-array records while preserving nested acceptanceCriteria arrays and boolean isSatisfied fields.
  Scope: layer-1+
- TEST-MCP-BUGTRIAGE-042: Grok CLI one-shot argument construction forwards an explicitly configured model. GrokCliAgentExecutionStrategyTests.BuildGrokArgumentList_ConfiguredModel_IncludesModelFlag verifies a real model name (for example grok-4.3) emits --model before --output-format in the one-shot argument list (validates TR-MCP-TRIAGE-003 runner invocation construction).
  Scope: layer-1+
- TEST-MCP-BUGTRIAGE-043: Grok CLI startup-rejection guards for the triage research runner (validates TR-MCP-TRIAGE-003). GrokCliAgentExecutionStrategyTests verify: the sentinel model value auto (any casing) or an empty model omits --model entirely so the CLI picks its default, because current Grok CLIs reject --model auto with "unknown model id" and the runner substitutes auto for unset tier models (BuildGrokArgumentList_AutoOrEmptyModel_OmitsModelFlag, 5 cases); effort flags are pinned to high because current Grok CLIs reject max with "unknown effort level" (BuildGrokArgumentList_ContainsExpectedFlagsInOrder). Evidence 2026-07-14: both rejections reproduced from captured run stderr; fixes deployed in 1.4.15/1.4.16; live research run for triage-group-27f5ecfe4c926fde completed exit 0 at 20:08Z, first successful run since 2026-07-07.
  Scope: layer-1+
- TEST-MCP-CLEARSESSION-001: Skills.Tests.ps1 (mcpserver-claude-code-plugin) asserts the clear-session SKILL.md contains the five ordered steps: workflow.sessionlog.completeTurn (end session), best-effort context clear plus the per-host manual clear commands (/clear, /new, New Task), CLAUDE.md and AGENTS.md instruction-file selection, add-profile execution, and a readiness report. Validates TR-MCP-CLEARSESSION-001 / FR-MCP-CLEARSESSION-001.
  Scope: layer-1+
- TEST-MCP-DB-006: Validates TR-MCP-DB-006. tests/McpServer.Support.Mcp.Tests/Services/KeyedAsyncLockTests.cs: AcquireAsync_SameKey_BlocksUntilReleased asserts a second same-key acquire stays pending (Task.WhenAny vs a 250ms delay) until the first is disposed, then completes; AcquireAsync_DifferentKeys_DoNotBlockEachOther asserts a different-key acquire completes while the first key is held. The pair discriminates a correct keyed lock from a no-op (fails SameKey) and a global lock (fails DifferentKeys) - red-verified against a no-op stub, green after implementing per-key reference-counted SemaphoreSlim. SessionLogServiceReplaceDeleteTests confirm DeleteTurnAsync correctness is preserved under the lock (AC2).
  Scope: layer-1+
- TEST-MCP-DOCFXWIKI-001: Tests must prove typed DocFX configuration, isolated process execution, secure artifact mapping, backward compatibility, and GitHub/Azure wiki output integration.
  Scope: layer-1+
- TEST-MCP-FAILSAFE-001: Pester proves Test-ReplFailsafeBackendUnreachable is true for backend_unavailable and HTTP 503, drain does not increment drainAttempts or quarantine on that abort, and a later drain in the same process can replay. Validates TR-MCP-FAILSAFE-001 / BUG-TRIAGE-159.
  Scope: layer-1+
- TEST-MCP-FED-BACKFILL-001: Per adapter: EnumerateAsync pages all workspace rows; hub ApplyAsync with GlobalWorkspaceId stores rows under the canonical workspace (not empty workspace) preserving ids/timestamps/versions; replay returns already_applied without duplicates. Backfill envelopes create no fanout rows. FederationBackfillService resumes from the durable cursor after interruption and marks per-workspace completion. FederationMiddleware serves reads locally while a workspace backfill is incomplete and forwards writes; switches to hub after completion. federation/status reports backfill state and staleReadStatus non-clear until complete. Parity report detects a missing row.
  Scope: layer-1+
- TEST-MCP-FED-PATH-001: Unit matrix {host: Windows, Linux (simulated)} x {input: C:\, C:/, \\srv\sh, //srv/sh, /home/x, relative, mixed separators} for the normalizer; triage submit/getReport/queryGroups on simulated Linux with Windows paths; architecture test scanning src/ for Path.GetFullPath on request paths outside the allowlist.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] The root-relative Combine cases of TR-MCP-FED-PATH-001 AC1 run in the normalizer contract base against the scripted MockData fixture and the production normalizer on both simulated hosts, with negative controls that reject an appended segment, a dropped drive, and a dropped UNC share. (evidence: tests/QBrainAi.Support.Mcp.Tests/Storage/WorkspacePathNormalizerContractTests.cs; WorkspacePathNormalizerMockDataTests.cs; ScriptedWorkspacePathNormalizer.cs; SimulatedWorkspaceHosts.cs)
- TEST-MCP-FED-ROUTE-001: Hub middleware/controller tests with in-memory topology: forwarded request (hub token, X-Mcp-Proxy-Id PAYTON-LEGION2, X-Workspace-Path F:\GitHub\McpServer) resolves to the proxy workspace canonical row and returns its TODOs; same with X-Workspace-Path = GlobalWorkspaceId; request with X-Mcp-Federation-Hop and unknown or missing workspace returns 4xx and never the hub primary workspace; hub token cannot address a workspace registered to a different proxy; LocalProxy FederationMiddleware sends GlobalWorkspaceId in X-Mcp-Global-Workspace-Id.
  Scope: layer-1+
- TEST-MCP-FED-WSSYNC-001: FederationTopologyService tests: enroll with configuration fields creates canonical rows whose proxy-owned fields equal the request; heartbeat with changed TodoPath updates the row; CurrentRequirementLayerKey set on the hub is preserved across heartbeats and reported as drift when the proxy differs; registration without configuration fields (old proxy) keeps current defaults. FederationLocalProxyEnrollmentService builds inventory carrying all proxy-owned fields.
  Scope: layer-1+
- TEST-MCP-FILETOOLS-001: Unit tests must cover repository discovery behavior, path safety, MCP schemas, client delegation, hosted-agent registration, QBAgent registration, and backward compatibility.
  Scope: layer-1+
- TEST-MCP-FILETOOLSINT-001: Integration tests must prove HTTP MCP, stdio MCP, REST client, hosted-agent, QBAgent, and workspace-isolation behavior for read_file, list_dir, and grep_files.
  Scope: layer-1+
- TEST-MCP-HEALTH-002: WorkspaceReadinessHealthCheck returns Healthy when an enabled primary workspace is registered and has a seeded token; returns Unhealthy when the token subsystem is uninitialized, no enabled workspace is registered, or the primary workspace has no seeded token.
  Scope: layer-1+
- TEST-MCP-HEALTH-003: Integration test with the data layer up: /mcpserver/todo returns 200 with a valid token and no X-Workspace-Path; unknown or missing keys return 401; /ready returns 200 Healthy with workspace-ready check listed.
  Scope: layer-1+
- TEST-MCP-HELP-001: Help transcript JSONL writer appends one JSON line per entry and reads all entries without overwriting prior lines.
  Scope: layer-1+
- TEST-MCP-HELP-002: Deterministic inbound guard blocks injection fixtures and allows benign bypass corpora with stable rule IDs.
  Scope: layer-1+
- TEST-MCP-HELP-003: Guard incident JSON logger persists one incident file per block and filters incidents by session id.
  Scope: layer-1+
- TEST-MCP-HELP-004: Conversation service terminates sessions on guard violations; options validator rejects invalid AgentHelp configuration.
  Scope: layer-1+
- TEST-MCP-HELP-005: HTTP integration tests cover session create, synchronous and streaming turns, transcript retrieval, and guardrail evidence persistence.
  Scope: layer-1+
- TEST-MCP-HELP-006: MCP STDIO tools agent_help_create_session, agent_help_submit_turn, and agent_help_get_status delegate to the conversation service with workspace overrides.
  Scope: layer-1+
- TEST-MCP-HELP-007: Typed AgentHelpClient methods dispatch to the expected /mcpserver/agent-help REST paths with matching DTO contracts.
  Scope: layer-1+
- TEST-MCP-HELP-008: REPL contract tests prove workflow.agenthelp.createSession, workflow.agenthelp.submitTurn, and workflow.agenthelp.getStatus dispatch through typed REPL workflow code, build caller-linkage seed metadata, and return standard result/error envelopes.
  Scope: layer-1+
- TEST-MCP-HELP-SEC-001: Injection fixture ignore-previous-instructions is blocked with rule injection.ignore-instructions.
  Scope: layer-1+
- TEST-MCP-HELP-SEC-002: Injection fixture api-key-exfiltration is blocked with rule injection.api-key-exfiltration.
  Scope: layer-1+
- TEST-MCP-HELP-SEC-003: Injection fixture write-todo-yaml is blocked with rule injection.write-todo-yaml.
  Scope: layer-1+
- TEST-MCP-HELP-SEC-004: Injection fixture disable-guardrails is blocked with rule injection.disable-guardrails.
  Scope: layer-1+
- TEST-MCP-HELP-SEC-005: Benign bypass fixtures remain allowed even when adjacent risky phrases appear in context.
  Scope: layer-1+
- TEST-MCP-HELP-SEC-006: Guardrail violations terminate the session and persist transcript plus incident evidence.
  Scope: layer-1+
- TEST-MCP-HELP-SEC-007: Marker prompt template contains the Agent Help (MCP Server issues) section and references MCP/REST invocation paths.
  Scope: layer-1+
- TEST-MCP-HOSTILEREVIEW-001: HostileReviewEntity_RoundTrip_SqlitePgSqlServer; HostileReviewSubmit_ValidRequest_CreatesQueueItem; HostileReviewSubmit_OversizedPayload_Rejected; HostileReviewSubmit_ForeignWorkspace_403.
  Scope: layer-1+
- TEST-MCP-HOSTILEREVIEW-002: HostileReviewSubmit_MissingArtifact_ReturnsDiagnosticNotSilentOmit; HostileReviewSubmit_StaleOrUnauthorizedLink_Diagnostic; HostileReviewSubmit_AmbiguousLink_Diagnostic; HostileReviewDispatch_ReauthorizesEveryArtifact; HostileReviewDispatch_ChangedHashOrRevokedAccess_FailsClosed; HostileReviewProtectedContext_NoRawPromptInQueueNotificationLogOrResult; HostileReviewResolvedInput_PerArtifactAndAggregateLimitsFailClosed; HostileReviewArtifact_SecretSentinelsNeverEcho; HostileReviewCrossWorkspaceArtifact_NeverResolves.
  Scope: layer-1+
- TEST-MCP-HOSTILEREVIEW-003: HostileReviewExecution_RecordsModelEffortAgentTemplateRunId; HostileReviewExecution_OmitsFabricatedTokenCounts.
  Scope: layer-1+
- TEST-MCP-HOSTILEREVIEW-004: HostileReviewGet_NormalizedFindings_TaxonomyComplete; HostileReview_RequestQuality_ScoresDisclosureScopeObjectiveConfidenceContext.
  Scope: layer-1+
- TEST-MCP-HOSTILEREVIEW-005: HostileReviewQuery_ByModelAndEffort_ReturnsOnlyMatchingRuns; HostileReviewQuery_ByRequesterAndTargetType_AndFilters; HostileReviewQuery_NoMatch_EmptyList.
  Scope: layer-1+
- TEST-MCP-HOSTILEREVIEW-006: HostileReview_Default_DoesNotMutateProductFiles; HostileReview_SurfaceParity_RestReplDirectorPlugin.
  Scope: layer-1+
- TEST-MCP-HVPIPE-001: Named tests of FR-MCP-HVPIPE-001: HostileValidationOptionsMissingUnknownOrDuplicateReviewerFailsStartup; ReviewerSeedingInsertsFiveOnceNeverOverwrites; ReviewerAdapterDefaultsToHighestEffortAndCliDefaultModel; UpdateReviewerStaleStateVersion409NothingChanged; UpdateReviewerLaunchChangeUnverifiedNotifiesAndRequestsProbe; ReviewerSurfacesParityGoldenContract.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-001-AC001: HostileValidationOptionsMissingUnknownOrDuplicateReviewerFailsStartup.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-001-AC002: ReviewerSeedingInsertsFiveOnceNeverOverwrites.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-001-AC003: ReviewerAdapterDefaultsToHighestEffortAndCliDefaultModel.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-001-AC004: UpdateReviewerStaleStateVersion409NothingChanged.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-001-AC005: UpdateReviewerLaunchChangeUnverifiedNotifiesAndRequestsProbe.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-001-AC006: ReviewerSurfacesParityGoldenContract.
- TEST-MCP-HVPIPE-002: Named tests of FR-MCP-HVPIPE-002: AgentIdentityGoldenContractTests; SubmitterAliasesNeverAssignedClaudeFamily; FinalVerificationSubmitterReviewerRefusedNotIndependent; SubmitterExclusionUsesTurnAgentNeverFreeText; RoundRobinAssignsInPoolOrderAfterCursor; RoundRobinCursorSurvivesRestart; RoundRobinConcurrentSubmissionsConsecutiveReviewersThreeProviders; ClaimReassignsWhenAssigneeNoLongerEligible; AuthorAgentNeverAssignedAndRefusedByVerification; ClaimReassignmentSkipsFullReviewerAndAllFullWritesNothing; ClaimSkipsRequestWhoseEligibleAssigneeIsFull.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-002-AC001: AgentIdentityGoldenContractTests.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-002-AC002: SubmitterAliasesNeverAssignedClaudeFamily; FinalVerificationSubmitterReviewerRefusedNotIndependent.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-002-AC003: SubmitterExclusionUsesTurnAgentNeverFreeText.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-002-AC004: RoundRobinAssignsInPoolOrderAfterCursor.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-002-AC005: RoundRobinCursorSurvivesRestart.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-002-AC006: RoundRobinConcurrentSubmissionsConsecutiveReviewersThreeProviders.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-002-AC007: ClaimReassignsWhenAssigneeNoLongerEligible.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-002-AC008: AuthorAgentNeverAssignedAndRefusedByVerification.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-002-AC009: ClaimReassignmentSkipsFullReviewerAndAllFullWritesNothing; ClaimSkipsRequestWhoseEligibleAssigneeIsFull.
- TEST-MCP-HVPIPE-003: Named tests of FR-MCP-HVPIPE-003: ReviewerBecomesOnlineOnlyByPassingProbe; ProbePassedReviewerOnlineResolvedModelAndEffort; ProbeFailuresOfflineWithCodeAndIncident; ProbeEvidenceCorruptServerIncidentReadinessUnchanged; NonceRepeatNoSecondProbe; NonceDisabledReviewer409; WorkerStartupProbesUnverifiedEnabledReviewersOnly; WorkerRestartNeverRetriesSettledProbe; StaleProbeNeverQualifiesNewConfiguration; StaleProbeFailureShutdownOrExpiryLeavesNewConfigurationUnchanged; ProbeFailureOfflineWithIncidentAndShutdownRequestsProbeAgain.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-003-AC001: ReviewerBecomesOnlineOnlyByPassingProbe.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-003-AC002: ProbePassedReviewerOnlineResolvedModelAndEffort.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-003-AC003: ProbeFailuresOfflineWithCodeAndIncident; ProbeEvidenceCorruptServerIncidentReadinessUnchanged.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-003-AC004: NonceRepeatNoSecondProbe.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-003-AC005: NonceDisabledReviewer409.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-003-AC006: WorkerStartupProbesUnverifiedEnabledReviewersOnly; WorkerRestartNeverRetriesSettledProbe.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-003-AC007: StaleProbeNeverQualifiesNewConfiguration; StaleProbeFailureShutdownOrExpiryLeavesNewConfigurationUnchanged.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-003-AC008: ProbeFailureOfflineWithIncidentAndShutdownRequestsProbeAgain.
- TEST-MCP-HVPIPE-004: Named tests of FR-MCP-HVPIPE-004: ScopeResolutionTurnRangeAndSessionMembers; ScopeResolutionSubmittingTurnExcludedOrRefused; ScopeResolutionOverMaxRefusedNeverTruncated; SessionScopeSnapshotAtAdmissionLaterTurnNotMember; UnnecessaryTurnIncludedAssessedAndReviewed; ScopeResolutionInvalidRangeOrSession400.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-004-AC001: ScopeResolutionTurnRangeAndSessionMembers.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-004-AC002: ScopeResolutionSubmittingTurnExcludedOrRefused.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-004-AC003: ScopeResolutionOverMaxRefusedNeverTruncated.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-004-AC004: SessionScopeSnapshotAtAdmissionLaterTurnNotMember.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-004-AC005: UnnecessaryTurnIncludedAssessedAndReviewed.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-004-AC006: ScopeResolutionInvalidRangeOrSession400.
- TEST-MCP-HVPIPE-005: Named tests of FR-MCP-HVPIPE-005: AdmissionRefusalListsEveryAffectedTurn; AdmissionConcurrentOverlapOneWinsThreeProviders; AdmissionNoEligibleReviewer409WithReviewerContext; HvSubmitVerbNoReviewerAddsContext; SubmitIdempotencyKeyReplayAndReuse; SubmitRequestShapeHasNoForceFlag.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-005-AC001: AdmissionRefusalListsEveryAffectedTurn.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-005-AC002: AdmissionConcurrentOverlapOneWinsThreeProviders.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-005-AC003: AdmissionNoEligibleReviewer409WithReviewerContext.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-005-AC004: HvSubmitVerbNoReviewerAddsContext.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-005-AC005: SubmitIdempotencyKeyReplayAndReuse.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-005-AC006: SubmitRequestShapeHasNoForceFlag.
- TEST-MCP-HVPIPE-006: Named tests of FR-MCP-HVPIPE-006: RequirementReferenceExtractorFindsResolvesAndOrders; RequirementReferenceUnresolvedListedNeverRefused; SubmitContextTooLongOrUnknownFormat400; ContextSecretRedactedInBriefAndReads.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-006-AC001: RequirementReferenceExtractorFindsResolvesAndOrders.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-006-AC002: RequirementReferenceUnresolvedListedNeverRefused.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-006-AC003: SubmitContextTooLongOrUnknownFormat400.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-006-AC004: ContextSecretRedactedInBriefAndReads.
- TEST-MCP-HVPIPE-007: Named tests of FR-MCP-HVPIPE-007: SubmitWakesWorkerAutoStart; ClaimConcurrentWorkersOneRunPerRequestThreeProviders; LeaseExpiryAbandonsFencesAndRequeues; LeaseExpiryEndsRequestLikeFailedRun; StaleFenceEveryWorkerWriteRefusedNothingChanged; SweepFinalizesFinalizingRequestOnce; GracefulShutdownNotCountedNoIncident; WorkerClaimsOnlyItsWorkspace; WorkerDeathEndsReviewerTreeAndRequeues; RunStartRecordedBeforeBriefAndCrashRecovered; RunStartAndHeartbeatRetryableConflictRepeated; SecondSweepSkipsRunAlreadyTakenOver; LateSweepNeverRevivesCompletedRequest.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-007-AC001: SubmitWakesWorkerAutoStart.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-007-AC002: ClaimConcurrentWorkersOneRunPerRequestThreeProviders.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-007-AC003: LeaseExpiryAbandonsFencesAndRequeues; LeaseExpiryEndsRequestLikeFailedRun.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-007-AC004: StaleFenceEveryWorkerWriteRefusedNothingChanged.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-007-AC005: SweepFinalizesFinalizingRequestOnce.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-007-AC006: GracefulShutdownNotCountedNoIncident.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-007-AC007: WorkerClaimsOnlyItsWorkspace.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-007-AC008: WorkerDeathEndsReviewerTreeAndRequeues.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-007-AC009: RunStartRecordedBeforeBriefAndCrashRecovered; RunStartAndHeartbeatRetryableConflictRepeated.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-007-AC010: SecondSweepSkipsRunAlreadyTakenOver.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-007-AC011: LateSweepNeverRevivesCompletedRequest.
- TEST-MCP-HVPIPE-008: Named tests of FR-MCP-HVPIPE-008: OutputLinesAppendedWithinFlushIntervalInOrder; OutputFlushedAtByteThreshold; LiveOutputLineObservableBeforeReviewerExits; ReviewerRunHasNoTimeLimit; CancelQueuedImmediateRunningAtNextHeartbeat; CancelVersusFinalizeOneCommitsThreeProviders; CancelTerminalOrFinalizing409; ReviewerRunLeavesSameAgentLiveSessionUnchanged; ReviewerRunCannotChangeWorkspaceFilesPerAdapter; ReviewerClosesOwnTurnAndExitsUnderStopGate; ReviewerStopEndsWholeProcessTree; CancelCommittedBeforeCloseWinsOverFinalizing.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-008-AC001: OutputLinesAppendedWithinFlushIntervalInOrder.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-008-AC002: OutputFlushedAtByteThreshold.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-008-AC003: LiveOutputLineObservableBeforeReviewerExits.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-008-AC004: ReviewerRunHasNoTimeLimit.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-008-AC005: CancelQueuedImmediateRunningAtNextHeartbeat.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-008-AC006: CancelVersusFinalizeOneCommitsThreeProviders.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-008-AC007: CancelTerminalOrFinalizing409.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-008-AC008: ReviewerRunLeavesSameAgentLiveSessionUnchanged.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-008-AC009: ReviewerRunCannotChangeWorkspaceFilesPerAdapter.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-008-AC010: ReviewerClosesOwnTurnAndExitsUnderStopGate.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-008-AC011: ReviewerStopEndsWholeProcessTree.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-008-AC012: CancelCommittedBeforeCloseWinsOverFinalizing.
- TEST-MCP-HVPIPE-009: Named tests of FR-MCP-HVPIPE-009: ReviewerFailureOfflineWithIncident; IncidentFiledThroughTriageWithDedupeKey; RepeatedFailureSameTriageGroup; IncidentFilingRetriedUntilFiled; AttemptsExhaustedRequestFailedTurnsReleased; ServerFailureIncidentReviewerUnchanged; IncidentFilingNeverCallsTriageWhileLedgerUnavailable; FailAttemptRefusalEndsAttemptWithoutStateClaim; WorkerIterationStopsWhenIncidentFilingAuditUnavailable.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-009-AC001: ReviewerFailureOfflineWithIncident.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-009-AC002: IncidentFiledThroughTriageWithDedupeKey.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-009-AC003: RepeatedFailureSameTriageGroup.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-009-AC004: IncidentFilingRetriedUntilFiled.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-009-AC005: AttemptsExhaustedRequestFailedTurnsReleased.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-009-AC006: ServerFailureIncidentReviewerUnchanged.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-009-AC007: IncidentFilingNeverCallsTriageWhileLedgerUnavailable.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-009-AC008: FailAttemptRefusalEndsAttemptWithoutStateClaim; WorkerIterationStopsWhenIncidentFilingAuditUnavailable.
- TEST-MCP-HVPIPE-010: Named tests of FR-MCP-HVPIPE-010: FinalizeStoresPerTurnScoresAndSeparateHolistic; FinalizeIncompleteOutputRefusedNothingCommitted; OverallVerdictHolisticBelow098Disagree; VerdictAgreeWithFailFindingRefused; HolisticScoresNeverComputedFromTurnScores; ForgedWorkerRecordInMessageTextIgnored; ReviewerRecordsExtractedOnceFromAssembledMessagesPerCli; ExitZeroWithoutReviewerRecordsIsNotAReview; ReviewerTurnMustBeCompletedChoreHoldingEveryRecord; OverallVerdictOnePredicateOverTurnsHolisticResponsesFindings; OverallVerdictBindsAdmittedResponsesNotLaterState.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-010-AC001: FinalizeStoresPerTurnScoresAndSeparateHolistic.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-010-AC002: FinalizeIncompleteOutputRefusedNothingCommitted.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-010-AC003: OverallVerdictHolisticBelow098Disagree; VerdictAgreeWithFailFindingRefused.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-010-AC004: HolisticScoresNeverComputedFromTurnScores.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-010-AC005: ForgedWorkerRecordInMessageTextIgnored.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-010-AC006: ReviewerRecordsExtractedOnceFromAssembledMessagesPerCli.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-010-AC007: ExitZeroWithoutReviewerRecordsIsNotAReview.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-010-AC008: ReviewerTurnMustBeCompletedChoreHoldingEveryRecord.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-010-AC009: OverallVerdictOnePredicateOverTurnsHolisticResponsesFindings.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-010-AC010: OverallVerdictBindsAdmittedResponsesNotLaterState.
- TEST-MCP-HVPIPE-011: Named tests of FR-MCP-HVPIPE-011: EvidenceStoredGzipOnlyRoundTripExact; FinalizeCorruptOrTruncatedLogRefused; CompressionRoundTripFailureNothingSaved; AppendEvidenceCorruptFailsRunAsServerFault; ResponseLogDownloadMultiMemberGzipMatchesChain; OutputHasNoSizeLimitNeverTruncated; ResultEvidenceDigestExcludesItselfAndRoundTrips; ResultCompressionFailureFailsRunAsServerFault; CorruptStoredPayloadReadAnswersEvidenceCorrupt; CorruptRequestEvidenceAtBriefFailsRunAsServerFault.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-011-AC001: EvidenceStoredGzipOnlyRoundTripExact.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-011-AC002: FinalizeCorruptOrTruncatedLogRefused.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-011-AC003: CompressionRoundTripFailureNothingSaved; AppendEvidenceCorruptFailsRunAsServerFault.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-011-AC004: ResponseLogDownloadMultiMemberGzipMatchesChain.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-011-AC005: OutputHasNoSizeLimitNeverTruncated.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-011-AC006: ResultEvidenceDigestExcludesItselfAndRoundTrips.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-011-AC007: ResultCompressionFailureFailsRunAsServerFault.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-011-AC008: CorruptStoredPayloadReadAnswersEvidenceCorrupt.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-011-AC009: CorruptRequestEvidenceAtBriefFailsRunAsServerFault.
- TEST-MCP-HVPIPE-012: Named tests of FR-MCP-HVPIPE-012: FinalizeFaultAfterEachWriteNothingVisibleThreeProviders; FrozenTurnEveryWritePathRefused; EndedRequestReleasesTurns; FinalizeTwiceSecondReturnsStoredNothingWritten; FinalizeSnapshotChangedRefused; FinalizeReplacesOtherPendingHvReviewKeepsStatus; MigrationMarksHvReviewedTurnsFinalThreeProviders; LateVerificationFailureCannotUndoCommittedFinalization; FinalizerPropagatesEveryFailRunOutcome.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-012-AC001: FinalizeFaultAfterEachWriteNothingVisibleThreeProviders.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-012-AC002: FrozenTurnEveryWritePathRefused.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-012-AC003: EndedRequestReleasesTurns.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-012-AC004: FinalizeTwiceSecondReturnsStoredNothingWritten.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-012-AC005: FinalizeSnapshotChangedRefused.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-012-AC006: FinalizeReplacesOtherPendingHvReviewKeepsStatus.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-012-AC007: MigrationMarksHvReviewedTurnsFinalThreeProviders.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-012-AC008: LateVerificationFailureCannotUndoCommittedFinalization.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-012-AC009: FinalizerPropagatesEveryFailRunOutcome.
- TEST-MCP-HVPIPE-013: Named tests of FR-MCP-HVPIPE-013: SelfReviewVariedAdmissionAssignmentAndBriefIdentical; SelfReviewVariedVerdictIdentical; HostileValidationNeverReadsAgentReview.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-013-AC001: SelfReviewVariedAdmissionAssignmentAndBriefIdentical.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-013-AC002: SelfReviewVariedVerdictIdentical.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-013-AC003: HostileValidationNeverReadsAgentReview.
- TEST-MCP-HVPIPE-014: Named tests of FR-MCP-HVPIPE-014: FinalizeNeverChangesTodos; HostileValidationHasNoTodoWriteDependency.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-014-AC001: FinalizeNeverChangesTodos.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-014-AC002: HostileValidationHasNoTodoWriteDependency.
- TEST-MCP-HVPIPE-015: Named tests of FR-MCP-HVPIPE-015: ResultDocumentCompleteAndDelineated; ResultBeforeCompletion409; RunningRequestsQueryAndLastOutput; HvSurfacesParityGoldenContract.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-015-AC001: ResultDocumentCompleteAndDelineated.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-015-AC002: ResultBeforeCompletion409.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-015-AC003: RunningRequestsQueryAndLastOutput.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-015-AC004: HvSurfacesParityGoldenContract.
- TEST-MCP-HVPIPE-016: Named tests of FR-MCP-HVPIPE-016: OutputStreamReconnectExactlyMissedEvents; EtaReportedOrUnknownNeverEstimated; OutputStreamContinuesAcrossAttempts; OutputStreamMalformedLastEventId400; OutputStreamAfterRemoval410.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-016-AC001: OutputStreamReconnectExactlyMissedEvents.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-016-AC002: EtaReportedOrUnknownNeverEstimated.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-016-AC003: OutputStreamContinuesAcrossAttempts.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-016-AC004: OutputStreamMalformedLastEventId400.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-016-AC005: OutputStreamAfterRemoval410.
- TEST-MCP-HVPIPE-017: Named tests of FR-MCP-HVPIPE-017: NotificationOutboxCommittedWithChangeGapFree; NotificationStreamReconnectNoLossNoDuplicate; HvWatcherDeliversOwnCompletionOnce; HvWatcherIgnoresOtherSessionsRequests; HvWatcherCursorAndInboxOneReplaceCrashSafe; HvWatcherInboxesCompletionBeforeSubmitReturns; HvWatcherRedeliversUndeliveredEntryWithSequence.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-017-AC001: NotificationOutboxCommittedWithChangeGapFree.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-017-AC002: NotificationStreamReconnectNoLossNoDuplicate.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-017-AC003: HvWatcherDeliversOwnCompletionOnce.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-017-AC004: HvWatcherIgnoresOtherSessionsRequests.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-017-AC005: HvWatcherCursorAndInboxOneReplaceCrashSafe.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-017-AC006: HvWatcherInboxesCompletionBeforeSubmitReturns.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-017-AC007: HvWatcherRedeliversUndeliveredEntryWithSequence.
- TEST-MCP-HVPIPE-018: Named tests of FR-MCP-HVPIPE-018: EveryHvMutationHasLedgerRows; AuditUnavailableEveryHvCommandRefusedNothingWritten; EvidenceReadsAuditedOrRefused; InternalEvidenceReadsAuditedOrRefused; AuditFailureDuringRunHardStop; EvidenceRemovalLeavesAuditLedgerUnchanged.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-018-AC001: EveryHvMutationHasLedgerRows.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-018-AC002: AuditUnavailableEveryHvCommandRefusedNothingWritten.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-018-AC003: EvidenceReadsAuditedOrRefused; InternalEvidenceReadsAuditedOrRefused.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-018-AC004: AuditFailureDuringRunHardStop.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-018-AC005: EvidenceRemovalLeavesAuditLedgerUnchanged.
- TEST-MCP-HVPIPE-019: Named tests of FR-MCP-HVPIPE-019: NoAutomaticEvidenceRemoval; RemoveEvidenceKeepsResultAndHashes; RemoveEvidenceActiveRequest409; RemovedEvidenceRead410WithHashes; RemovedEvidenceEveryReadSurfaceAnswersRemoved.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-019-AC001: NoAutomaticEvidenceRemoval.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-019-AC002: RemoveEvidenceKeepsResultAndHashes.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-019-AC003: RemoveEvidenceActiveRequest409.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-019-AC004: RemovedEvidenceRead410WithHashes.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-019-AC005: RemovedEvidenceEveryReadSurfaceAnswersRemoved.
- TEST-MCP-HVPIPE-020: Named tests of FR-MCP-HVPIPE-020: HvSurfacesParityGoldenContract; HvInternalUnitsHaveNoPublicSurface; HvSubmitVerbBindsTurnStableKey; HvSubmitVerbRetryableNotQueued; ReplPolicyHostileValidationClassification; HvSubmitKeyBoundedAndCoversInvocationIdentity.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-020-AC001: HvSurfacesParityGoldenContract.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-020-AC002: HvInternalUnitsHaveNoPublicSurface.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-020-AC003: HvSubmitVerbBindsTurnStableKey.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-020-AC004: HvSubmitVerbRetryableNotQueued.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-020-AC005: ReplPolicyHostileValidationClassification.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-020-AC006: HvSubmitKeyBoundedAndCoversInvocationIdentity.
- TEST-MCP-HVPIPE-021: Named tests of FR-MCP-HVPIPE-021: FinalizeGradesLinkedResponsesInSameTransaction; FinalizeModeOffResponsesNotGraded; ResponseTransitionClosesHvItemOnly; ModelGradeHasNoPublicSurface; ResponseGradeFailingFindingsKeptWithRemediation; ResponseClosedMidReviewGradeKeptNotApplied; ModelGradeInternalUnitInsideFinalization.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-021-AC001: FinalizeGradesLinkedResponsesInSameTransaction.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-021-AC002: FinalizeModeOffResponsesNotGraded.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-021-AC003: ResponseTransitionClosesHvItemOnly.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-021-AC004: ModelGradeHasNoPublicSurface.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-021-AC005: ResponseGradeFailingFindingsKeptWithRemediation.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-021-AC006: ResponseClosedMidReviewGradeKeptNotApplied.
  - [ ] Named tests exist and prove FR-MCP-HVPIPE-021-AC007: ModelGradeInternalUnitInsideFinalization.
- TEST-MCP-HYGIENE-001: WorkspaceValidation_ResultContract_HasRequiredFields; WorkspaceValidation_CleanWorkspace_ZeroFindings; WorkspaceValidation_UnknownRuleCode_Diagnostic.
  Scope: layer-1+
- TEST-MCP-HYGIENE-002: Rule_FrTrTest_MissingAcceptanceCriteria_FindsRecord; Rule_TrWithNoFr_Orphan; Rule_FrMissingTrOrTest_Orphan; Rule_TestWithNoFr_Orphan; Rule_BrokenOrDuplicateMapping.
  Scope: layer-1+
- TEST-MCP-HYGIENE-003: Rule_DoneTrue_IncompleteTasks; Rule_DoneFalse_AllTasksComplete; Rule_DoneWithoutDoneSummary; Rule_RemainingContradictsCompletion; Rule_MissingDependencyTarget; Rule_MissingReferencedRequirementId.
  Scope: layer-1+
- TEST-MCP-HYGIENE-004: Rule_InProgressTurn_OlderThan48h_UtcClock; Rule_TriageNonTerminal_UsesLiveDomainEnum; Rule_StaleThresholdOverride_BoundedAuthenticatedRecorded; Validation_AuthRequired; Validation_CancellationHonored; Validation_LargeWorkspace_Paginates.
  Scope: layer-1+
- TEST-MCP-HYGIENE-005: Parity_RestDirectorReplPlugin_SameRuleCodesAndCounts; Director_Exit1_WhenErrorSeverityPresent; Director_Exit0_WhenWarningOnly; Validation_NeverAutoRepairs.
  Scope: layer-1+
- TEST-MCP-LLMSTRATEGY-001: Focused xUnit tests under tests/McpServer.Support.Mcp.Tests call shipped factory/strategy types. They assert two roles resolve two strategies, a stub strategy sees the same turn-context object, HTTP flattening serializes context into chat messages, and no new third-party agent SDK is required. Tests do not start the Windows service.
  Scope: layer-1+
- TEST-MCP-MARKER-004: Validates FR-MCP-MARKER-004 and TR-MCP-MARKER-004. Unit: RemoveMarker on a temp workspace containing a marker file with sentinel content deletes the marker and leaves zero files matching AGENTS-README-FIRST.yaml.deleted-*, replacing RemoveMarker_ArchivesMarkerFileInsteadOfDeletingIt which asserted exactly the opposite. Unit: RemoveMarker on a workspace that also contains legacy .mcp-server.yaml and .mcp-server.json markers deletes all three and leaves no tombstone for any of them. Unit: RemoveMarker on a workspace with no marker present completes without throwing and creates no files.
  Scope: layer-1+
- TEST-MCP-MEMORY-001: Storage isolation tests SHALL prove Global memories and Workspace memories in two workspaces list as Global plus current workspace only, and that update/remove by ID cannot mutate another workspace-local memory.
  Scope: layer-1+
- TEST-MCP-MEMORY-002: CRUD behavior tests SHALL prove add, list, update, remove, soft-delete omission, scope preservation, scope changes, invalid ID, invalid text, invalid category, and invalid scope failures.
  Scope: layer-1+
- TEST-MCP-MEMORY-003: ID generation tests SHALL prove MEMORY-{CATEGORY}-{NNN} IDs are globally unique across Global and Workspace scopes, category counters are independent, and manually supplied duplicate active IDs are rejected.
  Scope: layer-1+
- TEST-MCP-MEMORY-004: REST/client contract tests SHALL prove MemoryController and MemoryClient route, serialize, and deserialize memory scope, text, ID, and version correctly, and that McpServerClient.Memory propagates workspace and auth settings.
  Scope: layer-1+
- TEST-MCP-MEMORY-005: REPL contract tests SHALL prove workflow.memory.add, workflow.memory.list, workflow.memory.update, and workflow.memory.remove dispatch through typed REPL workflow code, support scope values where applicable, and return standard result/error envelopes.
  Scope: layer-1+
- TEST-MCP-MEMORY-006: Marker-template contract tests SHALL prove default-marker-prompt contains the MCP Memories section, exact REQUIRED MEMORIES header, memory tool names, ID format, scope guidance, no-secrets guidance, agent-local import safeguards, updatedBy attribution, and session-log action guidance.
  Scope: layer-1+
- TEST-MCP-MEMORY-007: YAML schema and stdio contract tests SHALL prove valid workflow.memory.* envelopes pass; add without text fails; add/update with invalid scope fails; update/remove without ID fails; invalid MEMORY ID fails; unknown memory methods fail; and docs/stdio-tool-contract.json includes all memory tools.
  Scope: layer-1+
- TEST-MCP-MEMORY-008: Scope ordering tests SHALL prove list surfaces and required-memory injection return Global memories first sorted by ID and Workspace memories second sorted by ID, excluding workspace rows from other workspaces.
  Scope: layer-1+
- TEST-MCP-MEMORY-009: Agent plugin validation SHALL prove each supported plugin exposes memory tools and either injects REQUIRED MEMORIES with Global-first ordering at supported request boundaries or documents the host limitation with an explicit memory-list fallback.
  Scope: layer-1+
- TEST-MCP-MEMORY-FED-001: Memory federation tests SHALL prove memory adapter diagnostics, /mcpserver/memory domain inference, explicit-ID queued create eligibility, no-ID create rejection, queued memory update/delete replay metadata, signed envelope apply, stale base-version conflict behavior, fanout row creation, recipient apply, workspace ownership enforcement, invalid payload conflicts, version behavior, timestamp preservation, and idempotent soft-delete semantics.
  Scope: layer-1+
- TEST-MCP-PATHNORM-001: Per BDP v4 (MockData fixture green before production code, then Real fixture). Storage-key equivalence: C:\Users\kingd, C:/Users/kingd, c:/users/KINGD -> one key windows:C:/USERS/KINGD; \\srv\share\x and //srv/share/x -> one key keeping leading //; \\?\C:\x -> C:/x. Remote-path detection treats //srv/share as remote. ToNativePath returns backslash paths on Windows. Rekey: value mapping f(x) touches only Windows-shaped values and is idempotent; planner builds the mapping and statement plan from a scripted snapshot; executors per provider (SQLite in-memory; SQL Server and PostgreSQL provider fixtures) rewrite all workspace columns and recompute hashed values with FK validation; report counts unchanged per table and per workspace; startup guard rejects an unconverted store; rehearsal on a restored copy of McpServer_PAYTON_LEGION2 recorded as receipts. GlobalWorkspaceId for (PAYTON-LEGION2, F:/GitHub/McpServer) is stable across both input separators.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] The native-boundary (ToNativePath) behavior for a non-Windows host is proven against a simulated host, on Windows as well, without a host-conditional skip; the Windows-host cases keep running on a Windows host.
- TEST-MCP-PLUGIN-011: Validates TR-MCP-PLUGIN-011. mcpserver-claude-code-plugin/tests/StopGateHardening.Tests.ps1: 'does not block a completed code-edit turn whose only audit signal is commits' seeds current-turn.yaml (status=completed, codeEdits=1, all audit counters 0 except auditCommits=2) and asserts the stop-gate output does not contain a block/'audit is incomplete'; 'no-ops instead of blocking for a task-notification phantom turn' seeds status=in_progress, queryTitle=<task-notification>, zero work, with a session-state.yaml whose lastUpdated is fresh so the 24-hour session-age rule does not decide the case (amended 2026-10-15 by operator decision "Fix the stale tests now", PLAN-SESSIONTRIAGEFIX-001, triage-report-58f224bf), and asserts no block and no completeTurn in the repl log. Red before the fix (audit-incomplete block; completeTurn-did-not-mark-completed block), green after. Pester harness uses MCP_CACHE_DIR_OVERRIDE + MCP_PLUGIN_REPL_LOG/RESPONSE mocks.
  Scope: layer-1+
- TEST-MCP-PLUGIN-012: Validates TR-MCP-PLUGIN-012. mcpserver-claude-code-plugin/tests/CurrentTurnSessionRebind.Tests.ps1 dot-sources ..\lib\repl-invoke.ps1, seeds session-state.yaml (sessionId B) and current-turn.yaml (sessionId A, in_progress), calls Assert-ReplCurrentTurnFresh, and asserts it returns true and current-turn.yaml sessionId stays A; a second case seeds an empty current-turn.yaml sessionId and asserts true and sessionId B (amended 2026-10-15 by operator decision "Fix the stale tests now", PLAN-SESSIONTRIAGEFIX-001, with TR-MCP-PLUGIN-012 AC1, triage-report-d29a6e26). Red before the BUG-TRIAGE-071/075 fix, when the stale-sessionId reject returned false. ReplFailsafe.Tests.ps1 remain green (AC2); its degraded case answers as an unavailable backend (HTTP 503 backend_unavailable), the degraded mode the core accepts, and asserts that completeTurn returns true with a queued receipt whose failsafe record is kept (triage-report-702082e6: the earlier stand-in answered persisted plus degraded, which the core rejects as contradictory).
  Scope: layer-1+
- TEST-MCP-PLUGIN-013: HookTurnDedupe.Tests.ps1: the UserPromptSubmit hook reuses the open turn on a duplicate prompt (returns turn-already-open, does not call beginTurn) and opens a new turn for a differing prompt. Hermetic via a self-contained marker plus the forced host identity. Validates TR-MCP-PLUGIN-013 / BUG-TRIAGE-077.
  Scope: layer-1+
- TEST-MCP-PLUGINCORE-001: bats: sync writes manifest+sha256; guard OK after sync; fails on edit; fails on deletion; manifest required; re-sync repairs.
  Scope: layer-1+
- TEST-MCP-PLUGINCORE-002: core-guard job fails on a seeded undeclared lib file and passes when declared in PLUGIN-RESIDUAL.txt.
  Scope: layer-1+
- TEST-MCP-PLUGINCORE-003: bats: daemon roundtrip with --- terminator; one child serves N sends; auto-restart after kill; concurrent sends; persistent wrapper threads JSON params and honors fallback.
  Scope: layer-1+
- TEST-MCP-PLUGINCORE-004: Automated PowerShell runtime and plugin parity tests SHALL cover dictionary-backed multi-item dialog parsing, persistence delegation, empty-payload failure, propagation, and checksum integrity with zero failures and zero skips.
  Scope: layer-1+
- TEST-MCP-PLUGINCORE-005: Validates TR-MCP-PLUGINCORE-005. Doc-presence + parse check (receipt captured 2026-07-16): ConvertFrom-Yaml parses both templates/prompt-templates.yaml and src/McpServer.Support.Mcp/graphrag-global/input/canonical/templates/prompt-templates.yaml; both contain the strings 'same volume as the target' and 'cross-volume move' in the PowerShell.Mcp Command Routing block; the added guidance text contains no em-dashes/en-dashes (pre-existing dashes elsewhere in the template are out of scope).
  Scope: layer-1+
- TEST-MCP-PLUGIN-HEADER-002: Validates TR-MCP-PLUGIN-HEADER-001. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1 'TEST-MCP-PLUGIN-HEADER-002 emits the cache transcript path only when that file exists': with the cache session.jsonl actually created, the resolver reports it; the companion assertion in 'TEST-MCP-PLUGIN-PSONLY-001 resolves default agent runtime header fields' proves it is empty when the file was never created. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-PLUGIN-HEADER-003: Validates TR-MCP-PLUGIN-HEADER-001. PluginPowerShellRuntime.Tests.ps1 'TEST-MCP-PLUGIN-HEADER-003 prefers the verified provider session id and transcript from the host payload': ProviderSessionId and an existing TranscriptPath supplied from the host hook payload win over the MCP session id and cache path. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-PLUGIN-HEADER-004: Validates TR-MCP-PLUGIN-HEADER-001. PluginPowerShellRuntime.Tests.ps1 'TEST-MCP-PLUGIN-HEADER-004 never reports the plugin version as the agent executable version': with MCP_PLUGIN_VERSION set to 1.82.0 and executable discovery impossible, agentExecutableVersion is 'unknown' and never 1.82.0. Red before the fallback removal, green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-PLUGIN-HEADER-005: Validates TR-MCP-PLUGIN-HEADER-001. PluginPowerShellRuntime.Tests.ps1 'TEST-MCP-PLUGIN-HEADER-005 never re-submits a stale fabricated transcript path or echoed session id from cache': seeds session-state.yaml with a non-existent transcript path and an agentSessionId equal to the MCP session id, then asserts the SubmitAsync payload contains neither. Red before the submit-layer guard in Invoke-ReplPersistTurn, green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-PLUGININT-001: A shared deterministic Theory and companion AiTheory matrix must exercise the real Session Log workflow for all supported agent plugins.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] The deterministic matrix gives each PowerShell host (Codex, ClaudeCode, ClaudeCowork, Copilot, Grok) its scenario session id <AgentSourceType>-<yyyyMMddTHHmmssZ>-pluginint through MCP_SESSION_ID, the plugins' own session-id input (New-ReplPluginSessionId and New-PluginSessionId read it first), from its first plugin call, so bootstrap, openSession and every later verb persist under that id and Theory_Agent_BootstrapBeginAppendComplete_CapturesIdsCacheShaReceipt captures it; Node hosts are unchanged (triage-report-914a2812; PLAN-SESSIONTRIAGEFIX-001 E5; tests in tests/QBrainAi.PluginIntegration.Tests/PluginScenarioSessionIdTests.cs: ScenarioSessionEnvironmentAssertion_AcceptsPinnedIdAndRejectsMissingOtherOrSuffixOnly, ScenarioSessionEnvironment_PowerShellHosts_CarryScenarioSessionId, ScenarioSessionEnvironment_NodeHosts_Unchanged; and the five PowerShell rows of Theory_Agent_BootstrapBeginAppendComplete_CapturesIdsCacheShaReceipt).
  - [ ] Each docs/receipts/pluginint-p20-* receipt carries, committed beside its summary.json, the harness log that summary names (logFile), byte-identical to the log the P20 harness wrote; the repository's ignore rules do not exclude docs/receipts/pluginint-p20-*/harness.log although *.log stays ignored elsewhere, and PluginUpdateServiceHarnessTests reads the newest receipt's log (UpdateService, sanitized, Failed 0, Skipped 0) (triage-report-96ea2f71; PLAN-SESSIONTRIAGEFIX-001 E2; tests: PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero, and git check-ignore on both receipt logs as recorded evidence).
  - [ ] The harness writes to every plugin host process's stdin as UTF-8 without a byte order mark (NodeMcpStdioSession for the Node MCP stdio host, RealPluginProcessRunner for the others), so the first JSON-RPC line a Node MCP stdio host reads (initialize) is plain JSON its SDK parses, and the Cline v1 session initializes instead of timing out (triage-report-fbac91dc; PLAN-SESSIONTRIAGEFIX-001 E4; root cause measured 2026-10-15: with StandardInputEncoding Encoding.UTF8 .NET 10 writes EF BB BF before the first line on a pipe; tests in tests/QBrainAi.PluginIntegration.Tests/PluginStdinEncodingTests.cs: StdinEncodingAssertion_AcceptsBomFreeAndRejectsBomPrefixed, PluginHostStdin_Encoding_HasNoPreamble, RealPluginProcessRunner_ChildStdin_StartsWithJson; and the Cline rows of the deterministic matrix).
  - [ ] The AI lane's aiUnit strategy names a command the operating system running the lane can start (Linux: an executable PowerShell script with a pwsh shebang; Windows: a .cmd launcher), present in the test output folder, and that command asks grok for the plugin evaluator's own JSON: a schema requiring valid (boolean), missingFields and contradictions (string arrays) and allowing nothing else, with the prompt aiUnit passes kept intact as one argument and grok's structured output returned, so every host's AI-lane receipt carries the model's verdict and a receipt the model judges invalid fails the lane; QBrainAi.Review.Tests keeps its own wrapper (triage-report-3546932eb5e94bbc8602fcb586cf5d5b, BUG-BASELINE-017; PLAN-SESSIONTRIAGEFIX-001 E6; operator decision 2026-10-15 "Launch + evaluator schema"; tests in tests/QBrainAi.PluginIntegration.Tests/AiEvaluatorStrategyTests.cs: EvaluatorStrategyAssertion_AcceptsStartableEvaluatorAndRejectsCmdOnUnixMissingOrReviewSchema, EvaluatorStrategy_OutputFolder_NamesStartableCommand, EvaluatorWrapper_FakeGrok_SendsEvaluatorSchemaAndReturnsStructuredOutput; and the eight AiTheory_Agent_RequiresValidJsonFields rows of the AI lane). The evaluator's instructions state the receipt contract: agent, cacheFolder, entrypoint and status (completed) are required non-empty strings, sessionId, requestId and sourceSha identify the persisted turn, and deterministicFailure is optional and false when absent, so missingFields names only required fields that are absent or empty; the evaluation waits for the active strategy's TimeoutSeconds (one setting, 120 seconds against measured grok latencies of 28.7 to 59.3 seconds) plus a 30-second margin, inside the theory's 240-second timeout; the strategy names no model or temperature, so grok runs its default model (operator rule 2026-10-15: requirements name no model) (PLAN-SESSIONTRIAGEFIX-001 E6 follow-up after pluginint-20261015T1311, where 5 of 8 AI rows failed, 2 on the 60-second wait and 3 on missingFields=deterministicFailure; tests: EvaluatorContractAssertion_AcceptsStatedContractAndRejectsUnstatedOrRequiredFlag, EvaluatorWaitAssertion_AcceptsStrategyTimeoutPlusMarginAndRejectsFixedOrOverBudget, EvaluatorStrategyModelAssertion_AcceptsNoModelSpecAndRejectsPinnedModelOrTemperature, EvaluatorPrompt_Fixture_StatesReceiptContract, EvaluatorWait_Fixture_UsesStrategyTimeoutWithinTheoryBudget).
  - [ ] Each docs/receipts/pluginint-p19-* receipt carries, committed beside its summary.json, every log its P19 run wrote there: the native-suite logs its plugins and afterSync rows name (logFile) and, where the run kept one, its sync-agent-plugins.log (which the C-green-P19 review read), each byte-identical to the log the run wrote; the repository's ignore rules do not exclude docs/receipts/pluginint-p19-*/*.log although *.log stays ignored elsewhere, and PluginNativeSuiteReceiptTests reads the newest receipt's logs (the eight official plugins before and after SyncAgentPlugins, each Failed 0 and Skipped 0) (PLAN-SESSIONTRIAGEFIX-001 E7, the E2 sibling the integration lane run pluginint-20261015T1151 found; operator 2026-10-15 "Fix issues correctly"; tests: PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero and PluginNativeSuite_AfterSyncAgentPlugins_FailedZeroSkippedZero, and as recorded evidence git check-ignore and each copy's SHA-256 against its PAYTON-LEGION2 original).
  - [ ] The harness starts PowerShell plugin hosts through the PowerShell executable name each operating system provides, pwsh.exe on Windows and pwsh elsewhere, so the matrix needs no pwsh.exe alias on Linux (PLAN-SESSIONTRIAGEFIX-001 E8, operator decision 2026-10-15 "Fix them in this plan"; observed: PluginHostProcessAdapter.cs line 82 named pwsh.exe on every operating system, and LAB-OMARCHY passed only through its ~/.local/bin/pwsh.exe and /usr/local/bin/pwsh.exe symlinks; tests: PowerShellHostExecutableAssertion_AcceptsPerOsNameAndRejectsFixedName, PluginHostProcessAdapter_PowerShellHost_UsesOsExecutableName, and the PowerShell rows of Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr).
- TEST-MCP-PLUGIN-TRIAGE-001: Every plugin skill bundle documents when and how to submit triage reports and the async expectation.
  Scope: layer-1+
- TEST-MCP-PRODUCT-001: Unit tests for ProductEntity/membership CQRS handlers: isolation, PROD-* key accept (PROD-MCPSERVER) and reject (mcpserver, empty, missing prefix), unique key, soft-delete, owner add/remove, self-leave, unknown workspace rejected. Files: ProductEntityTests, CreateProductCommandHandlerTests, AddProductMemberCommandHandlerTests.
  Scope: layer-1+
- TEST-MCP-PRODUCT-002: Unit tests for product effective-requirements share: union, productScope=local, id collision two origins, origin layer miss excludes, leave drops sibling, outsider cannot share. File: GetProductEffectiveRequirementsQueryHandlerTests.
  Scope: layer-1+
- TEST-MCP-PRODUCT-003: Controller/API unit tests: 400 invalid key, 409 duplicate, 403 non-owner, 404 outsider get. File: ProductsControllerTests.
  Scope: layer-1+
- TEST-MCP-PRODUCT-004: Client and MCP/REPL contract tests prove adapters dispatch CQRS only. File: ProductClientTests plus MCP/REPL allow-list tests.
  Scope: layer-1+
- TEST-MCP-PRODUCT-005: Migration apply on empty and production-shaped DBs for SQLite, PostgreSQL, and SQL Server harnesses. Must not re-add unrelated SessionLogs agent-header columns. File: ProductMigrationApplyTests.
  Scope: layer-1+
- TEST-MCP-PRODUCT-006: Context pack/search: member includes sibling FR body with origin; does not include sibling .cs chunks; source type product-requirements filters to those chunks. File: ProductRequirementContextTests.
  Scope: layer-1+
- TEST-MCP-QBAGENT-001: Marker present - QBAgent binds baseUrl/apiKey from the marker and reaches QuadBrain; only the QuadBrain route is exposed. Marker absent - QBAgent exits gracefully (defined exit, no endpoint contact, no unhandled exception).
  Scope: layer-1+
- TEST-MCP-QBAGENTINT-001: Integration tests for QBAgent sending a request to QuadBrain, receiving a response with or without tool actions, and executing the returned external tool calls via the Microsoft Agent Framework loop.
  Scope: layer-1+
- TEST-MCP-QBEXEC-001: Classifier marks mcp_ tools internal. Interceptor executes every catalog mcp_* tool server-side and strips it. Only non-mcp_ calls remain as OpenAI tool_calls. Failed/unhandled internals are notes. All-internal success is finish_reason stop with result content. Tests use mocks or WebApplicationFactory and do not depend on the Windows service.
  Scope: layer-1+
- TEST-MCP-QBEXEC-002: Unit tests assert every catalog mcp_* name is handled by QuadBrainInternalToolExecutor through in-process application/CQRS or transaction-gated services (ITodoService, IRepoFileService, session-log, requirements, GraphRAG, desktop, PowerShell, client invoke, mutating git). Unknown catalog-absent mcp_ names return Unhandled. Tests mock those services. Tests do not HTTP-call MCP endpoints and do not depend on the Windows service. OpenAI surface: mcp_todo_query success returns finish_reason stop with the query result in assistant content.
  Scope: layer-1+
- TEST-MCP-QBEXEC-003: Unit tests asserting each brain invocation's full prompt+output is written to the session log under TurnId, AoT reconciliation is logged, and internal-tool executed/failed outcomes are recorded with secrets redacted.
  Scope: layer-1+
- TEST-MCP-QBINT-001: Integration tests over POST /v1/chat/completions through the real ASP.NET pipeline with orchestration and the internal-tool executor replaced by deterministic doubles.
  Scope: layer-1+
- TEST-MCP-QBLIVE-001: Service-composition coverage of the real four-role Quad-Brain loop (QuadBrainOrchestrationService + BrainSlotInvocationService + BrainSlotRegistryService + in-memory key server), faking only IBrainSlotChatClientFactory and the committing transaction coordinator.
  Scope: layer-1+
- TEST-MCP-QBLIVEINT-001: Integration coverage that drives the real orchestration through POST /v1/chat/completions over four seeded slots, faking only the per-brain LLM call and the transaction coordinator.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] ChatCompletions_RealOrchestration_AotLoggerDialog_PersistsWithoutCoordinator: with a turn-transaction coordinator registered that rejects every sessionlog.dialog transaction, POST /v1/chat/completions through the real orchestration (four seeded slots, scripted per-brain calls, and X-Session-Id and X-Turn-Id naming a seeded turn) returns the arbiter's content; the coordinator records no sessionlog.dialog operation and rejects none; and the brain-interaction dialog items (FR-MCP-QBEXEC-003) persist on the seeded turn, because FR-MCP-173 ac-1 routes every session-log write around the coordinator (PLAN-SESSIONTRIAGEFIX-001 A11, triage-report-fe344399). The outcome assertion is proven first by AotDialogOutcomeAssertion_AcceptsBypassedPersistedDialogAndRejectsCoordinatorOrMissingDialog over recorded outcomes: a recorded sessionlog.dialog operation, a rejected count above zero, and no persisted dialog item are each rejected.
- TEST-MCP-QBOLLAMA-001: QuadBrainOllamaEndpointIntegrationTests exercise the OpenAI-compatible QuadBrain endpoint against a real local Ollama server that OllamaServerFixture adopts or starts (FR-MCP-QBOLLAMA-002), proving FR-MCP-QBOPENAI-001 runs the normal Creativity, Logic and Arbiter workflow without faking the LLM calls, on Windows and on Linux once InstallOllama has provisioned Ollama (TR-MCP-NUKE-016).
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] ChatCompletions_LocalOllamaDefaultModel_TriggersNormalBrainSlots (prompts "Hello" and "Create Hello World in C++"): with the local Ollama default model assigned to all four slots, each prompt triggers the normal Creativity, Logic and Arbiter workflow through POST /v1/chat/completions and returns non-empty content; every brain call uses that model at the Ollama endpoint, and the session log captures the brain outputs on the turn.
  - [ ] QBAgentRunLoop_LocalOllamaDefaultModel_DisplaysQuadBrainResponse (prompt "Hello"): QBAgent displays the QuadBrain response it received, never a placeholder; its transcript names the model QuadBrain and the QBrainAi endpoint and never the Ollama model id or endpoint.
  - [ ] ChatCompletions_OpenAiCompatibleFieldVariants_AreLoggedToSessionTurn: content, reasoning and reasoning_content outputs are persisted to the turn's processing dialog.
  - [ ] The class runs against a real local Ollama: OllamaServerFixture adopts a server already answering or starts one from a discovered executable and stops only what it started (FR-MCP-QBOLLAMA-002); on LAB-OMARCHY the executable is the one InstallOllama provisions (TR-MCP-NUKE-016; PLAN-SESSIONTRIAGEFIX-001 B13, triage-report-680916ed).
- TEST-MCP-QBOLLAMA-002: Validates FR-MCP-QBOLLAMA-002 and TR-MCP-QBOLLAMA-002 through fake-driven unit tests of OllamaServerController that run in the default gate without a real Ollama binary. Cases: (1) a probe that succeeds immediately reports WasAlreadyRunning true and StartedByController false and never invokes the launcher; (2) StopAsync after that case does not terminate the pre-existing server; (3) a probe that fails then succeeds invokes the launcher exactly once, reports StartedByController true, and polls until reachable; (4) a resolver returning no executable throws an InvalidOperationException whose message names the InstallOllama target and never invokes the launcher; (5) a probe that never succeeds throws after the configured timeout and terminates the process the controller started, leaving no orphan; (6) StopAsync is idempotent and terminates a controller-started process exactly once across repeated calls.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Case (7), the executable resolver: the candidates are, in order, each PATH entry's ollama (ollama.exe on Windows), on Windows Ollama's install folder LocalAppData/Programs/Ollama/ollama.exe, and the portable executable InstallOllama provisions under the Nuke test-tools folder (ollama/ollama.exe on Windows; ollama/bin/ollama on Linux) (TR-MCP-QBOLLAMA-002, TR-MCP-NUKE-016; PLAN-SESSIONTRIAGEFIX-001 B13, triage-report-680916ed). Shown first by OllamaExecutableCandidatesAssertion_AcceptsPerOsLayoutAndRejectsWindowsLayoutOnLinux, then OllamaExecutableCandidates_PerOperatingSystem_UsesTheShippedResolver.
- TEST-MCP-QBOPENAI-001: An inbound OpenAI ChatCompletion request maps to QuadBrain orchestration and returns an OpenAI-shaped response with the Arbiter output as the assistant message; later slices assert tool definitions flow through and assistant tool_calls are emitted, and that QBAgent executes them via the Agent Framework loop.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] FR-MCP-QBOPENAI-001 ac-5 and ac-6: the mock gate ChatRoundOutcomeAssertion_AcceptsFinalAnswerAndRejectsEmptyOrExtraRounds (recorded outcomes: one round returning the reply, and the last reply after the budget, are accepted; empty content, an extra round and a reply other than the last are rejected), then QuadBrainOpenAiChatServiceTests CompleteAsync_NoToolsNonEnvelopeReply_ReturnsItAfterOneRound, CompleteAsync_NoToolsContinueEnvelope_StillRePrompts and CompleteAsync_RoundsExhaustedWithoutFinal_ReturnsLastArbiterOutput over scripted orchestration; QuadBrainOllamaEndpointIntegrationTests (TEST-MCP-QBOLLAMA-001) then run green against gemma4:e4b (PLAN-SESSIONTRIAGEFIX-001 B13; operator decision 2026-10-15 "Both rules (Recommended)").
- TEST-MCP-QBPROGRESS-001: Unit tests prove (1) full orchestration reports started then completed for Creativity, Logic, and ArbiterOfTruth with role output on completed, and Creativity/Logic started events occur before Arbiter started; (2) stream=true SSE contains quadbrain.role events for those reports before the final assistant chunk; (3) QBAgent SSE client prints Creativity started and Creativity completed with output from a canned SSE body without waiting for [DONE] to print the first role line. Tests use mocks or in-memory HTTP, not the Windows service.
  Scope: layer-1+
- TEST-MCP-QBRAIN-001: Locked tokens. Namespaces and assemblies use QBrainAi. Package ids use the QBrainAI prefix. QBAgent and qbagent stay.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] No C# namespace declaration uses QBrainAI or QBrain.AI.
  - [ ] QBrainAi.QBAgent is present and qbagent remains the QBAgent tool command.
- TEST-MCP-QBRAIN-002: Solution, project folders, and RootNamespace use QBrainAi. NukeBuild.Tests and _build stay.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] QBrainAi.sln exists and QBrainAi.sln does not.
  - [ ] tests/Build.Tests keeps RootNamespace NukeBuild.Tests.
  - [ ] build/_build.csproj is still _build.
- TEST-MCP-QBRAIN-003: New package ids and 1.x SharpNinja.QBrainAi.* facades. qbrain-ai-repl is canonical and qbrain-ai-repl remains on the facade.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] The seven published projects use QBrainAI package ids.
  - [ ] Seven facade projects keep the old SharpNinja.QBrainAi package ids.
- TEST-MCP-QBRAIN-004: Phase 1 does not rename the GitHub repository. RepositoryUrl still points at the live repo.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Packed project RepositoryUrl values still contain SharpNinja/McpServer or sharpninja/QBrainAi.
- TEST-MCP-QBRAIN-005: /qbrainai and /mcpserver both declared for a representative controller. /mcp-transport unchanged. QBrainAi config wins over Mcp. QBRAINAI_* env wins over MCP_*. MCP_UNTRUSTED stays.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Todo route templates include qbrainai/todo and qbrainai/todo.
  - [ ] MapMcp still uses /mcp-transport.
  - [ ] GetEffectiveMcpValue prefers QBrainAi and falls back to Mcp.
  - [ ] GetRequestedInstanceName prefers QBRAINAI_INSTANCE and falls back to MCP_INSTANCE.
- TEST-MCP-QBRAIN-006: Docker names use qbrain-ai. Service script defaults use QBrainAi, QBrain.AI, and C:\ProgramData\QBrainAi. McpServer_Omarchy and PAYTON-LEGION2 remain.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] docker-compose.mcp.yml uses image qbrain-ai:latest.
  - [ ] Manage-McpService.ps1 defaults match the new service name, display name, and install path.
- TEST-MCP-QBRAIN-007: Inventory TSV classifies every hit. Persisted names mcp.db, .mcpServer, and McpDbContext remain. No new EF table-rename migration is added for the rebrand.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] docs/receipts/qbrain-ai-rebrand-phase1-inventory.tsv has a class and a closed action on every row.
  - [ ] Source still contains mcp.db, .mcpServer, and class McpDbContext.
- TEST-MCP-QBRAIN-008: Requirements ids are stored before the rename, and Phase 1 is not claimed done without the hostile-validation receipt.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] The five FR and eight TR headings exist in the requirements documents.
  - [ ] No docs/receipts/hv file in this change records OverallVerdict AGREE for Phase 1.
- TEST-MCP-QBSEED-001: Unit coverage for BrainSlotStartupSeeder over a real in-memory McpDbContext, real BrainSlotRegistryService, and the in-memory key server (only the credential resolver stubbed).
  Scope: layer-1+
- TEST-MCP-QBSKILLS-001: Unit tests asserting the parser requires name+description, rejects missing name, and reads optional allowed-tools.
  Scope: layer-1+
- TEST-MCP-QBSKILLS-002: Unit tests asserting discovery returns name+description only, load returns the full body, and discovery includes vendored + workspace skills.
  Scope: layer-1+
- TEST-MCP-QBSKILLS-003: Unit tests asserting list_skills and load_skill are exposed as external tools and return the discovery list and named body respectively.
  Scope: layer-1+
- TEST-MCP-QBTOOLS-001: Unit tests asserting read_file/write_file/edit_file/list_files are registered as non-mcp_ external tools and delegate to the MCP client Repo surface (no direct filesystem access).
  Scope: layer-1+
- TEST-MCP-QBTOOLS-002: Unit tests (mock IProcessRunner) asserting git status builds expected args and parses output, push targets origin only, and an unknown subcommand is rejected.
  Scope: layer-1+
- TEST-MCP-QBTOOLS-003: Unit test (mock ResolveExecutable) asserting run_bash returns available=false when bash.exe is missing and runs the command when present.
  Scope: layer-1+
- TEST-MCP-QBTOOLS-004: Unit tests for EditAsync: replace-unique, ambiguous-fails, replace-all, missing-old-fails, traversal-rejected, nonexistent-fails, audit+updated-event.
  Scope: layer-1+
- TEST-MCP-QBTOOLS-005: Unit tests asserting gated EditAsync commits+audits, rolls back to original on transaction reject, and fails under a degraded coordinator.
  Scope: layer-1+
- TEST-MCP-QBTOOLS-006: Unit tests asserting McpHostedAgentToolAdapter exposes mcp_repo_edit, mcp_bash, and mcp_git, and that mutating variants route through the transaction-gated core.
  Scope: layer-1+
- TEST-MCP-QBTOOLS-007: Unit tests asserting run_powershell executes a command in a hosted runspace and returns captured output/error streams.
  Scope: layer-1+
- TEST-MCP-QBTOOLSINT-001: Integration tests (CustomWebApplicationFactory + in-memory transport) where the agent loads a skill then calls edit_file (applied through server RepoFileService) and git status; plus internal mcp_repo_edit executed server-side never reaching the agent.
  Scope: layer-1+
- TEST-MCP-QUAD-SESSION-001: Per-session QuadBrain instance attachment over global brains with session metadata propagation.
  Scope: layer-1+
- TEST-MCP-REPL-001: ✅ **Complete** - Given a REPL host process, when a well-formed YAML command envelope is sent to stdin, then a YAML response envelope is emitted to stdout with `type: result` and the expected result payload. **Covered by:** `Iteration1_IntegrationTests`, `YamlFramingTests`, `YamlEnvelopeShapeTests`
  Scope: layer-1+
- TEST-MCP-REPL-002: ✅ **Complete** - Given a REPL host process, when malformed YAML is sent to stdin, then a structured error response is emitted with `type: error` and descriptive error details, without crashing the host process. **Covered by:** `FakeYamlSerializerTests`, `YamlFramingTests`
  Scope: layer-1+
- TEST-MCP-REPL-003: ✅ **Complete** - Given a REPL host with no bootstrap invocation, when an operational command is sent, then the response contains `type: error` and appropriate error code. **Covered by:** `ProtocolHandshakeTests`, `TrustBootstrapFlowTests`
  Scope: layer-1+
- TEST-MCP-REPL-004: ✅ **Complete** - Given a REPL host, when trust bootstrap is invoked with a valid marker file, then the host verifies the marker signature, performs the health nonce challenge, caches the API key, and returns success. **Covered by:** `TrustBootstrapFlowTests`, `MarkerFileTrustTests`, `MockTrustBootstrapServiceTests`
  Scope: layer-1+
- TEST-MCP-REPL-005: ✅ **Complete** - Given a REPL host with completed bootstrap, when the API key in the marker file is rotated, then the host detects rotation via marker file watch and emits appropriate notifications. **Covered by:** `AuthRotationTests`, `AuthKeyAndWorkspaceTests`, `StubAuthRotationHandlerTests`
  Scope: layer-1+
- TEST-MCP-REPL-006: ✅ **Complete** - Given bootstrapped REPL commands for TODO operations (`workflow.todo.*`), when invoked with valid args, then results match the equivalent client operation semantics. **Covered by:** `TodoWorkflowTests`, `Iteration3IntegrationTests`, `TodoWorkflowTestExtensions`
  Scope: layer-1+
- TEST-MCP-REPL-007: ✅ **Complete** - Given bootstrapped REPL commands for session log operations (`workflow.session.*`), when invoked with valid args, then results match the equivalent client operation semantics. **Covered by:** `SessionLogWorkflowTests`, `SessionLogWorkflowIntegration2Tests`, `SessionLogWorkflowProductionTests`, `Iteration2IntegrationTests`
  Scope: layer-1+
- TEST-MCP-REPL-007-1: Given `TryResolveWithDiagnostics` with a workspace path containing no marker file, when called, then the error message enumerates every directory walked from the start path to its root. **Covered by:** `MarkerFileClientOptionsResolverTests.TryResolveWithDiagnostics_WhenMarkerMissing_EnumeratesSearchedPaths`
  Scope: layer-1+
- TEST-MCP-REPL-007-2: Given an explicit `workspacePathOverride` pointing to a workspace with a valid marker, when `TryResolveWithDiagnostics` is called, then resolution succeeds and the returned options carry the marker's API key. **Covered by:** `MarkerFileClientOptionsResolverTests.TryResolveWithDiagnostics_AcceptsExplicitWorkspaceArgument`
  Scope: layer-1+
- TEST-MCP-REPL-007-3: Given a marker file whose canonicalization is tampered, when `TryResolveWithDiagnostics` is called, then the error names the marker path and identifies "signature" failure. **Covered by:** `MarkerFileClientOptionsResolverTests.TryResolveWithDiagnostics_WhenSignatureFails_ReportsReason`
  Scope: layer-1+
- TEST-MCP-REPL-007-4: Given a marker whose HMAC payload is signed with LF-only (`\n`) line endings (matching the production server's `MarkerFileService.AppendPayloadLine`), when `TryResolveWithDiagnostics` is called on Windows or any platform where `Environment.NewLine` differs from `\n`, then signature verification succeeds. **Covered by:** `MarkerFileClientOptionsResolverTests.TryResolveWithDiagnostics_VerifiesSignatureBuiltWithLfLineEndings`
  Scope: layer-1+
- TEST-MCP-REPL-008: ✅ **Complete** - Given bootstrapped REPL commands for context operations (`client.context.*`), when invoked with valid args, then results match the equivalent client operation semantics. **Covered by:** `GenericClientPassthroughTests`, `Iteration5IntegrationTests`
  Scope: layer-1+
- TEST-MCP-REPL-009: ✅ **Complete** - Given bootstrapped REPL commands for requirements management (`workflow.requirements.*`), when invoked with valid args, then results match the equivalent client operation semantics. **Covered by:** `RequirementsWorkflowTests`, `Iteration4IntegrationTests`
  Scope: layer-1+
- TEST-MCP-REPL-010: ✅ **Complete** - Given bootstrapped REPL commands for workspace selection, when invoked with valid workspace paths, then workspace context resolution matches expected behavior. **Covered by:** `WorkspaceSelectionTests`, `AuthKeyAndWorkspaceTests`
  Scope: layer-1+
- TEST-MCP-REPL-011: ✅ **Complete** - Given generic client passthrough operations, when invoked with valid method/args, then operations delegate to the correct client type and method without duplicating logic. **Covered by:** `GenericClientPassthroughTests`, `Iteration5IntegrationTests`
  Scope: layer-1+
- TEST-MCP-REPL-012: ✅ **Complete** - Given streaming TODO operations (`streamStatus`, `streamPlan`, `streamImplement`), when invoked, then events stream correctly with proper cancellation handling. **Covered by:** `TodoWorkflowTests` (streaming event tests)
  Scope: layer-1+
- TEST-MCP-REPL-013: ✅ **Complete** - Given a REPL host, when EOF is received on stdin, then the host terminates gracefully. **Covered by:** `EndToEndFlowTests`
  Scope: layer-1+
- TEST-MCP-REPL-014: ✅ **Complete** - Given a workflow operation that throws an exception, when the command is executed, then a structured error response is emitted and the command loop continues. **Covered by:** `SessionLogWorkflowTests`, `TodoWorkflowTests` (error handling paths)
  Scope: layer-1+
- TEST-MCP-REPL-015: ✅ **Complete** - Given request IDs in command envelopes, when commands are executed, then response envelopes echo the same request ID for request/response matching. **Covered by:** `RequestResponseCorrelationTests`, `YamlEnvelopeShapeTests`
  Scope: layer-1+
- TEST-MCP-REPL-016: ✅ **Complete** - Given workflow implementations, when registered in DI, then all dependencies are resolved from the container and no services are instantiated via `new` outside DI. **Covered by:** `McpServerClientIntegrationTests`, DI registration tests
  Scope: layer-1+
- TEST-MCP-REPL-017: ✅ **Complete** - Given workspace selection via workspace selector, when commands target specific workspaces, then workspace context is properly scoped. **Covered by:** `WorkspaceSelectionTests`, `AuthKeyAndWorkspaceTests`
  Scope: layer-1+
- TEST-MCP-REPL-018: Tests must verify canonical agent keying, explicit --agent propagation, absence of production AgentOverride leakage, and named-agent child process launch behavior.

PR87 also covers approved FR-MCP-REPL-008 ac-4: the verified-marker cache selector uses canonical QBRAINAI_REPL_CACHE_DIR, legacy MCP_REPL_CACHE_DIR under ProductEnvironment precedence, then the existing profile fallback. Independent explicit-input cases and wrong adapters pass the mock gate before implementation; the same assertions bind the selector, and owned REPL runtime cases verify the child cache remains inside its instance root. No fixture uses the live profile trust cache.
  Scope: layer-1+
- TEST-MCP-REPL-019: ✅ **Complete** - Given namespace-organized command shapes, when workflows execute, then operations delegate to typed client contracts without duplicating business logic. **Covered by:** `TodoWorkflowTests`, `SessionLogWorkflowTests`, `RequirementsWorkflowTests`, `GenericClientPassthroughTests`
  Scope: layer-1+
- TEST-MCP-REPL-020: ✅ **Complete** - Given concurrent REPL operations, when workflows maintain stateful context, then session state and TODO selection are properly isolated per workflow instance. **Covered by:** `SessionLogWorkflowTests` (state management), `TodoWorkflowTests` (selection state)
  Scope: layer-1+
- TEST-MCP-REPL-021: When TryResolveWithDiagnostics is called on a workspace path containing no marker file, the error message enumerates every directory walked from the start path to its root. This verifies diagnostic usability when marker files cannot be found during workspace resolution.
  Scope: layer-1+
- TEST-MCP-REPL-022: When an explicit workspacePathOverride pointing to a workspace with a valid marker is provided to TryResolveWithDiagnostics, resolution succeeds and the returned options carry the marker's API key.
  Scope: layer-1+
- TEST-MCP-REPL-023: When a marker file's canonicalization is tampered and TryResolveWithDiagnostics is called, the error names the marker path and identifies 'signature' failure. This verifies secure marker validation and error reporting.
  Scope: layer-1+
- TEST-MCP-REPL-024: Given a marker whose HMAC payload is signed with LF-only (\n) line endings (matching production MarkerFileService.AppendPayloadLine), when TryResolveWithDiagnostics is called on Windows or any platform where Environment.NewLine differs from \n, signature verification succeeds. This ensures cross-platform marker compatibility.
  Scope: layer-1+
- TEST-MCP-REPL-025: Mock-backed unit and real-filesystem integration tests SHALL prove primary and failsafe strategy isolation, non-terminal degradation isolation, terminal notification, replay artifact fidelity, V4 path scoping, atomic writes, cancellation, dual failure, and normal primary behavior with zero failures and zero skips.
  Scope: layer-1+
- TEST-MCP-REPL-026: Validates TR-MCP-REPL-011 (PascalCase session-id agent + openSession persistence). mcpserver-claude-code-plugin/tests/SessionIdCanonicalAgent.Tests.ps1 dot-sources ..\lib\repl-invoke.ps1: asserts Get-ReplCanonicalAgentName('default')='Default' and matches ^[A-Z][A-Za-z0-9]*$, 'claude-code'/'claudecode'='ClaudeCode', 'codex'='Codex', 'grok'='GrokCode'; and Invoke-WorkflowOpenSession with 'sessionId: ClaudeCode-...-explicit' writes status=verified + that sessionId into session-state.yaml and returns true. Red before implementation (functions did not exist / openSession was a no-op), green after. Note: uses id 026 because TEST-MCP-REPL-011 was already taken.
  Scope: layer-1+
- TEST-MCP-REPL-027: Validates TR-MCP-REPL-012. mcpserver-claude-code-plugin/tests/ReplMethodTimeout.Tests.ps1 dot-sources ..\lib\repl-invoke.ps1: asserts Get-ReplMethodTimeoutSeconds returns >30 for workflow.todo.analyzeRequirements and workflow.requirements.generateDocument and exactly 30 for workflow.sessionlog.completeTurn/beginTurn; and with REPL_TIMEOUT=45/REPL_LONG_TIMEOUT=600 set, returns 45 for sessionlog and 600 for analyzeRequirements. Red before implementation (function absent), green after; Invoke-ReplRaw now uses Get-ReplMethodTimeoutSeconds.
  Scope: layer-1+
- TEST-MCP-REPL-028: ReplWorkspaceResolution.Tests.ps1: a marker-bearing current directory outranks an inherited MCP_WORKSPACE_PATH when the repl bridge resolves the workspace. Validates TR-MCP-REPL-013 / BUG-TRIAGE-077.
  Scope: layer-1+
- TEST-MCP-REPL-029: Validates TR-MCP-REPL-014. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1 (Example-B override pattern): workflow.sessionlog.setTurnTitle updates current-turn.yaml queryTitle and invokes the server turn title-update path; workflow.sessionlog.setSessionTitle writes session-state.yaml title and invokes the server session title-update path; both fail closed (non-zero) when no session/turn cache is present. Red before the handlers, green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-REPL-030: Validates TR-MCP-REPL-015. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1 (Example-B override pattern capturing Invoke-ReplPersistTurn args): an incidental re-submit (appendActions/appendDialog/completeTurn/supersede) with no explicit queryTitle param omits the turn title (and session title) from the persisted payload so a server-preserved title is not clobbered; an explicit queryTitle param still sends and updates the turn title; beginTurn seeds the turn title and seeds the session title only when session-state has none. Red before the omit change, green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-REPL-031: Validates TR-MCP-REPL-016. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1, Describe "TEST-MCP-REPL-031 failsafe queue drain". Fixture: a throwaway queue supplied through MCPSERVER_FAILSAFE_DIR plus MCP_CACHE_DIR_OVERRIDE, seeded with three synthetic client.SessionLog.SubmitAsync records stamped 20260714T154733Z, 20260716T112949Z, 20260720T230559Z; Invoke-ReplRaw is overridden to record the replay order and report success, so no backend and no real queue is touched. Asserts Invoke-ReplFailsafeDrain returns scanned 3, replayed 3, failed 0, quarantined 0, aborted false; that the three replays arrive in ascending timestamp order carrying the original requestIds; and that the queue directory is empty afterwards. Red before Invoke-ReplFailsafeDrain existed (CommandNotFoundException), green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-REPL-032: Validates TR-MCP-REPL-016. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: three synthetic records in a MCPSERVER_FAILSAFE_DIR sandbox; the overridden Invoke-ReplRaw returns a server-level "type: error / code: validation_failed" result for the middle record and success for the other two. Asserts all three records are attempted (the rejected one does not dam the queue), the summary reports replayed 2 and failed 1 and aborted false, the rejected record is still on disk while the other two are gone, and the retained record now carries drainAttempts 1. Red before the drain existed, green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-REPL-033: Validates TR-MCP-REPL-017. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: a MCPSERVER_FAILSAFE_DIR sandbox holding one deliberately unparseable YAML file plus one well-formed record; Invoke-ReplRaw is overridden to count submissions and report success. Asserts exactly one submission happens (the malformed record is never replayed), the summary reports quarantined 1 and replayed 1, the malformed file is gone from the queue root, the quarantine directory holds one .yaml plus one non-empty .reason.txt sidecar, and the queue root is empty. Red before the quarantine path existed, green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-REPL-034: Validates TR-MCP-REPL-017. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: a single synthetic record seeded with drainAttempts 5 in a MCPSERVER_FAILSAFE_DIR sandbox; Invoke-ReplRaw is overridden to count submissions. Asserts Invoke-ReplFailsafeDrain -MaxAttempts 5 makes zero submissions, reports quarantined 1 and replayed 0, removes the record from the queue root, and leaves exactly one record in the quarantine directory: an attempt-exhausted record is set aside, never retried forever and never deleted. Red before the attempt budget existed, green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-REPL-035: Validates TR-MCP-REPL-016. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: two synthetic records in a MCPSERVER_FAILSAFE_DIR sandbox; the overridden Invoke-ReplRaw returns Success false with the transport error "MCP_UNTRUSTED: marker refresh failed before REPL request". Asserts the drain stops after the first attempt rather than hammering the whole queue, reports aborted true with a non-empty abortReason and replayed 0 and quarantined 0, leaves both records on disk, and does not add a drainAttempts counter to the first record, so an outage cannot burn the attempt budget of good turns. Red before the drain existed, green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-REPL-036: Validates TR-MCP-REPL-016. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: a record written through the production Write-ReplFailsafe into a MCPSERVER_FAILSAFE_DIR sandbox, so it is registered as in flight; Invoke-ReplRaw is overridden to count submissions. Asserts the drain makes zero submissions, reports skipped 1 and replayed 0, and leaves the in-flight record on disk so the owning submit still controls it; then asserts Clear-ReplFailsafe removes it, proving the in-flight registration is released on completion. Red before in-flight tracking existed, green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-REPL-037: Validates TR-MCP-REPL-016. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: Invoke-ReplFailsafeDrain overridden with a call counter. Asserts three consecutive Invoke-ReplFailsafeDrainOnFirstSuccess calls produce exactly one drain (the once-per-process latch holds even after the first pass), and asserts by source inspection that the body of Invoke-ReplRaw in plugins/core/lib-ps/repl-invoke.ps1 calls Invoke-ReplFailsafeDrainOnFirstSuccess, which is the wiring point that proves backend reachability before any replay. Red before the hook existed (CommandNotFoundException), green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-REPL-038: Validates TR-MCP-REPL-016. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: Invoke-ReplFailsafeDrain and Invoke-ReplRaw both overridden with counters. Asserts Invoke-ReplMethod -Method workflow.failsafe.drain -ParamsYaml "maxRecords: 7" dispatches locally: the drain runs exactly once, receives MaxRecords 7 from the params, Invoke-ReplRaw is never called (no server round trip for a plugin-local verb), and the returned text contains the YAML drain summary an operator reads. Red before the verb existed (drain call count 0), green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-REPL-039: Validates TR-MCP-REPL-017. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1. Fixture: a sandbox failsafe directory holding two queued records plus one record already in the quarantine subdirectory; plugins/core/lib-ps/mcp-status.ps1 is run as a child pwsh process with MCPSERVER_FAILSAFE_DIR and MCP_CACHE_DIR_OVERRIDE pointed at the sandbox. Asserts the emitted JSON reports failsafeDir equal to the sandbox queue, failsafeCount 2, failsafeQuarantineCount 1, and pendingCount 2, so queue depth is truthful and quarantined records are excluded. Red before the status change (failsafeDir null), green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-REPL-040: Validates FR-MCP-143 with TR-MCP-REPL-018, TR-MCP-REPL-019, TR-MCP-REPL-020, TR-MCP-CLIENT-001, and TR-MCP-HEALTH-003. Pester, plugin core: a turn upsert invoked with an empty title binds and persists, covering appendDialog without a queryTitle, appendActions, completeTurn, and the supersede path; before the fix each case fails at parameter binding with a mandatory-parameter error, which the red run quotes. Pester: a supersede where the server holds a refined title and the local cache holds only the hook's raw first line preserves the refined title, and a supersede with no local title still persists. Pester: failTurn closes a turn that exists only in the plugin cache with an empty in-process REPL state, where it previously returned "No active session exists". Unit, client: serializing each session-log request body through the real McpClientJsonContext resolver succeeds, and the audit-driven cases cover every request body found in the typed client rather than only the three known sites; before the fix the anonymous bodies throw NotSupportedException from GetTypeInfo. Unit, server: with an unreachable store the health path no longer reports plain Healthy while persistence is dead, the trust nonce still echoes exactly, and a backend-unavailable condition surfaces as one typed error across REST and the MCP tools instead of four different shapes.
  Scope: layer-1+
- TEST-MCP-REPL-TRIAGE-001: Full client.triage.* and workflow.triage.* REPL surface works with correct envelopes.
  Scope: layer-1+
- TEST-MCP-REQ-003: Validates TR-MCP-REQ-003 AC-001 under FR-MCP-040: getTr and getTest report each record's stored createdAt and updatedAt, never the time of the read (PLAN-SESSIONTRIAGEFIX-001 B2, triage-report-b72cfaf4; operator decision 2026-10-16 "Create TEST-MCP-REQ-003 (Recommended)").
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] TR-MCP-REQ-003 AC-001: RequirementsDatabaseDocumentServiceTests.GetById_ReturnsTheStoredTimestamps (QBrainAi.Support.Mcp.Tests); RequirementsLegacyIdAndGenerateDocumentTests mock gate StoredTimestampAssertion_AcceptsStoredValuesAndRejectsReadTime and binding GetItems_ReportTheStoredTimestamps (QBrainAi.Repl.Core.Tests) pass.
- TEST-MCP-REQAC-001: Creating FR/TR/TEST with acceptanceCriteria and reading them back returns an identical AcceptanceCriterion list (id/text/isSatisfied/evidence), workspace-scoped.
  Scope: layer-1+
- TEST-MCP-REQAC-002: Null or empty acceptanceCriteria round-trips as an empty list with no null leakage.
  Scope: layer-1+
- TEST-MCP-REQAC-003: The requirements document renderer emits a deterministic Acceptance Criteria block and the parser tolerates it without throwing.
  Scope: layer-1+
- TEST-MCP-REQAC-004: copy-from-todo copies a TODO's acceptance criteria onto a requirement verbatim.
  Scope: layer-1+
- TEST-MCP-REQACPLUGIN-001: Validate the Bash plugin family emits and hydrates acceptanceCriteria on FR/TR/TEST requirement create/update commands and exposes copyAcceptanceCriteriaFromTodo.
  Scope: layer-1+
- TEST-MCP-REQACPLUGIN-002: Plugin regression tests prove caller-supplied acceptanceCriteria is not silently lost when requirement create/update responses explicitly report an empty criteria list.
  Scope: layer-1+
- TEST-MCP-REQAC-PLUGIN-BASH: Bash plugin repl-invoke shim tests cover acceptanceCriteria create/update blocks and copyAcceptanceCriteriaFromTodo dispatch, with focused Bats gates passing for all five Bash plugins.
  Scope: layer-1+
- TEST-MCP-REQACPLUGIN-BASH: In each bash plugin, tests/repl-invoke-shim.bats (or tests/plugin-helpers.bats) proves typed-params emits acceptanceCriteria on create and hydrates it from existing on partial update with zero failures and zero skips.
  Scope: layer-1+
- TEST-MCP-REQACPLUGIN-CAPTURE: Plugin regression tests prove caller-supplied acceptanceCriteria is not silently lost when requirement create/update responses explicitly report an empty criteria list.
  Scope: layer-1+
- TEST-MCP-REQACPLUGIN-LIVE: Per plugin family, a live invocation of workflow.requirements.createFr with acceptanceCriteria populated round-trips through the deployed server (commit 6d376ea+) and the resulting REST GET returns the structured criteria.
  Scope: layer-1+
- TEST-MCP-REQAC-PLUGIN-TS: TypeScript plugin tests cover requirements acceptanceCriteria schemas, typed parameter shaping, and live-equivalent request handling for Cline, Cline v2, and Opencode.
  Scope: layer-1+
- TEST-MCP-REQACPLUGIN-TS: In each TS plugin, tests/requirements.test.ts (or tests/complex-tools.test.ts) proves req_create_fr/req_update_fr/req_create_test forward acceptanceCriteria into the request payload with zero failures and zero skips.
  Scope: layer-1+
- TEST-MCP-REQEXPORT-002: Validates TR-MCP-REQEXPORT-002. tests/McpServer.Support.Mcp.Tests/Controllers/RequirementsControllerGenerateTests.cs: GenerateAsync_WikiUnlistedException_ReturnsStructuredErrorNamingExceptionType substitutes IRequirementsDocumentService.GenerateWikiAsync to throw KeyNotFoundException and asserts the action returns an ObjectResult status 500 whose body contains the message and KeyNotFoundException (exceptionType). Red before the catch-all (the exception escaped/threw), green after adding catch(Exception)->BuildGenerateExportError to both wiki try blocks. Existing GenerateAsync_WikiConfigFailure/WikiConflictFailure/WikiZipAssemblyFailure tests remain green (AC3).
  Scope: layer-1+
- TEST-MCP-REQEXPORT-003: Verifies generateDocument accepts format=markdown for docType=matrix (and other non-wiki docTypes) without a format rejection at the schema, validator, and workflow layers. Validates TR-MCP-REQEXPORT-003 / BUG-TRIAGE-074.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] TR-MCP-REQEXPORT-003 AC-001, in RequirementsLegacyIdAndGenerateDocumentTests (QBrainAi.Repl.Core.Tests): the mock gate GenerateDocument_YamlTrIsValidAndUnknownFormatIsInvalid, then GenerateDocument_EveryDocumentedFormatAndDocType_IsValid over the 27 documented pairs and GenerateDocument_UnknownFormatOrDocType_NamesThePathAndEveryValue (PLAN-SESSIONTRIAGEFIX-001 B3).
- TEST-MCP-REQRECOVERY-001: Provider-backed and production-entrypoint tests must prove deterministic dry-run, atomic mixed apply, rollback, stale-precondition rejection, idempotent replay, concurrency safety, exact audit readback, and synchronized supported-plugin behavior.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] A mixed create, update, delete, acceptance-criteria, mapping-upsert, and mapping-delete plan commits once and exact post-query state and counts match the approved dry-run.
  - [ ] Injected validation and persistence failures at each operation class leave the effective requirements, criteria, mappings, idempotency state, and audit state byte-equivalent to the pre-state.
  - [ ] Stale pre-state hash, duplicate operations, missing references, and idempotency-key content mismatch are rejected before mutation with stable classified errors.
  - [ ] Dry-run is mutation-free, identical replay returns the original receipt, and repeated post-state queries contain no duplicates or drift.
  - [ ] Two concurrent plans using the same pre-state prove that at most one commits and the loser receives a stale-precondition outcome without partial state.
  - [ ] All supported production plugin entrypoints that advertise requirements operations execute the canonical dry-run and apply fixtures with identical normalized results, zero failures, and zero skips.
  - [ ] A zero-skip matrix executes mixed commit, validation rollback, persistence rollback, stale precondition, idempotent replay, and concurrent-plan cases against SQLite, SQL Server, and PostgreSQL. Every provider row proves exact raw and effective post-state plus audit and idempotency state.
  - [ ] Layered fixtures containing inherited, shadowed, and out-of-bounds records prove that dry-run binds every scope and layer field, apply writes only the target layer, and any change to bound raw state, effective state, layer metadata, or scope bounds produces a no-mutation stale-precondition result.
- TEST-MCP-REQWS-001: Explicit workspacePath override for requirements document generation (follow-up to triage-report-f77331f9a33e4bd0ae4f55f0470743ed). RequirementsClientTests verify GenerateAsync with a workspacePath override replaces the client-bound X-Workspace-Path header for that call only and the bound header is preserved without an override. RequirementsWorkflowWorkspaceOverrideTests verify the real RequirementsWorkflow forwards the override to the generate request, preserves the bound workspace when absent, and the ReplCommandDispatcher forwards the workspacePath param from workflow.requirements.generateDocument envelopes to the workflow. Cross-workspace override without the target workspace's API key fails with 401 (per-workspace keys) instead of silently exporting the session-bound workspace's requirements. Evidence 2026-07-14: red before implementation, Client 23/23 and Repl.Core 810/810 green after; deployed in service and mcpserver-repl 1.4.15+.
  Scope: layer-1+
- TEST-MCP-RUNAS-001: Test AC1-AC7 using TDD before implementation. Unit tests first: helper-only selection/excluded callers, precedence, override/key/env rejection, pinned-path/models/finite positive <=120s timeout, local/Windows regression, identity mismatch/fail closed, framing/multibyte limits, SO_PEERCRED and exact workspace policy, 8 backlog/8 admitted/1 active/no queue, absolute deadlines, 4096 UUID/10m epoch replay and stalled-reader bounded cleanup, explicit per-transient-unit fixed environment. Real UID/systemd/socket integration: sharpninja Grok answer/auth/hooks, prompt 0700/0600 ownership, normal-exit leftovers, double fork/reparent/inherited pipes, disconnect/cancel/timeout/output limit, worker crash/sweep, manager absence/cleanup failure and cgroup-empty deadline receipts. Nuke failure/restart injection at every journal transition, both binary hashes, first-install cleanup, config/data preservation, linger and unit state rollback, RecoveryFailed admission stopped. Full current/prior unit suites zero failures/skips, independent phase reviews and durable full receipts. Final defaults-only actual helper completed answer correlated to invocation sharpninja UID and MCP mcpserver NNP unchanged; never accept echo/progress/launch-only. All gates remain unexecuted/unsatisfied. AC1: Helper-only Grok router forwards configured sharpninja identity/socket with AgentHelp.RunAs precedence and TodoPromptOptions.RunAs fallback. Reject session identity overrides. Empty Linux RunAs/local Windows retain semantics; voice/TODO/triage/persistent QuadBrain and other callers never reach worker; unsupported strategy plus effective RunAs fails closed.
AC2: Real Grok UID resolves to actual sharpninja policy UID (1000 here), MCP UID remains mcpserver and NoNewPrivileges=true throughout. No HOME-only impersonation, secret copy, sudo expansion or privilege weakening. Reject identity mismatch/unavailable worker without server-UID fallback.
AC3: Authenticate SO_PEERCRED before parse on restricted socket; exact workspace/executable/model policy. Reject ModelApiKey, nonempty AgentParameters/environment maps, identity override and nonpinned path; diagnostics exclude values. Bound protocol/admission/replay/backpressure; apply fixed HOME/PATH/XDG_RUNTIME_DIR/DBUS environment to each transient unit, not inherited manager state; read-only real authentication/answer preflight.
AC4: Agent Helper invoked using deployed defaults only, no session RunAs/path/model/strategy overrides, reaches completed with expected FINAL ANSWER from real Grok. Correlate helper turn and worker epoch/invocation started/exit/UID/cleanup receipts; echo, progress-only, fallback or incomplete answers fail.
AC5: Per-invocation systemd user transient cgroup lifetime terminates leftover/double-fork/reparented descendants and removes owned private prompt files on normal exit, cancellation, disconnect, timeout, output limit, exception, shutdown and worker crash/restart. Empty-cgroup and file-removal proof required; cleanup failure makes worker unhealthy and blocks admission.
AC6: Exclusive-lock journaled Nuke/pkexec deployment stages and verifies worker before MCP, preserves unrelated config/data, backs up/restores both binary trees/manifests/units/drop-ins/policy/YAML and active/enabled/masked plus linger states. Inject failures/restarts at every transition; incomplete journal recovered before update. RecoveryFailed retains backups and stopped admission; rollback never claims helper goal fixed.
AC7: TDD red evidence before code; full current/prior applicable unit suites and required integration/deployment scopes have zero failures and zero skips. Independent phase reviews with durable full request/response JSONL and logged verdict/readback, final accuracy/completeness >=98 before any done state. Explicit operator architecture approval required before implementation; no approval or implementation is claimed by this capture.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] AC1: Helper-only Grok router forwards configured sharpninja identity/socket with AgentHelp.RunAs precedence and TodoPromptOptions.RunAs fallback. Reject session identity overrides. Empty Linux RunAs/local Windows retain semantics; voice/TODO/triage/persistent QuadBrain and other callers never reach worker; unsupported strategy plus effective RunAs fails closed. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
  - [ ] AC2: Real Grok UID resolves to actual sharpninja policy UID (1000 here), MCP UID remains mcpserver and NoNewPrivileges=true throughout. No HOME-only impersonation, secret copy, sudo expansion or privilege weakening. Reject identity mismatch/unavailable worker without server-UID fallback. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
  - [ ] AC3: Authenticate SO_PEERCRED before parse on restricted socket; exact workspace/executable/model policy. Reject ModelApiKey, nonempty AgentParameters/environment maps, identity override and nonpinned path; diagnostics exclude values. Bound protocol/admission/replay/backpressure; apply fixed HOME/PATH/XDG_RUNTIME_DIR/DBUS environment to each transient unit, not inherited manager state; read-only real authentication/answer preflight. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
  - [ ] AC4: Agent Helper invoked using deployed defaults only, no session RunAs/path/model/strategy overrides, reaches completed with expected FINAL ANSWER from real Grok. Correlate helper turn and worker epoch/invocation started/exit/UID/cleanup receipts; echo, progress-only, fallback or incomplete answers fail. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
  - [ ] AC5: Per-invocation systemd user transient cgroup lifetime terminates leftover/double-fork/reparented descendants and removes owned private prompt files on normal exit, cancellation, disconnect, timeout, output limit, exception, shutdown and worker crash/restart. Empty-cgroup and file-removal proof required; cleanup failure makes worker unhealthy and blocks admission. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
  - [ ] AC6: Exclusive-lock journaled Nuke/pkexec deployment stages and verifies worker before MCP, preserves unrelated config/data, backs up/restores both binary trees/manifests/units/drop-ins/policy/YAML and active/enabled/masked plus linger states. Inject failures/restarts at every transition; incomplete journal recovered before update. RecoveryFailed retains backups and stopped admission; rollback never claims helper goal fixed. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
  - [ ] AC7: TDD red evidence before code; full current/prior applicable unit suites and required integration/deployment scopes have zero failures and zero skips. Independent phase reviews with durable full request/response JSONL and logged verdict/readback, final accuracy/completeness >=98 before any done state. Explicit operator architecture approval required before implementation; no approval or implementation is claimed by this capture. (evidence: Unsatisfied: proposed architecture; implementation, explicit approval and execution evidence outstanding.)
- TEST-MCP-SERVICEUPDATE-001: Mocks-first ordering/failure/path tests, Windows defaults regression, preservation policy tests, real temporary tar metadata smoke, full unit gates, independent review and local Nuke service update verification per docs/plans/2026-09-28-cross-platform-update-service.md.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] UpdateService selects Windows or Linux at runtime; Windows defaults/launcher remain compatible; Linux x64/arm64 use extensionless apphost; unsupported hosts fail before changes.
  - [ ] Both platforms preserve live appsettings.yaml and configured DataFolder with legacy fallback. Linux also preserves unit/drop-ins/environment files and Unix ownership/modes/ACLs/xattrs through a private retained archive.
  - [ ] Linux validates installed service identity and stage before stop; backup precedes replacement; restore precedes start; service executable and server/workspace health must pass. Failures preserve recovery artifacts and never report success.
  - [ ] Mocks-first tests and full applicable unit suites pass with zero failures/skips; independent gates pass; existing local Linux service update preserves configuration/data and trusted workspace behavior.
- TEST-MCP-SESSIONATTR-001: Unit tests prove filesModified or commit paths outside the workspace root are rejected or stored only with a foreign marker. Validates TR-MCP-SESSIONATTR-001 / BUG-TRIAGE-108.
  Scope: layer-1+
- TEST-MCP-SESSIONEND-001: Pester proves SessionEnd with no MCP_WORKSPACE_PATH exits 0 and writes {}. Identifiable workspace still flushes. Validates TR-MCP-SESSIONEND-001 / BUG-TRIAGE-140.
  Scope: layer-1+
- TEST-MCP-SESSIONLIFE-001: SessionLogLifecycle.Tests.ps1 covers retained fields, hook status, 404 recovery, empty-query verbs, and same-request degraded beginTurn.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Mock-backed consumer tests prove degraded complete-turn preservation, truthful hook status, empty-query omission, bounded missing-turn dialog recovery, and same-request cached metadata.
  - [ ] At least one real builder or shim test runs with MCP_PLUGIN_PERSIST_LOG unset and asserts the complete cached object and serialized persistence payload rather than only fixture echo.
  - [ ] The accepted test scope exits with zero failures and zero skips and records the concrete test names and evidence paths in the acceptance manifest.
- TEST-MCP-SESSIONLIFE-002: SessionLogLifecycleMetadata, quarantine repair, audit reconcile, child deadline, and stop-hook pin tests.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named Pester tests cover metadata precedence, immutable identity, ordinary first persist, canceled/cancelled supersession, durable reopen, and all three contract documents.
  - [ ] Named process tests cover primary, queued, and lost wrapper exits, structured result serialization, single child deadline and cleanup, Stop ordering, quarantine repair, audit reconciliation, and dialog classification.
  - [ ] Every mock-backed consumer case has a corresponding real implementation assertion and recorded process exit or serialized receipt; unaccepted candidate tests remain explicitly not accepted.
  - [ ] The accepted cumulative test scope exits with zero failures, zero skips, no missing reports, and no zero-discovery projects.
- TEST-MCP-SESSIONLIFE-003: Reuse TransactionGatedSessionLogServiceTests bypass coverage and add query plus repeated additive complete proofs under FR-MCP-173.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Transaction-gated session-log tests prove non-QuadBrain action, dialog, update, and additive complete persistence without coordinator or keyserver invocation.
  - [ ] Exact durable readback verifies every child collection and repeated additive complete remains duplicate-free.
  - [ ] The subset proof is linked to FR-MCP-173 but does not mark its broader first-party adapter obligations satisfied.
- TEST-MCP-SESSIONLIFE-004: Schema guard, DbUpdateException details.inner, uniqueness retry, contention versus outage, dialog classification, and ImportRecoveryEnvelopeTests.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Named C# tests cover schema predecessor detection, credential-safe provider details.inner, the bounded same-turn unique race retry, contention versus outage including health.storage=unreachable only for actual storage outage, and dialog storage classification.
  - [ ] Import recovery tests start from canonical and reconstructed bundles and cover missing source, traversal, reparse escape, malformed schema, size bounds, cancellation, concurrent replay, content-hash drift, and delete-only-after-verified-persistence.
  - [ ] Provider-specific uniqueness and schema tests use disposable databases and leave no remnant; an approved prior migration is not reopened solely because infrastructure is degraded.
  - [ ] The accepted cumulative test scope exits with zero failures, zero skips, no missing reports, and no zero-discovery projects.
  - [ ] Provider-backed tests seed a dense session graph, update it through SubmitAsync, and prove bounded completion, exact child-collection readback, and no Cartesian query shape. Concurrent same-session submissions prove bounded retry or classified retryable failure with no partial or duplicate rows.
  - [ ] A zero-skip matrix executes dense SubmitAsync and concurrent same-session updates against SQLite, SQL Server, and PostgreSQL. The SQL Server row covers error 1205 deadlock and five-second command-budget expiry, proves one normalized retryable outcome without false primary success or partial state, then proves exact readback after successful retry.
- TEST-MCP-SESSIONLIFE-005: Existing PluginPowerShellRuntime drain-timeout coverage and session_dialog method dispatch. Not expected red. Governed by FR-MCP-REPL-011.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Existing PluginPowerShellRuntime coverage proves drain SubmitAsync uses a 120-second default and honors a larger REPL_TIMEOUT instead of the ordinary two-second nested helper budget, automatic nested drain defers while a REPL call is active, and timeout leaves attempt and latch state unchanged.
  - [ ] Existing dispatch coverage proves retained dialog recovery records use method client.SessionLog.AppendDialogAsync and preserve exact workspace, agent, session, and request identity.
  - [ ] This proof-first scope stays green and does not authorize rewriting already-correct timeout behavior without a newly accepted failing real-code test.
- TEST-MCP-SESSIONLOG-001: Validates TR-MCP-SESSIONLOG-001. tests/McpServer.Support.Mcp.Tests/McpStdio/SessionLogLifecycleToolErrorTests.cs: SessionLogCompleteTurn_MalformedTurnJson_ReturnsStructuredError and SessionLogFailTurn_MalformedTurnJson_ReturnsStructuredError assert a malformed turnJson yields a JSON {error} (with message, no success) instead of a thrown JsonException; SessionLogCompleteTurn_NullTurnJson_ReturnsSuccess asserts the happy path still returns {success:true}. Red before the fix (2 of 3 threw), green after moving the deserialize into a try/catch and ApplyWorkspaceOverride inside the service try.
  Scope: layer-1+
- TEST-MCP-SESSIONLOG-002: Validates TR-MCP-SESSIONLOG-002. tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTests.cs: QueryAsync_TextMatchesProcessingDialogContent seeds a session whose unique token exists only in a ProcessingDialog item Content and asserts the text query returns it; QueryAsync_TextMatchesActionDescription does the same for an action Description. Red before widening BuildSearchText (both returned 0), green after. Existing QueryAsync scalar/boolean search tests (WhenQueryingByBooleanTextThenTermsCanMatchAcrossTurnFields et al.) remain green as the AC3 regression guard.
  Scope: layer-1+
- TEST-MCP-SESSIONLOG-003: Validates TR-MCP-SESSIONLOG-003. tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTests.cs: UpsertTurnAsync_CompletedEmptyTurn_AcceptedForClaudeCode_RejectedForQBAgent seeds a ClaudeCode session and a QBAgent session, then asserts a completed turn with zero decisions/actions/commits returns a turnId for ClaudeCode and throws ArgumentException for QBAgent. Regression guard locking the QBAgent-only scope so a future broadening of the gate to standard agents fails. Tool-description accuracy (AC2) applied in FwhMcpTools.SessionLog.cs.
  Scope: layer-1+
- TEST-MCP-SESSIONLOG-005: Validates TR-MCP-SESSIONLOG-005. tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTests.cs: SetSessionTitleAsync unconditionally changes an existing session Title; SetTurnTitleAsync unconditionally changes an existing turn QueryTitle; both throw InvalidOperationException when the session or turn does not exist. Red before the methods exist, green after. Scope: layer-1+.
  Scope: layer-1+
- TEST-MCP-SESSIONLOG-006: Validates AC-FR-MCP-SESSIONLOGCTX-001-001 through 007 and AC-TR-MCP-SESSIONLOG-006-001 through 008. Tests: SessionLogTurnContextValidatorTests, SessionLogServiceTurnContextTests, SessionLogTurnContextExtractorTests, SessionLogTurnContextBackfillTests, SessionLogTurnPlanFileTodoIdModelTests, AddSessionLogTurnPlanFileAndTodoIdMigrationTests, SessionLogControllerTests.BeginTurn_MissingFields_Returns400, BeginTurn_NoneNone_Returns201_AndGetReturnsNone, Query_FilterByTodoId_ReturnsOnlyMatches, SessionLogBeginTurn_MissingPlanFile_ReturnsStructuredError, SessionLogBeginTurn_NoneNone_ReturnsSuccess, BeginTurnAsync_SerializesPlanFileAndTodoId, BeginTurnAsync_PersistsPlanFileAndTodoId, BeginTurnAsync_ForwardsPlanFileAndTodoId, SanitizeTurn_CopiesPlanFileAndTodoId, SanitizeTurn_DoesNotMutateSource, SanitizeTurn_LeavesNoneUnchanged, Import_OmittedFields_PersistsExtractorResultOrNone, Ingest_OmittedFields_PersistsExtractorResultOrNone, Apply_OmittedFields_PersistsProperValue, SqliteMigration_UpAddsColumns_DownDropsThem, Invoke-WorkflowBeginTurn_* plugin contract tests.
  Scope: layer-1+
- TEST-MCP-SESSIONLOG-FED-001: Unit tests (SessionLogFederationStateAdapter with a SQLite store and the local session-log service) SHALL prove TR-MCP-FED-SESSIONLOG-001: full-graph equality with the local read, apply round trip, sanitizer parity, local-only snapshot, and unchanged missing-session and version behavior, each with a negative control that a partial or unsanitized payload fails.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] TR-MCP-FED-SESSIONLOG-001 AC-001: full-graph equality; negative control: the pre-change 8-field turn payload fails the comparison.
  - [ ] TR-MCP-FED-SESSIONLOG-001 AC-002: apply round trip to an empty store.
  - [ ] TR-MCP-FED-SESSIONLOG-001 AC-003: sanitizer parity; negative control: an unsanitized payload fails.
  - [ ] TR-MCP-FED-SESSIONLOG-001 AC-004: no federation data call while snapshotting (a federation data client double records zero calls).
  - [ ] TR-MCP-FED-SESSIONLOG-001 AC-005: missing and soft-deleted sessions, and explicit version unchanged.
- TEST-MCP-SESSIONLOGSAN-001: Tests must prove default and configured redaction across the complete session-log DTO graph and all supported read transports without changing persisted raw data or query semantics.
  Scope: layer-1+
- TEST-MCP-SESSIONLOGSAN-002: Validates TR-MCP-SESSIONLOGSAN-002. tests/McpServer.Support.Mcp.Tests/Services/SessionLogSanitizerTimeoutTests.cs: CreateTimeoutSanitizer now constructs SessionLogSanitizer with an injected RegexReplaceInvoker that raises RegexMatchTimeoutException deterministically for the catastrophic rule on large input (length>10000 + pattern match, no wall-clock). SanitizeString_WhenConfiguredRuleTimesOut_ReturnsTimeoutTokenAndDoesNotLogInput and SanitizeSessionLog_WhenOneFieldTimesOut_ContinuesSanitizingOtherFields verified 10/10 green across repeated isolated runs (previously flaked pass/pass/fail). Production path uses the default Regex.Replace invoker.
  Scope: layer-1+
- TEST-MCP-STRICTCOUNT-001: Pester proves workflow.sessionlog.updateTurn succeeds for omitted, empty, and single scalar tags/contextList under StrictMode with exit 0. Validates TR-MCP-STRICTCOUNT-001 / BUG-TRIAGE-158.
  Scope: layer-1+
- TEST-MCP-SUBLOG-001: Parseable sink posts a correctly shaped batch to /api/v1/ingest with X-P-Stream and basic auth; the subscriber invokes the message log once per received message with correct status/reason; sink errors do not fail the commit; no-op default logs nothing.
  Scope: layer-1+
- TEST-MCP-TEMPVOL-001: Pester proves the TEMP alignment helper sets TEMP and TMP to the workspace volume when they differ, and does not call PSGallery internals. Validates TR-MCP-TEMPVOL-001 / BUG-TRIAGE-117.
  Scope: layer-1+
- TEST-MCP-TESTRESOURCE-001: Tests prove packaged fixture inputs and isolated generated artifacts work without repository-root lookup, while source-contract tests retain live-source validation. Run independent mocks with rejection controls before implementation, bind the same assertions to real helpers, and run the applicable full suites.

PR87 coverage for approved FR-MCP-TESTRESOURCE-001 ac-4 through ac-12: independent contract tests and rejection controls cover owned configuration/database selection and consumer start plans; per-operation SQL first/later failure reporting and cancellation; pg_ctl arguments; LocalDB and process-environment collection membership; blank shared SQL connection fallback; CLI temp-variable ownership; junction buffers; and health nonce refusal. Bind the same assertions to the real helpers; the required host lanes supply physical consumer/provider evidence. Named fixtures include OwnedInstanceIsolationContractTests, SqlServerMigrationFailureContractTests and SqlServerMigrationFailureBindingTests. Mock and binding receipts remain distinct from runtime-lane evidence.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Focused fixture tests run from test output with the checkout root unavailable and still load exact packaged bytes.
  - [ ] Writer tests use an injected disposable output directory and prove no generated file appears under repository docs/reviews or fixture source paths.
  - [ ] A source inventory finds no remaining fixture-input or generated-output path that walks to the repository root in applicable test code.
  - [ ] Focused build/source-contract tests still validate current repository and sibling plugin source, including checksum drift when present.
- TEST-MCP-TODO-CLOSE-001: Unit tests cover REST and typed client close-by-id behavior, including timestamp creation and missing item failure.
  Scope: layer-1+
- TEST-MCP-TRACE-LEGACY-001: Traceability audit coverage for completed legacy MCP baseline rows FR-MCP-001 through FR-MCP-025. Completed rows use this explicit audit TEST ID instead of stale planned placeholders when exact older TEST IDs are not documented; the requirements matrix remains the evidence source.
  Scope: layer-1+
- TEST-MCP-TRACE-LEGACY-002: Traceability audit coverage for completed auth, agent, CQRS, workspace, prompt, voice, desktop, and template rows FR-MCP-026 through FR-MCP-050. Completed rows use this explicit audit TEST ID instead of stale planned placeholders when exact older TEST IDs are not documented.
  Scope: layer-1+
- TEST-MCP-TRACE-LEGACY-003: Traceability audit coverage for completed agent-pool, change-event, GitHub, GraphRAG, and Byrd process rows FR-MCP-052 through FR-MCP-083. Completed rows use this explicit audit TEST ID instead of stale planned placeholders when exact older TEST IDs are not documented.
  Scope: layer-1+
- TEST-MCP-TRACE-REPL-001: Traceability audit coverage for completed REPL rows FR-MCP-REPL-001 through FR-MCP-REPL-005. These rows are covered by the existing REPL workflow, command-shape, YAML-envelope, and client-delegation test families documented under TEST-MCP-REPL-001 through TEST-MCP-REPL-020.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-001: Unit tests cover source detection and parser fixture coverage for Claude, Codex, Grok, Cline, Copilot, and OpenCode formats.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-002: Unit tests verify neutral-to-session-log mapping preserves native values, keeps absent semantics absent, and marks deterministic derived IDs.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-003: Unit tests cover malformed lines, unknown events, incomplete turns, mixed schemas, cancellation, limits, traversal, and path escape rejection.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-004: Unit tests verify canonical YAML serialization is deterministic, redacted, LF-normalized, UTF-8 without BOM, and round-trips to equivalent objects.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-005: Unit tests verify idempotent replay keys, richer-field preservation, pre-submit failsafe creation, and retention/deletion based on precise persistence receipts.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-006: Unit and contract tests verify transcript conversion is implemented only in the shared non-plugin transcript core and plugin packages contain no transcript ingestion helpers, skills, endpoint shortcuts, or parser forks.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-007: Integration tests cover /mcpserver/sessionlog/ingest/path and /upload for defaults, folder discovery, multipart, ZIP limits, and security rejections.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-008: Tests verify typed client, REPL commands, HTTP MCP discovery, stdio MCP discovery, and tool invocation for transcript ingestion and normalization.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-009: Tests cover Cline paired JSON/JSONL, Copilot events folders, OpenCode JSONL, and read-only OpenCode SQLite snapshot normalization.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-010: End-to-end plugin tests verify Claude, Codex, and Grok plugin packages do not expose transcript ingestion while model-run logging continues through normal workflow.sessionlog tools and non-plugin transcript ingestion remains externally verifiable.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-011: Codex transcript adapter coverage for real rollout record classes (validates TR-MCP-TRANSCRIPT-002 and TR-MCP-TRANSCRIPT-003). Unit tests in tests/McpServer.Support.Mcp.Tests/Ingestion/CodexTranscriptAdapterCoverageTests.cs verify: function_call and custom_tool_call records normalize to assistant tool-call events with call_id/name/status metadata; function_call_output and custom_tool_call_output records normalize to tool-role events preserving output text and call pairing; reasoning records with recoverable summary text normalize to assistant reasoning events while encrypted-only reasoning is skipped and reported through one aggregate info diagnostic (codex_encrypted_reasoning); event_msg records are skipped as UI mirrors of response_item records with one aggregate info diagnostic (codex_event_msg_skipped) and no warnings; turn_context records contribute session model and workspace path without diagnostics; world_state and compacted records are skipped with one aggregate info diagnostic (codex_nonconversation_skipped); unknown top-level record types and unknown response_item payload types warn once per distinct type with occurrence counts (codex_unknown_record, codex_unknown_response_item). Evidence 2026-07-14: 9/9 tests red against prior adapter, green after fix; real 2599-line rollout normalizes to 1174 events with 0 warnings (previously 166 events with 2432 warnings).
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-012: Imported-session lifecycle semantics (validates TR-MCP-TRANSCRIPT-004). Unit tests in tests/McpServer.Support.Mcp.Tests/Services/SessionLogImportedSessionDeleteTests.cs and SessionLogResubmissionReviveTests.cs verify: turn-level keyed operations (DeleteTurnAsync, ReplaceTurnSectionAsync, DeleteTurnItemAsync) accept provider-native identifiers persisted by transcript imports (UUID session ids, tool-call request ids) so turns can be repaired by resubmission; DeleteSessionAsync remains canonical-only by policy, rejecting imported session ids (sessions are soft-delete only and never deletable for imports); SubmitAsync revives a soft-deleted session that still holds the unique (WorkspaceId, SourceType, SessionId) key by restoring its row graph (session, turns, child rows) with only the SoftDelete named query filter bypassed (Workspace tenancy filter stays active) and then applying the resubmitted turn data; whitespace identifiers stay rejected. Evidence 2026-07-14: revive tests red with InvalidOperationException (disappeared after UNIQUE constraint failure) before fix, 7/7 green after; full suite green (build.ps1 Test exit 0); live recovery of session 019f2580-48c8-7912-b6a9-27f61b18d0d3 in F:\GitHub\MouseKeyProxy from tombstone to 1174 corrected turns via re-ingest plus 28 turn-level deletes of stale duplicates.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-013: Validates FR-MCP-TRANSCRIPT-009 and TR-MCP-TRANSCRIPT-010. Unit: a JSONL transcript line larger than the former 8 MiB ceiling is ingested without an InvalidDataException, replacing the assertion in IngestionService_RejectsOversizedJsonlLine which is retargeted to prove the int.MaxValue ceiling is still an enforced bound rather than an absent one. Unit: the streaming reader returns identical records to the previous ReadAllLinesAsync implementation for an existing multi-record fixture, proving no regression in parse behavior. Unit: peak allocation while reading a many-small-line transcript stays proportional to the largest line rather than to file size. Integration: retained hostile-archive guards still reject their fixtures at the unchanged values, specifically archive entry count above 10,000, compression ratio above 20, ZIP symlink entries, and path-traversal entries, each returning 413 or 400 as previously mapped.
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-SEARCH-001: CodexTranscriptAdapterCoverageTests ingest inline JSONL for inter_agent_communication_metadata, tool_search_call, and tool_search_output with zero unknown diagnostics and persist cleanup. Validates TR-MCP-TRANSCRIPT-SEARCH-001 / BUG-TRIAGE-122.
  Scope: layer-1+
- TEST-MCP-TRIAGE-001: Intake accepts valid reports and rejects invalid reports across REST, client, and REPL.
  Scope: layer-1+
- TEST-MCP-TRIAGE-002: Deterministic grouping, McpServer workspace routing for core and plugin bugs, and 15-minute quiet-window behavior are verified.
  Scope: layer-1+
- TEST-MCP-TRIAGE-003: Research worker invokes configured direct agent with group JSON and prompt.
  Scope: layer-1+
- TEST-MCP-TRIAGE-004: Schema-valid research output creates exactly one BUG-TRIAGE-### TODO.
  Scope: layer-1+
- TEST-MCP-TRIAGE-005: Invalid agent output or failed agent run creates no TODO and leaves inspectable failure state.
  Scope: layer-1+
- TEST-MCP-TRIAGE-006: Multi-workspace isolation prevents cross-workspace grouping and status leakage.
  Scope: layer-1+
- TEST-MCP-TRIAGEERR-001: Unit and controller tests prove validation, not-found, persistence with inner, and backend_unavailable each emit code, message, retryable, and details on MCP tool JSON, REST ProblemDetails extensions, and REPL type error payload.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] TR-MCP-TRIAGEERR-001 AC-004: the mock gate ExecutionStrategyWrapperAssertion_AcceptsWrappedErrorClassificationAndRejectsDeviations (McpErrorClassifierTests; hand-built exception chains and scripted classifications, no database; it rejects the wrapped deadlock classified internal_server_error or without details.inner, the wrapped deadlock whose message is not in English classified internal_server_error, a wrapped unique violation classified retryable, and a plain InvalidOperationException classified retryable), then Classify_ExecutionStrategyWrapper_ClassifiesTheWrappedDatabaseError, the same cases bound to McpErrorClassifier.Classify, pass; SessionLogConcurrentTurnWriteIntegrationTests.SqlServer_ConcurrentDialogAppends_StoreDistinctOrdinals passes on a temporary SQL Server instance, every deadlock refusal retried (PLAN-SESSIONTRIAGEFIX-001 A7 follow-up).
- TEST-MCP-TRIAGEHELP-001: Progress-only grok-cli body is incomplete and names FINAL ANSWER. CLI failure with fallback flag on is not status completed. submitTurn timeout is at least HelperTimeout.
  Scope: layer-1+
- TEST-MCP-TRIAGEPLUGIN-001: Pester proves background openSession does not rebind root, cache replace resolves or named drift, profile cwd uses hook workspace path, beginTurn timeout is degraded queued, and completeTurn after sessionId rebind clears failsafe.
  Scope: layer-1+
- TEST-MCP-TRIAGEPLUGIN-002: ReplacePluginCache retains the current cache or rebinds to a replacement. If no replacement exists it emits a named version-drift error.
  Scope: layer-1+
- TEST-MCP-TRIAGEPLUGIN-003: Resolve-McpCacheDir with the hook workspace path succeeds when cwd is the user profile and env is empty.
  Scope: layer-1+
- TEST-MCP-TRIAGEPLUGIN-004: beginTurn persist timeout after failsafe returns degraded/queued and retains failsafe.
  Scope: layer-1+
- TEST-MCP-TRIAGEPLUGIN-005: completeTurn persist identity prefers current-turn sessionId after sessionId rebind.
  Scope: layer-1+
- TEST-MCP-TRIAGEREQ-001: Seed TR-066, listTr returns it, getTr updateTr deleteTr succeed, createTr of TR-066 is rejected.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] FR-MCP-TRIAGEREQ-001 ac-2 and TR-MCP-TRIAGEREQ-001 AC-001, in RequirementsLegacyIdAndGenerateDocumentTests (QBrainAi.Repl.Core.Tests): the mock gate GetFrAsync_CanonicalId_ReachesTheClientRoute and CreateFrAsync_LegacyId_IsRefusedBeforeAnyRequest with the assertion proof RouteAssertion_RejectsNoRequestAndAnotherRoute, then FrGetUpdateDelete_LegacyId_ReachTheFrRoute, TestGetUpdateDelete_LegacyId_ReachTheTestRoute, UpdateBatches_LegacyIds_ReachTheBatchRoutes and CreatesWithLegacyIdsAndEmptyIdLookups_AreRefusedBeforeAnyRequest (PLAN-SESSIONTRIAGEFIX-001 B7).
- TEST-MCP-TRIAGE-REQAC-001: Every new FR/TR/TEST acceptance criterion is referenced by at least one test and passes ValidateTraceability.
  Scope: layer-1+
- TEST-MCP-TRIAGESCHEMA-001: A fixture database missing the four agent header columns fails closed with pending-migration. Apply proofs: Sqlite MigrateAsync of 20260818205751_AddSessionLogTagsAndAgentSessionHeaders on a legacy SessionLogs table; SqlServer 20260818205807 captured Up() SQL on disposable LocalDB; Postgres 20260818205822 captured Up() SQL on disposable local PostgreSQL. After apply, sessionlog query with and without a text filter succeeds.
  Scope: layer-1+
- TEST-MCP-TRIAGESTORE-001: SessionLogService tests cover identical actions no duplicate, session tags round-trip, replace missing requestId not found, canceled status round-trip, and superseded persist with None sentinels without 500.
  Scope: layer-1+
- TEST-MCP-TRIAGESTORE-002: TriageService tests prove unreachable SQL fails within about 5 seconds with storage-unavailable, health liveness unchanged in contract, and no partial rows.
  Scope: layer-1+
- TEST-MCP-TRIAGESTORE-003: ReplaceTurn of a missing requestId throws KeyNotFoundException / classified 404. It does not upsert a new turn and does not emit an untyped EF save.
  Scope: layer-1+
- TEST-MCP-TRIAGESTORE-004: Turn status canceled and cancelled persist via submit and re-query with the same status.
  Scope: layer-1+
- TEST-MCP-TRIAGESTORE-005: SQLITE_BUSY and deadlock persistence failures classify as persistence_error retryable true with details.inner.
  Scope: layer-1+
- TEST-MCP-TRIAGESTORE-006: Superseded hook persist with omitted planFile/todoId writes None sentinels and status canceled.
  Scope: layer-1+
- TEST-MCP-TRIAGESTORE-007: Session-log SaveChanges and triage intake fail within about 5 seconds as backend_unavailable when storage is unreachable.
  Scope: layer-1+
- TEST-MCP-TRIAGETODO-001: TodoExecutionService SetTestPlanAsync succeeds when durable EXEC exists and execution-state row is missing. EfTodoService CreateAsync soft-deleted id revives or skips. Failed batch is retry-clean. Invalid dependsOn fails before insert.
  Scope: layer-1+
- TEST-MCP-TRIAGETODO-002: GenerateNextTodoId skips same-workspace soft-deleted EXEC ids. CreateAsync of a soft-deleted id revives or skips instead of opaque UNIQUE. Invalid dependsOn fails before insert.
  Scope: layer-1+
- TEST-MCP-TRIM-001: ProcessRunnerDependencyTests verifies all five public runner types belong to QBrainAi.Common.AgentCli; QBAgent runtime dependency graph excludes QBrainAi.Services, QBrainAi.Storage and Entity Framework; Services type forwards preserve old qualified names. Existing mocked git/bash and full unit suites remain passing. Trim analysis records only the actual remaining client-side warnings, with no added suppression.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Runner implementation, options, interface, request and result reside in Common.AgentCli with unchanged namespaces and signatures.
  - [ ] QBAgent dependency graph excludes server Services, Storage and EF assemblies.
  - [ ] Services forwards all moved public types, and existing mock-backed process/tool unit tests remain green.
  - [ ] Trim inventory is reduced from verified diagnostics only, with no new suppression or relaxed assertions.
- TEST-MCP-USECASE-001: Unit tests cover schema creation, workspace isolation, soft-delete hide, FR string FK link uniqueness, and handler CRUD/link behaviors with zero skips in the executed gate scope.
  Scope: layer-1+
- TEST-MCP-USECASE-002: Controller unit tests for UseCasesController. Acceptance: Controller tests green 0 skip.
  Scope: layer-1+
- TEST-MCP-USECASE-003: Diagram golden tests mermaid and plantuml. Acceptance: Mermaid sequenceDiagram; plantuml @startuml.
  Scope: layer-1+
- TEST-MCP-USECASE-004: Client unit tests with live JSON shapes. Acceptance: Client UseCase filter green including coverage DTO.
  Scope: layer-1+
- TEST-MCP-USECASE-005: Coverage query tests. Acceptance: Coverage gap tests green.
  Scope: layer-1+
- TEST-MCP-USECASE-006: Migration apply empty and production-shaped. Acceptance: UseCaseMigrationApplyTests green 0 skip.
  Scope: layer-1+
- TEST-MCP-USECASE-007: Audit emission on UC mutations. Acceptance: Audit tests green for create/update/delete/link.
  Scope: layer-1+
- TEST-MCP-USECASE-008: UI asset REST-only tests. Acceptance: UseCaseUiAssetTests green.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] StaticWebRootMarkdownTests (TR-MCP-USECASE-007 AC-1): over a temporary web root, the real static file middleware with the host options serves index.html, usecases/app.js and memory/site.css and serves none of README.md, memory/README.md, usecases/README.md and notes.MD; the contract rejects the named faults DefaultOptionsServeMarkdown, UnknownFileTypesServed and UiAssetTypeDropped inside a passing test; Program.cs passes the policy to every UseStaticFiles call; the host project removes wwwroot README.md files from Content. No case depends on the machine or the content root.
- TEST-MCP-USECASE-009: Approval and product hooks tests. Acceptance: Expanded scope approval/product tests green.
  Scope: layer-1+
- TEST-MCP-USECASE-010: Plugin-core usecase jest tests. Acceptance: usecase.test.ts green.
  Scope: layer-1+
- TEST-MCP-USECASE-011: Deploy smoke after UpdateService. Acceptance: health, /usecases/, create/link/diagram/coverage smoke log green.
  Scope: layer-1+
- TEST-MCP-USECASE-012: UML graph serialization goldens Mermaid schema v1 + PlantUML. Covers AC-013-*, AC-014-*, AC-T14-*.
  Scope: layer-1+
- TEST-MCP-USECASE-013: Diagram graph CQRS/storage get put isolation soft-delete invalid audit. Covers AC-012-*, AC-T11-3, AC-T12-1, AC-T16-1.
  Scope: layer-1+
- TEST-MCP-USECASE-014: Controller/client diagram-graph and kind=usecase export. Covers AC-T13-*, HTTP AC-012-5.
  Scope: layer-1+
- TEST-MCP-USECASE-015: Canvas UI asset/contract tests palette canvas drag hooks REST-only. Covers AC-011-*, AC-T15-*.
  Scope: layer-1+
- TEST-MCP-USECASE-016: Migration apply for graph storage. Covers AC-T11-1, AC-T11-2.
  Scope: layer-1+
- TEST-MCP-USECASE-017: Adversarial Grok hostile validator + live canvas smoke claim pack.
  Scope: layer-1+
- TEST-MCP-USECASE-018: For each fourteenth-review behavior, a consumer test and a real-service, native-provider, or adversarial test fail before production changes and pass afterward. SQLite, SQL Server LocalDB, healthy external PostgreSQL, requirements/wiki, repository root/process lifetime, marker/client/REST-MCP/workspace preflight, release build, build.ps1 Compile/Test/ValidateTraceability, trailer, migration, and evidence gates execute with zero failures and zero skips; aborted or timed-out hosts remain inconclusive failures.
  Scope: layer-1+
- TEST-MCP-USECASE-019: For every fifteenth-review behavior (TR-MCP-USECASE-018), write the consumer test and the real-service, kernel, provider, or adversarial test with scripted data first and prove them against that data in a green mock gate with negative controls before production changes (BDPv4: no red tests); then bind the same cases to production and prove focused and cumulative GREEN with exact zero-failure, zero-skip, zero-abort, and zero-inconclusive accounting from C:-physical artifacts. Platform-specific cases are integration tests that run in their host's lane (TR-MCP-NUKE-019).
  Scope: layer-1+
- TEST-MCP-USECASE-020: For each sixteenth-review finding (TR-MCP-USECASE-019), write the consumer-facing test and the real operating-system, mount, provider, process, or evidence-boundary test with scripted data first and prove them against that data in a green mock gate with negative controls before production changes (BDPv4: no red tests); then bind the same cases to production and prove focused and cumulative GREEN from C:-physical artifacts with exact zero-failure, zero-skip, zero-abort, zero-inconclusive, and expected-minimum accounting. Platform-specific cases are integration tests that run in their host's lane (TR-MCP-NUKE-019).
  Scope: layer-1+
- TEST-MCP-USECASE-021: Prove identity-preserving actor, flow, and step editing through discriminating independent fakes before production, then bind the same assertions to CQRS, REST, client, MCP, persistence, and authenticated readback.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Before production edits, contract tests pass against correct independent fake editor responses and prove inside the passing harness that wrong IDs, parent IDs, fields, and mutation counts are rejected.
  - [ ] The same contract cases and assertions pass against the real CQRS handlers for actor, flow, and step updates, including validation, missing, cross-workspace, and parent-mismatch cases.
  - [ ] Controller tests prove the three update routes and classified 400/404 responses; typed-client tests prove HTTP methods, paths, payloads, cancellation, and response deserialization; MCP-tool tests prove dispatcher inputs and serialized results.
  - [ ] A persistence test proves IDs and row counts are unchanged, aggregate approval/version and unrelated data are unchanged, UpdatedAtUtc advances, and authenticated readback returns the edited values.
  - [ ] Focused tests, the full applicable unit suite, Compile, and ValidateTraceability complete with zero failures and zero skips before deployment.
- TEST-MCP-VERIFYPOLICY-001: Prove exact argv/selectors round-trip, deterministic challenge-independent digest, key rotation, tamper/wrong-workspace/replayed-challenge rejection, malformed fields and escaping paths rejection. Exercise server configuration selection, updated policy reads, unavailable/ambiguous config, anonymous-token refusal including explicit workspace routing, and actual typed HTTP client decoding. Verify REPL and Node plugin consumers propagate typed failures and never accept unsigned or cross-workspace data.
  Scope: layer-1+
- TEST-MCP-VERIFYWRAP-001: Pester proves code-verify maps disk-full IOException to a typed status and returns within the documented timeout after a childless hang path. Validates TR-MCP-VERIFYWRAP-001 / BUG-TRIAGE-125 / BUG-TRIAGE-130.
  Scope: layer-1+
- TEST-MCP-WIKIEXPORT-001: Tests must cover docs/wiki.yaml loading, validation, renderer output, service integration, unchanged default behavior, and BDPv4 traceability for configured GitHub and Azure wiki exports.
  Scope: layer-1+
- TEST-MCP-WIKIEXPORT-002: Tests must prove marker generation creates a valid default docs/wiki.yaml, preserves existing configs, and keeps marker generation behavior intact.
  Scope: layer-1+
- TEST-MCP-WIKIEXPORT-003: WikiExport_WithoutDumpFlag_UnchangedBehavior; WikiExport_WithDumpFlag_WritesVersionedJsonKeyedByWorkspace; Dump_ContainsTodoRowsAndRequirementLinksMatchingStore; Dump_Sha256_MatchesCanonicalUtf8Json; DumpPolicyRegistry_CoversEveryCurrentDbSetExactlyOnce; Dump_SecretSentinels_RedactedAcrossScalarNestedAndEncodedPayloads; Dump_Diagnostics_NeverEchoSecretValue; Dump_ExcludesUnrelatedWorkspaceAndGlobalSecrets; Dump_FinalByteHash_VerifiesAfterRedaction; Dump_RelationshipsRemainLosslessExceptDeclaredRedactedText; Import_SourceOwnedProduct_RemapOwnerToDestination; Import_SharedProductMissing_RequiresSharedProductMapping; Import_SharedProductEquivalent_ReusesAuthorizedDestinationProduct; Dump_ForeignOwnedProduct_ExportsOnlySourceWorkspaceMembership.
  Scope: layer-1+
- TEST-MCP-WIKIEXPORT-004: AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml; Import_OperationalColumnsContainOnlyDestinationIdsAndPaths; Import_MalformedDump_VersionMismatch_MissingTables_UnsafePath_Rejected; Import_IdempotentReimport_NoDuplicateTodos. Import_ImmutableSourceProvenancePreservesOriginalIdentity; Import_ReplayableFederationAgentTriageHandoffRows_AreInertAfterRestart; Import_CredentialedFeatures_RequireDestinationReconfiguration; Import_RelationshipsAndEntityMaps_AreComplete; AddWorkspace_DumpReplay_CheckedBeforeDuplicateRegistration; Import_RegistrationAndHydration_CommitInOneTransaction; Import_PreCommitFailureOrCancellation_RollsBackWorkspaceAndRows; Import_PostCommitProjectionInitOrStartFailure_IsRecoverable; Import_CancelAfterCommit_LeavesPendingActivation; Import_ExistingRepositoryCheckout_PreservesGitAndSourceFiles; Import_ActivatedReplay_DoesNotReapplyFreshFilesystemPreconditions; Import_ServiceOwnedPathConflict_PrecommitDiagnosticOrRecoverableActivationFailure; Import_UnknownExistingMarker_IsNeverReplaced; Import_ConcurrentSameDump_SingleCommitAndReplayReceipt; Import_ConflictingDump_RejectsWithoutPartialHydration.
  Scope: layer-1+
- TEST-MCP-WIKIEXPORT-005: TodoYaml_NotSourceOfTruth_WhenDumpPresent; TodoYaml_Cleanup_ArchivesWithEvidence_NoSilentDelete; DumpAndTodoYaml_Conflict_DumpWins_DiagnosticNamesBoth.
  Scope: layer-1+
- TEST-MCP-XAGENT-001: Pester proves CompleteTurn refuses a GrokCode/ClaudeCode sessionId on a Codex current-turn and still completes a same-agent sessionId rotation without Submit 500. Validates TR-MCP-XAGENT-001 / BUG-TRIAGE-106 / BUG-TRIAGE-142.
  Scope: layer-1+
- TEST-REQAC-LIVE-001: Live criteria round-trip works
  Scope: layer-1+
- TEST-SUPPORT-010: Traceability audit coverage for the original broad FR-SUPPORT-010 support surface row. This broad parent row predates the later FR-SUPPORT-010A through FR-SUPPORT-010F split and maps to this explicit audit TEST ID while split child rows retain their dedicated support test IDs.
  Scope: layer-1+
- TEST-SUPPORT-010A-1: Given a `SessionLogService` constructed with a non-null `WorkspaceContext`, when `SubmitAsync` persists a session, then `SessionLogEntity.WorkspaceId` and every child entity's `WorkspaceId` equal the context's `WorkspacePath`. **Covered by:** `SessionLogServiceTests.SubmitAsync_StampsWorkspaceIdOnSessionEntity`, `SessionLogServiceTests.SubmitAsync_StampsWorkspaceIdOnEveryChildEntity`
  Scope: layer-1+
- TEST-SUPPORT-010A-2: Given a `SessionLogService` constructed with `workspaceContext: null` and a DbContext without `_workspaceId`, when `SubmitAsync` persists a session, then `SessionLogEntity.WorkspaceId` remains empty string. **Covered by:** `SessionLogServiceTests.SubmitAsync_WithNullWorkspaceContext_KeepsWorkspaceIdEmpty`
  Scope: layer-1+
- TEST-SUPPORT-010B-1: Given a malformed POST body that fails JSON deserialization against `UnifiedSessionLogDto`, when the controller returns 400, then the response content-type is `application/problem+json` and the errors object contains `$.workspace` (or the offending field path), never the `dto` parameter name. **Covered by:** `SessionLogControllerTests.WhenPostingMalformedWorkspaceFieldThenReturnsProblemDetailsWithoutDtoKey`
  Scope: layer-1+
- TEST-SUPPORT-010B-2: Given a POST body missing `sourceType`, when domain validation rejects it, then the response is `application/problem+json` with `sourceType` cited (not the legacy `{"error":"..."}` plain shape). **Covered by:** `SessionLogControllerTests.WhenPostingMissingSourceTypeThenReturnsProblemDetails`
  Scope: layer-1+
- TEST-SUPPORT-010C-1: Given a successful POST to `/mcpserver/sessionlog`, when `GET /mcpserver/sessionlog/{agent}/{sessionId}` is called under the same workspace context, then the response is 200 OK with the round-tripped session. **Covered by:** `SessionLogControllerTests.WhenPostingThenGetBySessionIdReturnsRecord`
  Scope: layer-1+
- TEST-SUPPORT-010C-2: Given a session exists, when `POST /mcpserver/sessionlog/{agent}/{sessionId}/turn` carries a `UnifiedRequestEntryDto`, then 201 is returned and the subsequent GET shows the appended turn. **Covered by:** `SessionLogControllerTests.WhenPostingTurnViaRestThenTurnIsRetrievable`, `SessionLogServiceTests.UpsertTurnAsync_NewTurn_AppendsWithoutDeletingSiblings`
  Scope: layer-1+
- TEST-SUPPORT-010C-3: Given the turn-append route, when PUT is used instead of POST, then 405 is returned with `Allow: POST`. **Covered by:** `SessionLogControllerTests.WhenPuttingTurnRouteThenReturns405WithAllowHeader`
  Scope: layer-1+
- TEST-SUPPORT-010E: Integration tests: open idempotent, begin creates in_progress, complete merges+finalizes with evidence gate, fail records note, missing session 404.
  Scope: layer-1+
- TEST-SUPPORT-010F: SQLite tests: partial session submit preserves omitted title/model; sparse turn submit preserves omitted response/queryText and prior collections.
  Scope: layer-1+
- TEST-SUPPORT-014: Integration tests verify session log operations: open idempotent, begin creates in_progress, complete merges and finalizes with evidence gate, fail records note, missing session returns 404.
  Scope: layer-1+
- TEST-SUPPORT-015: SQLite tests verify that partial session submit preserves omitted title/model and sparse turn submit preserves omitted response/queryText and prior collections.
  Scope: layer-1+
- TEST-SUPPORT-016: When SessionLogService with a non-null WorkspaceContext calls SubmitAsync, SessionLogEntity.WorkspaceId and every child entity's WorkspaceId equal the context's WorkspacePath. This ensures proper workspace isolation for session logs.
  Scope: layer-1+
- TEST-SUPPORT-017: When SessionLogService is constructed with workspaceContext: null and a DbContext without _workspaceId, SubmitAsync persists a session with SessionLogEntity.WorkspaceId remaining empty string.
  Scope: layer-1+
- TEST-SUPPORT-018: When a malformed POST body fails JSON deserialization against UnifiedSessionLogDto, the controller returns 400 with application/problem+json content-type and errors object contains the field path (e.g., $.workspace), never the dto parameter name.
  Scope: layer-1+
- TEST-SUPPORT-019: When a POST body is missing sourceType, domain validation rejects it with application/problem+json response citing sourceType (not legacy {"error":"..."} plain shape).
  Scope: layer-1+
- TEST-SUPPORT-020: When a successful POST to /mcpserver/sessionlog completes, GET /mcpserver/sessionlog/{agent}/{sessionId} under the same workspace context returns 200 OK with the round-tripped session.
  Scope: layer-1+
- TEST-SUPPORT-021: When the turn-append route receives a POST to /mcpserver/sessionlog/{agent}/{sessionId}/turn with a UnifiedRequestEntryDto, 201 is returned and the subsequent GET shows the appended turn.
  Scope: layer-1+
- TEST-SUPPORT-022: When PUT is used on the turn-append route instead of POST, the endpoint returns 405 Method Not Allowed with 'Allow: POST' header.
  Scope: layer-1+
- TEST-SUPPORT-024: Validates FR-SUPPORT-016 ac-1 to ac-4 and ac-6 to ac-8 and TR-SUPPORT-CORE-016 ac-1: SessionLogServiceReplaceDeleteTests for the existing behavior, and the PLAN-SESSIONTRIAGEFIX-001 A7, A8 and A9 tests (SessionLogServiceTests, SessionLogClientRequestBodyTests, SessionLogControllerDeleteItemTests, and SessionLogDeleteItemRoutingTests on a temporary instance).
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] FR-SUPPORT-016 ac-1 to ac-4 and TR-SUPPORT-CORE-016 ac-1: SessionLogServiceReplaceDeleteTests ReplaceTurnAsync_ClearsOmittedScalarsAndCollections, ReplaceTurnAsync_PreservesWorkspaceStampOnChildren, ReplaceTurnSectionAsync_ReplacesOnlyNamedSection, ReplaceTurnSectionAsync_EmptyPayload_ClearsSection, ClearTurnSectionAsync_RemovesAllItemsInSection, UpsertTurnAsync_StaysAdditive_DoesNotRemoveExistingItems, DeleteTurnItemAsync_StringSection_RemovesByValue, DeleteTurnItemAsync_Commit_RemovesBySha, DeleteTurnItemAsync_Action_RemovesByOrder, DeleteTurnItemAsync_UnknownKey_ReturnsFalse and ReplaceTurnSectionAsync_UnknownSection_Throws pass.
  - [ ] FR-SUPPORT-016 ac-6 (PLAN-SESSIONTRIAGEFIX-001 A7): the ordinal mock gate and the three ordinal and delete cases named there pass.
  - [ ] FR-SUPPORT-016 ac-7 (PLAN-SESSIONTRIAGEFIX-001 A8): the client mock gate and request case, the two controller cases and the two routing cases on a temporary instance named there pass.
  - [ ] FR-SUPPORT-016 ac-8 (PLAN-SESSIONTRIAGEFIX-001 A9): the null-element mock gate and the null-list, null-submit and empty-list cases named there pass.
  - [ ] FR-SUPPORT-016 ac-6 for concurrent writes and TR-SUPPORT-CORE-016 ac-4: the mock gate ConcurrentTurnWriteOutcomeAssertion_AcceptsDistinctOrRetryableRefusalAndRejectsDuplicateOrPartialWrite (QBrainAi.Support.Mcp.Tests; it also rejects two first inserts stored as two turn rows), then TurnWrites_InterleavedOnOneTurn_StoreDistinctOrdinalsOrRefuseRetryably (a file SQLite database; the second writer runs between the first writer's read and save, for a PATCH, a dialog append and a section replace), TurnWrites_TwoFirstInsertsOfOneRequestId_LeaveOneTurnRow (two PATCHes that each create the same new turn, interleaved the same way: one turn row remains, holding both writes' items with distinct ordinals) and SessionLogConcurrentTurnWriteIntegrationTests (concurrent dialog appends to one turn on SQLite, PostgreSQL and SQL Server temporary instances, every refusal retryable and retried, every stored ordinal distinct) pass; for ac-4's retry, the mock gate UniqueRaceAssertion_AcceptsOneRetriedUpdateAndRejectsMissingRetryOrDuplicate (SessionLogServiceTests), then UpsertTurnAsync_UniqueSessionLogTurnsRace_RetriesOnceAndUpdates, which forces the unique violation inside the service's own transaction and commits the winning row after the rollback, passes: the retry runs once, in a fresh transaction, and updates that row, leaving one turn row (no caller reaches this path in production today, because UpsertTurnAsync serializes its read and write whenever no transaction is open and no caller holds one) (PLAN-SESSIONTRIAGEFIX-001 A7 follow-up; operator decision 2026-10-16 "Keep retry, revert ac-1 (Recommended)").
- TEST-TIMER-001: Run the same scenario and assertions against independently scripted ITimerService/ITimerHandle mocks and both real services. Correct mock and deliberate negative responses must yield a green outer mock gate before product edits. Cover blocked cancellation registration, overlapping Restart against Stop and Dispose, new run after Stop, final Dispose, repeated shutdown, and reentrant calls.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Mock contract gate passes with zero failures/skips and rejects forbidden restart, stale token, and restart-after-dispose behaviors as expected negative controls.
  - [ ] The same contract tests pass against Core and UI.Core, and all four reproduced race cases no longer require external rescue.
  - [ ] Full five-project Manager suites, extension tests, and deployment-script tests have zero failures/skips before acceptance; deferred Claude HV must review all changed code/tests/docs before Phase 2 closure.
- TEST-TRIAGE-001: Unit and contract tests cover triage dashboard bucketing data, group/report/result rendering inputs, empty/error states, workspace filtering, and typed client dispatch for Director and MCP Web consumers.
  Scope: layer-1+
- TEST-TRIAGE-002: Unit and client tests cover triage-created TODO listing, workspace filtering, missing TODO anchors, group/run context mapping, and typed client dispatch.
  Scope: layer-1+
- TEST-WEB-001: Unit and UI tests prove FR-WEB-001 cockpit composition and refresh from MCP APIs.
  Scope: layer-1+
- TEST-WEB-002: Tests prove FR-WEB-002 workspace-as-mission binding and isolation.
  Scope: layer-1+
- TEST-WEB-003: Tests prove FR-WEB-003 status mapping and start/stop/recycle/all against AgentPoolClient mocks.
  Scope: layer-1+
- TEST-WEB-004: Tests prove FR-WEB-004 lane derivation and TodoClient usage.
  Scope: layer-1+
- TEST-WEB-005: Tests prove FR-WEB-005 fire-and-forget parallel pickup.
  Scope: layer-1+
- TEST-WEB-006: Tests prove FR-WEB-006 schedule persist and fire while UI is closed.
  Scope: layer-1+
- TEST-WEB-007: Tests prove FR-WEB-007 workspace/agent/TODO merge order.
  Scope: layer-1+
- TEST-WEB-008: Tests prove FR-WEB-008 one-click retry payload.
  Scope: layer-1+
- TEST-WEB-009: Tests prove FR-WEB-009 session-log streaming.
  Scope: layer-1+
- TEST-WEB-010: Tests prove FR-WEB-010 charts and daily metrics.
  Scope: layer-1+
- TEST-WEB-011: Tests prove FR-WEB-011 non-blocking feedback.
  Scope: layer-1+
- TEST-WEB-012: Tests prove FR-WEB-012 idle proposals stay tentative.
  Scope: layer-1+
- TEST-WEB-013: Tests prove FR-WEB-013 allow/deny fail-closed.
  Scope: layer-1+
- TEST-WEB-014: Tests prove FR-WEB-014 per-agent provider/model.
  Scope: layer-1+
- TEST-WEB-015: Tests prove FR-WEB-015 SHA rendering from session-log commit actions.
  Scope: layer-1+
- TEST-WEB-016: Tests prove FR-WEB-016 no prompt/file telemetry.
  Scope: layer-1+
- TEST-WEB-017: Tests prove FR-WEB-017 desktop and mobile layout.
  Scope: layer-1+
- TEST-WEB-018: Tests prove FR-WEB-018 blocked lane and cycle rejection.
  Scope: layer-1+
- TEST-WEB-019: Tests prove FR-WEB-019 prompt survives recycle.
  Scope: layer-1+
- TEST-WEB-020: Tests prove FR-WEB-020 server-side cap enforcement.
  Scope: layer-1+
