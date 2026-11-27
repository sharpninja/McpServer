# TEST-MCP

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP

### TEST-MCP-001

Given configurable RepoRoot/Todo paths, when service starts, then path resolution is correct.


### TEST-MCP-002

Given TODO API operations, when create/update/delete/query run, then contracts remain stable.


### TEST-MCP-003

Given multi-tenant workspace configuration, when requests are made with different `X-Workspace-Path` headers, then data remains isolated per workspace on the single shared port.


### TEST-MCP-004

Given vector + FTS data, when context search executes, then hybrid results are returned.


### TEST-MCP-005

Given GitHub sync enabled, when issue sync runs, then ISSUE-* mapping is consistent.


### TEST-MCP-006

Given STDIO mode, when tool requests are sent, then parity with HTTP behavior is preserved.


### TEST-MCP-007

Given workspace registration, when a workspace is created, then its directory scaffold is created and an `AGENTS-README-FIRST.yaml` marker file is written to its root pointing to the shared host port.


### TEST-MCP-008

Given tool registry with tags, when keyword search runs with a singular or plural term, then matching tools from both global and workspace scopes are returned. Given default buckets in config, when the server starts for the first time, then buckets are seeded and idempotent on subsequent starts.


### TEST-MCP-009

Given per-workspace auth tokens and `X-Workspace-Path` header resolution, when a request to any `/mcpserver/*` endpoint lacks `X-Api-Key`, then the server returns 401. When a valid token is provided, workspace resolution uses the three-tier chain: `X-Workspace-Path` header → API key reverse lookup → default workspace.


### TEST-MCP-010

Given valid pairing credentials, when the `/pair` login flow completes, then an HttpOnly session cookie is issued and the API key is returned. Given constant-time comparison, when two passwords of the same length differ by one character, then timing side-channel is not exploitable.


### TEST-MCP-011

Given a configured tunnel provider, when the hosted service starts, then the tunnel process launches and `GetStatusAsync` returns a public URL. When the service stops, the process is terminated within 5 s.


### TEST-MCP-012

Given an MCP client connecting to `/mcp-transport`, when a tool call is made, then the response is semantically equivalent to the corresponding REST endpoint result. Given a request without the required `Accept` header, then the endpoint returns 406.


### TEST-MCP-013

Given a workspace, when `StartAsync` completes, then `AGENTS-README-FIRST.yaml` exists at the workspace root with the shared host port, endpoint paths, and auth token. When `StopAsync` completes, then the marker file is removed.


### TEST-MCP-014

Given a TODO item with a title and description, when `RequirementsService.AnalyzeAsync` is called, then `ExtractRequirementIds` correctly parses both JSON-block and regex-fallback response formats and returns distinct, non-empty FR/TR ID lists.


### TEST-MCP-015

Given a Markdown file with a `# Session Log - {title}` header, when `MarkdownSessionLogParser.TryParse` is called, then it returns a `UnifiedSessionLogDto` with matching title, model, status, and at least one entry. Given a file without the header, then `TryParse` returns null.


### TEST-MCP-026

Given a CQRS Dispatcher with a registered command handler, when `SendAsync` is called with a valid command, then the handler is invoked and `Result<T>.IsSuccess` is true with the expected value.


### TEST-MCP-027

Given a CQRS command handler that returns `Result.Failure(error)`, when the Dispatcher processes the result, then it logs at Warning level with the error message and correlation context.


### TEST-MCP-028

Given a CQRS command handler that throws an exception, when the Dispatcher catches it, then `Result<T>.IsFailure` is true, `Result<T>.Exception` is set, and the Dispatcher logs at Error level with exception details.


### TEST-MCP-029

Given a new `CorrelationId`, when `Next()` is called multiple times, then the base ID remains stable and the counter increments sequentially. Given a correlation string `"12345678.3"`, when `CorrelationId.Parse` is called, then `BaseId` is `12345678` and the counter is `3`.


### TEST-MCP-030

Given a `CallContext` used as `ILogger`, when `LogInformation` is called, then the log entry is captured in `CallContext.LogEntries` with the correct level, message, and timestamp. Given the Dispatcher's `ILoggerProvider`, when a `DispatcherLogger` emits a log with a correlation scope, then the structured output includes `correlationBaseId` and `correlationStep` as separate fields.


### TEST-MCP-031

Given two pipeline behaviors registered in order, when a command is dispatched, then the first behavior's pre-processing runs before the second, and the second's post-processing runs before the first's. Given a behavior that returns `Result.Failure` without calling `next`, then the handler is never invoked.


### TEST-MCP-032

Given an empty database, when `AgentService.SeedBuiltInDefaultsAsync` is called, then 7 built-in agent definitions are created. Given a database already containing built-in agents, when `SeedBuiltInDefaultsAsync` is called again, then no duplicates are created (idempotent).


### TEST-MCP-033

Given the AgentService, when `UpsertDefinitionAsync` creates a new definition and `GetDefinitionAsync` retrieves it, then all fields match. When `DeleteDefinitionAsync` is called on a non-built-in definition, then it succeeds. When called on a built-in definition, then it returns `IsFailure` with an appropriate error.


### TEST-MCP-034

Given a workspace with an agent configured, when `BanAgentAsync` is called with `Global = false`, then only that workspace's agent is banned. When called with `Global = true`, then all workspaces with that agent are banned. When `UnbanAgentAsync` is called, then the agent is re-enabled.


### TEST-MCP-035

Given a Director `LoginHandler`, when the device authorization flow completes successfully, then `Result<LoginResult>.IsSuccess` is true and the token is cached. When the flow times out, then `Result<LoginResult>.IsFailure` with an appropriate error.


### TEST-MCP-036

Given a Director `LaunchAgentHandler`, when the agent is enabled and not banned, then the agent process is spawned and `Result<LaunchResult>.IsSuccess`. When the agent is banned, then `Result<LaunchResult>.IsFailure` with a ban reason.


### TEST-MCP-037

Given a Director `InitWorkspaceHandler`, when called on a valid workspace path, then `agents.yaml` is created and agents are registered via the MCP Server API. When the workspace path doesn't exist, then `Result.IsFailure`.


### TEST-MCP-038

Given a Director `BanAgentHandler`, when called with a valid agent ID and reason, then the MCP Server API is called to ban the agent and a Ban event is logged. When the agent doesn't exist, then `Result.IsFailure`.


### TEST-MCP-039

Given seeded canonical requirements docs, when `RequirementsDocumentService` loads them, then FR/TR/TEST/mapping entries are parsed correctly and generated Markdown preserves the canonical header and entry formats.


### TEST-MCP-040

Given `/mcpserver/requirements` CRUD endpoints, when an FR entry is created, generated via `/mcpserver/requirements/generate?doc=functional`, and deleted, then the generated document reflects each mutation and the deleted entry is no longer returned.


### TEST-MCP-041

Given /mcpserver/requirements/generate?doc=all, when the endpoint is called, then it writes Functional-Requirements.md, Technical-Requirements.md, Testing-Requirements.md, TR-per-FR-Mapping.md, and Requirements-Matrix.md to the workspace, preserves existing matrix rows, appends missing FR/TR/TEST IDs, and returns export metadata.


### TEST-MCP-042

Given concurrent requirement mutations, when `RequirementsDocumentService` persists updates, then writes remain atomic and the resulting Markdown files remain parseable without temp-file residue.


### TEST-MCP-043

Given MCP STDIO requirements tools (`requirements_list`, `requirements_generate`, `requirements_create`, `requirements_update`, `requirements_delete`), when agents invoke them, then results are semantically equivalent to the corresponding REST requirements endpoints.


### TEST-MCP-044

Given the three-tier workspace resolution chain, when `X-Workspace-Path` header is present, then it takes priority over API key reverse lookup; when absent, API key resolves workspace; when neither is present, the default workspace is used.


### TEST-MCP-045

Given EF Core global query filter on `WorkspaceId`, when entities are inserted for workspace A and queried from workspace B context, then workspace A's entities are not visible from workspace B.


### TEST-MCP-046

Given the Director client with `WorkspacePath` set, when API calls are made, then the `X-Workspace-Path` header is sent on all requests and workspace switching only changes the header, not the base URL.


### TEST-MCP-047

Given the typed client library with `McpServerClientOptions.WorkspacePath` set, when requests are sent, then the `X-Workspace-Path` header is present alongside `X-Api-Key`.


### TEST-MCP-048

Given a TODO item in workspace A, when `POST /mcpserver/todo/{id}/move` is called with `targetWorkspacePath` pointing to workspace B, then the item is created in workspace B with all fields preserved and deleted from workspace A. Given an invalid target workspace path, then the endpoint returns 400.


### TEST-MCP-049

Given voice conversation endpoints, when a session is created with a `DeviceId`, then `GET /mcpserver/voice/session?deviceId=` returns the active session. When `DELETE` is called, then the session is destroyed and subsequent status queries return 404.


### TEST-MCP-050

Given an active voice session, when `POST /mcpserver/voice/session/{id}/turn` is called with transcript text, then a `VoiceTurnResponse` is returned with assistant text. When `POST /mcpserver/voice/session/{id}/turn/stream` is called, then SSE events are streamed with `type` values of `chunk`, `tool_status`, `done`, or `error`.


### TEST-MCP-051

Given a voice session with `SessionIdleTimeoutMinutes` configured, when the session is idle beyond the timeout, then the `IdleShutdownCommand` is sent and the session is terminated. Given a device with an active session, when a new session is requested for the same device, then the existing session is returned (one-per-device enforcement).


### TEST-MCP-052

Given a Windows service running as LocalSystem, when `DesktopProcessLauncher.LaunchWithStdio` is called, then a process is created on the interactive desktop with redirected stdio pipes via `CreateProcessAsUser`. Given `LaunchVisible`, then a visible console window is created.


### TEST-MCP-053

Given an `appsettings.yaml` file with configuration overrides, when the server starts, then YAML values override `appsettings.json` values for matching keys. Given the YAML file is absent, then startup succeeds using JSON configuration only.


### TEST-MCP-054

Given the template service with a YAML file, when `GET /mcpserver/templates` is called with optional `category`, `tag`, and `keyword` query parameters, then a filtered list of `PromptTemplate` items is returned. When no filters are provided, all templates are returned.


### TEST-MCP-055

