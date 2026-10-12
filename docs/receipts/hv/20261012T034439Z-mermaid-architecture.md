<!-- SPDX-License-Identifier: Apache-2.0 -->
# Architecture: Turn Logging, Turn Reviews, and Stateless Agent Context

Status: architecture documentation of the merged plan PLAN-SESSIONTURNREVIEW-001 (`docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md`), pending operator approval of the merged plan. Operator directives (2026-10-11, verbatim): "Integrate attached to current plan.", "Reconstruct plan with added requirements", "Also publish the doc to develop", "Outline now, catalog in P0", "No new code until I approve merged plan.", "I want every model defined.  I want every unit of work request and respone defined.  I want every endpoint defined.", and "I want a single, complete activity diagram (mermaid) for every unit of work.  I want sequence diagrams for submitting each request type and processing every response.  I want a md doc describing the who plan as architecture documentation."; for plugins and scripts, "You should be designing all scripts and script tools around PowerShell and only PowerShell.", "This should have already been the standard for plugins.", and "I have not chosen PowerShell for scripting because of Microsoft or Windows. I have chosen it because it is simply the best tool for the job."

This document describes the whole merged plan as architecture: the components, the data each part owns, every unit of work with its request, response, and error contract, one complete activity diagram per unit of work, and sequence diagrams for submitting each request type and processing each response. The plan document remains the authority for the scope entries, the four execution phases, gates, tests, and acceptance criteria; the agent-context contracts are specified in `docs/Stateless-Agent-Context-Management.md`, and this document repeats them so it stands alone.

How to read the diagrams:

- Activity diagrams are Mermaid flowcharts read top to bottom. A rounded terminal is a start or an end, a diamond is a decision, a rectangle is an action, and every error exit is drawn and ends in a terminal that names the error code.
- Sequence diagrams show who sends what. Each `alt` block lists every response the caller can receive and how the caller processes it.
- Three kinds of activity diagram: a unit of work is exactly one CQRS command or query with its handler (operator, 2026-10-11: "I want to be clear, unit of work maps to one CQRS handler") and is labelled by the record's name; a policy is a rule a handler applies (for example CompletionGate); a process is plugin, host, REPL, or build orchestration that calls handlers (for example ApplyArrivalBridgeRecord). All labels are meaningful names (operator: "Please use human-readable, meaningful identifiers.").
- Requirement ids in brackets name the functional requirement the unit of work realizes. Ids of the form `FR-MCP-AGENTCONTEXT-*` are reserved for Phase 1 (the plan's P0 steps 1-9), where the agent-context catalog is created.

## 1. Goals

1. One user request is exactly one session-log turn, minted by the server when the message arrives, and every write lands on the turn of the request that caused it (Phase A).
2. Every turn carries a request class and two review records (agent and hostile) with lossless decimal scores, a status lifecycle, configurable thresholds, and a server-enforced completion gate (Phase B).
3. Every agent invocation can run stateless: the server builds a bounded, reproducible context package from the profile, rules, requirements, session-log evidence, trusted prior responses, and current code, and only responses graded at 98/98 with no FAIL or UNKNOWN become trusted context for later requests (Phase C).

The agent-context scope depends on the turn-logging scope (turns are the unit a package is built for, and session-log turns are evidence) and on the review scope (the thresholds and the 98/98 grading rule). Phases A, B, and C name scope areas; execution runs in four phases across all three (section 8): requirements, interfaces and code stubs, test-driven unit tests with mocks, and implementation by bounded context with parallel coding agents.

## 2. System context

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-1.svg)

## 3. Cross-cutting conventions

- Workspace scoping: every request is scoped to one workspace through the `X-Workspace-Path` header (REST), the `workspacePath` parameter (MCP tools, REPL), or `MCP_WORKSPACE_PATH` (plugins). Every stored record carries its `WorkspaceId`, and no read ever crosses workspaces.
- Authentication: `X-Api-Key` (or `api_key`) from the workspace marker. Plugins never call REST directly; they use the REPL wrapper.
- Plugin scripting: PowerShell only (operator, 2026-10-11: "You should be designing all scripts and script tools around PowerShell and only PowerShell."). `plugins/core/lib-ps` is the one plugin core for all nine hosts; cline, cline-v2, and opencode keep only the adapter entry file their host API forces, which forwards each host hook invocation (`{ input, output }`) to `lib/plugin-hook.ps1 -HostEvent` and applies the returned `output`, forwards each tool call (`{ params, hostContext }`) to `lib/Invoke-McpPlugin.ps1 -InvocationFromStdin`, and registers the tools listed in `lib/tool-descriptors.json`; the host's own build compiles that one file into the git-ignored `dist/`; Pester is the only plugin test runner.
- Routes: every REST route exists under `/qbrainai/...` with the legacy alias `/mcpserver/...`.
- Errors: every REST error of a unit this plan adds or changes is the `ClassifiedError` problem+json body `{ "type", "title", "status", "detail", "code", "message", "retryable", "details" }` emitted through `McpErrorClassifier` (`SessionLogController.cs` and the agent-context controller); `code` carries the unit's error code (for example `review_status_conflict`, `agent_context_budget_exceeded`), the HTTP status is the one stated per unit (400 validation, 403 not allowed, 404 not found, 409 conflict or disabled, 422 semantic refusal), and MCP tools return `{ code, error, message, retryable, details }`. The completion gate is the one exception to the code rule: its `code` is `validation_error` and its message starts with the token `session_turn_completion_gate` followed by the missing items in the order requestClass, agentReview, hostileReview (FR-MCP-SESSIONCLASS-002-AC006); the diagrams name that refusal by its token. Existing routes whose behavior this plan does not change keep today's error bodies (for example the `{ error }` 400 of the dialog route and the "Invalid session identifier." problem of the open and title routes), stated per unit. The REPL error envelope is `type: error` with `code`, `message`, and `retryable`.
- Identifiers: every identifier is human-readable and meaningful (operator, 2026-10-11: "Please use human-readable, meaningful identifiers."). Session ids `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`; server-minted request ids `req-<yyyyMMddTHHmmssZ>-<8 hex>`; agent-context record keys derive from the turn and session they belong to (section 6.3): packages `pkg-<requestId>-<nn>`, responses `rsp-<requestId>-<nn>`, grades `grd-<responseId>-<kind>-<nn>`, relationships `rel-<type>-<yyyyMMddTHHmmssZ>-<nn>`, HV requests `hvr-<sessionId>-<yyyyMMddTHHmmssZ>`. Document labels use the command, query, process, and policy names.
- Root ids: every record carries the root ids of its parents (workspace, MCP session, request, and for agent context the package and response).
- Secrets: no API key, token, password, or credential is ever stored in a turn field, a context package, or a response; the session-log sanitizer and the agent-context secret filter redact on write and on every read surface.
- Process: the plan runs in the four execution phases of section 8 under the Byrd Development Process v4 (requirements, tests red, mocks green, implementation green, full gates with zero failures and zero skips); each prerequisite, the end of Phase 1, Phase 2, and Phase 3, each bounded context at the end of its Phase 4 work package, and the phase-wide Phase 4 HV end with a Codex gpt-6-astra max hostile validation at accuracy and completeness of at least 98 with every claim PASS. Builds and tests run as Nuke targets natively on Windows or Linux; GitHub Actions and Azure Pipelines are not part of any requirement or process; Octopus (container on LAB-OMARCHY) is used only for releases.

## 4. Phase A: correct, independent turn logging

### 4.1 Components and data

- Server: `SessionLogService.SubmitWithOutcomesAsync` (per-turn outcomes), `ISessionLogService.MintTurnAsync`, `ISessionLogRequestIdMinter`, `ISessionLogTurnUpsertGuard`, `SessionLogTurnEntity.IngressNonce` (unique filtered index on (WorkspaceId, SessionLogId, IngressNonce) where not null, and a second unique filtered index on (WorkspaceId, RequestId) where IngressNonce is not null, so minted request ids are unique across the workspace's sessions while legacy rows are untouched; TR-MCP-SESSIONTURN-004 AC-006).
- PowerShell plugin core (`plugins/core/lib-ps`): prompt hook, arrival bridge (claude-code, claude-cowork), `Sync-PluginArrivalLog`, verb binding (`New-ReplInvocationBinding`, `Resolve-ReplTargetTurn`), truthful persist (`Get-ReplTurnWriteOutcome`, `Invoke-ReplPersistTurn`), failsafe and alias journal, Stop gate (`Get-PluginStopGateTurns`, pure `Get-PluginStopGateFindings`).
- TypeScript-host bindings (cline, cline-v2, opencode) in the same PowerShell core, reached through each host's logic-free adapter, which forwards each host hook invocation (`{ input, output }`) to `lib/plugin-hook.ps1 -HostEvent` and applies the returned `output`, forwards each tool call (`{ params, hostContext }`) to `lib/Invoke-McpPlugin.ps1 -InvocationFromStdin`, and registers the tools listed in `lib/tool-descriptors.json` (operator, 2026-10-11: "You should be designing all scripts and script tools around PowerShell and only PowerShell."); `plugins/core/lib-node` receives no new work and is retired.
- Plugin per-host-session state (one directory per host session key under the plugin cache):
  - `turns/<requestId>.yaml` with its own lock `turns/<requestId>.lock`: one file per turn.
  - `sessions/<hostSessionKey>/turn-state.yaml`: `writeTarget { requestId, deliveredAtUtc, source }`, `openDelivered [requestId]`, `queue [{ kind, requestId, nonce, enqueueOffset, enqueueTimestamp, promptSha256, state, stateOffset, mintedAtUtc }]`, `appliedThroughOffset`, `mcpSessionId`, `ledger [...]`, `unsupportedSince { causeOffset, causeOperation, reason }`; every entry carries the root ids; written by one atomic replace under `turn-open.lock`.
  - `sessions/<hostSessionKey>/ended.yaml` (`reason: session_end`), `sessions/<hostSessionKey>/aliases.yaml` (alias journal), `sessions/<key>/arrival-bridge.pid`.
  - `current-turn.yaml` is a read-only legacy status file and is never used for targeting.

Request lifecycle (plugin queue entry):

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-2.svg)

### 4.2 Units of work (CQRS handlers)

Each unit of work is exactly one CQRS command or query with its handler (operator, 2026-10-11: "I want to be clear, unit of work maps to one CQRS handler"), following `QBrainAi.Cqrs`: a `sealed record` implementing `ICommand<TResult>` or `IQuery<TResult>` and its handler in the same file under `src/QBrainAi.Services/<Area>/Commands` or `Queries`, registered by `AddCqrsHandlers`, returning `Result<TResult>`; controllers and MCP tools dispatch through `IDispatcher.SendAsync` or `QueryAsync` and map failures through `McpErrorClassifier`. Phase A and B handlers delegate to the `ISessionLogService` members the plan names.

#### MintSessionTurnCommand: mint a turn at ingress [FR-MCP-SESSIONTURN-001]

- Handler: `MintSessionTurnCommand(string WorkspacePath, string Agent, string SessionId, SessionLogMintTurnRequest Request) : ICommand<SessionLogMintTurnResult>` and `MintSessionTurnCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/MintSessionTurnCommand.cs`, delegating to `ISessionLogService.MintTurnAsync`.
- Trigger: a user message arrives at a host arrival mechanism (prompt hook, arrival bridge, or host-native adapter). Only hooks and adapters call it; there is no MCP tool, and the plugin wrapper refuses a model-originated call with `mint_requires_host_ingress`.
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/mint` (alias `/mcpserver/...`), client `MintTurnAsync`, REPL `workflow.sessionlog.mintTurn`.
- Request `SessionLogMintTurnRequest`: `ingressNonce` (string, required: the host's per-message id or a SHA-256 of host event fields), `queryTitle`, `queryText`, `model`, `planFile` (value or the `None` sentinel), `todoId` (value or `None`).
- Response `SessionLogMintTurnResult`: `requestId` (`req-<yyyyMMddTHHmmssZ>-<8 hex>`), `outcome` (`created` or `merged`), and `storedStatus` on `merged` only (TR-MCP-SESSIONTURN-004 AC-005).
  - 201 `{ requestId, outcome: "created" }`: a new in-progress turn.
  - 200 `{ requestId, outcome: "merged", storedStatus }`: the same nonce was already minted in this MCP session; the same `requestId` is returned and a terminal turn is not reopened.
- Errors: 400 `ingress_nonce_invalid` (missing or invalid nonce, TR-MCP-SESSIONTURN-004 AC-005), 400 `validation_error` (invalid `planFile` or `todoId`), 404 with detail code `session_not_open`, 500 with final detail code `mint_collision_exhausted` after six request-id collisions.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-3.svg)

#### SubmitSessionLogCommand: submit a session log with per-turn outcomes [FR-MCP-SESSIONTURN-004]

- Handler: `SubmitSessionLogCommand(string WorkspacePath, UnifiedSessionLogDto Log) : ICommand<SessionLogSubmitResult>` and `SubmitSessionLogCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/SubmitSessionLogCommand.cs`, delegating to `ISessionLogService.SubmitWithOutcomesAsync` and applying, per turn, policy TerminalTurnOutcomeGuard, policy RequestClassification when the turn carries `requestClass`, and policy CompletionGate when the write moves the turn to `completed` or `failed` (from S10).
- Surfaces: `POST /qbrainai/sessionlog` (existing route), MCP `sessionlog_submit`, client `SubmitAsync`, REPL submit. This is the route every turn verb already uses: the plugin verbs `appendActions`, `appendDecisions`, `updateTurn`, `completeTurn`, and `failTurn` send the one bound turn in a session document (`Invoke-ReplPersistTurn`, `plugins/core/lib-ps/repl-invoke.ps1:1602-1736`, method `client.SessionLog.SubmitAsync`; process PersistTurnWriteWithReceipt), and so do the REPL workflow (`src/QBrainAi.Repl.Core/SessionLogWorkflow.cs`) and McpAgent (`src/QBrainAi.McpAgent/SessionLog/SessionLogWorkflow.cs`). Dialog and titles use their own routes (AppendTurnDialogCommand, SetTurnTitleCommand, SetSessionTitleCommand).
- Request: the unified session-log document (`UnifiedSessionLogDto`) with its turns.
- Response 201 `SessionLogSubmitResult`: existing fields plus `turns: [{ requestId, outcome, storedStatus, reason }]`, where `outcome` is `created`, `merged`, or `skipped_terminal`. MCP returns `{ success, id, turns }`. The status code stays 201.
- Rules: an identical resubmit is `merged` with reason `no_change`; duplicate request ids in one submission apply last-wins with one outcome each; a write whose status would move a terminal turn (the stored status is terminal, that is `completed`, `failed`, `closed`, `canceled`, or `cancelled` (`IsTerminalTurnStatus`), the incoming status is supplied and differs from it, and either the incoming status is not terminal or the write is a superseded hook persist; today's guard at `SessionLogService.cs:1611-1618`, which the seam `ISessionLogTurnUpsertGuard` replaces unchanged, TR-MCP-SESSIONTURN-004 AC-002) is `skipped_terminal` and changes nothing, while a write that omits the status merges additively even on a terminal turn (omitted-status enrichment, FR-MCP-SESSIONTURN-004-AC003); there is no `skipped_duplicate` outcome.
- Errors: 400 `validation_error`; 400 `request_class_invalid`; 400 `validation_error` with the token `session_turn_completion_gate` (from S10: one or more turns of the submission move to `completed` or `failed` without meeting the gate; every turn is evaluated first, then the whole submission is rejected atomically and the error names each failing `requestId` with its missing items, FR-MCP-SESSIONCLASS-002-AC005).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-4.svg)

#### UpsertSessionTurnCommand: append or merge one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-006, FR-MCP-SESSIONTURN-007]

The single-turn routes (this unit, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, FailSessionTurnCommand) keep today's request bodies, response bodies, and errors (`SessionLogController.cs:191-220` and `:425-566`, `SessionLogService.cs:716-889`). They are the explicit route for deliberate status corrections (the comment on the submit guard, `SessionLogService.cs:1610`), so policy TerminalTurnOutcomeGuard does not apply to them and they report no per-turn outcome. This plan adds only the Phase B policies to them: RequestClassification when a write carries `requestClass` (S4), and CompletionGate when a write moves the turn into `completed` or `failed`, or a PUT carries a terminal status (S10, FR-MCP-SESSIONCLASS-002-AC003, AC004). Both refusals reach these routes through `McpErrorClassifier` (S5, TR-MCP-SESSIONCLASS-002 AC-003) as `ClassifiedError` bodies; every other error keeps today's body.

- Handler: `UpsertSessionTurnCommand(string WorkspacePath, string Agent, string SessionId, UnifiedRequestEntryDto Turn) : ICommand<long>` and `UpsertSessionTurnCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/UpsertSessionTurnCommand.cs`, delegating to `ISessionLogService.UpsertTurnAsync` (`SessionLogService.cs:716-806`), which returns the turn id; the controller composes today's body.
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/turn` (alias `/mcpserver/...`), client `UpsertTurnAsync(agent, sessionId, UnifiedRequestEntryDto)` returning `SessionLogTurnSubmitResult`, REPL `client.SessionLog.UpsertTurnAsync`; no MCP tool (MCP callers write turns through `sessionlog_submit`, SubmitSessionLogCommand). The plugin, REPL-workflow, and McpAgent verbs do not use this route; they persist through SubmitSessionLogCommand.
- Request: `UnifiedRequestEntryDto` (the turn with its `requestId`; from Phase B also `requestClass`). A new turn is inserted with its turn context (planFile and todoId validated as for a new entry); an existing turn is merged additively (omitted fields never clobber stored values), whatever its status.
- Response 201 `{ turnId, agent, sessionId, requestId }` (Location `/qbrainai/sessionlog/{agent}/{sessionId}`). A first insert that loses a unique-index race to a concurrent insert of the same requestId is retried once as an update of the winning row.
- Errors: 400 problem "Invalid turn body." (no body); 400 problem "Invalid turn payload." (invalid session or request id, invalid planFile or todoId, a filesModified path or commit not attributed to the workspace (FR-MCP-SESSIONATTR-001), or the existing QBAgent-only terminal compliance check `ValidateTerminalTurnCompliance`); 404 `not_found` for a missing session (`ClassifiedError`); from Phase B, 400 `request_class_invalid` and the completion-gate refusal (400 `validation_error` with the token `session_turn_completion_gate`), nothing saved.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-5.svg)

