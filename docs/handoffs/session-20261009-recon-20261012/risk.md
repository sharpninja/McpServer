<!-- SPDX-License-Identifier: Apache-2.0 -->
# Risk projection of the round-5 design changes (lane: risk)

Read-only review. Abbreviations used in citations:

- `arch:N` is line N of the working copy of `docs/architecture/SessionTurn-Review-AgentContext-Architecture.md` (629,718 bytes, 5,353 lines).
- `plan:N` is line N of the working copy of `docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md` (405,670 bytes, 712 lines).
- `path:line@a8d3934b` is develop code at the baseline.
- `N/` is the analyzer worktree `.claude/worktrees/netanalyzers-all`, branch `chore/netanalyzers-all`.
- `store` is the requirement export under `scratchpad/recon/store/`.

Every risk below gives:

- a severity (blocker, high, medium, or low) and a likelihood;
- the evidence;
- the phase or gate where it would surface;
- the smallest mitigation stated as a requirement or AC change.

Where no AC change can fix a risk, the mitigation is labeled "Operator question".

## Breaking changes

### R-B1. Server contract types become public API of the published `QBrainAI.Client` package

Severity medium. Likelihood high.

Evidence:

- `src/QBrainAi.Client/QBrainAi.Client.csproj@a8d3934b` packs `PackageId` `QBrainAI.Client` (net10.0). Its version comes from GitVersion (`build/Build.Compile.cs:12@a8d3934b`, `build/Build.BumpVersion.cs:7-18@a8d3934b`).
- Round 5 puts these server-side types into that package:
  - every new DTO (arch:243);
  - `ClassifiedErrorException` and the `ErrorCodes` table with HTTP statuses (arch:314);
  - `PageResult<TItem>` (arch:301);
  - `ScoreLexeme` and `ScoreLexemeJsonConverter` (arch:367).
- The client already has its own typed error family carrying `Code` and `Retryable` (`src/QBrainAi.Client/McpClientExceptions.cs:15-27@a8d3934b`). It maps 400, 401, 404, and 409 and sends everything else, 422 included, to a generic `QBrainAiException` (`src/QBrainAi.Client/McpClientBase.cs:613-630@a8d3934b`).
- Inference: the package now carries two error vocabularies.
- Inference: once a server-side type ships, renaming it (round 5 renamed six types; IDE0130 moves namespaces) is a breaking change for package consumers.

Surfaces: Phase 2 (the DTOs compile into the client) and the first package publish after Phase 4.

Mitigation:

- Add to the Phase 2 contract TR: "Changes to `QBrainAi.Client` public API are additive. At the Phase 2 exit, a public-API diff against `a8d3934b` lists only additions."
- Add: "The typed client raises `ClassifiedError` bodies as the existing `McpClientException` subclasses with `Code` and `Retryable`. `ClassifiedErrorException` is never thrown to a client caller. A 422 maps to a typed exception that carries the code."
- Each sentence gets a named case.

### R-B2. Error bodies and statuses on existing routes this plan changes

Severity medium. Likelihood medium.

Evidence:

- The architecture contradicts itself:
  - arch:83 says every REST error of a unit the plan "adds or changes" is the `ClassifiedError` problem+json body.
  - arch:598 says the five single-turn routes "keep today's request bodies, response bodies, and errors".
- Those same routes gain new refusals:
  - 400 `validation_error` for a `closed` status (arch:600);
  - the `review_*` codes and 422 `review_status_conflict` (arch:563);
  - 409 `conflict` with `retryable: true` (arch:523-524, arch:563).
- Today's Upsert errors are ProblemDetails with no `code` ("Invalid turn payload.", `src/QBrainAi.Support.Mcp/Controllers/SessionLogController.cs:202,213@a8d3934b`).
- Older plugins classify failures by regex over output text (`plugins/core/lib-ps/repl-invoke.ps1:1736-1745@a8d3934b`):
  - `timeout|backend_unavailable|HTTP 503` means queued;
  - everything else is `rejected` and throws.
- Inference: a pre-1.119.0 plugin reports a retryable 409 as a hard rejection.
- Inference: an older typed client reports a 422 as a generic exception.

Surfaces: Phase 4 SessionTurnLogging and TurnReviews G3 suites, and any host still on an old plugin cache before the one sync.

Mitigation:

- Fix arch:83 against arch:598.
- Add to TR-MCP-SESSIONTURN-004: for every changed existing route, each error in the unit's list is either today's body or a `ClassifiedError` that keeps `type`, `title`, `status`, and `detail`.
- Add one golden case: `ExistingTurnRoutes_ErrorBodies_TodayFieldsPreserved_CodeAdded`.

### R-B3. The `closed` refusal and the `cancelled` spelling

Severity medium (an HV FAIL is certain; a runtime break is limited to external writers). Likelihood high for HV, low at runtime.

Evidence:

- Two statements conflict:
  - arch:320 accepts `in_progress`, `completed`, `failed`, `canceled`, and the legacy `cancelled`;
  - arch:560 and arch:600 refuse every status other than the four, so `cancelled` is refused.
- The ed-a open question is still unanswered (`review-arch-r5/edit-ed-a.md:77`).
- Existing requirement text depends on `cancelled`. FR-MCP-SESSIONLIFE-002 names "canceled and cancelled supersede first persists" (`store/listFr.yaml:4573` onward).
- Develop's PowerShell core already rewrites `cancelled` to `canceled` (`plugins/core/lib-ps/repl-invoke.ps1:1278-1279@a8d3934b`).
- No agent-surface writer of `closed` exists at the baseline. `git grep` finds `closed` only in the terminal predicate (`src/QBrainAi.Services/Services/SessionLogService.cs:1359@a8d3934b`) and in TODO and GitHub code.
- Inference: the only runtime break is for external MCP or REST callers that write `cancelled` or `closed`.

Surfaces: HV of the architecture, then Phase 3 (TurnStatus cases).

Mitigation: one AC on the TurnStatus TR naming the exact accepted write set, including whether `cancelled` is accepted and how it is stored. Fix arch:560 and arch:600 to match. Case: `TurnStatus_AgentSurfaceWrite_CancelledAcceptedClosedRefused_AllSurfaces`.

### R-B4. Paging policy differs between old and new routes

Severity low. Likelihood medium.

Evidence:

- Old session-log queries clamp; new agent-context queries and Retrieve's `limit` refuse values outside 1..1000 with 400 (arch:301).
- Today's clamp: `src/QBrainAi.Services/Services/SessionLogService.cs:476-477@a8d3934b`, client default 100 (`src/QBrainAi.Client/Models/SessionLogModels.cs:340@a8d3934b`).
- Build dispatches Retrieve internally (arch:2684). `MaxCandidatesPerKind` allows up to 1000 per kind (arch:2348).
- Inference: if Build derives a Retrieve limit above 1000, the internal call gets 400. In `Pilot` or `Stateless` that fails the invocation and refuses the prompt.

Surfaces: Phase 4 ContextPackages, and the 8.6 live check with a non-default configuration.

Mitigation:

- Build's internal Retrieve limit is at most 1000, and AgentContextOptions validation rejects any configuration that would derive a larger one (`Build_MaxCandidateConfiguration_RetrieveLimitWithinBounds`).
- The tool descriptors' `inputSchema` states `limit` minimum 1 and maximum 1000.

### R-B5. The REPL envelope and a third copy of the error rules

Severity low. Likelihood medium.

Evidence:

- The REPL already has `Retryable` in its envelope (`src/QBrainAi.Repl.Core/IYamlEnvelope.cs:145@a8d3934b`).
- It has its own classifier mirroring the server's (`src/QBrainAi.Repl.Core/ReplMcpErrorClassifier.cs:7,77@a8d3934b`).
- Round 5 adds `ErrorCodes` as "the one table" read by the server, the REPL, and the plugin (arch:314), but names no change to `ReplMcpErrorClassifier`.

Mitigation: a golden contract AC. For every `ErrorCodes` entry, including 409 `conflict` with each `retryable` value, the REPL envelope's `code` and `retryable` equal the server's.

### R-B6. Version skew (old plugin with new server, new plugin with old server or old REPL, federation)

Severity medium. Likelihood medium.

Evidence:

- New plugin against an old server or an old REPL:
  - A missing mint route, or `unknown_method` from an older REPL, is classified as an outage and kept for a later drain (C9, arch:195).
  - `ensure-repl.ps1` returns at once when any REPL is installed (plan:674).
  - Inference: a host with an older global `qbrain-ai-repl` keeps every turn `pending-<nonce>` and degraded, with no explicit version message.
  - An old server without `/agent-context/status` reads as Mode `Off` (arch:2344). That case is safe.
- Old plugin against a new server: see R-B2.
- Federation:
  - Reads merge through `FederatedSessionLogService` (`src/QBrainAi.Support.Mcp/Program.cs:642-663@a8d3934b`, `src/QBrainAi.Services/Services/FederatedSessionLogService.cs:38@a8d3934b`).
  - The architecture never mentions federation (0 matches for "Federated").
  - Inference: peers on different versions return turns without the review fields, or bodies the new projection does not expect.

Surfaces: the Phase 4 rollout, both live-check hosts (LAB-OMARCHY, PAYTON-LEGION2), and any host not refreshed by `InstallReplTool`.

Mitigation:

- Add to the Stop gate warning AC (FR-MCP-SESSIONTURN-005-AC005): a turn held pending by `unknown_method` or the scoped mint 404 is warned with the installed REPL version and the server's answer.
- Add to the rollout AC: every host that runs a live check records the REPL version that answers `workflow.sessionlog.mintTurn`.
- Add one federation AC: reads through a federation peer that lacks the Phase B fields return those fields as `null` (C11).

## Runtime behavior

### R-R1. WorkspaceResolutionBehavior on every command and query, through a singleton dispatcher

Severity blocker if the behavior mutates the DI-scoped `WorkspaceContext` as written; otherwise high. Likelihood high.

How develop dispatches:

- `Dispatcher` is registered as a singleton (`src/QBrainAi.Cqrs/ServiceCollectionExtensions.cs:24-25@a8d3934b`).
- It keeps the root `IServiceProvider` (`src/QBrainAi.Cqrs/Dispatcher.cs:17,25@a8d3934b`).
- It resolves every handler and every `IPipelineBehavior` from that root provider on each dispatch (`Dispatcher.cs:93,96@a8d3934b`).
- `WorkspaceContext` is scoped (`src/QBrainAi.Support.Mcp/Program.cs:519@a8d3934b`, `src/QBrainAi.Support.Mcp/McpStdio/McpStdioHost.cs:324@a8d3934b`).
- Existing handlers already work around this:
  - Memory handlers build their own context per call: `new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath })` (`src/QBrainAi.Services/Memory/Commands/RememberMemoryCommandHandler.cs:16@a8d3934b`, and 12 more handlers).
  - Use-case handlers prefer `command.WorkspacePath` over the injected context (`src/QBrainAi.Services/UseCases/Commands/CreateUseCaseCommand.cs:21-34@a8d3934b`).

What the architecture specifies:

- The behavior resolves the workspace "into the scoped `WorkspaceContext`" (arch:250).
- It refuses an unresolved workspace with 404 `not_found`, `details.resource` `workspace` (arch:250).
- Inference: resolved from the root provider, the behavior writes one process-wide `WorkspaceContext` shared by concurrent dispatches, which is a cross-workspace race. Where scope validation is on, it instead fails with "Cannot resolve scoped service from root provider". `ValidateScopes` is used in `src/QBrainAi.McpAgent.SampleHost/SampleHostPreviewFactory.cs:46-47@a8d3934b`.

Existing callers that send an empty `WorkspacePath`:

- `src/QBrainAi.Support.Mcp/Controllers/UseCasesController.cs:504@a8d3934b`
- `src/QBrainAi.Support.Mcp/McpStdio/FwhMcpTools.Memory.cs:63@a8d3934b`
- `src/QBrainAi.Support.Mcp/Services/TransactionGatedMemoryService.cs:279-280@a8d3934b`
- Global memory with an empty workspace context is required behavior:
  - FR-MCP-MEMORY-001: "Global memories ... are not stamped with a workspace owner" (`store/listFr.yaml:3146` onward);
  - `tests/QBrainAi.Support.Mcp.Tests/Memory/MemoryGlobalDefaultWorkspaceTests.cs:16,30@a8d3934b`.
- Inference: a behavior that refuses an unresolved workspace breaks global memory and the ambient-context use-case paths.

Scope the architecture never names:

- The behavior covers 66 existing handler files: UseCases 23, Products 11, Memory 10, GraphRAG 17 (`git grep` at `a8d3934b`).
- The architecture names none of them (0 matches for Products, McpStdioHost, RunConsolidateJob, or any GraphRag command).
- The dispatcher is wired in two composition roots: `Program.cs:489-490@a8d3934b` and `McpStdioHost.cs:319-320@a8d3934b`. The architecture names only "DI" (arch:4762).
- Inference: if the behaviors are registered only in `Program.cs`, the stdio MCP transport bypasses ModeGateBehavior (Mode `Off` not enforced) and workspace resolution.

Why existing suites will not catch it:

- Existing suites build their own containers (`services.AddCqrs(...)` at `tests/QBrainAi.Support.Mcp.Tests/Memory/MemoryGlobalDefaultWorkspaceTests.cs:198@a8d3934b`, `MemoryS1Harness.CoreB.cs:171@a8d3934b`).
- Inference: G2 stays green while production behavior changes.

Requirement drift:

- TR-MCP-MT-002: an unregistered `X-Workspace-Path` is answered with 400 (`store/listTr.yaml:2651` onward). On MCP and the REPL the behavior answers 404. Two statuses for one condition break "identical on every surface".
- TR-MCP-CQRS-005 claims built-in `LoggingBehavior` and `ValidationBehavior` (`store/listTr.yaml:1437` onward). Develop registers no production behavior; only the test `PrefixBehavior` exists (`tests/QBrainAi.Cqrs.Tests/DispatcherTests.cs:74,145@a8d3934b`). This would be the first production behavior.

Surfaces: Phase 4 SharedComponents, G3, and the live checks. Phase 2's pass-through stubs (arch:4712) hide it.

Mitigation (AC changes on the WorkspaceResolutionBehavior TR):

- (a) The resolved workspace travels with the dispatch, and no DI-scoped service is written. Case: `WorkspaceResolution_ParallelDispatchesTwoWorkspaces_NoCrossWorkspaceRead`, run on the production singleton dispatcher.
- (b) An empty `WorkspacePath` on the existing global-memory and ambient-context records keeps today's behavior. Cases: `Memory_GlobalCreate_EmptyWorkspace_ProductionPipeline_StoresNullOwner`, `UseCases_AmbientWorkspace_ProductionPipeline_Unchanged`.
- (c) The behavior is registered in both composition roots. Case: `StdioHost_ModeOff_GatedCommand_409AgentContextDisabled`.
- (d) The existing UseCases, Memory, Products, and GraphRAG suites run once through the production DI composition.
- Reconcile TR-MCP-MT-002 (400) with the behavior's 404.
- Operator question: if (a) cannot be met without changing the `Dispatcher` lifetime or adding a scope per dispatch. That is a change to `QBrainAi.Cqrs`, which is published as `QBrainAI.Cqrs` (TR-MCP-CQRS-001).

### R-R2. A nested dispatch "joins the caller's transaction", which is undefined with root-resolved handlers

Severity high. Likelihood medium-high.

Evidence:

- arch:268: "A command dispatched from inside a delegate ... joins the caller's transaction and retry; it never opens a second one."
- Nested dispatches the architecture relies on:
  - DecideAgentResponseTrustCommand inside the grade transactions (arch:3008);
  - EvaluateStalenessCommand from ProjectOneResponse (arch:3087, arch:3226);
  - EvaluateStalenessCommand from Build in its sequence (arch:3486, arch:3530).
- EvaluateStalenessCommand itself runs "one SerializableWrite per response" (arch:3086).
- Handlers are transient and resolved per dispatch from the root provider (`Dispatcher.cs:93@a8d3934b`).
- Inference: the nested handler receives either the root-scoped `McpDbContext` shared by all requests or another instance. Neither gives a defined "join", and a retry of the outer delegate does not know about the inner writes.

Surfaces: Phase 4 ResponsesAndTrust and ContextGraph G3 suites.

Mitigation: AC on SerializableWrite: "A dispatch inside a delegate runs on the caller's DbContext and transaction; an outer rollback undoes the nested writes." Case: `SerializableWrite_NestedDispatch_SameTransaction_RollbackUndoesNestedWrites`, on three providers.

Operator question: if this needs the callee seam called directly instead of through `IDispatcher`.

### R-R3. A whole-session Serializable scope against "independent turn edits" and the five-second budget

Severity high. Likelihood medium-high on PostgreSQL and SQL Server, medium on SQLite.

What the decision and the design say:

- Locked decision: concurrent edits to different turns "do not interfere" (arch:96).
- Every turn write runs TurnWritePipeline inside SerializableWrite (arch:336, arch:598).
- Submit puts the session upsert and every turn in one delegate (arch:558, arch:570).

What develop does today:

- Submit opens Serializable (`src/QBrainAi.Services/Services/SessionLogService.cs:155-157@a8d3934b`).
- Inside it, it loads the whole session graph with six includes (`SessionLogService.cs:318-343@a8d3934b`).
- That load already deadlocks on SQL Server (1205, `SessionLogService.cs:345-357@a8d3934b`, FR-MCP-TRIAGESTORE-002).

The budgets:

- Non-submit saves have 5 s (`src/QBrainAi.Storage/StorageCommandBudget.cs:12@a8d3934b`).
- Submit defaults to 30 s (`src/QBrainAi.Services/Options/SessionLogSubmitOptions.cs:14@a8d3934b`).
- FR-MCP-SESSIONLIFE-003's last AC: a dense session update finishes inside the five-second storage budget, or returns one classified retryable outcome inside it (`store/listFr.yaml:4598` onward).
- arch:268 says the up to four attempts run within the unit's existing budget, without saying whether the budget is per attempt or for all of them.

Provider behavior (inference):

- SQL Server SERIALIZABLE takes key-range locks over the session's child rows.
- PostgreSQL SSI aborts with 40001 on read-write dependencies over the same rows.
- A non-deferred Microsoft.Data.Sqlite transaction begins as a write transaction (`~/.nuget/packages/microsoft.data.sqlite.core/10.0.0/lib/netstandard2.0/Microsoft.Data.Sqlite.xml:2460-2474`: deferred is opt-in). Every writer to the database file serializes.

Inference: the prompt hook, the bridge, a verb, and the Stop hook can write different turns of one session concurrently. They conflict, re-load the dense graph up to four times, and end in 409 retryable or budget expiry, which means queued and degraded persistence. That is the interference the decision forbids.