Given a valid `PromptTemplateCreateRequest`, when `POST /mcpserver/templates` is called, then a new template is persisted to YAML and returned with 201 Created. When a duplicate ID is submitted, then 409 Conflict is returned. When `PUT /mcpserver/templates/{id}` is called with partial update fields, only specified fields are changed. When `DELETE /mcpserver/templates/{id}` is called, the template is removed.


### TEST-MCP-056

Given a stored template with Handlebars content and declared variables, when `POST /mcpserver/templates/{id}/test` is called with variable values, then `PromptTemplateTestResult` contains the rendered content. When required variables are missing, then `MissingVariables` is populated and `Success` is false. When `POST /mcpserver/templates/test` is called with inline template content, the inline content is rendered without requiring a stored template.


### TEST-MCP-057

Given the Director TUI with a Templates tab, when `TemplateListViewModel.RefreshCommand` executes, then the CQRS `ListTemplatesQuery` flows through `ListTemplatesQueryHandler` → `ITemplateApiClient` → `TemplateClient` → REST API and populates the table view. When a template is selected and the test action is invoked via `TemplateDetailViewModel`, then `TestTemplateQuery` renders the template and displays output.


### TEST-MCP-058

Given `FileMarkerPromptProvider` with a valid `templates/default-marker-prompt.hbs.yaml` file, when `GetGlobalPromptTemplateAsync` is called, then the template content is returned and cached. When the file is missing, then `null` is returned and `MarkerFileService.DefaultPromptTemplate` is used as fallback.


### TEST-MCP-059

Given `TodoPromptProvider` with todo prompt templates stored in `IPromptTemplateService` by well-known IDs (`todo-status-prompt`, `todo-implement-prompt`, `todo-plan-prompt`), when the provider is queried, then file-loaded content is returned. When templates are missing from the store, then `TodoPromptDefaults` built-in constants are returned as fallback.


### TEST-MCP-060

Given `PairingHtmlRenderer` with pairing HTML templates stored in `IPromptTemplateService` by well-known IDs (`pairing-login-page`, `pairing-key-page`, `pairing-not-configured-page`), when rendering methods are called with substitution parameters, then `{errorBanner}`, `{apiKey}`, and `{serverUrl}` tokens are replaced. When templates are missing, then `PairingHtml` static method output is returned as fallback.


### TEST-MCP-061

Given `Mcp:AgentPool:Agents` configuration, when options bind and validate, then each definition accepts `AgentName`, `AgentPath`, `AgentModel`, `AgentSeed`, `AgentParameters`, `IsInteractiveDefault`, `IsTodoPlanDefault`, `IsTodoStatusDefault`, and `IsTodoImplementDefault`. Duplicate `AgentName` values (case-insensitive) or ambiguous defaults fail validation.


### TEST-MCP-062

Given one-shot or interactive requests with no `AgentName`, when context is `Plan`, `Status`, `Implement`, or `AdHoc`, then the default agent mapped for that intent is selected. Given an explicit `AgentName`, explicit assignment overrides default routing.


### TEST-MCP-063

Given one-shot request payloads, when both `promptTemplateId` and ad-hoc `promptText` are supplied or both are missing without resolvable context template, then the API returns 400. Given template-resolved mode, missing `id` returns 400.


### TEST-MCP-064

Given one-shot context without template ID, when context is `Plan`, `Status`, or `Implement`, then existing context-based template resolution is used. Given context `AdHoc` without template ID and no ad-hoc prompt text, then the API returns 400.


### TEST-MCP-065

Given prompt resolution requests with template ID, caller values, workspace-context values, and `id`, when rendering executes, then output includes `{id}` substitution and caller values override workspace-context values on key conflicts.


### TEST-MCP-066

Given no eligible pooled agent is idle, when one-shot requests are enqueued, then requests remain `queued` and transition to `processing` once an eligible agent becomes available, followed by terminal states (`completed`, `failed`, or `canceled`).


### TEST-MCP-067

Given queue operations, when move up/down is requested for queued items, then order changes correctly; when requested for the currently processing item, the operation is rejected. Cancel/remove semantics correctly update queue state and persisted metadata.


### TEST-MCP-068

Given pool lifecycle transitions, when queued/processing/completed/failed events occur, then notification SSE emits events in order with payload fields `AgentName`, `LastRequestPrompt`, and `SessionId`.


### TEST-MCP-069

Given multiple clients connected to a read-only response stream, when one client disconnects, then remaining subscribers continue receiving stream data and active pooled work is unaffected.


### TEST-MCP-070

Given a pooled agent processing a one-shot request, when an interactive voice connection targets that agent, then interactive linkage is established without canceling or reassigning the one-shot operation.


### TEST-MCP-071

Given an interactive stream connection, when the client disconnects, then `User is AFK.` is sent to the agent. When the client reconnects and stream establishment completes, then `User is here.` is sent. These messages are not sent for one-shot sessions.


### TEST-MCP-072

Given Director Agent Pool tab actions (connect, recycle, stop/start, queue move up/down, cancel/remove, free-form enqueue), when invoked from UI commands, then the correct REST endpoints are called and UI state refreshes from server snapshots and notifications.


### TEST-MCP-073

Given `McpServer.Support.Mcp` stateful services and registries, when architecture validation runs, then each authoritative data source is DI-owned (`singleton`/`scoped`), no stateful service is created outside DI, change notifications use `INotifyPropertyChanged`, and consumers pull current state from the source-of-truth service.


### TEST-MCP-074

Given TODO create/update and session log submit/append requests, when IDs violate canonical naming formats (`TODO persisted ids: ^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+-\d{3}$ or ^ISSUE-\d+$`, `sessionId: <Agent>-<yyyyMMddTHHmmssZ>-<suffix>`, `requestId: req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`), then APIs reject with validation errors and no data mutation occurs; valid IDs are accepted across YAML and SQLite TODO backends.


### TEST-MCP-075

Given `ChannelChangeEventBus`, when events are published with zero, one, or multiple subscribers, then publish does not throw and each active subscriber receives events independently; canceled subscriptions stop enumeration.


### TEST-MCP-076

Given TODO, session log, and repo mutation services, when create/update/delete-style operations succeed, then each service publishes one change event with the expected category/action/entityId values.


### TEST-MCP-077

Given extended mutation services (`ToolRegistryService`, `WorkspaceService`, `AgentService`), when representative create/update operations succeed, then each service publishes the expected category/action event.


### TEST-MCP-078

Given `GET /mcpserver/events`, when a client subscribes, then the response content type is `text/event-stream`.


### TEST-MCP-079

Given `GET /mcpserver/events?category=todo`, when a TODO change event is published, then the stream includes an `event: todo` payload containing the matching entity ID.


### TEST-MCP-080

Given category filtering on `/mcpserver/events`, when non-matching categories are published, then filtered subscribers do not receive those non-matching domain events.


### TEST-MCP-081

Given workspace-scoped GitHub auth endpoints, when a token is set via `PUT /mcpserver/gh/auth/token`, then `GET /mcpserver/gh/auth/status` reports `hasStoredToken=true`; when `DELETE /mcpserver/gh/auth/token` is called, the token is removed.


### TEST-MCP-082

Given GitHub OAuth bootstrap endpoints, when `GET /mcpserver/gh/oauth/config` is called, then effective configuration fields are returned; when OAuth is not fully configured, `GET /mcpserver/gh/oauth/authorize-url` returns 400 with a clear error.


### TEST-MCP-083

Given `GitHubCliService` with a stored workspace token, when GitHub commands are executed, then `IProcessRunner` receives a `ProcessRunRequest` containing `GitHubTokenOverride`; when no token exists and fallback is enabled, standard CLI execution is used.


### TEST-MCP-084

Given GitHub Actions workflow operations, when list/detail/rerun/cancel paths are invoked, then gh CLI commands and REST/client contracts for `/mcpserver/gh/actions/runs*` remain consistent and parse expected run/job/step metadata.


### TEST-MCP-085

Given natural-language workspace policy directives, when `POST /mcpserver/workspace/policy` or `workspace_policy_apply` is invoked with valid directives, then targeted workspace ban lists are mutated and invalid directives return structured 400 errors.


### TEST-MCP-086

Given audited copilot decoration, when invoke and streaming operations execute, then session-log submissions include `copilot_invocation` actions and completed status records.


### TEST-MCP-087

Given ingestion options and marker prompt generation for this repository, when host configuration post-processing runs, then repo-local src/McpServer.Cqrs and src/McpServer.Cqrs.Mvvm glob patterns are enforced, and marker output includes the Available Capabilities section without McpServer.UI.Core or McpServer.Director entries.


### TEST-MCP-088

Given direct website URL ingestion requests, when `POST /mcpserver/context/ingest-website` or `context_ingest_website` runs, then valid HTTP/HTTPS pages ingest as `external-web` sources, URL outcomes are returned, SSRF/private/link-local targets are blocked, redirects are bounded, and source dedup/update behavior is preserved by source key.


### TEST-MCP-089

Given a .NET 9 host application that registers the hosted Microsoft Agent Framework library against an MCP Server workspace, when the built-in agent workflow runs, then session log turns are created/updated through canonical identifiers, TODO plan/status/implementation operations execute through the existing MCP Server contracts, repository read/list/write tools browse repo-relative paths without host-specific glue code, local desktop process launch reuses the authenticated workspace desktop-launch contract, `mcp_powershell_session_*` tools execute commands inside a persistent in-process PowerShell session hosted by the agent itself, and host applications can drive the same local runspace interactively through `IMcpHostedAgent.PowerShellSessions`.


### TEST-MCP-090

Given representative controller and middleware failure paths across the server, when an unhandled exception produces HTTP 500, then the response body contains a non-empty detailed error description for the failed operation, excludes secrets and raw stack traces, and remains consistent across endpoints through the shared error-handling path.


### TEST-MCP-091

Given the admin configuration management surface, when configuration values are read or patched through the configuration controller and YAML helper, then effective settings are exposed as flattened key-value pairs, submitted YAML-backed keys are persisted and reloaded, and standard JWT Bearer admin authorization keeps the endpoints unavailable when OIDC is disabled.


### TEST-MCP-092

Given a TODO create request with id `ISSUE-NEW`, when GitHub issue creation succeeds, then the server persists the TODO using the canonical `ISSUE-{number}` id returned by GitHub, includes GitHub correlation metadata in the TODO note, and returns the canonical id from the create surface instead of the temporary alias.


### TEST-MCP-093

