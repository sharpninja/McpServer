<!-- SPDX-License-Identifier: Apache-2.0 -->
# Stateless Agent Context Management

Status: architecture specification (BDPv4) of the stateless agent-context work, published from the same source text as the agent-context part of the architecture document `docs/architecture/SessionTurn-Review-AgentContext-Architecture.md`, pending operator approval of the merged plan PLAN-SESSIONTURNREVIEW-001 (`docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md`), into which this work is integrated (all on branch `worktree-session-20261009` until it merges into `develop`). The architecture is the authority and the plan derives from it (operator, 2026-10-12: "The plan should depend on the architecture, not the other way around."); a change to it after the plan is approved is a plan change that needs a new hostile validation. Operator directives (2026-10-11, verbatim): "Integrate attached to current plan.", "Reconstruct plan with added requirements", "Also publish the doc to develop", "Outline now, catalog in P0", "No new code until I approve merged plan.", "I want every model defined.  I want every unit of work request and respone defined.  I want every endpoint defined.", "I want a single, complete activity diagram (mermaid) for every unit of work.  I want sequence diagrams for submitting each request type and processing every response.  I want a md doc describing the who plan as architecture documentation.", and "Ensure architecture doc includes all activity, sequence and use case diagrams for this plan."

Section numbers match the architecture document, which also covers the turn-logging (Phase A) and review (Phase B) work this feature builds on. After this introduction and its summary, this document holds: section 3, which repeats the architecture document's section 3.5 (the shared components every unit of work, policy, and process calls, each defined and drawn once, subsections 3.5.1 to 3.5.6) so this document stands alone; section 6, the Phase C models, configuration, entities and their state diagrams, data transfer objects, units of work with their activity diagrams, the plugin process RunStatelessInvocation, and the sequence diagrams; section 7, the agent-context use cases (7.11 to 7.16); and section 8, delivery (8.1 to 8.6). A reference to any other section (3.1 to 3.4, 4, 5, 9, or Addendum A) points into the architecture document. The requirement records (FR, TR, and TEST with acceptance criteria and mappings) are created from this specification in Phase 1 of the merged plan (its P0 steps 1-9).

## Summary

Each agent invocation can run stateless: it receives a bounded, reproducible package containing the current request, the canonical operator profile and rules, the applicable requirements, related session-log evidence, trusted prior responses, and relevant code at the current commit. Conversation history is not carried forward; the deterministic `REQUIRED MEMORIES` block the host already injects stays in the model's input (section 3.1, "Memories in Stateless mode", FR-MCP-MEMORY-003 and FR-MCP-MEMORY-007).

- Packages, responses, grades, and relationships are authoritative relational records; response artifacts are projected into GraphRAG for semantic retrieval with a deterministic relational fallback.
- Every package records source hashes, commit identity, selection rationale, token usage, contradictions, blockers, and omitted candidates.
- Package construction fails explicitly when mandatory context exceeds the token budget.
- Every response is graded by deterministic evidence checks and by an independent model; it is trusted only at correctness and completeness of at least 98 percent with zero FAIL and zero UNKNOWN.
- Rejected and stale responses are preserved for audit and excluded from trusted retrieval.
- Relationships: `implements`, `verifies`, `depends_on`, `supports`, `contradicts`, `supersedes`, `blocks`, `derived_from`, `relevant_to`.
- An opt-in `Mcp:AgentContext` mode (`Off` by default) keeps the current stateful behavior during the pilot; in `Off` the stored records stay readable for audit.
- The Mode gates only the agent-context commands. The shared components this work brings (section 3) apply in every Mode and change existing behavior, so the work is not purely additive: `IDispatcher` in the published `QBrainAI.Cqrs` package becomes scoped, WorkspaceResolutionBehavior resolves the workspace for every command and query (a workspace that does not resolve is 400 `validation_error` with `details.field` `workspacePath`), and a deadlock or busy store that outlasts SerializableWrite's four attempts answers 409 `conflict` with `retryable: true`, which the plugin treats as an outage, while a busy store outside SerializableWrite keeps today's 503 `persistence_error` with `retryable: true` (section 3.1, "Deadlock and busy outcomes").

Execution follows the merged plan's four phases (section 8): requirements, interfaces and code stubs, test-driven unit tests with mocks, then implementation by bounded context with parallel coding agents; every unit of work is one CQRS command or query with its handler, and every identifier is a meaningful name. Plugin behavior lives once in the PowerShell plugin core, which all nine hosts use, the TypeScript hosts through logic-free adapters (operator, 2026-10-11: "You should be designing all scripts and script tools around PowerShell and only PowerShell."). The attached plan's BDPv4 implementation order, mapped onto those phases: register requirements, use cases, and acceptance criteria (Phase 1); define the interface contracts and code stubs (Phase 2); establish green, discriminating mock tests with negative controls; add authoritative storage; implement deterministic package construction; implement grading, acceptance, staleness, rejection, and supersession; add the GraphRAG projection with the deterministic fallback; expose REST, MCP, client, REPL, and plugin interfaces; pilot stateless execution against the stateful workflow before changing any default. Tests cover deterministic replay, mandatory profile completeness, ranking, contradictions, grading thresholds, stale dependencies, workspace isolation, secret exclusion, token overflow, GraphRAG fallback, concurrency, idempotency, and the full zero-failure, zero-skip suite.

## 3. Shared components (the architecture document's section 3.5, repeated so this document stands alone)

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

```mermaid
flowchart TD
    S(["Command or query dispatched through IDispatcher"]) --> W[["WorkspaceResolutionBehavior"]]
    W -->|path unregistered, or no workspace and not allowed| E0(["400 validation_error, details.field workspacePath"])
    W -->|resolved from the record or the transport, or global allowed| G{"Record implements IModeGatedRequest?"}
    G -->|no| H(["Handler runs"])
    G -->|yes| M{"Mcp:AgentContext:Mode is Off?"}
    M -->|yes| E1(["409 agent_context_disabled"])
    M -->|no| H
```

#### 3.5.2 Storage and errors

- `DbErrorClassifier` (namespace `QBrainAi.Support.Mcp.Storage.Errors`): `IsUniqueViolation(exception, out indexName)` by provider code (SQL Server 2601 and 2627, PostgreSQL 23505, SQLite 2067 and 1555) and `IsTransientConflict(exception)` (SQL Server 1205, PostgreSQL 40001 and 40P01, SQLite 5 busy and 6 locked). A timeout or transport failure is never a transient conflict (it may have committed; `HandoffDbExceptions.IsCommitAmbiguous`, `src/QBrainAi.Services/Services/HandoffDbExceptions.cs:49`, keeps that meaning). Develop's message-text predicates (`SessionLogService.cs:234`, `:357`, `:812-817`) are context; new code calls only this classifier.
- `SerializableWrite` (namespace `QBrainAi.Support.Mcp.Storage.Transactions`): runs a unit's checks and its save as one delegate in one Serializable transaction on SQLite, PostgreSQL, and SQL Server (the pattern of `SessionLogService.SubmitAsync`, `SessionLogService.cs:153-157`, and `RequirementsRecoveryService.cs:80-126`). Every unit that evaluates the completion gate, the terminal-state guard, a review rule, a response status, or an idempotency key and then saves runs through it, so a concurrent write to the same turn or response commits wholly before or after it. Per-response writes name their responses: the first statements increment each response's `StatusVersion` in ascending `ResponseId` order. Isolation scope (section 3.1, "Turn-level isolation"): a single-turn write reads and writes only its session row and the one turn with its children, so writes to different turns of one session never conflict (`TurnWriteConcurrentDifferentTurnsSameSessionBothCommitNoConflictAnswer`, dense-session fixture, three providers); a multi-turn submission keeps its session-level scope. On the EF in-memory provider, which has no transactions, SerializableWrite runs the delegate once, as develop does (`SessionLogService.cs:155`). A caller-owned transaction is refused as develop refuses it today (`SessionLogService.cs:119-128`). A delegate reads and writes only the database: a unit reads the git commits, files, sources, and GraphRAG results it needs before SerializableWrite, and the delegate re-checks the database state those reads relied on (each locked response's `StatusVersion`, the turn's status); a changed state ends the attempt and the unit runs again from its reads, within the same bound. On SQLite a Serializable transaction holds the database write lock from its start, so no hook write waits on a retrieval or a git read (`BuildOnSqliteHookWriteDuringRetrievalNotBlocked`). A transient conflict, or a unique violation on a key the delegate declares (an idempotency key, a readable key), rolls the transaction back and runs the whole delegate again, at most three times (four attempts) unless the caller states another bound and exhaustion outcome (only the Phase A minter does, ReadableKeyAllocator below); then the unit returns 409 `conflict` with `retryable: true` and saves nothing. The attempts run within the unit's existing storage budget (FR-MCP-SESSIONLIFE-003). A refusal the delegate returns rolls back and is returned unchanged. A command dispatched from inside a delegate (for example DecideAgentResponseTrustCommand from a grade unit) joins the caller's transaction and retry; it never opens a second one. SharedComponents proves the component on a test entity it owns (`SerializableWriteForcedInterleavingTestEntityCommitsWhollyBeforeOrAfter`, `SerializableWriteRetriesExhausted409ConflictRetryableNothingSaved`); the turn cases belong to the gates of the contexts that own those units: `TurnWriteForcedInterleavingReviewResetVsCompleteNeverClosedWithoutReview`, `TurnWriteForcedInterleavingClassChangeVsCompleteGateSeesCommittedClass`, and `TurnWriteForcedInterleavingChoreFlipVsRunningWriteRunningKept`. Every case runs on SQLite, PostgreSQL, and SQL Server.

```mermaid
flowchart TD
    S(["SerializableWrite: delegate, ids to lock"]) --> B["Begin Serializable transaction; bump StatusVersion of each id to lock, ascending"]
    B --> RUN["Run checks and save"]
    RUN -->|refusal| RF(["Rolled back, refusal returned"])
    RUN -->|done| CM{"Commit"}
    CM -->|committed| OK(["Result returned"])
    RUN -->|transient conflict or declared key collision| RB2["Roll back"]
    CM -->|transient conflict| RB2
    RB2 --> N{"Under the retry bound, three unless the caller states one?"}
    N -->|yes| B
    N -->|no| EC(["409 conflict, retryable, or the caller's stated exhaustion outcome"])
```

- `IdempotentCreate` with `PayloadFingerprint` (namespace `QBrainAi.Support.Mcp.Storage.Idempotency`; precedent `TodoPayloadFingerprint`, `src/QBrainAi.Services/Services/TodoPayloadFingerprint.cs:10`): runs inside SerializableWrite. It looks the key up first, before any precondition that can change between attempts (a source, a status, a commit), so a retry of a lost response returns the stored record; a unit that reads outside the database before its SerializableWrite (Build, Submit, the grades) runs the same lookup read-only before those reads, so such a retry returns the stored record without reading any source. The fingerprint is SHA-256 over the canonical JSON of the client-supplied inputs only, never over server-observed sources. The caller states its mismatch policy: `Conflict` (every agent-context create: 409 `conflict` with `retryable: false`), `ReturnStored` (MintSessionTurnCommand: one host event, so a repeated nonce returns the stored turn whatever the payload; RelationshipWriter: identical links), or `MergeInto` (the insert-when-absent turn writes: the write is merged into the stored turn). A unique violation on the key index ends the attempt; SerializableWrite runs the unit again and the next lookup finds the winner, so one rule covers a retry and a concurrent duplicate. Soft delete (TR-MCP-DB-003): `McpDbContext` turns every delete into a soft delete and hides deleted rows through its query filter, while the unique indexes cover deleted rows too (`McpDbContext.cs:391,404,1302-1336`). The lookup therefore reads past the filter: a soft-deleted record with the key is revived (its deletion columns cleared) and then written as a new record with the new inputs and fingerprint (`created: true`), the pattern develop uses for a tombstoned session (`SessionLogService.cs:161-169`, `SessionLogResubmissionReviveTests`), so a deleted link or record can be created again (`IdempotentCreateSoftDeletedKeyRevivedCreatedTrue`).

```mermaid
flowchart TD
    S(["IdempotentCreate: key, PayloadFingerprint of the client inputs, mismatch policy"]) --> F{"Record with this key exists?"}
    F -->|yes| SAME{"Stored fingerprint equals the new one?"}
    SAME -->|yes| ST(["Stored record returned, created false"])
    SAME -->|no| POL{"Mismatch policy?"}
    POL -->|Conflict| E409(["409 conflict, retryable false: key reused with other inputs"])
    POL -->|ReturnStored| ST
    POL -->|MergeInto| MG(["Stored record returned for the caller to merge the write into"])
    F -->|soft-deleted| RV["Revive: clear the deletion columns"]
    RV --> INS
    F -->|no| INS["Caller's remaining checks, then insert or overwrite the revived row"]
    INS -->|inserted| NEW(["New record, created true"])
    INS -->|unique violation on the key index| RT(["Attempt ends: SerializableWrite retries and the next lookup finds the winner"])
```

- `ReadableKeyAllocator` (namespace `QBrainAi.Support.Mcp.Storage.Keys`): allocates the human-readable keys of section 3 (Identifiers). Strategy `NextOrdinal` (`<prefix><parent>-<nn>`, highest stored suffix within the key's unique index scope plus one; the agent-context keys `pkg-`, `rsp-`, and `grd-` under their record parent, `rel-` under its type and timestamp, and `hvr-` under its session and timestamp with `-<nn>` appended only from the second key of the same second, each with the shortened parent id rule and the formats of section 6.3) or `RandomHex` (`req-<yyyyMMddTHHmmssZ>-<8 hex>`, the Phase A minter). `NextOrdinal` reads past the soft-delete filter, so a deleted record's suffix is never allocated again (`ReadableKeyAllocatorSoftDeletedSuffixNeverReused`). A collision on the key's unique index declares a retry to SerializableWrite with the next candidate. The Phase A minter states its own bound and exhaustion outcome to SerializableWrite for request-id collisions only: today's six attempts and 500 `mint_collision_exhausted` (InsertMintedTurnWithRetry, section 4.2); its transient conflicts keep the default bound and exit; agent-context keys use SerializableWrite's default bound (four attempts), then 409 `conflict` with `retryable: true`.
- `PagedQuery` and `PageBounds` (namespace `QBrainAi.Support.Mcp.Storage.Paging`) with the wire type `PageResult<TItem>` (namespace `QBrainAi.Client.Paging`, fields `totalCount`, `limit`, `offset`, `items`): counts the matches and reads one page in the caller's stable order. `PageBounds` carries the policy: `Clamp` (QuerySessionLogsQuery, today's behavior: limit default 100, clamped to 1..1000, a negative offset read as 0, `SessionLogService.cs:476-477`) or `Refuse` (every agent-context query and RetrieveAgentContextCandidatesQuery's `limit`: limit 1..1000, default 50, offset at least 0, otherwise 400 `validation_error` with `details.field` `limit` or `offset`). The four agent-context page types of section 6.4 are `PageResult<TItem>`.

```mermaid
flowchart TD
    S(["PagedQuery: limit, offset, PageBounds, stable order"]) --> P{"PageBounds policy?"}
    P -->|Clamp| C["Default the limit when absent, clamp it to the bounds, read a negative offset as 0"]
    P -->|Refuse| V{"Limit and offset within the bounds?"}
    V -->|no| E(["400 validation_error, details.field limit or offset"])
    V -->|yes| Q["Count the matches, read one page in the stable order"]
    C --> Q
    Q --> R(["PageResult: totalCount, limit, offset, items"])
```

- `ClassifiedErrorException` and `ErrorCodes` (namespace `QBrainAi.Client.Errors`): every new handler fails with `Result<T>.Failure(new ClassifiedErrorException(code, status, message, details, retryable))` (`src/QBrainAi.Cqrs/Result.cs:50`); `ErrorCodes` is the one table of codes with their HTTP status and retryable flag, read by the server, the REPL envelope, and the plugin's receipt mapping (C9). `McpErrorClassifier` (`src/QBrainAi.Support.Mcp/Services/McpErrorClassifier.cs:14`) maps `ClassifiedErrorException` to the `ClassifiedError` problem+json body and the MCP tool error in one branch; it gains no per-feature branch. Retryable codes: transport outage and degraded persistence (503 `backend_unavailable`), a busy store (503 `persistence_error` with `retryable: true`, today's answer, `McpErrorClassifier.cs:129-139`), and 409 `conflict` when `retryable` is true (SerializableWrite exhausted, key allocation exhausted); every other code is final, including the REPL client-policy codes `transaction_degraded` and `mutation_not_transactional` (`ClientMutationPolicy.cs:203-218`), which `ErrorCodes` lists. The REPL classifier carries a server `ClassifiedError` answer through at every status: its `code`, `message`, `retryable`, and `details` reach the REPL envelope unchanged, where today only a 503 passes the server's code through and `conflict` is forced to `retryable: false` (`src/QBrainAi.Repl.Core/ReplMcpErrorClassifier.cs:60-95`), and any other answer becomes a classifier code or `method_invocation_error` (`src/QBrainAi.Repl.Core/ReplCommandDispatcher.cs:3346-3370`); so the plugin sees the completion-gate token, the review and agent-context codes, and the server's retryable flag (`ReplClassifierConflictRetryableTrueKept`, `ReplClassifierServerClassifiedErrorKeepsCodeRetryableDetailsEveryStatus`). Exceptions without a server body keep today's classification.
- `SecretRedactor` (namespace `QBrainAi.Support.Mcp.Redaction`): `Redact(text)` and `ContainsSecret(text)` over the rules of `SessionLogSanitizer` and `ApiKeyRedaction` (`src/QBrainAi.Services/Services/SessionLogSanitizer.cs:15`, `src/QBrainAi.Common.AgentCli/ApiKeyRedaction.cs:9`). It is the one redactor behind the session-log sanitizer's review paths and the agent-context secret filter and secret scan (section 3, Secrets).
- `ContentHasher` (namespace `QBrainAi.Support.Mcp.Hashing`): SHA-256 over UTF-8 bytes, lower-case hex, of the text's stored normal form; every hash this plan names is computed through it. `McpDbContext` rewrites every string it saves through `LineSanitizer.Sanitize`, which replaces U+2014 and U+2013 with an ASCII hyphen (`McpDbContext.cs:1351-1360,1704-1718`, `src/QBrainAi.Common.AgentCli/LineSanitizer.cs:23-28`); ContentHasher applies the same function before hashing, and every byte-for-byte comparison of this plan (Replay identity, `verdict_binding`, `reviewer_turn`) compares normal forms, so a value read back from the store hashes and compares as it was recorded (`ContentHasherTextWithEmAndEnDashesStoredAndReadBackHashEqual`).

#### 3.5.3 Session log (namespace `QBrainAi.Support.Mcp.SessionLog`)

