<!-- SPDX-License-Identifier: Apache-2.0 -->
# Architecture: Turn Logging, Turn Reviews, and Stateless Agent Context

Status: architecture documentation of the merged plan PLAN-SESSIONTURNREVIEW-001 (`docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md`), pending operator approval of the merged plan. Operator directives (2026-10-11, verbatim): "Integrate attached to current plan.", "Reconstruct plan with added requirements", "Also publish the doc to develop", "Outline now, catalog in P0", "No new code until I approve merged plan.", "I want every model defined.  I want every unit of work request and respone defined.  I want every endpoint defined.", and "I want a single, complete activity diagram (mermaid) for every unit of work.  I want sequence diagrams for submitting each request type and processing every response.  I want a md doc describing the who plan as architecture documentation."; for plugins and scripts, "You should be designing all scripts and script tools around PowerShell and only PowerShell.", "This should have already been the standard for plugins.", and "I have not chosen PowerShell for scripting because of Microsoft or Windows. I have chosen it because it is simply the best tool for the job."

This document describes the whole merged plan as architecture: the components, the shared components every unit calls (section 3.5: the CQRS pipeline behaviors, the storage and error components, and the session-log, scoring, agent-context, and plugin-core components, each defined and drawn once), the data each part owns, every unit of work with its request, response, and error contract, one complete activity diagram per unit of work, sequence diagrams for submitting each request type and processing each response, the use cases (section 7), delivery (section 8), and the entity diagrams of Addendum A, grouped by functionality (A.1 the references between the groups, A.2 to A.10 one diagram per group). This document is the authority, and the plan derives from it (operator, 2026-10-12: "The plan should depend on the architecture, not the other way around."): the plan's scope entries, four execution phases, gates, tests, and acceptance criteria cite the decisions, contracts, units, and cases defined here, and a change to this document after the plan is approved is a plan change that needs a new hostile validation. `docs/Stateless-Agent-Context-Management.md` publishes the agent-context part of this document (section 3.5, section 6, use cases 7.11 to 7.16, and delivery 8.1 to 8.6) from the same source text, so the two never differ.

How to read the diagrams:

- Activity diagrams are Mermaid flowcharts read top to bottom. A rounded terminal is a start or an end, a diamond is a decision, a rectangle is an action, and every error exit is drawn and ends in a terminal that names the error code.
- Sequence diagrams show who sends what. Each `alt` block lists every response the caller can receive and how the caller processes it.
- Three kinds of activity diagram: a unit of work is exactly one CQRS command or query with its handler (operator, 2026-10-11: "I want to be clear, unit of work maps to one CQRS handler") and is labelled by the record's name; a policy is a rule a handler applies (for example CompletionGate); a process is plugin, host, REPL, or build orchestration that calls handlers (for example ApplyArrivalBridgeRecord). All labels are meaningful names (operator: "Please use human-readable, meaningful identifiers."). Section 3.5 also draws each shared component once, as the activity diagram of that component.
- A double-bordered node (`[["..."]]`) is a call: a shared component of section 3.5, a policy, or a process used as a step. The calling diagram draws only the exits the call returns, labelled by its typed results; the called steps are drawn once, where the callee is defined.
- Requirement ids in brackets name the functional requirement the unit of work realizes. Ids of the form `FR-MCP-AGENTCONTEXT-*` are reserved for Phase 1 (the plan's P0 steps 1-9), where the agent-context catalog is created.

## 1. Goals

1. One user request is exactly one session-log turn, minted by the server when the message arrives, and every write lands on the turn of the request that caused it (Phase A).
2. Every turn carries a request class and two review records (agent and hostile) with lossless decimal scores, a status lifecycle, configurable thresholds, and a server-enforced completion gate (Phase B).
3. Every agent invocation can run stateless: the server builds a bounded, reproducible context package from the profile, rules, requirements, session-log evidence, trusted prior responses, and current code, and only responses graded at 98/98 with no FAIL or UNKNOWN become trusted context for later requests (Phase C).

The agent-context scope depends on the turn-logging scope (turns are the unit a package is built for, and session-log turns are evidence) and on the review scope (the thresholds and the 98/98 grading rule). Logic that more than one unit of work, policy, or process uses is one shared component (section 3.5), defined once and called from each user (operator, 2026-10-12: "If a pice of logic is hared between multiple UoW, that logic should be encapsulated and shared."). Phases A, B, and C name scope areas; execution runs in four phases across all three (section 8): requirements, interfaces and code stubs, test-driven unit tests with mocks, and implementation by bounded context with parallel coding agents.

## 2. System context

![System context: hosts, plugins, clients, server and store, part 1 of 11](c4/rendered/STR-02-01.svg)

View: STR-02-01

![System context: hosts, plugins, clients, server and store, part 2 of 11](c4/rendered/STR-02-01-part2.svg)

View: STR-02-01-part2

![System context: hosts, plugins, clients, server and store, part 3 of 11](c4/rendered/STR-02-01-part3.svg)

View: STR-02-01-part3

![System context: hosts, plugins, clients, server and store, part 4 of 11](c4/rendered/STR-02-01-part4.svg)

View: STR-02-01-part4

![System context: hosts, plugins, clients, server and store, part 5 of 11](c4/rendered/STR-02-01-part5.svg)

View: STR-02-01-part5

![System context: hosts, plugins, clients, server and store, part 6 of 11](c4/rendered/STR-02-01-part6.svg)

View: STR-02-01-part6

![System context: hosts, plugins, clients, server and store, part 7 of 11](c4/rendered/STR-02-01-part7.svg)

View: STR-02-01-part7

![System context: hosts, plugins, clients, server and store, part 8 of 11](c4/rendered/STR-02-01-part8.svg)

View: STR-02-01-part8

![System context: hosts, plugins, clients, server and store, part 9 of 11](c4/rendered/STR-02-01-part9.svg)

View: STR-02-01-part9

![System context: hosts, plugins, clients, server and store, part 10 of 11](c4/rendered/STR-02-01-part10.svg)

View: STR-02-01-part10

![System context: hosts, plugins, clients, server and store, part 11 of 11](c4/rendered/STR-02-01-part11.svg)

View: STR-02-01-part11

## 3. Cross-cutting conventions, design decisions, and contracts

- Workspace scoping: every request is scoped to one workspace through the `X-Workspace-Path` header (REST), the `workspacePath` parameter (MCP tools, REPL), or `MCP_WORKSPACE_PATH` (plugins); WorkspaceResolutionBehavior (section 3.5.1) resolves it for every command and query, existing ones included, into the `WorkspaceContext` of the caller's dispatch scope (`IDispatcher` becomes scoped), and a record that may run with no workspace (global memory) declares `IAllowsGlobalWorkspace`. Every stored record carries its `WorkspaceId`, and no read ever crosses workspaces.
- Authentication: `X-Api-Key` (or `api_key`) from the workspace marker. Plugins never call REST directly; they use the REPL wrapper.
- Plugin scripting: PowerShell only (operator, 2026-10-11: "You should be designing all scripts and script tools around PowerShell and only PowerShell."). `plugins/core/lib-ps` is the one plugin core for all nine hosts; cline, cline-v2, and opencode keep only the adapter entry file their host API forces, which forwards each host hook invocation the host binding table names (`{ input, output }`; on opencode's `event` hook, which fires for every host event including streaming updates, only an event of type `session.idle`, so no other event starts `pwsh`) to `lib/plugin-hook.ps1 -HostEvent` and applies the returned `output`, forwards each tool call (`{ params, hostContext }`) to `lib/Invoke-McpPlugin.ps1 -InvocationFromStdin`, and registers the tools listed in `lib/tool-descriptors.json`; the host's own build compiles that one file into the git-ignored `dist/`; Pester is the only plugin test runner.
- Routes: every REST route exists under `/qbrainai/...` with the legacy alias `/mcpserver/...`.
- Errors: every REST error of a unit this plan adds or changes is the `ClassifiedError` problem+json body `{ "type", "title", "status", "detail", "code", "message", "retryable", "details" }` emitted through `McpErrorClassifier` (`SessionLogController.cs` and the agent-context controller) from the handler's `ClassifiedErrorException` (section 3.5.2); `code` carries the unit's error code (for example `review_status_conflict`, `agent_context_budget_exceeded`), the HTTP status is the one stated per unit (400 validation, 403 not allowed, 404 not found, 409 conflict or disabled, 422 semantic refusal), and MCP tools return `{ code, error, message, retryable, details }`. A 409 `conflict` carries `retryable: true` when SerializableWrite or key allocation exhausted its retries (four attempts; only the Phase A minter states its own bound, for a collision of its random request id only, six attempts and 500 `mint_collision_exhausted`, while its transient conflicts keep the four attempts and the 409, section 3.5.2), which is also the answer when a deadlock or a busy store outlasts those attempts, and `retryable: false` when an idempotency key is reused with other inputs; the plugin treats a retryable 409 as an outage and keeps its failsafe record (a review excepted: reviews keep their no-failsafe rule, section 3.1); a busy store outside SerializableWrite keeps today's 503 `persistence_error` with `retryable: true` (`src/QBrainAi.Support.Mcp/Services/McpErrorClassifier.cs:129-139` at `a8d3934b`), and unreachable storage keeps its answer (section 3.1, "Deadlock and busy outcomes"). A workspace that does not resolve is 400 `validation_error` with `details.field` `workspacePath` on every surface, the status TR-MCP-MT-002 gives an unregistered workspace (WorkspaceResolutionBehavior, section 3.5.1), and every sequence draws that refusal after the dispatch. On REST, `WorkspaceResolutionMiddleware` still answers a missing or unknown `X-Workspace-Path` before any controller with today's `{ error }` bodies (400 for an unknown path and 404 for a workspace-required route with none, `src/QBrainAi.Support.Mcp/Middleware/WorkspaceResolutionMiddleware.cs:106-111,171-177` at `a8d3934b`). Those middleware bodies, and every existing body a unit keeps as today (stated per unit, for example the dialog route's `{ error }` 400 and the "Invalid turn body." problem), are outside this plan's error rule: they stay as they are on develop, and FR-MCP-TRIAGEERR-001's acceptance criterion of a machine-readable envelope on every REST failure is recorded as deliberately left open for them, as it is on develop. On success, `sessionlog_record_review` returns `{ success: true, review }` with the review record (`SessionLogTurnReviewDto`), each `agent_context_*` tool returns its unit's result DTO as camelCase JSON, and every existing tool keeps its shape (sections 4.2 and 5.2); MCP carries no HTTP status, so a status a diagram shows is the REST status, and an MCP caller tells a created record from an existing one only where the result says so (`created` on a relationship). The completion gate is the one exception to the code rule: its `code` is `validation_error` and its message starts with the token `session_turn_completion_gate` followed by the missing items in the order requestClass, agentReview, hostileReview (FR-MCP-SESSIONCLASS-002-AC006); the diagrams name that refusal by its token. Existing routes whose behavior this plan does not change keep today's error bodies (for example the `{ error }` 400 of the dialog route and the "Invalid session identifier." problem of the open and title routes), stated per unit, and a body-binding failure keeps today's validation problem with `errors` keyed by JSON path (section 3.4, Errors, which reconciles FR-SUPPORT-012). The REPL error envelope is `type: error` with `code`, `message`, and `retryable`.
- Identifiers: every identifier is human-readable and meaningful (operator, 2026-10-11: "Please use human-readable, meaningful identifiers."). Session ids `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`; server-minted request ids `req-<yyyyMMddTHHmmssZ>-<8 hex>`; agent-context record keys derive from the turn and session they belong to (section 6.3): packages `pkg-<requestId>-<nn>`, responses `rsp-<requestId>-<nn>`, grades `grd-<responseId>-<kind>-<nn>`, relationships `rel-<type>-<yyyyMMddTHHmmssZ>-<nn>`, HV requests `hvr-<sessionId>-<yyyyMMddTHHmmssZ>`; ReadableKeyAllocator (section 3.5.2) allocates every one of these keys and the server-minted request ids. Document labels use the command, query, process, policy, and shared component names. Namespaces say where a type goes and who owns it, and type and member names say what it is and what it does (section 3.5).
- Root ids: every record carries `WorkspaceId` and the root ids of every parent it hangs from, a turn's root id being its whole turn key (`Agent`, `SessionId`, `RequestId`): a turn child the turn key; a package child `PackageId` and the turn key; a response child `ResponseId`, `PackageId`, and the turn key; a grade those and, for a model grade, `HvRequestId`; an HV request item `HvRequestId`, `ResponseId`, `PackageId`, and the turn key (section 3.1, "Root ids"; section 6.3).
- Changes to existing behavior: no phase is purely additive. Phase C adds tables, endpoints, and the `Mcp:AgentContext:Mode` setting, but the shared components it brings change behavior every existing caller sees, in every Mode: `IDispatcher` in the published `QBrainAI.Cqrs` package becomes scoped and WorkspaceResolutionBehavior runs on every command and query (section 3.5.1); a deadlock or busy store that outlasts SerializableWrite's attempts answers 409 `conflict` with `retryable: true` where develop answers 503 (section 3.5.2); the published `QBrainAI.Client` package gains the new DTOs, `ClassifiedErrorException`, `ErrorCodes`, `PageResult`, and `ScoreLexeme` (section 8.1, "Published packages", states each package's version effect: the client additions a minor version, the scoped `IDispatcher` a major version under semantic versioning). Phases A and B change turn identity (server-minted request ids), turn writes (turn-level isolation, `closed` refused on agent surfaces, the completion gate), and the plugin's per-turn store; the TypeScript hosts' tool audit moves into the PowerShell core and keeps its `tool_call` and `tool_result` dialog items but writes no `design_decision` action per tool event (section 3.2, Tool-audit hook). Where this document states a behavior that replaces a live requirement record, it names the record inline once, in the form "(replaces ...)", so the plan lists every update.
- Secrets: no API key, token, password, or credential is ever stored in a context package or a response, and none is ever returned by a read surface; turn text is stored as written, searched as stored, and redacted on every read surface (FR-MCP-SESSIONLOGSAN-001, its ac4 for the search; section 3.4, Search); `SecretRedactor` (section 3.5.2) is the one redactor behind the session-log sanitizer's review paths, the turn read projection, and the agent-context secret filter and secret scan.
- Process: the plan runs in the four execution phases of section 8 under the Byrd Development Process v4 (requirements, tests red, mocks green, implementation green, full gates with zero failures and zero skips); each prerequisite, the end of Phase 1, Phase 2, and Phase 3, each bounded context at the end of its Phase 4 work package, and the phase-wide Phase 4 HV end with a Codex gpt-6-astra max hostile validation at accuracy and completeness of at least 98 with every claim PASS. Builds and tests run as Nuke targets natively on Windows or Linux; GitHub Actions and Azure Pipelines are not part of any requirement or process; Octopus (container on LAB-OMARCHY) is used only for releases.
### 3.1 Design decisions

Moved verbatim from the plan on 2026-10-12 (operator: "The plan should depend on the architecture, not the other way around."). Delivery references inside this text (P0 steps, slices A1-A9 and S1-S11, TODO ids, gates) name the plan's delivery units; the decisions and contracts themselves are defined here, and the plan cites them.

Locked operator decisions of 2026-10-09:

Turn logging
- Every user request (including messages queued mid-turn) is exactly one turn; actions and decisions map to the turn of the request that caused them, determined by the write-attribution rule below (D2). Enforcement of turn-logging correctness is Phase A and ships first.
- Plugin turn edits are independent: opening, editing, closing, or canceling one turn never changes another turn's local or server state; concurrent edits to different turns do not interfere.
- Unsupported-session failsafe (operator, 2026-10-10: "If QBrain is truly not available, use the failsafe already in the plugin." and, choosing between a failsafe capture and a warn-only Stop, "1"): when a host session fails closed under D6 (`unsupportedSince`), the model's session-log writes for a turn that was already open at that moment go to the plugin's existing failsafe instead of being refused (reviews excepted: they keep their no-failsafe rule and are recorded after the drain through the original pair), so the Stop gate's remedy stays accepted; the captured records drain at the next SessionStart through the trusted recovery flow with the turn's original pair (C5); every mint and every write for any other turn stays refused (FR-MCP-SESSIONTURN-005-AC001).
- Clear-session closing rule (operator, 2026-10-10, choosing option A over leaving blocked and withdrawn turns open: "A"): clear-session completes each turn that meets the completion policy, fails each withdrawn turn (reason "withdrawn before delivery") and each turn the model marks blocked (with its blocker reason; `failed` requires `requestClass` only, FR-MCP-SESSIONCLASS-002-AC002), and leaves every other turn in progress, reported by requestId with its exact missing items, never fabricating reviews (TR-MCP-CLEARSESSION-001-AC001). Definition (catalog cross-reference, not a change to the decision): "marked blocked" means the turn's `blockers` section (the existing decision-list field, FR-MCP-SESSIONTURN-006-AC001) holds at least one nonblank model-authored entry, read by the clear-session coordinator for that turn by its original (`sessionId`, `requestId`) pair, and the first entry is the fail reason; precedence per turn: withdrawn -> `failTurn` ("withdrawn before delivery"); else blocked -> `failTurn` with the blocker reason (blocked takes precedence over a ready completion); else meets the completion policy -> `completeTurn`; else stays in progress, reported with its exact missing items. Close prerequisites (catalog cross-reference, not a change to the decision): before clear-session fails a withdrawn or blocked turn, the model writes by that turn's original pair, within the same clear-session run and before its session-end step, every prerequisite the plugin and the server require for that close, in the Stop gate remedy order (FR-MCP-SESSIONTURN-005-AC009): a designDecision when the turn has no model evidence (FR-MCP-SESSIONTURN-005-AC002), then a `requestClass` when it is missing or `Unclassified`, judged from the turn's `queryText`; the coordinator then fails the turn with its reason (TR-MCP-CLEARSESSION-001-AC001).
- Request identity (operator D1, 2026-10-09: "have the server issue a request token when a message arrives"): the server mints every turn's `requestId` when a user message arrives (ingress), through the new mint operation `POST /qbrainai/sessionlog/{agent}/{sessionId}/mint`. The mint call carries an idempotency nonce derived from the host's own identity for that message event (host binding table, section 3.2): a host per-prompt id, or a SHA-256 of host event fields that every delivery of that event carries identically. Every delivery of one event therefore sends the same nonce and receives the same token, including network retries and failsafe replay on every host and, on hosts whose nonce is a stable per-message id (codex `turn_id`, grok and grok-bot `promptId`, cline-v2 `runId`, opencode `messageID`) or the bridge's single ordered source (claude-code, claude-cowork), two hook processes started by a doubly registered hook (on copilot and classic cline a second registration is refused before any mint, and a second new equal-field invocation fails closed); two separate user messages (including two intentionally identical prompts) are two events and receive two tokens. On copilot and classic cline, whose event fields include no per-message id, a message whose session, timestamp, and prompt all equal an already minted one is indistinguishable from a redelivery of it, so the host session fails closed under D6 instead of merging or guessing (host binding table, equal-field rule). The host id is only the idempotency key; the turn identity is the server-issued token. There is no prompt-text dedupe window. Every mint comes from a host arrival mechanism (a prompt hook, the arrival bridge, or a host-native hook adapter); no supported host relies on the model to mint. The stop gate's `duplicate_delivery_suspected` warning is a diagnostic only and is never counted as D1 compliance.
- Queued-message arrival (operator D5, 2026-10-09: "Arrival bridge"): a message the operator sends while a turn is running is minted when it is enqueued, not when the model later processes it. On hosts that record the enqueue in a transcript (Claude Code), a per-host-session arrival bridge started by the SessionStart hook tails the transcript and follows each queued message through the lifecycle the transcript records. Observed in this workspace's Claude Code transcripts on 2026-10-09: 117 `enqueue`, 79 `dequeue`, 35 `remove` (27 with reason `absorbed_mid_turn`), 2 `popAll`; a normal prompt is `enqueue` then `dequeue` within milliseconds, a mid-turn message is `enqueue` then `remove` with `absorbed_mid_turn` when it is injected into the running turn (for example the operator's mid-turn message beginning "write a global memory with the" was removed with `absorbed_mid_turn` at 2026-10-09T14:35:47.730Z), and `popAll` pulls queued text back into the input box. The bridge mints at `enqueue` (state `queued`); the request becomes `delivered` at `dequeue` (applied by the bridge from the transcript itself; on hosts with a bridge the prompt hook neither mints nor claims) or at `remove` with `absorbed_mid_turn`; it becomes `withdrawn` at `popAll` or a `remove` with any other or no reason. Task notifications and other system events that the host also enqueues are classified and never minted; on bridge hosts they keep their queue position (`kind: system`), so their `dequeue` never delivers a user request.
- Hosts without an arrival mechanism (operator D6, 2026-10-09: "Adapters or unsupported"): a host is supported for turn logging only through an arrival mechanism this plan specifies (a prompt hook that fires at enqueue, the claude-code/claude-cowork arrival bridge, or the classic-cline native-hook adapter); every other host is unsupported until a separately approved adapter exists, and a queue or enqueue event that P0.2 records on such a host becomes a traced follow-up adapter TODO (filed in P0.8) for operator approval (host binding table, section 3.2). A host with no arrival mechanism is declared unsupported for turn logging until it gains one: its plugin reports `turn_logging_unsupported` on every session-log verb and writes nothing (a host session that becomes unsupported after one of its requests was delivered follows the host binding table unsupported-session rule and the 2026-10-10 failsafe decision), and no first-sight minting is used anywhere.
- Plugin equivalence (operator D7, 2026-10-09, confirmed in chat "1 yes" for the requirement and its qualification posted by Codex in `MEMORY-PROCESS-007` at 17:48:48Z and 17:52:06Z): all QBrain.AI plugins keep equivalent functionality; shared capabilities, contracts, fixes, and behavior changes go to every plugin; host-specific adaptations preserve equivalent supported behavior; equivalence is limited by what each host's plugin infrastructure can support, no extraordinary effort is spent forcing unworkable equivalence, and each such limit is documented as a feature-specific capability limitation instead of being claimed as equivalent. A D6 `turn_logging_unsupported` host is such a documented limitation. The limitations live in `docs/AGENT-PLUGIN-FEATURE-MATRIX.md` (one row per plugin and feature: supported, adapted, or limited, with the reason and the receipt), first written in P0.8 from the P0.2 receipts, updated in A9 and S11, and checked by a docs contract test.
- Ninth plugin (operator D8, 2026-10-09: "2 yes"): `mcpserver-grok-bot-plugin` is in scope like the other eight.
- Write attribution (operator D2, 2026-10-09: tie each write to its request automatically through the agent app's hooks): every verb invocation is bound to one request automatically from host context, without the model naming it: the binding is the write target of the invoking host session at the moment the invocation starts, captured once and stored immutably with that invocation (its persist record and failsafe record), so a later message never retargets an invocation already started. A host session's write target is the request most recently delivered to the model in that host session: a prompt the host hands to the model as a new turn (the prompt hook on hosts without a bridge, or the bridge's `dequeue` delivery), or a queued message absorbed into the running turn (`absorbed_mid_turn`). Minting alone never moves the write target, so work the model is still doing for request R1 after R2 is enqueued stays on R1 until R2 is delivered. Explicit `requestId` remains accepted as an override, for example to continue R1 work after R2 was absorbed, and it is required while two or more delivered requests are open in the host session (D9). Attribution therefore maps each write to the request most recently delivered to the model in its host session when the write started. A write is never attributed by guesswork: when a verb cannot read its host session key from its own invocation context (host binding table, section 3.2), it binds only if exactly one live host session of its agent exists in the workspace (warning `host_session_inferred`); with none it is refused (`host_session_unknown`), and with more than one it is refused (`host_session_ambiguous`), in both cases unless it carries the trusted recovery or review-backfill pair (original MCP `sessionId` and `requestId`, verified against the server for this workspace and agent, C5); an explicit `requestId` alone never lets a verb without a host session key write. No verb ever targets the workspace-level `current-turn.yaml`.
- Ambiguous attribution (operator D9, 2026-10-09: "Refuse when ambiguous"): no host supplies a per-action signal of which request caused a write, so automatic binding applies only while exactly one delivered request is open in the host session. While two or more delivered requests are open (for example R2 absorbed mid-turn while R1 is still open), a write without an explicit `requestId` is refused with `request_ambiguous`, naming the open requests; the model passes `requestId` until all but one are closed. Writes are never attributed by guesswork.
- Copilot duplicate deliveries (operator D11, 2026-10-09: "Supported only if single hook"): Copilot gives no stable per-prompt id and separately registered hooks may see different `timestamp` values, so Copilot turn logging is supported only while the plugin's duplicate check proves exactly one MCP prompt hook across all Copilot hook sources; otherwise the Copilot prompt hook reports `turn_logging_unsupported` before any mint, and the condition is a D7 documented limitation until fixed. One Copilot prompt therefore never becomes two turns.
- Node runtime floor (operator D12, 2026-10-09: "Keep Node 18/20"), superseded for core code by the 2026-10-11 PowerShell-only ruling: no code of this plan runs on Node except the logic-free TypeScript-host adapters, which keep each host's declared engine unchanged (mcpserver-cline-plugin, mcpserver-cline-v2-plugin, and mcpserver-opencode-plugin `>=18`; the Cline application's own `node >=22` is unaffected). Exact review scores are validated and carried by the PowerShell core's raw-lexeme rules (S8); the adapters never parse a score (C10).

Request class
- Turn scalar `requestClass`: `Code` | `Docs` | `Chore` (case-insensitive in, canonical PascalCase out, anything else rejected). Required, enforced at transition to `completed` or `failed`, not at creation (hooks create turns before the agent can classify). Legacy rows backfilled to `Unclassified` (readable; never accepted on new writes from agent surfaces).

Review record (shared by `agentReview` and `hostileReview`)
- Fields: `status` (`Unnecessary` | `Unstarted` | `Running` | `Pass` | `Fail` | `Other`), `otherNotes`, `correctness`, `completeness` (lossless decimal 0..1), `correctnessThreshold`, `completenessThreshold` (server-stamped, read-only), `reasons` (string[]), `reviewer`, `reviewedAt` (UTC), `requestJsonlPath`, `responseJsonlPath`. No `passed` bool.
- One record per kind per turn; latest write wins.
- Server creates both at turn creation as `Unstarted`. `hostileReview` flips to `Unnecessary` when `requestClass` becomes `Chore`, and back to `Unstarted` if the class changes away while still `Unnecessary`. Records past `Unstarted`/`Unnecessary` are never auto-changed. Legacy turns are backfilled `agentReview=Unstarted`, `hostileReview=Unstarted`. (Catalog cross-references, not a change to the decision: the flip and its reverse apply only to in-progress turns, FR-MCP-SESSIONREVIEW-006-AC002 and UC-4; a reset restores the creation default for the turn's current class, FR-MCP-SESSIONREVIEW-001-AC004.)
- `otherNotes` with text is rejected unless `status` is `Other`.
- Strict per-status fields: `Unnecessary`/`Unstarted` carry no scores, reasons, reviewer, timestamps, or paths; `Running` requires `reviewer`, forbids scores; `Pass`/`Fail` require `correctness`, `completeness`, `reviewer`, `reviewedAt`; `hostileReview` `Pass`/`Fail` also require `requestJsonlPath` and `responseJsonlPath`; `Other` requires non-empty `otherNotes` (the Other reason; operator D4, 2026-10-09: "Other requires Other Reason be populated") and allows optional scores, `reasons`, `reviewer`, `reviewedAt`, and receipt paths. `Unnecessary` is server-managed only.
- Thresholds: `Mcp:SessionLog:ReviewThresholds:{Agent|Hostile}:{Correctness|Completeness}` (decimal 0..1, per instance through `McpInstanceResolver` overrides), default 0.98, inclusive (score >= threshold passes). Pass requires both scores to meet their thresholds. The server derives `Pass`/`Fail`; a supplied `Pass`/`Fail` that contradicts the derivation is rejected (422 `review_status_conflict`); omitting status lets the server derive it. `Other` is never derived. Thresholds in force are stamped on the record.
- Names stay `correctness`/`completeness`. HV tooling maps `accuracy` -> `correctness` and integer percent -> decimal/100.

Completion rules
- `completed` requires `requestClass` plus `agentReview.status` in `Pass`/`Fail`/`Other`; for `Code`/`Docs` also `hostileReview.status` in `Pass`/`Fail`/`Other`; for `Chore` `hostileReview` is not checked (HV not required). `Unstarted`/`Running` block where checked. Pass is not required (Fail/Other complete).
- `failed` requires `requestClass` only. Reviews remain writable on terminal turns.
- Server-internal writers (QuadBrain chat, AuditedAgentCliClient, WorkspacePolicyService, federation replication, transcript/session ingestion) are exempt by code-path origin; they never fabricate reviews.

Locked operator decisions of 2026-10-11 and 2026-10-12 for Phase C and the plugin core:

Stateless agent context (Phase C)
- Opt-in: `Mcp:AgentContext:Mode` is `Off` by default; `Pilot` builds and grades without injecting; `Stateless` injects the package and carries no history on a host whose plugin API can exclude history from the model request (P0.2 probe (j)); any other host treats `Stateless` as `Pilot`. Changing the default is a later operator decision taken from the pilot report.
- Authority: the relational store holds packages, responses, grades, and relationships; GraphRAG holds only a projection, and a deterministic relational fallback answers whenever GraphRAG is disabled, uninitialized, not indexed, or failing.
- Trust: a response is trusted only when its latest deterministic grade and its latest model grade both reach correctness and completeness of at least the trust thresholds (configurable only upward from a floor of 0.98: a value below 0.98 fails startup, and every grade and trust decision applies max(configured, 0.98), inclusive) with zero FAIL and zero UNKNOWN findings; model grades come only from the configured grader (default Codex, `gpt-6-astra`, `max`, the plan's HV reviewer).
- Budget: mandatory context (request, profile, rules, applicable requirements) is never truncated; when it exceeds the budget the build fails with `agent_context_budget_exceeded`, and in `Pilot` or `Stateless` mode the invocation fails (decision below).
- Audit: rejected, stale, and superseded responses are kept and never retrieved as trusted context.
- Package failure (operator, plan-readiness round 1 finding F2, verbatim answer "Fail the invocation"): in `Pilot` or `Stateless` mode, when the package cannot be built (budget overflow or any build error, including a retried or concurrent build that finds the stored `Overflow` package and returns the same 422), the invocation fails: no answer is produced, the plugin refuses the prompt with the error shown to the operator, and the request's turn is closed `canceled` with that reason (canceled turns are not gated, FR-MCP-SESSIONCLASS-002). A host whose prompt hook cannot refuse a prompt cannot run `Pilot` or `Stateless` and records that as a feature-matrix limitation.
- Model grade (operator, round-1 finding F3 and the follow-ups, verbatim: "Server grading worker (Recommended)", then "We are going to add an HV pipeline in the future, but not in this effort." and "Agents will specify a range of session turns to be HV, the HV agent will mark the grade you have been working."): this effort builds no grading worker or HV pipeline. An agent creates an HV request for a range of session-log turns (CreateAgentContextHvRequestCommand); the HV agent (the plan's Codex gpt-6-astra max reviewer, launched as every HV is) reviews that range and then, in a grade-only continuation of its own reviewer thread (`codex exec resume`) after the runner freezes the review receipts, records a model grade for each agent response in it (RecordModelGradeCommand; retry-safe, because an equal resubmission returns the stored grade), which the server accepts only from the configured grader, only for a response inside an open HV request, and only after the eight receipt, reviewer, and runtime-identity checks of the architecture document section 6.3 pass (`receipt_path`, `request_meta`, `verdict_block`, `verdict_binding`, `verdict_match`, `reviewer_turn`, `reviewer_independent`, and `runtime_identity`, which reads the reviewer's own rollout receipt and requires its recorded runtime model and effort to equal the configured grader's; otherwise 422 `agent_context_receipt_unverified` naming the check). Until a response has such a grade it stays `Grading` and is never trusted.
- PowerShell is the only plugin script language (operator, 2026-10-11, verbatim: "You should be designing all scripts and script tools around PowerShell and only PowerShell.", "This should have already been the standard for plugins.", and "I have not chosen PowerShell for scripting because of Microsoft or Windows. I have chosen it because it is simply the best tool for the job."; scope answer, verbatim: "PowerShell core only (Recommended)"). The ruling restores CLAUDE.md Required Usage Rule 8 ("Normal plugin execution must use PowerShell only") and the existing `ValidatePluginPowerShellOnly` target (`build/Build.SyncAgentPlugins.cs:75-87` and `:406-501`), from which the pre-merge plan had drifted by designing a second plugin core in TypeScript. Consequences: `plugins/core/lib-ps` is the one plugin core, and every plugin behavior of this plan is implemented and tested there once, for all nine hosts; `plugins/core/lib-node` receives no new work, and the pre-ruling A8 and S9 (a TypeScript implementation of the same behavior with its own jest copies of the core tests) are replaced by the TypeScript-host bindings in the PowerShell core and the TypeScript-host adapter contract (section 3.2); cline, cline-v2, and opencode keep only the adapter entry file their host API forces, which holds no logic and forwards every host event and tool call to the PowerShell core through `pwsh`; Pester is the only plugin test runner (bats cases ported in P0.1b; npm and jest are in no gate); the plan designs no Node, bash, bats, or Python script or tool (a host's own runtime runs its adapter, and a host that loads compiled output compiles the adapter with its existing build as a step of `SyncAgentPlugins`, never as a test); `plugins/core/lib-node` and `plugins/core/lib-sh` are retired under `MAINT-LIBNODERETIRE-001` and `MAINT-LIBSHRETIRE-001`. The D12 Node engine decision no longer governs core code; each TypeScript host keeps its declared engine. QB-AGENT-001's governance work is built in `plugins/core/lib-node`, so the ruling changes the agreed C1-C12 ownership (section 3.3 and the plan's "Coordination with QB-AGENT-001"): the governed-host protections are ported to PowerShell (next decision), and the change is coordinated through a CONTRACT entry.
- Governed-host protections (operator, 2026-10-12, round-2 finding R2-F10, verbatim answer "Port to PowerShell (Recommended)"): the protections the approved plan gave the TypeScript hosts through the Node core (structured failure causes, a governed context bound to its REPL bridge with no ambient HTTP fallback, and a governed flush through the context-bound failsafe only, keeping its completion blockers and never replaying a foreign-workspace failsafe) are kept and re-specified once in the PowerShell core (TR-MCP-SESSIONTURN-008 AC-010..AC-012, delivered in A8 by PluginCorePowerShell before any TypeScript host adopts the adapter), with their discriminating Pester tests. "No ambient HTTP fallback" governs the verbs; the marker trust check's read-only health and nonce request stays (section 3.1, Governed verbs and the trust check).

Locked operator decisions of 2026-10-12 (architecture round 5):

- Shared components and names (operator, 2026-10-12, verbatim: "If a pice of logic is hared between multiple UoW, that logic should be encapsulated and shared." and "\"Where it goes\" and \"Who Owns It\" are jobs for namespaces.  \"What it is\" and \"What it does\" are class and member names."): logic used by more than one unit of work, policy, or process is one component of section 3.5, drawn once and called from each user; namespaces follow section 3.5's rule.
- Stateless invocation on bridge hosts (operator, 2026-10-12, verbatim answer "Hook syncs, absorbed=Pilot (Recommended)"): on claude-code and claude-cowork, where the arrival bridge is the only minter, the prompt hook applies pending transcript records through `Sync-PluginArrivalLog` up to the transcript length at its own start, takes the bridge-minted `requestId` of the delivered request, and then runs the invocation (process RunStatelessInvocation); when that request's `enqueue` record is not yet in the transcript, the invocation fails (prompt refused, as for a build failure). A message absorbed into a running turn (`absorbed_mid_turn`) has no prompt to refuse and no history to exclude, so it runs as `Pilot` (the package is built and recorded, never injected, and the prompt is never refused).
- Response submission (operator, 2026-10-12, verbatim answer "Model calls submit tool (Recommended)"): in `Pilot` and `Stateless` the model submits its response with the existing `agent_context_submit_response` tool (SubmitAgentResponseCommand) before it closes the turn; the plugin supplies the `packageId` it recorded for the request (found again with QueryAgentContextPackagesQuery `{ agent, sessionId, requestId, status: Built }` when a hook process boundary lost it). A missing submission is not a Stop-gate item; the pilot report counts requests with a package and no response.
- Resumed host sessions (operator, 2026-10-12, verbatim answer "Revive, keep MCP session (Recommended)"): a SessionStart for a host session key whose `ended.yaml` says `session_end` (Claude Code resume and `codex exec resume` reuse the key) removes the ended marker in one atomic replace under `turn-open.lock`, as the idle revival does, and continues in the host session's recorded MCP session (`Invoke-SessionOpen` never replaces a recorded `mcpSessionId`).
- The `closed` status (operator, 2026-10-12, verbatim answer "Refuse from agent surfaces (Recommended)"): agent surfaces write only `in_progress`, `completed`, `failed`, and `canceled`; a write naming `closed` is refused with 400 `validation_error`; server-internal writers and existing rows keep `closed` (`TurnStatus`, section 3.5.3).

Locked operator decisions of 2026-10-12 from the pre-HV requirements reconciliation (round 5):

- Analyzer rules (operator, 2026-10-12, verbatim answer "Analyzers first, adopt rules (Recommended)"): the analyzer branch `chore/netanalyzers-all` merges before this plan's implementation, and every new type and test follows its rules: a namespace is the project's RootNamespace followed by the folder path (IDE0130; for example code in `src/QBrainAi.Services/SessionLog/Commands/` is `QBrainAi.Support.Mcp.SessionLog.Commands`, because the RootNamespace of `QBrainAi.Services` is `QBrainAi.Support.Mcp`), one type per file (MA0048, so a command or query record and its handler are separate files), and identifiers without underscores (CA1707 covers every identifier, so test methods, test classes, and test-only subclasses alike; the named cases of this document and the plan are written in PascalCase, and P0 re-points store evidence that cites a renamed test).
- Workspace resolution (operator, 2026-10-12, verbatim answer "Every command and query"): WorkspaceResolutionBehavior applies to every command and query, existing ones included. `IDispatcher` in `QBrainAi.Cqrs` (a published package) becomes a scoped facade that resolves the handler and the behaviors from the caller's DI scope (the HTTP request scope, the MCP server's per-call scope, or a scope a hosted service creates), so the behavior sets that scope's `WorkspaceContext` and a dispatch made inside a handler or a SerializableWrite delegate shares the caller's `McpDbContext` and transaction (section 3.5.1, "Dispatch scope"). A record with an empty `WorkspacePath` keeps the workspace its transport resolved; a record that may run with no workspace (global memory, FR-MCP-MEMORY-001) declares it with `IAllowsGlobalWorkspace`; the behavior never refuses a dispatch develop serves today. Both composition roots (`Program.cs` and `McpStdioHost.cs`) register the behaviors.
- Turn-level isolation (operator, 2026-10-12, verbatim answer "Turn-level (Recommended)"): a single-turn write (upsert, patch, replace, complete, fail, review record, review reset) isolates the turn it writes and that turn's children: it reads the session row only to locate the parent and writes only that turn and its children, so writes to different turns of one session never answer a conflict (section 3.5.2, isolation scope); a multi-turn submission keeps its session-level Serializable transaction, as `SessionLogService.SubmitAsync` does today. The session summary refresh after a single-turn write is its own short write after the turn commits, and its failure never fails the turn write.
- Code freeze exception (operator, 2026-10-12, verbatim answer "Record an exception (Recommended)"): the operator-directed analyzer work (`chore/netanalyzers-all`) is exempt from the code freeze.

Locked operator decision of 2026-10-13:

- Requirements by problem domain (operator, 2026-10-13, verbatim: "Dividing the new reqs into problem domains would be best split."; clarified answer "Catalog and work by domain (Recommended)"): the plan's requirements catalog is divided into problem domains that follow the bounded contexts of section 8.3 (turn arrival and binding; turn persistence, decision lists, and closing; request class and the completion gate; turn reviews; plugin core runners and host adoption; context packages; responses, grading, and trust; relationships, retrieval, and projection; agent-context surfaces and stateless invocation; the shared platform; build tooling), each domain holding its own requirements, AC ownership, mappings, updated existing records, and test churn, and the derivation and implementation work are split along the same domains. The hostile validation stays one review of the whole document.

Resolutions of the pre-HV requirements reconciliation (coordinator readings that reconcile the design with live requirements, code, and tests; each names what it reconciles and reuses an existing mechanism):

- Workspace failure: one answer on every surface, 400 `validation_error` with `details.field` `workspacePath`, the status TR-MCP-MT-002 gives an unregistered workspace; REST keeps the middleware's own bodies before any controller (section 3.5.1).
- Retry bound: SerializableWrite retries a declared conflict at most three times (four attempts) for every unit; only the Phase A minter states its own bound for one cause, a collision of its random request id (six attempts, then 500 `mint_collision_exhausted`, today's InsertMintedTurnWithRetry); its transient conflicts keep the default bound and the 409 `conflict` exit.
- Mint isolation: MintSessionTurnCommand inserts one turn and is a single-turn write under the turn-level isolation decision above, like the other single-turn writes.
- Soft delete (TR-MCP-DB-003): IdempotentCreate and ReadableKeyAllocator read past the soft-delete filter; a soft-deleted key is revived and written anew, as develop revives a tombstoned session (`SessionLogService.cs:161-169`); a deleted suffix is never reallocated.
- Stored normal form: `McpDbContext` replaces em and en dashes in every saved string (`LineSanitizer.Sanitize`); ContentHasher hashes, and every byte-for-byte check compares, that normal form.
- Database-only delegates: a SerializableWrite delegate does no git, file, source, or GraphRAG read; the unit reads first (after a read-only idempotency lookup) and the delegate re-checks the database state those reads relied on (section 3.5.2). Build therefore reads staleness read-only and persists nothing but its package and links; only EvaluateStalenessCommand persists `Stale`. A grade unit (RunDeterministicGradeCommand, RecordModelGradeCommand) runs every read-only check it has (the response, the HV request, the evidence checks and secret scan of a deterministic grade, and the receipt, reviewer, and runtime-identity checks of a model grade), then dispatches EvaluateStalenessCommand for the response, then runs its own SerializableWrite, whose delegate re-checks the response's `StatusVersion`; DecideAgentResponseTrustCommand reads only the database. RelationshipWriter takes the target hash its caller read before the write.
- Server errors on the REPL: `ReplMcpErrorClassifier` carries a server `ClassifiedError` answer through at every status (its `code`, `message`, `retryable`, and `details`), where today only a 503 keeps the server's code, `conflict` is forced to `retryable: false`, and any other answer becomes a classifier code or `method_invocation_error` (`ReplMcpErrorClassifier.cs:60-95`, `ReplCommandDispatcher.cs:3346-3370`), so the plugin receives the completion-gate token, the review and agent-context codes, and the server's retryable flag (section 3.5.2).
- Deadlock and busy outcomes: a SerializableWrite exhaustion is 409 `conflict` with `retryable: true`, and the plugin treats it as an outage (failsafe record kept, Resolve-MintOutcome and PersistTurnWriteWithReceipt), so the failsafe outcome of TEST-MCP-TRIAGESTORE-005, TR-MCP-TRIAGEERR-001, and FR-MCP-SESSIONLIFE-003 ac6 holds while their status code changes from 503 to 409; a busy store outside SerializableWrite keeps today's 503 `persistence_error` with `retryable: true` (`McpErrorClassifier.cs:129-139`), and unreachable storage keeps its answer (TEST-MCP-TRIAGESTORE-007 is unchanged).
- Plugin function home: the section 3.5.6 functions live in `plugins/core/lib-ps/repl-invoke.ps1`, the dot-sourceable library; `plugin-hook.ps1` dot-sources it; the arrival bridge is its own script entry that dot-sources it.
- Failsafe drain (FR-MCP-REPL-011, TR-MCP-REPL-016, TR-MCP-REPL-017, FR-MCP-FAILSAFE-001, FR-MCP-SESSIONLIFE-004): the drain keeps the existing quarantine as the one place for records it cannot replay. A retryable answer (transport outage, 503, 409 `conflict` with `retryable: true`) stops the walk without consuming attempts; a final answer increments the record's `drainAttempts` and the walk continues, skipping only the later records bound to the same turn while that turn's mint or write is unresolved; a record at the attempt budget (5), a malformed record, and a legacy record that names no request (it relied on `current-turn.yaml`) move to the quarantine subdirectory with their `.reason.txt`; a legacy record whose parameters name its session and request replays as today, including the exhausted numeric-keyed records the drain repairs and replays even when `drainAttempts` is exhausted (FR-MCP-SESSIONLIFE-004, `SessionLogQuarantineRepair.Tests.ps1:20`); nothing replays from the quarantine subdirectory; the Stop gate warns, never blocks, while the quarantine holds a record of an open turn.
- Completion gate writers: QBAgent's `mcp_session_turn_complete` (`QuadBrainInternalToolExecutor.cs:314-328`) stays gated and gains the optional `requestClass`, `agentReview`, and `hostileReview` arguments of CompleteSessionTurnCommand, so TR-MCP-SESSIONCLASS-004 covers it; REPL `importRecovery` stays gated: a recovered turn the gate refuses is reported in the import result with the gate's missing items and is never written with a fabricated review, and the REPL's status normalization stays as today (`SessionLogWorkflow.cs:725-750`: `completed-local` and `partial` become `completed`, `interrupted` becomes `failed`), so the one normalized value outside the agent write set, `superseded`, is refused with 400 `validation_error`.
- Turn statuses on agent surfaces: `in_progress`, `completed`, `failed`, `canceled`, and the legacy `cancelled` are accepted (FR-MCP-SESSIONLIFE-002, FR-MCP-TRIAGESTORE-001 ac1, TEST-MCP-TRIAGESTORE-004); `closed` and every other value are refused with 400 `validation_error`.
- Transaction-gated REPL clients (FR-MCP-120, TR-MCP-TXN-001, `ClientMutationPolicy.cs:161-172,188-219`): the agent-context client namespace is service-gated, because each of its writes runs in the server's SerializableWrite, and its read methods join the read allowlist; `Evaluate` drops the `!ReadMethods.ContainsKey(clientName)` clause at `ClientMutationPolicy.cs:213`, which today keeps any namespace with listed reads from being service-gated, and no existing decision changes because no namespace is in both lists today (reads: `context`, `graphRag`, `todo`, `federation`, `keyServer`, `subscriber`; service-gated: `agentPool`, `gitHub`, `memory`, `repo`, `requirements`, `sessionLog`, `template`, `tools`, `voice`); `transaction_degraded` and `mutation_not_transactional` join `ErrorCodes` and the Phase C error list, and a plugin that receives either from a build treats it as a build error.
- Memories in Stateless mode (FR-MCP-MEMORY-003, -007): the model's input is the rendered package, the current request, and the deterministic `REQUIRED MEMORIES` block the host already injects; history stays excluded.
- Root ids (standing rule): every new child row carries `WorkspaceId` and the root ids of every parent it hangs from, where a turn's root id is its whole turn key (`Agent`, `SessionId`, `RequestId`), because a request id is unique only within its session: a turn child carries the turn key; a package child carries `PackageId` and the turn key; a response child and a projection row carry `ResponseId`, `PackageId`, and the turn key; a grade carries those and, for a model grade, `HvRequestId`; a grade finding carries `GradeId` and every root id of its grade; an HV request item carries `HvRequestId`, `ResponseId`, `PackageId`, and the turn key (section 6.3, the rule's full statement). Every operational use of a turn carries or compares the whole key, never the request id alone: the stateless invocation's package recovery (QueryAgentContextPackagesQuery with `agent`, `sessionId`, and `requestId`) and its idempotency key, and the supersede check (sections 6.5 and 6.6).

Resolutions of the plan derivation (coordinator readings of 2026-10-13 for questions the domain plan editors raised; each reuses an existing mechanism):

- Requirement writes and staleness: `RequirementsDatabaseDocumentService`, the database implementation of `IRequirementsDocumentService` (`src/QBrainAi.Services/Requirements/IRequirementsDocumentService.cs:9`), dispatches EvaluateStalenessCommand after the commit of each write that changes or deletes a requirement record or its acceptance criteria (single writes, `UpdateBatchAsync`, `RequirementsDatabaseDocumentService.cs:817`, and the recovery apply and import that write through it), with the `responseIds` of the responses that cite a changed requirement (evidence `Ref`, or a `depends_on`, `derived_from`, or `supports` link to it); a change of the workspace's current requirement layer (`SetWorkspaceCurrentRequirementLayerAsync`, `:343`) dispatches it with no `responseIds`. The dispatch runs outside the requirement transaction and only while Mode is not `Off`; a dispatch that fails or exhausts its retries is logged and never fails the requirement write, because every read evaluates freshness itself. P0 records the side effect on TR-MCP-REQRECOVERY-001 and on the TRs of these `IRequirementsDocumentService` writes.
- Dependent responses (architecture HV round 1, AR1-F2): a response is trusted only while every response it depends on is, so IResponseStalenessRule checks the effective status of each dependency that names a response, recursively (section 3.5.5), and every read reports a dependent stale at once. To persist it, a unit whose commit moved a response out of `Trusted` or into `Rejected`, `Superseded`, or `Stale` (the grade units through the trust decision, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand) dispatches EvaluateStalenessCommand after that commit and before its projection pass, through `IDispatcher`, with the `responseIds` of the responses that depend on it (a `depends_on`, `derived_from`, or `supports` link to it from the response or from its package), only while Mode is not `Off`; a failed dispatch is logged and never fails the unit, every read still reports the dependent stale, and each dependent's own commit dispatches for its dependents in turn, so the chain ends at responses already stale (`ResponseLeavingTrustedDispatchesStalenessForDependentsAfterCommitFailureLoggedUnitSucceeds`).
- Requirement hash: the `Requirement` hash of section 6.3 covers the effective record the builder read (id, title, description, and each acceptance criterion's text in order), not its status, its acceptance criteria's satisfaction flags or evidence, or timestamps, so recording an AC satisfied or an HV verdict never stales a response; a deleted or superseded requirement is stale through IResponseStalenessRule's separate status check (section 3.5.5).
- Projection pass: the background pass of section 6.5 is a post-commit dispatch of ProjectAgentResponsesCommand with no `responseIds` (every `Pending` and `Failed` row, and `Skipped` rows once ingest is enabled) by the unit whose transaction marked a projection row (the grade units through the trust decision, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand), made through `IDispatcher` in the same scope after that unit's SerializableWrite commits and only while Mode is not `Off`; its failure never fails that unit, and a row it leaves is selected by the next pass or an explicit call. Passes can overlap; a save that finds its row already projected by another pass keeps that document and deletes its own, and every pass ends by reconciling the workspace's `agent-response` documents with the projection rows (nested activity ReconcileProjectedDocuments): a document that no `Projected` row names and that was ingested before the pass started is deleted, and a `Projected` row whose document is gone returns to `Pending`. The GraphRAG store, through each document's `sourceKey`, so holds every cleanup obligation, and no interleaving, exhausted save, or failed delete leaves a document past the next complete pass (section 6.5, ProjectAgentResponsesCommand). ProjectAgentResponsesCommand stays ContextGraph's; the dispatch is a cross-context call through its seam (section 8.3). Projection metadata: `IGraphRagService.IngestTextAsync` with `content` the response content, `title` the `responseId`, `sourceType` `agent-response`, and `sourceKey` `agent-context/<responseId>` (`GraphRagIngestTextRequest`, `src/QBrainAi.GraphRag/Models/GraphRagModels.cs:184`), so a GraphRAG hit maps back to its response by its source key.
- Hybrid-search hits as candidates: a chunk maps to its document's `SourceType` and `SourceKey` (`src/QBrainAi.Storage/Entities/ContextDocumentEntity.cs:22,27`): `repo` is a `CodeFile` candidate with the key as its path, `session-log` a `SessionLogTurn` candidate with the key resolved to its turn reference (inference on the key's form, verified in Phase 2), and every other source type (`external-web`, `external-doc`, `github-issue`, `github-pr`, `issue`, `brain-slot-curiosity`, `product-requirements`, `adhoc-text`, `agent-response`) is not a candidate kind and is dropped; TODOs, requirements, and receipts come from their stores, not from hybrid search.
- Applicable requirements: the requirements linked to the turn's TODO (`TodoRequirementLink`) when the turn's `todoId` names a TODO, an `ISSUE-<n>` id included because it is a TODO id, plus those linked to the turn by relationships and those named in `focusRefs`; a turn with `todoId` `None` has only the latter two.
- Tool loops in `Stateless`: history exclusion removes the messages of earlier requests only; on each later model call of the same request (a tool loop) the request's own messages (its prompt, the model's tool calls, and their results) stay, with the package and the `REQUIRED MEMORIES` block (`StatelessToolLoopOwnToolCallsKeptPriorRequestsExcluded`).
- Mode at runtime: ModeGateBehavior and GetAgentContextStatusQuery read `Mode` through `IOptionsMonitor<AgentContextOptions>`, the pattern of `src/QBrainAi.Services/Services/AgentPoolService.cs:36`, so a reloaded configuration takes effect without a restart and "Mode `Off` stops agent-context behavior without a deploy" holds (`ModeGateReloadedConfigurationOffRefusesWithoutRestart`).
- Storage budget and sizes: each agent-context SerializableWrite attempt runs under `StorageCommandBudget.Default`, the 5-second budget of non-submit session-log saves (`src/QBrainAi.Storage/StorageCommandBudget.cs:12`), and its expiry is the 503 `backend_unavailable` storage answer; input snapshots and audit rows are kept with their package for audit, bounded by the candidate limits and the token budget, with no purge in this plan; the injected package is bounded by `TokenBudget`, and each host's prompt-context limit is recorded in its P0.2 row.
- Builder binding case: `AgentContextBuildersEnvelopesBindThroughGenericClientPassthrough` runs in `tests/QBrainAi.PluginIntegration.Tests` (PluginCorePowerShell, G3 through `./build.ps1 PluginSessionLogIntegration`), which starts `pwsh` for each builder and binds its output through the real `GenericClientPassthrough` binder.
- Hosted agents: the hosted agent's `mcp_client_invoke` (`src/QBrainAi.McpAgent/Hosting/McpHostedAgentToolAdapter.cs:170-173`) reaches `client.AgentContext.*` once `AGENTCONTEXT` joins the passthrough allow-list, under the same units, ModeGateBehavior, and REPL client policy as any agent; RecordModelGradeCommand accepts only the configured grader, so a hosted agent cannot record a model grade; the ACID profile is unchanged (FR-MCP-136 AC 3).
- Pilot report and session-log turns: GetAgentContextPilotReportQuery counts refused prompts through SessionTurnLogging's Phase 2 read seam (the turns of the window closed `canceled` or `cancelled` whose `failureNote` starts with `agent_context_invocation_failed`), a cross-context call listed in section 8.3.
- Late `dequeue` on claude-code: P0.2 also verifies that a request's `dequeue` line (or its `remove` with `absorbed_mid_turn`) is in the transcript before that request's first tool call starts, so a verb started for the request applies it before binding; when it is not, claude-code is `turn_logging_unsupported` (D6), as for the other two bridge conditions (host binding table, claude-code).
- Host session start state: process StartHostSession runs develop's trust bootstrap first (`Invoke-FullBootstrap` with the marker snapshot, `plugins/core/lib-ps/plugin-hook.ps1:286-308` at `a8d3934b`) and develop's agent runtime header capture (`:314-326`, TR-MCP-PLUGIN-HEADER-001) unchanged; an unverified marker records `MCP_UNTRUSTED` in the workspace `session-state.yaml` as today and the process ends before the session open, with no server call. `session-state.yaml` keeps those trust and header fields and is never read for targeting. A new `mcpSessionId` comes from develop's `New-PluginSessionId` (`:47-56`): the `MCP_SESSION_ID` override when it is set, else `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`; host sessions that share an override share one MCP session, which is allowed because a turn's identity is its minted `requestId` and each host session keeps its own `turn-state.yaml` (C5).
- Prompt without a recorded session: the prompt hook's no-session recovery (`Invoke-PluginNoSessionRecovery`, `plugin-hook.ps1:541`, status `no-session` at `:778`) is replaced. A prompt hook whose host session has no recorded `mcpSessionId` (no SessionStart ran for its key) first runs process StartHostSession for that key from its own payload (the session key, and on claude-code and claude-cowork the `transcript_path` that starts the bridge); on a host without a SessionStart event this is the first-ingress writer. An unreachable server is then the mint's outage (`turn-opened-degraded`), and the drain opens the session before it mints.
- `final-response.ps1`: the `Invoke-McpPlugin.ps1 -Command CompleteTurn` entry (`plugins/core/lib-ps/Invoke-McpPlugin.ps1:344-351`, `final-response.ps1:27`) is the `completeTurn` verb: process BindVerbToTurn binds it from its invocation context like every verb, and its close passes `Test-ModelEvidence` and `Get-CloseRemedy`, so a turn without model evidence is refused locally with that remedy; the entry carries no evidence of its own.
- Degraded missing-turn dialog: FR-MCP-SESSIONLIFE-001 AC004 and TR-MCP-SESSIONLIFE-001 AC003 (a bounded recovery submit when a degraded turn's dialog finds no server turn) are replaced: a dialog bound to a `pending-<nonce>` id is captured behind its mint record and never sent (process PersistTurnWriteWithReceipt), a dialog answered 404 is `request_unknown`, final (section 3.4), and no plugin write creates a turn except the mint.
- Drain in flight: process DrainFailsafe keeps develop's in-flight skip: a record written by the submit in flight in the same process is not replayed by that process's drain (`plugins/core/lib-ps/repl-invoke.ps1:876-928,1094` at `a8d3934b`, TR-MCP-REPL-016). The removed first-success trigger also changes FR-MCP-172, TR-MCP-PERSIST-003, TR-MCP-FAILSAFE-001, TEST-MCP-FAILSAFE-001, TEST-MCP-SESSIONLIFE-005, TEST-MCP-195, and TR-MCP-SESSIONLIFE-004, which state it.
- Decision replay: the idempotent replay of the C7 triple is per invocation: replaying one captured invocation record never writes its triple twice, and two invocations with equal decision text are two decisions, because each invocation captures its own binding (C4).
- Quarantined legacy records: a quarantined legacy record that names no request belongs to no open turn, so the Stop gate does not warn on it; plugin Status counts it with the quarantine counts (`plugins/core/lib-ps/mcp-status.ps1:85-88` at `a8d3934b`).
- Required-memory renderer: the PowerShell renderer `hooks/scripts/memory-context.ps1` is tracked only in claude-cowork (512 lines at `03e934f`) and opencode (436 lines; the two copies differ); claude-code removed its copy at `d3a07e0` while its `tests/MemoryInjection.Tests.ps1:6` and `skills/memory/scripts/invoke.ps1:30` still load it, and codex (`2237b61`, which tries `lib/memory-context.ps1` first), copilot (`b54e029`), grok (`6400f6b`), grok-bot (`d7074c5`), and classic cline (`7909599`, whose jest `tests/memory-skill.test.ts:52-54` reads the file) load it from `skills/memory/scripts/invoke.ps1` and their memory tests without having it (inference: P0.1 baseline failures in those six repos). P0.1 reconciles the two copies into one renderer and, as the baseline fix of those six repos, restores it at `hooks/scripts/memory-context.ps1` in each (the path their skill and tests load today), so their memory suites are green before Phase 1. In Phase 4 PluginCorePowerShell moves that renderer into the core as `plugins/core/lib-ps/memory-context.ps1`, the sync carries it to all nine hosts, the core's required-memory handler and each host's memory skill load it from there, and HostPluginAdoption removes the host-repo copies (shared logic encapsulated once, section 3.5).
- Retired plugin records: TEST-MCP-150's skills-packaging cases move to each TypeScript host's Pester `tests/Skills.Tests.ps1`, and its adapter subject to the adapter suite (section 3.2); FR-MCP-PLUGINCORE-003 (persistent REPL daemon), with its TR and TEST, becomes `deferred`, because the Node broker and shell wrapper that implement it are retired by the PowerShell-only ruling and this plan specifies no PowerShell daemon.
- Governed verbs and the trust check: "no ambient HTTP fallback" (TR-MCP-SESSIONTURN-008) governs the verbs (session log, TODO, requirements, triage), which reach the server only through the REPL bridge; the marker trust check's read-only health and nonce request (`plugins/core/lib-ps/marker-resolver.ps1:218` at `a8d3934b`) is not a verb fallback and stays.
- Host details: the claude-cowork connector repair replaces the dangling `bin/mcpserver-stdio.cmd` that its `.mcp.json` names with a PowerShell script started as `pwsh -NoLogo -NoProfile -NonInteractive -File ${CLAUDE_PLUGIN_ROOT}/bin/mcpserver-stdio.ps1` (PowerShell-only ruling; no `.cmd`), its target recorded in Cowork's P0.2 row. Classic cline takes the workspace from its hook payload's `workspaceRoots` when it holds exactly one root; a task with several roots is `turn_logging_unsupported` (D6), a feature-matrix row, as cline-v2 is supported only with one run per user message; P0.2 records the field. The MCP section tools `sessionlog_replace_section` and `sessionlog_clear_section` list the two review sections beside the nine they list today (`src/QBrainAi.Support.Mcp/McpStdio/FwhMcpTools.SessionLog.cs:203,231` at `a8d3934b`), because they dispatch the review units for them (section 5.2).
- Workspace and the database context: after resolving, WorkspaceResolutionBehavior calls `OverrideWorkspaceId` on the dispatch scope's `McpDbContext`, as the REST middleware does (`src/QBrainAi.Support.Mcp/Middleware/WorkspaceResolutionMiddleware.cs:255` at `a8d3934b`), because the context captures its workspace at construction (`src/QBrainAi.Storage/McpDbContext.cs:25-28`) and the handler, with its context, is resolved before the pipeline runs (`src/QBrainAi.Cqrs/Dispatcher.cs:93-107`) (`WorkspaceResolutionRecordPathFiltersDbContextCreatedBeforeBehavior`).
- Mode gate completeness: `ModeGateEveryAgentContextCommandExceptTrustDecisionImplementsMarker` reflects over the agent-context command records and fails when one other than DecideAgentResponseTrustCommand lacks `IModeGatedRequest`, so a command that omits the marker cannot run in `Off` unnoticed.
- Dispatch history: unchanged. The dispatcher keeps the YAML of its last 200 requests and results in memory (`src/QBrainAi.Cqrs/Dispatcher.cs:16,69,235-256` at `a8d3934b`), read only by `tests/QBrainAi.Cqrs.Tests/DispatcherTests.cs:171-210` and exposed by no route or tool, and the turn text it holds is the text the store holds (redaction is on read), so routing the session-log units through `IDispatcher` adds no disclosure surface. The federated read stays, because GetSessionLogQuery and QuerySessionLogsQuery delegate to `ISessionLogService` (section 5.2), the decorated chain of `src/QBrainAi.Support.Mcp/Program.cs:642-663`.
- Turns open at the CompletionGate deploy: the gate refuses their completion until each carries the items it names (a class and, for Code and Docs, a terminal hostile review); the S10 rollout step lists the workspace's turns in progress (QuerySessionLogsQuery, status `in_progress`) in its deploy receipt, and the Stop gate remedy names each one's missing items; nothing is backfilled (section 8.5).
- Review reason dump policy: `SessionLogTurnReviewReasons` takes `COPY-SANITIZE+HISTORY`, the policy of the session-log child tables, listed explicitly beside them (`src/QBrainAi.Services/Services/WikiDumpTablePolicyRegistry.cs:29-31` at `a8d3934b`), because the registry default `COPY-SANITIZE` (`:42`) would drop its history.
- Test placement: a Phase 2 change that completes behavior rather than stubbing it (the `agent_context` federation registration, FR-MCP-103-AC006) records its case red before the change, in Phase 2, and Phase 3 keeps it green; a case that needs a later context's types runs at that context's gate (section 8.4; `ReadableKeyAllocatorSuffixPast99WidensLatestGradeOrderKept` at ResponsesAndTrust's); `--test-projects` naming `build/Build.Tests` is refused before any test runs; a BuildTooling case broken by another context's change is fixed by that context under the amendment rule of section 8.3 (`Catalog_HasExactlyEightEnabledScenarios` and PluginCorePowerShell's `GrokBot` scenario); the clear-session skill parity evidence lives in PluginIntegration (G3) and each plugin repo's Pester `Skills.Tests.ps1` (G6), never in Build.Tests.

Resolutions of round 5 derived from the decisions above (implementer readings, each naming the decision it follows; none adds design mechanism):

- Mode changes: a plugin that receives 409 `agent_context_disabled` from a build or a submission caches `Off` for its host session and continues statefully (no refusal, no `canceled` close), because `Off` is the stateful default and the documented rollback; every other build error still fails the invocation. When the Mode cannot be read at host session start, including a read that misses the SessionStart hook bound, or its value is unknown, the plugin assumes `Off` (that read is GetAgentContextStatusQuery, whose cost section 6.5 bounds). A change away from `Off` takes effect at the next host session start.
- A pending mint (transport outage) in `Pilot` or `Stateless` fails the invocation like a build outage ("Fail the invocation"); only a host session reported `turn_logging_unsupported` (D6) stays stateful. A final mint refusal leaves no turn, so the prompt is refused and the Stop gate warns the refusal, naming the prompt (FR-MCP-SESSIONTURN-001-AC008).
- A host whose prompt hook cannot refuse a prompt, or that runs the prompt anyway after a hook timeout, crash, or `plugin_core_unavailable`, runs `Pilot` and `Stateless` as `Off` and records that as a feature-matrix limitation (D7); P0.2 probe (j) records each host's refusal capability.
- Reads in `Off`: the read-only agent-context queries stay available in `Off` (ModeGateBehavior gates commands only), because the attached plan keeps rejected and stale responses "for audit".
- The pilot comparison: `Pilot` yields stateful responses with packages built and graded, and a period in `Stateless` on hosts with history exclusion yields stateless responses; the pilot report compares by `invocationMode` and also counts packages that failed (`Overflow`), refused prompts, and requests with a package and no response ("Pilot stateless execution against the current stateful workflow before changing defaults").
- Rule files outside version control (the default `AGENTS-README-FIRST.yaml`, regenerated at every server start) are package items with their hash for provenance and never a staleness dependency, so a restart does not make responses stale; the request's own turn is never a candidate, a dependency, or cited evidence.
- The agent-context live check runs on a separate server instance with its own `--instance` and explicit profile and rule sources, never on a shared server.
- Compaction and resume keep the recorded MCP session (FR-MCP-SESSIONLIFE-002: an already cached session identifier is never rewritten): StartHostSession records `mcpSessionId` only when none is recorded for the host session key, open turns stay bound to it, and no `unsupported_session` record of a still-live host session is drained. With no rotation there is nothing to rebind (replaces the session-rotation rebind of TR-MCP-PLUGIN-012, which rewrites `current-turn.yaml` to the rotated session, the "same-agent session rotation still rebinds" clause of FR-MCP-XAGENT-001, and the `completeTurn`-survives-a-sessionId-rebind clause of FR-MCP-TRIAGEPLUGIN-001).
- A prompt hook killed after its write-ahead mint record ends at the next hook, verb, or SessionStart drain with exactly one server turn for that message, delivered once (the drain re-mints the same nonce, which returns the stored turn).
- A contended `turn-open.lock` at prompt time writes a failsafe diagnostic and the Stop gate warns `turn-open-contended`, naming the prompt; the hook does not wait.
- Retryable answers (section 3.1, "Deadlock and busy outcomes"): a live verb or a drain answered with a transport outage, 503 `backend_unavailable`, a busy store (503 `persistence_error` with `retryable: true`), or 409 `conflict` with `retryable: true` keeps the failsafe record as for an outage (a review excepted: it keeps its no-failsafe rule, the verb reports the retryable code, and the model records the review again after the drain, section 5.2); a drain stops at the first retryable answer without consuming attempts and keeps every later record. A queued (degraded) write reports `persisted: false`, code `queued`, and exits 0, as today (FR-MCP-SESSIONLIFE-003, FR-MCP-REPL-009).
- Hook payloads without the host row's session key refuse and write nothing ("A write is never attributed by guesswork"); cline-v2 is supported only when P0.2 shows one run per user message (D6 otherwise); host-local commands that never reach the model (for example `/clear`, `/compact`) are system events when P0.2 shows they produce arrival events; an arrival bridge whose running version differs from the active plugin cache is treated as a stale pid and restarted from the active cache with `appliedThroughOffset` kept (FR-MCP-TRIAGEPLUGIN-001); clear-session fails a minted request that is still `queued` as withdrawn ("withdrawn before delivery").
- A requestClass-only change on a terminal turn through an additive write is accepted and not re-gated (the completion gate fires only on a move into `completed` or `failed` and on a Replace that carries either, CompletionGate's trigger, section 5.3); `hvVerdict` sent with an explicit `status` is 400 `validation_error` naming `status`; UC-7's live model and effort rule is an HV-protocol rule checked from the reviewer's receipts, not a plugin check; an evidence write is verified against the sanitized read projection.

### 3.2 Host binding table and TypeScript-host adapter contract

Host binding table (locks the D1 nonce source, the D5 arrival source, and the D2 host session key per host; every turn token is still minted by the server). Rules common to all hosts:
- Nonce: computed from the event itself, never from the hook process, so every delivery of one event yields the same nonce; a value outside `^[A-Za-z0-9._:-]{1,64}$` is replaced by the 64 lowercase hex characters of its SHA-256. Equal-field rule (copilot and classic cline, whose nonce hashes event fields with no per-message id): under `turn-open.lock`, a new prompt-hook invocation whose nonce already has a failsafe mint record or a `pending-<nonce>` turn in that host session, or whose mint returns `merged`, cannot be told apart from a separate message with equal session, timestamp, and prompt, so it mints and binds nothing and the host session fails closed under D6 exactly as the bridge does for an unattributable record (`unsupportedSince: { causeNonce, causeOperation: prompt_hook, reason: indistinguishable_arrival }` with the session-level envelope; every session-log verb then reports `turn_logging_unsupported` and writes nothing to the server until the session ends, except that writes for a turn already open at that moment go to the plugin failsafe under the unsupported-session failsafe rule, FR-MCP-SESSIONTURN-005-AC001; its open turns are listed at the next SessionStart). A network retry or failsafe replay of a stored mint record is a drain of that record, not a new invocation, and reuses its nonce without failing closed.
- Unsupported-session rule (D6; operator option 1 of 2026-10-10): every check that makes a host session report `turn_logging_unsupported` after one of its requests was delivered records `unsupportedSince` with the session-level envelope in `turn-state.yaml` in the same atomic replace that makes the session unsupported, with `reason` set to that check's code: `queue_removal_ambiguous` (bridge), `indistinguishable_arrival` (equal-field rule), `arrival_bridge_format_unrecognized` (an unknown queue operation, a record missing a required field, or a transcript shorter than `appliedThroughOffset`), `duplicate_prompt_hook` (the copilot or classic-cline single-registration check starts failing; the first verb or hook in that host session that observes it records it), or, for any other D6 check, its own reason code; plus `causeOffset` or `causeNonce` where the cause has one, else `detectedAtUtc`. Whatever the reason, the unsupported-session failsafe of FR-MCP-SESSIONTURN-005-AC001 then applies: writes for a turn of that host session still in progress when `unsupportedSince` was recorded (a delivered open request, or a `withdrawn` queue entry still `in_progress`, addressed by its original pair; a minted request still `queued` is not covered and stays a warning) are captured, and every mint and every other write is refused. The state lasts until the host session ends, or until a SessionStart clears its cause where the cause's own rule allows (the bridge restarting on a valid transcript); the captured records drain at that SessionStart. A host that is unsupported from its first event mints nothing, has no open turn, and records no `unsupportedSince`.
- P0.2 gating rule (D6, D7): every case, gate, or live assertion that needs a host's arrival mechanism or a P0.2-probed host field runs only when that host's P0.2 row passed; when the row failed or was unavailable, the host's D6 case `<Host>P02NotPassedTurnLoggingUnsupportedZeroMintsZeroWrites` runs instead in the same slice (A3: `ClaudeCodeP02NotPassedTurnLoggingUnsupportedZeroMintsZeroWrites`, `GrokP02NotPassedTurnLoggingUnsupportedZeroMintsZeroWrites`, `GrokBotP02NotPassedTurnLoggingUnsupportedZeroMintsZeroWrites`, `CopilotP02NotPassedTurnLoggingUnsupportedZeroMintsZeroWrites`, `CoworkP02NotPassedTurnLoggingUnsupportedZeroMintsZeroWrites`; A8: `ClineP02NotPassedTurnLoggingUnsupportedZeroMintsZeroWrites`, `ClineV2P02NotPassedTurnLoggingUnsupportedZeroMintsZeroWrites`, `OpenCodeP02NotPassedTurnLoggingUnsupportedZeroMintsZeroWrites`), and every live check excludes that host with its P0.2 receipt (codex excepted: a failed or unavailable codex row, or a codex enqueue-versus-processing result of "only when processed", stops P0 instead, because every HV reviewer persists its own turn through the Codex plugin; host binding table, codex). The P0.2 outcome that selected each branch is that host's feature-matrix receipt. The Cowork bridge fixtures of A3, the per-host mint cases of A3 (codex, grok, grok-bot, copilot) and A8 (classic cline, cline-v2, opencode), the copilot duplicate-hook transition of A9, TEST-MCP-PLUGIN-013-AC001, the codex live assertion, and the Phase A live check on claude-code are instances of this rule.
- Host session key: read from the verb's own invocation context as listed per host. Long-lived processes (stdio MCP servers, the arrival bridge itself) never trust a key captured at spawn time, because a host may change the session in place (Claude Code `/clear`; resume without an explicit id keeps the startup id in long-lived subprocesses per the Claude Code env-var reference); they resolve the key per call by the live-session rule.
- Queued-message arrival on every host (D5, D6): P0.2 records, per host, whether a message sent while a turn is running reaches an arrival mechanism at enqueue. Where the prompt event itself fires at enqueue for a message the host takes into the running turn, it is the arrival mechanism; a prompt event that fires at enqueue for a message the host runs as its own later turn is not, because a prompt-hook mint is delivered at once (TR-MCP-SESSIONTURN-001 AC-005) and would move the write target and the stop ledger to a request the model has not received (D2; FR-MCP-SESSIONTURN-005-AC001), so P0.2 records that result as only when processed. The only queue adapter this plan specifies is the claude-code/claude-cowork arrival bridge. For every other host whose P0.2 result is only when processed (under the rule above) (codex excepted: that result stops P0, because every HV reviewer persists its own turn through the Codex plugin), the host is unsupported for turn logging in this plan (`turn_logging_unsupported`, D6), whether or not it has a separate enqueue or message-queue event; when it has one, P0.2 records that event's observed payload, and P0.8 files a traced follow-up TODO proposing the host's adapter (payload, nonce, correlation, lifecycle, replay, tests) for operator approval; until that adapter is specified, approved, and delivered, the host stays `turn_logging_unsupported` in this plan and its `<Host>P02NotPassedTurnLoggingUnsupportedZeroMintsZeroWrites` case applies.
- Live-session rule (D2 fallback): a host session is live from its first mint until its session-end event (Claude Code `SessionEnd`, including reason `clear`; Codex `SessionEnd`; Copilot session end; cline-v2 and opencode session close) marks `sessions/<key>/ended.yaml`, or until 12 hours without a mint or verb (the idle rule writes `ended.yaml` with `reason: idle`; a session-end event writes `reason: session_end`). A verb without its own key binds to the single live session of its agent in the workspace (`host_session_inferred`), and is refused with `host_session_unknown` (none live) or `host_session_ambiguous` (several live) unless it passes the trusted (MCP `sessionId`, `requestId`) pair of C5.
- claude-code: nonce: the bridge's `q-` enqueue nonce (the bridge is the only minter, FR-MCP-SESSIONTURN-001-AC009); hook key payload `session_id`; verb key env `CLAUDE_CODE_SESSION_ID` read at verb start in shell, PowerShell, and hook subprocesses (equals `session_id`; those subprocesses see the updated value after `/clear`); stdio MCP verbs use the live-session rule; payloads carrying `agent_id` are subagent events (system events, no turn); the subagent's own Bash and PowerShell verbs carry the parent's `CLAUDE_CODE_SESSION_ID` and bind to the parent's write target; stop uses payload `stop_hook_active`. Arrival bridge (D5), the only minter on claude-code (known-good single source of truth: the host transcript is one ordered log written by one producer, so the bridge needs no key shared with the hook, which the host does not provide: Claude Code's `queue-operation` records carry no `promptId` although its user records do): started by SessionStart with the payload `transcript_path`, it applies `queue-operation` lines in file order. Every `enqueue` takes a queue position, so FIFO `dequeue` stays exact. An `enqueue` whose `content` the system-event classifier marks as a system event is added as `{ kind: system, enqueueOffset, enqueueTimestamp, promptSha256, state: queued }` with the session-level envelope `{ workspacePath, agent, mcpSessionId, hostSessionKey }` (it has no nonce or requestId, and none is ever invented for it) and is never minted. An `enqueue` whose `content` the classifier accepts as a user request: mint with nonce `q-` + SHA-256(`session_id` LF the record's starting byte offset in the transcript LF enqueue `timestamp` LF `content`) truncated to 62 hex characters (the offset is unique per record and identical on replay, so two distinct records with equal timestamp and content get two nonces), and add `{ kind: request, requestId, nonce, enqueueOffset, enqueueTimestamp, promptSha256, state: queued, stateOffset, mintedAtUtc }` with the root-id envelope to the queue in `sessions/<key>/turn-state.yaml`. `dequeue` (no content) removes the head of the host session's `queued` entries: a `kind: request` head becomes `delivered` and the write target; a `kind: system` head is removed and delivers nothing. `remove` and `popAll` records that match a `kind: system` entry drop it without a turn. `remove` with reason `absorbed_mid_turn`: the `queued` entry whose `promptSha256` equals the record's `content` hash (both written by the same producer) becomes `delivered` and the write target when exactly one queued entry has that hash. `remove` with any other or no reason: the matching `queued` entry becomes `withdrawn` when exactly one queued entry has that content hash. `popAll`: when, for every content line it carries, the number of lines with that content equals the number of queued entries with that content hash, those entries become `withdrawn`. Fail-closed rule (D6): a `remove` carries `commandUuid` and `deliveryId` (Claude Code 2.1.293), which also appear on the delivered message's `queued_command` attachment, but `enqueue` and `dequeue` carry no key shared with it, so a `remove` that matches two or more queued entries, or a `popAll` that carries fewer lines of a content than there are queued entries with that content, cannot be attributed (a `popAll` whose line count equals the matching entry count withdraws them all, because every candidate is withdrawn). The bridge never guesses: at the first such record it stops before it without advancing `appliedThroughOffset`, sets `turn-state.yaml` `unsupportedSince: { causeOffset, causeOperation, reason: queue_removal_ambiguous }`, and from then on every session-log verb in that host session reports `turn_logging_unsupported` and writes nothing to the server until the session ends, except that writes for a turn already open at that moment go to the plugin failsafe (FR-MCP-SESSIONTURN-005-AC001). Turns that are open at that moment are left exactly as they are on the server, the stop gate blocks on each only until the model's close of it is captured in the plugin failsafe and then warns `queue_removal_ambiguous` listing them (FR-MCP-SESSIONTURN-005-AC001), and the next SessionStart for the agent lists them with their original (MCP `sessionId`, `requestId`) pairs for closing through the trusted recovery flow (FR-MCP-SESSIONTURN-005-AC006). P0.2 records the `remove` identity fields; the bridge never uses them to pair with an `enqueue`. Only `queue-operation` lines are arrival records; every other record type is ignored by design. A `queue-operation` with an unknown operation or a missing required field is never skipped: the bridge stops before it without advancing `appliedThroughOffset`, records warning `arrival_bridge_format_unrecognized` and, after a delivered request, `unsupportedSince` with that reason and the record's `causeOffset` in the same atomic replace (unsupported-session rule), and the host session reports `turn_logging_unsupported` on every session-log verb (D6) until a verified adapter handles that shape, while writes for a turn already open go to the plugin failsafe (FR-MCP-SESSIONTURN-005-AC001). Each record's state change and the advance of `appliedThroughOffset` past it are one atomic replace of `turn-state.yaml` (temp file, flush, rename, under `turn-open.lock`); records that start below `appliedThroughOffset` are skipped, so a restart never re-applies a record; the server mint for `enqueue` happens before that replace and is idempotent (same nonce, `merged`). Records are applied by whichever process holds `turn-open.lock`: the bridge, or a session-log verb, which first records the transcript length at the moment the invocation starts and, after taking the lock, applies every pending record that ends at or before that length, never a record appended later, before it binds (FR-MCP-SESSIONTURN-007-AC006), so a verb started after a `dequeue` line was written binds to that request, and a `dequeue` appended after the verb started never retargets it (D2). The prompt hook on claude-code and claude-cowork neither mints nor claims; it classifies system events and starts no turn. P0.2 verifies that every user prompt (in each prompt form of FR-MCP-SESSIONTURN-001-AC009) produces an `enqueue` record, that `dequeue` always takes the head, and that a request's `dequeue` (or its `remove` with `absorbed_mid_turn`) is in the transcript before that request's first tool call starts (section 3.1, Late `dequeue` on claude-code); if any fails, claude-code is `turn_logging_unsupported` (D6) until an adapter with a traceable key exists, recorded in the feature matrix.
- claude-cowork: same fields and arrival bridge as claude-code. P0.2 verifies that Cowork executes plugin hooks; if it does not, Cowork has no arrival mechanism and is unsupported for turn logging under D6 (`turn_logging_unsupported`, nothing written) until it gains one. A9 repairs the dangling `bin/mcpserver-stdio.cmd` connector as a PowerShell script started through `pwsh -File` (section 3.1, Host details) and adopts the bridge for Cowork only when its P0.2 bridge probes pass, with the Cowork RED fixtures of A3 (an instance of the P0.2 gating rule above).
- codex: A9 adds `hooks/hooks.json` (SessionStart, UserPromptSubmit, PostToolUse, Stop, SessionEnd per the Codex hooks reference) with the codex `session-end` wrapper that its SessionEnd entry calls, generated by the existing wrapper generator, whose codex branch lists no `session-end` today (`plugins/core/hooks-templates/generate-wrappers.ps1:24-34` at `a8d3934b`), and, until the hook trust check below passes, codex reports `turn_logging_unsupported` on every session-log verb (D6) instead of the manual steps in `skills/enforcement/SKILL.md`, which have the model run the prompt hook itself and are removed in A9; nonce payload `turn_id`; hook key payload `session_id` (subagents share the parent's); verb key env `CODEX_THREAD_ID` only if P0.2 proves it equals `session_id` in shell commands, else the live-session rule. Codex skips plugin hooks until the operator trusts that hook definition (OpenAI Codex plugin documentation), so A9 and the rollout include an operator trust step, and the Phase A live check (after the elevated deploy and the branch REPL install) includes the live assertion that a fresh Codex prompt, launched through the exact HV launch form of the hostile validation protocol (`codex exec -m gpt-6-astra -c model_reasoning_effort="max" ... --json -`, prompt on stdin), produces a hook-minted server turn, and that a message sent with `codex queue` while a turn runs in an interactive session on the shared app-server daemon produces exactly one hook-minted turn of its own (both parts are the codex live assertion); codex stays `turn_logging_unsupported` until that assertion passes, and it runs only when codex's P0.2 row passed (P0.2 gating rule). Because every HV reviewer must persist its own turn through the Codex plugin, a codex P0.2 row that fails or is unavailable does not select the D6 fallback: P0 stops and the operator is shown the receipt.
- copilot: A9 points `plugin.json` at `hooks/hooks.json` and removes the dangling `.sh` hook file; nonce `cp-` + SHA-256(`sessionId` LF `timestamp` LF `prompt`) truncated to 61 hex characters (no per-message id exists, so the equal-field rule applies), from the UserPromptSubmit payload fields the Copilot CLI hooks reference documents (`sessionId`, `timestamp` in Unix milliseconds, `cwd`, `prompt`; VS Code form `session_id`, ISO `timestamp`); hook key `sessionId`/`session_id`; verbs use the live-session rule (no session env var is documented). The reference does not promise that two separately registered hook commands receive an identical `timestamp`, so the Copilot adoption also enforces a single delivery boundary: an A2-equivalent check enumerates every source the Copilot hooks reference documents (policy `/etc/github-copilot/policy.d/*.json`; repository `.github/hooks/*.json`; user `~/.copilot/hooks/` honoring `COPILOT_HOME`; inline hooks in `.github/copilot/settings.json`, `.github/copilot/settings.local.json`, and the cross-tool `.claude/settings.json` and `.claude/settings.local.json`; `~/.copilot/settings.json`; plugin `hooks.json` or `hooks/hooks.json`; all sources run, and the Copilot cloud agent reads only `.github/hooks/*.json`), detects duplicate MCP prompt hooks across all of them, repairs the duplicates it owns (plugin, repository, and user files, with a backup), and for a duplicate it cannot remove (a policy file) fails closed: Copilot turn logging reports `turn_logging_unsupported` with operator remediation text and, after a delivered request, records `unsupportedSince` with reason `duplicate_prompt_hook` (`DuplicateHookAfterDeliveryUnsupportedSinceOpenTurnCloseCaptured`), recorded as a D7 documented limitation; P0.2 captures two real deliveries of one prompt from two registered hooks when the Copilot CLI is available and records whether their `timestamp` values match (evidence only; D11 does not depend on it); A9 tests duplicate detection with one fixture per source, that with exactly one MCP prompt hook a delivery mints one turn, that two arrivals with equal prompt and different `timestamp` mint two turns, that a second invocation with equal session, timestamp, and prompt after the first has minted mints nothing and fails the host session closed with `indistinguishable_arrival` and zero later writes, and that a mint retry or failsafe replay reuses its nonce without failing closed (`CopilotSamePromptDifferentTimestampTwoMints`, `CopilotEqualFieldsSecondInvocationFailsClosedNoSecondMint`, `CopilotMintRetryAndFailsafeReplayReuseNonceNoFailClose`), and that with two MCP prompt hooks registered (in any pair of sources, including two deliveries that differ only in `timestamp`) the prompt hook reports `turn_logging_unsupported` before any mint and the server receives zero mints (D11).
- grok: the core reads camelCase keys when the host is grok: nonce `promptId`, hook key `sessionId`, stop `stopHookActive`, subagent `subagentType`; verb key env `GROK_SESSION_ID` only if P0.2 proves it is set for shell commands, else the live-session rule. Grok's default execution of `~/.claude/settings.json` hooks is a duplicate-registration source that A2's validator reports; with `promptId` as nonce, such duplicates still mint one turn.
- grok-bot: same payload handling as grok (camelCase `promptId` nonce, `sessionId` hook key, `stopHookActive`, `subagentType`) until P0.2 records the Grok Bot cloud runtime's actual hook payload, session environment for shell commands, and queued-message behavior; each P0.2 outcome selects among the rules above (prompt-hook arrival at enqueue, or `turn_logging_unsupported` with a follow-up adapter TODO for a recorded queue event, or a documented capability limitation per D7); verbs use `GROK_SESSION_ID` only if P0.2 proves it is set in the cloud runtime, else the live-session rule.
- cline (classic): the plugin itself is a stdio MCP server with no hooks, but the Cline app has native hooks (UserPromptSubmit with `prompt`; common fields `taskId`, `hookName`, `timestamp`, `workspaceRoots`, `userId`, `model`; seen in the local Cline source `/home/sharpninja/github/cline` at `b090c4a08`, `apps/vscode/src/core/hooks/hook-factory.ts:102-120,201-228` and `apps/vscode/proto/cline/hooks.proto:44-62`, not in public docs). Arrival adapter (D6): the cline plugin repo ships a Cline UserPromptSubmit hook, a PowerShell command hook (`pwsh -NoLogo -NoProfile -NonInteractive -File <plugin root>/lib/plugin-hook.ps1 -HookName user-prompt-submit -HostName cline`), that mints through the PowerShell core with nonce `cl-` + SHA-256(`taskId` LF `timestamp` LF `prompt`) truncated to 61 hex characters (the hook factory sets `timestamp` from `Date.now()` when the hook fires and no per-message id exists, so the equal-field rule applies, tested in A8 and in the cline repo's G6 suite in A9: `ClineSamePromptDifferentTimestampTwoMints`, `ClineEqualFieldsSecondInvocationFailsClosedNoSecondMint`, `ClineMintRetryAndFailsafeReplayReuseNonceNoFailClose`) and host session key `taskId`, with the workspace from `workspaceRoots` when it holds exactly one root (a task with several roots is `turn_logging_unsupported`, D6, section 3.1, Host details); the MCP server's tools carry no `taskId`, so they use the live-session rule; single registration is enforced as for copilot. P0.2 verifies in the installed Cline build that native hooks run, where hook scripts are installed, the payload fields, and whether queued messages fire the hook at arrival; if no arrival mechanism exists, classic Cline is unsupported for turn logging under D6 (`turn_logging_unsupported`).
- cline-v2: turn opens in `beforeRun` with nonce `runId`; host session key `conversationId` (lifecycle snapshot and tool context; the plugin stops stripping `sessionId`, `conversationId`, `runId`, `toolCallId` at `plugin.ts:122-140`); prompt text from the last user entry of `snapshot.messages`; required memories through `beforeModel`, the hook that injects them into the model request today (`mcpserver-cline-v2-plugin/src/plugin.ts:230-249` at `e776d94`), forwarded to the core (adapter contract below); stop-equivalent `afterRun` (no per-run session close); tool audit through `beforeTool` and `afterTool`, forwarded to the core's `tool-audit` hook (below; today `mcpserver-cline-v2-plugin/src/plugin.ts:250-275` at `e776d94`); `@cline/core` pin raised to the version shipping `@cline/shared` 0.0.92 types. P0.2 records whether a queued message starts its own run at arrival; if it does not, cline-v2 is unsupported for turn logging in this plan under D6 (`turn_logging_unsupported`), and if a message-queue event exists, P0.2 records that event's observed payload, and P0.8 files a traced follow-up TODO proposing the host's adapter (payload, nonce, correlation, lifecycle, replay, tests) for operator approval; until that adapter is specified, approved, and delivered, the host stays `turn_logging_unsupported` in this plan and its `<Host>P02NotPassedTurnLoggingUnsupportedZeroMintsZeroWrites` case applies.
- opencode: turn opens in `chat.message` for user messages with nonce `messageID` (fired when the message is created; that is arrival only under the queued-message arrival rule above, which P0.2 (i) checks); host session key `sessionID`; plugin tools read `sessionID` from the tool context the adapter forwards as `hostContext`, and shell commands get `MCP_HOST_SESSION_KEY` from the `shell.env` hook, whose returned `output.env` the adapter applies; stop-equivalent `session.idle`, delivered through the `event` hook (`event({ event })`, session key `event.properties.sessionID`); required memories on the `chat.message` `output` the adapter already forwards and applies, where the host's own handler adds them today (`mcpserver-opencode-plugin/src/plugin.ts:197-214` at `36ed34d`); tool audit through `tool.execute.before` and `tool.execute.after`, forwarded to the core's `tool-audit` hook (below; today `mcpserver-opencode-plugin/src/plugin.ts:226-254` at `36ed34d`); the start/end regexes and the factory signature are replaced.
- Host limits recorded as feature-matrix rows (D7; each row carries its P0.2 receipt; none adds a mechanism): (1) Stop-gate enforcement: classic cline has no Stop-equivalent hook in this table, so the Stop gate never runs there and its host sessions end only by the 12-hour idle rule; cline-v2 `afterRun` and opencode `session.idle` fire after the model has stopped, so the gate evaluates and reports but cannot hold the run open, and a Stop that leaves turns open records `stop_gate_overridden_by_host` (inference from the hook timing; P0.2 records each host's behavior). (2) Per-tool audit is ported, not a limit: the TypeScript hosts' tool audit (cline-v2 `beforeTool` and `afterTool`; opencode `tool.execute.before` and `.after`), which calls the packed Node core's `appendToolAction` today, moves into the PowerShell core's `tool-audit` hook below and keeps its `tool_call` and `tool_result` dialog items (C6); QB-AGENT-001's later `tool_execution` action type is unaffected. (3) Required memories (FR-MCP-MEMORY-007): forwarded on cline-v2 (`beforeModel`) and opencode (`chat.message` `output`) as above; classic cline's stdio entry attaches the block to request-boundary tool results today (`withRequiredMemoryInjection`, `mcpserver-cline-plugin/src/index.ts:89` at `7909599`), which the logic-free entry no longer does, so its native UserPromptSubmit PowerShell hook returns the block as the hook's `context_modification` output (`apps/vscode/proto/cline/hooks.proto:37-41` in the local Cline source at `b090c4a08`); where P0.2 shows that context does not reach the model, the row records the limitation with the explicit memory-list fallback FR-MCP-MEMORY-007 allows.
- Tool-audit hook (A8; the port of the packed Node core's `appendToolAction`, `plugins/core/lib-node/src/runtime/host-context.ts:355-385` at `a8d3934b`, so cline-v2 and opencode keep the audit they have today): `plugin-hook.ps1 -HostEvent` maps cline-v2 `beforeTool` and opencode `tool.execute.before` to `tool-audit` with phase `before`, and cline-v2 `afterTool` and opencode `tool.execute.after` to phase `after`. The core reads the tool name, input, and error from the forwarded event per host, as the host plugins do today: cline-v2 the name from `toolCall.name`, `tool.name`, `toolName`, or `name` (else `unknown_tool`), the input from `input` or `toolCall.input`, normalized against the tool's descriptor when the tool is one of the core's, and the error from `error`; opencode the name from `input.tool`, the input from `input.args` on `after`, and the error from `output.metadata._error`. It binds to the host session's write target as a hook invocation (process BindVerbToTurn with `MCP_PLUGIN_HOOK_INVOCATION=1`, so the record is never model evidence) and appends one dialog item to that turn through the `appendDialog` route of process PersistTurnWriteWithReceipt: role `tool`, category `tool_call` for `before` and `tool_result` for `after` or an error, content the JSON `{ tool, input, status }`, with `error` added only on an error, status `pending` for `before` or an error and `completed` otherwise, the item the Node core writes today (`host-context.ts:381`). With no write target, an unsupported host session, or a refused binding it writes nothing; an outage keeps the write in the failsafe like any verb; the hook answers the host with exit 0 and an empty result in every case, writing a failure only to stderr, so a tool call is never blocked or changed, as today. The Node core's accompanying `design_decision` action per tool event is not ported, because C6 records tool runs as dialog items and a decision is the C7 triple (FR-MCP-SESSIONTURN-006) (`TsHostToolAuditBeforeAfterAppendsToolCallAndToolResultOnWriteTarget`, `TsHostToolAuditErrorRecordedAsToolResultPendingWithError`, `TsHostToolAuditNoWriteTargetWritesNothingExitZero`, `TsHostToolAuditOutageCapturedInFailsafeToolCallNotBlocked`, `TsHostToolAuditNeverModelEvidenceNoDecisionAction`).

![Tool-audit hook of the TypeScript hosts (activity), part 1 of 2](c4/rendered/STR-03-01.svg)

View: STR-03-01

![Tool-audit hook of the TypeScript hosts (activity), part 2 of 2](c4/rendered/STR-03-01-part2.svg)

View: STR-03-01-part2

TypeScript-host adapter contract (operator, 2026-10-11, PowerShell-only ruling): each host keeps only the entry file its plugin API forces (cline-v2 and opencode `createMcpServerPlugin`; classic cline its stdio MCP server entry, while its native hooks are PowerShell command hooks), and that file holds no logic beyond one fixed host-binding constant, the event type its opencode `event` hook forwards: (1) for each host hook the host binding table names (cline-v2 `beforeRun`, `beforeModel`, `beforeTool`, `afterTool`, and `afterRun`; opencode `chat.message`, `tool.execute.before`, `tool.execute.after`, `event`, `shell.env`, and, for Stateless mode, `experimental.chat.messages.transform`), it starts `pwsh -NoLogo -NoProfile -NonInteractive -File <plugin root>/lib/plugin-hook.ps1 -HostEvent <the host's event name> -HostName <host> -WorkspacePath <the workspace directory the host's plugin API reports, recorded per host in P0.7>` as an argument list (never a shell string) with the complete host invocation on stdin as UTF-8 JSON, `{ input, output }`: the hook's input argument (cline-v2 the lifecycle event object; opencode the `input` object, which carries `sessionID`) and, where the hook signature has one, its output argument (opencode's `output` object), each serialized by the host runtime's own serializer with no field added, removed, or renamed (opencode's `event` hook fires for every host event, streaming message and part updates included, so for that hook the adapter forwards only an event whose `type` the host binding table names (`session.idle`) and returns at once without starting `pwsh` for any other type); the core, not the adapter, maps the host hook (`-HostEvent`, A8: cline-v2 `beforeRun` and opencode `chat.message` to `user-prompt-submit`, whose returned opencode `output` also carries the `REQUIRED MEMORIES` block (FR-MCP-MEMORY-007), cline-v2 `beforeModel` to the required-memory handler, which returns the model request with that block added unless the request already carries it, as the host's own `beforeModel` does today (`mcpserver-cline-v2-plugin/src/memory-context.ts:409-418` at `e776d94`), and in `Stateless` mode, where cline-v2's P0.2 row (probe (j)) records that the returned request replaces the model's messages, also applies the history-exclusion handler of RunStatelessInvocation (both render the block with the existing PowerShell required-memory renderer, moved from `hooks/scripts/memory-context.ps1` of the host repos into the core as `plugins/core/lib-ps/memory-context.ps1`, section 3.1, Required-memory renderer), cline-v2 `afterRun` to `stop-gate`, cline-v2 `beforeTool` and `afterTool` and opencode `tool.execute.before` and `tool.execute.after` to the `tool-audit` hook with phase `before` or `after` (above), opencode `event` to `stop-gate` when `event.type` is `session.idle` (session key `event.properties.sessionID`) and to an exit-0 no-op for every other event type (a defense, because the adapter forwards no other type), opencode `shell.env` to the core's shell-environment handler, which returns `{ output: { env: { MCP_HOST_SESSION_KEY: <input.sessionID> } } }`, and opencode `experimental.chat.messages.transform` to the history-exclusion handler of RunStatelessInvocation (its input is empty, so the core takes the session key from `output.messages[].info.sessionID`), which returns the replacement `output.messages` in Stateless mode and no change otherwise; any other hook exits with `host_event_unknown` and writes nothing); (2) it registers exactly the tools in the synced data file `lib/tool-descriptors.json` (`name`, `description`, `inputSchema`, `method`, `mutates`; review scores declared as strings, C10) and runs each call as `pwsh -NoLogo -NoProfile -NonInteractive -File <plugin root>/lib/Invoke-McpPlugin.ps1 -Command Invoke -Method <the descriptor's method> -WorkspacePath <workspace directory> -InvocationFromStdin` with the complete tool invocation on stdin as UTF-8 JSON, `{ params, hostContext }`: the call's arguments exactly as the host passed them and the host's separate tool-context argument (cline-v2 `AgentToolContext`, opencode `ToolContext`, which carries `sessionID`), so the core derives the host session key from `hostContext` per the host binding table and two host sessions calling one tool with identical arguments keep distinct keys, and no argument text is on the command line, so Linux and Windows quoting never applies; (3) it returns the core's exit status and stdout to the host: a tool result as the stdout text unchanged; a hook result, where the host API takes a return object (cline-v2), as the host runtime's JSON parse of the object the core writes in that host's shape (host binding table, section 3.2), never modified; and, where the hook signature has an output argument (opencode), by copying each property of the `output` object the core returns onto the host's output object and nothing else (for `experimental.chat.messages.transform`, `output.messages` is replaced as a whole); no review score is in any hook result; it never retries, caches, or rewrites; (4) when `pwsh` cannot be started or the host's event object cannot be serialized, it returns to the host the fixed failure `plugin_core_unavailable` with the start or serialization error text, and does nothing else (no retry, no fallback, no local state), so the host shows the failure and no turn is minted or written by the adapter. Each host repo tests its adapter with Pester (`tests/TypeScriptHostAdapter.Tests.ps1`), running the adapter under the host's own runtime with the core entry points replaced by the Pester fixture `Record-AdapterInvocation.ps1` and asserting the exact arguments, the unchanged stdin, the registered tools equal to `lib/tool-descriptors.json`, and the unchanged pass-through (`TsAdapterForwardsEventVerbatimToPluginHook`, `TsAdapterRegistersDescriptorsExactly`, `TsAdapterForwardsToolCallToInvokeMcpPlugin`, `TsAdapterReturnsCoreOutputUnchanged`, `TsAdapterTwoSessionsIdenticalArgsDistinctHostSessionKeys`, `OpenCodeChatMessageInputAndOutputForwardedOutputApplied`, `OpenCodeShellEnvSetsMcpHostSessionKey`, `OpenCodeEventHookSessionIdleRoutedToStopGateKeyFromEventProperties`, `OpenCodeEventHookOtherEventTypeNotForwardedNoPwshStart`, `OpenCodeMessagesTransformKeyFromMessageInfoSessionId`, `ClineV2ToolHooksForwardedToToolAuditReturnNothing`, `OpenCodeToolExecuteHooksForwardedToToolAuditReturnNothing`, `ClineV2BeforeModelForwardedRequiredMemoriesAddedOnce`, `OpenCodeChatMessageOutputCarriesRequiredMemories`, `TsAdapterPwshUnavailableOrEventNotSerializableReturnsPluginCoreUnavailableNothingElse`, and `TsAdapterToolArgsWithQuotesUnicodeNewlinesArriveExactly` run on Linux and on Windows, and `TsAdapterRealCoreEntryPointsBindHostEventAndToolMethod`, which drives the adapter against the real `plugin-hook.ps1` and `Invoke-McpPlugin.ps1` parameter binders with only the REPL replaced by a recording fake, so a contract the real core rejects cannot pass). Host glue per the host binding table (the events each adapter forwards and the PowerShell bindings that handle them): cline-v2 (`beforeRun` mint with `runId`, `conversationId` key, `beforeModel` required memories, `beforeTool` and `afterTool` tool audit, `afterRun` stop-equivalent, no per-run session close, `@cline/core` pin raised through npm), opencode (`chat.message` mint with `messageID` and required memories, `tool.execute.before` and `.after` tool audit, `sessionID` key via the forwarded `hostContext` and `shell.env`, `session.idle` stop-equivalent through the `event` hook, regex and factory replaced), cline (native UserPromptSubmit PowerShell command hook with `taskId` key, derived nonce, and required memories as its context output, MCP tools on the live-session rule, or `turn_logging_unsupported` per D6); each exposes `requestClass`, `session_record_review`, the `requestId` override (required under D9 while two or more delivered requests are open), truthful outcomes, decisions/requirements/blockers; the host limits are the feature-matrix rows above. This contract and the PowerShell-only ruling (section 3.1) replace the Node and shell records of the pre-ruling plugin core (replaces TR-MCP-SYNC-001's Node-core tarball vendoring and its TEST-MCP-194, TR-MCP-PLUGINCORE-001's `sh` and `ps1` pair, TR-MCP-PLUGINCORE-003's Node broker and shell wrapper, the Bash-family wording of TR-MCP-PLUGIN-009 (its hook output contract stays), the bats records TEST-MCP-142, TEST-MCP-149, TEST-MCP-PLUGINCORE-001, and TEST-MCP-PLUGINCORE-003, which move to Pester, and the jest record TEST-MCP-150, whose adapter subject the Pester adapter tests above replace and whose skills-packaging cases move to each TypeScript host's Pester `tests/Skills.Tests.ps1`; FR-MCP-PLUGINCORE-003 becomes `deferred`, section 3.1, Retired plugin records).

### 3.3 Order-independent contracts with QB-AGENT-001 (C1-C12)

Shared contracts (as agreed in `MEMORY-PROCESS-007`; P0.7 posts this consolidated text as a CONTRACT entry):
- C1 Multiple open turns per host session: a map of open turns by requestId plus a per-host-session write target (FR-MCP-SESSIONTURN-001-AC005, FR-MCP-SESSIONTURN-003, FR-MCP-SESSIONTURN-007); no single current-turn slot; "finish before another starts" applies per turn, never per session. D9 takes precedence over any latest-delivered fallback: automatic binding applies only while exactly one delivered request is open in the host session, otherwise an unqualified write is refused with `request_ambiguous`; minting alone never moves the write target.
- C2 Server-minted turn identity (alias journal: A3/A5 own it in the PowerShell cache, the only plugin cache since the 2026-10-11 PowerShell-only ruling, and A8 keeps committed aliases durably discoverable to every registered consumer through the PowerShell consumer interface (QB-AGENT-001's outbox among them once its PowerShell form exists; the operator decided the port on 2026-10-12, R2-F10, and Codex's ACK of the 2026-10-12 CONTRACT entry is the open item), which reapplies them idempotently until it has durably applied them (FR-MCP-SESSIONTURN-001-AC008); its observable contract is FR-MCP-SESSIONTURN-001-AC008, and its data model, functions, binding, atomicity, errors, and replay rule are the alias journal component of section 3.5.6, from which TR-MCP-SESSIONTURN-008 AC-007 is derived): requestIds come from the mint route (D1); an event recorded before the mint returns carries the turn's `ingressNonce` and the provisional `pending-<nonce>` id. The move from `pending-<nonce>` to the minted requestId is the single explicit exception to requestId immutability and never changes the logical owning turn: the alias (old id, new id, `aliasedAtUtc`) is journaled atomically before any event is rewritten, every bound event is rebound in original order (FR-MCP-SESSIONTURN-001-AC008), readers resolve either id through the alias journal's permanent lookup entry, which is never pruned (only the consumer-delivery obligation is retired once every required consumer acknowledges), and restart and replay reapply a journaled alias without minting again.
- C3 Turn outcomes and event persistence receipts stay separate (TR-MCP-SESSIONTURN-008 AC-006). Turn outcomes (server per turn: `created`, `merged`, `skipped_terminal`; no `skipped_duplicate`, removed by the operator on 2026-10-09) are owned by this plan; event receipts (eventId, payloadHash, committed revision) by QB-AGENT-001. On a persistence path with an audit outbox (QB-AGENT-001's; its governed-host protections move to the PowerShell core in A8 under R2-F10, and the rest of its Node work awaits Codex's ACK of the 2026-10-12 CONTRACT entry): `created` and `merged` (including reason `no_change`) count as persisted only when the event identity, hash, and committed revision match; `outcomeVerified:false` (server without per-turn outcomes) never satisfies a mandatory persistence obligation; `skipped_terminal` maps to `rejected`, is never retried or quarantined, and keeps the event and its receipt (reason, `storedStatus`) as terminal evidence, while only the plugin failsafe record is cleared.
- C4 Every persisted or queued event carries its immutable D2 binding `{ hostSessionKey, keySource, ingressNonce, requestId, boundAtUtc }`, captured once; replay always uses the captured binding and never re-resolves it; apart from the C2 alias, eventId, order, payloadJson, and payloadHash never change.
- C5 State is scoped per host session key (host binding table, section 3.2), not per process; one process may serve several host sessions (cline-v2 conversations); REPL ownership may stay per process. An explicit requestId resolves to exactly one immutable owning MCP session within the validated workspace and agent; ordinary host audit writes remain bound to their host-owned conversation context; explicit recovery and review-backfill operations (including the D10 HV-verdict backfill) may target an earlier host session only with its original (MCP `sessionId`, `requestId`) pair, selected through the trusted host or recovery flow.
- C6 Action types come only from `docs/context/action-types.md`. QB-AGENT-001 owns adding `tool_execution` (its requirement, the doc change, server validation, and FR/TR/TEST coverage) and no component emits it before that lands; code in this plan records tool runs as dialog items with category `tool_call`/`tool_result`, and its tests accept `tool_execution` once it is documented.
- C7 Each decision is written as the workspace triple: a `designDecisions` entry, a dialog item with category `decision`, and a `design_decision` action, with idempotent replay (FR-MCP-SESSIONTURN-006-AC004; A6, in the one PowerShell core).
- C8 Completion: after S10 a completion without `requestClass` and terminal reviews is refused with the completion-gate refusal (400, `code` `validation_error`, message starting with the token `session_turn_completion_gate`, section 3, Errors); the refusal is final for that submission attempt, not for the turn, which stays in progress for corrected review data; the same invalid submission is never retried automatically and its audit evidence is preserved (the plugins write carried evidence in a verified additive write before the status transition, FR-MCP-SESSIONCLASS-004-AC003); hooks never complete turns; `Other` "pending HV" is never an AGREE. QB-AGENT-001's governed completion additionally stays blocked while retained mandatory `skipped_terminal`/`rejected` evidence exists, even with zero pending and quarantine counts; clearing the plugin failsafe does not clear that obligation, and this plan's gates neither weaken nor duplicate that rule.
- C9 Typed failures carry the server or plugin detail code (`turn_immutable`, `skipped_terminal`, `host_session_unknown`, `host_session_ambiguous`, `request_ambiguous`, `request_unknown`, `turn_logging_unsupported`, `mint_requires_host_ingress`, `nonce_source_missing`, `session_turn_completion_gate`, `turn_missing_model_evidence`, `request_class_invalid`, `review_*`, `ingress_nonce_invalid`, `mint_collision_exhausted`, `session_not_open`, and the REPL client mutation policy's `transaction_degraded` and `mutation_not_transactional`, `src/QBrainAi.Repl.Core/ClientMutationPolicy.cs:188-219` at `a8d3934b`, FR-MCP-120, TR-MCP-TXN-001, which a build or submission treats as a build error, section 3.1) alongside QB-AGENT-001's broad `CoreRuntimeError` category, each with an explicit retryable flag (the one code table `ErrorCodes`, section 3.5.2): only transport outage, degraded persistence (503 `backend_unavailable`), a busy store outside SerializableWrite (today's 503 `persistence_error` with `retryable: true`, `src/QBrainAi.Support.Mcp/Services/McpErrorClassifier.cs:129-139` at `a8d3934b`), and a 409 `conflict` answered with `retryable: true` (SerializableWrite or key allocation exhausted after four attempts, section 3.5.2, which is also the answer when a deadlock or a busy store outlasts those attempts) are retryable (replaces TR-MCP-TRIAGEERR-001's "conflict ... retryable false unless SQLITE_BUSY or deadlock" and, for a write inside SerializableWrite, TEST-MCP-TRIAGESTORE-005's busy and deadlock outcome, `persistence_error` with `retryable: true`); the plugin handles a retryable 409 as an outage and keeps its failsafe record (section 3.1, "Deadlock and busy outcomes"; a review keeps its no-failsafe rule), and the REPL classifier carries the server's `ClassifiedError` (`code`, `message`, `retryable`, `details`) through at every status, so it keeps the body's `retryable` for `conflict` instead of forcing it false and a 400 or 422 detail code reaches the REPL caller (section 3.5.2; `src/QBrainAi.Repl.Core/ReplMcpErrorClassifier.cs:60-95` at `a8d3934b`: the conflict branch forces `retryable: false` at `:60-68`, and only a 503 passes the server's code through at `:70-95`; `ReplClassifierConflictRetryableTrueKept`, `ReplClassifierServerClassifiedErrorKeepsCodeRetryableDetailsEveryStatus`); retries are bounded (one scoped exception, ACKed by Codex at 2026-10-10T00:12:18Z: the trusted mint adapter, for the exact mint operation only, classifies an authenticated bound mint-endpoint HTTP 404 with no detail code, or a structured `unknown_method` or `method_not_found` result for the expected mint verb, as transport outage and schedules it only for a later failsafe drain, never a hot retry; the generic classifier and the audit outbox in-call retry loop are unchanged, prose, malformed metadata, arbitrary 404s, and unrelated operations never qualify, a 404 with `session_not_open` is final, and every other unknown code stays final); every listed code is final; an unknown code is final, never implies success, and is never retried without limit.
- C10 Review scores travel as their original text: lexical score text is preserved before any numeric coercion through parser, shim, transport, cache, and outbox; the TypeScript-host adapters never parse a score and the tool descriptors declare review scores as strings, so no host hands the core a binary double (exponent and signed forms are rejected by the core's raw-lexeme rules); any event hash is computed over the original text, never over a re-serialized parsed value.
- C11 Paginated turn listing and detail reads return `requestClass`, `agentReview`, and `hostileReview`, sanitized as FR-MCP-SESSIONLOGSAN-001 requires, without weakening stable-snapshot pagination, ordered detail retrieval, or browser credential exclusion; this read contract is fixed now, and S4 and S5 implement it on current `develop`.
- C12 The REPL release that includes `workflow.sessionlog.mintTurn`, `workflow.sessionlog.recordReview`, and the lossless score normalizer (S6) comes first; dependent host pins and QB-AGENT-001's P5 artifacts then update against that release; current REPL ownership and pinning work stays separate from advancing the pin. Amendment (ACKed by Codex at 2026-10-10T04:45:14Z in `MEMORY-PROCESS-007`): no accepted QB-AGENT-001 REPL pin may predate the Phase A release providing `workflow.sessionlog.mintTurn`, and the S6 capability remains required wherever it is consumed (QB-AGENT-001's own plan already waits for S6 before accepting a new pin).

### 3.4 Wire contracts for Phases A and B

Phase C wire contracts are sections 6.2 to 6.7.

Phase A
- `SessionLogSubmitResult` 201 body adds `turns: [{ requestId, outcome, storedStatus, reason }]`, `outcome` in `created|merged|skipped_terminal` (no `skipped_duplicate`; an identical resubmit is `merged` with reason `no_change`; duplicate requestIds in one submission are applied last-wins with one outcome each). MCP `sessionlog_submit` returns `{ success, id, turns }`. Status code stays 201 (late-hook snapshot protection).
- Plugin receipt on `skipped_terminal`: `code: skipped_terminal`, `persisted: false`, `storedStatus`, `reason`; verb returns false; exit 1; failsafe cleared. The `appendDialog` and `setTurnTitle` receipts map their own route results: 200 `persisted`, 404 `request_unknown`, an outage `queued` with a failsafe record keyed by the bound requestId; a submit answered without per-turn outcomes is reported `outcomeVerified: false` and never as a confirmed close (FR-MCP-SESSIONTURN-004-AC005). Every receipt reports a retryable answer (transport outage, 503 `backend_unavailable`, a busy store's 503 `persistence_error` with `retryable: true`, or 409 `conflict` with `retryable: true`, the answer a deadlock or busy store returns once SerializableWrite's four attempts are spent) as `queued` with its failsafe record kept, exit 0, as for an outage (section 3.1, "Deadlock and busy outcomes"; a review keeps its no-failsafe rule and is reported with the retryable code, section 5.2); a final code, `transaction_degraded` and `mutation_not_transactional` from the REPL's client mutation policy included (C9), is reported with that code, exit 1 (process PersistTurnWriteWithReceipt, section 4.4).
- Hook statuses (the full list is in section 4.4, OpenTurnFromPromptHook): new `system-event-skipped`, `turn-open-contended`, `turn-opened-degraded` (an outage or a retryable answer: the turn is opened locally under its `pending-<nonce>` id and its mint record waits for the failsafe drain), `turn-open-failed` (with reason `nonce_source_missing` or the server code); `turn-already-open` when a mint returns `merged` (same nonce) for an in-progress turn, with two exceptions: on copilot and classic cline a `merged` result of a new invocation fails the host session closed under the equal-field rule (`turn_logging_unsupported`, `indistinguishable_arrival`), while a stored mint retry or failsafe drain binds normally; and a `merged` result for a turn that is already terminal reports its `storedStatus` and the hook neither makes it the write target nor adds it to the open list or the stop ledger (FR-MCP-SESSIONTURN-001-AC002).
- Stop gate: blocks `turn_in_progress` for a delivered-ledger turn still in_progress (FR-MCP-SESSIONTURN-003-AC004), blocks `turn_withdrawn_unclosed` through the separate scan of `withdrawn` entries still in_progress, and only warns for a minted request still `queued` although its server turn is in_progress (`StopGateQueuedInProgressServerTurnNoBlockBeforeDeliveryWarns`); in a host session with `unsupportedSince`, a turn whose close is captured as an `unsupported_session` failsafe record is warned with the cause instead, and before that its remedy names only the missing model evidence, the missing `requestClass` (from S8/S9), and the close call, the terminal reviews being judged at the drain, FR-MCP-SESSIONTURN-005-AC001; outside `unsupportedSince`, a turn whose model close is held in the ordinary failsafe by a transport outage is warned as degraded persistence instead, and its ledger entry stays until the server confirms the close), `session_turn_completion_gate` (a turn whose most recent close the server refused with that code, recorded locally from the server response; such a turn is still in progress; no marker condition, FR-MCP-SESSIONTURN-005-AC003), failed build after edits (existing; read from the `lastBuildStatus` of this host session's `turns/<requestId>.yaml` files, where code-verify writes it, section 4.1), stale session (existing); blocks `turn_withdrawn_unclosed` (a withdrawn queued request still in_progress); warns on exactly the FR-MCP-SESSIONTURN-005-AC005 list (process EvaluateStopGate, section 4.4), whose retained failsafe records are a mint refused for a final reason, queued or quarantined, and an `open-failed` queue entry, each naming the prompt, and every quarantined record of an open turn with its reason; re-evaluates on every Stop, including `stop_hook_active=true`, blocking again while turns are open and recording `stop_gate_overridden_by_host` if the host stops anyway; passes with `{}` and clears closed entries. Auto-close removed. Block text: `session_turn_stop_gate: <n> turn(s) are not closed correctly:` then one line per turn `<id> (<code>): <remedy>`, then `The stop gate never closes turns for you.`; each turn's remedy accumulates every unmet prerequisite of that turn, in this order: for a turn whose most recent close the server refused with `session_turn_completion_gate`, the server's missing items first (in place of the locally computed `requestClass` and review items); at least one model-authored action, designDecision, or commit (FR-MCP-SESSIONTURN-005-AC002); `requestClass` (from S8/S9, when the plugin's requestClass and recordReview verbs ship); the terminal reviews the completion gate names (from S8/S9, when the plugin's requestClass and recordReview verbs ship, gate order; before S10 the server accepts those writes, so following the remedy literally still passes); then the single close call valid for its status: `workflow.sessionlog.failTurn` with reason 'withdrawn before delivery' for a withdrawn request, otherwise `workflow.sessionlog.completeTurn` (or `failTurn` if the work failed), passing requestId <id>. Following a turn's remedy literally makes the next Stop pass for that turn, including a Code turn that lacks both model evidence and terminal reviews (`StopGateEachRemedyFollowedLiterallyNextStopPasses`, `StopGateCodeTurnMissingEvidenceAndReviewsAccumulatedRemedyNextStopPasses`).
- Ingress mint (D1): `POST /qbrainai/sessionlog/{agent}/{sessionId}/mint` (also `/mcpserver/sessionlog/...`), client `MintTurnAsync` and REPL `workflow.sessionlog.mintTurn` (hook and adapter callers only; no MCP tool), exactly as TR-MCP-SESSIONTURN-004 AC-005 specifies: first mint 201 `outcome: created`; same nonce in the same session 200 `outcome: merged` with the same requestId. Failsafe mint record: `{ method: workflow.sessionlog.mintTurn, params: { ingressNonce, queryTitle, queryText, model, planFile: None, todoId: None }, provisionalRequestId: pending-<nonce>, binding: { hostSessionKey, keySource, ingressNonce, requestId: pending-<nonce>, boundAtUtc }, plus the root-id envelope of FR-MCP-SESSIONTURN-007-AC009 }` (hooks send the `None` sentinels; a later update sets the real values). Drain order: the plugin first replays an idempotent session open for the record's envelope `mcpSessionId`, then the mint; a `session_not_open` refusal is final only after that session open is confirmed (FR-MCP-SESSIONTURN-001-AC008).
- Arrival bridge (D5): its own script entry, which dot-sources `plugins/core/lib-ps/repl-invoke.ps1` for the section 3.5.6 functions (section 3.1, "Plugin function home"); `sessions/<key>/arrival-bridge.pid` and the `turn-state.yaml` lifecycle below (no separate offset file); the bridge is the only minter on claude-code and claude-cowork, a fail-closed host session records `unsupportedSince: { causeOffset, causeOperation, reason }`, and a session-log verb applies, before binding, only the complete records that end at or before the transcript length it recorded when it started (D2); the Stop hook (from its payload `transcript_path`) and SessionStart apply pending records through `Sync-PluginArrivalLog` the same way, up to the transcript length at their own start, so a dead bridge never hides a delivered request from the Stop gate.
- Plugin per-turn store: `turns/<requestId>.yaml` (one file per turn, own lock `turns/<requestId>.lock`; it holds the turn's `lastBuildStatus`, which code-verify writes under that lock in place of `current-turn.yaml`, where it writes it today (`plugins/core/lib-ps/plugin-hook.ps1:1209,1262` at `a8d3934b`), and from Phase C the request's `packageId`, section 4.1), `sessions/<hostSessionKey>/turn-state.yaml` `{ writeTarget: { requestId, deliveredAtUtc, source: prompt-hook | dequeue | absorbed-mid-turn } | null, openDelivered: [requestId], queue: [{ kind: request | system | open-failed (an `open-failed` entry keeps the queue position of a request whose mint was refused finally, holds no requestId, and is dropped without delivery by its dequeue, remove, or popAll), requestId and nonce (request entries only), enqueueOffset, enqueueTimestamp, promptSha256, state: queued | delivered | withdrawn, stateOffset, mintedAtUtc, storedStatus (set when a mint returned a terminal turn: the entry keeps its queue position and is never delivered) }], appliedThroughOffset, mcpSessionId, unsupportedSince, agentContextMode: Off | Pilot | Stateless }, every entry also carrying the root ids of FR-MCP-SESSIONTURN-007-AC009` (`unsupportedSince` per section 3.2; `agentContextMode`, from Phase C, is the host session's Mode cache of section 3.1, "Mode changes": absent before Phase C and read as `Off` when absent) (written only by the writers TR-MCP-SESSIONTURN-003 AC-007 lists: the arrival bridge, or a verb, the Stop hook, or SessionStart applying pending transcript records through `Sync-PluginArrivalLog`, the prompt hook on hosts without a bridge, the first-ingress writer on hosts without a SessionStart event (A8: it records the host session's `mcpSessionId` under the host session lock before the mint), the close verbs, the failsafe drain (alias rebind), the Stop gate (clearing confirmed-closed ledger entries and recording `stop_gate_overridden_by_host`), the first verb or hook that records `unsupportedSince` with reason `duplicate_prompt_hook`, and SessionStart recording the host session's own `mcpSessionId` and clearing a cause its rule allows, and, from Phase C, the two Mode cache writers: the host-session-start read, by StartHostSession (or, on a host without a SessionStart event, classic cline, cline-v2, and opencode, by the first-ingress writer at the first prompt), which reads the Mode with GetAgentContextStatusQuery, the plugin's only agent-context read at host session start, and records it, `Off` when it cannot read it (a read that misses the hook bound included) or does not know the value, and the build and submission paths of RunStatelessInvocation, which record `Off` after a 409 `agent_context_disabled`; each write one atomic replace under `turn-open.lock` through `Update-TurnState`; RunStatelessInvocation reads `agentContextMode` at each prompt; the bridge, the mint, and every verb read the MCP session id only from this file, the workspace-level `session-state.yaml` is a legacy file never used for targeting, and the rotated-sessionId rebind of `repl-invoke.ps1:1954-1965` is never applied to per-turn files), `sessions/<hostSessionKey>/ended.yaml`, `current-turn.yaml` kept only as a read-only legacy status file `{ turnRequestId }` never used for targeting (section 4.1 states its readers and the records whose targeting it replaces), the stop ledger as `ledger: [...]` inside `sessions/<hostSessionKey>/turn-state.yaml` (one file per host session, so an entry is added in the same atomic replace that delivers its request and advances `appliedThroughOffset`, never at mint; a crash leaves both or neither), all under `turn-open.lock`. No prompt ledger and no dedupe window. Every verb invocation carries a binding `{ hostSessionKey, keySource, ingressNonce, requestId, boundAtUtc }` (C4) captured once at start (D2) and stored in its persist and failsafe records; failsafe paths and fingerprints include the bound requestId. Verbs accept an explicit `requestId` override, required while two or more delivered requests are open (`request_ambiguous`, D9); unknown ids are refused with `request_unknown`.

Phase B turn shape (additive on `UnifiedRequestEntryDto`, server and client)
```json
"requestClass": "Code",
"agentReview": {
  "status": "Pass", "otherNotes": null,
  "correctness": 0.9875, "completeness": 1,
  "correctnessThreshold": 0.98, "completenessThreshold": 0.98,
  "reasons": ["..."], "reviewer": "ClaudeCode/opus-5.5-xhigh",
  "reviewedAt": "2026-10-09T14:03:12.123+00:00",
  "requestJsonlPath": null, "responseJsonlPath": null
},
"hostileReview": { "status": "Unstarted" }
```
- Unset review fields are emitted as `null` on every read surface (the `hostileReview` example above abbreviates them).
- Scores: JSON number or numeric string in, normalized number out (trailing zeros stripped). Every reader validates the raw lexeme before conversion (C#: `Utf8JsonReader.ValueSpan`/`ValueSequence` in `ScoreLexemeJsonConverter` (namespace `QBrainAi.Client.Scoring`, section 3.5.4) and `JsonElement.GetRawText()` in the REPL's lossless normalizer, which calls `ScoreLexeme`; PowerShell: the YAML/JSON scalar text kept by `Protect-ReplLosslessScalars`; the TypeScript-host adapters pass review scores through as strings and never parse them): pattern `^[0-9]+(\.[0-9]+)?$`, at most 28 fractional digits, value in [0,1]; then `decimal.Parse(lexeme, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)`, which is exact for every accepted lexeme (mantissa at most 10^28 < 2^96). Errors `review_score_format`, `review_score_precision`, `review_score_out_of_range`. Thresholds read-only (client values ignored on input); they are declared under the canonical root `QBrainAi: SessionLog: ReviewThresholds:` in `appsettings.yaml` and read as the `Mcp:SessionLog:ReviewThresholds:*` keys of section 3.1 (section 5.1).
- Errors (HTTP 400 with the listed code in the `ClassifiedError` `code` field, unless noted; the completion gate is the one exception, section 3 Errors): `review_invalid_status`, `review_unnecessary_not_client_writable`, `review_other_notes_not_allowed`, `review_other_notes_required`, `review_field_forbidden_for_status`, `review_field_required_for_status`, `review_score_out_of_range`, `review_receipt_paths_required`, `review_score_format`, `review_score_precision`, `review_section_reset_on_terminal_turn`, `review_field_too_long`, `review_timestamp_invalid`, `review_reason_blank`, `review_status_conflict` (422, includes thresholds in force and derived status), `request_class_invalid`, and the completion gate (`code` `validation_error`, message starting with the token `session_turn_completion_gate` and listing the missing items in order requestClass, agentReview, hostileReview; wherever this document names "the completion-gate refusal" or `session_turn_completion_gate` as a refusal, it means this body). A body-binding failure (a body that does not parse or bind) keeps today's validation problem, `ValidationProblemDetails` with `errors` keyed by the offending JSON path (`InvalidModelStateResponseFactory`, `src/QBrainAi.Support.Mcp/Program.cs:743-770` at `a8d3934b`), as FR-SUPPORT-012 and TR-SUPPORT-LOG-010 require; the domain refusals above are `ClassifiedError` problem+json bodies that, where they name a field, name it by its JSON path in `details.field` (for example `status` or `hvVerdict`), never by an action-parameter name, and carry no `errors` member (replaces, for these domain refusals only, the `errors` placement of the JSON path in FR-SUPPORT-012's third acceptance criterion and its twin FR-SUPPORT-010B with `details.field`).
- Write isolation: every Phase A or Phase B unit that evaluates the completion gate, the terminal-state guard, or a review rule and then saves (submit, upsert, patch, replace, complete, fail, the review record, and the review section reset) runs that check and its save through SerializableWrite (section 3.5.2), with turn-level isolation for a single-turn write and the session-level scope for a submission (section 3.1, "Turn-level isolation"; the mint, MintSessionTurnCommand, is a single-turn write too, section 3.1, "Mint isolation"), so a concurrent review reset, class change, or close of the same turn commits before or after it, never between its check and its save, writes to different turns of one session never answer a conflict, and when SerializableWrite's default bound (four attempts) is exhausted the unit returns 409 `conflict` with `retryable: true` and saves nothing; the forced-interleaving cases are listed there and run at the gates of the bounded contexts that own those units (section 8.4).
- Sections: `agentReview`, `hostileReview` (case-insensitive; aliases `agent-review`, `agent_review`, `hostile-review`, `hostile_review`). Section PUT replaces (latest wins); section DELETE resets to the default status, except on a completed or failed turn, where it returns 400 `review_section_reset_on_terminal_turn` and changes nothing; item DELETE returns 400.
- New surfaces: MCP tool `sessionlog_record_review(agent, sessionId, requestId, kind, reviewJson, workspacePath)`; the existing MCP tools `sessionlog_replace_section` and `sessionlog_clear_section`, called with a review section or alias, dispatch RecordSessionTurnReviewCommand and ResetSessionTurnReviewCommand instead of calling the service directly as today (`src/QBrainAi.Support.Mcp/McpStdio/FwhMcpTools.SessionLog.cs:203-253` at `a8d3934b`; `McpSectionToolsReviewSectionDispatchReviewUnits`), and `sessionlog_delete_item` on a review section is refused like the REST item route (`FwhMcpTools.SessionLog.cs:256-279`; section 5.2); client `RecordTurnReviewAsync`, `ClearTurnReviewAsync`; REPL/plugin `workflow.sessionlog.recordReview` (for kind hostile also accepts `hvVerdict`, which the REPL, the MCP tool's `reviewJson`, and the client carry unchanged to the server's mapping; the plugin verbs also accept `hvReceiptPath`, which only the plugin reads and maps (`ConvertFrom-McpHvReceipt`, for every host) and never forwards); `updateTurn`/`completeTurn`/`failTurn` accept `requestClass`, `designDecisions`, `requirementsDiscovered`, `blockers`. `FinalizeLifecycleTurnToolAsync` copies `requestClass` in addition to its existing field list (`FwhMcpTools.SessionLog.cs:328-369`, which never copies planFile or todoId); every other existing REST and MCP request and response shape stays as it is (architecture document sections 4.2 and 5.2 state each surface separately). Phase B adds no REST routes; Phase A adds the mint route, and Phase C adds the `/qbrainai/agent-context/*` routes (architecture document section 6.5).
- Search: `BuildSearchText` includes reviewer, reasons, and otherNotes; excludes requestClass, scores, thresholds, paths; the text filter runs over the raw stored turn, so review text is searched like every other field: a query for a raw value the sanitizer redacts in a review field still finds its turn, which reads back redacted (FR-MCP-SESSIONLOGSAN-001 ac4 kept, `tests/QBrainAi.Support.Mcp.IntegrationTests/Controllers/SessionLogSanitizationControllerTests.cs:95` at `a8d3934b`), and a reason present only in a replaced record no longer matches (`SessionLogSanitizerReviewTests.SearchRawReviewValueMatchesReadsRedactedReplacedReasonNoMatch`, S4; this updates TR-MCP-SESSIONREVIEW-002 AC-005, whose "a redacted review value matches no turn" clause is removed and whose case takes this name, section 5.2, QuerySessionLogsQuery).
### 3.5 Shared components

Operator rulings (2026-10-12, verbatim): "One thing I'm detecting as the design goes by is that it appears code will be duplicated under the CQRS and Unit of Work directives.  I want to be clear that DRY is a fundamental principal.  If a pice of logic is hared between multiple UoW, that logic should be encapsulated and shared."; "My goal is to get to a point where DRY is enforceable through tools, not process.  This is not a statement of work!"; and "\"Where it goes\" and \"Who Owns It\" are jobs for namespaces.  \"What it is\" and \"What it does\" are class and member names."

Rules for every diagram and unit in this document:

- Each component below is defined once, here. A unit of work, policy, or process that uses it draws one call node (`[["ComponentName ..."]]`) and only the exits the component returns; it never redraws the component's steps. Sequence diagrams show a component as one message or one participant.
- A component's error codes and HTTP statuses are the ones stated here; a unit lists them in its error list but does not restate the rule.
- Namespaces say where a type goes and who owns it; type and member names say what it is and what it does, with no owner or feature prefix for generic behavior. A new type's namespace is its project's RootNamespace followed by its folder path, as IDE0130 enforces on the analyzer branch that merges first (section 3.1, "Analyzers first, adopt rules"): `QBrainAi.Services` and `QBrainAi.Support.Mcp` have RootNamespace `QBrainAi.Support.Mcp`, `QBrainAi.Storage` has `QBrainAi.Support.Mcp.Storage`, and `QBrainAi.Client`, `QBrainAi.Cqrs`, and `QBrainAi.Repl.Core` use their assembly names; folders are the bounded context, then the layer. One type per file (MA0048), so a command or query record and its handler are separate files. Test methods are named without underscores (CA1707). Existing develop types are cited by their develop names and namespaces as of `a8d3934b`; renaming them is context, not work.
- A type that both the server and the REPL need lives in `QBrainAi.Client`, the one assembly both reference (`QBrainAi.Repl.Core` references only `QBrainAi.Client` and `QBrainAi.Common.AgentCli`; the server reaches `QBrainAi.Client` through `QBrainAi.Storage`); no new project is added.
- New DTOs are defined once, in `QBrainAi.Client.Models.<Context>`, and the server uses those types (the server already references `QBrainAi.Client`, `src/QBrainAi.Storage/QBrainAi.Storage.csproj:43`); existing DTOs that develop defines twice (for example `UnifiedRequestEntryDto` in `src/QBrainAi.Client/Models/SessionLogModels.cs:108` and `src/QBrainAi.Services/Models/UnifiedSessionLogDto.cs:135`) keep both definitions, and the Phase B fields are added to both.
- Every component has one implementation and one test class named for it (for example `SerializableWriteTests`); a unit's tests use the component's Phase 2 seam and never test the component's own branches again.

Owners (section 8.3): SharedComponents owns 3.5.1 and 3.5.2; SessionTurnLogging owns 3.5.3; TurnReviews owns 3.5.4; ContextPackages, ResponsesAndTrust, and ContextGraph own the parts of 3.5.5 named there; PluginCorePowerShell owns 3.5.6.

#### 3.5.1 Request pipeline (CQRS behaviors)

- Dispatch scope (`QBrainAi.Cqrs`, section 3.1 "Workspace resolution"): `IDispatcher` is registered scoped, as a facade that resolves the handler and every behavior from the DI scope it was itself resolved in and runs them through the singleton `Dispatcher`, which keeps its logger-provider and active-dispatch bookkeeping. The scope is the caller's: the HTTP request scope on REST and MCP Streamable HTTP, the per-call scope the MCP server opens on stdio (the ModelContextProtocol 0.8.0-preview.1 option `McpServerOptions.ScopeRequests`, "create a new service provider scope for each handled request", default on), and a scope the caller creates for a hosted service or the stdio host's startup work (`McpStdioHost.cs:340`). A dispatch made inside a handler or inside a SerializableWrite delegate uses the `IDispatcher` of that scope, so it shares the caller's `McpDbContext` and transaction. Today the singleton resolves transient handlers from the root provider (`src/QBrainAi.Cqrs/Dispatcher.cs:17,93,96`, `ServiceCollectionExtensions.cs:24-25,66`), so a handler that injects `McpDbContext` or `WorkspaceContext` (the UseCases, Products, and Memory handlers) receives root-level instances; the change ends that. It changes the published `QBrainAI.Cqrs` package (TR-MCP-CQRS-001, TR-MCP-CQRS-005), so the existing UseCases, Products, Memory, and GraphRAG suites run once through the production composition (`ExistingHandlersProductionPipelineBehaviorUnchanged`), with `DispatchNestedSharesDbContextAndTransaction` and `DispatchParallelRequestsTwoWorkspacesNoSharedDbContext`.
- `WorkspaceResolutionBehavior` (namespace `QBrainAi.Support.Mcp.Workspaces`, an `IPipelineBehavior` registered with `AddCqrsBehavior` in both composition roots, `src/QBrainAi.Support.Mcp/Program.cs:489-490` and `src/QBrainAi.Support.Mcp/McpStdio/McpStdioHost.cs:319-320`, for every command and query, existing ones included; `src/QBrainAi.Cqrs/IPipelineBehavior.cs:9`): a record that carries a non-empty `WorkspacePath` (the MCP tool's `workspacePath`, the REST controller's resolved workspace, the REPL session workspace) has it resolved through `WorkspaceIdentity` into the dispatch scope's `WorkspaceContext` (`src/QBrainAi.Storage/WorkspaceIdentity.cs:22`, `src/QBrainAi.Storage/Services/WorkspaceContext.cs:8`, TR-MCP-MT-001), and calls `OverrideWorkspaceId` on the scope's `McpDbContext` as the REST middleware does (`WorkspaceResolutionMiddleware.cs:255`), because the context captures its workspace at construction (`src/QBrainAi.Storage/McpDbContext.cs:25-28`) and the handler, with its context, is resolved before the pipeline runs (`src/QBrainAi.Cqrs/Dispatcher.cs:93-107`; `WorkspaceResolutionRecordPathFiltersDbContextCreatedBeforeBehavior`); a record with an empty path keeps the workspace the transport already resolved into that scope (on REST, `WorkspaceResolutionMiddleware`, which keeps answering before any controller with today's bodies, TR-MCP-MT-002, including its API-key resolution and its fourteen workspace-independent route prefixes, `WorkspaceResolutionMiddleware.cs:32-46`). A record that may run with no workspace at all implements `IAllowsGlobalWorkspace` (same namespace), as global memory requires (FR-MCP-MEMORY-001, `MemoryController.cs:125`, `MemoryGlobalDefaultWorkspaceTests:100`). Failure: a non-empty path that names no registered workspace, or no workspace from the record or the transport on a record that does not allow it, is refused with 400, the status TR-MCP-MT-002 gives REST, in the `ClassifiedError` body `validation_error` with `details.field` `workspacePath` (`WorkspaceResolutionUnregisteredPath400EverySurface`, `WorkspaceResolutionParallelDispatchesTwoWorkspacesNoCrossWorkspaceRead`, `WorkspaceResolutionGlobalMemoryCreateNoWorkspaceCreated`). The behavior never refuses a dispatch develop serves today; on REST a unit-level workspace failure is therefore reachable only through a record path that names an unregistered workspace.
- `ModeGateBehavior` (namespace `QBrainAi.Support.Mcp.AgentContext`): applies only to records that implement the marker interface `IModeGatedRequest` (same namespace); while `Mcp:AgentContext:Mode` is `Off` it refuses them with 409 `agent_context_disabled`. It reads `Mode` through `IOptionsMonitor<AgentContextOptions>` at each dispatch (the pattern of `src/QBrainAi.Services/Services/AgentPoolService.cs:36`), so a reloaded configuration takes effect without a restart (`ModeGateReloadedConfigurationOffRefusesWithoutRestart`; section 3.1, Mode at runtime). `ModeGateEveryAgentContextCommandExceptTrustDecisionImplementsMarker` reflects over the agent-context command records and fails when one other than DecideAgentResponseTrustCommand lacks the marker (section 3.1, Mode gate completeness). Gated: every agent-context command except DecideAgentResponseTrustCommand (dispatched only by the grade units). Not gated: GetAgentContextStatusQuery and every read-only agent-context query, so the stored records stay readable for audit in `Off` (the attached plan: "Preserve rejected and stale responses for audit"). A requirement update or delete dispatches EvaluateStalenessCommand only while Mode is not `Off`; after a return from `Off`, reads evaluate freshness themselves (section 6.5).
- Order: both behaviors run inside the dispatcher, after the controller or tool calls `SendAsync` or `QueryAsync` and before the handler; request validation runs in the handler. Every sequence diagram draws these refusals after the dispatch.

![RequestPipeline: WorkspaceResolutionBehavior and ModeGateBehavior (activity), part 1 of 2](c4/rendered/RequestPipeline.svg)

View: RequestPipeline

![RequestPipeline: WorkspaceResolutionBehavior and ModeGateBehavior (activity), part 2 of 2](c4/rendered/RequestPipeline-part2.svg)

View: RequestPipeline-part2

#### 3.5.2 Storage and errors

- `DbErrorClassifier` (namespace `QBrainAi.Support.Mcp.Storage.Errors`): `IsUniqueViolation(exception, out indexName)` by provider code (SQL Server 2601 and 2627, PostgreSQL 23505, SQLite 2067 and 1555) and `IsTransientConflict(exception)` (SQL Server 1205, PostgreSQL 40001 and 40P01, SQLite 5 busy and 6 locked). A timeout or transport failure is never a transient conflict (it may have committed; `HandoffDbExceptions.IsCommitAmbiguous`, `src/QBrainAi.Services/Services/HandoffDbExceptions.cs:49`, keeps that meaning). Develop's message-text predicates (`SessionLogService.cs:234`, `:357`, `:812-817`) are context; new code calls only this classifier.
- `SerializableWrite` (namespace `QBrainAi.Support.Mcp.Storage.Transactions`): runs a unit's checks and its save as one delegate in one Serializable transaction on SQLite, PostgreSQL, and SQL Server (the pattern of `SessionLogService.SubmitAsync`, `SessionLogService.cs:153-157`, and `RequirementsRecoveryService.cs:80-126`). Every unit that evaluates the completion gate, the terminal-state guard, a review rule, a response status, or an idempotency key and then saves runs through it, so a concurrent write to the same turn or response commits wholly before or after it. Per-response writes name their responses: the first statements increment each response's `StatusVersion` in ascending `ResponseId` order. Isolation scope (section 3.1, "Turn-level isolation"): a single-turn write reads its session row only to locate and validate the parent and writes only the one turn with its children; the session row's summary fields change only in the separate short write after the turn commits, whose failure never fails the turn write (`TurnWriteSummaryRefreshFailureNeverFailsTurnWrite`; develop refreshes them inside the turn's save, `SessionLogService.cs:762,794,879` at `a8d3934b`), so no two single-turn transactions write the same row, and writes to different turns of one session never answer a conflict (`TurnWriteConcurrentDifferentTurnsSameSessionBothCommitNoConflictAnswer`, dense-session fixture, three providers). Per provider: on SQL Server and PostgreSQL both transactions read the session row without updating it, so neither blocks the other on it, and a key-range or serialization conflict between two inserts near the same index position ends the loser's attempt, which SerializableWrite reruns within its bound; on SQLite the database write lock serializes the two writes, which wait within the busy timeout. A multi-turn submission keeps its session-level scope and refreshes the summary inside its own transaction. On the EF in-memory provider, which has no transactions, SerializableWrite runs the delegate once, as develop does (`SessionLogService.cs:155`). A caller-owned transaction is refused as develop refuses it today (`SessionLogService.cs:119-128`). A delegate reads and writes only the database: a unit reads the git commits, files, sources, and GraphRAG results it needs before SerializableWrite, and the delegate re-checks the database state those reads relied on (each locked response's `StatusVersion`, the turn's status); a changed state ends the attempt and the unit runs again from its reads, within the same bound. On SQLite a Serializable transaction holds the database write lock from its start, so no hook write waits on a retrieval or a git read (`BuildOnSqliteHookWriteDuringRetrievalNotBlocked`). A transient conflict, or a unique violation on a key the delegate declares (an idempotency key, a readable key), rolls the transaction back and runs the whole delegate again, at most three times (four attempts) unless the caller states another bound and exhaustion outcome (only the Phase A minter does, ReadableKeyAllocator below); then the unit returns 409 `conflict` with `retryable: true` and saves nothing. The attempts run within the unit's existing storage budget (FR-MCP-SESSIONLIFE-003). A refusal the delegate returns rolls back and is returned unchanged. A command dispatched from inside a delegate (for example DecideAgentResponseTrustCommand from a grade unit) joins the caller's transaction and retry; it never opens a second one. SharedComponents proves the component on a test entity it owns (`SerializableWriteForcedInterleavingTestEntityCommitsWhollyBeforeOrAfter`, `SerializableWriteRetriesExhausted409ConflictRetryableNothingSaved`); the turn cases belong to the gates of the contexts that own those units: `TurnWriteForcedInterleavingReviewResetVsCompleteNeverClosedWithoutReview`, `TurnWriteForcedInterleavingClassChangeVsCompleteGateSeesCommittedClass`, and `TurnWriteForcedInterleavingChoreFlipVsRunningWriteRunningKept`. Every case runs on SQLite, PostgreSQL, and SQL Server.

![SerializableWrite (activity), part 1 of 3](c4/rendered/SerializableWrite.svg)

View: SerializableWrite

![SerializableWrite (activity), part 2 of 3](c4/rendered/SerializableWrite-part2.svg)

View: SerializableWrite-part2

![SerializableWrite (activity), part 3 of 3](c4/rendered/SerializableWrite-part3.svg)

View: SerializableWrite-part3

- `IdempotentCreate` with `PayloadFingerprint` (namespace `QBrainAi.Support.Mcp.Storage.Idempotency`; precedent `TodoPayloadFingerprint`, `src/QBrainAi.Services/Services/TodoPayloadFingerprint.cs:10`): runs inside SerializableWrite. It looks the key up first, before any precondition that can change between attempts (a source, a status, a commit), so a retry of a lost response returns the stored record; a unit that reads outside the database before its SerializableWrite (Build, Submit, the grades) runs the same lookup read-only before those reads, so such a retry returns the stored record without reading any source. The fingerprint is SHA-256 over the canonical JSON of the client-supplied inputs only, never over server-observed sources. The caller states its mismatch policy: `Conflict` (every agent-context create: 409 `conflict` with `retryable: false`), `ReturnStored` (MintSessionTurnCommand: one host event, so a repeated nonce returns the stored turn whatever the payload; RelationshipWriter: identical links), or `MergeInto` (the insert-when-absent turn writes: the write is merged into the stored turn). A unique violation on the key index ends the attempt; SerializableWrite runs the unit again and the next lookup finds the winner, so one rule covers a retry and a concurrent duplicate. Soft delete (TR-MCP-DB-003): `McpDbContext` turns every delete into a soft delete and hides deleted rows through its query filter, while the unique indexes cover deleted rows too (`McpDbContext.cs:391,404,1302-1336`). The lookup therefore reads past the filter: a soft-deleted record with the key is revived (its deletion columns cleared) and then written as a new record with the new inputs and fingerprint (`created: true`), the pattern develop uses for a tombstoned session (`SessionLogService.cs:161-169`, `SessionLogResubmissionReviveTests`), so a deleted link or record can be created again (`IdempotentCreateSoftDeletedKeyRevivedCreatedTrue`).

![IdempotentCreate (activity), part 1 of 5](c4/rendered/IdempotentCreate.svg)

View: IdempotentCreate

![IdempotentCreate (activity), part 2 of 5](c4/rendered/IdempotentCreate-part2.svg)

View: IdempotentCreate-part2

![IdempotentCreate (activity), part 3 of 5](c4/rendered/IdempotentCreate-part3.svg)

View: IdempotentCreate-part3

![IdempotentCreate (activity), part 4 of 5](c4/rendered/IdempotentCreate-part4.svg)

View: IdempotentCreate-part4

![IdempotentCreate (activity), part 5 of 5](c4/rendered/IdempotentCreate-part5.svg)

View: IdempotentCreate-part5

- `ReadableKeyAllocator` (namespace `QBrainAi.Support.Mcp.Storage.Keys`): allocates the human-readable keys of section 3 (Identifiers). Strategy `NextOrdinal` (`<prefix><parent>-<nn>`, highest stored suffix within the key's unique index scope plus one; the agent-context keys `pkg-`, `rsp-`, and `grd-` under their record parent, `rel-` under its type and timestamp, and `hvr-` under its session and timestamp with `-<nn>` appended only from the second key of the same second, each with the shortened parent id rule and the formats of section 6.3) or `RandomHex` (`req-<yyyyMMddTHHmmssZ>-<8 hex>`, the Phase A minter). `NextOrdinal` reads past the soft-delete filter, so a deleted record's suffix is never allocated again (`ReadableKeyAllocatorSoftDeletedSuffixNeverReused`). A collision on the key's unique index declares a retry to SerializableWrite with the next candidate. The Phase A minter states its own bound and exhaustion outcome to SerializableWrite for request-id collisions only: today's six attempts and 500 `mint_collision_exhausted` (InsertMintedTurnWithRetry, section 4.2); its transient conflicts keep the default bound and exit; agent-context keys use SerializableWrite's default bound (four attempts), then 409 `conflict` with `retryable: true`.
- `PagedQuery` and `PageBounds` (namespace `QBrainAi.Support.Mcp.Storage.Paging`) with the wire type `PageResult<TItem>` (namespace `QBrainAi.Client.Paging`, fields `totalCount`, `limit`, `offset`, `items`): counts the matches and reads one page in the caller's stable order. `PageBounds` carries the policy: `Clamp` (QuerySessionLogsQuery, today's behavior: limit default 100, clamped to 1..1000, a negative offset read as 0, `SessionLogService.cs:476-477`) or `Refuse` (every agent-context query and RetrieveAgentContextCandidatesQuery's `limit`: limit 1..1000, default 50, offset at least 0, otherwise 400 `validation_error` with `details.field` `limit` or `offset`). The four agent-context page types of section 6.4 are `PageResult<TItem>`.

![PagedQuery (activity), part 1 of 2](c4/rendered/PagedQuery.svg)

View: PagedQuery

![PagedQuery (activity), part 2 of 2](c4/rendered/PagedQuery-part2.svg)

View: PagedQuery-part2

- `ClassifiedErrorException` and `ErrorCodes` (namespace `QBrainAi.Client.Errors`): every new handler fails with `Result<T>.Failure(new ClassifiedErrorException(code, status, message, details, retryable))` (`src/QBrainAi.Cqrs/Result.cs:50`); `ErrorCodes` is the one table of codes with their HTTP status and retryable flag, read by the server, the REPL envelope, and the plugin's receipt mapping (C9). `McpErrorClassifier` (`src/QBrainAi.Support.Mcp/Services/McpErrorClassifier.cs:14`) maps `ClassifiedErrorException` to the `ClassifiedError` problem+json body and the MCP tool error in one branch; it gains no per-feature branch. Retryable codes: transport outage and degraded persistence (503 `backend_unavailable`), a busy store (503 `persistence_error` with `retryable: true`, today's answer, `McpErrorClassifier.cs:129-139`), and 409 `conflict` when `retryable` is true (SerializableWrite exhausted, key allocation exhausted); every other code is final, including the REPL client-policy codes `transaction_degraded` and `mutation_not_transactional` (`ClientMutationPolicy.cs:203-218`), which `ErrorCodes` lists. The REPL classifier carries a server `ClassifiedError` answer through at every status: its `code`, `message`, `retryable`, and `details` reach the REPL envelope unchanged, where today only a 503 passes the server's code through and `conflict` is forced to `retryable: false` (`src/QBrainAi.Repl.Core/ReplMcpErrorClassifier.cs:60-95`), and any other answer becomes a classifier code or `method_invocation_error` (`src/QBrainAi.Repl.Core/ReplCommandDispatcher.cs:3346-3370`); so the plugin sees the completion-gate token, the review and agent-context codes, and the server's retryable flag (`ReplClassifierConflictRetryableTrueKept`, `ReplClassifierServerClassifiedErrorKeepsCodeRetryableDetailsEveryStatus`). Exceptions without a server body keep today's classification.
- `SecretRedactor` (namespace `QBrainAi.Support.Mcp.Redaction`): `Redact(text)` and `ContainsSecret(text)` over the rules of `SessionLogSanitizer` and `ApiKeyRedaction` (`src/QBrainAi.Services/Services/SessionLogSanitizer.cs:15`, `src/QBrainAi.Common.AgentCli/ApiKeyRedaction.cs:9`). It is the one redactor behind the session-log sanitizer's review paths and the agent-context secret filter and secret scan (section 3, Secrets).
- `ContentHasher` (namespace `QBrainAi.Support.Mcp.Hashing`): SHA-256 over UTF-8 bytes, lower-case hex, of the text's stored normal form; every hash this plan names is computed through it. `McpDbContext` rewrites every string it saves through `LineSanitizer.Sanitize`, which replaces U+2014 and U+2013 with an ASCII hyphen (`McpDbContext.cs:1351-1360,1704-1718`, `src/QBrainAi.Common.AgentCli/LineSanitizer.cs:23-28`); ContentHasher applies the same function before hashing, and every byte-for-byte comparison of this plan (Replay identity, `verdict_binding`, `reviewer_turn`) compares normal forms, so a value read back from the store hashes and compares as it was recorded (`ContentHasherTextWithEmAndEnDashesStoredAndReadBackHashEqual`).

#### 3.5.3 Session log (namespace `QBrainAi.Support.Mcp.SessionLog`)

- `TurnStatus`: `IsTerminal(status)` is true for `completed`, `failed`, `closed`, `canceled`, and `cancelled` (develop's set, `SessionLogService.cs:1355-1361`); every "terminal" and "nonterminal" in this document means this predicate. Agent surfaces (REST, MCP, client, REPL, plugin) accept the statuses `in_progress`, `completed`, `failed`, and `canceled` on writes (the legacy spelling `cancelled` stays accepted, as FR-MCP-SESSIONLIFE-002 relies on); a write naming `closed` is refused with 400 `validation_error` (operator, 2026-10-12, answer "Refuse from agent surfaces (Recommended)"); server-internal writers and existing rows keep `closed`.
- `TurnLocator`: validates the session id and, when the unit addresses a turn, the request id (`SessionLogIdentifierValidator`, `src/QBrainAi.Services/Services/SessionLogIdentifierValidator.cs:8`), finds the session by (workspace, agent, sessionId) and the turn by (session, requestId). It returns typed results; each surface maps them to its existing body, listed in the unit's error list. Every unit that addresses a session or a turn, including the review units and the agent-context units that name a turn, calls it with the same key; an agent-context request that names a turn carries `agent`, `sessionId`, and `requestId`.

![TurnLocator (activity), part 1 of 3](c4/rendered/TurnLocator.svg)

View: TurnLocator

![TurnLocator (activity), part 2 of 3](c4/rendered/TurnLocator-part2.svg)

View: TurnLocator-part2

![TurnLocator (activity), part 3 of 3](c4/rendered/TurnLocator-part3.svg)

View: TurnLocator-part3

- `TurnWritePipeline`: the one write path of every turn-writing unit (SubmitSessionLogCommand per turn, UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, FailSessionTurnCommand), run inside SerializableWrite. Modes: `Submit` (policy TerminalTurnOutcomeGuard first; the completion gate's missing items are collected and the submission is rejected once after every turn is evaluated), `Merge` (additive; insert when absent; Upsert, Patch), `Replace` (whole replacement; the turn must exist), and the status overrides `Complete` and `Fail` (Merge with the status set). The single-turn modes reject at the first refusal. The insert-when-absent race is IdempotentCreate with policy `MergeInto` (one retry mechanism, no separate race step). Review records carried in the turn body pass policy ReviewValidation (section 5.3) before RequestClassification. The completion gate runs when the write moves the turn into `completed` or `failed`, or when a Replace carries `completed` or `failed` (FR-MCP-SESSIONCLASS-002-AC004), never on any other write that only enriches an already terminal turn. The QBAgent terminal compliance check (`ValidateTerminalTurnCompliance`, `SessionLogService.cs:1329`) runs in the single-turn modes only, as today. Single-turn modes read only their session row and their turn (turn-level isolation); the session summary refresh runs after the turn commits, as its own short write whose failure never fails the turn write. Outputs: the turn id (single-turn modes) or the per-turn outcome (`Submit`). Typed failures, mapped by each surface to the bodies its unit lists: `InvalidIdentifier`, `SessionNotFound` (never in `Submit` mode, which opens or upserts its session first), `TurnNotFound`, `InvalidPayload`, `ReviewInvalid` (with the policy ReviewValidation code), `RequestClassInvalid`, `CompletionGateMissing` (with the missing items), and the SerializableWrite conflict.

![TurnWritePipeline (activity), part 1 of 6](c4/rendered/TurnWritePipeline.svg)

View: TurnWritePipeline

![TurnWritePipeline (activity), part 2 of 6](c4/rendered/TurnWritePipeline-part2.svg)

View: TurnWritePipeline-part2

![TurnWritePipeline (activity), part 3 of 6](c4/rendered/TurnWritePipeline-part3.svg)

View: TurnWritePipeline-part3

![TurnWritePipeline (activity), part 4 of 6](c4/rendered/TurnWritePipeline-part4.svg)

View: TurnWritePipeline-part4

![TurnWritePipeline (activity), part 5 of 6](c4/rendered/TurnWritePipeline-part5.svg)

View: TurnWritePipeline-part5

![TurnWritePipeline (activity), part 6 of 6](c4/rendered/TurnWritePipeline-part6.svg)

View: TurnWritePipeline-part6

- `TurnReadProjection`: the read shape of a turn on every read surface: review text through `SecretRedactor`, scores as their exact decimal lexemes (`ScoreLexeme`), unset review fields `null` (C11). Turn text is stored as written and redacted on every read surface (FR-MCP-SESSIONLOGSAN-001); its review-text redaction and the TR-MCP-SESSIONLOGSAN-001 decorator's review paths are the same `SecretRedactor` call, and search runs on the stored text (FR-MCP-SESSIONLOGSAN-001 ac4). Used by GetSessionLogQuery, QuerySessionLogsQuery, and the record RecordSessionTurnReviewCommand and ResetSessionTurnReviewCommand return.

#### 3.5.4 Scoring and reviews

- `ScoreLexeme` and `ScoreLexemeJsonConverter` (namespace `QBrainAi.Client.Scoring`; the rule of section 3.4, Scores): validates a score lexeme (pattern, at most 28 fractional digits, value in [0, 1]) and parses it exactly; errors 400 `review_score_format`, `review_score_precision`, `review_score_out_of_range`. The converter validates at deserialization, so an invalid score is refused before any lookup (an invalid score on an unknown turn is 400, not 404). The REPL's lossless normalizer calls `ScoreLexeme` for the review keys (`agentReview`, `hostileReview`, `recordReview`) and for the agent-context grade keys (`correctness` and `completeness` of `RecordModelGradeAsync`), so `verdict_match` (section 6.3) compares lexemes that no hop has re-serialized. The PowerShell core's copy (`Protect-ReplLosslessScalars`) is a language boundary, kept in step by the golden contract test of the review contract.
- `ScoreVerdict` (namespace `QBrainAi.Support.Mcp.Scoring`): derives `Pass` or `Fail` from two scores against their thresholds, inclusive (score >= threshold), and returns the thresholds in force for stamping. Parameters: the thresholds and an optional floor and count rule. Phase B reviews use the configured thresholds in [0, 1] and refuse a supplied `Pass` or `Fail` that contradicts the derivation with 422 `review_status_conflict`; agent-context grades and the trust decision apply `max(configured, 0.98)` and require zero FAIL and zero UNKNOWN findings. Used by policy ReviewValidation (RecordSessionTurnReviewCommand and review records in turn bodies), policy HvVerdictMapping, RunDeterministicGradeCommand, RecordModelGradeCommand, and DecideAgentResponseTrustCommand (which re-derives from the stored scores with the floor).
- `ReviewDefaults` (namespace `QBrainAi.Support.Mcp.Reviews`): the default review status for a kind and class: `agentReview` `Unstarted`; `hostileReview` `Unnecessary` when the class is `Chore`, otherwise `Unstarted`. Applied at turn insert, by a section reset, by the Chore flip of policy RequestClassification (only a record still at a default status, only on a nonterminal turn), and by the S3 backfill.

#### 3.5.5 Agent context

- `SourceResolver` and `SourceKind` (namespace `QBrainAi.Support.Mcp.AgentContext.Sources`, owner ContextPackages, called by ResponsesAndTrust and ContextGraph through its Phase 2 seam): resolves a source reference by kind and returns its content and its hash per the canonical forms of section 6.3 (through `ContentHasher`); `ReadCommit()` returns the workspace's HEAD commit, or fails with 409 `agent_context_commit_unavailable` (Build and Submit alike). Files are read at a commit, never from the working copy. A reference that names no existing source is 400 `validation_error` with `details.field` naming the reference, in every unit. `SourceKind` is the one vocabulary (`Request`, `Profile`, `Rule`, `Requirement`, `Todo`, `SessionLogTurn`, `CodeFile`, `PriorResponse`, `Package`, `Receipt`, `Commit`); the wire values `Code` (package items) and `File` (evidence) are aliases of `CodeFile`, and `Response` (relationship nodes) of `PriorResponse`, mapped here only.
- `PackageAssembler`, `CandidateRanker`, and `BuiltPackageLookup` (namespace `QBrainAi.Support.Mcp.AgentContext.Packages`, owner ContextPackages): `CandidateRanker` scores candidates by the rule of section 6.3, rounds half-even to six places, and orders by score descending then `sourceRef`; RetrieveAgentContextCandidatesQuery's relational path uses it. `PackageAssembler` turns a candidate snapshot into a package: secret filter (`SecretRedactor`), omission filter, findings, the mandatory-context check, the budget fill (FillBudgetWithCandidates), and rendering; BuildAgentContextPackageCommand runs it on live candidates and ReplayAgentContextPackageQuery on the stored input snapshot, which therefore records every input the assembler reads (section 6.3). `BuiltPackageLookup` finds a package in the workspace that is not `Overflow`: 404 `not_found` (`details.resource` `package`) or 409 `agent_context_package_overflow`.
- `ResponseStatusTransition` (namespace `QBrainAi.Support.Mcp.AgentContext.Responses`, owner ResponsesAndTrust): changes one response's status inside SerializableWrite with the response, and for a supersede the replacement too, locked in ascending `ResponseId` order. It checks the lifecycle of section 6.3 (a transition already applied with the same target and replacement returns unchanged; a transition the lifecycle does not allow is 409 `agent_context_transition_invalid`), sets `Status`, `StatusChangedUtc`, and `TrustedUtc` for `Trusted`, and applies every side effect in the same transaction: the projection row (entering `Trusted`: `Pending` for ingest; leaving `Trusted`: `Pending` for removal when the row holds a `GraphRagDocumentId`, otherwise `Removed`), and for `Rejected` or `Superseded` the response's `Pending` items in `Open` HV requests become `Closed` with that reason, and each request left with no `Pending` item becomes `Completed` with `CompletedUtc`.

![ResponseStatusTransition (activity), part 1 of 3](c4/rendered/ResponseStatusTransition.svg)

View: ResponseStatusTransition

![ResponseStatusTransition (activity), part 2 of 3](c4/rendered/ResponseStatusTransition-part2.svg)

View: ResponseStatusTransition-part2

![ResponseStatusTransition (activity), part 3 of 3](c4/rendered/ResponseStatusTransition-part3.svg)

View: ResponseStatusTransition-part3

- `IResponseStalenessRule` (namespace `QBrainAi.Support.Mcp.AgentContext.Responses`, owner ResponsesAndTrust, called read-only by ContextPackages and ContextGraph): evaluates one `Submitted`, `Grading`, or `Trusted` response and returns fresh, or stale with the first changed reference and its relationship. Dependencies are the `depends_on`, `derived_from`, and `supports` targets of the response and of its package, the response's `File`, `Requirement`, `Receipt`, and `SessionLogTurn` evidence (including evidence outside its package), and every cited requirement (stale when deleted or superseded), except the request's own turn (never a dependency or evidence, section 6.3) and Rule files outside version control (provenance only, never a dependency, section 6.3). A file dependency is hashed at the workspace's current HEAD commit. A dependency that names a response (kind `PriorResponse`: a package item, or a `depends_on`, `derived_from`, or `supports` target of the response or of its package) must also be effectively `Trusted` and fresh, because its `ContentSha256` never changes when it is rejected, superseded, made stale, or demoted: the rule reads that response's stored status and evaluates it in turn, so a prior response that is `Rejected`, `Superseded`, `Stale`, `Submitted`, or `Grading` makes the dependent response stale with that reference, at any depth of the chain. One evaluation memoizes each response it has evaluated, and a reference back to a response already on the evaluation path adds no staleness of its own, so a cycle of client-made links terminates (`StalenessPriorResponseDependencyRejectedSupersededStaleOrDemotedDependentStale`, `StalenessPriorResponseThroughPackageItemDependentStale`, `StalenessDependencyCycleTerminatesEachResponseEvaluatedOnce`). Queries and Build report the effective status and write nothing; only EvaluateStalenessCommand persists `Stale`, through ResponseStatusTransition, after it evaluates outside its SerializableWrite. A unit that needs the persisted status (the grade units, and a unit of another context) dispatches EvaluateStalenessCommand through `IDispatcher` before its own SerializableWrite.

![IResponseStalenessRule.Evaluate (activity), part 1 of 3](c4/rendered/IResponseStalenessRule.svg)

View: IResponseStalenessRule

![IResponseStalenessRule.Evaluate (activity), part 2 of 3](c4/rendered/IResponseStalenessRule-part2.svg)

View: IResponseStalenessRule-part2

![IResponseStalenessRule.Evaluate (activity), part 3 of 3](c4/rendered/IResponseStalenessRule-part3.svg)

View: IResponseStalenessRule-part3

- `ResponseIdList` (namespace `QBrainAi.Support.Mcp.AgentContext.Responses`): validates a list of response ids (at most 1000, each with the `rsp-` prefix; otherwise 400 `validation_error`) and selects them; ids that name no response in the workspace are returned in the result's `notFound` list (EvaluateStalenessCommand, ProjectAgentResponsesCommand).
- `RelationshipWriter` (namespace `QBrainAi.Support.Mcp.AgentContext.Graph`, owner ContextGraph, called by ContextPackages and ResponsesAndTrust inside their own transactions): writes a typed link with its target's hash (`ToSha256`, which the caller read through SourceResolver before its SerializableWrite, so the delegate stays database-only) and `CreatedBy`. A `supersedes` link is written only by SupersedeAgentResponseCommand; a client request for that type is 400 `validation_error` with `details.field` `type`, so one data source holds the supersede chain. Identical links return the stored link (`created: false`, IdempotentCreate with policy `ReturnStored`). Every link created by a command (`CreatedBy` names the command, for example Build's `derived_from` and `relevant_to`, Submit's `derived_from` and `supports`, Supersede's `supersedes`) is protected: their deletion is 409 `agent_context_relationship_protected`.

![RelationshipWriter (activity), part 1 of 2](c4/rendered/RelationshipWriter.svg)

View: RelationshipWriter

![RelationshipWriter (activity), part 2 of 2](c4/rendered/RelationshipWriter-part2.svg)

View: RelationshipWriter-part2

#### 3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell)

The processes BindVerbToTurn and PersistTurnWriteWithReceipt (section 4.4) are the shared verb pipeline: every verb sequence shows them as `ref` blocks, and every process that sends a turn write (the failsafe drain, clear-session, the stateless invocation's `canceled` close, and the TypeScript hosts' tool-audit hook, section 3.2) maps its result through PersistTurnWriteWithReceipt. The functions below are named for what they do. Each lives in `plugins/core/lib-ps/repl-invoke.ps1`, the core's dot-sourceable library (its guard, `repl-invoke.ps1:3137`), so the verbs, the failsafe drain, and the arrival bridge reach them in-process; `plugin-hook.ps1` cannot be dot-sourced (mandatory `HookName` and a top-level `switch`, `plugin-hook.ps1:3-6,1521-1534`), so it dot-sources `repl-invoke.ps1` to call them.

- `Resolve-MintOutcome`: classifies one mint result for the prompt hook, the arrival bridge, and the failsafe drain alike.

![Resolve-MintOutcome (activity), part 1 of 4](c4/rendered/Resolve-MintOutcome.svg)

View: Resolve-MintOutcome

![Resolve-MintOutcome (activity), part 2 of 4](c4/rendered/Resolve-MintOutcome-part2.svg)

View: Resolve-MintOutcome-part2

![Resolve-MintOutcome (activity), part 3 of 4](c4/rendered/Resolve-MintOutcome-part3.svg)

View: Resolve-MintOutcome-part3

![Resolve-MintOutcome (activity), part 4 of 4](c4/rendered/Resolve-MintOutcome-part4.svg)

View: Resolve-MintOutcome-part4

- `Invoke-SessionOpen`: the idempotent session open for an envelope's `mcpSessionId`, used by StartHostSession, the first-ingress writer, and the failsafe drain. It opens the id its caller recorded and records nothing itself: StartHostSession and the first-ingress writer record a new `mcpSessionId` in their own `Update-TurnState` replace before the first open, and only when none is recorded for the host session key, so an outage before the open is confirmed never changes the identity, and every retry and drain opens the same id (`SessionStartOpenOutageRecordedSessionIdReusedByNextStartAndDrain`, `TsHostFirstIngressOpenOutageRecordedSessionIdReusedByDrainNoSecondId`). A recorded `mcpSessionId` for a host session key is never replaced: a later SessionStart, a post-compact start, or a resumed session key keeps it (FR-MCP-SESSIONLIFE-002).

![Invoke-SessionOpen (activity), part 1 of 2](c4/rendered/Invoke-SessionOpen.svg)

View: Invoke-SessionOpen

![Invoke-SessionOpen (activity), part 2 of 2](c4/rendered/Invoke-SessionOpen-part2.svg)

View: Invoke-SessionOpen-part2

- `Set-SessionUnsupported`: the D6 fail-closed rule for every check that can fail a host session (the bridge's format and removal checks, the prompt hook's unsupported branch, the copilot and classic-cline single-registration and equal-field checks).

![Set-SessionUnsupported (activity), part 1 of 2](c4/rendered/Set-SessionUnsupported.svg)

View: Set-SessionUnsupported

![Set-SessionUnsupported (activity), part 2 of 2](c4/rendered/Set-SessionUnsupported-part2.svg)

View: Set-SessionUnsupported-part2

- `Update-TurnState`: the one writer of `sessions/<hostSessionKey>/turn-state.yaml` for every writer section 3.4 lists: one atomic replace under `turn-open.lock`, released on every path. A function that runs inside a change already holding the lock (for example `Set-SessionUnsupported` or `Invoke-SessionOpen` called by a writer that holds it) records into that same replace and takes no second lock.

![Update-TurnState (activity), part 1 of 3](c4/rendered/Update-TurnState.svg)

View: Update-TurnState

![Update-TurnState (activity), part 2 of 3](c4/rendered/Update-TurnState-part2.svg)

View: Update-TurnState-part2

![Update-TurnState (activity), part 3 of 3](c4/rendered/Update-TurnState-part3.svg)

View: Update-TurnState-part3

- `Test-ModelEvidence` and `Get-CloseRemedy`: model evidence is at least one model-authored action, designDecision, or commit; it is required before a `completed` or `failed` close and never for a `canceled` close; records captured in the failsafe for the turn count. `Get-CloseRemedy` takes the turn and the chosen close (`completed`, `failed`, or `canceled`) and returns the remedy of section 3.4 (Stop gate) for that turn in its fixed order; the Stop gate, clear-session, and the advisory completion precheck call it.

- Alias journal (C2; the one reader and writer of `sessions/<hostSessionKey>/aliases.yaml` for the prompt hook, the arrival bridge, the failsafe drain, the verbs, and every registered consumer, QB-AGENT-001's outbox among them once its PowerShell form exists). Data model: `consumers` `{ consumerId, registeredAtUtc }`; per alias a permanent `lookup` entry `{ pendingId, requestId, aliasedAtUtc, acks, rootIds }` (the root-id envelope of section 3, never pruned) and a `delivery` entry `{ pendingId, requiredConsumers, acks }`, whose required set is the consumers registered when the alias is committed and which is pruned only when every member has acknowledged. Binding: every function takes the host session from the invocation binding (the immutable C4/C5 context that BindVerbToTurn resolves), never from a parameter, so one host session never reads or writes another's aliases. Atomicity: every write is one atomic replace of `aliases.yaml` (temp file, flush, rename) under the host session's `turn-open.lock`, recorded inside the caller's lock when the caller already holds it, as `Update-TurnState` does; a contended lock is returned to the caller, which keeps its records and retries. Errors: every refusal is a typed final failure (the structured cause of `classified-error.ps1`) and writes nothing. Functions:
  - `Add-McpAlias -PendingId -RequestId`: called when a mint returns a turn for a `pending-<nonce>` id; commits the `lookup` and `delivery` entries in one replace before any bound record is rewritten; a repeat of a committed pair returns the stored entry and writes nothing, so restart and replay reapply a journaled alias without minting again; a `pendingId` already aliased to another `requestId` is refused with `request_ambiguous`.
  - `Register-McpAliasConsumer -ConsumerId`: idempotent and persisted; a consumer registered after an alias's commit is not in that alias's required set and reads it through `Get-McpAlias`.
  - `Get-McpUnacknowledgedAlias -ConsumerId`: the `delivery` entries whose required set holds the consumer and whose `acks` lack it, oldest `aliasedAtUtc` first.
  - `Get-McpAlias -Id`: resolves a `pendingId` or a `requestId` to its `lookup` entry; `$null` when no entry matches; `request_ambiguous` when more than one does.
  - `Confirm-McpAliasAcknowledged -ConsumerId -PendingId`: idempotent; records the ack on both entries and prunes the `delivery` entry once every required consumer has acknowledged; an unknown `pendingId` is refused with `request_unknown`.
  The cases of this component run in one Pester file, `plugins/core/test-fixtures/pester/AliasJournal.Tests.ps1` (`AliasJournalResolveEitherIdSameTurn`, `AliasJournalReplayNoReMint`, `AliasJournalSamePendingIdOtherRequestIdRefusedRequestAmbiguousNothingWritten`, `AliasLateRegistrationNotRequiredForOlderAliases`, `AliasRequiredSetPersistedRestartKeepsPruningEligibility`, `AliasPartialAckNotPruned`, `AliasTwoHostSessionsIsolated`, `AliasLookupAfterAllAcksPruneAndRestartBothIdsResolveExactAcksRepeatAckNoop`); the processes that call it keep their own cases (`AliasJournalCrashBoundariesOneMintNoLostWrite`, `AliasRebindQueuedWritesOriginalOrderThroughPersistPath`).

![Add-McpAlias (activity), part 1 of 3](c4/rendered/Add-McpAlias.svg)

View: Add-McpAlias

![Add-McpAlias (activity), part 2 of 3](c4/rendered/Add-McpAlias-part2.svg)

View: Add-McpAlias-part2

![Add-McpAlias (activity), part 3 of 3](c4/rendered/Add-McpAlias-part3.svg)

View: Add-McpAlias-part3

## 4. Phase A: correct, independent turn logging

### 4.1 Components and data

- Server (handlers in `src/QBrainAi.Services/SessionLog/Commands/`, namespace `QBrainAi.Support.Mcp.SessionLog.Commands`; the new Phase A DTOs `SessionLogMintTurnRequest` and `SessionLogMintTurnResult` in `src/QBrainAi.Client/Models/SessionLog/`, namespace `QBrainAi.Client.Models.SessionLog`): `SessionLogService.SubmitWithOutcomesAsync` (per-turn outcomes), `ISessionLogService.MintTurnAsync`, `ITerminalTurnOutcomeGuard` (namespace `QBrainAi.Support.Mcp.SessionLog`: the seam that replaces the inline submit guard, named for the rule it applies; only SubmitSessionLogCommand applies it, never the upsert route), `SessionLogTurnEntity.IngressNonce` (unique filtered index on (WorkspaceId, SessionLogId, IngressNonce) where not null, and a second unique filtered index on (WorkspaceId, RequestId) where IngressNonce is not null, so minted request ids are unique across the workspace's sessions while legacy rows are untouched; TR-MCP-SESSIONTURN-004 AC-006).
- Shared components the units below call (section 3.5, each drawn once there): `TurnLocator`, `TurnWritePipeline`, and `TurnStatus` (3.5.3); `SerializableWrite`, `IdempotentCreate` with `PayloadFingerprint`, `DbErrorClassifier`, `ReadableKeyAllocator` (its strategy `RandomHex` is the request-id minter), and `ClassifiedErrorException` (3.5.2); `ReviewDefaults` (3.5.4, from Phase B).
- PowerShell plugin core (`plugins/core/lib-ps`): prompt hook, arrival bridge (claude-code, claude-cowork; its own script entry, which dot-sources `repl-invoke.ps1`), `Sync-PluginArrivalLog`, verb binding (`New-ReplInvocationBinding`, `Resolve-ReplTargetTurn`), truthful persist (`Get-ReplTurnWriteOutcome`, `Invoke-ReplPersistTurn`), failsafe and alias journal, Stop gate (`Get-PluginStopGateTurns`, pure `Get-PluginStopGateFindings`), and the shared functions of section 3.5.6 that the processes below call: `Resolve-MintOutcome`, `Invoke-SessionOpen`, `Set-SessionUnsupported`, `Update-TurnState`, `Test-ModelEvidence`, `Get-CloseRemedy`, and the alias journal (`Add-McpAlias`, `Register-McpAliasConsumer`, `Get-McpUnacknowledgedAlias`, `Get-McpAlias`, `Confirm-McpAliasAcknowledged`). These functions live in `plugins/core/lib-ps/repl-invoke.ps1`, the dot-sourceable library (section 3.5.6), so the verbs, the failsafe drain, and the arrival bridge call them in-process; `plugin-hook.ps1` cannot be dot-sourced (`plugin-hook.ps1:3-6,1521-1534` at `a8d3934b`) and dot-sources `repl-invoke.ps1` to call them.
- TypeScript-host bindings (cline, cline-v2, opencode) in the same PowerShell core, reached through each host's logic-free adapter, which forwards each host hook invocation the host binding table names (`{ input, output }`; on opencode's `event` hook, which fires for every host event including streaming updates, only an event of type `session.idle`, so no other event starts `pwsh`) to `lib/plugin-hook.ps1 -HostEvent` and applies the returned `output`, forwards each tool call (`{ params, hostContext }`) to `lib/Invoke-McpPlugin.ps1 -InvocationFromStdin`, and registers the tools listed in `lib/tool-descriptors.json` (operator, 2026-10-11: "You should be designing all scripts and script tools around PowerShell and only PowerShell."); `plugins/core/lib-node` receives no new work and is retired.
- Plugin per-host-session state (one directory per host session key under the plugin cache):
  - `turns/<requestId>.yaml` with its own lock `turns/<requestId>.lock`: one file per turn (from Phase C it also keeps the request's `packageId`; the session-log turn gains no such field). It also holds the turn's `lastBuildStatus`, which code-verify writes (below).
  - `sessions/<hostSessionKey>/turn-state.yaml`: `writeTarget { requestId, deliveredAtUtc, source }`, `openDelivered [requestId]`, `queue [{ kind, requestId, nonce, enqueueOffset, enqueueTimestamp, promptSha256, state, stateOffset, mintedAtUtc, storedStatus }]` (`kind` is `request`, `system`, or `open-failed`; `storedStatus` only on a request entry whose mint returned `merged` with a terminal status), `appliedThroughOffset`, `mcpSessionId`, `ledger [...]`, `unsupportedSince { causeOffset, causeNonce, or detectedAtUtc, causeOperation, reason }`, and from Phase C `agentContextMode` (`Off`, `Pilot`, or `Stateless`: the host session's Mode cache of the section 3.1 "Mode changes" resolution, read as `Off` when absent; its writers are listed in section 3.4); every entry carries the root ids; written only through `Update-TurnState` (one atomic replace under `turn-open.lock`, section 3.5.6).
  - `sessions/<hostSessionKey>/ended.yaml` (`reason: session_end` or `idle`), `sessions/<hostSessionKey>/aliases.yaml` (the alias journal component of section 3.5.6), `sessions/<key>/arrival-bridge.pid`.
  - The failsafe queue keeps develop's location and quarantine (`Get-McpFailsafeDir` and `Get-McpFailsafeQuarantineDir`, `resolve-cache-dir.ps1:306,350` at `a8d3934b`; the quarantine subdirectory with a sibling `.reason.txt`, `Move-ReplFailsafeToQuarantine`, `repl-invoke.ps1:966`); the quarantine is the one place for a record the drain cannot replay (process DrainFailsafe).

The workspace-level `current-turn.yaml` becomes a read-only legacy file (this replaces TR-MCP-REPL-014's and TEST-MCP-REPL-029's `current-turn.yaml` title targeting, TR-MCP-REPL-020's cache fallback for `failTurn`, and FR-MCP-171's and TR-MCP-PERSIST-002's "`current-turn.yaml` remains in_progress", whose retention moves to the bound turn's failsafe record and `turns/<requestId>.yaml`). Its fate, once:

- Writers: after adoption no plugin code writes it. Code-verify (`Invoke-CodeVerify`, `plugin-hook.ps1:1199` at `a8d3934b`, today writing `lastBuildStatus` into it at `:1262`, read by the Stop gate at `:1034`) writes `lastBuildStatus` into the write target's `turns/<requestId>.yaml` under its lock instead, finding the host session from the hook payload's session key as a verb does; a payload without the key writes nothing (section 3.1). The Stop gate's failed-build block reads it from this host session's turn files (process EvaluateStopGate).
- Readers that stop reading it: every verb (bound by process BindVerbToTurn), the prompt hook's open (`Open-PluginTurn`, `plugin-hook.ps1:753-937`, replaced by process OpenTurnFromPromptHook), `Assert-ReplCurrentTurnFresh` (`repl-invoke.ps1:1913-2014`, which keeps only its marker-drift check), and the Stop gate's targeting.
- Readers that keep reading it, only to report a turn still in progress there: process StartHostSession (listed with its pair, addressable by explicit `requestId`), the Stop gate (a warning, never a block), and plugin Status (`mcp-status.ps1:14`). Build's open-turn guard `HasOpenPluginTurn` (`build/Build.SyncAgentPlugins.cs:696-726`) reads `openDelivered` of every `turn-state.yaml` as well as the legacy file, so `ReplacePluginCache_OpenTurn_RetainsExistingCache` (`build/Build.Tests/BuildTargetTests.cs:569` on the analyzer branch, `tests/Build.Tests` at `a8d3934b`; an infrastructure test) keeps its outcome with a per-host-session seed.
- Verbs: `workflow.sessionlog.beginTurn` (`Invoke-WorkflowBeginTurn`, `repl-invoke.ps1:2161`, dispatched at `:3047`) is retired from the plugin: no tool descriptor names it, and a call is refused with `mint_requires_host_ingress`, because only a host arrival mechanism opens a turn (MintSessionTurnCommand). The plugin's `workflow.sessionlog.openSession` (`Invoke-WorkflowOpenSession`, `repl-invoke.ps1:243-293`, which today writes only the workspace `session-state.yaml`) resolves its host session as process BindVerbToTurn does and opens that host session's recorded `mcpSessionId` through `Invoke-SessionOpen` (OpenSessionLogCommand); it writes no `session-state.yaml`.
- Tests: the PluginIntegration canonical turn (`tests/QBrainAi.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs:121-196`: bootstrap, `openSession`, `beginTurn` with a client-chosen `requestId`, the verbs, `completeTurn`, no host session key) is rewritten to the host path: a SessionStart payload carrying the host row's session key (process StartHostSession), the turn's arrival (on claude-code and claude-cowork a recorded transcript fixture whose `enqueue` and `dequeue` the arrival bridge applies, process ApplyArrivalBridgeRecord, because their prompt hook neither mints nor claims; on every other host a prompt hook payload carrying its nonce, process OpenTurnFromPromptHook; server-minted `requestId` either way), the verbs with that key in their invocation context, and `completeTurn` after a model-authored action (replaces FR-MCP-PLUGININT-001 ac1 and ac6, "begins a turn"). TEST-MCP-BUGTRIAGE-068 (`PluginPowerShellRuntime.Tests.ps1:2512`, a codex `stop-gate` run with no payload and a `current-turn.yaml` seed) is rewritten: its Stop payload carries the codex session key and its seed is the turn file's `lastBuildStatus`; it keeps asserting the failed-build block. The core Pester cases that seed `current-turn.yaml` and pass no host session key (about 120 references in 7 files, recon `code-plugin.md`) are re-seeded through `turn-state.yaml` and the per-turn files.

Request lifecycle (plugin queue entry; the state `open-failed` is the queue kind of the same name):

![Request lifecycle in the plugin turn store (state diagram), part 1 of 6](c4/rendered/STR-04-01.svg)

View: STR-04-01

![Request lifecycle in the plugin turn store (state diagram), part 2 of 6](c4/rendered/STR-04-01-part2.svg)

View: STR-04-01-part2

![Request lifecycle in the plugin turn store (state diagram), part 3 of 6](c4/rendered/STR-04-01-part3.svg)

View: STR-04-01-part3

![Request lifecycle in the plugin turn store (state diagram), part 4 of 6](c4/rendered/STR-04-01-part4.svg)

View: STR-04-01-part4

![Request lifecycle in the plugin turn store (state diagram), part 5 of 6](c4/rendered/STR-04-01-part5.svg)

View: STR-04-01-part5

![Request lifecycle in the plugin turn store (state diagram), part 6 of 6](c4/rendered/STR-04-01-part6.svg)

View: STR-04-01-part6

### 4.2 Units of work (CQRS handlers)

Each unit of work is exactly one CQRS command or query with its handler (operator, 2026-10-11: "I want to be clear, unit of work maps to one CQRS handler"), following `QBrainAi.Cqrs`: a `sealed record` implementing `ICommand<TResult>` or `IQuery<TResult>` and its handler, each in its own file (MA0048) under `src/QBrainAi.Services/SessionLog/Commands` (namespace `QBrainAi.Support.Mcp.SessionLog.Commands`, the RootNamespace and folder path, section 3.5), registered by `AddCqrsHandlers`, returning `Result<TResult>`; controllers and MCP tools dispatch through the scoped `IDispatcher.SendAsync` or `QueryAsync` and map failures through `McpErrorClassifier`. Phase A and B handlers delegate to the `ISessionLogService` members named in each unit below. After the dispatch and before every handler, WorkspaceResolutionBehavior resolves `WorkspacePath` (section 3.5.1): a path that names no registered workspace is 400 `validation_error` with `details.field` `workspacePath`, the status TR-MCP-MT-002 gives REST; on REST `WorkspaceResolutionMiddleware` answers a missing or unknown `X-Workspace-Path` earlier with today's body. That 400 belongs to every unit's error list below and is not repeated there, and each diagram below starts inside its handler. A refusal this plan adds is a `ClassifiedErrorException` (section 3.5.2); a unit that keeps a develop body names that body in its error list.

Storage rules every Phase A unit follows (section 3.5.2):

- Database-only delegates: no Phase A unit reads git, files, or sources. The turn context and workspace attribution checks are path string checks against the workspace root (`SessionLogWorkspaceAttributionValidator.cs:56-105` at `a8d3934b`), so each unit runs every check inside its SerializableWrite delegate and reads nothing before it.
- Isolation (section 3.1, "Turn-level isolation"): the mint and the single-turn writes (upsert, patch, replace, complete, fail) read their session row only to locate the parent and write only the one turn with its children (section 3.5.2, isolation scope), so writes to different turns of one session never answer a conflict (`TurnWriteConcurrentDifferentTurnsSameSessionBothCommitNoConflictAnswer`, dense-session fixture, three providers); the session summary refresh after a single-turn write is its own short write after the turn commits, and its failure never fails the turn write. SubmitSessionLogCommand keeps its session-level Serializable transaction, as `SessionLogService.SubmitAsync` does today (`SessionLogService.cs:155-157`). The mint inserts one turn and is a single-turn write like the others (section 3.1, "Mint isolation").
- Retry bound: SerializableWrite runs a delegate at most four attempts, then answers 409 `conflict` with `retryable: true` and saves nothing; the attempts run within the unit's storage budget (FR-MCP-SESSIONLIFE-003). Only the minter states its own bound, for one cause, a collision of its random request id (six attempts, then 500 `mint_collision_exhausted`); its transient conflicts keep the default bound and the 409 `conflict` exit (section 3.1, "Retry bound").
- Providers: on the EF in-memory provider SerializableWrite runs the delegate once with no transaction and no retry, as develop opens its transaction only on a relational provider (`SessionLogService.cs:155`), so the 151 tests in 15 files that run the real `SessionLogService` on `UseInMemoryDatabase` keep their single run (recon `code-server.md`). A caller-owned EF or `System.Transactions` transaction is refused before any write, as develop's `SubmitAsync` refuses it (`SessionLogService.cs:119-128`); `SessionLogSupersessionTests.SubmitAsyncCallerTransactionRejectsBeforeMutation`, `SubmitAsyncAmbientTransactionRejectsBeforeMutation`, and `SubmitAsyncPendingTrackedEditRejectsWithoutDiscardingEdit` (`SessionLogSupersessionTests.cs:192,204,178` at `a8d3934b`, renamed on the analyzer branch) and `SessionLogControllerReceiptTests.SubmitAsyncOuterRollbackNeverReturnsDurableReceipt` (`SessionLogControllerReceiptTests.cs:106`) keep their outcome.
- Deadlock and busy (section 3.1, "Deadlock and busy outcomes"): a transient conflict inside the delegate (SQL Server 1205, PostgreSQL 40001 and 40P01, SQLite 5 and 6, `DbErrorClassifier`) reruns the delegate; at the bound the unit answers 409 `conflict` with `retryable: true`, and the plugin handles that answer as an outage (failsafe record kept, receipt `queued`). A busy store outside SerializableWrite (the dialog append and the two title writes, which keep today's single save) keeps today's 503 `persistence_error` with `retryable: true` (`McpErrorClassifier.cs:129-139` at `a8d3934b`), which the plugin also handles as an outage, and unreachable storage keeps its answer (TEST-MCP-TRIAGESTORE-007 is unchanged). The failsafe outcome of TEST-MCP-TRIAGESTORE-005, TR-MCP-TRIAGEERR-001, and FR-MCP-SESSIONLIFE-003 ac6 holds while their status code changes from 503 to 409 (records to update). Two develop tests change their expected outcome: `SessionLogDenseGraphTests.UpsertTurnAsyncSqlDeadlock1205DoesNotReportMissingSession` (`SessionLogDenseGraphTests.cs:100` at `a8d3934b`; its interceptor fails every reader with 1205, `:286-301`) expects the 409 `conflict` `ClassifiedErrorException` with `retryable: true` after four attempts instead of the 503 `backend_unavailable` `StorageGraphMaterializationException`, with the session not reported missing and no turn saved; `SessionLogSupersessionTests.SubmitAsyncConcurrentCompletionPreservesCompletedResponse` (`SessionLogSupersessionTests.cs:216`) expects a completion error, when one occurs, to be that 409 instead of a raw `SqliteException` 5 or 6 (`:251-254`), and its `explicitTurnWriter` row now runs the upsert in its own turn-level SerializableWrite; both keep the preserved completed turn.

#### MintSessionTurnCommand: mint a turn at ingress [FR-MCP-SESSIONTURN-001]

- Handler: `MintSessionTurnCommand(string WorkspacePath, string Agent, string SessionId, SessionLogMintTurnRequest Request) : ICommand<SessionLogMintTurnResult>` in `src/QBrainAi.Services/SessionLog/Commands/MintSessionTurnCommand.cs` and `MintSessionTurnCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/MintSessionTurnCommandHandler.cs`, delegating to `ISessionLogService.MintTurnAsync`.
- Trigger: a user message arrives at a host arrival mechanism (prompt hook, arrival bridge, or host-native adapter). Only hooks and adapters call it; there is no MCP tool, and the plugin wrapper refuses a model-originated call with `mint_requires_host_ingress`.
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/mint` (alias `/mcpserver/...`), client `MintTurnAsync`, REPL `workflow.sessionlog.mintTurn`.
- Request `SessionLogMintTurnRequest`: `ingressNonce` (string, required: the host's per-message id or a SHA-256 of host event fields), `queryTitle`, `queryText`, `model`, `planFile` (value or the `None` sentinel), `todoId` (value or `None`).
- Response `SessionLogMintTurnResult`: `requestId` (`req-<yyyyMMddTHHmmssZ>-<8 hex>`), `outcome` (`created` or `merged`), and `storedStatus` on `merged` only (TR-MCP-SESSIONTURN-004 AC-005).
  - 201 `{ requestId, outcome: "created" }`: a new in-progress turn.
  - 200 `{ requestId, outcome: "merged", storedStatus }`: the same nonce was already minted in this MCP session; the same `requestId` is returned and a terminal turn is not reopened.
- Idempotency: the nonce lookup and the insert are IdempotentCreate keyed by (session, ingressNonce) with policy `ReturnStored`, inside SerializableWrite (section 3.5.2). The mint is one host event, so a repeated nonce returns the stored turn whatever the payload; no payload fingerprint is compared, because the nonce already identifies the event (the host's per-message id, or a SHA-256 of the event fields, section 3.2). A concurrent mint of the same nonce loses on the nonce index, and the rerun lookup returns the winner as `merged`. Request-id collisions use the minter's own bound (six attempts in all, then 500); transient conflicts use SerializableWrite's default bound (four attempts, then 409). The mint writes one turn under turn-level isolation (storage rules above).
- Errors: 400 `ingress_nonce_invalid` (missing or invalid nonce, TR-MCP-SESSIONTURN-004 AC-005), 400 `validation_error` (an invalid session id, TurnLocator `InvalidIdentifier`, or an invalid `planFile` or `todoId`), 404 with detail code `session_not_open` (TurnLocator `SessionNotFound`), 500 with final detail code `mint_collision_exhausted` after six request-id attempts, 409 `conflict` with `retryable: true` after SerializableWrite's four attempts (nothing saved; a retryable answer, so the plugin keeps its failsafe mint record for the drain as for an outage, section 3.1).

![MintSessionTurnCommand: mint a turn at ingress (activity), part 1 of 4](c4/rendered/MintSessionTurnCommand.svg)

View: MintSessionTurnCommand

![MintSessionTurnCommand: mint a turn at ingress (activity), part 2 of 4](c4/rendered/MintSessionTurnCommand-part2.svg)

View: MintSessionTurnCommand-part2

![MintSessionTurnCommand: mint a turn at ingress (activity), part 3 of 4](c4/rendered/MintSessionTurnCommand-part3.svg)

View: MintSessionTurnCommand-part3

![MintSessionTurnCommand: mint a turn at ingress (activity), part 4 of 4](c4/rendered/MintSessionTurnCommand-part4.svg)

View: MintSessionTurnCommand-part4

##### Nested activity: InsertMintedTurnWithRetry (insert step of MintSessionTurnCommand, rerun by SerializableWrite)

![Nested activity: InsertMintedTurnWithRetry (insert step of MintSessionTurnCommand, rerun by SerializableWrite) (activity), part 1 of 3](c4/rendered/InsertMintedTurnWithRetry.svg)

View: InsertMintedTurnWithRetry

![Nested activity: InsertMintedTurnWithRetry (insert step of MintSessionTurnCommand, rerun by SerializableWrite) (activity), part 2 of 3](c4/rendered/InsertMintedTurnWithRetry-part2.svg)

View: InsertMintedTurnWithRetry-part2

![Nested activity: InsertMintedTurnWithRetry (insert step of MintSessionTurnCommand, rerun by SerializableWrite) (activity), part 3 of 3](c4/rendered/InsertMintedTurnWithRetry-part3.svg)

View: InsertMintedTurnWithRetry-part3

#### SubmitSessionLogCommand: submit a session log with per-turn outcomes [FR-MCP-SESSIONTURN-004]

- Handler: `SubmitSessionLogCommand(string WorkspacePath, UnifiedSessionLogDto Log) : ICommand<SessionLogSubmitResult>` in `src/QBrainAi.Services/SessionLog/Commands/SubmitSessionLogCommand.cs` and `SubmitSessionLogCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/SubmitSessionLogCommandHandler.cs`, delegating to `ISessionLogService.SubmitWithOutcomesAsync`, which upserts the session row from the document and runs TurnWritePipeline in mode `Submit` for each turn, all inside one SerializableWrite at session level (section 3.5). Per turn the submit mode runs TurnLocator (session and request ids, as develop's `SubmitAsync` validates them, `SessionLogService.cs:138-148`), policy TerminalTurnOutcomeGuard, the turn context and workspace attribution checks (`:186-196`), the insert or additive merge, policy RequestClassification when the turn carries `requestClass`, and policy CompletionGate when the write moves the turn to `completed` or `failed` (from S10); it does not run the QBAgent terminal compliance check, which develop runs only on the single-turn paths (`:756`, `:792`, `:877`) and which only the single-turn modes run.
- Surfaces: `POST /qbrainai/sessionlog` (existing route), MCP `sessionlog_submit`, client `SubmitAsync`, REPL submit. This is the route every turn verb already uses: the plugin verbs `appendActions`, `appendDecisions`, `updateTurn`, `completeTurn`, and `failTurn` send the one bound turn in a session document (`Invoke-ReplPersistTurn`, `plugins/core/lib-ps/repl-invoke.ps1:1602-1736`, method `client.SessionLog.SubmitAsync`; process PersistTurnWriteWithReceipt), and so do the REPL workflow (`src/QBrainAi.Repl.Core/SessionLogWorkflow.cs`) and McpAgent (`src/QBrainAi.McpAgent/SessionLog/SessionLogWorkflow.cs`). Dialog and titles use their own routes (AppendTurnDialogCommand, SetTurnTitleCommand, SetSessionTitleCommand).
- Request: the unified session-log document (`UnifiedSessionLogDto`) with its turns (from Phase B a turn may carry `requestClass` and the review records, section 3.4); a supplied turn status is one an agent surface may write (`TurnStatus`: `in_progress`, `completed`, `failed`, `canceled`, or the legacy `cancelled`; `closed` and every other value are refused, section 3.1).
- Response 201 `SessionLogSubmitResult`: existing fields plus `turns: [{ requestId, outcome, storedStatus, reason }]`, where `outcome` is `created`, `merged`, or `skipped_terminal`. The status code stays 201. MCP `sessionlog_submit(json, workspacePath)` returns `{ success, id, turns }` (today `{ success, id }`; A1 adds `turns`), `{ error: "Invalid JSON" }` for a document that deserializes to null, and the classified `{ code, error, message, retryable, details }` for every other failure (`FwhMcpTools.SessionLog.cs:22-42`).
- Rules: an identical resubmit is `merged` with reason `no_change`; duplicate request ids in one submission apply last-wins with one outcome each; the session row is created when absent and a tombstoned session is revived, as today (`SessionLogService.cs:158-169`, `:200-225`), and a concurrent first submission of the same session loses on the session key and is merged by SerializableWrite's rerun (IdempotentCreate, policy `MergeInto`), which replaces develop's separate retry on the text "UNIQUE constraint failed" (`:234`); a write whose status would move a terminal turn (the stored status is terminal by `TurnStatus.IsTerminal`, that is `completed`, `failed`, `closed`, `canceled`, or `cancelled`, develop's `IsTerminalTurnStatus`; the incoming status is supplied and differs from it; and either the incoming status is not terminal or the write is a superseded hook persist; today's guard at `SessionLogService.cs:1611-1618`, which the seam `ITerminalTurnOutcomeGuard` replaces unchanged, TR-MCP-SESSIONTURN-004 AC-002) is `skipped_terminal` and changes nothing, while a write that omits the status merges additively even on a terminal turn (omitted-status enrichment, FR-MCP-SESSIONTURN-004-AC003); there is no `skipped_duplicate` outcome. TurnWritePipeline returns the completion gate's missing items for each turn instead of rejecting; this unit collects them and rejects the submission once, after every turn is evaluated (FR-MCP-SESSIONCLASS-002-AC005). A submission inside a caller-owned transaction is refused before any write, as today (storage rules above).
- Errors: 400 `validation_error` (a document that does not parse, an invalid session or request id, an invalid `planFile` or `todoId`, a filesModified path or commit not attributed to the workspace, or a status an agent surface may not write, `details.field` `status`); 400 `request_class_invalid` (from S4); for a review record a turn carries, the review codes of section 5.2 (400) and 422 `review_status_conflict` (from S4); 400 `validation_error` with the token `session_turn_completion_gate` (from S10: one or more turns of the submission move to `completed` or `failed` without meeting the gate; every turn is evaluated first, then the whole submission is rejected atomically and the error names each failing `requestId` with its missing items, FR-MCP-SESSIONCLASS-002-AC005); 409 `conflict` with `retryable: true` after SerializableWrite's four attempts of the whole submission (section 3.5.2), nothing saved. Every refusal inside the delegate rolls the transaction back, so nothing is saved.

![SubmitSessionLogCommand: submit a session log with per-turn outcomes (activity), part 1 of 5](c4/rendered/SubmitSessionLogCommand.svg)

View: SubmitSessionLogCommand

![SubmitSessionLogCommand: submit a session log with per-turn outcomes (activity), part 2 of 5](c4/rendered/SubmitSessionLogCommand-part2.svg)

View: SubmitSessionLogCommand-part2

![SubmitSessionLogCommand: submit a session log with per-turn outcomes (activity), part 3 of 5](c4/rendered/SubmitSessionLogCommand-part3.svg)

View: SubmitSessionLogCommand-part3

![SubmitSessionLogCommand: submit a session log with per-turn outcomes (activity), part 4 of 5](c4/rendered/SubmitSessionLogCommand-part4.svg)

View: SubmitSessionLogCommand-part4

![SubmitSessionLogCommand: submit a session log with per-turn outcomes (activity), part 5 of 5](c4/rendered/SubmitSessionLogCommand-part5.svg)

View: SubmitSessionLogCommand-part5

##### Nested activity: EvaluateSubmittedTurn (loop body of SubmitSessionLogCommand)

![Nested activity: EvaluateSubmittedTurn (loop body of SubmitSessionLogCommand) (activity), part 1 of 4](c4/rendered/EvaluateSubmittedTurn.svg)

View: EvaluateSubmittedTurn

![Nested activity: EvaluateSubmittedTurn (loop body of SubmitSessionLogCommand) (activity), part 2 of 4](c4/rendered/EvaluateSubmittedTurn-part2.svg)

View: EvaluateSubmittedTurn-part2

![Nested activity: EvaluateSubmittedTurn (loop body of SubmitSessionLogCommand) (activity), part 3 of 4](c4/rendered/EvaluateSubmittedTurn-part3.svg)

View: EvaluateSubmittedTurn-part3

![Nested activity: EvaluateSubmittedTurn (loop body of SubmitSessionLogCommand) (activity), part 4 of 4](c4/rendered/EvaluateSubmittedTurn-part4.svg)

View: EvaluateSubmittedTurn-part4

#### UpsertSessionTurnCommand: append or merge one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-006, FR-MCP-SESSIONTURN-007]

The single-turn routes (this unit, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, FailSessionTurnCommand) keep today's request bodies, response bodies, and errors (`SessionLogController.cs:191-220` and `:425-566`, `SessionLogService.cs:716-889`). They are the explicit route for deliberate status corrections (the comment on the submit guard, `SessionLogService.cs:1610`), so policy TerminalTurnOutcomeGuard does not apply to them and they report no per-turn outcome. Each runs TurnWritePipeline (section 3.5.3) inside SerializableWrite in its own mode: Upsert and Patch `Merge`, Replace `Replace`, Complete `Complete`, Fail `Fail`. The pipeline rejects at the first refusal and, in these modes only, also runs the existing QBAgent terminal compliance check (`ValidateTerminalTurnCompliance`, `SessionLogService.cs:1329`). A turn inserted when absent gets the review defaults of ReviewDefaults (from Phase B). The insert-when-absent race is the pipeline's IdempotentCreate with policy `MergeInto`: an insert that loses to a concurrent insert of the same requestId ends the attempt, SerializableWrite reruns the unit, and the lookup finds the winning row and merges into it. That is the only retry on this save, for all five units alike (it replaces the one bounded retry of FR-MCP-SESSIONLIFE-005 ac5 and TR-MCP-SESSIONLIFE-005 ac4), and DbErrorClassifier replaces develop's message-text race predicate (`SessionLogService.cs:764-797`, `:812-817`). Each of the five writes isolates its own turn (turn-level isolation, storage rules above). Each unit maps the pipeline's typed failures to its surface's body, listed in its error list. This plan adds to these routes only:

- the TurnStatus rule: a write naming a status other than `in_progress`, `completed`, `failed`, `canceled`, or the legacy `cancelled` (`closed` included) is refused with 400 `validation_error`, `details.field` `status` (section 3.1); Complete and Fail take their status from the route. QBAgent's internal tools `mcp_session_turn_update` and `mcp_session_turn_complete` are agent surfaces that call `ISessionLogService.UpsertTurnAsync` directly, without a dispatch (`QuadBrainInternalToolExecutor.cs:299-328` at `a8d3934b`; the update tool deserializes its arguments, status included, as `UnifiedRequestEntryDto`, `:308`), so `UpsertTurnAsync` applies the same check for them before TurnWritePipeline, as it applies the completion gate to them (section 5.3, policy CompletionGate), and their `closed` is refused with the same 400; the exempt server-internal origins keep `closed` (section 3.5.3);
- the Phase B policies: RequestClassification when a write carries `requestClass` (S4), and CompletionGate when a write moves the turn into `completed` or `failed` (S10, FR-MCP-SESSIONCLASS-002-AC003, AC004); a Replace whose replacement carries `completed` or `failed` is gated even when the stored status already equals it, because a whole replacement is not additive enrichment (FR-MCP-SESSIONCLASS-002-AC004: "or a PUT carrying a terminal status"), and `canceled` is never gated;
- the review rules of section 5.2 for a review record the body carries (Phase B turn shape, section 3.4): the review codes (400) and 422 `review_status_conflict`, typed failure `ReviewInvalid`;
- the SerializableWrite conflict: 409 `conflict` with `retryable: true` after four attempts, nothing saved (section 3.5.2).

These added refusals reach the routes through `McpErrorClassifier` (S5, TR-MCP-SESSIONCLASS-002 AC-003) as `ClassifiedError` bodies; every other error keeps today's body. The five diagrams below share one shape: the pipeline's result is written, the SerializableWrite conflict, or a typed failure; a typed failure is an identifier or payload failure, a missing session (and for Replace a missing turn), or a Phase B policy refusal (`RequestClassInvalid`, `ReviewInvalid`, `CompletionGateMissing`), and every refusal saves nothing.

- Handler: `UpsertSessionTurnCommand(string WorkspacePath, string Agent, string SessionId, UnifiedRequestEntryDto Turn) : ICommand<long>` in `src/QBrainAi.Services/SessionLog/Commands/UpsertSessionTurnCommand.cs` and `UpsertSessionTurnCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/UpsertSessionTurnCommandHandler.cs`, delegating to `ISessionLogService.UpsertTurnAsync` (`SessionLogService.cs:716-806`), which runs TurnWritePipeline in mode `Merge` and returns the turn id; the controller composes today's body.
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/turn` (alias `/mcpserver/...`), client `UpsertTurnAsync(agent, sessionId, UnifiedRequestEntryDto)` returning `SessionLogTurnSubmitResult`, REPL `client.SessionLog.UpsertTurnAsync`; no MCP tool (MCP callers write turns through `sessionlog_submit`, SubmitSessionLogCommand). The plugin, REPL-workflow, and McpAgent verbs do not use this route; they persist through SubmitSessionLogCommand.
- Request: `UnifiedRequestEntryDto` (the turn with its `requestId`; from Phase B also `requestClass` and the review records). A new turn is inserted with its turn context (planFile and todoId validated as for a new entry); an existing turn is merged additively (omitted fields never clobber stored values), whatever its status.
- Response 201 `{ turnId, agent, sessionId, requestId }` (Location `/qbrainai/sessionlog/{agent}/{sessionId}`). A first insert that loses the race to a concurrent insert of the same requestId is merged into the winning row (IdempotentCreate, policy `MergeInto`).
- Errors, by pipeline failure: no body, 400 problem "Invalid turn body."; `InvalidIdentifier` or `InvalidPayload` (invalid session or request id, invalid planFile or todoId, a filesModified path or commit not attributed to the workspace (FR-MCP-SESSIONATTR-001), or the existing QBAgent-only terminal compliance check `ValidateTerminalTurnCompliance`), 400 problem "Invalid turn payload."; `SessionNotFound`, 404 `not_found` (`ClassifiedError`); a status an agent surface may not write, 400 `validation_error` with `details.field` `status`; from Phase B, `RequestClassInvalid` 400 `request_class_invalid`, `ReviewInvalid` the review codes (400) or 422 `review_status_conflict`, and `CompletionGateMissing` the completion-gate refusal (400 `validation_error` with the token `session_turn_completion_gate` and the missing items), nothing saved; SerializableWrite's four attempts used, 409 `conflict` with `retryable: true`, nothing saved.

![UpsertSessionTurnCommand: append or merge one turn (activity), part 1 of 5](c4/rendered/UpsertSessionTurnCommand.svg)

View: UpsertSessionTurnCommand

![UpsertSessionTurnCommand: append or merge one turn (activity), part 2 of 5](c4/rendered/UpsertSessionTurnCommand-part2.svg)

View: UpsertSessionTurnCommand-part2

![UpsertSessionTurnCommand: append or merge one turn (activity), part 3 of 5](c4/rendered/UpsertSessionTurnCommand-part3.svg)

View: UpsertSessionTurnCommand-part3

![UpsertSessionTurnCommand: append or merge one turn (activity), part 4 of 5](c4/rendered/UpsertSessionTurnCommand-part4.svg)

View: UpsertSessionTurnCommand-part4

![UpsertSessionTurnCommand: append or merge one turn (activity), part 5 of 5](c4/rendered/UpsertSessionTurnCommand-part5.svg)

View: UpsertSessionTurnCommand-part5

#### PatchSessionTurnCommand: change named fields of one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-007]

- Handler: `PatchSessionTurnCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, UnifiedRequestEntryDto Turn) : ICommand<long>` in `src/QBrainAi.Services/SessionLog/Commands/PatchSessionTurnCommand.cs` and `PatchSessionTurnCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/PatchSessionTurnCommandHandler.cs`, delegating to `ISessionLogService.UpsertTurnAsync` with the route `requestId` (TurnWritePipeline mode `Merge`; `SessionLogController.cs` PATCH route, the additive verb of FR-SUPPORT-010G).
- Surfaces: `PATCH /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}`, client `PatchTurnAsync` returning `SessionLogTurnSubmitResult`, REPL `client.SessionLog.PatchTurnAsync`; no MCP tool.
- Request: `UnifiedRequestEntryDto` holding only the fields to change, merged additively (omitted fields never clobber stored values; a missing body is an empty turn; the route `requestId` replaces any body value). As today, a `requestId` the session does not hold is inserted as a new turn.
- Response 200 `{ turnId, agent, sessionId, requestId, status }` (`status` echoes the body's status, null when omitted). Errors as UpsertSessionTurnCommand (no "Invalid turn body." case).

![PatchSessionTurnCommand: change named fields of one turn (activity), part 1 of 5](c4/rendered/PatchSessionTurnCommand.svg)

View: PatchSessionTurnCommand

![PatchSessionTurnCommand: change named fields of one turn (activity), part 2 of 5](c4/rendered/PatchSessionTurnCommand-part2.svg)

View: PatchSessionTurnCommand-part2

![PatchSessionTurnCommand: change named fields of one turn (activity), part 3 of 5](c4/rendered/PatchSessionTurnCommand-part3.svg)

View: PatchSessionTurnCommand-part3

![PatchSessionTurnCommand: change named fields of one turn (activity), part 4 of 5](c4/rendered/PatchSessionTurnCommand-part4.svg)

View: PatchSessionTurnCommand-part4

![PatchSessionTurnCommand: change named fields of one turn (activity), part 5 of 5](c4/rendered/PatchSessionTurnCommand-part5.svg)

View: PatchSessionTurnCommand-part5

#### ReplaceSessionTurnCommand: replace one turn [FR-MCP-SESSIONTURN-004, FR-SUPPORT-016]

- Handler: `ReplaceSessionTurnCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, UnifiedRequestEntryDto Turn) : ICommand<long>` in `src/QBrainAi.Services/SessionLog/Commands/ReplaceSessionTurnCommand.cs` and `ReplaceSessionTurnCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/ReplaceSessionTurnCommandHandler.cs`, delegating to `ISessionLogService.ReplaceTurnAsync` (`SessionLogService.cs:839-889`), which runs TurnWritePipeline in mode `Replace`.
- Surfaces: `PUT /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}`, MCP `sessionlog_replace_turn` (returns `{ success, turnId, agent, sessionId, requestId, replaced: true }`), client `ReplaceTurnAsync` returning `SessionLogMutationResult`, REPL `client.SessionLog.ReplaceTurnAsync`.
- Request: the whole turn as `UnifiedRequestEntryDto` (a missing body is an empty turn; the route `requestId` replaces any body value). Omitted scalars reset and every section becomes exactly what the body carries; a superseded hook persist gets the `None` planFile and todoId sentinels when it carries none, as today (this depends only on the body, so it runs before the pipeline).
- Response 200 `{ turnId, agent, sessionId, requestId, replaced: true }`. Errors (all through `ClassifiedError`, as today): `InvalidIdentifier` or `InvalidPayload` 400 `validation_error` (invalid session or request id, invalid planFile or todoId, a filesModified path or commit not attributed to the workspace, the QBAgent-only terminal compliance check), and 400 `validation_error` with `details.field` `status` for a status an agent surface may not write; `SessionNotFound` and `TurnNotFound` 404 `not_found` (missing session or turn); from Phase B, `RequestClassInvalid` 400 `request_class_invalid`, `ReviewInvalid` the review codes (400) or 422 `review_status_conflict`, and `CompletionGateMissing` the completion-gate refusal when the replacement carries `completed` or `failed`; SerializableWrite's four attempts used, 409 `conflict` with `retryable: true`, nothing saved. The pipeline in mode `Replace` requires the turn to exist, replaces it whole, and is gated whenever the replacement carries `completed` or `failed`.
- Diagram: the call node runs TurnWritePipeline in mode `Replace` inside SerializableWrite (section 3.5.3); the body step before it (the route `requestId` and the `None` sentinels of a superseded hook persist) is the nested activity PrepareReplacementTurn below; `SessionNotFound` and `TurnNotFound` share one 404 `not_found` terminal because they share its status and body.

![ReplaceSessionTurnCommand: replace one turn (activity), part 1 of 5](c4/rendered/ReplaceSessionTurnCommand.svg)

View: ReplaceSessionTurnCommand

![ReplaceSessionTurnCommand: replace one turn (activity), part 2 of 5](c4/rendered/ReplaceSessionTurnCommand-part2.svg)

View: ReplaceSessionTurnCommand-part2

![ReplaceSessionTurnCommand: replace one turn (activity), part 3 of 5](c4/rendered/ReplaceSessionTurnCommand-part3.svg)

View: ReplaceSessionTurnCommand-part3

![ReplaceSessionTurnCommand: replace one turn (activity), part 4 of 5](c4/rendered/ReplaceSessionTurnCommand-part4.svg)

View: ReplaceSessionTurnCommand-part4

![ReplaceSessionTurnCommand: replace one turn (activity), part 5 of 5](c4/rendered/ReplaceSessionTurnCommand-part5.svg)

View: ReplaceSessionTurnCommand-part5

##### Nested activity: PrepareReplacementTurn (body step of ReplaceSessionTurnCommand, before TurnWritePipeline)

![Nested activity: PrepareReplacementTurn (body step of ReplaceSessionTurnCommand, before TurnWritePipeline) (activity)](c4/rendered/PrepareReplacementTurn.svg)

View: PrepareReplacementTurn

#### CompleteSessionTurnCommand: complete one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONCLASS-002]

- Handler: `CompleteSessionTurnCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, UnifiedRequestEntryDto Turn) : ICommand<long>` in `src/QBrainAi.Services/SessionLog/Commands/CompleteSessionTurnCommand.cs` and `CompleteSessionTurnCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/CompleteSessionTurnCommandHandler.cs`, delegating to `ISessionLogService.UpsertTurnAsync` with status `completed` (TurnWritePipeline mode `Complete`; `SessionLogController.cs:425-472`).
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/complete`, MCP `sessionlog_complete_turn` (optional `turnJson`; returns `{ success, turnId, agent, sessionId, requestId, status }`), client `CompleteTurnAsync` returning `SessionLogTurnSubmitResult`, REPL `client.SessionLog.CompleteTurnAsync`. The plugin `completeTurn` verb does not call this route: it sends the bound turn with status `completed` through SubmitSessionLogCommand after `Test-ModelEvidence` and the advisory pre-check (processes PersistTurnWriteWithReceipt and AdvisoryCompletionPrecheck), and the same policies apply there.
- Request, per surface: REST takes an optional `UnifiedRequestEntryDto` and merges the whole body additively (planFile and todoId validated); MCP `sessionlog_complete_turn(agent, sessionId, requestId, workspacePath, turnJson)` parses `turnJson` and copies only the fields `FinalizeLifecycleTurnToolAsync` lists (`FwhMcpTools.SessionLog.cs:328-369`: queryTitle, queryText, response, interpretation, failureNote, tokenCount, model, actions, commits, designDecisions, requirementsDiscovered, filesModified, blockers, tags, contextList, processingDialog; never planFile or todoId), and S5 adds only `requestClass` to that list; malformed `turnJson` returns the classified error before any write. Both set `requestId` from the route and status `completed`, so no body status reaches the TurnStatus rule.
- Response: REST 200 `{ turnId, agent, sessionId, requestId, status: "completed" }`; MCP `{ success, turnId, agent, sessionId, requestId, status }` or the classified error object. Errors (REST bodies; MCP returns the classified error object for each): `InvalidIdentifier` or `InvalidPayload` 400 problem "Invalid turn payload."; `SessionNotFound` 404 `not_found`; from Phase B, `RequestClassInvalid` 400 `request_class_invalid`, `ReviewInvalid` the review codes (400) or 422 `review_status_conflict`, and `CompletionGateMissing` the completion-gate refusal with the missing items in order; SerializableWrite's four attempts used, 409 `conflict` with `retryable: true`; nothing saved on any of them. The pipeline in mode `Complete` inserts the turn at `completed` when it is absent (today) and otherwise merges additively with status `completed`. The gate fires only when the write moves the turn into `completed`, so completing an already completed turn merges the carried fields without re-gating (FR-MCP-SESSIONCLASS-002-AC004).

![CompleteSessionTurnCommand: complete one turn (activity), part 1 of 4](c4/rendered/CompleteSessionTurnCommand.svg)

View: CompleteSessionTurnCommand

![CompleteSessionTurnCommand: complete one turn (activity), part 2 of 4](c4/rendered/CompleteSessionTurnCommand-part2.svg)

View: CompleteSessionTurnCommand-part2

![CompleteSessionTurnCommand: complete one turn (activity), part 3 of 4](c4/rendered/CompleteSessionTurnCommand-part3.svg)

View: CompleteSessionTurnCommand-part3

![CompleteSessionTurnCommand: complete one turn (activity), part 4 of 4](c4/rendered/CompleteSessionTurnCommand-part4.svg)

View: CompleteSessionTurnCommand-part4

#### FailSessionTurnCommand: fail one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONCLASS-002]

- Handler: `FailSessionTurnCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, UnifiedRequestEntryDto Turn) : ICommand<long>` in `src/QBrainAi.Services/SessionLog/Commands/FailSessionTurnCommand.cs` and `FailSessionTurnCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/FailSessionTurnCommandHandler.cs`, delegating to `ISessionLogService.UpsertTurnAsync` with status `failed` (TurnWritePipeline mode `Fail`; `SessionLogController.cs:448-472`).
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/fail`, MCP `sessionlog_fail_turn` (optional `turnJson`; returns `{ success, turnId, agent, sessionId, requestId, status }`), client `FailTurnAsync` returning `SessionLogTurnSubmitResult`, REPL `client.SessionLog.FailTurnAsync`. The plugin `failTurn` verb sends the bound turn with status `failed` and its error message through SubmitSessionLogCommand (process PersistTurnWriteWithReceipt), where the same policies apply.
- Request, per surface: REST takes an optional `UnifiedRequestEntryDto` and merges the whole body additively (its `failureNote` records the failure); MCP `sessionlog_fail_turn(agent, sessionId, requestId, workspacePath, turnJson)` copies only the fields of the same `FinalizeLifecycleTurnToolAsync` list (never planFile or todoId), plus `requestClass` from S5. Both set `requestId` from the route and status `failed`.
- Response: REST 200 `{ turnId, agent, sessionId, requestId, status: "failed" }`; MCP `{ success, turnId, agent, sessionId, requestId, status }` or the classified error object. Errors as CompleteSessionTurnCommand; the pipeline in mode `Fail` inserts the turn at `failed` when it is absent (today) and otherwise merges additively with the `failureNote` and status `failed`; the gate needs only `requestClass` for `failed` and fires only when the write moves the turn into `failed`.

![FailSessionTurnCommand: fail one turn (activity), part 1 of 4](c4/rendered/FailSessionTurnCommand.svg)

View: FailSessionTurnCommand

![FailSessionTurnCommand: fail one turn (activity), part 2 of 4](c4/rendered/FailSessionTurnCommand-part2.svg)

View: FailSessionTurnCommand-part2

![FailSessionTurnCommand: fail one turn (activity), part 3 of 4](c4/rendered/FailSessionTurnCommand-part3.svg)

View: FailSessionTurnCommand-part3

![FailSessionTurnCommand: fail one turn (activity), part 4 of 4](c4/rendered/FailSessionTurnCommand-part4.svg)

View: FailSessionTurnCommand-part4

#### OpenSessionLogCommand: open (ensure) a session [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-007]

- Handler: `OpenSessionLogCommand(string WorkspacePath, string Agent, string SessionId, string? Title, string? Model) : ICommand<bool>` (whether the session was created) in `src/QBrainAi.Services/SessionLog/Commands/OpenSessionLogCommand.cs` and `OpenSessionLogCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/OpenSessionLogCommandHandler.cs`, delegating to `ISessionLogService.OpenSessionAsync` (`SessionLogService.cs:596-620`, behavior unchanged); the controller and the MCP tool compose today's bodies, and the client returns `SessionLifecycleOpenResult`.
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/open` (`SessionLogController.cs:282`), MCP `sessionlog_open`, client `OpenSessionAsync`, REPL and plugin `workflow.sessionlog.openSession`; the plugin calls it only through `Invoke-SessionOpen` (section 3.5.6), from processes StartHostSession, DrainFailsafe (the idempotent session open replayed before each bound drained record), the first-ingress writer of hosts without SessionStart (process OpenTurnFromPromptHook), and the `openSession` verb for its host session's recorded `mcpSessionId` (section 4.1).
- Request: route `agent` and `sessionId`, optional `title` and `model` (MCP `sessionlog_open(agent, sessionId, workspacePath, title, model)`). Response: REST 200 `{ agent, sessionId, created }`; MCP `{ success, agent, sessionId, created }` (`created` false when the session already existed; an existing session is never changed).
- Errors: REST 400 problem "Invalid session identifier." for a session id invalid for the agent (TurnLocator `InvalidIdentifier`; today's body, unchanged); MCP the classified error object. `Invoke-SessionOpen` treats it as final.

![OpenSessionLogCommand: open (ensure) a session (activity), part 1 of 3](c4/rendered/OpenSessionLogCommand.svg)

View: OpenSessionLogCommand

![OpenSessionLogCommand: open (ensure) a session (activity), part 2 of 3](c4/rendered/OpenSessionLogCommand-part2.svg)

View: OpenSessionLogCommand-part2

![OpenSessionLogCommand: open (ensure) a session (activity), part 3 of 3](c4/rendered/OpenSessionLogCommand-part3.svg)

View: OpenSessionLogCommand-part3

#### AppendTurnDialogCommand: append processing dialog to one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-007]

- Handler: `AppendTurnDialogCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, IReadOnlyList<ProcessingDialogItemDto> Items) : ICommand<int>` (the turn's total dialog count) in `src/QBrainAi.Services/SessionLog/Commands/AppendTurnDialogCommand.cs` and `AppendTurnDialogCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/AppendTurnDialogCommandHandler.cs`, delegating to `ISessionLogService.AppendProcessingDialogAsync` (`SessionLogService.cs:405-465`, behavior unchanged).
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/dialog` (`SessionLogController.cs:232`), MCP `sessionlog_dialog`, client `AppendDialogAsync`, REPL `client.SessionLog.AppendDialogAsync`, plugin verb `appendDialog` (bound by process BindVerbToTurn; `repl-invoke.ps1:2492-2572`), the REPL workflow, and McpAgent. The HV protocol stores a long reviewer result as ordered dialog parts on the reviewer's own turn through this unit.
- Request: one or more `{ timestamp, role, content, category }` items, appended after the highest existing ordinal (MCP `sessionlog_dialog(agent, sessionId, requestId, itemsJson, workspacePath)`). Response: REST 200 `{ agent, sessionId, requestId, totalDialogCount }`; MCP `{ success, totalDialogItems }` (`FwhMcpTools.SessionLog.cs:76-97`); client `DialogAppendResult`.
- Errors: REST 400 `{ error }` for no item or an invalid session or request id (TurnLocator `InvalidIdentifier`), and 404 for a missing session or turn (`SessionNotFound`, `TurnNotFound`) (today's bodies, `SessionLogController.cs:232-280`, unchanged); MCP `{ error: "items required" }` for no item and the classified error object otherwise. The server appends on any turn status; the plugin refuses `appendDialog` on a turn it knows is terminal with `turn_immutable` before any call (FR-MCP-SESSIONTURN-004-AC004).

![AppendTurnDialogCommand: append processing dialog to one turn (activity), part 1 of 3](c4/rendered/AppendTurnDialogCommand.svg)

View: AppendTurnDialogCommand

![AppendTurnDialogCommand: append processing dialog to one turn (activity), part 2 of 3](c4/rendered/AppendTurnDialogCommand-part2.svg)

View: AppendTurnDialogCommand-part2

![AppendTurnDialogCommand: append processing dialog to one turn (activity), part 3 of 3](c4/rendered/AppendTurnDialogCommand-part3.svg)

View: AppendTurnDialogCommand-part3

#### SetTurnTitleCommand: retitle one turn [FR-MCP-SESSIONTURN-003, FR-MCP-SESSIONTURN-007]

- Handler: `SetTurnTitleCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, string Title) : ICommand<long>` (the turn id; the controller composes today's body, the client returns `SessionLogMutationResult`) in `src/QBrainAi.Services/SessionLog/Commands/SetTurnTitleCommand.cs` and `SetTurnTitleCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/SetTurnTitleCommandHandler.cs`, delegating to `ISessionLogService.SetTurnTitleAsync` (`SessionLogService.cs:922-955`, behavior unchanged: an explicit overwrite of `QueryTitle` that incidental submits never reset, TR-MCP-SESSIONLOG-005).
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/title` (`SessionLogController.cs:350`), client `SetTurnTitleAsync`, REPL `client.SessionLog.SetTurnTitleAsync`, plugin verb `setTurnTitle` (bound by process BindVerbToTurn, so an explicit `requestId` retitles only that turn, TR-MCP-SESSIONTURN-003 AC-005; `repl-invoke.ps1:2925-2941`); no MCP tool.
- Request `{ title }` (a missing body stores an empty title, as today). Response 200 `{ turnId, agent, sessionId, requestId, retitled: true }` (`SessionLogController.cs:363`). Errors: 400 invalid turn identifier for an invalid session or request id (TurnLocator `InvalidIdentifier`, today's body); 404 `not_found` for a missing session or turn (`SessionNotFound`, `TurnNotFound`; `ClassifiedError`). The plugin refuses `setTurnTitle` on a locally terminal turn with `turn_immutable` (FR-MCP-SESSIONTURN-004-AC004).

![SetTurnTitleCommand: retitle one turn (activity), part 1 of 3](c4/rendered/SetTurnTitleCommand.svg)

View: SetTurnTitleCommand

![SetTurnTitleCommand: retitle one turn (activity), part 2 of 3](c4/rendered/SetTurnTitleCommand-part2.svg)

View: SetTurnTitleCommand-part2

![SetTurnTitleCommand: retitle one turn (activity), part 3 of 3](c4/rendered/SetTurnTitleCommand-part3.svg)

View: SetTurnTitleCommand-part3

#### SetSessionTitleCommand: retitle a session [FR-MCP-SESSIONTURN-007]

- Handler: `SetSessionTitleCommand(string WorkspacePath, string Agent, string SessionId, string Title) : ICommand<long>` (the session row id; the controller composes today's body, the client returns `SessionLogMutationResult`) in `src/QBrainAi.Services/SessionLog/Commands/SetSessionTitleCommand.cs` and `SetSessionTitleCommandHandler` in `src/QBrainAi.Services/SessionLog/Commands/SetSessionTitleCommandHandler.cs`, delegating to `ISessionLogService.SetSessionTitleAsync` (`SessionLogService.cs:892-918`, behavior unchanged).
- Surfaces: `POST /qbrainai/sessionlog/{agent}/{sessionId}/title` (`SessionLogController.cs:313`), client `SetSessionTitleAsync`, REPL `client.SessionLog.SetSessionTitleAsync`, plugin verb `setSessionTitle` (targets the host session's own `mcpSessionId`, process StartHostSession; this replaces FR-MCP-REPL-010 inline AC (2), which writes the title into `session-state.yaml`); no MCP tool.
- Request `{ title }` (a missing body stores an empty title, as today). Response 200 `{ turnId, agent, sessionId, retitled: true }`, where `turnId` carries the session row id (`SessionLogController.cs:326`, unchanged). Errors: 400 invalid session identifier for an invalid session id (TurnLocator `InvalidIdentifier`, today's body); 404 `not_found` for a missing session (`SessionNotFound`; `ClassifiedError`).

![SetSessionTitleCommand: retitle a session (activity), part 1 of 3](c4/rendered/SetSessionTitleCommand.svg)

View: SetSessionTitleCommand

![SetSessionTitleCommand: retitle a session (activity), part 2 of 3](c4/rendered/SetSessionTitleCommand-part2.svg)

View: SetSessionTitleCommand-part2

![SetSessionTitleCommand: retitle a session (activity), part 3 of 3](c4/rendered/SetSessionTitleCommand-part3.svg)

View: SetSessionTitleCommand-part3

Existing session-log routes this plan does not change (they keep today's behavior and are not units of this plan): `POST .../{requestId}/begin` (`BeginTurnAsync`; the plugins stop calling it, because ingress mints through MintSessionTurnCommand), `DELETE .../{requestId}` and `DELETE .../{sessionId}` (turn and session delete), `POST /qbrainai/sessionlog/repair-workspace-stamps`, and the section routes `PUT` and `DELETE .../sections/{section}` and `DELETE .../sections/{section}/items/{itemKey}` for every section other than the two review sections. For `agentReview` and `hostileReview` (and their aliases) the section `PUT` dispatches RecordSessionTurnReviewCommand, the section `DELETE` dispatches ResetSessionTurnReviewCommand, and the item `DELETE` is refused by the existing delete-item handler (Phase B, S4).

### 4.3 Policies applied inside Phase A handlers

#### Policy TerminalTurnOutcomeGuard: terminal-turn outcome guard (`ITerminalTurnOutcomeGuard`) [FR-MCP-SESSIONTURN-004]

Applied by TurnWritePipeline in mode `Submit` only (SubmitSessionLogCommand), as today: the single-turn routes (UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, FailSessionTurnCommand) are the explicit route for deliberate status corrections and have no outcome guard (`SessionLogService.cs:1610`). It never saves; it returns the outcome the pipeline must honor. The seam `ITerminalTurnOutcomeGuard` (namespace `QBrainAi.Support.Mcp.SessionLog`; named for the rule it applies, since the upsert route never applies it) replaces the inline guard at `SessionLogService.cs:1606-1618` without changing its rule (TR-MCP-SESSIONTURN-004 AC-002): a write is skipped only when the stored status is terminal (`TurnStatus.IsTerminal`) and the incoming status is supplied, differs from it, and is either not terminal or carried by a superseded hook persist; a write that omits the status, a review-section write, and a requestClass-only write merge on a terminal turn (FR-MCP-SESSIONTURN-004-AC003, AC004). This diagram is the only drawing of the rule.

![Policy TerminalTurnOutcomeGuard: terminal-turn outcome guard (`ITerminalTurnOutcomeGuard`) (activity), part 1 of 2](c4/rendered/TerminalTurnOutcomeGuard.svg)

View: TerminalTurnOutcomeGuard

![Policy TerminalTurnOutcomeGuard: terminal-turn outcome guard (`ITerminalTurnOutcomeGuard`) (activity), part 2 of 2](c4/rendered/TerminalTurnOutcomeGuard-part2.svg)

View: TerminalTurnOutcomeGuard-part2

### 4.4 Processes (plugin, host, and build; they call the handlers above)

#### Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-002]

- Hosts: claude-code and claude-cowork (the bridge is their only minter; the prompt hook neither mints nor claims there). In `Pilot` or `Stateless`, the prompt hook on these hosts applies pending records through `Sync-PluginArrivalLog` up to the transcript length at its own start, takes the bridge-minted `requestId` of the delivered request, and runs process RunStatelessInvocation (section 6.6); when that request's `enqueue` record is not yet in the transcript the invocation fails, and a message absorbed into a running turn runs as `Pilot` (section 3.1, 2026-10-12).
- Trigger: a complete transcript record past `appliedThroughOffset`; also applied by `Sync-PluginArrivalLog` from a verb, the Stop hook, the prompt hook (above), or SessionStart up to the transcript length recorded at their own start.
- Input: transcript `queue-operation` records `enqueue`, `dequeue` (no content: it always takes the queue head), `remove` (with reason `absorbed_mid_turn`, another reason, or none), and `popAll`; a transcript shorter than `appliedThroughOffset` is an unrecognized format.
- Output: queue and delivery state in `turn-state.yaml`. Each record is one `Update-TurnState` change made by the process that holds `turn-open.lock` (the bridge, or a verb, the Stop hook, the prompt hook, or SessionStart through `Sync-PluginArrivalLog`): the record's queue change, any `unsupportedSince` that `Set-SessionUnsupported` records, and the advance of `appliedThroughOffset` past it are one atomic replace; the record's mint (MintSessionTurnCommand, classified by `Resolve-MintOutcome`) happens before that replace and is idempotent. The bridge keeps no write-ahead mint record: the transcript is its durable source, so a crash between the mint and the replace re-applies the record, and the same nonce returns the stored turn. Queue entries are of kind `request`, `system`, or `open-failed` (a final mint refusal keeps its queue position so later `dequeue`, `remove`, and `popAll` records stay attributable, delivers nothing, and is never minted again by the bridge, which keeps no failsafe record for it); a request entry whose mint returned `merged` with a terminal `storedStatus` keeps its position with that status, and its `dequeue`, `remove`, or `popAll` delivers and withdraws nothing (never the write target, the open list, or the ledger, FR-MCP-SESSIONTURN-001-AC002). A system event is also a host-local command that never reaches the model (for example `/clear`, `/compact`) when P0.2 shows it produces an arrival event (section 3.1).
- Fail closed (D6): a record the bridge cannot attribute (a `remove` matching zero or two or more queued entries, or a `popAll` in which some content's line count differs from the queued entries with that content or that leaves a queued entry unpopped) calls `Set-SessionUnsupported` with reason `queue_removal_ambiguous`; an unknown operation, a missing required field, a `dequeue` with no queue head, or a transcript shorter than `appliedThroughOffset` calls it with reason `arrival_bridge_format_unrecognized`; both with `causeOffset`. `Set-SessionUnsupported` records `unsupportedSince` only after a delivered request and otherwise only reports `turn_logging_unsupported`; either way the bridge stops before the record without advancing `appliedThroughOffset`.

The diagram below checks the record and hands each operation to its nested activity, which returns the queue change or a fail-closed reason; `Update-TurnState` then commits the change with `appliedThroughOffset` advanced past the record in one atomic replace. Both `Set-SessionUnsupported` calls pass `causeOffset`. A `popAll` is attributable when, for every content, the popped lines with that content equal the queued entries of every kind with that content and every queued entry is popped; it then withdraws every queued request entry in progress and drops every other queued entry.

![Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 1 of 7](c4/rendered/ApplyArrivalBridgeRecord.svg)

View: ApplyArrivalBridgeRecord

![Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 2 of 7](c4/rendered/ApplyArrivalBridgeRecord-part2.svg)

View: ApplyArrivalBridgeRecord-part2

![Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 3 of 7](c4/rendered/ApplyArrivalBridgeRecord-part3.svg)

View: ApplyArrivalBridgeRecord-part3

![Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 4 of 7](c4/rendered/ApplyArrivalBridgeRecord-part4.svg)

View: ApplyArrivalBridgeRecord-part4

![Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 5 of 7](c4/rendered/ApplyArrivalBridgeRecord-part5.svg)

View: ApplyArrivalBridgeRecord-part5

![Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 6 of 7](c4/rendered/ApplyArrivalBridgeRecord-part6.svg)

View: ApplyArrivalBridgeRecord-part6

![Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge (activity), part 7 of 7](c4/rendered/ApplyArrivalBridgeRecord-part7.svg)

View: ApplyArrivalBridgeRecord-part7

##### Nested activity: ApplyEnqueueRecord (enqueue step of ApplyArrivalBridgeRecord)

A system event is a task-notification, an agent-message, a subagent marker, or a host-local command that P0.2 shows produces an arrival event. A final mint refusal becomes an `open-failed` entry with the server code and no `requestId`; it keeps its queue position, is never minted again by the bridge (the bridge keeps no failsafe record for it), and the Stop gate warns naming the prompt.

![Nested activity: ApplyEnqueueRecord (enqueue step of ApplyArrivalBridgeRecord) (activity), part 1 of 3](c4/rendered/ApplyEnqueueRecord.svg)

View: ApplyEnqueueRecord

![Nested activity: ApplyEnqueueRecord (enqueue step of ApplyArrivalBridgeRecord) (activity), part 2 of 3](c4/rendered/ApplyEnqueueRecord-part2.svg)

View: ApplyEnqueueRecord-part2

![Nested activity: ApplyEnqueueRecord (enqueue step of ApplyArrivalBridgeRecord) (activity), part 3 of 3](c4/rendered/ApplyEnqueueRecord-part3.svg)

View: ApplyEnqueueRecord-part3

##### Nested activity: ApplyDequeueRecord (dequeue step of ApplyArrivalBridgeRecord)

A `dequeue` carries no content and always takes the queue head. An entry of kind `system` or `open-failed`, or a request entry terminal at mint, is dropped and delivers nothing; a request entry in progress becomes the write target and enters `openDelivered` and the ledger.

![Nested activity: ApplyDequeueRecord (dequeue step of ApplyArrivalBridgeRecord) (activity), part 1 of 2](c4/rendered/ApplyDequeueRecord.svg)

View: ApplyDequeueRecord

![Nested activity: ApplyDequeueRecord (dequeue step of ApplyArrivalBridgeRecord) (activity), part 2 of 2](c4/rendered/ApplyDequeueRecord-part2.svg)

View: ApplyDequeueRecord-part2

##### Nested activity: ApplyRemoveRecord (remove step of ApplyArrivalBridgeRecord)

A `remove` names the removed content; it is attributable only when exactly one queued entry matches it. A matched request entry in progress is delivered (reason `absorbed_mid_turn`, it becomes the write target) or withdrawn (another reason or none, the write target unchanged); any other matched entry is dropped.

![Nested activity: ApplyRemoveRecord (remove step of ApplyArrivalBridgeRecord) (activity), part 1 of 2](c4/rendered/ApplyRemoveRecord.svg)

View: ApplyRemoveRecord

![Nested activity: ApplyRemoveRecord (remove step of ApplyArrivalBridgeRecord) (activity), part 2 of 2](c4/rendered/ApplyRemoveRecord-part2.svg)

View: ApplyRemoveRecord-part2

#### Process OpenTurnFromPromptHook: open a turn from the prompt hook (hosts without a bridge) [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-002, FR-MCP-SESSIONTURN-003]

- Hosts: codex, grok, grok-bot, copilot, and classic cline (its native UserPromptSubmit hook is a PowerShell command hook); cline-v2 (supported only when P0.2 shows one run per user message, otherwise D6) and opencode through their adapters, which forward the prompt event to the same PowerShell core.
- Input: the host's prompt hook payload; a payload without the host row's session key refuses and writes nothing (section 3.1). Output: a delivered request and write target, or a hook status. In `Pilot` or `Stateless`, a delivered request continues with process RunStatelessInvocation (section 6.6), where a pending mint fails the invocation (section 3.1).
- Hook statuses: `turn-opened`, `turn-opened-degraded` (an outage or a retryable answer: names the `pending-<nonce>` id and the failsafe path, FR-MCP-SESSIONLIFE-001; the degraded mint replaces the degraded `beginTurn` of FR-MCP-SESSIONLIFE-001's statement, ac1, and ac3, TR-MCP-SESSIONLIFE-001, FR-MCP-170 ac1, TR-MCP-TRIAGESTORE-002, and FR-MCP-TRIAGEPLUGIN-001's "`beginTurn` Submit timeout"), `turn-already-open`, `system-event-skipped`, `turn-open-contended`, `turn-open-failed` (reason `nonce_source_missing` or the server code), `turn_logging_unsupported`, `host_session_unknown`.
- Lock: the open is one `Update-TurnState` change. The hook takes `turn-open.lock` once and never waits: on contention it reports `turn-open-contended`, writes a failsafe diagnostic naming the prompt (as for `nonce_source_missing`), and mints nothing, and the Stop gate warns `turn-open-contended` naming the prompt (section 3.1). `Invoke-SessionOpen` and `Set-SessionUnsupported` called inside the open record into its one atomic replace instead of taking the lock again.
- Write-ahead (TR-MCP-SESSIONTURN-001 AC-002): under the lock the hook applies the equal-field rule, then writes the failsafe mint record (nonce, binding, root-id envelope) before the first mint call. A created, merged, or merged-terminal result, and an equal-field fail-close, clear it after the replace; an outage or a retryable answer (a transport outage, 503 `backend_unavailable`, a busy store's 503 `persistence_error`, or 409 `conflict` with `retryable: true`) keeps it for process DrainFailsafe, which opens the session before it mints; a final refusal counts as the record's first final answer (`drainAttempts` 1), so the drain mints it again under the attempt budget, each drained final answer counting one more attempt (`Resolve-MintOutcome`, section 3.5.6), and then quarantines it (process DrainFailsafe), and the Stop gate warns naming the prompt while the record is queued or quarantined. A hook killed after that write ends at the next hook, verb, or SessionStart drain with exactly one server turn for the message, delivered once (section 3.1).
- First-ingress writer (hosts without a SessionStart event: classic cline, cline-v2, opencode): records a new `mcpSessionId` for the host session key only when none is recorded, in the open's replace, then opens it through `Invoke-SessionOpen` (section 3.4). From Phase C it is also the host session's Mode reader, as StartHostSession is on the other hosts: it reads the Mode with GetAgentContextStatusQuery at this first prompt and records `agentContextMode` in the same replace, `Off` when it cannot read it or does not know the value (section 3.4; section 3.1, "Mode changes").
- Before the open, the hook drains its host session's failsafe when it holds drainable records (process DrainFailsafe), so an earlier prompt's mint reaches the server first.

The diagram below runs the checks that need no lock, then the open itself as one `Update-TurnState` change (nested activity OpenTurnUnderLock). The host is supported for turn logging when its P0.2 row passed and, on copilot and classic cline, the single-registration check holds; otherwise `Set-SessionUnsupported` records the failing check's reason (for example `duplicate_prompt_hook`). A system event is a task-notification, an agent-message, `agent_id`, `subagentType`, the subagent environment marker, an opencode task child session, or a host-local command that P0.2 shows produces an arrival event.

![Process OpenTurnFromPromptHook: open a turn from the prompt hook (hosts without a bridge) (activity), part 1 of 4](c4/rendered/OpenTurnFromPromptHook.svg)

View: OpenTurnFromPromptHook

![Process OpenTurnFromPromptHook: open a turn from the prompt hook (hosts without a bridge) (activity), part 2 of 4](c4/rendered/OpenTurnFromPromptHook-part2.svg)

View: OpenTurnFromPromptHook-part2

![Process OpenTurnFromPromptHook: open a turn from the prompt hook (hosts without a bridge) (activity), part 3 of 4](c4/rendered/OpenTurnFromPromptHook-part3.svg)

View: OpenTurnFromPromptHook-part3

![Process OpenTurnFromPromptHook: open a turn from the prompt hook (hosts without a bridge) (activity), part 4 of 4](c4/rendered/OpenTurnFromPromptHook-part4.svg)

View: OpenTurnFromPromptHook-part4

##### Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change)

The equal-field check applies on copilot and classic cline: a nonce that already has a failsafe mint record or a pending turn in this host session fails the host session closed (`indistinguishable_arrival` with `causeNonce`). The write-ahead mint record carries the nonce, the binding, and the root-id envelope. The first-ingress writer applies on a host without a SessionStart event when no `mcpSessionId` is recorded for the host session. A created turn is delivered (write target, `openDelivered`, ledger entry); a merged terminal turn reports its `storedStatus` and is never the write target, in the open list, or in the ledger. The mint record follows the write-ahead rule above (`Resolve-MintOutcome`, section 3.5.6): a created, merged, or merged-terminal result and the equal-field fail-close clear it; an outage or a retryable answer keeps it for the drain; a final refusal keeps it with `drainAttempts` 1, so the drain mints it again under the attempt budget and then quarantines it (`PromptHookFinalMintRefusalRecordKeptDrainAttemptsOne`). The first-ingress writer records the new `mcpSessionId` in this replace before `Invoke-SessionOpen` runs, so an open that meets an outage keeps that identity and the drain opens the same id (`TsHostFirstIngressOpenOutageRecordedSessionIdReusedByDrainNoSecondId`).

![Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change) (activity), part 1 of 6](c4/rendered/OpenTurnUnderLock.svg)

View: OpenTurnUnderLock

![Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change) (activity), part 2 of 6](c4/rendered/OpenTurnUnderLock-part2.svg)

View: OpenTurnUnderLock-part2

![Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change) (activity), part 3 of 6](c4/rendered/OpenTurnUnderLock-part3.svg)

View: OpenTurnUnderLock-part3

![Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change) (activity), part 4 of 6](c4/rendered/OpenTurnUnderLock-part4.svg)

View: OpenTurnUnderLock-part4

![Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change) (activity), part 5 of 6](c4/rendered/OpenTurnUnderLock-part5.svg)

View: OpenTurnUnderLock-part5

![Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change) (activity), part 6 of 6](c4/rendered/OpenTurnUnderLock-part6.svg)

View: OpenTurnUnderLock-part6

#### Process BindVerbToTurn: bind a session-log verb to its turn [FR-MCP-SESSIONTURN-007]

- Verbs: `appendActions`, `appendDialog`, `appendDecisions`, `updateTurn`, `completeTurn`, `failTurn`, `setTurnTitle`, and from Phase B `recordReview`.
- Request: the verb's payload plus an optional explicit `requestId`, or a trusted recovery pair (`sessionId`, `requestId`).
- Response: the resolved target request id and an immutable invocation binding `{ hostSessionKey, keySource, ingressNonce, requestId, boundAtUtc }` stored with the persist and failsafe records; or a refusal code.
- Refusal codes: `request_unknown`, `request_ambiguous`, `turn_immutable`, `host_session_unknown`, `host_session_ambiguous`, `turn_logging_unsupported`; warning `host_session_inferred`.
- Trusted pair and `unsupportedSince` (section 3.1, unsupported-session failsafe): the trusted pair is the recovery and review-backfill route of C5, and it does not bypass `unsupportedSince`. A pair whose turn belongs to a host session that still has `unsupportedSince` while it is live is handled as that host session's own write (captured as `unsupported_session` when the turn was open when the session failed closed, refused otherwise), so nothing reaches the server during that host session; once that host session has ended or a SessionStart cleared its cause, the pair writes through to the server (its captured records drain then, process DrainFailsafe, and its reviews are recorded through the pair).
- Closed write target: with no delivered request open, an unqualified verb targets the write target even when it is closed, so `recordReview` and a requestClass-only update are accepted on it and every other verb is refused `turn_immutable` (FR-MCP-SESSIONTURN-004-AC004, FR-MCP-SESSIONTURN-007-AC002); with exactly one delivered request open and the write target closed, every other verb is refused `turn_immutable` naming the open request (FR-MCP-SESSIONTURN-007-AC007).
- A keyed verb for a host session ended by the idle rule revives it first (the marker removed by `Update-TurnState`, FR-MCP-SESSIONTURN-001-AC009).

The diagram below resolves the host session and then the target (two nested activities), and applies the `unsupportedSince` and terminal checks to the resolved target. A verb refused `turn_logging_unsupported` because it is a review is recorded after the drain through the original pair; a `turn_immutable` refusal names the target and any open delivered request; a bound verb proceeds to process PersistTurnWriteWithReceipt.

![Process BindVerbToTurn: bind a session-log verb to its turn (activity), part 1 of 5](c4/rendered/BindVerbToTurn.svg)

View: BindVerbToTurn

![Process BindVerbToTurn: bind a session-log verb to its turn (activity), part 2 of 5](c4/rendered/BindVerbToTurn-part2.svg)

View: BindVerbToTurn-part2

![Process BindVerbToTurn: bind a session-log verb to its turn (activity), part 3 of 5](c4/rendered/BindVerbToTurn-part3.svg)

View: BindVerbToTurn-part3

![Process BindVerbToTurn: bind a session-log verb to its turn (activity), part 4 of 5](c4/rendered/BindVerbToTurn-part4.svg)

View: BindVerbToTurn-part4

![Process BindVerbToTurn: bind a session-log verb to its turn (activity), part 5 of 5](c4/rendered/BindVerbToTurn-part5.svg)

View: BindVerbToTurn-part5

##### Nested activity: ResolveVerbHostSession (host session step of BindVerbToTurn)

A verb without a host session key in its invocation context binds through a trusted pair (`sessionId`, `requestId`) verified on the server, whose turn stays in its owning host session, or through the only live host session of its agent in the workspace (warning `host_session_inferred`). Loading a host session reads `sessions/<key>/turn-state.yaml`; an idle-ended host session revives.

![Nested activity: ResolveVerbHostSession (host session step of BindVerbToTurn) (activity), part 1 of 3](c4/rendered/ResolveVerbHostSession.svg)

View: ResolveVerbHostSession

![Nested activity: ResolveVerbHostSession (host session step of BindVerbToTurn) (activity), part 2 of 3](c4/rendered/ResolveVerbHostSession-part2.svg)

View: ResolveVerbHostSession-part2

![Nested activity: ResolveVerbHostSession (host session step of BindVerbToTurn) (activity), part 3 of 3](c4/rendered/ResolveVerbHostSession-part3.svg)

View: ResolveVerbHostSession-part3

##### Nested activity: ResolveVerbTarget (target step of BindVerbToTurn)

An explicit original pair of an earlier MCP session must be verified on the server and recorded for this host session, so a pair from another conversation is never accepted. An explicit `requestId` must be known in this host session's MCP session. Without an explicit target, two or more open delivered requests are refused `request_ambiguous` naming them; exactly one, or none with a write target set (open or closed), targets the write target; none without a write target is refused `request_unknown` naming the host session key.

![Nested activity: ResolveVerbTarget (target step of BindVerbToTurn) (activity), part 1 of 5](c4/rendered/ResolveVerbTarget.svg)

View: ResolveVerbTarget

![Nested activity: ResolveVerbTarget (target step of BindVerbToTurn) (activity), part 2 of 5](c4/rendered/ResolveVerbTarget-part2.svg)

View: ResolveVerbTarget-part2

![Nested activity: ResolveVerbTarget (target step of BindVerbToTurn) (activity), part 3 of 5](c4/rendered/ResolveVerbTarget-part3.svg)

View: ResolveVerbTarget-part3

![Nested activity: ResolveVerbTarget (target step of BindVerbToTurn) (activity), part 4 of 5](c4/rendered/ResolveVerbTarget-part4.svg)

View: ResolveVerbTarget-part4

![Nested activity: ResolveVerbTarget (target step of BindVerbToTurn) (activity), part 5 of 5](c4/rendered/ResolveVerbTarget-part5.svg)

View: ResolveVerbTarget-part5

#### Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-005]

- Request: for `appendActions`, `appendDecisions`, `updateTurn`, `completeTurn`, and `failTurn`, a session document holding only the bound turn, built by `New-McpPluginTurnUpsertRequest` with the binding and sent as `client.SessionLog.SubmitAsync` (`POST /qbrainai/sessionlog`, `Invoke-ReplPersistTurn`); for `appendDialog`, `client.SessionLog.AppendDialogAsync` (`POST .../{requestId}/dialog`); for `setTurnTitle`, `client.SessionLog.SetTurnTitleAsync` (`POST .../{requestId}/title`).
- Before sending (FR-MCP-SESSIONTURN-001-AC008: one turn's writes reach the server in their original order, and no server turn ever has a `pending-` requestId): a verb whose host session's failsafe holds drainable records, or whose bound id is a `pending-<nonce>` id, first runs process DrainFailsafe. When the drain stops, leaves a record of the bound turn queued, or the pending id still has no journaled alias, the write is captured in the failsafe behind the retained records (`queued`) and is never sent under the pending id, on the submit, dialog, and title routes.
- Close verbs first: `completeTurn` and `failTurn` call `Test-ModelEvidence` and are refused locally with `turn_missing_model_evidence` when the turn has no model-authored action, design decision, or commit (hook invocations excluded; FR-MCP-SESSIONTURN-005-AC002). A close that carries evidence sends it first as an additive non-transition write, verified against the sanitized read projection (section 3.1), and sends the transition only after that write is confirmed, so a gate refusal loses nothing (FR-MCP-SESSIONCLASS-004-AC003, from S8); a refused evidence write stops the close.
- The plugin's `canceled` close of a Phase C package failure (process RunStatelessInvocation, section 6.6) is written by the plugin on this path with the failure reason and never calls `Test-ModelEvidence`, which does not apply to a `canceled` close; an outage holds it in the failsafe, and the Stop gate warns degraded persistence naming the package failure (never blocks) and keeps the ledger entry until the drain confirms the close.
- Retryable answers (section 3.1, "Deadlock and busy outcomes" and "Retryable answers"): a transport outage, a retryable 503 (`backend_unavailable`, or a busy store's `persistence_error` with `retryable: true`), and 409 `conflict` with `retryable: true` (SerializableWrite's attempts used) are one outage case: the failsafe record is kept and the receipt is `persisted: false`, code `queued`, exit 0, as for 503 today (FR-MCP-SESSIONLIFE-003 ac6, FR-MCP-171), and the request stays open and ledgered until the drain confirms the write. A drain stops its walk on every retryable answer without counting an attempt (process DrainFailsafe).
- Final answers: a live verb clears its record on a final answer (every exit-1 receipt except `rejected` and `lost`); a drain keeps the record and counts the attempt instead (process DrainFailsafe).
- Response (plugin receipt): `persisted` (bool), `code` (`persisted`, `unchanged`, `queued`, `lost`, `rejected`, `skipped_terminal`, `turn_missing_model_evidence`, or the server error code with its `retryable` flag), `storedStatus`, `reason`, and `outcomeVerified` (false when the server answered without per-turn outcomes, FR-MCP-SESSIONTURN-004-AC005); exit code 0 for `persisted`, `unchanged`, and `queued` (a confirmed retained failsafe is a classified queued success, as today: FR-MCP-SESSIONLIFE-003, FR-MCP-REPL-009, `repl-invoke.ps1:3131-3141`), 1 otherwise.

![Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 1 of 8](c4/rendered/PersistTurnWriteWithReceipt.svg)

View: PersistTurnWriteWithReceipt

![Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 2 of 8](c4/rendered/PersistTurnWriteWithReceipt-part2.svg)

View: PersistTurnWriteWithReceipt-part2

![Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 3 of 8](c4/rendered/PersistTurnWriteWithReceipt-part3.svg)

View: PersistTurnWriteWithReceipt-part3

![Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 4 of 8](c4/rendered/PersistTurnWriteWithReceipt-part4.svg)

View: PersistTurnWriteWithReceipt-part4

![Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 5 of 8](c4/rendered/PersistTurnWriteWithReceipt-part5.svg)

View: PersistTurnWriteWithReceipt-part5

![Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 6 of 8](c4/rendered/PersistTurnWriteWithReceipt-part6.svg)

View: PersistTurnWriteWithReceipt-part6

![Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 7 of 8](c4/rendered/PersistTurnWriteWithReceipt-part7.svg)

View: PersistTurnWriteWithReceipt-part7

![Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) (activity), part 8 of 8](c4/rendered/PersistTurnWriteWithReceipt-part8.svg)

View: PersistTurnWriteWithReceipt-part8

##### Nested activity: SendBoundWrite (send step of PersistTurnWriteWithReceipt, for the evidence write and the bound write, and of the drain's ReplayBoundRecord)

A repeated unchanged update has the same fingerprint as this turn's last confirmed write and is not sent again. The write-ahead failsafe record is keyed by the bound `requestId` (a drained record is reused as its own write-ahead record). The send goes through the REPL under the bound id, or under its minted id through the alias journal: the submit with the bound turn, the dialog append, or the turn title. The retryable answers are a transport outage, a retryable 503, and 409 `conflict` with `retryable: true`. A completion-gate refusal is recorded locally with its missing items for the Stop gate, and the turn stays in progress. On a final answer (every exit-1 terminal below except `lost` and `rejected`) a live verb clears its record; a drain keeps it and counts the attempt (process DrainFailsafe).

![Nested activity: SendBoundWrite (send step of PersistTurnWriteWithReceipt, for the evidence write and the bound write, and of the drain's ReplayBoundRecord) (activity), part 1 of 5](c4/rendered/SendBoundWrite.svg)

View: SendBoundWrite

![Nested activity: SendBoundWrite (send step of PersistTurnWriteWithReceipt, for the evidence write and the bound write, and of the drain's ReplayBoundRecord) (activity), part 2 of 5](c4/rendered/SendBoundWrite-part2.svg)

View: SendBoundWrite-part2

![Nested activity: SendBoundWrite (send step of PersistTurnWriteWithReceipt, for the evidence write and the bound write, and of the drain's ReplayBoundRecord) (activity), part 3 of 5](c4/rendered/SendBoundWrite-part3.svg)

View: SendBoundWrite-part3

![Nested activity: SendBoundWrite (send step of PersistTurnWriteWithReceipt, for the evidence write and the bound write, and of the drain's ReplayBoundRecord) (activity), part 4 of 5](c4/rendered/SendBoundWrite-part4.svg)

View: SendBoundWrite-part4

![Nested activity: SendBoundWrite (send step of PersistTurnWriteWithReceipt, for the evidence write and the bound write, and of the drain's ReplayBoundRecord) (activity), part 5 of 5](c4/rendered/SendBoundWrite-part5.svg)

View: SendBoundWrite-part5

##### Nested activity: MapSubmitOutcome (submit answer step of SendBoundWrite)

A confirmed outcome has a matching identity and an exact readback equal to the intended fields; a readback that differs, a degraded answer, or an identity mismatch is not confirmed. A close is `completeTurn`, `failTurn`, or the plugin's `canceled` close; a confirmed close removes the request from `openDelivered` and the ledger. Without a turns list (an older server) the receipt is `persisted` with `outcomeVerified: false`: the record is cleared, because the server stored the write, but it is never a confirmed close, so `openDelivered` and the ledger keep the request. A `skipped_terminal` outcome caches the `storedStatus` as terminal, so later verbs are refused locally with `turn_immutable`.

![Nested activity: MapSubmitOutcome (submit answer step of SendBoundWrite) (activity), part 1 of 3](c4/rendered/MapSubmitOutcome.svg)

View: MapSubmitOutcome

![Nested activity: MapSubmitOutcome (submit answer step of SendBoundWrite) (activity), part 2 of 3](c4/rendered/MapSubmitOutcome-part2.svg)

View: MapSubmitOutcome-part2

![Nested activity: MapSubmitOutcome (submit answer step of SendBoundWrite) (activity), part 3 of 3](c4/rendered/MapSubmitOutcome-part3.svg)

View: MapSubmitOutcome-part3

#### Process DrainFailsafe: drain the failsafe [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-005]

- Trigger: SessionStart (process StartHostSession); the next prompt hook or verb whose host session's failsafe holds drainable records (after an outage, or after a prompt hook killed past its write-ahead mint record; processes OpenTurnFromPromptHook and PersistTurnWriteWithReceipt); and the explicit `workflow.failsafe.drain` verb (`Invoke-WorkflowFailsafeDrain`, `repl-invoke.ps1:2830-2856` at `a8d3934b`), which follows the same rules. The automatic drain on the first proven-reachable REPL call of a process (`Invoke-ReplFailsafeDrainOnFirstSuccess`, `:1188-1238`, wired in `Invoke-ReplRaw` at `:652-655`) is removed, because the hook and verb triggers already drain before their own sends and a drain started inside a REPL call could run inside an `Update-TurnState` change that holds `turn-open.lock`, as the prompt hook's mint does (inference); this replaces the first-success trigger of FR-MCP-REPL-011 ac1 and TR-MCP-REPL-016 and the same statement in FR-MCP-172, TR-MCP-PERSIST-003, TR-MCP-FAILSAFE-001, TR-MCP-SESSIONLIFE-004, TEST-MCP-FAILSAFE-001, TEST-MCP-SESSIONLIFE-005, and TEST-MCP-195, and TEST-MCP-REPL-037 (`PluginPowerShellRuntime.Tests.ps1:3859`) changes with it. The in-flight skip stays: a record written by the submit in flight in the same process is not replayed by that process's drain (`repl-invoke.ps1:876-928,1094`, TR-MCP-REPL-016).
- Input: failsafe records oldest first (the file name starts with the UTC capture stamp, `repl-invoke.ps1:1087-1090`), each bound record with its captured binding and root-id envelope.
- Engine: develop's `Invoke-ReplFailsafeDrain` (`repl-invoke.ps1:1027-1186`) becomes this process, and the quarantine (`Move-ReplFailsafeToQuarantine`, `:966`) is the one place for a record the drain cannot replay (section 3.1, "Failsafe drain"). Rules, applied to each record in order (nested activity DrainOneFailsafeRecord):
  - A retryable answer (a transport outage, a retryable 503, or 409 `conflict` with `retryable: true`) stops the walk without counting an attempt; that record and every later one stay queued (develop `:1164-1169`).
  - A confirmed answer removes the record: a receipt `persisted` (including `outcomeVerified: false`), `unchanged`, or `skipped_terminal`, or a mint that returns a turn.
  - A final answer (every other answer, including a mint refused for a final reason by `Resolve-MintOutcome` and a session open refused as an invalid session identifier by `Invoke-SessionOpen`) increments the record's `drainAttempts` and keeps the answer (`lastDrainError`, develop `:1172-1175`), and the walk continues. The record's turn (its bound `requestId`, or the pending id of a mint) is then unresolved for the rest of the walk: every later record of that turn is skipped without counting an attempt, so one turn's writes still reach the server in their original order (FR-MCP-SESSIONTURN-001-AC008), while records of other turns drain.
  - Quarantine, each with its `.reason.txt`: a malformed record (not readable YAML, not a mapping, no method or no params, as today), a record whose `drainAttempts` reached the budget of 5, a legacy record without a captured binding that names no request (it relied on `current-turn.yaml`), a write bound to a pending id whose mint record is quarantined, and an `unsupported_session` record whose turn is no longer in progress on the server (never forced onto that turn). A legacy record whose parameters name its session and request replays its method and parameters as today.
  - Not drained, no attempt counted: an `unsupported_session` record of a host session that is still live with `unsupportedSince` (FR-MCP-SESSIONTURN-005-AC001, section 3.1). It drains only after that host session has ended or a SessionStart cleared its cause (a post-compact or other SessionStart of the live session keeps it), and only while its turn is still in progress.
  - Repair, as today: the drain repairs each record it reads before the budget check (`Repair-ReplFailsafeQueuedRecord`, `:997-1025`, called at `:1115`: numeric-keyed `turns` become a list, and a turn without `planFile` or `todoId` gets the `None` sentinels), and a repaired record restarts at zero attempts (`:1133-1135`), so an exhausted numeric-keyed legacy record is repaired and replayed even when `drainAttempts` is exhausted (FR-MCP-SESSIONLIFE-004, `SessionLogQuarantineRepair.Tests.ps1:20`). Nothing replays from the quarantine subdirectory: at `a8d3934b` only the counts read it (`mcp-status.ps1:85-88`, `repl-invoke.ps1:2864-2872`).
  - The Stop gate warns, never blocks, while the quarantine holds a record of an open turn, and names the prompt of a mint refused for a final reason (process EvaluateStopGate).
- Requirements reconciled: the drain keeps develop's contract (oldest first, deletion only after a confirmed result, a refused record never blocks the records of other turns, quarantine with `.reason.txt` at the budget of 5, retryable answers abort without consuming attempts, repair of exhausted records). FR-MCP-REPL-011, TR-MCP-REPL-016, TR-MCP-REPL-017, FR-MCP-FAILSAFE-001, and FR-MCP-SESSIONLIFE-004 are updated only for the per-turn skip, the captured binding and the legacy rule, the three added quarantine reasons, 409 `conflict` with `retryable: true` in the retryable set, the per-host-session triggers, and the removed first-success trigger.
- Output: minted ids journaled as aliases through `Add-McpAlias` (section 3.5.6), and writes replayed onto their own turns through SendBoundWrite under the journaled alias, the bound id, or the original pair (trusted recovery).
- Delivery once (section 3.1): a prompt-hook mint record whose request was never delivered locally (the hook was killed before its replace) is delivered once when the drain mints it (the same nonce returns the stored turn): it is added to openDelivered and the ledger, and becomes the write target only when no later request was delivered in that host session; a merged terminal turn is never delivered.

![Process DrainFailsafe: drain the failsafe (activity)](c4/rendered/DrainFailsafe.svg)

View: DrainFailsafe

##### Nested activity: DrainOneFailsafeRecord (loop body of DrainFailsafe)

The read repairs a legacy record's shape as today (`Repair-ReplFailsafeQueuedRecord`), and a repaired record restarts at zero attempts, so an exhausted numeric-keyed legacy record passes the budget check and replays; nothing is read back from the quarantine subdirectory.

![Nested activity: DrainOneFailsafeRecord (loop body of DrainFailsafe) (activity), part 1 of 5](c4/rendered/DrainOneFailsafeRecord.svg)

View: DrainOneFailsafeRecord

![Nested activity: DrainOneFailsafeRecord (loop body of DrainFailsafe) (activity), part 2 of 5](c4/rendered/DrainOneFailsafeRecord-part2.svg)

View: DrainOneFailsafeRecord-part2

![Nested activity: DrainOneFailsafeRecord (loop body of DrainFailsafe) (activity), part 3 of 5](c4/rendered/DrainOneFailsafeRecord-part3.svg)

View: DrainOneFailsafeRecord-part3

![Nested activity: DrainOneFailsafeRecord (loop body of DrainFailsafe) (activity), part 4 of 5](c4/rendered/DrainOneFailsafeRecord-part4.svg)

View: DrainOneFailsafeRecord-part4

![Nested activity: DrainOneFailsafeRecord (loop body of DrainFailsafe) (activity), part 5 of 5](c4/rendered/DrainOneFailsafeRecord-part5.svg)

View: DrainOneFailsafeRecord-part5

##### Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord)

`Invoke-SessionOpen` replays the idempotent session open for the record's envelope `mcpSessionId` before every bound record. `Resolve-MintOutcome` classifies a drained mint as a stored retry, which never fails the host session closed; its final answer counts one attempt on the mint record (section 3.5.6). A mint that returns a turn journals the alias `pending-<nonce>` to the minted `requestId` through `Add-McpAlias` (section 3.5.6), rewrites the records bound to that alias, delivers once a prompt-hook request in progress that was never delivered locally, and removes the mint record. An `unsupported_session` write is checked against its turn's server status, read by its original pair, before it is sent.

![Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 1 of 7](c4/rendered/ReplayBoundRecord.svg)

View: ReplayBoundRecord

![Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 2 of 7](c4/rendered/ReplayBoundRecord-part2.svg)

View: ReplayBoundRecord-part2

![Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 3 of 7](c4/rendered/ReplayBoundRecord-part3.svg)

View: ReplayBoundRecord-part3

![Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 4 of 7](c4/rendered/ReplayBoundRecord-part4.svg)

View: ReplayBoundRecord-part4

![Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 5 of 7](c4/rendered/ReplayBoundRecord-part5.svg)

View: ReplayBoundRecord-part5

![Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 6 of 7](c4/rendered/ReplayBoundRecord-part6.svg)

View: ReplayBoundRecord-part6

![Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord) (activity), part 7 of 7](c4/rendered/ReplayBoundRecord-part7.svg)

View: ReplayBoundRecord-part7

#### Process EvaluateStopGate: evaluate the Stop gate [FR-MCP-SESSIONTURN-005]

- Trigger: every host Stop (Claude Code `stop_hook_active`, Grok `stopHookActive`; on cline-v2 the stop-equivalent `afterRun`; on opencode the `event` hook, whose adapter forwards only an event of type `session.idle`, its `{ event }` input unchanged as `-HostEvent event`, and starts no `pwsh` for any other type (`OpenCodeEventHookOtherEventTypeNotForwardedNoPwshStart`); the PowerShell core routes it to this gate, and any other type that reached the core would be an exit-0 no-op).
- Output: `{}` (pass) or a block whose text is `session_turn_stop_gate: <n> turn(s) are not closed correctly:` followed by one line per turn `<id> (<code>): <remedy>` and `The stop gate never closes turns for you.`; warnings in either case. Every remedy comes from `Get-CloseRemedy` in the fixed order of section 3.4. The gate never completes or fails a turn. A Stop payload without the host row's session key refuses and writes nothing (section 3.1).
- Blocks: `turn_in_progress`, `turn_withdrawn_unclosed`, `session_turn_completion_gate`, the reduced unsupported-session remedy, and the existing failed-build and stale-session blocks (FR-MCP-SESSIONTURN-005-AC007, section 3.4); the failed-build block reads `lastBuildStatus` from this host session's `turns/<requestId>.yaml` files, where code-verify writes it (section 4.1).
- A turn the server reports terminal from any surface leaves the open list (this replaces the "authoritative Completed proof" of FR-MCP-SESSIONLIFE-005 ac2).
- Warnings: exactly the FR-MCP-SESSIONTURN-005-AC005 list, each drawn in one of the two diagrams below. Per turn: model-canceled turns; a turn closed without model evidence by another surface; degraded persistence (a model close, or the plugin's `canceled` close of a package failure, held by an outage, naming that failure); a withdrawn close held by an outage; a captured unsupported-session close (naming the `unsupportedSince` reason and the captured records); and queued undelivered requests. Per host session: each `unsupportedSince` reason (`queue_removal_ambiguous`, `indistinguishable_arrival`, `duplicate_prompt_hook`, `arrival_bridge_format_unrecognized`, or any other) naming the open turns of its fail-closed host session and their captured records; retained failsafe records (a mint refused for a final reason, queued or quarantined, and an `open-failed` queue entry, each naming the prompt; every quarantined record of an open turn with its reason, process DrainFailsafe); `duplicate_delivery_suspected`; `nonce_source_missing` and `turn-open-contended` naming the prompt (section 3.1); `arrival_bridge_format_unrecognized`; an earlier `stop_gate_overridden_by_host`; a live bridge host session whose arrival bridge is not running; and a turn in progress in the legacy `current-turn.yaml`. Open turns of ended host sessions are not a Stop warning, because a Stop never blocks or warns on another host session's turns (FR-MCP-SESSIONTURN-005-AC001); the next SessionStart lists them with their pairs (process StartHostSession, FR-MCP-SESSIONTURN-005-AC006).

![Process EvaluateStopGate: evaluate the Stop gate (activity), part 1 of 4](c4/rendered/EvaluateStopGate.svg)

View: EvaluateStopGate

![Process EvaluateStopGate: evaluate the Stop gate (activity), part 2 of 4](c4/rendered/EvaluateStopGate-part2.svg)

View: EvaluateStopGate-part2

![Process EvaluateStopGate: evaluate the Stop gate (activity), part 3 of 4](c4/rendered/EvaluateStopGate-part3.svg)

View: EvaluateStopGate-part3

![Process EvaluateStopGate: evaluate the Stop gate (activity), part 4 of 4](c4/rendered/EvaluateStopGate-part4.svg)

View: EvaluateStopGate-part4

##### Nested activity: EvaluateTurnAtStop (loop body of EvaluateStopGate)

Each terminal below records one finding for the turn, and the loop continues with the next turn. A turn the server reports terminal leaves the open list and becomes eligible to clear; a `canceled` or `cancelled` turn is warned as model-canceled (a plugin `canceled` close names the package failure), and another terminal turn on which `Test-ModelEvidence` finds no model evidence is warned as closed without model evidence by another surface. A turn already open when this host session's `unsupportedSince` was recorded (a delivered open request or a withdrawn entry) is warned with the `unsupportedSince` reason when its close is captured in the failsafe (drained at the SessionStart after the host session ends or its cause is cleared, where the server gate judges it, FR-MCP-SESSIONTURN-005-AC001, AC003), and otherwise blocked with the reduced remedy of `Get-CloseRemedy` (missing model evidence and the close call, from S8 also `requestClass`, never the reviews).

![Nested activity: EvaluateTurnAtStop (loop body of EvaluateStopGate) (activity), part 1 of 3](c4/rendered/EvaluateTurnAtStop.svg)

View: EvaluateTurnAtStop

![Nested activity: EvaluateTurnAtStop (loop body of EvaluateStopGate) (activity), part 2 of 3](c4/rendered/EvaluateTurnAtStop-part2.svg)

View: EvaluateTurnAtStop-part2

![Nested activity: EvaluateTurnAtStop (loop body of EvaluateStopGate) (activity), part 3 of 3](c4/rendered/EvaluateTurnAtStop-part3.svg)

View: EvaluateTurnAtStop-part3

##### Nested activity: EvaluateOpenTurnAtStop (open turn step of EvaluateTurnAtStop)

Every block's remedy comes from `Get-CloseRemedy`: for a completion-gate refusal the server's missing items come first, for a withdrawn turn the remedy is `failTurn` "withdrawn before delivery", and for a delivered turn it is the accumulated remedy. A close held in the failsafe by an outage (the model's close, or the plugin's `canceled` close of a package failure, which the warning names) is warned as degraded persistence, and the ledger entry is kept until the drain confirms the close. A request queued locally but in progress on the server is warned with its `requestId` and enqueue time.

![Nested activity: EvaluateOpenTurnAtStop (open turn step of EvaluateTurnAtStop) (activity), part 1 of 4](c4/rendered/EvaluateOpenTurnAtStop.svg)

View: EvaluateOpenTurnAtStop

![Nested activity: EvaluateOpenTurnAtStop (open turn step of EvaluateTurnAtStop) (activity), part 2 of 4](c4/rendered/EvaluateOpenTurnAtStop-part2.svg)

View: EvaluateOpenTurnAtStop-part2

![Nested activity: EvaluateOpenTurnAtStop (open turn step of EvaluateTurnAtStop) (activity), part 3 of 4](c4/rendered/EvaluateOpenTurnAtStop-part3.svg)

View: EvaluateOpenTurnAtStop-part3

![Nested activity: EvaluateOpenTurnAtStop (open turn step of EvaluateTurnAtStop) (activity), part 4 of 4](c4/rendered/EvaluateOpenTurnAtStop-part4.svg)

View: EvaluateOpenTurnAtStop-part4

#### Process ValidateHookRegistration: validate and repair hook registration [FR-MCP-SESSIONTURN-001]

- Surfaces: `validate-claude-mcp-hooks.ps1` (check or repair) and `install-claude-mcp-hooks.ps1` in the Claude plugin repo.
- Output JSON: the validator's existing result plus the new `duplicate` and `repaired` statuses and the fields `duplicateHooks`, `pluginEnabled`, and `pluginKey`. The installer adds no bridge hook for an event the enabled plugin already covers, and removes managed duplicates with a backup; when the backup cannot be written, no settings file changes and the validator reports the failure.

![Process ValidateHookRegistration: validate and repair hook registration (activity), part 1 of 3](c4/rendered/ValidateHookRegistration.svg)

View: ValidateHookRegistration

![Process ValidateHookRegistration: validate and repair hook registration (activity), part 2 of 3](c4/rendered/ValidateHookRegistration-part2.svg)

View: ValidateHookRegistration-part2

![Process ValidateHookRegistration: validate and repair hook registration (activity), part 3 of 3](c4/rendered/ValidateHookRegistration-part3.svg)

View: ValidateHookRegistration-part3

#### Process StartHostSession: start a host session [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-007]

- Trigger: SessionStart (its startup, resume, clear, and compact sources); on claude-code, the PostCompact hook, which today routes to the session start and mints a new MCP session id (`plugins/core/lib-ps/plugin-hook.ps1:1525`, `repl-invoke.ps1:1954-1956`) and here runs this process for the same host session key; and a prompt hook whose host session has no recorded `mcpSessionId`, which runs it from its own payload in place of develop's no-session recovery (`Invoke-PluginNoSessionRecovery`, `plugin-hook.ps1:541`; section 3.1, Prompt without a recorded session). A payload without the host row's session key refuses and writes nothing (section 3.1).
- Trust and header (section 3.1, Host session start state): develop's trust bootstrap runs first (`Invoke-FullBootstrap` with the marker snapshot, `plugin-hook.ps1:286-308`), and an unverified marker records `MCP_UNTRUSTED` in the workspace `session-state.yaml` and ends the process before the session open, with no server call; develop's agent runtime header capture follows unchanged (`:314-326`, TR-MCP-PLUGIN-HEADER-001). A new `mcpSessionId` comes from `New-PluginSessionId` (`:47-56`): the `MCP_SESSION_ID` override when set, else `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`.
- Compaction and resume (section 3.1; FR-MCP-SESSIONLIFE-002: an already cached session identifier is never rewritten): a host session key that already has a recorded `mcpSessionId` keeps it, and a new one is recorded only when none is, so a later SessionStart, a post-compact start, or a resumed key opens no new MCP session for that key; its open turns stay bound to it and close in their original MCP session, and no `unsupported_session` record of that still-live host session is drained (process DrainFailsafe). This replaces the session-rotation rebind of TR-MCP-PLUGIN-012 and TEST-MCP-PLUGIN-012, FR-MCP-XAGENT-001's same-agent rotation rebind, and FR-MCP-TRIAGEPLUGIN-001's "completeTurn survives sessionId rebind". A SessionStart for a key whose `ended.yaml` says `session_end` (Claude Code resume and `codex exec resume` reuse the key) removes the marker in one atomic replace under `turn-open.lock`, as the idle revival does, and continues in the recorded MCP session.
- Locking: each state change is its own `Update-TurnState` change; no lock is held across the session open, `Sync-PluginArrivalLog` (which takes the lock itself), the drain's network calls, or the Mode read.
- Mode (from Phase C; section 3.1, "Mode changes"; section 3.4): the process reads the Mode with GetAgentContextStatusQuery and records it as `agentContextMode` in `turn-state.yaml` through its own `Update-TurnState` change; a Mode it cannot read (an outage, an error, or a read that misses the SessionStart hook bound) or a value it does not know is recorded as `Off`. It is the plugin's only agent-context read at host session start (section 6.6), and RunStatelessInvocation reads the cached value at each prompt. On a host without a SessionStart event the first-ingress writer makes the same read at the first prompt (process OpenTurnFromPromptHook).
- Output: the session open, the bridge, the drain, the Mode cache (from Phase C), and the open turns listed with their pairs: this host session's, the open turns of this agent's ended host sessions in the workspace (FR-MCP-SESSIONTURN-005-AC006), and a turn in progress in the legacy `current-turn.yaml` (addressable by explicit requestId). The listing is SessionStart `hookSpecificOutput`; a PostCompact run lists nothing and emits no `additionalContext` (FR-MCP-115, TR-MCP-PLUGIN-009, TEST-MCP-155 kept).

![Process StartHostSession: start a host session (activity), part 1 of 7](c4/rendered/StartHostSession.svg)

View: StartHostSession

![Process StartHostSession: start a host session (activity), part 2 of 7](c4/rendered/StartHostSession-part2.svg)

View: StartHostSession-part2

![Process StartHostSession: start a host session (activity), part 3 of 7](c4/rendered/StartHostSession-part3.svg)

View: StartHostSession-part3

![Process StartHostSession: start a host session (activity), part 4 of 7](c4/rendered/StartHostSession-part4.svg)

View: StartHostSession-part4

![Process StartHostSession: start a host session (activity), part 5 of 7](c4/rendered/StartHostSession-part5.svg)

View: StartHostSession-part5

![Process StartHostSession: start a host session (activity), part 6 of 7](c4/rendered/StartHostSession-part6.svg)

View: StartHostSession-part6

![Process StartHostSession: start a host session (activity), part 7 of 7](c4/rendered/StartHostSession-part7.svg)

View: StartHostSession-part7

#### Process EndHostSession: end a host session [FR-MCP-SESSIONTURN-007]

![Process EndHostSession: end a host session (activity)](c4/rendered/EndHostSession.svg)

View: EndHostSession

#### Process SyncAgentPluginsAtVersion: sync the agent plugins at a fixed version [TR-MCP-NUKE-003 under FR-MCP-BUILD-001]

- Surface: `./build.ps1 SyncAgentPlugins --agent-plugin-parent <dir> --plugin-version <x.y.z>` (Nuke; the parameter is new, develop computes the next minor version, `build/Build.SyncAgentPlugins.cs:63` at `a8d3934b`). This plan syncs once at `--plugin-version 1.120.0`, after the named prerequisite BUG-BUILD-008 has landed at 1.119.0 (sections 8.4 and 8.5).
- Checks at the sync: each repo the sync writes passes the packaged-path check, `ValidatePluginPowerShellOnlyPackage` over `lib/`, `hooks/`, `skills/`, `plugin.json`, and `CORE-MANIFEST.yaml` (`build/Build.SyncAgentPlugins.cs:244-245,405-433` at `a8d3934b`), which all nine pass. The tracked-tree check (`./build.ps1 ValidatePluginPowerShellOnly --agent-plugin-parent <dir>`) is not a step of the sync: it runs at HostPluginAdoption's exit over the tracked trees of all nine repos (sections 8.4 and 8.5), because the three TypeScript hosts keep their TypeScript logic, Jest suites, and vendored Node core until HostPluginAdoption's change removes them.
- Output: a receipt listing exactly nine repos with version and `lib/plugin-hook.ps1` SHA-256; the sync fails on any other count or a mismatch. The sync ends by refreshing the installed host caches of the synced version (`RefreshKnownPluginCaches`, `build/Build.SyncAgentPlugins.cs:71,652-662`), so it runs only at rollout and as a same-version re-sync, never in a gate (section 8.4).
- Tracked-tree check, at HostPluginAdoption's exit (TR-MCP-NUKE-003 AC-005): an extension rule over every tracked file (`git ls-files`) that allows on a TypeScript host only the adapter entry file `src/index.ts`, `tsconfig.json`, `package.json`, and the npm-owned lockfile, plus, in the git-ignored `dist/`, only the compiler output of that entry file (`index.js`, `index.d.ts`, and their `.map` files) from the host's existing `npm run build` (`tsc`); the git-ignored `node_modules/` holds installed dependencies, not plugin code, and is not scanned; any other bats, bash, Node, TypeScript, or Python file fails the run. Its content rule covers only the packaged paths above (section 8.4).

![Process SyncAgentPluginsAtVersion: sync the agent plugins at a fixed version (activity), part 1 of 3](c4/rendered/SyncAgentPluginsAtVersion.svg)

View: SyncAgentPluginsAtVersion

![Process SyncAgentPluginsAtVersion: sync the agent plugins at a fixed version (activity), part 2 of 3](c4/rendered/SyncAgentPluginsAtVersion-part2.svg)

View: SyncAgentPluginsAtVersion-part2

![Process SyncAgentPluginsAtVersion: sync the agent plugins at a fixed version (activity), part 3 of 3](c4/rendered/SyncAgentPluginsAtVersion-part3.svg)

View: SyncAgentPluginsAtVersion-part3

##### Nested activity: SyncOneAgentPlugin (loop body of SyncAgentPluginsAtVersion)

![Nested activity: SyncOneAgentPlugin (loop body of SyncAgentPluginsAtVersion) (activity), part 1 of 4](c4/rendered/SyncOneAgentPlugin.svg)

View: SyncOneAgentPlugin

![Nested activity: SyncOneAgentPlugin (loop body of SyncAgentPluginsAtVersion) (activity), part 2 of 4](c4/rendered/SyncOneAgentPlugin-part2.svg)

View: SyncOneAgentPlugin-part2

![Nested activity: SyncOneAgentPlugin (loop body of SyncAgentPluginsAtVersion) (activity), part 3 of 4](c4/rendered/SyncOneAgentPlugin-part3.svg)

View: SyncOneAgentPlugin-part3

![Nested activity: SyncOneAgentPlugin (loop body of SyncAgentPluginsAtVersion) (activity), part 4 of 4](c4/rendered/SyncOneAgentPlugin-part4.svg)

View: SyncOneAgentPlugin-part4

#### Process RunArrivalBridge: arrival bridge lifecycle (claude-code, claude-cowork) [FR-MCP-SESSIONTURN-001]

- Trigger: process StartHostSession on a bridge host, which starts the bridge's own script entry in `plugins/core/lib-ps` (it dot-sources `repl-invoke.ps1` for the section 3.5.6 functions). A bridge whose running version differs from the active plugin cache is treated as a stale pid: it is stopped and the bridge is restarted from the active cache with `appliedThroughOffset` kept (section 3.1, FR-MCP-TRIAGEPLUGIN-001).
- Stops when `ended.yaml` with `reason: session_end` is written; an idle end (`reason: idle`) does not stop it, so a message enqueued after an idle end is still minted at enqueue.

![Process RunArrivalBridge: arrival bridge lifecycle (claude-code, claude-cowork) (activity), part 1 of 3](c4/rendered/RunArrivalBridge.svg)

View: RunArrivalBridge

![Process RunArrivalBridge: arrival bridge lifecycle (claude-code, claude-cowork) (activity), part 2 of 3](c4/rendered/RunArrivalBridge-part2.svg)

View: RunArrivalBridge-part2

![Process RunArrivalBridge: arrival bridge lifecycle (claude-code, claude-cowork) (activity), part 3 of 3](c4/rendered/RunArrivalBridge-part3.svg)

View: RunArrivalBridge-part3

##### Nested activity: TailTranscriptRecords (loop of RunArrivalBridge)

![Nested activity: TailTranscriptRecords (loop of RunArrivalBridge) (activity), part 1 of 4](c4/rendered/TailTranscriptRecords.svg)

View: TailTranscriptRecords

![Nested activity: TailTranscriptRecords (loop of RunArrivalBridge) (activity), part 2 of 4](c4/rendered/TailTranscriptRecords-part2.svg)

View: TailTranscriptRecords-part2

![Nested activity: TailTranscriptRecords (loop of RunArrivalBridge) (activity), part 3 of 4](c4/rendered/TailTranscriptRecords-part3.svg)

View: TailTranscriptRecords-part3

![Nested activity: TailTranscriptRecords (loop of RunArrivalBridge) (activity), part 4 of 4](c4/rendered/TailTranscriptRecords-part4.svg)

View: TailTranscriptRecords-part4

### 4.5 Phase A sequence diagrams

Participants carry short names so that each sequence fits a landscape page; their qualifiers are stated here and in the line above each diagram. `Plugin` is the PowerShell plugin core (`plugins/core/lib-ps`): the arrival bridge, the prompt hook, a session-log verb, or the failsafe drain. `Model` is the model that calls a plugin verb (also the HV reviewer storing its result, or the clear-session coordinator closing a turn by its original pair). `REPL` is `qbrain-ai-repl` calling `QBrainAi.Client`. `Writer` is the caller of a server unit: the typed client, the REPL, an MCP caller, or a plugin, REPL-workflow, or McpAgent turn verb, as each sequence names. `Controller` is `SessionLogController` on REST, or the unit's MCP tool in `FwhMcpTools.SessionLog` where one exists. `Handler` is `IDispatcher` with the unit's handler, which delegates to `SessionLogService` (`ISessionLogService`). `Store` is the session-log database (`McpDbContext`).

Status codes are REST. An MCP tool returns the same result fields without a status, and every failure as the classified error object `{ code, error, message, retryable, details }` with the same `code`: each handler fails with `ClassifiedErrorException`, which `McpErrorClassifier` maps for REST and MCP alike (section 3.5.2); REST bodies that develop answers as problem details keep today's text.

Shared components are one message each (section 3.5): `TurnLocator`, `TurnWritePipeline`, `SerializableWrite`, and `IdempotentCreate` on the server; `Resolve-MintOutcome`, `Invoke-SessionOpen`, `Set-SessionUnsupported`, `Update-TurnState`, `Test-ModelEvidence`, `Get-CloseRemedy`, and the alias journal (`Add-McpAlias`) in the plugin. The processes BindVerbToTurn and PersistTurnWriteWithReceipt (section 4.4) are reference notes followed by their result branches; the server branches behind a plugin write are those of the sequence of the unit it dispatches. A server branch is labelled with the typed result of the component that produced it (for example `SessionNotFound` from TurnLocator) and the body the unit maps it to.

Every dispatch runs WorkspaceResolutionBehavior after `SendAsync` and before the handler (section 3.5.1), drawn as a note after the dispatch that names its refusal, `WorkspaceResolutionBehavior (400)`. On REST, `WorkspaceResolutionMiddleware` has already answered a missing or unknown `X-Workspace-Path` before the controller with today's body (TR-MCP-MT-002); a `WorkspacePath` that names no registered workspace inside the dispatch (an MCP `workspacePath` or the REPL session workspace) is refused with 400 `validation_error` and `details.field` `workspacePath`, the status TR-MCP-MT-002 gives REST. Request validation runs in the handler.

Response rules every sequence uses:
- The completion-gate refusal is 400 with `code` `validation_error` and a message that starts with the token `session_turn_completion_gate` and lists the missing items in the order requestClass, agentReview, hostileReview (section 3.4). The gate runs on CompletionGate's trigger (section 5.3): a write that moves the turn into `completed` or `failed`, or a Replace (PUT) that carries `completed` or `failed` over any stored status, an already terminal turn included, because a whole replacement is not enrichment; never on an additive write that only enriches a turn that is already terminal.
- Agent surfaces write the statuses `in_progress`, `completed`, `failed`, `canceled`, and the legacy `cancelled`; a write naming `closed` or any other value is refused with 400 `validation_error`, `details.field` `status` (`TurnStatus`, section 3.5.3). QBAgent's internal tools, which call `UpsertTurnAsync` without a dispatch, are refused the same way (section 4.2, UpsertSessionTurnCommand).
- The mint (an idempotency key) and every turn write that evaluates the terminal-state guard or the completion gate (submit, upsert, patch, replace, complete, fail) run their checks and their save as one database-only SerializableWrite (sections 3.4 and 3.5.2; turn-level isolation for the mint and the single-turn writes, section 3.1 "Mint isolation", and session-level for the submission); after four attempts the unit answers 409 `conflict` with `retryable: true` and saves nothing (the minter's own bound covers only request-id collisions: six attempts, then 500 `mint_collision_exhausted`; its transient conflicts keep the four attempts and the 409). The dialog append and the two title writes keep today's single save, and a busy store there keeps today's 503 `persistence_error` with `retryable: true` (`McpErrorClassifier.cs:129-139` at `a8d3934b`).
- In the plugin (C9 and section 3.1, "Deadlock and busy outcomes"): a transport outage, a retryable 503 (`backend_unavailable`, or a busy store's `persistence_error`), and 409 `conflict` with `retryable: true` are one outage case: a live verb keeps its failsafe record keyed by the bound requestId and reports `persisted: false`, code `queued`, exit 0 (FR-MCP-SESSIONLIFE-003, FR-MCP-REPL-009), and the failsafe drain stops its walk on such an answer without counting an attempt. On a final answer a live verb clears its record, while the drain keeps it and counts the attempt (process DrainFailsafe).
- The plugin functions of section 3.5.6 (`Resolve-MintOutcome`, `Invoke-SessionOpen`, `Set-SessionUnsupported`, `Update-TurnState`, `Test-ModelEvidence`, `Get-CloseRemedy`, and the alias journal's `Add-McpAlias` and consumer functions) live in `plugins/core/lib-ps/repl-invoke.ps1`; the prompt hook (`plugin-hook.ps1`) and the arrival bridge's own script entry dot-source it.

#### Sequence: submit MintSessionTurnCommand and process each response

`Plugin` is the arrival bridge (claude-code, claude-cowork, where it is the only minter) or the prompt hook (every other supported host, the TypeScript hosts through their adapters). Only hooks and adapters reach this path: the plugin wrapper refuses a model-originated `mintTurn` with `mint_requires_host_ingress` before any server call, and the hook calls `workflow.sessionlog.mintTurn` with `MCP_PLUGIN_HOOK_INVOCATION=1`. A system event never mints (bridge queue entry kind `system`, hook status `system-event-skipped`). Before the call, the prompt hook runs the nested sequence PrepareHookMint under `turn-open.lock`; the bridge mints straight from its `enqueue` record. The mint maps TurnLocator's `SessionNotFound` to 404 with detail code `session_not_open` and its `InvalidIdentifier` to 400 `validation_error`; IdempotentCreate inserts through InsertMintedTurnWithRetry (`ReadableKeyAllocator`, strategy `RandomHex`, six attempts, then 500 `mint_collision_exhausted`); a transient conflict uses SerializableWrite's four attempts, then 409 `conflict` with `retryable: true`. After the answer, `Resolve-MintOutcome` classifies it and the nested sequence ApplyMintOutcome applies it in the open's `Update-TurnState` replace, which releases `turn-open.lock`.

![Sequence: submit MintSessionTurnCommand and process each response, part 1 of 7](c4/rendered/STR-04-02.svg)

View: STR-04-02

![Sequence: submit MintSessionTurnCommand and process each response, part 2 of 7](c4/rendered/STR-04-02-part2.svg)

View: STR-04-02-part2

![Sequence: submit MintSessionTurnCommand and process each response, part 3 of 7](c4/rendered/STR-04-02-part3.svg)

View: STR-04-02-part3

![Sequence: submit MintSessionTurnCommand and process each response, part 4 of 7](c4/rendered/STR-04-02-part4.svg)

View: STR-04-02-part4

![Sequence: submit MintSessionTurnCommand and process each response, part 5 of 7](c4/rendered/STR-04-02-part5.svg)

View: STR-04-02-part5

![Sequence: submit MintSessionTurnCommand and process each response, part 6 of 7](c4/rendered/STR-04-02-part6.svg)

View: STR-04-02-part6

![Sequence: submit MintSessionTurnCommand and process each response, part 7 of 7](c4/rendered/STR-04-02-part7.svg)

View: STR-04-02-part7

![Sequence: submit MintSessionTurnCommand and process each response, alt 1 branch 1 of 6: created](c4/rendered/STR-04-02-alt1-1.svg)

View: STR-04-02-alt1-1

![Sequence: submit MintSessionTurnCommand and process each response, alt 1 branch 2 of 6: nonce already minted in this session](c4/rendered/STR-04-02-alt1-2.svg)

View: STR-04-02-alt1-2

![Sequence: submit MintSessionTurnCommand and process each response, alt 1 branch 3 of 6: nonce, session id, planFile, or todoId invalid](c4/rendered/STR-04-02-alt1-3.svg)

View: STR-04-02-alt1-3

![Sequence: submit MintSessionTurnCommand and process each response, alt 1 branch 4 of 6: SessionNotFound](c4/rendered/STR-04-02-alt1-4.svg)

View: STR-04-02-alt1-4

![Sequence: submit MintSessionTurnCommand and process each response, alt 1 branch 5 of 6: four attempts used by a transient conflict](c4/rendered/STR-04-02-alt1-5.svg)

View: STR-04-02-alt1-5

![Sequence: submit MintSessionTurnCommand and process each response, alt 1 branch 6 of 6: six request-id attempts used](c4/rendered/STR-04-02-alt1-6.svg)

View: STR-04-02-alt1-6

Resolve-MintOutcome (section 3.5.6) classifies the answer. Outage: a transport outage, a retryable answer (a retryable 503, or 409 `conflict` with `retryable: true`), the scoped mint 404 or `unknown_method` of C9 (an older server without the mint route), or `session_not_open` while the session open for this envelope is not confirmed. Final: `ingress_nonce_invalid`, `validation_error`, `mint_collision_exhausted`, or `session_not_open` after a confirmed open. On a first ingress, an outage of `Invoke-SessionOpen` continues as a mint outage (failsafe records in order: the session open, then the mint), and its final answer (invalid session identifier) ends the open as `turn-open-failed`. A prompt hook killed after its write-ahead mint record ends at the next hook, verb, or SessionStart drain with exactly one server turn for that message, delivered once (section 3.1).

##### Nested sequence: PrepareHookMint (prompt hook steps before the mint call)

The prompt hook takes `turn-open.lock` once through `Update-TurnState` and never waits. A contended lock and a missing nonce field each write a failsafe diagnostic naming the prompt and mint nothing; the Stop gate warns `turn-open-contended` and `nonce_source_missing` naming the prompt. On copilot and classic cline, a new invocation whose nonce already has a failsafe mint record or a pending turn fails the host session closed. Otherwise the hook writes its write-ahead failsafe mint record under `pending-<nonce>` and, on the first ingress of a host without a SessionStart event, opens the host session's new `mcpSessionId` (sequence OpenSessionLogCommand).

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), part 1 of 2](c4/rendered/PrepareHookMint.svg)

View: PrepareHookMint

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), part 2 of 2](c4/rendered/PrepareHookMint-part2.svg)

View: PrepareHookMint-part2

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 1 of 4: lock contended, part 1 of 2](c4/rendered/PrepareHookMint-alt1-1.svg)

View: PrepareHookMint-alt1-1

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 1 of 4: lock contended, part 2 of 2](c4/rendered/PrepareHookMint-alt1-1-part2.svg)

View: PrepareHookMint-alt1-1-part2

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 2 of 4: host nonce field missing, part 1 of 2](c4/rendered/PrepareHookMint-alt1-2.svg)

View: PrepareHookMint-alt1-2

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 2 of 4: host nonce field missing, part 2 of 2](c4/rendered/PrepareHookMint-alt1-2-part2.svg)

View: PrepareHookMint-alt1-2-part2

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 3 of 4: equal-field host, nonce already recorded, part 1 of 3](c4/rendered/PrepareHookMint-alt1-3.svg)

View: PrepareHookMint-alt1-3

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 3 of 4: equal-field host, nonce already recorded, part 2 of 3](c4/rendered/PrepareHookMint-alt1-3-part2.svg)

View: PrepareHookMint-alt1-3-part2

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 3 of 4: equal-field host, nonce already recorded, part 3 of 3](c4/rendered/PrepareHookMint-alt1-3-part3.svg)

View: PrepareHookMint-alt1-3-part3

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 4 of 4: nonce derived, part 1 of 5](c4/rendered/PrepareHookMint-alt1-4.svg)

View: PrepareHookMint-alt1-4

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 4 of 4: nonce derived, part 2 of 5](c4/rendered/PrepareHookMint-alt1-4-part2.svg)

View: PrepareHookMint-alt1-4-part2

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 4 of 4: nonce derived, part 3 of 5](c4/rendered/PrepareHookMint-alt1-4-part3.svg)

View: PrepareHookMint-alt1-4-part3

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 4 of 4: nonce derived, part 4 of 5](c4/rendered/PrepareHookMint-alt1-4-part4.svg)

View: PrepareHookMint-alt1-4-part4

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), alt 1 branch 4 of 4: nonce derived, part 5 of 5](c4/rendered/PrepareHookMint-alt1-4-part5.svg)

View: PrepareHookMint-alt1-4-part5

![Nested sequence: PrepareHookMint (prompt hook steps before the mint call), opt 1: first ingress on a host without a SessionStart event](c4/rendered/PrepareHookMint-opt1.svg)

View: PrepareHookMint-opt1

##### Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome)

`Update-TurnState` applies the outcome in the open's one atomic replace and releases `turn-open.lock`. On the bridge each outcome becomes a queue entry that keeps its position; on the prompt hook a created turn is delivered (write target, `openDelivered`, ledger). A merged terminal turn reports its `storedStatus` and is never the write target, in the open list, or in the ledger; its bridge entry's `dequeue` delivers nothing. On an outage the failsafe mint record is kept for process DrainFailsafe and the request is open locally under `pending-<nonce>`. A final answer leaves no turn: the bridge queues an `open-failed` entry that keeps its position and is never minted again by the bridge, the prompt hook keeps its mint record with that answer counted as its first attempt (`drainAttempts` 1), each drained mint answered finally counts one more (`Resolve-MintOutcome`, section 3.5.6) until the budget of 5 quarantines it, and the Stop gate warns naming the prompt.

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome)](c4/rendered/ApplyMintOutcome.svg)

View: ApplyMintOutcome

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 1 of 6: created, part 1 of 2](c4/rendered/ApplyMintOutcome-alt1-1.svg)

View: ApplyMintOutcome-alt1-1

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 1 of 6: created, part 2 of 2](c4/rendered/ApplyMintOutcome-alt1-1-part2.svg)

View: ApplyMintOutcome-alt1-1-part2

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 2 of 6: merged in progress, part 1 of 2](c4/rendered/ApplyMintOutcome-alt1-2.svg)

View: ApplyMintOutcome-alt1-2

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 2 of 6: merged in progress, part 2 of 2](c4/rendered/ApplyMintOutcome-alt1-2-part2.svg)

View: ApplyMintOutcome-alt1-2-part2

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 3 of 6: unsupported (copilot or classic cline, new invocation merged), part 1 of 3](c4/rendered/ApplyMintOutcome-alt1-3.svg)

View: ApplyMintOutcome-alt1-3

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 3 of 6: unsupported (copilot or classic cline, new invocation merged), part 2 of 3](c4/rendered/ApplyMintOutcome-alt1-3-part2.svg)

View: ApplyMintOutcome-alt1-3-part2

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 3 of 6: unsupported (copilot or classic cline, new invocation merged), part 3 of 3](c4/rendered/ApplyMintOutcome-alt1-3-part3.svg)

View: ApplyMintOutcome-alt1-3-part3

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 4 of 6: merged terminal, part 1 of 2](c4/rendered/ApplyMintOutcome-alt1-4.svg)

View: ApplyMintOutcome-alt1-4

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 4 of 6: merged terminal, part 2 of 2](c4/rendered/ApplyMintOutcome-alt1-4-part2.svg)

View: ApplyMintOutcome-alt1-4-part2

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 5 of 6: outage, part 1 of 2](c4/rendered/ApplyMintOutcome-alt1-5.svg)

View: ApplyMintOutcome-alt1-5

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 5 of 6: outage, part 2 of 2](c4/rendered/ApplyMintOutcome-alt1-5-part2.svg)

View: ApplyMintOutcome-alt1-5-part2

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 6 of 6: final, part 1 of 2](c4/rendered/ApplyMintOutcome-alt1-6.svg)

View: ApplyMintOutcome-alt1-6

![Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome), alt 1 branch 6 of 6: final, part 2 of 2](c4/rendered/ApplyMintOutcome-alt1-6-part2.svg)

View: ApplyMintOutcome-alt1-6-part2

#### Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes

`Writer` is the typed client, the REPL, an MCP caller (`sessionlog_submit`, which returns `{ success, id, turns }`), or a plugin, REPL-workflow, or McpAgent turn verb. The per-turn evaluation is TurnWritePipeline in mode `Submit` (section 3.5.3), which applies policy TerminalTurnOutcomeGuard (section 4.3) to each turn, collects the completion gate's missing items, and rejects the submission once after every turn is evaluated; it replaces the former nested sequence of the guard.

![Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, part 1 of 4](c4/rendered/STR-04-03.svg)

View: STR-04-03

![Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, part 2 of 4](c4/rendered/STR-04-03-part2.svg)

View: STR-04-03-part2

![Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, part 3 of 4](c4/rendered/STR-04-03-part3.svg)

View: STR-04-03-part3

![Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, part 4 of 4](c4/rendered/STR-04-03-part4.svg)

View: STR-04-03-part4

![Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 1 of 5: every turn evaluated, nothing refused, part 1 of 3](c4/rendered/STR-04-03-alt1-1.svg)

View: STR-04-03-alt1-1

![Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 1 of 5: every turn evaluated, nothing refused, part 2 of 3](c4/rendered/STR-04-03-alt1-1-part2.svg)

View: STR-04-03-alt1-1-part2

![Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 1 of 5: every turn evaluated, nothing refused, part 3 of 3](c4/rendered/STR-04-03-alt1-1-part3.svg)

View: STR-04-03-alt1-1-part3

![Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 2 of 5: document invalid, a status outside the agent set, InvalidIdentifier, or InvalidPayload, part 1 of 2](c4/rendered/STR-04-03-alt1-2.svg)

View: STR-04-03-alt1-2

![Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 2 of 5: document invalid, a status outside the agent set, InvalidIdentifier, or InvalidPayload, part 2 of 2](c4/rendered/STR-04-03-alt1-2-part2.svg)

View: STR-04-03-alt1-2-part2

![Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 3 of 5: a turn's RequestClassInvalid](c4/rendered/STR-04-03-alt1-3.svg)

View: STR-04-03-alt1-3

![Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 4 of 5: CompletionGateMissing for one or more turns](c4/rendered/STR-04-03-alt1-4.svg)

View: STR-04-03-alt1-4

![Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes, alt 1 branch 5 of 5: SerializableWrite's four attempts used](c4/rendered/STR-04-03-alt1-5.svg)

View: STR-04-03-alt1-5

#### Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response

`Plugin` is the `appendActions`, `appendDecisions`, or `updateTurn` verb. Process BindVerbToTurn applies pending transcript records through `Sync-PluginArrivalLog` and captures the binding once; process PersistTurnWriteWithReceipt sends a session document holding only the bound turn through the REPL (`client.SessionLog.SubmitAsync`), whose server branches are the SubmitSessionLogCommand sequence above, and maps the answer to the receipt below.

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, part 1 of 2](c4/rendered/STR-04-04.svg)

View: STR-04-04

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, part 2 of 2](c4/rendered/STR-04-04-part2.svg)

View: STR-04-04-part2

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 1 of 3: binding refused](c4/rendered/STR-04-04-alt1-1.svg)

View: STR-04-04-alt1-1

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 2 of 3: unsupported session, target already open when it failed closed, part 1 of 2](c4/rendered/STR-04-04-alt1-2.svg)

View: STR-04-04-alt1-2

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 2 of 3: unsupported session, target already open when it failed closed, part 2 of 2](c4/rendered/STR-04-04-alt1-2-part2.svg)

View: STR-04-04-alt1-2-part2

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 1 of 5](c4/rendered/STR-04-04-alt1-3.svg)

View: STR-04-04-alt1-3

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 2 of 5](c4/rendered/STR-04-04-alt1-3-part2.svg)

View: STR-04-04-alt1-3-part2

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 3 of 5](c4/rendered/STR-04-04-alt1-3-part3.svg)

View: STR-04-04-alt1-3-part3

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 4 of 5](c4/rendered/STR-04-04-alt1-3-part4.svg)

View: STR-04-04-alt1-3-part4

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 5 of 5](c4/rendered/STR-04-04-alt1-3-part5.svg)

View: STR-04-04-alt1-3-part5

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 1 of 5: created or merged](c4/rendered/STR-04-04-alt2-1.svg)

View: STR-04-04-alt2-1

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 2 of 5: skipped_terminal](c4/rendered/STR-04-04-alt2-2.svg)

View: STR-04-04-alt2-2

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 3 of 5: 201 without per-turn outcomes](c4/rendered/STR-04-04-alt2-3.svg)

View: STR-04-04-alt2-3

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 4 of 5: 400 validation_error or request_class_invalid, or the completion gate, part 1 of 2](c4/rendered/STR-04-04-alt2-4.svg)

View: STR-04-04-alt2-4

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 4 of 5: 400 validation_error or request_class_invalid, or the completion gate, part 2 of 2](c4/rendered/STR-04-04-alt2-4-part2.svg)

View: STR-04-04-alt2-4-part2

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 5 of 5: transport outage, retryable 503, or 409 conflict retryable, part 1 of 2](c4/rendered/STR-04-04-alt2-5.svg)

View: STR-04-04-alt2-5

![Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response, alt 2 branch 5 of 5: transport outage, retryable 503, or 409 conflict retryable, part 2 of 2](c4/rendered/STR-04-04-alt2-5-part2.svg)

View: STR-04-04-alt2-5-part2

#### Sequence: submit UpsertSessionTurnCommand and process each response

`Writer` is the typed client or the REPL (`client.SessionLog.UpsertTurnAsync`, returning `SessionLogTurnSubmitResult`); there is no MCP tool. The write is TurnWritePipeline in mode `Merge` (section 3.5.3): an absent turn is inserted through IdempotentCreate with policy `MergeInto`, the one retry mechanism for a concurrent insert of the same requestId; RequestClassification applies when the write carries `requestClass` (from S4) and CompletionGate when it moves the turn into `completed` or `failed` (from S10).

![Sequence: submit UpsertSessionTurnCommand and process each response, part 1 of 2](c4/rendered/STR-04-05.svg)

View: STR-04-05

![Sequence: submit UpsertSessionTurnCommand and process each response, part 2 of 2](c4/rendered/STR-04-05-part2.svg)

View: STR-04-05-part2

![Sequence: submit UpsertSessionTurnCommand and process each response, alt 1 branch 1 of 2: no body](c4/rendered/STR-04-05-alt1-1.svg)

View: STR-04-05-alt1-1

![Sequence: submit UpsertSessionTurnCommand and process each response, alt 1 branch 2 of 2: body present, part 1 of 2](c4/rendered/STR-04-05-alt1-2.svg)

View: STR-04-05-alt1-2

![Sequence: submit UpsertSessionTurnCommand and process each response, alt 1 branch 2 of 2: body present, part 2 of 2](c4/rendered/STR-04-05-alt1-2-part2.svg)

View: STR-04-05-alt1-2-part2

![Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 1 of 6: turn written (inserted, or merged additively whatever its status), part 1 of 3](c4/rendered/STR-04-05-alt2-1.svg)

View: STR-04-05-alt2-1

![Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 1 of 6: turn written (inserted, or merged additively whatever its status), part 2 of 3](c4/rendered/STR-04-05-alt2-1-part2.svg)

View: STR-04-05-alt2-1-part2

![Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 1 of 6: turn written (inserted, or merged additively whatever its status), part 3 of 3](c4/rendered/STR-04-05-alt2-1-part3.svg)

View: STR-04-05-alt2-1-part3

![Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 2 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 1 of 2](c4/rendered/STR-04-05-alt2-2.svg)

View: STR-04-05-alt2-2

![Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 2 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 2 of 2](c4/rendered/STR-04-05-alt2-2-part2.svg)

View: STR-04-05-alt2-2-part2

![Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 3 of 6: SessionNotFound](c4/rendered/STR-04-05-alt2-3.svg)

View: STR-04-05-alt2-3

![Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 4 of 6: RequestClassInvalid](c4/rendered/STR-04-05-alt2-4.svg)

View: STR-04-05-alt2-4

![Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 5 of 6: CompletionGateMissing](c4/rendered/STR-04-05-alt2-5.svg)

View: STR-04-05-alt2-5

![Sequence: submit UpsertSessionTurnCommand and process each response, alt 2 branch 6 of 6: SerializableWrite's four attempts used](c4/rendered/STR-04-05-alt2-6.svg)

View: STR-04-05-alt2-6

#### Sequence: submit PatchSessionTurnCommand and process each response

`Writer` is the typed client or the REPL (`client.SessionLog.PatchTurnAsync`, returning `SessionLogTurnSubmitResult`); there is no MCP tool. The write is TurnWritePipeline in mode `Merge`, as for UpsertSessionTurnCommand: a missing body is an empty turn, the route `requestId` replaces any body value, and a `requestId` the session does not hold is inserted, as today.

![Sequence: submit PatchSessionTurnCommand and process each response, part 1 of 3](c4/rendered/STR-04-06.svg)

View: STR-04-06

![Sequence: submit PatchSessionTurnCommand and process each response, part 2 of 3](c4/rendered/STR-04-06-part2.svg)

View: STR-04-06-part2

![Sequence: submit PatchSessionTurnCommand and process each response, part 3 of 3](c4/rendered/STR-04-06-part3.svg)

View: STR-04-06-part3

![Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 1 of 6: turn written (inserted, or merged additively whatever its status), part 1 of 3](c4/rendered/STR-04-06-alt1-1.svg)

View: STR-04-06-alt1-1

![Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 1 of 6: turn written (inserted, or merged additively whatever its status), part 2 of 3](c4/rendered/STR-04-06-alt1-1-part2.svg)

View: STR-04-06-alt1-1-part2

![Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 1 of 6: turn written (inserted, or merged additively whatever its status), part 3 of 3](c4/rendered/STR-04-06-alt1-1-part3.svg)

View: STR-04-06-alt1-1-part3

![Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 2 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 1 of 2](c4/rendered/STR-04-06-alt1-2.svg)

View: STR-04-06-alt1-2

![Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 2 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 2 of 2](c4/rendered/STR-04-06-alt1-2-part2.svg)

View: STR-04-06-alt1-2-part2

![Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 3 of 6: SessionNotFound](c4/rendered/STR-04-06-alt1-3.svg)

View: STR-04-06-alt1-3

![Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 4 of 6: RequestClassInvalid](c4/rendered/STR-04-06-alt1-4.svg)

View: STR-04-06-alt1-4

![Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 5 of 6: CompletionGateMissing](c4/rendered/STR-04-06-alt1-5.svg)

View: STR-04-06-alt1-5

![Sequence: submit PatchSessionTurnCommand and process each response, alt 1 branch 6 of 6: SerializableWrite's four attempts used](c4/rendered/STR-04-06-alt1-6.svg)

View: STR-04-06-alt1-6

#### Sequence: submit ReplaceSessionTurnCommand and process each response

`Writer` is the typed client, the REPL (`client.SessionLog.ReplaceTurnAsync`, returning `SessionLogMutationResult`), or an MCP caller of `sessionlog_replace_turn`. The write is TurnWritePipeline in mode `Replace`: the turn must exist, and every error is a `ClassifiedError` body, as today. A replacement that carries `completed` or `failed` meets CompletionGate even when the stored turn already has that status, so a PUT of an already completed turn that omits its reviews is refused and the stored reviews stay (`ReplaceCompletedTurnOmittingReviewsRefusedCompletionGateReviewsKept`, FR-MCP-SESSIONCLASS-002-AC004). A superseded hook persist without planFile or todoId takes the `None` sentinels before the call.

![Sequence: submit ReplaceSessionTurnCommand and process each response, part 1 of 3](c4/rendered/STR-04-07.svg)

View: STR-04-07

![Sequence: submit ReplaceSessionTurnCommand and process each response, part 2 of 3](c4/rendered/STR-04-07-part2.svg)

View: STR-04-07-part2

![Sequence: submit ReplaceSessionTurnCommand and process each response, part 3 of 3](c4/rendered/STR-04-07-part3.svg)

View: STR-04-07-part3

![Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 1 of 6: replaced, part 1 of 2](c4/rendered/STR-04-07-alt1-1.svg)

View: STR-04-07-alt1-1

![Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 1 of 6: replaced, part 2 of 2](c4/rendered/STR-04-07-alt1-1-part2.svg)

View: STR-04-07-alt1-1-part2

![Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 2 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 1 of 2](c4/rendered/STR-04-07-alt1-2.svg)

View: STR-04-07-alt1-2

![Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 2 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 2 of 2](c4/rendered/STR-04-07-alt1-2-part2.svg)

View: STR-04-07-alt1-2-part2

![Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 3 of 6: SessionNotFound or TurnNotFound](c4/rendered/STR-04-07-alt1-3.svg)

View: STR-04-07-alt1-3

![Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 4 of 6: RequestClassInvalid](c4/rendered/STR-04-07-alt1-4.svg)

View: STR-04-07-alt1-4

![Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 5 of 6: CompletionGateMissing (the replacement carries completed or failed, over any stored status), part 1 of 3](c4/rendered/STR-04-07-alt1-5.svg)

View: STR-04-07-alt1-5

![Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 5 of 6: CompletionGateMissing (the replacement carries completed or failed, over any stored status), part 2 of 3](c4/rendered/STR-04-07-alt1-5-part2.svg)

View: STR-04-07-alt1-5-part2

![Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 5 of 6: CompletionGateMissing (the replacement carries completed or failed, over any stored status), part 3 of 3](c4/rendered/STR-04-07-alt1-5-part3.svg)

View: STR-04-07-alt1-5-part3

![Sequence: submit ReplaceSessionTurnCommand and process each response, alt 1 branch 6 of 6: SerializableWrite's four attempts used](c4/rendered/STR-04-07-alt1-6.svg)

View: STR-04-07-alt1-6

#### Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response

`Model` is the model, or the clear-session coordinator closing by the original pair; `Plugin` is the `completeTurn` or `failTurn` verb. Before the close, `Test-ModelEvidence` requires at least one model-authored action, designDecision, or commit, and `Get-CloseRemedy` runs the advisory pre-check against the golden completion policy (process AdvisoryCompletionPrecheck, from S8), which only warns. A close that carries evidence first writes it as an additive write without a status through the REPL, verified against the sanitized read projection (section 3.1), so a gate refusal of the close loses nothing (FR-MCP-SESSIONCLASS-004-AC003). Process PersistTurnWriteWithReceipt then sends the bound turn at `completed` or `failed` through the REPL, and its server branches are the SubmitSessionLogCommand sequence (TurnWritePipeline mode `Submit`, origin Agent); the nested sequence ReceiveCloseAnswer maps the answer. A binding refusal is one of `request_unknown`, `request_ambiguous`, `turn_immutable`, `host_session_unknown`, `host_session_ambiguous`, or `turn_logging_unsupported`. A close captured as an `unsupported_session` record is judged by the server gate at the drain after its host session ends or its cause is cleared. An evidence write answered with a final server code stops the close; an evidence write answered with a retryable answer (a transport outage, a retryable 503, or 409 `conflict` with `retryable: true`) keeps two failsafe records in order, the evidence write and then the close, and the ledger keeps the request until the drain confirms the close.

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, part 1 of 2](c4/rendered/STR-04-08.svg)

View: STR-04-08

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, part 2 of 2](c4/rendered/STR-04-08-part2.svg)

View: STR-04-08-part2

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 1 branch 1 of 3: binding refused](c4/rendered/STR-04-08-alt1-1.svg)

View: STR-04-08-alt1-1

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 1 branch 2 of 3: unsupported session, target open when it failed closed](c4/rendered/STR-04-08-alt1-2.svg)

View: STR-04-08-alt1-2

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 1 branch 3 of 3: bound, part 1 of 3](c4/rendered/STR-04-08-alt1-3.svg)

View: STR-04-08-alt1-3

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 1 branch 3 of 3: bound, part 2 of 3](c4/rendered/STR-04-08-alt1-3-part2.svg)

View: STR-04-08-alt1-3-part2

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 1 branch 3 of 3: bound, part 3 of 3](c4/rendered/STR-04-08-alt1-3-part3.svg)

View: STR-04-08-alt1-3-part3

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 1 of 2: no model-authored action, decision, or commit, part 1 of 2](c4/rendered/STR-04-08-alt2-1.svg)

View: STR-04-08-alt2-1

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 1 of 2: no model-authored action, decision, or commit, part 2 of 2](c4/rendered/STR-04-08-alt2-1-part2.svg)

View: STR-04-08-alt2-1-part2

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 2 of 2: evidence present, part 1 of 6](c4/rendered/STR-04-08-alt2-2.svg)

View: STR-04-08-alt2-2

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 2 of 2: evidence present, part 2 of 6](c4/rendered/STR-04-08-alt2-2-part2.svg)

View: STR-04-08-alt2-2-part2

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 2 of 2: evidence present, part 3 of 6](c4/rendered/STR-04-08-alt2-2-part3.svg)

View: STR-04-08-alt2-2-part3

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 2 of 2: evidence present, part 4 of 6](c4/rendered/STR-04-08-alt2-2-part4.svg)

View: STR-04-08-alt2-2-part4

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 2 of 2: evidence present, part 5 of 6](c4/rendered/STR-04-08-alt2-2-part5.svg)

View: STR-04-08-alt2-2-part5

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, alt 2 branch 2 of 2: evidence present, part 6 of 6](c4/rendered/STR-04-08-alt2-2-part6.svg)

View: STR-04-08-alt2-2-part6

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, opt 1: items missing](c4/rendered/STR-04-08-opt1.svg)

View: STR-04-08-opt1

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, opt 2: the close carries evidence, part 1 of 3](c4/rendered/STR-04-08-opt2.svg)

View: STR-04-08-opt2

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, opt 2: the close carries evidence, part 2 of 3](c4/rendered/STR-04-08-opt2-part2.svg)

View: STR-04-08-opt2-part2

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, opt 2: the close carries evidence, part 3 of 3](c4/rendered/STR-04-08-opt2-part3.svg)

View: STR-04-08-opt2-part3

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, break 1: final server code on the evidence write](c4/rendered/STR-04-08-break1.svg)

View: STR-04-08-break1

![Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response, break 2: retryable answer on the evidence write](c4/rendered/STR-04-08-break2.svg)

View: STR-04-08-break2

##### Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence)

A confirmed close removes the request from `openDelivered` and the ledger. A completion-gate refusal returns the server message and the missing items in order (`failed`: `requestClass` only), is recorded for the Stop gate, and leaves the request open and ledgered. A retryable answer keeps the close in the failsafe keyed by the bound `requestId`, and the ledger keeps the request until the drain confirms the close.

![Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence)](c4/rendered/ReceiveCloseAnswer.svg)

View: ReceiveCloseAnswer

![Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 1 of 6: created or merged](c4/rendered/ReceiveCloseAnswer-alt1-1.svg)

View: ReceiveCloseAnswer-alt1-1

![Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 2 of 6: completion gate refused](c4/rendered/ReceiveCloseAnswer-alt1-2.svg)

View: ReceiveCloseAnswer-alt1-2

![Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 3 of 6: 400 validation_error or request_class_invalid, part 1 of 2](c4/rendered/ReceiveCloseAnswer-alt1-3.svg)

View: ReceiveCloseAnswer-alt1-3

![Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 3 of 6: 400 validation_error or request_class_invalid, part 2 of 2](c4/rendered/ReceiveCloseAnswer-alt1-3-part2.svg)

View: ReceiveCloseAnswer-alt1-3-part2

![Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 4 of 6: skipped_terminal](c4/rendered/ReceiveCloseAnswer-alt1-4.svg)

View: ReceiveCloseAnswer-alt1-4

![Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 5 of 6: 201 without per-turn outcomes](c4/rendered/ReceiveCloseAnswer-alt1-5.svg)

View: ReceiveCloseAnswer-alt1-5

![Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 6 of 6: transport outage, retryable 503, or 409 conflict retryable, part 1 of 2](c4/rendered/ReceiveCloseAnswer-alt1-6.svg)

View: ReceiveCloseAnswer-alt1-6

![Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence), alt 1 branch 6 of 6: transport outage, retryable 503, or 409 conflict retryable, part 2 of 2](c4/rendered/ReceiveCloseAnswer-alt1-6-part2.svg)

View: ReceiveCloseAnswer-alt1-6-part2

#### Sequence: submit CompleteSessionTurnCommand and process each response

`Writer` is the typed client, the REPL (`client.SessionLog.CompleteTurnAsync`), or an MCP caller of `sessionlog_complete_turn`, whose `turnJson` is copied only for the fields `FinalizeLifecycleTurnToolAsync` lists plus `requestClass` (S5) and is refused before any write when malformed. The write is TurnWritePipeline in mode `Complete` (Merge with status `completed`, insert when absent, as today); CompletionGate runs only on the move into `completed`, so an already completed turn is enriched without re-gating (FR-MCP-SESSIONCLASS-002-AC004).

![Sequence: submit CompleteSessionTurnCommand and process each response, part 1 of 3](c4/rendered/STR-04-09.svg)

View: STR-04-09

![Sequence: submit CompleteSessionTurnCommand and process each response, part 2 of 3](c4/rendered/STR-04-09-part2.svg)

View: STR-04-09-part2

![Sequence: submit CompleteSessionTurnCommand and process each response, part 3 of 3](c4/rendered/STR-04-09-part3.svg)

View: STR-04-09-part3

![Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 1 of 6: completed, or an already completed turn enriched, part 1 of 2](c4/rendered/STR-04-09-alt1-1.svg)

View: STR-04-09-alt1-1

![Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 1 of 6: completed, or an already completed turn enriched, part 2 of 2](c4/rendered/STR-04-09-alt1-1-part2.svg)

View: STR-04-09-alt1-1-part2

![Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 2 of 6: CompletionGateMissing](c4/rendered/STR-04-09-alt1-2.svg)

View: STR-04-09-alt1-2

![Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 3 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 1 of 2](c4/rendered/STR-04-09-alt1-3.svg)

View: STR-04-09-alt1-3

![Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 3 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 2 of 2](c4/rendered/STR-04-09-alt1-3-part2.svg)

View: STR-04-09-alt1-3-part2

![Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 4 of 6: RequestClassInvalid](c4/rendered/STR-04-09-alt1-4.svg)

View: STR-04-09-alt1-4

![Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 5 of 6: SessionNotFound](c4/rendered/STR-04-09-alt1-5.svg)

View: STR-04-09-alt1-5

![Sequence: submit CompleteSessionTurnCommand and process each response, alt 1 branch 6 of 6: SerializableWrite's four attempts used](c4/rendered/STR-04-09-alt1-6.svg)

View: STR-04-09-alt1-6

#### Sequence: submit FailSessionTurnCommand and process each response

`Writer` is the typed client, the REPL (`client.SessionLog.FailTurnAsync`), or an MCP caller of `sessionlog_fail_turn` (the same `turnJson` field list as `sessionlog_complete_turn`). The write is TurnWritePipeline in mode `Fail` (Merge with status `failed` and the `failureNote`, insert when absent, as today); CompletionGate runs only on the move into `failed` and then requires only `requestClass`.

![Sequence: submit FailSessionTurnCommand and process each response, part 1 of 3](c4/rendered/STR-04-10.svg)

View: STR-04-10

![Sequence: submit FailSessionTurnCommand and process each response, part 2 of 3](c4/rendered/STR-04-10-part2.svg)

View: STR-04-10-part2

![Sequence: submit FailSessionTurnCommand and process each response, part 3 of 3](c4/rendered/STR-04-10-part3.svg)

View: STR-04-10-part3

![Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 1 of 6: failed, or an already failed turn enriched, part 1 of 2](c4/rendered/STR-04-10-alt1-1.svg)

View: STR-04-10-alt1-1

![Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 1 of 6: failed, or an already failed turn enriched, part 2 of 2](c4/rendered/STR-04-10-alt1-1-part2.svg)

View: STR-04-10-alt1-1-part2

![Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 2 of 6: CompletionGateMissing (class missing or Unclassified on the move into failed), part 1 of 2](c4/rendered/STR-04-10-alt1-2.svg)

View: STR-04-10-alt1-2

![Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 2 of 6: CompletionGateMissing (class missing or Unclassified on the move into failed), part 2 of 2](c4/rendered/STR-04-10-alt1-2-part2.svg)

View: STR-04-10-alt1-2-part2

![Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 3 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 1 of 2](c4/rendered/STR-04-10-alt1-3.svg)

View: STR-04-10-alt1-3

![Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 3 of 6: InvalidIdentifier, or InvalidPayload (turn context, attribution, or QBAgent compliance), part 2 of 2](c4/rendered/STR-04-10-alt1-3-part2.svg)

View: STR-04-10-alt1-3-part2

![Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 4 of 6: RequestClassInvalid](c4/rendered/STR-04-10-alt1-4.svg)

View: STR-04-10-alt1-4

![Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 5 of 6: SessionNotFound](c4/rendered/STR-04-10-alt1-5.svg)

View: STR-04-10-alt1-5

![Sequence: submit FailSessionTurnCommand and process each response, alt 1 branch 6 of 6: SerializableWrite's four attempts used](c4/rendered/STR-04-10-alt1-6.svg)

View: STR-04-10-alt1-6

#### Sequence: submit OpenSessionLogCommand and process each response

`Writer` is the plugin through `Invoke-SessionOpen`, the typed client (`OpenSessionAsync`), the REPL, or an MCP caller of `sessionlog_open`. The plugin opens the session at every SessionStart (process StartHostSession, including a resumed or post-compact start of the same host session key), in the first-ingress writer of hosts without a SessionStart event, in the `openSession` verb for its host session's recorded `mcpSessionId`, and in the failsafe drain before each bound drained record. `Invoke-SessionOpen` maps the answer once for all of them (section 3.5.6): 200 with `created` true or false is confirmed; the opened id is the one its caller recorded before the first open (StartHostSession and the first-ingress writer record a new `mcpSessionId` only when none is recorded for the host session key), so an outage never changes the identity, every retry and drain opens the same id, and a resumed or compacted host session keeps its recorded MCP session (FR-MCP-SESSIONLIFE-002); 400 invalid session identifier is final; a transport outage keeps the records for the next drain.

![Sequence: submit OpenSessionLogCommand and process each response, part 1 of 4](c4/rendered/STR-04-11.svg)

View: STR-04-11

![Sequence: submit OpenSessionLogCommand and process each response, part 2 of 4](c4/rendered/STR-04-11-part2.svg)

View: STR-04-11-part2

![Sequence: submit OpenSessionLogCommand and process each response, part 3 of 4](c4/rendered/STR-04-11-part3.svg)

View: STR-04-11-part3

![Sequence: submit OpenSessionLogCommand and process each response, part 4 of 4](c4/rendered/STR-04-11-part4.svg)

View: STR-04-11-part4

![Sequence: submit OpenSessionLogCommand and process each response, alt 1 branch 1 of 4: session found](c4/rendered/STR-04-11-alt1-1.svg)

View: STR-04-11-alt1-1

![Sequence: submit OpenSessionLogCommand and process each response, alt 1 branch 2 of 4: SessionNotFound, part 1 of 2](c4/rendered/STR-04-11-alt1-2.svg)

View: STR-04-11-alt1-2

![Sequence: submit OpenSessionLogCommand and process each response, alt 1 branch 2 of 4: SessionNotFound, part 2 of 2](c4/rendered/STR-04-11-alt1-2-part2.svg)

View: STR-04-11-alt1-2-part2

![Sequence: submit OpenSessionLogCommand and process each response, alt 1 branch 3 of 4: InvalidIdentifier](c4/rendered/STR-04-11-alt1-3.svg)

View: STR-04-11-alt1-3

![Sequence: submit OpenSessionLogCommand and process each response, alt 1 branch 4 of 4: transport outage, part 1 of 2](c4/rendered/STR-04-11-alt1-4.svg)

View: STR-04-11-alt1-4

![Sequence: submit OpenSessionLogCommand and process each response, alt 1 branch 4 of 4: transport outage, part 2 of 2](c4/rendered/STR-04-11-alt1-4-part2.svg)

View: STR-04-11-alt1-4-part2

#### Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response

`Model` is the model, or the HV reviewer storing its result as ordered dialog parts; `Plugin` is the `appendDialog` verb. Process PersistTurnWriteWithReceipt sends `client.SessionLog.AppendDialogAsync` through the REPL; the typed client and the REPL call the same route, and MCP `sessionlog_dialog` answers `{ success, totalDialogItems }` or the classified error object instead. The server appends on any turn status; the plugin refuses a turn it knows is terminal with `turn_immutable` inside BindVerbToTurn (FR-MCP-SESSIONTURN-004-AC004). The handler checks the items before TurnLocator; REST errors keep today's bodies.

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, part 1 of 2](c4/rendered/STR-04-12.svg)

View: STR-04-12

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, part 2 of 2](c4/rendered/STR-04-12-part2.svg)

View: STR-04-12-part2

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 1 of 3: binding refused](c4/rendered/STR-04-12-alt1-1.svg)

View: STR-04-12-alt1-1

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 2 of 3: unsupported session, target already open when it failed closed, part 1 of 2](c4/rendered/STR-04-12-alt1-2.svg)

View: STR-04-12-alt1-2

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 2 of 3: unsupported session, target already open when it failed closed, part 2 of 2](c4/rendered/STR-04-12-alt1-2-part2.svg)

View: STR-04-12-alt1-2-part2

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 1 of 8](c4/rendered/STR-04-12-alt1-3.svg)

View: STR-04-12-alt1-3

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 2 of 8](c4/rendered/STR-04-12-alt1-3-part2.svg)

View: STR-04-12-alt1-3-part2

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 3 of 8](c4/rendered/STR-04-12-alt1-3-part3.svg)

View: STR-04-12-alt1-3-part3

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 4 of 8](c4/rendered/STR-04-12-alt1-3-part4.svg)

View: STR-04-12-alt1-3-part4

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 5 of 8](c4/rendered/STR-04-12-alt1-3-part5.svg)

View: STR-04-12-alt1-3-part5

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 6 of 8](c4/rendered/STR-04-12-alt1-3-part6.svg)

View: STR-04-12-alt1-3-part6

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 7 of 8](c4/rendered/STR-04-12-alt1-3-part7.svg)

View: STR-04-12-alt1-3-part7

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 8 of 8](c4/rendered/STR-04-12-alt1-3-part8.svg)

View: STR-04-12-alt1-3-part8

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 1 of 4: appended, part 1 of 2](c4/rendered/STR-04-12-alt2-1.svg)

View: STR-04-12-alt2-1

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 1 of 4: appended, part 2 of 2](c4/rendered/STR-04-12-alt2-1-part2.svg)

View: STR-04-12-alt2-1-part2

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 2 of 4: no item, or InvalidIdentifier, part 1 of 2](c4/rendered/STR-04-12-alt2-2.svg)

View: STR-04-12-alt2-2

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 2 of 4: no item, or InvalidIdentifier, part 2 of 2](c4/rendered/STR-04-12-alt2-2-part2.svg)

View: STR-04-12-alt2-2-part2

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 3 of 4: SessionNotFound or TurnNotFound, part 1 of 2](c4/rendered/STR-04-12-alt2-3.svg)

View: STR-04-12-alt2-3

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 3 of 4: SessionNotFound or TurnNotFound, part 2 of 2](c4/rendered/STR-04-12-alt2-3-part2.svg)

View: STR-04-12-alt2-3-part2

![Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response, alt 2 branch 4 of 4: transport outage or a retryable 503](c4/rendered/STR-04-12-alt2-4.svg)

View: STR-04-12-alt2-4

#### Sequence: submit SetTurnTitleCommand from a plugin verb and process each response

`Plugin` is the `setTurnTitle` verb; BindVerbToTurn makes an explicit `requestId` retitle only that turn (TR-MCP-SESSIONTURN-003 AC-005). Process PersistTurnWriteWithReceipt sends `client.SessionLog.SetTurnTitleAsync` through the REPL; the typed client and the REPL call the same route, and there is no MCP tool.

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, part 1 of 2](c4/rendered/STR-04-13.svg)

View: STR-04-13

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, part 2 of 2](c4/rendered/STR-04-13-part2.svg)

View: STR-04-13-part2

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 1 of 3: binding refused](c4/rendered/STR-04-13-alt1-1.svg)

View: STR-04-13-alt1-1

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 2 of 3: unsupported session, target already open when it failed closed, part 1 of 2](c4/rendered/STR-04-13-alt1-2.svg)

View: STR-04-13-alt1-2

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 2 of 3: unsupported session, target already open when it failed closed, part 2 of 2](c4/rendered/STR-04-13-alt1-2-part2.svg)

View: STR-04-13-alt1-2-part2

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 1 of 8](c4/rendered/STR-04-13-alt1-3.svg)

View: STR-04-13-alt1-3

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 2 of 8](c4/rendered/STR-04-13-alt1-3-part2.svg)

View: STR-04-13-alt1-3-part2

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 3 of 8](c4/rendered/STR-04-13-alt1-3-part3.svg)

View: STR-04-13-alt1-3-part3

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 4 of 8](c4/rendered/STR-04-13-alt1-3-part4.svg)

View: STR-04-13-alt1-3-part4

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 5 of 8](c4/rendered/STR-04-13-alt1-3-part5.svg)

View: STR-04-13-alt1-3-part5

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 6 of 8](c4/rendered/STR-04-13-alt1-3-part6.svg)

View: STR-04-13-alt1-3-part6

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 7 of 8](c4/rendered/STR-04-13-alt1-3-part7.svg)

View: STR-04-13-alt1-3-part7

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 1 branch 3 of 3: bound, part 8 of 8](c4/rendered/STR-04-13-alt1-3-part8.svg)

View: STR-04-13-alt1-3-part8

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 1 of 4: retitled, part 1 of 2](c4/rendered/STR-04-13-alt2-1.svg)

View: STR-04-13-alt2-1

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 1 of 4: retitled, part 2 of 2](c4/rendered/STR-04-13-alt2-1-part2.svg)

View: STR-04-13-alt2-1-part2

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 2 of 4: InvalidIdentifier, part 1 of 2](c4/rendered/STR-04-13-alt2-2.svg)

View: STR-04-13-alt2-2

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 2 of 4: InvalidIdentifier, part 2 of 2](c4/rendered/STR-04-13-alt2-2-part2.svg)

View: STR-04-13-alt2-2-part2

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 3 of 4: SessionNotFound or TurnNotFound, part 1 of 2](c4/rendered/STR-04-13-alt2-3.svg)

View: STR-04-13-alt2-3

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 3 of 4: SessionNotFound or TurnNotFound, part 2 of 2](c4/rendered/STR-04-13-alt2-3-part2.svg)

View: STR-04-13-alt2-3-part2

![Sequence: submit SetTurnTitleCommand from a plugin verb and process each response, alt 2 branch 4 of 4: transport outage or a retryable 503](c4/rendered/STR-04-13-alt2-4.svg)

View: STR-04-13-alt2-4

#### Sequence: submit SetSessionTitleCommand and process each response

`Writer` is the plugin `setSessionTitle` verb (which targets the host session's own `mcpSessionId`), the typed client, or the REPL; there is no MCP tool.

![Sequence: submit SetSessionTitleCommand and process each response, part 1 of 4](c4/rendered/STR-04-14.svg)

View: STR-04-14

![Sequence: submit SetSessionTitleCommand and process each response, part 2 of 4](c4/rendered/STR-04-14-part2.svg)

View: STR-04-14-part2

![Sequence: submit SetSessionTitleCommand and process each response, part 3 of 4](c4/rendered/STR-04-14-part3.svg)

View: STR-04-14-part3

![Sequence: submit SetSessionTitleCommand and process each response, part 4 of 4](c4/rendered/STR-04-14-part4.svg)

View: STR-04-14-part4

![Sequence: submit SetSessionTitleCommand and process each response, alt 1 branch 1 of 3: retitled](c4/rendered/STR-04-14-alt1-1.svg)

View: STR-04-14-alt1-1

![Sequence: submit SetSessionTitleCommand and process each response, alt 1 branch 2 of 3: InvalidIdentifier](c4/rendered/STR-04-14-alt1-2.svg)

View: STR-04-14-alt1-2

![Sequence: submit SetSessionTitleCommand and process each response, alt 1 branch 3 of 3: SessionNotFound](c4/rendered/STR-04-14-alt1-3.svg)

View: STR-04-14-alt1-3

#### Sequence: replay the failsafe (process DrainFailsafe) and process each response

`Trigger` is process StartHostSession at SessionStart, the next prompt hook or verb whose host session's failsafe holds drainable records, or the explicit `workflow.failsafe.drain` verb; `Plugin` is the failsafe drain (develop's `Invoke-ReplFailsafeDrain`, section 4.4). Records are replayed oldest first. A retryable answer (a transport outage, a retryable 503, or 409 `conflict` with `retryable: true`) stops the walk without counting an attempt, and that record and every later one stay queued; a final answer counts an attempt on its record and the walk continues, skipping the later records of the same turn; the quarantine is the one place for a record the drain cannot replay (section 3.1, "Failsafe drain"; FR-MCP-REPL-011, TR-MCP-REPL-016, TR-MCP-REPL-017, FR-MCP-FAILSAFE-001, FR-MCP-SESSIONLIFE-004 reconciled in process DrainFailsafe).

![Sequence: replay the failsafe (process DrainFailsafe) and process each response, part 1 of 2](c4/rendered/STR-04-15.svg)

View: STR-04-15

![Sequence: replay the failsafe (process DrainFailsafe) and process each response, part 2 of 2](c4/rendered/STR-04-15-part2.svg)

View: STR-04-15-part2

![Sequence: replay the failsafe (process DrainFailsafe) and process each response, loop 1: records oldest first, until a retryable answer stops the walk, part 1 of 2](c4/rendered/STR-04-15-loop1.svg)

View: STR-04-15-loop1

![Sequence: replay the failsafe (process DrainFailsafe) and process each response, loop 1: records oldest first, until a retryable answer stops the walk, part 2 of 2](c4/rendered/STR-04-15-loop1-part2.svg)

View: STR-04-15-loop1-part2

##### Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence)

`Plugin` is the failsafe drain. The read repairs a legacy record's shape as today (`Repair-ReplFailsafeQueuedRecord`), and a repaired record restarts at zero attempts, so an exhausted numeric-keyed legacy record replays (FR-MCP-SESSIONLIFE-004); nothing replays from the quarantine subdirectory. A record is quarantined with its `.reason.txt` when it is malformed, when its `drainAttempts` reached the budget of 5, or when it is a legacy record without a captured binding that names no request; a record whose turn was left unresolved earlier in this walk is skipped; an `unsupported_session` record of a host session that is still live with `unsupportedSince` is not drained (section 3.1). Every other record is sent by the nested sequence SendDrainedRecord, and its answer decides the record: retryable stops the walk, confirmed removes the record, final counts the attempt and leaves its turn unresolved for the rest of the walk, and not replayable quarantines it.

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence)](c4/rendered/ReplayOneFailsafeRecord.svg)

View: ReplayOneFailsafeRecord

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 1 of 6: malformed record, part 1 of 2](c4/rendered/ReplayOneFailsafeRecord-alt1-1.svg)

View: ReplayOneFailsafeRecord-alt1-1

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 1 of 6: malformed record, part 2 of 2](c4/rendered/ReplayOneFailsafeRecord-alt1-1-part2.svg)

View: ReplayOneFailsafeRecord-alt1-1-part2

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 2 of 6: its turn unresolved earlier in this walk, part 1 of 2](c4/rendered/ReplayOneFailsafeRecord-alt1-2.svg)

View: ReplayOneFailsafeRecord-alt1-2

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 2 of 6: its turn unresolved earlier in this walk, part 2 of 2](c4/rendered/ReplayOneFailsafeRecord-alt1-2-part2.svg)

View: ReplayOneFailsafeRecord-alt1-2-part2

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 3 of 6: drainAttempts at the budget of 5, record not repaired, part 1 of 2](c4/rendered/ReplayOneFailsafeRecord-alt1-3.svg)

View: ReplayOneFailsafeRecord-alt1-3

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 3 of 6: drainAttempts at the budget of 5, record not repaired, part 2 of 2](c4/rendered/ReplayOneFailsafeRecord-alt1-3-part2.svg)

View: ReplayOneFailsafeRecord-alt1-3-part2

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 4 of 6: legacy record that names no request, part 1 of 2](c4/rendered/ReplayOneFailsafeRecord-alt1-4.svg)

View: ReplayOneFailsafeRecord-alt1-4

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 4 of 6: legacy record that names no request, part 2 of 2](c4/rendered/ReplayOneFailsafeRecord-alt1-4-part2.svg)

View: ReplayOneFailsafeRecord-alt1-4-part2

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 5 of 6: unsupported_session record of a live host session, part 1 of 2](c4/rendered/ReplayOneFailsafeRecord-alt1-5.svg)

View: ReplayOneFailsafeRecord-alt1-5

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 5 of 6: unsupported_session record of a live host session, part 2 of 2](c4/rendered/ReplayOneFailsafeRecord-alt1-5-part2.svg)

View: ReplayOneFailsafeRecord-alt1-5-part2

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 6 of 6: legacy record naming its request, or a bound record, part 1 of 3](c4/rendered/ReplayOneFailsafeRecord-alt1-6.svg)

View: ReplayOneFailsafeRecord-alt1-6

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 6 of 6: legacy record naming its request, or a bound record, part 2 of 3](c4/rendered/ReplayOneFailsafeRecord-alt1-6-part2.svg)

View: ReplayOneFailsafeRecord-alt1-6-part2

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 1 branch 6 of 6: legacy record naming its request, or a bound record, part 3 of 3](c4/rendered/ReplayOneFailsafeRecord-alt1-6-part3.svg)

View: ReplayOneFailsafeRecord-alt1-6-part3

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 1 of 4: retryable answer, part 1 of 2](c4/rendered/ReplayOneFailsafeRecord-alt2-1.svg)

View: ReplayOneFailsafeRecord-alt2-1

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 1 of 4: retryable answer, part 2 of 2](c4/rendered/ReplayOneFailsafeRecord-alt2-1-part2.svg)

View: ReplayOneFailsafeRecord-alt2-1-part2

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 2 of 4: confirmed answer, part 1 of 2](c4/rendered/ReplayOneFailsafeRecord-alt2-2.svg)

View: ReplayOneFailsafeRecord-alt2-2

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 2 of 4: confirmed answer, part 2 of 2](c4/rendered/ReplayOneFailsafeRecord-alt2-2-part2.svg)

View: ReplayOneFailsafeRecord-alt2-2-part2

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 3 of 4: final answer, part 1 of 2](c4/rendered/ReplayOneFailsafeRecord-alt2-3.svg)

View: ReplayOneFailsafeRecord-alt2-3

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 3 of 4: final answer, part 2 of 2](c4/rendered/ReplayOneFailsafeRecord-alt2-3-part2.svg)

View: ReplayOneFailsafeRecord-alt2-3-part2

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 4 of 4: not replayable, part 1 of 2](c4/rendered/ReplayOneFailsafeRecord-alt2-4.svg)

View: ReplayOneFailsafeRecord-alt2-4

![Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence), alt 2 branch 4 of 4: not replayable, part 2 of 2](c4/rendered/ReplayOneFailsafeRecord-alt2-4-part2.svg)

View: ReplayOneFailsafeRecord-alt2-4-part2

##### Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord)

A legacy record replays its captured method and parameters through the REPL, as today. A bound record first replays the idempotent session open for its envelope `mcpSessionId` through `Invoke-SessionOpen` (the OpenSessionLogCommand sequence). A mint record re-mints its original nonce (the MintSessionTurnCommand sequence), and `Resolve-MintOutcome` classifies the answer as a stored retry, which never fails the host session closed, and whose final answer counts one attempt on the mint record (section 3.5.6); a returned turn journals `pending-<nonce>` to the minted `requestId` through `Add-McpAlias` (section 3.5.6, one atomic replace), rewrites the queued records under it, delivers it once through `Update-TurnState` when no delivery was recorded (never a merged terminal turn), and removes the mint record. A write, a close, or an `unsupported_session` record goes through process PersistTurnWriteWithReceipt under the journaled minted id, the bound id, or the original pair (trusted recovery); its server branches are the sequence of the unit it dispatches. A write bound to a pending id whose mint record is quarantined, and an `unsupported_session` record whose turn is no longer in progress, are not replayable.

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord)](c4/rendered/SendDrainedRecord.svg)

View: SendDrainedRecord

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 1 of 3: legacy record naming its request, part 1 of 3](c4/rendered/SendDrainedRecord-alt1-1.svg)

View: SendDrainedRecord-alt1-1

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 1 of 3: legacy record naming its request, part 2 of 3](c4/rendered/SendDrainedRecord-alt1-1-part2.svg)

View: SendDrainedRecord-alt1-1-part2

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 1 of 3: legacy record naming its request, part 3 of 3](c4/rendered/SendDrainedRecord-alt1-1-part3.svg)

View: SendDrainedRecord-alt1-1-part3

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 2 of 3: write bound to a pending id whose mint is quarantined, part 1 of 2](c4/rendered/SendDrainedRecord-alt1-2.svg)

View: SendDrainedRecord-alt1-2

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 2 of 3: write bound to a pending id whose mint is quarantined, part 2 of 2](c4/rendered/SendDrainedRecord-alt1-2-part2.svg)

View: SendDrainedRecord-alt1-2-part2

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 3 of 3: bound record, part 1 of 3](c4/rendered/SendDrainedRecord-alt1-3.svg)

View: SendDrainedRecord-alt1-3

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 3 of 3: bound record, part 2 of 3](c4/rendered/SendDrainedRecord-alt1-3-part2.svg)

View: SendDrainedRecord-alt1-3-part2

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 1 branch 3 of 3: bound record, part 3 of 3](c4/rendered/SendDrainedRecord-alt1-3-part3.svg)

View: SendDrainedRecord-alt1-3-part3

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 1 of 4: outage, or final (invalid session identifier), part 1 of 2](c4/rendered/SendDrainedRecord-alt2-1.svg)

View: SendDrainedRecord-alt2-1

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 1 of 4: outage, or final (invalid session identifier), part 2 of 2](c4/rendered/SendDrainedRecord-alt2-1-part2.svg)

View: SendDrainedRecord-alt2-1-part2

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 2 of 4: mint record, part 1 of 4](c4/rendered/SendDrainedRecord-alt2-2.svg)

View: SendDrainedRecord-alt2-2

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 2 of 4: mint record, part 2 of 4](c4/rendered/SendDrainedRecord-alt2-2-part2.svg)

View: SendDrainedRecord-alt2-2-part2

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 2 of 4: mint record, part 3 of 4](c4/rendered/SendDrainedRecord-alt2-2-part3.svg)

View: SendDrainedRecord-alt2-2-part3

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 2 of 4: mint record, part 4 of 4](c4/rendered/SendDrainedRecord-alt2-2-part4.svg)

View: SendDrainedRecord-alt2-2-part4

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), opt 1: a turn returned, part 1 of 2](c4/rendered/SendDrainedRecord-opt1.svg)

View: SendDrainedRecord-opt1

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), opt 1: a turn returned, part 2 of 2](c4/rendered/SendDrainedRecord-opt1-part2.svg)

View: SendDrainedRecord-opt1-part2

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 3 of 4: unsupported_session record, turn no longer in progress, part 1 of 2](c4/rendered/SendDrainedRecord-alt2-3.svg)

View: SendDrainedRecord-alt2-3

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 3 of 4: unsupported_session record, turn no longer in progress, part 2 of 2](c4/rendered/SendDrainedRecord-alt2-3-part2.svg)

View: SendDrainedRecord-alt2-3-part2

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 4 of 4: write, close, or unsupported_session record in progress, part 1 of 4](c4/rendered/SendDrainedRecord-alt2-4.svg)

View: SendDrainedRecord-alt2-4

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 4 of 4: write, close, or unsupported_session record in progress, part 2 of 4](c4/rendered/SendDrainedRecord-alt2-4-part2.svg)

View: SendDrainedRecord-alt2-4-part2

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 4 of 4: write, close, or unsupported_session record in progress, part 3 of 4](c4/rendered/SendDrainedRecord-alt2-4-part3.svg)

View: SendDrainedRecord-alt2-4-part3

![Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord), alt 2 branch 4 of 4: write, close, or unsupported_session record in progress, part 4 of 4](c4/rendered/SendDrainedRecord-alt2-4-part4.svg)

View: SendDrainedRecord-alt2-4-part4

## 5. Phase B: request class, review records, thresholds

### 5.1 Data model

- `SessionLogTurnEntity` gains `RequestClass` (16, indexed, null until the turn is classified) and, per review kind (agent, hostile): `Status` (16, required), `OtherNotes` (text), `Correctness`, `Completeness`, `CorrectnessThreshold`, `CompletenessThreshold` (decimal, precision 29 scale 28), `Reviewer` (256), `ReviewedAt` (UTC), `RequestJsonlPath`, `ResponseJsonlPath` (2048).
- Child `SessionLogTurnReviewReasonEntity { Id, WorkspaceId, SessionLogTurnId, Agent, SessionId, RequestId, Kind, Ordinal, Value }`: `Agent` (64), `SessionId` (256), and `RequestId` (256) are its turn's key (section 3.1, "Root ids"; the turn key of section 6.3, because a request id is unique only within its session, `src/QBrainAi.Storage/McpDbContext.cs:391,404`), copied from the turn in the same transaction, never changed, and in no key or index (`ReviewReasonRootIdsEqualItsTurnKey`). Its wiki dump policy is `COPY-SANITIZE+HISTORY`, listed with the session-log child tables (`src/QBrainAi.Services/Services/WikiDumpTablePolicyRegistry.cs:29-31` at `a8d3934b`; section 3.1, Review reason dump policy; `WikiDumpReviewReasonsCopySanitizeHistory`).
- Backfill: `RequestClass = Unclassified`, both review statuses `Unstarted` (`ReviewDefaults` for a turn without a class), on every existing row including soft-deleted rows.
- Database defaults (section 8.5, binary-only rollback): the migration that adds the columns gives each review `Status` the database default `Unstarted` and `RequestClass` the database default `Unclassified`, so a turn the prior binary inserts while the schema stays at the new migrations, which omits these columns, reads after the roll-forward as a backfilled row does, the `ReviewDefaults` of a turn without a class, and the roll-forward runs no second backfill (`RollbackPriorBinaryInsertsTurnRollForwardReadsDefaults`). The new binary writes each of these columns on every insert, so a new turn's `RequestClass` stays null until it is classified (inference: a default declared on the EF model for a nullable column would let EF Core omit an explicit null and store `Unclassified`). The migration runs Up, Down, and Up on SQLite, PostgreSQL, and SQL Server with the schema and the data equal after the second Up (`MigrationsUpDownUpThreeProvidersSchemaAndDataEqual`, section 8.5).
- Wire shape on `UnifiedRequestEntryDto` (both develop definitions, section 3.5): `requestClass` and the two review objects `agentReview`, `hostileReview` with `status`, `otherNotes`, `correctness`, `completeness`, `correctnessThreshold`, `completenessThreshold`, `reasons`, `reviewer`, `reviewedAt`, `requestJsonlPath`, `responseJsonlPath`; every read surface returns them through `TurnReadProjection` (section 3.5.3), so unset fields are emitted as `null`. The new review DTOs (`SessionLogTurnReviewKind`, `SessionLogTurnReviewInput`, `SessionLogTurnReviewDto`) are defined once in `src/QBrainAi.Client/Models/SessionLog/` (namespace `QBrainAi.Client.Models.SessionLog`).
- Request classes: `Code`, `Docs`, `Chore` (case-insensitive in, PascalCase out); `Unclassified` exists only for legacy rows.
- Review statuses: `Unnecessary`, `Unstarted`, `Running`, `Pass`, `Fail`, `Other`; `Unnecessary` and `Unstarted` are server-managed (`ReviewDefaults`, section 3.5.4), and a review body naming either from an agent surface is refused with `review_unnecessary_not_client_writable` (FR-MCP-SESSIONREVIEW-006-AC004, case `ReviewControllerClientCannotWriteUnstarted`).
- Thresholds: `ReviewThresholds:{Agent|Hostile}:{Correctness|Completeness}` under `SessionLog`, decimal in [0,1], default 0.98, inclusive, per instance through `McpInstanceResolver`; `ScoreVerdict` (section 3.5.4) applies them. They are declared under the canonical root as `QBrainAi: SessionLog: ReviewThresholds:` in `appsettings.yaml` (which already holds `QBrainAi:` with `SessionLog:` under it, `src/QBrainAi.Support.Mcp/appsettings.yaml:46,332`) and read as `Mcp:SessionLog:ReviewThresholds:*` after `CanonicalSectionProjectionProvider` projects the `QBrainAi:` and `Mcp:` keys onto `Mcp:` (`src/QBrainAi.Services/Options/CanonicalSectionProjectionProvider.cs:7-8,29`: within one layer `QBrainAi:` wins, and a legacy `Mcp:` key keeps working through 1.x as FR-MCP-QBRAIN-004 allows), as section 6.2 declares `AgentContext`. ConfigValidator checks them under both roots, canonical first, as TR-MCP-NUKE-008 requires. FR-MCP-SESSIONREVIEW-007-AC001, TR-MCP-SESSIONREVIEW-007 AC-004, and TR-MCP-NUKE-004 are updated to name the canonical root and cite TR-MCP-NUKE-008; FR-MCP-QBRAIN-004 and TR-MCP-NUKE-008 stay as they are.
- Turn status words: "terminal" and "nonterminal" mean `TurnStatus.IsTerminal` (section 3.5.3: `completed`, `failed`, `closed`, `canceled`, `cancelled`). Agent surfaces write `in_progress`, `completed`, `failed`, `canceled`, and the legacy `cancelled`; `closed` and every other value are refused with 400 `validation_error` (`TurnStatus`, section 3.5.3).

![Review status lifecycle (state diagram), part 1 of 5](c4/rendered/STR-05-01.svg)

View: STR-05-01

![Review status lifecycle (state diagram), part 2 of 5](c4/rendered/STR-05-01-part2.svg)

View: STR-05-01-part2

![Review status lifecycle (state diagram), part 3 of 5](c4/rendered/STR-05-01-part3.svg)

View: STR-05-01-part3

![Review status lifecycle (state diagram), part 4 of 5](c4/rendered/STR-05-01-part4.svg)

View: STR-05-01-part4

![Review status lifecycle (state diagram), part 5 of 5](c4/rendered/STR-05-01-part5.svg)

View: STR-05-01-part5

### 5.2 Units of work (CQRS handlers)

The same rule as section 4.2: each unit is one command or query and its handler. Request classification, review validation, and the completion gate are not units of their own; they are policies (section 5.3) that the turn write and close handlers (SubmitSessionLogCommand, the route every plugin, REPL-workflow, and McpAgent turn verb uses, and UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, FailSessionTurnCommand) apply through `TurnWritePipeline` (section 3.5.3). Units call the components of section 3.5 and never redraw their steps; `WorkspaceResolutionBehavior` (section 3.5.1) runs after the dispatch and before every handler below, so each unit lists its 400 `validation_error` with `details.field` `workspacePath` and does not draw it in its activity diagram (on REST the middleware answers an unknown workspace first, with its own body); the sequences draw it after the dispatch. The review commands and handlers live in `src/QBrainAi.Services/Reviews/Commands/` (namespace `QBrainAi.Support.Mcp.Reviews.Commands`) and the queries and handlers in `src/QBrainAi.Services/SessionLog/Queries/` (namespace `QBrainAi.Support.Mcp.SessionLog.Queries`), one type per file, so each record and its handler are two files. Like the Phase A handlers (section 4.2), every unit below delegates its storage work to an `ISessionLogService` member (the review units to the section members, the queries to `GetAsync` and `QueryAsync`), so the decorator chain `SessionLogSanitizingService`, `FederatedSessionLogService`, `TransactionGatedSessionLogService` (`src/QBrainAi.Support.Mcp/Program.cs:642-663`; the stdio host's chain without federation, `src/QBrainAi.Support.Mcp/McpStdio/McpStdioHost.cs:293-305`) wraps it as it wraps every other session-log call.

Write isolation: every Phase B write runs its checks and its save as one delegate through `SerializableWrite` (section 3.5.2): the review record and the review section reset below, and the turn writes that apply policies RequestClassification, ReviewValidation, and CompletionGate through `TurnWritePipeline`. The review record and the review reset are single-turn writes with turn-level isolation (section 3.1, "Turn-level isolation"): each reads its session row only to locate the parent and writes only the one turn with its children (section 3.5.2, isolation scope), so review writes to different turns of one session never answer a conflict (`TurnWriteConcurrentDifferentTurnsSameSessionBothCommitNoConflictAnswer`), while a review reset, a class change or Chore flip, a review write, and a close of the same turn commit wholly before or after one another, never between another write's check and its save. Every Phase B delegate reads and writes only the database: the review policies read only the request and the thresholds in force, and the server never opens a receipt or jsonl file. When SerializableWrite's default bound (four attempts) is exhausted, the unit returns 409 `conflict` with `retryable: true` and saves nothing; that is also the answer when a deadlock or a busy store outlasts the four attempts, while a busy store outside SerializableWrite keeps today's 503 `persistence_error` with `retryable: true` (section 3.1, "Deadlock and busy outcomes"). Cases, each on SQLite, PostgreSQL, and SQL Server, at TurnReviews' gates (section 8.4): `TurnWriteForcedInterleavingChoreFlipVsRunningWriteRunningKept` (FR-MCP-SESSIONREVIEW-006-AC003: a Chore flip that read `Unstarted` never overwrites a concurrent `Running` write) at TurnReviews' gate, and `TurnWriteForcedInterleavingReviewResetVsCompleteNeverClosedWithoutReview` and `TurnWriteForcedInterleavingClassChangeVsCompleteGateSeesCommittedClass`, which need the enforced completion gate, at the gate of TurnReviews' CompletionGate enforcement change set. SerializableWrite's own exhaustion case, `SerializableWriteRetriesExhausted409ConflictRetryableNothingSaved`, is SharedComponents', proved on its test entity (section 3.5.2).

#### RecordSessionTurnReviewCommand: record a review [FR-MCP-SESSIONREVIEW-001, -002, -003, -004, -005, -006, -007, FR-SUPPORT-016]

- Handler: `RecordSessionTurnReviewCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, SessionLogTurnReviewKind Kind, SessionLogTurnReviewInput Review) : ICommand<SessionLogTurnReviewDto>` in `src/QBrainAi.Services/Reviews/Commands/RecordSessionTurnReviewCommand.cs` and `RecordSessionTurnReviewCommandHandler` in `src/QBrainAi.Services/Reviews/Commands/RecordSessionTurnReviewCommandHandler.cs` (namespace `QBrainAi.Support.Mcp.Reviews.Commands`). It applies policy HvVerdictMapping when `hvVerdict` is supplied and policy ReviewValidation (origin Agent) to every write, then delegates the save to `ISessionLogService.ReplaceTurnSectionAsync` with section `agentReview` or `hostileReview` (`src/QBrainAi.Services/Services/ISessionLogService.cs:108`; `TurnSection` and `ParseSection` gain the two review members, `src/QBrainAi.Services/Services/SessionLogService.cs:1287-1317`), which runs `TurnLocator` and the save inside `SerializableWrite`; `TransactionGatedSessionLogService` wraps that member with its pre-mutation restore as it does today (`src/QBrainAi.Support.Mcp/Services/TransactionGatedSessionLogService.cs:169-183`). The review is not written through `UpsertTurnAsync`, whose mode `Merge` inserts an absent turn, while a review never creates a turn (404). This updates TR-SUPPORT-CORE-014 ac-5 ("record review uses UpsertTurnAsync") to the section member, and its case `RecordReviewWritesThroughUpsertTurnAsync` becomes `RecordReviewWritesThroughReplaceTurnSectionNeverCreatesTurn`.
- Surfaces: `PUT /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/sections/{section}` with section `agentReview` or `hostileReview` (aliases `agent-review`, `agent_review`, `hostile-review`, `hostile_review`, case-insensitive; a known non-review section stays on the existing section-replace handler, `SessionLogController.cs:576-601`); MCP `sessionlog_record_review(agent, sessionId, requestId, kind, reviewJson, workspacePath)` and MCP `sessionlog_replace_section` with a review section or alias, whose `sectionJson` is read as the same `SessionLogTurnReviewInput` body; client `RecordTurnReviewAsync`; REPL and plugin `workflow.sessionlog.recordReview` (plugin also accepts `hvReceiptPath`, mapped locally by process ConvertHvReceiptToReview; hostile kind accepts `hvVerdict`). Both MCP tools dispatch this command: today `sessionlog_replace_section` calls the service member directly (`src/QBrainAi.Support.Mcp/McpStdio/FwhMcpTools.SessionLog.cs:203-228`) and would skip the policies, the workspace behavior, and `hvVerdict`; every non-review section keeps that direct call (`McpSectionToolsReviewSectionDispatchReviewUnits`).
- Request `SessionLogTurnReviewInput`: `status` (optional), `otherNotes`, `correctness`, `completeness` (JSON number or numeric string; `ScoreLexemeJsonConverter` validates the lexeme when the body is deserialized, section 3.5.4), `reasons`, `reviewer`, `reviewedAt` (UTC with offset), `requestJsonlPath`, `responseJsonlPath`, `hvVerdict` (hostile only, sent without `status`). Thresholds on input are ignored.
- Response: REST 200 `SessionLogTurnReviewDto`, the turn's review record with server-derived status and stamped thresholds, read back through `TurnReadProjection` (section 3.5.3): the text fields are stored as written and redacted by `SecretRedactor` in the record the write returns and on every read surface, never before the save (FR-MCP-SESSIONLOGSAN-001: stored entities stay unchanged, sanitization is an outbound projection). MCP `{ success: true, review }` with the same record, from either tool; client and REPL the same record. Works on every turn status, including terminal turns. An identical write is a no-op (FR-MCP-SESSIONREVIEW-001-AC003). MCP failures return the classified error object with the same `code`.
- Errors, in the order the diagram checks them, identical on every surface (the golden contract carries multi-violation cases, `GoldenContractMultipleViolationsSameFirstCodeAllSurfaces`): 400 `validation_error` for an unknown section or kind (the PUT route keeps today's body, the `ClassifiedError` of the `ArgumentException`, `SessionLogController.cs:597-600`, `McpErrorClassifier.cs:92`); 400 `review_score_format`, `review_score_precision`, `review_score_out_of_range` (`ScoreLexeme`, at deserialization, so an invalid score on an unknown turn is 400, not 404); 400 `validation_error` with `details.field` `workspacePath` from `WorkspaceResolutionBehavior` after the dispatch; 400 `validation_error` with `details.field` `hvVerdict` on an agent review, or `details.field` `status` when `hvVerdict` comes with an explicit `status` (section 3.1); the codes of policy HvVerdictMapping and policy ReviewValidation (400 with the code in the `code` field, 422 `review_status_conflict` with the thresholds in force and the derived status); 400 `validation_error` for an invalid session or request id (`TurnLocator` `InvalidIdentifier`, same body as an unknown section); 404 `not_found` for a missing session or turn (`TurnLocator`); 409 `conflict` with `retryable: true` when `SerializableWrite` exhausts its default bound (four attempts), nothing saved.
- Retryable answers on the plugin's `recordReview` verb (section 3.1, "Retryable answers"): a review keeps its no-failsafe rule, because an older review must never overwrite a newer one, so the verb writes no failsafe record and never reports `queued`. A transport outage, 503 `backend_unavailable`, a busy store's 503 `persistence_error` with `retryable: true`, and this unit's 409 `conflict` with `retryable: true` are each reported with `persisted: false`, the answer's code, and `retryable: true`, exit 1 (every receipt other than `persisted`, `unchanged`, or `queued` exits 1, section 4.4), and the model records the review again for the same turn after the drain (process DrainFailsafe, section 4.4). The sequences of section 5.5 draw it.

![RecordSessionTurnReviewCommand: record a review (activity), part 1 of 8](c4/rendered/RecordSessionTurnReviewCommand.svg)

View: RecordSessionTurnReviewCommand

![RecordSessionTurnReviewCommand: record a review (activity), part 2 of 8](c4/rendered/RecordSessionTurnReviewCommand-part2.svg)

View: RecordSessionTurnReviewCommand-part2

![RecordSessionTurnReviewCommand: record a review (activity), part 3 of 8](c4/rendered/RecordSessionTurnReviewCommand-part3.svg)

View: RecordSessionTurnReviewCommand-part3

![RecordSessionTurnReviewCommand: record a review (activity), part 4 of 8](c4/rendered/RecordSessionTurnReviewCommand-part4.svg)

View: RecordSessionTurnReviewCommand-part4

![RecordSessionTurnReviewCommand: record a review (activity), part 5 of 8](c4/rendered/RecordSessionTurnReviewCommand-part5.svg)

View: RecordSessionTurnReviewCommand-part5

![RecordSessionTurnReviewCommand: record a review (activity), part 6 of 8](c4/rendered/RecordSessionTurnReviewCommand-part6.svg)

View: RecordSessionTurnReviewCommand-part6

![RecordSessionTurnReviewCommand: record a review (activity), part 7 of 8](c4/rendered/RecordSessionTurnReviewCommand-part7.svg)

View: RecordSessionTurnReviewCommand-part7

![RecordSessionTurnReviewCommand: record a review (activity), part 8 of 8](c4/rendered/RecordSessionTurnReviewCommand-part8.svg)

View: RecordSessionTurnReviewCommand-part8

#### ResetSessionTurnReviewCommand: reset a review section [FR-SUPPORT-016, FR-MCP-SESSIONREVIEW-001]

- Handler: `ResetSessionTurnReviewCommand(string WorkspacePath, string Agent, string SessionId, string RequestId, SessionLogTurnReviewKind Kind) : ICommand<SessionLogTurnReviewDto>` in `src/QBrainAi.Services/Reviews/Commands/ResetSessionTurnReviewCommand.cs` and `ResetSessionTurnReviewCommandHandler` in `src/QBrainAi.Services/Reviews/Commands/ResetSessionTurnReviewCommandHandler.cs` (namespace `QBrainAi.Support.Mcp.Reviews.Commands`), delegating to `ISessionLogService.ClearTurnSectionAsync` with the review section (`ISessionLogService.cs:121`; develop implements it as a replace with an empty payload, `SessionLogService.cs:1000-1001`), which inside `SerializableWrite` runs `TurnLocator`, refuses a `completed` or `failed` turn, and saves the `ReviewDefaults` record, under the same `TransactionGatedSessionLogService` restore (`TransactionGatedSessionLogService.cs:186-199`). The reset is a section write, not a turn delete, so the per-session delete gate of TR-MCP-DB-006 does not apply to it (this replaces the plan's note that TR-MCP-DB-006 covers a review section delete). The item route is not a review unit: it stays on the existing delete-item handler, which refuses review sections.
- Surface: `DELETE /qbrainai/sessionlog/{agent}/{sessionId}/{requestId}/sections/{section}` (a known non-review section stays on the existing clear-section handler, `SessionLogController.cs:606-628`); MCP `sessionlog_clear_section` with a review section or alias, which dispatches this command (today it calls the service member directly, `FwhMcpTools.SessionLog.cs:231-253`); client `ClearTurnReviewAsync`. Item delete `DELETE .../sections/{section}/items/{itemKey}` and MCP `sessionlog_delete_item` on a review section are refused (`SessionLogController.cs:635-658`, `FwhMcpTools.SessionLog.cs:256-279`).
- Response: the record reset by `ReviewDefaults` (section 3.5.4) to the default for its kind and the turn's current class, read back through `TurnReadProjection`; MCP `{ success: true, review }`.
- Errors: 400 problem "Invalid section or identifier." for an unknown section or an invalid session or request id (`TurnLocator` `InvalidIdentifier`; today's body, `SessionLogController.cs:624-627`; on MCP the classified `validation_error`); 400 `validation_error` with `details.field` `workspacePath` from `WorkspaceResolutionBehavior`; 404 `not_found` for a missing session or turn (`TurnLocator`); 400 `review_section_reset_on_terminal_turn` when the turn is `completed` or `failed`, record unchanged (FR-MCP-SESSIONREVIEW-001-AC004 and FR-SUPPORT-016 ac-5), while a turn that is `in_progress`, `canceled`, `cancelled`, or `closed` is reset; 409 `conflict` with `retryable: true` when `SerializableWrite` exhausts its default bound (four attempts), so a reset and a concurrent close of the same turn never both commit (`TurnWriteForcedInterleavingReviewResetVsCompleteNeverClosedWithoutReview`). The item delete on a review section never reaches this unit: the existing delete-item handler refuses it with 400.

![ResetSessionTurnReviewCommand: reset a review section (activity), part 1 of 5](c4/rendered/ResetSessionTurnReviewCommand.svg)

View: ResetSessionTurnReviewCommand

![ResetSessionTurnReviewCommand: reset a review section (activity), part 2 of 5](c4/rendered/ResetSessionTurnReviewCommand-part2.svg)

View: ResetSessionTurnReviewCommand-part2

![ResetSessionTurnReviewCommand: reset a review section (activity), part 3 of 5](c4/rendered/ResetSessionTurnReviewCommand-part3.svg)

View: ResetSessionTurnReviewCommand-part3

![ResetSessionTurnReviewCommand: reset a review section (activity), part 4 of 5](c4/rendered/ResetSessionTurnReviewCommand-part4.svg)

View: ResetSessionTurnReviewCommand-part4

![ResetSessionTurnReviewCommand: reset a review section (activity), part 5 of 5](c4/rendered/ResetSessionTurnReviewCommand-part5.svg)

View: ResetSessionTurnReviewCommand-part5

#### GetSessionLogQuery: read one session with its turns and reviews [FR-MCP-SESSIONREVIEW-001, FR-MCP-SESSIONREVIEW-004]

- Handler: `GetSessionLogQuery(string WorkspacePath, string Agent, string SessionId) : IQuery<UnifiedSessionLogDto>` in `src/QBrainAi.Services/SessionLog/Queries/GetSessionLogQuery.cs` and `GetSessionLogQueryHandler` in `src/QBrainAi.Services/SessionLog/Queries/GetSessionLogQueryHandler.cs` (namespace `QBrainAi.Support.Mcp.SessionLog.Queries`), delegating to `ISessionLogService.GetAsync` (`ISessionLogService.cs:65`), so the outbound sanitizer decorator redacts the session as today (FR-MCP-SESSIONLOGSAN-001).
- Surfaces: REST `GET /qbrainai/sessionlog/{agent}/{sessionId}` only (`SessionLogController.cs:163-179`; the typed client and the REPL have no single-session read and use QuerySessionLogsQuery, which has no session-id filter, so they select the session by its `sessionId` in the returned items).
- Response: 200 `UnifiedSessionLogDto`, each turn through `TurnReadProjection` (section 3.5.3): both review records, exact decimal scores (a 28-digit score reads back byte-equal), review text redacted by `SecretRedactor` from the raw stored text, and `null` for unset fields. A plugin readback that verifies an evidence write (FR-MCP-SESSIONCLASS-004-AC003) compares against this sanitized projection (section 3.1).
- Errors (today's bodies, from `TurnLocator`'s typed results; develop validates the id in the controller, `SessionLogController.cs:171-173`, and the handler keeps the body): 400 problem "Invalid session identifier." for a session id invalid for the agent; 404 with no body for a missing session; 400 `validation_error` with `details.field` `workspacePath` from `WorkspaceResolutionBehavior`.

![GetSessionLogQuery: read one session with its turns and reviews (activity), part 1 of 3](c4/rendered/GetSessionLogQuery.svg)

View: GetSessionLogQuery

![GetSessionLogQuery: read one session with its turns and reviews (activity), part 2 of 3](c4/rendered/GetSessionLogQuery-part2.svg)

View: GetSessionLogQuery-part2

![GetSessionLogQuery: read one session with its turns and reviews (activity), part 3 of 3](c4/rendered/GetSessionLogQuery-part3.svg)

View: GetSessionLogQuery-part3

#### QuerySessionLogsQuery: query sessions and turns with their reviews [FR-MCP-SESSIONREVIEW-001, FR-MCP-SESSIONREVIEW-004]

- Handler: `QuerySessionLogsQuery(string WorkspacePath, SessionLogQueryRequest Request) : IQuery<SessionLogQueryResult>` in `src/QBrainAi.Services/SessionLog/Queries/QuerySessionLogsQuery.cs` and `QuerySessionLogsQueryHandler` in `src/QBrainAi.Services/SessionLog/Queries/QuerySessionLogsQueryHandler.cs` (namespace `QBrainAi.Support.Mcp.SessionLog.Queries`), delegating to `ISessionLogService.QueryAsync` (`ISessionLogService.cs:54`).
- Surfaces: REST `GET /qbrainai/sessionlog?agent&agentDefinitionId&model&text&from&to&limit&offset&planFile&todoId&turnStatus&staleOlderThanHours` (`SessionLogController.cs:109-151`; there is no session-id filter), MCP `sessionlog_query` (the same filters without `agentDefinitionId` and `offset`), client `QueryAsync(SessionLogQueryRequest)`, REPL `client.SessionLog.QueryAsync`, plugins.
- Request `SessionLogQueryRequest` (unchanged). Paging is `PagedQuery` with `PageBounds` `Clamp` (section 3.5.2), never refused: `limit` defaults to 100 and is clamped to 1..1000, and a negative `offset` is read as 0 (`SessionLogService.cs:476-477`). Filtering, the total count, the order, and the page are computed from the raw stored data, before redaction (FR-MCP-SESSIONLOGSAN-001).
- Search and sanitization: the text filter matches `BuildSearchText` over the raw stored turn (`SessionLogService.cs:533,1367`), which Phase B extends with `reviewer`, `reasons`, and `otherNotes` (section 3.4, Search), so review text is searched like every other field: a query for a raw value the sanitizer redacts still finds its turn, and the turn reads back redacted (FR-MCP-SESSIONLOGSAN-001 ac4 kept, as `SessionLogSanitizationControllerTests.S16_QueryTextFilter_SecretContainingRawRecordStillParticipates_AndPagingMetadataUnchanged` pins, `tests/QBrainAi.Support.Mcp.IntegrationTests/Controllers/SessionLogSanitizationControllerTests.cs:95`); a reason present only in a replaced record no longer matches, because the stored record was replaced. This updates TR-MCP-SESSIONREVIEW-002 AC-005: its "a redacted review value matches no turn" clause is removed, and its case `SessionLogSanitizerReviewTests.Search_RedactedReviewValue_NoMatch_ReplacedReasonNoMatch` becomes `SearchRawReviewValueMatchesReadsRedactedReplacedReasonNoMatch`. The result is redacted by the decorator TR-MCP-SESSIONLOGSAN-001 names (`SessionLogSanitizingService`, outermost in both chains), whose review paths call `SecretRedactor`, the rule `TurnReadProjection` applies to review text; a second pass changes nothing (`SessionLogSanitizerTests.SanitizeString_IsIdempotent`, `tests/QBrainAi.Support.Mcp.Tests/Services/SessionLogSanitizerTests.cs:138`). TR-MCP-SESSIONLOGSAN-001 is updated to name `SecretRedactor` as its rule engine and the Phase B review fields as part of the graph it clones.
- Response: REST and client `SessionLogQueryResult { totalCount, limit, offset, items }`; MCP `{ totalCount, items }` only (`FwhMcpTools.SessionLog.cs:44-73`), so an MCP caller cannot request a later page; items are the matching sessions, each turn through `TurnReadProjection` as in GetSessionLogQuery. MCP failures return the classified error object.
- Errors (today's): 503 `persistence_error` with reason `pending_migration` while the session-log schema migration is pending, and other storage failures through `ClassifiedError`; 400 `validation_error` with `details.field` `workspacePath` from `WorkspaceResolutionBehavior`.

![QuerySessionLogsQuery: query sessions and turns with their reviews (activity), part 1 of 2](c4/rendered/QuerySessionLogsQuery.svg)

View: QuerySessionLogsQuery

![QuerySessionLogsQuery: query sessions and turns with their reviews (activity), part 2 of 2](c4/rendered/QuerySessionLogsQuery-part2.svg)

View: QuerySessionLogsQuery-part2

### 5.3 Policies applied inside handlers

Policy seams (S1): the server, the REPL, and McpAgent share one C# seam per policy and its constants, in `src/QBrainAi.Client/SessionLog/` (namespace `QBrainAi.Client.SessionLog`, which states the owner, so no type repeats it, section 3.5), each with one contract test class named for it: `RequestClasses` and `ReviewStatuses` (the value sets), `IRequestClassNormalizer` (policy RequestClassification; `RequestClassNormalizerContractTests`), `ITurnReviewValidator.Validate(kind, requestClass, priorStatus, input)` (policy ReviewValidation; `TurnReviewValidatorContractTests`), `ICompletionPolicy.GetMissing(targetStatus, requestClass, agentStatus, hostileStatus)` (policy CompletionPolicy; `CompletionPolicyContractTests`), `ReviewThresholds` (the thresholds stamped on a review, section 5.1), `TurnSections` (the turn section names, the review sections included), and `UtcTimestampJsonConverter` (UTC review timestamps with an offset); TurnReviews implements them (section 8.3). `IReviewThresholdProvider` (the thresholds in force per kind and instance, through `McpInstanceResolver`), the gate message builder, and policies HvVerdictMapping and CompletionGate are server types of TurnReviews (`src/QBrainAi.Services/Reviews/`, namespace `QBrainAi.Support.Mcp.Reviews`). The golden contract `docs/context/session-turn-review-contract.json` freezes their behavior (`SessionTurnReviewGoldenContractTests`).

#### Policy RequestClassification: request classification and the Chore flip [FR-MCP-SESSIONCLASS-001, FR-MCP-SESSIONREVIEW-006]

- Applied by: `TurnWritePipeline` (section 3.5.3) inside `SerializableWrite`, so for SubmitSessionLogCommand, UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, and FailSessionTurnCommand, whenever a write carries `requestClass` (surfaces: the plugin, REPL-workflow, and McpAgent verbs `updateTurn`, `completeTurn`, and `failTurn` through submit; REST turn upsert, PATCH, PUT, complete, and fail; MCP tools; client; REPL). It runs after the write's review records (policy ReviewValidation) and before policy CompletionGate, so the gate sees the class, the Chore flip, and the reviews this write stores (FR-MCP-SESSIONCLASS-002-AC001).
- Request: `requestClass` (string). Response: the turn with canonical `requestClass` and the review records after any lifecycle effect.
- Lifecycle effect: only on a turn that was nonterminal (`TurnStatus`) before this write, so a write that sets `Chore` and completes the turn in one call still flips the hostile record; a hostile record still at a default status takes the `ReviewDefaults` value for the new class, and a record past `Unstarted` or `Unnecessary` never changes (FR-MCP-SESSIONREVIEW-006-AC002, -AC003). A class-only change on a terminal turn is accepted and changes no review; an additive write carrying it is not re-gated, even when it leaves the turn failing the completion policy, while a Replace that carries `completed` or `failed` meets policy CompletionGate whatever else it changes (section 3.1; CompletionGate's trigger, below).
- Errors: 400 `request_class_invalid`.

![Policy RequestClassification: request classification and the Chore flip (activity), part 1 of 3](c4/rendered/RequestClassification.svg)

View: RequestClassification

![Policy RequestClassification: request classification and the Chore flip (activity), part 2 of 3](c4/rendered/RequestClassification-part2.svg)

View: RequestClassification-part2

![Policy RequestClassification: request classification and the Chore flip (activity), part 3 of 3](c4/rendered/RequestClassification-part3.svg)

View: RequestClassification-part3

#### Policy ReviewValidation: validate a review record and derive its status [FR-MCP-SESSIONREVIEW-003, -006, -007, FR-MCP-SESSIONCLASS-003]

- Applied by: RecordSessionTurnReviewCommand (origin Agent), and `TurnWritePipeline` for each review record a turn body carries: `agentReview` and `hostileReview` on SubmitSessionLogCommand, UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, and FailSessionTurnCommand from the Agent origin (McpAgent requests carry them, TR-MCP-SESSIONCLASS-004 AC-001) and from the Import and Replication origins, which store a supplied record exactly as received (status and stamped thresholds, not re-derived) after the same per-status and score validation (FR-MCP-SESSIONCLASS-003-AC002). It runs before policies RequestClassification and CompletionGate, so one write carrying `requestClass`, terminal reviews, and `completed` succeeds (FR-MCP-SESSIONCLASS-002-AC001). A turn body that omits a review keeps the stored record in the `Merge` modes and resets it to `ReviewDefaults` in the `Replace` mode (FR-MCP-SESSIONREVIEW-001-AC004); thresholds on input are ignored; ServerInternal writers never write a review (no review is ever fabricated).
- Implementation: the review validator seam of S1 (`ITurnReviewValidator`, `TurnReviewValidatorContractTests`), calling `ScoreVerdict` (section 3.5.4) for every derivation; it holds no threshold comparison of its own.
- Order: the checks run in the diagram's order and the first violation is returned, identical on REST, MCP, client, REPL, and the PowerShell core (golden contract, FR-MCP-SESSIONREVIEW-003-AC006).
- Errors: 400 with the code in the `code` field: `review_unnecessary_not_client_writable`, `review_invalid_status`, `review_field_required_for_status` (including a missing score when the status is omitted, `Pass`, or `Fail`), then the per-status field codes in this order: `review_field_forbidden_for_status`, `review_field_required_for_status`, `review_receipt_paths_required`, `review_other_notes_not_allowed`, `review_other_notes_required`, `review_reason_blank`, `review_timestamp_invalid`, `review_field_too_long`; 422 `review_status_conflict` from `ScoreVerdict` with the thresholds in force and the derived status.

![Policy ReviewValidation: validate a review record and derive its status (activity), part 1 of 5](c4/rendered/ReviewValidation.svg)

View: ReviewValidation

![Policy ReviewValidation: validate a review record and derive its status (activity), part 2 of 5](c4/rendered/ReviewValidation-part2.svg)

View: ReviewValidation-part2

![Policy ReviewValidation: validate a review record and derive its status (activity), part 3 of 5](c4/rendered/ReviewValidation-part3.svg)

View: ReviewValidation-part3

![Policy ReviewValidation: validate a review record and derive its status (activity), part 4 of 5](c4/rendered/ReviewValidation-part4.svg)

View: ReviewValidation-part4

![Policy ReviewValidation: validate a review record and derive its status (activity), part 5 of 5](c4/rendered/ReviewValidation-part5.svg)

View: ReviewValidation-part5

#### Policy HvVerdictMapping: HV verdict mapping (server) [FR-MCP-SESSIONREVIEW-005]

- Applied by: RecordSessionTurnReviewCommand when a hostile review carries `hvVerdict` and no `status` (a write with both is 400 `validation_error` naming `status`, section 3.1), with the hostile thresholds in force at that write, per instance through `McpInstanceResolver`. Its result goes on to policy ReviewValidation.
- Input: only the fields the server receives: `hvVerdict`, `correctness`, `completeness`, `reasons` (each FAIL and UNKNOWN claim of the receipt, prefixed `FAIL:` or `UNKNOWN:`, FR-MCP-SESSIONREVIEW-005-AC003), and `responseJsonlPath`. The server reads no receipt: `hvReceiptPath` never leaves the plugin.
- Effective verdict: AGREE counts only when no reason starts with `FAIL:` or `UNKNOWN:`; otherwise the write maps as DISAGREE. This is the rule the plugin applies when it sets `hvVerdict` from a receipt (FR-MCP-SESSIONREVIEW-005-AC003), so an MCP, client, or REPL caller and the plugin store the same status for the same fields; the golden contract's HV mapping cases run in C# and Pester (`HvVerdictMappingAgreeWithFailOrUnknownReasonMapsAsDisagree`).
- DISAGREE stores `Fail` when the scores derive `Fail`, otherwise `Other` with a nonempty `otherNotes` chosen deterministically: the FAIL claims of `reasons` when present, otherwise the UNKNOWN claims, otherwise the verdict-level reason `DISAGREE with no listed failed or unknown claims` plus the response receipt path (`HvVerdictMappingDisagreeUnknownOnlyPassScoresOtherNamesUnknownClaims`, `HvVerdictMappingDisagreeNoListedClaimsPassScoresOtherVerdictLevelReason`). A mapped write never returns `review_status_conflict` (`HvVerdictMappingInstanceOverride095Scores097And099AgreeStoresPassDisagreeStoresOther`).
- Errors: 400 `review_invalid_status` with `details.field` `hvVerdict` for a verdict other than AGREE or DISAGREE; 400 `review_field_required_for_status` naming a missing score; both before any mapping.

![Policy HvVerdictMapping: HV verdict mapping (server) (activity), part 1 of 3](c4/rendered/HvVerdictMapping.svg)

View: HvVerdictMapping

![Policy HvVerdictMapping: HV verdict mapping (server) (activity), part 2 of 3](c4/rendered/HvVerdictMapping-part2.svg)

View: HvVerdictMapping-part2

![Policy HvVerdictMapping: HV verdict mapping (server) (activity), part 3 of 3](c4/rendered/HvVerdictMapping-part3.svg)

View: HvVerdictMapping-part3

#### Policy CompletionPolicy: the completion rule [FR-MCP-SESSIONCLASS-002, FR-MCP-SESSIONCLASS-004]

- One rule, three uses: `CompletionPolicy` is the rule, frozen in the golden contract `docs/context/session-turn-review-contract.json` and implemented once in C# (the S1 completion-policy seam `ICompletionPolicy`, `CompletionPolicyContractTests`) for the server, the REPL, and McpAgent, and once in the PowerShell core (`Get-CloseRemedy`, section 3.5.6) across the language boundary, both run against the same golden contract; `CompletionGate` is its server enforcement; process AdvisoryCompletionPrecheck is its client advisory use.
- Rule: `completed` needs `requestClass` (`Unclassified` counts as missing) and `agentReview.status` in Pass, Fail, or Other; unless the class is `Chore`, also `hostileReview.status` in Pass, Fail, or Other (a missing class is not `Chore`, so following the remedy literally passes whichever class the model then sets); `failed` needs `requestClass` only. Output: the missing items in the order requestClass, agentReview, hostileReview; none means the rule is met.

![Policy CompletionPolicy: the completion rule (activity), part 1 of 3](c4/rendered/CompletionPolicy.svg)

View: CompletionPolicy

![Policy CompletionPolicy: the completion rule (activity), part 2 of 3](c4/rendered/CompletionPolicy-part2.svg)

View: CompletionPolicy-part2

![Policy CompletionPolicy: the completion rule (activity), part 3 of 3](c4/rendered/CompletionPolicy-part3.svg)

View: CompletionPolicy-part3

#### Policy CompletionGate: server enforcement of the completion rule [FR-MCP-SESSIONCLASS-002, -003, -004]

- Applied by: `TurnWritePipeline` (section 3.5.3) for every handler that can move a turn to `completed` or `failed` (SubmitSessionLogCommand submit, UpsertSessionTurnCommand upsert, PatchSessionTurnCommand patch, ReplaceSessionTurnCommand replace, CompleteSessionTurnCommand complete, FailSessionTurnCommand fail), from S10, on the turn as this write will store it (after policies ReviewValidation and RequestClassification).
- Trigger: a write that moves the turn into `completed` or `failed`, or a Replace (PUT) that carries `completed` or `failed` (FR-MCP-SESSIONCLASS-002-AC004); an additive write to an already terminal turn is never gated, and `canceled` and `cancelled` are not gated. A write naming `closed` or any other value outside the agent write set from an agent surface never reaches the gate: `TurnStatus` refuses it with 400 `validation_error` (section 3.5.3).
- Write origins: the enum `WriteOrigin` (`Agent`, the default, `Import`, `ServerInternal`, `Replication`) and its AsyncLocal scope `IWriteOriginAccessor`, in `src/QBrainAi.Services/SessionLog/` (namespace `QBrainAi.Support.Mcp.SessionLog`, owned by SessionTurnLogging with the scope it adds to `SessionLogService.cs`, section 8.3); the exempt writers set their origin through that scope, and no surface can select a non-Agent origin. The gate's tests are `CompletionGateContractTests` and `CompletionGateServiceTests`.
- Exempt origins: ServerInternal (`AuditedAgentCliClient`, `WorkspacePolicyService`, `QuadBrainOpenAiChatService`), Replication (federation apply), Import (`SessionLogIngestor`, `TranscriptSessionLogPersister`). Gated: Agent (all external surfaces), the QBAgent tools of `QuadBrainInternalToolExecutor`, and REPL `importRecovery` (section 3.1, "Completion gate writers"):
  - QBAgent's `mcp_session_turn_complete` (`src/QBrainAi.Support.Mcp/Services/QuadBrainInternalToolExecutor.cs:314-328`, declared at `src/QBrainAi.McpAgent/Hosting/McpHostedAgentToolAdapter.cs:68` and `src/QBrainAi.McpAgent/QBAgentDefinition.cs:15`) stays gated and gains the optional `requestClass`, `agentReview`, and `hostileReview` arguments of CompleteSessionTurnCommand. It already reads its arguments as a `UnifiedRequestEntryDto` (`:323`), so the Phase B fields on that DTO carry them (inference). TR-MCP-SESSIONCLASS-004 is updated to cover this tool beside `src/QBrainAi.McpAgent/SessionLog/SessionLogWorkflow.cs`.
  - REPL `importRecovery` (`src/QBrainAi.Repl.Core/SessionLogWorkflow.cs:326-365`) stays gated: it submits the merged session through SubmitSessionLogCommand (`:361`), so a refusal saves nothing of that submission and names each failing requestId; the import result reports each refused recovered turn with the gate's missing items, and no review is ever fabricated. A recovered status outside the agent write set is refused with 400 `validation_error`. At `a8d3934b` the REPL normalizes every recovered status before the submit (`NormalizeTurnStatus`, `:507-509,725-750`): `completed-local` (`tests/QBrainAi.Repl.Core.Tests/SessionLogWorkflowProductionTests.cs:190`) and `partial` become `completed` (asserted at `:213-215`) and reach the gate as `completed`, `interrupted` becomes `failed`, and `superseded` stays `superseded` (`:738-741`), the one normalized value the server refuses.
- Result: the policy returns the missing items and rejects nothing itself. The caller rejects: a single-turn mode at once, SubmitSessionLogCommand once after every turn is evaluated, naming each failing requestId (FR-MCP-SESSIONCLASS-002-AC005). The rejection is `ClassifiedErrorException` with 400 `validation_error`, the message starting with the token `session_turn_completion_gate` and listing the missing items in order (section 3.4 Errors, FR-MCP-SESSIONCLASS-002-AC006); `SerializableWrite` rolls the write back, so nothing of it is saved and the data context stays reusable (TR-MCP-SESSIONCLASS-002 AC-004).

![Policy CompletionGate: server enforcement of the completion rule (activity), part 1 of 2](c4/rendered/CompletionGate.svg)

View: CompletionGate

![Policy CompletionGate: server enforcement of the completion rule (activity), part 2 of 2](c4/rendered/CompletionGate-part2.svg)

View: CompletionGate-part2

### 5.4 Processes (plugin, REPL, and McpAgent)

#### Process ConvertHvReceiptToReview: convert a hostile-validation receipt into a review write (plugin), then policy HvVerdictMapping [FR-MCP-SESSIONREVIEW-005]

- Plugin side (`ConvertFrom-McpHvReceipt` in the PowerShell core, for every host): input the reviewer's JSON receipt and the response jsonl; output a `recordReview` payload with `correctness = accuracy/100`, `completeness = completeness/100` as exact decimal lexemes, `reasons` (each FAIL and UNKNOWN claim, prefixed `FAIL:` or `UNKNOWN:`), `reviewer`, `reviewedAt`, both jsonl paths, and `hvVerdict` (AGREE only when the receipt's OverallVerdict is AGREE and every claim is PASS, otherwise DISAGREE, FR-MCP-SESSIONREVIEW-005-AC003), and no `status`. It then sends that payload through the `recordReview` verb (process BindVerbToTurn, no failsafe); `hvReceiptPath` is never forwarded.
- Server side: RecordSessionTurnReviewCommand applies policy HvVerdictMapping, the only drawing of the verdict rule; its effective-verdict rule equals the plugin's AGREE rule above, so the plugin and a direct MCP, client, or REPL caller store the same status for the same fields.
- The plugin does not check the reviewer's live model or effort: UC-7's rule (gpt-6-astra, max) is an HV-protocol rule checked from the reviewer's receipts, not a plugin check (section 3.1).
- The HostileReview subsystem (FR-MCP-HOSTILEREVIEW-001 to -006: its durable review queue, its server-owned worker pinned to `gpt-6-astra` at effort `xhigh` by `HostileReviewWorkerOptions`, `src/QBrainAi.Services/Options/HostileReviewWorkerOptions.cs:16-19,79-82`, and its AGREE, DISAGREE, or UNKNOWN verdicts) is separate from a turn's `hostileReview`: its results are not HV receipts for this record, never pass through this process, and are not judged by UC-7's `max` rule. FR-MCP-HOSTILEREVIEW-003 keeps its `xhigh` worker unchanged, and FR-MCP-SESSIONREVIEW-005 is updated with this statement.
- The HV reviewer's brief and its turn: a user prompt that carries a hostile-review brief is a user message and opens its own turn, because the prompt hook classifies a background agent only by the host's subagent markers (process OpenTurnFromPromptHook, section 4.4), never by prompt text; the reviewer records the reviewed turn's `hostileReview` by that turn's original pair. This replaces FR-MCP-TRIAGEPLUGIN-001's text-detected background-brief reuse (`Test-PluginPromptIsBackgroundAgent` and `Get-PluginRootTurnIsolationDecision`, `plugins/core/lib-ps/plugin-hook.ps1:693,725`): the `reuse` and `turn-already-open` expectations of `plugins/core/test-fixtures/pester/TriagePluginIdentity.Tests.ps1:290,327,420` (TEST-MCP-TRIAGEPLUGIN-001, BUG-TRIAGE-121) become a new turn, and their no-cancel expectations still hold, because no prompt changes another turn (section 3.1, independent turn edits).

![Process ConvertHvReceiptToReview: convert a hostile-validation receipt into a review write (plugin), then policy HvVerdictMapping (activity), part 1 of 2](c4/rendered/ConvertHvReceiptToReview.svg)

View: ConvertHvReceiptToReview

![Process ConvertHvReceiptToReview: convert a hostile-validation receipt into a review write (plugin), then policy HvVerdictMapping (activity), part 2 of 2](c4/rendered/ConvertHvReceiptToReview-part2.svg)

View: ConvertHvReceiptToReview-part2

#### Process ClearSession: clear a session (clear-session coordinator) [TR-MCP-CLEARSESSION-001 under FR-MCP-CLEARSESSION-001]

- Surfaces: the clear-session coordinator in the PowerShell core (S8), used by all nine plugins (the TypeScript hosts through their adapters, S9), driven by the clear-session skill in all nine plugins.
- Turns: every request turn of the host session that is still open locally, by its original (`sessionId`, `requestId`) pair: a delivered request still open, a `withdrawn` entry still in progress, and a minted request still `queued`, which clear-session fails as withdrawn ("withdrawn before delivery", section 3.1).
- Each close goes through processes BindVerbToTurn and PersistTurnWriteWithReceipt (section 4.4) by the original pair, and ClearOneOpenTurn maps every receipt result through its nested activity ReportCloseReceipt. In a host session with `unsupportedSince`, `Get-CloseRemedy` counts the turn's captured `unsupported_session` records as written, as the Stop gate does, and a captured close is reported as captured for the next SessionStart drain.
- Output: per open request turn of the host session, one of: closed, closed elsewhere (with its `storedStatus`), close queued in the failsafe (a transport outage, 503 `backend_unavailable`, a busy store's 503 `persistence_error` with `retryable: true`, or a 409 `conflict` with `retryable: true`, the retryable answers the plugin treats as an outage, section 3.1, "Retryable answers"), close captured for the next drain, close unverified (`outcomeVerified: false`), or left in progress with its exact missing items or the refusal code. No review is ever fabricated.

![Process ClearSession: clear a session (clear-session coordinator) (activity)](c4/rendered/ClearSession.svg)

View: ClearSession

##### Nested activity: ClearOneOpenTurn (loop body of ClearSession)

Each open request turn is handled by its original (`sessionId`, `requestId`) pair. A turn the server already reports terminal from any surface is reported closed elsewhere with its `storedStatus`, and nothing is sent. Otherwise the close follows the precedence of section 3.1: a withdrawn request, or a minted request still `queued`, gets `failTurn` with reason "withdrawn before delivery"; a turn whose `blockers` section holds a nonblank model-authored entry gets `failTurn` with the first entry as reason; any other turn gets `completeTurn`. `Get-CloseRemedy` (section 3.5.6) evaluates the chosen close and counts the turn's captured `unsupported_session` records in a host session with `unsupportedSince`. When prerequisites are missing, the turn is left in progress and reported with its `queryText`, the reason, and the missing items in Stop remedy order; the model writes them by the original pair and reruns the coordinator. When only the close call remains, processes BindVerbToTurn and PersistTurnWriteWithReceipt send it by the original pair, and the nested activity ReportCloseReceipt maps the receipt.

![Nested activity: ClearOneOpenTurn (loop body of ClearSession) (activity), part 1 of 4](c4/rendered/ClearOneOpenTurn.svg)

View: ClearOneOpenTurn

![Nested activity: ClearOneOpenTurn (loop body of ClearSession) (activity), part 2 of 4](c4/rendered/ClearOneOpenTurn-part2.svg)

View: ClearOneOpenTurn-part2

![Nested activity: ClearOneOpenTurn (loop body of ClearSession) (activity), part 3 of 4](c4/rendered/ClearOneOpenTurn-part3.svg)

View: ClearOneOpenTurn-part3

![Nested activity: ClearOneOpenTurn (loop body of ClearSession) (activity), part 4 of 4](c4/rendered/ClearOneOpenTurn-part4.svg)

View: ClearOneOpenTurn-part4

##### Nested activity: ReportCloseReceipt (receipt step of ClearOneOpenTurn)

The receipt of process PersistTurnWriteWithReceipt for the chosen close becomes the turn's line in the clear-session report: `persisted` is reported closed; `skipped_terminal` is reported closed elsewhere with its `storedStatus`; `outcomeVerified: false` is reported unverified, not counted closed, and the coordinator is rerun to confirm; a close queued in the failsafe (a transport outage, 503 `backend_unavailable`, a busy store's 503 `persistence_error` with `retryable: true`, or a 409 `conflict` with `retryable: true`, section 3.1, "Retryable answers") is reported queued and is drained later; a close captured as an `unsupported_session` record is reported captured for the next SessionStart drain; a refusal or another server code leaves the turn in progress, reported with the code and, for `session_turn_completion_gate`, the server's missing items.

![Nested activity: ReportCloseReceipt (receipt step of ClearOneOpenTurn) (activity), part 1 of 3](c4/rendered/ReportCloseReceipt.svg)

View: ReportCloseReceipt

![Nested activity: ReportCloseReceipt (receipt step of ClearOneOpenTurn) (activity), part 2 of 3](c4/rendered/ReportCloseReceipt-part2.svg)

View: ReportCloseReceipt-part2

![Nested activity: ReportCloseReceipt (receipt step of ClearOneOpenTurn) (activity), part 3 of 3](c4/rendered/ReportCloseReceipt-part3.svg)

View: ReportCloseReceipt-part3

#### Process AdvisoryCompletionPrecheck: advisory completion pre-check (plugins, REPL, McpAgent) [FR-MCP-SESSIONCLASS-004]

- Run by: the plugin close verbs (S8, S9), the REPL workflow (S6), and McpAgent (S7) before they send the close (a turn at status `completed` or `failed` through SubmitSessionLogCommand, the route all three use). The plugin calls `Get-CloseRemedy` (section 3.5.6) in advisory mode; the REPL and McpAgent call policy CompletionPolicy. It never refuses: policy CompletionGate on the server is the only authority (FR-MCP-SESSIONCLASS-004-AC002, TR-MCP-SESSIONREVIEW-004 AC-004, TR-MCP-SESSIONCLASS-004 AC-003).
- A class or review recorded by another process, host session, or surface before the close is honored and is not reported missing. In a host session with `unsupportedSince`, the warning still prints and process BindVerbToTurn then captures the close for the drain.

![Process AdvisoryCompletionPrecheck: advisory completion pre-check (plugins, REPL, McpAgent) (activity)](c4/rendered/AdvisoryCompletionPrecheck.svg)

View: AdvisoryCompletionPrecheck

#### Process PreserveReviewScoreLexemes: lossless score handling in the REPL [FR-MCP-SESSIONREVIEW-004]

- Run by: `IReplJsonNumberNormalizer` inside `ReplCommandDispatcher.NormalizeJsonElement` and `GenericClientPassthrough.NormalizeForJson` (S6). It holds no lexeme rule of its own: it calls `ScoreLexeme` (`QBrainAi.Client.Scoring`, section 3.5.4), the same rule `ScoreLexemeJsonConverter` applies on the server and the client.
- Keys: `correctness` and `completeness` inside `agentReview`, `hostileReview`, or `recordReview`, and inside the request of `client.AgentContext.RecordModelGradeAsync` (the HV agent's model grades). `verdict_match` (section 6.3) compares score lexemes, and these lexemes reach the server exactly as the reviewer wrote them; the normalized number a read surface returns (trailing zeros stripped) is never the compared value.

![Process PreserveReviewScoreLexemes: lossless score handling in the REPL (activity), part 1 of 2](c4/rendered/PreserveReviewScoreLexemes.svg)

View: PreserveReviewScoreLexemes

![Process PreserveReviewScoreLexemes: lossless score handling in the REPL (activity), part 2 of 2](c4/rendered/PreserveReviewScoreLexemes-part2.svg)

View: PreserveReviewScoreLexemes-part2

### 5.5 Phase B sequence diagrams

#### Sequence: submit RecordSessionTurnReviewCommand and process each response

`A` is the model or the clear-session coordinator, `P` the plugin's `recordReview` verb, `R` `qbrain-ai-repl`, and `S` the surface: `SessionLogController` (PUT on the review section), or the MCP tool `sessionlog_record_review` or `sessionlog_replace_section` with a review section. The verb carries the kind and the review input or `hvReceiptPath`, with an optional `requestId` or original pair. Process BindVerbToTurn binds it: an unqualified `recordReview` binds to the write target even when that turn is closed (FR-MCP-SESSIONTURN-004-AC004), and a binding refusal is `request_unknown`, `request_ambiguous`, `host_session_unknown`, or `host_session_ambiguous`, with nothing sent. A host session reported `turn_logging_unsupported` refuses the review, which is recorded after the drain through the original pair. Before any write the plugin refuses an invalid receipt with its exact code (process ConvertHvReceiptToReview) and an invalid score lexeme with `review_score_format`, `review_score_precision`, or `review_score_out_of_range` (`Protect-ReplLosslessScalars`); the REPL normalizer calls `ScoreLexeme`, so the lexemes reach the server unchanged. A review is never captured in the failsafe (an older review must never overwrite a newer one, section 3.1, "Retryable answers"), so a transport outage or a retryable 503 (`backend_unavailable`, or a busy store's `persistence_error` with `retryable: true`) is reported with its code and `retryable: true`, not queued, exit 1, and the model records the review again after the drain (section 5.2, RecordSessionTurnReviewCommand). The server side, with each answer and the plugin's report of it, is the nested sequence ServeReviewWrite.

![Sequence: submit RecordSessionTurnReviewCommand and process each response, part 1 of 2](c4/rendered/STR-05-02.svg)

View: STR-05-02

![Sequence: submit RecordSessionTurnReviewCommand and process each response, part 2 of 2](c4/rendered/STR-05-02-part2.svg)

View: STR-05-02-part2

![Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 1 branch 1 of 5: binding refused](c4/rendered/STR-05-02-alt1-1.svg)

View: STR-05-02-alt1-1

![Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 1 branch 2 of 5: turn_logging_unsupported](c4/rendered/STR-05-02-alt1-2.svg)

View: STR-05-02-alt1-2

![Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 1 branch 3 of 5: receipt invalid (ConvertHvReceiptToReview)](c4/rendered/STR-05-02-alt1-3.svg)

View: STR-05-02-alt1-3

![Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 1 branch 4 of 5: score lexeme invalid (Protect-ReplLosslessScalars)](c4/rendered/STR-05-02-alt1-4.svg)

View: STR-05-02-alt1-4

![Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 1 branch 5 of 5: bound, part 1 of 2](c4/rendered/STR-05-02-alt1-5.svg)

View: STR-05-02-alt1-5

![Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 1 branch 5 of 5: bound, part 2 of 2](c4/rendered/STR-05-02-alt1-5-part2.svg)

View: STR-05-02-alt1-5-part2

![Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 2 branch 1 of 2: transport outage or retryable 503, part 1 of 2](c4/rendered/STR-05-02-alt2-1.svg)

View: STR-05-02-alt2-1

![Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 2 branch 1 of 2: transport outage or retryable 503, part 2 of 2](c4/rendered/STR-05-02-alt2-1-part2.svg)

View: STR-05-02-alt2-1-part2

![Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 2 branch 2 of 2: answered, part 1 of 3](c4/rendered/STR-05-02-alt2-2.svg)

View: STR-05-02-alt2-2

![Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 2 branch 2 of 2: answered, part 2 of 3](c4/rendered/STR-05-02-alt2-2-part2.svg)

View: STR-05-02-alt2-2-part2

![Sequence: submit RecordSessionTurnReviewCommand and process each response, alt 2 branch 2 of 2: answered, part 3 of 3](c4/rendered/STR-05-02-alt2-2-part3.svg)

View: STR-05-02-alt2-2-part3

##### Nested sequence: ServeReviewWrite (server step of the recordReview sequence)

`P` is the plugin reaching the surface through `qbrain-ai-repl`, `S` the controller or MCP tool, and `L` `IDispatcher` with `RecordSessionTurnReviewCommandHandler`. An unknown section and a score lexeme that `ScoreLexemeJsonConverter` refuses are answered before the dispatch and before any lookup. After `SendAsync`, `WorkspaceResolutionBehavior` answers an unresolved workspace; then the handler applies policies HvVerdictMapping and ReviewValidation and saves through `ISessionLogService.ReplaceTurnSectionAsync` (TurnLocator and the save inside SerializableWrite). A saved record returns with its derived status and stamped thresholds; 422 `review_status_conflict` carries the thresholds in force and the derived status, and the plugin resends without a status or with the derived one; a 400 is `validation_error` or a review code (`hvVerdict`, `status`, an invalid identifier, or a policy code); a missing session or turn is reported `request_unknown`; a 409 `conflict` with `retryable: true` is reported with code `conflict` and `retryable: true`, not queued, exit 1, and the model records the review again after the drain (section 5.2, RecordSessionTurnReviewCommand).

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence)](c4/rendered/ServeReviewWrite.svg)

View: ServeReviewWrite

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 1 branch 1 of 2: unknown section, or score invalid at deserialization, part 1 of 3](c4/rendered/ServeReviewWrite-alt1-1.svg)

View: ServeReviewWrite-alt1-1

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 1 branch 1 of 2: unknown section, or score invalid at deserialization, part 2 of 3](c4/rendered/ServeReviewWrite-alt1-1-part2.svg)

View: ServeReviewWrite-alt1-1-part2

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 1 branch 1 of 2: unknown section, or score invalid at deserialization, part 3 of 3](c4/rendered/ServeReviewWrite-alt1-1-part3.svg)

View: ServeReviewWrite-alt1-1-part3

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 1 branch 2 of 2: body bound, part 1 of 2](c4/rendered/ServeReviewWrite-alt1-2.svg)

View: ServeReviewWrite-alt1-2

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 1 branch 2 of 2: body bound, part 2 of 2](c4/rendered/ServeReviewWrite-alt1-2-part2.svg)

View: ServeReviewWrite-alt1-2-part2

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 1 of 6: workspace unresolved (WorkspaceResolutionBehavior), part 1 of 2](c4/rendered/ServeReviewWrite-alt2-1.svg)

View: ServeReviewWrite-alt2-1

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 1 of 6: workspace unresolved (WorkspaceResolutionBehavior), part 2 of 2](c4/rendered/ServeReviewWrite-alt2-1-part2.svg)

View: ServeReviewWrite-alt2-1-part2

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 2 of 6: saved (policies, then SerializableWrite), part 1 of 3](c4/rendered/ServeReviewWrite-alt2-2.svg)

View: ServeReviewWrite-alt2-2

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 2 of 6: saved (policies, then SerializableWrite), part 2 of 3](c4/rendered/ServeReviewWrite-alt2-2-part2.svg)

View: ServeReviewWrite-alt2-2-part2

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 2 of 6: saved (policies, then SerializableWrite), part 3 of 3](c4/rendered/ServeReviewWrite-alt2-2-part3.svg)

View: ServeReviewWrite-alt2-2-part3

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 3 of 6: 422 review_status_conflict (ScoreVerdict), part 1 of 2](c4/rendered/ServeReviewWrite-alt2-3.svg)

View: ServeReviewWrite-alt2-3

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 3 of 6: 422 review_status_conflict (ScoreVerdict), part 2 of 2](c4/rendered/ServeReviewWrite-alt2-3-part2.svg)

View: ServeReviewWrite-alt2-3-part2

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 4 of 6: 400 review code, hvVerdict, status, or id, part 1 of 2](c4/rendered/ServeReviewWrite-alt2-4.svg)

View: ServeReviewWrite-alt2-4

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 4 of 6: 400 review code, hvVerdict, status, or id, part 2 of 2](c4/rendered/ServeReviewWrite-alt2-4-part2.svg)

View: ServeReviewWrite-alt2-4-part2

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 5 of 6: session or turn not found (TurnLocator), part 1 of 2](c4/rendered/ServeReviewWrite-alt2-5.svg)

View: ServeReviewWrite-alt2-5

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 5 of 6: session or turn not found (TurnLocator), part 2 of 2](c4/rendered/ServeReviewWrite-alt2-5-part2.svg)

View: ServeReviewWrite-alt2-5-part2

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 6 of 6: SerializableWrite bound exhausted, part 1 of 2](c4/rendered/ServeReviewWrite-alt2-6.svg)

View: ServeReviewWrite-alt2-6

![Nested sequence: ServeReviewWrite (server step of the recordReview sequence), alt 2 branch 6 of 6: SerializableWrite bound exhausted, part 2 of 2](c4/rendered/ServeReviewWrite-alt2-6-part2.svg)

View: ServeReviewWrite-alt2-6-part2

#### Sequence: submit ResetSessionTurnReviewCommand and process each response

![Sequence: submit ResetSessionTurnReviewCommand and process each response, part 1 of 6](c4/rendered/STR-05-03.svg)

View: STR-05-03

![Sequence: submit ResetSessionTurnReviewCommand and process each response, part 2 of 6](c4/rendered/STR-05-03-part2.svg)

View: STR-05-03-part2

![Sequence: submit ResetSessionTurnReviewCommand and process each response, part 3 of 6](c4/rendered/STR-05-03-part3.svg)

View: STR-05-03-part3

![Sequence: submit ResetSessionTurnReviewCommand and process each response, part 4 of 6](c4/rendered/STR-05-03-part4.svg)

View: STR-05-03-part4

![Sequence: submit ResetSessionTurnReviewCommand and process each response, part 5 of 6](c4/rendered/STR-05-03-part5.svg)

View: STR-05-03-part5

![Sequence: submit ResetSessionTurnReviewCommand and process each response, part 6 of 6](c4/rendered/STR-05-03-part6.svg)

View: STR-05-03-part6

![Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 1 branch 1 of 2: unknown section](c4/rendered/STR-05-03-alt1-1.svg)

View: STR-05-03-alt1-1

![Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 1 branch 2 of 2: review section, part 1 of 2](c4/rendered/STR-05-03-alt1-2.svg)

View: STR-05-03-alt1-2

![Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 1 branch 2 of 2: review section, part 2 of 2](c4/rendered/STR-05-03-alt1-2-part2.svg)

View: STR-05-03-alt1-2-part2

![Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 1 of 6: workspace not resolved (WorkspaceResolutionBehavior)](c4/rendered/STR-05-03-alt2-1.svg)

View: STR-05-03-alt2-1

![Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 2 of 6: invalid session or request id (TurnLocator)](c4/rendered/STR-05-03-alt2-2.svg)

View: STR-05-03-alt2-2

![Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 3 of 6: session or turn not found (TurnLocator)](c4/rendered/STR-05-03-alt2-3.svg)

View: STR-05-03-alt2-3

![Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 4 of 6: turn completed or failed](c4/rendered/STR-05-03-alt2-4.svg)

View: STR-05-03-alt2-4

![Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 5 of 6: turn in_progress, canceled, cancelled, or closed, part 1 of 3](c4/rendered/STR-05-03-alt2-5.svg)

View: STR-05-03-alt2-5

![Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 5 of 6: turn in_progress, canceled, cancelled, or closed, part 2 of 3](c4/rendered/STR-05-03-alt2-5-part2.svg)

View: STR-05-03-alt2-5-part2

![Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 5 of 6: turn in_progress, canceled, cancelled, or closed, part 3 of 3](c4/rendered/STR-05-03-alt2-5-part3.svg)

View: STR-05-03-alt2-5-part3

![Sequence: submit ResetSessionTurnReviewCommand and process each response, alt 2 branch 6 of 6: SerializableWrite bound exhausted](c4/rendered/STR-05-03-alt2-6.svg)

View: STR-05-03-alt2-6

#### Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response

A transport outage, a retryable 503 (`backend_unavailable`, or a busy store's `persistence_error` with `retryable: true`), and a 409 `conflict` with `retryable: true` keep the failsafe record and are reported `queued`, exit 0 (section 3.1, "Retryable answers").

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, part 1 of 4](c4/rendered/STR-05-04.svg)

View: STR-05-04

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, part 2 of 4](c4/rendered/STR-05-04-part2.svg)

View: STR-05-04-part2

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, part 3 of 4](c4/rendered/STR-05-04-part3.svg)

View: STR-05-04-part3

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, part 4 of 4](c4/rendered/STR-05-04-part4.svg)

View: STR-05-04-part4

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 1 branch 1 of 2: binding refused](c4/rendered/STR-05-04-alt1-1.svg)

View: STR-05-04-alt1-1

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 1 branch 2 of 2: bound, part 1 of 4](c4/rendered/STR-05-04-alt1-2.svg)

View: STR-05-04-alt1-2

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 1 branch 2 of 2: bound, part 2 of 4](c4/rendered/STR-05-04-alt1-2-part2.svg)

View: STR-05-04-alt1-2-part2

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 1 branch 2 of 2: bound, part 3 of 4](c4/rendered/STR-05-04-alt1-2-part3.svg)

View: STR-05-04-alt1-2-part3

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 1 branch 2 of 2: bound, part 4 of 4](c4/rendered/STR-05-04-alt1-2-part4.svg)

View: STR-05-04-alt1-2-part4

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 2 branch 1 of 2: transport outage or retryable 503, part 1 of 2](c4/rendered/STR-05-04-alt2-1.svg)

View: STR-05-04-alt2-1

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 2 branch 1 of 2: transport outage or retryable 503, part 2 of 2](c4/rendered/STR-05-04-alt2-1-part2.svg)

View: STR-05-04-alt2-1-part2

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 2 branch 2 of 2: answered, part 1 of 3](c4/rendered/STR-05-04-alt2-2.svg)

View: STR-05-04-alt2-2

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 2 branch 2 of 2: answered, part 2 of 3](c4/rendered/STR-05-04-alt2-2-part2.svg)

View: STR-05-04-alt2-2-part2

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 2 branch 2 of 2: answered, part 3 of 3](c4/rendered/STR-05-04-alt2-2-part3.svg)

View: STR-05-04-alt2-2-part3

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 1 of 3: valid class, part 1 of 4](c4/rendered/STR-05-04-alt3-1.svg)

View: STR-05-04-alt3-1

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 1 of 3: valid class, part 2 of 4](c4/rendered/STR-05-04-alt3-1-part2.svg)

View: STR-05-04-alt3-1-part2

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 1 of 3: valid class, part 3 of 4](c4/rendered/STR-05-04-alt3-1-part3.svg)

View: STR-05-04-alt3-1-part3

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 1 of 3: valid class, part 4 of 4](c4/rendered/STR-05-04-alt3-1-part4.svg)

View: STR-05-04-alt3-1-part4

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 2 of 3: invalid class, part 1 of 2](c4/rendered/STR-05-04-alt3-2.svg)

View: STR-05-04-alt3-2

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 2 of 3: invalid class, part 2 of 2](c4/rendered/STR-05-04-alt3-2-part2.svg)

View: STR-05-04-alt3-2-part2

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 3 of 3: SerializableWrite bound exhausted, part 1 of 2](c4/rendered/STR-05-04-alt3-3.svg)

View: STR-05-04-alt3-3

![Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response, alt 3 branch 3 of 3: SerializableWrite bound exhausted, part 2 of 2](c4/rendered/STR-05-04-alt3-3-part2.svg)

View: STR-05-04-alt3-3-part2

The same policy runs when a client, REPL, or MCP caller sends `requestClass` through PatchSessionTurnCommand, UpsertSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, or FailSessionTurnCommand; their sequences in section 4.5 carry the `request_class_invalid` branch.

#### Sequence: submit GetSessionLogQuery and process the response

![Sequence: submit GetSessionLogQuery and process the response, part 1 of 2](c4/rendered/STR-05-05.svg)

View: STR-05-05

![Sequence: submit GetSessionLogQuery and process the response, part 2 of 2](c4/rendered/STR-05-05-part2.svg)

View: STR-05-05-part2

![Sequence: submit GetSessionLogQuery and process the response, alt 1 branch 1 of 4: workspace not resolved (WorkspaceResolutionBehavior)](c4/rendered/STR-05-05-alt1-1.svg)

View: STR-05-05-alt1-1

![Sequence: submit GetSessionLogQuery and process the response, alt 1 branch 2 of 4: session id invalid for the agent (TurnLocator)](c4/rendered/STR-05-05-alt1-2.svg)

View: STR-05-05-alt1-2

![Sequence: submit GetSessionLogQuery and process the response, alt 1 branch 3 of 4: session not found (TurnLocator)](c4/rendered/STR-05-05-alt1-3.svg)

View: STR-05-05-alt1-3

![Sequence: submit GetSessionLogQuery and process the response, alt 1 branch 4 of 4: found, part 1 of 3](c4/rendered/STR-05-05-alt1-4.svg)

View: STR-05-05-alt1-4

![Sequence: submit GetSessionLogQuery and process the response, alt 1 branch 4 of 4: found, part 2 of 3](c4/rendered/STR-05-05-alt1-4-part2.svg)

View: STR-05-05-alt1-4-part2

![Sequence: submit GetSessionLogQuery and process the response, alt 1 branch 4 of 4: found, part 3 of 3](c4/rendered/STR-05-05-alt1-4-part3.svg)

View: STR-05-05-alt1-4-part3

#### Sequence: submit QuerySessionLogsQuery and process the response

![Sequence: submit QuerySessionLogsQuery and process the response, part 1 of 2](c4/rendered/STR-05-06.svg)

View: STR-05-06

![Sequence: submit QuerySessionLogsQuery and process the response, part 2 of 2](c4/rendered/STR-05-06-part2.svg)

View: STR-05-06-part2

![Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 1 of 3: workspace not resolved (WorkspaceResolutionBehavior)](c4/rendered/STR-05-06-alt1-1.svg)

View: STR-05-06-alt1-1

![Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 2 of 3: session-log migration pending](c4/rendered/STR-05-06-alt1-2.svg)

View: STR-05-06-alt1-2

![Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 3 of 3: schema current, part 1 of 5](c4/rendered/STR-05-06-alt1-3.svg)

View: STR-05-06-alt1-3

![Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 3 of 3: schema current, part 2 of 5](c4/rendered/STR-05-06-alt1-3-part2.svg)

View: STR-05-06-alt1-3-part2

![Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 3 of 3: schema current, part 3 of 5](c4/rendered/STR-05-06-alt1-3-part3.svg)

View: STR-05-06-alt1-3-part3

![Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 3 of 3: schema current, part 4 of 5](c4/rendered/STR-05-06-alt1-3-part4.svg)

View: STR-05-06-alt1-3-part4

![Sequence: submit QuerySessionLogsQuery and process the response, alt 1 branch 3 of 3: schema current, part 5 of 5](c4/rendered/STR-05-06-alt1-3-part5.svg)

View: STR-05-06-alt1-3-part5

## 6. Phase C: stateless agent context

### 6.1 Scope and principles

- Each agent invocation can run stateless: it receives a bounded, reproducible context package instead of carrying conversation history forward. In `Stateless` the model's input is the rendered package, the current request, and the deterministic `REQUIRED MEMORIES` block the host already injects (FR-MCP-MEMORY-003, -007, which hold unchanged; section 3.1, "Memories in Stateless mode"); history stays excluded.
- A package contains the current request, the canonical operator profile and standing rules, the applicable requirements, related session-log evidence, trusted prior responses, and relevant code at the current commit, each with its provenance.
- The relational store is authoritative for packages, responses, grades, and relationships. Response artifacts are projected into GraphRAG for semantic retrieval, with a deterministic relational fallback that gives the same result whenever GraphRAG is disabled, uninitialized, or unavailable.
- A response becomes trusted only when a deterministic evidence grade and an independent model grade both reach correctness and completeness of at least 98 percent with zero FAIL and zero UNKNOWN findings. The model grade is recorded by the HV agent for the responses of an HV request that names a range of session-log turns; this effort has no grading worker or HV pipeline (operator, 2026-10-11). Rejected, stale, and superseded responses are kept for audit and never retrieved as trusted context.
- Mandatory context is never truncated: when it exceeds the token budget, package construction fails explicitly, and in `Pilot` or `Stateless` mode the invocation fails: no answer is produced (operator, 2026-10-11: "Fail the invocation").
- The mode is opt-in (`Mcp:AgentContext:Mode`); the current stateful behavior stays the default during the pilot. In `Off` the agent-context commands are refused and every stored record stays readable for audit (section 6.2).
- Requirement ids (reserved for Phase 1, the plan's P0 steps 1-9, and created there with acceptance criteria, mappings, and use cases): FR-MCP-AGENTCONTEXT-001 package construction and provenance; -002 token budget and overflow; -003 response submission; -004 grading, HV requests, and trust; -005 rejection, staleness, and supersession; -006 typed relationships; -007 GraphRAG projection and fallback; -008 REST, MCP, client, REPL, and plugin surfaces; -009 opt-in mode and pilot. The matching TR-MCP-AGENTCONTEXT-* and TEST-MCP-AGENTCONTEXT-* records are created in Phase 1.
- Existing records and stores this phase meets (the reconciliation, section 3.1), each with its relationship; none changes behavior unless the bullet names an update:
  - FR-SUPPORT-010 context pack (`ContextPack`, `src/QBrainAi.Services/Models/ContextPack.cs:3-16`, `POST /qbrainai/context/pack`, `src/QBrainAi.Support.Mcp/Controllers/ContextController.cs:164-165`): related and separate, no reuse and no supersede. It returns ranked context chunks for a query; a context package is a stored, hashed, replayable snapshot of one request's mandatory and ranked sources. The FR-MCP-AGENTCONTEXT-001 record states the distinction as a glossary entry.
  - FR-MCP-082 bounded execution context (`TodoExecutionService.GetExecutionContextAsync`, `src/QBrainAi.Services/Services/TodoExecutionService.cs:456`): related, not reused, cross-referenced from FR-MCP-AGENTCONTEXT-001. It summarizes one TODO's requirements and recent turns for resuming work; a package cites the TODO, requirement, and turn records themselves, each with the source hash staleness needs, which a summary does not carry.
  - The FR-MCP-HOSTILEREVIEW queue (requests with resolved artifact links, `src/QBrainAi.Services/Services/HostileReviewService.cs:290`, executions, findings, and its server-owned worker): separate. An HV request of this phase is a turn range an external HV agent grades, because this effort builds no grading worker (section 3.1); only RecordModelGradeCommand, after its eight checks, records a model grade, so a hostile review result is never one. The worker's identity (agent `Astra`, model `gpt-6-astra`, effort `xhigh`, enforced at startup, `src/QBrainAi.Services/Options/HostileReviewWorkerOptions.cs:13,16,19,77-82`) and `ModelGrader:*` (section 6.2) are separate option sets, so neither constrains the other; FR-MCP-HOSTILEREVIEW-003 is unchanged by this phase.
  - `MemoryEdgeEntity` (TR-MCP-MEMORY-MODEL-002, `src/QBrainAi.Storage/Entities/MemoryEdgeEntity.cs:5-32`): strengthen. The separate link store is justified against it beside the other link stores (section 6.3, `AgentContextRelationshipEntity`).
  - GraphRAG: reuse. Projection calls `IGraphRagService.IngestTextAsync` and `DeleteDocumentAsync` (`src/QBrainAi.GraphRag/Services/IGraphRagService.cs:35,46`) and retrieval reports develop's fallback reasons (`src/QBrainAi.GraphRag/Services/GraphRagService.cs:880-889`). The registered service refuses those mutations while required turn transactions are active (`TransactionGatedGraphRagService`, TR-MCP-TXN-001, `src/QBrainAi.Support.Mcp/Services/TransactionGatedGraphRagService.cs:68-74,89-93`); a projection save then records the row `Failed` with `LastError`, and a later pass retries it (section 6.3, projection row states).
  - Handoff idempotency: reuse. IdempotentCreate keeps the rule of TR-HANDOFF-TODO-001 (`src/QBrainAi.Services/Services/TodoPayloadFingerprint.cs:10`: the same key with an equal fingerprint returns the stored record, any other input is a conflict), and `HandoffDbExceptions.IsCommitAmbiguous` keeps its meaning (section 3.5.2).
  - FR-MCP-130 AC 3 ("Only CuriosityEngine may request GraphRAG/context admission") governs QuadBrain brain-slot output (its description), and `src/QBrainAi.GraphRag` holds no Curiosity check (git grep at `a8d3934b`); projecting a trusted agent response is no brain-slot admission (updates FR-MCP-130 AC 3's wording to brain-slot output).
  - FR-MCP-103 federation: strengthen. AC 1 enumerates every mutable state domain, so `agent_context` joins `FederationStateAdapterRegistry.RequiredDomains` (`src/QBrainAi.Services/Services/FederationStateAdapters.cs:128-142`) as a local-only domain through the existing `LocalOnlyFederationStateAdapter` registration (`src/QBrainAi.Services/Services/FederationAdapters/FederationStateAdapterServiceCollectionExtensions.cs:23-37`) and `InferDomain` maps `/qbrainai/agent-context` to it (`src/QBrainAi.Services/Services/FederationProxyService.cs:440-469`): it is never queued during a hub outage (`CanQueueDomain`, `:332-338`) and never applied from hub fanout (`src/QBrainAi.Services/Services/FederationOperationApplyService.cs:32-39`). A LocalProxy still forwards these routes live to the hub (AC 2, `FederationProxyService.ProxyAsync`, `:78`), so (inference) the hub's server holds the records and reads the commit from its own checkout of the workspace path, and answers 409 `agent_context_commit_unavailable` when it has none.
  - FR-MCP-WIKIEXPORT-003: strengthen. Each new `DbSet` gets an explicit dump policy in `WikiDumpTablePolicyRegistry.PolicyFor` (`src/QBrainAi.Services/Services/WikiDumpTablePolicyRegistry.cs:19-43`): `COPY-SANITIZE+HISTORY+INERT`, the policy of the HostileReview tables (`:35-36`), for every agent-context table except `AgentContextProjections`, which is `OMIT-LOCAL` (`:28`) because its rows name GraphRAG documents of the source workspace (inference: a copied row would point at a document the destination does not hold). The registry coverage test (`tests/QBrainAi.Support.Mcp.Tests/Services/WikiDumpG7OverlayTests.cs:56-71`) covers the new sets.

### 6.2 Configuration: `AgentContextOptions`

Section `Mcp:AgentContext` (declared under `QBrainAi: AgentContext:` in `appsettings.yaml`, projected onto `Mcp:` by `CanonicalSectionProjectionProvider`), class `AgentContextOptions` in `src/QBrainAi.Services/AgentContext/AgentContextOptions.cs` (namespace `QBrainAi.Support.Mcp.AgentContext`) with `SectionName = "Mcp:AgentContext"`, a `Validate()` method, and `ValidateOnStart()`.

- `Mode` (enum `AgentContextMode`: `Off`, `Pilot`, `Stateless`; default `Off`). `Off`: ModeGateBehavior (section 3.5.1) refuses every agent-context command except DecideAgentResponseTrustCommand (dispatched only by the grade units) with 409 `agent_context_disabled`; GetAgentContextStatusQuery and every read-only agent-context query stay available, so packages, responses, grades, relationships, HV requests, and the pilot report stay readable for audit; a requirement update or delete dispatches EvaluateStalenessCommand only while Mode is not `Off`. `Pilot`: packages are built and responses graded, but plugins keep the stateful conversation (no injection). `Stateless`: plugins inject the package beside the host's `REQUIRED MEMORIES` block and carry no history, on a host with an authoritative history-exclusion operation recorded in its P0.2 row; any other host treats `Stateless` as `Pilot` (section 6.6). Plugin reading of the mode (section 3.1, resolutions of round 5): the plugin reads `mode` through GetAgentContextStatusQuery at host session start (StartHostSession, or the first prompt on a host without a SessionStart event: cline, cline-v2, and opencode) and caches it for that host session as `agentContextMode` in its `turn-state.yaml` through `Update-TurnState` (section 3.4, plugin per-turn store); that read is the plugin's only agent-context read at host session start, and the query counts responses by effective status with one source read per distinct dependency and commit, so it answers within the SessionStart hook bound (section 6.4, GetAgentContextStatusQuery); a `mode` it cannot read, including a read that times out or misses the hook bound, or a value it does not know, is treated as `Off`; a 409 `agent_context_disabled` answer to a build or a submission caches `Off`, and the host session continues statefully (no refusal, no `canceled` close); a change away from `Off` takes effect at the next host session start. A host whose prompt hook cannot refuse a prompt, or that runs the prompt anyway after a hook timeout, crash, or `plugin_core_unavailable`, runs `Pilot` and `Stateless` as `Off` and records that as a feature-matrix limitation (P0.2 probe (j) records each host's refusal capability); a message absorbed into a running turn (`absorbed_mid_turn`) runs as `Pilot`.
- `TokenBudget` (int, default 32000, must be 1024..1000000).
- `BytesPerToken` (int, default 4, must be 1..16): the deterministic token estimate is `ceil(utf8ByteCount / BytesPerToken)` per item.
- `Trust:Correctness`, `Trust:Completeness` (decimal in [0.98, 1], default 0.98, inclusive): the attached plan's minimum of 98 percent is a floor that configuration cannot lower; a value below 0.98 or above 1 fails startup validation (`AgentContextOptionsTrustBelow098FailsStartup`, `AgentContextOptionsTrustAt098And1Accepted`). RunDeterministicGradeCommand and RecordModelGradeCommand derive and stamp each grade's verdict through ScoreVerdict (section 3.5.4) with `max(configured, 0.98)`; DecideAgentResponseTrustCommand never reads a stored verdict: it re-derives the verdict of each latest grade from that grade's stored scores and finding counts through ScoreVerdict against `max(configured now, 0.98)`, so no stored verdict or earlier threshold below the floor can trust a response (`TrustStoredThresholdBelowFloorFloorApplied`) and a raised threshold demotes a `Trusted` response at its next trust decision (`TrustThresholdRaisedAfterGradeAppliedAtNextDecision`). The Phase B review thresholds (FR-MCP-SESSIONREVIEW-007) stay configurable as approved.
- `MaxCandidatesPerKind` (int, default 50, 1..1000) and `MaxPriorResponses` (int, default 10, 0..100): after filtering and ranking, each optional kind keeps at most `MaxCandidatesPerKind` candidates in rank order (`PriorResponse` at most `MaxPriorResponses`), and the rest are omitted with reason `below_candidate_limit`; mandatory items are never cut by these limits (`BuildCandidatesBeyondPerKindLimitOmittedBelowCandidateLimit`, `BuildMaxPriorResponsesZeroNoPriorResponseItem`).
- `ProfileSources` (list of file globs resolved on the server host; default `~/.claude/profile/*.md` excluding `add-profile*.md`, the set the `/add-profile` skill reads) and `RuleSources` (list of workspace-relative paths; default `CLAUDE.md`, `AGENTS.md`, and `AGENTS-README-FIRST.yaml`, secret values redacted through SecretRedactor). Profile and rules are mandatory: when no profile file or no rule file resolves, the build fails with 409 `agent_context_profile_unavailable` instead of building a package without them. A rule file outside version control (the default `AGENTS-README-FIRST.yaml`, regenerated at every server start) is a package item with its hash for provenance and never a staleness dependency (no `derived_from` link), so a server restart does not make responses stale (`BuildUntrackedRuleSourceNoDerivedFromLinkHashKeptOnItem`, `StalenessServerRestartRegeneratesMarkerTrustedResponseStaysTrusted`).
- `ModelGrader:AgentName` (default `Codex`), `ModelGrader:RequiredModel` (default `gpt-6-astra`), `ModelGrader:RequiredEffort` (default `max`), `ModelGrader:ReceiptRoot` (workspace-relative, default `docs/receipts/hv`): a model grade from any other grader, or whose receipts are outside the receipt root or do not verify, is refused.
- `HvRequest:MaxTurns` (int, default 200, 1..2000): the largest turn range one HV request may name.
- `GraphRag:ProjectionEnabled` (bool, default true): false skips ingest only; removal of a projected document still runs while GraphRAG is enabled (section 6.3, projection row states). `GraphRag:UseForRetrieval` (bool, default true): BuildAgentContextPackageCommand retrieves its candidates with `preferGraphRag` equal to this value, recorded in `OptionsSnapshotJson`, and every candidate's ranking inputs, GraphRAG scores included, are part of `InputsSha256` through `RankingFeaturesJson`, so two builds that rank differently never share it (`BuildGraphRagScoreChangedSameSourcesDifferentInputsSha256`).
- Validation failures (startup): an unknown `Mode`, `TokenBudget`, `BytesPerToken`, `HvRequest:MaxTurns`, thresholds outside their ranges, an empty `RuleSources`, an empty grader identity, or an empty `ModelGrader:ReceiptRoot`. These ranges are enforced at server start by `Validate()` with `ValidateOnStart()` and proven by its G2 cases; they are not a G5 (`ValidateConfig`) check, an exclusion TR-MCP-NUKE-004 states (section 8.4).

### 6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`)

Every entity below is a new type in `src/QBrainAi.Storage/Entities/AgentContext/`, so its namespace follows the folder rule of section 3.5; develop's existing entities keep `QBrainAi.Support.Mcp.Storage.Entities`. Table names keep the `AgentContext` or `AgentResponse` prefix because the database has no namespace.

Common rules:

- Readable keys: string keys `[StringLength(128)]`, human-readable and derived from the records they belong to (formats per entity below), allocated by ReadableKeyAllocator (section 3.5.2, strategy `NextOrdinal`). A key is `<prefix><parentId><tail>` when that is at most 128 characters; otherwise the embedded parent id is shortened to its first `128 - length(prefix) - length(tail) - 13` characters followed by `-` and the first 12 hex digits of the SHA-256 of the full parent id, so keys stay readable, deterministic, and distinct for distinct parents. The complete parent ids are always stored in their own root-id columns, which keep the live length of 256 (`SessionLogEntity.SessionId` and `SessionLogTurnEntity.RequestId` allow 256), so no root id is ever truncated (`ReadableKeyAllocatorMaxLengthRootIdsFitAndStayDistinct` on SQLite, PostgreSQL, and SQL Server; `ReadableKeyAllocatorShortRootIdsKeepFullReadableForm`). The sequence suffix `<nn>` has at least two digits, starts at 01, is one more than the highest stored suffix for that prefix in the workspace compared as a number, and widens past 99 (100, 101, and so on) (`ReadableKeyAllocatorSuffixPast99WidensLatestGradeOrderKept`). A unique-index violation from a concurrent insert declares a retry to SerializableWrite with the next suffix; agent-context keys use SerializableWrite's default bound (four attempts), then 409 `conflict` with `retryable: true` (only the Phase A minter states its own bound, for one cause, a collision of its random request id: six attempts and 500 `mint_collision_exhausted`; its transient conflicts keep the default bound and the 409 exit, section 3.1, "Retry bound", and section 4.2).
- Keys are per workspace: every entity with a readable key has the composite primary key (`WorkspaceId`, `<key>`), the pattern develop uses so one TODO id can exist in several workspaces (TR-MCP-TODO-008, `src/QBrainAi.Storage/McpDbContext.cs:583-585`), and every reference to it is the composite foreign key (`WorkspaceId`, `<key>`). Every readable-key, idempotency-key, and natural unique index therefore includes `WorkspaceId`, and ReadableKeyAllocator reads its highest suffix in the workspace (under the `Workspace` query filter and past the soft-delete filter), so two workspaces that hold the same legacy request id get the same key string on two rows, without a collision and without exhausting the retries (`ReadableKeyAllocatorSameParentIdTwoWorkspacesSameKeyNoCollisionNoExhaustion`; request ids are unique only within a session, `McpDbContext.cs:391,404`).
- Child rows use `long` identity keys; each references its parent through the composite foreign key (`WorkspaceId`, parent key), and every composite unique key of a child is (`WorkspaceId`, parent key, ...) (stated per entity). Because `WorkspaceId` is part of that foreign key, a child's `WorkspaceId` always equals its parent's; StorageSchema's `ResolveWorkspaceIdForAddedEntity` cases stamp it from the parent, as develop stamps session-log children (BUG-SESSIONLOG-WS-002, `McpDbContext.cs:1665-1669`).
- Root ids (section 3.1, "Root ids"): every child row carries `WorkspaceId` and the root ids of every parent it hangs from, copied from the parent in the same transaction and never changed. The turn key is `Agent` (64), `SessionId` (256), and `RequestId` (256) together, because a request id is unique only within its session (sessions are unique by (`WorkspaceId`, `SourceType`, `SessionId`) and turns by (`SessionLogId`, `RequestId`), `McpDbContext.cs:391,404`). A package child (item, focus ref, input, input link, omission, finding) carries `PackageId` and the turn key; a response child (claim, evidence) and a projection row carry `ResponseId`, `PackageId`, and the turn key; a grade carries `ResponseId`, `PackageId`, the turn key, and, for a model grade, `HvRequestId`; a grade finding carries `GradeId` and every root id of its grade; an HV request item carries `HvRequestId`, `ResponseId`, `PackageId`, and the turn key. The root-id columns join no key or index beyond those stated per entity, because no unit reads a child by them (`RootIdsEveryAgentContextChildRowEqualsItsParents`, over every table of this section).
- Every entity has `[Required][StringLength(1024)] WorkspaceId` with the named `Workspace` query filter and `StampWorkspaceId` on add; timestamps are `DateTimeOffset` named `*Utc`; decimals are `HasPrecision(29, 28)`; list-valued data is stored in 4NF child tables; every hash is SHA-256 over the UTF-8 bytes of the text's stored normal form in lower-case hex, computed through ContentHasher (section 3.5.2: `McpDbContext` replaces em and en dashes in every string it saves, and ContentHasher hashes that same form), so a value read back from the store hashes as it was recorded, and every byte-for-byte check of this phase (Replay identity, `ContentSha256`, `verdict_binding`, `reviewer_turn`) compares normal forms; every column described as sanitized is written through SecretRedactor (section 3.5.2). Agent-context records are redacted when written, so no secret value is stored in a package or a response, while session-log turns keep FR-MCP-SESSIONLOGSAN-001's outbound-only redaction (AC 2); SecretRedactor holds the one rule set for both.
- Idempotency: a create that carries an idempotency key or a natural unique key runs IdempotentCreate (section 3.5.2) over that unique index with policy `Conflict`: the key is looked up first, before any precondition that can change between attempts, and the stored PayloadFingerprint of the client inputs (never of server-observed sources) is compared; an equal fingerprint returns the stored record, another is 409 `conflict` with `retryable: false`. A unit that reads outside the database before its SerializableWrite (Build, Submit, the two grades) runs the same lookup read-only before those reads, so a retry of a lost response returns the stored record without reading a source. RelationshipWriter uses policy `ReturnStored`. The store has no concurrency tokens.
- Soft delete (TR-MCP-DB-003): `McpDbContext` turns every delete of these entities into a soft delete, hides deleted rows through its `SoftDelete` query filter, and its keys and unique indexes cover deleted rows (`McpDbContext.cs:391,404,1302-1338,1363-1380`). The revive rule of IdempotentCreate and ReadableKeyAllocator (section 3.5.2) acts on each key as follows. The relationship's six-field key: DeleteAgentContextRelationshipCommand is the one unit that deletes an agent-context row (a client-made link); creating the identical link again revives that row, keeps its `RelationshipId`, sets `ToSha256`, `EvidenceRef`, `CreatedBy`, and `CreatedUtc` from the new create, and returns `created: true` (`CreateRelationshipAfterDeleteRevivedSameRelationshipIdCreatedTrue`). The readable keys (`pkg-`, `rsp-`, `grd-`, `rel-`, `hvr-`): ReadableKeyAllocator counts deleted suffixes, so a deleted link's `rel-` suffix is never given to another link (`ReadableKeyAllocatorSoftDeletedSuffixNeverReused`). The idempotency keys of packages, responses, and HV requests, and the model grade's natural key: no unit or surface deletes these records or their child rows, because they are kept for audit (section 6.1), so in this design their lookups never meet a deleted row.
- Write isolation: every unit that checks a response's status or grades, an HV request, or an idempotency or readable key and then saves runs its checks and its save as one delegate through SerializableWrite (section 3.5.2; the convention of `SessionLogService.cs:156`): BuildAgentContextPackageCommand, SubmitAgentResponseCommand, RunDeterministicGradeCommand and RecordModelGradeCommand with DecideAgentResponseTrustCommand, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand, CreateAgentContextRelationshipCommand, ProjectAgentResponsesCommand (each ProjectOneResponse save), and CreateAgentContextHvRequestCommand. Each delegate is database-only (section 3.1, "Database-only delegates"): the unit reads the git commit, files, sources and their hashes (each relationship target's `ToSha256` included), retrieval and GraphRAG results, git ancestry, and receipts before SerializableWrite, after its read-only idempotency lookup where it has a key, and its delegate re-checks the database state those reads relied on (each locked response's `StatusVersion`, the turn, the HV request); a changed state ends the attempt and the unit runs again from its reads, within the same bound. Build reads each prior response's staleness read-only and persists only its package with its child rows and its links; only EvaluateStalenessCommand persists `Stale` (through ResponseStatusTransition). A grade unit (RunDeterministicGradeCommand, RecordModelGradeCommand) runs every read-only check it has (the response, the HV request, the evidence and secret scan of a deterministic grade, the receipt, reviewer, and runtime-identity checks of a model grade), then dispatches EvaluateStalenessCommand for its response, then runs its own SerializableWrite, whose delegate re-checks the response's `StatusVersion` and dispatches DecideAgentResponseTrustCommand, which reads only the database (the status that dispatch left and the stored grades) and persists no `Stale`. Per-response writes name their responses, and the transaction's first statements increment each named response's `StatusVersion` in ascending `ResponseId` order (a supersede names the original and the replacement, an HV request every candidate response before it re-reads their status and inserts the items, a projection save its response), so every such operation on one response takes the row's write lock first and they serialize in commit order on SQLite, PostgreSQL, and SQL Server. A transient conflict (`DbErrorClassifier.IsTransientConflict`: PostgreSQL 40001 or 40P01, SQL Server 1205, SQLite 5 busy or 6 locked) or a unique violation on a key the unit declares rolls the whole delegate back and runs it again, at most three times (four attempts, SerializableWrite's default bound); then the unit returns 409 `conflict` with `retryable: true` and saves nothing (`TrustForcedInterleavingDeterministicFailVsModelPassNeverTrustedOnSupersededGrade`, `TrustForcedInterleavingRejectVsGradeRejectedNeverRevived`, `HvRequestForcedCreateVsRejectNoPendingItemOnTerminalResponse`, `HvRequestForcedCreateVsSupersedeNoPendingItemOnTerminalResponse`, `HvRequestForcedConcurrentOverlappingCreatesEachResponsePendingInAtMostOneOpenRequest`, `LifecycleForcedInterleavingSupersedeVsRejectSameOriginalOneWinsOtherRefused`, each on the three providers).
- Migrations `<yyyyMMddHHmmss>_AddAgentContext` are generated for SQLite, PostgreSQL, and SQL Server with the existing `dotnet ef migrations add` procedure.

`AgentContextPackageEntity` (table `AgentContextPackages`, key (`WorkspaceId`, `PackageId`), `PackageId` = `pkg-<requestId>-<nn>`, for example `pkg-req-20261011T223749Z-prompt-cc85-01`)

- `WorkspaceId`, `SessionId` (256, the MCP session), `Agent` (64), `RequestId` (256, the turn), all required: the request's turn, located by TurnLocator (section 3.5.3) with `agent`, `sessionId`, and `requestId`.
- `IdempotencyKey` (128, required): unique with `WorkspaceId`. `PayloadSha256` (64, required): the PayloadFingerprint of the Build request's client inputs (`sessionId`, `agent`, `requestId`, `queryText`, `tokenBudget`, and `focusRefs` in order). A repeated key returns the stored package when this fingerprint is equal (the same 422 with the stored diagnostics when it is `Overflow`), whatever sources, statuses, or commits changed in between, and is 409 `conflict` with `retryable: false` otherwise.
- `Status` (16): `Built` or `Overflow`.
- `CommitSha` (64, required): the workspace HEAD at build (`SourceResolver.ReadCommit`, section 3.5.5); `WorkingTreeDirty` (bool).
- `BuilderVersion` (32), `TokenBudget` (int), `BytesPerToken` (int), `MandatoryTokenCount` (int), `TotalTokenCount` (int).
- `InputsSha256` (64): hash of the canonical JSON of the whole input snapshot (the sanitized request text, `OptionsSnapshotJson`, the commit, the focus refs, every input row with its source hash, `ContainsSecret`, `ResponseStatus`, and `RankingFeaturesJson`, and every input link), so two builds that read or rank differently never share it. It is the provenance and replay check (Replay recomputes it from the stored snapshot, in the stored normal form, so a source that contains em or en dashes replays `identical: true`, `ReplaySourceWithEmAndEnDashesIdenticalTrue`), never the idempotency fingerprint.
- `OptionsSnapshotJson` (text, required): the canonical JSON of every `AgentContextOptions` value the build used (mode, budget, bytes per token, candidate limits, profile and rule sources, trust floor, GraphRAG flags, and the `preferGraphRag` value the build retrieved with), so replay never reads current configuration.
- `ContentSha256` (64): hash of the canonical rendered package in its stored normal form; null for `Overflow`.
- `QueryText` (text, sanitized), `RenderedContent` (text; null for `Overflow`).
- `CreatedUtc`.
- Indexes: (`WorkspaceId`, `RequestId`), (`WorkspaceId`, `SessionId`, `CreatedUtc`), unique (`WorkspaceId`, `IdempotencyKey`).
- Children: `Items` (for an `Overflow` package, its mandatory items with their token counts and without content, so a repeated or concurrent build returns the same 422 diagnostics), `Omissions`, `Findings`, `FocusRefs`, `Inputs`, `InputLinks`, each through the composite foreign key (`WorkspaceId`, `PackageId`) (`DeleteBehavior.Restrict`).

`AgentContextPackageItemEntity` (table `AgentContextPackageItems`, `long Id`)

- `WorkspaceId`, `PackageId` (FK (`WorkspaceId`, `PackageId`)), `Agent` (64), `SessionId` (256), `RequestId` (256) (root ids, the package's turn key), `Ordinal` (int); unique (`WorkspaceId`, `PackageId`, `Ordinal`).
- `Kind` (32): the `SourceKind` wire values of package items: `Request`, `Profile`, `Rule`, `Requirement`, `SessionLogTurn`, `Todo`, `Code` (the kind `CodeFile`), `Receipt`, `PriorResponse`.
- `SourceRef` (512): the requirement id, file path, turn reference `<agent>/<sessionId>/<requestId>`, TODO id, receipt path, or `responseId`.
- `SourceSha256` (64), `SourceCommitSha` (64): the commit a file under version control was read at; null for a file outside version control and for a record. A `Rule` item with a null `SourceCommitSha` is provenance only: no `derived_from` link names it and it is never a staleness dependency.
- `Mandatory` (bool), `Score` (decimal 0..1, null for mandatory items), `Rationale` (2048), `TokenCount` (int).
- `RetrievalPath` (16): `Relational` or `GraphRag`.
- `Content` (text, sanitized; secret values redacted in mandatory items).

`AgentContextPackageFocusRefEntity` (table `AgentContextPackageFocusRefs`, `long Id`): `WorkspaceId`, `PackageId` (FK (`WorkspaceId`, `PackageId`)), `Agent`, `SessionId`, `RequestId` (root ids), `Ordinal` (unique (`WorkspaceId`, `PackageId`, `Ordinal`)), `Kind` (32), `Ref` (512): the request's `focusRefs` exactly as given.

`AgentContextPackageInputEntity` (table `AgentContextPackageInputs`, `long Id`): the immutable replay snapshot of every candidate the builder considered, included or omitted: `WorkspaceId`, `PackageId` (FK (`WorkspaceId`, `PackageId`)), `Agent`, `SessionId`, `RequestId` (root ids), `Ordinal` (unique (`WorkspaceId`, `PackageId`, `Ordinal`)), `Kind` (32), `SourceRef` (512), `SourceSha256` (64), `SourceCommitSha` (64), `SourceVersion` (128: the git blob id for files under version control, otherwise the record's version or `UpdatedUtc` as read), `Content` (text, exactly as the builder read it after SecretRedactor and before ranking, stored in its normal form), `ContainsSecret` (bool: `SecretRedactor.ContainsSecret` over the content as read, before redaction; PackageAssembler's secret-filter step reads this flag), `ResponseStatus` (16, `PriorResponse` candidates only, null otherwise: the response's effective status at build time, its stored status or `Stale` when IResponseStalenessRule finds a changed dependency, so its trust and staleness state; the omission filter reads it), `RankingFeaturesJson` (text: the canonical JSON of every ranking input of the candidate, including its raw score before rounding and its retrieval path).

`AgentContextPackageInputLinkEntity` (table `AgentContextPackageInputLinks`, `long Id`): the `contradicts` and `blocks` relationships the findings step read at build time: `WorkspaceId`, `PackageId` (FK (`WorkspaceId`, `PackageId`)), `Agent`, `SessionId`, `RequestId` (root ids), `Ordinal` (unique (`WorkspaceId`, `PackageId`, `Ordinal`)), `RelationshipId` (128, a logical reference, kept as read), `Type` (32: `contradicts` or `blocks`), `FromKind` (32), `FromRef` (512), `ToKind` (32), `ToRef` (512).

Replay reads only the input rows, the input links, `OptionsSnapshotJson`, the focus refs, and `BuilderVersion`, and runs every PackageAssembler step on them (secret filter through SecretRedactor, omission filter, findings, the mandatory-context check, the budget fill, and rendering, section 3.5.5); it never reads live sources, so later changes to configuration, omitted sources, requirements, TODOs, turns, profile files, relationships, response statuses, or the working tree cannot change a replay. Its identity check compares the recomputed `InputsSha256` and `ContentSha256` with the stored ones, both over the stored normal form (section 3.5.2, ContentHasher).

Source kinds and hashes (`SourceSha256`, `ToSha256`, and evidence `Sha256`). `SourceKind` (section 3.5.5, owned by SourceResolver) is the one vocabulary of sources; its wire aliases are mapped in SourceResolver only: `Code` (package items, inputs, omissions) and `File` (evidence) are the kind `CodeFile` (relationship nodes), and `Response` (relationship nodes) is the kind `PriorResponse`. Hashes per kind:

- `CodeFile` (`Code`, `File`), `Profile`, `Rule`, `Receipt`: the file content at the recorded commit for files under version control (never the working copy), otherwise the file content as read; line endings are kept as stored, and the hash is over the content's stored normal form, the form the input row keeps.
- `Requirement`: the canonical JSON of the effective record the builder read (id, title, description, and each acceptance criterion's text in order), serialized with sorted keys and no insignificant whitespace; status, each criterion's satisfaction flag and evidence, and timestamps are not hashed, so recording a criterion satisfied or an HV verdict never stales a response, and a deleted or superseded requirement is stale through IResponseStalenessRule's status check (section 3.5.5; section 3.1, Requirement hash; `StalenessRequirementAcSatisfiedNotStaleTextChangedStale`).
- `SessionLogTurn`: the canonical JSON of the turn's request text, response, status, actions, and decisions, excluding timestamps. Every turn reference (package items, inputs, omissions, findings, evidence, and relationship nodes) is `<agent>/<sessionId>/<requestId>`, the TurnLocator key (section 3.5.3), read from the right (neither id contains `/`, `src/QBrainAi.Services/Services/SessionLogIdentifierValidator.cs:10-16`).
- `Todo`: the canonical JSON of the TODO's id, title, description lines, tasks, and done state.
- `PriorResponse` (`Response`): `ContentSha256`; because that hash never changes when the response is rejected, superseded, made stale, or demoted, IResponseStalenessRule also checks its effective status (section 3.5.5). `Package`: `ContentSha256`.
- `Request`: the sanitized request text.
- `Commit`: no hash; checked by ancestry.

Dependencies (IResponseStalenessRule, section 3.5.5): a dependency on a file under version control is re-hashed at the workspace's current HEAD commit, so an uncommitted edit does not make a response stale, committing it does, and a file absent at HEAD is a changed dependency (`StalenessUncommittedEditNotStaleCommittedEditStale`, `StalenessFileDeletedAtHeadStale`); a `Profile` file outside version control is re-hashed as read; a `Rule` file outside version control is never a dependency (section 6.2). The request's own turn (the package's `agent`, `sessionId`, and `requestId`) is never a `SessionLogTurn` candidate, a dependency, or cited evidence: the `Request` item carries its text, so no package item, input, or `derived_from` link names it (`BuildOwnTurnNotACandidateNoDerivedFromOwnTurn`, `StalenessOwnTurnCompletedResponseNotStale`), and an evidence ref naming it is refused at submission with 400 `validation_error`, `details.field` naming that evidence ref (`SubmitEvidenceCitesOwnTurnRejected400`).

Response evidence keeps two hashes. `AgentResponseEvidence.CitedSha256` is the hash the submission cited, stored exactly as submitted (null when none was cited). `AgentResponseEvidence.Sha256` is computed by the server at submission from the cited source through SourceResolver: `File` as `CodeFile`, and `Requirement`, `Receipt`, and `SessionLogTurn` as their kinds above (null when an existing source cannot be read at submission). A ref of these four kinds that names no existing source (a `File` path absent at its `commitSha`, or no requirement, receipt, or turn with that ref) is refused at submission with 400 `validation_error`, `details.field` naming the ref, as SourceResolver refuses it in every unit (`SubmitEvidenceRefNamesNoSourceRejected400`), so every stored evidence item names a source that existed when it was cited. For these four kinds the deterministic grade makes the evidence check FAIL when `CitedSha256` is present and differs from `Sha256`, so a wrong cited hash stays a FAIL after any restart (`GradeWrongCitedHashFailsAfterRestart`), and UNKNOWN when `Sha256` is null; `Commit` evidence carries no hash and is checked by ancestry (RunDeterministicGradeCommand, section 6.5). `Sha256` is the dependency: IResponseStalenessRule compares it with the current source and never reads `CitedSha256`. `Commit` evidence is not a dependency (a commit is immutable). There is no command-result evidence kind: a command's result is cited through the `Receipt` that records it.

Ranking scores (CandidateRanker, section 3.5.5) are decimals rounded half-even to 6 places before ordering, so float differences between hosts never reorder candidates; ties break on `sourceRef` by ordinal comparison.

`AgentContextOmissionEntity` (table `AgentContextOmissions`, `long Id`)

- `WorkspaceId`, `PackageId` (FK (`WorkspaceId`, `PackageId`)), `Agent`, `SessionId`, `RequestId` (root ids), `Ordinal` (unique (`WorkspaceId`, `PackageId`, `Ordinal`)), `Kind`, `SourceRef`, `SourceSha256`, `Score`.
- `Reason` (32): `budget`, `untrusted`, `stale`, `rejected`, `superseded`, `secret`, `duplicate`, `below_candidate_limit`.
- `Detail` (2048).

`AgentContextFindingEntity` (table `AgentContextFindings`, `long Id`)

- `WorkspaceId`, `PackageId` (FK (`WorkspaceId`, `PackageId`)), `Agent`, `SessionId`, `RequestId` (root ids), `Ordinal` (unique (`WorkspaceId`, `PackageId`, `Ordinal`)).
- `Kind` (16): `Contradiction` (two included items are linked by `contradicts`) or `Blocker` (an open item `blocks` the request's TODO or turn), computed from the input links.
- `SourceRef` (512), `OtherSourceRef` (512, contradictions only), `RelationshipId` (128), `Summary` (2048).

`AgentResponseEntity` (table `AgentResponses`, key (`WorkspaceId`, `ResponseId`), `ResponseId` = `rsp-<requestId>-<nn>`)

- `WorkspaceId`, `PackageId` (FK (`WorkspaceId`, `PackageId`), required), `SessionId`, `RequestId`, `Agent`, all required (root ids, copied from the package; a submission whose `agent` differs from the package's is 400 `validation_error`, `details.field` `agent`, `SubmitAgentDiffersFromPackage400`).
- `Model` (128), `Effort` (32), `InvocationMode` (16): `Stateful` or `Stateless`; `Stateless` is accepted only for a package whose `OptionsSnapshotJson` mode is `Stateless`, otherwise 400 `validation_error`, `details.field` `invocationMode` (`SubmitStatelessModeOnPilotPackageRejected400`), so the pilot comparison never counts a claimed stateless answer of a `Pilot` build.
- `IdempotencyKey` (128): unique with `WorkspaceId`.
- `SubmissionSha256` (64, required): the response's PayloadFingerprint, SHA-256 of the canonical JSON (sorted keys, no insignificant whitespace) of the whole submission: `packageId`, `agent`, `model`, `effort`, `invocationMode`, `contentFormat`, `content`, and `claims` and `evidence` in order; a repeated idempotency key returns the stored response only when this fingerprint is equal, otherwise 409 `conflict` with `retryable: false`.
- `StatusVersion` (long, default 0): the lock counter that SerializableWrite increments first for a per-response write (not an EF concurrency token).
- `Status` (16): `Submitted`, `Grading`, `Trusted`, `Rejected`, `Stale`, `Superseded`, changed only through ResponseStatusTransition (section 3.5.5) along the response lifecycle below.
- `StatusReason` (2048), `SupersededByResponseId` (128).
- `ContentFormat` (16): `markdown`, `text`, or `json`. `Content` (text, sanitized), `ContentSha256` (64, over the stored normal form of `Content`, so a reviewer that hashes the content it reads from the server gets this value, `SubmitContentWithEmDashContentSha256MatchesStoredContent`).
- `CommitSha` (64): the workspace HEAD commit at submission (`SourceResolver.ReadCommit`; unreadable is 409 `agent_context_commit_unavailable`, `SubmitCommitUnreadable409CommitUnavailable`).
- `SubmittedUtc`, `StatusChangedUtc`, `TrustedUtc`.
- Indexes: (`WorkspaceId`, `Status`), (`WorkspaceId`, `RequestId`), (`WorkspaceId`, `PackageId`), unique (`WorkspaceId`, `IdempotencyKey`).
- Children: `Claims`, `Evidence`, each through the composite foreign key (`WorkspaceId`, `ResponseId`).

`AgentResponseClaimEntity` (table `AgentResponseClaims`, `long Id`): `WorkspaceId`, `ResponseId` (FK (`WorkspaceId`, `ResponseId`)), `PackageId` (128), `Agent` (64), `SessionId` (256), `RequestId` (256) (root ids), `Ordinal`, `ClaimId` (64), `Text` (4096); unique (`WorkspaceId`, `ResponseId`, `ClaimId`) and unique (`WorkspaceId`, `ResponseId`, `Ordinal`).

`AgentResponseEvidenceEntity` (table `AgentResponseEvidence`, `long Id`): `WorkspaceId`, `ResponseId` (FK (`WorkspaceId`, `ResponseId`)), `PackageId`, `Agent`, `SessionId`, `RequestId` (root ids), `Ordinal` (unique (`WorkspaceId`, `ResponseId`, `Ordinal`)), `ClaimId` (64), `Kind` (16: `File` (the kind `CodeFile`), `Requirement`, `Receipt`, `Commit`, `SessionLogTurn`), `Ref` (2048: a workspace-relative path for `File` and `Receipt`, a requirement id, a full 40-hex commit SHA for `Commit`, or the turn reference `<agent>/<sessionId>/<requestId>` of a `SessionLogTurn`, never the response's own turn), `CitedSha256` (64, nullable, as submitted), `Sha256` (64, nullable, computed by the server), `CommitSha` (64: for `File`, the commit the cited content is read at; null otherwise).

`AgentResponseGradeEntity` (table `AgentResponseGrades`, key (`WorkspaceId`, `GradeId`), `GradeId` = `grd-<responseId>-<deterministic|model>-<nn>`, for example `grd-rsp-req-20261011T223749Z-prompt-cc85-01-model-01`)

- `WorkspaceId`, `ResponseId` (FK (`WorkspaceId`, `ResponseId`)), `PackageId` (128), `Agent` (64), `SessionId` (256), `RequestId` (256) (root ids).
- `Kind` (16): `Deterministic` or `Model`.
- `Grader` (256): `agent-context-deterministic/<BuilderVersion>`, or `<AgentName>/<model>/<effort>`.
- `Correctness`, `Completeness`, `CorrectnessThreshold`, `CompletenessThreshold` (decimal; the scores as exact lexemes, ScoreLexeme, section 3.5.4).
- `PassCount`, `FailCount`, `UnknownCount` (int).
- `Verdict` (16): `Pass` or `Fail`, derived by ScoreVerdict (section 3.5.4) when the grade is recorded: `Pass` only when both scores meet `max(threshold, 0.98)`, inclusive, and `FailCount` and `UnknownCount` are zero; the thresholds then in force are stamped in `CorrectnessThreshold` and `CompletenessThreshold`. The trust decision never reads this stored verdict; it re-derives it from the stored scores and counts (section 6.2).
- "Latest grade" of a kind means the grade with the greatest server-stamped `RecordedUtc`, ties broken by the greater `GradeId` suffix compared as a number; a client-supplied time never orders grades (`ModelGradeClientGradedUtcInFutureIgnoredServerStampedNewerFailBecomesLatest`). Concurrent grade writes are all kept, and the trust decision (DecideAgentResponseTrustCommand) is recomputed after each one inside that grade's SerializableWrite (write isolation above), so the stored status always reflects the latest committed grade pair.
- `HvRequestId` (128, required for `Model`, null for `Deterministic`; a root id and the composite foreign key (`WorkspaceId`, `HvRequestId`) to the HV request): the HV request whose review produced the grade. A unique filtered index on (`WorkspaceId`, `ResponseId`, `HvRequestId`) where `Kind` is `Model` is the IdempotentCreate key that makes the grade-submission continuation retry-safe: it is looked up first, before the HV request and response status checks; a repeated submission whose `PayloadSha256` equals the stored one returns that grade with 200, and a different one is refused with 409 `conflict` with `retryable: false` (`ModelGradeRetrySameSubmission200Existing`, `ModelGradeRetryDifferentSubmission409Conflict`).
- `PayloadSha256` (64, required for `Model`): the PayloadFingerprint of the RecordModelGradeRequest's client inputs (every request field in record order), never of the receipt hashes the server reads.
- `ReviewerAgent` (64, required for `Model`: the `ModelGrader:AgentName` the `reviewer_turn` check located the turn with), `ReviewerSessionId`, `ReviewerRequestId` (256, required for `Model`): the reviewer's own session-log turn that holds its entire result, by its full turn key, so the reference stays exact after a configuration change; `VerdictJsonSha256` (64): the hash of the verdict block the grade was verified against, over its stored normal form.
- `RequestJsonlPath`, `ResponseJsonlPath` (2048, required for `Model`, workspace-relative under `ModelGrader:ReceiptRoot`), `RequestJsonlSha256`, `ResponseJsonlSha256` (64): the receipts and their hashes as verified when the grade was recorded.
- `RolloutJsonlPath` (2048, required for `Model`, under `ModelGrader:ReceiptRoot`), `RolloutJsonlSha256` (64), `ReviewerThreadId` (64, required for `Model`): the reviewer's own Codex rollout receipt (after the review execution completes, the HV runner freezes a copy of that thread's rollout file next to the request and response jsonl, redacted by the receipt rule, before any grade is submitted) and the thread id it records; check 8 (`runtime_identity`) verifies them against that frozen copy, so the later grade-submission continuation of the same thread (RecordModelGradeCommand, two-step protocol) never changes the receipt it is checked against.
- `GradedUtc`: for a model grade, the time the reviewer reports in the request, kept as provenance only; for a deterministic grade, the server time of the grade. `RecordedUtc` (required): stamped by the server when the grade is saved; it alone orders grades.
- Child `Findings`, through the composite foreign key (`WorkspaceId`, `GradeId`).

`AgentResponseGradeFindingEntity` (table `AgentResponseGradeFindings`, `long Id`): `WorkspaceId`, `GradeId` (FK (`WorkspaceId`, `GradeId`)), `ResponseId` (128), `PackageId` (128), `Agent` (64), `SessionId` (256), `RequestId` (256), `HvRequestId` (128, null for a deterministic grade) (root ids, the grade's), `Ordinal` (unique (`WorkspaceId`, `GradeId`, `Ordinal`)), `ClaimId` (64), `Check` (256), `Outcome` (16: `PASS`, `FAIL`, `UNKNOWN`), `Evidence` (text, sanitized).

`AgentContextRelationshipEntity` (table `AgentContextRelationships`, key (`WorkspaceId`, `RelationshipId`), `RelationshipId` = `rel-<type>-<yyyyMMddTHHmmssZ>-<nn>`, for example `rel-depends_on-20261011T234000Z-01`; a link is a root record whose nodes are references, so it carries no parent id)

- `WorkspaceId`, `FromKind` (32), `FromRef` (512), `Type` (32), `ToKind` (32), `ToRef` (512).
- Node kinds (the `SourceKind` wire values of relationship nodes): `Package`, `Response` (the kind `PriorResponse`), `Requirement`, `SessionLogTurn`, `Todo`, `CodeFile`, `Receipt`, `Profile`, `Rule`.
- Types: `implements`, `verifies`, `depends_on`, `supports`, `contradicts`, `supersedes`, `blocks`, `derived_from`, `relevant_to`. A `supersedes` link is written only by SupersedeAgentResponseCommand through RelationshipWriter; a client request for type `supersedes` is 400 `validation_error`, `details.field` `type`, so one data source holds the supersede chain.
- `ToSha256` (64): the target's content hash when linked, through SourceResolver, read by the unit before its SerializableWrite (database-only delegate) and handed to RelationshipWriter (the staleness dependency hash of `depends_on`, `derived_from`, and `supports` links, IResponseStalenessRule, section 3.5.5).
- `EvidenceRef` (2048), `CreatedUtc`, and `CreatedBy` (128): the calling agent for a link made through CreateAgentContextRelationshipCommand, or the name of the command that made it (`BuildAgentContextPackageCommand` for a package's `derived_from` links to its included sources and its `relevant_to` link to its turn, `SubmitAgentResponseCommand` for a response's `derived_from` link to its package and its `supports` evidence links, `SupersedeAgentResponseCommand` for a `supersedes` link). Every link a command made is protected: its deletion is 409 `agent_context_relationship_protected`; a client-made link of any type can be deleted.
- Unique (`WorkspaceId`, `FromKind`, `FromRef`, `Type`, `ToKind`, `ToRef`): the IdempotentCreate key of RelationshipWriter with policy `ReturnStored`, so an identical link, including one created concurrently, returns the stored link with `created: false` (`CreateRelationshipConcurrentIdenticalOne201OneCreatedFalse`); indexes on (`WorkspaceId`, `FromKind`, `FromRef`) and (`WorkspaceId`, `ToKind`, `ToRef`).
- Why a separate link store: develop's `GraphRelationshipEntity` (`src/QBrainAi.Storage/Entities/GraphRelationshipEntity.cs:10`) joins two `GraphEntityEntity` nodes of the GraphRAG knowledge graph, which is a projection that can be disabled; `TodoRequirementLinkEntity` and `RequirementTraceabilityLinkEntity` (`src/QBrainAi.Storage/Entities/TodoRequirementLinkEntity.cs:10`, `src/QBrainAi.Storage/Entities/RequirementTraceabilityLinkEntity.cs:9`) each join one fixed pair of record kinds; `MemoryEdgeEntity` (TR-MCP-MEMORY-MODEL-002, `src/QBrainAi.Storage/Entities/MemoryEdgeEntity.cs:5-32`) joins two memories by memory id with a type and a weight; and `HostileReviewArtifactLinkEntity` (TR-MCP-HOSTILEREVIEW-002, `src/QBrainAi.Storage/Entities/HostileReviewArtifactLinkEntity.cs:6-7`) ties one hostile review request to one artifact. An agent-context link joins any two `SourceKind` references, keeps the target's hash at link time for staleness, records the command that made it for protection, and stays authoritative while GraphRAG is disabled; none of the five existing stores holds those fields, so the links are one store of their own. One rule differs on purpose: an identical agent-context link returns the stored link with `created: false`, while a duplicate explicit memory edge is 409 (`src/QBrainAi.Services/Memory/Models/MemoryCompetitiveModels.cs:282`), because a repeated or concurrent identical create, by a client or by a command, is a retry of one link, not a conflict between two.

`AgentContextHvRequestEntity` (table `AgentContextHvRequests`, key (`WorkspaceId`, `HvRequestId`), `HvRequestId` = `hvr-<sessionId>-<yyyyMMddTHHmmssZ>`, with `-<nn>` appended only when two are created in the same second)

- `WorkspaceId`, `SessionId` (256), `Agent` (64): the MCP session whose turns are reviewed, located by TurnLocator (section 3.5.3) with `agent` and `sessionId`.
- `FromRequestId`, `ToRequestId` (256): the first and last turns of the range, inclusive, in the session's turn order.
- `RequestedBy` (128), `Reason` (2048).
- `IdempotencyKey` (128, required): unique with `WorkspaceId`. `PayloadSha256` (64, required): the PayloadFingerprint of the request's client inputs (`sessionId`, `agent`, `fromRequestId`, `toRequestId`, `requestedBy`, and `reason`); a repeat with the same key and an equal fingerprint returns the stored request, and any other input with the same key is 409 `conflict` with `retryable: false`.
- `Status` (16): `Open` (accepting grades) or `Completed` (no item is `Pending`: every item is `Graded` by this request or `Closed` because its response became `Rejected` or `Superseded`).
- `CreatedUtc`, `CompletedUtc` (set when the request becomes `Completed`, by RecordModelGradeCommand or by ResponseStatusTransition).
- An `Open` request accepts grades from any later review run of the configured grader that passes the eight checks below, so an abandoned or failed review is recovered by running the review again for the same `hvRequestId`; an item already `Graded` keeps its grade, and a different grade for it is 409 `conflict` with `retryable: false` (`HvRequestReviewRerunAfterAbandonPendingItemsGradedRequestCompletes`).
- Child `Items`, through the composite foreign key (`WorkspaceId`, `HvRequestId`).

`AgentContextHvRequestItemEntity` (table `AgentContextHvRequestItems`, `long Id`): `WorkspaceId`, `HvRequestId` (FK (`WorkspaceId`, `HvRequestId`)), `ResponseId` (128, FK (`WorkspaceId`, `ResponseId`); that key's index is the lookup ResponseStatusTransition uses to close a response's `Pending` items), `PackageId` (128), `Agent` (64), `SessionId` (256), `RequestId` (256) (root ids: the response's, whose turn key names the session the HV request reviews), `Ordinal`, `Disposition` (16): `Pending`, `Graded`, or `Closed`, and `ClosedReason` (32): `rejected` or `superseded`; unique (`WorkspaceId`, `HvRequestId`, `ResponseId`) and unique (`WorkspaceId`, `HvRequestId`, `Ordinal`), so a response can be an item of several HV requests over time but `Pending` in at most one `Open` request. Items are the agent responses whose `RequestId` lies in the range when the request is created and whose status is `Submitted`, `Grading`, `Trusted`, or `Stale` (a `Rejected` or `Superseded` response is never listed, and a response already `Pending` in another `Open` request is not listed again, `HvRequestResponsePendingInOpenRequestNotListedAgain`); when a listed response becomes `Rejected` or `Superseded` while the request is `Open`, ResponseStatusTransition (section 3.5.5, called by RejectAgentResponseCommand or SupersedeAgentResponseCommand) sets its item `Closed` with that reason in the same SerializableWrite and, when no item stays `Pending`, sets the request `Completed` with `CompletedUtc` (`HvRequestRangeWithRejectedResponseNotListed`, `HvRequestItemRejectedMidReviewClosedAndRequestCompletes`, `HvRequestItemSupersededMidReviewClosed`).

Receipt and reviewer verification (RecordModelGradeCommand), before a model grade is saved; each failure refuses the grade with 422 `agent_context_receipt_unverified` and `details.check` naming the check:

1. `receipt_path`: both jsonl paths resolve inside the workspace under `ModelGrader:ReceiptRoot`.
2. `request_meta`: the request jsonl's first line is a JSON object with `type` `hv_request_meta` whose `reviewer.agent` contains `ModelGrader:AgentName`, `reviewer.model` equals `ModelGrader:RequiredModel`, and `reviewer.effort` equals `ModelGrader:RequiredEffort` (the HV runner writes exactly this object), and the request jsonl's body record names the `hvRequestId`.
3. `verdict_block`: the response jsonl records a completed turn (`turn.completed`) whose last agent message contains exactly one `=== VERDICT JSON ===` block that parses as one JSON object.
4. `verdict_binding`: that verdict names this `hvRequestId` and has a `grades` entry for this `responseId` whose `contentSha256` equals the response's `ContentSha256` (both over the stored normal form of the content the reviewer read from the server).
5. `verdict_match`: the submitted `correctness`, `completeness`, and `findings` equal that entry exactly (the same score lexemes, compared as ScoreLexeme values that no hop re-serializes, section 3.5.4; the same claimIds, checks, and outcomes in the same order), and the entry's findings name every claim of the response at least once (an empty or partial finding list is refused). Verdict semantics: each `grades` entry carries `verdict` `AGREE` or `DISAGREE` for its one response, and the verdict JSON's `overallVerdict` is the request's; an entry's `verdict` is `AGREE` exactly when ScoreVerdict derives `Pass` from its scores and counts at `max(Trust:Correctness, 0.98)` and `max(Trust:Completeness, 0.98)` (section 3.5.4) and its findings hold no FAIL or UNKNOWN, and `DISAGREE` otherwise, and `overallVerdict` is `AGREE` exactly when every entry is `AGREE`; any other combination is refused (`ModelGradeDisagreeVerdictPassingScoresRefusedVerdictMatch`, `ModelGradeAgreeVerdictFailingScoresOrFailFindingRefusedVerdictMatch`, `ModelGradeOverallAgreeWithDisagreeEntryRefusedVerdictMatch`). A consistent `DISAGREE` entry is a legitimate failing grade and is recorded, and in a mixed range each response's grade is recorded from its own entry under an overall `DISAGREE` (`ModelGradeConsistentDisagreeEntryRecordedFailingGrade`, `ModelGradeMixedRangeEachEntryRecordedOverallDisagree`).
6. `reviewer_turn`: TurnLocator (section 3.5.3) finds the turn with agent `ModelGrader:AgentName`, `reviewerSessionId`, and `reviewerRequestId` in the workspace, the turn was created at or after the HV request's `CreatedUtc`, and its stored result contains the same verdict block byte for byte, both compared in their stored normal form (the receipt's block passes through ContentHasher's normalization, since the store saved the turn in that form, `ModelGradeVerdictWithEmDashReviewerTurnMatchesInNormalForm`), where the stored result is the turn's response followed by its processingDialog parts in part order with each part's header line removed (the HV protocol stores the entire result on the reviewer's own turn and splits a long result into ordered parts; `ModelGradeVerdictSplitAcrossDialogPartsRejoinedAndAccepted`).
7. `reviewer_independent`: the reviewer's MCP session is neither the session the HV request reviews nor the session that submitted the response; independence is a separate review session and execution, not a different agent name, so an independently launched Codex reviewer grades a Codex-authored response (`ModelGradeCodexResponseIndependentCodexReviewerSessionAccepted`).
8. `runtime_identity`: the rollout receipt at `rolloutJsonlPath` resolves under `ModelGrader:ReceiptRoot`; its `session_meta` record's `id` equals `reviewerThreadId` and the `thread_id` of the response jsonl's `thread.started` event; it holds at least one `turn_context` record before the record of the completed review turn whose verdict check 3 read, and every `turn_context` record in it has `model` equal to `ModelGrader:RequiredModel` and `effort` equal to `ModelGrader:RequiredEffort`, so a rollout without live runtime context is refused (`ModelGradeRolloutWithoutTurnContextRefusedRuntimeIdentity`); and the verdict JSON names the same `reviewerThreadId`. The declared `request_meta` alone never satisfies this check (`ModelGradeLaunchMetaCorrectRolloutTurnContextOtherModelOrEffortRefusedRuntimeIdentity`, `ModelGradeRolloutThreadDiffersFromResponseStreamRefusedRuntimeIdentity`).

The hashes of the three jsonl receipts (request, response, rollout) and of the verdict block are stored on the grade. The remaining trust assumption is the workspace's session-log identity model (every agent of a workspace authenticates with the workspace key), which this plan does not change: a forged grade would need a forged reviewer session, a forged verdict, a forged rollout, and matching receipts, and each is checked against the others. Negative cases: `ModelGradeReceiptOutsideReceiptRootRefusedReceiptPath`, `ModelGradeRequestMetaOtherAgentModelOrEffortRefusedRequestMeta`, `ModelGradeDisagreeVerdictPassingScoresRefusedVerdictMatch`, `ModelGradeEmptyOrPartialFindingsRefusedVerdictMatch`, `ModelGradeVerdictForOtherResponseOrContentRefusedVerdictBinding`, `ModelGradeNoCompletedTurnOrTwoVerdictsRefusedVerdictBlock`, `ModelGradeReviewerTurnMissingRefusedReviewerTurn`, `ModelGradeReviewerTurnVerdictDiffersRefusedReviewerTurn`, `ModelGradeReviewerTurnBeforeHvRequestRefusedReviewerTurn`, `ModelGradeReviewerSameSessionAsProducingOrReviewedRefusedReviewerIndependent`, `ModelGradeLaunchMetaCorrectRolloutTurnContextOtherModelOrEffortRefusedRuntimeIdentity`, `ModelGradeRolloutThreadDiffersFromResponseStreamRefusedRuntimeIdentity`, `ModelGradeRolloutWithoutTurnContextRefusedRuntimeIdentity`, `ModelGradeAgreeVerdictFailingScoresOrFailFindingRefusedVerdictMatch`, `ModelGradeOverallAgreeWithDisagreeEntryRefusedVerdictMatch`; acceptance cases `ModelGradeCodexResponseIndependentCodexReviewerSessionAccepted`, `ModelGradeConsistentDisagreeEntryRecordedFailingGrade`, `ModelGradeMixedRangeEachEntryRecordedOverallDisagree`.

`AgentContextProjectionEntity` (table `AgentContextProjections`, key (`WorkspaceId`, `ResponseId`), one row per response through that composite foreign key)

- `WorkspaceId`, `ResponseId`, and the response's root ids `PackageId` (128), `Agent` (64), `SessionId` (256), `RequestId` (256).
- `State` (16): `Pending`, `Projected`, `Removed`, `Skipped`, `Failed`, changed only by ResponseStatusTransition (section 3.5.5) and by a ProjectOneResponse save, as the projection row state diagram below draws. The row is created `Pending` when its response first enters `Trusted`.
- `GraphRagDocumentId` (128): the document while one exists in GraphRAG (kept on a `Failed` row after a delete error and on a `Pending` row awaiting removal), `ProjectedContentSha256` (64), `LastError` (2048), `UpdatedUtc`.
- Selection: a projection pass without `responseIds` selects `Pending` and `Failed` rows, and `Skipped` rows while ingest is enabled again; named `responseIds` select their rows in any state.
- Lifecycle rules: for a response that is not `Trusted`, a row that holds a `GraphRagDocumentId` has its document deleted on the next pass while GraphRAG is enabled, whatever `ProjectionEnabled` says (`ProjectionDeleteFailedRetriedNextPassDocumentRemoved`, `ProjectionDisabledAfterProjectedRejectedResponseDocumentStillRemoved`); a selected row whose response is not `Trusted` and that holds no document becomes `Removed` and is not selected again (`ProjectionPendingThenRejectedBeforeIngestRowRemovedNotReselected`); `ProjectionEnabled` false skips ingest only (`Skipped`, `skippedReason` `projection_disabled`); while GraphRAG is disabled an ingest is `Skipped` (`skippedReason` `graphrag_disabled`) and a row awaiting removal stays `Pending` until GraphRAG is enabled (`ProjectionGraphRagDisabledRemovalStaysPendingRunsWhenEnabled`). ProjectOneResponse makes its GraphRAG call before SerializableWrite (database-only delegate), then saves the row through SerializableWrite with its response locked and re-reads the response status inside the delegate, so a reject that commits during an ingest leaves the row `Pending` with the new document id, removed on the next pass (`ProjectionForcedInterleavingRejectDuringIngestDocumentRemovedNextPass`, on the three providers); the delegate also re-reads the row, and a save that finds it already `Projected` by an overlapping pass for the same content keeps that document and deletes its own after the save (`ProjectionOverlappingPassesOneDocumentKeptOtherDeleted`). Every pass ends with ReconcileProjectedDocuments (section 6.5): an `agent-response` document of the workspace that no `Projected` row names and that was ingested before the pass started is deleted, and a `Projected` row whose document is gone returns to `Pending`, so racing ingests, a save that exhausted its retries after an ingest, and a failed delete leave no document past the next complete pass.
- Workspace scope: projection ingests only into `GraphRagStorageScope.Workspace`, the GraphRAG retrieval path queries only that scope, and a hit naming a response outside the workspace is dropped, so a projected document never reaches the global GraphRAG scope or another workspace (`ProjectionAndGraphRagRetrievalWorkspaceScopedNoCrossWorkspaceHit`).

Entity relationships, one diagram per functional group, in the groups of Addendum A (A.5 to A.10, which lists every column). A solid line is a composite foreign key (`WorkspaceId`, parent key); a dotted line is a logical reference by root ids or by a node reference, enforced by the units, not by a foreign key. Each relationship is drawn once; an entity drawn in a group other than its own is a relationship target.

Packages and their build inputs:

![Packages and their build inputs (section 6.3) (entities), part 1 of 3](c4/rendered/STR-06-01.svg)

View: STR-06-01

![Packages and their build inputs (section 6.3) (entities), part 2 of 3](c4/rendered/STR-06-01-part2.svg)

View: STR-06-01-part2

![Packages and their build inputs (section 6.3) (entities), part 3 of 3](c4/rendered/STR-06-01-part3.svg)

View: STR-06-01-part3

Package omissions and findings:

![Package omissions and findings (section 6.3) (entities), part 1 of 2](c4/rendered/STR-06-02.svg)

View: STR-06-02

![Package omissions and findings (section 6.3) (entities), part 2 of 2](c4/rendered/STR-06-02-part2.svg)

View: STR-06-02-part2

Responses and evidence:

![Responses and evidence (section 6.3) (entities), part 1 of 2](c4/rendered/STR-06-03.svg)

View: STR-06-03

![Responses and evidence (section 6.3) (entities), part 2 of 2](c4/rendered/STR-06-03-part2.svg)

View: STR-06-03-part2

Grades:

![Grades (section 6.3) (entities), part 1 of 2](c4/rendered/STR-06-04.svg)

View: STR-06-04

![Grades (section 6.3) (entities), part 2 of 2](c4/rendered/STR-06-04-part2.svg)

View: STR-06-04-part2

HV requests:

![HV requests (section 6.3) (entities), part 1 of 2](c4/rendered/STR-06-05.svg)

View: STR-06-05

![HV requests (section 6.3) (entities), part 2 of 2](c4/rendered/STR-06-05-part2.svg)

View: STR-06-05-part2

Relationships and the GraphRAG projection:

![Relationships and the GraphRAG projection (section 6.3) (entities), part 1 of 2](c4/rendered/STR-06-06.svg)

View: STR-06-06

![Relationships and the GraphRAG projection (section 6.3) (entities), part 2 of 2](c4/rendered/STR-06-06-part2.svg)

View: STR-06-06-part2

Response lifecycle (the transitions ResponseStatusTransition allows, section 3.5.5):

![Response lifecycle (state diagram), part 1 of 9](c4/rendered/STR-06-07.svg)

View: STR-06-07

![Response lifecycle (state diagram), part 2 of 9](c4/rendered/STR-06-07-part2.svg)

View: STR-06-07-part2

![Response lifecycle (state diagram), part 3 of 9](c4/rendered/STR-06-07-part3.svg)

View: STR-06-07-part3

![Response lifecycle (state diagram), part 4 of 9](c4/rendered/STR-06-07-part4.svg)

View: STR-06-07-part4

![Response lifecycle (state diagram), part 5 of 9](c4/rendered/STR-06-07-part5.svg)

View: STR-06-07-part5

![Response lifecycle (state diagram), part 6 of 9](c4/rendered/STR-06-07-part6.svg)

View: STR-06-07-part6

![Response lifecycle (state diagram), part 7 of 9](c4/rendered/STR-06-07-part7.svg)

View: STR-06-07-part7

![Response lifecycle (state diagram), part 8 of 9](c4/rendered/STR-06-07-part8.svg)

View: STR-06-07-part8

![Response lifecycle (state diagram), part 9 of 9](c4/rendered/STR-06-07-part9.svg)

View: STR-06-07-part9

ResponseStatusTransition applies only the transitions drawn, inside SerializableWrite with the response (and for a supersede the replacement) locked in ascending `ResponseId` order; the trust decision moves a response between `Submitted`, `Grading`, and `Trusted` and keeps a `Stale` response `Stale`. A repeat of a transition already applied (a Reject of a `Rejected` response, a Supersede naming the same replacement) returns the response unchanged with 200. Every other requested transition is 409 `agent_context_transition_invalid`: a Reject of a `Superseded` response, a Supersede of a `Rejected` original or of an original superseded by another response, and any transition out of `Rejected` or `Superseded`. Supersede validation that is not a lifecycle question (a replacement for another request, a replacement that is `Rejected` or `Superseded`, a cycle) is 409 `agent_context_supersede_invalid`, and a grade for a `Rejected` or `Superseded` response is 409 `agent_context_response_not_gradable` (grade units only). Each transition sets `Status` and `StatusChangedUtc` (and `TrustedUtc` on entering `Trusted`) and applies its projection row and HV item effects in the same transaction.

Projection row states (`AgentContextProjectionEntity.State`; the choice node `ProjectOneResponseSave` is one ProjectOneResponse save):

![Projection row states (state diagram), part 1 of 6](c4/rendered/STR-06-08.svg)

View: STR-06-08

![Projection row states (state diagram), part 2 of 6](c4/rendered/STR-06-08-part2.svg)

View: STR-06-08-part2

![Projection row states (state diagram), part 3 of 6](c4/rendered/STR-06-08-part3.svg)

View: STR-06-08-part3

![Projection row states (state diagram), part 4 of 6](c4/rendered/STR-06-08-part4.svg)

View: STR-06-08-part4

![Projection row states (state diagram), part 5 of 6](c4/rendered/STR-06-08-part5.svg)

View: STR-06-08-part5

![Projection row states (state diagram), part 6 of 6](c4/rendered/STR-06-08-part6.svg)

View: STR-06-08-part6

The transitions out of `Pending`, `Projected`, `Skipped`, `Failed`, and `Removed` that name the response entering or leaving `Trusted` are ResponseStatusTransition's projection effect (section 3.5.5): entering `Trusted` marks the row `Pending` for ingest; leaving `Trusted` marks it `Pending` for removal when it holds a `GraphRagDocumentId`, otherwise `Removed`. Every transition out of `ProjectOneResponseSave` is that save, through SerializableWrite with the response locked; `Projected` to `Pending` for a document gone is ReconcileProjectedDocuments' save, also with the response locked (section 6.5).

### 6.4 Data transfer objects

Every agent-context DTO is a new type defined once, in `src/QBrainAi.Client/Models/AgentContext/` (namespace `QBrainAi.Client.Models.AgentContext`), with `[JsonPropertyName]` on every property and registration in `McpClientJsonContext` (type, `List<>`, `IReadOnlyList<>`, and each closed `PageResult<TItem>` below). The server uses these same types (it references `QBrainAi.Client` through `src/QBrainAi.Storage/QBrainAi.Storage.csproj:43`), so there is no server copy and no parity test. JSON is camelCase; unset optional fields are `null`; scores and thresholds travel as exact decimal lexemes, validated at deserialization by `ScoreLexemeJsonConverter` (namespace `QBrainAi.Client.Scoring`, section 3.5.4), so an invalid score is refused before any lookup.

Shared types

- `AgentContextItemDto { ordinal, kind, sourceRef, sourceSha256, sourceCommitSha, mandatory, score, rationale, tokenCount, retrievalPath, content }`.
- `AgentContextOmissionDto { ordinal, kind, sourceRef, sourceSha256, score, reason, detail }`.
- `AgentContextFindingDto { ordinal, kind, sourceRef, otherSourceRef, relationshipId, summary }`.
- `AgentResponseClaimDto { claimId, text }`; `AgentResponseEvidenceDto { claimId, kind, ref, citedSha256, sha256, commitSha }` (on submission the client supplies `claimId`, `kind`, `ref` (a `SessionLogTurn` ref as `<agent>/<sessionId>/<requestId>`), an optional `citedSha256`, and, for `File`, an optional `commitSha` (default: the workspace HEAD commit read at submission through `SourceResolver.ReadCommit`); the server computes `sha256` and ignores a supplied value).
- `AgentResponseGradeFindingDto { claimId, check, outcome, evidence }`.
- `AgentResponseGradeDto { gradeId, responseId, kind, grader, correctness, completeness, correctnessThreshold, completenessThreshold, passCount, failCount, unknownCount, verdict, hvRequestId, reviewerAgent, reviewerSessionId, reviewerRequestId, reviewerThreadId, requestJsonlPath, responseJsonlPath, rolloutJsonlPath, verdictJsonSha256, gradedUtc, recordedUtc, findings }` (`recordedUtc` is the server stamp that orders grades; `gradedUtc` is provenance).
- `AgentContextRelationshipDto { relationshipId, fromKind, fromRef, type, toKind, toRef, toSha256, evidenceRef, createdBy, createdUtc }`.
- `AgentContextCandidateDto { kind, sourceRef, sourceSha256, score, rationale, retrievalPath }`.
- Summaries in paged queries reuse the record's result type, so no summary type repeats its fields: a package summary is an `AgentContextPackageResult` with `items`, `omissions`, `findings`, and `renderedContent` null; a response summary is an `AgentResponseResult` with `claims`, `evidence`, `grades`, and `content` null; an HV request summary is an `AgentContextHvRequestResult` with `items` null.
- Paged results: every agent-context query that lists records returns `PageResult<TItem>` (namespace `QBrainAi.Client.Paging`, fields `totalCount`, `limit`, `offset`, `items`; section 3.5.2) and reads through PagedQuery with `PageBounds` policy `Refuse`: `limit` 1..1000, default 50, `offset` at least 0, otherwise 400 `validation_error` with `details.field` `limit` or `offset`.

Requests and results (one pair per unit of work, section 6.5)

- GetAgentContextStatusQuery `AgentContextStatusResult { mode, tokenBudget, bytesPerToken, trustCorrectness, trustCompleteness, graphRag { enabled, state, projectionEnabled, useForRetrieval }, counts { packages, responsesByStatus } }`; available in every Mode. `responsesByStatus` counts each response by its effective status, computed at read through IResponseStalenessRule read-only and written nowhere, as the pilot report counts, so a stored `Trusted` response whose dependency changed counts as `Stale` (`StatusAfterDependencyChangeCountsStaleNotTrustedWritesNothing`, section 6.5). Plugins make this read once per host session start for `mode` (section 6.2), so the handler makes one SourceResolver read per distinct dependency and commit, shared by every response that cites it, and answers within the SessionStart hook bound with 1000 `Trusted` responses (`StatusThousandTrustedResponsesAnswersWithinSessionStartHookBound`, section 6.5; inference: the work is bounded by the distinct sources cited, not by the response count); no parameter or query is added.
- BuildAgentContextPackageCommand `BuildAgentContextPackageRequest { sessionId, agent, requestId, queryText, tokenBudget, focusRefs, idempotencyKey }` (`agent`, `sessionId`, and `requestId` name the request's turn, located by TurnLocator; `tokenBudget` optional and at most the configured budget; `focusRefs` optional list of `{ kind, ref }` to prefer; the idempotency fingerprint `PayloadSha256` covers every field except `idempotencyKey`, section 6.3); result `AgentContextPackageResult { packageId, status, sessionId, agent, requestId, commitSha, workingTreeDirty, builderVersion, tokenBudget, mandatoryTokenCount, totalTokenCount, inputsSha256, contentSha256, createdUtc, items, omissions, findings, renderedContent }`.
- GetAgentContextPackageQuery get package: route id plus `includeContent` (bool, default true); result `AgentContextPackageResult`.
- QueryAgentContextPackagesQuery `AgentContextPackageQuery { agent, sessionId, requestId, status, limit, offset }`; result `PageResult<AgentContextPackageResult>` of package summaries (`PageBounds` `Refuse`).
- ReplayAgentContextPackageQuery replay: route id; result `AgentContextReplayResult { packageId, builderVersion, identical, originalContentSha256, replayContentSha256, originalInputsSha256, replayInputsSha256, differences [{ ordinal, kind, sourceRef, change }] }` with `change` in `added`, `removed`, `hashChanged`, `reordered`.
- SubmitAgentResponseCommand `SubmitAgentResponseRequest { packageId, agent, model, effort, invocationMode, contentFormat, content, claims, evidence, idempotencyKey }` (at least one claim, and every claim at least one evidence item; `agent` equal to the package's; `invocationMode` `Stateless` only for a package built in `Stateless`; no evidence ref naming the request's own turn; each refusal is 400 `validation_error` naming the field); result `AgentResponseResult { responseId, packageId, sessionId, requestId, agent, model, effort, invocationMode, status, statusReason, supersededByResponseId, contentFormat, contentSha256, submissionSha256, commitSha, submittedUtc, statusChangedUtc, trustedUtc, claims, evidence, grades, content }`.
- GetAgentResponseQuery get response: route id plus `includeContent`; result `AgentResponseResult`.
- QueryAgentResponsesQuery `AgentResponseQuery { requestId, packageId, status, trustedOnly, limit, offset }`; result `PageResult<AgentResponseResult>` of response summaries (`PageBounds` `Refuse`).
- RunDeterministicGradeCommand deterministic grade: route id, no body; result `AgentResponseGradeResult { grade, responseStatus, statusReason }`.
- RecordModelGradeCommand `RecordModelGradeRequest { hvRequestId, reviewerSessionId, reviewerRequestId, reviewerThreadId, grader, model, effort, correctness, completeness, findings, requestJsonlPath, responseJsonlPath, rolloutJsonlPath, gradedUtc }` (scores and findings equal the reviewer's verdict entry for the response, section 6.3; `gradedUtc` is optional provenance and never orders grades); result `AgentResponseGradeResult` plus `hvRequestStatus`.
- DecideAgentResponseTrustCommand trust decision (internal, no surface, not mode-gated): result `AgentResponseTrustDecision { responseId, previousStatus, status, statusReason, deterministicGradeId, modelGradeId, decidedUtc }`; the dispatching grade handler copies `status` and `statusReason` into `AgentResponseGradeResult.responseStatus` and `statusReason`, so `responseStatus` is `Stale` when the grade unit's EvaluateStalenessCommand dispatch persisted `Stale` before its write (the decision keeps a `Stale` response `Stale`, section 6.3, write isolation).
- RejectAgentResponseCommand `RejectAgentResponseRequest { reason }`; result `AgentResponseResult`.
- SupersedeAgentResponseCommand `SupersedeAgentResponseRequest { supersededByResponseId, reason }`; result `AgentResponseResult`.
- EvaluateStalenessCommand `EvaluateStalenessRequest { responseIds }` (validated and selected by ResponseIdList, section 3.5.5; empty means every `Submitted`, `Grading`, or `Trusted` response); result `EvaluateStalenessResult { evaluatedCount, newlyStale [{ responseId, changedRef, relationshipId, reason }], notFound [responseId] }`: an id that names no response in the workspace is listed in `notFound`, is not counted in `evaluatedCount`, and raises no error (`EvaluateStalenessUnknownIdsNotCountedNoError`).
- CreateAgentContextRelationshipCommand `CreateAgentContextRelationshipRequest { fromKind, fromRef, type, toKind, toRef, evidenceRef }` (type `supersedes` is refused with 400 `validation_error`, `details.field` `type`); result `AgentContextRelationshipResult` (the `AgentContextRelationshipDto` fields plus `created`, bool).
- DeleteAgentContextRelationshipCommand delete relationship: route id; result 204.
- QueryAgentContextRelationshipsQuery `AgentContextRelationshipQuery { nodeKind, nodeRef, type, direction (out, in, both), limit, offset }`; result `PageResult<AgentContextRelationshipDto>` (`PageBounds` `Refuse`).
- RetrieveAgentContextCandidatesQuery `RetrieveAgentContextCandidatesRequest { queryText, agent, sessionId, requestId, focusRefs, kinds, limit, trustedOnly, preferGraphRag }` (`agent`, `sessionId`, and `requestId` together name the turn whose relationship neighborhood ranks the relational path, all three or none, located by TurnLocator; `focusRefs` optional; `limit` under `PageBounds` `Refuse`, 1..1000, default 50; `trustedOnly` defaults to true and only `true` is accepted for `PriorResponse`); result `AgentContextRetrieveResult { retrievalPath, fallbackReason, candidates }`.
- ProjectAgentResponsesCommand `ProjectAgentContextRequest { responseIds }` (validated and selected by ResponseIdList; empty means the rows the projection selection of section 6.3 names); result `ProjectAgentResponsesResult { projected, removed, skipped, failed [{ responseId, error }], notFound [responseId], skippedReason }`: an id that names no response in the workspace is listed in `notFound` (`ProjectionUnknownResponseIdListedInNotFound`); `skippedReason` is `projection_disabled` when only `ProjectionEnabled` is false and `graphrag_disabled` when GraphRAG is disabled (`ProjectionProjectionDisabledSkippedReasonProjectionDisabled`).
- RunStatelessInvocation stateless invocation: the plugin process of section 6.6, not a unit. It reads Mode with GetAgentContextStatusQuery at host session start (or at the first prompt on a host without a SessionStart event), caches it as `agentContextMode` in the host session's `turn-state.yaml`, and reads that cache at each prompt; a read that fails, times out, or misses the SessionStart hook bound, or an unknown value, is `Off` (section 6.2; the query's counts are effective and each distinct dependency is read once, GetAgentContextStatusQuery above). It builds with BuildAgentContextPackageCommand, and keeps the request's `packageId` in its per-turn file `turns/<requestId>.yaml` (the session-log turn gains no `packageId` field); the model submits its response with the `agent_context_submit_response` tool (SubmitAgentResponseCommand) before it closes the turn, and the plugin supplies that `packageId` (found again with QueryAgentContextPackagesQuery `{ agent, sessionId, requestId, status: Built }` when a hook process boundary lost it). In `Stateless` the model's input is the rendered package, the current request, and the host's deterministic `REQUIRED MEMORIES` block (FR-MCP-MEMORY-003, -007), with history excluded; the sentinel case `StatelessHostSentinelPriorTurnAbsentFromModelRequest` (section 6.6) asserts both that a prior turn is absent and that the memory block is present in the model request. On a build failure other than 409 `agent_context_disabled` (which caches `Off`, section 6.2), including a REPL refusal `transaction_degraded` or `mutation_not_transactional` (section 3.1, "Transaction-gated REPL clients"; `StatelessInvocationBuildTransactionDegradedTreatedAsBuildError`, section 6.6), the invocation fails: no answer is produced, the prompt is refused with the error, and the plugin (not the model) writes the `canceled` close, its `failureNote` starting with the token `agent_context_invocation_failed` followed by the failure code (the token the pilot report's `refusedPrompts` counts), on the persist path of process PersistTurnWriteWithReceipt, where `Test-ModelEvidence` never applies to a `canceled` close (updates FR-MCP-TRIAGESTORE-001 `ac-2`, under which hooks never produce `canceled` turns); an outage (any retryable answer, a 409 `conflict` with `retryable: true` included, section 3.1, "Retryable answers") holds that close in the failsafe, and the Stop gate warns degraded persistence naming the package failure (it never blocks) and keeps the ledger entry until the drain confirms the close.
- CreateAgentContextHvRequestCommand `CreateAgentContextHvRequestRequest { sessionId, agent, fromRequestId, toRequestId, requestedBy, reason, idempotencyKey }`; result `AgentContextHvRequestResult { hvRequestId, status, sessionId, agent, fromRequestId, toRequestId, requestedBy, reason, createdUtc, completedUtc, items [{ requestId, responseId, disposition, closedReason }] }`.
- GetAgentContextHvRequestQuery get HV request: route id; result `AgentContextHvRequestResult`.
- QueryAgentContextHvRequestsQuery `AgentContextHvRequestQuery { sessionId, status, limit, offset }`; result `PageResult<AgentContextHvRequestResult>` of HV request summaries (`PageBounds` `Refuse`).
- GetAgentContextPilotReportQuery `AgentContextPilotReportQuery { fromUtc, toUtc }`; result `AgentContextPilotReport { fromUtc, toUtc, byMode [{ invocationMode, responses, trusted, rejected, stale, meanCorrectness, meanCompleteness, trustRate }], overflowPackages, packagesWithoutResponse, refusedPrompts }`. `byMode` compares the responses of the window by `invocationMode` (`Stateful` from `Pilot`, `Stateless` from a period in `Stateless` on hosts with history exclusion) and counts each response by its effective status (IResponseStalenessRule, read-only; the query writes nothing, `PilotReportAfterDependencyChangeCountsStaleNotTrustedWritesNothing`); the means are over responses that have a model grade, and `trustRate` is trusted over all responses of the mode. `overflowPackages` counts the packages of the window that failed (`Overflow`; in `Pilot` or `Stateless` each one refused its prompt), `packagesWithoutResponse` counts the `Built` packages of the window that no response answers (no `AgentResponse` with that `PackageId`), and `refusedPrompts` counts the turns of the window closed `canceled` (or with the legacy spelling `cancelled`) whose `failureNote` starts with the token `agent_context_invocation_failed` that RunStatelessInvocation writes, so a refusal for an outage or for a commit, profile, or budget failure counts alike (`PilotReportCountsOverflowRefusedAndPackageWithoutResponse`, section 6.5).

Error codes (problem+json body `{ type, title, status, detail, code, message, retryable, details }`; MCP tools return `{ code, error, message, retryable, details }`). Every handler fails with `Result<T>.Failure(new ClassifiedErrorException(code, status, message, details, retryable))` (namespace `QBrainAi.Client.Errors`, section 3.5.2), and `ErrorCodes` holds each code with its HTTP status and retryable flag. Every code below is final (`retryable: false`) except 409 `conflict` answered with `retryable: true` and the 503 storage answers. Each code or branch names its case; a case named for several branches or units is one theory over them.

- 400 `validation_error` with `details.field` for malformed input, including a paging `limit` or `offset` outside `PageBounds` (`PagedQueriesLimitOrOffsetOutOfBounds400NamesField`, over the four paged queries and RetrieveAgentContextCandidatesQuery's `limit`), a malformed response id list (ResponseIdList), a reference that names no existing source (SourceResolver, `details.field` naming the reference), a client request for relationship type `supersedes` (`details.field` `type`), an evidence ref naming the request's own turn, a submission `agent` that differs from the package's, and an `invocationMode` `Stateless` against a package not built in `Stateless`; plus the score codes `review_score_format`, `review_score_precision`, `review_score_out_of_range` for grade scores (ScoreLexeme, section 3.5.4).
- 400 `validation_error` with `details.field` `workspacePath` (WorkspaceResolutionBehavior, section 3.5.1: a record path that names no registered workspace, refused after the dispatch, `WorkspaceResolutionUnregisteredPath400EverySurface`); on REST the workspace middleware answers before any controller with its own bodies (TR-MCP-MT-002).
- 400 `agent_context_receipt_paths_required` (model grade without its request, response, and rollout jsonl paths, the reviewer turn ids, or `reviewerThreadId`; `ModelGradeMissingReceiptPathsOrReviewerIds400ReceiptPathsRequired`).
- 400 `agent_context_relationship_invalid` (unknown kind or type, or a self-link; `CreateRelationshipUnknownKindTypeOrSelfLink400RelationshipInvalid`).
- 400 `agent_context_hv_request_invalid` (unknown session, an unknown or out-of-order range, a range over `HvRequest:MaxTurns`, or a range with no agent response; `HvRequestUnknownSessionBadRangeOverMaxTurnsOrNoResponse400HvRequestInvalid`).
- 403 `agent_context_grader_not_allowed` (model grade from a grader other than the configured one; `ModelGradeOtherGrader403GraderNotAllowed`).
- 404 `not_found` with `details.resource`: `package`, `response`, `relationship`, `session_turn` (TurnLocator found no session or no turn for `agent`, `sessionId`, and `requestId`), or `hv_request` (`RouteIdUnknownInWorkspace404NotFoundNamesResource`, over the four route ids).
- 409 `agent_context_disabled` (Mode `Off`: ModeGateBehavior refuses every agent-context command except DecideAgentResponseTrustCommand; the read-only queries stay available; `ModeOffEveryGatedCommand409AgentContextDisabledQueriesAnswer`, over every gated command and every query).
- 409 `agent_context_grade_not_in_hv_request` (the response is not an item of the named HV request; `ModelGradeResponseNotItemOfHvRequest409GradeNotInHvRequest`) and 409 `agent_context_hv_request_closed` (the HV request is `Completed`; `ModelGradeCompletedHvRequest409HvRequestClosed`).
- 409 `agent_context_package_overflow` (a response or replay against an `Overflow` package, BuiltPackageLookup; `SubmitOrReplayOverflowPackage409PackageOverflow`).
- 409 `agent_context_response_not_gradable` (a grade for a `Rejected` or `Superseded` response; grade units only; `GradeRejectedOrSupersededResponse409ResponseNotGradable`, over both grade units).
- 409 `agent_context_transition_invalid` (a status transition the response lifecycle does not allow, ResponseStatusTransition: a Reject of a `Superseded` response, a Supersede of a `Rejected` original or of an original superseded by another response, and every other transition the lifecycle does not draw; a repeat of an applied transition returns 200 unchanged).
- 409 `agent_context_supersede_invalid` (supersede validation: a replacement for another request, a replacement that is `Rejected` or `Superseded`, a cycle, or a `supersedes` link that would close a cycle; `SupersedeOtherRequestInvalidReplacementOrCycle409SupersedeInvalid`).
- 409 `agent_context_relationship_protected` (deleting a link a command created: Build's `derived_from` and `relevant_to` links, Submit's `derived_from` and `supports` links, Supersede's `supersedes` link).
- 409 `agent_context_commit_unavailable` (Build or Submit: the workspace is not a git checkout or its HEAD commit cannot be read, `SourceResolver.ReadCommit`; `BuildCommitUnreadable409CommitUnavailable`, `SubmitCommitUnreadable409CommitUnavailable`).
- 409 `agent_context_profile_unavailable` (no configured profile file or no rule file resolves; mandatory context cannot be assembled; `BuildNoProfileOrRuleFile409ProfileUnavailable`).
- 409 `conflict` with `retryable: false` (IdempotentCreate: an idempotency key, or a model grade's natural key, reused with other client inputs) and 409 `conflict` with `retryable: true` (SerializableWrite exhausted its four attempts, or ReadableKeyAllocator its attempts under the same bound; nothing saved; the exhaustion exit of every unit that runs through SerializableWrite, section 6.3; a plugin treats it as an outage, section 3.1).
- 409 `agent_context_replay_snapshot_unavailable` (the package has no stored input snapshot; `ReplayNoInputSnapshot409ReplaySnapshotUnavailable`) and 409 `agent_context_replay_builder_changed` (the package was built by another builder version; `details { recorded, current }`; `ReplayOtherBuilderVersion409ReplayBuilderChangedRecordedAndCurrent`).
- 422 `agent_context_receipt_unverified` with `details.check` naming the failed check: `receipt_path`, `request_meta`, `verdict_block`, `verdict_binding`, `verdict_match`, `reviewer_turn`, `reviewer_independent`, or `runtime_identity` (each check's case is listed in section 6.3, receipt and reviewer verification).
- 422 `agent_context_budget_exceeded` with `details { tokenBudget, mandatoryTokenCount, overflowTokenCount, mandatoryItems [{ kind, sourceRef, tokenCount }] }`.
- 503 storage answers, retryable, as develop gives them today outside SerializableWrite: `backend_unavailable` (storage unreachable, or over its command budget, `src/QBrainAi.Storage/StorageCommandBudget.cs:67-71`) and `persistence_error` with `retryable: true` (a busy database, `src/QBrainAi.Support.Mcp/Services/McpErrorClassifier.cs:129-139`); a plugin treats either as an outage.
- REPL only, before any request reaches the server: `transaction_degraded` (the turn transaction coordinator is degraded) and `mutation_not_transactional` (required turn transactions are active and the method is not allowed), from `ClientMutationPolicy` in the REPL error envelope with `details { clientName, methodName, policy }` (FR-MCP-120, TR-MCP-TXN-001; `src/QBrainAi.Repl.Core/ClientMutationPolicy.cs:197-218`, `src/QBrainAi.Repl.Core/ReplCommandDispatcher.cs:3295-3316`); both are final and in `ErrorCodes`. The `agentContext` client namespace is service-gated and its read methods are on the read allowlist (section 3.1, "Transaction-gated REPL clients"), and `Evaluate` drops the `!ReadMethods.ContainsKey(clientName)` clause at `ClientMutationPolicy.cs:213`, which today denies the service-gated pass to any namespace with listed reads (no develop namespace is in both lists, so no existing decision changes), so a read is never refused, a write is refused with `transaction_degraded` while the coordinator is degraded, and a write is allowed while transactions are required, because each write runs in the server's SerializableWrite (`ReplAgentContextReadsAllowedWhileDegradedWritesTransactionDegraded`, `ReplAgentContextWritesAllowedWhileTransactionsRequiredServiceGated`). A plugin that receives either code from a build treats it as a build error (section 6.4, RunStatelessInvocation).

### 6.5 Units of work (CQRS handlers), endpoints, and activity diagrams

Each unit is exactly one CQRS command or query with its handler (operator, 2026-10-11: "I want to be clear, unit of work maps to one CQRS handler"), one file per record and one per handler (MA0048: the record in `<Name>.cs`, its handler in `<Name>Handler.cs`, both paths named in each unit), registered by `AddCqrsHandlers`, in the folder of the bounded context that owns it (section 8.3), with the namespace equal to the folder (section 3.5): ContextPackages in `src/QBrainAi.Services/AgentContext/Packages/Commands/` or `Queries/` (namespace `QBrainAi.Support.Mcp.AgentContext.Packages.Commands` or `.Queries`), ResponsesAndTrust in `src/QBrainAi.Services/AgentContext/Responses/Commands/` or `Queries/` (`QBrainAi.Support.Mcp.AgentContext.Responses.Commands` or `.Queries`), and ContextGraph in `src/QBrainAi.Services/AgentContext/Graph/Commands/` or `Queries/` (`QBrainAi.Support.Mcp.AgentContext.Graph.Commands` or `.Queries`). Request and result DTOs are defined once in `src/QBrainAi.Client/Models/AgentContext/` (`QBrainAi.Client.Models.AgentContext`, section 6.4). Every surface dispatches the same record through `IDispatcher`: the controller and the MCP tool call `SendAsync` or `QueryAsync`; the typed client, REPL, and plugins reach them over REST. A handler reports a contract error as `Result<T>.Failure(new ClassifiedErrorException(code, status, message, details, retryable))` (section 3.5.2); `McpErrorClassifier` maps it, in its one branch, to the `ClassifiedError` problem+json body and the MCP tool error with that status, `code`, and `retryable`. The stateless invocation is not a unit: it is the plugin process RunStatelessInvocation (section 6.6).

Every REST route below exists under `/qbrainai/agent-context` and the alias `/mcpserver/agent-context` (controller `AgentContextController`, `[ApiController]`, authenticated by `WorkspaceAuthMiddleware`; `WorkspaceResolutionMiddleware` resolves the workspace from the `X-Workspace-Path` header or, without one, from the API key, and answers an unknown header with 400 and an unresolved workspace with 404 in its own `{error}` bodies before any controller, `src/QBrainAi.Support.Mcp/Middleware/WorkspaceResolutionMiddleware.cs:97-139,168-177`, section 3.5.1). MCP tools live in `FwhMcpTools.AgentContext.cs` with `workspacePath` as the first parameter and JSON-string bodies. The typed client is `AgentContextClient : McpClientBase`, exposed as `QBrainAiClient.AgentContext`. The REPL reaches it as `client.AgentContext.<Method>` (added to the `GenericClientPassthrough` allow-list as `AGENTCONTEXT`; classified in `ClientMutationPolicy` as stated below; YAML validators registered with explicit `payload.params.<key>` paths). Typed client signatures, which the REPL binds by parameter name, case-insensitively (`GenericClientPassthrough.cs:649-695`; a missing required parameter fails with `Missing required parameter`): a route id is a separate `string` parameter named `packageId`, `responseId`, `relationshipId`, or `hvRequestId`; a command's body record is one parameter named `request`; a query's filter record is one parameter named `query`; a scalar such as `includeContent` keeps its name. So the REPL params of BuildPackageAsync are `{ request: { sessionId, agent, requestId, queryText, tokenBudget, focusRefs, idempotencyKey } }`, of GetPackageAsync `{ packageId, includeContent }`, of RejectResponseAsync `{ responseId, request: { reason } }`, and of QueryPackagesAsync `{ query: { agent, sessionId, requestId, status, limit, offset } }`. PowerShell plugins call `Invoke-McpPlugin.ps1 -Command Invoke -Method client.AgentContext.<Method> -ParamsObject <params>` with `<params>` from the exported builder named in each unit (`McpPluginShim.psm1`). Builder contract, one builder per externally callable unit: `New-McpAgentContext<Verb>Params` takes one PowerShell parameter per route id, scalar, and body or filter field, named in PascalCase, typed string as `[string]`, bool as `[bool]`, int as `[int]`, a decimal score as `[string]` holding the exact lexeme, and a list as `[object[]]` of hashtables with the record's camelCase keys; a parameter is mandatory exactly when the client parameter or record field is required, and an omitted optional parameter is left out of the output (the server applies the default); the builder returns that envelope as an ordered hashtable: route ids and scalars at the top level under their client parameter names, and the record's camelCase fields, in record order with the values unchanged, nested under `request` or `query`; it never calls the server (`AgentContextBuildersParametersMatchRequestRecordsOutputKeysAndValuesExact`, generated over every unit from `docs/context/agent-context-contract.json`, and `AgentContextBuildersEnvelopesBindThroughGenericClientPassthrough`, which binds every builder's output through the real `GenericClientPassthrough` binder against the real `AgentContextClient` signatures). Tool descriptor contract: each externally callable unit has one entry in `lib/tool-descriptors.json` with `name` (the `agent_context_*` name in each unit), `description`, `inputSchema` (the JSON schema of that same envelope: route ids and scalars as top-level properties, the body as a required `request` or `query` object with the record's schema, scores as strings), `method` (the `client.AgentContext.<Method>` REPL method listed in each unit), and `mutates` (true for commands, false for queries), and the file holds exactly one `agent_context_*` entry per externally callable unit of `docs/context/agent-context-contract.json`, with that unit's method, `mutates` flag, and envelope schema (`ToolDescriptorsAgentContextOneEntryPerExternalUnitMatchesContract`); a TypeScript host's adapter runs `Invoke-McpPlugin.ps1 -Command Invoke -Method <method> -InvocationFromStdin` with `{ params, hostContext }` as JSON on stdin (the call's arguments, an envelope of that schema, and the host's separate tool context) (TR-MCP-SESSIONTURN-008), so PowerShell and TypeScript hosts reach the same command (operator, 2026-10-11: "You should be designing all scripts and script tools around PowerShell and only PowerShell.").

Transaction-gated REPL clients (section 3.1; updates FR-MCP-120 and TR-MCP-TXN-001 with the agent-context classification): in `KnownUnsafeClientMutationPolicy` the `agentContext` client namespace joins `ServiceGatedClientNamespaces`, because each of its writes runs in the server's SerializableWrite, and its eleven query methods (`GetStatusAsync`, `GetPackageAsync`, `QueryPackagesAsync`, `ReplayPackageAsync`, `GetResponseAsync`, `QueryResponsesAsync`, `QueryRelationshipsAsync`, `RetrieveAsync`, `GetPilotReportAsync`, `GetHvRequestAsync`, `QueryHvRequestsAsync`) join `ReadMethods` (`src/QBrainAi.Repl.Core/ClientMutationPolicy.cs:100-174`). `Evaluate` drops the `!ReadMethods.ContainsKey(clientName)` clause at `:213`, which today keeps any namespace with listed reads from being service-gated (section 3.1); no existing decision changes, because no develop namespace is in both lists (`:100-174`). A listed read passes before any state check (`:197-198`), so no query below is refused by the policy. A command is refused in the REPL, before any server call, with `transaction_degraded` while the turn transaction coordinator is degraded (`:201-208`); `mutation_not_transactional` (`:216-218`) answers a method that is neither a listed read nor in a service-gated namespace while required turn transactions are active, which no agent-context command reaches once the namespace is service-gated. Both codes join `ErrorCodes` and every command's error list below as REPL only; a plugin that receives either from a build treats it as a build error (section 6.6). Cases (section 6.4): `ReplAgentContextReadsAllowedWhileDegradedWritesTransactionDegraded`, `ReplAgentContextWritesAllowedWhileTransactionsRequiredServiceGated`.

Projection pass (section 3.1, Projection pass), stated here once and not drawn per unit: a unit whose transaction marked a projection row through ResponseStatusTransition (the grade units through the trust decision, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand) dispatches ProjectAgentResponsesCommand with no `responseIds` through `IDispatcher` after its SerializableWrite commits and before it answers, logging a failed dispatch without changing its own answer (`ProjectionPassDispatchedAfterTrustCommitFailureLoggedUnitSucceeds`). Dependent staleness (section 3.1, Dependent responses), stated here once and not drawn per unit either: the same units, when their commit moved a response out of `Trusted` or into `Rejected`, `Superseded`, or `Stale`, first dispatch EvaluateStalenessCommand with the `responseIds` of the responses that depend on it, with the same failure rule (`ResponseLeavingTrustedDispatchesStalenessForDependentsAfterCommitFailureLoggedUnitSucceeds`).

Request pipeline (section 3.5.1), stated here once and not drawn per unit: WorkspaceResolutionBehavior and then ModeGateBehavior run inside the scoped `IDispatcher` before every handler below, after the controller or the MCP tool calls `SendAsync` or `QueryAsync`; request validation runs in the handler and is its first drawn step, so each diagram starts where the handler starts. WorkspaceResolutionBehavior refuses a record path that names no registered workspace with 400 `validation_error`, `details.field` `workspacePath`, in every unit (`WorkspaceResolutionUnregisteredPath400EverySurface`). ModeGateBehavior refuses with 409 `agent_context_disabled` while `Mcp:AgentContext:Mode` is `Off`, only for a gated record (`ModeOffEveryGatedCommand409AgentContextDisabledQueriesAnswer`). Gated means the record implements `IModeGatedRequest`, which every agent-context command does except DecideAgentResponseTrustCommand: BuildAgentContextPackageCommand, SubmitAgentResponseCommand, RunDeterministicGradeCommand, RecordModelGradeCommand, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand, CreateAgentContextRelationshipCommand, DeleteAgentContextRelationshipCommand, ProjectAgentResponsesCommand, and CreateAgentContextHvRequestCommand. DecideAgentResponseTrustCommand (dispatched only by the two grade units, inside their transaction) and every query (GetAgentContextStatusQuery and the read-only queries) do not implement it and are not gated, so packages, responses, grades, relationships, HV requests, and the pilot report stay readable for audit in `Off` (section 3.1, Reads in `Off`). Each unit's error list names the workspace 400 and, for a gated unit only, the 409 `agent_context_disabled`. A requirement update or delete dispatches EvaluateStalenessCommand only while Mode is not `Off`; a dependency that changed while Mode was `Off` is still never reported fresh, because every read evaluates freshness read-only and every unit that relies on a persisted status dispatches EvaluateStalenessCommand before it relies on that status and before its own SerializableWrite.

Shared components (section 3.5) are drawn as call nodes `[["Component: purpose"]]` with only the exits the component returns, and their rules are not restated here: TurnLocator, IdempotentCreate (policy `Conflict` for every agent-context create, `ReturnStored` inside RelationshipWriter; the lookup runs first, before any precondition that can change between attempts; a unit that reads outside the database (Build, Submit, and the model grade) runs it read-only before those reads and again as the first step of its delegate), SerializableWrite (drawn as a call node followed by a box that holds its delegate: every check in the box and the save run in one Serializable transaction, a refusal inside the box rolls back, and every unit that runs through it has the exit 409 `conflict` with `retryable: true` when its four attempts are exhausted; the box reads and writes only the database, so a unit's git, file, source, retrieval, and GraphRAG reads are drawn before the call node, and the box re-checks the database state those reads relied on, a changed state ending the attempt so that the unit reads again within the same bound, section 3.1, Database-only delegates), ReadableKeyAllocator (the `pkg-`, `rsp-`, `grd-`, `rel-`, and `hvr-` keys, named where each unit inserts the record, in the save node or in the text above its diagram; its exhaustion is the same retryable 409 `conflict`), PagedQuery with `PageBounds` policy `Refuse` and the wire type `PageResult<TItem>`, SourceResolver, PackageAssembler with its nested loop FillBudgetWithCandidates, CandidateRanker, BuiltPackageLookup, ResponseStatusTransition, IResponseStalenessRule, ResponseIdList, RelationshipWriter, ScoreLexeme, ScoreVerdict, and SecretRedactor. A record looked up by its route id that does not exist in the workspace is 404 `not_found` with `details.resource` naming it (`package`, `response`, `relationship`, `hv_request`; `RouteIdUnknownInWorkspace404NotFoundNamesResource`). A paging `limit` or `offset` outside `PageBounds` is 400 `validation_error` with `details.field` naming it (`PagedQueriesLimitOrOffsetOutOfBounds400NamesField`, over the four paged queries and RetrieveAgentContextCandidatesQuery's `limit`). Outside SerializableWrite every unit keeps develop's retryable 503 storage answers, `backend_unavailable` and, for a busy store, `persistence_error` with `retryable: true` (`src/QBrainAi.Support.Mcp/Services/McpErrorClassifier.cs:129-139` at `a8d3934b`, section 6.4); they are stated here once and not repeated in each error list, and a plugin treats either as an outage (section 3.1, Deadlock and busy outcomes).

#### GetAgentContextStatusQuery: get status [FR-MCP-AGENTCONTEXT-008, -009]

- Handler: `GetAgentContextStatusQuery(string WorkspacePath) : IQuery<AgentContextStatusResult>` in `src/QBrainAi.Services/AgentContext/Packages/Queries/GetAgentContextStatusQuery.cs` and `GetAgentContextStatusQueryHandler` in `src/QBrainAi.Services/AgentContext/Packages/Queries/GetAgentContextStatusQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/status`; MCP `agent_context_status(workspacePath)`; client `GetStatusAsync()`; REPL `client.AgentContext.GetStatusAsync`; PowerShell `New-McpAgentContextStatusParams`; tool descriptor `agent_context_status`.
- Not gated: it answers in every Mode, `Off` included, and is the call through which plugins read `mode` (section 6.6).
- Counts: responses are counted by effective status, computed at read and written nowhere, through IResponseStalenessRule read-only, so a stored `Trusted` response whose dependency changed counts as `Stale`; the query writes nothing (`StatusAfterDependencyChangeCountsStaleNotTrustedWritesNothing`).
- Cost at host session start (risk R-R4; no parameter or query is added): plugins make this read once per host session start for `mode` (section 6.6), so the counts must not grow with responses times dependencies. The handler evaluates the `Submitted`, `Grading`, and `Trusted` responses with one SourceResolver read per distinct dependency and commit, the read each evaluation makes anyway, shared by every response that cites that dependency (inference: the work is bounded by the distinct files, requirements, receipts, and turns cited, not by the response count), and answers within the SessionStart hook bound with 1000 `Trusted` responses (`StatusThousandTrustedResponsesAnswersWithinSessionStartHookBound`). A plugin whose read misses the hook bound has an unreadable Mode and runs the host session as `Off` (section 3.1, Mode changes).
- Request: none. Response 200 `AgentContextStatusResult`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior). REPL: a listed read, never refused by ClientMutationPolicy.

![GetAgentContextStatusQuery: get status (activity), part 1 of 2](c4/rendered/GetAgentContextStatusQuery.svg)

View: GetAgentContextStatusQuery

![GetAgentContextStatusQuery: get status (activity), part 2 of 2](c4/rendered/GetAgentContextStatusQuery-part2.svg)

View: GetAgentContextStatusQuery-part2

#### BuildAgentContextPackageCommand: build a context package [FR-MCP-AGENTCONTEXT-001, -002]

- Handler: `BuildAgentContextPackageCommand(string WorkspacePath, BuildAgentContextPackageRequest Request) : ICommand<AgentContextPackageResult>` in `src/QBrainAi.Services/AgentContext/Packages/Commands/BuildAgentContextPackageCommand.cs` and `BuildAgentContextPackageCommandHandler` in `src/QBrainAi.Services/AgentContext/Packages/Commands/BuildAgentContextPackageCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/packages`; MCP `agent_context_build_package(workspacePath, requestJson)`; client `BuildPackageAsync(BuildAgentContextPackageRequest)`; REPL `client.AgentContext.BuildPackageAsync`; PowerShell `New-McpAgentContextBuildPackageParams`; tool descriptor `agent_context_build_package`.
- Request `BuildAgentContextPackageRequest`. Response 201 `AgentContextPackageResult` with status `Built` (Location `/qbrainai/agent-context/packages/{packageId}`), or 200 with the stored `Built` package when the idempotency key repeats with identical client inputs. A success response always carries a `Built` package.
- Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; a missing field, `tokenBudget` outside 1024 and the configured budget, or TurnLocator `InvalidIdentifier` with `details.field` `sessionId` or `requestId`); 404 `not_found` (`session_turn` for TurnLocator `SessionNotFound` or `TurnNotFound` with `agent`, `sessionId`, and `requestId`, so a turn of another agent is not found, `BuildTurnOfOtherAgent404SessionTurn`); 409 `agent_context_disabled`, `agent_context_commit_unavailable` (SourceResolver `ReadCommit`, `BuildCommitUnreadable409CommitUnavailable`), `agent_context_profile_unavailable` (`BuildNoProfileOrRuleFile409ProfileUnavailable`), `conflict` with `retryable: false` (the key reused with other client inputs), `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator exhausted after four attempts); 422 `agent_context_budget_exceeded` (an `Overflow` package is stored for audit with its mandatory items and token counts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`. The 422 is invariant under retry: when the key repeats with identical client inputs and the stored package is `Overflow`, the build returns the same 422 with the stored diagnostics, never 200 (`BuildRetryAfterLostOverflowResponseSame422StoredDiagnostics`).
- Idempotency (IdempotentCreate, policy `Conflict`): the fingerprint `PayloadSha256` is over the request's client inputs (`sessionId`, `agent`, `requestId`, `queryText`, `tokenBudget`, `focusRefs`), never over sources; `InputsSha256` stays the provenance and replay check. The unit runs the lookup read-only first, before the turn, commit, profile, retrieval, staleness, and source reads, and the delegate runs it again as its first step (section 3.5.2), so a retry of a lost 201 returns the stored package without reading any source, even after a source, a response status, or the HEAD commit changed (`BuildRetryAfterSourceChangedSameKeySameInputs200Stored`), and a second build of the same request with the same key, such as a duplicate hook delivery, returns the stored package and never refuses (`BuildDuplicateDeliverySameKey200StoredPackage`). Two concurrent builds with one key: the loser's insert hits the key's unique index, SerializableWrite runs its delegate again, and the delegate's lookup then returns the winner's package with 200, the same 422 when it is `Overflow` (`BuildConcurrentOverflowBoth422`), or 409 `conflict` with `retryable: false` when the client inputs differ.
- Reads before the write (section 3.1, Database-only delegates; nested activity ReadBuildInputs): with no transaction open, the unit locates the turn and notes its status, reads the HEAD commit and the profile and rule files, gathers the candidates (dispatching RetrieveAgentContextCandidatesQuery, which calls GraphRAG when preferred), reads each prior-response candidate's effective status through IResponseStalenessRule read-only, and reads and hashes every candidate; PackageAssembler then runs on that snapshot in memory. The SerializableWrite delegate reads and writes only the database: it runs the key lookup again, re-checks that the turn still has the status the reads saw (a change ends the attempt and the unit reads again, within SerializableWrite's bound of four attempts, `BuildTurnStatusChangedDuringReadsReadsAgainThenSaves`), and inserts the package, its children, its input snapshot, and its links. No transaction is open during a retrieval, a GraphRAG call, or a git read, so on SQLite no hook write waits on them (`BuildOnSqliteHookWriteDuringRetrievalNotBlocked`). Build persists nothing but its package and its links: it dispatches no EvaluateStalenessCommand and writes no response status (only EvaluateStalenessCommand persists `Stale`, section 3.1).
- Candidates: the request, the profile and rule files, and the applicable requirements (from the turn's TODO and relationships) are mandatory; RetrieveAgentContextCandidatesQuery, dispatched with the package's `agent`, `sessionId`, `requestId`, and `focusRefs` and with `preferGraphRag` equal to `GraphRag:UseForRetrieval`, supplies the TODOs, session-log turns, receipts, trusted fresh prior responses, and code at the HEAD commit; every focus ref is also a candidate as named, so a focus ref can name a prior response in any status. The request's own turn is never a candidate: the `Request` item carries its text (section 6.3, `BuildOwnTurnNotACandidateNoDerivedFromOwnTurn`). Each prior-response candidate's effective status comes from IResponseStalenessRule read-only and is recorded as its `ResponseStatus` input, so the omission filter records `untrusted`, `stale`, `rejected`, or `superseded` for a focus-ref prior response that is not `Trusted` and fresh (`BuildFocusRefNamesRejectedResponseOmittedRejected`). Build writes no response status: it reads the effective status, as the queries do.
- Deterministic construction (PackageAssembler, section 3.5.5): the candidates are filtered, ranked by CandidateRanker, cut to the candidate limits and the budget (FillBudgetWithCandidates), and rendered in the fixed kind order Request, Profile, Rule, Requirement, Todo, SessionLogTurn, Receipt, PriorResponse, Code. The rendered text is canonical: LF line endings, one header line per item `[kind] sourceRef sha256`, then one line per finding in finding order, `[finding] <kind> <sourceRef> <otherSourceRef> <summary>`, so the same inputs at the same commit give the same `contentSha256` and the findings are covered by it (`RenderFindingsAfterItemsCoveredByContentSha256`). Mandatory tokens equal to the budget build; one token more overflows (`BuildMandatoryEqualsBudgetBuiltOneMoreOverflows`).
- Links (RelationshipWriter, in the package's own transaction, with the target hashes ReadBuildInputs read, `CreatedBy` `BuildAgentContextPackageCommand`, so they are protected from deletion): the package is `derived_from` each included source except a `Rule` item read outside version control (provenance only, `BuildUntrackedRuleSourceNoDerivedFromLinkHashKeptOnItem`) and `relevant_to` its turn; a failure before the commit leaves neither the package nor a link (`BuildLinkWriteFailsNothingCommittedRetryCreatesLinks`).

The diagram draws the reads as one call of the nested activity ReadBuildInputs below; the overflow and the built package are inserted with a `pkg-` key from ReadableKeyAllocator through IdempotentCreate (the `Overflow` package with its mandatory items, token counts, and input snapshot and no content; the `Built` package with its items, omissions, findings, focus refs, and input snapshot). The 422 carries the token counts and mandatory items, the stored ones on a repeat. The SerializableWrite and its delegate are drawn as one call of the nested activity SavePackage below, whose edges are its returns. One 409 `conflict` terminal stands for both conflict answers: `retryable: false` for the key reused with other client inputs, and `retryable: true` when SerializableWrite or ReadableKeyAllocator is exhausted after four attempts.

![BuildAgentContextPackageCommand: build a context package (activity), part 1 of 6](c4/rendered/BuildAgentContextPackageCommand.svg)

View: BuildAgentContextPackageCommand

![BuildAgentContextPackageCommand: build a context package (activity), part 2 of 6](c4/rendered/BuildAgentContextPackageCommand-part2.svg)

View: BuildAgentContextPackageCommand-part2

![BuildAgentContextPackageCommand: build a context package (activity), part 3 of 6](c4/rendered/BuildAgentContextPackageCommand-part3.svg)

View: BuildAgentContextPackageCommand-part3

![BuildAgentContextPackageCommand: build a context package (activity), part 4 of 6](c4/rendered/BuildAgentContextPackageCommand-part4.svg)

View: BuildAgentContextPackageCommand-part4

![BuildAgentContextPackageCommand: build a context package (activity), part 5 of 6](c4/rendered/BuildAgentContextPackageCommand-part5.svg)

View: BuildAgentContextPackageCommand-part5

![BuildAgentContextPackageCommand: build a context package (activity), part 6 of 6](c4/rendered/BuildAgentContextPackageCommand-part6.svg)

View: BuildAgentContextPackageCommand-part6

##### Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite)

No transaction is open. RetrieveAgentContextCandidatesQuery is dispatched with the package's `agent`, `sessionId`, `requestId`, and `focusRefs`, and `preferGraphRag` equal to `GraphRag:UseForRetrieval`; the mandatory items (request, profile, rules, applicable requirements) and every focus ref join its results, and the request's own turn is never a candidate. IResponseStalenessRule evaluates each prior-response candidate read-only and its effective status becomes the input's `ResponseStatus`. SourceResolver reads and hashes every candidate (through ContentHasher), and the snapshot's `InputsSha256` is computed over them. The turn status noted here is the state the delegate re-checks.

![Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite) (activity), part 1 of 4](c4/rendered/ReadBuildInputs.svg)

View: ReadBuildInputs

![Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite) (activity), part 2 of 4](c4/rendered/ReadBuildInputs-part2.svg)

View: ReadBuildInputs-part2

![Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite) (activity), part 3 of 4](c4/rendered/ReadBuildInputs-part3.svg)

View: ReadBuildInputs-part3

![Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite) (activity), part 4 of 4](c4/rendered/ReadBuildInputs-part4.svg)

View: ReadBuildInputs-part4

##### Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler)

The delegate reads and writes only the database: it runs the key lookup again, re-checks that the turn still has the status ReadBuildInputs noted (a change ends the attempt and the unit reads again, within SerializableWrite's bound of four attempts), and inserts, with a `pkg-` key from ReadableKeyAllocator, either the `Overflow` package or the `Built` package with its RelationshipWriter links. A key collision ends the attempt and SerializableWrite runs the delegate again; a refusal rolls back and is returned unchanged.

![Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler) (activity), part 1 of 5](c4/rendered/SavePackage.svg)

View: SavePackage

![Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler) (activity), part 2 of 5](c4/rendered/SavePackage-part2.svg)

View: SavePackage-part2

![Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler) (activity), part 3 of 5](c4/rendered/SavePackage-part3.svg)

View: SavePackage-part3

![Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler) (activity), part 4 of 5](c4/rendered/SavePackage-part4.svg)

View: SavePackage-part4

![Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler) (activity), part 5 of 5](c4/rendered/SavePackage-part5.svg)

View: SavePackage-part5

##### Nested activity: FillBudgetWithCandidates (loop of PackageAssembler, for BuildAgentContextPackageCommand and ReplayAgentContextPackageQuery)

Each optional kind keeps at most `MaxCandidatesPerKind` candidates in rank order (`PriorResponse` at most `MaxPriorResponses`); mandatory items are never cut. A candidate whose tokens equal the remaining budget is included (`FillBudgetCandidateEqualsRemainingIncluded`, `BuildCandidatesBeyondPerKindLimitOmittedBelowCandidateLimit`, `BuildMaxPriorResponsesZeroNoPriorResponseItem`).

![Nested activity: FillBudgetWithCandidates (loop of PackageAssembler, for BuildAgentContextPackageCommand and ReplayAgentContextPackageQuery) (activity), part 1 of 3](c4/rendered/FillBudgetWithCandidates.svg)

View: FillBudgetWithCandidates

![Nested activity: FillBudgetWithCandidates (loop of PackageAssembler, for BuildAgentContextPackageCommand and ReplayAgentContextPackageQuery) (activity), part 2 of 3](c4/rendered/FillBudgetWithCandidates-part2.svg)

View: FillBudgetWithCandidates-part2

![Nested activity: FillBudgetWithCandidates (loop of PackageAssembler, for BuildAgentContextPackageCommand and ReplayAgentContextPackageQuery) (activity), part 3 of 3](c4/rendered/FillBudgetWithCandidates-part3.svg)

View: FillBudgetWithCandidates-part3

#### GetAgentContextPackageQuery: get a package [FR-MCP-AGENTCONTEXT-001]

- Handler: `GetAgentContextPackageQuery(string WorkspacePath, string PackageId, bool IncludeContent) : IQuery<AgentContextPackageResult>` in `src/QBrainAi.Services/AgentContext/Packages/Queries/GetAgentContextPackageQuery.cs` and `GetAgentContextPackageQueryHandler` in `src/QBrainAi.Services/AgentContext/Packages/Queries/GetAgentContextPackageQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/packages/{packageId}?includeContent=true`; MCP `agent_context_get_package(workspacePath, packageId, includeContent)`; client `GetPackageAsync(packageId, includeContent)`; REPL `client.AgentContext.GetPackageAsync`; PowerShell `New-McpAgentContextGetPackageParams`; tool descriptor `agent_context_get_package`.
- Not gated. Response 200 `AgentContextPackageResult` (a `Built` or `Overflow` package). Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior); 404 `not_found` (`package`). REPL: a listed read, never refused by ClientMutationPolicy.

![GetAgentContextPackageQuery: get a package (activity), part 1 of 2](c4/rendered/GetAgentContextPackageQuery.svg)

View: GetAgentContextPackageQuery

![GetAgentContextPackageQuery: get a package (activity), part 2 of 2](c4/rendered/GetAgentContextPackageQuery-part2.svg)

View: GetAgentContextPackageQuery-part2

#### QueryAgentContextPackagesQuery: query packages [FR-MCP-AGENTCONTEXT-001]

- Handler: `QueryAgentContextPackagesQuery(string WorkspacePath, AgentContextPackageQuery Query) : IQuery<PageResult<AgentContextPackageResult>>` in `src/QBrainAi.Services/AgentContext/Packages/Queries/QueryAgentContextPackagesQuery.cs` and `QueryAgentContextPackagesQueryHandler` in `src/QBrainAi.Services/AgentContext/Packages/Queries/QueryAgentContextPackagesQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/packages?agent&sessionId&requestId&status&limit&offset`; MCP `agent_context_query_packages(workspacePath, agent, sessionId, requestId, status, limit, offset)`; client `QueryPackagesAsync(AgentContextPackageQuery)`; REPL `client.AgentContext.QueryPackagesAsync`; PowerShell `New-McpAgentContextQueryPackagesParams`; tool descriptor `agent_context_query_packages`.
- Paging: PagedQuery with `PageBounds` `Refuse` (limit 1..1000, default 50, offset at least 0), stable order `CreatedUtc` descending, then `PackageId`. RunStatelessInvocation finds a request's package again with `{ agent, sessionId, requestId, status: Built }`, all three turn-key fields required for that recovery, because a legacy request id is unique only within its session (section 6.6; `StatelessInvocationPackageRecoveryFiltersWholeTurnKeyTwoLegacySessionsSameRequestId`); a filter by `agent` alone or with the other fields is accepted for any other caller (`QueryPackagesFiltersAgentSessionIdAndRequestId`).
- Not gated. Response 200 `PageResult<AgentContextPackageResult>` whose items are package summaries (`items`, `omissions`, `findings`, and `renderedContent` null, section 6.4). Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; an unknown status; `limit` or `offset` outside the bounds, `details.field` naming it). REPL: a listed read, never refused by ClientMutationPolicy.

![QueryAgentContextPackagesQuery: query packages (activity), part 1 of 2](c4/rendered/QueryAgentContextPackagesQuery.svg)

View: QueryAgentContextPackagesQuery

![QueryAgentContextPackagesQuery: query packages (activity), part 2 of 2](c4/rendered/QueryAgentContextPackagesQuery-part2.svg)

View: QueryAgentContextPackagesQuery-part2

#### ReplayAgentContextPackageQuery: replay a package [FR-MCP-AGENTCONTEXT-001]

- Handler: `ReplayAgentContextPackageQuery(string WorkspacePath, string PackageId) : IQuery<AgentContextReplayResult>` in `src/QBrainAi.Services/AgentContext/Packages/Queries/ReplayAgentContextPackageQuery.cs` and `ReplayAgentContextPackageQueryHandler` in `src/QBrainAi.Services/AgentContext/Packages/Queries/ReplayAgentContextPackageQueryHandler.cs`.
- REST `POST /qbrainai/agent-context/packages/{packageId}/replay`; MCP `agent_context_replay_package(workspacePath, packageId)`; client `ReplayPackageAsync(packageId)`; REPL `client.AgentContext.ReplayPackageAsync`; PowerShell `New-McpAgentContextReplayPackageParams`; tool descriptor `agent_context_replay_package`.
- Replay runs PackageAssembler, the same component the build ran, in memory on the package's immutable input snapshot only (the input rows with each prior-response candidate's `ResponseStatus` and each candidate's `ContainsSecret`, the input links that the findings read, `OptionsSnapshotJson`, and the focus refs, section 6.3), so every assembler step (secret filter, omission filter, findings, the mandatory-context check, FillBudgetWithCandidates, rendering) is reproduced; it reads no live source, configuration, relationship, or response status, stores nothing, and compares the result over the stored normal forms (ContentHasher, section 3.5.2), so replay stays identical after configuration, omitted-source, status, and non-Git source changes (`ReplayAfterConfigChangeIdentical`, `ReplayAfterOmittedSourceChangeIdentical`, `ReplayAfterRequirementTodoTurnProfileChangeIdentical`, `ReplaySourceWithEmAndEnDashesIdenticalTrue`).
- Not gated. Response 200 `AgentContextReplayResult`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior); 404 `not_found` (`package`); 409 `agent_context_package_overflow` (BuiltPackageLookup, `SubmitOrReplayOverflowPackage409PackageOverflow`), `agent_context_replay_snapshot_unavailable` (no stored snapshot, `ReplayNoInputSnapshot409ReplaySnapshotUnavailable`), `agent_context_replay_builder_changed` (built by another builder version; `details { recorded, current }`, `ReplayOtherBuilderVersion409ReplayBuilderChangedRecordedAndCurrent`). REPL: a listed read, never refused by ClientMutationPolicy.

![ReplayAgentContextPackageQuery: replay a package (activity), part 1 of 5](c4/rendered/ReplayAgentContextPackageQuery.svg)

View: ReplayAgentContextPackageQuery

![ReplayAgentContextPackageQuery: replay a package (activity), part 2 of 5](c4/rendered/ReplayAgentContextPackageQuery-part2.svg)

View: ReplayAgentContextPackageQuery-part2

![ReplayAgentContextPackageQuery: replay a package (activity), part 3 of 5](c4/rendered/ReplayAgentContextPackageQuery-part3.svg)

View: ReplayAgentContextPackageQuery-part3

![ReplayAgentContextPackageQuery: replay a package (activity), part 4 of 5](c4/rendered/ReplayAgentContextPackageQuery-part4.svg)

View: ReplayAgentContextPackageQuery-part4

![ReplayAgentContextPackageQuery: replay a package (activity), part 5 of 5](c4/rendered/ReplayAgentContextPackageQuery-part5.svg)

View: ReplayAgentContextPackageQuery-part5

#### SubmitAgentResponseCommand: submit a response [FR-MCP-AGENTCONTEXT-003]

- Handler: `SubmitAgentResponseCommand(string WorkspacePath, SubmitAgentResponseRequest Request) : ICommand<AgentResponseResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/SubmitAgentResponseCommand.cs` and `SubmitAgentResponseCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/SubmitAgentResponseCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/responses`; MCP `agent_context_submit_response(workspacePath, requestJson)`; client `SubmitResponseAsync(SubmitAgentResponseRequest)`; REPL `client.AgentContext.SubmitResponseAsync`; PowerShell `New-McpAgentContextSubmitResponseParams`; tool descriptor `agent_context_submit_response`. In `Pilot` and `Stateless` the model calls this tool before it closes the turn, and the plugin supplies the request's `packageId` (section 6.6).
- Response 201 `AgentResponseResult` with status `Submitted`, or 200 with the stored response for a repeated idempotency key with an identical submission (IdempotentCreate, policy `Conflict`, fingerprint `SubmissionSha256` of the whole client submission, section 6.3; `SubmitSameKeyIdenticalSubmission200Existing`, `SubmitSameKeySameContentDifferentPackage409Conflict`, `SubmitSameKeySameContentDifferentClaimsOrEvidence409Conflict`). The unit runs the lookup read-only first, before the package, commit, and evidence reads, and the delegate runs it again as its first step (section 3.5.2).
- Consistency with the package: `agent` equals the package's agent (`SubmitAgentDiffersFromPackage400`); `invocationMode` `Stateless` only on a package whose `OptionsSnapshotJson` mode is `Stateless` (`SubmitStatelessModeOnPilotPackageRejected400`); no `SessionLogTurn` evidence names the package's own turn `<agent>/<sessionId>/<requestId>` (`SubmitEvidenceCitesOwnTurnRejected400`). Each refusal is 400 `validation_error` with `details.field` naming the field or evidence ref.
- Evidence hashes (SourceResolver): each `File` is read at its `commitSha`, by default the HEAD commit read through `ReadCommit`; `Requirement`, `Receipt`, and `SessionLogTurn` are read from their sources; each cited hash is kept as `CitedSha256` and the server's hash is `Sha256`, null when an existing source cannot be read. The response, its claims and evidence, and its links (RelationshipWriter, `CreatedBy` `SubmitAgentResponseCommand`: `derived_from` the package, and `supports` for each `Requirement` or `File` evidence item, with the hashes read before the write) are saved in one transaction, so a failure before the commit leaves none of them (`SubmitLinkWriteFailsNothingCommitted`).
- Reads before the write (section 3.1, Database-only delegates; nested activity ReadSubmissionInputs): the package lookup, the consistency checks, the HEAD commit, and every evidence hash are read with no transaction open, and SecretRedactor redacts the content and its `ContentSha256` (ContentHasher, over the stored normal form, `SubmitContentWithEmDashContentSha256MatchesStoredContent`) is computed in memory. The delegate reads and writes only the database: it runs the key lookup again and inserts the response, its children, and its links. A stored package never changes (inference: no unit of this section updates a package after its insert, and its input snapshot is immutable, section 6.3), so the key is the only state the delegate re-checks.
- Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; empty content, no claim, a claim with no evidence item, duplicate claimId, evidence pointing to an unknown claim, an evidence kind outside `File`, `Requirement`, `Receipt`, `Commit`, `SessionLogTurn` (`SubmitCommandEvidenceKindRejected400`), a `Commit` ref that is not a 40-hex SHA, a `citedSha256` that is not 64 hex digits, a `File`, `Requirement`, `Receipt`, or `SessionLogTurn` ref that names no existing source (`SubmitEvidenceRefNamesNoSourceRejected400`), unknown format or mode, and the consistency refusals above); 404 `not_found` (`package`); 409 `agent_context_disabled`, `agent_context_package_overflow` (BuiltPackageLookup, `SubmitOrReplayOverflowPackage409PackageOverflow`), `agent_context_commit_unavailable` (SourceResolver `ReadCommit`, `SubmitCommitUnreadable409CommitUnavailable`), `conflict` with `retryable: false` (the key reused with a different submission: any differing package, agent, model, effort, mode, format, content, claim, or evidence), `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator exhausted after four attempts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

The well-formedness check covers: content nonempty, at least one claim, every claim with evidence, claimIds unique, evidence kinds, refs, and hashes well formed, format and mode known. The insert takes a `rsp-` key from ReadableKeyAllocator through IdempotentCreate and stores the response `Submitted`, its claims, and its evidence with both hashes, with the root ids copied from the package.

![SubmitAgentResponseCommand: submit a response (activity), part 1 of 6](c4/rendered/SubmitAgentResponseCommand.svg)

View: SubmitAgentResponseCommand

![SubmitAgentResponseCommand: submit a response (activity), part 2 of 6](c4/rendered/SubmitAgentResponseCommand-part2.svg)

View: SubmitAgentResponseCommand-part2

![SubmitAgentResponseCommand: submit a response (activity), part 3 of 6](c4/rendered/SubmitAgentResponseCommand-part3.svg)

View: SubmitAgentResponseCommand-part3

![SubmitAgentResponseCommand: submit a response (activity), part 4 of 6](c4/rendered/SubmitAgentResponseCommand-part4.svg)

View: SubmitAgentResponseCommand-part4

![SubmitAgentResponseCommand: submit a response (activity), part 5 of 6](c4/rendered/SubmitAgentResponseCommand-part5.svg)

View: SubmitAgentResponseCommand-part5

![SubmitAgentResponseCommand: submit a response (activity), part 6 of 6](c4/rendered/SubmitAgentResponseCommand-part6.svg)

View: SubmitAgentResponseCommand-part6

##### Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite)

No transaction is open. The consistency check refuses a submission whose `agent` differs from the package's, an `invocationMode` `Stateless` on a package not built in `Stateless`, and `SessionLogTurn` evidence naming the package's own turn, each with `details.field` naming the field or evidence ref. SourceResolver reads the HEAD commit, hashes each `File` at its `commitSha` (by default that commit) and each `Requirement`, `Receipt`, and `SessionLogTurn` ref from its source.

![Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite) (activity), part 1 of 4](c4/rendered/ReadSubmissionInputs.svg)

View: ReadSubmissionInputs

![Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite) (activity), part 2 of 4](c4/rendered/ReadSubmissionInputs-part2.svg)

View: ReadSubmissionInputs-part2

![Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite) (activity), part 3 of 4](c4/rendered/ReadSubmissionInputs-part3.svg)

View: ReadSubmissionInputs-part3

![Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite) (activity), part 4 of 4](c4/rendered/ReadSubmissionInputs-part4.svg)

View: ReadSubmissionInputs-part4

#### GetAgentResponseQuery: get a response [FR-MCP-AGENTCONTEXT-003]

- Handler: `GetAgentResponseQuery(string WorkspacePath, string ResponseId, bool IncludeContent) : IQuery<AgentResponseResult>` in `src/QBrainAi.Services/AgentContext/Responses/Queries/GetAgentResponseQuery.cs` and `GetAgentResponseQueryHandler` in `src/QBrainAi.Services/AgentContext/Responses/Queries/GetAgentResponseQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/responses/{responseId}?includeContent=true`; MCP `agent_context_get_response(workspacePath, responseId, includeContent)`; client `GetResponseAsync(responseId, includeContent)`; REPL `client.AgentContext.GetResponseAsync`; PowerShell `New-McpAgentContextGetResponseParams`; tool descriptor `agent_context_get_response`.
- Freshness: a read never reports `Trusted` for a response whose dependency changed. The handler evaluates the response with IResponseStalenessRule read-only (a `Rejected`, `Superseded`, or already `Stale` response is not evaluated) and returns the effective status (`Stale`, with `statusReason` naming the changed reference) without writing: a query never writes (`src/QBrainAi.Cqrs/IQuery.cs:5`, "Queries represent intent to read state without side effects"), and the stored status changes only through the commands that persist staleness (`GetResponseAfterFileChangeReportsStale`, `GetResponseAfterFileChangeWritesNothing`).
- Not gated. Response 200 `AgentResponseResult` (with every grade). Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior); 404 `not_found` (`response`). REPL: a listed read, never refused by ClientMutationPolicy.

![GetAgentResponseQuery: get a response (activity), part 1 of 2](c4/rendered/GetAgentResponseQuery.svg)

View: GetAgentResponseQuery

![GetAgentResponseQuery: get a response (activity), part 2 of 2](c4/rendered/GetAgentResponseQuery-part2.svg)

View: GetAgentResponseQuery-part2

#### QueryAgentResponsesQuery: query responses [FR-MCP-AGENTCONTEXT-003, -004]

- Handler: `QueryAgentResponsesQuery(string WorkspacePath, AgentResponseQuery Query) : IQuery<PageResult<AgentResponseResult>>` in `src/QBrainAi.Services/AgentContext/Responses/Queries/QueryAgentResponsesQuery.cs` and `QueryAgentResponsesQueryHandler` in `src/QBrainAi.Services/AgentContext/Responses/Queries/QueryAgentResponsesQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/responses?requestId&packageId&status&trustedOnly&limit&offset`; MCP `agent_context_query_responses(string workspacePath, string? requestId = null, string? packageId = null, string? status = null, bool trustedOnly = false, int limit = 50, int offset = 0)`; client `QueryResponsesAsync(AgentResponseQuery)`; REPL `client.AgentContext.QueryResponsesAsync`; PowerShell `New-McpAgentContextQueryResponsesParams`; tool descriptor `agent_context_query_responses`.
- Freshness: before filtering, counting, and paging, the handler evaluates with IResponseStalenessRule read-only every response matching the other filters whose status is `Submitted`, `Grading`, or `Trusted`, and filters, counts, and pages by the effective status, so `trustedOnly` and a `status` filter never return or count a response whose dependency changed; it writes nothing (`QueryResponsesTrustedOnlyAfterFileChangeExcludesStale`, `QueryResponsesAfterFileChangeWritesNothing`).
- Paging: PagedQuery with `PageBounds` `Refuse` (limit 1..1000, default 50, offset at least 0), stable order `SubmittedUtc` descending, then `ResponseId`.
- Not gated. Response 200 `PageResult<AgentResponseResult>` whose items are response summaries (`claims`, `evidence`, `grades`, and `content` null, section 6.4). Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; an unknown status; `limit` or `offset` outside the bounds, `details.field` naming it). REPL: a listed read, never refused by ClientMutationPolicy.

![QueryAgentResponsesQuery: query responses (activity), part 1 of 3](c4/rendered/QueryAgentResponsesQuery.svg)

View: QueryAgentResponsesQuery

![QueryAgentResponsesQuery: query responses (activity), part 2 of 3](c4/rendered/QueryAgentResponsesQuery-part2.svg)

View: QueryAgentResponsesQuery-part2

![QueryAgentResponsesQuery: query responses (activity), part 3 of 3](c4/rendered/QueryAgentResponsesQuery-part3.svg)

View: QueryAgentResponsesQuery-part3

#### RunDeterministicGradeCommand: run the deterministic grade [FR-MCP-AGENTCONTEXT-004]

- Handler: `RunDeterministicGradeCommand(string WorkspacePath, string ResponseId) : ICommand<AgentResponseGradeResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/RunDeterministicGradeCommand.cs` and `RunDeterministicGradeCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/RunDeterministicGradeCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/responses/{responseId}/grades/deterministic`; MCP `agent_context_grade_deterministic(workspacePath, responseId)`; client `RunDeterministicGradeAsync(responseId)`; REPL `client.AgentContext.RunDeterministicGradeAsync`; PowerShell `New-McpAgentContextGradeDeterministicParams`; tool descriptor `agent_context_grade_deterministic`.
- Checks, one finding per evidence item, by kind: for `File` (at its `commitSha`), `Requirement`, `Receipt`, and `SessionLogTurn`, PASS when `Sha256` is non-null and `CitedSha256` is absent or equal to it, FAIL when `CitedSha256` differs from a non-null `Sha256` (so a wrong cited hash stays FAIL after any restart, `GradeWrongCitedHashFailsAfterRestart`), and UNKNOWN when `Sha256` is null because the existing source could not be read at submission (a ref naming no source never reaches the grade: SubmitAgentResponseCommand refuses it); for `Commit`, PASS when `Ref` names a commit of the workspace repository that equals the response's `CommitSha` or is one of its ancestors, FAIL when the commit does not exist or is not such an ancestor, and UNKNOWN when the workspace is not a git checkout (`GradeCommitEvidenceAncestorPassMissingOrUnrelatedFailNoGitUnknown`). The commit and ancestry are read through SourceResolver; a grade judges evidence and never refuses for an unreadable commit, so where Build and Submit return 409 `agent_context_commit_unavailable`, the grade records UNKNOWN, which fails the verdict. Changes to a source after submission are staleness (IResponseStalenessRule), not grade findings. Global checks, one finding each: every claim has at least one evidence item; the content, every claim text, and every evidence ref contain no secret (`SecretRedactor.ContainsSecret`); the response and its package are fresh: after every read-only check it has (the response found and gradable, the evidence checks above, and the secret scan) and before its SerializableWrite, the unit dispatches EvaluateStalenessCommand for the response through `IDispatcher` (only EvaluateStalenessCommand persists `Stale`, section 3.1, Database-only delegates), and the finding is FAIL when the response is then `Stale`, so the trust decision reads the status that dispatch left.
- Scores: correctness = PASS findings / (PASS + FAIL) over evaluated checks, and 0 when no check could be evaluated (PASS + FAIL = 0); completeness = claims whose every evidence check is PASS / all claims (SubmitAgentResponseCommand guarantees at least one claim, each with evidence); both exact decimals. The verdict is ScoreVerdict's with `max(configured, 0.98)` and zero FAIL and UNKNOWN findings, so any UNKNOWN finding fails the verdict and a zero denominator can never yield trust.
- Each run adds a grade (a deterministic grade has no idempotency key and is re-run on request) under a key from ReadableKeyAllocator, `grd-<responseId>-deterministic-<nn>`, whose suffix widens past 99 (`ReadableKeyAllocatorSuffixPast99WidensLatestGradeOrderKept`); the server stamps `RecordedUtc`, which orders grades (section 6.3).
- Reads before the write and concurrency (section 3.1, Database-only delegates): with no transaction open the unit runs every read-only check it has (the response found and gradable, the evidence checks with their git reads, and the secret scan), then dispatches EvaluateStalenessCommand for the response, and derives the staleness finding, the scores, and the verdict in memory from the status that dispatch left (section 3.1: every read-only check, then the dispatch, then the SerializableWrite). The SerializableWrite that locks the response then re-checks that the response still has the status and `StatusVersion` read after the dispatch (a change ends the attempt and the unit reads again within the bound of four attempts, so a response rejected meanwhile is then refused 409 `agent_context_response_not_gradable`, `GradeResponseRejectedDuringReadsReadsAgain409NotGradable`), saves the grade, and dispatches the trust decision in the same transaction, so a concurrent reject, supersede, or staleness change commits before or after it (`TrustForcedInterleavingDeterministicFailVsModelPassNeverTrustedOnSupersededGrade`, `TrustForcedInterleavingRejectVsGradeRejectedNeverRevived`).
- Response 201 `AgentResponseGradeResult`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior); 404 `not_found` (`response`); 409 `agent_context_disabled`, `agent_context_response_not_gradable` (response `Rejected` or `Superseded`, `GradeRejectedOrSupersededResponse409ResponseNotGradable`), `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator exhausted after four attempts, or the dispatched EvaluateStalenessCommand exhausted); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

The save takes a `grd-` key from ReadableKeyAllocator and stores the grade with its findings and the thresholds in force, with `RecordedUtc` set by the server; ScoreVerdict applies `max(configured, 0.98)` and zero FAIL and UNKNOWN findings.

![RunDeterministicGradeCommand: run the deterministic grade (activity), part 1 of 6](c4/rendered/RunDeterministicGradeCommand.svg)

View: RunDeterministicGradeCommand

![RunDeterministicGradeCommand: run the deterministic grade (activity), part 2 of 6](c4/rendered/RunDeterministicGradeCommand-part2.svg)

View: RunDeterministicGradeCommand-part2

![RunDeterministicGradeCommand: run the deterministic grade (activity), part 3 of 6](c4/rendered/RunDeterministicGradeCommand-part3.svg)

View: RunDeterministicGradeCommand-part3

![RunDeterministicGradeCommand: run the deterministic grade (activity), part 4 of 6](c4/rendered/RunDeterministicGradeCommand-part4.svg)

View: RunDeterministicGradeCommand-part4

![RunDeterministicGradeCommand: run the deterministic grade (activity), part 5 of 6](c4/rendered/RunDeterministicGradeCommand-part5.svg)

View: RunDeterministicGradeCommand-part5

![RunDeterministicGradeCommand: run the deterministic grade (activity), part 6 of 6](c4/rendered/RunDeterministicGradeCommand-part6.svg)

View: RunDeterministicGradeCommand-part6

##### Nested activity: CheckOneEvidenceItem (loop body of RunDeterministicGradeCommand)

![Nested activity: CheckOneEvidenceItem (loop body of RunDeterministicGradeCommand) (activity), part 1 of 3](c4/rendered/CheckOneEvidenceItem.svg)

View: CheckOneEvidenceItem

![Nested activity: CheckOneEvidenceItem (loop body of RunDeterministicGradeCommand) (activity), part 2 of 3](c4/rendered/CheckOneEvidenceItem-part2.svg)

View: CheckOneEvidenceItem-part2

![Nested activity: CheckOneEvidenceItem (loop body of RunDeterministicGradeCommand) (activity), part 3 of 3](c4/rendered/CheckOneEvidenceItem-part3.svg)

View: CheckOneEvidenceItem-part3

#### RecordModelGradeCommand: record a model grade from an HV review [FR-MCP-AGENTCONTEXT-004]

- Handler: `RecordModelGradeCommand(string WorkspacePath, string ResponseId, RecordModelGradeRequest Request) : ICommand<AgentResponseGradeResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/RecordModelGradeCommand.cs` and `RecordModelGradeCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/RecordModelGradeCommandHandler.cs`.
- Caller: the HV agent (the configured grader, launched as every HV is), in two steps of one reviewer thread, because a `codex exec` run cannot hold its own completed-turn receipt while it is still running. Step 1, the review: the HV runner launches the reviewer (`codex exec -m <RequiredModel> -c model_reasoning_effort="<RequiredEffort>" --json -`), which reviews the turns of an HV request (CreateAgentContextHvRequestCommand), emits its `=== VERDICT JSON ===`, and stores its entire result on its own session-log turn; when the run completes the runner freezes the request, response, and rollout jsonl receipts (section 6.3). Step 2, the grade-only continuation: the runner resumes the same thread (`codex exec resume <reviewerThreadId> -m <RequiredModel> -c model_reasoning_effort="<RequiredEffort>" --json -`) with a prompt naming the frozen receipt paths, and the HV agent submits one grade per item through `New-McpAgentContextRecordModelGradeParams`, copied from its own verdict; the continuation reviews nothing and changes no receipt. A continuation that fails or is interrupted is resumed again with the same prompt: a resubmitted grade equal to the stored one returns it (200), so retries are safe (`ModelGradeTwoStepProtocolReviewThenContinuationSubmits`, `ModelGradeRetrySameSubmission200Existing`). There is no grading worker or HV pipeline in this effort (operator, 2026-10-11): the runner is the existing HV runner and both steps run the configured grader.
- REST `POST /qbrainai/agent-context/responses/{responseId}/grades/model`; MCP `agent_context_record_model_grade(string workspacePath, string responseId, string gradeJson)`; client `RecordModelGradeAsync(responseId, RecordModelGradeRequest)`; REPL `client.AgentContext.RecordModelGradeAsync`; PowerShell `New-McpAgentContextRecordModelGradeParams` (also builds the request from the reviewer's verdict: the scores and findings of its `grades` entry for the response copied exactly, the reviewer's own session-log turn ids, its thread id, and the request, response, and rollout jsonl paths); tool descriptor `agent_context_record_model_grade`.
- Response 201 `AgentResponseGradeResult` with `hvRequestStatus` (`Completed` once no item of the HV request is `Pending`), or 200 with the stored grade for a repeated equal submission.
- Order: the score lexemes are validated at deserialization by `ScoreLexemeJsonConverter` (ScoreLexeme, section 3.5.4), before the request pipeline and any lookup, and the REPL's lossless normalizer keeps the lexemes of `correctness` and `completeness` unchanged on the way, so `verdict_match` compares the reviewer's own lexemes. The stored-grade lookup (IdempotentCreate on the natural key `ResponseId`, `HvRequestId`, kind `Model`, policy `Conflict`, fingerprint `PayloadSha256` of the request's client inputs) runs read-only before the status, HV request, and item checks, the receipt reads, and the staleness dispatch, and again as the delegate's first step, so a retry after the request completed or the response was rejected still returns the stored grade. The request's `gradedUtc` is provenance only; the server stamps `RecordedUtc`, which orders grades (`ModelGradeClientGradedUtcInFutureIgnoredServerStampedNewerFailBecomesLatest`).
- Reads before the write (section 3.1, Database-only delegates; nested activity ReadModelGradeInputs): with no transaction open the unit checks the response, the findings' claims, the HV request, and the response's item in it, reads the receipts and the reviewer's turn for the eight checks of section 6.3, then dispatches EvaluateStalenessCommand for the response through `IDispatcher` (section 3.1: every read-only check comes first, the receipt, reviewer, and runtime-identity checks included, and only EvaluateStalenessCommand persists `Stale`, so the trust decision reads the status it left), and derives the verdict through ScoreVerdict. The SerializableWrite that locks the response then runs the key lookup again, re-checks that the response's status and `StatusVersion` (read after the dispatch), the HV request's status, and the item's disposition are those the reads saw (a change ends the attempt and the unit reads again within the bound of four attempts, `ModelGradeHvRequestCompletedDuringReadsReadsAgain409Closed`), inserts the grade, marks the item `Graded` and the HV request `Completed` with `CompletedUtc` when no item stays `Pending`, and dispatches the trust decision in the same transaction.
- Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; malformed findings, or a finding whose claimId the response does not have), `review_score_format`, `review_score_precision`, `review_score_out_of_range`, `agent_context_receipt_paths_required` (`ModelGradeMissingReceiptPathsOrReviewerIds400ReceiptPathsRequired`); 403 `agent_context_grader_not_allowed` (`ModelGradeOtherGrader403GraderNotAllowed`); 404 `not_found` (`response`, `hv_request`); 409 `agent_context_disabled`, `conflict` with `retryable: false` (a different grade already stored for this response and HV request), `agent_context_response_not_gradable` (`GradeRejectedOrSupersededResponse409ResponseNotGradable`), `agent_context_grade_not_in_hv_request` (`ModelGradeResponseNotItemOfHvRequest409GradeNotInHvRequest`), `agent_context_hv_request_closed` (`ModelGradeCompletedHvRequest409HvRequestClosed`), `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator exhausted after four attempts, or the dispatched EvaluateStalenessCommand exhausted); 422 `agent_context_receipt_unverified`; REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

The handler's validation checks that `hvRequestId`, the request, response, and rollout jsonl paths, `reviewerSessionId`, `reviewerRequestId`, and `reviewerThreadId` are present (otherwise `agent_context_receipt_paths_required`), that every finding has a claimId and a check with outcome PASS, FAIL, or UNKNOWN (otherwise `validation_error`), and that the grader agent, model, and effort equal the configured grader. The insert takes a `grd-` key from ReadableKeyAllocator through IdempotentCreate and stores the grade with `hvRequestId`, its findings, the receipt paths and hashes, the thresholds in force, and `RecordedUtc`. The 200 is a retry of the continuation; the 422 names the failed check. One decision holds the three request checks (receipt fields, findings, and the configured grader agent, model, and effort), each refusal on its own edge. The reads are drawn as one call of the nested activity ReadModelGradeInputs and the SerializableWrite with its delegate as one call of the nested activity SaveModelGrade, both below, whose edges are their returns. One 409 `conflict` terminal stands for both conflict answers: `retryable: false` for a different grade already stored for this response and HV request, and `retryable: true` when SerializableWrite or ReadableKeyAllocator is exhausted after four attempts.

![RecordModelGradeCommand: record a model grade from an HV review (activity), part 1 of 5](c4/rendered/RecordModelGradeCommand.svg)

View: RecordModelGradeCommand

![RecordModelGradeCommand: record a model grade from an HV review (activity), part 2 of 5](c4/rendered/RecordModelGradeCommand-part2.svg)

View: RecordModelGradeCommand-part2

![RecordModelGradeCommand: record a model grade from an HV review (activity), part 3 of 5](c4/rendered/RecordModelGradeCommand-part3.svg)

View: RecordModelGradeCommand-part3

![RecordModelGradeCommand: record a model grade from an HV review (activity), part 4 of 5](c4/rendered/RecordModelGradeCommand-part4.svg)

View: RecordModelGradeCommand-part4

![RecordModelGradeCommand: record a model grade from an HV review (activity), part 5 of 5](c4/rendered/RecordModelGradeCommand-part5.svg)

View: RecordModelGradeCommand-part5

##### Nested activity: ReadModelGradeInputs (reads of RecordModelGradeCommand, before its SerializableWrite)

No transaction is open. The gradable check runs in this order: the response is neither `Rejected` nor `Superseded`, the HV request is `Open`, and the response is an item of it. The eight checks are `receipt_path`, `request_meta`, `verdict_block`, `verdict_binding`, `verdict_match`, `reviewer_turn`, `reviewer_independent`, and `runtime_identity` (section 6.3); they read the frozen receipt files and the reviewer's own session-log turn and compare normal forms (ContentHasher, `ModelGradeVerdictWithEmDashReviewerTurnMatchesInNormalForm`). After every read-only check, the receipt, reviewer, and runtime-identity checks included (section 3.1, Database-only delegates), the dispatched EvaluateStalenessCommand persists `Stale` when a dependency changed; a `Stale` response stays gradable and DecideAgentResponseTrustCommand leaves it `Stale`. ScoreVerdict counts the PASS, FAIL, and UNKNOWN findings at `max(configured, 0.98)`. The diagram draws the response, claim, HV request, and item checks as one call of the nested activity CheckGradeTarget below.

![Nested activity: ReadModelGradeInputs (reads of RecordModelGradeCommand, before its SerializableWrite) (activity), part 1 of 3](c4/rendered/ReadModelGradeInputs.svg)

View: ReadModelGradeInputs

![Nested activity: ReadModelGradeInputs (reads of RecordModelGradeCommand, before its SerializableWrite) (activity), part 2 of 3](c4/rendered/ReadModelGradeInputs-part2.svg)

View: ReadModelGradeInputs-part2

![Nested activity: ReadModelGradeInputs (reads of RecordModelGradeCommand, before its SerializableWrite) (activity), part 3 of 3](c4/rendered/ReadModelGradeInputs-part3.svg)

View: ReadModelGradeInputs-part3

##### Nested activity: CheckGradeTarget (response, claim, HV request, and item checks of ReadModelGradeInputs)

No transaction is open. The checks run in this order: the response exists in the workspace, every finding's claimId names a claim of the response, the HV request exists in the workspace, and the response is gradable in it (neither `Rejected` nor `Superseded`, the HV request `Open`, and the response an item of it).

![Nested activity: CheckGradeTarget (response, claim, HV request, and item checks of ReadModelGradeInputs) (activity), part 1 of 3](c4/rendered/CheckGradeTarget.svg)

View: CheckGradeTarget

![Nested activity: CheckGradeTarget (response, claim, HV request, and item checks of ReadModelGradeInputs) (activity), part 2 of 3](c4/rendered/CheckGradeTarget-part2.svg)

View: CheckGradeTarget-part2

![Nested activity: CheckGradeTarget (response, claim, HV request, and item checks of ReadModelGradeInputs) (activity), part 3 of 3](c4/rendered/CheckGradeTarget-part3.svg)

View: CheckGradeTarget-part3

##### Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs)

The SerializableWrite locks the response. Its delegate reads and writes only the database: it runs the key lookup again, re-checks that the response's status and `StatusVersion` (read after the staleness dispatch), the HV request's status, and the item's disposition are those the reads saw (a change ends the attempt and the unit reads again, within the bound of four attempts), inserts the grade with a `grd-` key from ReadableKeyAllocator, marks the item `Graded` and the HV request `Completed` with `CompletedUtc` when no item stays `Pending`, and dispatches DecideAgentResponseTrustCommand in the same transaction.

![Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs) (activity), part 1 of 4](c4/rendered/SaveModelGrade.svg)

View: SaveModelGrade

![Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs) (activity), part 2 of 4](c4/rendered/SaveModelGrade-part2.svg)

View: SaveModelGrade-part2

![Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs) (activity), part 3 of 4](c4/rendered/SaveModelGrade-part3.svg)

View: SaveModelGrade-part3

![Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs) (activity), part 4 of 4](c4/rendered/SaveModelGrade-part4.svg)

View: SaveModelGrade-part4

#### DecideAgentResponseTrustCommand: decide trust (internal, after every grade) [FR-MCP-AGENTCONTEXT-004]

- Handler: `DecideAgentResponseTrustCommand(string WorkspacePath, string ResponseId) : ICommand<AgentResponseTrustDecision>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/DecideAgentResponseTrustCommand.cs` and `DecideAgentResponseTrustCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/DecideAgentResponseTrustCommandHandler.cs`; internal, dispatched through `IDispatcher.SendAsync` by the RunDeterministicGradeCommand and RecordModelGradeCommand handlers inside their SerializableWrite transaction, with the response already locked, never by a surface. It is not gated (it does not implement `IModeGatedRequest`) and runs no transaction of its own, so it has no conflict exit: its caller's SerializableWrite retries the whole delegate.
- Trigger: the two grade handlers dispatch it after their grade is saved, in the same transaction. No endpoint.
- Rule: a response is `Trusted` only when it is fresh and neither `Rejected`, `Stale`, nor `Superseded`, and the verdicts of both its latest deterministic grade and its latest model grade ("latest" by the server-stamped `RecordedUtc`, then `GradeId`, section 6.3) are `Pass`; otherwise it is `Grading`. It never reads a stored verdict: ScoreVerdict re-derives each latest grade's verdict from that grade's stored scores and finding counts against `max(configured now, 0.98)`, so no stored verdict or earlier threshold below the floor can trust a response, and a raised threshold demotes a `Trusted` response at its next decision (`TrustStoredThresholdBelowFloorFloorApplied`, `TrustThresholdRaisedAfterGradeAppliedAtNextDecision`). It reads no source and persists no `Stale` (section 3.1: a SerializableWrite delegate does no git, file, or source read, and only EvaluateStalenessCommand persists `Stale`): the dispatching grade unit ran EvaluateStalenessCommand for the response before its SerializableWrite, and a response that is then `Stale`, `Rejected`, or `Superseded` is not decided and keeps its status, so a dependency that changed after the deterministic grade and before the model grade makes the response `Stale`, never `Trusted` (`TrustDependencyChangedBeforeModelGradeStaleNotTrusted`). The status change and its projection-row side effect go through ResponseStatusTransition in the grade's transaction, so the stored status always reflects the latest committed grade pair, and a concurrent reject, supersede, or staleness change on the same response is ordered before or after it, never interleaved.

![DecideAgentResponseTrustCommand: decide trust (internal, after every grade) (activity), part 1 of 2](c4/rendered/DecideAgentResponseTrustCommand.svg)

View: DecideAgentResponseTrustCommand

![DecideAgentResponseTrustCommand: decide trust (internal, after every grade) (activity), part 2 of 2](c4/rendered/DecideAgentResponseTrustCommand-part2.svg)

View: DecideAgentResponseTrustCommand-part2

#### RejectAgentResponseCommand: reject a response [FR-MCP-AGENTCONTEXT-005]

- Handler: `RejectAgentResponseCommand(string WorkspacePath, string ResponseId, RejectAgentResponseRequest Request) : ICommand<AgentResponseResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/RejectAgentResponseCommand.cs` and `RejectAgentResponseCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/RejectAgentResponseCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/responses/{responseId}/reject`; MCP `agent_context_reject_response(workspacePath, responseId, reason)`; client `RejectResponseAsync(responseId, RejectAgentResponseRequest)`; REPL `client.AgentContext.RejectResponseAsync`; PowerShell `New-McpAgentContextRejectResponseParams`; tool descriptor `agent_context_reject_response`.
- Transition: ResponseStatusTransition to `Rejected` with the reason, inside a SerializableWrite that locks the response, with every side effect in the same transaction (the response's `Pending` items in `Open` HV requests become `Closed` with reason `rejected`, each request left with no `Pending` item becomes `Completed` with `CompletedUtc`, and the projection row is marked). A repeat on a `Rejected` response is the same transition and returns 200 unchanged. A reject and a supersede of the same response commit one after the other: the second is refused with 409 `agent_context_transition_invalid` and the stored status is the first one's (`LifecycleForcedInterleavingSupersedeVsRejectSameOriginalOneWinsOtherRefused`, on the three providers).
- Response 200 `AgentResponseResult` with status `Rejected`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; blank reason); 404 `not_found` (`response`); 409 `agent_context_disabled`, `agent_context_transition_invalid` (the response is `Superseded`), `conflict` with `retryable: true` (SerializableWrite exhausted after four attempts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`. The unit reads only the database, so its whole check runs in the delegate.

![RejectAgentResponseCommand: reject a response (activity), part 1 of 3](c4/rendered/RejectAgentResponseCommand.svg)

View: RejectAgentResponseCommand

![RejectAgentResponseCommand: reject a response (activity), part 2 of 3](c4/rendered/RejectAgentResponseCommand-part2.svg)

View: RejectAgentResponseCommand-part2

![RejectAgentResponseCommand: reject a response (activity), part 3 of 3](c4/rendered/RejectAgentResponseCommand-part3.svg)

View: RejectAgentResponseCommand-part3

#### SupersedeAgentResponseCommand: supersede a response [FR-MCP-AGENTCONTEXT-005]

- Handler: `SupersedeAgentResponseCommand(string WorkspacePath, string ResponseId, SupersedeAgentResponseRequest Request) : ICommand<AgentResponseResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/SupersedeAgentResponseCommand.cs` and `SupersedeAgentResponseCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/SupersedeAgentResponseCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/responses/{responseId}/supersede`; MCP `agent_context_supersede_response(workspacePath, responseId, supersededByResponseId, reason)`; client `SupersedeResponseAsync(responseId, SupersedeAgentResponseRequest)`; REPL `client.AgentContext.SupersedeResponseAsync`; PowerShell `New-McpAgentContextSupersedeResponseParams`; tool descriptor `agent_context_supersede_response`.
- Transition: one SerializableWrite locks both responses in ascending `ResponseId` order, so a concurrent reject of either commits before or after it; inside it, ResponseStatusTransition moves the original to `Superseded` with `SupersededByResponseId` (its `Pending` HV items become `Closed` with reason `superseded`, emptied requests become `Completed` with `CompletedUtc`, and the projection row is marked), and RelationshipWriter writes the `supersedes` link (`CreatedBy` `SupersedeAgentResponseCommand`, so it is protected) with its cycle rule over the `supersedes` links, the one data source of the supersede chain. A refusal by either rolls back both. A repeat naming the same replacement is the same transition and returns 200 unchanged; the first supersession stays on record and no `Rejected` response becomes `Superseded`.
- Response 200 `AgentResponseResult` with status `Superseded` (`SupersedeRepeatSameReplacementReturns200Unchanged`). Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; self-reference, blank reason); 404 `not_found` (`response` for either response); 409 `agent_context_disabled`, `agent_context_supersede_invalid` (a replacement for another turn, compared by the whole turn key `agent`, `sessionId`, and `requestId`, so a replacement with the same `requestId` in another session or of another agent is refused (`SupersedeSameRequestIdOtherSessionOrAgent409SupersedeInvalid`), the replacement `Rejected` or `Superseded`, or a link that would close a supersede cycle, `SupersedeOtherRequestInvalidReplacementOrCycle409SupersedeInvalid`), `agent_context_transition_invalid` (the original `Rejected`, or already superseded by another response: `SupersedeOriginalRejected409TransitionInvalid`, `SupersedeOriginalSupersededByOther409TransitionInvalid`), `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator exhausted after four attempts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`. The unit reads only the database (the `supersedes` link's target is the original response, whose hash is its stored `ContentSha256`, section 6.3, which the delegate reads and passes to RelationshipWriter before the link's write, section 3.1), so its whole check runs in the delegate.

In the diagram a valid replacement answers the same turn as the original (the same `agent`, `sessionId`, and `requestId`, the whole turn key of section 3) and is neither `Rejected` nor `Superseded`, and the transition node applies the side effects of the Transition bullet. One decision checks, in this order, that both responses exist in the workspace and that the replacement is valid for the original, each refusal on its own edge.

![SupersedeAgentResponseCommand: supersede a response (activity), part 1 of 5](c4/rendered/SupersedeAgentResponseCommand.svg)

View: SupersedeAgentResponseCommand

![SupersedeAgentResponseCommand: supersede a response (activity), part 2 of 5](c4/rendered/SupersedeAgentResponseCommand-part2.svg)

View: SupersedeAgentResponseCommand-part2

![SupersedeAgentResponseCommand: supersede a response (activity), part 3 of 5](c4/rendered/SupersedeAgentResponseCommand-part3.svg)

View: SupersedeAgentResponseCommand-part3

![SupersedeAgentResponseCommand: supersede a response (activity), part 4 of 5](c4/rendered/SupersedeAgentResponseCommand-part4.svg)

View: SupersedeAgentResponseCommand-part4

![SupersedeAgentResponseCommand: supersede a response (activity), part 5 of 5](c4/rendered/SupersedeAgentResponseCommand-part5.svg)

View: SupersedeAgentResponseCommand-part5

#### EvaluateStalenessCommand: evaluate staleness [FR-MCP-AGENTCONTEXT-005]

- Handler: `EvaluateStalenessCommand(string WorkspacePath, EvaluateStalenessRequest Request) : ICommand<EvaluateStalenessResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/EvaluateStalenessCommand.cs` and `EvaluateStalenessCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/EvaluateStalenessCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/staleness/evaluate`; MCP `agent_context_evaluate_staleness(workspacePath, responseIdsJson)`; client `EvaluateStalenessAsync(EvaluateStalenessRequest)`; REPL `client.AgentContext.EvaluateStalenessAsync`; PowerShell `New-McpAgentContextEvaluateStalenessParams`; tool descriptor `agent_context_evaluate_staleness`.
- Rule: IResponseStalenessRule (section 3.5.5), owned by ResponsesAndTrust: the `depends_on`, `derived_from`, and `supports` targets of the response and of its package, the response's evidence (including evidence outside its package), and every cited requirement are its dependencies, except the request's own turn and a `Rule` file outside version control; a file dependency is re-hashed at the workspace's current HEAD commit, a dependency whose source no longer exists, such as a deleted requirement, has changed, and a dependency that names a response must itself be effectively `Trusted` and fresh, evaluated in turn (section 3.5.5, `StalenessPriorResponseDependencyRejectedSupersededStaleOrDemotedDependentStale`) (`StalenessResponseOnlyEvidenceChangedMarksStale`, `StalenessUncommittedEditNotStaleCommittedEditStale`, `StalenessOwnTurnCompletedResponseNotStale`, `StalenessServerRestartRegeneratesMarkerTrustedResponseStaysTrusted`). This unit is the one persister of `Stale` (section 3.1, Database-only delegates): per response it evaluates the rule read-only with no transaction open (the file reads at the HEAD commit happen there), and only for a stale response runs one SerializableWrite that locks it, re-checks that its status and `StatusVersion` are those the evaluation saw (a change ends the attempt and the response is evaluated again, within the bound of four attempts), and persists `Stale` through ResponseStatusTransition.
- Callers: the surfaces; the units whose commit moved a response out of `Trusted` or into `Rejected`, `Superseded`, or `Stale` (the grade units, RejectAgentResponseCommand, SupersedeAgentResponseCommand, and this unit), which dispatch it after that commit for the responses that depend on that response (section 3.1, Dependent responses; `ResponseLeavingTrustedDispatchesStalenessForDependentsAfterCommitFailureLoggedUnitSucceeds`); ProjectOneResponse (ContextGraph) and the two grade units (RunDeterministicGradeCommand, RecordModelGradeCommand), which dispatch it through `IDispatcher` for their response before they rely on the status and before their own SerializableWrite (a grade unit after every read-only check it has, section 3.1); and `RequirementsDatabaseDocumentService` (the database `IRequirementsDocumentService`), which after the commit of a write that changes or deletes a requirement or its acceptance criteria dispatches it with the `responseIds` of the responses citing a changed requirement, and after a change of the current requirement layer dispatches it with none, only while Mode is not `Off`, logging a failed dispatch without failing the requirement write (section 3.1, Requirement writes and staleness; section 3.5.1; this updates TR-MCP-REQRECOVERY-001 and the TRs of those requirement writes, which gain this side effect; `RequirementUpdateAfterCommitDispatchesStalenessForCitingResponses`, `RequirementWriteStalenessDispatchFailureLoggedWriteSucceeds`). DecideAgentResponseTrustCommand reads the status the grade unit's dispatch left and evaluates no source. GetAgentContextStatusQuery, BuildAgentContextPackageCommand (for its prior-response candidates), GetAgentResponseQuery, QueryAgentResponsesQuery, RetrieveAgentContextCandidatesQuery, and GetAgentContextPilotReportQuery evaluate IResponseStalenessRule read-only and write nothing (queries are side-effect free, `src/QBrainAi.Cqrs/IQuery.cs:5`), so no context edits another context's files.
- Selection (ResponseIdList): the listed ids, or every `Submitted`, `Grading`, and `Trusted` response when none is listed; an id that names no response in the workspace is returned in `notFound`, is not counted in `evaluatedCount`, and raises no error (`EvaluateStalenessUnknownIdsNotCountedNoError`).
- Response 200 `EvaluateStalenessResult { evaluatedCount, newlyStale, notFound }`. When one response's write exhausts its retries the unit stops and returns 409 `conflict` with `retryable: true`; responses already marked stay marked, and a retry evaluates again. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; malformed list, more than 1000 ids, an id without the `rsp-` prefix); 409 `agent_context_disabled`, `conflict` with `retryable: true` (SerializableWrite exhausted after four attempts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

![EvaluateStalenessCommand: evaluate staleness (activity), part 1 of 2](c4/rendered/EvaluateStalenessCommand.svg)

View: EvaluateStalenessCommand

![EvaluateStalenessCommand: evaluate staleness (activity), part 2 of 2](c4/rendered/EvaluateStalenessCommand-part2.svg)

View: EvaluateStalenessCommand-part2

##### Nested activity: EvaluateOneResponseStaleness (loop body of EvaluateStalenessCommand)

The rule reads files at the workspace HEAD commit with no transaction open; the transition records the changed reference and its relationship and marks the projection row (ResponseStatusTransition, section 3.5.5).

![Nested activity: EvaluateOneResponseStaleness (loop body of EvaluateStalenessCommand) (activity), part 1 of 3](c4/rendered/EvaluateOneResponseStaleness.svg)

View: EvaluateOneResponseStaleness

![Nested activity: EvaluateOneResponseStaleness (loop body of EvaluateStalenessCommand) (activity), part 2 of 3](c4/rendered/EvaluateOneResponseStaleness-part2.svg)

View: EvaluateOneResponseStaleness-part2

![Nested activity: EvaluateOneResponseStaleness (loop body of EvaluateStalenessCommand) (activity), part 3 of 3](c4/rendered/EvaluateOneResponseStaleness-part3.svg)

View: EvaluateOneResponseStaleness-part3

#### CreateAgentContextRelationshipCommand: create a relationship [FR-MCP-AGENTCONTEXT-006]

- Handler: `CreateAgentContextRelationshipCommand(string WorkspacePath, CreateAgentContextRelationshipRequest Request) : ICommand<AgentContextRelationshipResult>` in `src/QBrainAi.Services/AgentContext/Graph/Commands/CreateAgentContextRelationshipCommand.cs` and `CreateAgentContextRelationshipCommandHandler` in `src/QBrainAi.Services/AgentContext/Graph/Commands/CreateAgentContextRelationshipCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/relationships`; MCP `agent_context_create_relationship(workspacePath, relationshipJson)`; client `CreateRelationshipAsync(CreateAgentContextRelationshipRequest)`; REPL `client.AgentContext.CreateRelationshipAsync`; PowerShell `New-McpAgentContextCreateRelationshipParams`; tool descriptor `agent_context_create_relationship`.
- The node kinds are `SourceKind` wire values, mapped by SourceResolver (`Response` is `PriorResponse`, `CodeFile` is the kind of `Code` items and `File` evidence), and both refs are resolved through it, with the target's `ToSha256`, before SerializableWrite (section 3.1, Database-only delegates), and RelationshipWriter receives that hash (section 3.1: it takes the target hash its caller read before the write), so the delegate holds only RelationshipWriter's database work; a ref that names no existing source is 400 `validation_error` with `details.field` `fromRef` or `toRef`, as in every unit. RelationshipWriter writes the link with `CreatedBy` the calling agent, so a client-made link stays deletable; a client cannot create type `supersedes` (400 `validation_error`, `details.field` `type`), because only SupersedeAgentResponseCommand writes the supersede chain. Two concurrent identical creates: the loser's insert hits the link's unique index, SerializableWrite runs the delegate again, and RelationshipWriter returns the winner's link with `created: false` (`CreateRelationshipConcurrentIdenticalOne201OneCreatedFalse`).
- Response 201 with `created: true`, or 200 with `created: false` for an identical stored link (a soft-deleted identical link is revived, keeps its `RelationshipId`, and is written anew with `created: true`, IdempotentCreate, section 3.5.2 and section 6.3, `CreateRelationshipAfterDeleteRevivedSameRelationshipIdCreatedTrue`). Errors: 400 `agent_context_relationship_invalid` (an unknown kind or type, or a self-link, `CreateRelationshipUnknownKindTypeOrSelfLink400RelationshipInvalid`), `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; type `supersedes`; a ref that names no existing source); 409 `agent_context_disabled`, `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator `rel-` exhausted after four attempts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

![CreateAgentContextRelationshipCommand: create a relationship (activity), part 1 of 3](c4/rendered/CreateAgentContextRelationshipCommand.svg)

View: CreateAgentContextRelationshipCommand

![CreateAgentContextRelationshipCommand: create a relationship (activity), part 2 of 3](c4/rendered/CreateAgentContextRelationshipCommand-part2.svg)

View: CreateAgentContextRelationshipCommand-part2

![CreateAgentContextRelationshipCommand: create a relationship (activity), part 3 of 3](c4/rendered/CreateAgentContextRelationshipCommand-part3.svg)

View: CreateAgentContextRelationshipCommand-part3

#### DeleteAgentContextRelationshipCommand: delete a relationship [FR-MCP-AGENTCONTEXT-006]

- Handler: `DeleteAgentContextRelationshipCommand(string WorkspacePath, string RelationshipId) : ICommand<bool>` in `src/QBrainAi.Services/AgentContext/Graph/Commands/DeleteAgentContextRelationshipCommand.cs` and `DeleteAgentContextRelationshipCommandHandler` in `src/QBrainAi.Services/AgentContext/Graph/Commands/DeleteAgentContextRelationshipCommandHandler.cs`.
- REST `DELETE /qbrainai/agent-context/relationships/{relationshipId}`; MCP `agent_context_delete_relationship(workspacePath, relationshipId)`; client `DeleteRelationshipAsync(relationshipId)`; REPL `client.AgentContext.DeleteRelationshipAsync`; PowerShell `New-McpAgentContextDeleteRelationshipParams`; tool descriptor `agent_context_delete_relationship`.
- Protection (RelationshipWriter): every link a command created is protected, because IResponseStalenessRule reads it, it holds the supersede chain, or it records the package's turn: Build's `derived_from` and `relevant_to` (`CreatedBy` `BuildAgentContextPackageCommand`, `DeleteRelationshipBuildMadeDerivedFrom409Protected`), Submit's `derived_from` and `supports` (`DeleteRelationshipServerMadeDerivedFrom409Protected`, `DeleteRelationshipServerMadeSupports409Protected`), and Supersede's `supersedes` (`DeleteRelationshipSupersedeMadeSupersedes409Protected`); a client-made link of any type can be deleted. The protection reads only the immutable `CreatedBy`, so the unit needs no SerializableWrite. The delete is a soft delete (`McpDbContext`, TR-MCP-DB-003), and a later identical create revives the link (section 3.5.2).
- Response 204. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior); 404 `not_found` (`relationship`); 409 `agent_context_disabled`, `agent_context_relationship_protected`; REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

![DeleteAgentContextRelationshipCommand: delete a relationship (activity), part 1 of 2](c4/rendered/DeleteAgentContextRelationshipCommand.svg)

View: DeleteAgentContextRelationshipCommand

![DeleteAgentContextRelationshipCommand: delete a relationship (activity), part 2 of 2](c4/rendered/DeleteAgentContextRelationshipCommand-part2.svg)

View: DeleteAgentContextRelationshipCommand-part2

#### QueryAgentContextRelationshipsQuery: query relationships [FR-MCP-AGENTCONTEXT-006]

- Handler: `QueryAgentContextRelationshipsQuery(string WorkspacePath, AgentContextRelationshipQuery Query) : IQuery<PageResult<AgentContextRelationshipDto>>` in `src/QBrainAi.Services/AgentContext/Graph/Queries/QueryAgentContextRelationshipsQuery.cs` and `QueryAgentContextRelationshipsQueryHandler` in `src/QBrainAi.Services/AgentContext/Graph/Queries/QueryAgentContextRelationshipsQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/relationships?nodeKind&nodeRef&type&direction&limit&offset`; MCP `agent_context_query_relationships(string workspacePath, string? nodeKind = null, string? nodeRef = null, string? type = null, string direction = "both", int limit = 50, int offset = 0)`; client `QueryRelationshipsAsync(AgentContextRelationshipQuery)`; REPL `client.AgentContext.QueryRelationshipsAsync`; PowerShell `New-McpAgentContextQueryRelationshipsParams`; tool descriptor `agent_context_query_relationships`.
- Paging: PagedQuery with `PageBounds` `Refuse` (limit 1..1000, default 50, offset at least 0), stable order `CreatedUtc`, then `RelationshipId`.
- Not gated. Response 200 `PageResult<AgentContextRelationshipDto>`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; a node kind without a ref, an unknown kind, type, or direction; `limit` or `offset` outside the bounds, `details.field` naming it). REPL: a listed read, never refused by ClientMutationPolicy.

![QueryAgentContextRelationshipsQuery: query relationships (activity), part 1 of 4](c4/rendered/QueryAgentContextRelationshipsQuery.svg)

View: QueryAgentContextRelationshipsQuery

![QueryAgentContextRelationshipsQuery: query relationships (activity), part 2 of 4](c4/rendered/QueryAgentContextRelationshipsQuery-part2.svg)

View: QueryAgentContextRelationshipsQuery-part2

![QueryAgentContextRelationshipsQuery: query relationships (activity), part 3 of 4](c4/rendered/QueryAgentContextRelationshipsQuery-part3.svg)

View: QueryAgentContextRelationshipsQuery-part3

![QueryAgentContextRelationshipsQuery: query relationships (activity), part 4 of 4](c4/rendered/QueryAgentContextRelationshipsQuery-part4.svg)

View: QueryAgentContextRelationshipsQuery-part4

#### RetrieveAgentContextCandidatesQuery: retrieve candidate context [FR-MCP-AGENTCONTEXT-007]

- Handler: `RetrieveAgentContextCandidatesQuery(string WorkspacePath, RetrieveAgentContextCandidatesRequest Request) : IQuery<AgentContextRetrieveResult>` in `src/QBrainAi.Services/AgentContext/Packages/Queries/RetrieveAgentContextCandidatesQuery.cs` and `RetrieveAgentContextCandidatesQueryHandler` in `src/QBrainAi.Services/AgentContext/Packages/Queries/RetrieveAgentContextCandidatesQueryHandler.cs` (ContextPackages, with the relational path; the GraphRAG path is ContextGraph's, behind the retrieval seam).
- REST `POST /qbrainai/agent-context/retrieve`; MCP `agent_context_retrieve(workspacePath, requestJson)`; client `RetrieveAsync(RetrieveAgentContextCandidatesRequest)`; REPL `client.AgentContext.RetrieveAsync`; PowerShell `New-McpAgentContextRetrieveParams`; tool descriptor `agent_context_retrieve`. Also dispatched by BuildAgentContextPackageCommand with the package's `agent`, `sessionId`, `requestId`, and `focusRefs`, and `preferGraphRag` equal to `GraphRag:UseForRetrieval`.
- A named turn (`agent`, `sessionId`, and `requestId`, all or none) is found through TurnLocator; the named turn itself is never a candidate (section 3.1). When the request names a turn or `focusRefs`, the relational path ranks by explicit relationships to that turn, its TODO, its requirements, and the focus refs first (`RetrieveSameQueryTextTwoTurnNeighborhoodsDifferentRanking`); without them it ranks by the existing hybrid search (FTS5 BM25 with vector similarity) alone. Every `PriorResponse` candidate is evaluated with IResponseStalenessRule read-only before it is returned, so a response whose dependency changed is omitted and nothing is written (`RetrieveAfterEvidenceFileChangeResponseExcludedNothingWritten`). Both paths end in CandidateRanker (scores rounded half-even to six places, descending, then `sourceRef` by ordinal comparison) and the cut to `limit`.
- Path choice: `preferGraphRag` false takes the relational path with no `fallbackReason`; `preferGraphRag` true with `GraphRag:UseForRetrieval` false takes it with `fallbackReason` `retrieval_disabled` (`RetrievePreferGraphRagUseForRetrievalFalseRelationalWithRetrievalDisabled`). The GraphRAG path answers only the `PriorResponse` kind, because GraphRAG holds only projected responses: a request whose `kinds` is absent or names any other kind takes the relational path with `fallbackReason` `graphrag_kinds_unsupported`; otherwise it queries the projected responses in `GraphRagStorageScope.Workspace` only, drops a hit that names no response of this workspace, keeps one hit per response, and keeps the hits that are `Trusted` and fresh, scored by the GraphRAG score (`RetrieveGraphRagOtherKindsRequestedRelationalWithReason`, `RetrieveGraphRagOrderedByScoreThenSourceRefCutToLimit`, `ProjectionAndGraphRagRetrievalWorkspaceScopedNoCrossWorkspaceHit`, `RetrieveGraphRagDuplicateHitsForOneResponseReturnedOnce`). Any fallback uses the relational path and reports `fallbackReason` (`graphrag_disabled`, `graphrag_not_initialized`, `graphrag_not_indexed`, `index_in_progress`, `retrieval_disabled`, `graphrag_error`, or `graphrag_kinds_unsupported`).
- Not gated. Response 200 `AgentContextRetrieveResult`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; `trustedOnly: false` with kind `PriorResponse`; `limit` outside `PageBounds` `Refuse`, 1..1000, default 50, `details.field` `limit`; only some of `agent`, `sessionId`, and `requestId`; TurnLocator `InvalidIdentifier`); 404 `not_found` (`session_turn` for TurnLocator `SessionNotFound` or `TurnNotFound`, `RetrieveUnknownTurn404SessionTurn`). REPL: a listed read, never refused by ClientMutationPolicy. A query opens no transaction, so when Build dispatches it, its GraphRAG and source reads run before Build's SerializableWrite.

![RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 1 of 10](c4/rendered/RetrieveAgentContextCandidatesQuery.svg)

View: RetrieveAgentContextCandidatesQuery

![RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 2 of 10](c4/rendered/RetrieveAgentContextCandidatesQuery-part2.svg)

View: RetrieveAgentContextCandidatesQuery-part2

![RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 3 of 10](c4/rendered/RetrieveAgentContextCandidatesQuery-part3.svg)

View: RetrieveAgentContextCandidatesQuery-part3

![RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 4 of 10](c4/rendered/RetrieveAgentContextCandidatesQuery-part4.svg)

View: RetrieveAgentContextCandidatesQuery-part4

![RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 5 of 10](c4/rendered/RetrieveAgentContextCandidatesQuery-part5.svg)

View: RetrieveAgentContextCandidatesQuery-part5

![RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 6 of 10](c4/rendered/RetrieveAgentContextCandidatesQuery-part6.svg)

View: RetrieveAgentContextCandidatesQuery-part6

![RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 7 of 10](c4/rendered/RetrieveAgentContextCandidatesQuery-part7.svg)

View: RetrieveAgentContextCandidatesQuery-part7

![RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 8 of 10](c4/rendered/RetrieveAgentContextCandidatesQuery-part8.svg)

View: RetrieveAgentContextCandidatesQuery-part8

![RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 9 of 10](c4/rendered/RetrieveAgentContextCandidatesQuery-part9.svg)

View: RetrieveAgentContextCandidatesQuery-part9

![RetrieveAgentContextCandidatesQuery: retrieve candidate context (activity), part 10 of 10](c4/rendered/RetrieveAgentContextCandidatesQuery-part10.svg)

View: RetrieveAgentContextCandidatesQuery-part10

#### ProjectAgentResponsesCommand: project responses into GraphRAG [FR-MCP-AGENTCONTEXT-007]

- Handler: `ProjectAgentResponsesCommand(string WorkspacePath, ProjectAgentContextRequest Request) : ICommand<ProjectAgentResponsesResult>` in `src/QBrainAi.Services/AgentContext/Graph/Commands/ProjectAgentResponsesCommand.cs` and `ProjectAgentResponsesCommandHandler` in `src/QBrainAi.Services/AgentContext/Graph/Commands/ProjectAgentResponsesCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/projection/graphrag`; MCP `agent_context_project_graphrag(workspacePath, responseIdsJson)`; client `ProjectToGraphRagAsync(ProjectAgentContextRequest)`; REPL `client.AgentContext.ProjectToGraphRagAsync`; PowerShell `New-McpAgentContextProjectGraphRagParams`; tool descriptor `agent_context_project_graphrag`. Also dispatched by the background pass after ResponseStatusTransition marks a projection row (entering or leaving `Trusted`): the unit whose transaction marked the row (the grade units through the trust decision, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand) dispatches this command with no `responseIds` through `IDispatcher` in its own scope after its SerializableWrite commits; a failed dispatch is logged and never fails that unit, and a row it leaves is selected by the next pass or an explicit call (section 3.1, Projection pass; `ProjectionPassDispatchedAfterTrustCommitFailureLoggedUnitSucceeds`). This command is gated, so the pass does not run while Mode is `Off`, and a document left in GraphRAG meanwhile never becomes trusted context, because the GraphRAG retrieval path keeps only `Trusted`, fresh hits.
- Selection (ResponseIdList, section 6.3): the rows of the listed ids in any state, or, with none listed, every `Pending` and `Failed` row and every `Skipped` row once ingest is enabled again; an id that names no response in the workspace is returned in `notFound` (`ProjectionUnknownResponseIdListedInNotFound`).
- Projection lifecycle (ProjectOneResponse; the row states are drawn in section 6.3): the response's staleness is persisted first by dispatching EvaluateStalenessCommand through `IDispatcher`, so a response whose dependency changed is removed, never projected (`ProjectionTrustedWithChangedDependencyRemovedNotProjected`). A `Trusted` response is ingested into `GraphRagStorageScope.Workspace` only, through `IGraphRagService.IngestTextAsync` with `content` the response content, `title` the `responseId`, `sourceType` `agent-response`, and `sourceKey` `agent-context/<responseId>` (`src/QBrainAi.GraphRag/Models/GraphRagModels.cs:184`), when its projected hash differs (this admits agent responses to GraphRAG, so it updates FR-MCP-130 AC 3, which admits only CuriosityEngine requests, to scope that rule to brain-slot output); with `GraphRag:ProjectionEnabled` false or GraphRAG disabled its row is `Skipped` with `skippedReason` `projection_disabled` or `graphrag_disabled` (`ProjectionProjectionDisabledSkippedReasonProjectionDisabled`). For a response that is not `Trusted`, a row that holds a `GraphRagDocumentId` (`Projected`, or `Failed` after a delete error) is deleted on this pass while GraphRAG is enabled, whatever `ProjectionEnabled` says (`ProjectionDisabledAfterProjectedRejectedResponseDocumentStillRemoved`, `ProjectionDeleteFailedRetriedNextPassDocumentRemoved`); while GraphRAG is disabled that row stays `Pending`, never `Skipped`, and is removed once GraphRAG is enabled (`ProjectionGraphRagDisabledRemovalStaysPendingRunsWhenEnabled`); a selected row with no document becomes `Removed` and is not selected again (`ProjectionPendingThenRejectedBeforeIngestRowRemovedNotReselected`). The row state is saved through SerializableWrite with the response locked, and the response status is re-read there, after the GraphRAG call, so the delegate reads and writes only the database (section 3.1, Database-only delegates): when the response left `Trusted` during an ingest, the row keeps the new document id and stays `Pending` for removal (`ProjectionForcedInterleavingRejectDuringIngestDocumentRemovedNextPass`, on the three providers); a GraphRAG error is `Failed` with `LastError`, keeping any document id; a delete that `DeleteDocumentAsync` answers with `Success: false` and no exception (the document is already gone, `GraphRagService.cs:490-492`, for example removed by ReconcileProjectedDocuments) is a completed removal, so the row becomes `Removed` and is never `Failed` for it (`ProjectionDeleteDocumentAlreadyGoneRowRemoved`). Passes can overlap (a surface call beside the background pass), and `IngestTextAsync` creates a new document on every call (`src/QBrainAi.GraphRag/Services/GraphRagService.cs:354` at `a8d3934b`), so the delegate also re-reads the row: a save that finds the row already `Projected` with another pass's document for the same `ProjectedContentSha256` keeps the stored one and deletes the document it ingested after the save, so a response has at most one projected document (`ProjectionOverlappingPassesOneDocumentKeptOtherDeleted`).
- Reconciliation (nested activity ReconcileProjectedDocuments, after the loop, on every pass while GraphRAG is enabled): the GraphRAG store itself holds every cleanup obligation, because each projected document carries `sourceType` `agent-response` and `sourceKey` `agent-context/<responseId>`. A document of that source type in the workspace scope is current only while the response its source key names has a `Projected` row naming that document id; the pass lists the documents of that source type (`IGraphRagService.ListDocumentsAsync` with `sourceType`, page by page, `src/QBrainAi.GraphRag/Services/IGraphRagService.cs:40` at `a8d3934b`) and deletes every other one ingested before the pass started (`DeleteDocumentAsync`, which answers `Success: false` for a document already gone, `GraphRagService.cs:488-492`), so a document that two racing passes ingested, a document whose save exhausted its retries after the ingest, and a document whose delete failed are removed by the next complete pass (`ProjectionReconcileTwoIngestsWithRejectEveryDocumentRemoved`, `ProjectionReconcileSaveExhaustedAfterIngestOrphanRemovedNextPass`, `ProjectionReconcileDuplicateDeleteFailedRetriedNextPass`). A document ingested after the pass started is left to the pass that ingested it or a later one. A `Projected` row whose document the listing lacks and whose `UpdatedUtc` is before the pass started returns to `Pending` through SerializableWrite with the response locked, so the next pass ingests it again (`ProjectionReconcileProjectedRowDocumentMissingBackToPending`). A delete error is logged and the document is met again on the next pass; a save that exhausts its retries ends the unit with 409 `conflict` with `retryable: true`.
- Response 200 `ProjectAgentResponsesResult { projected, removed, skipped, failed, skippedReason, notFound }`; GraphRAG being disabled is not an error. When a save exhausts its retries the unit stops and returns 409 `conflict` with `retryable: true`; rows already saved stay saved. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; malformed list, more than 1000 ids, an id without the `rsp-` prefix); 409 `agent_context_disabled`, `conflict` with `retryable: true` (SerializableWrite exhausted after four attempts, in a save or in the dispatched EvaluateStalenessCommand); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

![ProjectAgentResponsesCommand: project responses into GraphRAG (activity), part 1 of 2](c4/rendered/ProjectAgentResponsesCommand.svg)

View: ProjectAgentResponsesCommand

![ProjectAgentResponsesCommand: project responses into GraphRAG (activity), part 2 of 2](c4/rendered/ProjectAgentResponsesCommand-part2.svg)

View: ProjectAgentResponsesCommand-part2

##### Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand)

![Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand) (activity), part 1 of 5](c4/rendered/ProjectOneResponse.svg)

View: ProjectOneResponse

![Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand) (activity), part 2 of 5](c4/rendered/ProjectOneResponse-part2.svg)

View: ProjectOneResponse-part2

![Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand) (activity), part 3 of 5](c4/rendered/ProjectOneResponse-part3.svg)

View: ProjectOneResponse-part3

![Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand) (activity), part 4 of 5](c4/rendered/ProjectOneResponse-part4.svg)

View: ProjectOneResponse-part4

![Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand) (activity), part 5 of 5](c4/rendered/ProjectOneResponse-part5.svg)

View: ProjectOneResponse-part5

##### Nested activity: ReconcileProjectedDocuments (after the loop of ProjectAgentResponsesCommand)

![Nested activity: ReconcileProjectedDocuments (after the loop of ProjectAgentResponsesCommand) (activity), part 1 of 3](c4/rendered/ReconcileProjectedDocuments.svg)

View: ReconcileProjectedDocuments

![Nested activity: ReconcileProjectedDocuments (after the loop of ProjectAgentResponsesCommand) (activity), part 2 of 3](c4/rendered/ReconcileProjectedDocuments-part2.svg)

View: ReconcileProjectedDocuments-part2

![Nested activity: ReconcileProjectedDocuments (after the loop of ProjectAgentResponsesCommand) (activity), part 3 of 3](c4/rendered/ReconcileProjectedDocuments-part3.svg)

View: ReconcileProjectedDocuments-part3

#### GetAgentContextPilotReportQuery: pilot report [FR-MCP-AGENTCONTEXT-009]

- Handler: `GetAgentContextPilotReportQuery(string WorkspacePath, AgentContextPilotReportQuery Query) : IQuery<AgentContextPilotReport>` in `src/QBrainAi.Services/AgentContext/Responses/Queries/GetAgentContextPilotReportQuery.cs` and `GetAgentContextPilotReportQueryHandler` in `src/QBrainAi.Services/AgentContext/Responses/Queries/GetAgentContextPilotReportQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/pilot/report?fromUtc&toUtc`; MCP `agent_context_pilot_report(workspacePath, fromUtc, toUtc)`; client `GetPilotReportAsync(AgentContextPilotReportQuery)`; REPL `client.AgentContext.GetPilotReportAsync`; PowerShell `New-McpAgentContextPilotReportParams`; tool descriptor `agent_context_pilot_report`.
- Comparison (section 3.1, the pilot comparison): responses submitted in the window are grouped by `invocationMode` (`Pilot` yields `Stateful` responses, a period in `Stateless` on hosts with history exclusion yields `Stateless` ones). Per mode: counts by effective status, through IResponseStalenessRule read-only, so a stored `Trusted` response whose dependency changed counts as `Stale` and nothing is written (`PilotReportAfterDependencyChangeCountsStaleNotTrustedWritesNothing`); mean latest model correctness and completeness over the responses that have a model grade ("latest" by `RecordedUtc`, then `GradeId`); and `trustRate`, the `Trusted` responses over all responses of the mode (`PilotReportResponsesWithoutModelGradeExcludedFromMeansInTrustRateDenominator`). For the window it also counts the packages that failed (`overflowPackages`, the `Overflow` packages), the requests with a package and no response (`packagesWithoutResponse`, the `Built` packages no response answers), and the refused prompts (`refusedPrompts`, the turns closed `canceled`, or with the legacy spelling `cancelled`, whose `failureNote` starts with the token `agent_context_invocation_failed` that RunStatelessInvocation writes, section 6.6, so a refusal for an outage or a commit, profile, or budget failure is counted alike) (`PilotReportCountsOverflowRefusedAndPackageWithoutResponse`).
- Not gated, so the report stays readable after a rollback to `Off`. Response 200 `AgentContextPilotReport`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; a missing or invalid `fromUtc` or `toUtc`, or `fromUtc` not before `toUtc`). REPL: a listed read, never refused by ClientMutationPolicy.

![GetAgentContextPilotReportQuery: pilot report (activity), part 1 of 2](c4/rendered/GetAgentContextPilotReportQuery.svg)

View: GetAgentContextPilotReportQuery

![GetAgentContextPilotReportQuery: pilot report (activity), part 2 of 2](c4/rendered/GetAgentContextPilotReportQuery-part2.svg)

View: GetAgentContextPilotReportQuery-part2

#### CreateAgentContextHvRequestCommand: create an HV request for a range of turns [FR-MCP-AGENTCONTEXT-004]

- Handler: `CreateAgentContextHvRequestCommand(string WorkspacePath, CreateAgentContextHvRequestRequest Request) : ICommand<AgentContextHvRequestResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/CreateAgentContextHvRequestCommand.cs` and `CreateAgentContextHvRequestCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/CreateAgentContextHvRequestCommandHandler.cs`.
- Caller: any agent (operator, 2026-10-11: "Agents will specify a range of session turns to be HV, the HV agent will mark the grade you have been working.").
- REST `POST /qbrainai/agent-context/hv-requests`; MCP `agent_context_create_hv_request(string workspacePath, string sessionId, string agent, string fromRequestId, string toRequestId, string requestedBy, string reason, string idempotencyKey)`; client `CreateHvRequestAsync(CreateAgentContextHvRequestRequest)`; REPL `client.AgentContext.CreateHvRequestAsync`; PowerShell `New-McpAgentContextCreateHvRequestParams`; tool descriptor `agent_context_create_hv_request`.
- Response 201 `AgentContextHvRequestResult` (Location `/qbrainai/agent-context/hv-requests/{hvRequestId}`) with its items: every agent response whose `RequestId` lies in the range and whose status is not `Rejected` or `Superseded` and that is not already `Pending` in another `Open` HV request, in turn order, each `Pending` (`HvRequestResponsePendingInOpenRequestNotListedAgain`). A repeat with the same `idempotencyKey` and the same `sessionId`, `agent`, range, `requestedBy`, and `reason` returns 200 with the stored request and its current items and status, so a retry after a lost 201 never creates a second request (`HvRequestRetrySameKeySameInputsReturns200Stored`); the same key with any other input returns 409 `conflict` with `retryable: false` (`HvRequestSameKeyOtherInputs409Conflict`). The key lookup (IdempotentCreate, policy `Conflict`, fingerprint of the client inputs) is the first step inside the transaction, before the session, range, and status checks; the unit reads only the database, so it needs no read-only lookup before its SerializableWrite (section 3.5.2). A concurrent create that wins the key's unique index ends the loser's attempt, SerializableWrite runs the delegate again, and the lookup returns the winner, the same rule Build and Submit follow.
- Concurrency: creation runs through SerializableWrite (section 3.5.2), whose first statements take the `StatusVersion` lock of every response of the range in ascending `ResponseId` order; the delegate then re-reads their status and inserts the items. A concurrent RejectAgentResponseCommand or SupersedeAgentResponseCommand on a candidate therefore commits either before (the response is excluded) or after (its ResponseStatusTransition finds the new `Pending` item and closes it), so no `Pending` item ever names a terminal response (`HvRequestForcedCreateVsRejectNoPendingItemOnTerminalResponse`, `HvRequestForcedCreateVsSupersedeNoPendingItemOnTerminalResponse`), and two concurrent creations over overlapping ranges lock their shared responses in the same order, so the second sees the first's `Pending` items and each response is `Pending` in at most one `Open` request (`HvRequestForcedConcurrentOverlappingCreatesEachResponsePendingInAtMostOneOpenRequest`), each case on the three providers. When every candidate turned terminal the creation fails with `agent_context_hv_request_invalid` and stores nothing; when SerializableWrite exhausts its four attempts the unit returns 409 `conflict` with `retryable: true`.
- Turns (TurnLocator): the session by `agent` and `sessionId`, then each endpoint turn by its `requestId`; `SessionNotFound` is the unknown-session refusal and `TurnNotFound` the range refusal below.
- Review contract: the brief for an HV request asks the reviewer to emit in its `=== VERDICT JSON ===` the `hvRequestId` and `grades`, its `overallVerdict`, and one entry per item `{ responseId, contentSha256, verdict, correctness, completeness, findings [{ claimId, check, outcome }] }` naming every claim of the response, each `verdict` consistent with its scores and findings and `overallVerdict` `AGREE` only when every entry is (section 6.3, `verdict_match`), and to store its entire result on its own session-log turn; the runner then freezes the receipts and runs the grade-only continuation of the same thread (RecordModelGradeCommand, two-step protocol), and RecordModelGradeCommand accepts a grade only when it equals that entry and that turn (section 6.3).
- Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; blank fields, or TurnLocator `InvalidIdentifier`), `agent_context_hv_request_invalid` (an unknown session, an endpoint turn not in the session, from after to, a range longer than `HvRequest:MaxTurns`, or a range with no gradable agent response, `HvRequestUnknownSessionBadRangeOverMaxTurnsOrNoResponse400HvRequestInvalid`); 409 `agent_context_disabled`, `conflict` with `retryable: false` (the idempotency key reused with other inputs), `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator `hvr-` exhausted after four attempts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

The selection re-reads the locked responses and keeps, in turn order and each `Pending`, those neither `Rejected` nor `Superseded` and not `Pending` in another `Open` HV request. The insert takes a `hvr-` key from ReadableKeyAllocator through IdempotentCreate and stores the HV request `Open` with its items. The diagram draws the turn, range, and selection checks as one call of the nested activity SelectHvRequestItems below, inside the delegate.

![CreateAgentContextHvRequestCommand: create an HV request for a range of turns (activity), part 1 of 4](c4/rendered/CreateAgentContextHvRequestCommand.svg)

View: CreateAgentContextHvRequestCommand

![CreateAgentContextHvRequestCommand: create an HV request for a range of turns (activity), part 2 of 4](c4/rendered/CreateAgentContextHvRequestCommand-part2.svg)

View: CreateAgentContextHvRequestCommand-part2

![CreateAgentContextHvRequestCommand: create an HV request for a range of turns (activity), part 3 of 4](c4/rendered/CreateAgentContextHvRequestCommand-part3.svg)

View: CreateAgentContextHvRequestCommand-part3

![CreateAgentContextHvRequestCommand: create an HV request for a range of turns (activity), part 4 of 4](c4/rendered/CreateAgentContextHvRequestCommand-part4.svg)

View: CreateAgentContextHvRequestCommand-part4

##### Nested activity: SelectHvRequestItems (turn, range, and item selection inside the SerializableWrite delegate of CreateAgentContextHvRequestCommand)

It runs inside the delegate, after the key lookup, and reads only the database; a refusal rolls the transaction back and is returned unchanged. TurnLocator finds the session by `agent` and `sessionId`, then each endpoint turn by its `requestId`. One `agent_context_hv_request_invalid` terminal stands for the unknown session (`SessionNotFound`), the range refusals (an endpoint turn not in the session, from after to, or a range longer than `HvRequest:MaxTurns`), and a range with no gradable agent response; each edge names its case.

![Nested activity: SelectHvRequestItems (turn, range, and item selection inside the SerializableWrite delegate of CreateAgentContextHvRequestCommand) (activity), part 1 of 3](c4/rendered/SelectHvRequestItems.svg)

View: SelectHvRequestItems

![Nested activity: SelectHvRequestItems (turn, range, and item selection inside the SerializableWrite delegate of CreateAgentContextHvRequestCommand) (activity), part 2 of 3](c4/rendered/SelectHvRequestItems-part2.svg)

View: SelectHvRequestItems-part2

![Nested activity: SelectHvRequestItems (turn, range, and item selection inside the SerializableWrite delegate of CreateAgentContextHvRequestCommand) (activity), part 3 of 3](c4/rendered/SelectHvRequestItems-part3.svg)

View: SelectHvRequestItems-part3

#### GetAgentContextHvRequestQuery: get an HV request [FR-MCP-AGENTCONTEXT-004]

- Handler: `GetAgentContextHvRequestQuery(string WorkspacePath, string HvRequestId) : IQuery<AgentContextHvRequestResult>` in `src/QBrainAi.Services/AgentContext/Responses/Queries/GetAgentContextHvRequestQuery.cs` and `GetAgentContextHvRequestQueryHandler` in `src/QBrainAi.Services/AgentContext/Responses/Queries/GetAgentContextHvRequestQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/hv-requests/{hvRequestId}`; MCP `agent_context_get_hv_request(string workspacePath, string hvRequestId)`; client `GetHvRequestAsync(hvRequestId)`; REPL `client.AgentContext.GetHvRequestAsync`; PowerShell `New-McpAgentContextGetHvRequestParams`; tool descriptor `agent_context_get_hv_request`.
- Not gated. Response 200 `AgentContextHvRequestResult` with its items. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior); 404 `not_found` (`hv_request`). REPL: a listed read, never refused by ClientMutationPolicy.

![GetAgentContextHvRequestQuery: get an HV request (activity), part 1 of 2](c4/rendered/GetAgentContextHvRequestQuery.svg)

View: GetAgentContextHvRequestQuery

![GetAgentContextHvRequestQuery: get an HV request (activity), part 2 of 2](c4/rendered/GetAgentContextHvRequestQuery-part2.svg)

View: GetAgentContextHvRequestQuery-part2

#### QueryAgentContextHvRequestsQuery: query HV requests [FR-MCP-AGENTCONTEXT-004]

- Handler: `QueryAgentContextHvRequestsQuery(string WorkspacePath, AgentContextHvRequestQuery Query) : IQuery<PageResult<AgentContextHvRequestResult>>` in `src/QBrainAi.Services/AgentContext/Responses/Queries/QueryAgentContextHvRequestsQuery.cs` and `QueryAgentContextHvRequestsQueryHandler` in `src/QBrainAi.Services/AgentContext/Responses/Queries/QueryAgentContextHvRequestsQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/hv-requests?sessionId&status&limit&offset`; MCP `agent_context_query_hv_requests(string workspacePath, string? sessionId = null, string? status = null, int limit = 50, int offset = 0)`; client `QueryHvRequestsAsync(AgentContextHvRequestQuery)`; REPL `client.AgentContext.QueryHvRequestsAsync`; PowerShell `New-McpAgentContextQueryHvRequestsParams`; tool descriptor `agent_context_query_hv_requests`.
- Paging: PagedQuery with `PageBounds` `Refuse` (limit 1..1000, default 50, offset at least 0), stable order `CreatedUtc` descending, then `HvRequestId`.
- Not gated. Response 200 `PageResult<AgentContextHvRequestResult>` whose items are HV request summaries (`items` null, section 6.4). Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; an unknown status; `limit` or `offset` outside the bounds, `details.field` naming it). REPL: a listed read, never refused by ClientMutationPolicy.

![QueryAgentContextHvRequestsQuery: query HV requests (activity), part 1 of 2](c4/rendered/QueryAgentContextHvRequestsQuery.svg)

View: QueryAgentContextHvRequestsQuery

![QueryAgentContextHvRequestsQuery: query HV requests (activity), part 2 of 2](c4/rendered/QueryAgentContextHvRequestsQuery-part2.svg)

View: QueryAgentContextHvRequestsQuery-part2

### 6.6 Processes (plugin)

#### Process RunStatelessInvocation: run a stateless invocation (plugin process) [FR-MCP-AGENTCONTEXT-009]

- Not a unit of work: the plugin orchestrates GetAgentContextStatusQuery (the Mode), BuildAgentContextPackageCommand (the package), and QueryAgentContextPackagesQuery (finding the package again) through the REPL; the model submits its response through SubmitAgentResponseCommand, and the plugin then requests RunDeterministicGradeCommand.
- PowerShell core: `Invoke-PluginAgentContextInvocation` in `plugins/core/lib-ps/repl-invoke.ps1`, the dot-sourceable library of section 3.5.6 (section 3.1, Plugin function home), for all nine hosts (the TypeScript hosts through their adapters); the prompt hook in `plugin-hook.ps1` dot-sources that library and calls it, and process ApplyArrivalBridgeRecord calls it in-process for a delivery `absorbed_mid_turn` (the arrival bridge is its own script entry that dot-sources the same library, section 3.5.6). Hosts inject the rendered package through their prompt-context mechanism (hook additional context or the host's system-context API).
- Output: in `Stateless` mode the model receives only the rendered package, the request, and the `REQUIRED MEMORIES` block, without prior conversation turns; in `Pilot` mode the package is built and recorded but not injected; in `Off` nothing happens.
- Trigger (operator, 2026-10-12: "Hook syncs, absorbed=Pilot (Recommended)"): the prompt hook of a user request on every host. On hosts without a bridge the hook's own mint result decides (process OpenTurnFromPromptHook, classified by `Resolve-MintOutcome`, section 3.5.6). On claude-code and claude-cowork, where the arrival bridge is the only minter, the prompt hook applies pending transcript records through `Sync-PluginArrivalLog` up to the transcript length at its own start, takes the bridge-minted `requestId` of the delivered request, and then runs the invocation; when that request's `enqueue` record is not yet in the transcript, the invocation fails: the prompt is refused, and, with no `requestId`, no turn is closed (`StatelessInvocationBridgeHostHookSyncsAndTakesBridgeMintedRequestId`, `StatelessInvocationBridgeHostEnqueueNotYetWrittenPromptRefused`). A message absorbed into a running turn (`absorbed_mid_turn`, delivered by process ApplyArrivalBridgeRecord) has no prompt to refuse and no history to exclude, so it runs as `Pilot`: the package is built and recorded, never injected, the prompt is never refused, and a build failure is recorded in `turns/<requestId>.yaml` without a `canceled` close (`StatelessInvocationAbsorbedMidTurnRunsAsPilotNeverRefused`).
- Mode (section 3.1, resolutions of round 5): the plugin reads `mode` through GetAgentContextStatusQuery at host session start (SessionStart, or the first prompt on a host without a SessionStart event: cline, cline-v2, and opencode) and caches it as `agentContextMode` in the host session's `turn-state.yaml` (section 3.4, Plugin per-turn store; absent before Phase C and read as `Off`) through `Update-TurnState` (`StatelessInvocationHostWithoutSessionStartModeReadAtFirstPrompt`), and RunStatelessInvocation reads `agentContextMode` at each prompt; the status query at host session start is the plugin's only agent-context read there, and its cost is bounded in GetAgentContextStatusQuery (section 6.5, risk R-R4); a Mode it cannot read, including a read that misses the hook bound, or a value it does not know, is `Off` (`StatelessInvocationModeUnreadableAtSessionStartAssumedOff`); a change away from `Off` takes effect at the next host session start (`StatelessInvocationModeSwitchedOnMidSessionTakesEffectAtNextSessionStart`). A 409 `agent_context_disabled` answer to the build or to the submission records `agentContextMode` `Off` for the host session through `Update-TurnState` and the request continues statefully: no refusal, no `canceled` close, and the turn closes as any stateful turn, because `Off` is the stateful default and the documented rollback (`StatelessInvocationModeSwitchedOffMidSessionBuild409DisabledContinuesStatefulNoCancel`, `StatelessInvocationModeSwitchedOffBeforeCloseSubmit409DisabledTurnClosesNormally`).
- Hosts that run it as `Off`: a host session reported `turn_logging_unsupported` (Phase A, D6) has no minted turn and stays stateful, reporting `turn_logging_unsupported`; a host whose prompt hook cannot refuse a prompt, or that runs the prompt anyway after a hook timeout, crash, or `plugin_core_unavailable`, runs `Pilot` and `Stateless` as `Off` (no build, no refusal) and its feature-matrix row records the limitation; P0.2 probe (j) records each host's refusal capability (`StatelessInvocationHostCannotRefuseTreatedAsOffNoBuildFeatureMatrixRow`).
- The turn (hosts without a bridge, by `Resolve-MintOutcome`): `created`, or `merged` with the turn in progress (a duplicate delivery), runs the build; a duplicate delivery's second build carries the same idempotency key and client inputs, so it returns the stored package and never refuses (`StatelessInvocationDuplicateDeliverySecondBuildReturnsStoredPackageNoSpuriousRefusal`). An `outage` (a `pending-<nonce>` turn) fails the invocation like a build outage ("Fail the invocation"): the prompt is refused with the outage shown, and the `canceled` close of the pending id is captured in the failsafe behind its mint record, in the DrainFailsafe order (`StatelessInvocationMintOutagePendingTurnPromptRefusedCancelQueuedBehindMint`). A `final` refusal leaves no turn, so the prompt is refused with the code and the Stop gate warns the refusal, naming the prompt (FR-MCP-SESSIONTURN-001-AC008, `StatelessInvocationFinalMintRefusalPromptRefusedStopWarnsNamingPrompt`); a hook that mints nothing (`nonce_source_missing`, or `turn-open-contended`) also leaves no turn and is refused the same way. `unsupported` (the equal-field rule) and `merged` with a terminal turn run nothing.
- Build: BuildAgentContextPackageCommand with the bound `agent`, `sessionId`, and `requestId`, the prompt text, and idempotency key `inv-` followed by the lowercase hex SHA-256 of the UTF-8 text `<agent>`, LF, `<sessionId>`, LF, `<requestId>` (68 characters, within the key's 128; the whole turn key, because a legacy request id is unique only within its session, so two such turns build two packages, `StatelessInvocationIdempotencyKeyFromWholeTurnKeyTwoLegacySessionsTwoPackages`). The plugin keeps the returned `packageId` in its per-turn file `turns/<requestId>.yaml`; no session-log turn field holds it.
- Package failure (operator, 2026-10-11: "Fail the invocation"): for any other build error or outage (a transport outage, 503 `backend_unavailable`, a busy store's 503 `persistence_error` with `retryable: true`, and 409 `conflict` with `retryable: true` are outages, section 3.1, Deadlock and busy outcomes and Retryable answers; the REPL policy's `transaction_degraded` and `mutation_not_transactional` are build errors, section 6.5, `StatelessInvocationBuildTransactionDegradedTreatedAsBuildError`), or a success response whose package status is not `Built` (a defensive check; the build never returns one, `StatelessInvocationPackageNotBuiltRefused`), the plugin refuses the prompt with the error code and message shown to the operator (for `agent_context_budget_exceeded` also `tokenBudget`, `mandatoryTokenCount`, `overflowTokenCount`, and the mandatory items), produces no answer, and closes the request's own turn `canceled`: the plugin, not the model, sends the bound turn at status `canceled` with `failureNote` `agent_context_invocation_failed <code>: <message>` (with the same counts for an overflow, `StatelessInvocationOverflowRefusalAndFailureNoteCarryTokenCounts`) through SubmitSessionLogCommand on the persist path of process PersistTurnWriteWithReceipt (canceled turns are not gated, FR-MCP-SESSIONCLASS-002; `Test-ModelEvidence` never applies to a `canceled` close, section 3.5.6). Receipt branches: `persisted` removes the request from `openDelivered` and the Stop ledger after the confirmed close, so the Stop gate never waits on a turn the plugin itself closed (`StatelessPackageFailureClosesOwnTurnCanceledRemovedFromLedgerNoOtherTurnTouched`); `queued` (an outage) holds the close in the failsafe, and the Stop gate warns degraded persistence naming the package failure, never blocks, and keeps the ledger entry until the drain confirms the close (`StatelessPackageFailureCancelCloseQueuedByOutageStopWarnsDegradedDrainConfirmsAndClears`); any other receipt is shown with its code and the turn stays open for the Stop gate. This is the only `canceled` a hook ever writes, and only for the failed request's own turn, never another turn (the Phase A isolation rule, refined for this case, which updates FR-MCP-TRIAGESTORE-001 ac-2: `IsolationHooksNeverPersistCanceledExceptOwnTurnPackageFailure`).
- History exclusion (operator: "Each agent invocation can run stateless"): in `Stateless` mode the model's input for the request is exactly the rendered package, the current request, and the deterministic `REQUIRED MEMORIES` block the host already injects (section 3.1, Memories in Stateless mode; FR-MCP-MEMORY-003, -007); history stays excluded. Exclusion removes the messages of earlier requests only: on each later model call of the same request (a tool loop) the request's own messages (its prompt, the model's tool calls, and their results) stay with the package and the memory block (section 3.1, Tool loops in `Stateless`; `StatelessToolLoopOwnToolCallsKeptPriorRequestsExcluded`). The plugin achieves it only through an authoritative host operation recorded in the host's P0.2 row: replacing the model's message list for that request (opencode `experimental.chat.messages.transform`, whose `output.messages` the adapter replaces with the list the core returns, which carries the memory block; its input is empty, so the core takes the host session key from `output.messages[].info.sessionID`; and cline-v2 `beforeModel`, forwarded by its adapter, where cline-v2's P0.2 row (probe (j)) records that the model request the core returns replaces the model's messages, that request carrying the package, the current request, and the memory block, section 3.2, TypeScript-host adapter contract) or starting a fresh host invocation whose only input is the package, the request, and the memory block. A host without such an operation cannot run `Stateless`: its plugin treats `Stateless` as `Pilot` (the package is built and recorded, the model answers statefully, and the submission's `invocationMode` is `Stateful`) and its feature-matrix row records the limitation (`StatelessHostWithoutHistoryExclusionTreatedAsPilotFeatureMatrixRow`). Each supported host has a host-level test that sends a sentinel prior turn and asserts, on the message list the host passes to its model call (for opencode, the host's list after the adapter applies the core's `output.messages`), captured by the host test harness and not by the adapter fixture, which records only what the adapter forwards, that the sentinel is absent while the package, the current request, and the `REQUIRED MEMORIES` block are present (`StatelessHostSentinelPriorTurnAbsentFromModelRequest`); the Phase 4 live check repeats it on each host whose P0.2 row records history exclusion.
- Submission (operator, 2026-10-12: "Model calls submit tool (Recommended)"): in `Pilot` and `Stateless` the model submits its response with the `agent_context_submit_response` tool (SubmitAgentResponseCommand) before it closes the turn, with its claims, evidence, and `invocationMode` (`Stateless` only when history was excluded; `Stateful` in `Pilot`, for an absorbed message, and on a host that runs `Stateless` as `Pilot`). The plugin binds the call to its request as it binds a verb (process BindVerbToTurn) and supplies that request's `packageId` from `turns/<requestId>.yaml`, or, when a hook process boundary lost it, from QueryAgentContextPackagesQuery `{ agent, sessionId, requestId, status: Built }` (`StatelessInvocationPackageIdLostFoundAgainByRequestId`); after a 201 or 200 it requests the deterministic grade (RunDeterministicGradeCommand) for the returned `responseId`. A failed submission or grade is reported in the tool result and writes nothing to the turn; a missing submission is not a Stop-gate item, and the pilot report counts requests with a package and no response. The model grade comes later from an HV request covering the turn (CreateAgentContextHvRequestCommand, RecordModelGradeCommand).

The diagram draws the refusal and its `canceled` close as one call of the nested activity FailInvocation, and the injection, the model's submission, and the grade request as one call of the nested activity AnswerAndSubmit. An unreadable Mode and a cached 409 `agent_context_disabled` both mean `Off`. When no turn exists to close (a bridge-host request whose `enqueue` record is not yet in the transcript, a final mint refusal, `nonce_source_missing`, or `turn-open-contended`), the prompt is refused with its code; for a final mint refusal the Stop gate also warns, naming the prompt. An absorbed request whose build fails records the failure in its per-turn file and is neither refused nor closed, and the model answers statefully. The build carries `agent`, `sessionId`, `requestId`, the prompt text, and the idempotency key `inv-` plus the SHA-256 of its whole turn key (Build bullet above). The Mode, host, trigger, and turn checks before the build are drawn as one call of the nested activity ResolveInvocationTurn below, whose edges are its returns.

![Process RunStatelessInvocation: run a stateless invocation (plugin process) (activity), part 1 of 6](c4/rendered/RunStatelessInvocation.svg)

View: RunStatelessInvocation

![Process RunStatelessInvocation: run a stateless invocation (plugin process) (activity), part 2 of 6](c4/rendered/RunStatelessInvocation-part2.svg)

View: RunStatelessInvocation-part2

![Process RunStatelessInvocation: run a stateless invocation (plugin process) (activity), part 3 of 6](c4/rendered/RunStatelessInvocation-part3.svg)

View: RunStatelessInvocation-part3

![Process RunStatelessInvocation: run a stateless invocation (plugin process) (activity), part 4 of 6](c4/rendered/RunStatelessInvocation-part4.svg)

View: RunStatelessInvocation-part4

![Process RunStatelessInvocation: run a stateless invocation (plugin process) (activity), part 5 of 6](c4/rendered/RunStatelessInvocation-part5.svg)

View: RunStatelessInvocation-part5

![Process RunStatelessInvocation: run a stateless invocation (plugin process) (activity), part 6 of 6](c4/rendered/RunStatelessInvocation-part6.svg)

View: RunStatelessInvocation-part6

##### Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build)

A host session reported `turn_logging_unsupported`, a cached Mode `Off` (an unreadable Mode included), and a host whose prompt hook cannot refuse (P0.2 probe (j)) leave the turn stateful. A delivery absorbed into a running turn runs as `Pilot`. On a bridge host the hook applies pending transcript records through `Sync-PluginArrivalLog` up to its start and takes the bridge-minted `requestId`; on another host `Resolve-MintOutcome` classifies the hook's own mint. A request with no turn to close is refused with its code (a final mint refusal, `nonce_source_missing`, or a contended `turn-open.lock`; for a final mint refusal the Stop gate also warns, naming the prompt); a mint outage (a `pending-` nonce turn) fails the invocation through FailInvocation; an `unsupported` or merged terminal mint leaves the turn stateful. Each branch ends in its own terminal so the diagram stays one page.

![Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build) (activity), part 1 of 5](c4/rendered/ResolveInvocationTurn.svg)

View: ResolveInvocationTurn

![Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build) (activity), part 2 of 5](c4/rendered/ResolveInvocationTurn-part2.svg)

View: ResolveInvocationTurn-part2

![Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build) (activity), part 3 of 5](c4/rendered/ResolveInvocationTurn-part3.svg)

View: ResolveInvocationTurn-part3

![Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build) (activity), part 4 of 5](c4/rendered/ResolveInvocationTurn-part4.svg)

View: ResolveInvocationTurn-part4

![Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build) (activity), part 5 of 5](c4/rendered/ResolveInvocationTurn-part5.svg)

View: ResolveInvocationTurn-part5

##### Nested activity: FailInvocation (refusal and canceled close of RunStatelessInvocation)

The plugin refuses the prompt with the error code and message shown to the operator (for `agent_context_budget_exceeded` also `tokenBudget`, `mandatoryTokenCount`, `overflowTokenCount`, and the mandatory items) and produces no answer. It then closes the request's own turn `canceled` with `failureNote` `agent_context_invocation_failed <code>: <message>` through process PersistTurnWriteWithReceipt, with no model-evidence check. On `persisted` the request leaves `openDelivered` and the Stop ledger after the confirmed close; on `queued` (an outage, including a busy store and 409 `conflict` with `retryable: true`, which keep the failsafe record, section 3.1, Retryable answers; or a pending mint whose close waits behind its mint record) the close is held in the failsafe and the Stop gate warns degraded persistence naming the package failure, never blocks, and keeps the ledger entry until the drain confirms; any other receipt is shown with its code and the turn stays open for the Stop gate.

![Nested activity: FailInvocation (refusal and canceled close of RunStatelessInvocation) (activity), part 1 of 2](c4/rendered/FailInvocation.svg)

View: FailInvocation

![Nested activity: FailInvocation (refusal and canceled close of RunStatelessInvocation) (activity), part 2 of 2](c4/rendered/FailInvocation-part2.svg)

View: FailInvocation-part2

##### Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation)

The plugin keeps the returned `packageId` in `turns/<requestId>.yaml`. It injects only for `Stateless` on a host with an authoritative history-exclusion operation (P0.2 row): the model input becomes the rendered package, the request, and the `REQUIRED MEMORIES` block, and `invocationMode` is `Stateless`; `Pilot`, an absorbed request, and `Stateless` on a host without that operation answer statefully with `invocationMode` `Stateful`. Before it closes the turn the model calls `agent_context_submit_response`, and the plugin supplies the `packageId` from the per-turn file, or from QueryAgentContextPackagesQuery `{ agent, sessionId, requestId, status: Built }`. A failed submission is reported in the tool result and the model resubmits; the pilot report counts a package without a response. A failed grade request is reported the same way and the grade is re-run on request. After a 201 grade the response stays `Grading` (or `Stale`, when the grade's staleness dispatch found a changed dependency) until an HV request covering the turn records its model grade (CreateAgentContextHvRequestCommand, RecordModelGradeCommand).

![Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation) (activity), part 1 of 5](c4/rendered/AnswerAndSubmit.svg)

View: AnswerAndSubmit

![Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation) (activity), part 2 of 5](c4/rendered/AnswerAndSubmit-part2.svg)

View: AnswerAndSubmit-part2

![Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation) (activity), part 3 of 5](c4/rendered/AnswerAndSubmit-part3.svg)

View: AnswerAndSubmit-part3

![Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation) (activity), part 4 of 5](c4/rendered/AnswerAndSubmit-part4.svg)

View: AnswerAndSubmit-part4

![Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation) (activity), part 5 of 5](c4/rendered/AnswerAndSubmit-part5.svg)

View: AnswerAndSubmit-part5

### 6.7 Phase C sequence diagrams

Participants carry short names, qualified here. `Caller` is any surface (the plugin through the REPL, the REPL, the typed client, or an MCP client). `Controller` is `AgentContextController` for REST (routes under `/qbrainai/agent-context`, shown without that prefix) and the `FwhMcpTools.AgentContext` tool for MCP. `Handler` is the unit's handler (namespace `QBrainAi.Support.Mcp.AgentContext.Packages`, `.Responses`, or `.Graph`, then `.Commands` or `.Queries`, by owning context). `Store` is the workspace database (`McpDbContext`). `GraphRAG` is `IGraphRagService`. `Plugin` is the PowerShell plugin core (`plugins/core/lib-ps`) and `Host` the agent host that runs it. `IDispatcher` and its pipeline behaviors are drawn once, in the RequestPipeline sequence; every other sequence draws the dispatch as one `SendAsync` or `QueryAsync` message from `Controller` to `Handler` followed by the note `ref RequestPipeline`, and an internal dispatch (a handler dispatching another unit through `IDispatcher`, which runs the same pipeline) as a handler self-message. Status codes shown are REST: an MCP tool returns the same result body without a status, and an error as the classified error object with the same `code` and `retryable` (`ClassifiedErrorException` mapped by `McpErrorClassifier`, section 3.5.2). A handler's result returns through the dispatcher to the controller; each branch draws only the controller's answer, and the caller's reaction is a note over `Caller`. Each sequence has at most five participants, so it fits the width of a landscape letter page at 8 pt; a shared component that has no room for a participant is one handler self-message. Labels carry no `<br/>` line breaks: the renderer wraps an unbroken label to the space between participants, while a broken label keeps the width of its longest line and widens the diagram. A sequence too long for three pages calls a nested sequence (heading `##### Nested sequence: <Name> (<purpose>)`) through one `ref` note; the nested sequence draws every branch and error terminal of its steps, and the parent continues after the note on the nested sequence's success path.

Shared components (section 3.5) appear as one message, one participant, or one `ref` note, with only the exits they return; their steps are drawn once in section 3.5:

- Request pipeline: drawn once, in the RequestPipeline sequence below, with both of its refusals after the dispatch: WorkspaceResolutionBehavior's 400 `validation_error` with `details.field` `workspacePath`, and ModeGateBehavior's 409 `agent_context_disabled`. Every other sequence shows the dispatch and then the note `ref RequestPipeline`, marked gated (the record implements `IModeGatedRequest`: every agent-context command except DecideAgentResponseTrustCommand, which only the grade units dispatch) or not gated (GetAgentContextStatusQuery and every read-only query, which stay available in Mode `Off` so the stored records stay readable for audit, section 3.1 "Reads in `Off`"). Request validation runs in the handler, after the dispatch; the one earlier refusal is an invalid score lexeme, which `ScoreLexemeJsonConverter` refuses at deserialization (section 3.5.4).
- `SerializableWrite`: a unit that checks and then saves draws one note naming the delegate it runs. The delegate reads and writes only the database (section 3.1, "Database-only delegates"): every git, file, source, retrieval, and GraphRAG read is drawn before that note, and the delegate re-checks the database state those reads relied on (the turn's status, each locked response's `StatusVersion`); a changed state ends the attempt and the unit runs again from its reads. Its exhaustion exit, after at most four attempts (section 3.5.2), is the branch `409 conflict, retryable true` (nothing saved), which the caller may retry with the same request within the bounded retries of C9 (section 3.3). Key allocation (`ReadableKeyAllocator`) exhausts into the same answer.
- `IdempotentCreate`: one `IdempotentCreate.Find` message, before any precondition that can change, with the exits stored record (200, or the stored `Overflow` package's 422 for Build), `409 conflict, retryable false` (key reused with other inputs), and absent (a soft-deleted record with the key is revived and written anew, section 3.5.2). A unit that reads outside the database (Build, Submit, and the model grade) draws the lookup read-only before those reads, so a retry returns the stored record without reading any source, and its delegate looks the key up again as its first step; the HV request, which reads only the database, draws it inside its SerializableWrite, as `RelationshipWriter` does for an identical link. A concurrent duplicate that wins the key's unique index ends the attempt, and the rerun's lookup finds the winner, so a retry and a race take the same branch.
- Lookups: `BuiltPackageLookup` (404 `not_found` with `details.resource` `package`, 409 `agent_context_package_overflow`), `TurnLocator` keyed by `agent`, `sessionId`, and `requestId` (`InvalidIdentifier`, `SessionNotFound`, `TurnNotFound`, mapped to each unit's bodies), and `ResponseIdList` (400 `validation_error`; unknown ids returned in the result's `notFound`).
- `IResponseStalenessRule`: a read-only self-message of the handler wherever a unit only reads a status (every query, and Build through RetrieveAgentContextCandidatesQuery). Only EvaluateStalenessCommand persists `Stale` (section 3.1, "Database-only delegates"): a unit that relies on the persisted status dispatches it before its own SerializableWrite (ProjectOneResponse, and the two grade units, whose trust decision then reads the status it left).
- `PagedQuery` with `PageBounds` `Refuse` (limit 1..1000, default 50, offset at least 0, otherwise 400 `validation_error` with `details.field` `limit` or `offset`) returns `PageResult` `{ totalCount, limit, offset, items }`. The note "PagedQuery paging" means: the caller reads the next page at `offset` plus the items count until it reaches `totalCount`.
- `ResponseStatusTransition` (same transition already applied: 200 unchanged; a transition the lifecycle does not allow: 409 `agent_context_transition_invalid`), `RelationshipWriter` (inside the caller's delegate, with the target hashes `SourceResolver` read before it), `SourceResolver`, and `PackageAssembler`, as named in each sequence.
- The plugin processes OpenTurnFromPromptHook, ApplyArrivalBridgeRecord, BindVerbToTurn, and PersistTurnWriteWithReceipt appear as `ref` notes.
- Projection pass: not drawn per sequence. A unit whose transaction marked a projection row (the two grade units through the trust decision, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand) dispatches ProjectAgentResponsesCommand with no `responseIds` after its commit and before it answers, and a failed dispatch is logged without changing its answer (section 6.5; the ProjectAgentResponsesCommand sequence draws the pass). When its commit moved a response out of `Trusted` or into `Rejected`, `Superseded`, or `Stale`, it first dispatches EvaluateStalenessCommand for the responses that depend on that response, under the same rule (section 3.1, Dependent responses; the EvaluateStalenessCommand sequence draws it).

#### Sequence: RequestPipeline (every Phase C dispatch)

The CQRS behaviors of section 3.5.1 run inside the scoped `IDispatcher`, after the controller or tool calls `SendAsync` or `QueryAsync` and before the handler, for every command and query. WorkspaceResolutionBehavior resolves a record's non-empty `WorkspacePath` into the dispatch scope's `WorkspaceContext` and otherwise keeps the workspace the transport resolved; it refuses a path that names no registered workspace, or a record with no workspace that does not implement `IAllowsGlobalWorkspace`, with 400 `validation_error` and `details.field` `workspacePath` on every surface, the status TR-MCP-MT-002 gives an unregistered workspace. On REST, `WorkspaceResolutionMiddleware` still answers a missing or unknown `X-Workspace-Path` before any controller with today's body (TR-MCP-MT-002), so the behavior's refusal reaches REST only through a record path that names an unregistered workspace. A plugin that receives 409 `agent_context_disabled` from a build or a submission caches `Off` for its host session and continues statefully (section 3.1, Mode changes).

![Sequence: RequestPipeline (every Phase C dispatch), part 1 of 4](c4/rendered/RequestPipeline-Sequence.svg)

View: RequestPipeline-Sequence

![Sequence: RequestPipeline (every Phase C dispatch), part 2 of 4](c4/rendered/RequestPipeline-Sequence-part2.svg)

View: RequestPipeline-Sequence-part2

![Sequence: RequestPipeline (every Phase C dispatch), part 3 of 4](c4/rendered/RequestPipeline-Sequence-part3.svg)

View: RequestPipeline-Sequence-part3

![Sequence: RequestPipeline (every Phase C dispatch), part 4 of 4](c4/rendered/RequestPipeline-Sequence-part4.svg)

View: RequestPipeline-Sequence-part4

![Sequence: RequestPipeline (every Phase C dispatch), alt 1 branch 1 of 2: path unregistered, or no workspace and not allowed, part 1 of 2](c4/rendered/RequestPipeline-Sequence-alt1-1.svg)

View: RequestPipeline-Sequence-alt1-1

![Sequence: RequestPipeline (every Phase C dispatch), alt 1 branch 1 of 2: path unregistered, or no workspace and not allowed, part 2 of 2](c4/rendered/RequestPipeline-Sequence-alt1-1-part2.svg)

View: RequestPipeline-Sequence-alt1-1-part2

![Sequence: RequestPipeline (every Phase C dispatch), alt 1 branch 2 of 2: resolved into the scope's WorkspaceContext](c4/rendered/RequestPipeline-Sequence-alt1-2.svg)

View: RequestPipeline-Sequence-alt1-2

![Sequence: RequestPipeline (every Phase C dispatch), alt 2 branch 1 of 2: record implements IModeGatedRequest and Mode is Off, part 1 of 3](c4/rendered/RequestPipeline-Sequence-alt2-1.svg)

View: RequestPipeline-Sequence-alt2-1

![Sequence: RequestPipeline (every Phase C dispatch), alt 2 branch 1 of 2: record implements IModeGatedRequest and Mode is Off, part 2 of 3](c4/rendered/RequestPipeline-Sequence-alt2-1-part2.svg)

View: RequestPipeline-Sequence-alt2-1-part2

![Sequence: RequestPipeline (every Phase C dispatch), alt 2 branch 1 of 2: record implements IModeGatedRequest and Mode is Off, part 3 of 3](c4/rendered/RequestPipeline-Sequence-alt2-1-part3.svg)

View: RequestPipeline-Sequence-alt2-1-part3

![Sequence: RequestPipeline (every Phase C dispatch), alt 2 branch 2 of 2: not gated, or Mode is Pilot or Stateless, part 1 of 3](c4/rendered/RequestPipeline-Sequence-alt2-2.svg)

View: RequestPipeline-Sequence-alt2-2

![Sequence: RequestPipeline (every Phase C dispatch), alt 2 branch 2 of 2: not gated, or Mode is Pilot or Stateless, part 2 of 3](c4/rendered/RequestPipeline-Sequence-alt2-2-part2.svg)

View: RequestPipeline-Sequence-alt2-2-part2

![Sequence: RequestPipeline (every Phase C dispatch), alt 2 branch 2 of 2: not gated, or Mode is Pilot or Stateless, part 3 of 3](c4/rendered/RequestPipeline-Sequence-alt2-2-part3.svg)

View: RequestPipeline-Sequence-alt2-2-part3

#### Sequence: submit GetAgentContextStatusQuery

The plugin reads the Mode once at host session start (SessionStart, or the first prompt on a host without a SessionStart event: cline, cline-v2, and opencode) and caches it as `agentContextMode` in the host session's `turn-state.yaml` through `Update-TurnState`; a Mode it cannot read, including a read that misses the SessionStart hook bound, or a value it does not know, is `Off`, and a change away from `Off` takes effect at the next host session start (section 3.1, Mode changes). Responses are counted by effective status, through `IResponseStalenessRule` read-only, and the query writes nothing. Because every host session start makes this read (risk R-R4), the handler reads each distinct dependency and commit once through `SourceResolver`, shared by every `Submitted`, `Grading`, or `Trusted` response that cites it (inference: the work is bounded by the distinct dependencies cited, not by the response count), and answers within the SessionStart hook bound with 1000 `Trusted` responses (`StatusThousandTrustedResponsesAnswersWithinSessionStartHookBound`).

![Sequence: submit GetAgentContextStatusQuery](c4/rendered/STR-06-09.svg)

View: STR-06-09

![Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 1 of 7](c4/rendered/STR-06-09-alt1-1.svg)

View: STR-06-09-alt1-1

![Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 2 of 7](c4/rendered/STR-06-09-alt1-1-part2.svg)

View: STR-06-09-alt1-1-part2

![Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 3 of 7](c4/rendered/STR-06-09-alt1-1-part3.svg)

View: STR-06-09-alt1-1-part3

![Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 4 of 7](c4/rendered/STR-06-09-alt1-1-part4.svg)

View: STR-06-09-alt1-1-part4

![Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 5 of 7](c4/rendered/STR-06-09-alt1-1-part5.svg)

View: STR-06-09-alt1-1-part5

![Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 6 of 7](c4/rendered/STR-06-09-alt1-1-part6.svg)

View: STR-06-09-alt1-1-part6

![Sequence: submit GetAgentContextStatusQuery, alt 1 branch 1 of 2: server answers, part 7 of 7](c4/rendered/STR-06-09-alt1-1-part7.svg)

View: STR-06-09-alt1-1-part7

![Sequence: submit GetAgentContextStatusQuery, alt 1 branch 2 of 2: outage, any error, or no answer within the hook bound, part 1 of 2](c4/rendered/STR-06-09-alt1-2.svg)

View: STR-06-09-alt1-2

![Sequence: submit GetAgentContextStatusQuery, alt 1 branch 2 of 2: outage, any error, or no answer within the hook bound, part 2 of 2](c4/rendered/STR-06-09-alt1-2-part2.svg)

View: STR-06-09-alt1-2-part2

#### Sequence: submit BuildAgentContextPackageCommand

`Caller` is usually the plugin running process RunStatelessInvocation (below). Build reads before it writes (section 3.1, "Database-only delegates"): the read-only key lookup, then nested sequence ReadBuildInputs, then `PackageAssembler` in memory, and only then nested sequence SaveBuiltPackage, whose SerializableWrite delegate reads and writes only the database. No transaction is open during a retrieval, a GraphRAG call, or a git read, so on SQLite no hook write waits on them (`BuildOnSqliteHookWriteDuringRetrievalNotBlocked`). The idempotency fingerprint is over the request's client inputs, and the read-only lookup runs before every read that can change, so a retry of a lost answer returns the stored package even after a source or the HEAD commit changed; a retry of an overflow finds the stored `Overflow` package and returns the same 422, never an answer. `InputsSha256` stays the provenance and replay check. `PackageAssembler` runs the secret filter (`SecretRedactor`), the omission filter, the findings, the mandatory-context check, the budget fill, and the rendering (section 3.5.5), and never truncates mandatory context. The input snapshot records every input the assembler reads, including each prior-response candidate's trust and staleness state at build time and the `contradicts` and `blocks` links the findings read. Build persists nothing but its package, its children, its snapshot, and its links: it reads staleness and never persists it (only EvaluateStalenessCommand persists `Stale`).

![Sequence: submit BuildAgentContextPackageCommand, part 1 of 3](c4/rendered/STR-06-10.svg)

View: STR-06-10

![Sequence: submit BuildAgentContextPackageCommand, part 2 of 3](c4/rendered/STR-06-10-part2.svg)

View: STR-06-10-part2

![Sequence: submit BuildAgentContextPackageCommand, part 3 of 3](c4/rendered/STR-06-10-part3.svg)

View: STR-06-10-part3

![Sequence: submit BuildAgentContextPackageCommand, alt 1 branch 1 of 2: request invalid (ids, tokenBudget)](c4/rendered/STR-06-10-alt1-1.svg)

View: STR-06-10-alt1-1

![Sequence: submit BuildAgentContextPackageCommand, alt 1 branch 2 of 2: request valid, part 1 of 2](c4/rendered/STR-06-10-alt1-2.svg)

View: STR-06-10-alt1-2

![Sequence: submit BuildAgentContextPackageCommand, alt 1 branch 2 of 2: request valid, part 2 of 2](c4/rendered/STR-06-10-alt1-2-part2.svg)

View: STR-06-10-alt1-2-part2

![Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 1 of 4: stored package Built (a retry or a duplicate delivery)](c4/rendered/STR-06-10-alt2-1.svg)

View: STR-06-10-alt2-1

![Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 2 of 4: stored package Overflow (a retry)](c4/rendered/STR-06-10-alt2-2.svg)

View: STR-06-10-alt2-2

![Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 3 of 4: key reused with other inputs](c4/rendered/STR-06-10-alt2-3.svg)

View: STR-06-10-alt2-3

![Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 4 of 4: absent, part 1 of 5](c4/rendered/STR-06-10-alt2-4.svg)

View: STR-06-10-alt2-4

![Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 4 of 4: absent, part 2 of 5](c4/rendered/STR-06-10-alt2-4-part2.svg)

View: STR-06-10-alt2-4-part2

![Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 4 of 4: absent, part 3 of 5](c4/rendered/STR-06-10-alt2-4-part3.svg)

View: STR-06-10-alt2-4-part3

![Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 4 of 4: absent, part 4 of 5](c4/rendered/STR-06-10-alt2-4-part4.svg)

View: STR-06-10-alt2-4-part4

![Sequence: submit BuildAgentContextPackageCommand, alt 2 branch 4 of 4: absent, part 5 of 5](c4/rendered/STR-06-10-alt2-4-part5.svg)

View: STR-06-10-alt2-4-part5

##### Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction)

Every read here runs outside a transaction. `SourceResolver` reads the workspace HEAD commit and the profile and rule sources, and resolves and hashes every candidate through `ContentHasher`. RetrieveAgentContextCandidatesQuery is dispatched with the package's `agent`, `sessionId`, `requestId`, and `focusRefs`, and `preferGraphRag` equal to `GraphRag:UseForRetrieval`; it calls GraphRAG when preferred and reads each prior-response candidate's status through `IResponseStalenessRule` read-only, writing nothing. Every focus ref is also a candidate, and the request's own turn is never one. A missing profile or rule file is fixed by the operator in `ProfileSources` or `RuleSources`. The turn's status read here is the state SaveBuiltPackage re-checks.

![Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), part 1 of 2](c4/rendered/ReadBuildInputs-Sequence.svg)

View: ReadBuildInputs-Sequence

![Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), part 2 of 2](c4/rendered/ReadBuildInputs-Sequence-part2.svg)

View: ReadBuildInputs-Sequence-part2

![Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 1 branch 1 of 3: InvalidIdentifier](c4/rendered/ReadBuildInputs-Sequence-alt1-1.svg)

View: ReadBuildInputs-Sequence-alt1-1

![Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 1 branch 2 of 3: SessionNotFound or TurnNotFound](c4/rendered/ReadBuildInputs-Sequence-alt1-2.svg)

View: ReadBuildInputs-Sequence-alt1-2

![Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 1 branch 3 of 3: turn found, with its status, part 1 of 2](c4/rendered/ReadBuildInputs-Sequence-alt1-3.svg)

View: ReadBuildInputs-Sequence-alt1-3

![Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 1 branch 3 of 3: turn found, with its status, part 2 of 2](c4/rendered/ReadBuildInputs-Sequence-alt1-3-part2.svg)

View: ReadBuildInputs-Sequence-alt1-3-part2

![Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 2 branch 1 of 3: commit unreadable](c4/rendered/ReadBuildInputs-Sequence-alt2-1.svg)

View: ReadBuildInputs-Sequence-alt2-1

![Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 2 branch 2 of 3: no profile file or no rule file resolves](c4/rendered/ReadBuildInputs-Sequence-alt2-2.svg)

View: ReadBuildInputs-Sequence-alt2-2

![Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 2 branch 3 of 3: commit, profile, and rules read, part 1 of 4](c4/rendered/ReadBuildInputs-Sequence-alt2-3.svg)

View: ReadBuildInputs-Sequence-alt2-3

![Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 2 branch 3 of 3: commit, profile, and rules read, part 2 of 4](c4/rendered/ReadBuildInputs-Sequence-alt2-3-part2.svg)

View: ReadBuildInputs-Sequence-alt2-3-part2

![Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 2 branch 3 of 3: commit, profile, and rules read, part 3 of 4](c4/rendered/ReadBuildInputs-Sequence-alt2-3-part3.svg)

View: ReadBuildInputs-Sequence-alt2-3-part3

![Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction), alt 2 branch 3 of 3: commit, profile, and rules read, part 4 of 4](c4/rendered/ReadBuildInputs-Sequence-alt2-3-part4.svg)

View: ReadBuildInputs-Sequence-alt2-3-part4

##### Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence)

The delegate reads and writes only the database, within at most four attempts (section 3.5.2). It looks the key up again, which finds a concurrent build that won the key, re-checks the turn status ReadBuildInputs read (a change ends the attempt, and Build runs again from its reads), and inserts the package, its children, its input snapshot, and its links: `RelationshipWriter` with `CreatedBy` `BuildAgentContextPackageCommand`, `derived_from` each included source except a `Rule` read outside version control, and `relevant_to` the turn, with the hashes ReadBuildInputs read. An `Overflow` package is stored for audit with its mandatory items and token counts and no content. The package, its children, and its links commit together. The plugin keeps the `packageId` in its per-turn file and, in `Stateless`, injects `renderedContent`.

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), part 1 of 2](c4/rendered/SaveBuiltPackage.svg)

View: SaveBuiltPackage

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), part 2 of 2](c4/rendered/SaveBuiltPackage-part2.svg)

View: SaveBuiltPackage-part2

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 1 of 6: stored package Built](c4/rendered/SaveBuiltPackage-alt1-1.svg)

View: SaveBuiltPackage-alt1-1

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 2 of 6: stored package Overflow](c4/rendered/SaveBuiltPackage-alt1-2.svg)

View: SaveBuiltPackage-alt1-2

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 3 of 6: key reused with other inputs](c4/rendered/SaveBuiltPackage-alt1-3.svg)

View: SaveBuiltPackage-alt1-3

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 4 of 6: absent, and the turn status changed since ReadBuildInputs, part 1 of 3](c4/rendered/SaveBuiltPackage-alt1-4.svg)

View: SaveBuiltPackage-alt1-4

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 4 of 6: absent, and the turn status changed since ReadBuildInputs, part 2 of 3](c4/rendered/SaveBuiltPackage-alt1-4-part2.svg)

View: SaveBuiltPackage-alt1-4-part2

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 4 of 6: absent, and the turn status changed since ReadBuildInputs, part 3 of 3](c4/rendered/SaveBuiltPackage-alt1-4-part3.svg)

View: SaveBuiltPackage-alt1-4-part3

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 5 of 6: absent, and the turn status unchanged](c4/rendered/SaveBuiltPackage-alt1-5.svg)

View: SaveBuiltPackage-alt1-5

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 2 branch 1 of 2: PackageAssembler returned Overflow, part 1 of 2](c4/rendered/SaveBuiltPackage-alt2-1.svg)

View: SaveBuiltPackage-alt2-1

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 2 branch 1 of 2: PackageAssembler returned Overflow, part 2 of 2](c4/rendered/SaveBuiltPackage-alt2-1-part2.svg)

View: SaveBuiltPackage-alt2-1-part2

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 2 branch 2 of 2: assembled, part 1 of 2](c4/rendered/SaveBuiltPackage-alt2-2.svg)

View: SaveBuiltPackage-alt2-2

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 2 branch 2 of 2: assembled, part 2 of 2](c4/rendered/SaveBuiltPackage-alt2-2-part2.svg)

View: SaveBuiltPackage-alt2-2-part2

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 6 of 6: retries exhausted (SerializableWrite or key allocation), part 1 of 2](c4/rendered/SaveBuiltPackage-alt1-6.svg)

View: SaveBuiltPackage-alt1-6

![Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence), alt 1 branch 6 of 6: retries exhausted (SerializableWrite or key allocation), part 2 of 2](c4/rendered/SaveBuiltPackage-alt1-6-part2.svg)

View: SaveBuiltPackage-alt1-6-part2

#### Sequence: submit GetAgentContextPackageQuery

![Sequence: submit GetAgentContextPackageQuery, part 1 of 5](c4/rendered/STR-06-11.svg)

View: STR-06-11

![Sequence: submit GetAgentContextPackageQuery, part 2 of 5](c4/rendered/STR-06-11-part2.svg)

View: STR-06-11-part2

![Sequence: submit GetAgentContextPackageQuery, part 3 of 5](c4/rendered/STR-06-11-part3.svg)

View: STR-06-11-part3

![Sequence: submit GetAgentContextPackageQuery, part 4 of 5](c4/rendered/STR-06-11-part4.svg)

View: STR-06-11-part4

![Sequence: submit GetAgentContextPackageQuery, part 5 of 5](c4/rendered/STR-06-11-part5.svg)

View: STR-06-11-part5

![Sequence: submit GetAgentContextPackageQuery, alt 1 branch 1 of 2: not found](c4/rendered/STR-06-11-alt1-1.svg)

View: STR-06-11-alt1-1

![Sequence: submit GetAgentContextPackageQuery, alt 1 branch 2 of 2: found, Built or Overflow, part 1 of 3](c4/rendered/STR-06-11-alt1-2.svg)

View: STR-06-11-alt1-2

![Sequence: submit GetAgentContextPackageQuery, alt 1 branch 2 of 2: found, Built or Overflow, part 2 of 3](c4/rendered/STR-06-11-alt1-2-part2.svg)

View: STR-06-11-alt1-2-part2

![Sequence: submit GetAgentContextPackageQuery, alt 1 branch 2 of 2: found, Built or Overflow, part 3 of 3](c4/rendered/STR-06-11-alt1-2-part3.svg)

View: STR-06-11-alt1-2-part3

#### Sequence: submit QueryAgentContextPackagesQuery

![Sequence: submit QueryAgentContextPackagesQuery, part 1 of 4](c4/rendered/STR-06-12.svg)

View: STR-06-12

![Sequence: submit QueryAgentContextPackagesQuery, part 2 of 4](c4/rendered/STR-06-12-part2.svg)

View: STR-06-12-part2

![Sequence: submit QueryAgentContextPackagesQuery, part 3 of 4](c4/rendered/STR-06-12-part3.svg)

View: STR-06-12-part3

![Sequence: submit QueryAgentContextPackagesQuery, part 4 of 4](c4/rendered/STR-06-12-part4.svg)

View: STR-06-12-part4

![Sequence: submit QueryAgentContextPackagesQuery, alt 1 branch 1 of 2: unknown status, or limit or offset outside PageBounds](c4/rendered/STR-06-12-alt1-1.svg)

View: STR-06-12-alt1-1

![Sequence: submit QueryAgentContextPackagesQuery, alt 1 branch 2 of 2: filters valid, part 1 of 2](c4/rendered/STR-06-12-alt1-2.svg)

View: STR-06-12-alt1-2

![Sequence: submit QueryAgentContextPackagesQuery, alt 1 branch 2 of 2: filters valid, part 2 of 2](c4/rendered/STR-06-12-alt1-2-part2.svg)

View: STR-06-12-alt1-2-part2

#### Sequence: submit ReplayAgentContextPackageQuery

`PackageAssembler` rebuilds in memory from the stored input snapshot, `OptionsSnapshotJson`, and the focus refs only; because the snapshot records every input the assembler reads (section 3.5.5), Replay reproduces every assembler step without reading a live source or configuration, and stores nothing.

![Sequence: submit ReplayAgentContextPackageQuery, part 1 of 5](c4/rendered/STR-06-13.svg)

View: STR-06-13

![Sequence: submit ReplayAgentContextPackageQuery, part 2 of 5](c4/rendered/STR-06-13-part2.svg)

View: STR-06-13-part2

![Sequence: submit ReplayAgentContextPackageQuery, part 3 of 5](c4/rendered/STR-06-13-part3.svg)

View: STR-06-13-part3

![Sequence: submit ReplayAgentContextPackageQuery, part 4 of 5](c4/rendered/STR-06-13-part4.svg)

View: STR-06-13-part4

![Sequence: submit ReplayAgentContextPackageQuery, part 5 of 5](c4/rendered/STR-06-13-part5.svg)

View: STR-06-13-part5

![Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 1 of 5: not found](c4/rendered/STR-06-13-alt1-1.svg)

View: STR-06-13-alt1-1

![Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 2 of 5: Overflow](c4/rendered/STR-06-13-alt1-2.svg)

View: STR-06-13-alt1-2

![Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 3 of 5: no input snapshot stored](c4/rendered/STR-06-13-alt1-3.svg)

View: STR-06-13-alt1-3

![Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 4 of 5: built by another builder version](c4/rendered/STR-06-13-alt1-4.svg)

View: STR-06-13-alt1-4

![Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 5 of 5: replayable, part 1 of 3](c4/rendered/STR-06-13-alt1-5.svg)

View: STR-06-13-alt1-5

![Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 5 of 5: replayable, part 2 of 3](c4/rendered/STR-06-13-alt1-5-part2.svg)

View: STR-06-13-alt1-5-part2

![Sequence: submit ReplayAgentContextPackageQuery, alt 1 branch 5 of 5: replayable, part 3 of 3](c4/rendered/STR-06-13-alt1-5-part3.svg)

View: STR-06-13-alt1-5-part3

![Sequence: submit ReplayAgentContextPackageQuery, alt 2 branch 1 of 2: identical, part 1 of 2](c4/rendered/STR-06-13-alt2-1.svg)

View: STR-06-13-alt2-1

![Sequence: submit ReplayAgentContextPackageQuery, alt 2 branch 1 of 2: identical, part 2 of 2](c4/rendered/STR-06-13-alt2-1-part2.svg)

View: STR-06-13-alt2-1-part2

![Sequence: submit ReplayAgentContextPackageQuery, alt 2 branch 2 of 2: differences, part 1 of 2](c4/rendered/STR-06-13-alt2-2.svg)

View: STR-06-13-alt2-2

![Sequence: submit ReplayAgentContextPackageQuery, alt 2 branch 2 of 2: differences, part 2 of 2](c4/rendered/STR-06-13-alt2-2-part2.svg)

View: STR-06-13-alt2-2-part2

#### Sequence: submit SubmitAgentResponseCommand

`Caller` is the plugin carrying the model's `agent_context_submit_response` tool call, which the model makes before it closes the turn; the plugin supplies the `packageId` it kept for the request (section 3.1, Response submission). `SubmissionSha256` (through `ContentHasher`, over the whole submission) is the idempotency fingerprint. Submit reads before it writes (section 3.1, "Database-only delegates"): the read-only key lookup, then nested sequence ReadSubmissionInputs, then `SecretRedactor` redacts the content in memory, and only then the SerializableWrite. Its delegate reads and writes only the database: it looks the key up again, which finds a concurrent submission that won the key, and inserts the response, its claims and evidence with both hashes, and its links (`RelationshipWriter`, `CreatedBy` `SubmitAgentResponseCommand`: `derived_from` the package, `supports` each `Requirement` or `File` evidence item, with the hashes ReadSubmissionInputs read). A stored package never changes, so the key is the only state the delegate re-checks. The response, its children, and its links commit together.

![Sequence: submit SubmitAgentResponseCommand, part 1 of 3](c4/rendered/STR-06-14.svg)

View: STR-06-14

![Sequence: submit SubmitAgentResponseCommand, part 2 of 3](c4/rendered/STR-06-14-part2.svg)

View: STR-06-14-part2

![Sequence: submit SubmitAgentResponseCommand, part 3 of 3](c4/rendered/STR-06-14-part3.svg)

View: STR-06-14-part3

![Sequence: submit SubmitAgentResponseCommand, alt 1 branch 1 of 2: content, claims, evidence, format, or mode invalid, part 1 of 2](c4/rendered/STR-06-14-alt1-1.svg)

View: STR-06-14-alt1-1

![Sequence: submit SubmitAgentResponseCommand, alt 1 branch 1 of 2: content, claims, evidence, format, or mode invalid, part 2 of 2](c4/rendered/STR-06-14-alt1-1-part2.svg)

View: STR-06-14-alt1-1-part2

![Sequence: submit SubmitAgentResponseCommand, alt 1 branch 2 of 2: request valid, part 1 of 2](c4/rendered/STR-06-14-alt1-2.svg)

View: STR-06-14-alt1-2

![Sequence: submit SubmitAgentResponseCommand, alt 1 branch 2 of 2: request valid, part 2 of 2](c4/rendered/STR-06-14-alt1-2-part2.svg)

View: STR-06-14-alt1-2-part2

![Sequence: submit SubmitAgentResponseCommand, alt 2 branch 1 of 3: stored, same submission](c4/rendered/STR-06-14-alt2-1.svg)

View: STR-06-14-alt2-1

![Sequence: submit SubmitAgentResponseCommand, alt 2 branch 2 of 3: key reused with another submission (any field)](c4/rendered/STR-06-14-alt2-2.svg)

View: STR-06-14-alt2-2

![Sequence: submit SubmitAgentResponseCommand, alt 2 branch 3 of 3: absent, part 1 of 5](c4/rendered/STR-06-14-alt2-3.svg)

View: STR-06-14-alt2-3

![Sequence: submit SubmitAgentResponseCommand, alt 2 branch 3 of 3: absent, part 2 of 5](c4/rendered/STR-06-14-alt2-3-part2.svg)

View: STR-06-14-alt2-3-part2

![Sequence: submit SubmitAgentResponseCommand, alt 2 branch 3 of 3: absent, part 3 of 5](c4/rendered/STR-06-14-alt2-3-part3.svg)

View: STR-06-14-alt2-3-part3

![Sequence: submit SubmitAgentResponseCommand, alt 2 branch 3 of 3: absent, part 4 of 5](c4/rendered/STR-06-14-alt2-3-part4.svg)

View: STR-06-14-alt2-3-part4

![Sequence: submit SubmitAgentResponseCommand, alt 2 branch 3 of 3: absent, part 5 of 5](c4/rendered/STR-06-14-alt2-3-part5.svg)

View: STR-06-14-alt2-3-part5

![Sequence: submit SubmitAgentResponseCommand, alt 3 branch 1 of 4: winner with the same submission](c4/rendered/STR-06-14-alt3-1.svg)

View: STR-06-14-alt3-1

![Sequence: submit SubmitAgentResponseCommand, alt 3 branch 2 of 4: winner with another submission](c4/rendered/STR-06-14-alt3-2.svg)

View: STR-06-14-alt3-2

![Sequence: submit SubmitAgentResponseCommand, alt 3 branch 3 of 4: absent, part 1 of 2](c4/rendered/STR-06-14-alt3-3.svg)

View: STR-06-14-alt3-3

![Sequence: submit SubmitAgentResponseCommand, alt 3 branch 3 of 4: absent, part 2 of 2](c4/rendered/STR-06-14-alt3-3-part2.svg)

View: STR-06-14-alt3-3-part2

![Sequence: submit SubmitAgentResponseCommand, alt 3 branch 4 of 4: retries exhausted (SerializableWrite or key allocation), part 1 of 2](c4/rendered/STR-06-14-alt3-4.svg)

View: STR-06-14-alt3-4

![Sequence: submit SubmitAgentResponseCommand, alt 3 branch 4 of 4: retries exhausted (SerializableWrite or key allocation), part 2 of 2](c4/rendered/STR-06-14-alt3-4-part2.svg)

View: STR-06-14-alt3-4-part2

##### Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction)

Every read here runs outside a transaction. `BuiltPackageLookup` finds the package; the submission must match it (`agent` equal to the package's agent, `invocationMode` `Stateless` only on a `Stateless` package, and no `SessionLogTurn` evidence naming the package's own turn, section 3.1). `SourceResolver` reads the workspace HEAD commit and hashes each `File` evidence ref at its `commitSha` and each `Requirement`, `Receipt`, and `SessionLogTurn` ref from its source; each cited hash is kept as `CitedSha256`, and the server's hash is `Sha256`, null when an existing source cannot be read.

![Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), part 1 of 2](c4/rendered/ReadSubmissionInputs-Sequence.svg)

View: ReadSubmissionInputs-Sequence

![Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), part 2 of 2](c4/rendered/ReadSubmissionInputs-Sequence-part2.svg)

View: ReadSubmissionInputs-Sequence-part2

![Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 1 branch 1 of 4: package not found](c4/rendered/ReadSubmissionInputs-Sequence-alt1-1.svg)

View: ReadSubmissionInputs-Sequence-alt1-1

![Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 1 branch 2 of 4: package Overflow](c4/rendered/ReadSubmissionInputs-Sequence-alt1-2.svg)

View: ReadSubmissionInputs-Sequence-alt1-2

![Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 1 branch 3 of 4: agent, mode, or own-turn evidence inconsistent with the package, part 1 of 2](c4/rendered/ReadSubmissionInputs-Sequence-alt1-3.svg)

View: ReadSubmissionInputs-Sequence-alt1-3

![Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 1 branch 3 of 4: agent, mode, or own-turn evidence inconsistent with the package, part 2 of 2](c4/rendered/ReadSubmissionInputs-Sequence-alt1-3-part2.svg)

View: ReadSubmissionInputs-Sequence-alt1-3-part2

![Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 1 branch 4 of 4: package Built, submission consistent, part 1 of 2](c4/rendered/ReadSubmissionInputs-Sequence-alt1-4.svg)

View: ReadSubmissionInputs-Sequence-alt1-4

![Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 1 branch 4 of 4: package Built, submission consistent, part 2 of 2](c4/rendered/ReadSubmissionInputs-Sequence-alt1-4-part2.svg)

View: ReadSubmissionInputs-Sequence-alt1-4-part2

![Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 2 branch 1 of 3: commit unreadable](c4/rendered/ReadSubmissionInputs-Sequence-alt2-1.svg)

View: ReadSubmissionInputs-Sequence-alt2-1

![Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 2 branch 2 of 3: a ref names no existing source](c4/rendered/ReadSubmissionInputs-Sequence-alt2-2.svg)

View: ReadSubmissionInputs-Sequence-alt2-2

![Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 2 branch 3 of 3: every ref resolved, part 1 of 2](c4/rendered/ReadSubmissionInputs-Sequence-alt2-3.svg)

View: ReadSubmissionInputs-Sequence-alt2-3

![Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction), alt 2 branch 3 of 3: every ref resolved, part 2 of 2](c4/rendered/ReadSubmissionInputs-Sequence-alt2-3-part2.svg)

View: ReadSubmissionInputs-Sequence-alt2-3-part2

#### Sequence: submit GetAgentResponseQuery

`IResponseStalenessRule` evaluates a `Submitted`, `Grading`, or `Trusted` response read-only and the query returns its effective status, writing nothing, so a `Trusted` answer has just been checked.

![Sequence: submit GetAgentResponseQuery, part 1 of 5](c4/rendered/STR-06-15.svg)

View: STR-06-15

![Sequence: submit GetAgentResponseQuery, part 2 of 5](c4/rendered/STR-06-15-part2.svg)

View: STR-06-15-part2

![Sequence: submit GetAgentResponseQuery, part 3 of 5](c4/rendered/STR-06-15-part3.svg)

View: STR-06-15-part3

![Sequence: submit GetAgentResponseQuery, part 4 of 5](c4/rendered/STR-06-15-part4.svg)

View: STR-06-15-part4

![Sequence: submit GetAgentResponseQuery, part 5 of 5](c4/rendered/STR-06-15-part5.svg)

View: STR-06-15-part5

![Sequence: submit GetAgentResponseQuery, alt 1 branch 1 of 2: not found](c4/rendered/STR-06-15-alt1-1.svg)

View: STR-06-15-alt1-1

![Sequence: submit GetAgentResponseQuery, alt 1 branch 2 of 2: found, part 1 of 4](c4/rendered/STR-06-15-alt1-2.svg)

View: STR-06-15-alt1-2

![Sequence: submit GetAgentResponseQuery, alt 1 branch 2 of 2: found, part 2 of 4](c4/rendered/STR-06-15-alt1-2-part2.svg)

View: STR-06-15-alt1-2-part2

![Sequence: submit GetAgentResponseQuery, alt 1 branch 2 of 2: found, part 3 of 4](c4/rendered/STR-06-15-alt1-2-part3.svg)

View: STR-06-15-alt1-2-part3

![Sequence: submit GetAgentResponseQuery, alt 1 branch 2 of 2: found, part 4 of 4](c4/rendered/STR-06-15-alt1-2-part4.svg)

View: STR-06-15-alt1-2-part4

#### Sequence: submit QueryAgentResponsesQuery

`IResponseStalenessRule` evaluates every matching `Submitted`, `Grading`, or `Trusted` response read-only before the query filters, counts, and pages by effective status; it writes nothing.

![Sequence: submit QueryAgentResponsesQuery, part 1 of 4](c4/rendered/STR-06-16.svg)

View: STR-06-16

![Sequence: submit QueryAgentResponsesQuery, part 2 of 4](c4/rendered/STR-06-16-part2.svg)

View: STR-06-16-part2

![Sequence: submit QueryAgentResponsesQuery, part 3 of 4](c4/rendered/STR-06-16-part3.svg)

View: STR-06-16-part3

![Sequence: submit QueryAgentResponsesQuery, part 4 of 4](c4/rendered/STR-06-16-part4.svg)

View: STR-06-16-part4

![Sequence: submit QueryAgentResponsesQuery, alt 1 branch 1 of 2: unknown status, or limit or offset outside PageBounds](c4/rendered/STR-06-16-alt1-1.svg)

View: STR-06-16-alt1-1

![Sequence: submit QueryAgentResponsesQuery, alt 1 branch 2 of 2: filters valid, part 1 of 4](c4/rendered/STR-06-16-alt1-2.svg)

View: STR-06-16-alt1-2

![Sequence: submit QueryAgentResponsesQuery, alt 1 branch 2 of 2: filters valid, part 2 of 4](c4/rendered/STR-06-16-alt1-2-part2.svg)

View: STR-06-16-alt1-2-part2

![Sequence: submit QueryAgentResponsesQuery, alt 1 branch 2 of 2: filters valid, part 3 of 4](c4/rendered/STR-06-16-alt1-2-part3.svg)

View: STR-06-16-alt1-2-part3

![Sequence: submit QueryAgentResponsesQuery, alt 1 branch 2 of 2: filters valid, part 4 of 4](c4/rendered/STR-06-16-alt1-2-part4.svg)

View: STR-06-16-alt1-2-part4

#### Sequence: submit RunDeterministicGradeCommand

The grade reads before it writes, in the order of section 3.1 ("Database-only delegates"): every read-only check, then the EvaluateStalenessCommand dispatch, then the SerializableWrite. Before the SerializableWrite the handler loads the response and checks that it is gradable (found, neither `Rejected` nor `Superseded`), has `SourceResolver` check each evidence item (nested activity CheckOneEvidenceItem) and the commit ancestry, so no git read runs inside a transaction, and runs `SecretRedactor.ContainsSecret` over the content, claims, and evidence refs; it then dispatches EvaluateStalenessCommand for the response (the only writer of `Stale`, which evaluates outside any transaction and persists `Stale` in its own SerializableWrite when a dependency of the response or its package changed) and reads the response's status and `StatusVersion` that the dispatch left. It then runs the global checks (every claim has evidence, and the freshness of the response and its package, from that status), computes the exact scores, and derives the verdict with `ScoreVerdict` at `max(configured, 0.98)`. The SerializableWrite locks the response and re-checks its `StatusVersion` against the one the reads saw (a concurrent reject, supersede, or staleness change ends the attempt, and the unit runs again from its reads, so a response rejected meanwhile is then refused 409 `agent_context_response_not_gradable`, `GradeResponseRejectedDuringReadsReadsAgain409NotGradable`), saves the grade, its findings, and the thresholds in force, and stamps `RecordedUtc`, which orders "latest". The trust decision is the same dispatch, drawn the same way, as in SubmitOneModelGrade: DecideAgentResponseTrustCommand, not gated, in the same transaction, reading only the database (the stored status and the latest grades). The caller processes the answer by `responseStatus`: `Stale` is excluded from trusted retrieval and needs a new response; `Grading` after a `Pass` waits for the model grade of an HV request covering the turn (CreateAgentContextHvRequestCommand); a `Fail` shows every FAIL and UNKNOWN finding, to fix and submit a new response.

![Sequence: submit RunDeterministicGradeCommand, part 1 of 4](c4/rendered/STR-06-17.svg)

View: STR-06-17

![Sequence: submit RunDeterministicGradeCommand, part 2 of 4](c4/rendered/STR-06-17-part2.svg)

View: STR-06-17-part2

![Sequence: submit RunDeterministicGradeCommand, part 3 of 4](c4/rendered/STR-06-17-part3.svg)

View: STR-06-17-part3

![Sequence: submit RunDeterministicGradeCommand, part 4 of 4](c4/rendered/STR-06-17-part4.svg)

View: STR-06-17-part4

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 1 of 4: not found](c4/rendered/STR-06-17-alt1-1.svg)

View: STR-06-17-alt1-1

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 2 of 4: Rejected or Superseded](c4/rendered/STR-06-17-alt1-2.svg)

View: STR-06-17-alt1-2

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 1 of 9](c4/rendered/STR-06-17-alt1-3.svg)

View: STR-06-17-alt1-3

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 2 of 9](c4/rendered/STR-06-17-alt1-3-part2.svg)

View: STR-06-17-alt1-3-part2

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 3 of 9](c4/rendered/STR-06-17-alt1-3-part3.svg)

View: STR-06-17-alt1-3-part3

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 4 of 9](c4/rendered/STR-06-17-alt1-3-part4.svg)

View: STR-06-17-alt1-3-part4

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 5 of 9](c4/rendered/STR-06-17-alt1-3-part5.svg)

View: STR-06-17-alt1-3-part5

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 6 of 9](c4/rendered/STR-06-17-alt1-3-part6.svg)

View: STR-06-17-alt1-3-part6

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 7 of 9](c4/rendered/STR-06-17-alt1-3-part7.svg)

View: STR-06-17-alt1-3-part7

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 8 of 9](c4/rendered/STR-06-17-alt1-3-part8.svg)

View: STR-06-17-alt1-3-part8

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 3 of 4: gradable, part 9 of 9](c4/rendered/STR-06-17-alt1-3-part9.svg)

View: STR-06-17-alt1-3-part9

![Sequence: submit RunDeterministicGradeCommand, alt 2 branch 1 of 3: responseStatus Stale, part 1 of 2](c4/rendered/STR-06-17-alt2-1.svg)

View: STR-06-17-alt2-1

![Sequence: submit RunDeterministicGradeCommand, alt 2 branch 1 of 3: responseStatus Stale, part 2 of 2](c4/rendered/STR-06-17-alt2-1-part2.svg)

View: STR-06-17-alt2-1-part2

![Sequence: submit RunDeterministicGradeCommand, alt 2 branch 2 of 3: verdict Pass, responseStatus Grading, part 1 of 2](c4/rendered/STR-06-17-alt2-2.svg)

View: STR-06-17-alt2-2

![Sequence: submit RunDeterministicGradeCommand, alt 2 branch 2 of 3: verdict Pass, responseStatus Grading, part 2 of 2](c4/rendered/STR-06-17-alt2-2-part2.svg)

View: STR-06-17-alt2-2-part2

![Sequence: submit RunDeterministicGradeCommand, alt 2 branch 3 of 3: verdict Fail, part 1 of 2](c4/rendered/STR-06-17-alt2-3.svg)

View: STR-06-17-alt2-3

![Sequence: submit RunDeterministicGradeCommand, alt 2 branch 3 of 3: verdict Fail, part 2 of 2](c4/rendered/STR-06-17-alt2-3-part2.svg)

View: STR-06-17-alt2-3-part2

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 4 of 4: retries exhausted (here or in EvaluateStalenessCommand), part 1 of 2](c4/rendered/STR-06-17-alt1-4.svg)

View: STR-06-17-alt1-4

![Sequence: submit RunDeterministicGradeCommand, alt 1 branch 4 of 4: retries exhausted (here or in EvaluateStalenessCommand), part 2 of 2](c4/rendered/STR-06-17-alt1-4-part2.svg)

View: STR-06-17-alt1-4-part2

#### Sequence: submit RecordModelGradeCommand from an HV review

`Agent` is the agent that asks for the review; `Runner` is the existing HV runner; `Reviewer` is the HV agent (the configured grader, Codex `gpt-6-astra` `max`, one reviewer thread, in two steps). In step 1 the reviewer reviews the turns and responses, emits the VERDICT JSON with its `overallVerdict`, a `grades` entry per response carrying that response's `verdict`, and its own thread id, and stores its entire result on its own session-log turn; when that run completes, the runner freezes the request, response, and rollout jsonl receipts under the receipt root.

![Sequence: submit RecordModelGradeCommand from an HV review, part 1 of 8](c4/rendered/STR-06-18.svg)

View: STR-06-18

![Sequence: submit RecordModelGradeCommand from an HV review, part 2 of 8](c4/rendered/STR-06-18-part2.svg)

View: STR-06-18-part2

![Sequence: submit RecordModelGradeCommand from an HV review, part 3 of 8](c4/rendered/STR-06-18-part3.svg)

View: STR-06-18-part3

![Sequence: submit RecordModelGradeCommand from an HV review, part 4 of 8](c4/rendered/STR-06-18-part4.svg)

View: STR-06-18-part4

![Sequence: submit RecordModelGradeCommand from an HV review, part 5 of 8](c4/rendered/STR-06-18-part5.svg)

View: STR-06-18-part5

![Sequence: submit RecordModelGradeCommand from an HV review, part 6 of 8](c4/rendered/STR-06-18-part6.svg)

View: STR-06-18-part6

![Sequence: submit RecordModelGradeCommand from an HV review, part 7 of 8](c4/rendered/STR-06-18-part7.svg)

View: STR-06-18-part7

![Sequence: submit RecordModelGradeCommand from an HV review, part 8 of 8](c4/rendered/STR-06-18-part8.svg)

View: STR-06-18-part8

![Sequence: submit RecordModelGradeCommand from an HV review, loop 1: each response item of the HV request, part 1 of 2](c4/rendered/STR-06-18-loop1.svg)

View: STR-06-18-loop1

![Sequence: submit RecordModelGradeCommand from an HV review, loop 1: each response item of the HV request, part 2 of 2](c4/rendered/STR-06-18-loop1-part2.svg)

View: STR-06-18-loop1-part2

##### Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence)

The reviewer sends `client.AgentContext.RecordModelGradeAsync` through the REPL, whose lossless normalizer keeps `correctness` and `completeness` as their exact lexemes (`ScoreLexeme`, section 3.5.4), so `verdict_match` compares the verdict entry's own lexemes. `ScoreLexemeJsonConverter` refuses an invalid lexeme at deserialization, before the dispatch and before any lookup, with 400 `review_score_format`, `review_score_precision`, or `review_score_out_of_range`. The request carries `hvRequestId`, the scores and findings copied from the reviewer's `grades` entry, the receipt paths, the reviewer's turn ids, and `reviewerThreadId`. The model grade reads before it writes, in the order of section 3.1 ("Database-only delegates"): every read-only check (the response, the HV request and its item, and the eight receipt, reviewer, and runtime-identity checks), then the EvaluateStalenessCommand dispatch, then the SerializableWrite. Nested sequence ReadModelGradeInputs looks the stored grade up read-only, checks the response, the HV request, and its item, reads the receipt files and the reviewer's own turn for the eight checks of section 6.3, and only then dispatches EvaluateStalenessCommand for the response (the only writer of `Stale`, which evaluates outside any transaction); the handler then derives the verdict with `ScoreVerdict` at the 0.98 floor, in memory. The SerializableWrite then locks the response and reads and writes only the database: it looks the grade up again, which finds a concurrent continuation that stored it first; re-checks the response's `StatusVersion` and the HV request's status and item that the reads saw (a change ends the attempt, and the unit runs again from its reads); inserts the grade with its findings, receipt hashes, and the thresholds in force, stamps `RecordedUtc`, marks the item `Graded`, completes the HV request with `CompletedUtc` when no item is `Pending`, and dispatches DecideAgentResponseTrustCommand (not gated), whose decision reads the stored status and the latest grades; all of it commits together. The reviewer reports a `Grading` result's FAIL and UNKNOWN findings, or a `Stale` result's changed dependency, to the agent that requested the HV.

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence)](c4/rendered/SubmitOneModelGrade.svg)

View: SubmitOneModelGrade

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 1 branch 1 of 2: score lexeme invalid (ScoreLexemeJsonConverter)](c4/rendered/SubmitOneModelGrade-alt1-1.svg)

View: SubmitOneModelGrade-alt1-1

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 1 branch 2 of 2: lexemes valid, part 1 of 3](c4/rendered/SubmitOneModelGrade-alt1-2.svg)

View: SubmitOneModelGrade-alt1-2

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 1 branch 2 of 2: lexemes valid, part 2 of 3](c4/rendered/SubmitOneModelGrade-alt1-2-part2.svg)

View: SubmitOneModelGrade-alt1-2-part2

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 1 branch 2 of 2: lexemes valid, part 3 of 3](c4/rendered/SubmitOneModelGrade-alt1-2-part3.svg)

View: SubmitOneModelGrade-alt1-2-part3

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 1 of 4: hvRequestId, a jsonl path, a reviewer turn id, or reviewerThreadId missing, part 1 of 2](c4/rendered/SubmitOneModelGrade-alt2-1.svg)

View: SubmitOneModelGrade-alt2-1

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 1 of 4: hvRequestId, a jsonl path, a reviewer turn id, or reviewerThreadId missing, part 2 of 2](c4/rendered/SubmitOneModelGrade-alt2-1-part2.svg)

View: SubmitOneModelGrade-alt2-1-part2

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 2 of 4: findings invalid](c4/rendered/SubmitOneModelGrade-alt2-2.svg)

View: SubmitOneModelGrade-alt2-2

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 3 of 4: grader not the configured one](c4/rendered/SubmitOneModelGrade-alt2-3.svg)

View: SubmitOneModelGrade-alt2-3

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 4 of 4: request valid, part 1 of 5](c4/rendered/SubmitOneModelGrade-alt2-4.svg)

View: SubmitOneModelGrade-alt2-4

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 4 of 4: request valid, part 2 of 5](c4/rendered/SubmitOneModelGrade-alt2-4-part2.svg)

View: SubmitOneModelGrade-alt2-4-part2

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 4 of 4: request valid, part 3 of 5](c4/rendered/SubmitOneModelGrade-alt2-4-part3.svg)

View: SubmitOneModelGrade-alt2-4-part3

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 4 of 4: request valid, part 4 of 5](c4/rendered/SubmitOneModelGrade-alt2-4-part4.svg)

View: SubmitOneModelGrade-alt2-4-part4

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 2 branch 4 of 4: request valid, part 5 of 5](c4/rendered/SubmitOneModelGrade-alt2-4-part5.svg)

View: SubmitOneModelGrade-alt2-4-part5

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 3 branch 1 of 4: equal grade stored meanwhile](c4/rendered/SubmitOneModelGrade-alt3-1.svg)

View: SubmitOneModelGrade-alt3-1

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 3 branch 2 of 4: a different grade stored meanwhile](c4/rendered/SubmitOneModelGrade-alt3-2.svg)

View: SubmitOneModelGrade-alt3-2

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 3 branch 3 of 4: absent, part 1 of 3](c4/rendered/SubmitOneModelGrade-alt3-3.svg)

View: SubmitOneModelGrade-alt3-3

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 3 branch 3 of 4: absent, part 2 of 3](c4/rendered/SubmitOneModelGrade-alt3-3-part2.svg)

View: SubmitOneModelGrade-alt3-3-part2

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 3 branch 3 of 4: absent, part 3 of 3](c4/rendered/SubmitOneModelGrade-alt3-3-part3.svg)

View: SubmitOneModelGrade-alt3-3-part3

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 1 of 3: responseStatus Trusted, part 1 of 3](c4/rendered/SubmitOneModelGrade-alt4-1.svg)

View: SubmitOneModelGrade-alt4-1

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 1 of 3: responseStatus Trusted, part 2 of 3](c4/rendered/SubmitOneModelGrade-alt4-1-part2.svg)

View: SubmitOneModelGrade-alt4-1-part2

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 1 of 3: responseStatus Trusted, part 3 of 3](c4/rendered/SubmitOneModelGrade-alt4-1-part3.svg)

View: SubmitOneModelGrade-alt4-1-part3

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 2 of 3: responseStatus Grading, part 1 of 2](c4/rendered/SubmitOneModelGrade-alt4-2.svg)

View: SubmitOneModelGrade-alt4-2

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 2 of 3: responseStatus Grading, part 2 of 2](c4/rendered/SubmitOneModelGrade-alt4-2-part2.svg)

View: SubmitOneModelGrade-alt4-2-part2

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 3 of 3: responseStatus Stale, part 1 of 2](c4/rendered/SubmitOneModelGrade-alt4-3.svg)

View: SubmitOneModelGrade-alt4-3

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 4 branch 3 of 3: responseStatus Stale, part 2 of 2](c4/rendered/SubmitOneModelGrade-alt4-3-part2.svg)

View: SubmitOneModelGrade-alt4-3-part2

![Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence), alt 3 branch 4 of 4: retries exhausted](c4/rendered/SubmitOneModelGrade-alt3-4.svg)

View: SubmitOneModelGrade-alt3-4

##### Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction)

Every read here runs outside a transaction, in the order of the unit's checks (section 6.5): the stored grade (IdempotentCreate on the natural key `ResponseId`, `HvRequestId`, kind `Model`, policy `Conflict`, read-only, so a retry after the request completed or the response was rejected still returns the stored grade), the response, each finding's `claimId`, the HV request, the response's status, the HV request's status, the item, and the eight receipt, reviewer, and runtime-identity checks of section 6.3 over the receipt files and the reviewer's own session-log turn. A failed check is answered with `details.check`; the reviewer makes its submission equal its verdict entry and its turn, or reruns the HV on the configured grader. EvaluateStalenessCommand then persists `Stale` when a dependency of the response or its package changed, so the trust decision in the SerializableWrite never trusts it (a `Stale` response stays gradable, and the decision leaves it `Stale`); when that dispatch's own write exhausts its four attempts, the grade answers 409 `conflict` with `retryable: true` and saves nothing. The response's `StatusVersion` and the HV request's state that the SerializableWrite re-checks are the ones read after that dispatch.

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), part 1 of 2](c4/rendered/ReadModelGradeInputs-Sequence.svg)

View: ReadModelGradeInputs-Sequence

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), part 2 of 2](c4/rendered/ReadModelGradeInputs-Sequence-part2.svg)

View: ReadModelGradeInputs-Sequence-part2

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 1 branch 1 of 3: equal grade stored (a retried continuation)](c4/rendered/ReadModelGradeInputs-Sequence-alt1-1.svg)

View: ReadModelGradeInputs-Sequence-alt1-1

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 1 branch 2 of 3: a different grade stored](c4/rendered/ReadModelGradeInputs-Sequence-alt1-2.svg)

View: ReadModelGradeInputs-Sequence-alt1-2

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 1 branch 3 of 3: absent, part 1 of 2](c4/rendered/ReadModelGradeInputs-Sequence-alt1-3.svg)

View: ReadModelGradeInputs-Sequence-alt1-3

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 1 branch 3 of 3: absent, part 2 of 2](c4/rendered/ReadModelGradeInputs-Sequence-alt1-3-part2.svg)

View: ReadModelGradeInputs-Sequence-alt1-3-part2

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 1 of 7: response not found](c4/rendered/ReadModelGradeInputs-Sequence-alt2-1.svg)

View: ReadModelGradeInputs-Sequence-alt2-1

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 2 of 7: a finding names a claim the response does not have](c4/rendered/ReadModelGradeInputs-Sequence-alt2-2.svg)

View: ReadModelGradeInputs-Sequence-alt2-2

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 3 of 7: HV request not found](c4/rendered/ReadModelGradeInputs-Sequence-alt2-3.svg)

View: ReadModelGradeInputs-Sequence-alt2-3

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 4 of 7: response Rejected or Superseded](c4/rendered/ReadModelGradeInputs-Sequence-alt2-4.svg)

View: ReadModelGradeInputs-Sequence-alt2-4

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 5 of 7: HV request Completed](c4/rendered/ReadModelGradeInputs-Sequence-alt2-5.svg)

View: ReadModelGradeInputs-Sequence-alt2-5

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 6 of 7: response not an item of the HV request](c4/rendered/ReadModelGradeInputs-Sequence-alt2-6.svg)

View: ReadModelGradeInputs-Sequence-alt2-6

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 7 of 7: gradable item of an Open HV request, part 1 of 2](c4/rendered/ReadModelGradeInputs-Sequence-alt2-7.svg)

View: ReadModelGradeInputs-Sequence-alt2-7

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 2 branch 7 of 7: gradable item of an Open HV request, part 2 of 2](c4/rendered/ReadModelGradeInputs-Sequence-alt2-7-part2.svg)

View: ReadModelGradeInputs-Sequence-alt2-7-part2

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 3 branch 1 of 2: a check fails](c4/rendered/ReadModelGradeInputs-Sequence-alt3-1.svg)

View: ReadModelGradeInputs-Sequence-alt3-1

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 3 branch 2 of 2: all eight pass, part 1 of 2](c4/rendered/ReadModelGradeInputs-Sequence-alt3-2.svg)

View: ReadModelGradeInputs-Sequence-alt3-2

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 3 branch 2 of 2: all eight pass, part 2 of 2](c4/rendered/ReadModelGradeInputs-Sequence-alt3-2-part2.svg)

View: ReadModelGradeInputs-Sequence-alt3-2-part2

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 4 branch 1 of 2: its write exhausted the retries](c4/rendered/ReadModelGradeInputs-Sequence-alt4-1.svg)

View: ReadModelGradeInputs-Sequence-alt4-1

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 4 branch 2 of 2: evaluated: fresh, or Stale persisted, part 1 of 2](c4/rendered/ReadModelGradeInputs-Sequence-alt4-2.svg)

View: ReadModelGradeInputs-Sequence-alt4-2

![Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction), alt 4 branch 2 of 2: evaluated: fresh, or Stale persisted, part 2 of 2](c4/rendered/ReadModelGradeInputs-Sequence-alt4-2-part2.svg)

View: ReadModelGradeInputs-Sequence-alt4-2-part2

#### Sequence: submit RejectAgentResponseCommand

`ResponseStatusTransition` (section 3.5.5) applies the side effects in the same transaction: the projection row becomes `Pending` for removal when it holds a `GraphRagDocumentId` (otherwise `Removed`), the response's `Pending` items in `Open` HV requests become `Closed` (`rejected`), and each request left with no `Pending` item becomes `Completed` with `CompletedUtc`.

![Sequence: submit RejectAgentResponseCommand, part 1 of 3](c4/rendered/STR-06-19.svg)

View: STR-06-19

![Sequence: submit RejectAgentResponseCommand, part 2 of 3](c4/rendered/STR-06-19-part2.svg)

View: STR-06-19-part2

![Sequence: submit RejectAgentResponseCommand, part 3 of 3](c4/rendered/STR-06-19-part3.svg)

View: STR-06-19-part3

![Sequence: submit RejectAgentResponseCommand, alt 1 branch 1 of 2: blank reason](c4/rendered/STR-06-19-alt1-1.svg)

View: STR-06-19-alt1-1

![Sequence: submit RejectAgentResponseCommand, alt 1 branch 2 of 2: reason given, part 1 of 3](c4/rendered/STR-06-19-alt1-2.svg)

View: STR-06-19-alt1-2

![Sequence: submit RejectAgentResponseCommand, alt 1 branch 2 of 2: reason given, part 2 of 3](c4/rendered/STR-06-19-alt1-2-part2.svg)

View: STR-06-19-alt1-2-part2

![Sequence: submit RejectAgentResponseCommand, alt 1 branch 2 of 2: reason given, part 3 of 3](c4/rendered/STR-06-19-alt1-2-part3.svg)

View: STR-06-19-alt1-2-part3

![Sequence: submit RejectAgentResponseCommand, alt 2 branch 1 of 5: response not found](c4/rendered/STR-06-19-alt2-1.svg)

View: STR-06-19-alt2-1

![Sequence: submit RejectAgentResponseCommand, alt 2 branch 2 of 5: already Rejected (same transition)](c4/rendered/STR-06-19-alt2-2.svg)

View: STR-06-19-alt2-2

![Sequence: submit RejectAgentResponseCommand, alt 2 branch 3 of 5: Superseded (the lifecycle does not allow it)](c4/rendered/STR-06-19-alt2-3.svg)

View: STR-06-19-alt2-3

![Sequence: submit RejectAgentResponseCommand, alt 2 branch 4 of 5: transitioned](c4/rendered/STR-06-19-alt2-4.svg)

View: STR-06-19-alt2-4

![Sequence: submit RejectAgentResponseCommand, alt 2 branch 5 of 5: retries exhausted](c4/rendered/STR-06-19-alt2-5.svg)

View: STR-06-19-alt2-5

#### Sequence: submit SupersedeAgentResponseCommand

Both responses are locked in ascending `ResponseId` order. `ResponseStatusTransition` sets the original `Superseded` with its side effects (projection row, HV items `Closed` with `superseded`), and `RelationshipWriter` writes the `supersedes` link and checks the cycle in the same transaction, so the supersede chain has one data source (section 3.5.5).

![Sequence: submit SupersedeAgentResponseCommand, part 1 of 3](c4/rendered/STR-06-20.svg)

View: STR-06-20

![Sequence: submit SupersedeAgentResponseCommand, part 2 of 3](c4/rendered/STR-06-20-part2.svg)

View: STR-06-20-part2

![Sequence: submit SupersedeAgentResponseCommand, part 3 of 3](c4/rendered/STR-06-20-part3.svg)

View: STR-06-20-part3

![Sequence: submit SupersedeAgentResponseCommand, alt 1 branch 1 of 2: self-reference or blank reason](c4/rendered/STR-06-20-alt1-1.svg)

View: STR-06-20-alt1-1

![Sequence: submit SupersedeAgentResponseCommand, alt 1 branch 2 of 2: request valid, part 1 of 4](c4/rendered/STR-06-20-alt1-2.svg)

View: STR-06-20-alt1-2

![Sequence: submit SupersedeAgentResponseCommand, alt 1 branch 2 of 2: request valid, part 2 of 4](c4/rendered/STR-06-20-alt1-2-part2.svg)

View: STR-06-20-alt1-2-part2

![Sequence: submit SupersedeAgentResponseCommand, alt 1 branch 2 of 2: request valid, part 3 of 4](c4/rendered/STR-06-20-alt1-2-part3.svg)

View: STR-06-20-alt1-2-part3

![Sequence: submit SupersedeAgentResponseCommand, alt 1 branch 2 of 2: request valid, part 4 of 4](c4/rendered/STR-06-20-alt1-2-part4.svg)

View: STR-06-20-alt1-2-part4

![Sequence: submit SupersedeAgentResponseCommand, alt 2 branch 1 of 4: either not found](c4/rendered/STR-06-20-alt2-1.svg)

View: STR-06-20-alt2-1

![Sequence: submit SupersedeAgentResponseCommand, alt 2 branch 2 of 4: other request, or replacement Rejected or Superseded](c4/rendered/STR-06-20-alt2-2.svg)

View: STR-06-20-alt2-2

![Sequence: submit SupersedeAgentResponseCommand, alt 2 branch 3 of 4: both valid, part 1 of 2](c4/rendered/STR-06-20-alt2-3.svg)

View: STR-06-20-alt2-3

![Sequence: submit SupersedeAgentResponseCommand, alt 2 branch 3 of 4: both valid, part 2 of 2](c4/rendered/STR-06-20-alt2-3-part2.svg)

View: STR-06-20-alt2-3-part2

![Sequence: submit SupersedeAgentResponseCommand, alt 3 branch 1 of 3: already superseded by this replacement](c4/rendered/STR-06-20-alt3-1.svg)

View: STR-06-20-alt3-1

![Sequence: submit SupersedeAgentResponseCommand, alt 3 branch 2 of 3: original Rejected, or superseded by another response, part 1 of 2](c4/rendered/STR-06-20-alt3-2.svg)

View: STR-06-20-alt3-2

![Sequence: submit SupersedeAgentResponseCommand, alt 3 branch 2 of 3: original Rejected, or superseded by another response, part 2 of 2](c4/rendered/STR-06-20-alt3-2-part2.svg)

View: STR-06-20-alt3-2-part2

![Sequence: submit SupersedeAgentResponseCommand, alt 3 branch 3 of 3: transitioned, part 1 of 2](c4/rendered/STR-06-20-alt3-3.svg)

View: STR-06-20-alt3-3

![Sequence: submit SupersedeAgentResponseCommand, alt 3 branch 3 of 3: transitioned, part 2 of 2](c4/rendered/STR-06-20-alt3-3-part2.svg)

View: STR-06-20-alt3-3-part2

![Sequence: submit SupersedeAgentResponseCommand, alt 4 branch 1 of 2: the link would close a cycle](c4/rendered/STR-06-20-alt4-1.svg)

View: STR-06-20-alt4-1

![Sequence: submit SupersedeAgentResponseCommand, alt 4 branch 2 of 2: link written](c4/rendered/STR-06-20-alt4-2.svg)

View: STR-06-20-alt4-2

![Sequence: submit SupersedeAgentResponseCommand, alt 2 branch 4 of 4: retries exhausted](c4/rendered/STR-06-20-alt2-4.svg)

View: STR-06-20-alt2-4

#### Sequence: submit EvaluateStalenessCommand

This unit is the only writer of `Stale` (section 3.1, "Database-only delegates"). The two grade units dispatch it for their response after their read-only checks and before their own SerializableWrite, and ProjectOneResponse before its row saves (section 3.5.5, `IResponseStalenessRule`), so each reads the status it leaves; and the units whose commit moved a response out of `Trusted` or into `Rejected`, `Superseded`, or `Stale` (the grade units through the trust decision, RejectAgentResponseCommand, SupersedeAgentResponseCommand, and this unit) dispatch it after that commit with the `responseIds` of the responses that depend on that response, only while Mode is not `Off`, logging a failed dispatch without changing their own answer (section 3.1, Dependent responses). Each selected response is read first and written second (nested activity EvaluateOneResponseStaleness): `IResponseStalenessRule` reads its dependencies outside any transaction (files at the workspace HEAD commit; never the request's own turn or a Rule file outside version control), and a changed dependency persists `Stale` through `ResponseStatusTransition` in the response's own SerializableWrite, which locks it and re-checks the `StatusVersion` the read saw (a change ends the attempt, and that response is read again).

![Sequence: submit EvaluateStalenessCommand, part 1 of 4](c4/rendered/STR-06-21.svg)

View: STR-06-21

![Sequence: submit EvaluateStalenessCommand, part 2 of 4](c4/rendered/STR-06-21-part2.svg)

View: STR-06-21-part2

![Sequence: submit EvaluateStalenessCommand, part 3 of 4](c4/rendered/STR-06-21-part3.svg)

View: STR-06-21-part3

![Sequence: submit EvaluateStalenessCommand, part 4 of 4](c4/rendered/STR-06-21-part4.svg)

View: STR-06-21-part4

![Sequence: submit EvaluateStalenessCommand, alt 1 branch 1 of 2: malformed, over 1000 ids, or an id without rsp-](c4/rendered/STR-06-21-alt1-1.svg)

View: STR-06-21-alt1-1

![Sequence: submit EvaluateStalenessCommand, alt 1 branch 2 of 2: selected (an empty list selects every Submitted, Grading, Trusted), part 1 of 3](c4/rendered/STR-06-21-alt1-2.svg)

View: STR-06-21-alt1-2

![Sequence: submit EvaluateStalenessCommand, alt 1 branch 2 of 2: selected (an empty list selects every Submitted, Grading, Trusted), part 2 of 3](c4/rendered/STR-06-21-alt1-2-part2.svg)

View: STR-06-21-alt1-2-part2

![Sequence: submit EvaluateStalenessCommand, alt 1 branch 2 of 2: selected (an empty list selects every Submitted, Grading, Trusted), part 3 of 3](c4/rendered/STR-06-21-alt1-2-part3.svg)

View: STR-06-21-alt1-2-part3

![Sequence: submit EvaluateStalenessCommand, loop 1: each selected response, part 1 of 3](c4/rendered/STR-06-21-loop1.svg)

View: STR-06-21-loop1

![Sequence: submit EvaluateStalenessCommand, loop 1: each selected response, part 2 of 3](c4/rendered/STR-06-21-loop1-part2.svg)

View: STR-06-21-loop1-part2

![Sequence: submit EvaluateStalenessCommand, loop 1: each selected response, part 3 of 3](c4/rendered/STR-06-21-loop1-part3.svg)

View: STR-06-21-loop1-part3

![Sequence: submit EvaluateStalenessCommand, alt 2 branch 1 of 2: every response evaluated, part 1 of 4](c4/rendered/STR-06-21-alt2-1.svg)

View: STR-06-21-alt2-1

![Sequence: submit EvaluateStalenessCommand, alt 2 branch 1 of 2: every response evaluated, part 2 of 4](c4/rendered/STR-06-21-alt2-1-part2.svg)

View: STR-06-21-alt2-1-part2

![Sequence: submit EvaluateStalenessCommand, alt 2 branch 1 of 2: every response evaluated, part 3 of 4](c4/rendered/STR-06-21-alt2-1-part3.svg)

View: STR-06-21-alt2-1-part3

![Sequence: submit EvaluateStalenessCommand, alt 2 branch 1 of 2: every response evaluated, part 4 of 4](c4/rendered/STR-06-21-alt2-1-part4.svg)

View: STR-06-21-alt2-1-part4

![Sequence: submit EvaluateStalenessCommand, alt 2 branch 2 of 2: a response's write exhausted its retries](c4/rendered/STR-06-21-alt2-2.svg)

View: STR-06-21-alt2-2

#### Sequence: submit CreateAgentContextRelationshipCommand

`SourceResolver` resolves both refs and hashes the target before the SerializableWrite, whose delegate reads and writes only the database (section 3.1, "Database-only delegates"). `RelationshipWriter` (section 3.5.5) records that hash as `ToSha256` and `CreatedBy` (the calling agent), returns an identical link as stored (IdempotentCreate with policy `ReturnStored`), and refuses a client's `supersedes` link; a client therefore never reaches the writer's cycle check, which only SupersedeAgentResponseCommand uses.

![Sequence: submit CreateAgentContextRelationshipCommand, part 1 of 3](c4/rendered/STR-06-22.svg)

View: STR-06-22

![Sequence: submit CreateAgentContextRelationshipCommand, part 2 of 3](c4/rendered/STR-06-22-part2.svg)

View: STR-06-22-part2

![Sequence: submit CreateAgentContextRelationshipCommand, part 3 of 3](c4/rendered/STR-06-22-part3.svg)

View: STR-06-22-part3

![Sequence: submit CreateAgentContextRelationshipCommand, alt 1 branch 1 of 2: unknown kind or type, or a self-link](c4/rendered/STR-06-22-alt1-1.svg)

View: STR-06-22-alt1-1

![Sequence: submit CreateAgentContextRelationshipCommand, alt 1 branch 2 of 2: link well formed, part 1 of 2](c4/rendered/STR-06-22-alt1-2.svg)

View: STR-06-22-alt1-2

![Sequence: submit CreateAgentContextRelationshipCommand, alt 1 branch 2 of 2: link well formed, part 2 of 2](c4/rendered/STR-06-22-alt1-2-part2.svg)

View: STR-06-22-alt1-2-part2

![Sequence: submit CreateAgentContextRelationshipCommand, alt 2 branch 1 of 2: a ref names no existing source](c4/rendered/STR-06-22-alt2-1.svg)

View: STR-06-22-alt2-1

![Sequence: submit CreateAgentContextRelationshipCommand, alt 2 branch 2 of 2: both refs resolve, part 1 of 3](c4/rendered/STR-06-22-alt2-2.svg)

View: STR-06-22-alt2-2

![Sequence: submit CreateAgentContextRelationshipCommand, alt 2 branch 2 of 2: both refs resolve, part 2 of 3](c4/rendered/STR-06-22-alt2-2-part2.svg)

View: STR-06-22-alt2-2-part2

![Sequence: submit CreateAgentContextRelationshipCommand, alt 2 branch 2 of 2: both refs resolve, part 3 of 3](c4/rendered/STR-06-22-alt2-2-part3.svg)

View: STR-06-22-alt2-2-part3

![Sequence: submit CreateAgentContextRelationshipCommand, alt 3 branch 1 of 4: type supersedes](c4/rendered/STR-06-22-alt3-1.svg)

View: STR-06-22-alt3-1

![Sequence: submit CreateAgentContextRelationshipCommand, alt 3 branch 2 of 4: identical link stored](c4/rendered/STR-06-22-alt3-2.svg)

View: STR-06-22-alt3-2

![Sequence: submit CreateAgentContextRelationshipCommand, alt 3 branch 3 of 4: new link](c4/rendered/STR-06-22-alt3-3.svg)

View: STR-06-22-alt3-3

![Sequence: submit CreateAgentContextRelationshipCommand, alt 3 branch 4 of 4: retries exhausted](c4/rendered/STR-06-22-alt3-4.svg)

View: STR-06-22-alt3-4

#### Sequence: submit DeleteAgentContextRelationshipCommand

A link a command created is protected because the staleness rule reads it or it holds the supersede chain: Build's `derived_from` and `relevant_to`, Submit's `derived_from` and `supports`, and Supersede's `supersedes`.

![Sequence: submit DeleteAgentContextRelationshipCommand, part 1 of 4](c4/rendered/STR-06-23.svg)

View: STR-06-23

![Sequence: submit DeleteAgentContextRelationshipCommand, part 2 of 4](c4/rendered/STR-06-23-part2.svg)

View: STR-06-23-part2

![Sequence: submit DeleteAgentContextRelationshipCommand, part 3 of 4](c4/rendered/STR-06-23-part3.svg)

View: STR-06-23-part3

![Sequence: submit DeleteAgentContextRelationshipCommand, part 4 of 4](c4/rendered/STR-06-23-part4.svg)

View: STR-06-23-part4

![Sequence: submit DeleteAgentContextRelationshipCommand, alt 1 branch 1 of 3: not found](c4/rendered/STR-06-23-alt1-1.svg)

View: STR-06-23-alt1-1

![Sequence: submit DeleteAgentContextRelationshipCommand, alt 1 branch 2 of 3: made by a command (CreatedBy names it)](c4/rendered/STR-06-23-alt1-2.svg)

View: STR-06-23-alt1-2

![Sequence: submit DeleteAgentContextRelationshipCommand, alt 1 branch 3 of 3: made by a client, part 1 of 2](c4/rendered/STR-06-23-alt1-3.svg)

View: STR-06-23-alt1-3

![Sequence: submit DeleteAgentContextRelationshipCommand, alt 1 branch 3 of 3: made by a client, part 2 of 2](c4/rendered/STR-06-23-alt1-3-part2.svg)

View: STR-06-23-alt1-3-part2

#### Sequence: submit QueryAgentContextRelationshipsQuery

![Sequence: submit QueryAgentContextRelationshipsQuery, part 1 of 4](c4/rendered/STR-06-24.svg)

View: STR-06-24

![Sequence: submit QueryAgentContextRelationshipsQuery, part 2 of 4](c4/rendered/STR-06-24-part2.svg)

View: STR-06-24-part2

![Sequence: submit QueryAgentContextRelationshipsQuery, part 3 of 4](c4/rendered/STR-06-24-part3.svg)

View: STR-06-24-part3

![Sequence: submit QueryAgentContextRelationshipsQuery, part 4 of 4](c4/rendered/STR-06-24-part4.svg)

View: STR-06-24-part4

![Sequence: submit QueryAgentContextRelationshipsQuery, alt 1 branch 1 of 2: kind without ref, unknown type or direction, or outside PageBounds, part 1 of 2](c4/rendered/STR-06-24-alt1-1.svg)

View: STR-06-24-alt1-1

![Sequence: submit QueryAgentContextRelationshipsQuery, alt 1 branch 1 of 2: kind without ref, unknown type or direction, or outside PageBounds, part 2 of 2](c4/rendered/STR-06-24-alt1-1-part2.svg)

View: STR-06-24-alt1-1-part2

![Sequence: submit QueryAgentContextRelationshipsQuery, alt 1 branch 2 of 2: filters valid, part 1 of 2](c4/rendered/STR-06-24-alt1-2.svg)

View: STR-06-24-alt1-2

![Sequence: submit QueryAgentContextRelationshipsQuery, alt 1 branch 2 of 2: filters valid, part 2 of 2](c4/rendered/STR-06-24-alt1-2-part2.svg)

View: STR-06-24-alt1-2-part2

#### Sequence: submit RetrieveAgentContextCandidatesQuery

`Caller` is a surface, or BuildAgentContextPackageCommand's handler through `QueryAsync` in nested sequence ReadBuildInputs, before any transaction. The request carries `queryText`, `agent`, `sessionId`, `requestId`, `focusRefs`, `kinds`, `limit`, `trustedOnly`, and `preferGraphRag`. A named turn is `agent`, `sessionId`, and `requestId`, all three or none, and is never itself a candidate. Nested sequence ChooseRetrievalPath gathers the candidates. Every `PriorResponse` candidate is then evaluated by `IResponseStalenessRule` read-only, so only `Trusted` and fresh responses are kept and nothing is written; `CandidateRanker` (section 3.5.5) orders by score, then `sourceRef`, and the result is cut to `limit`. The same inputs give the same candidates on the relational path.

![Sequence: submit RetrieveAgentContextCandidatesQuery, part 1 of 4](c4/rendered/STR-06-25.svg)

View: STR-06-25

![Sequence: submit RetrieveAgentContextCandidatesQuery, part 2 of 4](c4/rendered/STR-06-25-part2.svg)

View: STR-06-25-part2

![Sequence: submit RetrieveAgentContextCandidatesQuery, part 3 of 4](c4/rendered/STR-06-25-part3.svg)

View: STR-06-25-part3

![Sequence: submit RetrieveAgentContextCandidatesQuery, part 4 of 4](c4/rendered/STR-06-25-part4.svg)

View: STR-06-25-part4

![Sequence: submit RetrieveAgentContextCandidatesQuery, alt 1 branch 1 of 2: trustedOnly false for PriorResponse, partial turn key, or limit outside PageBounds, part 1 of 2](c4/rendered/STR-06-25-alt1-1.svg)

View: STR-06-25-alt1-1

![Sequence: submit RetrieveAgentContextCandidatesQuery, alt 1 branch 1 of 2: trustedOnly false for PriorResponse, partial turn key, or limit outside PageBounds, part 2 of 2](c4/rendered/STR-06-25-alt1-1-part2.svg)

View: STR-06-25-alt1-1-part2

![Sequence: submit RetrieveAgentContextCandidatesQuery, alt 1 branch 2 of 2: request valid, part 1 of 2](c4/rendered/STR-06-25-alt1-2.svg)

View: STR-06-25-alt1-2

![Sequence: submit RetrieveAgentContextCandidatesQuery, alt 1 branch 2 of 2: request valid, part 2 of 2](c4/rendered/STR-06-25-alt1-2-part2.svg)

View: STR-06-25-alt1-2-part2

![Sequence: submit RetrieveAgentContextCandidatesQuery, opt 1: turn named](c4/rendered/STR-06-25-opt1.svg)

View: STR-06-25-opt1

![Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 1 of 3: InvalidIdentifier](c4/rendered/STR-06-25-alt2-1.svg)

View: STR-06-25-alt2-1

![Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 2 of 3: SessionNotFound or TurnNotFound](c4/rendered/STR-06-25-alt2-2.svg)

View: STR-06-25-alt2-2

![Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 3 of 3: no turn named, or the turn found, part 1 of 6](c4/rendered/STR-06-25-alt2-3.svg)

View: STR-06-25-alt2-3

![Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 3 of 3: no turn named, or the turn found, part 2 of 6](c4/rendered/STR-06-25-alt2-3-part2.svg)

View: STR-06-25-alt2-3-part2

![Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 3 of 3: no turn named, or the turn found, part 3 of 6](c4/rendered/STR-06-25-alt2-3-part3.svg)

View: STR-06-25-alt2-3-part3

![Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 3 of 3: no turn named, or the turn found, part 4 of 6](c4/rendered/STR-06-25-alt2-3-part4.svg)

View: STR-06-25-alt2-3-part4

![Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 3 of 3: no turn named, or the turn found, part 5 of 6](c4/rendered/STR-06-25-alt2-3-part5.svg)

View: STR-06-25-alt2-3-part5

![Sequence: submit RetrieveAgentContextCandidatesQuery, alt 2 branch 3 of 3: no turn named, or the turn found, part 6 of 6](c4/rendered/STR-06-25-alt2-3-part6.svg)

View: STR-06-25-alt2-3-part6

##### Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence)

The GraphRAG path answers only the `PriorResponse` kind, because GraphRAG holds only projected responses, and queries only `GraphRagStorageScope.Workspace`; it keeps hits that name a response of this workspace, one hit per response, scored by the GraphRAG score. `preferGraphRag` false takes the relational path with no `fallbackReason`. Every fallback takes the relational path and reports its `fallbackReason`: `retrieval_disabled` (`GraphRag:UseForRetrieval` false), `graphrag_kinds_unsupported` (`kinds` absent or naming another kind), `graphrag_disabled`, `graphrag_not_initialized`, `graphrag_not_indexed`, or `index_in_progress` (from the status), and the query result's reason, or `graphrag_error` on an exception. The relational path ranks by the links to the named turn, its TODO, its requirements, and the focus refs first, then by the hybrid search.

![Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), part 1 of 3](c4/rendered/ChooseRetrievalPath.svg)

View: ChooseRetrievalPath

![Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), part 2 of 3](c4/rendered/ChooseRetrievalPath-part2.svg)

View: ChooseRetrievalPath-part2

![Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), part 3 of 3](c4/rendered/ChooseRetrievalPath-part3.svg)

View: ChooseRetrievalPath-part3

![Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), opt 1: preferGraphRag, GraphRag:UseForRetrieval, and kinds only PriorResponse](c4/rendered/ChooseRetrievalPath-opt1.svg)

View: ChooseRetrievalPath-opt1

![Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), alt 1 branch 1 of 2: GraphRAG enabled and indexed](c4/rendered/ChooseRetrievalPath-alt1-1.svg)

View: ChooseRetrievalPath-alt1-1

![Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), alt 2 branch 1 of 2: result ok](c4/rendered/ChooseRetrievalPath-alt2-1.svg)

View: ChooseRetrievalPath-alt2-1

![Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), alt 2 branch 2 of 2: FallbackUsed or an exception, part 1 of 2](c4/rendered/ChooseRetrievalPath-alt2-2.svg)

View: ChooseRetrievalPath-alt2-2

![Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), alt 2 branch 2 of 2: FallbackUsed or an exception, part 2 of 2](c4/rendered/ChooseRetrievalPath-alt2-2-part2.svg)

View: ChooseRetrievalPath-alt2-2-part2

![Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), alt 1 branch 2 of 2: not preferred, or a fallbackReason from the request or the status, part 1 of 2](c4/rendered/ChooseRetrievalPath-alt1-2.svg)

View: ChooseRetrievalPath-alt1-2

![Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence), alt 1 branch 2 of 2: not preferred, or a fallbackReason from the request or the status, part 2 of 2](c4/rendered/ChooseRetrievalPath-alt1-2-part2.svg)

View: ChooseRetrievalPath-alt1-2-part2

#### Sequence: submit ProjectAgentResponsesCommand

`Caller` is a surface, or the background pass that runs after a projection row is marked `Pending`: the unit whose transaction marked the row dispatches the same command with no `responseIds` through `IDispatcher`, without the controller, after its SerializableWrite commits, and logs a failed dispatch without failing (section 3.1, Projection pass). The selection includes `Failed` rows, so a failed ingest or a failed delete is retried on the next pass; the projection row states are the state diagram of section 6.3.

![Sequence: submit ProjectAgentResponsesCommand, part 1 of 4](c4/rendered/STR-06-26.svg)

View: STR-06-26

![Sequence: submit ProjectAgentResponsesCommand, part 2 of 4](c4/rendered/STR-06-26-part2.svg)

View: STR-06-26-part2

![Sequence: submit ProjectAgentResponsesCommand, part 3 of 4](c4/rendered/STR-06-26-part3.svg)

View: STR-06-26-part3

![Sequence: submit ProjectAgentResponsesCommand, part 4 of 4](c4/rendered/STR-06-26-part4.svg)

View: STR-06-26-part4

![Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 1 of 2: malformed, over 1000 ids, or an id without rsp-](c4/rendered/STR-06-26-alt1-1.svg)

View: STR-06-26-alt1-1

![Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 1 of 8](c4/rendered/STR-06-26-alt1-2.svg)

View: STR-06-26-alt1-2

![Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 2 of 8](c4/rendered/STR-06-26-alt1-2-part2.svg)

View: STR-06-26-alt1-2-part2

![Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 3 of 8](c4/rendered/STR-06-26-alt1-2-part3.svg)

View: STR-06-26-alt1-2-part3

![Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 4 of 8](c4/rendered/STR-06-26-alt1-2-part4.svg)

View: STR-06-26-alt1-2-part4

![Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 5 of 8](c4/rendered/STR-06-26-alt1-2-part5.svg)

View: STR-06-26-alt1-2-part5

![Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 6 of 8](c4/rendered/STR-06-26-alt1-2-part6.svg)

View: STR-06-26-alt1-2-part6

![Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 7 of 8](c4/rendered/STR-06-26-alt1-2-part7.svg)

View: STR-06-26-alt1-2-part7

![Sequence: submit ProjectAgentResponsesCommand, alt 1 branch 2 of 2: selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled), part 8 of 8](c4/rendered/STR-06-26-alt1-2-part8.svg)

View: STR-06-26-alt1-2-part8

![Sequence: submit ProjectAgentResponsesCommand, loop 1: each selected response, part 1 of 2](c4/rendered/STR-06-26-loop1.svg)

View: STR-06-26-loop1

![Sequence: submit ProjectAgentResponsesCommand, loop 1: each selected response, part 2 of 2](c4/rendered/STR-06-26-loop1-part2.svg)

View: STR-06-26-loop1-part2

![Sequence: submit ProjectAgentResponsesCommand, alt 2 branch 1 of 2: every response handled](c4/rendered/STR-06-26-alt2-1.svg)

View: STR-06-26-alt2-1

![Sequence: submit ProjectAgentResponsesCommand, alt 2 branch 2 of 2: a row write exhausted its retries](c4/rendered/STR-06-26-alt2-2.svg)

View: STR-06-26-alt2-2

##### Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence)

Staleness belongs to ResponsesAndTrust, so the projection (ContextGraph) dispatches EvaluateStalenessCommand for the response first. The GraphRAG calls run outside any transaction; every row save runs afterwards in SerializableWrite with the response locked, reads and writes only the database, and re-reads the response status there (section 3.1, "Database-only delegates"). `ProjectionEnabled` false skips ingest only; a removal still runs while GraphRAG is enabled, and while GraphRAG is disabled a row awaiting removal stays `Pending`.

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), part 1 of 3](c4/rendered/ProjectOneResponse-Sequence.svg)

View: ProjectOneResponse-Sequence

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), part 2 of 3](c4/rendered/ProjectOneResponse-Sequence-part2.svg)

View: ProjectOneResponse-Sequence-part2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), part 3 of 3](c4/rendered/ProjectOneResponse-Sequence-part3.svg)

View: ProjectOneResponse-Sequence-part3

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 1 of 4: Trusted, not projected at its ContentSha256, part 1 of 2](c4/rendered/ProjectOneResponse-Sequence-alt1-1.svg)

View: ProjectOneResponse-Sequence-alt1-1

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 1 of 4: Trusted, not projected at its ContentSha256, part 2 of 2](c4/rendered/ProjectOneResponse-Sequence-alt1-1-part2.svg)

View: ProjectOneResponse-Sequence-alt1-1-part2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 2 branch 1 of 2: ProjectionEnabled and GraphRAG enabled, part 1 of 3](c4/rendered/ProjectOneResponse-Sequence-alt2-1.svg)

View: ProjectOneResponse-Sequence-alt2-1

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 2 branch 1 of 2: ProjectionEnabled and GraphRAG enabled, part 2 of 3](c4/rendered/ProjectOneResponse-Sequence-alt2-1-part2.svg)

View: ProjectOneResponse-Sequence-alt2-1-part2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 2 branch 1 of 2: ProjectionEnabled and GraphRAG enabled, part 3 of 3](c4/rendered/ProjectOneResponse-Sequence-alt2-1-part3.svg)

View: ProjectOneResponse-Sequence-alt2-1-part3

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 1 of 4: still Trusted, ingest ok, part 1 of 4](c4/rendered/ProjectOneResponse-Sequence-alt3-1.svg)

View: ProjectOneResponse-Sequence-alt3-1

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 1 of 4: still Trusted, ingest ok, part 2 of 4](c4/rendered/ProjectOneResponse-Sequence-alt3-1-part2.svg)

View: ProjectOneResponse-Sequence-alt3-1-part2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 1 of 4: still Trusted, ingest ok, part 3 of 4](c4/rendered/ProjectOneResponse-Sequence-alt3-1-part3.svg)

View: ProjectOneResponse-Sequence-alt3-1-part3

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 1 of 4: still Trusted, ingest ok, part 4 of 4](c4/rendered/ProjectOneResponse-Sequence-alt3-1-part4.svg)

View: ProjectOneResponse-Sequence-alt3-1-part4

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 2 of 4: row already Projected by an overlapping pass, same content, part 1 of 6](c4/rendered/ProjectOneResponse-Sequence-alt3-2.svg)

View: ProjectOneResponse-Sequence-alt3-2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 2 of 4: row already Projected by an overlapping pass, same content, part 2 of 6](c4/rendered/ProjectOneResponse-Sequence-alt3-2-part2.svg)

View: ProjectOneResponse-Sequence-alt3-2-part2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 2 of 4: row already Projected by an overlapping pass, same content, part 3 of 6](c4/rendered/ProjectOneResponse-Sequence-alt3-2-part3.svg)

View: ProjectOneResponse-Sequence-alt3-2-part3

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 2 of 4: row already Projected by an overlapping pass, same content, part 4 of 6](c4/rendered/ProjectOneResponse-Sequence-alt3-2-part4.svg)

View: ProjectOneResponse-Sequence-alt3-2-part4

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 2 of 4: row already Projected by an overlapping pass, same content, part 5 of 6](c4/rendered/ProjectOneResponse-Sequence-alt3-2-part5.svg)

View: ProjectOneResponse-Sequence-alt3-2-part5

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 2 of 4: row already Projected by an overlapping pass, same content, part 6 of 6](c4/rendered/ProjectOneResponse-Sequence-alt3-2-part6.svg)

View: ProjectOneResponse-Sequence-alt3-2-part6

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 3 of 4: no longer Trusted, ingest ok, part 1 of 4](c4/rendered/ProjectOneResponse-Sequence-alt3-3.svg)

View: ProjectOneResponse-Sequence-alt3-3

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 3 of 4: no longer Trusted, ingest ok, part 2 of 4](c4/rendered/ProjectOneResponse-Sequence-alt3-3-part2.svg)

View: ProjectOneResponse-Sequence-alt3-3-part2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 3 of 4: no longer Trusted, ingest ok, part 3 of 4](c4/rendered/ProjectOneResponse-Sequence-alt3-3-part3.svg)

View: ProjectOneResponse-Sequence-alt3-3-part3

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 3 of 4: no longer Trusted, ingest ok, part 4 of 4](c4/rendered/ProjectOneResponse-Sequence-alt3-3-part4.svg)

View: ProjectOneResponse-Sequence-alt3-3-part4

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 4 of 4: ingest error, part 1 of 2](c4/rendered/ProjectOneResponse-Sequence-alt3-4.svg)

View: ProjectOneResponse-Sequence-alt3-4

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 3 branch 4 of 4: ingest error, part 2 of 2](c4/rendered/ProjectOneResponse-Sequence-alt3-4-part2.svg)

View: ProjectOneResponse-Sequence-alt3-4-part2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 2 branch 2 of 2: ProjectionEnabled false, or GraphRAG disabled](c4/rendered/ProjectOneResponse-Sequence-alt2-2.svg)

View: ProjectOneResponse-Sequence-alt2-2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 2 of 4: not Trusted, the row holds a GraphRagDocumentId, part 1 of 2](c4/rendered/ProjectOneResponse-Sequence-alt1-2.svg)

View: ProjectOneResponse-Sequence-alt1-2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 2 of 4: not Trusted, the row holds a GraphRagDocumentId, part 2 of 2](c4/rendered/ProjectOneResponse-Sequence-alt1-2-part2.svg)

View: ProjectOneResponse-Sequence-alt1-2-part2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 4 branch 1 of 2: GraphRAG enabled, part 1 of 2](c4/rendered/ProjectOneResponse-Sequence-alt4-1.svg)

View: ProjectOneResponse-Sequence-alt4-1

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 4 branch 1 of 2: GraphRAG enabled, part 2 of 2](c4/rendered/ProjectOneResponse-Sequence-alt4-1-part2.svg)

View: ProjectOneResponse-Sequence-alt4-1-part2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 4 branch 2 of 2: GraphRAG disabled, part 1 of 4](c4/rendered/ProjectOneResponse-Sequence-alt4-2.svg)

View: ProjectOneResponse-Sequence-alt4-2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 4 branch 2 of 2: GraphRAG disabled, part 2 of 4](c4/rendered/ProjectOneResponse-Sequence-alt4-2-part2.svg)

View: ProjectOneResponse-Sequence-alt4-2-part2

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 4 branch 2 of 2: GraphRAG disabled, part 3 of 4](c4/rendered/ProjectOneResponse-Sequence-alt4-2-part3.svg)

View: ProjectOneResponse-Sequence-alt4-2-part3

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 4 branch 2 of 2: GraphRAG disabled, part 4 of 4](c4/rendered/ProjectOneResponse-Sequence-alt4-2-part4.svg)

View: ProjectOneResponse-Sequence-alt4-2-part4

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 3 of 4: not Trusted, no document](c4/rendered/ProjectOneResponse-Sequence-alt1-3.svg)

View: ProjectOneResponse-Sequence-alt1-3

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 4 of 4: nothing to change, part 1 of 2](c4/rendered/ProjectOneResponse-Sequence-alt1-4.svg)

View: ProjectOneResponse-Sequence-alt1-4

![Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence), alt 1 branch 4 of 4: nothing to change, part 2 of 2](c4/rendered/ProjectOneResponse-Sequence-alt1-4-part2.svg)

View: ProjectOneResponse-Sequence-alt1-4-part2

#### Sequence: stateless invocation end to end (process RunStatelessInvocation)

`Host` is the agent host, `Plugin` its PowerShell plugin core (`Invoke-PluginAgentContextInvocation` in `plugins/core/lib-ps/repl-invoke.ps1`, the dot-sourceable library of section 3.5.6, which the prompt hook's `plugin-hook.ps1` dot-sources), `Server` the QBrain.AI server reached through the REPL (the session-log and agent-context units), and `Model` the host's model call. The main sequence resolves the turn and calls two nested sequences, BuildAndDeliverPackage and SubmitAndGradeResponse. The decisions of section 3.1 (2026-10-12) apply:

- Mode: read once at host session start (SessionStart, or the first prompt on cline, cline-v2, and opencode) with GetAgentContextStatusQuery and cached as `agentContextMode` in the host session's `turn-state.yaml`; `Off` when it cannot be read, including a read that misses the hook bound (GetAgentContextStatusQuery sequence above). A 409 `agent_context_disabled` from the build or the submission caches `Off` and continues statefully (no refusal, no `canceled` close); every other build error fails the invocation, including `transaction_degraded` and `mutation_not_transactional` from the REPL's transaction-gated client policy (section 3.1, "Transaction-gated REPL clients").
- Bridge hosts (claude-code, claude-cowork): the prompt hook applies pending transcript records through `Sync-PluginArrivalLog` up to the transcript length at its own start and takes the bridge-minted `requestId` of the delivered request; when that request's `enqueue` record is not yet in the transcript, the invocation fails (prompt refused, as for a build failure).
- A message absorbed into a running turn (`absorbed_mid_turn`) runs as `Pilot`: the package is built and kept, never injected, and the prompt is never refused.
- A pending mint (transport outage) fails the invocation like a build outage, and the `canceled` close of the pending id is queued in the failsafe behind its mint record; only a host session reported `turn_logging_unsupported` (D6) stays stateful, and an `unsupported` mint outcome or a `merged` one with a terminal turn runs nothing. A final mint refusal leaves no turn: the prompt is refused and the Stop gate warns the refusal, naming the prompt; a hook that mints nothing (`nonce_source_missing`, `turn-open-contended`) is refused the same way. A duplicate delivery (`merged` for an in-progress turn, `turn-already-open`) runs the build again with the same idempotency key and client inputs, which returns the stored package and never refuses (section 6.6, `StatelessInvocationDuplicateDeliverySecondBuildReturnsStoredPackageNoSpuriousRefusal`).
- A host whose prompt hook cannot refuse a prompt, or that runs the prompt anyway after a hook timeout, crash, or `plugin_core_unavailable`, runs `Pilot` and `Stateless` as `Off` (feature-matrix limitation, D7).
- The plugin keeps the request's `packageId` in its per-turn file `turns/<requestId>.yaml`; the session-log turn gains no field. The model submits its response with the `agent_context_submit_response` tool before it closes the turn, and the plugin supplies the kept `packageId` (found again with QueryAgentContextPackagesQuery `{ agent, sessionId, requestId, status: Built }` when a hook process boundary lost it). A missing submission is not a Stop-gate item; the pilot report counts requests with a package and no response.
- The package-failure `canceled` close is written by the plugin, not the model, on the persist path of process PersistTurnWriteWithReceipt with status `canceled` and the failure reason; `Test-ModelEvidence` never applies to it. An outage or any other retryable answer (409 `conflict` with `retryable: true`, or a busy store's 503 `persistence_error`; section 3.1, "Retryable answers") holds it in the failsafe (receipt `queued`), and the Stop gate warns degraded persistence naming the package failure (never blocks), keeping the ledger entry until the drain confirms the close.
- History exclusion and memories (FR-MCP-MEMORY-003, -007): in `Stateless` the model's input is the rendered package, the current request, and the deterministic `REQUIRED MEMORIES` block the host already injects; history stays excluded, and within one request's tool loop the request's own prompt, tool calls, and results stay on each later model call (`StatelessToolLoopOwnToolCallsKeptPriorRequestsExcluded`). The message list the host passes to its model call (for opencode, after the adapter applies the core's `output.messages`), captured by the host test harness, is the observation point of `StatelessHostSentinelPriorTurnAbsentFromModelRequest`, which finds the sentinel prior turn absent and the package, the current request, and the `REQUIRED MEMORIES` block present.

![Sequence: stateless invocation end to end (process RunStatelessInvocation), part 1 of 4](c4/rendered/STR-06-27.svg)

View: STR-06-27

![Sequence: stateless invocation end to end (process RunStatelessInvocation), part 2 of 4](c4/rendered/STR-06-27-part2.svg)

View: STR-06-27-part2

![Sequence: stateless invocation end to end (process RunStatelessInvocation), part 3 of 4](c4/rendered/STR-06-27-part3.svg)

View: STR-06-27-part3

![Sequence: stateless invocation end to end (process RunStatelessInvocation), part 4 of 4](c4/rendered/STR-06-27-part4.svg)

View: STR-06-27-part4

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 1 branch 1 of 2: bridge host (claude-code, claude-cowork), part 1 of 3](c4/rendered/STR-06-27-alt1-1.svg)

View: STR-06-27-alt1-1

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 1 branch 1 of 2: bridge host (claude-code, claude-cowork), part 2 of 3](c4/rendered/STR-06-27-alt1-1-part2.svg)

View: STR-06-27-alt1-1-part2

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 1 branch 1 of 2: bridge host (claude-code, claude-cowork), part 3 of 3](c4/rendered/STR-06-27-alt1-1-part3.svg)

View: STR-06-27-alt1-1-part3

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 1 branch 2 of 2: other hosts, part 1 of 3](c4/rendered/STR-06-27-alt1-2.svg)

View: STR-06-27-alt1-2

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 1 branch 2 of 2: other hosts, part 2 of 3](c4/rendered/STR-06-27-alt1-2-part2.svg)

View: STR-06-27-alt1-2-part2

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 1 branch 2 of 2: other hosts, part 3 of 3](c4/rendered/STR-06-27-alt1-2-part3.svg)

View: STR-06-27-alt1-2-part3

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 1 of 5: Mode Off, D6 unsupported, unsupported or terminal merge, or the host cannot refuse, part 1 of 2](c4/rendered/STR-06-27-alt2-1.svg)

View: STR-06-27-alt2-1

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 1 of 5: Mode Off, D6 unsupported, unsupported or terminal merge, or the host cannot refuse, part 2 of 2](c4/rendered/STR-06-27-alt2-1-part2.svg)

View: STR-06-27-alt2-1-part2

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 2 of 5: mint pending (outage), part 1 of 4](c4/rendered/STR-06-27-alt2-2.svg)

View: STR-06-27-alt2-2

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 2 of 5: mint pending (outage), part 2 of 4](c4/rendered/STR-06-27-alt2-2-part2.svg)

View: STR-06-27-alt2-2-part2

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 2 of 5: mint pending (outage), part 3 of 4](c4/rendered/STR-06-27-alt2-2-part3.svg)

View: STR-06-27-alt2-2-part3

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 2 of 5: mint pending (outage), part 4 of 4](c4/rendered/STR-06-27-alt2-2-part4.svg)

View: STR-06-27-alt2-2-part4

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 3 of 5: bridge host, enqueue record not yet in the transcript, part 1 of 2](c4/rendered/STR-06-27-alt2-3.svg)

View: STR-06-27-alt2-3

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 3 of 5: bridge host, enqueue record not yet in the transcript, part 2 of 2](c4/rendered/STR-06-27-alt2-3-part2.svg)

View: STR-06-27-alt2-3-part2

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 4 of 5: final mint refusal, or no mint (nonce_source_missing, turn-open-contended), part 1 of 4](c4/rendered/STR-06-27-alt2-4.svg)

View: STR-06-27-alt2-4

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 4 of 5: final mint refusal, or no mint (nonce_source_missing, turn-open-contended), part 2 of 4](c4/rendered/STR-06-27-alt2-4-part2.svg)

View: STR-06-27-alt2-4-part2

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 4 of 5: final mint refusal, or no mint (nonce_source_missing, turn-open-contended), part 3 of 4](c4/rendered/STR-06-27-alt2-4-part3.svg)

View: STR-06-27-alt2-4-part3

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 4 of 5: final mint refusal, or no mint (nonce_source_missing, turn-open-contended), part 4 of 4](c4/rendered/STR-06-27-alt2-4-part4.svg)

View: STR-06-27-alt2-4-part4

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 5 of 5: delivered, or a duplicate delivery, in Pilot or Stateless (absorbed_mid_turn runs as Pilot), part 1 of 5](c4/rendered/STR-06-27-alt2-5.svg)

View: STR-06-27-alt2-5

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 5 of 5: delivered, or a duplicate delivery, in Pilot or Stateless (absorbed_mid_turn runs as Pilot), part 2 of 5](c4/rendered/STR-06-27-alt2-5-part2.svg)

View: STR-06-27-alt2-5-part2

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 5 of 5: delivered, or a duplicate delivery, in Pilot or Stateless (absorbed_mid_turn runs as Pilot), part 3 of 5](c4/rendered/STR-06-27-alt2-5-part3.svg)

View: STR-06-27-alt2-5-part3

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 5 of 5: delivered, or a duplicate delivery, in Pilot or Stateless (absorbed_mid_turn runs as Pilot), part 4 of 5](c4/rendered/STR-06-27-alt2-5-part4.svg)

View: STR-06-27-alt2-5-part4

![Sequence: stateless invocation end to end (process RunStatelessInvocation), alt 2 branch 5 of 5: delivered, or a duplicate delivery, in Pilot or Stateless (absorbed_mid_turn runs as Pilot), part 5 of 5](c4/rendered/STR-06-27-alt2-5-part5.svg)

View: STR-06-27-alt2-5-part5

##### Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence)

The plugin builds with the bound `agent`, `sessionId`, and `requestId`, the prompt text, and the idempotency key `inv-` plus the lowercase hex SHA-256 of `<agent>` LF `<sessionId>` LF `<requestId>` (section 6.6), and keeps the returned `packageId` in its per-turn file `turns/<requestId>.yaml`. `Stateless` on a prompt delivery, on a host whose P0.2 row records an authoritative history-exclusion operation, replaces the model's input through that operation (opencode's `output.messages`, or the model request cline-v2's `beforeModel` returns where its P0.2 row records request replacement, section 3.2), or starts a fresh host invocation, with the package, the request, and the `REQUIRED MEMORIES` block; `Pilot`, an absorbed message, and a host without history exclusion (which runs `Stateless` as `Pilot`) answer statefully. A 422 (also a retried overflow), any other error, an outage (a retryable 409 `conflict` or a busy store's 503 included, section 3.1, "Deadlock and busy outcomes"), or a package not `Built` fails the invocation (operator: "Fail the invocation"): the plugin, not the model, closes the request's own turn `canceled` with the `failureNote` `agent_context_invocation_failed` followed by the code and message (section 6.6) on the persist path of process PersistTurnWriteWithReceipt, with no `Test-ModelEvidence`; a confirmed close leaves `openDelivered` and the Stop ledger, and a close held in the failsafe by an outage or a retryable answer (receipt `queued`) makes the Stop gate warn degraded persistence naming the package failure, never block, and keep the ledger entry until the drain confirms the close. An absorbed message has no prompt to refuse: its failure is recorded in the per-turn file and nothing is refused or closed.

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence)](c4/rendered/BuildAndDeliverPackage.svg)

View: BuildAndDeliverPackage

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 1 of 3: 201, or 200 with package status Built, part 1 of 3](c4/rendered/BuildAndDeliverPackage-alt1-1.svg)

View: BuildAndDeliverPackage-alt1-1

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 1 of 3: 201, or 200 with package status Built, part 2 of 3](c4/rendered/BuildAndDeliverPackage-alt1-1-part2.svg)

View: BuildAndDeliverPackage-alt1-1-part2

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 1 of 3: 201, or 200 with package status Built, part 3 of 3](c4/rendered/BuildAndDeliverPackage-alt1-1-part3.svg)

View: BuildAndDeliverPackage-alt1-1-part3

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 2 branch 1 of 2: Stateless prompt delivery, host with history exclusion, part 1 of 3](c4/rendered/BuildAndDeliverPackage-alt2-1.svg)

View: BuildAndDeliverPackage-alt2-1

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 2 branch 1 of 2: Stateless prompt delivery, host with history exclusion, part 2 of 3](c4/rendered/BuildAndDeliverPackage-alt2-1-part2.svg)

View: BuildAndDeliverPackage-alt2-1-part2

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 2 branch 1 of 2: Stateless prompt delivery, host with history exclusion, part 3 of 3](c4/rendered/BuildAndDeliverPackage-alt2-1-part3.svg)

View: BuildAndDeliverPackage-alt2-1-part3

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 2 branch 2 of 2: Pilot, absorbed_mid_turn, or no history exclusion, part 1 of 2](c4/rendered/BuildAndDeliverPackage-alt2-2.svg)

View: BuildAndDeliverPackage-alt2-2

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 2 branch 2 of 2: Pilot, absorbed_mid_turn, or no history exclusion, part 2 of 2](c4/rendered/BuildAndDeliverPackage-alt2-2-part2.svg)

View: BuildAndDeliverPackage-alt2-2-part2

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 2 of 3: 409 agent_context_disabled, part 1 of 3](c4/rendered/BuildAndDeliverPackage-alt1-2.svg)

View: BuildAndDeliverPackage-alt1-2

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 2 of 3: 409 agent_context_disabled, part 2 of 3](c4/rendered/BuildAndDeliverPackage-alt1-2-part2.svg)

View: BuildAndDeliverPackage-alt1-2-part2

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 2 of 3: 409 agent_context_disabled, part 3 of 3](c4/rendered/BuildAndDeliverPackage-alt1-2-part3.svg)

View: BuildAndDeliverPackage-alt1-2-part3

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 3 of 3: 422, any other error, an outage, or a package not Built, part 1 of 3](c4/rendered/BuildAndDeliverPackage-alt1-3.svg)

View: BuildAndDeliverPackage-alt1-3

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 3 of 3: 422, any other error, an outage, or a package not Built, part 2 of 3](c4/rendered/BuildAndDeliverPackage-alt1-3-part2.svg)

View: BuildAndDeliverPackage-alt1-3-part2

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 1 branch 3 of 3: 422, any other error, an outage, or a package not Built, part 3 of 3](c4/rendered/BuildAndDeliverPackage-alt1-3-part3.svg)

View: BuildAndDeliverPackage-alt1-3-part3

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 1 of 2: absorbed_mid_turn, part 1 of 2](c4/rendered/BuildAndDeliverPackage-alt3-1.svg)

View: BuildAndDeliverPackage-alt3-1

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 1 of 2: absorbed_mid_turn, part 2 of 2](c4/rendered/BuildAndDeliverPackage-alt3-1-part2.svg)

View: BuildAndDeliverPackage-alt3-1-part2

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 2 of 2: prompt delivery, part 1 of 5](c4/rendered/BuildAndDeliverPackage-alt3-2.svg)

View: BuildAndDeliverPackage-alt3-2

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 2 of 2: prompt delivery, part 2 of 5](c4/rendered/BuildAndDeliverPackage-alt3-2-part2.svg)

View: BuildAndDeliverPackage-alt3-2-part2

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 2 of 2: prompt delivery, part 3 of 5](c4/rendered/BuildAndDeliverPackage-alt3-2-part3.svg)

View: BuildAndDeliverPackage-alt3-2-part3

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 2 of 2: prompt delivery, part 4 of 5](c4/rendered/BuildAndDeliverPackage-alt3-2-part4.svg)

View: BuildAndDeliverPackage-alt3-2-part4

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), alt 3 branch 2 of 2: prompt delivery, part 5 of 5](c4/rendered/BuildAndDeliverPackage-alt3-2-part5.svg)

View: BuildAndDeliverPackage-alt3-2-part5

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), opt 1: close queued: an outage or a retryable answer, part 1 of 2](c4/rendered/BuildAndDeliverPackage-opt1.svg)

View: BuildAndDeliverPackage-opt1

![Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence), opt 1: close queued: an outage or a retryable answer, part 2 of 2](c4/rendered/BuildAndDeliverPackage-opt1-part2.svg)

View: BuildAndDeliverPackage-opt1-part2

##### Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence)

The model calls the `agent_context_submit_response` tool before it closes its turn; the plugin binds the call to its request as it binds a verb (process BindVerbToTurn) and supplies the kept `packageId`, or finds it again with QueryAgentContextPackagesQuery `{ agent, sessionId, requestId, status: Built }` when a hook process boundary lost it. `invocationMode` is `Stateless` only when history was excluded. A failed submission or grade is reported in the tool result and writes nothing to the turn; a missing submission is not a Stop-gate item, and the pilot report counts requests with a package and no response. The model grade comes later from an HV request covering the turn.

![Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), part 1 of 2](c4/rendered/SubmitAndGradeResponse.svg)

View: SubmitAndGradeResponse

![Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), part 2 of 2](c4/rendered/SubmitAndGradeResponse-part2.svg)

View: SubmitAndGradeResponse-part2

![Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 1 branch 1 of 3: 201 or 200](c4/rendered/SubmitAndGradeResponse-alt1-1.svg)

View: SubmitAndGradeResponse-alt1-1

![Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 2 branch 1 of 2: 201, part 1 of 2](c4/rendered/SubmitAndGradeResponse-alt2-1.svg)

View: SubmitAndGradeResponse-alt2-1

![Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 2 branch 1 of 2: 201, part 2 of 2](c4/rendered/SubmitAndGradeResponse-alt2-1-part2.svg)

View: SubmitAndGradeResponse-alt2-1-part2

![Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 2 branch 2 of 2: an error or an outage](c4/rendered/SubmitAndGradeResponse-alt2-2.svg)

View: SubmitAndGradeResponse-alt2-2

![Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 1 branch 2 of 3: 409 agent_context_disabled, part 1 of 2](c4/rendered/SubmitAndGradeResponse-alt1-2.svg)

View: SubmitAndGradeResponse-alt1-2

![Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 1 branch 2 of 3: 409 agent_context_disabled, part 2 of 2](c4/rendered/SubmitAndGradeResponse-alt1-2-part2.svg)

View: SubmitAndGradeResponse-alt1-2-part2

![Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence), alt 1 branch 3 of 3: any other error or an outage](c4/rendered/SubmitAndGradeResponse-alt1-3.svg)

View: SubmitAndGradeResponse-alt1-3

#### Sequence: submit GetAgentContextPilotReportQuery

The report groups the window's responses by `invocationMode`, counts them by effective status through `IResponseStalenessRule` read-only (nothing written), and also counts the `Overflow` packages, the refused prompts, and the requests with a package and no response; the operator compares the modes on it before changing any default (section 3.1, the pilot comparison).

![Sequence: submit GetAgentContextPilotReportQuery, part 1 of 4](c4/rendered/STR-06-28.svg)

View: STR-06-28

![Sequence: submit GetAgentContextPilotReportQuery, part 2 of 4](c4/rendered/STR-06-28-part2.svg)

View: STR-06-28-part2

![Sequence: submit GetAgentContextPilotReportQuery, part 3 of 4](c4/rendered/STR-06-28-part3.svg)

View: STR-06-28-part3

![Sequence: submit GetAgentContextPilotReportQuery, part 4 of 4](c4/rendered/STR-06-28-part4.svg)

View: STR-06-28-part4

![Sequence: submit GetAgentContextPilotReportQuery, alt 1 branch 1 of 2: fromUtc or toUtc missing or not UTC, or from not before to, part 1 of 2](c4/rendered/STR-06-28-alt1-1.svg)

View: STR-06-28-alt1-1

![Sequence: submit GetAgentContextPilotReportQuery, alt 1 branch 1 of 2: fromUtc or toUtc missing or not UTC, or from not before to, part 2 of 2](c4/rendered/STR-06-28-alt1-1-part2.svg)

View: STR-06-28-alt1-1-part2

![Sequence: submit GetAgentContextPilotReportQuery, alt 1 branch 2 of 2: window valid, part 1 of 2](c4/rendered/STR-06-28-alt1-2.svg)

View: STR-06-28-alt1-2

![Sequence: submit GetAgentContextPilotReportQuery, alt 1 branch 2 of 2: window valid, part 2 of 2](c4/rendered/STR-06-28-alt1-2-part2.svg)

View: STR-06-28-alt1-2-part2

#### Sequence: submit CreateAgentContextHvRequestCommand

`Caller` is any agent; the request carries `sessionId`, `agent`, `fromRequestId`, `toRequestId`, `requestedBy`, `reason`, and `idempotencyKey`. Every step reads only the database, so the lookup, the session and range checks, the candidate locks (every candidate response in ascending `ResponseId` order, then a re-read of its status), and the insert run in one SerializableWrite delegate, and a concurrent create with the same key is found by the rerun's lookup and a concurrent reject or supersede commits wholly before or after.

![Sequence: submit CreateAgentContextHvRequestCommand, part 1 of 3](c4/rendered/STR-06-29.svg)

View: STR-06-29

![Sequence: submit CreateAgentContextHvRequestCommand, part 2 of 3](c4/rendered/STR-06-29-part2.svg)

View: STR-06-29-part2

![Sequence: submit CreateAgentContextHvRequestCommand, part 3 of 3](c4/rendered/STR-06-29-part3.svg)

View: STR-06-29-part3

![Sequence: submit CreateAgentContextHvRequestCommand, alt 1 branch 1 of 2: blank field](c4/rendered/STR-06-29-alt1-1.svg)

View: STR-06-29-alt1-1

![Sequence: submit CreateAgentContextHvRequestCommand, alt 1 branch 2 of 2: fields given, part 1 of 4](c4/rendered/STR-06-29-alt1-2.svg)

View: STR-06-29-alt1-2

![Sequence: submit CreateAgentContextHvRequestCommand, alt 1 branch 2 of 2: fields given, part 2 of 4](c4/rendered/STR-06-29-alt1-2-part2.svg)

View: STR-06-29-alt1-2-part2

![Sequence: submit CreateAgentContextHvRequestCommand, alt 1 branch 2 of 2: fields given, part 3 of 4](c4/rendered/STR-06-29-alt1-2-part3.svg)

View: STR-06-29-alt1-2-part3

![Sequence: submit CreateAgentContextHvRequestCommand, alt 1 branch 2 of 2: fields given, part 4 of 4](c4/rendered/STR-06-29-alt1-2-part4.svg)

View: STR-06-29-alt1-2-part4

![Sequence: submit CreateAgentContextHvRequestCommand, alt 2 branch 1 of 4: same inputs (a retry or a concurrent winner)](c4/rendered/STR-06-29-alt2-1.svg)

View: STR-06-29-alt2-1

![Sequence: submit CreateAgentContextHvRequestCommand, alt 2 branch 2 of 4: other inputs](c4/rendered/STR-06-29-alt2-2.svg)

View: STR-06-29-alt2-2

![Sequence: submit CreateAgentContextHvRequestCommand, alt 2 branch 3 of 4: absent, part 1 of 2](c4/rendered/STR-06-29-alt2-3.svg)

View: STR-06-29-alt2-3

![Sequence: submit CreateAgentContextHvRequestCommand, alt 2 branch 3 of 4: absent, part 2 of 2](c4/rendered/STR-06-29-alt2-3-part2.svg)

View: STR-06-29-alt2-3-part2

![Sequence: submit CreateAgentContextHvRequestCommand, alt 3 branch 1 of 3: InvalidIdentifier](c4/rendered/STR-06-29-alt3-1.svg)

View: STR-06-29-alt3-1

![Sequence: submit CreateAgentContextHvRequestCommand, alt 3 branch 2 of 3: unknown session, bad range, too long, or no gradable response, part 1 of 2](c4/rendered/STR-06-29-alt3-2.svg)

View: STR-06-29-alt3-2

![Sequence: submit CreateAgentContextHvRequestCommand, alt 3 branch 2 of 3: unknown session, bad range, too long, or no gradable response, part 2 of 2](c4/rendered/STR-06-29-alt3-2-part2.svg)

View: STR-06-29-alt3-2-part2

![Sequence: submit CreateAgentContextHvRequestCommand, alt 3 branch 3 of 3: range valid, part 1 of 2](c4/rendered/STR-06-29-alt3-3.svg)

View: STR-06-29-alt3-3

![Sequence: submit CreateAgentContextHvRequestCommand, alt 3 branch 3 of 3: range valid, part 2 of 2](c4/rendered/STR-06-29-alt3-3-part2.svg)

View: STR-06-29-alt3-3-part2

![Sequence: submit CreateAgentContextHvRequestCommand, alt 2 branch 4 of 4: retries exhausted (SerializableWrite or key allocation), part 1 of 2](c4/rendered/STR-06-29-alt2-4.svg)

View: STR-06-29-alt2-4

![Sequence: submit CreateAgentContextHvRequestCommand, alt 2 branch 4 of 4: retries exhausted (SerializableWrite or key allocation), part 2 of 2](c4/rendered/STR-06-29-alt2-4-part2.svg)

View: STR-06-29-alt2-4-part2

#### Sequence: submit GetAgentContextHvRequestQuery

![Sequence: submit GetAgentContextHvRequestQuery, part 1 of 5](c4/rendered/STR-06-30.svg)

View: STR-06-30

![Sequence: submit GetAgentContextHvRequestQuery, part 2 of 5](c4/rendered/STR-06-30-part2.svg)

View: STR-06-30-part2

![Sequence: submit GetAgentContextHvRequestQuery, part 3 of 5](c4/rendered/STR-06-30-part3.svg)

View: STR-06-30-part3

![Sequence: submit GetAgentContextHvRequestQuery, part 4 of 5](c4/rendered/STR-06-30-part4.svg)

View: STR-06-30-part4

![Sequence: submit GetAgentContextHvRequestQuery, part 5 of 5](c4/rendered/STR-06-30-part5.svg)

View: STR-06-30-part5

![Sequence: submit GetAgentContextHvRequestQuery, alt 1 branch 1 of 2: not found](c4/rendered/STR-06-30-alt1-1.svg)

View: STR-06-30-alt1-1

![Sequence: submit GetAgentContextHvRequestQuery, alt 1 branch 2 of 2: found](c4/rendered/STR-06-30-alt1-2.svg)

View: STR-06-30-alt1-2

#### Sequence: submit QueryAgentContextHvRequestsQuery

![Sequence: submit QueryAgentContextHvRequestsQuery, part 1 of 4](c4/rendered/STR-06-31.svg)

View: STR-06-31

![Sequence: submit QueryAgentContextHvRequestsQuery, part 2 of 4](c4/rendered/STR-06-31-part2.svg)

View: STR-06-31-part2

![Sequence: submit QueryAgentContextHvRequestsQuery, part 3 of 4](c4/rendered/STR-06-31-part3.svg)

View: STR-06-31-part3

![Sequence: submit QueryAgentContextHvRequestsQuery, part 4 of 4](c4/rendered/STR-06-31-part4.svg)

View: STR-06-31-part4

![Sequence: submit QueryAgentContextHvRequestsQuery, alt 1 branch 1 of 2: unknown status, or limit or offset outside PageBounds](c4/rendered/STR-06-31-alt1-1.svg)

View: STR-06-31-alt1-1

![Sequence: submit QueryAgentContextHvRequestsQuery, alt 1 branch 2 of 2: filters valid, part 1 of 2](c4/rendered/STR-06-31-alt1-2.svg)

View: STR-06-31-alt1-2

![Sequence: submit QueryAgentContextHvRequestsQuery, alt 1 branch 2 of 2: filters valid, part 2 of 2](c4/rendered/STR-06-31-alt1-2-part2.svg)

View: STR-06-31-alt1-2-part2

## 7. Use cases

Mermaid has no UML use case diagram, so each use case is drawn as a flowchart: actors are on the left as stadium shapes, the use case is the central oval, and included or extending behaviors are ovals linked with `include` and `extend` labels. UC-1 to UC-9 are defined here: each section gives the actors, flows, and realized requirements above its diagram (moved from the plan on 2026-10-12; the plan's Phase 1 creates the use-case records from these sections). Sections 7.11 to 7.16 are the agent-context use cases, named by their titles; their records are created in Phase 1 with the FR-MCP-AGENTCONTEXT catalog. A flow names in parentheses the unit, process, policy, or shared component (section 3.5) that performs a step; the component's rules are stated once in section 3.5 and are not repeated here. One alternate applies to every use case and is not repeated in each: a request whose workspace path names no registered workspace, or that resolves no workspace on a record that does not allow it, is refused with 400 `validation_error`, `details.field` `workspacePath` (WorkspaceResolutionBehavior, section 3.5.1). Diagram labels are short forms of the flows; the overview's labels are short forms of the section titles, and Phase 1 creates each use case under its section title.

### 7.1 Overview

![Use cases: overview (use cases), part 1 of 12](c4/rendered/STR-07-01.svg)

View: STR-07-01

![Use cases: overview (use cases), part 2 of 12](c4/rendered/STR-07-01-part2.svg)

View: STR-07-01-part2

![Use cases: overview (use cases), part 3 of 12](c4/rendered/STR-07-01-part3.svg)

View: STR-07-01-part3

![Use cases: overview (use cases), part 4 of 12](c4/rendered/STR-07-01-part4.svg)

View: STR-07-01-part4

![Use cases: overview (use cases), part 5 of 12](c4/rendered/STR-07-01-part5.svg)

View: STR-07-01-part5

![Use cases: overview (use cases), part 6 of 12](c4/rendered/STR-07-01-part6.svg)

View: STR-07-01-part6

![Use cases: overview (use cases), part 7 of 12](c4/rendered/STR-07-01-part7.svg)

View: STR-07-01-part7

![Use cases: overview (use cases), part 8 of 12](c4/rendered/STR-07-01-part8.svg)

View: STR-07-01-part8

![Use cases: overview (use cases), part 9 of 12](c4/rendered/STR-07-01-part9.svg)

View: STR-07-01-part9

![Use cases: overview (use cases), part 10 of 12](c4/rendered/STR-07-01-part10.svg)

View: STR-07-01-part10

![Use cases: overview (use cases), part 11 of 12](c4/rendered/STR-07-01-part11.svg)

View: STR-07-01-part11

![Use cases: overview (use cases), part 12 of 12](c4/rendered/STR-07-01-part12.svg)

View: STR-07-01-part12

### 7.2 Use case: Log each user request as exactly one turn (plan UC-1) [FR-MCP-SESSIONTURN-001, -002, -003, -007, -008]

UC-1 Log each user request as exactly one turn. Actors: Operator, agent host (any of the nine), plugin hooks or tools, model. Precondition: workspace trusted (marker signature and health nonce verified). Basic flow: operator submits a prompt; on a prompt-hook or adapter host the hook classifies it as a user request, takes the turn-open lock, derives the nonce from the event (host binding table), writes its write-ahead failsafe mint record, asks the server to mint the requestId (MintSessionTurnCommand; the server opens the turn), classifies the result (Resolve-MintOutcome), and records the token as the host session's write target at once (Update-TurnState); on a bridge host (claude-code, claude-cowork) the arrival bridge mints from the transcript `enqueue` record (state `queued`) and the request becomes the write target at its `dequeue`, or at an `absorbed_mid_turn` removal (process ApplyArrivalBridgeRecord; FR-MCP-SESSIONTURN-001-AC009, -AC010); the request enters the stop ledger when it is delivered (at once on prompt-hook and adapter hosts; at its `dequeue` or `absorbed_mid_turn` removal on bridge hosts), never at mint; every verb the model invokes is bound automatically to the host session's write target at invocation start (process BindVerbToTurn); model closes the turn. Alternate flows: the mint call is retried or replayed and returns the same requestId; a doubly registered hook delivers the event twice and both deliveries get the same requestId (only on hosts whose nonce is a stable per-message id or the bridge's single ordered source; on copilot and classic cline a second registration is refused before any mint, and a second new equal-field invocation fails the host session closed under the equal-field rule (Set-SessionUnsupported), while a stored mint retry or failsafe drain reuses its nonce); the server is unreachable and a provisional `pending-<nonce>` turn is rebound on drain; an identical prompt sent again later is a new event and opens a second turn; background event is skipped (on a bridge host it keeps its queue position as an unminted system entry, so its `dequeue` delivers nothing); operator sends a message mid-turn, the arrival bridge mints its turn at enqueue without touching the running turn or the write target, the message is delivered (absorbed into the running turn, or dequeued) and from then on, while the earlier request is still open, an unqualified write is refused with `request_ambiguous` (D9) and the model names the requestId of each write; once the earlier request is closed, unqualified writes bind to the delivered request; the operator pulls a queued message back (`popAll`) before delivery, the request is withdrawn, and the stop gate asks the model to fail it; a verb without a host session key in a workspace with two live sessions is refused rather than guessed; a subagent's hooks fire and leave this session's turns unchanged; while two delivered requests are open an unqualified verb is refused with `request_ambiguous` and the model names the requestId (D9); on Copilot with more than one MCP prompt hook registered the hook reports `turn_logging_unsupported` and mints nothing (D11); on a host with no arrival mechanism every session-log verb reports `turn_logging_unsupported` (D6); a SessionStart after compaction, or after a resume that reuses the host session key (Claude Code resume, `codex exec resume`), continues in the host session's recorded MCP session: a resumed key whose `ended.yaml` says `session_end` is revived by removing that marker in one atomic replace under `turn-open.lock` (as the idle revival does), Invoke-SessionOpen never replaces a recorded `mcpSessionId`, open turns stay bound to it, and no `unsupported_session` record of a still-live host session is drained (FR-MCP-SESSIONLIFE-002; replaces the session-rotation rebind of TR-MCP-PLUGIN-012 and FR-MCP-XAGENT-001 and FR-MCP-TRIAGEPLUGIN-001's "completeTurn survives sessionId rebind"); a prompt hook killed after its write-ahead mint record ends, at the next hook, verb, or SessionStart drain, with exactly one server turn for that message, delivered once; a contended `turn-open.lock` at prompt time mints nothing: the hook does not wait, writes a failsafe diagnostic, and the Stop gate warns `turn-open-contended` naming the prompt; a final mint refusal opens no turn (`turn-open-failed` with the server code; the refusal counts as the first drain attempt of its write-ahead mint record, which the drain mints again under the attempt budget and then quarantines, section 3.1, "Failsafe drain", and Resolve-MintOutcome, section 3.5.6) and the Stop gate warns the refusal naming the prompt (FR-MCP-SESSIONTURN-001-AC008), and on a bridge host the refused request becomes an `open-failed` queue entry that keeps its queue position and delivers nothing; a hook payload without its host row's session key is refused and writes nothing; a host-local command that never reaches the model (for example `/clear`, `/compact`) is a system event when P0.2 shows it produces an arrival event; cline-v2 is supported only when P0.2 shows one run per user message (D6 otherwise); an arrival bridge whose running version differs from the active plugin cache is restarted from the active cache with `appliedThroughOffset` kept; clear-session fails a minted request that is still `queued` as withdrawn ("withdrawn before delivery"). Realizes FR-MCP-SESSIONTURN-001, -002, -003, -007, -008; updated FR-MCP-SESSIONLIFE-001, FR-MCP-SESSIONLIFE-002, FR-MCP-REPL-010, FR-MCP-TRIAGESTORE-001.

The diagram's short labels map to the flows above: minting at ingress runs MintSessionTurnCommand and Resolve-MintOutcome; a retry, a failsafe replay, and a drain after a killed prompt hook send the same nonce and get the one stored turn; a queued message is minted and later delivered by process ApplyArrivalBridgeRecord; a withdrawn request (`popAll`, or `remove` without `absorbed_mid_turn`) is one the Stop gate asks the model to fail; an outage's `pending-<nonce>` turn is rebound by process DrainFailsafe; a resumed or compacted host session is revived by process StartHostSession and keeps its recorded MCP session (Invoke-SessionOpen); a contended `turn-open.lock` or a final mint refusal opens no turn, and the Stop gate warns naming the prompt.

![Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 1 of 8](c4/rendered/UC-1.svg)

View: UC-1

![Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 2 of 8](c4/rendered/UC-1-part2.svg)

View: UC-1-part2

![Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 3 of 8](c4/rendered/UC-1-part3.svg)

View: UC-1-part3

![Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 4 of 8](c4/rendered/UC-1-part4.svg)

View: UC-1-part4

![Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 5 of 8](c4/rendered/UC-1-part5.svg)

View: UC-1-part5

![Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 6 of 8](c4/rendered/UC-1-part6.svg)

View: UC-1-part6

![Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 7 of 8](c4/rendered/UC-1-part7.svg)

View: UC-1-part7

![Use case: Log each user request as exactly one turn (plan UC-1) (use cases), part 8 of 8](c4/rendered/UC-1-part8.svg)

View: UC-1-part8

### 7.3 Use case: Persist session-log writes with truthful receipts (plan UC-2) [FR-MCP-SESSIONTURN-004]

UC-2 Persist session-log writes with truthful receipts. Actors: model, plugin, server. Basic flow: model calls a verb; plugin persists (process PersistTurnWriteWithReceipt); server writes the turn (SubmitSessionLogCommand, TurnWritePipeline inside SerializableWrite) and returns the per-turn outcome; plugin reports persisted only for created/merged. Alternate: target turn is terminal, server reports `skipped_terminal`, plugin reports `persisted:false`, exits non-zero, clears the failsafe; legacy server without outcomes yields `outcomeVerified:false`; a write that keeps conflicting with a concurrent write to the same turn, or whose deadlock or busy store outlasts SerializableWrite's four attempts, is refused with 409 `conflict` (`retryable: true`, SerializableWrite, section 3.4 write isolation), which the plugin handles as an outage (section 3.1, "Deadlock and busy outcomes": the failsafe outcome of TEST-MCP-TRIAGESTORE-005, TR-MCP-TRIAGEERR-001, and FR-MCP-SESSIONLIFE-003 ac6 holds while their status changes from 503 to 409, and TR-MCP-TRIAGEERR-001's "retryable false unless SQLITE_BUSY or deadlock" for `conflict` is replaced; unreachable storage keeps its answer, so TEST-MCP-TRIAGESTORE-007 is unchanged); the server is unreachable or answers 503 `backend_unavailable`, a busy store's 503 `persistence_error` with `retryable: true` (outside SerializableWrite, `src/QBrainAi.Support.Mcp/Services/McpErrorClassifier.cs:129-139` at `a8d3934b`), or 409 `conflict` with `retryable: true`, and the write is kept in the failsafe and reported `persisted:false`, code `queued`, exit 0 (degraded persistence, FR-MCP-SESSIONLIFE-003, FR-MCP-REPL-009); a failsafe drain (process DrainFailsafe, section 3.1, "Failsafe drain") stops at the first retryable answer without consuming attempts and keeps that record and every later one, while a final answer counts one of the record's drain attempts and the drain continues, skipping only the later records of the same unresolved turn; a record at the attempt budget (5), a malformed record, and a legacy record that names no request move to the quarantine with their `.reason.txt`, and nothing replays from the quarantine; a legacy record whose parameters name its session and request replays as today, including the exhausted numeric-keyed records that the drain repairs and replays (FR-MCP-SESSIONLIFE-004). Realizes FR-MCP-SESSIONTURN-004; updated FR-MCP-SESSIONLIFE-003, FR-MCP-PLUGININT-001, FR-MCP-REPL-011, FR-MCP-FAILSAFE-001, FR-MCP-SESSIONLIFE-004.

![Use case: Persist session-log writes with truthful receipts (plan UC-2) (use cases), part 1 of 4](c4/rendered/UC-2.svg)

View: UC-2

![Use case: Persist session-log writes with truthful receipts (plan UC-2) (use cases), part 2 of 4](c4/rendered/UC-2-part2.svg)

View: UC-2-part2

![Use case: Persist session-log writes with truthful receipts (plan UC-2) (use cases), part 3 of 4](c4/rendered/UC-2-part3.svg)

View: UC-2-part3

![Use case: Persist session-log writes with truthful receipts (plan UC-2) (use cases), part 4 of 4](c4/rendered/UC-2-part4.svg)

View: UC-2-part4

### 7.4 Use case: Close every request turn before stopping (plan UC-3) [FR-MCP-SESSIONTURN-005, -006]

UC-3 Close every request turn before stopping. Actors: model, host Stop hook. Basic flow: model records actions/decisions (incl. via `designDecisions`) and closes each turn (a `completed` or `failed` close requires model evidence, Test-ModelEvidence); Stop hook (process EvaluateStopGate) finds all closed with evidence and passes, clearing the ledger. Alternate: a turn is open (including one whose close the plugin refused for missing model evidence) or its most recent close was refused by the server's completion gate, the Stop hook blocks with the reason and the accepted remedy (Get-CloseRemedy); a turn closed without evidence by another surface is only warned and never closes turns itself. Alternate: the host session failed closed under D6 (`unsupportedSince`) while a turn was open; the Stop hook blocks with the reduced remedy (only the missing model evidence, the missing `requestClass` from S8/S9, and the close call; the terminal reviews are judged at the drain), the model's evidence and close for that turn are captured in the plugin failsafe (reviews are refused and recorded after the drain through the original pair), the next Stop warns instead of blocking, and a later SessionStart drains the captured records with the turn's original pair through the trusted recovery flow (a post-compact SessionStart of the still-live host session drains none of them). Alternate: a model close held in the failsafe by a transport outage, or the package-failure `canceled` close of a stateless or pilot invocation (section 7.15) held there, is warned as degraded persistence (naming the package failure for the `canceled` close, which never needs model evidence) and never blocks; its ledger entry stays until the drain confirms the close. Alternate: a contended turn-open lock or a final mint refusal at prompt time left no turn, and the Stop hook warns naming the prompt. Alternate: the failsafe quarantine holds a record of an open turn, and the Stop hook warns and never blocks (section 3.1, "Failsafe drain"). A missing agent-context response submission (section 7.12) is not a Stop-gate item. Realizes FR-MCP-SESSIONTURN-005, -006; updated FR-MCP-SESSIONLIFE-005, FR-MCP-CLEARSESSION-001.

![Use case: Close every request turn before stopping (plan UC-3) (use cases), part 1 of 5](c4/rendered/UC-3.svg)

View: UC-3

![Use case: Close every request turn before stopping (plan UC-3) (use cases), part 2 of 5](c4/rendered/UC-3-part2.svg)

View: UC-3-part2

![Use case: Close every request turn before stopping (plan UC-3) (use cases), part 3 of 5](c4/rendered/UC-3-part3.svg)

View: UC-3-part3

![Use case: Close every request turn before stopping (plan UC-3) (use cases), part 4 of 5](c4/rendered/UC-3-part4.svg)

View: UC-3-part4

![Use case: Close every request turn before stopping (plan UC-3) (use cases), part 5 of 5](c4/rendered/UC-3-part5.svg)

View: UC-3-part5

### 7.5 Use case: Classify a request (plan UC-4) [FR-MCP-SESSIONCLASS-001, FR-MCP-SESSIONREVIEW-006]

UC-4 Classify a request. Actors: model. Basic flow: model sets `requestClass` (Code, Docs, Chore) on its turn (policy RequestClassification); an `Unstarted` hostile record flips to Unnecessary for Chore (and back if the class changes away while it is still Unnecessary), on in-progress turns only (ReviewDefaults); records past Unstarted are never changed automatically. Alternate: invalid value rejected (400 `request_class_invalid`); a requestClass-only change on a terminal turn is accepted and changes no review, and an additive write carrying it is not re-gated (the completion gate fires only on a move into `completed` or `failed` and on a Replace that carries either, policy CompletionGate); a Chore flip racing a review write or a close of the same turn commits wholly before or after it (SerializableWrite), so a record that a concurrent write moved past its default is never changed by the flip. Realizes FR-MCP-SESSIONCLASS-001, FR-MCP-SESSIONREVIEW-006.

![Use case: Classify a request (plan UC-4) (use cases), part 1 of 3](c4/rendered/UC-4.svg)

View: UC-4

![Use case: Classify a request (plan UC-4) (use cases), part 2 of 3](c4/rendered/UC-4-part2.svg)

View: UC-4-part2

![Use case: Classify a request (plan UC-4) (use cases), part 3 of 3](c4/rendered/UC-4-part3.svg)

View: UC-4-part3

### 7.6 Use case: Complete or fail a turn under the completion gate (plan UC-5) [FR-MCP-SESSIONCLASS-002, -003, -004]

UC-5 Complete or fail a turn under the completion gate. Actors: model, server, server-internal writers, QBAgent and the REPL's transcript import. Basic flow: model completes; the server's turn write (TurnWritePipeline) runs the gate (policy CompletionGate), which checks class and review statuses; turn completes. Alternate: gate unmet, 400 with missing items (`code` `validation_error`, token `session_turn_completion_gate`), turn stays in progress; fail requires only class; exempt origins (the server-internal writers of section 3.1, "Completion rules") bypass; the plugin, REPL, and McpAgent pre-checks only warn with the exact gate message and never refuse (the server gate is the only authority), and a server refusal is reported with the server's message and missing items; QBAgent's `mcp_session_turn_complete` tool stays gated and passes the optional `requestClass`, `agentReview`, and `hostileReview` arguments of CompleteSessionTurnCommand (TR-MCP-SESSIONCLASS-004); REPL `importRecovery` stays gated, reports each recovered turn the gate refuses in its import result with the gate's missing items, never writes a fabricated review, and has a recovered status outside the agent write set refused with 400 `validation_error` (section 3.1, "Completion gate writers"; the REPL keeps its status normalization, `completed-local` and `partial` to `completed` and `interrupted` to `failed`, before it submits, so the one normalized status refused is `superseded`, `src/QBrainAi.Repl.Core/SessionLogWorkflow.cs:725-750` at `a8d3934b`); `canceled` is not gated; a write that only enriches an already terminal turn is not gated, while a Replace (PUT) that carries `completed` or `failed` is gated over any stored status, because a whole replacement is not enrichment (FR-MCP-SESSIONCLASS-002-AC004); an agent surface (REST, MCP, client, REPL, plugin, and QBAgent's internal session-log tools, which call `UpsertTurnAsync` directly, `src/QBrainAi.Support.Mcp/Services/QuadBrainInternalToolExecutor.cs:299-328` at `a8d3934b`) writes only `in_progress`, `completed`, `failed`, `canceled`, and the legacy `cancelled`, and a write naming `closed` or any other value is refused with 400 `validation_error` (server-internal writers and existing rows keep `closed`, TurnStatus); a close racing a review reset or class change on the same turn commits before or after it, never between its gate check and its save (SerializableWrite, section 3.4 write isolation). Realizes FR-MCP-SESSIONCLASS-002, -003, -004; updated FR-SUPPORT-013, FR-SUPPORT-014.

![Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 1 of 8](c4/rendered/UC-5.svg)

View: UC-5

![Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 2 of 8](c4/rendered/UC-5-part2.svg)

View: UC-5-part2

![Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 3 of 8](c4/rendered/UC-5-part3.svg)

View: UC-5-part3

![Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 4 of 8](c4/rendered/UC-5-part4.svg)

View: UC-5-part4

![Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 5 of 8](c4/rendered/UC-5-part5.svg)

View: UC-5-part5

![Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 6 of 8](c4/rendered/UC-5-part6.svg)

View: UC-5-part6

![Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 7 of 8](c4/rendered/UC-5-part7.svg)

View: UC-5-part7

![Use case: Complete or fail a turn under the completion gate (plan UC-5) (use cases), part 8 of 8](c4/rendered/UC-5-part8.svg)

View: UC-5-part8

### 7.7 Use case: Record an agent self-review (plan UC-6) [FR-MCP-SESSIONREVIEW-001, -002, -003, -006, -008, FR-SUPPORT-016]

UC-6 Record an agent self-review. Actors: model via any of the nine plugins (on a host recorded `turn_logging_unsupported` every verb reports that code and no review is recorded). Basic flow: model records `agentReview` (Running, then scores); the server finds the turn (TurnLocator), validates each score as an exact decimal lexeme (ScoreLexeme), derives Pass/Fail from thresholds, and stamps them (ScoreVerdict). Alternate: conflicting supplied status 422; invalid fields 400; review recorded after completion; a write that keeps conflicting with a concurrent write to the same turn gets 409 `conflict` (`retryable: true`) once SerializableWrite's four attempts are spent; like every retryable answer to a review (a transport outage, 503 `backend_unavailable`, a busy store's 503 `persistence_error`, or that 409), it is not queued, because a review keeps its no-failsafe rule: the verb reports `persisted: false` with the answer's code and `retryable: true`, exits 1, and the model records the review again for the same turn after the drain (section 3.1, "Retryable answers"; section 5.2, RecordSessionTurnReviewCommand); a review section reset (ResetSessionTurnReviewCommand) restores the creation default for the turn's current class, and is refused with 400 `review_section_reset_on_terminal_turn` on a `completed` or `failed` turn, record unchanged, while an `in_progress`, `canceled`, `cancelled`, or `closed` turn is reset (FR-MCP-SESSIONREVIEW-001-AC004, FR-SUPPORT-016 ac-5). Realizes FR-MCP-SESSIONREVIEW-001, -002, -003, -006, -008, FR-SUPPORT-016 (new); updated FR-MCP-SESSIONLOGSAN-001.

![Use case: Record an agent self-review (plan UC-6) (use cases), part 1 of 5](c4/rendered/UC-6.svg)

View: UC-6

![Use case: Record an agent self-review (plan UC-6) (use cases), part 2 of 5](c4/rendered/UC-6-part2.svg)

View: UC-6-part2

![Use case: Record an agent self-review (plan UC-6) (use cases), part 3 of 5](c4/rendered/UC-6-part3.svg)

View: UC-6-part3

![Use case: Record an agent self-review (plan UC-6) (use cases), part 4 of 5](c4/rendered/UC-6-part4.svg)

View: UC-6-part4

![Use case: Record an agent self-review (plan UC-6) (use cases), part 5 of 5](c4/rendered/UC-6-part5.svg)

View: UC-6-part5

### 7.8 Use case: Record a hostile validation review with receipts (plan UC-7) [FR-MCP-SESSIONREVIEW-002, -005]

UC-7 Record a hostile validation review with receipts. Actors: hostile reviewer (Codex CLI, gpt-6-astra, max), coordinator agent. Basic flow: at each HV point of "Execution organization" once its gates are green (D10), the coordinator writes the HV brief and launches the Codex reviewer; the runner records the request and response jsonl; the reviewer writes its markdown and JSON receipts and stores its entire result on its own session-log turn; from the Phase 4 live-check deploy on, the coordinator records `hostileReview` from the receipt (accuracy/completeness mapped, receipt paths set) and backfills the Phase 4 context verdicts by original pair before the phase-wide Phase 4 HV; for every HV point before that deploy, it records the verdict on the work package's coordinating turn (dialog and designDecisions with the receipt paths) and with `RecordValidationResultAsync`. Alternate: DISAGREE with passing scores recorded as Other with notes; missing receipt paths rejected; a retryable answer to the review write is reported with its code and `retryable: true`, exit 1, never queued, and the coordinator records the review again after the drain (as in UC-6, section 5.2); `hvVerdict` sent together with an explicit `status` is refused with 400 `validation_error` naming `status`; a run whose live model or effort is not gpt-6-astra/max is not accepted as HV (an HV-protocol rule: the coordinator checks the reviewer's recorded model and effort in its receipts before recording any verdict, so process ConvertHvReceiptToReview and the plugin do not check it); from the Phase 4 live-check deploy on, Code or Docs turns of a work package that completed before its HV carry `Other` "pending HV <gate>" and receive the verdict through `recordReview`, each addressed by its original (`sessionId`, `requestId`) pair (FR-MCP-SESSIONREVIEW-002-AC002). The reviewer's brief is a user prompt and opens its own turn, because the prompt hook classifies a background agent only by the host's subagent markers, never by prompt text (section 5.4; this replaces FR-MCP-TRIAGEPLUGIN-001's text-detected background-brief reuse); the HostileReview subsystem (FR-MCP-HOSTILEREVIEW-001 to -006, its `xhigh` worker, and its AGREE, DISAGREE, or UNKNOWN verdicts) is separate: its results are not HV receipts for this use case and are not judged by its `max` rule (section 5.4; FR-MCP-HOSTILEREVIEW-003 is unchanged). Realizes FR-MCP-SESSIONREVIEW-002, -005 (-005 updated with that separation); updated FR-MCP-TRIAGEPLUGIN-001.

![Use case: Record a hostile validation review with receipts (plan UC-7) (use cases), part 1 of 5](c4/rendered/UC-7.svg)

View: UC-7

![Use case: Record a hostile validation review with receipts (plan UC-7) (use cases), part 2 of 5](c4/rendered/UC-7-part2.svg)

View: UC-7-part2

![Use case: Record a hostile validation review with receipts (plan UC-7) (use cases), part 3 of 5](c4/rendered/UC-7-part3.svg)

View: UC-7-part3

![Use case: Record a hostile validation review with receipts (plan UC-7) (use cases), part 4 of 5](c4/rendered/UC-7-part4.svg)

View: UC-7-part4

![Use case: Record a hostile validation review with receipts (plan UC-7) (use cases), part 5 of 5](c4/rendered/UC-7-part5.svg)

View: UC-7-part5

### 7.9 Use case: Configure review pass thresholds (plan UC-8) [FR-MCP-SESSIONREVIEW-007]

UC-8 Configure review pass thresholds. Actors: operator. Basic flow: operator sets thresholds per kind and score (or per instance); startup accepts them; new reviews derive with them and stamp them (ScoreVerdict). Alternate: value outside [0,1] rejected. These review thresholds are separate from the agent-context trust thresholds, which never go below 0.98 (section 6.2). Realizes FR-MCP-SESSIONREVIEW-007.

![Use case: Configure review pass thresholds (plan UC-8) (use cases), part 1 of 2](c4/rendered/UC-8.svg)

View: UC-8

![Use case: Configure review pass thresholds (plan UC-8) (use cases), part 2 of 2](c4/rendered/UC-8-part2.svg)

View: UC-8-part2

### 7.10 Use case: Read review scores losslessly (plan UC-9) [FR-MCP-SESSIONREVIEW-004]

UC-9 Read review scores losslessly. Actors: agents, client apps. Basic flow: a client reads a turn through REST, MCP, client, REPL, or a plugin and gets the exact decimal scores (TurnReadProjection: scores as their exact lexemes through ScoreLexeme, review text through SecretRedactor, unset review fields `null`); the REPL keeps the lexemes through its lossless normalizer (process PreserveReviewScoreLexemes, which calls ScoreLexeme) and the PowerShell core through `Protect-ReplLosslessScalars`. Realizes FR-MCP-SESSIONREVIEW-004.

![Use case: Read review scores losslessly (plan UC-9) (use cases), part 1 of 2](c4/rendered/UC-9.svg)

View: UC-9

![Use case: Read review scores losslessly (plan UC-9) (use cases), part 2 of 2](c4/rendered/UC-9-part2.svg)

View: UC-9-part2

### 7.11 Use case: Build a context package for a request [FR-MCP-AGENTCONTEXT-001, -002]

Actors: agent host and plugin, server. Precondition: the request's turn exists (UC-1) and the mode is `Pilot` or `Stateless`. Basic flow: the plugin asks the server to build a package for the request, named by `agent`, `sessionId`, and `requestId` (TurnLocator); the server gathers the request, profile, rules, applicable requirements, related turns (never the request's own turn), TODOs, receipts, trusted prior responses, and code at the current commit (SourceResolver), filters secrets (SecretRedactor) and untrusted items, ranks (CandidateRanker), fills the budget, records provenance and an input snapshot holding every input the assembler reads (PackageAssembler), and returns the rendered package; the plugin keeps the `packageId` in its per-turn file `turns/<requestId>.yaml`. Alternate flows: mandatory context exceeds the budget and the build fails with `agent_context_budget_exceeded`, storing an `Overflow` package, and a retried or concurrent build returns the same 422 with the stored diagnostics; the same idempotency key with the same inputs returns the same package, found before any precondition that can change (IdempotentCreate), and the same key with other inputs is 409 `conflict` with `retryable: false`; the commit is unreadable (409 `agent_context_commit_unavailable`); no profile or rule file resolves (409 `agent_context_profile_unavailable`); the Mode was switched to `Off` after the plugin cached it (409 `agent_context_disabled`, ModeGateBehavior), and the plugin caches `Off` and continues statefully; replay proves the package is reproducible from its input snapshot.

![Use case: Build a context package for a request (use cases), part 1 of 5](c4/rendered/STR-07-02.svg)

View: STR-07-02

![Use case: Build a context package for a request (use cases), part 2 of 5](c4/rendered/STR-07-02-part2.svg)

View: STR-07-02-part2

![Use case: Build a context package for a request (use cases), part 3 of 5](c4/rendered/STR-07-02-part3.svg)

View: STR-07-02-part3

![Use case: Build a context package for a request (use cases), part 4 of 5](c4/rendered/STR-07-02-part4.svg)

View: STR-07-02-part4

![Use case: Build a context package for a request (use cases), part 5 of 5](c4/rendered/STR-07-02-part5.svg)

View: STR-07-02-part5

### 7.12 Use case: Submit and grade an agent response [FR-MCP-AGENTCONTEXT-003, -004]

Actors: model, plugin, server, hostile reviewer. Basic flow: in `Pilot` and `Stateless` the model submits its response with its claims and evidence through the `agent_context_submit_response` tool (SubmitAgentResponseCommand) before it closes the turn, and the plugin supplies the request's `packageId` (kept in its per-turn file `turns/<requestId>.yaml`, or found again with QueryAgentContextPackagesQuery `{ agent, sessionId, requestId, status: Built }` when a hook process boundary lost it; the package is found by BuiltPackageLookup); the deterministic grade runs (RunDeterministicGradeCommand); an agent creates an HV request covering the turn (CreateAgentContextHvRequestCommand) and the HV agent records the model grade (RecordModelGradeCommand) after the eight receipt, reviewer, and runtime-identity checks; after each grade the trust decision (DecideAgentResponseTrustCommand) marks the response `Trusted` (ResponseStatusTransition) when the latest grade of each kind (latest by the server-stamped `RecordedUtc`, then `GradeId`) passes at `max(configured, 0.98)` with zero FAIL and zero UNKNOWN (ScoreVerdict). Alternate flows: a grade fails and the response stays `Grading` with every failure shown; a grader other than the configured one is refused; the operator rejects the response (RejectAgentResponseCommand): a repeated reject returns it unchanged, and a reject of a `Superseded` response is refused with 409 `agent_context_transition_invalid` (ResponseStatusTransition); a dependency changes after the deterministic grade, and the model grade's staleness evaluation (EvaluateStalenessCommand, which the grade dispatches before its own write and which alone persists `Stale`) marks the response `Stale`, which the trust decision, reading only the database, leaves `Stale`, never `Trusted`; evidence that cites the request's own turn is refused with 400 `validation_error`; an HV request retried with the same idempotency key returns the stored request, and a response already `Pending` in another open HV request is not listed again; a write that spends SerializableWrite's four attempts is refused with 409 `conflict` (`retryable: true`) and saves nothing; a submission answered 409 `agent_context_disabled` (Mode switched to `Off`) makes the plugin cache `Off` and continue statefully; a request with a package and no submitted response is not a Stop-gate item, and the pilot report counts it.

The diagram's short labels map to the flows above: the model submits through the `agent_context_submit_response` tool; the trust decision applies ResponseStatusTransition; an operator reject runs RejectAgentResponseCommand, and a reject of a `Superseded` response is the 409; an HV request retried with its idempotency key returns the stored request; a request with no submission is counted by the pilot report.

![Use case: Submit and grade an agent response (use cases), part 1 of 7](c4/rendered/STR-07-03.svg)

View: STR-07-03

![Use case: Submit and grade an agent response (use cases), part 2 of 7](c4/rendered/STR-07-03-part2.svg)

View: STR-07-03-part2

![Use case: Submit and grade an agent response (use cases), part 3 of 7](c4/rendered/STR-07-03-part3.svg)

View: STR-07-03-part3

![Use case: Submit and grade an agent response (use cases), part 4 of 7](c4/rendered/STR-07-03-part4.svg)

View: STR-07-03-part4

![Use case: Submit and grade an agent response (use cases), part 5 of 7](c4/rendered/STR-07-03-part5.svg)

View: STR-07-03-part5

![Use case: Submit and grade an agent response (use cases), part 6 of 7](c4/rendered/STR-07-03-part6.svg)

View: STR-07-03-part6

![Use case: Submit and grade an agent response (use cases), part 7 of 7](c4/rendered/STR-07-03-part7.svg)

View: STR-07-03-part7

### 7.13 Use case: Retrieve trusted prior context [FR-MCP-AGENTCONTEXT-007]

Actors: server (inside package construction), client apps. Basic flow: retrieval returns ranked candidates (CandidateRanker on the relational path), only trusted responses among prior responses, preferring GraphRAG when the caller asks for it (`preferGraphRag`; a package build asks with the value of `GraphRag:UseForRetrieval` and records it, so its ranking inputs are part of its provenance); a request that names a turn names it by `agent`, `sessionId`, and `requestId` (TurnLocator), and `limit` is bounded (PageBounds `Refuse`). Every read checks staleness read-only and writes nothing (IResponseStalenessRule), and the query answers in every Mode, `Off` included, because ModeGateBehavior gates commands only. Alternate flow: GraphRAG disabled, uninitialized, not indexed, failing, or asked for kinds other than `PriorResponse`, and the deterministic relational path answers with the fallback reason; a `limit` outside 1..1000 is refused with 400 `validation_error`.

![Use case: Retrieve trusted prior context (use cases), part 1 of 3](c4/rendered/STR-07-04.svg)

View: STR-07-04

![Use case: Retrieve trusted prior context (use cases), part 2 of 3](c4/rendered/STR-07-04-part2.svg)

View: STR-07-04-part2

![Use case: Retrieve trusted prior context (use cases), part 3 of 3](c4/rendered/STR-07-04-part3.svg)

View: STR-07-04-part3

### 7.14 Use case: Maintain relationships, staleness, and supersession [FR-MCP-AGENTCONTEXT-005, -006]

Actors: model, operator, server. Basic flow: links are created between packages, responses, requirements, turns, TODOs, code, and receipts (RelationshipWriter, which records each target's current hash as the calling unit read it before its write); when a dependency changes, including a response dependency that leaves `Trusted` or becomes `Rejected`, `Superseded`, or `Stale` (the unit that moved it dispatches EvaluateStalenessCommand for its dependents after its commit, section 3.1, Dependent responses), its dependents become stale (IResponseStalenessRule; only EvaluateStalenessCommand persists `Stale`, through ResponseStatusTransition, and every read reports the effective status and writes nothing): a file dependency is compared at the workspace's HEAD commit, so an uncommitted edit changes nothing and committing it does, while a Rule file outside version control and the request's own turn are never dependencies; a newer response supersedes an older one (SupersedeAgentResponseCommand, the only writer of a `supersedes` link). A requirement update or delete re-evaluates staleness (EvaluateStalenessCommand, dispatched by the requirement service after its commit for the responses that cite the changed requirement; a failed dispatch is logged and never fails the requirement write) only while the Mode is not `Off`; after a return from `Off` every read evaluates freshness itself. Alternate flows: invalid link, cycle, or unresolvable ref refused (an unresolvable ref is 400 `validation_error` naming it); a client request for a `supersedes` link is refused with 400 `validation_error` (`details.field` `type`); an identical link created again returns the stored link with `created: false`; a link a command made (Build's `derived_from`, Submit's `derived_from` and `supports`, Supersede's `supersedes`) cannot be deleted (409 `agent_context_relationship_protected`); a supersede of an original already `Rejected` or superseded by another response is refused with 409 `agent_context_transition_invalid` (ResponseStatusTransition), a supersede whose replacement is invalid or would close a cycle with 409 `agent_context_supersede_invalid`, and a repeat naming the same replacement returns it unchanged.

![Use case: Maintain relationships, staleness, and supersession (use cases), part 1 of 4](c4/rendered/STR-07-05.svg)

View: STR-07-05

![Use case: Maintain relationships, staleness, and supersession (use cases), part 2 of 4](c4/rendered/STR-07-05-part2.svg)

View: STR-07-05-part2

![Use case: Maintain relationships, staleness, and supersession (use cases), part 3 of 4](c4/rendered/STR-07-05-part3.svg)

View: STR-07-05-part3

![Use case: Maintain relationships, staleness, and supersession (use cases), part 4 of 4](c4/rendered/STR-07-05-part4.svg)

View: STR-07-05-part4

### 7.15 Use case: Run a stateless or pilot invocation [FR-MCP-AGENTCONTEXT-008, -009]

Actors: operator, agent host and plugin, model. Precondition: the plugin read the Mode at host session start (GetAgentContextStatusQuery, at SessionStart, or at the first prompt on cline, cline-v2, and opencode, which have no SessionStart event) and cached it as `agentContextMode` in the host session's `turn-state.yaml`; a Mode it cannot read, including a read that misses the SessionStart hook bound, or a value it does not know, is `Off`, and a change away from `Off` takes effect at the next host session start. Basic flow (process RunStatelessInvocation, run by the plugin's prompt hook, with the agent-context surfaces of FR-MCP-AGENTCONTEXT-008): in `Stateless` mode the plugin builds the package for a delivered request and the model answers from the rendered package, the current request, and the deterministic `REQUIRED MEMORIES` block the host already injects (section 3.1, "Memories in Stateless mode", FR-MCP-MEMORY-003, -007), with history excluded, on a host whose P0.2 row records history exclusion (any other host runs `Stateless` as `Pilot`); in `Pilot` mode the package is built and recorded but not injected, and the model answers statefully; the plugin keeps the `packageId` in its per-turn file `turns/<requestId>.yaml`, and the model submits its response with the `agent_context_submit_response` tool before it closes the turn (section 7.12); the pilot report compares the requests by `invocationMode` before any default changes (`Pilot` yields stateful responses, a period in `Stateless` on hosts with history exclusion yields stateless ones) and also counts `Overflow` packages, refused prompts, and requests with a package and no response. On claude-code and claude-cowork, where the arrival bridge is the only minter, the prompt hook first applies pending transcript records through `Sync-PluginArrivalLog` up to the transcript length at its own start and takes the bridge-minted `requestId` of the delivered request. Alternate flows: mode `Off` keeps today's behavior; a build or submission answered 409 `agent_context_disabled` (the Mode switched to `Off` during the host session) makes the plugin cache `Off` for the host session and continue statefully, with no refusal and no `canceled` close; a build failure (operator, 2026-10-11: "Fail the invocation"), any other build error (`transaction_degraded` and `mutation_not_transactional` from a transaction-gated REPL client included, section 3.1) or an outage included, refuses the prompt with the error code and message shown to the operator, produces no answer, and closes the turn `canceled` with that reason (written by the plugin on the persist path of process PersistTurnWriteWithReceipt; Test-ModelEvidence never applies to a `canceled` close); when an outage holds that close in the failsafe, the Stop gate warns degraded persistence naming the package failure, never blocks, and keeps the ledger entry until the drain confirms the close; a pending mint (transport outage) fails the invocation in the same way, and a final mint refusal leaves no turn, so the prompt is refused and the Stop gate warns the refusal naming the prompt; on a bridge host, a delivered request whose `enqueue` record is not yet in the transcript fails the invocation (prompt refused, as for a build failure); a message absorbed into a running turn (`absorbed_mid_turn`) runs as `Pilot`: the package is built and recorded, never injected, and the prompt is never refused; a host recorded `turn_logging_unsupported` (D6) stays stateful; a host whose prompt hook cannot refuse a prompt, or that runs the prompt anyway after a hook timeout, crash, or `plugin_core_unavailable`, treats `Pilot` and `Stateless` as `Off`, with the limitation in its feature-matrix row (P0.2 probe (j)); the pilot report and the other read-only agent-context queries stay readable in `Off`.

The diagram's short labels map to the flows above: "409 disabled" is 409 `agent_context_disabled`; the `canceled` close of a refused prompt is written on the persist path of process PersistTurnWriteWithReceipt; the pilot report is GetAgentContextPilotReportQuery; a bridge host's prompt hook syncs the transcript and takes the bridge-minted `requestId`; an absorbed message builds and records its package and never refuses the prompt.

![Use case: Run a stateless or pilot invocation (use cases), part 1 of 7](c4/rendered/STR-07-06.svg)

View: STR-07-06

![Use case: Run a stateless or pilot invocation (use cases), part 2 of 7](c4/rendered/STR-07-06-part2.svg)

View: STR-07-06-part2

![Use case: Run a stateless or pilot invocation (use cases), part 3 of 7](c4/rendered/STR-07-06-part3.svg)

View: STR-07-06-part3

![Use case: Run a stateless or pilot invocation (use cases), part 4 of 7](c4/rendered/STR-07-06-part4.svg)

View: STR-07-06-part4

![Use case: Run a stateless or pilot invocation (use cases), part 5 of 7](c4/rendered/STR-07-06-part5.svg)

View: STR-07-06-part5

![Use case: Run a stateless or pilot invocation (use cases), part 6 of 7](c4/rendered/STR-07-06-part6.svg)

View: STR-07-06-part6

![Use case: Run a stateless or pilot invocation (use cases), part 7 of 7](c4/rendered/STR-07-06-part7.svg)

View: STR-07-06-part7

### 7.16 Use case: Project responses into GraphRAG [FR-MCP-AGENTCONTEXT-007]

Actors: server (background pass), operator. Basic flow: after a unit commits a status change that marks a projection row (a grade's trust decision, a reject, a supersede, a staleness evaluation), it dispatches the pass (ProjectAgentResponsesCommand with no `responseIds`); a pass selects the `Pending` and `Failed` projection rows, and `Skipped` rows once ingest is enabled again (or the named `responseIds`, validated by ResponseIdList, with unknown ids returned in `notFound`); each selected response's staleness is evaluated first (EvaluateStalenessCommand, dispatched); trusted responses are ingested into GraphRAG's workspace scope, and for a response that stopped being trusted, including one whose dependency changed, a row that holds a GraphRAG document has it deleted and a row that holds none becomes `Removed`; each row is saved through SerializableWrite with its response locked, re-reading the response status after the GraphRAG call, so a reject that commits during an ingest leaves the new document marked for removal on the next pass (section 6.3, projection row states). Alternate flows: `ProjectionEnabled` false skips ingest only, and removal still runs while GraphRAG is enabled; GraphRAG disabled, so every ingest is skipped and a row awaiting removal stays `Pending` until GraphRAG is enabled; an ingest or delete error is recorded (`Failed`) and retried on the next pass, while a delete of a document already gone counts as removed; two passes that overlap keep one projected document per response, and the later save deletes the document it ingested; every pass ends by reconciling the workspace's `agent-response` documents with the projection rows, so a document left by racing ingests, an exhausted save, or a failed delete is removed by the next complete pass (section 6.5, ReconcileProjectedDocuments).

![Use case: Project responses into GraphRAG (use cases), part 1 of 4](c4/rendered/STR-07-07.svg)

View: STR-07-07

![Use case: Project responses into GraphRAG (use cases), part 2 of 4](c4/rendered/STR-07-07-part2.svg)

View: STR-07-07-part2

![Use case: Project responses into GraphRAG (use cases), part 3 of 4](c4/rendered/STR-07-07-part3.svg)

View: STR-07-07-part3

![Use case: Project responses into GraphRAG (use cases), part 4 of 4](c4/rendered/STR-07-07-part4.svg)

View: STR-07-07-part4

## 8. Delivery: four execution phases and parallel domain coding

Operator directive (2026-10-11, verbatim): "I want the implementation plan to be organized into requirements as a phase, interfaces and code stubs as a phase for the entire plan, test-driven development unit tests with mocks as a single phase, and then you can begin implementation coding in a domain-driven design breakdown so that agents can be run in parallel for the coding." This section defines the delivery structure; the plan's "Execution organization" schedules it and cites this section.

### 8.1 Phase order

![Phase order (activity), part 1 of 2](c4/rendered/STR-08-01.svg)

View: STR-08-01

![Phase order (activity), part 2 of 2](c4/rendered/STR-08-01-part2.svg)

View: STR-08-01-part2

The analyzer branch `chore/netanalyzers-all` merges first, before the prerequisites and Phase 1 (section 3.1, "Analyzer rules"; its work is exempt from the code freeze, section 3.1, "Code freeze exception"), so the P0.1 baseline, every Phase 2 type, and every Phase 3 test name are taken under its rules (IDE0130 namespaces, MA0048 one type per file, CA1707 test names without underscores) and at its paths: its commit `fe5879ba` moved `tests/Build.Tests` to `build/Build.Tests` (replaces the `tests/Build.Tests` path that TR-MCP-NUKE-002 names and that the evidence of TR-MCP-NUKE-002, TR-MCP-NUKE-005 to -010, and FR-MCP-PLUGININT-001 cites). Every later arrow is crossed only after that phase's gates are green with zero failures and zero skips, its hostile validation (Codex gpt-6-astra max) returns AGREE at accuracy and completeness of at least 98 with every claim PASS, and the operator approves. Section 8.2 says what each phase delivers.

Published packages: two changes of this plan reach packages that the Nuke `PackNuGet` target publishes. It packs `QBrainAI.Client`, `QBrainAI.Cqrs`, `QBrainAI.Cqrs.Mvvm`, `QBrainAI.Repl.Core`, and `QBrainAI.McpAgent` at one version, `--package-version` or the `next-version` of `GitVersion.yml` (`build/Build.PackNuGet.cs:8-9,15-43,46-74`; 1.4.40 at `a8d3934b`):

- `QBrainAI.Client` grows in Phase 2: the new DTOs (`QBrainAi.Client.Models.SessionLog`, `QBrainAi.Client.Models.AgentContext`), `ClassifiedErrorException` and `ErrorCodes` (`QBrainAi.Client.Errors`), `PageResult<TItem>` (`QBrainAi.Client.Paging`), and `ScoreLexeme` with `ScoreLexemeJsonConverter` (`QBrainAi.Client.Scoring`) become public API beside the existing `McpClientException` family (`src/QBrainAi.Client/McpClientExceptions.cs:15-27`), which stays the typed error the client raises to its callers. Every change is an addition, so its version effect is a minor version.
- `QBrainAI.Cqrs` changes in Phase 4 (SharedComponents): `IDispatcher` becomes the scoped facade of section 3.5.1 ("Dispatch scope") in place of today's singleton registration (`src/QBrainAi.Cqrs/ServiceCollectionExtensions.cs:22-27`). Every public signature stays, but a consumer that resolves `IDispatcher` from the root provider or injects it into a singleton fails under DI scope validation (inference), so its version effect is a major version under semantic versioning.

The first release after Phase 4 publishes both changes. Every `QBrainAI.*` package shares the one version, and FR-MCP-QBRAIN-004 ends the 1.x configuration aliases at 2.0, so the operator sets that release's version at the release (Octopus), and its release notes name both changes.

### 8.2 What each phase delivers

- Prerequisites, after the analyzer branch has merged: every pre-existing failure fixed under its own TODO (P0.1, including the Windows findings), the REPL `generateDocument` validator fix, the Nuke targets every later gate runs through (`Test --test-projects --test-filter`, which also reaches the integration and validation projects of G3, section 8.4; `TestPluginCore`; `TestAgentPlugins`), the port of every bats case to Pester (P0.1b; Pester is the only plugin test runner, operator, 2026-10-11: "You should be designing all scripts and script tools around PowerShell and only PowerShell."), `build/Build.Tests` removed from `MigrationIntegrationTests` (P0.1c, TR-MCP-NUKE-014; the analyzer branch moved the project and still lists it there, `build/Build.TestDependencies.cs:73` on `chore/netanalyzers-all`), BUG-BUILD-005 fixed before the first G5 run, and BUG-BUILD-008 landed at its plugin version 1.119.0 before this plan's sync (both in section 8.4).
- Phase 1, requirements (the plan's P0 steps 1-9): the whole catalog of Phases A, B, and C created in the requirement store from this architecture and the plan's requirement catalog, the use cases, the feature matrix rows, and the phase and domain TODOs (`REQ-MERGEDPLAN-001`, `DESIGN-MERGEDPLAN-001`, `TEST-MERGEDPLAN-001`, one `IMPL-<CONTEXT>-001` per bounded context, `VERIFY-MERGEDPLAN-001`).
- Phase 2, interfaces and code stubs: every command and query record with a handler stub, every policy and seam interface with a stub, every section 3.5 component with its Phase 2 seam and a stub (the two CQRS behaviors registered as pass-through stubs that call the next handler, and `IDispatcher` kept at today's registration until SharedComponents changes it, so existing behavior stays live), every new entity class and its `IEntityTypeConfiguration<T>` as compiling, unregistered classes (no `DbSet`, no registration in `McpDbContext`, and no new property on an existing entity, because `McpDbContext.cs:301-308` ignores the pending-model-changes warning, so a registered model change without its migration would make queries select columns the database lacks and regress the existing tests that call `Migrate()`; the registered model change, the three migrations, and the S3 backfill belong to the Phase 4 context StorageSchema), every DTO and client mirror with JSON context registration, every controller route, MCP tool, client method, and REPL method (they only dispatch, and each controller route and MCP tool maps its unit's typed failures to the bodies of the unit's error list, section 8.3), every new plugin function and tool signature in `plugins/core/lib-ps`, the options classes and `appsettings.yaml` sections, DI, and the golden contract files. Stubs return neutral values and never throw, so Phase 3 tests fail on assertions. Existing behavior stays live: the handlers of the units that wrap today's routes are delegating handlers that call today's `ISessionLogService` members unchanged, so every existing route, tool, REPL method, and plugin path behaves as today and the existing suites stay green; only the new members and new units are stubs, reached by no existing path, and each Phase 4 context switches its delegating handlers to the new members when its Phase 3 tests go green. No plugin repo changes before Phase 4: the TypeScript hosts keep their current adapters and Jest suites until HostPluginAdoption replaces both in one change after the sync (A9).
- Phase 3, unit tests with mocks: every unit test the catalog names, written from the acceptance criteria, recorded RED against the stubs, then green against mocks with the oracle and mutant pattern and a recorded negative check each. A Phase 2 change that completes behavior rather than stubbing it (the `agent_context` federation registration, FR-MCP-103-AC006) records its case red in Phase 2, before the change, and Phase 3 keeps it green (section 3.1, Test placement). Each section 3.5 component gets its one test class (for example `SerializableWriteTests`); a unit's tests mock the component's Phase 2 seam and never test the component's own branches again.
- Phase 4, implementation: each bounded context replaces its stubs with the real implementation until its Phase 3 tests pass unchanged, adds its integration tests, and runs its gates; then the contexts merge in the rollout order and the integration gates, live checks, and phase-wide HV run.

### 8.3 Bounded contexts (Phase 4 work packages)

The diagram draws merge order only: a context starts when every context with an arrow into it has merged. StorageSchema (the registered model change, the migrations for three providers, the S3 backfill) runs first and alone; SharedComponents (sections 3.5.1 and 3.5.2, with the `QBrainAi.Cqrs` dispatch scope) runs second and alone; then the six server contexts run in parallel. PluginCorePowerShell and BuildTooling call no C# component, so they run in parallel from the start of Phase 4; HostPluginAdoption follows both, and Documentation runs last. Calls between contexts go through the callee's Phase 2 seam and never wait for the callee to merge, so they draw no arrow; they are listed after the context bullets below.

![Bounded contexts in merge order, part 1 of 5](c4/rendered/STR-08-02.svg)

View: STR-08-02

![Bounded contexts in merge order, part 2 of 5](c4/rendered/STR-08-02-part2.svg)

View: STR-08-02-part2

![Bounded contexts in merge order, part 3 of 5](c4/rendered/STR-08-02-part3.svg)

View: STR-08-02-part3

![Bounded contexts in merge order, part 4 of 5](c4/rendered/STR-08-02-part4.svg)

View: STR-08-02-part4

![Bounded contexts in merge order, part 5 of 5](c4/rendered/STR-08-02-part5.svg)

View: STR-08-02-part5

Ownership rule: Phase 2 completes every shared file except the registered model change and the migrations (`McpClientJsonContext`, the new DTOs in `src/QBrainAi.Client/Models/SessionLog/` and `src/QBrainAi.Client/Models/AgentContext/` and the Phase B fields on the existing DTOs, REPL allow-lists and validators, `ClientMutationPolicy` (its agent-context entries and the `Evaluate` change they need, which drops the `!ReadMethods.ContainsKey(clientName)` clause at `src/QBrainAi.Repl.Core/ClientMutationPolicy.cs:213` at `a8d3934b`, section 3.1, "Transaction-gated REPL clients", and section 6.5; no namespace is in both lists today, so no existing decision changes and the existing policy tests stay green), DI with the registration of the two CQRS behaviors, configuration and `AgentContextOptions` (`src/QBrainAi.Services/AgentContext/AgentContextOptions.cs`), controllers, MCP tools, client methods, golden contracts, the `agent_context` federation domain registration (section 6.1, FR-MCP-103), the unregistered entity and configuration classes (the agent-context entities in `src/QBrainAi.Storage/Entities/AgentContext/`), and the dispatch of EvaluateStalenessCommand, made only while Mode is not `Off` (section 3.5.1), from the requirement write paths of `RequirementsDatabaseDocumentService` after their commit (`src/QBrainAi.Services/Requirements/`; section 3.1, Requirement writes and staleness)), so each context edits only its own files. The surface error mapping is one of these shared files: Phase 2 writes, completely, how every controller route and MCP tool maps its unit's typed failures to the bodies of the unit's error list (today's body where the unit keeps it, otherwise the `ClassifiedError` body), with one golden case per route in Phase 3 (RED against the stubbed handler, green against a mocked dispatcher). Its owner is therefore the Phase 2 work package (`DESIGN-MERGEDPLAN-001`): no Phase 4 context edits a surface file, and a context whose unit needs another mapping raises a Phase 2 amendment. A new type's namespace is its project's RootNamespace followed by its folder (section 3.5: `src/QBrainAi.Services/<Folder>/` is `QBrainAi.Support.Mcp.<Folder>`, `src/QBrainAi.Storage/<Folder>/` is `QBrainAi.Support.Mcp.Storage.<Folder>`, and `QBrainAi.Client`, `QBrainAi.Cqrs`, and `QBrainAi.Repl.Core` keep their assembly names), and every command or query record and its handler are two files (MA0048), so the folders named below give each context's namespaces; section 3.5 names each component's owner:

- StorageSchema: the registered `McpDbContext` model change (new `DbSet`s and configuration registrations, the new properties on `SessionLogTurnEntity`, the `ResolveWorkspaceIdForAddedEntity` cases), the dump policy of each new `DbSet` in `WikiDumpTablePolicyRegistry` (section 6.1, FR-MCP-WIKIEXPORT-003; in the same change as the sets, because `tests/QBrainAi.Support.Mcp.Tests/Services/WikiDumpG7OverlayTests.cs:56-71` requires a policy for every registered set), the three migrations per provider, and the S3 backfill.
- SharedComponents: the components of sections 3.5.1 and 3.5.2 and their folders: the `QBrainAi.Cqrs` dispatch scope (the scoped `IDispatcher` facade in `src/QBrainAi.Cqrs/`, published as `QBrainAI.Cqrs`, section 8.1), `WorkspaceResolutionBehavior` (`src/QBrainAi.Services/Workspaces/`), `ModeGateBehavior` and `IModeGatedRequest` (their two files in `src/QBrainAi.Services/AgentContext/`), `DbErrorClassifier` (`src/QBrainAi.Storage/Errors/`), `SerializableWrite` (`src/QBrainAi.Storage/Transactions/`), `IdempotentCreate` and `PayloadFingerprint` (`src/QBrainAi.Storage/Idempotency/`), `ReadableKeyAllocator` (`src/QBrainAi.Storage/Keys/`), `PagedQuery` and `PageBounds` (`src/QBrainAi.Storage/Paging/`), `PageResult<TItem>` (`src/QBrainAi.Client/Paging/`), `ClassifiedErrorException` and `ErrorCodes` (`src/QBrainAi.Client/Errors/`) with the one `ClassifiedErrorException` branch of `McpErrorClassifier` (`src/QBrainAi.Support.Mcp/Services/McpErrorClassifier.cs:14`) and the REPL classifier's pass-through of a server `ClassifiedError` (code, message, retryable, details) at every status (`src/QBrainAi.Repl.Core/ReplMcpErrorClassifier.cs:60-95`, whose conflict branch forces `retryable: false` today at `:60-68` and which passes the server's code through only for a 503 at `:70-95`, section 3.5.2), `SecretRedactor` (`src/QBrainAi.Services/Redaction/`), and `ContentHasher` (`src/QBrainAi.Services/Hashing/`), each with its one test class. It runs alone, right after StorageSchema (whose registered model its three-provider cases use) and before every other server context and ReplAndAgents; its gate runs only the cases its own components complete (section 8.4); PluginCorePowerShell, BuildTooling, and HostPluginAdoption call no C# component and keep their own order.
- SessionTurnLogging: the section 3.5.3 components in `src/QBrainAi.Services/SessionLog/` (`TurnStatus`, `TurnLocator`, `TurnWritePipeline`, `TurnReadProjection`, and `ITerminalTurnOutcomeGuard` with the TerminalTurnOutcomeGuard implementation); `SessionLogService.cs` and `TransactionGatedSessionLogService.cs`, including every review-related change in them (the S4 service wiring of the review seams, the review sections in `TurnSection` and `ParseSection`, default records at turn creation, the Chore flip, include chains, graph paths, and the S10 write-origin scope with the gate call next to `ValidateTerminalTurnCompliance`); the record and handler files in `src/QBrainAi.Services/SessionLog/Commands/` and `src/QBrainAi.Services/SessionLog/Queries/` of MintSessionTurnCommand, SubmitSessionLogCommand, UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, FailSessionTurnCommand, OpenSessionLogCommand, AppendTurnDialogCommand, SetTurnTitleCommand, SetSessionTitleCommand, GetSessionLogQuery, and QuerySessionLogsQuery (each switched from its Phase 2 delegating form to the new members at its activation point); the minter; and the nine-entry `agent_plugins` marker contracts in `MarkerFileService.cs` (A9 server part).
- TurnReviews: the section 3.5.4 components (`ScoreLexeme` and `ScoreLexemeJsonConverter` in `src/QBrainAi.Client/Scoring/`, `ScoreVerdict` in `src/QBrainAi.Services/Scoring/`, `ReviewDefaults` in `src/QBrainAi.Services/Reviews/`), the review mapper, validator, request-class normalizer, and threshold provider, the `SessionLogSanitizer` review paths (through `SecretRedactor`), the RecordSessionTurnReviewCommand and ResetSessionTurnReviewCommand record and handler files in `src/QBrainAi.Services/Reviews/Commands/`, and the RequestClassification, HvVerdictMapping, and CompletionGate implementations, with the CompletionGate enforcement change set of section 8.5; the S1 review DTOs in `src/QBrainAi.Client/Models/SessionLog/` are Phase 2 shared files. SessionTurnLogging calls these only through the Phase 2 seams, so the two contexts run in parallel.
- ContextPackages: `SourceResolver` and `SourceKind` (`src/QBrainAi.Services/AgentContext/Sources/`; `SourceResolver.ReadCommit` is the commit reader), `PackageAssembler`, `CandidateRanker`, and `BuiltPackageLookup` with the candidate sources, renderer, and token estimator (`src/QBrainAi.Services/AgentContext/Packages/`), and the record and handler files of GetAgentContextStatusQuery, BuildAgentContextPackageCommand, GetAgentContextPackageQuery, QueryAgentContextPackagesQuery, ReplayAgentContextPackageQuery, and RetrieveAgentContextCandidatesQuery (with its relational path) in `src/QBrainAi.Services/AgentContext/Packages/Commands/` and `src/QBrainAi.Services/AgentContext/Packages/Queries/`. The secret filter is `SecretRedactor` (SharedComponents).
- ResponsesAndTrust: `ResponseStatusTransition`, `IResponseStalenessRule` with its implementation (the Phase 2 seam that ContextPackages and ContextGraph call read-only), `ResponseIdList`, the grader, and the receipt verifier (`src/QBrainAi.Services/AgentContext/Responses/`), and the record and handler files of SubmitAgentResponseCommand, GetAgentResponseQuery, QueryAgentResponsesQuery, RunDeterministicGradeCommand, RecordModelGradeCommand, DecideAgentResponseTrustCommand, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand, CreateAgentContextHvRequestCommand, GetAgentContextHvRequestQuery, QueryAgentContextHvRequestsQuery, and GetAgentContextPilotReportQuery in `src/QBrainAi.Services/AgentContext/Responses/Commands/` and `src/QBrainAi.Services/AgentContext/Responses/Queries/`.
- ContextGraph: `RelationshipWriter` and the GraphRAG retrieval path behind the retrieval seam (`src/QBrainAi.Services/AgentContext/Graph/`), and the record and handler files of CreateAgentContextRelationshipCommand, DeleteAgentContextRelationshipCommand, QueryAgentContextRelationshipsQuery, and ProjectAgentResponsesCommand in `src/QBrainAi.Services/AgentContext/Graph/Commands/` and `src/QBrainAi.Services/AgentContext/Graph/Queries/`.
- ReplAndAgents: the REPL normalizer (which calls `ScoreLexeme`) and workflow, including `importRecovery`'s report of the turns the completion gate refuses (`src/QBrainAi.Repl.Core/SessionLogWorkflow.cs:326`), the McpAgent workflow files named by S6 and S7, and the optional `requestClass`, `agentReview`, and `hostileReview` arguments of QBAgent's `mcp_session_turn_complete` (`src/QBrainAi.Support.Mcp/Services/QuadBrainInternalToolExecutor.cs:314-328`; section 3.1, "Completion gate writers").
- PluginCorePowerShell owns `plugins/core/lib-ps` (including the section 3.5.6 functions in `plugins/core/lib-ps/repl-invoke.ps1`, the arrival bridge's own script entry, and the processes BindVerbToTurn and PersistTurnWriteWithReceipt), `plugins/core/test-fixtures` (the core Pester suites and recorded host payload fixtures), `plugins/core/hooks-templates` (the grok-bot wrappers, which the generator's host list lacks today, `plugins/core/hooks-templates/generate-wrappers.ps1:8`, and the codex `session-end` wrapper, which its codex branch does not write today, `:24-34`), and `tests/QBrainAi.PluginIntegration.Tests` (the `GrokBot` host kind and the rewritten canonical flow, section 4.1); BuildTooling owns `build/`, including the infrastructure suite `build/Build.Tests` (moved from `tests/Build.Tests` by the analyzer branch, section 8.1); HostPluginAdoption owns the nine plugin repos; Documentation owns `docs/` and `templates/`.

Cross-context calls, each through the callee's Phase 2 seam so the contexts run in parallel: every server context and ReplAndAgents calls the SharedComponents components; SessionTurnLogging calls the TurnReviews policies and `ReviewDefaults`; every unit that names a session or a turn calls `TurnLocator` (SessionTurnLogging); ContextPackages calls the GraphRAG retrieval seam and `RelationshipWriter` (ContextGraph) and `IResponseStalenessRule` (ResponsesAndTrust, read-only); ResponsesAndTrust calls `BuiltPackageLookup` and `SourceResolver` (ContextPackages), `RelationshipWriter` (ContextGraph), and `ScoreVerdict` (TurnReviews); ContextGraph calls `SourceResolver`, `IResponseStalenessRule` (read-only), and `ResponseIdList`, and dispatches EvaluateStalenessCommand through `IDispatcher`; ResponsesAndTrust dispatches ProjectAgentResponsesCommand (ContextGraph) after its status commits (the projection pass, section 3.1); the requirement service (`RequirementsDatabaseDocumentService`) dispatches EvaluateStalenessCommand (ResponsesAndTrust) after its requirement writes commit; GetAgentContextPilotReportQuery (ResponsesAndTrust) reads the turns closed with an `agent_context_invocation_failed` `failureNote` through SessionTurnLogging's read seam; ReplAndAgents calls `ScoreLexeme` (TurnReviews).

A context that needs a change outside its files stops and raises a Phase 2 amendment instead of editing another context's files.

#### Process RunPlanPhases: run the four phases

![Process RunPlanPhases: run the four phases (activity), part 1 of 3](c4/rendered/RunPlanPhases.svg)

View: RunPlanPhases

![Process RunPlanPhases: run the four phases (activity), part 2 of 3](c4/rendered/RunPlanPhases-part2.svg)

View: RunPlanPhases-part2

![Process RunPlanPhases: run the four phases (activity), part 3 of 3](c4/rendered/RunPlanPhases-part3.svg)

View: RunPlanPhases-part3

##### Nested activity: RunOnePlanPhase (loop body of RunPlanPhases)

![Nested activity: RunOnePlanPhase (loop body of RunPlanPhases) (activity), part 1 of 2](c4/rendered/RunOnePlanPhase.svg)

View: RunOnePlanPhase

![Nested activity: RunOnePlanPhase (loop body of RunPlanPhases) (activity), part 2 of 2](c4/rendered/RunOnePlanPhase-part2.svg)

View: RunOnePlanPhase-part2

##### Nested activity: ValidateAndApprove (used by RunOnePlanPhase and RunPlanPhases)

![Nested activity: ValidateAndApprove (used by RunOnePlanPhase and RunPlanPhases) (activity), part 1 of 3](c4/rendered/ValidateAndApprove.svg)

View: ValidateAndApprove

![Nested activity: ValidateAndApprove (used by RunOnePlanPhase and RunPlanPhases) (activity), part 2 of 3](c4/rendered/ValidateAndApprove-part2.svg)

View: ValidateAndApprove-part2

![Nested activity: ValidateAndApprove (used by RunOnePlanPhase and RunPlanPhases) (activity), part 3 of 3](c4/rendered/ValidateAndApprove-part3.svg)

View: ValidateAndApprove-part3

##### Nested activity: RunPhase4WorkPackages (loop of RunPlanPhases)

A context is ready when every context with an arrow into it in section 8.3 has merged: StorageSchema first, then SharedComponents, before the server contexts; PluginCorePowerShell and BuildTooling from the start; HostPluginAdoption after both; Documentation last.

![Nested activity: RunPhase4WorkPackages (loop of RunPlanPhases) (activity)](c4/rendered/RunPhase4WorkPackages.svg)

View: RunPhase4WorkPackages

#### Process RunDomainCodingAgent: one Phase 4 work package

The coding agent's brief names the context's files, stubs, and tests, the section 3.5 components it calls through their Phase 2 seams (never re-implemented), and the standing rules.

![Process RunDomainCodingAgent: one Phase 4 work package (activity), part 1 of 2](c4/rendered/RunDomainCodingAgent.svg)

View: RunDomainCodingAgent

![Process RunDomainCodingAgent: one Phase 4 work package (activity), part 2 of 2](c4/rendered/RunDomainCodingAgent-part2.svg)

View: RunDomainCodingAgent-part2

##### Nested activity: ImplementBoundedContext (loop of RunDomainCodingAgent)

![Nested activity: ImplementBoundedContext (loop of RunDomainCodingAgent) (activity)](c4/rendered/ImplementBoundedContext.svg)

View: ImplementBoundedContext

##### Nested activity: ImplementationPass (loop body of ImplementBoundedContext)

![Nested activity: ImplementationPass (loop body of ImplementBoundedContext) (activity), part 1 of 4](c4/rendered/ImplementationPass.svg)

View: ImplementationPass

![Nested activity: ImplementationPass (loop body of ImplementBoundedContext) (activity), part 2 of 4](c4/rendered/ImplementationPass-part2.svg)

View: ImplementationPass-part2

![Nested activity: ImplementationPass (loop body of ImplementBoundedContext) (activity), part 3 of 4](c4/rendered/ImplementationPass-part3.svg)

View: ImplementationPass-part3

![Nested activity: ImplementationPass (loop body of ImplementBoundedContext) (activity), part 4 of 4](c4/rendered/ImplementationPass-part4.svg)

View: ImplementationPass-part4

### 8.4 Gates

- G1 targeted tests through `./build.ps1 Test --test-projects --test-filter`, G2 the unit gate (`./build.ps1 Test`), G3 integration suites (next bullet), G4 the PowerShell plugin core through `./build.ps1 TestPluginCore`, G5 config and traceability (`./build.ps1 ValidateConfig`, `./build.ps1 ValidateTraceability`), G6 the plugin suites through `./build.ps1 TestAgentPlugins`: Pester in the six PowerShell-host repos (selected with `--plugins`) for every exit before HostPluginAdoption, because the TypeScript hosts keep their Jest suites until HostPluginAdoption replaces them, and Pester in all nine repos from HostPluginAdoption on, including the phase-wide Phase 4 exit. Every build and test step is a Nuke target run natively on Linux or Windows; Build.Tests is the one exception (last bullet but one).
- G3 runs through Nuke only: `./build.ps1 MigrationIntegrationTests --skip InstallTestDependencies` for the three-provider cases, `./build.ps1 PluginSessionLogIntegration` for the plugin integration suite, and `./build.ps1 Test --test-projects <project> --test-filter <filter>` for `QBrainAi.Support.Mcp.IntegrationTests`, `QBrainAi.SessionLog.Validation`, `QBrainAi.Repl.IntegrationTests`, and `QBrainAi.Acid.IntegrationTests`. Named prerequisite (P0.1a, TR-MCP-NUKE-011): no Nuke target runs those four projects today, because `Test` leaves out every `*.IntegrationTests` and `*.Validation` project and filters out `Category=Integration`, the trait the integration projects' classes carry (`build/Build.Test.cs:16-24,50`, `build/Build.TestDependencies.cs:60-61`), and `MigrationIntegrationTests` runs only `QBrainAi.Support.Mcp.Tests` and Build.Tests (`build/Build.TestDependencies.cs:70-74`). So `--test-projects` also selects a project that `Test` leaves out by default (never Build.Tests), and for such a project the default category filter drops its `Category!=Integration` term; TR-MCP-NUKE-011 AC-001 and AC-003 are updated to say so.
- Component tests and the gate that runs each case: each C# component of section 3.5 has one test class (for example `SerializableWriteTests`) that runs in G2, with its three-provider cases (SQLite, PostgreSQL, SQL Server) in G3; the section 3.5.6 plugin core functions have their Pester tests in G4. A context's gate runs only the cases that its own code and the contexts merged before it complete, so a case that needs two contexts runs at the gate of the one that merges later in the rollout order (section 8.5), on its branch with the earlier one merged. SharedComponents' exit runs, green, before any context that calls its components starts: its components' own cases, among them `SerializableWriteRetriesExhausted409ConflictRetryableNothingSaved` and a forced interleaving of two writes to one test entity (`SerializableWriteForcedInterleavingTestEntityCommitsWhollyBeforeOrAfter`), each on the three providers, the dispatch-scope cases (`ExistingHandlersProductionPipelineBehaviorUnchanged`, `DispatchNestedSharesDbContextAndTransaction`, `DispatchParallelRequestsTwoWorkspacesNoSharedDbContext`), and the workspace-resolution, IdempotentCreate, ReadableKeyAllocator, ContentHasher, and REPL classifier cases of sections 3.5.1 and 3.5.2. The section 3.5 cases that need later contexts run at those contexts' gates: `TurnWriteConcurrentDifferentTurnsSameSessionBothCommitNoConflictAnswer` (TurnWritePipeline) at SessionTurnLogging's; `TurnWriteForcedInterleavingChoreFlipVsRunningWriteRunningKept` (TurnWritePipeline with the review units and ReviewDefaults) at TurnReviews'; `TurnWriteForcedInterleavingReviewResetVsCompleteNeverClosedWithoutReview` and `TurnWriteForcedInterleavingClassChangeVsCompleteGateSeesCommittedClass`, which need the enforced completion gate, at the gate of TurnReviews' CompletionGate enforcement change set (section 8.5); `BuildOnSqliteHookWriteDuringRetrievalNotBlocked` (Build with a session-log write) at ContextPackages'; and `ReadableKeyAllocatorSuffixPast99WidensLatestGradeOrderKept` (the suffix with the grade order of section 6.3) at ResponsesAndTrust's. By the same rule, `ReviewReasonRootIdsEqualItsTurnKey` (section 5.1: the review reason rows written on StorageSchema's registered model) runs at TurnReviews' gate.
- Plugin host cases (section 3.2): the wrapper generator (`plugins/core/hooks-templates/generate-wrappers.ps1`, PluginCorePowerShell, section 8.3), whose codex branch writes no `session-end` wrapper today (`:24-34` at `a8d3934b`) and whose host list lacks grok-bot (`:8`), writes the codex `session-end` wrapper that A9's codex `hooks/hooks.json` SessionEnd entry calls, and the grok-bot wrappers; its output is checked in G4, as its generated grok manifest is today (TEST-MCP-BUGTRIAGE-059, `plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1:2221`), and the sync writes it into each repo (`build/Build.SyncAgentPlugins.cs:41,236`). Each TypeScript host's adapter suite (`tests/TypeScriptHostAdapter.Tests.ps1`, section 3.2), with the required-memory cases `ClineV2BeforeModelForwardedRequiredMemoriesAddedOnce` and `OpenCodeChatMessageOutputCarriesRequiredMemories` (FR-MCP-MEMORY-007), runs in G6 from HostPluginAdoption on, on Linux and Windows.
- Agent context Mode in the gates: every suite runs with `Mcp:AgentContext:Mode` `Off` (the default) except the agent-context cases, which set the Mode per case. In `Off` every existing suite passes unchanged (stateful behavior), every gated agent-context command answers 409 `agent_context_disabled`, and the read-only agent-context queries answer (ModeGateBehavior gates commands only, section 3.5.1); the plugin core cases assert that a build or submission answered 409 `agent_context_disabled` caches `Off` for the host session and continues statefully, with no refusal and no `canceled` close.
- Test churn is carried by the change that causes it, as a step of that change's gate and never as an exclusion (the counts are heuristic counts at `a8d3934b`):
  - The CompletionGate enforcement change set: about 196 existing tests in 43 files write a `completed` or `failed` turn without a class or reviews, at least 120 of them on the real server path; they migrate in that change set through the per-project `WithCompletionEvidence(turn, requestClass)` fixture helper, which adds a class and terminal reviews and never weakens an assertion. Eight assert an outcome the gate reverses or a body it preempts and are rewritten to the gated outcome (`tests/QBrainAi.Support.Mcp.Tests/Services/SessionLogServiceTests.cs:538,1230,1261`, `tests/QBrainAi.Support.Mcp.IntegrationTests/Controllers/SessionLogControllerTests.cs:386,420,701,733,777`). `tests/QBrainAi.Support.Mcp.Tests/Services/SessionLogServiceReplaceDeleteTests.cs:26,61` replace a turn with `completed`, so from that change set on they stay green only through the helper, which is how FR-SUPPORT-016's baseline-green evidence holds at every later gate. The PluginIntegration scenarios that complete turns record a class and reviews first, and QBAgent's `mcp_session_turn_complete` and REPL `importRecovery` follow section 3.1 ("Completion gate writers").
  - PluginCorePowerShell, in G4 and in G3's `PluginSessionLogIntegration`: about 25 drain cases (TEST-MCP-REPL-031 to -039, TEST-MCP-REPL-037's first-success drain among them, TEST-MCP-FAILSAFE-001, and the quarantine repair cases) follow the drain rules of section 3.1 ("Failsafe drain") and section 4.4 (process DrainFailsafe): the TEST-MCP-REPL and TEST-MCP-FAILSAFE cases are rewritten to them, and the quarantine repair cases keep their outcome, because the drain still repairs and replays the exhausted numeric-keyed legacy records and nothing replays from the quarantine subdirectory (`plugins/core/test-fixtures/pester/SessionLogQuarantineRepair.Tests.ps1:20`, FR-MCP-SESSIONLIFE-004); the background-brief prompt cases (`TriagePluginIdentity.Tests.ps1:290,327,420`, whose prompts today's text detection reuses as a background brief) expect a new turn where they expect reuse or `turn-already-open` today and keep their no-cancel expectations (section 5.4, FR-MCP-TRIAGEPLUGIN-001); the verb tests that seed the workspace-level `current-turn.yaml` (about 120 references in 7 core Pester files) are re-fixtured to a host session key and the per-turn files, `current-turn.yaml` being the read-only legacy file of section 4.1; and the `beginTurn` and local `openSession` verb cases (`plugins/core/test-fixtures/pester/SessionLogTurnContextBeginTurn.Tests.ps1:133-179`, `TriagePluginIdentity.Tests.ps1:165`, and in the host repos `tests/ReplMethodTimeout.Tests.ps1:28` and the `skills.bats` cases the Pester port carries), the canonical PluginIntegration flow (`tests/QBrainAi.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs:121-196`), and `TEST-MCP-BUGTRIAGE-068` change to the fates section 4.1 states.
  - SessionTurnLogging: the two develop tests whose expected outcome becomes the 409 `conflict` with `retryable: true` (`tests/QBrainAi.Support.Mcp.Tests/Services/SessionLogDenseGraphTests.cs:100`, `tests/QBrainAi.Support.Mcp.Tests/Services/SessionLogSupersessionTests.cs:216`; section 4.2, storage rules).
  - BuildTooling, in the Build.Tests infrastructure step: the sync's open-turn cache guard `HasOpenPluginTurn` (`build/Build.SyncAgentPlugins.cs:666,696-726` at `a8d3934b`, which today scans `current-turn.yaml`) reads `openDelivered` of every per-host-session `turn-state.yaml` as well as the legacy file (section 4.1), so the guard case `ReplacePluginCache_OpenTurn_RetainsExistingCache` (`tests/Build.Tests/BuildTargetTests.cs:569` at `a8d3934b`, in `build/Build.Tests` after the analyzer merge) keeps its outcome with a per-host-session seed; the change is Nuke behavior, accountable only through TR-MCP-NUKE-003 under FR-MCP-BUILD-001 (process SyncAgentPluginsAtVersion, section 4.4).
- G5 configuration, named prerequisite BUG-BUILD-005: `ConfigValidator` recognizes only a root `Mcp:` line (`build/ConfigValidator.cs:9-10`), while the live `appsettings.yaml` root is `QBrainAi:` (`src/QBrainAi.Support.Mcp/appsettings.yaml:46`), so `./build.ps1 ValidateConfig` fails today ("Missing 'Mcp' section", `build/Build.ValidateConfig.cs:21-22`). BUG-BUILD-005 is fixed before the first G5 run; TR-MCP-NUKE-004 then checks the review thresholds under both roots, canonical `QBrainAi:` first, with their per-instance overrides (TR-MCP-NUKE-008, section 5.1). The `AgentContext` ranges of section 6.2 are not a G5 check: `AgentContextOptions.Validate()` with `ValidateOnStart()` refuses a value outside them at server start (section 6.2), its G2 cases prove it, and TR-MCP-NUKE-004 states that exclusion.
- Plugin sync steps (BuildTooling, TR-MCP-NUKE-003; process SyncAgentPluginsAtVersion, section 4.4):
  - Scope of `ValidatePluginPowerShellOnly` (AC-005): an extension rule over every tracked path, and its content rule only over the packaged paths it scans today (`lib/`, `hooks/`, `skills/`, `plugin.json`, `CORE-MANIFEST.yaml`; `build/Build.SyncAgentPlugins.cs:405-433`), because the content rule over every tracked path fails 41 tracked documents and tests in the nine repos today (each `ENFORCEMENT.md`, most `README.md` files, and `tests/Skills.Tests.ps1`, whose own assertion text names bash).
  - Order: the sync validates each repo it writes with that packaged-path check (`ValidatePluginPowerShellOnlyPackage`, `build/Build.SyncAgentPlugins.cs:244-245,405-433`), which all nine pass. The tracked-tree check of a TypeScript host runs at HostPluginAdoption's exit, because the host keeps its TypeScript logic, Jest suites, and vendored Node core until HostPluginAdoption's change removes them; a tracked-tree check at the sync would fail those three hosts before their adoption can start.
  - Installed caches: SyncAgentPlugins ends by writing the synced repos into the installed host caches of the synced version (`RefreshKnownPluginCaches`, `build/Build.SyncAgentPlugins.cs:71,652-662`), so the sync is the installed-equals-synced step of section 8.5 and runs only there, after the server deploy, and as a same-version re-sync; no gate runs it.
  - Version, named prerequisite: 1.119.0 already names the unpushed BUG-BUILD-008 branches of eight plugin repos and three installed caches on this host, so BUG-BUILD-008 lands at 1.119.0 first and this plan syncs once at 1.120.0, which no branch or cache carries.
- Build.Tests (`build/Build.Tests`) is infrastructure, never a Nuke target and never in G2: it runs directly (`dotnet test build/Build.Tests`) as the infrastructure check when `build/` changes, the one exception to the Nuke rule (operator, 2026-10-11: "Nuke does not run its own test.").
- Windows evidence runs natively on PAYTON-LEGION2 through the operator's WSMan route in a worktree of `F:/GitHub/McpServer`; Linux evidence runs natively on LAB-OMARCHY.

### 8.5 Rollout and rollback

- Merge and deploy order: StorageSchema, SharedComponents, SessionTurnLogging, TurnReviews without the CompletionGate enforcement change set, ContextPackages, ResponsesAndTrust, ContextGraph, ReplAndAgents; deploy the server (agent context Mode `Off`: the plugins stay stateful, every gated agent-context command answers 409 `agent_context_disabled`, and the read-only agent-context queries answer); merge PluginCorePowerShell, BuildTooling; sync all nine plugins once at one explicit version (`./build.ps1 SyncAgentPlugins --agent-plugin-parent /home/sharpninja/github --plugin-version 1.120.0`, after BUG-BUILD-008 has landed at 1.119.0, section 8.4; it carries the Phase A, B, and C plugin work together; the installed-equals-synced step, which the sync's own refresh of the installed caches of that version begins, and the operator's codex hook trust follow, and a same-version re-sync, which refreshes content and never bumps the version, runs only when a later Phase 4 fix changes `plugins/core/lib-ps`); merge HostPluginAdoption, whose exit runs `./build.ps1 ValidatePluginPowerShellOnly --agent-plugin-parent /home/sharpninja/github` over the tracked trees of all nine repos (section 8.4); then `MAINT-LIBSHRETIRE-001`, and, once Codex ACKs the 2026-10-12 PowerShell-only CONTRACT entry or the operator resolves it, `MAINT-LIBNODERETIRE-001`; merge and deploy the CompletionGate enforcement change set, whose deploy receipt lists the workspace's turns in progress at that moment (QuerySessionLogsQuery, status `in_progress`): the gate refuses their completion until each carries the items it names, the Stop gate remedy names them, and nothing is backfilled (section 3.1, Turns open at the CompletionGate deploy); Documentation; then the integration checks.
- Migrations: StorageSchema's three migrations per provider run Up, Down, and Up again on SQLite, PostgreSQL, and SQL Server in G3 (`./build.ps1 MigrationIntegrationTests`), with the schema and the data equal after the second Up (`MigrationsUpDownUpThreeProvidersSchemaAndDataEqual`); a migration rolls back only through that tested Down.
- Binary-only rollback (`UpdateService` to the prior build while the schema stays at the new migrations): the new required review status columns carry the database default `Unstarted`, and `RequestClass` the database default `Unclassified`, so a turn the prior binary inserts during the rollback reads as its ReviewDefaults after the roll-forward, which runs no second backfill (`RollbackPriorBinaryInsertsTurnRollForwardReadsDefaults`); the Phase C tables are new, and the prior binary never writes them.
- Readable keys across workspaces: every readable key is per workspace (section 6.3, a composite key with `WorkspaceId`), so two workspaces that hold the same legacy request id get the same key string on two rows, with no collision and no retry exhaustion, before and after the rollout (`ReadableKeyAllocatorSameParentIdTwoWorkspacesSameKeyNoCollisionNoExhaustion`).
- On the shared servers, Mode `Pilot` only on the operator's instruction (the agent-context live check runs on its own instance, section 8.6). A change away from `Off` takes effect at each host session's next start, and only on hosts whose prompt hook can refuse a prompt (P0.2 probe (j)); any other host, and a host that runs the prompt anyway after a hook timeout, crash, or `plugin_core_unavailable`, runs `Pilot` and `Stateless` as `Off` with the limitation in its feature-matrix row. The pilot report goes to the operator: `Pilot` yields stateful responses with packages built and graded, a period in `Stateless` on hosts with history exclusion yields stateless responses, and the report compares them by `invocationMode` and also counts `Overflow` packages, refused prompts, and requests with a package and no response. `Stateless` as a default is a later operator decision taken from that report.
- Server deploys run through the Nuke `UpdateService` target with operator approval; releases use Octopus only. Rollback: Mode `Off` stops agent-context behavior without a deploy, and `Off` means stateful continuation: a running host session whose plugin cached `Pilot` or `Stateless` gets 409 `agent_context_disabled` at its next build or submission, caches `Off`, and continues statefully (no refusal, no `canceled` close; every other build error still fails the invocation), and a plugin that cannot read the Mode at host session start, including a read that misses the SessionStart hook bound, assumes `Off`. The stored packages, responses, grades, relationships, HV requests, and the pilot report stay readable in `Off` for audit. Schema rollback follows the migrations bullet above.

### 8.6 End-to-end verification

- Turn logging and reviews: the live checks of the plan's "End-to-end verification" section.
- Agent context, on LAB-OMARCHY and PAYTON-LEGION2, each on a separate server instance started for the check with its own `--instance`, Mode `Pilot`, and explicit `ProfileSources` and `RuleSources`, never on a shared server (so no other live agent session is subject to prompt refusal, and the profile never resolves in a service account's home): a delivered request builds one package with the request's root ids (on claude-code and claude-cowork the prompt hook takes the bridge-minted `requestId` after applying pending transcript records); replay reports `identical: true`; an over-budget fixture prompt is refused with `agent_context_budget_exceeded` shown to the operator, no answer is produced, and the request's turn is closed `canceled` with that reason and leaves the Stop ledger once the server confirms the close, and a retried build with the same idempotency key returns the same 422 with the stored diagnostics; the model submits its response with the `agent_context_submit_response` tool before closing the turn, and the response passes the deterministic grade; an HV request covering its turn is created, the HV agent records a passing model grade with verified receipts, and the response becomes `Trusted`; a later package includes it as a prior response; committing a change to a file it depends on makes it `Stale` (an uncommitted edit does not, because staleness reads files at the workspace HEAD commit); with GraphRAG disabled retrieval reports `graphrag_disabled` and answers from the relational path; with GraphRAG enabled the trusted response is projected and a rejected one removed; the pilot report lists these requests by `invocationMode` (all `Stateful` under `Pilot`) and counts the `Overflow` package and the refused prompt; switching the instance to Mode `Off` while a host session runs makes its next build answer 409 `agent_context_disabled`, after which the host session continues statefully with no refusal and no `canceled` close, and the read-only queries, the pilot report among them, still answer.

## 9. Diagram index

Every diagram of this document, by section, named by its heading (or, where several diagrams share one heading, by the subject its lead-in names). A nested diagram is named in parentheses after the diagram that calls it, and its heading (`Nested activity: <Name>` or `Nested sequence: <Name>`) stands directly after that diagram (operator, 2026-10-12: "In multipage diagrams, collapse loops to nested diagrams" and "The nested diagrams must be generateded directly after their use."); a diagram that would span more than three letter pages at 8 to 12 point text is split that way. A shared component appears in the unit, policy, process, and sequence diagrams that use it as one call node, one message, or one participant, and the verb pipeline as a reference to process BindVerbToTurn or PersistTurnWriteWithReceipt. Views (PLAN-C4MODEL-001; operator, 2026-10-19: "Every diagram in every doc should come from the same C4 definition."): every diagram is a view of `docs/architecture/c4/workspace.dsl` (this document's elements in `model/str.dsl`, its views in `views/str.dsl`), shown as the image `c4/rendered/<view key>.svg` followed by the line `View: <view key>`, and rendered by `./build.ps1 ArchitectureDiagrams` on one US letter page at 8 to 16 point text (TR-MCP-NUKE-017 AC-5). A diagram named by its heading (a unit of work, policy, process, shared component, nested activity, or nested sequence) has its name as view key, and a nested sequence that shares its name with a nested activity adds `-Sequence`; a use case of the plan keeps its id (`UC-1` to `UC-9`); every other diagram has the key `STR-<section>-<NN>` named below. A diagram larger than one page continues in the views `<key>-part2`, `<key>-part3`, and so on; each branch of an `alt` block of a sequence is its own view `<key>-alt<k>-<i>`, an `opt` or `break` block is the view `<key>-opt<k>` or `<key>-break<k>`, and a loop body is the view `<key>-loop<k>`, each placed after the view that holds the block. The earlier Mermaid renders (operator, 2026-10-12: straight lines, 8 pt minimum and 12 pt maximum text on 8.5 x 11 in pages, "Use landscape for sequence diagrams.", and "Use ELK for Activity Diagrams and Use Cases, and dagre for Sequence.") remain the receipt `docs/receipts/diagrams/architecture-merged-r2-20261013T174051Z/`.

- Section 2: the system context (view STR-02-01).
- Section 3.2: the tool-audit hook of the TypeScript hosts (view STR-03-01).
- Section 3.5, the shared components, each drawn once: the request pipeline (WorkspaceResolutionBehavior and ModeGateBehavior, 3.5.1, view RequestPipeline); SerializableWrite, IdempotentCreate, and PagedQuery (3.5.2); TurnLocator and TurnWritePipeline (3.5.3); ResponseStatusTransition, IResponseStalenessRule, and RelationshipWriter (3.5.5); Resolve-MintOutcome, Invoke-SessionOpen, Set-SessionUnsupported, Update-TurnState, and Add-McpAlias (3.5.6).
- Section 4, Phase A:
  - state diagram (4.1): the request lifecycle (view STR-04-01);
  - units of work (4.2): MintSessionTurnCommand (nested activity InsertMintedTurnWithRetry), SubmitSessionLogCommand (nested activity EvaluateSubmittedTurn), UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand (nested activity PrepareReplacementTurn), CompleteSessionTurnCommand, FailSessionTurnCommand, OpenSessionLogCommand, AppendTurnDialogCommand, SetTurnTitleCommand, SetSessionTitleCommand;
  - policy (4.3): TerminalTurnOutcomeGuard;
  - processes (4.4): ApplyArrivalBridgeRecord (nested activities ApplyEnqueueRecord, ApplyDequeueRecord, ApplyRemoveRecord), OpenTurnFromPromptHook (OpenTurnUnderLock), BindVerbToTurn (ResolveVerbHostSession, ResolveVerbTarget), PersistTurnWriteWithReceipt (SendBoundWrite, and MapSubmitOutcome after SendBoundWrite), DrainFailsafe (DrainOneFailsafeRecord, and ReplayBoundRecord after it), EvaluateStopGate (EvaluateTurnAtStop, and EvaluateOpenTurnAtStop after it), ValidateHookRegistration, StartHostSession, EndHostSession, SyncAgentPluginsAtVersion (SyncOneAgentPlugin), RunArrivalBridge (TailTranscriptRecords);
  - sequences (4.5): MintSessionTurnCommand (view STR-04-02; nested sequences PrepareHookMint, ApplyMintOutcome); SubmitSessionLogCommand (STR-04-03); SubmitSessionLogCommand from a plugin verb (STR-04-04); UpsertSessionTurnCommand (STR-04-05); PatchSessionTurnCommand (STR-04-06); ReplaceSessionTurnCommand (STR-04-07); the close from a plugin verb, completeTurn or failTurn through SubmitSessionLogCommand (STR-04-08; ReceiveCloseAnswer); CompleteSessionTurnCommand (STR-04-09); FailSessionTurnCommand (STR-04-10); OpenSessionLogCommand (STR-04-11); AppendTurnDialogCommand from a plugin verb (STR-04-12); SetTurnTitleCommand from a plugin verb (STR-04-13); SetSessionTitleCommand (STR-04-14); the failsafe replay of process DrainFailsafe (STR-04-15; ReplayOneFailsafeRecord, and SendDrainedRecord after it).
- Section 5, Phase B:
  - state diagram (5.1): the review status (view STR-05-01);
  - units of work (5.2): RecordSessionTurnReviewCommand, ResetSessionTurnReviewCommand, GetSessionLogQuery, QuerySessionLogsQuery;
  - policies (5.3): RequestClassification, ReviewValidation, HvVerdictMapping, CompletionPolicy, CompletionGate;
  - processes (5.4): ConvertHvReceiptToReview, ClearSession (nested activities ClearOneOpenTurn, and ReportCloseReceipt after it), AdvisoryCompletionPrecheck, PreserveReviewScoreLexemes;
  - sequences (5.5): RecordSessionTurnReviewCommand (view STR-05-02; nested sequence ServeReviewWrite); ResetSessionTurnReviewCommand (STR-05-03); a classification through SubmitSessionLogCommand from the plugin updateTurn verb (STR-05-04); GetSessionLogQuery (STR-05-05); QuerySessionLogsQuery (STR-05-06).
- Section 6, Phase C:
  - entity relationship diagrams (6.3), one per group of Addendum A: packages and their build inputs (view STR-06-01); package omissions and findings (STR-06-02); responses and evidence (STR-06-03); grades (STR-06-04); HV requests (STR-06-05); relationships and the GraphRAG projection (STR-06-06);
  - state diagrams (6.3): the response lifecycle (STR-06-07); the projection row states (STR-06-08);
  - units of work (6.5): GetAgentContextStatusQuery; BuildAgentContextPackageCommand (nested activities ReadBuildInputs, SavePackage, FillBudgetWithCandidates, the latter also used by ReplayAgentContextPackageQuery); GetAgentContextPackageQuery; QueryAgentContextPackagesQuery; ReplayAgentContextPackageQuery; SubmitAgentResponseCommand (ReadSubmissionInputs); GetAgentResponseQuery; QueryAgentResponsesQuery; RunDeterministicGradeCommand (CheckOneEvidenceItem); RecordModelGradeCommand (ReadModelGradeInputs with its nested activity CheckGradeTarget, then SaveModelGrade); DecideAgentResponseTrustCommand; RejectAgentResponseCommand; SupersedeAgentResponseCommand; EvaluateStalenessCommand (EvaluateOneResponseStaleness); CreateAgentContextRelationshipCommand; DeleteAgentContextRelationshipCommand; QueryAgentContextRelationshipsQuery; RetrieveAgentContextCandidatesQuery; ProjectAgentResponsesCommand (ProjectOneResponse); GetAgentContextPilotReportQuery; CreateAgentContextHvRequestCommand (SelectHvRequestItems); GetAgentContextHvRequestQuery; QueryAgentContextHvRequestsQuery;
  - process (6.6): RunStatelessInvocation (nested activities ResolveInvocationTurn, FailInvocation, AnswerAndSubmit);
  - sequences (6.7): RequestPipeline (every Phase C dispatch, view RequestPipeline-Sequence); GetAgentContextStatusQuery (STR-06-09); BuildAgentContextPackageCommand (STR-06-10; nested sequences ReadBuildInputs, view ReadBuildInputs-Sequence, and SaveBuiltPackage); GetAgentContextPackageQuery (STR-06-11); QueryAgentContextPackagesQuery (STR-06-12); ReplayAgentContextPackageQuery (STR-06-13); SubmitAgentResponseCommand (STR-06-14; ReadSubmissionInputs, view ReadSubmissionInputs-Sequence); GetAgentResponseQuery (STR-06-15); QueryAgentResponsesQuery (STR-06-16); RunDeterministicGradeCommand (STR-06-17); RecordModelGradeCommand from an HV review (STR-06-18; SubmitOneModelGrade, and ReadModelGradeInputs after it, view ReadModelGradeInputs-Sequence); RejectAgentResponseCommand (STR-06-19); SupersedeAgentResponseCommand (STR-06-20); EvaluateStalenessCommand (STR-06-21); CreateAgentContextRelationshipCommand (STR-06-22); DeleteAgentContextRelationshipCommand (STR-06-23); QueryAgentContextRelationshipsQuery (STR-06-24); RetrieveAgentContextCandidatesQuery (STR-06-25; ChooseRetrievalPath); ProjectAgentResponsesCommand (STR-06-26; ProjectOneResponse, the sequence of the nested activity of the same name, view ProjectOneResponse-Sequence); the stateless invocation end to end, process RunStatelessInvocation (STR-06-27; BuildAndDeliverPackage, SubmitAndGradeResponse); GetAgentContextPilotReportQuery (STR-06-28); CreateAgentContextHvRequestCommand (STR-06-29); GetAgentContextHvRequestQuery (STR-06-30); QueryAgentContextHvRequestsQuery (STR-06-31).
- Section 7, use cases: the overview (7.1, view STR-07-01); UC-1 to UC-9 (7.2 to 7.10, views UC-1 to UC-9); the agent-context use cases (7.11 to 7.16, views STR-07-02 to STR-07-07).
- Section 8, delivery: the phase order (8.1, view STR-08-01); the bounded contexts in merge order (8.3, view STR-08-02); process RunPlanPhases (nested activities RunOnePlanPhase, ValidateAndApprove, RunPhase4WorkPackages); process RunDomainCodingAgent (nested activities ImplementBoundedContext, and ImplementationPass after it).
- Addendum A, entity relationship diagrams by functionality: A.1 references between the groups (view STR-A-01), then one diagram per group: A.2 sessions and turns (Phases A and B, STR-A-02), A.3 turn records (STR-A-03), A.4 requirements, TODOs, and use cases (existing entities the plan references, STR-A-04), A.5 packages and their build inputs (STR-A-05), A.6 package omissions and findings (STR-A-06), A.7 responses and evidence (STR-A-07), A.8 grades (STR-A-08), A.9 HV requests (STR-A-09), and A.10 relationships, the GraphRAG projection, and retrieval stores (STR-A-10) (A.5 to A.10 are Phase C).

## Addendum A. Entity diagrams with relationships

Operator directive (2026-10-11, verbatim): "I want an entity diagram with relationships as an addendum".

These diagrams show every entity the merged plan adds or changes, and every existing entity those reference, with keys and relationships. Existing entities are read from `src/QBrainAi.Storage/Entities` and `src/QBrainAi.Storage/McpDbContext.cs` at `a8d3934b` and are drawn with their keys; Phase A and Phase B add columns to the turn and one child table (sections 4.1 and 5.1); Phase C entities are those of section 6.3, which gives each column's length and meaning. Entity names drop the `Entity` suffix (section 6.3's own diagrams name the tables). Every entity carries `WorkspaceId` (required, the canonical workspace path, with the named `Workspace` query filter); a diagram draws it where it is part of a key, and no relationship crosses workspaces.

Columns: a diagram draws each entity's key columns (its primary key, its foreign keys, the columns of its unique indexes, and the column each dotted reference uses) and, where the page allows, the columns the plan adds. The list above each diagram names every Phase A, B, and C column the diagram does not draw and the entity's unique and other indexes (it names a drawn column only to say what it holds), so every column of sections 4.1, 5.1, and 6.3 is in this addendum.

Root ids (section 3.1, "Root ids"): every new child row carries `WorkspaceId` and the root ids of every parent it hangs from, copied from its parent in the same transaction and never changed, and the root-id columns join no key or index beyond those listed. The turn key is `Agent`, `SessionId`, and `RequestId` together, because a request id is unique only within its session (`McpDbContext.cs:391,404`; `RootIdsEveryAgentContextChildRowEqualsItsParents`, `ReviewReasonRootIdsEqualItsTurnKey`).

Notation:

- `||--o{` and similar solid lines are database foreign keys: every Phase C foreign key is the composite (`WorkspaceId`, parent key); existing ones cascade or restrict as configured in `McpDbContext`.
- `..` dotted lines (`||..o{`, `}o..o{`) are logical references by root ids (the turn key) or by polymorphic references (a `Kind` with a `SourceRef`, or a relationship's `FromKind` and `FromRef`); the units enforce them, not a foreign key.
- Attribute keys: `PK` primary key, `FK` foreign key, `UK` part of a unique index; a column can carry several (for example `PK, FK, UK`). Which columns form each unique index is stated in the list above the diagram.

Grouping (operator, 2026-10-12, verbatim: "I would be ok with grouping by functionality"): A.2 to A.10 group the entities by functionality, and each entity's columns appear in exactly one group; an entity of another group drawn without columns in a group diagram is a relationship target defined in its own group. Each relationship is drawn exactly once: a foreign key in its child's group, a logical reference inside one group in that group, and a logical reference between groups in A.1. A.5 to A.10 are the six Phase C groups of section 6.3.

### A.1 References between the groups

Columns the dotted references use:

- A package and a response name their turn by the turn key. A model grade names its reviewer's own turn by `ReviewerAgent`, `ReviewerSessionId`, and `ReviewerRequestId`. An HV request names the reviewed MCP session by `Agent` and `SessionId` (the session's `SourceType` and `SessionId`) and its range of turns by `FromRequestId` and `ToRequestId`. A turn names its TODO by `TodoId`.
- A package item names its source by `Kind` and `SourceRef` (a turn as `<agent>/<sessionId>/<requestId>`; a prior response is included only while `Trusted`). A finding and an input link name the relationship they read by `RelationshipId`. A relationship names each end by `FromKind` and `FromRef`, or `ToKind` and `ToRef`.
- Inputs, omissions, and findings name their sources with the same `SourceRef` kinds as the items, and evidence names a requirement or a turn by `Ref`; those references resolve the same way and are not drawn again. Kinds without an entity (code files, receipts, profiles, rules, commits) have no box.

![A.1 References between the groups (entities), part 1 of 9](c4/rendered/STR-A-01.svg)

View: STR-A-01

![A.1 References between the groups (entities), part 2 of 9](c4/rendered/STR-A-01-part2.svg)

View: STR-A-01-part2

![A.1 References between the groups (entities), part 3 of 9](c4/rendered/STR-A-01-part3.svg)

View: STR-A-01-part3

![A.1 References between the groups (entities), part 4 of 9](c4/rendered/STR-A-01-part4.svg)

View: STR-A-01-part4

![A.1 References between the groups (entities), part 5 of 9](c4/rendered/STR-A-01-part5.svg)

View: STR-A-01-part5

![A.1 References between the groups (entities), part 6 of 9](c4/rendered/STR-A-01-part6.svg)

View: STR-A-01-part6

![A.1 References between the groups (entities), part 7 of 9](c4/rendered/STR-A-01-part7.svg)

View: STR-A-01-part7

![A.1 References between the groups (entities), part 8 of 9](c4/rendered/STR-A-01-part8.svg)

View: STR-A-01-part8

![A.1 References between the groups (entities), part 9 of 9](c4/rendered/STR-A-01-part9.svg)

View: STR-A-01-part9

### A.2 Sessions and turns (Phases A and B)

Columns and indexes not drawn:

- `SessionLog`: unique (`WorkspaceId`, `SourceType`, `SessionId`) (`McpDbContext.cs:391`); `SourceType` holds the agent name, so the turn key's `Agent` and `SessionId` locate a session (TurnLocator, section 3.5.3).
- `SessionLogTurn`, existing: unique (`SessionLogId`, `RequestId`) (`McpDbContext.cs:404`); the plan reads `Status` (`in_progress`, or a terminal status of `TurnStatus`, section 3.5.3), `QueryTitle`, `QueryText`, and `PlanFile`.
- `SessionLogTurn`, Phase A (section 4.1): `IngressNonce`, null on legacy rows, with a unique filtered index on (`WorkspaceId`, `SessionLogId`, `IngressNonce`) where `IngressNonce` is not null, and a second unique filtered index on (`WorkspaceId`, `RequestId`) where `IngressNonce` is not null, so a server-minted `RequestId` is unique across the workspace's sessions while legacy rows keep their per-session uniqueness (TR-MCP-SESSIONTURN-004 AC-006).
- `SessionLogTurn`, Phase B (section 5.1): `RequestClass` is indexed and null until the turn is classified; ten columns per review kind, named with the kind prefix as the plan names the two status columns (`AgentReviewStatus`, `HostileReviewStatus`; the other eighteen names follow that prefix, inference). Drawn: `RequestClass` and the two `Status` columns. Not drawn, for each prefix `AgentReview` and `HostileReview`: `OtherNotes`, `Correctness`, `Completeness`, `CorrectnessThreshold`, `CompletenessThreshold` (decimal, precision 29 scale 28), `Reviewer`, `ReviewedAt` (UTC), `RequestJsonlPath`, `ResponseJsonlPath`.
- Defaults (section 5.1): the backfill sets `RequestClass` to `Unclassified` and both review statuses to `Unstarted` on every existing row, soft-deleted rows included; the migration gives each review `Status` the database default `Unstarted` and `RequestClass` the database default `Unclassified`, so a turn the prior binary inserts during a binary-only rollback reads as its `ReviewDefaults` after the roll-forward (`RollbackPriorBinaryInsertsTurnRollForwardReadsDefaults`, section 8.5). The new binary writes these columns on every insert, so a new turn's `RequestClass` stays null until it is classified.
- `SessionLogTurnReviewReason` (Phase B, table `SessionLogTurnReviewReasons`): `WorkspaceId`; one row per reason, ordered by `Ordinal` within its `Kind` (`Agent` or `Hostile`); its root ids are its turn's key, in no key or index.

![A.2 Sessions and turns (Phases A and B) (entities)](c4/rendered/STR-A-02.svg)

View: STR-A-02

### A.3 Turn records

The existing children of the turn, unchanged by the plan; each references its turn by `SessionLogTurnId` with `DeleteBehavior.Restrict` (`McpDbContext.cs:414-462`). The plan writes each decision as the workspace triple (section 3.3, C7) through three of them: a `SessionLogTurnStringList` row with `ListType` `designDecisions`, a `SessionLogProcessingDialog` item with `Category` `decision`, and a `SessionLogAction` row with `Type` `design_decision`; rows are ordered by `Ordinal` (`Order` for actions).

![A.3 Turn records (entities), part 1 of 3](c4/rendered/STR-A-03.svg)

View: STR-A-03

![A.3 Turn records (entities), part 2 of 3](c4/rendered/STR-A-03-part2.svg)

View: STR-A-03-part2

![A.3 Turn records (entities), part 3 of 3](c4/rendered/STR-A-03-part3.svg)

View: STR-A-03-part3

### A.4 Requirements, TODOs, and use cases (existing entities the plan references)

Keys at `a8d3934b`: `TodoItem` (`McpDbContext.cs:581-591`), `TodoRequirementLink` (`:611-623`), `Requirement` (`:677-687`), `RequirementAcceptanceCriterion` (`:695-702`), `RequirementTraceabilityLink` (`:711-724`: key (`WorkspaceId`, `FrId`, `TargetKind`, `TargetId`), source foreign key (`WorkspaceId`, `SourceKind`, `FrId`)), `UseCase` (`:780-792`), `UseCaseFrLink` (`:883-902`: unique (`WorkspaceId`, `UseCaseId`, `FrId`), requirement foreign key (`WorkspaceId`, `FrKind`, `FrId`)); `UseCase.WorkspaceId` and `UseCaseFrLink.WorkspaceId` are also foreign keys to the workspace row, not drawn. The plan reads requirements and TODOs as sources, through the fields section 6.3 hashes for each kind.

![A.4 Requirements, TODOs, and use cases (existing entities the plan references) (entities), part 1 of 3](c4/rendered/STR-A-04.svg)

View: STR-A-04

![A.4 Requirements, TODOs, and use cases (existing entities the plan references) (entities), part 2 of 3](c4/rendered/STR-A-04-part2.svg)

View: STR-A-04-part2

![A.4 Requirements, TODOs, and use cases (existing entities the plan references) (entities), part 3 of 3](c4/rendered/STR-A-04-part3.svg)

View: STR-A-04-part3

### A.5 Packages and their build inputs (Phase C)

Columns and indexes not drawn (string unless a type is given):

- `AgentContextPackage` (table `AgentContextPackages`, key (`WorkspaceId`, `PackageId`), `PackageId` = `pkg-<requestId>-<nn>`): drawn are its turn key and unique (`WorkspaceId`, `IdempotencyKey`); indexes (`WorkspaceId`, `RequestId`) and (`WorkspaceId`, `SessionId`, `CreatedUtc`); `PayloadSha256`, `Status` (`Built` or `Overflow`), `CommitSha`, `WorkingTreeDirty` (bool), `BuilderVersion`, `TokenBudget`, `BytesPerToken`, `MandatoryTokenCount`, `TotalTokenCount` (int), `InputsSha256`, `OptionsSnapshotJson`, `ContentSha256`, `QueryText`, `RenderedContent`, `CreatedUtc` (datetimeoffset). Its six child tables reference it through (`WorkspaceId`, `PackageId`) with `DeleteBehavior.Restrict`.
- Each package child below has a `long Id` key, the foreign key (`WorkspaceId`, `PackageId`), unique (`WorkspaceId`, `PackageId`, `Ordinal`), and the root ids `Agent`, `SessionId`, and `RequestId`, its package's turn key.
- `AgentContextPackageItem` (table `AgentContextPackageItems`): `Kind`, `SourceSha256`, `SourceCommitSha`, `Mandatory` (bool), `Score` (decimal), `Rationale`, `TokenCount` (int), `RetrievalPath` (`Relational` or `GraphRag`), `Content`.
- `AgentContextPackageFocusRef` (table `AgentContextPackageFocusRefs`): `Kind`, `Ref`.
- `AgentContextPackageInput` (table `AgentContextPackageInputs`, the replay snapshot): `Kind`, `SourceRef`, `SourceSha256`, `SourceCommitSha`, `SourceVersion`, `Content`, `ContainsSecret` (bool), `ResponseStatus` (`PriorResponse` candidates only), `RankingFeaturesJson`.
- `AgentContextPackageInputLink` (table `AgentContextPackageInputLinks`, the `contradicts` and `blocks` links read at build): `RelationshipId` is drawn, kept as read; `Type`, `FromKind`, `FromRef`, `ToKind`, `ToRef`.

![A.5 Packages and their build inputs (Phase C) (entities), part 1 of 2](c4/rendered/STR-A-05.svg)

View: STR-A-05

![A.5 Packages and their build inputs (Phase C) (entities), part 2 of 2](c4/rendered/STR-A-05-part2.svg)

View: STR-A-05-part2

### A.6 Package omissions and findings (Phase C)

Columns and indexes not drawn (string unless a type is given); both are package children, with the `long Id` key, the foreign key, and the unique index of the package children in A.5:

- `AgentContextOmission` (table `AgentContextOmissions`): `Agent`, `SessionId`, `RequestId` (root ids); `Kind`, `SourceRef`, `SourceSha256`, `Score` (decimal), `Reason` (`budget`, `untrusted`, `stale`, `rejected`, `superseded`, `secret`, `duplicate`, `below_candidate_limit`), `Detail`.
- `AgentContextFinding` (table `AgentContextFindings`): `Agent`, `SessionId`, `RequestId` (root ids); `RelationshipId` is drawn; `Kind` (`Contradiction` or `Blocker`), `SourceRef`, `OtherSourceRef` (contradictions only), `Summary`.

![A.6 Package omissions and findings (Phase C) (entities)](c4/rendered/STR-A-06.svg)

View: STR-A-06

### A.7 Responses and evidence (Phase C)

Columns and indexes not drawn (string unless a type is given):

- `AgentResponse` (table `AgentResponses`, key (`WorkspaceId`, `ResponseId`), `ResponseId` = `rsp-<requestId>-<nn>`): drawn are the required foreign key (`WorkspaceId`, `PackageId`), the root ids `PackageId`, `Agent`, `SessionId`, and `RequestId` copied from the package, unique (`WorkspaceId`, `IdempotencyKey`), and `SupersededByResponseId`; indexes (`WorkspaceId`, `Status`), (`WorkspaceId`, `RequestId`), (`WorkspaceId`, `PackageId`); `Model`, `Effort`, `InvocationMode` (`Stateful` or `Stateless`), `SubmissionSha256`, `StatusVersion` (long, default 0, the lock counter), `Status` (`Submitted`, `Grading`, `Trusted`, `Rejected`, `Stale`, `Superseded`), `StatusReason`, `ContentFormat`, `Content`, `ContentSha256`, `CommitSha`, `SubmittedUtc`, `StatusChangedUtc`, `TrustedUtc` (datetimeoffset). Its claims and evidence reference it through (`WorkspaceId`, `ResponseId`).
- `AgentResponseClaim` (table `AgentResponseClaims`, `long Id`): unique (`WorkspaceId`, `ResponseId`, `ClaimId`) and unique (`WorkspaceId`, `ResponseId`, `Ordinal`); `PackageId`, `Agent`, `SessionId`, `RequestId` (root ids); `Text`.
- `AgentResponseEvidence` (table `AgentResponseEvidence`, `long Id`): unique (`WorkspaceId`, `ResponseId`, `Ordinal`); `PackageId`, `Agent`, `SessionId`, `RequestId` (root ids); `ClaimId`, `Kind` (`File`, `Requirement`, `Receipt`, `Commit`, `SessionLogTurn`), `Ref`, `CitedSha256` (as submitted), `Sha256` (computed by the server at submission), `CommitSha` (`File` only: the commit the cited content is read at).

![A.7 Responses and evidence (Phase C) (entities), part 1 of 2](c4/rendered/STR-A-07.svg)

View: STR-A-07

![A.7 Responses and evidence (Phase C) (entities), part 2 of 2](c4/rendered/STR-A-07-part2.svg)

View: STR-A-07-part2

### A.8 Grades (Phase C)

Columns and indexes not drawn (string unless a type is given):

- `AgentResponseGrade` (table `AgentResponseGrades`, key (`WorkspaceId`, `GradeId`), `GradeId` = `grd-<responseId>-<deterministic|model>-<nn>`): drawn are the foreign key (`WorkspaceId`, `ResponseId`), the nullable foreign key (`WorkspaceId`, `HvRequestId`) to the HV request (required for `Model`, null for `Deterministic`), the unique filtered index (`WorkspaceId`, `ResponseId`, `HvRequestId`) where `Kind` is `Model` (the model grade's natural key), and the reviewer's turn key `ReviewerAgent`, `ReviewerSessionId`, `ReviewerRequestId` (required for `Model`); `PackageId`, `Agent`, `SessionId`, `RequestId` (root ids, with `ResponseId` and `HvRequestId`); `Kind` (`Deterministic` or `Model`), `Grader`, `Correctness`, `Completeness`, `CorrectnessThreshold`, `CompletenessThreshold` (decimal), `PassCount`, `FailCount`, `UnknownCount` (int), `Verdict` (`Pass` or `Fail`), `PayloadSha256` (required for `Model`), `VerdictJsonSha256`, `RequestJsonlPath`, `ResponseJsonlPath`, `RequestJsonlSha256`, `ResponseJsonlSha256`, `RolloutJsonlPath`, `RolloutJsonlSha256`, `ReviewerThreadId` (required for `Model`), `GradedUtc` (datetimeoffset, provenance only), `RecordedUtc` (datetimeoffset, required, stamped by the server; it alone orders grades). Its findings reference it through (`WorkspaceId`, `GradeId`).
- `AgentResponseGradeFinding` (table `AgentResponseGradeFindings`, `long Id`): unique (`WorkspaceId`, `GradeId`, `Ordinal`); `ResponseId`, `PackageId`, `Agent`, `SessionId`, `RequestId`, `HvRequestId` (null for a deterministic grade) (root ids, the grade's); `ClaimId`, `Check`, `Outcome` (`PASS`, `FAIL`, `UNKNOWN`), `Evidence`.

![A.8 Grades (Phase C) (entities), part 1 of 2](c4/rendered/STR-A-08.svg)

View: STR-A-08

![A.8 Grades (Phase C) (entities), part 2 of 2](c4/rendered/STR-A-08-part2.svg)

View: STR-A-08-part2

### A.9 HV requests (Phase C)

Columns and indexes not drawn (string unless a type is given):

- `AgentContextHvRequest` (table `AgentContextHvRequests`, key (`WorkspaceId`, `HvRequestId`), `HvRequestId` = `hvr-<sessionId>-<yyyyMMddTHHmmssZ>`, with `-<nn>` appended only when two are created in the same second): drawn are `Agent` and `SessionId` (the reviewed MCP session) and unique (`WorkspaceId`, `IdempotencyKey`); `FromRequestId`, `ToRequestId` (the range, inclusive, in the session's turn order), `RequestedBy`, `Reason`, `PayloadSha256`, `Status` (`Open` or `Completed`), `CreatedUtc`, `CompletedUtc` (datetimeoffset). Its items reference it through (`WorkspaceId`, `HvRequestId`).
- `AgentContextHvRequestItem` (table `AgentContextHvRequestItems`, `long Id`): foreign keys (`WorkspaceId`, `HvRequestId`) and (`WorkspaceId`, `ResponseId`), whose index is the lookup ResponseStatusTransition uses to close a response's `Pending` items; unique (`WorkspaceId`, `HvRequestId`, `ResponseId`) and unique (`WorkspaceId`, `HvRequestId`, `Ordinal`); `PackageId`, `Agent`, `SessionId`, `RequestId` (root ids, the response's, with `HvRequestId` and `ResponseId`); `Disposition` (`Pending`, `Graded`, `Closed`), `ClosedReason` (`rejected` or `superseded`).

![A.9 HV requests (Phase C) (entities)](c4/rendered/STR-A-09.svg)

View: STR-A-09

### A.10 Relationships, the GraphRAG projection, and retrieval stores (Phase C)

Columns and indexes not drawn (string unless a type is given):

- `AgentContextRelationship` (table `AgentContextRelationships`, key (`WorkspaceId`, `RelationshipId`), `RelationshipId` = `rel-<type>-<yyyyMMddTHHmmssZ>-<nn>`): a root record whose ends are references, so it carries no parent id; drawn is the six-field unique (`WorkspaceId`, `FromKind`, `FromRef`, `Type`, `ToKind`, `ToRef`), RelationshipWriter's IdempotentCreate key with policy `ReturnStored`; indexes (`WorkspaceId`, `FromKind`, `FromRef`) and (`WorkspaceId`, `ToKind`, `ToRef`); `ToSha256` (read by the unit before SerializableWrite and handed to RelationshipWriter), `EvidenceRef`, `CreatedUtc` (datetimeoffset), `CreatedBy`.
- `AgentContextProjection` (table `AgentContextProjections`, key (`WorkspaceId`, `ResponseId`), one row per response through that composite foreign key): `PackageId`, `Agent`, `SessionId`, `RequestId` (root ids, the response's); `GraphRagDocumentId` is drawn; `State` (`Pending`, `Projected`, `Removed`, `Skipped`, `Failed`), `ProjectedContentSha256`, `LastError`, `UpdatedUtc` (datetimeoffset).
- Existing retrieval stores, unchanged: `ContextDocument` (`Id` `adhoc-<Guid>` for a GraphRAG ingest) with its `ContextChunk` rows (foreign key `DocumentId`, `McpDbContext.cs:379-387`), and the knowledge graph's `GraphEntity` and `GraphRelationship` (foreign keys `SourceEntityId` and `TargetEntityId`, `McpDbContext.cs:565-579`), which a projection never points at.

![A.10 Relationships, the GraphRAG projection, and retrieval stores (Phase C) (entities), part 1 of 3](c4/rendered/STR-A-10.svg)

View: STR-A-10

![A.10 Relationships, the GraphRAG projection, and retrieval stores (Phase C) (entities), part 2 of 3](c4/rendered/STR-A-10-part2.svg)

View: STR-A-10-part2

![A.10 Relationships, the GraphRAG projection, and retrieval stores (Phase C) (entities), part 3 of 3](c4/rendered/STR-A-10-part3.svg)

View: STR-A-10-part3

Relationship notes:

- `SessionLog` to `SessionLogTurn` and every existing turn child are existing foreign keys; Phase B adds `SessionLogTurnReviewReason` as a new child of the turn (one row per reason, discriminated by `Kind`, ordered by `Ordinal`, carrying its turn's key), and the review fields as columns on the turn row (one record per kind per turn, so no separate review table).
- Phase A adds `SessionLogTurn.IngressNonce` with a unique filtered index per MCP session (`WorkspaceId`, `SessionLogId`, `IngressNonce`), and a second unique filtered index on (`WorkspaceId`, `RequestId`) where `IngressNonce` is set, so a server-minted `RequestId` is unique across the workspace's sessions; legacy rows keep their per-session uniqueness (TR-MCP-SESSIONTURN-004 AC-006).
- `AgentContextPackage` and `AgentResponse` refer to their turn by the turn key (`Agent`, `SessionId`, `RequestId`), not by the turn's numeric `Id`, so packages and responses survive turn soft-delete and federation snapshots that renumber turn rows; the service validates that the turn exists when a package is built (BuildAgentContextPackageCommand, whose delegate re-checks the turn).
- `AgentContextPackageItem.SourceRef`, `AgentContextRelationship.FromRef` and `ToRef`, and `RelationshipId` on findings and input links are polymorphic or soft references resolved by kind in the service layer; staleness (IResponseStalenessRule, persisted only by EvaluateStalenessCommand) compares each dependency's current hash with the stored `ToSha256` of a `depends_on`, `derived_from`, or `supports` link, or with the evidence `Sha256`, and checks the effective status of every dependency that names a response (section 3.5.5).
- `AgentResponse.SupersededByResponseId` is a self reference within one turn (the same turn key `Agent`, `SessionId`, `RequestId`); SupersedeAgentResponseCommand prevents cycles, and a `supersedes` row in `AgentContextRelationship` records the same link with its evidence.
- `AgentContextProjection` has at most one row per response (key (`WorkspaceId`, `ResponseId`)); its `GraphRagDocumentId`, while a document exists, is the id of the `ContextDocumentEntity` row (the `Documents` set, id `adhoc-<Guid>`) that `IGraphRagService.IngestTextAsync` creates with its `ContextChunkEntity` children (`src/QBrainAi.GraphRag/Services/GraphRagService.cs:348-372`), always in the workspace scope; removal deletes that document and its chunks (`IGraphRagService.DeleteDocumentAsync`). It never points at a knowledge-graph entity (`GraphEntity`, which stays a separate graph node) or at the global scope.
- Keys: requirement-family keys are composite (`WorkspaceId`, `Kind`, `Id`) and TODO keys (`WorkspaceId`, `Id`); every agent-context readable key is per workspace, the composite primary key (`WorkspaceId`, key) over a human-readable string derived from the records it belongs to (`pkg-`, `rsp-`, `grd-`, `rel-`, `hvr-`, section 6.3), and every reference to it is the composite foreign key; every child row of the new tables uses a `long` identity key. Soft delete (TR-MCP-DB-003) hides deleted rows, and the keys and unique indexes cover them (section 6.3).
- `AgentContextHvRequest` names a range of turns of one MCP session (logical reference by `Agent` and `SessionId`); its items list the responses in that range when it was created; each model grade names the HV request that produced it (foreign key), and the request completes when no item stays `Pending` (each item `Graded`, or `Closed` because its response was rejected or superseded, section 6.3).