Given workspace-scoped GitHub CLI execution, when gh commands run with either a stored workspace token or fallback authentication, then the process runner receives the resolved workspace root as the working directory.


### TEST-MCP-094

Given an existing `ISSUE-{number}` TODO updated through any server TODO update surface, when the local update succeeds, then the server preserves the existing description, syncs title/state/priority metadata back to GitHub using canonical `priority: HIGH|MEDIUM|LOW` labels, and posts a GitHub issue comment describing the applied change set.


### TEST-MCP-095

Given an `ISSUE-NEW` TODO created through the HTTP API, when GitHub-origin comments are added and GitHub-to-TODO sync runs, then the TODO note gains the generated GitHub comment section without altering the TODO description. When the TODO priority changes and a TODO-authored note comment is appended, then the GitHub issue receives the canonical updated priority label and a GitHub comment containing the appended note text. When the GitHub issue is later closed externally and GitHub-to-TODO sync runs again, then the TODO is marked done.


### TEST-MCP-096

Given an empty authoritative SQLite TODO store and an existing `TODO.yaml`, when initialization runs and later authoritative mutations project back to YAML, then bootstrap import, deterministic projection ordering, projection-only YAML behavior, and preservation of `notes`, `completed`, and `code-review-remediation` metadata are all verified by automated tests.


### TEST-MCP-097

Given a TODO item mutated through create/update/delete and queried through storage, REST, typed client, and integration surfaces, when audit history is requested, then append-only ordered states, delete-history retention, not-found behavior, and explicit projection-failure classification are all verified by automated tests.


### TEST-MCP-098

Given a Parseable-bound log event with more than 250 structured properties and user properties that collide with reserved Parseable field names, when `ParseableEventFormatter` serializes the event, then the payload contains at most 250 top-level fields, canonical reserved metadata is preserved, and overflow non-reserved properties are deterministically omitted.


### TEST-MCP-099

Given the repository Azure DevOps pipeline definition, when tracked paths change on `main` or `develop` or through a pull request, then `azure-pipelines.yml` triggers the core CI workflow, runs config validation/build/test/docs/MSIX/package jobs with the documented branch and variable gates, and skips optional feed/docs publication safely when the required Azure DevOps variables are absent.


### TEST-MCP-100

Given a PowerShell `McpSession` workspace with an active session cached in `.mcpSession/current-session.json`, when the legacy `.mcpServer/session.yaml` wrapper is missing and the module is reinitialized or resolves a session without an explicit `Session` argument, then it reuses the cached current session object and session ID. When the session is completed, both cache files are removed.


### TEST-MCP-101

Given trust-bootstrap marker rendering and the public PowerShell bootstrap modules, when the marker signature is valid and `/health` echoes the submitted nonce exactly, then `McpSession`, `McpTodo`, and `McpContext` initialize successfully and proceed with MCP usage. When the signature is invalid or the nonce does not match, then each bootstrap module emits `MCP_UNTRUSTED`, does not probe additional endpoints, and aborts MCP usage before session-log or TODO traffic continues.


### TEST-MCP-102

Given the provider-factory, native encryption, and maintenance-command workstreams, when SQLite, PostgreSQL, and SQL Server are configured for clean-database integration runs, then provider-specific migrations apply successfully and the live encryption state matches the configured state. When encryption is enabled or disabled on an existing database, then the provider-specific transition workflow shall expose a no-data-loss maintenance procedure and dry-run plan before mutation, with automated dry-run tests covering SQLite SEE, PostgreSQL `pg_tde`, and SQL Server TDE command generation. SQL Server provider and migration integration coverage shall use self-managed SQL Server LocalDB instances that are created and torn down by the test harness, while SQL Server TDE validation shall run against a separate non-LocalDB SQL Server target because LocalDB cannot validate TDE.


### TEST-MCP-103

Given a Byrd execution TODO, when unit tests are not defined, then the service rejects transition to `Implementing`; when unit tests are defined through the test-plan API, then the TODO advances to `TestReady`.


### TEST-MCP-104

Given a Byrd execution TODO linked to requirements, session turns, and modified files, when bounded execution context or checkpoint delta context is requested, then the server returns only concise snippets, recent turn summaries, relevant files, artifacts, commits, and updated next action for that TODO.


### TEST-MCP-105

Given the Byrd execution REST controller, STDIO MCP tools, typed client, and `adb_step` surface, when representative phase creation, active TODO lookup, status progression, and screenshot validation calls are executed, then structured contracts remain stable and Android validation results are returned without arbitrary shell passthrough.


### TEST-MCP-106

Given requirements export with doc=all and format=wiki, when generation runs, then docs/Project/wiki contains both azure/ and github/ folders, each manifest includes generatedAtUtc, Azure includes `.order`, and GitHub includes `_Sidebar.md` and `_Footer.md`.


### TEST-MCP-107

Given wiki ingest with Azure and GitHub document folders, when manifest and file modified timestamps identify a newer source, then import selects that source; when the two checks disagree, import fails unless preferredWikiFormat is supplied.


### TEST-MCP-108

Given the REPL requirements workflow, when wiki export or import is invoked, then export returns format, docType, generatedAtUtc, outputRoot, and written file metadata, and import accepts path-keyed documents with per-document timestamps.


### TEST-MCP-109

Given Codex, Claude Code, Copilot, and Cline agent plugins, when requirements wiki workflows are used, then each plugin exposes the wiki requirements contract and routes generate/ingest envelopes without expecting archive bytes.


### TEST-MCP-110

Question CRUD (happy, validation, 404).


### TEST-MCP-111

Answer CRUD (orphan rejection on deleted question, cascade delete).


### TEST-MCP-112

Accept-answer flow (single-accept invariant, un-accept clears).


### TEST-MCP-113

Tag filter AND-semantics + empty-result case.


### TEST-MCP-114

Vote increment / decrement on Question and Answer; concurrency atomicity.


### TEST-MCP-115

Comment thread create / list / delete with depth-cap enforcement.


### TEST-MCP-116

FAQ endpoint projection shape, ordering, deeplink format.


### TEST-MCP-117

Search: created question/answer text is found via `IContextSearchService`; removed on delete.


### TEST-MCP-118

Author resolution precedence (body > API key > JWT > anonymous-rejected).


### TEST-MCP-119

Workspace isolation (mirror `EfTodoService_WorkspaceIsolationTests`).


### TEST-MCP-120

MCP STDIO tool parity for each REST endpoint.


### TEST-MCP-121

`QaClient` end-to-end against `CustomWebApplicationFactory`.


### TEST-MCP-122

`QaWorkflow` unit tests in `tests/McpServer.Repl.Core.Tests` (NSubstitute over `QaClient`); REPL agent-stdio integration test in `tests/McpServer.Repl.IntegrationTests` that spawns the host with `ReplChildProcessHelper`, sends a `workflow.qa.*` YAML envelope, asserts the response shape.


### TEST-MCP-123

PowerShell module Pester tests (if a `tools/powershell/tests/` pattern exists, otherwise smoke-script invoked from `./build.ps1 Test` or a new `ValidatePowerShell` target).


### TEST-MCP-124

Skill smoke test: each new `qa/SKILL.md` is loaded by the plugin packager and its frontmatter passes the standard skill validation script in each plugin repo.


### TEST-MCP-125

Audit emission tests: one audit row per mutation, correct `Action`, `Version` monotonic per `(EntityKind, EntityId)`, `Actor` populated via `IQaAuthorResolver`, `SnapshotJson` round-trips.


### TEST-MCP-126

Audit query tests: paging contract, filter combinations, empty-result case, workspace isolation (audits from workspace A invisible to workspace B).


### TEST-MCP-127

Vote audit: an `UPDATE ... SET VoteCount = VoteCount + @delta` plus an audit row are emitted in a single transaction (both succeed or both rollback). Audit row's `Actor` is populated from `IQaAuthorResolver`, so voter identity is captured per event.


### TEST-MCP-128

Answer-with-sources: round-trip `CreateAnswerRequest.Sources` -> `AnswerEntity.SourcesJson` -> `AnswerDto.Sources`; FAQ projection includes the sources array; deletion of an answer hard-deletes the sources via the existing cascade.


### TEST-MCP-129

Skill mandate text test: each sibling-plugin `skills/qa/SKILL.md` contains the exact mandatory rule block (regex match on the callout) and the `sources[]` schema example. Validation is a small PowerShell or `dotnet test` content-check (`tests/McpServer.Qa.Validation/SkillMandateTests.cs` or a `tools/plugin-skill-check.ps1` invoked from `./build.ps1 Test`).


### TEST-MCP-130

Close / duplicate flow tests: close-with-reason, reopen, mark-as-duplicate (canonical link both ways), FAQ excludes closed by default, FAQ surfaces duplicate redirect when requested with `?includeClosed=true`, audit rows captured per transition.


### TEST-MCP-131

Sanitization test corpus: XSS-payload corpus validates that every common attack vector is stripped on Question/Answer/Comment write; `bodyHtml` contains only allow-listed tags/attributes; raw `body` is preserved verbatim; round-trip Markdown -> HTML matches snapshot.


### TEST-MCP-132

FAQ wiki page generation test: build target produces deterministic Markdown matching the snapshot fixture, wiki index files updated, generated page renders cleanly in both Azure DevOps and GitHub wiki conventions (e.g., `_Sidebar.md` / `.order` references present).


### TEST-MCP-133

Voter-history endpoint: posting N votes from M distinct actors produces N audit rows; `GET /questions/{id}/voters` returns exactly those rows projected to `{ actor, action, createdAt }`; same for answers; workspace isolation enforced.


### TEST-MCP-134

One-vote-per-user enforcement: same actor posts vote_up twice -> second call is no-op (no counter change, no second audit row); actor posts vote_up then vote_down -> counter delta is -2, audit row recorded with action `vote_change`; actor revokes vote -> counter delta is -1, audit row `vote_revoke`; unique index prevents duplicate `QaVoteEntity` rows under concurrent calls (test with parallel writes against in-memory SQLite using `Task.WhenAll`).


### TEST-MCP-135

Current vote state endpoint: `GET /questions/{id}/votes` returns one row per active voter from `QaVoteEntity` after a sequence of apply / change / revoke calls; revoked voters do not appear; workspace isolation enforced.


### TEST-MCP-136