- `TurnStatus`: `IsTerminal(status)` is true for `completed`, `failed`, `closed`, `canceled`, and `cancelled` (develop's set, `SessionLogService.cs:1355-1361`); every "terminal" and "nonterminal" in this document means this predicate. Agent surfaces (REST, MCP, client, REPL, plugin) accept the statuses `in_progress`, `completed`, `failed`, and `canceled` on writes (the legacy spelling `cancelled` stays accepted, as FR-MCP-SESSIONLIFE-002 relies on); a write naming `closed` is refused with 400 `validation_error` (operator, 2026-10-12, answer "Refuse from agent surfaces (Recommended)"); server-internal writers and existing rows keep `closed`.
- `TurnLocator`: validates the session id and, when the unit addresses a turn, the request id (`SessionLogIdentifierValidator`, `src/QBrainAi.Services/Services/SessionLogIdentifierValidator.cs:8`), finds the session by (workspace, agent, sessionId) and the turn by (session, requestId). It returns typed results; each surface maps them to its existing body, listed in the unit's error list. Every unit that addresses a session or a turn, including the review units and the agent-context units that name a turn, calls it with the same key; an agent-context request that names a turn carries `agent`, `sessionId`, and `requestId`.

```mermaid
flowchart TD
    S(["TurnLocator: agent, sessionId, requestId"]) --> V{"Ids valid?"}
    V -->|no| E1(["InvalidIdentifier"])
    V -->|yes| SE{"Session found?"}
    SE -->|no| E2(["SessionNotFound"])
    SE -->|yes| RQ{"Turn addressed?"}
    RQ -->|no| OKS(["Session"])
    RQ -->|yes| T{"Turn found?"}
    T -->|no| E3(["TurnNotFound"])
    T -->|yes| OKT(["Session and turn"])
```

- `TurnWritePipeline`: the one write path of every turn-writing unit (SubmitSessionLogCommand per turn, UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, CompleteSessionTurnCommand, FailSessionTurnCommand), run inside SerializableWrite. Modes: `Submit` (policy TerminalTurnOutcomeGuard first; the completion gate's missing items are collected and the submission is rejected once after every turn is evaluated), `Merge` (additive; insert when absent; Upsert, Patch), `Replace` (whole replacement; the turn must exist), and the status overrides `Complete` and `Fail` (Merge with the status set). The single-turn modes reject at the first refusal. The insert-when-absent race is IdempotentCreate with policy `MergeInto` (one retry mechanism, no separate race step). Review records carried in the turn body pass policy ReviewValidation (section 5.3) before RequestClassification. The completion gate runs when the write moves the turn into `completed` or `failed`, or when a Replace carries `completed` or `failed` (FR-MCP-SESSIONCLASS-002-AC004), never on any other write that only enriches an already terminal turn. The QBAgent terminal compliance check (`ValidateTerminalTurnCompliance`, `SessionLogService.cs:1329`) runs in the single-turn modes only, as today. Single-turn modes read only their session row and their turn (turn-level isolation); the session summary refresh runs after the turn commits, as its own short write whose failure never fails the turn write. Outputs: the turn id (single-turn modes) or the per-turn outcome (`Submit`). Typed failures, mapped by each surface to the bodies its unit lists: `InvalidIdentifier`, `SessionNotFound` (never in `Submit` mode, which opens or upserts its session first), `TurnNotFound`, `InvalidPayload`, `ReviewInvalid` (with the policy ReviewValidation code), `RequestClassInvalid`, `CompletionGateMissing` (with the missing items), and the SerializableWrite conflict.

```mermaid
flowchart TD
    S(["TurnWritePipeline: mode, entry, origin"]) --> LOC[["TurnLocator"]]
    LOC -->|invalid id or no session| F1(["Typed failure"])
    LOC -->|session found| GD[["Policy TerminalTurnOutcomeGuard, Submit mode only"]]
    GD -->|skipped_terminal or no_change| OUT(["Outcome only, no write"])
    GD -->|other modes, created, or merged| CTX{"Context valid and attributed?"}
    CTX -->|no| F2(["InvalidPayload"])
    CTX -->|yes| EXT{"Turn exists?"}
    EXT -->|no, Replace| F3(["TurnNotFound"])
    EXT -->|no, other modes| INS[["IdempotentCreate MergeInto: insert with ReviewDefaults"]]
    EXT -->|yes| APP["Merge, or replace; Complete and Fail set status"]
    INS -->|stored turn| APP
    INS -->|inserted| RV[["Policy ReviewValidation, when the body carries reviews"]]
    APP --> RV
    RV -->|invalid| F7(["ReviewInvalid"])
    RV -->|valid or none| PC[["Policy RequestClassification, when requestClass is supplied"]]
    PC -->|invalid| F4(["RequestClassInvalid"])
    PC -->|valid or not supplied| GT[["Policy CompletionGate, when the write moves into completed or failed, or a Replace carries one"]]
    GT -->|missing items| F5(["CompletionGateMissing"])
    GT -->|met or not applicable| QB{"Single-turn QBAgent close without evidence?"}
    QB -->|yes| F6(["InvalidPayload"])
    QB -->|no| OK(["Children stamped, summary refreshed: turn id or per-turn outcome"])
```

- `TurnReadProjection`: the read shape of a turn on every read surface: review text through `SecretRedactor`, scores as their exact decimal lexemes (`ScoreLexeme`), unset review fields `null` (C11). Turn text is stored as written and redacted on every read surface (FR-MCP-SESSIONLOGSAN-001); its review-text redaction and the TR-MCP-SESSIONLOGSAN-001 decorator's review paths are the same `SecretRedactor` call, and search runs on the stored text (FR-MCP-SESSIONLOGSAN-001 ac4). Used by GetSessionLogQuery, QuerySessionLogsQuery, and the record RecordSessionTurnReviewCommand and ResetSessionTurnReviewCommand return.

#### 3.5.4 Scoring and reviews

- `ScoreLexeme` and `ScoreLexemeJsonConverter` (namespace `QBrainAi.Client.Scoring`; the rule of section 3.4, Scores): validates a score lexeme (pattern, at most 28 fractional digits, value in [0, 1]) and parses it exactly; errors 400 `review_score_format`, `review_score_precision`, `review_score_out_of_range`. The converter validates at deserialization, so an invalid score is refused before any lookup (an invalid score on an unknown turn is 400, not 404). The REPL's lossless normalizer calls `ScoreLexeme` for the review keys (`agentReview`, `hostileReview`, `recordReview`) and for the agent-context grade keys (`correctness` and `completeness` of `RecordModelGradeAsync`), so `verdict_match` (section 6.3) compares lexemes that no hop has re-serialized. The PowerShell core's copy (`Protect-ReplLosslessScalars`) is a language boundary, kept in step by the golden contract test of the review contract.
- `ScoreVerdict` (namespace `QBrainAi.Support.Mcp.Scoring`): derives `Pass` or `Fail` from two scores against their thresholds, inclusive (score >= threshold), and returns the thresholds in force for stamping. Parameters: the thresholds and an optional floor and count rule. Phase B reviews use the configured thresholds in [0, 1] and refuse a supplied `Pass` or `Fail` that contradicts the derivation with 422 `review_status_conflict`; agent-context grades and the trust decision apply `max(configured, 0.98)` and require zero FAIL and zero UNKNOWN findings. Used by policy ReviewValidation (RecordSessionTurnReviewCommand and review records in turn bodies), policy HvVerdictMapping, RunDeterministicGradeCommand, RecordModelGradeCommand, and DecideAgentResponseTrustCommand (which re-derives from the stored scores with the floor).
- `ReviewDefaults` (namespace `QBrainAi.Support.Mcp.Reviews`): the default review status for a kind and class: `agentReview` `Unstarted`; `hostileReview` `Unnecessary` when the class is `Chore`, otherwise `Unstarted`. Applied at turn insert, by a section reset, by the Chore flip of policy RequestClassification (only a record still at a default status, only on a nonterminal turn), and by the S3 backfill.

#### 3.5.5 Agent context

- `SourceResolver` and `SourceKind` (namespace `QBrainAi.Support.Mcp.AgentContext.Sources`, owner ContextPackages, called by ResponsesAndTrust and ContextGraph through its Phase 2 seam): resolves a source reference by kind and returns its content and its hash per the canonical forms of section 6.3 (through `ContentHasher`); `ReadCommit()` returns the workspace's HEAD commit, or fails with 409 `agent_context_commit_unavailable` (Build and Submit alike). Files are read at a commit, never from the working copy. A reference that names no existing source is 400 `validation_error` with `details.field` naming the reference, in every unit. `SourceKind` is the one vocabulary (`Request`, `Profile`, `Rule`, `Requirement`, `Todo`, `SessionLogTurn`, `CodeFile`, `PriorResponse`, `Package`, `Receipt`, `Commit`); the wire values `Code` (package items) and `File` (evidence) are aliases of `CodeFile`, and `Response` (relationship nodes) of `PriorResponse`, mapped here only.
- `PackageAssembler`, `CandidateRanker`, and `BuiltPackageLookup` (namespace `QBrainAi.Support.Mcp.AgentContext.Packages`, owner ContextPackages): `CandidateRanker` scores candidates by the rule of section 6.3, rounds half-even to six places, and orders by score descending then `sourceRef`; RetrieveAgentContextCandidatesQuery's relational path uses it. `PackageAssembler` turns a candidate snapshot into a package: secret filter (`SecretRedactor`), omission filter, findings, the mandatory-context check, the budget fill (FillBudgetWithCandidates), and rendering; BuildAgentContextPackageCommand runs it on live candidates and ReplayAgentContextPackageQuery on the stored input snapshot, which therefore records every input the assembler reads (section 6.3). `BuiltPackageLookup` finds a package in the workspace that is not `Overflow`: 404 `not_found` (`details.resource` `package`) or 409 `agent_context_package_overflow`.
- `ResponseStatusTransition` (namespace `QBrainAi.Support.Mcp.AgentContext.Responses`, owner ResponsesAndTrust): changes one response's status inside SerializableWrite with the response, and for a supersede the replacement too, locked in ascending `ResponseId` order. It checks the lifecycle of section 6.3 (a transition already applied with the same target and replacement returns unchanged; a transition the lifecycle does not allow is 409 `agent_context_transition_invalid`), sets `Status`, `StatusChangedUtc`, and `TrustedUtc` for `Trusted`, and applies every side effect in the same transaction: the projection row (entering `Trusted`: `Pending` for ingest; leaving `Trusted`: `Pending` for removal when the row holds a `GraphRagDocumentId`, otherwise `Removed`), and for `Rejected` or `Superseded` the response's `Pending` items in `Open` HV requests become `Closed` with that reason, and each request left with no `Pending` item becomes `Completed` with `CompletedUtc`.

```mermaid
flowchart TD
    S(["ResponseStatusTransition: target, reason, replacement"]) --> SAME{"Already applied?"}
    SAME -->|yes| UN(["Unchanged, 200"])
    SAME -->|no| C{"Lifecycle allows it?"}
    C -->|no| E(["409 agent_context_transition_invalid"])
    C -->|yes| ST["Set status and timestamps"]
    ST --> PJ["Mark the projection row"]
    PJ --> HV{"Rejected or Superseded?"}
    HV -->|yes| CL["Close Pending HV items, complete emptied requests"]
    HV -->|no| OK(["Saved with the caller"])
    CL --> OK
```

- `IResponseStalenessRule` (namespace `QBrainAi.Support.Mcp.AgentContext.Responses`, owner ResponsesAndTrust, called read-only by ContextPackages and ContextGraph): evaluates one `Submitted`, `Grading`, or `Trusted` response and returns fresh, or stale with the first changed reference and its relationship. Dependencies are the `depends_on`, `derived_from`, and `supports` targets of the response and of its package, the response's `File`, `Requirement`, `Receipt`, and `SessionLogTurn` evidence (including evidence outside its package), and every cited requirement (stale when deleted or superseded), except the request's own turn (never a dependency or evidence, section 6.3) and Rule files outside version control (provenance only, never a dependency, section 6.3). A file dependency is hashed at the workspace's current HEAD commit. Queries and Build report the effective status and write nothing; only EvaluateStalenessCommand persists `Stale`, through ResponseStatusTransition, after it evaluates outside its SerializableWrite. A unit that needs the persisted status (the grade units, and a unit of another context) dispatches EvaluateStalenessCommand through `IDispatcher` before its own SerializableWrite.

```mermaid
flowchart TD
    S(["IResponseStalenessRule.Evaluate: one response"]) --> ST{"Status Submitted, Grading, or Trusted?"}
    ST -->|no| N(["Not evaluated"])
    ST -->|yes| D["Collect depends_on, derived_from, and supports targets, evidence, and cited requirements, except the own turn and untracked Rule files"]
    D --> H{"Every dependency hashes as recorded, files read at the workspace HEAD commit?"}
    H -->|yes| F(["Fresh"])
    H -->|no, or a cited requirement deleted or superseded| C(["Stale: the first changed reference and its relationship"])
```

- `ResponseIdList` (namespace `QBrainAi.Support.Mcp.AgentContext.Responses`): validates a list of response ids (at most 1000, each with the `rsp-` prefix; otherwise 400 `validation_error`) and selects them; ids that name no response in the workspace are returned in the result's `notFound` list (EvaluateStalenessCommand, ProjectAgentResponsesCommand).
- `RelationshipWriter` (namespace `QBrainAi.Support.Mcp.AgentContext.Graph`, owner ContextGraph, called by ContextPackages and ResponsesAndTrust inside their own transactions): writes a typed link with its target's hash (`ToSha256`, which the caller read through SourceResolver before its SerializableWrite, so the delegate stays database-only) and `CreatedBy`. A `supersedes` link is written only by SupersedeAgentResponseCommand; a client request for that type is 400 `validation_error` with `details.field` `type`, so one data source holds the supersede chain. Identical links return the stored link (`created: false`, IdempotentCreate with policy `ReturnStored`). Every link created by a command (`CreatedBy` names the command, for example Build's `derived_from` and `relevant_to`, Submit's `derived_from` and `supports`, Supersede's `supersedes`) is protected: their deletion is 409 `agent_context_relationship_protected`.

```mermaid
flowchart TD
    S(["RelationshipWriter in the caller's transaction: link, createdBy"]) --> T{"Type supersedes and the caller is not SupersedeAgentResponseCommand?"}
    T -->|yes| E1(["400 validation_error, details.field type"])
    T -->|no| CY{"A supersedes link that would close a cycle?"}
    CY -->|yes| E2(["409 agent_context_supersede_invalid"])
    CY -->|no| IC[["IdempotentCreate keyed by the link's six fields, policy ReturnStored: insert with the caller's ToSha256 and CreatedBy"]]
    IC -->|stored link| SAME(["created false"])
    IC -->|new link| OK(["created true"])
```

#### 3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell)

The processes BindVerbToTurn and PersistTurnWriteWithReceipt (section 4.4) are the shared verb pipeline: every verb sequence shows them as `ref` blocks, and every process that sends a turn write (the failsafe drain, clear-session, the stateless invocation's `canceled` close) maps its result through PersistTurnWriteWithReceipt. The functions below are named for what they do. Each lives in `plugins/core/lib-ps/repl-invoke.ps1`, the core's dot-sourceable library (its guard, `repl-invoke.ps1:3137`), so the verbs, the failsafe drain, and the arrival bridge reach them in-process; `plugin-hook.ps1` cannot be dot-sourced (mandatory `HookName` and a top-level `switch`, `plugin-hook.ps1:3-6,1521-1534`), so it dot-sources `repl-invoke.ps1` to call them.

- `Resolve-MintOutcome`: classifies one mint result for the prompt hook, the arrival bridge, and the failsafe drain alike.

```mermaid
flowchart TD
    S(["Resolve-MintOutcome: mint result, new invocation or stored retry"]) --> R{"Result?"}
    R -->|201 created| C(["created"])
    R -->|200 merged, stored status in_progress| M{"New invocation on copilot or classic cline?"}
    M -->|yes| U(["unsupported: indistinguishable_arrival, by Set-SessionUnsupported"])
    M -->|no| MI(["merged in progress: same requestId, turn-already-open"])
    R -->|200 merged, stored status terminal| T(["merged terminal: report storedStatus; never the write target, the open list, or the ledger"])
    R -->|transport outage, a retryable 503 or 409, or the scoped mint 404 or unknown_method of C9| O(["outage: pending id pending-nonce, failsafe mint record kept for the drain"])
    R -->|ingress_nonce_invalid, validation_error, or mint_collision_exhausted| F(["final: turn-open-failed with the code; a drained record counts an attempt"])
    R -->|session_not_open| SO{"Session open confirmed for this envelope?"}
    SO -->|yes| F
    SO -->|no| O
```

- `Invoke-SessionOpen`: the idempotent session open for an envelope's `mcpSessionId`, used by StartHostSession, the first-ingress writer, and the failsafe drain. A recorded `mcpSessionId` for a host session key is never replaced: a later SessionStart, a post-compact start, or a resumed session key keeps it (FR-MCP-SESSIONLIFE-002).

```mermaid
flowchart TD
    S(["Invoke-SessionOpen: envelope mcpSessionId"]) --> R{"Open result?"}
    R -->|200 created true or false| REC[["Update-TurnState: record mcpSessionId only when none is recorded for the host session key"]]
    REC --> OK(["confirmed"])
    R -->|transport outage| O(["outage: records kept for the next drain"])
    R -->|400 invalid session identifier| F(["final: invalid session identifier"])
```

- `Set-SessionUnsupported`: the D6 fail-closed rule for every check that can fail a host session (the bridge's format and removal checks, the prompt hook's unsupported branch, the copilot and classic-cline single-registration and equal-field checks).

```mermaid
flowchart TD
    S(["Set-SessionUnsupported: reason, causeOffset or causeNonce"]) --> D{"A request already delivered in this host session?"}
    D -->|no| N(["turn_logging_unsupported reported; nothing recorded"])
    D -->|yes| R[["Update-TurnState: record unsupportedSince with the reason and causeOffset, causeNonce, or detectedAtUtc"]]
    R --> U(["Host session failed closed: open turns follow the unsupported-session failsafe decision"])
```

- `Update-TurnState`: the one writer of `sessions/<hostSessionKey>/turn-state.yaml` for every writer section 3.4 lists: one atomic replace under `turn-open.lock`, released on every path. A function that runs inside a change already holding the lock (for example `Set-SessionUnsupported` or `Invoke-SessionOpen` called by a writer that holds it) records into that same replace and takes no second lock.

```mermaid
flowchart TD
    S(["Update-TurnState: change to apply"]) --> L{"turn-open.lock acquired?"}
    L -->|no| C(["contended: returned to the caller; the prompt hook reports turn-open-contended and writes its failsafe diagnostic"])
    L -->|yes| RD["Read turn-state.yaml and apply the change"]
    RD --> W{"Temp file written, flushed, and renamed over turn-state.yaml?"}
    W -->|yes| REL["Release turn-open.lock"]
    REL --> OK(["State replaced"])
    W -->|no| REL2["Release turn-open.lock"]
    REL2 --> E(["Error returned, file unchanged"])
```

- `Test-ModelEvidence` and `Get-CloseRemedy`: model evidence is at least one model-authored action, designDecision, or commit; it is required before a `completed` or `failed` close and never for a `canceled` close; records captured in the failsafe for the turn count. `Get-CloseRemedy` takes the turn and the chosen close (`completed`, `failed`, or `canceled`) and returns the remedy of section 3.4 (Stop gate) for that turn in its fixed order; the Stop gate, clear-session, and the advisory completion precheck call it.

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
- `PriorResponse` (`Response`): `ContentSha256`. `Package`: `ContentSha256`.
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
5. `verdict_match`: the submitted `correctness`, `completeness`, and `findings` equal that entry exactly (the same score lexemes, compared as ScoreLexeme values that no hop re-serializes, section 3.5.4; the same claimIds, checks, and outcomes in the same order), and the entry's findings name every claim of the response at least once (an empty or partial finding list is refused).
6. `reviewer_turn`: TurnLocator (section 3.5.3) finds the turn with agent `ModelGrader:AgentName`, `reviewerSessionId`, and `reviewerRequestId` in the workspace, the turn was created at or after the HV request's `CreatedUtc`, and its stored result contains the same verdict block byte for byte, both compared in their stored normal form (the receipt's block passes through ContentHasher's normalization, since the store saved the turn in that form, `ModelGradeVerdictWithEmDashReviewerTurnMatchesInNormalForm`), where the stored result is the turn's response followed by its processingDialog parts in part order with each part's header line removed (the HV protocol stores the entire result on the reviewer's own turn and splits a long result into ordered parts; `ModelGradeVerdictSplitAcrossDialogPartsRejoinedAndAccepted`).
7. `reviewer_independent`: the reviewer's MCP session is neither the session the HV request reviews nor the session that submitted the response; independence is a separate review session and execution, not a different agent name, so an independently launched Codex reviewer grades a Codex-authored response (`ModelGradeCodexResponseIndependentCodexReviewerSessionAccepted`).
8. `runtime_identity`: the rollout receipt at `rolloutJsonlPath` resolves under `ModelGrader:ReceiptRoot`; its `session_meta` record's `id` equals `reviewerThreadId` and the `thread_id` of the response jsonl's `thread.started` event; every `turn_context` record in it has `model` equal to `ModelGrader:RequiredModel` and `effort` equal to `ModelGrader:RequiredEffort`; and the verdict JSON names the same `reviewerThreadId`. The declared `request_meta` alone never satisfies this check (`ModelGradeLaunchMetaCorrectRolloutTurnContextOtherModelOrEffortRefusedRuntimeIdentity`, `ModelGradeRolloutThreadDiffersFromResponseStreamRefusedRuntimeIdentity`).

The hashes of the three jsonl receipts (request, response, rollout) and of the verdict block are stored on the grade. The remaining trust assumption is the workspace's session-log identity model (every agent of a workspace authenticates with the workspace key), which this plan does not change: a forged grade would need a forged reviewer session, a forged verdict, a forged rollout, and matching receipts, and each is checked against the others. Negative cases: `ModelGradeReceiptOutsideReceiptRootRefusedReceiptPath`, `ModelGradeRequestMetaOtherAgentModelOrEffortRefusedRequestMeta`, `ModelGradeDisagreeVerdictPassingScoresRefusedVerdictMatch`, `ModelGradeEmptyOrPartialFindingsRefusedVerdictMatch`, `ModelGradeVerdictForOtherResponseOrContentRefusedVerdictBinding`, `ModelGradeNoCompletedTurnOrTwoVerdictsRefusedVerdictBlock`, `ModelGradeReviewerTurnMissingRefusedReviewerTurn`, `ModelGradeReviewerTurnVerdictDiffersRefusedReviewerTurn`, `ModelGradeReviewerTurnBeforeHvRequestRefusedReviewerTurn`, `ModelGradeReviewerSameSessionAsProducingOrReviewedRefusedReviewerIndependent`, `ModelGradeLaunchMetaCorrectRolloutTurnContextOtherModelOrEffortRefusedRuntimeIdentity`, `ModelGradeRolloutThreadDiffersFromResponseStreamRefusedRuntimeIdentity`; acceptance case `ModelGradeCodexResponseIndependentCodexReviewerSessionAccepted`.

`AgentContextProjectionEntity` (table `AgentContextProjections`, key (`WorkspaceId`, `ResponseId`), one row per response through that composite foreign key)

- `WorkspaceId`, `ResponseId`, and the response's root ids `PackageId` (128), `Agent` (64), `SessionId` (256), `RequestId` (256).
- `State` (16): `Pending`, `Projected`, `Removed`, `Skipped`, `Failed`, changed only by ResponseStatusTransition (section 3.5.5) and by a ProjectOneResponse save, as the projection row state diagram below draws. The row is created `Pending` when its response first enters `Trusted`.
- `GraphRagDocumentId` (128): the document while one exists in GraphRAG (kept on a `Failed` row after a delete error and on a `Pending` row awaiting removal), `ProjectedContentSha256` (64), `LastError` (2048), `UpdatedUtc`.
- Selection: a projection pass without `responseIds` selects `Pending` and `Failed` rows, and `Skipped` rows while ingest is enabled again; named `responseIds` select their rows in any state.
- Lifecycle rules: for a response that is not `Trusted`, a row that holds a `GraphRagDocumentId` has its document deleted on the next pass while GraphRAG is enabled, whatever `ProjectionEnabled` says (`ProjectionDeleteFailedRetriedNextPassDocumentRemoved`, `ProjectionDisabledAfterProjectedRejectedResponseDocumentStillRemoved`); a selected row whose response is not `Trusted` and that holds no document becomes `Removed` and is not selected again (`ProjectionPendingThenRejectedBeforeIngestRowRemovedNotReselected`); `ProjectionEnabled` false skips ingest only (`Skipped`, `skippedReason` `projection_disabled`); while GraphRAG is disabled an ingest is `Skipped` (`skippedReason` `graphrag_disabled`) and a row awaiting removal stays `Pending` until GraphRAG is enabled (`ProjectionGraphRagDisabledRemovalStaysPendingRunsWhenEnabled`). ProjectOneResponse makes its GraphRAG call before SerializableWrite (database-only delegate), then saves the row through SerializableWrite with its response locked and re-reads the response status inside the delegate, so a reject that commits during an ingest leaves the row `Pending` with the new document id, removed on the next pass (`ProjectionForcedInterleavingRejectDuringIngestDocumentRemovedNextPass`, on the three providers); the delegate also re-reads the row, and a save that finds it already `Projected` by an overlapping pass for the same content keeps that document and deletes its own after the save (`ProjectionOverlappingPassesOneDocumentKeptOtherDeleted`).
- Workspace scope: projection ingests only into `GraphRagStorageScope.Workspace`, the GraphRAG retrieval path queries only that scope, and a hit naming a response outside the workspace is dropped, so a projected document never reaches the global GraphRAG scope or another workspace (`ProjectionAndGraphRagRetrievalWorkspaceScopedNoCrossWorkspaceHit`).

Entity relationships, one diagram per functional group, in the groups of Addendum A (A.5 to A.10, which lists every column). A solid line is a composite foreign key (`WorkspaceId`, parent key); a dotted line is a logical reference by root ids or by a node reference, enforced by the units, not by a foreign key. Each relationship is drawn once; an entity drawn in a group other than its own is a relationship target.

Packages and their build inputs:

```mermaid
erDiagram
    SessionLogTurns ||..o{ AgentContextPackages : "built for, by turn key"
    AgentContextPackages ||--o{ AgentContextPackageItems : contains
    AgentContextPackages ||--o{ AgentContextPackageFocusRefs : focuses
    AgentContextPackages ||--o{ AgentContextPackageInputs : snapshots
    AgentContextPackages ||--o{ AgentContextPackageInputLinks : "snapshots links"
    AgentContextRelationships ||..o{ AgentContextPackageInputLinks : "read at build, by RelationshipId"
```

Package omissions and findings:

```mermaid
erDiagram
    AgentContextPackages ||--o{ AgentContextOmissions : records
    AgentContextPackages ||--o{ AgentContextFindings : records
    AgentContextRelationships ||..o{ AgentContextFindings : "by RelationshipId"
```

Responses and evidence:

```mermaid
erDiagram
    SessionLogTurns ||..o{ AgentResponses : "answers, by turn key"
    AgentContextPackages ||--o{ AgentResponses : "answered by"
    AgentResponses ||--o{ AgentResponseClaims : asserts
    AgentResponses ||--o{ AgentResponseEvidence : cites
```

Grades:

```mermaid
erDiagram
    AgentResponses ||--o{ AgentResponseGrades : "graded by"
    AgentContextHvRequests |o--o{ AgentResponseGrades : "model grades"
    SessionLogTurns ||..o{ AgentResponseGrades : "reviewer turn"
    AgentResponseGrades ||--o{ AgentResponseGradeFindings : lists
```

HV requests:

```mermaid
erDiagram
    SessionLogs ||..o{ AgentContextHvRequests : "turn range, by Agent and SessionId"
    AgentContextHvRequests ||--o{ AgentContextHvRequestItems : lists
    AgentResponses ||--o{ AgentContextHvRequestItems : "reviewed in"
```

Relationships and the GraphRAG projection:

```mermaid
erDiagram
    AgentContextRelationships }o..o{ AgentContextPackages : "From or To kind Package"
    AgentContextRelationships }o..o{ AgentResponses : "From or To kind Response"
    AgentResponses ||--o| AgentContextProjections : "projected as"
```

Response lifecycle (the transitions ResponseStatusTransition allows, section 3.5.5):

```mermaid
stateDiagram-v2
    [*] --> Submitted: SubmitAgentResponseCommand
    Submitted --> Grading: first grade recorded, trust decision
    Grading --> Grading: grade recorded, latest pair not both Pass
    Grading --> Trusted: latest deterministic and latest model grade both Pass at the floor
    Trusted --> Grading: newer grade or re-derived verdict Fails
    Submitted --> Stale: EvaluateStalenessCommand, a dependency changed
    Grading --> Stale: EvaluateStalenessCommand, a dependency changed
    Trusted --> Stale: EvaluateStalenessCommand, a dependency changed
    Stale --> Stale: grade recorded, status kept
    Submitted --> Rejected: RejectAgentResponseCommand
    Grading --> Rejected: RejectAgentResponseCommand
    Trusted --> Rejected: RejectAgentResponseCommand
    Stale --> Rejected: RejectAgentResponseCommand
    Submitted --> Superseded: SupersedeAgentResponseCommand
    Grading --> Superseded: SupersedeAgentResponseCommand
    Trusted --> Superseded: SupersedeAgentResponseCommand
    Stale --> Superseded: SupersedeAgentResponseCommand
    Rejected --> [*]
    Superseded --> [*]
```

ResponseStatusTransition applies only the transitions drawn, inside SerializableWrite with the response (and for a supersede the replacement) locked in ascending `ResponseId` order; the trust decision moves a response between `Submitted`, `Grading`, and `Trusted` and keeps a `Stale` response `Stale`. A repeat of a transition already applied (a Reject of a `Rejected` response, a Supersede naming the same replacement) returns the response unchanged with 200. Every other requested transition is 409 `agent_context_transition_invalid`: a Reject of a `Superseded` response, a Supersede of a `Rejected` original or of an original superseded by another response, and any transition out of `Rejected` or `Superseded`. Supersede validation that is not a lifecycle question (a replacement for another request, a replacement that is `Rejected` or `Superseded`, a cycle) is 409 `agent_context_supersede_invalid`, and a grade for a `Rejected` or `Superseded` response is 409 `agent_context_response_not_gradable` (grade units only). Each transition sets `Status` and `StatusChangedUtc` (and `TrustedUtc` on entering `Trusted`) and applies its projection row and HV item effects in the same transaction.

Projection row states (`AgentContextProjectionEntity.State`; the choice node `ProjectOneResponseSave` is one ProjectOneResponse save):

```mermaid
stateDiagram-v2
    state ProjectOneResponseSave <<choice>>
    [*] --> Pending: response first enters Trusted
    Pending --> ProjectOneResponseSave: projection pass selects the row
    Failed --> ProjectOneResponseSave: projection pass selects the row
    Skipped --> ProjectOneResponseSave: selected while ingest is enabled, or named in responseIds
    ProjectOneResponseSave --> Projected: response Trusted, ingest enabled, document ingested, status still Trusted on re-read
    ProjectOneResponseSave --> Skipped: response Trusted, ProjectionEnabled false or GraphRAG disabled
    ProjectOneResponseSave --> Removed: response not Trusted, GraphRAG enabled, held document deleted or none held
    ProjectOneResponseSave --> Pending: response not Trusted while GraphRAG disabled, or left Trusted during the ingest with the new document id kept
    ProjectOneResponseSave --> Failed: GraphRAG ingest or delete error, LastError set
    Projected --> Pending: response leaves Trusted, document awaiting removal
    Pending --> Removed: response leaves Trusted, no document held
    Skipped --> Removed: response leaves Trusted, no document held
    Failed --> Removed: response leaves Trusted, no document held
    Failed --> Pending: response enters Trusted, or leaves it while a document is held
    Removed --> Pending: response enters Trusted again
```

The transitions out of `Pending`, `Projected`, `Skipped`, `Failed`, and `Removed` that name the response entering or leaving `Trusted` are ResponseStatusTransition's projection effect (section 3.5.5): entering `Trusted` marks the row `Pending` for ingest; leaving `Trusted` marks it `Pending` for removal when it holds a `GraphRagDocumentId`, otherwise `Removed`. Every transition out of `ProjectOneResponseSave` is that save, through SerializableWrite with the response locked.

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
- QueryAgentContextPackagesQuery `AgentContextPackageQuery { requestId, sessionId, status, limit, offset }`; result `PageResult<AgentContextPackageResult>` of package summaries (`PageBounds` `Refuse`).
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
- RunStatelessInvocation stateless invocation: the plugin process of section 6.6, not a unit. It reads Mode with GetAgentContextStatusQuery at host session start (or at the first prompt on a host without a SessionStart event), caches it as `agentContextMode` in the host session's `turn-state.yaml`, and reads that cache at each prompt; a read that fails, times out, or misses the SessionStart hook bound, or an unknown value, is `Off` (section 6.2; the query's counts are effective and each distinct dependency is read once, GetAgentContextStatusQuery above). It builds with BuildAgentContextPackageCommand, and keeps the request's `packageId` in its per-turn file `turns/<requestId>.yaml` (the session-log turn gains no `packageId` field); the model submits its response with the `agent_context_submit_response` tool (SubmitAgentResponseCommand) before it closes the turn, and the plugin supplies that `packageId` (found again with QueryAgentContextPackagesQuery `{ requestId, status: Built }` when a hook process boundary lost it). In `Stateless` the model's input is the rendered package, the current request, and the host's deterministic `REQUIRED MEMORIES` block (FR-MCP-MEMORY-003, -007), with history excluded; the sentinel case `StatelessHostSentinelPriorTurnAbsentFromModelRequest` (section 6.6) asserts both that a prior turn is absent and that the memory block is present in the model request. On a build failure other than 409 `agent_context_disabled` (which caches `Off`, section 6.2), including a REPL refusal `transaction_degraded` or `mutation_not_transactional` (section 3.1, "Transaction-gated REPL clients"; `StatelessInvocationBuildTransactionDegradedTreatedAsBuildError`, section 6.6), the invocation fails: no answer is produced, the prompt is refused with the error, and the plugin (not the model) writes the `canceled` close, its `failureNote` starting with the token `agent_context_invocation_failed` followed by the failure code (the token the pilot report's `refusedPrompts` counts), on the persist path of process PersistTurnWriteWithReceipt, where `Test-ModelEvidence` never applies to a `canceled` close (updates FR-MCP-TRIAGESTORE-001 `ac-2`, under which hooks never produce `canceled` turns); an outage (any retryable answer, a 409 `conflict` with `retryable: true` included, section 3.1, "Retryable answers") holds that close in the failsafe, and the Stop gate warns degraded persistence naming the package failure (it never blocks) and keeps the ledger entry until the drain confirms the close.
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

Every REST route below exists under `/qbrainai/agent-context` and the alias `/mcpserver/agent-context` (controller `AgentContextController`, `[ApiController]`, authenticated by `WorkspaceAuthMiddleware`; `WorkspaceResolutionMiddleware` resolves the workspace from the `X-Workspace-Path` header or, without one, from the API key, and answers an unknown header with 400 and an unresolved workspace with 404 in its own `{error}` bodies before any controller, `src/QBrainAi.Support.Mcp/Middleware/WorkspaceResolutionMiddleware.cs:97-139,168-177`, section 3.5.1). MCP tools live in `FwhMcpTools.AgentContext.cs` with `workspacePath` as the first parameter and JSON-string bodies. The typed client is `AgentContextClient : McpClientBase`, exposed as `QBrainAiClient.AgentContext`. The REPL reaches it as `client.AgentContext.<Method>` (added to the `GenericClientPassthrough` allow-list as `AGENTCONTEXT`; classified in `ClientMutationPolicy` as stated below; YAML validators registered with explicit `payload.params.<key>` paths). Typed client signatures, which the REPL binds by parameter name, case-insensitively (`GenericClientPassthrough.cs:649-695`; a missing required parameter fails with `Missing required parameter`): a route id is a separate `string` parameter named `packageId`, `responseId`, `relationshipId`, or `hvRequestId`; a command's body record is one parameter named `request`; a query's filter record is one parameter named `query`; a scalar such as `includeContent` keeps its name. So the REPL params of BuildPackageAsync are `{ request: { sessionId, agent, requestId, queryText, tokenBudget, focusRefs, idempotencyKey } }`, of GetPackageAsync `{ packageId, includeContent }`, of RejectResponseAsync `{ responseId, request: { reason } }`, and of QueryPackagesAsync `{ query: { requestId, sessionId, status, limit, offset } }`. PowerShell plugins call `Invoke-McpPlugin.ps1 -Command Invoke -Method client.AgentContext.<Method> -ParamsObject <params>` with `<params>` from the exported builder named in each unit (`McpPluginShim.psm1`). Builder contract, one builder per externally callable unit: `New-McpAgentContext<Verb>Params` takes one PowerShell parameter per route id, scalar, and body or filter field, named in PascalCase, typed string as `[string]`, bool as `[bool]`, int as `[int]`, a decimal score as `[string]` holding the exact lexeme, and a list as `[object[]]` of hashtables with the record's camelCase keys; a parameter is mandatory exactly when the client parameter or record field is required, and an omitted optional parameter is left out of the output (the server applies the default); the builder returns that envelope as an ordered hashtable: route ids and scalars at the top level under their client parameter names, and the record's camelCase fields, in record order with the values unchanged, nested under `request` or `query`; it never calls the server (`AgentContextBuildersParametersMatchRequestRecordsOutputKeysAndValuesExact`, generated over every unit from `docs/context/agent-context-contract.json`, and `AgentContextBuildersEnvelopesBindThroughGenericClientPassthrough`, which binds every builder's output through the real `GenericClientPassthrough` binder against the real `AgentContextClient` signatures). Tool descriptor contract: each externally callable unit has one entry in `lib/tool-descriptors.json` with `name` (the `agent_context_*` name in each unit), `description`, `inputSchema` (the JSON schema of that same envelope: route ids and scalars as top-level properties, the body as a required `request` or `query` object with the record's schema, scores as strings), `method` (the `client.AgentContext.<Method>` REPL method listed in each unit), and `mutates` (true for commands, false for queries), and the file holds exactly one `agent_context_*` entry per externally callable unit of `docs/context/agent-context-contract.json`, with that unit's method, `mutates` flag, and envelope schema (`ToolDescriptorsAgentContextOneEntryPerExternalUnitMatchesContract`); a TypeScript host's adapter runs `Invoke-McpPlugin.ps1 -Command Invoke -Method <method> -InvocationFromStdin` with `{ params, hostContext }` as JSON on stdin (the call's arguments, an envelope of that schema, and the host's separate tool context) (TR-MCP-SESSIONTURN-008), so PowerShell and TypeScript hosts reach the same command (operator, 2026-10-11: "You should be designing all scripts and script tools around PowerShell and only PowerShell.").

Transaction-gated REPL clients (section 3.1; updates FR-MCP-120 and TR-MCP-TXN-001 with the agent-context classification): in `KnownUnsafeClientMutationPolicy` the `agentContext` client namespace joins `ServiceGatedClientNamespaces`, because each of its writes runs in the server's SerializableWrite, and its eleven query methods (`GetStatusAsync`, `GetPackageAsync`, `QueryPackagesAsync`, `ReplayPackageAsync`, `GetResponseAsync`, `QueryResponsesAsync`, `QueryRelationshipsAsync`, `RetrieveAsync`, `GetPilotReportAsync`, `GetHvRequestAsync`, `QueryHvRequestsAsync`) join `ReadMethods` (`src/QBrainAi.Repl.Core/ClientMutationPolicy.cs:100-174`). `Evaluate` drops the `!ReadMethods.ContainsKey(clientName)` clause at `:213`, which today keeps any namespace with listed reads from being service-gated (section 3.1); no existing decision changes, because no develop namespace is in both lists (`:100-174`). A listed read passes before any state check (`:197-198`), so no query below is refused by the policy. A command is refused in the REPL, before any server call, with `transaction_degraded` while the turn transaction coordinator is degraded (`:201-208`); `mutation_not_transactional` (`:216-218`) answers a method that is neither a listed read nor in a service-gated namespace while required turn transactions are active, which no agent-context command reaches once the namespace is service-gated. Both codes join `ErrorCodes` and every command's error list below as REPL only; a plugin that receives either from a build treats it as a build error (section 6.6). Cases (section 6.4): `ReplAgentContextReadsAllowedWhileDegradedWritesTransactionDegraded`, `ReplAgentContextWritesAllowedWhileTransactionsRequiredServiceGated`.

Projection pass (section 3.1, Projection pass), stated here once and not drawn per unit: a unit whose transaction marked a projection row through ResponseStatusTransition (the grade units through the trust decision, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand) dispatches ProjectAgentResponsesCommand with no `responseIds` through `IDispatcher` after its SerializableWrite commits and before it answers, logging a failed dispatch without changing its own answer (`ProjectionPassDispatchedAfterTrustCommitFailureLoggedUnitSucceeds`).

Request pipeline (section 3.5.1), stated here once and not drawn per unit: WorkspaceResolutionBehavior and then ModeGateBehavior run inside the scoped `IDispatcher` before every handler below, after the controller or the MCP tool calls `SendAsync` or `QueryAsync`; request validation runs in the handler and is its first drawn step, so each diagram starts where the handler starts. WorkspaceResolutionBehavior refuses a record path that names no registered workspace with 400 `validation_error`, `details.field` `workspacePath`, in every unit (`WorkspaceResolutionUnregisteredPath400EverySurface`). ModeGateBehavior refuses with 409 `agent_context_disabled` while `Mcp:AgentContext:Mode` is `Off`, only for a gated record (`ModeOffEveryGatedCommand409AgentContextDisabledQueriesAnswer`). Gated means the record implements `IModeGatedRequest`, which every agent-context command does except DecideAgentResponseTrustCommand: BuildAgentContextPackageCommand, SubmitAgentResponseCommand, RunDeterministicGradeCommand, RecordModelGradeCommand, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand, CreateAgentContextRelationshipCommand, DeleteAgentContextRelationshipCommand, ProjectAgentResponsesCommand, and CreateAgentContextHvRequestCommand. DecideAgentResponseTrustCommand (dispatched only by the two grade units, inside their transaction) and every query (GetAgentContextStatusQuery and the read-only queries) do not implement it and are not gated, so packages, responses, grades, relationships, HV requests, and the pilot report stay readable for audit in `Off` (section 3.1, Reads in `Off`). Each unit's error list names the workspace 400 and, for a gated unit only, the 409 `agent_context_disabled`. A requirement update or delete dispatches EvaluateStalenessCommand only while Mode is not `Off`; a dependency that changed while Mode was `Off` is still never reported fresh, because every read evaluates freshness read-only and every unit that relies on a persisted status dispatches EvaluateStalenessCommand before it relies on that status and before its own SerializableWrite.

Shared components (section 3.5) are drawn as call nodes `[["Component: purpose"]]` with only the exits the component returns, and their rules are not restated here: TurnLocator, IdempotentCreate (policy `Conflict` for every agent-context create, `ReturnStored` inside RelationshipWriter; the lookup runs first, before any precondition that can change between attempts; a unit that reads outside the database (Build, Submit, and the model grade) runs it read-only before those reads and again as the first step of its delegate), SerializableWrite (drawn as a call node followed by a box that holds its delegate: every check in the box and the save run in one Serializable transaction, a refusal inside the box rolls back, and every unit that runs through it has the exit 409 `conflict` with `retryable: true` when its four attempts are exhausted; the box reads and writes only the database, so a unit's git, file, source, retrieval, and GraphRAG reads are drawn before the call node, and the box re-checks the database state those reads relied on, a changed state ending the attempt so that the unit reads again within the same bound, section 3.1, Database-only delegates), ReadableKeyAllocator (the `pkg-`, `rsp-`, `grd-`, `rel-`, and `hvr-` keys, named where each unit inserts the record, in the save node or in the text above its diagram; its exhaustion is the same retryable 409 `conflict`), PagedQuery with `PageBounds` policy `Refuse` and the wire type `PageResult<TItem>`, SourceResolver, PackageAssembler with its nested loop FillBudgetWithCandidates, CandidateRanker, BuiltPackageLookup, ResponseStatusTransition, IResponseStalenessRule, ResponseIdList, RelationshipWriter, ScoreLexeme, ScoreVerdict, and SecretRedactor. A record looked up by its route id that does not exist in the workspace is 404 `not_found` with `details.resource` naming it (`package`, `response`, `relationship`, `hv_request`; `RouteIdUnknownInWorkspace404NotFoundNamesResource`). A paging `limit` or `offset` outside `PageBounds` is 400 `validation_error` with `details.field` naming it (`PagedQueriesLimitOrOffsetOutOfBounds400NamesField`, over the four paged queries and RetrieveAgentContextCandidatesQuery's `limit`). Outside SerializableWrite every unit keeps develop's retryable 503 storage answers, `backend_unavailable` and, for a busy store, `persistence_error` with `retryable: true` (`src/QBrainAi.Support.Mcp/Services/McpErrorClassifier.cs:129-139` at `a8d3934b`, section 6.4); they are stated here once and not repeated in each error list, and a plugin treats either as an outage (section 3.1, Deadlock and busy outcomes).

#### GetAgentContextStatusQuery: get status [FR-MCP-AGENTCONTEXT-008, -009]

- Handler: `GetAgentContextStatusQuery(string WorkspacePath) : IQuery<AgentContextStatusResult>` in `src/QBrainAi.Services/AgentContext/Packages/Queries/GetAgentContextStatusQuery.cs` and `GetAgentContextStatusQueryHandler` in `src/QBrainAi.Services/AgentContext/Packages/Queries/GetAgentContextStatusQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/status`; MCP `agent_context_status(workspacePath)`; client `GetStatusAsync()`; REPL `client.AgentContext.GetStatusAsync`; PowerShell `New-McpAgentContextStatusParams`; tool descriptor `agent_context_status`.
- Not gated: it answers in every Mode, `Off` included, and is the call through which plugins read `mode` (section 6.6).
- Counts: responses are counted by effective status, computed at read and written nowhere, through IResponseStalenessRule read-only, so a stored `Trusted` response whose dependency changed counts as `Stale`; the query writes nothing (`StatusAfterDependencyChangeCountsStaleNotTrustedWritesNothing`).
- Cost at host session start (risk R-R4; no parameter or query is added): plugins make this read once per host session start for `mode` (section 6.6), so the counts must not grow with responses times dependencies. The handler evaluates the `Submitted`, `Grading`, and `Trusted` responses with one SourceResolver read per distinct dependency and commit, the read each evaluation makes anyway, shared by every response that cites that dependency (inference: the work is bounded by the distinct files, requirements, receipts, and turns cited, not by the response count), and answers within the SessionStart hook bound with 1000 `Trusted` responses (`StatusThousandTrustedResponsesAnswersWithinSessionStartHookBound`). A plugin whose read misses the hook bound has an unreadable Mode and runs the host session as `Off` (section 3.1, Mode changes).
- Request: none. Response 200 `AgentContextStatusResult`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior). REPL: a listed read, never refused by ClientMutationPolicy.