#### PatchSessionTurnCommand: change named fields of one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-007]

- Handler: `PatchSessionTurnCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, UnifiedRequestEntryDto Turn) : ICommand<long>` and `PatchSessionTurnCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/PatchSessionTurnCommand.cs`, delegating to `ISessionLogService.UpsertTurnAsync` with the route `requestId` (`SessionLogController.cs` PATCH route, the additive verb of FR-SUPPORT-010G).
- Surfaces: `PATCH /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}`, client `PatchTurnAsync` returning `SessionLogTurnSubmitResult`, REPL `client.SessionLog.PatchTurnAsync`; no MCP tool.
- Request: `UnifiedRequestEntryDto` holding only the fields to change (a missing body is an empty turn; the route `requestId` replaces any body value). As today, a `requestId` the session does not hold is inserted as a new turn.
- Response 200 `{ turnId, agent, sessionId, requestId, status }` (`status` echoes the body's status, null when omitted). Errors as UpsertSessionTurnCommand (no "Invalid turn body." case).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-6.svg)

#### ReplaceSessionTurnCommand: replace one turn [FR-MCP-SESSIONTURN-004, FR-SUPPORT-016]

- Handler: `ReplaceSessionTurnCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, UnifiedRequestEntryDto Turn) : ICommand<long>` and `ReplaceSessionTurnCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/ReplaceSessionTurnCommand.cs`, delegating to `ISessionLogService.ReplaceTurnAsync` (`SessionLogService.cs:839-889`).
- Surfaces: `PUT /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}`, MCP `sessionlog_replace_turn` (returns `{ success, turnId, agent, sessionId, requestId, replaced: true }`), client `ReplaceTurnAsync` returning `SessionLogMutationResult`, REPL `client.SessionLog.ReplaceTurnAsync`.
- Request: the whole turn as `UnifiedRequestEntryDto` (a missing body is an empty turn; the route `requestId` replaces any body value). Omitted scalars reset and every section becomes exactly what the body carries; a superseded hook persist gets the `None` planFile and todoId sentinels when it carries none, as today.
- Response 200 `{ turnId, agent, sessionId, requestId, replaced: true }`. Errors (all through `ClassifiedError`, as today): 400 `validation_error` (invalid session or request id, invalid planFile or todoId, a filesModified path or commit not attributed to the workspace, the QBAgent-only terminal compliance check), 404 `not_found` (missing session or turn); from Phase B, 400 `request_class_invalid` and the completion-gate refusal when the replacement carries a terminal status.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-7.svg)

#### CompleteSessionTurnCommand: complete one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONCLASS-002]

- Handler: `CompleteSessionTurnCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, UnifiedRequestEntryDto Turn) : ICommand<long>` and `CompleteSessionTurnCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/CompleteSessionTurnCommand.cs`, delegating to `ISessionLogService.UpsertTurnAsync` with status `completed` (`SessionLogController.cs:425-472`).
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/complete`, MCP `sessionlog_complete_turn` (optional `turnJson`; returns `{ success, turnId, agent, sessionId, requestId, status }`), client `CompleteTurnAsync` returning `SessionLogTurnSubmitResult`, REPL `client.SessionLog.CompleteTurnAsync`. The plugin `completeTurn` verb does not call this route: it sends the bound turn with status `completed` through SubmitSessionLogCommand after its model-evidence check and advisory pre-check (processes PersistTurnWriteWithReceipt and AdvisoryCompletionPrecheck), and the same policies apply there.
- Request: optional `UnifiedRequestEntryDto` merged additively (from Phase B it may carry `requestClass`); the route sets `requestId` and status `completed`.
- Response 200 `{ turnId, agent, sessionId, requestId, status: "completed" }`. Errors as PatchSessionTurnCommand; the gate fires only when the write moves the turn into `completed`, so completing an already completed turn merges the carried fields without re-gating (FR-MCP-SESSIONCLASS-002-AC004).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-8.svg)

#### FailSessionTurnCommand: fail one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONCLASS-002]

- Handler: `FailSessionTurnCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, UnifiedRequestEntryDto Turn) : ICommand<long>` and `FailSessionTurnCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/FailSessionTurnCommand.cs`, delegating to `ISessionLogService.UpsertTurnAsync` with status `failed` (`SessionLogController.cs:448-472`).
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/fail`, MCP `sessionlog_fail_turn` (optional `turnJson`; returns `{ success, turnId, agent, sessionId, requestId, status }`), client `FailTurnAsync` returning `SessionLogTurnSubmitResult`, REPL `client.SessionLog.FailTurnAsync`. The plugin `failTurn` verb sends the bound turn with status `failed` and its error message through SubmitSessionLogCommand (process PersistTurnWriteWithReceipt), where the same policies apply.
- Request: optional `UnifiedRequestEntryDto` merged additively (its `failureNote` records the failure; from Phase B it may carry `requestClass`); the route sets `requestId` and status `failed`.
- Response 200 `{ turnId, agent, sessionId, requestId, status: "failed" }`. Errors as PatchSessionTurnCommand; the gate needs only `requestClass` for `failed` and fires only when the write moves the turn into `failed`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-9.svg)

#### OpenSessionLogCommand: open (ensure) a session [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-007]

- Handler: `OpenSessionLogCommand(string WorkspacePath, string Agent, string SessionId, string? Title, string? Model) : ICommand<SessionLogOpenResult>` and `OpenSessionLogCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/OpenSessionLogCommand.cs`, delegating to `ISessionLogService.OpenSessionAsync` (`SessionLogService.cs:596-620`, behavior unchanged).
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/open` (`SessionLogController.cs:282`), MCP `sessionlog_open`, client `OpenSessionAsync`, REPL and plugin `workflow.sessionlog.openSession`; called by processes StartHostSession, DrainFailsafe (the idempotent session open replayed before a drained mint), and the first-ingress writer of hosts without SessionStart.
- Request: route `agent` and `sessionId`, optional `title` and `model`. Response 200 `{ agent, sessionId, created }` (`created` false when the session already existed; an existing session is never changed).
- Errors: 400 problem "Invalid session identifier." for a session id invalid for the agent (today's body, unchanged).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-10.svg)

#### AppendTurnDialogCommand: append processing dialog to one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-007]

- Handler: `AppendTurnDialogCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, IReadOnlyList<ProcessingDialogItemDto> Items) : ICommand<SessionLogDialogAppendResult>` and `AppendTurnDialogCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/AppendTurnDialogCommand.cs`, delegating to `ISessionLogService.AppendProcessingDialogAsync` (`SessionLogService.cs:405-465`, behavior unchanged).
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/dialog` (`SessionLogController.cs:232`), MCP `sessionlog_dialog`, client `AppendDialogAsync`, REPL `client.SessionLog.AppendDialogAsync`, plugin verb `appendDialog` (bound by process BindVerbToTurn; `repl-invoke.ps1:2492-2572`), the REPL workflow, and McpAgent. The HV protocol stores a long reviewer result as ordered dialog parts on the reviewer's own turn through this unit.
- Request: one or more `{ timestamp, role, content, category }` items, appended after the highest existing ordinal. Response 200 `{ agent, sessionId, requestId, totalDialogCount }`.
- Errors: 400 `{ error }` for no item or an invalid session or request id, and 404 for a missing turn (today's bodies, `SessionLogController.cs:232-280`, unchanged). The server appends on any turn status; the plugin refuses `appendDialog` on a turn it knows is terminal with `turn_immutable` before any call (FR-MCP-SESSIONTURN-004-AC004).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-11.svg)

#### SetTurnTitleCommand: retitle one turn [FR-MCP-SESSIONTURN-003, FR-MCP-SESSIONTURN-007]

- Handler: `SetTurnTitleCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, string Title) : ICommand<SessionLogTitleResult>` and `SetTurnTitleCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/SetTurnTitleCommand.cs`, delegating to `ISessionLogService.SetTurnTitleAsync` (`SessionLogService.cs:922-955`, behavior unchanged: an explicit overwrite of `QueryTitle` that incidental submits never reset, TR-MCP-SESSIONLOG-005).
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/title` (`SessionLogController.cs:350`), client `SetTurnTitleAsync`, REPL `client.SessionLog.SetTurnTitleAsync`, plugin verb `setTurnTitle` (bound by process BindVerbToTurn, so an explicit `requestId` retitles only that turn, TR-MCP-SESSIONTURN-003 AC-005; `repl-invoke.ps1:2925-2941`); no MCP tool.
- Request `{ title }` (a missing body stores an empty title, as today). Response 200 `{ turnId, agent, sessionId, requestId, retitled: true }` (`SessionLogController.cs:363`). Errors: 400 for an invalid session or request id; 404 `not_found` for a missing session or turn (`ClassifiedError`). The plugin refuses `setTurnTitle` on a locally terminal turn with `turn_immutable` (FR-MCP-SESSIONTURN-004-AC004).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-12.svg)

#### SetSessionTitleCommand: retitle a session [FR-MCP-SESSIONTURN-007]

- Handler: `SetSessionTitleCommand(string WorkspacePath, string Agent, string SessionId, string Title) : ICommand<SessionLogTitleResult>` and `SetSessionTitleCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/SetSessionTitleCommand.cs`, delegating to `ISessionLogService.SetSessionTitleAsync` (`SessionLogService.cs:892-918`, behavior unchanged).
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/title` (`SessionLogController.cs:313`), client `SetSessionTitleAsync`, REPL `client.SessionLog.SetSessionTitleAsync`, plugin verb `setSessionTitle` (targets the host session's own `mcpSessionId`, process StartHostSession); no MCP tool.
- Request `{ title }` (a missing body stores an empty title, as today). Response 200 `{ turnId, agent, sessionId, retitled: true }`, where `turnId` carries the session row id (`SessionLogController.cs:326`, unchanged). Errors: 400 for an invalid session id; 404 `not_found` for a missing session (`ClassifiedError`).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-13.svg)

Existing session-log routes this plan does not change (they keep today's behavior and are not units of this plan): `POST .../{requestId}/begin` (`BeginTurnAsync`; the plugins stop calling it, because ingress mints through MintSessionTurnCommand), `DELETE .../{requestId}` and `DELETE .../{sessionId}` (turn and session delete), `POST /qbrainai/sessionlog/repair-workspace-stamps`, and the section routes `PUT` and `DELETE .../sections/{section}` and `DELETE .../sections/{section}/items/{itemKey}` for every section other than the two review sections. For `agentReview` and `hostileReview` (and their aliases) the section `PUT` dispatches RecordSessionTurnReviewCommand, the section `DELETE` dispatches ResetSessionTurnReviewCommand, and the item `DELETE` is refused by the existing delete-item handler (Phase B, S4).

### 4.3 Policies applied inside Phase A handlers

#### Policy TerminalTurnOutcomeGuard: terminal-turn outcome guard (`ISessionLogTurnUpsertGuard`) [FR-MCP-SESSIONTURN-004]

Applied by SubmitSessionLogCommand only, as today: the single-turn routes (UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, FailSessionTurnCommand) are the explicit route for deliberate status corrections and have no outcome guard (`SessionLogService.cs:1610`). It never saves; it returns the outcome the handler must honor. The seam replaces the inline guard at `SessionLogService.cs:1606-1618` without changing its rule (TR-MCP-SESSIONTURN-004 AC-002): a write is skipped only when the stored status is terminal and the incoming status is supplied, differs from it, and is either not terminal or carried by a superseded hook persist; a write that omits the status, a review-section write, and a requestClass-only write merge on a terminal turn (FR-MCP-SESSIONTURN-004-AC003, AC004).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-14.svg)

### 4.4 Processes (plugin, host, and build; they call the handlers above)

#### Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-002]

- Hosts: claude-code and claude-cowork (the bridge is their only minter; the prompt hook neither mints nor claims there).
- Trigger: a complete transcript record past `appliedThroughOffset`; also applied by `Sync-PluginArrivalLog` from a verb, the Stop hook, or SessionStart up to the transcript length recorded at their own start.
- Input: transcript `queue-operation` records `enqueue`, `dequeue`, `remove` (with reason `absorbed_mid_turn`, another reason, or none), and `popAll`.
- Output: queue and delivery state in `turn-state.yaml` (one atomic replace per record under `turn-open.lock`), mint calls (MintSessionTurnCommand), and `unsupportedSince` on a fail-closed condition.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-15.svg)

#### Process OpenTurnFromPromptHook: open a turn from the prompt hook (hosts without a bridge) [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-002, FR-MCP-SESSIONTURN-003]

- Hosts: codex, grok, grok-bot, copilot, and classic cline (its native UserPromptSubmit hook is a PowerShell command hook); cline-v2 and opencode through their adapters, which forward the prompt event to the same PowerShell core.
- Input: the host's prompt hook payload. Output: a delivered request and write target, or a hook status.
- Hook statuses: `turn-opened`, `turn-already-open`, `system-event-skipped`, `turn-open-contended`, `turn-open-failed` (reason `nonce_source_missing` or the server code), `turn_logging_unsupported`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-16.svg)

#### Process BindVerbToTurn: bind a session-log verb to its turn [FR-MCP-SESSIONTURN-007]

- Verbs: `appendActions`, `appendDialog`, `appendDecisions`, `updateTurn`, `completeTurn`, `failTurn`, `setTurnTitle`, and from Phase B `recordReview`.
- Request: the verb's payload plus an optional explicit `requestId`, or a trusted recovery pair (`sessionId`, `requestId`).
- Response: the resolved target request id and an immutable invocation binding `{ hostSessionKey, keySource, ingressNonce, requestId, boundAtUtc }` stored with the persist and failsafe records; or a refusal code.
- Refusal codes: `request_unknown`, `request_ambiguous`, `turn_immutable`, `host_session_unknown`, `host_session_ambiguous`, `turn_logging_unsupported`; warning `host_session_inferred`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-17.svg)

#### Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-005]

- Request: for `appendActions`, `appendDecisions`, `updateTurn`, `completeTurn`, and `failTurn`, a session document holding only the bound turn, built by `New-McpPluginTurnUpsertRequest` with the binding and sent as `client.SessionLog.SubmitAsync` (`POST /qbrainai/sessionlog`, `Invoke-ReplPersistTurn`); for `appendDialog`, `client.SessionLog.AppendDialogAsync` (`POST .../{requestId}/dialog`); for `setTurnTitle`, `client.SessionLog.SetTurnTitleAsync` (`POST .../{requestId}/title`).
- Close verbs first: `completeTurn` and `failTurn` are refused locally with `turn_missing_model_evidence` when the turn has no model-authored action, design decision, or commit (hook invocations excluded; FR-MCP-SESSIONTURN-005-AC002), and a close that carries evidence writes it as a verified additive non-transition write before the transition, so a gate refusal loses nothing (FR-MCP-SESSIONCLASS-004-AC003, from S8).
- Response (plugin receipt): `persisted` (bool), `code` (`persisted`, `skipped_terminal`, `queued`, `turn_missing_model_evidence`, or the server error code), `storedStatus`, `reason`, and `outcomeVerified` (false when the server answered without per-turn outcomes, FR-MCP-SESSIONTURN-004-AC005); exit code 0 or 1.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-18.svg)

#### Process DrainFailsafe: drain the failsafe [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-005]

- Trigger: SessionStart, and the next verb after an outage.
- Input: failsafe records in their original order, each with its captured binding and root-id envelope.
- Output: minted ids journaled as aliases, writes replayed onto their own turns, records removed only after a confirmed result.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-19.svg)

#### Process EvaluateStopGate: evaluate the Stop gate [FR-MCP-SESSIONTURN-005]

- Trigger: every host Stop (Claude Code `stop_hook_active`, Grok `stopHookActive`; on cline-v2 and opencode the stop-equivalent `afterRun` or `session.idle`, forwarded by the adapter to the same PowerShell gate).
- Output: `{}` (pass) or a block whose text is `session_turn_stop_gate: <n> turn(s) are not closed correctly:` followed by one line per turn `<id> (<code>): <remedy>` and `The stop gate never closes turns for you.`; warnings in either case. The gate never completes or fails a turn.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-20.svg)

#### Process ValidateHookRegistration: validate and repair hook registration [FR-MCP-SESSIONTURN-001]

- Surfaces: `validate-claude-mcp-hooks.ps1` (check or repair) and `install-claude-mcp-hooks.ps1` in the Claude plugin repo.
- Output JSON: the validator's existing result plus the new `duplicate` and `repaired` statuses and the fields `duplicateHooks`, `pluginEnabled`, and `pluginKey`. The installer adds no bridge hook for an event the enabled plugin already covers, and removes managed duplicates with a backup.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-21.svg)

#### Process StartHostSession: start a host session [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-007]

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-22.svg)

#### Process EndHostSession: end a host session [FR-MCP-SESSIONTURN-007]

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-23.svg)

#### Process SyncAgentPluginsAtVersion: sync the agent plugins at a fixed version [TR-MCP-NUKE-003 under FR-MCP-BUILD-001]

- Surface: `./build.ps1 SyncAgentPlugins --agent-plugin-parent <dir> --plugin-version <x.y.z>` (Nuke), then `./build.ps1 ValidatePluginPowerShellOnly --agent-plugin-parent <dir>`.
- Output: a receipt listing exactly nine repos with version and `lib/plugin-hook.ps1` SHA-256; the gate fails on any other count or a mismatch. `ValidatePluginPowerShellOnly` then checks every tracked file (`git ls-files`) and allows on a TypeScript host only the adapter entry file `src/index.ts`, `tsconfig.json`, `package.json`, and the npm-owned lockfile, plus, in the git-ignored `dist/`, only the compiler output of that entry file (`index.js`, `index.d.ts`, and their `.map` files) from the host's existing `npm run build` (`tsc`); the git-ignored `node_modules/` holds installed dependencies, not plugin code, and is not scanned; any other bats, bash, Node, TypeScript, or Python file fails the run (TR-MCP-NUKE-003 AC-005).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-24.svg)

#### Process RunArrivalBridge: arrival bridge lifecycle (claude-code, claude-cowork) [FR-MCP-SESSIONTURN-001]

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-25.svg)

### 4.5 Phase A sequence diagrams

#### Sequence: submit MintSessionTurnCommand and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-26.svg)

#### Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-27.svg)

#### Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-28.svg)

#### Sequence: submit UpsertSessionTurnCommand and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-29.svg)

#### Sequence: submit PatchSessionTurnCommand and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-30.svg)

#### Sequence: submit ReplaceSessionTurnCommand and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-31.svg)

#### Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-32.svg)

#### Sequence: submit CompleteSessionTurnCommand and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-33.svg)

#### Sequence: submit FailSessionTurnCommand and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-34.svg)

#### Sequence: submit OpenSessionLogCommand and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-35.svg)

#### Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-36.svg)

#### Sequence: submit SetTurnTitleCommand from a plugin verb and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-37.svg)

#### Sequence: submit SetSessionTitleCommand and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-38.svg)

#### Sequence: replay the failsafe (process DrainFailsafe) and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-39.svg)

## 5. Phase B: request class, review records, thresholds

### 5.1 Data model

- `SessionLogTurnEntity` gains `RequestClass` (16, indexed) and, per review kind (agent, hostile): `Status` (16, required), `OtherNotes` (text), `Correctness`, `Completeness`, `CorrectnessThreshold`, `CompletenessThreshold` (decimal, precision 29 scale 28), `Reviewer` (256), `ReviewedAt` (UTC), `RequestJsonlPath`, `ResponseJsonlPath` (2048).
- Child `SessionLogTurnReviewReasonEntity { Id, WorkspaceId, SessionLogTurnId, Kind, Ordinal, Value }`.
- Backfill: `RequestClass = Unclassified`, both review statuses `Unstarted`, on every existing row including soft-deleted rows.
- Wire shape on `UnifiedRequestEntryDto`: `requestClass` and the two review objects `agentReview`, `hostileReview` with `status`, `otherNotes`, `correctness`, `completeness`, `correctnessThreshold`, `completenessThreshold`, `reasons`, `reviewer`, `reviewedAt`, `requestJsonlPath`, `responseJsonlPath`; unset fields are emitted as `null`.
- Request classes: `Code`, `Docs`, `Chore` (case-insensitive in, PascalCase out); `Unclassified` exists only for legacy rows.
- Review statuses: `Unnecessary`, `Unstarted`, `Running`, `Pass`, `Fail`, `Other`; `Unnecessary` and `Unstarted` are server-managed, and a review body naming either is refused with `review_unnecessary_not_client_writable` (FR-MCP-SESSIONREVIEW-006-AC004, case `ReviewController_ClientCannotWriteUnstarted`).
- Thresholds: `Mcp:SessionLog:ReviewThresholds:{Agent|Hostile}:{Correctness|Completeness}`, decimal in [0,1], default 0.98, inclusive, per instance through `McpInstanceResolver`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-40.svg)

### 5.2 Units of work (CQRS handlers)

The same rule as section 4.2: each unit is one command or query and its handler. Request classification and the completion gate are not units of their own; they are policies the turn write and close handlers (SubmitSessionLogCommand, the route every plugin, REPL-workflow, and McpAgent turn verb uses, and UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, FailSessionTurnCommand) apply (section 5.3).

#### RecordSessionTurnReviewCommand: record a review [FR-MCP-SESSIONREVIEW-001, -002, -003, -004, -005, -006, -007, FR-SUPPORT-016]

- Handler: `RecordSessionTurnReviewCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, SessionLogTurnReviewKind Kind, SessionLogTurnReviewInput Review) : ICommand<SessionLogTurnReviewDto>` and `RecordSessionTurnReviewCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/RecordSessionTurnReviewCommand.cs`, applying policy HvVerdictMapping when `hvVerdict` is supplied.
- Surfaces: `PUT /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/sections/{section}` with section `agentReview` or `hostileReview` (aliases `agent-review`, `agent_review`, `hostile-review`, `hostile_review`, case-insensitive); MCP `sessionlog_record_review(agent, sessionId, requestId, kind, reviewJson, workspacePath)`; client `RecordTurnReviewAsync`; REPL and plugin `workflow.sessionlog.recordReview` (plugin also accepts `hvReceiptPath`, mapped locally; hostile kind accepts `hvVerdict`).
- Request `SessionLogTurnReviewInput`: `status` (optional), `otherNotes`, `correctness`, `completeness` (JSON number or numeric string, lexeme `^[0-9]+(\.[0-9]+)?$`, at most 28 fractional digits, value in [0,1]), `reasons`, `reviewer`, `reviewedAt` (UTC with offset), `requestJsonlPath`, `responseJsonlPath`, `hvVerdict` (hostile only: on an agent review it is refused with 400 `validation_error`, `details.field` `hvVerdict`). Thresholds on input are ignored.
- Response: the turn's review record with server-derived status and stamped thresholds. Works on every turn status, including completed and failed turns.
- Errors: 404 `not_found` (turn); 400 `validation_error` for an unknown section or `hvVerdict` on an agent review; 400 with the code: `review_invalid_status`, `review_unnecessary_not_client_writable`, `review_other_notes_not_allowed`, `review_other_notes_required`, `review_field_forbidden_for_status`, `review_field_required_for_status`, `review_score_out_of_range`, `review_receipt_paths_required`, `review_score_format`, `review_score_precision`, `review_field_too_long`, `review_timestamp_invalid`, `review_reason_blank`, 422 `review_status_conflict` (includes the thresholds in force and the derived status).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-41.svg)

#### ResetSessionTurnReviewCommand: reset a review section [FR-SUPPORT-016, FR-MCP-SESSIONREVIEW-001]

- Handler: `ResetSessionTurnReviewCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, SessionLogTurnReviewKind Kind) : ICommand<SessionLogTurnReviewDto>` and `ResetSessionTurnReviewCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/ResetSessionTurnReviewCommand.cs`. The item route is not a review unit: it stays on the existing delete-item handler, which refuses review sections.
- Surface: `DELETE /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/sections/{section}`; client `ClearTurnReviewAsync`. Item delete `DELETE .../sections/{section}/items/{itemKey}` on a review section is rejected.
- Response: the record reset to the creation default for the turn's current class.
- Errors: 400 `validation_error` (unknown section); 404 `not_found` (turn); 400 `review_section_reset_on_terminal_turn` (record unchanged). The item delete on a review section never reaches this unit: the existing delete-item handler refuses it with 400.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-42.svg)

#### GetSessionLogQuery: read one session with its turns and reviews [FR-MCP-SESSIONREVIEW-001, FR-MCP-SESSIONREVIEW-004]

- Handler: `GetSessionLogQuery(string WorkspacePath, string Agent, string SessionId) : IQuery<UnifiedSessionLogDto>` and `GetSessionLogQueryHandler` in `src/QBrainAi.Services/SessionLog/Queries/GetSessionLogQuery.cs`.
- Surfaces: REST `GET /qbrainai/sessionlog/{agent}/{sessionId}` only (`SessionLogController.cs:163-179`; the typed client and the REPL have no single-session read and use QuerySessionLogsQuery, which has no session-id filter, so they select the session by its `sessionId` in the returned items).
- Response: 200 `UnifiedSessionLogDto` with both review records per turn, exact decimal scores (a 28-digit score reads back byte-equal), sanitized text, and `null` for unset fields.
- Errors (today's bodies): 400 problem "Invalid session identifier." for a session id invalid for the agent; 404 with no body for a missing session.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-43.svg)

#### QuerySessionLogsQuery: query sessions and turns with their reviews [FR-MCP-SESSIONREVIEW-001, FR-MCP-SESSIONREVIEW-004]

- Handler: `QuerySessionLogsQuery(string WorkspacePath, SessionLogQueryRequest Request) : IQuery<SessionLogQueryResult>` and `QuerySessionLogsQueryHandler` in `src/QBrainAi.Services/SessionLog/Queries/QuerySessionLogsQuery.cs`.
- Surfaces: REST `GET /qbrainai/sessionlog?agent&agentDefinitionId&model&text&from&to&limit&offset&planFile&todoId&turnStatus&staleOlderThanHours` (`SessionLogController.cs:109-151`; there is no session-id filter), MCP `sessionlog_query` (the same filters without `agentDefinitionId` and `offset`), client `QueryAsync(SessionLogQueryRequest)`, REPL `client.SessionLog.QueryAsync`, plugins.
- Request `SessionLogQueryRequest` (unchanged). Paging is clamped, never refused: `limit` defaults to 100 and is clamped to 1..1000, and a negative `offset` is read as 0 (`SessionLogService.cs:476-477`).
- Response `SessionLogQueryResult { totalCount, limit, offset, items }`: the matching sessions, each turn with both review records as in GetSessionLogQuery.
- Errors (today's): 503 `persistence_error` with reason `pending_migration` while the session-log schema migration is pending, and other storage failures through `ClassifiedError`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-44.svg)

### 5.3 Policies applied inside handlers

#### Policy RequestClassification: request classification and the Chore flip [FR-MCP-SESSIONCLASS-001, FR-MCP-SESSIONREVIEW-006]

- Applied by: SubmitSessionLogCommand, UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, FailSessionTurnCommand whenever a write carries `requestClass` (surfaces: the plugin, REPL-workflow, and McpAgent verbs `updateTurn`, `completeTurn`, and `failTurn` through submit; REST turn upsert, PATCH, PUT, complete, and fail; MCP tools; client; REPL).
- Request: `requestClass` (string). Response: the turn with canonical `requestClass` and the review records after any lifecycle effect.
- Errors: 400 `request_class_invalid`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-45.svg)

#### Policy HvVerdictMapping: HV verdict mapping (server) [FR-MCP-SESSIONREVIEW-005]

- Applied by: RecordSessionTurnReviewCommand when a hostile review carries `hvVerdict`, with the thresholds in force at that write.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-46.svg)

#### Policy CompletionGate: completion gate [FR-MCP-SESSIONCLASS-002, -003, -004]

- Applied by: every handler that can transition a turn to `completed` or `failed` (SubmitSessionLogCommand submit, UpsertSessionTurnCommand upsert, PatchSessionTurnCommand patch, ReplaceSessionTurnCommand replace, CompleteSessionTurnCommand complete, FailSessionTurnCommand fail), from S10.
- Policy: `completed` needs `requestClass` and `agentReview.status` in Pass, Fail, or Other; for Code and Docs also `hostileReview.status` in Pass, Fail, or Other; for Chore the hostile review is not checked. `failed` needs `requestClass` only. `canceled` is not gated. Only the transition is gated; terminal enrichment is not re-gated.
- Exempt origins: ServerInternal (`AuditedAgentCliClient`, `WorkspacePolicyService`, `QuadBrainOpenAiChatService`), Replication (federation apply), Import (`SessionLogIngestor`, `TranscriptSessionLogPersister`). Gated: Agent (all external surfaces), `QuadBrainInternalToolExecutor`, REPL importRecovery.
- Error: 400 `session_turn_completion_gate`, message token-prefixed and listing the missing items in the order requestClass, agentReview, hostileReview; the whole write is rejected atomically.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-47.svg)

### 5.4 Processes (plugin, REPL, and McpAgent)

#### Process ConvertHvReceiptToReview: convert a hostile-validation receipt into a review write (plugin), then policy HvVerdictMapping [FR-MCP-SESSIONREVIEW-005]

- Plugin side (`ConvertFrom-McpHvReceipt` in the PowerShell core, for every host): input the reviewer's JSON receipt and the response jsonl; output a `recordReview` payload with `correctness = accuracy/100`, `completeness = completeness/100` as exact decimals, `reasons`, `reviewer`, `reviewedAt`, both jsonl paths, and `hvVerdict`.
- Server side: maps `hvVerdict` with the thresholds in force at that write.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-48.svg)

#### Process ClearSession: clear a session (clear-session coordinator) [TR-MCP-CLEARSESSION-001 under FR-MCP-CLEARSESSION-001]

- Surfaces: the clear-session coordinator in the PowerShell core (S8), used by all nine plugins (the TypeScript hosts through their adapters, S9), driven by the clear-session skill in all nine plugins.
- Output: per open request turn of the host session, one of: completed, failed with a reason, or left in progress with its exact missing items. No review is ever fabricated.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-49.svg)

#### Process AdvisoryCompletionPrecheck: advisory completion pre-check (plugins, REPL, McpAgent) [FR-MCP-SESSIONCLASS-004]

- Run by: the plugin close verbs (S8, S9), the REPL workflow (S6), and McpAgent (S7) before they send the close (a turn at status `completed` or `failed` through SubmitSessionLogCommand, the route all three use). It never refuses: the server gate (CompletionGate) is the only authority.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-50.svg)

#### Process PreserveReviewScoreLexemes: lossless review score handling in the REPL [FR-MCP-SESSIONREVIEW-004]

- Run by: `IReplJsonNumberNormalizer` inside `ReplCommandDispatcher.NormalizeJsonElement` and `GenericClientPassthrough.NormalizeForJson` (S6).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-51.svg)

### 5.5 Phase B sequence diagrams

#### Sequence: submit RecordSessionTurnReviewCommand and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-52.svg)

#### Sequence: submit ResetSessionTurnReviewCommand and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-53.svg)

#### Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-54.svg)

The same policy runs when a client, REPL, or MCP caller sends `requestClass` through PatchSessionTurnCommand, UpsertSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, or FailSessionTurnCommand; their sequences in section 4.5 carry the `request_class_invalid` branch.

#### Sequence: submit GetSessionLogQuery and process the response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-55.svg)

#### Sequence: submit QuerySessionLogsQuery and process the response

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-56.svg)

## 6. Phase C: stateless agent context

### 6.1 Scope and principles