Test local read/write/restart during outage, version commit publication, project isolation and reviewed convergence using TEST-MCP-222..237. Retain explicit DirectProxy loop-protection and route tests. Replace LocalProxy hub-authority, queued-write and read-switch assertions for Dolt mode.

**Acceptance Criteria:**
- [ ] Given a local commit during outage, when reconnect runs, then history is exchanged without replaying the original command.

### TEST-MCP-137

Given templates/prompt-templates.yaml, when the marker-template contract tests run, then default-marker-prompt contains the frontier-to-implementation planning guidance, explicit requirements capture guidance, and TDD unit-test planning guidance.


### TEST-MCP-138

Unit and provider tests must fail red until WorkspaceService is database-authoritative and DbFkContractTests prove every WorkspaceId entity has a canonical Workspaces FK with non-cascade delete behavior. Non-memory FKs are required; MemoryEntity and MemoryIndexEntity use optional FKs because Global scope stores null WorkspaceId. Provider tests must prove the same physical constraints.

**Acceptance Criteria:**
- [ ] DbFkContractTests fails when a non-memory WorkspaceId entity lacks a required non-cascade Workspaces FK.
- [ ] Memory relationship tests prove optional Workspaces FKs accept Global null, reject orphan non-null workspace IDs, and preserve parent/index ownership consistency.
- [ ] SQLite, PostgreSQL, and SQL Server migrated physical schemas expose the expected FK nullability and delete action with existing rows preserved.

### TEST-MCP-139

Unit tests must fail red until persistent delete paths preserve rows through soft-delete metadata and every mutable entity writes DataAuditLog rows for create, update, and soft-delete operations.

**Acceptance Criteria:**
- [ ] A negative-control model with Cascade fails the memory relationship contract; the real EF model uses Restrict or NoAction.
- [ ] Provider tests soft-delete a memory and prove its version and index rows remain queryable for audit and recovery.
- [ ] Migrated SQLite, PostgreSQL, and SQL Server schemas have non-cascade MemoryId FKs and preserve seeded row counts.

### TEST-MCP-140

Unit and provider tests must fail red until TODO requirement links and requirement traceability links enforce FKs, missing requirements are backfilled, and SQLite, SQL Server, and PostgreSQL migrations preserve data.

**Acceptance Criteria:**
- [ ] Unit tests fail when TODO requirement links omit a durable TODO or Requirements FK.
- [ ] Unit tests fail when FR/TR/TEST traceability links omit source or target Requirements FKs.
- [ ] Provider migrations backfill missing requirement anchors and preserve existing TODO, requirement, and traceability rows.
- [ ] SQLite, SQL Server, and PostgreSQL migrations enforce the physical FKs without data loss.

### TEST-MCP-141

Add or update a documentation contract test proving docs/Development-Process-draft-v3.md captures the plan creation requirements for decision-complete frontier-model handoff plans, FR/TR/TEST traceability, TDD-first red/green behavior, and zero-failure zero-skip Byrd gates.


### TEST-MCP-142

Bats coverage must prove workflow.requirements.updateFr, updateTr, and updateTest accept priority changes and do not fail inside the Codex plugin wrapper.


### TEST-MCP-143

Validate that outstanding-session consolidation creates MCP-backed requirements and TODO traceability, inventories dirty workspaces, preserves unrelated changes, blocks unsafe deploys, and records zero-failure zero-skip validation gates before completion.

**Acceptance Criteria:**
- [ ] Build.Tests red cases verify a fresh run with a unique run ID accepts exactly the Nuke-emitted selected-project inventory and one fresh per-project TRX for every expected unit and provider project.
- [ ] Build.Tests red cases independently reject a missing expected project report and a duplicate project report.
- [ ] Build.Tests red cases reject stale reports from a prior run and reports whose run-start, source-file manifest, tool-version manifest, or candidate-source hashes do not match the current candidate.
- [ ] Build.Tests red cases reject total zero, zero discovery, executed less than total, failed greater than zero, skipped greater than zero, and notExecuted greater than zero.
- [ ] Build.Tests red cases reject Pester failed blocks or containers even when individual test counters appear green.
- [ ] Build.Tests red cases prove an earlier Pester, Nuke Test, or Build.Tests command failure blocks the gate even when a later command exits zero.
- [ ] Proposed concrete cases include SessionLifeUnitGateTests.Validator_AcceptsFreshRunInventoryAndCompleteReports, RejectsMissingExpectedProjectReport, RejectsDuplicateProjectReport, RejectsStaleReportFromPriorRun, RejectsZeroDiscoveryOrEmptyReport, RejectsSkippedOrNotExecutedTests, RejectsPesterFailedBlocksOrContainers, RejectsCandidateSourceManifestDrift, and RejectsEarlierCommandFailureEvenWhenLaterCommandSucceeds; names remain proposed until the P1 red-test review accepts them.

### TEST-MCP-144

Given a TODO description containing Markdown headings, lists, code fences, blank lines, leading indentation, and trailing content, create, update, read, audit, and projection paths preserve the exact meaningful formatting with zero failures and zero skips.


### TEST-MCP-145

Automated tests shall verify client request serialization, controller mixed-batch acceptance and whole-batch rejection, repository transaction rollback, and REPL schema validation for requirements batch commands.


### TEST-MCP-146

Plugin validation shall include shell syntax checks, Cline bridge JSON-stdio tests, Cline schema preflight tests, and JSON Schema parse checks for the published REPL request schema.


### TEST-MCP-147

Given agent-facing documentation, marker templates, pipeline references, and generated requirements wiki outputs, automated tests shall verify single-line JSON stdio guidance, current plugin registry guidance, existing pipeline file references, and Azure/GitHub wiki output file parity.


### TEST-MCP-148

Build.Tests SHALL verify build.ps1 relaunches through a PowerShell host with -NoLogo, -NoProfile, and -NonInteractive, Build.Tests pwsh.exe helpers include those flags, and live deployment guidance examples include those flags.


### TEST-MCP-149

Bats validation covers AC-SKILLS-001 through AC-SKILLS-006 for mcpserver-codex-plugin, mcpserver-claude-code-plugin, mcpserver-claude-cowork-plugin, mcpserver-copilot-plugin, and mcpserver-grok-plugin.

**Acceptance Criteria:**
- [x] Every target plugin contains non-empty SKILL.md files for sync-logs, commit-sync, and wrap-up. (evidence: Codex manifest Bats, Copilot skills Bats, and full Claude Code, Cowork, and Grok skills Bats passed.)
- [x] Every new skill has YAML frontmatter with name and description. (evidence: Shell Bats workflow skill frontmatter assertions passed.)
- [x] sync-logs documents status check, session/turn handling, dialog/action appends, background-session discovery, factual summary, and no raw REST. (evidence: Shell Bats sync-logs AC-SKILLS-003 assertions passed.)
- [x] commit-sync documents pause, repo-scope report, explicit acknowledgement, full dirty-tree staging, commit SHA capture, push result, and no force/rewrite behavior. (evidence: Shell Bats commit-sync AC-SKILLS-004 assertions passed.)
- [x] wrap-up documents marker trust, requirement reconciliation, wiki export, validation, commit/push, session-log reconciliation, and turn completion/failure. (evidence: Shell Bats wrap-up AC-SKILLS-005 assertions passed.)
- [x] Manifests or package files expose/package the new skills using each plugin's existing convention. (evidence: Codex/Claude/Grok skillsPath, Copilot skill entries, and Cowork skills directory convention validated.)

### TEST-MCP-150

Jest validation covers AC-SKILLS-001 through AC-SKILLS-006 for mcpserver-cline-plugin, mcpserver-cline-v2-plugin, and mcpserver-opencode-plugin.

**Acceptance Criteria:**
- [x] Every target plugin contains non-empty SKILL.md files for sync-logs, commit-sync, and wrap-up. (evidence: Cline, Cline v2, and OpenCode skills Jest tests passed.)
- [x] Every new skill has YAML frontmatter with name and description. (evidence: TypeScript skills Jest frontmatter assertions passed.)
- [x] sync-logs documents status check, session/turn handling, dialog/action appends, background-session discovery, factual summary, and no raw REST. (evidence: TypeScript sync-logs AC-SKILLS-003 assertions passed.)
- [x] commit-sync documents pause, repo-scope report, explicit acknowledgement, full dirty-tree staging, commit SHA capture, push result, and no force/rewrite behavior. (evidence: TypeScript commit-sync AC-SKILLS-004 assertions passed.)
- [x] wrap-up documents marker trust, requirement reconciliation, wiki export, validation, commit/push, session-log reconciliation, and turn completion/failure. (evidence: TypeScript wrap-up AC-SKILLS-005 assertions passed.)
- [x] package.json exposes or packages the skills directory using the plugin's existing distribution convention. (evidence: package.json files include skills/ and npm build/test passed for all TypeScript plugins.)

### TEST-MCP-151

Process validation covers red/green/final evidence and the zero-failure zero-skip focused validation gate for the workflow-skill rollout.

**Acceptance Criteria:**
- [x] Validation tests fail before the skills/manifest changes and pass after implementation, with red and green command output captured in session-log actions. (evidence: Red gate failures and green/final passing commands were run and recorded in session-log actions.)
- [x] Final validation has zero failures and zero skips in the executed focused scope. (evidence: Final Bats/Jest output reported all ok/pass and no skipped tests; git diff --check passed across all modified repos.)

### TEST-MCP-152

Unit and integration validation shall prove generated requirements exports render TEST descriptions readably and display acceptance criteria as a bulleted list for both GitHub and Azure wiki outputs.

**Acceptance Criteria:**
- [x] Focused tests fail before the renderer change because wiki testing output still uses dense table rows for descriptions. (evidence: Red run of RequirementsDocumentServiceTests failed on missing TEST-MCP-001 subsection before renderer update.)
- [x] Focused tests pass after the renderer change and assert AC bullets in both GitHub and Azure wiki outputs. (evidence: Focused service test run passed 10 of 10; combined requirements service and AC run passed 15 of 15.)
- [x] Final validation has zero failures and zero skips in the executed scope. (evidence: build Compile succeeded; focused RequirementsDocumentService and RequirementAcceptanceCriteria tests passed 15 of 15; RequirementsControllerTests passed 7 of 7; no skipped tests were reported.)

### TEST-MCP-153

Regression tests SHALL verify all plugin batch requirement methods accept unindented YAML records, indented YAML records, and inline JSON-array records while preserving nested acceptanceCriteria arrays and boolean isSatisfied fields.


