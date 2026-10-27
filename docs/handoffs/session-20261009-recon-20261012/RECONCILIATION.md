<!-- SPDX-License-Identifier: Apache-2.0 -->
# Round-5 reconciliation: new requirements, existing requirements, code, tests, and projected problems

Date: 2026-10-12 (UTC 23:58). Scope: the merged plan PLAN-SESSIONTURNREVIEW-001 and its round-5 architecture (`docs/architecture/SessionTurn-Review-AgentContext-Architecture.md`, 5354 lines, 172 diagrams), reconciled against the live requirement store (exported read-only through the plugin: 353 FR, 487 TR, 512 TEST, 1037 mappings), develop code and tests at `a8d3934b`, and the pending analyzer branch `chore/netanalyzers-all`. Detail with citations is in the five lane reports beside this file: `req-ab.md`, `req-c.md`, `code-server.md`, `code-plugin.md`, `risk.md`.

## Verdict

The round-5 architecture is not ready for HV. The reconciliation found two blockers, a set of contradictions with live requirements that the plan does not list as updates, large test churn the plan does not budget, and thirteen inconsistencies left between the parallel-edited parts. Almost every fix is a requirement or AC change; four items need an operator decision (section 5).

## 1. New requirements against existing requirements

What holds:
- Every new id (21 FR, 25 TR, 23 TEST, the nine reserved FR-MCP-AGENTCONTEXT ids, 128 named Phase C cases) is free in the store, the projections, and develop; mapping coverage holds; the 26 projection line references the plan uses match.

Existing records the design contradicts that are NOT in the plan's "Updated existing requirements" list (each needs an update record, or the design must change):
- Session-id rotation and rebind: TR-MCP-PLUGIN-012, FR-MCP-XAGENT-001, FR-MCP-TRIAGEPLUGIN-001 (the design keeps a recorded MCP session on compaction and resume).
- `current-turn.yaml` targeting: TR-MCP-REPL-014, TR-MCP-REPL-020, TEST-MCP-REPL-029, FR-MCP-171, TR-MCP-PERSIST-002 (the design makes it a read-only legacy file).
- Failsafe drain: FR-MCP-REPL-011 (AC2, AC3), TR-MCP-REPL-016, TR-MCP-REPL-017, FR-MCP-SESSIONLIFE-004 (a rejected record must not block newer ones; quarantine at an attempt budget; exhausted legacy records replay), against the round-5 "drain stops at the first retryable answer, keeps every later record" and "every unbound record retained and warned".
- Deadlock and busy outcomes: TEST-MCP-TRIAGESTORE-005, TEST-MCP-TRIAGESTORE-007, TR-MCP-TRIAGEERR-001, FR-MCP-SESSIONLIFE-003 (ac6) expect a retryable 503-style outcome with a failsafe record; round 5 returns 409 `conflict` with no failsafe record.
- Error classification: FR-MCP-TRIAGEERR-001 and TR-MCP-TRIAGEERR-001 ("retryable false unless SQLITE_BUSY or deadlock"), the REPL classifier (`ReplMcpErrorClassifier.cs:60-95` forces `conflict` to non-retryable).
- Workspace resolution: TR-MCP-MT-001, TR-MCP-MT-002 (an unregistered workspace is 400 today, the new behavior answers 404), TR-MCP-MT-003, TR-MCP-CQRS-005 (claims a LoggingBehavior and ValidationBehavior that do not exist; round 5 adds the first production behaviors).
- Memory: FR-MCP-MEMORY-001 (global memories carry no workspace owner, so an empty workspace path is valid), FR-MCP-MEMORY-003 and -007 (overlap with Phase C retrieval).
- Sanitization and search: TR-MCP-SESSIONLOGSAN-001, FR-MCP-SESSIONLOGSAN-001 ac4 (search computed from raw data, against "a redacted review value matches nothing").
- Other: TR-MCP-SYNC-001 (Node vendoring), the bats, jest, and bash records the PowerShell-only ruling retires, FR-SUPPORT-012 (JSON-path validation bodies), TR-SUPPORT-CORE-014 ac-5 (review handler), FR-MCP-QBRAIN-004 and TR-MCP-NUKE-008 (thresholds on the deprecated `Mcp:` configuration root), FR-MCP-120 and TR-MCP-TXN-001 (turn transactions), FR-MCP-HOSTILEREVIEW-003 (its worker is pinned to effort xhigh while the grader requires max), FR-MCP-130, FR-MCP-103 federation and FR-MCP-WIKIEXPORT-003 (15 new tables to cover).

Overlaps that need a stated relationship (reuse, strengthen, or supersede): FR-SUPPORT-010 "context pack", FR-MCP-082 bounded Byrd execution context, the FR-MCP-HOSTILEREVIEW queue, `MemoryEdgeEntity` beside the new relationship store, develop's GraphRAG and handoff idempotency.

Status spelling: the architecture both accepts and refuses `cancelled` (ARCH:320 versus ARCH:560, :600), while TEST-MCP-TRIAGESTORE-004, FR-MCP-SESSIONLOGCTX-001, and FR-MCP-SESSIONLIFE-002 rely on it.