- Each agent invocation can run stateless: it receives a bounded, reproducible context package instead of carrying conversation history forward.
- A package contains the current request, the canonical operator profile and standing rules, the applicable requirements, related session-log evidence, trusted prior responses, and relevant code at the current commit, each with its provenance.
- The relational store is authoritative for packages, responses, grades, and relationships. Response artifacts are projected into GraphRAG for semantic retrieval, with a deterministic relational fallback that gives the same result whenever GraphRAG is disabled, uninitialized, or unavailable.
- A response becomes trusted only when a deterministic evidence grade and an independent model grade both reach correctness and completeness of at least 98 percent with zero FAIL and zero UNKNOWN findings. The model grade is recorded by the HV agent for the responses of an HV request that names a range of session-log turns; this effort has no grading worker or HV pipeline (operator, 2026-10-11). Rejected, stale, and superseded responses are kept for audit and never retrieved as trusted context.
- Mandatory context is never truncated: when it exceeds the token budget, package construction fails explicitly, and in `Pilot` or `Stateless` mode the invocation fails: no answer is produced (operator, 2026-10-11: "Fail the invocation").
- The mode is opt-in (`Mcp:AgentContext:Mode`); the current stateful behavior stays the default during the pilot.
- Requirement ids (reserved for Phase 1, the plan's P0 steps 1-9, and created there with acceptance criteria, mappings, and use cases): FR-MCP-AGENTCONTEXT-001 package construction and provenance; -002 token budget and overflow; -003 response submission; -004 grading, HV requests, and trust; -005 rejection, staleness, and supersession; -006 typed relationships; -007 GraphRAG projection and fallback; -008 REST, MCP, client, REPL, and plugin surfaces; -009 opt-in mode and pilot. The matching TR-MCP-AGENTCONTEXT-* and TEST-MCP-AGENTCONTEXT-* records are created in Phase 1.

### 6.2 Configuration: `AgentContextOptions`

Section `Mcp:AgentContext` (declared under `QBrainAi: AgentContext:` in `appsettings.yaml`, projected onto `Mcp:` by `CanonicalSectionProjectionProvider`), class `QBrainAi.Support.Mcp.Options.AgentContextOptions` with `SectionName = "Mcp:AgentContext"`, a `Validate()` method, and `ValidateOnStart()`.

- `Mode` (enum `AgentContextMode`: `Off`, `Pilot`, `Stateless`; default `Off`). `Off`: every agent-context endpoint except status returns 409 `agent_context_disabled`. `Pilot`: packages are built and responses graded, but plugins keep the stateful conversation (no injection). `Stateless`: plugins inject the package and carry no history, on a host with an authoritative history-exclusion operation recorded in its P0.2 row; any other host treats `Stateless` as `Pilot` (section 6.6).
- `TokenBudget` (int, default 32000, must be 1024..1000000).
- `BytesPerToken` (int, default 4, must be 1..16): the deterministic token estimate is `ceil(utf8ByteCount / BytesPerToken)` per item.
- `Trust:Correctness`, `Trust:Completeness` (decimal in [0.98, 1], default 0.98, inclusive): the attached plan's minimum of 98 percent is a floor that configuration cannot lower; a value below 0.98 or above 1 fails startup validation (`AgentContextOptions_TrustBelow098_FailsStartup`, `AgentContextOptions_TrustAt098And1_Accepted`), and RunDeterministicGradeCommand, RecordModelGradeCommand, and DecideAgentResponseTrustCommand apply `max(configured, 0.98)` so no stored or earlier value below the floor can trust a response (`Trust_StoredThresholdBelowFloor_FloorApplied`). The Phase B review thresholds (FR-MCP-SESSIONREVIEW-007) stay configurable as approved.
- `MaxCandidatesPerKind` (int, default 50, 1..1000) and `MaxPriorResponses` (int, default 10, 0..100).
- `ProfileSources` (list of file globs resolved on the server host; default `~/.claude/profile/*.md` excluding `add-profile*.md`, the set the `/add-profile` skill reads) and `RuleSources` (list of workspace-relative paths; default `CLAUDE.md`, `AGENTS.md`, and `AGENTS-README-FIRST.yaml`, secret values redacted). Profile and rules are mandatory: when no profile file or no rule file resolves, the build fails with 409 `agent_context_profile_unavailable` instead of building a package without them.
- `ModelGrader:AgentName` (default `Codex`), `ModelGrader:RequiredModel` (default `gpt-6-astra`), `ModelGrader:RequiredEffort` (default `max`), `ModelGrader:ReceiptRoot` (workspace-relative, default `docs/receipts/hv`): a model grade from any other grader, or whose receipts are outside the receipt root or do not verify, is refused.
- `HvRequest:MaxTurns` (int, default 200, 1..2000): the largest turn range one HV request may name.
- `GraphRag:ProjectionEnabled` (bool, default true) and `GraphRag:UseForRetrieval` (bool, default true).
- Validation failures (startup): `TokenBudget`, `BytesPerToken`, `HvRequest:MaxTurns`, thresholds outside their ranges, an empty `RuleSources`, an empty grader identity, or an empty `ModelGrader:ReceiptRoot`.

### 6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities`)

Common rules: string keys `[StringLength(128)]`, human-readable and derived from the records they belong to (formats per entity below). Bounded readable keys: a key is `<prefix><parentId><tail>` when that is at most 128 characters; otherwise the embedded parent id is shortened to its first `128 - length(prefix) - length(tail) - 13` characters followed by `-` and the first 12 hex digits of the SHA-256 of the full parent id, so keys stay readable, deterministic, and distinct for distinct parents; the complete parent ids are always stored in their own root-id columns, which keep the live length of 256 (`SessionLogEntity.SessionId` and `SessionLogTurnEntity.RequestId` allow 256), so no root id is ever truncated (`AgentContextKeys_MaxLengthRootIds_FitAndStayDistinct` on SQLite, PostgreSQL, and SQL Server; `AgentContextKeys_ShortRootIds_KeepFullReadableForm`); a sequence suffix `<nn>` (two digits, from 01) is allocated as one more than the highest existing suffix for that prefix, and a unique-index violation from a concurrent insert retries with the next suffix (at most five times, then 409 `conflict`); child rows use `long` identity keys; every entity has `[Required][StringLength(1024)] WorkspaceId` with the named `Workspace` query filter and `StampWorkspaceId` on add; timestamps are `DateTimeOffset` named `*Utc`; decimals are `HasPrecision(29, 28)`; list-valued data is stored in 4NF child tables; idempotency comes from unique indexes (the store has no concurrency tokens). Per-response serialization: every write that changes a response's grades or status (RunDeterministicGradeCommand and RecordModelGradeCommand with DecideAgentResponseTrustCommand, RejectAgentResponseCommand, SupersedeAgentResponseCommand, and EvaluateStalenessCommand) runs in one transaction at `IsolationLevel.Serializable` (the convention of `SessionLogService.cs:156`) whose first statement increments that response's `StatusVersion`, so every such operation on one response takes the row's write lock first and they serialize in commit order on SQLite, PostgreSQL, and SQL Server; a serialization failure or deadlock (PostgreSQL 40001 or 40P01, SQL Server 1205, SQLite busy) rolls the whole operation back and retries it from the start, at most three times, then returns 409 `conflict` with `retryable: true` (`Trust_ForcedInterleaving_DeterministicFailVsModelPass_NeverTrustedOnSupersededGrade`, `Trust_ForcedInterleaving_RejectVsGrade_RejectedNeverRevived`, each on the three providers). Migrations `<yyyyMMddHHmmss>_AddAgentContext` are generated for SQLite, PostgreSQL, and SQL Server with the existing `dotnet ef migrations add` procedure.

`AgentContextPackageEntity` (table `AgentContextPackages`, key `PackageId` = `pkg-<requestId>-<nn>`, for example `pkg-req-20261011T223749Z-prompt-cc85-01`)

- `WorkspaceId`, `SessionId` (256, the MCP session), `Agent` (64), `RequestId` (256, the turn), all required.
- `IdempotencyKey` (128, required): unique with `WorkspaceId`.
- `Status` (16): `Built` or `Overflow`.
- `CommitSha` (64, required), `WorkingTreeDirty` (bool).
- `BuilderVersion` (32), `TokenBudget` (int), `BytesPerToken` (int), `MandatoryTokenCount` (int), `TotalTokenCount` (int).
- `InputsSha256` (64): hash of the canonical build inputs (request text, options in force, commit, and every candidate source hash).
- `OptionsSnapshotJson` (text, required): the canonical JSON of every `AgentContextOptions` value the build used (mode, budget, bytes per token, candidate limits, profile and rule sources, trust floor, GraphRAG flags), so replay never reads current configuration.
- `ContentSha256` (64): hash of the canonical rendered package; null for `Overflow`.
- `QueryText` (text, sanitized), `RenderedContent` (text; null for `Overflow`).
- `CreatedUtc`.
- Indexes: (`WorkspaceId`, `RequestId`), (`WorkspaceId`, `SessionId`, `CreatedUtc`), unique (`WorkspaceId`, `IdempotencyKey`).
- Children: `Items`, `Omissions`, `Findings`, `FocusRefs`, `Inputs` (`DeleteBehavior.Restrict`).

`AgentContextPackageItemEntity` (table `AgentContextPackageItems`, `long Id`)

- `WorkspaceId`, `PackageId` (FK), `Ordinal` (int, unique with `PackageId`).
- `Kind` (32): `Request`, `Profile`, `Rule`, `Requirement`, `SessionLogTurn`, `Todo`, `Code`, `Receipt`, `PriorResponse`.
- `SourceRef` (512): the requirement id, file path, `requestId`, TODO id, receipt path, or `responseId`.
- `SourceSha256` (64), `SourceCommitSha` (64, for `Code`).
- `Mandatory` (bool), `Score` (decimal 0..1, null for mandatory items), `Rationale` (2048), `TokenCount` (int).
- `RetrievalPath` (16): `Relational` or `GraphRag`.
- `Content` (text, sanitized; secret values redacted in mandatory items).

`AgentContextPackageFocusRefEntity` (table `AgentContextPackageFocusRefs`, `long Id`): `WorkspaceId`, `PackageId` (FK), `Ordinal`, `Kind` (32), `Ref` (512): the request's `focusRefs` exactly as given.

`AgentContextPackageInputEntity` (table `AgentContextPackageInputs`, `long Id`): the immutable replay snapshot of every candidate the builder considered, included or omitted: `WorkspaceId`, `PackageId` (FK), `Ordinal` (unique with `PackageId`), `Kind` (32), `SourceRef` (512), `SourceSha256` (64), `SourceCommitSha` (64), `SourceVersion` (128: the git blob id for files under version control, otherwise the record's version or `UpdatedUtc` as read), `Content` (text, exactly as the builder read it after sanitization and before ranking), `RankingFeaturesJson` (text: the canonical JSON of every ranking input of the candidate, including its raw score before rounding and its retrieval path). Replay reads only this snapshot, `OptionsSnapshotJson`, the focus refs, and `BuilderVersion`; it never reads live sources, so later changes to configuration, omitted sources, requirements, TODOs, turns, profile files, or the working tree cannot change a replay.

Source hashes (`SourceSha256`, `ToSha256`), all SHA-256 over UTF-8 bytes:

- `Code`, `Profile`, `Rule`, `Receipt`, `CodeFile`: the file content at the recorded commit for files under version control (never the working copy), otherwise the file content as read; line endings are kept as stored.
- `Requirement`: the canonical JSON of the record (id, title, description, status, and its acceptance criteria in order), serialized with sorted keys and no insignificant whitespace.
- `SessionLogTurn`: the canonical JSON of the turn's request text, response, status, actions, and decisions, excluding timestamps.
- `Todo`: the canonical JSON of the TODO's id, title, description lines, tasks, and done state.
- `PriorResponse`, `Response`: `ContentSha256`. `Package`: `ContentSha256`.
- `Request`: the sanitized request text.
- Response evidence (`AgentResponseEvidence.Sha256`, computed by the server at submission from the cited source): `File` as `Code`, and `Requirement`, `Receipt`, and `SessionLogTurn` as their kinds above; a submitted hash that differs makes that evidence check FAIL in the deterministic grade. These hashes are dependencies of the response: staleness compares them with the current source. `Commit` and `Command` evidence is not a dependency (a commit is immutable; a command result is checked only by the deterministic grade).

Ranking scores are decimals rounded half-even to 6 places before ordering, so float differences between hosts never reorder candidates; ties break on `sourceRef` by ordinal comparison.

`AgentContextOmissionEntity` (table `AgentContextOmissions`, `long Id`)

- `WorkspaceId`, `PackageId`, `Ordinal`, `Kind`, `SourceRef`, `SourceSha256`, `Score`.
- `Reason` (32): `budget`, `untrusted`, `stale`, `rejected`, `superseded`, `secret`, `duplicate`, `below_candidate_limit`.
- `Detail` (2048).

`AgentContextFindingEntity` (table `AgentContextFindings`, `long Id`)

- `WorkspaceId`, `PackageId`, `Ordinal`.
- `Kind` (16): `Contradiction` (two included items are linked by `contradicts`) or `Blocker` (an open item `blocks` the request's TODO or turn).
- `SourceRef` (512), `OtherSourceRef` (512, contradictions only), `RelationshipId` (128), `Summary` (2048).

`AgentResponseEntity` (table `AgentResponses`, key `ResponseId` = `rsp-<requestId>-<nn>`)

- `WorkspaceId`, `PackageId` (FK, required), `SessionId`, `RequestId`, `Agent`, all required (root ids).
- `Model` (128), `Effort` (32), `InvocationMode` (16): `Stateful` or `Stateless`.
- `IdempotencyKey` (128): unique with `WorkspaceId`.
- `SubmissionSha256` (64, required): SHA-256 of the canonical JSON (sorted keys, no insignificant whitespace) of the whole submission: `packageId`, `agent`, `model`, `effort`, `invocationMode`, `contentFormat`, `content`, and `claims` and `evidence` in order; a repeated idempotency key returns the stored response only when this fingerprint is equal, otherwise 409 `conflict`.
- `StatusVersion` (long, default 0): the lock counter of the per-response serialization rule (not an EF concurrency token).
- `Status` (16): `Submitted`, `Grading`, `Trusted`, `Rejected`, `Stale`, `Superseded`.
- `StatusReason` (2048), `SupersededByResponseId` (128).
- `ContentFormat` (16): `markdown`, `text`, or `json`. `Content` (text, sanitized), `ContentSha256` (64).
- `CommitSha` (64): the workspace commit at submission.
- `SubmittedUtc`, `StatusChangedUtc`, `TrustedUtc`.
- Indexes: (`WorkspaceId`, `Status`), (`WorkspaceId`, `RequestId`), (`WorkspaceId`, `PackageId`), unique (`WorkspaceId`, `IdempotencyKey`).
- Children: `Claims`, `Evidence`.

`AgentResponseClaimEntity` (table `AgentResponseClaims`, `long Id`): `WorkspaceId`, `ResponseId`, `Ordinal`, `ClaimId` (64, unique per response), `Text` (4096).

`AgentResponseEvidenceEntity` (table `AgentResponseEvidence`, `long Id`): `WorkspaceId`, `ResponseId`, `Ordinal`, `ClaimId` (64), `Kind` (16: `File`, `Requirement`, `Receipt`, `Commit`, `Command`, `SessionLogTurn`), `Ref` (2048), `Sha256` (64), `CommitSha` (64).

`AgentResponseGradeEntity` (table `AgentResponseGrades`, key `GradeId` = `grd-<responseId>-<deterministic|model>-<nn>`, for example `grd-rsp-req-20261011T223749Z-prompt-cc85-01-model-01`)

- `WorkspaceId`, `ResponseId` (FK), `PackageId`, `RequestId` (256) (root ids).
- `Kind` (16): `Deterministic` or `Model`.
- `Grader` (256): `agent-context-deterministic/<BuilderVersion>`, or `<AgentName>/<model>/<effort>`.
- `Correctness`, `Completeness`, `CorrectnessThreshold`, `CompletenessThreshold` (decimal).
- `PassCount`, `FailCount`, `UnknownCount` (int).
- `Verdict` (16): `Pass` or `Fail`, derived: `Pass` only when both scores meet their thresholds and `FailCount` and `UnknownCount` are zero.
- "Latest grade" of a kind means the grade with the greatest `GradedUtc`, ties broken by the greatest `GradeId` by ordinal comparison; concurrent grade writes are all kept, and the trust decision (DecideAgentResponseTrustCommand) is recomputed after each one inside that grade's serialized transaction (per-response serialization rule above), so the stored status always reflects the latest committed grade pair.
- `HvRequestId` (128, required for `Model`, null for `Deterministic`): the HV request whose review produced the grade.
- `ReviewerSessionId`, `ReviewerRequestId` (256, required for `Model`): the reviewer's own session-log turn that holds its entire result; `VerdictJsonSha256` (64): the hash of the verdict block the grade was verified against.
- `RequestJsonlPath`, `ResponseJsonlPath` (2048, required for `Model`, workspace-relative under `ModelGrader:ReceiptRoot`), `RequestJsonlSha256`, `ResponseJsonlSha256` (64): the receipts and their hashes as verified when the grade was recorded.
- `RolloutJsonlPath` (2048, required for `Model`, under `ModelGrader:ReceiptRoot`), `RolloutJsonlSha256` (64), `ReviewerThreadId` (64, required for `Model`): the reviewer's own Codex rollout receipt (the HV runner copies the completed reviewer thread's rollout file next to the request and response jsonl, redacted by the receipt rule) and the thread id it records; check 8 (`runtime_identity`) verifies them.
- `GradedUtc`.
- Child `Findings`.

`AgentResponseGradeFindingEntity` (table `AgentResponseGradeFindings`, `long Id`): `WorkspaceId`, `GradeId`, `Ordinal`, `ClaimId` (64), `Check` (256), `Outcome` (16: `PASS`, `FAIL`, `UNKNOWN`), `Evidence` (text, sanitized).

`AgentContextRelationshipEntity` (table `AgentContextRelationships`, key `RelationshipId` = `rel-<type>-<yyyyMMddTHHmmssZ>-<nn>`, for example `rel-depends_on-20261011T234000Z-01`)

- `WorkspaceId`, `FromKind` (32), `FromRef` (512), `Type` (32), `ToKind` (32), `ToRef` (512).
- Node kinds: `Package`, `Response`, `Requirement`, `SessionLogTurn`, `Todo`, `CodeFile`, `Receipt`, `Profile`, `Rule`.
- Types: `implements`, `verifies`, `depends_on`, `supports`, `contradicts`, `supersedes`, `blocks`, `derived_from`, `relevant_to`.
- `ToSha256` (64): the target's content hash when linked (used for staleness of `depends_on` and `derived_from`).
- `EvidenceRef` (2048), `CreatedBy` (128), `CreatedUtc`.
- Unique (`WorkspaceId`, `FromKind`, `FromRef`, `Type`, `ToKind`, `ToRef`); indexes on (`WorkspaceId`, `FromKind`, `FromRef`) and (`WorkspaceId`, `ToKind`, `ToRef`).

`AgentContextHvRequestEntity` (table `AgentContextHvRequests`, key `HvRequestId` = `hvr-<sessionId>-<yyyyMMddTHHmmssZ>`, with `-<nn>` appended only when two are created in the same second)

- `WorkspaceId`, `SessionId` (256), `Agent` (64): the MCP session whose turns are reviewed.
- `FromRequestId`, `ToRequestId` (256): the first and last turns of the range, inclusive, in the session's turn order.
- `RequestedBy` (128), `Reason` (2048).
- `Status` (16): `Open` (accepting grades) or `Completed` (no item is `Pending`: every item is `Graded` by this request or `Closed` because its response became `Rejected` or `Superseded`).
- `CreatedUtc`, `CompletedUtc`.
- Child `Items`.

`AgentContextHvRequestItemEntity` (table `AgentContextHvRequestItems`, `long Id`): `WorkspaceId`, `HvRequestId` (FK), `Ordinal`, `RequestId` (256), `ResponseId` (128, unique with `HvRequestId`), `Disposition` (16): `Pending`, `Graded`, or `Closed`, and `ClosedReason` (32): `rejected` or `superseded`. Items are the agent responses whose `RequestId` lies in the range when the request is created and whose status is `Submitted`, `Grading`, `Trusted`, or `Stale` (a `Rejected` or `Superseded` response is never listed); when a listed response becomes `Rejected` or `Superseded` while the request is `Open`, RejectAgentResponseCommand or SupersedeAgentResponseCommand sets its item `Closed` with that reason in the same serialized transaction and completes the request when no item stays `Pending` (`HvRequest_RangeWithRejectedResponse_NotListed`, `HvRequest_ItemRejectedMidReview_ClosedAndRequestCompletes`, `HvRequest_ItemSupersededMidReview_Closed`).

Receipt and reviewer verification (RecordModelGradeCommand), before a model grade is saved; each failure refuses the grade with 422 `agent_context_receipt_unverified` and `details.check` naming the check:

1. `receipt_path`: both jsonl paths resolve inside the workspace under `ModelGrader:ReceiptRoot`.
2. `request_meta`: the request jsonl's first line is a JSON object with `type` `hv_request_meta` whose `reviewer.agent` contains `ModelGrader:AgentName`, `reviewer.model` equals `ModelGrader:RequiredModel`, and `reviewer.effort` equals `ModelGrader:RequiredEffort` (the HV runner writes exactly this object), and the request jsonl's body record names the `hvRequestId`.
3. `verdict_block`: the response jsonl records a completed turn (`turn.completed`) whose last agent message contains exactly one `=== VERDICT JSON ===` block that parses as one JSON object.
4. `verdict_binding`: that verdict names this `hvRequestId` and has a `grades` entry for this `responseId` whose `contentSha256` equals the response's `ContentSha256`.
5. `verdict_match`: the submitted `correctness`, `completeness`, and `findings` equal that entry exactly (same score lexemes; the same claimIds, checks, and outcomes in the same order), and the entry's findings name every claim of the response at least once (an empty or partial finding list is refused).
6. `reviewer_turn`: the session-log turn (`reviewerSessionId`, `reviewerRequestId`) exists in the workspace, its session's agent is `ModelGrader:AgentName`, the turn was created at or after the HV request's `CreatedUtc`, and its stored result contains the same verdict block byte for byte, where the stored result is the turn's response followed by its processingDialog parts in part order with each part's header line removed (the HV protocol stores the entire result on the reviewer's own turn and splits a long result into ordered parts; `ModelGrade_VerdictSplitAcrossDialogParts_RejoinedAndAccepted`).
7. `reviewer_independent`: the reviewer's MCP session is neither the session the HV request reviews nor the session that submitted the response; independence is a separate review session and execution, not a different agent name, so an independently launched Codex reviewer grades a Codex-authored response (`ModelGrade_CodexResponse_IndependentCodexReviewerSession_Accepted`).
8. `runtime_identity`: the rollout receipt at `rolloutJsonlPath` resolves under `ModelGrader:ReceiptRoot`; its `session_meta` record's `id` equals `reviewerThreadId` and the `thread_id` of the response jsonl's `thread.started` event; every `turn_context` record in it has `model` equal to `ModelGrader:RequiredModel` and `effort` equal to `ModelGrader:RequiredEffort`; and the verdict JSON names the same `reviewerThreadId`. The declared `request_meta` alone never satisfies this check (`ModelGrade_LaunchMetaCorrect_RolloutTurnContextOtherModelOrEffort_Refused_RuntimeIdentity`, `ModelGrade_RolloutThreadDiffersFromResponseStream_Refused_RuntimeIdentity`).

The hashes of the three jsonl receipts (request, response, rollout) and of the verdict block are stored on the grade. The remaining trust assumption is the workspace's session-log identity model (every agent of a workspace authenticates with the workspace key), which this plan does not change: a forged grade would need a forged reviewer session, a forged verdict, a forged rollout, and matching receipts, and each is checked against the others. Negative cases: `ModelGrade_DisagreeVerdictPassingScores_Refused_VerdictMatch`, `ModelGrade_EmptyOrPartialFindings_Refused_VerdictMatch`, `ModelGrade_VerdictForOtherResponseOrContent_Refused_VerdictBinding`, `ModelGrade_NoCompletedTurnOrTwoVerdicts_Refused_VerdictBlock`, `ModelGrade_ReviewerTurnMissing_Refused_ReviewerTurn`, `ModelGrade_ReviewerTurnVerdictDiffers_Refused_ReviewerTurn`, `ModelGrade_ReviewerTurnBeforeHvRequest_Refused_ReviewerTurn`, `ModelGrade_ReviewerSameSessionAsProducingOrReviewed_Refused_ReviewerIndependent`, `ModelGrade_LaunchMetaCorrect_RolloutTurnContextOtherModelOrEffort_Refused_RuntimeIdentity`, `ModelGrade_RolloutThreadDiffersFromResponseStream_Refused_RuntimeIdentity`; acceptance case `ModelGrade_CodexResponse_IndependentCodexReviewerSession_Accepted`.

`AgentContextProjectionEntity` (table `AgentContextProjections`, key `ResponseId`)

- `WorkspaceId`.
- `State` (16): `Pending`, `Projected`, `Removed`, `Skipped`, `Failed`.
- `GraphRagDocumentId` (128), `ProjectedContentSha256` (64), `LastError` (2048), `UpdatedUtc`.
- Projection always uses `GraphRagStorageScope.Workspace`; a projected document never reaches the global GraphRAG scope or another workspace.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-57.svg)

Response lifecycle:

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-58.svg)

### 6.4 Data transfer objects

Server DTOs live in `src/QBrainAi.Services/Models/AgentContextModels.cs` (namespace `QBrainAi.Support.Mcp.Models`); client mirrors in `src/QBrainAi.Client/Models/AgentContextModels.cs` with `[JsonPropertyName]` on every property and registration in `McpClientJsonContext` (type, `List<>`, `IReadOnlyList<>`); a parity test keeps them identical. JSON is camelCase; unset optional fields are `null`; decimals travel as exact lexemes (the Phase B score rule).

Shared types

- `AgentContextItemDto { ordinal, kind, sourceRef, sourceSha256, sourceCommitSha, mandatory, score, rationale, tokenCount, retrievalPath, content }`.
- `AgentContextOmissionDto { ordinal, kind, sourceRef, sourceSha256, score, reason, detail }`.
- `AgentContextFindingDto { ordinal, kind, sourceRef, otherSourceRef, relationshipId, summary }`.
- `AgentResponseClaimDto { claimId, text }`; `AgentResponseEvidenceDto { claimId, kind, ref, sha256, commitSha }`.
- `AgentResponseGradeFindingDto { claimId, check, outcome, evidence }`.
- `AgentResponseGradeDto { gradeId, responseId, kind, grader, correctness, completeness, correctnessThreshold, completenessThreshold, passCount, failCount, unknownCount, verdict, hvRequestId, reviewerSessionId, reviewerRequestId, reviewerThreadId, requestJsonlPath, responseJsonlPath, rolloutJsonlPath, verdictJsonSha256, gradedUtc, findings }`.
- `AgentContextRelationshipDto { relationshipId, fromKind, fromRef, type, toKind, toRef, toSha256, evidenceRef, createdBy, createdUtc }`.
- `AgentContextCandidateDto { kind, sourceRef, sourceSha256, score, rationale, retrievalPath }`.
- Query results follow `{ totalCount, limit, offset, items }`.

Requests and results (one pair per unit of work, section 6.5)

- GetAgentContextStatusQuery `AgentContextStatusResult { mode, tokenBudget, bytesPerToken, trustCorrectness, trustCompleteness, graphRag { enabled, state, projectionEnabled, useForRetrieval }, counts { packages, responsesByStatus } }`.
- BuildAgentContextPackageCommand `BuildAgentContextPackageRequest { sessionId, agent, requestId, queryText, tokenBudget, focusRefs, idempotencyKey }` (`tokenBudget` optional and at most the configured budget; `focusRefs` optional list of `{ kind, ref }` to prefer); result `AgentContextPackageResult { packageId, status, sessionId, agent, requestId, commitSha, workingTreeDirty, builderVersion, tokenBudget, mandatoryTokenCount, totalTokenCount, inputsSha256, contentSha256, createdUtc, items, omissions, findings, renderedContent }`.
- GetAgentContextPackageQuery get package: route id plus `includeContent` (bool, default true); result `AgentContextPackageResult`.
- QueryAgentContextPackagesQuery `AgentContextPackageQuery { requestId, sessionId, status, limit, offset }`; result `AgentContextPackageQueryResult` of package summaries (all fields except items, omissions, findings, and content).
- ReplayAgentContextPackageQuery replay: route id; result `AgentContextReplayResult { packageId, builderVersion, identical, originalContentSha256, replayContentSha256, originalInputsSha256, replayInputsSha256, differences [{ ordinal, kind, sourceRef, change }] }` with `change` in `added`, `removed`, `hashChanged`, `reordered`.
- SubmitAgentResponseCommand `SubmitAgentResponseRequest { packageId, agent, model, effort, invocationMode, contentFormat, content, claims, evidence, idempotencyKey }` (at least one claim, and every claim at least one evidence item); result `AgentResponseResult { responseId, packageId, sessionId, requestId, agent, model, effort, invocationMode, status, statusReason, supersededByResponseId, contentFormat, contentSha256, submissionSha256, commitSha, submittedUtc, statusChangedUtc, trustedUtc, claims, evidence, grades, content }`.
- GetAgentResponseQuery get response: route id plus `includeContent`; result `AgentResponseResult`.
- QueryAgentResponsesQuery `AgentResponseQuery { requestId, packageId, status, trustedOnly, limit, offset }`; result `AgentResponseQueryResult` of response summaries.
- RunDeterministicGradeCommand deterministic grade: route id, no body; result `AgentResponseGradeResult { grade, responseStatus, statusReason }`.
- RecordModelGradeCommand `RecordModelGradeRequest { hvRequestId, reviewerSessionId, reviewerRequestId, reviewerThreadId, grader, model, effort, correctness, completeness, findings, requestJsonlPath, responseJsonlPath, rolloutJsonlPath, gradedUtc }` (scores and findings equal the reviewer's verdict entry for the response, section 6.3); result `AgentResponseGradeResult` plus `hvRequestStatus`.
- DecideAgentResponseTrustCommand trust decision (internal, no surface): result `AgentResponseTrustDecision { responseId, previousStatus, status, statusReason, deterministicGradeId, modelGradeId, decidedUtc }`; the dispatching grade handler copies `status` and `statusReason` into `AgentResponseGradeResult.responseStatus` and `statusReason`.
- RejectAgentResponseCommand `RejectAgentResponseRequest { reason }`; result `AgentResponseResult`.
- SupersedeAgentResponseCommand `SupersedeAgentResponseRequest { supersededByResponseId, reason }`; result `AgentResponseResult`.
- EvaluateStalenessCommand `EvaluateStalenessRequest { responseIds }` (empty means every non-terminal response); result `AgentContextStalenessResult { evaluatedCount, newlyStale [{ responseId, changedRef, relationshipId, reason }] }`.
- CreateAgentContextRelationshipCommand `CreateAgentContextRelationshipRequest { fromKind, fromRef, type, toKind, toRef, evidenceRef }`; result `AgentContextRelationshipDto` plus `created` (bool).
- DeleteAgentContextRelationshipCommand delete relationship: route id; result 204.
- QueryAgentContextRelationshipsQuery `AgentContextRelationshipQuery { nodeKind, nodeRef, type, direction (out, in, both), limit, offset }`; result `AgentContextRelationshipQueryResult`.
- RetrieveAgentContextCandidatesQuery `AgentContextRetrieveRequest { queryText, sessionId, requestId, focusRefs, kinds, limit, trustedOnly, preferGraphRag }` (`sessionId` and `requestId` together name the turn whose relationship neighborhood ranks the relational path, both or neither; `focusRefs` optional; `trustedOnly` defaults to true and only `true` is accepted for `PriorResponse`); result `AgentContextRetrieveResult { retrievalPath, fallbackReason, candidates }`.
- ProjectAgentResponsesCommand `ProjectAgentContextRequest { responseIds }` (empty means every response whose projection is pending or out of date); result `AgentContextProjectionResult { projected, removed, skipped, failed [{ responseId, error }], skippedReason }`.
- RunStatelessInvocation stateless invocation: plugin-internal; it uses GetAgentContextStatusQuery, BuildAgentContextPackageCommand, and SubmitAgentResponseCommand; on a build failure the invocation fails (no answer) and the turn is closed `canceled` with the error.
- CreateAgentContextHvRequestCommand `CreateAgentContextHvRequestRequest { sessionId, agent, fromRequestId, toRequestId, requestedBy, reason }`; result `AgentContextHvRequestResult { hvRequestId, status, sessionId, agent, fromRequestId, toRequestId, requestedBy, reason, createdUtc, completedUtc, items [{ requestId, responseId, disposition, closedReason }] }`.
- GetAgentContextHvRequestQuery get HV request: route id; result `AgentContextHvRequestResult`.
- QueryAgentContextHvRequestsQuery `AgentContextHvRequestQuery { sessionId, status, limit, offset }`; result `AgentContextHvRequestQueryResult` of results without items.
- GetAgentContextPilotReportQuery `AgentContextPilotReportQuery { fromUtc, toUtc }`; result `AgentContextPilotReport { fromUtc, toUtc, byMode [{ invocationMode, responses, trusted, rejected, stale, meanCorrectness, meanCompleteness, trustRate }] }`.

Error codes (problem+json body `{ type, title, status, detail, code, message, retryable, details }`; MCP tools return `{ code, error, message, retryable, details }`)

- 400 `validation_error` with `details.field` for malformed input, plus the Phase B score codes `review_score_format`, `review_score_precision`, `review_score_out_of_range` for grade scores.
- 400 `agent_context_receipt_paths_required` (model grade without its request, response, and rollout jsonl paths, the reviewer turn ids, or `reviewerThreadId`).
- 400 `agent_context_relationship_invalid` (unknown kind or type, or a self-link).
- 400 `agent_context_hv_request_invalid` (unknown session, an unknown or out-of-order range, a range over `HvRequest:MaxTurns`, or a range with no agent response).
- 403 `agent_context_grader_not_allowed` (model grade from a grader other than the configured one).
- 409 `agent_context_grade_not_in_hv_request` (the response is not an item of the named HV request) and 409 `agent_context_hv_request_closed` (the HV request is `Completed`).
- 404 `not_found` with `details.resource` (`package`, `response`, `relationship`, `session_turn`, `ref`, `hv_request`).
- 409 `agent_context_disabled` (mode `Off`).
- 409 `agent_context_package_overflow` (a response or replay against an `Overflow` package).
- 409 `agent_context_response_not_gradable` (response `Rejected` or `Superseded`).
- 409 `agent_context_supersede_invalid` (cross-package request mismatch, a cycle, or a terminal target).
- 409 `agent_context_commit_unavailable` (the workspace is not a git checkout or the recorded commit cannot be read).
- 409 `agent_context_profile_unavailable` (no configured profile file or no rule file resolves; mandatory context cannot be assembled).
- 409 `conflict` (idempotency key reused with different inputs).
- 409 `agent_context_replay_snapshot_unavailable` (the package has no stored input snapshot) and 409 `agent_context_replay_builder_changed` (the package was built by another builder version; `details { recorded, current }`).
- 422 `agent_context_receipt_unverified` with `details.check` naming the failed check: `receipt_path`, `request_meta`, `verdict_block`, `verdict_binding`, `verdict_match`, `reviewer_turn`, `reviewer_independent`, or `runtime_identity`.
- 422 `agent_context_budget_exceeded` with `details { tokenBudget, mandatoryTokenCount, overflowTokenCount, mandatoryItems [{ kind, sourceRef, tokenCount }] }`.

### 6.5 Units of work (CQRS handlers), endpoints, and activity diagrams

Each unit is exactly one CQRS command or query with its handler (operator, 2026-10-11: "I want to be clear, unit of work maps to one CQRS handler"), in `src/QBrainAi.Services/AgentContext/Commands` or `Queries` (namespace `QBrainAi.Support.Mcp.AgentContext.Commands` or `.Queries`), one file per record and handler, registered by `AddCqrsHandlers`. Every surface dispatches the same record through `IDispatcher`: the controller and the MCP tool call `SendAsync` or `QueryAsync`; the typed client, REPL, and plugins reach them over REST. A handler reports a contract error as `Result<T>.Failure(new AgentContextException(code, statusCode, message, details))`; `McpErrorClassifier` maps `AgentContextException` to the problem+json body with that status and `code`. The stateless invocation is not a unit: it is the plugin process RunStatelessInvocation (section 6.6).

Every REST route below exists under `/qbrainai/agent-context` and the alias `/mcpserver/agent-context` (controller `AgentContextController`, `[ApiController]`, workspace required through `X-Workspace-Path`, authenticated by `WorkspaceAuthMiddleware`). MCP tools live in `FwhMcpTools.AgentContext.cs` with `workspacePath` as the first parameter and JSON-string bodies. The typed client is `AgentContextClient : McpClientBase`, exposed as `QBrainAiClient.AgentContext`. The REPL reaches it as `client.AgentContext.<Method>` (added to the `GenericClientPassthrough` allow-list as `AGENTCONTEXT`; mutating methods registered in `ClientMutationPolicy`; YAML validators registered with explicit `payload.params.<key>` paths). Typed client signatures, which the REPL binds by parameter name, case-insensitively (`GenericClientPassthrough.cs:649-695`; a missing required parameter fails with `Missing required parameter`): a route id is a separate `string` parameter named `packageId`, `responseId`, `relationshipId`, or `hvRequestId`; a command's body record is one parameter named `request`; a query's filter record is one parameter named `query`; a scalar such as `includeContent` keeps its name. So the REPL params of BuildPackageAsync are `{ request: { sessionId, agent, requestId, queryText, tokenBudget, focusRefs, idempotencyKey } }`, of GetPackageAsync `{ packageId, includeContent }`, of RejectResponseAsync `{ responseId, request: { reason } }`, and of QueryPackagesAsync `{ query: { requestId, sessionId, status, limit, offset } }`. PowerShell plugins call `Invoke-McpPlugin.ps1 -Command Invoke -Method client.AgentContext.<Method> -ParamsObject <params>` with `<params>` from the exported builder named in each unit (`McpPluginShim.psm1`). Builder contract, one builder per externally callable unit: `New-McpAgentContext<Verb>Params` takes one PowerShell parameter per route id, scalar, and body or filter field, named in PascalCase, typed string as `[string]`, bool as `[bool]`, int as `[int]`, a decimal score as `[string]` holding the exact lexeme, and a list as `[object[]]` of hashtables with the record's camelCase keys; a parameter is mandatory exactly when the client parameter or record field is required, and an omitted optional parameter is left out of the output (the server applies the default); the builder returns that envelope as an ordered hashtable: route ids and scalars at the top level under their client parameter names, and the record's camelCase fields, in record order with the values unchanged, nested under `request` or `query`; it never calls the server (`AgentContextBuilders_ParametersMatchRequestRecords_OutputKeysAndValuesExact`, generated over every unit from `docs/context/agent-context-contract.json`, and `AgentContextBuilders_EnvelopesBindThroughGenericClientPassthrough`, which binds every builder's output through the real `GenericClientPassthrough` binder against the real `AgentContextClient` signatures). Tool descriptor contract: each externally callable unit has one entry in `lib/tool-descriptors.json` with `name` (the `agent_context_*` name in each unit), `description`, `inputSchema` (the JSON schema of that same envelope: route ids and scalars as top-level properties, the body as a required `request` or `query` object with the record's schema, scores as strings), `method` (the `client.AgentContext.<Method>` REPL method listed in each unit), and `mutates` (true for commands, false for queries); a TypeScript host's adapter runs `Invoke-McpPlugin.ps1 -Command Invoke -Method <method> -InvocationFromStdin` with `{ params, hostContext }` as JSON on stdin (the call's arguments, an envelope of that schema, and the host's separate tool context) (TR-MCP-SESSIONTURN-008), so PowerShell and TypeScript hosts reach the same command (operator, 2026-10-11: "You should be designing all scripts and script tools around PowerShell and only PowerShell.").

Every unit except GetAgentContextStatusQuery, DecideAgentResponseTrustCommand, and RunStatelessInvocation (including CreateAgentContextHvRequestCommand, GetAgentContextHvRequestQuery, and QueryAgentContextHvRequestsQuery) starts with the same two checks, drawn in each diagram: the workspace resolves (otherwise 404 `Workspace required`), and the mode is not `Off` (otherwise 409 `agent_context_disabled`).

#### GetAgentContextStatusQuery: get status [FR-MCP-AGENTCONTEXT-008, -009]

- Handler: `GetAgentContextStatusQuery(string WorkspacePath) : IQuery<AgentContextStatusResult>` and `GetAgentContextStatusQueryHandler` in `src/QBrainAi.Services/AgentContext/Queries/GetAgentContextStatusQuery.cs`.
- REST `GET /qbrainai/agent-context/status`; MCP `agent_context_status(workspacePath)`; client `GetStatusAsync()`; REPL `client.AgentContext.GetStatusAsync`; PowerShell `New-McpAgentContextStatusParams`; tool descriptor `agent_context_status`.
- Request: none. Response 200 `AgentContextStatusResult`. Errors: 404 workspace.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-59.svg)

#### BuildAgentContextPackageCommand: build a context package [FR-MCP-AGENTCONTEXT-001, -002]

- Handler: `BuildAgentContextPackageCommand(string WorkspacePath, BuildAgentContextPackageRequest Request) : ICommand<AgentContextPackageResult>` and `BuildAgentContextPackageCommandHandler` in `src/QBrainAi.Services/AgentContext/Commands/BuildAgentContextPackageCommand.cs`.
- REST `POST /qbrainai/agent-context/packages`; MCP `agent_context_build_package(workspacePath, requestJson)`; client `BuildPackageAsync(BuildAgentContextPackageRequest)`; REPL `client.AgentContext.BuildPackageAsync`; PowerShell `New-McpAgentContextBuildPackageParams`; tool descriptor `agent_context_build_package`.
- Request `BuildAgentContextPackageRequest`. Response 201 `AgentContextPackageResult` (Location `/qbrainai/agent-context/packages/{packageId}`), or 200 with the existing package when the idempotency key repeats with identical inputs.
- Errors: 400 `validation_error`; 404 `not_found` (`session_turn`); 409 `agent_context_disabled`, `agent_context_commit_unavailable`, `agent_context_profile_unavailable`, `conflict` (key reused with different inputs); 422 `agent_context_budget_exceeded` (an `Overflow` package is stored for audit).
- Concurrency: two builds with the same idempotency key race on the unique index; the loser re-reads the winner's package and returns it with 200 when the inputs match, or 409 `conflict` when they differ.
- Deterministic construction: candidates are gathered per kind, filtered, ranked by score descending then `sourceRef` ordinal, and rendered in the fixed kind order Request, Profile, Rule, Requirement, Todo, SessionLogTurn, Receipt, PriorResponse, Code; the rendered text is canonical (LF line endings, one header line per item `[kind] sourceRef sha256`), so the same inputs at the same commit give the same `contentSha256`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-60.svg)

