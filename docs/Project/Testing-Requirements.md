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
- TEST-MCP-102: Given the provider-factory, native encryption, and maintenance-command workstreams, when SQLite, PostgreSQL, and SQL Server are configured for clean-database integration runs, then provider-specific migrations apply successfully and the live encryption state matches the configured state. When encryption is enabled or disabled on an existing database, then the provider-specific transition workflow shall expose a no-data-loss maintenance procedure and dry-run plan before mutation, with automated dry-run tests covering SQLite SEE, PostgreSQL `pg_tde`, and SQL Server TDE command generation. SQL Server provider and migration integration coverage shall use self-managed SQL Server LocalDB instances that are created and torn down by the test harness, while SQL Server TDE validation shall run against a separate non-LocalDB SQL Server target because LocalDB cannot validate TDE.
  Scope: layer-1+
- TEST-MCP-103: Given a Byrd execution TODO, when unit tests are not defined, then the service rejects transition to `Implementing`; when unit tests are defined through the test-plan API, then the TODO advances to `TestReady`.
  Scope: layer-1+
- TEST-MCP-104: Given a Byrd execution TODO linked to requirements, session turns, and modified files, when bounded execution context or checkpoint delta context is requested, then the server returns only concise snippets, recent turn summaries, relevant files, artifacts, commits, and updated next action for that TODO.
  Scope: layer-1+
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
  **Acceptance Criteria:**
  - [ ] Adapter diagnostics tests fail if any required domain is uncovered or reports incorrect local-only/apply-supported status.
  - [ ] Proxy tests prove live-forwarded domains keep working and only replayable mutating requests are queued during hub outage.
  - [ ] Topology tests prove stale base-version operations create conflicts and suppress fanout.
  - [ ] Replay and fanout tests prove signed envelopes are verified before local apply.
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
- TEST-MCP-147: Given agent-facing documentation, marker templates, pipeline references, and generated requirements wiki outputs, automated tests shall verify single-line JSON stdio guidance, current plugin registry guidance, existing pipeline file references, and Azure/GitHub wiki output file parity.
  Scope: layer-1+
- TEST-MCP-148: Build.Tests SHALL verify build.ps1 relaunches through a PowerShell host with -NoLogo, -NoProfile, and -NonInteractive, Build.Tests pwsh.exe helpers include those flags, and live deployment guidance examples include those flags.
  Scope: layer-1+
- TEST-MCP-149: Bats validation covers AC-SKILLS-001 through AC-SKILLS-006 for mcpserver-codex-plugin, mcpserver-claude-code-plugin, mcpserver-claude-cowork-plugin, mcpserver-copilot-plugin, and mcpserver-grok-plugin.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Every target plugin contains non-empty SKILL.md files for sync-logs, commit-sync, and wrap-up. (evidence: Codex manifest Bats, Copilot skills Bats, and full Claude Code, Cowork, and Grok skills Bats passed.)
  - [x] Every new skill has YAML frontmatter with name and description. (evidence: Shell Bats workflow skill frontmatter assertions passed.)
  - [x] sync-logs documents status check, session/turn handling, dialog/action appends, background-session discovery, factual summary, and no raw REST. (evidence: Shell Bats sync-logs AC-SKILLS-003 assertions passed.)
  - [x] commit-sync documents pause, repo-scope report, explicit acknowledgement, full dirty-tree staging, commit SHA capture, push result, and no force/rewrite behavior. (evidence: Shell Bats commit-sync AC-SKILLS-004 assertions passed.)
  - [x] wrap-up documents marker trust, requirement reconciliation, wiki export, validation, commit/push, session-log reconciliation, and turn completion/failure. (evidence: Shell Bats wrap-up AC-SKILLS-005 assertions passed.)
  - [x] Manifests or package files expose/package the new skills using each plugin's existing convention. (evidence: Codex/Claude/Grok skillsPath, Copilot skill entries, and Cowork skills directory convention validated.)
- TEST-MCP-150: Jest validation covers AC-SKILLS-001 through AC-SKILLS-006 for mcpserver-cline-plugin, mcpserver-cline-v2-plugin, and mcpserver-opencode-plugin.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Every target plugin contains non-empty SKILL.md files for sync-logs, commit-sync, and wrap-up. (evidence: Cline, Cline v2, and OpenCode skills Jest tests passed.)
  - [x] Every new skill has YAML frontmatter with name and description. (evidence: TypeScript skills Jest frontmatter assertions passed.)
  - [x] sync-logs documents status check, session/turn handling, dialog/action appends, background-session discovery, factual summary, and no raw REST. (evidence: TypeScript sync-logs AC-SKILLS-003 assertions passed.)
  - [x] commit-sync documents pause, repo-scope report, explicit acknowledgement, full dirty-tree staging, commit SHA capture, push result, and no force/rewrite behavior. (evidence: TypeScript commit-sync AC-SKILLS-004 assertions passed.)
  - [x] wrap-up documents marker trust, requirement reconciliation, wiki export, validation, commit/push, session-log reconciliation, and turn completion/failure. (evidence: TypeScript wrap-up AC-SKILLS-005 assertions passed.)
  - [x] package.json exposes or packages the skills directory using the plugin's existing distribution convention. (evidence: package.json files include skills/ and npm build/test passed for all TypeScript plugins.)
- TEST-MCP-151: Process validation covers red/green/final evidence and the zero-failure zero-skip focused validation gate for the workflow-skill rollout.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Validation tests fail before the skills/manifest changes and pass after implementation, with red and green command output captured in session-log actions. (evidence: Red gate failures and green/final passing commands were run and recorded in session-log actions.)
  - [x] Final validation has zero failures and zero skips in the executed focused scope. (evidence: Final Bats/Jest output reported all ok/pass and no skipped tests; git diff --check passed across all modified repos.)
- TEST-MCP-152: Unit and integration validation shall prove generated requirements exports render TEST descriptions readably and display acceptance criteria as a bulleted list for both GitHub and Azure wiki outputs.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Focused tests fail before the renderer change because wiki testing output still uses dense table rows for descriptions. (evidence: Red run of RequirementsDocumentServiceTests failed on missing TEST-MCP-001 subsection before renderer update.)
  - [x] Focused tests pass after the renderer change and assert AC bullets in both GitHub and Azure wiki outputs. (evidence: Focused service test run passed 10 of 10; combined requirements service and AC run passed 15 of 15.)
  - [x] Final validation has zero failures and zero skips in the executed scope. (evidence: build Compile succeeded; focused RequirementsDocumentService and RequirementAcceptanceCriteria tests passed 15 of 15; RequirementsControllerTests passed 7 of 7; no skipped tests were reported.)
- TEST-MCP-153: Regression tests SHALL verify all plugin batch requirement methods accept unindented YAML records, indented YAML records, and inline JSON-array records while preserving nested acceptanceCriteria arrays and boolean isSatisfied fields.
  Scope: layer-1+
- TEST-MCP-154: Given a TODO update payload that changes implementationTasks[].done and omits top-level done, when the plugin wrapper builds the TODO update HTTP body, then the body preserves nested task done values and omits parent done so the server cannot accidentally complete the TODO.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Regression test fails if nested implementation task completion is promoted to top-level done. (evidence: Bats output reported ok 1 TODO HTTP body does not promote implementation task done to parent done in all five plugin repositories.)
- TEST-MCP-155: Given mocked successful and failed session/compact hook dependencies, when SessionStart, SessionEnd, PreCompact, and PostCompact scripts run, then each status-only path emits {} and PostCompact emits no additionalContext.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Hook regression tests assert exact {} output for session-start, session-end, pre-compact, and post-compact. (evidence: tests/hooks.bats passed in the affected plugin repositories with exact-output assertions.)
  - [x] Copilot has hook regression coverage equivalent to the affected script surface. (evidence: Added F:\GitHub\mcpserver-copilot-plugin\tests\hooks.bats and passed 6/6.)
- TEST-MCP-156: Verify GitHubCliService passes command-scoped safe.directory environment variables for workspace-scoped calls and uses gh --repo without workspace cwd when a repository is configured.
  Scope: layer-1+
- TEST-MCP-157: Verify the PowerShell wrapper returns a non-zero result with a timeout diagnostic when a plugin helper command hangs beyond TimeoutSeconds, while normal helper wrapper tests still pass.
  Scope: layer-1+
