# Hostile Validator Receipt

- TimestampUtc: 2026-08-12T18:56:50Z
- ValidatorIdentity: GrokSubagentHostile
- Workspace: F:\GitHub\McpServer
- Plugin: F:\GitHub\mcpserver-grok-plugin version 1.85.0 (`.grok-plugin/plugin.json`)
- ReviewSessionId: GrokCode-20260812T185650Z-hostile-sessionlog-002-skeptic
- ReviewRequestId: req-20260812T185650Z-001-hostile-sessionlog-002-skeptic
- TodoId: MCP-SESSIONLOG-002 (Done=false is expected; not evaluated as a claim)
- Default posture: FAIL until independently re-verified
- Prior stale receipt ignored as proof: docs/receipts/hostile-validator-20260812T193000Z.md
- OverallVerdict: AGREE

## Claims reviewed

1. `plugins/core/lib-ps/plugin-hook.ps1` `Open-PluginTurn` no longer hardcodes `planFile='None'` and `todoId='None'`. It calls `Resolve-PluginTurnPlanContext`, which uses `Get-PlanFilePathFromInput`, else the last existing `plan-todo-map.yaml` entry, else None/None, then `Find-PlanTodoId`.
2. `plugins/core/lib-ps/repl-invoke.ps1` `Invoke-WorkflowBeginTurn` treats same-requestId as reopen: it does not default omitted `planFile`/`todoId` to None, and `Invoke-ReplPersistTurn` is called without `-PlanFile`/`-TodoId` so stored values are not overwritten.
3. Slice 8 tests drive the persist path, not source grep: `InvokeWorkflowBeginTurnTests.cs` calls `SessionLogService.UpsertTurnAsync` for missing-fields reject, None/None, mapped pair, and reopen omit-preserve. Pester `SessionLogTurnContextBeginTurn.Tests.ps1` invokes real user-prompt-submit (`MCP_PLUGIN_REPL_LOG`) for no-map None and mapped plan/todo, and invokes `Invoke-WorkflowBeginTurn` with `MCP_PLUGIN_PERSIST_LOG` for omitted-first-persist None and reopen unbound fields.
4. `FR-MCP-SESSIONLOGCTX-001` store record has children `AC-FR-MCP-SESSIONLOGCTX-001-001` through `007`. `TR-MCP-SESSIONLOG-006` has `AC-TR-MCP-SESSIONLOG-006-001` through `008`. `TEST-MCP-SESSIONLOG-006` lists those FR/TR AC ids. Verify via MCP requirements_list, not markdown.
5. Fresh implementer gating logs exist under `C:\Users\kingd\AppData\Local\Temp\grok-goal-f5bc2686e8c7\implementer` and must be re-run: persist-query 46/0/0, backfill-import 10/0/0, SessionLog excl Transcript plus InvokeWorkflowBeginTurnTests 247/0/0, integration 54/0/0, Pester begin-turn 4/0/0.
6. `Get-PlanFilePathFromInput` still accepts `TOOL_INPUT`, so the mapped Pester case can send a real plan path plus `MCP-SESSIONLOG-002` from `plan-todo-map.yaml`.

## Explicit FAIL list

- None. All six assigned claims independently re-verified as PASS.

## Trust bootstrap (review process, not a reviewed claim)

- Marker path: F:\GitHub\McpServer\AGENTS-README-FIRST.yaml
- `Test-MarkerSignature -MarkerFile`: True
- GET `http://PAYTON-LEGION2:7147/health?nonce=d505fdfab4c64a25abdae56a69dd8676`: HEALTH_STATUS=Healthy, HEALTH_VERSION=1.4.25+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e, HEALTH_NONCE_MATCH=True
- Plugin Status: available, agent=GrokCode, cacheDir=`F:\GitHub\McpServer\.mcpServer\grok`, version=1.85.0
- `workflow.sessionlog.bootstrap`: initialized=true (deprecated: true metadata)
- Plugin cache after open+begin: sessionId=`GrokCode-20260812T185650Z-hostile-sessionlog-002-skeptic`; current-turn.yaml turnRequestId=`req-20260812T185650Z-001-hostile-sessionlog-002-skeptic` status=in_progress
- Native `sessionlog_*` / `requirements_list` tools were not in this subagent function list. Requirements and session query used the required Grok plugin wrapper (`workflow.requirements.listFr/listTr/listTest`, `getFr/getTr/getTest`, `workflow.sessionlog.queryHistory`, `client.SessionLog.QueryAsync`). No raw REST fallback for those surfaces.
- Live UpdateService/deploy is a plan non-goal. Live 1.4.25 begin_turn field support was not used as a FAIL reason.

## Claim 1: Open-PluginTurn uses Resolve-PluginTurnPlanContext

**Verdict: PASS**

Independent read of `F:\GitHub\McpServer\plugins\core\lib-ps\plugin-hook.ps1` after this review started.