```mermaid
flowchart TD
    S(["Handler runs: GET status"]) --> O["Read AgentContextOptions in force"]
    O --> G["IGraphRagService.GetStatusAsync: enabled and state"]
    G --> ST[["IResponseStalenessRule, read-only: Submitted, Grading, Trusted"]]
    ST --> C["Count packages, and responses by effective status"]
    C --> R(["200 AgentContextStatusResult"])
```

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

```mermaid
flowchart TD
    S(["Handler runs: POST packages"]) --> V{"Fields present, tokenBudget in range?"}
    V -->|no| E1(["400 validation_error"])
    V -->|yes| ICR[["IdempotentCreate lookup, read-only"]]
    ICR -->|"stored, same client inputs"| PST{"Stored package status?"}
    ICR -->|"key reused, other inputs"| E5(["409 conflict, retryable only when exhausted"])
    ICR -->|absent| RB[["ReadBuildInputs, no transaction open"]]
    PST -->|Built| EX(["200 the stored package"])
    PST -->|Overflow| E6(["422 agent_context_budget_exceeded"])
    RB -->|refused| ER(["Refusal of ReadBuildInputs, drawn there"])
    RB -->|input snapshot| PA[["PackageAssembler in memory"]]
    PA --> SP[["SavePackage"]]
    SP -->|"stored Built"| EX
    SP -->|"stored Overflow, or Overflow committed"| E6
    SP -->|"key reused, or retries exhausted"| E5
    SP -->|"turn status changed: read again"| ICR
    SP -->|"Built package committed"| OK(["201 package, status Built"])
```

##### Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite)

No transaction is open. RetrieveAgentContextCandidatesQuery is dispatched with the package's `agent`, `sessionId`, `requestId`, and `focusRefs`, and `preferGraphRag` equal to `GraphRag:UseForRetrieval`; the mandatory items (request, profile, rules, applicable requirements) and every focus ref join its results, and the request's own turn is never a candidate. IResponseStalenessRule evaluates each prior-response candidate read-only and its effective status becomes the input's `ResponseStatus`. SourceResolver reads and hashes every candidate (through ContentHasher), and the snapshot's `InputsSha256` is computed over them. The turn status noted here is the state the delegate re-checks.

```mermaid
flowchart TD
    S(["ReadBuildInputs: no transaction open"]) --> TL[["TurnLocator: agent, sessionId, requestId"]]
    TL -->|InvalidIdentifier| E1(["400 validation_error, sessionId or requestId"])
    TL -->|"SessionNotFound or TurnNotFound"| E3(["404 not_found session_turn"])
    TL -->|"turn found, status noted"| GIT[["SourceResolver.ReadCommit: HEAD commit"]]
    GIT -->|unavailable| E4(["409 agent_context_commit_unavailable"])
    GIT -->|HEAD commit| PRF{"A profile file and a rule file resolve?"}
    PRF -->|no| E7(["409 agent_context_profile_unavailable"])
    PRF -->|yes| GA["Candidates: mandatory items, focus refs, Retrieve results"]
    GA --> ST[["IResponseStalenessRule, read-only: prior responses"]]
    ST --> IH[["SourceResolver: content and hash of each candidate"]]
    IH --> R(["Input snapshot with InputsSha256"])
```

##### Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler)

The delegate reads and writes only the database: it runs the key lookup again, re-checks that the turn still has the status ReadBuildInputs noted (a change ends the attempt and the unit reads again, within SerializableWrite's bound of four attempts), and inserts, with a `pkg-` key from ReadableKeyAllocator, either the `Overflow` package or the `Built` package with its RelationshipWriter links. A key collision ends the attempt and SerializableWrite runs the delegate again; a refusal rolls back and is returned unchanged.

```mermaid
flowchart TD
    S(["SavePackage: snapshot and assembled package"]) --> SW[["SerializableWrite, database only"]]
    SW -->|retries exhausted| EC(["409 conflict, retryable true"])
    subgraph TX [Delegate of SerializableWrite]
        IC[["IdempotentCreate lookup"]]
        TS{"Turn status as the reads saw it?"}
        SO[["Insert the Overflow package"]]
        SV[["Insert the Built package"]]
        LK[["RelationshipWriter: derived_from, relevant_to"]]
    end
    SW -->|attempt| IC
    IC -->|"stored, same client inputs"| ST(["Returned: the stored package"])
    IC -->|"key reused, other inputs"| E5(["409 conflict, retryable false"])
    IC -->|absent| TS
    TS -->|"changed: attempt ends"| RA(["Returned: read again"])
    TS -->|"same, mandatory over the budget"| SO
    TS -->|"same, assembled"| SV
    SO -->|committed| E6(["422 agent_context_budget_exceeded"])
    SV --> LK
    LK -->|committed| OK(["201 package, status Built"])
    SO -->|"key collision: attempt ends"| SW
    SV -->|"key collision: attempt ends"| SW
```

##### Nested activity: FillBudgetWithCandidates (loop of PackageAssembler, for BuildAgentContextPackageCommand and ReplayAgentContextPackageQuery)

Each optional kind keeps at most `MaxCandidatesPerKind` candidates in rank order (`PriorResponse` at most `MaxPriorResponses`); mandatory items are never cut. A candidate whose tokens equal the remaining budget is included (`FillBudgetCandidateEqualsRemainingIncluded`, `BuildCandidatesBeyondPerKindLimitOmittedBelowCandidateLimit`, `BuildMaxPriorResponsesZeroNoPriorResponseItem`).

```mermaid
flowchart TD
    S(["Optional candidates after the secret and omission filters"]) --> RK[["CandidateRanker: score rounded half-even to 6 places, descending, then sourceRef by ordinal comparison"]]
    RK --> MORE{"Next candidate in rank order?"}
    MORE -->|none left| DONE(["Return every candidate placed"])
    MORE -->|candidate| LIM{"Beyond the first MaxCandidatesPerKind of its kind in rank order, or MaxPriorResponses for PriorResponse?"}
    LIM -->|yes| OML["Omit with reason below_candidate_limit"]
    LIM -->|no| FIT{"Its tokens at most the remaining budget?"}
    FIT -->|yes| ADD["Include with score, rationale, and retrieval path, then subtract its tokens"]
    FIT -->|no| OMB["Omit with reason budget"]
    ADD --> MORE
    OMB --> MORE
    OML --> MORE
```

#### GetAgentContextPackageQuery: get a package [FR-MCP-AGENTCONTEXT-001]

- Handler: `GetAgentContextPackageQuery(string WorkspacePath, string PackageId, bool IncludeContent) : IQuery<AgentContextPackageResult>` in `src/QBrainAi.Services/AgentContext/Packages/Queries/GetAgentContextPackageQuery.cs` and `GetAgentContextPackageQueryHandler` in `src/QBrainAi.Services/AgentContext/Packages/Queries/GetAgentContextPackageQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/packages/{packageId}?includeContent=true`; MCP `agent_context_get_package(workspacePath, packageId, includeContent)`; client `GetPackageAsync(packageId, includeContent)`; REPL `client.AgentContext.GetPackageAsync`; PowerShell `New-McpAgentContextGetPackageParams`; tool descriptor `agent_context_get_package`.
- Not gated. Response 200 `AgentContextPackageResult` (a `Built` or `Overflow` package). Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior); 404 `not_found` (`package`). REPL: a listed read, never refused by ClientMutationPolicy.

```mermaid
flowchart TD
    S(["Handler runs: GET package"]) --> F{"Package found in the workspace?"}
    F -->|no| E2(["404 not_found package"])
    F -->|yes| IC{"includeContent?"}
    IC -->|yes| FULL["Load items with content, omissions, findings, rendered content"]
    IC -->|no| SUM["Load items without content, omissions, findings"]
    FULL --> R(["200 AgentContextPackageResult"])
    SUM --> R
```

#### QueryAgentContextPackagesQuery: query packages [FR-MCP-AGENTCONTEXT-001]

- Handler: `QueryAgentContextPackagesQuery(string WorkspacePath, AgentContextPackageQuery Query) : IQuery<PageResult<AgentContextPackageResult>>` in `src/QBrainAi.Services/AgentContext/Packages/Queries/QueryAgentContextPackagesQuery.cs` and `QueryAgentContextPackagesQueryHandler` in `src/QBrainAi.Services/AgentContext/Packages/Queries/QueryAgentContextPackagesQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/packages?requestId&sessionId&status&limit&offset`; MCP `agent_context_query_packages(workspacePath, requestId, sessionId, status, limit, offset)`; client `QueryPackagesAsync(AgentContextPackageQuery)`; REPL `client.AgentContext.QueryPackagesAsync`; PowerShell `New-McpAgentContextQueryPackagesParams`; tool descriptor `agent_context_query_packages`.
- Paging: PagedQuery with `PageBounds` `Refuse` (limit 1..1000, default 50, offset at least 0), stable order `CreatedUtc` descending, then `PackageId`. RunStatelessInvocation finds a request's package again with `{ requestId, status: Built }` (section 6.6).
- Not gated. Response 200 `PageResult<AgentContextPackageResult>` whose items are package summaries (`items`, `omissions`, `findings`, and `renderedContent` null, section 6.4). Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; an unknown status; `limit` or `offset` outside the bounds, `details.field` naming it). REPL: a listed read, never refused by ClientMutationPolicy.

```mermaid
flowchart TD
    S(["Handler runs: GET packages"]) --> V{"Status filter absent or known?"}
    V -->|no| E2(["400 validation_error"])
    V -->|yes| PQ[["PagedQuery, PageBounds Refuse: filter, order CreatedUtc descending, then PackageId"]]
    PQ -->|"limit or offset outside the bounds"| E2
    PQ -->|page| R(["200 PageResult of package summaries"])
```

#### ReplayAgentContextPackageQuery: replay a package [FR-MCP-AGENTCONTEXT-001]

- Handler: `ReplayAgentContextPackageQuery(string WorkspacePath, string PackageId) : IQuery<AgentContextReplayResult>` in `src/QBrainAi.Services/AgentContext/Packages/Queries/ReplayAgentContextPackageQuery.cs` and `ReplayAgentContextPackageQueryHandler` in `src/QBrainAi.Services/AgentContext/Packages/Queries/ReplayAgentContextPackageQueryHandler.cs`.
- REST `POST /qbrainai/agent-context/packages/{packageId}/replay`; MCP `agent_context_replay_package(workspacePath, packageId)`; client `ReplayPackageAsync(packageId)`; REPL `client.AgentContext.ReplayPackageAsync`; PowerShell `New-McpAgentContextReplayPackageParams`; tool descriptor `agent_context_replay_package`.
- Replay runs PackageAssembler, the same component the build ran, in memory on the package's immutable input snapshot only (the input rows with each prior-response candidate's `ResponseStatus` and each candidate's `ContainsSecret`, the input links that the findings read, `OptionsSnapshotJson`, and the focus refs, section 6.3), so every assembler step (secret filter, omission filter, findings, the mandatory-context check, FillBudgetWithCandidates, rendering) is reproduced; it reads no live source, configuration, relationship, or response status, stores nothing, and compares the result over the stored normal forms (ContentHasher, section 3.5.2), so replay stays identical after configuration, omitted-source, status, and non-Git source changes (`ReplayAfterConfigChangeIdentical`, `ReplayAfterOmittedSourceChangeIdentical`, `ReplayAfterRequirementTodoTurnProfileChangeIdentical`, `ReplaySourceWithEmAndEnDashesIdenticalTrue`).
- Not gated. Response 200 `AgentContextReplayResult`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior); 404 `not_found` (`package`); 409 `agent_context_package_overflow` (BuiltPackageLookup, `SubmitOrReplayOverflowPackage409PackageOverflow`), `agent_context_replay_snapshot_unavailable` (no stored snapshot, `ReplayNoInputSnapshot409ReplaySnapshotUnavailable`), `agent_context_replay_builder_changed` (built by another builder version; `details { recorded, current }`, `ReplayOtherBuilderVersion409ReplayBuilderChangedRecordedAndCurrent`). REPL: a listed read, never refused by ClientMutationPolicy.

```mermaid
flowchart TD
    S(["Handler runs: POST replay"]) --> BL[["BuiltPackageLookup: packageId"]]
    BL -->|not found| E2(["404 not_found package"])
    BL -->|Overflow| E3(["409 agent_context_package_overflow"])
    BL -->|Built package| SN{"Input snapshot stored?"}
    SN -->|no| E4(["409 agent_context_replay_snapshot_unavailable"])
    SN -->|yes| BV{"BuilderVersion equals the running builder's version?"}
    BV -->|no| E5(["409 agent_context_replay_builder_changed with recorded and current"])
    BV -->|yes| RB[["PackageAssembler on the stored input snapshot: no live source read, nothing stored"]]
    RB --> CMP["Compare InputsSha256, ContentSha256, and items by ordinal"]
    CMP --> SAME{"Identical?"}
    SAME -->|yes| R1(["200 identical true"])
    SAME -->|no| R2(["200 identical false with differences: added, removed, hashChanged, reordered"])
```

#### SubmitAgentResponseCommand: submit a response [FR-MCP-AGENTCONTEXT-003]

- Handler: `SubmitAgentResponseCommand(string WorkspacePath, SubmitAgentResponseRequest Request) : ICommand<AgentResponseResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/SubmitAgentResponseCommand.cs` and `SubmitAgentResponseCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/SubmitAgentResponseCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/responses`; MCP `agent_context_submit_response(workspacePath, requestJson)`; client `SubmitResponseAsync(SubmitAgentResponseRequest)`; REPL `client.AgentContext.SubmitResponseAsync`; PowerShell `New-McpAgentContextSubmitResponseParams`; tool descriptor `agent_context_submit_response`. In `Pilot` and `Stateless` the model calls this tool before it closes the turn, and the plugin supplies the request's `packageId` (section 6.6).
- Response 201 `AgentResponseResult` with status `Submitted`, or 200 with the stored response for a repeated idempotency key with an identical submission (IdempotentCreate, policy `Conflict`, fingerprint `SubmissionSha256` of the whole client submission, section 6.3; `SubmitSameKeyIdenticalSubmission200Existing`, `SubmitSameKeySameContentDifferentPackage409Conflict`, `SubmitSameKeySameContentDifferentClaimsOrEvidence409Conflict`). The unit runs the lookup read-only first, before the package, commit, and evidence reads, and the delegate runs it again as its first step (section 3.5.2).
- Consistency with the package: `agent` equals the package's agent (`SubmitAgentDiffersFromPackage400`); `invocationMode` `Stateless` only on a package whose `OptionsSnapshotJson` mode is `Stateless` (`SubmitStatelessModeOnPilotPackageRejected400`); no `SessionLogTurn` evidence names the package's own turn `<agent>/<sessionId>/<requestId>` (`SubmitEvidenceCitesOwnTurnRejected400`). Each refusal is 400 `validation_error` with `details.field` naming the field or evidence ref.
- Evidence hashes (SourceResolver): each `File` is read at its `commitSha`, by default the HEAD commit read through `ReadCommit`; `Requirement`, `Receipt`, and `SessionLogTurn` are read from their sources; each cited hash is kept as `CitedSha256` and the server's hash is `Sha256`, null when an existing source cannot be read. The response, its claims and evidence, and its links (RelationshipWriter, `CreatedBy` `SubmitAgentResponseCommand`: `derived_from` the package, and `supports` for each `Requirement` or `File` evidence item, with the hashes read before the write) are saved in one transaction, so a failure before the commit leaves none of them (`SubmitLinkWriteFailsNothingCommitted`).
- Reads before the write (section 3.1, Database-only delegates; nested activity ReadSubmissionInputs): the package lookup, the consistency checks, the HEAD commit, and every evidence hash are read with no transaction open, and SecretRedactor redacts the content and its `ContentSha256` (ContentHasher, over the stored normal form, `SubmitContentWithEmDashContentSha256MatchesStoredContent`) is computed in memory. The delegate reads and writes only the database: it runs the key lookup again and inserts the response, its children, and its links. A stored package never changes (inference: no unit of this section updates a package after its insert, and its input snapshot is immutable, section 6.3), so the key is the only state the delegate re-checks.
- Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; empty content, no claim, a claim with no evidence item, duplicate claimId, evidence pointing to an unknown claim, an evidence kind outside `File`, `Requirement`, `Receipt`, `Commit`, `SessionLogTurn` (`SubmitCommandEvidenceKindRejected400`), a `Commit` ref that is not a 40-hex SHA, a `citedSha256` that is not 64 hex digits, a `File`, `Requirement`, `Receipt`, or `SessionLogTurn` ref that names no existing source (`SubmitEvidenceRefNamesNoSourceRejected400`), unknown format or mode, and the consistency refusals above); 404 `not_found` (`package`); 409 `agent_context_disabled`, `agent_context_package_overflow` (BuiltPackageLookup, `SubmitOrReplayOverflowPackage409PackageOverflow`), `agent_context_commit_unavailable` (SourceResolver `ReadCommit`, `SubmitCommitUnreadable409CommitUnavailable`), `conflict` with `retryable: false` (the key reused with a different submission: any differing package, agent, model, effort, mode, format, content, claim, or evidence), `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator exhausted after four attempts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

The well-formedness check covers: content nonempty, at least one claim, every claim with evidence, claimIds unique, evidence kinds, refs, and hashes well formed, format and mode known. The insert takes a `rsp-` key from ReadableKeyAllocator through IdempotentCreate and stores the response `Submitted`, its claims, and its evidence with both hashes, with the root ids copied from the package.

```mermaid
flowchart TD
    S(["Handler runs: POST responses"]) --> V{"Submission well formed?"}
    V -->|no| E2(["400 validation_error, details.field"])
    V -->|yes| ICR[["IdempotentCreate lookup, read-only"]]
    ICR -->|"stored, same submission"| EX(["200 the stored response"])
    ICR -->|"key reused, other submission"| E5(["409 conflict, retryable false"])
    ICR -->|absent| RS[["ReadSubmissionInputs, no transaction open"]]
    RS -->|refused| ER(["Refusal of ReadSubmissionInputs, drawn there"])
    RS -->|"package and evidence hashes"| FP[["SecretRedactor, then ContentSha256"]]
    FP --> SW[["SerializableWrite, database only"]]
    SW -->|retries exhausted| EC(["409 conflict, retryable true"])
    subgraph TX [Delegate of SerializableWrite]
        IC[["IdempotentCreate lookup"]]
        INS[["Insert the response, claims, evidence"]]
        LK[["RelationshipWriter: derived_from, supports"]]
    end
    SW -->|attempt| IC
    IC -->|"stored, same submission"| EX
    IC -->|"key reused, other submission"| E5
    IC -->|absent| INS
    INS -->|"key collision: attempt ends"| SW
    INS --> LK
    LK -->|committed| OK(["201 response, status Submitted"])
```

##### Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite)

No transaction is open. The consistency check refuses a submission whose `agent` differs from the package's, an `invocationMode` `Stateless` on a package not built in `Stateless`, and `SessionLogTurn` evidence naming the package's own turn, each with `details.field` naming the field or evidence ref. SourceResolver reads the HEAD commit, hashes each `File` at its `commitSha` (by default that commit) and each `Requirement`, `Receipt`, and `SessionLogTurn` ref from its source.

```mermaid
flowchart TD
    S(["ReadSubmissionInputs: no transaction open"]) --> BL[["BuiltPackageLookup: packageId"]]
    BL -->|not found| E3(["404 not_found package"])
    BL -->|Overflow| E4(["409 agent_context_package_overflow"])
    BL -->|Built package| CS{"Agent, mode, and evidence fit the package?"}
    CS -->|no| E2(["400 validation_error, details.field"])
    CS -->|yes| SR[["SourceResolver: ReadCommit, then each evidence hash"]]
    SR -->|"commit unavailable"| E6(["409 agent_context_commit_unavailable"])
    SR -->|"a ref names no existing source"| E2
    SR -->|"hashes, null where unreadable"| R(["Package and evidence hashes"])
```

#### GetAgentResponseQuery: get a response [FR-MCP-AGENTCONTEXT-003]

- Handler: `GetAgentResponseQuery(string WorkspacePath, string ResponseId, bool IncludeContent) : IQuery<AgentResponseResult>` in `src/QBrainAi.Services/AgentContext/Responses/Queries/GetAgentResponseQuery.cs` and `GetAgentResponseQueryHandler` in `src/QBrainAi.Services/AgentContext/Responses/Queries/GetAgentResponseQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/responses/{responseId}?includeContent=true`; MCP `agent_context_get_response(workspacePath, responseId, includeContent)`; client `GetResponseAsync(responseId, includeContent)`; REPL `client.AgentContext.GetResponseAsync`; PowerShell `New-McpAgentContextGetResponseParams`; tool descriptor `agent_context_get_response`.
- Freshness: a read never reports `Trusted` for a response whose dependency changed. The handler evaluates the response with IResponseStalenessRule read-only (a `Rejected`, `Superseded`, or already `Stale` response is not evaluated) and returns the effective status (`Stale`, with `statusReason` naming the changed reference) without writing: a query never writes (`src/QBrainAi.Cqrs/IQuery.cs:5`, "Queries represent intent to read state without side effects"), and the stored status changes only through the commands that persist staleness (`GetResponseAfterFileChangeReportsStale`, `GetResponseAfterFileChangeWritesNothing`).
- Not gated. Response 200 `AgentResponseResult` (with every grade). Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior); 404 `not_found` (`response`). REPL: a listed read, never refused by ClientMutationPolicy.

```mermaid
flowchart TD
    S(["Handler runs: GET response"]) --> F{"Response found in the workspace?"}
    F -->|no| E2(["404 not_found response"])
    F -->|yes| ST[["IResponseStalenessRule, read-only: effective status, nothing written"]]
    ST -->|"fresh, stale with the changed reference, or not evaluated"| L["Load claims, evidence, grades with findings, and content when requested, and report the effective status"]
    L --> R(["200 AgentResponseResult"])
```

#### QueryAgentResponsesQuery: query responses [FR-MCP-AGENTCONTEXT-003, -004]

- Handler: `QueryAgentResponsesQuery(string WorkspacePath, AgentResponseQuery Query) : IQuery<PageResult<AgentResponseResult>>` in `src/QBrainAi.Services/AgentContext/Responses/Queries/QueryAgentResponsesQuery.cs` and `QueryAgentResponsesQueryHandler` in `src/QBrainAi.Services/AgentContext/Responses/Queries/QueryAgentResponsesQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/responses?requestId&packageId&status&trustedOnly&limit&offset`; MCP `agent_context_query_responses(string workspacePath, string? requestId = null, string? packageId = null, string? status = null, bool trustedOnly = false, int limit = 50, int offset = 0)`; client `QueryResponsesAsync(AgentResponseQuery)`; REPL `client.AgentContext.QueryResponsesAsync`; PowerShell `New-McpAgentContextQueryResponsesParams`; tool descriptor `agent_context_query_responses`.
- Freshness: before filtering, counting, and paging, the handler evaluates with IResponseStalenessRule read-only every response matching the other filters whose status is `Submitted`, `Grading`, or `Trusted`, and filters, counts, and pages by the effective status, so `trustedOnly` and a `status` filter never return or count a response whose dependency changed; it writes nothing (`QueryResponsesTrustedOnlyAfterFileChangeExcludesStale`, `QueryResponsesAfterFileChangeWritesNothing`).
- Paging: PagedQuery with `PageBounds` `Refuse` (limit 1..1000, default 50, offset at least 0), stable order `SubmittedUtc` descending, then `ResponseId`.
- Not gated. Response 200 `PageResult<AgentResponseResult>` whose items are response summaries (`claims`, `evidence`, `grades`, and `content` null, section 6.4). Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; an unknown status; `limit` or `offset` outside the bounds, `details.field` naming it). REPL: a listed read, never refused by ClientMutationPolicy.

```mermaid
flowchart TD
    S(["Handler runs: GET responses"]) --> V{"Status filter absent or known?"}
    V -->|no| E2(["400 validation_error"])
    V -->|yes| ST[["IResponseStalenessRule, read-only: effective status of every matching Submitted, Grading, or Trusted response, nothing written"]]
    ST --> TO{"trustedOnly?"}
    TO -->|yes| T["Restrict to effective status Trusted"]
    TO -->|no| A["Every effective status, or the requested one"]
    T --> PQ[["PagedQuery, PageBounds Refuse: order SubmittedUtc descending, then ResponseId"]]
    A --> PQ
    PQ -->|"limit or offset outside the bounds"| E2
    PQ -->|page| R(["200 PageResult of response summaries"])
```

#### RunDeterministicGradeCommand: run the deterministic grade [FR-MCP-AGENTCONTEXT-004]

- Handler: `RunDeterministicGradeCommand(string WorkspacePath, string ResponseId) : ICommand<AgentResponseGradeResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/RunDeterministicGradeCommand.cs` and `RunDeterministicGradeCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/RunDeterministicGradeCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/responses/{responseId}/grades/deterministic`; MCP `agent_context_grade_deterministic(workspacePath, responseId)`; client `RunDeterministicGradeAsync(responseId)`; REPL `client.AgentContext.RunDeterministicGradeAsync`; PowerShell `New-McpAgentContextGradeDeterministicParams`; tool descriptor `agent_context_grade_deterministic`.
- Checks, one finding per evidence item, by kind: for `File` (at its `commitSha`), `Requirement`, `Receipt`, and `SessionLogTurn`, PASS when `Sha256` is non-null and `CitedSha256` is absent or equal to it, FAIL when `CitedSha256` differs from a non-null `Sha256` (so a wrong cited hash stays FAIL after any restart, `GradeWrongCitedHashFailsAfterRestart`), and UNKNOWN when `Sha256` is null because the existing source could not be read at submission (a ref naming no source never reaches the grade: SubmitAgentResponseCommand refuses it); for `Commit`, PASS when `Ref` names a commit of the workspace repository that equals the response's `CommitSha` or is one of its ancestors, FAIL when the commit does not exist or is not such an ancestor, and UNKNOWN when the workspace is not a git checkout (`GradeCommitEvidenceAncestorPassMissingOrUnrelatedFailNoGitUnknown`). The commit and ancestry are read through SourceResolver; a grade judges evidence and never refuses for an unreadable commit, so where Build and Submit return 409 `agent_context_commit_unavailable`, the grade records UNKNOWN, which fails the verdict. Changes to a source after submission are staleness (IResponseStalenessRule), not grade findings. Global checks, one finding each: every claim has at least one evidence item; the content, every claim text, and every evidence ref contain no secret (`SecretRedactor.ContainsSecret`); the response and its package are fresh: after every read-only check it has (the response found and gradable, the evidence checks above, and the secret scan) and before its SerializableWrite, the unit dispatches EvaluateStalenessCommand for the response through `IDispatcher` (only EvaluateStalenessCommand persists `Stale`, section 3.1, Database-only delegates), and the finding is FAIL when the response is then `Stale`, so the trust decision reads the status that dispatch left.
- Scores: correctness = PASS findings / (PASS + FAIL) over evaluated checks, and 0 when no check could be evaluated (PASS + FAIL = 0); completeness = claims whose every evidence check is PASS / all claims (SubmitAgentResponseCommand guarantees at least one claim, each with evidence); both exact decimals. The verdict is ScoreVerdict's with `max(configured, 0.98)` and zero FAIL and UNKNOWN findings, so any UNKNOWN finding fails the verdict and a zero denominator can never yield trust.
- Each run adds a grade (a deterministic grade has no idempotency key and is re-run on request) under a key from ReadableKeyAllocator, `grd-<responseId>-deterministic-<nn>`, whose suffix widens past 99 (`ReadableKeyAllocatorSuffixPast99WidensLatestGradeOrderKept`); the server stamps `RecordedUtc`, which orders grades (section 6.3).
- Reads before the write and concurrency (section 3.1, Database-only delegates): with no transaction open the unit runs every read-only check it has (the response found and gradable, the evidence checks with their git reads, and the secret scan), then dispatches EvaluateStalenessCommand for the response, and derives the staleness finding, the scores, and the verdict in memory from the status that dispatch left (section 3.1: every read-only check, then the dispatch, then the SerializableWrite). The SerializableWrite that locks the response then re-checks that the response still has the status and `StatusVersion` read after the dispatch (a change ends the attempt and the unit reads again within the bound of four attempts, so a response rejected meanwhile is then refused 409 `agent_context_response_not_gradable`, `GradeResponseRejectedDuringReadsReadsAgain409NotGradable`), saves the grade, and dispatches the trust decision in the same transaction, so a concurrent reject, supersede, or staleness change commits before or after it (`TrustForcedInterleavingDeterministicFailVsModelPassNeverTrustedOnSupersededGrade`, `TrustForcedInterleavingRejectVsGradeRejectedNeverRevived`).
- Response 201 `AgentResponseGradeResult`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior); 404 `not_found` (`response`); 409 `agent_context_disabled`, `agent_context_response_not_gradable` (response `Rejected` or `Superseded`, `GradeRejectedOrSupersededResponse409ResponseNotGradable`), `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator exhausted after four attempts, or the dispatched EvaluateStalenessCommand exhausted); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

The save takes a `grd-` key from ReadableKeyAllocator and stores the grade with its findings and the thresholds in force, with `RecordedUtc` set by the server; ScoreVerdict applies `max(configured, 0.98)` and zero FAIL and UNKNOWN findings.

```mermaid
flowchart TD
    S(["Handler runs: POST grades/deterministic"]) --> F{"Response found and gradable?"}
    F -->|"not found"| E2(["404 not_found response"])
    F -->|"Rejected or Superseded"| E3(["409 agent_context_response_not_gradable"])
    F -->|yes| CL[["Each evidence item (nested: CheckOneEvidenceItem)"]]
    CL --> SEC[["SecretRedactor.ContainsSecret: content, claims, refs"]]
    SEC --> EV["Dispatch EvaluateStalenessCommand for it"]
    EV -->|"409 conflict, retryable true"| EC(["409 conflict, retryable true"])
    EV -->|evaluated| SC["Staleness finding, exact scores"]
    SC --> VD[["ScoreVerdict: max of configured and 0.98"]]
    VD --> SW[["SerializableWrite, locking the response"]]
    SW -->|retries exhausted| EC
    subgraph TX [Delegate of SerializableWrite]
        RC{"Response as the reads saw it?"}
        SV[["Save the grade and its findings"]]
        TR["Dispatch DecideAgentResponseTrustCommand"]
    end
    SW -->|attempt| RC
    RC -->|"changed: attempt ends, read again"| F
    RC -->|same| SV
    SV -->|"key collision: attempt ends"| SW
    SV --> TR
    TR -->|committed| R(["201 grade with the response status"])
```

##### Nested activity: CheckOneEvidenceItem (loop body of RunDeterministicGradeCommand)

```mermaid
flowchart TD
    S(["Next evidence item of the claim"]) --> K{"Evidence kind"}
    K -->|"File, Requirement, Receipt, or SessionLogTurn"| H{"Server-computed Sha256 present?"}
    H -->|"no: the existing source could not be read at submission"| UF["Finding UNKNOWN"]
    H -->|yes| C{"CitedSha256 absent or equal to Sha256?"}
    C -->|yes| PF["Finding PASS"]
    C -->|no| FF["Finding FAIL"]
    K -->|Commit| AN[["SourceResolver, kind Commit: Ref against the response's CommitSha"]]
    AN -->|"workspace not a git checkout"| UF
    AN -->|"equal to it or one of its ancestors"| PF
    AN -->|"missing or unrelated commit"| FF
    PF --> NX(["Finding recorded: continue with the next item"])
    FF --> NX
    UF --> NX
```

#### RecordModelGradeCommand: record a model grade from an HV review [FR-MCP-AGENTCONTEXT-004]