#### GetAgentContextPackageQuery: get a package [FR-MCP-AGENTCONTEXT-001]

- Handler: `GetAgentContextPackageQuery(string WorkspacePath, string PackageId, bool IncludeContent) : IQuery<AgentContextPackageResult>` and `GetAgentContextPackageQueryHandler` in `src/QBrainAi.Services/AgentContext/Queries/GetAgentContextPackageQuery.cs`.
- REST `GET /qbrainai/agent-context/packages/{packageId}?includeContent=true`; MCP `agent_context_get_package(workspacePath, packageId, includeContent)`; client `GetPackageAsync(packageId, includeContent)`; REPL `client.AgentContext.GetPackageAsync`; PowerShell `New-McpAgentContextGetPackageParams`; tool descriptor `agent_context_get_package`.
- Response 200 `AgentContextPackageResult`. Errors: 404 workspace or `not_found` (`package`), 409 `agent_context_disabled`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-61.svg)

#### QueryAgentContextPackagesQuery: query packages [FR-MCP-AGENTCONTEXT-001]

- Handler: `QueryAgentContextPackagesQuery(string WorkspacePath, AgentContextPackageQuery Query) : IQuery<AgentContextPackageQueryResult>` and `QueryAgentContextPackagesQueryHandler` in `src/QBrainAi.Services/AgentContext/Queries/QueryAgentContextPackagesQuery.cs`.
- REST `GET /qbrainai/agent-context/packages?requestId&sessionId&status&limit&offset` (limit default 50, max 1000); MCP `agent_context_query_packages(workspacePath, requestId, sessionId, status, limit, offset)`; client `QueryPackagesAsync(AgentContextPackageQuery)`; REPL `client.AgentContext.QueryPackagesAsync`; PowerShell `New-McpAgentContextQueryPackagesParams`; tool descriptor `agent_context_query_packages`.
- Response 200 `AgentContextPackageQueryResult`. Errors: 400 `validation_error` (limit or offset out of range, unknown status), 404 workspace, 409 `agent_context_disabled`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-62.svg)