Surfaces: the G3 three-provider cases, and the Phase 4 live checks on this workspace's long sessions.

Mitigation (AC changes):

- Add to FR-MCP-SESSIONTURN-004 (or its TR): "Two concurrent writes to different turns of one session both commit within the storage budget, with no retryable answer, on each provider." Case: `TurnWrite_ConcurrentDifferentTurnsSameSession_BothCommit_NoConflictAnswer`, dense-session fixture.
- State that the budget covers all attempts.
- Run FR-MCP-SESSIONLIFE-003's dense-session AC through SerializableWrite.
- Operator question: if the AC cannot hold with a whole-session scope (narrowing the lock to the turn row is design).

### R-R4. Long agent-context transactions and the cost of a status read at SessionStart

Severity high in `Pilot` or `Stateless`. Likelihood medium; the `Off` default limits exposure.

Long transactions:

- Build's SerializableWrite delegate holds all of these (arch:2692-2707):
  - TurnLocator, `ReadCommit`, and the profile reads;
  - RetrieveAgentContextCandidatesQuery (hybrid search, and GraphRAG when preferred);
  - the read-only staleness rule over every prior-response candidate, with files hashed at HEAD;
  - PackageAssembler and the inserts.
- Inference with R-R3's SQLite note: while a Build runs, every session-log write to that database waits behind it, so a 5 s save expires and the plugin queues it.
- The 8.6 live check runs on a separate single-user instance (arch:4885), so it would not observe this.

The status read:

- GetAgentContextStatusQuery counts responses by effective status, running the staleness rule over every Submitted, Grading, and Trusted response (arch:2665, arch:2672).
- Plugins call it at every host session start (arch:2344, arch:3359).
- The SessionStart hook's default bound is 30 s (FR-MCP-SESSIONLIFE-004).
- A hook timeout makes the host run `Pilot` and `Stateless` as `Off` (arch:151). Inference: the feature silently degrades as the response count grows.

Surfaces: production `Pilot` on the shared servers (arch:4879).

Mitigation:

- AC: "A session-log write that runs while a Build or a staleness evaluation runs on the same SQLite database commits within its 5 s budget." Case: `Build_SlowRetrieval_ConcurrentTurnWrite_CommitsWithinBudget`.
- AC: "GetAgentContextStatusQuery answers within the SessionStart hook bound with 1000 trusted responses." Settling the 6.4 and 6.5 contradiction (R-H2 item 3) in favor of the stored count meets it.
- Operator question: if the first AC requires moving source reads outside the transaction.

### R-R5. DbErrorClassifier replacing message-text predicates

Severity medium. Likelihood medium.

Evidence:

- Today's text predicates:
  - SQLite "UNIQUE constraint failed" (`SessionLogService.cs:234@a8d3934b`);
  - deadlock text (`SessionLogService.cs:357-368@a8d3934b`);
  - `SessionLogService.cs:812-817@a8d3934b`;
  - `McpErrorClassifier` busy and conflict detection (`src/QBrainAi.Support.Mcp/Services/McpErrorClassifier.cs:112-139@a8d3934b`).
- Round 5 classifies by code instead: SQLite 2067 and 1555, SQL Server 2601 and 2627, PostgreSQL 23505; transient 1205, 40001, 40P01, SQLite 5 and 6 (arch:267).
- Inference on what the classifier must handle:
  - SQLite reports UNIQUE as primary code 19 with extended code 2067, so the classifier must read the extended code.
  - SQLite busy can surface as `StorageCommandBudgetExceededException` (`StorageCommandBudget.cs:67-80@a8d3934b`), which is neither a provider code nor transient, so no retry happens.
  - PostgreSQL 40001 can arrive at commit, which the diagram draws (arch:278).
- TR-MCP-TRIAGEERR-001 still says conflict is "retryable false unless SQLITE_BUSY or deadlock" (`store/listTr.yaml:6873` onward). The plan's updated-requirements list (plan:483-526) does not include it.

Surfaces: Phase 4 SharedComponents G3.

Mitigation:

- Classifier cases using exceptions captured from each real provider: `DbErrorClassifier_SqliteExtended2067_IsUniqueViolation`, `DbErrorClassifier_PostgresCommit40001_IsTransientConflict`, `DbErrorClassifier_BudgetExceeded_NotTransientConflict`.
- Add TR-MCP-TRIAGEERR-001 to the updated-requirements list, with the retryable 409 text.

### R-R6. Routing existing session-log routes through IDispatcher

Severity medium. Likelihood medium.

Dispatcher retention:

- The dispatcher serializes every request to YAML (`Dispatcher.cs:69@a8d3934b`).
- It keeps the last 200 request and result texts in memory (`Dispatcher.cs:16,235-256@a8d3934b`).
- Turn text is stored as written and redacted only on read (arch:86).
- Inference: whole session documents, raw text included, sit in process memory, and every submit pays an extra YAML serialization.

The decorator chain:

- Today's chain is `SessionLogSanitizingService`, then `FederatedSessionLogService`, then `TransactionGatedSessionLogService`, then `SessionLogService` (`Program.cs:642-663@a8d3934b`).
- The architecture names only `TransactionGatedSessionLogService.cs`, as a file to own (arch:4766).
- Inference: GetSessionLogQuery and QuerySessionLogsQuery built on TurnReadProjection and PagedQuery (arch:1949) bypass the federated merge of `FederatedSessionLogService.QueryAsync`.

Mitigation:

- AC: "No dispatch log retains an unredacted session-log or agent-context request body."
- AC: "GetSessionLogQuery and QuerySessionLogsQuery return the same federated results as today's `ISessionLogService.QueryAsync`" (golden case with a federation peer).

### R-R7. The ModeGateBehavior marker fails open

Severity medium. Likelihood low-medium.

Evidence:

- Gating is opt-in through `IModeGatedRequest` (arch:251).
- The sequence note says "gated (every command)" (arch:3414), but DecideAgentResponseTrustCommand is exempt (arch:251, arch:2656).
- Inference: a new command that omits the marker runs in `Off`, and nothing fails.

Mitigation: AC with the case `ModeGate_EveryAgentContextCommandExceptDecide_ImplementsMarker` (reflection over the assembly).

### R-R8. Staleness dispatch from requirement writes

Severity medium. Likelihood medium in `Pilot`.

Evidence:

- A requirement update or delete dispatches EvaluateStalenessCommand while Mode is not `Off` (arch:251, arch:3087, arch:4762).
- An empty id list selects every Submitted, Grading, and Trusted response (arch:3088).
- Unstated:
  - whether the dispatch is synchronous;
  - whether its failure fails or rolls back the requirement write;
  - how it nests in the requirement transaction (`RequirementsRecoveryService.cs:80-126`, the precedent named at arch:268).

Mitigation: AC: "A requirement write evaluates only the responses that cite that requirement, and a staleness failure never fails or rolls back the requirement write." Case: `RequirementUpdate_StalenessFailure_RequirementWriteCommitted`.

## Data and migrations

### R-D1. Three migrations on three providers; Down tested only on SQLite

Severity medium. Likelihood medium.

Evidence:

- StorageSchema generates three migrations per provider, in order (plan:119): `_AddSessionLogTurnIngressNonce`, `_AddSessionLogTurnReviewsAndRequestClass`, `_AddAgentContext`.
- Each provider project holds 41 migrations at `a8d3934b` (Sqlite, PostgreSql, SqlServer).
- The schema change is large:
  - 16 new Phase C tables (arch 6.3 entity headings, arch:2368-2503);
  - the review reason table (arch:1831);
  - about 22 new turn columns (arch:1830);
  - two filtered unique indexes (arch:479).
- S3's RED tests list "SQLite Up/Down and backfill tests" and only apply tests for PostgreSQL and SQL Server (plan:631).
- Rollback "only through their tested Down" (arch:4880, plan:674, plan:676).

Mitigation: AC on StorageSchema: Up, Down, Up for all three migrations on all three providers. Case: `Migrations_UpDownUp_ThreeProviders_SchemaAndDataEqual`.

### R-D2. Binary-only rollback with new required columns

Severity medium. Likelihood low-medium.

Evidence:

- The plan's rollback is "`UpdateService` to the prior build" (plan:674).
- The review `Status` is required (arch:1830).
- The backfill runs once, at migration (arch:1832).
- No database default is named anywhere (0 matches for defaultValue, HasDefaultValue, or "database default" in arch and plan).
- A binary ahead of its migration already answers 503 `pending_migration` (`McpErrorClassifier.cs:58-68@a8d3934b`).
- Inference: EF adds a non-null column to an existing table with the type default (empty string). Turns the prior binary inserts during a rollback get an empty review status and class. After roll-forward they are neither `Unstarted` nor a valid class, and the backfill does not run again.

Mitigation: S3 AC: "The review status columns default to `Unstarted` and `RequestClass` to `Unclassified` in the database; reads map an unknown stored status to its ReviewDefaults value." Case: `Rollback_PriorBinaryInsertsTurn_RollForward_ReadsDefaults`.

### R-D3. Legacy rows at the CompletionGate deploy

Severity low-medium. Likelihood high for turns open at deploy time.

Evidence:

- The backfill sets `Unclassified` and `Unstarted` (arch:110, arch:115).
- `Unclassified` is never accepted on a write, and Code and Docs completions need a terminal hostile review (arch:122).
- `closed`, `canceled`, and `cancelled` rows stay terminal (arch:320, arch:1837).
- Inference: every legacy turn still in progress at the gate deploy needs a class and, for Code and Docs, an HV or `Other` review before it can complete.

Mitigation: AC on the CompletionGate deploy: the deploy receipt lists every in-progress turn at deploy time with its `Get-CloseRemedy` items.

### R-D4. Readable keys across workspaces

Severity medium. Likelihood low.

Evidence:

- Agent-context keys are store-wide string primary keys built from the parent id (arch:2361), and every entity has the `Workspace` query filter (arch:2363).
- ReadableKeyAllocator takes "the highest stored suffix within the key's unique index scope" (arch:300).
- Minted request ids are unique per workspace only; legacy ids are not (arch:479). The example key in arch:2368 is built from a legacy id.
- Inference: two workspaces holding the same legacy request id collide on `pkg-` and `rsp-` keys. Under the query filter the allocator cannot see the other workspace's suffixes, retries until its five-attempt exhaustion, and returns a 409 retryable on every attempt (also `review-arch-r5/edit-ed-cm.md:40`).

Mitigation: AC with the case `ReadableKeyAllocator_SameParentIdTwoWorkspaces_DistinctKeys_NoExhaustion`.

### R-D5. Root ids on child rows

Severity medium (HV). Likelihood medium.

Evidence:

- arch:85: "every record carries the root ids of its parents (workspace, MCP session, request, and for agent context the package and response)."
- Child rows carry only `WorkspaceId` and their parent key:
  - package focus refs, inputs, and input links (arch:2393-2397);
  - claims, evidence, and grade findings (arch:2444-2465);
  - HV items (arch:2488);
  - `SessionLogTurnReviewReasonEntity` (arch:1831).
- The standing operator rule treats a missing parent id as a design failure (memory "Carry root ids").

Mitigation: requirement text in arch:85 stating exactly which ids a child row carries.

Operator question: if the answer is to add the session and request ids to every child table.

## Delivery and process

### R-P1. The analyzer branch contradicts the architecture's namespace, file, and test-name rules

Severity: blocker for the Phase 2 compile if the analyzer branch merges first. Likelihood high.

Evidence (subagent read of `N/`; RootNamespace lines verified here):

- Rule configuration on `N/`:
  - IDE0130 is a warning, checking that a namespace equals RootNamespace plus folder (`N/.editorconfig:157-159`).
  - `TreatWarningsAsErrors` stays on (`N/src/Directory.Build.props:10`).
  - MA0048 (one type per file) is a warning (Meziantou analyzer config line 244).
- RootNamespace values:
  - `QBrainAi.Services` keeps `QBrainAi.Support.Mcp` (`src/QBrainAi.Services/QBrainAi.Services.csproj@a8d3934b` and `N/src/QBrainAi.Services/QBrainAi.Services.csproj:6`).
  - `QBrainAi.Storage` becomes `QBrainAi.Support.Mcp.Storage` on `N/` (line 6).