- TEST-MCP-158: Keyserver trust and audit tests SHALL verify party registration, manifest signing, manifest verification, replay nonce rejection, stale sequence rejection, expiry rejection, disabled/unknown party or key rejection, key rotation descriptor preservation, signed manifest trace lookup/reporting, and audit persistence.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] `TransactionSecurityControllerTests`, `TransactionSecurityClientTests`, `DurableTransactionSecurityStorageTests`, and `SeparateTransactionServiceIntegrationTests` cover keyserver trust and audit behavior with zero skipped tests in the executed scope.
- TEST-MCP-159: Subscriber commit and abort tests SHALL verify manifest trust, protected diffgram decrypt/hash validation, idempotent duplicate commit, conflict rejection, abort semantics, transaction status reporting, key-ring rotation, durable status, and concurrent commit/replay behavior.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] `TransactionSecurityControllerTests`, `DurableTransactionSecurityStorageTests`, `SeparateTransactionServiceIntegrationTests`, and subscriber-focused transaction tests cover accepted, rejected, pending, committed, and aborted status paths.
- TEST-MCP-160: Real keyserver/subscriber integration tests SHALL validate the separate keyserver and subscriber hosts without mocks, including valid commit, tampered manifest, stale sequence, encrypted-body mismatch, subscriber key-ring configuration, and file-backed key provisioning.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] `SeparateTransactionServiceIntegrationTests` and durable transaction-security integration coverage pass with zero failures and zero skips.
- TEST-MCP-161: MCP transaction gating tests SHALL verify coordinator commit/degraded paths, durable timeout rollback cancellation, pub-sub handoff/replay/retention, federation apply/control-plane gating, memory add/update/delete rollback, TODO CRUD rollback, repo/template/requirements/session/tool registry compensation, GraphRAG/GitHub/context/voice/agent-pool fail-closed gates, stdio routing, and generic client protected namespace policy.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Focused and full Support.Mcp/Repl.Core test suites cover transaction gating and fail-closed behavior with zero skipped tests in the executed scope.
- TEST-MCP-162: Transaction traceability/import tests SHALL prove FR-MCP-118 through FR-MCP-128, transaction TR records, TEST-MCP-158 through TEST-MCP-173, and live TODO references resolve without placeholder transaction-plan entries.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] `TurnTransactionPlanArtifactTests.TransactionPlanRequirements_AreConcreteAndMapped` fails if transaction FR/TR/TEST records regress to placeholder backfills or lose matrix/mapping rows.
- TEST-MCP-163: Deferred-scope documentation tests SHALL prove remaining future autonomous Quad-Model branches, direct agent execution, desktop launch, tunnels, workspace/auth/server configuration, full remote/runtime compensation, complete delayed-rollback isolation, bucket/GitHub compensation, quarantine/fine-tuning automation, implicit fallback behavior, and full key-rotation lifecycle automation remain explicit future work rather than silently reported as complete.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] TurnTransactionPlanArtifactTests.PlanArtifacts_PreserveDeferredScopeAndDesignRounds validates deferred scope and design artifacts while distinguishing authorized Quad-Brain branches from remaining future scope. (evidence: tests/McpServer.Support.Mcp.Tests/Documentation/TurnTransactionPlanArtifactTests.cs)
- TEST-MCP-164: aiUnit plan review tests SHALL validate committed aiUnit run-log evidence for PLAN-TURNTRANSACTIONS-001 and fail on critical/high findings.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] `PlanTransactionReviewTests` validates `artifacts/aiunit-plan-review/aiunit-review-plan-20260612T060729.901Z.json`, reviewed scope, pass status, and absence of critical/high findings.
- TEST-MCP-165: Imported diagram preservation tests SHALL validate all six imported Mermaid diagrams, stable IDs, imported source references, and repo annotations.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] `TurnTransactionPlanArtifactTests.ImportedPlan_PreservesAllSixMermaidDiagramIds` covers AD-TXN-001, AD-CURIOSITY-001, SD-DIFFGRAM-001, AD-AOT-001, AD-WEIGHT-001, and ARCH-QUAD-001.
- TEST-MCP-166: Keyserver manifest diagram tests SHALL derive coverage from SD-DIFFGRAM-001 signing, verification, invalid, and valid branches.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Keyserver tests cover manifest sign/verify, hash validation, replay/sequence rejection, and signed manifest trace behavior.
- TEST-MCP-167: Subscriber diagram tests SHALL derive coverage from SD-DIFFGRAM-001 subscriber decrypt, hash, commit, reject, duplicate, abort, and status branches.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Subscriber tests cover protected-envelope commit, old-key decrypt, rotated-key decrypt, abort, and status lifecycle behavior.
- TEST-MCP-168: End-to-end turn transaction diagram tests SHALL derive coverage from AD-TXN-001 commit, subscriber unavailable, degraded, and no-success-before-ack branches.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Coordinator and transaction pub-sub tests cover signed manifest handoff, subscriber commit acknowledgement, timeout, degraded status, and durable replay.
- TEST-MCP-169: Degraded rollback reconciliation tests SHALL validate AD-TXN-001 and AD-AOT-001 in-scope rollback/audit behavior while requiring AoT execution to use the authorized ArbiterOfTruth transaction path.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Transaction-gated services and pub-sub tests verify rollback compensation, rollback failure reporting, durable pending-commit cancellation, additive audit evidence, and authorized AoT transaction routing. (evidence: BrainSlotInvocationTransactionTests and TurnTransactionPlanArtifactTests)
- TEST-MCP-170: Quad scope enforcement tests SHALL prove Curiosity admission, weight updates, AoT execution, and quad orchestration execute only through FR-MCP-129 through FR-MCP-135 authorization gates, while unrelated autonomous branches remain fail-closed.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Plan artifact tests validate the implemented/deferred split and reject stale claims that authorized AoT, weight update, or full orchestration branches remain disabled. (evidence: TurnTransactionPlanArtifactTests.PlanArtifacts_PreserveDeferredScopeAndDesignRounds)
- TEST-MCP-171: Quad architecture conformance tests SHALL validate ARCH-QUAD-001 component mapping, trust boundaries, authorized quad branches, and remaining future branches.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Plan artifact tests validate architecture/design artifacts, repo component references, authorized branch routing, and remaining future branch documentation. (evidence: TurnTransactionPlanArtifactTests.PlanArtifacts_PreserveDeferredScopeAndDesignRounds)
- TEST-MCP-172: Architecture Round 1 conformance tests SHALL validate trust model, component boundaries, storage boundaries, threat model, rollback/audit decisions, and gap analysis.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] `TurnTransactions-Architecture-Round1.md` is covered by `TurnTransactionPlanArtifactTests`.
- TEST-MCP-173: Design Round 2 conformance tests SHALL validate DTOs, entities, options, interfaces, endpoint contracts, reason codes, audit payloads, XMLDoc obligations, and AC-to-test mapping.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] `TurnTransactions-Design-Round2.md`, `TurnTransactions-Mutation-Endpoint-Audit.md`, requirements docs, matrix, and mappings are covered by `TurnTransactionPlanArtifactTests`.
- TEST-MCP-174: Brain slot documentation and plan artifact coverage SHALL prove FR-MCP-129 through FR-MCP-135, TR-MCP-QUAD-001 through TR-MCP-QUAD-007, TEST-MCP-174 through TEST-MCP-185, diagram annotations, and the AD-CURIOSITY-001-BR-EXTERNAL, AD-AOT-001, AD-WEIGHT-001, and full quad orchestration implementation split are present.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] TurnTransactionPlanArtifactTests fails if brain-slot requirement IDs, diagram annotations, mapping rows, or matrix rows are missing. (evidence: TurnTransactionPlanArtifactTests)
- TEST-MCP-175: Brain slot durable registry coverage SHALL prove workspace-scoped CRUD, role validation, one enabled slot per workspace and role, replaceExisting replacement audit, soft delete/disable semantics, credentialReference-only persistence, status projection, and workspace isolation.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] BrainSlotRegistryServiceTests covers create, update, enable, disable, delete, replacement, audit, readiness, and workspace isolation. (evidence: BrainSlotRegistryServiceTests)
- TEST-MCP-176: Brain slot REST, client, and STDIO contract coverage SHALL prove list/get/upsert/delete/enable/disable/status/invoke/orchestrate/aot-reconcile/weight-update routes serialize the public DTOs correctly, hide raw credentials, enforce AgentManager policy behavior, and preserve workspace/auth propagation.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] BrainSlotsControllerTests, BrainSlotClientTests, BrainSlotContractArtifactTests, and plugin tests cover every public brain-slot operation. (evidence: BrainSlotsControllerTests; BrainSlotClientTests; BrainSlotContractArtifactTests; brain-slots.test.ts)
- TEST-MCP-177: Brain slot provider and credential coverage SHALL prove env:, config:, and file: credential references resolve without persistence or response leakage; OpenAI and OpenAI-compatible client creation validates endpoint policy; disallowed hosts, loopback without explicit allowance, timeout, and cancellation fail closed without live network dependency.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] BrainSlotCredentialResolverTests and fake invocation/provider tests never require live network credentials. (evidence: BrainSlotCredentialResolverTests and fake provider coverage)
- TEST-MCP-178: Brain slot transaction admission coverage SHALL prove execution is rejected unless brain-slot execution and required turn transactions are enabled, no output is returned before subscriber commit, commit failure/timeout/degradation discards output, and diffgrams contain slot metadata and hashes.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] BrainSlotInvocationTransactionTests covers disabled gates, failed commits, delayed commits, and committed output return. (evidence: BrainSlotInvocationTransactionTests)
- TEST-MCP-179: Curiosity admission coverage SHALL prove only CuriosityEngine can request GraphRAG/context admission, admission happens only after committed subscriber acknowledgement, failed commits do not inject model output into cache/GraphRAG, and Left/Right/Arbiter invocations never mutate cache.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] BrainSlotInvocationTransactionTests and BrainSlotContainmentTests cover committed Curiosity admission and rejected non-Curiosity admission. (evidence: BrainSlotInvocationTransactionTests; BrainSlotContainmentTests)
- TEST-MCP-180: Quad containment coverage SHALL prove AoT reconciliation execution, weight update execution, and full automatic quad orchestration are available only through explicit FR-MCP-134/FR-MCP-135 gates, while non-Curiosity GraphRAG mutation and implicit fallback model behavior remain fail-closed.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] QuadBrainOrchestrationServiceTests covers the authorized quad branches and BrainSlotContainmentTests covers the remaining cache-mutation boundary. (evidence: QuadBrainOrchestrationServiceTests; BrainSlotContainmentTests)
- TEST-MCP-181: Quad orchestration service coverage SHALL prove full Quad-Brain orchestration rejects non-ready workspaces, invokes roles through transaction-gated slots in the required order, and returns final committed AoT output with role transaction metadata.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] QuadBrainOrchestrationServiceTests covers ready and non-ready orchestration paths. (evidence: QuadBrainOrchestrationServiceTests)
- TEST-MCP-182: AoT reconciliation execution coverage SHALL prove AoT reconciliation executes through the ArbiterOfTruth slot, includes Left/Right/Curiosity evidence, returns committed output only after subscriber acknowledgement, and fails closed without fallback.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] QuadBrainOrchestrationServiceTests covers Arbiter invocation and committed final output. (evidence: QuadBrainOrchestrationServiceTests)
- TEST-MCP-183: Quad weight update coverage SHALL prove approved weight updates persist weights and versions with audits, while missing approvals, stale versions, invalid roles, disabled slots, and invalid weights are rejected without mutation.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] QuadBrainOrchestrationServiceTests covers approved persistence/audit and missing-approval rejection. (evidence: QuadBrainOrchestrationServiceTests)
- TEST-MCP-184: Quad public contract parity coverage SHALL prove REST, client, STDIO, and Node plugin surfaces expose orchestration, AoT reconciliation, and weight update contracts consistently.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] BrainSlotsControllerTests, BrainSlotClientTests, BrainSlotContractArtifactTests, and brain-slots.test.ts cover public parity. (evidence: BrainSlotsControllerTests; BrainSlotClientTests; BrainSlotContractArtifactTests; brain-slots.test.ts)
- TEST-MCP-185: Quad traceability closure coverage SHALL prove FR-MCP-134 through FR-MCP-135, TR-MCP-QUAD-005 through TR-MCP-QUAD-007, TEST-MCP-181 through TEST-MCP-185, and the imported diagrams no longer describe AoT, weight updates, or full quad orchestration as deferred once implemented.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] TurnTransactionPlanArtifactTests covers the implemented/deferred split and matrix rows. (evidence: TurnTransactionPlanArtifactTests)
- TEST-MCP-186: Tests SHALL verify the ACID tightly coupled Microsoft Agent Framework profile metadata, strict option defaults, filtered model-visible tools, serialized function invocation settings, DI compatibility, and backward compatibility for default hosted-agent registration.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] A red test fails until the ACID profile metadata and option defaults are implemented.
  - [x] A red test fails until ACID run options filter unsafe tools and preserve serialized function invocation.
  - [x] A regression test proves default non-ACID hosted-agent registration still exposes the existing tool surface.
  - [x] Executed ACID profile tests finish with zero failed and zero skipped tests.