### TEST-MCP-154

Given a TODO update payload that changes implementationTasks[].done and omits top-level done, when the plugin wrapper builds the TODO update HTTP body, then the body preserves nested task done values and omits parent done so the server cannot accidentally complete the TODO.

**Acceptance Criteria:**
- [x] Regression test fails if nested implementation task completion is promoted to top-level done. (evidence: Bats output reported ok 1 TODO HTTP body does not promote implementation task done to parent done in all five plugin repositories.)

### TEST-MCP-155

Given mocked successful and failed session/compact hook dependencies, when SessionStart, SessionEnd, PreCompact, and PostCompact scripts run, then each status-only path emits {} and PostCompact emits no additionalContext.

**Acceptance Criteria:**
- [x] Hook regression tests assert exact {} output for session-start, session-end, pre-compact, and post-compact. (evidence: tests/hooks.bats passed in the affected plugin repositories with exact-output assertions.)
- [x] Copilot has hook regression coverage equivalent to the affected script surface. (evidence: Added F:\GitHub\mcpserver-copilot-plugin\tests\hooks.bats and passed 6/6.)

### TEST-MCP-156

Verify GitHubCliService passes command-scoped safe.directory environment variables for workspace-scoped calls and uses gh --repo without workspace cwd when a repository is configured.


### TEST-MCP-157

Verify the PowerShell wrapper returns a non-zero result with a timeout diagnostic when a plugin helper command hangs beyond TimeoutSeconds, while normal helper wrapper tests still pass.


### TEST-MCP-158

Keyserver trust and audit tests SHALL verify party registration, manifest signing, manifest verification, replay nonce rejection, stale sequence rejection, expiry rejection, disabled/unknown party or key rejection, key rotation descriptor preservation, signed manifest trace lookup/reporting, and audit persistence.

**Acceptance Criteria:**
- [x] `TransactionSecurityControllerTests`, `TransactionSecurityClientTests`, `DurableTransactionSecurityStorageTests`, and `SeparateTransactionServiceIntegrationTests` cover keyserver trust and audit behavior with zero skipped tests in the executed scope.

### TEST-MCP-159

Subscriber commit and abort tests SHALL verify manifest trust, protected diffgram decrypt/hash validation, idempotent duplicate commit, conflict rejection, abort semantics, transaction status reporting, key-ring rotation, durable status, and concurrent commit/replay behavior.

**Acceptance Criteria:**
- [x] `TransactionSecurityControllerTests`, `DurableTransactionSecurityStorageTests`, `SeparateTransactionServiceIntegrationTests`, and subscriber-focused transaction tests cover accepted, rejected, pending, committed, and aborted status paths.

### TEST-MCP-160

Real keyserver/subscriber integration tests SHALL validate the separate keyserver and subscriber hosts without mocks, including valid commit, tampered manifest, stale sequence, encrypted-body mismatch, subscriber key-ring configuration, and file-backed key provisioning.

**Acceptance Criteria:**
- [x] `SeparateTransactionServiceIntegrationTests` and durable transaction-security integration coverage pass with zero failures and zero skips.

### TEST-MCP-161

MCP transaction gating tests SHALL verify coordinator commit, degraded, rollback, durable timeout cancellation, and pub-sub handoff/replay/retention for QuadBrain brain-slot.invoke and brain-slot.weight-update (and other RequiresKeyserver operations). Generic REPL client protected-namespace policy remains in this TEST. First-party non-QuadBrain mutations (TODO, requirements, session-log including QBAgent, memory, repo, prompt templates, tool registry, GitHub, GraphRAG, voice, agent pool, ingest, context, federation apply/control) are governed by FR-MCP-173 / TEST-MCP-221 and SHALL persist without coordinator/keyserver; they are not fail-closed under this TEST. Covered by TurnTransactionCoordinatorTests, BrainSlotInvocationTransactionTests, QuadBrainOrchestrationServiceTests, and remaining TEST-MCP-168 pub-sub tests. Zero failures and zero skips in the executed QuadBrain/coordinator scope.

**Acceptance Criteria:**
- [ ] TurnTransactionCoordinatorTests prove ExecuteAsync signs and commits for brain-slot.invoke when Enabled and RequiredForMutations are true, and bypasses SignManifestAsync for todo.update.
- [ ] BrainSlotInvocationTransactionTests and QuadBrainOrchestrationServiceTests prove brain-slot.invoke and brain-slot.weight-update still hit ITurnTransactionCoordinator.
- [ ] Generic REPL client protected-namespace policy tests remain in this TEST. Non-QuadBrain TransactionGated adapter persist/bypass is TEST-MCP-221, not this TEST.

### TEST-MCP-162

Transaction traceability/import tests SHALL prove FR-MCP-118 through FR-MCP-128, transaction TR records, TEST-MCP-158 through TEST-MCP-173, and live TODO references resolve without placeholder transaction-plan entries.

**Acceptance Criteria:**
- [x] `TurnTransactionPlanArtifactTests.TransactionPlanRequirements_AreConcreteAndMapped` fails if transaction FR/TR/TEST records regress to placeholder backfills or lose matrix/mapping rows.

### TEST-MCP-163

Deferred-scope documentation tests SHALL prove remaining future autonomous Quad-Model branches, direct agent execution, desktop launch, tunnels, workspace/auth/server configuration, full remote/runtime compensation, complete delayed-rollback isolation, bucket/GitHub compensation, quarantine/fine-tuning automation, implicit fallback behavior, and full key-rotation lifecycle automation remain explicit future work rather than silently reported as complete.

**Acceptance Criteria:**
- [x] TurnTransactionPlanArtifactTests.PlanArtifacts_PreserveDeferredScopeAndDesignRounds validates deferred scope and design artifacts while distinguishing authorized Quad-Brain branches from remaining future scope. (evidence: tests/McpServer.Support.Mcp.Tests/Documentation/TurnTransactionPlanArtifactTests.cs)

### TEST-MCP-164

aiUnit plan review tests SHALL validate committed aiUnit run-log evidence for PLAN-TURNTRANSACTIONS-001 and fail on critical/high findings.

**Acceptance Criteria:**
- [x] PlanTransactionReviewTests validates the original committed aiUnit JSON run log for PLAN-TURNTRANSACTIONS-001, its reviewed scope, pass status, and absence of critical or high findings. (evidence: Git commit dd502e001f4ea9c4b7c175cee9d8c0b27b7b56cf blob 8f046a33caf8c0e04d74ac7f67628df5e86145e1; tests/McpServer.PlanReview.Tests/TestResults/original-runlog-green.trx (1 passed, 0 failed, 0 skipped))
- [x] The byte-identical original run log is copied as a test resource and read from test output; running the test does not use a repository path or write docs/reviews. (evidence: tests/McpServer.PlanReview.Tests/Fixtures/README.md; SHA-256 D826A458802C3252EEE88D898D633E610331506D186350D54E71573686B51878; original-runlog-green.trx)

### TEST-MCP-165

Imported diagram preservation tests SHALL validate all six imported Mermaid diagrams, stable IDs, imported source references, and repo annotations.

**Acceptance Criteria:**
- [x] `TurnTransactionPlanArtifactTests.ImportedPlan_PreservesAllSixMermaidDiagramIds` covers AD-TXN-001, AD-CURIOSITY-001, SD-DIFFGRAM-001, AD-AOT-001, AD-WEIGHT-001, and ARCH-QUAD-001.

### TEST-MCP-166

Keyserver manifest diagram tests SHALL derive coverage from SD-DIFFGRAM-001 signing, verification, invalid, and valid branches.

**Acceptance Criteria:**
- [x] Keyserver tests cover manifest sign/verify, hash validation, replay/sequence rejection, and signed manifest trace behavior.

### TEST-MCP-167

Subscriber diagram tests SHALL derive coverage from SD-DIFFGRAM-001 subscriber decrypt, hash, commit, reject, duplicate, abort, and status branches.

**Acceptance Criteria:**
- [x] Subscriber tests cover protected-envelope commit, old-key decrypt, rotated-key decrypt, abort, and status lifecycle behavior.

### TEST-MCP-168

End-to-end turn transaction diagram tests SHALL derive coverage from AD-TXN-001 commit, subscriber unavailable, degraded, and no-success-before-ack branches.

**Acceptance Criteria:**
- [x] Coordinator and transaction pub-sub tests cover signed manifest handoff, subscriber commit acknowledgement, timeout, degraded status, and durable replay.

### TEST-MCP-169

Degraded rollback reconciliation tests SHALL validate AD-TXN-001 and AD-AOT-001 in-scope rollback/audit behavior while requiring AoT execution to use the authorized ArbiterOfTruth transaction path.

**Acceptance Criteria:**
- [x] Transaction-gated services and pub-sub tests verify rollback compensation, rollback failure reporting, durable pending-commit cancellation, additive audit evidence, and authorized AoT transaction routing. (evidence: BrainSlotInvocationTransactionTests and TurnTransactionPlanArtifactTests)

### TEST-MCP-170

Quad scope enforcement tests SHALL prove Curiosity admission, weight updates, AoT execution, and quad orchestration execute only through FR-MCP-129 through FR-MCP-135 authorization gates, while unrelated autonomous branches remain fail-closed.

**Acceptance Criteria:**
- [x] Plan artifact tests validate the implemented/deferred split and reject stale claims that authorized AoT, weight update, or full orchestration branches remain disabled. (evidence: TurnTransactionPlanArtifactTests.PlanArtifacts_PreserveDeferredScopeAndDesignRounds)

### TEST-MCP-171

Quad architecture conformance tests SHALL validate ARCH-QUAD-001 component mapping, trust boundaries, authorized quad branches, and remaining future branches.

**Acceptance Criteria:**
- [x] Plan artifact tests validate architecture/design artifacts, repo component references, authorized branch routing, and remaining future branch documentation. (evidence: TurnTransactionPlanArtifactTests.PlanArtifacts_PreserveDeferredScopeAndDesignRounds)

### TEST-MCP-172

Architecture Round 1 conformance tests SHALL validate trust model, component boundaries, storage boundaries, threat model, rollback/audit decisions, and gap analysis.

**Acceptance Criteria:**
- [x] `TurnTransactions-Architecture-Round1.md` is covered by `TurnTransactionPlanArtifactTests`.

### TEST-MCP-173

Design Round 2 conformance tests SHALL validate DTOs, entities, options, interfaces, endpoint contracts, reason codes, audit payloads, XMLDoc obligations, and AC-to-test mapping.