- The architecture's rule is "assembly name followed by its folder path" (arch:241). Its names include `QBrainAi.Services.SessionLog.Commands` (arch:479, arch:512), `QBrainAi.Services.AgentContext.*` (arch:2652), and `QBrainAi.Storage.Errors|Transactions|Idempotency|Keys|Paging` (arch:267-301).
- Inference: each of those names fails IDE0130 under `N/`. The existing sibling `src/QBrainAi.Services/UseCases/Commands/AddUseCaseFlowCommand.cs:9` uses `QBrainAi.Support.Mcp.UseCases.Commands`.
- MA0048 conflicts with "a record and its handler in the same file" (arch:512, arch:2652). `N/` has not split any such file.
- Test names:
  - Commit `6f883f0d` renamed 4,468 test names to remove underscores (CA1707).
  - The plan and architecture name 455 distinct underscore identifiers (plan 360, arch 171, overlap 76, environment variables excluded).
  - Neither document mentions CA1707, Meziantou, MA0048, or the branch; arch:241 mentions IDE0130 only.
- Merge positions:
  - `N/` has 514 uncommitted modified files and a 53-project SDK switch to `Meziantou.NET.Sdk` (pinned 1.0.185).
  - The docs worktree is based on `f56dcf70` and is 12 commits behind `a8d3934b`.
  - `N/` is `a8d3934b` plus 7 commits.
- Line drift in cited files is small: `SessionLogService.cs` grows by 3 lines, with the anchor at 1355 moving to 1358. 49 of the architecture's 77 short-form line citations point into files that `N/` changes.

Surfaces: the Phase 2 exit (`./build.ps1 Compile`), and Phase 3 (test names).

Mitigation (Operator question):

1. The merge order of `chore/netanalyzers-all` and this plan.
2. The namespace rule: either RootNamespace equals the assembly name per project, or the architecture's names become RootNamespace plus folder.
3. MA0048: separate files for each record and handler, or an exemption.
4. The spelling of the plan's named test cases.

Requirement change in any case: a Phase 2 exit AC, "every new type passes the repository's analyzer configuration at the Phase 2 base commit."

### R-P2. Store evidence names tests that the analyzer branch renamed

Severity medium. Likelihood high once `N/` merges.

Evidence (subagent):

- 36 FR AC evidence entries across 14 FRs, and 37 TEST descriptions, cite underscore test names.
- Every cited name that exists at `a8d3934b` (32 from FR evidence, 95 from TEST text) is gone on `N/`.
- `ValidateTraceability` reads only the `docs/Project` markdown ids and never source, test names, or evidence (`build/Build.ValidateTraceability.cs:16-80`; TraceabilityValidator id regexes at :9-24, failure rule at :115).
- So no gate catches drift between named cases and code; only HV does.

Mitigation:

- An updated-requirements entry in P0 re-points the evidence to the renamed tests (the records listed).
- A Phase 3 exit AC: the RED receipt maps every catalog-named case to a discovered test (Nuke `Test` listing and Pester discovery), with zero missing.

### R-P3. The plan is not re-derived from round 5

Severity high (HV failure is certain, and P0 would create the wrong TODO set). Likelihood certain until the plan is derived.

The plan still lacks round 5:

- 0 matches for SharedComponents, SerializableWrite, WorkspaceResolutionBehavior, ModeGateBehavior, DbErrorClassifier, PageResult, ITerminalTurnOutcomeGuard, ScoreLexemeJsonConverter, and `agent_context_transition_invalid`.
- It keeps the old names:
  - `ISessionLogTurnUpsertGuard` (plan:355, plan:607);
  - `SessionLogReviewScoreJsonConverter` (plan:432, plan:627);
  - `SessionTurnCompletionGateException` (plan:362, plan:627, plan:635, plan:645).

Delivery text that disagrees with the architecture:

- The bounded contexts (plan:118-129) and the TODO chain (plan:135) have no SharedComponents or `IMPL-SHAREDCOMPONENTS-001`.
- The rollout (plan:131, plan:674) leaves out SharedComponents, which the architecture orders second (arch:4745, arch:4878).
- The breaking-changes list (plan:673) omits the round-5 breaks: the `closed` refusal, `ClassifiedError` bodies, the retryable 409, and the workspace 404 on existing CQRS records.
- plan:676 still calls Phase C "additive and not breaking".

There is no `review-arch-r5/edit-plan.md`, so the derivation of `PLAN-EDITOR-BRIEF.md` has not run.

Mitigation: run the plan derivation before HV, with three additions to that brief's tasks:

- SharedComponents in every delivery list and in the TODO chain;
- the round-5 breaks in plan:673;
- TR-MCP-TRIAGEERR-001, TR-MCP-MT-001/002, TR-MCP-CQRS-005, and FR-MCP-MEMORY-001 in "Updated existing requirements".

### R-P4. The critical path, and SharedComponents gate cases that need later contexts

Severity high. Likelihood high.

The path:

- StorageSchema, then SharedComponents (serialized, alone), then six server contexts (arch:4745-4751, arch:4765).
- Every context ends in its own HV at 98/98 (arch:4835-4866).

Convergence so far:

- Plan-readiness HV scores rose about one point per round: r1 89/86, r2 90/87, r3 91/89, r4 92/90 (`docs/receipts/hv/hostile-validator-plan-readiness-merged-r1..r4-*.md`).
- Inference: the serial HV gates on the critical path number at least twelve:
  - five prerequisites (plan:103);
  - Phases 1, 2, and 3;
  - StorageSchema and SharedComponents;
  - the slowest server context;
  - HostPluginAdoption, Documentation, and the phase-wide check.
- SharedComponents adds one serial gate.

Cases that cannot run at their own gate:

- SharedComponents' own case list includes `TurnWrite_ForcedInterleaving_ReviewResetVsComplete_NeverClosedWithoutReview`, `TurnWrite_ForcedInterleaving_ClassChangeVsComplete_GateSeesCommittedClass`, and `TurnWrite_ForcedInterleaving_ChoreFlipVsRunningWrite_RunningKept` (arch:268).
- Those cases exercise TurnWritePipeline (SessionTurnLogging) and ReviewDefaults and CompletionGate (TurnReviews), which do not exist at SharedComponents' exit.
- The exit requires its own tests green before any caller starts (arch:4871).

Mitigation: an AC move. Assign the three `TurnWrite_ForcedInterleaving_*` cases to the integration gate of whichever of SessionTurnLogging and TurnReviews merges later. SharedComponents keeps `SerializableWrite_RetriesExhausted_409ConflictRetryable_NothingSaved` plus a generic forced-interleaving case on a test entity.

### R-P5. File ownership against the surfaces that still need behavior

Severity medium. Likelihood high.

Evidence:

- Phase 2 completes controllers and MCP tools as shared files that "only dispatch" (arch:4762, plan:111).
- But each unit maps typed failures to its surface's body (arch:598), keeping today's bodies per route (arch:1870, arch:1901). That is per-route logic Phase 4 contexts need, and no context owns those files.
- Review behavior spans files owned by two contexts that run in parallel:
  - SessionTurnLogging owns `SessionLogService.cs` and `TransactionGatedSessionLogService.cs`, including the review wiring (arch:4766);
  - TurnReviews owns the review policies (arch:4767).
- `McpErrorClassifier.cs` gets one SharedComponents branch (arch:4765).
- A change outside a context's files stops for a Phase 2 amendment (arch:4776).
- Inference: amendments serialize the parallel phase.

Mitigation: Phase 2 exit AC: every controller and MCP tool holds its complete error mapping per the unit's error list, proven by one golden case per route, so Phase 4 never edits a surface file.

### R-P6. QB-AGENT-001 contract changes in round 5 without a CONTRACT entry

Severity medium. Likelihood medium.

Evidence:

- Any contract change needs a CONTRACT entry and Codex's ACK before dependent work integrates (plan:91).
- Round 5 changed C9: the retryable 409 `conflict` and the `ErrorCodes` table (arch:195). No new CONTRACT entry is recorded. The entries named at arch:136, arch:186-189, and arch:4878 concern the PowerShell-only change.
- C8 and C9 name `session_turn_completion_gate` as the refusal code (arch:194-195), but the server body's `code` is `validation_error` with that token in the message (arch:83, arch:228). Inference: a consumer that branches on `code` misses the refusal.
- Codex's ACK of the 2026-10-12 CONTRACT entry is still open (arch:188-189).

Mitigation:

- Reword C8 and C9 to name the body code and the message token.
- Add a P0.7 AC: post a CONTRACT entry listing every round-5 change to C1-C12 and record the ACK.

### R-P7. The code freeze and the analyzer worktree

Severity low-medium. Likelihood medium.

Evidence:

- plan:42 says nothing under `src/`, `tests/`, `build/`, or `plugins/` changes in any repo until approval.
- `N/` holds 7 commits and 514 uncommitted modified files.
- The operator asked for the analyzer (arch:241, "Add the analyzer"), but the freeze text has no exception.

Mitigation: Operator question. Record the analyzer exception verbatim in plan:42, or hold the branch.

### R-P8. P0 bulk creation

Severity low. Likelihood low.

Evidence:

- P0 creates records through `workflow.requirements.createBatch` (plan:181).
- The plan's baseline (FR 352, TR 480, TEST 512, plan:180) no longer matches the store export (353, 487, 512).
- P0 step 3 re-verifies before creating, which covers the drift.

Mitigation: AC on P0 step 4: "a rerun after a partial batch creates only the missing records."

## Documentation and HV

### R-H1. Size, convergence, and unrendered diagrams

Severity high. Likelihood high.

Size:

- The architecture is 309,477 bytes at HEAD `89859efc` (`git cat-file -s`) and 629,718 bytes in the working copy. The brief's 423 KB figure is likely an intermediate state.
- `docs/Stateless-Agent-Context-Management.md` is 313,277 bytes. The plan is 405,670.

Diagrams:

- The architecture has 172 mermaid blocks: 111 flowcharts, 46 sequence diagrams, 11 ER diagrams, and 4 state diagrams.
- No round-5 render exists. The newest file in `scratchpad/arch/pages` is from 01:38; round 5 ran 17:52 to 18:24.
- Every editor reports that `mermaid.parse` was not run: `edit-ed-a.md:3`, `edit-ed-aseq.md:3`, `edit-ed-b.md:3`, `edit-ed-cu.md:3`, `edit-ed-cm.md:53`.
- The largest flowcharts have about 33 nodes (arch:813) and 32 (arch:2957). All 46 sequences have at most five participants (counted), so ed-cseq's width concern is resolved.

HV history: scores rose about one point per round (R-P4).

Mitigation (process, no new requirement):

- Parse and render all 172 blocks to the page standard before HV.
- Scope the HV brief's claims by section.

### R-H2. Cross-file dependencies from the round-5 edit logs, checked in the assembled architecture

Resolved in the assembled document:

- Hook status `turn-opened-degraded` and queue `storedStatus` (arch:207, arch:211).
- The Secrets bullet (arch:86).
- Policy and nested-diagram lists (arch:4891, arch:4893).
- Resolve-MintOutcome's outage exit includes the retryable 409, 503, and contention (arch:431).
- Update-TurnState's nested replace (arch:459).
- TurnWritePipeline: Submit mode, the ReviewValidation step, and the Replace gate (arch:336, arch:356).
- Nested dispatch joins the caller's transaction (arch:268).
- `relevant_to` is protected (arch:404).
- The mint 409 is in its error list (arch:524).
- The Upsert "retried once" text is gone.
- BindVerbToTurn's closed write target (arch:923, arch:956).
- The CompletionGate PUT case (arch:2047, arch:2053).
- Projection `Skipped` selection (arch:2508, arch:3225).
- The owner of the requirement-update dispatch (arch:4762).
- 0 remaining matches for `ISessionLogTurnUpsertGuard`, `SessionTurnCompletionGateException`, `AgentContextException`, `IAgentResponseStalenessRule`, `AgentContextKeys_`, `AgentContextRetrieveRequest`, `AgentContextStalenessResult`, and `AgentContextProjectionResult`.
- Sequence participants are at most five everywhere.

Still inconsistent (each is a likely HV FAIL; severity medium unless noted):

1. **Build staleness.** The activity reads staleness read-only and writes no status (arch:2684, arch:2701, arch:3087). The sequence persists it by dispatching EvaluateStalenessCommand inside Build's transaction (arch:3486, arch:3530). That command runs its own SerializableWrite per response (arch:3086). The ed-cseq and ed-cu edit logs disagree on this point.
2. **The staleness dependency set.** `depends_on` is included at arch:391, arch:397, and arch:2472 and excluded at arch:3086 (`edit-ed-cm.md:38`).
3. **Status counts.** 6.4 counts the stored status (arch:2603). 6.5 counts the effective status, with a staleness evaluation (arch:2665, arch:2672). Unresolved conflict at `edit-ed-cu.md:62`. This also drives R-R4.
4. **The pilot report.** `refusedPrompts` appears in 6.5 (arch:3265, arch:3275) but not in the 6.4 DTO (arch:2626).
5. **Review reset on a terminal turn (high).** These texts say only completed or failed turns refuse a reset:
   - 3.4 (arch:230);
   - the 5.1 state diagram (arch:1854);
   - the Reset diagram (arch:1911).

   One sentence of the Reset error list says both "a `canceled` or `closed` turn can still be reset" and "(completed, failed, closed, canceled, or cancelled; record unchanged)" (arch:1901). ed-b reported F10 resolved as "every terminal status" (`edit-ed-b.md:11`).