- Handler: `RecordModelGradeCommand(string WorkspacePath, string ResponseId, RecordModelGradeRequest Request) : ICommand<AgentResponseGradeResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/RecordModelGradeCommand.cs` and `RecordModelGradeCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/RecordModelGradeCommandHandler.cs`.
- Caller: the HV agent (the configured grader, launched as every HV is), in two steps of one reviewer thread, because a `codex exec` run cannot hold its own completed-turn receipt while it is still running. Step 1, the review: the HV runner launches the reviewer (`codex exec -m <RequiredModel> -c model_reasoning_effort="<RequiredEffort>" --json -`), which reviews the turns of an HV request (CreateAgentContextHvRequestCommand), emits its `=== VERDICT JSON ===`, and stores its entire result on its own session-log turn; when the run completes the runner freezes the request, response, and rollout jsonl receipts (section 6.3). Step 2, the grade-only continuation: the runner resumes the same thread (`codex exec resume <reviewerThreadId> -m <RequiredModel> -c model_reasoning_effort="<RequiredEffort>" --json -`) with a prompt naming the frozen receipt paths, and the HV agent submits one grade per item through `New-McpAgentContextRecordModelGradeParams`, copied from its own verdict; the continuation reviews nothing and changes no receipt. A continuation that fails or is interrupted is resumed again with the same prompt: a resubmitted grade equal to the stored one returns it (200), so retries are safe (`ModelGradeTwoStepProtocolReviewThenContinuationSubmits`, `ModelGradeRetrySameSubmission200Existing`). There is no grading worker or HV pipeline in this effort (operator, 2026-10-11): the runner is the existing HV runner and both steps run the configured grader.
- REST `POST /qbrainai/agent-context/responses/{responseId}/grades/model`; MCP `agent_context_record_model_grade(string workspacePath, string responseId, string gradeJson)`; client `RecordModelGradeAsync(responseId, RecordModelGradeRequest)`; REPL `client.AgentContext.RecordModelGradeAsync`; PowerShell `New-McpAgentContextRecordModelGradeParams` (also builds the request from the reviewer's verdict: the scores and findings of its `grades` entry for the response copied exactly, the reviewer's own session-log turn ids, its thread id, and the request, response, and rollout jsonl paths); tool descriptor `agent_context_record_model_grade`.
- Response 201 `AgentResponseGradeResult` with `hvRequestStatus` (`Completed` once no item of the HV request is `Pending`), or 200 with the stored grade for a repeated equal submission.
- Order: the score lexemes are validated at deserialization by `ScoreLexemeJsonConverter` (ScoreLexeme, section 3.5.4), before the request pipeline and any lookup, and the REPL's lossless normalizer keeps the lexemes of `correctness` and `completeness` unchanged on the way, so `verdict_match` compares the reviewer's own lexemes. The stored-grade lookup (IdempotentCreate on the natural key `ResponseId`, `HvRequestId`, kind `Model`, policy `Conflict`, fingerprint `PayloadSha256` of the request's client inputs) runs read-only before the status, HV request, and item checks, the receipt reads, and the staleness dispatch, and again as the delegate's first step, so a retry after the request completed or the response was rejected still returns the stored grade. The request's `gradedUtc` is provenance only; the server stamps `RecordedUtc`, which orders grades (`ModelGradeClientGradedUtcInFutureIgnoredServerStampedNewerFailBecomesLatest`).
- Reads before the write (section 3.1, Database-only delegates; nested activity ReadModelGradeInputs): with no transaction open the unit checks the response, the findings' claims, the HV request, and the response's item in it, reads the receipts and the reviewer's turn for the eight checks of section 6.3, then dispatches EvaluateStalenessCommand for the response through `IDispatcher` (section 3.1: every read-only check comes first, the receipt, reviewer, and runtime-identity checks included, and only EvaluateStalenessCommand persists `Stale`, so the trust decision reads the status it left), and derives the verdict through ScoreVerdict. The SerializableWrite that locks the response then runs the key lookup again, re-checks that the response's status and `StatusVersion` (read after the dispatch), the HV request's status, and the item's disposition are those the reads saw (a change ends the attempt and the unit reads again within the bound of four attempts, `ModelGradeHvRequestCompletedDuringReadsReadsAgain409Closed`), inserts the grade, marks the item `Graded` and the HV request `Completed` with `CompletedUtc` when no item stays `Pending`, and dispatches the trust decision in the same transaction.
- Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; malformed findings, or a finding whose claimId the response does not have), `review_score_format`, `review_score_precision`, `review_score_out_of_range`, `agent_context_receipt_paths_required` (`ModelGradeMissingReceiptPathsOrReviewerIds400ReceiptPathsRequired`); 403 `agent_context_grader_not_allowed` (`ModelGradeOtherGrader403GraderNotAllowed`); 404 `not_found` (`response`, `hv_request`); 409 `agent_context_disabled`, `conflict` with `retryable: false` (a different grade already stored for this response and HV request), `agent_context_response_not_gradable` (`GradeRejectedOrSupersededResponse409ResponseNotGradable`), `agent_context_grade_not_in_hv_request` (`ModelGradeResponseNotItemOfHvRequest409GradeNotInHvRequest`), `agent_context_hv_request_closed` (`ModelGradeCompletedHvRequest409HvRequestClosed`), `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator exhausted after four attempts, or the dispatched EvaluateStalenessCommand exhausted); 422 `agent_context_receipt_unverified`; REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

The handler's validation checks that `hvRequestId`, the request, response, and rollout jsonl paths, `reviewerSessionId`, `reviewerRequestId`, and `reviewerThreadId` are present (otherwise `agent_context_receipt_paths_required`), that every finding has a claimId and a check with outcome PASS, FAIL, or UNKNOWN (otherwise `validation_error`), and that the grader agent, model, and effort equal the configured grader. The insert takes a `grd-` key from ReadableKeyAllocator through IdempotentCreate and stores the grade with `hvRequestId`, its findings, the receipt paths and hashes, the thresholds in force, and `RecordedUtc`. The 200 is a retry of the continuation; the 422 names the failed check. One decision holds the three request checks (receipt fields, findings, and the configured grader agent, model, and effort), each refusal on its own edge. The reads are drawn as one call of the nested activity ReadModelGradeInputs and the SerializableWrite with its delegate as one call of the nested activity SaveModelGrade, both below, whose edges are their returns. One 409 `conflict` terminal stands for both conflict answers: `retryable: false` for a different grade already stored for this response and HV request, and `retryable: true` when SerializableWrite or ReadableKeyAllocator is exhausted after four attempts.

```mermaid
flowchart TD
    S(["Body deserialized: POST grades/model"]) --> LX[["ScoreLexemeJsonConverter: both scores"]]
    LX -->|invalid lexeme| E2(["400 review_score_format, precision, or out_of_range"])
    LX -->|"valid: pipeline, then the handler"| V{"Receipt fields, findings, and grader valid?"}
    V -->|"a receipt field missing"| E5(["400 agent_context_receipt_paths_required"])
    V -->|"a finding malformed"| E6(["400 validation_error"])
    V -->|"not the configured grader"| E7(["403 agent_context_grader_not_allowed"])
    V -->|yes| ICR[["IdempotentCreate lookup, read-only"]]
    ICR -->|"stored, equal submission"| EXR(["200 the stored grade"])
    ICR -->|"another grade stored"| E14(["409 conflict, retryable only when exhausted"])
    ICR -->|absent| RM[["ReadModelGradeInputs, no transaction open"]]
    RM -->|refused| ER(["Refusal of ReadModelGradeInputs, drawn there"])
    RM -->|"checks passed, verdict derived"| SM[["SaveModelGrade"]]
    SM -->|"stored, equal submission"| EXR
    SM -->|"another grade stored, or retries exhausted"| E14
    SM -->|"changed: read again"| ICR
    SM -->|committed| R(["201 grade, response status, hvRequestStatus"])
```

##### Nested activity: ReadModelGradeInputs (reads of RecordModelGradeCommand, before its SerializableWrite)

No transaction is open. The gradable check runs in this order: the response is neither `Rejected` nor `Superseded`, the HV request is `Open`, and the response is an item of it. The eight checks are `receipt_path`, `request_meta`, `verdict_block`, `verdict_binding`, `verdict_match`, `reviewer_turn`, `reviewer_independent`, and `runtime_identity` (section 6.3); they read the frozen receipt files and the reviewer's own session-log turn and compare normal forms (ContentHasher, `ModelGradeVerdictWithEmDashReviewerTurnMatchesInNormalForm`). After every read-only check, the receipt, reviewer, and runtime-identity checks included (section 3.1, Database-only delegates), the dispatched EvaluateStalenessCommand persists `Stale` when a dependency changed; a `Stale` response stays gradable and DecideAgentResponseTrustCommand leaves it `Stale`. ScoreVerdict counts the PASS, FAIL, and UNKNOWN findings at `max(configured, 0.98)`. The diagram draws the response, claim, HV request, and item checks as one call of the nested activity CheckGradeTarget below.

```mermaid
flowchart TD
    S(["ReadModelGradeInputs: no transaction open"]) --> CT[["CheckGradeTarget"]]
    CT -->|refused| ERT(["Refusal of CheckGradeTarget, drawn there"])
    CT -->|"an item of an Open HV request"| RV{"The eight receipt checks pass?"}
    RV -->|"a check fails"| E13(["422 agent_context_receipt_unverified"])
    RV -->|"all eight pass"| EV["Dispatch EvaluateStalenessCommand for it"]
    EV -->|"409 conflict, retryable true"| EC(["409 conflict, retryable true"])
    EV -->|evaluated| VD[["ScoreVerdict over the findings"]]
    VD --> R(["Checks passed, verdict derived"])
```

##### Nested activity: CheckGradeTarget (response, claim, HV request, and item checks of ReadModelGradeInputs)

No transaction is open. The checks run in this order: the response exists in the workspace, every finding's claimId names a claim of the response, the HV request exists in the workspace, and the response is gradable in it (neither `Rejected` nor `Superseded`, the HV request `Open`, and the response an item of it).

```mermaid
flowchart TD
    S(["CheckGradeTarget: no transaction open"]) --> F{"Response in the workspace?"}
    F -->|no| E8(["404 not_found response"])
    F -->|yes| CK{"Each claimId a claim of it?"}
    CK -->|no| E6(["400 validation_error"])
    CK -->|yes| HR{"HV request in the workspace?"}
    HR -->|no| E10(["404 not_found hv_request"])
    HR -->|yes| GQ{"Gradable in that HV request?"}
    GQ -->|"Rejected or Superseded"| E9(["409 agent_context_response_not_gradable"])
    GQ -->|"HV request Completed"| E11(["409 agent_context_hv_request_closed"])
    GQ -->|"not an item"| E12(["409 agent_context_grade_not_in_hv_request"])
    GQ -->|yes| R(["Gradable: an item of an Open HV request"])
```

##### Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs)

The SerializableWrite locks the response. Its delegate reads and writes only the database: it runs the key lookup again, re-checks that the response's status and `StatusVersion` (read after the staleness dispatch), the HV request's status, and the item's disposition are those the reads saw (a change ends the attempt and the unit reads again, within the bound of four attempts), inserts the grade with a `grd-` key from ReadableKeyAllocator, marks the item `Graded` and the HV request `Completed` with `CompletedUtc` when no item stays `Pending`, and dispatches DecideAgentResponseTrustCommand in the same transaction.

```mermaid
flowchart TD
    S(["SaveModelGrade: checks passed, verdict derived"]) --> SW[["SerializableWrite, locking the response"]]
    SW -->|retries exhausted| EC(["409 conflict, retryable true"])
    subgraph TX [Delegate of SerializableWrite]
        IC[["IdempotentCreate lookup"]]
        RC{"Response, HV request, item as read?"}
        INS[["Insert the grade, item Graded"]]
        TR["Dispatch DecideAgentResponseTrustCommand"]
    end
    SW -->|attempt| IC
    IC -->|"stored, equal submission"| EXR(["200 the stored grade"])
    IC -->|"another grade stored"| E14(["409 conflict, retryable false"])
    IC -->|absent| RC
    RC -->|"changed: attempt ends"| RA(["Returned: read again"])
    RC -->|same| INS
    INS -->|"key collision: attempt ends"| SW
    INS --> TR
    TR -->|committed| R(["201 grade, response status, hvRequestStatus"])
```

#### DecideAgentResponseTrustCommand: decide trust (internal, after every grade) [FR-MCP-AGENTCONTEXT-004]

- Handler: `DecideAgentResponseTrustCommand(string WorkspacePath, string ResponseId) : ICommand<AgentResponseTrustDecision>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/DecideAgentResponseTrustCommand.cs` and `DecideAgentResponseTrustCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/DecideAgentResponseTrustCommandHandler.cs`; internal, dispatched through `IDispatcher.SendAsync` by the RunDeterministicGradeCommand and RecordModelGradeCommand handlers inside their SerializableWrite transaction, with the response already locked, never by a surface. It is not gated (it does not implement `IModeGatedRequest`) and runs no transaction of its own, so it has no conflict exit: its caller's SerializableWrite retries the whole delegate.
- Trigger: the two grade handlers dispatch it after their grade is saved, in the same transaction. No endpoint.
- Rule: a response is `Trusted` only when it is fresh and neither `Rejected`, `Stale`, nor `Superseded`, and the verdicts of both its latest deterministic grade and its latest model grade ("latest" by the server-stamped `RecordedUtc`, then `GradeId`, section 6.3) are `Pass`; otherwise it is `Grading`. It never reads a stored verdict: ScoreVerdict re-derives each latest grade's verdict from that grade's stored scores and finding counts against `max(configured now, 0.98)`, so no stored verdict or earlier threshold below the floor can trust a response, and a raised threshold demotes a `Trusted` response at its next decision (`TrustStoredThresholdBelowFloorFloorApplied`, `TrustThresholdRaisedAfterGradeAppliedAtNextDecision`). It reads no source and persists no `Stale` (section 3.1: a SerializableWrite delegate does no git, file, or source read, and only EvaluateStalenessCommand persists `Stale`): the dispatching grade unit ran EvaluateStalenessCommand for the response before its SerializableWrite, and a response that is then `Stale`, `Rejected`, or `Superseded` is not decided and keeps its status, so a dependency that changed after the deterministic grade and before the model grade makes the response `Stale`, never `Trusted` (`TrustDependencyChangedBeforeModelGradeStaleNotTrusted`). The status change and its projection-row side effect go through ResponseStatusTransition in the grade's transaction, so the stored status always reflects the latest committed grade pair, and a concurrent reject, supersede, or staleness change on the same response is ordered before or after it, never interleaved.

```mermaid
flowchart TD
    S(["Dispatched in a grade unit's delegate, response locked"]) --> ST{"Stored status Stale, Rejected, or Superseded?"}
    ST -->|"yes: not decided"| DONE(["AgentResponseTrustDecision returned, status unchanged"])
    ST -->|no| LG["Read the latest deterministic and model grades"]
    LG --> SV[["ScoreVerdict: re-derive at max of configured now and 0.98"]]
    SV --> D{"Both grades exist, both verdicts Pass?"}
    D -->|"yes: target Trusted"| TR[["ResponseStatusTransition to the target"]]
    D -->|"no: target Grading"| TR
    TR --> DONE2(["AgentResponseTrustDecision returned"])
```

#### RejectAgentResponseCommand: reject a response [FR-MCP-AGENTCONTEXT-005]

- Handler: `RejectAgentResponseCommand(string WorkspacePath, string ResponseId, RejectAgentResponseRequest Request) : ICommand<AgentResponseResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/RejectAgentResponseCommand.cs` and `RejectAgentResponseCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/RejectAgentResponseCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/responses/{responseId}/reject`; MCP `agent_context_reject_response(workspacePath, responseId, reason)`; client `RejectResponseAsync(responseId, RejectAgentResponseRequest)`; REPL `client.AgentContext.RejectResponseAsync`; PowerShell `New-McpAgentContextRejectResponseParams`; tool descriptor `agent_context_reject_response`.
- Transition: ResponseStatusTransition to `Rejected` with the reason, inside a SerializableWrite that locks the response, with every side effect in the same transaction (the response's `Pending` items in `Open` HV requests become `Closed` with reason `rejected`, each request left with no `Pending` item becomes `Completed` with `CompletedUtc`, and the projection row is marked). A repeat on a `Rejected` response is the same transition and returns 200 unchanged. A reject and a supersede of the same response commit one after the other: the second is refused with 409 `agent_context_transition_invalid` and the stored status is the first one's (`LifecycleForcedInterleavingSupersedeVsRejectSameOriginalOneWinsOtherRefused`, on the three providers).
- Response 200 `AgentResponseResult` with status `Rejected`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; blank reason); 404 `not_found` (`response`); 409 `agent_context_disabled`, `agent_context_transition_invalid` (the response is `Superseded`), `conflict` with `retryable: true` (SerializableWrite exhausted after four attempts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`. The unit reads only the database, so its whole check runs in the delegate.

```mermaid
flowchart TD
    S(["Handler runs: POST reject"]) --> V{"Reason nonblank?"}
    V -->|no| E2(["400 validation_error"])
    V -->|yes| SW[["SerializableWrite, locking the response"]]
    SW -->|retries exhausted| EC(["409 conflict, retryable true"])
    subgraph TX [Delegate of SerializableWrite]
        F{"Response found in the workspace?"}
        RT[["ResponseStatusTransition to Rejected, with the reason"]]
    end
    SW -->|attempt| F
    F -->|no| E3(["404 not_found response"])
    F -->|yes| RT
    RT -->|"already Rejected: unchanged"| SAME(["200 AgentResponseResult, unchanged"])
    RT -->|"Superseded: lifecycle refuses"| E4(["409 agent_context_transition_invalid"])
    RT -->|"transition saved, committed"| OK(["200 AgentResponseResult status Rejected"])
```

#### SupersedeAgentResponseCommand: supersede a response [FR-MCP-AGENTCONTEXT-005]

- Handler: `SupersedeAgentResponseCommand(string WorkspacePath, string ResponseId, SupersedeAgentResponseRequest Request) : ICommand<AgentResponseResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/SupersedeAgentResponseCommand.cs` and `SupersedeAgentResponseCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/SupersedeAgentResponseCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/responses/{responseId}/supersede`; MCP `agent_context_supersede_response(workspacePath, responseId, supersededByResponseId, reason)`; client `SupersedeResponseAsync(responseId, SupersedeAgentResponseRequest)`; REPL `client.AgentContext.SupersedeResponseAsync`; PowerShell `New-McpAgentContextSupersedeResponseParams`; tool descriptor `agent_context_supersede_response`.
- Transition: one SerializableWrite locks both responses in ascending `ResponseId` order, so a concurrent reject of either commits before or after it; inside it, ResponseStatusTransition moves the original to `Superseded` with `SupersededByResponseId` (its `Pending` HV items become `Closed` with reason `superseded`, emptied requests become `Completed` with `CompletedUtc`, and the projection row is marked), and RelationshipWriter writes the `supersedes` link (`CreatedBy` `SupersedeAgentResponseCommand`, so it is protected) with its cycle rule over the `supersedes` links, the one data source of the supersede chain. A refusal by either rolls back both. A repeat naming the same replacement is the same transition and returns 200 unchanged; the first supersession stays on record and no `Rejected` response becomes `Superseded`.
- Response 200 `AgentResponseResult` with status `Superseded` (`SupersedeRepeatSameReplacementReturns200Unchanged`). Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; self-reference, blank reason); 404 `not_found` (`response` for either response); 409 `agent_context_disabled`, `agent_context_supersede_invalid` (a different request, the replacement `Rejected` or `Superseded`, or a link that would close a supersede cycle, `SupersedeOtherRequestInvalidReplacementOrCycle409SupersedeInvalid`), `agent_context_transition_invalid` (the original `Rejected`, or already superseded by another response: `SupersedeOriginalRejected409TransitionInvalid`, `SupersedeOriginalSupersededByOther409TransitionInvalid`), `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator exhausted after four attempts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`. The unit reads only the database (the `supersedes` link's target is the original response, whose hash is its stored `ContentSha256`, section 6.3, which the delegate reads and passes to RelationshipWriter before the link's write, section 3.1), so its whole check runs in the delegate.

In the diagram a valid replacement has the same `requestId` as the original and is neither `Rejected` nor `Superseded`, and the transition node applies the side effects of the Transition bullet. One decision checks, in this order, that both responses exist in the workspace and that the replacement is valid for the original, each refusal on its own edge.

```mermaid
flowchart TD
    S(["Handler runs: POST supersede"]) --> V{"Replacement differs, reason nonblank?"}
    V -->|no| E2(["400 validation_error"])
    V -->|yes| SW[["SerializableWrite, locking both by ResponseId"]]
    SW -->|retries exhausted| EC(["409 conflict, retryable true"])
    subgraph TX [Delegate of SerializableWrite]
        F{"Both found, replacement valid?"}
        RT[["ResponseStatusTransition: original to Superseded"]]
        RW[["RelationshipWriter: supersedes, cycle rule"]]
    end
    SW -->|attempt| F
    F -->|"a response not found"| E3(["404 not_found response"])
    F -->|"replacement not valid"| E4(["409 agent_context_supersede_invalid"])
    F -->|yes| RT
    RT -->|"already by this replacement"| SAME(["200 response, unchanged"])
    RT -->|"Rejected, or by another response"| E5(["409 agent_context_transition_invalid"])
    RT -->|saved| RW
    RW -->|"would close a cycle"| E4
    RW -->|committed| OK(["200 response, status Superseded"])
```

#### EvaluateStalenessCommand: evaluate staleness [FR-MCP-AGENTCONTEXT-005]

- Handler: `EvaluateStalenessCommand(string WorkspacePath, EvaluateStalenessRequest Request) : ICommand<EvaluateStalenessResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/EvaluateStalenessCommand.cs` and `EvaluateStalenessCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/EvaluateStalenessCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/staleness/evaluate`; MCP `agent_context_evaluate_staleness(workspacePath, responseIdsJson)`; client `EvaluateStalenessAsync(EvaluateStalenessRequest)`; REPL `client.AgentContext.EvaluateStalenessAsync`; PowerShell `New-McpAgentContextEvaluateStalenessParams`; tool descriptor `agent_context_evaluate_staleness`.
- Rule: IResponseStalenessRule (section 3.5.5), owned by ResponsesAndTrust: the `depends_on`, `derived_from`, and `supports` targets of the response and of its package, the response's evidence (including evidence outside its package), and every cited requirement are its dependencies, except the request's own turn and a `Rule` file outside version control; a file dependency is re-hashed at the workspace's current HEAD commit, and a dependency whose source no longer exists, such as a deleted requirement, has changed (`StalenessResponseOnlyEvidenceChangedMarksStale`, `StalenessUncommittedEditNotStaleCommittedEditStale`, `StalenessOwnTurnCompletedResponseNotStale`, `StalenessServerRestartRegeneratesMarkerTrustedResponseStaysTrusted`). This unit is the one persister of `Stale` (section 3.1, Database-only delegates): per response it evaluates the rule read-only with no transaction open (the file reads at the HEAD commit happen there), and only for a stale response runs one SerializableWrite that locks it, re-checks that its status and `StatusVersion` are those the evaluation saw (a change ends the attempt and the response is evaluated again, within the bound of four attempts), and persists `Stale` through ResponseStatusTransition.
- Callers: the surfaces; ProjectOneResponse (ContextGraph) and the two grade units (RunDeterministicGradeCommand, RecordModelGradeCommand), which dispatch it through `IDispatcher` for their response before they rely on the status and before their own SerializableWrite (a grade unit after every read-only check it has, section 3.1); and `RequirementsDatabaseDocumentService` (the database `IRequirementsDocumentService`), which after the commit of a write that changes or deletes a requirement or its acceptance criteria dispatches it with the `responseIds` of the responses citing a changed requirement, and after a change of the current requirement layer dispatches it with none, only while Mode is not `Off`, logging a failed dispatch without failing the requirement write (section 3.1, Requirement writes and staleness; section 3.5.1; this updates TR-MCP-REQRECOVERY-001 and the TRs of those requirement writes, which gain this side effect; `RequirementUpdateAfterCommitDispatchesStalenessForCitingResponses`, `RequirementWriteStalenessDispatchFailureLoggedWriteSucceeds`). DecideAgentResponseTrustCommand reads the status the grade unit's dispatch left and evaluates no source. GetAgentContextStatusQuery, BuildAgentContextPackageCommand (for its prior-response candidates), GetAgentResponseQuery, QueryAgentResponsesQuery, RetrieveAgentContextCandidatesQuery, and GetAgentContextPilotReportQuery evaluate IResponseStalenessRule read-only and write nothing (queries are side-effect free, `src/QBrainAi.Cqrs/IQuery.cs:5`), so no context edits another context's files.
- Selection (ResponseIdList): the listed ids, or every `Submitted`, `Grading`, and `Trusted` response when none is listed; an id that names no response in the workspace is returned in `notFound`, is not counted in `evaluatedCount`, and raises no error (`EvaluateStalenessUnknownIdsNotCountedNoError`).
- Response 200 `EvaluateStalenessResult { evaluatedCount, newlyStale, notFound }`. When one response's write exhausts its retries the unit stops and returns 409 `conflict` with `retryable: true`; responses already marked stay marked, and a retry evaluates again. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; malformed list, more than 1000 ids, an id without the `rsp-` prefix); 409 `agent_context_disabled`, `conflict` with `retryable: true` (SerializableWrite exhausted after four attempts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

```mermaid
flowchart TD
    S(["Handler runs: POST staleness/evaluate"]) --> RL[["ResponseIdList: validate and select, none listed selecting every Submitted, Grading, and Trusted response"]]
    RL -->|"malformed, more than 1000, or an id without rsp-"| E2(["400 validation_error"])
    RL -->|"selected, unknown ids in notFound"| L[["For each selected response (nested: EvaluateOneResponseStaleness)"]]
    L -->|every response evaluated| R(["200 EvaluateStalenessResult: evaluatedCount, newlyStale, notFound"])
    L -->|"a write exhausted its retries"| EC(["409 conflict, retryable true"])
```

##### Nested activity: EvaluateOneResponseStaleness (loop body of EvaluateStalenessCommand)

The rule reads files at the workspace HEAD commit with no transaction open; the transition records the changed reference and its relationship and marks the projection row (ResponseStatusTransition, section 3.5.5).

```mermaid
flowchart TD
    S(["Next selected response"]) --> RU[["IResponseStalenessRule, read-only, no transaction"]]
    RU -->|"fresh, or not evaluated"| NX(["Continue with the next response"])
    RU -->|stale| SW[["SerializableWrite, locking the response"]]
    SW -->|retries exhausted| EX(["Stop the loop: the unit returns 409 conflict, retryable true"])
    subgraph TX [Delegate of SerializableWrite]
        RC{"Response as the evaluation saw it?"}
        MK[["ResponseStatusTransition to Stale"]]
    end
    SW -->|attempt| RC
    RC -->|"changed: attempt ends, evaluate again"| RU
    RC -->|same| MK
    MK -->|"committed, added to newlyStale"| NX
```

#### CreateAgentContextRelationshipCommand: create a relationship [FR-MCP-AGENTCONTEXT-006]

- Handler: `CreateAgentContextRelationshipCommand(string WorkspacePath, CreateAgentContextRelationshipRequest Request) : ICommand<AgentContextRelationshipResult>` in `src/QBrainAi.Services/AgentContext/Graph/Commands/CreateAgentContextRelationshipCommand.cs` and `CreateAgentContextRelationshipCommandHandler` in `src/QBrainAi.Services/AgentContext/Graph/Commands/CreateAgentContextRelationshipCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/relationships`; MCP `agent_context_create_relationship(workspacePath, relationshipJson)`; client `CreateRelationshipAsync(CreateAgentContextRelationshipRequest)`; REPL `client.AgentContext.CreateRelationshipAsync`; PowerShell `New-McpAgentContextCreateRelationshipParams`; tool descriptor `agent_context_create_relationship`.
- The node kinds are `SourceKind` wire values, mapped by SourceResolver (`Response` is `PriorResponse`, `CodeFile` is the kind of `Code` items and `File` evidence), and both refs are resolved through it, with the target's `ToSha256`, before SerializableWrite (section 3.1, Database-only delegates), and RelationshipWriter receives that hash (section 3.1: it takes the target hash its caller read before the write), so the delegate holds only RelationshipWriter's database work; a ref that names no existing source is 400 `validation_error` with `details.field` `fromRef` or `toRef`, as in every unit. RelationshipWriter writes the link with `CreatedBy` the calling agent, so a client-made link stays deletable; a client cannot create type `supersedes` (400 `validation_error`, `details.field` `type`), because only SupersedeAgentResponseCommand writes the supersede chain. Two concurrent identical creates: the loser's insert hits the link's unique index, SerializableWrite runs the delegate again, and RelationshipWriter returns the winner's link with `created: false` (`CreateRelationshipConcurrentIdenticalOne201OneCreatedFalse`).
- Response 201 with `created: true`, or 200 with `created: false` for an identical stored link (a soft-deleted identical link is revived, keeps its `RelationshipId`, and is written anew with `created: true`, IdempotentCreate, section 3.5.2 and section 6.3, `CreateRelationshipAfterDeleteRevivedSameRelationshipIdCreatedTrue`). Errors: 400 `agent_context_relationship_invalid` (an unknown kind or type, or a self-link, `CreateRelationshipUnknownKindTypeOrSelfLink400RelationshipInvalid`), `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; type `supersedes`; a ref that names no existing source); 409 `agent_context_disabled`, `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator `rel-` exhausted after four attempts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

```mermaid
flowchart TD
    S(["Handler runs: POST relationships"]) --> V{"Known kinds and type, and not a self-link?"}
    V -->|no| E2(["400 agent_context_relationship_invalid"])
    V -->|yes| RS[["SourceResolver: fromRef, toRef, ToSha256 of the target"]]
    RS -->|"a ref names no existing source"| E3(["400 validation_error, details.field fromRef or toRef"])
    RS -->|both resolve| SW[["SerializableWrite, database only"]]
    SW -->|retries exhausted| EC(["409 conflict, retryable true"])
    subgraph TX [Delegate of SerializableWrite]
        RW[["RelationshipWriter, createdBy the caller, key rel-"]]
    end
    SW -->|attempt| RW
    RW -->|"type supersedes"| E4(["400 validation_error, details.field type"])
    RW -->|"stored identical link"| SAME(["200 the stored link, created false"])
    RW -->|"new link, committed"| OK(["201 link, created true"])
```

#### DeleteAgentContextRelationshipCommand: delete a relationship [FR-MCP-AGENTCONTEXT-006]

- Handler: `DeleteAgentContextRelationshipCommand(string WorkspacePath, string RelationshipId) : ICommand<bool>` in `src/QBrainAi.Services/AgentContext/Graph/Commands/DeleteAgentContextRelationshipCommand.cs` and `DeleteAgentContextRelationshipCommandHandler` in `src/QBrainAi.Services/AgentContext/Graph/Commands/DeleteAgentContextRelationshipCommandHandler.cs`.
- REST `DELETE /qbrainai/agent-context/relationships/{relationshipId}`; MCP `agent_context_delete_relationship(workspacePath, relationshipId)`; client `DeleteRelationshipAsync(relationshipId)`; REPL `client.AgentContext.DeleteRelationshipAsync`; PowerShell `New-McpAgentContextDeleteRelationshipParams`; tool descriptor `agent_context_delete_relationship`.
- Protection (RelationshipWriter): every link a command created is protected, because IResponseStalenessRule reads it, it holds the supersede chain, or it records the package's turn: Build's `derived_from` and `relevant_to` (`CreatedBy` `BuildAgentContextPackageCommand`, `DeleteRelationshipBuildMadeDerivedFrom409Protected`), Submit's `derived_from` and `supports` (`DeleteRelationshipServerMadeDerivedFrom409Protected`, `DeleteRelationshipServerMadeSupports409Protected`), and Supersede's `supersedes` (`DeleteRelationshipSupersedeMadeSupersedes409Protected`); a client-made link of any type can be deleted. The protection reads only the immutable `CreatedBy`, so the unit needs no SerializableWrite. The delete is a soft delete (`McpDbContext`, TR-MCP-DB-003), and a later identical create revives the link (section 3.5.2).
- Response 204. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior); 404 `not_found` (`relationship`); 409 `agent_context_disabled`, `agent_context_relationship_protected`; REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

```mermaid
flowchart TD
    S(["Handler runs: DELETE relationship"]) --> F{"Link found in the workspace?"}
    F -->|no| E2(["404 not_found relationship"])
    F -->|yes| PRT[["RelationshipWriter protection: CreatedBy names a command?"]]
    PRT -->|protected| E4(["409 agent_context_relationship_protected"])
    PRT -->|client-made| D["Soft-delete the link"]
    D --> OK(["204"])
```