**Acceptance Criteria:**
- [x] `TurnTransactions-Design-Round2.md`, `TurnTransactions-Mutation-Endpoint-Audit.md`, requirements docs, matrix, and mappings are covered by `TurnTransactionPlanArtifactTests`.

### TEST-MCP-174

Brain slot documentation and plan artifact coverage SHALL prove FR-MCP-129 through FR-MCP-135, TR-MCP-QUAD-001 through TR-MCP-QUAD-007, TEST-MCP-174 through TEST-MCP-185, diagram annotations, and the AD-CURIOSITY-001-BR-EXTERNAL, AD-AOT-001, AD-WEIGHT-001, and full quad orchestration implementation split are present.

**Acceptance Criteria:**
- [x] TurnTransactionPlanArtifactTests fails if brain-slot requirement IDs, diagram annotations, mapping rows, or matrix rows are missing. (evidence: TurnTransactionPlanArtifactTests)

### TEST-MCP-175

Brain slot durable registry coverage SHALL prove workspace-scoped CRUD, role validation, one enabled slot per workspace and role, replaceExisting replacement audit, soft delete/disable semantics, credentialReference-only persistence, status projection, and workspace isolation.

**Acceptance Criteria:**
- [x] BrainSlotRegistryServiceTests covers create, update, enable, disable, delete, replacement, audit, readiness, and workspace isolation. (evidence: BrainSlotRegistryServiceTests)

### TEST-MCP-176

Brain slot REST, client, and STDIO contract coverage SHALL prove list/get/upsert/delete/enable/disable/status/invoke/orchestrate/aot-reconcile/weight-update routes serialize the public DTOs correctly, hide raw credentials, enforce AgentManager policy behavior, and preserve workspace/auth propagation.

**Acceptance Criteria:**
- [x] BrainSlotsControllerTests, BrainSlotClientTests, BrainSlotContractArtifactTests, and plugin tests cover every public brain-slot operation. (evidence: BrainSlotsControllerTests; BrainSlotClientTests; BrainSlotContractArtifactTests; brain-slots.test.ts)

### TEST-MCP-177

Brain slot provider and credential coverage SHALL prove env:, config:, and file: credential references resolve without persistence or response leakage; OpenAI and OpenAI-compatible client creation validates endpoint policy; disallowed hosts, loopback without explicit allowance, timeout, and cancellation fail closed without live network dependency.

**Acceptance Criteria:**
- [x] BrainSlotCredentialResolverTests and fake invocation/provider tests never require live network credentials. (evidence: BrainSlotCredentialResolverTests and fake provider coverage)

### TEST-MCP-178

Brain slot transaction admission coverage SHALL prove execution is rejected unless brain-slot execution and required turn transactions are enabled, no output is returned before subscriber commit, commit failure/timeout/degradation discards output, and diffgrams contain slot metadata and hashes.

**Acceptance Criteria:**
- [x] BrainSlotInvocationTransactionTests covers disabled gates, failed commits, delayed commits, and committed output return. (evidence: BrainSlotInvocationTransactionTests)

### TEST-MCP-179

Curiosity admission coverage SHALL prove only CuriosityEngine can request GraphRAG/context admission, admission happens only after committed subscriber acknowledgement, failed commits do not inject model output into cache/GraphRAG, and Left/Right/Arbiter invocations never mutate cache.

**Acceptance Criteria:**
- [x] BrainSlotInvocationTransactionTests and BrainSlotContainmentTests cover committed Curiosity admission and rejected non-Curiosity admission. (evidence: BrainSlotInvocationTransactionTests; BrainSlotContainmentTests)

### TEST-MCP-180

Quad containment coverage SHALL prove AoT reconciliation execution, weight update execution, and full automatic quad orchestration are available only through explicit FR-MCP-134/FR-MCP-135 gates, while non-Curiosity GraphRAG mutation and implicit fallback model behavior remain fail-closed.

**Acceptance Criteria:**
- [x] QuadBrainOrchestrationServiceTests covers the authorized quad branches and BrainSlotContainmentTests covers the remaining cache-mutation boundary. (evidence: QuadBrainOrchestrationServiceTests; BrainSlotContainmentTests)

### TEST-MCP-181

Quad orchestration service coverage SHALL prove full Quad-Brain orchestration rejects non-ready workspaces, invokes roles through transaction-gated slots in the required order, and returns final committed AoT output with role transaction metadata.

**Acceptance Criteria:**
- [x] QuadBrainOrchestrationServiceTests covers ready and non-ready orchestration paths. (evidence: QuadBrainOrchestrationServiceTests)

### TEST-MCP-182

AoT reconciliation execution coverage SHALL prove AoT reconciliation executes through the ArbiterOfTruth slot, includes Left/Right/Curiosity evidence, returns committed output only after subscriber acknowledgement, and fails closed without fallback.

**Acceptance Criteria:**
- [x] QuadBrainOrchestrationServiceTests covers Arbiter invocation and committed final output. (evidence: QuadBrainOrchestrationServiceTests)

### TEST-MCP-183

Quad weight update coverage SHALL prove approved weight updates persist weights and versions with audits, while missing approvals, stale versions, invalid roles, disabled slots, and invalid weights are rejected without mutation.

**Acceptance Criteria:**
- [x] QuadBrainOrchestrationServiceTests covers approved persistence/audit and missing-approval rejection. (evidence: QuadBrainOrchestrationServiceTests)

### TEST-MCP-184

Quad public contract parity coverage SHALL prove REST, client, STDIO, and Node plugin surfaces expose orchestration, AoT reconciliation, and weight update contracts consistently.

**Acceptance Criteria:**
- [x] BrainSlotsControllerTests, BrainSlotClientTests, BrainSlotContractArtifactTests, and brain-slots.test.ts cover public parity. (evidence: BrainSlotsControllerTests; BrainSlotClientTests; BrainSlotContractArtifactTests; brain-slots.test.ts)

### TEST-MCP-185

Quad traceability closure coverage SHALL prove FR-MCP-134 through FR-MCP-135, TR-MCP-QUAD-005 through TR-MCP-QUAD-007, TEST-MCP-181 through TEST-MCP-185, and the imported diagrams no longer describe AoT, weight updates, or full quad orchestration as deferred once implemented.

**Acceptance Criteria:**
- [x] TurnTransactionPlanArtifactTests covers the implemented/deferred split and matrix rows. (evidence: TurnTransactionPlanArtifactTests)

### TEST-MCP-186

Tests SHALL verify the ACID tightly coupled Microsoft Agent Framework profile metadata, strict option defaults, filtered model-visible tools, serialized function invocation settings, DI compatibility, and backward compatibility for default hosted-agent registration.

**Acceptance Criteria:**
- [x] A red test fails until the ACID profile metadata and option defaults are implemented.
- [x] A red test fails until ACID run options filter unsafe tools and preserve serialized function invocation.
- [x] A regression test proves default non-ACID hosted-agent registration still exposes the existing tool surface.
- [x] Executed ACID profile tests finish with zero failed and zero skipped tests.

### TEST-MCP-187

RETIRED 2026-07-20 by FR-MCP-142 and TR-MCP-QB-001. This test covered the hosted coding agent reaching QuadBrain orchestration through the mcp_quadbrain_coding_execute tool. That tool has been removed from the shared hosted-agent catalog because it exposed a QuadBrain capability to every McpServer.McpAgent host rather than to QBAgent alone, and it was already dead for QBAgent since QuadBrainInternalToolExecutor had no case for it. Its coverage is replaced by absence assertions under TEST-MCP-193: McpHostedAgentAdapterTests.AcidRuntime_ExposesNoQuadBrainSurface, HostedAgentQuadBrainAbsenceTests (registration functions, attached run-option tools, and QBAgentDefinition tool-name lists), and HostedAgentWorkflowIntegrationTests.AddMcpServerMcpAgent_SharedToolCatalog_ContainsNoQuadBrainTool. QuadBrain coding work continues to run server-side through POST /v1/chat/completions, which QBAgent invokes as an OpenAI-compatible model; that path keeps its own integration coverage.

**Acceptance Criteria:**
- [x] A prompt-array integration test invokes `mcp_quadbrain_coding_execute` for multiple coding-task prompts and asserts the request path, payload, metadata, and committed response shape.
- [x] An ACID profile test proves the Quad Brain coding tool is exposed while unsafe tools remain blocked.
- [x] Tests use an in-memory MCP HTTP handler and do not require live external model credentials or network calls.
- [x] Executed ACID profile tests finish with zero failed and zero skipped tests.

### TEST-MCP-188

Registered 2026-07-20 to close a pre-existing traceability drift: this id was cited at tests/Build.Tests/DocumentationGuidanceTests.cs:35 but existed in neither the MCP requirements store nor docs/Project/Testing-Requirements.md, so a naive next-free-id calculation would have collided with live code. The test verifies that the workspace marker template pins the PowerShell and Node runtime guidance agents depend on: DocumentationGuidanceTests asserts the generated marker prompt instructs agents to invoke PowerShell as pwsh.exe from PATH rather than hard-coding an absolute install path, and that Node usage guidance is present, so that agents on hosts with WindowsApps or MSIX PowerShell layouts still resolve a working runtime. Fixture: the rendered marker prompt template read from the repository. This entry documents existing coverage; no behavior changed when it was registered.


### TEST-MCP-189

Validates FR-MCP-140 and TR-MCP-SEC-005. Unit: the emitted AGENTS-README-FIRST.yaml signature block contains a fields array and a format string, and the fields array lists exactly the canonical marker-v1 payload field names in the order BuildSignaturePayload emits them. Unit, byte-compatibility gate: the HMAC-SHA256 signature value computed for a fixed known marker is unchanged by the refactor, proving the six existing independent verifiers (McpSession.psm1, plugins lib-ps and lib-sh marker resolvers, lib-node and mcp-repl-ts TypeScript resolvers, mcpserver-agent-core marker-trust.ts, MarkerFileClientOptionsResolver.cs) continue to validate. Unit, conditional tail: a marker written with an agentPlugins contract emits the agentPlugins.policy and agentPlugins.contractDigest names in fields, and a marker written without one does not. Unit, drift guard: the canonical key order documented in docs/REPL-AGENT-GUIDE.md matches SignaturePayloadFields exactly, so the prose specification cannot drift from the code.