6. **Agent-surface statuses.** See R-B3: arch:320 against arch:560 and arch:600.
7. **The converter name.** 3.4 Scores still names `SessionLogReviewScoreJsonConverter` (arch:227).
8. **Addendum A is stale (high, because StorageSchema could build from it).** `60-addendum-entities.md` was last written at 17:40, before the editors started, and was not regenerated.
   - A.5 lacks `PayloadSha256`, the `AgentContextPackageInputLink` entity, and the inputs' `ContainsSecret` and `ResponseStatus` (arch:5085-5140).
   - A.8 lacks `RecordedUtc` and `PayloadSha256` (arch:5214-5255).
   - Child `Ordinal UK` keys omit the parent, against 6.3's composite keys (arch:2362, arch:2393).
9. **The section 9 index** names `CreateHvRequestAttempt` as a nested diagram and as a delegate, and names `ProjectOneResponseSequence` (arch:4893).
   - Neither heading exists: ed-cu deleted the attempt diagram (`edit-ed-cu.md:3`).
   - The sequence is now headed `ProjectOneResponse` (arch:4155), the same name as the nested activity at arch:3238.
10. **The Mode cache.** It lives in `turn-state.yaml` through `Update-TurnState` (arch:3359, arch:3384), but is absent from the file schema and writer list (arch:211, arch:485; `edit-ed-cu.md:65`).
11. **ModeGate wording.** "Gated (every command)" (arch:3414) against the DecideAgentResponseTrustCommand exemption (arch:251).
12. **Attempt bounds.** SerializableWrite runs a delegate at most four times (arch:268, arch:279). ReadableKeyAllocator declares its collisions to SerializableWrite with bounds of six attempts (minter) and five (agent-context keys) (arch:300, arch:523, arch:2361). Both bounds cannot hold.
13. **The authority sentence.** arch:6 says the plan "remains the authority for ... gates, tests, and acceptance criteria", against the architecture-first ruling at arch:90.

Mitigation: one coordinator pass over these 13 items, plus the plan derivation of R-P3. Each fix is a text change; none adds mechanism.

## Top ten risks in priority order

1. **R-R1** (blocker or high): WorkspaceResolutionBehavior on all 66 existing handler files, through a singleton dispatcher that resolves from the root provider. It breaks empty-workspace global memory, and stdio may bypass both behaviors.
2. **R-P1** (blocker for the Phase 2 compile if the analyzer merges first): IDE0130 against the "assembly name plus folder" namespaces, MA0048 against one file per record and handler, and CA1707 renames against 455 underscore names in the documents.
3. **R-P3** (high, certain): the plan is not re-derived. SharedComponents is missing from contexts, TODOs, and rollout; old type names remain; the round-5 breaks are unlisted.
4. **R-H2** (high): thirteen cross-part inconsistencies remain. The worst are the Build staleness contradiction, the stale Addendum A, and the self-contradicting review-reset rule.
5. **R-R3** (high): the whole-session Serializable scope breaks "independent turn edits" and the five-second dense-session AC on PostgreSQL and SQL Server.
6. **R-R2** (high): a nested dispatch "joining" the caller's transaction is undefined with transient, root-resolved handlers.
7. **R-R4** (high in `Pilot`): Build holds a write transaction across retrieval and git reads on SQLite, and the status query evaluates every response's staleness at each SessionStart.
8. **R-P4** (high): twelve or more serial HV gates on the critical path, at about one point per round; SharedComponents' gate lists cases that need contexts merged after it.
9. **R-H1** (high): the architecture doubled to 630 KB, and none of the 172 diagrams has been parsed or rendered since round 5.
10. **R-P5** (medium, likely): surface error mapping lives in Phase 2 shared files that no Phase 4 context owns, so amendments serialize the parallel phase. R-B1 and R-B2 (client package API and changed-route error bodies) follow closely.

## Summary for the coordinator

Eight of the top ten risks are high or worse, and two of those are blockers.

The first blocker is R-R1. The architecture registers WorkspaceResolutionBehavior for every command and query and has it write the scoped `WorkspaceContext`. But develop's `Dispatcher` is a singleton that resolves handlers and behaviors from the root provider (`Dispatcher.cs:17,93,96@a8d3934b`). The 66 existing handler files (UseCases, Memory, Products, GraphRAG) are never mentioned, and global memory and the ambient use-case paths send an empty `WorkspacePath`. Stdio has its own composition root (`McpStdioHost.cs:319`).

The second blocker is R-P1 (analyzer branch):

- IDE0130 uses RootNamespace (`QBrainAi.Support.Mcp` for Services), so the architecture's `QBrainAi.Services.*` and `QBrainAi.Storage.*` names fail the build.
- MA0048 contradicts "record and handler in one file".
- 4,468 tests lost their underscores while the documents name 455 underscore cases.
- The merge order, namespace rule, and test spelling need operator answers.

Certain HV failures:

- The plan is not re-derived: no SharedComponents, the old type names, and none of the round-5 breaks listed.
- Thirteen cross-part inconsistencies remain in the assembled architecture. The worst are Build persisting staleness in its sequence while its activity forbids it, stored versus effective status counts, the self-contradicting review-reset rule (arch:1901), `cancelled` accepted versus refused, a stale Addendum A missing the round-5 columns, and SerializableWrite's four attempts against the allocator's six and five.

Runtime risks:

- The whole-session Serializable scope conflicts with the locked "independent turn edits" decision and the five-second dense-session AC (R-R3).
- Nested dispatch "joins" the caller's transaction only by assertion (R-R2).
- Build holds a transaction across GraphRAG and git reads on SQLite (R-R4).

Delivery risks:

- SharedComponents adds a serial HV gate.
- Its gate lists `TurnWrite_ForcedInterleaving_*` cases that need contexts merged after it (R-P4).

Almost every mitigation is an AC change.