Traceability defects:
- The store holds no AC ids for any updated family, and the plan's `<REQID>-AC###` "existing format" exists nowhere in the store.
- `TEST-MERGEDPLAN-001` and `TEST-MCP-PLUGIN-PSONLY-001` would be read as TEST ids by the strict traceability run.
- Six multi-named ACs are missing from the ownership list; TEST-MCP-SESSIONLIFE-002 ac1 and ac2 have no owner; TR-MCP-PLUGIN-011, -013, TR-MCP-SESSIONLOG-003, TR-MCP-DTO-001 lack mapping rows; FR-MCP-CLEARSESSION-001 has no ACs and its cited "inline AC1" does not exist; `superseded` is not a writable status in the REPL validator.
- 17 Phase C error codes or branches have no named test.
- Use cases: every FR is covered, but the "0 existing use cases" baseline is stale (UC-125 exists).

## 2. New requirements against existing code and tests

Server, client, REPL, storage (develop `a8d3934b`):
- The completion gate (S10) is the largest break: about 196 existing tests write `completed` or `failed` turns without a class or reviews, at least 120 reach the real server path, and 8 assert the opposite outcome or a body the gate preempts (`SessionLogServiceTests:538,1230,1261`, integration `SessionLogControllerTests:386,420,701,733,777`). The plan relies on `SessionLogServiceReplaceDeleteTests:26,61` staying green "at every later gate"; both go red under the gated PUT. The QBAgent `mcp_session_turn_complete` tool (`QuadBrainInternalToolExecutor.cs:314-328`) and REPL `importRecovery` have no way to supply class or reviews.
- Develop rules the architecture omits that live tests pin: Submit refuses caller-owned transactions (`SessionLogService.cs:119-128`, 4 tests); the Serializable transaction is opened only on relational providers (`:155`), while 151 tests run the real service on EF InMemory; the SQL Server 1205 contract (`SessionLogDenseGraphTests:100`) and the busy-exception race (`SessionLogSupersessionTests:216`) change.
- WorkspaceResolutionBehavior: develop's dispatcher is a singleton that resolves handlers and behaviors from the root provider (`Dispatcher.cs:17,93,96`), so the behavior cannot write the request-scoped `WorkspaceContext`; it would reach 66 existing handler files (UseCases 23, Products 11, Memory 10, GraphRAG 17); global memory sends an empty workspace path by design (`MemoryController.cs:125`, `MemoryGlobalDefaultWorkspaceTests:100`); the stdio MCP host has its own composition root (`McpStdioHost.cs:319-320`), which would bypass both behaviors if only `Program.cs` registers them.
- IdempotentCreate ignores soft-delete tombstones; the unique indexes are unfiltered (`McpDbContext.cs:391,404`), so a deleted link cannot be re-created and key allocation can be trapped.
- `McpDbContext` rewrites em and en dashes in every stored string, which breaks Phase C replay identity, `ContentSha256`, and the byte-for-byte reviewer-turn check.
- The MCP review section tools bypass the review units; `ClientMutationPolicy` blocks unlisted agent-context REPL calls when turn transactions are enabled or degraded.
- `QBrainAI.Client` is a published package: new DTOs, `ClassifiedErrorException`, `ErrorCodes`, `PageResult`, and `ScoreLexeme` become its public API, beside its existing `McpClientException` family.

Plugin core and build:
- About 25 drain cases break under the round-5 drain rules; about 120 `current-turn.yaml` references across 7 core Pester files, the `beginTurn` and local `openSession` verbs, and the PluginIntegration canonical flow have no stated fate (the plan budgets four refinement files).
- `TEST-MCP-BUGTRIAGE-068`, kept "unchanged" by the plan, breaks (no Stop payload key; code-verify still writes `current-turn.yaml`); the FR-MCP-TRIAGEPLUGIN-001 tests contradict A3's hostile-brief rule while the plan says "no edit".
- The section 3.5.6 functions placed in `plugin-hook.ps1` are unreachable from the verb and drain processes, because that file cannot be dot-sourced.
- Build: `RefreshKnownPluginCaches` overwrites installed host caches; version `1.119.0` is already used by unpushed BUG-BUILD-008 branches and installed caches; the per-sync PowerShell-only validation deadlocks TypeScript adoption; the AC-005 content scan would fail 41 tracked docs and tests; ConfigValidator cannot see the `QBrainAi:` root (BUG-BUILD-005) and has no AgentContext ranges; G3 has no Nuke runner for the IntegrationTests and Validation projects.
- Hosts: the TypeScript adapter contract drops memory injection, tool audit, and cline-v2 `beforeModel`; Stop enforcement is impossible on classic cline, cline-v2, and opencode; codex lacks a session-end wrapper.

