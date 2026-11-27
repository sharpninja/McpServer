# TEST-MCP-SESSIONLOG

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-SESSIONLOG

### TEST-MCP-SESSIONLOG-001

Validates TR-MCP-SESSIONLOG-001. tests/McpServer.Support.Mcp.Tests/McpStdio/SessionLogLifecycleToolErrorTests.cs: SessionLogCompleteTurn_MalformedTurnJson_ReturnsStructuredError and SessionLogFailTurn_MalformedTurnJson_ReturnsStructuredError assert a malformed turnJson yields a JSON {error} (with message, no success) instead of a thrown JsonException; SessionLogCompleteTurn_NullTurnJson_ReturnsSuccess asserts the happy path still returns {success:true}. Red before the fix (2 of 3 threw), green after moving the deserialize into a try/catch and ApplyWorkspaceOverride inside the service try.


### TEST-MCP-SESSIONLOG-002

Validates TR-MCP-SESSIONLOG-002. tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTests.cs: QueryAsync_TextMatchesProcessingDialogContent seeds a session whose unique token exists only in a ProcessingDialog item Content and asserts the text query returns it; QueryAsync_TextMatchesActionDescription does the same for an action Description. Red before widening BuildSearchText (both returned 0), green after. Existing QueryAsync scalar/boolean search tests (WhenQueryingByBooleanTextThenTermsCanMatchAcrossTurnFields et al.) remain green as the AC3 regression guard.


### TEST-MCP-SESSIONLOG-003

Validates TR-MCP-SESSIONLOG-003. tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTests.cs: UpsertTurnAsync_CompletedEmptyTurn_AcceptedForClaudeCode_RejectedForQBAgent seeds a ClaudeCode session and a QBAgent session, then asserts a completed turn with zero decisions/actions/commits returns a turnId for ClaudeCode and throws ArgumentException for QBAgent. Regression guard locking the QBAgent-only scope so a future broadening of the gate to standard agents fails. Tool-description accuracy (AC2) applied in FwhMcpTools.SessionLog.cs.


### TEST-MCP-SESSIONLOG-005

Validates TR-MCP-SESSIONLOG-005. tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTests.cs: SetSessionTitleAsync unconditionally changes an existing session Title; SetTurnTitleAsync unconditionally changes an existing turn QueryTitle; both throw InvalidOperationException when the session or turn does not exist. Red before the methods exist, green after. Scope: layer-1+.


### TEST-MCP-SESSIONLOG-006

Validates AC-FR-MCP-SESSIONLOGCTX-001-001 through 007 and AC-TR-MCP-SESSIONLOG-006-001 through 008. Tests: SessionLogTurnContextValidatorTests, SessionLogServiceTurnContextTests, SessionLogTurnContextExtractorTests, SessionLogTurnContextBackfillTests, SessionLogTurnPlanFileTodoIdModelTests, AddSessionLogTurnPlanFileAndTodoIdMigrationTests, SessionLogControllerTests.BeginTurn_MissingFields_Returns400, BeginTurn_NoneNone_Returns201_AndGetReturnsNone, Query_FilterByTodoId_ReturnsOnlyMatches, SessionLogBeginTurn_MissingPlanFile_ReturnsStructuredError, SessionLogBeginTurn_NoneNone_ReturnsSuccess, BeginTurnAsync_SerializesPlanFileAndTodoId, BeginTurnAsync_PersistsPlanFileAndTodoId, BeginTurnAsync_ForwardsPlanFileAndTodoId, SanitizeTurn_CopiesPlanFileAndTodoId, SanitizeTurn_DoesNotMutateSource, SanitizeTurn_LeavesNoneUnchanged, Import_OmittedFields_PersistsExtractorResultOrNone, Ingest_OmittedFields_PersistsExtractorResultOrNone, Apply_OmittedFields_PersistsProperValue, SqliteMigration_UpAddsColumns_DownDropsThem, Invoke-WorkflowBeginTurn_* plugin contract tests.

**Acceptance Criteria:**
- [ ] Round-trip planFile/todoId on persist and get.
- [ ] None sentinel persists and is never rewritten to null.
- [ ] New persist without fields is rejected.
- [ ] Relative, exact, and ~/ plan paths are accepted; .. is rejected.
- [ ] Query filter and text search match planFile and todoId.
- [ ] Backfill upgrades None from turn contents and ~ history.
- [ ] Import/ingest/federation persist a validated pair, never null.
- [ ] Validator rules covered by SessionLogTurnContextValidatorTests.
- [ ] Additive omit-preserve and replace-required covered by SessionLogServiceTurnContextTests.
- [ ] Extractor ranking covered by SessionLogTurnContextExtractorTests.
- [ ] Schema/migration covered by model and provider migration tests.
- [ ] Backfill None-only upgrades covered by SessionLogTurnContextBackfillTests.
- [ ] REST/MCP begin required fields covered by controller and tool tests.
- [ ] Client/agent/REPL carry fields covered by their session-log tests.
- [ ] Sanitizer clones new fields covered by SessionLogSanitizerTests.