#### QueryAgentContextRelationshipsQuery: query relationships [FR-MCP-AGENTCONTEXT-006]

- Handler: `QueryAgentContextRelationshipsQuery(string WorkspacePath, AgentContextRelationshipQuery Query) : IQuery<PageResult<AgentContextRelationshipDto>>` in `src/QBrainAi.Services/AgentContext/Graph/Queries/QueryAgentContextRelationshipsQuery.cs` and `QueryAgentContextRelationshipsQueryHandler` in `src/QBrainAi.Services/AgentContext/Graph/Queries/QueryAgentContextRelationshipsQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/relationships?nodeKind&nodeRef&type&direction&limit&offset`; MCP `agent_context_query_relationships(string workspacePath, string? nodeKind = null, string? nodeRef = null, string? type = null, string direction = "both", int limit = 50, int offset = 0)`; client `QueryRelationshipsAsync(AgentContextRelationshipQuery)`; REPL `client.AgentContext.QueryRelationshipsAsync`; PowerShell `New-McpAgentContextQueryRelationshipsParams`; tool descriptor `agent_context_query_relationships`.
- Paging: PagedQuery with `PageBounds` `Refuse` (limit 1..1000, default 50, offset at least 0), stable order `CreatedUtc`, then `RelationshipId`.
- Not gated. Response 200 `PageResult<AgentContextRelationshipDto>`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; a node kind without a ref, an unknown kind, type, or direction; `limit` or `offset` outside the bounds, `details.field` naming it). REPL: a listed read, never refused by ClientMutationPolicy.

```mermaid
flowchart TD
    S(["Handler runs: GET relationships"]) --> V{"Filters valid: nodeKind with nodeRef, known kind, type, and direction?"}
    V -->|no| E2(["400 validation_error"])
    V -->|yes| DIR{"Direction"}
    DIR -->|out| Q1["Links where the node is From"]
    DIR -->|in| Q2["Links where the node is To"]
    DIR -->|both| Q3["Union of both"]
    Q1 --> PQ[["PagedQuery, PageBounds Refuse: filter by type, order CreatedUtc, then RelationshipId"]]
    Q2 --> PQ
    Q3 --> PQ
    PQ -->|"limit or offset outside the bounds"| E2
    PQ -->|page| R(["200 PageResult of AgentContextRelationshipDto"])
```

#### RetrieveAgentContextCandidatesQuery: retrieve candidate context [FR-MCP-AGENTCONTEXT-007]

- Handler: `RetrieveAgentContextCandidatesQuery(string WorkspacePath, RetrieveAgentContextCandidatesRequest Request) : IQuery<AgentContextRetrieveResult>` in `src/QBrainAi.Services/AgentContext/Packages/Queries/RetrieveAgentContextCandidatesQuery.cs` and `RetrieveAgentContextCandidatesQueryHandler` in `src/QBrainAi.Services/AgentContext/Packages/Queries/RetrieveAgentContextCandidatesQueryHandler.cs` (ContextPackages, with the relational path; the GraphRAG path is ContextGraph's, behind the retrieval seam).
- REST `POST /qbrainai/agent-context/retrieve`; MCP `agent_context_retrieve(workspacePath, requestJson)`; client `RetrieveAsync(RetrieveAgentContextCandidatesRequest)`; REPL `client.AgentContext.RetrieveAsync`; PowerShell `New-McpAgentContextRetrieveParams`; tool descriptor `agent_context_retrieve`. Also dispatched by BuildAgentContextPackageCommand with the package's `agent`, `sessionId`, `requestId`, and `focusRefs`, and `preferGraphRag` equal to `GraphRag:UseForRetrieval`.
- A named turn (`agent`, `sessionId`, and `requestId`, all or none) is found through TurnLocator; the named turn itself is never a candidate (section 3.1). When the request names a turn or `focusRefs`, the relational path ranks by explicit relationships to that turn, its TODO, its requirements, and the focus refs first (`RetrieveSameQueryTextTwoTurnNeighborhoodsDifferentRanking`); without them it ranks by the existing hybrid search (FTS5 BM25 with vector similarity) alone. Every `PriorResponse` candidate is evaluated with IResponseStalenessRule read-only before it is returned, so a response whose dependency changed is omitted and nothing is written (`RetrieveAfterEvidenceFileChangeResponseExcludedNothingWritten`). Both paths end in CandidateRanker (scores rounded half-even to six places, descending, then `sourceRef` by ordinal comparison) and the cut to `limit`.
- Path choice: `preferGraphRag` false takes the relational path with no `fallbackReason`; `preferGraphRag` true with `GraphRag:UseForRetrieval` false takes it with `fallbackReason` `retrieval_disabled` (`RetrievePreferGraphRagUseForRetrievalFalseRelationalWithRetrievalDisabled`). The GraphRAG path answers only the `PriorResponse` kind, because GraphRAG holds only projected responses: a request whose `kinds` is absent or names any other kind takes the relational path with `fallbackReason` `graphrag_kinds_unsupported`; otherwise it queries the projected responses in `GraphRagStorageScope.Workspace` only, drops a hit that names no response of this workspace, keeps one hit per response, and keeps the hits that are `Trusted` and fresh, scored by the GraphRAG score (`RetrieveGraphRagOtherKindsRequestedRelationalWithReason`, `RetrieveGraphRagOrderedByScoreThenSourceRefCutToLimit`, `ProjectionAndGraphRagRetrievalWorkspaceScopedNoCrossWorkspaceHit`, `RetrieveGraphRagDuplicateHitsForOneResponseReturnedOnce`). Any fallback uses the relational path and reports `fallbackReason` (`graphrag_disabled`, `graphrag_not_initialized`, `graphrag_not_indexed`, `index_in_progress`, `retrieval_disabled`, `graphrag_error`, or `graphrag_kinds_unsupported`).
- Not gated. Response 200 `AgentContextRetrieveResult`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; `trustedOnly: false` with kind `PriorResponse`; `limit` outside `PageBounds` `Refuse`, 1..1000, default 50, `details.field` `limit`; only some of `agent`, `sessionId`, and `requestId`; TurnLocator `InvalidIdentifier`); 404 `not_found` (`session_turn` for TurnLocator `SessionNotFound` or `TurnNotFound`, `RetrieveUnknownTurn404SessionTurn`). REPL: a listed read, never refused by ClientMutationPolicy. A query opens no transaction, so when Build dispatches it, its GraphRAG and source reads run before Build's SerializableWrite.

```mermaid
flowchart TD
    S(["Handler runs: POST retrieve"]) --> V{"Request valid: agent, sessionId, and requestId all or none, trustedOnly true for PriorResponse, limit 1..1000?"}
    V -->|no| E2(["400 validation_error"])
    V -->|yes| NT{"Turn named?"}
    NT -->|yes| TL[["TurnLocator: agent, sessionId, requestId"]]
    TL -->|InvalidIdentifier| E2
    TL -->|"SessionNotFound or TurnNotFound"| E3(["404 not_found session_turn"])
    TL -->|turn found| PG
    NT -->|no| PG{"preferGraphRag?"}
    PG -->|no| REL["Relational path: candidates other than the named turn"]
    PG -->|yes| UR{"GraphRag:UseForRetrieval?"}
    UR -->|no| FB0["fallbackReason retrieval_disabled"]
    FB0 --> REL
    UR -->|yes| KD{"kinds names only PriorResponse?"}
    KD -->|no| FB1["fallbackReason graphrag_kinds_unsupported"]
    FB1 --> REL
    KD -->|yes| GS["IGraphRagService.GetStatusAsync"]
    GS --> EN{"Enabled and indexed?"}
    EN -->|no| FB2["fallbackReason from the status"]
    FB2 --> REL
    EN -->|yes| GQ["IGraphRagService.QueryAsync over the projected responses in GraphRagStorageScope.Workspace"]
    GQ --> GQR{"Query result"}
    GQR -->|"FallbackUsed or exception"| FB3["fallbackReason from the result, or graphrag_error"]
    FB3 --> REL
    GQR -->|ok| MAPG["Keep hits naming a response of this workspace, one hit per response, scored by the GraphRAG score"]
    REL --> R1["Score: linked by relationships to the named turn and focus refs first, then the hybrid search score"]
    MAPG --> TF[["IResponseStalenessRule, read-only: keep PriorResponse candidates that are Trusted and fresh, nothing written"]]
    R1 --> TF
    TF --> RK[["CandidateRanker, then cut to limit"]]
    RK --> RES(["200 AgentContextRetrieveResult: retrievalPath, fallbackReason when a fallback happened, candidates"])
```

#### ProjectAgentResponsesCommand: project responses into GraphRAG [FR-MCP-AGENTCONTEXT-007]

- Handler: `ProjectAgentResponsesCommand(string WorkspacePath, ProjectAgentContextRequest Request) : ICommand<ProjectAgentResponsesResult>` in `src/QBrainAi.Services/AgentContext/Graph/Commands/ProjectAgentResponsesCommand.cs` and `ProjectAgentResponsesCommandHandler` in `src/QBrainAi.Services/AgentContext/Graph/Commands/ProjectAgentResponsesCommandHandler.cs`.
- REST `POST /qbrainai/agent-context/projection/graphrag`; MCP `agent_context_project_graphrag(workspacePath, responseIdsJson)`; client `ProjectToGraphRagAsync(ProjectAgentContextRequest)`; REPL `client.AgentContext.ProjectToGraphRagAsync`; PowerShell `New-McpAgentContextProjectGraphRagParams`; tool descriptor `agent_context_project_graphrag`. Also dispatched by the background pass after ResponseStatusTransition marks a projection row (entering or leaving `Trusted`): the unit whose transaction marked the row (the grade units through the trust decision, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand) dispatches this command with no `responseIds` through `IDispatcher` in its own scope after its SerializableWrite commits; a failed dispatch is logged and never fails that unit, and a row it leaves is selected by the next pass or an explicit call (section 3.1, Projection pass; `ProjectionPassDispatchedAfterTrustCommitFailureLoggedUnitSucceeds`). This command is gated, so the pass does not run while Mode is `Off`, and a document left in GraphRAG meanwhile never becomes trusted context, because the GraphRAG retrieval path keeps only `Trusted`, fresh hits.
- Selection (ResponseIdList, section 6.3): the rows of the listed ids in any state, or, with none listed, every `Pending` and `Failed` row and every `Skipped` row once ingest is enabled again; an id that names no response in the workspace is returned in `notFound` (`ProjectionUnknownResponseIdListedInNotFound`).
- Projection lifecycle (ProjectOneResponse; the row states are drawn in section 6.3): the response's staleness is persisted first by dispatching EvaluateStalenessCommand through `IDispatcher`, so a response whose dependency changed is removed, never projected (`ProjectionTrustedWithChangedDependencyRemovedNotProjected`). A `Trusted` response is ingested into `GraphRagStorageScope.Workspace` only, through `IGraphRagService.IngestTextAsync` with `content` the response content, `title` the `responseId`, `sourceType` `agent-response`, and `sourceKey` `agent-context/<responseId>` (`src/QBrainAi.GraphRag/Models/GraphRagModels.cs:184`), when its projected hash differs (this admits agent responses to GraphRAG, so it updates FR-MCP-130 AC 3, which admits only CuriosityEngine requests, to scope that rule to brain-slot output); with `GraphRag:ProjectionEnabled` false or GraphRAG disabled its row is `Skipped` with `skippedReason` `projection_disabled` or `graphrag_disabled` (`ProjectionProjectionDisabledSkippedReasonProjectionDisabled`). For a response that is not `Trusted`, a row that holds a `GraphRagDocumentId` (`Projected`, or `Failed` after a delete error) is deleted on this pass while GraphRAG is enabled, whatever `ProjectionEnabled` says (`ProjectionDisabledAfterProjectedRejectedResponseDocumentStillRemoved`, `ProjectionDeleteFailedRetriedNextPassDocumentRemoved`); while GraphRAG is disabled that row stays `Pending`, never `Skipped`, and is removed once GraphRAG is enabled (`ProjectionGraphRagDisabledRemovalStaysPendingRunsWhenEnabled`); a selected row with no document becomes `Removed` and is not selected again (`ProjectionPendingThenRejectedBeforeIngestRowRemovedNotReselected`). The row state is saved through SerializableWrite with the response locked, and the response status is re-read there, after the GraphRAG call, so the delegate reads and writes only the database (section 3.1, Database-only delegates): when the response left `Trusted` during an ingest, the row keeps the new document id and stays `Pending` for removal (`ProjectionForcedInterleavingRejectDuringIngestDocumentRemovedNextPass`, on the three providers); a GraphRAG error is `Failed` with `LastError`, keeping any document id. Passes can overlap (a surface call beside the background pass), and `IngestTextAsync` creates a new document on every call (`src/QBrainAi.GraphRag/Services/GraphRagService.cs:354` at `a8d3934b`), so the delegate also re-reads the row: a save that finds the row already `Projected` with another pass's document for the same `ProjectedContentSha256` keeps the stored one and deletes the document it ingested after the save, so a response has at most one projected document (`ProjectionOverlappingPassesOneDocumentKeptOtherDeleted`).
- Response 200 `ProjectAgentResponsesResult { projected, removed, skipped, failed, skippedReason, notFound }`; GraphRAG being disabled is not an error. When a save exhausts its retries the unit stops and returns 409 `conflict` with `retryable: true`; rows already saved stay saved. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; malformed list, more than 1000 ids, an id without the `rsp-` prefix); 409 `agent_context_disabled`, `conflict` with `retryable: true` (SerializableWrite exhausted after four attempts, in a save or in the dispatched EvaluateStalenessCommand); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

```mermaid
flowchart TD
    S(["Handler runs: POST projection/graphrag"]) --> RL[["ResponseIdList: validate and select, none listed selecting the Pending and Failed rows, and the Skipped rows once ingest is enabled"]]
    RL -->|"malformed, more than 1000, or an id without rsp-"| E2(["400 validation_error"])
    RL -->|"selected, unknown ids in notFound"| L[["For each selected response (nested: ProjectOneResponse)"]]
    L -->|every response handled| R(["200 ProjectAgentResponsesResult: projected, removed, skipped, skippedReason, failed, notFound"])
    L -->|"a write exhausted its retries"| EC(["409 conflict, retryable true"])
```

##### Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand)

```mermaid
flowchart TD
    S(["Next selected response"]) --> EV["Dispatch EvaluateStalenessCommand for this response through IDispatcher"]
    EV -->|"409 conflict, retryable true"| X(["Stop the loop: the unit returns 409 conflict, retryable true"])
    EV -->|"evaluated, status persisted"| TR{"Response Trusted?"}
    TR -->|yes| PE{"GraphRAG enabled and ProjectionEnabled?"}
    PE -->|"no: Skipped, projection_disabled or graphrag_disabled"| SV[["SerializableWrite, locking the response: re-read its status and row after any GraphRAG call, then save the row state, keeping an overlapping pass's document and deleting its own"]]
    PE -->|yes| UP{"Projected content hash equals ContentSha256?"}
    UP -->|yes| NX(["Continue with the next response"])
    UP -->|no| ING["IGraphRagService.IngestTextAsync in GraphRagStorageScope.Workspace with the content and its root ids"]
    TR -->|no| DOC{"Row holds a GraphRagDocumentId?"}
    DOC -->|"no: Removed"| SV
    DOC -->|yes| GE{"GraphRAG enabled?"}
    GE -->|"no: the row stays Pending"| NX
    GE -->|yes| DEL["IGraphRagService.DeleteDocumentAsync"]
    ING -->|"ok: Projected, or Pending for removal with the new document id when the response left Trusted meanwhile. Error: Failed"| SV
    DEL -->|"ok: Removed. Error: Failed, keeping the document id"| SV
    SV -->|committed| NX
    SV -->|retries exhausted| X
```

#### GetAgentContextPilotReportQuery: pilot report [FR-MCP-AGENTCONTEXT-009]

- Handler: `GetAgentContextPilotReportQuery(string WorkspacePath, AgentContextPilotReportQuery Query) : IQuery<AgentContextPilotReport>` in `src/QBrainAi.Services/AgentContext/Responses/Queries/GetAgentContextPilotReportQuery.cs` and `GetAgentContextPilotReportQueryHandler` in `src/QBrainAi.Services/AgentContext/Responses/Queries/GetAgentContextPilotReportQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/pilot/report?fromUtc&toUtc`; MCP `agent_context_pilot_report(workspacePath, fromUtc, toUtc)`; client `GetPilotReportAsync(AgentContextPilotReportQuery)`; REPL `client.AgentContext.GetPilotReportAsync`; PowerShell `New-McpAgentContextPilotReportParams`; tool descriptor `agent_context_pilot_report`.
- Comparison (section 3.1, the pilot comparison): responses submitted in the window are grouped by `invocationMode` (`Pilot` yields `Stateful` responses, a period in `Stateless` on hosts with history exclusion yields `Stateless` ones). Per mode: counts by effective status, through IResponseStalenessRule read-only, so a stored `Trusted` response whose dependency changed counts as `Stale` and nothing is written (`PilotReportAfterDependencyChangeCountsStaleNotTrustedWritesNothing`); mean latest model correctness and completeness over the responses that have a model grade ("latest" by `RecordedUtc`, then `GradeId`); and `trustRate`, the `Trusted` responses over all responses of the mode (`PilotReportResponsesWithoutModelGradeExcludedFromMeansInTrustRateDenominator`). For the window it also counts the packages that failed (`overflowPackages`, the `Overflow` packages), the requests with a package and no response (`packagesWithoutResponse`, the `Built` packages no response answers), and the refused prompts (`refusedPrompts`, the turns closed `canceled`, or with the legacy spelling `cancelled`, whose `failureNote` starts with the token `agent_context_invocation_failed` that RunStatelessInvocation writes, section 6.6, so a refusal for an outage or a commit, profile, or budget failure is counted alike) (`PilotReportCountsOverflowRefusedAndPackageWithoutResponse`).
- Not gated, so the report stays readable after a rollback to `Off`. Response 200 `AgentContextPilotReport`. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; a missing or invalid `fromUtc` or `toUtc`, or `fromUtc` not before `toUtc`). REPL: a listed read, never refused by ClientMutationPolicy.

```mermaid
flowchart TD
    S(["Handler runs: GET pilot report"]) --> V{"fromUtc and toUtc valid UTC and from before to?"}
    V -->|no| E2(["400 validation_error"])
    V -->|yes| ST[["IResponseStalenessRule, read-only: effective status of every response submitted in the window, nothing written"]]
    ST --> Q["Group the responses by invocationMode"]
    Q --> AGG["Per mode: counts by effective status, mean latest model correctness and completeness over the responses with a model grade, trustRate over all responses of the mode"]
    AGG --> FC["Count overflowPackages, packagesWithoutResponse, refusedPrompts"]
    FC --> R(["200 AgentContextPilotReport"])
```

#### CreateAgentContextHvRequestCommand: create an HV request for a range of turns [FR-MCP-AGENTCONTEXT-004]

- Handler: `CreateAgentContextHvRequestCommand(string WorkspacePath, CreateAgentContextHvRequestRequest Request) : ICommand<AgentContextHvRequestResult>` in `src/QBrainAi.Services/AgentContext/Responses/Commands/CreateAgentContextHvRequestCommand.cs` and `CreateAgentContextHvRequestCommandHandler` in `src/QBrainAi.Services/AgentContext/Responses/Commands/CreateAgentContextHvRequestCommandHandler.cs`.
- Caller: any agent (operator, 2026-10-11: "Agents will specify a range of session turns to be HV, the HV agent will mark the grade you have been working.").
- REST `POST /qbrainai/agent-context/hv-requests`; MCP `agent_context_create_hv_request(string workspacePath, string sessionId, string agent, string fromRequestId, string toRequestId, string requestedBy, string reason, string idempotencyKey)`; client `CreateHvRequestAsync(CreateAgentContextHvRequestRequest)`; REPL `client.AgentContext.CreateHvRequestAsync`; PowerShell `New-McpAgentContextCreateHvRequestParams`; tool descriptor `agent_context_create_hv_request`.
- Response 201 `AgentContextHvRequestResult` (Location `/qbrainai/agent-context/hv-requests/{hvRequestId}`) with its items: every agent response whose `RequestId` lies in the range and whose status is not `Rejected` or `Superseded` and that is not already `Pending` in another `Open` HV request, in turn order, each `Pending` (`HvRequestResponsePendingInOpenRequestNotListedAgain`). A repeat with the same `idempotencyKey` and the same `sessionId`, `agent`, range, `requestedBy`, and `reason` returns 200 with the stored request and its current items and status, so a retry after a lost 201 never creates a second request (`HvRequestRetrySameKeySameInputsReturns200Stored`); the same key with any other input returns 409 `conflict` with `retryable: false` (`HvRequestSameKeyOtherInputs409Conflict`). The key lookup (IdempotentCreate, policy `Conflict`, fingerprint of the client inputs) is the first step inside the transaction, before the session, range, and status checks; the unit reads only the database, so it needs no read-only lookup before its SerializableWrite (section 3.5.2). A concurrent create that wins the key's unique index ends the loser's attempt, SerializableWrite runs the delegate again, and the lookup returns the winner, the same rule Build and Submit follow.
- Concurrency: creation runs through SerializableWrite (section 3.5.2), whose first statements take the `StatusVersion` lock of every response of the range in ascending `ResponseId` order; the delegate then re-reads their status and inserts the items. A concurrent RejectAgentResponseCommand or SupersedeAgentResponseCommand on a candidate therefore commits either before (the response is excluded) or after (its ResponseStatusTransition finds the new `Pending` item and closes it), so no `Pending` item ever names a terminal response (`HvRequestForcedCreateVsRejectNoPendingItemOnTerminalResponse`, `HvRequestForcedCreateVsSupersedeNoPendingItemOnTerminalResponse`), and two concurrent creations over overlapping ranges lock their shared responses in the same order, so the second sees the first's `Pending` items and each response is `Pending` in at most one `Open` request (`HvRequestForcedConcurrentOverlappingCreatesEachResponsePendingInAtMostOneOpenRequest`), each case on the three providers. When every candidate turned terminal the creation fails with `agent_context_hv_request_invalid` and stores nothing; when SerializableWrite exhausts its four attempts the unit returns 409 `conflict` with `retryable: true`.
- Turns (TurnLocator): the session by `agent` and `sessionId`, then each endpoint turn by its `requestId`; `SessionNotFound` is the unknown-session refusal and `TurnNotFound` the range refusal below.
- Review contract: the brief for an HV request asks the reviewer to emit in its `=== VERDICT JSON ===` the `hvRequestId` and `grades`, one entry per item `{ responseId, contentSha256, correctness, completeness, findings [{ claimId, check, outcome }] }` naming every claim of the response, and to store its entire result on its own session-log turn; the runner then freezes the receipts and runs the grade-only continuation of the same thread (RecordModelGradeCommand, two-step protocol), and RecordModelGradeCommand accepts a grade only when it equals that entry and that turn (section 6.3).
- Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; blank fields, or TurnLocator `InvalidIdentifier`), `agent_context_hv_request_invalid` (an unknown session, an endpoint turn not in the session, from after to, a range longer than `HvRequest:MaxTurns`, or a range with no gradable agent response, `HvRequestUnknownSessionBadRangeOverMaxTurnsOrNoResponse400HvRequestInvalid`); 409 `agent_context_disabled`, `conflict` with `retryable: false` (the idempotency key reused with other inputs), `conflict` with `retryable: true` (SerializableWrite or ReadableKeyAllocator `hvr-` exhausted after four attempts); REPL only (ClientMutationPolicy, before any server call): `transaction_degraded`, `mutation_not_transactional`.

The selection re-reads the locked responses and keeps, in turn order and each `Pending`, those neither `Rejected` nor `Superseded` and not `Pending` in another `Open` HV request. The insert takes a `hvr-` key from ReadableKeyAllocator through IdempotentCreate and stores the HV request `Open` with its items. The diagram draws the turn, range, and selection checks as one call of the nested activity SelectHvRequestItems below, inside the delegate.

```mermaid
flowchart TD
    S(["Handler runs: POST hv-requests"]) --> V{"Every request field nonblank?"}
    V -->|no| E2(["400 validation_error"])
    V -->|yes| SW[["SerializableWrite, locking the range's responses"]]
    SW -->|retries exhausted| EC(["409 conflict, retryable true"])
    subgraph TX [Delegate of SerializableWrite]
        IC[["IdempotentCreate lookup"]]
        SI[["SelectHvRequestItems"]]
        INS[["Insert the HV request Open and its items"]]
    end
    SW -->|attempt| IC
    IC -->|"stored, same inputs"| R2(["200 the stored request, current items"])
    IC -->|"key reused, other inputs"| E8(["409 conflict, retryable false"])
    IC -->|absent| SI
    SI -->|refused| ERS(["Refusal of SelectHvRequestItems, drawn there"])
    SI -->|"items selected"| INS
    INS -->|"key collision: attempt ends"| SW
    INS -->|committed| R(["201 AgentContextHvRequestResult"])
```

##### Nested activity: SelectHvRequestItems (turn, range, and item selection inside the SerializableWrite delegate of CreateAgentContextHvRequestCommand)

It runs inside the delegate, after the key lookup, and reads only the database; a refusal rolls the transaction back and is returned unchanged. TurnLocator finds the session by `agent` and `sessionId`, then each endpoint turn by its `requestId`. One `agent_context_hv_request_invalid` terminal stands for the unknown session (`SessionNotFound`), the range refusals (an endpoint turn not in the session, from after to, or a range longer than `HvRequest:MaxTurns`), and a range with no gradable agent response; each edge names its case.

```mermaid
flowchart TD
    S(["SelectHvRequestItems: in the delegate"]) --> TL[["TurnLocator: session, then both endpoint turns"]]
    TL -->|InvalidIdentifier| E2(["400 validation_error"])
    TL -->|"SessionNotFound: session"| E4(["400 agent_context_hv_request_invalid"])
    TL -->|"TurnNotFound: range"| E4
    TL -->|"both turns found"| RG{"From not after to, within MaxTurns?"}
    RG -->|"no: range"| E4
    RG -->|yes| SEL["Keep the gradable responses, each Pending"]
    SEL --> ANY{"At least one response kept?"}
    ANY -->|"no: none gradable"| E4
    ANY -->|yes| R(["Items selected, each Pending"])
```

#### GetAgentContextHvRequestQuery: get an HV request [FR-MCP-AGENTCONTEXT-004]

- Handler: `GetAgentContextHvRequestQuery(string WorkspacePath, string HvRequestId) : IQuery<AgentContextHvRequestResult>` in `src/QBrainAi.Services/AgentContext/Responses/Queries/GetAgentContextHvRequestQuery.cs` and `GetAgentContextHvRequestQueryHandler` in `src/QBrainAi.Services/AgentContext/Responses/Queries/GetAgentContextHvRequestQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/hv-requests/{hvRequestId}`; MCP `agent_context_get_hv_request(string workspacePath, string hvRequestId)`; client `GetHvRequestAsync(hvRequestId)`; REPL `client.AgentContext.GetHvRequestAsync`; PowerShell `New-McpAgentContextGetHvRequestParams`; tool descriptor `agent_context_get_hv_request`.
- Not gated. Response 200 `AgentContextHvRequestResult` with its items. Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior); 404 `not_found` (`hv_request`). REPL: a listed read, never refused by ClientMutationPolicy.

```mermaid
flowchart TD
    S(["Handler runs: GET hv-requests/{hvRequestId}"]) --> F{"HV request found in the workspace?"}
    F -->|no| E2(["404 not_found hv_request"])
    F -->|yes| L["Load items with their dispositions"]
    L --> R(["200 AgentContextHvRequestResult"])
```

#### QueryAgentContextHvRequestsQuery: query HV requests [FR-MCP-AGENTCONTEXT-004]

- Handler: `QueryAgentContextHvRequestsQuery(string WorkspacePath, AgentContextHvRequestQuery Query) : IQuery<PageResult<AgentContextHvRequestResult>>` in `src/QBrainAi.Services/AgentContext/Responses/Queries/QueryAgentContextHvRequestsQuery.cs` and `QueryAgentContextHvRequestsQueryHandler` in `src/QBrainAi.Services/AgentContext/Responses/Queries/QueryAgentContextHvRequestsQueryHandler.cs`.
- REST `GET /qbrainai/agent-context/hv-requests?sessionId&status&limit&offset`; MCP `agent_context_query_hv_requests(string workspacePath, string? sessionId = null, string? status = null, int limit = 50, int offset = 0)`; client `QueryHvRequestsAsync(AgentContextHvRequestQuery)`; REPL `client.AgentContext.QueryHvRequestsAsync`; PowerShell `New-McpAgentContextQueryHvRequestsParams`; tool descriptor `agent_context_query_hv_requests`.
- Paging: PagedQuery with `PageBounds` `Refuse` (limit 1..1000, default 50, offset at least 0), stable order `CreatedUtc` descending, then `HvRequestId`.
- Not gated. Response 200 `PageResult<AgentContextHvRequestResult>` whose items are HV request summaries (`items` null, section 6.4). Errors: 400 `validation_error` (`details.field` `workspacePath`, WorkspaceResolutionBehavior; an unknown status; `limit` or `offset` outside the bounds, `details.field` naming it). REPL: a listed read, never refused by ClientMutationPolicy.

```mermaid
flowchart TD
    S(["Handler runs: GET hv-requests"]) --> V{"Status filter absent or known?"}
    V -->|no| E2(["400 validation_error"])
    V -->|yes| PQ[["PagedQuery, PageBounds Refuse: filter by sessionId and status, order CreatedUtc descending, then HvRequestId"]]
    PQ -->|"limit or offset outside the bounds"| E2
    PQ -->|page| R(["200 PageResult of HV requests without items"])
```

### 6.6 Processes (plugin)

#### Process RunStatelessInvocation: run a stateless invocation (plugin process) [FR-MCP-AGENTCONTEXT-009]

