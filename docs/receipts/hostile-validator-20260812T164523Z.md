# Hostile Validator Receipt

- TimestampUtc: 2026-08-12T16:45:23Z
- ValidatorIdentity: GrokSubagentHostile
- Workspace: F:\GitHub\McpServer
- Plugin: F:\GitHub\mcpserver-grok-plugin
- ReviewSessionId: GrokCode-20260812T164435Z-hostile-plan-sessionlog-002
- ReviewRequestId: req-20260812T164435Z-001-hostile-plan-sessionlog-002
- ServerTurnId: 40595
- PlanFile: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\019ff6a0-c5fb-7162-8a7b-c7c1234385d1\plan.md
- Default posture: FAIL until independently re-verified
- OverallVerdict: DISAGREE

## Claims reviewed

1. Plan AC ids follow existing workspace naming (AC-FR119-008 style), not FR-AC-1 / TR-AC-1 / ac-1.
2. ACs are children of the FR and TR; TEST-MCP-SESSIONLOG-006 lists those child ids and the tests that prove them.
3. Plan is decision-complete for MCP-SESSIONLOG-002: field names, None sentinel, new-entry vs update, exact/~/ paths, backfill including agent history under ~, import requires a proper value, Byrd slices with named red tests.
4. Plan matches current code write paths (UpsertTurnAsync, MapSingleEntry, begin_turn, three-provider migrations) well enough that an implementer need not invent remaining product decisions.
5. Every FR/TR child AC is covered by at least one named test method in a named file.
6. Plan does not contradict FR-SUPPORT-015 additive merge or TR-MCP-SESSIONLOG-001 structured errors.

## Explicit FAIL list

- Claim 1 FAIL: invented hyphen-stripped IDs (`AC-FRSESSIONLOGCTX001-001`, `AC-TRSESSIONLOG006-001`) do not match live TR children (`AC-TR-MCP-AGENT-015-001`) or the mechanical remainder of `AC-FR119-008`.
- Claim 2 FAIL: live `TEST-MCP-SESSIONLOG-006` AcceptanceCriteria count is 0 and its Condition uses (1)-(7), not the plan child ids. Plan TEST "Validates" headers omit `AC-FRSESSIONLOGCTX001-004`.
- Claim 3 FAIL: remaining product decisions include TR-body workspace-relative vs plan exact/~/ paths, begin_turn required vs re-open preserve vs hook `None` overwrite, SubmitAsync import branch, query-filter normalization, backfill guard OR, and import partial-field merge.
- Claim 4 FAIL: plan misses `SubmitAsync` import vs interactive (`sourceFilePath`), dual `SessionLogQueryRequest` types, `FederationDataClient.BuildSessionLogQueryString`, `SessionLogIngestor.cs` in the files list, and REPL/McpAgent throw-on-duplicate vs service re-open.
- Claim 5 FAIL: no named exact `planFile` filter test; no `SessionLogIngestor` test; no three-provider migration Up/Down test; Slice 8 has no named methods.

## Trust bootstrap (review process, not a reviewed claim)

- Marker path: F:\GitHub\McpServer\AGENTS-README-FIRST.yaml
- Test-MarkerSignature -MarkerFile: True
- GET http://PAYTON-LEGION2:7147/health?nonce=2be0452a600a4480b1c398473de18520: HEALTH_STATUS=Healthy, HEALTH_VERSION=1.4.25+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e, HEALTH_NONCE_MATCH=True
- Plugin: F:\GitHub\mcpserver-grok-plugin. workflow.sessionlog.bootstrap initialized=true. workflow.sessionlog.openSession failed (exit 1) because plugin cache still pointed at GrokCode-20260812T155231Z-hostile-sessionlog-002. Native sessionlog_open created=true for the dedicated review session.
- Native sessionlog_begin_turn: success=true, turnId=40595, status=in_progress

Operator profile loaded (14 files, add-profile.grok.md excluded): PROFILE.md, user-payton-byrd.md, accuracy-first-verify-sources.md, approve-before-execute.md, philosophical-dialogue-mode.md, log-decisions-as-conclusions.md, session-turn-title-summary.md, never-skip-explicit-actions.md, adversarial-review-global.md, bring-the-receipts.md, lab-authorization.md, no-attitude-honesty-tell.md, no-python-lab.md, no-shortcuts-precision-over-convenience.md.