#### ReplayAgentContextPackageQuery: replay a package [FR-MCP-AGENTCONTEXT-001]

- Handler: `ReplayAgentContextPackageQuery(string WorkspacePath, string PackageId) : IQuery<AgentContextReplayResult>` and `ReplayAgentContextPackageQueryHandler` in `src/QBrainAi.Services/AgentContext/Queries/ReplayAgentContextPackageQuery.cs`.
- REST `POST /qbrainai/agent-context/packages/{packageId}/replay`; MCP `agent_context_replay_package(workspacePath, packageId)`; client `ReplayPackageAsync(packageId)`; REPL `client.AgentContext.ReplayPackageAsync`; PowerShell `New-McpAgentContextReplayPackageParams`; tool descriptor `agent_context_replay_package`.
- Replay rebuilds the package in memory from its immutable input snapshot only (`OptionsSnapshotJson`, focus refs, and `AgentContextPackageInputs`, section 6.3) with the same builder version, reads no live source or configuration, stores nothing, and compares the result, so replay stays identical after configuration, omitted-source, and non-Git source changes (`Replay_AfterConfigChange_Identical`, `Replay_AfterOmittedSourceChange_Identical`, `Replay_AfterRequirementTodoTurnProfileChange_Identical`).
- Response 200 `AgentContextReplayResult`. Errors: 404 workspace or `not_found` (`package`), 409 `agent_context_disabled`, `agent_context_package_overflow`, `agent_context_replay_snapshot_unavailable` (no stored snapshot), `agent_context_replay_builder_changed` (built by another builder version; `details { recorded, current }`).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-63.svg)

#### SubmitAgentResponseCommand: submit a response [FR-MCP-AGENTCONTEXT-003]

- Handler: `SubmitAgentResponseCommand(string WorkspacePath, SubmitAgentResponseRequest Request) : ICommand<AgentResponseResult>` and `SubmitAgentResponseCommandHandler` in `src/QBrainAi.Services/AgentContext/Commands/SubmitAgentResponseCommand.cs`.
- REST `POST /qbrainai/agent-context/responses`; MCP `agent_context_submit_response(workspacePath, requestJson)`; client `SubmitResponseAsync(SubmitAgentResponseRequest)`; REPL `client.AgentContext.SubmitResponseAsync`; PowerShell `New-McpAgentContextSubmitResponseParams`; tool descriptor `agent_context_submit_response`.
- Response 201 `AgentResponseResult` with status `Submitted`, or 200 with the existing response for a repeated idempotency key with an identical submission (`SubmissionSha256`, section 6.3; `Submit_SameKeyIdenticalSubmission_200Existing`, `Submit_SameKeySameContentDifferentPackage_409Conflict`, `Submit_SameKeySameContentDifferentClaimsOrEvidence_409Conflict`).
- Errors: 400 `validation_error` (empty content, no claim, a claim with no evidence item, duplicate claimId, evidence pointing to an unknown claim, unknown format or mode); 404 `not_found` (`package`); 409 `agent_context_disabled`, `agent_context_package_overflow`, `conflict` (key reused with a different submission: any differing package, agent, model, effort, mode, format, content, claim, or evidence).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-64.svg)

#### GetAgentResponseQuery: get a response [FR-MCP-AGENTCONTEXT-003]

- Handler: `GetAgentResponseQuery(string WorkspacePath, string ResponseId, bool IncludeContent) : IQuery<AgentResponseResult>` and `GetAgentResponseQueryHandler` in `src/QBrainAi.Services/AgentContext/Queries/GetAgentResponseQuery.cs`.
- REST `GET /qbrainai/agent-context/responses/{responseId}?includeContent=true`; MCP `agent_context_get_response(workspacePath, responseId, includeContent)`; client `GetResponseAsync(responseId, includeContent)`; REPL `client.AgentContext.GetResponseAsync`; PowerShell `New-McpAgentContextGetResponseParams`; tool descriptor `agent_context_get_response`.
- Response 200 `AgentResponseResult` (with every grade). Errors: 404 workspace or `not_found` (`response`), 409 `agent_context_disabled`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-65.svg)

#### QueryAgentResponsesQuery: query responses [FR-MCP-AGENTCONTEXT-003, -004]

- Handler: `QueryAgentResponsesQuery(string WorkspacePath, AgentResponseQuery Query) : IQuery<AgentResponseQueryResult>` and `QueryAgentResponsesQueryHandler` in `src/QBrainAi.Services/AgentContext/Queries/QueryAgentResponsesQuery.cs`.
- REST `GET /qbrainai/agent-context/responses?requestId&packageId&status&trustedOnly&limit&offset`; MCP `agent_context_query_responses(string workspacePath, string? requestId = null, string? packageId = null, string? status = null, bool trustedOnly = false, int limit = 50, int offset = 0)`; client `QueryResponsesAsync(AgentResponseQuery)`; REPL `client.AgentContext.QueryResponsesAsync`; PowerShell `New-McpAgentContextQueryResponsesParams`; tool descriptor `agent_context_query_responses`.
- Response 200 `AgentResponseQueryResult`. Errors: 400 `validation_error`, 404 workspace, 409 `agent_context_disabled`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-66.svg)

#### RunDeterministicGradeCommand: run the deterministic grade [FR-MCP-AGENTCONTEXT-004]

- Handler: `RunDeterministicGradeCommand(string WorkspacePath, string ResponseId) : ICommand<AgentResponseGradeResult>` and `RunDeterministicGradeCommandHandler` in `src/QBrainAi.Services/AgentContext/Commands/RunDeterministicGradeCommand.cs`.
- REST `POST /qbrainai/agent-context/responses/{responseId}/grades/deterministic`; MCP `agent_context_grade_deterministic(workspacePath, responseId)`; client `RunDeterministicGradeAsync(responseId)`; REPL `client.AgentContext.RunDeterministicGradeAsync`; PowerShell `New-McpAgentContextGradeDeterministicParams`; tool descriptor `agent_context_grade_deterministic`.
- Checks, one finding per check and claim: every claim has at least one evidence item; every `File` evidence exists at its commit with the cited hash; every `Requirement` evidence id exists in the requirement store; every `Receipt` path exists with the cited hash; every `SessionLogTurn` evidence resolves; the content, every claim text, and every evidence ref contain no secret; the package is not stale. A check that cannot be evaluated is UNKNOWN.
- Scores: correctness = PASS findings / (PASS + FAIL) over evaluated checks, and 0 when no check could be evaluated (PASS + FAIL = 0); completeness = claims whose every evidence check is PASS / all claims (SubmitAgentResponseCommand guarantees at least one claim, each with evidence); both exact decimals. Any UNKNOWN finding already fails the verdict, so a zero denominator can never yield trust.
- Response 201 `AgentResponseGradeResult`. Errors: 404 workspace or `not_found` (`response`), 409 `agent_context_disabled`, `agent_context_response_not_gradable`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-67.svg)

#### RecordModelGradeCommand: record a model grade from an HV review [FR-MCP-AGENTCONTEXT-004]

- Handler: `RecordModelGradeCommand(string WorkspacePath, string ResponseId, RecordModelGradeRequest Request) : ICommand<AgentResponseGradeResult>` and `RecordModelGradeCommandHandler` in `src/QBrainAi.Services/AgentContext/Commands/RecordModelGradeCommand.cs`.
- Caller: the HV agent (the configured grader, launched as every HV is) after it reviews the turns of an HV request (CreateAgentContextHvRequestCommand). There is no grading worker or HV pipeline in this effort (operator, 2026-10-11).
- REST `POST /qbrainai/agent-context/responses/{responseId}/grades/model`; MCP `agent_context_record_model_grade(string workspacePath, string responseId, string gradeJson)`; client `RecordModelGradeAsync(responseId, RecordModelGradeRequest)`; REPL `client.AgentContext.RecordModelGradeAsync`; PowerShell `New-McpAgentContextRecordModelGradeParams` (also builds the request from the reviewer's verdict: the scores and findings of its `grades` entry for the response copied exactly, the reviewer's own session-log turn ids, its thread id, and the request, response, and rollout jsonl paths); tool descriptor `agent_context_record_model_grade`.
- Response 201 `AgentResponseGradeResult` with `hvRequestStatus` (`Completed` once every item of the HV request is graded).
- Errors: 400 `validation_error`, `review_score_format`, `review_score_precision`, `review_score_out_of_range`, `agent_context_receipt_paths_required`; 403 `agent_context_grader_not_allowed`; 404 `not_found` (`response`, `hv_request`); 409 `agent_context_disabled`, `agent_context_response_not_gradable`, `agent_context_grade_not_in_hv_request`, `agent_context_hv_request_closed`; 422 `agent_context_receipt_unverified`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-68.svg)

#### DecideAgentResponseTrustCommand: decide trust (internal, after every grade) [FR-MCP-AGENTCONTEXT-004]

- Handler: `DecideAgentResponseTrustCommand(string WorkspacePath, string ResponseId) : ICommand<AgentResponseTrustDecision>` and `DecideAgentResponseTrustCommandHandler` in `src/QBrainAi.Services/AgentContext/Commands/DecideAgentResponseTrustCommand.cs`; internal, dispatched through `IDispatcher.SendAsync` by the RunDeterministicGradeCommand and RecordModelGradeCommand handlers inside their transaction, never by a surface.
- Trigger: the RunDeterministicGradeCommand and RecordModelGradeCommand handlers dispatch it after their grade is saved, in the same transaction. No endpoint.
- Rule: a response is `Trusted` only when it is not `Rejected`, `Stale`, or `Superseded`, its latest deterministic grade is `Pass`, and its latest model grade is `Pass` ("latest" as defined in section 6.3: greatest `GradedUtc`, then greatest `GradeId`); otherwise it is `Grading`. The decision runs inside the grade's serialized transaction (per-response serialization rule, section 6.3), so the stored status always reflects the latest committed grade pair, and a concurrent reject, supersede, or staleness change on the same response is ordered before or after it, never interleaved.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-69.svg)

#### RejectAgentResponseCommand: reject a response [FR-MCP-AGENTCONTEXT-005]

- Handler: `RejectAgentResponseCommand(string WorkspacePath, string ResponseId, RejectAgentResponseRequest Request) : ICommand<AgentResponseResult>` and `RejectAgentResponseCommandHandler` in `src/QBrainAi.Services/AgentContext/Commands/RejectAgentResponseCommand.cs`.
- REST `POST /qbrainai/agent-context/responses/{responseId}/reject`; MCP `agent_context_reject_response(workspacePath, responseId, reason)`; client `RejectResponseAsync(responseId, RejectAgentResponseRequest)`; REPL `client.AgentContext.RejectResponseAsync`; PowerShell `New-McpAgentContextRejectResponseParams`; tool descriptor `agent_context_reject_response`.
- Response 200 `AgentResponseResult` with status `Rejected` (a repeat on a rejected response returns 200 unchanged). Errors: 400 `validation_error` (blank reason), 404, 409 `agent_context_disabled`, `agent_context_response_not_gradable` (already `Superseded`).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-70.svg)

#### SupersedeAgentResponseCommand: supersede a response [FR-MCP-AGENTCONTEXT-005]

- Handler: `SupersedeAgentResponseCommand(string WorkspacePath, string ResponseId, SupersedeAgentResponseRequest Request) : ICommand<AgentResponseResult>` and `SupersedeAgentResponseCommandHandler` in `src/QBrainAi.Services/AgentContext/Commands/SupersedeAgentResponseCommand.cs`.
- REST `POST /qbrainai/agent-context/responses/{responseId}/supersede`; MCP `agent_context_supersede_response(workspacePath, responseId, supersededByResponseId, reason)`; client `SupersedeResponseAsync(responseId, SupersedeAgentResponseRequest)`; REPL `client.AgentContext.SupersedeResponseAsync`; PowerShell `New-McpAgentContextSupersedeResponseParams`; tool descriptor `agent_context_supersede_response`.
- Response 200 `AgentResponseResult` with status `Superseded`. Errors: 400 `validation_error` (self-reference, blank reason), 404 (either response), 409 `agent_context_disabled`, `agent_context_supersede_invalid` (different request, a supersede cycle, or the replacement itself rejected or superseded).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-71.svg)

#### EvaluateStalenessCommand: evaluate staleness [FR-MCP-AGENTCONTEXT-005]

- Handler: `EvaluateStalenessCommand(string WorkspacePath, EvaluateStalenessRequest Request) : ICommand<AgentContextStalenessResult>` and `EvaluateStalenessCommandHandler` in `src/QBrainAi.Services/AgentContext/Commands/EvaluateStalenessCommand.cs`.
- REST `POST /qbrainai/agent-context/staleness/evaluate`; MCP `agent_context_evaluate_staleness(workspacePath, responseIdsJson)`; client `EvaluateStalenessAsync(EvaluateStalenessRequest)`; REPL `client.AgentContext.EvaluateStalenessAsync`; PowerShell `New-McpAgentContextEvaluateStalenessParams`; tool descriptor `agent_context_evaluate_staleness`.
- Also triggered internally before BuildAgentContextPackageCommand gathers prior responses, by RetrieveAgentContextCandidatesQuery for every prior-response candidate before it is returned, and after a requirement update or delete.
- Rule: a non-terminal response is stale when any `depends_on`, `derived_from`, or `supports` target of the response or of its package now hashes differently from its `ToSha256`, when any of its `File`, `Requirement`, `Receipt`, or `SessionLogTurn` evidence (including evidence outside its package) now hashes differently from the evidence's recorded `Sha256`, or when a cited requirement was deleted or superseded (`Staleness_ResponseOnlyEvidenceChanged_MarksStale`); each status change runs in the per-response serialized transaction.
- Response 200 `AgentContextStalenessResult`. Errors: 400 `validation_error` (malformed list, more than 1000 ids, an id without the `rsp-` prefix), 404 workspace, 409 `agent_context_disabled`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-72.svg)

#### CreateAgentContextRelationshipCommand: create a relationship [FR-MCP-AGENTCONTEXT-006]

- Handler: `CreateAgentContextRelationshipCommand(string WorkspacePath, CreateAgentContextRelationshipRequest Request) : ICommand<AgentContextRelationshipResult>` and `CreateAgentContextRelationshipCommandHandler` in `src/QBrainAi.Services/AgentContext/Commands/CreateAgentContextRelationshipCommand.cs`.
- REST `POST /qbrainai/agent-context/relationships`; MCP `agent_context_create_relationship(workspacePath, relationshipJson)`; client `CreateRelationshipAsync(CreateAgentContextRelationshipRequest)`; REPL `client.AgentContext.CreateRelationshipAsync`; PowerShell `New-McpAgentContextCreateRelationshipParams`; tool descriptor `agent_context_create_relationship`.
- Response 201 with `created: true`, or 200 with `created: false` for an identical existing link. Errors: 400 `agent_context_relationship_invalid`, 404 `not_found` (`ref`), 409 `agent_context_disabled`, `agent_context_supersede_invalid` (a `supersedes` cycle).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-73.svg)

#### DeleteAgentContextRelationshipCommand: delete a relationship [FR-MCP-AGENTCONTEXT-006]