### TEST-MCP-190

Validates FR-MCP-141 and TR-MCP-SVC-002. Unit: ResolveExecutable skips a PATH entry under Microsoft\WindowsApps that contains a zero-byte alias stub of the requested name, and returns a real executable from a later PATH entry instead. Unit, false-positive guard: a PATH entry under C:\Program Files\WindowsApps, the genuine MSIX install root, is NOT skipped, so packaged executables such as the Store PowerShell remain resolvable; the guard matches only the Microsoft\WindowsApps alias directory. Unit: when the only candidate lies under Microsoft\WindowsApps, resolution reports not-found rather than returning the unusable stub path.


### TEST-MCP-191

Validates FR-MCP-141 and TR-MCP-TUN-004. Unit: NgrokTunnelProvider constructed with an IProcessEnvironmentService applies the interactive user's USERPROFILE, HOME, APPDATA, and PATH to the child ProcessStartInfo it builds for the ngrok process. Unit: the provider resolves the ngrok binary through ResolveExecutable against that enriched PATH rather than passing the literal string ngrok as the file name. Unit: when the binary cannot be resolved the provider logs at Warning rather than Error, and the emitted message names both the Windows-service or Store-alias cause and the remedies. Note that these paths are dormant under the shipped configuration (Tunnel Provider empty, Ngrok.Enabled false), so the coverage is unit-level rather than end to end.


### TEST-MCP-192

Validates TR-MCP-SEC-006 under FR-MCP-129 and FR-MCP-134. Unit: a brain slot whose party id was renamed to brain-slot:creativity, with an active signing key still registered only under the legacy brain-slot:left-hemisphere party, becomes ready after reconciliation, and the key material under the new key id brain-slot:creativity:signing:1 is byte-identical to the legacy key material. Unit, preservation: the legacy party row and legacy key row still exist and remain active after reconciliation, so historical diffgram signatures that reference the legacy key id stay verifiable; the operation is a copy, never a move. Unit, idempotence: running reconciliation twice performs no second write and leaves exactly one key per party id. Unit, no-op paths: reconciliation does nothing when the new party already has an active signing key, and does nothing when no legacy party or key exists, and it never generates new key material in either case. Unit: the same mapping holds for Logic from the legacy brain-slot:right-hemisphere party, while CuriosityEngine and ArbiterOfTruth are untouched.


### TEST-MCP-193

Validates FR-MCP-142 and TR-MCP-QB-001. Absence tests, each written to fail if the surface returns. Node: the shared plugin core exports no brainSlotTools symbol and allToolDescriptors contains no descriptor whose name starts with brain_slot, asserted over the built package rather than the source. Node: dispatching a brain_slot name through the host context throws Unknown tool. Server: the MCP tool surface assembled from the Support.Mcp assembly contains no tool named brain_slot_ anything, asserted by scanning the registered McpServerTool names so the test survives a future file reappearing under a different name. Repl: resolving the client passthrough name BRAINSLOTS fails to resolve rather than returning BrainSlotClient. Agent framework: the shared hosted-agent tool catalog contains no tool name containing quadbrain. Registry: no file matching brain_slot_*.json exists under mcps. Positive control that the server path survives: POST /v1/chat/completions still reaches QuadBrain orchestration and QBAgentChatClientFactory still targets model id QuadBrain at the marker baseUrl, so the removal cannot be satisfied by breaking QBAgent.


### TEST-MCP-194

Validates TR-MCP-SYNC-001. Build.Tests source-convention checks over build/Build.SyncAgentPlugins.cs, following the existing BuildTargetTests read-the-source idiom: (1) the source names the version-less stable vendor file sharpninja-mcpserver-plugin-core.tgz; (2) the source contains no versioned tarball literal matching sharpninja-mcpserver-plugin-core-digits.digits.digits.tgz, so a future hard-coded version cannot return; (3) the source asserts the packed tarball version against plugins/core/lib-node/package.json, pinned by requiring the assertion code to reference the package.json version read. Red state before the fix: the versioned constant sharpninja-mcpserver-plugin-core-0.1.0.tgz is present and the stable name is absent.


### TEST-MCP-195

Pester in plugins/core covering FR-MCP-170/171/172: (1) Invoke-WorkflowAppendDialog for an existing current-turn does not call client.SessionLog.SubmitAsync and does call AppendDialogAsync or POST dialog. (2) Invoke-ReplPersistTurn on HTTP 503 backend_unavailable returns false, sets degraded/queued details, leaves failsafe, does not throw. (3) Failsafe drain on SubmitAsync timeout/503 aborts without drainAttempts increment, without ReplFailsafeDrainCompleted latch, without Failsafe queue drain failed on stderr, and a later drain replays. (4) getFr EXIT 0 with body before 30s when a queued session_submit 503s.

**Acceptance Criteria:**
- [x] Invoke-WorkflowAppendDialog for an existing current-turn does not call client.SessionLog.SubmitAsync and does call AppendDialogAsync or POST dialog. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Invoke-ReplPersistTurn on HTTP 503 backend_unavailable returns false, sets degraded/queued details, leaves failsafe, does not throw. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Failsafe drain on SubmitAsync timeout/503 aborts without drainAttempts increment, without ReplFailsafeDrainCompleted latch, without Failsafe queue drain failed on stderr, and a later drain replays. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] getFr EXIT 0 with body before 30s when a queued session_submit 503s. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Get-ReplMethodTimeoutSeconds during drain for client.SessionLog.SubmitAsync returns REPL_FAILSAFE_DRAIN_TIMEOUT default 120 or REPL_TIMEOUT when that is greater, never 2. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] A failsafe SubmitAsync that succeeds within that budget is replayed and the yaml is removed. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Nested drain while ReplRawInFlight is set is deferred; getFr still EXIT 0 with body before 30s when a queued session_submit 503s. (evidence: plugins/core test-fixtures; tests/McpServer.Repl.Core.Tests; overlay G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

### TEST-MCP-196

C# tests covering FR-MCP-170 and TR-MCP-PERSIST-004: AppendProcessingDialogAsync appends items and GET returns them. Missing turn is 404 classified not-found retryable false. Concurrent TODO query during SubmitAsync does not yield backend_unavailable when the SQLite file is valid. GET /health?nonce= still echoes nonce.


### TEST-MCP-221

Unit tests SHALL prove: (1) TurnTransactionKeyserverScope.RequiresKeyserver is true only for brain-slot:/brain-slot./quadbrain. and false for todo.update, requirements.fr.update, sessionlog.submit, memory.add, repo.write, github.cli, workflow.todo.update, federation.control, context.mutate, and requirements.ingest; (2) TransactionGated adapters and TransactionalTodoWorkflow persist/succeed while CapturingCoordinator.Request stays null even when TurnTransactions.Enabled and RequiredForMutations are true and the coordinator would reject or is degraded; (3) brain-slot.invoke still hits the coordinator; (4) TurnTransactionCoordinator.ExecuteAsync bypasses SignManifestAsync for todo.update. Covered by TurnTransactionKeyserverScopeTests, TransactionGated*Tests, TransactionalTodoWorkflowTests, TurnTransactionCoordinatorTests, BrainSlotInvocationTransactionTests. Zero failures and zero skips in those classes.


### TEST-MCP-222

Verify FR-MCP-174 and TR-MCP-FEDERATION-001 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] Given A/main=C0 and the hub link is down, when A changes TODO T1.title from old to local, then the local API succeeds, a local reread returns local, and no hub request or history commit occurs.
- [ ] Given H/main=C0 and the gateway is stopped, when H edits T1.title, then H persists it locally while remote/main remains C0.
- [ ] Given the prior offline edit, when the application and Dolt restart, then T1.title remains local and main remains C0 until an explicit version commit.

### TEST-MCP-223

Verify FR-MCP-175 and TR-MCP-FEDERATION-002 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] Given main=C0 and remote=C0, when SaveChanges persists T1.title=draft, then main and remote stay C0 and no publication intent is emitted.
- [ ] Given that draft and a valid preview, when commit request R1 succeeds, then main=A1, the reply says committed_locally, and one recoverable publication intent targets A1.
- [ ] Given B has a draft and desired head A1, when B polls, then outcome=waiting_local_work, B/main=C0 and the draft value is unchanged.
- [ ] Given A1 committed while disconnected and a crash before intent persistence, when A restarts, then history scanning rediscovers A1 exactly once and publishes it after reconnect.

### TEST-MCP-224

Verify FR-MCP-176 and TR-MCP-FEDERATION-003 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] Given LAB path /home/sharpninja/github/McpServer and LEGION path F:/github/McpServer, when both enroll as ProjectId Atlas, then both reference Atlas and retain their distinct local paths.
- [ ] Given target has TODO X, when clone is requested, then409 target_not_empty leaves X and all target files unchanged.
- [ ] Given node A has only Atlas membership, when it enrolls or clones SecretProject, then403 unauthorized_project returns no rows, objects or project existence details.
- [ ] Given Atlas is discoverable but A has a nonempty destination, when each plugin selects Atlas and attempts enrollment/clone, then it displays ProjectId, selected branch and409 target_not_empty with remediation; no database is overwritten and no different project is selected.

### TEST-MCP-225

Verify FR-MCP-177 and TR-MCP-FEDERATION-004 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] Given main T1.title=main and feature/search T1.title=feature, when alternating pooled reads run100 times, then each returns only its selected value.
- [ ] Given main=C0 and feature/search=F1, when F1 publishes, then remote feature=F1 and all main heads remain C0.
- [ ] Given a preview for main=C0 and feature=F1 but feature advances to F2, when merge submits the old preview, then409 head_changed creates no commit and changes no ref.

### TEST-MCP-226

Verify FR-MCP-178 and TR-MCP-FEDERATION-005 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] Given C0 contains T1.title=old,T1.description=old,T2.priority=low, when A1 changes title=A, B1 changes description=B and H1 changes T2.priority=high, then merges M1(A1,B1), M2(M1,H1) converge all heads to M2 with values A,B,high and all three commits in ancestry.
- [ ] Given H has an uncommitted title draft at C0, when A publishes A1, then remote=A1, H remains C0 with its draft, and B can accept A1.
- [ ] Given A1 and B1 descend from C0, when both accept requests expect C0, then one succeeds and the other receives409 head_changed; after merge M1 publishes, both A1 and B1 remain ancestors.

### TEST-MCP-227