- `Open-PluginTurn` at lines 715-721 builds beginTurn params from `$turnContext = Resolve-PluginTurnPlanContext` and sends `$turnContext.planFile` / `$turnContext.todoId`. There is no hardcoded `'None'` pair at the call site.
- `Resolve-PluginTurnPlanContext` at lines 1007-1024:
  - starts with `Get-PlanFilePathFromInput`
  - if that path is missing or not a leaf, uses `Get-LastPlanTodoMapEntry` only when that mapped `planFile` exists on disk
  - otherwise returns `planFile='None'` / `todoId='None'`
  - otherwise calls `Find-PlanTodoId -PlanFile $planFile` and substitutes `'None'` only when the map lookup is blank
- `Find-PlanTodoId` is at lines 1079-1096 and matches the last `plan-todo-map.yaml` row whose `planFile` equals the resolved path.

Note (not a FAIL of this workspace-core claim): `F:\GitHub\mcpserver-grok-plugin\lib\plugin-hook.ps1` `Open-PluginTurn` still omits plan/todo params entirely (lines 715-719). That is the installed plugin copy, not the claimed `plugins/core` file.

## Claim 2: Invoke-WorkflowBeginTurn reopen omit-preserve

**Verdict: PASS**

Independent read of `F:\GitHub\McpServer\plugins\core\lib-ps\repl-invoke.ps1`.

- Lines 1451-1458: `$isReopen` is true when current-turn `turnRequestId` equals the incoming `requestId`. Defaulting omitted fields to `'None'` is inside `if (-not $isReopen)`.
- Lines 1499-1503: reopen calls `Invoke-ReplPersistTurn` without `-PlanFile`/`-TodoId`. First persist passes both.
- `Invoke-ReplPersistTurn` persist-log seam records `boundPlanFile`/`boundTodoId` from `PSBoundParameters` (lines 1040-1041). Independent Pester reopen case asserted both false and empty values.
- Empty unbound strings are omitted by `New-McpPluginTurnUpsertRequest` (`McpPluginShim.psm1` lines 441-447). `SessionLogService` merge (`ApplyValue` with `dto.PlanFile is not null`) therefore keeps stored values. Independent C# reopen test stored `docs/plans/foo.md` + `MCP-SESSIONLOG-002` after a second `UpsertTurnAsync` that omitted both fields.

## Claim 3: Slice 8 tests drive persist / hook paths, not source grep

**Verdict: PASS**

Independent file read plus independent execution.

`tests/McpServer.Support.Mcp.Tests/Plugins/InvokeWorkflowBeginTurnTests.cs` constructs `SessionLogService` against in-memory `McpDbContext` and calls `UpsertTurnAsync` for:

- `Invoke_WorkflowBeginTurn_MissingFields_FailsValidation` (ArgumentException, 0 rows)
- `Invoke_WorkflowBeginTurn_FirstTurn_SendsNoneWhenNoPlanMap` (stores None/None)
- `Invoke_WorkflowBeginTurn_FirstTurn_SendsMappedPlanAndTodo` (stores mapped pair)
- `Invoke_WorkflowBeginTurn_Reopen_OmitsFieldsAndDoesNotOverwrite` (omit preserves stored pair)

`plugins/core/test-fixtures/pester/SessionLogTurnContextBeginTurn.Tests.ps1`:

- `Invoke-UserPromptSubmit` launches real `plugin-hook.ps1 -HookName user-prompt-submit` with `MCP_PLUGIN_REPL_LOG` for no-map None and mapped plan/todo (`TOOL_INPUT` set).
- `Invoke-WorkflowBeginTurnCapture` sets `MCP_PLUGIN_PERSIST_LOG` and calls `Invoke-WorkflowBeginTurn` for omitted-first-persist None and reopen unbound fields.

Independent re-run (this review):

- Pester: Passed=4 Failed=0 Skipped=0 Total=4 (Pester 5.7.1). Log: `C:\Users\kingd\AppData\Local\Temp\grok-goal-f5bc2686e8c7\hostile-validator-skeptic\pester-beginturn.log`
- `FullyQualifiedName~InvokeWorkflowBeginTurnTests`: LIST_COUNT=4, Passed 4 Failed 0 Skipped 0 EXIT=0

These tests execute the hook/persist seams. They are not source-grep assertions.

## Claim 4: FR/TR/TEST AC children via MCP requirements list

**Verdict: PASS**

Verified through the required Grok plugin wrapper, not `docs/Project` markdown.

Commands (plugin, workspace `F:\GitHub\McpServer`):

- `workflow.requirements.listFr` area=MCP
- `workflow.requirements.listTr` area=MCP
- `workflow.requirements.listTest` area=MCP
- `workflow.requirements.getFr` id=FR-MCP-SESSIONLOGCTX-001
- `workflow.requirements.getTr` id=TR-MCP-SESSIONLOG-006
- `workflow.requirements.getTest` id=TEST-MCP-SESSIONLOG-006

Evidence files: `C:\Users\kingd\AppData\Local\Temp\grok-goal-f5bc2686e8c7\hostile-validator-skeptic\list-fr.txt`, `list-tr.txt`, `list-test.txt`, `get-fr.txt`, `get-tr.txt`, `get-test.txt`.