- Not a unit of work: the plugin orchestrates GetAgentContextStatusQuery (the Mode), BuildAgentContextPackageCommand (the package), and QueryAgentContextPackagesQuery (finding the package again) through the REPL; the model submits its response through SubmitAgentResponseCommand, and the plugin then requests RunDeterministicGradeCommand.
- PowerShell core: `Invoke-PluginAgentContextInvocation` in `plugins/core/lib-ps/repl-invoke.ps1`, the dot-sourceable library of section 3.5.6 (section 3.1, Plugin function home), for all nine hosts (the TypeScript hosts through their adapters); the prompt hook in `plugin-hook.ps1` dot-sources that library and calls it, and process ApplyArrivalBridgeRecord calls it in-process for a delivery `absorbed_mid_turn` (the arrival bridge is its own script entry that dot-sources the same library, section 3.5.6). Hosts inject the rendered package through their prompt-context mechanism (hook additional context or the host's system-context API).
- Output: in `Stateless` mode the model receives only the rendered package, the request, and the `REQUIRED MEMORIES` block, without prior conversation turns; in `Pilot` mode the package is built and recorded but not injected; in `Off` nothing happens.
- Trigger (operator, 2026-10-12: "Hook syncs, absorbed=Pilot (Recommended)"): the prompt hook of a user request on every host. On hosts without a bridge the hook's own mint result decides (process OpenTurnFromPromptHook, classified by `Resolve-MintOutcome`, section 3.5.6). On claude-code and claude-cowork, where the arrival bridge is the only minter, the prompt hook applies pending transcript records through `Sync-PluginArrivalLog` up to the transcript length at its own start, takes the bridge-minted `requestId` of the delivered request, and then runs the invocation; when that request's `enqueue` record is not yet in the transcript, the invocation fails: the prompt is refused, and, with no `requestId`, no turn is closed (`StatelessInvocationBridgeHostHookSyncsAndTakesBridgeMintedRequestId`, `StatelessInvocationBridgeHostEnqueueNotYetWrittenPromptRefused`). A message absorbed into a running turn (`absorbed_mid_turn`, delivered by process ApplyArrivalBridgeRecord) has no prompt to refuse and no history to exclude, so it runs as `Pilot`: the package is built and recorded, never injected, the prompt is never refused, and a build failure is recorded in `turns/<requestId>.yaml` without a `canceled` close (`StatelessInvocationAbsorbedMidTurnRunsAsPilotNeverRefused`).
- Mode (section 3.1, resolutions of round 5): the plugin reads `mode` through GetAgentContextStatusQuery at host session start (SessionStart, or the first prompt on a host without a SessionStart event: cline, cline-v2, and opencode) and caches it as `agentContextMode` in the host session's `turn-state.yaml` (section 3.4, Plugin per-turn store; absent before Phase C and read as `Off`) through `Update-TurnState` (`StatelessInvocationHostWithoutSessionStartModeReadAtFirstPrompt`), and RunStatelessInvocation reads `agentContextMode` at each prompt; the status query at host session start is the plugin's only agent-context read there, and its cost is bounded in GetAgentContextStatusQuery (section 6.5, risk R-R4); a Mode it cannot read, including a read that misses the hook bound, or a value it does not know, is `Off` (`StatelessInvocationModeUnreadableAtSessionStartAssumedOff`); a change away from `Off` takes effect at the next host session start (`StatelessInvocationModeSwitchedOnMidSessionTakesEffectAtNextSessionStart`). A 409 `agent_context_disabled` answer to the build or to the submission records `agentContextMode` `Off` for the host session through `Update-TurnState` and the request continues statefully: no refusal, no `canceled` close, and the turn closes as any stateful turn, because `Off` is the stateful default and the documented rollback (`StatelessInvocationModeSwitchedOffMidSessionBuild409DisabledContinuesStatefulNoCancel`, `StatelessInvocationModeSwitchedOffBeforeCloseSubmit409DisabledTurnClosesNormally`).
- Hosts that run it as `Off`: a host session reported `turn_logging_unsupported` (Phase A, D6) has no minted turn and stays stateful, reporting `turn_logging_unsupported`; a host whose prompt hook cannot refuse a prompt, or that runs the prompt anyway after a hook timeout, crash, or `plugin_core_unavailable`, runs `Pilot` and `Stateless` as `Off` (no build, no refusal) and its feature-matrix row records the limitation; P0.2 probe (j) records each host's refusal capability (`StatelessInvocationHostCannotRefuseTreatedAsOffNoBuildFeatureMatrixRow`).
- The turn (hosts without a bridge, by `Resolve-MintOutcome`): `created`, or `merged` with the turn in progress (a duplicate delivery), runs the build; a duplicate delivery's second build carries the same idempotency key and client inputs, so it returns the stored package and never refuses (`StatelessInvocationDuplicateDeliverySecondBuildReturnsStoredPackageNoSpuriousRefusal`). An `outage` (a `pending-<nonce>` turn) fails the invocation like a build outage ("Fail the invocation"): the prompt is refused with the outage shown, and the `canceled` close of the pending id is captured in the failsafe behind its mint record, in the DrainFailsafe order (`StatelessInvocationMintOutagePendingTurnPromptRefusedCancelQueuedBehindMint`). A `final` refusal leaves no turn, so the prompt is refused with the code and the Stop gate warns the refusal, naming the prompt (FR-MCP-SESSIONTURN-001-AC008, `StatelessInvocationFinalMintRefusalPromptRefusedStopWarnsNamingPrompt`); a hook that mints nothing (`nonce_source_missing`, or `turn-open-contended`) also leaves no turn and is refused the same way. `unsupported` (the equal-field rule) and `merged` with a terminal turn run nothing.
- Build: BuildAgentContextPackageCommand with the bound `agent`, `sessionId`, and `requestId`, the prompt text, and idempotency key = `requestId`. The plugin keeps the returned `packageId` in its per-turn file `turns/<requestId>.yaml`; no session-log turn field holds it.
- Package failure (operator, 2026-10-11: "Fail the invocation"): for any other build error or outage (a transport outage, 503 `backend_unavailable`, a busy store's 503 `persistence_error` with `retryable: true`, and 409 `conflict` with `retryable: true` are outages, section 3.1, Deadlock and busy outcomes and Retryable answers; the REPL policy's `transaction_degraded` and `mutation_not_transactional` are build errors, section 6.5, `StatelessInvocationBuildTransactionDegradedTreatedAsBuildError`), or a success response whose package status is not `Built` (a defensive check; the build never returns one, `StatelessInvocationPackageNotBuiltRefused`), the plugin refuses the prompt with the error code and message shown to the operator (for `agent_context_budget_exceeded` also `tokenBudget`, `mandatoryTokenCount`, `overflowTokenCount`, and the mandatory items), produces no answer, and closes the request's own turn `canceled`: the plugin, not the model, sends the bound turn at status `canceled` with `failureNote` `agent_context_invocation_failed <code>: <message>` (with the same counts for an overflow, `StatelessInvocationOverflowRefusalAndFailureNoteCarryTokenCounts`) through SubmitSessionLogCommand on the persist path of process PersistTurnWriteWithReceipt (canceled turns are not gated, FR-MCP-SESSIONCLASS-002; `Test-ModelEvidence` never applies to a `canceled` close, section 3.5.6). Receipt branches: `persisted` removes the request from `openDelivered` and the Stop ledger after the confirmed close, so the Stop gate never waits on a turn the plugin itself closed (`StatelessPackageFailureClosesOwnTurnCanceledRemovedFromLedgerNoOtherTurnTouched`); `queued` (an outage) holds the close in the failsafe, and the Stop gate warns degraded persistence naming the package failure, never blocks, and keeps the ledger entry until the drain confirms the close (`StatelessPackageFailureCancelCloseQueuedByOutageStopWarnsDegradedDrainConfirmsAndClears`); any other receipt is shown with its code and the turn stays open for the Stop gate. This is the only `canceled` a hook ever writes, and only for the failed request's own turn, never another turn (the Phase A isolation rule, refined for this case, which updates FR-MCP-TRIAGESTORE-001 ac-2: `IsolationHooksNeverPersistCanceledExceptOwnTurnPackageFailure`).
- History exclusion (operator: "Each agent invocation can run stateless"): in `Stateless` mode the model's input for the request is exactly the rendered package, the current request, and the deterministic `REQUIRED MEMORIES` block the host already injects (section 3.1, Memories in Stateless mode; FR-MCP-MEMORY-003, -007); history stays excluded. Exclusion removes the messages of earlier requests only: on each later model call of the same request (a tool loop) the request's own messages (its prompt, the model's tool calls, and their results) stay with the package and the memory block (section 3.1, Tool loops in `Stateless`; `StatelessToolLoopOwnToolCallsKeptPriorRequestsExcluded`). The plugin achieves it only through an authoritative host operation recorded in the host's P0.2 row: replacing the model's message list for that request (opencode `experimental.chat.messages.transform`, whose `output.messages` the adapter replaces with the list the core returns, which carries the memory block; its input is empty, so the core takes the host session key from `output.messages[].info.sessionID`; and cline-v2 `beforeModel`, forwarded by its adapter, where cline-v2's P0.2 row (probe (j)) records that the model request the core returns replaces the model's messages, that request carrying the package, the current request, and the memory block, section 3.2, TypeScript-host adapter contract) or starting a fresh host invocation whose only input is the package, the request, and the memory block. A host without such an operation cannot run `Stateless`: its plugin treats `Stateless` as `Pilot` (the package is built and recorded, the model answers statefully, and the submission's `invocationMode` is `Stateful`) and its feature-matrix row records the limitation (`StatelessHostWithoutHistoryExclusionTreatedAsPilotFeatureMatrixRow`). Each supported host has a host-level test that sends a sentinel prior turn and asserts, on the message list the host passes to its model call (for opencode, the host's list after the adapter applies the core's `output.messages`), captured by the host test harness and not by the adapter fixture, which records only what the adapter forwards, that the sentinel is absent while the package, the current request, and the `REQUIRED MEMORIES` block are present (`StatelessHostSentinelPriorTurnAbsentFromModelRequest`); the Phase 4 live check repeats it on each host whose P0.2 row records history exclusion.
- Submission (operator, 2026-10-12: "Model calls submit tool (Recommended)"): in `Pilot` and `Stateless` the model submits its response with the `agent_context_submit_response` tool (SubmitAgentResponseCommand) before it closes the turn, with its claims, evidence, and `invocationMode` (`Stateless` only when history was excluded; `Stateful` in `Pilot`, for an absorbed message, and on a host that runs `Stateless` as `Pilot`). The plugin binds the call to its request as it binds a verb (process BindVerbToTurn) and supplies that request's `packageId` from `turns/<requestId>.yaml`, or, when a hook process boundary lost it, from QueryAgentContextPackagesQuery `{ requestId, status: Built }` (`StatelessInvocationPackageIdLostFoundAgainByRequestId`); after a 201 or 200 it requests the deterministic grade (RunDeterministicGradeCommand) for the returned `responseId`. A failed submission or grade is reported in the tool result and writes nothing to the turn; a missing submission is not a Stop-gate item, and the pilot report counts requests with a package and no response. The model grade comes later from an HV request covering the turn (CreateAgentContextHvRequestCommand, RecordModelGradeCommand).

The diagram draws the refusal and its `canceled` close as one call of the nested activity FailInvocation, and the injection, the model's submission, and the grade request as one call of the nested activity AnswerAndSubmit. An unreadable Mode and a cached 409 `agent_context_disabled` both mean `Off`. When no turn exists to close (a bridge-host request whose `enqueue` record is not yet in the transcript, a final mint refusal, `nonce_source_missing`, or `turn-open-contended`), the prompt is refused with its code; for a final mint refusal the Stop gate also warns, naming the prompt. An absorbed request whose build fails records the failure in its per-turn file and is neither refused nor closed, and the model answers statefully. The build carries `agent`, `sessionId`, `requestId`, the prompt text, and idempotency key `requestId`. The Mode, host, trigger, and turn checks before the build are drawn as one call of the nested activity ResolveInvocationTurn below, whose edges are its returns.

```mermaid
flowchart TD
    S(["Prompt hook, or a delivery absorbed_mid_turn"]) --> RI[["ResolveInvocationTurn"]]
    RI -->|"stateful: Off, or no build"| STF(["Stateful turn as today"])
    RI -->|"prompt refused, no turn"| RFN(["Prompt refused, no turn to close"])
    RI -->|"outage: a pending-nonce turn"| FI[["FailInvocation"]]
    RI -->|"turn bound, or absorbed as Pilot"| BP[["BuildAgentContextPackageCommand"]]
    BP -->|"409 agent_context_disabled"| OFF["Cache Mode Off through Update-TurnState"]
    OFF --> STF
    BP -->|"other error, outage, or not Built"| ABF{"Absorbed request?"}
    ABF -->|no| FI
    ABF -->|yes| ABW(["Failure in the per-turn file only"])
    BP -->|"201, or 200 Built"| AS[["AnswerAndSubmit"]]
    AS -->|"submit 409 agent_context_disabled"| OFF
    AS -->|"submitted, or an error reported"| DN(["Outcome of AnswerAndSubmit, drawn there"])
    FI --> FO(["Invocation failed, no answer: drawn there"])
```

##### Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build)

A host session reported `turn_logging_unsupported`, a cached Mode `Off` (an unreadable Mode included), and a host whose prompt hook cannot refuse (P0.2 probe (j)) leave the turn stateful. A delivery absorbed into a running turn runs as `Pilot`. On a bridge host the hook applies pending transcript records through `Sync-PluginArrivalLog` up to its start and takes the bridge-minted `requestId`; on another host `Resolve-MintOutcome` classifies the hook's own mint. A request with no turn to close is refused with its code (a final mint refusal, `nonce_source_missing`, or a contended `turn-open.lock`; for a final mint refusal the Stop gate also warns, naming the prompt); a mint outage (a `pending-` nonce turn) fails the invocation through FailInvocation; an `unsupported` or merged terminal mint leaves the turn stateful. Each branch ends in its own terminal so the diagram stays one page.

```mermaid
flowchart TD
    S(["ResolveInvocationTurn"]) --> MD{"Logging supported, Mode not Off?"}
    MD -->|no| ST1(["Stateful turn as today"])
    MD -->|yes| CAN{"Hook can refuse (probe j)?"}
    CAN -->|"no: run as Off"| ST2(["Stateful turn as today"])
    CAN -->|yes| TRG{"Trigger?"}
    TRG -->|absorbed_mid_turn| ABS(["Turn bound: build as Pilot"])
    TRG -->|bridge host| SY[["Sync-PluginArrivalLog"]]
    TRG -->|other host| MR[["Resolve-MintOutcome"]]
    SY -->|enqueue not written| RF1(["Prompt refused, no turn"])
    SY -->|bridge requestId| B1(["Turn bound: build"])
    MR -->|final or contended| RF2(["Prompt refused, no turn"])
    MR -->|unsupported or terminal| ST3(["Stateful turn"])
    MR -->|outage| OUT(["Fail the invocation"])
    MR -->|created or open| B2(["Turn bound: build"])
```

##### Nested activity: FailInvocation (refusal and canceled close of RunStatelessInvocation)

The plugin refuses the prompt with the error code and message shown to the operator (for `agent_context_budget_exceeded` also `tokenBudget`, `mandatoryTokenCount`, `overflowTokenCount`, and the mandatory items) and produces no answer. It then closes the request's own turn `canceled` with `failureNote` `agent_context_invocation_failed <code>: <message>` through process PersistTurnWriteWithReceipt, with no model-evidence check. On `persisted` the request leaves `openDelivered` and the Stop ledger after the confirmed close; on `queued` (an outage, including a busy store and 409 `conflict` with `retryable: true`, which keep the failsafe record, section 3.1, Retryable answers; or a pending mint whose close waits behind its mint record) the close is held in the failsafe and the Stop gate warns degraded persistence naming the package failure, never blocks, and keeps the ledger entry until the drain confirms; any other receipt is shown with its code and the turn stays open for the Stop gate.

```mermaid
flowchart TD
    S(["FailInvocation: the failure's code and message"]) --> REF["Refuse the prompt: code, message, counts"]
    REF --> CN[["Process PersistTurnWriteWithReceipt: close canceled"]]
    CN -->|persisted| CL(["Failed: cleared from openDelivered and the ledger"])
    CN -->|"queued: outage"| CQ(["Failed: close in the failsafe, Stop gate warns"])
    CN -->|"other receipt code"| CO(["Failed: receipt shown, turn stays open"])
```

##### Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation)

The plugin keeps the returned `packageId` in `turns/<requestId>.yaml`. It injects only for `Stateless` on a host with an authoritative history-exclusion operation (P0.2 row): the model input becomes the rendered package, the request, and the `REQUIRED MEMORIES` block, and `invocationMode` is `Stateless`; `Pilot`, an absorbed request, and `Stateless` on a host without that operation answer statefully with `invocationMode` `Stateful`. Before it closes the turn the model calls `agent_context_submit_response`, and the plugin supplies the `packageId` from the per-turn file, or from QueryAgentContextPackagesQuery `{ requestId, status: Built }`. A failed submission is reported in the tool result and the model resubmits; the pilot report counts a package without a response. A failed grade request is reported the same way and the grade is re-run on request. After a 201 grade the response stays `Grading` (or `Stale`, when the grade's staleness dispatch found a changed dependency) until an HV request covering the turn records its model grade (CreateAgentContextHvRequestCommand, RecordModelGradeCommand).

```mermaid
flowchart TD
    S(["AnswerAndSubmit: the Built package"]) --> REC["Keep packageId in turns/requestId.yaml"]
    REC --> MM{"Inject the package for this request?"}
    MM -->|"Pilot, absorbed, or no history exclusion"| RUN1["No injection: invocationMode Stateful"]
    MM -->|"Stateless, history exclusion"| INJ["Model input: package, request, memories"]
    RUN1 --> SUB["Model calls agent_context_submit_response"]
    INJ --> SUB
    SUB --> SR{"Submit result"}
    SR -->|"409 agent_context_disabled"| OFF(["Returned: Mode Off to cache"])
    SR -->|"other error or outage"| SE(["In the tool result, turn untouched"])
    SR -->|"201 or 200"| GRD["Request RunDeterministicGradeCommand"]
    GRD --> GDR{"Grade result"}
    GDR -->|201| WAIT(["Grading until an HV model grade"])
    GDR -->|"error or outage"| GE(["In the tool result, turn untouched"])
```

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
- Projection pass: not drawn per sequence. A unit whose transaction marked a projection row (the two grade units through the trust decision, RejectAgentResponseCommand, SupersedeAgentResponseCommand, EvaluateStalenessCommand) dispatches ProjectAgentResponsesCommand with no `responseIds` after its commit and before it answers, and a failed dispatch is logged without changing its answer (section 6.5; the ProjectAgentResponsesCommand sequence draws the pass).

#### Sequence: RequestPipeline (every Phase C dispatch)

The CQRS behaviors of section 3.5.1 run inside the scoped `IDispatcher`, after the controller or tool calls `SendAsync` or `QueryAsync` and before the handler, for every command and query. WorkspaceResolutionBehavior resolves a record's non-empty `WorkspacePath` into the dispatch scope's `WorkspaceContext` and otherwise keeps the workspace the transport resolved; it refuses a path that names no registered workspace, or a record with no workspace that does not implement `IAllowsGlobalWorkspace`, with 400 `validation_error` and `details.field` `workspacePath` on every surface, the status TR-MCP-MT-002 gives an unregistered workspace. On REST, `WorkspaceResolutionMiddleware` still answers a missing or unknown `X-Workspace-Path` before any controller with today's body (TR-MCP-MT-002), so the behavior's refusal reaches REST only through a record path that names an unregistered workspace. A plugin that receives 409 `agent_context_disabled` from a build or a submission caches `Off` for its host session and continues statefully (section 3.1, Mode changes).

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Workspace as WorkspaceResolutionBehavior
    participant ModeGate as ModeGateBehavior
    participant Handler
    Caller->>Controller: request with X-Workspace-Path or workspacePath
    Controller->>Workspace: SendAsync(command) or QueryAsync(query)
    Note over Workspace,ModeGate: inside IDispatcher, before the handler
    alt path unregistered, or no workspace and not allowed
        Workspace-->>Controller: Failure, ClassifiedError 400
        Controller-->>Caller: 400 validation_error, details.field workspacePath
        Note over Caller: fix X-Workspace-Path or workspacePath
    else resolved into the scope's WorkspaceContext
        Workspace->>ModeGate: next
        alt record implements IModeGatedRequest and Mode is Off
            ModeGate-->>Controller: Failure, ClassifiedError 409
            Controller-->>Caller: 409 agent_context_disabled
            Note over Caller: a plugin caches Off for the host session and continues statefully
        else not gated, or Mode is Pilot or Stateless
            ModeGate->>Handler: Handle(record)
            Note over Handler: request validation, then the unit's steps
            Handler-->>Controller: Result, success or ClassifiedErrorException
            Controller-->>Caller: status and body
        end
    end
```

#### Sequence: submit GetAgentContextStatusQuery

The plugin reads the Mode once at host session start (SessionStart, or the first prompt on a host without a SessionStart event: cline, cline-v2, and opencode) and caches it as `agentContextMode` in the host session's `turn-state.yaml` through `Update-TurnState`; a Mode it cannot read, including a read that misses the SessionStart hook bound, or a value it does not know, is `Off`, and a change away from `Off` takes effect at the next host session start (section 3.1, Mode changes). Responses are counted by effective status, through `IResponseStalenessRule` read-only, and the query writes nothing. Because every host session start makes this read (risk R-R4), the handler reads each distinct dependency and commit once through `SourceResolver`, shared by every `Submitted`, `Grading`, or `Trusted` response that cites it (inference: the work is bounded by the distinct dependencies cited, not by the response count), and answers within the SessionStart hook bound with 1000 `Trusted` responses (`StatusThousandTrustedResponsesAnswersWithinSessionStartHookBound`).

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant GraphRAG
    participant Store
    Caller->>Controller: GET /status
    alt server answers
        Controller->>Handler: QueryAsync(GetAgentContextStatusQuery)
        Note over Controller,Handler: ref RequestPipeline, not gated: readable in Mode Off
        Handler->>GraphRAG: GetStatusAsync
        GraphRAG-->>Handler: enabled, state
        Handler->>Store: count packages, and responses by stored status
        Store-->>Handler: counts, the Submitted, Grading, Trusted responses
        Handler->>Handler: IResponseStalenessRule, read-only, each dependency read once
        Note over Handler: counts by effective status, nothing written
        Controller-->>Caller: 200 AgentContextStatusResult
        Note over Caller: a plugin caches the Mode for the host session, Off means stateful
    else outage, any error, or no answer within the hook bound
        Note over Caller: a plugin caches Off for the host session
    end
```

#### Sequence: submit BuildAgentContextPackageCommand

`Caller` is usually the plugin running process RunStatelessInvocation (below). Build reads before it writes (section 3.1, "Database-only delegates"): the read-only key lookup, then nested sequence ReadBuildInputs, then `PackageAssembler` in memory, and only then nested sequence SaveBuiltPackage, whose SerializableWrite delegate reads and writes only the database. No transaction is open during a retrieval, a GraphRAG call, or a git read, so on SQLite no hook write waits on them (`BuildOnSqliteHookWriteDuringRetrievalNotBlocked`). The idempotency fingerprint is over the request's client inputs, and the read-only lookup runs before every read that can change, so a retry of a lost answer returns the stored package even after a source or the HEAD commit changed; a retry of an overflow finds the stored `Overflow` package and returns the same 422, never an answer. `InputsSha256` stays the provenance and replay check. `PackageAssembler` runs the secret filter (`SecretRedactor`), the omission filter, the findings, the mandatory-context check, the budget fill, and the rendering (section 3.5.5), and never truncates mandatory context. The input snapshot records every input the assembler reads, including each prior-response candidate's trust and staleness state at build time and the `contradicts` and `blocks` links the findings read. Build persists nothing but its package, its children, its snapshot, and its links: it reads staleness and never persists it (only EvaluateStalenessCommand persists `Stale`).

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: POST /packages (BuildAgentContextPackageRequest)
    Controller->>Handler: SendAsync(BuildAgentContextPackageCommand)
    Note over Controller,Handler: ref RequestPipeline, gated
    alt request invalid (ids, tokenBudget)
        Controller-->>Caller: 400 validation_error, details.field
        Note over Caller: fix the field, never resend unchanged
    else request valid
        Handler->>Store: IdempotentCreate.Find, read-only, before any source read
        alt stored package Built (a retry or a duplicate delivery)
            Controller-->>Caller: 200 AgentContextPackageResult, status Built
            Note over Caller: use the returned package
        else stored package Overflow (a retry)
            Controller-->>Caller: 422 agent_context_budget_exceeded, stored diagnostics
            Note over Caller: refuse the prompt, close the turn canceled
        else key reused with other inputs
            Controller-->>Caller: 409 conflict, retryable false
            Note over Caller: new key, or the same inputs
        else absent
            Note over Caller,Store: ref ReadBuildInputs (below), each refusal there answers the caller
            Handler->>Handler: PackageAssembler: assemble the candidate snapshot
            Note over Caller,Store: ref SaveBuiltPackage (below), SerializableWrite, database only
        end
    end
```

##### Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction)

Every read here runs outside a transaction. `SourceResolver` reads the workspace HEAD commit and the profile and rule sources, and resolves and hashes every candidate through `ContentHasher`. RetrieveAgentContextCandidatesQuery is dispatched with the package's `agent`, `sessionId`, `requestId`, and `focusRefs`, and `preferGraphRag` equal to `GraphRag:UseForRetrieval`; it calls GraphRAG when preferred and reads each prior-response candidate's status through `IResponseStalenessRule` read-only, writing nothing. Every focus ref is also a candidate, and the request's own turn is never one. A missing profile or rule file is fixed by the operator in `ProfileSources` or `RuleSources`. The turn's status read here is the state SaveBuiltPackage re-checks.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Handler->>Store: TurnLocator(agent, sessionId, requestId)
    alt InvalidIdentifier
        Controller-->>Caller: 400 validation_error, details.field
    else SessionNotFound or TurnNotFound
        Controller-->>Caller: 404 not_found, details.resource session_turn
        Note over Caller: open or mint the turn first
    else turn found, with its status
        Handler->>Handler: SourceResolver: ReadCommit, profile and rule sources
        alt commit unreadable
            Controller-->>Caller: 409 agent_context_commit_unavailable
            Note over Caller: refuse the prompt with the code, close the turn canceled
        else no profile file or no rule file resolves
            Controller-->>Caller: 409 agent_context_profile_unavailable
            Note over Caller: refuse the prompt with the code, close the turn canceled
        else commit, profile, and rules read
            Handler->>Handler: QueryAsync(RetrieveAgentContextCandidatesQuery)
            Note over Handler: staleness read-only, nothing written
            Handler->>Handler: SourceResolver: resolve and hash every candidate
        end
    end
```

##### Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence)

The delegate reads and writes only the database, within at most four attempts (section 3.5.2). It looks the key up again, which finds a concurrent build that won the key, re-checks the turn status ReadBuildInputs read (a change ends the attempt, and Build runs again from its reads), and inserts the package, its children, its input snapshot, and its links: `RelationshipWriter` with `CreatedBy` `BuildAgentContextPackageCommand`, `derived_from` each included source except a `Rule` read outside version control, and `relevant_to` the turn, with the hashes ReadBuildInputs read. An `Overflow` package is stored for audit with its mandatory items and token counts and no content. The package, its children, and its links commit together. The plugin keeps the `packageId` in its per-turn file and, in `Stateless`, injects `renderedContent`.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Handler->>Store: IdempotentCreate.Find, a concurrent winner
    alt stored package Built
        Controller-->>Caller: 200 AgentContextPackageResult, status Built
    else stored package Overflow
        Controller-->>Caller: 422 agent_context_budget_exceeded, stored diagnostics
    else key reused with other inputs
        Controller-->>Caller: 409 conflict, retryable false
    else absent, and the turn status changed since ReadBuildInputs
        Note over Handler,Store: the attempt ends, Build runs again from its reads
    else absent, and the turn status unchanged
        alt PackageAssembler returned Overflow
            Handler->>Store: insert the Overflow package, mandatory items, snapshot
            Controller-->>Caller: 422 agent_context_budget_exceeded, token counts
            Note over Caller: refuse the prompt with the token counts, close the turn canceled
        else assembled
            Handler->>Store: insert the package, its children, its snapshot
            Handler->>Store: RelationshipWriter: derived_from and relevant_to links
            Controller-->>Caller: 201 AgentContextPackageResult, Location
            Note over Caller: keep the packageId, inject in Stateless
        end
    else retries exhausted (SerializableWrite or key allocation)
        Controller-->>Caller: 409 conflict, retryable true, nothing saved
        Note over Caller: retry the same request within the bounded retries of C9
    end
```

#### Sequence: submit GetAgentContextPackageQuery

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: GET /packages/{packageId}?includeContent
    Controller->>Handler: QueryAsync(GetAgentContextPackageQuery)
    Note over Controller,Handler: ref RequestPipeline, not gated: readable in Mode Off
    Handler->>Store: load the package in the workspace
    alt not found
        Controller-->>Caller: 404 not_found, details.resource package
        Note over Caller: report the id
    else found, Built or Overflow
        Store-->>Handler: package, items, omissions, findings, content
        Controller-->>Caller: 200 AgentContextPackageResult
        Note over Caller: verify contentSha256 of renderedContent before use
    end
```

#### Sequence: submit QueryAgentContextPackagesQuery

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: GET /packages?requestId&sessionId&status&limit&offset
    Controller->>Handler: QueryAsync(QueryAgentContextPackagesQuery)
    Note over Controller,Handler: ref RequestPipeline, not gated: readable in Mode Off
    alt unknown status, or limit or offset outside PageBounds
        Controller-->>Caller: 400 validation_error, details.field
    else filters valid
        Handler->>Store: PagedQuery, CreatedUtc descending then PackageId
        Store-->>Handler: PageResult
        Controller-->>Caller: 200 PageResult of package summaries
        Note over Caller: PagedQuery paging
    end
```

#### Sequence: submit ReplayAgentContextPackageQuery

`PackageAssembler` rebuilds in memory from the stored input snapshot, `OptionsSnapshotJson`, and the focus refs only; because the snapshot records every input the assembler reads (section 3.5.5), Replay reproduces every assembler step without reading a live source or configuration, and stores nothing.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    participant Assembler as PackageAssembler
    Caller->>Controller: POST /packages/{packageId}/replay
    Controller->>Handler: QueryAsync(ReplayAgentContextPackageQuery)
    Note over Controller,Handler: ref RequestPipeline, not gated: readable in Mode Off
    Handler->>Store: BuiltPackageLookup(packageId)
    alt not found
        Controller-->>Caller: 404 not_found, details.resource package
    else Overflow
        Controller-->>Caller: 409 agent_context_package_overflow
    else no input snapshot stored
        Controller-->>Caller: 409 agent_context_replay_snapshot_unavailable
    else built by another builder version
        Controller-->>Caller: 409 agent_context_replay_builder_changed
        Note over Caller: report recorded and current, replay needs the same builder
    else replayable
        Handler->>Assembler: assemble(stored snapshot, OptionsSnapshotJson)
        Assembler-->>Handler: items, InputsSha256, ContentSha256
        Note over Handler: compare by ordinal, nothing stored
        Controller-->>Caller: 200 AgentContextReplayResult
        alt identical
            Note over Caller: deterministic replay proven
        else differences
            Note over Caller: report each added, removed, hashChanged, reordered item
        end
    end
```

#### Sequence: submit SubmitAgentResponseCommand

`Caller` is the plugin carrying the model's `agent_context_submit_response` tool call, which the model makes before it closes the turn; the plugin supplies the `packageId` it kept for the request (section 3.1, Response submission). `SubmissionSha256` (through `ContentHasher`, over the whole submission) is the idempotency fingerprint. Submit reads before it writes (section 3.1, "Database-only delegates"): the read-only key lookup, then nested sequence ReadSubmissionInputs, then `SecretRedactor` redacts the content in memory, and only then the SerializableWrite. Its delegate reads and writes only the database: it looks the key up again, which finds a concurrent submission that won the key, and inserts the response, its claims and evidence with both hashes, and its links (`RelationshipWriter`, `CreatedBy` `SubmitAgentResponseCommand`: `derived_from` the package, `supports` each `Requirement` or `File` evidence item, with the hashes ReadSubmissionInputs read). A stored package never changes, so the key is the only state the delegate re-checks. The response, its children, and its links commit together.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: POST /responses (SubmitAgentResponseRequest)
    Controller->>Handler: SendAsync(SubmitAgentResponseCommand)
    Note over Controller,Handler: ref RequestPipeline, gated
    alt content, claims, evidence, format, or mode invalid
        Controller-->>Caller: 400 validation_error, details.field
        Note over Caller: fix and resend
    else request valid
        Handler->>Store: IdempotentCreate.Find, read-only, before any source read
        alt stored, same submission
            Controller-->>Caller: 200 AgentResponseResult, the stored response
            Note over Caller: treat as submitted
        else key reused with another submission (any field)
            Controller-->>Caller: 409 conflict, retryable false
        else absent
            Note over Caller,Store: ref ReadSubmissionInputs (below), each refusal there answers the caller
            Handler->>Handler: SecretRedactor: redact the content, then ContentSha256
            Note over Handler,Store: SerializableWrite, database only
            Handler->>Store: IdempotentCreate.Find, a concurrent winner
            alt winner with the same submission
                Controller-->>Caller: 200 AgentResponseResult, the stored response
            else winner with another submission
                Controller-->>Caller: 409 conflict, retryable false
            else absent
                Handler->>Store: insert the response, claims, evidence, and links
                Controller-->>Caller: 201 AgentResponseResult, status Submitted
                Note over Caller: the plugin requests the deterministic grade
            else retries exhausted (SerializableWrite or key allocation)
                Controller-->>Caller: 409 conflict, retryable true, nothing saved
                Note over Caller: retry the same request within the bounded retries of C9
            end
        end
    end
```

##### Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction)

Every read here runs outside a transaction. `BuiltPackageLookup` finds the package; the submission must match it (`agent` equal to the package's agent, `invocationMode` `Stateless` only on a `Stateless` package, and no `SessionLogTurn` evidence naming the package's own turn, section 3.1). `SourceResolver` reads the workspace HEAD commit and hashes each `File` evidence ref at its `commitSha` and each `Requirement`, `Receipt`, and `SessionLogTurn` ref from its source; each cited hash is kept as `CitedSha256`, and the server's hash is `Sha256`, null when an existing source cannot be read.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Handler->>Store: BuiltPackageLookup(packageId)
    alt package not found
        Controller-->>Caller: 404 not_found, details.resource package
    else package Overflow
        Controller-->>Caller: 409 agent_context_package_overflow
    else agent, mode, or own-turn evidence inconsistent with the package
        Controller-->>Caller: 400 validation_error, details.field
    else package Built, submission consistent
        Handler->>Handler: SourceResolver: ReadCommit, hash each evidence ref
        alt commit unreadable
            Controller-->>Caller: 409 agent_context_commit_unavailable
        else a ref names no existing source
            Controller-->>Caller: 400 validation_error, details.field the ref
            Note over Caller: cite an existing source and resend
        else every ref resolved
            Note over Handler: Sha256 and CitedSha256 kept per evidence item
        end
    end
```

#### Sequence: submit GetAgentResponseQuery

`IResponseStalenessRule` evaluates a `Submitted`, `Grading`, or `Trusted` response read-only and the query returns its effective status, writing nothing, so a `Trusted` answer has just been checked.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: GET /responses/{responseId}?includeContent
    Controller->>Handler: QueryAsync(GetAgentResponseQuery)
    Note over Controller,Handler: ref RequestPipeline, not gated: readable in Mode Off
    Handler->>Store: load the response, claims, evidence, grades
    alt not found
        Controller-->>Caller: 404 not_found, details.resource response
    else found
        Handler->>Handler: IResponseStalenessRule, read-only
        Note over Handler: effective status, nothing written
        Controller-->>Caller: 200 AgentResponseResult, effective status
        Note over Caller: use as context only when Trusted
    end
```

#### Sequence: submit QueryAgentResponsesQuery

`IResponseStalenessRule` evaluates every matching `Submitted`, `Grading`, or `Trusted` response read-only before the query filters, counts, and pages by effective status; it writes nothing.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: GET /responses?requestId&packageId&status&trustedOnly&limit&offset
    Controller->>Handler: QueryAsync(QueryAgentResponsesQuery)
    Note over Controller,Handler: ref RequestPipeline, not gated: readable in Mode Off
    alt unknown status, or limit or offset outside PageBounds
        Controller-->>Caller: 400 validation_error, details.field
    else filters valid
        Handler->>Handler: IResponseStalenessRule, read-only
        Note over Handler: effective status of every match, nothing written
        Handler->>Store: PagedQuery by effective status
        Store-->>Handler: PageResult, SubmittedUtc descending then ResponseId
        Controller-->>Caller: 200 PageResult of response summaries
        Note over Caller: PagedQuery paging
    end
```

#### Sequence: submit RunDeterministicGradeCommand

The grade reads before it writes, in the order of section 3.1 ("Database-only delegates"): every read-only check, then the EvaluateStalenessCommand dispatch, then the SerializableWrite. Before the SerializableWrite the handler loads the response and checks that it is gradable (found, neither `Rejected` nor `Superseded`), has `SourceResolver` check each evidence item (nested activity CheckOneEvidenceItem) and the commit ancestry, so no git read runs inside a transaction, and runs `SecretRedactor.ContainsSecret` over the content, claims, and evidence refs; it then dispatches EvaluateStalenessCommand for the response (the only writer of `Stale`, which evaluates outside any transaction and persists `Stale` in its own SerializableWrite when a dependency of the response or its package changed) and reads the response's status and `StatusVersion` that the dispatch left. It then runs the global checks (every claim has evidence, and the freshness of the response and its package, from that status), computes the exact scores, and derives the verdict with `ScoreVerdict` at `max(configured, 0.98)`. The SerializableWrite locks the response and re-checks its `StatusVersion` against the one the reads saw (a concurrent reject, supersede, or staleness change ends the attempt, and the unit runs again from its reads, so a response rejected meanwhile is then refused 409 `agent_context_response_not_gradable`, `GradeResponseRejectedDuringReadsReadsAgain409NotGradable`), saves the grade, its findings, and the thresholds in force, and stamps `RecordedUtc`, which orders "latest". The trust decision is the same dispatch, drawn the same way, as in SubmitOneModelGrade: DecideAgentResponseTrustCommand, not gated, in the same transaction, reading only the database (the stored status and the latest grades). The caller processes the answer by `responseStatus`: `Stale` is excluded from trusted retrieval and needs a new response; `Grading` after a `Pass` waits for the model grade of an HV request covering the turn (CreateAgentContextHvRequestCommand); a `Fail` shows every FAIL and UNKNOWN finding, to fix and submit a new response.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: POST /responses/{responseId}/grades/deterministic
    Controller->>Handler: SendAsync(RunDeterministicGradeCommand)
    Note over Controller,Handler: ref RequestPipeline, gated
    Handler->>Store: load the response
    alt not found
        Controller-->>Caller: 404 not_found, details.resource response
    else Rejected or Superseded
        Controller-->>Caller: 409 agent_context_response_not_gradable
    else gradable
        Handler->>Handler: SourceResolver: each evidence item, the commit ancestry
        Note over Handler: secret scan of content, claims, and evidence refs
        Handler->>Handler: SendAsync(EvaluateStalenessCommand), this response
        Note over Handler: global checks, exact scores, ScoreVerdict
        Note over Handler,Store: SerializableWrite, response locked, database only
        Handler->>Store: re-check StatusVersion, save the grade and findings
        Handler->>Handler: SendAsync(DecideAgentResponseTrustCommand)
        Controller-->>Caller: 201 AgentResponseGradeResult, responseStatus
        alt responseStatus Stale
            Note over Caller: never trusted, submit a new response
        else verdict Pass, responseStatus Grading
            Note over Caller: await the model grade of an HV request
        else verdict Fail
            Note over Caller: fix each FAIL and UNKNOWN, submit anew
        end
    else retries exhausted (here or in EvaluateStalenessCommand)
        Controller-->>Caller: 409 conflict, retryable true, nothing saved
        Note over Caller: retry the same request within the bounded retries of C9
    end
```

#### Sequence: submit RecordModelGradeCommand from an HV review

`Agent` is the agent that asks for the review; `Runner` is the existing HV runner; `Reviewer` is the HV agent (the configured grader, Codex `gpt-6-astra` `max`, one reviewer thread, in two steps). In step 1 the reviewer reviews the turns and responses, emits the VERDICT JSON with a `grades` entry per response and its own thread id, and stores its entire result on its own session-log turn; when that run completes, the runner freezes the request, response, and rollout jsonl receipts under the receipt root.

```mermaid
sequenceDiagram
    autonumber
    participant Agent
    participant Runner
    participant Reviewer
    participant Controller
    Agent->>Controller: POST /hv-requests (CreateAgentContextHvRequestCommand)
    Controller-->>Agent: 201 hvRequestId with its response items
    Agent->>Runner: HV brief naming hvRequestId
    Runner->>Reviewer: step 1: codex exec (review)
    Note over Reviewer,Controller: reviews, emits the VERDICT JSON, stores its result on its own turn
    Reviewer-->>Runner: run completes (turn.completed)
    Note over Runner,Reviewer: freezes the request, response, and rollout jsonl receipts
    Runner->>Reviewer: step 2: codex exec resume reviewerThreadId
    Note over Runner,Reviewer: grade-only continuation naming the frozen receipt paths
    loop each response item of the HV request
        Note over Reviewer,Controller: ref SubmitOneModelGrade (below)
    end
    Reviewer-->>Runner: continuation completes
    Note over Runner: an interrupted continuation is resumed again with the same prompt
```

##### Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence)

The reviewer sends `client.AgentContext.RecordModelGradeAsync` through the REPL, whose lossless normalizer keeps `correctness` and `completeness` as their exact lexemes (`ScoreLexeme`, section 3.5.4), so `verdict_match` compares the verdict entry's own lexemes. `ScoreLexemeJsonConverter` refuses an invalid lexeme at deserialization, before the dispatch and before any lookup, with 400 `review_score_format`, `review_score_precision`, or `review_score_out_of_range`. The request carries `hvRequestId`, the scores and findings copied from the reviewer's `grades` entry, the receipt paths, the reviewer's turn ids, and `reviewerThreadId`. The model grade reads before it writes, in the order of section 3.1 ("Database-only delegates"): every read-only check (the response, the HV request and its item, and the eight receipt, reviewer, and runtime-identity checks), then the EvaluateStalenessCommand dispatch, then the SerializableWrite. Nested sequence ReadModelGradeInputs looks the stored grade up read-only, checks the response, the HV request, and its item, reads the receipt files and the reviewer's own turn for the eight checks of section 6.3, and only then dispatches EvaluateStalenessCommand for the response (the only writer of `Stale`, which evaluates outside any transaction); the handler then derives the verdict with `ScoreVerdict` at the 0.98 floor, in memory. The SerializableWrite then locks the response and reads and writes only the database: it looks the grade up again, which finds a concurrent continuation that stored it first; re-checks the response's `StatusVersion` and the HV request's status and item that the reads saw (a change ends the attempt, and the unit runs again from its reads); inserts the grade with its findings, receipt hashes, and the thresholds in force, stamps `RecordedUtc`, marks the item `Graded`, completes the HV request with `CompletedUtc` when no item is `Pending`, and dispatches DecideAgentResponseTrustCommand (not gated), whose decision reads the stored status and the latest grades; all of it commits together. The reviewer reports a `Grading` result's FAIL and UNKNOWN findings, or a `Stale` result's changed dependency, to the agent that requested the HV.

```mermaid
sequenceDiagram
    autonumber
    participant Reviewer
    participant Controller
    participant Handler
    participant Store
    Reviewer->>Controller: POST /responses/{responseId}/grades/model
    alt score lexeme invalid (ScoreLexemeJsonConverter)
        Controller-->>Reviewer: 400 review_score_format, _precision, or _out_of_range
    else lexemes valid
        Controller->>Handler: SendAsync(RecordModelGradeCommand)
        Note over Controller,Handler: ref RequestPipeline, gated
        alt hvRequestId, a jsonl path, a reviewer turn id, or reviewerThreadId missing
            Controller-->>Reviewer: 400 agent_context_receipt_paths_required
        else findings invalid
            Controller-->>Reviewer: 400 validation_error, details.field
        else grader not the configured one
            Controller-->>Reviewer: 403 agent_context_grader_not_allowed
        else request valid
            Note over Reviewer,Store: ref ReadModelGradeInputs (below), each refusal there answers the reviewer
            Note over Handler,Store: SerializableWrite, response locked, database only
            Handler->>Store: IdempotentCreate.Find, a concurrent continuation
            alt equal grade stored meanwhile
                Controller-->>Reviewer: 200 the stored AgentResponseGradeResult
            else a different grade stored meanwhile
                Controller-->>Reviewer: 409 conflict, retryable false
            else absent
                Handler->>Store: re-check, insert the grade, item Graded
                Handler->>Handler: SendAsync(DecideAgentResponseTrustCommand)
                Controller-->>Reviewer: 201 AgentResponseGradeResult, responseStatus, hvRequestStatus
                alt responseStatus Trusted
                    Note over Reviewer,Controller: the response becomes retrievable context
                else responseStatus Grading
                    Note over Reviewer,Controller: report each FAIL and UNKNOWN finding
                else responseStatus Stale
                    Note over Reviewer,Controller: report the changed dependency
                end
            else retries exhausted
                Controller-->>Reviewer: 409 conflict, retryable true, nothing saved
                Note over Reviewer: resubmit the same grade
            end
        end
    end
```

##### Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction)

Every read here runs outside a transaction, in the order of the unit's checks (section 6.5): the stored grade (IdempotentCreate on the natural key `ResponseId`, `HvRequestId`, kind `Model`, policy `Conflict`, read-only, so a retry after the request completed or the response was rejected still returns the stored grade), the response, each finding's `claimId`, the HV request, the response's status, the HV request's status, the item, and the eight receipt, reviewer, and runtime-identity checks of section 6.3 over the receipt files and the reviewer's own session-log turn. A failed check is answered with `details.check`; the reviewer makes its submission equal its verdict entry and its turn, or reruns the HV on the configured grader. EvaluateStalenessCommand then persists `Stale` when a dependency of the response or its package changed, so the trust decision in the SerializableWrite never trusts it (a `Stale` response stays gradable, and the decision leaves it `Stale`); when that dispatch's own write exhausts its four attempts, the grade answers 409 `conflict` with `retryable: true` and saves nothing. The response's `StatusVersion` and the HV request's state that the SerializableWrite re-checks are the ones read after that dispatch.

```mermaid
sequenceDiagram
    autonumber
    participant Reviewer
    participant Controller
    participant Handler
    participant Store
    Handler->>Store: IdempotentCreate.Find, read-only
    alt equal grade stored (a retried continuation)
        Controller-->>Reviewer: 200 the stored AgentResponseGradeResult
        Note over Reviewer: continue with the next item
    else a different grade stored
        Controller-->>Reviewer: 409 conflict, retryable false
        Note over Reviewer: report it, never overwrite a stored grade
    else absent
        Handler->>Store: load the response, the HV request, and its item
        alt response not found
            Controller-->>Reviewer: 404 not_found, details.resource response
        else a finding names a claim the response does not have
            Controller-->>Reviewer: 400 validation_error, details.field
        else HV request not found
            Controller-->>Reviewer: 404 not_found, details.resource hv_request
        else response Rejected or Superseded
            Controller-->>Reviewer: 409 agent_context_response_not_gradable
        else HV request Completed
            Controller-->>Reviewer: 409 agent_context_hv_request_closed
        else response not an item of the HV request
            Controller-->>Reviewer: 409 agent_context_grade_not_in_hv_request
        else gradable item of an Open HV request
            Handler->>Handler: read the receipts, run the eight checks of section 6.3
            alt a check fails
                Controller-->>Reviewer: 422 agent_context_receipt_unverified, details.check
                Note over Reviewer: match the verdict entry and turn, or rerun the HV
            else all eight pass
                Handler->>Handler: SendAsync(EvaluateStalenessCommand), this response
                alt its write exhausted the retries
                    Controller-->>Reviewer: 409 conflict, retryable true, nothing saved
                    Note over Reviewer: resubmit the same grade
                else evaluated: fresh, or Stale persisted
                    Note over Handler: the status and StatusVersion the write re-checks
                end
            end
        end
    end
```

#### Sequence: submit RejectAgentResponseCommand

`ResponseStatusTransition` (section 3.5.5) applies the side effects in the same transaction: the projection row becomes `Pending` for removal when it holds a `GraphRagDocumentId` (otherwise `Removed`), the response's `Pending` items in `Open` HV requests become `Closed` (`rejected`), and each request left with no `Pending` item becomes `Completed` with `CompletedUtc`.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: POST /responses/{responseId}/reject (reason)
    Controller->>Handler: SendAsync(RejectAgentResponseCommand)
    Note over Controller,Handler: ref RequestPipeline, gated
    alt blank reason
        Controller-->>Caller: 400 validation_error, details.field reason
    else reason given
        Note over Handler,Store: SerializableWrite, responseId locked
        Handler->>Store: ResponseStatusTransition(responseId, Rejected, reason)
        alt response not found
            Controller-->>Caller: 404 not_found, details.resource response
        else already Rejected (same transition)
            Controller-->>Caller: 200 AgentResponseResult, unchanged
        else Superseded (the lifecycle does not allow it)
            Controller-->>Caller: 409 agent_context_transition_invalid
        else transitioned
            Controller-->>Caller: 200 AgentResponseResult, status Rejected
            Note over Caller: excluded from trusted retrieval
        else retries exhausted
            Controller-->>Caller: 409 conflict, retryable true, nothing saved
            Note over Caller: retry the same request within the bounded retries of C9
        end
    end
```

#### Sequence: submit SupersedeAgentResponseCommand

Both responses are locked in ascending `ResponseId` order. `ResponseStatusTransition` sets the original `Superseded` with its side effects (projection row, HV items `Closed` with `superseded`), and `RelationshipWriter` writes the `supersedes` link and checks the cycle in the same transaction, so the supersede chain has one data source (section 3.5.5).

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: POST /responses/{responseId}/supersede
    Note over Caller,Controller: supersededByResponseId, reason
    Controller->>Handler: SendAsync(SupersedeAgentResponseCommand)
    Note over Controller,Handler: ref RequestPipeline, gated
    alt self-reference or blank reason
        Controller-->>Caller: 400 validation_error, details.field
    else request valid
        Note over Handler,Store: SerializableWrite, both responses locked
        Handler->>Store: load the original and the replacement
        alt either not found
            Controller-->>Caller: 404 not_found, details.resource response
        else other request, or replacement Rejected or Superseded
            Controller-->>Caller: 409 agent_context_supersede_invalid
        else both valid
            Handler->>Store: ResponseStatusTransition(original, Superseded, replacement)
            alt already superseded by this replacement
                Controller-->>Caller: 200 AgentResponseResult, unchanged
            else original Rejected, or superseded by another response
                Controller-->>Caller: 409 agent_context_transition_invalid
            else transitioned
                Handler->>Store: RelationshipWriter: replacement supersedes original
                alt the link would close a cycle
                    Controller-->>Caller: 409 agent_context_supersede_invalid
                    Note over Caller: rolled back, nothing saved
                else link written
                    Controller-->>Caller: 200 AgentResponseResult, status Superseded
                    Note over Caller: retrieval now returns the replacement only
                end
            end
        else retries exhausted
            Controller-->>Caller: 409 conflict, retryable true, nothing saved
            Note over Caller: retry the same request within the bounded retries of C9
        end
    end
```

#### Sequence: submit EvaluateStalenessCommand

This unit is the only writer of `Stale` (section 3.1, "Database-only delegates"). The two grade units dispatch it for their response after their read-only checks and before their own SerializableWrite, and ProjectOneResponse before its row saves (section 3.5.5, `IResponseStalenessRule`), so each reads the status it leaves. Each selected response is read first and written second (nested activity EvaluateOneResponseStaleness): `IResponseStalenessRule` reads its dependencies outside any transaction (files at the workspace HEAD commit; never the request's own turn or a Rule file outside version control), and a changed dependency persists `Stale` through `ResponseStatusTransition` in the response's own SerializableWrite, which locks it and re-checks the `StatusVersion` the read saw (a change ends the attempt, and that response is read again).

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: POST /staleness/evaluate (responseIds)
    Controller->>Handler: SendAsync(EvaluateStalenessCommand)
    Note over Controller,Handler: ref RequestPipeline, gated
    Handler->>Store: ResponseIdList(responseIds)
    alt malformed, over 1000 ids, or an id without rsp-
        Controller-->>Caller: 400 validation_error, details.field responseIds
    else selected (an empty list selects every Submitted, Grading, Trusted)
        loop each selected response
            Note over Handler,Store: ref EvaluateOneResponseStaleness: read, then SerializableWrite
        end
        alt every response evaluated
            Controller-->>Caller: 200 evaluatedCount, newlyStale, notFound
            Note over Caller: list each stale response with its changed ref
        else a response's write exhausted its retries
            Controller-->>Caller: 409 conflict, retryable true
            Note over Caller: retry the same request within the bounded retries of C9
        end
    end
```

#### Sequence: submit CreateAgentContextRelationshipCommand

`SourceResolver` resolves both refs and hashes the target before the SerializableWrite, whose delegate reads and writes only the database (section 3.1, "Database-only delegates"). `RelationshipWriter` (section 3.5.5) records that hash as `ToSha256` and `CreatedBy` (the calling agent), returns an identical link as stored (IdempotentCreate with policy `ReturnStored`), and refuses a client's `supersedes` link; a client therefore never reaches the writer's cycle check, which only SupersedeAgentResponseCommand uses.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    participant Resolver as SourceResolver
    Caller->>Controller: POST /relationships (CreateAgentContextRelationshipRequest)
    Controller->>Handler: SendAsync(CreateAgentContextRelationshipCommand)
    Note over Controller,Handler: ref RequestPipeline, gated
    alt unknown kind or type, or a self-link
        Controller-->>Caller: 400 agent_context_relationship_invalid
    else link well formed
        Handler->>Resolver: resolve both refs, hash the target
        alt a ref names no existing source
            Controller-->>Caller: 400 validation_error, details.field the ref
        else both refs resolve
            Note over Handler,Store: SerializableWrite, database only
            Handler->>Store: RelationshipWriter(link, createdBy the agent)
            alt type supersedes
                Controller-->>Caller: 400 validation_error, details.field type
            else identical link stored
                Controller-->>Caller: 200 link, created false
            else new link
                Controller-->>Caller: 201 link, created true
            else retries exhausted
                Controller-->>Caller: 409 conflict, retryable true, nothing saved
                Note over Caller: retry the same request within the bounded retries of C9
            end
        end
    end
```

#### Sequence: submit DeleteAgentContextRelationshipCommand

A link a command created is protected because the staleness rule reads it or it holds the supersede chain: Build's `derived_from` and `relevant_to`, Submit's `derived_from` and `supports`, and Supersede's `supersedes`.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: DELETE /relationships/{relationshipId}
    Controller->>Handler: SendAsync(DeleteAgentContextRelationshipCommand)
    Note over Controller,Handler: ref RequestPipeline, gated
    Handler->>Store: load the link
    alt not found
        Controller-->>Caller: 404 not_found, details.resource relationship
    else made by a command (CreatedBy names it)
        Controller-->>Caller: 409 agent_context_relationship_protected
        Note over Caller: report the link as protected
    else made by a client
        Handler->>Store: delete the link
        Controller-->>Caller: 204
    end
```

#### Sequence: submit QueryAgentContextRelationshipsQuery

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: GET /relationships?nodeKind&nodeRef&type&direction&limit&offset
    Controller->>Handler: QueryAsync(QueryAgentContextRelationshipsQuery)
    Note over Controller,Handler: ref RequestPipeline, not gated: readable in Mode Off
    alt kind without ref, unknown type or direction, or outside PageBounds
        Controller-->>Caller: 400 validation_error, details.field
    else filters valid
        Handler->>Store: PagedQuery: out, in, or both, by type
        Store-->>Handler: PageResult, CreatedUtc then RelationshipId
        Controller-->>Caller: 200 PageResult of relationships
        Note over Caller: PagedQuery paging
    end
```

#### Sequence: submit RetrieveAgentContextCandidatesQuery

`Caller` is a surface, or BuildAgentContextPackageCommand's handler through `QueryAsync` in nested sequence ReadBuildInputs, before any transaction. The request carries `queryText`, `agent`, `sessionId`, `requestId`, `focusRefs`, `kinds`, `limit`, `trustedOnly`, and `preferGraphRag`. A named turn is `agent`, `sessionId`, and `requestId`, all three or none, and is never itself a candidate. Nested sequence ChooseRetrievalPath gathers the candidates. Every `PriorResponse` candidate is then evaluated by `IResponseStalenessRule` read-only, so only `Trusted` and fresh responses are kept and nothing is written; `CandidateRanker` (section 3.5.5) orders by score, then `sourceRef`, and the result is cut to `limit`. The same inputs give the same candidates on the relational path.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: POST /retrieve (RetrieveAgentContextCandidatesRequest)
    Controller->>Handler: QueryAsync(RetrieveAgentContextCandidatesQuery)
    Note over Controller,Handler: ref RequestPipeline, not gated: readable in Mode Off
    alt trustedOnly false for PriorResponse, partial turn key, or limit outside PageBounds
        Controller-->>Caller: 400 validation_error, details.field
    else request valid
        opt turn named
            Handler->>Store: TurnLocator(agent, sessionId, requestId)
        end
        alt InvalidIdentifier
            Controller-->>Caller: 400 validation_error, details.field
        else SessionNotFound or TurnNotFound
            Controller-->>Caller: 404 not_found, details.resource session_turn
        else no turn named, or the turn found
            Note over Handler,Store: ref ChooseRetrievalPath (below): candidates, retrievalPath, fallbackReason
            Handler->>Handler: IResponseStalenessRule, read-only
            Handler->>Handler: CandidateRanker, then cut to limit
            Controller-->>Caller: 200 retrievalPath, fallbackReason when one applied
            Note over Caller: use the candidates in rank order
        end
    end
```

##### Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence)

The GraphRAG path answers only the `PriorResponse` kind, because GraphRAG holds only projected responses, and queries only `GraphRagStorageScope.Workspace`; it keeps hits that name a response of this workspace, one hit per response, scored by the GraphRAG score. `preferGraphRag` false takes the relational path with no `fallbackReason`. Every fallback takes the relational path and reports its `fallbackReason`: `retrieval_disabled` (`GraphRag:UseForRetrieval` false), `graphrag_kinds_unsupported` (`kinds` absent or naming another kind), `graphrag_disabled`, `graphrag_not_initialized`, `graphrag_not_indexed`, or `index_in_progress` (from the status), and the query result's reason, or `graphrag_error` on an exception. The relational path ranks by the links to the named turn, its TODO, its requirements, and the focus refs first, then by the hybrid search.

```mermaid
sequenceDiagram
    autonumber
    participant Handler
    participant Store
    participant GraphRAG
    opt preferGraphRag, GraphRag:UseForRetrieval, and kinds only PriorResponse
        Handler->>GraphRAG: GetStatusAsync
        GraphRAG-->>Handler: enabled and indexed, or a fallbackReason
    end
    alt GraphRAG enabled and indexed
        Handler->>GraphRAG: QueryAsync, workspace scope
        alt result ok
            GraphRAG-->>Handler: hits
            Note over Handler: one hit per response of this workspace
        else FallbackUsed or an exception
            GraphRAG-->>Handler: fallbackReason, or graphrag_error
            Handler->>Store: relational path
            Store-->>Handler: candidates
        end
    else not preferred, or a fallbackReason from the request or the status
        Handler->>Store: relational path: links first, then hybrid search
        Store-->>Handler: candidates
    end
```

#### Sequence: submit ProjectAgentResponsesCommand

`Caller` is a surface, or the background pass that runs after a projection row is marked `Pending`: the unit whose transaction marked the row dispatches the same command with no `responseIds` through `IDispatcher`, without the controller, after its SerializableWrite commits, and logs a failed dispatch without failing (section 3.1, Projection pass). The selection includes `Failed` rows, so a failed ingest or a failed delete is retried on the next pass; the projection row states are the state diagram of section 6.3.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: POST /projection/graphrag (responseIds)
    Controller->>Handler: SendAsync(ProjectAgentResponsesCommand)
    Note over Controller,Handler: ref RequestPipeline, gated
    Handler->>Store: ResponseIdList(responseIds)
    alt malformed, over 1000 ids, or an id without rsp-
        Controller-->>Caller: 400 validation_error, details.field responseIds
    else selected (an empty list selects every Pending and Failed row, and Skipped rows once ingest is enabled)
        loop each selected response
            Note over Handler,Store: ref ProjectOneResponse (below)
        end
        alt every response handled
            Controller-->>Caller: 200 projected, removed, skipped, failed, notFound
            Note over Caller: Failed rows are selected again on the next pass
        else a row write exhausted its retries
            Controller-->>Caller: 409 conflict, retryable true
            Note over Caller: retry the same request within the bounded retries of C9
        end
    end
```

##### Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence)

Staleness belongs to ResponsesAndTrust, so the projection (ContextGraph) dispatches EvaluateStalenessCommand for the response first. The GraphRAG calls run outside any transaction; every row save runs afterwards in SerializableWrite with the response locked, reads and writes only the database, and re-reads the response status there (section 3.1, "Database-only delegates"). `ProjectionEnabled` false skips ingest only; a removal still runs while GraphRAG is enabled, and while GraphRAG is disabled a row awaiting removal stays `Pending`.

```mermaid
sequenceDiagram
    autonumber
    participant Handler
    participant Store
    participant GraphRAG
    Handler->>Handler: SendAsync(EvaluateStalenessCommand), this response
    Note over Handler: fresh, or Stale persisted
    Handler->>Store: read the response status and its projection row
    alt Trusted, not projected at its ContentSha256
        alt ProjectionEnabled and GraphRAG enabled
            Handler->>GraphRAG: IngestTextAsync, workspace scope, root ids
            GraphRAG-->>Handler: document id, or an error
            Handler->>Store: SerializableWrite: re-read the status and the row, save the row
            alt still Trusted, ingest ok
                Note over Store: Projected, document id and hash stored
            else row already Projected by an overlapping pass, same content
                Note over Store: the stored document kept
                Handler->>GraphRAG: DeleteDocumentAsync, the document this pass ingested
            else no longer Trusted, ingest ok
                Note over Store: keeps the document id, Pending for removal
            else ingest error
                Note over Store: Failed with LastError
            end
        else ProjectionEnabled false, or GraphRAG disabled
            Handler->>Store: SerializableWrite: Skipped with skippedReason
        end
    else not Trusted, the row holds a GraphRagDocumentId
        alt GraphRAG enabled
            Handler->>GraphRAG: DeleteDocumentAsync
            GraphRAG-->>Handler: ok, or an error
            Handler->>Store: SerializableWrite: Removed, or Failed with LastError
        else GraphRAG disabled
            Note over Store: stays Pending until GraphRAG is enabled
        end
    else not Trusted, no document
        Handler->>Store: SerializableWrite: Removed, never selected again
    else nothing to change
        Note over Handler: continue with the next response
    end
```

#### Sequence: stateless invocation end to end (process RunStatelessInvocation)

`Host` is the agent host, `Plugin` its PowerShell plugin core (`Invoke-PluginAgentContextInvocation` in `plugins/core/lib-ps/repl-invoke.ps1`, the dot-sourceable library of section 3.5.6, which the prompt hook's `plugin-hook.ps1` dot-sources), `Server` the QBrain.AI server reached through the REPL (the session-log and agent-context units), and `Model` the host's model call. The main sequence resolves the turn and calls two nested sequences, BuildAndDeliverPackage and SubmitAndGradeResponse. The decisions of section 3.1 (2026-10-12) apply:

- Mode: read once at host session start (SessionStart, or the first prompt on cline, cline-v2, and opencode) with GetAgentContextStatusQuery and cached as `agentContextMode` in the host session's `turn-state.yaml`; `Off` when it cannot be read, including a read that misses the hook bound (GetAgentContextStatusQuery sequence above). A 409 `agent_context_disabled` from the build or the submission caches `Off` and continues statefully (no refusal, no `canceled` close); every other build error fails the invocation, including `transaction_degraded` and `mutation_not_transactional` from the REPL's transaction-gated client policy (section 3.1, "Transaction-gated REPL clients").
- Bridge hosts (claude-code, claude-cowork): the prompt hook applies pending transcript records through `Sync-PluginArrivalLog` up to the transcript length at its own start and takes the bridge-minted `requestId` of the delivered request; when that request's `enqueue` record is not yet in the transcript, the invocation fails (prompt refused, as for a build failure).
- A message absorbed into a running turn (`absorbed_mid_turn`) runs as `Pilot`: the package is built and kept, never injected, and the prompt is never refused.
- A pending mint (transport outage) fails the invocation like a build outage, and the `canceled` close of the pending id is queued in the failsafe behind its mint record; only a host session reported `turn_logging_unsupported` (D6) stays stateful, and an `unsupported` mint outcome or a `merged` one with a terminal turn runs nothing. A final mint refusal leaves no turn: the prompt is refused and the Stop gate warns the refusal, naming the prompt; a hook that mints nothing (`nonce_source_missing`, `turn-open-contended`) is refused the same way. A duplicate delivery (`merged` for an in-progress turn, `turn-already-open`) runs the build again with the same idempotency key and client inputs, which returns the stored package and never refuses (section 6.6, `StatelessInvocationDuplicateDeliverySecondBuildReturnsStoredPackageNoSpuriousRefusal`).
- A host whose prompt hook cannot refuse a prompt, or that runs the prompt anyway after a hook timeout, crash, or `plugin_core_unavailable`, runs `Pilot` and `Stateless` as `Off` (feature-matrix limitation, D7).
- The plugin keeps the request's `packageId` in its per-turn file `turns/<requestId>.yaml`; the session-log turn gains no field. The model submits its response with the `agent_context_submit_response` tool before it closes the turn, and the plugin supplies the kept `packageId` (found again with QueryAgentContextPackagesQuery `{ requestId, status: Built }` when a hook process boundary lost it). A missing submission is not a Stop-gate item; the pilot report counts requests with a package and no response.
- The package-failure `canceled` close is written by the plugin, not the model, on the persist path of process PersistTurnWriteWithReceipt with status `canceled` and the failure reason; `Test-ModelEvidence` never applies to it. An outage or any other retryable answer (409 `conflict` with `retryable: true`, or a busy store's 503 `persistence_error`; section 3.1, "Retryable answers") holds it in the failsafe (receipt `queued`), and the Stop gate warns degraded persistence naming the package failure (never blocks), keeping the ledger entry until the drain confirms the close.
- History exclusion and memories (FR-MCP-MEMORY-003, -007): in `Stateless` the model's input is the rendered package, the current request, and the deterministic `REQUIRED MEMORIES` block the host already injects; history stays excluded, and within one request's tool loop the request's own prompt, tool calls, and results stay on each later model call (`StatelessToolLoopOwnToolCallsKeptPriorRequestsExcluded`). The message list the host passes to its model call (for opencode, after the adapter applies the core's `output.messages`), captured by the host test harness, is the observation point of `StatelessHostSentinelPriorTurnAbsentFromModelRequest`, which finds the sentinel prior turn absent and the package, the current request, and the `REQUIRED MEMORIES` block present.

```mermaid
sequenceDiagram
    autonumber
    participant Operator
    participant Host
    participant Plugin
    participant Server
    participant Model
    Note over Plugin,Server: host session start: GetAgentContextStatusQuery, Mode cached, Off when unreadable
    Operator->>Host: prompt
    Host->>Plugin: prompt hook
    alt bridge host (claude-code, claude-cowork)
        Note over Plugin,Server: ref ApplyArrivalBridgeRecord through Sync-PluginArrivalLog: bridge-minted requestId
    else other hosts
        Note over Plugin,Server: ref OpenTurnFromPromptHook: MintSessionTurnCommand, Resolve-MintOutcome
    end
    alt Mode Off, D6 unsupported, unsupported or terminal merge, or the host cannot refuse
        Host->>Model: prompt with history (stateful, as today)
    else mint pending (outage)
        Plugin-->>Host: refuse the prompt with the outage
        Host-->>Operator: prompt refused, no answer
        Note over Plugin,Server: ref PersistTurnWriteWithReceipt: canceled close queued behind the mint record
    else bridge host, enqueue record not yet in the transcript
        Plugin-->>Host: refuse the prompt, as for a build failure
        Host-->>Operator: prompt refused, no answer
    else final mint refusal, or no mint (nonce_source_missing, turn-open-contended)
        Plugin-->>Host: refuse the prompt, no turn exists
        Host-->>Operator: prompt refused, no answer
        Note over Plugin: the Stop gate warns the refusal, naming the prompt
    else delivered, or a duplicate delivery, in Pilot or Stateless (absorbed_mid_turn runs as Pilot)
        Note over Operator,Model: ref BuildAndDeliverPackage (below), a failed build ends the invocation there
        Note over Host,Model: ref SubmitAndGradeResponse (below), after a Built package
    end
```

##### Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence)

The plugin builds with the bound `agent`, `sessionId`, and `requestId`, the prompt text, and idempotency key `requestId`, and keeps the returned `packageId` in its per-turn file `turns/<requestId>.yaml`. `Stateless` on a prompt delivery, on a host whose P0.2 row records an authoritative history-exclusion operation, replaces the model's input through that operation (opencode's `output.messages`, or the model request cline-v2's `beforeModel` returns where its P0.2 row records request replacement, section 3.2), or starts a fresh host invocation, with the package, the request, and the `REQUIRED MEMORIES` block; `Pilot`, an absorbed message, and a host without history exclusion (which runs `Stateless` as `Pilot`) answer statefully. A 422 (also a retried overflow), any other error, an outage (a retryable 409 `conflict` or a busy store's 503 included, section 3.1, "Deadlock and busy outcomes"), or a package not `Built` fails the invocation (operator: "Fail the invocation"): the plugin, not the model, closes the request's own turn `canceled` with the `failureNote` `agent_context_invocation_failed` followed by the code and message (section 6.6) on the persist path of process PersistTurnWriteWithReceipt, with no `Test-ModelEvidence`; a confirmed close leaves `openDelivered` and the Stop ledger, and a close held in the failsafe by an outage or a retryable answer (receipt `queued`) makes the Stop gate warn degraded persistence naming the package failure, never block, and keep the ledger entry until the drain confirms the close. An absorbed message has no prompt to refuse: its failure is recorded in the per-turn file and nothing is refused or closed.

```mermaid
sequenceDiagram
    autonumber
    participant Operator
    participant Host
    participant Plugin
    participant Server
    participant Model
    Plugin->>Server: BuildAgentContextPackageCommand, idempotencyKey requestId
    alt 201, or 200 with package status Built
        Plugin->>Plugin: keep packageId in the per-turn file
        alt Stateless prompt delivery, host with history exclusion
            Plugin-->>Host: output.messages: package, request, required memories
            Host->>Model: that message list, no history
        else Pilot, absorbed_mid_turn, or no history exclusion
            Host->>Model: prompt with history (stateful)
        end
    else 409 agent_context_disabled
        Note over Plugin: caches Off: no refusal, no canceled close
        Host->>Model: prompt with history (stateful)
    else 422, any other error, an outage, or a package not Built
        alt absorbed_mid_turn
            Note over Plugin: failure kept in the per-turn file, nothing refused
        else prompt delivery
            Plugin-->>Host: refuse the prompt with the code and message
            Host-->>Operator: prompt refused, no answer
            Note over Plugin,Server: ref PersistTurnWriteWithReceipt: own turn canceled, no Test-ModelEvidence
            opt close queued: an outage or a retryable answer
                Note over Plugin: Stop gate warns degraded persistence, never blocks
            end
        end
    end
```

##### Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence)

The model calls the `agent_context_submit_response` tool before it closes its turn; the plugin binds the call to its request as it binds a verb (process BindVerbToTurn) and supplies the kept `packageId`, or finds it again with QueryAgentContextPackagesQuery `{ requestId, status: Built }` when a hook process boundary lost it. `invocationMode` is `Stateless` only when history was excluded. A failed submission or grade is reported in the tool result and writes nothing to the turn; a missing submission is not a Stop-gate item, and the pilot report counts requests with a package and no response. The model grade comes later from an HV request covering the turn.

```mermaid
sequenceDiagram
    autonumber
    participant Model
    participant Plugin
    participant Server
    Model->>Plugin: agent_context_submit_response tool call
    Plugin->>Server: SubmitAgentResponseCommand, kept packageId, invocationMode
    alt 201 or 200
        Plugin->>Server: RunDeterministicGradeCommand
        alt 201
            Server-->>Plugin: grade, responseStatus Grading or Stale
            Plugin-->>Model: tool result: the model grade comes from a later HV request
        else an error or an outage
            Plugin-->>Model: tool result with the code, the grade is re-run on request
        end
    else 409 agent_context_disabled
        Plugin-->>Model: tool result, agent_context_disabled
        Note over Plugin: caches Off, the turn closes as any stateful turn
    else any other error or an outage
        Plugin-->>Model: tool result with the code and message
        Note over Model: resubmits or reports it
    end
```

#### Sequence: submit GetAgentContextPilotReportQuery

The report groups the window's responses by `invocationMode`, counts them by effective status through `IResponseStalenessRule` read-only (nothing written), and also counts the `Overflow` packages, the refused prompts, and the requests with a package and no response; the operator compares the modes on it before changing any default (section 3.1, the pilot comparison).

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: GET /pilot/report?fromUtc&toUtc
    Controller->>Handler: QueryAsync(GetAgentContextPilotReportQuery)
    Note over Controller,Handler: ref RequestPipeline, not gated: readable in Mode Off
    alt fromUtc or toUtc missing or not UTC, or from not before to
        Controller-->>Caller: 400 validation_error, details.field
    else window valid
        Handler->>Store: group the window's responses by invocationMode
        Store-->>Handler: counts, latest grades by RecordedUtc
        Controller-->>Caller: 200 AgentContextPilotReport
        Note over Caller: compare the modes before changing any default
    end
```

#### Sequence: submit CreateAgentContextHvRequestCommand

`Caller` is any agent; the request carries `sessionId`, `agent`, `fromRequestId`, `toRequestId`, `requestedBy`, `reason`, and `idempotencyKey`. Every step reads only the database, so the lookup, the session and range checks, the candidate locks (every candidate response in ascending `ResponseId` order, then a re-read of its status), and the insert run in one SerializableWrite delegate, and a concurrent create with the same key is found by the rerun's lookup and a concurrent reject or supersede commits wholly before or after.

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: POST /hv-requests (CreateAgentContextHvRequestRequest)
    Controller->>Handler: SendAsync(CreateAgentContextHvRequestCommand)
    Note over Controller,Handler: ref RequestPipeline, gated
    alt blank field
        Controller-->>Caller: 400 validation_error, details.field
    else fields given
        Note over Handler,Store: SerializableWrite: one delegate from the key lookup to the save
        Handler->>Store: IdempotentCreate.Find(idempotencyKey, request fingerprint)
        alt same inputs (a retry or a concurrent winner)
            Controller-->>Caller: 200 the stored AgentContextHvRequestResult
        else other inputs
            Controller-->>Caller: 409 conflict, retryable false
        else absent
            Handler->>Store: TurnLocator(agent, sessionId), both endpoint turns
            alt InvalidIdentifier
                Controller-->>Caller: 400 validation_error, details.field
            else unknown session, bad range, too long, or no gradable response
                Controller-->>Caller: 400 agent_context_hv_request_invalid, reason
                Note over Caller: correct the range
            else range valid
                Handler->>Store: lock candidates in ascending ResponseId, re-read status
                Handler->>Store: IdempotentCreate.Insert request (Open), Pending items
                Controller-->>Caller: 201 AgentContextHvRequestResult with items
                Note over Caller: hand hvRequestId to the HV agent in its brief
            end
        else retries exhausted (SerializableWrite or key allocation)
            Controller-->>Caller: 409 conflict, retryable true, nothing saved
            Note over Caller: retry with the same idempotencyKey within the bounded retries of C9
        end
    end
```

#### Sequence: submit GetAgentContextHvRequestQuery

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: GET /hv-requests/{hvRequestId}
    Controller->>Handler: QueryAsync(GetAgentContextHvRequestQuery)
    Note over Controller,Handler: ref RequestPipeline, not gated: readable in Mode Off
    Handler->>Store: load the HV request with its items
    alt not found
        Controller-->>Caller: 404 not_found, details.resource hv_request
    else found
        Controller-->>Caller: 200 AgentContextHvRequestResult
        Note over Caller: list Pending items still awaiting the HV agent
    end
```

#### Sequence: submit QueryAgentContextHvRequestsQuery

```mermaid
sequenceDiagram
    autonumber
    participant Caller
    participant Controller
    participant Handler
    participant Store
    Caller->>Controller: GET /hv-requests?sessionId&status&limit&offset
    Controller->>Handler: QueryAsync(QueryAgentContextHvRequestsQuery)
    Note over Controller,Handler: ref RequestPipeline, not gated: readable in Mode Off
    alt unknown status, or limit or offset outside PageBounds
        Controller-->>Caller: 400 validation_error, details.field
    else filters valid
        Handler->>Store: PagedQuery, CreatedUtc descending then HvRequestId
        Store-->>Handler: PageResult
        Controller-->>Caller: 200 PageResult of HV requests without items
        Note over Caller: PagedQuery paging
    end
```

## 7. Agent-context use cases

### 7.11 Use case: Build a context package for a request [FR-MCP-AGENTCONTEXT-001, -002]

Actors: agent host and plugin, server. Precondition: the request's turn exists (UC-1) and the mode is `Pilot` or `Stateless`. Basic flow: the plugin asks the server to build a package for the request, named by `agent`, `sessionId`, and `requestId` (TurnLocator); the server gathers the request, profile, rules, applicable requirements, related turns (never the request's own turn), TODOs, receipts, trusted prior responses, and code at the current commit (SourceResolver), filters secrets (SecretRedactor) and untrusted items, ranks (CandidateRanker), fills the budget, records provenance and an input snapshot holding every input the assembler reads (PackageAssembler), and returns the rendered package; the plugin keeps the `packageId` in its per-turn file `turns/<requestId>.yaml`. Alternate flows: mandatory context exceeds the budget and the build fails with `agent_context_budget_exceeded`, storing an `Overflow` package, and a retried or concurrent build returns the same 422 with the stored diagnostics; the same idempotency key with the same inputs returns the same package, found before any precondition that can change (IdempotentCreate), and the same key with other inputs is 409 `conflict` with `retryable: false`; the commit is unreadable (409 `agent_context_commit_unavailable`); no profile or rule file resolves (409 `agent_context_profile_unavailable`); the Mode was switched to `Off` after the plugin cached it (409 `agent_context_disabled`, ModeGateBehavior), and the plugin caches `Off` and continues statefully; replay proves the package is reproducible from its input snapshot.

```mermaid
flowchart LR
    HO(["Agent host and plugin"]) --- U(("Build a context package (BuildAgentContextPackageCommand)"))
    SV(["Server"]) --- U
    U -. include .-> I1(("Retrieve candidates (RetrieveAgentContextCandidatesQuery)"))
    U -. include .-> I2(("Assemble: secrets, omissions, findings, budget, rendering (PackageAssembler)"))
    E1(("Mandatory over budget: 422, invocation fails")) -. extend .-> U
    E2(("Repeated key: same package (IdempotentCreate)")) -. extend .-> U
    E3(("Replay proves determinism (ReplayAgentContextPackageQuery)")) -. extend .-> U
    E4(("Commit or profile unavailable: 409")) -. extend .-> U
    E5(("Mode switched to Off: 409 agent_context_disabled, plugin continues statefully")) -. extend .-> U
```

### 7.12 Use case: Submit and grade an agent response [FR-MCP-AGENTCONTEXT-003, -004]

Actors: model, plugin, server, hostile reviewer. Basic flow: in `Pilot` and `Stateless` the model submits its response with its claims and evidence through the `agent_context_submit_response` tool (SubmitAgentResponseCommand) before it closes the turn, and the plugin supplies the request's `packageId` (kept in its per-turn file `turns/<requestId>.yaml`, or found again with QueryAgentContextPackagesQuery `{ requestId, status: Built }` when a hook process boundary lost it; the package is found by BuiltPackageLookup); the deterministic grade runs (RunDeterministicGradeCommand); an agent creates an HV request covering the turn (CreateAgentContextHvRequestCommand) and the HV agent records the model grade (RecordModelGradeCommand) after the eight receipt, reviewer, and runtime-identity checks; after each grade the trust decision (DecideAgentResponseTrustCommand) marks the response `Trusted` (ResponseStatusTransition) when the latest grade of each kind (latest by the server-stamped `RecordedUtc`, then `GradeId`) passes at `max(configured, 0.98)` with zero FAIL and zero UNKNOWN (ScoreVerdict). Alternate flows: a grade fails and the response stays `Grading` with every failure shown; a grader other than the configured one is refused; the operator rejects the response (RejectAgentResponseCommand): a repeated reject returns it unchanged, and a reject of a `Superseded` response is refused with 409 `agent_context_transition_invalid` (ResponseStatusTransition); a dependency changes after the deterministic grade, and the model grade's staleness evaluation (EvaluateStalenessCommand, which the grade dispatches before its own write and which alone persists `Stale`) marks the response `Stale`, which the trust decision, reading only the database, leaves `Stale`, never `Trusted`; evidence that cites the request's own turn is refused with 400 `validation_error`; an HV request retried with the same idempotency key returns the stored request, and a response already `Pending` in another open HV request is not listed again; a write that spends SerializableWrite's four attempts is refused with 409 `conflict` (`retryable: true`) and saves nothing; a submission answered 409 `agent_context_disabled` (Mode switched to `Off`) makes the plugin cache `Off` and continue statefully; a request with a package and no submitted response is not a Stop-gate item, and the pilot report counts it.

The diagram's short labels map to the flows above: the model submits through the `agent_context_submit_response` tool; the trust decision applies ResponseStatusTransition; an operator reject runs RejectAgentResponseCommand, and a reject of a `Superseded` response is the 409; an HV request retried with its idempotency key returns the stored request; a request with no submission is counted by the pilot report.

```mermaid
flowchart LR
    MD(["Model"]) --- U(("Submit and grade a response"))
    PL(["Plugin"]) --- U
    HR(["Hostile reviewer"]) --- U
    SV(["Server"]) --- U
    U -. include .-> I1(("Submit (SubmitAgentResponseCommand)"))
    U -. include .-> I2(("Deterministic grade (RunDeterministicGradeCommand)"))
    U -. include .-> I3(("Model grade (RecordModelGradeCommand)"))
    U -. include .-> I4(("Trust (DecideAgentResponseTrustCommand)"))
    E1(("Grade fails: stays Grading")) -. extend .-> U
    E2(("Unconfigured grader: 403")) -. extend .-> U
    E3(("Operator reject (of a Superseded one: 409)")) -. extend .-> U
    E4(("Dependency changed: Stale, not Trusted")) -. extend .-> U
    E5(("HV request retried: stored request")) -. extend .-> U
    E6(("No submission: not a Stop-gate item")) -. extend .-> U
```

### 7.13 Use case: Retrieve trusted prior context [FR-MCP-AGENTCONTEXT-007]

Actors: server (inside package construction), client apps. Basic flow: retrieval returns ranked candidates (CandidateRanker on the relational path), only trusted responses among prior responses, preferring GraphRAG when the caller asks for it (`preferGraphRag`; a package build asks with the value of `GraphRag:UseForRetrieval` and records it, so its ranking inputs are part of its provenance); a request that names a turn names it by `agent`, `sessionId`, and `requestId` (TurnLocator), and `limit` is bounded (PageBounds `Refuse`). Every read checks staleness read-only and writes nothing (IResponseStalenessRule), and the query answers in every Mode, `Off` included, because ModeGateBehavior gates commands only. Alternate flow: GraphRAG disabled, uninitialized, not indexed, failing, or asked for kinds other than `PriorResponse`, and the deterministic relational path answers with the fallback reason; a `limit` outside 1..1000 is refused with 400 `validation_error`.

```mermaid
flowchart LR
    SV(["Server: package builder"]) --- U(("Retrieve trusted prior context (RetrieveAgentContextCandidatesQuery)"))
    CA(["Client apps"]) --- U
    U -. include .-> I1(("Read-only staleness check of each prior response (IResponseStalenessRule)"))
    E1(("GraphRAG unavailable or asked for other kinds: relational fallback with reason")) -. extend .-> U
    E2(("Mode Off: still answers (read-only query)")) -. extend .-> U
```

### 7.14 Use case: Maintain relationships, staleness, and supersession [FR-MCP-AGENTCONTEXT-005, -006]

Actors: model, operator, server. Basic flow: links are created between packages, responses, requirements, turns, TODOs, code, and receipts (RelationshipWriter, which records each target's current hash as the calling unit read it before its write); when a dependency changes, its dependents become stale (IResponseStalenessRule; only EvaluateStalenessCommand persists `Stale`, through ResponseStatusTransition, and every read reports the effective status and writes nothing): a file dependency is compared at the workspace's HEAD commit, so an uncommitted edit changes nothing and committing it does, while a Rule file outside version control and the request's own turn are never dependencies; a newer response supersedes an older one (SupersedeAgentResponseCommand, the only writer of a `supersedes` link). A requirement update or delete re-evaluates staleness (EvaluateStalenessCommand, dispatched by the requirement service after its commit for the responses that cite the changed requirement; a failed dispatch is logged and never fails the requirement write) only while the Mode is not `Off`; after a return from `Off` every read evaluates freshness itself. Alternate flows: invalid link, cycle, or unresolvable ref refused (an unresolvable ref is 400 `validation_error` naming it); a client request for a `supersedes` link is refused with 400 `validation_error` (`details.field` `type`); an identical link created again returns the stored link with `created: false`; a link a command made (Build's `derived_from`, Submit's `derived_from` and `supports`, Supersede's `supersedes`) cannot be deleted (409 `agent_context_relationship_protected`); a supersede of an original already `Rejected` or superseded by another response is refused with 409 `agent_context_transition_invalid` (ResponseStatusTransition), a supersede whose replacement is invalid or would close a cycle with 409 `agent_context_supersede_invalid`, and a repeat naming the same replacement returns it unchanged.

```mermaid
flowchart LR
    MD(["Model"]) --- U(("Maintain relationships and staleness"))
    OP(["Operator"]) --- U
    SV(["Server"]) --- U
    U -. include .-> I1(("Create, delete, query links (CreateAgentContextRelationshipCommand to QueryAgentContextRelationshipsQuery, RelationshipWriter)"))
    U -. include .-> I2(("Evaluate staleness at the HEAD commit (EvaluateStalenessCommand, IResponseStalenessRule)"))
    E1(("Supersede a response (SupersedeAgentResponseCommand)")) -. extend .-> U
    E2(("Invalid link, cycle, client supersedes link, or protected command-made link refused")) -. extend .-> U
    E3(("Supersede of a Rejected or already superseded response: 409 agent_context_transition_invalid")) -. extend .-> U
```

### 7.15 Use case: Run a stateless or pilot invocation [FR-MCP-AGENTCONTEXT-008, -009]

Actors: operator, agent host and plugin, model. Precondition: the plugin read the Mode at host session start (GetAgentContextStatusQuery, at SessionStart, or at the first prompt on cline, cline-v2, and opencode, which have no SessionStart event) and cached it as `agentContextMode` in the host session's `turn-state.yaml`; a Mode it cannot read, including a read that misses the SessionStart hook bound, or a value it does not know, is `Off`, and a change away from `Off` takes effect at the next host session start. Basic flow (process RunStatelessInvocation, run by the plugin's prompt hook, with the agent-context surfaces of FR-MCP-AGENTCONTEXT-008): in `Stateless` mode the plugin builds the package for a delivered request and the model answers from the rendered package, the current request, and the deterministic `REQUIRED MEMORIES` block the host already injects (section 3.1, "Memories in Stateless mode", FR-MCP-MEMORY-003, -007), with history excluded, on a host whose P0.2 row records history exclusion (any other host runs `Stateless` as `Pilot`); in `Pilot` mode the package is built and recorded but not injected, and the model answers statefully; the plugin keeps the `packageId` in its per-turn file `turns/<requestId>.yaml`, and the model submits its response with the `agent_context_submit_response` tool before it closes the turn (section 7.12); the pilot report compares the requests by `invocationMode` before any default changes (`Pilot` yields stateful responses, a period in `Stateless` on hosts with history exclusion yields stateless ones) and also counts `Overflow` packages, refused prompts, and requests with a package and no response. On claude-code and claude-cowork, where the arrival bridge is the only minter, the prompt hook first applies pending transcript records through `Sync-PluginArrivalLog` up to the transcript length at its own start and takes the bridge-minted `requestId` of the delivered request. Alternate flows: mode `Off` keeps today's behavior; a build or submission answered 409 `agent_context_disabled` (the Mode switched to `Off` during the host session) makes the plugin cache `Off` for the host session and continue statefully, with no refusal and no `canceled` close; a build failure (operator, 2026-10-11: "Fail the invocation"), any other build error (`transaction_degraded` and `mutation_not_transactional` from a transaction-gated REPL client included, section 3.1) or an outage included, refuses the prompt with the error code and message shown to the operator, produces no answer, and closes the turn `canceled` with that reason (written by the plugin on the persist path of process PersistTurnWriteWithReceipt; Test-ModelEvidence never applies to a `canceled` close); when an outage holds that close in the failsafe, the Stop gate warns degraded persistence naming the package failure, never blocks, and keeps the ledger entry until the drain confirms the close; a pending mint (transport outage) fails the invocation in the same way, and a final mint refusal leaves no turn, so the prompt is refused and the Stop gate warns the refusal naming the prompt; on a bridge host, a delivered request whose `enqueue` record is not yet in the transcript fails the invocation (prompt refused, as for a build failure); a message absorbed into a running turn (`absorbed_mid_turn`) runs as `Pilot`: the package is built and recorded, never injected, and the prompt is never refused; a host recorded `turn_logging_unsupported` (D6) stays stateful; a host whose prompt hook cannot refuse a prompt, or that runs the prompt anyway after a hook timeout, crash, or `plugin_core_unavailable`, treats `Pilot` and `Stateless` as `Off`, with the limitation in its feature-matrix row (P0.2 probe (j)); the pilot report and the other read-only agent-context queries stay readable in `Off`.