- TEST-MCP-187: Verifies that the hosted MCP coding agent executes coding prompts through the Quad Brain orchestration client surface without live external model calls. Integration test invokes mcp_quadbrain_coding_execute for multiple coding-task prompts and asserts the request path, payload, metadata, and committed response shape. Tests use in-memory MCP HTTP handler without live external model credentials or network calls.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] A prompt-array integration test invokes `mcp_quadbrain_coding_execute` for multiple coding-task prompts and asserts the request path, payload, metadata, and committed response shape.
  - [x] An ACID profile test proves the Quad Brain coding tool is exposed while unsafe tools remain blocked.
  - [x] Tests use an in-memory MCP HTTP handler and do not require live external model credentials or network calls.
  - [x] Executed ACID profile tests finish with zero failed and zero skipped tests.
- TEST-MCP-ACID-001: Baseline full ACID turn-transaction lifecycle with key server and subscriber mocked in-process and the coordinator as system under test; happy commit, mutation-abort+rollback, subscriber-unavailable degraded+rollback, and all published-message rejections.
  Scope: layer-1+
- TEST-MCP-ACID-002: Same lifecycle commits with key server and subscriber as real spun-up WebApplicationFactory hosts torn down after the test; coordinator drives sign and commit over the HTTP transports.
  Scope: layer-1+
- TEST-MCP-ACID-003: SignManifest success plus rejections - unknown party, unknown key, replay nonce, stale sequence.
  Scope: layer-1+
- TEST-MCP-ACID-004: VerifyManifest valid plus rejections relying parties act on - signature mismatch, wrong subscriber.
  Scope: layer-1+
- TEST-MCP-ACID-005: Commit, idempotent re-commit, and rejections - signature mismatch, encrypted-body mismatch, plaintext mismatch, stale sequence, wrong subscriber, decrypt-required failure. Subscriber validates the key server verification result.
  Scope: layer-1+
- TEST-MCP-ACID-006: Committed, bypassed (disabled/non-mutating), aborted, rejected on key-server sign failure fail-closed before mutation, rejected on subscriber commit rejection with rollback, degraded on subscriber unavailable. Coordinator validates key server and subscriber results.
  Scope: layer-1+
- TEST-MCP-AIUNIT-001: Add tests in tests/Build.Tests/ covering:
- Build target properties AiCodeReview and AiProjectReview exist.
- WriteAiUnitReviewMarkdownFromData produces correct MD file with prompt + response sections.
- CreateAiUnitClient (internal or exposed) constructs without throw when given valid IConfiguration stub (mocked config for ActiveStrategy, Strategies section); returns non-null client-like object.
- Optional: stubbed SendAsync test verifies that the returned client is called with correct FrontierRequest shape for given reviewType.

These tests must pass with mocks before the real client construction logic is filled (BDP).
  Scope: layer-1+
- TEST-MCP-AIUNIT-002: aiUnit must independently review warning suppression governance decisions, requirements traceability, TODO state, generated artifacts, and source suppression inventory.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] The aiUnit prompt names FR-MCP-139, TR-MCP-QUALITY-001, TEST-MCP-AIUNIT-002, and PLAN-WARNREMEDIATION-001. (evidence: tests/McpServer.Review.Tests/AiReviewTests.cs)
  - [x] The aiUnit prompt instructs review of requirements exports, TODO state, source suppression inventory, and the dedicated NUKE target. (evidence: tests/McpServer.Review.Tests/AiReviewTests.cs and build/Build.AiWarningSuppressionReview.cs)
  - [x] The aiUnit prompt requires structured findings for missing approvals, unapproved suppressions, unmatched acceptance criteria, TODO drift, and missing validation evidence. (evidence: tests/McpServer.Review.Tests/AiReviewTests.cs)
  - [x] The aiUnit review can be invoked directly through the AiWarningSuppressionReview NUKE target. (evidence: build/Build.AiWarningSuppressionReview.cs)
  - [ ] A completed warning remediation closeout must include the aiUnit review result or documented blocker before PLAN-WARNREMEDIATION-001 is marked done.