`FR-MCP-SESSIONLOGCTX-001` acceptanceCriteria ids:

- AC-FR-MCP-SESSIONLOGCTX-001-001
- AC-FR-MCP-SESSIONLOGCTX-001-002
- AC-FR-MCP-SESSIONLOGCTX-001-003
- AC-FR-MCP-SESSIONLOGCTX-001-004
- AC-FR-MCP-SESSIONLOGCTX-001-005
- AC-FR-MCP-SESSIONLOGCTX-001-006
- AC-FR-MCP-SESSIONLOGCTX-001-007

`TR-MCP-SESSIONLOG-006` acceptanceCriteria ids:

- AC-TR-MCP-SESSIONLOG-006-001
- AC-TR-MCP-SESSIONLOG-006-002
- AC-TR-MCP-SESSIONLOG-006-003
- AC-TR-MCP-SESSIONLOG-006-004
- AC-TR-MCP-SESSIONLOG-006-005
- AC-TR-MCP-SESSIONLOG-006-006
- AC-TR-MCP-SESSIONLOG-006-007
- AC-TR-MCP-SESSIONLOG-006-008

`TEST-MCP-SESSIONLOG-006` acceptanceCriteria lists those same fifteen FR/TR AC ids (001-007 and 001-008).

Note (not a FAIL of the store-record claim): TEST title is still the placeholder `Test TEST-MCP-SESSIONLOG-006`. All listed AC ids are `isSatisfied: false`. Markdown export was not consulted.

## Claim 5: Independent re-run of gating logs

**Verdict: PASS**

Implementer logs exist at `C:\Users\kingd\AppData\Local\Temp\grok-goal-f5bc2686e8c7\implementer\` (`sessionlog-002-persist-query.log`, `sessionlog-002-backfill-import.log`, `sessionlog-002-tests.log`, `sessionlog-002-integration.log`, `sessionlog-002-pester-beginturn.log`). Those files were not treated as proof.

Independent re-run from `F:\GitHub\McpServer` via `pwsh.exe -NoProfile -NonInteractive` script `hostile-validator-skeptic-tests.ps1`. EXIT=0.

- persist-query filter `FullyQualifiedName~SessionLogServiceTurnContext|FullyQualifiedName~SessionLogTurnContext|FullyQualifiedName~BeginTurn_NoneNone|FullyQualifiedName~Query_FilterByTodoId|FullyQualifiedName~SessionLogBeginTurn_|FullyQualifiedName~InvokeWorkflowBeginTurnTests`: LIST_COUNT=46, Passed 46 Failed 0 Skipped 0 EXIT=0
- backfill-import filter `FullyQualifiedName~SessionLogTurnContextBackfill|FullyQualifiedName~SubmitAsync_Import|FullyQualifiedName~Import_Omitted|FullyQualifiedName~Ingest_Omitted|FullyQualifiedName~Apply_Omitted`: LIST_COUNT=10, Passed 10 Failed 0 Skipped 0 EXIT=0
- SessionLog excl Transcript plus begin: `FullyQualifiedName~SessionLog&FullyQualifiedName!~TranscriptMcpStdioHostTests|FullyQualifiedName~InvokeWorkflowBeginTurnTests`: LIST_COUNT=247, Passed 247 Failed 0 Skipped 0 EXIT=0
- integration `FullyQualifiedName~SessionLog`: LIST_COUNT=54, Passed 54 Failed 0 Skipped 0 EXIT=0
- Pester `plugins/core/test-fixtures/pester/SessionLogTurnContextBeginTurn.Tests.ps1`: Passed 4 Failed 0 Skipped 0

Logs: `C:\Users\kingd\AppData\Local\Temp\grok-goal-f5bc2686e8c7\hostile-validator-skeptic\`

This matches the implementer claimed counts. Transcript exclusion is a filter, not a skipped test.

## Claim 6: Get-PlanFilePathFromInput accepts TOOL_INPUT

**Verdict: PASS**

`Get-PlanFilePathFromInput` in `plugins/core/lib-ps/plugin-hook.ps1` lines 942-954:

- reads hook payload `file_path`
- else `tool_input.file_path` / `tool_input.path`
- else `$env:TOOL_INPUT`

Pester mapped case writes a real plan file, writes `plan-todo-map.yaml` with `todoId: MCP-SESSIONLOG-002`, and sets `TOOL_INPUT` to that plan path. Independent Pester run passed `Invoke-WorkflowBeginTurn_FirstTurn_SendsMappedPlanAndTodo` (1.94s). That case asserts the REPL log contains the escaped plan path and `todoId: MCP-SESSIONLOG-002`.

## Non-goals (not used as FAIL)

- Live 1.4.25 UpdateService/deploy and live `begin_turn` planFile/todoId support.
- MCP-SESSIONLOG-002 remaining Done=false.
- Use-case diagram UI claims.
- Stale `docs/receipts/hostile-validator-20260812T193000Z.md`.