Verify FR-MCP-179 and TR-MCP-FEDERATION-006 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] Given desired=7 and reported=6, when poll runs twice, then both responses request7 and stored reported remains6.
- [ ] Given desired becomes8 while report7 is in flight, when report7 arrives, then reported=7, desired=8 and the subscription remains pending.
- [ ] Given remote=A1 but control still names C0 after a crash, when reconciliation runs, then it records A1 and advances desired generation without producing another commit.
- [ ] Given a node remembers epoch E1 generation90 and the hub restores a backup at generation40, when the hub reopens, then epoch E2 differs from E1; E1 reports fail409 hub_epoch_changed and no head is force-reset. Node A1 is retained until verified ancestry recovery republishes missing history.

### TEST-MCP-228

Verify FR-MCP-180 and TR-MCP-FEDERATION-007 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] Given base priority=low, A1 priority=high and B1 priority=medium, when B integrates A1, then needs_review shows low/medium/high, B/main remains B1 and no public ref is overwritten.
- [ ] Given A1 deletes T1 and B1 edits T1.title, when histories merge, then needs_review retains B1 and the deleted/edited alternatives for review.
- [ ] Given separate commits add T1 dependsOn T2 and T2 dependsOn T1, when Dolt merges without cell conflicts, then domain validation records dependency_cycle and leaves the prior valid live head unchanged.
- [ ] Given a resolution preview binds local=B1 and incoming=A1, when incoming advances to A2, then resolve returns409 stale_review with no merge commit.
- [ ] Given unresolved needs_review at12:00, when the clock reaches12:15, then plugins/status and the operator inbox show warning with ReviewId and age900s; at24h severity is critical, with at most one repeat per hour.
- [ ] Given Atlas/main is blocked by review R1, when Atlas/feature publishes F2 and another project publishes X1, then both succeed while Atlas/main remains unchanged.
- [ ] Given A changes title and B changes description with no domain violation, when merge runs, then both values survive; competing priority or completion edits produce needs_review instead of choosing a timestamp winner.

### TEST-MCP-229

Verify FR-MCP-181 and TR-MCP-FEDERATION-008 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] Given remote accepted A1 and reply was lost, when request R1 retries, then it returns A1 and the existing receipt; no A2 commit is created.
- [ ] Given worker epoch changes4 to5, when epoch4 reports success, then409 stale_worker_epoch leaves current receipt and desired state unchanged.
- [ ] Given B/main=C0 with title=draft, when A1 is fetched, then outcome=waiting_local_work, main stays C0 and title remains draft; at15min status/inbox shows the age warning.
- [ ] Given B supports schema1 and incoming S1 requires schema2, when sync runs, then409 incompatible_schema leaves B's live data/head unchanged and records the required version.
- [ ] Given source and destination have different schema versions and an incoming change has a verified equivalent data effect on the destination, when synchronization applies that change, then the destination stores the intended typed values, preserves unrelated stored values and relationships, and advances the permitted ref with the expected ancestry and receipt. Applying the operation requires no schema migration or binary replacement solely to match version numbers.
- [ ] Given operation R1 published A1 but its local reply was lost, when a plugin resumes, reads status and requests cancellation, then it shows publishedHead=A1 and a durable cancellation outcome for remaining work; it never claims A1 was rolled back or creates a second commit.

### TEST-MCP-230

Verify FR-MCP-182 and TR-MCP-FEDERATION-009 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] Given tags LA and LG and both counters start1, when both create a TODO, then generated IDs differ and both records survive reconciliation.
- [ ] Given LA allocated sequence42, when main reverts and another branch creates a record, then its sequence is greater than42.
- [ ] Given LAB and LEGION have different entities named TODO-7, when migration resolves the collision, then scoped aliases resolve each original identity and every relational reference follows its reviewed canonical mapping; historical text remains byte-identical.
- [ ] Given existing IMPL-UNITGREEN-001 and old TODO references, when imported and read by their original IDs, then they resolve through permanent aliases without rewriting commits, PR text, session logs, memories or documents.
- [ ] Given LAB TODO-7 and LEGION TODO-91 share normalized title Fix login, description and dependency set, when migration plan runs, then it emits a duplicate candidate with both IDs and evidence; until explicit review, neither is merged or deleted.
- [ ] Given human ID IMPL-UNITGREEN-001 is unreserved, when two nodes reserve it concurrently, then exactly one receives201 and the other409 human_id_reserved; retry by the winning requestId returns the same reservation.
- [ ] Given an offline-created provisional TODO requesting an unreserved human ID, when it reconnects and publishes, then409 human_id_unreserved retains the local record until reservation or reviewed alias resolution.
- [ ] Given LAB TODO-7 and LEGION TODO-91 are a duplicate candidate, when each plugin opens identity review, then both origin IDs, canonical/provisional IDs, similarity evidence and reservation outcome are visible; no alias or row changes before an authorized decision.

### TEST-MCP-231

Verify FR-MCP-183 and TR-MCP-FEDERATION-010 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] Given one fixture for commit R1, conflict R2 and migration R3, when each installed first-party plugin calls its wrapper, then normalized IDs, heads, error codes and audit agent identity match the typed client result.
- [ ] Given server lacks federation contract1, when a plugin invokes commit, then unsupported_contract is shown and no raw SQL or shell fallback executes.
- [ ] Given a cached leaseFence4 and session branch feature, when reconnect discovers fence5, then the plugin retains visible context but refuses writes with lease_lost until reacquired.
- [ ] Given a branch observation is older than24h, when status is displayed, then stale=true and observedUtc are visible; the branch, drafts and refs are not deleted.

### TEST-MCP-232

Verify FR-MCP-184 and TR-MCP-FEDERATION-011 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

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

### TEST-MCP-233

Verify FR-MCP-185 and TR-MCP-FEDERATION-012 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

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

### TEST-MCP-234

Verify FR-MCP-186 and TR-MCP-FEDERATION-013 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

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
- [ ] Given an unavailable port, exited process, or unmet SQL readiness deadline, when startup cannot establish a verified instance within its bounded retry policy, it exits nonzero with safe diagnostics and without a successful instance result. It stops any exact child it started and retains diagnostics and data in the run-owned directory.
- [ ] Given the checksum-pinned Dolt executable and a non-elevated caller, when Nuke StartDoltTestInstance is invoked concurrently, each successful invocation starts and retains a distinct run-owned Dolt process and data directory on a distinct OS-selected loopback port, verifies SQL readiness, and exits zero with a machine-readable result containing the listening port and actual process ID. Routine startup creates no service and requests no elevation.
- [ ] Given a successfully started instance, when Nuke StopDoltTestInstance receives exactly one of its port or process ID, it verifies current process ownership, stops only that process tree, verifies that its listener and process have exited, and retains the instance data and diagnostics. An ambiguous, unowned, or stale selector must never stop another process. Other running instances remain available.

### TEST-MCP-235

Verify FR-MCP-187 and TR-MCP-FEDERATION-014 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

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

### TEST-MCP-236

Verify FR-MCP-188 and TR-MCP-FEDERATION-015 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] Given A1 records an already-sent GitHub update, when B integrates A1, then B state includes the result and outbound GitHub/email invocation count is0.
- [ ] Given accepted commit A1 and effect E1, when sync retries10 times, then the effect ledger still has one E1 and no new external dispatch occurs.
- [ ] Given audit backfill contains command names and receipt payloads, when the hub ingests them, then they are stored as evidence and no business command executes.
- [ ] Given QuadBrain coordinator is degraded but local authorization passes, when federation commit/control/audit operations execute, then none invokes the keyserver and valid local work succeeds.
- [ ] Given incoming A1 contains a TODO completed state and inert historical external-action metadata, when the plugin shows synchronization success, then it labels data received with zero external operations executed; requesting an external operation requires a separate explicit authorized command and its own receipt.

### TEST-MCP-237

Verify FR-MCP-189 and TR-MCP-FEDERATION-016 using the exact pending Given/When/Then criteria below, including restored plugin obligations where applicable. Current plus prior Byrd gates require zero failures and zero skips.

**Acceptance Criteria:**
- [ ] Given agent1 owns a draft, when agent2 commits the same branch, then409 branch_busy creates no commit and the draft remains agent1-owned.
- [ ] Given fence4 expires and fence5 is issued, when a fence4 edit arrives, then409 lease_lost persists no row changes.
- [ ] Given preview fingerprint W1, when the working set becomes W2, then commit with W1 returns409 working_set_changed and requires a fresh preview.
- [ ] Given a write admitted under fence4, when expiry occurs mid-transaction, then that write finishes under the gate before fence5 takeover; a later fence4 write is rejected.
- [ ] Given request R1 created A1 and reply was lost, when R1 retries after restart, then A1 is returned by its commit trailer and history contains no duplicate commit.
- [ ] Given an edit holds the branch gate, when the integration worker requests a live head move, then it waits; after the edit finishes, fingerprint revalidation detects the draft and returns waiting_local_work without overwriting it.
- [ ] Given agent1 owns A/main fence4 with a draft, when agent2 views context and requests editing, then it sees the owner and branch, receives409 branch_busy, and may select a separate feature branch; owner release/takeover requires explicit authorization and preserves the main draft without commit or discard.

### TEST-MCP-238

The 11-case Pester contract suite shall prove profile-reload behavior against a mock-first provider and the production provider, including host mapping, immediate and fallback delivery, dynamic thresholds, unavailable evidence, arm-only lifecycle behavior, repository synchronization, and discriminating negative controls.

**Acceptance Criteria:**
- [ ] The suite verifies host-specific add-profile instructions for all nine official plugins.
- [ ] The suite verifies immediate compact-start injection and retention of the threshold arm.
- [ ] The suite verifies next-prompt fallback when compact context injection is unavailable.
- [ ] The suite verifies silence below 70 percent using the Codex-reported 258400-token context window.
- [ ] The suite verifies one firing at or above 70 percent followed by disarm.
- [ ] The suite verifies Claude assistant usage parsing with a dynamically supplied context window.
- [ ] The suite verifies unavailable usage or context-window evidence leaves state armed.
- [ ] The suite rejects a fixed one-million-token assumption and the retired coordination-memory identifier.
- [ ] The suite verifies PostCompact remains arm-only and cannot start a session or inject context.
- [ ] The production-bound suite verifies byte-identical provider synchronization across all nine plugin repositories.
- [ ] The suite includes discriminating negative controls for threshold and context-window drift.
