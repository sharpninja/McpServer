# TR-MCP-SESSIONLOG

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-SESSIONLOG-001

**Session-log lifecycle tools return structured errors** — The session-log lifecycle MCP tools (sessionlog_complete_turn, sessionlog_fail_turn) SHALL return a structured {error} result for every failure mode, including a malformed turnJson payload and a workspace-resolution failure; no failure may surface as the ModelContextProtocol SDK's opaque "An error occurred invoking sessionlog_complete_turn" message. Acceptance Criteria: (AC1) sessionlog_complete_turn/sessionlog_fail_turn with a malformed turnJson return {error} carrying the exception message and no success field; (AC2) a workspace-resolution failure during ApplyWorkspaceOverride returns {error} rather than escaping uncaught; (AC3) a valid or null turnJson still returns {success:true}. Origin: BUG-TRIAGE-070/075 (JsonSerializer.Deserialize and ApplyWorkspaceOverride were outside the try in FinalizeLifecycleTurnToolAsync/UpsertLifecycleTurnToolAsync, FwhMcpTools.SessionLog.cs). Validated by TEST-MCP-SESSIONLOG-001.
**Covered by:** FR: FR-SUPPORT-012; TEST: TEST-MCP-SESSIONLOG-001, TEST-MCP-SESSIONLOG-002, TEST-MCP-SESSIONLOG-003, TEST-MCP-SESSIONLOG-005, TEST-SUPPORT-010B-1, TEST-SUPPORT-010B-2
**Status:** pending
Scope: layer-1+

## TR-MCP-SESSIONLOG-002

**Session-log text search covers dialog and child collections** — Session-log query text search SHALL match content in processing-dialog items, actions (description/type/filePath), commits (message/sha/branch/files), string-list sections (design decisions / requirements discovered / files modified / blockers), tags, and context items - not only the four turn scalar fields (QueryText, QueryTitle, Response, Interpretation). Acceptance Criteria: (AC1) a query whose term exists only in a processing-dialog item returns the session; (AC2) a query whose term exists only in an action description returns the session; (AC3) existing scalar-field and boolean queries still return the correct sessions. Origin: BUG-TRIAGE-070 (SessionLogService.BuildSearchText joined only the four scalar fields). The full turn graph is Include-loaded in QueryAsync before the client-side matcher runs, so child navigations are populated for both SQLite and SQL Server. Validated by TEST-MCP-SESSIONLOG-002.
**Covered by:** FR: FR-SUPPORT-012; TEST: TEST-MCP-SESSIONLOG-001, TEST-MCP-SESSIONLOG-002, TEST-MCP-SESSIONLOG-003, TEST-MCP-SESSIONLOG-005, TEST-SUPPORT-010B-1, TEST-SUPPORT-010B-2
**Status:** pending
Scope: layer-1+

## TR-MCP-SESSIONLOG-003

**Terminal-turn compliance gate is QBAgent-only** — The terminal-turn decision/action/commit compliance gate (ValidateTerminalTurnCompliance) SHALL apply only to the QBAgent ACID source type; standard agents (ClaudeCode, Cursor, Copilot, ...) SHALL be able to complete or fail a turn with no decision/action/commit items. The sessionlog_complete_turn and sessionlog_fail_turn MCP tool descriptions SHALL state this QBAgent-only scope so operators/triage do not misattribute plugin-side completion failures to a server gate. Acceptance Criteria: (AC1) UpsertTurnAsync with a completed empty turn is accepted for a ClaudeCode session and rejected (ArgumentException) for a QBAgent session; (AC2) the complete/fail tool descriptions state the gate is QBAgent-only. Origin: BUG-TRIAGE-082/083 (reporter hypothesized the server gate blocked ClaudeCode completions; the gate is QBAgent-only, so the real cause is plugin-local). Validated by TEST-MCP-SESSIONLOG-003.
**Covered by:** FR: FR-SUPPORT-012; TEST: TEST-MCP-SESSIONLOG-001, TEST-MCP-SESSIONLOG-002, TEST-MCP-SESSIONLOG-003, TEST-MCP-SESSIONLOG-005, TEST-SUPPORT-010B-1, TEST-SUPPORT-010B-2
**Status:** pending
Scope: layer-1+

## TR-MCP-SESSIONLOG-005

**Dedicated session and turn title-update service, endpoints, and client** — ISessionLogService MUST expose SetSessionTitleAsync(sourceType, sessionId, title) and SetTurnTitleAsync(sourceType, sessionId, requestId, title) that unconditionally set the title on an existing session or turn (throwing when the session or turn does not exist), save, and publish a change event. SessionLogService, the TransactionGated decorator, and the Federated decorator MUST implement or delegate these. The capability MUST be reachable via REST on SessionLogController and via the typed SessionLogClient. Validated by TEST-MCP-SESSIONLOG-005. Covered by FR: FR-SUPPORT-012. Status: pending. Scope: layer-1+.
**Covered by:** FR: FR-SUPPORT-012; TEST: TEST-MCP-SESSIONLOG-001, TEST-MCP-SESSIONLOG-002, TEST-MCP-SESSIONLOG-003, TEST-MCP-SESSIONLOG-005, TEST-SUPPORT-010B-1, TEST-SUPPORT-010B-2
**Status:** pending
Scope: layer-1+

## TR-MCP-SESSIONLOG-006

**Required planFile and todoId scalars with None sentinel and backfill** — Session-log turn storage, DTOs, REST, MCP tools, and plugin wrappers SHALL expose required string fields planFile and todoId. New interactive creates/submits/beginTurn/replace-turn reject omitted, null, or empty values. Accepted planFile values are a workspace-relative path, an exact absolute path, a ~/ home-relative path, or the literal sentinel None. Accepted todoId values are a canonical MCP TODO id, ISSUE-N, or None. An EF migration adds both columns, defaults existing rows to None, then a one-time backfill scans turn contents and agent history under ~. Uncertain extractions remain None. Query/get results SHALL return both fields.
**Covered by:** FR: FR-MCP-SESSIONLOGCTX-001; TEST: TEST-MCP-SESSIONLOG-006
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] todoId is None, canonical TODO id, or ISSUE-N. FR/TR/TEST ids fail. planFile is None or a normalized relative/exact/~/ path, max 2048, no .. .
- [ ] Additive upsert/complete omits preserve stored values. Supplied values are validated. ReplaceTurnAsync requires both fields.
- [ ] Extractor scans turn text, tags, context, files, actions, dialog, and agent history under ~. Tag TODO ids win. Ties and FR/TR/TEST ids yield None.
- [ ] SessionLogTurns.PlanFile required string max 2048 default None. TodoId required string max 128 default None. Three provider migrations add both columns.
- [ ] Backfill writes extractor hits onto None columns only. Non-None is not overwritten. Rerun is a no-op.
- [ ] POST begin and sessionlog_begin_turn require planFile and todoId. Missing is 400/error. None/None succeeds.
- [ ] SessionLogClient.BeginTurnAsync, McpAgent BeginTurnAsync, and REPL beginTurn serialize and persist the supplied values.
- [ ] Read projection copies planFile and todoId onto a clone. Source is unchanged. None is not rewritten.