The analyzer branch (`chore/netanalyzers-all`, 7 commits plus 514 uncommitted files):
- IDE0130 checks RootNamespace plus folder, and RootNamespace is `QBrainAi.Support.Mcp` for Services (and `QBrainAi.Support.Mcp.Storage` for Storage on the branch), so every `QBrainAi.Services.*` and `QBrainAi.Storage.*` namespace round 5 names fails that build.
- MA0048 (one type per file) conflicts with "a record and its handler in one file".
- CA1707: the branch renamed 4,468 test methods to drop underscores; the plan and architecture name about 475 new cases with underscores, and the store still cites about 130 pre-rename names in evidence and TEST text (no gate checks that drift today: ValidateTraceability reads only ids from the markdown projections).
- `Build.Tests` moved to `build/Build.Tests`, against 22 plan and architecture citations.

## 3. Projected problems, in priority order

1. Blocker: WorkspaceResolutionBehavior on every command and query through the singleton dispatcher (cross-workspace race or scope failure, global memory broken, stdio bypass).
2. Blocker for the Phase 2 compile if the analyzer branch merges first: namespaces, file-per-type, and underscore test names.
3. High, certain: the plan is not re-derived from round 5 (no SharedComponents in contexts, TODOs, or rollout; old type names; the round-5 breaking changes unlisted; Phase C still called "additive and not breaking").
4. High: thirteen cross-part inconsistencies (section 4).
5. High: the whole-session Serializable scope conflicts with the locked "independent turn edits" decision and the five-second dense-session AC on PostgreSQL and SQL Server.
6. High: "a nested dispatch joins the caller's transaction" is undefined with transient, root-resolved handlers.
7. High in Pilot: Build holds a write transaction across retrieval and git reads on SQLite; the status query would evaluate every response's staleness at each SessionStart.
8. High: the completion gate breaks about 120 existing server-path tests and two internal writers.
9. High: twelve or more serial HV gates on the critical path, and SharedComponents' gate lists `TurnWrite_ForcedInterleaving_*` cases that need contexts merged after it.
10. Medium: surface error mapping lives in Phase 2 shared files no Phase 4 context owns; the client package's public API grows; data risks (Down migrations tested only on SQLite, binary-only rollback with required columns and no database defaults, readable keys colliding across workspaces under the query filter, child rows without root ids).

## 4. Cross-part inconsistencies left by the parallel edit pass

1. Build staleness: the activity reads staleness read-only; the sequence persists it by a nested dispatch.
2. Staleness dependency set: `depends_on` included in 3.5 and 6.3, excluded at one 6.5 line.
3. Status counts: stored in 6.4, effective in 6.5.
4. Pilot report: `refusedPrompts` in 6.5, missing from the 6.4 DTO.
5. Review reset: one Phase B sentence still says both "a canceled or closed turn can still be reset" and lists every terminal status.
6. Agent-surface statuses: `cancelled` accepted in 3.5.3, refused in the unit lists.
7. 3.4 Scores still names `SessionLogReviewScoreJsonConverter`.
8. Addendum A was regenerated before the editors added columns: it lacks `PayloadSha256`, `AgentContextPackageInputLink`, the inputs' `ContainsSecret` and `ResponseStatus`, the grade's `RecordedUtc`, and the composite keys.
9. The section 9 index names a deleted nested diagram and a renamed sequence.
10. The Mode cache in `turn-state.yaml` is missing from the file schema and writer list.
11. ModeGate wording "gated (every command)" against the DecideAgentResponseTrustCommand exemption.
12. Attempt bounds: SerializableWrite allows four attempts, the key allocators six and five.
13. The header still says the plan "remains the authority for ... gates, tests, and acceptance criteria", against architecture first.

## 5. Decisions needed from the operator

1. The analyzer branch and this plan: the merge order, the namespace rule (RootNamespace equal to the assembly name per project, or the architecture's names become RootNamespace plus folder), MA0048 (one file per record and per handler, or an exemption), and the spelling of the planned test cases under CA1707.
2. Workspace resolution: apply the new behavior only to the records this plan adds (opt-in, existing handlers untouched, the resolved workspace passed to the handler without writing a DI-scoped service), or to every command and query (which needs a per-dispatch scope, a change to the published `QBrainAI.Cqrs`).
3. Serializable scope for turn writes: keep the whole-session scope (and relax "independent turn edits" under contention), or narrow the isolation to the turn being written.
4. The code freeze: record an exception for the analyzer branch, or hold that branch until the merged plan is approved.

## 6. Next steps (after the decisions)

1. One coordinated architecture pass: the 13 inconsistencies, the decisions above, root ids on child rows (the standing "carry root ids" rule), tombstone-aware idempotency, hashing over the stored normal form, reachable plugin function locations, drain rules reconciled with FR-MCP-REPL-011 and TR-MCP-REPL-016/017 (quarantine budget kept), the internal writers' completion-gate exemption stated for `mcp_session_turn_complete` and `importRecovery`, Addendum A regenerated.
2. Plan derivation: every contradicted record above added to "Updated existing requirements", SharedComponents in the contexts, TODO chain, and rollout, the round-5 breaking changes listed, the test churn budgeted (completion gate, drain, `current-turn.yaml`), and the analyzer decisions applied to every named case.
3. Re-render to the page standard (28 diagrams still need four or more pages), holistic read, then HV round 1.