- Handler: `DeleteAgentContextRelationshipCommand(string WorkspacePath, string RelationshipId) : ICommand<bool>` and `DeleteAgentContextRelationshipCommandHandler` in `src/QBrainAi.Services/AgentContext/Commands/DeleteAgentContextRelationshipCommand.cs`.
- REST `DELETE /qbrainai/agent-context/relationships/{relationshipId}`; MCP `agent_context_delete_relationship(workspacePath, relationshipId)`; client `DeleteRelationshipAsync(relationshipId)`; REPL `client.AgentContext.DeleteRelationshipAsync`; PowerShell `New-McpAgentContextDeleteRelationshipParams`; tool descriptor `agent_context_delete_relationship`.
- Response 204. Errors: 404, 409 `agent_context_disabled`. A `supersedes` link created by SupersedeAgentResponseCommand cannot be deleted while the response stays `Superseded` (409 `agent_context_supersede_invalid`).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-74.svg)

#### QueryAgentContextRelationshipsQuery: query relationships [FR-MCP-AGENTCONTEXT-006]

- Handler: `QueryAgentContextRelationshipsQuery(string WorkspacePath, AgentContextRelationshipQuery Query) : IQuery<AgentContextRelationshipQueryResult>` and `QueryAgentContextRelationshipsQueryHandler` in `src/QBrainAi.Services/AgentContext/Queries/QueryAgentContextRelationshipsQuery.cs`.
- REST `GET /qbrainai/agent-context/relationships?nodeKind&nodeRef&type&direction&limit&offset`; MCP `agent_context_query_relationships(string workspacePath, string? nodeKind = null, string? nodeRef = null, string? type = null, string direction = "both", int limit = 50, int offset = 0)`; client `QueryRelationshipsAsync(AgentContextRelationshipQuery)`; REPL `client.AgentContext.QueryRelationshipsAsync`; PowerShell `New-McpAgentContextQueryRelationshipsParams`; tool descriptor `agent_context_query_relationships`.
- Response 200 `AgentContextRelationshipQueryResult`. Errors: 400 `validation_error` (node kind without ref, unknown type or direction), 404 workspace, 409 `agent_context_disabled`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-75.svg)

#### RetrieveAgentContextCandidatesQuery: retrieve candidate context [FR-MCP-AGENTCONTEXT-007]

- Handler: `RetrieveAgentContextCandidatesQuery(string WorkspacePath, AgentContextRetrieveRequest Request) : IQuery<AgentContextRetrieveResult>` and `RetrieveAgentContextCandidatesQueryHandler` in `src/QBrainAi.Services/AgentContext/Queries/RetrieveAgentContextCandidatesQuery.cs`.
- REST `POST /qbrainai/agent-context/retrieve`; MCP `agent_context_retrieve(workspacePath, requestJson)`; client `RetrieveAsync(AgentContextRetrieveRequest)`; REPL `client.AgentContext.RetrieveAsync`; PowerShell `New-McpAgentContextRetrieveParams`; tool descriptor `agent_context_retrieve`. Also called by BuildAgentContextPackageCommand with the package's `sessionId`, `requestId`, and `focusRefs`.
- When the request names a turn (`sessionId` and `requestId`) or `focusRefs`, the relational path ranks by explicit relationships to that turn, its TODO, its requirements, and the focus refs first (`Retrieve_SameQueryTextTwoTurnNeighborhoods_DifferentRanking`); without them it ranks by the hybrid search alone. Every `PriorResponse` candidate is checked for staleness (EvaluateStalenessCommand) before it is returned, so a response whose dependency changed is marked `Stale` and omitted (`Retrieve_AfterEvidenceFileChange_ResponseStaleAndExcluded`). The relational path then ranks by the existing hybrid search (FTS5 BM25 with vector similarity), with ties broken by `sourceRef`. The GraphRAG path queries the projected responses; any fallback uses the relational path and reports `fallbackReason` (`graphrag_disabled`, `graphrag_not_initialized`, `graphrag_not_indexed`, `index_in_progress`, `retrieval_disabled`, or `graphrag_error`).
- Response 200 `AgentContextRetrieveResult`. Errors: 400 `validation_error` (`trustedOnly: false` with kind `PriorResponse`, limit out of range, only one of `sessionId` and `requestId`), 404 workspace, 404 `not_found` (`session_turn`) for a named turn that does not exist in the workspace (`Retrieve_UnknownTurn_404SessionTurn`), 409 `agent_context_disabled`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-76.svg)

#### ProjectAgentResponsesCommand: project responses into GraphRAG [FR-MCP-AGENTCONTEXT-007]

- Handler: `ProjectAgentResponsesCommand(string WorkspacePath, ProjectAgentContextRequest Request) : ICommand<AgentContextProjectionResult>` and `ProjectAgentResponsesCommandHandler` in `src/QBrainAi.Services/AgentContext/Commands/ProjectAgentResponsesCommand.cs`.
- REST `POST /qbrainai/agent-context/projection/graphrag`; MCP `agent_context_project_graphrag(workspacePath, responseIdsJson)`; client `ProjectToGraphRagAsync(ProjectAgentContextRequest)`; REPL `client.AgentContext.ProjectToGraphRagAsync`; PowerShell `New-McpAgentContextProjectGraphRagParams`; tool descriptor `agent_context_project_graphrag`. Also run by a background pass after DecideAgentResponseTrustCommand, RejectAgentResponseCommand, SupersedeAgentResponseCommand, and EvaluateStalenessCommand mark a projection `Pending`.
- Response 200 `AgentContextProjectionResult`. Errors: 400 `validation_error` (malformed list, more than 1000 ids, an id without the `rsp-` prefix), 404 workspace, 409 `agent_context_disabled`. GraphRAG being disabled is not an error: every selected projection is `Skipped` with `skippedReason: graphrag_disabled`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-77.svg)

#### GetAgentContextPilotReportQuery: pilot report [FR-MCP-AGENTCONTEXT-009]

- Handler: `GetAgentContextPilotReportQuery(string WorkspacePath, AgentContextPilotReportQuery Query) : IQuery<AgentContextPilotReport>` and `GetAgentContextPilotReportQueryHandler` in `src/QBrainAi.Services/AgentContext/Queries/GetAgentContextPilotReportQuery.cs`.
- REST `GET /qbrainai/agent-context/pilot/report?fromUtc&toUtc`; MCP `agent_context_pilot_report(workspacePath, fromUtc, toUtc)`; client `GetPilotReportAsync(AgentContextPilotReportQuery)`; REPL `client.AgentContext.GetPilotReportAsync`; PowerShell `New-McpAgentContextPilotReportParams`; tool descriptor `agent_context_pilot_report`.
- Response 200 `AgentContextPilotReport`. Errors: 400 `validation_error` (missing offset or from after to), 404 workspace, 409 `agent_context_disabled`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-78.svg)

#### CreateAgentContextHvRequestCommand: create an HV request for a range of turns [FR-MCP-AGENTCONTEXT-004]

- Handler: `CreateAgentContextHvRequestCommand(string WorkspacePath, CreateAgentContextHvRequestRequest Request) : ICommand<AgentContextHvRequestResult>` and `CreateAgentContextHvRequestCommandHandler` in `src/QBrainAi.Services/AgentContext/Commands/CreateAgentContextHvRequestCommand.cs`.
- Caller: any agent (operator, 2026-10-11: "Agents will specify a range of session turns to be HV, the HV agent will mark the grade you have been working.").
- REST `POST /qbrainai/agent-context/hv-requests`; MCP `agent_context_create_hv_request(string workspacePath, string sessionId, string agent, string fromRequestId, string toRequestId, string requestedBy, string reason)`; client `CreateHvRequestAsync(CreateAgentContextHvRequestRequest)`; REPL `client.AgentContext.CreateHvRequestAsync`; PowerShell `New-McpAgentContextCreateHvRequestParams`; tool descriptor `agent_context_create_hv_request`.
- Response 201 `AgentContextHvRequestResult` (Location `/qbrainai/agent-context/hv-requests/{hvRequestId}`) with its items: every agent response whose `RequestId` lies in the range and whose status is not `Rejected` or `Superseded`, in turn order, each `Pending`.
- Review contract: the brief for an HV request asks the reviewer to emit in its `=== VERDICT JSON ===` the `hvRequestId` and `grades`, one entry per item `{ responseId, contentSha256, correctness, completeness, findings [{ claimId, check, outcome }] }` naming every claim of the response, and to store its entire result on its own session-log turn; RecordModelGradeCommand accepts a grade only when it equals that entry and that turn (section 6.3).
- Errors: 400 `validation_error` (blank fields), `agent_context_hv_request_invalid` (unknown session, an endpoint turn not in the session, from after to, a range longer than `HvRequest:MaxTurns`, or a range with no gradable agent response); 404 workspace; 409 `agent_context_disabled`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-79.svg)

#### GetAgentContextHvRequestQuery: get an HV request [FR-MCP-AGENTCONTEXT-004]

- Handler: `GetAgentContextHvRequestQuery(string WorkspacePath, string HvRequestId) : IQuery<AgentContextHvRequestResult>` and `GetAgentContextHvRequestQueryHandler` in `src/QBrainAi.Services/AgentContext/Queries/GetAgentContextHvRequestQuery.cs`.
- REST `GET /qbrainai/agent-context/hv-requests/{hvRequestId}`; MCP `agent_context_get_hv_request(string workspacePath, string hvRequestId)`; client `GetHvRequestAsync(hvRequestId)`; REPL `client.AgentContext.GetHvRequestAsync`; PowerShell `New-McpAgentContextGetHvRequestParams`; tool descriptor `agent_context_get_hv_request`.
- Response 200 `AgentContextHvRequestResult` with its items. Errors: 404 workspace or `not_found` (`hv_request`), 409 `agent_context_disabled`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-80.svg)

#### QueryAgentContextHvRequestsQuery: query HV requests [FR-MCP-AGENTCONTEXT-004]

- Handler: `QueryAgentContextHvRequestsQuery(string WorkspacePath, AgentContextHvRequestQuery Query) : IQuery<AgentContextHvRequestQueryResult>` and `QueryAgentContextHvRequestsQueryHandler` in `src/QBrainAi.Services/AgentContext/Queries/QueryAgentContextHvRequestsQuery.cs`.
- REST `GET /qbrainai/agent-context/hv-requests?sessionId&status&limit&offset`; MCP `agent_context_query_hv_requests(string workspacePath, string? sessionId = null, string? status = null, int limit = 50, int offset = 0)`; client `QueryHvRequestsAsync(AgentContextHvRequestQuery)`; REPL `client.AgentContext.QueryHvRequestsAsync`; PowerShell `New-McpAgentContextQueryHvRequestsParams`; tool descriptor `agent_context_query_hv_requests`.
- Response 200 `AgentContextHvRequestQueryResult`. Errors: 400 `validation_error` (unknown status, limit or offset out of range), 404 workspace, 409 `agent_context_disabled`.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-81.svg)

### 6.6 Processes (plugin)

#### Process RunStatelessInvocation: run a stateless invocation (plugin process) [FR-MCP-AGENTCONTEXT-009]

- Not a unit of work: the plugin orchestrates the handlers GetAgentContextStatusQuery (status), BuildAgentContextPackageCommand (build), SubmitAgentResponseCommand (submit), and RunDeterministicGradeCommand (deterministic grade) through the REPL.
- Trigger: a delivered user request (Phase A processes ApplyArrivalBridgeRecord and OpenTurnFromPromptHook) on a host whose plugin reads `Mode` through GetAgentContextStatusQuery.
- PowerShell core: `Invoke-PluginAgentContextInvocation` in `plugin-hook.ps1`, for all nine hosts (the TypeScript hosts through their adapters). Hosts inject the rendered package through their prompt-context mechanism (hook additional context or the host's system-context API).
- Output: in `Stateless` mode the model receives only the rendered package and the request, without prior conversation turns; in `Pilot` mode the package is built and recorded but not injected; in `Off` nothing happens.
- Package failure (operator, 2026-10-11: "Fail the invocation"): in `Pilot` or `Stateless` mode, when the build fails (budget overflow, profile unavailable, commit unavailable, any other error, or an outage), the plugin refuses the prompt with the error code and message shown to the operator, produces no answer, and closes the request's turn `canceled` with that reason (canceled turns are not gated, FR-MCP-SESSIONCLASS-002). This is the only `canceled` a hook ever writes, and only for the failed request's own turn, never another turn (the Phase A isolation rule, refined for this case: `Isolation_HooksNeverPersistCanceled_ExceptOwnTurnPackageFailure`); the plugin removes the request from `openDelivered` and the Stop ledger only after the server confirms the close, so the Stop gate never waits on a turn the plugin itself closed (`StatelessPackageFailure_ClosesOwnTurnCanceled_RemovedFromLedger_NoOtherTurnTouched`). A host whose prompt hook cannot refuse a prompt cannot run `Pilot` or `Stateless`; its plugin treats those modes as `Off` and its feature-matrix row records the limitation.
- History exclusion (operator: "Each agent invocation can run stateless"): in `Stateless` mode the model's input for the request is exactly the rendered package plus the current request. The plugin achieves it only through an authoritative host operation recorded in the host's P0.2 row: replacing the model's message list for that request (opencode `experimental.chat.messages.transform`, whose `output.messages` the adapter replaces with the list the core returns) or starting a fresh host invocation whose only input is the package and the request. A host without such an operation cannot run `Stateless`: its plugin treats `Stateless` as `Pilot` (the package is built and recorded, the model answers statefully) and its feature-matrix row records the limitation (`Stateless_HostWithoutHistoryExclusion_TreatedAsPilot_FeatureMatrixRow`). Each supported host has a host-level test that sends a sentinel prior turn and asserts, on the model request the host actually sends (recorded by the adapter fixture), that the sentinel is absent while the package and the current request are present (`StatelessHost_SentinelPriorTurn_AbsentFromModelRequest`).
- A host recorded `turn_logging_unsupported` (Phase A, D6) has no minted turn, so RunStatelessInvocation does not run there: the plugin stays stateful and reports `turn_logging_unsupported`.
- After the model answers, the response is submitted (SubmitAgentResponseCommand) at turn close and graded deterministically (RunDeterministicGradeCommand); its model grade comes later from an HV request covering its turn (CreateAgentContextHvRequestCommand, RecordModelGradeCommand).

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-82.svg)

### 6.7 Phase C sequence diagrams

In every diagram, `Caller` is any surface (plugin through the REPL, the REPL, the typed client, or an MCP client); REST requests go through `AgentContextController` and MCP requests through `FwhMcpTools.AgentContext`, both dispatching the unit's command or query through `IDispatcher` to its handler (shown as `SVC`). The mode-Off and workspace responses are the same for every request and are drawn once per diagram as the first `alt` branches.

#### Sequence: submit GetAgentContextStatusQuery

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-83.svg)

#### Sequence: submit BuildAgentContextPackageCommand

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-84.svg)

#### Sequence: submit GetAgentContextPackageQuery

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-85.svg)

#### Sequence: submit QueryAgentContextPackagesQuery

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-86.svg)

#### Sequence: submit ReplayAgentContextPackageQuery

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-87.svg)

#### Sequence: submit SubmitAgentResponseCommand

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-88.svg)

#### Sequence: submit GetAgentResponseQuery

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-89.svg)

#### Sequence: submit QueryAgentResponsesQuery

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-90.svg)

#### Sequence: submit RunDeterministicGradeCommand

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-91.svg)

#### Sequence: submit RecordModelGradeCommand from an HV review

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-92.svg)

#### Sequence: submit RejectAgentResponseCommand

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-93.svg)

#### Sequence: submit SupersedeAgentResponseCommand

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-94.svg)

#### Sequence: submit EvaluateStalenessCommand

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-95.svg)

#### Sequence: submit CreateAgentContextRelationshipCommand

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-96.svg)

#### Sequence: submit DeleteAgentContextRelationshipCommand

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-97.svg)

#### Sequence: submit QueryAgentContextRelationshipsQuery

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-98.svg)

#### Sequence: submit RetrieveAgentContextCandidatesQuery

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-99.svg)

#### Sequence: submit ProjectAgentResponsesCommand

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-100.svg)

#### Sequence: stateless invocation end to end (process RunStatelessInvocation)

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-101.svg)

#### Sequence: submit GetAgentContextPilotReportQuery

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-102.svg)

#### Sequence: submit CreateAgentContextHvRequestCommand

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-103.svg)

#### Sequence: submit GetAgentContextHvRequestQuery

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-104.svg)

#### Sequence: submit QueryAgentContextHvRequestsQuery

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-105.svg)

## 7. Use cases

Mermaid has no UML use case diagram, so each use case is drawn as a flowchart: actors are on the left as stadium shapes, the use case is the central oval, and included or extending behaviors are ovals linked with `include` and `extend` labels. UC-1 to UC-9 are the plan's existing use cases (verbatim flows in the plan's "Use cases (new, 9)" section). Sections 7.11 to 7.16 are the agent-context use cases, named by their titles; their records are created in Phase 1 with the FR-MCP-AGENTCONTEXT catalog.

### 7.1 Overview

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-106.svg)

### 7.2 Use case: Log each user request as exactly one turn (plan UC-1) [FR-MCP-SESSIONTURN-001, -002, -003, -007, -008]

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-107.svg)

### 7.3 Use case: Persist session-log writes with truthful receipts (plan UC-2) [FR-MCP-SESSIONTURN-004]

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-108.svg)

### 7.4 Use case: Close every request turn before stopping (plan UC-3) [FR-MCP-SESSIONTURN-005, -006]

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-109.svg)

### 7.5 Use case: Classify a request (plan UC-4) [FR-MCP-SESSIONCLASS-001, FR-MCP-SESSIONREVIEW-006]

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-110.svg)

### 7.6 Use case: Complete or fail a turn under the completion gate (plan UC-5) [FR-MCP-SESSIONCLASS-002, -003, -004]

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-111.svg)

### 7.7 Use case: Record an agent self-review (plan UC-6) [FR-MCP-SESSIONREVIEW-001, -002, -003, -006, -008, FR-SUPPORT-016]

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-112.svg)

### 7.8 Use case: Record a hostile validation review with receipts (plan UC-7) [FR-MCP-SESSIONREVIEW-002, -005]

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-113.svg)

### 7.9 Use case: Configure review pass thresholds (plan UC-8) [FR-MCP-SESSIONREVIEW-007]

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-114.svg)