The diagram's short labels map to the flows above: "409 disabled" is 409 `agent_context_disabled`; the `canceled` close of a refused prompt is written on the persist path of process PersistTurnWriteWithReceipt; the pilot report is GetAgentContextPilotReportQuery; a bridge host's prompt hook syncs the transcript and takes the bridge-minted `requestId`; an absorbed message builds and records its package and never refuses the prompt.

```mermaid
flowchart LR
    OP(["Operator"]) --- U(("Run a stateless or pilot invocation (RunStatelessInvocation)"))
    HO(["Agent host and plugin"]) --- U
    MD(["Model"]) --- U
    U -. include .-> I1(("Build a context package for a request"))
    U -. include .-> I2(("Submit and grade an agent response"))
    E1(("Mode Off or 409 disabled: stateful")) -. extend .-> U
    E2(("Build fails or mint pending: refused, canceled")) -. extend .-> U
    E3(("Pilot report, readable in Off")) -. extend .-> U
    E4(("Bridge host: hook syncs the transcript")) -. extend .-> U
    E5(("Absorbed mid-turn: runs as Pilot")) -. extend .-> U
    E6(("Host cannot refuse a prompt: runs as Off")) -. extend .-> U
    E7(("Canceled close held by an outage: warn")) -. extend .-> U
```