## Claim 1: Plan AC ids follow existing workspace naming (AC-FR119-008 style), not FR-AC-1 / TR-AC-1 / ac-1

**Verdict: FAIL**

Observation: the plan forbids `FR-AC-1`, `TR-AC-1`, `ac-1`, and a free-floating `AC-001` series. Plan children are `AC-FRSESSIONLOGCTX001-001` .. `007` and `AC-TRSESSIONLOG006-001` .. `008` (plan.md lines 36-45, 49-96).

Observation: existing IDs independently found:

- `docs/Project/TurnTransactions-Requirements-Batch.yaml` line 60: `AC-FR119-008` under `FR-MCP-119`
- Live MCP `requirements_list` type=tr: `TR-MCP-AGENT-015` children are `AC-TR-MCP-AGENT-015-001` .. `004`
- Live MCP `TR-SUPPORT-CORE-015` children are `ac-1` .. `ac-4` (the exact token the plan says not to use, and the most common live TR AC id)
- Repo also has `AC-MCP-901` and `AC-CODEXBIND-001`

Inference rejected as PASS: compact hyphen-stripping (`SESSIONLOGCTX001`, `SESSIONLOG006`) has zero live or repo matches. A mechanical reading of `AC-FR119-008` for `FR-MCP-SESSIONLOGCTX-001` keeps the remainder hyphen (`AC-FRSESSIONLOGCTX-001-001`). The only well-formed live TR children use the full parent id (`AC-TR-MCP-AGENT-015-001`), not `AC-TRSESSIONLOG006-001`.

The negative half (not `FR-AC-1`) is true. The positive half (follow existing naming) is false.

## Claim 2: ACs are children of the FR and TR; TEST-MCP-SESSIONLOG-006 lists those child ids and the tests that prove them

**Verdict: FAIL**

Observation: the plan document treats ACs as FR/TR children and has a TEST section that names most of them plus test methods (plan.md section 1A).

Observation from live MCP store (requirements_list, then PowerShell filter):

- `FR-MCP-SESSIONLOGCTX-001` AcceptanceCriteria count: 0. Body is a SHALL about required plan file / TODO id, None, backfill. No child ids.
- `TR-MCP-SESSIONLOG-006` AcceptanceCriteria count: 0. Body requires workspace-relative plan path or canonical TODO id or None. No child ids.
- `TEST-MCP-SESSIONLOG-006` AcceptanceCriteria count: 0. Condition uses numbered (1)-(7) and says "Validated by tests in tests/McpServer.Support.Mcp.Tests covering SessionLogService and turn DTO validation". It does not list `AC-FRSESSIONLOGCTX001-*`, `AC-TRSESSIONLOG006-*`, or the plan's named methods.

Observation: plan TEST "Validates" headers never cite `AC-FRSESSIONLOGCTX001-004` even though Slice 1 includes that parent AC. Plan line 47 says persist children through MCP before Slice 1; that is future work, not a current TEST listing.

Claim 2 requires TEST to list those child ids. Live TEST does not. Plan TEST map omits 004.

## Claim 3: Plan is decision-complete for MCP-SESSIONLOG-002

**Verdict: FAIL**

Locked (observation, not enough for PASS):