### 7.10 Use case: Read review scores losslessly (plan UC-9) [FR-MCP-SESSIONREVIEW-004]

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-115.svg)

### 7.11 Use case: Build a context package for a request [FR-MCP-AGENTCONTEXT-001, -002]

Actors: agent host and plugin, server. Precondition: the request's turn exists (UC-1) and the mode is `Pilot` or `Stateless`. Basic flow: the plugin asks the server to build a package for the request; the server gathers the request, profile, rules, applicable requirements, related turns, TODOs, receipts, trusted prior responses, and code at the current commit, filters secrets and untrusted items, ranks, fills the budget, records provenance, and returns the rendered package. Alternate flows: mandatory context exceeds the budget and the build fails with `agent_context_budget_exceeded`; the same idempotency key returns the same package; the commit is unreadable; replay proves the package is reproducible.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-116.svg)

### 7.12 Use case: Submit and grade an agent response [FR-MCP-AGENTCONTEXT-003, -004]

Actors: model, plugin, server, hostile reviewer. Basic flow: at turn close the plugin submits the model's response with its claims and evidence; the server runs the deterministic grade; an agent creates an HV request covering the turn (CreateAgentContextHvRequestCommand) and the HV agent records the model grade (RecordModelGradeCommand) after the receipt and reviewer checks; the server marks the response trusted when both grades pass at 98/98 with zero FAIL and zero UNKNOWN. Alternate flows: a grade fails and the response stays `Grading` with every failure shown; a grader other than the configured one is refused; the operator rejects the response.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-117.svg)

### 7.13 Use case: Retrieve trusted prior context [FR-MCP-AGENTCONTEXT-007]

Actors: server (inside package construction), client apps. Basic flow: retrieval returns ranked candidates, only trusted responses among prior responses, preferring GraphRAG. Alternate flow: GraphRAG disabled, uninitialized, not indexed, or failing, and the deterministic relational path answers with the fallback reason.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-118.svg)

### 7.14 Use case: Maintain relationships, staleness, and supersession [FR-MCP-AGENTCONTEXT-005, -006]

Actors: model, operator, server. Basic flow: links are created between packages, responses, requirements, turns, TODOs, code, and receipts; when a dependency changes, its dependents become stale; a newer response supersedes an older one. Alternate flows: invalid link, cycle, or unresolvable ref refused.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-119.svg)

### 7.15 Use case: Run a stateless or pilot invocation [FR-MCP-AGENTCONTEXT-009]

Actors: operator, agent host and plugin, model. Basic flow: in `Stateless` mode the plugin builds the package for a delivered request and the model answers from the package alone; in `Pilot` mode the package is built and recorded but the model answers statefully; the pilot report compares both before any default changes. Alternate flows: mode `Off` keeps today's behavior; a build failure (operator, 2026-10-11: "Fail the invocation") refuses the prompt with the error code and message shown to the operator, produces no answer, and closes the turn `canceled`; a host whose prompt hook cannot refuse a prompt treats `Pilot` and `Stateless` as `Off`, with the limitation in its feature-matrix row.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-120.svg)

### 7.16 Use case: Project responses into GraphRAG [FR-MCP-AGENTCONTEXT-007]

Actors: server (background pass), operator. Basic flow: trusted responses are ingested into GraphRAG and responses that stop being trusted are removed. Alternate flows: GraphRAG disabled and every projection skipped; an ingest or delete error is recorded and retried on the next pass.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-121.svg)

## 8. Delivery: four execution phases and parallel domain coding

Operator directive (2026-10-11, verbatim): "I want the implementation plan to be organized into requirements as a phase, interfaces and code stubs as a phase for the entire plan, test-driven development unit tests with mocks as a single phase, and then you can begin implementation coding in a domain-driven design breakdown so that agents can be run in parallel for the coding." The plan's section "Execution organization" is authoritative; this section describes it as architecture.

### 8.1 Phase order

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-122.svg)

Each arrow is crossed only after that phase's gates are green with zero failures and zero skips, its hostile validation (Codex gpt-6-astra max) returns AGREE at accuracy and completeness of at least 98 with every claim PASS, and the operator approves.

### 8.2 What each phase delivers

- Prerequisites: every pre-existing failure fixed under its own TODO (P0.1, including the Windows findings), the REPL `generateDocument` validator fix, the Nuke targets every later gate runs through (`Test --test-projects --test-filter`, `TestPluginCore`, `TestAgentPlugins`), and the port of every bats case to Pester (P0.1b; Pester is the only plugin test runner, operator, 2026-10-11: "You should be designing all scripts and script tools around PowerShell and only PowerShell."), and Build.Tests removed from `MigrationIntegrationTests` (P0.1c, TR-MCP-NUKE-014).
- Phase 1, requirements (the plan's P0 steps 1-9): the whole catalog of Phases A, B, and C created in the requirement store from the plan text, the use cases, the feature matrix rows, and the phase and domain TODOs (`REQ-MERGEDPLAN-001`, `DESIGN-MERGEDPLAN-001`, `TEST-MERGEDPLAN-001`, one `IMPL-<CONTEXT>-001` per bounded context, `VERIFY-MERGEDPLAN-001`).
- Phase 2, interfaces and code stubs: every command and query record with a handler stub, every policy and seam interface with a stub, every new entity class and its `IEntityTypeConfiguration<T>` as compiling, unregistered classes (no `DbSet`, no registration in `McpDbContext`, and no new property on an existing entity, because `McpDbContext.cs:301-308` ignores the pending-model-changes warning, so a registered model change without its migration would make queries select columns the database lacks and regress the existing tests that call `Migrate()`; the registered model change, the three migrations, and the S3 backfill belong to the Phase 4 context StorageSchema), every DTO and client mirror with JSON context registration, every controller route, MCP tool, client method, and REPL method (they only dispatch), every plugin function and tool signature, the options classes and `appsettings.yaml` sections, DI, and the golden contract files. Stubs return neutral values and never throw, so Phase 3 tests fail on assertions.
- Phase 3, unit tests with mocks: every unit test the catalog names, written from the acceptance criteria, recorded RED against the stubs, then green against mocks with the oracle and mutant pattern and a recorded negative check each.
- Phase 4, implementation: each bounded context replaces its stubs with the real implementation until its Phase 3 tests pass unchanged, adds its integration tests, and runs its gates; then the contexts merge in the rollout order and the integration gates, live checks, and phase-wide HV run.

### 8.3 Bounded contexts (Phase 4 work packages)

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-123.svg)

Ownership rule: Phase 2 completes every shared file except the registered model change and the migrations (`McpClientJsonContext`, DTOs, REPL allow-lists and validators, `ClientMutationPolicy`, DI, configuration, controllers, MCP tools, client methods, golden contracts, and the unregistered entity and configuration classes), so each context edits only its own files:

- StorageSchema: the registered `McpDbContext` model change (new `DbSet`s and configuration registrations, the new properties on `SessionLogTurnEntity`, the `ResolveWorkspaceIdForAddedEntity` cases), the three migrations per provider, and the S3 backfill.
- SessionTurnLogging: `SessionLogService.cs` and `TransactionGatedSessionLogService.cs`, including every review-related change in them (the S4 service wiring of the review seams, the review sections in `TurnSection` and `ParseSection`, default records at turn creation, the Chore flip, include chains, graph paths, and the S10 write-origin scope with the gate call next to `ValidateTerminalTurnCompliance`), and the bodies of MintSessionTurnCommand, SubmitSessionLogCommand, UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, FailSessionTurnCommand, OpenSessionLogCommand, AppendTurnDialogCommand, SetTurnTitleCommand, SetSessionTitleCommand, GetSessionLogQuery, and QuerySessionLogsQuery, the minter, and the TerminalTurnOutcomeGuard implementation.
- TurnReviews: the S1 seams in `src/QBrainAi.Client/SessionLog/`, the review mapper, validator, normalizer, deriver, defaults, and threshold provider, the `SessionLogSanitizer` review paths, the RecordSessionTurnReviewCommand and ResetSessionTurnReviewCommand bodies, and the RequestClassification, HvVerdictMapping, and CompletionGate implementations; SessionTurnLogging calls them only through the Phase 2 seams, so the two contexts run in parallel.
- ContextPackages: the handler files of GetAgentContextStatusQuery, BuildAgentContextPackageCommand, GetAgentContextPackageQuery, QueryAgentContextPackagesQuery, ReplayAgentContextPackageQuery, and RetrieveAgentContextCandidatesQuery (with its relational path), and `src/QBrainAi.Services/AgentContext/Packages/` (builder, candidate sources, ranker, renderer, token estimator, secret filter, commit reader).
- ResponsesAndTrust: the handler files of SubmitAgentResponseCommand, GetAgentResponseQuery, QueryAgentResponsesQuery, RunDeterministicGradeCommand, RecordModelGradeCommand, DecideAgentResponseTrustCommand, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand, CreateAgentContextHvRequestCommand, GetAgentContextHvRequestQuery, QueryAgentContextHvRequestsQuery, and GetAgentContextPilotReportQuery, and `src/QBrainAi.Services/AgentContext/Responses/` (grader, receipt verifier, staleness).
- ContextGraph: the handler files of CreateAgentContextRelationshipCommand, DeleteAgentContextRelationshipCommand, QueryAgentContextRelationshipsQuery, and ProjectAgentResponsesCommand, the GraphRAG retrieval path behind the retrieval seam, and `src/QBrainAi.Services/AgentContext/Graph/`.
- ReplAndAgents: the REPL normalizer and workflow and the McpAgent workflow files named by S6 and S7.
- PluginCorePowerShell owns `plugins/core/lib-ps`; BuildTooling owns `build/`; HostPluginAdoption owns the nine plugin repos; Documentation owns `docs/` and `templates/`.

A context that needs a change outside its files stops and raises a Phase 2 amendment instead of editing another context's files.

#### Process RunPlanPhases: run the four phases

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-124.svg)

#### Process RunDomainCodingAgent: one Phase 4 work package

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-125.svg)

### 8.4 Gates

- G1 targeted tests through `./build.ps1 Test --test-projects --test-filter`, G2 the unit gate (`./build.ps1 Test`), G3 integration suites, G4 the PowerShell plugin core through `./build.ps1 TestPluginCore`, G5 config and traceability (`./build.ps1 ValidateConfig`, `./build.ps1 ValidateTraceability`), G6 the plugin suites (Pester in all nine repos) through `./build.ps1 TestAgentPlugins`.
- Build.Tests is independent of Nuke and runs directly as the infrastructure check when `build/` changes.
- Windows evidence runs natively on PAYTON-LEGION2 through the operator's WSMan route in a worktree of `F:/GitHub/McpServer`; Linux evidence runs natively on LAB-OMARCHY.

### 8.5 Rollout and rollback

- Merge and deploy order: StorageSchema, SessionTurnLogging, TurnReviews without the CompletionGate enforcement change set, ContextPackages, ResponsesAndTrust, ContextGraph, ReplAndAgents; deploy the server (agent context Mode `Off`); merge PluginCorePowerShell, BuildTooling; sync all nine plugins once at one explicit version (`./build.ps1 SyncAgentPlugins --agent-plugin-parent /home/sharpninja/github --plugin-version 1.119.0`, which carries the Phase A, B, and C plugin work together; the installed-equals-synced step and the operator's codex hook trust follow, and a same-version re-sync, which refreshes content and never bumps the version, runs only when a later Phase 4 fix changes `plugins/core/lib-ps`); merge HostPluginAdoption (then `MAINT-LIBSHRETIRE-001`, and once Codex ACKs the 2026-10-12 PowerShell-only CONTRACT entry or the operator resolves it `MAINT-LIBNODERETIRE-001`); merge and deploy the CompletionGate enforcement change set; Documentation; then the integration checks.
- Mode `Pilot` only on the operator's instruction; the pilot report goes to the operator; `Stateless` as a default is a later operator decision.
- Server deploys run through the Nuke `UpdateService` target with operator approval; releases use Octopus only. Rollback: Mode `Off` stops agent-context behavior without a deploy; migrations roll back only through their tested Down.

### 8.6 End-to-end verification

- Turn logging and reviews: the live checks of the plan's "End-to-end verification" section.
- Agent context with Mode `Pilot` on LAB-OMARCHY and PAYTON-LEGION2: a delivered request builds one package with the request's root ids; replay reports `identical: true`; an over-budget fixture refuses the prompt with `agent_context_budget_exceeded` and closes the turn `canceled`; a submitted response passes the deterministic grade, an HV request covering its turn is created, the HV agent records a passing model grade with verified receipts, and the response becomes `Trusted`; a later package includes it as a prior response; changing a file it depends on makes it `Stale`; with GraphRAG disabled retrieval reports `graphrag_disabled` and answers from the relational path; with GraphRAG enabled the trusted response is projected and a rejected one removed.

## 9. Diagram index

- Activity diagrams, one per unit of work (each a CQRS command or query): turn logging (MintSessionTurnCommand, SubmitSessionLogCommand, UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, FailSessionTurnCommand, OpenSessionLogCommand, AppendTurnDialogCommand, SetTurnTitleCommand, SetSessionTitleCommand), reviews (RecordSessionTurnReviewCommand, ResetSessionTurnReviewCommand, GetSessionLogQuery, QuerySessionLogsQuery), and agent context (the twenty-three handlers of section 6.5).
- Activity diagrams for policies (TerminalTurnOutcomeGuard, RequestClassification, HvVerdictMapping, CompletionGate) and processes (ApplyArrivalBridgeRecord, OpenTurnFromPromptHook, BindVerbToTurn, PersistTurnWriteWithReceipt, DrainFailsafe, EvaluateStopGate, ValidateHookRegistration, StartHostSession, EndHostSession, SyncAgentPluginsAtVersion, RunArrivalBridge, ConvertHvReceiptToReview, ClearSession, AdvisoryCompletionPrecheck, PreserveReviewScoreLexemes, RunStatelessInvocation, RunPlanPhases, RunDomainCodingAgent).
- Sequence diagrams: one per command or query submitted by a surface, each processing every response (sections 4.5, 5.5, 6.7), plus the plugin-verb sequences (turn writes and closes through SubmitSessionLogCommand, dialog, turn title, classification) and the process sequences for DrainFailsafe and RunStatelessInvocation.
- Use case diagrams: the overview and every use case (section 7).
- State diagrams: request lifecycle (4.1), review status (5.1), response lifecycle (6.3). Entity relationship diagrams: 6.3 and Addendum A. System context: 2. Phase order and bounded contexts: 8.1, 8.3.

## Addendum A. Entity diagram with relationships

Operator directive (2026-10-11, verbatim): "I want an entity diagram with relationships as an addendum".

This diagram shows every entity the merged plan adds or changes, and every existing entity those reference, with keys and relationships. Existing entities are read from `src/QBrainAi.Storage/Entities` (properties and keys as they stand at the plan baseline); Phase A and Phase B additions are marked in the attribute comments; Phase C entities are those of section 6.3. Every entity also carries `WorkspaceId` (required, the canonical workspace path, with the named `Workspace` query filter); it is listed once per entity and no relationship crosses workspaces.

Notation:

- `||--o{` and similar solid lines are database foreign keys (cascade or restrict as configured in `McpDbContext`).
- `..` dotted lines (`||..o{`, `}o..o{`) are logical references by root ids or polymorphic refs (for example `SessionId` and `RequestId` strings, or a relationship's `FromKind`/`FromRef`); the store enforces them in the service layer, not with a foreign key.
- Attribute keys: `PK` primary key, `FK` foreign key, `UK` part of a unique index.

![diagram](./20261012T034439Z-mermaid/20261012T034439Z-mermaid-architecture-126.svg)

Relationship notes:

- `SessionLog` to `SessionLogTurn` and every turn child are existing foreign keys; Phase B adds `SessionLogTurnReviewReason` as a new child of the turn (one row per reason, discriminated by `Kind`, ordered by `Ordinal`), and the review fields as columns on the turn row (one record per kind per turn, so no separate review table).
- Phase A adds `SessionLogTurn.IngressNonce` with a unique filtered index per MCP session (WorkspaceId, SessionLogId, IngressNonce), and a second unique filtered index on (WorkspaceId, RequestId) where IngressNonce is set, so a server-minted `RequestId` is unique across the workspace's sessions; legacy rows keep their per-session uniqueness (TR-MCP-SESSIONTURN-004 AC-006).
- `AgentContextPackage` and `AgentResponse` refer to their turn by the root ids `SessionId` and `RequestId` (with `Agent` on the package), not by the turn's numeric `Id`, so packages and responses survive turn soft-delete and federation snapshots that renumber turn rows; the service validates the turn exists when a package is built (BuildAgentContextPackageCommand).
- `AgentContextPackageItem.SourceRef`, `AgentContextRelationship.FromRef`/`ToRef`, and `AgentContextFinding.RelationshipId` are polymorphic or soft references resolved by kind in the service layer; staleness (EvaluateStalenessCommand) compares each reference's current hash with the stored `SourceSha256` or `ToSha256`.
- `AgentResponse.SupersededByResponseId` is a self reference within one request; SupersedeAgentResponseCommand prevents cycles, and a `supersedes` row in `AgentContextRelationship` records the same link with its evidence.
- `AgentContextProjection` has at most one row per response (key `ResponseId`); its `GraphRagDocumentId` is the id of the `ContextDocumentEntity` row (the `Documents` set, id `adhoc-<Guid>`) that `IGraphRagService.IngestTextAsync` creates with its `ContextChunkEntity` children (`src/QBrainAi.GraphRag/Services/GraphRagService.cs:348-372`), always in the workspace scope; removal deletes that document and its chunks (`IGraphRagService.DeleteDocumentAsync`). It never points at a knowledge-graph entity (`GraphEntity`, which stays a separate graph node) or at the global scope.
- Requirement-family keys are composite (`WorkspaceId`, `Kind`, `Id`); TODO keys are (`WorkspaceId`, `Id`); agent-context keys are human-readable strings derived from the turn and session they belong to (`pkg-`, `rsp-`, `grd-`, `rel-`, `hvr-`, section 6.3); every child row of the new tables uses a `long` identity key.
- `AgentContextHvRequest` names a range of turns of one MCP session (logical reference by `SessionId`); its items list the responses in that range when it was created; each model grade names the HV request that produced it (foreign key), and the request completes when no item stays `Pending` (each item `Graded`, or `Closed` because its response was rejected or superseded, section 6.3).