### 7.16 Use case: Project responses into GraphRAG [FR-MCP-AGENTCONTEXT-007]

Actors: server (background pass), operator. Basic flow: after a unit commits a status change that marks a projection row (a grade's trust decision, a reject, a supersede, a staleness evaluation), it dispatches the pass (ProjectAgentResponsesCommand with no `responseIds`); a pass selects the `Pending` and `Failed` projection rows, and `Skipped` rows once ingest is enabled again (or the named `responseIds`, validated by ResponseIdList, with unknown ids returned in `notFound`); each selected response's staleness is evaluated first (EvaluateStalenessCommand, dispatched); trusted responses are ingested into GraphRAG's workspace scope, and for a response that stopped being trusted, including one whose dependency changed, a row that holds a GraphRAG document has it deleted and a row that holds none becomes `Removed`; each row is saved through SerializableWrite with its response locked, re-reading the response status after the GraphRAG call, so a reject that commits during an ingest leaves the new document marked for removal on the next pass (section 6.3, projection row states). Alternate flows: `ProjectionEnabled` false skips ingest only, and removal still runs while GraphRAG is enabled; GraphRAG disabled, so every ingest is skipped and a row awaiting removal stays `Pending` until GraphRAG is enabled; an ingest or delete error is recorded (`Failed`) and retried on the next pass; two passes that overlap keep one projected document per response, and the later save deletes the document it ingested.

```mermaid
flowchart LR
    SV(["Server background pass"]) --- U(("Project responses into GraphRAG (ProjectAgentResponsesCommand)"))
    OP(["Operator"]) --- U
    E1(("GraphRAG disabled: ingest skipped, removal stays Pending")) -. extend .-> U
    E2(("Ingest or delete error: Failed, retried next pass")) -. extend .-> U
    E3(("Dependency changed: removed, never projected")) -. extend .-> U
    E4(("ProjectionEnabled false: ingest skipped, removal still runs")) -. extend .-> U
    E5(("Reject during an ingest: document removed on the next pass (SerializableWrite)")) -. extend .-> U
```

## 8. Delivery

### 8.1 Phase order

```mermaid
flowchart TD
    ANZ["Analyzer branch chore/netanalyzers-all merged"] --> PRE["Prerequisites (sections 8.2 and 8.4)"]
    PRE --> REQ["Phase 1 Requirements: the whole catalog"]
    REQ --> DES["Phase 2 Interfaces and code stubs"]
    DES --> TST["Phase 3 TDD unit tests with mocks"]
    TST --> IMP["Phase 4 Implementation: a coding agent per context"]
    IMP --> VER["Integration, live checks, phase-wide HV, approval"]
```

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

```mermaid
flowchart LR
    SS["StorageSchema: first, alone"] --> SC["SharedComponents: second, alone"]
    subgraph Server["Server contexts, in parallel"]
        STL["SessionTurnLogging"]
        TR["TurnReviews"]
        CP["ContextPackages"]
        RT["ResponsesAndTrust"]
        CG["ContextGraph"]
        RA["ReplAndAgents"]
    end
    subgraph Plugins["Plugins and build, in parallel"]
        PS["PluginCorePowerShell"]
        BT["BuildTooling"]
    end
    SC --> STL
    SC --> TR
    SC --> CP
    SC --> RT
    SC --> CG
    SC --> RA
    PS --> HPA["HostPluginAdoption: nine plugin repos"]
    BT --> HPA
    STL --> DOC["Documentation"]
    TR --> DOC
    CP --> DOC
    RT --> DOC
    CG --> DOC
    RA --> DOC
    HPA --> DOC
```

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

```mermaid
flowchart TD
    S(["Merged plan approved"]) --> AZ["Analyzer branch merged first (section 8.1)"]
    AZ --> P0["Run the prerequisites, each under its own TODO and HV"]
    P0 --> PL[["For each of Phases 1, 2, and 3 (nested: RunOnePlanPhase)"]]
    PL -->|operator withholds approval| WAIT(["Stop: wait for the operator"])
    PL -->|Phases 1 to 3 approved| P4[["Phase 4 work packages until every context merges in the rollout order (nested: RunPhase4WorkPackages)"]]
    P4 --> INT["Integration gates, PluginSessionLogIntegration, live checks on Linux and Windows"]
    INT --> FHV[["Phase-wide HV and operator approval (nested: ValidateAndApprove)"]]
    FHV -->|approved| E(["Plan complete"])
    FHV -->|operator withholds approval| WAIT
```

##### Nested activity: RunOnePlanPhase (loop body of RunPlanPhases)

```mermaid
flowchart TD
    S(["Next phase: Requirements, Interfaces and stubs, or Unit tests with mocks"]) --> WK["Do the phase's work for the entire plan"]
    WK --> GT{"Phase gates green with zero failures and zero skips?"}
    GT -->|no| FX["Fix within the phase"]
    FX --> GT
    GT -->|yes| VA[["Phase HV and operator approval (nested: ValidateAndApprove)"]]
    VA -->|approved| OK(["Return phase approved: continue with the next phase"])
    VA -->|operator withholds approval| NO(["Return approval withheld"])
```

##### Nested activity: ValidateAndApprove (used by RunOnePlanPhase and RunPlanPhases)

```mermaid
flowchart TD
    S(["Validation requested"]) --> HV["Launch the HV (Codex gpt-6-astra max) and store the full verdict"]
    HV --> HR{"AGREE, both scores at least 98, every claim PASS?"}
    HR -->|no| SH["Show every FAIL and UNKNOWN to the operator, fix"]
    SH --> HV
    HR -->|yes| OA{"Operator approves?"}
    OA -->|yes| AP(["Return approved"])
    OA -->|no| WH(["Return approval withheld"])
```

##### Nested activity: RunPhase4WorkPackages (loop of RunPlanPhases)

A context is ready when every context with an arrow into it in section 8.3 has merged: StorageSchema first, then SharedComponents, before the server contexts; PluginCorePowerShell and BuildTooling from the start; HostPluginAdoption after both; Documentation last.

```mermaid
flowchart TD
    S(["Phase 4 starts"]) --> RDY["Launch a coding agent per ready context (process RunDomainCodingAgent)"]
    RDY --> MG["Merge each ready context in the rollout order, its gates re-run on the integration head"]
    MG --> ALL{"Every context merged in the rollout order?"}
    ALL -->|no| RDY
    ALL -->|yes| DONE(["Return every context merged"])
```

#### Process RunDomainCodingAgent: one Phase 4 work package

The coding agent's brief names the context's files, stubs, and tests, the section 3.5 components it calls through their Phase 2 seams (never re-implemented), and the standing rules.

```mermaid
flowchart TD
    S(["Context TODO ready: its dependencies merged"]) --> BR["Branch from the integration head: Phase 3 and every merged context"]
    BR --> AG["Brief the coding agent: files, stubs, tests, seams, standing rules"]
    AG --> IMPL[["Implement until the context's tests, gates, and HV pass (nested: ImplementBoundedContext)"]]
    IMPL -->|change needed outside the context's files| AMD(["Stop: raise a Phase 2 amendment for the operator"])
    IMPL -->|AGREE at 98/98, every claim PASS| MG(["Ready to merge in the rollout order"])
```

##### Nested activity: ImplementBoundedContext (loop of RunDomainCodingAgent)

```mermaid
flowchart TD
    S(["Implementation starts"]) --> P[["One implementation pass (nested: ImplementationPass)"]]
    P -->|change needed outside the context's files| AMD(["Return change outside the context's files"])
    P -->|a test, gate, or HV not yet passing| P
    P -->|AGREE at 98/98, every claim PASS| OK(["Return AGREE"])
```

##### Nested activity: ImplementationPass (loop body of ImplementBoundedContext)

```mermaid
flowchart TD
    S(["Pass starts"]) --> IM["Replace stubs with the real implementation"]
    IM --> OUT{"Change needed outside the context's files?"}
    OUT -->|yes| AMD(["Return change outside the context's files"])
    OUT -->|no| T3{"Context's Phase 3 tests green, unchanged?"}
    T3 -->|no| NP(["Return not yet passing"])
    T3 -->|yes| IT["Add the context's integration tests"]
    IT --> G{"Context gates green, zero failures and zero skips, Linux and Windows?"}
    G -->|no| NP
    G -->|yes| HV["Context HV (Codex gpt-6-astra max)"]
    HV --> HR{"AGREE at 98/98, every claim PASS?"}
    HR -->|no| NP
    HR -->|yes| OK(["Return AGREE"])
```

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