- Field names `planFile` / `todoId` (JSON) and `PlanFile` / `TodoId` (C# / SQL) are stated.
- Sentinel is exact `None`.
- New-entry vs additive update vs PUT replace is written in section 2.3.
- Exact and `~/` path rules, backfill including `~` history, import-must-have-a-value, and named Byrd slice tests are written.

Remaining decisions an implementer must invent (observation vs current artifacts):

1. Live `TR-MCP-SESSIONLOG-006` body: "Accepted values are a workspace-relative plan file path or canonical MCP TODO id, or the literal sentinel None." Plan 2.4 and `AC-FRSESSIONLOGCTX001-004` accept exact Windows/Unix/`~/` paths. The plan tells the implementer to persist AC children and not hand-edit `docs/Project/*.md`, but it never says to rewrite that TR sentence. Workspace-relative only vs exact/`~/` is unresolved against the store.
2. `todo_get` MCP-SESSIONLOG-002 backfill text lists turn contents only and "workspace-relative plan file paths". Plan adds `~` agent history and exact paths. Extra scope is written, but it conflicts with the current TR/TODO wording the implementer is supposed to implement.
3. Section 3.5 makes `sessionlog_begin_turn` parameters required (no C# default). Section 2.3 says a second begin of the same request id is an update and omitted fields preserve. Hook 3.6 sends `None`/`None` when no active plan. Supplying `None` on re-open overwrites stored values. The three layers do not have one locked behavior.
4. `SubmitAsync` already treats `sourceFilePath` null as interactive (canonical id checks) and non-null as import. Plan 3.3 never names this branch. Extract-then-validate for import must be invented at `SubmitAsync` / `UpsertTurns` / `MapNewTurns`, not only `MapSingleEntry`.
5. Two `SessionLogQueryRequest` types exist (`ISessionLogService.cs` record and Client `SessionLogModels.cs` class) plus `FederationDataClient.BuildSessionLogQueryString`. Plan says "query signature if filters are on the interface".
6. Query filters are exact string match. `~/` is persisted expanded. Whether query input is expanded/normalized is not locked.
7. Backfill guard is "completed-flag row or only update where both columns are None" (OR).
8. Import step 2: if either field is missing/invalid, "persist the extractor result" can discard a valid sibling field. Merge vs replace-both is not locked.
9. Extractor "exact paths under `~` that look like plan files" has no predicate.
10. Agent-history globs say "matching the session AgentSessionId or workspace slug" with no match algorithm.
11. Slice 3 turns on `MapSingleEntry` validation; REPL/McpAgent/import fixtures that omit the fields stay red until later slices. Gate filters hide that.

Named Byrd slices exist. Decision-complete is false.

## Claim 4: Plan matches current write paths well enough that an implementer need not invent remaining product decisions

**Verdict: FAIL**

Matches (observation):

- `UpsertTurnAsync` insert uses `MapSingleEntry`; update uses `UpdateEntryFromDto(..., mergeOmittedFields: true)` (`SessionLogService.cs` 522-530).
- `ReplaceTurnAsync` update uses `UpdateEntryFromDto(..., mergeOmittedFields: false)` (FR-SUPPORT-010G) (590-603).
- `ApplyValue` already implements omit-preserve (1290-1297).
- `sessionlog_begin_turn` builds a DTO via a mutator on `QueryTitle`/`QueryText` then upserts (`FwhMcpTools.SessionLog.cs` 129-141). Adding two parameters fits that shape.
- `SessionLifecycleBeginRequest` exists as a record in `SessionLogRequestModels.cs` 27-44. Controller `BeginTurnAsync` copies body fields onto `UnifiedRequestEntryDto` (355-365).
- Three provider assemblies exist; last SQLite provider migration is `20260808102524_AddUseCaseDiagramGraph`. Plan name `AddSessionLogTurnPlanFileAndTodoId` matches that style.
- Dual DTO copies exist (`Services/Models/UnifiedSessionLogDto.cs` and `Client/Models/SessionLogModels.cs`). Plan says keep them in sync.
- REST ArgumentException mapping already uses Problem title `Invalid turn payload.` (controller lines 172-174, 446-448).
- `SanitizeTurn` clones onto a new DTO and does not copy the new fields yet (185-214). Adding copies is obvious.

Does not match well enough (observation):

- Import/federation/transcript all call `SubmitAsync` with a `sourceFilePath` (`SessionLogIngestor.cs` 219, `TranscriptSessionLogPersister.cs` 31). New session create uses `MapNewTurns` -> `MapSingleEntry` with no extractor. If validation is only in `MapSingleEntry`, every current import throws. Plan section 8 does not list `SessionLogIngestor.cs`.
- `ISessionLogService.QueryAsync` already takes a request record (215-240). Client has a second class. Federation query-string builder omits the new filters. Plan left filter placement as an "if".
- `src/McpServer.Storage/Migrations` still exists and is behind the provider assemblies (last: `20260722214500_AddAgentSessionHeaderFields`). Plan says "if this repo still keeps a design-time copy".
- McpAgent `BeginTurnAsync` throws when the request id already exists (`SessionLogWorkflow.cs` 141-145). REPL `TurnState.BeginTurn` throws "already exists" (834-840). Service `UpsertTurnAsync` re-opens. Plan 2.3 says second begin is an update. Implementer must invent which layer wins.
- Plugin `workflow.sessionlog.openSession` failed in this review while native open succeeded. That does not change the product plan, but it shows plugin beginTurn is not the same write path as `sessionlog_begin_turn`.

## Claim 5: Every FR/TR child AC is covered by at least one named test method in a named file

**Verdict: FAIL**

Covered in the plan TEST map (observation): 001, 002, 003, 006 (extractor/backfill), TR 001-003, 005-008 have named methods.

Not covered:

- `AC-FRSESSIONLOGCTX001-005` requires exact filters on `planFile` and `todoId`. Named tests are `QueryAsync_FilterByTodoId_ReturnsOnlyMatches` and `QueryAsync_TextMatchesPlanFileOnly_ReturnsSession`. There is no `QueryAsync_FilterByPlanFile_*`.
- `AC-FRSESSIONLOGCTX001-007` names transcript + federation tests only. Plan 2.5 also requires `SessionLogIngestor`. Existing file `tests/McpServer.Support.Mcp.Tests/Ingestion/SessionLogIngestorImportTests.cs` is not extended.
- `AC-TRSESSIONLOG006-004` requires SQLite, SQL Server, and PostgreSQL migrations that add both columns and Down drops them. Named test is only `SessionLogTurnEntity_PlanFileAndTodoId_RequiredWithExpectedMaxLengths` (model metadata). Slice 4 gate is `./build.ps1 Compile`.
- `AC-FRSESSIONLOGCTX001-004` has validator tests (`ExactWindows`, `HomeRelative`, `ParentSegment`) but is absent from every "Validates" header.
- Slice 8 (plugins/docs) has no named test methods.

## Claim 6: Plan does not contradict FR-SUPPORT-015 additive merge or TR-MCP-SESSIONLOG-001 structured errors

**Verdict: PASS**

Observation: `FR-SUPPORT-015` (Functional-Requirements.md 1923-1930 and TR-SUPPORT-CORE-015 live ACs ac-1..ac-4) requires omitted turn fields never overwrite. Plan 2.3 / 3.3 uses `ApplyValue` on additive upsert/complete and requires both fields only on new entry and PUT replace. That matches current `UpsertTurnAsync` vs `ReplaceTurnAsync`. Replace is FR-SUPPORT-010G, which the plan leaves in place. Out-of-scope says do not change 015 for other fields.

Hook `None`/`None` on a re-open is a supplied value, so 015 (omitted) does not apply. That is a claim 3 hole, not a 015 contradiction.

Observation: `TR-MCP-SESSIONLOG-001` requires `sessionlog_complete_turn` / `sessionlog_fail_turn` to return structured `{error}` for malformed `turnJson` and workspace-resolution failure via `McpToolErrors.Serialize`. Plan 2.4 / 3.5 reuse that serializer for begin_turn validation failures and forbid raw EF/JsonException. REST already maps `ArgumentException` to 400 Problem `Invalid turn payload.` Extending the same path does not retract AC1-AC3 of TR-001.

## Session log proof (this review)

- Native sessionlog_open: success=true, created=true, sessionId=GrokCode-20260812T164435Z-hostile-plan-sessionlog-002
- Native sessionlog_begin_turn: success=true, turnId=40595, status=in_progress
- Native sessionlog_dialog: success=true, totalDialogItems=2
- Native sessionlog_replace_section actions: success=true, replaced=true, integer order 1..6
- Native sessionlog_complete_turn: success=true, turnId=40595, status=completed
- sessionlog_query proof (agent=GrokCode, text=hostile-validator-20260812T164523Z, limit=5):
  - SessionId: GrokCode-20260812T164435Z-hostile-plan-sessionlog-002
  - RequestId: req-20260812T164435Z-001-hostile-plan-sessionlog-002
  - Turn status: completed
  - queryTitle: Hostile validate MCP-SESSIONLOG-002 plan
  - response includes OverallVerdict DISAGREE and both receipt paths
  - actions: integer order 1 through 6 present
  - processingDialog: 2 items (reasoning + decision)
  - filesModified: docs/receipts/hostile-validator-20260812T164523Z.md and .json
  - Session-level status remains in_progress (turnCount=1). That does not change the completed review turn.

Plugin note: workflow.sessionlog.bootstrap succeeded. workflow.sessionlog.openSession failed (exit 1). Plugin cache `F:\GitHub\McpServer\.mcpServer\grok\session-state.yaml` remained on GrokCode-20260812T155231Z-hostile-sessionlog-002. Review persistence is the native sessionlog_* turn above.

## OverallVerdict

DISAGREE