- TEST-MCP-AUTH-010: Given the auth-token subsystem is initialized, when a request hits a workspace-independent /mcpserver/* route with an unknown or missing API key and no X-Workspace-Path, then WorkspaceAuthMiddleware returns 401. This is a regression test (previously returned 503).
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Unknown API key with unresolved workspace returns 401.
  - [x] Missing API key with unresolved workspace returns 401.
  - [x] Empty RepoRoot fallback path with unresolved workspace returns 401.
- TEST-MCP-AUTH-011: When WorkspaceTokenService.IsInitialized is false, WorkspaceAuthMiddleware returns 503 with a Retry-After header and a JSON body.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Uninitialized subsystem returns 503 with Retry-After header and JSON body.
- TEST-MCP-AUTH-012: WorkspaceTokenService.IsInitialized is false before any token is generated and true after GenerateToken.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] IsInitialized returns false before any token is generated.
  - [x] IsInitialized returns true after GenerateToken.
- TEST-MCP-BATCH-001: Regression tests SHALL verify all plugin batch requirement methods accept unindented YAML records, indented YAML records, and inline JSON-array records while preserving nested acceptanceCriteria arrays and boolean isSatisfied fields.
  Scope: layer-1+
- TEST-MCP-BUGTRIAGE-042: Grok CLI one-shot argument construction forwards an explicitly configured model. GrokCliAgentExecutionStrategyTests.BuildGrokArgumentList_ConfiguredModel_IncludesModelFlag verifies a real model name (for example grok-4.3) emits --model before --output-format in the one-shot argument list (validates TR-MCP-TRIAGE-003 runner invocation construction).
  Scope: layer-1+
- TEST-MCP-BUGTRIAGE-043: Grok CLI startup-rejection guards for the triage research runner (validates TR-MCP-TRIAGE-003). GrokCliAgentExecutionStrategyTests verify: the sentinel model value auto (any casing) or an empty model omits --model entirely so the CLI picks its default, because current Grok CLIs reject --model auto with "unknown model id" and the runner substitutes auto for unset tier models (BuildGrokArgumentList_AutoOrEmptyModel_OmitsModelFlag, 5 cases); effort flags are pinned to high because current Grok CLIs reject max with "unknown effort level" (BuildGrokArgumentList_ContainsExpectedFlagsInOrder). Evidence 2026-07-14: both rejections reproduced from captured run stderr; fixes deployed in 1.4.15/1.4.16; live research run for triage-group-27f5ecfe4c926fde completed exit 0 at 20:08Z, first successful run since 2026-07-07.
  Scope: layer-1+
- TEST-MCP-DB-006: Validates TR-MCP-DB-006. tests/McpServer.Support.Mcp.Tests/Services/KeyedAsyncLockTests.cs: AcquireAsync_SameKey_BlocksUntilReleased asserts a second same-key acquire stays pending (Task.WhenAny vs a 250ms delay) until the first is disposed, then completes; AcquireAsync_DifferentKeys_DoNotBlockEachOther asserts a different-key acquire completes while the first key is held. The pair discriminates a correct keyed lock from a no-op (fails SameKey) and a global lock (fails DifferentKeys) - red-verified against a no-op stub, green after implementing per-key reference-counted SemaphoreSlim. SessionLogServiceReplaceDeleteTests confirm DeleteTurnAsync correctness is preserved under the lock (AC2).
  Scope: layer-1+
- TEST-MCP-DOCFXWIKI-001: Tests must prove typed DocFX configuration, isolated process execution, secure artifact mapping, backward compatibility, and GitHub/Azure wiki output integration.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Mock-backed unit tests cover configuration defaults, validation, process requests, platform filtering, staging, manifest inclusion, and failure cleanup.
  - [ ] A real DocFX scratch-workspace test generates content and verifies both GitHub and Azure output trees.
  - [ ] Traversal, absolute external paths, reparse escapes, duplicate targets, timeout, non-zero exit, and missing output are covered.
  - [ ] The current-plus-prior gate reports zero failures and zero skips.
- TEST-MCP-FILETOOLS-001: Unit tests must cover repository discovery behavior, path safety, MCP schemas, client delegation, hosted-agent registration, QBAgent registration, and backward compatibility.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Focused current-plus-prior unit scopes complete with zero failures and zero skipped tests at every Byrd slice gate.
  - [ ] Tests cover defaults, paging, truncation, recursion, regex/glob behavior, cancellation, caps, empty results, and path-policy failures.
- TEST-MCP-FILETOOLSINT-001: Integration tests must prove HTTP MCP, stdio MCP, REST client, hosted-agent, QBAgent, and workspace-isolation behavior for read_file, list_dir, and grep_files.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] HTTP and stdio tools/list advertise all three exact tool names and schemas.
  - [ ] All three tools execute against an isolated workspace through REST/client and MCP transport paths.
  - [ ] Development deployment passes live discovery and invocation checks before environment promotion.
- TEST-MCP-HEALTH-002: WorkspaceReadinessHealthCheck returns Healthy when an enabled primary workspace is registered and has a seeded token; returns Unhealthy when the token subsystem is uninitialized, no enabled workspace is registered, or the primary workspace has no seeded token.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Returns Healthy when enabled primary workspace is registered with seeded token.
  - [x] Returns Unhealthy when token subsystem is uninitialized.
  - [x] Returns Unhealthy when no enabled workspace is registered.
  - [x] Returns Unhealthy when primary workspace has no seeded token.
- TEST-MCP-HEALTH-003: Integration test with the data layer up: /mcpserver/todo returns 200 with a valid token and no X-Workspace-Path; unknown or missing keys return 401; /ready returns 200 Healthy with workspace-ready check listed.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] /mcpserver/todo returns 200 with valid token and no X-Workspace-Path.
  - [x] Unknown or missing API keys return 401.
  - [x] /ready returns 200 Healthy with workspace-ready check.
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
  **Acceptance Criteria:**
  - [x] Contract tests fail if the marker prompt omits the MCP Memories section, REQUIRED MEMORIES header, memory tool names, ID format, scope guidance, or no-secrets guidance. (evidence: MemoryContractArtifactTests.GeneratedMarkerPrompt_IncludesMemoryInstructions.)
  - [x] Contract tests fail if memory context documentation omits agent-local import safeguards, updatedBy attribution, or session-log action guidance. (evidence: MemoryContractArtifactTests.MemoryContextDocumentation_IncludesImportAndAttributionRules.)
- TEST-MCP-MEMORY-007: YAML schema and stdio contract tests SHALL prove valid workflow.memory.* envelopes pass; add without text fails; add/update with invalid scope fails; update/remove without ID fails; invalid MEMORY ID fails; unknown memory methods fail; and docs/stdio-tool-contract.json includes all memory tools.
  Scope: layer-1+
- TEST-MCP-MEMORY-008: Scope ordering tests SHALL prove list surfaces and required-memory injection return Global memories first sorted by ID and Workspace memories second sorted by ID, excluding workspace rows from other workspaces.
  Scope: layer-1+
- TEST-MCP-MEMORY-009: Agent plugin validation SHALL prove each supported plugin exposes memory tools and either injects REQUIRED MEMORIES with Global-first ordering at supported request boundaries or documents the host limitation with an explicit memory-list fallback.
  Scope: layer-1+
- TEST-MCP-MEMORY-FED-001: Memory federation tests SHALL prove memory adapter diagnostics, /mcpserver/memory domain inference, explicit-ID queued create eligibility, no-ID create rejection, queued memory update/delete replay metadata, signed envelope apply, stale base-version conflict behavior, fanout row creation, recipient apply, workspace ownership enforcement, invalid payload conflicts, version behavior, timestamp preservation, and idempotent soft-delete semantics.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Registry coverage includes memory as covered, non-local-only, and apply-supported.
  - [ ] Proxy tests prove explicit-ID memory creates queue and no-ID memory creates do not queue.
  - [ ] Proxy tests prove memory update/delete operations queue with domain memory and the path resource ID.
  - [ ] Adapter tests prove create preserves ID, scope, workspace ownership, category, raw text, timestamps, and version.
  - [ ] Adapter tests prove update increments version and preserves workspace ownership.
  - [ ] Adapter tests prove delete soft-deletes and replayed delete is idempotent.
  - [ ] Adapter tests prove cross-workspace operations, invalid JSON, and invalid IDs conflict without mutation.
  - [ ] Federation operation tests prove signed memory envelopes apply, stale versions conflict without overwrite, and hub fanout can be applied by a recipient.
- TEST-MCP-PLUGIN-011: Validates TR-MCP-PLUGIN-011. mcpserver-claude-code-plugin/tests/StopGateHardening.Tests.ps1: 'does not block a completed code-edit turn whose only audit signal is commits' seeds current-turn.yaml (status=completed, codeEdits=1, all audit counters 0 except auditCommits=2) and asserts the stop-gate output does not contain a block/'audit is incomplete'; 'no-ops instead of blocking for a task-notification phantom turn' seeds status=in_progress, queryTitle=<task-notification>, zero work, and asserts no block and no completeTurn in the repl log. Red before the fix (audit-incomplete block; completeTurn-did-not-mark-completed block), green after. Pester harness uses MCP_CACHE_DIR_OVERRIDE + MCP_PLUGIN_REPL_LOG/RESPONSE mocks.
  Scope: layer-1+
- TEST-MCP-PLUGIN-012: Validates TR-MCP-PLUGIN-012. mcpserver-claude-code-plugin/tests/CurrentTurnSessionRebind.Tests.ps1 dot-sources ..\lib\repl-invoke.ps1, seeds session-state.yaml (sessionId B) and current-turn.yaml (sessionId A, in_progress), calls Assert-ReplCurrentTurnFresh, and asserts it returns true and current-turn.yaml sessionId is now B. Red before the fix (stale-sessionId reject returned false), green after replacing the two sessionId hard-rejects with an active-session re-bind. ReplFailsafe.Tests.ps1 remain green (AC2).
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
  **Acceptance Criteria:**
  - [ ] A red test reproduces the documented appendDialog silent no-op with ConvertFrom-Yaml dictionary output.
  - [ ] Tests prove multi-item delegation and fail-closed empty payload behavior.
  - [ ] Canonical and propagated plugin suites complete with zero failures and zero skips.
- TEST-MCP-PLUGINCORE-005: Validates TR-MCP-PLUGINCORE-005. Doc-presence + parse check (receipt captured 2026-07-16): ConvertFrom-Yaml parses both templates/prompt-templates.yaml and src/McpServer.Support.Mcp/graphrag-global/input/canonical/templates/prompt-templates.yaml; both contain the strings 'same volume as the target' and 'cross-volume move' in the PowerShell.Mcp Command Routing block; the added guidance text contains no em-dashes/en-dashes (pre-existing dashes elsewhere in the template are out of scope).
  Scope: layer-1+
- TEST-MCP-PLUGININT-001: A shared deterministic Theory and companion AiTheory matrix must exercise the real Session Log workflow for all supported agent plugins.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Exactly eight scenario rows cover Codex, Claude Code, Claude Cowork, Copilot, Grok, Cline, Cline v2, and OpenCode.
  - [ ] Every deterministic row proves bootstrap, begin, append action/dialog, complete, durable query, and workspace cache isolation.
  - [ ] Every AiTheory row receives the persisted receipt/artifact and returns a strict semantic completeness result that is asserted by the test.
  - [ ] A legacy PLUGIN_ROOT_OVERRIDE value is injected and proven unable to alter the expected cache path.
  - [ ] The focused target and each plugin native suite complete with zero failures and zero skips.
- TEST-MCP-PLUGIN-TRIAGE-001: Every plugin skill bundle documents when and how to submit triage reports and the async expectation.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Skill tests or repository checks verify every plugin bundle includes triage guidance.
- TEST-MCP-QBAGENT-001: Marker present - QBAgent binds baseUrl/apiKey from the marker and reaches QuadBrain; only the QuadBrain route is exposed. Marker absent - QBAgent exits gracefully (defined exit, no endpoint contact, no unhandled exception).
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] With a valid marker, QBAgent binds baseUrl/apiKey and applies the QBAgent profile. (evidence: QBAgentBootstrapperTests valid-marker cases.)
  - [x] With no marker, QBAgent exits gracefully (exit 0) contacting no endpoint. (evidence: QBAgentBootstrapperTests no-marker case + QBAgentRunLoopTests.)
  - [x] The chat client targets the QuadBrain /v1 endpoint derived from the marker base URL. (evidence: QBAgentChatClientFactoryTests BuildEndpoint/Create.)
- TEST-MCP-QBAGENTINT-001: Integration tests for QBAgent sending a request to QuadBrain, receiving a response with or without tool actions, and executing the returned external tool calls via the Microsoft Agent Framework loop.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] QBAgent sends a prompt and receives a plain assistant response when no tool action is returned. (evidence: QBAgentSendingIntegrationTests.QBAgent_NoToolAction_ReturnsPlainResponse - real Agent Framework loop over OpenAI wire, 1 orchestration round, plain text returned.)
  - [x] When QuadBrain returns an external tool call, the Agent Framework loop executes the corresponding tool and continues the turn. (evidence: QBAgentSendingIntegrationTests.QBAgent_ExternalToolCall_AgentExecutesAndContinues - external apply_patch executed by FunctionInvokingChatClient, 2 rounds, final answer returned.)
  - [x] Internal tools are not executed by the agent (they were executed server-side); only external tool calls reach the agent. (evidence: QBAgentSendingIntegrationTests.QBAgent_InternalTool_ExecutedServerSide_NeverReachesAgent - mcp_todo_update ran in the internal executor and was stripped; agent invoked no tool, single round.)
- TEST-MCP-QBEXEC-001: Classifier marks mcp_ tools internal; interceptor executes handled internal tools and strips them while keeping external and failed/unhandled internal; the OpenAI surface strips internal tool calls and emits only external ones (and emits none when all elected tools ran server-side).
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Tools are classified internal (mcp_ prefix) vs external. (evidence: QuadBrainToolInterceptionTests classifier cases.)
  - [x] Internal tools execute server-side and are stripped; only external calls are emitted. (evidence: QuadBrainToolInterceptionTests + endpoint test ChatCompletions_InternalToolExecuted_IsStripped.)
  - [x] Internal-tool failures surface as a note, never as a tool command. (evidence: endpoint test ChatCompletions_InternalToolFailure_BecomesNote.)
- TEST-MCP-QBEXEC-002: Unit tests asserting mcp_todo_update routes through ITransactionGatedTodoMutationService, mcp_repo_edit through TransactionGatedRepoFileService, mcp_git push via ProcessRunner targets origin, and an unknown mcp_ tool returns Unhandled.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] todo/requirements/repo routes go through the transaction-gated services; unknown mcp_ returns Unhandled.
- TEST-MCP-QBEXEC-003: Unit tests asserting each brain invocation's full prompt+output is written to the session log under TurnId, AoT reconciliation is logged, and internal-tool executed/failed outcomes are recorded with secrets redacted.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Each brain invocation full prompt+output logged under TurnId; AoT logged; executed/failed outcomes recorded; secrets redacted.
- TEST-MCP-QBINT-001: Integration tests over POST /v1/chat/completions through the real ASP.NET pipeline with orchestration and the internal-tool executor replaced by deterministic doubles.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] A request without a workspace token returns 401.
  - [x] An authorized request returns the Arbiter decision as the assistant message.
  - [x] An external tool elected by QuadBrain is returned to the agent as a tool call.
  - [x] An MCP-internal tool executed server-side is stripped from the response.
  - [x] An internal tool failure is surfaced as a note rather than a tool call.
- TEST-MCP-QBLIVE-001: Service-composition coverage of the real four-role Quad-Brain loop (QuadBrainOrchestrationService + BrainSlotInvocationService + BrainSlotRegistryService + in-memory key server), faking only IBrainSlotChatClientFactory and the committing transaction coordinator.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] All four roles are invoked in order (Left, Right, Curiosity, Arbiter) and the committed Arbiter decision is returned.
  - [x] A tool_calls Arbiter output is returned verbatim as the orchestration output.
  - [x] With only three roles seeded the loop rejects QuadNotReady without calling any brain.
  - [x] With execution disabled no brain is called and the loop rejects ExecutionDisabled.
- TEST-MCP-QBLIVEINT-001: Integration coverage that drives the real orchestration through POST /v1/chat/completions over four seeded slots, faking only the per-brain LLM call and the transaction coordinator.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] A plain Arbiter decision is returned as the assistant message (finish_reason stop) with all four roles invoked.
  - [x] A tool_calls Arbiter output surfaces as an OpenAI assistant tool call (finish_reason tool_calls).
  - [x] With no slots seeded the endpoint returns an empty decision (loop rejects QuadNotReady).
- TEST-MCP-QBOPENAI-001: An inbound OpenAI ChatCompletion request maps to QuadBrain orchestration and returns an OpenAI-shaped response with the Arbiter output as the assistant message; later slices assert tool definitions flow through and assistant tool_calls are emitted, and that QBAgent executes them via the Agent Framework loop.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] An OpenAI ChatCompletion request maps to QuadBrain orchestration and returns the Arbiter output as the assistant message. (evidence: QuadBrainOpenAiChatServiceTests + QuadBrainOpenAiEndpointIntegrationTests.ChatCompletions_Authorized_ReturnsArbiterContent.)
  - [x] Tool definitions flow through and assistant tool_calls are emitted for external tools. (evidence: QuadBrainOpenAiChatServiceTests tool-call parsing + endpoint test ChatCompletions_ExternalTool_ReturnedAsToolCall.)
  - [x] Bearer / X-Api-Key auth is enforced (401 on missing/invalid token). (evidence: QuadBrainOpenAiAuthTests + endpoint test ChatCompletions_NoToken_Returns401.)
- TEST-MCP-QBSEED-001: Unit coverage for BrainSlotStartupSeeder over a real in-memory McpDbContext, real BrainSlotRegistryService, and the in-memory key server (only the credential resolver stubbed).
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] With execution enabled and four roles configured, StartAsync makes the quad ready (all roles enabled).
  - [x] Running the seeder twice is idempotent (exactly four enabled slots, no exception).
  - [x] With execution disabled, or with no slots configured, nothing is provisioned.
  - [x] One invalid slot is skipped without throwing and the remaining valid slots are still provisioned.
- TEST-MCP-QBSKILLS-001: Unit tests asserting the parser requires name+description, rejects missing name, and reads optional allowed-tools.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Requires name+description; rejects missing name; reads optional allowed-tools.
- TEST-MCP-QBSKILLS-002: Unit tests asserting discovery returns name+description only, load returns the full body, and discovery includes vendored + workspace skills.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Discovery returns name+description only; load returns full body; both roots included.
- TEST-MCP-QBSKILLS-003: Unit tests asserting list_skills and load_skill are exposed as external tools and return the discovery list and named body respectively.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] list_skills returns discovery; load_skill returns the named skill body.
- TEST-MCP-QBTOOLS-001: Unit tests asserting read_file/write_file/edit_file/list_files are registered as non-mcp_ external tools and delegate to the MCP client Repo surface (no direct filesystem access).
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] The four file tools are present, non-mcp_-prefixed, and delegate to the MCP client.
  - [ ] A path-traversal request is rejected server-side.
- TEST-MCP-QBTOOLS-002: Unit tests (mock IProcessRunner) asserting git status builds expected args and parses output, push targets origin only, and an unknown subcommand is rejected.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] status/diff/log build expected args and parse results.
  - [ ] push args never include a non-origin remote; unknown subcommand rejected.
- TEST-MCP-QBTOOLS-003: Unit test (mock ResolveExecutable) asserting run_bash returns available=false when bash.exe is missing and runs the command when present.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] bash absent yields available=false without throwing; bash present returns stdout/stderr/exit.
- TEST-MCP-QBTOOLS-004: Unit tests for EditAsync: replace-unique, ambiguous-fails, replace-all, missing-old-fails, traversal-rejected, nonexistent-fails, audit+updated-event.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Unique replace succeeds; missing/ambiguous fail per rules; traversal and nonexistent rejected; audit + change event emitted.
- TEST-MCP-QBTOOLS-005: Unit tests asserting gated EditAsync commits+audits, rolls back to original on transaction reject, and fails under a degraded coordinator.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Commit applies + audits; reject restores original; degraded coordinator fails.
- TEST-MCP-QBTOOLS-006: Unit tests asserting McpHostedAgentToolAdapter exposes mcp_repo_edit, mcp_bash, and mcp_git, and that mutating variants route through the transaction-gated core.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] mcp_repo_edit/mcp_bash/mcp_git present; mutating ones transaction-gated.
- TEST-MCP-QBTOOLS-007: Unit tests asserting run_powershell executes a command in a hosted runspace and returns captured output/error streams.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] run_powershell runs a command and returns output and error streams from the hosted runspace.
- TEST-MCP-QBTOOLSINT-001: Integration tests (CustomWebApplicationFactory + in-memory transport) where the agent loads a skill then calls edit_file (applied through server RepoFileService) and git status; plus internal mcp_repo_edit executed server-side never reaching the agent.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Agent loads a skill body then calls edit_file and the edit lands through the server.
  - [ ] git status runs externally and returns; mcp_repo_edit executes server-side and is stripped.
- TEST-MCP-QUAD-SESSION-001: Per-session QuadBrain instance attachment over global brains with session metadata propagation.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] CompleteAsync with a sessionId/turnId attaches them to the orchestration request metadata and TurnId.
  - [x] Without a session id, no session metadata is attached (anonymous instance).
  - [x] A /v1 request's X-Session-Id header reaches the orchestration (integration).
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
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Canonicalization theory covers Codex, ClaudeCode, GrokCode, and OpenCode-style names. (evidence: tests/McpServer.Repl.IntegrationTests/MarkerFileClientOptionsResolverTests.cs)
  - [x] Resolver test proves cache record agent key is canonical and AgentOverride remains unset after production resolution. (evidence: tests/McpServer.Repl.IntegrationTests/MarkerFileClientOptionsResolverTests.cs)
  - [x] Child process helper test proves --agent and the named value are present in the launched REPL argument list. (evidence: tests/McpServer.Repl.IntegrationTests/ReplChildProcessHelper.cs; tests/McpServer.Repl.IntegrationTests/MarkerFileClientOptionsResolverTests.cs)
- TEST-MCP-REPL-019: ✅ **Complete** - Given namespace-organized command shapes, when workflows execute, then operations delegate to typed client contracts without duplicating business logic. **Covered by:** `TodoWorkflowTests`, `SessionLogWorkflowTests`, `RequirementsWorkflowTests`, `GenericClientPassthroughTests`
  Scope: layer-1+
- TEST-MCP-REPL-020: ✅ **Complete** - Given concurrent REPL operations, when workflows maintain stateful context, then session state and TODO selection are properly isolated per workflow instance. **Covered by:** `SessionLogWorkflowTests` (state management), `TodoWorkflowTests` (selection state)
  Scope: layer-1+
- TEST-MCP-REPL-021: When TryResolveWithDiagnostics is called on a workspace path containing no marker file, the error message enumerates every directory walked from the start path to its root. This verifies diagnostic usability when marker files cannot be found during workspace resolution.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Error message enumerates all directories walked from start path to root
  - [x] Test passes with implementation in MarkerFileClientOptionsResolverTests.TryResolveWithDiagnostics_WhenMarkerMissing_EnumeratesSearchedPaths
- TEST-MCP-REPL-022: When an explicit workspacePathOverride pointing to a workspace with a valid marker is provided to TryResolveWithDiagnostics, resolution succeeds and the returned options carry the marker's API key.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Resolution succeeds when explicit workspace path points to valid marker
  - [x] Returned options contain the marker's API key
- TEST-MCP-REPL-023: When a marker file's canonicalization is tampered and TryResolveWithDiagnostics is called, the error names the marker path and identifies 'signature' failure. This verifies secure marker validation and error reporting.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Error message names the marker path
  - [x] Error identifies 'signature' as the failure reason
- TEST-MCP-REPL-024: Given a marker whose HMAC payload is signed with LF-only (\n) line endings (matching production MarkerFileService.AppendPayloadLine), when TryResolveWithDiagnostics is called on Windows or any platform where Environment.NewLine differs from \n, signature verification succeeds. This ensures cross-platform marker compatibility.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Signature verification succeeds for LF-only signed payload on all platforms
  - [x] Test validates cross-platform Environment.NewLine compatibility
- TEST-MCP-REPL-025: Mock-backed unit and real-filesystem integration tests SHALL prove primary and failsafe strategy isolation, non-terminal degradation isolation, terminal notification, replay artifact fidelity, V4 path scoping, atomic writes, cancellation, dual failure, and normal primary behavior with zero failures and zero skips.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] A failing primary mock does not fail non-terminal plugin persistence when the failsafe mock succeeds.
  - [ ] Closing a degraded turn returns the exact failsafe path and the artifact round-trips the attempted turn payload.
  - [ ] Primary success, explicit cancellation, and dual-failure paths are covered.
  - [ ] Current and prior McpServer.Repl.Core scopes complete with zero failures and zero skips.
- TEST-MCP-REPL-026: Validates TR-MCP-REPL-011 (PascalCase session-id agent + openSession persistence). mcpserver-claude-code-plugin/tests/SessionIdCanonicalAgent.Tests.ps1 dot-sources ..\lib\repl-invoke.ps1: asserts Get-ReplCanonicalAgentName('default')='Default' and matches ^[A-Z][A-Za-z0-9]*$, 'claude-code'/'claudecode'='ClaudeCode', 'codex'='Codex', 'grok'='GrokCode'; and Invoke-WorkflowOpenSession with 'sessionId: ClaudeCode-...-explicit' writes status=verified + that sessionId into session-state.yaml and returns true. Red before implementation (functions did not exist / openSession was a no-op), green after. Note: uses id 026 because TEST-MCP-REPL-011 was already taken.
  Scope: layer-1+
- TEST-MCP-REPL-027: Validates TR-MCP-REPL-012. mcpserver-claude-code-plugin/tests/ReplMethodTimeout.Tests.ps1 dot-sources ..\lib\repl-invoke.ps1: asserts Get-ReplMethodTimeoutSeconds returns >30 for workflow.todo.analyzeRequirements and workflow.requirements.generateDocument and exactly 30 for workflow.sessionlog.completeTurn/beginTurn; and with REPL_TIMEOUT=45/REPL_LONG_TIMEOUT=600 set, returns 45 for sessionlog and 600 for analyzeRequirements. Red before implementation (function absent), green after; Invoke-ReplRaw now uses Get-ReplMethodTimeoutSeconds.
  Scope: layer-1+
- TEST-MCP-REPL-028: ReplWorkspaceResolution.Tests.ps1: a marker-bearing current directory outranks an inherited MCP_WORKSPACE_PATH when the repl bridge resolves the workspace. Validates TR-MCP-REPL-013 / BUG-TRIAGE-077.
  Scope: layer-1+
- TEST-MCP-REPL-TRIAGE-001: Full client.triage.* and workflow.triage.* REPL surface works with correct envelopes.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] REPL tests cover passthrough, typed workflow routing, deprecated metadata, and errors.
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
  **Acceptance Criteria:**
  - [x] Direct sourced shell assertions pass for all five Bash plugin repos: criteria-only update emits caller criteria, no-criteria create omits criteria, and explicit empty response returns requirements_acceptance_criteria_not_captured. (evidence: Focused shell assertions passed for Codex, Claude Code, Claude Cowork, Copilot, and Grok.)
  - [x] Focused Jest tests pass for Cline, Cline v2, and OpenCode covering criteria-only update forwarding and explicit empty-response failure. (evidence: Cline and Cline v2 requirements.test.ts passed; OpenCode complex-tools.test.ts passed with coverage disabled for focused scope after full file tests passed.)
  - [x] TypeScript plugin builds pass after the guard is added. (evidence: npm run build passed for Cline, Cline v2, and OpenCode.)
- TEST-MCP-REQAC-PLUGIN-BASH: Bash plugin repl-invoke shim tests cover acceptanceCriteria create/update blocks and copyAcceptanceCriteriaFromTodo dispatch, with focused Bats gates passing for all five Bash plugins.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Codex, Claude Code, Claude Cowork, Copilot, and Grok Bash plugin focused Bats gates pass for acceptanceCriteria and copyAcceptanceCriteriaFromTodo. (evidence: Focused WSL Bats filters returned AC_EXIT=0 and COPY_EXIT=0 for all five Bash plugin repos.)
- TEST-MCP-REQACPLUGIN-BASH: In each bash plugin, tests/repl-invoke-shim.bats (or tests/plugin-helpers.bats) proves typed-params emits acceptanceCriteria on create and hydrates it from existing on partial update with zero failures and zero skips.
  Scope: layer-1+
- TEST-MCP-REQACPLUGIN-CAPTURE: Plugin regression tests prove caller-supplied acceptanceCriteria is not silently lost when requirement create/update responses explicitly report an empty criteria list.
  Scope: layer-1+
- TEST-MCP-REQACPLUGIN-LIVE: Per plugin family, a live invocation of workflow.requirements.createFr with acceptanceCriteria populated round-trips through the deployed server (commit 6d376ea+) and the resulting REST GET returns the structured criteria.
  Scope: layer-1+
- TEST-MCP-REQAC-PLUGIN-TS: TypeScript plugin tests cover requirements acceptanceCriteria schemas, typed parameter shaping, and live-equivalent request handling for Cline, Cline v2, and Opencode.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Cline, Cline v2, and Opencode build/test gates pass with acceptanceCriteria coverage. (evidence: Cline and Cline v2 npm build/test passed; Opencode npm build and full Jest passed with coverage thresholds.)
- TEST-MCP-REQACPLUGIN-TS: In each TS plugin, tests/requirements.test.ts (or tests/complex-tools.test.ts) proves req_create_fr/req_update_fr/req_create_test forward acceptanceCriteria into the request payload with zero failures and zero skips.
  Scope: layer-1+
- TEST-MCP-REQEXPORT-002: Validates TR-MCP-REQEXPORT-002. tests/McpServer.Support.Mcp.Tests/Controllers/RequirementsControllerGenerateTests.cs: GenerateAsync_WikiUnlistedException_ReturnsStructuredErrorNamingExceptionType substitutes IRequirementsDocumentService.GenerateWikiAsync to throw KeyNotFoundException and asserts the action returns an ObjectResult status 500 whose body contains the message and KeyNotFoundException (exceptionType). Red before the catch-all (the exception escaped/threw), green after adding catch(Exception)->BuildGenerateExportError to both wiki try blocks. Existing GenerateAsync_WikiConfigFailure/WikiConflictFailure/WikiZipAssemblyFailure tests remain green (AC3).
  Scope: layer-1+
- TEST-MCP-REQEXPORT-003: Verifies generateDocument accepts format=markdown for docType=matrix (and other non-wiki docTypes) without a format rejection at the schema, validator, and workflow layers. Validates TR-MCP-REQEXPORT-003 / BUG-TRIAGE-074.
  Scope: layer-1+
- TEST-MCP-REQWS-001: Explicit workspacePath override for requirements document generation (follow-up to triage-report-f77331f9a33e4bd0ae4f55f0470743ed). RequirementsClientTests verify GenerateAsync with a workspacePath override replaces the client-bound X-Workspace-Path header for that call only and the bound header is preserved without an override. RequirementsWorkflowWorkspaceOverrideTests verify the real RequirementsWorkflow forwards the override to the generate request, preserves the bound workspace when absent, and the ReplCommandDispatcher forwards the workspacePath param from workflow.requirements.generateDocument envelopes to the workflow. Cross-workspace override without the target workspace's API key fails with 401 (per-workspace keys) instead of silently exporting the session-bound workspace's requirements. Evidence 2026-07-14: red before implementation, Client 23/23 and Repl.Core 810/810 green after; deployed in service and mcpserver-repl 1.4.15+.
  Scope: layer-1+
- TEST-MCP-SESSIONLOG-001: Validates TR-MCP-SESSIONLOG-001. tests/McpServer.Support.Mcp.Tests/McpStdio/SessionLogLifecycleToolErrorTests.cs: SessionLogCompleteTurn_MalformedTurnJson_ReturnsStructuredError and SessionLogFailTurn_MalformedTurnJson_ReturnsStructuredError assert a malformed turnJson yields a JSON {error} (with message, no success) instead of a thrown JsonException; SessionLogCompleteTurn_NullTurnJson_ReturnsSuccess asserts the happy path still returns {success:true}. Red before the fix (2 of 3 threw), green after moving the deserialize into a try/catch and ApplyWorkspaceOverride inside the service try.
  Scope: layer-1+
- TEST-MCP-SESSIONLOG-002: Validates TR-MCP-SESSIONLOG-002. tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTests.cs: QueryAsync_TextMatchesProcessingDialogContent seeds a session whose unique token exists only in a ProcessingDialog item Content and asserts the text query returns it; QueryAsync_TextMatchesActionDescription does the same for an action Description. Red before widening BuildSearchText (both returned 0), green after. Existing QueryAsync scalar/boolean search tests (WhenQueryingByBooleanTextThenTermsCanMatchAcrossTurnFields et al.) remain green as the AC3 regression guard.
  Scope: layer-1+
- TEST-MCP-SESSIONLOG-003: Validates TR-MCP-SESSIONLOG-003. tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTests.cs: UpsertTurnAsync_CompletedEmptyTurn_AcceptedForClaudeCode_RejectedForQBAgent seeds a ClaudeCode session and a QBAgent session, then asserts a completed turn with zero decisions/actions/commits returns a turnId for ClaudeCode and throws ArgumentException for QBAgent. Regression guard locking the QBAgent-only scope so a future broadening of the gate to standard agents fails. Tool-description accuracy (AC2) applied in FwhMcpTools.SessionLog.cs.
  Scope: layer-1+
- TEST-MCP-SESSIONLOGSAN-001: Tests must prove default and configured redaction across the complete session-log DTO graph and all supported read transports without changing persisted raw data or query semantics.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Unit tests cover every default detector, overlapping rules, deterministic replacements, recursive object payloads, invalid patterns, duplicate IDs, timeouts, and non-mutation.
  - [ ] Service tests cover QueryAsync and GetAsync for every DTO field and preserve total count, ordering, offset, and limit.
  - [ ] HTTP, stdio, and federated integration tests return redacted payloads while direct database verification retains raw values.
  - [ ] Executed current-plus-prior test scope reports zero failures and zero skips.
- TEST-MCP-SESSIONLOGSAN-002: Validates TR-MCP-SESSIONLOGSAN-002. tests/McpServer.Support.Mcp.Tests/Services/SessionLogSanitizerTimeoutTests.cs: CreateTimeoutSanitizer now constructs SessionLogSanitizer with an injected RegexReplaceInvoker that raises RegexMatchTimeoutException deterministically for the catastrophic rule on large input (length>10000 + pattern match, no wall-clock). SanitizeString_WhenConfiguredRuleTimesOut_ReturnsTimeoutTokenAndDoesNotLogInput and SanitizeSessionLog_WhenOneFieldTimesOut_ContinuesSanitizingOtherFields verified 10/10 green across repeated isolated runs (previously flaked pass/pass/fail). Production path uses the default Regex.Replace invoker.
  Scope: layer-1+
- TEST-MCP-SUBLOG-001: Parseable sink posts a correctly shaped batch to /api/v1/ingest with X-P-Stream and basic auth; the subscriber invokes the message log once per received message with correct status/reason; sink errors do not fail the commit; no-op default logs nothing.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] A no-op subscriber message-log default exists and a Parseable HTTP sink POSTs a flat JSON batch with X-P-Stream + basic auth. (evidence: SubscriberMessageLogTests Parseable sink cases.)
  - [x] One message-log entry is emitted per received message at the audit chokepoint, independent of the durable audit gate. (evidence: SubscriberMessageLogTests chokepoint case.)
- TEST-MCP-TODO-CLOSE-001: Unit tests cover REST and typed client close-by-id behavior, including timestamp creation and missing item failure.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Controller tests prove close-by-id sets done true and a completion timestamp. (evidence: TodoControllerTests.CloseAsync_WhenItemExists_SetsDoneAndCompletedDate.)
  - [x] Controller tests prove close-by-id preserves not-found failure behavior. (evidence: TodoControllerTests.CloseAsync_WhenItemMissing_ReturnsNotFound.)
  - [x] Client tests prove the close method calls the dedicated endpoint and deserializes TodoMutationResult. (evidence: TodoClientTests.CloseAsync_PostsCorrectUrl.)
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
  **Acceptance Criteria:**
  - [x] Fixtures identify supported source kinds and reject ambiguous/unknown bundles. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: TranscriptFixtureInventoryTests; Detector_DiscoversEverySupportedRealTranscriptSource.)
- TEST-MCP-TRANSCRIPT-002: Unit tests verify neutral-to-session-log mapping preserves native values, keeps absent semantics absent, and marks deterministic derived IDs.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Missing model, token, response, action, status, and timestamp fields are not invented. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: loss-aware normalization and unsupported/malformed diagnostics in TranscriptCorePipelineTests.)
- TEST-MCP-TRANSCRIPT-003: Unit tests cover malformed lines, unknown events, incomplete turns, mixed schemas, cancellation, limits, traversal, and path escape rejection.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Strict mode rejects error diagnostics while lenient mode returns valid data plus diagnostics. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: malformed/unsupported/incomplete adapter tests and HTTP strict=false multi-status coverage.)
- TEST-MCP-TRANSCRIPT-004: Unit tests verify canonical YAML serialization is deterministic, redacted, LF-normalized, UTF-8 without BOM, and round-trips to equivalent objects.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Repeated runs over identical normalized input produce byte-identical YAML. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: NormalizationWritesArtifactsWithoutSessionPersistence and canonical mapping checks.)
- TEST-MCP-TRANSCRIPT-005: Unit tests verify idempotent replay keys, richer-field preservation, pre-submit failsafe creation, and retention/deletion based on precise persistence receipts.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Failsafe files are deleted only when persisted=true and degraded=false. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: HTTP path persistence/deletion; pending failsafe envelope; root-id failsafe naming; TEST-MCP-REPL-025 Pester.)
- TEST-MCP-TRANSCRIPT-006: Unit and contract tests verify transcript conversion is implemented only in the shared non-plugin transcript core and plugin packages contain no transcript ingestion helpers, skills, endpoint shortcuts, or parser forks.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Plugin inventory tests prove transcript ingestion helper files, skills, and REPL endpoint shortcuts are absent. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: plugin Pester TEST-MCP-TRANSCRIPT-010 endpoint absence and legacy parser removal.)
  - [x] Shared core transcript tests cover parser and projector behavior without relying on plugin-specific ingestion code. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: Support.Mcp transcript unit scope 60/0/0 and plugin endpoint absence.)
- TEST-MCP-TRANSCRIPT-007: Integration tests cover /mcpserver/sessionlog/ingest/path and /upload for defaults, folder discovery, multipart, ZIP limits, and security rejections.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Invalid requests return 400, unauthorized paths 403, exceeded limits 413, and mixed folder runs 207 Multi-Status where appropriate. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: TranscriptIngestionControllerTests status mapping; SessionLogTranscriptIngestionControllerTests mixed ZIP/security.)
- TEST-MCP-TRANSCRIPT-008: Tests verify typed client, REPL commands, HTTP MCP discovery, stdio MCP discovery, and tool invocation for transcript ingestion and normalization.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] sessionlog_ingest_path and sessionlog_normalize_path require workspacePath and delegate through the shared service. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: TranscriptMcpTool/StdioHost; McpTransport transcript tools; Repl.Core dispatcher; Client ingest tests.)
- TEST-MCP-TRANSCRIPT-009: Tests cover Cline paired JSON/JSONL, Copilot events folders, OpenCode JSONL, and read-only OpenCode SQLite snapshot normalization.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] OpenCode SQLite tests prove consistent backup snapshot use and no source DB/WAL writes. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: OpenCodeSqliteTranscriptTests normalization without source writes and WAL snapshot capture.)
- TEST-MCP-TRANSCRIPT-010: End-to-end plugin tests verify Claude, Codex, and Grok plugin packages do not expose transcript ingestion while model-run logging continues through normal workflow.sessionlog tools and non-plugin transcript ingestion remains externally verifiable.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Claude, Codex, and Grok plugin packages expose no transcript ingestion skill, helper, or endpoint shortcut. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: clean plugin Pester TEST-MCP-TRANSCRIPT-010 endpoint absence.)
  - [x] Representative model sessions can write turns, actions, and completions through workflow.sessionlog without automatic transcript import. (evidence: 2026-07-10 focused gates: Support.Mcp transcript unit 60/0/0, transcript integration+McpTransport 22/0/0, Repl.Core transcript 4/0/0, Client ingest transcript 2/0/0, clean plugin Pester 47/0/0. Tests: clean plugin Pester model-authored logging checks; live Codex turn req-20260711T022759Z-prompt-c4f0. External-client sampling remains the separate deployment TODO gate.)
- TEST-MCP-TRANSCRIPT-011: Codex transcript adapter coverage for real rollout record classes (validates TR-MCP-TRANSCRIPT-002 and TR-MCP-TRANSCRIPT-003). Unit tests in tests/McpServer.Support.Mcp.Tests/Ingestion/CodexTranscriptAdapterCoverageTests.cs verify: function_call and custom_tool_call records normalize to assistant tool-call events with call_id/name/status metadata; function_call_output and custom_tool_call_output records normalize to tool-role events preserving output text and call pairing; reasoning records with recoverable summary text normalize to assistant reasoning events while encrypted-only reasoning is skipped and reported through one aggregate info diagnostic (codex_encrypted_reasoning); event_msg records are skipped as UI mirrors of response_item records with one aggregate info diagnostic (codex_event_msg_skipped) and no warnings; turn_context records contribute session model and workspace path without diagnostics; world_state and compacted records are skipped with one aggregate info diagnostic (codex_nonconversation_skipped); unknown top-level record types and unknown response_item payload types warn once per distinct type with occurrence counts (codex_unknown_record, codex_unknown_response_item). Evidence 2026-07-14: 9/9 tests red against prior adapter, green after fix; real 2599-line rollout normalizes to 1174 events with 0 warnings (previously 166 events with 2432 warnings).
  Scope: layer-1+
- TEST-MCP-TRANSCRIPT-012: Imported-session lifecycle semantics (validates TR-MCP-TRANSCRIPT-004). Unit tests in tests/McpServer.Support.Mcp.Tests/Services/SessionLogImportedSessionDeleteTests.cs and SessionLogResubmissionReviveTests.cs verify: turn-level keyed operations (DeleteTurnAsync, ReplaceTurnSectionAsync, DeleteTurnItemAsync) accept provider-native identifiers persisted by transcript imports (UUID session ids, tool-call request ids) so turns can be repaired by resubmission; DeleteSessionAsync remains canonical-only by policy, rejecting imported session ids (sessions are soft-delete only and never deletable for imports); SubmitAsync revives a soft-deleted session that still holds the unique (WorkspaceId, SourceType, SessionId) key by restoring its row graph (session, turns, child rows) with only the SoftDelete named query filter bypassed (Workspace tenancy filter stays active) and then applying the resubmitted turn data; whitespace identifiers stay rejected. Evidence 2026-07-14: revive tests red with InvalidOperationException (disappeared after UNIQUE constraint failure) before fix, 7/7 green after; full suite green (build.ps1 Test exit 0); live recovery of session 019f2580-48c8-7912-b6a9-27f61b18d0d3 in F:\GitHub\MouseKeyProxy from tombstone to 1174 corrected turns via re-ingest plus 28 turn-level deletes of stale duplicates.
  Scope: layer-1+
- TEST-MCP-TRIAGE-001: Intake accepts valid reports and rejects invalid reports across REST, client, and REPL.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Unit and integration tests cover valid and invalid report submission through public surfaces.
- TEST-MCP-TRIAGE-002: Deterministic grouping, McpServer workspace routing for core and plugin bugs, and 15-minute quiet-window behavior are verified.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Tests prove grouping keys, workspace isolation, quiet deadline resets, and McpServer core/plugin routing fallback behavior.
- TEST-MCP-TRIAGE-003: Research worker invokes configured direct agent with group JSON and prompt.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Tests verify dispatch input and configured prompt rendering.
- TEST-MCP-TRIAGE-004: Schema-valid research output creates exactly one BUG-TRIAGE-### TODO.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Tests verify idempotent TODO creation from valid research output.
- TEST-MCP-TRIAGE-005: Invalid agent output or failed agent run creates no TODO and leaves inspectable failure state.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Tests verify failed runs preserve output or errors and do not create TODOs.
- TEST-MCP-TRIAGE-006: Multi-workspace isolation prevents cross-workspace grouping and status leakage.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Tests verify query filters and grouping scope never cross workspace boundaries.
- TEST-MCP-TRIAGE-REQAC-001: Every new FR/TR/TEST acceptance criterion is referenced by at least one test and passes ValidateTraceability.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Traceability validation covers all triage requirement IDs and acceptance criteria.
- TEST-MCP-WIKIEXPORT-001: Tests must cover docs/wiki.yaml loading, validation, renderer output, service integration, unchanged default behavior, and BDPv4 traceability for configured GitHub and Azure wiki exports.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Loader tests cover valid schema, absent optional home template, invalid schema, duplicate ids, duplicate targets, bad platforms, missing source files, invalid generated sources, path traversal, and navigation references to unknown or duplicated documents.
  - [x] Service tests prove no docs/wiki.yaml preserves current GitHub and Azure wiki output.
  - [x] Export tests prove configured workspace Markdown and generated sources are emitted to eligible GitHub and Azure folders with correct platform filtering.
  - [x] Navigation tests prove GitHub _Sidebar.md and Azure .order files follow the configured tree including nested sections.
  - [x] Home tests prove template token replacement and default navigation-derived home rendering.
  - [x] Invalid configuration tests prove no existing export files are modified on validation failure.
  - [x] Build guidance or traceability tests prove generated manifests include configured documents and preserve required requirements documents when declared.
- TEST-MCP-WIKIEXPORT-002: Tests must prove marker generation creates a valid default docs/wiki.yaml, preserves existing configs, and keeps marker generation behavior intact.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] A marker write in a workspace without docs/wiki.yaml creates docs/wiki.yaml and the marker file.
  - [x] The generated docs/wiki.yaml deserializes to an object with schema mcp-wiki-export/v1, six declared generated documents, and navigation references covering every document once.
  - [x] A marker write in a workspace with an existing docs/wiki.yaml preserves the exact existing content.
  - [x] Focused marker and wiki export tests pass with zero failures and zero skips.
- TEST-REQAC-LIVE-001: Live criteria round-trip works
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Criterion A
  - [x] Criterion B (evidence: passed via integration test)
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
  **Acceptance Criteria:**
  - [x] Open operation is idempotent
  - [x] Begin creates session in in_progress state
  - [x] Complete merges and finalizes with evidence gate
  - [x] Fail records note in session
- TEST-SUPPORT-015: SQLite tests verify that partial session submit preserves omitted title/model and sparse turn submit preserves omitted response/queryText and prior collections.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Partial session submit preserves omitted title and model fields
  - [x] Sparse turn submit preserves omitted response and queryText fields
  - [x] Prior collections are preserved during partial updates
- TEST-SUPPORT-016: When SessionLogService with a non-null WorkspaceContext calls SubmitAsync, SessionLogEntity.WorkspaceId and every child entity's WorkspaceId equal the context's WorkspacePath. This ensures proper workspace isolation for session logs.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] SessionLogEntity.WorkspaceId matches context WorkspacePath
  - [x] Every child entity's WorkspaceId matches context WorkspacePath
- TEST-SUPPORT-017: When SessionLogService is constructed with workspaceContext: null and a DbContext without _workspaceId, SubmitAsync persists a session with SessionLogEntity.WorkspaceId remaining empty string.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] WorkspaceId remains empty string when workspace context is null
  - [x] Session persists successfully without workspace context
- TEST-SUPPORT-018: When a malformed POST body fails JSON deserialization against UnifiedSessionLogDto, the controller returns 400 with application/problem+json content-type and errors object contains the field path (e.g., $.workspace), never the dto parameter name.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Response content-type is application/problem+json
  - [x] Errors object contains field path ($.workspace) not parameter name
- TEST-SUPPORT-019: When a POST body is missing sourceType, domain validation rejects it with application/problem+json response citing sourceType (not legacy {"error":"..."} plain shape).
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Response is application/problem+json format
  - [x] sourceType field is cited in error response
- TEST-SUPPORT-020: When a successful POST to /mcpserver/sessionlog completes, GET /mcpserver/sessionlog/{agent}/{sessionId} under the same workspace context returns 200 OK with the round-tripped session.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] POST to /mcpserver/sessionlog succeeds
  - [x] GET /mcpserver/sessionlog/{agent}/{sessionId} returns 200 OK
  - [x] Response contains the round-tripped session data
- TEST-SUPPORT-021: When the turn-append route receives a POST to /mcpserver/sessionlog/{agent}/{sessionId}/turn with a UnifiedRequestEntryDto, 201 is returned and the subsequent GET shows the appended turn.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] POST to turn route returns 201 Created
  - [x] Subsequent GET shows the appended turn
- TEST-SUPPORT-022: When PUT is used on the turn-append route instead of POST, the endpoint returns 405 Method Not Allowed with 'Allow: POST' header.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] PUT request returns 405 Method Not Allowed
  - [x] Response includes Allow: POST header
- TEST-TRIAGE-001: Unit and contract tests cover triage dashboard bucketing data, group/report/result rendering inputs, empty/error states, workspace filtering, and typed client dispatch for Director and MCP Web consumers.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Service tests cover dashboard queue composition, run history/result mapping, workspace isolation, and empty data.
  - [ ] Controller tests cover dashboard, run query, run detail, and not-found/error envelopes.
  - [ ] Client tests cover new typed triage methods and query-string dispatch.
- TEST-TRIAGE-002: Unit and client tests cover triage-created TODO listing, workspace filtering, missing TODO anchors, group/run context mapping, and typed client dispatch.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [x] Service tests verify TODO ID and CreatedAtUtc values come from TodoRecordEntity and remain workspace-scoped. (evidence: TriageServiceTests.QueryCreatedTodosAsync_ReturnsTodoIdsCreatedAtUtcAndTriageContext)
  - [x] Controller tests verify the read-only endpoint returns the service result. (evidence: TriageControllerTests.QueryCreatedTodosAsync_ReturnsCreatedTodoIndex)
  - [x] Client tests verify the typed triage TODO method calls the expected URL with workspace filters. (evidence: TriageClientTests.QueryCreatedTodosAsync_SendsWorkspaceFilter)
